# Attack completion warnings — source fix refined, Fast PASS on genuine references, native BLOCKED (2026-11-12)

Branch: `codex/kmc-attack-completion-warnings`, draft PR #33.
Base: main `680d02cb2cae62bbf646f2064b42014d145b3e71` (0.2.5, including PR #31).
Scope: the owner's report of a no-Standard warning after Royon's full attack, and related ordinary attack warnings.

## Continuation on the lab machine: review correction and production build

Continued from the reviewed handoff commit `421cd82e` (still the PR head; PR open, not merged). No newer remote work existed.

**Review finding (corrected).** The first correction quieted *every* later dispatch refusal once the intent had
dispatched. Since `HandleCommandTerminal` retains the intent only after `Success` or native ranged-tail termination
(any failure retires it), a later dispatch is an RT repetition, the ranged-tail continuation, or the explicit legacy
unified TB alternation. Its refusal may be a routine reassessment, but `TryExecuteAction` has three
`CommandAdmissionFailure` paths that log nothing: scheduler registration, native command-container admission and
scheduler confirmation. For those the banner was the only visible signal, so a real admission fault in a
continuation would have left no trace.

**Refinement.**
- `UnifiedMountedStockAttackPolicy.IsRoutineContinuationRefusal(codes)` treats only re-read state as routine: turn,
  action, selection, relationship, combat, life state, target, path, range and lifecycle codes. A fault or configuration
  code (`CommandAdmissionFailure`, `RangeOriginMismatch`, `FeatureDisabled`, `BodyProfileUnsupported`,
  `NoEligibleWeapon`, `UnsupportedWeaponCategory`, `MountedRangedUnsupported`), an unknown future code or no code at
  all is not routine.
- `MountedAttackIntent.GetDispatchRejectionFeedback(feedback, codes)` answers a player request that never dispatched.
  After accepted work, a later refusal is quiet only when it is routine. A fault or configuration refusal still
  answers. The rejection retires the intent, so that answer appears at most once. Automatic intents never answer the
  player, which is unchanged.
- The controller logs every stock dispatch rejection, automatic or player, with action, generation, codes, the
  presentation decision and the feedback text. This is read before the intent is cleared.
- `GetRejectionFeedback` (exhausted TB intent) is unchanged: exhaustion after accepted dispatch is routine by
  definition.

Native budgets, ownership, eligibility, attack counts, movement, turns, command admission and the paired-turn
configuration are unchanged. No action is cleared, refunded, synthesized or replayed.

**Tests.** `RealTimeContinuationFeedback` now drives the API the controller calls, using eight refusals produced by
the actual `MountedCombatActionEvaluator`. Each is quiet after dispatch and answered for a new request. The new
`LaterDispatchFaultFeedback` covers six fault/configuration refusals from the real evaluator, a mixed routine+fault
refusal, the code-only admission failure and an uncoded refusal. It checks that each still answers after dispatch, is
quiet for an automatic intent and is quiet once retired. `MountedCombatDomainTests.Eligible` became `internal` for
reuse.

**Mutation check.** With the dispatch API temporarily reverted to the blanket rule (test project only, direct MSBuild,
source restored and verified), the new test failed and nothing else did: `1167 PASS / 1 FAIL`.

**Checks run (budget `attack-completion-warnings`, 1.25 of 90 min).** Both used `scripts/Test-Tiers.ps1 -Tier Fast`
with MSBuild 17.14 against the installed Kingmaker `Assembly-CSharp.dll` (`3b6450ff…`) and UMM `0Harmony12.dll`
(`aa1cd483…`), via `LocalGamePaths.props`. This is a net47, C# 7.3 production build.

| Input | Build | Components | Validate-Source | Tier |
| --- | --- | --- | --- | --- |
| Unchanged PR head `421cd82e` | PASS 27.5 s | 1167/0, 1.7 s | 151/0, 6.4 s | PASS 3/0, 0.6 min |
| Refinement (this commit) | PASS 30.8 s | 1168/0, 1.7 s | 151/0, 6.3 s | PASS 3/0, 0.65 min |

The refined build's `bin/Release/KingmakerMountedCombat.dll` is SHA-256
`4c60638b175c06e3c6dbdb031afce85715c8ba600750b35fda248e93d9cfae52`, MVID `fcbb4221-85e2-4ba7-adb5-21968daecc9d`. It is a
local build artifact, not packaged or installed. The impact map selected no further family. `Test-AssemblyContracts.ps1`
was not added because no native member, token or contract changed.

**Native: BLOCKED, NOT RUN.** This machine has the game, but:
- no KMC build is installed (`Mods` holds only other projects' mods);
- no `KMC_AUTOMATION_BASELINE`/`KMC_AUTOMATION_WORKING` archive is among the 95 saves, which `Get-KmcValidatedFixturePair`
  requires;
- no `runtime-state\fixture-qualification.json` exists;
- no KMC line or owner log exists (the local `output_log.txt` has no KMC entries).

The KBP/KMG fixtures belong to other projects and are not admissible. No staging, deployment, launch, lock or save
access happened, so there was nothing to restore. The native cause therefore remains established from source only.

**Residual (not changed).** In a still-active paired TB turn, a same-target click that lands after the command's
terminal but before the next `Update` retires the intent would be treated as continuation and stay quiet. The drive
runs at the end of every `Update`, so the window is about one frame. Native evidence could confirm it.

## Original offline checkpoint (2026-11-12, kept)

## Cause and action economy

The matching text is KMC's `UnifiedMountedStockAttackPolicy.NoStandardFeedback`.
`HandleCommandTerminal` releases `stockIntentCommand` after a successful attack but retains the higher-level intent.
On a later update in the same native turn, `DecideNext` correctly returns `CompleteTurnBasedIntent` because the
requested actor's Standard is spent. The previous `stockIntentCommand == null` feedback condition then mistook
completed work for a request that had never dispatched. The warning was emitted without dispatching another attack.
Target invalidation or the turn ending first takes another path, explaining why the banner need not appear every time.

This applies to the shared completion path for rider and mount attacks, melee/ranged and single/full routines,
including the retained intent after native ranged-tail termination. These are source conclusions, not new game runs.

The inspected action paths retain native ownership: the actor's own queue runs its attack, the native command spends
that actor's resources, and the mount owns delegated movement. `MountedPairAttackCommand.ChildAttack` is the command
itself, not a second attack spending alongside it. Ordinary paired TB rider intent does not automatically spend the
mount's Standard; the explicit legacy unified setting has its existing separate routing. No additional double-spend
defect was established. The recorded native contracts remain in `planning/ASSEMBLY-CONTRACT-MATRIX.md` and the accepted
movement/full-attack rules in `docs/FINALIZE-F3-RULES-COMPARISON.md`.

## Correction

`MountedAttackIntent` now remembers that a command was admitted for its current generation. The existing
`ObserveDispatch` records it; `Begin` and `Cancel` reset it. Terminal command-reference cleanup and same-target
continuation/promotion do not reset it.

`GetRejectionFeedback` returns a warning only for a live player request that has not dispatched. The controller uses
it for both exhausted TB intent and later dispatch rejection, taking the decision before clearing the intent.
Thus completed work and background continuation stop quietly. A fresh unavailable command still explains its refusal,
including a new click after completion, a replacement target/action, or the first delayed dispatch of a paused order.
Automatic proposals remain quiet. Existing logs and late-failure feedback state remain available.

The remaining warning producers were inspected: actual invalid selection/turn clicks, Smite range, Delay, Charge,
explicit control delivery, and save/load/area admission retain their current warnings. There is no global warning
filter. The owner's other unspecified banners have not all been reproduced or identified.

No native spending, eligibility, movement, turn, attack count, command admission, persistence schema, package version,
or dependency changed. No release, tag, merge, installation, game launch, or personal save access was performed.

## Focused verification

The selected domain suite compiles actual repository production source and existing tests with C# 7.3,
warnings as errors, under the official .NET SDK 8.0.100 in the scratch workspace. It links these four test groups:

- `MountedStockAttackPolicyTests` (including nine new feedback regressions).
- `MountedAutomaticAttackPolicyTests`.
- `MountedPlayerIntentBoundaryTests`.
- `UnifiedMountedTurnPolicyTests`.

Clean baseline: **83 passed / 0 failed**. Changed source: **92 passed / 0 failed**, exit 0, **2.683 seconds** including
incremental compilation and execution. No compiler warnings or errors. Independent source review found no blocking
defects in production or the nine regression cases; that review did not claim a separate game/test execution.

The new cases cover both actors with the partner still ready, genuine fresh spent-action clicks, same-target repeats,
Stop/new click, retarget/action replacement, automatic proposals, promotion before/after dispatch, separate pairs, and
later real-time admission failure. They assert returned feedback and public dispatch decisions, not the private marker.

Exact command (temporary harness; no repository dependency added):

```bash
DOTNET_CLI_TELEMETRY_OPTOUT=1 \
/workspace/scratch/5f43da885da0/tooling/dotnet/dotnet run \
  --project /workspace/scratch/5f43da885da0/tooling/mounted-focused/MountedFocused.csproj \
  --configuration Release --no-restore
```

Temporary evidence is under `tooling/mounted-focused/` in that workspace: `baseline-snapshot/` preserves the clean
source/binaries/log and hashes; `attack-feedback-final.log` and `attack-feedback-final.json` hold the changed run.
The harness links production files directly and calls the four existing `Register` methods. The added tests also remain
part of the normal repository test project through the already included `MountedStockAttackPolicyTests.cs`.

Changed-source SHA-256 identities for that run:

| File | SHA-256 |
| --- | --- |
| `Domain/MountedStockAttack.cs` | `c5ba8eda79e86852e008b0a75b13cf13b0164e312cfdef5a6d92fa22c273d921` |
| `Integration/MountedCombatController.cs` | `97d6abfeb76f3ec09e87369a01f8fee14e9d734d8e9da58d47e78a8dd24ced9c` |
| `tests/MountedStockAttackPolicyTests.cs` | `9de957416ea54989414db12caafa79090c14a7725f627a142f72e0dc4c4a22fe` |

The first two paths are under `src/KingmakerMountedCombat/`. These are source hashes, not a DLL identity.

## Remaining evidence

DATA was offline. This environment has no genuine Kingmaker/UMM references or game fixture. The focused run is
**domain behavior on .NET 8**, not a net47 production DLL build, a Fast/Candidate qualification, or Unity/Harmony/native
UI evidence. The integration-controller edits were inspected but not compiled against the game here. No change to
native budgets is claimed from offline tests. No installed-build identity was established.

When a native check is separately available, use the repository's guarded workflow and actual current installation
snapshot. The affected acceptance is a surviving-target rider full/single attack and mount attack, then a deliberately
new spent-action click, in both initiative arrangements; confirm quiet completion, a useful fresh refusal, unchanged
native costs, and an unmounted control. Also observe an RT continuation and replacement. No new broad campaign is
selected by this handoff. Unrelated 0.2.5 save issues and previously recorded test failures remain outside this fix.
