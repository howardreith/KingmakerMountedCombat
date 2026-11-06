# F4 review handoff: ordinary world orders, native AI, transitions and Horse gait

## Current disposition: entrance/reload finishing mission (2026-11-06)

**F4: ENGINEERING PARTIAL. Ready for final review; owner acceptance (art and the full laptop stack) is separate.**
- **Accepted:** the real dungeon entrance with its one authorized after-entry Auto (r08 PASS).
- **Not run:** its native reload. It is built, reviewed and validated against r08's real receipt, through a passing WhatIf. Both authorized starts were consumed because r07 exposed a read-back defect.
- **INPUT BLOCKED:** WORLD-02. No trap save was supplied.

This is not "PARTIAL WITH WORLD-02 INPUT BLOCKED ONLY": the reload is also outstanding. No merge, release, install or F5.

### Current candidate (f4f)

| Item | Value |
|---|---|
| Source | `3f824e30601f3126bba45e8ec4d9660b4bfbf15c` (branch head after records: see PR #19) |
| DLL | SHA-256 `3240963f3c4966f17ae121412808bbac4f17af1dba76d0fd05ece0b4dc4f7f3d` · MVID `cc86d1d6-502e-4991-8805-618755a9ae47` |
| Package | `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f4f-diagnostic.zip` · `4efc6665274ac8cff979eeaae3bd9240810ef6e6f855b16659a5589da411245e` |
| Suite | `finalize-f4f-3f824e30-20261106` · `f6513447e2f05ddef185547ac921a24f5296e61ec1dfab72f5a94f19cec8bdc5` |
| Engine / dependencies | Kingmaker 2.1.7b. Base-game staging with the retained CotW/CMI selection `retained-dependencies-final211.json`. This is not the owner's full laptop stack. |

The preview.103 label is shared by different binaries, so pin the identities above.

**Executed f4d, for history:**
- source `b57712efcdb54dbaf02660e4dca721f902594677`;
- DLL `2c8e352255fff67ca7243cc305777fbf5fd6904b9b05f76fbb5086fddd8be2f0`, MVID `07a436fa-51d3-41b3-accc-20dc875af14d`;
- package `dfa0b231ba6880046b35a0db94babecf26c5b82853d1b160bfef91abd5d1a5df`;
- suite `finalize-f4d-b57712ef-20261106` · `ec45fd7579f05f9395ca2518ead87fa366ddb214d29fbd287378858cfe7a3c6b`.

**Intermediate f4e (r07):**
- source `676c3a8a`;
- MVID `fab53a55-ab2a-4f43-bc82-2540ac89ae4e`;
- package `f31d1d34d7a2e55282a0f7066e2fefd6461bc49196fe6fc2cb2f624d788c48f9`;
- suite `finalize-f4e-676c3a8a-20261106` · `a056917cdaf9bdbf3e06bbb5a55d5b223b314d1ee7c0971a8e3e4245afa9f6f0`.

### Entrance → Auto → reload chain

| Step | Result |
|---|---|
| Producer `persistence-p07-save` / `area-dungeon-entry` | **r08 `20261106-finalize-f4f-dungeon-08`: PASS** (external reader). Native assertions 47/0.<br>• The setup transfer reached the hub with no write and kept the same mounted pair.<br>• The native `ActionEnterToDungeon` and party `Go` loaded the floor `9d1278a2…`.<br>• Same world and party, same rider `b6628a77…` and mount `d79a4f6c…`, generation 1 before and after.<br>• Legal Mounted relationship, unique entities, the same bound views (−339134 / −340558).<br>• Exactly two transfers, no load replay, no mount cast.<br>• Then the ordinary continuation: settle, ground move, stock attack, target removal. |
| Its one Auto | `Auto_1.zks` · SHA-256 `b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5` · 395809 bytes · native type Auto, AfterEntry · header Name `Autosave1`, campaign `d41185be-edc1-47c0-b9d5-e1d7a9c8e65f` ("cvb"), Area = floor.<br>Admitted in the hub, committed on the floor after pair restoration; the barrier shows Mounted, 2 suspensions and 2 resumes.<br>Its schema-3 member holds exactly that pair. 1 save, 1 load, 0 unauthorized. |
| Native reload `persistence-p07-load` / `area-dungeon-entry-auto` | **NOT RUN: start ceiling (8 of 8).**<br>• Built as the read-only cold consumer of exactly that Auto. It declares one hash-pinned read-only leaf and no write.<br>• It reuses the transition-Auto qualification (own area and campaign, no inherited transfer state, own-archive debt, unique bound cold views) and the established ordinary continuation.<br>• Its source is admitted only from a completed, restored PASS `area-dungeon-entry` producer in the `transition-auto` role.<br>• It refuses another case, role, alternate, type, hash or area, and a failed or unrestored producer. The r07 FAIL source is refused.<br>• The exact consumer request was derived from r08's receipt and validated, and the consumer WhatIf against r08 passed (`20261106-finalize-f4f-dungeon-reload-whatif`). |
| WORLD-02 trap | **INPUT BLOCKED.** No suitable Manual save was supplied and no start was spent; see the requested input below. |

**Runs in this mission, original verdicts kept:**
- **r07 `20261106-finalize-f4e-dungeon-07` (f4e): FAIL.** Every arrival assertion passed. The scenario's KMC read-back then used the legacy single-pair reader, but the product writes the schema-3 archive member, so the read did not report Current. The consumer's qualification had the same gap. Both are corrected in `3f824e30`, which reads the member the product writes, as the UPG-01 check does.
- **r08 (f4f): PASS.**
- **r06: FAIL, kept.** Its full Auto hash, measured from the retained isolated file, is `7f64cfeddce5aaebe2ab3bdeb664210ef96abb3d32197718b48330881caee9c5` (396056 bytes). It remains supporting evidence only.

**Restoration (r07, r08):**
- Mods, Working fixture, baseline, save protection and allowlist all passed.
- The inventory digest `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1` is unchanged.
- No game process or waiter remains.

**Next owner decision:** one more start for the reload on f4f (`f4run -Kind dungeon-entry-auto -SourceRunId 20261106-finalize-f4f-dungeon-08 -SourceSha256 b8e4109e…`). This needs a new start allowance and about 10 minutes of launch reservation; 10.53 minutes of the 75 remain.

**Requested trap input:** one ordinary Manual save from a compatible KMC build, with an eligible rider and its existing mount in a pet-permitted area, a revealed, armed trap 4–30 m away and safely reachable, nearby enemies cleared, and room for a group move or disarm order. Name its required dependencies; a disposable copy is used.

### Mission work and checks

- **Authority and starts:** the ceiling went from 6 to 8 on the same record, logged append-only as `startCeilingAdjustments` (2026-11-06T19:48Z). The helper refuses a ninth start before any lock; this was checked offline and against the real record.
- **Commits:**
  - `dac9b13a`: the consumer case.
  - `676c3a8a`: review corrections, the reader refusing transfer rows, intake pinning the floor, and a compiled leaf-declaration check.
  - `3f824e30`: the r07 correction.
- **Review:** one independent review of the intake, reader and consumer delta found no MAJOR problems. MINORs 1–3 were applied, including `scripts/Test-Finalize4DungeonReloadDeclaration.ps1`: the reload declares exactly one read-only Auto, and the cross-area consumer keeps its resave leaf.
- **Checks:** Candidate ×2 (9/9 each), Fast ×1 (plus 2 failed build attempts), persistence fixtures 687/0, three WhatIf runs (two producers, one consumer), and offline request validation.
- **Spend:** `finalize-f4` **64.47 of 75 min**, **8 of 8 starts**.

**Retained unchanged at their own binaries:**
- r03's original FAIL and its Hold diagnosis;
- r04's three passing rows (aggregate FAIL kept; its last two rows did not run);
- r05's PASS, its gait legs and its private motion packet;
- r01 and r02 FAILs.

## History: disposition after the F4 completion mission (2026-11-06)

**F4: ENGINEERING PARTIAL. Ready for final review; owner acceptance is separate.** The return label is not COMPLETE, and not INPUT BLOCKED alone. The trap source is still missing, and the hub→dungeon transition is also not demonstrated. The completion mission's sections below supersede the earlier disposition where they differ; the first three runs keep their original verdicts.

| Completion requirement | Outcome |
|---|---|
| 1. r03 Hold stall: real cause | **Identified and resolved natively.** The cast's carried approach left every live hostile out of the sling's reach and sight (18.5 m and 13.0 m, no LOS). Native Hold filters the brain's proposals to targets in reach with LOS, so it proposed nothing: proposals stayed at 37 from resume to stall. `player-operation-live` was a stale refusal from the cast period. This is a fixture premise, not KMC. The row now records the premise. When the premise is false, one disclosed ordinary ground click returns the pair, and a native Stop with an AI off/on cycle clears the player-move hold the click leaves (review MAJOR). r04: the premise was met at once (bow 12.19 m, hostile at 8.2 m in LOS). One stationary dispatch under native Hold, Hold kept, displacement 0.0, `playerMoveHold` false, rider Hold true. Reader PASS. |
| 2. Chest and a coherent exploration result | **PASS (r05, external reader).** The chest order was clicked while the group travelled. It replaced exactly the Horse's travel (native `Interrupt`). One bind and one terminal matching this order; the carry was 12.8 m from 16.8 m out. Opened once by the rider, still mounted; the other members finished their own travel. The four-leg speed comparison is replaced by three mounted legs: party pace, alone, and the native Slowed control. |
| 3. Real hub→dungeon transition, after-entry Auto, native reload | **Not demonstrated. r06 FAIL (scenario defect).** The setup transfer to the hub wrote nothing and kept the pair. The native `ActionEnterToDungeon` resolved the floor and opened the selector. The native `Go` ran with the pair Mounted. Then the scenario's own post-`Go` check read `GroupManager.gameObject`, which the AfterEntry load had destroyed in the same call, and threw NullReferenceException. So no in-game post-arrival assertion ran. Supporting observation only: the one authorized native write (`Auto_1.zks`, sha `7f64cfed…`, run-scoped isolated root, 0 unauthorized requests) has header Area = floor `9d1278a2…`. Its KMC record holds the same pair (generation 1, rider `b6628a77…`, mount `d79a4f6c…`) at the `Go` tick. Corrected in `c1c56439` (Unity lifetime check); not re-run, because all six starts are used. The reload was not run. |
| 4. Trap order on a reachable native trap | **INPUT BLOCKED (no start spent).** No admissible save has a revealed, reachable trap: the hub has none, and `Auto_1121`'s three active traps are 87–92 m from the party in an Auto save, which the owner-seed authority refuses. Needed: an owner **Manual** save in a pet-permitted area, enemies cleared, with a revealed interactable trap 4–30 m from an eligible mounted rider. |
| 5. Horse motion and legitimate slow movement | **PASS (r05).** Ordered motion was inspected: 40-sample traces and four frames per leg, all inside their legs. Party leg: Normal ×40 at 3.048 m/s, equal to the party pace (modified speed 5.08). Solo: Normal at 5.08. Native Slowed control: Slow ×40 at 2.54 m/s (modified 2.54); the condition was removed after (speed 5.08 again). The frames show the run stride under pace and a walk when slowed. |

### First-effort disposition (kept; superseded above where it differs)

All seven requirements (WORLD-01, WORLD-02, WORLD-03, AI-01, GAIT-01, AI-02, REG-01) have a source-established cause and a correction with offline regressions.

**Shown natively on the corrected candidates:**
- the group chest order (WORLD-03);
- the party-selection boundary that dismounted riders (WORLD-01 cause);
- the routine continuation and after-kill acquisition (AI-01);
- melee engagement with a moving enemy (AI-02);
- the party-paced trot (GAIT-01).

**Not shown natively:**
- the trap order (WORLD-02), because no admissible input has a reachable discovered trap;
- the full hub-to-dungeon load with its autosave and reload;
- the excluded-area and floor-to-floor controls.

All three F4 starts are used. Each run's original outer verdict is **FAIL**; each is explained below and kept as recorded.

**F3 closeout (recorded here):** F3 engineering review was accepted at PR #18 head `5dd648253b0c78e6aadb3b16ee6ff78f14b598ae`. The owner merged it as `a92b043ac3d0936ea804c4f2bb96c9165aa30247` on 2026-11-06. Owner play acceptance is separate. The reviewer did not run the game.
- Nothing about F3's evidence is reattributed.
- Its two polish observations remain unselected owner items:
  - the post-success "no Standard" banner;
  - the disabled post-Charge slider that still shows a distance.

## Identities

| Item | Value |
|---|---|
| Repository / branch | howardreith/KingmakerMountedCombat · `codex/kmc-finalize-f4-orders-ai-transitions` |
| Base | `a92b043a` (F3 merge) |
| Product commits | `b88e6e40` (five fixes), `f66c7c9e` (review corrections + native rows), `5d7505a7` (re-review correction) |
| Candidate f4a | head `5d7505a7` · DLL `47a9d82a413523119dcb1e91a136f1e9f1ca4bb44e6b7a5f67373623f08bfcf5` · MVID `4fa4cf92-5bbd-44c2-921e-087938fd5be7` · package `2efc5ce4de2eaf80f68d0cda8e57dd7b60b34fe463d7e8a0944346ed03dcc5fa` · suite `finalize-f4a-5d7505a7-20261106` (`f4f89d5c…`) |
| Candidate f4b | head `0f5d1e2a` (adds one compiled row name and reader registration) · DLL `e78ca1f6f5bc899cdbf14ceba657ca7e68d018560eb5cc9e0dfc93ef091cf2e0` · MVID `19adcb84-3d21-41bb-839a-c8984d477acc` · package `a1cf38bd1ec1f65d78b47558b252bbacf0d166a40c4f214574866ae129f0d21f` · suite `finalize-f4b-0f5d1e2a-20261106` (`8476e4e3…`) |
| Candidate f4c | head `5626a3b4` (adds one disclosed fixture setup line) · DLL `6e2e52ad70ea06f6901fcba6b88f2e7f823806e7ff4830aba113922462919e90` · MVID `01c60206-36e3-4f81-a959-ef97616ed57a` · package `de14768ee4445f4f6e3f076f608c0aef62bfe335c5ea215298c9dc4e57fd3ecd` · suite `finalize-f4c-5626a3b4-20261106` (`0328dbc5…`) |
| Product behaviour | Identical in f4a, f4b and f4c. Later commits change only diagnostics, readers and docs. |
| Packages | `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f4{a,b,c}-diagnostic.zip` (lab-relative). Displayed version `0.1.0-chunk7b-preview.103` is shared and is not identity. |
| Engine / staging | Kingmaker 2.1.7b. Base-game staging with the retained CotW/CMI selection (`retained-dependencies-final211.json`). Loader digest `db2c3901…`. This is not the owner's laptop stack. |

## Causal findings and corrections

| Requirement | Cause (decompiled 2.1.7b contracts) | Correction | Direct outcome |
|---|---|---|---|
| WORLD-03 chest | `ClickMapObjectHandler.TryInteract` → `SelectUnit` picks the closest selected non-pet (the Horse is projected to its rider), so the rider gets `UnitInteractWithObject`. KMC carried only spells and trap disarms. A chest command ran native `TickApproaching` on the rider's disabled stock agent: no movement and no real-time timeout, until the owner walked the Horse within 4 m. | The exact `StandardChest`/`LootComponent` interaction is a carried kind (`ContainerInteraction`); doors, devices and derived types stay native. A new real-time chest order replaces only the exact mount travel held when the order ran (review MAJOR). | r02: native group click → rider chosen → Horse carried 12.8 m from 16.9 m out → opened once by the rider, still mounted. |
| WORLD-02 trap | `TryInteract` runs `InterruptMove` on every selected unit for an accepted trap order, before the chosen unit's `Run`. The rider owns no move; the pair's travel is the mount's command, so the Horse kept walking. | Inside that accepted real click only, the native stop also reaches the selected rider's mount. It also ends the rider's own approaching orders (native `IsMoveUnit` semantics). Hover and prediction simulations never act. | Offline only. **Native NOT RUN: input blocked** (see below). |
| WORLD-01 exterior → dungeon | `ActionEnterToDungeon` → party selection `GroupManager.Go` → `Player.ReInitPartyCharacters` removes and re-adds every member. KMC dismounted on the removal, before `LoadArea`. Going deeper uses `LoadAreaForce` with no party selection, which is why floor to floor kept working. | A removal that stays in game inside the native re-initialisation waits for its end. A member back in the party keeps its exact pair; anything else is cleaned up as before. The scope is frame-bound and isolated. | r02: native `ShowPartySelection` + Go with the pair mounted → same owner, generation and Mounted state. |
| AI-01 archer | KMC replaced native `UnitAttack.UpdateTarget` for the pair command, so after a kill the rest of a Rapid Shot routine was lost and the archer waited for its cooldown. "Goblin inspect failed" is the native knowledge check `InspectUnitsController` queues on every death (`BattleLogManager` `UnitInspectFailed`); it is UI only and is not touched. | A started routine whose target is down continues on the nearest remembered enemy by the native rule, within the pair's own admission without approach. Nothing new is spent, and the remaining planned attacks are the bound. | r03: arrow 1 kills, arrows 2–3 continue on the next hostile in the same command (3/3 planned). The next automatic routine follows about 0.7 s later. |
| AI-02 melee | The pair approach faulted after 4 repaths, and every 0.75 m target drift counted as one. A walking enemy exhausted that in about 1–2 s and was put in the rider's `UnreachableUnits`, so the native brain filtered it until enemies came close. | A drift repath while the pair is closing in no longer consumes the allowance (bounded at 40). The elapsed bound grows by the travel time to the initial gap, capped at 8 s. Charge is unchanged. | r01 and r03: AI on, no click; the hostile walked 13.6 m; 10 progressing repaths, 0 counted, success, kill, not unreachable. |
| GAIT-01 | Native `TickOnUnit` picks the Slow gait when speed is below 95% of the blueprint speed. A group click paces everyone to the slowest selected member (floor 3.048 m/s), so the 50 ft Horse walks. r21 recorded `Slow:191`. | Presentation only: the exact KMC Horse, slowed by pacing alone and unimpaired, shows its run cycle at the unchanged pace. Explicit Slow, Stealth, Charge, combat, turn-based, an impaired Horse and a pace below 0.55 of its speed keep the native gait. | r02: party leg `Normal:183` at speed limit and mean 3.048 m/s; solo Normal at 5.08. |
| REG-01 | Floor to floor uses `LoadAreaForce` and the existing transfer/restore path. | Not changed. | Not re-run. |

**Reviews:**
- The first independent review found one MAJOR (a chest order waited behind the pair's travel) and four MINOR items. All were corrected in `f66c7c9e`.
- A focused re-review found one more MAJOR: a first-tick rule could cancel a newer player move after a pause. It was corrected in `5d7505a7` by binding the exact travel instance at the order's `Run`.

## Native outcomes (original verdicts kept)

| Run | Candidate | Original verdict | Re-evaluation / scope |
|---|---|---|---|
| `20261106-finalize-f4a-ai-rt-01` | f4a | **FAIL**: "unknown subscenario: F4-melee-engage" (the host row registry and the family schema dispatch lacked the new scenario). Five in-game rows PASS. | Reader-only re-evaluation on the immutable evidence after the registration-only corrections `fb27eb81` and `0f5d1e2a`: `Test-RuntimeGameResult -RequirePass` PASS and `Assert-KmcSprint4NaturalEvidence` PASS. The live working identity could not be re-verified after restoration. Continuation was not applicable: the kill fell on the last arrow. |
| `20261106-finalize-f4b-surface-02` | f4b | **FAIL**: owner-surface reader, `legsApplicable` 3 of 4. The rider-on-foot comparison leg found no admissible route after the F4 stage moved the pair. | Kept FAIL; the 4-leg rule was not loosened. Every F4 behaviour fact is recorded (table above). The F4 exploration assertion also stops on `carriedOutcome` null: the observer read the carried-approach outcome before it was published. That is an observer gap, not a product fault. |
| `20261106-finalize-f4c-ai-rt-03` | f4c | **FAIL**: the leaf deadline in `S4-controls` step 12. After a native Hold in bow reach, KMC's stop/hold cancel interrupted an automatic routine dispatched the same frame; later proposals were refused as `player-operation-live`. | `F4-melee-engage` and `S4-ranged-retarget` rows PASS (row-level F4 and Sprint 4 assertions). The stall's live operation is **not identified**. The same row passed in r01 on f4a, and the code path is unchanged between f4a and f4c. Bounded known issue. |

### Completion mission runs (candidate f4d; original verdicts kept)

| Run | Original verdict | Facts |
|---|---|---|
| `20261106-finalize-f4d-ai-rt-04` | **FAIL**: tranche leaf deadline in `S4-melee-retarget` step 0, then cleanup deadline. | `F4-melee-engage`, `S4-ranged-retarget` (with continuation) and `S4-controls` completed. All three pass the current row readers (reader-only, immutable evidence). A native fixture-floor hostile (`a1855a47…`, not a spawned row target) damaged the rider during the controls row and kept the pair in combat, so the melee row's out-of-combat start condition never held. `S4-melee-retarget` and `S4-hold-no-chase` did not run on f4d. They passed in r01 on f4a, whose product behaviour is identical. Bounded known issue: fixture premise. |
| `20261106-finalize-f4d-surface-05` | **PASS** (external owner-surface reader, F4 exploration contract). | See requirements 2 and 5. Disclosed setup: camera scroll per frame, one 3 m rider step so the rider is the closest selected non-pet, and the native Slowed condition for the control leg only. |
| `20261106-finalize-f4d-dungeon-06` | **FAIL**: `NullReferenceException` in the scenario after native `Go`. | See requirement 3. |

The first r04 launch request was refused before any lock or launch by the batch guard: two failed equivalent attempts already sat in batch `finalize-f4:ai-1`. It ran under batch `finalize-f4:ai-2` as a materially changed experiment (new candidate, return/clearing step, stricter reader). No start was charged for the refusal.

**Completion restoration (r04–r06):** Mods restored, Working fixture restored, baseline immutable, save protection and allowlist passed, inventory digest `60e3d6cc…` unchanged, no game process left.

**Restoration (all three runs):**
- Mods restored, Working fixture restored, baseline immutable, save protection and save-write allowlist passed.
- Save inventory digest `60e3d6cc…` is unchanged.
- No game process, lock or installed candidate remains.

## Visual evidence (inspected; private)

The local packet is `review-packets/finalize-f4/` (lab-relative):
- `finalize4-gait-mounted-party.png` (`202c46a4…`): the Horse mid-stride under party pace, beside the just-opened stash chest.
- `finalize4-gait-mounted-solo.png` (`d0c106ac…`): the native solo control.
- `CAPTIONS.txt` and the re-evaluation logs.

A still frame shows a stride pose only. It does not prove gait quality or rule out foot sliding. **No approved transfer route exists in this tooling, so nothing was published or uploaded.**

## Remaining work and known issues

**After the completion mission:**
- WORLD-01: re-run `persistence-p07-save` `area-dungeon-entry` on a candidate with `c1c56439`. This needs a new start and authority. Then run the native reload of its Auto as a cold consumer, which is a further start.
- WORLD-02: INPUT BLOCKED, as described below.
- `S4-melee-retarget` and `S4-hold-no-chase` on f4d: not run (fixture hostile; r04).
- The r03 cause is resolved; the r02 observer gap is resolved (r05 terminal identity).

**First effort (kept as recorded):**

- **WORLD-02 native trap: BLOCKED input.** It needs an owner-made **Manual** save in a pet-permitted area beside a revealed, interactable trap reachable by the pair. The two named retained alternatives were inspected:
  - `KMC_S2_ROGUELIKE_SURFACE_CANDIDATE` (the hub) has no trap.
  - `KMC_S2_DUNGEON_FLOOR_CANDIDATE` (`Auto_1121`) has 3 active traps but is an Auto save; the owner-seed authority admits Manual saves only (Sprint 2 r23). Saved perception state also does not survive a load (Sprint 3).
- **WORLD-01 full transition not run.** Under the owner-surface zero-write authority, the after-entry autosave is a fatal violation. A hub-to-floor load with autosave and reload needs a declared one-write authority extension (about 8 files). The causal boundary itself is shown natively. The excluded-area (capital) detach is unchanged code, with native evidence only on older bytes.
- **Not shown natively:**
  - the locked or unreachable chest neighbour (the hub has none), offline only;
  - turn-based continuation (same code path);
  - native slow-gait controls (encumbrance, stealth, terrain), offline policy only.
- **r03 Hold-in-bow-reach stall:** cause unidentified; see above.
- **r02 observer gap:** the carried outcome is read before it is published. A later run should read the bound approach's own outcome.
- **Owner UX decision:** under party pace the Horse now shows its run cycle at about 60% of its solo rate. Whether that reads as a natural trot or as foot sliding is the owner's call.

## Completion mission: identities, reviews, spend

- **Authority:** the forwarded completion mission raised the start ceiling from 3 to 6 on the same `finalize-f4` record. It was recorded append-only as `startCeilingAdjustments` (2026-11-06T17:39Z); the time ceiling stayed 75 minutes. The dungeon entry declared one write (the after-entry Auto) and one load. No floor-to-floor control was combined.
- **Commits:**
  - `d8b54943` (Hold premise, decision snapshot, chest terminal observer, chest during travel, F4 gait contract);
  - `5bfac302` (review corrections);
  - `ed99320f` (P07 `area-dungeon-entry`: one leaf `Auto_1.zks`, Area = floor, AdmissionArea = hub, write cap 1, load cap 1; a transfer-admitted leaf's write lease opens only in its commit area);
  - `8216d551`, `b57712ef` (review corrections);
  - `c1c56439` (r06 correction, after the runs).
- **Candidate f4d:** head `b57712ef` · DLL `2c8e3522…` · MVID `07a436fa-51d3-41b3-accc-20dc875af14d` · package `dfa0b231ba6880046b35a0db94babecf26c5b82853d1b160bfef91abd5d1a5df` · suite `finalize-f4d-b57712ef-20261106` (`ec45fd75…`).
- **Reviews:**
  - Independent review of `d8b54943`: one MAJOR (the return move armed the player-move hold), fixed in `5bfac302`; MINORs 1–8 applied.
  - Independent review of `ed99320f`: no MAJOR. MINORs 1, 2, 4 and 5 applied in `b57712ef`. MINOR 3 (record the authority's admitted-write count before `Go`) is deferred: the scenario does not hold the authority. The `workingSaveRequestCount` quota and the timeout cover it.
  - The MINOR 2 check is what exposed the r06 defect.
- **Checks:** Fast ×7, Candidate ×1 (12/12; harness PASS, manifest contracts 756/0), one package, one suite, offline request-shape validation of all three requests, and three WhatIf runs. Synthetic readers:
  - Sprint 4: 141/0;
  - owner surface: 216/0;
  - persistence fixtures: 657/0 (26 dungeon-entry faults, each checked to fail for its intended reason).
- **Spend:** `finalize-f4` **47.88 of 75 min**, **6 of 6 starts**. The launch helper refuses a seventh offline.
- **Private packet:** `review-packets/finalize-f4/r05-motion/` (lab-relative): 12 hash-verified frames and `CAPTIONS.txt`. Nothing was published.

## Spend and state (first effort, superseded above)

- **Budget** `finalize-f4`: **23.85 of 75 min**, **3 of 3 starts** (r01, r02, r03). The launch helper refuses a fourth start offline.
- **Checks:**
  - Fast ×5;
  - Candidate ×2: harness 281/0, manifest contracts 756/0, unit tests 1087/0, Sprint 4 reader 130/0, owner-surface reader 194/0;
  - 3 packages and 3 suites (charged as imported entries);
  - 3 WhatIf dry runs;
  - offline request-shape validation;
  - a full world-reader chain check on the r21 artifact shape.
- **Not run:** the Full tier and persistence or cold-load campaigns. No persistence schema changed.
- **State:** no merge, tag, release, install or F5.

## Owner check (short)

1. With the whole group selected, click a chest well away from the mounted rider: the Horse should walk over and the rider open it once.
2. Ride from the Beneath the Stolen Lands hub into the dungeon through the party screen: the riders should stay mounted. Then go one floor deeper.
3. In real-time combat with AI on: a Rapid Shot archer's leftover arrows should go to the next goblin after a kill, and a melee rider should ride out to meet approaching enemies.
4. Walk the group: the Horse should trot at the group's pace. Judge the look.
