# PR-2 / PR-4 lifecycle composition mapping

Owner authorization 2026-11-28a section 7. This is review material for PR-2 (#70). It does not change accepted PR-4 (#66, reviewed source
`d615d1d5`), and it is not the PR-6 integration. It records how the two lifecycle changes compose, so PR-6 wires one ordering instead of
choosing one whole composition root or running both disable routines in sequence.

## Shared files

| File | PR-2 (this branch) | PR-4 (accepted `d615d1d5`) |
|---|---|---|
| `CompositionRoot.cs` `SetEnabled(false)` | delegates its entry to `MountedServiceTeardownSequence.BeginDisable` | delegates the whole transition to `ServiceLifecycleSequence.Disable` through `LifecycleBoundary` |
| `CompositionRoot.cs` `SetEnabled(true)` | unchanged | `ServiceLifecycleSequence.Enable` (repair consent started after a successful enable) |
| `CompositionRoot.cs` `PerformFailureCleanup` | unchanged | `ServiceLifecycleSequence.FailureCleanup` (repair consent stopped) |
| `CompositionRoot.cs` `UpdateCore` | `MountedServiceTeardownSequence.RetryWhileDisabled` before the disabled-state early return | unchanged |
| `CompositionRoot.cs` `Dispose` | `MountedServiceTeardownSequence.Unload` | unchanged |
| `MountedLifecycleSubscriber.HandleModDisable` | `MountedServiceTeardownSequence.ModDisable` (drain, readiness, fence, charge drain, `CleanupPairs`, drain after) | unchanged |
| `KingmakerMountedPairRuntime.RestoreMovementAuthority` | round 16: under `CleanupTrigger.ModDisabled` a live mount Move command the pair does not own is not interrupted or stopped | unchanged |

`ServiceLifecycleSequence.Disable`'s first three steps (`SaveOrTeardownBusy`, `LoadInFlight`, `HandleModDisable`) are the pre-PR-2 root
body that PR-2's `BeginDisable` replaced with the same checks plus a safe pre-drain. They are therefore one step, not two.

## Composed disable (`SetEnabled(false)`)

`ServiceLifecycleSequence.Disable` with its first three steps replaced by one boundary call, `BeginTeardown()`, which runs
`MountedServiceTeardownSequence.BeginDisable` and maps its verdict to PR-4's existing refusal messages:

| # | Step | Source | On refusal |
|---|---|---|---|
| 1 | Safe pre-drain of retained owned pair cleanup, only outside the save/load/worker fences | PR-2 `BeginDisable` | (no refusal) |
| 2 | Readiness: `SaveSuspended`, `AnyChargeFenced`, `!AllTeardownOwnersReady` | PR-2 `BeginDisable` (= PR-4 `SaveOrTeardownBusy`) | refused; nothing changed |
| 3 | `LoadInFlight` | PR-2 `BeginDisable` (= PR-4) | refused; nothing changed |
| 4 | `HandleModDisable` → `ModDisable`: drain, readiness, admission fence, charge drain, `CleanupPairs` (relationship dismount, successor preserved), drain after | PR-2 | refused; hooks, owner and retry kept |
| 5 | Record `ShieldEnabled`; `DisableShield` | PR-4 | refused; services and repair consent kept |
| 6 | `DisableSelection`; on conflict `EnableShield` if it was enabled | PR-4 | refused; Shield rolled back |
| 7 | `IsEnabled = false` | PR-4 | — |
| 8 | `StopRepair` (repair consent cleared after a successful stop only) | PR-4 | — |
| 9 | `DisableServices` | PR-4 | — |

## Enable, failure, per-frame retry and unload

- **Enable:** PR-4 `ServiceLifecycleSequence.Enable` unchanged: selection, `IsEnabled = true`, `StartRepair`, remaining services. PR-2 does
  not change enable.
- **Failure cleanup:** PR-4 `ServiceLifecycleSequence.FailureCleanup` unchanged (`Combat.Cancel`, `Dismount(CleanupTrigger.Exception)`,
  Shield stop, `IsEnabled = false`, `StopRepair`). The `Exception` trigger keeps stopping both actors; PR-2's successor preservation applies
  to `ModDisabled` only.
- **Per-frame retry:** PR-2 `RetryWhileDisabled` stays in `UpdateCore` before the disabled-state early return, outside the
  save/load/area/restoration fences, so retained work left by a refused disable or a failure cleanup is retried while disabled.
- **Unload:** PR-2 `MountedServiceTeardownSequence.Unload` unchanged (load fence first, owned-worker wait, `HandleModDisable`, Shield
  release, hooks removed last). PR-4 does not change `Dispose`.

## Local composition probe (offline)

A local probe compiled the composed `ServiceLifecycleSequence` above with this branch's real `MountedServiceTeardownSequence` over a
recording fake service side (sources and runner under
`analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/pr2/lifecycle-probe/`, metered 1.0 s). Result: **23 pass / 0 fail**.
- Ordinary disable order:
  `Drain > HandleModDisable > Drain > DrainCharge > CleanupPairs > Drain > DisableShield > DisableSelection > IsEnabled=False > StopRepair > DisableServices`.
- Retained cleanup refuses before any service change, then succeeds on a later request.
- Save and load fences refuse with zero drains.
- Cancellation-created cleanup refuses before Shield, selection or repair, then succeeds.
- A stuck Shield contribution refuses with services and repair consent kept.
- A selection conflict rolls Shield back.
- Repair consent returns only with a successful re-enable, and failure cleanup clears it.
- The disabled-state retry drains outside fences and is withheld inside the area fence.
- Unload is refused during a load and on residue (hooks kept), then becomes Ready with hooks removed last.

This probe checks the ordering with fakes. It is not the integrated root. At PR-6 the actual composed boundary is tested: successor
preservation, failed disable and retry, Shield rollback, repair consent across stop/re-enable and failure, and refused then successful
unload, through the PR-4 component tests and the PR-2 harness teardown cases on the integrated source.
