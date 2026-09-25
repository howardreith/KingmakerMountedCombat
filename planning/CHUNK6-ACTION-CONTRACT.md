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

## Action table

Legend for **6A status**: `IMPLEMENTED` = built and qualified in Chunk 6A;
`MAPPED` = seam contract only, no code; `LATER` = deferred with its mission tag.

### 1. Combat Mount — IMPLEMENTED (6A)

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
| Save/load | settled transitions persist normally; a save requested while a voluntary transition is unsettled is truthfully deferred by `MountedCombatTransitionLedger` reporting an unsettled control, never captured mid-flight. No schema change |
| Player-facing | native Mount Companion in the abilities drawer; disabled reason names the exact failing gate |

### 2. Combat Dismount — IMPLEMENTED (6A)

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

### 6. Staged move–cast–move — LATER (6D)

| Facet | Contract |
|---|---|
| Controlling actors | rider (action) and mount (both movement legs) |
| Native surface | `TurnController.GetRemainingMovementRange` `0x06000C59` / `GetRemainingMovementTime` `0x06000C5A` / `GetRemainingActionMovementRangeFeet` `0x06000C58`, `HasMovement` `0x06000C52`, `HasNormalMovement` `0x06000C51`, `EnabledSingleActionMove` `0x06000C1C`; movement time is accounted through `TurnController.TimeMoved` `0x06000C14` |
| Cost owner | native Move budget of the **mount** for both legs (the accepted CRPG preset), rider's own action for the cast |
| Open question for 6D | the partner context's `TimeMoved` must carry between legs without a second grant, and `AutoStopAfterFirstMoveAction` `0x04000687` must not end the rider-led boundary between them |

### 7. Staged double-move ranged action — LATER (6D)

| Facet | Contract |
|---|---|
| Controlling actors | rider (ranged action) and mount (both move legs) |
| Native surface | as row 6, plus `UnitEntityData.UsedTwoMoveAction` `0x06008383` as the exact two-move predicate and `IsFullAttackRestrictedBecauseOfMoveAction` for the restriction KMC's CRPG preset deliberately does not add for transport |
| Cost owner | native |
| Open question for 6D | interaction between the mount spending both Move actions and the rider retaining a Standard ranged attack |

### 8. Mounted Combat defensive feat — LATER (6E)

| Facet | Contract |
|---|---|
| Controlling actor | rider, reacting on behalf of the mount |
| Native surface | the pre-consequence attack-result seam is `RuleAttackWithWeapon` `0x02000D4C`: `AttackRoll` `0x06007197`, `IsCharge` `0x06007185`, `OnTrigger` `0x0600719D`, `CreateRuleDealDamage` `0x060071A1`. Immediate/Swift debt is `Cooldowns.SwiftAction` `0x0600C3BA`/`0x0600C3BB`, reset by `Cooldowns.Clear` `0x0600C3BE` at each `Prepare` |
| Cost owner | an immediate action, i.e. the rider's Swift debt, written **only** by native `UpdateCooldowns` through a real Swift-typed command — never by a direct field write |
| Open question for 6E | Kingmaker has no native immediate-action reaction command for a player unit outside `UnitAttackOfOpportunity` `0x02000502`; 6E must establish whether a Swift-typed command can be admitted out of turn at all, or whether the feat must be modelled as a per-round allowance observed at the attack rule |

## What Chunk 6A explicitly does not do

No mounted Charge, no mounted casting or item changes, no staged moving
actions, no Mounted Combat feat, no second rules preset, no revival of either
legacy turn authority, no new persistence schema, no main merge, no release, no
permanent installation.
