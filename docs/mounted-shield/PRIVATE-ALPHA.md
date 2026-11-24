# Mounted Shield — private alpha (owner amendment 2, 2026-11-23)

Milestone: an installable **private alpha** for the owner's own playtesting (owner amendment 2; citation in
[HANDOFF.md](HANDOFF.md) section 0). Broader in-game gameplay, persistence, compatibility and UI acceptance is **deferred to
the owner's alpha playtest** and is listed below as outstanding. This is not a release, not full feature acceptance and not
native qualification.

> **NO NATIVE TESTING OCCURRED.** The agent did not start the game in this continuation (zero guarded starts); the alpha has
> not been loaded under UMM, and nothing listed under "Owner playtest checklist" has been observed in the game.

## What was actually run

- Controller focused Fast gate on the exact source: PASS (build on the pinned UMM 0.31.1 reference; 1525 component tests; source
  validation 153; 18 focused scripts including selection rollback, Mounted Shield readers, native contracts on genuine engine
  types, and UMM binding checks against 0.31.1 and the installed 0.33.0). Offline only.
- Package validation 11/0 and embedded-icon identity on this exact DLL.
- Independent read-only code/safety review: PASS FOR GUARDED NATIVE TESTING on this source (no live use planned).
- Not run: any game start (the no-save smoke was dropped because that path does not snapshot UMM settings), any save
  creation, any gameplay.

## Package

| Item | Value |
| --- | --- |
| Version | `0.2.16-alpha.1` (version.json, Info.json `Version`, `BuildIdentity.ProductVersion`; assembly version stays `0.1.0.0`) |
| Loader | Info.json `ManagerVersion` `0.31.1` (build reference and declared minimum); installed native target UMM 0.33.0 unchanged |
| ZIP | `artifacts/KingmakerMountedCombat-0.2.16-alpha.1-mounted-shield-bb903669-diagnostic.zip`, SHA-256 `fa5fb284e889e414093ee8b939deb330084eb029f3d02f622f094ac6444fd34a`, 4282758 bytes |
| DLL | `KingmakerMountedCombat.dll`, SHA-256 `91e87536f7a37270ddac7db2dfd6942926cb385a844e3452a4b2f471626e2918`, MVID `62f8f8fa-7b05-4d10-91bb-0181635dfa9c` |
| Icon | one embedded `KingmakerMountedCombat.Assets.MountedShieldFeat.png`, SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |
| Source | commit `bb903669c038c9b35802e467c5ba3394b586997b` on `codex/mounted-shield` (draft PR #61) |

The packaging path names every package `…-diagnostic.zip` (repository convention for agent-built packages); the alpha
identity is the version and this record.

## Install (manual, by the owner)

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`, and the current
   `<Kingmaker>\Mods\KingmakerMountedCombat` folder (your installed 0.2.15).
2. Use a **separate save** (or a copy) for alpha testing. Once a character takes Mounted Shield, that save references the
   new feat; loading it later with 0.2.15 (which lacks the feat) is untested and may drop the feat or fail.
3. Close the game. Copy the package's `KingmakerMountedCombat` folder into `<Kingmaker>\Mods`, replacing the old copy.
4. Start the game. UMM lists **Mr. Bard's Kingmaker Mounted Combat** with version `0.2.16-alpha.1`.

## Roll back

1. Close the game.
2. Replace `<Kingmaker>\Mods\KingmakerMountedCombat` with your backup (0.2.15: package
   `KingmakerMountedCombat-0.2.15-release-diagnostic.zip`, SHA-256 `0b650bb3dea5003e36606a217d1f2269f1b42b69050d35b5f1785bc18d228af8`).
3. Use saves made before the alpha (or restore the backed-up save folder). Do not load alpha saves in which Mounted Shield was
   taken with 0.2.15.

## Owner playtest checklist (priority order)

Rule under test: a mounted rider with Mounted Combat, Shield Focus and Mounted Shield, carrying an effective physical shield,
gives the mount a Shield bonus equal to the shield's **base** shield bonus + 1 (Shield Focus) + 1 more with Greater Shield
Focus; enhancement does not count; the same amount is added once to the rider's existing Mounted Combat hit-negation
Mobility check. No new toggle; the passive AC stays when the Mounted Combat preference is off or its use is spent.

1. **Feat selection and icon.** At a feat choice (ordinary and fighter bonus lists): Mounted Shield appears with the supplied
   icon, is refused without Mounted Combat or without Shield Focus, is selectable with both, and Back/cancel at the level-up
   screen behaves. Check the feat's name, description and icon in the feat list, level-up and character sheet.
2. **Amounts.** Mounted rider, heavy steel shield (base +2), Shield Focus: mount AC +3 (Shield). Add Greater Shield Focus:
   +4. Light shield/buckler (base +1): +2 (+3 with Greater).
3. **Enhancement exclusion.** A +1/+3/+5 heavy shield gives the same +3 as a mundane one.
4. **Competing shield bonuses.** Cast Shield (the spell) on the mount: the mount's AC shows the higher Shield bonus only
   (bonuses do not stack); Mounted Shield's own check bonus to the rider's Mobility still applies.
5. **Mounted Combat check.** When the mount is hit and the rider's Mounted Combat check fires, the combat log shows the check
   with "(Mounted Shield +3)"; equality fails, one above succeeds (the existing Mounted Combat rule).
6. **Equipment changes.** Unequip the shield, switch to a weapon set without the shield, or equip a tower shield: the mount's
   bonus updates (tower base +4 → +5 with Shield Focus).
7. **Dismount.** Dismounting removes the bonus; remounting restores it.
8. **Save/reload.** Save while mounted with the bonus, load: exactly one Mounted Shield bonus on the mount (not doubled), and
   it still updates on equipment changes.

Also outstanding for the owner's alpha (not exercised by the agent): real-time and turn-based combat cases at length,
reaction preference off and spent use, two mounted pairs at once, respec, shield bash and buckler-specific behavior, rider
or mount death, area transitions, other mods present (Tabletop Rules, Cutscene Skip, Buff Planner, Gunslinger), Brilliant
Energy weapons, and complete UI inspection.

## Known limitations

- Native gameplay of Mounted Shield has **not** been observed by the agent in this or any earlier continuation (see HANDOFF.md).
- PN3-F5: a foreign mod that selects feats ignoring prerequisites can, for one frame, see Mounted Shield in a list before
  KMC withdraws it (optional compatibility limit; stock selection is unaffected).
- The historical Sprint 6 diagnostic rows still force check dice above 20 by their original design (PN14-F5); this affects
  agent diagnostics only, not play.
- The supplemental pre-native review loop did not return PASS in fourteen rounds; the last three found no defect in the
  shipped feature (details in PRE_NATIVE_REVIEW.md).
