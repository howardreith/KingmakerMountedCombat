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
