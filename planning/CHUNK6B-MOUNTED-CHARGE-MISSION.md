# Chunk 6B — Mounted Charge mission (opened 2026-10-03)

Status: `OPENED — MEASUREMENT FIRST`. Nothing is implemented, enabled or claimed by this document. It
opens the bounded 6B mission the owner decision of 2026-10-02 (section F) names, on the integration branch
from the stabilized Chunk 6A head (exit record: `CHUNK 6A IMPLEMENTATION STABLE / FINAL QUALIFICATION
DEFERRED TO CHUNK 6 CONSOLIDATION`, docs commit b655a501). Main stays the accepted Chunk 5 delivery; the 87
Chunk 6A rows are not rerun per 6B candidate; final acceptance waits for the Chunk 6 consolidation.

## Governing contracts (unchanged by this document)

- `planning/CHUNK6-ACTION-CONTRACT.md` section 3: the native surface is `AbilityCustomCharge` (type
  `0x02000598`; `CanTarget` `0x06002BBD`, `Deliver` `0x06002BB6`, `RuntimeRoutine` `0x06002BB8`,
  `TurnBasesRoutine` `0x06002BB7`, `IsEngageUnit` `0x06002BB5`, `GetMinRangeMeters` `0x06002BBA`/`0x06002BBB`,
  `GetMaxRangeMeters` `0x06002BBC`, `Cleanup` `0x06002BB9`); Charge owns a forced path plus a queued native
  `UnitAttack` and stamps `RuleAttackWithWeapon.IsCharge` (`0x06007186`). The cost owner is the enclosing
  Standard/full-round ability shell. The 6A obligation holds until a pair charge is qualified: Charge is
  rejected at availability, targeting, click, admission and execution while mounted, before any movement or
  resource expenditure (`MountedChargeSafetyPolicy`/`MountedChargeSafetyService`), and ordinary unmounted
  Charge passes through untouched.
- `planning/TARGETING-REACH-CHARGE-CONTRACT.md`, "Basic mounted charge — deferred with evidence" and the
  stretch disposition gate: the stock primitive cannot separate mover from attacker; a basic charge is
  permitted only if the already-qualified pair transaction can add a straight/path-valid charge flag and the
  rider cost without a broad patch; otherwise the disposition is `DEFER — EVIDENCED` and the feature stays
  absent and default-off.
- The native engine owns every cost: a mounted charge may cost a resource only by being routed through the
  native command that already charges it; prediction and availability stay side-effect free; a transition
  that cannot be resolved lawfully is refused with an exact reason. Production pathing, adjacency and
  collision rules are never weakened.

## Problem statement (from section 3)

The mount must own the forced path and the queued attack while the rider owns the Standard action, and
`IsEngageUnit` must not strand the pair mid-path. The qualified pair attack transaction
(`MountedPairAttackCommand`: the rider-owned Standard attack delivered from the mount's position after a
mount-owned delegated approach, with exact admission, repath, cost, terminal and restoration evidence) is the
only candidate carrier. A charge differs from that transaction in four measurable ways: the approach must be
a straight, clear, path-valid line of at least the minimum and at most the maximum charge distance; the mover
runs at charge speed under the charge state; the single attack carries the charge rule flag and the native
charge modifiers; and the cost is the charge ability's own (full-round in turn-based mode, one native shell).

## Measurement plan (the first increment; no product change)

Every item is answered from the pinned installed assembly (SHA `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`,
MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`) by read-only reflection and IL reading, and from stock
unmounted charge observed natively through the existing Chunk 4 charge-safety scenario rows
(`C4-CHARGE-unmounted-rider`: distance, charging state, maximum rider Standard and Move, `UnitAttack.IsCharge`).

1. Consumers of the charge flag: which rulebook events read `RuleAttackWithWeapon.IsCharge` /
   `UnitAttack.IsCharge` (attack bonus, armor class penalty, the stock charge buff and its duration) and
   whether the flag is honoured on a `UnitAttack` that the pair transaction creates for the rider (the rider
   is the attack initiator; the mount is the mover).
2. The charge cost in both modes: what the enclosing ability shell charges (Standard, full-round, Move) at
   `Deliver` and in `TurnBasesRoutine`, and what the native turn controller requires of the mover's movement
   budget during the forced path.
3. `CanTarget`: the exact admission (caster-origin distance against `GetMinRangeMeters`/`GetMaxRangeMeters`,
   `ObstacleAnalyzer.TraceAlongNavmesh`, surrounding-unit clearance, current-turn movement) and which actor
   each check reads, so the pair variant can evaluate the same checks from the mount's origin and footprint
   without a broad patch.
4. `IsEngageUnit` and the mid-path interruption semantics: what the stock routine does when the target moves,
   dies or becomes unreachable, and how the forced path is released, so the pair variant can never strand the
   pair (no half-charged state, no stranded forced path, no orphaned queued attack).
5. The stock charge state on the mover (`UnitDescriptor.State.IsCharging`, `AgentASP.IsCharging`, speed
   change) and its `Cleanup`: whether the state is applied to the caster only and whether a pair-owned
   transaction can apply the equivalent state to the mount through native API without a patch.

Outcome of the measurement: either a bounded design that extends the qualified pair transaction with a
`MountedChargeTransaction` (straight path validity from the mount's origin, charge speed on the mount, the
charge flag on the rider's single attack, the rider's native full-round cost through the ability shell, exact
refusal reasons, interruption and cleanup through the existing terminal paths) and a focused development
qualification (RT and TB positive cases, min/max range refusals, obstruction, target loss mid-path, cancellation,
duplicate request, save/load across a pending charge, exact restoration), or the recorded disposition
`DEFER — EVIDENCED` with the exact measured obstacle. In both outcomes the 6A safety boundary stays in force
until a pair charge is qualified by the external reader.

## Development qualification shape (focused; not the 87 6A rows)

- FAST while implementing (component tests, source pins, the charge reader's synthetic acceptance and refusal),
  CANDIDATE once before a native freeze, FULL only at the Chunk 6 consolidation.
- One external PowerShell reader is the acceptance authority for the charge rows; the compiled scenario records
  facts and checks structure only.
- Each native candidate reruns only the charge rows it affects plus the Chunk 4 charge-safety regression
  (`chunk4-charge-safety-rt`/`-tb`), with fresh isolated restored transactions and session logs preserved.
- Historical failures are retained immutably; a commit, candidate, proof or targeted PASS is never
  authorization to merge main or publish a release.
