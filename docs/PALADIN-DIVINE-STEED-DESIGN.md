# Paladin Divine Steed design

Status: DESIGN ONLY — implementation forbidden before horse technical and human acceptance

## Dependency gate

Production may begin only after:

1. the KMC horse companion qualifies unmounted;
2. `medium-humanoid-horse-v1` qualifies technically; and
3. the exact horse package passes human visual/gameplay review.

Until then this file is a contract proposal, not execution authority.

## Exact Kingmaker contracts still to bind

The bounded blueprint audit must identify the Paladin class/progression, level-5 Divine Bond selection, weapon-bond option, selection prerequisites/mutual-exclusion mechanism, resource/daily-use facts, and any existing celestial-companion mechanics. No GUID or level insertion point will be guessed.

## Proposed structure

- append one KMC Divine Steed option to the exact level-5 Divine Bond choice;
- preserve mutual exclusivity with weapon bond through the stock selection contract;
- reuse the qualified KMC horse unit/view/profile, but create Paladin-owned feature/progression facts rather than modifying the Ranger feature;
- drive companion level from full Paladin level, not the Ranger animal-companion rank table;
- apply the rules-required Intelligence minimum through an explicit KMC upgrade fact;
- stage call/dismiss and daily uses independently from permanent companion creation;
- stage celestial progression and spell resistance at evidence-backed levels;
- define death/replacement, owner death, respec, save/load, uninstall, and mounted cleanup before implementation.

## Staged checkpoints

1. selection/mutual-exclusion and full-level ownership;
2. bounded call/dismiss with daily resource accounting;
3. Intelligence and celestial progression;
4. spell resistance;
5. death/replacement, respec, save/load, and uninstall;
6. mounted integration using the already accepted horse profile.

Do not combine every feature into the first checkpoint. Each checkpoint requires its own source/build/component and applicable runtime evidence. No checkpoint may alter Ranger horse progression or the Mammoth contract.

## Save and uninstall policy

Like the Ranger horse, a Paladin steed feature would introduce KMC blueprint references into saves. The design must require respec away from Divine Steed before uninstalling unless later evidence establishes a safe native replacement transaction. The mounted relationship itself remains nonserialized and clears across existing save/area boundaries.

