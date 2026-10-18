# Chunk 7 diagnostic focused checks

These are offline component, source-adapter, strict JSON-reader and real private-filesystem checks. They do not launch Kingmaker, write native saves, stage a candidate, or establish native gameplay proof. Generated executables, extracted adapter method bodies and synthetic artifacts remain under the project analysis-cache. The extracted source adapters replace only external native boundaries. The persistence cleanup adapter executes the unchanged product artifact writer against a real private filesystem.

Run the relevant scripts with Windows PowerShell from the repository. The shared initializer resolves the current repository and project lab; no private staging paths are required. All production C# remains in src/KingmakerMountedCombat/Diagnostics; test-only template C# is in tests/Chunk7Diagnostics and must not be compiled into the mod.

Selected cleanup regression checks:

- Test-Chunk7WorldCleanupAdapter.ps1: actual exploration engine cleanup methods, native command/process/worker/queued-load/lease ownership.
- Test-Chunk7HostOwnedEngines.ps1: actual host completion/owner/world adapters, references retained until proven Dispose, queued loads and early serialization barrier.
- Test-Chunk7HostCompletion.ps1: bounded failure gate and actual terminal host method.
- Test-Chunk7PersistenceCleanupAdapter.ps1: preparation and transition before fact removal, actual write-once pending/final artifacts, one merged FAIL row after later cleanup success.
- Test-Chunk7CombatCleanupAdapter.ps1: native combat cleanup ownership and independent retries.
- Test-Chunk7RefusedPairProposals.ps1: the actual RT companion-window refused-proposal classifier, compiled against the game's Newtonsoft, on trace-shaped rows (null executor is a String-typed JValue).
- Test-Chunk7CleanupEvidence.ps1: exact optional pending roles, request/candidate/manifest identity, nonempty FAIL errors, rejection of promotion to PASS.

Closed protocol and source intake: Test-Chunk7PersistenceProtocol.ps1, Test-Chunk7PersistenceWiring.ps1, Test-Chunk7CanonicalEvidence.ps1, Test-Chunk7PersistenceSource.ps1, Test-Chunk7CombatPersistenceSource.ps1, Test-Chunk7AreaPersistenceSource.ps1. Source tests retain actual synthetic ZIP/member/hash/restoration receipts.

Strict diagnostic readers: Test-Chunk7PersistenceEvidence.ps1, Test-Chunk7CombatPersistenceEvidence.ps1, Test-Chunk7CombatPersistenceObservation.ps1, Test-Chunk7AreaPersistenceEvidence.ps1, Test-Chunk7CombatEvidence.ps1, Test-Chunk7RealtimeEvidence.ps1, Test-Chunk7NativeLifeEvidence.ps1. Several source and family checks intentionally import their affected lower-level reader fixtures; read printed counts per named family rather than adding nested totals as unique cases.

No new whole-game or native acceptance follows from these checks. The same-call R1 ordering regression remains separate from actual later native life delivery. Small body identities are outside these diagnostics' declared fixture evidence.

Exact native observation/source JSON types: Test-Chunk7PersistenceTypes.ps1 and Test-Chunk7SourceReceiptTypes.ps1 reject coerced integers and booleans across complete source/cold observations and restored private source receipts. Native object hashes, frame/round values and split sentinels retain their existing signed semantics.

Test-Chunk7ObserverAcquisition.ps1 executes extracted installation/disposal methods with a faulting external Harmony boundary. It verifies retained partial installation, rollback retries, incomplete captures and field-before-install bindings. Historical allocation trace callers retain the eager default; selected Chunk 7 callers opt in to deferred installation.
