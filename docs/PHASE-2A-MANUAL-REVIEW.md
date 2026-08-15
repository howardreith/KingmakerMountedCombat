# Phase 2A manual visual review

Status: BLOCKED - MANUAL VISUAL ACCEPTANCE REQUIRED

This is the sole authorized human checkpoint before combat implementation. The review build is version `0.1.0-phase2a-review.2`; the qualified implementation commit is `9686105e6ebc7531764b2f614e9d323b6d427410`, and its two complete-suite processes used ZIP SHA-256 `266b007892ebe6e09ecd09612fd6fa5e8ce63e533da719cbeebdb2e1eec81604`, DLL SHA-256 `68e71ff952b8ea48aa4c77479d968680837c54b975f94ae977cc872be485b097`, and MVID `f4347f61-7a6b-43c0-8c4c-ccacb0002944`. The guarded launcher also requires the sidecar manifest to bind the current clean Phase 2 branch HEAD and exact ZIP at launch; use the final stop report's package SHA if a record-only descendant regenerated that sidecar.

From repository root, run this one PowerShell command and answer the high-impact confirmation prompt only if the displayed target is Steam App 640820, the exact live Kingmaker Mods directory, and guarded Working-only review:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\runtime\Invoke-KingmakerManualReview.ps1 -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2a-review.2-diagnostic.zip" -ExpectedCurrentQualificationSha256 95e9a3c6203f11cfdd7e615849eb61ebc060131075f6a7f40771dcd7eeb68a7a -ExpectedSupersededWorkingSha256 a5f7a7fb77f0465df1591360ecd1730e2c28215d83e27aafc70efef3110e6dc5 -PriorSaveTransactionStatePath "C:\Dev\KingmakerMountedCombatLab\runtime-state\save-transactions\20260814T100000Z-boundary-area-pass.json" -ExpectedPriorSaveTransactionRunId 20260814T100000Z-boundary-area-pass -ExpectedPriorSaveTransactionStateSha256 b25f80c799207657650cf29118078edc4685bcbb51e1609ba8b167cab13052e0 -ExpectedPriorSaveMetadataDigest 58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be -ProtectedSaveContinuityAuthorityPath "C:\Dev\KingmakerMountedCombatLab\runtime-state\protected-save-authorities\20260814T1445257441387Z-user-fixture-preparation.json" -ExpectedProtectedSaveContinuityEpochId 20260814T1445257441387Z-user-fixture-preparation -ExpectedProtectedSaveContinuityAuthoritySha256 77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9 -ExpectedProtectedAutoSaveName Auto_1120.zks -ExpectedProtectedAutoSaveSha256 9599e9e15bdd04cde7b15d47795510b95cd8e1333f107f2e3b64469ebf3330fc -ExpectedProtectedQuickSaveName Quick_438.zks -ExpectedProtectedQuickSaveSha256 e26dc2ce00e6ed68953c2c9b1e89584ae3c3bd50f4cf698ecc2c624e21d10575
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
