# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-19 UTC.** After the owner opened Steam, preview.34 passed the three-cycle RT session (BJ) and actual slope traversal (BL). TB session BK exposed a fixture assumption: one TB ordinary order addresses one actor, so the partner needs its own input. Candidate35 corrects the fixture and strengthens its evidence checks; native proof remains pending. All63 completed native transactions independently restored actual preview.37 and protected data. Exact-final qualification and human/physical-input checks remain open.

Branch: `codex/mounted-combat-phase3f-playable-core`. Reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e` and qualified gameplay `ec5d44e6eddc9839d273176b345f7c9701520450` retain their accepted scope in the [paired milestone](PAIRED-ACTIVATION-MILESTONE.md). Earlier preview.13 human feedback remains separate; no preview.37 or candidate human approval is inferred. All Chunk 4 measurements require paired activation=true and unified turn/scheduler/overlay=false.

## New native evidence

These are the latest completed runs for each scope, not blanket qualification of candidate35. Exact source/package identities, original failures and chronological detail remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md) and immutable runtime evidence.

| Stable scenarios / runs | Result and scope |
|---|---|
| `chunk4-charge-safety-rt/tb`; E/F | Each 50/0. A/B reproduced unsafe admission of actual Charge `c78506dd0e14f7c45a599990e4e65038` / `AbilityCustomCharge`. Scoped early rejection, player feedback, queued execution revalidation, unmounted/unrelated controls, subsequent legal work and End passed. Full mounted Charge remains the owner's missing feature for Chunk 6. |
| `chunk4-sustained-melee-rt/ranged-rt/sustained-tb`; G/M/N | Held/repeated input across at least three complete native routines; rider melee/ranged, eligible Horse attacks, TB actor order/exhaustion/early End. Ranged-tail continuation was narrowly repaired. Exact-final clean repetition remains required. |
| `chunk4-interrupt-melee-rt/ranged-rt`; R/T | Pause/Stop, retargeting, moving targets and actual target death before/during delivery. T 52/0 preserved released projectiles and genuine costs. |
| `chunk4-obstruction-ranged-rt`; AT | Preview.24, 47/0 native/outer, 1/0 row. 166 blocked frames/6.529 sec, 60 samples/no drops; Stop costs retained, three-bow recovery plus native fourth melee tail. Both targets released; native combat exit settled after 3.043 sec. |
| `chunk4-rider-death-tb`; AI | Preview.21, 47/0 native/outer. Actual 119 damage caused persistent Dead/FinallyDead; live Horse Primary interrupted, spent rider attack retained, grants 2/2, two unrelated turns, native encounter exit/zero records and restored policy/AI/selection. |
| `chunk4-rider-incapacitation-tb`; AK | Preview.22, 47/0. Actual 105/Unconscious interrupted live rider Primary; spent mount attack/grants/unrelated turns preserved. Native recovery to 92 wounds; equipment readiness settled naturally in 6.229 sec. |
| `chunk4-mount-death-tb`; AL | Preview.22, 47/0. Actual 28/Dead interrupted live rider Primary; spent mount attack/grants/unrelated turns preserved. Native nonfinal-death policy recovered mount to 10 wounds after exit. This is not persistent final mount death. |
| `chunk4-targeting-mount-rt`; AM | Preview.22, 48/0, two rows. Native heal 3 to 0 spent its slot, paused queries stayed pure; two enemy hits against mount AC15 left mount damage 2/rider 0. |
| `chunk4-targeting-rider-rt`; AN | Preview.22, 49/0, three rows. Native heal/slot expenditure/paused purity; one area-entry save per actor, Horse +3/rider +8 vs DC16; round callbacks separate. Hostile attacks used rider AC24. |
| `chunk4-targeting-area-unmounted-rt`; AO | Preview.22, 47/0. Actual Dismount; once-per-actor entry saves on the same native frame, rider +8/roll20 PASS and Horse +3/roll5 FAIL; round callbacks separate. |
| `chunk4-inspection-rt`; AQ | Preview.23, 48/0, two rows. Both native character-sheet bindings, selection and tabs preserved resources/positions; native close-button path closed the owned window. |
| `chunk4-traversal-core`; AZ | Preview.29, 257/0 native and outer: door 59/0, doorway 62/0, turns 76/0, party 60/0. Zero phase/recovery faults under unchanged .10 bounds. Native closing completed; 22 blocked samples cut-ready, terminal state retained, Stop costs preserved and native return Success/home .0481 m. |
| `chunk4-horse-strike-comparison-rt`; BA | Preview.29, 48/0 native/outer, two rows. Three mounted Primaries each 1/1/1; unmounted routine 3/3/3. Sixty real camera PNGs/no errors; opposite yaw removes wall occlusion and restores. Sampled rider remains visibly seated/present and separately visible after dismount. Rear angle limits subtle unmounted bite review. |
| `chunk4-traversal-slope`; BL | Preview.34, 50/0 native/outer PASS. Exact native hub load preserved campaign/actor identities, with 106 loading and ten stable frames. Actual elevation change11.258069m, 329 samples/no drops, 382 controller updates/328 moving ticks, no phase fault. Native footprint/collision/cost/synchronization checks unchanged. A sampled camera frame shows the mounted pair on the outdoor route; no HUD or human approval. BB/BC/BD/BF remain20/1 failures of the old location. |
| `chunk4-area-cleanup`; BG | Preview.32, 47/0 native and outer PASS. Actual Game.ReloadArea emitted native unload/loading callbacks; all seven measured records used paired=true and other authorities/overlay=false. Fresh-world checks verified native agents, selection and absence of attachment, private movement and anchor residue. This qualifies same-area reload, not cross-area traversal or physical UI input. BE's original native 47/0 / outer 0/1 FAIL remains unchanged. |
| `chunk4-session-rt`; BJ | Preview.34, 49/0 native/outer PASS, three cycles. Native mount/move/ordinary attack/enemy death/combat exit/dismount; rider4/4 and Horse3/3 routines each cycle. Zero actor records/private partner/attachment residue after cleanup; subscriptions remain3 owners/16 entries; 284 ordinary events/no drops. Movement succeeds with riderMove0 and correctly absent TB-only retirement fields. BH remains46/2 FAIL; BI never deployed. |
| `chunk4-session-tb`; BK | Preview.34, 46/2 native/outer FAIL at stage4/cycle0. Rider's native full routine completed4/4 Success and charged6/3; Horse retained0/0, with empty slots/queues. The fixture awaited both routines after only one addressed TB order. Candidate35 orders each actor through settled native selection/prediction in the same activation. No TB cycle PASS is inferred yet. |

## Changes and causes

Gameplay repairs remain narrow: unsupported Charge is rejected by exact native identity; sustained ranged orders retain native continuation after the mixed ranged/melee tail; one pair-local native movement-entry hook fixes the reproduced stale-frame position/yaw reference during final turning. Native attacks, range, conditions, per-actor budgets, projectiles and cancellation remain authoritative. No scheduler redesign was made.

Fixture corrections follow actual failures: retire the parent forced-D20 probe before child cases; respect native death/roster/consumed-slot and equipment-readiness results; invoke the actual UI close path; release both target leases and wait for native encounter exit; frame the existing Horse camera; observe native door closing/graph readiness and preserve exact numeric/terminal samples. Original failed runs remain failures. Death uses actual native damage and life controllers, never assigned final flags or resurrection.

BC shows why raw path height cannot select physical slopes in this area. Public static native `UnitMovementAgentBase.Move` (`060018DD`) returns physics-adjusted ground points without applying them to an actor; the instance overload applies those points during real movement. Candidate 31 reuses the static helper in bounded .25 m steps along otherwise eligible native paths. It preserves existing destinations, endpoint/detour/time limits and all actual-motion/collision/footprint/cost assertions. Ground projections are candidate-selection observations, not traversal proof. BD then measured these nearby paths as level. Candidate 32 changes only slope search radii to 32/22/11 m, still 24 points inside the existing 33 m endpoint cap; actual traversal and all prior deadlines/sample/cost/collision assertions remain required.

BE exposed a separate evidence-parser defect: PowerShell evaluated an ungrouped -or/-and expression so its intended row-start intake allowance failed. Candidate 32 groups the failed-row-result clause explicitly. The real retained records first fail, then all 8 pass and 7 unpaired measured variants reject; a permanent full-envelope test also rejects incompatible intake flags. BE remains overall FAIL, and no gameplay lifecycle/setting behavior changed.

BH exposed a diagnostic mode mismatch: RT ground admission returns before arming the rider-turn adapter, so its LastGroundMove terminal fields are not produced. Candidate33 observes actual native UnitMoveTo completion and empty slots in both modes, retaining the additional adapter checks in TB. BJ now proves that correction natively. No deadline, native command or gameplay policy changed.

Candidate34 addresses the repeated physically level Dwarf_1 failures through native DungeonStartHub loading with AutoSaveMode.None before resolving/snapshotting/mounting the pair. Initial Working verification, unchanged campaign/actor identities, actual loading and ten stable frames are required; failed loading drains before cleanup. No coordinates, navmesh, footprint or thresholds are changed. BL now proves actual traversal under the existing deadlines and .10 synchronization bound.

BK exposed a second diagnostic mode mismatch. Candidate35 keeps the RT path and issues separate ordinary TB orders to settled rider/mount selections, varying first actor by cycle. Both share the original30-second leaf deadline and paired activation. Actual completed routines, command identities, handoff frames, prediction purity and preservation of the partner's genuine costs are recorded and validated. No forced modes, cooldown resets, attack synthesis or production scheduler change. The new missing-order mutation first fails against the old parser; corrected COMPONENT checks and a new native TB run are required.

## Qualification and identities

**COMPONENT:** Full35 PASS (exit0): components377/0, harness249/0, core375/0, play106/0, metadata2/0, extended367/0, obstruction106/0, traversal149/0, outer45/0, Charge86/0. Release build passes; source/package validation follows this checkpoint. Tests SHA256 `7452b49cf25692194bb7f9c08fb962c4e4ac2edc9c50d9786dff9b3b67b0240b`. Parser envelopes do not certify gameplay.

**ASSEMBLY CONTRACT:** Kingmaker 561/0, read-only Wrath 24/0, detached hook constructions 30/0 plus observers and movement-entry contract 1/0. Exact Kingmaker SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Actual Unity construction/execution remains mandatory.

| Latest native-tested package (BJ/BL PASS, BK FAIL) | Identity |
|---|---|
| Source34 | `7ab1ac939a75c4abd296a005fe025605cbf75587` |
| Private ZIP | `KingmakerMountedCombat-0.1.0-chunk4-preview.34-native-slope-location-diagnostic.zip` |
| ZIP / manifest SHA256 | `24b085a77bbb9fb2b6e1c764772c52f35a1960622d1bf18994c53312b2f31965` / `fe9f29698b22cda7adf7dde00783af59ac8e1560315ccb1f9c77fd2c4d48005c` |
| DLL SHA256 / MVID | `1b73d711478fc6cefbf39169b215f8417763b84175bc63b582caceb70758f7bd` / `6667dc56-40a0-499d-beb6-055614b9567c` |
| Suite34 SHA256 | `634c7531fe4b2a3e32a33762de21017e083f7e122e261ec78aa7794f4b40b42c` |

Package34 rebuild/source22/0 and package11/0 passed; guarded publication verified source34. Candidate35 awaits its own package/native qualification. Retained WhatIf30 PASS/no mutation SHA `5b7a568bfffc7f96101a043b6f525c8282742d3fe9d1d937d54b3895ba608685` covers the unchanged transaction/admission/WhatIf workflow. Only pure post-run evidence validation changed. Fresh exact-package admission and all live guards remain mandatory; no new WhatIf execution is claimed.

**NATIVE INTEGRATION:** TB session remains open. Candidate35 needs its focused TB case, then exact-final new Charge/sustained/incoming/life/control/traversal/session cases, paired full-round orders, A05, A10 and relevant Horse/Mammoth controls. BJ/BL qualify their scopes on34; BG qualifies same-area reload on32. Earlier paired evidence is accepted baseline, not Chunk4 completion. Native evidence stays outside Git/packages under `runtime-evidence`; restoration receipts are under `analysis-cache/chunk4-native`.

**HUMAN PLAY:** Pending. BA has useful actual scene frames and sampled seated-motion observations. Native renderers, animation handles and residuals alone are not visual approval. Desktop capture failed twice with unsupported `SetIsBorderRequired` interface; no replacement automation was built. HUD/countdown feedback, physical input and owner visual acceptance remain pending.

## Restoration and manual checks

BI aborted before deployment because Steam was absent; its independent no-change audit passed at `2026-09-19T21:58:34.2074850Z`, log SHA256 `a5fef7d989a267d2517cceeed71271c0dcba6bbab1ef49fbe0e9982bd4db7f66`. It is not a native run. The owner then opened Steam; the existing safety check passed before BJ/BK/BL. No Steam account settings, credentials or guard were changed.

All63 independent native restoration audits PASS; latest BL `2026-09-19T22:44:48.9296325Z`, log SHA256 `002a213495dd966ef0c0b916fd448b1889992bcf8d09428e87c32b1d2d398e65`, no game/lock. Saves `bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`; full Mods `02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`; Params `dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview.37 DLL/cache remain `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`. Human campaigns, immutable KMC_AUTOMATION_BASELINE and the separate preview.13 backup are preserved. No permanent deployment, foreign-mod change, main merge or public release occurred.

Targeted manual checks, after native qualification and when the owner chooses to playtest:

- Use actual mounted Charge in RT/TB; check clear rejection, untouched queued/hover costs, legal follow-up and End Turn. Confirm ordinary unmounted Charge still works.
- Compare held and repeated ordinary attacks; inspect native countdown/feedback and seated Horse strike/recovery, including the unmounted comparison.
- Physically select/inspect each actor; heal and attack each independently, then observe an area effect on both.
- Play an ordinary encounter in each mode, vary actor order and early End, and check a door, narrow route, slope and cleanup boundary.

Mounted persistence/cold-load debt belongs to Chunk 5; this mission does not implement it. Full Charge, moving casts and feat expansion remain Chunk 6. Mid-combat mounting, cross-round Delay and conservative mode switching remain explicit limitations.
