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

No version bump, merge, release, permanent installation, loader replacement or human-save operation occurred.
The independent Mounted Shield draft PR 61 remains separate; it should consume the agreed baseline when this
change is integrated rather than quietly restoring installed-loader build references.
