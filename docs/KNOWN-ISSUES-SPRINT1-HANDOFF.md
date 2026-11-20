# Known issues remediation, Sprint 1 — validation tools (2026-11-15)

Sprint 1 of the Mounted Combat Known Issues Remediation Epic: T01, T02, T03 and the first
[current issue ledger](CURRENT-KNOWN-ISSUES.md). An offline validation-tooling repair. Nothing was launched,
staged, installed or run in the game, and no gameplay code changed.

## Identity

- **Base:** main `7a5cd2c393fad80fe09700ecbc40949245335449` (release 0.2.11). It is the epic's planning baseline;
  nothing had landed after it, and none of the three corrections existed on it.
- **Branch:** `codex/kmc-known-issues-sprint1`, in its own worktree. Draft PR #45, no reviewers requested.
- **Commits:** `07edd2fe` (T01), `3c64128a` (T02), `42fdf52d` (T03), `471fc632` (records), `a80a28b0` (review
  corrections to T02 and T03), `fdd93249` (records), `e52a59f0` (owner review of PR #45), then this record.
- **Product version:** unchanged, 0.2.11. T02 changes diagnostic code, so a build of this branch is a different
  binary from the released 0.2.11.

## Dispositions

| ID | Disposition | In one line |
|---|---|---|
| T01 | Implemented and verified offline | The capacity probe states the six-pair contract; 71 checks run where 29 did. The script still exits 1 at T04. |
| T02 | Implemented and verified offline | Reader repaired and verified offline. No native repeated-world qualification. |
| T03 | Implemented and verified offline | Loader identity and API are separate results; assembly contracts 810/0 on this machine. |
| T04 | Open correction (new) | The charge probe has further reflection drift behind the old assertion. Preserved, not repaired. |
| T05 | Open correction (new) | `Test-Chunk7SixPairNativeOwnership.ps1` does not compile. Pre-existing. |
| T06 | Open correction (new) | `Test-Chunk7RepeatedWorldProtocol.ps1` stops at an undefined variable. Pre-existing. |

## T01 — charge ownership capacity probe

- **Was:** `scripts/Test-ChargeOwnershipContracts.ps1` asserted `nativeCapacity==2` and that a third reservation
  is refused. Production has admitted six since `af08a59d` (2026-10-22), so the assertion threw and nothing after
  it had run since.
- **Now:**
  - The two-pair routing, cleanup, retained-failure and generation cases are unchanged. The stale bound became a
    read-only query that a third pair is admissible beside two owners.
  - A new isolated exercise, `ProbePairCapacity`, on the compiled product with genuine `UnitEntityData`
    containers: its own registry (from the production service constructor), six reserve-and-commit pairs,
    overlap refused in either role while capacity remains, the seventh refused with nothing created and the six
    registrations unchanged, one retirement through the production `Dismount`, the refused pair admitted into the
    released capacity, and the bound holding again.
  - The bound is the production constant, asserted equal to the reviewed literal six, and the capacity each
    compiled service wrapper passes to the registry is read from its IL.
- **Limit:** the wrappers that read `Kingmaker.Game` cannot run detached (its type initializer needs the Unity
  player). They are checked by IL, not executed. Their behavioural harness is T05.
- **Result:** 71 pass, then the T04 failure. Before: 29 pass, then the stale assertion.
- **Counterexample:** a build with `NativePairCapacity = 5` fails the probe at its first capacity check.

## T02 — repeated-world subscriber contract

- **Was:** the reader required 13 lifecycle interfaces and 15 owned rows by count. Production holds 15 lifecycle
  interfaces plus the defence and tack services (21 process-owned rows). The producer's own in-game check held
  the same stale literals, so a current build could not have produced passing evidence at all.
- **Now:**
  - Reader: reviewed revisions r1 (historical, implicit) and r2 (declared), selected by evidence contract and
    producer identity, with exact membership, one instance per process-owned subscriber and totals from the
    reviewed set. Identities it cannot place are refused as unsupported.
  - Producer: `Chunk7ReplaySubscriberContract` declares r2; the scenario emits `chunk7-repeated-worlds-v3` with
    the revision name. Diagnostic evidence protocol only.
  - Horse owners follow the revision. The reader's list was stale too (see the ledger).
- **Checks:**
  - `scripts/chunk7/Test-Chunk7RepeatedWorldEvidence.ps1`: 220 pass. Historical fixtures unchanged and passing;
    synthetic current fixtures pass; missing, duplicate and foreign handlers, wrong ownership, stale world
    identities and mixed serial/hash rows are refused, each asserted by its own reason. On main this file exits 1
    after 165.
  - `scripts/chunk7/Test-Chunk7ReplaySubscriberContract.ps1` (new): 21 pass, 0 unassessed. It subscribes each
    compiled production subscriber to the installed game's `EventBus`, reads it back through the product's own
    snapshot, compares producer, reader and compiled metadata, runs the producer rule and the reader boundary on
    that inventory, and compares r1 with the tag `v0.1.0-chunk7-qualified.96`.
  - Five component tests for the producer rule (components 1234, was 1229).
- **Counterexample:** a build whose lifecycle subscriber loses `ITeleportHandler` fails the compiled check.
- **A regression of mine, found and fixed before commit:** `Test-Chunk7OwnerSeedRepeatedEvidence.ps1` imports the
  fixture builder by function. My first version kept the fixture table at script scope and broke it (29 pass on
  main, 4 fail). The fixtures now live inside the function. It passes 29 again.
- **Side effect:** `Test-Chunk7OwnedReplayEvidence.ps1` exited 1 on main for the same stale pin and now passes.
- **Synthetic fixtures are labelled synthetic** in the test file and its output. No real repeated-world evidence
  exists on this machine (the lab's `runtime-evidence` is empty), so no recorded artifact was re-evaluated and no
  recorded verdict changed.

## T03 — UMM identity and API

- **Was:** three in-line pins (SHA-256, MVID, version of 0.33.2) in `Test-AssemblyContracts.ps1`. The installed
  loader here is the genuine 0.33.0.
- **Now:**
  - `planning/UMM-LOADER-PROFILES.json`: reviewed identities with provenance (0.28.2, 0.33.0, 0.33.2).
  - `scripts/UmmLoaderContracts.ps1`: identity against one selected profile; API against the reference itself.
  - `scripts/Test-UmmLoaderContracts.ps1`: one reference per process, four separate result lines.
  - `scripts/Test-UmmLoaderContractChecks.ps1`: the acceptance cases and the matrix.
  - `LocalGamePaths.props.example` documents two optional local properties, `UnityModManagerProfile` and
    `UnityModManagerReferencePaths`. Neither is read by the build.
- **Selection:** `-UmmProfile`, else `UnityModManagerProfile`, else the reviewed profile the reference
  identifies as. Without a selection the installed loader must itself be a reviewed profile.
- **Results:** see the matrix in the ledger. Kingmaker assembly contracts 810 pass, 0 fail (801 pass, 3 fail
  before); the 785 checks unrelated to the loader are line-for-line unchanged.
- **Counterexamples:** selecting `umm-0.33.2` or `umm-0.28.2` for the installed loader fails identity and only
  identity; an unprofiled loader with a complete API fails identity; six synthetic stub loaders each fail the
  API; a wrong game-assembly pin still fails (809 pass, 1 fail).
- **A weakness of mine, found by the negative tests and fixed:** the first binding sweep scanned member-reference
  tokens and treated one exception type as the end of the table. A missing member raises that same type, so the
  scan stopped silently. The sweep now reads the product's loader references by name from its metadata (with the
  dnlib beside the installed loader) and binds each one.
- **Unchanged:** `Info.json` `ManagerVersion` 0.28.2; `planning/ENVIRONMENT-FINGERPRINT.json` and the native
  runner's loader pin (0.33.2); the installed loader.

## What was run

All under Windows PowerShell 5.1, offline. Budget `known-issues-sprint1`: about 12 of 90 minutes in five tier
entries (one failed entry is the baseline, which stops at the stale charge assertion by design). The final rows
are from the head after both rounds of review corrections.

| Check | Result |
|---|---|
| Baseline at `7a5cd2c3`: `Test-Tiers.ps1 -Tier Fast -Focused Test-ChargeOwnershipContracts.ps1,…` | build, components 1229/0 and Validate-Source 151/0 pass; charge script FAIL at the two-pair assertion |
| Final: `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint1 -Focused Test-TierSelection.ps1,Test-AssemblyContractConfiguration.ps1,Test-UmmLoaderContractChecks.ps1,chunk7\Test-Chunk7RepeatedWorldEvidence.ps1,chunk7\Test-Chunk7ReplaySubscriberContract.ps1,chunk7\Test-Chunk7RepeatedWorldAdapter.ps1,chunk7\Test-Chunk7OwnerSeedRepeatedEvidence.ps1,chunk7\Test-Chunk7OwnedReplayEvidence.ps1` | **PASS 11/0**, 3.34 min: components 1234/0, Validate-Source 151/0, tier selection 29, checker configuration 6, loader checks 22 (1 unassessed, 0 errors), reader 220, compiled contract 21, adapter 76, owner-seed 29, owned replay 59 |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | **810 pass, 0 fail** |
| `Test-ChargeOwnershipContracts.ps1` (complete run) | 71 pass, then **exit 1** at T04 |
| `chunk7\Test-Chunk7RepeatedOwnerActorIdentity.ps1` | 17 pass on main and on the branch |
| `chunk7\Test-Chunk7ProgressionFixtures.ps1` | 19 pass on main and on the branch |
| `chunk7\Test-Chunk7SixPairNativeOwnership.ps1` | compile failure on main and on the branch (T05) |
| `chunk7\Test-Chunk7RepeatedWorldProtocol.ps1` | 27 pass, then failure on main and on the branch (T06) |

Four mutation runs (capacity 5, lost interface, wrong game pin, a sweep that ignores the configuration) each
built or ran a temporary copy and were reverted; the working tree holds none of them.

## Independent review

One independent read of the three code commits, before any use. It found no defect in T01 or in the T02
producer, and five in the T02 reader and T03. All five are corrected in `a80a28b0`, each with a check.

| Finding | Correction |
|---|---|
| **T03.** The compiled-reference binding ignored accessibility: a loader member that became non-public still "bound", and the API passed. | A bound member must also be public on a visible type. A synthetic loader with `internal ModEntry.Active` and `private UI.tabId` now fails the API. |
| **T03.** The binding sweep took `bin\Release`, else `bin\Debug`, so it could describe a build that was not under test. A mistyped `-ProductDll` read as unassessed. | The scripts take `-Configuration` and use exactly that build. A named product that does not exist is refused (exit 2). An unassessed sweep is stated on the API line. |
| **T03.** `UnityModManagerProfile` from `LocalGamePaths.props` was applied to any reference, and would have failed the check script. | It applies to the installed loader only, and the check script never reads it. |
| **T02.** Names were compared with culture-sensitive operators, which ignore characters such as U+00AD, so an inventory with an invisible character in a name was accepted. | Every evidence name is compared ordinally. Five checks with U+00AD. |
| **T02.** Some refusals were not labelled unsupported: a release-scheme producer with v1/v2, v1 from preview.100 or later, a missing contract name, a version with a trailing line break. | Each is now an explicit "Unsupported repeated-world evidence" refusal, with a check. |

## Owner review of `fdd93249` (PR #45)

Two findings, both in T03, both corrected in `e52a59f0`.

**1. The umbrella did not hand its configuration to the assembly checker.** `scripts/Test.ps1` called
`Test-AssemblyContracts.ps1` with `-Target` only, so the loader binding sweep read the Release build whatever
the umbrella was testing.

- `Test.ps1` now forwards `-Configuration $Configuration`.
- The call chain, as checked:
  - Full: `Test-Tiers.ps1` → `Test.ps1 -Configuration` → `Test-AssemblyContracts.ps1 -Target -Configuration`.
  - Candidate: `Test-Tiers.ps1` → `Test-AssemblyContracts.ps1`; the selector forwards `-Configuration` to
    any child that declares it, which the checker does.
  - `-Target Both` hands the configuration to both children.
- `Test-TierSelection.ps1` runs the real umbrella with tracing children. It now asserts the configuration the
  assembly checker received. On `fdd93249` it fails ("Full did not forward target/configuration/SkipBuild
  through the actual umbrella"); now it passes 29.
- New `Test-AssemblyContractConfiguration.ps1` runs the real checker in private repository layouts. 6 pass:
  - only a Debug build exists: a Debug run binds it; a run that is not told the configuration reads nothing and
    reports the sweep unassessed;
  - a Debug build beside a different Release file: the Debug run reads the Debug build, and the Release run
    reads the other file and fails on it;
  - `-Target Both` hands the configuration to its Kingmaker child.
- The checker now prints which build its sweep read.
- **Counterexample:** a sweep that takes Release, else Debug, whatever it is told fails the new test.
- **Found by the new test:** a product with no loader reference crashed the sweep under strict mode instead of
  failing it. It now fails explicitly.
- The Full umbrella was not run to verify this, by instruction.

**2. The loader matrix showed an incomplete inspection as a finished row.** For an additional reference with no
reviewed profile, the matrix printed the child's identity and API lines without checking that there were any.

- One inspection now ends as exactly one of four outcomes:

  | Outcome | Row | Counts as |
  |---|---|---|
  | Completed, API PASS | identity not asserted, API PASS | data |
  | Completed, API FAIL | identity not asserted, API FAIL, the incompatible members | data: an unprofiled or below-minimum loader is not required to be compatible |
  | Reference unavailable | UNAVAILABLE, both cells UNASSESSED | unassessed |
  | No usable result | INSPECTION ERROR with the checker's exit code, both cells UNASSESSED, then the checker's own output | error: the script exits 1 and says the matrix is incomplete |

- The exit code and the result lines must agree; anything else is the last outcome.
- Before, with an unreadable file as an additional reference: a row with three blank cells, `PASS=18 FAIL=0`,
  exit 0. After: an `INSPECTION ERROR` row, the `BadImageFormatException` text kept, `ERRORS=1`, exit 1.
- Regressions in `Test-UmmLoaderContractChecks.ps1` (22 pass, was 18): the classifier on eleven result shapes;
  a real unreadable reference; a real absent reference; synthetic compatible and incompatible loaders.
- No identity check, incompatibility report or declared minimum changed.

## Not run, and not claimed

- No game launch, no native fixture, no transaction, no install, no loader change.
- No native repeated-world run. T02's current contract is not natively observed.
- `Test-AssemblyContracts.ps1 -Target Both` and `-Target Wrath`: this machine has no Wrath install.
- Candidate and Full tiers: not selected. Sprint 1 touches no gameplay code. The forwarding through the Full
  umbrella is checked by the tracing run in `Test-TierSelection.ps1`, not by a Full run.
- UMM 0.28.2: no genuine binary available, so unassessed.
- The remaining offline scripts outside the Fast tier were not surveyed. Three of the neighbours that were run
  are stale (T04–T06); others may be.

## Evidence

Logs are in the lab, outside the repository:
`C:\Dev\KingmakerMountedCombatLab\artifacts\known-issues-sprint1\`, numbered `01` to `21`: the baselines at
`7a5cd2c3`, the T01 full run, the T04 scratch characterisation (labelled UNCOMMITTED), the three mutation runs,
the loader matrix, the assembly contracts before and after, the Fast runs before and after each review, and
the before and after of both owner-review findings (`16` to `21`).

## For the next session

- `LocalGamePaths.props` is machine-local and not tracked. The worktree's copy lists the NuGet reference copies
  for the matrix; the main checkout's copy does not, so there the 0.33.2 row reads UNASSESSED until
  `UnityModManagerReferencePaths` is added.
- A build of this branch emits repeated-world v3. The reader accepts it for product versions 0.2.11 and later.
  A later change to a production subscriber's interfaces, or a new process-lifetime subscriber, needs a new
  revision in both `Chunk7ReplaySubscriberContract.cs` and the reader; the compiled check fails until it has one.
- Facts worth keeping:
  - `Kingmaker.Game`'s type initializer throws detached, and the failure is permanent for the process. Never touch
    `Game` statics in a detached probe.
  - The detached `EventBus` works with uninitialized subscriber objects, and the product's own
    `Chunk4SubscriptionSnapshot.Capture` reads it.
  - Two `UnityModManager` assemblies cannot be reflection-only loaded in one process.
  - In Windows PowerShell 5.1, `Module.ResolveMember` reports an unbindable member reference as
    `ArgumentOutOfRangeException`, the same type as a token past the end of the table.
- **Not authorized and not done:** merge, release, tag, install, reviewer request, Sprint 2.
