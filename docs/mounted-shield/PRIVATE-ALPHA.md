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
- Independent read-only code/safety review: PASS FOR GUARDED NATIVE TESTING on `bb903669` (no live use planned). The controller's
  final review rounds 1 and 2 then required corrections, made in `6fb53633` and `fedb7008` (this source); its next round
  reviews them.
- Not run: any game start (the no-save smoke was dropped because that path does not snapshot UMM settings), any save
  creation, any gameplay.

## Package

| Item | Value |
| --- | --- |
| Version | `0.2.16-alpha.1` (version.json, Info.json `Version`, `BuildIdentity.ProductVersion`; assembly version stays `0.1.0.0`) |
| Loader | Info.json `ManagerVersion` `0.31.1` (build reference and declared minimum); installed native target UMM 0.33.0 unchanged |
| ZIP | `artifacts/KingmakerMountedCombat-0.2.16-alpha.1-mounted-shield-fedb7008-diagnostic.zip`, SHA-256 `8c1c5ae99c03dc53eab0cb64cd4a6f67685b61af0d26aebddb32a798d08c653c`, 4,283,148 bytes |
| DLL | `KingmakerMountedCombat.dll`, SHA-256 `2822469373d20a47eaff21c305c8325329dfbc10f5138ea95ce46a8b9476fe41`, MVID `c4e178d8-355d-4a79-93ad-ff1af7cb7c7b` |
| Icon | one embedded `KingmakerMountedCombat.Assets.MountedShieldFeat.png`, SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |
| Source | commit `fedb70086495599273824a500da3b2b5c7122c08` on `codex/mounted-shield` (draft PR #61) |

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
