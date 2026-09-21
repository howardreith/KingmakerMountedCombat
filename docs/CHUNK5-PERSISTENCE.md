# Chunk 5: save-scoped persistence and cold-load recovery

**IN PROGRESS.** Branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `8a297fa019ff205f50fd7b911b6f6e03d46fbb1a` and Chunk 4 source `429377d707a9976be65639e0c27954d8b4ff3717`. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO. Historical experiments and exact run identities remain in [the journal](../MOUNTED-COMBAT-JOURNAL.md); runtime saves and proprietary evidence stay in the lab.

## Storage and resume semantics

The installed native transaction supports the extensionless archive member `kmc-mounted-state`. Unknown `.json` members are treated as area files, so that suffix is deliberately absent. Schema 2 is bounded primitive JSON: 32 KiB, depth 12, 4096 tokens, explicit properties, finite numbers, enums and unique native IDs. It contains no CLR object construction, command instances, Unity references, delegates or duplicate health/buff state. Schema 1 migrates in memory without invented combat state; missing metadata creates no pair. Future data is retained without rewriting the source archive. Exact native load entry gates now reject unreadable, future or incompatible metadata before world disposal. Broader P06 invalid combat/reference coverage remains unqualified.

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
| P06 | Preview52 ten native/outer cases335/0: legacy/current retry, schema1 migration, missing actors/mismatched profile, malformed/unsupported profile, metadata campaign/policy/future refusals | Native foreign-header campaign, invalid combat participation and exact-final repeat |
| P07 | Owned atomic replacement success/locked-destination failure and cleanup component/installed-service checks | Native failed/canceled write/load, duplicate notifications, area/view changes, disable/re-enable and removal |
| P08 | Accepted Chunk 4 evidence remains historical | Exact-final gameplay regression and post-load variants |

Cold evidence loads the actual owned archive with a fresh PID. The orchestrator copies/hash-checks authorized bytes and compares telemetry after native results; it supplies no missing gameplay state. P05 A/B/A demonstrates state follows the selected archive. Save-root/campaign/type/name/path/hash/ownership guards are run-scoped; old scenarios retain strict Working-only authorization. Native quick/auto test settings use temporary getters without changing saved preferences.

Frozen preview.52 source: 0ff108e31ac39473a541886800b363e2365c1680, published on the integration branch. Private ZIP KingmakerMountedCombat-0.1.0-chunk5-preview.52-p06-live-disposal-diagnostic-diagnostic.zip SHA256 74532dd7299d9181e7377823e5a96c59cb67cfc4b8fe919f418409e6cc1e19e2; manifest 19383ffac5200dd8f7dfc0561737b34c373336c619425a6f4c760cfadc62d933. DLL a64c1d6609e6399630e3636568d5ecde06a9c2b2f9ee217eac5ea9c646d06ab0, MVID 614b8958-c49d-41a1-ad3b-f9cedbf59252. Suite54 SHA256 e55585d8a3eb9875c1c4bd06d5cf6fcc1b8b92ea4025a9858a3c50569ee35554. Source22/components436/contracts92/package11/result-regression7 PASS. Lab p06-52-ledger.json SHA256 985cbc936a0a679146f3d240d4eb13729625c9f9a0542fe1c6a606daf7cc593e binds ten native/outer PASS results,335/0. Exact per-case identities and original failures are in the journal.

The legacy case exposed native LoadGameFromMainMenu disposing views before LoadRoutine. Preview52 releases live mounted leases after admission and before disposal, using no-End/no-forfeit housekeeping; rejected loads leave the original world untouched. Both accepted B/A transitions and three native refusal entries are now measured, followed by usable ordinary movement/attack. P06 metadata campaign mismatch is qualified; a different native-header campaign and damaged combat participation still need their own cases.

## Restoration and removal

All ten preview52 P06 processes exited normally and restored their actual intake; final restoration **2026-09-21T10:30:46.1328279Z**. Human preview.54 DLL 2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9, saves/settings/caches/foreign Mods and separate preview.37/preview.13 backups remain preserved. No game process or transaction remains. Earlier exact analytics/profile recoveries and original failures are historical journal evidence. Every next transaction requires fresh current-data checks.

The preview44 preparation timeout raised native LoadGameException and reset the world. Its exact profile drift was recovered through pinned one-run guards; that failure remains FAIL. Graceful owned timeout/failure recovery and Prepare-to-Disable/removal implementation/native qualification remain TODO. Permanent custom Horse/feature dependencies are separate from transient pair metadata. Arbitrary DLL deletion is not certified; valuable campaigns must not be stripped. No permanent candidate deployment, main merge or public release is authorized. The owner approved only the DLL package-entry limit from4 to5 MiB; ZIP/Info limits, allowlists, hashes, dependencies and runtime protections are unchanged.

Manual checklist after engineering qualification, using an authorized disposable save:

1. Mount the existing pair, move/attack, and note remaining actions.
2. Make a real save; verify controls, relationship and remaining actions still work.
3. Quit fully, launch a fresh process, and load that exact save.
4. Verify the same actors and legitimate remainder; advance two paired activations.
5. Save again; repeat quick/auto and a copied/renamed archive round trip.
