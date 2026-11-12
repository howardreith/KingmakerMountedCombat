# Horse group pace repair — handoff (2026-11-12)

Branch `codex/kmc-horse-group-pace` from main `bd22a045` (0.2.6; tree identical to `42b8fd97`). Mission: the owner's
"Horse speed repair mission" (prompt plus `output_log.txt`, kept outside Git in the lab's
`horse-speed-repair-mission\`). Scope: the exploration slowdown only.

## Evidence classes

| Class | What it establishes |
| --- | --- |
| Owner log (0.2.5 loaded) | 105 pace summaries for mount `638ced0f…`: 103 at `speedLimit=3.05`, 2 at `5.49`, all `modifiedSpeed=4.06`. Long legs at 3.05 averaged 2.56–2.87 m/s while the party's displacement measure was up to 5.19. The two 5.49 legs averaged 4.70 and 4.75. 5.49 = 1.8 × 3.048 exactly. The log does not name a cause. |
| Installed-assembly source (Kingmaker 2.1.7b, decompiled to scratch only) | The native group pace and acceleration rules below, and the KMC routing that bypasses them. |
| Offline checks on this branch | Policy and contract behaviour (below). |
| In game | **Nothing.** This machine has no KMC install, no KMC Baseline/Working fixture and no `runtime-state\fixture-qualification.json`; the only fixtures present belong to other mods. Nothing was staged, launched or restored. |

**Installed build versus source.** The log shows 0.2.5 loading, from the owner's machine (`C:/Users/Howie`). It records no DLL hash or MVID, so the installed binary cannot be proven identical to the 0.2.5 release (`17591738…`, MVID `35939218…`, built from `e5f006f5`). Its patch-installation line and the 0.2.5 save-completion diagnostics are consistent with that release. Between `v0.2.5` and this base, only attack-warning code changed (`MountedStockAttack.cs`, `MountedCombatController.cs`) plus the version. The selection, routing and observer code read here is the code the owner ran.

## What the observation measures

`MountedExplorationObserver` samples every 0.25 s of `Time.unscaledTime` (pauses and time scale included) in real-time `Default` mode outside combat. A leg is the mount's exact player-made `UnitMoveTo`.

- `speedLimit`, `modifiedSpeed`, `currentSpeed` and `agentMaxSpeed` are read **once, at the leg's first sample**.
- `meanSpeed` is the mount's sampled path length divided by elapsed time.
- `partyMeanDisplacementSpeed` is the mean straight-line displacement of the other party members divided by the same elapsed time.

So the start snapshot could predate a later change of limit. The mean speeds over the whole leg rule that out for the final leg: 17.71 m in 6.39 s, never above the 3.05 limit.

## Cause (source inspection; not demonstrated in game)

1. **Native pace.** `ClickGroundHandler.MoveSelectedUnitsToPoint` (0x060093DB) gives every member of a multi-unit selection the same `SpeedLimit`: the slowest *selected* `ModifiedSpeedMps`, floored at 30 ft/round = 3.048 m/s. `RunCommand` (0x060093DC) creates each member's `UnitMoveTo` with that limit.
2. **Native acceleration (the "Accelerated Move" option).** In mouse play the only route is `UnitMovementAgent.OnPathComplete` (0x060018B9). For a *new* path it calls `UnitCommand.AccelerateIfPathIsLong` (0x060027B4).
   - If `CanAccelerate()` and the path is at least 12.7 m, the command accelerates: Charge gait, `SpeedLimit *= 1.8`.
   - Then `Accelerate()` is called on **every command of every unit in `SelectionManager.SelectedUnits`**.
   - `CanAccelerate` (0x060027B7) refuses in each of these cases: already accelerated, not player-made, not Normal gait, the setting is off, the executor is in combat, the executor's `SpeedIsAffected`, or the party is heavily loaded/overloaded with the encumbrance slowdown.
   - `UnitEntityData.CurrentSpeedMps` then yields `min(ModifiedSpeedMps × 1.8, SpeedLimit)`, which is 5.486 for a 3.048 group.
   - No other caller exists outside the gamepad continuous-move layer.
3. **KMC routing.** Two pieces of KMC routing combine:
   - In real time, `NormalizeMultiSelection`/`NormalizeSingleSelection` (`GameMountedRelationshipService.cs`) project the mount onto its rider, so the horse is never in `SelectedUnits`.
   - `GroundCommandPrefix` → `RouteGroundCommand` rewrites the rider's `RunCommand` onto the mount (`RewriteRiderToMount`).

   The horse therefore executes the rider's group order with the group limit, but the native acceleration offer reaches the rider's empty command slots and **skips the horse**.
4. **Why the horse sometimes ran.** Its own path completion can still accelerate it. However, `UnitMovementAgent.PathTo` clears `m_RequestedNewPath` when the approaching command re-requests the same destination before the asynchronous path returns, which `UnitActionController` does every tick. Own-path acceleration is therefore intermittent for any single unit. Native groups do not depend on it, because any one member's success accelerates everyone selected. This explains the two 5.49 legs with `modifiedSpeed` unchanged: acceleration changes the command's limit and gait, never the unit's modified speed. This step is source reasoning and is not instrumented in game.
5. **Not the cause.**
   - 3.05 is the native floor/slowest-30-ft limit, not a restriction on the horse.
   - `modifiedSpeed` stayed 4.06 (40 ft) in all 105 legs, including the two the native rule accelerated. Acceleration
     requires `SpeedIsAffected` to be false, and a halving rule (stealth, staggered, slowed, entangled, difficult
     terrain, weather) would have changed the modified speed. So no such restriction explains the constant lower cap.
     This is an inference from the log, not a per-leg reading of the conditions.
   - Leinna's fatigue and the animation/IK exceptions name other units and do not bear on the horse's limit.
   - BagOfTricks' party-speed multiplier cannot be excluded from the log alone. However, the exact 1.8 ratio on long paths only matches native acceleration.

## Change

- `Domain/MountedGroupPacePolicy.cs` (new):
  - `NativeGroupOfferRan`: the trigger command became accelerated in that call, which is exactly when the native offer ran.
  - `Decide`: offer the native acceleration only when all of these hold: the pair is mounted, real time (not turn-based), the rider is selected, the mount is not selected (a selected mount already received the offer), the mount's Move slot holds the **exact ground order routed from the rider** (`playerMove`, bound by `ObserveGroundOrderRun`), and that order is unfinished and not yet accelerated.
- `MountedPatchController`: exact-token prefix/postfix on `UnitCommand.AccelerateIfPathIsLong` (0x060027B4). The prefix records `IsAccelerated`; the postfix visits each pair. Exceptions are logged, never thrown into the native path callback.
- `MountedCombatController.GroupPace.cs` (new): calls native `Accelerate()` on that one command.
  - It writes no speed, limit, gait, agent field or stat; native `CanAccelerate` keeps every legitimate refusal.
  - It writes one log line per routed order: carried, or refused, with the limit and gait before and after, the mount's modified and current speed, `SpeedIsAffected`, combat, the setting and the encumbrance.
- `MountedExplorationObserver`: the per-order summary line additionally reports:
  - the selection the order came from;
  - whether the mount's order was accelerated at any sample, its highest limit, and its limit and gait at the end;
  - the highest agent maximum speed;
  - how many ordinary party members held a player order, how many of those were accelerated, and their highest limit.

  This is still one line per order, with no per-frame output.

**Preserved:**
- Attack, door, trap, spell, recovery and charge moves are separate owned moves and are not carried. Charge also keeps its own `MaxSpeedOverride` lease.
- Turn-based movement and its budgets are excluded twice: by the policy and by the native in-combat refusal.
- Completion, interruption, replacement and arrival stay with the unchanged native command.
- The mount remains the only mover, and the rider gains no command.
- The locomotion gait follows the command's native Charge gait (`MountedHorseGaitPolicy` only acts on Normal-gait orders), so animation matches movement.
- No state is saved. The accelerated flag lives on the transient command, as natively.

**Not changed (observations for the owner):**
- The carried `UnitAreaTransition` of the entrance repair has the same missing offer when walking to a map exit.
- The gamepad stick-acceleration layer (`InGameInputLayer`) also walks `SelectedUnits`.
- Because the horse is not selected, the native group limit is computed from the rider's speed, not the horse's. A horse slower than 30 ft is still bounded by its own speed, but the party no longer waits for it.

## Checks (budget `horse-group-pace`)

| Check | Result |
| --- | --- |
| `Test-Tiers.ps1 -Tier Fast` | PASS 3/0: net47 build, components 1175/0 (7 new), Validate-Source 151/0; 0.53 min |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 765 PASS / 3 FAIL. All 8 new group-pace pins PASS. The 3 failures are the installed UnityModManager SHA-256/MVID/version identity checks and reproduce identically on the unchanged base (757/3). |
| Mutation: `Decide` never offers | 3 horse-pace regressions FAIL (1172/3); source restored |

The regression model encodes the decompiled native rules as a test fixture. It is a reading of native code, not native evidence.

## Acceptance (native)

| Row | Status |
| --- | --- |
| A. Long party move, before/after pace | NOT RUN (no KMC install/fixture) |
| B. Short/long, cancel/replace, whole-party/rider-only/horse-only selection | NOT RUN |
| C. Legitimate slower movement (slowed/entangled, setting off, heavy load) | NOT RUN; offline model only |
| D. Turn-based enter/leave, movement budgets, charge | NOT RUN; the change excludes turn-based play and owned/charge moves by construction |
| E. Save/reload or area transition | NOT RUN; no retained state was added |
| F. Exact candidate | Fast build of the branch head; no package, no native binary tested |

**To verify:** owner play with the branch DLL, or a qualified KMC fixture.
- Order a whole-party move longer than 13 m with Accelerated Move on. The new "Mounted group pace" line and the summary's `mountAccelerated`/`partyAccelerated` fields should agree, and `meanSpeed` should track `partyMeanDisplacementSpeed`.
- Repeat with the horse slowed, with Accelerated Move off, and on a short move, where the horse should keep the group walk.
- Then fight one turn-based round and move again.
