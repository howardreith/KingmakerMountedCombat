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
