# Autonomous blockers

Status: IN PROGRESS

Branch: `codex/mounted-combat-phase2-alpha`

## Intake and authorization disposition

Repository identity, exact ancestry, origin/upstream, mission hash, frozen Phase 1 evidence, installed environment, schema-v2 protected-save authority, package pins, and post-review external restoration are PASS. The user explicitly accepted exact Phase 2A commit `09a63729e0847c540ae7e79e9e3876d005ee9afe` and package SHA-256 `80b0498cd9d1e6d8dd518ffd8d343af56bcc711306492548c221b9559c98cef7`. The manual visual gate is closed and combat Tranches C-F are authorized without intermediate approval. No critical blocker is active.

## Preserved manual-session discrepancy

The sole manual session reached exact `READY`, bound the accepted package/DLL/MVID, remained read-only, and restored Mods, Working, Baseline, all protected saves, locks, sentinels, processes, and live deployment exactly. Contrary to the user's observed terminal PASS, the immutable durable `manual-review-result.json` and orchestration records say `FAIL`: normal exit removed loaded fixture objects roughly four seconds before process termination, and the in-game monitor classified that teardown as a boundary escape. The visual acceptance remains explicit and valid; the terminal result is not relabeled. A bounded, one-way post-READY teardown guard plus deterministic tests repair the attributable monitor defect before combat runtime use.

## Protected external state

Exactly one Baseline and one Working exist, with zero KMC near-matches. Baseline is immutable. Only exact `KMC_AUTOMATION_WORKING` may change inside the guarded transaction. Chained authority epoch `20260816T0115379973604Z-user-attested-quicksave-continuity`, SHA-256 `111c794baf8bd1062ef7ecf8c307f4e3badb7a519cf3e66ae1f8f2442b770701`, protects inventory digest `03a91757453a8e875e376f31f1664decb0a6e60dcf6433f2348795a90be634c9` and pin-set digest `6f137d57e3a519b3a315a4c654b5feee51c8495f7104c26bfd4da097930c9ff4`. Its schema-v1 parent remains byte-identical at SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`. `Quick_3.zks` and `Quick_438.zks` remain protected and non-writable. Any later non-Working drift, restoration failure, unexpected process/dialog, Steam/account/update/cloud prompt, or transaction ambiguity is an immediate critical stop.

## Known presentation issues, not blockers

- slight rider seat gap/hover above the Mammoth back;
- stiff analytical pose and no saddle or reins;
- evidence limited to the exact Medium-humanoid/Mammoth/one-handed profile;
- the accepted profile is Mammoth-specific and cannot be generalized;
- contradictory mounted feedback observed during review, now covered by a narrow state-projection repair;
- fixture occlusion, selection silhouette, camera feel, and pointer feel remain private-alpha limitations.

## Exact next action

Stationary mounted rider melee hit is qualified twice in both real-time and turn-based modes. No critical blocker is active. Fresh repaired-package miss Pass A `20260816T130000Z-rider-miss-rt-passA` passes `47/0`; same-package Pass B `20260816T131500Z-rider-miss-rt-passB` is preserved uncredited `FAIL` `20/1`. In B, the new sleepless lease and both awake gates hold exactly, but native combat never begins. Schema v8 did not retain every internal `UnitCombatJoinController` predicate, so no cause is guessed. Observation-only schema v10/v11 now binds live entity/group membership, consciousness, ignored-by-combat, exact enemy-list materialization, fog, and stealth/ambush while preserving v1-v9 and all safety gates. Exact next action is commit/publication/package/WhatIf and one fresh diagnostic process before a predicate-specific repair. Invalid-target, target-death, cleanup, and non-mounted-control remain open. Do not infer qualification, weaken gates, launch Wrath, touch another project, create a public release, or merge `main`.

### Superseding diagnostic checkpoint - 2026-08-16T13:44:00Z

Schema-v10 fresh run `20260816T133000Z-rider-miss-rt-joinprobe` passes `48/0`; same-package B `20260816T134500Z-rider-miss-rt-passB` is uncredited `FAIL` `20/1`. B proves every retained native join gate except target consciousness, but consciousness alone does not distinguish unconscious from dead or identify the cause. Exact installed life-controller inspection shows damage/HP/Constitution/death flags are authoritative. Observation-only schema v12/v13 now binds those values at creation, activation, every frame, and the first native life transition while preserving schemas v1-v11. Complete offline gates pass `21/198/17/166/217`. No critical blocker is active; exact next action is guarded publication/package/WhatIf and one fresh diagnostic run before any target-life mutation or combat-scope advance.

Life probe `20260816T140000Z-rider-miss-rt-lifeprobe` is uncredited `FAIL` `20/1` and resolves the consciousness ambiguity: the disposable target is created and activated Conscious at damage `0`/HP `1`, then receives damage `15` and emits one native `Conscious -> Dead` transition before combat entry. Restoration is independently exact. The damage initiator/source is not yet evidenced, so health/stat mutation remains prohibited. Observation-only schema v14/v15 records first incoming attack/damage identities, weapon/source, amount, and exact pre-dispatch timing; current PASS rejects all pre-dispatch interference and historical v1-v13 remain valid. Gates pass `21/198/17/168/226`. No critical blocker is active; one clean-package incoming-rule probe is required before the narrow provisioning repair.

Schema-v14 A `20260816T143000Z-rider-miss-rt-ruleprobe` passes `49/0`; same-package B `20260816T150000Z-rider-miss-rt-ruleprobeB` is uncredited `FAIL` `20/1` and proves the cause is one ordinary pre-dispatch weapon attack by a third player-faction actor, UUID `d17c8fd0-6627-40c4-bf9a-024cf001c1f7`, for 17 real non-DOT damage. External restoration is independently exact. Player-faction identity does not by itself authorize or scope an AI lease. Observation-only schema v16/v17 now records exact group/player-party/direct-control/effective-and-raw-AI/command-empty context while retaining schemas v1-v15. Gates pass `21/198/17/169/230`. No critical blocker is active; one fresh actor-context probe is required before any behavioral repair.
