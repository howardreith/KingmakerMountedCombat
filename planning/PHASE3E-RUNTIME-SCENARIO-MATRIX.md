# Phase 3E Paired-Scheduler Runtime Scenario Matrix

Status: PAIRED SCHEDULER PIVOT — SEPARATE-TURN FALLBACK READY

## Final Phase 3E gate disposition

Gate 1 remains immutable `PASS 140/0` from dev.4 Mammoth A/B. The final dev.12 Horse rerun additionally passes one exact Horse Mount Primary scheduler action (`8 PASS / 1 FAIL` rows overall; Mount Primary itself exact), but `exact-turn-completion` is `FAIL` under K9. Gates 2-5 are `DEFER — EVIDENCED` because mission order forbids broadening after that failure. Gate 6 ships the accepted separate-turn fallback with both experimental gates default false. No failed result is relabeled.

Final Gate 6 package authority is clean guarded-published commit `16ccc71cabde70398130386f0e9e9380e1110495`, version `0.1.0-phase3e-fallback.1`, ZIP/manifest/DLL SHA-256 `9451787c08d39ec2164d75f1c36fb4d54245e4228ff12855950fc26798be6698` / `43e783839fcb1c25c064c9f9f58934bb158f94ca6d7f81da332907789f2c0881` / `5bcc3bc61bb1677ea81037fdc5a8ebd740ff4d0753d5255e37fcc789e6407f2f`, and MVID `57f442aa-fc22-4277-810b-3328300e37e7`. Package validation `10/0`, suite12 `ada2b36ecc739f2f964d37a92d321b63a5110153a5f186e7673b653a5d7700ec`, guarded WhatIf purity, and independent post-WhatIf audit pass. No new live gameplay credit is inferred from this settings-default/package checkpoint.

The first same-package dev.12 process `20260905T082300Z-phase3e-dev12-horse-tb-gate2` is an immutable pre-scheduler intake failure caused by the inherited one-frame out-of-combat Mount admission race. Rerun `20260905T090000Z-phase3e-dev12-horse-tb-gate2-rerun` is the decisive architecture result: its Horse Mount Primary lease is exact and residue-free, then native turn selection retains the redundant Horse and triggers fallback. Both runs passed independent restoration audit before gameplay evidence was read.

Dev.11 is clean published commit `b50a44cdfdf160f06f19ee48b8c5af7afc2385fa`; package/suite SHA-256 are `4cc3fd262c06a112d5bdca92032ca0b65262623608c4ba454bc284307425a4a4` / `2577387f77bbf569e50a224b9acc5251be7078e92f9ec431a6785b18c5648b33`, and WhatIf passed. Immutable audited live `20260905T051700Z-phase3e-dev11-horse-tb-gate2` is game `FAIL 52/2` after the first seven PASS rows. The exact log positively proves one post-Tick Horse skip, zero fallback/native Horse turn, and the original next unrelated unit `b6628a77-4962-47a4-a17c-88d9836fc9d5`. The remaining wait was not a production deadlock: this legitimate directly controllable roster member remained in native `Preparing` awaiting player input.

Dev.12 is the final bounded comprehensive Gate 2 cycle. Schema 6 records the exact native roster and uses native `ForceToEnd(false)` once only for an idle, reference-exact, leased fixture-player turn; it rejects mounted Horse, hostile, foreign, busy, duplicate, UI/pending blocked, or resource-mutating traversal. Explicit other-pair traversal is permitted only while unmounted for the spent-ledger controls. Production scheduler, turn selector, initiative, commands, resources, and defaults remain unchanged. All Gate 2+ rows stay `TODO` until one clean package/WhatIf/live/audit run reaches them.

Dev.10 clean commit/package/suite are `0b4dd1cd494a2765035477325afb1ae0e1bd3ee9` / `820047c137fde066f93045638c7ca27b0e27638f00e0053da681ac1146260052` / `2f73d4867ee6fcc09875b3a7491258c225eaadf391dad539989ac312e8724214`; WhatIf passed. Immutable audited live `20260905T030300Z-phase3e-dev10-horse-tb-gate2` is game `FAIL 52/2`. The natural pre-mounted rider turn and first seven rows passed, including exact rider-only attack ownership and cost. The next-rider-turn wait failed because the old mount-candidate postfix recursively called stock selection before the ended rider turn cleared; stock chose the Horse again, KMC entered fallback, and the Horse became native current unit. No Horse scheduler lease/action occurred.

Dev.11 consumes one narrow turn-completion repair cycle. It defers the reference-exact mount candidate in the `ChooseNextUnit` postfix and advances it once through the same native method from a `CombatController.Tick` postfix after `CurrentTurn` is null. Evidence schema 5 requires positive deferred/post-Tick counters, zero fallback/native Horse turn, a later exact rider current turn, and the complete disposed Horse mount-primary lease/ledger/rule/animation cardinality. Complete offline gates pass `22/Release/316/18/242/402`; all Gate 2+ rows remain `TODO` until one clean package/WhatIf/live/audit result. Gate 1 A/B remains immutable `PASS 140/0` and is not rerun.

Dev.9 clean published/package/suite identities are `f9082b166cd4958281d97707aac90e1c8a7f8ed4` / `adeb8a305f647738b765881869215cc1a48e669530ab926a3a607ebe6fb015f5` / `9cab4d8398ee9a5ea23e22e186b3e777a20e415c724041471ba0138f7f2e98a0`; its full-continuity WhatIf passed. Immutable live `20260905T010000Z-phase3e-dev9-horse-tb-gate2` is game `FAIL 42/2`; independent audit-before-read passed exact suite, save metadata/content, Mods, Baseline, Working, process, lock, sentinel, and deployment restoration. The rider Mount shell was visited once by stock TB command advancement, was stock-eligible, and started/finished `Success` one frame after admission. Exact installed logging proves the later ability delivery was rejected solely by the still-intentional domain rule that mounting is outside-combat only. No paired-scheduler command existed, so every Gate 2+ gameplay row remains `TODO` and no scheduler repair cycle or kill criterion fired.

Dev.10 changes diagnostic setup only. The Horse TB scenario must first complete the established native out-of-combat Mount path, then enter its exact mounted pair into the target/combat/TB natural-rider-turn setup under reversible pair-local AI isolation. It must begin existing sequencing only after the single-entry, rider-principal, exact-current-turn, separate-ledger state is observed. `mount-in-combat-*` and `mount-ability-in-combat` remain `TODO`; they are not skipped into PASS and remain reserved for Tranche 7 after the earlier gates. A single clean-package run will attribute every reached row.

Dev.8 clean package/suite/WhatIf are immutable and audited. Live `20260904T221700Z-phase3e-dev8-horse-tb-gate2` is outer/game `FAIL 42/2` before any Gate 2 row: the natural rider turn and every readiness gate passed, the native Mount click/cast request admitted one exact non-AI rider Move-slot shell, but the diagnostic incorrectly required its stock `CreatedByPlayer=false` field to be true and failed in the admission frame. No post-admission native tick, relationship, scheduler lease, action, rule, damage, resource, or turn-completion path ran. Dev.9 corrects only that provenance assertion and evidence schema; all runtime rows remain truthfully unchanged pending one fresh package process.

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
| exact-turn-completion | FAIL | Final dev.12 rerun hit K9 when native selection retained the redundant Horse after the exact pair command had completed. |

Do not broaden beyond this gate until pass A and B both pass.

Immutable pre-credit attempts: dev.2 run `20260904T094306Z-phase3e-dev2-mammoth-tb-passA` remains `FAIL 69/1` with exact restoration. All gameplay and ownership/cardinality facts passed: admission `3982`, first grant `4293`, start `4294`, last drive `4527`, `235` once-per-frame drives, one start/terminal/mount Standard charge, one mount-owned attack/roll/damage, zero rider cost, rider current retained, no native mount turn, and exact cleanup. The one failing in-game diagnostic counted preserved native `WaitingForUI` frames as actionable.

Dev.3 run `20260904T113800Z-phase3e-dev3-mammoth-tb-passA` remains immutable outer `FAIL`, game `PASS 70/0`, after an immediate exact restoration audit. Admission/first grant/start/last drive were `3987/4309/4310/4542`; `234` drives, one start/terminal/mount charge, one mount-owned rule chain, zero rider cost/native mount turn/duplicate/fault/residue all passed. The sole outer contradiction required schema-55/56 action-actor `CanActInCombat=false`, while all real dev.21-dev.3 evidence records true. Dev.4 corrects only that external predicate. At that checkpoint neither Gate 1 row was credited; fresh same-package dev.4 A/B below now provide authority.

Credited dev.4 A/B use clean published `27e088b4dafe4d449127b5e2920f09b3a0ed4f79`, ZIP `c6636c54eaee15bc1ab7c1c72a867dd0d0bc9ff62ae14d3a62dbd61672da3d7a`, suite `20260904T123300Z-phase3e-dev4-paired-scheduler-suite3` / `686f131a580377ca0b77ffc28bdd3d04eb12bfc0f6d24d8f59ad5ceb1963ce7b`. Runs A/B pass `70/0` each with frames `3984/4295/4296/4529` and `3968/4280/4281/4514`, `235` unique drives each, and exact audited restoration. Gate 1 runtime total is `140/0`; `exact-turn-completion` remains TODO pending explicit next-combatant advancement evidence.

## Gate 2 — sequencing and separate ledgers

Dev.5 changes only diagnostic setting scope: the existing Horse TB suite temporarily enables and exactly restores `EnablePairedCommandScheduler`. Its clean package/suite/WhatIf passed, but immutable live `20260904T155035Z-phase3e-dev5-horse-tb-gate2` failed before Mount admission because one foreign AI-created Horse Standard command occupied the unmounted slot. Rider current/actionable identity held for `2,520` frames and the scheduler correctly rejected the foreign command. Independent audit-before-read passed exact restoration. No Gate 2 row receives credit and no scheduler repair cycle is consumed.

Immutable dev.6 run `20260904T174752Z-phase3e-dev6-horse-tb-gate2` is outer/game `FAIL 42/2` at `AwaitCombatMount`; independent audit-before-read passed exact restoration. The new Horse-AI isolation boundary passed before target creation, but a separate rider AI Standard attack existed before diagnostic turn preparation. Native preparation removed it while rider hands remained busy. The exact rider-owned Mount `UnitUseAbility` occupied the rider Move slot with `CanStart=true`, legal proximity, and correct executor/target/type, yet stayed unstarted for 30 seconds with `executorHandsBusy=true`. It completed only after cleanup restored RT, and the relationship transition correctly refused the mode change. No relationship, scheduler lease, mount command, attack/rule/damage, resource, or completion row ran; no Gate 2 credit and no scheduler repair cycle is assigned.

Immutable dev.7 run `20260904T195400Z-phase3e-dev7-horse-tb-gate2` is also outer/game `FAIL 42/2` at `AwaitCombatMount`, after an independent audit-before-read passed exact suite/save/Mods/Baseline/Working restoration. The dev.7 rider and Horse AI/readiness correction passed: both exact leases were active and stable before target creation, both containers remained empty, rider hands/equipment became idle, and the native Mount click admitted one exact rider Move-slot `UnitUseAbility` at legal adjacency with `CanStart=true`, no cooldown, and no approach. Earlier prose incorrectly called this command “player-created”; it did not establish `CreatedByPlayer=true`. It remained unstarted for 30 seconds and ran only after cleanup returned to RT. This confirms a remaining native turn-start gate, not a paired scheduler failure; no Gate 2 row, repair cycle, or kill criterion is credited.

Dev.8 is the final bounded native-start intake checkpoint before broader Gate 2 attribution. It removes the diagnostic-only direct rider `StartTurn` from this path and requires the natural native turn, cleared `m_NextUnit`, cleared zero-count `WaitingForUI`, Default/unpaused mode, exact AwakeUnits enumeration/view tick eligibility, no nausea, selection, hands, equipment, and empty command predicates for two frames. Schema-v2 evidence records exact pre-extension stock `TickCommandTurnBased` encounters and eligibility for the rider Mount shell, plus start/terminal frames and every remaining hard predicate. PASS requires zero synthetic turn requests, at least one stock-eligible visit, start within two actionable frames, no duplicate frame visit, successful terminal removal, exact rider Move cost, unchanged Horse ledger, and rider current identity. A truthful deadline remains uncredited and must identify no enumeration, native turn-gate rejection, or native `ShouldStartCommand` rejection without inference.

Dev.7 changes only diagnostic intake/readiness: a separate reversible rider-AI lease validates after the Horse lease and before the sole target creation, native Mount admission waits for idle rider hands/equipment while retaining the required `Preparing` player-input state, and cleanup requires exact restoration of both leases. Complete offline gates pass. One fresh clean-package process remains required before any Gate 2-5 row can change from TODO.

| Row | Status | Required evidence |
|---|---|---|
| mount-first-rider-second | DEFER — EVIDENCED | Not broadened after K9; one exact mount-only action passed. |
| rider-first-mount-second | DEFER — EVIDENCED | The later ordinary sequence was interrupted by fail-closed fallback after K9. |
| mount-action-only | PASS | Dev.12 rerun: one Horse Bite chain, mount Standard only, rider Standard unchanged. |
| rider-action-only | PASS | Dev.12 rerun preserved the earlier exact rider-only Phase 3D row. |
| one-ledger-already-spent | DEFER — EVIDENCED | Not fully qualified before K9. |
| both-ledgers-spent | DEFER — EVIDENCED | Not fully qualified before K9. |
| target-dies-after-first | DEFER — EVIDENCED | Not reached before K9. |
| cancellation | DEFER — EVIDENCED | Scheduler component coverage only; runtime sequencing gate not reached. |
| interruption | DEFER — EVIDENCED | Scheduler component coverage only; runtime sequencing gate not reached. |
| end-turn | FAIL | Native post-action turn completion retained the redundant mount. |
| next-round-reset-once | DEFER — EVIDENCED | Round-boundary K9 prevents qualification. |
| unrelated-initiative-order | DEFER — EVIDENCED | Dev.11 preserved one unrelated successor, but final comprehensive traversal failed. |

## Gate 3 — ordinary hostile-click TB melee and ranged

| Row | Status | Required evidence |
|---|---|---|
| TB-melee-approach | DEFER — EVIDENCED | Not reached after the required turn-completion gate failed. |
| explicit-rider-only | PASS | Dev.12 rerun retained exact rider-only ownership and cost before K9. |
| explicit-mount-only | PASS | Dev.12 rerun passed exact Horse Mount Primary before K9. |
| TB-ranged-approach-to-range | DEFER — EVIDENCED | Unified TB ranged remains unqualified; Phase 3D RT behavior is retained. |
| TB-ranged-no-forced-melee | DEFER — EVIDENCED | Unified TB ranged remains unqualified; Phase 3D RT behavior is retained. |
| TB-Shortbow | DEFER — EVIDENCED | Not reached after the required turn-completion gate failed. |
| TB-Crossbow-reload | DEFER — EVIDENCED | Not reached after the required turn-completion gate failed. |
| TB-Sling | DEFER — EVIDENCED | Not reached after the required turn-completion gate failed. |
| LoS-recovery | DEFER — EVIDENCED | Not reached after the required turn-completion gate failed. |
| target-movement-bounded-repath | DEFER — EVIDENCED | Not reached after the required turn-completion gate failed. |
| movement-cancellation | DEFER — EVIDENCED | Not reached after the required turn-completion gate failed. |

## Gate 4 — combat Mount/Dismount and lifecycle

| Row | Status | Required evidence |
|---|---|---|
| combat-Mount-before-either-acted | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| combat-Mount-after-rider-acted | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| combat-Mount-after-mount-acted | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| combat-Mount-after-partial-movement | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| combat-Dismount | DEFER — EVIDENCED | Unified-TB split behavior not reached; accepted RT behavior retained. |
| no-extra-turn | DEFER — EVIDENCED | K9 prevents unified-TB qualification. |
| mount-upcoming-native-slot | DEFER — EVIDENCED | K9 is the unresolved redundant-slot boundary. |
| mount-past-native-slot | DEFER — EVIDENCED | Not reached after K9. |
| RT-to-TB | DEFER — EVIDENCED | Scheduler remains default off in fallback. |
| TB-to-RT | DEFER — EVIDENCED | Scheduler remains default off in fallback. |
| rider-unconsciousness | DEFER — EVIDENCED | Component cleanup only; unified runtime tranche not reached. |
| mount-unconsciousness | DEFER — EVIDENCED | Component cleanup only; unified runtime tranche not reached. |
| target-invalidation | DEFER — EVIDENCED | Component cleanup only; unified runtime tranche not reached. |
| invalid-during-transition | DEFER — EVIDENCED | Not reached after K9. |
| area-save-cleanup | PASS | Scheduler state is runtime-only; audited processes left no lease/deployment/save residue. |

## Gate 5 — five-foot step and AoO

| Row | Status | Required evidence |
|---|---|---|
| mounted-five-foot-step-no-AoO | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| mounted-ordinary-move-AoO-control | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| five-foot-distance-bound | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| five-foot-one-step-limit | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| five-foot-post-movement-rejection | DEFER — EVIDENCED | Gated tranche not opened after K9. |
| unmounted-five-foot-control | DEFER — EVIDENCED | No new Phase 3E runtime control; stock path unchanged. |
| unrelated-party-AoO-control | DEFER — EVIDENCED | No broad AoO patch was added. |
| hostile-movement-AoO-control | DEFER — EVIDENCED | No broad AoO patch was added. |

## Gate 6 — fallback and regressions

| Row | Status | Required evidence |
|---|---|---|
| separate-turn-fallback | PASS | Accepted Phase 3C path retained; fallback package defaults unified mode and scheduler off. |
| scheduler-gate-disabled | PASS | Default-false component gate remains inert; fallback package also defaults unified mode off. |
| non-mounted-Horse | DEFER — EVIDENCED | Scheduler defaults inert; no new fallback-package runtime row. |
| non-mounted-Mammoth | DEFER — EVIDENCED | Scheduler defaults inert; no new fallback-package runtime row. |
| unrelated-companion | DEFER — EVIDENCED | Dev.11 preserved one unrelated successor; comprehensive order later failed. |
| unrelated-combatant-initiative | DEFER — EVIDENCED | K9 prevents unified initiative qualification; separate turns remain stock. |
| unmounted-melee | PASS | Inherited qualified Phase 3D control; fallback changes only settings defaults/version. |
| unmounted-ranged | DEFER — EVIDENCED | Phase 3D final automated control remained deferred; focused manual control retained. |
| RT-hostile-click-melee | PASS | Phase 3D dev.17 A/B accepted rows; no RT production seam changed for fallback. |
| RT-ranged | PASS | Phase 3D dev.17 A/B accepted rows; no RT production seam changed for fallback. |
| Horse-regression | PASS | Qualified Horse native controls/presentation retained; fallback only changes experimental defaults. |
| Mammoth-regression | PASS | Gate 1 A/B and inherited presentation remain exact; scheduler is dormant by default. |
| no-foreign-mod-dependency | PASS | Source/package allowlist and assembly/reference gates remain authoritative. |

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
