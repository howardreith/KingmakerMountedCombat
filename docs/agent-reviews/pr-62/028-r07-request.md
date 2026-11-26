<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r07-request part=1/1 sha256=34566ab1908f9cf92585237fadc788bdbb041c671bbc6d4b66dce42b79829409 -->
### Claude Opus — review request — round 7 — candidate `85d18da514`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r07-4f34ef` |
| Checkpoint | `final` (final) |
| Candidate H | `85d18da51490e4e78bc2ef4c1fbc58b658b75057` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 7 of 7 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-7 corrections for Mounted Combat PR #62 (owner-authorized additional review, cap 7; one +5-minute adjustment of the existing allowance, recorded in allowanceAdjustments), the three blocking findings in one batch, under OWNER AMENDMENT 1 (scope freeze). Scope connection: the archived observer and shared-source comparisons decide whether binaries built against pinned UMM 0.31.1 may be reused (R1-F8); the tier-selection fixture regression (R6-F1) and the invented Full duration (R6-F2) were introduced by this mission's receipt and single-scan changes. Smallest corrections: the round-6 failure-history switch is removed rather than extended. The earlier accounting finding stays resolved. Code and tests 53c987fa; 1e175383 docs only (CURRENT.md, docs/UMM-0311-BASELINE.md); 85d18da5 adds only docs/evidence/umm-0311/ (earlier records kept at their original identities). Both DLLs unchanged. PR #62 currently reports a merge conflict with main; it is recorded separately and not resolved in this batch (no merge or rebase).

R1-F8: FrozenProductSource.ps1 matches the inherited MSBuild files with case-insensitive exact Git pathspecs: ':(icase)Directory.Build.props' and ':(icase)Directory.Build.targets' in the shared production-input comparison, ':(icase)tools/Directory.Build.props' and ':(icase)tools/Directory.Build.targets' only in the observer-specific comparison, so main-only reuse still ignores tools/. R6-F1: Test-TierSelection.ps1 copies the real LoaderInputStamp.ps1 and excludes it from tracing-stub generation, initializes the boundary as an empty Git repository, creates the Debug output placeholders (both DLLs; the stub component runner already exists) and writes a current Debug receipt with Write-KmcLoaderInputStamp, checked by Test-KmcLoaderInputStamp; all existing assertions run unchanged. R6-F2: WorkflowBudget.ps1 returns to its round-5 content (no -Failed switch); Test-Tiers.ps1 records a step duration as before except that a failed FULL umbrella step only heartbeats (Update-KmcWorkflowEntry without a step duration), so a refused Full attempt is charged through its entry and leaves Full history unchanged: established, or unknown. The receipt is still checked once. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=umm-0311-baseline python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 244.4s | Controller receipt on 85d18da5: TIER FAST PASS=4 FAIL=0 (4.07 min), components 1488/0, Validate-Source 153/0, Test-PackageAdmissionAndReuse 58/0; admitted as budget entry umm-0311-baseline:20261126T024815297Z:20992:eabb1fd1 (244 s). |
| scripts/Test-FrozenProductSource.ps1; scripts/Test-TierSelection.ps1 (direct, on the round-7 batch) | no | pass | 45s | docs/evidence/umm-0311/frozen-product-source-r7.log (41/0 on the round-7 code); docs/evidence/umm-0311/tier-selection-r7.log (29/0 on the round-7 code). Counted in the budget as imported entries (11.0 s and 34.0 s, each run's own timer). The tier-selection fixture uses stub children and a stub component runner; no Full product suite ran. |
| Workflow budget umm-0311-baseline | no | pass | 0 | Live ledger read before admitting work: 28.05 of 30.62 min, 2 adjustments. One owner-authorized adjustment recorded (30.62 -> 35.62 min; authorizing assignment and text in the record). Round 7: two imported direct runs (45.0 s) and the gate (244 s): 32.86 of 35.62 min, 2.75 min left. No reset, no fresh budget; docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r7.json is the record before the gate. |
| Native checks | no | not_run | 0 | Not authorized: the guarded launcher stages into the live installation's Mods folder and no disposable game-copy harness exists for Mounted (CURRENT.md native table). |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/frozen-product-source-r7.log (41/0 on the round-7 code)
- docs/evidence/umm-0311/tier-selection-r7.log (29/0 on the round-7 code)
- docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r7.json
- CURRENT.md
- docs/UMM-0311-BASELINE.md
- prepared under owner amendments revision 1 sha256 c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43

**Prior findings to assess:** `R1-F8`, `R6-F1`, `R6-F2`

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
