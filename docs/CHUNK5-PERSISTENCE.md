# Chunk 5: save-scoped persistence and cold-load recovery

Status: IN PROGRESS. Source starts at integration `50ebd9be905eeb1fb375c33580fb405bc829f799`, qualified gameplay `429377d707a9976be65639e0c27954d8b4ff3717` / preview.54. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO.

## Storage, transaction and resume semantics

IN PROGRESS: exact installed SaveManager/ISaver/ZipSaver inspection and owned native archive I/O establish an in-transaction primitive JSON member seam. A metadata addition is deferred until native Save(), survives a fresh saver, native header update and rename. No gameplay persistence format, snapshot/rebind implementation or runtime pass is claimed yet. Save-specific primitive metadata must travel inside the actual archive, with schema validation and no external pair state. Restoration must preserve spent and unused actions without repeating preparations.

## Qualification

| Scenario | Required behavior | Status |
|---|---|---|
| P01 | Manual write, same-session continuation, exit, cold load, movement/attack | TODO |
| P02 | TB remainder, participation and two subsequent paired activations | TODO |
| P03 | Step/conversion/reaction/conditions and exactly-once rounds | TODO |
| P04 | Native RT active-command snapshot and loaded outcome controls | TODO |
| P05 | Manual/quick/auto, rotation, copies/renames, alternate saves | TODO |
| P06 | Legacy/current/invalid/future schema and campaign isolation | TODO |
| P07 | Failed/canceled operations, views/areas, disable/removal | TODO |
| P08 | Final accepted gameplay regression including post-load variants | TODO |

COMPONENT / ASSEMBLY CONTRACT / NATIVE INTEGRATION / HUMAN PLAY evidence remain distinct. Bootstrap candidate `0.1.0-chunk5-preview.1`: source validation 22 PASS / 0 FAIL; components 401 PASS / 0 FAIL; existing guarded harness 250 PASS / 0 FAIL; focused assembly/storage contracts 15 PASS / 0 FAIL. Logs: `analysis-cache/chunk5-persistence/{tests-bootstrap3,harness-bootstrap,contracts-authorization}.txt`. P01-P08 have not run.

## Removal and manual review

TODO: establish the bounded Prepare-to-Disable path; custom Horse acquisition dependencies must remain distinct from transient pair metadata. Arbitrary DLL removal is not certified. Manual checklist after native qualification: create an owned test save while mounted; continue playing; quit fully; launch/load that save; inspect pair, controls and remaining actions; advance two activations; save and repeat.

## Intake and restoration

Host DESKTOP-SRJJ623. Actual installed preview.54 DLL SHA256 `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9` matches the accepted source package. Main intake was clean and tree-identical to the integration descendant; returned to the integration branch. No game process observed, no external changes or transaction. Re-snapshot actual human data and installation before any guarded batch; historical preview.37/preview.13 are not restoration targets. Private bootstrap packaging is the next checkpoint; no package is qualified for persistence.

## Bootstrap implementation and exact seams

- `persistence-isolation` reuses the schema-v2 strict Working loader and original write ban. The guarded transaction copies only the admitted Working archive into its run-owned lab profile. Native enumeration, descriptor paths and area stash are redirected; Steam replication/upload is suppressed within the owned process. Native load-counter disk writes are suppressed only in this isolated mode, retaining the selected archive bytes. The process retains isolation through shutdown. No run may switch back to human roots while live.
- The isolated-file service snapshots exact leaf/name/native type/campaign/area permissions, rejects ambiguous or unexpected entries, baseline names/hashes, traversal, reparse ancestry and hardlinks, and leases actual write completion. It is not yet enabled as native write authority. Old scenarios remain strict.
- Save/load authorization now precedes relationship cleanup; only the explicitly armed old suppression probe retains its diagnostic cleanup. Control suspension starts on enumeration, with exactly-once exception/disposal cleanup. The missing gameplay snapshot/restoration path means ordinary saves still use historical dismount behavior in this unqualified development candidate.
- `SaveRoutine` is an iterator: entry/creation is not serialization. It waits for prior native commits, replaces the descriptor with a temporary new slot, prepares metadata/screenshots, turns entity state off, and runs serialization on a worker. Enumeration may finish after serialization but before final archive replacement. `Save()` commit and final slot replacement require separate evidence.
- `PrepareSave` constructs paths independently of `SavePath`; list refresh also resets its field. `AreaDataStash` caches its own path and native load clears it. `LoadRoutine` ordinarily updates the header counter; this is not a KMC metadata migration. Native TB load rebuilds controller state, so semantic rehydration must precede renewed preparation.

The exact assembly MVID is `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Bounded proprietary inspection stays in `analysis-cache/chunk5-persistence`. Detached CLR construction of `PrepareSave` fails on Unity ECalls; actual IL transformation passes, but only an owned Unity process can qualify full hook construction. Earlier probe/compiler failures are retained in that cache. No inference from detached tests qualifies P01 or action continuity.