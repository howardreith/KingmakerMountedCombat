# Chunk 7 batch122: managed-Mono allocation callback probe — FAIL

The batch121 disposable hosts proved the exact installed Mono DLL can deliver
legacy allocation callbacks to an unmanaged host. This separate experiment
asked whether **managed code running inside that Mono runtime** could safely
register the same callback. It did not reach registration.

An isolated .NET Framework 4.7 probe assembly was built with a static rooted
delegate, a bounded 100-string loop, and callback/event removal in `finally`.
A disposable .NET 10 embedding host loaded the exact installed Mono DLL,
selected the game's Managed assembly directory and initialized a Mono domain.
It printed `monoDomainInitialized=True`, then exited 1 before
`mono_domain_assembly_open` returned. Windows Application Error 1000 recorded
`dotnet.exe` faulting in `mono-2.0-bdwgc.dll`, exception `c0000005`, module
offset `000000000001614f`, report
`ac670b55-0095-44f4-a410-d422b6aa0b99`. The crash cause is **not yet
identified**. A mixed CoreCLR/Mono host is not equivalent to Unity's process;
this failure neither validates nor disproves a game-side callback.

Private immutable receipt:
`analysis-cache/chunk7-continuation/f11-mono-managed-embed-host-20261021/receipt.json`,
SHA-256 `58754c6bcdf8d4233c84af3bc0af3dd860b56441b8cd9c0aa5357cd7e344d7fc`.
It pins source and binary hashes for the managed probe and host, installed Mono
hash, exit code and Windows fault identity. The managed callback was **never
installed or invoked**, so no allocation result follows. The isolated host
experiment touched no game, live Mods, profile, save, product DLL or package.

An independent read-only review found **no safe KMC-managed profiler
registration** on present evidence. Legacy profiler installation and event
selection are process-global; the installed API has no demonstrated prior-state
query, callback drain or reversible owner release. Base-only Mods staging does
not prove exclusivity from Unity/UMM profiler state, and a managed allocation
callback could recursively allocate or throw. Do not repeat this host unchanged
or promote its previous unmanaged callback count to game evidence.

The exact installed `UnityEngine.CoreModule.dll` contains
`UnityEngine.Profiling.Recorder.Get`, `isValid`, `enabled`,
`sampleBlockCount`, `FilterToCurrentThread` and
`CollectFromAllThreads`. Unity's
[2018.4 Recorder contract](https://docs.unity3d.com/2018.4/Documentation/ScriptReference/Profiling.Recorder.html)
says the recorder is available only in Editor and Development Players and
`sampleBlockCount` counts the previous frame's Begin/End samples. Its
[current-thread filter](https://docs.unity3d.com/2018.4/Documentation/ScriptReference/Profiling.Recorder.FilterToCurrentThread.html)
explicitly mentions `GC.Alloc` and requires resetting the filter. The local
metadata does not prove this player's `GC.Alloc` handle is valid or responds
to controlled allocations. A follow-up independent review confirmed the
installed `Debug.isDebugBuild` getter and advised reading that single value
in the **next already-required native case**. If false, Unity's documented
Recorder route stops. If true, a bounded `GC.Alloc` validity/positive-control
probe is justified; preserve its prior `enabled` state and reset any thread
filter in `finally`. Even a positive result would measure prior-frame event
count, not allocated bytes, and would need same-window attribution review.

F11 allocation rate and normal-player acceptance remain **IN PROGRESS**.

To carry that discriminator on a required future native case, the existing
two-pair world scenario now records `UnityEngine.Debug.isDebugBuild` once in
its observation root after its ordinary admission. It neither starts a
Recorder nor changes settings or game state. The independent installed-API,
reader-compatibility and ordinary-path review found no concrete defect;
previous artifacts remain valid without the optional new field. The affected
Fast tier (build, component/source checks and world-evidence reader) passed
4/0, with the focused world reader passing 195/0 within it. No native result
has yet supplied this field; its value is unknown. This source change creates
a new compiled observer identity when packaged, so old native PASSes are not
claimed for it.
