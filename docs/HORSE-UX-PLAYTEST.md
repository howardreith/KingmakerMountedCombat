# Horse native-controls private-alpha playtest

Status: `INSTALLED - MANUAL REVIEW REQUIRED`

The guarded helper installed and byte-verified `0.1.0-phase3c-dev.13` in the local Kingmaker UMM Mods directory.

## Exact artifact

- branch: `codex/mounted-combat-phase3c-native-controls`;
- production implementation: `e951fb5394ff4f8e791dd27f49b75d71d76a8b1f`;
- package/diagnostic checkpoint: `42debbb814823dbdcd3a39cdc4353a5c3ee3d12d`;
- package: `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3c-dev.13-native-controls-ux-final-diagnostic.zip`;
- package SHA-256: `5f23757e17c51a2fe67374da6d218cb9efc1c4fb49ae282060d02577bf7a9fa3`;
- manifest SHA-256: `82bd78046ec3d393fd45f7af250bae3c2b8194da6e379648b7b6a2c26e9c3a1b`;
- DLL SHA-256 / MVID: `6fce17eee8b8f5d8b6b987dd7408ab6ddb9a177ad0236429c41e40ffcc3c282e` / `9f3344f0-4314-4b2e-ac15-08729c3727d4`;
- installed inventory digest: `c6911a7e68016c3e2ceef2abb2fd160f9ddd550704f794035d0b11890b296754`.

Do not manually copy, replace, or delete files in the live Mods directory. Close Kingmaker and UMM before running deployment commands.

```powershell
# Install when KMC is absent
& 'C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1' -Operation Install -PackagePath 'C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3c-dev.13-native-controls-ux-final-diagnostic.zip' -AllowDocumentationDescendant -Confirm:$false

# Replace another exact KMC deployment
& 'C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1' -Operation Replace -PackagePath 'C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3c-dev.13-native-controls-ux-final-diagnostic.zip' -AllowDocumentationDescendant -Confirm:$false

# Back up and uninstall only KMC
& 'C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1' -Operation Uninstall -Confirm:$false
```

Before uninstalling a save that selected the KMC Horse, respec to a stock companion choice and save while unmounted. Removing a mod-defined companion blueprint from a save that still references it is unsupported.

## Focused manual checklist

1. Confirm UMM reports `Kingmaker Mounted Combat 0.1.0-phase3c-dev.13` and no diagnostic overlay is visible by default.
2. Create or load a real Ranger Horse and confirm exactly one visible, selectable, controllable Horse with Horse - not Mammoth - large, party, Ranger-selection, and feature art.
3. Select the rider, open the native abilities drawer, activate Mount Companion, and click the Horse. Repeat the target test with an eligible Mammoth if available; verify an ineligible target gives useful feedback.
4. Confirm no occupied hotbar slot was overwritten. If desired, drag the native Mount, Dismount, Rider Primary, or Mount Primary ability from the drawer to an empty slot.
5. On the rider's TB turn, use native Rider Primary on a visible hostile. Confirm immediate target cursor/click behavior, one attack/damage chain, and that Mount Primary is unavailable or explains the wrong turn.
6. On the Horse's TB turn, use native Mount Primary. Confirm one Horse Bite, one Horse Standard cost, visible native Bite motion, and that Rider Primary is unavailable or explains the wrong turn.
7. In RT, use native Rider Primary and Horse Primary. Confirm both deal damage once, no duplicate command or attack occurs, and the Horse attack animation is visually readable.
8. Compare solo unmounted Horse and mounted-pair movement, then issue a mixed-party group move. Inspect walk/run/turn/stop/reverse animation, sliding, and whether any slower group pace is ordinary formation behavior.
9. Inspect side, front, and three-quarter mounted views at idle and while moving. Check saddle contact, pelvis height, thigh width, knee line, feet near stirrups, neck/tack/torso clipping, shield clearance, and one-handed weapon clearance.
10. Open Inventory and the Character screen while mounted. Confirm the pair remains mounted, previews remain coherent, and no repeating `IKController.SetupFbbik` / `SetupIkSystem` exception appears.
11. Click a distant door while the mount is idle and confirm one mount approach, one rider-owned interaction, opening, and traversal. While the mount is already moving, confirm a clear mount-busy response rather than command replacement or duplicate interaction.
12. Verify SaveRequested and a true area transition cleanly dismount, loading starts unmounted, Wild Shape cleanly dismounts with visible transform/revert, and no automatic remount occurs.
13. Use native Dismount and confirm selection, view, pose, command, avoidance, control facts, and relationship clean up. Repeat a focused Mammoth Mount/Primary/Dismount check to confirm its profile and controls remain intact.

Report physical input feel, visual/animation quality, and rule/ownership failures separately. Screenshots from side/front/three-quarter views and the relevant KMC log lines are especially useful.

## Known private-alpha limitations

- Rider and Horse retain separate turn-based turns.
- Stock right-click/auto-attack integration is not implemented; use the native KMC primary abilities.
- Mounted ranged combat is unsupported.
- Mounted state remains transient and nonserialized; save/load and true area transitions remain unmounted.
- No automatic remount.
- Mixed-party movement feel, final portrait quality, final pose, physical-pointer usability, visible Bite readability, and real Inventory/Character-screen IK behavior are human gates.
- Paladin Divine Steed remains design only.
- This is not a public release and the branch is not merged to `main`.
