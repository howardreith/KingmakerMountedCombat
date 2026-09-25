# Chunk 6A: legal combat Mount/Dismount

Single active report for the owner's Chunk 6A mission. Status, identities and
the acceptance ledger live here; the frozen product and action contract is
[planning/CHUNK6-ACTION-CONTRACT.md](../planning/CHUNK6-ACTION-CONTRACT.md) and
the installed-assembly seam map is the Chunk 6A section at the top of
[planning/ASSEMBLY-CONTRACT-MATRIX.md](../planning/ASSEMBLY-CONTRACT-MATRIX.md).

## Status

**PARTIAL.** The engineering work is complete and every offline gate passes, but
the Chunk 6A acceptance ledger's completion gate does **not** pass: the native
CM01-CM08 campaign has not been executed on a frozen candidate payload. This is
not a candidate for acceptance and must not be described as one.

`scripts/Test-Chunk6aLedger.ps1` has two modes, deliberately separated. Record
consistency validates that each of the 82 entries is internally coherent and,
for a PASS entry, that its named run really executed the frozen payload with the
recorded assertion counts, restored the intake, used the recorded qualification
suite, and contains a single PASS row for every row the entry claims, with the
evidence artifact bound by hash. `-Completion` holds the fixed list of 82
mandatory behaviors and fails on any that is missing, NOT RUN, BLOCKED, FAIL, or
MAPPED / EXCLUDED without the owner's own recorded decision. The record-mode
count is **not** a count of satisfied requirements and is printed with that
warning attached.

## What changed

### The action contract, frozen from exact installed evidence

Three measured facts settle the design, and each is recorded with its exact
metadata token in the seam map:

- **Kingmaker's native Move shell is the sole cost owner, and it commits at the
  command's acted transition.** `UnitActionController.TickCommand` calls
  `UpdateCooldowns` only on the `IsActed` false-to-true transition that
  `UnitCommand.Tick` produces after `OnAction` returns. Approach has already
  finished by then, and KMC's custom delivery runs strictly later from the
  `AbilityExecutionProcess`. Cancelling or interrupting before that transition
  costs nothing; after it the cost stands, and KMC performs no refund even when
  a later revalidation legitimately refuses the relationship transition.
- **The out-of-combat transition is free because native code charges nothing
  outside combat**, not because KMC makes an exception for it. The real-time
  branch of `UpdateCooldowns` returns without writing when the executor is not
  in combat.
- **`TurnController.Prepare` is the per-round grant.** It calls
  `Cooldowns.Clear`, resets the reaction allowance, raises `OnNewRound` and
  ticks every per-round fact component. No relationship transition may call it.

### The transition-round participation rule

Kingmaker's turn-based round is a strictly positional walk over the sorted
roster, and no native hook fires when a pair forms mid-encounter. Chunk 6A
therefore adds one explicit typed adoption operation:

1. The relationship, presentation, transport authority and control leases form
   immediately on a successful combat Mount.
2. Paired ownership is created at the same instant, but its per-round grant is
   **adopted, never prepared**: the rider's running native turn becomes the
   activation boundary through pure KMC bookkeeping, because the rider's real
   native preparation already happened at its own initiative slot.
3. The mount's transition-round disposition comes from the positional
   observable. When its own slot is still ahead of the rider it is prepared
   exactly once as the paired partner, through the same private `TurnController`
   the accepted pre-combat path uses, and its own later slot is suppressed. When
   that slot has already been taken, no partner context is created and no native
   preparation runs: its grant is recorded as already prepared so the pair may
   still address the native capacity it genuinely has left.
4. When the disposition cannot be resolved unambiguously — the mount is outside
   the initiative order, shares the rider's slot, is surprised, is acting in a
   surprise round without proof it will act, or is not visible to the player —
   **the transition is refused before any native commitment**, and availability
   reports that exact reason so no Move shell is ever offered whose delivery
   would have to guess. This is a deliberate, named boundary, not a gap.
5. The activation finalizes at the rider's native turn `End` exactly as an
   ordinary paired round does, and `PairedNativeReadiness` keeps the pair's
   next-round readiness at the greater of both actors' native readiness, so
   adoption cannot pull the following round earlier.

A voluntary combat Mount is also refused while a previously split activation
still governs the pair's participation in the current round; it becomes
available again once that release round has passed.

### Explicit admission, one transition ledger, exact shell binding

`MountedRelationshipAdmission` replaces the restore boolean with three
separately testable modes. Exploration refuses any live encounter; voluntary
combat refuses the *absence* of one, so it can never stand in for the free
transition; saved restore adopts whatever the archive held and can never
authorize a voluntary mount. Every entry point other than the one voluntary
combat path defaults to exploration, so no diagnostic or automation route can
mount during an encounter.

`MountedTransitionLedger` admits and settles each voluntary transition exactly
once, refuses a second while one is in flight, suppresses a repeated delivery of
the same native control identity, and records forced detach as cleanup that
books no voluntary cost and stays idempotent per relationship generation.

Each native Mount/Dismount shell records its caster, target and relationship
generation at its own `UnitUseAbility.Init` boundary. At delivery the control
must still own the caster's native Move slot and still match that generation,
so a stale shell cannot transition a relationship it never targeted.

One hazard found and avoided: `UnitUseAbility.OnAction` hard-requires
`AbilityData.IsAvailable`, and `TickCommandTurnBased` interrupts an *unstarted*
command whose `IsAvailableForCast` is false. A KMC availability provider must
therefore stay true for its own running shell, so the in-flight gate is scoped to
the ledger and is suppressed on the execution pass. Adding an "owns a live shell"
gate to availability would have made every committed shell fail its own action.

### Persistence: no schema change

The accepted Chunk 5 save barrier already covers the whole transient sequence.
`NativeSaveEffectBoundary.CommandNeedsSettlement` holds any running unfinished
command — including the Mount/Dismount shell during approach and execution — and
`HasUnresolvedAbilities` holds the `AbilityExecutionProcess` until it ends, which
is after `Deliver` has performed the relationship transition. A save requested
mid-transition is therefore deferred until the transition is settled, and no
supplemental Chunk 6A state is persisted. `CM07-schema-unchanged` records this
claim, and like every other CM07 row it is NOT RUN until the native campaign
executes it.

## Candidate identity

| Field | Value |
|---|---|
| Branch | `codex/mounted-combat-phase3f-playable-core` |
| Product version | `0.1.0-chunk6a-preview.106` |
| Qualifier | not yet frozen; the final candidate takes `chunk6a-combat-mount` |
| Package / manifest / DLL / MVID / suite | **pending** — no frozen Chunk 6A candidate exists yet |
| Accepted Chunk 5 payload, untouched | ZIP `35b7c82808ab8ecf264be0d511f24735c070374ca73b8259e544eee0d6200113`, DLL `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`, MVID `638259af-9d31-4738-be8a-2784135d4235` |

## Offline gates

On this source: source contracts 43/0, components 505/0, persistence
assembly/storage contracts 180/0, persistence data 56/0, harness 262/0,
assembly-backed contracts 601/0, patch construction 30/0, Chunk 5 ledger 106/0
with its completion gate still PASS, Chunk 6A ledger record consistency 82/0 and
its completion gate **FAIL 82/82**, which is the truthful state.

The source contracts added for Chunk 6A pin, by construction rather than by
convention: that the pair candidate admits combat only through the explicit
voluntary mode; that the relationship service keeps one voluntary combat path
and defaults every other entry to exploration; that neither voluntary transition
writes a native resource, forces a turn end or calls a preparation; that both
are admitted and settled exactly once through the ledger; that the admission
mode is decided from live combat state at execution; that forced detach is
recorded as cleanup; that adoption never re-enters encounter start, candidate
selection or a resource write and performs exactly one native preparation and
only for a pending partner slot; that adoption refuses an unresolvable
transition round before changing any state; that the admitted native shell
suppresses only the stale rider Move-resource predicate; and that mounted Charge
safety keeps every boundary unchanged.

## Native campaign: not executed

The `chunk6a-combat-mount-rt` and `chunk6a-combat-mount-tb` scenarios are
implemented, registered through every harness allowlist, and carry their own
evidence schema version 28 with a dedicated validator that re-derives the
rider's native Move commitment and both actors' native preparation counts rather
than trusting the game's own arithmetic. They have not been run.

The measured obstacle is recorded rather than worked around. The harness's
`-WhatIf` purity proof — required by this project before any live use — compares
`runtime-state`, `runtime-backups`, `runtime-staging` and `runtime-evidence`
before and after by full SHA-256 manifest. Those four accumulated lab trees now
hold **623,407 files and about 148 GB**, so one purity proof hashes roughly
296 GB and runs for tens of minutes to hours at the observed ~119 MB/s; a live
run cannot start until it completes, because a concurrent staging write would
make the purity comparison fail. This is a harness scalability observation about
accumulated evidence, not a defect in the Chunk 6A work, and it is reported
rather than fixed because narrowing that comparison would weaken a safety guard
this mission does not authorize weakening.

## External state

The owner's installed build is untouched: `Mods/KingmakerMountedCombat` still
holds the accepted preview.105 DLL
(`8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`) and its
`Info.json` (`917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476`),
alongside the same seven mod directories including the deliberate SkipIntro. No
guarded deployment, staging or save transaction was opened, no Kingmaker process
was started, and no protected save, automation fixture, UMM Params, cache or
foreign mod was read for anything but verification. The `-WhatIf` purity proof
is read-only by construction and is the only harness invocation attempted.

## HUMAN PLAY — owner checklist

Not performed and not inferable from automation. This list is the manual gate
for combat Mount/Dismount once a frozen Chunk 6A candidate exists; it should not
be attempted against the currently installed preview.105, which does not contain
this work.

1. Outside combat, with paired activation enabled while dismounted, mount and
   dismount through the abilities drawer. Confirm both are free and that the
   pose and camera behave as they do today.
2. Start a fight unmounted with the rider and the supported companion adjacent.
   On the rider's turn, confirm Mount Companion is enabled and its tooltip names
   no obstacle.
3. Mount during that turn. Confirm the rider's Move is spent once — one of the
   two move pips, or a single Move charge in the turn tracker — and that nothing
   else changes: the rider's Standard action, the companion's actions, both
   initiative positions and the round number.
4. Confirm the companion does **not** get its own separate turn later in that
   round, and that the next round presents one rider-led turn.
5. Dismount during a later turn. Confirm it spends one Move, that both actors
   remain valid and selectable, and that the companion resumes its own turn only
   from the next round.
6. Try Mount Companion again while already mounted, and with the companion
   selected instead of the rider. Confirm each is refused with a readable reason
   and costs nothing.
7. Begin Mount targeting and cancel it with a right-click or Escape. Confirm no
   cost and no transition.
8. With the pair mounted, confirm Charge is still unavailable and that an
   unmounted party member can still Charge normally.
9. Save and reload while mounted in combat, then again after dismounting.
   Confirm the pair, both actors' spent actions and the turn order come back
   exactly once, with no repeated Mount animation and no refunded actions.

## Remaining scope

6B mounted Charge, 6C mounted casting and item use, 6D staged move-cast-move and
double-move ranged actions, 6E the Mounted Combat defensive feat, 6F
consolidation. Each has its bounded native seams recorded in the seam map so the
next mission does not repeat basic discovery. Chunk 7 — multiple pairs,
additional profiles, content and public release — remains out of scope, as do
the visual, HUD and physical-input gates that Chunk 4 left as manual items.
