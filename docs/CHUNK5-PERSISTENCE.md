# Chunk 5: save-scoped persistence and cold-load recovery

Status: IN PROGRESS. Integration descends from reviewed `8a297fa019ff205f50fd7b911b6f6e03d46fbb1a` and native-qualified source `429377d707a9976be65639e0c27954d8b4ff3717` / preview.54. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO.

## Storage, transaction and resume semantics

IN PROGRESS. Exact installed SaveManager/ISaver/ZipSaver inspection and owned native archive I/O establish an in-transaction primitive member seam. Use extensionless `kmc-mounted-state`: the installed loader treats unknown `.json` members as area state. Primitive UTF8 metadata survives native commit, clone and rename; no post-finalization rewrite is needed. Preview.7 implements the bounded schema, per-enumerator snapshot at native header serialization, current native actor debt at PostLoad and dedicated validated pair/control attachment. The first actual native write exposed global serializer interference, repaired with an independent serializer and a reproduction regression; P01-save-D now passes23 native assertions including ordinary movement/attack and exact outer restoration; cold qualification remains open; combat state remains explicitly unsupported in this development slice. Save-specific state must travel in the archive and preserve spent and unused actions without repeating preparations.

## Qualification

| Scenario | Required behavior | Status |
|---|---|---|
| P01 | Manual write, same-session continuation, exit, cold load, movement/attack | IN PROGRESS |
| P02 | TB remainder, participation and two subsequent paired activations | TODO |
| P03 | Step/conversion/reaction/conditions and exactly-once rounds | TODO |
| P04 | Native RT active-command snapshot and loaded outcome controls | TODO |
| P05 | Manual/quick/auto, rotation, copies/renames, alternate saves | TODO |
| P06 | Legacy/current/invalid/future schema and campaign isolation | TODO |
| P07 | Failed/canceled operations, views/areas, disable/removal | TODO |
| P08 | Final accepted gameplay regression including post-load variants | TODO |

COMPONENT / ASSEMBLY CONTRACT / NATIVE INTEGRATION / HUMAN PLAY remain distinct. Preview.4: source22/0, focused native archive/write-lease contracts23/0, primitive data28/0 (including native JSON-default regression); unchanged preview.3 components404/0 and harness250/0, owned-copy guards8/0. Profile/settings guards8/0 and actual installation registration6/0 remain applicable. Logs remain in `analysis-cache/chunk5-persistence`; no P01-P08 runtime qualification is claimed.

## Isolation bootstrap and exact seams

- `persistence-isolation` copies only the admitted Working fixture into a run-owned lab profile. Native enumeration/descriptor/stash paths are redirected and cloud operations suppressed for that process. Existing strict write denial remains active; the exact read archive stays unchanged. Root/campaign/leaf/type/hash authority rejects traversal, aliases, links, foreign campaigns and unknown existing files. The P01 save scenario now admits one explicit native Manual destination, leases its actual prepared writer, constrains ZIP temporary files to the same root and requires real commit before load admission. Native write qualification is pending.
- Authorization precedes save/load cleanup. Per-enumerator control scopes unwind on completion, failure and disposal. The new ordinary save path retains the pair and suspends only serialization-sensitive controls and AI fields; native same-session qualification is pending.
- `SaveRoutine` creation precedes serialization. The native routine waits, allocates a temporary descriptor, takes a screenshot, turns entities off and starts worker serialization. Iterator completion may precede final commit/overwrite. Native `LoadRoutine` mutates its header counter; test isolation suppresses only that selected archive's scoped SaveJson/Save calls, never claims a write occurred, and retains source bytes.
- Native TB load reconstructs controller state; late visual restoration alone cannot preserve legitimate grants. Snapshot and early semantic rebind remain required.

Exact assembly MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Proprietary inspection stays outside Git. Detached checks prove method construction and real owned native archive behavior, not Unity lifecycle or gameplay.

Bootstrap history: A failed before launch on culturally ambiguous profile ordering, now fixed with ordinal digest regression. B failed before fixture loading because Harmony12 rejected the iterator MoveNext patch; preview.2 replaces it with narrow native ZipSaver hooks and an exception-safe selected-load scope. Full actual WhatIf purity PASS on source `8612544`; the preflight/WhatIf branches remain unchanged. Isolation-C PASS14/0 native and PASS outer on preview.2/source1b8747e, with normal process exit and exact actual intake restoration. P01-save-A/source4cb95af made a real native Manual write, then FAIL0/1: global JSON defaults reduced pair metadata to11 bytes (`{"$id":"1"}`). The live pair was still mounted. No cold load was attempted. Actual intake restored at18:59:42Z after normal process exit. Preview.4 uses an independent serializer for storage and observations; P01-save-B confirms native commit and same-session pair/control/debt retention, but FAIL0/1 on diagnostic pointer priority. Actual archive SHA ddeb73d9367ac62e35ae04394cd4f523a6c54c183347ef2a4b800caf3cf83403,847753 bytes. Native SetAbility(null) enters Ability mode; preview.5 fixes only that fixture call with native ClearPointerMode and an explicit mode assertion. PID15416 exited normally; actual intake restored19:07:14Z. P01-save-C completes the actual write and normal mounted movement, then FAIL0/1 at stage4 timeout in native Pause: the fixture checked initiative readiness before its unpause call. Preview.6 fixes that ordering, maintains the established target memory lease, and records native pause/readiness and partial counts. Gameplay/serialization are unchanged. PID14496 exited normally; actual intake restored19:16:46Z. P01-save-D PASS23/0 native and PASS outer (preview.6/sourcef81749d): real write, unchanged live pair/controls/debt, ordinary movement and attack, normal PID16336 exit, actual intake restored19:21:29Z. Archive SHA84ffb91c85fe10b0ccacc92befe9a1e4fd2be88e639f197f79bf9bd678c83270. Fresh PID15624/P01-load-A FAIL1/1 before attachment; early PostLoad wrongly required GameId before native assignment. Exact source inspection confirms PlayerState.PostLoad precedes setting GameId from the selected header. Preview.7 binds the new Player object during the actual load enumeration and checks campaign/area before presentation; components408/0 and archive contracts23/0. Load-A source bytes unchanged and intake restored19:23:46Z. Next p01-load-world/suite8/P01-load-B from the same D archive.

## Intake and restoration

Host DESKTOP-SRJJ623. Actual installed human preview.54 DLL SHA256 `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9`. B's process exited normally; Mods, saves and profile/cache bytes were unchanged/restored. Startup added one default SkipIntro UMM entry and changed three measured PlayerPrefs values. The repository recovery helper restored exact intake at `2026-09-20T17:59:01.6829430+00:00`; `runtime-evidence/20260920-chunk5-isolation-B/profile-recovery.json` PASS. Original native FAIL evidence remains intact. No game or lock remains. The narrow checked restoration is now in the normal finally path. Snapshot actual intake before each batch; preview.37/preview.13 are separate historical backups.

## Removal and manual review

TODO: implement and qualify the bounded Prepare-to-Disable path. Permanent custom Horse acquisition dependencies remain distinct from transient metadata; arbitrary DLL deletion is not certified. After native qualification: save an owned fixture while mounted, continue, quit fully, launch/load that save, inspect pair/controls/remaining actions, advance two activations, save and repeat. No permanent candidate installation or release is authorized.
