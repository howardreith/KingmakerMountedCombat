# PR-1 order lifecycle: working record (round-4 correction)

Owner instruction 2026-11-26 section 4: one coherent lifecycle correction for R1-F3, R1-F7, R1-F11, R2-F1, R2-F3, R2-F4,
R3-F1, R3-F2, R3-F3 and R3-F4 (R3-F5 optional). This record states the lifecycle, the fact each transition needs, the
installed native contract it relies on, and where the round-3 candidate (19fef308) broke it.

## Installed native facts (Kingmaker 2.1.7b, read from Assembly-CSharp IL)

- `UnitCommand.Tick`: if not running -> LogError + Interrupt; `TimeSinceStart += dt`; **`OnTick()`**; if finished -> return;
  on the animation act (or pretend-act time) -> **`Result = OnAction()`**, then **`IsActed = true`**, raise the act event;
  if a result and the animation is absent or finished -> `OnEnded`; finally **`if (TimeSinceStart > 9 && !(this is
  UnitMoveContiniously)) { LogError; Interrupt(); }`**. The 9-second deadline therefore applies to this wrapper (a
  `UnitAttack`), and `OnTick` runs before `OnAction` in the same tick.
- `UnitCommand.ForceFinish(result)` (reached by `ForceFinishForTurnBased`): sets `IsActed` (SetIsActed) **without calling
  `OnAction`**, sets the result and calls `OnEnded`. `IsActed` is therefore bookkeeping, not delivery evidence.
- `UnitCombatState.set_ManualTarget` is an 8-byte auto-property setter (not reliably patchable: inline candidate). Its
  writers: `UnitCommands.UpdateCombatTarget` (from the private `Run(cmd, bool, bool)` and `TryUseSpecialHandlers`; writes
  only when `cmd.MarkManualTarget && cmd.AiAction == null`), `ClickUnitHandler.OnClick` (directly, **after** its `Run`),
  `UnitEntityData.TryCancelCommands`, `UnitCombatState.Clear`, `SelectionManagerBase.Stop`, cutscene `AttackCommand`,
  `TouchSpellsController`.

## Lifecycle (one stock-intent generation G)

| Stage | Owner of the fact | Fact required |
|---|---|---|
| Accepted order | `MountedStockIntent` + `MountedRetainedOrderLedger.Begin(G, automatic, melee)` | the click/proposal and its accepted attack kind |
| Actor admission | `TryDriveStockAttackIntent` -> `UnifiedMountedStockAttackPolicy.DecideNext` / `MountJoinsIntent` | per-actor eligibility; rider not redispatched once `RiderEnded(G)` |
| Native start | wrapper `OnStart` | native start inputs; approach authority; movement permission |
| First actual delivery | wrapper `OnAction` (native act) or a raised attack rule / advanced attack index | **not** `IsActed` |
| Actor terminal outcome | `HandleCommandTerminal` | native terminal branch + delivery facts of that wrapper |
| Order retirement | `ClearStockAttackIntent` (one place) | generation-scoped cleanup: intent, ledger, ownership, pending refusal |

The order generation outlives each participant's command: a rider attempt can end (G retained for the horse) while G stays
current; retirement of G ends every unreleased wrapper of G but leaves a released strike to native completion.

## Corrections (one mechanism per concern)

1. **Delivery boundary (R1-F3, R2-F1).** `StrikeReleased` = the wrapper's own `OnAction` ran (marked at its entry), or an
   attack rule was raised, or the routine advanced. `IsActed` alone (force-finish) is no longer delivery.
2. **Terminal disposition (R1-F3, R2-F4).** `MountedRetainedOrderLedger.DecideTerminal` decides from the recorded native
   facts: a turn-based player rider melee attempt ended for the rider's own capability with zero delivery -> rider ended,
   G retained for one horse recheck; a horse terminal with delivery settles the pending refusal silently; a final
   retirement with neither actor delivering presents the pending refusal once.
3. **Pending refusal bound to G (R2-F4).** Held in the ledger with its generation; horse admission does not settle it;
   `Clear`/`Begin` (every replacement and cancellation) drops it.
4. **Wrapper retirement (R2-F1, R3-F1, R3-F2).** One controller path (`RetireOrderWrapper`) for turn-based supersession,
   real-time retirement and perception loss: a released strike is left to native completion (the order still retires, so
   it cannot repeat); an unreleased wrapper is marked retired **on the wrapper** (`OrderRetired`), removed from its native
   queue if queued, and interrupted if it holds a slot. No controller collection retains finished wrappers.
5. **Promotion admission (R3-F1, R3-F2).** `AdmitQueuedExplicitRun` / `AdmitQueuedAutomaticRun` refuse a wrapper whose
   `OrderRetired` is set (promotion-first ordering is covered by the existing supersession check, which now retires through
   the same path).
6. **Selection ownership (R1-F11).** `MountedManualTargetOwnership` per actor records the order's own write (G, target);
   any actual non-own assignment relinquishes it, same target included: a Run-installed (slot or verified promotion)
   command with `MarkManualTarget && AiAction == null` whose post-Run target is that actor's selection (observed before the
   competing-work classification, so coexisting Free/Swift writes count), and the click handler's post-Run write
   (`ClickUnitHandler.OnClick` prefix/postfix: a selection equal to the clicked unit that this click did not issue as the
   pair's own order). A queue insertion or a refused promotion writes nothing and changes nothing.
7. **Approach and clocks (R1-F7, R2-F3, R3-F3).** A waiting carrier holds `TimeSinceStart` (native 9 s deadline) and the
   KMC phase clock; the attack restart resets phase accounting. Movement revocation is applied only after current strike
   admission is evaluated. Sight goals inside the carrier's arrival radius (no achievable movement or turn) are not
   chosen; a goal that ended without admission is excluded and a bounded alternative is tried.
8. **Evidence (R3-F4 and section 5).** The domain decisions are driven in sequence by component tests (terminal -> next
   dispatch, retirement -> promotion, delivery -> supersession, assignment -> cleanup); the wrapper members by the
   attack-path harness; the controller glue by compiled-wiring checks. The lost-reach fixture starts inside reach.
