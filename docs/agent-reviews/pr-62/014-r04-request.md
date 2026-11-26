<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r04-request part=1/1 sha256=20dbcd66fd3dcf6f3282c9a366b0db449a8143dbb7bc1730953b7fa4ac87dc6f -->
### Claude Opus — review request — round 4 — candidate `13bf09d8e9`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r04-542323` |
| Checkpoint | `final` (final) |
| Candidate H | `13bf09d8e90010f26a7a1758efa007455c1ba40d` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 4 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-4 corrections for Mounted Combat PR #62, the complete blocking ledger in one batch, prepared under OWNER AMENDMENT 1 (scope freeze). Code d63d4cc0 and test fix dbe51d82; 3edbac00 and 85a57088 docs only; 0413ac8d and 13bf09d8 add only docs/evidence/umm-0311/ (round-3 records kept at their original identities, round-4 records added as *-r4). Both DLLs unchanged (no production input changed after 11df053b). Scope connection: the archived and local reuse receipts are this mission's tooling that decides whether a DLL built against pinned UMM 0.31.1 may be reused, so missing inputs (R3-F1, R1-F8), an unenforced direct boundary (R1-F8), a misleading plan line (R2-F2) and hashing before budget admission (R3-F2) are corrected minimally; R3-F3 is a regression this mission introduced in the launcher's parameter positions; R1-F11 and R3-F4 correct the stated evidence. R1-F2's remaining requirement (admitting a selected non-owner loader profile at the launcher against the target's files) serves native runs, which the amendment does not grant: following its removal guidance, the launcher-side selection this mission added in round 2 is removed rather than completed, which also restores the parameter positions; R1-F2 is rebutted as follow-up. Optional R3-F5 deferred. Budget: the existing mission allowance umm-0311-baseline (15 min, owner adjustment recorded); verified before validation at 2 entries, 7.12 min. A first round-4 gate on 0413ac8d failed (203.5 s): 1,488 components and 153 source checks passed but two new budget-order cases started a nested tier against a disposable budget while the gate entry was active, which the budget rules refuse; dbe51d82 runs them as top-level invocations (reproduced outside the gate with a simulated enclosing entry: 51/51). The final gate took 211.5 s; the allowance now stands at 14.02 of 15 min. No fresh budget. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=umm-0311-baseline python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 211.5s | Controller receipt on 13bf09d8 (log gate-focused-13bf09d8e900-d43d449b6c4-aa0bb756c59-20261124T215622539605Z.log): TIER FAST PASS=4 FAIL=0 (build, FAST components TOTAL PASS=1488 FAIL=0, FAST Validate-Source TOTAL PASS=153 FAIL=0, FOCUSED Test-PackageAdmissionAndReuse TOTAL PASS=51 FAIL=0); budget umm-0311-baseline now 14.02 of 15 min (four entries, including the failed first round-4 gate). |
| scripts/Test-PackageAdmissionAndReuse.ps1 (disposable lab clone of dbe51d82, with a simulated enclosing workflow entry) | no | pass | 184s | docs/evidence/umm-0311/package-admission-reuse-r4.json (51/51, disposable lab clone of dbe51d82, run with a simulated enclosing workflow entry as under the gate); run directly (not a budgeted tier). |
| scripts/Test-FrozenProductSource.ps1; scripts/Test-Chunk7NativeLoaderContracts.ps1; scripts/Validate-Source.ps1 at d63d4cc0/13bf09d8 | no | pass | ~3 min | docs/evidence/umm-0311/frozen-product-source-r4.log (FROZEN PRODUCT SOURCE PASS=32 FAIL=0 at d63d4cc0); docs/evidence/umm-0311/chunk7-native-loader-contracts-r4.log (LOADER CONTRACT PASS 117 FAIL 0 at d63d4cc0); Validate-Source TOTAL PASS=153 FAIL=0 on 13bf09d8. |
| Native checks | no | not_run | 0 | Not authorized: the guarded launcher stages into the live installation's Mods folder and no disposable game-copy harness exists for Mounted (CURRENT.md native table). |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/package-admission-reuse-r4.json
- docs/evidence/umm-0311/frozen-product-source-r4.log
- docs/evidence/umm-0311/chunk7-native-loader-contracts-r4.log
- docs/evidence/umm-0311/workflow-budget-umm-0311-baseline.json
- CURRENT.md
- docs/UMM-0311-BASELINE.md
- prepared under owner amendments revision 1 sha256 c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43

**Prior findings to assess:** `R1-F11`, `R1-F2`, `R1-F8`, `R2-F2`, `R3-F1`, `R3-F2`, `R3-F3`, `R3-F4`, `R3-F5`

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
