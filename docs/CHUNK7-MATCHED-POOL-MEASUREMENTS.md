# Comparable resident-pool measurements

Status: IN PROGRESS. This adds measurement support for the selected Chunk 7D
campaign; native timing and retention acceptance remain open.

The two-pair exploration scenario keeps its original world evidence and adds
`chunk7-matched-pool-measurements.json`, contract
`chunk7-matched-resident-pool-v1`. The new artifact binds the exact original
world-artifact hash and candidate identity. Old evidence is not upgraded into
this contract by a newer reader.

The first existing mount/dismount cycle warms the resident services. Four windows
then observe zero pairs after cycle one, one and two pairs during cycle two, and
zero pairs after cycle two. The original three cycles and twelve native mounted
ability inputs remain unchanged. Sampling adds no command, resource reset,
preparation, save, load or mode transition.

Every window retains and compares the same 22 distinct native object references:
world, Game, Player, persistent/area state and main actor; two relationship
boundaries and two turn participants; and four actor/descriptor/view triples.
It also validates the actual owner/generation sequence and active pair counts
0/1/2/0. Replaced or aliased references, unsettled native command slots,
preparation, shells, loading, workers or serialization refuse sampling. A
finished command may remain in its native slot briefly; the observer waits for
native Empty within the existing scenario deadline, without clearing the slot
or issuing another input. Sampler and identity custody remain owned until
release succeeds.

Reported Update cost includes KMC safety, scenario and sampler work. Whole-frame
elapsed time also includes the game and rendering. Managed heap occupancy and GC
collection counts are observable, but heap growth is not allocated bytes, an
allocation rate, or proof of a leak. Equal resident pools remove one known
confounder; time/order effects and observer overhead remain. Quiet diagnostics
still perform heartbeat, world and command polling, so these windows are not a
measurement of fully inactive diagnostics.

No optimization or numeric performance acceptance follows from the synthetic
checks. Native comparison requires an admitted field fixture and all four
settled windows on the identified candidate. Six-pair stress, repeated area/load
settlement, and actual diagnostics-off comparison remain separate required
observations. The earlier failed preview.3 run lacks a settled-zero window and
uses unmatched resident pools; its original evidence and limitations remain.

## Exact installed observation limits

A metadata-only review of the installed runtime on October 13 confirmed that its System.GC has neither GetAllocatedBytesForCurrentThread nor GetTotalAllocatedBytes. Unity's available Mono heap/used-size APIs measure occupancy. Its native memory-manager total is not a demonstrated cumulative managed-byte counter. ProfilerRecorder is absent. Recorder.sampleBlockCount is an event count and can return zero for an invalid handle; GC.Alloc marker availability and frame/thread/reset semantics remain unobserved. No profiler state was enabled or changed, and no native method was called for this review. An allocation rate is therefore unavailable on the current evidence.

The Update timer measures inclusive wall-clock work inside CompositionRoot.UpdateCore. It includes safety, scenario, telemetry and sampling work, and excludes callbacks or LateUpdate work outside that method. It is not total KMC CPU time. KMC log counters count logger calls and UTF-16 character lengths, not complete file bytes or all game logging. EventBus snapshots count selected KMC-assembly entries in the inspected native lists; they do not enumerate every Harmony, Unity, delegate or static reference. Deliberately resident boundaries and participants are distinct from active, failed and unsettled owners. None of these endpoint counts alone proves GC reclamation or absence of a leak.

The first timing sample includes reset heap/GC observation; log counters omit the first measured Update before the sample count becomes one. These limits remain part of any comparison. The metadata review supplies no numeric performance result, optimization justification, allocation PASS or new native qualification. Raw assembly identities and original member inventories remain in the private measurement-metadata-review01.json record under breadth-intake-integration01.
