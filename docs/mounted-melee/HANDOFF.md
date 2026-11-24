# Mounted melee repair — handoff (0.2.16-alpha.4 private alpha)

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
  Round 4 (`b158efc3`, alpha.3) verified 7 of 12 prior findings and raised 8 new ones; this candidate is the round-5
  correction of all 13 open findings (section 3). Round 5 is the last formal round the enrollment allows.
- **Scope:** all seven MMR outcomes are implemented at source (section 2).
- **Package:** `artifacts/KingmakerMountedCombat-0.2.16-alpha.4-mounted-alpha4-66492352-diagnostic.zip` (section 5). The
  alpha.2 `c5b147a6` and alpha.3 `bd63035e` packages are kept as history and are **not** playtest artifacts.
- **Native game launched: no** (section 4).

## 2. Issues

| Issue | Cause (status) | In alpha 4 | Offline evidence | Native/owner |
| --- | --- | --- | --- | --- |
| MMR-01 seating | **Hypothesis** from source: the animated saddle sampled `seatFromSource` from the rider's pose in the frame Mount Companion fired (cast clip, approach walk, a view's first frame after load). | No literal basis. The Horse saddle is acquired provisionally from the attachment sample, then settles once on the rider's **own still pose**, measured live in the Horse mount root's frame (rider pelvis minus Chest, after native animation; `SettledSeatSampler`: ≥120 frames, within 2 mm for 20 frames, at most 1800). The accepted Horse correction is applied relative to it. Every attachment logs `Horse seatCalibration: phase=attach` then `phase=settled` (or `unsettled`) with `observedSeatFromSource`, `appliedSeatFromSource`, `seatCorrection`, `frames`. Mammoth and Small unchanged. Thresholds are design choices, not measurements. | `Test-MountedAlpha2SeatAttachment.ps1` on the actual runtime attach/restore/replacement and adapter calibration/settle/notice/frame members: cast→idle provisional then settled with both log lines from the real logger; remount and mounted-load settle identically; view replacement with one adapter; a restless pose keeps the provisional seat; Mammoth/Small unaffected; disabling settling fails 5/7. Unit test of the sampler. | N01, N10 |
| MMR-02 TB approach | Native `IgnoreClick` checks the predicted rider `UnitAttack` radius against the mount's visualized path (or the rider position), while KMC's strike is admitted from the mount origin. | When native drops the exact predicted plain melee `UnitAttack` of the selected, addressable rider on the highlighted hostile (rider attack admitted by the dispatcher), the click is admitted only if (a) the strike's production admission passes where the pair stands, or (b) the mount agent's native route, walked until the strike's production admission passes at a projected position (supported rider offset, command LOS), costs no more than the mount's grant **in paired execution's own terms** (`PairedMountGrantedMetres`: addressable, able to act, can move, not prone, mirrored step/single-move context, allocation) — and that order's first carrier then **follows exactly that validated route** (`ForcedPath`, which native turn-based `TickApproaching` prefers to any visualizer path). Otherwise native's refusal stands and the previous order is untouched. Each drop logs decision, mount mode and grant reason. | Policy tests (step, ordinary move, in range after spent movement, over budget, failed route, endpoint out of reach/sight, ended/immobile/unaddressable horse, refused Standard, non-attack prediction, accepted click untouched). Contract: the grant helper shares `CanMovePairedMount`'s terms; the click reads no rider step flags; the probe walks native `FindPath` with the position admission (offset, LOS); the carrier sets `ForcedPath`; native `TickApproaching` reads `ForcedPath` before `CurrentPathForUnit`. | N02, N03 |
| MMR-03 horse participation | Default paired TB excluded the horse; round 3: the rider's own approach counted as newer work. | `MountedRetainedOrderLedger`: kind fixed at acceptance; own wrapper and exact delegated moves recorded before native Run (repaths included); only natively accepted work classified (private Run postfix, `AddToQueueInternal` postfix); finished identities released each update; an explicit order yields only to work accepted after it, automatic orders also to occupied slots. | Ledger, ordering (older slot work vs newer rider click, both orders) and release tests; acceptance-seam contract. | N04 |
| MMR-04 RT engagement | Traced from installed code and data (section 6). | No targeting system added. Among candidates native ranked **equal** to its proposal (in reach) and would score equally, the pair takes the nearest (live order kept; different weights never reordered). Per-order engagement record; travel credited only when the same exact carrier owned the mount at both ends of an interval. Each proposal logs native's own ranking. Any mount Hold now restricts automatic movement (below). | Tie-break, attribution and model tests; installed `Character_Brain` bytes pinned. | N05 |
| MMR-05 RT response | Movement waited for strike readiness. | An out-of-reach melee order may be dispatched before readiness for the selected striker — the rider, or the horse when the rider may not strike or the order is the horse's — decided by the planned strike's production admission; native `ShouldStartCommand` keeps the start. A rider strike's own delegated move approaches pre-start; a horse strike approaches with native's own approach of that command. A queued order (behind a native prerequisite) moves only after promotion, as an unmounted one. An automatic order ends quietly when its target leaves the rider group's perception (pre-start, start, repath, carrier creation). | Policy tests (striker, early dispatch, queued → promoted, keep/reissue/stop/wait/bounded retry); contract (hooks, no `CanStart`/`IgnoreCooldown`, native ticks only slots, perception checks). | N06 |
| MMR-06 horse Trip | Stock Mammoth `AddFacts` carry `TrippingBite`. | New horses: grant withheld. Existing horses: only on the owner's request. Candidates are **logged only**; the request/result live in `<save data parent>\KingmakerMountedCombat\` (never the Mods tree). The whole request must be exactly one `horse=<UniqueId>` line (comments/blank lines allowed). The request is claimed (`.claimed`) before any change; the removal is recorded in the session before reporting, so a failed result write finishes as applied without a second removal (also after a reload); a stranded claim from an earlier session closes as `.interrupted` with nothing removed. | `HorseTripRepairPolicyTests` on real temporary folders (staged Mods tree hashed unchanged) with a failure injected at every step; genuine-object contract. | N07 |
| MMR-07 bite animation | Installed `HorseAnimationSet_Bite` → `BiteAttack`. | No animation change. A strike planned without a handle after an earlier recorded handle gets no fallback, assignment or second record (counted, logged once); binding faults never reach the native postfix. | Binding tests; bite contract on genuine handle/action/manager objects, incl. adopt/supply-then-missing and a later hoof strike. | N08 |
| Mounted Shield | Unchanged. | — | Shield 114/0, rollback 68/0, icon `fd519874…`. | N09 |

## 3. Review dispositions (round 5)

Open after round 4: R3-F1, R3-F2, R1-F8, R1-F11, R1-F12 (prior) and R4-F1…R4-F8 (new). All are addressed in this candidate:
`cb253463` (R3-F2, R4-F1, R3-F1, R1-F8, R4-F2, R1-F11, R4-F3, R4-F8), `07890d08` (R4-F7), `e5d02e40` (R4-F4, R4-F5, R4-F6),
`89f5482b` (R1-F12), `66492352` (version). Round-4 verified findings are unchanged.

## 4. Validation, native status and limitations

- **Offline (merged tree `66492352`):** components 1557/0; Validate-Source; native contract, lifecycle contract, bite contract,
  seat attachment, chunk7 slot harness all passing; package `Validate-Package` 11/0 and receipt 9/0. Gate receipts are in the
  developer report.
- **Native execution: none.** This session has no desktop/game input tool (browser automation only), so a fresh Beneath the
  Stolen Lands fixture cannot be created through character creation or played; the guarded save-backed scenarios still
  require the historical fixtures; the no-save `mod-load-smoke` route stays inadmissible. No pre-native review (no admissible
  route); zero guarded starts or fixture minutes; nothing to restore. Rows N01–N10 remain owner observations.
- **Known baseline failures (not selected; identical on `122a9f94`):** `Test-MountedAttackStallRepair.ps1`,
  `Test-WorldOwnershipContracts.ps1`.
- **Unobserved / behaviour notes:** rendered seat and settle timing (a mount made mid-fight may settle on the combat stance);
  real routes and turn accounting; RT timing; live engagement; repair file IO; visible bite; save/load. A mount Hold left by an
  earlier build's carrier in an old save now keeps automatic attacks still until Stop or a new order clears it. In real time a
  horse that becomes ready first attacks after the rider's waiting strike, not before it. A validated click route is used only
  for the same click's order on the same turn; otherwise the carrier uses native routing.

## 5. Exact identities

- Base: main `783b2b3dea3303e10cc4379dfa0a36ceac66a76b`. Packaged source: `66492352fa95b0c20855524799722f4125130a7f`.
- ZIP: `KingmakerMountedCombat-0.2.16-alpha.4-mounted-alpha4-66492352-diagnostic.zip`, 4,308,850 bytes, SHA-256
  `aeb816de72d13397e4800046859d1e4cf33ccc166f34747ec21afede6a400dc5`; Info.json `0.2.16-alpha.4`, ManagerVersion `0.31.1`.
- DLL: SHA-256 `99cad9609b2149d11b740fd7777206cadd092f82b35a7bb073b970aba39a883d`, MVID `fe54643a-b8a6-4cb8-89a9-6a4d48883970`.
- Mounted Shield icon: SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`.
- History (not for playtest): alpha.2 `c5b147a6` (`d82368ce…`), alpha.3 `bd63035e` (`3be62a3d…`). Rollback: alpha.1 (`d7143ba3…`).

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
