# Chunk 7 — six-mounted native Manual and its cold consumer (batch189 preparation)

Status: **IN PROGRESS.** Offline implementation, tests and review only. No native run,
package or suite is recorded here. The standing completion mission remains active.

## Why

Batch188 passed the zero-write six-pair lifecycle. F09 still lacked six-pair mounted
persistence: the cold-qualified fixture Manual308 is unmounted, so it cannot certify a
six-mounted save or its cold restoration. This change adds two closed owned-pairs cases on
the same fixture:

| Case | Scenario | Native I/O | Pins |
| --- | --- | --- | --- |
| `six-horse-mounted-persistence-save` | `chunk7-persistence-save` | 1 load (Manual308), 1 Manual | Source* = batch185 cold qualification of Manual308 |
| `six-horse-mounted-persistence-load` | `chunk7-persistence-load` | 1 load (the producer's Manual309), 0 writes | Same Source*; Checkpoint* = the producer run and its persistence evidence |

The output is `Manual_309_KMC_C7_SIX_MOUNTED_WORLD.zks`: the next native number after
Manual308, under a name distinct from the fixture's (`CreateNewSave` would otherwise make a
repeated name unique).

## Producer and consumer (diagnostics)

`Diagnostics/Chunk7PersistenceScenario.SixPair.cs` is a closed branch of the existing
persistence engine. Every historical two-pair assertion stays unchanged; the main file only
routes the six cases to it.

- **Producer.** It binds the twelve exact sourced roles and the remote Linzi through the
  existing inherited-fixture custody. It then issues one native selected-ability Mount per
  role, in role order. Each input waits for every actor to be idle and for the previous shell
  and relationship transition to settle. The producer records each minted owner and
  generation and requires the registry to hold exactly those owners. Each rider's own
  Dismount and Rider Primary go into its first empty hotbar slots, and the persistent capture
  must be exactly those twelve. Then comes one native Manual through the existing owned-save
  admission (Manual309 reservation, single writer, readback). The readback must hold every
  role once under its minted owner, generation and declared profile, plus the twelve slots.
- **Cold consumer.** In a separate process it loads only the producer's Manual309. It
  requires the whole six-pair exploration graph to reach presentation, and adopts each
  restored owner and generation from the archive. It requires 12 semantic restores, 6
  presentations, 12 native post-loads, 12 one-time debt restores, and no native
  preparation, round, combat-actor restore, Mount replay or save.
- **Both.** They route one fixed two-metre ground move through every pair, with exact
  owners asserted at every step, then run the existing all-owner cleanup.

## Unattended failure exit

The owner requires that no six-pair run need manual intervention. The batch188 exit covered
only the zero-write stress on the world engine. It now also covers these two persistence
cases on the persistence engine, under the same request, token, hash and quiescence guards,
plus two new conditions:

- every native write the run requested must have completed within the declared quota, with
  saves requested = authorized = staged snapshots = completed saves, and at most one for the
  producer and zero for the cold consumer;
- the engine's own committed, hashed `chunk7-persistence-cleanup-pending.json` must exist.

The persistence engine's retained-failure artifact now latches only after it is written and
hashed, and retries at most once per second. This matches the world engine's batch188
correction.

**Accepted residual.** The exit treats a requested write that never completed as possibly
in flight, so it never fires in that state. If the producer's single native write fails, the
scenario fails and its ordinary cleanup normally settles; the host then publishes FAIL and
exits normally, with no exit needed. Only a double failure needs the owner: the write fails
*and* cleanup cannot settle. Widening the exit to "settled but failed" writes would require
proving the persistence service's failure accounting complete, and is not attempted here.

## Outer contracts

- **Selection, descriptor and routing.** These are closed in C# (`Chunk7OwnedPairsSelection`,
  `RuntimeRequest.Validate`) and in PowerShell (`Chunk7SixPairPersistenceEvidence.ps1`).
- **Producer source gate.** It is the unchanged batch185 Manual308 qualification, including
  its marker and the copy route.
- **Consumer source gate.** It re-derives that fixture provenance, then pins the producer's
  completed receipts, re-runs the full six-pair reader on the producer's evidence, and
  verifies the exact Manual309 bytes.
- **Closed persistence reader.** It requires an exact observation set, the six-pair archive,
  the boundary snapshots, six ordered native Mount inputs (each minting one owner), six fixed
  continuations, the cold restoration observer and cleanup.
- **Separate-process cold proof.** The runner checks a distinct process, the producer's
  read-back archive equal to the loaded archive, identical owners, generations and slots, the
  same candidate, and unchanged roster, progression, remote companion and pair provenance.

## Verification (offline)

| Check | Result |
| --- | --- |
| Six-pair persistence stages, actual partial with native facades | 82/0 |
| Shared-engine wiring of every six-pair hook, with detection self-test | 12/0 |
| Closed reader, producer gate, Manual309 leaf, cold proof (generated evidence) | 122/0 |
| Unattended exit, actual partial with real command line | 78/0 (51 before) |
| Persistence cleanup adapter, incl. new latch checks | 103/0 |
| Six-pair fixture source/remote | 37/0 |
| Owned-pairs authority and protocol (C#) | 551/0 |
| Owned-pairs DTO C#/PowerShell parity | 209/0 |
| Fast | 3/0 (869 components, 147 source) |
| Harness | 281/0 |

A 35-test focused regression set passes. The one exception, `Test-Chunk7ObserverAcquisition`,
already failed to compile at HEAD (stale restoration-probe facades); it is unrelated and
unchanged here. A mutation run on scratch copies caught 37 of 38 targeted mutations. The
survivor removes `owner.Generation == sixGenerations[index]`, a condition the `expectedOwners`
generation check still enforces.

The independent review was CLEAR, with no blockers. Its two SHOULD-FIX items were handled:
the shared-engine hooks now have the wiring check above, and the failed-write residual is
recorded above. Its notes led to six-pair cold-deadline forensics and a reader bound that
covers the cleanup window.

## Test repair

`Test-Chunk7PersistenceCleanupAdapter` did not compile at HEAD `f2038214`; this was reproduced
in a detached HEAD worktree before any change. Its compile list and facades were stale
(Small/Stock Mammoth selections, Manticore fields, the three-argument `NativeName` and a
`Process` facade shadowing `System.Diagnostics.Process`). These were repaired minimally; the
adapter now runs.

## Not claimed

No native six-mounted Manual, cold restoration, performance acceptance or HUMAN PLAY. The
unattended exit's real result through the full game-result reader (SHOULD-FIX 3) remains open.
