# Post-combat save and door lock: handoff (2026-11-22)

Mission: [MOUNTED-SAVE-AND-DOOR-LOCK-MISSION.md](MOUNTED-SAVE-AND-DOOR-LOCK-MISSION.md). Branch
`codex/mounted-save-and-door-lock`, worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\mounted-save-and-door-lock`, from
main `61519031` (= tag `v0.2.14`, the incident baseline). Manual review procedure by owner direction: Claude develops, Howie
takes this handoff and the commit to Pro. No agent-review controller enrollment exists for this work.

## 1. Status (read this first)

**Diagnostic checkpoint, plus one implemented correction (accurate failure reasons). The persistent blocker is NOT yet
identified and the incident is NOT fixed.**

- All four quicksave requests entered the deferred wait, which establishes that readiness was false at each activation.
  The log does not identify the blocking condition, does not establish that it was the same across requests, and does not
  measure any wait's duration. No source reading established the condition either (section 4). This checkpoint adds a
  single-authority, bounded observation that names the blocking owner (command, projectile, ability process, pair owner,
  preparation, transition, shell or charge fence), its refusing condition, and how each wait ended.
- Three requests then failed the wait's world/mode/loading check. The baseline checks player replacement separately in the
  readiness read and binds the player before the first tick, so on the ordinary path those three failures were a disallowed
  game mode or a manual loading screen. The log does not say which mode or screen. "The native world changed" misnamed them.
  The refusal decision is unchanged; the reason is now named ("the game menu opened while it waited for X"). This removes a
  misleading message only. It does not release any lock, shorten the wait or change readiness.
- The fourth request was still waiting when the log ends.
- No native run: there is no qualified KMC Working fixture on this machine, and the owner's save is not available here
  (section 9).

## 2. What happened in the incident log

Input: `C:\Dev\KingmakerMountedCombatLab\output_log(20261122-180838).txt`, 2,326,054 bytes, SHA-256
`8b0e422fd84d59cb9b700ea7fa3db039cfdb8d75afe72affbbd76773730edd30`, 10,797 LF lines (verified). Kept private, not committed.

| Lines | Event |
|---|---|
| 10783 | Request 1 (Quick, `Quicksave1`), `saveFence=False`, nothing queued. |
| 10784 | "Saving after current actions and projectiles finish": request 1 activated and was **not ready** at activation. |
| 10785, 10789 | Requests 2 and 3 queued behind it (`saveFence=True`; 10789 names `DeferredSaveEnumerator`1`). |
| 10786–10790 | Exploration observations show the party mostly standing (`stillFraction=0.83`) and new move orders barely progressing. These are movement-observation durations, not measurements of the save wait; the log has no timestamps that measure it. |
| 10791 | Request 1 refused: "the native world changed while its effects were finishing". |
| 10792–10795 | Requests 2 and 3 each activated ("Saving after…"), were not ready, and were refused with the same text. The adjacent lines fit a refusal inside activation but do not prove it. |
| 10796–10797 | Request 4, logged with `mode=Default`, nothing queued: activated, not ready again. The log ends there. |

Readings, each tied to source (corrected after the Pro review of `0b4b8fa4`, finding 4):

1. A save that is ready at activation serializes at once and never prints "Saving after…" (`DeferredSaveEnumerator.Activate`:
   `Waiting = !ready(); if (!Waiting) { StartSerialization(); return; }`). All four requests printed it, so readiness was
   false at each activation. That does not establish that the same condition or object blocked every request, or that it
   stayed unresolved between them. Each request released its own fence (terminal → `ReleaseUnstartedSaveFence`); the log
   proves repeated deferral, not one leaked fence.
2. None of the three recorded failures used the 30-second timeout text ("did not finish within the bounded wait"). "The native world
   changed" came only from the per-tick world check (`tick` in `DeferNativeSave`), which refused for
   `world == null || Player replaced || IsManualLoadingScreenActive || mode ∉ {Default, Pause}`. The baseline binds `world`
   in `beginWait` before the first tick and checks player replacement separately in `ready()` (with its own message), so on
   the ordinary path the three failures were a disallowed mode or a manual loading screen. Which menu, dialog, cutscene or
   screen is unknown. The wait lengths are unknown.
3. Doors: while any request waits, `SaveCommandStartPrefix` refuses `UnitCommand.Start` for every ordinary command of every
   actor (`NativeDeferredSave.Waiting`). A door interaction is an ordinary command, so the wait provides a source-supported
   explanation for that symptom. The incident log contains no door attempt and no observed recovery. Retiring the wait
   removes this particular hold; other command-admission conditions still exist, and recovery after the wait remains to be
   verified in the game.

## 3. Native contracts verified (Kingmaker 2.1.7b, `Assembly-CSharp.dll`, read-only IL via the installed UMM `dnlib`)

- `LoadingProcess.StartLoadingProcessInternal` `0x06007FC5`: `StartLoadingScreen` then `m_CurrentProcess = queued`.
  `TickLoading` `0x06007FC2`: when `MoveNext` returns false, it clears `m_CurrentProcess`, invokes the callback (nulled by KMC
  for a retired failure), then dequeues and starts the next queued process in the same call. That is why requests 2 and 3
  activated and failed back to back.
- `LoadingProcess.get_IsLoadingInProcess` `0x06007FBC` is `m_CurrentProcess != null || m_Queue.Count > 0`. KMC's postfix
  hides it while a request waits, so `Game.Tick` (which reads it) keeps the world running during the wait.
- `UnitCommand.Start` `0x060027A5` has no failure branch of its own: a prefix-refused command stays unstarted in its slot.
- `UnitCommands.RemoveFinishedAndUpdateQueue` `0x060026BD` promotes the queue head only when its own slot is empty. A Standard
  head also needs the Move slot empty. `UnitCombatState.AttackOfOpportunity` uses `Run` (slot, startable during a wait via
  `MayStartDuringWait`). `AbilityEffectStickyTouch.Apply` uses `AddToQueueFirst` (queue).
- Projectiles: `ProjectileController.Tick` `0x06009276` moves `m_NewProjectiles` into `m_Projectiles`, ticks each, marks
  `Cleared` on an exception and removes a projectile once `Destroyed` (hit + `LifetimeParticlesAfterHit`) or `Cleared`.
  `Projectile.Tick` only sets `IsHit`. The only `OnHit` caller is `ProjectileHitController.Tick` `0x06009284`, for
  `HitThisFrame` (`IsHit && TimeAfterHit < 0.0001`). `ProjectileController.Clear` is called only by rest, dialog start
  and `Stop`.
- `AbilityExecutionController.Tick` `0x06008FCC` ticks every process and removes the `IsEnded` ones.
- `UnitLifeController.SetLifeState` `0x06009164` interrupts all of a unit's commands on a life-state change.

## 4. Readiness terms against the evidence

`SaveEffectsReady` refuses on the first of these, in order:
1. A pair owner not save-ready.
2. Native preparation.
3. A relationship transition.
4. A relationship shell.
5. Any unit's command needing settlement.
6. A relationship-shell command.
7. An unresolved projectile.
8. A non-ended ability process.

The admission fence (Mounted Charge ownership or a native charge boundary) is checked before them. None of these can be
convicted from the log:

- **Charge fence:** a draining charge owner logs "Charge ownership barrier" on every attempt; the log has none. Unlikely, not
  excluded (a pending native charge boundary would not log).
- **Pair owner, preparation, transition, shell:** no Mount/Dismount, charge or turn-based activity after combat. Unlikely, not
  excluded.
- **Commands (all units in the area):** `CommandNeedsSettlement` counts any started unfinished command except
  `UnitMoveContiniously`, any unfinished `UnitAttackOfOpportunity` and a committed touch delivery even while queued. A
  started `UnitMoveTo`, a long-running NPC command or a stuck enemy action anywhere in the area would hold the save. This
  is the broadest term and the most plausible, but it is unproven.
- **Projectiles:** a projectile stays unresolved only until `OnHit` completes or it is `Cleared`/removed. A projectile that
  never reaches its target point (and has no `MaxRange`) would never resolve. TRE 0.0.194's firearm projectile clones the
  native crossbow blueprint (`FirearmProjectileBlueprints.cs`) and only adjusts visuals in `BeforeLaunch`. Nothing in source
  makes it unreachable. The owner's "projectiles are firing" keeps this open.
- **Abilities:** TRE's reload (`ReloadTestMusketAbilityLogic.Deliver`) completes in one step and yields one target, so it
  leaves no running process. Another caster's process is not excluded.
- **The second attack (10765–10768):** `cause=Unrecorded` with `CommandAdmissionFailure` is the fallback label of
  `DescribeTerminalFailure` for an interrupt with no recorded cause, not evidence of a failed admission. The reload was the
  `UnitUseAbility` substituted by TRE's `EmptyFirearmAttackCommandPatch`. Its `ReloadEndedPostfix` schedules `ResumeAttack`,
  which cancels on a dead target or no longer queues. No source path found that leaves a command, projectile or ownership
  obligation alive after it. Still a lead, not cleared.

Evidence gaps: which term held, and its owner. The Kingmaker native log for the session (only an unrelated later
`output_log.txt` exists on this machine). What the player opened during the wait.

## 5. What changed

| File | Change |
|---|---|
| `Domain/SaveWaitObservation.cs` (new) | `SaveWaitBlockerKind`; `SaveWaitWorldPolicy` (the world/mode/loading decision, unchanged in effect, plus named reasons, player text and the unchanged 30 s limit); `SaveWaitObservation` (per request, timed from activation: a line when the blocking owner or its refusing condition changes, at most 12 lines, then counted; the timed detail refreshed every 0.25 s of waiting without a line; one end line with outcome, reason, waited and queued time, the latest condition and detail and the time of that snapshot; holds strings, an identity number and times only). |
| `Integration/MountedDeferredSave.cs` | `SaveEffectsReady()` is now `EvaluateSaveEffects().Ready`. `EvaluateSaveEffects` evaluates the same terms in the same order and returns the first refusing term with its owner. `PinWorldSaveScope` and the combat save gate keep calling `SaveEffectsReady`. `DeferNativeSave`: same fence → fault → effects order; observes every readiness read; the tick names world replacement / loading screen / mode (with the mode) and the blocker being waited for; timeout text names the blocker; one end report per wait: ReadyToSerialize (the wait settled and native serialization actually started; the archive's own outcome stays with the existing commit machinery), TimedOut, ModeChanged, LoadingScreen, WorldReplaced, Faulted (any exception that ended the wait) or Cancelled (a disposal no fault, refusal or timeout caused). Wait times are measured from activation; queued time is reported separately. Every observation and report is contained: a failing logger or description cannot change the save, its fence, pause restoration or world release. |
| `Integration/NativeSaveEffectBoundary.cs` | `FirstUnresolvedProjectile` / `FirstUnresolvedAbility` are the single predicates (`HasUnresolved*` derive from them). Read-only describers: field reads only, no native tick, iterator or retained reference. Unity asset names are read only for a real asset, never inlined. |
| `Domain/MountedPairRegistry.cs`, `GameMountedRelationshipService.Ownership.cs`, `MountedWorldService.cs` | `AllSaveReady()` = `FirstUnreadySaveOwner() == null`. A pair's save readiness is its named refusal being absent (the same five terms, same order). |
| `Domain/DeferredSaveEnumerator.cs` | `TimedOut`, `Disposed` and `Failure` (the exception that ended the operation, recorded before its disposal) read-outs; optional timeout description (a failing one falls back to the old text); optional serialization-started notification, invoked only after native serialization started and contained. No behavior change. |

Not changed: readiness itself, the hold and its exceptions, the 30 s timeout, request ordering, Manual/Quick/Auto semantics,
pause handling, fences, the archive worker drain, save format. No coalescing or cancellation of queued requests.

Example of the new log for the incident's shape (fakes; names illustrative):

```
Mounted save wait: request=1; name=Quick:Quicksave1; t=0.00s; queuedFor=0.00s; blocker=Projectile; newOwner; waitingFor=Leinna's AcidSplash projectile; condition=[membership=active; isHit=True; cleared=False; hitObserved=False]; detail=[blueprint=…; launcher=…; target=…; passedTime=…]; change=1.
Mounted save wait ended: request=1; name=Quick:Quicksave1; outcome=ModeChanged; reason=mode=EscMode; manualLoadingScreen=False; worldBound=True; waited=16.10s; queuedFor=0.00s; lastBlocker=Projectile; …; snapshotAt=16.00s; …
Save did not complete; the previous complete save is unchanged. Save was not written: the game menu opened while it waited for Leinna's AcidSplash projectile.
```

## 6. Validation (offline only)

Budget `save-door-lock` (a new record for this incident; the shared `development` record had 10.37 of 90 min left from PR #57).

- **Failing on baseline, then passing.** The assertion "a menu opened during a deferred wait is named, not reported as a
  world change" was added to the existing `Test-Chunk7SaveBoundaryAdapter.ps1` template in a temporary detached worktree of
  `61519031` and run against the baseline source:
  `FAIL menu opened during a deferred wait is named, not reported as a world change System.Exception: refusal misnamed: Save was not written: the native world changed while its effects were finishing.`
  Log `artifacts\save-door-lock\baseline-61519031-chunk7-adapter-with-mode-assertion.log`. The same assertion passes on
  the candidate in `Test-DeferredSaveWait.ps1` (case 2).
- **Pre-existing failures, classified.** `Test-Chunk7SaveBoundaryAdapter.ps1` already fails on the pristine baseline:
  PASS=35 FAIL=47, cascading from "real drained commit can settle without invented native callback". Log
  `artifacts\save-door-lock\baseline-61519031-chunk7-adapter-pristine.log`, sha256 `cb5b808d…`. On the candidate (template
  updated only to splice the new production members) the log is byte-identical (same sha256), so every per-case verdict is
  unchanged. It is not used as a gate. These are among the known stale harnesses (T04–T08); not touched here.
- **New focused regression, `scripts/Test-DeferredSaveWait.ps1`.** It splices the production `DeferNativeSave`,
  `SaveReadiness`, `ObserveSaveWait`, `ReportSaveWaitEnd`, the native-queue adapter (`StartScreen`, `MoveNext`, `Waiting`) and
  the hold (`SaveCommandStartPrefix`, `DeferredSaveLoadingPostfix`) verbatim. Only game objects and the readiness evaluation
  are fakes. 10/10 cases:
  1. idle world saves at once with no wait, notice, hold or wait lines;
  2. menu during wait: refused before serialization, menu and blocker named, door held during and admitted after, pause
     restored, fence released once;
  3. three queued requests during the menu: each retires with its own named refusal and end line, none serializes,
     nothing holds afterwards, a later settled save does not wait;
  4. loading screen named;
  5. replaced world named;
  6. blocker changes reported once each, settled wait serializes with the pause restored;
  7. native cancellation → Cancelled, fence, pause and hold released;
  8. readiness fault → Faulted;
  9. failing logger changes nothing;
  10. no blocking object retained (weak reference).

  Six production mutations, each one line in a generated copy, all killed: generic world text (D1), outcome ignoring the
  refusal (D2), blocker not observed (D3), end not reported (D4), observer not contained (D5), hold ignoring the wait (D6).
- **Native-type contracts, `Test-PersistenceContracts.ps1`** (real Kingmaker assembly, uninitialized native objects):
  - first unresolved projectile is the exact active offender, read without changing `m_Updating`;
  - a launched projectile is named `pending`;
  - a delivered one shows `hitObserved=True`;
  - the exact unfinished ability process is named without being retired;
  - the exact started `UnitAttack` is described.

  The reflective enumerator construction now pads the new optional parameter with its default. 228/0.
- **Components:** 10 new (`SaveWaitObservationTests`): policy, every mode text, transitions, bound, contained descriptions,
  no retention, no-wait, timeout identity and text, timeout-description fault, cancellation identity.
- **Two more pre-existing failures, classified the same way.** Each was run once on base `61519031` from a temporary
  detached worktree in the lab layout:
  - `Test-ChargeOwnershipContracts.ps1`: 71 PASS, then "Parameter count mismatch" in a charge-owner reflective call. The
    candidate's per-check verdicts and terminal exception are identical. Log
    `baseline-61519031-Test-ChargeOwnershipContracts.log`.
  - `chunk7\Test-Chunk7SixPairNativeOwnership.ps1`: its facade lacks `MountedPairCandidate` and does not compile, on
    baseline and candidate alike. Log `baseline-61519031-Test-Chunk7SixPairNativeOwnership.log`.

  Neither can gate this change. The changed pair-readiness binding is covered by the production build and the component
  `MountedPairOwnershipTests`.
- **Fast tier** with focused gate `scripts/Test-SaveDoorLockFocused.ps1` (`Test-DeferredSaveWait.ps1`,
  `Test-PersistenceContracts.ps1`). It clears `PSModulePath` for its Windows PowerShell children, as
  `Test-CityToolbarProneFocused.ps1` does. Command:
  `$env:KMC_WORKFLOW_BUDGET_ID='save-door-lock'; .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-SaveDoorLockFocused.ps1`.
  Result **PASS** in 0.45 min: components 1483/0, Validate-Source 153/0, focused 2 of 2 (wait regression 10/0 with 6/6
  mutations killed; persistence contracts 228/0). Log `artifacts\save-door-lockast-r1e.log` (sha256 `ef0227e9…`). Built
  DLL `bin\Release\KingmakerMountedCombat.dll` sha256 `20d15ef555ba3cfd275ed0f3d6f7d23d4fb00af9b746b8a47ead216a052731dd`.
  This is a development build, not packaged or staged.

- **Round 2 (after the Pro review of `0b4b8fa4`).** Fast PASS in the same budget (4.53 of 90 min spent in total):
  - components 1487/0 (4 new review regressions);
  - Validate-Source 153/0;
  - focused `Test-SaveDoorLockFocused.ps1`: wait regression 15/0 with 8/8 production mutations killed; readiness
    equivalence against baseline 1,024 states, 0 failures; persistence contracts 228/0.

  Log `artifacts\save-door-lock\fast-r2.log` (sha256 `b9b70c35…`). DLL sha256
  `fc271c96db847b5b6b67cf76a819a8c455ca5dd796ab16bd68f3717ad9a7fdbe`. The chunk7 save-boundary adapter, updated only with
  the new members' fakes, keeps per-case verdicts identical to the pristine baseline (PASS=35 FAIL=47), log
  `candidate-r2-chunk7-adapter.log`.

Not covered offline: every native behavior. (Round 2 covers the real timeout path through `DeferNativeSave` with a
deterministic clock.)

## 7. Compatibility and safety

Single readiness authority preserved (diagnostics read the production result; `PinWorldSaveScope` unchanged). Observation is
read-only, bounded (12 transition lines per request plus one end line, nothing per frame) and contained. No native iteration
API is used (`m_Projectiles` / `m_NewProjectiles` / `m_Abilities` read by field, as before). No reference outlives the frame.
No save-format, action, preparation, resource or projectile mutation. TRE untouched. No cross-mod dependency.

## 8. Review round 1 (Pro, of `0b4b8fa4`): findings and dispositions

Verdict: request changes before using this as the diagnostic checkpoint. Pro found no change to save eligibility or the
command hold, and agreed the evidence does not identify the blocker. All four findings are accepted and corrected in the
round-2 candidate (commit after `0b4b8fa4`; section 10).

| Finding | Disposition | Correction and evidence |
|---|---|---|
| F1 [P2] the end report can repeat a stale snapshot of an unchanged owner | Accepted, fixed | `SaveWaitObservation` separates logging from state. A line is written when the owner **or its refusing condition** changes (`newOwner` / `sameOwnerConditionChanged`). The condition is the refusal reason, projectile membership/hit/cleared/observed, command state, or ability `ended`, without timing. The timed detail is refreshed every 0.25 s of waiting without a line. The end line carries the latest condition and detail with `snapshotAt`. Bounded output, containment and no retained native reference are unchanged. Regressions: component "same-owner condition change…" (same pair identity, new refusal; the end shows the new one and not the old) and "refreshes an unchanged owner's detail…" (same projectile goes pending→active and hit; timing alone never logs; the end shows the latest snapshot and its time); harness "the same blocking owner changing its refusing condition…" through the production observer. Mutation D8 (condition not observed) is killed. |
| F2 [P2] `Serialized` reported before serialization started; faults reported as cancellation | Accepted, fixed | The outcome is renamed `ReadyToSerialize` and reported only from a contained notification the enumerator invokes **after** native serialization actually started. Every other end is reported once from the operation's terminal, after `DeferredSaveEnumerator.Fail` has recorded `Failure`. Precedence: recorded refusal, timeout, fault (`Faulted`, reflected screen exceptions unwrapped), and only then `Cancelled`. The archive's outcome stays with the existing commit machinery. Pro's three probes are harness cases: screen start throws → `Faulted`, never `ReadyToSerialize`; first writer step throws after the wait settled → exactly one `ReadyToSerialize` and no second end line; begin-wait notification throws → `Faulted`, not `Cancelled`. Component case "records the fault… reports serialization only once started". Mutation D2 (fault reported as cancellation) is killed. |
| F3 [P2] queued time reported as waiting time | Accepted, fixed | The observation is created at activation (first readiness read inside `Activate`); lines report `t` and `waited` from activation and `queuedFor` separately. `DeferNativeSave` takes its clock from `SaveWaitClock()` (a `Stopwatch` per request in production), so the harness substitutes a deterministic clock. Pro's probe is a harness case through the real timeout: wrapped at 0, activated at 20, timed out at 50.01 → first line `t=0.00s; queuedFor=20.00s`, end `waited=30.01s; queuedFor=20.00s`, blocker named in the refusal. Mutation D7 is killed. |
| F4 [P2] handoff and records stated plausible explanations as facts | Accepted, fixed | Sections 1–2, CURRENT.md and AUTONOMOUS-RESUME.md now say only what the evidence supports. All four requests entered the wait. Three failed the world/mode/loading check (a disallowed mode or manual loading screen on the ordinary path, which one unknown). The fourth was still waiting. The blocker, its sameness across requests and the wait lengths are unknown. Door recovery after the wait is unverified. The resume rule now requires supported causal evidence (an instrumented trace or a faithful baseline reproduction of a source-proven defect), not specifically a game log. |

Pro's answers to the round-1 questions, recorded:
- Naming the blocker first is the right next step. The command gate remains worth investigating, but a dependency cycle
  is a hypothesis until shown reachable and reproduced.
- Keeping the menu/dialog refusal (not suspending the wait) is appropriate for this PR. Suspension would be a functional
  change needing explicit timeout, pause, queue and hold-release behavior.
- No counterexample to the evaluator's equivalence was found, but the earlier evidence only tested how the wait consumes a
  faked readiness result. **Added in round 2:** the harness now also splices the production `EvaluateSaveEffects` and
  `SaveEffectsReady` with the baseline `61519031` `SaveEffectsReady` (read with `git show`). Both read the same fake world
  for all 2^8 term combinations × raw/queued command × active/pending projectile (1,024 states) plus the no-world refusal.
  Readiness is identical; the reported kind is the first refusing term in the baseline order; the reported subject, unit,
  queue and pending flags are the exact objects. A one-off mutant that drops the preparation term fails 256 states, so the
  check discriminates.

## 9. Next reproduction and required inputs

Required to repair the blocker:
1. **A log from the game with this build** reproducing the state. The owner can do this on a copy of their save (section 11).
   The new "Mounted save wait" lines name the blocking owner directly.
2. Or **a disposable copy of the save** taken shortly before the incident, supplied by the owner under the seed-provenance
   directive (`docs/VALIDATION-POLICY.md`), to qualify a KMC Working fixture for a guarded native run. None exists here (the
   save root has no `KMC_AUTOMATION_*` save). The incident happened on another machine or session: this machine's
   `output_log.txt` is a different, later session.

Once the blocker is named, the repair targets that owner's lifecycle. A faithful regression reproduces it first and the
native acceptance is the formerly failing sequence: save, door, cold load.

## 10. Identities and records

- Baseline: `61519031` (main = `v0.2.14`). TRE reference: `v0.0.194` `805f9d59` (read with `git show`; that working tree is
  another session's and was not touched).
- Fast attempts under budget `save-door-lock` (all recorded; nothing reset), 3.57 of 90 min spent in total:
  1. `fast-r1.log` failed: the new harness host lacked `Get-FileHash` (now .NET hashing).
  2. `fast-r1b.log` failed in `Test-PersistenceContracts.ps1`: reflective construction lacked the new optional parameter
     (now padded), then a Unity internal-call inlining problem in the describers (asset names are now read in one
     non-inlined helper).
  3. `fast-r1c.log` failed in the pre-existing `Test-ChargeOwnershipContracts.ps1` (`Get-FileHash` under a PowerShell 7
     parent).
  4. `fast-r1d.log` failed in the two pre-existing baseline failures (section 6). They were then removed from the
     selection.
  5. `fast-r1e.log` **passed**.

## 11. Owner reproduction with this build (play on a copy of the save)

1. Install the candidate DLL from this commit's package (to be built after review), with the same mods as the incident
   (TRE 0.0.194, Waking Rune 0.12.3, Last Azlanti Preserver 0.1.3, CotW and the rest of the incident's list).
2. Load a save before the fight. With the mounted rider holding the Reliable Pistol +1 (automatic attacks on), fight until
   combat ends. If possible, let one target die mid-sequence and let an automatic attack reload and retarget.
3. Right after combat, press quicksave **once** and wait at least 35 s without opening any menu.
4. Click a door while the "Saving after…" notice is shown, then again after the save completes or fails.
5. Send `output_log.txt`. The lines that matter start with `Mounted save wait:` and `Mounted save wait ended:`. The end line
   says how the wait ended (for example `ReadyToSerialize`); whether the save was written is reported by the existing save
   messages.
