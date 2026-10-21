# Chunk 7 stale level-up autosave families

Status: the families and cases in the blocked table are **NOT READY**. The
assisted new-campaign fixture was removed from that table after its completion
path changed to the initial native Auto followed by a final native Manual. Its
reader and source checks remain development work; no native success is implied.
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

Each still-blocked family below waits, after its native choices complete, for a terminal native Auto whose
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
| `chunk7-cumulative-acquisition` (legacy Auto source mode only) | `Chunk7ManualDerivedScenario.Build.cs` / `ObserveTerminalAutosave` | The legacy source mode still waits for a native level-up Auto. The assisted producer-and-cold mode now has a separate Manual completion path; its native qualification remains pending. |
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
- `chunk7-manual-derived-acquisition`. The non-cumulative cases now finish native choices and
  field admission, then request a distinct native `Manual_2_KMC_C7_MANUAL_DERIVED_FIELD.zks`.
  The producer observes request, prepared write, worker, callback, and settled field roster;
  its strict reader binds the newly acquired owner/Horse to that field state. This is an
  engineering route whose owner-seed source, same-candidate native producer and separate
  cold consumer passed in [batches 131–133](CHUNK7-BATCH131-133-F10-ACQUIRED-COLD-2026-10-21.md).
  This does not pass the historical Auto producer. Assisted cumulative dispatch now takes the
  Manual path; the legacy source mode retains the blocked Auto route. Neither has a native cumulative PASS yet.
- `chunk7-assisted-cold-inventory`.
- `chunk7-assisted-fixture-preparation`. The selected producer now follows the
  bounded native Ranger choices with its guarded final Manual, without waiting
  for a level-up Auto. It still needs its own actual new-campaign native result
  and independent cold consumer; fixture assistance does not certify ordinary
  acquisition or progression.
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

`chunk7-acquired-manual-derivative` does not wait for an autosave. Its one-owner
cases now consume the new Manual-derived field archive, require the acquired pair
already in the saved field roster, and produce a later genuine Manual. The
two-owner cumulative route retains its historical Auto intake. Neither route
transfers a PASS from the other or from the pre-repair fixture.

## Repair each family needs before it is selected

Separate actual native choice completion from persistence. When a persistent output is needed,
follow completion with an explicitly authorized native Manual save. Update the request and
authority, the writer quota, the descriptor and reader, and downstream intake together. Never
manufacture a level-up callback or relabel a Manual as Auto.

For the manual-derived family, the selected narrow route is one read-only qualified
Manual seed load, the already bounded ordinary native choices and paid field
admission where selected, then one separately reserved native Manual after the
actor/Horse and field state settle. Capture the actual native save number,
request/prepared/written object chain, worker and callback through the existing
Manual-probe pattern. The resulting archive must be qualified as a new field
source with honest seed and assistance provenance, not impersonate the old
remote-companion Auto. The prelaunch refusal was removed only for this non-cumulative
route after selection, authority, runtime, reader, downstream intake and focused
negative checks were updated. Native producer and cold-consumer results remain due.

When a family is repaired, remove its row from `Get-KmcChunk7StaleLevelUpAutosaveFamilies`,
from the test, and from this note in the same change.

## Families that remain to be corrected

1. `chunk7-ordinary-acquisition`
2. `chunk7-cumulative-acquisition` legacy Auto source mode
3. `chunk7-native-progression`: the historical level case and two historical respec cases

The selected owner-seed Manual-derived producer and one-owner acquired cold
consumer are repaired and passed actual native qualification in batches 131–133.
Other Manual-derived source modes and the blocked historical families above
retain their own readiness and evidence requirements.

This is not a new universal prerequisite. Optional prologue production and the historical
automation families are not repaired by this change.
