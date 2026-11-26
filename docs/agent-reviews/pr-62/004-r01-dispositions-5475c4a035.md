<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r01-dispositions-5475c4a035 part=1/1 sha256=5475c4a035c6309de917e8d7d7aba41b60c4f975027f2528de6c5a791a915c32 -->
### Claude Opus — finding dispositions — round 1

Candidate after corrections: `50c25f257e2354140677de1c23f6b8b4a79a24c4`

- **R1-F1** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  docs/evidence/umm-0311/pinned-loader-msbuild.json (9/9): late-compiler-list-rewrite-refused-product, late-compiler-list-rewrite-refused-observer (HintPath and early cache check correct; a target after FindReferenceAssembliesForReferences rewrites the UnityModManager entry; refused before a DLL is emitted), direct-msbuild-compiles-against-pinned-loader, observer-compiles-against-pinned-loader, stray-hintpath-to-unitymodmanager-copy-refused.
- **R1-F2** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  Test-Chunk7NativeLoaderContracts.ps1 (107/107): baseline profile admits its own loader and Harmony 2.3.1.1 creator; baseline observations under the default owner profile, owner observations under the baseline profile, owner UMM with the baseline creator (both profiles) and a profile without a runtime record are refused; the exact owner inventory still passes. Test-Harness: 'runtime game result judged against the selected loader profile' (owner profile accepted; umm-0.31.1 refused as not the measured environment; umm-0.33.2 refused, no runtime record) and the existing mixed-identity case. No native run.
- **R1-F3** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  Test-FrozenProductSource.ps1 (28/28): documentation-only descendant keeps the archived observer identity; observer-only change rejects the archived observer; observer-only change keeps main-only reuse.
- **R1-F4** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  docs/evidence/umm-0311/package-admission-reuse.json (24/24): main-admission-matching-zip-and-manifest-accepted, observer-admission-matching-zip-and-manifest-accepted, admission-correct-outer-zip-with-another-builds-dll-rejected (manifest packageSha256 equals the new ZIP; the DLL is another build of the product).
- **R1-F5** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  Observer from source f5a9f386 via Package.ps1 -IncludeRemovalObserver (Package-Observer route): observer-package-validation.json (DLL f9697f91..., MVID a5e1bb2f-..., entries, references UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1, validator 8/8), observer-package-manifest.json (ZIP 5944cc74...), observer-api-audit.json (8 types/20 members resolved on 0.31.1, 0.32.4, owner-0.33.0, 0.33.4) on that same DLL.
- **R1-F6** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  docs/evidence/umm-0311/package-admission-reuse.json (24/24): pins-genuine-definition-accepted, pins-omitted-harmony12-rejected, pins-harmony12-naming-another-cache-file-rejected, pins-digest-with-trailing-line-feed-rejected (cache already populated).
- **R1-F7** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  docs/evidence/umm-0311/package-admission-reuse.json (24/24): main-validator-genuine-package-accepted, main-validator-one-element-array-minimum-rejected, observer-validator-genuine-package-accepted, observer-validator-one-element-array-minimum-rejected.
- **R1-F8** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  docs/evidence/umm-0311/package-admission-reuse.json (24/24): reuse-of-unchanged-verified-outputs-accepted, reuse-after-pin-change-rejected, reuse-after-verifier-change-rejected, reuse-after-project-reference-change-rejected, test-tiers-skipbuild-with-old-outputs-refused.
- **R1-F9** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  docs/evidence/umm-0311/package-admission-reuse.json (24/24): main-validator-dll-compiled-against-umm-0.33-rejected (released 0.2.15 DLL, UnityModManager 0.33.0.0), main/observer-validator-extra-UnityModManager.dll-rejected, -extra-0Harmony12.dll-rejected, -extra-Assembly-CSharp.dll-rejected, genuine controls accepted; commands and fixture identities in the report.
- **R1-F10** — `fixed` — commits: `796935cc`, `4bf7dfcb`, `f5a9f386`, `71c04410`, `50c25f25`
  docs/evidence/umm-0311/pinned-loader-msbuild.json (9/9): before-baseline-project-compiles-against-installed-loader on the pinned commit fb743911200f150af407198c6c28519e522ef58c (default parameter, existence and checkout verified before building), independent of origin/main.
- **R1-F11** — `fixed` — commits: `71c04410`, `50c25f25`
  CURRENT.md top: entry points, candidate table for both products (DLL/MVID/references/packages/validators), audit tool sources and invocation, source vs component counts, native prerequisite per row including the installed 0.33.0 row, combined-stack status, the 2026-11-23 entry kept as history; docs/UMM-0311-BASELINE.md round-2 section; the PR #62 description updated to match.
- **R1-F12** — `fixed` — commits: `796935cc`
  Trailing blank lines removed from planning/ENVIRONMENT-FINGERPRINT.json, scripts/Test-Harness.ps1, docs/CHUNK7-NATIVE-LOADER-IDENTITY.md, scripts/Test-Chunk7NativeLoaderContracts.ps1, docs/CURRENT-SUPPORT.md (git diff --check clean).

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 178.0s | Controller receipt on 50c25f25. |
| scripts/Test-PinnedLoaderMsBuild.ps1 (temporary clone of 796935cc) | no | pass | ~5 min | docs/evidence/umm-0311/pinned-loader-msbuild.json (9/9). |
| scripts/Test-PackageAdmissionAndReuse.ps1 (disposable lab clone of f5a9f386) | no | pass | ~8 min | docs/evidence/umm-0311/package-admission-reuse.json (24/24). |
| Test-FrozenProductSource.ps1; Test-Chunk7NativeLoaderContracts.ps1; Test-Harness.ps1 | no | pass | ~25 min | 28/28; 107/107; Test-Harness 280 passed, 3 failed - identical to the pre-reconciliation commit a61c1a5c (test-harness-summary.json: two need deleted Phase 3E artifacts, one composition-boundary check), pre-existing and unrelated. |
| Package.ps1 -IncludeRemovalObserver -ArtifactQualifier umm-0311-f5a9f386; LoaderApiAudit on both packaged DLLs | no | pass | ~3 min | package-validation.json (12/12), observer-package-validation.json (8/8), manifests, api-audit.json and observer-api-audit.json (resolved on 0.31.1, 0.32.4, owner-0.33.0, 0.33.4). |
| Native checks | no | not_run | 0 | Prerequisites per row in CURRENT.md. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
