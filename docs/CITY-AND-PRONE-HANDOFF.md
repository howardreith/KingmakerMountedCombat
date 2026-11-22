# City toolbar, city companion access and prone ranged target: handoff (2026-11-22)

Missions: [original](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md),
[original plus the owner's toolbar repair addendum](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION-WITH-TOOLBAR-REPAIR-ADDENDUM.md),
and [the manual continuation](MOUNTED-COMBAT-CITY-AND-PRONE-MANUAL-CONTINUATION-MISSION.md) (current; it replaces the
execution model and remaining work plan; the earlier acceptance criteria and safety rules still apply).
Branch `codex/mounted-city-and-prone-fixes` from `main` `dbf63702` (0.2.13). Draft PR #57. Development candidate
only: no merge, release, tag, install or native run.

**Current status (manual round 4, 2026-11-22).** Claude, in ordinary interactive development, answered the ten findings of
the replacement enrollment's review round 3. Source candidate `e429d290` (`4cdc34c0` with a test-fixture fix); this
handoff is documentation on top of it. The focused Fast gate passes on `e429d290`. The corrections are **claimed by
Claude** and await the owner's independent (Pro) review: none of them is independently verified yet (section 9). Native
behavior remains NOT RUN for all three original issues (sections 11–12).

**Enrollment history (kept visible as the addendum requires).**
- First enrollment `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c`: 26 review rounds and 27 writer
  invocations. The reviewer effort was `high` for rounds 1–16 and `max` from round 17. The last reviewed candidate was
  `6298f2a5`, decision `changes_required`. Its review records are archived at `1edd82f6` (`docs/agent-reviews/pr-57/`,
  records 001–114). The owner paused it after round 26 and authorized one re-enrollment to change the repair strategy
  and test architecture.
- This enrollment, `kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f`:
  - review round 1 returned `changes_required` (R1-F1…F16) on `83fdf493`;
  - review round 2 returned `changes_required` on `b34616ff`. It verified R1-F1…F9, R1-F12 and R1-F14…F16. It kept
    R1-F10, R1-F11 and R1-F13 open and added R2-F1…F7;
  - writer invocation 3 answered round 2 (section 9);
  - review round 3 returned `changes_required` on `62518858` (ten findings: R1-F10, R1-F13, R2-F1…F5, R3-F1…F3). It
    verified R1-F11, R2-F6 and R2-F7 and **rejected** the round-2 fixes of R1-F10, R1-F13 and R2-F1…F5, so not every
    round-2 finding was accepted as fixed. The controller then reported BLOCKED (R1-F10 and R1-F13 open for three
    consecutive reviews; three of five rounds used);
  - the owner resumed it at 05:31Z; writer invocation 4 ran until 05:33:52Z and stopped with no edit (the worktree stayed
    clean at `62518858`);
  - handover to manual development (this session, under the manual continuation mission): no controller process was
    running and no writer or transaction was active, so no stop request was needed. The pinned controller
    (`C:\Dev\agent-review\versions\34fb8529…`) archived the round requests, reviews, envelopes, dispositions and the
    blocked status as transcript-only commit `6c1a61d7` (records 115–126, after the first enrollment's 001–114, which are
    unchanged). It then cancelled the enrollment at 05:48:35Z with the reason "Owner-directed return to manual development for
    the remainder of PR #57". **Cancellation receipt:** PR comment `status-9-cancelled` (`agent-review:v1 … key=status-9-cancelled`,
    sha256 `50f16543…`) and state transition `develop → cancelled` at `2026-11-22T05:48:35Z`. The registry and worktree
    claims were released (`status` now refuses: not registered; no `agent-review-enrollment.json` in the git directory).
    By design the archive ran before the cancellation, so the cancellation comment itself is on the PR but not in
    `docs/agent-reviews/pr-57/`.

  These counts are in addition to the first enrollment's. No later round, enrollment or automated review was started.

Native contracts below were read from the installed Kingmaker 2.1.7b assembly (MVID `07fa1e4d…`) through the owner's
private local decompilation, used as a read-only reference; nothing was imported. The members this change depends on
are pinned in `scripts/Test-AssemblyContracts.ps1`.

## 1. Mount / Dismount shortcut disappears (toolbar persistence)

**Established cause (unchanged).**
- The public control `KMC_MountStateAbility` is a runtime fact that KMC leases; a toolbar slot binds its `AbilityData`.
- Native `UnitUISettings.GetSlot` and `CleanupBadSlots` empty an ability slot whose fact is removed or inactive (the
  pinned `IsBad` rows), unless a live fact of the same blueprint exists to rebind to.
- Ordinary play makes the fact temporarily absent:
  - area unload with a mounted pair;
  - the capital moving companions to `RemoteCompanions`;
  - a save made in the capital that wrote no KMC row for such a rider.
- Once the slot was empty, the one-time placement record stopped any re-placement.

Which of these paths the owner hit is not established: there is no owner log or save. The city path matches the report.

**Did the two city issues share a cause?** They share a trigger (`CapitalCompanionLogic`'s roster change), not a
defect:
- issue 1 is KMC's slot persistence through a temporary absence of the fact;
- issue 2 is native `GetGroup`'s in-game pet filter (section 2).

### The repair: one shared production lifecycle component (addendum C)

**Files and roles.**
- **`Domain/MountedToolbarLifecycle.cs`** (`MountedToolbarLifecycle<TUnit>`) is the single production owner of every
  toolbar decision, with its sequencing and actor selection, at every boundary:
  - update and observation of player edits;
  - native slot invalidation;
  - save header and save scope;
  - area suspension and resume;
  - selected load begin, abandon and completion;
  - unfenced disposal and the main menu;
  - companion replacement and retirement;
  - disable.
- **`Domain/MountedControlPlacementIntent.cs`** holds its per-world state and the evidence rule (`ApplyBar`).
- **`IMountedToolbarHost<TUnit>`** is the narrow adapter interface.
- **`Integration/NativeMountedControlService.Toolbar.cs`** implements it for Kingmaker and carries the service's
  toolbar entry points.
- **Tests** (`tests/MountedControlPlacementIntentTests.cs`) drive the real component through thin fakes. The fakes'
  slot classification is the production `MountedControlSlotPolicy.ResolveExactKind`, including a non-ability item slot
  that forwards a null blueprint.
- **Wiring fixture** (`tests/Chunk7Diagnostics/ToolbarAdapterWiringFixture.cs`) compiles the actual adapter file
  against labeled Kingmaker facades (section 8).

**The model.** A rider's arrangement is three things:
- one **chosen slot**, or "off the bar";
- whether that slot is **pending** (emptied by native cleanup or by a KMC sweep, so owed back);
- the **known copies**: every other public binding seen on the bar, identified by index and slot object. They are held
  weakly, so a slot object that left every bar is never kept alive; a sweep that clears a binding drops its copy entry
  at once.

One rule applies at every boundary: observe the bar first, then mark what the boundary empties as pending. `ApplyBar`
reads the bar as follows:
- The first public binding that is not a known copy (canonical bindings first, then aliases, by index) is the player's:
  it becomes the chosen slot, live. This covers a move, a fresh placement and a re-placement at the pending slot.
- With no such binding:
  - a live arrangement was removed by the player, and its copies yield;
  - a pending one keeps waiting while its slot is free;
  - if the player filled the pending slot, the first surviving copy (an older alias included) becomes the choice, or
    the shortcut is off the bar.
- Every other public binding is a known copy afterwards.

A live control keeps exactly one shortcut. A pending slot returns once into its empty slot. An unavailable bar defers
that return; only an available bar that cannot hold the index gives it up.

**Native operations (revised in manual round 4).**
- **The bar's owner** comes from the verified native relationship, never from a slot's contents: GetSlot's own `unit`
  argument when that unit's bar is this one, else `UnitUISettings.Owner.Unit` (the descriptor CleanupBadSlots binds its
  writes to). A stale slot naming a discarded rider object cannot hide the current bar or end a whole-bar pass (R3-F2).
- **Per invocation.** Each prefix returns its own operation, carried to its own postfix in Harmony 1.2's per-invocation
  `__state` (the pattern is already used by other KMC patches on the installed Harmony12; Harmony 2 finalizers do not
  exist there). A postfix completes only the operation its prefix opened; a prefix that opened nothing completes nothing
  (R2-F4).
- **Threw.** A prefix whose postfix never ran threw: the two methods never nest (installed contract), so the next entry
  settles it. While a selected load is open and no player world exists, thrown operations are held in their own list,
  which a later operation cannot consume. They are settled when their own world is current again, and dropped on world
  replacement, an unfenced disposal (main menu) and disable.
- **The bar as native started.** The prefix notes every public binding (slot object, and what CleanupBadSlots would
  rebind into it: the active fact's data) and every other occupied slot. After the postfix, or at settlement, the bar is
  observed **as it stood then**, occupancy included, and the written slots are applied in index order:
  - an emptied chosen slot becomes pending; a rebound chosen slot is live;
  - a rebound copy stays a copy under its new slot object;
  - an emptied copy is gone, unless it was the last surviving copy of a pending choice whose slot was occupied, in
    which case it becomes that choice.
- **What a thrown operation can have written** (method-specific evidence, R2-F5). GetSlot only ever writes an empty slot
  bound to the unit; it constructs no ability slot (installed contract). CleanupBadSlots writes that, or a new binding of
  exactly the fact data noted at the prefix. Anything else in a noted slot is a later edit, read at the next ordinary
  observation. So a fact deactivated after a genuine rebind does not make the rebind a placement, and a player's
  placement after GetSlot threw is never credited to native. A supported exception-safe boundary does not exist on
  Harmony 1.2: a prefix that re-ran the original would run other mods' GetSlot patches twice.

**Authority and identity.**
- Placement authority goes by exact roster identity. An object on the current player roster authors its actor's
  arrangement. An object off the roster authors only while no other object of the same character is on the roster as
  a current rider or reserve or detached owner. A former instance kept by pair custody is therefore only cleaned up.
- Companion identity is checked before any observation and independently of the bar. A pet link naming another
  companion, or cleared while the rider no longer gets the control, retires the arrangement. The companion an
  arrangement belongs to is recorded as soon as it is known, whether or not the bar is readable, and a fresh load's
  arrangement takes the loaded rider's link (R2-F3).
- A retirement made while the bar cannot be read clears that bar's surviving public bindings the first time it is
  readable again: the player cannot edit an unreadable bar, so they predate the retirement. A later placement for the new
  companion is then an ordinary fresh placement (R2-F3).
- A unit positively identified as a pet owns no arrangement or marker. Rows admitted before it resolved are purged when
  it appears as a pet.
- **Capture authority (R1-F10, R3-F1).** Every capture first prepares: each placement unit's bar is observed now, so rows
  describe the bars as they stand. Only the current instance of each character authors rows, in the owned save and in
  the in-world captures (area transfer, retained remount), for the public rows and the other managed rows alike. The
  lifecycle names the authoring units to the adapter before either category is produced. A pet authors unless another
  object of the same actor is the current persistent pet of an authoring owner. The host's public-binding predicate
  accepts only the public control or a supported Mount / Dismount alias, never a callable Mounted Charge.

**Archive rows.** On a fresh load, every row that restores as the public control is considered in archive order: kind
6 and the older Mount / Dismount kinds 1 and 2. The first row whose slot can hold the control is the choice; a replay
takes the first row.
- **Unreadable at seeding (R2-F1).** While the actor or its bar is unreadable, the choice is deferred: all public rows
  are kept and the choice is made at the first observation of a readable bar. A save in the meantime carries them as they
  were read (their original kinds, each a valid distinct row).
- **Observed before seeding.** A fresh load's rows never overwrite what the same incoming world already observed for
  that actor (a native read of its bar before the restore). The one exception follows the same principle: an observed
  choice that was only an older Mount / Dismount alias (an F2 leftover, which the player cannot place) yields to the
  archive's record of the actor. That record is its public row, or a marker without one (off the bar). The alias becomes
  what seeding first makes of it: a known copy, or the fallback its native emptying earned when the archive's slot is
  occupied. A canonical binding read before the restore still wins (genuine native recovery) (R2-F2).
- Seeding first and observing second give the same result.

| Boundary | What the lifecycle does, in order |
| --- | --- |
| Update | Settle an aborted native operation (held while a load is open and no player world exists); follow the player world. In that open-load, no-world interval, stop: nothing is observed, cleared or released. Then for each rider: a pet is purged of rider state; a non-authoring former instance has its public bindings cleaned; otherwise observe, the host leases facts, repair, return the pending slot, and run the one automatic placement. Then departures (observed, classified, KMC bindings, auto-use and facts cleared), the reserve sweep (observed, cleared with the chosen slot pending, persistent pets swept and purged of rider state), and revalidation of absent actors. |
| Native `GetSlot(i, unit)` / `CleanupBadSlots` | Owner from the unit argument or `UnitUISettings.Owner`; the bar as it stood noted at the prefix; this invocation's operation returned through `__state`; outcome applied at its own postfix, or at settlement from that method's possible writes, as above. Only in the world that produced the operation, for a unit on its roster. The lifecycle alone holds an operation's unit outside its prefix-to-postfix span. |
| Save header | Prepare (observe every authoring placement unit); retire departed former riders; revalidate. Each arrangement whose actor resolves as a player character that is not a pet writes one kind-6 row (chosen slot, live or pending, 0–127), or, while a fresh load's choice still awaits a readable bar, its rows as read; and one kind-7 marker. Other managed rows only from authoring units. |
| In-world capture (area transfer, retained remount) | Prepare, then for each authoring current rider or reserve owner (not a pet) one kind-6 row when its chosen slot holds the callable live public control, and its marker; one row and one marker per actor. Other managed rows only from authoring units. |
| Save scope | Observe, then every sanitation unit loses its public bindings (chosen slot pending, copies released). Its other KMC bindings go too: an older Mounted Charge becomes the stock Charge first, and the rest are leased back. It also loses its KMC auto-use and its facts. The scope's end is an update, then the leases. |
| Area unload / resume | As the save scope, with the other bindings cleared. Resume is an update. |
| Selected load | Nothing is moved or cleared. The outgoing world keeps its records, observed units, bars and any thrown native operation through a missing-player interval. A distinct world releases them; the incoming world seeds from its rows without overwriting its own earlier observations. |
| Main menu / unfenced disposal | Records and any open native operation are released, and earlier-world observed units are cleaned up at the next update. |
| Disable / dispose | Every sanitation unit loses every exact KMC binding, auto-use and fact; nothing is remembered. |

**Placement versus sanitation (addendum C3).**
- Placement eligibility covers current riders on the roster and authoring former riders of this world (both not pets),
  and reserve or detached owners.
- Sanitation coverage adds:
  - every observed former rider, including non-authoring and earlier-world objects;
  - riders that rejoined before the next update;
  - the exact persistent pet of every swept unit, including the pet of a pair-custody reserve owner.
- Sanitation touches only exact KMC slots, auto-use selections and facts.

**Save format: unchanged.** Kinds 6 and 7, the 128-row bound and the archive's validation are kept, with one kind-6 row
per actor.

### Decisions that differ from the first enrollment's candidate (stated explicitly)

1. **No load fence.** Records are kept per world and are not moved at load begin. While a load is open and no player
   world exists, nothing happens, including settlement of a thrown native write.
2. **The `IsBad` postfix is removed.** `GetSlot` and `CleanupBadSlots` are bracketed and compared by slot object; the
   `IsBad` contract rows stay pinned.
3. **Reserve and detached bars are observed and swept every update.**
4. **The save scope does not lease the public control;** its chosen slot returns through the update at the end of the
   scope.

Retracted in writer invocation 2: set semantics, under which all of the player's duplicate bindings were kept (review
R1-F1).

**Remaining uncertainty (§4).**
- Respec's whole-bar reset (`UnitUISettings.CleanupSlots`) is not tracked.
- A native slot replacement outside `GetSlot` and `CleanupBadSlots` would read as a removal; none is known in 2.1.7b.
- The player dragging the control back onto a copy's own slot under the same slot object is still a copy.
- A thrown CleanupBadSlots whose rebind slot the player re-places with the same fact data within the same frame, before
  settlement, is indistinguishable from native's rebind and is read as native's (it settles at the next entry, normally the
  same frame; the player cannot edit an unreadable bar or edit during a load).
- A seeded arrangement records the loaded rider's pet link; an in-world replay that seeds an actor with no arrangement
  records the current link (before this change, no link; either way a replay never recreates a retired actor).
- Behavior is component-tested through the production component, and the adapter wiring through its fixture. The
  fixture still mirrors the main service file's `Update`, `SetEnabled`/`Dispose` and the Harmony forwarders; the changed
  forwarders' parameter binding is checked by reflection on the built product against the installed native names
  (CONTRACT). Nothing is observed in game; the native cases are NOT RUN (section 11).

## 2. Animal companions in the city's inventory and character screens

Unchanged in this enrollment (addendum B).
- **Cause.** In a capital, the full-screen Inventory, Character and Spellbook screens use `GroupController` with
  `WithRemote` and `WithPet` (`UIUtility.GetGroup(true, true)`, token `06004281`). It adds pets only from
  `Player.ControllableCharacters`, which requires `IsInGame`, and `CapitalCompanionLogic` takes custom companions and
  their pets out of the game.
- **Fix.** An exact-token postfix projects each listed non-pet owner's own existing persistent pet right after it,
  under native's pet rules. It applies only while Mounted Combat is enabled and only on that capital roster. Nothing
  changes party membership, ownership, `IsInGame`, scenes, formation, XP or combat.

## 3. Mounted attacks against prone and downed targets

Unchanged in this enrollment (addendum B) and in manual round 4. Status (the manual continuation mission's wording):

The identified rejection of an explicit mounted attack on a living target that was already unconscious is corrected in
source. The original prone-target report remains unreproduced; whether the reported target was conscious and prone or
already unconscious remains unknown.

**Unconscious (downed) enemy.** Native `UnitAttack` keeps an explicit order on an enemy that was already unconscious
when ordered. `UnifiedMountedStockAttackPolicy.TargetLifeAdmits` mirrors that rule for the mounted pair path. The
allowance is recorded for a player order only (`MountedAttackIntent.TargetUnconsciousWhenOrdered`) and kept while that
target lives (`MountedPairAttackCommand.AdmitTargetUnconsciousWhenOrdered`).

**Conscious, tripped enemy.** A code trace found no mounted divergence: classification, admission, approach, range,
line of sight and firing have no prone term. Clicking the lying body can miss its colliders, which is native and
target-side. This is a trace, not an observation.

## 4. Lifecycle properties through the production path (addendum D)

| Property | Behavioral tests (production component through thin fakes) |
| --- | --- |
| D1 intent before cleanup | `IntentBeforeCleanup`; `NativeInvalidationVersusRemoval`; `LegacyAliases`; `ThrownOperationEvidence` (c–e: the bar as native started) |
| D2 same edits, either order | `SameEditsEitherOrder`; `LegacyChargeEitherOrder`; `FormerInstanceKeepsReplacement`; `FormerInstanceInWorldCapture`; `IncomingObservationBeforeRestore` and `LoneAliasBeforeOrAfterSeeding` (native read before and after seeding); `ArchivedR17F2AbortedThenSuccessfulRead` (save first and update first) |
| D3 no move or resurrection | `RepeatedBoundariesIdempotent`; `OccupiedAndIndependent`; `KnownCopies`; `DelayedBarKeepsOlderRows` (repeated updates); `HiddenBarCompanionRetirement` |
| D4 canceled load keeps the outgoing state | `LoadLifetime`; `MissingPlayerDuringCanceledLoad`; `FencedThrownNativeWrite`; `HeldThrownWriteSurvivesCompleteRead`; `ArchivedR17F1AbandonedLoadFallback`; `AcR26F1…`; `AcR26F2…` (fenced); wiring R2-F4 check |
| D5 replacement and disposal release | `AcR26F2…`; `MissingPlayerDuringCanceledLoad`; `FencedThrownNativeWrite` (replacement and main-menu controls); `HeldThrownWriteSurvivesCompleteRead` (disable control); `ReserveCopiesReleased` (weak copies); `DisableAndLifetime`; wiring check for the thrown operation |
| D6 invalidation versus removal | `NativeInvalidationVersusRemoval`; `NativeOperationEdges`; `NativeRebindOfPendingSlot`; `ThrownCleanupEdges`; `ThrownOperationEvidence`; `ArchivedR17F2…`; `ArchivedR19F3…`; `KnownCopies`; wiring R3-F2 check |

## 5. Inherited acceptance cases (addendum F)

Provenance for all six: enrollment `…-e55c`, round 26, reviewed commit `6298f2a5`, archived review
`docs/agent-reviews/pr-57/112-r26-review.md` (envelope `113`). These are acceptance-case references, not findings of
this enrollment. Evidence type: UNIT (the production lifecycle through thin fakes). All six pass on source candidate
`e429d290` (focused Fast gate of manual round 4, section 10; unchanged test bodies). None is observed in game.

| Case | Outcome | Test | What it exercises |
| --- | --- | --- | --- |
| AC-R26-F4 | PASS | `AcR26F4ReservePetSanitized`, `PairCustodyOwnerPetSanitized` | The remote owner and its separately held horse: a factless Mount Primary slot and auto-use are cleared inside serialization and by disable. The ordinary ability and auto-use survive, also with pair custody. |
| AC-R22-F3 | PASS | `AcR22F3FreshPlacementAfterNativeEmpty` | A never-observed reserve rider's only slot 3 is emptied by `GetSlot`. The current live control is placed at 11 (asserted). Save before any scan: 11 and one marker; one shortcut at 11 on return and after a cold restore. Untouched: 3. With a known copy: only 3, or only 11. |
| AC-R25-F3 | PASS | `AcR25F3FreshShortcutAfterReplacement` | A's 9 is retired; B's placement at 11 is saved and restored once; with no placement nothing returns. |
| AC-R26-F1 | PASS | `AcR26F1CanceledLoadKeepsEdit` | After the sweep, the live ability is placed at 11 (asserted); a load is begun and abandoned before observation. The save holds 11 and one marker, restored once. No edit: 9. |
| AC-R26-F2 | PASS | `AcR26F2MainMenuReleases` | After the main menu: no records, world, observed unit, native operation or load. Fenced control: 3 only, one shortcut on return. Real transition: empty records. |
| AC-R26-F3 | PASS | `AcR26F3RejoinedRiderAutoUse` | A stale Rider Primary selection, asserted present, on a rider that rejoined before a save, a disable or a disabled save (installed after disabling). It is null inside serialization and afterwards; an ordinary selection survives. |

## 6. Acceptance record (mission §7)

Evidence types:
- UNIT: production code driven through thin labeled fakes.
- WIRING: the actual adapter file through its offline fixture.
- ARCHIVE: the production archive DTO, validator and codec.
- ADAPTER: actual methods against typed facades.
- CONTRACT: installed-assembly reflection.
- COMPOSITION: real transpilers with substituted engine boundaries.
- TRACE: source reading only, with no execution of that code path.
- NATIVE: in-game structured evidence.

Source identity: source candidate `e429d290` (product DLL of its Release build sha256 `51e70773…161df`, built by the
gate; no package was built). Results below are from the focused gate on that commit unless a row names another commit.

| Area | Case | Result | Evidence |
| --- | --- | --- | --- |
| Toolbar transitions | Chosen nondefault slot; repeated city entry and exit; mounted and dismounted arrival; interior transition | PASS | UNIT `SurvivesTransitions` |
| Toolbar transitions | The same in the real capital; forced dismount and the same pet's recovery | NOT RUN | NATIVE (playtest 1) |
| Toolbar persistence | Save and load inside and outside the city | PASS | UNIT `SaveLoadInsideAndOutsideCity` |
| Toolbar persistence | Temporary mount absence (same pet unresolved, fact inactive, faulted rider, bar unavailable) | PASS | UNIT `CompanionLoss`, `HeaderDuringInterruption`, `UnavailableBarDefersRestore`, `CompanionReplacedWhileBarHidden`, `HiddenBarCompanionRetirement` (same pet: 9 once) |
| Toolbar persistence | Moves and removals in both orders; known copies; one shortcut | PASS | UNIT `SameEditsEitherOrder`, `KnownCopies`, `MoveSupersedesPending`, `RemovalAfterSaveRelease` |
| Toolbar persistence | Occupied slots, another rider, unrelated slots | PASS | UNIT `OccupiedAndIndependent` |
| Toolbar persistence | Companion removal, roster loss, disable, world disposal; former instances and late-resolving pets | PASS | UNIT `CompanionLoss`, `DisableAndLifetime`, `FormerInstanceKeepsReplacement`, `FormerInstanceInWorldCapture`, `LateResolvingPetPurged`, `HiddenBarCompanionRetirement` |
| Toolbar persistence | Older archives read while the bar is unreadable; leftover aliases read before or after seeding | PASS | UNIT `DelayedBarKeepsOlderRows`, `LoneAliasBeforeOrAfterSeeding`, `MixedArchiveRows`, `IncomingObservationBeforeRestore` |
| Toolbar persistence | Native operations: own postfix, held thrown writes, method-specific settlement, occupancy as native started | PASS | UNIT `HeldThrownWriteSurvivesCompleteRead`, `ThrownOperationEvidence`, `FencedThrownNativeWrite`, `ThrownCleanupEdges`, `NativeOperationEdges` |
| Toolbar persistence | Adapter forwarding, roster inputs, capture authority, Charge versus the public predicate, held write through the service brackets, native bar owner | PASS | WIRING (9 checks; 9 wiring mutations killed) |
| Toolbar persistence | Archive bound and row shapes | PASS | ARCHIVE (7/0) |
| Toolbar persistence | Actual archive capture and presentation restore | PASS | ADAPTER (six-pair, 22/0) |
| Toolbar persistence | Native bad-slot replacement, rebind shape, native empty slot bound to the unit, drag-off writes null; `UnitUISettings.Owner`; no nesting; GetSlot only empties; GetSlot parameter names | PASS | CONTRACT (installed assembly) |
| Toolbar persistence | The product's GetSlot / CleanupBadSlots prefixes and postfix bind `__instance`, `index`, `unit` and `__state` | PASS | CONTRACT (reflection on the built product) |
| Toolbar persistence | Area-restore adapter | not executed | `Test-CityAreaAdapterBaseline.ps1` 63/63 compares compiler diagnostics only: base and candidate both fail to compile its facade with the same 63 errors, and none of the area-restoration assertions run |
| Toolbar persistence | The same in game | NOT RUN | NATIVE (playtests 1–2) |
| City companion UI | Owner and pet mapping, exact instance, multiple pets, exclusions, repeated opening, disabled | PASS | UNIT `CityCompanionRosterProjectionTests` (unchanged) |
| City companion UI | `GroupController` uses `GetGroup`; pets from `ControllableCharacters`; exact token | PASS | CONTRACT |
| City companion UI | Selecting the reserve pet; an identifiable item equipped persisting through reopen and reload on that same pet | NOT RUN | NATIVE (playtest 3) |
| Prone ranged attack | Explicit order on a downed enemy valid while it lives; one that falls later abandoned; dead cancelled; only player orders carry the allowance | PASS | UNIT `MountedDownedTargetOrderTests` (`OnlyPlayerOrders`, `ExplicitOrderAdmitsDowned`): the executed target predicates `TargetLifeAdmits` / `IsValidTarget` |
| Prone ranged attack | Conscious prone enemy: click, admission, approach, range and line of sight, firing | PASS (no divergence found) | TRACE |
| Prone ranged attack | Matched standing, conscious-prone and already-unconscious enemies, mounted and dismounted, RT and TB, near and normal distance, with an observed roll | NOT RUN | NATIVE (playtest 4) |
| Negative controls | Charge keeps its conscious-only rule at admission and revalidation (`TargetValid` requires `IsConscious && !IsFinallyDead`: `MountedCombatController.Charge.cs`; the `MountedPairAttackCommand` charge revalidation). Continuation candidates are filtered to conscious, not finally dead enemies. The automatic-gate target check stays conscious-only. | PASS | TRACE. The component tests feed these paths preclassified inputs; no executed test passes an unconscious unit through these integration filters. |
| Negative controls | Blocked line of sight, unavailable actions, out-of-range approach, standing targets, unmounted ranged, mounted melee, mounted penalties | NOT RUN | NATIVE (paths unchanged in code; playtest 5) |
| Shared regressions | Tabletop Rules Expansion click composition, both orders | PASS | COMPOSITION |
| Shared regressions | Existing component suite | PASS | UNIT (1472/0 on `e429d290`) |
| Shared regressions | U02 presentation probe; F2 reader source guards | PASS on `56366c41` only | detached probe 239/0 and F2 guards ran on `56366c41` (first enrollment); not rerun on any later commit, so not evidence for `e429d290` |

## 7. R1–R26 regression map (addendum E)

Every finding of the first enrollment (round 6 raised none), with the covering test or subcase in
`tests/MountedControlPlacementIntentTests.cs` unless another file is named. "r*N*" marks a later round's re-raise.
*Gone* marks a finding whose mechanism no longer exists (copy registry, scan ledger, load fence); the test asserts the
protected behavior.

Manual round 4 (replacement R1-F13) restores the three sequences review round 3 found missing, each as one complete test
in the archived event order (bold below; the archived requests are in `docs/agent-reviews/pr-57/081-r19-review.md`). All
three also pass on the evaluated candidate `6251885` (checked by compiling them against its production sources): they were
missing coverage, not live defects. Every other row is unchanged.

| Finding | Concern | Covered by |
| --- | --- | --- |
| R1-F1 | removal through a reserve save and load | `RemovalSurvivesReserveSaveLoad` |
| R1-F2 | edit between update and save scope | `SameEditsEitherOrder` (save first) |
| R1-F3 (r2, r3) | native emptying before re-lease; fact back first; move then cleanup | `NativeInvalidationVersusRemoval` |
| R1-F4 | replay after a removal | `InWorldReplay` |
| R1-F5 | permanent pet loss in reserve | `CompanionLoss` |
| R1-F6 | city roster after disable | `CityCompanionRosterProjectionTests` |
| R1-F7 | archive bound | `Test-CityArchiveRowBound.ps1` |
| R1-F8 | native fixture description | section 11 |
| R1-F9 | conscious prone traced separately | section 3; playtest 4 |
| R2-F1 (r3, r4) | header during an interruption | `HeaderDuringInterruption` |
| R2-F2 (r3) | edit then departure; header before departure cleanup | `IntentBeforeCleanup` |
| R2-F3 | same pet link unresolved | `CompanionLoss` |
| R2-F4 (r11) | removal right after the save scope | `RemovalAfterSaveRelease` |
| R2-F5 | adapter facades and baseline | `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1`; `Test-CityAreaAdapterBaseline.ps1` |
| R3-F1 (r4) | outgoing world writing into the loaded world | `LoadLifetime`; `NativeOperationEdges` — *gone* (fence) |
| R3-F2 | prone playtest negative controls | playtest 5 |
| R4-F1 (r5) | removal then ineligibility or deactivation | `NativeInvalidationVersusRemoval` |
| R4-F2 (r5–r7) | permanent loss and pet changes | `CompanionLoss`; `InWorldReplay`; `CompanionReplacedWhileBarHidden` |
| R4-F3 | area suspension before the departure loop | `IntentBeforeCleanup` |
| R4-F4 | validation claims | section 10 |
| R5-F1 (r6) | abandoned load | `LoadLifetime`; `FencedThrownNativeWrite` — *gone* (fence) |
| R5-F2 | superseded scope | `LoadLifetime` |
| R7-F1 | older capital save's reserve binding | `ReserveBars` |
| R8-F1 | canonical over alias; Mounted Charge to stock Charge, first update or save | `ReserveBars`; `LegacyChargeEitherOrder`; wiring check |
| R8-F2 | faulted rider | `CompanionLoss` |
| R9-F1 | disable (and save) before or after partial registration | `UnregisteredBlueprints`: an item slot (null blueprint through the production classifier) and an ordinary ability keep their exact objects inside serialization and after disable; the public control is cleaned only once registered |
| R9-F2 (r10) | companion replaced during the scope | `CompanionChangeDuringSave` |
| R9-F3 | scan bookkeeping lifetime | `DisableAndLifetime`; `ReserveCopiesReleased` — *gone* (ledger) |
| R9-F4 (r10) | alias while the pet is unresolved | `LegacyAliases` |
| R10-F1 (r11) | header before repair, live alias | `LegacyAliases`; archive check |
| R11-F1 | alias beside canonical, saved first | `LegacyAliases`; archive check |
| R12-F1 | stale and current canonical at save | `KnownCopies` |
| R12-F2 | alias the scope returned | `LegacyAliases`; `RemovalAfterSaveRelease` |
| R12-F3 | pet change clears canonical and alias | `CompanionLoss` |
| R12-F4 (r13) | native rebind then removal | `NativeInvalidationVersusRemoval`; `NativeRebindOfPendingSlot` |
| R13-F1 | returned alias emptied | `LegacyAliases` |
| R13-F2 | no autofill copy after a manual placement | `InitialPlacement` |
| R13-F3 | slot occupied by another KMC binding | `HeaderDuringInterruption`; archive check |
| R13-F4 | aborted operation across a replacement | `NativeOperationEdges`; `FencedThrownNativeWrite` (replacement control) |
| R14-F1 | manual placement emptied then filled | `NativeOperationEdges` |
| R14-F2 (r15–r17) | canonical 4 and alias 9 | `LegacyAliases` |
| R14-F3 | occupied at save, occupant cleared | `HeaderDuringInterruption` |
| R14-F4 | disabled, another world loaded | `DisableAndLifetime` |
| R14-F5 | rebind of an interrupted slot during an open load, then removal | `NativeRebindOfPendingSlot` |
| R15-F1 (r16–r19) | copies emptied, saved, re-placed | `KnownCopies` |
| R15-F2 | disabled save | `DisableAndLifetime` |
| R16-F1 | automatic placement then a pet change | `CompanionLoss` |
| R17-F1 (r18, r19) | filled chosen slot with a surviving copy or alias; r19: the same under an abandoned selected load | `KnownCopies` (base, r18: the fallback without a load); **r19 `ArchivedR17F1AbandonedLoadFallback`**: chosen 4 interrupted with alias 9 a known copy, Fireball at 4, selected load begun, native empties 9, abandoned → asserts Fireball at 4 and exactly one shortcut at 9 in the continuing world, in the save (row 9, one marker) and after the cold restore |
| R17-F2 (r18–r20) | validity query and throws; an aborted call immediately followed by a successful one | `NativeOperationEdges` (read-only query then removal stays removed; r18 throw-before-write then removal; r19 write-then-throw recovers; r20 cleanup write-then-throw saved); `ThrownCleanupEdges`; **`ArchivedR17F2AbortedThenSuccessfulRead`**: an aborted GetSlot(9) or CleanupBadSlots (no write, no postfix) immediately followed by a successful GetSlot(9) before any update → asserts recovery at 9 once, update first and save first (row 9, one marker); then a deliberate removal stays removed (record only); control: read-only query then removal stays removed |
| R17-F3 (r18) | retirement during an open load: native rebind (base) or two-slot cleanup without a replacement fact (r18), then abandonment | `CompanionLoss` R17-F3 subcases (retired, binding cleared, record-only save, nothing returns for B) |
| R18-F1 (r19–r24) | emptied chosen slot with copies; rebinds; removals | `KnownCopies` (r24: the pet-absent update, then `GetSlot(9)`) |
| R18-F2 | bad alias not credited to a removed alias | `LegacyAliases` |
| R18-F3 | combat harness liveness | `Test-CityTargetLiveness.ps1` |
| R19-F1 | migration before native reads the alias; alias fallback | `LegacyAliases` |
| R19-F2 (r20–r22) | move past a copy | `MoveSupersedesPending` |
| R19-F3 | newer placement invalidated (after an interruption; after a companion retirement) | `NativeOperationEdges` R19-F3 (interrupted 3, placement at 9 invalidated before the update → 9, not 3); **`ArchivedR19F3ReplacementPlacementInvalidated`**: A's 3 retired by the change to pet B, B's fresh live placement at 9, native invalidates it before its first ordinary observation → asserts the save holds 9 and one marker, the continuing world and the cold restore have exactly 9 (never 3) and the actor is no longer retired; stale-batch rejection control `CompanionLoss` R17-F3 |
| R20-F1 (r21, r22) | copy lifetime | `ReserveCopiesReleased`; `AcR26F2…` |
| R21-F1 | update-first stale and current | `KnownCopies` |
| R21-F2 | disabled save then another world | `DisableAndLifetime` |
| R21-F3 | evidence labels | section 6 (case-level evidence types; executed predicates versus traced filters) |
| R22-F1 | filled 3, copies 7 and 9 | `KnownCopies` |
| R22-F2 | older rows, 3 occupied | `KnownCopies`; `MixedArchiveRows` (kind 6 and kind 2) |
| R22-F3 (r23–r26) | first reserve scan after native emptying | `ReserveBars`: occupied → 9; native rebind of 9, then 9 / move 11 / removal; 3 free with rebind and move → 11 or no move → 3; save, return and cold restore. Also `AcR22F3…` (r26). |
| R23-F1 | callable copy survives a stale-copy emptying | `KnownCopies` |
| R23-F2 | main menu then a new game | `DisableAndLifetime` |
| R23-F3 (r24, r25) | moved callable alias | `MoveSupersedesPending` |
| R24-F1 | temporary facts on a reserve rider | `ReserveBars` |
| R24-F2 | the incoming world's first native report | `LoadLifetime`; `IncomingObservationBeforeRestore` (actual rows, both orders) |
| R24-F3 (r25) | missing player during a load | `LoadLifetime`; `MissingPlayerDuringCanceledLoad`; `FencedThrownNativeWrite` |
| R25-F1 | never-scanned reserve rider through another pair's area transfer | `InWorldReplay` |
| R25-F2 | reserve managed auto-use | `DisableAndLifetime` |
| R25-F3 (r26) | fresh placement on a scanned reserve bar | `ReserveBars`; `AcR25F3…` |
| R26-F1 … F4 | the inherited acceptance cases | section 5 |

## 8. Production mutations (addendum E)

`scripts/Test-ToolbarLifecycleMutations.ps1`, the seventh child of the focused wrapper, runs two campaigns in fresh
temp directories:
- **lifecycle:** the component and its tests;
- **wiring:** the actual adapter file with the component and the wiring fixture. The fixture's Kingmaker types and the
  service's main-file members are labeled substitutes.

Each unmutated copy must pass. Each one-line production mutation must make its named checks fail. The script confirms
afterwards that the worktree is byte-identical.

Results on `e429d290` (manual round 4, focused gate; section 10): baselines lifecycle 52/0 and wiring 9/0; all 31
mutations killed. (Earlier, on `b868cd41`: 43/0, 5/0, 18 of 18.) Manual round 4 adapted the needles of M1b, M3e, M4a and
W3 to the corrected code without changing what they break. It added M5–M8 and W6–W9, one per repaired boundary. Two
link-recording points for R2-F3 (at seeding, and at a hidden-bar observation) each suffice for the tested sequence, so no
single-line mutation isolates either; M8a covers the unreadable-bar sanitation.

| Mutation | Class | Failing checks |
| --- | --- | --- |
| M1a no observation before a sweep | observation | 1 |
| M1b no observation before a native write is applied | observation | 16 |
| M2a pets omitted from sanitation | actor | 2 |
| M2b reserve owners omitted from observation | actor | 9 |
| M2c a pair-custody former instance authors | actor / authority | 2 (review R1-F10) |
| M3a abandonment releases the outgoing records | world lifetime | 8 |
| M3b a replaced world keeps the outgoing records | world lifetime | 26 |
| M3c no release without a world | world lifetime | 3 |
| M3d a missing player in an open load treated as disposal | world lifetime | 1 (review R1-F2) |
| M3e a thrown native write dropped at a missing player in an open load | world lifetime | 2 (review R2-F4) |
| M4a a player's null write counted as native | edit versus invalidation | 1 |
| M4b a native emptying leaves nothing pending | edit versus invalidation | 21 |
| M4c a removal kept as owed back | edit versus invalidation | 15 |
| M5a an in-world capture takes rows from a non-authoring former instance | capture authority | 1 (R1-F10) |
| M5b a capture writes rows from cached arrangements without preparing | capture preparation | 18 (R1-F10, R3-F1) |
| M6a a postfix that opened nothing consumes the held thrown write | native invocation | 1 (R2-F4) |
| M6b a thrown GetSlot credited with a rebind it cannot write | native evidence | 1 (R2-F5) |
| M6c a thrown cleanup's rebind judged by the fact's later state | native evidence | 1 (R2-F5) |
| M6d a thrown operation read against occupancy at settlement | native evidence | 1 (R2-F5) |
| M7a a fresh load chooses among its rows while the bar is unreadable | archive selection | 1 (R2-F1) |
| M7b a leftover alias read before seeding overrides the archive | observation provenance | 1 (R2-F2) |
| M8a a bar unreadable at retirement is not sanitized when it returns | retirement | 1 (R2-F3) |
| W1 save scope not forwarded | wiring | 2 |
| W2 detached roster omitted | wiring | 1 |
| W3 native prefix not forwarded | wiring | 4 |
| W4 no persistent pet | wiring | 1 |
| W5 no reserve or detached owners | wiring | 2 |
| W6 other managed rows from every candidate, not the authoring units | wiring / authority | 1 (R1-F10) |
| W7 the host's public-binding predicate accepts any callable KMC command | wiring / predicate | 1 (R3-F1) |
| W8 the bar owner derived from a slot's contents | wiring / owner | 1 (R3-F2) |
| W9 GetSlot's unit argument ignored | wiring / owner | 1 (R3-F2) |

Limit: the main service file's `SetEnabled`, `Update` and `Dispose`, and the one-line Harmony forwarders, are mirrored
in the fixture, not compiled. For the forwarders changed in manual round 4, `Test-AssemblyContracts.ps1` reads the built
product and checks each prefix's and postfix's parameter names and types against the installed GetSlot / CleanupBadSlots
signatures (Harmony 1.2 binds by name). It does not execute them.

## 9. Review dispositions (this enrollment)

Round 1: all 16 findings fixed in writer invocation 2. Round 2 verified 13 of them.

Round 2 (writer invocation 3; commits `3f19c91a`, `b868cd41` and the handoff commit). Dated history: "fixed" below is what
writer invocation 3 claimed. Review round 3 verified R1-F11, R2-F6 and R2-F7 and rejected R1-F10, R1-F13 and R2-F1…F5;
those are answered in the round-3 table after this one.

| Finding | Disposition | Change and evidence |
| --- | --- | --- |
| R1-F10 retained former instance | fixed | Placement authority by exact roster identity (`Authors`), in reconciliation, capture and departures. `FormerInstanceKeepsReplacement` covers pair custody with a party replacement saved at once and a reserve replacement after two updates: only 11 and one marker, also after a cold restore. M2c is killed. |
| R1-F11 pet admitted before it resolved | fixed | Pets are purged of arrangement and marker when reconciled, swept or revalidated as pets. `LateResolvingPetPurged` checks no pet state or row, and that a late rider still restores. |
| R1-F13 mapped tests skipped archived boundaries | fixed | `UnregisteredBlueprints` uses a real non-ability item slot through save and disable. `ReserveBars` R22-F3 includes the native rebind. `KnownCopies` r24 includes the pet-absent update. `CompanionLoss` R17-F3 runs under an open load with rebind and two-slot cleanup. Map updated (section 7). |
| R2-F1 occupied canonical row hid a legacy row | fixed | All public rows are considered in archive order for the first restorable slot. `MixedArchiveRows` (kind 6 at occupied 3, kind 2 at 9, autofill off) keeps Fireball and one shortcut at 9 through two updates, a resave and a reload; the marker-only control restores nothing. |
| R2-F2 seeding overwrote an incoming observation | fixed | A fresh load's rows never overwrite an observed arrangement or a retirement of the same incoming world. `IncomingObservationBeforeRestore` gives only 3 in both orders (immediate save, party return, cold restore); a marker-only group beside a recovered binding keeps 5; an off-bar control restores nothing. |
| R2-F3 hidden bar hid a companion replacement | fixed | The companion check runs before the bar-availability check. `CompanionReplacedWhileBarHidden`: B gets no slot; with the same pet, one restoration at 9. |
| R2-F4 missing player dropped a thrown native write | fixed | A thrown operation is held while a load is open and no player world exists. `FencedThrownNativeWrite` gives row 9 and one shortcut; the replacement and main-menu controls drop it. M3e is killed. |
| R2-F5 thrown cleanup turned a copy into a placement | fixed | Native outcomes are read against the bar native started from (the operation context). A thrown live rebind counts. `ThrownCleanupEdges`: (a) only 3; (b) only 11. The throw-before-write removal control stays in `NativeOperationEdges`. |
| R2-F6 discarded slot objects retained | fixed | Copies are held weakly and dropped when a sweep clears their binding. `ReserveCopiesReleased`: the count drops to 0 after the sweep; a GC check shows a removed rider's copy object is not kept alive; a legitimate return restores 3 once. |
| R2-F7 handoff omissions | fixed | Section 6 restores the case-level acceptance record: executed predicates are UNIT, and the Charge, continuation and automatic filters are TRACE. Playtest 3 checks an identifiable item persisting on the same reserve pet through reopen and reload; playtest 1 checks the chosen slot through a forced dismount and the same pet's return. |

Round 3 (review of `62518858`; answered in manual round 4 by Claude, commits `4cdc34c0` and `e429d290`, source candidate
`e429d290`). **Status of every row: implementation claimed by Claude, not independently verified;** closure awaits the
owner's independent (Pro) review. "Fails on `6251885`" means the new test was compiled against the evaluated candidate's
production sources (test call shapes shimmed for the new native-operation token only) and failed there, and it passes on
`e429d290`.

| Finding | Correction (claimed) | Test / subcase and asserted outcome | Evidence | Result |
| --- | --- | --- | --- | --- |
| R1-F10 former instance authors in-world and managed rows | Every capture prepares (each placement bar observed now). In-world rows come only from authoring riders and reserve owners, one row and one marker per actor. The adapter's other managed rows come only from the units the lifecycle names as authors. | `FormerInstanceInWorldCapture` (party and reserve replacements): in-world capture before an update gives only 11 and one marker; replayed after a suspension, the current instance has 11 and the former object no binding; the next save holds 11. `FormerInstanceKeepsReplacement` (owned save, unchanged). Wiring R1-F10: the former object in custody with a callable Mounted Charge at the replacement's 11; in-world and owned captures give only kind 6 at 11 with one marker and no managed row from the former object; the rows pass the archive's key validation (production `SavedRowKey`). | UNIT + WIRING; mutations M5a, M5b, W6 | PASS; both new checks fail on `6251885` (in-world rows `[9]` plus two markers; wiring `A:11/5, A:9/6`, two markers) |
| R2-F1 delayed toolbar discards an older row | A fresh load whose actor or bar is unreadable keeps all public rows and chooses at the first readable observation; a save meanwhile carries them as read. | `DelayedBarKeepsOlderRows`: kind 6 at 3, Dismount at 9 and a marker seeded with the bar unreadable (autofill off); the unreadable-bar save carries both rows; once readable (Fireball at 3, 9 empty), after three updates Fireball stays and the only shortcut is 9; resave gives 9 and one marker; cold restores of the resave and of the unreadable-bar save give 9; marker-only control restores nothing. | UNIT; mutation M7a | PASS; fails on `6251885` |
| R2-F2 alias read before seeding moves the choice | An alias-only observation of the incoming world made before its rows were seeded yields to the archive's record (public row, or marker = off the bar); the alias becomes a known copy, or the fallback its native emptying earned when the archived slot is occupied. | `LoneAliasBeforeOrAfterSeeding`: reserve rider, archived 3, empty 3, lone stale alias at 9 emptied by GetSlot(9) immediately before and immediately after seeding; both orders give only 3 and one marker on the immediate save, the party return and the cold restore. Marker-only variant: off the bar in both orders (this also removes a previously unreported order dependence: before, seeding first gave off-bar and a native read first gave 9). Controls: `IncomingObservationBeforeRestore` (canonical marker-only recovery keeps 5), `RemovalSurvivesReserveSaveLoad`. | UNIT; mutation M7b | PASS; native-first case fails on `6251885` (saved `[9]`) |
| R2-F3 hidden bar reintroduces A's slot | The companion link is recorded independently of the bar, and seeded with the loaded rider's link. A retirement made while the bar is unreadable clears that bar's surviving bindings when it is next readable. | `HiddenBarCompanionRetirement` (a): the same array hidden and reattached with A's original 9; retirement observed during the outage; no 9 after the bar returns, in the save and after a cold load; B's fresh 11 is saved and restored once; the same pet keeps 9 once. (b): saved 9 seeded with the bar unreadable; A replaced by B; an empty bar revealed: no 9 (same pet: 9). | UNIT; mutation M8a | PASS; (a) and (b) each fail on `6251885` (`[9]`) |
| R2-F4 unrelated postfix consumes the held write | Each prefix returns its own operation through Harmony `__state`; a postfix completes only its own; thrown operations are held in a separate list through an open load's missing player and released on replacement, main menu and disable. | `HeldThrownWriteSurvivesCompleteRead`: GetSlot empties 9 and throws; selected load; player world missing; complete GetSlot calls on an ordinary and an empty slot; update; abandoned: row 9 and one marker, one shortcut at 9, the ordinary slot unchanged; disable control. `FencedThrownNativeWrite` (replacement and main-menu controls). Wiring R2-F4: the same through the service's prefix and postfix. | UNIT + WIRING; mutations M6a, M3e | PASS; both fail on `6251885` |
| R2-F5 settlement mistakes later edits for native writes | A thrown operation is settled from what its method can write (GetSlot only empties; a cleanup rebind must bind exactly the fact data noted at the prefix) and against the bar's occupancy when native started. The native exception is untouched (no catch, no re-run). | `ThrownOperationEvidence` (c): 3 pending, copy 9; cleanup rebinds 9 and throws; fact deactivated before settlement → saved 3, only 3. (d): GetSlot(9) throws before writing; player places live 9 → saved 9, only 9. (e): Fireball at pending 3; GetSlot empties fallback 9 and throws; Fireball moves to 5 → saved 9, live 9, Fireball at 5, reload 9. | UNIT; mutations M6b, M6c, M6d | PASS; (c), (d) and (e) each fail on `6251885` |
| R3-F1 in-world capture records Charge as Mount / Dismount | The host's public-binding predicate accepts only the public control or a supported alias; the capture's fresh observation finds the current public placement. | Wiring R3-F1: callable Mounted Charge at the former public 9; removed: Charge row at 9, no kind 6, one marker; moved to 11: Charge at 9 and kind 6 at 11, one marker; both valid archives; the host predicate refuses the Charge and accepts the live control. | WIRING; mutations W7, M5b | PASS; fails on `6251885` (false kind 6 at 9) |
| R3-F2 a stale slot owner suppresses native invalidation | The bar owner comes from GetSlot's `unit` argument (checked against the bar) or `UnitUISettings.Owner.Unit`, never from slot contents; the prefixes pass the unit argument. | Wiring R3-F2: a stale public slot naming the discarded rider object, emptied by GetSlot before the first scan (owner attached, and unit argument only) → the current rider recovers 9, the discarded object nothing, saved 9; whole-bar cleanup with a stale-owner alias at 4 before the chosen 9 → 9 returns once, the alias does not. Contract rows: `Owner` type and token, no nesting, GetSlot only empties, GetSlot parameter names, product forwarder shapes. | WIRING + CONTRACT; mutations W8, W9 | PASS; fails on `6251885` |
| R1-F13 regression map omits archived triggers | The three sequences as single tests in archived order; map rows name the subcase and asserted outcome (section 7). | `ArchivedR17F1AbandonedLoadFallback`, `ArchivedR17F2AbortedThenSuccessfulRead`, `ArchivedR19F3ReplacementPlacementInvalidated` | UNIT | PASS; they also pass on `6251885` (missing coverage, not defects) |
| R3-F3 developer summary omits the six inherited cases | Section 5 here and the developer summary returned with this candidate list all six individually with result, test and evidence type. | (reporting) | — | done in both deliverables |

## 10. Validation (offline, this enrollment)

Commands under `docs/VALIDATION-POLICY.md`: a focused development checkpoint; no Candidate, Full or native campaign
selected.
- **Invocation 1:** gates FAIL on `45e799ce`, then PASS on `63d35c13` and `83fdf493`.
- **Invocation 2:** gate FAIL on `9ecc4a77` (fixture compile), then PASS on `56366c41` and `b34616ff`. Probe gates on
  `56366c41`: presentation contracts PASS (239/0); F2 reader FAIL only at the missing r05 evidence (T09), with its
  source guards passing.
- **Invocation 3:** `dotnet build` of the product and tests. Focused gate:
  - FAIL on `3f19c91a` (26.4 s), with two test-fixture mistakes: an R22-F3 variant name that also matched "moved", and
    the replacement control leaving outgoing objects in the party list. Fixed in `b868cd41`.
  - PASS on `b868cd41` (37.7 s).
- The gate on the reported candidate commit is the result of record.

| Check (gate on `b868cd41`) | Result | Limits |
| --- | --- | --- |
| build | PASS | Release |
| components | 1463/0 | 43 toolbar, 6 city-roster, 3 downed-target tests |
| Validate-Source | 153/0 | source rules |
| assembly contracts (Kingmaker) | 825/0 | reflection only |
| TRE click composition | 21/0, both orders | substituted execution |
| archive row bound | 7/0 | production DTO and codec |
| six-pair archive adapter | 22/0 | typed facades |
| area adapter baseline | PASS (63/63) | compile step only |
| target liveness | 3/0 | source checks |
| toolbar mutations | PASS: lifecycle 43/0, wiring 5/0, 18/18 killed | temp copies; worktree unchanged |

Workflow budget (development): 66.7 of 90 minutes spent after the `b868cd41` gate.

### Manual round 4 (Claude, 2026-11-22)

A focused development checkpoint under `docs/VALIDATION-POLICY.md`. No Candidate, Full, package or native campaign was
selected. The cumulative development budget was carried over, not reset.
- **Reproductions first:** a scratch compile of the toolbar tests and wiring fixture (as the mutation script's baselines
  compile them). Then the current tests against the evaluated candidate `6251885`'s production sources, with a
  mechanical call-shape shim for the new native token: lifecycle 46/6 and wiring 5/4 there. Every new round-3 reproduction
  failed. The later subcases R2-F5 (d), (e) and R2-F3 (b) were isolated and each failed too. The three archived sequences
  passed.
- **`MSBuild KingmakerMountedCombat.sln /p:Configuration=Release`:** PASS (33 s), against the installed game assemblies.
- **`Test-AssemblyContracts.ps1 -Target Kingmaker`:** 832/0 (825 before; 7 new rows), standalone before the gate.
- **`.\scripts\Test-Tiers.ps1 -Plan -Tier Fast -Focused Test-CityToolbarProneFocused.ps1`:** 4 steps selected (build,
  components, Validate-Source, the focused wrapper's 7 children); estimate 34 s; budget 67.27 of 90 minutes spent.
- **Gate on `4cdc34c0`: FAIL** (33.7 s focused step). `Test-CityArchiveRowBound.ps1` did not compile: its own fake toolbar
  host lacked the two new host members. Every other child passed. Kept as failed evidence:
  `C:\Dev\KingmakerMountedCombatLab\artifacts\pr57-manual-round4-gates\gate-focused-4cdc34c0-FAIL.log` (sha256
  `4a2a89bb…`). Fixed in `e429d290` (test fixture only); the child alone then gave 7/0.
- **Gate on `e429d290`: PASS**, `TIER FAST PASS=4 FAIL=0` (0.83 min; focused step 36 s; whole invocation 50 s). Log
  `C:\Dev\KingmakerMountedCombatLab\artifacts\pr57-manual-round4-gates\gate-focused-e429d290-PASS.log` (sha256
  `6eb4f4e0…`). Budget afterwards: 68.92 of 90 minutes spent (entries 129 failed and 130 completed).

| Check (gate on `e429d290`) | Result | Limits |
| --- | --- | --- |
| build | PASS | Release; product DLL sha256 `51e70773…161df` |
| components | 1472/0 | 52 toolbar (9 new), 6 city-roster, 3 downed-target tests |
| Validate-Source | 153/0 | source rules |
| assembly contracts (Kingmaker) | 832/0 | reflection only; 7 new toolbar rows (installed contract and built-product forwarder shapes) |
| TRE click composition | 21/0, both orders | substituted execution |
| archive row bound | 7/0 | production DTO and codec |
| six-pair archive adapter | 22/0 | typed facades |
| area adapter baseline | PASS (63/63) | compile step only: base and candidate both fail to compile the facade with the same 63 errors; no area-restoration assertion executes |
| target liveness | PASS | source checks |
| toolbar mutations | PASS: lifecycle 52/0, wiring 9/0, 31/31 killed | temp copies; worktree unchanged |

Not rerun on `e429d290`: the U02 presentation probe and the F2 reader (results belong to `56366c41`), and every native
case.

## 11. Native checks: what exists and what each case needs

No native case ran, in either enrollment or in manual round 4. Under the manual continuation mission, native work comes
**after** the owner's independent review of this source candidate: the next step is to assess the repository-approved
package build and the guarded runner (`scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`), and use the smallest
supported path. The headless enrollment's command allowlist is not proof that this is impossible. If native work stays
unavailable or owner-run, the playtest below is the executable path. Status: **PENDING** (all native rows NOT RUN).
Per-case prerequisites found so far (unchanged):
- the capital route's reader (`Chunk7CapitalArrivalEvidence.ps1`) predates F2;
- no driver selects a reserve pet portrait in the group panel;
- an enemy made prone or unconscious before the click needs new diagnostic setup and reader rows;
- the F2 chain's retained r05 evidence is absent (T09).

## 12. Owner playtest (exact steps)

1. **Nondefault slot through transitions.** Use the main character or a companion with a Horse.
   - Put Mount / Dismount in slot 9.
   - Travel to the capital, walk to an interior and back, leave, and return to adventuring. The shortcut stays in 9.
   - Repeat while mounted.
   - Save and load inside the capital and outside it; 9 holds.
   - **Forced dismount and recovery:** while mounted, get unhorsed (for example the Horse knocked prone or a scripted
     dismount) or make the Horse temporarily unavailable. Pass: the shortcut shows its ordinary unavailable state or is
     absent, and no other ability takes slot 9. When the same Horse is back and mounting is legal, the shortcut is in
     slot 9 exactly once.
2. **Removals, moves and occupied slots.**
   - Remove the shortcut, visit the capital, save there, load and leave: it does not come back.
   - Move it to another slot and save at once, then load: it is in the new slot only.
   - Put another ability where it was before a visit: that ability is not overwritten.
   - Drag a second copy of the control onto the bar while it is usable: one shortcut remains.
   - If an older capital save shows Mount / Dismount on a reserve companion's bar: load it, save at once in the capital,
     reload, and bring that companion into the party. The shortcut comes back once, in the same slot.
   - With a second rider, release or replace its Horse just before an area transition (while another pair is mounted):
     the old slot stays empty for the new companion.
3. **City companion access.** Take a mercenary or custom companion with a Horse into the capital.
   - Open Inventory (I) and Character (C). The Horse appears after its owner.
   - Select it and check its name, statistics and barding and neck slots.
   - Equip one identifiable item (for example a named neck item) and leave it equipped.
   - Close and reopen the screen: the same Horse, still listed once, still wears that item.
   - Save, load, reopen and select the Horse again: the same item is still equipped on it, and no portrait is
     duplicated.
   - Disable Mounted Combat in UMM: the Horse is no longer listed. Re-enable it: the Horse is listed once.

   **Combat setup S (items 4 and 5).** The rider is a single-attack longbow user: base attack bonus below +6, no
   Rapid Shot, Manyshot or haste, and no other extra-attack source, so one attack action is one roll.
   - **Fallback (extra attacks):** keep them and compare with the same rider's unmounted routine in the same mode,
     position and target. The roll count and action cost must match.
   - **Turn based:** read the action bar's spent actions after each click.
   - **Real time:** start paused, click, unpause, note the game time of the first roll, and count the rolls within
     about 5.5 game seconds after it. That is just under one 6-second round; later rolls belong to the next automatic
     round.
   - **Pass:** under setup S, one roll in the window (with the fallback, the dismounted count). **Fail:** one roll more
     or fewer.
   - **Ammunition:** Kingmaker 2.1.7b does not track arrows for an ordinary bow. If another mod shows an ammunition
     count, it must drop by exactly the number of rolls; otherwise ammunition is not compared.
4. **Prone and unconscious targets, mounted and dismounted.** Use setup S, at about 10 ft and about 40 ft, in real time
   and turn based. Compare:
   - (a) a standing enemy;
   - (b) a conscious prone enemy, for example tripped: click its standing footprint and, separately, its lying body;
   - (c) an already-unconscious enemy, clicked after it fell.

   Run every case mounted, and again dismounted with the same weapon and positions.
   - **Pass:** an actual attack roll in the combat log in each case (one under setup S), the horse never walks into
     melee for the shot, and the mounted and dismounted routines match.
   - Note any click that becomes a move. This is the original prone-target report and is still unverified in game.
5. **Negative controls and neighbours.** All are NOT RUN in this enrollment. Use the setup-S rider mounted, with a
   longbow, against one hostile enemy; hover a roll to see its modifiers.
   - **Wall:** with a solid wall between rider and a standing enemy within bow range, click the enemy.
     - Pass: never a roll through the wall. Either an ordinary approach until line of sight followed by the setup-S
       routine, or no attack and no action spent.
     - Fail: a roll while the wall blocks the line.
   - **Spent action:** in turn-based mode, spend the Standard action on a first shot, then click again.
     - Pass: no second roll, the refusal "… has no Standard action left this turn", and the horse's actions unchanged.
     - Fail: a second roll in the same turn.
   - **Beyond firing range:** start farther away than the longbow reaches and click.
     - Pass: the ordinary approach (the horse moves with the pair's own movement), and at most one setup-S routine once
       in range. In turn-based mode, if movement runs out first, there is no roll and no Standard action spent. In real
       time, if no roll occurs within 12 game seconds, pause and note it.
     - Fail: a roll from out of range, or an action spent without a shot.
   - **Mounted melee:** switch to a single-attack melee weapon and click an adjacent standing enemy.
     - Pass: the rider's melee routine and action cost as on 0.2.13 with the same rider and positions; the horse attacks
       only on its own opportunity.
     - Fail: no roll, or a routine or spend that 0.2.13 does not show.
   - **Unmounted ranged:** dismount and repeat the near standing shot with the same rider, weapon and position.
     - Pass: one roll (turn based) or one in the real-time window, as on 0.2.13.
     - Fail: a different count or action cost.
   - **Penalties and expenditure:** compare a mounted ranged roll's hover breakdown on 0.2.13 and on this build at the
     same distance and movement.
     - The same mounted ranged penalty lines appear.
     - Against a prone target the native prone modifier appears.
     - The turn-based action (Standard, or full round for a full attack) is spent exactly once.
     - Ammunition is checked as in setup S.
     - The unconscious enemy stays prone, and no extra attack is granted.

Restoration: no native transaction ran (manual round 4 included: no package, staging, launch, lock or save was touched).

## 13. Status of each original issue (manual round 4)

| Issue | Source | Offline evidence | Native behavior |
| --- | --- | --- | --- |
| 1. Mount / Dismount keeps the chosen toolbar slot (city, area, party/reserve, save/load, companion lifecycle; moves and removals respected) | Corrections complete as claimed by Claude for all eight round-3 functional findings; awaiting the owner's independent review | UNIT 52 toolbar tests, WIRING 9, CONTRACT, 31 mutations (section 10) | NOT RUN (playtests 1–2) |
| 2. The owner's animal companion in the capital's Inventory and Character screens | Unchanged since the first enrollment (UI projection of the exact persistent, reciprocal pet) | UNIT `CityCompanionRosterProjectionTests`; CONTRACT | NOT RUN (playtest 3) |
| 3. A mounted ranged character cannot attack a prone target | The identified rejection of an explicit mounted attack on a living target that was already unconscious is corrected in source. The original prone-target report remains unreproduced; whether the reported target was conscious and prone or already unconscious remains unknown. | UNIT `MountedDownedTargetOrderTests` (executed predicates); conscious prone: TRACE only | NOT RUN (playtests 4–5). The report stays open until the behavior is verified or the owner explicitly accepts this unresolved status. |
