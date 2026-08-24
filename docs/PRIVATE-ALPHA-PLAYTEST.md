# Private alpha playtest

## Round 2 final manual regression — superseding handoff

Status: `PRIVATE ALPHA STABILIZATION ROUND 2 COMPLETE  MANUAL REGRESSION REQUIRED`.

Use only the immutable package at `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip` after matching its ZIP, adjacent manifest, DLL, MVID, branch, and commit to `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression-identity.json`. The external identity record supersedes every historical artifact table below. Do not install a historical package from `artifacts\historical`.

Do not manually extract into or curate the live `Mods` tree. With Kingmaker and Unity Mod Manager closed, install through the guarded helper:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Install -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip" -Confirm:$false
```

If an exact KMC deployment already exists, use `-Operation Replace` with the same package. After play, uninstall and verify absence:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Uninstall -Confirm:$false
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation VerifyAbsent
```

Use only an expendable non-protected save. Never alter `KMC_AUTOMATION_BASELINE`; do not use KMC automation fixtures for ordinary manual play.

### Focused checklist

Record `PASS` or `FAIL`, a concise observation, and any screenshot/log timestamp for every row:

| # | Manual exercise | Required observation |
|---|---|---|
| 1 | RT rider melee, adjacent | One rider attack/roll/damage/resource chain; no Mammoth attack or duplicate |
| 2 | RT rider melee, approach required | Mammoth alone approaches; rider performs one supported melee attack |
| 3 | TB Mammoth turn movement | Ordinary ground click visibly moves the Mammoth/pair; native turn and controls remain understandable |
| 4 | TB rider turn movement and melee | Rider-turn ground input routes through the Mammoth, then one rider melee action uses the rider ledger |
| 5 | RT-to-TB and TB-to-RT | Pair, rider selection, portrait, action bar, camera, attachment, and pose remain coherent |
| 6 | Distant door approach/open/traverse | One Mammoth approach, one rider-owned open, doorway traversal, no repeat/oscillation/backtracking |
| 7 | Menus and fog/world flash | Character, inventory/spellbook, map, pause, and combat menus preserve visible mounted pair and usable UI; report latency or any fog/world flash |
| 8 | Wild Shape and revert | Clean dismount; transformed rider remains visible/controllable; reverted rider view, parent, pose, and selection are clean |
| 9 | Mounted ranged rejection | RT and TB reject visibly and deterministically without partial command/resource change |
| 10 | Unmounted ranged control | The same ranged action remains stock after dismount |
| 11 | Mammoth primary | Exactly one Mammoth natural attack and Mammoth-owned Standard cost; no rider attack |
| 12 | Save/load and area boundary | Intentional clean dismount; load/arrival remains unmounted; no automatic remount or residue |
| 13 | Explicit Dismount | Rider/Mammoth visibility, selection, portrait/action bar, camera, pose, parent, agents, and controls restore cleanly |

Also assess ordinary mouse target selection, physical pointer feel, button feedback, clipping, rider leg/mount contact, weapon pose, camera framing, and overall presentation. These are human judgments; internal ownership fields and automation do not substitute for them.

Stop and report immediately on a competing rider path, separation, wrong actor/target/weapon/turn/resource, duplicate movement/command/attack/roll/damage/interaction/opportunity chain, real path/target failure, repeated door interaction, rider disappearance, stale attachment/pose/UI state, save residue, unexpected dialog, Steam/Steam Guard/account/cloud/update prompt, or deployment/restoration ambiguity.

Human acceptance must explicitly name the exact package SHA-256, manifest SHA-256, DLL SHA-256, and MVID. It authorizes neither a `main` merge nor horse execution by itself; those remain separate decisions.

## Historical stabilization notice

Automated stabilization is `PASS`: fresh same-package RT A/B, TB A/B, and doorway qualification total `434/0`, with exact independent save/Mods restoration after every process. The remaining checkpoint is a focused human regression for UI-screen visibility, polymorph/revert visibility, ranged rejection/control, and ordinary play feel.

Historical status: `PRIVATE ALPHA COMPLETE — PLAYTEST HANDOFF`

This is a private diagnostic playtest handoff, not a public release. Install only the fresh package named in the final handoff response and its adjacent sidecar manifest. The historical artifact below remains immutable evidence and must not be installed as the stabilized build.

## Historical artifact identity — do not install for stabilization regression

| Field | Preserved historical value |
|---|---|
| Branch | `codex/mounted-combat-phase2-alpha` |
| Commit | `eae1abd554e67f8e864571a97d48f479a75304af` |
| Product version | `0.1.0-phase2b-dev.1` |
| Package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-diagnostic.zip` |
| Package SHA-256 | `6291a001513b490bf111eb34866634bd86c55183ba789d3dfc531854790e78b0` |
| Manifest SHA-256 | `16d01881350b2ce6172aecf27d47b630588d249ca3a199e1eb27f643e2c14cf4` |
| DLL SHA-256 | `9f715f4ddd4e086cee8f2aa3aaa4e746401cec113fc529809d1376eab1caf6c1` |
| DLL MVID | `292982ba-6486-4807-b4fa-c15dc0558266` |

The final stabilization package uses the same artifact path after guarded rebinding to the final clean documentation HEAD. Verify its ZIP, sidecar manifest, commit, DLL hash, and MVID against the final handoff response; those values are one inseparable identity. Do not use the historical hashes above for the new regression.

## Supported profile and environment

- Pathfinder: Kingmaker Enhanced Plus Edition `2.1.7b` / Steam App `640820`.
- Unity Mod Manager `0.28.2`.
- One directly controlled Medium humanoid rider and that rider's exact active larger Mammoth companion.
- The qualified presentation uses the existing one-handed fixture equipment.
- Real-time and turn-based basic melee combat are in scope only as listed below.

Bag of Tricks, Kingmaker Buff Planner, and other mods are neither dependencies nor KMC-owned content. Their presence and behavior are outside KMC qualification.

## Historical installation guidance — superseded

Do not use the former manual extraction/UMM installation path. Use only the guarded install, replace, uninstall, and verification commands in the superseding Round 2 section above. The qualification scenario scripts are not manual-play launchers, and protected KMC automation fixtures remain off-limits for casual testing.

## Controls and expected ownership

The bottom-right `Kingmaker Mounted Combat` overlay is the only player-facing surface.

1. Select the supported rider as the sole selected unit.
2. Click `Mount`. The rider remains the selection, portrait, action-bar, and camera principal; the Mammoth becomes the sole pathfinding authority.
3. Use ordinary ground movement. The Mammoth should path and the rider should remain attached without an independent rider path.
4. In combat, click `Rider melee`, then one visible hostile target. The rider owns exactly one basic melee attack and its Standard cost. If approach is required, the Mammoth moves the pair; in turn-based mode the rider owns the movement accounting.
5. Click `Mammoth primary`, then one visible hostile target. The Mammoth owns exactly one primary natural attack and its Standard cost; the rider does not also attack.
6. Ordinary Stop/Hold, command interruption, target invalidation, combat end, incapacitation, death, pair invalidation, or `Dismount` must leave no half-active pair or command residue.
7. Click `Dismount` before saving, loading, changing area, disabling the mod, or ending the session. KMC also attempts fail-safe cleanup at those boundaries.

Rejected clicks or actions should explain the reason and make no partial change. Do not interpret a disabled button as a request to alter the character, save, or another mod to satisfy the test.

## Focused stabilization regression checklist

Use an expendable save and the supported Medium-humanoid/Mammoth/one-handed profile:

1. Mount, then open and close the character sheet, inventory/spellbook, map, pause menu, and combat menu. Confirm the rider stays mounted, visible, selected, attached, and usable.
2. While mounted, Wild Shape to the previously failing elemental form. Confirm a clean dismount and a visible, controllable stock elemental. Revert and confirm the ordinary humanoid view is visible, selected, and free of stale pose/parent residue.
3. In real time, use `Rider melee` through the overlay and click a hostile target. Confirm one rider attack and useful rejection feedback if the click is invalid.
4. Enter turn-based mode while mounted. Confirm rider and Mammoth roster/turn representation remains understandable, ground movement on the rider turn moves through the Mammoth, and `Rider melee` performs one rider attack.
5. Exit turn-based mode while still mounted. Confirm the rider remains selected and owns the portrait, action bar, and camera without pressing Mount again.
6. Use `Mammoth primary` once and confirm only the Mammoth attacks and pays its own Standard action.
7. Open and traverse a door while mounted; confirm the pair remains mounted and mobile.
8. Attempt a mounted ranged attack in RT and TB; confirm the deterministic private-alpha rejection. Dismount and confirm the same ranged attack remains stock.
9. Save and perform a true area transition while mounted; confirm the intentional clean dismount message. Loading remains unmounted and automatic remount does not occur.
10. Finish with explicit `Dismount`; confirm rider/Mammoth visibility, selection, action bar, camera, pose, parent, and movement are clean.

## Report immediately

- Rider and Mammoth both pathfinding, competing, or separating.
- More than one rider/Mammoth attack, roll, damage, or action cost from one click.
- Any unexpected `Attack of Opportunity` or `Charge` behavior attributed to KMC.
- Wrong attacker, target, weapon, turn, Standard cost, or movement cost.
- Command, selection, UI, camera, pose, or relationship residue after cancel/dismount/combat/lifecycle boundaries.
- A non-mounted unit behaving differently while KMC is installed.
- A save or foreign mod changed by KMC, or any failure to restore the pre-run state.
- Any Steam, cloud, account, update, or credential prompt; leave it unanswered and end the test safely.

For a useful defect report, provide the product version, commit/package hash, RT or TB mode, concise reproduction steps, observed versus expected result, `Player.log`, and a screenshot when presentation/UI is involved. Do not send saves containing valued or private campaign data.

## Known limitations

- Exact Medium-humanoid/Mammoth/one-handed profile only.
- Slight seat gap or hovering and a stiff analytical pose are accepted private-alpha limitations.
- No saddle or reins; doorway/camera occlusion and the native bright-blue selection silhouette can obscure the pair.
- No generalized reach weapons, alternate rider bodies, or alternate mount anatomy.
- Explicit mounted attacks of opportunity are absent/default-off. The active-command guard only prevents synchronization-created duplicate stock opportunities.
- Basic mounted charge is absent/default-off.
- Ranged mounted combat, mounted spellcasting, mounted feats, additional mounts, Small riders, enemy riders, persistent mounted state, and automatic remount are unsupported.
- The relationship is intentionally transient and nonserialized. Never rely on a mounted state surviving save/load, area change, mod disable, or uninstall.

## Uninstall

1. Dismount and make a normal unmounted save only after confirming the pair and UI are clean.
2. Exit Kingmaker completely.
3. Remove only the exact `Mods\KingmakerMountedCombat` directory installed from the verified package, preferably through UMM.
4. Do not edit or remove Bag of Tricks, Kingmaker Buff Planner, another mod, or any save as part of KMC uninstall.
5. Relaunch and verify no KMC overlay or relationship returns. Because KMC adds no serialized relationship, blueprint fact, buff, unit part, or hotbar record, no save migration is expected.

No public release, support guarantee, or compatibility claim follows from this private handoff.
