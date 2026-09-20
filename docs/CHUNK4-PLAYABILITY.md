# Chunk 4: sustained play and core safety

**IN PROGRESS â€” 2026-09-20 UTC.** Source42 passes five new native roots. Its sustained melee cases complete three rider routines but only two Horse routines. Candidate43 extends this measurement to three successful native routines from each eligible actor and compares Horse cadence under held/repeated input. No native43 evidence yet. All88 completed runtime transactions restored the actual human installation and protected data.

Branch `codex/mounted-combat-phase3f-playable-core`; reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, accepted gameplay `ec5d44e6eddc9839d273176b345f7c9701520450`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) remains accepted engineering evidence. Tested configuration: paired activation=true; unified turn/scheduler/overlay=false. Preview.13 feedback is separate; no preview.37 or candidate human approval is inferred.

## New native evidence

Latest completed results by scope follow. They do not qualify candidate43 automatically. Exact original failures, hashes and chronology remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md), [resume](../AUTONOMOUS-RESUME.md) and immutable `runtime-evidence/<run>` directories. Counts below are native/outer assertions unless stated otherwise.

| Stable scenario / run suffix | Result and scope |
|---|---|
| `chunk4-charge-safety-rt/tb`; CG/CH | Source42,51/0 and5/0 children each. Mounted rider/mount reject exact native Charge before approach/costs; real unmounted/unrelated Charge works. RT actual Pause/queued purity and legal recovery; TB native Preparing/Acting purity, End and unrelated turns. Queued relationship change cannot bypass rejection; genuine earlier Mount movement remains spent. |
| `chunk4-sustained-melee-rt/ranged-rt`; CJ/CK | Source42,50/0 and4/0 children each under schema21. Adjacent/approach Ã— held/repeated: three rider routines, one intent/no duplicate or windup restart, zero rider transport tax/forced D20. Melee112/126 repeat clicks, rider periods11.346â€“11.366sec; Horse only two complete routines per case. Ranged72/77 clicks, periods6.012â€“6.159sec; adjacent full plans, approach three bows plus native melee-tail termination. Three-Horse coverage remains open. |
| `chunk4-obstruction-ranged-rt`; CI | Source42,47/0,1/0 child.165 blocked frames/6.508sec/60 samples/no drops. Eligible12m point;14/16m search rings unexercised. Real footprint/range, Stop/costs, native three-bow/four-entry recovery and cleanup retained. |
| `chunk4-sustained-tb`; CA | Source40,52/0,6/0 children: rider/mount first, each exhausted while partner can act, early End and fresh activation; separate costs and unrelated turns. |
| `chunk4-interrupt-melee-rt/ranged-rt`; CB/CC | Source40,52/0 and53/0. Actual Pause/Stop, moving target, retarget during windup/in-flight, actual target death before/during delivery. Released projectiles/costs survive; legal follow-up works. |
| `chunk4-rider-death-tb`; AI | Preview21,47/0. Actual119 damage causes persistent Dead/FinallyDead, interrupts live Horse Primary; spent rider/grants preserved, unrelated turns/exit/zero records. |
| `chunk4-rider-incapacitation-tb`; AK | Preview22,47/0. Actual105 damage/Unconscious interrupts live rider Primary; native recovery to92 wounds, partner costs/grants/unrelated turns retained. |
| `chunk4-mount-death-tb`; AL | Preview22,47/0. Actual28 damage/Dead interrupts live rider Primary; spent mount attack retained. Native nonfinal-death policy recovers to10 wounds after exit; not persistent final mount death. |
| `chunk4-targeting-mount-rt/rider-rt/area-unmounted-rt`; AM/AN/AO | Preview22,48/0,49/0,47/0. Native targeted heal/slot and hostile attacks use independent actors/AC; area entry resolves once per eligible actor with separate saves; native Dismount control. |
| `chunk4-inspection-rt`; AQ | Preview23,48/0. Both native sheets/selection, resource/position purity, native close button. |
| `chunk4-traversal-core`; AZ | Preview29,257/0: door59, doorway62, turns76, party60. Actual blocked door/Stop/return; full1.060606 footprint/.10 synchronization bound, no phase faults. |
| `chunk4-traversal-slope`; BO | Source35,50/0. Native hub loading,11.2561932m elevation,329 samples/no drops/no phase fault; collision/footprint retained. |
| `chunk4-area-cleanup`; BG | Preview32,47/0. Actual native reload/unload/loading callbacks, fresh-world selection/agents and no attachment/private-context residue. |
| `chunk4-session-rt/tb`; BN/BM | Source35,49/0 each, three complete native mount/encounter/dismount cycles. Rider4/4, Horse3/3, actual enemy death/exit; TB alternates actor order with genuine costs. Zero records/private context; subscriptions3 owners/16 entries, no dropped events. |
| `chunk4-horse-strike-comparison-rt`; BA | Preview29,48/0. Three mounted Primaries1/1/1 and unmounted3/3/3;60 actual scene PNGs, camera restored. Sampled seated rider/strike/recovery visible; rear angle limits detail. No HUD, physical input or HUMAN PLAY approval. |

## Changes and unresolved behavior

Gameplay fixes remain scoped: exact-identity mounted Charge rejection, continuation after native mixed ranged/melee-tail termination, and the reproduced pair-local stale-frame movement position/yaw correction. Native Full/Single/Primary, weapon reach, budgets, conditions, cancellation and targeting remain authoritative. Full mounted Charge is still the owner's missing feature for Chunk6.

Diagnostic repairs followed actual failures: durable targets/native readiness/AI isolation, retiring a leaked parent D20 override, real death/UI/door/area/session behavior, native Charge queue delivery and mode-appropriate Pause. Global Pause is refused in native TB; RT tests hold actual Pause. Charge identity is `c78506dd0e14f7c45a599990e4e65038`/native `AbilityCustomCharge`, not localized text or all ability commands. Historical failures retain their original status.

CF41 remains an unexplained native recovery interruption: CompleteMovement interrupted after2.429463m with rider Move0, .459m residual versus .3m arrival radius. Source42 records path endpoint/radii/view position/nearby footprints. CG succeeds on different routes and does not explain CF. No refund, accepting Interrupt as Success or RT extension of the TB endpoint bridge was made.

Candidate43 only strengthens sustained measurement: Stop waits for three complete Horse routines as well as three rider routines in melee; schema27 binds mount periods to native starts and compares the same weapon under the existing measured timing tolerance. Legacy21 retains its scope; ranged never forces an out-of-reach mount attack. Targets are not replenished, and native clocks/modes/costs are unchanged.

## Qualification and identities

**COMPONENT:** Candidate43 Release/source22/0, full checks exit0: components377/0,harness249/0,core375/0,play123/0,metadata2/0,extended367/0,obstruction106/0,traversal149/0,outer45/0,Charge460/0. New parser red/green includes two-Horse rejection, native time binding and accelerated cadence rejection. Tests SHA`5a4e75eaec85d3afb641e786d0ff785da4b42fafd3375fafea94a3501eb40ab6`. DLL`1d6f8a196b23ac23b72ae5cb0151ebf1f487f0c92986b4c39a4c0e162e4124c7`/MVID`ec5ce566-4b3e-4a40-9fbd-790a32010680`. Exact43 source/package/suite identities are recorded after commit in ACTIVE-RUN and immutable manifests.

**ASSEMBLY CONTRACT:** Kingmaker569/0, read-only Wrath24/0, detached30/0 plus observers. Kingmaker SHA`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Proprietary source remains local-only.

Published source42 `db22b424d0df01baca5cdf63e563e21af86c3fc3`, version`0.1.0-chunk4-preview.42`: ZIP`8fa06ade38d8085dd3b0e35f0343390722f88306952cc5a21e807e00f8172fda`, manifest`c98ba41a3968ac3cc62affc65a3f83df05bcf85886f59b76571e2d7100e1cd77`, suite`a8a7e6d12ae032d30db607ac7c2e37c39a59f32f6ee1d3d77cfaafb7f1a0de95`. Source/package/guarded publication passed. Retained WhatIf30 covers unchanged transaction/admission/deployment; fresh suite/live guards remain mandatory.

**NATIVE INTEGRATION:** First qualify43 three-Horse sustained melee, then exact-final Charge RT/TB, other new roots and paired full-round/A05/accepted A10/Horse/Mammoth regression. **HUMAN PLAY:** pending. Unsupported desktop capture leaves HUD/countdown and physical input unavailable; native scene images do not close those checks.

## Restored state and manual checks

All88 restoration audits PASS; latest CK at2026-09-20T03:55:45.0486607Z, log`15ce0070b27ea6bd01d213f7c83a120fdeca709ad8891c4dfd930a223383a467`. Kingmaker/lock absent at checkpoint. Saves`bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`, fullMods`02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`, Params`dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview37 DLL/cache`20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, campaigns, immutable BASELINE and separate preview13 backup preserved. No permanent deployment, foreign-mod edit, main merge or release.

Targeted owner checklist after native qualification:

- Actual mounted Charge rejection, pure hover/queue, legal follow-up/End, unmounted Charge control in RT/TB.
- Held/repeated attacks, seated motion, Horse mounted/unmounted strike/recovery and native countdown.
- Physical selection/inspection/heal/hostile targeting of each actor and an area effect on both.
- An encounter per mode, varied actor order/early End, door/narrow route/slope and supported cleanup boundary.

Chunk4 remains incomplete. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Chunk5 persistence/cold load stays a roadmap item requiring its own mission; implementation has not begun.
