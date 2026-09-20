# Chunk 4 alpha playtest

This alpha distributes the exact qualified `0.1.0-chunk4-preview.54` payload. The version is retained so the release, local installation and 31 passing native scenarios identify the same bytes. Tested source: `429377d707a9976be65639e0c27954d8b4ff3717`. [Qualification, package hashes and remaining checks](CHUNK4-PLAYABILITY.md).

## Before mounting

In Kingmaker's Unity Mod Manager window, open **Kingmaker Mounted Combat** and check **Enable paired activation prototype (before mounting)**. Do this outside combat while dismounted, each time you launch the game. This build's developer configuration is session-only and starts with paired activation off. Leave unified mounted turn, paired command scheduler and diagnostic overlay off. The installed UMM configuration already enables the mod and shows UMM on startup.

Use one supported Horse or Mammoth pair and mount before combat. Rider and mount share the rider's activation but retain separate native action budgets. Ordinary melee/ranged attacks, explicit Primary attacks, movement, Stop and early End are supported in the tested conditions.

## Focused playtest

- Compare holding an ordinary attack order with repeated clicks. Try rider melee, ranged and eligible mount attacks, both adjacent and approaching. Include retargeting, pause/Stop and a dying target.
- In turn-based combat, vary which actor acts first; exhaust one actor while the other still has work, then try early End.
- Inspect/select both actors, heal or attack each separately, and try an area effect. Check a door, narrow route, turns, slope and party movement.
- Review the rider's seated motion, Horse strike and recovery from a clear side view, and the native action countdown/feedback. These remain human review items.
- Try Charge, then a legal move/attack and End. Mounted Charge must give a clear unsupported message without spending actions; unmounted Charge remains native.

## Known limits

Full mounted Charge is not implemented; safe rejection does not add the feature. Mid-combat mounting, cross-round Delay and broader mode-switch behavior remain limited. Mounted persistence/cold-load restoration is not qualified; use a disposable copy for the alpha and dismount before saving or changing sessions. Persistence is the next roadmap item, not part of this alpha's qualification.

When reporting a problem, include the mode, mount, selected actor, target/weapon, whether the pair was mounted before combat, the action sequence and expected/observed result. Visual, physical-input and human-play approval are separate from the native engineering passes.
