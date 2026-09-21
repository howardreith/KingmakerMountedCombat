# Chunk 5: save-scoped persistence and cold-load recovery

**IN PROGRESS.** Branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `8a297fa019ff205f50fd7b911b6f6e03d46fbb1a` and Chunk 4 source `429377d707a9976be65639e0c27954d8b4ff3717`. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO. Historical experiments and exact run identities remain in [the journal](../MOUNTED-COMBAT-JOURNAL.md); runtime saves and proprietary evidence stay in the lab.

## Storage and resume semantics

The installed native transaction supports the extensionless archive member `kmc-mounted-state`. Unknown `.json` members are treated as area files, so that suffix is deliberately absent. Schema 2 is bounded primitive JSON: 32 KiB, depth 12, 4096 tokens, explicit properties, finite numbers, enums and unique native IDs. It contains no CLR object construction, command instances, Unity references, delegates or duplicate health/buff state. Schema 1 migrates in memory without invented combat state; missing metadata creates no pair. Future data is retained without rewriting the source archive. Invalid/incompatible native load behavior still needs P06 qualification and fail-closed repairs.

One immutable game-thread snapshot is captured at the verified native header barrier, before entity shutdown and worker serialization. It travels through the native writer and archive commit; no external pair record, filename-keyed sidecar or post-finalization archive edit is used. The verified overwrite branch uses same-directory atomic replacement, retaining the previous complete archive on replacement failure.

Owned facts/hotbar leases and serialized AI ownership are temporarily suspended without voluntary dismount, End or forfeiture. A per-enumerator wait in the native loading queue allows already-started effects to finish while holding new ordinary starts. The game clock continues during that wait. Native reactions, exact granted native preparation commands and an existing held-touch delivery may finish; the unresolved ability and projectile collections must drain. Unstarted approach remains transient intent: position and accrued native cost are saved at the native barrier. Timeout is a failed save, never an empty successful write. Full cancellation/worker recovery remains P07 work.

Schema 2 supplements native actor current action/reaction debt, roster/initiative/current turn, native AI obligations, game-clock timers, paired participation/condition forfeiture and movement/step/conversion commitments. Historical high-water observations are bookkeeping, never current expenditure. Early actor restoration precedes resource preparation/admission; native turn contexts are rebound without replaying Prepare. Presentation/owned controls restore later, once, through a dedicated path that invokes neither Mount nor acquisition. Native buff eligibility recognizes the already prepared partner while retaining native tick times and round effects.

Only one eligible Horse/Mammoth pair is supported. Qualification uses paired activation=true, both legacy authorities=false, overlay=false. Transport adds no rider Move tax. New mid-combat mounting and cross-round Delay remain unsupported; conservative mode conversion remains explicit. Full Charge belongs to Chunk 6; safe rejection does not implement it.

## Native evidence and remaining gates

These are causal engineering checkpoints, **not the consolidated final-candidate suite**. Each successful source/cold entry below also passes its outer validator and restores actual intake. Assertion counts describe different fixtures and are not directly comparable.

| Gate | Evidence to date | Mandatory remainder |
|---|---|---|
| P01 | Preview6 save-D23/0, preview7 cold-B20/0: real Manual archive, full exit, same actors/controls, movement/attack | Exact-final repeat, including Horse |
| P02 | Partial movement9:26/0+20/0; rider spent12:36/0+29/0; partner orders13:34/0+24/0; exhausted13/14:33/0+23/0; pending End15/16:30/0+28/0. Legal remainder/rejected spent work and two later grants measured | Exact-final boundaries |
| P03 | Step16:34/0+28/0; conversion17:46/0+29/0; real round effect20:31/0+24/0; reaction21:45/0+31/0. Condition/split43:25/0+17/0, native harm/forfeit and principal remainder retained, next two true actor preparations. Preparation45:28/0+17/0 saves from inside preparation after its command resolves. Suspended Delay46:82/0+66/0 retains same-round grant and native effects | Exact-final suite |
| P04 | Idle RT29, active attack30, projectile32, unmounted approach33, mounted approach/attack34 all save/cold PASS. Casting38: unmounted390/0+370/0, mounted690/0+720/0 | TB active/preparation boundaries, relevant overlapping effects, final comparisons |
| P05 | Manual/quick/auto native writes and overwrite/rotation22/23:44/0 source and20/0 cold each; renamed archive23:20/0. A/B/A25:17/0+45/0, distinct mounted/unmounted saves and another actual save after load. Queued48:34/0+20/0, three requests/actual commits and cold302 | Exact-final suite |
| P06 | Bounded parser/migration/authorization component checks | Native legacy, malformed/future, invalid actor/profile/campaign/configuration fixtures and safe participation failure |
| P07 | Owned atomic replacement success/locked-destination failure and cleanup component/installed-service checks | Native failed/canceled write/load, duplicate notifications, area/view changes, disable/re-enable and removal |
| P08 | Accepted Chunk 4 evidence remains historical | Exact-final gameplay regression and post-load variants |

Cold evidence loads the actual owned archive with a fresh PID. The orchestrator copies/hash-checks authorized bytes and compares telemetry after native results; it supplies no missing gameplay state. P05 A/B/A demonstrates state follows the selected archive. Save-root/campaign/type/name/path/hash/ownership guards are run-scoped; old scenarios retain strict Working-only authorization. Native quick/auto test settings use temporary getters without changing saved preferences.

Frozen preview.48 source: 76c9a2eff727f37c36b304061d9c4393d39f60f5. Private ZIP KingmakerMountedCombat-0.1.0-chunk5-preview.48-p05-queued-request-projection-diagnostic.zip, SHA256 0f45c35a35cb5c040d004b926753c840bfb053a0081bd2d0789f3ba2e11cfa59; manifest 92add4c0f87c8973c385fe6c16115b9bff5d2719852aabce14992080cdb53a59. DLL 3d0794d693f5fc9bb383830e9d97b8e0e39ef5b628faa60c9e705f32c6c6abdd, MVID a50de278-a883-401d-8517-43f8f9044d8e. Suite50 SHA256 812e1724d8dee7b2d4669969f31728ce9440899362235c0581559dccbeb29a14. Source22/components434/contracts89/package11 PASS; unchanged47 fixture181/harness259 and profile13 checks apply.

Exact48 roots 20260921-chunk5-P05-overlap-save-B/load-B, PIDs8816/5924, native34/0+20/0 and outer PASS. Three native SaveGame requests precede all snapshots; callbacks observe snapshot counts1/2/3. Actual Manual300/301/302 archives SHA256 ae7362c893fbc00ebec4f08225344c0e258237bbbd8f9c5dba17700c08683f22, ec98a841623a97090a8ae28ee69e2a2fdecc8703186df7850b204f1b18a785f7, and 0edcb5de5b57e61836f1f7cd01f5aa9e1374dc1b8bf45f069c93f704c25b705e. The cold process selects Manual302 (848578bytes), with no runner state injection. Both processes retain controls and usable ordinary movement/attacks; unauthorized/suppressed writes0. Earlier47 correctly FAILed two request-time filename predictions before writing;48 repairs only unique declared request admission, preserving actual native PrepareSave path/hash/write-lease checks. All original failures remain in the journal/evidence.

## Restoration and removal

Both exact45 processes exited normally and restored current saves/settings/caches/foreign Mods; last restoration **2026-09-21T07:56:37.6368956Z**. Actual human preview.54 DLL `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9` was verified afterward, with no game process. Separate preview.37 and preview.13 backups remain intact. Recheck actual intake before every transaction; historical snapshots are never restoration authority over newer data.

The preview44 preparation timeout raised native LoadGameException and reset the world. Its exact profile drift was recovered through pinned one-run guards; that failure remains FAIL. Graceful owned timeout/failure recovery and Prepare-to-Disable/removal implementation/native qualification remain TODO. Permanent custom Horse/feature dependencies are separate from transient pair metadata. Arbitrary DLL deletion is not certified; valuable campaigns must not be stripped. No permanent candidate deployment, main merge or public release is authorized. The owner approved only the DLL package-entry limit from4 to5 MiB; ZIP/Info limits, allowlists, hashes, dependencies and runtime protections are unchanged.

Manual checklist after engineering qualification, using an authorized disposable save:

1. Mount the existing pair, move/attack, and note remaining actions.
2. Make a real save; verify controls, relationship and remaining actions still work.
3. Quit fully, launch a fresh process, and load that exact save.
4. Verify the same actors and legitimate remainder; advance two paired activations.
5. Save again; repeat quick/auto and a copied/renamed archive round trip.
