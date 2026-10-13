# Chunk 7 bootstrap callsite repair

Status: PASS focused source checks; native replay required. This continues the sustained 7B–7D mission after source checkpoint 2ae504605601695aad2a27b462a5f15f09d0e9bf. It does not qualify a new candidate or the failed preview.5 output.

## Observed defect and change

Preview.5 dispatched the authored native new game and its first Auto save, but the separate Player.GameId setter postfix delivered no identity observation. The original probe recorded entered1/exited1/minted0/saveRequests1/prepared1 and correctly refused the sequence. That native FAIL and its exact restoration receipts remain in [the native record](CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md).

The repaired diagnostic observes the exact installed LoadNewGame callsite immediately after the original Guid assignment. It preserves every native instruction, operand, label and exception block and adds only the observer call. The exact module/member/local contracts and sole uninterrupted receiver/Guid/setter sequence must match. A missing, duplicate or changed site refuses installation. The setter itself is no longer patched; Mono inlining is a hypothesis, not a proven cause.

The observation requires the same admitted Game, Player and SaveManager, the synchronous new-game frame, an actual main actor, one canonical native identity and no prior save request. It does not mint an identity, change native mutation order, prepare actors or change resources. Existing partial-install and failed-unpatch ownership remains retained. The strict reader now requires the three actual patched methods and callsite installation metadata; old setter-only success cannot qualify the new contract.

## Review and focused verification

The independently reviewed map contains ten exact paths: one compiled probe, its focused test/adapter/reader changes, two synthetic proof fixtures and one additive diagnostic child. Eight existing root bases and two absent new paths were checked before integration. No project Compile item, scenario, authority, quota or historical fixture guard changed. All 44 private evidence pins matched. Independent source review and final hash-only closure found no blocker.

Root ran one supervisor on the integrated tree. Four stages passed with actual exit0 in 7.2249797 seconds (host7.348126 seconds):

| Check | Actual result | Seconds |
| --- | --- | --- |
| Release solution compilation | PASS; 450 production sources | 2.2408267 |
| Exact probe regression | 40 PASS / 0 FAIL | 0.8170252 |
| Existing scenario/cleanup adapter | 72 PASS / 0 FAIL | 0.8068045 |
| Strict bootstrap reader | 116 PASS / 0 FAIL | 3.1799444 |

Command: Windows PowerShell NoProfile/NonInteractive -File analysis-cache/chunk7-continuation/root-bootstrap-integration01/Invoke-Focused01.ps1. It selects only the three named checks after compilation; it does not invoke the whole diagnostic wrapper.

The private source lane retained an old native-shaped reproduction (3 PASS / 2 FAIL), then passed the repair. The compiled matcher inspected the exact installed IL read-only (17/0); it neither patched nor invoked the game. Reader shape transition retained3/1 before and4/0 after. Affected bootstrap/cold/Manual/derived/acquired source and reader neighbors passed with their declared substitutes. Their immediate-parent fixtures do not prove the entire native ancestry. The initial tool timeout and two incomplete reader directories remain preserved; no duration is inferred for receipts lacking complete process timing.

These checks substitute native objects, Harmony registration, metadata and artifact boundaries as described in their receipts. Dynamic IL executes the original-shaped setter and actual inserted observer, but proves no Unity/Mono delivery. Native evidence and source tests remain separate.

## Exact receipts and next dependency

All paths below are under analysis-cache/chunk7-continuation:

| Receipt | SHA256 |
| --- | --- |
| bootstrap-probe-next/integration-map01.json | 4e50e1499ee94b35c9f71aae0c60b97f23d9ff7940951ff9df73721369dc45c2 |
| bootstrap-probe-next/verification01.json | 65dec4a58473af811151caea828fc9a1d6e3c801742b6ca1119bf8a7f860ec98 |
| root-bootstrap-integration01/applied01.json | 214de95f609a973749efdef7c7ad156901c449eb42c3202b8de2ab119f621ae8 |
| root-bootstrap-integration01/communicated-review01.json | 2840f8c5408a39bc8a67692ef79241226145bb1916765c30cef46e5a764e1567 |
| root-bootstrap-integration01/focused01.json | a84247945e58e7600e40706cab0c7a788266ab2069604932edfa5eb488671393 |
| root-bootstrap-integration01/caller01.json | 285d219e72ae6a7a73fee4de75005eb935c8627fe33ff889240f8d89e06395fe |

The communicated-review receipt was written by root to record the independent read-only review; it is not reviewer-authored or OS-signed.

Changed observer bytes require a newly frozen Candidate/Kingmaker artifact and the guarded authored-main bootstrap replay. It must observe the identity callback before the first save, settle native workers and restore the actual installation. Only successful same-candidate bootstrap evidence may admit the cold inventory and Manual seed chain. A separately bounded cumulative acquisition path is being implemented to preserve owner1 while acquiring owner2 through ordinary choices. No third/sixth owner, grants or actual actor/resource selections are admitted by that source work.

The next full WhatIf supervisor's generated log-capture test passes7/0 with the strict terminal predicate unchanged. Its width4096 capture avoids the demonstrated wrapping failure. This is preparation only; current source requires its own full qualification and no preview.5 proof may be relabeled as current.

Native budget remains4/25, all four attempts FAIL and settled. Mandatory multi-pair combat/persistence, ordinary acquisition/progression, declared Small, six-pair stress and measured hardening remain open. No breadth-ready or HUMAN PLAY claim is made.
