# Phase 3 horse mission draft

Status: `TODO` — draft only; not execution authority.

This proposal is tailored to the qualified Phase 2 Medium-humanoid/Mammoth architecture. It may be considered only after explicit human acceptance of the exact Round 2 manual-regression package and a separate user authorization. Until then, no horse work begins.

## Inherited evidence and invariants

The future horse work must preserve the proven separation of relationship state, game adapters, command routing, movement synchronization, view attachment, selection/UI, lifecycle cleanup, diagnostics, and guarded runtime testing. The mount remains the sole pathfinding authority while mounted; rider and mount attacks retain independent rule/resource ownership; state remains transient and nonserialized unless a later persistence decision explicitly changes that policy.

Mammoth offsets, anchor geometry, scale, pose tuning, collider assumptions, and doorway observations are Mammoth-profile evidence only. They are not defaults for a horse. Internal ownership telemetry is not human-usability proof, and stock path-object replacement telemetry is not itself a failure absent a correlated player-visible contract failure.

No tranche may import or redistribute Wrath code, models, animations, controllers, textures, or other proprietary content. Exact installed Wrath metadata may remain a read-only interoperability reference under the repository policy; production must use Kingmaker-native assets and original KMC code/data only.

## Admission gate

Before Tranche A, record all of the following:

- explicit human acceptance of the exact Round 2 package SHA-256, manifest SHA-256, DLL SHA-256, and MVID;
- separate user authorization for the horse phase;
- exact clean branch/HEAD/origin/upstream and a horse-specific mission branch decision;
- clean runtime/process/lock/sentinel/transaction/deployment state;
- superseding Phase 3 contract, risk, runtime-scenario, persistence, blueprint-ownership, uninstall, and manual-review records.

Failure of this gate is `BLOCKED — CRITICAL` for horse execution, not permission to reinterpret this draft.

## Horse Tranche A — exact native asset audit

Reverify the exact installed Kingmaker asset identified as `CR1_HorseRiding` and audit the summoned pony. Determine, with hashes and bounded local metadata inspection, whether the pony and horse share or differ in:

- mesh and material set;
- skeleton and bone hierarchy;
- animator/controller and animation clips;
- authored scale and view transforms;
- chest, saddle, stirrup, rider-reference, weapon, and effect transforms;
- colliders, corpulence, movement agent, avoidance, and selection geometry;
- locomotion, idle, walk, run, turn, stop, hit, death, and attack behavior;
- natural attacks, unit/view blueprints, summoning ownership, and lifecycle hooks.

Record exact type/member/asset inventories, dimensions, hashes, screenshots, and pseudocode-level findings only. Use Kingmaker-native assets. Import no Wrath model, animation, controller, code, or asset. Do not create a companion or mounted profile until this audit produces an evidence-backed seam and updates the relevant contract records.

## Horse Tranche B — horse animal companion

Create an original KMC-owned horse companion blueprint structure using the qualified native Kingmaker horse view. Base companion mechanics on one exact known-working Kingmaker animal companion while preserving original KMC blueprint ownership and provenance.

The tranche must separately specify and qualify:

- size, base statistics, skills, saves, speed, defenses, and progression;
- natural attacks and independently owned attack/action resources;
- companion ownership, recruitment, party membership, level synchronization, and Ranger selection integration;
- death, unconsciousness, resurrection, dismissal, removal, respec, save/load, and uninstall behavior;
- blueprint/fact/component identity, collision policy, and deterministic removal without orphaned save residue.

Add the horse to Ranger companion selection only within this separately authorized tranche. Qualify creation, progression, combat, lifecycle, respec, save/load, and uninstall while unmounted before allowing the unit into any mounted scenario. A horse that is not a sound unmounted companion cannot advance.

## Horse Tranche C — horse-mounted profile

Create an independent profile named `medium-humanoid-horse-v1`. Do not reuse Mammoth offsets, scale values, pose constants, collider assumptions, or attachment geometry. Use the native horse chest/stirrup geometry and Tranche A rig evidence to derive anchors and offsets.

Qualify at minimum:

- idle, walk, run, turns, stop, acceleration, and animation transitions;
- rider attachment, feet/stirrups, hands/reins where available, weapon pose, scale, rotation, and restoration;
- colliders, selection circles, portrait/action-bar/camera ownership, group movement, and formation behavior;
- doorway approach/open/traverse and narrow-space behavior;
- separate RT and TB movement, rider-turn routing, supported rider melee, and horse primary natural attack;
- mounted ranged rejection and unchanged unmounted controls;
- save/load/area/mode/view/death/incapacitation/party-removal/mod-disable/exception/uninstall cleanup;
- zero duplicate command, movement, turn, attack, roll, damage, interaction, opportunity, or resource chain.

Require a clean package, guarded runtime qualification, exact external restoration, and one explicit human visual review for the horse profile. Automation may prove structure and measurements but not subjective clipping, physical pointer feel, ordinary mouse usability, or rendered presentation acceptance.

## Horse Tranche D — player-facing Mount action

Evaluate a transient, nonserialized action-bar or toolbar `Mount` action with the intended flow: activate `Mount`, then click one eligible mount. Eligibility, cancellation, feedback, selection, target admission, range/approach policy, and one-shot consumption must be contract-first and independently testable.

Preserve the current overlay as a safe fallback until the native-feeling surface passes deterministic and human usability review. Do not add a persistent hotbar slot, blueprint, fact, feature, save field, or automatic remount residue without a separate persistence decision and uninstall proof. A UI handler may orchestrate only; relationship transitions and target decisions remain in services/domain code.

## Horse Tranche E — Paladin Divine Steed

Begin only after the unmounted horse companion and `medium-humanoid-horse-v1` profile are qualified and accepted. Add a Paladin level-5 Divine Bond choice that is mutually exclusive with weapon bond and uses full Paladin-level companion progression.

Define the Intelligence minimum and later Divine Steed progression from exact Kingmaker rules/contracts. Stage these concerns separately rather than combining them into the first checkpoint:

- choice acquisition and weapon-bond exclusivity;
- call/dismiss behavior and ownership;
- companion scaling and full Paladin-level progression;
- Intelligence minimum and later celestial progression;
- spell resistance;
- death, resurrection, respec, save/load, and uninstall behavior;
- mounted-profile integration and private qualification.

No tranche claims full tabletop or Wrath parity. The first horse implementation checkpoint must not bundle every Divine Steed feature.

## Explicit exclusions

This draft does not authorize Small riders, persistent mounted state, automatic remount, mounted ranged combat, mounted spellcasting rules, mounted feats, explicit mounted attacks of opportunity, mounted charge, enemy/AI riders, public release, or merge to `main`.

## Proposed tranche exits

Each tranche exits with a coherent reviewable commit, updated contract/risk/runtime records, complete applicable offline gates, a clean guarded package where runtime work applies, immutable evidence, exact restoration, and a truthful `PASS`, `FAIL`, `DEFER — EVIDENCED`, or `BLOCKED — CRITICAL` disposition. Advancing to the next tranche requires the prior exit to pass and any stated human review to be explicitly accepted.
