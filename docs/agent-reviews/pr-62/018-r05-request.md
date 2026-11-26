<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r05-request part=1/1 sha256=543a923d41171c085d47c8c07a348241d1aba75d8e08ac651f5abb8384c8d918 -->
### Claude Opus — review request — round 5 — candidate `e7ec0ef8cd`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r05-4431a8` |
| Checkpoint | `final` (final) |
| Candidate H | `e7ec0ef8cdc4383a1c4a010aee29b16cdc6ac631` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `fb743911200f150af407198c6c28519e522ef58c` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 5 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round-5 corrections for Mounted Combat PR #62, the complete blocking ledger in one batch, under OWNER AMENDMENT 1 (scope freeze) and the owner's 2026-11-25 authorization (owner-authorization-20261125-pass3-preserver-dice-mounted.md: fifth round within the cap of five; the round-4 direct focused validation accepted as a one-time documented overrun, reconciled with the append-only import; 10 more minutes from the reconciled total; further direct build/test runs counted). Scope connection: the owner-profile admission receipt (R1-F2) and the local reuse receipt (R1-F8) are this mission's additions and must state the identities they record; the second receipt scan on Full reuse (R3-F2) is a regression of this mission's reuse boundary; R4-F1 is this mission's validation accounting. Smallest corrections; alternate-loader launcher support stays removed; no new framework. Code and tests d5268152; 0c32698c docs only (CURRENT.md, docs/UMM-0311-BASELINE.md); e7ec0ef8 adds only docs/evidence/umm-0311/ (round-4 records kept at their original identities). Both DLLs unchanged (no production input changed after 11df053b).

R1-F2: Get-KmcBaseGameDependencies takes the resolved runtime loader profile (mandatory) and, after measuring the installed loader files, refuses when the installed UnityModManager or Harmony 2 (0Harmony.dll) SHA-256 differs from the profile ('Base-game dependency differs from runtime loader profile <id>: installed Harmony 2 <sha>, profile <sha>'); the launcher passes $runtimeLoaderProfile at its dependency admission, which precedes Enter-KmcModsTransaction, so the admission receipt's profile identities are the verified installed ones. The launcher stays owner-only. R1-F8: the reuse receipt (Get-KmcLoaderInputStamp) lists the repository-root Directory.Build.props and Directory.Build.targets by path, hashed when present and recorded absent otherwise, independent of Git tracking. R3-F2: Test-Tiers no longer runs a separate parent 'reuse check' for Full; Test.ps1 -SkipBuild checks the receipt once inside the admitted umbrella step, before any runner (the same check protects a direct Test.ps1 -SkipBuild). Fast and Candidate keep their admitted reuse-check step; the plan still lists the check and its estimate; nothing is hashed before admission. R4-F1: see the budget validation entry. Native runtime NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=umm-0311-baseline python C:/Dev/agent-review/agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 | yes | pass | 182.8s | Controller receipt on e7ec0ef8: TIER FAST PASS=4 FAIL=0 (3.04 min), components 1488/0, Validate-Source 153/0, Test-PackageAdmissionAndReuse 53/0; admitted as budget entry umm-0311-baseline:20261125T103501414Z:36672:d0514c5e (182 s). |
| scripts/Test-BaseGameIsolation.ps1; scripts/Test-Chunk7NativeLoaderContracts.ps1; scripts/Validate-Source.ps1 (direct, on the round-5 batch) | no | pass | 38s | docs/evidence/umm-0311/base-game-isolation-r5.log (121/0 on the round-5 code); docs/evidence/umm-0311/chunk7-native-loader-contracts-r5.log (LOADER CONTRACT PASS 118 FAIL 0 on the round-5 code); Validate-Source 153/0. Counted in the budget as imported entries (11.7 s, 2.6 s, 23.7 s, each run's own timer), as the owner instructed. |
| Workflow budget umm-0311-baseline (scripts/Reconcile-WorkflowBudget.ps1) | no | pass | 0 | Before validation the ledger was verified at 4 entries, 14.02 of 15 min (the round-3 gates 222 s and 205 s, the failed round-4 gate 203 s, the round-4 gate 211 s). Reconciliation: five imported entries for the round-4 direct runs that never received budget admission (11.5 s, 166 s, 20.9 s, 184 s, 13.9 s; 396.3 s), so 20.62 min; second allowanceAdjustments entry 15 -> 30.62 min (owner: 10 more minutes from the reconciled total). Round 5: three imported direct runs (38.0 s) and the gate (182 s): 24.29 of 30.62 min, 6.33 min left. No reset, no fresh budget; the four original entries, including the failed gate, unchanged. |
| Native checks | no | not_run | 0 | Not authorized: the guarded launcher stages into the live installation's Mods folder and no disposable game-copy harness exists for Mounted (CURRENT.md native table). |

**Evidence references:**
- docs/evidence/umm-0311/INDEX.md
- docs/evidence/umm-0311/workflow-budget-umm-0311-baseline-r5.json
- docs/evidence/umm-0311/workflow-budget-umm-0311-baseline.json
- docs/evidence/umm-0311/base-game-isolation-r5.log
- docs/evidence/umm-0311/chunk7-native-loader-contracts-r5.log
- docs/evidence/umm-0311/package-admission-reuse-r4.json
- CURRENT.md
- docs/UMM-0311-BASELINE.md
- prepared under owner amendments revision 1 sha256 c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43

**Prior findings to assess:** `R1-F2`, `R1-F8`, `R3-F2`, `R4-F1`

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
