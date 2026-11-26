# PR-5 extraction run: provenance (PR-5 review R1-F9)

`PR-5-extraction-run-b1c82f31.log` beside this file is the original, unedited output of the metered Fast + focused run named in
the mission's section 5 (SHA-256 `c2619aafd873583b62e38cbeca869c836b2808d1d3ca5b49af8f2191989374f4`, 243,088 bytes, LF line endings: the digest of the committed blob; a checkout with `core.autocrlf=true` writes CRLF and hashes differently).

| Fact | Value |
|---|---|
| Command | `scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1` (Release) with `KMC_WORKFLOW_BUDGET_ID=mounted-alpha2-melee-pose-20261124` |
| Shared-budget entry | `mounted-alpha2-melee-pose-20261124:20261125T153652676Z:26920:3fe551bf` (tier, started 2026-11-25T15:36:52Z, ended 15:38:07Z, 75 s, completed); its admission line is the log's first budget line |
| Results in the log | build clean; components TOTAL PASS=1534 FAIL=0; Validate-Source 153/0; bite native contract 3/0; bite contract 28/0; presentation 239/0; MMR-HORSE-BITE FOCUSED PASS (14 of 14 children) |
| Product built by the run | `bin\Release\KingmakerMountedCombat.dll` sha256 `750fbcdab5051a4e3e131ef3369098012c99c6ca3e5e3a290b27693c5e37419b` (printed by the Shield and Archery children) |

**Binding to b1c82f31.** The run executed on the extraction working tree in `worktrees/mmr-horse-bite` immediately before the commits:
the branch was created from `783b2b3d` at 15:36:37Z, the run ended at 15:38:07Z, and `9ae211e7` (mission) and `b1c82f31`
(extraction) were both committed at 15:38:49Z (branch reflog). The log itself names no commit. To bind it, `b1c82f31` was checked out
in the same worktree on 2026-11-26 and rebuilt (`MSBuild KingmakerMountedCombat.sln /t:Rebuild /p:Configuration=Release`; the product
project is `<Deterministic>true</Deterministic>` with no Release PDB). The rebuilt `KingmakerMountedCombat.dll` has sha256
`750fbcdab5051a4e3e131ef3369098012c99c6ca3e5e3a290b27693c5e37419b`, byte-identical to the product the logged run tested. The rebuild
was metered as shared-budget entry `mounted-alpha2-melee-pose-20261124:imported:20261126T033929725Z:1fcb48ac` (35.8 s); the
worktree was returned to `codex/mmr-horse-bite` afterwards.

The mission text says the run was "at the extraction commit"; precisely, it was on the tree that became that commit 42 seconds later,
as the identical product shows.

**Later verification (2026-11-26, owner-authorized).** A separately metered run of the same command on the committed `b1c82f31`, in an isolated
worktree, is recorded with its own date and identity in `PR-5-b1c82f31-verification-2026-11-26.md`. The log above is unchanged.
