# Mounted melee repair — handoff (0.2.16-alpha.3 private alpha)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md).
Install, rollback and playtest: [PRIVATE-ALPHA.md](PRIVATE-ALPHA.md). Issue ledger: [ACCEPTANCE_LEDGER.json](ACCEPTANCE_LEDGER.json).
Package record: [package-identity.json](package-identity.json).

## 1. Outcome

- **Factory:** foreground enrollment `kmc-codex-mounted-alpha2-melee-pose-20261124102246-64c1` (pinned controller
  `versions/85dece1c…`), draft PR #64 targeting `main`. Rounds 1 (`2769f113`), 2 (`b108dcea`) and 3 (`79a26474`) returned
  `changes_required`; round 3 stopped the enrollment on the no-progress rule. The owner resumed it with the original scope
  (`resume --reason "Owner requests completion of the full original MMR scope using a revised correction and production-path
  verification approach"`; no amendment, no new enrollment, limits unchanged). The continuation is saved verbatim at
  `analysis-cache/mounted-alpha2-melee-pose-20261124/owner-continuations/owner-continuation-2026-11-24-full-scope.md`
  (ordinary task evidence, outside the repository and controller state). This candidate is the round-4 correction.
- **Scope:** all seven MMR outcomes are implemented at source (section 2). Nothing is narrowed or deferred by amendment.
- **Package:** `artifacts/KingmakerMountedCombat-0.2.16-alpha.3-mounted-alpha3-bd63035e-diagnostic.zip` (section 5). The
  blocked `0.2.16-alpha.2` package `c5b147a6` (`d82368ce…`) is kept as historical evidence and is **not** a playtest artifact.
- **Native game launched: no** (section 4).

## 2. Issues

| Issue | Cause (status) | In alpha 3 | Offline evidence | Native/owner |
| --- | --- | --- | --- | --- |
| MMR-01 seating | **Hypothesis** from source: the animated saddle sampled `seatFromSource` from the rider's pose in the frame Mount Companion fired. | The Horse declares its seat basis (`AnimatedSaddleCalibration`): rider pelvis minus Horse Chest in the mount root's local frame, `(-0.1892, 0.8601, 0.0175)`. **Provenance:** only X ≈ −0.1892 (Chest X = 0) is a documented preview.3 final-render observation (docs/PHASE3H-IMPLEMENTATION.md, `5b25b96e`); Y/Z first appear as that commit's test constants and the run's raw seat log is not retained, so they are not independently verified. That the basis equals an idle/remount sample is a hypothesis; the seat line logs `observedSeatFromSource` so the owner's log can check it. | `Test-MountedAlpha2SeatAttachment.ps1` 6/0 splices the **actual** runtime attach/restore/replacement members and nearly all of `MountedRiderPoseAdapter` (rig resolution, lease, Configure, `ConfigureAnimatedSaddle`, `AcquireSaddleCalibration`, Update/LateUpdate, `ApplyAnimatedSeat`, Deconfigure) with the real Domain lease/saddle/calibration over a Unity hierarchy facade; only leg IK (`ApplyPose`) is stubbed to its seat write. Cast vs idle first mount seat identically with the declared basis; dismount restores the exact baseline and destroys the adapter; remount; view replacement restores the old view with one adapter; Mammoth/Small unaffected. Removing the declared seat fails 3/6. The old chunk7 slot harness had gone stale (stubs updated, 8/0). | N01, N10 |
| MMR-02 TB approach | Installed `TurnController.IgnoreClick` checks the predicted rider `UnitAttack` radius (rider corpulence + target + reach) against the **mount's** visualized path (KMC transpiler), or the rider position when the path is empty, while the strike KMC executes is admitted from the mount origin (mount corpulence + target + planned range, command LOS). | **Repaired:** when native drops the exact predicted plain melee `UnitAttack` of the selected, addressable rider on the highlighted hostile, the click is admitted only if (a) the strike's production admission (an unrun `MountedPairSingleAttack`) passes where the pair stands, or (b) the mount's own agent finds a complete native route (`UnitMovementAgent.FindPath`, pair radius, LOS) whose cost is within the mount's granted movement for the player's step/single-move mode and whose endpoint admits the strike (`UnitCommand.IsUnitCloseEnough`, rider eye offset). Otherwise native's refusal stands and the previous order is untouched. One log line per drop names the decision and its inputs. | Policy tests: legal step, ordinary movement, in-range after spent movement, over budget, failed route, endpoint out of reach/sight, non-attack prediction, unaddressable rider (other initiative arrangement), refused Standard, accepted click untouched. Contract: the postfix calls the policy, the probe and the mount allowance; the probe uses the production admission, native `FindPath` (the same call native path preview makes) and `IsUnitCloseEnough`. | N02, N03 |
| MMR-03 horse participation | Default paired TB excluded the horse from a rider order; round 3: the rider's own delegated move counted as newer work. | One melee rider order includes the horse on its own eligible paired activation. `MountedRetainedOrderLedger` fixes the attack kind at acceptance, records the order's own wrapper and every exact delegated move before native Run (repath and pre-start included), and classifies only work native **accepted** (private Run postfix = slot; `AddToQueueInternal` postfix = queue). Accepted competing work on either actor keeps the horse out; reactions, the pair's own commands, AI work and coexisting Free/Swift do not; a Free/Swift that must move its actor does. | Ledger tests through the production object (own move + repath keep follow-through; genuine player move refuses; queued, completed, refused, Free/Swift, reaction, pair, AI); attack kind at acceptance survives a deferred drive and weapon change. Contract: every native queue insertion is inside `AddToQueueInternal`, every slot acceptance (`OnRun`) inside private `Run`; observation only from their postfixes. | N04 |
| MMR-04 RT engagement | Traced from installed code and data (section 6). | No target selection added. Engagement record per automatic order (window opens at first update **or** first proposal; own vs player displacement); every real-time proposal carries native's own ranking (brain context `m_Enemies` with distance/weight, best score). The mount's Hold now restricts automatic movement only when the player put it there (below). | Engagement attribution tests; model of the traced comparison (two eligible, nearer excluded/unreachable/unconscious/disliked, in-reach preference, manual target kept, held rider); contract pins the installed `Character_Brain` actions and considerations byte-exact and the reader's brain fields. | N05 |
| MMR-05 RT response | Confirmed: movement waited for strike readiness (KMC Wait; native start gate before the pair command's approach). | **Repaired:** a real-time out-of-reach melee order is dispatched while the rider's Standard waits only on native timing; native `ShouldStartCommand` still decides the start (initiative, cooldown, hands). While unstarted, native `TickApproaching` drives the pair command's own delegated move (one movement owner); it stops at legal range, re-approaches a moved target, releases on Hold/hidden target/turn-based, waits for a newer mount order, retries a failed route at most 3 times, and is adopted at start. Release never stops a successor's movement. | Policy tests (admission before readiness, keep/reissue/stop/wait/bounded retry); contract: hook wiring, no `CanStart`/`IsIgnoreCooldown` override or `IgnoreCooldown` call, moves announced before Run, adoption at start, native start gate reads `CanActInCombat` and the cooldown. | N06 |
| MMR-06 horse Trip | Stock Mammoth `AddFacts` carry `TrippingBite` (`ManeuverOnAttack(Bite, Trip)`). | New horses: the exact grant is withheld (unchanged). Existing horses: **no automatic deletion.** A sourceless exact stock TrippingBite on the KMC Horse is reported (log line + `horse-trip-repair-candidates.txt` in the mod folder); the owner removes it from that one horse with `horse-trip-repair-request.txt` (`horse=<UniqueId>`), applied once under the shared write gate, verified, consumed (`.applied`/`.refused`, result file). | `HorseTripRepairPolicyTests` (5): exact request; legacy-ordered copy without request stays; sourced copy, other shape/GUID, wolf/dog, ambiguous, malformed refused; pending/not loaded/gate closed; failed removal retried; no re-application after reload. Contract on genuine objects (57/0). | N07 |
| MMR-07 bite animation | Installed `HorseAnimationSet_Bite` → clip `BiteAttack`; native gives a routine one `SpeedScale`. | No animation change. Handle binding and strike reader are production classes (`HorseStrikeHandleBinding`) behind narrow boundaries; a later stock handle of a different action is now a counted rejection instead of an exception in the native postfix. | 7 binding/reader tests; `Test-MountedAlpha2BiteContract.ps1` 24/0 on genuine handle/action/manager objects (adopt once, refresh, refusals, hoof control, fallback cardinality) and IL: `OnAction` calls `ObserveHorseStrike` and base `OnAction` exactly once. Formatter test labelled formatter-only. | N08 |
| Mounted Shield | Unchanged. | — | Shield 114/0, rollback 68/0, icon `fd519874…` in the packaged DLL. | N09 |

## 3. Review dispositions (round 4)

Round 3's twelve blocking findings, all addressed in this candidate (sources: `9e5ad5f3` lifecycle/movement/engagement,
`ed1d83e2` seat harness, `80048c77` saved horses, `bd63035e` bite path): R3-F3, R1-F5, R2-F5, R2-F1 (retained-order ledger and
acceptance seams); R1-F8 (Hold provenance, carrier Hold restore, Run keep); R3-F1 (pre-start approach); R3-F2 (click
admission); R1-F1 (explicit saved-horse repair); R1-F11 (traced comparison, per-order record, native ranking); R1-F12 (seat
harness); R1-F13 (bite production path); R3-F4 (checklist, PRIVATE-ALPHA). Earlier verified findings are unchanged.

## 4. Validation, native status and limitations

- **Offline (merged tree `bd63035e`):** components 1551/0; Validate-Source; alpha-2 native contract 57/0; lifecycle contract
  39/0; bite contract 24/0; seat attachment 6/0; chunk7 slot harness 8/0; package `Validate-Package` 11/0, receipt 9/0.
- **Controller focused gate:** on `bd63035e` the gate ran every child and failed only `Test-MountedAlpha2Package.ps1`, because
  the package record still named the alpha.2 ZIP (`Validate-Package`: packaged Info.json ≠ repository alpha.3); receipt
  `gate-focused-bd63035e5c40-d43d449b6c4-ae9890c77f2-20261124T204426252510Z` (preserved). The records head adds only this
  record and the package identity and is gated with the receipt child; its receipt is in the developer report.
- **Native execution: none.** Capability check: this session has no desktop/game input tool (only browser automation), so a
  fresh Beneath the Stolen Lands fixture cannot be created through character creation or played; the guarded save-backed
  scenarios still require the historical qualification fixtures; the no-save `mod-load-smoke` route remains inadmissible. No
  supplemental pre-native review was run (no admissible live route). Zero guarded starts, zero fixture/setup minutes, no
  restoration needed. Dependent rows: N01–N10.
- **Known baseline failures (not selected; identical on `122a9f94`):** `Test-MountedAttackStallRepair.ps1`,
  `Test-WorldOwnershipContracts.ps1`.
- **Unobserved:** rendered seat, real routes and step accounting in TB, real-time timing, live engagement, saved-horse file
  repair, visible bite, save/load. RT ordering change: while the rider's early-dispatched order waits for its cooldown, a horse
  that becomes ready first attacks after the rider's strike rather than before it.

## 5. Exact identities

- Base: main `783b2b3dea3303e10cc4379dfa0a36ceac66a76b`. Packaged source: `bd63035e5c4082b47111f9cf2621988133500fc2`.
- ZIP: `KingmakerMountedCombat-0.2.16-alpha.3-mounted-alpha3-bd63035e-diagnostic.zip`, 4,304,071 bytes, SHA-256
  `3be62a3d4a95dbef235f5964743e0f4866ddd020d56d9386d85ecb521808850f`; Info.json `0.2.16-alpha.3`, ManagerVersion `0.31.1`.
- DLL: SHA-256 `31e798c7e8515b065d14cc67f65c4e1d7ef179de6f40b9494a6f2ffc74559a4b`, MVID `9277a9f6-0fd0-4da0-877f-cacdfe91dc55`.
- Mounted Shield icon: SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`.
- Historical (not for playtest): alpha.2 `c5b147a6` ZIP `d82368ce…`. Rollback: alpha.1 ZIP `d7143ba3…`.

## 6. Native target comparison (MMR-04, installed 2.1.7b)

`Character_Brain` actions: `ManualAttackAiAction` (BaseScore 10, UseWhenAIDisabled; actor HasManualTarget 1/0; target
ManualOnly 1/0/0) then `AttackAiAction` (BaseScore 1; target `AttackTargetsPriority` = StandStill InRange 1.0/0.8 ×
UnconsciousPenalty LifeState 1/0/0.001 × NotImpatient 0.5/1.0). Candidates: the rider group's memory enemies minus dead,
untargetable, aggro-filtered and (for the party) conscious enemies unaware of the party; a live manual target is added.
Sort: manual +1,000,000; in action range +1 if last target, else 0; outside range in real time −0.001 × distance. Validity:
with the rider's AiMovementForbidden or a target in AIData.UnreachableUnits, only an in-range target in LOS is valid. The first
strictly best score wins. Consequences: a live valid manual target is kept; otherwise a target already in reach beats one
needing movement; among targets needing movement the nearest eligible wins. KMC's pair-specific inputs: the rider is the brain
unit (distances from the rider's position, which follows the mount), and a failed mount approach records the target in the
rider's UnreachableUnits.

## 7. Owner decisions

Playtest per PRIVATE-ALPHA.md. Merge, release and installation are the owner's. No merge, tag, release or installation was done.
