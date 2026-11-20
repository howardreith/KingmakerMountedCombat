# Mr. Bard's Kingmaker Mounted Combat 0.2.13

Released by owner order on 2026-11-20. This update to [0.2.12](RELEASE-NOTES-0.2.12.md) fixes one release-blocking
problem: 0.2.12 did not load beside Mr. Bard's Tabletop Rules Expansion. Nothing else changes. The current support,
limits, removal and rollback contract is [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md). Every issue's status is in the
[current issue ledger](CURRENT-KNOWN-ISSUES.md).

## Fixed

- **0.2.12 did not load beside Tabletop Rules Expansion (X01).**
  - **Before:** with Tabletop Rules Expansion (mod id `KingmakerGunslinger`) installed, Unity Mod Manager reported
    Mounted Combat 0.2.12 as `Not loaded`. A campaign that uses Mounted Combat content then failed to load, with
    `Failed to load blueprint by guid 3c81f5a02be74d96a4e07d1c92b6f385` followed by a `LoadGameException`.
  - **Cause:** both mods change the same game method, the one that turns a click on a creature into an order. 0.2.12
    replaced one of that method's order submissions, and Tabletop Rules Expansion rewrites all three of them. Whichever
    mod loaded second refused to start. 0.2.11 and earlier did not change that submission, so they were not affected.
  - **Now:** Mounted Combat leaves the submission in place and only decides, before it runs, whether a mounted
    rider's move should go to the horse instead. Both mods load in either order. Firearm attacks, casts, looting and
    interaction go through Tabletop Rules Expansion exactly as before. Without Tabletop Rules Expansion, behaviour is
    the same as 0.2.12.
  - **Your save:** nothing in it is changed or removed. The failure came from Mounted Combat not starting, which left
    its blueprints missing for that session. Loading the affected save with 0.2.13 has **not** been tested here; see
    [How this binary relates to the testing](#how-this-binary-relates-to-the-testing).

## Unchanged

Everything else is as in [0.2.12](RELEASE-NOTES-0.2.12.md), including its retained limits, the shooting-from-the-saddle
rule and the still-open items listed there.

## Upgrade

1. Back up `Saved Games`.
2. Close the game. Replace the `Mods\KingmakerMountedCombat\` folder with this package's.

### Saves

- Saves made with 0.2.0–0.2.12 load in 0.2.13. 0.2.13 writes saves exactly as 0.2.12 does.
- **Going back:** 0.2.12 reads 0.2.13 saves, but 0.2.12 does not load beside Tabletop Rules Expansion. With that mod
  installed, roll back to 0.2.11 (see the [0.2.12 notes](RELEASE-NOTES-0.2.12.md#saves) for what 0.2.11 reads), or
  remove it. Rollback otherwise means your pre-upgrade backup and the older package
  ([details](CURRENT-SUPPORT.md#rollback-and-downgrade)).

## How this binary relates to the testing

- **Offline:** a new regression registers both mods' real code with the installed patching library, in both load
  orders and each mod alone. It then runs the combined code on real game objects: the correct horse moves, each order
  is submitted once, and firearm attacks and casts reach Tabletop Rules Expansion unchanged. Released 0.2.12 fails it
  with the reported error (2 pass, 5 fail); the fix passes 21/0. The game's ground-order handling and the actual
  running of orders are simulated in that check, so it does not show firearm gameplay.
- **In the game, startup only:** this release's own DLL, and before it the reviewed fix, each started in a fresh game
  process beside the Tabletop Rules Expansion **0.0.140** installed on this machine. Both mods loaded `Active` and
  Mounted Combat registered its feats with no failure. The guarded test harness recorded both runs as failed for one
  reason: this machine runs Unity Mod Manager 0.33.0, while the harness pins 0.33.2. 0.33.0 is also the version in
  the reported log.
- **Not tested in the game:** startup beside Tabletop Rules Expansion **0.0.191** (the reported version), loading the
  reported save, a mounted click, and a firearm attack.
- **Package:** this release's DLL is rebuilt with the new version number, so its hash differs from the reviewed test
  build (`5d79c78c…`). The source is the same except for the version number. The release package's hashes are on the
  GitHub release; the DLL started in the game above is that release DLL.

**Engineering record:** [the repair handoff](GUNSLINGER-CLICK-COMPAT-REPAIR-HANDOFF.md) (PR #55, merged into main as
`a4d25e45`).
