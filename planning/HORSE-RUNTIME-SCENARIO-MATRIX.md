# Horse runtime scenario matrix

Superseding status (2026-08-26T15:03:02Z): IN PROGRESS - dev.10 aggregate is historical restored `FAIL 32/1`, with registration `13/0`, unmounted `19/1`, and exact TB command admission now proven. Its only failure is the healthy queued Bite losing the active turn when Kingmaker consumed a preexisting next-unit handoff after the first diagnostic `StartTurn`. Dev.11 requires two-frame exact horse-turn stability and performs at most one public reassertion only after observing that replacement. One clean audited aggregate remains before mounted-profile admission.

Superseding status (2026-08-26T13:26:50Z): IN PROGRESS - dev.9 aggregate is historical restored `FAIL 31/1`, with registration `13/0`, unmounted `18/1`, and exact RT Bite `1/1/1`, forced D20 `3`, zero unexpected pair attacks, and `16` damage. Its only failure is TB `0/0/0` after scenario dispatch before the full native turn/action readiness boundary. Dev.10 waits for exact Prepared/CanAct, unpaused, empty-command, idle-hand/equipment, Standard-action, live-target, CanAttack, and exact-selection readiness; it removes the redundant post-prepare scenario interruption and requires exact Standard ownership plus terminal Success. One clean audited aggregate remains before mounted-profile admission.

Superseding status (2026-08-26T11:20:50Z): IN PROGRESS - dev.8 aggregate is historical restored `FAIL 28/1`, but proves the requested RT Bite owned Standard and completed one exact attack/roll/damage chain for `15` damage. The remaining failure is the scenario's invalid exact-one-D20 predicate; credited stock critical evidence establishes `>= 1` as the correct boundary. Dev.9 publishes forced-roll and unexpected-pair counters for RT/TB and retains exact `1/1/1` plus zero-duplicate gates. One clean audited aggregate remains before mounted-profile admission.

Superseding status (2026-08-26T09:47:04Z): IN PROGRESS - dev.7 aggregate is historical restored `FAIL 28/1` and proves the submitted Bite was consumed by Kingmaker's native same-target active-command merge while native combat continued. Dev.8 isolates only the temporary horse's explicit RT/TB dispatch and requires reference-identical Standard ownership. One clean audited aggregate remains before mounted-profile admission.

Superseding status (2026-08-26T08:08:44Z): IN PROGRESS - dev.6 runtime proves exact Bite/Hoof/Hoof enumeration and all preceding gates, but remains historical restored `FAIL 28/1` because its first stock RT Bite never reached a terminal state before the old global deadline. Dev.7 adds one 20-second exact start/approach-state snapshot and no production behavior change; one audited aggregate observation remains before repair or mounted-profile admission.

Superseding status (2026-08-26T06:33:22Z): IN PROGRESS - dev.5 reached combat but remains historical restored `FAIL 26/1` because enabled hands repeated Bite through the secondary empty-hand fallback. Dev.6 adopts the exact native no-hands Bite/Hoof/Hoof topology; one clean aggregate remains before mounted-profile admission.

Superseding status (2026-08-26T05:10:00Z): IN PROGRESS - dev.4 progression and stock movement passed; aggregate remains historical restored `FAIL 25/1` at the later diagnostic target placement boundary. One dev.5 owner-relative harness retry remains before mounted-profile admission.

Superseding status (2026-08-26T03:40:00Z): IN PROGRESS - dev.3 aggregate is historical restored `FAIL 22/1`; exact Call of the Wild manual-leveling compatibility repair is offline-green and one dev.4 aggregate remains before mounted-profile admission.

Superseding status (2026-08-25T23:15:42Z): IN PROGRESS — first aggregate is historical restored `FAIL 22/1`; one exact dev.3 progression-repair retry remains before mounted-profile admission.

Superseding status (2026-08-25T21:29:32Z): IN PROGRESS — native asset audit credited; unmounted aggregate implemented but not yet runtime-credited.

Status: TODO — scenario contracts defined; runtime evidence not yet credited

All save-backed rows use only the project-owned `KMC_AUTOMATION_WORKING` fixture, suite-scoped external-state admission, KMC-only Mods staging, transaction recovery, and audit-before-evidence. Each credited A/B pair must use one exact immutable package.

| Tranche | Scenario | Mode | Required result |
|---|---|---|---|
| B | `horse-companion-unmounted-suite` | one bounded RT-to-TB-to-RT Working process | corrected stock-baseline registration; creation/ownership; rank 4 with either committed class level 4 or exact native target-XP handoff for manual leveling; native selection/movement; Bite+two-Hoof enumeration; exact RT/TB Bite chains; death/recovery; respec destruction; Ranger uninstall/re-enable; unrelated-pet/Mammoth and mode/pause/selection/target restoration |
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
