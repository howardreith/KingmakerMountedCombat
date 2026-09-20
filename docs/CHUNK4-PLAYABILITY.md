# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-20 UTC.** FB52 completes the matched control: both mounted and unmounted Horse moves interrupt at the same navmesh edge. Both remain FAIL, now without a fixture exception. Charge recovery on full-footprint interior destinations is pending.53 removes unused archive scans from live preflight; fresh WhatIf proof is required. All104 native transactions and EX restored actual human state.

Branch `codex/mounted-combat-phase3f-playable-core`; reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, accepted gameplay `ec5d44e6eddc9839d273176b345f7c9701520450`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) is accepted engineering evidence. Configuration: paired activation=true; unified turn/scheduler/overlay=false. Preview.13 feedback retains its scope; no preview.37 or candidate human approval is inferred.

## New native evidence

Latest results by scope follow; they do not automatically qualify53. CP-DU run IDs start `20260920-chunk4-`. Earlier dates, exact hashes and original failures remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md), [resume](../AUTONOMOUS-RESUME.md) and immutable local runtime evidence. Counts are native/outer assertions unless specified.

| Stable scenario / run suffix | Result and scope |
|---|---|
| `chunk4-ground-arrival-rt`; FB | Source52 investigative FAIL,46/4,0/2 children, exactly two native movement results/no fixture exception. Same Horse and endpoint: mounted2.592m travel/.481m residual; unmounted2.529m/.422m; radius.3m, origins match.047m/.06m. Both native waypoint-plane interruptions at the same full-footprint navmesh edge. Post-Dismount AI reassertion works;86/84 samples/no drops. Attribution is complete; this invalid destination is not a supported-arrival PASS. |
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

FB52 completes the missing unmounted control for CF41/DU47: matched origins, identical native endpoint and full Horse footprint produce waypoint-plane termination outside the.3m arrival radius in both states. Eight-ray probes identify the same navmesh edge. No mounted-only movement repair is justified.52 reasserts the existing fixture AI lease after Dismount and selects interior Charge recovery destinations with the native full-footprint probe. FC/FD must still qualify that legal recovery. Native costs/collision/results remain unchanged; no refund, position assignment or Interrupt-as-Success workaround. Original failures remain in the journal.

The guarded preflight took707.6seconds before FB. Its unused live-run archive scans include41.389GiB/206720 staging files.53 moves the four historical-root inventories into the WhatIf/declined purity branch; current Mods bytes and save snapshots, locks/admission and finally restoration still run normally. WhatIf still compares all five complete trees. Fresh actual WhatIf53 and measured live timing are pending; no cache or assertion relaxation was introduced.
## Qualification and identities

**COMPONENT:** Candidate53 Release/source22/0 and full checks exit0: components388/0,harness250/0,core375/0,ground61/0,play123/0,outer-play10/0,metadata2/0,extended367/0,obstruction106/0,traversal149/0,outer50/0,Charge460/0. Tests SHA`34a1c2d534716854425956e695ddd3ca8bddfc76acd7c30c08f91d5d31a5fd65`. Exact source/package/suite identities are recorded after commit in ACTIVE-RUN and immutable manifests. Parser checks do not qualify native movement.

**ASSEMBLY CONTRACT:** Kingmaker577/0 including eight new read-only steering fields. Retained read-only Wrath24/0, detached30/0 plus observers. Kingmaker SHA`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Proprietary source stays local.

Published source52 `34c85001875a8069fa9675360a861d9455ddc6ed`, version`0.1.0-chunk4-preview.52`: private ZIP`c68c2ba180fed5c6016436ba43fb7dbb2bc883ceb155e36c645cd62b3dd6aea7`, manifest`011ebe3f85b810efc81f038113bf60f022487f926436ee44bad8b55e6a696124`, DLL`50867fc548122d1b5fcd02b0b78d540ef317e076131241514179b43e042cce28`/MVID`9b246579-b6f8-4250-8347-d5cbea95f85e`, suite`b8dd3bb39769c3c2c9f37a7525176d17e464d2844e054a301be26d9100581266`. Package11/0, exact request33/0 and guarded remote identity verified. PowerShell push wrappers returned1 for native stderr progress; both retained logs verify remote34c8500. Fresh53 package/suite, actual WhatIf purity and exact request checks are mandatory before live use. Earlier identities remain in the journal.

**NATIVE INTEGRATION:** Next fresh WhatIf53, FC/FD Charge recovery, then all20 required new roots and paired full-round/A05/accepted A10/Horse/Mammoth final regression on the exact candidate/configuration. **HUMAN PLAY:** pending. Unsupported desktop capture leaves HUD/countdown and physical input unavailable; native scene images do not close those checks.

## Restored state and targeted manual checks

All104 native restoration audits plus EX prelaunch audit PASS; latest FB2026-09-20T10:43:05.2899442Z/log`52c87365f84c06b59250ec42742297eeb7c236d5a217492763bf0ea715a18eab`. Kingmaker/lock absent at checkpoint. Saves`bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`, fullMods`02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`, Params`dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview37 DLL/cache`20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, campaigns, immutable BASELINE and separate preview13 backup preserved. No permanent deployment, foreign-mod edit, main merge or release.

- Mounted Charge warning/hover/queue, legal follow-up/End and unmounted Charge in both modes.
- Held/repeated attacks, seated motion, Horse mounted/unmounted strike/recovery and native countdown.
- Physical rider/mount selection, inspection, targeted heal/attack and an area effect on both.
- Ordinary encounter per mode, varied actor order/early End, door/narrow route/slope and supported cleanup boundary.

Chunk4 remains incomplete. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Chunk5 persistence/cold load stays a roadmap item requiring its own mission; implementation has not begun.
