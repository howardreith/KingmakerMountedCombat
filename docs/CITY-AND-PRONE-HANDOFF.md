# City toolbar, city companion access and prone ranged target: handoff (2026-11-21)

Mission: [MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md). Branch
`codex/mounted-city-and-prone-fixes` from `main` `dbf63702` (0.2.13). Draft PR #57. Development candidate only: no
merge, release, tag, install or native run.

Native contracts below were read from the installed Kingmaker 2.1.7b assembly (MVID `07fa1e4d…`) through the owner's
private local decompilation (read-only reference, nothing imported). The members this change depends on are pinned in
`scripts/Test-AssemblyContracts.ps1` (section "City companion roster" / "Toolbar persistence").

Review round 1 (Codex, `changes_required`, findings R1-F1…F9) was addressed in the commit after `78a0964e`; see
"Round 1 corrections" below.

## 1. Mount / Dismount shortcut disappears (toolbar persistence)

**Established cause.** The public control `KMC_MountStateAbility` is a runtime fact KMC leases; a toolbar slot binds
its `AbilityData`. Native `UnitUISettings.GetSlot` and `CleanupBadSlots` replace any ability slot whose fact is
removed or inactive (`MechanicActionBarSlotAbility.IsBad`) with an empty slot, unless a live fact of the same blueprint
exists at that moment. Several ordinary paths make the fact temporarily absent:

- area unload with a mounted pair: `SuspendAreaControls` removes the facts **and empties the KMC slots itself**; only
  the narrow capture/restore rows put the slot back, and they skip carried pairs that stay dismounted or are retained
  for retry ("keeps its current control bindings", which by then are empty) and every row after a failed restoration;
- the capital (`CapitalCompanionLogic.RemoveAllCompanions`) moves every active companion to `RemoteCompanions` and takes
  custom companions out of the game, so their riders leave KMC's candidate roster and lose the fact;
- a save made in the capital writes no KMC row for such a rider (`CapturePersistentSlots` only reads live facts of
  current candidates) while the native save keeps a stale KMC slot.

Once the slot is empty, the one-time placement record stops any re-placement, so the shortcut is gone for good.
Which of these paths the owner hit is not established (no owner log or save); the city path matches the report.

**Fix (existing ownership and persistence path, no second manager).** `MountedControlPlacementIntent` (domain) keeps
each rider's *arrangement* by actor id: a chosen slot, or "off the bar" (the player removed the live control, filled
its slot meanwhile, or the one placement found a full bar). It holds ids, indices and the pet link id only. Its
`Reconcile` method holds the per-update decisions in the service's order, and `NativeMountedControlService` only
reads and writes the bar:
- a live control in a slot is the current arrangement (a move updates it);
- a missing, inactive or duplicated desired public fact that the update replaces is recorded as a temporary absence
  **before** the arrangement is read, so a replacement after native bad-slot cleanup returns to the chosen slot
  (R1-F3);
- KMC removing the facts (area unload, save scope, disable) or the rider leaving the candidate roster while still on
  the player's roster (party, detached, reserve) is a temporary absence; the KMC slots of a departing rider are
  emptied (native would, and no native save holds a KMC identity);
- at a boundary that empties the slots (save scope, area unload) the bar as it stands is observed first, so a move
  or removal since the last update is authoritative; while a save scope's own leases are pending, they alone put
  the emptied slots back (R1-F2);
- a live control on no slot returns only after a temporary absence and only into an empty slot; otherwise the
  arrangement becomes off-bar and is never re-added; one copy only;
- an arrangement is retired on positive evidence of permanent loss, re-checked every update for absent riders and
  before saving: the rider left every player roster, or its pet link (`UnitDescriptor.PetUniqueId`) is cleared or names
  another unit than the one it was observed with; an unresolvable rider proves nothing and keeps it (R1-F5). Disable,
  disposal and a new load clear all arrangements; roster membership is matched by actor id, so a load's replaced
  objects never erase a seeded arrangement;
- the owned save archive carries an absent rider's arrangement as the existing rows: its chosen slot (kind 6) and its
  placement record (kind 7), or the placement record alone when the control is off the bar (R1-F1); only for a rider
  that resolves uniquely in `Player.AllCharacters`. Every required row is written; more than 128 rows fails the
  archive's own validation (the existing bounded failure path) instead of silently dropping a rider (R1-F7);
- `RestorePersistentSlots(rows, loadedWorld)`: a freshly loaded save initialises the arrangements from its rows; an
  in-world replay (area transfer, retained remount) seeds only a rider without an arrangement, and its slot-writing
  loop writes a public-control row only where the rider's current arrangement puts it, so an older row never
  resurrects a moved or removed control (R1-F4).

**Save format:** unchanged (same row kinds, bounds and validation). A 0.2.x reader skips a kind-6 row of an actor that
is not a candidate and keeps the placement record. Pre-F2 builds already read any kind 6/7 archive as invalid KMC
metadata (docs/FINALIZE-F2-CANONICAL-CONTROLS.md); downgrade stays unsupported, as before.

**Round 2 additions.**
- *Native cleanup observed when it decides (R1-F3):* an exact-token postfix on `MechanicActionBarSlotAbility.IsBad`
  (token `06002F6E`) reports `true` for a slot holding the public control to
  `NativeMountedControlService.ObserveInvalidatedPublicSlot`, which records a temporary absence. Native `GetSlot` and
  `CleanupBadSlots` call `IsBad` before replacing a slot (pinned contract), so a slot native empties is never later read
  as the player's removal, even when the fact is active again before the next update. Every other slot is ignored.
  The earlier "within one frame" residual is closed. The remaining native whole-bar reset is respec
  (`UnitUISettings.CleanupSlots`, called by `Player` respec), which also replaces the pet.
- *Archive header before the next update (R2-F1):* `CapturePersistentSlots(includeAbsentIntent: true)` first classifies
  each reconciled rider's bar and fact (binding by blueprint even if its fact is inactive; a not-leaseable fact is a
  temporary absence unless the pet link proves loss), and a rider whose live control waits to return writes its slot
  row with one placement record.
- *Roster departure (R2-F2):* the departure loop observes the departing rider's bar before recording the absence and
  emptying its slots.
- *Unresolved pet (R2-F3):* a reconciled rider that is not leaseable right now keeps its arrangement unless its
  persistent pet link (`PetUniqueId`) is cleared or names another unit.
- *Lease release (R2-F4):* restoring the public control from a save-scope lease records the rebind (`ObserveBound`), so a
  removal right after the save is the player's.

**Round 3 additions.**
- *Exact invalidated slot (R1-F3):* `ObserveInvalidatedPublicSlot` locates the reported slot object on the rider's own
  bar (reference identity, before native replaces it) and records that index as the chosen slot plus the absence; a
  move or a first manual placement since the last update is therefore what returns. Slot objects not on a current-world
  rider's bar are ignored.
- *Stale binding at the header (R2-F1):* when the public fact is active but the slot holding the public control still
  carries older data (replaced fact, slot not yet rebound), the live capture cannot write it (callable live data only);
  the classification records it as the remembered slot, so the archive carries kind 6 at that slot plus one kind 7,
  and restore binds the current data.
- *Departure before cleanup (R2-F2):* the save header also handles still-observed current-world riders that left the
  candidate roster but whose departure cleanup has not run (`ObserveDeparture`, shared with that cleanup): their last
  bar is observed and the departure classified before rows are emitted.
- *World boundary (R3-F1):* each observed unit records the player world it was first reconciled in. An earlier world's
  object (a load replaces the world and its objects, keeping actor ids) is cleaned up but never observed into, never
  marks an absence for, and is never reported by native cleanup into the loaded world's arrangements; roster membership
  is checked by exact instance.

**Round 4 additions.**
- *Header vs live writer (R2-F1):* the header carries a bound public slot as the remembered slot whenever the live
  capture will not write it, using the writer's own conditions (`LiveCaptureWritesPublicSlot`: ShouldLease, active
  fact, exact data, caster) — including a rider whose pet briefly does not resolve while its old fact is still active —
  as long as the rider still gets the control or its pet link names the same companion.
- *Before withdrawal (R4-F1):* when an update is about to withdraw or replace a live public fact, the bar is observed
  first, so a removal made since the last update stays a removal; an absence native cleanup already reported stays.
- *Permanent loss clears the slot (R4-F2):* when a reconciled rider no longer gets the control and its pet link is
  cleared or names another unit (`ProvesPermanentLoss`), its public-control slots are emptied, even on a bar native UI
  never inspected; other slots and same-pet temporary absence are untouched.
- *Area suspension (R4-F3):* `SuspendAreaControls` classifies still-observed former candidates (`ObserveDeparture`, the
  save header's rule) before `RemoveAllManagedFacts(true)` empties their bars.
- *Load fence (R3-F1):* the production load path now calls `BeginSelectedLoad`, which clears the records and fences
  the outgoing player object (`MountedControlLoadFence`): until native loading replaces
  `Game.Instance.State.PlayerState` (2.1.7b `ThreadedGameLoader` deserializes a new `Player`), `Update` reconciles
  nothing and no observation (native cleanup, bar observation, absence) writes; the fence lifts when the player object
  differs, or when the load is abandoned (`MountedPersistenceService` calls `AbandonLoadedWorld`). `BeginLoadedWorld`
  alone stays fence-free for the two diagnostics that re-arm the first placement within one world.

**Round 5 additions.**
- *Observe before any fact change (R4-F1):* the pre-update observation now runs whenever the update will withdraw,
  replace or re-lease the public fact, including a fact native already deactivated (no slot, so no `IsBad`); a removal
  made while the fact was live stays a removal, and absences already recorded (native cleanup, KMC suspension, a load)
  stay pending.
- *Direct change of pets (R4-F2):* `RetireOnPetChange` runs before anything can overwrite the stored pet link (start of
  each candidate's reconciliation, every bar observation including the save header and departures, and the native
  cleanup report): a nonempty link naming a different companion (`PetLinkChanged`) retires the arrangement and clears
  the rider's public-control slots. The same id temporarily unresolved still keeps and restores the slot.
- *Abandoned and superseded loads (R5-F1, R5-F2):* `MountedControlLoadFence` now belongs to its load scope
  (`BeginSelectedLoad(scope)`) and moves the outgoing records (arrangements and placement records) aside instead of
  discarding them. `AbandonLoadedWorld(scope)` from the owning scope restores them exactly when the outgoing world is
  still current (reserve slots and deliberate removals the bar cannot reconstruct), and drops them after a world
  replacement. A superseded scope's end callback now abandons its own fence; another scope's fence is never released;
  a newer scope that starts on the same fenced world takes ownership of the records already moved aside.

**Round 6 additions.**
- *Cleared pet link at native cleanup (R4-F2):* `MountedControlPlacementIntent.ObserveNativeSlotInvalidated` checks the
  companion identity before it records anything: a cleared link (or one naming another companion) is positive evidence
  that the slot's companion was removed, so the arrangement is retired instead of being re-recorded with an empty link
  that a later pet could adopt. A pet B linked before the first update therefore gets no slot (a placement record
  only); the same persistent pet id briefly unresolved still returns to its slot once.
- *Native cleanup during an abandoned load (R5-F1):* while a selected load fences the outgoing world, the native
  cleanup report goes to the load scope's outgoing records (`MountedControlLoadFence.ObserveOutgoingSlotInvalidated`,
  current world must be the fenced one). `AbandonLoadedWorld` restores those records including the cleanup, so the
  slot returns once in the surviving world; a successful world replacement drops them with the rest of the outgoing
  records.

**Round 7 additions.**
- *Companion loss at every boundary (R4-F2):* `RetireOnCompanionLoss` (domain `MountedControlPlacementIntent`
  method of the same name) replaces the pet-change check at each update, every bar observation (save header, area
  suspension, departures) and the native cleanup report. It retires on a link naming another companion (as before),
  and also on a cleared link while the rider no longer gets the control. A mounted rider whose relationship faulted
  still gets the control and keeps its slot. An empty link never overwrites the companion identity already stored.
  A retired actor is remembered for the rest of the world. An in-world replay (area transfer, retained remount) of
  rows captured before the retirement neither seeds nor writes that slot. A freshly loaded world starts with none.
  Same-id temporary non-resolution keeps and restores the slot as before.
- *Older capital saves (R7-F1):* `AdmitReserveBindings` runs at each update, at the save header's classification and
  at the save scope. Once per unit object it looks at current-world riders on the reserve or detached roster that are
  neither candidates nor still observed. An identifiable public-control binding (the public control, or an older exact
  Mount or Dismount binding) of a rider whose pet link names a companion is recovered as its chosen slot, with its
  placement record. Every KMC binding on that bar is then cleared, since no fact stands behind it, and unrelated slots
  are untouched. A cleared link recovers nothing and still clears the binding. The save scope also leases any KMC
  binding still on a reserve or detached bar. Disable and disposal clear those bindings too.

**Round 8 additions.**
- *Older reserve bars follow the migration rules (R8-F1):* reserve recovery now uses
  `MountedControlSlotPolicy.RecoveredPublicSlot`, the same precedence as `ReconcilePublicControlSlots`. An existing
  public Mount / Dismount binding wins over an older Mount or Dismount alias, and the first exact alias is used only
  when no public binding exists. While the KMC bindings are cleared, an older Mounted Charge becomes the rider's own
  stock Charge in the same slot (`ResolveStockChargeForSlot`: only when that Charge is available and not already on the
  bar, one per bar). Every other KMC binding is emptied and unrelated slots are untouched.
- *A still-entitled rider keeps its interrupted slot (R8-F2):* in live, unbound reconciliation
  (`MountedControlPlacementIntent.Reconcile`) an empty pet link no longer revalidates the arrangement away. The control
  is live, so a mounted or faulted relationship still grants Dismount. The known identity and the interrupted index
  are kept, and the control returns to its chosen empty slot. A link naming another companion still retires the
  arrangement there, and a cleared link after entitlement ends still retires it (`RetireOnCompanionLoss`).

**Round 9 additions.**
- *Identity before registration (R9-F1):* `ResolveKind` now delegates to `MountedControlSlotPolicy.ResolveExactKind`,
  which returns None for a missing blueprint (an item or another non-ability slot) before any comparison. Before or
  during registration a null control field can no longer match it. Disable, disposal, reserve cleanup and the save
  scope's lease capture therefore touch only identifiable KMC bindings.
- *Lease release after a companion change (R9-F2):* `ClearSerializationHotbarLeases` puts the public control back
  only while `AllowsLeaseRestore` holds. That means the leased index is still the rider's arrangement and its pet link
  names the same companion. A retirement, an off-bar arrangement or a different slot recorded by the scope-ending
  update wins. Same-companion restores and the placeholder check are unchanged.
- *Scan bookkeeping lifetime (R9-F3):* the reserve scan uses `MountedScanLedger`. On every update (before any early
  return) and at every scan it is synchronized with the current player and reserve roster: units that left the roster
  are dropped, and everything (the world included) is dropped when the world changes or none is current. It is also
  reset when a selected load begins, on disable and on disposal. A unit is scanned once per stay on the reserve roster.
  A rescan is idempotent: cleared bindings stay cleared and an existing arrangement wins.
- *A party rider's older alias while its pet is unresolved (R9-F4):* `RecoverLegacyAlias` records an exact older
  Mount / Dismount alias as the chosen slot before repair empties it. It runs at the start of
  `ReconcilePublicControlSlots` when the public control is not live, and in every bar observation (so archive capture
  before the first update sees it). It uses `RecoveredPublicSlot`'s precedence, so a canonical binding wins, and the
  reserve rule: a different companion, or a cleared link while the rider no longer gets the control, recovers
  nothing, and an existing arrangement wins.

**Round 10 additions.**
- *One row for a live alias saved before migration (R10-F1):* the live writer now counts an older Mount / Dismount
  row it writes (kind 1 or 2, still restored as the public control) as the actor's public binding
  (`MountedControlSlotPolicy.IsPublicBindingRow`). The remembered-row writer adds rows only through
  `AbsentRowsAfterLive`, which adds nothing when the live writer already wrote that binding and never adds a second
  placement record. The archive gets the chosen index once and one placement record. Its duplicate-key validation is
  unchanged; `Test-CityArchiveRowBound.ps1` checks both alias kinds through the production codec, with a duplicate
  control refused.
- *Leased aliases honor retirement (R9-F2):* the save-scope lease release applies `AllowsLeaseRestore` to every
  public binding (`IsPublicControlKind`): the canonical control and an older Mount / Dismount alias. A slot retired for
  a replaced companion stays empty. The same companion's alias comes back and the next update migrates it.
- *Native cleanup of an inactive alias (R9-F4):* the `IsBad` observer now also handles an older exact Mount /
  Dismount alias. Before native empties it, the bar's chosen alias index is recorded through `RecoverLegacyAlias`,
  with the same precedence (a canonical binding on the bar wins), companion rules and existing-arrangement rule.
  While a selected load fences the world, the index goes to the load scope's outgoing records
  (`MountedControlLoadFence.AdmitOutgoingSurvivingBinding`).

## 2. Animal companions in the city's inventory and character screens

**Established cause.** In a capital the full-screen Inventory, Character and Spellbook screens use `GroupController`
with `WithRemote` and `WithPet`, i.e. `UIUtility.GetGroup(withRemote: true, withPet: true)`
(token `06004281`). It lists every party and reserve companion, in or out of the game, but adds pets only from
`Player.ControllableCharacters`, which requires `IsInGame`. `CapitalCompanionLogic` sets custom companions (and
companions without a capital spawn place or inactive dual companions) out of the game, and `OnIsInGameChanged` takes
their pets out with them. Those owners appear; their animal companions do not. The main character's pet and in-game
companions' pets were already listed natively. Native record: batch199 row 59 (`3842ad94`, 2026-10-24) recorded the
hired Small Ranger in Oleg's as remote and out of game, and its Horse out of game.

**Fix.** A bounded UI roster projection: an exact-token Harmony postfix on `UIUtility.GetGroup` delegates to
`CityCompanionRosterAdapter` → `CityCompanionRosterProjection` (domain). Only while Mounted Combat is enabled (UMM
toggle on; the patches stay installed until disposal, so a disabled mod returns the native list itself, R1-F6) and
only in a capital with both flags (the full-screen pet roster; Vendor and field rosters unchanged), each listed non-pet
owner's own pet is inserted right after it when the link is reciprocal (`Descriptor.Pet` / `Master`), the pet is not a
summon, is one of the player's cross-scene characters, and the owner is not detached or an ex-companion (native's own
pet rules); never twice. The existing persistent pet instance is listed, so native selection, inventory, doll and
character sheet bind that unit. Nothing changes party membership, ownership, `IsInGame`, scenes, formation, XP or
combat. A native-rule failure falls back to the native list (logged once). Dead pets follow native (not filtered).
Selection fallback when a selected pet leaves the roster is native (`GroupController.UpdateSelectedUnits` picks the
first listed unit). Rename keeps working (eligibility reads `Player.AllCharacters`).

**Shared cause?** The two city issues share a **trigger** (the capital's native roster change, `CapitalCompanionLogic`)
but not a defect: issue 1 is KMC's slot persistence through temporary fact absence; issue 2 is native `GetGroup`'s
in-game pet filter.

## 3. Mounted ranged attack against a prone target

**Unconscious (downed) enemy — established mechanism and fix.** An unconscious unit is prone
(`UnitProneController.ShouldBeProne`), lying on the ground. Native `UnitAttack.Init` records
`m_IsUnconsciousAtStartOfCommand`; `UpdateTarget` keeps an explicit order on an enemy that was already unconscious when
ordered (and abandons one that falls unconscious later), so an unmounted player can shoot a downed enemy. The mounted
pair path required a conscious target at every gate: the stock-click classification, the intent driver
(`MountedCombatController` → `IsValidTarget` → `CancelInvalidIntent`, silent, observation
`intent-cancel-target-invalid`), the admission context ("Choose one valid living target.") and the pair command
(`ValidateNativeSequenceTarget`, `TryEndExpectedTargetInvalidation`, ranged-tail and routine continuation). Stage:
command admission / intent validation, before approach or firing; RT and TB alike (shared path). Fix:
`UnifiedMountedStockAttackPolicy.TargetLifeAdmits` mirrors the native rule; `MountedAttackIntent` records
`TargetUnconsciousWhenOrdered` for a player order only; `MountedPairAttackCommand.AdmitTargetUnconsciousWhenOrdered`
keeps that exact target valid while it lives. Unchanged: automatic proposals, routine continuation to the next target,
Charge, a target that falls unconscious after the order, dead targets, modifiers, ammunition, costs, LoS/range, approach.

**Conscious, tripped enemy — traced separately (R1-F9), no mounted divergence found in code.** Stage by stage, a
conscious prone target differs from a standing one only where native itself differs, identically mounted or not:
- *Mouse targeting:* `UnitEntityView.EnterProneState` sets `IsProne`, stops movement and drops the unit's movement
  agent from obstacle groups; it does not move the colliders. The core collider follows the torso only for a **dead**
  unit (`SwitchCoreColliderToDeadState`), the soft collider keeps its setup pose. So clicking the lying body of a
  conscious prone enemy can miss the colliders and hit the ground; that is native and target-side, not rider-side.
  A missed unit click becomes a ground order (for a mounted rider, KMC moves the pair, which looks like "won't shoot").
- *Click classification:* `ClickUnitHandler.GetPriority` / `OnClick` have no prone branch; a hit unit click gives the
  nearest selected unit a `UnitAttack` and raises `OnAttackRequested`, mounted or not.
- *Admission:* KMC's gates read conscious/finally-dead/hostile/attackable/visible; a conscious prone target passes
  exactly as a standing one (life-state is the only state these gates read).
- *Approach, range, line of sight, firing:* no prone term in native `UnitCommand`/`UnitAttack` or KMC's pair command;
  distance uses positions and corpulence, unchanged by prone; LoS reads the static geometry and eye position.
So for a conscious prone enemy the code predicts the same mounted and unmounted outcome; the plausible native-only
difficulty is clicking the lying body. This is a code trace, not an observation; native legs are NOT RUN (below).
The reported weapon and the enemy's state are not identifiable from available evidence.

## Round 1 corrections (R1-F1…F9)

| Finding | Disposition | Where |
| --- | --- | --- |
| F1 removal lost by a reserve save | fixed: off-bar arrangements, record-only rows | `MountedControlPlacementIntent`, `CaptureAbsentIntent`, `MountedControlSlotPolicy.AbsentArrangementRows` |
| F2 edit between update and save | fixed: boundary observation, lease authority | `ObserveArrangementsAtBoundary`, `Reconcile(leasePending)` |
| F3 re-lease erased the absence | fixed: replacement recorded before reading | `ReconcileFacts` → `Reconcile(factReplaced)` |
| F4 replay resurrected a removal | fixed: `loadedWorld` seeding, write guard | `RestorePersistentSlots`, `Seed`, `AllowsSavedSlot` |
| F5 permanent loss in reserve | fixed: pet-link/roster revalidation | `Revalidate`, `RevalidateAbsentArrangements` |
| F6 projection after disable | fixed: enabled gate | `CityCompanionRosterProjection.ProjectRoster`, `NativeMountedControlService.IsActive` |
| F7 silent truncation | fixed: no truncation; archive validation refuses >128 | `CaptureAbsentIntent`; `MountedArchiveData.ValidateSlots` now reads `MountedControlSlotPolicy.FitsArchive` (same bound, tested) |
| F8 fixture description | corrected (this document, "Native checks") | — |
| F9 conscious prone not traced | traced (section 3); native legs NOT RUN with prerequisites | — |

Affected neighbours (round 2, R2-F5): both actual-method adapters' facades now implement the candidate's signatures
(`CapturePersistentSlots(legacySchema, includeAbsentIntent)`, `RestorePersistentSlots(rows, loadedWorld)`) and assert
the modes. `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1` also received the facade members its current actual methods
call (`Report(string,bool)`, no-op `ObserveScenePlacements` / `TryRestoreRetainedIntent`, outside its archive loop) and
the domain sources `MountedArchiveData` needs (`MountedControlSlotPolicy.cs`, `NativeMountedControl.cs`); it now
compiles and passes (22/0, including "actual archive presentation restores its control rows as a freshly loaded world"
and "actual archive capture asks for absent riders' arrangements") and is selected in the focused wrapper.
Round 3 (R2-F5): `Test-CityAreaAdapterBaseline.ps1` (in the focused wrapper) performs that adapter's own compile step
— the same `RestoreAreaPair` extraction, template substitution, csc, flags and source list, guarded against drift by
pinning the adapter script's compile line — once on base `dbf63702` (`git show`) and once on the committed candidate
(`git show HEAD`), records each input's blob id, normalizes diagnostics (tree root and line/column removed) and
compares them as multisets; see the validation table for the counts. Earlier note:
`chunk7\Test-Chunk7AreaRestorePreflightAdapter.ps1` still does not compile; its errors are base-tree errors: at base
`dbf63702`, `RestoreAreaPair` and `MountedPairCandidate` use `transfer.Lifecycle`, `AreaTransfer`/`AreaPair` and the
retained-intent members, `PairHeldByDetachedCleanup` and `MountedAreaControlRule` (`git grep` at `dbf63702`), none of which
its facade (last changed 2026-10-19, `b81dc5f8`) or its source list defines. The candidate diagnostic run
(`Test-Tiers.ps1 -Tier Fast -SkipBuild -Focused chunk7\Test-Chunk7AreaRestorePreflightAdapter.ps1`) reported 63 compile
errors (CS0103/CS1061/CS0246/CS1503/CS0117 against those members), none naming `RestorePersistentSlots` or
`loadedWorld`; the facade's new in-world assertion therefore is not executed. Repairing that facade is outside this mission; it is not
selected.
`Test-Finalize2UpgradeReaders.ps1`'s source locator now matches the stable `CapturePersistentSlots` signature prefix;
that reader remains known-broken (T09, retained r05 evidence absent on this machine) and pins a historical source tree,
so it is not selected.

## Validation (offline, this candidate)

| Command | Result | Limits |
| --- | --- | --- |
| Controller `focused` gate (`agent_review.py verify --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1`) on the reported candidate commit | the PASS of record (receipt bound to that commit; see the final report for commit and duration) | Build, components, Validate-Source, focused wrapper. The rows below are that run's counts |

Local runs before each commit are development runs, not results of record. Round 3: the first local Fast run (with
build) on the uncommitted tree failed in the focused step (`Test-CityAreaAdapterBaseline.ps1`: call-depth overflow, a
helper named like `git`); three `-SkipBuild` retries followed: one failed (a single-element array unrolled to a string),
one failed (an empty-difference wrapping artifact), one passed; the controller gate then passed on `29ffa3a5` in 25.7 s.
Round 4: one local Fast run (with build) on the uncommitted tree passed (components 1443/0, Validate-Source 153/0,
focused 5/5; its area-adapter comparison read the previous commit as HEAD); the controller gate then passed on
`9ce8b7c1` in 22.4 s. Round 5: one local Fast run (with build) on the uncommitted tree passed (components 1447/0,
Validate-Source 153/0, focused 5/5, area comparison against the previous HEAD); the controller gate then passed on
`85d70ff2` in 29.6 s. Round 6: the two new reproductions were first run against a stub keeping the old behavior (the
fence ignored the outgoing cleanup, the cleanup observer re-recorded an empty link): the local Fast run exited 1 (two
runs, with and without build; the tool output was truncated before the FAIL lines, and the only changes from the
all-pass round-5 tree were those two tests and the stub). After the fix, one local Fast run (with build) on the
uncommitted tree passed (components 1449/0 with both new tests passing, Validate-Source 153/0, focused 5/5, area
comparison against the previous HEAD); the controller gate then passed on `d27728b3` in 23.7 s. Round 7: one local
Fast run (with build) on the fixed, uncommitted tree passed (components 1452/0 with the three new tests passing,
Validate-Source 153/0, focused 5/5, area comparison against the previous HEAD). A mutation run on the same tree then
reverted four of the new domain decisions: the cleared-link retirement, the replay block, the reserve recovery and
the identity preservation. That run exited 1; the tool output was truncated before the FAIL lines. The fix was
restored before commit. The controller gate then passed on `9cec5c5d` in 50.4 s. Round 8: a mutation run restored the
two old decisions, the unconditional revalidation and the first-match recovery precedence; it exited 1 (FAIL lines
truncated in the tool output). After restoring the fix, one local Fast run (with build) passed: components 1454/0
with the two new tests passing, Validate-Source 153/0, focused 5/5, area comparison against the previous HEAD. The
Charge migration in the toolbar double mirrors the service's loop; the service's `ResolveStockChargeForSlot` itself
is exercised only in native play. The controller gate then passed on `a3462521` in 24.8 s. Round 9: one local Fast run
(with build) on the fixed tree passed: components 1458/0 with the four new tests passing, Validate-Source 153/0,
focused 5/5. A mutation run then reverted three domain decisions: the null-blueprint rule, the lease-restore check and
the ledger's pruning and release. It exited 1 (FAIL lines truncated in the tool output), and the decisions were
restored before commit. The alias recovery's call sites are service code, mirrored by the toolbar double. The
controller gate then passed on `c856b4e2` in 45.9 s. Round 10: one local Fast run (with build) on the fixed tree
passed: components 1461/0 with the three new tests passing, Validate-Source 153/0, archive bound 5/0 with the two new
alias-codec checks, focused 5/5. A mutation run then made two decisions canonical-only again: `AbsentRowsAfterLive`'s
public-binding check and `IsPublicControlKind`. It exited 1 (FAIL lines truncated), and the decisions were restored
before commit. The toolbar double models the live alias writer, alias leases and alias cleanup; the native `IsBad`
call order on older bars is not exercised offline. The controller gate on the round-10 commit is the result of record.
| components (`bin\Tests`) | 1461/0 | 41 toolbar, 6 city-roster and 3 downed-target tests |
| Validate-Source | 153/0 | source/payload rules |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` (via wrapper) | 820/0 (adds the `IsBad` override row) | reflection-only contracts; Wrath not selected |
| `Test-GunslingerClickComposition.ps1` (via wrapper) | 21/0, both patch orders | substituted native execution, not gameplay |
| `Test-CityArchiveRowBound.ps1` (via wrapper) | 5/0 (adds both live-alias codec checks) | production `MountedArchiveData`/`MountedArchiveCodec` from this worktree; in-memory graphs |
| `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1` (via wrapper) | 22/0 | actual capture/restore methods, typed facades |
| `Test-CityAreaAdapterBaseline.ps1` (via wrapper) | PASS: base `dbf63702` 63 errors, candidate 63 errors, 0 only in either | the area adapter's compile step on both trees; it executes in neither, so its in-world assertion stays unexecuted |

`Test-CityToolbarProneFocused.ps1` is the repository-owned wrapper (the controller's focused gate takes one script
name) that runs exactly the five checks named in it; nothing broader.

## Acceptance record

Evidence types: UNIT = component test with labeled test doubles of native slots/units, decisions from production
domain code; ARCHIVE = production archive DTO, validator and codec compiled from this worktree; ADAPTER = actual
production methods compiled against typed facades; CONTRACT = installed-assembly
reflection; COMPOSITION = real transpilers, substituted engine boundaries; TRACE = source/decompilation reading;
NATIVE = in-game structured evidence.

| Area | Case | Result | Evidence |
| --- | --- | --- | --- |
| Toolbar transitions | Chosen nondefault slot; repeated city entry/exit; interior transition; save scopes | PASS | UNIT |
| Toolbar transitions | Mounted and dismounted arrival in the real capital | NOT RUN | NATIVE (prerequisites below) |
| Toolbar persistence | Reserve rider save/cold load: one slot row + one record; returns to its slot | PASS | UNIT |
| Toolbar persistence | Removal survives a reserve save/load as a record only, never re-added | PASS | UNIT |
| Toolbar persistence | Move/removal right before a save: bar after the save and after loading keep it | PASS | UNIT |
| Toolbar persistence | Native fact invalidation returns once; live removal stays removed | PASS | UNIT |
| Toolbar persistence | In-world replay never resurrects a removed/moved control; fresh rider still restored | PASS | UNIT |
| Toolbar persistence | Permanent pet loss in reserve retires; kept pet round-trips; different pet not reused | PASS | UNIT |
| Toolbar persistence | Occupied slot, another rider, unrelated slots | PASS | UNIT |
| Toolbar persistence | Native cleanup with the fact active again before the update returns the slot once; live removal stays | PASS | UNIT |
| Toolbar persistence | Save header before the next update keeps a just-invalidated binding (6 + one 7); live removal saves 7 only; cold restore | PASS | UNIT |
| Toolbar persistence | Move or removal right before a roster departure is what the reserve save and the return keep | PASS | UNIT |
| Toolbar persistence | Same pet link briefly unresolved keeps the slot; cleared link retires it | PASS | UNIT |
| Toolbar persistence | Removal right after a save scope's lease release stays removed through save and load | PASS | UNIT |
| Toolbar persistence | Archive: 128 rows round-trip ids/indices/kinds; 127 + 2 refused ("Owned archive control slots are not bounded."); off-bar record round-trips | PASS | ARCHIVE (production DTO/validator/codec) |
| Toolbar persistence | Actual archive capture requests absent arrangements; archive presentation restores as a loaded world | PASS | ADAPTER (six-pair, 22/0) |
| Toolbar persistence | Actual area restore replays in-world | NOT RUN | adapter does not compile at base or candidate; matched comparison: identical 63 diagnostics |
| Toolbar persistence | Native cleanup after a move or a first manual placement returns the cleaned slot | PASS | UNIT |
| Toolbar persistence | Header with a replacement fact and a stale slot: kind 6 at the slot + one kind 7; restore binds new data once | PASS | UNIT |
| Toolbar persistence | Reserve save between the roster change and departure cleanup: unchanged, moved and removed arrangements | PASS | UNIT |
| Toolbar persistence | An earlier world's rider object authors nothing in the loaded world (no row, explicit off-bar record) | PASS | UNIT |
| Toolbar persistence | Load fence in production order: update and native cleanup while the outgoing player is current write nothing; abandoned load lifts it | PASS | UNIT |
| Toolbar persistence | Header while the pet briefly does not resolve: kind 6 at 9 + one kind 7; restore binds 9 once; removal record only | PASS | UNIT |
| Toolbar persistence | Removal just before the fact lease is lost stays removed when the same pet returns; native invalidation control returns | PASS | UNIT |
| Toolbar persistence | Confirmed pet loss clears an uninspected public slot; a different pet cannot revive it; same-pet absence keeps it | PASS | UNIT |
| Toolbar persistence | Area suspension classifies a departed rider's recent move (3) or removal before emptying its bar | PASS | UNIT |
| Toolbar persistence | Removal then native deactivation before the update stays removed (save: record only); native cleanup control restores 9 | PASS | UNIT |
| Toolbar persistence | Direct pet change A -> B (B unresolved), update-first and save-first: B never inherits 9; same-id control restores | PASS | UNIT |
| Toolbar persistence | Abandoned load restores a bar-less reserve slot and an autofill off-bar removal (save and party return) | PASS | UNIT |
| Toolbar persistence | Superseded load releases its own fence; unstarted replacement leaves controls running; older scope cannot release a newer fence | PASS | UNIT |
| Toolbar persistence | `MechanicActionBarSlotAbility.IsBad` override identity and its use by GetSlot/CleanupBadSlots | PASS | CONTRACT |
| Toolbar persistence | Native bad-slot replacement this relies on | PASS | CONTRACT |
| Toolbar persistence | Save/load in and out of the real capital, forced dismount, pet unavailability in game | NOT RUN | NATIVE (prerequisites below) |
| City companion UI | Owner/pet mapping, order, exact instance, multiple pets, no duplicates, repeated opening | PASS | UNIT |
| City companion UI | Exclusions; only the capital pet roster; disabled returns the native list, re-enable resumes | PASS | UNIT |
| City companion UI | GroupController uses GetGroup; GetGroup pets from ControllableCharacters; exact token | PASS | CONTRACT |
| City companion UI | Selecting the reserve pet, equipment change, save/load in a real capital | NOT RUN | NATIVE (prerequisites below) |
| Prone ranged attack | Downed enemy: explicit order valid while it lives; later-unconscious abandoned; dead cancelled | PASS | UNIT |
| Prone ranged attack | Rider's own shot dispatched, no mount substitution (RT and TB decisions) | PASS | UNIT |
| Prone ranged attack | Conscious prone enemy: click, admission, approach, range/LoS and firing stages | PASS (no divergence found) | TRACE |
| Prone ranged attack | Matched mounted/dismounted, standing/conscious-prone/unconscious, RT/TB, near and normal distance | NOT RUN | NATIVE (prerequisites below) |
| Negative controls | Brain proposals, Charge and continuation keep the conscious-only rule; downed ally refused | PASS | UNIT |
| Negative controls | Blocked LoS, unavailable actions, out-of-range approach, standing targets, unmounted ranged, mounted melee, mounted penalties | NOT RUN | NATIVE (paths unchanged in code; owner steps in playtest item 5) |
| Shared regressions | Tabletop Rules Expansion click composition, both orders | PASS | COMPOSITION |
| Shared regressions | Existing component suite | PASS | UNIT |

## Native checks: what exists, what each case needs (corrected, R1-F8)

Correction: the earlier statement that no reserve owner/pet fixture exists was wrong. The Chunk 7 capital rows use one:
batch199 r58/r59 (source `3842ad94`, 2026-10-24, DLL MVID `eef3c13c…`) transferred to Oleg's Trading Post (an
`IsCapital` area where `CapitalCompanionLogic` applies), where the hired Small Ranger (pair B rider) became remote and
out of game and its Horse out of game; r58 wrote the capital Manual 309 and r59 cold-loaded it. Those PASS verdicts
belong to that binary and are not evidence for this candidate.

Current prerequisites, per case (none run on this candidate):
1. **Every native case:** the guarded runner (`scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`) validates a
   candidate package bound to the commit (`Validate-Package.ps1`, manifest commit/branch). No command available to
   this enrollment builds that package (packaging is a selected candidate operation), so no native case can start here.
2. **Capital arrival/cold (toolbar persistence and reserve pet presence):** the in-game route and its reader
   (`scripts/runtime/Chunk7CapitalArrivalEvidence.ps1`) predate F2 (2026-11-05): they require exactly two main-rider
   control rows of kinds `2,3` (Dismount + rider primary) and `ManagedHotbarSlotCount` 2. The current product writes
   the public control (kind 6) and its placement record (kind 7), and with this change also pair B's remembered
   arrangement. Running the rows needs a reader revision that distinguishes the main pair's live rows from pair B's
   remembered rows (versioned so b199's verdicts keep their identity), plus same-candidate lineage: batch199 needed
   A = c1–c5 and B = x4s, r58, r59 (8 launches, more than one five-scenario batch).
3. **Reserve pet selection and an equipment interaction:** no existing driver opens the full-screen Inventory or
   Character group panel, selects a portrait in `GroupController` and changes equipment. `Chunk4InspectionScenario`
   opens the character sheet and calls `GroupController.SelectUnit` for a party actor, which is the closest existing
   surface; a reserve-pet leg needs that driver extended plus a reader.
4. **F2 moved/removed shortcut chain (run `20261105-finalize-f2-r05`):** its reader pins r05's retained evidence and
   a historical source tree; the retained evidence is absent here (known issue T09).
5. **Prone ranged legs:** the ordinary longbow fixture (`OrdinaryAttackControlsEvidence.ps1`, standing targets) and
   the Chunk 6a TB longbow cases provide the standing control legs. No existing diagnostic makes an *enemy* prone
   (`Chunk7CombatNativeLife.Prone` knocks the rider prone) or unconscious before a click; both legs need new
   diagnostic setup (changing binary identity) and reader rows recording posture and consciousness at the click,
   attacker, weapon, target, distance, LoS, action availability and the attack roll or cancellation stage.

## Owner playtest (exact steps)

1. Main character or a companion with a Horse: put Mount / Dismount in a non-default slot (e.g. slot 9). Travel to the
   capital, walk to an interior and back, leave, return to adventuring. The shortcut stays in slot 9. Repeat mounted.
   Save and load inside the capital and outside; the slot holds.
2. Remove the shortcut from the bar; visit the capital, save there, load, leave: it does not come back. Move it to
   another slot and save at once; load: it is in the new slot only. Put another ability where it was before a visit:
   it is not overwritten. If an older capital save exists in which a reserve companion still shows Mount / Dismount
   on its bar, load it, save at once in the capital, reload and bring that companion into the party: the shortcut
   comes back in the same slot, once. With a second, dismounted rider, release or replace its Horse just before an
   area transition (while another pair is mounted): its old slot stays empty for the new companion.
3. A mercenary/custom companion with a Horse: in the capital open Inventory (I) and Character (C): the Horse appears
   after its owner; select it, check its name, statistics and barding/neck slots, equip and unequip an item; close and
   reopen; save, load and reopen. No duplicate portraits. Disable Mounted Combat in UMM: the Horse is no longer listed;
   re-enable: it is listed once.
   **Combat setup S (items 4 and 5).** The rider is a single-attack longbow user: base attack bonus below +6, no
   Rapid Shot, Manyshot or haste effect, and no other extra-attack source, so one attack action is one roll. If the
   only available rider has extra attacks, keep them and use the matched control instead: the mounted routine must have
   the same number of rolls and the same action cost as the same rider's unmounted routine in the same mode, position
   and target (a legal native full attack then passes; an additional routine or a second action spend fails). In turn
   based, read the action bar's spent actions after each click; the routine ends when the action bar shows the action
   spent and no further roll follows before you act again. In real time, use one counting window for every run,
   mounted and dismounted alike: start the game paused, click, unpause, note the game time of the first roll in the
   log, let the game run until about 5.5 game seconds after that first roll (just under one real-time round of
   6 seconds: the routine of that round falls inside it, the next automatic round's first roll does not), then pause
   and count the rolls logged inside the window. Rolls after the window belong to the next automatic round and are not
   counted. Pass: under setup S, exactly one roll in the window; with the
   fallback, the same number of rolls as the dismounted run's window. Fail: a roll more (or fewer) than the matched
   count inside the window, including a delayed extra roll after the first one. Kingmaker 2.1.7b does not track
   arrows for an ordinary bow; if your setup does show an ammunition count (another mod), it must drop by exactly the
   number of rolls, otherwise ammunition is not compared.
4. Combat with setup S, mounted, at about 10 ft and about 40 ft, real time and turn based, each against: a standing
   enemy, a tripped but conscious enemy (click the enemy's standing footprint and, separately, the lying body), and an
   unconscious enemy. Pass: the rider's own routine (one roll under setup S) each time (the unconscious enemy only when
   clicked after it fell), no horse walking into melee for the shot. Repeat dismounted with the same weapon and
   positions; the mounted and dismounted routines must match. Note any case where the click becomes a move.

5. Negative controls and neighbours (all NOT RUN in this enrollment), with the setup-S rider mounted and a longbow
   equipped, against one hostile enemy; the combat log shows each attack roll with its modifiers (hover the roll):
   - **Wall:** place the pair so a solid wall stands between rider and a standing enemy within bow range (no line of
     sight). Click the enemy. Pass: never a roll through the wall; either an ordinary approach until line of sight
     exists followed by the setup-S routine, or no attack and no action spent. Fail: a roll while the wall still blocks
     the line.
   - **Spent action:** turn based, use the rider's Standard action on a first shot, then click the enemy again. Pass: no
     second roll, a "no Standard action" refusal (KMC's text is "… has no Standard action left this turn"), and the
     horse's own actions unchanged. Fail: a second roll in the same turn.
   - **Beyond firing range:** start farther from the enemy than the longbow reaches and click it. Pass: the ordinary
     approach (the horse moves toward the target with the pair's own movement) and at most the setup-S routine once in
     range; in turn based, if the movement runs out first, no roll and no Standard spent. In real time, once in range,
     count with the setup-S window; if no roll occurs within 12 game seconds of the click, pause and note it. Fail: a
     roll from out of range, or an action spent without a shot.
   - **Mounted melee:** switch the rider to a single-attack melee weapon and click an adjacent standing enemy. Pass: the
     rider's melee routine and action cost as on 0.2.13 with the same rider and positions (the horse attacks only on its
     own opportunity, as before). Fail: no roll, or a routine or action spend 0.2.13 does not show.
   - **Unmounted ranged:** dismount and repeat the near standing shot. Pass: the setup-S routine, as on 0.2.13.
   - **Penalties and expenditure:** compare the hover breakdown of a mounted ranged roll on 0.2.13 and on this build
     at the same distance and movement: the same mounted ranged penalty lines appear (this change does not touch
     them); against a prone target the native prone modifier line appears. The turn-based action spent by the routine
     (Standard, or full round for a full attack) is spent exactly once; ammunition as in setup S. The unconscious enemy
     stays prone, and no extra attack is granted.

Restoration: no native transaction ran.
