# Phase 2 combat implementation report

Status: TODO

The user accepted the exact Phase 2A visual-review foundation, so the combat ordering gate is open. Version `0.1.0-phase2b-dev.1` now contains the first compile-clean scoped implementation; no runtime combat row is qualified yet.

The rider owns initiative and the only pair resource ledger. One rider Standard wrapper manually drives exactly one native ignored-cooldown `UnitAttack` child, using either the rider or exact active Mammoth as the rule initiator. `IsSingleAttack=true` prevents full, off-hand, additional-limb, or combined sequences. The Mammoth remains the sole pathfinder; exact turn-based movement is charged through the current rider `TurnController`, and an exact separate Mammoth turn is ended. The rider pathfinder remains disabled.

The transient overlay exposes distinct `Rider melee` and `Mammoth primary` choices. One exact-pair enemy click is consumed; ranged rider weapons, spells, invalid targets, unavailable rider Standards, wrong turns, unsupported profiles, and concurrent pair commands fail closed with feedback. Rider reach is scoped to Mammoth corpulence plus target corpulence plus planned melee-weapon range through the exact native private range method. Mammoth mode requires the exact first native additional limb selected by stock single-attack logic.

The same change introduces a guarded runtime-only target service using the stock generic Mammoth blueprint, a `HideAndDontSave` hostile faction, zero XP, disabled AI, native no-loot state, and exact destruction-controller drain, plus a rule probe for initiator/target/roll/damage counts. The fresh template lacks an initialized attack limb, so controlled provisioning uses native `UnitBody.AddAdditionalLimb` with only the qualified active Mammoth's exact natural-melee blueprint; that transient non-loot body item is destroyed with the target. These surfaces are inert unless a validated exact-Working runtime scenario invokes them. No save payload, blueprint, active mount, player inventory, party, quest, dialogue, or persistent fact is changed.

Offline evidence at this implementation checkpoint: source `21/0`, Release build PASS, component `175/0`, visual/source-order `17/0`, harness/protocol `152/0`, and exact assembly contracts `137/0` (`126/0` Kingmaker, `11/0` Wrath). Live real-time/turn-based evidence, action-cost deltas, lifecycle qualification, and non-mounted controls remain required before any combat PASS claim; the failed provisioning probes prove cleanup, but receive no combat qualification credit.
