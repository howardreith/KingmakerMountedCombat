# Mounted Combat journal

## 2026-08-13T16:44:09Z — mandatory intake and mission preservation

- Branch / HEAD: `codex/mounted-combat-feasibility` / `d2aaecaf81d92238ce0309e2914c3bd2ec6516a0`.
- Active version: `0.0.1-feasibility` (identity declared; project not yet scaffolded).
- Work completed: confirmed the standalone root, clean initial tree, expected standalone origin, author, single worktree, and absence of Git operation markers; created the mandated branch; preserved the full 1,512-line mission; inventoried the lab, platform, references, harness, processes, Mods, and fixture availability; fingerprinted exact gameplay/UMM/Harmony/Unity assemblies; generated bounded ignored decompilations for initial exact types; created the original guarded push helper.
- Commands/evidence: `git rev-parse/status/remote/worktree/log`; PowerShell filesystem/process/appmanifest inspection; `Get-FileHash`; isolated reflection metadata inspection; MSBuild/Roslyn/ILSpy version checks; exact ILSpy type lists and bounded type decompilation.
- Exact test counts: product tests 0 PASS / 0 FAIL; runtime scenarios 0 PASS / 0 FAIL. Intake assertions 18 PASS / 0 FAIL. One initial branch-write command was sandbox-denied and then completed through the approved scoped Git operation; this was not a product-test failure.
- Runtime evidence IDs/paths: none; no game launched. Read-only intake source: lab `environment-intake.json` SHA-256 `84aa0b4592807ad0ed6ce9fd3fd12de90b9a9be35ff02bcfba6f0021182b8226`.
- Hashes/MVIDs: Kingmaker `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; Wrath SHA-256 `2cb7160b7154d4ffacc77b9c51b1eb26199e1294300f04fdfc073367b2ef8953`, MVID `90a9869c-2792-4c7b-bfb7-5a8b33da7c82`.
- Rejected theories: expected UMM 0.32.x is not the installed authority; observed UMM is 0.28.2.0. Kingmaker does not contain the Wrath `UnitPartRider`, `UnitPartSaddled`, or `SaddledUnitController` types.
- Current uncertainty: exact member/caller graph, Kingmaker authoritative-mover seam, native candidate mount/anchor, and all runtime behavior.
- External state/restoration: no game, Mods, save, Steam, or Wrath mutation occurred; no transaction was opened. Live Mods read-only manifest digest at intake: `e9545a645933e19acfcbb464d084a75319668bf0ff29b04468e03e2900399069`.
- Exact next action: complete the exact Wrath mounted dependency trace and Kingmaker candidate member trace, then update the contract gate before production relationship code.

## 2026-08-13T17:17:18Z — exact mounted contracts and diagnostic scaffold checkpoint

- Branch / HEAD: `codex/mounted-combat-feasibility` / `3801345720241eeab75f2944d91948f182ca26aa`.
- Active version: `0.0.1-feasibility`.
- Work completed: traced the exact local Wrath movement, command, avoidance, entity/view, selection, formation, targeting, serialization, and cleanup graph; proved the corresponding Kingmaker control points and absences; recorded exact tokens and a bounded no-global-tick seam; created the initial standalone net47/C# 7.3/AnyCPU scaffold and strict no-save request/result protocol.
- Commands/tests run: bounded `ilspycmd` exact-type decompilation and metadata listing in ignored analysis cache; exact caller/callee inspection; Visual Studio MSBuild `Rebuild` Release; deterministic console test runner; JSON parse validation.
- Exact PASS/FAIL counts: contract responsibility rows 15 PASS / 0 FAIL / 1 IN PROGRESS (native candidate presentation); pure/component tests 7 PASS / 0 FAIL; build attempts 1 PASS / 1 FAIL. The first build failed because installed UMM 0.28.2 has a co-installed net48 Harmony2 dependency; the project now explicitly ignores only that indirect target-framework attribute mismatch while KMC remains net47 and references only the Harmony12 API. Runtime scenarios remain 0 PASS / 0 FAIL.
- Runtime evidence IDs/paths: none; neither game was launched. Bounded evidence is under lab `analysis-cache\wrath-bounded` and `analysis-cache\kingmaker-bounded` only.
- Hashes/MVIDs: authority hashes unchanged; diagnostic DLL SHA-256 `77f05b78aa0bcc324ab020805565fa8869c0c4b413a267fb8d9da77a90f6a936`, MVID `88c01451-757e-49a9-8926-424de16ef47f`; guarded push helper SHA-256 `fcbd5dd80f943f604ad4883d5611807cd5e0b7ab5ab1263ac672b0790d7d9fce`.
- Rejected theories: Wrath is not a cosmetic `Mount()` attachment and is not an unconstrained two-agent relationship; it delegates rider intent to the mount, disables rider avoidance, synchronizes rider entity/view every tick, redirects selection/formation, couples combat commands, and persists reciprocal parts. A global Kingmaker movement-tick patch is unnecessary. `AgentOverride` alone is insufficient because `MoveTo`, precomputed paths, and avoidance inspect stock `AgentASP` directly.
- Current uncertainty: one native Kingmaker candidate and stable anchor remain unproven; private ground-command patch behavior, custom override ordering, residual drift, doorway/formation behavior, and all runtime cleanup are unqualified.
- External state/restoration: no game, Mods, save, Steam, or Wrath mutation occurred; no runtime transaction was opened; live Mods remains untouched.
- Exact next action: finish the native candidate/anchor metadata inventory, close or reject the presentation gate, then complete source/build/package validators and commit the diagnostic scaffold.
