# F3 review handoff: movement economy, paired initiative and mounted HUD

## Disposition

**F3: ENGINEERING COMPLETE. Ready for final review; owner acceptance is separate.**

The F3 completion mission (review baseline PR #18 at `32cc4d78`) selected three unmet results. All three are now demonstrated:

1. **Movement-facing prediction panel.** During a rider-selected ground preview, the panel's five-foot, Move, Standard and full-round slots describe the **mount's** native actions, from the same entitlement as the path and execution. Swift, Free and target, object or ability previews stay the rider's.
2. **A genuine rider full attack** with the mount unmoved, followed by legal mount movement in the same paired activation (r05 rows B1 and B2).
3. **One grouped TB run, r05, passes** through its fixture teardown and the external readers, with exact restoration.

r03's aggregate **FAIL** stands. Its rows remain evidence only at their original single-attack scope (reader r2).

## Identities

| Item | Value |
|---|---|
| Repository / branch | howardreith/KingmakerMountedCombat · `codex/kmc-finalize-f3-movement-economy-hud` |
| Integrated base | origin/main `81702786` (the owner merged F2 PR #17) |
| Final compiled source | `ef5dd11b01f283040e3b1fc6c814aae7b052ff7d` (candidate `finalize-f3d`). Later commits change only docs. |
| DLL | SHA-256 `4a88036bb76b0fb72e3636dacc1089bf118080466bd4bad2f532d0dfe77ac5e7` · MVID `d441e6ef-be47-4eb3-b931-b18a15171fda` |
| Package | `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f3d-diagnostic.zip` (lab-relative) · SHA-256 `1ff161e1caa9e2bb3aa15970fb05da796b35a5fb44ea10447c76cc96f91ecd2c` |
| Suite | `finalize-f3d-ef5dd11b-20261106` · snapshot SHA-256 `f9876e4a07c378963acd92de39e2365afdba6e88b9e1db047d2edf1c6a004bdc` |
| Displayed version | `0.1.0-chunk7b-preview.103`. This is shared with F2 and earlier F3 candidates and is **not** sufficient identity. |
| Engine and dependencies | Kingmaker 2.1.7b. Base-game staging with the retained CotW/CMI selection (`retained-dependencies-final211.json`), installed loader digest `db2c3901…`. Working fixture `KMC_AUTOMATION_WORKING`. This is not acceptance of the owner's full mod stack. |
| Reader | `scripts/runtime/Finalize3EconomyEvidence.ps1` **r3**: TB needs contract v2, RT accepts v1 or v2. Its synthetic test passes 109/0. |

## What changed in this completion

| Area | Diagnosis | Change |
|---|---|---|
| **Prediction panel** | The panel's view model reads the rider's context while the rider is selected. So a mount's partial move showed as the rider's untouched Move, and a rider full attack made the mount's legal travel look spent. Native also only advances the mount's private action states on the mount's own command events, which rider-selected pair travel never delivers. | After the native path writes its predictions for the rider, `FillPairedMountStates` refreshes the mount's own native `CombatAction`s. Their identity, subscriptions and turn-start slider ranges are kept. The current states are recomputed from the mount's cooldowns and restrictions by native `ComputeCurrentActionStates`, with three mid-turn corrections from live native facts: a partial first move keeps its remaining travel; a started double move keeps its conversion travel; `HasFiveFootStep` gives the remaining step. The path then writes the same predictions into them. `ActionsState.UpdateMoveDistance` and `UpdateActionStatesFromPath` read each state's owner. The Move, five-foot and Standard slot getters and the full-round slot's own reads (transpiled) resolve to the mount only during a filled, rider-selected ground preview. |
| **Movement-limit mirror** | The mirrored ground limit was never cleared. A step choice could leak into the pair's next activation, and native `TrySelectMovementLimit` never lifts a step limit while a step remains. | The decision is extracted to `Domain/PairedMovementInputPolicy`, which production uses. It is cleared when the partner context is disposed. |
| **B1 row** | r03's B1 was an ordinary single attack: the default cursor variant, no cycle to Full. | The ordinary native input now does it: hover, cycle to Full with the native right-click (Chunk 4's recipe), then left-click. It records the native `IsFullAttack`, planned and delivered strikes, approach, both actors, the activation and the round. B2 is the rider-selected mount move in the same activation. |
| **Cleanup** | In r03, EscMode began within about one frame of the TB-end unpause, and only one native leave-combat tick ran. The opener is **not identified**: KMC sends no Escape and makes no EscMode call. Only two native paths start EscMode, the Esc menu and a full-screen tutorial. | Every cleanup mode change is recorded. One owned, once-only close goes through the identified native window: `TutorialWindow.CloseWindow` (refused if "don't show" is ticked) or `EscMenuWindow.Show(false)`. An unidentified owner is recorded and left alone. F3 waits now use real time. |

**Review:** one independent read-only review found one blocking defect, the partial mount Move shown as Unable, and five smaller ones. All six were corrected in `ef5dd11b` before packaging. The review confirmed:
- no cooldown is written and no entitlement is granted;
- the transpiler stacks and postfix bindings are correct;
- RT, unmounted and unrelated actors keep native readers.

## Native outcomes (original verdicts kept)

| Run | Candidate | Verdict | Scope |
|---|---|---|---|
| `20261106-finalize-f3a-tb-01` | f3a `0bfb937a` | **FAIL** | Acquisition setup (experience entitlement). |
| `20261106-finalize-f3b-tb-02` | f3b `276c2a16` | **FAIL** | Charge-lane step outlived the 30 s leaf. |
| `20261106-finalize-f3c-tb-03` | f3c `fc62e9b7` | **FAIL** (unchanged) | Seven rows, then the cleanup timed out in EscMode. Reader r1 stopped on two expectation errors. Reader r2 supports the seven rows at **single-attack** scope only. Reader r3 refuses the FAIL artifact. |
| `20261106-finalize-f3c-rt-04` | f3c `fc62e9b7` | **PASS** | RT marker and ordinary movement/combat neighbour. Reader r3 still holds. Retained at its own bytes: the RT path is untouched by this completion. |
| `20261106-finalize-f3d-tb-05` | f3d `ef5dd11b` | **PASS** | 9/9 subscenarios: seven F3 rows and the tranche rows. External reader r3: all assertions hold. Teardown native. |

**r05, decisive rows** (pair B is mount-first; feet are as the native panel reports them):
- **B1, genuine full attack.**
  - The rider was selected, 1.53 m from the hostile, with an estimate of 4 attacks. The Full variant came after 2 right-click cycles, and the hover changed no live state.
  - Native `MountedPairAttackCommand` (a `UnitAttack`) ran with `IsFullAttack` true, `IsSingleAttack` false, **4 planned and 4 delivered** strikes, and no approach. Those strikes were Longsword +1 at +17/+12/+7 and Gore at +10, with 4 attack rolls at the hostile.
  - The rider went from 0/0 to Standard 6 / Move 3, and its `HasNormalMovement` became false.
  - The mount stayed at Standard 0 / Move 0 and moved 0.00 m.
  - The activation `ed9365…:1` and round 1 were unchanged.
  - The path preview read the mount: 15.24 m green (3 s at 5.08 m/s), step mode off.
  - Panel: Move is the mount's at 50/50 ft (WillBeUsed for the hovered 20 ft); five-foot, Standard and full round are the mount's; Swift is the rider's.
- **B2, legal mount movement.**
  - A rider-selected ground click moved the mount 6.10 m on 1.20 s of its own Move. The mount's Standard stayed 0; the rider stayed 6/3.
  - Same activation and round. No extra turn or preparation (the reader's per-round turn and preparation counts hold).
  - Panel: mount Move 29/50 ft.
- **Neighbours.**
  - A2: after the first move, the panel's mount Move is 20/40 ft and not spent.
  - S1: after the full step, five-foot is Used, and Move and Standard movement are Unable.
  - C2: after the delivered Charge, five-foot, Move and Standard movement are Unable, and no line is drawn.
  - D1: after the double move, Move is Used, and Standard shows the remaining conversion travel (29/40 ft) with no attack.
  - The initiative arrangements, tracker, dismount and D4 rows pass as before.
- **Cleanup.** The modes went Pause (130.85 s), then Default (130.86 s), then native leave-combat. **EscMode did not occur, so the owned intervention did not run.** r03's opener remains unidentified; the intervention is a bounded safeguard, not an attribution.
- **Restoration.**
  - Mods restored, Working fixture restored, save protection and the save-write allowlist passed, baseline immutable.
  - Save inventory digest `60e3d6cc…` (unchanged).
  - No game process, no lock and no installed candidate remain.

## Visual evidence (inspected; private)

Six normal-camera captures from r05 are in the lab under `runtime-evidence/20261106-finalize-f3d-tb-05/movement-visuals/`, all with UMM closed:
- `tracker-round1` `d1cd3877…`
- `a2-after-move` `94f9244b…`
- `b1-after-genuine-full-attack` `eb193679…`
- `b2-after-mount-move` `f578b024…`
- `s1-after-step` `9c71cba3…`
- `c2-after-charge` `fd86a2a3…`

Three were inspected and copied, with captions and hashes, into the **local review packet** `review-packets/finalize-f3-r05/` (lab-relative):
- **b1:** the horse's full green Move line after the full attack; the panel Move slot lit at 23.
- **b2:** the panel Move slot partly used at 38.
- **c2:** no line, and the movement slots are greyed.

Path lines in captures are controlled setup: native `CalculatePathForCommand` was run for a scenario-chosen point. The panel values above come from genuine native ground hovers.

**Access limitation:** this mission does not authorize publishing images, and no approved private transfer route exists in this tooling. The images are therefore in the lab packet only and are not committed or uploaded.

**Corrected caption:** the r03 capture `finalize3-b1-after-rider-full-attack.png` (`0144ba4f…`) keeps its name, but it shows the state after the rider's **single** attack. It is not full-attack evidence.

## Remaining owner-facing observations (not selected; no change made)

- **Refusal banner after a rider attack.** After the rider's attack completes (r03 single attack, r05 full attack), the existing stock-attack intent reaches its "resources exhausted" completion branch and shows "*Rider* has no Standard action left this turn. Select *Horse* to attack with it."
  - It spends nothing.
  - The branch is unchanged from the F2 base; F3 only changed its wording.
  - Whether to suppress it after a successful attack is an owner UX decision.
- **Slider detail after a Charge.** After the Charge, the mount's Move slot is Unable but still reports its remaining Move time as distance (32/50 ft). Native `UpdateMoveDistance` does not apply the forced-travel restriction. The c2 capture shows the slots greyed.
- **No mount badge** on the rider's portrait, and hovering the mount maps to no tracker entry. Not selected.
- **D4 boundary as disclosed:** a Charge that travelled and was then cancelled also closes travel, and nothing is refunded.

## Spend and state

- **Budget:** record `finalize-f3` at **51.47 of 90 min**, **5 of 6 starts** (four TB, one RT). One start remains unused and is not scheduled.
  - **Start ceiling decision (append-only):** 4 starts were set by the original F3 mission. The forwarded F3 completion mission (2026-11-06) raised it to 6 on the same record, keeping the 90-minute ceiling.
  - The cap is procedural. The F3 launch helper refuses a seventh start offline, before any validation, lock or launch. This was tested on a copied record: refused at 6 of 6, admitted at 4 of 6.
- **Batches:** `finalize-f3:economy-3` was declared for r05.
- **Checks this completion** (all metered):
  - Fast twice: one build failure, which was a UniRx reference in the probe, then a pass.
  - Candidate once: unit tests 1078/0, harness 756/0, native IL patch construction 40/0, reader r3 109/0, F2 readers 111/0.
  - Package and suite.
  - External request validator: both scenarios pass.
  - WhatIf: development-scoped, pass.
  - One TB launch.
- **Not run:** the Full tier and cold or persistence campaigns. No schema or persisted-state change was made; D4 still reads the existing `TimeForced`. RT was not rerun: its path is untouched and r04 stands.
- **State:** no merge, tag, release, install or F4.
