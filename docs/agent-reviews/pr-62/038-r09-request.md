<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r09-request part=1/1 sha256=a7ba9d5ff7b3984f28075355fdeeffa7e2939a54b7b2ce7e4985570743e55fde -->
### Claude Opus — review request — round 9 — candidate `5b03449e5b`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r09-1f7913` |
| Checkpoint | `final` (final) |
| Candidate H | `5b03449e5b02c5a5359f537a09e727f32aa6e0b4` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 9 of 9 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-9 correction for Mounted Combat PR #62 (owner-authorized additional review, total cap 9; one +5-minute adjustment of the existing allowance, recorded in allowanceAdjustments), R8-F1 only, under OWNER AMENDMENT 1 (scope freeze). Scope connection: R8-F1 is a side effect of this mission's round-8 change that excluded the Full receipt step from the estimate. The correction is limited to the Full -SkipBuild admission estimate in Test-Tiers.ps1; the child outcome mechanism, duration recording, ledger schema, compiler pins, packaging, native launcher and gameplay are unchanged. Code and test e3723c0d; 4ec1c5a5 docs only (CURRENT.md, docs/UMM-0311-BASELINE.md); 5b03449e adds only docs/evidence/umm-0311/ (earlier records kept at their original identities). PR #62's merge conflict with main is recorded separately and not resolved here.

R8-F1: after the existing estimate calculation, when the tier is Full, -SkipBuild is selected, no 'FULL umbrella (scripts/Test.ps1)' duration is recorded and a 'reuse check' duration is, that receipt duration becomes the admission lower bound for selection (the admission estimate) and the FULL umbrella step (its step estimate); the total estimate stays unknown, the plan's Full line shows the bound, and nothing is written to Full history. With an inclusive Full duration it is used once; with neither duration the unknown-estimate policy is unchanged; Full with a build, Fast and Candidate are unchanged. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=umm-0311-baseline python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 239.6s | Controller receipt on 5b03449e: TIER FAST PASS=4 FAIL=0 (3.99 min), components 1488/0, Validate-Source 153/0, Test-PackageAdmissionAndReuse 65/0; admitted as budget entry umm-0311-baseline:20261126T045117508Z:37956:855ad540 (239 s). |
| R8-F1 reproduction (Test-Tiers plans and one admission-refused run against temporary budgets); scripts/Test-TierSelection.ps1 (direct, on the round-9 batch) | no | pass | 45s | docs/evidence/umm-0311/r8f1-reproduction.txt (A: inclusive Full 200 s with receipt 120 s plans estimate=200s; B: receipt 600 s, no Full history plans estimate=unknown with the Full line 'last=unknown (admission lower bound 600s from the recorded reuse check)', and a 1-minute allowance refuses the run as known oversized work with no entry and no Full history; C: neither recorded stays unknown without a bound; D: Full with a build shows no bound; Fast still lists the receipt step with last=600s); docs/evidence/umm-0311/tier-selection-r9.log (29/0 on the round-9 code). Counted in the budget as imported entries (8.2 s and 36.5 s). |
| Workflow budget umm-0311-baseline | no | pass | 0 | Live ledger read before admitting work: 37.79 of 40.62 min, 4 adjustments. One owner-authorized adjustment recorded (40.62 -> 45.62 min; authorizing assignment and text in the record; refuses re-application). Round 9: two imported direct runs (44.7 s) and the gate (239 s): 42.52 of 45.62 min, 3.1 min left. docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r9.json is the record before the gate. |
| Native checks | no | not_run | 0 | Not authorized: the guarded launcher stages into the live installation's Mods folder and no disposable game-copy harness exists for Mounted (CURRENT.md native table). |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/r8f1-reproduction.txt
- docs/evidence/umm-0311/tier-selection-r9.log (29/0 on the round-9 code)
- docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r9.json
- CURRENT.md
- docs/UMM-0311-BASELINE.md
- prepared under owner amendments revision 1 sha256 c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43

**Prior findings to assess:** `R8-F1`

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
