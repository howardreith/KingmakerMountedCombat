# UMM 0.31.1 build and support baseline

Source checkpoint, 2026-11-23. Owner direction: support UMM 0.31.x consistently across the Kingmaker mods and
check newer versions separately. This checkpoint changes no gameplay behavior, installed runtime, save or release.

## Before and after

| Surface | Before | This source checkpoint |
| --- | --- | --- |
| Main mod `Info.json` | Minimum 0.28.2 | Minimum 0.31.1 |
| Removal observer `Info.json` | Minimum 0.28.2 | Minimum 0.31.1 |
| Both production projects | Whatever UMM/Harmony12 was installed in `UnityModManagerDir` | Exact UMM 0.31.1 and Harmony12 1.2.0.1 references |
| Loader contract profiles | Historical 0.28.2, newer 0.33.0/0.33.2 | Adds genuine 0.31.1 identity as primary target; retains earlier identities |
| Historical native fingerprint | Exact 0.33.2 evidence environment | Unchanged |

0.31.1 is the existing build and native comparison baseline in Tabletop Rules Expansion and Cutscene Skip:
[Tabletop Rules PR 153](https://github.com/howardreith/KingmakerGunslingerAndOtherTabletopRules/pull/153) and
[Cutscene Skip PR 2](https://github.com/howardreith/KingmakerSkipCinematic/pull/2). Their results do not establish
Mounted Combat compatibility. No support is asserted for 0.31.0 or versions below 0.31.1.

## Reproducible build references

`scripts/loader-reference-pins.json` records the exact sources, archive entry, archive hash, DLL hashes and
assembly versions. `scripts/Get-PinnedLoaderReferences.ps1` downloads missing references into ignored
`obj/loader-references`, verifies the archive before extracting one exact entry, then verifies the DLL hash and
assembly identity before accepting it. Existing mismatched files fail without replacement. It never falls back
to the installed loader and never installs or packages these references.

`scripts/Build-Local.ps1` acquires the references. `LoaderReferences.targets`, imported by both production
projects, verifies the references and both manifests before direct MSBuild resolves assemblies. `Private=False`
remains in place. The minimum must equal the pinned UMM assembly version (with its fourth version part `.0`).
Both standalone package validators also inspect the packaged DLL's emitted UMM and Harmony12 assembly reference
versions. A previously released DLL compiled against UMM 0.33.0 cannot pass merely by lowering its manifest.

The genuine UMM package SHA-256 is `05a82b66bc44d56d1246f146d16e9fa8f9a42c450c5bc334bcfa9efddbdb53c9`;
its UMM DLL SHA-256 is `2e038ff23ab01d538dfef792096e1f5c04802d1edac3660db20d0d54c167c2c4`.
Its module MVID is `230caa64-1cae-4f42-a86c-8f16b5faba9e`, length 204288 bytes, and the relevant UI tokens are
`06000151`, `06000152`, `0600016A`. The profile records that this is PE/CLI metadata inspection, not native testing.

## Compatibility matrix and remaining checks

*Dated 2026-11-23 (source checkpoint, history). Current status, results and native prerequisites: `CURRENT.md`.*

| Runtime target | Evidence available for this checkpoint | Remaining |
| --- | --- | --- |
| 0.31.1, primary | Genuine package and DLL hashes verified; exact loader metadata available | Compile both products; bind every referenced UMM member; focused native startup/settings/toggle/mount/save-reload |
| 0.32.4 | Target identified from sibling projects | Admit genuine runtime identity; API binding; focused native comparison |
| 0.33.0 | Existing exact loader profile and historical offline API evidence | Re-run bound-member and native comparison on the baseline-built candidate |
| 0.33.2 | Existing exact loader profile, historical native environment | Re-run bound-member and native comparison on the baseline-built candidate |
| 0.33.4 | Newer target, not an admitted exact profile here | Acquire/inspect actual loader and Harmony identities, bind candidate, then native comparison |

The loader matrix checker automatically includes the pinned baseline build reference alongside the installed
loader and explicit `-AdditionalReference` paths. It reports unavailable inputs as unassessed. An identity match
or API pass is not a native pass; the absence of exceptions at startup does not prove mounted combat or saving.

Focused validation on 2026-11-23: the exact UMM archive/DLL and Harmony12 DLL hashes match the pins. Four JSON
files, four XML files and all seven changed PowerShell scripts parse. Seven reference-admission behavior checks
passed in 3.954 seconds using PowerShell 7 and .NET SDK 8 MSBuild: genuine references accepted; direct MSBuild
reaches reference resolution after validation; a corrupted cache is rejected without replacement; direct MSBuild
stops before resolution on that corruption; a manifest minimum mismatch is rejected; an assembly-version mismatch
is rejected even with the correct DLL hash; and verification refuses missing inputs without downloading them.
These checks exercise build-input admission, not a Kingmaker product build or Windows PowerShell 5 runtime.
The package reference guard also passed two detached checks using genuine released DLLs: it accepts the
0.31.1-built Tabletop Rules Expansion reference set and rejects the released Mounted Combat 0.2.15 reference set
(which emits UMM 0.33.0.0). That exercises the shared emitted-version check; the full Windows package validators
were not executed here.

## Reconciliation with PR 63 and Windows validation (2026-11-24)

PR 63 (`5be0f737`, merged into `codex/mounted-shield`) solved the same baseline independently. Its fixes are carried
into this branch, adapted to this branch's pins (UnityModManager and Harmony12 both pinned in
`scripts/loader-reference-pins.json`; PR 63 pinned only UnityModManager and kept the installed Harmony12):

- **Resolved compiler inputs.** `LoaderReferences.targets` now also checks, after reference resolution and immediately
  before compilation, that the UnityModManager and 0Harmony12 inputs Csc receives are exactly the verified files in
  `obj/loader-references` (PR 63 checked UnityModManager only).
- **Frozen product source.** The loader pins, `Get-PinnedLoaderReferences.ps1`, `LoaderReferences.targets` and
  `Build-Local.ps1` are production inputs for `FrozenProductSource.ps1`; a change invalidates reuse of an older frozen
  product (`Test-FrozenProductSource.ps1`).
- **Native Windows target.** The environment fingerprint, the runtime result's UMM version assertion and the native
  cache-creator identity now agree on the installed UMM 0.33.0.0 (`63e5baf7…`, MVID `54059519-…`), replacing the stale
  0.33.2.0 expectations that the section above listed as blockers. Historical 0.33.2 records keep their identity.
- Not carried over: PR 63's separate UMM-only build reference (`planning/UMM-BUILD-REFERENCE.json`,
  `Restore-UmmBuildReference.ps1`, `Test-UmmBuildReference.ps1`) and its own validation records, which duplicate this
  branch's pins.

The pin verifier now matches every pin string through its absolute end (a trailing line feed is refused) and requires
the archive entry and archive hash together.

Windows results on this branch (Kingmaker 2.1.7b managed assemblies, Visual Studio Build Tools):

- `Build-Local.ps1`: both production projects compile against the pins; the product DLL
  (`d20474bf5e4593c84dbb662f5a552798ab07ffe13853085a8443bd157dfe756d`, MVID `9db1be50-bceb-4bb3-a167-14e4ce288e8c`) and
  the removal observer reference exactly UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1; component tests 153/153.
- `Package.ps1 -ArtifactQualifier umm-0311-272238ce`: package validation 12/12 (packaged DLL references checked).
- `Test-PinnedLoaderMsBuild.ps1` 6/6 on a temporary clone: the commit before this baseline compiles against the installed
  UnityModManager 0.33.0.0; without the pinned cache a direct MSBuild is refused; after acquisition it compiles against
  0.31.1.0/1.2.0.1; an altered cached file and a stray HintPath to a copy outside the verified folder are refused before
  compilation; the installed loader directory is not a compile input.
- `Test-FrozenProductSource.ps1` 25/25, `Test-Chunk7NativeLoaderContracts.ps1` 101/101,
  `Test-UmmLoaderContractChecks.ps1` 22 passed (4 unassessed inputs), `Test-Tiers.ps1 -Tier Fast` passed.
- Static loader API audit of the candidate DLL: every UMM and Harmony12 member it binds resolves on 0.31.1, 0.32.4, the
  owner's 0.33.0 and 0.33.4 (14 types, 42 members). Static evidence only.

Not run: native game execution on any loader. The native harness targets the installed 0.33.0 identity; a native 0.31.1,
0.32.4 or 0.33.4 run needs its own guarded environment and exact identities.

## Review round 6 (2026-11-25, scope freeze)

- **Observer inputs.** The removal observer inherits `tools/Directory.Build.props` and `tools/Directory.Build.targets`.
  Both are local reuse-receipt inputs whether or not Git tracks them, and part of the archived observer comparison
  (`Assert-KmcFrozenProductManifest`); main-only reuse does not depend on them.
- **Full duration after a refusal.** `Update-KmcWorkflowEntry -Failed` (used by `Test-Tiers.ps1` for a failed step) keeps
  a longer recorded step duration, so a Full run refused by the receipt before its runner is charged to its entry but
  does not replace the established Full-suite duration; a later admission with less remaining time is still refused.
  The receipt is still checked once.

## Review round 5 (2026-11-25, scope freeze)

- **Owner-profile dependency admission.** `Get-KmcBaseGameDependencies` takes the resolved runtime loader profile and
  refuses, before anything is staged, when the installed UnityModManager or Harmony 2 (`0Harmony.dll`) it measures differs
  from that profile, so the admission receipt's profile identities are the verified installed ones. The launcher stays
  owner-only; alternate-loader launch support stays removed.
- **Reuse receipt.** The repository-root `Directory.Build.props` and `Directory.Build.targets` are receipt inputs whether
  or not Git tracks them (absent is recorded as absent).
- **One check for Full reuse.** A Full `-SkipBuild` run no longer checks the receipt in a separate parent step; `Test.ps1
  -SkipBuild` checks it once inside the admitted umbrella step, before any runner. A direct `Test.ps1 -SkipBuild` keeps
  the same check; Fast and Candidate keep theirs; the plan still lists the check and its estimate.

## Review round 4 (2026-11-24, scope freeze)

- **Launcher profile selection removed.** Under the owner's scope freeze, native execution is not part of this change, so
  the launcher's `-RuntimeLoaderProfileId` selection (added in round 2) is removed rather than completed: the launcher's
  parameter positions are again those before this mission (`Scenario` first) and it admits only the measured owner
  profile. The readers keep their exact owner identities; admitting another runtime profile at the launcher is follow-up.
- **Reuse.** The local reuse receipt also covers the build bootstrap, `LocalGamePaths.props`, `Info.json` and
  `version.json`; `Test.ps1 -SkipBuild` enforces it; tier executions check it only inside their admitted step and `-Plan`
  reports one verdict. Archived (frozen-source) reuse also compares `LoaderReferences.Final.targets` and
  `scripts/LoaderPins.ps1`.

## Review round 2 (2026-11-24)

- **Compiler list.** The resolved-input check now runs immediately before `CoreCompile` (after every reference-selection
  step, including a later rewrite) on `@(ReferencePathWithRefAssemblies)`, the exact list Csc receives, and also checks the
  two loader files' SHA-256 against the pins. A target that rewrites that list after reference selection is refused
  before any DLL is emitted, for both the mod and the removal observer (`Test-PinnedLoaderMsBuild`).
- **Runtime loader profile** (launcher selection removed in round 4). `scripts/runtime/RuntimeLoaderProfile.ps1` selects a profile from
  `planning/UMM-LOADER-PROFILES.json` (`runtime` records: exact UnityModManager and Harmony 2 shared-state creator).
  The default is the measured owner installation (0.33.0.0 with Harmony 2.3.6); the 0.31.1 baseline record carries
  Harmony 2.3.1.1. `Test-RuntimeGameResult.ps1 -RuntimeLoaderProfileId` and the native inventory
  (`Assert-KmcBaseGameNativeInventory -LoaderProfileId`) judge observations against the selected profile; a profile that
  is not the measured installed loader, a profile without a runtime record (0.28.2, 0.33.2) and mixed identities are
  refused. Historical records stay bound to their original profiles.
- **Removal observer.** Reuse of an archived observer requires its own `tools/KmcRemovalObserver` inputs to be unchanged
  (main-only reuse is unaffected); the observer is packaged, validated and audited like the mod (CURRENT.md).
- **Package boundary.** Runtime admission (`Assert-KmcPackageManifest`) measures the ZIP with the product's standalone
  validator and requires the DLL SHA-256, MVID and entries to equal the manifest, so an outer ZIP with a correct digest
  but another build's DLL is refused before staging. Both validators require a scalar-string minimum. The pin verifier
  requires exactly the two canonical records. `-SkipBuild` reuse is bound to a loader-input identity recorded by each
  build (pins, verifier, MSBuild guard, project files, cached loader bytes).
- Results are in CURRENT.md; `Test-PackageAdmissionAndReuse` exercises the standalone validators on real packages built in
  a disposable lab (including the released 0.2.15 DLL compiled against UMM 0.33.0.0), runtime admission and reuse.

## Review round 3 (2026-11-24)

- **Final compiler-input check.** `LoaderReferences.Final.targets`, imported by both projects after
  `Microsoft.CSharp.targets` (so after `Directory.Build.targets` and any other repository-level hook), holds the
  `BeforeTargets="CoreCompile"` check. A repository hook at the same boundary that rewrites the list is refused for both
  projects. The two loader files are re-hashed by `Get-PinnedLoaderReferences.ps1 -VerifyCompilerInputs` against the
  parsed pin file, so a valid reformatted or reordered pin file is accepted.
- **One pin parser.** `scripts/LoaderPins.ps1` (`Read-KmcLoaderPinDefinition`) validates the pin definition offline. It
  checks exactly the two canonical records, unique names, string fields through their end, paired archive fields, and that
  the minimum equals the pinned UnityModManager version. The acquisition helper, the compiler check and both standalone
  validators use it, so a missing, duplicated or mismapped pin is refused without a cache.
- **One package measurement.** The launcher measures each package once with its standalone validator and passes that
  result to the common and the normal-player observer admissions. Both compare package and DLL SHA-256, MVID, product
  version and entries.
- **Runtime profile at the launcher.** `Resolve-KmcRuntimeLoaderProfileForEnvironment` resolves the selected profile
  against the measured installed loader before admission. The profile is passed to the native inventory and the result
  reader, and recorded in the admission receipt. With a selected profile, UnityModManager and Harmony 2 are verified even
  when no native-cache or shared-state observation appears. Profiles state their native eligibility.
- **Reuse.** The build receipt binds compiled sources and output bytes (both DLLs and the component runner). Fast,
  Candidate and Full check it first, and plan mode lists the check.
- Records: `loader-profile-inventory.json` is restored, and the evidence index names the producer of every file.

No version bump, merge, release, permanent installation, loader replacement or human-save operation occurred.
The independent Mounted Shield draft PR 61 remains separate; it should consume the agreed baseline when this
change is integrated rather than quietly restoring installed-loader build references.
