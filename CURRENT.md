# Chunk 5 persistence - 2026-09-20

IN PROGRESS. [Single report](docs/CHUNK5-PERSISTENCE.md). Chunk 4 native engineering accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO.

Preview.9/source `c06098e2917b8cacdd839b85fa5e1fbe375f2e93` passed the first real TB round trip: P02-save-B26/0, fresh-process P02-load-A20/0, both outer PASS. Archive `e65a6d5ca94596c4d7f68e5e6b945a6022ea6a6c50dc6f8f86b6247697787439` carries schema2 actor debt, round/roster, paired identity and movement commitments. Cold load retained mount Move0.17698051 and unused rider Standard/Move, restored four native actor records and the pair/controls once, then ordinary movement/attack and two true paired refreshes passed. No fixture acquisition or Mount replay occurred in the cold process.

Frozen DLL `c22180b317555b81682ae9055e43d5906d3cfe145cc071b1eb35a969fd3c8dc3`, MVID `52ff0010-ff9c-48e0-b34a-1ec87572297f`; p02-config-refresh package ZIP `956ac1aaa394275a70aef8090d5d10c38584ec1e6ebcba408db8bf5929eca869`. Build22/0, package11/0, native contracts30/0; relevant unchanged components412/0, data42/0, harness253/0 and owned-copy guards11/0. The approved DLL-only5 MiB cap changes no other limit or guard.

Development preview.10 adds bounded P02 checkpoint parameters for rider-spent, attack/transport exhaustion, explicit End/pending participation and partner-order boundaries, plus unrelated-turn-order checks. Build22/0, components413/0, native contracts30/0, fixture guards25/0 and full harness254/0 PASS. Production save/restore code is unchanged from9. Next: freeze10, create fresh intake suite11 and run the rider-spent save/cold pair, then the remaining checkpoints. P03-P08 and a consolidated exact-final suite, including P01, remain mandatory. Earlier P01 causal save/load passed across previews6/7; this does not qualify the final candidate. Active exact receipts: `analysis-cache/chunk5-persistence/ACTIVE.json`.

Native PID16172 completed22:01:41.8719489Z; full actual-intake restoration completed22:01:57.2221102Z. Human preview.54 DLL2203a68c.../saves/settings/caches/foreign Mods restored; separate preview.37/preview.13 backups retained. No game/transaction remains. Paired=true; legacy authorities/overlay=false. No permanent installation/main merge/release.

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
