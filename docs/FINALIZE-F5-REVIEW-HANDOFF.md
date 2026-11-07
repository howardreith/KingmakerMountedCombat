# F5 review handoff: companion naming and a fitted Riding Dog saddle

## Disposition after the completion mission (2026-11-07)

**F5 ENGINEERING PARTIAL — CORRECTED SADDLE STILL MIS-RENDERS NATIVELY; COLD CONSUMER NOT RUN; OWNER ACCEPTANCE SEPARATE.**

The completion mission (review baseline PR #20 at 83863820) raised the start ceiling from 3 to 6 on the same finalize-f5 record. The 60-minute ceiling did not change. One corrected producer ran (start 4). Its game side passed every row and wrote the native Manual. The outer reader then refused the saddle fit, and the captures show the same defect: the pad does not drape but renders as thin red shards standing out of the back, plus a strip down the flank. The cause is not yet diagnosed. The remaining 17.48 minutes cannot cover a corrected candidate plus both admissions: Candidate is about 4.4 min, freezing about 1.5 min, and each launch reserves 10 min. So no further start was used. The consumer never ran, because a FAIL producer is not a source.

**Start 4: `20261106-finalize-f5d-producer-04`. FAIL, kept.**
- **Identity:**
  - Commit d32fdc28 on the attached branch.
  - DLL `119e7a98adcb632bd1a33bc67fb4d128513af021ccc9df5d3f083e9132f0439c`, MVID 31f3c7c9-2d58-4960-a8ab-4ac3067bdf43.
  - Package finalize-f5d `5cf589cb1e8f806ec0df7a6966797574b573d914fe8cebedf65cccb96076354c`.
  - Suite `finalize-f5d-d32fdc28-20261106` `c8b233aa9ce850992e29be769fd211a41aec855ab7a93dafa7fc64dfedf8571d`.
- **Game side: status PASS.** It covered both mounts, the all-pair archive, continuation, cleanup and every naming row:
  - sheet, dialog and six typed entries;
  - OK, Esc, the refused markup name, Default Name and the hidden non-pet;
  - distinct, then duplicate, names.
- **Ordinary label, new:** each companion's native overtip read `Ébène l'Ancien`, bound to its own unit (dog `91586178…`, Mastodon `d79a4f6c…`).
  - The run reads the overtip's rendered character buffer. In this TMP the `text` getter keeps the prefab placeholder "New name"; the independent review caught that before launch.
- **Native Manual:** `Manual_300_KMC_C7_WORLD.zks`, SHA-256 `3e70c359fafe625b82d51679df936bf642bd385e517c5f793e5fef9edbb08bee`, 858106 bytes. It is failed-run evidence and not a consumer source.
- **Reader verdict, FAIL:** "A pad or seat point leaves the back (0.544 m): unmounted".
  - **Saddle numbers:** fit values on the seat line are on design. Mount-local, the seat is 2.3 cm over the back (0.8857 over 0.8627), the cantle 4.5 cm and the pommel 7.6 cm.
  - **Comparison with r03:** r03 had the seat 13 cm *under* the back and the pommel 31 cm over it, so the d307cf97 axis/smoothing correction moved the seat line where intended.
  - **Same fit throughout:** unmounted, mounted and after motion; 513 vertices, 411 casts, 0 misses, one build.
- **What the frames show** (private, `runtime-evidence/20261106-finalize-f5d-producer-04/movement-visuals`):
  - `dog-unmounted-side` / `-other` and `dog-bite-unmounted`: red and ochre shards 10–20 cm tall above mid-back. No continuous pad surface is visible.
  - `dog-mounted-other`: a thin strip from the rider's hip down the flank.
  - The female Small rider and her repaired leg are visible and seated. AC 19, speed 40 and size Medium are unchanged across tack, mount and motion. There is no tack on the Mastodon.
- **Restoration complete:** Mods, protected saves, the Working fixture and the save allowlist all passed. The digest is `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1`.

**Why the shape is still wrong: not diagnosed.**
- Facts:
  - The fit diagnostics along the centre line are right.
  - Some pad or seat vertex stands 0.544 m above the body's top midline vertex at its own z.
  - The rendered pad is mostly missing, with shards and strips showing.
- Hypotheses that fit these facts (not proven):
  - At some z, `DorsalVertex` picks a non-dorsal vertex. That vertex's skin weights (spine-only skinning) and rise would then both be wrong, so the vertices follow a limb under animation.
  - The pad surface sits inside the animated body (a fit-pose vs render-pose mismatch), so only edges poke out.
  - Pad faces are oriented inward and culled.
- The run records aggregates only, not per-vertex data. Settling the cause needs per-vertex fit data for the real posed dog, which the mission did not select building. The owner decides whether to add it.
- This is not attributed to anything external. It is a known product defect of PET-02.

**What this mission delivered offline (commits 6686f6e4, d32fdc28):**
- **Fit diagnostic:** `MaxRiseAboveBack`, the highest pad or seat point above the dorsal line, in the geometry and the scenario.
  - Regressions assert it, plus finite positions, normals and weights, on the ordinary, fur and chest/leg-column bodies.
- **Upper fit envelope in `Assert-KmcFinalize5Tack`:**
  - Seat 1–5 cm over the back.
  - Rolls and every pad or seat point under 8 cm.
  - Synthetic refusals: a buried seat, r03's 31 cm pommel, a fin over a correct seat, a floating seat, and evidence without the diagnostic.
  - r03's real evidence still refuses, and r04 refused natively. The old ordering-only check would have *passed* r04.
- **Overtip labels** recorded in the producer and consumer.
- **Cold consumer requirements:**
  - Its names must be observed with zero naming-service writes and no last outcome.
  - Dog statistics are compared across tack, mount, motion and reload.
- **`Assert-KmcFinalize5ColdAgainstSource` / `scripts/Test-Finalize5ColdAgainstSource.ps1`:** the consumer is checked against its producer's recorded ids, owners, names, overtips, archive (name/hash/length) and binary.
- Reader checks went from 18 to 36, all passing under strict mode.
- The start guard (lab-local helper) reads the record's append-only `startCeilingAdjustments` and refuses a seventh start. It has 6 synthetic checks.

**Coverage, stated exactly:**
- **Naming UI path:** native click events on the sheet button and dialog buttons, and keys sent through the field's own `ProcessEvent`. No OS mouse or keyboard test was run; the existing input recipe needs a desktop takeover, and nothing new was built.
- **Ordinary labels:** native overtip text (rendered buffer) at save. Not covered:
  - the portrait tooltip (the party portrait has no name text; its tooltip was not driven);
  - the combat log (no combat in this scene);
  - Buff Planner (a source reading only, not a UI test).
- **Persistence after restart:** NOT RUN.

**Budget, reconciled.**
- **Total:** finalize-f5 is at 42.52 of 60 min, 4 of 6 starts.
- **Imported spend:** the 18 F5 Fast tiers charged to the shared `development` record (430 s, 2026-11-06 22:51–00:13Z) are now cross-referenced into finalize-f5, one imported entry per source entry id. The development entries are unchanged.
- **This mission:**
  - Fast 0.5 min and Candidate 4.36 min (11/0).
  - Package 0.5 min, plus one 1 s entry for a mistyped package path that ran nothing (recorded, not removed).
  - Suite 0.9 min, WhatIf 0.3 min, producer 1.97 min.
- Starts 5 and 6 are unused. No reset, no borrowed capacity, no new record.

**To finish PET-02 and the persistence proof (owner decision):**
- the saddle-render cause (likely needs per-vertex data for the real dog, or a human look in the editor);
- a corrected candidate;
- a producer and consumer on the same record, which needs more than the 17.48 min left.

The naming implementation needs no change.

F4 stays accepted for integration with WORLD-02 deferred, not passed ([deferred items](FINALIZATION-DEFERRED-ITEMS.md)). No merge, release, tag, install, publication or F6. No game, transaction or waiter remains.

## Disposition at review (2026-11-07, superseded above)

**F5 ENGINEERING PARTIAL — START CEILING REACHED; OWNER ACCEPTANCE SEPARATE.**

- **PET-01 (naming): demonstrated in-game, producer side.** Three producer runs drove the real sheet button, the native text dialog, typing, OK, Esc, a refused markup name, Default Name, a non-companion and distinct-then-duplicate names. Every check passed in each run. The native Manual save was written (r03).
- **PET-02 (tack): attached, adopted, single and mechanically neutral in every run.** The fitted *shape* is not yet accepted. The r03 reader refused it: the seat sat below the back and the pommel stood 31 cm high. The cause was diagnosed and corrected offline, with a regression that reproduces it, but the correction has **not** been seen in the game.
- **Not run:** the separate-process cold consumer, which is the persistence-after-restart proof. All three finalize-f5 starts were spent on producers. Each failure exposed a different concrete defect; all three are corrected and none is fixed by weakening a check.
- **Needed to finish:** owner authorization for **two more starts on the same record** (one producer, one consumer) for the frozen corrected candidate below. 33 of the 60 minutes remain. Nothing else is blocked.
- WORLD-02 (F4) stays deferred and not passed: see [FINALIZATION-DEFERRED-ITEMS.md](FINALIZATION-DEFERRED-ITEMS.md).
- No merge, release, install or F6.

## What changed

**PET-01 — Rename Companion.**
- **Storage:** the native per-unit `UnitDescriptor.CustomName`. It is a JSON property saved with that unit, and it is the field native `ChangeUnitName` writes. `CharacterName` returns it before the blueprint name. A write sets that field and raises native `IUnitNameHandler.OnUnitNameChanged`, which refreshes overtips and dialog portraits.
- **Untouched:** blueprints, IDs, ownership, progression. There is no parallel name table. A replacement companion is a new unit with an empty name.
- **Entry point:** the character sheet's name line gets two copies of the native message box's own button.
  - **Rename** appears for an eligible companion.
  - **Default Name** appears only once a custom name exists, and asks a native Yes/No before resetting.
  - The UI members are reached by exact metadata token; the product still does not reference `UnityEngine.UI`.
- **Dialog:** the native PC text box, the one the save-overwrite flow uses, prefilled with the current name.
  - KMC starts the field's edit at open (`EditableTextInput.BeingEdit`). Typing then reaches the field, and native `KeyboardAccess` runs no game binding while the field holds focus.
  - That same focus means the field consumes Esc. The service therefore closes its own box through the box's Esc handler when the field reports its native cancel, and gives the selection back when the box closes, so hotkeys return.
  - The invalid-name message and the reopened dialog open from `Update`, never inside the box's own callbacks.
- **Eligibility:** read when the dialog opens and again on the exact unit at confirmation. The unit must be:
  - a pet whose master's `Pet` is that unit;
  - owned by a master who is one of the player's characters;
  - player faction, and not a summon.
  Selection changes cannot redirect a confirmation.
- **Input rules:**
  - The ends are trimmed and whitespace runs collapse to one space.
  - At most 24 visible characters (64 UTF-16 units).
  - Refused: control characters and invisible format characters. Joiners (ZWJ/ZWNJ) are kept.
  - Also refused: `< > { }`, which native labels read as rich text or templates. Nothing is escaped or rewritten.
  - Glyphs the game font lacks are dropped by the native field itself.
- **Buff Planner** (read-only check of its source): it keys units by `UniqueId` and labels them with `unit.CharacterName`. It picks up the new name with no retargeting, and needs no change and no dependency.

**PET-02 — Riding Dog tack (original procedural art; nothing extracted or copied).**
- **Design:** a red wool pad with an ochre border, a leather seat with a low cantle and pommel, and a girth.
  - It is built by casting rays onto the dog's own body, where its bones hold it, in mount-local space.
  - The hit distances are smoothed along each sample's own ray, so fur cards and limbs don't spike the drape.
  - Every vertex is skinned to the spine alone, using the dorsal midline's bone weights at its z.
- **Attachment:**
  - One `SkinnedMeshRenderer` per live view, for the KMC Riding Dog blueprint `b7a14f25…` only.
  - It is added on the native `IUnitViewAttachedUIHandler` event, which fires on spawn, load and view replacement, and handed to the view's native renderer list (`MarkRenderersAndCollidersAreUpdated`). Fog, fade, highlight and visibility then treat it as part of the dog.
  - A polymorphed dog is skipped. Only the dog view's own parts are fitted, never a mounted rider's.
  - A fit is cached per body mesh only when it is complete. Nothing is saved, and dispose unparents the object before destroying it.
- **Material:** a clone of the dog's own material, using the native `PF/StandardDynamic` shader. `_MainTex` and `_BumpMap` are replaced with a procedural atlas and a flat normal.
- **Mechanics untouched:** no fact, stat, size, speed, collision, reach or item. Measured in r03: the dog's AC 19, speed 40 and size Medium are unchanged across tack, mount and motion. The stock Mastodon never received tack. The Small-rider seat and rig repair are unchanged.

**Harness (F5 case only; historical cases keep their expectations).**
- The new Chunk 7 case is `companion-names-tack-manual`: producer `chunk7-persistence-save`, consumer `chunk7-persistence-load`.
- The reader is `scripts/runtime/Finalize5CompanionEvidence.ps1`, with its synthetic test `scripts/Test-Finalize5CompanionReaders.ps1`.
- The exact F5 branch is admitted beside F1–F4 in both the external and compiled request validators.
- Post-F2 control rows: the F5 case binds only Rider Primary (kind 3). Per rider it expects exactly one kind 3, at most one public control (kind 6) and at most one placement record (kind 7, index 0), and no hidden Dismount (kind 2).

## Native results (finalize-f5)

| Start | Run | Package · DLL · commit | Verdict | What happened |
|---|---|---|---|---|
| 1 | `20261106-finalize-f5a-producer-01` | f5a `3720d265…` · `37d31218…` · `d47f62e1` | **FAIL** | All naming, dialog and tack steps passed and all 16 captures were written. Save preparation then refused on a pre-F2 control-slot assertion. Captures showed a ragged pad edge and an empty typed field. |
| 2 | `20261106-finalize-f5b-producer-02` | f5b `f6097436…` · `d60a2532…` · `b7dc8a6b` | **FAIL** | All naming, dialog and tack steps passed, the typed text was drawn, and the native Manual was written. The post-save check failed: F2's canonical-control migration rewrote the harness-placed hidden Dismount bindings (designed behaviour; the harness setup was stale). Captures showed a fin above the back. |
| 3 | `20261106-finalize-f5c-producer-03` | f5c `3b0906a1…` · `655c1823…` · `4562f8fa` | **FAIL** | **Game side PASS on every row:** two mounts, all-pair archive, continuation, cleanup, and every F5 naming step. The native Manual `15686545…` was written. The outer reader then refused the tack fit: seat 0.734 under a 0.863 back, pommel 1.175. |

Original verdicts are kept and nothing was replayed or re-attributed. Each run's cause and correction is in its commit:
- r01 → `b7dc8a6b`
- r02 → `4562f8fa`
- r03 → `d307cf97`

All three runs restored completely: Mods, protected saves, Working fixture and save allowlist all passed, and the restored save-inventory digest is `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1`. No game, lock or waiter remains.

**What r03's evidence shows (the reader passes all naming and tack-presence checks when only the tack-shape check is bypassed; diagnostic only, verdict unchanged).**

*Opening the sheet and dialog*
- The sheet offered Rename for the default-named Mastodon (`d79a4f6c…`) and no Default Name.
- The dialog opened focused on "Mastodon", with game hotkeys suspended and every actor idle.

*Typing and the name paths*
- Six typed entries reached the field exactly (`Ébène l'Ancien`, `Nobody`, `<b>Rex</b>`, …).
- OK renamed the companion; ID, master and ownership were unchanged.
- Esc changed nothing and released the keyboard.
- The markup entry explained itself and reopened with the typed text.
- Default Name restored "Mastodon".
- Rename was hidden for the Halfling rider.

*Final labels*
- Riding Dog (`e32dcc06…`): "Biscuit", then the duplicate "Ébène l'Ancien".
- At save, both companions read "Ébène l'Ancien" on distinct IDs.

*Tack*
- One adopted tack on the dog view, unmounted, mounted and after cast, bite and turn, with one mesh build.
- None on the Mastodon.

## Next run, once authorized (corrected candidate = this branch's head)

Freeze from the attached branch head; do not detach and do not commit records between the two runs. The launcher pins the manifest's commit and branch, and the consumer's source guard requires the producer's exact DLL and commit.
1. Candidate with the F5 focused tests, then `Package.ps1 -ArtifactQualifier finalize-f5d`, then the suite snapshot.
2. Producer WhatIf, then the producer (`chunk7-persistence-save`, case `companion-names-tack-manual`).
3. Consumer WhatIf, then the consumer (`chunk7-persistence-load`, same case, with `-PersistenceSourceRunId` and `-ExpectedPersistenceSourceSha256` set to the producer's Manual).
4. Inspect the tack captures, then commit the records.
Reserve: about 5 min of preparation plus two launches. The launch guard needs 10 min free at each admission; 33 min remain.

## Offline checks (this branch)
- **Fast tier:** PASS. Components include name policy, eligibility, and tack fit on a clean body, a body with fur spikes, a leg bone and a jumping axis (the last fails on the r03 geometry).
- **Candidate tier:** PASS 11/0 on each frozen candidate.
- **Focused tests:**
  - F5 reader: 18 checks, including the corrected reader against r01's real producer evidence and r02's real post-F2 archive.
  - Request branch: 32 checks.
  - Sprint 5 readers: 90 checks.
  - F1 localization keys.

## Spend
- **finalize-f5:** 26.83 of 60 min, 3 of 3 starts. Each producer took about 1.9 min of game time. Three Candidate tiers took 7.1, 3.9 and 3.9 min; packages and suites took 5.2 min; WhatIfs took 1.1 min. All entries and failures are kept; there has been no reset or adjustment beyond the initial 90 → 60 allowance.
- My Fast runs while editing were charged to the shared `development` record.

## Owner judgments still pending
- **The tack's look:** colours, thickness, and how the female Halfling sits on it. Only the pre-correction shapes have been seen; the private captures are under each run's `movement-visuals`.
- **The sheet buttons:** placement and wording. "Rename" sits on the name line; both buttons sit under a renamed name.
- **Ordinary play:** portrait tooltip, combat log and Buff Planner labels (they read the same native field; not captured separately). A real mouse click on the sheet button (the run pressed it through its native click event). Typing through the OS keyboard (the run sent keys through the field's own key handler).

## Small owner playcheck
Open a companion's character sheet, press Rename, type a name, and confirm. Check the portrait tooltip and Buff Planner. Press Default Name. Save, quit, load. Look at the saddled Riding Dog from both sides while it walks, then mounted.
