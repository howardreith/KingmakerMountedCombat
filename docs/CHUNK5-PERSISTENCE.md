# Chunk 5: save-scoped persistence and cold-load recovery

Status: IN PROGRESS. Integration branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `8a297fa019ff205f50fd7b911b6f6e03d46fbb1a` and native-qualified Chunk 4 source `429377d707a9976be65639e0c27954d8b4ff3717`. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO.

## Storage and save semantics

The installed native transaction supports an extensionless `kmc-mounted-state` archive member. Unknown `.json` members are interpreted as area files, so that extension is unsuitable. Schema2 extends the original bounded primitive JSON for campaign/area/native actor IDs, supported profile and policy, native current cooldown/reaction debt, game-clock time and owned control bindings. An independent serializer avoids the game's global opt-in JSON contracts. Missing metadata creates no pair; future schema data is preserved rather than migrated implicitly. Schema1 migrates in memory with no invented combat state. Schema2 adds supplemental combat records; native combat qualification and removal remain TODO.

The matching native header-write barrier captures one game-thread snapshot before entity shutdown and worker serialization. Metadata enters the native writer before commit; there is no sidecar or post-finalization archive rewrite. Managed facts/hotbar leases and the serialized AI backing field are temporarily suspended without dismounting the relationship. Scoped cleanup restores live ownership. P01-save-D verifies the actual write, unchanged same-session pair/current debt/controls, normal movement and an ordinary native attack. Worker failures, abandoned iterators, overlapping requests and overwrite/rotation still require P05/P07 implementation and qualification.

## Load semantics

Restoration belongs to the selected archive's actual load enumeration and newly deserialized Player object. Native `PlayerState.PostLoad` precedes publication of `Player.GameId`; preview.7 fixes the failed early campaign check by binding semantic restoration to that new world, then requiring the selected header's campaign/area before presentation. Early actor callbacks restore current native debt; a separate validated attachment path restores the eligible existing actors and owned controls without acquisition, a Mount cast or a new activation.

Duplicate callbacks, canceled ownership and replacement-world bindings have component regressions. P01-load-B now passes20/0 native and outer validation on preview.7: semantic actor restoration2, presentation1, three facts/two slots, no duplicates/Mount cast, usable ordinary movement and rider attack in a fresh process. Native combat state and turn controllers are recreated on load, so P02 must restore participation and legitimate remaining work before a preparation refresh; a visual remount cannot provide that guarantee.

## Isolation and native evidence

The persistence-specific guard admits an exact run-owned lab profile, campaign, native save type, leaf and hash. It rejects outside-root paths, aliases, links, foreign campaigns and ambiguous files. Native enumeration, descriptor, stash and ZIP temporary paths are redirected inside that profile. Old scenarios retain their strict authorization mode. The current P01 mode admits one real Manual destination; quick/auto and rotation are still TODO.

Cold-load input is the actual prior PASS archive, copied after source-run ownership/restoration/hash validation. The runner supplies only archive/header identity, never pair or action state. Only the selected native load's header-counter rewrite is suppressed to preserve source bytes; no suppressed save is counted as a write.

| Native evidence | Result |
|---|---|
| isolation-C, preview.2 | PASS 14/0; real native enumeration/load of the isolated fixture, no writes |
| P01-save-D, preview.6/source `f81749ddf97c4f9cd2eb9402d3df06954d9a89f5` | PASS 23/0 and outer PASS; real Manual write, pair/control/debt retention, movement and ordinary rider attack |
| P01-load-A, same preview.6 in fresh PID 15624 | FAIL 1/1 before attachment; early actor binding rejected the not-yet-published campaign ID |
| P01-load-B, preview.7/source `9da3e0551c4c97f456690776a6465cf9983d4e6e` | PASS20/0 native and outer PASS; fresh PID13092 restored the same pair/controls once and completed ordinary movement/attack |

P01-save-D archive SHA256 `84ffb91c85fe10b0ccacc92befe9a1e4fd2be88e639f197f79bf9bd678c83270`, under `runtime-staging/persistence-20260920-chunk5-P01-save-D/Saved Games/Manual_300_KMC_P01.zks`. Its native player clock matches the metadata snapshot. The save process PID 16336 exited normally; a separate process loaded those archive bytes. No duplicate companion was spawned. Cold-load movement and an ordinary native rider attack pass on preview.7; preview.6 wrote the source under the unchanged schema. P01 must still be rerun end to end on the exact final candidate.

Earlier failures remain in the journal and immutable evidence: save-A exposed global JSON defaults; save-B exposed the diagnostic pointer-mode call; save-C exposed a paused fixture readiness wait. Distinct fixes produced save-D's native PASS. No failed run is relabeled.

## Current candidate and checks

Source `ac7f0dba34460bf5cab3989e4be80a1f73f9c28b`, version `0.1.0-chunk5-preview.7`. DLL SHA256 `1d080296d571c52d1332cfa6b46abe77e7067986572f75feab5692243ecdbbb8`, MVID `76bd6522-303a-46a6-9dbf-bbfc93dc5a42`. Build/source 22/0, components 408/0, exact native archive/write-lease contracts 23/0. Unchanged data 28/0, harness 250/0, owned-copy guards 8/0, profile/settings guards 8/0 and installation registration 6/0 are earlier evidence. These levels do not prove native cold recovery.

Package validation FAIL: the DLL is 4,197,376 bytes, 3,072 bytes over the existing 4 MiB entry cap. The `p01-load-world` ZIP SHA256 `28662023a2805830d74ea704e8f5e98d5256521d85577066b1e59b29e2c99629` is frozen and unqualified. No native run used that failed qualifier; the later cap5 qualifier passed P01-load-B. A tested proposal raises only the DLL cap to 5 MiB, retaining the ZIP/Info.json limits and all allowlists, identity and runtime guards. The owner approved this exact DLL-only change; [AGENTS.md](../AGENTS.md) records the narrow exception. The proposal passed 11 checks and rejected both oversized DLL and Info fixtures offline. The full existing harness passes253/0, including the DLL, Info and compressed-ZIP boundaries against the actual validator; log analysis-cache/chunk5-persistence/harness7-cap5.txt. Gameplay source/DLL are unchanged.

Qualified p01-load-world-cap5 ZIP56561480e811540d7bd3301f64f7f2feef2d475623bc42e02011e089762a16fc / manifestd000ea32440a20ff2d7e83af6f9e0551533ab38ee58e36a7c1930252b3ce4391 contains the exact unchanged preview.7 DLL. Suite8 SHAb922c2c66c76b0982873bd52a345891201ef5bc2d945d8985aba5ea76bdc26b9 admitted fresh intake. P01-load-B passes from save-D. Next is the smallest spent-action/paired-turn cold slice.

Development preview.8 implements schema2 combat metadata and schema1 migration in memory. It captures actor current debt, native AI action-use/cooldown obligations, game-clock timers, roster/initiative/current-turn state, paired activation and movement/step commitments. Native actor PostLoad restores missing state; the first native combat initialization is followed by constructor-only context and participation rebind before preparation/admission. Load housekeeping discards old references without gameplay End/forfeit. The native combat-only save predicate is replaced; other native save predicates and all runtime authorization remain.

Build/source22/0, components412/0, data42/0 and exact installed-assembly/storage contracts28/0 PASS. Current DLL `7ca90b3355deb500d6574f5e66e4dfbec1d00309fd87397095c3eb387bff6550`, MVID `745d9a95-1e1f-42be-935e-e938491483e3`; no preview.8 package/native run. These checks do not establish P02. Next is the smallest real partial-movement save/cold load. The existing diagnostic target's unregistered runtime faction cannot qualify a cold archive; the P02 fixture must use a validated native faction and no cold-process fixture reattachment.

P02-save-A on preview.8/source2712f15031de24c316f5cd7a42d5664c93f6f8ca failed5/1 before movement or saving (native PID16968, stage1 deadline). The native party/target entered combat and the game clock advanced, but no native turn was measured. No archive write occurred. Normal exit, installed human DLL2203a68c... restored, run state restored21:39:41.3798572Z and outer validation completed21:39:47.8743691Z; no game/lock remains. Native factiond75c5993785785d468211d9a1a3c87a6 was resolved without modifying it. Local assembly inspection identifies SettingsRoot.HandleSettingsUpdated0600346B dropping boolean caches; the current hypothesis is that this reset invalidated the pre-load cached TB configuration. Preview.9 reasserts only the declared P02 temporary cache after that native reset, with no persisted settings or gameplay-state write. Add native TB setting/controller/status/roster observations; qualify this hypothesis in P02-save-B. Production persistence semantics are unchanged from the preview.8 attempt.

## Required final qualification

| Scenario | Required behavior | Status |
|---|---|---|
| P01 | Manual write, same-session continuation, exit, cold load, movement/attack | IN PROGRESS |
| P02 | TB remainder, participation and two subsequent paired activations | TODO |
| P03 | Step/conversion/reaction/conditions and exactly-once rounds | TODO |
| P04 | Native RT active-command snapshot and loaded outcome controls | TODO |
| P05 | Manual/quick/auto, rotation, copies/renames, alternating saves | TODO |
| P06 | Legacy/current/invalid/future schema and campaign isolation | TODO |
| P07 | Failed/canceled operations, views/areas, disable/removal | TODO |
| P08 | Final accepted gameplay regression including post-load variants | TODO |

Keep paired activation true and both legacy authorities/overlay false. Current actor debt must remain distinct from historical high-water observations. Full Charge, extra profiles/pairs and new gameplay mechanics are later missions. Every mandatory native row must pass on the final frozen candidate; physical-input/visual review remains separate.

## Human state, removal and manual review

Host DESKTOP-SRJJ623. Actual human preview.54 DLL `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9`, saves/settings/caches/foreign Mods were restored after each completed transaction. Latest full restoration: `2026-09-20T20:19:07.2823584Z`, after normal P01-load-B exit; source archive unchanged. Native process13092 ran20:18:02.0857424-20:18:54.4836970Z; the archive came from an earlier, fully exited process. No game/lock remains. Separate preview.37/preview.13 backups are retained. Fresh actual intake is required before another transaction; historical snapshots are not rollback authority.

Prepare-to-Disable/removal implementation and native qualification are TODO. Permanent custom Horse acquisition dependencies remain separate from transient pair metadata. Arbitrary DLL deletion is not certified. No permanent candidate installation, main merge or public release is authorized.

Manual checklist after engineering qualification, using an authorized disposable save:

1. Mount the existing eligible pair, move/attack, and record remaining actions.
2. Make a real save; verify the pair, controls and remaining actions still work.
3. Quit fully, launch a fresh process, and load that exact save.
4. Verify the same actors, controls and legitimate remainder; advance two paired activations.
5. Save again, then repeat a quick/auto and copied-save round trip.
