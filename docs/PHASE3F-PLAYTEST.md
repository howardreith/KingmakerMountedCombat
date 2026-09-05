# Phase 3F private native-control playtest

Status: HUMAN VALIDATION PENDING. Partial candidate `0.1.0-phase3f-preview.2`, source `a4f54bf959d32caa871baeba3117b081362552ed`. No Phase 3F game session has run. Test only a disposable `KMC_AUTOMATION_WORKING` fixture through the guarded harness. One active mounted pair; separate native TB turns. **TB movement resources remain unqualified.** This preview does not claim full attacks or Wrath parity.

Both experimental flags and the diagnostic overlay must be off. All controls below are native abilities/action-bar bindings; no UMM window or diagnostic overlay is needed.

1. Select the rider, use **Mount Companion**, click the rider's exact Horse, then ground-click to walk. The Horse should move the pair; mounting/dismounting should work repeatedly without duplicates. Repeat with an independently supported Mammoth.
2. Watch idle breathing, walking, turning, stopping, Horse Bite and rider melee/ranged attacks, then return to idle. Check the seat from front, side and rear outdoors and indoors. Record a short clip if the rider floats, moves toward the neck, rolls, stretches or lags.
3. With rider-only selection, ordinary-click an adjacent hostile, then a fresh distant hostile. Expect legal melee attacks, Horse approach when needed, and repeated attacks from one click while the target survives. Repeat with the whole party selected: allies retain their own commands and the pair does not double-dispatch.
4. Equip a bow/crossbow/sling and ordinary-click a surviving distant hostile. Expect repeated rider shots from genuine weapon range/LoS. The Horse should not close for Bite. Observe normal native ammo/reload, misses and damage attribution.
5. Try a paused hostile click then unpause, and an out-of-combat hostile initiation. Repeat Stop/Hold, replacement ground movement, retarget and explicit **Rider Primary/Mount Primary**. Expect clear precedence and no new attacks from canceled input. Record separately any already released projectile or native opportunity attack.
6. In TB, ordinary-click on the rider's native turn, then try the mount on its own turn. Only that actor may attack. Explicit primaries should remain usable; an attack or cancellation should leave remaining native actions available. **Do not mark movement costs passed:** log partial movement on rider-before-mount and mount-before-rider order across two rounds, including Standards spent, Stop, remount and mode changes. This is an outstanding blocker.
7. Inspect Mount's cyan up arrow and Dismount's gold down arrow at actual action-bar size, including hover, disabled and cooldown states. Check tooltips and existing bindings. Neither control may overwrite an occupied binding.
8. Dismount. In a guard-authorized Working-write session, save only the Working fixture and reload it through the harness. Otherwise mark this step NOT RUN; never write in a read-only review session. Expect no mounted state or automatic remount; the legitimate Horse companion/feature should remain. Area/load, incapacity and changed-view boundaries must clean temporary control/pose leases.

Report the exact version, fixture/scenario, selection, RT/TB mode, weapon and target for each issue. Mark your own PASS/FAIL; no human result is pre-filled here. Do not use valued saves. Removing the mod while a save still references its Horse companion/feature is unsupported; transient mounting alone does not make uninstall safe.

## Installation

The existing fallback remains installed unchanged. The candidate's complete hashes and limitations are in [the implementation report](PHASE3F-PLAYABLE-CORE-IMPLEMENTATION.md). Its ZIP SHA-256 is `57fe254d5d39773750cba22ef47380d36d69fc5e92c6be5f759ae4d7ab6e2aa2`.

Close Kingmaker and UMM. From host PowerShell on `codex/mounted-combat-phase3f-playable-core`, review the exact guarded replacement first (this WhatIf was verified):

```powershell
$deploymentGuard = 'C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1'
$candidatePackage = 'C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3f-preview.2-playable-core-private-preview-diagnostic.zip'
& $deploymentGuard -Operation Replace -PackagePath $candidatePackage -AllowDocumentationDescendant -WhatIf
```

**Only after choosing permanent installation**, run the following in that same host session. This mission did not run these mutation commands:

```powershell
& $deploymentGuard -Operation Replace -PackagePath $candidatePackage -AllowDocumentationDescendant -Confirm:$false
& $deploymentGuard -Operation VerifyInstalled -PackagePath $candidatePackage -AllowDocumentationDescendant
```

The guard backs up the exact current KMC payload and checks foreign Mods. Never manually copy/delete the live Mods tree. Installation alone is not runtime qualification or permission to use valued saves.

Outstanding setup gates: one Steam client already running in verified Offline Mode; a correctly prepared guarded Horse/Working review session; and native pointer/visual observation. The historical Mammoth/overlay/read-only manual-review setup does not satisfy this checklist and cannot authorize step 8. No diagnostic overlay or Primary-only script can substitute for the ordinary-input steps. These setup and gameplay gates remain pending; do not start an unguarded session to bypass them.
