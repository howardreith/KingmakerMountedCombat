# Mr. Bard's Kingmaker Mounted Combat 0.2.16-alpha.2 (alpha)

**Alpha prerelease.** 0.2.15 remains the latest stable release. **Nothing in this alpha was run in the game before
release, not even startup.** Use a separate save (see Install).

This alpha combines the Mounted Shield feat from
[0.2.16-alpha.1](https://github.com/howardreith/KingmakerMountedCombat/releases/tag/v0.2.16-alpha.1) with the Unity Mod
Manager 0.31.1 loader baseline. It builds on
[0.2.15](https://github.com/howardreith/KingmakerMountedCombat/releases/tag/v0.2.15).

## Added (from 0.2.16-alpha.1, unchanged)

- **Mounted Shield** (feat; requires Mounted Combat and Shield Focus). While mounted with an effective physical shield,
  your mount gains a Shield bonus to AC equal to the shield's base shield bonus + 1 (Shield Focus), + 1 more with Greater
  Shield Focus. Enhancement does not count. The same amount is added once to your Mounted Combat check to negate a hit
  on the mount.

## Changed

- **Unity Mod Manager minimum is now 0.31.1** (0.2.15 declared 0.28.2). The mod and the removal observer are compiled
  against hash-pinned UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1 references, independent of the loader installed
  on the build machine.

## Loader compatibility evidence

| Item | Status |
|---|---|
| Declared minimum (`Info.json` `ManagerVersion`, mod and removal observer) | UMM 0.31.1 |
| Pinned compile baseline | UnityModManager 0.31.1.0, 0Harmony12 1.2.0.1 (hash-pinned, `scripts/loader-reference-pins.json`) |
| Static loader API audit | Every UnityModManager and 0Harmony12 member each DLL binds resolved on UMM 0.31.1, 0.32.4, 0.33.0 and 0.33.4 (static only) |
| Out-of-game loader binding checks | The Mounted Shield focused gate checks the compiled mod's loader bindings against the genuine 0.31.1 reference and the installed 0.33.0 loader (offline, no game start) |
| Native gameplay and combined mod-stack testing | **NOT RUN** on any loader version |

The static audit and the offline binding checks are not native compatibility testing, and they make no claim about
future UMM versions.

## Known limitations (alpha)

- Mounted Shield has **not** been observed in the game. Feat selection and icon, the amounts, enhancement exclusion,
  competing shield bonuses, the Mounted Combat check, equipment changes, dismount and save/reload are all still to be
  checked in play ([playtest checklist](https://github.com/howardreith/KingmakerMountedCombat/blob/v0.2.16-alpha.2/docs/mounted-shield/PRIVATE-ALPHA.md#owner-playtest-checklist-priority-order)).
- A foreign mod that selects feats while ignoring prerequisites can, for one frame, see Mounted Shield in a list before
  this mod withdraws it (optional compatibility limit; stock selection is unaffected).
- The historical Sprint 6 diagnostic rows still force check dice above 20 by design; this affects agent diagnostics only,
  not play.
- Everything listed as open for 0.2.15 still applies, including the post-combat save and door lock (X05).

## Install

1. Back up `%USERPROFILE%\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games` and your current
   `<Kingmaker>\Mods\KingmakerMountedCombat` folder.
2. Use a **separate save** (or a copy) for alpha testing. Once a character takes Mounted Shield, that save references the
   new feat; loading it later with 0.2.15 (which lacks the feat) is untested and may drop the feat or fail.
3. Close the game. Unzip the `KingmakerMountedCombat` folder from
   `KingmakerMountedCombat-0.2.16-alpha.2-release-diagnostic.zip` into `<Kingmaker>\Mods\`, replacing the earlier version.
4. UMM lists **Mr. Bard's Kingmaker Mounted Combat** with version `0.2.16-alpha.2`.

The second asset, `KmcRemovalObserver-0.1.0-observer.8-release-diagnostic.zip`, is the separately shipped **KMC Removal
Observer**: a minimal diagnostic mod that does nothing unless started with its own command-line request, used to check a
save loaded without Mounted Combat. You do not need it to play.

## Roll back

Close the game, restore your backed-up `KingmakerMountedCombat` folder (for example 0.2.15), and use saves made before the
alpha.

## Testing

- **Offline only.** The release gates, packages and their identities are recorded on this GitHub release and in
  `CURRENT.md` at the release tag.
- **Not tested in the game:** startup, the Mounted Shield feat in play, and any loader version.
