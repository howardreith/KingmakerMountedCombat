# Chunk 4: sustained play and core safety

**IN PROGRESS — 2026-09-20 UTC.** Source37 completed all five TB Charge cases (BS51/0 native), but outer validation correctly failed because requested Pause was not yet active. Candidate38 waits for actual Pause and verifies a held frame, while retaining the real unpaused Mount delivery transition. Focused native proof and exact-final qualification remain required. All70 completed native transactions independently restored actual preview.37 and protected data.

Branch `codex/mounted-combat-phase3f-playable-core`; reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, accepted gameplay `ec5d44e6eddc9839d273176b345f7c9701520450`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) remains accepted engineering evidence. Preview.13 feedback is separate; no preview.37 or candidate human approval is inferred. Tested configuration is paired activation=true, unified turn/scheduler/overlay=false.

## New native evidence

Latest completed evidence by scope is below. It does not blanket-qualify candidate38. Exact original failures, hashes and chronology are retained in the [journal](../MOUNTED-COMBAT-JOURNAL.md), [resume record](../AUTONOMOUS-RESUME.md) and immutable runtime directories under `runtime-evidence/<run>`.

| Stable scenarios / run suffixes | Result and scope |
|---|---|
| `chunk4-charge-safety-rt`; BP | Source35,51/0 native/outer, five rows PASS under its original protocol. Exact native Charge blueprint `c78506dd0e14f7c45a599990e4e65038` / `AbilityCustomCharge`; mounted rider/mount rejected before motion or expenditure with one clear warning. Genuine unmounted/unrelated Charge, queue purity, execution revalidation and legal follow-up passed. Actual paused-frame proof remains pending; the earlier same-frame Pause request did not establish it. Parent forced-D20 probe absent. |
| `chunk4-charge-safety-tb`; BQ | Source35,50/2 native/outer, four rows PASS/one FAIL. Mounted rider/mount and unmounted/unrelated controls pass. Queued Charge stays unstarted/unacted; Mount is outside native range when an unrelated actor receives the first turn. No queue-promotion/approach trace occurs. This is an open fixture gate, not proved unsafe Charge execution. Original E/F passes retain their earlier scope. |
| `chunk4-charge-safety-tb`; BS | Source37,51/0 native,5/0 child,0/1 outer FAIL. Real Mount cast shell finished Success while its matching non-engaging execution still owned delivery. Charge queued legally while unmounted, same-frame Mount succeeded, execution rejected before approach/costs, legal move/attack/End recovered. Queue callback was unpaused; schema24 required Pause. This failure is retained. |
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


BR36's precondition failure remains FAIL. BS37 supplies the missing lifecycle observation but reveals a different gap: `Game.IsPaused=true` requests a later mode transition. Candidate38 waits for the actual getter on a later frame before hover/click and live-attack queue inputs, then holds another frame and checks frozen game time, all three observed actors' costs/positions and the unchanged live command. Schema25 requires these observations for every direct control and both legal recoveries. The separate state-change queue remains unpaused at real Mount delivery. Legacy schema24 still rejects BS; no result is retroactively passed.
## Qualification and identities

**COMPONENT:** Full35 PASS: components377/0,harness249/0,core375/0,play106/0,metadata2/0,extended367/0,obstruction106/0,traversal149/0,outer45/0,Charge86/0. Candidate36 full checks also PASS (exit0), with Charge130/0 and other counts unchanged; Release/source22/0 PASS. Tests SHA`92c700bbbffe6a94cfa1e6ab1a1b6849981faea9bc6c0f648ddce4688a8a7389`. Package/native checks pending at this checkpoint. Parser envelopes are not gameplay proof.

Candidate37 full checks and package PASS; published source `8695f7784aef4c0f90a70c5e2d350fb383bca9ff`. Private ZIP `KingmakerMountedCombat-0.1.0-chunk4-preview.37-native-mount-delivery-lifetime-diagnostic.zip`, SHA`ef4ce0a3856dac7c903a6377896cf1de06a61da1e9816a1a99f250d54e87e7d0`, manifest`2f045bbd0a059f1ec19aa8f43a73f511f29cf05ff28abbeb2f76a581c31527d6`; DLL`a1c04eb20ac84770327793e164d2e6990f88e6799de05e3d407eb1958bcbcfe0`/MVID`fbf56097-5125-4bc3-afbc-2c5e10e58ad5`. BS does not pass outer qualification. Candidate38 Release/source22/0 and full checks exit0 pass: Charge426/0, Kingmaker569/0, other37 counts unchanged; tests SHA`47bcde125e34c3fadf2b1539522299309b34590b08ecece70f5e8763d92a1bb3`. Package/native execution follow. Its source/package identities are frozen in the immutable manifest and ACTIVE-RUN after commit.

**ASSEMBLY CONTRACT:** Kingmaker561/0, read-only Wrath24/0, detached constructions30/0 plus observer contracts on35. Exact Kingmaker SHA`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Bounded new combat-admission IL remains local-only. New diagnostic hook targets our own method, without a production integration change.

| Package with session/slope/RT Charge PASS | Identity |
|---|---|
| Source35 / version | `398acff6a30f2c9d72b5baeb49c933a49e44f1df` / `0.1.0-chunk4-preview.35` |
| ZIP | `KingmakerMountedCombat-0.1.0-chunk4-preview.35-native-session-actor-orders-diagnostic.zip` |
| ZIP / manifest SHA256 | `bddfba2d3a42c54c70e9297af3cccd2bb8f7b0f4d53680294939df7842513854` / `ad0cbd91e800aa1a37d8c4ed8cdf185a1d8da121134a38d161a357c2e01489de` |
| DLL SHA256 / MVID | `ef456a282eb80144a90e67a07975eee68fb26a3503a4455f52326bcbeb6e0da3` / `af6ee118-082f-4886-80ca-a122b9273cf3` |
| Suite35 SHA256 | `d7b8054321b0aaf07d219aeba9082e740147c65b61d70dd9f988b7e0e688a03c` |

Source35 was guarded-published; build/source22/0 and package11/0 PASS. Candidate36 source/package/suite identities will be recorded in local ACTIVE-RUN and its immutable manifest after commit. Built36 DLL`3b5b995d3bd167875d8af04f209076a68c01b4a6b9105d7bdaf0ddd8672eb02e`, MVID`74d08591-db47-4d25-9f3f-9160ed702e50`. Retained WhatIf30 PASS/no-mutation log`5b7a568bfffc7f96101a043b6f525c8282742d3fe9d1d937d54b3895ba608685` covers unchanged transaction/admission/deployment code;36 Common.ps1 changes only post-run schema dispatch. Fresh package admission and live guards remain mandatory.

**NATIVE INTEGRATION:** First qualify38 Charge TB/RT, then exact-final sustained/interrupt/targeting/death/inspection/traversal/session cases, paired full-round orders, A05, accepted A10 and Horse/Mammoth regression. Prior passes retain their tested scope. **HUMAN PLAY:** pending; desktop capture failed twice with unsupported SetIsBorderRequired. Native scene images exclude HUD and physical input. No replacement automation system was built.

## Restored state and targeted manual checks

All70 independent restoration audits PASS; latest BS at2026-09-20T00:29:07.9166776Z, log`ad0576a327bc2e3b67f17dc18223f3c13642ab8c73d2c26d090bf0b78431d34a`. No game/lock at checkpoint. Saves`bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`; fullMods`02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`; Params`dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview.37 DLL/cache`20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, campaigns, immutable KMC_AUTOMATION_BASELINE and separatepreview.13 backup are preserved. No permanent deployment, foreign-mod edit, main merge or public release.

When the owner chooses to playtest after native qualification:

- Try actual mounted Charge in RT/TB; inspect clear rejection, hover/queue costs, legal follow-up and End. Confirm ordinary unmounted Charge.
- Compare held/repeated ordinary attacks and Horse mounted/unmounted strike/recovery; inspect native countdown and seated motion.
- Physically select/inspect/heal/attack each actor, then observe one area effect on both.
- Play an encounter per mode, vary actor order/early End, and use a door, narrow route, slope and supported cleanup boundary.

Chunk4 remains incomplete. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. After Chunk4, Chunk5 persistence/cold-load work is a roadmap item requiring its own mission; no Chunk5 implementation has begun.
