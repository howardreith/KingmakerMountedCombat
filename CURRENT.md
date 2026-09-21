# Chunk 5 persistence — 2026-09-21 UTC

IN PROGRESS. [Single report](docs/CHUNK5-PERSISTENCE.md). Branch `codex/mounted-combat-phase3f-playable-core`. Frozen24/source4da5791936ad12e7fc0c8bcd7c8f9432529e1312 produced actual mounted A and voluntarily dismounted B archives (source17/0 PASS). Cold A loaded correctly, then B failed14/1 at saved-control equality: native hotbars retained AbilityData for two removed facts, so B recorded inactive bindings. No pair/action leakage was observed before this assertion.

Preview25 changes only capture of legitimate control bindings: current-world actor, current active owned fact, exact AbilityData/caster and current lease policy. It does not create inactive abilities during restoration. The alternating fixture now records saved/actual slots before comparison. Source22/0 and components422/0 PASS; unchanged24 harness257/0 and owned fixtures63/0 apply. DLL8ecb001287d720c552cbb30fdbba633a53233c25d6b471a3d2ba7329949e8466/MVID6852eb45-b1a9-4603-a35b-3ef7da751445. Next freeze/package25 and rerun source/cold alternating using new saves; preserve24 failure/archives.

Actual human preview54 restored02:25:30.7737439Z; DLL2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9 verified, no game/transaction. Separate37/13 backups, human saves/settings and foreign Mods preserved. Fresh intake/process/data checks remain mandatory. Exact run/package pointers: analysis-cache/chunk5-persistence/ACTIVE.json.

P01/all five P02 causal cold boundaries and P03 step/conversion/round-effect/reaction pass. P05 Manual/Quick/Auto actual creation, two overwrites/rotations each, cold loads and renamed copy pass causally on22/23. None substitutes for final-candidate P01–P08.

Remaining: finish alternating/subsequent save; P03 conditions/split/suspension; P04 RT/active commands/unmounted control; P05/P06/P07 invalid/failure/lifecycle/removal; P08 and consolidated final suite. Paired=true, legacy authorities/overlay=false. Chunk4 engineering accepted; visual/HUD/physical-input and HUMAN PLAY TODO. No permanent installation/main merge/public release.

---
# Owner-approved alpha delivery - 2026-09-20 UTC

**PASS - exact preview.54 installed for the owner's alpha test.** The owner accepted Chunk 4 and authorized committing, merging into default branch `main`, publishing an alpha prerelease and installing it locally. These instructions supersede historical delivery restrictions. The tested source remains `429377d707a9976be65639e0c27954d8b4ff3717`; the qualified ZIP/DLL is unchanged. [Setup and manual checklist](docs/ALPHA-PLAYTEST.md), [qualification](docs/CHUNK4-PLAYABILITY.md).

Guarded installation completed at 2026-09-20T15:53:37.1343622Z. DLL SHA256 `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9`; actual preview.37 DLL/cache retained under `C:/Dev/KingmakerMountedCombatLab/runtime-backups/deployment-management/20260920T1553321732377Z-dff666c373544f848061522b19567b50/KingmakerMountedCombat`. Protected saves, foreign Mods and UMM Params are unchanged. Kingmaker is closed; no runtime transaction remains. Current human installation is now preview.54: future runtime campaigns require a fresh actual intake and must not restore the historical preview.37 campaign snapshot over it. The separate preview.13 backup is preserved.

Paired activation starts off in this unchanged developer build. Before mounting each game process, enable **Enable paired activation prototype (before mounting)** in KMC's UMM panel while dismounted and outside combat; keep both legacy authorities and the overlay off. Native engineering gates PASS; visual/physical-input and HUMAN PLAY checks remain TODO. No Chunk 5 implementation was begun.

Delivery receipts and final merge/release identities belong in `analysis-cache/chunk4-alpha54/ACTIVE.json`. On resumption, first run `git status --short` and read that receipt before changing external state. The existing deployment helper's documentation-only binding now recognizes exact `AGENTS.md`; actual policy tests pass 18/0, package 11/0, and a fresh WhatIf proves five deployment trees and UMM Params unchanged. All binary/source identity, clean-tree, process, exact-target, backup and foreign-mod guards remain in force.

---
# Chunk 4 native qualification - 2026-09-20 UTC

**PASS for native engineering gates; TODO for targeted visual/physical-input and HUMAN PLAY checks.** Tested source `429377d707a9976be65639e0c27954d8b4ff3717`, private `0.1.0-chunk4-preview.54`, on `codex/mounted-combat-phase3f-playable-core`. All 31 new exact-candidate roots and outer validators pass: 20 required Chunk 4 roots, fresh Horse capture, paired full-round/A05/A10 and Horse/Mammoth regression. Required paired configuration was measured. [Single report, identities and manual checklist](docs/CHUNK4-PLAYABILITY.md).

All 142 native transactions plus the separately recorded EX prelaunch attempt restored actual intake. Human preview.37, protected campaigns/BASELINE, settings/caches/foreign Mods and separate preview.13 backup are preserved. No game/transaction remains; Steam is left open. No permanent deployment, main merge or release. Full mounted Charge remains missing for Chunk 6. Chunk 5 persistence is the next roadmap step only; its implementation is not authorized by this mission.

---
# Combined actor-allocation and paired-activation milestone

Status: **PASS for the supported pre-combat pair**. [Implementation, evidence, limits and private checklist](docs/PAIRED-ACTIVATION-MILESTONE.md).

Final `0.1.0-paired-preview.37`, tested source `ec5d44e6eddc9839d273176b345f7c9701520450`: three paired activations plus refresh in both initiative arrangements; supported A01-A09; final A05 with 36 matched preparation samples; accepted A10 with 32 cases / 287 assertions; Mammoth TB with 66 assertions. All ten final native runs and their outer validators PASS. Full final checks PASS. Configuration: paired activation true; both legacy authorities and diagnostic overlay false.

The private package remains the exact tested ZIP, SHA256 `2215156d43679913ee134c563f704cee37ce564ab70dc50d7009781250ee3738`; DLL `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`. This documentation checkpoint does not rebuild the payload. Work remains on `codex/mounted-combat-phase3f-playable-core`, preserving reviewed `45e3d276754257f4513342d5bce7626dd609d252`, Chunk 2 source `c804ba052760063f747cde83265e660916984d72` and legitimate descendants. [Frozen Chunk 2 evidence](docs/CHUNK2-ACTOR-ALLOCATIONS.md).

All 56 campaign transactions independently restored their actual preview.13 intake, protected saves and full Mods tree; the last audit was at 2026-09-08 12:12 UTC. Later read-only reconciliation finds preview.37 already installed by the separately recorded 18:29 UTC deployment, with the exact preview.13 DLL/cache retained in its backup. Current saves and several foreign-mod settings also differ from the campaign intake; preserve this newer external state. UMM parameter bytes still match, and no game or transaction lock remains. This reconciliation performs no runtime transaction or installation. [Accepted preview.13 human play](docs/CHUNK1-HUMAN-PLAY.md) retains its scope.

One pair, Horse/Mammoth, mounted before combat; rider initiative is principal and native actor costs remain separate. Mounted save restoration and cold-load debt rebinding remain later work. Cross-round Delay is visibly rejected; other unqualified transitions and new-candidate human review remain explicit in the report. These limits do not qualify a complete mounted-combat feature.
