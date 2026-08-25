# Horse phase mission

Status: IN PROGRESS

Date: 2026-08-25

## Authority and starting identity

This mission is the user-authorized successor to the accepted Mammoth private alpha. It begins from exact Phase 2 closure commit `ecb89500eb36eabbf889ccda7185843bd1e3e7c5` on `codex/mounted-combat-phase3-horse`. Phase 2 implementation commit `1241222459209aea1e6127bedd7d630df3940b99` and the human-tested package remain immutable evidence; horse work must not rewrite or relabel them.

The local policy boundary prevented authenticated pull-request creation and direct integration to `main`. The exact pending integration procedure is recorded in the Phase 2 closure records. The user expressly authorized the fallback: this horse branch starts from the exact published closure commit while that policy-bound merge remains pending.

## Product objective

Produce one private-alpha, Kingmaker-native horse path containing:

1. an exact native horse and summoned-pony asset audit;
2. an original KMC-owned horse animal-companion blueprint family;
3. additive Ranger animal-companion selection integration;
4. a qualified unmounted horse companion;
5. an independent `medium-humanoid-horse-v1` mounted presentation and control profile;
6. a nonserialized target-selected Mount surface while retaining the Phase 2 overlay fallback;
7. an evidence-backed Wrath mounted-command-model recommendation; and
8. Paladin Divine Steed design and contracts only.

The terminal status is:

```text
HORSE ALPHA COMPLETE  MANUAL VISUAL AND GAMEPLAY REVIEW REQUIRED
```

## Tranche sequence

### Tranche 0 — exact Wrath command and turn model

Use only the exact installed read-only Wrath assemblies. Wrath is not launched. Record initiative, turn membership, selection, action bar, camera, movement/action ledgers, paired attack routing, cancellation, AI boundaries, full attacks, and RT/TB transitions. Do not import source, assembly, asset, blueprint, or serialization types.

Gate: an evidence-backed recommendation must exist before any generic Kingmaker turn/action rewrite. The current conclusion is to retain the functioning Phase 2 separate-turn model through the horse alpha; see `planning/WOTR-MOUNTED-COMMAND-MODEL.md`.

### Tranche A — native horse and pony audit

Reverify `CR1_HorseRiding` (`9e9e75c484e68734487e609714565202`) from the exact installed Kingmaker files. Resolve the summoned pony independently. Compare prefab, mesh, skeleton, animator/controller, clips, authored stirrups, scale, movement agent, colliders, attacks, materials, death/reaction coverage, and blueprint ownership.

Gate: native Kingmaker ownership and exact resource identity are mandatory. No Wrath asset and no imported model are permitted.

### Tranche B — original horse companion

Create KMC-owned `AnimalCompanionUnitHorse`, `AnimalCompanionFeatureHorse`, and `AnimalCompanionUpgradeHorse` blueprints. Base lifecycle and progression on one exact, known-working Kingmaker animal-companion contract while using the native horse view. Add the feature to the existing Ranger companion selection without replacing any stock choice.

Gate: qualify creation, ownership, progression, selection, party movement, RT/TB natural attacks, death/recovery, save/load, respec, uninstall, and non-horse controls while unmounted. A horse that is not a sound unmounted companion cannot enter mounted qualification.

### Tranche C — independent horse-mounted profile

Create `medium-humanoid-horse-v1`. Mammoth transform, pose, and offset values are forbidden inputs. Use the exact horse Chest/stirrup geometry and runtime measurements. Qualify the full mounted matrix without changing `medium-humanoid-mammoth-v1`.

Gate: technical qualification plus one fresh human visual/gameplay checkpoint for the exact package.

### Tranche D — target-selected Mount

Implement a nonserialized interaction in which the player activates Mount and then selects an eligible creature. It must use exact eligibility, useful rejection feedback, no persistent fact/hotbar/save residue, and retain the overlay as fallback until separately accepted.

### Tranche E — Paladin design only

Map the exact Paladin level-5 Divine Bond selection and design horse progression, mutual exclusion, Intelligence minimum, call/dismiss, daily use, celestial progression, spell resistance, death/replacement, respec, save/load, uninstall, and mounted integration. Production implementation remains forbidden until the unmounted and mounted horse are technically qualified and visually accepted.

## Qualification economy and safety

- Continue using suite-scoped external-state admission, Working-only save authority, KMC-only Mods staging, audit-before-evidence, same-package A/B, and guarded publication.
- Observation-only asset instrumentation receives its direct scenario and targeted gates.
- A shared production subsystem change requires appropriate Mammoth regression. Horse-only presentation or blueprint changes do not justify unrelated multi-hour Mammoth reruns.
- Preserve all failed evidence and packages. Never replace Phase 2 artifacts.
- Do not launch or modify Wrath.
- Do not merge the horse branch to `main` before later human acceptance and do not publish a public release.

## Explicit exclusions

This mission does not authorize Paladin Divine Steed implementation, Small riders, another mount species, mounted ranged combat, mounted spellcasting rules, mounted feats, mounted AoO, mounted charge, enemy riders, persistent mounted state, automatic remount, public release, or horse-branch integration to `main`.

