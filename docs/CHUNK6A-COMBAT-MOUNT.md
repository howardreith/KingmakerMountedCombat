# Chunk 6A: legal combat Mount/Dismount

Single active report for the owner's Chunk 6A mission. Status, identities and
the acceptance ledger live here; the frozen product and action contract is
[planning/CHUNK6-ACTION-CONTRACT.md](../planning/CHUNK6-ACTION-CONTRACT.md) and
the installed-assembly seam map is the Chunk 6A section at the top of
[planning/ASSEMBLY-CONTRACT-MATRIX.md](../planning/ASSEMBLY-CONTRACT-MATRIX.md).

## Status

**IMPLEMENTATION CANDIDATE — NATIVE QUALIFICATION BLOCKED. PARTIAL overall.** The
acceptance ledger's completion gate does **not** pass. The mandatory list is **87
behaviours**; the truthful count is **0 PASS, 1 FAIL, 86 BLOCKED**, so completion
fails **87/87**. This is not a candidate for acceptance, must not be described as
one, and no part of it may be called finished engineering while the completion
ledger stands at 0 of 87.

What changed since the reviewed candidate is that the campaign actually ran. The
starting-payload registration the previous run reported as its blocker is done, the
qualification suite exists, and `chunk6a-combat-mount-rt` reached gameplay and
returned **43 assertion passes against one failure**. That one failure is in the
out-of-combat Mount preamble every tranche scenario performs. It is recorded as
`CM01-exploration-free` **FAIL** — not BLOCKED — bound to run `c6a-mount-rt-1`, its
exact failing row and assertion, and the campaign-c payload it was observed on.
`chunk6a-combat-mount-tb` was never attempted.

Its root is **no longer unexplained**. Read-only inspection of the installed
assembly established it: `UnitUseAbility.OnAction` (`0x06002737`) returns a
terminal result unless the execution process engages a unit, so the command
completes and leaves the native Move slot while its `AbilityExecutionProcess`
delivers on later frames — which is why rediscovering the shell from that slot at
`Deliver` could not work. See *Deliver ownership* below. **A deterministic blocker
is established from installed IL and repaired in the implementation candidate; the
repair passes offline gates and awaits exact native proof.** It is not natively
proven, and the historical FAIL is retained immutably rather than replaced by the
repair.

Two things block completion, and they are different in kind:

1. **The repaired preamble defect above, still unproven natively.** No CM case can
   be demonstrated until a new candidate actually runs, because every tranche
   scenario depends on that preamble.
2. **A host resource limit.** The diagnostic candidate built to identify that
   defect's exact root has not been launched, because its required pre-launch
   `-WhatIf` purity proof was killed three times by the host under system-wide low
   memory, with an external Steam process holding roughly 12.5 GB of 32.5 GB. The
   proof was not narrowed or skipped and the candidate was not launched without it.

### The review remediation: six repairs

An owner review of the published preview.107 candidate found six defects. All six
are repaired on this candidate; none was argued away and none of the earlier
retained failures was removed.

**R1 — a spent partner slot is closed, not reopened.** The retain disposition
recorded a partner whose native initiative slot had already passed as `Granted`
and `Prepared` but **not** `Ended`, and the tests affirmed that the pair could
still address it on the rider's adopted boundary. That is a second action
opportunity for an actor that already took its slot, and it applied equally to a
fully spent, partially spent, surprise-skipped or visibility-skipped companion.
The disposition now records that slot as granted, prepared **and ended**. `Ended`
closes `CanAddress`, and with it partner selection, paired native command
admission and paired movement, while `OwnsRoundEffects` stays true so native
timers due at that boundary still belong to it. The observed Standard, Move and
Swift debt is recorded and never lowered, no private partner `TurnController` is
created, no preparation, refresh or callback replay runs, and eligibility returns
only through Kingmaker's next lawful allocation. The tests that asserted
addressability are replaced rather than loosened, with new behavioral cases for a
fully spent mount, a partially spent mount, residual cooldown debt, nothing
spent, surprise-skipped and visibility-skipped slots in both roster positions,
and recovery at the next allocation.

**R2 — attachment and adoption are one transaction.** The relationship used to
commit and increment its generation, after which `MountedPairActivated` attempted
adoption and merely *logged* a refusal, so a successful native Mount could leave a
mounted relationship with no valid paired activation.
`MidEncounterAdoptionPlan` is now a typed, immutable, generation-bound record of
the exact live encounter: session, round, current turn, roster identities and
indices, initiative disposition, surprise and visibility, liveness, ability to act
and consciousness. `MountRiderOn` plans it before committing, revalidates it field
by field immediately before the commit, and only then commits the adoption,
rebound to the one generation a committed relationship produces. A refusal after
attachment performs exact compensating cleanup — activation, private partner
context, preparation reservation, armed pair and renewal floor removed, the
relationship returned to `Unmounted`, and the transition reported as **failed**
rather than Mounted.

The compensation writes no native resource, calls no preparation or turn end, and
refunds nothing, so a Move that Kingmaker already committed for the approach
stays spent. The generation stays **advanced**, which is what retires every native
shell bound to the old relationship so the failed control cannot be delivered
again. Every refusal path inside the commit precedes the single
`partnerContext.Prepare()`, because that call clears the partner's cooldowns and
cannot be undone; a rollback therefore never has to un-prepare an actor, and a
source contract pins that ordering. The activation announcement no longer attempts
adoption at all: it is the consistency check for the transaction's invariant.

**R3 — turn-based delivery requires an *acting* rider turn.** `Preparing` was
accepted on the assumption that nothing is skipped in that window, and there is no
exact native evidence for it: `TurnController.Prepare` runs at the actor's own
slot and only then advances to Acting, and a player command is delivered by
`UnitCommands.Tick` inside Acting. Admitting a transition while the turn is still
Preparing would settle the transition ledger and the adopted paired grant against
a turn whose native preparation has not finished, so Preparing is now refused —
with its own reason, not the generic wrong-turn one.

**R4 — a Dismount delivery revalidates its target identity.**
`DismountTargetIdentityPolicy` decides it, free of engine types so each rejected
condition is directly tested: a missing target, a foreign target, a target that
changed after the command was created, a missing caster identity, a relationship
whose rider changed, and a stale generation. The most specific obstacle is always
the one reported.

**R5 — the save barrier queries the transition state.** The action contract
claimed the ledger deferred an unsettled transition, but `SaveEffectsReady` never
consulted it. It now defers on `HasUnsettledRelationshipTransition`, the ledger's
own admit-to-settle window, and on `OwnsUnsettledRelationshipShell`, a registered
Mount/Dismount shell that has not finished. The second term is strictly more than
`CommandNeedsSettlement` covers, because that predicate requires the command to be
*running* and would let a queued but unstarted relationship shell through. Nothing
is claimed for CM07 from this: all eleven CM07 rows remain mandatory native work.

**R6 — durable status language.** The action contract's legend claimed
`IMPLEMENTED` meaning built **and qualified**. Combat Mount and combat Dismount are
relabelled `IMPLEMENTATION CANDIDATE — NATIVE QUALIFICATION BLOCKED`, and
`IMPLEMENTED` is reserved and claimed by no row. This report and the three active
documents no longer describe the work as complete.

The mandatory list grows from 82 to **85** ids: `CM01-combat-mount-preparing-refused`
for R3's boundary, and `CM02-adoption-plan-invalidated` plus
`CM02-adoption-compensation-releases` for R2's transaction. Nothing was removed or
relabelled.

R2's regression needs a late invalidation that cannot be produced from outside
without writing game state, because the adoption disposition changes only at a turn
boundary and a turn boundary cancels the approach. It is therefore driven by a
**bounded diagnostic adoption fault**, which makes the commit refuse at its first
check exactly as a real late invalidation does. The fault arms only on an idle
unmounted pair with the paired lifecycle enabled, only one at a time, bound to two
exact distinct actor identities; it is consumed once, disarmed on use and on every
cleanup path, and writes nothing. Three source contracts pin those properties and
pin that it is armed exactly once, by this scenario only.

### The starting-payload registration, now authorized

The previous run reported this as its one blocker and declined to change it. The
owner has now explicitly authorized the exact addition, so it is made:

```
Info.json                   917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476
KingmakerMountedCombat.dll  8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2
```

Its expected entry count is **two**, which is a fact about the tree rather than a
loosened bound: the guarded deployment replaced the DLL and the game has not been
run since, so there is no UMM loader cache, and the count is expressed the way every
other payload expresses it — as the size of its own pin set. Provenance is the
guarded deployment receipt
`runtime-state/deployment-operations/20260925T0200587550503Z-94de251601b24a04a1f5394f57a92f64.json`.

Nothing is widened. Strict byte equality is preserved for every pinned file, and
there is no wildcard, no alternate entry count, no "latest" or newest-wins logic, no
fallback or partial match, and no authority to restore any older or newer intake. A
tree matching no pin set still fails closed. The absence of `Info.json` is now its
own refusal rather than a hashing error, which narrows nothing — every pin set is
keyed on that file, so a tree without it could never have matched.

The negative tests are retained and **extended from 4 assertions to 13**: mutated
bytes per leaf, an extra unregistered file, a missing registered file, a renamed
registered file with the right bytes and the right count, a mixed payload whose DLL
bytes are foreign to its `Info.json`, and a subdirectory. Each is proven rejected,
and the exact live installation is proven accepted before and after, read only.

`scripts/Test-Chunk6aLedger.ps1` has two modes, deliberately separated. Record
consistency validates that each of the 87 entries is internally coherent and,
for a PASS entry, that its named run really executed the frozen payload with the
recorded assertion counts, restored the intake, used the recorded qualification
suite, and contains a single PASS row for every row the entry claims, with the
evidence artifact bound by hash. `-Completion` holds the fixed list of 87
mandatory behaviors and fails on any that is missing, NOT RUN, BLOCKED, FAIL, or
MAPPED / EXCLUDED without the owner's own recorded decision. The record-mode
count is **not** a count of satisfied requirements and is printed with that
warning attached.

Record consistency also holds the `retainedFailures` collection, and it holds it
the way the mandatory list is held: the required retained records are fixed in the
validator itself, so a failure that really happened cannot be dropped by editing
the ledger. Each retained record is bound exactly as strictly as a live FAIL —
its own run, scenario, observed payload, evidence artifact by hash, exact failing
row, that row's own assertion text, and a reason naming the behaviour — and every
behaviour that is currently FAIL must already be retained, so the record exists
before any later candidate can flip the live entry to PASS. When that flip
happens, the live entry moves to PASS on the new frozen payload and the retained
record stays byte-for-byte as it is: a PASS on a newer payload never unmakes a
FAIL on an older one.

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
   preparation runs, and its allocation for this round is recorded as granted,
   prepared **and ended**: the slot really did happen, so it is not re-offered.
   An ended partner cannot be addressed, selected as the pair's acting partner or
   admitted for a paired native command on the rider's boundary, while the native
   timers due at that boundary still belong to it. Its observed native Standard,
   Move and Swift debt is recorded and never lowered, nothing is refreshed or
   replayed, and it becomes eligible again only at Kingmaker's next lawful
   allocation.
4. When the disposition cannot be resolved unambiguously — the mount is outside
   the initiative order, shares the rider's slot, is surprised, is acting in a
   surprise round without proof it will act, or is not visible to the player —
   **the transition is refused**, and availability reports that exact reason so
   no Move shell is offered whose delivery would have to guess. Refusal at
   *prediction* precedes native commitment and therefore costs nothing; refusal
   at *delivery* does not, and the committed Move stands. This is a deliberate,
   named boundary, not a gap. The next section states which gate runs when.
5. The activation finalizes at the rider's native turn `End` exactly as an
   ordinary paired round does, and `PairedNativeReadiness` keeps the pair's
   next-round readiness at the greater of both actors' native readiness, so
   adoption cannot pull the following round earlier.

A voluntary combat Mount is also refused while a previously split activation
still governs the pair's participation in the current round; it becomes
available again once that release round has passed.

### Authority timing: which gate runs when, and what each one can prevent

A combat Mount passes two different gates at two different times, and they are
not interchangeable. Saying so precisely matters because only one of them runs
early enough to prevent a cost.

`MountedAuthorityPolicy.IsQualifiedForCombatMount` is the qualified-authority
decision: paired activation enabled, and **neither** retired mounted-turn
authority live. It is asked twice.

1. **At prediction, before Kingmaker creates anything.**
   `MountedPlayerActionController` fills `CombatMountAuthorityQualified` and
   `CombatMountAuthorityReason` from the live settings, and the evaluator refuses
   an in-combat Mount whose authority is unqualified, naming the exact obstacle.
   This runs while the player is looking at the ability, before any command
   exists. The consequence is exact: **no native command is created, no Move is
   charged, no relationship state changes, and no shell is registered.** An
   unqualified authority observed here costs the player nothing.

2. **At execution, inside `MountRiderOn`, after Kingmaker has already
   committed.** By the time delivery reaches the relationship service the native
   Move command has passed its own `IsActed` transition and `UpdateCooldowns`
   has run, so the Move is spent. The authority is revalidated from live
   settings anyway, because the player may have changed a setting between
   prediction and delivery, and a refusal here returns a failed
   `TransitionResult` before `coordinator.Mount`. The consequence is equally
   exact and deliberately different: **the committed Move stands, the
   relationship does not form, the shell becomes terminal, and KMC refunds
   nothing.**

This second gate is therefore *not* a protection against paying for a refused
transition, and it is not described as one anywhere. It protects the
*relationship* from forming under an authority that cannot govern it. Kingmaker's
native Move shell is the sole cost owner; KMC observes that cost and never
rewrites it, so the honest statement is that a late refusal is a spent Move with
no mount, not a free retry. The same asymmetry governs the adoption disposition
and the turn-eligibility gate: unresolvable at prediction is free, unresolvable
at delivery is paid.

Two behavioral regressions hold this contract. `combat Mount requires the
qualified paired authority` proves the prediction gate refuses each unqualified
combination and names its obstacle, and `an authority withdrawn after admission
keeps the committed cost and forms no relationship` proves the execution gate
refuses while leaving the ledger's committed record and the relationship
generation untouched.

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
generation at its own `UnitUseAbility.Init` boundary. At delivery the shell must
still be reachable through its own **execution-context** binding and must still
match that generation, so a stale shell cannot transition a relationship it never
targeted. Ownership is no longer decided by asking whether the control still
occupies the caster's native Move slot: the installed IL proves that it usually
does not by then, and the repair is described under *Deliver ownership* below.

One hazard found and avoided: `UnitUseAbility.OnAction` hard-requires
`AbilityData.IsAvailable`, and `TickCommandTurnBased` interrupts an *unstarted*
command whose `IsAvailableForCast` is false. A KMC availability provider must
therefore stay true for its own running shell, so the in-flight gate is scoped to
the ledger and is suppressed on the execution pass. Adding an "owns a live shell"
gate to availability would have made every committed shell fail its own action.

### Deliver ownership: proven from installed IL, repaired with an exact binding

The first candidate resolved a delivering shell by looking in the caster's native
Move slot at `Deliver` time. Read-only inspection of the installed
`Assembly-CSharp.dll` (SHA-256 `3b6450ff…5afb`, MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`) shows why that cannot work:

- `UnitCommands.Run` (`0x060026B3`) stores `m_Commands[cmd.Type] = cmd`
  unconditionally, and `GetCommand(CommandType)` (`0x060026A9`) is a plain
  `m_Commands[(int)type]` read. The slot is a single cell, not a history.
- `UnitUseAbility.OnAction` (`0x06002737`) sets `ExecutionProcess` from
  `RuleCastSpell.ExecutionProcess` at instructions [166–167] and then ends with
  `get_ExecutionProcess` → `get_IsEngageUnit` → `brtrue` → `ldc.i4.0/ret`, else
  `ldc.i4.3/ret`. For a relationship ability that engages no unit the return is
  **terminal**.

So the command completes and leaves the Move slot while its
`AbilityExecutionProcess` goes on delivering on later frames. Move-slot
rediscovery at `Deliver` is therefore unsound in general, and the first candidate
had no reliable owner to consume.

The repair binds the shell to the one thing that survives: the command's own
`AbilityExecutionContext`. A postfix on that exact `OnAction` boundary records
`command.ExecutionProcess?.Context` against the shell in a
`ConditionalWeakTable`, and `NativeMountedAbilityLogic.Deliver` passes its own
context into dispatch. `NativeShellBindingPolicy` then decides ownership as one
pure typed decision, in a fixed order:

1. A **poisoned** execution is permanently refused, before the Move slot is even
   consulted, so an unresolvable conflict cannot be salvaged by whatever occupies
   that slot afterwards.
2. The **execution-context** binding is authoritative whenever it exists.
3. The **Move slot** is admitted only when that slot command's own execution
   process created *this very context* — exactly the case where delivery happened
   synchronously inside `OnAction` before the postfix could bind. It is never a
   recent-shell, last-shell, caster-only or generation-only lookup.
4. If both bindings exist they must name the same shell. **Disagreement is an
   explicit refusal** that poisons the execution and retires both shells, never a
   preference for either.
5. Anything else is an explicit refusal.

Past that decision the shell is still checked for retirement, exactly-once
consumption, kind, caster, target, Dismount target identity and relationship
generation, and every permanent refusal retires it so a rejected shell can never
become valid again. `TryDispatch` sets the terminal state in a `finally`, so one
shell makes exactly one delivery attempt whether it succeeds, is refused, or
throws — and a committed native cost is never refunded on any of those paths.
Bindings are weakly held and refusals are recorded in a bounded 64-entry
lifecycle ledger, so ordinary play cannot accumulate diagnostic state.

The binding order and refusal set are proven offline by an exhaustive sweep over
every combination of the decision's inputs, and the wiring is pinned by source
contracts. **That is offline engineering, not native proof.** The deterministic
blocker was established from installed IL and repaired in this implementation
candidate; the repair passes the offline gates and awaits exact native evidence
from a campaign run.

### Persistence: no schema change, and the barrier now queries the transition

No schema change: a voluntary transition adds no saved field, and the existing
pair, debt and turn-context records carry the new states.

The accepted Chunk 5 save barrier already covers the transient sequence.
`NativeSaveEffectBoundary.CommandNeedsSettlement` holds any running unfinished
command — including the Mount/Dismount shell during approach and execution — and
`HasUnresolvedAbilities` holds the `AbilityExecutionProcess` until it ends, which
is after `Deliver` has performed the relationship transition.

That was the whole argument before the review, and R5 replaced the argument with
two exact queries. `SaveEffectsReady` now also defers on
`NativeMountedControlService.HasUnsettledRelationshipTransition`, the transition
ledger's own admit-to-settle window, and on `OwnsUnsettledRelationshipShell`, a
registered Mount/Dismount shell that has not finished. The second term is strictly
more than `CommandNeedsSettlement` covers, because that predicate requires the
command to be **running** and would let a queued but unstarted relationship shell
through. The ledger term is a guard that fails closed rather than one a frame
boundary is expected to observe, since the admit-to-settle window lies inside one
synchronous game-thread call — it is queried instead of argued away.

None of that is CM07 evidence. All eleven CM07 rows, `CM07-schema-unchanged`
included, remain mandatory native work and are BLOCKED until the campaign runs
them.

## Candidate identity

Four packages were built on the remediated preview.108 line. The campaign ran on
`-campaign-c`; `-campaign-d` carries only diagnostics and was never launched.

| Field | `-campaign-c` (the run candidate) | `-campaign-d` (diagnostics, unlaunched) |
|---|---|---|
| Product version | `0.1.0-chunk6a-preview.108` | `0.1.0-chunk6a-preview.108` |
| Source commit bound by the manifest | `88328a31ff1636c68a65bdfb4bcffaf3072649a6` | `796829731087462c87e5a525b21aac2c43fce8ae` |
| ZIP SHA-256 | `e4bbc5ce1172916075aa341339b7f2e91782c316a50b214818f2b2493bb9800e` | `937144b1e917b0cd710c00de11d29f80874d4b1a3819ce890a42682625004bbb` |
| DLL SHA-256 | `6190c3540f70a8d288076a177913577d50107a5073b1a88357c63d1b6bd81f97` | `1b130b280de0d0a0a4482c01388d5f8b1dd6d9ac101a07427e053e3dd6622e8b` |
| DLL MVID | `88f43b5f-86cd-4d68-b1a7-58b217c4ae16` | `124fa1a4-ddc5-4cb8-a69b-dab6e091b4c4` |
| Purity proof | `c6a-r3-whatif` **PASS** | **none** — killed three times |
| Live runs | `c6a-r3-smoke` PASS, `c6a-mount-rt-1` FAIL | none |

The two earlier ones are superseded and must not be used for a campaign:
`-remediated-r1` (`2c0737e`, ZIP `a6a5036c…`) predates the compensation cleanup-path
repair, and `-campaign-a` (`bbda7ce`, ZIP `1cbe6554…`) predates it too.

Qualification suite: `20260925-chunk6a-suite001`, snapshot `f48d1900…`, bound to
`-campaign-c`.

Accepted Chunk 5 payload, untouched: ZIP
`35b7c82808ab8ecf264be0d511f24735c070374ca73b8259e544eee0d6200113`, DLL
`8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`, MVID
`638259af-9d31-4738-be8a-2784135d4235`.

Every one of these is a private engineering candidate. None is installed, merged or
released, and the owner's accepted preview.105 installation is untouched.

`Assert-KmcPackageManifest` requires a manifest to bind the exact current `HEAD` on a
clean worktree, so every documentation commit after a build invalidates that build for
runtime use and the next campaign payload must be rebuilt at the then-current head.
On the reviewed preview.107 line that rebuild was **measured rather than claimed**:
four qualified packages built at four different commits all produced the identical DLL
`cac89e2037b898813925782e25d5e1b5c8ef8dc24bead2388abbb01b590c8366` / MVID
`3739324f-ff40-4049-9a82-91667d8dbf24`, because nothing under `src/` changed between
them. Those four are the reviewed line, retained exactly as published; none carries
the six repairs, so none may be used for a Chunk 6A campaign.

## Offline gates

All re-measured on the remediated source, not carried forward: source contracts
**62/0**, components **522/0**, persistence assembly/storage contracts 180/0,
persistence data 56/0, profile protection 53/0, owned fixtures 620/0, validation
copies 118/0, harness 265/0, assembly-backed contracts 619/0, patch construction
30/0, package 11/0, Phase 3F contracts 9/0, registered starting installation
**13/0**, Chunk 4 core 375/0, ground 61/0, obstruction 106/0, traversal 149/0,
Chunk 5 ledger 106/0 with its completion gate still **PASS** on all 105 mandatory
behaviors, and Chunk 6A ledger record consistency **85/0** with its completion gate
**FAIL 85/85**, which is the truthful state. The whole `Test.ps1` umbrella exits 0
in 227 s.

No standing gate fails. `Test-Chunk2StartingInstallation.ps1`, which failed on
every run of the reviewed candidate because the owner's installation was
unregistered, now passes 13 of 13 with its negative controls extended rather than
relaxed.

Every accepted protocol envelope is unchanged on this source, which is the
regression signal that matters: ordinary controls 42/0, Chunk 4 play 123/0,
Chunk 4 extended 382/0, Chunk 4 traversal 149/0, Chunk 4 Charge 460/0, actor
allocation 82/0, paired restrictions 41/0, paired condition commands 67/0, paired
death 32/0, Phase 3G 20/0, Phase 3H 42/0 and Mammoth paired 18/0 all match their
pre-change counts exactly.
The eighteen assembly contracts pin the exact seams this design rests on:
`TickCommand`, both `UpdateCooldowns` overloads, `HasMoveAction` with its two
`Used*MoveAction` predicates, `Cooldowns.Clear`, `UnitCommand.get_IsActed` and
`TickApproaching`, `AbilityData.get_IsAvailable` and `get_IsAvailableForCast`, and
the roster observables `FindUnitInfo`, `ChooseNextUnit`, `StartRound`,
`HandleCombatStart` and the three `TBUnitInfo` fields adoption reads.

The source contracts pin, by construction rather than by convention: that the pair
candidate admits combat only through the explicit voluntary mode; that the
relationship service keeps one voluntary combat path and defaults every other entry
to exploration; that neither voluntary transition writes a native resource, forces a
turn end or calls a preparation; that both are admitted and settled exactly once
through the ledger; that the admission mode is decided from live combat state at
execution; that forced detach is recorded as cleanup; that adoption never re-enters
encounter start, candidate selection or a resource write and performs exactly one
native preparation and only for a pending partner slot; that adoption refuses an
unresolvable transition round before changing any state; that the admitted native
shell suppresses only the stale rider Move-resource predicate; and that mounted
Charge safety keeps every boundary unchanged.

The remediation adds thirteen more, one per repaired property: that a spent partner
slot is ended and can never be addressed again on that boundary; that a retained
partner creates no private native context and its closed allocation is observable;
that the adoption plan is immutable, generation-bound and never records an
unavailable disposition; that the lifecycle exposes plan, revalidate, commit and
rollback separately; that the rollback removes only KMC bookkeeping and never writes
or refunds a native resource; that a voluntary combat mount plans, revalidates,
commits and only then adopts, compensating a refusal; that the compensating path
returns the relationship to unmounted, reports failure and never refunds or rewinds
the generation; that the lifecycle is bound exactly once as the single adoption
authority; that the activation announcement never attempts adoption; that a
turn-based transition requires an acting rider turn and names the preparing
boundary; that a dismount delivery revalidates its captured target, its delivery
target, its rider and its generation; that the save barrier defers on an unsettled
relationship shell and on the transition ledger itself; and that the diagnostic
adoption fault is bounded to one idle exact pair, consumed once, refuses before any
state change, writes nothing, and is armed exactly once by this scenario.

## Native campaign: executed, and blocked on one preamble defect now rooted in IL

The campaign was run. It is not complete, and no mandatory case has native evidence.
Everything below is what actually happened, in order.

### The `-WhatIf` purity proof: one retained failure, four passes, three host kills

This project requires a `-WhatIf` purity proof before any live runtime use. Every
attempt is listed, including the ones that produced no verdict.

| Attempt | Package | Result |
|---|---|---|
| `c6a-whatif-1` | preview.106 (unqualified) | **FAILED** after 54 min: `WhatIf purity failed: an external tree changed.` |
| `c6a-whatif-3` | preview.107 `-r2` | refused correctly: the worktree was dirty |
| `c6a-whatif-4` | preview.107 `-r2` | **PASS** after ~55 min |
| `c6a-whatif-5` | preview.107 `-final2` | **PASS** |
| `c6a-whatif-6` | preview.107 `-final3`, repaired invoker | **PASS** |
| `c6a-r1-whatif` | preview.108 `-remediated-r1` | **KILLED, no verdict** — host low memory |
| `c6a-campaign-whatif` | preview.108 `-campaign-b` | **PASS** |
| `c6a-r3-whatif` | preview.108 `-campaign-c` | **PASS** |
| `c6a-r4-whatif` | preview.108 `-campaign-d` | **KILLED, no verdict** — host low memory |
| `c6a-r4b-whatif` | preview.108 `-campaign-d` | **KILLED, no verdict** — host low memory |

The three kills share one external cause that this mission may not touch: the host
stopped the background proof under system-wide low memory while `steamwebhelper`
held about **12.5 GB of the machine's 32.5 GB**. That is Steam's own process, not
this work's, and Steam is also a prerequisite for launching the game, so it was left
alone. Each kill left no lock and no partial state, because a `-WhatIf` run is
read-only by construction and never reaches a transaction. None of them is evidence
about purity, and none is counted as one.

`c6a-whatif-1` is retained as an unresolved bounded finding, unchanged from the
reviewed candidate: it named neither the root nor the entry, so what it observed is
still unknown and still attributed to nothing. It has not recurred across four
later passes. The comparison was never narrowed; it was only made diagnosable.

**What the missing proof does and does not permit.** `-campaign-d` carries only
diagnostics, and its pre-launch proof could not complete, so **it was not launched**.
That is the rule followed rather than argued around. For the record, and not as a
substitute: between the candidate whose proof passed (`-campaign-c`, `88328a3`) and
`-campaign-d` (`7968297`) the entire runtime-harness delta is **three lines added to
a name list**; `Invoke-KingmakerRuntimeScenario.ps1` and
`QualificationSuiteContinuity.ps1` are byte-identical, and no transaction, staging,
restore, lock, backup, quarantine or manifest code differs. The purity-relevant
harness behaviour is therefore provably the same code that proved pure — which is
worth knowing, and is still not a proof of this package.

### The live runs

| Run | Package | Outcome |
|---|---|---|
| `c6a-smoke-1` | preview.107 `-final2` | **FAIL** in 4 s: empty `QualificationSuiteId` rejected at binding |
| `c6a-smoke-2` | preview.107 `-final3` | **FAIL** in 4 s: unregistered starting payload |
| `c6a-r2-smoke` | preview.108 `-campaign-b` | **FAIL** after launch: shipped-default read |
| `c6a-r3-smoke` | preview.108 `-campaign-c` | **PASS** |
| `c6a-mount-rt-1` | preview.108 `-campaign-c` | **FAIL**: 43 assertion passes, 1 failure |
| `chunk6a-combat-mount-tb` | — | **never attempted** |

Every run restored external state. Each recorded `modsRestored` true and
`saveProtectionPassed` true, and each run transaction reached `phase=restored`.

`c6a-r3-smoke` is the first live PASS. The game launched, this exact DLL loaded
against the pinned `Assembly-CSharp`
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb` with UMM
0.28.2.0 and Harmony12 1.2.0.1, `relationshipState` Unmounted, no loaded area, zero
save requests and zero load requests.

`c6a-mount-rt-1` is the substantive run. It launched, loaded the Working fixture and
verified its identity, ran 3,050 frames over 68.6 seconds, and produced **43
assertion passes against one failure**, with `horse-companion-blueprint-registration`
PASS. It is a real result, not a blocked run — and it is a FAIL.

### Four latent defects that only running could expose

Each was invisible while the starting-payload registry refused every run, and each is
repaired with a regression rather than worked around.

1. **The no-save run transaction binding.** `New-KmcRunTransactionState` was always
   called with all three qualification-suite arguments; for a no-save run those
   variables are unbound, so PowerShell passed empty strings into
   `ValidatePattern`-guarded parameters and binding failed before the function's own
   completeness rule could run. No no-save runtime scenario had been runnable since
   the suite pin set became mandatory. Repaired by splatting the suite arguments only
   for a save-backed run; nothing relaxed.
2. **The registry's true scope.** It gates *every* live scenario, not only the
   save-backed ones, because the no-save smoke also stages into `Mods`. Established
   by running it, and reproduced independently by the standing Chunk 2 gate.
3. **The no-save smoke read a shipped default.** It failed on
   `movementExperimentEnabled` true, which was `DiagnosticSettings`' shipped default
   after a deliberate flip on 2026-08-28 in `a84dde9`; the newest prior smoke
   evidence is 2026-08-13, so no no-save smoke had been passable since. Repaired by
   scoping every experiment off for the smoke's own duration, asserting it could, and
   publishing the shipped defaults as new v1 evidence fields. Two harness regressions.
4. **A scenario could reach the game and then be rejected as unknown.** A
   tranche-handled scenario reports its own name as a subscenario;
   `chunk6a-combat-mount-rt` was registered in the save-backed list, the
   evidence-suite scope, the blueprint-audit scope and the required-rows switch, but
   not in the one list `Test-RuntimeResult` uses as its known-subscenario registry.
   That turned the RT run's real failure into a validator error. Repaired, and guarded
   by the invariant: a new harness test parses the tranche's own scenario policy and
   requires every scenario it handles to be in the validator's known set —
   reconstructed from **both** halves of that set, since the split across two files is
   what allowed the omission — and to be accepted by the orchestrator.

### The defect: root established from installed IL, repaired, not yet proven

The out-of-combat native Mount that every tranche scenario performs as its preamble
did not establish a mounted pair within its 25-second bound. The deadline recorded
mode `Default`, no pause, `feedback` "Ready to mount.", `command` null and
`hasCooldown` false — and nothing about **why**, because a relationship shell refusal
is raised as a native warning and never written to the player-action feedback that
observation samples.

Two mechanisms were checked first and **both were disproved**, which narrowed the
search without resolving it:

- `UnitCommands.Run` (`0x060026B3`) stores `m_Commands[cmd.Type] = cmd`
  unconditionally, with no combat check, so an out-of-combat Move-typed ability
  **does** occupy the Move slot. `GetCommand(CommandType)` (`0x060026A9`) is exactly
  `m_Commands[(int)type]`.
- `Init` runs before that store, and the shell is keyed on the command object rather
  than the slot, so registration order is not the problem either.

At that point the cause was genuinely unknown and nothing was blamed for it. It was
then **established**, from the same read-only inspection of the installed assembly:
`UnitUseAbility.OnAction` (`0x06002737`) sets `ExecutionProcess` from
`RuleCastSpell.ExecutionProcess` and ends with `get_IsEngageUnit` → `brtrue` →
`ldc.i4.0/ret`, else `ldc.i4.3/ret`. For a relationship ability that engages no
unit the return is **terminal**, so the command completes and leaves the Move slot
while its `AbilityExecutionProcess` goes on delivering on later frames. Move-slot
rediscovery at `Deliver` therefore had no owner left to find. That is a
deterministic blocker, not a suspicion, and *Deliver ownership* above describes
the exact binding that repairs it.

The repair passes the offline gates and **awaits exact native proof**. It is not
natively qualified, and the `CM01-exploration-free` FAIL on campaign-c is retained
immutably in the ledger's `retainedFailures` collection: when a later candidate
passes this behaviour, the live entry flips to PASS on that new frozen payload and
the historical failure stays exactly as recorded, because a PASS on a newer payload
never unmakes a FAIL on an older one.

Two instruments exist to prove it. `chunk6a-mount-preamble` is a narrow save-backed
scenario whose entire claim is that preamble, so a regression is attributable
without running a long suite; and every `ResolveDeliveringShell` refusal is now
recorded in a bounded lifecycle ledger, with the deadline carrying the relationship
state, the dispatch and refusal counters, and an exact shell-state description.
Neither has been run against a new candidate yet.

### The qualification suite

Created and retained: `20260925-chunk6a-suite001`, snapshot
`f48d19004ce942b9b8e5d5b316034d893797410a847cb23df15283500a27a6ec`, save digest
`511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426`, Mods digest
`c4e783ebd7776e3dc298528438ebf5097fc7c61b483881d49cf4a14a965a721c`, bound to
`-campaign-c`. The blocker the reviewed candidate reported is gone: suite creation
now succeeds, and the RT scenario reached gameplay through it.

## External state

The owner's installation is untouched, verified after every run and again at the
close: `Mods/KingmakerMountedCombat` holds the accepted preview.105 `Info.json`
`917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476` and DLL
`8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2` in a two-entry
tree, alongside the same seven mod directories including the deliberate SkipIntro.
`Assembly-CSharp.dll` is still
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, the exact
assembly every pinned contract and seam token was read from. 275 protected saves are
unchanged. No Kingmaker process and no runtime lock remain.

Six guarded run transactions were opened across the campaign and **all six reached
`phase=restored`**: `c6a-smoke-1`, `c6a-smoke-2`, `c6a-r2-smoke` and `c6a-r3-smoke`
as `no-save-v1`, and `c6a-mount-rt-1` as `save-backed-v3-suite`. Each recorded
`modsRestored` true and `saveProtectionPassed` true. One save-backed run loaded the
Working fixture exactly once and issued zero save requests.

No guarded deployment transaction was opened, so the installed DLL was never
replaced. No protected save, automation fixture outside its own run scope, UMM
`Params.xml`, cache or foreign mod was written. The candidate DLLs
`6190c354…` and `1b130b28…` appear nowhere in the installation: **no candidate is
installed**, as required.

The three killed purity proofs left nothing behind — no lock, no transaction, no
partial state — because a `-WhatIf` run is read-only by construction and none of them
reached a transaction.

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
10. Mount when the companion's initiative slot has **already passed** this round
    (its turn came before the rider's). Confirm the mount succeeds, then confirm
    the companion is not selectable as the pair's actor and the pair cannot move
    for the remainder of that turn: the companion's turn this round is over, and
    the rider has just spent its Move. Confirm ordinary paired movement returns
    at the next round.
11. Watch the moment the rider's turn begins. While the turn panel is still
    settling — before the rider can act — confirm Mount Companion is disabled and
    its tooltip says the transition waits until the rider's turn has finished
    preparing, and that it becomes enabled once the turn is actually acting.
12. Mount, dismount and mount again across several rounds in one encounter, then
    save, quit to the main menu, relaunch and load. Confirm both actors' spent
    actions, the pair and the turn order come back exactly once.

## Remaining scope, with the 6B handoff

None of the following is implemented, partially enabled or simulated here. Each
row of the action contract records the controlling actor, native surface, cost
owner, boundaries and open question; the seam map records the exact metadata
tokens so the next mission starts from evidence rather than discovery.

**6B — mounted Charge.** The whole feature lives behind `AbilityCustomCharge`
(type `0x02000598`). Its `Deliver` `0x06002BB6` dispatches to `RuntimeRoutine`
`0x06002BB8` or `TurnBasesRoutine` `0x06002BB7`, each of which owns a **forced
path** plus a **queued native `UnitAttack`**, and the resulting attack is stamped
`RuleAttackWithWeapon.IsCharge` `0x06007186`. The concrete problem 6B must solve
is a split of ownership the accepted architecture has not yet needed: the mount
must own the forced path while the rider keeps the Standard action that the
enclosing ability shell charges, and `IsEngageUnit` `0x06002BB5` must not strand
the pair mid-path if the target dies or moves. `CanTarget` `0x06002BBD` plus
`GetMinRangeMeters` `0x06002BBA`/`0x06002BBB` and `GetMaxRangeMeters`
`0x06002BBC` are the range surface a mounted variant would have to satisfy with
the mount's geometry rather than the rider's. Until that is done, the exact
multi-boundary rejection in `MountedChargeSafetyPolicy` and
`MountedChargeSafetyService` must stay exactly as it is; Chunk 6A's source
contract pins it, and its `chunk4-charge-safety-rt`/`-tb` rows are mandatory
CM08 ledger ids.

**6C — mounted casting and item use.** Cost stays native per the spell's or item
ability's own `ActionType`; the open questions are concentration
(`MakeConcentrationCheckIfCastingIsDifficult` `0x0600273A`,
`TryCastingDefensively` `0x0600273B`) and hand readiness
(`DontWaitForHands` `0x06002720`) while the mount is the physical mover.

**6D — staged moving actions.** The partner context's `TimeMoved`
`0x06000C13`/`0x06000C14` must carry between legs without a second grant, and
`m_AutoStopAfterFirstMoveAction` `0x04000687` must not end the rider-led boundary
between them.

**6E — the Mounted Combat defensive feat.** The pre-consequence seam is
`RuleAttackWithWeapon.AttackRoll` `0x06007197` inside `OnTrigger` `0x0600719D`.
The unresolved question is whether an immediate action is expressible at all:
`UnitAttackOfOpportunity` `0x02000502` is the only native out-of-turn player
command, so 6E must establish whether a Swift-typed command can be admitted out
of turn, or whether the feat must be modelled as a per-round allowance observed
at the attack rule. Swift debt itself is `Cooldowns.SwiftAction`
`0x0600C3BA`/`0x0600C3BB`, reset by `Cooldowns.Clear` at each `Prepare`.

**6F — consolidation.** Chunk 7 — multiple pairs, additional profiles, content
and public release — remains out of scope, as do the visual, HUD and
physical-input gates that Chunk 4 left as manual items, and the `P08-tb` bounded
known issue that Chunk 5 left open.
