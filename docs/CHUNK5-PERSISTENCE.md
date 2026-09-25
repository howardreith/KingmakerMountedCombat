# Chunk 5: save-scoped persistence and cold-load recovery

**COMPLETE — every mandatory Chunk 5 behavior PASS natively on one frozen payload (`0.1.0-chunk5-preview.105`; completion gate PASS; see the completion section and `chunk5-ledger.json`); accepted by the owner on 2026-09-25 and delivered as the alpha prerelease `v0.1.0-chunk5-preview.105` from the `main` merge; not installed locally.** Branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `8a297fa019ff205f50fd7b911b6f6e03d46fbb1a` and Chunk 4 source `429377d707a9976be65639e0c27954d8b4ff3717`. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO. Historical experiments and exact run identities remain in [the journal](../MOUNTED-COMBAT-JOURNAL.md); runtime saves and proprietary evidence stay in the lab.

The owner accepted this candidate on 2026-09-25 and authorized the default-branch merge, the push and an alpha prerelease of the exact qualified payload; nothing was rebuilt, and local installation was not requested. Delivery receipts and the final merge/release identities are in the lab (nalysis-cache/chunk5-alpha105/ACTIVE.json). [Alpha setup and focused playtest](ALPHA-PLAYTEST.md).

## Storage and resume semantics

The installed native transaction supports the extensionless archive member `kmc-mounted-state`. Unknown `.json` members are treated as area files, so that suffix is deliberately absent. Schema 2 is bounded primitive JSON: 32 KiB, depth 12, 4096 tokens, explicit properties, finite numbers, enums and unique native IDs. It contains no CLR object construction, command instances, Unity references, delegates or duplicate health/buff state. Schema 1 migrates in memory without invented combat state; missing metadata creates no pair. Future data is retained without rewriting the source archive. Exact native load entry gates now reject unreadable, future or incompatible metadata before world disposal. Preview56 also qualifies missing combat actors and incompatible native AI references: independently valid debt is retained and participation stays blocked until a valid save is loaded. Canceling an unstarted replacement load does not clear the failed world's admission fence.

One immutable game-thread snapshot is captured at the verified native header barrier, before entity shutdown and worker serialization. It travels through the native writer and archive commit; no external pair record, filename-keyed sidecar or post-finalization archive edit is used. The verified overwrite branch uses same-directory atomic replacement, retaining the previous complete archive on replacement failure.

Owned facts/hotbar leases and serialized AI ownership are temporarily suspended without voluntary dismount, End or forfeiture. A per-enumerator wait in the native loading queue allows already-started effects to finish while holding new ordinary starts. The game clock continues during that wait. Native reactions, exact granted native preparation commands and an existing held-touch delivery may finish; the unresolved ability and projectile collections must drain. Unstarted approach remains transient intent: position and accrued native cost are saved at the native barrier. Timeout is a failed save, never an empty successful write. Selected-wait timeout and unstarted native save/load cancellation recover without success callbacks or losing the completed world. Native enumeration can finish before archive commit: completion now waits for its actual worker task and verified completed descriptor. All four serializer tasks drain before native failure cleanup. Preview54 qualifies a failed atomic replacement, unchanged last-good native reload, subsequent write and fresh cold load; arbitrary cancellation during native serialization is not established by that case.

Schema 2 supplements native actor current action/reaction debt, roster/initiative/current turn, native AI obligations, game-clock timers, paired participation/condition forfeiture and movement/step/conversion commitments. Historical high-water observations are bookkeeping, never current expenditure. Early actor restoration precedes resource preparation/admission; native turn contexts are rebound without replaying Prepare. Presentation/owned controls restore later, once, through a dedicated path that invokes neither Mount nor acquisition. Native buff eligibility recognizes the already prepared partner while retaining native tick times and round effects.

Only one eligible Horse/Mammoth pair is supported. Qualification uses paired activation=true, both legacy authorities=false, overlay=false. Transport adds no rider Move tax. New mid-combat mounting and cross-round Delay remain unsupported; conservative mode conversion remains explicit. Full Charge belongs to Chunk 6; safe rejection does not implement it.

## Native evidence and remaining gates

These are causal engineering checkpoints, **not the consolidated final-candidate suite**. Each successful source/cold entry below also passes its outer validator and restores actual intake. Assertion counts describe different fixtures and are not directly comparable.

| Gate | Evidence to date | Mandatory remainder |
|---|---|---|
| P01 | Preview6 save-D23/0, preview7 cold-B20/0: real Manual archive, full exit, same actors/controls, movement/attack | PASS on the completion candidate (`P01-save`, `P01-load`) |
| P02 | Partial movement9:26/0+20/0; rider spent12:36/0+29/0; partner orders13:34/0+24/0; exhausted13/14:33/0+23/0; pending End15/16:30/0+28/0. Legal remainder/rejected spent work and two later grants measured | PASS on the completion candidate: all five checkpoints, save and cold, as their own entries |
| P03 | Step16:34/0+28/0; conversion17:46/0+29/0; real round effect20:31/0+24/0; reaction21:45/0+31/0. Condition/split43:25/0+17/0, native harm/forfeit and principal remainder retained, next two true actor preparations. Preparation45:28/0+17/0 saves from inside preparation after its command resolves. Suspended Delay46:82/0+66/0 retains same-round grant and native effects | PASS on the completion candidate: all seven, save and cold, as their own entries |
| P04 | Idle RT29, active attack30, projectile32, unmounted approach33, mounted approach/attack34 all save/cold PASS. Casting38: unmounted390/0+370/0, mounted690/0+720/0 | PASS on the completion candidate: ten real-time checkpoints, save and cold; the active turn-based, unresolved-preparation and overlapping-effect boundaries are the P02 and P03 entries executed on the same payload (preparing condition, pending reaction, round effect, suspended Delay), not mappings |
| P05 | Manual/quick/auto native writes and overwrite/rotation22/23:44/0 source and20/0 cold each; renamed archive23:20/0. A/B/A25:17/0+45/0, distinct mounted/unmounted saves and another actual save after load. Queued48:34/0+20/0, three requests/actual commits and cold302 | PASS on the completion candidate: manual, quick, auto, queued, alternating, and the renamed archive, save and cold |
| P06 | Preview52 ten native/outer cases335/0: legacy/current retry, schema1 migration, missing actors/mismatched profile, malformed/unsupported profile, metadata campaign/policy/future refusals  Preview56 damaged-combat missing/AI:49/0 each; canceled retry stays fenced, four duplicate callbacks do not replay debt/preparation, valid reload advances two activations. Preview70 post-disposal load failure26/0 plus restart recovery20/0: corrupt area member passes admission and fails after the world is destroyed, nothing is presented, and recovery is restart-only | PASS on the completion candidate: all fourteen refusals and migrations, including the foreign native header against campaign B own archive |
| P07 | Preview53 canceled wait/queued load60/0+cold20/0; real timeout58/0+cold20/0. Same world/debt/controls, unchanged last-good native reload, subsequent actual write and usable play. Preview54 locked replacement59/0+cold20/0: one actual replacement failure, no success callback, previous bytes intact, real retry write. Preview58 same-area reload33/0+cold20/0: measured retained cross-scene views, real post-area write and ordinary continuation. Preview63 cross-area entry34/0+cold20/0 and preview64 cross-area exit34/0+cold20/0 at the campaign's own hub: real transfers, retained views, both authored autosave modes proven distinct at their native barriers, destination writes and cold round trips. Preview69 transition-autosave cold load24/0 each: both authored Auto archives themselves open, in a fresh process, the exact world each captured | PASS on the completion candidate: timeout, cancel-wait, locked replacement, serialization cancel and its output (each with cold), disable/re-enable, campaign B and cold, removal, integration-absent, genuine no-DLL, disable during load, rider and mount death with cold, rider size change with cold, area reload, cross-entry and cross-exit with cold and transition autosaves |
| P08 | Accepted Chunk 4 evidence remains historical; `final98-p08-rt` 54/0 on the completion candidate | PASS on the completion candidate: the real-time loop and the three accepted turn-based controls; `P08-tb` stays the bounded, non-mandatory known issue |

## Chunk 5 completion candidate: one frozen payload, one ledger

Candidate `0.1.0-chunk5-preview.105`, qualifier `chunk5-completion`, source
`471e1df92ee919c92bf42d0bc03ab6c2d2cc4114`. Private ZIP
`KingmakerMountedCombat-0.1.0-chunk5-preview.105-chunk5-completion-diagnostic.zip`
SHA256 `35b7c82808ab8ecf264be0d511f24735c070374ca73b8259e544eee0d6200113`,
manifest `d772d8dded65e54249f77e1a6e10829a938f90483c56041228fb70651b753fa3`,
DLL `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`, MVID
`638259af-9d31-4738-be8a-2784135d4235`, qualification suite
`20260924-chunk5-suite122` SHA256
`5200fee36cfd316def98eee2a5f7208499029b141ef27d4f0f5d2af384a2c9dd`; removal
observer `KmcRemovalObserver-0.1.0-observer.1-chunk5-completion-105-diagnostic.zip`
SHA256 `46a2f3e8017cc9cf2fa51dfe8284351a748ae791d46f6337dbba1e4250e2342c`
from the same commit. Offline gates on that exact source: source 29, components 483, contracts 180, data 56, owned fixtures 620, validation copies 118, harness 262, profile protection 53, package 11, observer package 7, all
FAIL=0. This remains an unqualified private engineering candidate: it is not
installed for the owner, not merged and not released.

The acceptance ledger `chunk5-ledger.json` beside this document holds one entry
per mandatory behavior of `scripts/Test-Chunk5Ledger.ps1`'s fixed list (105
ids) plus the non-mandatory `P08-tb` known issue, generated from the restored
runtime results of the runs named in it; nothing in the ledger is scored by the
generator. The checker has two modes. Record consistency binds every PASS entry
to its run's scenario, case, assertion counts, frozen payload identity, suite
identity, restoration flags and the SHA256 of its evidence rows, a death cold
entry to its source run's recorded life state, and the no-DLL entry to the
observer result and to the `prepare-removal` run whose cleanup archive it
opened (106/0). `-Completion` fails on any mandatory id that is
missing, NOT RUN, BLOCKED, FAIL, or MAPPED / EXCLUDED without the owner's own
recorded decision: CHUNK5 COMPLETION GATE PASS: all 105 mandatory behaviors PASS on 0.1.0-chunk5-preview.105.

| Entry | Gate | Status | Run | Result | Note |
|---|---|---|---|---|---|
| `P01-save` | P01 | **PASS** | `final105-p01-save` | 23/0 (PID 25876) |  |
| `P01-load` | P01 | **PASS** | `final105-p01-load` | 20/0 (PID 15856) |  |
| `P02-save-partial-movement` | P02 | **PASS** | `final105-p02-save-partial-movement` | 31/0 (PID 27760) |  |
| `P02-load-partial-movement` | P02 | **PASS** | `final105-p02-load-partial-movement` | 26/0 (PID 27080) |  |
| `P02-save-rider-spent` | P02 | **PASS** | `final105-p02-save-rider-spent` | 39/0 (PID 26192) |  |
| `P02-load-rider-spent` | P02 | **PASS** | `final105-p02-load-rider-spent` | 30/0 (PID 22164) |  |
| `P02-save-between-partner-orders` | P02 | **PASS** | `final105-p02-save-between-partner-orders` | 34/0 (PID 17580) |  |
| `P02-load-between-partner-orders` | P02 | **PASS** | `final105-p02-load-between-partner-orders` | 24/0 (PID 18064) |  |
| `P02-save-exhausted` | P02 | **PASS** | `final105-p02-save-exhausted` | 34/0 (PID 27104) |  |
| `P02-load-exhausted` | P02 | **PASS** | `final105-p02-load-exhausted` | 23/0 (PID 28420) |  |
| `P02-save-explicit-end` | P02 | **PASS** | `final105-p02-save-explicit-end` | 31/0 (PID 26172) |  |
| `P02-load-explicit-end` | P02 | **PASS** | `final105-p02-load-explicit-end` | 28/0 (PID 16020) |  |
| `P03-save-step` | P03 | **PASS** | `final105-p03-save-step` | 34/0 (PID 28520) |  |
| `P03-load-step` | P03 | **PASS** | `final105-p03-load-step` | 28/0 (PID 14224) |  |
| `P03-save-conversion` | P03 | **PASS** | `final105-p03-save-conversion` | 46/0 (PID 27768) |  |
| `P03-load-conversion` | P03 | **PASS** | `final105-p03-load-conversion` | 29/0 (PID 27476) |  |
| `P03-save-round-effect` | P03 | **PASS** | `final105-p03-save-round-effect` | 30/0 (PID 27976) |  |
| `P03-load-round-effect` | P03 | **PASS** | `final105-p03-load-round-effect` | 24/0 (PID 23976) |  |
| `P03-save-reaction` | P03 | **PASS** | `final105-p03-save-reaction` | 45/0 (PID 25488) |  |
| `P03-load-reaction` | P03 | **PASS** | `final105-p03-load-reaction` | 30/0 (PID 28560) |  |
| `P03-save-condition` | P03 | **PASS** | `final105-p03-save-condition` | 25/0 (PID 25704) |  |
| `P03-load-condition` | P03 | **PASS** | `final105-p03-load-condition` | 17/0 (PID 27220) |  |
| `P03-save-condition-preparing` | P03 | **PASS** | `final105-p03-save-condition-preparing` | 28/0 (PID 23212) |  |
| `P03-load-condition-preparing` | P03 | **PASS** | `final105-p03-load-condition-preparing` | 17/0 (PID 24320) |  |
| `P03-save-suspended` | P03 | **PASS** | `final105-p03-save-suspended` | 83/0 (PID 22020) |  |
| `P03-load-suspended` | P03 | **PASS** | `final105-p03-load-suspended` | 66/0 (PID 22776) |  |
| `P04-save-unmounted-spent` | P04 | **PASS** | `final105-p04-save-unmounted-spent` | 22/0 (PID 20868) |  |
| `P04-load-unmounted-spent` | P04 | **PASS** | `final105-p04-load-unmounted-spent` | 18/0 (PID 28212) |  |
| `P04-save-mounted-spent` | P04 | **PASS** | `final105-p04-save-mounted-spent` | 25/0 (PID 18908) |  |
| `P04-load-mounted-spent` | P04 | **PASS** | `final105-p04-load-mounted-spent` | 18/0 (PID 26048) |  |
| `P04-save-unmounted-attack` | P04 | **PASS** | `final105-p04-save-unmounted-attack` | 24/0 (PID 27500) |  |
| `P04-load-unmounted-attack` | P04 | **PASS** | `final105-p04-load-unmounted-attack` | 18/0 (PID 24888) |  |
| `P04-save-mounted-attack` | P04 | **PASS** | `final105-p04-save-mounted-attack` | 27/0 (PID 28080) |  |
| `P04-load-mounted-attack` | P04 | **PASS** | `final105-p04-load-mounted-attack` | 18/0 (PID 26964) |  |
| `P04-save-unmounted-projectile` | P04 | **PASS** | `final105-p04-save-unmounted-projectile` | 26/0 (PID 19500) |  |
| `P04-load-unmounted-projectile` | P04 | **PASS** | `final105-p04-load-unmounted-projectile` | 19/0 (PID 21264) |  |
| `P04-save-mounted-projectile` | P04 | **PASS** | `final105-p04-save-mounted-projectile` | 29/0 (PID 23740) |  |
| `P04-load-mounted-projectile` | P04 | **PASS** | `final105-p04-load-mounted-projectile` | 19/0 (PID 29608) |  |
| `P04-save-unmounted-approach` | P04 | **PASS** | `final105-p04-save-unmounted-approach` | 22/0 (PID 25920) |  |
| `P04-load-unmounted-approach` | P04 | **PASS** | `final105-p04-load-unmounted-approach` | 20/0 (PID 24204) |  |
| `P04-save-mounted-approach` | P04 | **PASS** | `final105-p04-save-mounted-approach` | 25/0 (PID 28536) |  |
| `P04-load-mounted-approach` | P04 | **PASS** | `final105-p04-load-mounted-approach` | 20/0 (PID 28712) |  |
| `P04-save-unmounted-casting` | P04 | **PASS** | `final105-p04-save-unmounted-casting` | 396/0 (PID 17672) |  |
| `P04-load-unmounted-casting` | P04 | **PASS** | `final105-p04-load-unmounted-casting` | 388/0 (PID 1504) |  |
| `P04-save-mounted-casting` | P04 | **PASS** | `final105-p04-save-mounted-casting` | 677/0 (PID 16696) |  |
| `P04-load-mounted-casting` | P04 | **PASS** | `final105-p04-load-mounted-casting` | 737/0 (PID 22048) |  |
| `P05-save-manual` | P05 | **PASS** | `final105-p05-save-manual` | 44/0 (PID 29560) |  |
| `P05-load-manual` | P05 | **PASS** | `final105-p05-load-manual` | 20/0 (PID 28424) |  |
| `P05-save-quick` | P05 | **PASS** | `final105-p05-save-quick` | 44/0 (PID 28540) |  |
| `P05-load-quick` | P05 | **PASS** | `final105-p05-load-quick` | 20/0 (PID 29668) |  |
| `P05-save-auto` | P05 | **PASS** | `final105-p05-save-auto` | 44/0 (PID 29416) |  |
| `P05-load-auto` | P05 | **PASS** | `final105-p05-load-auto` | 20/0 (PID 28840) |  |
| `P05-save-queued` | P05 | **PASS** | `final105-p05-save-queued` | 34/0 (PID 28040) |  |
| `P05-load-queued` | P05 | **PASS** | `final105-p05-load-queued` | 20/0 (PID 28228) |  |
| `P05-save-alternating` | P05 | **PASS** | `final105-p05-save-alternating` | 17/0 (PID 29540) |  |
| `P05-load-alternating` | P05 | **PASS** | `final105-p05-load-alternating` | 45/0 (PID 28572) |  |
| `P05-load-manual-renamed` | P05 | **PASS** | `final105-p05-load-manual-renamed` | 20/0 (PID 26684) |  |
| `P06-legacy` | P06 | **PASS** | `final105-p06-legacy` | 34/0 (PID 21884) |  |
| `P06-schema1` | P06 | **PASS** | `final105-p06-schema1` | 34/0 (PID 29372) |  |
| `P06-future` | P06 | **PASS** | `final105-p06-future` | 33/0 (PID 28280) |  |
| `P06-malformed` | P06 | **PASS** | `final105-p06-malformed` | 33/0 (PID 24684) |  |
| `P06-profile` | P06 | **PASS** | `final105-p06-profile` | 33/0 (PID 27612) |  |
| `P06-campaign` | P06 | **PASS** | `final105-p06-campaign` | 33/0 (PID 24100) |  |
| `P06-missing-rider` | P06 | **PASS** | `final105-p06-missing-rider` | 34/0 (PID 6104) |  |
| `P06-missing-mount` | P06 | **PASS** | `final105-p06-missing-mount` | 34/0 (PID 24656) |  |
| `P06-mismatched-profile` | P06 | **PASS** | `final105-p06-mismatched-profile` | 34/0 (PID 29132) |  |
| `P06-policy` | P06 | **PASS** | `final105-p06-policy` | 33/0 (PID 28484) |  |
| `P06-combat-missing` | P06 | **PASS** | `final105-p06-combat-missing` | 49/0 (PID 28344) |  |
| `P06-combat-ai` | P06 | **PASS** | `final105-p06-combat-ai` | 49/0 (PID 23320) |  |
| `P06-failed-area-load` | P06 | **PASS** | `final105-p06-failed-area-load` | 26/0 (PID 18276) |  |
| `P06-foreign-header-campaign` | P06 | **PASS** | `final105-p06-foreign-header` | 33/0 (PID 27876) |  |
| `P07-timeout` | P07 | **PASS** | `final105-p07-timeout` | 58/0 (PID 23032) |  |
| `P07-timeout-cold` | P07 | **PASS** | `final105-p07-timeout-cold` | 20/0 (PID 27772) |  |
| `P07-cancel-wait` | P07 | **PASS** | `final105-p07-cancel-wait` | 60/0 (PID 28788) |  |
| `P07-cancel-wait-cold` | P07 | **PASS** | `final105-p07-cancel-wait-cold` | 20/0 (PID 29172) |  |
| `P07-locked-replace` | P07 | **PASS** | `final105-p07-locked-replace` | 59/0 (PID 24052) |  |
| `P07-locked-replace-cold` | P07 | **PASS** | `final105-p07-locked-replace-cold` | 20/0 (PID 28304) |  |
| `P07-serialization-cancel` | P07 | **PASS** | `final105-p07-cancel` | 52/0 (PID 23288) |  |
| `P07-serialization-cancel-output` | P07 | **PASS** | `final105-p07-output` | 51/0 (PID 22316) |  |
| `P07-serialization-cancel-output-cold` | P07 | **PASS** | `final105-p07-output-cold` | 20/0 (PID 20052) |  |
| `P07-disable-reenable` | P07 | **PASS** | `final105-p07-disable` | 53/0 (PID 27192) |  |
| `P07-campaign-b` | P07 | **PASS** | `final105-p07-campaign-b` | 63/0 (PID 27096) |  |
| `P07-campaign-b-cold` | P07 | **PASS** | `final105-p07-campaign-b-cold` | 7/0 (PID 26908) |  |
| `P07-prepare-removal` | P07 | **PASS** | `final105-p07-removal` | 40/0 (PID 27344) |  |
| `P07-absent-kmc` | P07 | **PASS** | `final105-p07-absent` | 9/0 (PID 26648) |  |
| `P07-removal-no-dll` | P07 | **PASS** | `final105-p07-removal-no-dll` | 12/0 (PID 26796) |  |
| `P07-disable-during-load` | P07 | **PASS** | `final105-p07-disable-load` | 33/0 (PID 10524) |  |
| `P07-rider-death` | P07 | **PASS** | `final105-p07-rider-death` | 26/0 (PID 11368) |  |
| `P07-rider-death-cold` | P07 | **PASS** | `final105-p07-rider-death-cold` | 3/0 (PID 26356) |  |
| `P07-mount-death` | P07 | **PASS** | `final105-p07-mount-death` | 26/0 (PID 26752) |  |
| `P07-mount-death-cold` | P07 | **PASS** | `final105-p07-mount-death-cold` | 3/0 (PID 27056) |  |
| `P07-rider-size-change` | P07 | **PASS** | `final105-p07-size` | 22/0 (PID 27204) |  |
| `P07-rider-size-change-cold` | P07 | **PASS** | `final105-p07-size-cold` | 3/0 (PID 28108) |  |
| `P07-area-reload` | P07 | **PASS** | `final105-p07-area-reload` | 33/0 (PID 29280) |  |
| `P07-area-reload-cold` | P07 | **PASS** | `final105-p07-area-reload-cold` | 20/0 (PID 26612) |  |
| `P07-area-cross-entry` | P07 | **PASS** | `final105-p07-area-cross-entry` | 34/0 (PID 29104) |  |
| `P07-area-cross-entry-cold` | P07 | **PASS** | `final105-p07-area-cross-entry-cold` | 20/0 (PID 24264) |  |
| `P07-area-cross-entry-auto` | P07 | **PASS** | `final105-p07-area-cross-entry-auto` | 24/0 (PID 28580) |  |
| `P07-area-cross-exit` | P07 | **PASS** | `final105-p07-area-cross-exit` | 34/0 (PID 28356) |  |
| `P07-area-cross-exit-cold` | P07 | **PASS** | `final105-p07-area-cross-exit-cold` | 20/0 (PID 6632) |  |
| `P07-area-cross-exit-auto` | P07 | **PASS** | `final105-p07-area-cross-exit-auto` | 24/0 (PID 27708) |  |
| `P08-rt` | P08 | **PASS** | `final105-p08-rt` | 54/0 (PID 25780) |  |
| `P08-ordinary-attack-controls-tb` | P08 | **PASS** | `final105-p08-ordinary-tb` | 64/0 (PID 27236) |  |
| `P08-chunk4-sustained-tb` | P08 | **PASS** | `final105-p08-sustained-tb` | 52/0 (PID 27832) |  |
| `P08-mounted-mammoth-primary-hit-tb` | P08 | **PASS** | `final105-p08-mammoth-tb` | 66/0 (PID 24436) |  |
| `P08-tb` | P08 | **NOT RUN** |  |  | Bounded known issue, not mandatory: the turn-based longbow full-round attack count fails with the same assertion on Phase 3H preview.6, before any Chunk 5 work; the accepted Chu... |

Every row above ran against that single payload and suite in its own process,
and every run restored the actual intake (`modsRestored`/`workingRestored`
true, no runtime lock retained).

The failing runs on the way to this payload are retained with their evidence,
each a finding that changed the payload: `final99-p07-removal` and
`final99-p06-foreign-header` (preview.99), `final100-p07-removal`,
`final101-p07-removal`, `final102-p07-removal` (the removal binding, 5B),
`final103-p07-absent` (the isolation's write-lease seams, 5B),
`final104-p03-save-suspended` (the suspended-Delay fixture left its same-round
target to the initiative dice; the target is now arranged pre-encounter like
the rider and every candidate's timing is recorded) and
`final104-p04-load-mounted-casting` (the debt-continuity helper's fixed 1 ms
slack sat below the installed clock's millisecond rounding of each frame's
delta against the float cooldown tick; the slack is now derived from that
rounding, and debt may still only fall). Every one of those runs restored the
actual intake.

## Disable and removal contract (5B)

**Prepare-to-Disable / removal.** `MountedRemovalPreparation` is the user-visible
contract: a UMM GUI button ("Prepare to disable / remove KMC") runs an
inspection-only assessment first (`RemovalReadinessPolicy`, pure, component-tested).
It refuses, with the exact reason, while a permanent KMC Horse reference exists
in the loaded world or cross-scene party (a unit of the KMC Horse blueprint
`4016c7db400ab721ff125aef9e65e202`, or a character holding the KMC Horse
companion or advancement feature), because a save holding one cannot be opened
without the mod and the campaign must not be stripped to make removal "safe".
It refuses outside a settled out-of-combat world: any party member in combat
(the engine's own `IsSaveAllowed` admits a manual save under the qualified
paired policy, so it is deliberately not the removal rule), an active mounted
command or stock attack intent, a paired activation, pending combat
restoration, a load in flight, a suspended or draining save, a held world or a
pending reset, a non-Default game mode. The inspection fails CLOSED: an
exception during the permanent-reference scan is "cannot establish safe
removal", never "clean". Units stashed in areas that are not loaded are not
inspected; their absence from the written cleanup archive is what is verified
instead (below). Otherwise it dismounts through the registered disable's own
cleanup and requests one NEW native save named `KMC_CLEANUP` through the
engine.

**The cleanup archive is bound, not looked up.** The installed engine never
updates the descriptor a save is requested with: `SaveManager.SaveRoutine`
(`0x0600BEF3`) keeps it only as `originalSave` (IL_01A0), prepares and
registers a copy of its own (IL_01DC..IL_022C), and the worker
`SerializeAndSaveThread` (`0x0600802A`) writes that copy's archive in place,
reaching the Clear/RenameFile replacement site KMC transpiles only when an
original archive is passed (IL_031C). A first-ever save such as `KMC_CLEANUP`
therefore records no replacement commit, and the requested instance never
learns its path (read-only IL receipts `saveinfo-lifecycle-il.txt`,
`save-worker-il.txt`). `MountedPersistenceService` records each ordinary
completion per operation (`CompletedSaveCount`, `LastCompletedSave`: the
requested descriptor, the registered descriptor the engine wrote, its path),
latched only on the path that saw the wrapped routine end, the archive worker
settle without fault and the prepared descriptor read complete, and recorded
before the scope is released. Readiness is reported only when the written
archive binds to exactly that record: one wrapped save operation completed
since the request, completed for this request's own descriptor (reference
identity, never a name, never a `FirstOrDefault` over the save list), its
written descriptor registered in the requested one's place, complete on disk,
manual, named `KMC_CLEANUP` and of the loaded campaign; every retained member
scanned (`NativeMountedSaveStorage.FindReferences`: all `.json` members and
`kmc-mounted-state`, bounded at 1024 members / 64 MB per member / 512 MB total,
length mismatch is a failure) for any KMC-registered blueprint identity with
zero hits; the KMC member recording no pair, no `Combat` supplement and no
control binding; the archive hashed. Once the operation has completed every
binding fact is final, so a mismatch, a failed read-back, scan or hash is
reported as **unconfirmed** ("Do not remove KMC on this result; prepare
again"), never as safe.

Native case `prepare-removal` (`final105-p07-removal` PASS 40/0): a real KMC
Horse unit spawned through the engine's creator is named and refused; then, in
real combat with the diagnostic enemy, the assessment refuses by its own rule
while the engine itself would admit a manual save (`engineSaveAllowed`
recorded); after the encounter the assessment settles, the cleanup save
`Manual_301_KMC_CLEANUP.zks` is written, bound to the completion record with
the replacement-commit record at zero (both archives of the walk are
first-ever saves) and verified from its bytes; the registered disable then
succeeds and re-enable/remount reuse the same actors. Four failing runs of
this case on earlier payloads are retained, each a payload-changing finding:
`final99-p07-removal` (real combat pauses the game and the walk must unpause
before gating on Default mode), `final100-p07-removal` (the diagnostic
enemy's memory lease may be refreshed only while the encounter continues),
`final101-p07-removal` (the written-archive facts were read from the requested
descriptor, which the engine never updates) and `final102-p07-removal` (the
binding waited on a replacement commit a first-ever save never records).

**Disable during a real load.** Native case `disable-during-load`
(`final105-p07-disable-load` PASS 33/0) probes the exact registered UMM
toggle at every third frame a live mounted load owns -- before early
restoration and at the semantic-restored / presentation-pending boundary -- and
requires refusal at every one, with the load then restoring the pair exactly
once; then the equivalent-state cycle at rest (disable, six native frames,
re-enable, remount, four native frames, invariants clean, bounded control
counts, same actors); then a second load whose disable is requested in the
same frame as the load before the routine has started, recorded as what
production does and judged by its outcome (an accepted disable must mean the
engine opened the save with no KMC restoration at all; a refused one, the
measured outcome, means the load restored the pair once and the rest cycle
repeats from it). Run `final94-p07-disable-load` established that a same-frame
disable, re-enable and remount right after a load produces a scoped attachment
the next frame's production invariant check invalidates; the case separates
those steps by the same native frames the `disable-reenable` case uses and
judges the remount only after that check has run. That failure is retained.

**Integration-absent load.** Native case `absent-kmc` (`final105-p07-absent`
PASS 9/0) opens the cleanup archive the `prepare-removal` run wrote, in a
fresh process, with KMC's gameplay and persistence integration DETACHED before
the native load (every KMC gameplay and persistence Harmony guard removed,
services off), moves the main character through ordinary input and writes one
NEW engine-only archive (`Manual_302_KMC_ABSENT2.zks`) that carries no KMC
member at all, over an untouched cleanup archive. The DLL is still loaded (it
hosts the automation) and the run-scoped save isolation stays, so this is
"integration absent", not "mod absent". Run `final94-p07-absent` established
that the isolation's fail-closed header commit guard refuses the engine's own
header update during such a load unless the isolation itself keeps the loaded
archive read-only; the isolation installs its own `SaveManager.LoadRoutine`
seam once for that purpose. Run `final103-p07-absent` established the same
for writes: the isolation's write leases were observed through the
persistence controller's `PrepareSave` postfix, which this case detaches, so
the engine-only save reached the commit guard with no lease and was refused
(the engine deleted its output and never called back). The leases are now
observed and released through the isolation's own seams on the exact
`PrepareSave` and `SerializeAndSaveThread` tokens, and the controller carries
neither. Both failures are retained.

**The genuine no-DLL load.** Native case `removal-no-dll`
(`final105-p07-removal-no-dll` PASS 12/0) is the mission's bounded external
observation path: `tools/KmcRemovalObserver` is a separate minimal UMM mod
(its own assembly, no reference to KMC, packaged by
`scripts/Package-Observer.ps1` from the same clean commit and validated by
`scripts/Validate-ObserverPackage.ps1`). The launcher stages the live Mods
clone WITHOUT any `KingmakerMountedCombat` entry (staging mode
`live-clone-minus-kmc-plus-observer`: the verified starting installation is
removed from the clone and restored exactly afterwards) with the observer in
its place, binds the observer package to the candidate commit, and passes only
observer arguments. The observer routes every native save-root, area-stash and
cloud-replication resolution into the owned profile (the same patched seams
and metadata tokens as KMC's own isolation, the same installed assembly MVID),
proves KMC absent from the Mods tree, the process's loaded assemblies, UMM's
mod list and Harmony's patch owners, reads the cleanup archive header through
the engine, loads it through the engine's own main-menu path
(`UI.MainMenu.LoadGame`), records the opened campaign, area and party, moves
the main character through a real ground click, and quits. It writes no save.
`scripts/runtime/Test-KmcObserverResult.ps1` validates that result against the
run's request and the archive bytes on disk (only the engine's own header
member may differ after its LoadedTimes update; every other member is
byte-identical; the KMC member's retention is recorded) and composes the run
record, and the ledger binds the entry to the observer result and to the
`prepare-removal` run whose cleanup archive it opened. UMM's startup rewrite
replaces the KMC entry in `Params.xml` with the observer's entry in its exact
place; that one replacement is admitted only for a declared removal-observer
run and the snapshot bytes are restored exactly. Shakedown
`final99-p07-removal-no-dll-pilot` (observer 12/0 in-process on the preview.98
cleanup archive; harness FAIL on the then-unadmitted UMM delta and a schema-1
game record; profile restored through `Recover-KmcPersistenceProfile.ps1`) is
retained.

## Harmful lifecycle boundaries (5C)

Native cases `rider-death` / `mount-death` (`final98-p07-rider-death`,
`final98-p07-mount-death`, with fresh-process cold loads `final98-p07-rider-death-cold`,
`final98-p07-mount-death-cold`): while mounted and in real combat with the diagnostic
enemy, the subject receives lethal native enemy damage under the unchanged
difficulty multiplier (`RuleDealDamage` from the enemy, never a state edit); the
lifecycle cleanup ends the pair with the partner unharmed; the engine's own save
admission after the encounter is recorded condition by condition
(`SaveManager.IsSaveAllowed 0x06008028`: loaded area, party combat, game-over
reason, dialog, cutscene, global-map encounter) together with the subject's
native life state; a NEW native save `KMC_DEATH` records no pair over an
untouched first archive; an in-process reload and a fresh-process cold load
both invent nothing and restore nothing while the native life state the engine
left is exactly the one carried (the ledger binds each cold entry's subject
life state to its source run's recorded admission).

**Each subject dies under the strongest policy the engine lets it survive as a
world.** The fixture Mammoth is a non-essential pet: it dies under the Chunk 4
scoped death policy (`NativeDeathPolicyLease`: the cached values of the game
settings "dead companions rise after combat" and "death's door" for this
process only, never the persisted settings, the damage multiplier untouched,
exact restoration verified and recorded in the `death-policy-restored` row),
so `TrueDeath` is true during the case, the death is final, and Dead is what
the save, the reload and the cold load carry
(`final98-p07-mount-death` 26/0: HpLeft -19, finally dead, still in the world,
dropped from `Player.Party` by the engine; cold 3/0: exactly one dead
player-faction unit, the mount). The fixture rider is the campaign's main
character: an essential unit made finally dead is the engine's own game over
(`GameOverController` `EssentialUnitIsDead`), which leaves no world to save,
so the rider dies under the campaign's live policy. With the owner's
"dead companions rise after combat" setting ON, `GameDifficulty.TrueDeath` is
false, `UnitLifeController.OnUnitDeath` does not make the death final and the
engine's own rule revives the rider at the end of the encounter -- measured
Conscious at HpLeft 7 (damage 58 to 38) at the first frame after combat with
every `IsSaveAllowed` condition satisfied -- and that revived state is what
persists (`final98-p07-rider-death` 26/0, cold 3/0: no dead unit, the rider
Conscious). The case records the policy decision (`permanent`, `essential`,
`mainCharacter`, `immortal`, `trueDeath`) and the fixture validator admits the
live policy only for an essential subject and only unchanged.

**Live eligibility change.** Native case `rider-size-change`
(`final105-p07-size` PASS 22/0, cold `final105-p07-size-cold` PASS 3/0): the
engine's own `EnlargePersonBuff` (resolved by name, applied through the native
buff system, never a state edit) makes the mounted Medium rider Large; KMC's
mounted invariant (rider exactly Medium) ends the pair by its own rule with
both actors alive and the effect in place; the engine admits a save; a NEW
no-pair archive `Manual_301_KMC_SIZE.zks` is written over an untouched first
archive; the in-process reload and the fresh-process cold load restore nothing
and invent nothing while exactly the enlarged rider it recorded is Large and
carries the effect (the supported mount is natively larger than Medium and is
excluded from that count).

The three earlier payloads are retained as their failing runs: preview.95 and
preview.96 measured the revival under the live policy for both subjects before
the policy lease was used (`death-revival-triage96.md`); preview.97 measured
the rider as the main character under the permanent policy and the finally
dead pet absent from `Player.Party` in a fresh process (`death98.md`).

Nothing in the ledger is mapped any more. The P05 quick/auto/queued/renamed/
alternating slots and the P07 area transitions, timeout, cancel-wait and
locked replacement each have their own PASS entry on this payload (the ledger
table above). The active turn-based, unresolved-preparation and
overlapping-effect boundaries the P04 gate names are the P02 and P03 entries
executed on this same payload -- the active paired turn at its five
checkpoints; step, conversion, round effect, pending reaction, condition,
preparing condition and suspended Delay -- each its own PASS entry rather
than a mapping to older evidence. Condition, form and missing-actor
boundaries are the P06 refusals; party removal and mode transitions remain
the accepted Chunk 4 lifecycle evidence.


## The intermittent 30 s leaf deadline, triaged

The `ordinary-attack-controls-tb` attempt `final86-p08-ordinary-tb` that failed
60/2 on the 30-second `Phase3gControls` leaf deadline is explained from its own
retained evidence and the installed IL (lab receipt
`leaf-deadline-triage86.md`, `standard-action-il.txt`). It stalled in case
`C03-rider-move-B`: at that arena position the near-side adjacency point around
the target was not walkable, the fixture chose the far-side point (5.8 m instead
of the 4.59 m the passing re-run used), the native move cost 5.41 s of
MoveAction cooldown, and the engine's `UsedTwoMoveAction` rule
(`MoveAction > 3 s`) therefore spent the standard action; the fixture's stage
then waited for `HasStandardAction()`, which the native rules could not restore
in that turn, until the deadline. It is a fixture geometry outcome on an
unmounted case, intermittent because the arena position at the sixteenth case
is not controlled; no KMC production code is involved. The fixture is accepted
Chunk 4 evidence and is not changed here; the earlier roots carrying the same
deadline message are different scenarios and keep their own records.

## Campaign B: a genuine second native game under isolated routing

Run `final92-p07-campaign-b` (`persistence-p07-save`, case `campaign-b`) on
`0.1.0-chunk5-preview.92` (source `47bc7dca9c0a6b9ec2a00ef6940acc33764a33ab`, DLL
`ab15a1564d85a00a9ebd12e207d8db01c67f44ff5c1d772e3ea1af1f05ae3fb8`, MVID
`c2d48575-4b33-477e-b500-422d3dfd3cb6`, suite `20260923-chunk5-suite109`)
**PASS 63/0**, PID 18028, loads 2 / writes 4, mods and Working restored, no lock.

The isolated save authority may declare bootstrap leaves for exactly one native new
game per run. Before the run the authority is bound to campaign A alone; the
scenario opens the bootstrap window immediately before the native new game; the
engine mints `Player.GameId` (`Guid.NewGuid` at `LoadNewGame` IL_011A-012E) and
the authority freezes exactly that identity on B's first admitted write -- the
engine's own autosave, whose `SaveRoutine` is constructed at IL_0525 before any
area is loaded, so a bootstrap autosave leaf admits a null observed area and still
commits in its declared area. KMC assigns, predicts or fabricates no identity.
Fixture-campaign requests never project onto bootstrap leaves and the minted
identity never reaches fixture leaves (25 authority contract checks, 177/0).

The engine's new game is entered the way the engine itself does it after
`ResetToMainMenu` (`CheatsTransfer.NewGameCoroutine 0600C47A`): wait for the
loading process, then `MainMenu.EnterGame 06000D84`, which shows the loading
screen, disposes the menu UI, loads base mechanics and only then runs
`Game.LoadNewGame(preset, null)`. A direct `LoadNewGame` from the live menu
(run `final91-p07-campaign-b`) failed inside `SceneLoader.LoadAreaCoroutine`
with `ArgumentException: Destination scene is not valid`; that failure is
retained. The preset is the engine's own authored start: the Endless start
preset when the installed license enables it (it did: area = enter-point area
`c49315fe499f0e5468af6f19242499a2`, `MakeAutosave`, no character-generation
product, `Game.NewGameUnit` null), otherwise the main-campaign preset.

Measured: A mounted (rider `b6628a77…`, Mammoth `d79a4f6c…`), opening archive
`Manual_300_KMC_P01.zks` `880dd427…`; a real ground click moved the pair 2.95 m;
the post-expenditure archive is a NEW save `Manual_301_KMC_P01B.zks` `18a12694…`
(its own name, because `CreateNewSave 06008015` passes a repeated name through
`MakeNameUnique`, which appends a space and a number -- run `final90` was refused
on exactly that and is retained). Esc-menu reset released the pair and every
lease. The engine minted **`bf673e4e-5e19-4ec3-b5a5-54d59ea73357` / "Baron"**,
frozen once; B's autosave `Auto_1.zks` `52f39750…` and manual `Manual_302_KMC_B.zks`
`86c83859…` are clean by rows and by bytes (KMC member Mounted=false, no
pair, no slots, B's own campaign and area); in B: Unmounted, 0 bindings, no
restoration, no Mount cast, no actor shared with A. `LoadGameFromMainMenu` back
to `Manual_301_KMC_P01B` from inside B: A under its own identity in a new world,
the exact pair restored once (semantics 2 / presentation 1), 2/2 bindings, mount
position delta 0.0 m, debt conserved, B's world disposed exactly once, all four
archives byte-identical, then movement, a delivered attack and usable play.
Receipt: lab `campaign-b92.md`. B's own cold load in a fresh process and the
foreign native header against B's archive are the two cases below.
**B's archives opened cold.** Native case `campaign-b` cold
(`final105-p07-campaign-b-cold` PASS 7/0): B's own manual archive
`Manual_302_KMC_B.zks` (admitted under the identity the engine minted for B,
read from the source run's own frozen observation, never the fixture's) opens
in a fresh process as a world of its own -- B's identity and area, no
supported mount, no pair, no binding, nothing restored or invented -- then B's
main character moves through a real ground click and a NEW manual save
`Manual_303_KMC_B2.zks` is written in B, with B's source byte-identical
afterwards.

**Foreign native header.** P06 case `foreign-header-campaign`
(`final105-p06-foreign-header` PASS 33/0): B's own archive whose KMC member is
rewritten to claim A (a metadata-only derivation of B's manual archive; the
native header stays B's) is refused before enumeration at all three native
entry points, and the original world is retained. The run's isolation
authority admits that archive as a declared read-only foreign-identity leaf
(loads of exactly B's identity, never a write). Run `final99-p06-foreign-header`
established that the authority must declare it; that failure is retained.


## Code-review remediation (R1-R7)

Candidate `0.1.0-chunk5-preview.86`, qualifier `chunk5-remediation`, source
`245a340a192ea6725938fdcbaf884ef1188f26b0`. ZIP
`a83d80d0b3cf6e9cbae97dada4b377160347c0a29a4e13de4ee87309e271593b`, manifest
`b44fa858a42d8f2bdd1de33280f2ea80659399b9fd2f518fe94f4ae09de99730`, DLL
`86e27fa5beb98aaba1968f614de710369bf1801787439dfdf1857cc9a03d13a5`, MVID
`05c5cdbf-35ce-4882-9ddc-e0e009dbf556`, suite `20260923-chunk5-suite101`.

The external review resolved only `8ea70c8`, which was the published branch head;
the branch is now published at the candidate's descendant, so the reviewed source
is retrievable. Every finding below was checked against the local implementation
before being changed.

| Finding | Disposition | Evidence |
|---|---|---|
| R1 first-yield worker ownership | **Partly already present, completed.** The double capture around the native step and the re-read at the release boundary were already there. Added: the latch is now taken in a `finally` so a step that creates the worker and then throws cannot leave it unobserved, and the read reports whether the answer was *established* -- an unreadable routine defers instead of releasing. Static inspection settles the premise: `<saveTask>5__2` (`0x04008CEA`) is stored exactly once, at `IL_0621` of `MoveNext`, and never rewritten or nulled, so it outlives disposal. | contracts 140/0 incl. 6 new ownership checks; `final86-p07-cancel` 47/0 |
| R2 interrupted-save consistency | **Boundary established and the missing artifact delivered.** The worker reads LIVE state on its own thread: `Game.Instance.Player.CrossSceneState` at `IL_0063-006D`, the live `LoadedAreaState` three times, and `b__2` stashes the live area state. Area transfer now joins save, load and teardown in refusing while an owned worker can still commit. A new `serialization-cancel-output` case stops at settlement so the interrupted operation's own archive survives and is cold-loaded directly. | `final86-p07-output` 46/0; **`final86-p07-output-cold` 20/0** on hash `d8805fdd72099fd5dd16b53df097d4c825c625c5d2adca0e5a170bd05272b398` |
| R3 commit vs cleanup reporting | **Fixed.** The commit is recorded the instant the native replacement returns, before descriptor rebinding and ownership completion, and the outcome is decided from that boundary: committed, not written, or unconfirmed. Unchanged previous bytes are claimed only when a previous archive existed and is still present; a first-ever save says so instead. The commit record covers replacement commits only: a first-ever save never reaches the replacement site (`SerializeAndSaveThread` IL_031C), its archive is written in place, and the engine deletes that output itself when its worker fails, so "not written" stands for it; its ordinary completion is the persistence service's own per-operation completion record (preview.105). | contracts 140/0 incl. the post-commit-fault case; source contract pins the recording order |
| R4 turn-based regression | **Reclassified.** The previous comparison used Phase 3H preview.6, an old failed candidate. The accepted Chunk 4 controls all match their accepted counts on this payload. The Phase 3H fixture's own evidence shows `nativeFullAttack=false`, `actorFullAttackRestrictedByMove=true`, one planned and one completed attack, range satisfied -- a **disproved obsolete fixture expectation**, original FAIL retained, fixture not rebuilt. | `ordinary-attack-controls-tb` 64/0 (HG 64/0), `chunk4-sustained-tb` 52/0 (GN 52/0), `mounted-mammoth-primary-hit-tb` 66/0 (HL 66/0); receipt `tb-regression-classification86.md` |
| R5 disable/re-enable, load refusal, removal | **Partly done.** Disable/re-enable and disable-during-save are natively qualified; disable during a live load is guarded in production and covered by source contracts only. The bounded Prepare-to-Disable/removal contract and mod-absent loading are **NOT IMPLEMENTED**. | `final86-p07-disable` 51/0 |
| R6 campaign isolation, missing cases | **NOT DONE.** Disposable campaign B, A->B->A isolation, and the remaining P04 active TB/overlapping-effect boundaries are not implemented. | BLOCKED/NOT RUN below |
| R7 profile/harness exceptions | **Reviewed and narrowed.** A byte-identical `Params.xml` now passes without requiring a SkipIntro append. The analytics exception is shape-based, applies only to entries present on exactly one side (creation or dispatch), keeps any entry present on both sides in the identity digest, and reports every admitted entry with its exact path, length and hash. | profile protection 48/0 incl. four path-shape negatives and the in-place-rewrite negative |

### Case-level P01-P08 on preview.86

| Gate | Coverage run | Result |
|---|---|---|
| P01 | `final86-p01-save` / `final86-p01-load` | PASS 23/0, PASS 20/0 |
| P02 | `final86-p02-*-partial-movement` | PASS 32/0, PASS 26/0 |
| P03 | `final86-p03-*-step` | PASS 34/0, PASS 28/0 |
| P04 | `final86-p04-*-mounted-attack` | PASS 27/0, PASS 18/0 |
| P04 | active TB / overlapping-effect boundaries | **NOT RUN** |
| P05 | `final86-p05-*-manual` | PASS 44/0, PASS 20/0 |
| P05 | quick/auto, rotation, queued, renamed, A/B/A | **NOT RUN on this payload** (earlier payload evidence only) |
| P06 | `final86-p06-legacy`, `final86-p06-failedarea` | PASS 34/0, PASS 26/0 |
| P06 | genuine other-campaign isolation | **BLOCKED** (needs disposable campaign B) |
| P07 | `final86-p07-cancel`, `final86-p07-disable` | PASS 47/0, PASS 51/0 |
| P07 | `final86-p07-output` + `final86-p07-output-cold` | PASS 46/0, PASS 20/0 |
| P07 | area transitions | **NOT RUN on this payload** |
| P07 | removal contract, mod-absent load | **NOT IMPLEMENTED** |
| P08 | `final86-p08-rt` | PASS 54/0 |
| P08 | `ordinary-attack-controls-tb`, `chunk4-sustained-tb`, `mounted-mammoth-primary-hit-tb` | PASS 64/0, 52/0, 66/0 |

The intermittent `ordinary-attack-controls-tb` failure recorded here (60/2 on the 30-second `Phase3gControls` leaf deadline, re-run 64/0) is now explained; see "The intermittent 30 s leaf deadline, triaged" above.

Offline gates on this source: source 26, components 30, contracts 140, data 56,
owned fixtures 321, P06 guards 104, harness 261, profile 48, package 11 -- all
FAIL=0.

## Exact-final acceptance set on one frozen candidate

Frozen candidate `0.1.0-chunk5-preview.83`, qualifier `chunk5-final`, source
`0f80d03f8ce0914adea1b6a6641835d7292a93f4`. Private ZIP
`KingmakerMountedCombat-0.1.0-chunk5-preview.83-chunk5-final-diagnostic.zip`
SHA256 `ecd89591af9bc4eb64f754d2ebdf49c9ad867640ee99021acbe7a8171ef0256a`,
manifest `888879edc947c3ae79643cb6bc47837a131f78c20b11f119ae8d8f947305e007`,
DLL `45da40cbbf769e63c0881cbad44752ce4abc8a7896b2a791607b000c423e9ab0`,
MVID `f1401cfb-dba1-4afe-a10e-7c55c810e54e`. Qualification suite
`20260923-chunk5-suite98` SHA256
`8aeef3b2ff1be65df8d7df882d542f1ede3ca7e2ca9ac1d3b2ce0cdb6435e9a1`. Offline
gates on that exact source: source 24, components (patch construction 30) PASS,
contracts 127, data 56, owned fixtures 321, P06 fixture guards 104, harness 261,
profile protection 48, package 11 — all FAIL=0. This remains an unqualified
private engineering candidate; it is not installed, merged or released.

Every row below ran against that single payload and suite, and every one
restored the actual intake (`modsRestored`/`workingRestored` true, no runtime
lock retained).

| Gate | Source run | Cold or second run |
|---|---|---|
| P01 | `final83-p01-save` PASS 23/0 | `final83-p01-load` PASS 20/0 |
| P02 | `final83-p02-save-partial-movement` PASS 31/0 | `final83-p02-load-partial-movement` PASS 26/0 |
| P03 | `final83-p03-save-step` PASS 33/0 | `final83-p03-load-step` PASS 28/0 |
| P04 | `final83-p04-save-mounted-attack` PASS 27/0 | `final83-p04-load-mounted-attack` PASS 18/0 |
| P05 | `final83-p05-save-manual` PASS 44/0 | `final83-p05-load-manual` PASS 20/0 |
| P06 | `final83-p06-legacy` PASS 34/0 | `final83-p06-failedarea` PASS 26/0 |
| P07 | `final83-p07-disable` PASS 51/0 | `final83-p07-cancel` PASS 46/0 |
| P08 | `final83-p08-rt` PASS 54/0 | `final83-p08-tb` **FAIL 51/2**, see below |

**P08 turn-based remains a bounded known issue.** `phase3h-combat-loop-tb`
failed 51/2 on `phase3d-tranche-0: 3h-rider-longbow-ordinary` with "Native
sequence ended with fewer attacks than the legal full-round fixture expected."
The last previous attempt at that scenario, `20260906-phase3h-preview6-tb-final`
on `0.1.0-phase3h-preview.6`, failed with the same count and the same assertion,
before any Chunk 5 work existed. The real-time variant of the same scenario
passes 54/0 here, matching its own green baseline `20260920-chunk4-HH` on
`0.1.0-chunk4-preview.54` exactly. The turn-based longbow full-round attack
count is therefore recorded as an unresolved issue with its exact inputs; it is
not attributed to Chunk 5 and not claimed as fixed.

**One real regression was found and fixed by this set.** `phase3h-combat-loop-rt`
first failed 30/1 with "Mounted control save scope is unavailable." Commit
`5650d16` had added `serializationSuspended` to the begin action of
`NativeMountedControlService.WrapSaveRoutine`, which correctly refuses a second
owned control scope over a save that already holds one, but the horse companion
engine's save-scope probe still started a scope by hand and then drove that same
wrapper over it. From that commit onward the probe overlapped itself, and no
`phase3h` run happened in between, so it stayed latent. The production guard is
unchanged; the probe now lets the wrapper own the scope and samples the
suspended state from inside the wrapped routine.

Cold evidence loads the actual owned archive with a fresh PID. The orchestrator copies/hash-checks authorized bytes and compares telemetry after native results; it supplies no missing gameplay state. P05 A/B/A demonstrates state follows the selected archive. Save-root/campaign/type/name/path/hash/ownership guards are run-scoped; old scenarios retain strict Working-only authorization. Native quick/auto test settings use temporary getters without changing saved preferences.

Frozen preview.57 source: 65395394494a7cb8c7519b925a932985e50fa1eb, guarded-published. Private ZIP KingmakerMountedCombat-0.1.0-chunk5-preview.57-p07-area-placement-diagnostic.zip SHA256 0ab05ee8ca0f6b983e65452a37d2f739e0eed739742c23233287048bc5ed0e53; manifest a13e46bfaba0d22960348cc26413cec6ae4219f9dec1d24449b82a03eb4ad29e. DLL 1f8ef0577ec9da1201385e48d4760e7fad9e8597f647a54143fe1ac4ea62bfce, MVID faad04be-714e-4700-a92d-875fa9ad048c. Suite59 SHA256 d0d165694a3e53f54c33a422d485929f6bb803237cfaed881fd37cd3ee36f683. Source22/components438/contracts110/owned-fixtures213/full-harness261/package11 PASS. This package is an unqualified engineering checkpoint, not a completed persistence delivery. Final stopping documentation does not rebuild it.

Preview57's real area reload failed9/1 at a diagnostic assertion that both party views were replaced; that original evidence remains FAIL. Read-only IL inspection of the installed Assembly-CSharp (SHA256 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, MVID 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7) now settles the call-site Boolean the stop report left open. Game.LoadArea06000CD5 passes `saveInfo != null` as SceneLoader.UnloadEntitiesCoroutine06008096's unloadCrossScene04008DA5; that iterator always destroys DynamicRoot and destroys CrossSceneRoot only when the flag is true, and UnloadAreaCoroutine06008095 interrupts every cross-scene unit's commands but destroys only units whose master left Party/PartyCharacters/DetachedPartyCharacters/RemoteCompanions/ExCompanions. Game.ReloadArea06000CD6 calls LoadArea with a null saveInfo. **An ordinary area transfer therefore retains the party rider and its pet mount together with their exact native views; replacement is the save-load contract.** The assertion's fixture premise was disproved, not a gameplay defect: the production AreaTransitionPrefix is a prefix on that same LoadArea06000CD5 and BeginAreaTransition already cancels whenever saveInfo is non-null, so the transfer arms only on the retained-view branch. Receipts: lab analysis-cache/chunk5-persistence/area-unload-contract.txt and area-view-lifecycle.txt.

The measured LoadArea queue order is preload, before-exit SaveRoutine, UnloadAreaCoroutine, UnloadEntitiesCoroutine, UnloadUnusedAssets, LoadAreaCoroutine, OnAreaLoaded06000CD7, after-entry SaveRoutine, AreaLoadingComplete06000CD8. The restoration hook therefore runs after native party placement and before the after-entry autosave, as designed.

**The native load's point of no return.** SaveManager.LoadRoutine06000BF00 reads and deserializes the header at IL011E/0147, then destroys the current world with RootUiContext.DisposeUiScene at IL0215 and **Game.DisposeState at IL021F**, clears the area list and AreaDataStash, and only then constructs ThreadedGameLoader at **IL0262**, after which CopyToStash, GameStatistic.Deserialize, RestoreAreaBlueprint, Player.PostLoad, Player.set_GameId and ApplyUpgrades follow. Area and entity content is therefore deserialized strictly **after** the loaded world has already been destroyed, while the header is parsed before it. Of the seven handlers, six are Finally scopes and the only Catch covers SystemDialogException over IL02CE-02DE and rethrows at IL02DF; the whole-method Fault handler only invokes the iterator's Dispose. A post-disposal deserialization failure therefore leaves no native rollback: the previous world is gone and nothing has replaced it. This fixes the fixture design for the outstanding failed-load case — corrupting the header fails before disposal and only repeats existing admission coverage, whereas corrupting a native area member fails past the point of no return. Receipt: lab failed-load-boundary70.md.

**P06 native load failure after the old world is destroyed, on preview70.** The static prediction above located the area read inside LoadRoutine and was **wrong**; the first native run disproved it and the case was rebuilt on the measured boundary. The corrupt member is read by AreaDataStash.UnstashAreaState inside **SceneLoader.LoadAreaCoroutine**, a separate and later loading process, and **LoadingProcess.Update** — not TickLoading — catches it and rethrows as LoadGameException at IL00144. SaveManager.LoadRoutine itself therefore **completes**, and its own after-load callback legitimately fires.

That makes the real failure mode worse than predicted: the previous world is destroyed, the engine reports a successful load, and the process is left in GameModeType.None with no loaded area at all. Root 20260923-chunk5-P06-failed-area-load-F (PID20084) PASS26/0 measures exactly that. The derivative Manual_813_KMC_P06_AREA.zks 38d0241c64be1fca1d36855516c84e1cbf42f2855fe7098f4bf9b365baf1d399 corrupts exactly one native member — the archive's own loaded area 9d1278a2f599b2a4daab53abdfe88d2e.json — inside a structurally valid ZIP, with header, KMC metadata and every other member SHA-compared unchanged and the source 84ffb91c… byte-identical throughout. It passes normal admission (rejections0) and fails only after disposal (nativeWorldDisposals1), recording the real native error `JsonSerializationException: Unexpected end when reading JSON`.

**KMC presents nothing into a world that does not exist.** Presentation stays at 1, relationship Unmounted, zero live units, no combat fence, and no owned save or serialization scope held. Early debt restoration *did* run (semantic 2 to 4) because Player.PostLoad genuinely completed inside the save load; those actors died with the world, so the retained selection is **inert rather than absent**, and the next load replaces it through the existing abandoned-scope path. The presentation gate's requirement of a loaded area is precisely what stops the engine's false-success callback from producing a restoration, and that is what this case qualifies.

**In-session recovery is not available; recovery is restart-only.** Retrying the *known-good* archive in the same process also fails, with `ArgumentException: Destination scene is not valid` from SceneManager.MoveGameObjectToScene inside SceneLoader.LoadAreaCoroutine: the Unity scene state is left invalid by the first failure, so every later load in that process fails too (failures2, recoveredInSession false, still GameModeType.None after 143.7s in the earlier measurement). This is reported as restart-only and never as proven in-session recovery. Restart recovery itself is proven separately: root 20260923-chunk5-P01-restart-recovery-A (PID2256) PASS20/0 loads that exact archive in a **fresh process**, reaches a loaded area in Default mode and completes real movement, a delivered attack and usable continuation.

Because the case deliberately ends with no world, the save-backed game-result guard is **inverted** for it rather than skipped — an accidental loaded area or active native game mode now fails it — and the ordinary-continuation requirement is replaced by an explicit check that no continuation was reported in a worldless process. Evidence rows are split positionally at the failure: every row before it must carry the exact pair, every row from it onwards must carry no actor unless a retry genuinely recovered. Validation-copy guards rise 61 to 104. Receipt: lab failed-load-boundary70.md, which records the disproved prediction alongside the measured correction.

**P07 save-worker lifetime after interruption, on preview70.** Review identified a real path in the published source: LoadingProcess.StopAll to NativeDeferredSave.AbandonOwned to owned wrapper Dispose to ScopedEnumerator cleanup, releasing AI/control restoration and the active-save scope. TrackNativeSave does wait for the archive worker, but only on the **enumerated** completion path; disposal runs the scope end action directly and never reaches that wait. WaitForAll protects the worker's four serializer subtasks and says nothing about the outer mounted-save scope's lifetime. So an interruption was releasing leases, clearing the overlap guard and skipping the world-reference restoration while the worker could still commit — silently, with no failure reported.

The boundary was established before any fix. SaveManager/<SaveRoutine>d__46::MoveNext0600BEF3 runs PrepareSave at IL01ED, writes header and screenshot inside a write scope at IL0424-0529 whose Catch(Exception) at IL0531 **swallows** a failed write, runs the PreSave barrier at IL054A-0595, and only at **IL061C** starts the background worker through ThreadedGameLoader.RunSafelyInEditor; from there the iterator only **polls** Task.IsCompleted/IsFaulted and never waits or joins. No persistence type carries a CancellationToken, and the only cancellation surfaces are LoadingProcess.StopAll06007FC3, which drops enumerators without touching a running serializer, and a UI timeout. **A started worker cannot be aborted, only drained.** Receipt: lab save-cancellation-boundary70.md.

The policy is therefore refuse-and-drain, never pretend. On early disposal with a live worker the scope is retained, no protection is released, no cancellation is reported, and the save is marked draining; a per-frame drain in Update — not a blocking wait, so required completion work keeps running — releases exactly once when the task settles, restores the native world reference and reports the truthful outcome. While draining, load admission refuses **before** world disposal.

Root 20260923-chunk5-P07-worker-drain-F (PID20084 class) **PASS43/0** qualifies it natively. The in-flight boundary is the operation's own: prepared leaf Manual_301_KMC_P01.zks, the scope's own Task identity, Task.IsCompleted false, exactly one hold at that leaf — not a cumulative worker count. Reaching it needed a bounded diagnostic hold that is inert unless an isolated run arms it, holds one already-authorized owned worker at its entry on that worker's **own** thread, is self-releasing and is disposed even on scenario failure; the serializer and commit path run unchanged afterwards. While the worker was live: leases retained, StopAll deferred with one deferral and zero drains and no cancellation callback, a second serialization refused through the real overlap guard, a conflicting load refused with **zero** world disposals, repeated cancellation neither releasing twice nor inventing an outcome, and a disable request unable to remove the drain owner. Settlement was truthful and byte-checked: committed, replaced **in place** 6ce1eafd to 31954ce1 with failedSaves0, released exactly once, controls and world reference restored, further Update and StopAll calls no-ops, debt conserved, then a real subsequent write 4df7b606 and ordinary movement, a delivered attack and usable continuation. Root -cold-A (PID20352) **PASS20/0** loads that subsequent save in a fresh process and plays.

**A measured limitation, not containment.** Across StopAll the engine's own suspension ends while the worker still writes: IsLoadingInProcess goes True to False, with IsPaused false and GameModeType.Default throughout. KMC's retained leases are **not** a substitute for that native suspension, and nothing here claims they prevent mutation of state the serializer may still read. This is recorded as observed behaviour in this modded configuration; it is not an independently established claim about the unmodified base game.

**Campaign identity.** Kingmaker.Player::set_GameId06000DE3 has exactly two meaningful callers: Game::LoadNewGame06000CDC mints a fresh Guid.NewGuid identity at IL011A-012E, and SaveManager.LoadRoutine adopts SaveInfo.GameId at IL0415-041A. A campaign identity is thus either minted by the engine starting a new game or inherited from an archive, never otherwise produced. Receipt: lab campaign-identity-contract70.md. This is why the P06 native foreign-header campaign case is blocked on an owner-prepared disposable second campaign rather than synthesized; see AUTONOMOUS-BLOCKERS.md.

Preview58 corrects the runtime assertion, the Assert-KmcAreaPersistenceEvidence contract and its synthetic negatives together. The scenario now writes an `area-reload-observed` row before any area qualification, carrying null-safe per-actor native-view identity, aliveness, entity binding, owned-pair exactness, resolved-actor count, the mounted-invariant string, presentation/attachment observation and the native loading-queue state. Qualification requires the declared retained disposition to match both measured instance IDs, both views to be live, bound and the pair's own, ValidateMountedInvariants to be clean, and no duplicate native actor — in addition to the unchanged world, debt, exactly-once control/slot and real post-area write checks. No threshold, actor, debt, cleanup or control check was relaxed, and the parser pins `area-reload` to the retained contract so the scenario cannot relabel its way past a failure. Owned fixture checks rise to 225 with twelve new area negatives (replaced/mislabeled/missing/unbound/stale views, duplicate actor, broken invariant, relabeled expectation, missing/late observation, legacy view drift, unsettled queue, observation mid-load). No production gameplay code changed.

Frozen preview.58 source: 7c4b86ddcdb1bc16a4c1e4f3d28656cecc1d3e65. Private ZIP KingmakerMountedCombat-0.1.0-chunk5-preview.58-p07-area-view-contract-diagnostic.zip SHA256 4ef132af0cf62c4ab61392c82f14d9c54eabe0f454655d87752c3de54904f80e; manifest 972c3042a8ab49fe2fc029fd251be204b5ef81f6300b27d6ccd12cd97d1753d7. DLL 95cce8114bece250e70568a84e173a3616a8b0a0b89d421cff543e18ad2ab830, MVID ccd89bc0-dfd6-40eb-b7fb-4acd5226dc67. Suite60 SHA256 c9d2718437ebd53be3b53ef5678bbcdf68ca1dc9d58f53dd30a998c41ab35d0a. Source22/components438/contracts110/data56/owned-fixtures225/validation-copies61/full-harness261/profile20/package11 PASS. This candidate qualifies the same-area P07 case only; it is not a completed persistence delivery.

Preview59 adds the two ordinary cross-area cases. `AutoSaveMode` is `None=0, BeforeExit=1, AfterEntry=2, LoadFromSave=3`; a module-wide scan of the public Game.LoadArea06000CC9 call sites finds seven literal None and five literal AfterEntry, and the only non-literal sources are the blueprint fields AreaTransition.AutoSaveMode0400123A, CapitalExit.AutoSaveMode04005692 and TeleportParty.AutoSaveMode04005D2C. Both BeforeExit and AfterEntry are therefore ordinary authored transition modes, so driving either through the public entry reproduces real native behaviour; the 5-arg overload is private and is never invoked. Because that entry teleports when the enter point already belongs to the loaded area, autosave ordering can only be qualified across a real area change. The declared destination is DungeonStartHub areafd1b6fa9f788ca24e86bd922a10da080 through entry104849f5f7ea36748aeeb036551047a9, which is the disposable Endless fixture campaign's own hub and already carries native evidence from chunk4-traversal-slope. Receipt: lab area-transition-contract58.md.

The first preview59 attempt FAILed 0/1 before the transfer completed, rejected by the isolated save authority, and that failure established a further native contract. SaveManager.SaveRoutine06008029 is an iterator, so KMC's save-admission prefix runs when LoadArea06000CD5 synchronously constructs the enumerator while the **departure** area is still loaded, whereas execution, PrepareSave06008025 and the committed header all follow in the destination. An authored after-entry autosave therefore legitimately spans two areas across its admission and commit boundaries. Preview60 declares that explicitly: a save leaf may carry an AdmissionArea distinct from its committed Area only when the run declares a transition, only when the leaf is writable, and only when the two areas are exactly the declared endpoints — the authority refuses any other pair at construction. Single-area leaves keep the unchanged exact contract, so this expresses a measured native boundary rather than relaxing the guard, and a rejected boundary now names the observed and declared identity instead of costing another run. Component checks rise to 439.

A cross-area case makes no scenario-driven pre-transfer write: the engine's own autosave is its departure or arrival archive. `area-cross-exit` must autosave the still-mounted departure area before suspension; `area-cross-entry` must autosave the destination only after restoration, proving the ordering at the native header barrier itself through counters captured inside SaveSnapshotStaged. The request now declares `persistenceAreaTarget`, and the isolated save authority pins every post-transfer write to that exact area, which tightens rather than relaxes the existing name/type/campaign/area contract. Owned fixture checks rise to 255 with fourteen new cross-area negatives per mode.

**Return-entry research and a corrected destination.** A bounded read-only identifier lookup over the installed sharedassets1.assets, controlled against the already qualified DungeonStartHub_Enter104849f5f7ea36748aeeb036551047a9, establishes that **no BlueprintAreaEnterPoint targets Area_Dwarf_1**: that GUID is followed only by its own scene references, as are the other delve floors through Area_Dwarf_10957af755145b0494587c511e18f1d7c6. The nine entry blueprints in this content space name only the two hubs, the final dungeon and an old cave; delve floors are reached through the DLC's own DungeonStageInitializer inside OnAreaLoaded06000CD7. An authored return leg into Area_Dwarf_1 therefore cannot be performed over the public native transition path, and no GUID or campaign transition may be invented to create one.

The same lookup corrects the preview59 fixture assumption. The disposable Working archive holds exactly two area states, the loaded floor 9d1278a2f599b2a4daab53abdfe88d2e and c49315fe499f0e5468af6f19242499a2, which resolves to **DungeonStartHub_Roguelike**. DungeonStartHubfd1b6fa9f788ca24e86bd922a10da080 is the story hub, so preview60-62 transferred this Endless campaign into an area it does not itself use through a story-campaign enter point. The campaign's own hub is the roguelike one, and the retained evidence is direct: its archive already carries that area's persisted state. Destination B moves there through DungeonStartHub_Roguelike_Enterd27d1d11f23ba9b46b6cdf00584ad08c as a fixture parameter change only, with the single-transfer contract, suspension/resume of one, admission-versus-committed area lifecycle and every persistence acceptance check unchanged. This fixture difference is **not** established as the cause of the movement failure below; that route keeps its own open known issue. Receipt: lab return-entry-research63.md.

**P07 cross-area result on preview63 at the campaign's own hub.** `area-cross-entry` is qualified source and cold. Root 20260923-chunk5-P07-area-cross-entry-save-F (PID15060) PASS34/0: real transfer 9d1278a2f599b2a4daab53abdfe88d2e to c49315fe499f0e5468af6f19242499a2 across 87 loading frames, one suspension and one resume, rider view -339300 and mount view -340724 retained, and the authored after-entry autosave's own native header barrier recording snapshots1/suspensions1/**resumes1**/Mounted/destination with the exact pair — restoration precedes that autosave. Its Auto archive is 949983 bytes (f4ef4d57aab073805559a7f80f72b199b4a606f9b02ae291e6b45d586bbd8e16) in the destination; the post-transfer Manual write 01de991c1507321e3d8674fcc9d609793b14178be98cdd8faa010316a7c311fc is a distinct archive in the same area. Ordinary continuation then completes on this arrival: native movement Success across 2.955m of a 3m route stopping 0.046m from target, a delivered attack, and three facts/two slots/zero Mount casts. Root -load-F (PID17468) PASS20/0 loads that exact B archive in a fresh process — not a later A save and not the Working fixture — verifying area B, the same pair, semantics2/presentation1, three facts, two slots and no Mount cast before its own movement and attack.

`area-cross-exit` is qualified source and cold on preview64 after the profile-identity correction below. Root -save-G (PID20224) PASS34/0 and -load-G (PID19644) PASS20/0. Its before-exit autosave is the distinct artifact the contract requires: 848361 bytes (290d4e12f7bd700d517731c4074f222df2e50a056c0e1a4b59cf3fd7e78ab9dd) captured in the **departure** area 9d1278a2f599b2a4daab53abdfe88d2e with a barrier of snapshots1/suspensions0/**resumes0**/Mounted, so it precedes suspension and carries no destination state — the exact complement of the after-entry barrier. The transfer still completes to c49315fe with one suspension and one resume, and the separate post-transfer Manual write c0e784733750fdeea1a25f271e0fa4338a22f4e00aaae3055b2a7fe3bef17c83 commits in the destination. The cold process loads that exact destination archive, not the autosave and not a later save, restoring the same pair with semantics2/presentation1, three facts, two slots and no Mount cast. That "never the autosave" statement describes the **destination Manual** cold subcase only; it is a property of which archive those two roots select, not a claim about the autosave's own loadability, which the transition-Auto subcase below establishes separately.

**The three distinct cross-area roles.** Completing this family required three different things to be true, and each is qualified by its own evidence rather than inherited from another:

| Role | What it establishes | Evidence |
|---|---|---|
| Autosave creation and order | The engine writes the authored autosave at the correct point relative to suspension and restoration | preview63 entry barrier snapshots1/suspensions1/**resumes1** in the destination; preview64 exit barrier snapshots1/suspensions0/**resumes0** in the departure area |
| Destination Manual cold load | State written *after* arrival survives a full process exit | preview63/64 -load roots, cold20/0 each, loading the post-transfer Manual leaf |
| Transition Auto cold load | The autosave *itself* is a loadable world, not merely a correctly ordered artifact | preview69 -auto-F roots, cold24/0 each, loading Auto_1.zks directly |

**P07 transition-autosave cold load on preview69.** Both authored Auto archives were loaded directly, as Auto saves, in fresh processes. Root 20260923-chunk5-P07-area-cross-entry-auto-F (PID17440) PASS24/0 loads the after-entry archive f4ef4d57aab073805559a7f80f72b199b4a606f9b02ae291e6b45d586bbd8e16 and opens **destination** area c49315fe499f0e5468af6f19242499a2. Root -exit-auto-F (PID17508) PASS24/0 loads the before-exit archive 290d4e12f7bd700d517731c4074f222df2e50a056c0e1a4b59cf3fd7e78ab9dd and opens **departure** area 9d1278a2f599b2a4daab53abdfe88d2e — it inherits no destination identity, no pending transfer and no spurious area resume from the process that produced it, recording suspensions0/resumes0/pendingFalse. Each then supports a distinct subsequent ordinary write in the world it actually opened, mounted, at Manual_1_KMC_P01.zks: 3dd9ba585470f139 in B and a19076831045775e in A. Neither write touches the archive it loaded, and both source archives are byte-identical afterwards.

Two native contracts had to be established for these cases rather than assumed. A transition autosave is a native **Auto** save, so the cold loader must expect that exact type; expecting the destination Manual leaf rejected the run at its own descriptor check. And SaveManager.FindUnusedSaveNumber06008024 maxes only over saves of the **matching type**, so an isolated root holding just the Auto source starts native Manual numbering at 1, not at the 300 a staged Working fixture produces — declaring the wrong leaf made PrepareSave06008025 refuse its own descriptor and collapse the mode to None. The generic cold rule that a cold process writes nothing is therefore scoped, not removed: exactly these two cases may make one write, and two new protections were added alongside — the loaded archive must be byte-identical afterwards, and the write must be a distinct leaf. Owned fixture negatives reject swapping Manual for Auto, BeforeExit for AfterEntry, either area, the campaign, the hash or the source role.

**Profile identity and the game's achievement cache.** The earlier -save-F attempt completed its scenario 34/0 but was FAIL overall on blocked restoration, and that root stays FAIL; the cause was neither KMC nor the owner's SkipIntro mod. The failing check compares only the LocalLow profile tree, while UMM's Params.xml lives under the game install directory and is compared separately, after that check had already thrown. The real drift is the installed game's own achievement cache: the profile root holds dozens of fixed-size achievements.dat<suffix> leaves dating from 2024, and the game adds or rewrites one on many launches. The first exception drawn around that family was too weak, and review caught it before it could hide a real loss. It recognized `^achievements.dat[^/\]{0,32}$` and accepted a length of either the native size **or zero**, while retention checked only that the path still existed — so a preexisting settled 12288-byte cache truncated to zero satisfied both checks and reported as untouched.

The exception is now a **before/after transition** rule rather than a per-entry one, derived from the retained observations and a bounded read-only inventory of the actual profile root. The native filename contract is a direct child of the profile root matching `^achievements\.dat[^/\\.]{0,11}$` — an unescaped dot and a 32-character tail admitted lookalikes, and a dot in the suffix admits unrelated extensions, so both were tightened. The settled native size is 12288. A leaf that **already existed** must still exist and keep its exact prior length; the game may rewrite its bytes, but it may not change its size and may not empty it. A leaf that is **new** must arrive at exactly the settled size. Every other transition throws, so nonempty-to-empty is a failure, not an accepted change. Deletion, nested paths, unrelated leaves of the same size, invalid sizes, Params.xml and PlayerPrefs all remain fully protected, and the raw before/after inventories with both hashes are retained.

Reporting is corrected to match: accepted churn is written to `profile-cache-changes.json` as **expected external changes** with path, transition, length and before/after SHA256, and `profileBytesRestoredExactly` is true only when that list is empty. A run with accepted cache churn no longer claims that all profile bytes were restored exactly. Profile protection checks rise from 20 to 38; the unjustified `achievements.database` positive was removed rather than any negative being weakened, and new negatives cover new-leaf sizes 0/4096/24576, truncate-to-empty, growth, deletion, an empty leaf becoming populated, and nested, unrelated and dotted lookalikes at both sizes. The owner's achievement data was not reverted and no cache or setting snapshot is ever auto-restored to make a guard pass; the stale lock from the blocked run was released through the owned transaction recovery helper after a passing WhatIf.

Historical detail for the superseded -save-F root: Its gameplay evidence is nonetheless the distinct before-exit artifact: the autosave is 848209 bytes captured in the **departure** area 9d1278a2f599b2a4daab53abdfe88d2e with a barrier of snapshots1/suspensions0/resumes0/Mounted, confirming it precedes suspension and does not contain destination state, while the transfer still completed to c49315fe with one suspension/one resume, retained views, Success movement over 2.956m and a destination write cd0dd33060e7e154c9983d0aab10986dea4f014f7481f8b1a65092b701b7ffa9.

**P07 cross-area result: persistence qualified through the post-transfer write; ordinary continuation blocked by destination terrain.** Root 20260923-chunk5-P07-area-cross-entry-save-D (preview62) reaches native/outer 25/1. The transfer is real: source 9d1278a2f599b2a4daab53abdfe88d2e to destination fd1b6fa9f788ca24e86bd922a10da080 across 88 loading frames with one suspension and one resume. Rider view -339308 and mount view -340732 are retained across that genuine area change, alive, entity-bound, still the pair's own views, one native actor each. The authored after-entry autosave's own native header barrier records snapshots1/suspensions1/**resumes1**/relationship Mounted/area destination with the exact pair, which is the direct proof that restoration precedes the after-entry autosave; the Auto archive is 889629 bytes, mounted, in the destination area. The post-transfer Manual write commits in the destination with the mounted pair and zero Mount casts.

The single failure is the shared ordinary-movement continuation in the destination, not persistence. It is deterministic and now fully characterised: from (-25.8238926, 11.7294607, 25.18078) the mount reaches (-25.3390751, 12.929203, 27.08651), 1.97m of progress with a 1.20m climb, then an empty A* path and native Interrupt 1.19m short of an approach point the trace placed at the origin's elevation. Party movement had settled to zero, stuckTime was 0, no navmesh obstacle or static block was reported, and all eight full-footprint probes at corpulence 1.06 returned residual 0.0, so both the crowding and footprint-occupancy hypotheses are disproved. This is a located fixture-terrain blocker of the same class as the historical BD/BF physical-location failures at this hub; no threshold was reduced and Interrupt is never accepted as success. Resolving it needs a fixture decision — an authored return transfer into the qualified Area_Dwarf_1 terrain, which also needs that area's own enter-point GUID and two suspensions/resumes, or a different arrival point — not another replay. Details in AUTONOMOUS-BLOCKERS.md.

**P07 same-area result.** Owned roots 20260922-chunk5-P07-area-reload-save-A (PID14548, native/outer33/0) and -load-A (PID15700, native/outer20/0). The save process measured rider view -339308 and mount view -340732 before `Game.ReloadArea` and the **same two instance IDs** after it across 63 further native loading frames, both alive, entity-bound, still the pair's own captured views, one native actor each and no mounted-invariant error — the retained contract the installed IL predicts. Real unload and one suspension/one resume were observed, zero Mount casts, three facts/zero duplicates/two owned slots, and conserved native debt. The post-area Manual write ec7785237877e58a365ad0631988d0f2b4efa03f0abd6e54976d2e16901c3471 differs from the initial f6aa1d94e98f44c63a386e291ac0397bbfc4c959aad6b06136943f9c76cbfa60, native play resumed, and real movement plus a delivered native attack followed. A separate fresh process then cold-loaded that exact archive: same pair IDs, semantics2/presentation1, three facts/two slots, no Mount cast, then movement, attack and usable continuation. Both transactions restored actual human preview.54 intake, protected saves, settings, caches and foreign Mods. Lab p07-58-ledger.json SHA256 cb80040213a4f3679d97adc228921be4ef548ab7566ba1d3fde4550c862f4957.

Preview56's two damaged-combat native/outer PASS49/0 cases remain in lab p06-56-ledger.json SHA256 f1a6a274f44e94be9a26a31264a0f2ad4d125ade8e997770c985cb869fabd711. Earlier payload identities and original failures remain in the journal.

The legacy case exposed native LoadGameFromMainMenu disposing views before LoadRoutine. Preview52 releases live mounted leases after admission and before disposal, using no-End/no-forfeit housekeeping; rejected loads leave the original world untouched. Both accepted B/A transitions and three native refusal entries are now measured, followed by usable ordinary movement/attack. P06 metadata campaign mismatch and damaged combat participation are qualified; a different native-header campaign still needs its own case. The first damaged-combat trial exposed LoadRoutine finishing before native turn-controller initialization. Preview56 makes missing-controller/actor failure final only after the full native load queue finishes.

## Restoration and removal

This mission's 105 native transactions on the completion candidate, and every failed run on preview.99-104, restored the actual intake (`modsRestored`/`workingRestored` true); final restoration **2026-09-25T01:08:15.9725085+00:00** after `final105-p08-mammoth-tb`. The preview.98 set (26 transactions, final restoration 2026-09-24T02:18:33.5458033+00:00) is historical. Human preview.54 DLL 2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9 installed, 275 saves, all seven Mods directories, both automation fixtures byte-identical (BASELINE c29d965c…, WORKING 5eb4e0b4…), BASELINE immutable, no game process, no runtime lock. Earlier restorations below are historical.

Both preview69 P07 transition-autosave processes exited and restored their actual intake; final restoration **2026-09-23T05:58:01.4574559+00:00**, both with `profileBytesRestoredExactly` true and zero accepted cache churn. Human preview.54 DLL 2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9, 275 saves, settings, caches, all seven Mods directories and the separate preview.37/preview.13 backups remain preserved; both automation fixtures are byte-identical to their recorded intake (BASELINE c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512, WORKING 5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6) and KMC_AUTOMATION_BASELINE stayed immutable. No game process or transaction remains.

The owner's SkipIntro mod is installed deliberately to shorten boot time for automated runs, and its actual registration in UMM's Params.xml is preserved unchanged. It needed no special-case setting and no broad Params or PlayerPrefs exemption: the mod-directory check simply accounts for it. A separate PlayerPrefs drift (EternalKingdom, KingdomDifficulty and Unity session counters) seen on an earlier root did **not** recur on either clean preview69 run, which confirms it was an artifact of that run's abnormal deadline termination rather than of normal operation — so no prefs guard was widened to accommodate it. Earlier exact analytics/profile recoveries and original failures, including the preview57 area failure, are historical journal evidence. Every next transaction requires fresh current-data checks.

The preview44 preparation timeout raised native LoadGameException and reset the world. Its exact profile drift was recovered through pinned one-run guards; that failure remains FAIL. Preview53 qualifies graceful pre-serialization timeout/cancellation recovery. Preview54 additionally qualifies locked-destination commit failure recovery. Cancellation during native serialization and the bounded Prepare-to-Disable/removal contract are natively qualified on the completion candidate (`final105-p07-cancel`, `final105-p07-output` + cold, `final105-p07-removal`, `final105-p07-absent`, `final105-p07-removal-no-dll`, `final105-p07-disable-load`). Permanent custom Horse/feature dependencies are separate from transient pair metadata. Arbitrary DLL deletion is not certified; valuable campaigns must not be stripped. No permanent candidate deployment, main merge or public release is authorized. The owner approved only the DLL package-entry limit from4 to5 MiB; ZIP/Info limits, allowlists, hashes, dependencies and runtime protections are unchanged.

Removal instructions for a human install: open KMC's UMM panel while a game is
loaded, dismounted or mounted, outside combat and outside a save or load; press
**Prepare to disable / remove KMC**; read the status line. `Ready` names the
new cleanup save (`KMC_CLEANUP`) written through the engine; load that save (or
any save the status accepts) before disabling the mod in UMM or deleting the
DLL. `Refused` names the reason: a permanent KMC Horse reference in the loaded
world or party means that campaign depends on the mod and must not be stripped;
an in-flight save/load or a forbidden save state means wait and retry. Removing
the DLL while a save still carries a KMC Horse unit or feature is not certified.

Manual checklist after engineering qualification, using an authorized disposable save:

1. Mount the existing pair, move/attack, and note remaining actions.
2. Make a real save; verify controls, relationship and remaining actions still work.
3. Quit fully, launch a fresh process, and load that exact save.
4. Verify the same actors and legitimate remainder; advance two paired activations.
5. Save again; repeat quick/auto and a copied/renamed archive round trip.
6. Press Prepare to disable / remove KMC while a pair is mounted; confirm the
   status names a new `KMC_CLEANUP` save and that the archive lists in the
   native load menu.
7. Quit fully, disable KMC in UMM or move the DLL out of `Mods`, launch a fresh
   process, and load that `KMC_CLEANUP` save with no KMC DLL present; confirm
   the party opens dismounted with no missing-blueprint errors. Restore the DLL
   afterwards. (The automation performs this exact observation natively through
   the removal observer, `final105-p07-removal-no-dll`; the human step is the
   owner's own confirmation of it.)

8. With a party member holding the KMC Horse companion feature, press the button
   again and confirm it refuses with the permanent-reference reason.
