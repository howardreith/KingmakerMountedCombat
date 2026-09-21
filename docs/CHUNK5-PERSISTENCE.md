# Chunk 5: save-scoped persistence and cold-load recovery

**IN PROGRESS.** Branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `8a297fa019ff205f50fd7b911b6f6e03d46fbb1a` and Chunk 4 source `429377d707a9976be65639e0c27954d8b4ff3717`. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO. Historical experiments and exact run identities remain in [the journal](../MOUNTED-COMBAT-JOURNAL.md); runtime saves and proprietary evidence stay in the lab.

## Storage and resume semantics

The installed native transaction supports the extensionless archive member `kmc-mounted-state`. Unknown `.json` members are treated as area files, so that suffix is deliberately absent. Schema 2 is bounded primitive JSON: 32 KiB, depth 12, 4096 tokens, explicit properties, finite numbers, enums and unique native IDs. It contains no CLR object construction, command instances, Unity references, delegates or duplicate health/buff state. Schema 1 migrates in memory without invented combat state; missing metadata creates no pair. Future data is retained without rewriting the source archive. Invalid/incompatible native load behavior still needs P06 qualification and fail-closed repairs.

One immutable game-thread snapshot is captured at the verified native header barrier, before entity shutdown and worker serialization. It travels through the native writer and archive commit; no external pair record, filename-keyed sidecar or post-finalization archive edit is used. The verified overwrite branch uses same-directory atomic replacement, retaining the previous complete archive on replacement failure.

Owned facts/hotbar leases and serialized AI ownership are temporarily suspended without voluntary dismount, End or forfeiture. A per-enumerator wait in the native loading queue allows already-started effects to finish while holding new ordinary starts. The game clock continues during that wait. Native reactions and an exact existing held-touch delivery may finish; the unresolved ability and projectile collections must drain. Unstarted approach remains transient intent: position and accrued native cost are saved at the native barrier. Timeout is a failed save, never an empty successful write. Full cancellation/worker recovery remains P07 work.

Schema 2 supplements native actor current action/reaction debt, roster/initiative/current turn, native AI obligations, game-clock timers, paired participation/condition forfeiture and movement/step/conversion commitments. Historical high-water observations are bookkeeping, never current expenditure. Early actor restoration precedes resource preparation/admission; native turn contexts are rebound without replaying Prepare. Presentation/owned controls restore later, once, through a dedicated path that invokes neither Mount nor acquisition. Native buff eligibility recognizes the already prepared partner while retaining native tick times and round effects.

Only one eligible Horse/Mammoth pair is supported. Qualification uses paired activation=true, both legacy authorities=false, overlay=false. Transport adds no rider Move tax. New mid-combat mounting and cross-round Delay remain unsupported; conservative mode conversion remains explicit. Full Charge belongs to Chunk 6; safe rejection does not implement it.

## Native evidence and remaining gates

These are causal engineering checkpoints, **not the consolidated final-candidate suite**. Each successful source/cold entry below also passes its outer validator and restores actual intake. Assertion counts describe different fixtures and are not directly comparable.

| Gate | Evidence to date | Mandatory remainder |
|---|---|---|
| P01 | Preview6 save-D23/0, preview7 cold-B20/0: real Manual archive, full exit, same actors/controls, movement/attack | Exact-final repeat, including Horse |
| P02 | Partial movement9:26/0+20/0; rider spent12:36/0+29/0; partner orders13:34/0+24/0; exhausted13/14:33/0+23/0; pending End15/16:30/0+28/0. Legal remainder/rejected spent work and two later grants measured | Exact-final boundaries |
| P03 | Step16:34/0+28/0; conversion17:46/0+29/0; real round effect20:31/0+24/0; reaction21:45/0+31/0. Condition/split43:25/0+17/0, native harm/forfeit and principal remainder retained, next two true actor preparations | Save during preparation, suspended participation, exact-final suite |
| P04 | Idle RT29, active attack30, projectile32, unmounted approach33, mounted approach/attack34 all save/cold PASS. Casting38: unmounted390/0+370/0, mounted690/0+720/0 | TB active/preparation boundaries, relevant overlapping effects, final comparisons |
| P05 | Manual/quick/auto native writes and overwrite/rotation22/23:44/0 source and20/0 cold each; renamed archive23:20/0. A/B/A25:17/0+45/0, distinct mounted/unmounted saves and another actual save after load | Overlapping/repeated queued requests; exact-final suite |
| P06 | Bounded parser/migration/authorization component checks | Native legacy, malformed/future, invalid actor/profile/campaign/configuration fixtures and safe participation failure |
| P07 | Owned atomic replacement success/locked-destination failure and cleanup component/installed-service checks | Native failed/canceled write/load, duplicate notifications, area/view changes, disable/re-enable and removal |
| P08 | Accepted Chunk 4 evidence remains historical | Exact-final gameplay regression and post-load variants |

Cold evidence loads the actual owned archive with a fresh PID. The orchestrator copies/hash-checks authorized bytes and compares telemetry after native results; it supplies no missing gameplay state. P05 A/B/A demonstrates state follows the selected archive. Save-root/campaign/type/name/path/hash/ownership guards are run-scoped; old scenarios retain strict Working-only authorization. Native quick/auto test settings use temporary getters without changing saved preferences.

Frozen preview.43 source: `9eae8f54d75c15e62f9f86dff533a6baadec8e45`. Private ZIP `KingmakerMountedCombat-0.1.0-chunk5-preview.43-p03-native-ready-turn-diagnostic.zip`, SHA256 `e46895fed29e3aecb132430e83643801fe9d84dada2d6d8f13f40cc4149918e8`; manifest `d38a7d574895782639a1f299d61b83a0d448de4db805f46cc7f8ea492750bfa7`. DLL `8a7da1c9bb01672b208ecba348bce463915a7e011593f0fdd3e25d9894b76b37`, MVID `57ffacf2-ed2a-4345-aa58-6e30e63fcb13`. Suite45 SHA256 `d9312bf56beb11ef1b218663fbbc75386a7cf4d441fa05f5b0c3c66be5e15bb8`. Source22/0, components430/0, package11/0; unchanged installed contracts89/0, fixtures157/0, harness259/0 plus parser/parameter checks apply.

Exact43 roots `20260921-chunk5-P03-condition-save-C/load-C` use distinct PIDs15568/16464. Actual manual archive SHA256 `aa8e412fcc9592e08af9c6e5f1bf9471e9b8e174ac4764632000a7d97d213a1a`,811357bytes. Native self-harm14, mount Standard12/Move3/Swift6 and principal Standard0 survive at gameTicks434219240000/grant f8e2d96cf63e4932a04df6a77bc609a7:1. Cold semantics4/presentation0, no Mount, acquisition, duplicate native actor or condition replay. The source-only diagnostic condition/fact was removed before saving without changing harm/debt; this qualifies retained forfeiture and split participation, not an active harmful condition across load. Ordinary rider attack and two later preparations per actor pass.

## Restoration and removal

Both exact43 processes exited normally and restored current saves/settings/caches/foreign Mods; last restoration **2026-09-21T07:27:22.3340802Z**. Actual human preview.54 DLL `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9` was verified afterward, with no game process. Separate preview.37 and preview.13 backups remain intact. Recheck actual intake before every transaction; historical snapshots are never restoration authority over newer data.

Prepare-to-Disable/removal implementation and native qualification remain TODO. Permanent custom Horse/feature dependencies are separate from transient pair metadata. Arbitrary DLL deletion is not certified; valuable campaigns must not be stripped. No permanent candidate deployment, main merge or public release is authorized. The owner approved only the DLL package-entry limit from4 to5 MiB; ZIP/Info limits, allowlists, hashes, dependencies and runtime protections are unchanged.

Manual checklist after engineering qualification, using an authorized disposable save:

1. Mount the existing pair, move/attack, and note remaining actions.
2. Make a real save; verify controls, relationship and remaining actions still work.
3. Quit fully, launch a fresh process, and load that exact save.
4. Verify the same actors and legitimate remainder; advance two paired activations.
5. Save again; repeat quick/auto and a copied/renamed archive round trip.
