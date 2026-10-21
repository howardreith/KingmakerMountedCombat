# Chunk 7 stale level-up autosave families

Status: the families and cases in the blocked table are **NOT READY**. None of them is selected for the current batch.
`scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1` refuses each one before any selection
intake, lock, transaction, Mods swap or game launch, so a run that is known to be futile
cannot start. Their code paths are kept as they are, with a NOT READY note at each wait site.

## Native fact

The installed game (Kingmaker 2.1.7b) never registers `LevelUpAutoSaveController`.
`GameModesFactory` builds its controllers explicitly, and that list does not include it. So
an ordinary level-up never requests an Auto save. Nothing handles
`ILevelUpCompleteUIHandler.HandleLevelUpComplete` with a save. This was observed natively in
`20261017-c7b-world19-ownerseed-b2s1-twopair` (zero completion entries, save requests and
prepared writes). The owner-seed exploration setup already expects zero writes for this reason
(`Chunk7OwnedPairsSelection.ExpectedSetupWrites`).

Each family below waits, after its native choices complete, for a terminal native Auto whose
requested descriptor `IsAutoLevelupSave`. That wait can never complete. The run would stall
until its stage deadline and then fail.

October 20 source audit after batch102: the shared
`NativeOrdinaryAcquisitionProbe` installs its completion hook on that
unregistered controller and defines `Complete` as one observed Auto request
plus one prepared Auto write. Installation can succeed while the native method
never runs. `Chunk7OrdinaryBuildDriver.ObserveCommit` independently checks the
actual actor's one-level native advancement, unchanged XP, native UI closure,
sourced reciprocal Horse, and paid-hire money where applicable. That existing
proof is a sound input to a new completion contract, but it is not itself a
save or cold-load result. Changing only `TickBuild`'s wait would still leave the
selection's `Auto_1.zks`, persistence authority, `nativeWrite` observation,
external reader and downstream acquired/cumulative source intake bound to an
impossible Auto. No family was unblocked or run by this audit.

[Batch103](CHUNK7-BATCH103-F10-PAID-HIRE-READER-2026-10-20.md) separately
re-evaluates the retained F09 paid-hire choice sequence. It proves a limited
native paid custom-companion acquisition operation in that fixture, without
changing its fixture-only provenance or repairing any stale Auto-wait family.
[Batch104](CHUNK7-BATCH104-F10-EXISTING-ACQUISITION-READER-2026-10-20.md)
likewise proves a source-specific existing owner-seed Ranger acquisition
operation from its retained native choices, without unblocking those families.

## Blocked families

| Family (scenario / case) | Wait site | Route verified by code path |
| --- | --- | --- |
| `chunk7-ordinary-acquisition` (every case) | `src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionScenario.cs:112` (stage 1, `ObserveCompletedAutosave` at `:266`) | `RuntimeAutomationHost` builds `Chunk7OrdinaryAcquisitionScenario`. Stage 0 completes the build and stage 1 waits. |
| `chunk7-manual-derived-acquisition` (every case) | `src/KingmakerMountedCombat/Diagnostics/Chunk7ManualDerivedScenario.Build.cs:196` (build stage 1, `ObserveTerminalAutosave` at `:222`) | The native load always ends in `StartBuild`, and `TickBuild` stage 1 waits. |
| `chunk7-cumulative-acquisition` (every case, including the assisted cumulative plan) | the same `Chunk7ManualDerivedScenario.Build.cs:196` / `:222` | `Chunk7ManualDerivedScenario.CreateCumulative` uses the same `StartBuild` / `TickBuild` path. |
| `chunk7-assisted-fixture-preparation` | `src/KingmakerMountedCombat/Diagnostics/Chunk7AssistedFixtureScenario.Setup.cs:161` (`ObserveLevelupAutosave` at `Chunk7AssistedFixtureScenario.Save.cs:55`) | Stage 2 `TickBuild` waits for the level-up Auto before stage 3 and the final Manual. |
| `chunk7-native-progression` / `horse-native-level`, `horse-native-retrain-horse`, `horse-native-retrain-mammoth` | `src/KingmakerMountedCombat/Diagnostics/Chunk7NativeProgressionSession.cs (legacy ObserveTerminalAutosave)` | The historical acquired-source cases retain the native level-up Auto wait and are refused before launch. |

The registry is `Get-KmcChunk7StaleLevelUpAutosaveFamilies` in
`scripts/runtime/RuntimeHarness.Common.ps1`. The runner calls `Assert-KmcChunk7FamilyReady`
twice: once with the scenario, right after its modules load, and once with the selected
progression case, right after the closed progression selection is read. Both calls come before
approval, the lock and every transaction. The refusal reads:

> Scenario family <name> is not ready: it waits for a native level-up autosave that the
> installed game never requests (LevelUpAutoSaveController is not registered); repair its
> completion contract before selection.

`scripts/chunk7/Test-Chunk7StaleLevelUpFamilies.ps1` runs the actual runner for every blocked
family and case and expects this exact refusal. It also expects the selected families to
pass this gate.

## Still admitted

These families do not route through a level-up autosave wait:

- `chunk7-owned-two-pair-campaign`. This includes owner-seed exploration, whose setup already
  expects zero level-up writes.
- `chunk7-assisted-cold-inventory`.
- The owned-pair persistence, combat, realtime and native-life families.
- `chunk7-native-progression` with `horse-native-dismiss` or `owned-horse-native-dismiss`.
  The dismissal session writes nothing.
- `chunk7-native-progression` with `owned-horse-native-level`,
  `owned-horse-native-retrain-horse` or `owned-horse-native-retrain-mammoth`.
  These cases use a distinct, authorized genuine Manual after native choice
  completion. Admission no longer waits for a nonexistent level-up Auto.
  They still need a fresh qualified acquired two-pair `KMC_C7_WORLD` exploration
  Manual and actual native leveling or retraining eligibility before a native
  result can be claimed. The owner-seed `KMC_C7_SEED_WORLD` Manual and the
  zero-pair `KMC_C7_SEED_CHECKPOINT` cannot substitute for that ancestry.

A related family has an input dependency rather than a wait.
`chunk7-acquired-manual-derivative` does not wait for an autosave. However, its source is a
level-up Auto descriptor (`Chunk7AcquiredManualScenario.Save.cs`, `IsAutoLevelupSave`), and only
the blocked manual-derived or cumulative families produce that descriptor. It stays admitted
for already retained sources. It cannot get a fresh source until one of those producers is
repaired.

## Repair each family needs before it is selected

Separate actual native choice completion from persistence. When a persistent output is needed,
follow completion with an explicitly authorized native Manual save. Update the request and
authority, the writer quota, the descriptor and reader, and downstream intake together. Never
manufacture a level-up callback or relabel a Manual as Auto.

For the manual-derived family, the narrow route is one read-only qualified
Manual seed load, the already bounded ordinary native choices and paid field
admission where selected, then one separately reserved native Manual after the
actor/Horse and field state settle. Capture the actual native save number,
request/prepared/written object chain, worker and callback through the existing
Manual-probe pattern. The resulting archive must be qualified as a new field
source with honest seed and assistance provenance, not impersonate the old
remote-companion Auto. Keep the prelaunch stale-family refusal until selection,
authority, runtime, reader, downstream intake and focused negative checks agree.

When a family is repaired, remove its row from `Get-KmcChunk7StaleLevelUpAutosaveFamilies`,
from the test, and from this note in the same change.

## Families that remain to be corrected

1. `chunk7-ordinary-acquisition`
2. `chunk7-manual-derived-acquisition`
3. `chunk7-cumulative-acquisition`
4. `chunk7-assisted-fixture-preparation`
5. `chunk7-native-progression`: the historical level case and two historical respec cases
6. Downstream input only: `chunk7-acquired-manual-derivative`, for fresh sources

This is not a new universal prerequisite. Optional prologue production and the historical
automation families are not repaired by this change.
