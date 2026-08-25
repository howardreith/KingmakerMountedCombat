# Horse runtime scenario matrix

Superseding status (2026-08-25T21:29:32Z): IN PROGRESS — native asset audit credited; unmounted aggregate implemented but not yet runtime-credited.

Status: TODO — scenario contracts defined; runtime evidence not yet credited

All save-backed rows use only the project-owned `KMC_AUTOMATION_WORKING` fixture, suite-scoped external-state admission, KMC-only Mods staging, transaction recovery, and audit-before-evidence. Each credited A/B pair must use one exact immutable package.

| Tranche | Scenario | Mode | Required result |
|---|---|---|---|
| B | `horse-companion-unmounted-suite` | one bounded RT→TB→RT Working process | corrected stock-baseline registration; creation/ownership; rank 4→level 4; native selection/movement; Bite+two-Hoof enumeration; exact RT/TB Bite chains; death/recovery; respec destruction; Ranger uninstall/re-enable; unrelated-pet/Mammoth and mode/pause/selection/target restoration |
| A | `horse-native-asset-audit` | loaded Working fixture, observation only | exact horse/pony blueprint, resource, rig, animation, collider, body, ownership, and reverse-reference record; no unit/save mutation |
| B | `horse-companion-blueprint-registration` | loaded Working fixture, observation only | exact KMC GUID/object identities, AddPet/class/view/stats/attacks/upgrade/localization, Ranger 7→8 append, exact 8→7 restore, exact 7→8 re-enable; no spawn/save mutation |
| B | `horse-companion-create` | RT | Ranger selection contains stock choices plus one KMC horse; exact AddPet/SetMaster pair; Large native view; selectable and controllable |
| B | `horse-companion-progression` | RT | expected pet level/rank/upgrade/stat/natural-attack progression at bounded checkpoints |
| B | `horse-unmounted-movement` | RT A/B | ordinary party movement and selection; no duplicate command; non-horse matched control |
| B | `horse-unmounted-combat` | RT A/B | native bite/hoof contract, attack/roll/damage/resource cardinality, death/recovery |
| B | `horse-unmounted-turn-based` | TB A/B | controllable separate horse turn, movement, natural attack, no turn/action duplication |
| B | `horse-save-load-respec` | RT | exact companion survives save/load; respec-away removes it; reinstall/reselect is coherent |
| B | `horse-uninstall-boundary` | no game + bounded save check | documented respec-before-uninstall policy; no runtime registration residue after disable/unload |
| C | `horse-mounted-open-ground` | RT A/B | horse sole mover; rider attachment/pose stable; rider and horse primaries exact |
| C | `horse-mounted-turn-based` | TB A/B | horse turn controllable; rider-turn movement delegates to horse; both overlay attacks exact |
| C | `horse-mounted-transitions` | RT→TB→RT | coherent pair, selection, portrait, action bar, camera, pose, ledgers, and cleanup |
| C | `horse-mounted-doorway` | RT | distant click, horse approach, one rider-owned stock interaction, stock traversal and target reach |
| C | `horse-mounted-lifecycle` | RT/TB | menus, Wild Shape, explicit dismount, save/load, area, death/incapacitation, cancel/interruption |
| C | `mammoth-shared-regression` | RT/TB targeted | unchanged Mammoth profile and shared relationship/command behavior |
| D | `target-selected-mount-action` | RT | activate Mount then select exact eligible horse/Mammoth; useful rejection; overlay fallback preserved; no saved action residue |

## Universal cardinality gates

The aggregate row supersedes separate automated processes for the creation, progression, RT movement/combat, TB combat, death/recovery, and respec portions of the individual Tranche B rows. It does not relabel actual disk save/load as automated: the guarded harness forbids Kingmaker's crash-unsafe temporary save leaf, so persistence across a real save/reload remains in the final human checklist.

No row may produce duplicate movement, command, turn, attack, roll, damage, interaction, opportunity, animation-trigger, or resource chains. Native path-object replacement is telemetry, not a standalone product failure when the player-owned command remains healthy and the accepted player-visible contract succeeds.

## Human-only rows

The exact final horse package requires human review of ordinary pointer targeting, seat and stirrup appearance, idle/walk/run/turn/stop/reverse feel, doorway/indoor clipping, selection/portrait/action bar/camera feel, menus and fog/world flash, Rider/Horse primary discoverability, and mount/dismount presentation.
