<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r18-dispositions-f721b7465d part=1/1 sha256=f721b7465de79486414c5f8666f99482d1cddeeb53a1048d93cad501359639b7 -->
### Claude Opus — finding dispositions — round 18

Candidate after corrections: `751124ab84352b60c07c664b48b22a9b10f8100f`

- **R15-F1** — `fixed` — commits: `751124ab84`
  When native empties a recorded copy while the chosen slot is free, MountedControlPlacementIntent.ObserveNativeSlotInvalidated now drops it from Entry.Copies. The new PruneCopies, called from ObserveArrangementOnBar and ReconcilePublicControlSlots, also drops copies whose slot no longer holds a public binding. A live binding the player later places in that slot is therefore not a copy and becomes the arrangement. A native rebind of a still-present copy keeps its copy status.

Regression NewPlacementInFormerCopySlot, run update-first and save-first: stale canonical bindings at 3 and 9; GetSlot empties 3, then 9; the player drags the current control into 9. Result: one shortcut at 9, the save carries 9, and a reload restores one shortcut at 9. The untouched-copy control (LiveCopyAfterCanonicalCleanup) still passes.

The mutation keeping emptied copies failed this regression. The gate passed on 751124ab.
- **R17-F1** — `fixed` — commits: `751124ab84`
  ObserveInvalidatedPublicSlot records whether the interrupted choice's slot is free at report time (PendingSlotInvalidation.ChosenFree). ApplySlotInvalidation passes this to ObserveNativeSlotInvalidated, ObserveNativeAliasInvalidated and ObserveNativeCleanupOutcome, and to their fenced equivalents. If the chosen slot is occupied, native emptying of the fallback copy promotes that copy to the interrupted choice instead of discarding it.

Regression FallbackCleanupWithOccupiedChoice: on the mixed older bar, GetSlot empties 4, the player puts Fireball at 4, then GetSlot empties the stale alias at 9 before any update or save header. When the same pet returns, the result is one shortcut at 9 with Fireball at 4. The save carries 9, a reload restores 9 with Fireball at 4, and a resave carries 9.

The mutation removing the promotion failed this regression. The gate passed on 751124ab.
- **R17-F2** — `fixed` — commits: `751124ab84`
  The GetSlot prefix (MountedPatchController.SlotReadPrefix) now passes the native `index` parameter; the assembly contract pins that parameter name. ObserveInvalidatedPublicSlot ignores any report for another index while a GetSlot operation is open.

ResolvePendingBadSlots counts a report only if the slot object was actually replaced; an untouched slot is discarded. An operation that never returned, so its postfix did not run, is closed at the next Begin or Update and its queued reports are dropped (DiscardPendingBadSlots), never applied. A standalone read inside a stale scope followed by a player removal therefore changes nothing.

Regression AbortedNativeOperation: an aborted operation, then a standalone IsBad read on the stale shortcut at 9, then a deliberate removal before the update. Result: the slot stays empty, the save holds only the placement marker, and a reload leaves it empty. Control: a completed native emptying is recovered once.

The mutation applying the aborted reports, in the double that mirrors the service, failed this regression. The gate passed on 751124ab.

Limit: the bracket and discard logic is service code mirrored by the double, and native call timing is NOT RUN.
- **R17-F3** — `fixed` — commits: `751124ab84`
  ObserveNativeSlotInvalidated and the rebound branch of ObserveNativeCleanupOutcome now refuse a report for a retired actor that has no arrangement. In such a case the report can only come from a stale binding of the retired companion, because a new companion's binding is recorded through ObserveBound and clears the retirement. The first report in a batch retires and clears A's bindings: through RetireOnCompanionLoss live, or RetireOutgoingOnCompanionLoss while fenced. Later reports in the same batch then cannot recreate the arrangement in either the live or the outgoing records.

The double's batch now processes every queued report in order, as production does, instead of stopping at retirement.

Regression RetiredRiderCleanupBatch, run fenced (with the load abandoned) and unfenced: A's shortcut at 3 with a copy at 9, the link switched to B, autofill off, and one cleanup pass empties both with no active replacement fact. After B becomes available, both old slots are empty, Attack at 0 is intact, and the save has no public row for B.

The mutation removing the guard failed the fenced case. The gate passed on 751124ab.
- **R18-F1** — `fixed` — commits: `751124ab84`
  Before repairing, ReconcilePublicControlSlots now collects every secondary copy (IsSecondaryPublicSlot) of the interrupted choice. When the chosen slot is free, it removes all of them, not only the first, then lets ReconcileChosenSlot return the control to the chosen slot.

Regression RepairRemovesAllCopies: the chosen stale binding at 3 with live canonical copies at 7 and 9; GetSlot empties 3, then two reconciliations run.
- Exactly one shortcut is at 3, and the save carries one row at 3.
- A reload restores one shortcut at 3.
- Removing it afterwards persists, and the following save has no slot row.

The mutation clearing only the first copy failed this regression. The gate passed on 751124ab.
- **R18-F2** — `fixed` — commits: `751124ab84`
  ObserveInvalidatedPublicSlot now accepts an alias report only when LegacyAliasIndex(slots), the bar's selected alias, equals the reported index. A report for a secondary alias is never attributed to the selected alias. The same pending item feeds both the live records and the outgoing load records.

Regression SecondaryAliasReport: a recorded live Mount alias at 4, a bad Dismount alias at 9 and no canonical binding. GetSlot reads only 9, then the player removes 4. The removal persists, the save holds only the placement marker, and a reload leaves the bar empty. Control: GetSlot actually emptying the selected alias at 4 is recovered.

The mutation removing the selected-alias check, in the double that mirrors the service, failed this regression. The gate passed on 751124ab.
- **R18-F3** — `fixed` — commits: `751124ab84`
  In scripts/Test-Harness.ps1, the 'target liveness must preserve in-state/hostility admission and native released/finished lifecycle without invented success' assertion now requires:
- `inState && TargetLifeAdmits(attackTarget) && hostile && actionActor.CanAttack(attackTarget)`;
- the helper's delegation to UnifiedMountedStockAttackPolicy.TargetLifeAdmits(state.IsConscious, state.IsDead, state.IsFinallyDead, the exact ordered unconscious target);
- the policy body `!targetDead && !targetFinallyDead && (targetConscious || targetUnconsciousWhenOrdered)`.
It still requires MountedTargetTerminationPolicy.Decide, `childAttack.IsActed && childAttack.LastAttackRule != null`, the in-state check and `childAttack != null && childAttack.IsFinished`.

The new scripts/Test-CityTargetLiveness.ps1 evaluates the same predicate against this worktree. It also confirms the old inline expression is gone and that Test-Harness.ps1 carries the corrected predicate. It is now the sixth child of the focused wrapper and passes 3/0 in the gate.

The dead-target rejection and explicit-order downed-target controls are the component tests in tests/MountedDownedTargetOrderTests.cs, which pass within 1494/0.

Limit: the rest of Test-Harness.ps1 was not run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 45.2s | Result of record on commit 751124ab (log gate-focused-751124ab8435-d43d449b6c4-a030b738800-20261121T194246132670Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's six children, including the new Test-CityTargetLiveness.ps1. Offline only. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.74 min | Development run: components 1494/0, Validate-Source 153/0, assembly contracts 823/0 (GetSlot row now also pins the 'index' parameter), click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, target liveness 3/0, focused 6/6. The tests/TestRunner.cs change was reverted. The rest of Test-Harness.ps1 was not run; only its changed assertion is reproduced by Test-CityTargetLiveness.ps1. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1488/6. Domain mutations: copy dropping and fallback promotion disabled, retired-actor guard removed. Double mutations, each mirroring the production line: aborted reports applied instead of discarded, only the first copy removed, selected-alias check removed. Exactly the six new toolbar regressions failed. All mutations were reverted and a grep confirmed none remained. R18-F3 is a script change, checked by Test-CityTargetLiveness.ps1 rather than by mutation. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
