<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r01-request part=1/1 sha256=14b055300be71f180f2a0c05264d553a6d039a04f2c5ce211ec882e1d949cb57 -->
### Claude Opus — review request — round 1 — candidate `215d68fe3d`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r01-fd1f0b` |
| Checkpoint | `final` (final) |
| Candidate H | `215d68fe3dea9fc11cdd7d341e428b63d3197c33` |
| Target B (`main`) | `fb743911200f150af407198c6c28519e522ef58c` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 1 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Mounted Combat PR #62 (this repository only), adopted and reconciled with #63. a61c1a5c (existing #62): both Info.json minimums 0.31.1; product and removal observer compile against pinned UMM 0.31.1 / Harmony12 1.2.0.1 in obj/loader-references (scripts/loader-reference-pins.json, Get-PinnedLoaderReferences.ps1; no installed-loader fallback; never packaged); LoaderReferences.targets verifies the cache before reference resolution; package validators inspect packaged DLL references. 0cbf0167 reconciles #63 (5be0f737, merged into codex/mounted-shield): native Windows target aligned to the installed UMM 0.33.0.0 identity (ENVIRONMENT-FINGERPRINT, Test-RuntimeGameResult, Chunk7 cache-creator identity, harness fixtures); loader pins/verifier/MSBuild guard/Build-Local added to the FrozenProductSource production inputs; resolved compiler-input check (UnityModManager and 0Harmony12) in LoaderReferences.targets. #63's separate UMM-only build reference, restore script, its test and records are not carried over (duplicate of this branch's pins; #63 kept the installed Harmony12). 12a97259 drops a duplicated umm-0.31.1 profile from the cherry-pick; 532d6a3f exact pin strings (\z) and paired archive fields, new Test-PinnedLoaderMsBuild.ps1; 272238ce refusal cases build without clean; 0be6e3ae docs only; 215d68fe adds only docs/evidence/umm-0311/. Mounted Shield PR #61 and its enrollment untouched. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PinnedLoaderMsBuild.ps1 | yes | pass | 96.0s | Controller receipt on 215d68fe: build, Fast components, Validate-Source and the pinned-loader MSBuild test. |
| scripts/Test-PinnedLoaderMsBuild.ps1 (temporary clone of 272238ce) | no | pass | ~3 min | 6/6; docs/evidence/umm-0311/pinned-loader-msbuild.json (baseline fb74391120 compiles against installed 0.33.0.0). |
| scripts/Build-Local.ps1; scripts/Package.ps1 -ArtifactQualifier umm-0311-272238ce | no | pass | ~3 min | Component tests 153/153; DLL d20474bf... MVID 9db1be50...; package df1eb771... validation 12/12 (package-validation.json, package-manifest.json); observer DLL references also 0.31.1.0/1.2.0.1. |
| Test-FrozenProductSource.ps1; Test-Chunk7NativeLoaderContracts.ps1; Test-UmmLoaderContractChecks.ps1 | no | pass | ~2 min | 25/25; 101/101; 22 passed with 4 unassessed inputs. |
| LoaderApiAudit on d20474bf (0.31.1, 0.32.4, owner-0.33.0, 0.33.4); profile inventory | no | pass | ~2s | api-audit.json (14 types/42 members resolved on all four); loader-profile-inventory.json. Static only. |
| Native checks | no | not_run | 0 | Native harness targets the installed 0.33.0 identity; no native run performed. |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/pinned-loader-msbuild.json
- docs/evidence/umm-0311/package-validation.json
- docs/evidence/umm-0311/api-audit.json
- docs/UMM-0311-BASELINE.md

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
