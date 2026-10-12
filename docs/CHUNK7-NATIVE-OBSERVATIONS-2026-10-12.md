# Chunk 7 native checkpoint — 12 October 2026

Status: IN PROGRESS. Neither selected exploration run passed the full scenario.
Root was the sole live executor; both transactions are settled. No native save was written.

Frozen product: b97db97f8f9641eea802a725295161ebdac4ad03, preview.3,
DLL SHA256 096671ee0629acd32d1c3a16906bac508e7afa2d6b2a75d5890ddd2755aee2a4,
MVID 76544775-d44a-443b-8de7-12f2b3e191fa. Package SHA256
6e3de9177c92accb009145bcda6bb03be127fec2bdf2460851bceb3276678418.
Harness HEAD: e557064c773b07a1dc036b7ab8a015f4492fc672. Suite:
chunk7b-world03-20261012, SHA256 aa94fa4c955fccf66e8273aa6af228448c1518ec18d5dc04f7dbc10c2da29cfd.
Private execution manifests and raw logs remain under analysis-cache/chunk7-continuation;
original native records remain under runtime-evidence/<run-id>.

## Actual execution

The unchanged full guarded WhatIf passed in 9400.5291734 seconds (156m40s),
400.5291734 seconds beyond its separately declared 150-minute planning estimate.
It completed without live mutation. No cancelled audit is counted as PASS.

| Run | Configuration | Duration | Actual result |
| --- | --- | ---: | --- |
| 20261012-chunk7b-world03 | KMC with installed UMM/Harmony only | 133.9013171 s | FAIL before fixture completion, 0 passed / 1 failed assertion |
| 20261012-chunk7b-world03-installed-control | Current installed Mods cloned transactionally, unchanged candidate | 105.9559007 s | FAIL, 4 passed / 1 failed assertion |

The first run failed in native Player.PostLoad (token 06000DC7): the saved
MainCharacter identity was absent from CrossSceneState.AllEntityData at the
native Single predicate. Horse registration separately rejected a Ranger
AllFeatures count different from the strict seven-GUID contract. The actual
isolated list has not yet been observed. The loader reader also rejected the
installed HarmonySharedState in-memory assembly and UMM's candidate cache path;
the independently reviewed loader-only correction passes 93 focused checks. It accepts only the observed pinned creator/cache contracts, preserves candidate hashes/MVIDs, and does not change the native FAIL.

The comparison changed the configuration to distinguish the isolation dependency;
it is not base-game proof or a general compatibility matrix. It verified Working
load, native diagnostic Ranger choice commits, and two normal native Mount inputs.
Both exact command/process bindings survived selection changes, with conserved
exploration resources. This fixture construction is not ordinary acquisition proof.
The first ground click then failed the exact-command assertion. Runtime cleanup
passed; independent movement, unmounted movement control and repeated settled cycles
remain unproven. The raw failed artifact remains FAIL and cannot qualify source saves.

## Preliminary measurements

The comparison recorded 30 warmup frames and 120 samples in each completed phase:

| Phase | KMC update median / p95 | Whole-frame median / p95 | KMC messages |
| --- | --- | --- | ---: |
| Zero pairs | 0.1609 / 0.2766 ms | 11.0100 / 11.6784 ms | 0 |
| One pair | 0.2292 / 0.3181 ms | 11.0047 / 11.5239 ms | 0 |
| Two pairs | 0.2462 / 0.3489 ms | 11.0069 / 11.5655 ms | 0 |

These include native diagnostic observer overhead and the installed configuration.
There is no settled-zero or six-pair sample, allocation-rate observation, accepted
performance threshold, or hardening PASS. Process heap deltas are not allocations.
The native catalogue observed Gnome and Halfling Small races; it did not create or
qualify a Small mounted body/pose. Both existing rider fixtures lacked natural next-level XP.

## Restoration and next dependencies

Both combined transaction receipts report phase=restored, no restoration errors,
Mods restored, protected saves passed, Baseline immutable, Working restored and
save-write allowlist passed. Before/after Mods digest:
02b2efc8c35dc0bbd7b041b6fa1f6f9888b51bf0490ab7b9ca34dc0901977dc0.
Before/after save inventory digest:
2e69c4a4b4611327b0acb09d002a996e42980ff23f2ce256ac3d7dd064d9c74a.
The isolated run additionally verifies exact profile bytes and unchanged loader
dependencies. Both attributed game processes exited and the transaction lock was released.

Automatic approval review refused direct save archive inspection and then the
existing read-only fixture guard because both parse save contents. It permitted
only names/lengths/timestamps/raw-hash continuity inspection. No such rejected
command ran; no alternative archive reader was used. Native gameplay comparison
was separately approved through the unchanged guarded runner. Further archive-content
diagnosis needs that access boundary resolved; native logs/source work remain available.

The ground callback defect was reproduced 12 PASS / 7 FAIL and repaired 19/0: a per-owner
stock-intent predicate prevents post-input cleanup from cancelling the fresh move.
Explicit Stop and pre-admission cancellation are unchanged; native rerun is required.
The strict Ranger option guard now reports a bounded observed list on refusal (11/0);
its seven-option acceptance rule is unchanged and the actual base-only list remains unknown.

The subsequent 140-path combined source checkpoint passes production compilation, 805
component tests and 22 selected non-archive checks. Progression diagnostics are integrated;
closed native bootstrap review is still pending. These offline results do not change the
native verdicts above. Next: finish bootstrap integration and affected checks, freeze the
reviewed candidate, then rerun the affected native rows. The original preview.3 evidence
and all failed reproductions remain immutable.
A qualified base-only fixture/registration path remains a required dependency. Missing
native combat/persistence/progression/Small/six-pair evidence and human play/visual review
remain open. No 7B–7D completion, owner-play readiness, or 7E promotion is claimed.
