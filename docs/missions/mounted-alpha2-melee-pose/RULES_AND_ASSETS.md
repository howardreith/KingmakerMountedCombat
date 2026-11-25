# Horse attack / trip / animation research for the next mission

Research only; no implementation, native launch, UI operation, or repository mutation.

## Baseline verified

GitHub currently reports PR #61 open on `codex/mounted-shield`, head `122a9f94680d1fbf512a0f9ec7386a1447d0c1f7`. `main` remains `fb743911200f150af407198c6c28519e522ef58c`. The mission should use the owner-playtested alpha lineage explicitly, rather than accidentally starting from main without Mounted Shield. Repository links below pin the PR head; this is a source baseline, not a claim about which binary the owner's running process loaded.

## Rules conclusion

**An ordinary horse companion has no intrinsic bite-triggered Trip. Wolves are not the only legitimate users: riding dogs also have it.**

The Core Rulebook horse-companion entry supplies a bite and two hooves, and its listed advancement adds ability scores and combat training. It does not list the trip special attack. The wolf-companion entry explicitly includes trip with its bite. The Bestiary riding dog likewise has a bite with trip. Do not remove native trip from wolves, stock dogs that have it, or KMC Riding Dogs to fix a horse-only problem.

- [Horse companion, Core Rulebook p.54](https://www.aonprd.com/DruidCompanions.aspx?ItemName=Horse)
- [Wolf companion, Core Rulebook p.54](https://www.aonprd.com/DruidCompanions.aspx?ItemName=Wolf)
- [Riding Dog, Bestiary p.87](https://www.aonprd.com/MonsterDisplay.aspx?ItemName=Riding%20Dog)

The universal Trip special attack is a hit-triggered opportunity to attempt a maneuver with its specified natural attack. It is distinct from choosing the ordinary trip combat maneuver instead of a melee attack. The mission should remove an unintended intrinsic trigger, if demonstrated, while preserving legitimate explicit maneuvers and independently granted feat/item/buff abilities. It should not assert that a horse can never participate in any Trip rule, nor add a new maneuver button as part of this repair.

- [Universal monster rule: Trip](https://legacy.aonprd.com/bestiary/universalMonsterRules.html#trip)
- [Core combat: Trip maneuver](https://legacy.aonprd.com/coreRulebook/combat.html#trip)

The screenshot is evidence that a Trip appeared in the owner's combat flow. It is not enough to identify the source component, distinguish a special trigger from a selected maneuver, or prove the unit's blueprint from its display name. Begin the mission by binding the log event to the exact attacker, target, command, weapon, and originating fact/component.

## Actual horse construction: donor and shared-reference surfaces

Source: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs`.

| Identity | Value / source |
| --- | --- |
| KMC horse unit | `4016c7db400ab721ff125aef9e65e202` |
| KMC horse selection feature | `7db7c50677e39f09feef56f3831fc723` |
| KMC horse advancement | `98e651899e6278d938de77af1d69bd32` |
| Native view donor | `CR1_HorseRiding`, `9e9e75c484e68734487e609714565202` |
| Companion-mechanics donor | `AnimalCompanionUnitMammoth`, `e7aa96d15a45238438ae4cfb476f6bb9` |
| Mammoth feature / upgrade | `6adc3aab7cde56b40aa189a797254271` / `6a23d16a4476af644af89d91f9f96790` |
| Dog feature, also used for companion-contract parity and Riding Dog construction | `f894e003d31461f48a02f5caec4e3359` |
| Horse bite | Shared unique blueprint named `Bite1d4`; historical exact binding `35dfad6517f401145af54111be04d6cf` |
| Horse hooves | Shared `Hoof1d4`, `b0e472a49ff2a294f93faa3ab757a4a5` |

Important distinctions from the proposed “wolf bite clone” hypothesis:

1. `Register` resolves the shared stock `Bite1d4` by unique name (`:544–545`). It does **not** clone a wolf weapon or create a KMC-owned bite blueprint. The historical GUID above comes from the checked-in animation contract; reverify the resolved weapon and its live components/enchantments on the owner's installation.
2. A new horse unit is constructed (`:602–641`). Its view, visuals, customization, and body equipment defaults come from the native riding horse. Its faction/brain/type and companion base are taken from the Mammoth.
3. The horse copies Mammoth `AdditionalTemplates` and `AddFacts` arrays without a trip-specific exclusion (`:635–636`). `CopyAddClassLevels` also copies the donor's archetypes and selection arrays (`:1080–1095`). These are provenance surfaces to inspect, not proof that any currently contains Trip.
4. The new unit's explicit components are the copied class-level setup and `AllowDyingCondition`. The horse feature gets its own AddPet and no-companion prerequisite. Its advancement explicitly has only Strength/Constitution bonuses. `CopyFeatureContract` copies presentation/classification fields, not donor feature components (`:1107–1122`).
5. `CopyBody` disables hand fallbacks and supplies ordered natural limbs `[bite, hoof, hoof]` (`:1025–1047`). No automatic-trip exclusion is present there. Existing registration assertions verify limb identity/count, not the complete attack-trigger provenance (`Diagnostics/HorseCompanionBlueprintRegistrationAuditService.cs:145–156`).
6. KMC Riding Dog separately preserves the stock dog's body and facts; its only shown fact exclusion is size-changing facts (`:767–783`). Its description expressly promises biting/tripping. Do not broaden a horse fix into dog behavior.

**Investigation target:** inspect the resolved bite's components/enchantments, Mammoth-derived facts/templates/class selections, the live horse's feats/buffs/equipment, and the originating combat-maneuver rule context. The source does not establish donor contamination as the cause. A fix must not mutate the shared stock bite or global trip behavior and thereby change other animals.

[Pinned horse construction source](https://github.com/howardreith/KingmakerMountedCombat/blob/122a9f94680d1fbf512a0f9ec7386a1447d0c1f7/src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs)

## Bite animation: existing evidence and current route

There is already an implementation and historical Kingmaker-native evidence to investigate. Do not begin by assuming that a bite asset is absent or importing a Wrath asset.

- Native horse prefab resource: `5e0b93738ad54dd4ba101b3513ac4590`, path `Assets/Mechanics/Bundles/Prefabs/Characters/Creatures/Medium/Horse/HorseRiding.prefab`, rig `HorseRiding_body_RIG`.
- `planning/HORSE-PONY-ASSET-AUDIT.md` records native horse special attacks and file-level bite/hoof/gore coverage.
- `planning/ASSET-RIG-ANIMATION-INVENTORY.md` records `ArmoredHorseAnimationSet_LocoMotion` and native attack coverage. These are historical installed-asset observations, not a fresh inventory of the owner's process.
- `planning/HORSE-MOVEMENT-AND-ANIMATION-CONTRACT.md:30–44` records `HorseAnimationSet_Bite` / `SpecialAttack`, adoption of a stock-created handle, and historical successful RT/TB command/roll/damage chains. It explicitly leaves visual readability to human observation.

Current `HorsePrimaryAttackAnimationAdapter.SupplyExact` preserves and validates a stock handle, or supplies a narrow native SpecialAttack fallback when no handle exists. It requires the exact KMC Horse, current mounted relationship, `MountPrimaryNatural` command, exact planned attack/weapon, and horse animation manager/action-set membership. It records handle creation/adoption/refresh/rejection; it is not a replacement damage executor.

Ordinary mounted attacks are not categorically bypassing this adapter: the normal mount dispatch uses `MountPrimaryNatural`; `MountedPairAttackCommand.CreateAndValidateChildAttack` calls it, and `MountedPatchController.AttackAnimationPostfix` feeds later native handle creation through `MountedAnimationAdapter`. Inspect which actual planned limb/action/variant/clip runs in the failing ordinary sequence and whether its handle plays, has weight, acts, finishes, or is interrupted. Do not diagnose a disconnected path merely because the adapter predates ordinary attacks.

The existing `HorseNativeAssetAuditService` already enumerates animation manager actions and animator-controller clip names (`:431–485`). Reuse or narrowly extend that evidence path if fresh inspection is needed. Record identifiers and observations only; do not package game model/clip/controller payloads.

- [Animation adapter](https://github.com/howardreith/KingmakerMountedCombat/blob/122a9f94680d1fbf512a0f9ec7386a1447d0c1f7/src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs)
- [Historical movement/animation contract](https://github.com/howardreith/KingmakerMountedCombat/blob/122a9f94680d1fbf512a0f9ec7386a1447d0c1f7/planning/HORSE-MOVEMENT-AND-ANIMATION-CONTRACT.md)
- [Native asset audit](https://github.com/howardreith/KingmakerMountedCombat/blob/122a9f94680d1fbf512a0f9ec7386a1447d0c1f7/src/KingmakerMountedCombat/Diagnostics/HorseNativeAssetAuditService.cs)

The mission can require a readable native horse bite presentation where the installed Kingmaker action/clip supports it. It should require evidence and a precise limitation if the available asset cannot meet that visual goal. A named action alone does not prove the desired appearance; neither a cosmetic substitute nor an extra animation event may create another attack, damage packet, delay, or resource spend.

## Suggested bounded acceptance checks

1. Confirm the reported Trip's exact unit/weapon/source. Reproduce with a plain KMC Horse and no independently granting trip ability, in the owner's reported mounted melee flow. Compare unmounted behavior if needed to isolate the source.
2. After correction, an ordinary successful horse bite does not trigger intrinsic Trip. Preserve the intended bite/hoof attack sequence and native hit/miss/damage ownership; do not use “no Trip” as a substitute for confirming the horse actually attacked.
3. A wolf and a stock/KMC dog that legitimately has bite-triggered trip retain it. Any legitimate separately invoked or granted maneuver remains available; no global horse-type Trip veto.
4. Existing owner horse saves remain usable. If the defect is a persisted inherited fact, address that exact origin without deleting learned feats, independent buffs, or unrelated companions' facts.
5. Observe ordinary mounted horse attacks at a normal play camera: the intended native bite handle and visible motion correspond to the real attack, the rider/horse face the target, and the handle ends without duplicate actions/damage or a stuck animation. Preserve hoof sequences and non-horse animation behavior.
6. Preserve the owner-observed Mounted Shield behavior as a regression check: equipment-dependent mount AC and the single Mounted Combat check contribution. The new mission does not reopen the feat design or promote the limited screenshots into complete release acceptance.

Keep broader combat, animation, and lifecycle expansion outside this issue unless the reproduced defect demonstrates a direct dependency. No new full validation campaign is implied by these research notes.
