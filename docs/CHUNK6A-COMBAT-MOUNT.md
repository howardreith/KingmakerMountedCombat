# Chunk 6A: legal combat Mount/Dismount

Single active report for the owner's Chunk 6A mission. Status, identities and
the acceptance ledger live here; the frozen product and action contract is
[planning/CHUNK6-ACTION-CONTRACT.md](../planning/CHUNK6-ACTION-CONTRACT.md) and
the installed-assembly seam map is the Chunk 6A section at the top of
[planning/ASSEMBLY-CONTRACT-MATRIX.md](../planning/ASSEMBLY-CONTRACT-MATRIX.md).

## Status

**BLOCKED for native qualification; PARTIAL overall.** The engineering work is
complete, the candidate is frozen and every offline gate passes, but no
save-backed native scenario can be started, so the acceptance ledger's completion
gate does **not** pass and all 82 mandatory behaviors are BLOCKED. This is not a
candidate for acceptance and must not be described as one.

### The blocker, exactly

A save-backed runtime scenario requires a qualification-suite snapshot, and
`scripts/runtime/New-KmcQualificationSuiteSnapshot.ps1` refuses to create one:

```
Existing KMC tree differs from the exact registered starting payload.
  QualificationSuiteContinuity.ps1:63
```

`Assert-KmcRegisteredStartingPayload` holds a fixed registry of accepted starting
installations, keyed by the installed `Info.json` hash. It has six entries, the
newest being Chunk 5's starting payload preview.54
(`Info.json` `1224394f59ec598895a0d6ffd1db05527d04f334a063461a019c11f98ddbe528`).
The owner's current installation is the accepted **preview.105** alpha, whose
`Info.json` is `917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476`
and whose DLL is `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`.
That hash is not in the registry, so no pin set is selected. The installed tree
also has only **two** entries — `Info.json` and the DLL, with no UMM loader cache,
because the guarded deployment replaced the DLL and the game has not been run
since — while every registered pin set describes a **three**-entry tree including
that cache, so the entry-count check fails before any hash is compared.

Registering preview.105 as an accepted starting payload is an identity-guard
change, and this mission explicitly does not authorize broadening identity
exceptions "merely to make a run pass." It is also genuinely the owner's call:
that registry is the record of which installations the owner has accepted as a
qualification starting point. So it is reported, not changed.

**What would unblock it**, in the owner's own terms and needing no guard
weakening: register the already-accepted preview.105 installation in
`Assert-KmcRegisteredStartingPayload` the way preview.13, preview.37 and
preview.54 were registered for their own missions — `Info.json`
`917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476`, DLL
`8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`, with the
entry-count expectation matching a two-entry tree that has no loader cache yet.
Its provenance is already documented: guarded deployment receipt
`runtime-state/deployment-operations/20260925T0200587550503Z-94de251601b24a04a1f5394f57a92f64.json`.
Once registered, the frozen candidate below needs no rebuild: take a suite
snapshot and run `chunk6a-combat-mount-rt` and `chunk6a-combat-mount-tb`.

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
| Product version | `0.1.0-chunk6a-preview.107` |
| Qualifier | `chunk6a-combat-mount` |
| Package | `KingmakerMountedCombat-0.1.0-chunk6a-preview.107-chunk6a-combat-mount-diagnostic.zip` |
| Source commit bound by the manifest | `3dfcb07e5d75b2677a7497337eb48384762dd918` |
| ZIP SHA-256 | `9f0cb8d25ab83796a1a42222f3d7460bf9170b6d9902d41884e9184ef11c85e6` |
| Manifest SHA-256 | `4feaacf9dd43de22d64f34183c62a02f870732092296d8680a65a6e47f793d56` |
| DLL SHA-256 | `93656626d82277ab5b76eb616935d9361970f9edce54dd55a8e6616bf299272b` |
| DLL MVID | `4574951e-e2e4-4329-9bd6-4b51e46430ed` |
| Qualification suite | **none** — blocked; see the blocker above |
| Accepted Chunk 5 payload, untouched | ZIP `35b7c82808ab8ecf264be0d511f24735c070374ca73b8259e544eee0d6200113`, DLL `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`, MVID `638259af-9d31-4738-be8a-2784135d4235` |

This package is a private engineering candidate. It is not installed, not merged
and not released, and the accepted preview.105 package is untouched. Note that
the harness requires a package manifest to bind the exact current `HEAD`, so a
later documentation commit invalidates this binding for runtime use; rebuilding
at the final `HEAD` reproduces the same DLL bytes because no `src/` file changes.

## Offline gates

On this source: source contracts 46/0, components 511/0, persistence
assembly/storage contracts 180/0, persistence data 56/0, profile protection 53/0,
owned fixtures 620/0, validation copies 118/0, harness 262/0, assembly-backed
contracts 619/0, patch construction 30/0, package 11/0, Chunk 5 ledger 106/0 with
its completion gate still PASS, Chunk 6A ledger record consistency 82/0 and its
completion gate **FAIL 82/82**, which is the truthful state.

The eighteen new assembly contracts pin the exact seams this design rests on:
`TickCommand`, both `UpdateCooldowns` overloads, `HasMoveAction` with its two
`Used*MoveAction` predicates, `Cooldowns.Clear`, `UnitCommand.get_IsActed` and
`TickApproaching`, `AbilityData.get_IsAvailable` and `get_IsAvailableForCast`, and
the roster observables `FindUnitInfo`, `ChooseNextUnit`, `StartRound`,
`HandleCombatStart` and the three `TBUnitInfo` fields adoption reads.

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
than trusting the game's own arithmetic. They have not been run, because a
save-backed scenario cannot obtain a qualification-suite snapshot — see the
blocker under Status.

### Retained failure: the `-WhatIf` purity proof did not pass

This project requires a `-WhatIf` purity proof before any live runtime use. One
was attempted for `mod-load-smoke` against an unqualified throwaway package. It
ran for 54 minutes and then **failed** with `WhatIf purity failed: an external
tree changed.` That failure is retained here as the reason the native campaign is
NOT RUN, and it is deliberately **not** attributed to a cause I did not establish.

What is measured:

- The purity proof compares `runtime-state`, `runtime-backups`, `runtime-staging`
  and `runtime-evidence` plus the live `Mods` root before and after, by full
  SHA-256 manifest over every file. Those four lab trees hold **623,407 files and
  148,296,003,194 bytes**, so one proof hashes about 296 GB. Observed throughput
  was 119 MB/s falling to 31 MB/s in small-file regions.
- The harness's error does not name which of the five manifests differed.
- No file in any of the four trees had a modification time inside the 90-minute
  window, so a plain concurrent content write is **disproved**.
- `runtime-state` (3,829 files) and the live `Mods` root (358 files) are stable
  across two consecutive manifest passes and their path sets are unchanged.
- Running `Test-Harness.ps1`, then the whole `Test.ps1` umbrella, then every
  persistence gate (`Test-PersistenceContracts`, `Test-PersistenceData`,
  `Test-PersistenceProfileProtection`, `Test-PersistenceSaveFixtures`,
  `Test-PersistenceValidationFixtures`) left the path-and-length sets of
  `runtime-evidence`, `runtime-staging` and `runtime-backups` **unchanged**. So
  none of the offline work performed during the proof perturbs those trees.

A determinism test — two consecutive manifest passes over each tree, with nothing
else running — was then performed to distinguish a real perturbation from
non-deterministic enumeration or hashing at this scale:

| Tree | Files | Bytes | Pass 1 | Pass 2 | Digest stable |
|---|---|---|---|---|---|
| `runtime-state` | 3,829 | 279 MB | — | — | yes |
| live `Mods` | 358 | — | — | — | yes |
| `runtime-evidence` | 13,321 | 6.05 GB | 10 s | 8 s | yes |
| `runtime-backups` | 193,050 | 49.96 GB | 172 s | 165 s | yes |
| `runtime-staging` | 413,207 | 91.98 GB | >20 min, **stopped** | — | **not measured** |

`runtime-staging` was stopped after twenty minutes of its first pass so the
purity proof itself — which is a superset of this test and now names the
differing tree — could run instead. Its per-entry cost is the dominant term:
472,074 entries at roughly 400 per second, which also explains why the failed
proof took 54 minutes rather than the few minutes the byte volume alone implies.

What is **not** established: which of the five manifests differed, and why.
`runtime-staging` is the one tree whose determinism is unmeasured, which makes it
the most likely candidate, but that is a narrowing of the search and not a
finding. Eliminating the causes I could think of does not prove the cause lies
outside this repository's tooling, and it is not called an environmental or engine
problem here.

The proof is not narrowed or bypassed to get past this. Restricting that
comparison to the trees a run can mutate would weaken a safety guard this mission
does not authorize weakening, so the campaign stays NOT RUN and the candidate
stays PARTIAL.

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
