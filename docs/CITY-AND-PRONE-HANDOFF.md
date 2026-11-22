# City toolbar, city companion access and prone ranged target: handoff (2026-11-22)

Missions: [original](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md) and
[original plus the owner's toolbar repair addendum](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION-WITH-TOOLBAR-REPAIR-ADDENDUM.md).
Branch `codex/mounted-city-and-prone-fixes` from `main` `dbf63702` (0.2.13). Draft PR #57. Development candidate
only: no merge, release, tag, install or native run.

**Enrollment history (kept visible as the addendum requires).**
- First enrollment `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c`: 26 review rounds, 27 writer
  invocations. The reviewer ran at `high` effort for rounds 1–16 and `max` from round 17. The last reviewed candidate
  was `6298f2a5`, with decision `changes_required`. Its review records are archived at `1edd82f6`
  (`docs/agent-reviews/pr-57/`, records 001–114). The owner paused it after round 26 and authorized one re-enrollment
  to change the repair strategy and test architecture.
- This enrollment `kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f`: writer invocation 1, review round 1
  pending. The counts here are in addition to the first enrollment's.

Native contracts below were read from the installed Kingmaker 2.1.7b assembly (MVID `07fa1e4d…`) through the owner's
private local decompilation (a read-only reference; nothing was imported). The members this change depends on are
pinned in `scripts/Test-AssemblyContracts.ps1`.

## 1. Mount / Dismount shortcut disappears (toolbar persistence)

**Established cause (unchanged from the first enrollment).**
- The public control `KMC_MountStateAbility` is a runtime fact that KMC leases; a toolbar slot binds its `AbilityData`.
- Native `UnitUISettings.GetSlot` and `CleanupBadSlots` empty an ability slot whose fact is removed or inactive (the
  pinned `IsBad` rows), unless a live fact of the same blueprint exists to rebind to.
- Ordinary play makes the fact temporarily absent:
  - area unload with a mounted pair (KMC's own suspension also emptied the KMC slots);
  - the capital: `CapitalCompanionLogic.RemoveAllCompanions` moves companions to `RemoteCompanions`, so their riders
    leave KMC's candidate roster and lose the fact;
  - a save made in the capital wrote no KMC row for such a rider.
- Once the slot was empty, the one-time placement record stopped any re-placement, so the shortcut was gone for good.

Which of these paths the owner hit is not established (there is no owner log or save); the city path matches the
report.

**Did the two city issues share a cause?** They share a **trigger**, the capital's native roster change
(`CapitalCompanionLogic`), but not a defect:
- issue 1 is KMC's slot persistence through a temporary absence of the fact;
- issue 2 is native `GetGroup`'s in-game pet filter (section 2).

### The repair: one shared production lifecycle component (addendum C)

The first enrollment's repair spread its decisions across `NativeMountedControlService` and several domain helpers:
a copy registry, a load fence, a reserve-scan ledger and a native-report scope. Its component tests reimplemented the
service's orchestration in a private test `Service`. This enrollment replaces that structure.

- **`Domain/MountedToolbarLifecycle.cs`** (`MountedToolbarLifecycle<TUnit>`) is the single production owner of every
  toolbar decision, together with its sequencing and actor selection, at every boundary:
  - update (`Reconcile`) and observation of player edits;
  - native slot invalidation (`BeginNativeSlotOperation` / `EndNativeSlotOperation`; an aborted operation is settled
    at the next entry);
  - the save header (`CaptureRows(includeAbsent: true)`) and the save scope (`BeginSave`);
  - area suspension (`SuspendArea`) and resume;
  - selected load begin and abandon (`BeginSelectedLoad` / `AbandonSelectedLoad`), and completion by world
    replacement;
  - unfenced world disposal or return to the main menu;
  - companion replacement and retirement;
  - disable and disposal (`Disable`).
- **`Domain/MountedControlPlacementIntent.cs`** holds that component's state for one player world: each rider's
  arrangement, the retired set and the placement records. It also holds the pure evidence rule `ApplyBar`.
- **`IMountedToolbarHost<TUnit>`** (in the same file as the lifecycle) is the narrow game adapter. It reads and writes
  native bars and slots, facts and their `AbilityData`, rosters (party, reserve, detached, pets), the player world and
  the other exact KMC bindings and auto-use selections. It makes no toolbar decision.
- **`Integration/NativeMountedControlService.Toolbar.cs`** implements that host for Kingmaker and forwards the
  service's existing entry points to the lifecycle: `Update`, `SetEnabled`, `SuspendAreaControls`, the save scope,
  `CapturePersistentSlots`, `RestorePersistentSlots`, `BeginSelectedLoad` / `AbandonLoadedWorld`, the native slot
  brackets and `Dispose`. The main service file went from 3,823 to 2,272 lines; the adapter file has 455.
- **Tests** (`tests/MountedControlPlacementIntentTests.cs`) drive the real component through thin game-facing fakes:
  - bar and slots, facts and `AbilityData` generations;
  - roster membership (party, remote, detached, ex-companions, pets);
  - player world identity;
  - the component's own archive rows.

  The drivers only call the component's public boundaries in the order the service and the persistence service call
  them. The duplicated test `Service` is gone.

**The model.** A rider's arrangement is the **set of slots where the player keeps the control**, plus the subset that
is **pending**: emptied by native bad-slot cleanup or by a KMC sweep, not by the player, and therefore owed back. An
empty set is "off the bar": the player removed it, or the one automatic placement found a full main bar.

One rule applies at every boundary: **observe the bar first, then mark what the boundary empties as pending.** The
evidence rule (`ApplyBar`) reads the bar as follows:
- a slot of the arrangement that is no longer on the bar and is not pending was removed by the player;
- a public binding at a slot outside the arrangement, or back at a pending slot, was placed by the player; the bar is
  the arrangement from then on, and earlier pending slots are superseded;
- a pending slot the player filled with anything else is given up, never overwritten.

Pending slots return, each once, into empty slots when the control is live. KMC writes the public control into no slot
outside the arrangement except the one automatic placement.

Companion identity is checked before any observation. The arrangement is retired, and the rider's public bindings
(canonical and alias) are cleared, when either:
- the persistent pet link (`PetUniqueId`) names another companion; or
- the link is cleared while the rider no longer gets the control (`ShouldLease`).

A mounted or faulted rider that still gets the control keeps the arrangement with an empty link. A retired actor is not
re-seeded by an older in-world replay.

| Boundary | What the lifecycle does, in order |
| --- | --- |
| Update | Settle an aborted native operation, follow the player world. Each rider: observe, then the host leases facts (unchanged adapter work), then repair (rebind stale canonical data; an alias inside the arrangement becomes the public control in place when live, otherwise it is cleared and pending; an alias outside it is cleared), then return pending slots and run the one automatic placement. Departed riders: observe, then still on the roster: KMC bindings cleared with public slots pending; off the roster: retired. Reserve and detached owners: observe, then clear (public slots pending), then migrate or clear their other KMC bindings; their persistent pets are swept. Absent actors: retire on roster loss or a changed or cleared pet link. |
| Native `GetSlot(i)` / `CleanupBadSlots` | Prefix: note the public bindings in range by slot object. Postfix: every noted slot native replaced with anything but a public binding (a rebind keeps it) was emptied, so the bar as it was is observed and the emptied slots become pending. No postfix (native threw): at the next entry only a slot holding native's own write (an empty slot bound to the unit) counts; the player's drag-off writes null and never counts. Only in the world that produced the operation, and only for a unit on its roster. |
| Save header | Observe every placement unit (riders, observed former riders, reserve owners), retire departed former riders, revalidate absent actors. Each arrangement whose actor resolves uniquely as a player character writes one kind-6 row per slot (live or pending, index 0–127) and one kind-7 placement record. |
| Save scope | Observe every placement unit. Then every sanitation unit loses its public bindings (pending), its other KMC bindings (leased back by the host after the save), its exact KMC auto-use selection and its KMC facts. A disabled service sanitizes and records nothing. The scope's end is an update (the pending slots return), then the leases. |
| Area unload / resume | As the save scope, with the host clearing the other bindings. Resume is an update. |
| Selected load | Nothing is moved or cleared. The outgoing world keeps its records; an abandoned load simply finds them unchanged. A distinct player world releases them, and the incoming world starts empty, then seeds from its archive rows. A missing player releases them only when no selected load is open. |
| Main menu / unfenced disposal | No player world and no open load: the records (and any open native operation) are released. Observed units of an earlier world are cleaned up and dropped at the next update. |
| Disable / dispose | Every sanitation unit loses every exact KMC binding, auto-use selection and fact. Records, observed units and the load are dropped. |

**Placement versus sanitation (addendum C3).**
- *Placement eligibility* (who can own an arrangement): current riders that are not pets, observed former riders of
  the current world (not present under another object), and reserve or detached owners.
- *Sanitation coverage* (whose managed references a cleanup boundary clears) is wider: current riders (including one
  that rejoined the party before the next update), every observed former rider (an earlier world's object too),
  reserve and detached owners, and those owners' exact current-world persistent pets (`Descriptor.Pet` whose master is
  the owner).
- Sanitation touches only exact KMC slots, auto-use selections and facts. Ordinary abilities, ordinary auto-use
  selections and equipment are untouched.

**Save format: unchanged.** The row kinds are 6 and 7, with the 128-row bound and the archive's own validation
(`MountedArchiveData.ValidateSlots`). A rider with several slots writes several kind-6 rows at distinct indices; the
archive keys rows by actor and index. Older Mount / Dismount rows (kinds 1 and 2) collapse to one control, as F2 does:
the first whose slot can hold the control on a fresh load. Downgrade stays unsupported, as before.

**Contracts kept.** The native contract pins (the `IsBad`, `CleanupBadSlots`, `GetSlot`, rebind-shape, empty-slot and
drag-off rows), the Tabletop Rules Expansion click composition (unchanged paths; its regression runs in both patch
orders), stable actor, pair and save identities, and every requirement of the original §3–§4.

### Decisions that differ from the first enrollment's candidate (stated explicitly)

1. **The player's own duplicate bindings are kept.** The previous candidate kept one "effective" slot and removed
   every other public binding as a "copy". The 0.2.13 baseline (`main`) never removed duplicates. Here the arrangement
   is the set of slots the player keeps:
   - KMC creates no extra binding and removes none the player kept;
   - a pending slot the player filled yields only itself.

   Findings whose expected result was "exactly one control" for a bar on which the player had several bindings now
   expect the player's bindings instead. They are marked *set* in the regression map (section 6). Their concrete harm
   is still covered: a shortcut lost, moved, duplicated by KMC or resurrected. I read the mission's "do not … duplicate
   it" as forbidding KMC from creating copies, not as requiring KMC to delete bindings the player placed. If the owner
   wants duplicates collapsed, that is an owner decision; the evidence rule would then need a precedence order.
2. **No load fence.** Records are per player world and are not moved at load begin. An abandoned load therefore keeps
   everything, including the reserve-scan classification of AC-R26-F1, and a replacement or unfenced disposal releases
   it (D4, D5).
3. **The `IsBad` postfix is removed.** Native invalidation is observed by bracketing `GetSlot` and `CleanupBadSlots`
   and comparing slot objects (prefix and postfix). A standalone `IsBad` query records nothing by construction. The
   `IsBad` contract rows stay pinned, because they explain why native empties the slot.
4. **Reserve and detached bars are observed and swept every update.** This replaces the previous one-time first scan
   and ledger. Their persistent pets are swept for exact KMC bindings too, and at save and disable also for auto-use
   selections and facts.
5. **The save scope no longer leases the public control.** Its slots become pending and return through the update at
   the end of the scope; other exact KMC bindings keep their leases. An older alias the scope cleared therefore comes
   back as the canonical control at its index, one update earlier than the alias migration would have made it.
6. **Older aliases** are evidence of the player's slot only on a bar without a canonical binding: the first alias, as
   F2 migrates. An alias that appears at a new position after an observation (the player moved it) is the player's
   placement. Aliases outside the arrangement are cleared as F2 leftovers.

**Remaining uncertainty (§4).**
- Native `UnitUISettings.CleanupSlots` (respec) resets the whole bar, as before.
- A native slot replacement outside `GetSlot` and `CleanupBadSlots` would be read as the player's removal; none is
  known in 2.1.7b.
- If `GetSlot` runs inside `CleanupBadSlots`, the outer bracket is settled early by the native-write rule.
- Every behavior above is component-tested through the production component, not observed in game. The native cases
  are NOT RUN (section 10).

## 2. Animal companions in the city's inventory and character screens

Unchanged in this enrollment (addendum B).

**Established cause.** In a capital, the full-screen Inventory, Character and Spellbook screens use `GroupController`
with `WithRemote` and `WithPet`, that is `UIUtility.GetGroup(withRemote: true, withPet: true)` (token `06004281`).
That call lists every party and reserve companion, in or out of the game, but adds pets only from
`Player.ControllableCharacters`, which requires `IsInGame`. `CapitalCompanionLogic` takes custom companions out of the
game (also companions without a capital spawn place, and inactive dual companions), and `OnIsInGameChanged` takes
their pets out with them. So those owners appear, but their animal companions do not. Batch199 row 59 (`3842ad94`)
recorded the hired Small Ranger in Oleg's as remote and out of game, and its Horse out of game.

**Fix.** A bounded UI roster projection: an exact-token Harmony postfix on `UIUtility.GetGroup` delegates to
`CityCompanionRosterAdapter` → `CityCompanionRosterProjection`. It applies only while Mounted Combat is enabled (R1-F6)
and only in a capital with both flags. Each listed non-pet owner's own pet is inserted right after it, under native's
own pet rules:
- the link is reciprocal (`Descriptor.Pet` / `Master`);
- the pet is not a summon and is one of the player's cross-scene characters;
- the owner is not detached and not an ex-companion;
- no pet is listed twice.

The existing persistent pet instance is listed, so native selection, inventory, doll and character sheet bind that
exact unit. Nothing changes party membership, ownership, `IsInGame`, scenes, formation, XP or combat. A native-rule
failure falls back to the native list. Dead pets follow native. Selection fallback is native. Rename keeps working.

## 3. Mounted attacks against prone and downed targets

Unchanged in this enrollment (addendum B), except for this wording.

**Status: the identified rejection of attacks against an already-unconscious target is corrected in source. The
original prone-target report still needs matched in-game verification.**

**Unconscious (downed) enemy: the mechanism and the source fix.**
- An unconscious unit is prone. Native `UnitAttack` keeps an explicit order on an enemy that was already unconscious
  when ordered (`m_IsUnconsciousAtStartOfCommand`).
- The mounted pair path required a conscious target at every gate: stock-click classification, intent validation, the
  admission context, and the pair command's target validity, ranged tail and continuation.
- `UnifiedMountedStockAttackPolicy.TargetLifeAdmits` mirrors the native rule. `MountedAttackIntent` records
  `TargetUnconsciousWhenOrdered` for a player order only. `MountedPairAttackCommand.AdmitTargetUnconsciousWhenOrdered`
  keeps that exact target valid while it lives.
- Unchanged: automatic proposals, continuation to the next target, Charge, a target that falls unconscious after the
  order, dead targets, modifiers, ammunition, costs, line of sight and range, approach.

**Conscious, tripped enemy: traced stage by stage, with no mounted divergence found in code (R1-F9).** Mouse targeting
is native and target-side: a prone conscious unit's colliders keep their setup pose, so a click on the lying body can
hit the ground, which for a mounted rider becomes a pair move. Click classification, admission, approach, range, line
of sight and firing have no prone term. This is a code trace, not an observation. The reported weapon and the enemy's
state are not identifiable from the available evidence.

## 4. Lifecycle properties through the production path (addendum D)

| Property | Behavioral tests (production component through thin fakes) |
| --- | --- |
| D1 intent recorded before cleanup destroys its native evidence | `IntentBeforeCleanup` (departure, save header before departure cleanup, area suspension before the departure loop); `NativeInvalidationVersusRemoval` (move, then native cleanup); `LegacyAliases` (alias cleared while not live) |
| D2 the same edits give the same result whether saving or updating first | `SameEditsEitherOrder`: six edits (move, removal, native emptying then a new placement, native emptying then a filled slot, native emptying with the fact back, move then departure). Each runs update-first and save-first on identical games, comparing archive rows, bars and cold-loaded bars |
| D3 repeated scans or saves never move or resurrect | `RepeatedBoundariesIdempotent` (four rounds of updates, saves and area cycles); `OccupiedAndIndependent` (a given-up slot never returns) |
| D4 a canceled load preserves the complete outgoing state | `LoadLifetime` (reserve slot, removal, native emptying made while the load was open, missing-player frame); `AcR26F1CanceledLoadKeepsEdit`; `AcR26F2MainMenuReleases` (fenced abandonment keeps the duplicate) |
| D5 replacement and unfenced disposal release the old world | `AcR26F2MainMenuReleases` (main menu: no records, no world, no observed unit, no native operation, no load; real transition: empty incoming records); `DisableAndLifetime` (main menu then a new game); `LoadLifetime` (outgoing objects author nothing) |
| D6 native invalidation keeps intent; a deliberate removal stays a removal | `NativeInvalidationVersusRemoval`, `NativeOperationEdges` (validity query, throw before and after the write, outgoing world's aborted operation, filled slot after an emptying, invalidated new placement) |

## 5. Inherited acceptance cases (addendum F)

Provenance for all six: enrollment `…-e55c`, round 26, reviewed commit `6298f2a5`, archived review
`docs/agent-reviews/pr-57/112-r26-review.md` (envelope `113`). These are acceptance-case references, not findings of
this enrollment. Evidence type for all six: UNIT, meaning the production `MountedToolbarLifecycle` through thin fakes.
All six pass in `bin\Tests` (1448/0) and again in the mutation baseline (28/0) on the reported candidate.

| Case | Outcome | Test | What it exercises |
| --- | --- | --- | --- |
| AC-R26-F4 reserve owner's pet | PASS | `AcR26F4ReservePetSanitized` | Separate membership: the owner is in `RemoteCompanions`, its horse is not. A stale Mount Primary slot and auto-use selection on the horse, with no fact. Save with no prior update: inside serialization the slot is empty and the auto-use is null. Disable: both are cleared. The horse's ordinary ability and another pet's ordinary auto-use survive. |
| AC-R22-F3 fresh placement after native emptied the only shortcut | PASS | `AcR22F3FreshPlacementAfterNativeEmpty` | A never-observed reserve rider with only slot 3; `GetSlot` empties it; the player places at 11; save before any reserve scan. The archive holds 11 and one record; one shortcut at 11 on a same-world return and after a cold restore. No-edit control: 3. Duplicate controls: 3 and 7 untouched, or 7 and 11 after the placement (*set*). |
| AC-R25-F3 fresh shortcut after companion replacement | PASS | `AcR25F3FreshShortcutAfterReplacement` | Reserve rider with a live fact at 9 observed; pet A replaced by B; an update retires A's arrangement; placement at 11; save; cold restore gives one shortcut at 11 for B. Without a placement: nothing, and 9 is never restored. |
| AC-R26-F1 canceled load and the next save | PASS | `AcR26F1CanceledLoadKeepsEdit` | Reserve rider swept (9 cleared); placement at 11; a selected load begun and abandoned before world replacement and before KMC observes the edit. The immediate save carries 11 and one record; a cold restore gives 11 once. No-edit control: 9. |
| AC-R26-F2 main menu releases the duplicate registry | PASS | `AcR26F2MainMenuReleases` | A duplicate (3 and 7) on a never-observed reserve rider, recorded through `GetSlot(3)`; return to the main menu; an update with no player. The lifecycle holds no records, no world, no observed unit, no native operation and no load. Controls: a fenced missing-player frame keeps the records, and abandonment saves 3 and 7; a real transition gives empty incoming records. |
| AC-R26-F3 rejoined rider's stale auto-use | PASS | `AcR26F3RejoinedRiderAutoUse` | A stale `KMC_RiderPrimary` selection moves from reserve to the party immediately before a save, and separately before disable, and in a disabled save. Inside serialization and afterwards it is null; an ordinary selection survives; the disabled save records nothing. |

## 6. R1–R26 regression map (addendum E)

Every finding of the first enrollment (IDs from the archived envelopes; round 6 raised none), with the test or
subcase covering it in `tests/MountedControlPlacementIntentTests.cs`, unless another file is named.
- "r*N*" marks a later round's re-raise of the same finding.
- *set*: the old expectation was "exactly one control" on a bar where the player kept several bindings; the test now
  expects the player's bindings (decision 1).
- *gone*: the mechanism the finding concerned no longer exists (copy registry, scan ledger, load fence, `IsBad`
  scope); the test asserts the behavior the finding protected.

| Finding | Concern | Covered by |
| --- | --- | --- |
| R1-F1 | removal through a reserve save and load | `RemovalSurvivesReserveSaveLoad` |
| R1-F2 | move or removal between update and save scope | `SameEditsEitherOrder` (move 9→3, remove 9; save-first branch: archive, bar after the scope, cold load) |
| R1-F3 (r2, r3) | native emptying before re-lease; fact back first; move then cleanup | `NativeInvalidationVersusRemoval` (first three subcases) |
| R1-F4 | retained remount replay after a removal | `InWorldReplay` (removal, then replay) |
| R1-F5 | permanent pet loss in reserve | `CompanionLoss` (R1-F5 subcase, with kept-pet control) |
| R1-F6 | city roster after disable | `CityCompanionRosterProjectionTests` (unchanged) |
| R1-F7 | archive bound, no silent truncation | `Test-CityArchiveRowBound.ps1`: 128 lifecycle rows round-trip; 129 required rows written and then refused by the production validator |
| R1-F8 | native fixture description | section 10 (unchanged correction) |
| R1-F9 | conscious prone traced separately | section 3 trace; native legs NOT RUN (owner playtest 4) |
| R2-F1 (r3, r4) | save header during an interruption, stale binding, unresolved pet | `HeaderDuringInterruption` (first three subcases) |
| R2-F2 (r3) | edit then departure; header before departure cleanup | `IntentBeforeCleanup` (departure; header subcase) |
| R2-F3 | same pet link, pet unresolved | `CompanionLoss` (R2-F3 subcase) |
| R2-F4 (r11) | removal right after the save scope returns the slot | `RemovalAfterSaveRelease` (canonical and alias) |
| R2-F5 | adapter facades and matched baseline | `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1` 22/0; `Test-CityAreaAdapterBaseline.ps1` |
| R3-F1 (r4) | outgoing world writing into the loaded world | `LoadLifetime` (R3-F1 subcase: outgoing edit and outgoing object's later edit); `NativeOperationEdges` (outgoing aborted operation) — *gone* (fence) |
| R3-F2 | prone playtest negative controls | owner playtest item 5 (unchanged) |
| R4-F1 (r5) | removal then brief ineligibility; removal then native deactivation | `NativeInvalidationVersusRemoval` (R4-F1 and R4-F1 r5 subcases) |
| R4-F2 (r5, r6, r7) | permanent loss on a party rider; direct pet change; cleared link with a native report; pet change across a transition | `CompanionLoss` (R4-F2, r5, r6 subcases); `InWorldReplay` (r7, with the same-pet control) |
| R4-F3 | area suspension before the departure loop | `IntentBeforeCleanup` (area subcase with a second rider) |
| R4-F4 | validation claims | section 8 reports only runs of this enrollment, with the gate receipt as the result of record |
| R5-F1 (r6) | abandoned load keeps reserve intent, removal, native emptying during the load | `LoadLifetime` (first block) — *gone* (fence) |
| R5-F2 | superseded load scope | `LoadLifetime` (owner-only abandonment) |
| R7-F1 | older capital save's reserve binding: carried, not serialized, cleared on disable | `ReserveBars` (R7-F1 block) |
| R8-F1 | older reserve bar: canonical beats alias; Mounted Charge to stock Charge | `ReserveBars` (R8-F1 block) |
| R8-F2 | faulted rider, empty link, native cleanup | `CompanionLoss` (R8-F2 subcase) |
| R9-F1 | disable before registration: only identifiable KMC blueprints | `UnregisteredBlueprints` (the production `ResolveExactKind` the adapter's `ReadSlot` uses) |
| R9-F2 (r10) | companion replaced during the save scope | `CompanionChangeDuringSave` (canonical and alias) |
| R9-F3 | scan bookkeeping lifetime | `DisableAndLifetime` (removed rider released; main menu) — *gone* (ledger) |
| R9-F4 (r10) | older alias while the pet is unresolved | `LegacyAliases` (native-first and update-first) |
| R10-F1 (r11) | header before repair with a live alias | `LegacyAliases` (first subcase); archive check "an older alias at 9" |
| R11-F1 | alias beside a canonical binding, saved first | `LegacyAliases` (R11-F1 subcase, cold load); archive check "alias at 3 beside canonical 9" |
| R12-F1 | stale and current canonical bindings at save | `PlayerCopies` (first subcase) — *set* (3 and 9) |
| R12-F2 | alias the scope returned, second save | `LegacyAliases` (R12-F2/R13-F1 subcase); `RemovalAfterSaveRelease` (alias) |
| R12-F3 | pet change clears canonical and alias | `CompanionLoss` (R12-F3 subcase) |
| R12-F4 (r13) | native rebind then removal | `NativeInvalidationVersusRemoval` (R12-F4 subcase) |
| R13-F1 | returned alias emptied by `GetSlot` | `LegacyAliases` (R12-F2/R13-F1 subcase: the scope returns the canonical control at the alias index) |
| R13-F2 | save before the first update, no autofill copy | `InitialPlacement` (first subcase) |
| R13-F3 | remembered slot occupied by another KMC binding | `HeaderDuringInterruption` (R13-F3 subcase); archive check "occupied by an older Mounted Charge" |
| R13-F4 | aborted native operation across a world replacement | `NativeOperationEdges` (R13-F4 subcase) |
| R14-F1 | manual placement emptied, then its slot filled | `NativeOperationEdges` (R14-F1 subcase) |
| R14-F2 (r15, r16, r17) | canonical 4 and alias 9 under native cleanup, save, update, alias rebind | `LegacyAliases` (four variants) |
| R14-F3 | occupied at save, occupant cleared later | `HeaderDuringInterruption` (R14-F3 subcase) |
| R14-F4 | disabled, then another world loaded | `DisableAndLifetime` (disabled save and load) |
| R14-F5 | native rebind during an open load, abandoned | `NativeInvalidationVersusRemoval` (R12-F4 rebind then removal); `LoadLifetime` (an open load changes no record) — *gone* (fence) |
| R15-F1 (r16–r19) | copies emptied, saved, re-placed | `PlayerCopies` (R15-F1, R15-F1 r18 subcases) — *set* for r16/r17; r18 asserts 9 alone; r19 via `MoveSupersedesPending` |
| R15-F2 | disabled save of an older surviving shortcut | `DisableAndLifetime` (disabled save) |
| R16-F1 | automatic placement, then a change of companions | `CompanionLoss` (R16-F1 subcase) |
| R17-F1 (r18, r19) | filled chosen slot with a surviving copy | `PlayerCopies` (filled-3 subcase, live, save and cold load) — *set* (7 and 9); r19 via `LoadLifetime` (open load) |
| R17-F2 (r18, r19, r20) | validity query, throws before and after the write, header after a throw | `NativeOperationEdges` (first four subcases) |
| R17-F3 (r18) | retirement under a load fence and in a cleanup batch | `CompanionLoss` (r6: cleared link reported by native cleanup) — *gone* (fence) |
| R18-F1 (r19–r24) | emptied chosen slot with copies; rebind then removal | `PlayerCopies` (R15-F1/R18-F1 subcase *set*; r21–r24 rebind-then-removal subcase keeps the removal and the ordinary ability at 0) |
| R18-F2 | emptying of a bad alias not credited to a removed alias | `LegacyAliases` (R18-F2 subcase) |
| R18-F3 | combat harness liveness assertion | `Test-CityTargetLiveness.ps1` (unchanged, focused) |
| R19-F1 | KMC migration before native reads the alias | `LegacyAliases` (update-first variant) |
| R19-F2 (r20, r21, r22) | move past a surviving copy, rebind, re-invalidation | `MoveSupersedesPending` (first two blocks) — *set* (the surviving copy 7 stays beside 11) |
| R19-F3 | newer placement invalidated before reconcile | `NativeOperationEdges` (R19-F3 subcase) |
| R20-F1 (r21, r22) | copy registry lifetime | `DisableAndLifetime` (removed rider released) — *gone* (registry) |
| R21-F1 | update-first stale and current canonical bindings | `PlayerCopies` (first subcase, update-first) — *set* |
| R21-F2 | disabled save then another world | `DisableAndLifetime` |
| R21-F3 | evidence labels | section 9 evidence types |
| R22-F1 | filled 3, copies at 7 and 9, save | `PlayerCopies` (filled-3 subcase) — *set* |
| R22-F2 | older rows 3 and 9, 3 occupied at restore | `PlayerCopies` (R22-F2 subcase) |
| R22-F3 (r23–r26) | first reserve scan after native emptying (occupied, rebound, moved, removed, free, only shortcut) | `ReserveBars` (four variants); `AcR22F3FreshPlacementAfterNativeEmpty` (r26) |
| R23-F1 | copy survives a stale-copy emptying | `PlayerCopies` (filled-3 subcase, `GetSlot(9)` before the save) — *set* |
| R23-F2 | main menu then a new game | `DisableAndLifetime` (R23-F2 subcase) |
| R23-F3 (r24, r25) | a moved callable alias, with a copy, pet unavailable | `MoveSupersedesPending` (alias block, both availabilities) |
| R24-F1 | temporary KMC facts on a reserve rider at save and disable | `ReserveBars` (R24-F1 block) |
| R24-F2 | incoming world's first native report | `LoadLifetime` (R24-F2 subcase) |
| R24-F3 (r25) | missing-player frame during a load, abandoned | `LoadLifetime` (R24-F3 subcase); `AcR26F2MainMenuReleases` (fenced) |
| R25-F1 | reserve rider available before its first scan during an area transfer | `InWorldReplay` (R25-F1 subcase) |
| R25-F2 | reserve rider's managed auto-use at save and disable | `DisableAndLifetime` (R25-F2 subcase) |
| R25-F3 (r26) | fresh placement on an already-scanned reserve bar; after retirement | `ReserveBars` (R25-F3 block); `AcR25F3FreshShortcutAfterReplacement` |
| R26-F1 … R26-F4 | the inherited acceptance cases | section 5 |

## 7. Production mutations (addendum E)

`scripts/Test-ToolbarLifecycleMutations.ps1`, the seventh child of the focused wrapper, works as follows:
- It copies the production component, its state class, `MountedControlSlotPolicy`, the enum file and the toolbar
  tests into a fresh temp directory.
- It compiles them with a generated entry point and requires the unmutated copy to pass every test.
- It then applies one production-line mutation at a time to the temp copy (the needle must occur exactly once),
  recompiles, reruns, and requires every named test to fail.
- It checks afterwards that the worktree sources are byte-identical. No mutated code exists in the worktree or in any
  commit.

Results on `63d35c13` (controller gate log `gate-focused-63d35c134931-…-20261122T022745636297Z.log`): baseline 28/0.

| Mutation (production) | Class | Killed by |
| --- | --- | --- |
| M1a: no observation before a sweep (`SweepBoundary`) | skips pre-cleanup observation | D1 `IntentBeforeCleanup`, `InWorldReplay` (2 tests failed) |
| M1b: no observation before a native emptying is recorded | skips pre-cleanup observation | D6 both, `LoadLifetime`, `LegacyAliases`, `ReserveBars`, AC-R22-F3, AC-R26-F2 (7) |
| M2a: reserve owners' pets omitted from the sanitation sweep | omits a required actor | AC-R26-F4 (1) |
| M2b: reserve owners omitted from the placement and observation sweep | omits a required actor | `HeaderDuringInterruption`, `ReserveBars`, AC-R22-F3, AC-R25-F3, AC-R26-F1 (5) |
| M3a: abandonment releases the outgoing records | outgoing-world lifetime | D4/D5 `LoadLifetime`, AC-R26-F1, AC-R26-F2 (3) |
| M3b: a replaced world keeps the outgoing records | outgoing-world lifetime | AC-R26-F2 (1) |
| M3c: no release without a world (main menu) | outgoing-world lifetime | `DisableAndLifetime`, AC-R26-F2 (2) |
| M4a: a player's null write after an aborted operation counts as native | edit confused with invalidation | D6 `NativeOperationEdges` (1) |
| M4b: a native emptying leaves no pending slot | invalidation confused with edit | D6 both, `HeaderDuringInterruption`, `CompanionLoss`, `LoadLifetime`, `PlayerCopies`, `MoveSupersedesPending`, `LegacyAliases`, AC-R22-F3, AC-R26-F2 (10) |
| M4c: a player's removal is kept as if owed back (`ApplyBar`) | edit confused with invalidation | 17 tests, including D6 `NativeInvalidationVersusRemoval` and `RemovalSurvivesReserveSaveLoad` |

A development run on `45e799ce` showed M4c surviving its named test. The D6 removal subcase did not take the
wrongly kept slot through a boundary. That subcase now saves and cycles an area after the removal, and M4c is killed
on `63d35c13`.

## 8. Validation (offline, this enrollment)

Commands, under `docs/VALIDATION-POLICY.md` (focused development checkpoint; no Candidate, Full or native campaign
selected):
- `dotnet build src/KingmakerMountedCombat/KingmakerMountedCombat.csproj -c Release`: PASS (development compile, 15 s);
  the same for the test project.
- `Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1`, local development runs:
  - the first two (budget entries `20261122T021853947Z` with build and `20261122T022017262Z` with `-SkipBuild`)
    exited 1. The tool output was truncated before any FAIL line, because child output is relayed only when each child
    ends.
  - To read the toolbar results, `tests/Program.cs` was changed temporarily to register only the toolbar tests and
    stop. The first such build failed (CS0162, warnings as errors; entry `20261122T022130038Z`). The next run
    (`20261122T022136817Z`) showed 28/0 toolbar tests and still exited 1, later in the focused step. The temporary change was reverted before any commit (`git status` showed
    `tests/Program.cs` unmodified) and is absent from the diff.
- Controller `focused` gate (`agent_review.py verify --gate focused -- -Tier Fast -Focused
  Test-CityToolbarProneFocused.ps1`), whose full log the controller keeps:
  - on `45e799ce`: FAIL, 32.9 s. Everything passed except the mutation child: M4c survived its named test (section 7).
  - on `63d35c13`: PASS, 53.2 s. That run's counts are below.
  - The gate on the reported candidate commit is the result of record.

| Check (gate run on `63d35c13`) | Result | Limits |
| --- | --- | --- |
| build | PASS | Release |
| components (`bin\Tests`) | 1448/0 | 28 toolbar tests (replacing the previous 86 test-double tests), 6 city-roster, 3 downed-target |
| Validate-Source | 153/0 | source and payload rules |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 825/0 | reflection-only contracts; Wrath not selected |
| `Test-GunslingerClickComposition.ps1` | 21/0, both patch orders | substituted native execution, not gameplay |
| `Test-CityArchiveRowBound.ps1` | 7/0 | lifecycle rows through the production `MountedArchiveData` and `MountedArchiveCodec`; in-memory |
| `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1` | 22/0 | actual capture and restore methods, typed facades |
| `Test-CityAreaAdapterBaseline.ps1` | PASS (63 base errors, 63 candidate errors, none only in either) | compile step only; the adapter executes in neither tree |
| `Test-CityTargetLiveness.ps1` | 3/0 | source checks |
| `Test-ToolbarLifecycleMutations.ps1` | PASS: baseline 28/0, 10/10 mutations killed | temp copies; worktree unchanged |

Workflow budget (development): 58.9 of 90 minutes spent before this enrollment's first run, and 62.4 after the
`63d35c13` gate.

## 9. Acceptance record

Evidence types:
- UNIT: the production `MountedToolbarLifecycle` (toolbar) or production domain code, driven through thin labeled fakes
  of native state.
- ARCHIVE: the production archive DTO, validator and codec.
- ADAPTER: actual production methods compiled against typed facades.
- CONTRACT: installed-assembly reflection.
- COMPOSITION: real transpilers with substituted engine boundaries.
- TRACE: source and decompilation reading.
- NATIVE: in-game structured evidence.

Source identity: the candidate commit reported with this handoff. Binary: not packaged; no native run.

| Area | Case | Result | Evidence |
| --- | --- | --- | --- |
| Toolbar transitions | Chosen nondefault slot; repeated city entry and exit; mounted and dismounted arrival; interior transition | PASS | UNIT `SurvivesTransitions` |
| Toolbar transitions | The same in the real capital | NOT RUN | NATIVE (section 10) |
| Toolbar persistence | Save and load inside and outside the city | PASS | UNIT `SaveLoadInsideAndOutsideCity` |
| Toolbar persistence | Temporary mount absence: same pet unresolved, fact inactive, faulted relationship | PASS | UNIT `CompanionLoss`, `HeaderDuringInterruption`, `InWorldReplay` |
| Toolbar persistence | Player moves and removes the shortcut, in both orders against a save | PASS | UNIT `SameEditsEitherOrder`, `RemovalSurvivesReserveSaveLoad`, `RemovalAfterSaveRelease`, `MoveSupersedesPending` |
| Toolbar persistence | Occupied slots never overwritten; another rider; unrelated slots | PASS | UNIT `OccupiedAndIndependent`, `PlayerCopies` |
| Toolbar persistence | Genuine companion removal, roster loss, disable and world disposal clean up | PASS | UNIT `CompanionLoss`, `DisableAndLifetime`, `AcR26F2MainMenuReleases` |
| Toolbar persistence | Lifecycle properties D1–D6 | PASS | UNIT (section 4) |
| Toolbar persistence | Inherited acceptance cases (six) | PASS | UNIT (section 5) |
| Toolbar persistence | Production mutations (four classes, ten mutations) | PASS (all killed) | UNIT through the mutation script (section 7) |
| Toolbar persistence | Archive: 128 lifecycle rows round-trip; 129 required rows refused; off-bar record; alias and occupancy rows | PASS | ARCHIVE |
| Toolbar persistence | Actual archive capture requests absent arrangements; presentation restores as a loaded world | PASS | ADAPTER |
| Toolbar persistence | Native bad-slot replacement, rebind shape, empty slot bound to the unit, drag-off writes null | PASS | CONTRACT |
| Toolbar persistence | Save and load in the real capital, forced dismount, pet unavailability in game | NOT RUN | NATIVE (section 10) |
| City companion UI | Owner and pet mapping, order, exact instance, multiple pets, no duplicates, repeated opening, exclusions, disabled | PASS | UNIT (`CityCompanionRosterProjectionTests`, unchanged) |
| City companion UI | `GroupController` uses `GetGroup`; `GetGroup` takes pets from `ControllableCharacters`; exact token | PASS | CONTRACT |
| City companion UI | Selecting the reserve pet, an equipment change, save and load in a real capital | NOT RUN | NATIVE |
| Prone ranged attack | Downed enemy: explicit order valid while it lives; later-unconscious abandoned; dead cancelled; only player orders | PASS | UNIT (`MountedDownedTargetOrderTests`, unchanged) |
| Prone ranged attack | Conscious prone enemy: click, admission, approach, range and line of sight, firing | PASS (no divergence found) | TRACE |
| Prone ranged attack | Matched standing, conscious-prone and already-unconscious enemies, mounted and dismounted, RT and TB, near and normal distance, with an observed roll | NOT RUN | NATIVE (owner playtest 4) |
| Negative controls | Blocked line of sight, unavailable actions, out-of-range approach, standing targets, unmounted ranged, mounted melee, mounted penalties | NOT RUN | NATIVE (paths unchanged; owner playtest 5) |
| Shared regressions | Tabletop Rules Expansion click composition, both orders | PASS | COMPOSITION |
| Shared regressions | Existing component suite | PASS | UNIT |

## 10. Native checks: what exists and what each case needs

No native case ran in this enrollment. As in the first enrollment, the guarded runner
(`scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`) needs a candidate package bound to the commit. No command
available to this enrollment builds that package (packaging is a selected candidate operation). The case prerequisites
are unchanged:
- **Capital arrival and cold load:** the route's reader (`scripts/runtime/Chunk7CapitalArrivalEvidence.ps1`) predates
  F2 and needs a versioned revision for kind-6 and kind-7 rows. Its lineage needed eight launches in batch199
  (A = c1–c5, B = x4s, r58, r59). The batch199 r58/r59 capital fixture (Oleg's; source `3842ad94`) exists; its PASS
  verdicts belong to that binary only.
- **Reserve pet selection and equipment:** no driver opens the full-screen group panel and selects a portrait; the
  closest is `Chunk4InspectionScenario`.
- **Prone legs:** standing controls exist (the ordinary longbow fixture and the Chunk 6a TB longbow cases). An enemy
  made prone or unconscious before the click needs new diagnostic setup and reader rows.
- **F2 shortcut chain (`20261105-finalize-f2-r05`):** its retained evidence is absent here (T09).

## 11. Owner playtest (exact steps)

1. **Nondefault slot through transitions.** Use the main character or a companion with a Horse.
   - Put Mount / Dismount in a non-default slot, for example slot 9.
   - Travel to the capital, walk into an interior and back, leave, and return to adventuring. The shortcut stays in
     slot 9.
   - Repeat while mounted.
   - Save and load inside the capital and outside it. The slot holds.
2. **Removals, moves and occupied slots.**
   - Remove the shortcut from the bar. Visit the capital, save there, load, and leave: it does not come back.
   - Move it to another slot and save at once, then load: it is in the new slot only.
   - Put another ability where it was before a visit: that ability is not overwritten.
   - If you had placed the shortcut in two slots, both stay and neither multiplies.
   - If an older capital save shows Mount / Dismount on a reserve companion's bar: load it, save at once in the
     capital, reload, and bring that companion into the party. The shortcut comes back once in the same slot.
   - With a second rider, release or replace its Horse just before an area transition: the old slot stays empty for
     the new companion.
3. **City companion access.** Take a mercenary or custom companion with a Horse into the capital.
   - Open Inventory (I) and Character (C). The Horse appears after its owner.
   - Select it and check its name, statistics and barding and neck slots. Equip and unequip an item.
   - Close and reopen, then save, load and reopen. No portrait is duplicated.
   - Disable Mounted Combat in UMM: the Horse is no longer listed. Re-enable it: the Horse is listed once.

   **Combat setup S (items 4 and 5).** The rider is a single-attack longbow user: base attack bonus below +6, no
   Rapid Shot, Manyshot or haste, and no other extra-attack source.
   - If only a rider with extra attacks is available, the mounted routine must match the same rider's unmounted
     routine in rolls and action cost.
   - In turn-based mode, read the spent actions after each click.
   - In real-time mode, start paused, click, unpause, and count the rolls logged within 5.5 game seconds after the
     first roll.
4. **Prone and unconscious targets, mounted and dismounted.** Use setup S, at about 10 ft and about 40 ft, in real
   time and turn based. Compare against these enemies:
   - (a) a standing enemy;
   - (b) a conscious prone enemy, for example tripped: click its standing footprint and, separately, its lying body;
   - (c) an already-unconscious enemy, clicked after it fell.

   Run every case mounted, and again dismounted with the same weapon and positions.
   - Pass: an **actual attack roll** appears in the combat log in each case (one under setup S), the horse never
     walks into melee for the shot, and the mounted and dismounted routines match.
   - Note any case where the click becomes a move instead of a shot. This is the original prone-target report and is
     still unverified in game.
5. **Negative controls and neighbours.** Use the setup-S rider, mounted, with a longbow, against one hostile enemy;
   hover a roll to see its modifiers.
   - **Wall:** never a roll through a wall.
   - **Spent action:** turn based, no second roll after the Standard action is spent.
   - **Beyond range:** the ordinary approach, then at most one routine once in range.
   - **Mounted melee:** the same as on 0.2.13.
   - **Unmounted ranged:** the same as on 0.2.13.
   - **Penalties and modifiers:** the same mounted ranged penalty lines as on 0.2.13, and the native prone modifier
     against a prone target.
   - **Expenditure:** the action is spent once; the unconscious enemy stays prone; no extra attack.

Restoration: no native transaction ran.
