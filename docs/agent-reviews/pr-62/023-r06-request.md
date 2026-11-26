<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r06-request part=1/1 sha256=cb67dd816bb0a818a773a0b9976fcdc6f5226e69d84c5b9698619536c50d15b6 -->
### Claude Opus — review request — round 6 — candidate `1384140efa`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r06-ad4004` |
| Checkpoint | `final` (final) |
| Candidate H | `1384140efa9ac9b1d8c24909c986a326694bb452` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 6 of 6 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-6 corrections for Mounted Combat PR #62 (owner-authorized additional round, cap 6; validation within the existing remaining allowance, no time added), the complete blocking ledger in one batch, under OWNER AMENDMENT 1 (scope freeze). Scope connection: the local reuse receipt and the archived observer comparison are this mission's tooling that decides whether binaries built against pinned UMM 0.31.1 may be reused (R1-F8); the step-history replacement on an early refusal is a regression of this mission's single-scan Full reuse change (R5-F1). Smallest corrections; no new framework; alternate-loader launcher support stays removed. The accounting disposition of earlier rounds is unchanged. Code and tests bad0ad0c; f7be3057 docs only (CURRENT.md, docs/UMM-0311-BASELINE.md); 1384140e adds only docs/evidence/umm-0311/ (earlier records kept at their original identities). Both DLLs unchanged.

R1-F8: Get-KmcLoaderInputStamp lists tools/Directory.Build.props and tools/Directory.Build.targets (the MSBuild files the removal observer inherits) by path, hashed when present and recorded absent otherwise, independent of Git tracking; Assert-KmcFrozenProductManifest compares them with tools/KmcRemovalObserver for archived observer reuse only, so main-only reuse is unaffected. R5-F1: Update-KmcWorkflowEntry gains -Failed; Test-Tiers passes it for a failed step. The entry is still heartbeated and completed (the attempt is charged), but a failed step replaces the step's recorded duration only when it ran longer than the established one, so a Full run refused by the receipt before the runner keeps the Full-suite duration that governs later admission. The receipt is still checked once (Test.ps1 -SkipBuild inside the admitted umbrella step). Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=umm-0311-baseline python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 214.7s | Controller receipt on 1384140e: TIER FAST PASS=4 FAIL=0 (3.57 min), components 1488/0, Validate-Source 153/0, Test-PackageAdmissionAndReuse 57/0; admitted as budget entry umm-0311-baseline:20261125T155518745Z:35724:cc84560e (214 s). |
| scripts/Test-FrozenProductSource.ps1 and a workflow-budget step-history probe (direct, on the round-6 batch) | no | pass | 11s | docs/evidence/umm-0311/frozen-product-source-r6.log (37/0 on the round-6 code); docs/evidence/umm-0311/workflow-step-history-probe-r6.txt (Update-KmcWorkflowEntry on a temporary budget root seeded with 600 s: a failed 3.2 s step keeps 600, a failed 700 s step records 700, a passed 650 s step records 650). Counted in the budget as imported entries (10.4 s; the 0.4 s probe imported at the 1 s minimum). |
| Workflow budget umm-0311-baseline | no | pass | 0 | Verified before validation at 24.29 of 30.62 min (round-5 state). Round 6: two imported direct runs (11.4 s) and the gate (214 s): 28.05 of 30.62 min, 2.57 min left. No allowance change, no reset; docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r6.json is the record before the gate. |
| Native checks | no | not_run | 0 | Not authorized: the guarded launcher stages into the live installation's Mods folder and no disposable game-copy harness exists for Mounted (CURRENT.md native table). |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/frozen-product-source-r6.log (37/0 on the round-6 code)
- docs/evidence/umm-0311/workflow-step-history-probe-r6.txt
- docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r6.json
- CURRENT.md
- docs/UMM-0311-BASELINE.md
- prepared under owner amendments revision 1 sha256 c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43

**Prior findings to assess:** `R1-F8`, `R5-F1`

**Owner amendments to the mission — revision 1, captured-source sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43`.** Applied by the owner with `resume --amend-mission`; each owner message is preserved verbatim, in order. The effective task is the original mission plus these amendments. The reported work comes from a developer invocation whose input included them, and the reviewer of this round receives a byte-exact copy of the captured document. Public rendering after the controller's standard sanitization; the digest identifies the private captured bytes, which this rendering can differ from.

```text
# Owner amendments to the mission

Cumulative record kept by the agent-review controller. Each owner message below was applied by the owner through `resume --amend-mission` and is preserved verbatim, in order, between its labeled marker lines. Later amendments supersede conflicting earlier requirements; requirements they do not change still apply.

----- BEGIN OWNER AMENDMENT 1 (captured 2026-11-24T21:39:33Z; message sha256 29ebc773d4294f39ce94c84cdd004f90e665f4da94a292a354842ae576b1eb2e) -----
Freeze the UMM 0.31.1 mission scope.

Remaining work must directly establish the agreed UMM baseline, substantiate
the agreed compatibility claims, or resolve a regression introduced by this
mission.

For each proposed correction, identify that connection before implementing it.
Disposition findings outside those boundaries as separate follow-up work
through the existing review process.

Prefer the smallest correction. If unnecessary tooling introduced by this
mission causes a blocker, consider removing that addition rather than
completing or expanding it. Preserve unrelated work and existing safeguards.

Use focused behavioral verification and existing required gates. Do not add
new frameworks, qualification campaigns, or acceptance requirements.

This amendment grants no additional review rounds, validation time, native
execution, merges, or releases.

The endpoint remains reviewable UMM 0.31.1 baseline PRs with accurately stated
compatibility evidence.
----- END OWNER AMENDMENT 1 -----
```

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
