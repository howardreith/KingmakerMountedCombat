# Phase 3E Paired-Scheduler Runtime Scenario Matrix

Status: IN PROGRESS

All runtime rows require a clean guarded package, fresh run ID, exact package/DLL identity, transactional Mods restoration, protected-save proof, immutable failed evidence, and postrun audit before gameplay evidence is read. `TODO` means unqualified, not failed.

## Gate 0 — observation and component contract

| Row | Status | Required evidence |
|---|---|---|
| exact-command-lifecycle-observation | PASS | Dev.1 run `20260904T060000Z-phase3e-dev1-mammoth-tb-observation-passA`: exact awake/in-list Mammoth Standard command encountered `2,485` times; stock false `2,485`, true `0`; rider exact current/Preparing; zero override and scheduler drive. Gameplay row remains immutable expected `FAIL 49/1`; immediate independent audit passed before read. |
| leased-command-accepted | PASS | Component policy/state test binds exact KMC rider/mount/turn/generation/slot identities. Runtime ownership remains Gate 1. |
| foreign-command-rejected | PASS | Reference-different origin is rejected before drive. |
| AI-command-rejected | PASS | AI action/origin is rejected before drive. |
| AoO-command-excluded | PASS | `UnitAttackOfOpportunity` is explicitly ineligible and remains stock. |
| wrong-rider-rejected | PASS | Current rider mismatch is rejected. |
| wrong-mount-rejected | PASS | Executor mismatch is rejected. |
| stale-generation-rejected | PASS | Relationship-generation mismatch is rejected. |
| stale-turn-rejected | PASS | Exact turn-reference mismatch is rejected. |
| slot-replacement-rejected | PASS | Expected-slot mismatch is rejected and cannot be adopted. |
| start-exactly-once | PASS | Repeated lifecycle observation records one start. Native runtime cardinality remains Gate 1. |
| tick-at-most-once-per-frame | PASS | Same-frame second authorization faults closed with one drive count. |
| finish-exactly-once | PASS | Second terminal assignment is rejected. |
| interrupt-exactly-once | PASS | Repeated normal or fault cleanup retains one interrupt. |
| cleanup-idempotent | PASS | Second dispose is inert and cleanup count remains one. |
| exception-cleanup | PASS | Component fault/dispose test retains reason and exact cleanup state. Runtime exception isolation remains later. |
| no-serialized-state | PASS | Domain and integration state are plain runtime services with no serialization attribute or persistent owner. |
| fallback-disabled-inert | PASS | Default-false component gate rejects admission without a drive. Runtime fallback remains Gate 6. |

## Gate 1 — minimal in-range mount primary

These two rows use the same immutable package and suite but fresh game processes.

| Row | Status | Required evidence |
|---|---|---|
| shared-rider-turn-mount-primary-passA | PASS | Dev.4 `20260904T133300Z-phase3e-dev4-mammoth-tb-passA`, `70/0`, audited before read. |
| shared-rider-turn-mount-primary-passB | PASS | Dev.4 `20260904T140400Z-phase3e-dev4-mammoth-tb-passB`, `70/0`, same immutable package/suite, audited before read. |
| rider-remains-current | PASS | A/B retain the exact rider at dispatch/outcome and every scheduler drive; zero mount turn. |
| mount-standard-only | PASS | A/B each record mount Standard `0 -> 6` once and rider Standard `0 -> 0`. |
| one-chain-cardinality | PASS | A/B each record one start, terminal, attack, roll, damage, and resource transition; zero duplicate drive. |
| no-native-mount-turn | PASS | A/B record zero native action-actor turn start and retain rider principal. |
| exact-turn-completion | TODO | Rider turn stays while command runs and can advance once after eligible pair work is done. |

Do not broaden beyond this gate until pass A and B both pass.

Immutable pre-credit attempts: dev.2 run `20260904T094306Z-phase3e-dev2-mammoth-tb-passA` remains `FAIL 69/1` with exact restoration. All gameplay and ownership/cardinality facts passed: admission `3982`, first grant `4293`, start `4294`, last drive `4527`, `235` once-per-frame drives, one start/terminal/mount Standard charge, one mount-owned attack/roll/damage, zero rider cost, rider current retained, no native mount turn, and exact cleanup. The one failing in-game diagnostic counted preserved native `WaitingForUI` frames as actionable.

Dev.3 run `20260904T113800Z-phase3e-dev3-mammoth-tb-passA` remains immutable outer `FAIL`, game `PASS 70/0`, after an immediate exact restoration audit. Admission/first grant/start/last drive were `3987/4309/4310/4542`; `234` drives, one start/terminal/mount charge, one mount-owned rule chain, zero rider cost/native mount turn/duplicate/fault/residue all passed. The sole outer contradiction required schema-55/56 action-actor `CanActInCombat=false`, while all real dev.21-dev.3 evidence records true. Dev.4 corrects only that external predicate. At that checkpoint neither Gate 1 row was credited; fresh same-package dev.4 A/B below now provide authority.

Credited dev.4 A/B use clean published `27e088b4dafe4d449127b5e2920f09b3a0ed4f79`, ZIP `c6636c54eaee15bc1ab7c1c72a867dd0d0bc9ff62ae14d3a62dbd61672da3d7a`, suite `20260904T123300Z-phase3e-dev4-paired-scheduler-suite3` / `686f131a580377ca0b77ffc28bdd3d04eb12bfc0f6d24d8f59ad5ceb1963ce7b`. Runs A/B pass `70/0` each with frames `3984/4295/4296/4529` and `3968/4280/4281/4514`, `235` unique drives each, and exact audited restoration. Gate 1 runtime total is `140/0`; `exact-turn-completion` remains TODO pending explicit next-combatant advancement evidence.

## Gate 2 — sequencing and separate ledgers

Dev.5 changes only diagnostic setting scope: the existing Horse TB suite temporarily enables and exactly restores `EnablePairedCommandScheduler`. Complete offline gates pass. One clean-package runtime process will attribute existing rows before new Gate 2 evidence is added.

| Row | Status | Required evidence |
|---|---|---|
| mount-first-rider-second | TODO | Two actor-owned actions, distinct ledgers, deterministic completion. |
| rider-first-mount-second | TODO | Default ordinary-click order; target remains valid for second action. |
| mount-action-only | TODO | Mount spends only mount Standard; rider action remains available. |
| rider-action-only | TODO | Rider spends only rider Standard; mount action remains available. |
| one-ledger-already-spent | TODO | Available actor acts; spent actor does not refresh or duplicate. |
| both-ledgers-spent | TODO | Turn advances once, does not stick. |
| target-dies-after-first | TODO | Second action canceled before start with zero late rule/damage. |
| cancellation | TODO | Exact active/pending command interrupted once; slots and lease clean. |
| interruption | TODO | Native interruption result preserved once; no refund/duplicate. |
| end-turn | TODO | Player end-turn cannot strand or double-run pair work. |
| next-round-reset-once | TODO | One natural rider prep and one pair-local mount ledger prep; no duplicate events. |
| unrelated-initiative-order | TODO | No skipped, duplicated, or reordered unrelated combatant. |

## Gate 3 — ordinary hostile-click TB melee and ranged

| Row | Status | Required evidence |
|---|---|---|
| TB-melee-approach | TODO | Mount-owned approach, then rider Standard and legal mount Standard in deterministic order. |
| explicit-rider-only | TODO | One rider primary, no mount action/cost. |
| explicit-mount-only | TODO | One mount primary, no rider action/cost. |
| TB-ranged-approach-to-range | TODO | Mount stops at rider-native range/LoS; rider fires natively. |
| TB-ranged-no-forced-melee | TODO | No mount melee closure caused by ranged intent. |
| TB-Shortbow | TODO | Native rider weapon/range/ammunition/AoO behavior; distinct ledgers. |
| TB-Crossbow-reload | TODO | Native reload/ammunition/weapon behavior and exact actor cost. |
| TB-Sling | TODO | Native rider Sling command/rules and no foreign dependency. |
| LoS-recovery | TODO | Mount-owned bounded recovery; native child LoS remains authority. |
| target-movement-bounded-repath | TODO | Existing single bounded repath, exact cancellation. |
| movement-cancellation | TODO | Exact move and action intent terminate without late attack. |

## Gate 4 — combat Mount/Dismount and lifecycle

| Row | Status | Required evidence |
|---|---|---|
| combat-Mount-before-either-acted | TODO | Rider Move cost once; untouched Standards preserved; no extra turn. |
| combat-Mount-after-rider-acted | TODO | Rider spent state preserved; mount does not refresh rider. |
| combat-Mount-after-mount-acted | TODO | Mount spent state preserved; upcoming/past slot reconciled safely. |
| combat-Mount-after-partial-movement | TODO | Both movement ledgers preserved exactly. |
| combat-Dismount | TODO | Rider Move cost once; split deferred to safe round boundary. |
| no-extra-turn | TODO | No immediate second rider/mount turn or unrelated skip. |
| mount-upcoming-native-slot | TODO | Redundant active-round slot suppressed without refresh. |
| mount-past-native-slot | TODO | Merge does not create another current-round action. |
| RT-to-TB | TODO | Scheduler starts clean only at exact TB/rider-turn boundary. |
| TB-to-RT | TODO | Exact lease interrupted/disposed; accepted RT behavior resumes. |
| rider-unconsciousness | TODO | Exact cleanup/fallback, zero stale drive. |
| mount-unconsciousness | TODO | Exact cleanup/fallback, zero stale drive. |
| target-invalidation | TODO | Pending/running action terminates honestly; no late chain. |
| invalid-during-transition | TODO | No ledger reset, extra turn, or residue. |
| area-save-cleanup | TODO | No scheduler state serialized; no lease/command residue. |

## Gate 5 — five-foot step and AoO

| Row | Status | Required evidence |
|---|---|---|
| mounted-five-foot-step-no-AoO | TODO | Native step mode, mount physical movement, legal bound, zero step-only AoO. |
| mounted-ordinary-move-AoO-control | TODO | Stock provocation remains possible. |
| five-foot-distance-bound | TODO | No more than native 7.5-foot bound. |
| five-foot-one-step-limit | TODO | Second step unavailable. |
| five-foot-post-movement-rejection | TODO | Ordinary prior movement disqualifies step. |
| unmounted-five-foot-control | TODO | Stock unmounted behavior unchanged. |
| unrelated-party-AoO-control | TODO | Another party member remains stock. |
| hostile-movement-AoO-control | TODO | Hostile movement/opportunity processing remains stock. |

## Gate 6 — fallback and regressions

| Row | Status | Required evidence |
|---|---|---|
| separate-turn-fallback | TODO | `EnableUnifiedMountedTurn=false` preserves accepted Phase 3C turns; scheduler inert. |
| scheduler-gate-disabled | TODO | Unified Phase 3D setting may remain on while scheduler gate false; no mount command eligibility extension. |
| non-mounted-Horse | TODO | No registration/admission/drive. |
| non-mounted-Mammoth | TODO | No registration/admission/drive. |
| unrelated-companion | TODO | Commands and resources unchanged. |
| unrelated-combatant-initiative | TODO | Native order and turn count exact. |
| unmounted-melee | TODO | Native command/rule/resource path unchanged. |
| unmounted-ranged | TODO | Native weapon/ammunition/AoO path unchanged. |
| RT-hostile-click-melee | TODO | Phase 3D accepted RT seam unchanged where production code overlaps. |
| RT-ranged | TODO | Phase 3D accepted RT range/LoS/reload/cancel behavior unchanged. |
| Horse-regression | TODO | Exact supported Horse profile and ownership remain sound. |
| Mammoth-regression | TODO | Existing Mammoth presentation/movement/primary behavior remains sound. |
| no-foreign-mod-dependency | TODO | Package and runtime references contain no gameplay-mod dependency. |

## Qualification order

1. finalize exact lifecycle answer;
2. component tests;
3. minimal mount-primary vertical slice;
4. fresh-process A/B;
5. completion/sequencing;
6. ordinary TB melee;
7. TB ranged;
8. combat Mount/Dismount;
9. five-foot step/AoO;
10. RT seams actually changed;
11. focused Horse/Mammoth regressions;
12. final immutable package and manual review.

Documentation-only changes do not trigger gameplay reruns. Observation-only changes receive only the exact observation scenario. Historical failures retain their original status and identity.
