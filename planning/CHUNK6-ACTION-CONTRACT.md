# Chunk 6 action contract

Single source of truth for the action economy, native command ownership and
adaptation status of every remaining Chunk 6 combat feature. Only **combat
Mount** and **combat Dismount** are implemented in Chunk 6A; every other row is
a bounded seam/adaptation contract recorded so the later missions do not repeat
basic forensics.

Exact installed contract for every claim below: `Assembly-CSharp.dll` SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Required configuration:
`EnablePairedActivation=true`, `EnableUnifiedMountedTurn=false`,
`EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`, one
supported pair, Horse and Mammoth profiles, C# 7.3 / .NET Framework 4.7 /
Harmony12.

## The exact native cost machinery (measured, applies to every row)

`UnitActionController.TickCommand` `0x0600911E` is the sole resource-commitment
site. At `IL_00AD`-`IL_00C5` it pushes `UnitCommand.IsActed` `0x06002763`,
calls `UnitCommand.Tick` `0x060027A7`, and calls
`UnitActionController.UpdateCooldowns` `0x06009120` **only** on the
false-to-true transition, and only while `IsRunning` `0x06002762`.
`UnitCommand.Tick` sets `IsActed` at `IL_0181` immediately after `OnAction`
`0x060027B1` returns.

`UnitActionController.UpdateCooldowns` `0x06009120` then branches:

| Condition | Effect on the executor |
|---|---|
| `CombatController.IsInTurnBasedCombat()` `0x06000BF6` and `Executor.IsInCombat` `0x060082F1` | delegates to `UnitEntityData.UpdateCooldowns` `0x0600838F`, which is **additive**: `Move` adds `3` to `Cooldowns.MoveAction` `0x0600C3B9`, `Standard` adds `6` (plus `3` Move when `IsFullRoundAction` `0x06000BA2`), `Swift` adds `6`, `Free` adds nothing |
| real time, `Executor.IsInCombat` true, `IsIgnoreCooldown` `0x06002772` false | **absolute** assignment: `Move` and, notably, `Free` both set `MoveAction = 3 - TimeSinceStart`; `Standard` sets `StandardAction = 6 - TimeSinceStart`; `Swift` sets `SwiftAction = 6 - TimeSinceStart` |
| `Executor.IsInCombat` false (out of combat) | returns without writing anything |
| real time and `IsIgnoreCooldown` true | returns without writing anything |

`CommandType` `0x02001A79` is `Free=0`, `Standard=1`, `Swift=2`, `Move=3`.
`UnitEntityData.HasMoveAction` `0x0600837F` is
`UsedStandardAction() ? !UsedOneMoveAction() : !UsedTwoMoveAction()`, where
`UsedOneMoveAction` `0x06008382` is `MoveAction > 0 || IsMoveActionRestricted()`
and `UsedTwoMoveAction` `0x06008383` is
`MoveAction > (IsMoveActionRestricted() ? 0 : 3)`.

Two consequences the whole chunk depends on. First, **the out-of-combat
Mount/Dismount transition is free because native code charges nothing outside
combat** — KMC adds no exception for it. Second, **the commitment boundary is
the acted transition**, which is strictly after approach
(`UnitCommand.TickApproaching` `0x060027A6` runs before `Start` `0x060027A5` in
`TickCommand` at `IL_0077`-`IL_0087`) and strictly before KMC's custom delivery
(`AbilityCustomLogic.Deliver`, reached from the `AbilityExecutionProcess`
created inside `UnitUseAbility.OnAction` `0x06002737`). Cancelling or
interrupting before the acted transition costs nothing; after it the cost
stands and KMC must never refund it.

## The transition-round participation rule (frozen from exact evidence)

`TurnController.Prepare` `0x06000C3C` **is** the per-round grant. It calls
`Cooldowns.Clear` `0x0600C3BE` at `IL_0032`, restores cooldowns only for
commands still live, resets `AttackOfOpportunityCount` `0x06009378`, calls
`UnitCombatState.OnNewRound` `0x0600939D`, raises
`IUnitNewCombatRoundHandler`, calls `CombatAiData.TickRound` `0x06001E0B`, and
ticks every `ITickEachRound` fact component. A second `Prepare` for the same
actor in the same round would therefore refund all spent debt and replay round
effects. **No relationship transition may call it.**

`CombatController.ChooseNextUnit` `0x06000BD2` is a strictly **positional**
round-robin over `m_Units` `0x0400064F`: it finds the index of
`CurrentTurn?.Unit ?? m_NextUnit` (`<ChooseNextUnit>b__63_0` `0x06000C03`),
walks forward with wraparound, and calls `StartRound` `0x06000BD3` exactly at
the wrap point. `StartRound` is the only in-round re-sort
(`SortUnits` `0x06000BDD`); `Tick` `0x06000BD1` consumes `m_IsUnitsChanged`
without re-sorting. Therefore, during the rider's own turn, a unit earlier in
`m_Units` has already taken its slot this round and a unit later in `m_Units`
has not — and that index comparison is the exact, unambiguous observable for
the mount's transition-round disposition.

`CombatController.HandleCombatStart` `0x06000BE2` is reached only from
`Reset` `0x06000BDB`, itself reached only from `Enable` `0x06000BE9`,
`Disable` `0x06000BEA` and `HandlePartyCombatStateChanged` `0x06000BED`. **No
native hook fires when a pair forms mid-encounter**, so Chunk 6A adds its own
explicit adoption boundary rather than re-entering encounter-start code.

The frozen rule, implemented as one explicit typed adoption operation:

1. The relationship, presentation, transport authority and control leases form
   **immediately** on a successful combat Mount. Nothing is deferred there.
2. Paired activation ownership is created at that same instant, but its
   per-round grant is **adopted, never prepared**. The rider's running native
   turn becomes the activation boundary through pure KMC bookkeeping
   (`Begin`/`BeginActorPreparation`/`FinishActorPreparation` call no native
   method); the rider's real native `Prepare` already happened at its own slot
   and is not repeated.
3. The mount's disposition is decided from the positional observable above:
   - **partner slot pending** (mount later in `m_Units` than the rider, and not
     yet prepared this round): the mount is prepared exactly once, as the
     paired partner, through the same private `TurnController` the accepted
     pre-combat path uses, and its own later slot is suppressed. Total for the
     round: one preparation, one participation — identical to accepted
     pre-combat paired semantics, which already relocate the mount's
     participation to the rider's initiative position.
   - **partner slot spent** (mount earlier in `m_Units`, or already prepared,
     acted or ended this round): **no** partner context and **no** native
     preparation. The mount's ActorState is granted, marked prepared and
     immediately ended as bookkeeping, with its current debt observed as the
     baseline. Its round participation stays exactly where it already happened.
4. Either way the activation finalizes at the rider's native turn `End`
   `0x06000C46` exactly as an ordinary paired round does, and the next round
   begins with an ordinary `Begin` at the rider's native `Prepare`.
5. `PairedNativeReadiness` keeps the pair's next-round readiness at
   `max(rider, mount)` native readiness, so adoption cannot pull the following
   round earlier.

No actor receives more than one preparation or more than one lawful
participation opportunity in the transition round; no actor silently loses
already-spent debt; no fresh grant, cooldown write or initiative change is
performed by the transition.

### Named limitations of the adoption dispositions

Both are deliberate and neither affects accounting.

**No paired participation in the retain disposition's own round.** The `partner
slot spent` disposition creates no private partner `TurnController` and records
the mount's allocation as granted, prepared and ended. Because `CanAddressActor`
requires a non-ended actor, the mount cannot be addressed, selected as the pair's
acting partner, admitted for a paired native command, or moved as the pair's mover
for the remainder of the rider's adopted boundary.

That is the correct reading of Kingmaker's own rules rather than a lost resource.
This disposition fires only when the mount's roster slot is *earlier* than the
rider's, which means its turn in this round has already ended, and an actor whose
turn has ended cannot act again until its next turn. The mount's native cooldowns
are untouched and every observed debt is retained; what is withheld is a second
participation it had already relinquished. Ordinary paired movement and
presentation resume at the next round's `Begin`, where the partner is prepared
normally. The rider, meanwhile, has just spent its own Move on the transition.

Before this correction the pair could still address and move that partner, which
handed out an action opportunity the engine had already closed. The contract above
always said "granted, marked prepared and immediately ended"; the code did not,
and R1 repaired the code to match its own frozen contract.

**Re-mounting after a voluntary combat Dismount within one round.** A voluntary
Dismount splits the activation and `splitReleaseRound` governs the partner's
participation until the next native round. A second voluntary combat Mount is
therefore refused until that release round has passed, with the reason "The
pair's previous mounted participation is still resolving this round." Allowing it
would mean layering a second activation over a split one whose participation
disposition is still open.

## Action table

Legend for **6A status**: `IMPLEMENTATION CANDIDATE - NATIVE QUALIFICATION BLOCKED`
= built, gated offline and published, with every mandatory native case still
BLOCKED in `docs/chunk6a-ledger.json`; `IMPLEMENTED` is reserved for built AND
qualified and is not claimed by any row below;
`MAPPED` = seam contract only, no code; `LATER` = deferred with its mission tag.

### 1. Combat Mount — IMPLEMENTATION CANDIDATE, NATIVE QUALIFICATION BLOCKED (6A)

| Facet | Contract |
|---|---|
| Controlling actor(s) | rider only; the mount is the exact target |
| Native surface | `KMC_MountCompanionAbility` `f053faad986631688defa003cd7bda0e`, `BlueprintAbility.ActionType = CommandType.Move`, `AbilityRange.Unlimited`, `CanTargetFriends`; `NativeMountedAbilityLogic` supplies `IsAvailableFor`/`CanTarget`/`IsAbilityVisible`/`Deliver`; command is a stock `UnitUseAbility` from `CreateCastCommand` `0x06002725` |
| RT cost owner | native `UpdateCooldowns` RT branch: rider `MoveAction = 3 - TimeSinceStart` |
| TB cost owner | native `UpdateCooldowns` TB branch: rider `MoveAction += 3` |
| Prediction | `MountedPlayerActionEvaluator.Evaluate` through `GetNativeMountAvailability`; side-effect-free, no cooldown or state write |
| Admission | explicit `MountedRelationshipAdmission.VoluntaryCombat` mode; every ownership/body/life/control/view/agent/mode/generation check retained |
| Commit | the acted transition described above, inside the native Move shell |
| Delivery | `Deliver` → `TryDispatch` → `TryExecuteNativeMount`; revalidates identity, target, adjacency, generation and turn; the admitted-shell context suppresses **only** the now-stale `RiderHasMoveAction` predicate |
| Target/path/range | real geometry; approach radius clamped **down** only, by `CombatMountDismountPolicy.TryGetMountApproachRadius` through `UnitCommand.set_ApproachRadius` `0x06002767`; no teleport, no enlarged reach |
| Interruption | before the acted transition: no cost, no transition. After it: the cost stands, the transition still revalidates and may legitimately refuse; no refund |
| Paired participation | the transition-round rule above; one transition ledger entry makes repeated delivery idempotent |
| Save/load | settled transitions persist normally; a save requested while a voluntary transition is unsettled is truthfully deferred: `MountedPersistenceService.SaveEffectsReady` queries both `NativeMountedControlService.HasUnsettledRelationshipTransition` (the `MountedTransitionLedger` admit-to-settle window) and `OwnsUnsettledRelationshipShell` (a registered Mount/Dismount shell that has not finished, including one still queued), so the transition is never captured mid-flight. No schema change. The CM07 rows remain mandatory native work and are not claimed from this source reasoning |
| Player-facing | native Mount Companion in the abilities drawer; disabled reason names the exact failing gate |

### 2. Combat Dismount — IMPLEMENTATION CANDIDATE, NATIVE QUALIFICATION BLOCKED (6A)

| Facet | Contract |
|---|---|
| Controlling actor(s) | rider only; target is the rider itself |
| Native surface | `KMC_DismountAbility` `3af2b81f4d72bbb30501fa730fcdf36e`, `CommandType.Move`, `AbilityRange.Personal`, `CanTargetSelf` |
| RT / TB cost owner | identical to combat Mount; the rider's native Move shell, once |
| Prediction / admission / commit / delivery | as combat Mount, through `GetNativeDismountAvailability` and `TryExecuteNativeDismount`; `CleanupTrigger.Manual` marks the voluntary path |
| Target revalidation | exact mounted rider identity and relationship generation; no adjacency requirement |
| Interruption | as combat Mount |
| Paired participation | voluntary Dismount splits the activation through the accepted `SplitPairedActivation` path: existing debt retained, no immediate independent mount turn, the mount's separate participation resumes at the next native round (`splitReleaseRound`) |
| Forced detach | death, incapacitation, invalid pair, area/session/lifecycle cleanup, disable/removal, restoration failure and exception use cleanup triggers, pay **no** voluntary Move cost, and remain idempotent |
| Save/load | as combat Mount |
| Player-facing | native Dismount on the mounted rider |

### 3. Mounted Charge — LATER (6B); safety boundary preserved in 6A

| Facet | Contract |
|---|---|
| Controlling actor | would be the rider, delivered by the mount |
| Native surface | `AbilityCustomCharge` type `0x02000598`: `CanTarget` `0x06002BBD`, `Deliver` `0x06002BB6`, `RuntimeRoutine` `0x06002BB8`, `TurnBasesRoutine` `0x06002BB7`, `IsEngageUnit` `0x06002BB5`, `GetMinRangeMeters` `0x06002BBA`/`0x06002BBB`, `GetMaxRangeMeters` `0x06002BBC`. Charge owns a forced path plus a queued native `UnitAttack`, and stamps `RuleAttackWithWeapon.IsCharge` `0x06007186` |
| Cost owner | the enclosing Standard/full-round ability shell; unchanged |
| 6A obligation | **rejected at availability, targeting, click, admission and execution while mounted, before any movement or resource expenditure**, by `MountedChargeSafetyPolicy`/`MountedChargeSafetyService`. Ordinary unmounted Charge passes through untouched. Ordinary approach-and-attack is not Charge |
| 6B problem statement | the mount must own the forced path and the queued attack while the rider owns the Standard action, and `IsEngageUnit` must not strand the pair mid-path |

### 4. Ordinary mounted spellcasting — MAPPED (6C)

| Facet | Contract |
|---|---|
| Controlling actor | rider |
| Native surface | stock `UnitUseAbility` with `CommandType` taken from the spell's own `BlueprintAbility.ActionType`; `AbilityData.Spend` `0x06002B60` inside `OnAction`; `RuleCastSpell` creates the `AbilityExecutionProcess` (`get_ExecutionProcess` `0x06002714`, `IsEnded` `0x06008FD1`) |
| Cost owner | native, per the spell's own action type; KMC adds nothing |
| Open question for 6C | concentration (`MakeConcentrationCheckIfCastingIsDifficult` `0x0600273A`, `TryCastingDefensively` `0x0600273B`) while the mount is the physical mover, and whether carried motion must suppress `DontWaitForHands` `0x06002720` |
| 6A obligation | none beyond not regressing it; casting is neither enabled nor blocked by 6A |

### 5. Mounted item use — MAPPED (6C)

| Facet | Contract |
|---|---|
| Controlling actor | rider |
| Native surface | item activation reaches the same `UnitUseAbility` shell through the item's ability; `ItemEntity.IsSpendCharges` `0x06007B4B` is consumed inside `OnAction` |
| Cost owner | native, per the item ability's action type |
| 6A obligation | none |

### 6. Staged move–cast–move — IMPLEMENTED, SAME-ACTIVATION CONTRACT QUALIFIED (6D)

| Facet | Contract |
|---|---|
| Controlling actors | rider (action) and mount (both movement legs) |
| Native surface | `TurnController.GetRemainingMovementRange` `0x06000C59` / `GetRemainingMovementTime` `0x06000C5A` / `GetRemainingActionMovementRangeFeet` `0x06000C58`, `HasMovement` `0x06000C52`, `HasNormalMovement` `0x06000C51`, `EnabledSingleActionMove` `0x06000C1C`; movement time is accounted through `TurnController.TimeMoved` `0x06000C14` |
| Cost owner | native Move budget of the **mount** for both legs (the accepted CRPG preset), rider's own action for the cast |
| Same-activation rule (`C6D-move-cast-move`, reader `Assert-KmcStagedSameActivation`) | every cost event of the three steps belongs to one native activation: one turn identity, one round, the rider as current actor, no turn-transition boundary (prepare, clear, turn-end, round state/handler or AI round) inside the window; exactly one rider `cost-after` in the window. Turn-based: the second leg continues the mount allocation already used by the first leg (`mountUsedOneMove` true before it, `remainingNativeTime` strictly lower than at the first leg's end and below the fresh 6 s grant, the mount Move cooldown unchanged by the cast). A sequence whose second leg sits in a refreshed turn never qualifies (negative tests in `Test-Chunk6dStaged.ps1`). Qualified on the immutable preview.205 evidence through a harness-only re-evaluation (all three steps on turn 1565247616), re-run on the closeout candidate |
| Closed question | the partner context's `TimeMoved` carries between legs without a second grant and `AutoStopAfterFirstMoveAction` `0x04000687` does not end the rider-led boundary between them (the recorded `autoStopAfterFirstMoveAction` facts) |

### 7. Staged double-move ranged action — IMPLEMENTED, SAME-ACTIVATION CONTRACT QUALIFIED (6D)

| Facet | Contract |
|---|---|
| Controlling actors | rider (ranged action) and mount (both move legs) |
| Native surface | as row 6, plus `UnitEntityData.UsedTwoMoveAction` `0x06008383` as the exact two-move predicate and `IsFullAttackRestrictedBecauseOfMoveAction` for the restriction KMC's CRPG preset deliberately does not add for transport |
| Cost owner | native |
| Same-activation rule (`C6D-double-move-ranged`) | the mount's two Move legs (step 0) and the rider's ranged attack (step 1) share one native activation under the rule of row 6; the rider's Standard is available immediately before the ranged attack (`steps[1].before.rider.standard` zero and `riderHasStandard`), exactly one native Standard cost belongs to that attack, and in turn-based play the mount had used both Move actions before the attack (`mountUsedTwoMove`, mount Move cooldown above 3 s). The later scroll cast (step 2) is a separate next-turn control and is **not** evidence for the retained-Standard claim; the earlier reader's step-2 retention rule was removed (preview.205 evidence: steps 0/1 on turn -2051895680 round 3, step 2 after the native rollover). A sequence whose attack sits in a refreshed turn never qualifies |
| Closed question | the mount spending both Move actions leaves the rider's Standard ranged attack intact within the same activation; a refreshed allocation is detected and refused by the reader |

### 8. Mounted Combat defensive feat — 6E FEASIBILITY COMPLETE — DEFENSIVE FEAT DEFERRED; NOT IMPLEMENTED

| Facet | Contract |
|---|---|
| Disposition | **NOT IMPLEMENTED.** The product contains no Mounted Combat feat, no reaction command, no negated attack roll and no Ride check. `Chunk6eReactionEvidence.ps1` (schema 46, contract `native-mounted-reaction-feasibility-v1`) and `Chunk6StagedScenario.cs` record native Swift/reaction **feasibility observations** only; the reader requires that no product reaction fires and no attack roll is negated |
| Observed native limitation (bounded) | Kingmaker admits a Swift-typed `UnitUseAbility` on the rider's own turn (`C6E-swift-on-own-turn`: the rod-quickened instrument spends the Swift cooldown natively) but has no immediate-action reaction command for a player unit outside `UnitAttackOfOpportunity` `0x02000502`: an out-of-turn Swift input is admitted into a shell that never starts (finished `Success` without running, or interrupted by the next pair preparation) with no cost, cast or spend (`C6E-swift-out-of-turn`); an attack on the mount is observed at `RuleAttackWithWeapon` `0x02000D4C` (`C6E-attack-on-mount-observed`) and the real-time reaction window is observable (`C6E-reaction-window`). This bounds the designs measured; it does not prove every possible design impossible |
| Authorized path | bounded feasibility, then defer (owner authorization of 2026-10-08). No unbounded reaction implementation campaign is opened by this closeout |
| Cost owner (if ever implemented) | an immediate action, i.e. the rider's Swift debt, written **only** by native `UpdateCooldowns` through a real Swift-typed command — never by a direct field write |
| Native surface (reference) | pre-consequence attack-result seam `RuleAttackWithWeapon`: `AttackRoll` `0x06007197`, `IsCharge` `0x06007185`, `OnTrigger` `0x0600719D`, `CreateRuleDealDamage` `0x060071A1`; Swift debt `Cooldowns.SwiftAction` `0x0600C3BA`/`0x0600C3BB`, reset by `Cooldowns.Clear` `0x0600C3BE` at each `Prepare` |

## What Chunk 6A explicitly does not do

No mounted Charge, no mounted casting or item changes, no staged moving
actions, no Mounted Combat feat, no second rules preset, no revival of either
legacy turn authority, no new persistence schema, no main merge, no release, no
permanent installation.


## 2026-09-28T01:48:01.223Z - exact native unacted Mount interruption

Pinned Kingmaker Assembly-CSharp SHA2563b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb / MVID07fa1e4d-8618-41b3-9b8d-faa17d3b26f7. OnMovementInterrupted(0x0600184F) uses the UnitMoveTo-only Move getter0x0600269F; GetCommand(CommandType)0x060026A9 observes the base slot. Native Interrupt0x060027AC owns ResultInterrupt and calls OnEnded0x060027B2, which owns completion; neither charges or marks acted. Preview119 c6a-path-snapshot-a-obstruction directly observed the exact unacted UnitUseAbility left pending after this callback. Source120 adds prefix capture/postfix same-command revalidation and shell retirement before native Interrupt, with no resource writes. Detached ownership negatives24/0, native assembly629/0 and wrapper constructionPASS; actual new callback execution remains a Unity gate. Full cause, original assertion and artifact hashes: docs/CHUNK6A-COMBAT-MOUNT.md and docs/chunk6a-evidence-history.json. Observation caps, deadlines and qualification assertions unchanged.


## 2026-09-28T03:23:07.787Z - obstruction envelope and external result facets

Preview120 directly observed NativeMountedControlService's exact unacted interruption callback in Unity: command381715712, same shell, nativeInterrupt/OnEnded at frame1946, zero cost/transition and preserved action/reaction window. Overall qualification nevertheless FAIL: Assert-KmcChunk6aObstruction used corpulence +1.0 instead of the frozen CombatMountDismountPolicy.NativeAdjacentReachMeters1.5. Source121 fixes only the validator formula; real Horse corpulence0.9 plus rider0.5 must derive2.9. Source114/0 and causal216/0 regressions retain the same tolerances and closed-door observations; the historical parser replay never changes120's overallFAIL. Exact original artifacts and both native/external result facets are retained in docs/chunk6a-evidence-history.json. New121 requires fulloffline/newcandidate/newproof/nativequalification.
