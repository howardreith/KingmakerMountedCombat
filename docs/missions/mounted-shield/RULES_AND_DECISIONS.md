# Mounted Shield — rules and implementation decisions

This document explains the decisions adopted by MISSION.md. It is a specification, not evidence that the feature has been implemented or tested.

## Outcome and scope

Implement **Mounted Shield** as one passive combat feat belonging to the rider. It requires **Mounted Combat and Shield Focus**. While the character is actually riding a valid mount and using an eligible physical shield, provide the eligible shield amount to the current mount's AC and to the rider's existing Mounted Combat hit-negation Mobility check. Use the supplied feat-selection icon. No second icon, action-bar ability, activation, toggle, resource, or new configuration option is required.

The feat's published rules explicitly include Shield Focus and exclude enhancement bonuses. The mod substitutes Mobility for the tabletop Ride check. Preserve the existing Mounted Combat trigger, eligible attack types, comparison, attempt consumption, and combat-mode timing. This mission adds a modifier; it does not redesign the reaction system. [Mounted Shield, Advanced Player's Guide p. 165](https://aonprd.com/FeatDisplay.aspx?ItemName=Mounted%20Shield); [user-supplied rules](https://www.d20pfsrd.com/feats/combat-feats/mounted-shield-combat/).

## Eligible shield amount

Use the rider's **actively equipped physical shield**, including the active equipment set and the engine's existing shield-availability rules. A shield merely carried in inventory does not qualify. A generic shield-typed buff, the Shield spell, or a shield weapon's attack/damage enhancement is not the physical shield's base defense.

For this mission, the amount is the shield's non-enhancement base AC contribution plus Shield Focus's +1 and, when possessed, Greater Shield Focus's +1. Count each contribution once. Avoid taking a precomputed total that already includes a focus bonus and then adding that bonus again. Ordinary buckler/light/heavy/tower base values are +1/+1/+2/+4; read the game's native shield data rather than duplicating an item-name table. [Equipment](https://legacy.aonprd.com/coreRuleBook/equipment.html); [Shield Focus](https://aonprd.com/FeatDisplay.aspx?ItemName=Shield%20Focus).

**Greater Shield Focus is an explicit mission interpretation.** Its text increases a shield's AC bonus and stacks with Shield Focus. Include that additional non-enhancement +1 in both Mounted Shield benefits. The shorter Mounted Shield description does not separately name this feat; do not claim an official FAQ resolved the interaction. [Greater Shield Focus](https://aonprd.com/FeatDisplay.aspx?ItemName=Greater%20Shield%20Focus).

With Shield Focus alone, buckler/light/heavy/tower results are +2/+2/+3/+5. A heavy shield +5 still supplies +3; a heavy shield with both focus feats supplies +4. An inactive shield supplies zero, including the focus contributions. Item or spell enhancement must never enter this calculation. Do not transfer item powers, cover, defenses against particular attacks, armor check penalties, or unrelated temporary AC modifiers.

Honor native shield suppression and restoration. If the game currently removes a shield's protection during a bash or another equipment state, do not keep this feat's benefits alive by checking only whether an item occupies the slot. If native Improved Shield Bash retains shield protection, the shield remains eligible. Reuse existing behavior; do not build a new tabletop buckler, shield-bash, or tower-cover subsystem. [Improved Shield Bash](https://aonprd.com/FeatDisplay.aspx?ItemName=Improved%20Shield%20Bash).

## AC and Mobility are separate applications

Apply the mount's amount as a **shield bonus**, preserving the game's normal descriptor behavior. It competes with another shield bonus; it does not stack as untyped AC. A mount with a +4 shield effect and a rider supplying +3 retains the higher +4 shield contribution. Armor, natural armor, deflection, and other ordinary independent sources continue to behave normally. [Shield-bonus stacking](https://legacy.aonprd.com/coreRuleBook/equipment.html).

The Mobility addition is the eligible amount, **not the mount's net AC improvement**. In that +4-versus-+3 example, the rider still gets +3 on the particular Mounted Combat check. Do not derive it by comparing the mount's before/after AC or by asking whether the AC modifier won a stacking comparison.

Normal touch attacks ignore the transferred shield AC; flat-footed AC retains it. Let the native modifier system handle this distinction. A touch attack's disregard for shield AC does not itself remove the separate check addition if the existing Mounted Combat implementation permits that hit-negation attempt. This follows from the feat's separately stated benefits. [Combat: touch and flat-footed AC](https://legacy.aonprd.com/coreRulebook/combat.html).

Apply the check addition only within the existing Mounted Combat hit-negation path, only for the owning rider and current mount. Do not alter the displayed general Mobility stat, unrelated Mobility rolls, other mounted checks, or another rider's checks. Existing armor check penalties remain part of the underlying roll. Preserve strict success: the final check must **exceed** the incoming attack result; equality still fails. An attack that already misses after AC calculation is already a miss and consumes no hit-negation attempt. [Mounted Combat](https://aonprd.com/FeatDisplay.aspx?ItemName=Mounted%20Combat).

## Lifecycle and presentation

Resolve benefits from current authoritative riding and equipment state. Removing or changing a shield, changing active equipment sets, dismounting, changing mounts, invalidating the rider/mount relationship, losing the feat, respecializing, or disabling the feature must remove or recalculate only this feature's contribution. Do not remove another source's shield bonus. Save/load, area transitions, and repeated reconciliation must restore one correct contribution without accumulation or orphaned state.

Register the feat consistently with existing combat feats and eligible selection lists, with stable identity and prerequisites. Present accurate localized name and description, the provided icon, and native AC/check breakdown attribution where supported. The text should communicate both benefits and enhancement exclusion. The icon must survive normal build/package/install paths and selection UI use; a missing or undecodable new icon must produce a clear diagnostic and isolated withdrawal of the new feat, without taking down the mod or silently substituting another image.

The optional Combat Stamina trick on the reference page is outside scope. Do not add its enhancement-sharing behavior, a stamina resource, or changes to Mounted Combat's established timing. New general shield rules, mount control repairs, and unrelated feat selection rewrites are also outside this feature.

## Suggested player-facing text

**Name:** Mounted Shield

**Description:** While you are mounted and using a shield, your mount gains a shield bonus to AC equal to your shield's base bonus, including Shield Focus and Greater Shield Focus but excluding enhancement bonuses. You also add this amount to Mobility checks made by Mounted Combat to negate hits against your mount. This benefit is passive.

Use the actual native prerequisite display for Mounted Combat and Shield Focus. Match current localization/presentation conventions. The description reflects this mission's explicit Greater Shield Focus interpretation.

The complete evidence matrix is in `ACCEPTANCE.md`; all execution rows start NOT_RUN.
