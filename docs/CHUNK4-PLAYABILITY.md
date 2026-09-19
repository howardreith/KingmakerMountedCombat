# Chunk 4: sustained play and core safety

**IN PROGRESS - resumed 2026-09-19 UTC.** Preview.32 has a native/outer area-cleanup PASS (BG, 47/0). Farther slope candidates failed before movement (BF, 20/1). BH's RT session fixture incorrectly waited for TB-only movement bookkeeping; candidate33 corrects that fixture and adds native command/slot/cost observations. Slope, new RT/TB session runs and exact-final regression remain open. All60 completed temporary native transactions restored actual preview.37/data; fresh intake matches those bytes.

Branch: `codex/mounted-combat-phase3f-playable-core`. Reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e` and qualified gameplay `ec5d44e6eddc9839d273176b345f7c9701520450` retain their accepted scope in the [paired milestone](PAIRED-ACTIVATION-MILESTONE.md). Earlier preview.13 human feedback remains separate; no preview.37 or candidate human approval is inferred. All Chunk 4 measurements require paired activation=true and unified turn/scheduler/overlay=false.

## New native evidence

These are the latest completed runs for each scope, not blanket qualification of candidate 32. Exact source/package identities, original failures and chronological detail remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md) and immutable runtime evidence.

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
| `chunk4-traversal-slope`; BB/BC/BD/BF | Each 20/1 native/outer FAIL before movement. BD measured nearby physical paths as level. BF on preview.32 searched 32/22/11 m: nine eligible paths span at most 0.05884 m; fifteen others fail the unchanged planar endpoint tolerance. No actual slope movement was measured. A different fixture location is needed; do not replay these routes or lower thresholds. |
| `chunk4-area-cleanup`; BG | Preview.32, 47/0 native and outer PASS. Actual Game.ReloadArea emitted native unload/loading callbacks; all seven measured records used paired=true and other authorities/overlay=false. Fresh-world checks verified native agents, selection and absence of attachment, private movement and anchor residue. This qualifies same-area reload, not cross-area traversal or physical UI input. BE's original native 47/0 / outer 0/1 FAIL remains unchanged. |
| `chunk4-session-rt`; BH | Preview.32, 46/2 native and outer FAIL. The Horse leaf exceeded its unchanged 30-second deadline at Phase3gControls, progress 2 / cycle 0 / zero ordinary events. No session cycle or final subscription/record bounds were qualified. Inspect this setup failure before another session campaign; no repair or timeout increase was attempted at this stopping point. |

## Changes and causes

Gameplay repairs remain narrow: unsupported Charge is rejected by exact native identity; sustained ranged orders retain native continuation after the mixed ranged/melee tail; one pair-local native movement-entry hook fixes the reproduced stale-frame position/yaw reference during final turning. Native attacks, range, conditions, per-actor budgets, projectiles and cancellation remain authoritative. No scheduler redesign was made.

Fixture corrections follow actual failures: retire the parent forced-D20 probe before child cases; respect native death/roster/consumed-slot and equipment-readiness results; invoke the actual UI close path; release both target leases and wait for native encounter exit; frame the existing Horse camera; observe native door closing/graph readiness and preserve exact numeric/terminal samples. Original failed runs remain failures. Death uses actual native damage and life controllers, never assigned final flags or resurrection.

BC shows why raw path height cannot select physical slopes in this area. Public static native `UnitMovementAgentBase.Move` (`060018DD`) returns physics-adjusted ground points without applying them to an actor; the instance overload applies those points during real movement. Candidate 31 reuses the static helper in bounded .25 m steps along otherwise eligible native paths. It preserves existing destinations, endpoint/detour/time limits and all actual-motion/collision/footprint/cost assertions. Ground projections are candidate-selection observations, not traversal proof. BD then measured these nearby paths as level. Candidate 32 changes only slope search radii to 32/22/11 m, still 24 points inside the existing 33 m endpoint cap; actual traversal and all prior deadlines/sample/cost/collision assertions remain required.

BE exposed a separate evidence-parser defect: PowerShell evaluated an ungrouped -or/-and expression so its intended row-start intake allowance failed. Candidate 32 groups the failed-row-result clause explicitly. The real retained records first fail, then all 8 pass and 7 unpaired measured variants reject; a permanent full-envelope test also rejects incompatible intake flags. BE remains overall FAIL, and no gameplay lifecycle/setting behavior changed.

BH exposed a diagnostic mode mismatch: RT ground admission returns before arming the rider-turn adapter, so its LastGroundMove terminal fields are not produced. Candidate33 waits on the actual native UnitMoveTo and empty slots in both modes, retaining the additional adapter checks in TB. Its parser rejects incomplete/foreign/interrupted movement, occupied slots, rider cost and missing TB retirement; RT's absent TB fields are valid. Focused COMPONENT regression319/0 and Release build pass; new native session evidence remains mandatory. No deadline, native command or gameplay policy changed.

## Qualification and identities

**COMPONENT:** Full 32 PASS (exit 0): components 377/0, harness 249/0, core 375/0, play 106/0, metadata 2/0, extended 277/0, obstruction 106/0, traversal 131/0, outer 45/0, Charge 86/0. Build/source 22/0 PASS. Tests SHA256 `c0d638c426d319fc94fb9e656f1dc5b056bdc427dd4a2db4c98ede8e17ca535b`. Parser envelopes do not certify gameplay.

**ASSEMBLY CONTRACT:** Kingmaker 557/0, read-only Wrath 24/0, detached hook constructions 30/0 plus observers and movement-entry contract 1/0. Exact Kingmaker SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Actual Unity construction/execution remains mandatory.

| Latest native-tested package (BF/BH FAIL, BG PASS) | Identity |
|---|---|
| Source 32 | `751f164817c76a56129b46552ff32224fc1f5776` |
| Private ZIP | `KingmakerMountedCombat-0.1.0-chunk4-preview.32-area-intake-slope-search-diagnostic.zip` |
| ZIP / manifest SHA256 | `e388b909c7e556ab1e65dd57dca95f2bf9370535f71db940552210be1e5226bd` / `012919df8251c0e1f9b982c49582ff18649d3be8b5f305dbe34d22d22fe0da2c` |
| DLL SHA256 / MVID | `d7d131b2b8347f303a6f34a1fe390d7c0d3122abcd1ec9eeb1a925b87fc65203` / `3fa9c75d-4131-4e0b-a824-4445e4428e5b` |
| Suite32 SHA256 | `bc000091e090d911a4b4f58f215f3c9d3e671266b8155c220340aa216a677d7c` |

Package rebuild/source 22/0 and package checks 11/0 passed; guarded publication verified exact source32. This stopping checkpoint changes documentation only and does not rebuild or reinstall that payload. Retained WhatIf30 PASS/no mutation SHA `5b7a568bfffc7f96101a043b6f525c8282742d3fe9d1d937d54b3895ba608685` covers the unchanged transaction/admission/WhatIf workflow; only the pure post-run boundary validator changed in Common.ps1. Fresh exact-package admission and all live guards remain mandatory. No WhatIf32 execution is claimed.

**NATIVE INTEGRATION:** Open gates are actual slope traversal, RT/TB ordinary encounter cycles and bounded cleanup; then exact-final new Charge/sustained/incoming/life/control/traversal cases, paired full-round orders, A05, A10 and relevant Horse/Mammoth controls. BG closes same-area reload on source32; a later code candidate needs its own final qualification. Earlier paired activation evidence is accepted baseline, not Chunk 4 completion. Evidence stays outside Git/packages under `runtime-evidence` and per-run restoration receipts under `analysis-cache/chunk4-native`.

**HUMAN PLAY:** Pending. BA has useful actual scene frames and sampled seated-motion observations. Native renderers, animation handles and residuals alone are not visual approval. Desktop capture failed twice with unsupported `SetIsBorderRequired` interface; no replacement automation was built. HUD/countdown feedback, physical input and owner visual acceptance remain pending.

## Restoration and manual checks

All 60 independent native restoration audits PASS; latest BH `2026-09-14T01:17:30.2647274Z`, log SHA256 `01100e02e70d3e7825143f0c909b2a93b6fc08bd1e8d4001a500c152fd2ac4eb`, no game/lock. Saves `bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`; full Mods `02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`; Params `dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview.37 DLL/cache remain `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`. Human campaigns, immutable KMC_AUTOMATION_BASELINE and the separate preview.13 backup are preserved. No permanent deployment, foreign-mod change, main merge or public release occurred.

Targeted manual checks, after native qualification and when the owner chooses to playtest:

- Use actual mounted Charge in RT/TB; check clear rejection, untouched queued/hover costs, legal follow-up and End Turn. Confirm ordinary unmounted Charge still works.
- Compare held and repeated ordinary attacks; inspect native countdown/feedback and seated Horse strike/recovery, including the unmounted comparison.
- Physically select/inspect each actor; heal and attack each independently, then observe an area effect on both.
- Play an ordinary encounter in each mode, vary actor order and early End, and check a door, narrow route, slope and cleanup boundary.

Mounted persistence/cold-load debt belongs to Chunk 5; this mission does not implement it. Full Charge, moving casts and feat expansion remain Chunk 6. Mid-combat mounting, cross-round Delay and conservative mode switching remain explicit limitations.
