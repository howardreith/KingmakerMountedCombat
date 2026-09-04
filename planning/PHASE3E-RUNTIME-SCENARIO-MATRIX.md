# Phase 3E Paired-Scheduler Runtime Scenario Matrix

Status: IN PROGRESS

All runtime rows require a clean guarded package, fresh run ID, exact package/DLL identity, transactional Mods restoration, protected-save proof, immutable failed evidence, and postrun audit before gameplay evidence is read. `TODO` means unqualified, not failed.

## Gate 0 — observation and component contract

| Row | Status | Required evidence |
|---|---|---|
| exact-command-lifecycle-observation | TODO | Mount awake-list membership, reference-exact `TickCommandTurnBased` encounter, stock result, rider turn/status, exact slot/queue, and zero scheduler drive. |
| leased-command-accepted | TODO | Exact KMC origin, rider/mount/turn/generation/slot identities all match. |
| foreign-command-rejected | TODO | Reference-different same-shape command cannot receive a lease. |
| AI-command-rejected | TODO | AI action/origin cannot receive a lease. |
| AoO-command-excluded | TODO | `UnitAttackOfOpportunity` remains stock and unleased. |
| wrong-rider-rejected | TODO | Current turn/reference mismatch rejected. |
| wrong-mount-rejected | TODO | Executor/relationship mismatch rejected. |
| stale-generation-rejected | TODO | Dismount/remount generation invalidates old lease. |
| stale-turn-rejected | TODO | TurnController reference/round change invalidates lease. |
| slot-replacement-rejected | TODO | Replacement is not adopted or driven. |
| start-exactly-once | TODO | Duplicate start request faults/refuses without second native start. |
| tick-at-most-once-per-frame | TODO | Same-frame repeated callback produces one drive. |
| finish-exactly-once | TODO | One terminal result/end transition. |
| interrupt-exactly-once | TODO | Repeated cleanup produces one exact interrupt. |
| cleanup-idempotent | TODO | Repeated cleanup has no mutation/residue. |
| exception-cleanup | TODO | Fault latches reason, exact interrupt/removal, unrelated state unchanged. |
| no-serialized-state | TODO | No serializer-visible scheduler state or save mutation. |
| fallback-disabled-inert | TODO | New gate false produces no registration/admission/drive. |

## Gate 1 — minimal in-range mount primary

These two rows use the same immutable package and suite but fresh game processes.

| Row | Status | Required evidence |
|---|---|---|
| shared-rider-turn-mount-primary-passA | TODO | In-range exact mount Standard command starts within bound, one native action chain, one mount Standard charge, zero rider cost. |
| shared-rider-turn-mount-primary-passB | TODO | Exact independent repeat of pass A. |
| rider-remains-current | TODO | Same rider `CurrentTurn.Unit` before/after every drive, start, act, terminal, and cleanup observation. |
| mount-standard-only | TODO | Mount Standard `0 -> 6` exactly once; rider Standard unchanged. |
| one-chain-cardinality | TODO | One wrapper start, one child/animation action, one attack roll, at most one damage, one terminal result. |
| no-native-mount-turn | TODO | Zero `StartTurn(mount)`/turn-start events/extra tracker entry. |
| exact-turn-completion | TODO | Rider turn stays while command runs and can advance once after eligible pair work is done. |

Do not broaden beyond this gate until pass A and B both pass.

## Gate 2 — sequencing and separate ledgers

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
