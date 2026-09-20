# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-20 UTC.** Source39 passes all five TB Charge cases. RT passes mounted-rider actual Pause/recovery but fails the unmounted fixture's navmesh origin setup before Charge input. Candidate40 chooses an interior setup destination without weakening final assertions. Exact-final qualification remains required. All73 native transactions restored actual human preview.37 and protected data.

Branch `codex/mounted-combat-phase3f-playable-core`; reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, accepted gameplay `ec5d44e6eddc9839d273176b345f7c9701520450`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) remains accepted engineering evidence. Preview.13 feedback is separate; no preview.37 or candidate human approval is inferred. Tested configuration is paired activation=true, unified turn/scheduler/overlay=false.

## New native evidence

Latest completed evidence by scope is below. It does not blanket-qualify candidate40. Exact original failures, hashes and chronology are retained in the [journal](../MOUNTED-COMBAT-JOURNAL.md), [resume record](../AUTONOMOUS-RESUME.md) and immutable runtime directories under `runtime-evidence/<run>`.

| Stable scenarios / run suffixes | Result and scope |
|---|---|
| `chunk4-charge-safety-tb`; BU | Source39,51/0 native/outer,5/0 child PASS. Exact Charge identity; mounted rider/mount reject without motion/cost, one warning. Genuine unmounted rider7.22475m/unrelated7.25476m Charge. Real queued Mount state change rejects before approach; both recoveries permit movement/attack/native End and unrelated turns. Native TB planning/acting inputs are pure; global Pause is not supported by Kingmaker TB. |
| `chunk4-charge-safety-rt`; BV | Source39,47/2 native/outer FAIL,1/1 child. Mounted rider passes real held Pause, pure queued inputs and4/4 ordinary recovery. Before unmounted Charge, native origin walk succeeds but stops .05912m beyond its navmesh projection, failing the .01m bound. No unmounted control or later rows complete. Prior BP35 passed its older protocol; BS/BT failures remain in history. |
| `chunk4-sustained-melee-rt/ranged-rt/sustained-tb`; G/M/N | Held/repeated input across at least three complete native routines; rider melee/ranged, eligible Horse attacks; TB actor order/exhaustion/early End. Native ranged-tail continuation narrowly repaired. Exact-final repetition required. |
| `chunk4-interrupt-melee-rt/ranged-rt`; R/T | Pause/Stop, moving targets, deliberate retarget and actual death before/during delivery. T52/0 preserves in-flight native projectiles and genuine costs. |
| `chunk4-obstruction-ranged-rt`; AT | Preview24,47/0.166 blocked frames/6.529sec/60 samples/no drops; Stop retains costs, three-bow routine and native fourth melee tail recover. Both targets released; native combat exit settles. |
| `chunk4-rider-death-tb`; AI | Preview21,47/0. Actual119 damage produces persistent Dead/FinallyDead. Live Horse Primary interrupts; spent rider attack and grants2/2 retained; two unrelated turns, native exit and zero records. No final flag assignment or resurrection. |
| `chunk4-rider-incapacitation-tb`; AK | Preview22,47/0. Actual105 damage/Unconscious interrupts live rider Primary; mount costs/grants/unrelated turns retained. Native recovery to92 wounds; equipment readiness settles naturally. |
| `chunk4-mount-death-tb`; AL | Preview22,47/0. Actual28 damage/Dead interrupts live rider Primary; spent mount attack retained. Native nonfinal-death policy recovers mount to10 wounds after exit; not persistent final mount death. |
| `chunk4-targeting-mount-rt`; AM | Preview22,48/0. Native heal3→0 spends slot; paused queries pure; two hostile hits use mount AC15 and leave rider untouched. |
| `chunk4-targeting-rider-rt`; AN | Preview22,49/0. Native heal/slot expenditure/pause purity; one area-entry save per actor, Horse+3/rider+8 versusDC16; round callbacks separate. Hostile targeting uses rider AC24. |
| `chunk4-targeting-area-unmounted-rt`; AO | Preview22,47/0. Actual Dismount; once-per-actor entry saves on the same frame, rider roll20 PASS/Horse roll5 FAIL. |
| `chunk4-inspection-rt`; AQ | Preview23,48/0. Native character-sheet bindings and selection for both actors; resources/positions preserved; native close-button path closes the owned window. |
| `chunk4-traversal-core`; AZ | Preview29,257/0: door59/0, doorway62/0, turns76/0, party60/0. Native door closing/blocked route/Stop/return; full1.060606 footprint and .10 synchronization bound retained, no phase/recovery faults. |
| `chunk4-traversal-slope`; BO | Source35,50/0. Real native hub loading preserves campaign/actor identities. Actual11.2561932m elevation,329 samples/no drops,382 controller updates/328 moving ticks; no phase fault. Native footprint/collision/costs unchanged. |
| `chunk4-area-cleanup`; BG | Preview32,47/0. Real same-area Game.ReloadArea/unload/loading callbacks, all seven measured records in paired configuration; fresh-world native agents/selection and no attachment/private movement/anchor residue. |
| `chunk4-session-tb/rt`; BM/BN | Source35,49/0 each, three complete cycles each. Native mount/movement, rider4/4 and Horse3/3 ordinary routines, enemy death/encounter exit/dismount. TB alternates actor order within the same activation with genuine6/3 per-actor costs preserved. Zero actor records/private context/attachment residue; subscriptions remain3 owners/16 entries;578/280 events, no drops. BK's one-input/two-routine fixture failure remains FAIL. |
| `chunk4-horse-strike-comparison-rt`; BA | Preview29,48/0. Three mounted Primaries each1/1/1; unmounted routine3/3/3. Sixty real1280×720 scene PNGs, no capture errors, camera restored. Sampled rider seated/present, Horse strike/recovery and separate dismounted rider visible; rear angle limits subtle bite detail. This is not HUD/physical-input or HUMAN PLAY approval. |

## Changes and causes

Gameplay repairs remain scoped: exact-identity unsupported Charge rejection, native continuation after the mixed ranged/melee tail, and a pair-local movement-entry correction for the reproduced stale-frame position/yaw reference. No scheduler redesign, forced attack mode, new transport tax or range/weapon restriction. Full mounted Charge remains the owner's missing feature for Chunk6; safe rejection does not implement it.

Fixture repairs follow retained failures: native readiness and target lifecycle, retiring the parent D20 override, actual death policy and UI close behavior, native door/graph readiness and physical slope location, and mode-appropriate session inputs. Candidate35's separate TB actor orders now have BM native proof. Original failures and rejected hypotheses remain in history.

BQ exposed native group-memory combat admission before Mount approached. A one-shot diagnostic hook now queues Charge through native `AddToQueueInternal060026B8` immediately before real Mount delivery changes the relationship, with no hostile present during approach. BS proves that callback is unpaused and that the cast shell can finish Success before its non-engaging execution delivers. Real approach costs, native perception/target invalidation, identity, same-frame dispatch, budgets, positions and the30-second deadline remain authoritative. The hook is removed on completion/cleanup; no mid-combat mounting is enabled.


BR36 and BS37 remain failed evidence. BS exposed the difference between requesting and observing Pause. BT38 then exposed the separate native TB policy already recorded by the assembly tests: global Pause is refused during TB combat. Candidate39 requires actual held Pause only in RT. For TB it validates genuine synchronous inputs in Preparing/Acting, including the exact turn principal and unchanged frame, time, actor budgets and positions. Schema26 preserves all pre-approach Charge rejection, queue purity and recovery assertions; legacy24/25 results are unchanged. No native clock, readiness, condition, perception or target invalidation is overridden.

BV39 adds a separate fixture correction: an endpoint alone did not leave room for native stopping near a navmesh edge. Candidate40 probes eight native segments around candidate destinations, using at least the actor's real corpulence and .5m clearance. The real walk must still succeed, move at least .5m and finish within the original .01m origin-projection bound. No production navigation/footprint/collision change, actor positioning, cooldown reset or relaxed assertion.

## Qualification and identities

**COMPONENT:** Candidate40 Release/source22/0 and full checks exit0 PASS: components377/0,harness249/0,core375/0,play106/0,metadata2/0,extended367/0,obstruction106/0,traversal149/0,outer45/0,Charge460/0. Tests SHA`134a13858ed4a30d27f038d50b1f45a66dafa897ca42650c033bdbb8faf3d895`. DLL`c92c7e74061d36ebfea872374b9df544c8141571ba52cb8aebc32d04367ecd32`/MVID`cbe52ce4-01b3-4e8a-9d62-bf88646ff1e6`. Private40 source/package identities are recorded in ACTIVE-RUN and immutable manifest after commit; native results remain pending.

**ASSEMBLY CONTRACT:** Kingmaker569/0, read-only Wrath24/0, detached constructions30/0 plus observers. Exact Kingmaker SHA`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Native TB Pause refusal is an existing tested contract. Bounded native source remains local-only.

Published source39 `2a2bc5a7fb537da791bf49b83c7f990125d2ea1b` / `0.1.0-chunk4-preview.39`: ZIP`9a2b793f0cca87fd717524f894121c9a22a2f635289ee8d1a074ed24310e92b6`, manifest`94ccfbac7d3bc16f5efac5050b4644242c3fecead0722267ccdefb593b138eb7`, suite`9c8f1f9cd5f6758d0505e73c37c50bcf0ff084af23858a2ce359e52df09198be`. Build/tests/package/guarded publication passed. BU passes TB; BV retains its RT failure. Full package names/identities remain in the journal/resume. Retained WhatIf30 PASS/no mutation covers unchanged transaction/admission/deployment;40 launcher/Common are identical to39. Fresh package admission and live guards remain mandatory.

**NATIVE INTEGRATION:** First qualify40 Charge TB/RT, then exact-final sustained/interrupt/targeting/death/inspection/traversal/session cases, paired full-round orders, A05, accepted A10 and Horse/Mammoth regression. Prior passes retain their scope. **HUMAN PLAY:** pending. Two unsupported desktop-capture failures leave HUD/countdown and physical input unavailable; native scene images do not close those checks. No replacement automation.

## Restored state and targeted manual checks

All73 independent restoration audits PASS; latest BV at2026-09-20T01:25:47.4865851Z, log`f08b949b848fdaf031bf2c7c6c1370d09dba8b7769527abeb36507602d381880`. No game/lock at checkpoint. Saves`bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`; fullMods`02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`; Params`dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview.37 DLL/cache`20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, campaigns, immutable KMC_AUTOMATION_BASELINE and separatepreview.13 backup are preserved. No permanent deployment, foreign-mod edit, main merge or public release.

When the owner chooses to playtest after native qualification:

- Try actual mounted Charge in RT/TB; inspect clear rejection, hover/queue costs, legal follow-up and End. Confirm ordinary unmounted Charge.
- Compare held/repeated ordinary attacks and Horse mounted/unmounted strike/recovery; inspect native countdown and seated motion.
- Physically select/inspect/heal/attack each actor, then observe one area effect on both.
- Play an encounter per mode, vary actor order/early End, and use a door, narrow route, slope and supported cleanup boundary.

Chunk4 remains incomplete. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. After Chunk4, Chunk5 persistence/cold-load work is a roadmap item requiring its own mission; no Chunk5 implementation has begun.
