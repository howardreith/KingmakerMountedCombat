# Sprint 7: usable Mounted Charge (CHARGE-01)

**SPRINT 7 ENGINEERING COMPLETE - READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

## Identity

| | |
|---|---|
| Baseline | `main` `4efd45a0`, the real merge of PR #11 |
| Final compiled source | `ca10a71d`, candidate sprint7l. Later commits are docs only |
| DLL | SHA-256 `aa21102355b6714005537d706a0413806c404b167926ba020f0f9efdc057f80d`, MVID `49f21a56-9601-4ca1-a5da-0525394ddd9c`, 8,730,112 bytes (limit 9 MiB) |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint7l-diagnostic.zip`, SHA-256 `825bc569941708aa3aa0213d2b8a2150ef6efa8badb26e59497bf078f7b9e648` |
| Suite | `sprint7l-ca10a71d-20261103` |
| Earlier accepted candidates | sprint7i, `31f48c19`, DLL `cc7ffb1eed2454fc80c1fc9d0935caffe66894ca0f6651880e3fba0250f16ad6`, MVID `bf09bc6f-ba41-478f-a44d-c31867ba5e07` (a01 to a05). sprint7j, `5f7c214b`, DLL `b1cfd8d40f5d8f03955ef6a123176c66f5b7d6b4ceed387f4b425642645cd649`, MVID `6af2075f-5a09-4494-8d50-a3e73c007b09` (a06) |

The final source changes shared admission, cleanup and lane code since sprint7i and sprint7j. a01 to a06 stay on their own bytes and are not relabelled; see Evidence for what the corrective runs do and do not re-exercise.

## What was wrong and what changed

The previous state: real-time Charge existed, off by default; turn-based was a blanket refusal after preview.176 spent a round, travelled 0.169 m and delivered no attack.

| Blocker | Evidence | Repair |
|---|---|---|
| Off by default | `DiagnosticSettings` | On by default. The mod has no settings file, so the toggle is session-scoped and an opt-out lasts until the next launch |
| TB carrier lost its line | preview.176 log: `moveResult=Interrupt` five times; native `TickApproaching` calls `PathTo`, whose path completion clears force mode. The stock charge holds no approaching command at all | The TB carrier sets `NextApproachTime = MaxValue`, so the lease alone drives it, and re-forces a stopped line as the stock TB routine does. a02: one forced path, 9.6 m, attack delivered |
| Busy pair refused everything | r01: in real time the rider's native brain nearly always has an attack running, and "requires an idle pair" refused the click. Earlier fixtures isolated AI | Charge replaces the pair's ordinary attack or move, as stock does. A door, door recovery, a running charge or a mount's released swing still refuses |
| A brain proposal ended a charge | `AdmitChargeCommand` read any unowned command as a deliberate replacement | A proposal carrying `AiAction` waits for the charge. A deliberate order still replaces it |
| An old attack's repath retired the shell | r03: barrier `pair-command-replacement` before delivery | Replaceable work ends at the shell's admission, after the shared decision allows it |
| No AC penalty | The buff was removed as soon as the attack ended | A delivered charge hands the native buff to its native round. A cancelled one still removes it. An earlier native buff is replaced at the next charge, so charging round after round is not refused |
| Lane read at the centre only | A Mammoth's body is wide | Both flanks at 0.8 corpulence, at half-corpulence steps, up to the landing point, at admission. The ground already occupied is not asked |

The refused-target message now gives the shared decision's reason. Stock Charge while mounted says to use Mounted Charge.

## Contract

Rule source: Pathfinder 1e, d20pfsrd combat page: charge is a full-round action, at least 10 ft and up to double speed directly toward the opponent, a clear path, one melee attack, +2 attack and -2 AC until your next turn; if your mount charges you take the AC penalty and, attacking at the end, the bonus. Native values below are from the installed assembly's `AbilityCustomCharge`.

| | Native / rule | Mounted Charge |
|---|---|---|
| Input | Ability, target selector | The rider's **Mounted Charge** ability and the native target handler. Rider Primary and Mount Primary stay retired |
| Minimum | TB: 5-ft step + min weapon range + both corpulences. RT: 10 ft + both corpulences | The same, read from the mount |
| Maximum | `CombatSpeedMps * 6` | The same, from the mount |
| Line | Navmesh trace from the caster to the target | The same, plus the flanks |
| Landing | Nearby unit inside 0.8 of corpulences blocks | The same, with the rider's reach |
| Speed, path | Double speed, forced path | On the mount's agent only. The rider's agent is untouched |
| Already in reach | Below minimum | Also refused when the target is inside the rider's weapon reach from the mount |
| TB eligibility | Own turn, `TimeMoved == 0` | The rider's own open turn; rider Standard and Move free; the paired turn can address the mount; mount not moved; mount Standard free. Otherwise refused with nothing spent |
| Rider cost | Full round | Native shell: Standard 6 and Move 3 in TB, Standard in RT |
| Attack | One, `IsCharge`, native rolls | One native attack from the rider, at the mount's stop inside the rider's reach. Only the rider's own Pounce lifts the single attack, and the engine decides |
| Modifiers | Rider buff +2 / -2 AC for 1 round | The native buff, on the rider alone |
| Interruption | Ends without attack | Target loss, Stop, replacement, mode change or a mount that can no longer move end it. The committed cost stays, the effect goes |

Declared CRPG adaptations, for review:
- **The mount pays its travel.** In TB it pays Move equal to the seconds travelled (about 1.2 s for 9.6 m), through the existing delegated-movement ledger, and native force-mode rules leave it no further movement that turn. It keeps its own Standard.
- **The mount's attack stays its own.** It may attack by selection on the same turn or by its own AI in real time, with no charge bonus and from its own reach. No automatic joint attack was added. If the rider's stop is outside the mount's reach, the mount needs an ordinary approach it can no longer make.
- **The mount takes no -2 AC.** The rule text concerns the rider's penalty when the mount charges; the mount's own penalty is not modelled.
- **A spent partner refuses.** A mount that moved or attacked first means no charge that turn, and it is not reset.
- **A ranged rider is refused** before anything is spent. No mount-only charge was added.
- **The stock Charge button is not redirected.** It stays refused with a message naming Mounted Charge.

## Evidence

Budget `sprint7`: 77.75 of 90 minutes at the final Candidate, no adjustment, 15 launches including the corrections below. Runs are under `runtime-evidence\`, prefix `20261103-sprint7-`. Each restored exactly: the Mods tree was empty before and after, the same as each run's captured start.

| Run | Candidate | What | Verdict |
|---|---|---|---|
| `a01-rt` | sprint7i | Ordinary click, too near and obstructed refused, charge with a forced natural 20, one `IsCharge` attack in the native critical branch with native damage rule, +2 against the ordinary attack (8 vs 6), AC 15 to 13 to 15 at expiry, spent refusal. Stop and target death keep the cost, deliver nothing and leave no buff | PASS |
| `a02-tb` | sprint7i | Rider first, natural dice, no forced die. Same turn, activation and round. Rider 6/3, mount Standard 0 and Move 1.21, no more mount movement. Spent refusal, spent-partner refusal, next activation renewed with the penalty gone, a further charge | PASS |
| `a03-tbm` | sprint7i | Mount first, initiative -35 / 55. The same observations | PASS |
| `a04-save` | sprint7i | Delivered charge on the first rider's later turn, then a native save. Rider 6/3, mount Move 1.21, one buff, second pair's actions independent | PASS |
| `a05-cold` | sprint7i | Separate process, archive `e4d49e58...`. Same cost and turn, one buff, no shell, no owner, no cast, no charge attack. Next turn renews, buff gone | FAIL retained (unregistered row name), re-evaluated PASS |
| `a06-pounce` | sprint7j | Disclosed setup. Pounce on the mount: `fullAttack` false, one attack. Pounce on the rider: `fullAttack` true. The fixture rider has one iterative attack, so the full attack's count stays 1 | PASS |

Preserved earlier attempts, all restored:

| Run | Candidate | Verdict and cause |
|---|---|---|
| `r01-rt` | sprint7b | FAIL: busy pair refused; rows unregistered |
| `r02-tb` | sprint7c | Launcher FAIL (compound list); the retained artifact passes the reader. First native TB delivery |
| `r03-rt` | sprint7d | FAIL: leaf deadline; old attack's repath retired the shell |
| `r04-rt` | sprint7e | No launch: the batch guard refused after two failures; opened batch `accept-2` for the changed experiment |
| `r05-rt` | sprint7e | FAIL retained, re-evaluated PASS: durable target's temp HP, life flags settle after the rule |
| `r06-tbm` | sprint7f | FAIL retained: schema 51 absent from a master list; the artifact passes the reader |
| `s01-save` | sprint7g | FAIL retained: reader expected the second rider's turn; re-evaluated PASS |

Re-evaluations: `analysis-cache\sprint7\`. Reader: `scripts\runtime\Sprint7ChargeEvidence.ps1`; synthetic checks `scripts\Test-Sprint7ChargeReaders.ps1` (68 PASS). Candidate passed on sprint7i, sprint7j and sprint7l with `Test-Chunk6bCharge` focused (502 reader checks). `Test-Chunk6bChargePath` (142) passed directly on `7e8eccd2`, before the later corrections, and was not rerun.

Reviews: one independent production review before live use (corrections in `7e8eccd2`), one delta review (corrections in `3dda516a`), and one delta review of the queue and lane correction before its live use (corrections in `e45dba6c`).

**Baseline failures, reproduced once on accepted Sprint 6 `8da3272d`, not caused here:** `Test-ChargeOwnershipContracts.ps1` stops at "compiled two-pair native adapter refuses a third reservation"; `Test-ChargeBuffSurface.ps1` fails resolving an installed path.

**Offline only:** ranged rider, too far, mount cannot move, unaddressable mount, already in reach, TB resource refusals; these are `MountedChargePolicyTests`. Ownership per pair is per-controller by construction, seen in a04's archive, with no two simultaneous charges natively.

**Unexecuted:**
- A turn-based Stop or target loss; the real-time routes share the path.
- A mount tripped mid-travel; a landing point blocked by a third creature.
- A wall-adjacent start after the flank rewrite: the `obstructed` control is centre-line.
- The door-recovery refusal.
- A Small or Dog pair: the geometry change depends only on corpulence, so no neighbour was selected.
- The enchantment-visual child path, which keeps the removal instead of the hand-off.
- A rider with real iterative attacks under Pounce.

**Stale historical diagnostics, not selected:** `C7-combat-tb-charge-refusal` (`ProveChargeRefusal`) and the `chunk6b-charge-tb` refusal row prove the retired TB refusal. They would need rewriting before any re-run. Older charge persistence rows that expect no buff after delivery may need the same.

## Review corrections: queue ownership and body-clearance revalidation

Two control-flow findings from the review of PR #12 at `e21625d2`. The review was static and reproduced no gameplay failure.

**A. A pending or refused request must not cancel a live Charge.** Admission used to drain the charge on any unowned request, before it knew what the request would become.

Native contracts inspected:
- Public `Run` clears the queue, then may merge, queue (`TryAddToQueueInsteadOfRunImmediately`) or displace.
- `AddToQueue` runs at once when the slot is free, and that `Run` interrupts the paired slot.
- `AddToQueueInternal` is the true enqueue.
- Promotion is private `Run(fromQueue: true)`, after `RemoveFirst`, with both slots empty.

`MountedChargeRequestPolicy` now decides by route, and nothing it returns for a live charge is destructive:

| Request during a live charge | Result |
|---|---|
| The charge's own shell, attack or carrier | Admitted |
| Brain proposal | Waits; never ends the charge |
| Rider enqueue, including `AddToQueue` with a free slot | Pending; never reaches the `Run` that would interrupt |
| Mount enqueue, or a mount `Run` native would queue | Refused with a message, nothing changed |
| Any request while the attack waits in the queue, or a mount order during the shell's frames | Refused with a message: native `Run` would clear the attack, or delivery would end the order |
| Queue promotion | Goes back on the queue, neither dropped nor displacing |
| Public `Run` on the rider or mount | Decided by the later gates. A refusal changes nothing. If admitted, native displaces the exact command or carrier, and the charge reaches its own terminal once, keeping the cost |

**Narrow limitation, chosen on purpose:** the mount cannot hold a queued order during a charge, because each carrier's native `Run` clears the mount's queue and the order would vanish silently. It is refused with a message instead. The blanket mount-queue checks in revalidation and carrier admission stay valid for the same reason.

Terminal cleanup leaves a successor alone. The charge's manual target is released at its terminal, before native `Run` writes the successor's. The forced path is stopped unless a Move or Standard order holds a path of its own, decided by path identity (`HoldsForcedLine`), so a Free or Swift command such as an attack of opportunity cannot leave the line running. An attack cleared from the queue by native `Run` is detected and drained.

**B. A forced line must meet the same body rule.** Revalidation fed only the centre trace.
- `MountedChargeLane` is now the one rule: both flanks at 0.8 corpulence, at half-corpulence steps, including the landing footprint, skipping ground the body already covers. Admission and revalidation share it.
- `LaneClear` defaults to false in the `BeforeRepath` phase, the only phase that precedes a forced line, so a caller feeding the centre alone fails closed.
- Enforced at: the first `Apply`, every `Repath`, each new carrier's re-force, and the stopped-line re-force (`NeedsReforce` gates `Maintain`). A running line reads nothing, so there is no per-frame scan; the legacy tick reads the lane only before a re-force.
- Later phases are unchanged: the native attack admission, landing check and final range still decide arrival. Minimum distance and free actions are not re-asked after commitment.

**Focused checks** (`Test-Sprint7ChargeAdmission.ps1`, the compiled controller and carrier on real native container and command types; `MountedChargeRevalidationTests`). Against the reviewed DLL `b1cfd8d4...`, the admission script reported **PASS=2 FAIL=6**, including a drain on a merely pending follow-up. After: **PASS=11 FAIL=0**. The lane and route decisions are unit tests of the production functions with narrow and wide mounts, and readers now have 83 synthetic checks. A brain proposal's `AiAction` is a Unity object whose null test needs the engine, so that case is covered by the policy test and the native rows, not the adapter.

**Native confirmation:**

| Run | Candidate | What | Verdict |
|---|---|---|---|
| `20261104-sprint7-q01-rt` | sprint7k | The refusals and pending follow-up held with the same command, forced-path count, cleanup count and cost, and the charge delivered. The row then stalled: a raw queued rider attack cannot close its own distance | FAIL retained; fixture, not Charge |
| `20261104-sprint7-q02-rt` | sprint7l | Same requests with the brain live. Follow-up promoted after delivery. A ground order replaced a charge: Interrupt, cost kept, no attack, no buff, and the successor moved 2.11 m. A target moved to a clear lane: followed and delivered, forced count 2. **A live centre-clear, flank-blocked lane:** "no longer clears the mount's body", forced count stayed 1, no attack, cost kept | PASS |
| `20261104-sprint7-q03-tb` | sprint7l | A stopped lawful line re-forced and delivered, forced count 2, rider 6/3. A flank-blocked lane cancelled in the turn: rider 6/3, mount Move 0.02, no attack or buff, next activation arrives | PASS |

Limits of this confirmation:
- In q03 the target moved past the repath distance as well as the path stopping, so the repath's lane read decided it. A stopped path with a lane changed by a smaller move is covered only by `MustAuthorizeReforce` and the same call site.
- The promoted raw attack shows the successor becoming active, not attacking: raw queued rider attacks are not a route ordinary play reaches for a mounted rider.
- Only a Mammoth pair ran natively. Narrow and wide bodies are unit tests of the lane decision, not native evidence.
- Mount-first, the committed save and cold load, and Pounce were not repeated on the corrective source. Those paths share the admission and cleanup code, and a01 to a06 remain on their own bytes.

## Owner play checklist

1. Mount a melee rider, with Sprint 7 installed; no setting is needed.
2. **Real time:** in combat, pick **Mounted Charge** from the rider's abilities and click an enemy 5 to 20 m away down a clear line. The pair runs in, the rider makes one attack, then ordinary fighting resumes.
3. Try a target too close and one behind a wall: a clear message and nothing spent.
4. Press Stop during a charge: the pair stops, the action stays spent.
5. **Turn-based:** repeat on the rider's turn. Rider's turn is then spent; select the mount and attack with it. Then End Turn.
6. Attack with the mount first on a later turn, then try Mounted Charge: refused with a reason.
7. Charge on two rounds running.
8. Save right after a turn-based charge and reload: no second attack, no free action.
9. Watch for a wide mount refused where the lane looks open, or the rider's Charge staying unavailable. Those are the likeliest tuning points.
