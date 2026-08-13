# Autonomous resume

Updated: 2026-08-13T16:44:09Z

- Exact branch / HEAD: `codex/mounted-combat-feasibility` / `d2aaecaf81d92238ce0309e2914c3bd2ec6516a0`.
- Working status: durable intake files are untracked; `LocalGamePaths.props` remains ignored and untracked.
- Active version: `0.0.1-feasibility`.
- Last successful gate: mandatory repository isolation and exact platform/reference intake PASS; production pre-code gate remains IN PROGRESS.
- Current failure/hypothesis: no product failure. Hypothesis under test is that Kingmaker's older command/movement/entity/view primitives can support one mount-authoritative, runtime-only pair without a global patch.
- Files being changed: planning and docs intake records only; no production source exists.
- Exact next command: inspect bounded Wrath `UnitPartRider`, `UnitPartSaddled`, and `SaddledUnitController` alongside Kingmaker `UnitMovementAgentBase`, `UnitCommands`, `ClickGroundHandler`, `SelectionManager`, and formation helpers; record exact member contracts.
- Analysis profile: bounded decompilations exist only in lab `analysis-cache\wrath-bounded` and `analysis-cache\kingmaker-bounded`; no bulk source tree was produced.
- Runtime profile: no KMC harness or transaction active; live runtime roots were empty at intake.
- Unrestored external state: none.
