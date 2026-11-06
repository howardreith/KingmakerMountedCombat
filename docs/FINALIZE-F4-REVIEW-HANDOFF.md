# F4 review handoff: ordinary world orders, native AI, transitions and Horse gait

## Disposition

**F4: ENGINEERING PARTIAL. Ready for review; owner acceptance is separate.**

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

## Spend and state

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
