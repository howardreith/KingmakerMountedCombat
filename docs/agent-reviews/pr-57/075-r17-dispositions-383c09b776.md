<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r17-dispositions-383c09b776 part=1/1 sha256=383c09b7764546fe42a1e789f74ff8f303dc5df5893005a97206da3b28cca768 -->
### Claude Opus — finding dispositions — round 17

Candidate after corrections: `352c12aca79a1a681f75608a342fdf2edec7a1d8`

- **R14-F2** — `fixed` — commits: `352c12aca7`
  MountedControlPlacementIntent.ObserveNativeCleanupOutcome's rebound branch now returns without recording when the slot is a secondary copy of an interrupted choice, either a recorded copy or an alias report (aliasReport). ApplySlotInvalidation passes item.Copies and item.Alias, and the fenced path forwards both through ObserveOutgoingCleanupOutcome. A native rebind of the surviving older alias therefore keeps the interrupted choice at 4. A rebind at the chosen slot itself is still recorded as live (NativeRebindOutcome passes).

Regression AliasRebindAfterCanonicalCleanup: on MixedOlderBar, GetSlot empties 4, then CleanupBadSlots rebinds the alias at 9 to a live shell. Reconciliation gives one shortcut at 4 with slot 9 empty, the save carries 4, and a reload restores one shortcut at 4.

The mutation removing the check failed this regression ('Reconciliation promoted the rebound alias instead of 4'). The gate passed on 352c12ac.
- **R15-F1** — `fixed` — commits: `352c12aca7`
  ObserveInvalidatedPublicSlot records, with each report, the other public bindings on the bar at that moment (PendingSlotInvalidation.Copies). ObserveNativeSlotInvalidated stores them as Entry.Copies on the interrupted choice; Copy, ReplaceWith and the outgoing load records carry them.

IsSecondaryBinding now treats a recorded copy as secondary even when it holds live data. The untouched live copy at 9 after GetSlot(3) is therefore never observed, written or kept as the choice. A binding placed afterwards with live data is not a recorded copy and stays the player's.

Regression LiveCopyAfterCanonicalCleanup, using StaleThenCurrentCanonical's bar (stale at 3, current at 9) with GetSlot(3) just before the save:
- one kind-6 row, at 3;
- the continuing world settles on one shortcut at 3 with slot 9 empty;
- a reload restores one shortcut at 3.
Control: a new player placement at 9 after the cleanup is saved at 9. The manual-move control in TwoCanonicalCleanup still passes.

The mutation ignoring recorded copies failed this regression. The gate passed on 352c12ac.
- **R17-F1** — `fixed` — commits: `352c12aca7`
  IsSecondaryPublicSlot now also requires the interrupted chosen slot to be free (ChosenSlotFree). ObserveArrangementOnBar passes the same flag to ObserveBindingAtBoundary, which promotes the surviving copy when the chosen slot is occupied. One decision therefore drives bar observation, live repair (ReconcilePublicControlSlots), capture classification, the live writer and lease release (AllowsLeaseRestore at the promoted index).

Regression OccupiedChosenSlotWithCopy, run update-first and save-first: GetSlot empties 4 on MixedOlderBar, Fireball goes to 4 and the same pet returns. Both orders give:
- one shortcut at 9 in the continuing world, with Fireball at 4;
- a save carrying 9;
- a reload with one shortcut at 9 and Fireball at 4;
- a resave carrying 9.

The mutation removing the promotion failed this regression at saveFirst=True. The gate passed on 352c12ac.
- **R17-F2** — `fixed` — commits: `352c12aca7`
  New exact patch MountedPatchController.NativeGetSlotToken = 0x06002FFE, a prefix and postfix on UnitUISettings.GetSlot(int, UnitEntityData) that share the CleanupBadSlots bracket. A new assembly-contract row pins that token and the method's construction of MechanicActionBarSlotEmpty; it passes (823/0).

ObserveInvalidatedPublicSlot now returns unless a native slot operation is open on that unit's UISettings. A standalone IsBad query, such as Finalize2ControlsScenario.F2SlotKey, therefore records nothing. Reports made inside an operation are resolved when it returns, from the slot's state at that point (an emptying for GetSlot; IsNativeRebind for CleanupBadSlots). Nested native calls fold into the outer operation, and a bracket left open by an exception is reset and resolved at the next Update.

Regression StandaloneIsBadQuery: a stale canonical slot with a live replacement fact is queried directly and left in place; the player then removes it before reconciliation. It stays removed, the save holds only the placement record, and a reload leaves it empty. Control: an actual native emptying is recovered once.

The mutation removing the gate in the double, which mirrors the production check, failed this regression. The gate passed on 352c12ac.

Limit: the double mirrors the service's bracket gate, and the GetSlot patch's timing is NOT RUN natively.
- **R17-F3** — `fixed` — commits: `352c12aca7`
  In the fenced branch, ApplySlotInvalidation now first calls the new MountedControlLoadFence.RetireOutgoingOnCompanionLoss. That runs RetireOnCompanionLoss on the scope's outgoing intent, a retirement abandonment keeps. When it retires, the service immediately clears the rider's public bindings (ClearPublicControlSlots) and records nothing else. An abandoned load therefore restores the retirement with no obsolete binding left for B to adopt.

Regression FencedRebindCompanionChange:
- Setup: A's interrupted shortcut at 9 holds stale data, the rider now links to eligible B, and a selected load fences the world.
- Native cleanup rebinds the slot, then the load is abandoned and the game reconciles and saves.
- Result: the old slot is empty, Attack at 0 is unchanged, and the save has no public row for B.
The same-A rebind (FencedRebindAbandoned) and completed-load isolation controls still pass.

The mutation disabling the fenced retirement failed this regression ('Pet B adopted pet A's retired shortcut'). The gate passed on 352c12ac.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 47.2s | Result of record on commit 352c12ac (log gate-focused-352c12aca79a-d43d449b6c4-a030b738800-20261121T191233793732Z.log), exit 0, worktree unchanged. Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including the assembly contracts with the GetSlot token row. Offline only; the new GetSlot patch is not exercised natively. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (token discovery, then the fixed tree with PASS lines temporarily silenced) | no | pass | 0.82 min | Development runs. In the first, a temporary non-zero contract check printed the GetSlot token 06002FFE, which was then pinned. Fixed tree: components 1488/0, Validate-Source 153/0, assembly contracts 823/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The tests/TestRunner.cs change was reverted. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, FAIL lines visible: 1483/5. Domain mutations: rebound-copy check removed, recorded copies ignored, occupied-slot promotion removed, fenced retirement disabled. Double mutation: the native-operation gate, mirroring the production check. Exactly the five new regressions failed. All mutations were reverted and a grep confirmed none remained. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
