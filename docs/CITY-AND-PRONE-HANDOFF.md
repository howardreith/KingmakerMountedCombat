# City toolbar, city companion access and prone ranged target: handoff (2026-11-22)

Missions: [original](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md) and
[original plus the owner's toolbar repair addendum](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION-WITH-TOOLBAR-REPAIR-ADDENDUM.md).
Branch `codex/mounted-city-and-prone-fixes` from `main` `dbf63702` (0.2.13). Draft PR #57. Development candidate
only: no merge, release, tag, install or native run.

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
  - writer invocation 3 answers round 2 (section 9). Review round 3 is pending.

  These counts are in addition to the first enrollment's.

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

**Native operations.** The prefix notes every public binding on the bar as native starts (the operation's context).
After the postfix, or when an operation that threw is settled at the next entry, the bar is observed **as that
context** and the written slots are applied in index order:
- an emptied chosen slot becomes pending; a rebound chosen slot is live;
- a rebound copy stays a copy under its new slot object;
- an emptied copy is gone, unless it was the last surviving copy of a filled pending choice, in which case it becomes
  that choice.

For an operation that threw, only native's own writes count: an empty slot bound to the unit, or a live binding of the
control. A public binding the player placed after native started is outside the context, so it is read at the next
ordinary observation as the player's edit. While a selected load is open and no player world exists, a thrown operation
is kept and settled when its own world is current again; a replacement or the main menu drops it.

**Authority and identity.**
- Placement authority goes by exact roster identity. An object on the current player roster authors its actor's
  arrangement. An object off the roster authors only while no other object of the same character is on the roster as
  a current rider or reserve or detached owner. A former instance kept by pair custody is therefore only cleaned up.
- Companion identity is checked before any observation and independently of the bar. A pet link naming another
  companion, or cleared while the rider no longer gets the control, retires the arrangement.
- A unit positively identified as a pet owns no arrangement or marker. Rows admitted before it resolved are purged when
  it appears as a pet.

**Archive rows.** On a fresh load, every row that restores as the public control is considered in archive order: kind
6 and the older Mount / Dismount kinds 1 and 2. The first row whose slot can hold the control is the choice; a replay
takes the first row. A fresh load's rows never overwrite what the same incoming world already observed for that actor
(a native read of its bar before the restore). Seeding first and observing second gives the same result.

| Boundary | What the lifecycle does, in order |
| --- | --- |
| Update | Settle an aborted native operation (held while a load is open and no player world exists); follow the player world. In that open-load, no-world interval, stop: nothing is observed, cleared or released. Then for each rider: a pet is purged of rider state; a non-authoring former instance has its public bindings cleaned; otherwise observe, the host leases facts, repair, return the pending slot, and run the one automatic placement. Then departures (observed, classified, KMC bindings, auto-use and facts cleared), the reserve sweep (observed, cleared with the chosen slot pending, persistent pets swept and purged of rider state), and revalidation of absent actors. |
| Native `GetSlot(i)` / `CleanupBadSlots` | Context noted at the prefix; outcome applied at the postfix or at settlement, as above. Only in the world that produced the operation, for a unit on its roster. The lifecycle alone holds the operation's unit. |
| Save header | Observe every authoring placement unit; retire departed former riders; revalidate. Each arrangement whose actor resolves as a player character that is not a pet writes one kind-6 row (chosen slot, live or pending, 0–127) and one kind-7 marker. |
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
- Behavior is component-tested through the production component, and the adapter wiring through its fixture. Nothing
  is observed in game; the native cases are NOT RUN (section 11).

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

Unchanged in this enrollment (addendum B), except for this wording.

The identified rejection of attacks against an already-unconscious target is corrected in source. The original prone-target report still needs matched in-game verification.

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
| D1 intent before cleanup | `IntentBeforeCleanup`; `NativeInvalidationVersusRemoval`; `LegacyAliases` |
| D2 same edits, either order | `SameEditsEitherOrder`; `LegacyChargeEitherOrder`; `FormerInstanceKeepsReplacement`; `IncomingObservationBeforeRestore` (native read before and after restore) |
| D3 no move or resurrection | `RepeatedBoundariesIdempotent`; `OccupiedAndIndependent`; `KnownCopies` |
| D4 canceled load keeps the outgoing state | `LoadLifetime`; `MissingPlayerDuringCanceledLoad`; `FencedThrownNativeWrite`; `AcR26F1…`; `AcR26F2…` (fenced) |
| D5 replacement and disposal release | `AcR26F2…`; `MissingPlayerDuringCanceledLoad`; `FencedThrownNativeWrite` (controls); `ReserveCopiesReleased` (weak copies); `DisableAndLifetime`; wiring check for the thrown operation |
| D6 invalidation versus removal | `NativeInvalidationVersusRemoval`; `NativeOperationEdges`; `NativeRebindOfPendingSlot`; `ThrownCleanupEdges`; `KnownCopies` |

## 5. Inherited acceptance cases (addendum F)

Provenance for all six: enrollment `…-e55c`, round 26, reviewed commit `6298f2a5`, archived review
`docs/agent-reviews/pr-57/112-r26-review.md` (envelope `113`). These are acceptance-case references, not findings of
this enrollment. Evidence type: UNIT (the production lifecycle through thin fakes). All six pass on the reported
candidate.

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

Source identity: the candidate commit reported with this handoff; no package was built.

| Area | Case | Result | Evidence |
| --- | --- | --- | --- |
| Toolbar transitions | Chosen nondefault slot; repeated city entry and exit; mounted and dismounted arrival; interior transition | PASS | UNIT `SurvivesTransitions` |
| Toolbar transitions | The same in the real capital; forced dismount and the same pet's recovery | NOT RUN | NATIVE (playtest 1) |
| Toolbar persistence | Save and load inside and outside the city | PASS | UNIT `SaveLoadInsideAndOutsideCity` |
| Toolbar persistence | Temporary mount absence (same pet unresolved, fact inactive, faulted rider, bar unavailable) | PASS | UNIT `CompanionLoss`, `HeaderDuringInterruption`, `UnavailableBarDefersRestore`, `CompanionReplacedWhileBarHidden` |
| Toolbar persistence | Moves and removals in both orders; known copies; one shortcut | PASS | UNIT `SameEditsEitherOrder`, `KnownCopies`, `MoveSupersedesPending`, `RemovalAfterSaveRelease` |
| Toolbar persistence | Occupied slots, another rider, unrelated slots | PASS | UNIT `OccupiedAndIndependent` |
| Toolbar persistence | Companion removal, roster loss, disable, world disposal; former instances and late-resolving pets | PASS | UNIT `CompanionLoss`, `DisableAndLifetime`, `FormerInstanceKeepsReplacement`, `LateResolvingPetPurged` |
| Toolbar persistence | Adapter forwarding and roster inputs | PASS | WIRING (5 checks, 5 wiring mutations killed) |
| Toolbar persistence | Archive bound and row shapes | PASS | ARCHIVE (7/0) |
| Toolbar persistence | Actual archive capture and presentation restore | PASS | ADAPTER (six-pair, 22/0) |
| Toolbar persistence | Native bad-slot replacement, rebind shape, native empty slot bound to the unit, drag-off writes null | PASS | CONTRACT |
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
| Shared regressions | Existing component suite; U02 presentation probe; F2 reader source guards | PASS | UNIT (1463/0); detached probe 239/0 and F2 guards on `56366c41` (unchanged service paths since) |

## 7. R1–R26 regression map (addendum E)

Every finding of the first enrollment (round 6 raised none), with the covering test or subcase in
`tests/MountedControlPlacementIntentTests.cs` unless another file is named. "r*N*" marks a later round's re-raise.
*Gone* marks a finding whose mechanism no longer exists (copy registry, scan ledger, load fence); the test asserts the
protected behavior.

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
| R17-F1 (r18, r19) | filled chosen slot with a surviving copy or alias | `KnownCopies`; r19 `LoadLifetime` |
| R17-F2 (r18–r20) | validity query and throws | `NativeOperationEdges`; `ThrownCleanupEdges` |
| R17-F3 (r18) | retirement during an open load: native rebind (base) or two-slot cleanup without a replacement fact (r18), then abandonment | `CompanionLoss` R17-F3 subcases (retired, binding cleared, record-only save, nothing returns for B) |
| R18-F1 (r19–r24) | emptied chosen slot with copies; rebinds; removals | `KnownCopies` (r24: the pet-absent update, then `GetSlot(9)`) |
| R18-F2 | bad alias not credited to a removed alias | `LegacyAliases` |
| R18-F3 | combat harness liveness | `Test-CityTargetLiveness.ps1` |
| R19-F1 | migration before native reads the alias; alias fallback | `LegacyAliases` |
| R19-F2 (r20–r22) | move past a copy | `MoveSupersedesPending` |
| R19-F3 | newer placement invalidated | `NativeOperationEdges` |
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

Results on `b868cd41` (gate log `gate-focused-b868cd411099-…-20261122T034846987360Z.log`): baselines lifecycle 43/0
and wiring 5/0; all 18 mutations killed.

| Mutation | Class | Failing checks |
| --- | --- | --- |
| M1a no observation before a sweep | observation | 2 |
| M1b no observation before a native write is applied | observation | 13 |
| M2a pets omitted from sanitation | actor | 2 |
| M2b reserve owners omitted from observation | actor | 8 |
| M2c a pair-custody former instance authors | actor / authority | 1 (review R1-F10) |
| M3a abandonment releases the outgoing records | world lifetime | 6 |
| M3b a replaced world keeps the outgoing records | world lifetime | 20 |
| M3c no release without a world | world lifetime | 3 |
| M3d a missing player in an open load treated as disposal | world lifetime | 1 (review R1-F2) |
| M3e a thrown native write dropped at a missing player in an open load | world lifetime | 1 (review R2-F4) |
| M4a a player's null write counted as native | edit versus invalidation | 1 |
| M4b a native emptying leaves nothing pending | edit versus invalidation | 16 |
| M4c a removal kept as owed back | edit versus invalidation | 13 |
| W1 save scope not forwarded | wiring | 2 |
| W2 detached roster omitted | wiring | 1 |
| W3 native prefix not forwarded | wiring | 2 |
| W4 no persistent pet | wiring | 1 |
| W5 no reserve or detached owners | wiring | 2 |

Limit: the main service file's `SetEnabled`, `Update` and `Dispose`, and the one-line Harmony forwarders, are mirrored
in the fixture, not compiled.

## 9. Review dispositions (this enrollment)

Round 1: all 16 findings fixed in writer invocation 2. Round 2 verified 13 of them.

Round 2 (writer invocation 3; commits `3f19c91a`, `b868cd41` and the handoff commit):

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

## 11. Native checks: what exists and what each case needs

No native case ran. The guarded runner (`scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`) needs a commit-bound
candidate package, and no command available to this enrollment builds one. Per-case prerequisites (unchanged):
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

Restoration: no native transaction ran.
