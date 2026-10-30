# Validation policy

Owner directive of October 11, 2026. This document governs validation frequency
for development, review, packaging, runtime wrappers and release work. Earlier
mission/handoff chronology describes its original campaign; it cannot silently
select a broad campaign for an ordinary checkpoint.

1. Ordinary fixes end at a reviewable development checkpoint: inspect the diff,
   compile affected production code when needed, and test changed behavior and
   actual dependencies. State the focused pass's limits. Documentation changes
   need relevant text/link checks, without a DLL, package or acceptance ledger.
2. Review the coherent change before broad qualification. A review correction
   gets the failing reproduction and affected neighbors first. Batch related
   corrections before an expensive candidate build. Broaden only for an
   identified shared dependency, unresolved regression risk, or selected release
   or milestone qualification.
3. Full compatibility, historical replay, whole-roster persistence, live-lab
   purity and release reproducibility belong to a frozen candidate or justified
   broad task. They are not prerequisites for every edit, commit or review.
   Inspect nested child calls; a wrapper must not silently broaden the scope.
4. Build an unchanged candidate once and reuse its verified artifact. Distinguish
   product source, DLL/package hashes, harness/reader/fixture versions, environment
   and documentation provenance. Keep evidence bound to its original inputs;
   never label old PASS as execution on changed bytes. Documentation updates
   preserve a qualified artifact, using its frozen worktree when identity guards
   require it, rather than rebuilding to stamp a newer documentation HEAD.
5. A reader-only correction may re-evaluate immutable raw evidence only when all
   needed observations exist. Retain old and new verdicts. A changed observer or
   fixture needs its affected native scenario again. Observer/diagnostic code
   compiled into the product changes the binary identity.
6. Preserve per-transaction locks, current installation and save protection,
   package validation, controlled staging, rollback and exact restoration. Never
   kill an active transaction or omit cleanup for a time budget. Safety changes
   need focused positive and negative behavior checks before live use.
7. Ordinary validation has a 15-minute planning target and 30-minute ceiling,
   excluding necessary safe completion/restoration already in progress. If a
   selected check will exceed the ceiling, do not automatically launch broad work.
   Report useful evidence and the precise remaining check. Explicit release campaigns have
   separately declared budgets. These are workflow limits, not measured speeds.
8. Prefer behavior and stable contracts; reuse existing infrastructure. Add tests
   for concrete risks, not implementation text, cosmetic edits, arbitrary counts
   or prose wording unless wording is the contract. Reject zero-test selections.
   Reproduce a suspected baseline failure once under matched conditions, classify
   it accurately and retain it. Do not repeatedly rerun unrelated suites or
   convert known failures to PASS.
9. Use one supervised invocation with captured logs and a concise completion
   summary; preserve required child-process isolation and resumable receipts for
   unchanged inputs. Avoid repeated polling/restarts. Report tier/scope, changes,
   actual commands/durations, pass/fail counts, deferred release coverage and
   artifact identity where relevant. A focused pass is not full qualification.

## Selected entry points

```powershell
# Ordinary implementation: builds production/components and selected neighbors.
.\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-Chunk6cCasting.ps1
# After review, when freezing a native candidate, choose Candidate instead of Fast.
.\scripts\Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker -Focused Test-Chunk6cCasting.ps1
# Explicit milestone/final offline qualification only (the task must be named):
.\scripts\Test-Tiers.ps1 -Tier Full -AssemblyTarget Kingmaker -QualificationTask 'chunk7 final'
# Resolve the selection, prerequisites, artifact identity, impact map and remaining
# allowance without building, running, locking or launching:
.\scripts\Test-Tiers.ps1 -Plan -Tier Candidate -Focused chunk7\Test-Chunk7PersistenceEvidence.ps1
```

Candidate contains Fast; never run both for unchanged inputs. Full forwards the
assembly target, configuration and build choice through Test.ps1. Unsupported
combinations are refused before expensive work. Selected scripts are deduplicated
against the tier's built-in checks. `-Focused` accepts repository-relative paths
inside `scripts\`; traversal, links and paths outside the tree are refused. Current
KMC assembly checks default to Kingmaker; Wrath or Both must be selected explicitly.
`scripts/WorkflowBudget.ps1` keeps one cumulative sprint allowance (90 minutes by default; the record's `allowanceMinutes` is authoritative and any change is recorded in its `allowanceAdjustments` with the authorizing assignment, as the `sprint1` record's one-time 2026-10-30 adjustment to 135 minutes is;
`KMC_WORKFLOW_BUDGET_ID`, record under the lab's `runtime-state\workflow-budgets`):
Test-Tiers and the native launcher ask for admission before any validation, build,
lock or launch (a pure parameter-shape refusal, which needs no lab state, is decided first and
is never an attempt) and again before every further step (cumulative allowance, 30-minute
block ceiling, known oversized work refused rather than started); children inherit a
parent's entry only after it is verified live, and a real native launch under such a parent
is still one counted attempt of the batch (recorded under the parent, no spend of its own);
a real native launch is subject to the batch rule (five distinct scenario/case selections
per batch, two failed attempts per selection); the launcher re-admits before its live
transaction and immediately before the game starts, suspends the entry's clock for
settlement/restoration (never charged), meters the final readers, and completes the entry
only at the final verdict, so a late restoration or reader failure stays a failed attempt;
an interrupted entry with unknown spend blocks admission until
`scripts/Reconcile-WorkflowBudget.ps1` records it; and an exhausted allowance refuses
new work without ever shortening settlement or restoration. Isolated persistence runs
may keep the player's archive-path spelling with the launcher's `-EngineShapedSaveRoot`. Documentation or external-reader work may
invoke its relevant reader directly, without a product build.

Package creation, suite freeze, launcher purity, the cross-feature native campaign
and final persistence/acceptance ledgers are explicit candidate/release operations,
not requirements for a development checkpoint. Choose native cases by risk:
movement/attack/mount changes need affected RT/TB behavior and unmounted controls;
shared paired-turn/action budgets need both initiative arrangements and complete
activation/spending; persistence/lifecycle/disable/removal needs its actual
cold-load/removal path. Reader fixes use existing immutable re-evaluation with
positive/negative checks and old/new verdicts; run native again when observations
are missing or observer/fixture bytes changed.

Live safety and reader identity paths remain intact. Dependency-scoped purity
reuse and narrower reader identities require a separately bounded follow-up with
dependency-bound proof and bounded filesystem tests. This patch does not bypass
live guards, weaken the completion validator or redesign the ledger.

The owner separately selected preview.211 final qualification after this workflow
reset. Its 109-stage campaign, 105 mandatory Chunk 5 behaviors, both completion
ledgers and final FULL now pass on the unchanged released product; see
[the closeout](CHUNK6-ENGINEERING-CLOSEOUT-2026-10-11.md). Human acceptance remains
pending. This completed milestone does not select another campaign for ordinary
work. All frequency, identity and live-safety rules above remain in force.
Snapshot actual current human installation for any later transaction; never copy
a historical preview pin into its restoration target.

## Development acceleration (owner directives of October 16-17, 2026)

[The owner's directives, verbatim.](CHUNK7-OWNER-DIRECTIVES-2026-10-16.md) For
development work they supersede three earlier active requirements; the earlier
text in dated checkpoint documents remains history and is not rewritten.

- **Seed provenance.** Autonomous prologue production is no longer the universal
  starting prerequisite. An owner-identified, owner-supplied disposable save may be
  admitted on owner provenance plus archive validation plus one successful isolated
  native cold load. The master stays immutable outside the game's writable save
  root; every load uses a fresh ordinary copy, never a hard link. The seed is never
  relabelled as producer output and proves nothing it did not exercise. Automated
  prologue tooling and its failed evidence are retained as optional tooling.
- **Audit scope.** The blanket whole-lab rehash before and after every native
  attempt is replaced, for development runs only, by a reviewed dependency-scoped
  audit over the run's actual inputs and every surface the runner or game may
  modify. Unrelated historical archives, retired artifacts, other worktrees and
  read-only reference trees are excluded and are not claimed unchanged afterwards.
  Exposed restore targets are never excluded. Broad audit and release
  qualification remain available for a separately selected milestone.
- **Native attempts.** The 25-attempt lifetime cap is replaced by bounded batches
  of at most five declared native scenarios. The historical ledger is preserved
  exactly (14 attempts, all final FAIL); nothing is reset or renumbered. Two
  equivalent failures without a new distinguishing observation require diagnosis
  or a changed experiment before another run.
- **Review.** Coherent production and safety changes still get one independent
  review before live use. Mechanical regeneration by an unchanged reviewed generator
  does not need a separate review procession; changing the generator or its safety
  assumptions does. Recorded values in generated receipts are derived from validated
  inputs rather than restated as literals.

Unchanged and still mandatory: per-transaction locks; current-state backup and
exact restoration of every exposed surface; package, seed, runner and recovery
identity; focused positive and negative checks for safety changes; native evidence
for behaviour claims; no transfer of an old PASS to changed bytes; one native
executor; never terminating a live transaction for a time budget.

### Shared transaction surfaces and the no-deployment window

The runner moves the **entire** installed `Mods` tree into a transaction backup and
moves a staged tree into its place; restore quarantines the staged tree and moves the
original back. The whole installed `Mods` tree is therefore a shared transaction
surface even when only KMC is active, and so are UMM's `Params.xml` under
`Kingmaker_Data\Managed\UnityModManager`, the game profile and the save root. From
the pre-run snapshot until verified restoration there is a no-deployment window: no
human game session, no second executor, and no other project installing into those
paths. Other projects may keep editing and building elsewhere.

An unexpected write that reaches the staged tree during a run is preserved
byte-exact in that run's quarantine, the live tree is returned to its pre-run
snapshot so the human installation stays coherent, and the run is reported as
**not restored and not passed**, naming the quarantine so any newer deployment can
be re-applied. The only runtime write accepted as expected is UMM's assembly cache
`<name>.dll.<digits>.cache` that is a byte copy of a `<name>.dll` staged by the run,
established from three real runs.
