# F2 review handoff

## Disposition

**F2 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER ACCEPTANCE SEPARATE.**

ART-02, CTRL-01, CTRL-02 and CTRL-03 are implemented and demonstrated natively:
- **Repaint:** the toolbar redraws after the one automatic placement.
- **Legacy migration:** an authentic old toolbar row is migrated in place.
- **Slot cycle:** Dismount → Mount works through the public slot.
- **Native saves:** one kept and one removed control are written.
- **Separate-process reload:** restores the kept control at its slot, drawn and usable, and remembers the removed control across a load.

Restoration was exact on every run. Nothing was merged, tagged, released or installed, and no F3 work was started.

## Identities

- Repository howardreith/KingmakerMountedCombat, branch `codex/kmc-finalize-f2-canonical-controls`, base `66e96f90` (F1 merge). Previous review heads: `befbd73c`, `68727845`.
- The current head is in PR #17. Only docs commits follow the consumer candidate.

**Producer F2f** (run `20261105-finalize-f2-r05`):
- source `ff1f2e5a1200b3f2ecfe3be04304a08768e9e280`
- DLL `9e4180bacd6258a2d0a450d13faeb810b5ced2df82265eb5f92460fed2cbe81e`, MVID `59977cd3-163e-4641-b8d3-9d5a23a035fe`
- package `KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f2f-diagnostic.zip`, `2b2c9699ffeec28cf2120e16e39d7f279d1fc379e51aef77d29e5ad0e61c68b2`
- suite `finalize-f2f-ff1f2e5a-20261105`, `beafa8dd9bfd185982617e40ce5a90a11dc43370b2f58ddf45694921f5ba4964`

**Consumer F2g** (run `20261105-finalize-f2-r07`):
- source `ffb5c3281fe5e51740fbd2233e0c15ea638a00b7`, src tree `1a9965350dff0b5cda11260ca68943739b9e820c`
- DLL `99ea4a0993e9622427aa6599762da6b47190f5d6f326c538fd698fd5b2403991`, MVID `2bcaa58f-83de-462d-abdc-d6706ffa1379`
- package `KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f2g-diagnostic.zip`, `801478566be4de4dab1f9056bc63fca71aedc3c391d925af49a8473cee76fae1`
- suite `finalize-f2g-ffb5c328-20261105`, `5bcd2385cde026949aa400fe7240cbeeddb38febcef6f18ee9f6e078836ebac6`

**Superseded candidates:**
- F2e `a44ead69` (r04).
- F2d `7879e463`, which never ran natively.
- Production code from F2d to F2g differs only in `CompositionRoot.cs`. That change keeps this automation route from selecting the retired helpers, the same as ordinary play. Everything else is Diagnostics, harness, tests or docs.

**Environment:**
- Kingmaker 2.1.7b.
- Base-game-only KMC staging with the archives' retained dependencies: Call of the Wild 1.14.4c and Craft Magic Items 2.1.0 (`analysis-cache/sprint1/retained-dependencies-final211.json`).
- Rider `b6628a77-4962-47a4-a17c-88d9836fc9d5` with Mammoth `d79a4f6c-b74e-4868-95bd-533899131acb`.
- This is not proof of the owner's mod stack.

## Inputs and outputs

| Role | File | SHA-256 | Bytes | KMC rows |
|---|---|---|---|---|
| Authentic input (released preview.211) | `Quick_1.zks` (run `final211-p05-save-quick`) | `83e92e1ae1a38dda731a32aff6b4ff6ad424e5e5f6777f758db6832a8eb7122b` | 848858 | schema 2: Dismount 2@14, retired rider primary 3@15 |
| Kept (r05 output, r07 cold input) | `Manual_1_KMC_P01.zks`, internal `KMC_P01` | `7b0ac4bcc3ce646063a1c0585a0ebc9d2eb3fe80332c5172bd1a336fad3dc703` | 870781 | {6@9, 7@0} |
| Removed (r05 output, r07 second input) | `Manual_2_KMC_F2_REMOVED.zks`, internal `KMC_F2_REMOVED` | `b323e108d66b290fbc580e5cff21e14a6eed3c70779a0ddec1c0e5d07b0a46dc` | 870363 | {7@0} |

- All archives: campaign `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f` ("cvb"), area `9d1278a2f599b2a4daab53abdfe88d2e`.
- Correction to the earlier record: the authentic old-toolbar source is **preview.211**, not preview.103. Its rows are kind 2 at 14 and retired kind 3 at 15. Old kinds 1 and 5 have offline coverage only, because no retained archive carries them.

## Producer/consumer bridge (owner-selected, exact)

- **Why it exists:** the reader accepts a consumer only on its producer's exact build. The consumer needed a corrected diagnostic build, the r06 bootstrap fix and the capture handling, so the owner selected one narrowly pinned allowance instead of a producer replay.
- **The declaration:** `Get-KmcFinalize2ProducerBridge` in `scripts/runtime/Finalize2UpgradeEvidence.ps1`. It applies only when the commit or DLL differ, and it requires all of the following:
  - **Producer:** exactly r05, with its commit, DLL, MVID and suite id/hash. Its own suite snapshot must freeze the declared commit, package, DLL and MVID. Its PASS result and all five restoration receipts must be present, and its two native write observations must carry exactly the declared hashes and lengths.
  - **Consumer inputs:** exactly the two declared archives, matching name, internal name, hash, length, campaign and area. They are ordinary copies in the owned isolated root and read-only (authority entries pinned by hash).
  - **Consumer source, read from git rather than from a claimed label:**
    - `git rev-parse <commit>:src` equals `1a996535…`.
    - `git diff --name-only ff1f2e5a <commit> -- src` is exactly `Diagnostics/PersistenceIsolationBootstrap.cs` and `Diagnostics/RuntimePersistenceFinalize2Scenario.cs`.
    - The consumer's suite snapshot freezes its commit, DLL and MVID.
  - **Actors and process:** the same actors, in a different process.
  - **Existing checks:** every behavioural, archive, quota, path, no-write and restoration check still applies.
- **Change between F2f and F2g, diagnostics only:**
  - The bootstrap's enumeration expects the consumer's two declared saves. The entry declaration moved unchanged into `DeclareSaveEntries`; the decision is `ExpectedEnumerationCount`/`AdmitsEnumeration`, which adds a distinct-name rule.
  - The capture closes the mod-manager overlay for its frame and records that it did.
  - Gameplay, the save format, serialization, control capture/restore, placement policy and binding are unchanged.
- **Evidence ownership:** r05's observations and bytes keep F2f's identity; r07's rows carry F2g's DLL.

## Native outcomes, each at its original verdict

| Run | Candidate | Verdict | Outcome |
|---|---|---|---|
| r01 | F2a `c19dcc44` | FAIL | The fixture's main bar was full, so the cycle had no slot (experiment changed). |
| r02 | F2b `5017fe0e` | FAIL | Re-evaluated with two reader-only corrections: still FAIL, because the harness's own selection dropped the Charge frame. |
| r03 | F2c `66bed248` | FAIL | Toolbar not repainted after placement. Real defect, fixed in F2d. |
| r04 | F2e `a44ead69` | FAIL (39/1) | Repaint, migration and Dismount observed. The Mount click came during the native Move cooldown; observer timing fixed. |
| r05 | F2f `ff1f2e5a` | **PASS** | Producer, detailed below. |
| r06 | F2f `ff1f2e5a` | FAIL | Refused at the isolation bootstrap before any load (expected one save). Harness gap, fixed. |
| r07 | F2g `ffb5c328` | **PASS** | Cold consumer through the declared bridge, detailed below. External reader included. |

**r05, producer:**
- **Legacy mapping:** old 2@14 became the live public control at 14 plus its record (one migration); slot 15 was cleared, as in ordinary play.
- **In-session removal:** stayed removed for 30 frames.
- **Repaint:** slot 13 was drawn empty. After the disclosed `BeginLoadedWorld` record reset (this proves the placement and repaint path, not a natural first acquisition), placement happened at frame 4600 and the control was drawn by frame 4601, with no other refresh.
- **Slot cycle:** Dismount ran at once. Mount waited 2.18 s for the native Move cooldown (2.82 s at the start), then produced one exact shell.
- **Saves:** after a swap to slot 9, two native Manuals had exactly the rows above. No native member named a KMC control identity.

**r07, consumer (new process):**
- **Kept archive:**
  - Saved and live rows were {6@9, 7@0}. One public control at 9 was bound to the loaded rider's live fact, with its placement record.
  - Counters: no placement attempt, no migration and no cast at load.
  - The main bar matched the producer's pre-save bar, no hidden shell was exposed, and all strings resolved ("Mount / Dismount", hidden shells, mounted delivery "Charge").
  - The visible bar drew the control at 9.
  - The ordinary continuation (movement plus attack) ran.
  - Dismount worked through slot 9. Mount waited 2.76 s for the native Move cooldown (2.82 s at the start), then produced one shell. The slot and fact never moved.
- **Removed archive, loaded natively in the same consumer process:**
  - New actor objects, pair restored, rows {7@0}. No binding came back, the fact and record were present, and the bar matched the producer's.
  - The control stayed absent through 60 update frames and a reselection from party member `d17c8fd0…` back to the rider.
  - The drawer-entry Dismount/Mount cycle ran (122 watched frames, no reinsertion, no placement).
  - Both inputs were unchanged and there were no writes.

**Restoration on r04–r07:**
- Mods and Working restored, baseline immutable, save-protection and write-allowlist checks passed.
- Save inventory digest stayed `60e3d6cc…a1`.
- No game, lock or installed candidate remains, and the live Mods tree is empty, as at intake.

## Visual packet (private, lab paths; not committed or published)

- `runtime-evidence/20261105-finalize-f2-r07/movement-visuals/finalize-f2-p05-cold-kept.png` (`ca595344…`): the main bar shows the Mount / Dismount icon in slot 9.
- `…/finalize-f2-p05-cold-removed.png` (`9837ade3…`): after "Game loaded", slot 9 is empty, and the combat log shows the drawer cycle's Dismount and Mount Companion.
- Both were captured with the mod-manager overlay closed (recorded `ummOpenAtCapture=false`) and the overlay reopened afterwards.
- The r04/r05 frames are covered by the mod-manager window and do not show the toolbar. The repaint claim rests on r05's structured visible-bar reads, not on those images.
- The r03 frames show the icon in place and in target selection.

## Checks

**Offline:**
- Candidate on `a8dd5875` and on `ffb5c328` (the consumer candidate): 19/0 and 11/0. Fast with focused checks on each commit in between.
- F2 upgrade reader self-test 77/0:
  - producer and consumer evidence;
  - bridge positives, plus negatives for an unlisted producer or consumer source, a source change outside the declared files, wrong producer identity or package, changed, swapped or resized archives, an unfrozen consumer DLL, the same process, a non-PASS producer and a producer without exact restoration;
  - the real declaration against r05's retained receipts and bytes;
  - a pin that the acceptance call passes no override.
- `Test-Finalize2ConsumerEnumeration` 14/0: production's own `DeclareSaveEntries`, `ExpectedEnumerationCount`, `AdmitsEnumeration` and `PersistenceSaveAuthorization`, run over ordinary copies of r05's archives with the real consumer request shape.
  - Admits exactly the two inputs (both read-only and hash-pinned, no writable output).
  - Refuses one missing, an extra, a duplicate, an alias, a foreign campaign and a wrong hash.
  - The single-input route still expects one save.
- Kind-6/7 codec regression: Test-PersistenceData 91/0.
- Seven repaired persistence test compile lists, as recorded before.

**Live request validators** (external and compiled):
- The exact r07 request was admitted.
- Refused, by both validators: an unlisted scenario, a foreign branch, F2 without the upgrade chain, no removed archive, a wrong removed leaf and aliased archives.
- The reader's real bridge predicate admitted r05 for this consumer and refused an unlisted archive.
- WhatIf w08 PASS.

**Independent read-only reviews:**
1. `befbd73c..a8dd5875`
2. the r04 timing fix
3. `68727845..cadbc224`

None found a blocker, and every should-fix was applied. Neither of the first two caught the r06 gap.

## Limitations

- **Old kinds 1 and 5:** offline coverage only.
- **Downgrade:** older KMC builds read an F2 archive with kinds 6/7 as invalid metadata, so rollback is unsafe for F2-written saves. Keep pre-F2 backups and the matching older package.
- **Build identity:** several development builds share the preview.103 label; full source/DLL/package identity names a build.
- **Removed half:** it was loaded in the same new consumer process as the kept half, not in a further separate cold boot.
- **Cosmetic:**
  - The selected-slot glow does not show while a swapped shell is in target selection.
  - Hidden shell names appear in combat text.
- **Not updated:** historical no-autoplacement diagnostic scenarios.
- **Out of scope:** F3 owns post-Charge movement and the action preview.
- **Owner decisions remaining:** owner art approval and acceptance on the owner's own laptop stack are separate.

## Spend and state

- **Budget:** `finalize-f2` 44.87 of 60 min. Starts: 7 of the authorized 8 (r01–r07); the eighth was not needed. The start cap is procedural, not enforced in code.
- **Not changed:** no time increase, reset or new budget id.
- **Push:** audited ordinary fast-forward push. The guarded helper does not admit this branch and was not changed.
- **Left intact:** the rejected local draft branch and the failed runs.
