# Chunk 6B preview.176 continuation audit — 2026-10-05

**IN PROGRESS; persistence continuation BLOCKED — CRITICAL at the owner's explicit unproven-persistence boundary.**
The interrupted campaign finished before takeover. Its WhatIf purity proof is PASS, while its six native
stages are **5 PASS / 1 FAIL**. The TB failure is a failed charge delivery, not a staging failure or a
spent-turn refusal in a later row. The frozen product was neither changed nor rebuilt during this audit.
Chunk 6B is not stable. No new TB experiment or pending-charge save was attempted.

This dated audit supersedes earlier *current-state* summaries, not their historical results. The original
preview.175 outcome remains 5 PASS / 1 FAIL. The original preview.176 measurement, package, suite, receipts,
failed FAST attempt and failed TB stage remain unchanged. The earlier Part G design is a hypothesis and
its save-cleanup premise is contradicted by the actual active persistence path.

## Identity and reconciliation

The sole worktree is `C:/Dev/KingmakerMountedCombatLab/repo/KingmakerMountedCombat`, branch
`codex/mounted-combat-phase3f-playable-core`. Intake status was clean. Fetch succeeded without resetting,
rebasing or changing branches; HEAD, upstream and independently read remote were all
`40c40e4510c93c7c41d7656a870ccbffa34dd849`, tree `e9d3d14ec5b87387ace38daef8ac5f3dc25bf9c4`.
The candidate is one 13-file commit above preview.175 source `8f8440231aed34d798f1b9be5115c033ff1a99b1`,
with no documentation file in that commit. This audit's later documentation HEAD is a separate identity.

| Frozen item | Exact identity |
| --- | --- |
| Product | `0.1.0-chunk6b-preview.176` |
| Package | `artifacts/KingmakerMountedCombat-0.1.0-chunk6b-preview.176-chunk6b-charge-u-diagnostic.zip` |
| Package SHA256 | `0d006d8f0096330a0ce7a1ee1fa08255c449ee8484089d220c2c6e4d3a12814a` |
| Manifest SHA256 | `8322afe04eb812ce24c7d03da90eff0eefbd94e3986315816f26e876610a80ca` |
| DLL SHA256 | `d54dc7b6ec62303796890f35502141cd3d2d638b80279f58fc8579170334fdf5` |
| DLL MVID / bytes | `da58e151-48da-48ab-861c-5cb83febb959` / 5,457,920 |
| Suite | `20261005-chunk6b-charge-u` |
| Suite snapshot SHA256 | `cfc9a39eb089b4d603df399ec1bfa30ea0f1bc81b39c348f6d0345af700f7c96` |
| Purity receipt SHA256 | `45cd5ac73c2678f79bff9c684ddf9131b5dd0fff735356ac4dfb4e0f89af0c4c` |
| Purity process / exit | `purity-process-preview176-a.json`, exit 0, completed `2026-10-05T14:35:01.1486390Z` |

Paths beginning `artifacts/`, `runtime-state/`, `runtime-evidence/`, `logs/` or `analysis-cache/` below are
relative to `C:/Dev/KingmakerMountedCombatLab`. Old campaign receipts are under
`analysis-cache/chunk6b-charge/`; new independent evidence is under
`analysis-cache/codex-continuation-176-20261005/`. `intake-git.json` records all requested Git commands.

## Six terminal stages

Every row binds the exact package and suite above. Run IDs have prefix `c6b-charge176-a-` followed by the
suffix shown. Native assertion counts are the compiled structural checks; the external reader is the
acceptance authority. The TB native label and canned positive-row prose do not establish delivery.

| Stage / run suffix | Launcher exit | Native assertions | Overall assertions | External verdict | Restoration |
| --- | ---: | --- | --- | --- | --- |
| C6B-CHARGE-RT / `charge-rt` | 0 | 75/0 | 75/0 | PASS; all 15 registered RT rows | PASS, errors `[]` |
| C6B-CHARGE-TB / `charge-tb` | 1 | 64/0 | 0/1 | FAIL: `Chunk 6B charge: the mount did not carry the charge` | PASS, errors `[]` |
| C6B-PATH-RT / `charge-path-rt` | 0 | 62/0 | 62/0 | PASS | PASS, errors `[]` |
| C6B-PATH-TB / `charge-path-tb` | 0 | 62/0 | 62/0 | PASS | PASS, errors `[]` |
| CHARGE-SAFETY-RT / `charge-safety-rt` | 0 | 66/0 | 66/0 | PASS | PASS, errors `[]` |
| CHARGE-SAFETY-TB / `charge-safety-tb` | 0 | 66/0 | 66/0 | PASS | PASS, errors `[]` |

| Run suffix | SHA256 of its `runtime-request.json` |
| --- | --- |
| `charge-rt` | `ad6f7a56c8d7a0ca99f2f06493df107110385fb06ca72a58154b64d92565b350` |
| `charge-tb` | `1cd9a33a9c53e1ebb1998a3e4340abac3d6f65c8c86a3973e03d090c0ebb4dda` |
| `charge-path-rt` | `f425887f36b0246398e0db845abd818f4e3e6b0ff1a13df5f8a5f71b785b7f54` |
| `charge-path-tb` | `0397e72b239672c76e529c0d3f0931ce10a91dff3b2d8b5965ff246439ca6432` |
| `charge-safety-rt` | `f4a7fddac32ed52ce89f7f449206fdfdf74e35b14ed162808c12c2436927e781` |
| `charge-safety-tb` | `44b43000e7321ffb4c2ed8a67e1d5caa3899484831bcac33dcd2fe5c6d6ee5ee` |

All six run, save and Mods transactions have terminal phase `restored`, all five restoration Booleans true,
and empty `restorationErrors`. Each synchronous launcher returned a terminal process receipt. The game
exited through the harness's observed-exit/stable-absence gate before restoration; the harness records no
native OS exit code, so an OS exit code of zero is **not** inferred. No separate recovery or intervention
was found or performed. Ordinary `finally` restoration quarantined disposable Working-file churn.
The batch completed at `2026-10-05T14:50:38.1175685Z`. No game, build, qualification or installation
transaction was active at takeover. The unrelated pre-existing PowerShell session was left alone.

For **each** stage, before and after:

- Whole Mods content digest: `02b2efc8c35dc0bbd7b041b6fa1f6f9888b51bf0490ab7b9ca34dc0901977dc0`.
- Save metadata inventory digest: `2e69c4a4b4611327b0acb09d002a996e42980ff23f2ce256ac3d7dd064d9c74a`.
- Suite save bytes-and-metadata digest: `511077a04981fe3657d47eb0fc8727f67ea5602f755575c6b4aae1142974a426`;
  content digest: `3fff5894445ce8428449a1e16acec040f6424a4a358e7aba737bb35fcaf43afa`.
- Immutable baseline SHA256: `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`.
- Restored Working SHA256: `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`.
- Human KMC remains **preview.105**: DLL SHA256
  `8e231c388540cee50087ae47a2843bff06c69b6bf668b4a35f0ddfc3844f61a2`, Info.json SHA256
  `917523483b5850ac53ab8bd39ab9a34caaacfa0abfeae64b6d111fcdf7a71476`.

These different digest formats are not interchangeable. The immutable suite contains every individual
save-file hash. Each launcher checks suite continuity before and after its transaction; its receipt does
not duplicate all individual hashes. `initial-audit.json` independently re-read every byte, path and
timestamp now: 275 save files and 358 Mods files equal the frozen inventory. It records individual before
and after hashes, all six receipt chains, and 441 immutable evidence-file hashes; **176 checks PASS / 0 FAIL**.
Its SHA256 is `2a675f45fc41187bbffbc103f0eccbb8beeb9435a4428f327d92f06b76b0b455`.

## Pre-run prediction versus measurement

The original `analysis-cache/chunk6b-charge/PREDICTION-176-TB-STAGE.md` was read and retained. Its cost
prediction was confirmed: positive spent rider Standard **6**, Move **3**, and neither mount resource.
Its sequencing concern was **not** confirmed. The positive charge travelled **0.169029981 m**, exhausted
four bounded repaths/five carrier attempts, and terminated `Interrupt` with **zero** child attacks and
zero charge attack rules. Every carrier reported `ticks=0; moveResult=Interrupt`; the lease restored with
empty cleanup debt. The external reader's rejection is justified by these native measurements.

The subsequent below-minimum and stock-rejected rows both observed the rider's own Preparing turn,
`TimeMoved=0`, Standard=0 and Move=0. Source stage 4 ends the diagnostic encounter and waits for native
controller shutdown before starting the next case. Preserved UMM logs name fresh paired activations
`9f63a216c696438cba02400171d49db9:1` and `456bca308eb84f4b8528a74ca570eb94:1`, each with two native
preparations, after the positive activation `a15458b15bb7493394a601cd7372fb7a:1` ended. They pass independent
row replay. No new End Turn logic, cooldown clear, refund or fixture sequencing change is warranted.

The exact upstream cause of carrier interruption is still unproven. The single authorized Part F
experiment did **not** establish TB support. The experimental source currently permits admission when
enabled; it no longer implements the old blanket TB refusal. Do not describe preview.176 as failing
closed with that historical reason. Default-off remains true; no extension of the TB experiment occurred.

## Part F audit

The actual diff and compiled binary were inspected. Four source contracts exist, but their passing shape
checks alone do not prove runtime lifecycle safety. `Test-FrozenDelegation.ps1` in the new audit directory
additionally invokes the actual frozen permission getter and policies on detached objects: **365/0**.
It tests all 64 combinations of the six live-charge facts, missing lease, all 128 policy-input combinations,
all 32 legacy-overload inputs and all eight relationship-turn inputs, and confirms no command or lease
field changed during a permission read. The pinned native `IsFinished` accessor was verified to be a
single Boolean-field read before invocation. No game, actor, turn, queue or native cleanup method was run.

| Required claim | Audit result and limit |
| --- | --- |
| Exact live charge alone gains Preparing delegation | PASS for the compiled command/policy boundary; controller passes its active attack command's authority and exact rider/mount facts. Stale ownership across failed cleanup is not certified. |
| Ordinary pair commands gain no authority | PASS: ordinary chargeMode=false refuses Preparing delegation; door and legacy paths do not supply charge authority. The pre-existing ground-movement policy is unchanged. |
| Mount/Dismount remain Acting-only in TB | PASS: `CombatMountDismountPolicy.IsTurnEligible` still requires exact rider and Acting; compiled truth table exercised. |
| All six reported live facts required | PASS: charge mode, unfinished command, applied lease, unrestored lease, no application failure, no revalidation failure; null lease also refuses. Actual getter exercised independently for every combination. |
| Narrow disjunct | PASS: prior Acting admission OR Preparing with charge authority, with the original exact-pair/mode/rider/mount gates outside the disjunct. |
| Five-argument overload retains boundary | PASS: forwards false/false; every legacy combination exercised. |
| No fabricated/refunded action state | PASS for reviewed Part F diff and permission reads. The native failed positive retains full rider cost; later debt reset follows native encounter shutdown/preparation. |
| Stale/failed/compensated/interrupted/restored charges cannot exploit seam | PASS when the reported failed/finished/restored facts are set. **TODO for interrupted-cleanup ownership and compensation-debt integration.** The getter is not a proof that those facts are always set or references retained when cleanup throws. |

Part F therefore has a narrow tested predicate, **FAIL native delivery**, and incomplete exceptional
lifecycle proof. It is not accepted functionality and not authority to continue TB implementation.

## Part G / 6B.4 scope and stop boundary

The mission explicitly requires **save during pending charge, cold load, combat end and disable**, on
Chunk 5 machinery. The owner's continuation additionally requires inspection of the boundaries below.
The earlier seven-case draft does not itself qualify them or prove that new persistence cases are
registered. No charge persistence case exists in the current launcher/protocol lists.

| Boundary | Source path and current evidence | Remaining obligation |
| --- | --- | --- |
| Rider incapacity | Lifecycle -> Dismount(Incapacitated) -> combat.Cancel; RT row accepted | Exception/debt and scheduler postconditions remain unproven. |
| Mount incapacity | Same path; RT row accepted | Same; native movement after dissolution is not itself proof of a KMC carrier. |
| Relationship invalidation / view loss | ValidateActivePair -> Dismount; revalidation checks exact pair | Dedicated in-flight invalidation and failed-cleanup proof. |
| Voluntary dismount | Existing Acting-turn native transition; Dismounting subscriber cancels combat | In-flight charge case and ordinary transition regression. |
| Safe target loss | Command target validation/revalidation -> Interrupt; RT row accepted, shared target service retained | General faulted-cleanup guarantees still required. |
| Combat end / mode change | Lifecycle calls combat.Cancel; combat-ended RT row accepted | Mode-change charge row and complete ownership postconditions. |
| Area transition | Armed transfer uses SuspendAreaPair -> GuardBoundary(AreaSuspension); fallback AreaUnloading cleanup | Pending-charge transfer, post-placement and failure/recovery evidence. |
| Save | Active persistence SavePrefix returns before legacy GuardNativeBoundary; header calls Capture before control suspension | **BLOCKED — CRITICAL: no proven pre-serialization charge cancellation/postcondition barrier.** |
| Load / world replacement | BeginLoadHousekeeping -> GuardBoundary(LoadRequested), then restoration | Cold proof that a saved native buff/command/path cannot resurrect. |
| Feature disable | Admission reads EnableMountedCharge; running revalidation has no feature-enabled fact | Active-charge disable cancellation is not established. |
| Mod disable / unload | Composition refuses in-flight save/load, then lifecycle.HandleModDisable -> Dismount | Charge-specific cleanup must propagate failure and retain debt before disposal. |
| Pre-commit cancellation / in-flight interruption | Both respective RT rows accepted | They do not fault-inject ordinary cancellation's ownership releases. |
| Interrupted cleanup / recovery | Admission compensation has postconditions and a faulted owner; ordinary Cancel follows another path | Durable owner, retry/drain and save/disable admission must be proven together. |

Concrete source findings on `40c40e4`:

1. `MountedPatchController.SavePrefix` at line 896 returns immediately for an authorized save when
   `PatchBridge.Persistence != null`. The later legacy `GuardNativeBoundary(SaveRequest, ...)` is not that
   active path. `MountedPersistenceService.BeforeNativeHeader` at line 538 raises `SaveSnapshotStarting`,
   captures mounted metadata, then suspends controls. There is **no production subscriber** to that event;
   the only subscription is a diagnostic condition-preparation fixture. Removing KMC control facts does
   not prove cancellation/restoration of the separate charge command, lease or native Charge buff.
2. The custom `MountedSaveData` has no charge transaction fields. That is a useful schema fact, **not**
   proof about the engine's native actor/buff/command serialization or a cold load. The actual barrier
   ordering and exclusion of all live charge-owned state still require exact contracts and native proof.
3. `MountedCombatController.Cancel` at line 844 sets `activeCommand=null` **before** scheduler interruption
   and `command.Interrupt()`. An exception can bypass the remaining cleanup. `HandleCommandTerminal`
   clears active ownership and places the command in `finishedCommandPendingSweep`; its sweep can clear
   that reference based on container absence without testing `ChargeCleanupComplete` or debt.
4. `MountedPairAttackCommand.OnEnded` attempts lease restoration and logs retained debt, but
   `TryDischargeChargeCleanupDebt` has no caller. `faultedChargeCleanupOwner` is assigned only by admission
   compensation; there is no retry/drain consumer. `GameMountedRelationshipService.Dismount` catches
   subscriber exceptions and proceeds with relationship cleanup. A relationship cleanup PASS is therefore
   insufficient to establish all charge postconditions during a fault.
5. `StopDelegatedMove` performs native interruption/removal and records slot restoration. It can throw;
   ordinary lifecycle cancellation has no equivalent of admission compensation's independent steps and
   postcondition confirmation. A cleared field or a logged cleanup attempt cannot replace ownership proof.

The lease itself uses a postcondition-based retryable ledger; preserve it. This audit does not recommend
rewriting the architecture. The missing connection is between ordinary terminal/lifecycle cleanup,
retained native ownership/debt and the persistence barrier. The owner's instruction to **stop at an
unproven persistence boundary** applies here, before any native pending-charge write, cold load, source
repair that assumes the draft's ordering, or new candidate cycle. Existing tests passing does not remove it.

For every unmeasured boundary, the following remain TODO together: terminal exact charge command;
stopped/removed delegated carrier; free mount Move slot; no stale rider Standard slot; no scheduler
registration; every owned lease mutation restored; no owned forced path/speed/charging/buff residue;
retained active/faulted owner until postconditions hold; no lost compensation debt; no serialized/resumed
charge; no action refund/preparation replay; ordinary attacks and transitions unchanged. Native force-mode
latch semantics remain the existing measured contract: releasing the owned path is distinct from clearing
an engine latch, which the next lawful path must resolve. No threshold is weakened.

## Verification and retained failures

| Check | Result |
| --- | --- |
| Frozen component executable, rerun | 618/0 |
| Frozen charge-reader synthetic suite, rerun | 323/0 |
| Source contracts, rerun before documentation edits | 143/0 |
| Actual compiled delegation/transition predicates plus field residue | 365/0, detached only |
| Existing exact persistence assembly/storage contracts | 180/0, not pending-charge qualification |
| Independent immutable/restoration audit | 176/0; 441 historical files inventoried |
| Unchanged reader replay | Original 5 PASS / 1 FAIL reproduced; RT rows 15/0, TB rows 3/1 |
| Original FAST attempt 1 | FAIL retained, exit 1: synthetic TB positive still forced unavailable |
| Original FAST attempt 2 | 15/0, exit 0; original log hash independently matched |
| Original CANDIDATE at `40c40e4` | 26/0, exit 0; original log hash independently matched |
| Original WhatIf purity | PASS, exit 0; distinct from the native campaign |

No FAST/CANDIDATE/package/purity cycle was repeated: there is no new product candidate. The exact earlier
FAST failure remains `fast-tier-preview176-1-receipt.json` and `logs/preview176-fast-1.log` (log SHA256
`f0f9a9d1848d47b788952ebfe7eec78dec35def2806d6bcf051d76e8bf8064fc`). Preview.175's RT failure and full
5/1 campaign remain in its original outcome, receipts and session logs. Earlier preview.173/.174 failures
remain recorded below the historical mission entries; no historical verdict was relabeled.

New audit-only unsuccessful invocations are retained too: initial fetch/process reads needed sandbox
escalation; the first reader/source invocations were refused by the process's PowerShell execution policy
(`verification-exits.json`, original logs). Explicit process-local script invocation then passed in
separate `*-attempt2` logs. The independent audit's first executable draft looked up ZIP names using
forward slashes, while the frozen archive uses backslashes; the failing draft is retained as
`Audit-Preview176-attempt2.ps1`. Only that new audit script was corrected to normalize entry names.
These are audit orchestration failures, not product or runtime failures.

## Handoff and next safe step

Only documentation changes belong to this repository checkpoint. Product, fixture, reader, tests,
version and every frozen receipt remain unchanged. All new probes and results are lab-local. No merge,
release, tag, PR, permanent install, protected-save write, guard/threshold change, HUMAN PLAY authority,
Chunk 6B.5 or unrelated future work occurred. The 15 registered RT rows are not a claim that every mission
row is complete: maximum-range and duplicate-request coverage, the wider TB matrix and 6B.4 remain open.

The final lab handoff is
`analysis-cache/codex-continuation-176-20261005/CODEX-CHUNK6B-PREVIEW-176-CONTINUATION-HANDOFF-2026-10-05.md`.
It records the final documentation HEAD/tree, remote equality, clean worktree, current process check and
final hash audit. Product receipts must continue to name `40c40e4`, never the documentation descendant.

Exact next safe command, from this worktree (read-only):

```powershell
rg -n 'SavePrefix|BeforeNativeHeader|SaveSnapshotStarting|TryDischargeChargeCleanupDebt|faultedChargeCleanupOwner' src/KingmakerMountedCombat/Integration
```

Next mission step upon resumption: resolve the documented persistence stop boundary by specifying one
synchronous charge-ownership barrier and a durable cleanup-debt owner using the existing transaction and
postcondition model. Establish the exact native save/cancellation ordering and behavior-focused fault
coverage before attempting a pending-charge save. The failed TB experiment needs an explicit disposition;
do not silently extend it, rerun preview.176, refund its cost, or insert an unnecessary fresh-turn repair.
