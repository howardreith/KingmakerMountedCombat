# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-20 UTC.** Source46 passes new Charge safety, sustained RT combat and obstruction. Its sustained TB fixture fails after three passing cases: a paid step leaves the rider outside reach. Candidate47 repairs fixture placement; native47 and final regression remain pending. All98 transactions restored actual human state.

Branch `codex/mounted-combat-phase3f-playable-core`; reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, accepted gameplay `ec5d44e6eddc9839d273176b345f7c9701520450`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) is accepted engineering evidence. Configuration: paired activation=true; unified turn/scheduler/overlay=false. Preview.13 feedback retains its scope; no preview.37 or candidate human approval is inferred.

## New native evidence

Latest results by scope follow; they do not automatically qualify47. CP-CU run IDs start `20260920-chunk4-`. Earlier dates, exact hashes and original failures remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md), [resume](../AUTONOMOUS-RESUME.md) and immutable local runtime evidence. Counts are native/outer assertions unless specified.

| Stable scenario / run suffix | Result and scope |
|---|---|
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

Fixture repairs preserve native services: durable targets/readiness/AI isolation, retiring leaked D20 overrides, actual death/UI/door/loading/session effects and native queue delivery. Source46 corrects CO45's rider-relative target-bound mismatch, exercised by CP46. Candidate47 replaces the unverified tangent with the existing native-range/occupied-endpoint search and asserts actual post-move range, recording destination/path/outcome. No range, time/repath, collision, cost or assertion limit is relaxed.

CF41 remains an unexplained native recovery interruption after2.429463m, .459m residual versus.3m arrival radius and rider Move0. Later different-route successes do not explain it; additional endpoint/nearby-footprint observations exist from42 onward. No refund or Interrupt-as-Success workaround. Original failures remain immutable.

## Qualification and identities

**COMPONENT:** Candidate47 Release/source22/0, full checks exit0: components388/0,harness249/0,core375/0,play123/0,outer-play10/0,metadata2/0,extended367/0,obstruction106/0,traversal149/0,outer45/0,Charge460/0. Tests SHA`65e446c0d7e0450c99ca3795ef90421a9fddf90415c614fed591216f9cbf8e7b`. New47 native qualification is pending; exact source/package/suite identities are recorded after commit in ACTIVE-RUN and immutable manifests.

**ASSEMBLY CONTRACT:** Kingmaker569/0, read-only Wrath24/0, detached30/0 plus observers. Kingmaker SHA`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Proprietary source stays local.

Published source46 `a886158cccf56df68dd721ba6576aef2ae4f414d`, version`0.1.0-chunk4-preview.46`: private ZIP`9357a7693be241cc26c211427b4e5fc10de93dac2044bb468743949f1bf2a192`, manifest`75bfabe6d6d1fbcf6bba37e5bd20781b698ec66900d47d2e2329d1d6c55d2d25`, DLL`be4c1626094eb76e87c504b350117927481a809d11fcc0866b8a8c9cf273a3d7`/MVID`3c01d462-3bf7-4246-a349-602f153ea950`, suite`c30f450c23785756bf563beaefa9586a5ef23dd3f382e839ccb57e6ed23765af`. Source/package/guarded publication passed. WhatIf30 provenance covers unchanged admission/deployment/inventory/WhatIf; fresh package/suite/live guards remain mandatory.

**NATIVE INTEGRATION:** First DT sustained TB47, then all20 new roots and paired full-round/A05/accepted A10/Horse/Mammoth final regression on the exact candidate/configuration. **HUMAN PLAY:** pending. Unsupported desktop capture leaves HUD/countdown and physical input unavailable; native scene images do not close those checks.

## Restored state and targeted manual checks

All98 independent restoration audits PASS; latest CU2026-09-20T07:24:50.2041982Z/log`0a117b6868e299ff18ccc6eacb153688f5b51d2f2ec62deb47105759bdbffad6`. Kingmaker/lock absent at checkpoint. Saves`bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`, fullMods`02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`, Params`dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview37 DLL/cache`20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, campaigns, immutable BASELINE and separate preview13 backup preserved. No permanent deployment, foreign-mod edit, main merge or release.

- Mounted Charge warning/hover/queue, legal follow-up/End and unmounted Charge in both modes.
- Held/repeated attacks, seated motion, Horse mounted/unmounted strike/recovery and native countdown.
- Physical rider/mount selection, inspection, targeted heal/attack and an area effect on both.
- Ordinary encounter per mode, varied actor order/early End, door/narrow route/slope and supported cleanup boundary.

Chunk4 remains incomplete. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Chunk5 persistence/cold load stays a roadmap item requiring its own mission; implementation has not begun.
