# Phase 2A manual visual review

Status: BLOCKED - MANUAL VISUAL ACCEPTANCE REQUIRED

This is the sole authorized human checkpoint before combat implementation. The review build remains version `0.1.0-phase2a-review.2`. Its two complete-suite processes were qualified from implementation commit `9686105e6ebc7531764b2f614e9d323b6d427410`; the replacement review package is rebound to the final clean record/continuity HEAD. The final stop report supplies the only runnable exact command and pins its package, sidecar manifest, DLL, branch, commit, schema-v2 authority, and protected-save pin set.

Protected continuity is now authority epoch `20260816T0115379973604Z-user-attested-quicksave-continuity`, SHA-256 `111c794baf8bd1062ef7ecf8c307f4e3badb7a519cf3e66ae1f8f2442b770701`, protected-pin-set SHA-256 `6f137d57e3a519b3a315a4c654b5feee51c8495f7104c26bfd4da097930c9ff4`, and inventory digest `03a91757453a8e875e376f31f1664decb0a6e60dcf6433f2348795a90be634c9`. It chains to the immutable schema-v1 epoch and authorizes only the user-attested `Quick_3.zks` and `Quick_438.zks` continuity transitions. It grants no save-write authority. The prior schema-v1-only launcher command and Auto/Quick arguments are superseded and must not be reused.

From repository root, use the final stop report to replace every `FINAL_*` token below. Do not run the template itself. Answer the high-impact confirmation prompt only if the displayed target is Steam App 640820, the exact live Kingmaker Mods directory, and guarded Working-only review:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\runtime\Invoke-KingmakerManualReview.ps1 -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2a-review.2-diagnostic.zip" -ExpectedPackageSha256 FINAL_PACKAGE_SHA256 -ExpectedPackageManifestSha256 FINAL_MANIFEST_SHA256 -ExpectedDllSha256 FINAL_DLL_SHA256 -ExpectedBranch codex/mounted-combat-phase2-alpha -ExpectedCommit FINAL_HEAD -ExpectedCurrentQualificationSha256 95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a -ExpectedSupersededWorkingSha256 a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5 -PriorSaveTransactionStatePath "C:\Dev\KingmakerMountedCombatLab\runtime-state\save-transactions\20260814T100000Z-boundary-area-pass.json" -ExpectedPriorSaveTransactionRunId 20260814T100000Z-boundary-area-pass -ExpectedPriorSaveTransactionStateSha256 b25f80c799207657650cf29118078edc4685bcbb51e1609ba8b167cab13052e0 -ExpectedPriorSaveMetadataDigest 58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be -ProtectedSaveContinuityAuthorityPath "C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260816T0115379973604Z-user-attested-quicksave-continuity.json" -ExpectedProtectedSaveContinuityEpochId 20260816T0115379973604Z-user-attested-quicksave-continuity -ExpectedProtectedSaveContinuityAuthoritySha256 111c794baf8bd1062ef7ecf8c307f4e3badb7a519cf3e66ae1f8f2442b770701 -ExpectedProtectedSavePinSetSha256 6f137d57e3a519b3a315a4c654b5feee51c8495f7104c26bfd4da097930c9ff4
```

Wait until the launcher prints `KMC MANUAL VISUAL REVIEW READY.` Do not save, load, enter combat, change area, change game mode, close the launcher, or respond to any Steam/account/update/cloud prompt. If an unexpected prompt appears, leave it untouched and close Kingmaker normally so the launcher can restore state.

Review this exact checklist:

- Mount and Dismount transition: no detached body, implausible snap, or lasting pose residue.
- Mounted idle: rider posture, seat/contact, hands, feet, knees, torso, and Mammoth silhouette are acceptable.
- Walk and run: rider remains attached and readable without objectionable sliding, pumping, clipping, or jitter.
- Turn, reversal, and stop: facing and body settle acceptably with no persistent twist or correction pop.
- Doorway and party formation: the mounted pair remains understandable despite fixture walls and edge framing; non-pair movement remains usable.
- One-handed equipment: the protected fixture's available weapon presentation is acceptable. Shield and safe two-handed variants are unavailable and are not claimed.
- Selection, portrait, action bar, and on-screen `Dismount` action: ownership is understandable and pointer interaction feels usable. Observe the native bright-blue selection silhouette deliberately.
- Camera follow and away/back selection: framing and follow feel are usable during movement and after returning to the rider.
- Final Dismount: the rider, Mammoth, selection, attachment, and pose return cleanly.

Known compromises to judge, not overlook: there is no saddle or reins; the profile is a deterministic analytical Medium-humanoid/Mammoth pose rather than a new authored animation set; doorway walls and screen-edge framing can occlude the pair; Kingmaker's bright-blue selected-unit silhouette can cover much of the Mammoth; automated gameplay-camera captures do not show all UI chrome; physical pointer feel and subjective camera feel are manual-only; and the fixture exposes only its original one-handed equipment set. This build contains no authorized combat implementation.

When the checklist is complete, exit Kingmaker normally and keep this launcher open. Acceptance is not ready to report until the launcher itself finishes with session `PASS`, exact Mods/Working/protected-save restoration, and no process/lock/sentinel residue. Then explicitly accept or reject version `0.1.0-phase2a-review.2` and the package SHA-256 reported in the final checkpoint response. Combat remains forbidden until that explicit decision.
