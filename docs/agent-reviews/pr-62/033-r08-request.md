<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r08-request part=1/1 sha256=b4d9cb5192adb44832d3d74cd6f2275cccbae7e58c6c8ed0ce010da97cc0c7fb -->
### Claude Opus — review request — round 8 — candidate `bc38080d5e`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r08-af6281` |
| Checkpoint | `final` (final) |
| Candidate H | `bc38080d5e634f43d911202b4ddf7534dbdca3e1` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 8 of 8 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-8 corrections for Mounted Combat PR #62 (owner-authorized additional review, total cap 8; one +5-minute adjustment of the existing allowance, recorded in allowanceAdjustments), R7-F1 and R7-F2 in one batch, under OWNER AMENDMENT 1 (scope freeze). Scope connection: both findings are in this mission's single-receipt-scan Full reuse and its budget accounting (the round-7 'record only after success' rule and the separately estimated receipt step). The round-7 rule is replaced by the owner's outcome contract; no journal, lock or status framework was added and the receipt is still scanned once. Code and tests 9a11e0a6; a8f94888 docs only (CURRENT.md, docs/UMM-0311-BASELINE.md); bc38080d adds only docs/evidence/umm-0311/ (earlier records kept at their original identities). Both DLLs, compiler pins, packaging, the native launcher and gameplay unchanged. PR #62's merge conflict with main is recorded separately and not resolved in this batch.

R7-F1: LoaderInputStamp.ps1 defines $KmcReceiptRefusedExitCode = 51417; Test.ps1 -SkipBuild exits with it when the receipt refuses reuse before any build or test work (the message is still printed). Test-Tiers records the exit code of its test child (Invoke-TestScript) and, for the FULL umbrella step only, a child that exited with that code is a no-work refusal: the attempt heartbeats and is charged through its entry and Full history is left as it was (established or unknown). Every other outcome records the step's measured duration: a Full run whose work started records its cost whether it passes or fails, and that cost governs the next admission. R7-F2: the Full receipt check stays a plan step marked 'timed within FULL umbrella' and is excluded from the estimate and admission sum, so an inclusive Full duration is counted once and a missing standalone receipt duration no longer makes a known Full estimate unknown; Fast and Candidate keep their separately timed receipt step. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=umm-0311-baseline python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 259.3s | Controller receipt on bc38080d: TIER FAST PASS=4 FAIL=0 (4.32 min), components 1488/0, Validate-Source 153/0, Test-PackageAdmissionAndReuse 64/0; admitted as budget entry umm-0311-baseline:20261126T033909306Z:11492:17f2c182 (259 s). |
| scripts/Test-TierSelection.ps1; scripts/Test-Tiers.ps1 -Plan (Full and Fast) (direct, on the round-8 batch) | no | pass | 37s | docs/evidence/umm-0311/tier-selection-r8.log (29/0 on the round-8 code); docs/evidence/umm-0311/tier-plan-probe-r8.log (the Full plan lists the receipt step as 'timed within FULL umbrella', Fast lists it with last=). Counted in the budget as imported entries (33.6 s and 3.0 s). The tier-selection fixture uses stub children; no Full product suite ran. |
| Workflow budget umm-0311-baseline | no | pass | 0 | Live ledger read before admitting work: 32.86 of 35.62 min, 3 adjustments. One owner-authorized adjustment recorded (35.62 -> 40.62 min; authorizing assignment and text in the record; refuses re-application). Round 8: two imported direct runs (36.6 s) and the gate (259 s): 37.79 of 40.62 min, 2.83 min left. docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r8.json is the record before the gate. |
| Native checks | no | not_run | 0 | Not authorized: the guarded launcher stages into the live installation's Mods folder and no disposable game-copy harness exists for Mounted (CURRENT.md native table). |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/tier-selection-r8.log (29/0 on the round-8 code)
- docs/evidence/umm-0311/tier-plan-probe-r8.log
- docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r8.json
- CURRENT.md
- docs/UMM-0311-BASELINE.md
- prepared under owner amendments revision 1 sha256 c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43

**Prior findings to assess:** `R7-F1`, `R7-F2`

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
