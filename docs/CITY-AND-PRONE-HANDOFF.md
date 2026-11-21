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

**Residual (code-level).** A public fact switched off and back on within one frame, with native bad-slot cleanup in
between, is not observed as an absence; every multi-frame absence is.

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

Affected neighbours examined: `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1` and
`chunk7\Test-Chunk7AreaRestorePreflightAdapter.ps1` extract the actual `CaptureArchive` / `RestoreArchivePresentation`
/ `RestoreAreaPair` methods and compile them against typed facades. Run once on this branch, both fail to compile for
reasons that predate it: the facades lack members later features added (F2's `MountedControlSlotPolicy`, already
referenced by `MountedArchiveData`; `MountedAreaControlRule`; retained-intent types and counters; `Report(string,bool)`).
Classified pre-existing (by the missing members, not re-run at main); they are not selected and not edited here.
`Test-Finalize2UpgradeReaders.ps1`'s source locator now matches the stable `CapturePersistentSlots` signature prefix;
that reader remains known-broken (T09, retained r05 evidence absent on this machine) and pins a historical source tree,
so it is not selected.

## Validation (offline, this candidate)

| Command | Result | Limits |
| --- | --- | --- |
| `.\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1` (local run before commit; the controller `focused` gate receipt is bound to the reported candidate) | PASS | Build, components, Validate-Source, focused wrapper; about 1 minute |
| components (`bin\Tests`) | 1429/0 | 9 toolbar, 6 city-roster and 3 downed-target tests |
| Validate-Source | 153/0 | source/payload rules |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` (via wrapper) | 819/0 | reflection-only contracts; Wrath not selected |
| `Test-GunslingerClickComposition.ps1` (via wrapper) | 21/0, both patch orders | substituted native execution, not gameplay |

`Test-CityToolbarProneFocused.ps1` is the repository-owned wrapper (the controller's focused gate takes one script
name) that runs exactly the two checks named in it; nothing broader.

## Acceptance record

Evidence types: UNIT = component test with labeled test doubles of native slots/units, decisions from production
domain code; CONTRACT = installed-assembly
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
| Toolbar persistence | Archive row bound: exact fit kept, 127 + an absent rider's 2 rows refused (validator predicate) | PASS | UNIT |
| Toolbar persistence | Actual capture/restore methods through the archive adapters | NOT RUN | adapters pre-existing broken (see above) |
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
| Negative controls | Blocked LoS, unavailable actions, out-of-range approach, standing targets, unmounted ranged, mounted melee, mounted penalties | NOT RUN | NATIVE (paths unchanged in code) |
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
   it is not overwritten.
3. A mercenary/custom companion with a Horse: in the capital open Inventory (I) and Character (C): the Horse appears
   after its owner; select it, check its name, statistics and barding/neck slots, equip and unequip an item; close and
   reopen; save, load and reopen. No duplicate portraits. Disable Mounted Combat in UMM: the Horse is no longer listed;
   re-enable: it is listed once.
4. Combat, mounted with a longbow, at about 10 ft and about 40 ft, real time and turn based, each against: a standing
   enemy, a tripped but conscious enemy (click the enemy's standing footprint and, separately, the lying body), and an
   unconscious enemy. Expect an attack roll in the log each time (the unconscious one only when clicked after it fell),
   no horse walking into melee. Repeat dismounted with the same weapon and positions. Note any case where the click
   becomes a move.

Restoration: no native transaction ran.
