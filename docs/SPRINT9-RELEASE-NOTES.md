# Kingmaker Mounted Combat: release candidate preview.101 (draft notes)

**Not published.** Owner acceptance is pending, and publication needs the owner's explicit decision. Package and hashes: [the acceptance record](SPRINT9-INTEGRATED-ACCEPTANCE.md).

## Supported

- Kingmaker 2.1.7 with Unity Mod Manager. No other mod is required.
- Ride your companion: the KMC Horse or Riding Dog from a Ranger's companion choice or a Paladin's Divine Bond, or a supported stock Mammoth.
- Medium riders on the Horse or Mammoth. Halfling and Gnome riders on the Horse or Riding Dog.
- Up to six mounted pairs. Two pairs are tested in combat. Six are tested for exploration and saving, not combat.

## What the sprints added

- **Saving:** Manual, Quick and Auto work while mounted, including overwrite and area autosaves.
- **World:** one-click doors and carried touch spells, with fair movement speed.
- **Combat:** ordinary clicks, native attacks and Haste, retargeting on a kill, Stop and Hold respected. Rider Primary and Mount Primary are gone.
- **Small riders:** proper seating and rigs, plus the Riding Dog.
- **Mounted Combat feat:** a native feat with Mounted Combat's hit-negation reaction.
- **Mounted Charge:** real time and turn-based.
- **Paladin:** Divine Bond at level 5 offers Weapon Bond or Divine Mount.

## Fixed in this candidate

- Divine Mount now registers on a game without Call of the Wild. It previously left the Paladin on the stock bond.
- A mounted Divine Mount can use Celestial Smite in turn-based combat, including as the turn's first action.
- The mod's UMM page no longer mentions retired actions, and it states the current save dependencies.

## Settings

Open them in UMM with Ctrl+F10. **Enable Mounted Charge** is on by default, can change only outside combat with every pair dismounted, and is not saved: it turns on again at the next launch. The Mounted Combat reaction has its own in-game toggle. Everything else is fixed.

## Known limits

- **Traps:** riding up to disarm a trap has never been executed by the automated tests.
- **Turn-based doors** have not been demonstrated.
- **Without Call of the Wild:** registration, mounting, movement and saving are tested. Paladin and Riding Dog gameplay is not. The stock game should level a pet by itself and rest it, which may refill Celestial Smite.
- **Paladin** was driven through the native level-up controller, not on-screen clicks, and its class panel still lists weapon upgrades a mount path never receives.
- **Adaptations:** Mounted Charge is the rider's own charge, with the mount paying its travel, rather than a mount charge. A Divine Mount is an ordinary persistent companion that recovers if it falls; it gets a flat +4 Intelligence and real celestial defenses, but is not reclassified as a magical beast and does not gain the template's own spell resistance.
- **Celestial Smite** has one current target per mount and lasts one day. **A rest returns the use and does not clear the mark.**
- Of a mount's own abilities, only Celestial Smite works from the saddle in turn-based combat. A first-action Smite beyond range waits until the rider acts, with no message, and the mount may then try to approach on its own; that path has not been played.
- Other mods are untested beyond the Call of the Wild and Craft Magic Items the fixtures use. Borrowed, summoned, flying or exotic mounts, barding, Cavalier, enemy mounted AI and six-pair combat are not supported.

## Upgrade

Back up `Saved Games` first. Replace the two files in `Mods\KingmakerMountedCombat\`, then load.

- One authentic preview.211 save was loaded with its exact Call of the Wild and Craft Magic Items, resaved and cold-loaded. That is the upgrade evidence. A save needs the same mods it was written with.
- **Characters from older saves are not converted.** A Paladin already past level 5 keeps the stock bond and is offered nothing. The native respec is the route to change.

## Rollback

Rollback is **your pre-upgrade backup plus the matching older package and its dependencies**. A save written by this version is not certified under an older one.

## Removal

**These can tie a save to KMC:** a custom Horse or Riding Dog, the Mounted Combat feat, and Divine Bond. **Weapon Bond counts too** when chosen through KMC's level-5 selection. An older stock-bond character does not.

- **Clean uninstallation from a dependent save is unproven.** Keep the mod and its dependencies, or restore a pre-upgrade backup.
- **Prepare to disable / remove KMC** (the UMM page) refuses and names each reference when it finds one.
- If it succeeds, the campaign had no permanent reference: it dismounts, writes one new save and verifies it. That save was loaded with the mod absent, on a stock Mammoth campaign. It says nothing about a campaign that used KMC content.
- Never edit a save to strip KMC data.
