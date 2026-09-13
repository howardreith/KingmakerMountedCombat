# Chunk 4: sustained play and core safety

**IN PROGRESS â€” 2026-09-13 UTC.** New core traversal and Horse mechanics pass. The slope fixture rejected native paths before movement; candidate 31 corrects its physical-height selection and awaits native execution. Area/session checks and exact-final combat regression remain open. All 55 temporary native transactions independently restored actual human preview.37 and protected data.

Branch: `codex/mounted-combat-phase3f-playable-core`. Reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e` and qualified gameplay `ec5d44e6eddc9839d273176b345f7c9701520450` retain their accepted scope in the [paired milestone](PAIRED-ACTIVATION-MILESTONE.md). Earlier preview.13 human feedback remains separate; no preview.37 or candidate human approval is inferred. All Chunk 4 measurements require paired activation=true and unified turn/scheduler/overlay=false.

## New native evidence

These are the latest completed runs for each scope, not blanket qualification of candidate 31. Exact source/package identities, original failures and chronological detail remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md) and immutable runtime evidence.

| Stable scenarios / runs | Result and scope |
|---|---|
| `chunk4-charge-safety-rt/tb`; E/F | Each 50/0. A/B reproduced unsafe admission of actual Charge `c78506dd0e14f7c45a599990e4e65038` / `AbilityCustomCharge`. Scoped early rejection, player feedback, queued execution revalidation, unmounted/unrelated controls, subsequent legal work and End passed. Full mounted Charge remains the owner's missing feature for Chunk 6. |
| `chunk4-sustained-melee-rt/ranged-rt/sustained-tb`; G/M/N | Held/repeated input across at least three complete native routines; rider melee/ranged, eligible Horse attacks, TB actor order/exhaustion/early End. Ranged-tail continuation was narrowly repaired. Exact-final clean repetition remains required. |
| `chunk4-interrupt-melee-rt/ranged-rt`; R/T | Pause/Stop, retargeting, moving targets and actual target death before/during delivery. T 52/0 preserved released projectiles and genuine costs. |
| `chunk4-obstruction-ranged-rt`; AT | Preview.24, 47/0 native/outer, 1/0 row. 166 blocked frames/6.529 sec, 60 samples/no drops; Stop costs retained, three-bow recovery plus native fourth melee tail. Both targets released; native combat exit settled after 3.043 sec. |
| `chunk4-rider-death-tb`; AI | Preview.21, 47/0 native/outer. Actual 119 damage caused persistent Dead/FinallyDead; live Horse Primary interrupted, spent rider attack retained, grants 2/2, two unrelated turns, native encounter exit/zero records and restored policy/AI/selection. |
| `chunk4-rider-incapacitation-tb`; AK | Preview.22, 47/0. Actual 105/Unconscious interrupted live rider Primary; spent mount attack/grants/unrelated turns preserved. Native recovery to 92 wounds; equipment readiness settled naturally in 6.229 sec. |
| `chunk4-mount-death-tb`; AL | Preview.22, 47/0. Actual 28/Dead interrupted live rider Primary; spent mount attack/grants/unrelated turns preserved. Native nonfinal-death policy recovered mount to 10 wounds after exit. This is not persistent final mount death. |
| `chunk4-targeting-mount-rt`; AM | Preview.22, 48/0, two rows. Native heal 3â†’0 spent its slot, paused queries stayed pure; two enemy hits against mount AC15 left mount damage 2/rider 0. |
| `chunk4-targeting-rider-rt`; AN | Preview.22, 49/0, three rows. Native heal/slot expenditure/paused purity; one area-entry save per actor, Horse +3/rider +8 vs DC16; round callbacks separate. Hostile attacks used rider AC24. |
| `chunk4-targeting-area-unmounted-rt`; AO | Preview.22, 47/0. Actual Dismount; once-per-actor entry saves on the same native frame, rider +8/roll20 PASS and Horse +3/roll5 FAIL; round callbacks separate. |
| `chunk4-inspection-rt`; AQ | Preview.23, 48/0, two rows. Both native character-sheet bindings, selection and tabs preserved resources/positions; native close-button path closed the owned window. |
| `chunk4-traversal-core`; AZ | Preview.29, 257/0 native and outer: door 59/0, doorway 62/0, turns 76/0, party 60/0. Zero phase/recovery faults under unchanged .10 bounds. Native closing completed; 22 blocked samples cut-ready, terminal state retained, Stop costs preserved and native return Success/home .0481 m. |
| `chunk4-horse-strike-comparison-rt`; BA | Preview.29, 48/0 native/outer, two rows. Three mounted Primaries each 1/1/1; unmounted routine 3/3/3. Sixty real camera PNGs/no errors; opposite yaw removes wall occlusion and restores. Sampled rider remains visibly seated/present and separately visible after dismount. Rear angle limits subtle unmounted bite review. |
| `chunk4-traversal-slope`; BB/BC | Preview.29/.30, each 20/1 native/outer FAIL before movement. BC records all 72 navmesh observations at y=6.5270004272460938 and all 24 rejected path callbacks. Candidate 31 uses native physical ground projection for selection; actual slope remains unqualified. |

## Changes and causes

Gameplay repairs remain narrow: unsupported Charge is rejected by exact native identity; sustained ranged orders retain native continuation after the mixed ranged/melee tail; one pair-local native movement-entry hook fixes the reproduced stale-frame position/yaw reference during final turning. Native attacks, range, conditions, per-actor budgets, projectiles and cancellation remain authoritative. No scheduler redesign was made.

Fixture corrections follow actual failures: retire the parent forced-D20 probe before child cases; respect native death/roster/consumed-slot and equipment-readiness results; invoke the actual UI close path; release both target leases and wait for native encounter exit; frame the existing Horse camera; observe native door closing/graph readiness and preserve exact numeric/terminal samples. Original failed runs remain failures. Death uses actual native damage and life controllers, never assigned final flags or resurrection.

BC shows why raw path height cannot select physical slopes in this area. Public static native `UnitMovementAgentBase.Move` (`060018DD`) returns physics-adjusted ground points without applying them to an actor; the instance overload applies those points during real movement. Candidate 31 reuses the static helper in bounded .25 m steps along otherwise eligible native paths. It preserves existing destinations, endpoint/detour/time limits and all actual-motion/collision/footprint/cost assertions. Ground projections are candidate-selection observations, not traversal proof. Native 31 remains required.

## Qualification and identities

**COMPONENT:** Full 31 PASS (exit 0): components 377/0, harness 248/0, core 375/0, play 106/0, metadata 2/0, extended 277/0, obstruction 106/0, traversal 131/0, outer 45/0, Charge 86/0. Build/source 22/0 PASS. Tests SHA256 `8757fafff3aad28ffa6f9f4e21dd185914662ff9457363ce2349bbd7f11501eb`. Parser envelopes do not certify gameplay.

**ASSEMBLY CONTRACT:** Kingmaker 557/0, read-only Wrath 24/0, detached hook constructions 30/0 plus observers and movement-entry contract 1/0. Exact Kingmaker SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; .NET4.7/C#7.3/Harmony12. Actual Unity construction/execution remains mandatory.

| Latest native-tested package (BC failed slope) | Identity |
|---|---|
| Source 30 | `064272b3804b561341766a73572cb90c2c2a69f9` |
| Private ZIP | `KingmakerMountedCombat-0.1.0-chunk4-preview.30-slope-surface-observation-diagnostic-diagnostic.zip` |
| ZIP / manifest SHA256 | `7bcde9dbcd6cd50a34ddbe40c968c7553d44943117f82766d9374f4c6e217451` / `29f65e0077a279372edecae78a6866ea834001f5676d6c46cd0fe31e73f34085` |
| DLL SHA256 / MVID | `e437e41be342475b46997fb15569984a5be69ed768f53a9e44b09dd6bf657e80` / `f6f89e2d-5482-4c88-a02f-7f0c72b8dd33` |
| Suite30 / WhatIf30 SHA256 | `dd56d59b3212dc568e6f2dd3b678453345b83050d813ffa70b7e71c8171200b4` / `5b7a568bfffc7f96101a043b6f525c8282742d3fe9d1d937d54b3895ba608685` |

Candidate 31 built DLL `4adf7a2d22ee3b71d8bf6181d23dd2ec3738fb656b6c56483850dc56bb9499b5`, MVID `94957da2-1071-4f6d-ae22-2d384df96c31`. Its publication/package/native state is recorded in the append-only package manifest and local `analysis-cache/chunk4-native/ACTIVE-RUN.json` as work continues. WhatIf30 proved no external mutation; its unchanged transaction workflow is reused with fresh exact-package admission and mandatory live checks. No WhatIf31 execution is claimed.

**NATIVE INTEGRATION:** Open gates are actual slope traversal, supported area reload, RT/TB ordinary encounter cycles and bounded cleanup; then exact-final new Charge/sustained/incoming/life/control/traversal cases, paired full-round orders, A05, A10 and relevant Horse/Mammoth controls. Earlier paired activation evidence is accepted baseline, not Chunk 4 completion. Evidence stays outside Git/packages under `runtime-evidence` and per-run restoration receipts under `analysis-cache/chunk4-native`.

**HUMAN PLAY:** Pending. BA has useful actual scene frames and sampled seated-motion observations. Native renderers, animation handles and residuals alone are not visual approval. Desktop capture failed twice with unsupported `SetIsBorderRequired` interface; no replacement automation was built. HUD/countdown feedback, physical input and owner visual acceptance remain pending.

## Restoration and manual checks

All 55 independent native restoration audits PASS; latest BC `2026-09-13T23:23:34.1705940Z`, log SHA256 `6bb66955ea229f74ae323d893c08fdb9aa7fce12734429c693e6093506b5dd8e`, no game/lock. Saves `bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`; full Mods `02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`; Params `dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Human preview.37 DLL/cache remain `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`. Human campaigns, immutable KMC_AUTOMATION_BASELINE and the separate preview.13 backup are preserved. No permanent deployment, foreign-mod change, main merge or public release occurred.

Targeted manual checks, after native qualification and when the owner chooses to playtest:

- Use actual mounted Charge in RT/TB; check clear rejection, untouched queued/hover costs, legal follow-up and End Turn. Confirm ordinary unmounted Charge still works.
- Compare held and repeated ordinary attacks; inspect native countdown/feedback and seated Horse strike/recovery, including the unmounted comparison.
- Physically select/inspect each actor; heal and attack each independently, then observe an area effect on both.
- Play an ordinary encounter in each mode, vary actor order and early End, and check a door, narrow route, slope and cleanup boundary.

Mounted persistence/cold-load debt belongs to Chunk 5; this mission does not implement it. Full Charge, moving casts and feat expansion remain Chunk 6. Mid-combat mounting, cross-round Delay and conservative mode switching remain explicit limitations.