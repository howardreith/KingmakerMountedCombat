# Horse native controls and presentation implementation

Status: `PASS (technical) - MANUAL REVIEW REQUIRED`

Date: 2026-08-30

## Exact handoff identity

| Field | Exact value |
|---|---|
| Branch | `codex/mounted-combat-phase3c-native-controls` |
| Accepted Horse stabilization input | `8ff5813b36eb1af04e1329a1993b2476ae6ad691` |
| Final production implementation | `e951fb5394ff4f8e791dd27f49b75d71d76a8b1f` |
| Final package/diagnostic checkpoint | `42debbb814823dbdcd3a39cdc4353a5c3ee3d12d` |
| Version | `0.1.0-phase3c-dev.13` |
| Package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3c-dev.13-native-controls-ux-final-diagnostic.zip` |
| Package SHA-256 | `5f23757e17c51a2fe67374da6d218cb9efc1c4fb49ae282060d02577bf7a9fa3` |
| Manifest SHA-256 | `82bd78046ec3d393fd45f7af250bae3c2b8194da6e379648b7b6a2c26e9c3a1b` |
| DLL SHA-256 | `6fce17eee8b8f5d8b6b987dd7408ab6ddb9a177ad0236429c41e40ffcc3c282e` |
| DLL MVID | `9f3344f0-4314-4b2e-ac15-08729c3727d4` |
| Stable suite | `20260830T113500Z-phase3c-dev13-native-controls-suite11` |
| Suite snapshot SHA-256 | `675b7868a3c0565d2139764a41782a971fd5e90fbf0059cffc5c13d8c06123be` |

The package contains only `KingmakerMountedCombat/Info.json` (335 bytes) and `KingmakerMountedCombat/KingmakerMountedCombat.dll` (3,848,704 bytes). It contains no extracted Kingmaker or Wrath payload.

`e951fb5` is the final production behavior change: it follows and validates the refreshed stock Horse Bite animation handle. `42debbb` changes only the Mammoth TB diagnostic selection and the dev.13 build identity. Source and harness regression prove that it does not change production relationships, commands, movement, attacks, UI, animation, action economy, save behavior, or cleanup.

## Native ability surface

Four original KMC runtime abilities use Kingmaker's ordinary abilities drawer and selected-ability cursor:

| Ability | GUID | Owner and behavior |
|---|---|---|
| `KMC_MountCompanionAbility` | `f053faad986631688defa003cd7bda0e` | Eligible unmounted rider activates it, then clicks an exact owned supported Horse or Mammoth. |
| `KMC_DismountAbility` | `3af2b81f4d72bbb30501fa730fcdf36e` | Mounted rider invokes the existing exact cleanup path. |
| `KMC_RiderPrimaryAbility` | `27364df661b3c121eabb97a31aa73a83` | Rider owns actor, command, weapon, and Standard; mount supplies approach/pathfinding only. |
| `KMC_MountPrimaryAbility` | `f88a50d6fdbebbd709c3e323d2f52f5e` | Exact Horse or Mammoth owns actor, command, natural weapon, and Standard. |

The native ability shell is Free because the established KMC attack wrapper remains the one Standard-action owner. Availability follows the exact RT/TB actor, current turn, Standard availability, target validity, weapon support, lifecycle state, and command-idle boundary. Wrong-turn and invalid-state requests expose precise availability/refusal feedback instead of silently doing nothing.

The controls are runtime facts. They are removed during the complete save coroutine and restored afterward, removed on disable/respec/pet replacement, and never serialized as mounted state. `ActionBarAutoFillIgnored=true`: KMC writes no user hotbar slot, overwrites no occupied slot, and creates no duplicate binding. The player may drag an ability from the native abilities drawer to an empty slot.

The old IMGUI control overlay is hidden by default. UMM option `Show diagnostic mounted-control overlay` retains it as an explicit emergency/diagnostic fallback.

## Qualified runtime results

Horse native-controls aggregate `20260830T074300Z-phase3c-dev12-native-controls-passG` passed `66/0` (`14/0` registration plus `52/0` Horse UX) on final production commit `e951fb5`. It exercised the actual Kingmaker selected-ability handler, target cursor/click, native cast request, KMC command acceptance, and terminal command path. Dev.13 did not repeat this unaffected aggregate because its only behavioral diff is in the Mammoth diagnostic engine.

The Horse aggregate proves:

- invalid Mount targeting is refused; exact Horse targeting starts and completes Mount;
- native Dismount completes exact cleanup;
- control facts are `2 -> 0 -> 2` across the guarded save scope, with zero duplicates or managed hotbar slots;
- TB Rider Primary succeeds on the rider turn with rider actor/command/resource ownership and one child;
- TB Horse Primary succeeds on the Horse turn with Horse actor/command/resource ownership, exact Bite `35dfad6517f401145af54111be04d6cf`, one child, and Horse-only Standard cost;
- RT Rider and Horse primaries each succeed through the native target-click path;
- each credited attack has one attack/roll/damage chain, zero unexpected pair attacks, and zero repaths;
- the stock-created `HorseAnimationSet_Bite` `SpecialAttack` handle is exact, acted, finished, not interrupted, adopted once in TB and again in RT, with zero rejection;
- final relationship is Unmounted, temporary targets are removed, and unrelated pets are preserved.

Final same-package Mammoth regression `20260830T122900Z-phase3c-dev13-mammoth-tb-passI` passed `67/0`. The exact Mammoth owned the selected turn, click, command, PrimaryHand natural weapon, and Standard (`0 -> 6`); the rider Standard remained `0`. One attack, roll, and damage rule dealt 24 damage, with zero duplicate pair attacks, zero repaths, a healthy unchanged `medium-humanoid-mammoth-v1` profile, and exact cleanup.

The preceding dev.12 Mammoth run is preserved as immutable diagnostic failure: its old scenario forced rider selection before a mount-owned TB action. Production correctly rejected that selection. Dev.13 selects the exact policy-required action actor and passes without weakening any product predicate.

## Portrait and rights

Initialized-library audit `20260829T043500Z-phase3c-dev3-stock-portrait-passA` scanned 104,667 stock objects and found no suitable native Horse/Pony portrait or icon set. `CR1_HorseRiding` has no usable portrait; this was the exact cause of the Mammoth fallback.

KMC now embeds an original, redistributable Horse portrait set created for this project. It is not cropped, traced, or derived from Kingmaker, Wrath, YouTube, or the supplied screenshots.

| Asset | Dimensions | SHA-256 |
|---|---:|---|
| `HorsePortraitOriginalMaster.png` | 1024x1536 | `0b623b98440de8131c138d08f45d87e02b51f034cba313aeb36f81cbe078520f` |
| `HorsePortraitLarge.png` | 692x1024 | `8b7b4386de1b5adbd9f7f9f1c3728de32325b03c5f2dfc2fe6c7babf95a712e7` |
| `HorsePortraitMedium.png` | 330x432 | `890327ecc9e9b092b4343140fd9eb839800bb1044d8e4aeafeaaa1476a44ba61` |
| `HorsePortraitSmall.png` | 185x242 | `d0c5c876a827a0b8842d35833492e2d40b632ad5fbec7e70c8c2d72f7209fa16` |
| `HorseIcon.png` | 128x128 | `b088d4b29de3cdfc536c254cf47abbe52af4000aa8f25ac742c3d0612a253f02` |

Runtime portrait blueprint GUID is `6874a165bf8bda3531ee4e2abc10c899`. Horse unit, party, Ranger selection, feature, and control surfaces no longer use Mammoth art. Human review still decides visual quality and readability.

## Movement, pose, animation, and IK

The exact Horse blueprint speed remains 50 feet and the live movement-agent maximum remains `5.08`. In the bounded solo measurements, the unmounted Horse averaged `2.3040` world units/second over `0.8531` seconds and the mounted pair averaged `2.9747` over `0.6568` seconds. The mounted pair was not slower than the unmounted Horse in this controlled bound; no blueprint-speed increase was made. Mixed-party formation feel and long-run comparison with stock controls remain manual/observational gates because stock group movement may cap to slower members.

Final Horse-only pose candidate C changes candidate B as follows:

| Parameter | Candidate B | Candidate C |
|---|---:|---:|
| pelvis local position | `(0,-0.12,-0.02)` | `(0,-0.17,-0.02)` |
| left/right foot target | `(+/-0.18,-0.58,0.11)` | `(+/-0.15,-0.62,0.11)` |
| left/right knee hint | `(+/-0.20,-0.14,0.16)` | `(+/-0.16,-0.16,0.16)` |

Candidate C measured left/right stirrup distances `0.39243558` / `0.4600763`, zero clamps, microunit residuals, and maximum/average pose application `19.0` / `13.9` microseconds. Mammoth values, native Horse scale, assets, and other rider categories are unchanged. Visual seat contact, clipping, and weapon/shield clearance remain human-gated.

The controlled DollRoom probe observed exact rider IK setup `1/1`, a Horse simple-unit-view preview stable for three frames, and no attributable KMC IK exception. It does not prove every real Inventory/Character-screen path; that physical UI flow remains in the manual checklist. Buff Planner exceptions remain outside this repository.

Door feedback now uses the exact mount display name or generic `mount`, never `Mammoth` for a Horse. An occupied foreign/current Move slot is not overwritten; the player receives a nonexceptional busy boundary and may retry after it becomes idle. The already human-confirmed distant-door approach/open/traverse behavior remains accepted.

## Gates, restoration, and installation

Final offline totals are source `22/0`, clean Release build, component `267/0`, visual/source-order `18/0`, harness/protocol `235/0`, and exact assembly `378/0` (`354` Kingmaker plus `24` Wrath), with PowerShell/JSON parsers, diff, package validation, and prohibited-payload validation passing.

Suite admission and both targeted WhatIf passes proved zero mutation. The final Mammoth run's run/save/Mods transaction records are all `restored`; their SHA-256 values are `b84c5eef29bd57ddc7eb098e3599d20d1886c2e150bd56dd69483a888a4b6054`, `2b8ea826e32e3aac3f26c405c8945659613ebd061be9fb7d8a0693a5f9db1a3c`, and `322cf0b238c027f7f5257ca5b0c030322ad461c6a75d61ea669bf5ef041e0e18`. Baseline remained immutable, Working was restored, and no process/lock/sentinel/staging residue remained.

The guarded deployment helper installed and verified dev.13. Installed UMM ID/version are `KingmakerMountedCombat` / `0.1.0-phase3c-dev.13`; installed inventory digest is `c6911a7e68016c3e2ceef2abb2fd160f9ddd550704f794035d0b11890b296754`. Deployment record SHA-256 is `1f579ce9143f26e7aa70a421617be884c7d19cf320001b0cb5404ae1690e0920`; foreign Mods inventory was unchanged.

## Deliberate limits

This private alpha does not implement unified/shared mounted turns, stock right-click or auto-attack integration, mounted ranged combat, mounted spellcasting, mounted feats, mounted AoO, mounted charge, persistent mounted state, automatic remount, additional species, Small riders, enemy riders, Paladin Divine Steed, a public release, or a merge to `main`.

Rider and mount retain separate TB turns. Native controls live in the abilities drawer unless the player places them in an empty hotbar slot. Physical pointer feel, portrait quality, final pose, visible attack-animation readability, mixed-party movement feel, real Inventory/Character-screen IK, and ordinary save/area/UI flow require human review.
