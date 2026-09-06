# Chunk 1: Ordinary attack correctness

Status: IN PROGRESS. Starting branch `codex/mounted-combat-phase3f-playable-core`, local and host remote `1d2b8c3ccad14009653af9dc6420ee9af7b2e804`; clean intake. Historical candidate/source identities remain in [Phase 3H](PHASE3H-IMPLEMENTATION.md).

## Named native boundary under investigation

Installed Assembly-CSharp SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`. UMM 0.28.2 and its installed legacy Harmony12 API remain unchanged. Bounded local inspection verifies these observation hooks (no native implementation is redistributed):

| Member | Token | Responsibility |
| --- | --- | --- |
| TurnController.UpdateActionPredictions() | 06000C6E | Temporary command container, pointer handler simulation, native approach/action estimates |
| TurnController.SetAttackMode(private AttackMode, bool) | 06000C3F | Native cursor mode transition; diagnostic observes its integer enum value |
| PointerController.SimulateClick(GameObject, bool) | 060093C7 | Calls selected simulated handler while SimulatingClick is true |
| ClickUnitHandler.OnClick(GameObject, Vector3, int, bool, bool) | 060093ED | Native hostile request events and command dispatch |
| UnitCommands.Run(UnitCommand) | 060026B2 | Original/replacement command admission |
| UnitAttack.InitAttacks() / OnStart() / OnAction() | 0600267C / 0600267E / 06002681 | Native plan, actual start replan and attack delivery |
| UnitCommand.OnEnded(bool) | 060027B2 | Native terminal boundary |
| UnitActionController.UpdateCooldowns(UnitCommand) | 06009120 | Native action expenditure |

The historical fixture calls TurnController.OnHoverObjectChanged directly. Native UpdateSelectedClickHandler instead selects a handler from PointerController.PointerOn/WorldPosition. A mismatched fixture pointer is a discriminating hypothesis, not a proven repair. Separately, native prediction may downgrade full mode for predicted approach. The scoped trace observes both boundaries before gameplay changes. Full-mode flags and resources are never overridden.

## Evidence and open gates

Local campaign root: `C:\Dev\KingmakerMountedCombatLab\analysis-cache\runtime-evidence\chunk1-20260906`. Intake at 2026-09-06T13:18:37Z found no Kingmaker/UMM/Wrath process. Full current save digest `bddad0065064ebc166c8414e660b938fe9be2e4894c3b9916cdd972c8bf05913`; Mods digest `0e45b19883f5405a076df6a92ef5175282817516aafdc6f343dbbbcf2e10e835`. Installed preview.7 DLL/cache remain `6bc01b982309125963d2355983b37391f244cfea314093fe4b008ad21a241ff3`. These are fresh measurements, not permission to overwrite later human changes.

C01-C03 and native regression are IN PROGRESS. COMPONENT and ASSEMBLY CONTRACT results will be recorded separately from NATIVE INTEGRATION. HUMAN PLAY is pending: computer-use initialization returned native pipe unavailable (OS error 2). Mod-absent certification is pending a verified native fixture with no permanent KMC references. No runtime qualification or allocation completion is claimed.

The first native run, `20260906-chunk1-trace-tb-A`, reproduces the historical longbow failure on diagnostic source `665a559d94d842d16fc3ecf9cd37ab530d30a2c2`, version `0.1.0-chunk1-preview.1`. ZIP SHA-256 `3f54a0796d965ab98a7330712ca7c186115cd578552ba4aeaccbd6de4a073ffb`, DLL `0063e8b0f66c044d1cdadd96a70fe6f00525137ef5fbeb470500a59f58513203`, MVID `d2ffa61d-cde1-43f3-8be4-f613a9d025ff`. The seven gameplay leaves are 6 PASS / 1 FAIL; parent/registration totals are not product coverage. All 239 trace events were captured without drops or observation errors.

**Causal finding so far: harness/input defect.** Before mounted routing, the current turn was already Single. The fixture's highlighted unit became the hostile target, but PointerOn was null and the selected simulation handler remained ClickGroundHandler. No hostile prediction ran between the hover notification and real dispatch. Native start replanned one legal Single attack and native charging spent Standard 0→6, Move 0. This evidence does not prove a general mounted prediction defect. Both the host helper and an independent complete-tree audit verified actual intake restoration at 2026-09-06T13:46:29Z; the immutable log SHA is `a6ececd223684b76607884b987a6c33bcd35624dfabe7140f1106932efa6eaf8`.

The next candidate changes fixture input: an exception-safe lease supplies consistent native PointerOn/WorldPosition/simulation-position values, calls native handler selection and UpdateActionPredictions, then native IgnoreClick and the selected handler. It restores only the temporary pointer fields; native planning/mode choice and action expenditure are observed without overrides. Stable scenario `ordinary-attack-controls-tb` reuses the existing fixture, envelope, transactional launcher and validators. C01-B/C/D carry mode/mounted/Primary/loadout parameters. Repeated native predictions and same-target requests compare live pair, command identities, positions and resources. A higher-priority observation prefix now captures the original command before production routing consumes it. New exact pointer setters/handler-selection tokens are assembly-checked; the host guard adds only this exact scenario name.

Preview.1 offline checks: source 22/0, COMPONENT 345/0, visual/capture 23/0, inventory 10/0, harness 243/0, phase protocols 14/0 and 29/0, ASSEMBLY CONTRACT 415/0, package 10/0. Preview.2 checks: source 22/0, COMPONENT 345/0, visual/capture 23/0, inventory 10/0, harness 243/0, phase protocols 14/0 and 29/0, ordinary protocol 17/0, ASSEMBLY CONTRACT 418/0, package 10/0. Early diagnostic compilation errors were fixed before packaging; no failed build was deployed.

`20260906-chunk1-controls-tb-A`, source `85432cf79de83dc5e76ddc73599549658f50bc4e`, preview.2: **2 PASS / 1 FAIL**. ZIP `79378e0113344c7a8689bdf7abe860597f4beff75667c6eba95fb1ffe3c5b5b0`, manifest `c79408b07fd926f46e3b283f8f573c58a4d75f4d40b8d2a27c690699c6e4c217`, DLL `9c0b4c6e599ed4ee0aec781c877b07884d3ebb350e98909d09b8ca7a2a9081fc`, MVID `5e4b0897-400e-4617-8ec7-0b26a57fe479`. Coherent native prediction admits five-shot plans in both enabled/unmounted B and mounted C. C completes 5/5 and spends rider Standard 6 / Move 3; explicit D completes 1/1 and spends Standard 6 / Move 0. Prediction snapshots remain equal and repeated C requests retain one sequence. B is interrupted after 4/5 with the same full-round cost; the interruption caller was not observed. This rules out the old Single-mode fixture result as evidence of a production mode defect, while keeping C01 red. Full restoration PASS at 14:23:02 UTC; immutable log SHA `88f1e372473121caadb77f11c33114abaa9ec2f4770cc1fd4ae4f1cf50e1f4b0`.

Preview.3's next observations cover UnitCommand.Interrupt (`060027AC`) and failed UnitAttack.UpdateTarget (`06002683`), plus terminal target health/position and native attack-roll bonuses. Native fixture seams are ModifiableValue.set_BaseValue (`06007EEB`), BuffCollection.AddBuff (`060029F4`), Buff.Remove (`060029E9`), UnitState.AddCondition/RemoveCondition (`06001FB7`/`06001FB9`). These change only disposable actor inputs and restore through owned leases. Native right-click selects Single; no mode flag is assigned. The stable scenario now contains 15 parameterized comparisons; its aggregate budget is 510 seconds plus 60 seconds of parent fixture setup, preserving every 30-second leaf deadline. Carried-movement/spent-Move and final regression are still pending.
