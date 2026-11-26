<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r03-request part=1/1 sha256=806be39deec46440dcf75aff54fd4c8d89346f484c8fdf2770e9b118742794ea -->
### Claude Opus — review request — round 3 — candidate `293668a4c0`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r03-735731` |
| Checkpoint | `final` (final) |
| Candidate H | `293668a4c0250794f95d1dd36afee373ef75412e` |
| Target B (`main`) | `fb743911200f150af407198c6c28519e522ef58c` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 3 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-3 corrections for Mounted Combat PR #62 (this repository only). Code 11df053b; 9d971620, b0362d99 and 293668a4 docs only; e4e32899 and d8376646 add only docs/evidence/umm-0311/. New LoaderReferences.Final.targets is imported after Microsoft.CSharp.targets in both projects, so the final compiler-input check runs after the standard and repository-level hooks (including a Directory.Build.targets BeforeTargets=CoreCompile hook) and reads digests from the parsed pin definition. scripts/LoaderPins.ps1 (Read-KmcLoaderPinDefinition) is one side-effect-free parser for Get-PinnedLoaderReferences and both standalone validators (both canonical records, minimum equal to the UMM pin, no download or cache needed). One standalone measurement per package (Get-KmcMeasuredPackage) is reused by common and normal-player admissions; DLL SHA-256, MVID, entries and product version are compared before a request or suite snapshot is created. The guarded launcher resolves the runtime loader profile (-RuntimeLoaderProfileId, default the measured owner installation) and carries it to dependency admission, the native inventory, the result reader and the admission receipt; the loader stack is verified without the optional cache/shared-state observations. LoaderInputStamp binds compiled sources, the build bootstrap and output bytes (both shipping DLLs and the component runner); Fast, Candidate and Full consult it before reuse and plan mode lists it. loader-profile-inventory.json restored unchanged from its original Git blob; evidence index names the producer of each file. Gate run under the owner-authorized mission budget umm-0311-baseline (15 min; prior development ledger exhausted and unchanged): gate on d8376646 3.7 min, then the corrective rerun on 293668a4 (entry documents reconciled) 3.4 min: 7.12 of 15 min used. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=umm-0311-baseline python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 205.2s | Controller receipt on 293668a4: TIER FAST PASS=4 FAIL=0; focused Test-PackageAdmissionAndReuse TOTAL PASS=45 FAIL=0; budget umm-0311-baseline spent 7.12 of 15 min (two entries: the same gate on d8376646 passed in 222.6s before the docs-only correction 293668a4). Budget record created 2026-11-24 with the owner's authorization recorded in allowanceAdjustments; development ledger (91.42 of 90 min) unchanged (sha256 1365a39f before and after). |
| scripts/Test-PinnedLoaderMsBuild.ps1 (temporary clone of 11df053b) | no | pass | ~6 min | docs/evidence/umm-0311/pinned-loader-msbuild.json (12/12, produced at 11df053b). |
| scripts/Test-PackageAdmissionAndReuse.ps1 (disposable lab clone of 11df053b) | no | pass | ~8 min | docs/evidence/umm-0311/package-admission-reuse.json (45/45, produced at 11df053b). |
| Test-Chunk7NativeLoaderContracts.ps1; Test-FrozenProductSource.ps1; UMM loader contract checks | no | pass | ~6 min | 116/116; 28/28; 23 passed, 2 unassessed by design (a loader not on this machine is reported unassessed, never passed). Logs in docs/evidence/umm-0311/. |
| scripts/Test-Harness.ps1 (at e4e32899 = 11df053b plus docs/evidence only) | no | fail | ~20 min | 280 passed, 3 failed: the same three failures as the f5a9f386 baseline and the pre-reconciliation commit a61c1a5c (docs/evidence/umm-0311/test-harness-summary.json, qualifying: no game process). An earlier run during another session's game process (222/61) was not qualifying and is not counted. |
| Package.ps1 -IncludeRemovalObserver -ArtifactQualifier umm-0311-11df053b; LoaderApiAudit | no | pass | ~3 min | package-validation.json, observer-package-validation.json and manifests (main 89352f07..., observer 43c5fc82...); DLL bytes unchanged from round 2 (d20474bf, f9697f91), so the static audits (resolved on 0.31.1, 0.32.4, owner-0.33.0, 0.33.4) are reused and labelled as such. |
| Native checks | no | not_run | 0 | Not authorized in this mission: the guarded launcher stages into the live installation's Mods folder; a disposable game-copy harness for Mounted does not exist. 0.31.1/0.32.4/0.33.4 additionally need a reviewed runtime profile and a guarded environment (CURRENT.md native table). |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/package-admission-reuse.json
- docs/evidence/umm-0311/pinned-loader-msbuild.json
- docs/evidence/umm-0311/chunk7-native-loader-contracts.log
- docs/evidence/umm-0311/test-harness-summary.json
- docs/evidence/umm-0311/loader-profile-inventory.json
- CURRENT.md
- planning/UMM-LOADER-PROFILES.json

**Prior findings to assess:** `R1-F1`, `R1-F11`, `R1-F2`, `R1-F4`, `R1-F6`, `R1-F8`, `R2-F1`, `R2-F2`, `R2-F3`, `R2-F4`, `R2-F5`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
