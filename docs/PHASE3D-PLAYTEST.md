# Phase 3D Unified Mounted Combat Private-Alpha Playtest

Status: BLOCKED — CRITICAL - no manual-review package was issued

## Exact artifact

- branch: `codex/mounted-combat-phase3d-unified-combat`;
- production implementation: `9bf83d21b5367e057130b41bf7379512a638c025`;
- presentation checkpoint: `63d324d909ec333cd16cde5e942f419759faa664`;
- runtime/package checkpoint: dev.1-dev.20 are immutable diagnostic history. Clean dev.17 passed all 29 reached mounted/transition RT rows twice; its final automated unmounted Sling control is `DEFER — EVIDENCED` after a native prior-command continuation contaminated two distinct targets before input. Dev.19 established exact TB adjacency, actionable rider turn, untouched ledgers, and enabled Mount, but its bounded harness wait stopped before input on a dormant Horse AI attack command. Dev.20 presentation passes all four evidence rows and `49/0` assertions. Dev.21 used the rider principal while retaining Mammoth action ownership, crossed real input admission, and then failed `49/1`: the exact Mammoth-owned Standard-slot command remained unstarted for 30 seconds because the Mammoth was not Kingmaker's current turn unit. This is the controlling architecture blocker, not a manual gate;
- version: `0.1.0-phase3d-dev.21`;
- diagnostic package: `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3d-dev.21-unified-combat-alpha-diagnostic.zip`;
- package, manifest, DLL SHA-256, and DLL MVID: `067c9b49b8b7767e808a011315769fe996b642c068b9972f300160a1d8a61d26` / `5c1360a6368ffe385cee842b918149b46747180d03866960e77562dd63a9d605` / `198dfe3cf8b187b6803eb6218e2cca68b2cf71530e4cb6d546e1423fe5e9f6c9` / `fb0bbedb-f63a-417c-9cea-f6ee71d3ed18`;
- qualification disposition: the diagnostic package is not approved for manual playtest. Fresh TB evidence accepted a Mammoth-owned Standard attack on the rider-owned shared turn, but Kingmaker never started it because only the current turn actor's commands advance. Use of the existing fallback or a new scheduler design requires a separate decision.

Do not install this diagnostic package for ordinary play or manually copy, replace, merge, or curate the live Kingmaker `Mods` directory. No final manual-review package exists.

## Deferred focused manual checklist

This checklist is retained as the intended acceptance surface, but it is not authorized for execution against dev.21. It becomes applicable only after a separately authorized architecture disposition produces a qualifying package.

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
