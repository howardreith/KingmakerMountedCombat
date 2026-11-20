# Tabletop Rules Expansion startup conflict — repair handoff (2026-11-20)

Release-blocking compatibility defect in **0.2.12**: with Mr. Bard's Tabletop Rules Expansion (mod id
`KingmakerGunslinger`) **0.0.191** installed, Mounted Combat fails to load, and a save that references Mounted
Combat content then fails to load. Branch `codex/gunslinger-click-approach-compat`, from `origin/main` =
`12527350` (the tagged `v0.2.12` merge). PR #55, merged into main as `a4d25e45`; released in 0.2.13 by owner order
([notes](RELEASE-NOTES-0.2.13.md)). Not installed.

## Incident

Owner log `output_log(20261120-043652).txt` (kept in the lab root, not in the repository). Pathfinder: Kingmaker
2.1.7b, Unity Mod Manager 0.33.0, 28 mods.

1. UMM loads mods in id order. `KingmakerGunslinger` 0.0.191 loads first and reports `Active`.
2. `[KingmakerMountedCombat] Version '0.2.12'. Loading.` then `InvalidOperationException: Cannot-attack click approach
   structural contract mismatch: one UnitCommands.Run of the click command's local after the construction`, from
   `MountedClickApproachTranspiler.CannotAttackApproachRun` inside `Harmony12.PatchProcessor` under
   `MountedPatchController..ctor`, `CompositionRoot..ctor`, `Main.Load`. Module `e64656cd6fe3422780bf6a7cb87c3477`
   is the published 0.2.12 DLL's MVID. The constructor's catch unpatches everything; `Not loaded.`
3. Loading `Quicksave2` then fails: `Failed to load blueprint by guid 3c81f5a02be74d96a4e07d1c92b6f385` while
   deserializing an `ActivatableAbility`, then `NullReferenceException` in `Player.PostLoad_Patch2`, rethrown as
   `LoadGameException`. That GUID is `MountedCombatToggleGuid`
   (`src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.MountedCombatFeat.cs`). The blueprint is
   published only by a loaded Mounted Combat, so the save's reference is missing for that session. The save was not
   available here; the exact final null expression was not established.

## Cause (reproduced with both released binaries)

- 0.2.12 (`cdfbe509`, the Sprint 3 closeout's M04 repair) added a transpiler on `ClickUnitHandler.OnClick`
  (060093ED) that **replaced** the one `callvirt UnitCommands.Run(UnitCommand)` submitting the click command's local
  (local 7: the cast, attack or cannot-attack `UnitMoveTo`) with a call to its own hook. 0.2.11 patched `OnClick`
  with a prefix only, which is why the two mods coexisted before 0.2.12.
- Tabletop Rules Expansion 0.0.191 `NativeFirearmAttackOrderPatch.DesktopBody` (release commit `3e13965d`) rewrites
  **all three** of that method's `callvirt Run` into `ldloc invocation; call NativeFirearmAttackOrder.Submit(UnitCommands,
  UnitCommand, InputInvocation)` and throws unless it rewrote exactly three.
- Whichever transpiler runs second fails. Reproduced through the installed Harmony12 shim with the released DLLs
  (Mounted Combat SHA-256 `a85844951fcf26e6afb7bc2b523ebfe5da5402c6e3e40ccc9b5f1ce6dfb58857`, MVID
  `e64656cd-6fe3-4227-80bf-6a7cb87c3477`; Tabletop Rules Expansion DLL SHA-256
  `7c3b0324f40e32b0a476c24e9e66a63a2ecefd8908dc3902bed8ec81a5d87287`, MVID `cf7f9608-c42f-47bb-8487-13d4d230136e`,
  both equal to their release manifests): other mod first → Mounted Combat throws the incident's exception; Mounted
  Combat first → the other mod throws `Native desktop firearm order callsites changed.` Reversing the order only
  moves the failure.
- No other method is transpiled by both mods (source inventory of 0.0.191's production transpilers against Mounted
  Combat's 60 transpiled targets). Prefix/postfix overlaps cannot fail a structural check and predate 0.2.12.

## Repair (Mounted Combat only; no companion change needed)

`MountedClickApproachTranspiler.CannotAttackApproachRun` no longer replaces the submission. It inserts a guard in
front of it and leaves the submission itself untouched:

```
dup; ldloc command; call PatchMethods.ClickApproachRouted; brtrue skip
<submission unchanged>                 // native: ldloc command; callvirt Run
                                       // or:     ldloc command; ldloc invocation; call NativeFirearmAttackOrder.Submit
br join; skip: pop; join: nop
```

- `ClickApproachRouted(UnitCommands, UnitCommand) : bool` runs the unchanged `RouteRiderClickApproach` inside the same
  catch as before. True (skip the submission) when the cannot-attack approach of a mounted rider was given to the
  mount through the ground-order lifecycle, refused by its admission, or a fault came after the command was already
  given to another unit. Otherwise false, and the method's own submission runs exactly as it stands. With the other
  mod absent this is the 0.2.12 behaviour instruction for instruction (native `Run` on the same container).
- **Both submission shapes are recognised exactly**: native `call/callvirt UnitCommands.Run(UnitCommand)`, or a
  `ldloc` of a local whose type is `KingmakerGunslinger.Firing.NativeFirearmAttackOrder+InputInvocation` followed by a
  static void `NativeFirearmAttackOrder.Submit(UnitCommands, UnitCommand, that type)`. Exactly one such submission of
  the click command's local after the `UnitMoveTo` construction is required, with no exception boundary or branch
  target inside it. The other mod's three Submits, its invocation local, its input guard and its cleanup are never
  touched. Because Mounted Combat no longer consumes a native `Run`, the other mod still finds its three in either
  order, and both orders produce the same composed method.
- **Why the other mod's behaviour is preserved.** The router can only act on an exact `UnitMoveTo` of a mounted rider
  outside simulation (`MountedDestinationMarkerPolicy.RoutesRiderClickApproach`). For such a command the other mod's
  `Submit` finds no proposal binding (its bindings are made for `UnitAttack`, auto-use casts, reloads and simulated
  previews) and runs `commands.Run(command)`; the router's native `Run` on the mount (or rider) is the same
  submission that `Submit` would have made, with the other mod's global `Run` prefix/postfix still applying. Firearm
  attacks, auto-use casts, simulated input, interaction and looting never reach the router's skip branch. Native
  `simulate=true` comes only from `PointerController.SimulateClick`, which also sets `SimulatingClick` (whole-assembly
  scan), so the two mods' simulation signals agree.
- **Diagnostics.** A shape mismatch names the failed expectation, how many submissions it found and what each later
  use of the command local is. `PatchExact` now wraps any Harmony failure with the method, token, Harmony id and hook,
  and the owners and transpilers already recorded on the method (the failed patch itself is never recorded).
- `PatchUnitClick()` holds the `OnClick` registration so the regression runs the production registration itself.
- **Source guard (owner review requested).** `Validate-Source.ps1`'s "independent namespace and persistence identity"
  rule forbids the text `KingmakerGunslinger` anywhere in production source. Exact recognition needs the other mod's
  type name, so the rule now excludes exactly one line, the constant
  `internal const string GunslingerOrderType = "KingmakerGunslinger.Firing.NativeFirearmAttackOrder";` in the
  recognizer, and a new assertion requires that line to occur once, in that file only. Every other mention anywhere in
  production source still fails. No project reference, namespace, `using`, blueprint, GUID or save identity changes.
- Blueprint GUIDs, save format and initialization order are unchanged; nothing was moved to hide a failed start.

## Evidence

Tested product commits: `98bfdb7e` (Release DLL SHA-256
`7fbe57217c41e0464a610774e81b4f515f507e8e4bb9b25ce1536710970fb387`, MVID `1c150015-1fc0-4fcb-a226-cd031a4dfe8c`), then
the review corrections `80dc8f78` (Release DLL SHA-256 `5d79c78cae5c27708c3102f1618a3d22ef8a21f89f793ea171979280c84fd647`,
MVID `15d95e93-9806-4dde-a222-7581f38d4ad9`).
Installed environment: Assembly-CSharp SHA-256 `3b6450ff…5afb` (2.1.7b), UMM 0.33.0 (`63e5baf7…2691`), 0Harmony12
1.2.0.1 (`aa1cd483…1292`), 0Harmony 2.3.6 (`df043f05…3e80`).

### New regression: `scripts/Test-GunslingerClickComposition.ps1`

Real production transpilers of both mods, registered through the installed Harmony12 → HarmonyLib pipeline
(`PatchProcessor`, `UpdateWrapper`, patch sorting, `MethodPatcher.CreateReplacement`, shared patch state that rebuilds
the combined chain on every registration). Mounted Combat registers through its own `PatchUnitClick`; the other mod's
`Desktop` is registered exactly as its `Install` does (default `HarmonyMethod`, owner `KingmakerGunslinger`). One step
is replaced: `HarmonyLib.PatchTools.DetourMethod` for `OnClick` only, because .NET Framework cannot JIT the original
(it calls the Unity InternalCall `Component.get_transform`; established with a link-substitution experiment).

- **Composition (6 checks; listings compared with branch targets numbered by first appearance and exception blocks
  included):** Mounted Combat alone; the other mod alone; other mod first (chain other → Mounted
  Combat); Mounted Combat first (chain Mounted Combat → other); the two combined methods equal instruction for
  instruction; an unknown rewrite by a probe owner fails Mounted Combat's registration with the expectation, the
  later uses of the local, the method and the owner `probe.foreign` named.
- **Executed (15 checks, including the reverse order's own slice):** the click command's guarded submission slice of the actual composed method is emitted
  and run with native command objects, the real router and routing body and the other mod's real `Submit`. Boundaries
  substituted: native `Run(UnitCommand)` records instead of running; `GroundCommandPrefix/Postfix` (unchanged by this
  repair) send the rider's order to its pair's mount; `Game.Instance` is null; units, containers and the relationship
  world are uninitialised objects with only the fields read. Covered: mounted approach run once by the correct mount
  with the original command object and no second submission; two independent pairs; Mounted Combat alone; unmounted
  pair, unit outside every pair and no world (one `Submit`, one native run on the clicking unit); a mounted rider's
  `UnitAttack` and `UnitUseAbility` reach `Submit` with the same container, command and invocation; simulated input
  (preview marked by the other mod); null command; a fault after hand-off (no second submission, admission
  completed); a fault before hand-off (original submission once); a command already given to another unit; an
  unrouted submission that throws. Every check states whether the routing hook faulted, so no outcome can pass
  through the fault fallback.
- **Before/after.** Released 0.2.12 DLL: PASS=2 FAIL=5, with the incident's exception (other mod first) and the other
  mod's own guard failure (Mounted Combat first). Repaired build: PASS=21 FAIL=0 (`80dc8f78`; 20/0 at `98bfdb7e` before the review added the reverse-order slice).
- The repaired DLL also composes with the installed Tabletop Rules Expansion **0.0.140** (same submission shape) in
  both orders (scratch reproduction, not part of the regression).

### Existing checks

- `Test-MarkerOwnershipContracts.ps1` updated to the guard contract (C3 now requires the exact seven-instruction
  guard and an unchanged method otherwise; C4 requires the hook to submit nothing itself): PASS=18 FAIL=0.
- `Test-Tiers.ps1 -Tier Fast -Focused Test-GunslingerClickComposition.ps1,Test-MarkerOwnershipContracts.ps1`
  (budget `gunslinger-click-compat`): PASS 5/0 in 0.37 min — build, components 1411/0, Validate-Source 152/0,
  composition 20/0, marker contracts 18/0.

### Runtime (first session: refused before launch)

- Package (normal process, `scripts/Package.ps1 -ArtifactQualifier gunslinger-click-compat`, from clean `6bfd8846`):
  `artifacts/KingmakerMountedCombat-0.2.12-gunslinger-click-compat-diagnostic.zip`, SHA-256
  `c4b9de6624e2ffdf552462073195d8eadeeba47e7be12e2a1773cf3894d2cec3`, DLL `5d79c78c…d647`, MVID
  `15d95e93-9806-4dde-a222-7581f38d4ad9`; package validator 11/0. The version label stays 0.2.12: identify it by
  hash, never by label.
- The development-scoped WhatIf stopped at `Archive root is missing: C:\Dev\KingmakerMountedCombatLab\runtime-backups`.
  Nothing was staged, installed or launched.

### Native checks (continuation after review, 2026-11-20)

**Lab prerequisites.** The configured lab root is `C:\Dev\KingmakerMountedCombatLab` (`environment-intake.json`).
`runtime-backups` and `runtime-staging` were absent; `runtime-evidence` existed and was empty; `runtime-state` held only
`workflow-budgets` (no `active-transaction.lock` and no `transactions`, `run-transactions` or `deployment-operations`
records); no Kingmaker process was running; the live Mods tree had no Mounted Combat folder. No workflow budget since
2026-11-12 records a native run. This reads as a lab whose run archives were cleared, not an interrupted transaction:
there was nothing to recover. The repository has no setup script for these roots (the launcher only refuses when they
are missing), so the two empty directories were created by hand, as review permitted. Nothing else was created: no
recovery record, receipt, pin or evidence.

**Artifact identity.** The reviewed zip (`c4b9de66…`) was verified: DLL `5d79c78c…d647`, Info.json `4e1772fb…`. The
launcher binds a package manifest to the current branch and HEAD, and native runs to a `codex/mounted-combat-*` branch,
so the same commit `78774ab0` (two documentation commits after the reviewed `6bfd8846`; no source change) was packaged
again on a local alias branch `codex/mounted-combat-gunslinger-click-compat-native` with qualifier
`gunslinger-click-compat-native-run`: zip `3539b1cec6c50bc0c5cc8060ef627453c905cb0b011c3688b9c9f2c2f699849e`, **DLL
`5d79c78cae5c27708c3102f1618a3d22ef8a21f89f793ea171979280c84fd647`, MVID `15d95e93-9806-4dde-a222-7581f38d4ad9`**
(the build is deterministic), Info.json `4e1772fb…`. The tested DLL is the reviewed DLL.

**WhatIf.** Full (not development-scoped) WhatIf, Windows PowerShell 5.1, budget `gunslinger-click-compat`: source
153/0, package 11/0, `Runtime WhatIf purity PASS`.

**Run `gunslinger-click-compat-smoke-140b` (`mod-load-smoke`, fresh process, live mod clone plus Mounted Combat).** This
run uses the **installed Tabletop Rules Expansion 0.0.140** (DLL `00ef6757…`), not the incident's 0.0.191. Evidence:
`runtime-evidence/gunslinger-click-compat-smoke-140b/`; the game's log copied to
`analysis-cache/gunslinger-click-compat-native/smoke-140b/output_log.txt` (SHA-256 `41bbcec0…5d2e10`).

- Game-side result (`runtime-game-result.json`, written in the native process): `status=PASS`, product 0.2.12, DLL
  `5d79c78c…`, MVID `15d95e93…`, Kingmaker 2.1.7b, Assembly-CSharp `3b6450ff…`, Harmony12 1.2.0.1.
- Loader inventory (captured in the native process): all 17 mods `active=true`, including `KingmakerGunslinger` and
  `KingmakerMountedCombat`.
- Log: `[KingmakerGunslinger] Active.`; `[KingmakerMountedCombat] Active.` (no `Not loaded`); `Mounted Combat feat
  selections: …; failure=none; Mounted Archery offered; failure=none.` No structural-contract exception, no `Harmony
  could not patch`, no Mounted Combat exception.
- Toggle `3c81f5a0…`: its registration is part of the step that logged `failure=none` (any failure there removes it).
  No direct blueprint lookup by GUID was observed.
- **Harness verdict: FAIL**, solely `Runtime game-result platform identity is not exact.`: the game reported UMM
  `0.33.0.0` (`63e5baf7…`), while `planning/ENVIRONMENT-FINGERPRINT.json` and `Test-RuntimeGameResult.ps1` pin UMM
  `0.33.2.0` (`f6f7e18a…`). The machine's UMM differs from the repository's pinned environment (the incident log also
  shows 0.33.0). The pin and validator were not changed; whether to re-pin is an owner decision. Every other identity
  field matched.
- Restoration: `modsRestored=true`, `saveProtectionPassed=true`, `workingRestored=true`; Mods digest `51a2e474…`
  before = after; save inventory digest `6a8a8ad1…` before = after. Checked afterwards: live Mods has no Mounted
  Combat folder, the installed Tabletop Rules Expansion DLL is still `00ef6757…`, no game process, no lock.
- An earlier attempt (`gunslinger-click-compat-smoke-140`, from the PR branch) was refused before launch (`Runtime
  request branch is outside the KMC prefix.`; `launchIssued=false`) and restored identically.

**Tabletop Rules Expansion 0.0.191 natively: not run.** The launcher stages a specific foreign build only through a
retained-dependency selection, which admits a producer run's staged folder or the one owner-pinned F1 input. The
verified 0.0.191 release was extracted unchanged (zip `c09ca625…`, DLL `7c3b0324…`, 458 files) to
`analysis-cache/gunslinger-click-compat-native/tre-0.0.191/KingmakerGunslinger`, but adding an exactly pinned X01 input
to that guard (same pattern as F1) was blocked in this session as a change to a safety guard and was not made. The
installed 0.0.140 was not replaced.

**Save recovery and gameplay: not run.** `Quicksave2` is not on this machine; the local saves are protected human saves
and no qualified disposable fixture with a mounted pair and a firearm exists here.

### Independent review

One independent review of `98bfdb7e` (fresh reviewer given the code and both mods' sources): no blocking findings.
Corrected in `80dc8f78`: the patch-failure message now names every hook; `Validate-Source.ps1` also confines the
constant's symbols to the recognizer, which resolves no foreign type; composed-IL comparisons now include branch wiring
and blocks, and the reverse order's slice is executed. Recorded, not changed (pre-existing in 0.2.12, unchanged by this
repair): if `GroundCommandPostfix` itself throws after the rider kept its own order, or after a refused admission,
the routing hook's fault fallback lets the method's own submission run, so that command may be submitted twice or
despite the refusal. Low likelihood. `docs/KNOWN-ISSUES-SPRINT3-CLOSEOUT.md` still describes the old `ClickApproachRun`
as dated history.

## Remaining checks

- **Code review:** accepted for verification (no blocking defect); the `Validate-Source.ps1` exception accepted.
- **Patch composition repaired: verified offline** with real Harmony composition and executed submission slices.
  Limits: ground admission (`GroundCommandPrefix/Postfix`) and native command execution were substituted; the attack
  and cast slices prove argument passthrough to the other mod's `Submit`, not firearm gameplay.
- **Native startup with 0.0.140: observed** (game-side PASS, both mods `Active`, `failure=none`); harness verdict FAIL
  on the UMM platform pin only (above).
- **Native startup with 0.0.191 (the incident version): not observed.** Needs either an owner-approved pinned input in
  `Read-KmcRetainedDependencySelection` (then `-BaseGameOnly -RetainedDependencySelectionPath`), or a run on a machine
  with 0.0.191 installed. Pass: both `Active`, no `Not loaded`, `failure=none`, no structural exception.
- **Owner's save recovery: not verified.** Load an unchanged copy of `Quicksave2` with this build and 0.0.191; pass
  means no `Failed to load blueprint by guid 3c81f5a02be74d96a4e07d1c92b6f385`, no `PostLoad_Patch2` exception and no
  `LoadGameException`.
- **Gameplay: not verified.** A mounted cannot-attack click (the correct horse moves, the pair stays mounted) and a
  firearm attack through Tabletop Rules Expansion.
- **Known pre-existing edge (not repaired here):** a `GroundCommandPostfix` fault after hand-off or refusal (see
  Independent review).
- **Restoration:** exact (above).

### Candidate checks (after the review corrections)

Selected Candidate (`-Tier Candidate -AssemblyTarget Kingmaker`, same focused scripts, budget
`gunslinger-click-compat`): build, components 1411/0, Validate-Source 153/0, composition 21/0, marker contracts 18/0
passed; `Test-Harness.ps1` then failed 255/26 and the tier stopped. 22 of the 26 were `Runtime state is ambiguous:
Kingmaker=1` from the foreign game process above. Rerun once with no game process: **278/3**, the three
baseline-identical failures (two historical package zips absent from `artifacts/`; the `CompositionRoot.cs` source pin
`if (errors.Length != 0) throw new AggregateException(...)`, absent at `12527350` as well). The steps the tier did not
reach were run directly: `Test-PairedActivationContracts.ps1` pass (patch construction 40/0), Kingmaker assembly
contracts 815/0; `Test-RuntimeArtifactManifestContract.ps1` fails reading retained native evidence absent from this lab
(`runtime-evidence/final105-p08-mammoth-tb/runtime-request.json`), unrelated to this change. Budget spent: about 12 of 90
minutes.
