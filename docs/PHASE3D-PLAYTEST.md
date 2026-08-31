# Phase 3D Unified Mounted Combat Private-Alpha Playtest

Status: IN PROGRESS — do not install until this document records a clean guarded-published package identity

## Exact artifact

- branch: `codex/mounted-combat-phase3d-unified-combat`;
- production implementation: `9bf83d21b5367e057130b41bf7379512a638c025`;
- presentation checkpoint: `63d324d909ec333cd16cde5e942f419759faa664`;
- runtime/package checkpoint: dev.1-dev.5 are immutable uncredited diagnostic history; dev.6 clean checkpoint pending;
- version: `0.1.0-phase3d-dev.6`;
- package, manifest, DLL SHA-256, and DLL MVID: pending clean-HEAD packaging and guarded runtime qualification.

Do not manually copy, replace, merge, or curate the live Kingmaker `Mods` directory. Installation and removal commands will be bound to the final exact package after the guarded runtime tranche passes.

## Focused manual checklist

1. Confirm UMM shows the exact Phase 3D version recorded above, no diagnostic overlay is visible by default, and existing hotbar slots are unchanged.
2. Mount outside combat through the native Mount Companion target cursor. Confirm the new saddle icon is legible and the Horse's small party portrait is a recognizable head/neck close-up.
3. Start TB combat while mounted. Confirm exactly one rider portrait appears in initiative, the rider remains selected and camera/action-bar principal, and no separate Horse turn-order portrait appears.
4. During that shared rider turn, use ordinary hostile right-click once with a melee weapon. Confirm the Horse approaches, the rider attacks, the Horse attacks when legal, each spends only its own action, and no duplicate attack occurs. Repeat with Rider Primary and Mount Primary to spend only one actor's action.
5. End the shared turn manually with one actor still able to act, then repeat after both actors have spent their relevant actions. Report any skipped unrelated combatant, duplicate immediate turn, or confusing active-turn feedback.
6. In TB combat, make one mounted five-foot step beside a hostile with a valid AoO. Confirm the Horse physically moves no more than the native step distance, no AoO occurs solely for the step, no ordinary Move is spent, and a second step or a step after ordinary movement is rejected. Confirm ordinary mounted movement can still provoke.
7. Mount during combat before either actor acts, after the rider has spent Standard, and after the Horse has spent Standard. Confirm Mount costs rider Move and preserves every spent action. Dismount in combat and confirm no immediate extra rider/Horse turn appears.
8. In RTWP, ordinary hostile right-click with a melee weapon should establish persistent pair intent: approach once, continue legal rider/Horse attacks through native cooldowns, stop on ground click/Stop/new target, and never fire a late canceled attack.
9. Equip a bow or other native ranged weapon and right-click a hostile outside current range. Confirm the Horse stops when the rider has legal range and line of sight, the rider continues native fire, and the Horse does not close unnecessarily into melee. Repeat cancellation and a target that moves enough to require bounded repath.
10. Check a crossbow/reload case and a sling case. Verify ammunition/reload behavior remains native. Compatible modded ranged weapons may work through native `UnitAttack`; no Gunslinger dependency or firearm-specific promise is made.
11. Exercise Rider Primary in RT and TB after movement, after an RT/TB transition, on a rejected target, and through target-selection cancel. Every non-lifecycle outcome must leave the exact mounted relationship active.
12. Inspect the final Horse seat at idle, walk, run, turn, stop, reverse, door interaction, RT/TB switch, Inventory, and Character screen. The pelvis should contact or nearly contact the saddle/back; report severe torso, neck, weapon, shield, leg, or Horse-body clipping. A little clipping is preferable to visible hovering.
13. Confirm explicit Dismount, save/area cleanup, Wild Shape cleanup, Horse creation/lifecycle/reload where applicable, and door approach/open/traverse remain coherent and never automatically remount.
14. Run one focused Mammoth Mount, movement, Primary, and Dismount check. Its profile, portrait, movement, and attack behavior must remain unchanged.

For failures, preserve the exact time, RT/TB mode, selected unit, equipped weapon, target, visible result, and relevant KMC log lines. Screenshots of the initiative tracker, saddle icon, Horse small portrait, and side/three-quarter mounted seat are especially useful.

## Known bounded limitations

- This is one rider/one owned Horse or the focused existing Mammoth profile, not a general mounted-combat system.
- Iterative/full-attack parity is not claimed beyond safe native command reuse.
- Positive cover/concealment behavior remains native; KMC does not rewrite those rules.
- Firearms and modded ranged weapons are compatible only when their provider exposes ordinary Kingmaker weapon/`UnitAttack`/ammunition/reload behavior.
- Mounted state remains runtime-only and intentionally nonserialized; save/load and area boundaries return unmounted.
- No automatic remount, mounted charge, mounted spellcasting, mounted feats, Small rider, enemy rider, Paladin Divine Steed, public release, or `main` merge is included.
