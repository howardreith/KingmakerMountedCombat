# Horse phase mission

Status: PASS (technical) - manual visual/gameplay review required

Date: 2026-08-25

## Final technical disposition - 2026-08-27

The authorized horse-alpha implementation is complete at exact implementation commit `04a86870322f136bc3d7423b2e0ef31cf06d4145`, version `0.1.0-phase3b-dev.17`. Immutable package SHA-256 is `5d61b8febad67637954ad52f7e0bf8f6081fc2ffa87266407721fc00b4d5585e`; its manifest/DLL SHA-256 are `75bb23b5289cce77799f8001f6966346cd324ac80c787fe1bcf49ea4e0963ced` / `505c5c983ad94bbfc7e287284743427bc331d90fd6d4f9d6aacc16fe653e6875`, with MVID `4d9fff51-a040-41d4-b642-6f433c7a4b6a`.

Credited audited runtime evidence is horse aggregate `20260827T014000Z-horse-mounted-dev17-passF`, whose immutable game records pass registration `13/0` and horse behavior `38/0` (`51/0` total), plus targeted Mammoth regression `20260827T030300Z-mammoth-primary-dev17-passA`, `62/0`. The exact package is installed locally through the guarded deployment helper. Technical completion does not claim ordinary pointer feel, visual seat/gait quality, actual disk save/reload, public-release readiness, stock mounted right-click/auto-attack, unified turns, mounted ranged combat, or Paladin implementation. Those boundaries remain explicit in the playtest and implementation reports.

Tranches 0 through D are technically complete within the private-alpha scope. Tranche E remains design only. The next authorized action is human visual/gameplay review of the exact installed package; the horse branch must not merge to `main` before acceptance.

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
