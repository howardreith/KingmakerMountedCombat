# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-20 UTC.** Source47 newly passes sustained TB. Its RT Charge test safely rejects Charge but a subsequent legal recovery move is interrupted near its destination. Candidate48 adds a matched mounted/unmounted Horse movement comparison to attribute that failure; no gameplay repair yet. All100 transactions restored actual human state.

Branch `codex/mounted-combat-phase3f-playable-core`; reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, accepted gameplay `ec5d44e6eddc9839d273176b345f7c9701520450`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) is accepted engineering evidence. Configuration: paired activation=true; unified turn/scheduler/overlay=false. Preview.13 feedback retains its scope; no preview.37 or candidate human approval is inferred.

## New native evidence

Latest results by scope follow; they do not automatically qualify48. CP-DU run IDs start `20260920-chunk4-`. Earlier dates, exact hashes and original failures remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md), [resume](../AUTONOMOUS-RESUME.md) and immutable local runtime evidence. Counts are native/outer assertions unless specified.

| Stable scenario / run suffix | Result and scope |
|---|---|
| `chunk4-charge-safety-rt`; DU | Source47,50/2,4 PASS/1 FAIL children. Mounted rider rejection/recovery, unmounted rider Charge, mounted Horse rejection and unrelated Charge PASS. Real queued Mount cannot bypass execution rejection; no Charge motion/cost. Later legal recovery moves2.553m then native `CompleteMovement` interrupts at.472m from the unchanged endpoint/.3m radius. Recovery remains FAIL. |
| `chunk4-sustained-tb`; DT | Source47,52/0,6/0 children across rounds2-7: both actor orders, exhausted rider/partner work, exhausted mount/rider work, early End and fresh successor. Paid Horse Move.2669236 preserves rider6/3; actual arrival1.810m inside native minimum2.210m reach. Full4/4 rider,3/3 Horse, explicit Primaries1/1 and unrelated turns retained. |
| `chunk4-charge-safety-rt/tb`; CP/CQ | Source46,51/0 and5/0 children each. Exact mounted Charge rejects before approach/costs; pure hover, RT actual Pause/queue and TB Preparing/Acting input. Actual unmounted/unrelated Charge travels7.195-7.318m with native costs/no warning. Real queued Mount change cannot bypass execution guard; preceding costs retained. Legal recovery moves, TB End/unrelated turns PASS. CP newly exercises corrected3-20m fixture bound. |
| `chunk4-sustained-melee-rt`; CR | Source46,50/0,4/0 children. Adjacent/approach held/repeated each completes three full rider and Horse routines,12/9 resolutions, R/M alternation.133/139 clicks; native periods~11.3sec, measured frame step.04sec. No restart, duplicate, rider Move tax, forced D20 or replenishment; costs/Stop preserved. |
| `chunk4-sustained-ranged-rt`; CT | Source46,50/0,4/0. Adjacent held/repeat each3 full rider/2 full Horse,12/6 resolutions,114 clicks, rider periods11.463-11.508sec. Approach each3 native three-bow prefixes of four-entry plans,9/0 resolutions,76 clicks, periods6.009-6.028sec. Native melee-tail termination is not full-plan completion. Projectile/input/cost/Stop checks pass; CR separately proves three Horse routines. |
| `chunk4-obstruction-ranged-rt`; CS | Source46,47/0,1/0.219 blocked frames/7.106sec/65 samples/no drops. New14m ring exercised;16m unexercised. Pure prediction, real footprint/range, Stop and native three-bow recovery retained. |
| `chunk4-sustained-tb`; CU | Source46,49/2,3/1 children FAIL. Rider-first/mount-first/rider-exhausted PASS. Paid native Horse Move.1774126 preserves rider6/3, but ends2.414m away versus rider2.210m reach. Next Horse Full3/3 spends6/3; rider0/0 cannot approach with exhausted Horse. Command interrupts without attack cost; fixture waits for an impossible start. Candidate47 fixes placement; no cap extension. Earlier CA40 passed52/0,6/0 on different geometry. |
| `chunk4-interrupt-melee-rt/ranged-rt`; CB/CC | Source40,52/0 and53/0. Actual Pause/Stop, moving target, retarget in windup/in-flight, native target death before/during delivery; released projectiles and genuine costs survive, legal follow-up works. |
| `chunk4-rider-death-tb`; AI | Source21,47/0. Actual119 damage causes persistent Dead/FinallyDead; live Horse command interrupted, spent rider/grants retained, unrelated turns/exit/zero records. |
| `chunk4-rider-incapacitation-tb`; AK | Source22,47/0. Actual105 damage/Unconscious interrupts live rider command; native recovery to92 wounds, partner costs/grants/unrelated turns retained. |
| `chunk4-mount-death-tb`; AL | Source22,47/0. Actual28 damage/Dead interrupts live rider command, spent mount attack retained. Native nonfinal-death policy recovers to10 wounds after exit; not persistent final mount death. |
| `chunk4-targeting-mount-rt/rider-rt/area-unmounted-rt`; AM/AN/AO | Source22,48/0,49/0,47/0. Native targeted heal/slot and hostile attacks use independent actors/AC; area entry once per eligible actor with separate saves; native Dismount control. |
| `chunk4-inspection-rt`; AQ | Source23,48/0. Both native sheets/selection, resource/position purity and native close button. |
| `chunk4-traversal-core`; AZ | Source29,257/0: door59, doorway62, turns76, party60. Actual blocked door/Stop/return, full1.060606 footprint/.10 synchronization bound, no phase faults. |
| `chunk4-traversal-slope`; BO | Source35,50/0. Actual hub load,11.256m elevation,329 samples/no drops/no phase fault, native collision/footprint retained. |
| `chunk4-area-cleanup`; BG | Source32,47/0. Real same-area reload/unload/loading callbacks and fresh-world cleanup; not cross-area qualification. |
| `chunk4-session-rt/tb`; BN/BM | Source35,49/0 each, three native mount/encounter/dismount cycles. Rider4/4, Horse3/3, actual enemy death/exit; TB varied actor order/costs. Records/private context return to zero; subscriptions3 owners/16 entries, no dropped events. |
| `chunk4-horse-strike-comparison-rt`; BA | Source29,48/0. Three mounted Primaries1/1/1 and unmounted3/3/3;60 actual scene PNGs, camera restored. Seated rider/strike/recovery visible in sampled frames; rear angle limits bite detail. No HUD, physical input or HUMAN PLAY approval. |

## Changes and open behavior

Gameplay repairs are scoped: exact-identity mounted Charge rejection; continuation after native mixed ranged/melee-tail termination; pair-local stale-frame movement position/yaw correction; and RT priority for a waiting eligible mount after rider dispatch. CM44 captured a ready idle Horse passed over for another rider routine; CN45 and CR46 supply new native repair evidence. Native Full/Single/Primary, reach, budgets, conditions, targeting, cancellation and ranged mount eligibility remain authoritative. TB scheduling is unchanged.

Charge is the owner's missing feature, exact blueprint `c78506dd0e14f7c45a599990e4e65038`/native `AbilityCustomCharge`. It is not matched by localized text or a blanket ability guard. **Full mounted Charge remains FEATURE-NOT-PRESENT for Chunk6.** Safe rejection does not implement it.

Fixture repairs preserve native services: durable targets/readiness/AI isolation, retiring leaked D20 overrides, actual death/UI/door/loading/session effects and native queue delivery. Source46 corrects CO45's rider-relative target-bound mismatch, exercised by CP46. Source47 replaces the unverified tangent with existing native-range/occupied-endpoint placement; DT proves actual post-move reach. No range, time/repath, collision, cost or assertion limit is relaxed.

CF41 and DU47 retain the same unexplained native recovery-interruption stack. DU's endpoint equals its requested destination; no other actors are within6m except the carried rider with avoidance disabled. That rules out endpoint displacement and nearby actor crowding in DU, but does not prove a mounted-only defect. Candidate48 registers `chunk4-ground-arrival-rt` with mounted/unmounted leaves: the same Horse walks the recorded route through native controls, actually dismounts, and clears the rider by native movement. Read-only observations add waypoint-plane, steering, path and full-footprint probes. An interrupted leg remains FAIL while the control is collected. No position assignment, readiness reset, refund or Interrupt-as-Success workaround. Native48 evidence is pending.

## Qualification and identities

**COMPONENT:** Candidate48 Release/source22/0 and full checks exit0: components388/0,harness249/0,core375/0,ground61/0,play123/0,outer-play10/0,metadata2/0,extended367/0,obstruction106/0,traversal149/0,outer50/0,Charge460/0. Tests SHA`7e7d2b791fd749e4ae0a99efb61d46bb04b817f492733d29c4361cc3928d330a`. Exact source/package/suite identities are recorded after commit in ACTIVE-RUN and immutable manifests. Parser checks do not qualify native movement.

**ASSEMBLY CONTRACT:** Kingmaker577/0 including eight new read-only steering fields. Retained read-only Wrath24/0, detached30/0 plus observers. Kingmaker SHA`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Proprietary source stays local.

Published source47 `c7bea356e66ef438e6c0f209601b25702afa9ec2`, version`0.1.0-chunk4-preview.47`: private ZIP`65df24222340d3feae334e1dc059dd7dc6386b875738a461025e8438cc6f1686`, manifest`e6617267ea81a43cdcfa8641b0591019019c54ee0d9a131539e88d9a2ddbf957`, DLL`0315574bebc0a11e0c4c0892c7fdff4d1f57d2f193f12d9fcea5d0c9c0f9668b`/MVID`7b399512-4490-4357-95b4-47aa97d468fc`, suite`f2f0fff5a5c87aba5831a0950ae56af5044ed053b6ed3ac093fe2ca942959700`. Source/package/guarded publication passed. New48 registration requires fresh guarded WhatIf purity, package/suite and live admission checks.

**NATIVE INTEGRATION:** Next EX mounted/unmounted ground comparison48, then resolve/attribute arrival failure and run all20 new roots and paired full-round/A05/accepted A10/Horse/Mammoth final regression on the exact candidate/configuration. **HUMAN PLAY:** pending. Unsupported desktop capture leaves HUD/countdown and physical input unavailable; native scene images do not close those checks.

## Restored state and targeted manual checks

All100 independent restoration audits PASS; latest DU2026-09-20T08:11:23.5367530Z/log`986b6496b52dd85e3fbc4db38fc28a91d5c21d6bf998173b8580faa88eefeaf8`. Kingmaker/lock absent at checkpoint. Saves`bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`, fullMods`02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`, Params`dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview37 DLL/cache`20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, campaigns, immutable BASELINE and separate preview13 backup preserved. No permanent deployment, foreign-mod edit, main merge or release.

- Mounted Charge warning/hover/queue, legal follow-up/End and unmounted Charge in both modes.
- Held/repeated attacks, seated motion, Horse mounted/unmounted strike/recovery and native countdown.
- Physical rider/mount selection, inspection, targeted heal/attack and an area effect on both.
- Ordinary encounter per mode, varied actor order/early End, door/narrow route/slope and supported cleanup boundary.

Chunk4 remains incomplete. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Chunk5 persistence/cold load stays a roadmap item requiring its own mission; implementation has not begun.
