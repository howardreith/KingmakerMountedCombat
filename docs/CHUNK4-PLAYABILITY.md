# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-20 UTC.** CM44 completes three Horse routines in all four melee cases (native50/0,4/0 children), but the outer reader rejects its new schema. Its trace also shows a ready Horse passed over for another full rider routine. Candidate45 repairs that RT dispatch priority and the outer schema admission. Native45 qualification is pending. All90 transactions restored the actual human installation and protected data.

Branch `codex/mounted-combat-phase3f-playable-core`; reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, accepted gameplay `ec5d44e6eddc9839d273176b345f7c9701520450`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) remains accepted engineering evidence. Tested configuration: paired activation=true; unified turn/scheduler/overlay=false. Preview.13 feedback is separate; no preview.37 or candidate human approval is inferred.

## New native evidence

Latest completed results by scope follow. They do not qualify candidate45 automatically. Exact original failures, hashes and chronology remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md), [resume](../AUTONOMOUS-RESUME.md) and immutable `runtime-evidence/<run>` directories. Counts below are native/outer assertions unless stated otherwise.

| Stable scenario / run suffix | Result and scope |
|---|---|
| `chunk4-charge-safety-rt/tb`; CG/CH | Source42,51/0 and5/0 children each. Mounted rider/mount reject exact native Charge before approach/costs; real unmounted/unrelated Charge works. RT actual Pause/queued purity and legal recovery; TB native Preparing/Acting purity, End and unrelated turns. Queued relationship change cannot bypass rejection; genuine earlier Mount movement remains spent. |
| `chunk4-sustained-melee-rt`; CM | Source44 native50/0,4/0 children PASS; outer0/1 FAIL (schema27 omitted from outer allowlist). Three complete Horse routines each; riders4/3/3/3;133/147 repeated clicks, one intent/no duplicates/tax/forced D20. Held case passes over a ready idle Horse for a fourth full rider routine, lengthening one Horse interval from~11.35 to17.363sec. Candidate45 repairs priority and outer admission; original result stays FAIL. |
| `chunk4-sustained-melee-rt`; CL | Source43 FAIL: native14/1, outer0/1; no completed child artifact. Retained log admits3 rider/3 Horse commands on held target,19 rider/2 Horse on repeated target. This is not full-plan or eligibility proof. Extra rider completions renewed the fixture timer until the outer300-second deadline. Candidate44 captures the missing native state earlier. |
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

Candidate44 corrects that fixture timer: only progress toward each required actor quota renews the unchanged30-second leaf deadline. Failure captures both actors' native availability, Horse reach/conditions/commands and current routines/events before cleanup. No gameplay repair is selected from incomplete evidence.

Candidate45 uses that native observation for a small RT policy repair: an existing ordinary intent gives the waiting eligible mount priority after rider dispatch when paired activation is enabled. Native budgets, current range, active-command exclusion and TB order remain authoritative; repeated input preserves the preference, while Stop/retarget clears it. The outer reader now admits schema27 only for its two RT roots. CM44 passes the repaired parser in a separate COMPONENT revalidation; no native45 result is inferred.

## Qualification and identities

**COMPONENT:** Candidate45 Release/source22/0, full checks exit0: components386/0,harness249/0,core375/0,play123/0,outer-play10/0,metadata2/0,extended367/0,obstruction106/0,traversal149/0,outer45/0,Charge460/0. New parser red/green includes two-Horse rejection, native time binding and accelerated cadence rejection. Tests SHA`fe5ff17177313eda12649b1cc45a568de98d447ae42bab39204748b6e6442b8f`. DLL`c49a4e24df38734e9f4dca20c7b4b127479fc537d358643e3a6efccadad5bfdb`/MVID`795b9464-5d97-4ea7-a064-0e1d502d9523`. Exact45 source/package/suite identities are recorded after commit in ACTIVE-RUN and immutable manifests.

**ASSEMBLY CONTRACT:** Kingmaker569/0, read-only Wrath24/0, detached30/0 plus observers. Kingmaker SHA`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Proprietary source remains local-only.

Published source44 `800d26ef3cfef322222a8a3085b266c2eff2710f`, version`0.1.0-chunk4-preview.44`: ZIP`f71500e70d940e08b923d29bfc8d94fd65d5a39f976bcb19de8a07748644c85d`, manifest`b7f169487c3e78346382e448c4209c002e594896c565b108f6349898fb9d02c5`, suite`4e867405ec0dfabc9cc018b90fbc5d821ca5113a9837e43d956c9079634ee9d5`. Source/package/guarded publication passed. WhatIf30 provenance covers unchanged admission/deployment/inventory/WhatIf; Common45 only changes the reviewed post-run schema function. Fresh suite/live guards remain mandatory.

**NATIVE INTEGRATION:** First qualify45 RT partner dispatch through sustained melee, then exact-final Charge RT/TB, other new roots and paired full-round/A05/accepted A10/Horse/Mammoth regression. **HUMAN PLAY:** pending. Unsupported desktop capture leaves HUD/countdown and physical input unavailable; native scene images do not close those checks.

## Restored state and manual checks

All90 restoration audits PASS; latest CM at2026-09-20T05:01:31.1817039Z, log`b5cc4e712dedfad1976ed97ba1c6d5fbdf0b33685eeb11317c85cc4827f1ef10`. Kingmaker/lock absent at checkpoint. Saves`bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`, fullMods`02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`, Params`dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview37 DLL/cache`20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, campaigns, immutable BASELINE and separate preview13 backup preserved. No permanent deployment, foreign-mod edit, main merge or release.

Targeted owner checklist after native qualification:

- Actual mounted Charge rejection, pure hover/queue, legal follow-up/End, unmounted Charge control in RT/TB.
- Held/repeated attacks, seated motion, Horse mounted/unmounted strike/recovery and native countdown.
- Physical selection/inspection/heal/hostile targeting of each actor and an area effect on both.
- An encounter per mode, varied actor order/early End, door/narrow route/slope and supported cleanup boundary.

Chunk4 remains incomplete. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Chunk5 persistence/cold load stays a roadmap item requiring its own mission; implementation has not begun.
