# Phase 3D Runtime Scenario Matrix

Status: IN PROGRESS

All rows require a fresh clean Phase 3D package, guarded `KMC_AUTOMATION_WORKING` use, exact Mods/save restoration, structured telemetry, and independent post-run zero-mutation audit. `PASS` requires actual native input admission and visible outcome where specified; internal service invocation alone receives no usability credit.

## Runtime automation checkpoint — 2026-08-30T22:31:43Z

The strict runtime tranche is implemented for three guarded outer scenarios: `phase3d-horse-presentation-suite`, `phase3d-unified-combat-rt-suite`, and `phase3d-unified-combat-tb-suite`. Their exact required row sets, semantic ownership/resource/cardinality checks, immutable artifact validation, and synthetic mutation regressions pass locally. Every table row below remains `TODO` or `IN PROGRESS` until fresh clean-package Kingmaker evidence is admitted; offline or synthetic evidence is not relabeled as runtime PASS.

The new aggregates directly cover the core presentation, Rider Primary, stock melee, Shortbow/Light Crossbow/Sling, mode-transition, shared-turn, combat Mount/Dismount, five-foot, ordinary AoO, and unmounted-control rows. Rider/mount native incapacitation, full lifecycle/save/area/Wild Shape/door regressions, and the focused Mammoth regression remain separate existing guarded scenarios. Any matrix item not physically exercised by the aggregates must be run separately or retained as an explicit manual/known-limit gate.

Dev.1 RT attempt `20260830T233000Z-phase3d-dev1-rt-passA` is immutable uncredited `FAIL 3/1` before this matrix began: its nested Horse registration prerequisite rejected the new outer scenario name, so no Phase 3D artifact or gameplay row existed. Mandatory postrun restoration passed. Dev.2 repairs only that exact closed scenario allowlist and passes the complete offline gate; every matrix row below remains uncredited until fresh dev.2 evidence.

## Shared initiative

| Scenario | RT | TB | Required proof | Status |
|---|---:|---:|---|---|
| mounted-combat-start-single-initiative-entry |  | yes | one rider entry, zero mount entries | TODO |
| mounted-rider-initiative-bonus |  | yes | rider roll/result/bonus owns pair placement | TODO |
| mounted-turn-rider-portrait |  | yes | tracker current entry and portrait are rider | TODO |
| mounted-separate-action-ledgers | yes | yes | independent before/after cooldowns | TODO |
| mounted-shared-turn-action-order |  | yes | mount move, rider attack, mount attack without duplicate | TODO |
| mount-in-combat-before-either-acted |  | yes | merge preserves both ledgers; no extra turn | TODO |
| mount-in-combat-rider-already-acted |  | yes | spent rider resources preserved | TODO |
| mount-in-combat-mount-already-acted |  | yes | spent mount resources preserved | TODO |
| dismount-in-combat-no-extra-turn |  | yes | split deferred to safe next-round boundary | TODO |
| RT-to-TB-shared-turn | yes | yes | one rider turn, cooldown preservation | TODO |
| TB-to-RT-shared-turn | yes | yes | pair initiative/cooldown reconciliation | TODO |
| rider-incapacitation-turn-cleanup | yes | yes | clean dismount, coherent next actor | TODO |
| mount-incapacitation-turn-cleanup | yes | yes | clean dismount/removal, coherent next actor | TODO |

## Five-foot step

| Scenario | Required proof | Status |
|---|---|---|
| mounted-five-foot-step-no-aao | adjacent hostile requests zero successful disengage AoO for exact step | TODO |
| mounted-five-foot-step-distance | mount movement is positive and no more than native `7.5 ft` plus tolerance | TODO |
| mounted-five-foot-step-resource | rider and mount ordinary Move cooldown unchanged | TODO |
| mounted-five-foot-step-after-movement-rejected | stock restriction and explicit feedback | TODO |
| mounted-ordinary-move-aao-control | valid stock disengage AoO can occur | TODO |
| unmounted-five-foot-step-control | stock unmounted result unchanged | TODO |

## Rider Primary isolation

| Scenario | Required proof | Status |
|---|---|---|
| rider-primary-does-not-dismount-rt | relationship remains mounted after terminal hit/miss | TODO |
| rider-primary-does-not-dismount-tb | same under rider-led shared turn | TODO |
| rider-primary-rejection-does-not-dismount | no transition/cleanup event | TODO |
| rider-primary-target-cancel-does-not-dismount | target selector cancel has no cleanup | TODO |
| rider-primary-after-movement-does-not-dismount | movement plus attack retains exact pair | TODO |
| rider-primary-after-shared-turn-transition-does-not-dismount | RT/TB transition plus attack retains pair | TODO |

## Stock melee

| Scenario | RT | TB | Status |
|---|---:|---:|---|
| mounted-stock-click-melee-adjacent | yes |  | TODO |
| mounted-stock-click-melee-approach | yes |  | TODO |
| mounted-stock-click-melee-auto-repeat | yes |  | TODO |
| mounted-stock-click-melee-cancel | yes |  | TODO |
| mounted-stock-click-melee-shared-turn |  | yes | TODO |
| mounted-stock-click-melee-rider-only-explicit | yes | yes | TODO |
| mounted-stock-click-melee-mount-only-explicit | yes | yes | TODO |
| mounted-stock-click-invalid-target-feedback | yes | yes | TODO |
| unmounted-stock-attack-control | yes | yes | TODO |

Each mounted row records actual `ClickUnitHandler` admission, exact stock command identity, movement, attack/rule/roll/damage cardinality, actor/resource owner, terminal result, cancellation, and relationship retention.

## Ranged

| Scenario | RT | TB | Status |
|---|---:|---:|---|
| mounted-bow-adjacent | yes |  | TODO |
| mounted-bow-approach-to-range | yes |  | TODO |
| mounted-bow-auto-fire | yes |  | TODO |
| mounted-bow-cancel | yes |  | TODO |
| mounted-bow-shared-turn |  | yes | TODO |
| mounted-crossbow-or-reload-control | yes | yes | TODO |
| mounted-sling-control | yes | yes | TODO |
| mounted-ranged-line-of-sight | yes | yes | TODO |
| mounted-ranged-cover-concealment | yes | yes | TODO |
| mounted-ranged-aao-native-control | yes | yes | TODO |
| mounted-ranged-does-not-force-melee | yes | yes | TODO |
| unmounted-ranged-control | yes | yes | TODO |

## UI, presentation, and regression

| Scenario | Required proof | Status |
|---|---|---|
| mounted-single-rider-turn-portrait | one rider tracker portrait, rider selection/action bar/camera | TODO |
| Horse-small-portrait-close-up | original KMC art crop readable at party/tracker size | IN PROGRESS - original close-up integrated; runtime rendering/human legibility pending |
| saddle-icon | native safe reference or original KMC-owned saddle art; no Wrath asset | IN PROGRESS - native path/manifest scan found no safe reference; original KMC saddle integrated; runtime rendering/human legibility pending |
| mount-ability-in-combat | visible, eligible, one rider Move cost | TODO |
| dismount-ability-in-combat | visible, one rider Move cost, no extra turn | TODO |
| Horse-pose-final-idle-walk-run-turn-stop | final one-cycle lower-seat acceptance | IN PROGRESS - dev.2 pelvis-local `Y=-0.29` failed exact right-stirrup gate (`0.5325693`); single repair preserves pelvis `Y=-0.17` and applies stable Horse mount-root `Y=-0.08`; fresh dev.3 runtime/manual evidence pending |
| Horse-pose-Mammoth-unchanged | Mammoth profile bytes and behavior unchanged | IN PROGRESS - source constants and component locks unchanged; focused runtime regression pending |
| Horse creation/lifecycle/save reload | previously qualified behavior remains exact | TODO |
| door approach/open/traverse | pair interaction remains exact | TODO |
| Wild Shape and save/area cleanup | clean dismount | TODO |
| target-selected Mount outside combat | Phase 3C control remains exact | TODO |
| explicit Dismount | Phase 3C control plus combat cost contract | TODO |
| focused Mammoth Mount/move/Primary/Dismount | one bounded regression only | TODO |
| foreign-mod isolation | no dependency/reference or foreign mutation | TODO |

## Manual gate

The final review package must ask a human to verify ordinary hostile right-click feel, one shared rider portrait/turn, understandable separate actor actions, five-foot-step cursor/feedback, combat Mount/Dismount feedback, ranged stopping behavior, saddle icon legibility, tight Horse portrait crop, and the final lowered Horse seat. The milestone stops at `UNIFIED MOUNTED COMBAT ALPHA — MANUAL REVIEW REQUIRED`.
