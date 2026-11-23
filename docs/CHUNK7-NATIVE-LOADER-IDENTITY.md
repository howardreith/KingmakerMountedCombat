# Native loader identity contracts

This reader correction is based on exact installed Kingmaker UMM/Harmony metadata and the unchanged native loader inventory from run 20261012-chunk7b-world03. That native scenario remains FAIL before gameplay. A loader-reader PASS does not change the scenario result or establish gameplay, compatibility or a complete stock assembly allowlist.

Harmony 2.3.1.1, 0Harmony.dll, has SHA256 5f7ad0df1eb08e81f66d4006d3a9e5cb40fc05e381f36d7bd0513f334f6a251f and MVID 6c906b4f-1de4-40f3-a86c-b0ebb3f48ea6. HarmonyLib.HarmonySharedState.GetOrCreateSharedStateType (06000096) creates a Cecil module named HarmonySharedState. ModuleDefinition.CreateModule (06000E77) assigns Guid.NewGuid; ReflectionHelper.Load (06002D1C/06002D1D) serializes and calls Assembly.Load(byte[]). The resulting native observation is IsDynamic=false, empty Location, a nonempty generated MVID and no file SHA256.

The reader accepts only one HarmonySharedState entry with its exact full assembly identity and record shape, canonical unique MVID, empty-string location and null hash. The creator must also appear once at the exact installed UMM path, with the pinned full identity, MVID and SHA256, matching the separately guarded dependency receipt. Other non-dynamic fileless assemblies still fail. This is an identity-shape check under a pinned creator; creationObserved and contentCertified stay false because the existing native artifact contains neither a creation callback nor content bytes.

UMM 0.28.2 has SHA256 75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39 and MVID edf4be29-0c88-4482-876d-e946a0f9e363. ModEntry.Load (0600009A) builds a cache path from the mod assembly path plus a canonical UInt16 decimal suffix and .cache. The native KMC entry was loaded from KingmakerMountedCombat.dll.51087.cache with the exact selected candidate SHA256 and MVID.

The reader accepts that cache only in the exact staged KMC directory, with a canonical decimal suffix from 0 through 65535 and the exact candidate hash/MVID. It also requires the observed UMM full identity, MVID, SHA256 and exact path to match the pinned dependency receipt. Arbitrary cache suffixes, locations and changed candidate bytes remain refused.

Focused validation preserves the original native reader FAIL and a controlled derivative that exposes the separate cache rejection. The final reader passes 93 positive/negative cases, including the unchanged 32 historical inventory cases, creator/dependency scalar-type negatives, foreign modules, missing/duplicate roles and malformed memory/cache identities. Re-evaluation uses the unchanged native artifact SHA256 3e4cb83625f43f4fcb866b4543dbf320ba06293cdcee8db0184d579043adf3d6; no native artifact is rewritten.

This is a PowerShell reader/test change. Product DLL, candidate, native producers, transaction guards and restoration receipts are unchanged. Existing dynamic-assembly and stock-inventory limitations remain explicit.

## 2026-10-17: owner reinstall of UMM 0.33.2.0

The sections above describe the loader as it was installed for run 20261012-chunk7b-world03 and stay as written. On 2026-10-16 (files written 23:42:58 UTC) the owner reinstalled Unity Mod Manager. Hashed offline from the installed files:

- UnityModManager.dll: UnityModManager, Version=0.33.2.0, MVID 914c93ba-b0bd-409f-9d38-84e575289dc2, 204800 bytes, SHA256 f6f7e18aff88910b01d23e3ce0829f3b4188c6784b0fcea56f9b3af8aa14d0a9.
- 0Harmony.dll: 0Harmony, Version=2.3.6.0, MVID 4dbb7cd6-acb5-43ef-8d7e-6fc104af5bdc, SHA256 df043f05c6e43602fa8e8f38f52a3a2cf962aab7a65a9d85573ea25f01a63e80.
- 0Harmony12.dll: byte-identical to the existing pin (SHA256 aa1cd483..., MVID 918c071f-...).

The reader now pins these exact identities. Nothing was turned into a pattern or a range. The mechanisms the reader depends on were checked again in the new binaries. Harmony 2.3.6.0 still creates HarmonySharedState in memory: GetOrCreateSharedStateType (06000095) calls ModuleDefinition.CreateModule (06000E95), which assigns Guid.NewGuid, and ReflectionHelper.Load (06002DA1/06002DA2) calls Assembly.Load(byte[]). UMM 0.33.2.0 ModEntry.Load (06000111) still names its cache from the mod path plus a ushort decimal suffix and `.cache`. Its suffix hash now includes the manager version, so cache file names differ from the 0.28.2 ones; the accepted shape (0 through 65535, exact candidate directory, hash and MVID) is unchanged.

KMC binds only UMM members whose signatures are unchanged: ModEntry Info/Logger/OnGUI/OnSessionStop/OnUpdate/OnUnload/OnToggle, ModInfo.Id, the ModLogger Log/Warning/Error/LogException(string,Exception) methods, and the UI capture host (get_Instance 06000151, get_Opened 06000152, ToggleWindow(bool) 0600016A). Test-AssemblyContracts checks the ModEntry, ModInfo and ModLogger surface by name and exact signature, so a later reinstall that keeps the API but moves tokens still passes, and a genuine API break still fails.

Evidence produced on UMM 0.28.2.0, including the 93-case reader receipt and every native run above, stays bound to its original environment. It is not transferred to 0.33.2.0. No native run has yet been made on 0.33.2.0.

## 2026-11-15: reviewed loader profiles

The sections above stay as written. The loader identities they record are now also kept as reviewed profiles in
`planning/UMM-LOADER-PROFILES.json`, each with its provenance: 0.28.2 and 0.33.2 from the records above, and 0.33.0,
hashed on 2026-11-15 from the loader installed on the development machine (UnityModManager, Version=0.33.0.0, MVID
54059519-2754-445a-b5ee-fdb9326336e2, 204800 bytes, SHA256
63e5baf7b1738e4091b5fd17ccb738ecdb4d1dbf246061dfd55dc52835d52691).

`scripts/Test-AssemblyContracts.ps1` no longer pins one loader in line. It checks the installed loader's identity
against one selected profile and its API against the binary itself, and reports the two separately
(`scripts/UmmLoaderContracts.ps1`). The native loader reader and the runner's dependency pin described above are
unchanged and still name 0.33.2.0. No native run has been made on 0.33.0.0 by this work.

## 2026-11-23: owner-selected 0.31.1 build baseline and exact runtime target

The owner asked to align Mounted Combat with the older compatibility baseline previously selected for the other
Kingmaker mods. The selected floor is **0.31.1**. It is not 0.32.0, and it is not a requirement to downgrade a working
newer installation.

### Decision and source evidence

- [Cutscene Skip compatibility](https://github.com/howardreith/KingmakerSkipCinematic/blob/6c1d65aba5bb4624a7e23819210400d0c281054e/docs/COMPATIBILITY.md)
  records 0.31.1 as its verified minimum and pinned build reference, with additional native tests of 0.32.4 and 0.33.0.
- [Tabletop Rules build pins](https://github.com/howardreith/KingmakerGunslingerAndOtherTabletopRules/blob/06abcc775e478407d5d209cb69c7db663423f07d/scripts/loader-reference-pins.json)
  independently select the same 0.31.1 DLL and package hashes. Its 0.0.188 release adopted that baseline; the current
  records still retain it.
- [Buff Planner's native result reader](https://github.com/howardreith/KingmakerBuffPlanner/blob/913798712f3685236c8f9d5cc1e8c04ea9fc9248/scripts/RuntimeAutomation.Common.ps1#L468-L475)
  separately pins the owner's installed UMM 0.33.0.0 and SHA-256 `63e5baf7...`.
- The sibling compatibility investigation reproduced a Craft Magic Items failure with **Harmony 2.3.5** in a synthetic
  early-0.32-era combination. Harmony 2.3.6 restored the affected API; the tested 0.32.4 and 0.33.0 combinations worked.
  It did not establish that every 0.32+ or 0.33+ loader is broken, nor that 0.31.1 is a Linux-specific requirement.
  See also [upstream UMM issue 156](https://github.com/newman55/unity-mod-manager/issues/156).
- KMC's November 15 T03 record already reported genuine 0.31.1 passing offline API inspection, with identity explicitly
  unasserted and native execution not run. This update adds genuine binary identity evidence; it does not transfer
  another mod's native passes to Mounted Combat.

### Exact reference and runtime identities

The build reference comes from the author's
[UnityModManager 0.31.1 NuGet package](https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.31.1/unitymodmanager.0.31.1.nupkg).
The package was independently downloaded and its bytes checked against the two sibling pin records.

| Field | Pinned build reference | Current Windows native target |
|---|---|---|
| Assembly version | 0.31.1.0 | 0.33.0.0 |
| DLL SHA-256 | `2e038ff23ab01d538dfef792096e1f5c04802d1edac3660db20d0d54c167c2c4` | `63e5baf7b1738e4091b5fd17ccb738ecdb4d1dbf246061dfd55dc52835d52691` |
| MVID | `230caa64-1cae-4f42-a86c-8f16b5faba9e` | `54059519-2754-445a-b5ee-fdb9326336e2` |
| DLL bytes | 204288 | 204800 |
| UI tokens: Instance / Opened / ToggleWindow(bool) | 06000151 / 06000152 / 0600016A | 06000151 / 06000152 / 0600016A |

The 0.31.1 package is 94256 bytes, SHA-256
`05a82b66bc44d56d1246f146d16e9fa8f9a42c450c5bc334bcfa9efddbdb53c9`; the selected entry is
`lib/net35/UnityModManager.dll`. Its CLI metadata was read without executing the assembly and cross-checked with a
second parser. [The metadata record](umm-baseline/loader-metadata.json) states that scope explicitly.

The 0.33.0 native target uses the existing November 15 genuine installed-file record, corroborated by the sibling
runtime pins and later startup records. No live installation was re-measured from this environment. Harmony12 remains
the same exact 1.2.0.1 binary, and the current runtime's Harmony 2.3.6 shared-state creator pin is unchanged.

### What this patch changes

The build restores only the exact 0.31.1 reference into an ignored build directory. Existing mismatched reference
bytes are refused; they are not silently replaced by a different loader. MSBuild validates the selected reference
before compilation, including direct builds used by Test-Tiers. The loader is neither installed nor packaged.

The current environment fingerprint, runtime result version assertion, cache-creator identity and their synthetic
fixtures now agree on the exact installed 0.33.0 runtime. A 0.31.1 build reference is not accepted as evidence that the
native process used 0.31.1. The current native reader continues to reject a substituted 0.31.1/0.33.2 runtime or mixed
version/hash/MVID observations. A future native 0.31.1 qualification must select a matching guarded environment and
its own exact Harmony 2.3.1.1 creator identity; it cannot reuse this 0.33.0 lane's result.

The build pin and restore/build helpers are product inputs for frozen-source comparison. Changing them invalidates
reuse of an older build through that route. Existing package receipt fields and historical native evidence are kept.

### Validation and remaining work

This is a development checkpoint. The exact 0.31.1 package/DLL identity and metadata were verified here. Additional
executed checks and their limits are recorded in [the validation summary](umm-baseline/VALIDATION.md). Windows/.NET Framework
compilation, the existing Windows harness suites and native game execution have not been run in this environment.

Before native use, verify the newly built product's bindings using the existing loader contract against both the
pinned 0.31.1 reference and the selected installed 0.33.0 reference, and run the affected Windows reader contracts.
The unchanged strict native-cache allowance still requires exact candidate bytes, MVID, directory, creator identity
and a canonical UInt16 suffix. Existing sibling 0.33.0 cache observations support that lane, but this patch does not
claim a newly executed KMC cache/restoration qualification.

The Mounted Shield run remains BLOCKED until its required native fixture, gameplay driver, current independent
review and native acceptance are completed. No acceptance row or prior reader verdict is promoted by this pin change.
The companion change is imported through the existing enrollment's guarded development/push flow; the controller-owned
`codex/mounted-shield` head and its review counters are not edited out of band.

