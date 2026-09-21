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
| P05 | Manual/quick/auto native writes and overwrite/rotation22/23:44/0 source and20/0 cold each; renamed archive23:20/0. A/B/A25:17/0+45/0, distinct mounted/unmounted saves and another actual save after load | Overlapping/repeated queued requests; exact-final suite |
| P06 | Bounded parser/migration/authorization component checks | Native legacy, malformed/future, invalid actor/profile/campaign/configuration fixtures and safe participation failure |
| P07 | Owned atomic replacement success/locked-destination failure and cleanup component/installed-service checks | Native failed/canceled write/load, duplicate notifications, area/view changes, disable/re-enable and removal |
| P08 | Accepted Chunk 4 evidence remains historical | Exact-final gameplay regression and post-load variants |

Cold evidence loads the actual owned archive with a fresh PID. The orchestrator copies/hash-checks authorized bytes and compares telemetry after native results; it supplies no missing gameplay state. P05 A/B/A demonstrates state follows the selected archive. Save-root/campaign/type/name/path/hash/ownership guards are run-scoped; old scenarios retain strict Working-only authorization. Native quick/auto test settings use temporary getters without changing saved preferences.

Frozen preview.46 source: 6aa19a32309974a8900c87085f592912b035481c. Private ZIP KingmakerMountedCombat-0.1.0-chunk5-preview.46-p03-suspended-participation-diagnostic.zip, SHA256 7a6b3f6665244de401d299fa35ad6eec8e2144b7cb5318f1601e14599a84fb25; manifest f73edd9c1769e8621b4f802473e1e006717c7a964e5cd81bbbea7c91eb9d4a17. DLL 873f90b59d27167d6dc7cda491035a3d40cf5d07be73817308080123a215433d, MVID 02f4da8d-5267-4821-9c7e-f3d58d7ea469. Suite48 SHA256 171f3b9f14009eaf08981ed1d417c5440a723e8e7672dd519f5222d26aa854c6. Source22/components430/contracts89/fixtures174/harness259/package11 PASS; unchanged profile13 checks apply.

Exact46 roots 20260921-chunk5-P03-suspended-save-A/load-A, PIDs2924/6536, native82/0+66/0 and outer PASS. Real Manual SHA256 46e94612b397bc2d5f60e1ed12b30852239b044ff7e2c577436f8e77172080b2,855962bytes. Saved round2/grant f7e2307c857c4511b60ae11cdec1579c:2 is suspended with its disposed native Delayed boundary and pending order. Same-session and cold resume keep the same grant and unused resources; native clear/round-effect counters change by zero on resume and exactly one in each later activation. Native food healing remains at one delivered round/damage2, then progresses to rounds2/3 and damage1/0. Movement, ordinary attack, unrelated turn order and cross-round Delay rejection pass. Cold binds only saved actors/buffs/order, with no initiative, health or fixture injection. Earlier causal checkpoints remain in the journal.

## Restoration and removal

Both exact45 processes exited normally and restored current saves/settings/caches/foreign Mods; last restoration **2026-09-21T07:56:37.6368956Z**. Actual human preview.54 DLL `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9` was verified afterward, with no game process. Separate preview.37 and preview.13 backups remain intact. Recheck actual intake before every transaction; historical snapshots are never restoration authority over newer data.

The preview44 preparation timeout raised native LoadGameException and reset the world. Its exact profile drift was recovered through pinned one-run guards; that failure remains FAIL. Graceful owned timeout/failure recovery and Prepare-to-Disable/removal implementation/native qualification remain TODO. Permanent custom Horse/feature dependencies are separate from transient pair metadata. Arbitrary DLL deletion is not certified; valuable campaigns must not be stripped. No permanent candidate deployment, main merge or public release is authorized. The owner approved only the DLL package-entry limit from4 to5 MiB; ZIP/Info limits, allowlists, hashes, dependencies and runtime protections are unchanged.

Manual checklist after engineering qualification, using an authorized disposable save:

1. Mount the existing pair, move/attack, and note remaining actions.
2. Make a real save; verify controls, relationship and remaining actions still work.
3. Quit fully, launch a fresh process, and load that exact save.
4. Verify the same actors and legitimate remainder; advance two paired activations.
5. Save again; repeat quick/auto and a copied/renamed archive round trip.
