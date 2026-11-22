# City toolbar, city companion access and prone ranged target: handoff (2026-11-22)

Missions: [original](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md) and
[original plus the owner's toolbar repair addendum](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION-WITH-TOOLBAR-REPAIR-ADDENDUM.md).
Branch `codex/mounted-city-and-prone-fixes` from `main` `dbf63702` (0.2.13). Draft PR #57. Development candidate
only: no merge, release, tag, install or native run.

**Enrollment history (kept visible as the addendum requires).**
- First enrollment `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c`: 26 review rounds and 27 writer
  invocations. The reviewer effort was `high` for rounds 1–16 and `max` from round 17. The last reviewed candidate was
  `6298f2a5`, with decision `changes_required`. Its review records are archived at `1edd82f6`
  (`docs/agent-reviews/pr-57/`, records 001–114). The owner paused it after round 26 and authorized one re-enrollment,
  to change the repair strategy and test architecture.
- This enrollment, `kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f`: review round 1 returned
  `changes_required` (R1-F1…F16) on `83fdf493`. Writer invocation 2 answers it (section 8). Review round 2 is pending.
  These counts are in addition to the first enrollment's.

Native contracts below were read from the installed Kingmaker 2.1.7b assembly (MVID `07fa1e4d…`) through the owner's
private local decompilation, used as a read-only reference; nothing was imported. The members this change depends on
are pinned in `scripts/Test-AssemblyContracts.ps1`.

## 1. Mount / Dismount shortcut disappears (toolbar persistence)

**Established cause (unchanged from the first enrollment).**
- The public control `KMC_MountStateAbility` is a runtime fact that KMC leases; a toolbar slot binds its `AbilityData`.
- Native `UnitUISettings.GetSlot` and `CleanupBadSlots` empty an ability slot whose fact is removed or inactive (the
  pinned `IsBad` rows), unless a live fact of the same blueprint exists to rebind to.
- Ordinary play makes the fact temporarily absent:
  - area unload with a mounted pair, where KMC's own suspension also emptied the KMC slots;
  - the capital: `CapitalCompanionLogic.RemoveAllCompanions` moves companions to `RemoteCompanions`, so their riders
    leave KMC's candidate roster and lose the fact;
  - a save made in the capital wrote no KMC row for such a rider.
- Once the slot was empty, the one-time placement record stopped any re-placement, so the shortcut was gone for good.

Which of these paths the owner hit is not established: there is no owner log or save. The city path matches the
report.

**Did the two city issues share a cause?** They share a **trigger**, the capital's native roster change
(`CapitalCompanionLogic`), but not a defect:
- issue 1 is KMC's slot persistence through a temporary absence of the fact;
- issue 2 is native `GetGroup`'s in-game pet filter (section 2).

### The repair: one shared production lifecycle component (addendum C)

**Files and roles.**
- **`Domain/MountedToolbarLifecycle.cs`** (`MountedToolbarLifecycle<TUnit>`) is the single production owner of every
  toolbar decision, with its sequencing and actor selection, at every boundary:
  - update (`Reconcile`) and observation of player edits;
  - native slot invalidation (`BeginNativeSlotOperation` / `EndNativeSlotOperation`; an aborted operation is settled
    at the next entry);
  - the save header (`CaptureRows(includeAbsent: true)`) and the save scope (`BeginSave`);
  - area suspension (`SuspendArea`) and resume;
  - selected load begin and abandon, and completion by world replacement;
  - unfenced disposal and the main menu;
  - companion replacement and retirement;
  - disable and disposal.
- **`Domain/MountedControlPlacementIntent.cs`** holds that component's per-world state and the evidence rule
  (`ApplyBar`).
- **`IMountedToolbarHost<TUnit>`**, in the lifecycle file, is the narrow adapter interface. It reads and writes bars,
  slots, facts, rosters, pets and the player world, and the other exact KMC bindings and auto-use selections.
- **`Integration/NativeMountedControlService.Toolbar.cs`** implements that host for Kingmaker and forwards the service's
  entry points. Those are the save scope, area suspension and resume, capture, restore, the native slot brackets,
  load begin and abandon, the candidate rosters and diagnostics. Disable and dispose stay in the main file and call
  `toolbar.Disable()`.
- **Tests** (`tests/MountedControlPlacementIntentTests.cs`) drive the real component through thin fakes. The fakes'
  slot classification is the production `MountedControlSlotPolicy.ResolveExactKind`.
- **Wiring fixture** (`tests/Chunk7Diagnostics/ToolbarAdapterWiringFixture.cs`) compiles the actual adapter file
  against labeled Kingmaker facades and drives the service's entry points (section 7).

**The model.** A rider's arrangement is three things:
- one **chosen slot** (or "off the bar");
- whether that slot is **pending**: emptied by native bad-slot cleanup or by a KMC sweep, not by the player, and so
  owed back;
- the **known copies**: every other public binding seen on the bar, identified by index and slot object.

One rule applies at every boundary: observe the bar first, then mark what the boundary empties as pending. The
evidence rule (`ApplyBar`) reads the bar as follows:
- Copies that left the bar are forgotten. An archive row's arrangement takes the older aliases on its bar as copies
  at its first observation.
- The first public binding that is **not a known copy** (canonical bindings first, then aliases, by index) is the
  player's: it becomes the chosen slot, live. This covers a move, a fresh placement and a re-placement at the pending
  slot.
- With no such binding:
  - a live arrangement was removed by the player: off the bar, and its copies yield;
  - a pending arrangement keeps waiting while its slot is free;
  - if the player filled the pending slot, the first surviving copy becomes the choice (the fallback); with no copy,
    the shortcut is off the bar.
- Every other public binding on the bar is a known copy afterwards.

Native writes are applied in index order after observing the bar as it stood before:
- an emptied chosen slot becomes pending; a rebound chosen slot is live, so a later removal is the player's;
- a rebound copy stays a copy under its new slot object;
- an emptied copy is gone, except that the last surviving copy of a pending choice whose slot was filled becomes that
  choice.

A live control keeps **exactly one shortcut**: repair rebinds the chosen slot and removes every copy. A pending slot
returns once into its empty slot. An unavailable bar defers the return; only a bar that cannot hold the index gives it
up. KMC writes the public control into no slot outside the arrangement, except the one automatic placement.

**Companion identity.** It is checked before any observation. The arrangement is retired, and the rider's public
bindings cleared, when the persistent pet link (`PetUniqueId`) names another companion, or is cleared while the rider
no longer gets the control. A retired actor is not re-seeded by an older in-world replay.

| Boundary | What the lifecycle does, in order |
| --- | --- |
| Update | Settle an aborted native operation; follow the player world. While a selected load is open and no player world exists, stop there: nothing is observed, cleared or released. Then, for each rider: observe; the host leases facts (unchanged adapter work); repair (one shortcut, alias migration in place); return the pending slot; run the one automatic placement. Departed riders: observe, then still on the roster: KMC bindings cleared with the chosen slot pending; off the roster: retired. Their other KMC bindings, KMC auto-use selection and facts go. Reserve and detached owners: observe, then clear (chosen slot pending); their other KMC bindings migrate or clear; their persistent pets are swept. Absent actors: retire on roster loss or a changed or cleared pet link. |
| Native `GetSlot(i)` / `CleanupBadSlots` | Prefix: note the public bindings in range by slot object. Postfix: observe the bar as it was, then apply each slot native wrote (above). No postfix (native threw): at the next entry, only a slot holding native's own write (an empty slot bound to the unit) counts; the player's drag-off writes null and never counts. Only in the world that produced the operation, for a unit on its roster. The lifecycle alone holds the operation's unit; the adapter reads it to close the operation. |
| Save header | Observe every placement unit; retire departed former riders; revalidate absent actors. Each arrangement whose actor resolves as a player character that is not a pet writes one kind-6 row for its chosen slot (live or pending, index 0–127) and one kind-7 placement record. |
| Save scope | Observe every placement unit. Then every sanitation unit loses its public bindings (chosen slot pending) and its other KMC bindings (an older Mounted Charge becomes the stock Charge first; the rest are leased back after the save). It also loses its exact KMC auto-use selection and its KMC facts. A disabled service sanitizes and records nothing. The scope's end is an update, then the leases. |
| Area unload / resume | As the save scope, with the other bindings cleared rather than leased. Resume is an update. |
| Selected load | Nothing is moved or cleared. The outgoing world keeps its records, observed units and bars, through a missing-player interval too. A distinct player world releases them; the incoming world seeds from its archive rows. A missing player releases them only when no selected load is open. |
| Main menu / unfenced disposal | No player world and no open load: the records and any open native operation are released, and observed units of the earlier world are cleaned up and dropped at the next update. |
| Disable / dispose | Every sanitation unit loses every exact KMC binding, auto-use selection and fact. Records, observed units, the open operation and the load are dropped. |

**Placement versus sanitation (addendum C3).**
- *Placement eligibility* (who may own an arrangement) covers current riders that are not pets; observed former riders
  of the current world that no other current object of the same actor replaces (rider, reserve or detached); and
  reserve or detached owners. Archive rows naming a resolvable pet own nothing; an unresolved actor keeps its rows.
- *Sanitation coverage* is wider:
  - current riders, including a rider that rejoined before the next update;
  - every observed former rider, including an earlier world's object;
  - reserve and detached owners;
  - the exact current-world persistent pet of every one of them. This includes the pet of a reserve owner that also
    sits in pair custody; that pet is reached from the owner, not from the party.
- Sanitation touches only exact KMC slots, auto-use selections and facts. Ordinary abilities, ordinary auto-use
  selections and equipment are untouched.

**Save format: unchanged.** Row kinds 6 and 7, the 128-row bound and the archive's own validation are kept, with one
kind-6 row per actor. An older archive with several public rows seeds its first kind-6 row whose slot can hold the
control on a fresh load. Older Mount / Dismount rows (kinds 1 and 2) collapse to one control in the same way (F2).

**Contracts kept.** The native contract pins, the Tabletop Rules Expansion click composition (unchanged paths; its
regression runs in both patch orders), stable actor, pair and save identities, and the original §3–§4.

### Decisions that differ from the first enrollment's candidate (stated explicitly)

1. **No load fence.** Records are kept per player world and are not moved at load begin. An abandoned load therefore
   keeps everything, including copies and reserve classification. A replacement or an unfenced disposal releases it.
   While the load is open and no player world exists, the update does nothing at all (review R1-F2).
2. **The `IsBad` postfix is removed.** Native invalidation is observed by bracketing `GetSlot` and `CleanupBadSlots`
   and comparing slot objects. A standalone `IsBad` query records nothing. The `IsBad` contract rows stay pinned.
3. **Reserve and detached bars are observed and swept every update.** This replaces the one-time first scan and its
   ledger.
4. **The save scope does not lease the public control.** Its chosen slot becomes pending and returns through the
   update at the end of the scope. An older alias the scope cleared comes back as the canonical control at its index.

Retracted from writer invocation 1: set semantics, under which the player's duplicate bindings were all kept. Review
round 1 (R1-F1) showed it changed archived regression outcomes. The arrangement is one chosen slot again, with known
copies (above).

**Remaining uncertainty (§4).**
- Native `UnitUISettings.CleanupSlots` (respec) resets the whole bar, as before.
- A native slot replacement outside `GetSlot` and `CleanupBadSlots` would be read as the player's removal; none is
  known in 2.1.7b.
- A rebind completed by a native operation that then threw is not counted.
- If the player moves a copy back to the same index under the same slot object, it is still a copy.
- Every behavior above is component-tested through the production component, and the adapter's wiring through its
  offline fixture. None is observed in game; the native cases are NOT RUN (section 10).

## 2. Animal companions in the city's inventory and character screens

Unchanged in this enrollment (addendum B).

**Established cause.** In a capital, the full-screen Inventory, Character and Spellbook screens use `GroupController`
with `WithRemote` and `WithPet`, that is `UIUtility.GetGroup(withRemote: true, withPet: true)` (token `06004281`).
That call lists every party and reserve companion, in or out of the game, but adds pets only from
`Player.ControllableCharacters`, which requires `IsInGame`. `CapitalCompanionLogic` takes custom companions out of the
game, and their pets go with them. Batch199 row 59 (`3842ad94`) recorded the hired Small Ranger in Oleg's as remote and
out of game, and its Horse out of game.

**Fix.** A bounded UI roster projection: an exact-token Harmony postfix on `UIUtility.GetGroup` delegates to
`CityCompanionRosterAdapter` → `CityCompanionRosterProjection`. It applies only while Mounted Combat is enabled and
only in a capital with both flags. Each listed non-pet owner's own pet is inserted right after it, under native's own
pet rules:
- the link is reciprocal;
- the pet is not a summon and is one of the player's cross-scene characters;
- the owner is not detached and not an ex-companion;
- no pet is listed twice.

The existing persistent pet instance is listed. Nothing changes party membership, ownership, `IsInGame`, scenes,
formation, XP or combat.

## 3. Mounted attacks against prone and downed targets

Unchanged in this enrollment (addendum B), except for this wording.

The identified rejection of attacks against an already-unconscious target is corrected in source. The original prone-target report still needs matched in-game verification.

**Unconscious (downed) enemy: the mechanism and the source fix.**
- Native `UnitAttack` keeps an explicit order on an enemy that was already unconscious when ordered
  (`m_IsUnconsciousAtStartOfCommand`).
- The mounted pair path required a conscious target at every gate.
- `UnifiedMountedStockAttackPolicy.TargetLifeAdmits` mirrors the native rule. `MountedAttackIntent` records
  `TargetUnconsciousWhenOrdered` for a player order only. `MountedPairAttackCommand.AdmitTargetUnconsciousWhenOrdered`
  keeps that exact target valid while it lives.
- Unchanged: automatic proposals, continuation, Charge, a target that falls unconscious after the order, dead targets,
  modifiers, ammunition, costs, line of sight and range, approach.

**Conscious, tripped enemy: traced, with no mounted divergence found in code (R1-F9 of the first enrollment).**
- Clicking the lying body of a conscious prone enemy can miss its colliders. This is native and target-side; for a
  mounted rider, the missed click becomes a pair move.
- Classification, admission, approach, range, line of sight and firing have no prone term.

This is a code trace, not an observation. The reported weapon and the enemy's state are not identifiable from the
available evidence.

## 4. Lifecycle properties through the production path (addendum D)

| Property | Behavioral tests (production component through thin fakes) |
| --- | --- |
| D1 intent recorded before cleanup destroys its native evidence | `IntentBeforeCleanup` (departure, header before departure cleanup, area suspension before the departure loop); `NativeInvalidationVersusRemoval` (move, then native cleanup); `LegacyAliases` (alias cleared while not live; the R19-F1 alias fallback) |
| D2 the same edits give the same result whether saving or updating first | `SameEditsEitherOrder` (six edits on identical games, comparing rows, bars and cold-loaded bars); `LegacyChargeEitherOrder`; `FormerInstanceKeepsReplacement` |
| D3 repeated scans or saves never move or resurrect | `RepeatedBoundariesIdempotent`; `OccupiedAndIndependent`; `KnownCopies` (one shortcut) |
| D4 a canceled load preserves the complete outgoing state | `LoadLifetime`; `MissingPlayerDuringCanceledLoad` (a travelling rider's live 9 through a missing-player update); `AcR26F1CanceledLoadKeepsEdit`; `AcR26F2MainMenuReleases` (fenced: the duplicate keeps yielding) |
| D5 replacement and unfenced disposal release the old world | `AcR26F2MainMenuReleases`; `MissingPlayerDuringCanceledLoad` (replacement control); `DisableAndLifetime`; `LoadLifetime`; wiring check "a native slot operation that threw is released" |
| D6 native invalidation keeps intent; a deliberate removal stays a removal | `NativeInvalidationVersusRemoval`; `NativeOperationEdges`; `NativeRebindOfPendingSlot`; `KnownCopies` (R18-F1 r20–r24) |

## 5. Inherited acceptance cases (addendum F)

Provenance for all six: enrollment `…-e55c`, round 26, reviewed commit `6298f2a5`, archived review
`docs/agent-reviews/pr-57/112-r26-review.md` (envelope `113`). These are acceptance-case references, not findings of
this enrollment. Evidence type: UNIT (the production lifecycle through thin fakes). All six pass in `bin\Tests` and in
the mutation baseline on the reported candidate.

| Case | Outcome | Test | What it exercises |
| --- | --- | --- | --- |
| AC-R26-F4 | PASS | `AcR26F4ReservePetSanitized` (with `PairCustodyOwnerPetSanitized` for review R1-F5) | Separate memberships: the remote owner and its horse. A factless Mount Primary slot and auto-use on the horse are absent inside serialization and after disable; the ordinary ability and another pet's ordinary auto-use survive. The same holds for a remote or detached owner that also sits in pair custody. |
| AC-R22-F3 | PASS | `AcR22F3FreshPlacementAfterNativeEmpty` | A never-observed reserve rider with only slot 3; `GetSlot` empties it. The player places the **current live control** at 11; the test asserts the live identity first. Saving before any scan gives 11 and one record, then one shortcut at 11 on a same-world return and after a cold restore. Untouched: 3. With a known copy at 7: only 3 untouched, only 11 after the placement. |
| AC-R25-F3 | PASS | `AcR25F3FreshShortcutAfterReplacement` | Pet A's 9 is retired; a placement at 11 for B is saved and restored once; without a placement nothing returns, and 9 is never restored. |
| AC-R26-F1 | PASS | `AcR26F1CanceledLoadKeepsEdit` | The reserve sweep cleared 9. The player places the **live** ability at 11 (asserted). A selected load is begun and abandoned before any observation. The immediate save holds 11 and one record; a cold restore gives 11 once. No edit: 9. |
| AC-R26-F2 | PASS | `AcR26F2MainMenuReleases` (and the wiring check for the adapter's reference, review R1-F8) | A recorded duplicate (3 and 7) on a never-observed reserve rider; return to the main menu; an update with no player. No records, world, observed unit, native operation or load remain. Fenced control: the abandoned load saves 3 only, and one shortcut at 3 returns. Real-transition control: empty incoming records. |
| AC-R26-F3 | PASS | `AcR26F3RejoinedRiderAutoUse` | A stale `KMC_RiderPrimary` selection, asserted present, moves from reserve to the party immediately before a save, separately before disable, and in a disabled save. In the disabled save the selection is installed after disabling. It is null inside serialization and afterwards; an ordinary selection survives; the disabled save records nothing. |

## 6. R1–R26 regression map (addendum E)

Every finding of the first enrollment (IDs from the archived envelopes; round 6 raised none), with the test or
subcase in `tests/MountedControlPlacementIntentTests.cs` unless another file is named. "r*N*" marks a later round's
re-raise. *Gone* marks a finding whose mechanism no longer exists (the copy registry, scan ledger or load fence); the
test asserts the behavior the finding protected.

| Finding | Concern | Covered by |
| --- | --- | --- |
| R1-F1 | removal through a reserve save and load | `RemovalSurvivesReserveSaveLoad` |
| R1-F2 | move or removal between update and save scope | `SameEditsEitherOrder` (save-first branch) |
| R1-F3 (r2, r3) | native emptying before re-lease; fact back first; move then cleanup | `NativeInvalidationVersusRemoval` |
| R1-F4 | retained remount replay after a removal | `InWorldReplay` |
| R1-F5 | permanent pet loss in reserve | `CompanionLoss` |
| R1-F6 | city roster after disable | `CityCompanionRosterProjectionTests` (unchanged) |
| R1-F7 | archive bound | `Test-CityArchiveRowBound.ps1` (128 lifecycle rows round-trip; 129 refused by the production validator) |
| R1-F8 | native fixture description | section 10 |
| R1-F9 | conscious prone traced separately | section 3; native NOT RUN (owner playtest 4) |
| R2-F1 (r3, r4) | header during an interruption, stale binding, unresolved pet | `HeaderDuringInterruption` |
| R2-F2 (r3) | edit then departure; header before departure cleanup | `IntentBeforeCleanup` |
| R2-F3 | same pet link, pet unresolved | `CompanionLoss` |
| R2-F4 (r11) | removal right after the save scope returns the slot | `RemovalAfterSaveRelease` (canonical and alias) |
| R2-F5 | adapter facades and matched baseline | `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1`; `Test-CityAreaAdapterBaseline.ps1` |
| R3-F1 (r4) | outgoing world writing into the loaded world | `LoadLifetime` (R3-F1 subcase); `NativeOperationEdges` (R13-F4) — *gone* (fence) |
| R3-F2 | prone playtest negative controls | owner playtest item 5 (restored, review R1-F16) |
| R4-F1 (r5) | removal then brief ineligibility; removal then native deactivation | `NativeInvalidationVersusRemoval` |
| R4-F2 (r5, r6, r7) | permanent loss, direct pet change, cleared link with a native report, change across a transition | `CompanionLoss`; `InWorldReplay` (r7) |
| R4-F3 | area suspension before the departure loop | `IntentBeforeCleanup` |
| R4-F4 | validation claims | section 9 reports only runs of this enrollment; the gate receipt is the result of record |
| R5-F1 (r6) | abandoned load keeps reserve intent, removals, native emptying | `LoadLifetime` — *gone* (fence) |
| R5-F2 | superseded load scope | `LoadLifetime` |
| R7-F1 | older capital save's reserve binding | `ReserveBars` |
| R8-F1 | older reserve bar: canonical over alias; Mounted Charge to stock Charge (first update or save) | `ReserveBars`; `LegacyChargeEitherOrder` (save first and update first); wiring check "save first … Mounted Charge" |
| R8-F2 | faulted rider, empty link, native cleanup | `CompanionLoss` |
| R9-F1 | disable before or after partial registration | `UnregisteredBlueprints`: `Disable` with nothing registered and with only the public control registered; ordinary slot objects keep their identity, a positive control is cleaned once identifiable |
| R9-F2 (r10) | companion replaced during the save scope | `CompanionChangeDuringSave` |
| R9-F3 | scan bookkeeping lifetime | `DisableAndLifetime` — *gone* (ledger) |
| R9-F4 (r10) | older alias while the pet is unresolved | `LegacyAliases` |
| R10-F1 (r11) | header before repair with a live alias | `LegacyAliases`; archive check "an older alias at 9" |
| R11-F1 | alias beside a canonical binding, saved first | `LegacyAliases`; archive check "alias at 3 beside canonical 9" |
| R12-F1 | stale and current canonical bindings at save | `KnownCopies` (save first: 3) |
| R12-F2 | alias the scope returned, second save | `LegacyAliases`; `RemovalAfterSaveRelease` |
| R12-F3 | pet change clears canonical and alias | `CompanionLoss` |
| R12-F4 (r13) | native rebind then removal | `NativeInvalidationVersusRemoval`; `NativeRebindOfPendingSlot` |
| R13-F1 | returned alias emptied by `GetSlot` | `LegacyAliases` (the scope returns the canonical control at the alias index) |
| R13-F2 | save before the first update, no autofill copy | `InitialPlacement` |
| R13-F3 | remembered slot occupied by another KMC binding | `HeaderDuringInterruption`; archive check "occupied by an older Mounted Charge" |
| R13-F4 | aborted native operation across a world replacement | `NativeOperationEdges` |
| R14-F1 | manual placement emptied, then filled | `NativeOperationEdges` |
| R14-F2 (r15, r16, r17) | canonical 4 and alias 9 under cleanup, save, update, alias rebind | `LegacyAliases` (four variants) |
| R14-F3 | occupied at save, occupant cleared later | `HeaderDuringInterruption` |
| R14-F4 | disabled, then another world loaded | `DisableAndLifetime` |
| R14-F5 | native rebind of an interrupted slot during an open load, abandoned, then removal | `NativeRebindOfPendingSlot` (one sequence, with the native-emptying control) |
| R15-F1 (r16–r19) | copies emptied, saved, re-placed | `KnownCopies` (both emptied, only 3 emptied, stale 3 current 9, r18, r19) |
| R15-F2 | disabled save of an older surviving shortcut | `DisableAndLifetime` |
| R16-F1 | automatic placement, then a change of companions | `CompanionLoss` |
| R17-F1 (r18, r19) | filled chosen slot with a surviving copy or alias | `KnownCopies` (R17-F1 canonical and alias variants); r19 by the absence of a fence (`LoadLifetime`) |
| R17-F2 (r18, r19, r20) | validity query, throws before and after the write, header after a throw | `NativeOperationEdges` |
| R17-F3 (r18) | retirement under a fence and in a cleanup batch | `CompanionLoss` (r6) — *gone* (fence) |
| R18-F1 (r19–r24) | emptied chosen slot with copies; rebinds; removals | `KnownCopies` (R18-F1, r19, r20/r21, r22–r24) |
| R18-F2 | emptying of a bad alias not credited to a removed alias | `LegacyAliases` |
| R18-F3 | combat harness liveness assertion | `Test-CityTargetLiveness.ps1` |
| R19-F1 | KMC migration before native reads the alias; alias fallback for a filled slot | `LegacyAliases` (update-first variant; R19-F1 / review R1-F4 subcase) |
| R19-F2 (r20, r21, r22) | move past a surviving copy, rebind, re-invalidation | `MoveSupersedesPending` (only 11) |
| R19-F3 | newer placement invalidated before reconcile | `NativeOperationEdges` |
| R20-F1 (r21, r22) | copy registry lifetime | `DisableAndLifetime`; `AcR26F2MainMenuReleases` |
| R21-F1 | update-first stale and current canonical bindings | `KnownCopies` (one callable control at 3) |
| R21-F2 | disabled save, then another world | `DisableAndLifetime` |
| R21-F3 | evidence labels | section 9 evidence types |
| R22-F1 | filled 3, copies at 7 and 9, save before any update | `KnownCopies` (7, live and cold) |
| R22-F2 | older rows 3 and 9, 3 occupied at restore | `KnownCopies` (9 only) |
| R22-F3 (r23–r26) | first reserve scan after native emptying | `ReserveBars` (occupied → 9, moved → 11, removed → record, free and moved → 11); `AcR22F3FreshPlacementAfterNativeEmpty` (r26) |
| R23-F1 | a callable copy survives a stale-copy emptying | `KnownCopies` (7) |
| R23-F2 | main menu, then a new game | `DisableAndLifetime` |
| R23-F3 (r24, r25) | a moved callable alias, with a copy, pet unavailable | `MoveSupersedesPending` (one control at 11) |
| R24-F1 | temporary KMC facts on a reserve rider | `ReserveBars` |
| R24-F2 | the incoming world's first native report | `LoadLifetime` (through archive seeding, update, save and B's return) |
| R24-F3 (r25) | missing player during a load, abandoned | `LoadLifetime`; `MissingPlayerDuringCanceledLoad`; `AcR26F2MainMenuReleases` |
| R25-F1 | a never-scanned reserve rider stays in reserve through another pair's area transfer | `InWorldReplay` (capture, suspension and replay with the rider in reserve; then a save and its return) |
| R25-F2 | reserve rider's managed auto-use | `DisableAndLifetime` |
| R25-F3 (r26) | fresh placement on a scanned reserve bar; after retirement | `ReserveBars`; `AcR25F3FreshShortcutAfterReplacement` |
| R26-F1 … R26-F4 | the inherited acceptance cases | section 5 |

## 7. Production mutations (addendum E; review R1-F14)

`scripts/Test-ToolbarLifecycleMutations.ps1`, the seventh child of the focused wrapper, runs two campaigns in fresh
temp directories:
- **lifecycle:** the production component with its behavioral tests;
- **wiring:** the actual adapter file `NativeMountedControlService.Toolbar.cs`, the component, and the wiring fixture.
  The fixture's Kingmaker types and the service's main-file members are labeled substitutes; the main file's `Update`
  order is mirrored.

Each unmutated copy must pass every check. Each mutation edits one production line in the temp copy and must make
every named check fail. Afterwards the script confirms the worktree sources are byte-identical; no mutated code is in
any commit.

Results on `56366c41` (gate log `gate-focused-56366c41f337-…-20261122T031113243294Z.log`): baselines lifecycle 36/0
and wiring 5/0; all 16 mutations killed.

| Mutation (production) | Class | Named check(s) that failed (total failing) |
| --- | --- | --- |
| M1a no observation before a sweep | pre-cleanup observation | D1 (2) |
| M1b no observation before a native write is applied | pre-cleanup observation | D6, AC-R22-F3 (9) |
| M2a persistent pets omitted from sanitation | omitted actor | AC-R26-F4, review R1-F5 (2) |
| M2b reserve owners omitted from observation | omitted actor | reserve bars, AC-R22-F3 (8) |
| M3a abandonment releases the outgoing records | world lifetime | D4/D5, AC-R26-F1, AC-R26-F2 (4) |
| M3b a replaced world keeps the outgoing records | world lifetime | AC-R26-F2 (2) |
| M3c no release without a world (main menu) | world lifetime | AC-R26-F2, disable and lifetime (2) |
| M3d a missing player during an open load treated as disposal | world lifetime | review R1-F2 (1) |
| M4a a player's null write after an aborted operation counts as native | edit versus invalidation | D6 native operations (1) |
| M4b a native emptying leaves nothing pending | edit versus invalidation | D6, AC-R22-F3, save header (12) |
| M4c a player's removal kept as owed back (`ApplyBar`) | edit versus invalidation | removal survives, D6 (11) |
| W1 the service stops forwarding the save scope | adapter wiring | wiring: pair custody and detached owners (2) |
| W2 the roster adapter omits the detached roster | adapter roster input | wiring: pair custody and detached owners (1) |
| W3 the native slot prefix stops forwarding | adapter wiring | wiring: native `GetSlot` brackets (2) |
| W4 the host returns no persistent pet | adapter roster input | wiring: pair custody and detached owners (1) |
| W5 the host reports no reserve or detached owners | adapter roster input | wiring: pair custody and detached owners (2) |

The wiring fixture's checks drive the real adapter:
- a remote owner in pair custody and a detached owner: save capture, save scope and disable;
- the native `GetSlot` bracket: emptying returns once, and a removal stays;
- a thrown native operation released by the main menu and by disable, in service and lifecycle (R1-F8);
- save-first stock Charge migration (R1-F6);
- pet archive rows owning nothing (R1-F11).

The patch methods in `MountedPatchController` that call the bracket are one-line forwarders and are not in the
fixture. The main file's `SetEnabled`, `Update` and `Dispose` are mirrored, not compiled.

## 8. Review round 1 (this enrollment): dispositions

| Finding | Disposition | Change and evidence |
| --- | --- | --- |
| R1-F1 known duplicates became extra shortcuts | fixed | One chosen slot with known copies by slot object (`ApplyBar`); one shortcut while live. `KnownCopies`, `MoveSupersedesPending`, `AcR22F3…` and the archive check restore the archived outcomes. The duplicate stays on the bar through the R18-F1 removal subcases. |
| R1-F2 missing player during a canceled load | fixed | `Reconcile` does nothing while a selected load is open and no player world exists. `MissingPlayerDuringCanceledLoad` covers it, with a replacement control; M3d is killed. |
| R1-F3 native rebind left the slot pending | fixed | A rebound chosen slot is live; a rebound copy keeps copy identity. `NativeRebindOfPendingSlot` runs the R14-F5 sequence with a native-emptying control. |
| R1-F4 legacy alias fallback lost | fixed | Aliases on the bar are copies, and the first surviving copy is the fallback when the player fills the pending slot. `LegacyAliases` R19-F1 subcase. |
| R1-F5 pet hidden by pair custody | fixed | Sanitation adds the persistent pet of every swept unit. `PairCustodyOwnerPetSanitized` (remote and detached; save and disable); wiring check; M2a and W4 are killed. |
| R1-F6 save first lost the legacy Charge | fixed | The adapter migrates an older Mounted Charge to the stock Charge before any sweep destroys it. `LegacyChargeEitherOrder`; wiring check (W1 and W5 also fail it). |
| R1-F7 departure skipped auto-use | fixed | Departure clears the exact KMC auto-use. `DepartureClearsAutoUse` (rider and former pet, with an ordinary control). |
| R1-F8 adapter held the thrown operation's unit | fixed | The adapter field is removed; the lifecycle is the operation's only owner (`NativeOperationUnit`). Wiring check: main menu and disable release it in service and lifecycle. |
| R1-F9 unavailable bar consumed the restore | fixed | `RestorePending` defers when the bar is unavailable. `UnavailableBarDefersRestore`. |
| R1-F10 former instance retired its reserve replacement | fixed | `OwnsRecords` yields to any current rider, reserve or detached object of the same actor. `FormerInstanceKeepsReplacement`, save first and update first. |
| R1-F11 archive rows on pets | fixed | Seeding skips a resolvable pet, and capture writes no row for one. `PetRowsOwnNothing`; wiring check. |
| R1-F12 three inherited legs ran on different fixture state | fixed | The disabled-save leg installs the selection after disabling and asserts it. The AC-R22-F3 and AC-R26-F1 edits place the current live control and assert its identity. |
| R1-F13 map entries stopped short | fixed | R25-F1 keeps the rider in reserve through capture and suspension. R24-F2 continues through seeding, update, save and return. R9-F1 runs `Disable` through the production classifier with ordinary slot objects. |
| R1-F14 no wiring mutation | fixed | Wiring fixture over the actual adapter file and five wiring mutations (section 7). |
| R1-F15 two offline probes broken by the split | fixed | Details below. |
| R1-F16 deferred combat playtest lost controls | fixed | Section 3 carries the exact two-sentence status. Section 11 restores the concrete melee, dismounted ranged and expenditure steps, including when ammunition is observable. |

R1-F15 in detail:
- `Test-Finalize2UpgradeReaders.ps1` reads the capture from the toolbar partial. On `56366c41` its four
  removal-safeguard checks and every synthetic producer, consumer and bridge check passed. It then stopped at the
  already-known missing r05 retained evidence (T09), independently of this change.
- `Test-MountedPresentationContracts.ps1` creates the lifecycle on its blank service. Its U02 section and the whole
  probe passed (239/0) on `56366c41`.

## 9. Validation (offline, this enrollment)

Commands under `docs/VALIDATION-POLICY.md`: a focused development checkpoint, with no Candidate, Full or native
campaign selected.

**Writer invocation 1:**
- Local Fast runs, one with a temporary diagnostic edit to `tests/Program.cs` that was reverted and is absent from the
  diff.
- Controller gate FAIL on `45e799ce`: M4c survived its named test.
- Controller gate PASS on `63d35c13` and on `83fdf493`, the round-1 candidate.

**Writer invocation 2:**
- `dotnet build` of the product and of the test project (development compiles).
- Controller `focused` gate (`-Tier Fast -Focused Test-CityToolbarProneFocused.ps1`):
  - FAIL on `9ecc4a77` (47.4 s). The wiring baseline did not compile, and the compiler output was swallowed inside the
    script function. Everything else passed: components 1456/0 and lifecycle baseline 36/0.
  - PASS on `56366c41` (33.1 s).
- Gate runs with a single probe each, on `56366c41`:
  - `-Focused Test-MountedPresentationContracts.ps1`: PASS, 15.3 s.
  - `-Focused Test-Finalize2UpgradeReaders.ps1`: FAIL, 16.8 s, only at the missing r05 evidence (section 8).
- The gate on the reported candidate commit is the result of record.

| Check (gate run on `56366c41`) | Result | Limits |
| --- | --- | --- |
| build | PASS | Release |
| components (`bin\Tests`) | 1456/0 | 36 toolbar, 6 city-roster, 3 downed-target tests |
| Validate-Source | 153/0 | source and payload rules |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 825/0 | reflection-only; Wrath not selected |
| `Test-GunslingerClickComposition.ps1` | 21/0, both patch orders | substituted native execution |
| `Test-CityArchiveRowBound.ps1` | 7/0 | lifecycle rows through the production archive DTO and codec |
| `chunk7\Test-Chunk7SixPairArchiveAdapter.ps1` | 22/0 | actual capture and restore methods, typed facades |
| `Test-CityAreaAdapterBaseline.ps1` | PASS (63/63, none only in either) | compile step only |
| `Test-CityTargetLiveness.ps1` | 3/0 | source checks |
| `Test-ToolbarLifecycleMutations.ps1` | PASS: lifecycle 36/0, wiring 5/0, 16/16 mutations killed | temp copies; worktree unchanged |

Workflow budget (development): 62.4 of 90 minutes spent after invocation 1, and 65.1 after the R1-F15 probe runs.

## 10. Native checks: what exists and what each case needs

No native case ran in this enrollment. The guarded runner (`scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1`)
needs a candidate package bound to the commit, and no command available to this enrollment builds that package.
Per-case prerequisites (unchanged):
- **Capital arrival and cold load:** the capital route's reader (`scripts/runtime/Chunk7CapitalArrivalEvidence.ps1`)
  predates F2 and needs a versioned revision for kind-6 and kind-7 rows. Its lineage needed eight launches in batch199.
  The batch199 r58/r59 capital fixture exists; its verdicts belong to that binary.
- **Reserve pet selection:** no driver opens the full-screen group panel and selects a portrait.
- **Prone legs:** an enemy made prone or unconscious before the click needs new diagnostic setup and reader rows.
- **F2 shortcut chain:** its retained r05 evidence is absent here (T09).

## 11. Owner playtest (exact steps)

1. **Nondefault slot through transitions.** Use the main character or a companion with a Horse.
   - Put Mount / Dismount in a non-default slot, for example slot 9.
   - Travel to the capital, walk to an interior and back, leave, and return to adventuring. The shortcut stays in
     slot 9.
   - Repeat while mounted.
   - Save and load inside the capital and outside it; the slot holds.
2. **Removals, moves and occupied slots.**
   - Remove the shortcut from the bar, visit the capital, save there, load and leave: it does not come back.
   - Move it to another slot and save at once, then load: it is in the new slot only.
   - Put another ability where it was before a visit: that ability is not overwritten.
   - Drag a second copy of the control onto the bar while it is usable: one shortcut remains.
   - If an older capital save shows Mount / Dismount on a reserve companion's bar: load it, save at once in the
     capital, reload, and bring that companion into the party. The shortcut comes back once, in the same slot.
   - With a second rider, release or replace its Horse just before an area transition (while another pair is
     mounted): the old slot stays empty for the new companion.
3. **City companion access.** Take a mercenary or custom companion with a Horse into the capital.
   - Open Inventory (I) and Character (C). The Horse appears after its owner.
   - Select it and check its name, statistics and barding and neck slots. Equip and unequip an item.
   - Close and reopen, then save, load and reopen. No portrait is duplicated.
   - Disable Mounted Combat in UMM: the Horse is no longer listed. Re-enable it: the Horse is listed once.

   **Combat setup S (items 4 and 5).** The rider is a single-attack longbow user: base attack bonus below +6, no
   Rapid Shot, Manyshot or haste effect, and no other extra-attack source, so one attack action is one roll.
   - **Fallback (extra attacks).** If the only available rider has extra attacks, keep them and use the matched
     control instead. The mounted routine must have the same number of rolls and the same action cost as the same
     rider's unmounted routine in the same mode, position and target. A legal native full attack then passes; an
     additional routine or a second action spend fails.
   - **Turn based.** Read the action bar's spent actions after each click. The routine ends when the action bar shows
     the action spent and no further roll follows before you act again.
   - **Real time: counting window.** Use the same window for every run, mounted and dismounted alike:
     1. Start the game paused, click, then unpause.
     2. Note the game time of the first roll in the log.
     3. Let the game run until about 5.5 game seconds after that first roll. That is just under one real-time round
        of 6 seconds, so the routine of that round falls inside the window and the next automatic round's first roll
        does not.
     4. Pause and count the rolls logged inside the window. Rolls after it belong to the next automatic round and are
        not counted.
   - **Pass:** under setup S, exactly one roll in the window; with the fallback, the same number of rolls as the
     dismounted run's window.
   - **Fail:** one roll more or fewer than the matched count inside the window, including a delayed extra roll after
     the first.
   - **Ammunition.** Kingmaker 2.1.7b does not track arrows for an ordinary bow. If your setup shows an ammunition
     count (from another mod), it must drop by exactly the number of rolls; otherwise ammunition is not compared.
4. **Prone and unconscious targets, mounted and dismounted.** Use setup S, at about 10 ft and about 40 ft, in real
   time and turn based. Compare:
   - (a) a standing enemy;
   - (b) a conscious prone enemy, for example tripped: click its standing footprint and, separately, its lying body;
   - (c) an already-unconscious enemy, clicked after it fell.

   Run every case mounted, and again dismounted with the same weapon and positions.
   - **Pass:** an actual attack roll appears in the combat log in each case (one under setup S), the horse never walks
     into melee for the shot, and the mounted and dismounted routines match.
   - Note any case where the click becomes a move instead of a shot. This is the original prone-target report and is
     still unverified in game.
5. **Negative controls and neighbours.** All are NOT RUN in this enrollment. Use the setup-S rider mounted, with a
   longbow equipped, against one hostile enemy. The combat log shows each attack roll with its modifiers (hover the
   roll).
   - **Wall:** place the pair so a solid wall stands between the rider and a standing enemy within bow range, with no
     line of sight. Click the enemy.
     - Pass: never a roll through the wall. Either an ordinary approach until line of sight exists, followed by the
       setup-S routine, or no attack and no action spent.
     - Fail: a roll while the wall still blocks the line.
   - **Spent action:** in turn-based mode, use the rider's Standard action on a first shot, then click the enemy again.
     - Pass: no second roll, a "no Standard action" refusal (KMC's text is "… has no Standard action left this turn"),
       and the horse's own actions unchanged.
     - Fail: a second roll in the same turn.
   - **Beyond firing range:** start farther from the enemy than the longbow reaches, and click it.
     - Pass: the ordinary approach (the horse moves toward the target with the pair's own movement), and at most the
       setup-S routine once in range. In turn-based mode, if the movement runs out first, there is no roll and no
       Standard action spent. In real time, once in range, count with the setup-S window; if no roll occurs within 12
       game seconds of the click, pause and note it.
     - Fail: a roll from out of range, or an action spent without a shot.
   - **Mounted melee:** switch the rider to a single-attack melee weapon and click an adjacent standing enemy.
     - Pass: the rider's melee routine and action cost are as on 0.2.13 with the same rider and positions. The horse
       attacks only on its own opportunity, as before.
     - Fail: no roll, or a routine or action spend that 0.2.13 does not show.
   - **Unmounted ranged:** dismount and repeat the near standing shot, with the same rider, weapon and position.
     - Pass: the setup-S routine (one roll in turn based, or one roll in the real-time window), as on 0.2.13.
     - Fail: no roll, or a different count or action cost from 0.2.13.
   - **Penalties and expenditure:** compare the hover breakdown of a mounted ranged roll on 0.2.13 and on this build,
     at the same distance and movement.
     - The same mounted ranged penalty lines appear; this change does not touch them.
     - Against a prone target the native prone modifier line appears.
     - The turn-based action the routine spends (Standard, or full round for a full attack) is spent exactly once.
     - Ammunition is checked as in setup S.
     - The unconscious enemy stays prone, and no extra attack is granted.

Restoration: no native transaction ran.
