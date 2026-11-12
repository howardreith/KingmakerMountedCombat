# Attack completion warnings — source fix, native confirmation pending (2026-11-12)

Branch: `codex/kmc-attack-completion-warnings`.
Base: main `680d02cb2cae62bbf646f2064b42014d145b3e71` (0.2.5, including PR #31).
Scope: the owner's report of a no-Standard warning after Royon's full attack, and related ordinary attack warnings.

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
