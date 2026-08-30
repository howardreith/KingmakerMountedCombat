# Paladin Divine Steed design

Dependency update (2026-08-30T22:31:43Z): Phase 3D implements a rider-led shared initiative/turn model with rider portrait/selection principal, separate rider and mount action ledgers, combat Mount/Dismount reconciliation, ordinary stock attack intent, and native ranged routing. These are offline-green and awaiting fresh guarded runtime plus human acceptance. A future Divine Steed must reuse the accepted shared-turn coordinator and independent-ledger contract rather than create a Paladin-specific turn pool. This is a design dependency only: Paladin implementation remains explicitly unauthorized.

Dependency update (2026-08-30T14:05:47Z): Phase 3C now technically qualifies the Ranger Horse's native Mount/Dismount and Rider/Horse primary ability surfaces, actual RT/TB selected-ability target path, original Horse portrait set, stock-created Bite animation handle, bounded solo movement, candidate-C pose stability, controlled DollRoom IK observation, and same-package Mammoth regression. Exact final package version is `0.1.0-phase3c-dev.13`. Physical input feel, portrait/presentation quality, visible animation, real Inventory/Character-screen IK, and ordinary gameplay flow remain human-gated. Paladin Divine Steed remains design-only and is not implemented by this checkpoint.

Dependency update (2026-08-28T16:28:49Z): the real Ranger-created KMC Horse, native companion ownership, unmounted/mounted control path, independent `medium-humanoid-horse-v1` profile, ordinary stock damage-to-life-state transition, same-Horse recovery, and bounded removal cleanup are now technically qualified on dev.27. Human dev.23 evidence independently confirms ordinary Horse creation, visibility, selection, control, Mount, and the native Horse/tack silhouette. Candidate B repairs the measured high/wide pose and is technically stable, but final side/three-quarter visual acceptance plus real save/reload, area, rest, and UI respec away/back acceptance remain open. Paladin Divine Steed therefore remains design-only; no implementation authority is created by this update.

Status: DESIGN ONLY — implementation forbidden before horse technical and human acceptance

Dependency update (2026-08-27T03:40:27Z): the unmounted horse companion and `medium-humanoid-horse-v1` have now passed bounded technical qualification on exact dev.17. The third gate - human visual/gameplay acceptance of that exact package - remains open. This document remains design only and grants no Paladin implementation authority.

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
