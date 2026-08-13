# Reference provenance

Status: PASS for snapshot identity; reuse review remains per-fragment.

Captured at 2026-08-13T16:44:09Z. All source repositories were inspected read-only. Each is a clean standalone primary worktree with no linked-worktree or reparse-point relationship to another active product.

| Reference | Branch / exact HEAD | Origin | License posture | Phase 1 use |
|---|---|---|---|---|
| Vek17/TabletopTweaks-Base | `master` / `221decc02265c780685650ea987bca0a9eeaf49a` | `https://github.com/Vek17/TabletopTweaks-Base.git` | MIT, copyright Sean Petrie | Contract leads only unless an exact fragment is later recorded |
| zephe0n/AutoMount | `master` / `59bd24fd49d9e4868e2b88d8d0019f21e6f69cf3` | `https://github.com/zephe0n/AutoMount.git` | MIT text, placeholder copyright fields | Contract leads only; no source copied |
| fl01/pathfinder-wotr-multiplayer | `main` / `6402b9ba9dd503dfb6054e595919fbd8992c4a21` | `https://github.com/fl01/pathfinder-wotr-multiplayer.git` | No local license or notice | Factual observations only; source reuse prohibited |
| CasDragon/DragonFixes | `master` / `cf74881ebc2e3762bdab720275fc903d44377764` | `https://github.com/CasDragon/DragonFixes.git` | MIT, copyright CasDragon | Contract leads only unless an exact fragment is later recorded |

The harness reference is a non-Git, non-linked snapshot of `howardreith/KingmakerBuffPlanner`, `main` at `c06793d2238577093b96a2dc3172839070e7d69a`, recorded clean by its manifest. All 29 declared files matched length and SHA-256. The snapshot has no included license, is incomplete, and is not executable as copied. Its transaction and protocol concepts may be studied, but KMC scripts and types must be original.

Declared missing optional harness files:

- `Directory.Build.props`
- `docs/WIN10-AUTONOMOUS-RUNTIME-TESTING.md`
- `scripts/package.ps1`
- `scripts/Test-RuntimeRequest.ps1`
- `scripts/Test-RuntimeResult.ps1`
- `codex-policy`

The snapshot also contains copied `obj` output, including a test EXE and PDB. Those files are reference debris and are excluded from every KMC source/package allowlist.

No third-party source fragment has been copied into this repository.
