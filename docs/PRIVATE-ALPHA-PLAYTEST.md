# Private alpha playtest

## Stabilization notice

The artifact below is preserved historical qualification and is no longer the package to install for a new regression test. Human testing found rider visibility, non-world UI, ordinary overlay melee input, and turn-based control defects. A replacement package identity and focused checklist will be inserted only after the stabilization technical gates pass. Until then, status is `IN PROGRESS`; do not treat the historical package as the stabilized build.

Historical status: `PRIVATE ALPHA COMPLETE — PLAYTEST HANDOFF`

This is a private diagnostic playtest handoff, not a public release. Install only the exact artifact identity below.

## Final artifact identity

| Field | Required final value |
|---|---|
| Branch | `codex/mounted-combat-phase2-alpha` |
| Commit | `eae1abd554e67f8e864571a97d48f479a75304af` |
| Product version | `0.1.0-phase2b-dev.1` |
| Package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-diagnostic.zip` |
| Package SHA-256 | `6291a001513b490bf111eb34866634bd86c55183ba789d3dfc531854790e78b0` |
| Manifest SHA-256 | `16d01881350b2ce6172aecf27d47b630588d249ca3a199e1eb27f643e2c14cf4` |
| DLL SHA-256 | `9f715f4ddd4e086cee8f2aa3aaa4e746401cec113fc529809d1376eab1caf6c1` |
| DLL MVID | `292982ba-6486-4807-b4fa-c15dc0558266` |

Do not substitute an earlier diagnostic package. The ZIP, sidecar manifest, commit, DLL hash, and MVID are one inseparable identity.

## Supported profile and environment

- Pathfinder: Kingmaker Enhanced Plus Edition `2.1.7b` / Steam App `640820`.
- Unity Mod Manager `0.28.2`.
- One directly controlled Medium humanoid rider and that rider's exact active larger Mammoth companion.
- The qualified presentation uses the existing one-handed fixture equipment.
- Real-time and turn-based basic melee combat are in scope only as listed below.

Bag of Tricks, Kingmaker Buff Planner, and other mods are neither dependencies nor KMC-owned content. Their presence and behavior are outside KMC qualification.

## Installation

1. Close Kingmaker and Unity Mod Manager. Confirm no game, updater, Steam prompt, or mod deployment is active.
2. Verify the final ZIP and sidecar hashes with `Get-FileHash -Algorithm SHA256` and compare them character-for-character with the completed table above.
3. Open the ZIP and confirm it contains exactly one `KingmakerMountedCombat` directory with `Info.json` and `KingmakerMountedCombat.dll`; do not add foreign files.
4. Install that directory as a normal UMM mod, or extract it to the Kingmaker `Mods` directory so the resulting path is `Mods\KingmakerMountedCombat`.
5. Start Kingmaker normally. Verify UMM lists `Kingmaker Mounted Combat` at the exact product version before loading an expendable test save.

The guarded automation scripts are qualification tooling, not required for ordinary private-alpha play. Do not run a qualification request or use the protected KMC automation fixtures for casual testing.

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

## Suggested playtest sequence

Run the sequence independently in real-time and turn-based mode:

1. Mount, idle, walk, run, turn, reverse, stop, and pass a doorway.
2. Switch selection away and back; observe portrait, action bar, selection circle, camera follow, and group movement.
3. Make a stationary rider melee hit and miss when naturally available.
4. Make a rider movement-to-attack from clearly outside melee range.
5. Make a stationary Mammoth primary attack.
6. Cancel one approach with Stop/Hold; interrupt another with a different command.
7. Let combat end while mounted, then begin a new combat.
8. On expendable state only, observe cleanup after rider or Mammoth unconsciousness/death and companion/pair invalidation.
9. Dismount, save, load, change area, disable/re-enable the mod, and confirm no relationship or visual residue returns automatically.
10. Repeat any defect once if safe, recording RT/TB mode, exact preceding actions, target, and whether restoration remained clean.

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
