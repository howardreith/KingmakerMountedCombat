> Current qualification: the owner subsequently authorized final execution, and
> the unchanged preview.211 campaign, both completion ledgers and final FULL passed.
> See [the engineering closeout](CHUNK6-ENGINEERING-CLOSEOUT-2026-10-11.md).
> The record below is historical: its assumed outcomes and original failures
> remain unchanged. Its old next-command recipes are not an active assignment.
> Human acceptance remains pending; the October 11 focused policy still governs.

# Chunk 6 closeout summary — 2026-10-11

Status: **ENGINEERING WORK COMPLETE ON THE NAMED CLOSEOUT SCOPE; FINAL ACCEPTANCE GATE NOT RE-RUN ON THE RELEASED
CANDIDATE.** Merged to `main` and released at the owner's explicit instruction of 2026-10-11, which also instructed that the
running verification be stopped and that the remaining stages be assumed to pass. This document states exactly what was
observed and exactly what was assumed, so nothing here is mistaken for proof.

## What this release contains

Released candidate `0.1.0-chunk6d-preview.211`:

| Identity | Value |
|---|---|
| Source commit | `a0d70fa41f25498aa2f31dfdfebc1ad29750f4a6` |
| Product DLL | `11e993093d2042f615cfc9d6aecc33d630d2e41e8125dd680656b6a1057a48ea` |
| DLL MVID | `0aa68ba0-f293-480e-8ecb-d7bdaeca73d0` |
| Diagnostic package | `e89f431ae1303b217e987b2ff12171a05bac434d97809962c3596e2942874039` |
| Freeze record | `candidate211-freeze.json` `272507c6fc980c352ec94c17f91357bea52ca745a37f87672d77d16bd01223ae` |
| Stage plan | `final-plan211.json` `8707079d77f7c2ff7f5701b8d42a8559cdbdb345b169f76e17198f33386ca517` (109 stages) |
| Purity proof | exit 0 in 126.9 min, receipt `b912089a7525150f3ea991ec5ae614ba5532086ac070bf8213014ec0909830cc` |

All fourteen offline gates hold zero-exit receipts at that commit against 697 tracked inputs and a clean worktree: build,
components, compiled observer, the closure, foundation, 6D, 6E and positioning readers, input registration, FAST, CANDIDATE,
package, observer package and suite snapshot.

## Supported features

- **6A legal combat Mount and Dismount** — real time and turn based, the rider's single native Move, paired participation,
  forced detach, persistence through save and load, area reload, cold load, and disable/removal readiness.
- **6B pair-owned Mounted Charge** — real time supported; turn-based delivery remains `DEFER — EVIDENCED`.
- **6C ordinary mounted spellcasting and item use** — mounted and unmounted, both modes, fifteen rows each.
- **6D staged move-cast-move and double-move ranged action** — both modes, on one native activation.
- **6E Mounted Combat defensive feat** — `FEASIBILITY COMPLETE — DEFENSIVE FEAT DEFERRED; NOT IMPLEMENTED`. The product
  contains no Mounted Combat feat, no reaction command, no negated attack roll and no Ride check. The native limitation
  observed is bounded, not a proof that every design is impossible. Bounded feasibility then defer, under the owner
  authorization of 2026-10-08.

Real-time and turn-based limitations: 6B turn-based delivery is deferred with evidence; a 6A transition delivers on the
rider's Acting turn only, so an input during turn preparation waits; the turn-based cleanup of an incapacitated pair follows
the native join and leave controllers.

## What the closeout resolved

The 2026-10-09 consolidation on preview.205 left a ledger of 68 PASS / 18 BLOCKED / 1 FAIL. Every named gap is now closed:

- **The fifteen previously unimplemented 6A ids** received isolated scenarios in the existing lifecycle, transition and
  persistence families: the seven approach invalidations (left area, view or agent loss, loading cutscene, generation
  change, rider death, mount death, injected exception), turn end, mode exit, the four P04 closure checkpoints (save
  routes, deferred unsettled save, combat area reload, pending-approach area transition), cold load and the schema claim.
- **CM08-area-restoration** is a completed native reload through the supported suspension and restoration lifecycle, with
  identity, views, movement authority and commands all tracked. The legacy `chunk4-area-cleanup` fixture expected an
  `AreaUnloading` delivery the in-area lifecycle never produces; it is obsolete and stays a retained historical failure,
  not a product defect.
- **CM03-mount-spent-standard** issues its native ground order on the Acting turn and settles there.
- **Both composites** (`CM08-persistence-suite`, `CM08-disable-removal-readiness`) were rebuilt from their own members and
  bound to a Chunk 5 persistence ledger of the same payload.
- **The 6D same-activation contract** was corrected and re-validated. `Chunk6dStagedEvidence.ps1` refuses any native
  turn-transition or round-refresh boundary inside a claimed activation, requires one native turn identity in turn-based
  mode, and requires the row to open on the rider's own turn. `C6D-double-move-ranged` binds the two mount Moves and the
  rider ranged attack with the rider's Standard available immediately before the attack and exactly one native Standard
  cost on it; `C6D-move-cast-move` binds both legs and the cast. Negative validators refuse a ranged attack on a refreshed
  turn, one whose boundaries carry another turn identity, a turn end inside the activation, a missing rider Standard, a
  real-time round refresh between the Moves and the attack, a second leg on a refreshed turn, a second leg from a refreshed
  allocation, and a second leg that consumed none of the retained movement time. The later scroll cast on a refreshed turn
  is accepted only as a separate control.

## Product defects found and fixed by running these cases

1. **preview.209 — deferred save deadlock.** A deferred native save held the start of the very relationship shell its own
   readiness term waited for, so the hold and the readiness term deadlocked until the 30-second bound refused the save.
   `MountedPatchController.SaveCommandStartPrefix` now exempts a command the save itself awaits.
2. **preview.210 — delivered ranged attack reach.** A mounted ranged full attack's plan ends with the mount's natural melee
   attack, which cannot reach when the pair fires from range, so the engine interrupts with target-invalid after the ranged
   attacks. The check that distinguishes that legitimate completion from a dropped intent required every delivered attack
   to satisfy `distance <= bodyRadius + weaponRange` with no tolerance, while the sibling term for the remaining melee
   attacks already allowed the product's own 0.05 m `RangeTolerance`. Because the native approach stops the pair exactly at
   the envelope, an 8 mm overshoot (16.84816 m against 16.84 m) turned the completion into a cancelled intent and stopped
   repeated fire after one routine. The delivered-prefix comparison now uses
   `MountedCombatSpatialPolicy.DeliveredAttackWithinReach`.

Harness-only corrections, product untouched: the closure settled-row capture rule, the 6E refused-Swift never-rises rule in
both modes, the 6C partner clear nested in its own later native Prepare, and the chunk4 ranged-tail reach tolerance. Each
was proven against retained immutable evidence before being committed, and the harness evaluations show they change only
the rows they target.

## Evidence actually observed

| Candidate | Source | Native campaign result |
|---|---|---|
| preview.206 | `e7c28020` | stages 1–15 ran; retained partial campaign `ca12e57d…` |
| preview.207 | `b90d75a8` | 70 stages: 63 PASS, 7 FAIL (all fixture or reader contracts); purity 97.5 min |
| preview.208 | `4ce6621a` | 109 stages: 105 PASS, 4 FAIL retained; closure `666b883a…`; purity 101.9 min |
| preview.209 | `27ca8fce` | 109 stages plan a: 102 PASS; plan b 7 re-runs: 4 PASS, 2 reader-only with native PASS, 1 FAIL; closures `cce64ae5…` and `bcaf523b…`; purity 108.5 min |
| preview.210 | `ad9c6d9c` | 109 stages: 106 PASS, 3 retained; plan b 3 re-runs: 2 PASS, 1 FAIL; purity 126.9 min |
| preview.211 | `a0d70fa4` | purity exit 0; **campaign stopped by owner instruction at stage 25 of 109 with 24 PASS and nothing retained** |

**The most complete validated acceptance state is preview.209**, retained in the lab and copied into this repository for the
record:

- `docs/chunk6a-ledger-preview209.json` — 86 PASS, 0 FAIL, 1 BLOCKED, record checks 87/87, 40 retained failures bound.
  Built under reader commit `7f24c685`; it validates only under that harness identity.
- `docs/chunk5-ledger-preview209.json` — 106 entries, 105 PASS, 0 FAIL, 1 non-mandatory NOT RUN; record checks 106/106;
  **completion gate PASS: all 105 mandatory Chunk 5 behaviours PASS on that payload.**

The single blocked id on that ledger was `CM08-sustained-rt`, whose ranged approach-repeat row could not be set up. A
development run on preview.211 (`dev211-4-sustained-ranged-rt`) produced that row for the first time on any candidate:
native PASS 65/0 with all four sub-case rows PASS, and the corrected reader accepts the artifact. That is a single
development run, not campaign evidence.

## What remains to be done

1. **Re-run the acceptance campaign on preview.211 and rebuild both ledgers.** The 109-stage campaign and the 105-row
   Chunk 5 matrix were not completed on this candidate. Until they are, no ledger describes the released binary, and
   `CM08-sustained-rt` has no campaign evidence. The handoff document gives the exact commands.
2. **`docs/chunk6a-ledger.json` is the accepted preview.205 artifact and is stale.** Its recorded harness identity no
   longer matches the repository's readers, which the 207, 208, 209 and 211 harness corrections changed. This was already
   true before this closeout. `Test-Chunk6aLedger.ps1` therefore fails its harness-identity assertion until the ledger is
   rebuilt on the released candidate. The FULL verification tier fails for the same reason.
3. **HUMAN PLAY acceptance has not been performed and is not inferable from automation.** The 6A checklist is in
   `docs/CHUNK6A-COMBAT-MOUNT.md`; the 6B, 6C and 6D items are in `docs/CHUNK6-CONTINUATION.md` under this date.
4. **Three intermittent conditions remain known and unfixed**, each recoverable by a fresh-prefix re-run: the dismount
   ground-setup engagement flags, the native `achievements.dat` profile leaf growing during an owned persistence process,
   and the P03 preparing-condition cold load. A fourth is a harness observation race where the stage observer reads
   `orchestration.json` before the launcher writes it; the run itself passes.
5. **Retained failures are immutable and must stay so.** Forty are bound in the preview.209 ledger. A later PASS never
   erases one.

## External state

Every stage and matrix row restored its own transaction: Mods, protected saves, baseline immutability, the working fixture
and the save allowlist. The interrupted preview.211 stage 25 also recorded `restored`, and the owner's installation was
verified afterwards to hold the human `0.1.0-chunk5-preview.105` build with its original timestamps and no runtime locks.
No protected save was written. The pinned game installation, UMM and settings were not touched.
