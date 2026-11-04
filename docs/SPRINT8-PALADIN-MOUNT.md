# Sprint 8: Paladin Divine Mount (PAL-01)

**IN PROGRESS.** This record is completed at the sprint's close.

## Rules and native contract

Rule source: [Core Rulebook, Paladin, Divine Bond](https://legacy.aonprd.com/corerulebook/classes/paladin.html). Native values are from the installed Kingmaker 2.1.7b assembly and the stock blueprints observed in play.

| Rule (Core Rulebook) | Native contract | Sprint 8 behaviour |
|---|---|---|
| At 5th level the paladin chooses one bond; the form cannot be changed | `PaladinProgression` level 5 grants `WeaponBondFeature` directly. `LevelUpHelper.AddFeatures` adds level-entry features with no prerequisite check | Level 5 holds a native `BlueprintFeatureSelection` offering Weapon Bond or Divine Mount. Reversible, exact slot replacement |
| Weapon: +1 at 5th, +1 per three levels, uses at 5, 9, 13, 17 | `WeaponBondPlus2` to `Plus6`, `WeaponBondAdditionalUse` in the class level entries | Unchanged on the weapon path. On the mount path these entries are not added, so nothing hidden remains |
| Archetypes may replace the bond | `RemoveFeatures` entries; `CanAddArchetype` refuses when a removal has no matching entry | An archetype removing level 5's Weapon Bond now removes the selection instead, so it is still legal and is offered no bond |
| Mount functions as a druid's companion at paladin level | `AddPet` reads `AnimalCompanionRank`; rank to pet level is native `RankToLevel`. Other classes stack ranks. Selections offer `AnimalCompanionEmptyCompanion` when a pet exists | Rank = paladin level: 5 ranks on choosing, then one per paladin level from a Paladin-only progression. Not the Ranger's reduced rate |
| Heavy horse for Medium, pony for Small; dog also suitable | Horse and Riding Dog are the supported additive mounts. No pony exists. A Medium rider cannot ride the Riding Dog | Medium: Horse. Small: Horse or Riding Dog |
| Intelligence at least 6 | Horse and Riding Dog base Intelligence is 2 | A fact on the current pet: +4 Intelligence, reaching 6 for an Intelligence 2 animal. See adaptations |
| Call mount: full-round, 1/day at 5th, +1 per 4 levels (4 at 17th), appears adjacent | `BlueprintAbilityResource`, `IncreaseResourceAmount`, `AbilityResourceLogic`, native `Translocate` | **Call Divine Mount**, a real spell-like full-round ability with those uses. Refused while mounted, or if the mount cannot act |
| 11th: celestial template, magical beast for spells | No native template. `AddDamageResistancePhysical`, `AddDamageResistanceEnergy`, ability and buff components exist | Pet facts: DR 5/evil and resist 10 at 11th, DR 10/evil and resist 15 at 13th (when the rank table reaches 11 HD); Celestial Smite 1/day. See adaptations |
| 15th: SR = paladin level + 11 | `UnitPartSpellResistance` | A pet fact holding SR = current paladin level + 11, refreshed when the paladin levels |
| Mount dies: no new mount for 30 days or a level, -1 attack and damage | Native companions are persistent and recover (`AllowDyingCondition`). There is no summon to refuse | Not modelled. See adaptations |

Declared CRPG adaptations:

- **Persistent companion.** The mount is an ordinary persistent native companion. It is not summoned and it does not vanish. Replacing it uses the native respec.
- **Death.** A fallen mount follows the native companion rule and recovers. The 30-day lockout and the -1 penalty are not modelled, because there is no summon for them to refuse.
- **Intelligence.** A flat +4 rather than a floor at 6, so an animal whose Intelligence was already raised keeps the raise.
- **Celestial template.** Damage reduction, energy resistance and smite are real. Darkvision has no native meaning for party units. The magical-beast reclassification is not applied, so spells that target animals still affect the mount. The template's own spell resistance (CR + 5) is not added; the Divine Bond's own SR at 15th is. The template table comes from the Bestiary as stated in the installed Call of the Wild template text, because the primary page could not be fetched.
- **Size.** No pony exists. A Small paladin chooses the Horse or the Riding Dog, and a Medium paladin does not see the Riding Dog because it could not ride it.
