# Chunk 7 — sibling-slot correction and unattended six-pair FAIL exit (batch188 preparation)

Status: **IN PROGRESS.** Offline implementation, tests and independent review only. No native
run, package or suite is recorded here. The standing completion mission remains active.

## Cause carried from batch187

Six rider views share one native parent (`-93932`, 16 children). The previous attachment
lease captured each rider's sibling ordinal at mount and restored that ordinal at dismount.
Under first-in-first-out mount/dismount, an ordinal captured while earlier riders were absent
pointed past the end once they returned. In cycle 3, rider B requested index 13 with 12
children, Unity clamped it to 11, and the exact verification correctly failed. In cycles 1–2
the same proxy passed while the parent's child order was silently permuted. Batch187's
original native and outer FAILs, and batch186's, remain immutable.

## Product correction

`Domain/ScopedTransformAttachmentLease.cs` now owns one inert *sibling slot reservation* per
lease:

- **Acquire:** captures everything, then owns the reservation and places it immediately after
  the rider. It verifies that placement, detaches the rider, and verifies the reservation took
  over the vacated slot. The parent's child count never changes while mounted.
- **Restore:** returns the rider immediately in front of its own reservation. Only after the
  container, the slot and the world pose verify does it release the reservation (it leaves the
  container within the call; destruction completes at frame end), then it re-verifies.
- **Retries:** a verified slot binding is sticky, so a retry never repositions the rider. A
  rollback before detachment proves only container membership.
- **Lost reservation:** if a third party destroyed it, the captured ordinal is used with exact
  equality, and a warning is logged.
- **Polymorph replacement:** the replacement is appended last in the captured container. That
  is the placement native `Polymorph.TryReplaceView` gives it without KMC, and it never moves
  the reserved slot.
- **Scene roots:** parentless roots use the exact scene identity as their container. An invalid
  or unloaded scene is refused before unparenting.

`Integration/KingmakerMountedPairRuntime.cs` supplies the Unity operations. Reservations are
inactive `KMC_RiderSiblingSlot` objects with no components and `HideAndDontSave`. The runtime
includes reservation custody in every residue and reference gate. It deconfigures and verifies
the pose baseline before detachment, as before, but releases the pose component only after the
attachment and slot verify.

The binding sibling assertion is now relative to the owned reservation. The independent review
judged this the correct exactness contract, not a weakening. The absolute ordinal was neither
necessary (native code may remove earlier siblings) nor sufficient (batch187 cycles 1–2). The
two still agree whenever no unrelated earlier sibling changed. An IL scan found no native code
that reorders or index-iterates the shared roots' children.

## Diagnostics

- **Six-pair lifecycle:** the scenario captures each rider parent's exact child order before
  the first mount, and requires identical order and parentage after every settled cycle. The
  strict reader requires `siblingOrderVerifiedCycles=3`.
- **Unattended FAIL exit** (owner requirement: no manual intervention). Applies only to the
  exact disposable zero-write six-pair stress request, after the 45 s completion drain cannot
  settle. Before acting it requires all of:
  - the exact request, token and hash, one load and zero writes;
  - every persistence, load and serialization quiescence flag;
  - the engine's own committed, hashed retained-cleanup artifact.

  It then publishes an honest atomic FAIL result and calls `Application.Quit()` once. It never
  marks custody settled, fabricates cleanup, writes saves or kills a process. The outer runner
  still waits for exit and restores every surface.
- **Retained cleanup artifact:** `RetainCleanup` now latches only after the atomic artifact
  and its hash exist, retrying at most once per second.
- **Boundary residue gates:** leaked reservations are now counted.

## Verification (offline)

- Fast 3/0 (869 components, 147 source); Test-Harness 281/0.
- 20 domain slot tests, including:
  - the exact batch187 layout, anchored to the native ordinals `{0,7,5,4,8,9}`;
  - all 720 dismount orders and 200 random interleavings;
  - scene roots, partial-failure and retry paths, and the replacement path.
- Actual-method checks:
  - slot runtime 8/0; sibling observer 8/0; FAIL exit 51/0;
  - world cleanup adapter 88/0; host completion 17/0; host owned engines 62/0;
  - cleanup evidence 47/0; cleanup JSON 7/0.
- Six-pair checks: ownership 42/0, fixture 34/0, orchestration 19/0, reader 44/0.
- Mutation checks: every lease mutation and all nine exit-guard mutations are caught.
- The host-owned-engines test failed to compile at HEAD `0acf2a1b` (stale facade, reproduced
  before any change). Its facade is now repaired.

Independent review: CLEAR with no blockers. SHOULD-FIX 1, 2 and 4 are corrected with
failing-first tests. A focused re-review by the same reviewer was also CLEAR: about 150k injected
faults on the corrected lease produced no defect, while a rebuilt prior lease reproduced the old
permutations. SHOULD-FIX 3 (feeding the exit's real result through the full game-result reader)
must close before any exit-produced result counts toward qualification. Restoration never
depends on it.

## Not claimed

No native qualification, six-pair PASS, mounted Manual/cold, performance acceptance or HUMAN
PLAY. The native view-replacement regression belongs in the final 7E matrix's F08 row.
