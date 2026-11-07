# F5 review handoff: companion naming and a fitted Riding Dog saddle

## Disposition after the producer/consumer completion mission (2026-11-07)

**F5 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER ACCEPTANCE SEPARATE.**

The mission (review baseline PR #20 at d87b148e) raised the same finalize-f5 record's start ceiling from 7 to 8, append-only. The 90-minute time ceiling is unchanged. Exactly two starts ran: a producer under the corrected UTF-8 reader and its cold consumer in a fresh game process. Both PASS, and the cross-run reader passes. No gameplay code changed.

### Identities

- **Compiled source:** fd8bb82018d70c93732c7a1bedcf4a9a60c2c2af (r06's).
- **Frozen and launched head:** d87b148edfc9b48989f253ca31054c047bac5033, attached branch `codex/kmc-finalize-f5-names-dog-tack`, never detached.
  - fd8bb820 → d87b148e changes only `Read-KmcJson`, its focused regression and three records. There is no C# or project-file delta.
- **DLL:** `8459126ce033443bda4fad9a4444984fa3217ca528aad63d723a5818ee8f9361`, MVID e90f71ee-c299-40ca-9e80-562fd2b497b1.
  - Byte-identical to r06's. Verified before and after the Candidate rebuild and after packaging.
- **Package:** `KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f5h-diagnostic.zip`, SHA-256 `b078d8700e429caff162922cb3c6555227aeb2cd512e2ef3e2fbe274699c23d3`.
  - It differs from r06's package `2013e870…` only because its manifest records the new head.
- **Suite:** `finalize-f5h-d87b148e-20261107`, snapshot SHA-256 `664ad81f3a55b5f88a966fe45e4b0ed268841cdd04e78f5d5754fc80c67a6b90`.
  - r06's suite `finalize-f5g-fd8bb820-20261107` has full snapshot SHA-256 `6b9bce9573e211ad089bad4a955e84642148481626c5565bbfe0d64f75dde523`, from the frozen file.
- **Native target:** Kingmaker 2.1.7b, BaseGameOnly, with the retained dependency selection `retained-dependencies-final211.json`. This is the fixture configuration, not the owner's full mod stack.

### Gates before freezing

- **Focused reader regression:** 55/55 under Windows PowerShell 5.1.19041 Desktop, the launch runtime. It includes the UTF-8 no-BOM accented-name probe against the real helper.
- **Reader paths:**
  - The launcher dot-sources the worktree's `RuntimeHarness.Common.ps1`, the only non-test `Read-KmcJson`.
  - The cross-run checker reads with `-Encoding UTF8`.
- **Candidate:** 11/0 at d87b148e in 4.31 min, including Fast, with every sub-suite at FAIL=0.
- **Other checks:**
  - The request validator passed both shapes.
  - The producer and consumer WhatIf checks passed.
  - No new review: the only delta since the reviewed fd8bb820 is the owner-reviewed d6c88978.

### Producer r07 (start 7): PASS

- **Run:** `20261107-finalize-f5h-producer-07`, game process 32424. Result PASS, evidence PASS, 4/4 subscenarios, no errors.
- **Naming:** every row passes in the external reader:
  - the duplicate Unicode name `Ébène l'Ancien`;
  - Esc and cancel;
  - markup refusal;
  - reset;
  - the distinct name;
  - labels and overtips.
- **Identities at save:**
  - Riding Dog `f56df47a-0b0d-4113-80d2-6c42540166bc` (blueprint b7a14f25…, master `d17c8fd0…`, the female Small halfling).
  - Mastodon `d79a4f6c-b74e-4868-95bd-533899131acb` (master `b6628a77…`).
  - Both have CustomName and CharacterName `Ébène l'Ancien`.
- **Saddle:** in every pose, clearance −0.0036…0.0508 with 0 unsupported. Fit 109.3 ms. One binding on the dog and none on the Mammoth. Dog AC 19, speed 40, Medium, unchanged.
- **Inspected frames** (private, `movement-visuals`): unmounted side and mounted side show one continuous pad and seat on the back and the girth behind the elbow. The rider is seated with her leg unchanged, and there is no shard or buried surface.
- **Continuation:** the planned second-pair continuation passed its plan check.
- **Native Manual:** `Manual_300_KMC_C7_WORLD.zks`, SHA-256 `d39f8b46e37d3f39af73053b45846be0da525a679004086b7d76f51c4dc4d90b`, 858008 bytes.
  - Schema 3, area 9d1278a2…, campaign d41185be…. Two pairs: Mastodon generation 1, and Riding Dog `small-halfling-female-ridingdog-v1` generation 2. Five control slots.
- **Archive size:** larger than r06's 376897 bytes only because its `header.png` thumbnail is a real 480659-byte render; r06's was a 4100-byte near-blank image. The world, party and player entries match r06's within a few KB.
- **Restoration:**
  - Mods, Working, baseline, save protection and the write allowlist are all true.
  - Save-inventory digest `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1`, the same as r06's.

### Cold consumer r08 (start 8): PASS

- **Run:** `20261107-finalize-f5h-consumer-08`, a fresh game process 33832. Same commit, branch, DLL and MVID as r07.
  - It loaded r07's exact Manual (`d39f8b46…`, 858008 bytes) through the unchanged source rule.
  - Result PASS, evidence PASS, 3/3 subscenarios, no errors.
- **Names:**
  - Before any rename, reset or sheet action, both companions read CustomName and CharacterName `Ébène l'Ancien`: the duplicate Unicode name, unchanged.
  - The overtips render it on both.
  - Naming-service writes: 0 at the cold-name observation and 0 at the overtips. The last outcome is null, so nothing test-side renamed them.
- **Identities:** the cold dog and Mastodon have the saved unit IDs, masters and blueprints. Both are the restored archive mounts of their pairs, and both still own their pets and are eligible.
- **Saddle:**
  - It was re-fitted in the new process: fit 105.6 ms, against the producer's 109.3, with clearance equal to float noise.
  - One binding on the dog (`unitBound` f56df47a…) and none on the Mammoth. Adopted, visible, the same bones as `dog3`, `PF/StandardDynamic`. 513 vertices, 411 casts, 0 misses, back at seat 0.8627.
  - The same holds after the dismount/remount cycle (`tackRemounted`).
  - Dog AC 19, speed 40, Medium, unchanged.
- **Frames** (inspected, private): cold restored side and cold remounted side show the same coherent saddle with the rider seated. The cold dog sheet shows `Ébène l'Ancien` with Default Name and Rename, AC 19 and speed 40.
- **Lifecycle:**
  - The cold sheets for both companions offered Rename and Default Name and closed.
  - Native continuation followed its plan, and the terminal reader passed.
- **Writes and restoration:** the consumer wrote no save. The source Manual is still `d39f8b46…`, 858008 bytes. Restoration is complete with the same digest `60e3d6cc…`.
- **Cross-run reader:** `scripts/Test-Finalize5ColdAgainstSource.ps1 -ProducerRunId 20261107-finalize-f5h-producer-07 -ConsumerRunId 20261107-finalize-f5h-consumer-08` passes.
  - It checks both PASS, the same commit, branch, DLL and MVID, and the exact Manual.
  - Per companion: the saved ID, CustomName and CharacterName, the master, the blueprint and the overtip text.
  - The dog's statistics.

### Earlier runs, kept

- **r06** `20261107-finalize-f5g-producer-06`: ORIGINAL OUTER FAIL / NATIVE GAME PASS. The verdict, evidence bytes and archive (`c673673c…`, 376897 bytes) are kept untouched. Its UTF-8 re-evaluation (below) is separate evidence, and r06 was not used as a source.
- **r01–r05:** FAIL verdicts kept at their own builds. r05's private specimen is kept.

### Budget

- **Spend:** 74.9 of 90 minutes and 8 of 8 starts on the same finalize-f5 record.
  - The ceiling chain is 3 → 6 → 7 → 8, append-only, and the time ceiling is unchanged.
  - A ninth start is refused offline, as checked against the real record.
  - The 430 s development cross-references are retained and not recharged.
- **This mission:** Candidate 4.31 min, package and suite about 2.2 min, two WhatIf checks, producer 2.03 min and consumer 1.75 min.
- **State:** no game, waiter or transaction lock remains.

### Not tested, and unchanged

- **Not tested:** physical mouse and keyboard input, the portrait tooltip, the combat log and Buff Planner screens. Posed clearance is verified offline on one specimen; in-game clearance is checked in the fit pose.
- **Owner acceptance:** saddle art and taste, and full-stack play, remain separate.
- **Unchanged:** WORLD-02 stays deferred and not passed. F6 is not started. No merge, release, tag, install or publication.

## Superseded disposition after the saddle-diagnosis mission (2026-11-07)

**F5 ENGINEERING PARTIAL — SADDLE CAUSE PROVEN, CORRECTED AND DEMONSTRATED NATIVELY; PRODUCER r06 GAME PASS BUT REFUSED BY A HARNESS ENCODING DEFECT (NOW FIXED); COLD CONSUMER NOT RUN; OWNER ACCEPTANCE SEPARATE.**

The mission (review baseline PR #20 at f384591c) raised the same finalize-f5 record from 60 to 90 minutes and from 6 to 7 starts, both append-only. A focused check refuses an eighth start. Two starts ran: one bounded specimen collection and one corrected producer. The seventh start is unused, because a consumer cannot take r06 as its source (see "Why the consumer did not run").

### Proven saddle cause (first failing stage: sampling/fit)

- **Specimen:** start 5, `20261107-finalize-f5e-specimen-05` (commit 8caa860c, DLL `7aca0d94…`, MVID 70dc8a63…).
  - Captured privately: the exact fit input (body `dog3`, 2488 vertices, 3672 triangles, 37 bones, quality Auto, blendWeights FourBones, no LOD group, the only body renderer), the fit output and its mesh pre-image.
  - Same-frame `BakeMesh` snapshots of body and tack, with every matrix, at unmounted idle, mounted idle and mid-walk.
  - The specimen is in the run's evidence folder. The offline replay, its reports and a hash receipt are under `analysis-cache/finalize-f5-saddle-replay/` (`receipt.json`). Nothing proprietary is committed.
- **Replay identity:** the r04 fitter on the captured input reproduces the captured tack to 1.25e-6 m.
- **Conversion and skinning are exact, so not the cause:**
  - Fit → mesh pre-image → fit-pose skin round trip: 1.4e-7 m over all 513 vertices.
  - The native baked tack equals an independent linear-blend skin in all three poses (0.0000 m).
  - BakeMesh output matched renderer-local space in every pose (body within 5e-6 m of the independent skin).
- **Cause:**
  - The dog's back has a top vertex only every 5–10 cm. At z 0.11–0.13 (the seat) and 0.19–0.20, `DorsalVertex`'s narrow window held only chest-underside vertices: vertex 206 at y 0.369 (UpperTorso/Neck0), and y 0.400–0.447. The real back from above is at 0.862–0.870 there.
  - That reference set the axis for every cast, every smoothing anchor and the girth ring at those z.
  - The pad folded into shards: 39 vertices more than 5 cm off the body, up to 17 cm. The seat's front normals turned forward: 36 vertices more than 5 cm off. The girth ran up to 13 cm into the chest: 55 vertices buried. This is what r04's frames showed.
- **r04's 0.544 m `maxRise`:** measured against that wrong chest vertex (vertex 139 sat 4.3 cm above the true back). The refusal was right in substance, because the independent clearance shows real shards, but its number used a wrong reference.

### Correction (bad4d0f5, review corrections fd8bb820, harness fix d6c88978)

- **Back sampling:** the back is where the vertical midline line meets the coat from above, with that triangle's bone weights. The axis uses the median of five such samples over 8 cm, so a fur tuft on the midline cannot shift a row.
- **Girth:** sits behind the forelegs' elbows (4 cm behind the seat). At its old place it ran 4 cm into the elbows even after the sampling fix.
- **Clearance:** the fit measures every vertex's signed clearance along its surface normal, as `MinClearance`, `MaxClearance` and `Unsupported`, using one shared static measure.
  - The reader requires all three: nothing more than 5 mm inside the body (half the pad's 1 cm lift), nothing 8 cm or more off it, and nothing without body under it.
  - `maxRise` is now against the back surface. Existing bounds are unchanged.
- **Weights:** spine-only bones remain: UpperTorso, LowerTorso, Neck0, plus traces of Neck1 and Pelvis (summed weights 0.5 and 0.1 across 513 vertices).
- **Before → after on the real specimen** (independent signed closest-point clearance, all three poses):
  - Pad: −3.4 mm…1.7 cm, none more than 5 cm off (before: 39 more than 5 cm off).
  - Seat: 3 mm…5.1 cm, the designed cantle and pommel (before: 36 more than 5 cm off).
  - Girth: 2.3 mm…3.1 cm, none buried (before: 55 buried, up to 13 cm).
- **Regressions:**
  - A synthetic sparse back over a midline keel inside the belly. The r04 fitter fails it (`maxRise` 0.705 m) and the corrected one passes (0.051 m, no keel-bone weight).
  - The fur-card test holds at 1.29 cm displacement against its 2 cm bound.
  - The clearance measure classifies a buried point, a floating point and an unsupported point.
  - The reader refuses the r04 shapes and is pinned at ±1 mm of each bound. 55 reader checks.
- **Review and gates:** one independent read-only review, nothing blocking; its should-fix items were batched into fd8bb820. Candidate 11/0 at fd8bb820, including Fast: 1100 unit and 149 component checks.

### Producer r06 (start 6): game PASS, recorded verdict FAIL, kept

- **Run:** `20261107-finalize-f5g-producer-06`.
  - Commit fd8bb82018d70c93732c7a1bedcf4a9a60c2c2af. DLL `8459126ce033443bda4fad9a4444984fa3217ca528aad63d723a5818ee8f9361`, MVID e90f71ee-c299-40ca-9e80-562fd2b497b1.
  - Package `2013e8706281e8ec773574cb0b746fa8903588d10de2e9d2c6891725122a934a`; suite `finalize-f5g-fd8bb820-20261107` (`6b9bce95…`).
  - BaseGameOnly with the retained dependency selection.
- **Saddle in-game:** the numbers are identical to the replay: `maxRise` 0.0509, clearance −0.0036…0.0508, 0 unsupported, fit 118 ms.
  - One binding on the dog and none on the Mammoth, adopted. Dog AC 19, speed 40, Medium, unchanged through tack, mount and motion.
  - Inspected frames: one continuous pad on the back with the seat and rolls, a girth strap behind the elbow on both sides, and the female Small rider seated with her leg unchanged. Walking shows the pad and girth following the dog. No shards, fin or buried surface.
  - Frames stay private in the run's `movement-visuals`.
- **Naming in-game:**
  - Every naming row from r04 recurs: the duplicate name `Ébène l'Ancien`, Esc and cancel, markup refusal, reset, the distinct name, and labels.
  - Overtips render the name on the dog (0c7e9a2f…) and on the Mastodon (d79a4f6c…).
- **Continuation and save:** the planned second-pair continuation (angle 270) landed with residual 0.433 m. The native Manual was written: `Manual_300_KMC_C7_WORLD.zks`, SHA-256 `c673673c44cd2dcb3665fdde5cb8141ddd9434cd4f3ced3b03fca7ba4d8f3f1e`, 376897 bytes.
- **Restoration:** complete, save-inventory digest `60e3d6cc…`.
- **Why it FAILed:**
  - The harness's shared `Read-KmcJson` decoded the game's UTF-8 evidence (no byte-order mark) as ANSI under Windows PowerShell 5.1, so `Ébène` became `Ã‰bÃ¨ne`. The first non-ASCII comparison, the renamed Mammoth, refused the run.
  - r01–r04 never reached that comparison; r04 stopped at the saddle.
  - Reading the recorded evidence offline with UTF-8, r06 passes the whole persistence reader chain, including the F5 naming, overtip, saddle clearance, statistics, plan and capture checks. ANSI reproduces the recorded refusal exactly.
  - The recorded verdict stays FAIL. d6c88978 fixes the reader and adds a regression that fails on the old body.

### Why the consumer did not run

- The launcher accepts a cold source only with a recorded PASS from the same commit and DLL. r06 is FAIL, and the reader fix is a new commit.
- Accepting r06 anyway would be a source-check bypass, which is not done.
- A fresh producer on the seventh start would leave no consumer. The seventh start is therefore unused.

### Also in this mission

- **r05's own FAIL:** the second pair's fixed +z 2 m continuation from where F5's visual walk ended overshot by 1.57 m (residual 1.573). That walk's endpoint follows the dog's spawn facing.
- **Fix:** F5's second pair now plans that ray with the existing `SelectAreaContinuationGoal`, as the area-autosave and Paladin cases already do, and the reader checks the click is the plan's own eligible goal. r06's residual was 0.433.
- **Movement claim:** this is an inferred, not proven, native cause (an off-navmesh clamp), guarded by the selector's clamp, trace and occupancy rules.

### Exact remaining cause and the next owner decision

- **Remaining:** fresh-process persistence of the corrected saddle and names is undemonstrated. One start remains and two are needed: a producer whose verdict can be PASS under the fixed reader, and its consumer.
- **Recommended:** raise the same record's start ceiling 7 → 8 (no time increase). The remaining 26.03 minutes cover a Candidate at d6c88978 plus a package, suite and two launches (estimated about 18 minutes). Then run producer then consumer from the attached branch with no records commit between them.
- **Alternative:** a narrowly pinned owner-authorized re-evaluation route admitting r06 as a source. This would need new guard code and review; it is not recommended.

### Budget

- **Spend:** 63.97 of 90 minutes, 6 of 7 starts, on the same finalize-f5 record. The 18 development cross-references (430 s) are retained and not recharged.
- **Starts:** r01–r04 FAIL (kept); r05 specimen FAIL (the continuation; its specimen is complete); r06 game PASS / recorded FAIL.
- **State:** no game, waiter or transaction lock remains.

### Not tested, and unchanged

- **Not tested:** physical mouse and keyboard input, portrait tooltip, combat log and Buff Planner screens. Posed clearance is checked offline on one specimen; in-game clearance is checked in the fit pose.
- **Unchanged:** WORLD-02 stays deferred and not passed. F6 is not started. No merge, release, tag, install or publication.

## Disposition after the completion mission (2026-11-07, superseded above)

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
