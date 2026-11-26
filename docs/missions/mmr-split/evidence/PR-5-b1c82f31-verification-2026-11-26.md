# PR-5 R1-F9: verification of the existing focused checks on frozen extraction commit b1c82f31 (run 2026-11-26)

Authorized by the owner instruction of 2026-11-26 (`docs/missions/mmr-split/owner-authorization-2026-11-26b.md` section 6): one
separately metered verification of the relevant existing checks on frozen extraction commit `b1c82f31`, in an isolated worktree,
through the supported validation path. This record has its own date and identity. It does not replace or edit the original log
(`PR-5-extraction-run-b1c82f31.log`, run 2026-11-25 15:36:52Z-15:38:07Z on the pre-commit tree), and it does not prove what that
earlier pre-commit tree was. It shows what the committed `b1c82f31` produces when the same command runs on it today.

| Fact | Value |
|---|---|
| Worktree | `worktrees/mmr-horse-bite-b1c82f31-verify`, created by `git worktree add --detach ... b1c82f31` (isolated; detached; kept for inspection) |
| Commit / tree | `b1c82f31b47dc96d93d556bc152970f4f634f17d` / tree `2eabcd4993e363c4226dd36461379d55c7f6982b`; `git status --porcelain --untracked-files=all` empty before the run; no tracked change after it |
| Environment file | `LocalGamePaths.props` (git-ignored, machine-local game path) copied from `worktrees/mmr-horse-bite`, sha256 `ecd0ad7dda775222823d785ddce0e626bfae17836cb73661b04873e97fbbd122` |
| Command | `scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1` (Release), `KMC_WORKFLOW_BUDGET_ID=mounted-alpha2-melee-pose-20261124` |
| Run | 2026-11-26T13:01:05Z to 13:02:34Z, exit 0; shared-budget entry `mounted-alpha2-melee-pose-20261124:20261126T130105827Z:44652:b1163834` (tier, completed, 1.48 min) |
| Product built | `bin/Release/KingmakerMountedCombat.dll` sha256 `750fbcdab5051a4e3e131ef3369098012c99c6ca3e5e3a290b27693c5e37419b` (the same digest the original run printed) |
| Component executable | `bin/Tests/Release/KingmakerMountedCombat.Tests.exe` sha256 `56a1c86150287b8f6c9ab1ce359e0b0f9074244793d93b49721cb23e604ed341` |
| Scripts (git blob / file sha256) | in the identity-after file: Test-Tiers, Test-MmrHorseBiteFocused, Test-MmrHorseBiteNativeContract, Test-MountedAlpha2BiteContract, Test-MountedPresentationContracts, Validate-Source |
| Results | components 1534/0; Validate-Source 153/0; bite native contract 3/0; bite contract 28/0; presentation 239/0; Mounted Shield 114/0, rollback 68/0; Archery 54/0; Ranger/Riding Dog 183/0; replay 29/0; frozen source 23/0; UMM loaders; MMR-HORSE-BITE FOCUSED PASS (14 of 14 children) |
| Comparison with the original log | every count line identical; only the tier's elapsed time differs (1.48 vs 1.25 min): `PR-5-b1c82f31-verification-2026-11-26.count-comparison.txt` |

An earlier attempt at 2026-11-26T13:00:42Z (budget entry `mounted-alpha2-melee-pose-20261124:20261126T130042922Z:32652:e90dfe57`,
failed, about 2 s) stopped at the build because the fresh worktree lacked `LocalGamePaths.props`. It ran no check and is preserved in
the operator records (`decomposition/pr5/r1f9-verify/attempt1-env-failure`).

Files (sha256 of the committed blobs, LF):
- `PR-5-b1c82f31-verification-2026-11-26.log` `10e7b067749772e4583cf435ad5dcaf1acf99ca125c0299da6f7fffaacaed696`
- `PR-5-b1c82f31-verification-2026-11-26.identity-before.txt` `c04709e63ecd8bd74083c3789b83cace2a5a0274acee3b43530dc44df688cd78`
- `PR-5-b1c82f31-verification-2026-11-26.identity-after.txt` `5f68c478da7ec953f83df634387e7a7abcecc6d72a85846f31601a6e19aae78d`
- `PR-5-b1c82f31-verification-2026-11-26.count-comparison.txt` `4ad06bd39aa0b3202c77a101d1c2574fc2fa85c96f71b96d419535d70f239de6`

# PR-5 R2-F5: mutation demonstration (2026-11-26)

With one record write (`horsePrimaryAnimationHandle = handle;`) moved ahead of the metadata read in `RecordHorsePrimaryAnimation`
(`PR-5-R2-F5-mutation.diff`), a Release build (metered, 41.9 s) and the bite contract (metered, 1.0 s) show the R2-F5 check failing:
the fault is still the `name` read (SecurityException, ECall) past the precondition, but the first record write now precedes it
(IL 68 before get_name at IL 74) and a field is published (`PR-5-R2-F5-mutation-contract.log`). The source was restored with
`git checkout` immediately; the committed candidate's bite contract passes (39/0). Two earlier metered runs on 2026-11-26 (build
34.3 s, contract 1.0 s) ran on the unmutated source because the edit failed to apply; they demonstrate nothing and are kept in the
operator records only.

- `PR-5-R2-F5-mutation.diff` `6c090e492fef7485b9306c2eac1d0f6783c33c71ca604bc7dd97ba962beaeb2e`
- `PR-5-R2-F5-mutation-contract.log` `1fc58edc0fd18c61a3aa18b176841649ea6c3bfdc236eec9d776e0392bb5e84d`
