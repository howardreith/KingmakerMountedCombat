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

Preview.1 offline checks: source 22/0, COMPONENT 345/0, visual/capture 23/0, inventory 10/0, harness 243/0, phase protocols 14/0 and 29/0, ASSEMBLY CONTRACT 415/0, package 10/0. Preview.2's initial protocol/negative checks are 15/0; native comparison is pending. Early diagnostic compilation errors were fixed before packaging; no failed build was deployed.
