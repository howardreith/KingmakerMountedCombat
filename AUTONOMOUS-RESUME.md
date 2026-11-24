# Mounted Shield private alpha (2026-11-24, owner amendment 2)

Read CURRENT.md's first section, docs/mounted-shield/HANDOFF.md section 0 (private-alpha paragraph) and
docs/mounted-shield/PRIVATE-ALPHA.md.

- Done: amendment 2 recorded (revision 2, `d735f07d`); alpha version 0.2.16-alpha.1; closure review and targeted recheck PASS
  on `bb903669` (controller round-1 corrections in `6fb53633`); focused gate PASS; package `KingmakerMountedCombat-0.2.16-alpha.1-mounted-shield-6fb53633-diagnostic.zip`
  (`fa5fb284…`). No native work (zero guarded starts); no agent-generated save (save-backed routes need the historical
  qualification suite; no desktop-control tool).
- Next: the controller's final review on the records head; the owner's private-alpha playtest. A later live smoke first needs
  qualified settings/profile protection on the no-save path.
- Keep: enrollment `kmc-codex-mounted-shield-20261123114646-4433`, budget `mounted-shield`, draft PR #61; no merge, release,
  tag or installation.

# Mounted Shield continuation (2026-11-23, owner amendment 1)

Read CURRENT.md's first section and docs/mounted-shield/HANDOFF.md section 0.

- Done: companion `5be0f737` imported; amendment recorded (revision 1, `8ee86965`); rollback, planned check totals and
  the gameplay driver/readers in `cb74ad58`; focused Fast gate and package on `cb74ad58`.
- Round 4 corrections in `bb0c53d5` (PN4-F1..F10).
- Next, once the canonical qualified KMC_AUTOMATION_BASELINE/WORKING pair is back: guarded startup smoke, then
  `sprint6-shield-rt`, `sprint6-shield-tb` and `mounted-shield-tb-manual`, restoring after each; then the final factory
  review. Any production or safety change before that needs another supplemental review.
- Keep: enrollment `kmc-codex-mounted-shield-20261123114646-4433`, budget id `mounted-shield`
  (set `KMC_WORKFLOW_BUDGET_ID=mounted-shield` for verify), draft PR #61, no merge/release/installation.

# UMM baseline follow-up (2026-11-23, owner decision)

Read the first two sections of CURRENT.md and the 2026-11-23 entry in docs/CHUNK7-NATIVE-LOADER-IDENTITY.md.

- Pin the distributable's build reference and declared minimum to **0.31.1** as the sibling mods do.
- The selected installed Windows runtime remains a separate exact **0.33.0** identity. Its native evidence does not
  establish native 0.31.1 or Linux/Proton acceptance.
- Preserve older loader profiles and all frozen receipts. Import this companion commit through the existing
  Mounted Shield enrollment; record the owner's amendment using the installed controller's supported route.
- Rebuild and run focused binding/reader checks on Windows before native use. The previous feature DLL/gate does not
  certify a build made with the changed reference.
- Complete the Mounted Shield code review, gameplay driver and required native acceptance after fixture admission.
  The pin decision alone does not create a qualified save or an accepted factory review.

# Mounted Shield feat (retained f19c8333 run record, 2026-11-23, PR #61)

Factory mission KMC-MOUNTED-SHIELD-2026-11-23 (foreground enrollment `kmc-codex-mounted-shield-20261123114646-4433`). Read
CURRENT.md (its first section) and docs/mounted-shield/HANDOFF.md.

- **Rules:**
  - One calculation (`MountedShieldPolicy.Amount` over `MountedShieldService.ReadFacts`) serves the mount AC and the check;
    never snapshot it, never read enhancement, never add a parallel test-only formula.
  - The mount value is a native `Shield` modifier owned by `MountedShieldContributionLedger`; it is released by the fact's
    turn-off (the native save path) and must never be written into a save or survive its pair.
  - Mounted Archery and Mounted Shield are withdrawn independently by the one `KmcCombatFeatPublicationSet`.
- **Open:** every native/UI acceptance row (UMM 0.33.0/0.33.2 pin and no KMC Working fixture; owner decisions).
- **Not authorized:** merge, release, install.

# Release 0.2.15 (owner order, 2026-11-22)

0.2.15 releases PR #59 (merged as `d409bffc`): X05's save-wait diagnostics and accurate save messages. Read CURRENT.md
(its first section), docs/RELEASE-NOTES-0.2.15.md and docs/SAVE-AND-DOOR-LOCK-HANDOFF.md. Open: X05's cause and repair
(needs an owner log with 0.2.15 or a disposable save); nothing was run in the game; the UMM 0.33.0/0.33.2 platform pin
(owner decision).

# Post-combat save and door lock (2026-11-22, branch codex/mounted-save-and-door-lock)

Diagnostic checkpoint, revised after the first Pro review (`0b4b8fa4`); awaiting the next Pro review through Howie (manual
procedure). Read CURRENT.md (its first section) and
docs/SAVE-AND-DOOR-LOCK-HANDOFF.md.

- **Rules:**
  - Change save readiness or the hold only on supported causal evidence: an instrumented game trace naming the blocking
    owner, or a faithful baseline reproduction of a source-proven defect.
  - The existing 30 s timeout is a fallback, not a repair.
- **Not authorized:** merge, release, install.

# Release 0.2.14 (owner order, 2026-11-22)

0.2.14 releases PR #57 (merged as `4e5c9c0c`): X02 toolbar placement, X03 capital companion screens, X04 explicit attack
on an already-unconscious enemy. Read CURRENT.md (its first section), docs/RELEASE-NOTES-0.2.14.md and
docs/CITY-AND-PRONE-HANDOFF.md. Open: all three behaviours natively (no qualified KMC Working fixture on this machine;
owner playtest steps in the handoff, section 12); X04's original prone-target report; the UMM 0.33.0/0.33.2 platform pin
(owner decision).

# Release 0.2.13 (owner order, 2026-11-20)

0.2.13 releases only the X01 repair (PR #55, merged as `a4d25e45`). Read CURRENT.md (its first section),
docs/RELEASE-NOTES-0.2.13.md and docs/GUNSLINGER-CLICK-COMPAT-REPAIR-HANDOFF.md. Open: 0.0.191 natively, the owner's
save recovery, mounted-click and firearm gameplay; the UMM 0.33.0/0.33.2 platform pin (owner decision).

# Tabletop Rules Expansion startup conflict — repair (2026-11-20)

Branch `codex/gunslinger-click-approach-compat` from `12527350` (`v0.2.12`), PR #55, merged and released in 0.2.13.

- **State:** the 0.2.12 `ClickUnitHandler.OnClick` transpiler no longer replaces the click command's submission; it
  guards it, so it composes with Tabletop Rules Expansion's rewrite of the same submissions in either order. Offline
  composition and executed-slice regression pass. Native `mod-load-smoke` with the installed 0.0.140: game-side PASS
  (both `Active`, `failure=none`); harness FAIL only on the UMM 0.33.0-vs-0.33.2 platform pin. 0.0.191 natively, the
  owner's save and gameplay are not verified.
- **Rules:**
  - Never make the click approach consume, replace or duplicate a submission another mod may rewrite. The router
    decides; the method's own submission runs or is skipped.
  - Recognise foreign submission shapes exactly (type and signature), never by arity alone. An unknown shape is a
    structural mismatch with diagnostics, not a fallback.
  - The foreign type name lives in exactly one constant (`Validate-Source.ps1` asserts it); no reference, namespace,
    `using` or saved identity of that mod.
  - `scripts/Test-GunslingerClickComposition.ps1` needs the released 0.0.191 DLL at
    `<lab>/compatibility-binaries/KingmakerGunslinger-0.0.191/` (SHA-256 pinned); never commit it.
- **Not authorized:** permanent install, loader replacement.

# Release 0.2.12 (owner order, 2026-11-19)

Release 0.2.12 ships the merged Known Issues Remediation Epic (PRs #45–#53). Read CURRENT.md (its first section),
docs/RELEASE-NOTES-0.2.12.md, docs/KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md, docs/CURRENT-KNOWN-ISSUES.md and
docs/CURRENT-SUPPORT.md.

- **State:** 0.2.12 is the current release. Every ID has one disposition. Unresolved: S01, M04's original incident,
  C01, T04–T08, T09. Native NOT RUN before release. No further sprint is planned by the epic.
- **Rules:**
  - The ledger's "Current status" section is canonical. A later change updates its row there first, then its sprint
    section; dated handoffs and release notes are not rewritten.
  - Never call an offline result native-verified or an unresolved report fixed.
  - A release's identity is its tag, tagged merge and recorded package hashes, never its version label alone.
  - Support, removal and rollback statements live in docs/CURRENT-SUPPORT.md and cite source, a handoff or a release
    note. Never infer rollback safety from the version label: every release since 0.2.0 has assembly version 0.1.0.0.
- **Not authorized:** install, staging, loader replacement, reviewer request or a further sprint.

# Known issues remediation — merged into main, before release 0.2.12 (2026-11-19, history)

The accepted stack #45–#52 is merged into main (ordinary merge commits, in order); PR #53 (Sprint 8's documentation and
closeout) lands after it. Read CURRENT.md (its first section), docs/KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md,
docs/CURRENT-KNOWN-ISSUES.md and docs/CURRENT-SUPPORT.md.

- **State:** the epic's scheduled engineering pass is complete, closed out and integrated. Every ID has one
  disposition. Unresolved: S01, M04's original incident, C01, T04–T08, T09. The latest release is still `v0.2.11`
  (historical release merge `7a5cd2c3`); main holds the epic after it, unreleased and not release-qualified. Native NOT
  RUN. No further sprint is planned by the epic.
- **Rules:**
  - The ledger's "Current status" section is canonical. A later change updates its row there first, then its sprint
    section; dated handoffs are not rewritten.
  - Never call a main-only item released, an offline result native-verified, or an unresolved report fixed. Merged to
    main is not released.
  - Never confuse current main with the release commit: `v0.2.11` stays at `7a5cd2c3`.
  - Support, removal and rollback statements live in docs/CURRENT-SUPPORT.md and cite source, a handoff or a release
    note. Never infer rollback safety from the version label: releases since 0.2.0 and every build of main share
    assembly version 0.1.0.0, and builds of main since `v0.2.11` also say 0.2.11.
- **Not authorized:** release, tag, version bump, package, install, staging, loader replacement, reviewer request or a
  further sprint.

# Known issues remediation, Sprint 7 — review checkpoint (2026-11-19)

Branch `codex/kmc-known-issues-sprint7` (draft PR targeting `codex/kmc-known-issues-sprint6`), from `f835c503`. Read
CURRENT.md, docs/KNOWN-ISSUES-SPRINT7-HANDOFF.md and docs/CURRENT-KNOWN-ISSUES.md. Sprint 8 bases on this branch
after review.

- **State:** U01-U04 implemented and verified offline as presentation projections; U04's native delivery and
  lifecycle corrected after the review of `292cd23c` (`92310c52`) and its failure containment after the review of
  `e7720f39` (`6ff46a2a`), and its pair cleanup made exact after the review of `89ace80f` (`e54009e9`); T09 recorded.
  Fast PASS 21/0; mutations P1-P34 34/34 caught. Native NOT RUN; no screen viewed.
- **Rules:**
  - Presentation reads authority; it never writes gameplay state to make native UI show something.
  - U01 reads only `MountedMovementState.ForcedTravelRecorded` on the mount allocation whose grant is the live
    activation's identity (`MountedMovementDisplayPolicy`). Never clear `TimeMoved`/`TimeForced` or a cooldown for display.
  - U02's mapping is one value identity plus a weak slot reference on the control service. Never match a slot by name,
    title, icon, blueprint alone or index; every target-selection start or end, and every lifecycle boundary, ends it.
  - U03 projects copies of the progression data native returns, afresh per panel build. Never edit the stock
    progression, a unit's `ProgressionData`/`LevelEntry` lists or the grant filter for display.
  - U04 derives the badge and hover from the live relationships each native call. Keep no tracker entry, view, unit or
    pair strongly; badge objects only weakly and bounded; the badge never takes pointer events.
  - The badge refresh seam is `UpdateCrippled` plus `Initialize`; never `HealthUpdate` (hit-point changes only), never
    polling or an object scan. Every world boundary, disable and disposal hides and destroys every badge; a badge whose
    removal fails stays owned (weak, bounded with the badges in use) and is retried; never clear ownership of a live
    badge, never reuse a residue as a portrait's badge.
  - The folded-mount hover redirect is one lease released before every native hover pass and at every boundary; native
    owns its own true/false pass. A party-portrait hover does not reach the tracker; do not invent one.
  - Releasing the redirect is a cleanup obligation: its two steps run independently and it is kept (weak, bounded at
    four) until each completes, its target is gone or replaced, or native's completed pass recomputes that tracker.
    A new redirect never overwrites unfinished cleanup.
  - A pair's cleanup ends the active redirect only when it is exactly the redirect's pair (owner, generation, rider,
    mount); any other pair's cleanup only retries owed cleanup. World boundaries, disable and disposal release it always.
  - Presentation seams are contained and report a fault once per seam.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 8.

# Known issues remediation, Sprint 6 — review checkpoint (2026-11-18)

Branch `codex/kmc-known-issues-sprint6` (draft PR targeting `codex/kmc-known-issues-sprint5`), from `772543e1`. Read
CURRENT.md, docs/KNOWN-ISSUES-SPRINT6-HANDOFF.md and docs/CURRENT-KNOWN-ISSUES.md. Sprint 7 bases on this branch
after review.

- **State:** A01, A02, A04 and A05 corrected offline; A03 settled; reviews of `2ae4cc8b` (`ce9b1383`), `fa533e97`
  (`1d0054f0`), `dffd3e72` (`0e1125fa`) and `95ae0b30` (`7898b1a4`) corrected. Fast PASS 13/0; mutations Q1–Q26 all
  caught. Native NOT RUN.
- **Rules:**
  - A same-target click continues a retained intent only while its request has not dispatched or its dispatched
    command is live (`IsFreshRequestAfterTerminal`). Never bind freshness to frames or timers.
  - Every owned pair-command end records a typed `MountedAttackEndCause` (`InterruptOwnedCommand`). A condition explains
    a terminal only through `CausalCapability`: the recorded `ActorCapability` block, or the block of a native
    capability branch recorded by `NativeCommandTerminalBranches` where native takes it. Never infer it from the
    terminal result, the started flag or the terminal snapshot.
  - The automatic `CanNotAttack` refusal is stateless and automatic-only. Player routes never read it.
  - A target is recorded unreachable only for a typed `ApproachFailed` end with approach evidence; `Unrecorded` is
    never evidence.
  - Faults record their cause before any diagnostic; the terminal is always delivered.
  - Native `Run` replacement uses only the token-exact `MountedNativeRunScope` (transpiler and `finally`); never re-resolve
    pair ownership to close a scope. Only the innermost open token on the ending command's container decides.
  - State transitions precede diagnostics, and diagnostics are contained, in the terminal handler, charge cleanup and
    the retired scheduler.
  - Native terminal facts live in the native invocation's own locals (`dup; capture; stloc` / `ldloc` into the terminal
    hook). Never hold a command, actor or controller in a static or thread-static field to classify a terminal.
  - The retired scheduler's lease release is deferred: a failed terminal observation faults the scheduler lease and records the scheduler's interrupt bookkeeping; it does not re-end the already-finished command, interrupt it again, or replace its existing terminal cause, and it does not archive; the next update
    after native slot removal, relationship cleanup or disposal releases the lease. Do not describe it as always
    released.
  - The CanNotAttack term reaches its own observation only through `ObserveShouldBeInterrupted`'s scalar frame (one per
    wrapped call, closed in a `finally`). Never reintroduce a single per-thread slot for it.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 7.

# Known issues remediation, Sprint 5 — review checkpoint (2026-11-17)

Branch `codex/kmc-known-issues-sprint5` (draft PR targeting `codex/kmc-known-issues-sprint3-closeout`), from
`af5b2bc7`. Read CURRENT.md, docs/KNOWN-ISSUES-SPRINT5-HANDOFF.md and docs/CURRENT-KNOWN-ISSUES.md. Sprint 6 bases on
this branch after review.

- **State:** R01 closed as not reproducible (no product change), R02 documented. Fast PASS 8/0 (0.82 min). Native NOT RUN.
- **Rules:**
  - The ranged-movement authority is the pair's current shared paired grant only. A mount's own `native:` record is
    riderless or pre-mount and never counts.
  - Do not add a "same horse this round" rule.
  - If a future change lets a mounted mount take its own slot, `Test-MountedRangedMovementContracts.ps1` fails, and
    the separate-initiative question reopens with that new path.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 6.

# Known issues remediation, Sprint 3 closeout — review checkpoint (2026-11-17)

Branch `codex/kmc-known-issues-sprint3-closeout` (draft PR targeting `codex/kmc-known-issues-sprint4`), from
`14f1b474`. Read CURRENT.md, docs/KNOWN-ISSUES-SPRINT3-CLOSEOUT.md and docs/CURRENT-KNOWN-ISSUES.md. Sprint 5 bases on
this closeout after review.

- **State:** M04 closed as engineering complete (two closeout repairs, full path inventory, bounded observation).
  Review of `b1a88b4e` (R1–R3) addressed in `cdfbe509`. Fast PASS 13/0 (0.59 min). Native NOT RUN.
- **Rules:**
  - Every product-built `UnitMoveTo` or `UnitAreaTransition` must be in the inventory pinned by
    `Test-MarkerOwnershipContracts.ps1`. A new one that asks for a marker fails it.
  - A pair operation's delegated move asks for no marker.
  - A native order that a mounted rider cannot walk goes through the ground-order lifecycle, or is refused. It is
    never hidden or completed.
  - Such routing is bound to the exact native call site that runs the order (a transpiler on that one `Run`), never
    to a frame or a flag. The original command object is what the mount runs, recognised by identity
    (`IsPairPlayerOrder`), never a copy.
  - A pointer placement is recorded only from the `AddPointer` call itself.
  - The marker observer writes on anomaly transitions only, contained, with no strong references.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 5.

# Known issues remediation, Sprint 4 — review of 813d00a1 checkpoint (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48), continued from the reviewed head `813d00a1`. Read CURRENT.md,
docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md (R10 section first) and docs/CURRENT-KNOWN-ISSUES.md.

- **State:** R10 corrected offline. Fast PASS 10/0. Native NOT RUN.
- **Rule:**
  - Scene ownership of a C02 operation is decided at its prefix (`NativeSceneOperationOwnership`,
    `MountedSceneOperationOwnership.Decide`) and carried in the typed capture.
  - Never recompute it from the global mode at the postfix.
  - Commands use their own `CutscenePlayerData`: a live player with this exact command current.
  - `TranslocateUnit` uses the cutscene parameter scope.
  - Do not treat every patched invocation as scene-owned.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 5.

# Known issues remediation, Sprint 4 — review of e1c0c7ad checkpoint (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48), continued from the reviewed head `e1c0c7ad`. Read CURRENT.md,
docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md (R7–R9 section first) and docs/CURRENT-KNOWN-ISSUES.md.

- **State:** R7, R8 and R9 corrected offline. Fast PASS 10/0. Native NOT RUN.
- **Seams added:**
  - `MountedScenePlacementAttributionCore.Apply` (Domain) is the only place one operation is applied to the captured
    pairs' holds. Log only through its report delegate.
  - `MountedScenePlacementSource` names the operation; never use a display string as authority.
  - The `CommandTeleportParty` prefix records the proven target set. `TranslocateUnit` and `CommandMoveUnit` stay
    single-unit unless installed IL proves otherwise.
  - `MountedScriptZoneChecks.CheckEach` (Domain) is the C04 zone pass.
  - `MountedScenePlacementHold.Reorient` holds a standalone rider turn at the rider's position.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 5.

# Known issues remediation, Sprint 4 — review of 263ac3ea checkpoint (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48), continued from the reviewed head `263ac3ea`. Read CURRENT.md,
docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md (R4–R6 section first) and docs/CURRENT-KNOWN-ISSUES.md.

- **State:** R4, R5 and R6 corrected offline. Fast PASS 10/0. Native NOT RUN.
- **Rules the review established:**
  - **Combat-hold notice:**
    - Never spend it before a receiver was reached. A returning `EventBus.RaiseEvent` proves nothing.
    - Count receivers inside the action native invokes per subscriber.
    - Never attempt it during a load.
  - **Scene placements:** classify moves and turns of both units (`ClassifyOperationWrite`, six inputs, wrapped angle
    comparison).
    - A rider-only turn reorients a pending placement; with nothing pending it is held at the rider's position (R7).
    - Keep attribution limited to the three patched operations.
  - **Fault reports:** each hook family keeps its own `MountedBoundedFaultReport`.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 5.

# Known issues remediation, Sprint 4 — review repair checkpoint (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48), continued from the reviewed head `b30c24f8`. Read CURRENT.md,
docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md (review section first) and docs/CURRENT-KNOWN-ISSUES.md.

- **State:** R1, R2 and R3 corrected offline; C01 still explicitly unsupported (wording narrowed). Fast PASS 10/0.
  Native NOT RUN.
- **Seams added by the repair:**
  - `MountedScriptZoneObservations<TUnit>` (Domain): zone observations per mounting instance (`MountedPair`). Never
    key them by the rider alone, and never depend on cleanup reaching them.
  - `MountedScenePlacementHold` (Domain): the rider synchronizer's only pending-placement state; always the scene's
    latest. The owner's poll reads `RiderMovementAgent.CurrentScenePlacement()`, not the raw pending value.
  - `MountedCombatHoldNotifier` (Integration): the only path for the combat-hold notice, through the native
    `IWarningNotificationUIHandler`. Do not make other persistence Reports visible through it.
- **Do not:** lift the encounter refusal or the settled-participation guard for C01.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 5.

# Known issues remediation, Sprint 4 — review checkpoint (2026-11-17)

Branch `codex/kmc-known-issues-sprint4`; parent `e5b92474`, the accepted head of `codex/kmc-known-issues-sprint3`
(draft PR #47, open), which the Sprint 4 draft PR targets. Read CURRENT.md, docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md and
docs/CURRENT-KNOWN-ISSUES.md.

- **State:** C04 and C02 implemented and verified offline; C01 explicitly unsupported with its blocker; C03
  intentional limit documented, with one separate repair (the combat-hold notice). Fast PASS 10/0. Native NOT RUN.
- **Seams added:**
  - `MountedScriptZonePolicy` (Domain) and `MountedScriptZoneAdapter` (Integration), from a postfix of
    `ScriptZoneController.Tick` (060092A0). They only call native `ScriptZone.CheckUnit`; never write a position here.
  - `MountedScenePlacementAttribution`, around `TranslocateUnit.RunAction` (06008FA2), `CommandMoveUnit.IsFinished`
    (06009E4E) and `CommandTeleportParty.OnRun` (06009E90). The decision is
    `MountedSceneContinuityPolicy.AttributesRiderPlacement`; the unattributed rule is `HoldsUnattributedPlacement`
    (keep its 3 m threshold).
  - `MountedRetainedIntentPolicy.CombatHoldNotice`, asked on every restore-loop evaluation.
- **Do not:** lift the encounter refusal or the settled-participation guard to support C01; that needs an owner
  decision on a native participation contract.
- **Contract scripts:** `Test-CandlemereCrossingContracts.ps1` and `Test-ScenePlacementAttributionContracts.ps1`. Both
  take `-ProductDll` for base comparisons and read installed files in place.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 5.

# Known issues remediation, Sprint 3 and its continuation — review checkpoint (2026-11-16)

Branch `codex/kmc-known-issues-sprint3` (draft PR #47); base `1dffc3e0`, the reviewed head of `codex/kmc-known-issues-sprint2`
(draft PR #46), which the PR targets. Read CURRENT.md, docs/KNOWN-ISSUES-SPRINT3-HANDOFF.md (last section first)
and docs/CURRENT-KNOWN-ISSUES.md.

- **State:** M01, M02, M03 and M05 implemented and verified offline; M04 investigation complete, issue unresolved,
  with two adjacent marker repairs; the group-pace fault report corrected; C04 recorded for Sprint 4. The owner's
  review of `5e27c9bd` (R1, R2) is corrected. The review of `25cbdbe2` accepted R2; its one R1 lifecycle finding
  is corrected, the review of `fe22563a` (world boundaries after the guard) is corrected, and the review of
  `119088ed` (who owns the mount's override at the arrival) is corrected. Fast PASS 11/0. Five independent reviews
  (Sprint 3, continuation, R1/R2, lifecycle, world boundaries), corrected.
  Native NOT RUN; gamepad never run.
- **Seams of the continuation (M05):**
  - `MountedContinuousDriveOwner` (pure) is the one place that binds, settles and retires a pair's stick movement.
    It talks to the game only through `IMountedContinuousDriveHost`; the adapter is
    `GameMountedRelationshipService.ContinuousDriveHost`. Put new decisions in the owner, where tests run them.
  - It is called from six places only: the `UnitMoveContiniously.Init` postfix (bind), the public
    `UnitCommands.Run` postfix (admission of the bound command, which must also see a refused Run), the internal
    `Run` postfix (an admitted rider order, by either route), `ValidateActivePair` before the invariant (settle),
    `Dismount` (release), and `CompositionRoot.UpdateCore` before its enabled gate (retry of retained cleanup).
  - Retirement is two separate steps on a residue record: end the command, clean the captured agent on its captured
    view. A record leaves only through `RetirementEstablished`. Never clear the watch without transferring it.
  - Lifecycle (review of `25cbdbe2`): `Dismount` reports retained stick cleanup in its result
    (`MountedCleanupCompletionPolicy.WithRetainedStickMovement`); every gate reads `IsComplete` or
    `IsWithoutResidue`. Do not set `cleanupRetryRequired` for it, and do not add it to a readiness pre-gate that
    runs before the cleanup.
  - Deliberate or automatic recovery is decided in one place, `ReleaseRecoversExplicitly`. A dismount that can
    repeat every frame must be automatic: `CleanupTrigger.Exception`, or bracketed by `automaticCleanupRetry`.
  - `ValidateActivePair` must keep calling `Dismount` directly after the invariant;
    `Test-PostLoadViewReadinessContracts.ps1` pins that.
  - `GuardBoundary` is strict. `GuardWorldBoundary` (area suspension, load, event suspension only) passes over a
    stick-only obligation; the owner kept that policy at the review of `fe22563a`.
  - What follows those guards must agree. Owners declare a world predicate (`DeclareDetachedCleanup`) beside their
    retained one.
    - `SettledForAdmissionDiscard` reads the world predicate.
    - A load calls `DiscardAdmissionBindingsForWorldReplacement`, which sets cleanup-only owners aside
      (`earlierWorldCleanup`).
    - The arrival defers a held pair through `DeferHeldArrival` / `DeferHeldCarriedArrival`. Never widen those beyond
      Restore, Defer and the pair's own retained agent.
    - "Its own retained agent" is `ArrivalRefusedOnlyForRetainedAgent`. It asks the exact held owner's boundary
      `RetainsCapturedAgent(current view, current override, generation)`, then the candidate's
      `RefusedOnlyForMountAgentOverride`. The candidate alone cannot tell a foreign override from the pair's own.
  - `Test-PostLoadViewReadinessContracts.ps1` pins two `MarkArrivalDeferred` call sites and one view read per
    arrival decision; the held deferrals share those sites.
  - `MountedWorldService` can be built offline with a registered owner (see `WorldFixture` in the continuous-drive
    script). Detach a boundary's `Dismounting` event and `ChargeCleanupBarrier` first: they need the game.
  - `Test-ChargeOwnershipContracts.ps1` still stops at T04 after 71 checks. Keep it out of a Fast selection that
    has to pass.
  - The real `GameMountedRelationshipService` can be constructed offline (a logger proxy and `DiagnosticSettings`)
    and its `Dismount`, `ValidateActivePair` and `Shutdown` run unmounted. The contract script swaps its private
    `continuousDrive` field for an owner on the scripted host.
  - The guard inside native `UnitMoveContiniously.OnEnded` (`EndedContinuousCommandOverride`) is the only thing that
    keeps a pair-bound command's end from clearing a foreign override. It finds the owner through
    `continuousDriveCommands`, filled at binding and emptied when a retirement is established.
  - `KingmakerMountedPairRuntime.MountOverrideOwner` is the invariant's only opening: a mount override is
    admissible only if the owner says it is the bound live drive. Never widen that test.
  - `ContinuousDriveTranspilers` holds the two transpilers; each requires its recorded site. `UpdateMovement` and
    `MoveSelectedUnitsToPoint` are registered through `GamepadMovementTranspiler` and `GroundOrderTranspiler`,
    which compose them with the Sprint 3 stages.
  - `ReportGroupPaceFault` goes through `MountedBoundedFaultReport`; every hook of these two sprints uses it.
  - `scripts/Test-ContinuousDriveContracts.ps1` runs only as a focused selection, like the other two.
- **Facts for a successor (gamepad):**
  - `InGameInputLayer.UpdateMovement` is the only creator of `UnitMoveContiniously`. It reads the driven unit once,
    and returns before that while the party is in combat.
  - `UnitMoveContiniously.Init` cannot run offline (Unity components); `UnitCommands.Run`'s refusal can, on objects
    made with `FormatterServices.GetUninitializedObject`.
  - A continuous command is not `IsMoveUnit`: `InterruptMove` and `CancelMountMovement` do not end it. Only its own
    tick (stick at zero), an `Interrupt`, or a `Run` that replaces it does.
  - Native `UnitMoveContiniously.OnEnded` clears the override of the executor's *current* view; the command stores
    neither view nor agent. `Interrupt` stamps its result before `OnEnded` and never repeats it.
  - Only the internal `Run(cmd, fromQueue, doNotClearQueue)` puts a command into a slot. Queue promotion calls it
    directly. A postfix on the public `Run` does not see promoted commands.
  - A new Move or Standard order ends the commands of both of those slots of the *same* container, or is queued
    behind an uninterruptible one. The pair's mount and rider are two containers, so that pairing never crosses
    between them: a stationary rider order survives the mount's movement by design.
  - `UnitEntityView.AgentOverride = null` destroys the component at the end of the frame; a new `Init` in the same
    frame adopts the dying one. Compare agents by reference and test liveness separately.
  - Native code treats a non-null override on the current unit as "stick-driven" (`UpdateMovement`,
    `MoveSelectedUnitsToPoint`, `UnitEntityData.CurrentSpeedMps`, `UnitMovementAgent.SlowDown`). The last two still
    read the selected rider.
  - An internal interface of the product can be implemented from a contract script with a `RealProxy` (a throwing
    logger, a scripted host).
- **Seams of Sprint 3:**
  - `MountedCombatController.ClassifyMountGroupOrder` is the one place that says what the mount's Move-slot
    command is to the pair (`MountedGroupPacePolicy.ClassifyOrder`: routed ground order, carried entrance order,
    or none). The acceleration carry, the gamepad follower answer and the marker prefix all go through it. A new
    owned move of the mount must stay `None` there.
  - The entrance carrier is bound in `MountedEntranceCarrierWatch` only after its `Run`, and let go by the
    controller's `Update` (`ReleaseEntranceCarrier`). Both watches answer for one pair generation.
  - `GroupPaceTranspilers` holds the two transpilers. Each requires exactly its recorded site and throws
    otherwise, which fails the patch installation. `MoveSelectedUnitsToPoint` now has two stages: the earlier
    paired-input stage, then `SpeedInputs`.
  - `MountedPaceActors.Project` is the only projection: a mover per unit, each actor once, nulls kept. The native
    selection is never changed.
  - `MountedDestinationMarkers.RetireRiderPointer` is the only write to the native pointer table. The entry is
    removed before the object is destroyed, because the native per-frame pass dereferences every entry.
  - `MountedPersistenceService.NewRegroupMove` is the one factory of the scene regroup move.
  - `scripts/Test-PhysicalMoverPaceContracts.ps1` runs the production transpilers on the installed methods' real
    instructions and reads the compiled wiring. It runs only as a focused selection, like the Sprint 2 script.
- **Facts for a successor:**
  - `UnitEntityView.MoveTo` always plans through the stock agent (`AgentASP.PathTo`), whatever override is
    installed, and neither `PathTo` nor the path callback tests whether the agent is enabled. A mounted rider's
    own commands are therefore still planned: their path completion calls `AccelerateIfPathIsLong` and places the
    rider's destination pointer.
  - `UnitEntityView.OnMovementStarted` does nothing but place a pointer.
  - `ClickPointerManager` never removes a pointer and shows one whenever its unit holds a live marker move or
    `UnitAreaTransition`, wherever it was last placed.
  - `UnitMoveTo` and `UnitAreaTransition` share the Move slot (`CommandType.Move`).
  - In gamepad mode `Game.Instance.UI.SelectionManager` is the console `UnitSelectionManager`; the mouse
    `SelectionManager` and the mod's selection projection are not in play.
  - `UnitCommands.Run` refuses a command whose executor is another unit, and only logs.
  - The installed reader yields modern Harmony instructions; a production transpiler takes Harmony 1.2 ones. Build
    them from the same instruction list you compare against, or local-variable operands will not compare equal.
  - Run mutation checks in a separate worktree. Then no mutant binary can be left in the tree whose results you
    report.
- **Open from this sprint:** M04 (needs the owner's description or a pointer-table log), native acceptance of
  M01–M03 and M05 (the M05 rows need a gamepad).
- **Next:** owner review of the draft PR. Sprint 4 is not started; its order is C04, C01, C02, C03 (C04, the
  Candlemere crossing while mounted, was added by the owner on 2026-11-16 and is recorded in the ledger).

# Known issues remediation, Sprint 2 — review checkpoint (2026-11-16, after owner review R1 and R2)

Branch `codex/kmc-known-issues-sprint2` (draft PR #46); base `b62c59a8`, the reviewed head of
`codex/kmc-known-issues-sprint1` (draft PR #45), which the PR targets. Read CURRENT.md,
docs/KNOWN-ISSUES-SPRINT2-HANDOFF.md and docs/CURRENT-KNOWN-ISSUES.md.

- **State:** S02 implemented and verified offline. S01 investigation complete, issue unresolved; diagnostics only.
  Owner review of `f71fabe7` corrected (R1 arrival placement through saving and loading, R2 S01 wording).
  Fast PASS 6/0. Two independent reviews, corrected. Native NOT RUN.
- **Seams:**
  - `KingmakerMountedPairRuntime.RestoreViewReadinessRefusal` is the only place restoration reads view activity.
    `RestoreAreaPair` and `RestoreRetainedIntentCore` call it before their decision and before any attachment;
    `scripts/Test-PostLoadViewReadinessContracts.ps1` holds them to that from the compiled IL. A new restoration
    path that attaches a pair must do the same.
  - `MountedRestoreViewReadinessPolicy.Refusal` is what `MountedViewActivityPolicy.IsAdmissible` accepts in
    `Default`, party-selector overlay included. The mounted invariant itself has no readiness allowance.
  - A pair deferred at arrival carries its arrival placement (`AreaPair.Arrival`, a Domain
    `MountedRetainedArrivalPlacement`). While neither unit has moved it is exempt from the retry's separation rule
    (`StandsAtArrivalPlacement`).
  - Owner review R1: that placement is saved and loaded with the row. `MountedRetainedIntentCapture.Row` and
    `SaveText` produce the member text; `MountedRetainedIntentAdoption` decides what a load adopts. The integration
    only projects fields (`ToRecords`, `ToAdoptedPairs`). The record text has version 2 for a save that holds such
    a pair; version 1 bytes are otherwise unchanged. A new copy of a custody row must go through
    `AreaPair.CarriedCopy`, which leaves `Arrival` out: the contract script allows exactly two writers of that
    field and three callers of that method. The placement names its area and is honoured and saved only there.
  - `MountedPersistenceService` subscribes `MountedWorldService.PairDismounting`; only `CleanupTrigger.Manual`
    releases a retained row.
  - `NativeSaveRequestPolicy` mirrors the order of the installed `Game.SaveGame` (06000CE4);
    `Test-PersistenceContracts.ps1` checks that order from IL.
- **Facts for a successor:**
  - World-map, kingdom and settlement modes set `IsSleeping` on every unit; a sleeping unit's view is inactive;
    `Game.Tick` ticks no mode while a loading process is active.
  - `OnlyOneSave` and `AutosaveEnabled` are fields of the native settings root, not properties: an IL order check
    must read field operands, not only calls.
  - `LoadingProcess.StartLoadingProcess` only stores an operation; its first step runs from the loader's update.
  - Mounting is admitted in `Default` only (`MountedGameModePolicy.CanAdmitMountedAction`), not under a pause.
  - The native party selector (`GroupManager`) is a field of the UI canvas, not a property.
  - In PowerShell, `[IO.File]` resolves a relative path against the process directory, not the shell location.
    Use absolute paths in a worktree.
  - A source file restored with `Copy-Item` keeps its old timestamp, so the incremental build can skip and leave a
    stale binary. Touch restored files, or rebuild, before trusting a tier result after a mutation run.
- **Open from this sprint:** S01 (needs the owner's answer or a log from this build or later); native acceptance
  of S02 on a world-map arrival; T07, T08.
- **Next:** owner review of the draft PR. Sprint 3 (M01–M04) is not started.

# Known issues remediation, Sprint 1 — review checkpoint (2026-11-15)

Branch `codex/kmc-known-issues-sprint1` (draft PR #45); base main `7a5cd2c3` (0.2.11). Read CURRENT.md,
docs/KNOWN-ISSUES-SPRINT1-HANDOFF.md and docs/CURRENT-KNOWN-ISSUES.md.

- **State:** T01–T03 implemented and verified offline. Fast PASS 11/0. One independent review and one owner
  review, both corrected. Native
  NOT RUN. The ledger is started;
  Sprints 2–8 are carried-forward rows that have not been rechecked.
- **Seams:**
  - `Chunk7ReplaySubscriberContract` (producer) and `Get-KmcChunk7ReplaySubscriberContracts` (reader) declare the
    repeated-world subscriber revisions separately. `Test-Chunk7ReplaySubscriberContract.ps1` holds them to the
    compiled product. A new production `EventBus` subscriber or interface needs a new revision in both.
  - `scripts/UmmLoaderContracts.ps1` is the only loader rule. `Test-AssemblyContracts.ps1` calls it for the
    installed loader; `Test-UmmLoaderContracts.ps1` inspects any other reference, one per process.
- **Facts for a successor:**
  - `Kingmaker.Game`'s type initializer throws detached and poisons the type for the process.
  - The detached `EventBus` accepts uninitialized subscriber objects; the product's
    `Chunk4SubscriptionSnapshot.Capture` reads it.
  - Two `UnityModManager` assemblies cannot be reflection-only loaded in one process.
  - A PowerShell function that returns one element returns a scalar: index with `@(...)[0]`.
  - PowerShell's `-ceq`, `-cin` and `-ccontains` are culture-sensitive and ignore U+00AD. Use ordinal comparison
    for names in evidence.
  - Reflection resolves a member reference whatever its accessibility. Check `IsPublic` and `IsVisible` as well.
- **Open from this sprint:** T04 (charge probe drift after `AdmitChargeCommand` and
  `RequireChargeBuffSurface` changed), T05, T06.
- **Next:** owner review of the draft PR. Sprint 2 (S01, S02) is not started.
# Mounted attack stall repair — review checkpoint (2026-11-15)

Branch `codex/mounted-attack-stall-repair` (draft PR); base main `474b8667` (0.2.10). Read CURRENT.md and
docs/MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md.

- **State:** terminal classification, turn-based admission check, execution-boundary check and diagnostics. Fast PASS
  on genuine references. Focused probe 59/0; it fails on a build of `474b8667`. One independent review, corrected.
  Native NOT RUN.
- **Seams:**
  - `MountedAttackTerminalPolicy.Classify` / `RetainsIntent` is the only rule for whether a finished command keeps its
    retained order. `HandleCommandTerminal` is its only caller.
  - `MountedActorCapabilityPolicy` is the only capability rule. `NativeActorCapability.Read` is the only reading.
    Callers: `CaptureContext` (admission), `StartChildAttack` (release) and `ReportTerminalOnce` (diagnostic).
- **Facts for a successor:**
  - In 2.1.7b a raw native `Success` does not mean an executed command: `UnitCommand.ForceFinish` stamps it, and
    `IsActed`, on a command that never started. Its only callers are the two turn-based checks in
    `UnitActionController.TickCommandTurnBased`.
  - `HasStandardAction()` already covers Nauseated, and nothing else about conditions.
  - `UnitState.CanAct` also reads prone and animation state. Never use it as an admission gate: read the conditions.
  - Detached: `CombatController.IsInTurnBasedCombat()` and `UnitState.CanAct` throw (they need `Game`). A `UnitState`
    built by its constructor answers `HasCondition` and `IsHelpless`. `EventBus` works, but keys subscribers by
    concrete type, so a recording subscriber must be a real (emitted) type, not a proxy.
- **Native next step:** owner play with the branch DLL in the reported encounter, or a qualified fixture: rows S1–S9
  in the handoff. Search the log for `retired without execution` and `Mounted stock attack dispatch rejected`.
- **Open:** the Serenity/Fascinate trigger is unidentified. In real time a blocked rider's order still waits in its
  slot and the mount does not attack in its place (unchanged).

# Mounted Archery registration repair — review checkpoint (2026-11-13)

Branch `codex/mounted-archery-registration-repair` (draft PR); base main `fef7b71e` (0.2.9). Read CURRENT.md and
docs/MOUNTED-ARCHERY-REGISTRATION-REPAIR-HANDOFF.md.

- **State:** validator corrected and per-invariant diagnostics added. Fast PASS on genuine references. Focused script
  54/0. Native NOT RUN.
- **Seams:**
  - `MountedArcheryFeatContract.Violation` is the only rule.
  - `HorseCompanionBlueprintService.BuildMountedArcheryContract` and `MountedArcheryContractViolation` are the code the
    registration and the focused script share.
- **Facts for a successor:**
  - In 2.1.7b, `BlueprintFeature.IsClassFeature` is true on every stock feat. It is read only by
    `Player.RespecCompanion`. Never require false for a feat.
  - Unity aligns each serialized C# bool to 4 bytes (no typetree in these files).
  - Kingmaker blueprints carry both `name` and `Name`, so PowerShell's property adapter refuses them. Use `FieldInfo`.
  - In this UnityEngine.CoreModule, `Object.Equals` for two non-null objects is `ReferenceEquals`. Uninitialized
    blueprints therefore compare as live ones.
- **Native next step:** owner play with the branch DLL. Search the log for `Mounted Archery feat contract:` and
  `Mounted Archery offered`, then take rows R1–R5 of the handoff. After that, the older Mounted Archery rows N1–N6, C
  and C2 become reachable.

# Mounted Archery — review checkpoint (2026-11-13)

Branch `codex/mounted-archery` (draft PR); base main `6b6a3e6a` (0.2.8). Read CURRENT.md and
docs/MOUNTED-ARCHERY-HANDOFF.md.

- **State:** feat, TB mounted ranged penalty, icon. Fast PASS on genuine references. Independent review corrected.
  Native NOT RUN.
- **Seams:**
  - `MountedRangedMovementService` (global `IGlobalRulebookHandler<RuleCalculateAttackBonus>`; previous event must be
    the enclosing `RuleAttackRoll` with `ACRule` set and `AttackBonusRule` null).
  - `MountedMovementStateAdapter.TryReadCurrentMountTravel` — the only rules reading of an allocation; `PeekMovement`
    stays diagnostic.
  - `MountedRangedMovementPolicy.IsAllocationCurrent` / `CurrentPairedGrant`: the authority is the pair's current
    paired activation (`MountedPairTurnParticipant.CurrentPairedGrant`), not the rider's movement record. Legacy modes
    use ungranted round records.
  - `MountedRangedMovementPolicy.QualifyingRangedWeaponAttack` / `CommittedRollCalculation`: a wielded weapon in the same
    attacker's unresolved roll. No `RuleAttackWithWeapon` requirement, so Gunslinger Dead Shot is covered.
  - `KmcCombatFeatPublicationSet`: the required lists are ordinary and fighter. Availability is membership of KMC's
    Mounted Archery feature. Losing it there withdraws: KMC's own entry is narrowly removed from every list that holds
    it, foreign-changed lists included, and again if it reappears. Exact leases still guard whole-array publish and
    restore. Before withdrawal it refreshes each frame by reference to the required arrays. Once withdrawn it scans
    every array's contents each frame, which catches in-place reintroduction. At apply and attack time it checks in
    full.
- **Native next step:** owner play with the branch DLL, or a qualified fixture: rows N1–N5 in the handoff. Log line
  `Mounted ranged movement:` carries pair, allocation, travel, ownership and contribution.
- **Open:** a pair acting on separate initiative slots gets no contribution (conservative); up to 0.25 s of
  second-Move travel can go unpenalized; the native difficulty floor can absorb part of the penalty.

# Cutscene continuity — review checkpoint (2026-11-12)

Branch `codex/kmc-cutscene-mounted-continuity` (draft PR); base main `0754c44a` (0.2.7). Read CURRENT.md and
docs/CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md.

- **State:** source fix plus diagnostics. Fast PASS on genuine references. Native NOT RUN. An independent review is
  needed before live use.
- **Seams:**
  - `MountedGameModeDisposition.PreserveScriptedScene`.
  - The `UnitCommands.Run` prefix calls `MountedPersistenceService.ObserveSceneCommand`.
  - `IInGameHandler` calls `ObserveSceneInGameChange`.
  - The restore loop reads `CutsceneControlledUnit.GetControllingPlayer` and `UnitCommand.Cutscene`.
- **Native next step:** owner play with the branch DLL, or a qualified KMC fixture. The essential rows are hostile
  dialogue into combat and a scene during an encounter, both TB and RT. Take the rest from the handoff table.
- **Also on this branch (owner request):** the Bald Hilltop Mount lockout.
  - `NativeSceneControl.IsUnderLiveSceneControl` is the only reading of actor scene control.
  - Never test `CutceneControlledUnit != null`: it is never cleared.
  - The change-of-reason line "Mounted player action unavailable" names the refusing input in an owner log.
- **Open:**
  - A scripted command on a seated rider during combat is not handed off.
  - A rider-only teleport is kept on the mount.
  - A pair still staged at combat start fights unmounted for that encounter.

# Horse group pace — review checkpoint (2026-11-12)

Branch `codex/kmc-horse-group-pace` (draft PR); base main `bd22a045` (0.2.6). Read CURRENT.md and
docs/HORSE-GROUP-PACE-REPAIR-HANDOFF.md.

- **State:** source fix plus diagnostics. Fast PASS on genuine references. Native NOT RUN.
- **Seam:** `UnitCommand.AccelerateIfPathIsLong` 0x060027B4. The routed order's identity is `MountedCombatController.playerMove`, bound only by the ground click's own Run.
- **Native next step:** owner play with the branch DLL, or a qualified KMC fixture. Use a whole-party move longer than 13 m with Accelerated Move on, then the restriction, short-move and turn-based rows in the handoff.
- **Open, same mechanism, not changed:**
  - the entrance-carried `UnitAreaTransition`;
  - gamepad stick acceleration;
  - the group limit computed from the rider rather than the horse.

# Attack completion warnings — review checkpoint after the lab continuation (2026-11-12)

Branch `codex/kmc-attack-completion-warnings`, draft PR #33; base main `680d02cb` (0.2.5). Read CURRENT.md and the top
of docs/ATTACK-COMPLETION-WARNINGS-HANDOFF.md.

- **State:** source correction plus a review refinement (code-classified later-dispatch refusals; all stock dispatch
  rejections logged). Ten regressions in total. Fast PASS 3/0 on genuine references (components 1168/0).
- **Lifetime:** command-terminal cleanup clears `stockIntentCommand` but intentionally retains `stockIntent`. The
  latter remembers accepted dispatch. Read the rejection feedback before clearing the intent; same-target promotion
  must preserve dispatch history.
- **Native next step:** it needs a qualified KMC Baseline/Working fixture and `runtime-state\fixture-qualification.json`
  on this machine (neither exists), or an owner session with the branch DLL. Take the scenarios from the handoff's
  remaining-evidence section.
- **Delivery:** review branch only; no merge/release/tag/install/launch. Existing unrelated defects remain open.

# Exploration repair resume — partial, native blocked (2026-11-12)

Branch `codex/kmc-exploration-markers-speed-events` (draft PR). Read CURRENT.md and docs/EXPLORATION-REPAIR-HANDOFF.md.
- **State:** source repairs for issues 3–7 have the owner review's corrections and pass Fast. Issues 1–2 are parked by
  the owner and have diagnostics only.
- **Saved intent:** archive member `kmc-retained-intent` (Domain `MountedRetainedIntentRecord`). It is written only
  when intent exists, adopted at `OnAreaLoaded` only for the exact campaign, area and `TimeController.GameTime`
  ticks, and never for a pair that `kmc-mounted-state` holds.
- **Native:** every row is NOT RUN. It needs the Working fixture and runtime-state, or an owner session with this DLL.
- **Next:** owner play on the branch DLL. Issues 1–2 wait for the owner's details.

Facts for a successor:
- **World map:** the area whose `CustomUIScene.SceneName == SceneName.CustomUIGlobalMap`. Native `GlobalMapRules` enters
  `GlobalMap` mode during scene init, so no attachment is admitted there.
- **Console selection:** `UnitSelectionManager` (console UI) exists only under gamepad control. The `AgentOverride`
  click-point branch of `MoveSelectedUnitsToPoint` does not run in mouse play.
- **Corpse:** a corpse click is `UnitLootUnit` (Standard, 1 m). Loot bags (`DroppedLoot`) are map objects with a
  `LootComponent`.
- **Camp:** `RestInteraction.OnInteract` issues `UnitApproachCamp` per unit (pets included) under one
  `RestGroupCommand` with no timeout.
- **Familiar:** `TeleportToMaster` copies `Master.View.transform.parent`.

# Finalization resume — F6 accepted and merged; no planned engineering (2026-11-07)

**Integration:** main contains F6 as merge `eb16d2f1`, owner-merged; its tree is identical to the reviewed `4b8a5f02`. Read CURRENT.md and the closeout section at the top of docs/FINALIZE-F6-REVIEW-HANDOFF.md.

**State:** the six-sprint selected refinement engineering is complete, except that WORLD-02 stays deferred and not passed (docs/FINALIZATION-DEFERRED-ITEMS.md).

**What remains:**
- the owner's play acceptance on the f6f package `74baa915…`;
- an explicit publication decision.

There is no F7. A defect the owner reports selects only its concrete causal correction.

**Not authorized:** release, tag, publication, installation or a new native start. The finalize-f6 record is at 5 of 6 starts and 42.75 of 90 minutes; the sixth start was never used and is not a testing target.

# F6 resume — final review stop, completion PASS on re-evaluation (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f6; branch codex/kmc-finalize-f6-falls-final-acceptance; draft PR #21. Read CURRENT.md and the top of docs/FINALIZE-F6-REVIEW-HANDOFF.md.
- **State:** ENGINEERING COMPLETE — READY FOR FINAL REVIEW.
- **Spend:** 5 of 6 starts, 42.75/90 minutes. Start 6 is a contingency for a concrete diagnosed defect only.
- **Not authorized:** merge, release, tag, install or publication.

Facts for a successor:
- **Native stand-up:** TurnController.HandleUnitWillGetUp adds +3 s to Move and cancels the order. While the Standard is unused, HasMoveAction() stays true after one Move, so read the Move cooldown.
- **Every new Phase 3D row name** must be in `Get-KmcPhase3dHorseRuntimeRows`, and every new chunk7 persistence row in `Get-KmcChunk7PersistenceRows`. Otherwise the outer reader refuses the run as an "unknown subscenario".
- **Retired custody:** an unseated pair whose custody retired reads `None`, not `Unmounted`.
- **Split timing:** the paired split can end the activation a frame after the fall resolves.

# F6 resume — review stop, 4 of 4 starts (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f6; branch codex/kmc-finalize-f6-falls-final-acceptance. Read CURRENT.md and the top of docs/FINALIZE-F6-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL.
  - Shown natively: the TB death fall, the RT prone fall, and the post-fall save/reload (r03 pinned reader-only source, r04 PASS).
  - Not shown natively: the safe-landing branch, the TB prone, prone-then-death and the TB stand-up cost.
- **Spend:** 32.77/90 minutes, 4/4 starts. No further start is authorized.
- **Not authorized:** merge, release, tag, install or publication.

Facts for a successor:
- **Mount death during the pair's own activation:** the existing paired split force-ends that activation (native ForceToEnd). A stand-up row must wait for the rider's next native turn.
- **New chunk7 persistence row names** must be added to `Get-KmcChunk7PersistenceRows`, or the runtime-result readers refuse the run as an "unknown subscenario".
- **New Phase 3D child scenarios** need the `-clike` lists in `ChildEntryPreambleEvidence.ps1`, not only the exact-name lists.
- **Labelled test outcome:** `MountedRiderFallService.ScenarioOutcome` picks the d20 inside the native check from its own stat and adjusted DC. It is null in play.
- **The F3 row `F3-tb-b-round1`** can refuse its full attack non-deterministically (r02).

# F5 resume — final review stop, cold consumer PASS (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack; draft PR #20. Read CURRENT.md and the top of docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING COMPLETE — READY FOR FINAL REVIEW.
  - Producer r07 and cold consumer r08 both PASS at d87b148e (DLL `8459126c…`), and the cross-run reader passes.
  - Manual `d39f8b46…`, 858008 bytes.
- **Spend:** 8 of 8 starts, 74.9/90 minutes. No further start is authorized.
- **Not authorized:** merge, release, tag, install, publication or F6.
- **Next:** final review and owner acceptance.

# Superseded F5 resume — saddle-diagnosis mission stop (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack; draft PR #20. Read CURRENT.md and the top of docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. Saddle cause proven and corrected. r06 (fd8bb820) is game PASS with the corrected saddle and all naming rows, but its recorded verdict is FAIL (harness UTF-8 defect, fixed in d6c88978). The consumer has not run.
- **Spend:** 6 of 7 starts, 63.97/90 minutes.
- **Not authorized:** reusing r06 (recorded FAIL) as a source, an eighth start, more time, merge, release or F6.
- **To finish once the owner raises the ceiling to 8:**
  - Run a Candidate at the current head.
  - Run `scripts/Package.ps1 -ArtifactQualifier`, then the suite snapshot.
  - Run the producer `companion-names-tack-manual` (chunk7-persistence-save) and inspect its frames.
  - Run the consumer (chunk7-persistence-load) from the producer's run id and Manual hash.
  - Run `scripts/Test-Finalize5ColdAgainstSource.ps1`.
  - Make no records commit between producer and consumer.

Facts for a successor:
- **Real dog body:** the dog3 back has a top vertex only every 5–10 cm. Sample surfaces with rays, never with the "highest vertex in a window" rule.
- **Specimen and replay:** the tack specimen (`tack-specimen/` in a run's evidence) plus the replay in `analysis-cache/finalize-f5-saddle-replay/` reproduce any fit offline. `Replay.cs` compiles with the VS 2022 BuildTools Roslyn `csc`, Kingmaker's Newtonsoft.Json, and the UnityEngine and CoreModule DLLs copied beside it for type loading only.
- **Evidence encoding:** game evidence is UTF-8 without a byte-order mark. Read it with `-Encoding UTF8` (`Read-KmcJson` now does).
- **Earlier F5 facts:** the TMP `m_char_buffer`, overtip and post-F2 slot facts are in the superseded section below.

# Superseded F5 resume — completion mission stop (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack; draft PR #20. Read CURRENT.md and the top of docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. r04 (d32fdc28, f5d) has naming and the Manual PASS game-side; the saddle reader FAILs and the frames show shards. The consumer has not run.
- **Spend:** 4 of 6 starts, 42.52/60 min.
- **Not authorized:** reusing r04 as a source, a start without a diagnosed correction, more time, merge, release or F6.

Facts for a successor:
- This TMP's `SetText` never stores `m_text`; read a rendered label from `TMP_Text.m_char_buffer` (0x040014C2) up to its 0.
- Overtips live in `OvertipManager.m_UnitControllers` (via `Game.Instance.UI.BarkManager`) and draw `CharacterName` with a decorative first letter.
- The tack's centre-line diagnostics can be on design while the rendered pad is wrong. Judge it by `maxRise` and the frames.
- The scratch guard `f5-guard.ps1` derives the ceiling from the record's `startCeilingAdjustments` chain, capped at 6.
# F5 resume — review stop, start ceiling reached (2026-11-07, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack from main 3f19e9bd. Read CURRENT.md and docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. 3 of 3 starts used, 26.83/60 min. The producer runs r01–r03 all FAILed for causes since corrected; the cold consumer has not run.
- **Not authorized:** a fourth start, merge, release or F6.
- **F4:** integrated by the owner (3f19e9bd). WORLD-02 is deferred, not passed ([deferred items](docs/FINALIZATION-DEFERRED-ITEMS.md)).

Facts for a successor:
- **Post-F2 controls:** a rider can hold the public control (kind 6) and its placement record (kind 7, index 0). A saved hidden Dismount (kind 2) binding is migrated into kind 6, so the old Chunk 7 two-per-rider slot expectations predate F2 (the F5 case binds Rider Primary only).
- **Native text dialog:** while its field holds focus, KeyboardAccess runs no binding, Esc included. Keys sent through `TMP_InputField.ProcessEvent` need `ForceLabelUpdate` before they are drawn.
- **Real dog body:** fur and limbs spike ray casts, and the midline's lowest point drops to the ground ahead of the seat. Tack geometry must smooth from one anchor per sample and be skinned to the spine only.
- **Launch:** freeze from the attached head and commit records only after both runs.

# F4 resume — final review stop, reload PASS (2026-11-06)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and the current section of docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** F4 ENGINEERING PARTIAL — WORLD-02 INPUT BLOCKED ONLY.
  - r08 (entrance) and r10 (reload) PASS on the same DLL 3240963f…/cc86d1d6.
  - 10 of 10 starts used, at 68.17/80 min.
- **Not authorized:** an eleventh start, merge, release, publication, or F5.
- **Only remaining item:** the native trap attempt, which needs the owner's Manual trap save.

# F4 resume — final review stop after the final reload execution mission (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions (head 82689ab7 plus records); draft PR #19. Read CURRENT.md and the current section of docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL.
  - The reload is NOT RUN.
  - Start 9 was spent on a pre-staging refusal: the detached HEAD made `git branch --show-current` empty, so `.Trim()` threw in `Assert-KmcPackageManifest`.
  - WORLD-02 is input blocked.
  - 9 of 9 starts are used, at 64.7/80 min.
- **Not authorized:** a tenth start, merge, release, publication, or F5.

Facts for a successor:
- The launcher pins the package manifest's commit *and* branch to the worktree. Never detach to reach a frozen commit.
- Commit records only after the native runs, or package from the head the run will use.
- A refusal after admission counts as a start in the record.

# F4 resume — final review stop after the entrance/reload finishing mission (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and the current section of docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL.
  - The r08 entrance is PASS.
  - The reload is not run: 8 of 8 starts are used, at 64.47 of 75 min.
  - WORLD-02 is input blocked.
- **Not authorized:** merge, release, publication, a new start, or F5.

Next run, if the owner authorizes one start:
- The scratch helper is `f4run.ps1 -Kind dungeon-entry-auto -SourceRunId 20261106-finalize-f4f-dungeon-08 -SourceSha256 b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5`. It is pinned to f4f; a new ceiling needs the same append-only record entry and a helper change.
- The consumer WhatIf `20261106-finalize-f4f-dungeon-reload-whatif` already passed.

Facts for a successor:
- The product writes the schema-3 archive member (`Pairs[]`). `NativeMountedSaveStorage.Read` / `persistence.LoadedData` is the legacy view and is not Current for schema 3. Use `ReadArchive` / `LoadedArchiveData`, as the cold restore path does.
- The launch guard reserves the 600 s outer timeout, so each start needs at least 10 minutes of budget left.
- `@()` around a comma-wrapped .NET List from a function counts the list as one element. Enumerate it with `foreach`.

# F4 resume — final review stop after the completion mission (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. 6 of 6 starts are used (47.88/75 min). The launch helper refuses a seventh start.
- **Not authorized:** merge, release, publication, a new start, or F5.

Facts for a successor:
- Native `GroupManager.Go` runs `SetGroup`, `Show(false)` and then the go action (the AfterEntry `LoadArea`) in one call. The hub's selector component is destroyed by then, so read it only through Unity's `== null`.
- The workflow batch guard refuses a third equivalent attempt in a batch after two failures. A materially changed candidate runs under a new batch id. A refusal charges no start.
- The Working fixture floor has native hostiles that can join a row's fight and keep the pair in combat. The Sprint 4 melee row then waits for out-of-combat forever.
- Native Hold (`AiMovementForbidden`) filters brain proposals to targets in reach with LOS. A completed player move arms KMC's player-move hold; Stop clears it, and an AI off/on cycle clears the Stop suppression.
- The P07 dungeon entry declares one leaf `Auto_1.zks` with Area = floor and AdmissionArea = hub, write cap 1 and load cap 1. The Working fixture's baseline and working areas are both the floor `9d1278a2…`.

# F4 resume — review stop (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions. Read CURRENT.md and docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. All 3 F4 starts are used (23.85/75 min). The launch helper refuses a fourth start.
- **Not authorized:** merge, release, publication, a new start, or F5.

Facts for a successor:
- Registering a new tranche scenario takes more than the Sprint 4 list. The `-clike 'sprint4-*'` prefix dispatches in Chunk6aNativeMammothEvidence.ps1 and ChildEntryPreambleEvidence.ps1 need it, and so do both row lists: RuntimeHarness.Common.ps1 near 4223 and RuntimeProtocol.cs near 1566.
- Owner-surface isolation is zero-write. The native after-entry autosave of a dungeon entry is a fatal violation there.
- The roguelike hub holds 2 LootComponent (ChestForStash) and no trap.
- After an F4 stage moves the pair, the rider-on-foot comparison leg may find no route.
- `UnitAttack.Target` has a private setter, so the continuation sets it through the exact token 0x0600266C.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f4.json.

# F3 resume — final review stop (2026-11-06, completion)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f3; branch codex/kmc-finalize-f3-movement-economy-hud; draft PR #18. Read CURRENT.md and docs/FINALIZE-F3-REVIEW-HANDOFF.md.
- **State:** ENGINEERING COMPLETE. TB r05 and RT r04 PASS.
- **Allowance:** 5 of 6 starts used, at 51.47/90 min. The sixth start was a contingency for a diagnosed defect and is not scheduled; the F3 launch helper refuses a seventh.
- **Not authorized:** merge, release, image publication, or F4.

Completion facts:
- **Native full attack in TB.** It needs the attack variant cycled to Full with the native right-click before the left click. The fixture's pair-B rider, at level 11, plans four strikes.
- **Panel.** The prediction panel's slots follow their `TurnController`. The mount's private states only advance on the mount's own command events, so the fill recomputes them on every rider-selected ground hover. `ComputeCurrentActionStates` is a turn-start snapshot: any started Move or conversion counts as gone, and a used step still counts as available.
- **Cleanup.** In r05 it went Pause, Default, then native leave-combat, and EscMode did not recur. r03's opener is unidentified.

# F3 resume — review stop (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f3; branch codex/kmc-finalize-f3-movement-economy-hud. Read CURRENT.md and docs/FINALIZE-F3-REVIEW-HANDOFF.md. All four F3 starts were used at that stop; the completion mission later raised the ceiling to six.

Facts for a successor:
- The Working fixture's second party member has no pet. The Sprint 6 Ranger acquisition needs four levels of experience granted first.
- A row step bound must stay under the tranche's 30 s leaf deadline, or a stall loses its facts.
- With the rider selected, the TB preview previously read the rider's TurnController. PairedMovementContext now routes it to the mount.
- Native: a partial five-foot step leaves a residual step. A move does not empty movement (`DoesCommandEmptiesMovement` is false).
- Native `UsedStandardAction` ignores the two-move conversion; `HasStandardAction` does not.
- The fixture rider's TB stock attack is a single attack unless it approaches.
- r03's cleanup stalled in EscMode with the party still in combat; the cause is unknown.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f3.json.

# F1 resume — final review stop

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f1; branch codex/kmc-finalize-f1-creation-ui-icons; draft PR16. Read CURRENT.md and docs/FINALIZE-F1-REVIEW-HANDOFF.md. Preserve newer work. Do not restart intake or historical campaigns.

State: F1d run 20261105-finalize-f1d-native-01 passed and restored. All four F1 starts are used. Nothing runs from this resume: no launch, allowance increase, merge, release or F2 is authorized. A review correction that changes product bytes needs a new owner decision, package, suite and start; do not relabel run evidence onto it.

Facts for a successor:
- WhatIf stops before Test-RuntimeRequest. scripts/Test-FinalizeF1Finish.ps1 validates the next request's exact shape directly.
- The package allowlist excludes UnityEngine.UI, so the product reaches those members by exact token through reflection.
- The observer's refresh-scroll probe returns -99 because no parent ScrollRect is found from the layer. It gives no scroll evidence.
- Creator Next requires all ability and skill points spent. UMM's Ctrl+F10 needs real scan codes under synthetic input. Check the class list's scroll before clicking; it differs in a second creator.
- The publishing helper registers images by exact path and SHA, and each new image needs owner approval; see docs/evidence/finalize-f1/publishing-authority.json.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f1.json.

