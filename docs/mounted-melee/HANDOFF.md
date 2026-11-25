# Mounted melee repair — handoff (0.2.16-alpha.6 private alpha)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md).
Install, rollback and playtest: [PRIVATE-ALPHA.md](PRIVATE-ALPHA.md). Issue ledger: [ACCEPTANCE_LEDGER.json](ACCEPTANCE_LEDGER.json).
Package record: [package-identity.json](package-identity.json).

## 1. Outcome

- **Factory:** foreground enrollment `kmc-codex-mounted-alpha2-melee-pose-20261124102246-64c1` (pinned controller
  `versions/85dece1c…`), draft PR #64 targeting `main`. Rounds 1–3 returned `changes_required`; round 3 stopped the enrollment
  on the no-progress rule. The owner resumed it with the original scope (`resume --reason "Owner requests completion of the
  full original MMR scope using a revised correction and production-path verification approach"`; no amendment, no new
  enrollment, limits unchanged). The continuation is saved verbatim at
  `analysis-cache/mounted-alpha2-melee-pose-20261124/owner-continuations/owner-continuation-2026-11-24-full-scope.md`.
  Round 4 (`b158efc3`, alpha.3) verified 7 of 12 prior findings and raised 8 new ones. Round 5 (`d032ff32`, alpha.4)
  verified 10 of the 13 prior fixes (R3-F1, R4-F1 and R4-F2 were rejected) and raised 9 new findings: 12 open, of which 11
  block readiness and R5-F9 is optional polish. The enrollment then stopped at its 5-round cap; the owner extended it to 7
  formal rounds (`resume --max-rounds 7`; continuation saved verbatim beside the first one as
  `owner-continuation-2026-11-24-extension-7-rounds.md`). Round 6 (`8b4f6507`, alpha.5) verified 10 of the 12 prior findings
  (R5-F2 and R5-F4 rejected) and raised R6-F1…R6-F4: 6 open, all blocking. This candidate is the round-7 correction of those 6
  (section 3); round 7 is the last formal round of the extension.
- **Scope:** all seven MMR outcomes are implemented at source (section 2).
- **Package:** `artifacts/KingmakerMountedCombat-0.2.16-alpha.6-mounted-alpha6-46444b30-diagnostic.zip` (section 5). The alpha.2 `c5b147a6`, alpha.3 `bd63035e`,
  alpha.4 `66492352` and alpha.5 `a0f741b5` packages are kept as history and are **not** playtest artifacts.
- **Native game launched: no** (section 4).

## 2. Issues

| Issue | Cause (status) | In alpha 4 | Offline evidence | Native/owner |
| --- | --- | --- | --- | --- |
| MMR-01 seating | **Hypothesis** from source: the rider anchor is fixed from the Chest as it stood when Mount Companion fired, and the seat was sampled from the rider's pose in that frame. | No literal basis. The Horse seat sample is normalized: the attach-frame anchor relation (`MountRootPositionOffset`) plus the rider's own rider-local pelvis, so the Chest's height at attach no longer enters the seat. It is acquired provisionally at attach and settles on the mean of a window that stays within 2 mm of its start for 0.5 s of *advancing* animation time (≥2 s total), only if the window shows the pose was actually evaluated (frame-to-frame change above 0.01 mm in at least 3 frames); paused, disabled and bit-frozen frames never settle it. For the next 30 s of animation a later evidenced resting window more than 1 cm away corrects it (at most twice; logged `phase=corrected`). The accepted correction is applied relative to it. Each mount logs `Horse seatCalibration: phase=attach` then `settled`/`unsettled` with `observedSeatFromSource`, `appliedSeatFromSource`, `anchorFromSource`, `seatCorrection`, `frames`, `animationSeconds`. Mammoth and Small unchanged; thresholds are design choices. | `Test-MountedAlpha2SeatAttachment.ps1` 14/0 on the actual runtime and adapter members with production rider-root sync: different Chest heights at attach and after view replacement give the same pelvis-from-Chest; a cast held paused through 120 updates settles only after resuming, at idle, matching remount; a frozen cast with positive game time and an enabled manager does not settle and, resumed at idle, matches remount; an evaluated held cast is corrected once to idle; a breathing idle settles once with no correction over 32 s; sub-2 mm drift settles only when still; restoration, one adapter, Mammoth/Small unaffected; each pre-fix behaviour reintroduced fails. | N01, N10 |
| MMR-02 TB approach | Native `IgnoreClick` checks the predicted rider `UnitAttack` radius against the mount's visualized path (or the rider position), while KMC's strike is admitted from the mount origin. | When native drops the exact predicted plain melee `UnitAttack` of the selected, addressable rider on the highlighted hostile (rider attack admitted by the dispatcher), the click is admitted only if (a) the strike's production admission passes where the pair stands, or (b) the mount agent's native route, walked by the production `MountedRouteWalker` within the grant (the point at exactly the grant is itself evaluated and refined back to the sight window's start) to the first point where the strike's production admission passes (supported rider offset and eye **rotated with the mount's heading** along the route, command LOS), costs no more than the mount's grant **in paired execution's own terms** (`PairedMountGrantedMetres`: addressable, able to act, can move, not prone, mirrored step/single-move context, and the allocation's distance **across the order** — into the Standard conversion unless single-move, step, auto-stop, restriction or a spent Standard ends it, as `MountedMovementState.Advance` grants it) — and every carrier of that order is created only on a route **revalidated from its current origin and grant** by the same walk (`ForcedPath`, which native turn-based `TickApproaching` prefers to any visualizer path) on a point command whose destination is the verified endpoint with a 0.05 m arrival radius, so native's proximity rule cannot complete it early; when the proof cannot be re-established the approach ends cleanly, with no native fallback. Otherwise native's refusal stands and the previous order is untouched. Each drop logs decision, mount mode and grant reason. | Policy tests (step, ordinary move, in range after spent movement, over budget, failed route, endpoint out of reach/sight, ended/immobile/unaddressable horse, refused Standard, non-attack prediction, accepted click untouched). Walker tests (first legal reach between samples, grant just inside/short, corner, shifted start). `Test-MountedAlpha2AttackPathHarness.ps1` runs the actual `BeginDelegatedMove`/`TickApproach`/revalidator at a corner: shifts of 0/0.1/0.3 m recost (2.6/2.7/2.9 m) and the carrier follows the revalidated route; a grant that no longer covers it ends the approach with no carrier or fallback; a sight window 1.412–1.428 m between samples passes at grant 1.42 (stop at 1.412, one strike) and refuses at 1.40; a horse inside the old point-command radius with the rider blind travels 1.4 m along the flank to the verified endpoint and strikes once (old command: completes without moving); a right-angle turn with a 0.4 m rider offset predicts the admitted point (translated offset: blocked). Order-allowance test: admission equals `Advance` execution across the conversion (mount pays; limits hold). | N02, N03 |
| MMR-03 horse participation | Default paired TB excluded the horse; round 3: the rider's own approach counted as newer work. | `MountedRetainedOrderLedger`: kind fixed at acceptance; own wrapper and exact delegated moves recorded before native Run (repaths included); only natively accepted work classified (private Run postfix, `AddToQueueInternal` postfix); finished identities released each update; an explicit order yields only to work accepted after it, automatic orders also to occupied slots; native queue promotion (`fromQueue`) is never a new acceptance. | Ledger, ordering (older slot work vs newer rider click, both orders; promotion of older queued work vs insertion after the click) and release tests; acceptance-seam contract. | N04 |
| MMR-04 RT engagement | Traced from installed code and data (section 6). | No targeting system added. For a rider **melee** proposal only, among candidates native ranked **equal** to its proposal (in reach) and would score equally, the pair takes the nearest (live order kept; different weights never reordered; a ranged proposal keeps the brain's choice). The report names native's `proposed` target and the pair's `selected` target separately. Per-order engagement record; travel credited only when the same exact carrier owned the mount at both ends of an interval. Each proposal logs native's own ranking. Any mount Hold now restricts automatic movement (below). | Tie-break, attribution and model tests; installed `Character_Brain` bytes pinned. | N05 |
| MMR-05 RT response | Movement waited for strike readiness. | An out-of-reach melee order may be dispatched before readiness for its own actor (a rider order for the rider, an explicit horse order for the horse; temporary readiness never converts one into the other), decided by the planned strike's production admission (probed only for an actual early-dispatch decision); native `ShouldStartCommand` keeps the start. A rider strike's own delegated move approaches pre-start; a horse strike approaches with native's own approach of that command, its sight and radius set before the first classification. A queued order (behind a native prerequisite) moves only after promotion, as an unmounted one. An unreleased automatic strike ends quietly when its target leaves the rider group's perception — at pre-start, any native start (in range included), arrival, repath, carrier creation and the release boundary — releasing only its exact carrier. | `Test-MountedAlpha2AttackPathHarness.ps1` 27/0 on the actual pair-command members: a queued horse strike behind an obstruction does not move while queued, approaches with sight on after promotion and starts once when sight clears (unobstructed control: one start, no approach); a hidden automatic target before start and at arrival yields zero starts and releases only the exact carrier (successor kept; perceived and explicit controls start once). Policy tests and contract as before. | N06 |
| MMR-06 horse Trip | Stock Mammoth `AddFacts` carry `TrippingBite`. | New horses: grant withheld. Existing horses: only on the owner's request. Candidates are **logged only**; the request/result live in `<save data parent>\KingmakerMountedCombat\` (never the Mods tree). The whole request must be exactly one `horse=<UniqueId>` line (comments/blank lines allowed). The request is claimed (`.claimed`) first and every selection and disposition is made from the claimed file's bytes (a request replaced before the claim is judged as replaced; one written after it is a separate later request; a waiting claim yields to a newer request as `.superseded`; a transient read or lookup failure after the claim keeps the exact claim for the next poll in the same session, and only a claim from an earlier game session is closed as `.interrupted`); the removal is recorded in the session before reporting, so a failed result write finishes as applied without a second removal (also after a reload); a stranded claim from an earlier session closes as `.interrupted` with nothing removed. | `HorseTripRepairPolicyTests` on real temporary folders: replacement A→B between observation and claim for an applicable and a refused request (removed fact, claimed bytes, result and archive agree), writes after the claim, one-time read/lookup failures after the claim (next poll: one removal, matching applied archive), failure injection at every step, staged Mods tree unchanged; genuine-object contract. | N07 |
| MMR-07 bite animation | Installed `HorseAnimationSet_Bite` → `BiteAttack`. | No animation change. A strike planned without a handle after an earlier recorded handle gets no fallback, assignment or second record (counted, logged once); binding faults never reach the native postfix. | Binding tests; bite contract on genuine handle/action/manager objects, incl. adopt/supply-then-missing and a later hoof strike. | N08 |
| Mounted Shield | Unchanged. | — | Shield 114/0, rollback 68/0, icon `fd519874…`. | N09 |

## 3. Review dispositions (round 7)

Round 6 verified R3-F1, R4-F1, R4-F2, R5-F1, R5-F3, R5-F5, R5-F6, R5-F7, R5-F8 and R5-F9 (10 of 12) and rejected R5-F2 and
R5-F4. Open: those two plus R6-F1…R6-F4, all blocking. Corrections: `14201310` (R6-F3), `0ba1d622` (R6-F4), `f36eb1eb` (R5-F4),
`74494116` (R5-F2, R6-F1, R6-F2, with harness evidence), `46444b30` (version). Earlier verified findings are unchanged.

## 4. Validation, native status and limitations

- **Offline (integrated tree `46444b30`):** components 1563/0; Validate-Source 153/0; native contract 66/0; lifecycle contract
  66/0; bite contract 28/0; seat attachment 14/0; attack-path harness 39/0; chunk7 slot harness 8/0; package `Validate-Package`
  11/0 and receipt 9/0. Gate receipts are in the developer report.
- **Native execution: none.** This session has no desktop/game input tool (browser automation only), so a fresh Beneath the
  Stolen Lands fixture cannot be created through character creation or played; the guarded save-backed scenarios still
  require the historical fixtures; the no-save `mod-load-smoke` route stays inadmissible. No pre-native review (no admissible
  route); zero guarded starts or fixture minutes; nothing to restore. Rows N01–N10 remain owner observations.
- **Known baseline failures (not selected; identical on `122a9f94`):** `Test-MountedAttackStallRepair.ps1`,
  `Test-WorldOwnershipContracts.ps1`.
- **Unobserved / behaviour notes:** rendered seat and settle timing (a mount made mid-fight may settle on the combat stance; a rider whose idle is bit-static never settles and keeps the attachment sample; a culled animator showing a moving pose can still settle, corrected only within 30 s);
  real routes and turn accounting; RT timing; live engagement; repair file IO; visible bite; save/load. A mount Hold left by an
  earlier build's carrier in an old save now keeps automatic attacks still until Stop or a new order clears it. In real time a
  horse that becomes ready first attacks after the rider's waiting strike, not before it. A click-admitted order's carriers always
  revalidate; if the proof expires the approach ends rather than falling back to native routing.

## 5. Exact identities

- Base: main `783b2b3dea3303e10cc4379dfa0a36ceac66a76b`. Packaged source: `46444b302ea9a0c77950dcf28f31d5472b9d3992`.
- ZIP: `KingmakerMountedCombat-0.2.16-alpha.6-mounted-alpha6-46444b30-diagnostic.zip`, 4,311,613 bytes, SHA-256
  `93ef3edac36c3cc47d1bb279e3d82bee3e0814ee39d37331223e2660e35ee115`; Info.json `0.2.16-alpha.6`, ManagerVersion `0.31.1`.
- DLL: SHA-256 `a600244819aee9cd0b44ad54215198447aa2d8dde7ca322f23f4921cc8561ad0`, MVID `31c65ba0-564b-422d-9c29-5a19dd71a485`.
- Mounted Shield icon: SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`.
- History (not for playtest): alpha.2 `c5b147a6`, alpha.3 `bd63035e`, alpha.4 `66492352`, alpha.5 `a0f741b5` (`26f73f66…`). Rollback: alpha.1 (`d7143ba3…`).

## 6. Native target comparison (MMR-04, installed 2.1.7b)

`Character_Brain` actions: `ManualAttackAiAction` (BaseScore 10, UseWhenAIDisabled; actor HasManualTarget 1/0; target
ManualOnly 1/0/0) then `AttackAiAction` (BaseScore 1; target `AttackTargetsPriority` = StandStill InRange 1.0/0.8 ×
UnconsciousPenalty LifeState 1/0/0.001 × NotImpatient 0.5/1.0). Candidates: the rider group's memory enemies minus dead,
untargetable, aggro-filtered and (for the party) conscious enemies unaware of the party; a live manual target is added. Sort:
manual +1,000,000; in action range +1 if last target, else 0; outside range in real time −0.001 × distance (List.Sort is unstable
among equal weights). Validity: with the rider's AiMovementForbidden or a target in AIData.UnreachableUnits, only an in-range
target in LOS is valid. The first strictly best score wins. Consequences: a live valid manual target is kept; otherwise a target
in reach beats one needing movement; among targets needing movement the nearest eligible wins; among equally ranked in-reach
targets native's choice is arbitrary, which the pair resolves to the nearest.

## 7. Owner decisions

Playtest per PRIVATE-ALPHA.md. Merge, release and installation are the owner's. No merge, tag, release or installation was done.
