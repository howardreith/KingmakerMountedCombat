# Native loader identity contracts

This reader correction is based on exact installed Kingmaker UMM/Harmony metadata and the unchanged native loader inventory from run 20261012-chunk7b-world03. That native scenario remains FAIL before gameplay. A loader-reader PASS does not change the scenario result or establish gameplay, compatibility or a complete stock assembly allowlist.

Harmony 2.3.1.1, 0Harmony.dll, has SHA256 5f7ad0df1eb08e81f66d4006d3a9e5cb40fc05e381f36d7bd0513f334f6a251f and MVID 6c906b4f-1de4-40f3-a86c-b0ebb3f48ea6. HarmonyLib.HarmonySharedState.GetOrCreateSharedStateType (06000096) creates a Cecil module named HarmonySharedState. ModuleDefinition.CreateModule (06000E77) assigns Guid.NewGuid; ReflectionHelper.Load (06002D1C/06002D1D) serializes and calls Assembly.Load(byte[]). The resulting native observation is IsDynamic=false, empty Location, a nonempty generated MVID and no file SHA256.

The reader accepts only one HarmonySharedState entry with its exact full assembly identity and record shape, canonical unique MVID, empty-string location and null hash. The creator must also appear once at the exact installed UMM path, with the pinned full identity, MVID and SHA256, matching the separately guarded dependency receipt. Other non-dynamic fileless assemblies still fail. This is an identity-shape check under a pinned creator; creationObserved and contentCertified stay false because the existing native artifact contains neither a creation callback nor content bytes.

UMM 0.28.2 has SHA256 75b96e25a3a9fbadb47dd14a4ab490cb8c98143a6242aff3bba6145cd3047f39 and MVID edf4be29-0c88-4482-876d-e946a0f9e363. ModEntry.Load (0600009A) builds a cache path from the mod assembly path plus a canonical UInt16 decimal suffix and .cache. The native KMC entry was loaded from KingmakerMountedCombat.dll.51087.cache with the exact selected candidate SHA256 and MVID.

The reader accepts that cache only in the exact staged KMC directory, with a canonical decimal suffix from 0 through 65535 and the exact candidate hash/MVID. It also requires the observed UMM full identity, MVID, SHA256 and exact path to match the pinned dependency receipt. Arbitrary cache suffixes, locations and changed candidate bytes remain refused.

Focused validation preserves the original native reader FAIL and a controlled derivative that exposes the separate cache rejection. The final reader passes 93 positive/negative cases, including the unchanged 32 historical inventory cases, creator/dependency scalar-type negatives, foreign modules, missing/duplicate roles and malformed memory/cache identities. Re-evaluation uses the unchanged native artifact SHA256 3e4cb83625f43f4fcb866b4543dbf320ba06293cdcee8db0184d579043adf3d6; no native artifact is rewritten.

This is a PowerShell reader/test change. Product DLL, candidate, native producers, transaction guards and restoration receipts are unchanged. Existing dynamic-assembly and stock-inventory limitations remain explicit.
