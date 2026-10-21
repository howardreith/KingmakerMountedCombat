# Chunk 7 F11 measurement design — IN PROGRESS

[Batch122](CHUNK7-BATCH122-F11-MANAGED-MONO-PROBE-2026-10-21.md)
preserves a failed managed-on-Mono embedding probe: the exact Mono DLL faulted
at assembly loading, before callback registration. Independent review found
the process-global profiler install/event controls lack demonstrated exclusive
ownership and reversible teardown in Kingmaker. No KMC-managed profiler is
admitted. The exact installed Unity 2018.4 Recorder metadata includes
`GC.Alloc`-relevant validity, sample-count and thread-filter APIs. A bounded
read-only `Debug.isDebugBuild` observation should ride the next independently
required native case. The official 2018.4 contract limits Recorder to Editor
and Development Players. Only a true development-build result warrants a
guarded validity/positive-control probe; invalid or zero response cannot
become an allocation-rate result.

[Batch121's installed-Mono host discriminator](CHUNK7-BATCH121-F11-INSTALLED-MONO-ALLOCATION-PROBE-2026-10-21.md)
has now proved that a legacy allocation callback on the exact Kingmaker Mono
DLL can count 100 controlled allocations in a disposable process. Its
explicit-cleanup variant failed after callback removal; the process-lifetime
variant exited cleanly. A second process-lifetime host counted the same 100
controlled allocations and 7,400 bytes via the installed object-size API.
These are offline ABI observations, not an admitted game profiler,
allocation-rate result or change to the predeclared comparison rule below.

[Batches101–102](CHUNK7-BATCH101-102-F11-UPDATE-TIMING-2026-10-20.md)
added opt-in KMC Update phase spans and localized the active-pair increase
primarily to `world.UpdatePairs`. A reviewed correction reuses a Unity list in
the mounted pose invariant check rather than allocating a component array each
frame. Separate exact-DLL native 0/1/2/0 sequences passed 8/0 with restoration.
The comparison is diagnostic; allocated-byte/event rate, diagnostics-off and
final-package acceptance remain open.

[Batch80](CHUNK7-BATCH80-F11-REGISTRY-RETIREMENT-2026-10-20.md) reviewed a
second idle-frame allocation correction in registry retirement and ran one
guarded twelve-window 0/1/2/0 comparison on preview.93. Native/outer PASS
8/0 and exact restoration do not establish allocated-byte rate or a causal
cross-candidate speedup. Active-pair timing still exceeds zero-state spread;
diagnostics-off and ordinary-player checks remain open.

[Batch79](CHUNK7-BATCH79-F11-FRAME-SNAPSHOT-2026-10-20.md) corrected two
repeated pair-array copies and successful-visit error-list creation in the
KMC frame path. Reviewed actual-DLL and native checks passed. The twelve
new windows still show active-pair timing above the matched zero-state spread;
they do not measure managed allocation events or establish a causal timing
reduction. Allocation rate, diagnostics-off and ordinary-player gates stay open.

[Batch78](CHUNK7-BATCH78-F11-REPEATED-WINDOWS-2026-10-20.md) completed one
reviewed three-window-per-state 0/1/2/0 native observation on preview.91:
game/outer PASS 8/0 with exact restoration, stable subscriptions and zero
KMC messages. Median KMC Update means were 0.2326/0.2893/0.3035/0.2484 ms.
The final zero-pair mean included a 122.56 ms full-GC-associated outlier.
This advances the repeated-window requirement but does not measure allocation
rate, diagnostics-off behavior or final-package performance acceptance.

[Batch73](CHUNK7-BATCH73-F11-MATCHED-POOL-2026-10-20.md) provides one
current-candidate native 0/1/2/0 resident-pool sequence on preview.87.
It confirms stable subscription counts and zero KMC messages, while its
diagnostic-included KMC Update means increase from about 0.343 ms at zero
pairs to 0.424/0.458 ms at one/two pairs. One window per active state is
insufficient for the three-window comparison below; managed allocation rate,
diagnostics-off behavior and final-candidate acceptance remain unmeasured.

The existing opt-in `MountedRuntimeMeasurement` samples 30 warmup and 120
measured frames. It records inclusive KMC `CompositionRoot.Update` ticks,
whole-frame `Time.unscaledDeltaTime`, KMC log count, retained managed heap,
owned boundaries and native subscriptions. The retained heap delta is **not**
allocation rate. Its output explicitly says so; `Chunk7MatchedPoolEvidence`
keeps `managedAllocationRateMeasured=false`. Neither an old heap delta nor a
process working-set reading may be promoted into an allocation claim.

The accepted owner-seed b3s1d development artifact
`20261018-c7b-world26-ownerseed-b3s1d-save/chunk7-world-evidence.json`
(SHA-256 `01438210fbc9cdf8f8bc228b3d973ffff7220eaa32449bf43f4462fb355723a8`)
observed a 0-pair window with service mean 0.2056 ms, p95 0.2714 ms and
frame p95 11.7062 ms, followed by a 1-pair window with service mean
0.2289 ms, p95 0.2770 ms and frame p95 11.4263 ms. Both recorded zero KMC
log messages. These are two short windows on preview.26, not final-candidate
performance acceptance and not a 2-pair or allocation-rate measurement.

The selected final design uses the same candidate, graphics/settings state,
idle scene, camera, party and unpaused RT mode for 0/1/2 settled pairs. Each
state receives three separately labeled windows, each with declared warmup
and sample counts; the exact actor/owner/gear state is captured at both ends.
No native command, save, diagnostic overlay or area load occurs inside a
window. A conditional six-pair window is added only after an admitted fixture
exists. Matched windows measure KMC Update cost and whole-frame time
separately. Subscription, owner, retained-view and KMC logging counts are
checked before and after every lifecycle cycle and across repeated windows.

For allocation rate, a bounded opt-in counter must measure managed allocation
events or allocated bytes around the same KMC Update boundary. Report bytes
per Update and bytes per elapsed sample second; state explicitly that an
Update-only counter excludes KMC hooks outside Update and other engine
threads. Read-only inspection of the exact installed `mscorlib.dll` and Mono
runtime found no `GC.GetAllocatedBytesForCurrentThread`,
`GC.GetTotalAllocatedBytes`, or corresponding native per-thread export string.
The inspection receipt is
`analysis-cache/chunk7-continuation/f11-allocation-api-20261019/f11-installed-allocation-api.json`,
SHA-256 `c6d77f5d1b49ff7a36040516d666fdfd8f70e5cb0f68c22d5b74627da9721a07`.
An October 20 bounded decompilation checked the alternative
`AppDomain.MonitoringTotalAllocatedMemorySize` on the same installed
`mscorlib.dll` SHA-256
`e6875b3f6d3e681da2fbd11b1077c5550305fb4654eada1113736a08929227e9`:
the getter throws `NotImplementedException`; the monitoring enable setter
also throws and its getter always returns false. This API cannot supply the
F11 counter. The read-only receipt is
`analysis-cache/chunk7-continuation/f11-allocation-api-20261019/f11-appdomain-monitoring-20261020.json`,
SHA-256 `64ebb050ae3b8f0ee1b9f478a57e9e6aa20e8794b25d55a6c3124441674ae26d`.
No monitoring state was enabled and no game was launched for that inspection.
`mono_gc_get_heap_size` and `mono_gc_get_used_size` report heap state, not
allocation throughput. F11 stays **IN PROGRESS** while a supported, scoped
allocation-event measurement seam is established; retained heap cannot stand
in for it.

The exact installed Mono PE export table does contain allocation-profiler
registration and event names, but provides no demonstrated disable,
unregister or destruction path. Installing a process-wide callback from this
mod could retain it after diagnostics stop and interfere with other mods, so
that is not an admitted product instrument. The read-only export/API receipt
is `analysis-cache/chunk7-continuation/f11-allocation-api-20261019/f11-native-profiler-boundary.json`,
SHA-256 `5b502936fce91e59b9fcff781ffb071d5b84ff2c168a76227bbe0efcdd6c89a3`.
An external, process-lifetime profiler on a disposable guarded launch remains
a candidate only after exact installed ABI, bounded output and transaction
behavior are reviewed; this document does not authorize or claim that run.
Read-only installed inventory found no `mono-profiler-log`, `libmono-profiler-*`
or `mprof` binary in Kingmaker, no profiler boot setting and no guarded
launch/output contract. The receipt is
`analysis-cache/chunk7-continuation/f11-allocation-api-20261019/f11-external-profiler-inventory.json`,
SHA-256 `481a171f73a1c0f9ac4cc34e46a31e9a463dcc1f986f325c2abd4727eacd40f3`.
That route therefore also requires reviewed implementation before a measured
F11 allocation result can be claimed.

An October 21 independent read-only review of the locally cached Owlcat
profiler source found that its out-of-process route uses
`DetourCreateProcessWithDlls`, a UnityPlayer PDB beside the game executable,
and client/network startup. Those requirements do not fit this project's
guarded Steam launch and read-only game installation. The source also exposes
a Unity-plugin `StartProfiling` entry point, but that is only a possible attach
pattern, not an approved game-side instrument. A smaller process-lifetime
native shim would still need a reviewed exact-DLL stage/hash and request-only
load, prior-profiler/interference policy for the process-global Mono callback,
an allocation-free nonthrowing bounded callback with overflow and reentrancy
counts, same-thread KMC Update bracketing, output confined to the run root, and
process-exit finalization before Mods restoration. The host's 7,400 bytes for
100 Mono strings are managed object-size bytes, not total Unity/native allocator
bytes. No such shim or safe game attach exists yet. The current Visual Studio
BuildTools installation also exposes no `cl.exe` or `clang.exe` in the inspected
toolchain paths; no compiler or dependency was installed as a workaround.

Before seeing final measurements, the comparison rule is: repeated windows
must retain the same declared actor/owner/subscription population within
each state, and repeated settled lifecycle cycles must return to the same
zero-pair residue and subscription counts. Zero is the expected count of
unprompted KMC warnings/errors in quiet windows. For timing and allocation,
compare the three-window median at 1 and 2 pairs with the matched 0-pair
baseline; use the baseline's observed window-to-window spread as the noise
band, and investigate an increasing per-pair delta beyond that band. Whole
frame p95 is context for player impact, not a proxy for KMC's own cost.
Record the actual deltas even if they are below the investigation threshold;
do not move the comparison rule after observing them.

This document defines the measurement and acceptance comparison only. It
does not claim a new instrument, native run, threshold PASS or ordinary-player
usability result. Final exact-package qualification must bind any F11 evidence
to the delivered binary and diagnostics-disabled controls.
