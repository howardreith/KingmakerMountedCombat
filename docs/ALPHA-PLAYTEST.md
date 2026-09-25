# Chunk 5 alpha playtest

This alpha distributes the exact qualified `0.1.0-chunk5-preview.105` payload. The version is retained so the release, the acceptance ledger and the 105 passing native scenarios identify the same bytes. Tested source: `471e1df92ee919c92bf42d0bc03ab6c2d2cc4114`. It includes everything from the Chunk 4 alpha. [Qualification, ledger and package hashes](CHUNK5-PERSISTENCE.md).

## Before mounting

In Kingmaker's Unity Mod Manager window, open **Kingmaker Mounted Combat** and check **Enable paired activation prototype (before mounting)**. Do this outside combat while dismounted, each time you launch the game. This build's developer configuration is session-only and starts with paired activation off. Leave unified mounted turn, paired command scheduler and diagnostic overlay off.

Use one supported Horse or Mammoth pair and mount before combat. Rider and mount share the rider's activation but retain separate native action budgets. Ordinary melee/ranged attacks, explicit Primary attacks, movement, Stop and early End are supported in the tested conditions.

## What is new in this alpha

**Mounted saves and loads.** A save written while mounted, in or out of combat, restores the same pair once on load with its owned controls, remaining actions and native cooldowns, without casting Mount or replaying preparation. Manual, quick and auto slots, queued and renamed archives, area transitions with the engine's own autosaves, and loading in a fresh game process are qualified. A save KMC cannot restore safely (future or malformed metadata, a different campaign, an incompatible configuration) is refused before the world is replaced and the current world is kept; a save with a missing actor or damaged combat state restores no pair.

**Prepare to disable / remove.** KMC's UMM panel has **Prepare to disable / remove KMC (dismount, then write a clean save)**. Outside combat, with no save or load in flight, it dismounts through KMC's own cleanup and writes a NEW save named `KMC_CLEANUP` through the engine, then verifies that archive: it must record no pair, no combat participation and no KMC control binding, and no member may reference a KMC-registered blueprint. `Ready` names the archive to load before disabling or removing the mod. `Refused` names why: a KMC Horse companion in the party or world (that campaign depends on the mod), combat, or an in-flight save or load. `Unconfirmed` means do not remove KMC on that result; prepare again.

## Focused playtest

1. Mount the pair, move and attack, and note remaining actions.
2. Make a real save while mounted, keep playing, then load it in the same session. Confirm the same rider and mount, the same remaining actions, no extra Mount cast, and usable play (move, attack, end turn).
3. Quit fully, launch a fresh game process and load that exact save. Verify the same actors and remainder, then play two paired activations.
4. Save again; repeat with a quicksave, an autosave (an area transition) and a copied or renamed archive.
5. In turn-based combat, save at different points of the pair's turn (after partial movement, after the rider has acted, with a Delay pending) and load each; confirm the turn resumes where it was with no replayed action or effect.
6. Press **Prepare to disable / remove KMC** while mounted; confirm the status names a new `KMC_CLEANUP` save and that it lists in the native load menu.
7. Quit fully, disable KMC in UMM or move the DLL out of `Mods`, launch a fresh process and load that `KMC_CLEANUP` save with no KMC DLL present; confirm the party opens dismounted with no missing-blueprint errors. Restore the DLL afterwards. (The automation performed this exact observation natively through a separate observer mod; this step is your own confirmation of it.)
8. With a party member holding the KMC Horse companion feature, press the button again and confirm it refuses with the permanent-reference reason.

## Known limits

Full mounted Charge is not implemented; safe rejection does not add the feature. Mid-combat mounting and cross-round Delay remain unsupported. One Phase 3H turn-based longbow full-round attack expectation remains an open known issue outside this alpha's qualified list. KMC never edits an existing archive, but this is an alpha: keep a copy of any campaign save you value before testing removal.

When reporting a problem, include the mode, mount, selected actor, target/weapon, whether the pair was mounted before combat, the exact save/load or removal sequence and expected/observed result. Visual, physical-input and human-play approval are separate from the native engineering passes.
