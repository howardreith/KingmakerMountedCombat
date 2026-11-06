# F3 review handoff: movement economy, paired initiative and mounted HUD

## Disposition

**F3: ENGINEERING PARTIAL. Ready for review; owner acceptance is separate.**

- **Product changes:** all six requirements have a product change (ECON-01, ECON-02, HUD-01, HUD-02, HUD-03, TURN-01), plus item 8 (actor-named refusals).
- **Native evidence:**
  - Grouped TB run r03 recorded all seven rows. Under reader r2, every F3 assertion holds on those recorded rows.
  - The RT neighbor r04 PASSES.
- **Why PARTIAL, not COMPLETE:**
  - **No TB run has a PASS verdict.** r03 ends FAIL: after the rows, the tranche's fixture cleanup timed out with the game in `EscMode`.
  - **One ECON-02 branch is not exercised natively:** a full-round rider attack while the mount is still unmoved.

All four starts are used.

## Identities

| Item | Value |
|---|---|
| Repository / branch | howardreith/KingmakerMountedCombat · `codex/kmc-finalize-f3-movement-economy-hud` |
| Integrated base | origin/main `81702786` (the owner merged F2 PR #17; same tree as `75039001`) |
| Final compiled source | `fc62e9b71e63168a0bd92d47d05cdaffff4f83b2` (candidate `finalize-f3c`). Later commits change only the reader, scripts and docs. |
| DLL | SHA-256 `e7701bc286a41507b8cf2383ef8afac02cd278e892a1130eaaa10d53c43a1640` · MVID `d3257036-0b2f-4498-8272-e969f4ed8e12` |
| Package | `artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f3c-diagnostic.zip` (lab-relative) · SHA-256 `171b094fbee901effa263f090a783c03d928676db507a17d45f9743771ebde33` |
| Suite | `finalize-f3c-fc62e9b7-20261106` · snapshot SHA-256 `41710d219e300f253f218cf0ffa6a7bf8b60b819defbd80cb35863bb1c83f51a` |
| Displayed version | `0.1.0-chunk7b-preview.103`. This is shared with F2 and is **not** sufficient identity. |
| Engine | Kingmaker 2.1.7b, Assembly-CSharp `3b6450ff…`. Base-game staging plus the retained CotW/CMI selection. Working fixture `KMC_AUTOMATION_WORKING` (`5eb4e0b4…`). |

## Causal findings and correction

| Requirement | Before: cause established from source and native contracts | Change | Native outcome (r03 rows, r04) |
|---|---|---|---|
| **ECON-01** | Execution already charged only the mount (`TickDelegated` writes the mount's MoveAction). With the rider selected, which is the default, the path, step mode and prediction read the rider's never-charged `TurnController`. A mount that had moved still showed a fresh green line, and a second "green" move silently became the native conversion of the mount's Standard (`MoveAction > 3`). The owner's S12 refusal followed a genuine rider attack. | The path settings and paired execution read the mount's movement context (`PairedMovementContext`) whichever pair actor is selected. Rider-selected prediction states for the path come from the mount's real budget. | A2: a 6.10 m move charged the mount 1.49 s of Move; the mount's Standard stayed 0 and the rider stayed 0/0. The preview then showed 6.14 m green and 18.33 m total, from the mount. A3: the mount attacked, with Standard 6. D1: a 15.42 m move took the mount's Move to 5.26 s (the conversion); the preview showed the remaining 3.02 m. D2: the mount attack was refused with "Mastodon has no Standard action left this turn. Select cvb to attack with it." |
| **ECON-02** | The rider's exhausted turn forced native five-foot-step mode on the rider context, which also drove the pair's execution. | The mount mirrors only explicit ground limit choices: the step key and a cycled ground limit. It does not mirror limits forced by the rider's spending or by a hovered target's smart variant. Native `TrySelectMovementLimit` then validates the limit against the mount. | B1: the rider attacked (Standard 6) with the mount unmoved. The preview read the mount at 15.24 m green and 30.48 m total. B2: the mount moved 6.10 m on 1.20 s of its own Move. **Not exercised:** the fixture rider made a single attack here (Move 0). The full-round case (rider Move 3, so the rider has no normal movement) depends on the mirror branch, which is native-unverified. In r02 the old mirror turned a full-attack approach into a mount step; that defect is what the final mirror rule fixes. |
| **HUD-01** | The step was tracked on the mount's allocation, but the rider context still advertised a step or normal movement. | Same movement context. | S1: the whole step was spent (1.71 m, mount Move 0). The preview then showed 0 m, and S2 moved 0.00 m. A **partial** step leaves the native residual step (`DoesCommandEmptiesMovement` is false for a move); r02 showed the preview agreeing at 1.30 m. |
| **HUD-02 / D4** | The rider context drew a step line after the Charge, while execution already refused travel (`TimeForced > 0`). | Same movement context. D4 reuses the existing persisted allocation record (`TimeForced`), with no save schema change. | C1: the Charge was delivered with 9.76 m of forced travel; the rider is at 6/3 and the mount's Standard stayed 0. C2: the preview showed 0 m. C3: a move and a step were both refused, with no position or cost change. C4: the mount's retained Standard attack was dispatched (Standard 6). |
| **HUD-03** | Native `ClickPointerManager` draws a marker at alpha 1 only when its unit is selected. The pair's command runs on the mount while the rider is selected, so the marker drew at alpha 0.5. | Its one `SelectedUnits.Contains` call is transpiled: the pair's mount counts as selected when its rider is. | r04: the mount's marker was at alpha 1.0 in all 138 frames while the rider was selected; the unmounted control was at alpha 1.0. The TB marker was not measured separately; it uses the same hook. |
| **TURN-01** | Native `ChooseNextUnit` already skipped the paired mount. The tracker's projection filter only ran in legacy unified-turn mode, so horses stayed in the strip. | The tracker omits exactly the mount that the candidate loop skips (`HidesPairedTrackerEntry`, which has no side effects), and never the current unit. Scheduling is unchanged. | r03: no mount entries while mounted; mounted mounts got no native turns. Reader r2 checked no duplicate preparation and the mount's reappearance and its own turn the round after the dismount. |
| **Item 8** | The refusal said "this actor's attack" without naming anyone. | The refusal names the spent actor, and the partner if that partner can still attack. A per-actor cost trace is logged. | A4: "cvb has no Standard action left this turn." A capture shows "VBNM HAS NO STANDARD ACTION LEFT THIS TURN. SELECT HORSE TO ATTACK WITH IT." |

There was no native reproduction **before** patching. The cause was traced through the source and native contracts. All native traces are post-fix and actor-specific.

## Wrath comparison

See [FINALIZE-F3-RULES-COMPARISON.md](FINALIZE-F3-RULES-COMPARISON.md). It covers Wrath 2.7.0 (`eb82d8f2`, Assembly `2cb7160b…`) and was a static, read-only reading. In that build the tracker skips a unit that has a rider, the pair shares one turn, and the mount's own budget moves the pair. Exact visual parity is unverified.

## Native outcomes (original verdicts kept)

| Run | Candidate | Verdict | What happened |
|---|---|---|---|
| `20261106-finalize-f3a-tb-01` | f3a `0bfb937a`, DLL `da062a36…` | **FAIL** | Setup: the second owner's Ranger companion acquisition had no level-up entitlement. Sprint 6 grants experience first. |
| `20261106-finalize-f3b-tb-02` | f3b `276c2a16`, DLL `085e88f2…` | **FAIL** | Four rows ran. The Charge-lane step outlived the 30 s leaf deadline: its bound was 40 s, so its facts were lost. The old mirror defect described under ECON-02 was observed here. |
| `20261106-finalize-f3c-tb-03` | f3c `fc62e9b7` | **FAIL** | All seven rows recorded. Afterwards the tranche cleanup timed out: the party stayed in combat with no enemies, the game was in `EscMode`, and leave-combat time does not advance there. **Reader r1** stops at two expectation errors: B1 assumed a full-round rider attack, and D1 read native `UsedStandardAction`, which does not count the two-move conversion. **Reader r2**, analysing only the seven F3 rows, finds every F3 assertion holds. The run verdict stays FAIL. |
| `20261106-finalize-f3c-rt-04` | f3c `fc62e9b7` | **PASS** | 5/0. The external F3 reader passes. |

All four runs restored exactly: Mods restored, Working fixture restored, save protection passed, save inventory digest `60e3d6cc…`.

## Visual evidence

Six inspected normal-camera captures are in the lab, not committed. They are controlled setup: the path lines were requested through the native `CalculatePathForCommand` for a scenario-chosen point.

| Capture | Path (lab-relative) | SHA-256 (prefix) |
|---|---|---|
| Initiative strip: riders and hostile only | `runtime-evidence/20261106-finalize-f3c-tb-03/movement-visuals/finalize3-tracker-round1.png` | `0abe6fff…` |
| Rider selected after the mount's move: green for the mount's remaining Move, then yellow | `…tb-03/movement-visuals/finalize3-a2-after-move.png` | `21946d34…` |
| After the rider's attack: the horse's full path, plus the named refusal banner | `…tb-03/movement-visuals/finalize3-b1-after-rider-full-attack.png` | `0144ba4f…` |
| After the whole step: no line | `…tb-03/movement-visuals/finalize3-s1-after-step.png` | `7846c42c…` |
| After the Charge: no line | `…tb-03/movement-visuals/finalize3-c2-after-charge.png` | `1db89c83…` |
| RT: bright destination ring for the pair's travel | `runtime-evidence/20261106-finalize-f3c-rt-04/movement-visuals/finalize3-rt-marker.png` | `a449225a…` |

Committing these needs the owner's approval through the publishing helper, as F1 did.

## Remaining concrete issues

- **Selected gap: no TB run has a PASS verdict.** The cause of `EscMode` during cleanup is unexplained. F3 code does not enter it: `PauseBind` only ends the TB turn, and UMM was closed at every capture. This is a bounded known issue in the harness cleanup, not a product verdict.
- **Selected gap: the ECON-02 full-round case is native-unverified.** The full-round mirror branch needs one native row with a rider that full-attacks without an approach.
- **Owner UX limit:**
  - The prediction panel's Move slot still follows the selected rider's own Move action.
  - The rider's portrait carries no mount badge.
  - Hovering the mount in the world maps to no tracker entry.
- **Explicit D4 boundary:**
  - A charge that travelled and was then cancelled also closes travel, as the native stock charge does. Nothing is refunded.
  - D4 applies to the TB activation. A mode change forfeits that activation natively, and renewal needs at least 6 native seconds.

## Spend and state

- **Budget:** record `finalize-f3` at 40.00 of 90 min; **4 of 4 starts used** (three TB, one RT).
- **Batches:** `finalize-f3:economy-1` for r01 and r02. `finalize-f3:economy-2` was declared for the changed experiment of r03 and r04.
- **Checks run** (the scenario author also ran two Fast tiers):
  - Fast six times, during development. One of those ran before an edit applied, so it tested unchanged code.
  - Candidate twice: 11/0 on `0bfb937a`, and 10/0 with `-SkipBuild` on the f3c DLL. Both include 31/31 native IL patch constructions and Test-Harness 756/0.
  - Reader test 80/0.
- **Not repeated:** Full tier, persistence campaigns and cold round trips. Not needed: the D4 record is the existing persisted `TimeForced`, with no schema change, and an offline test covers it.
- **State:** no game is running, no lock is held and no candidate is installed. Protected saves are untouched. No merge, release or install.

## Next decision

**Small owner check** in TB:
1. Move the horse partway with the rider selected, then select the horse and attack.
2. Attack with the rider, then move the unmoved horse.
3. Take the step, or Charge, and look at the line.
4. Look at the initiative strip.

A review correction that changes product bytes needs a new owner decision on starts.
