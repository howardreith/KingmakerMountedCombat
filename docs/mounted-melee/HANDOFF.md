# Mounted melee repair — handoff (0.2.16-alpha.2 private alpha)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md).
Install, rollback and playtest: [PRIVATE-ALPHA.md](PRIVATE-ALPHA.md). Issue ledger: [ACCEPTANCE_LEDGER.json](ACCEPTANCE_LEDGER.json).
Package record: [package-identity.json](package-identity.json).

## 1. Outcome

- **Factory:** foreground enrollment `kmc-codex-mounted-alpha2-melee-pose-20261124102246-64c1` (pinned controller
  `versions/85dece1c…`, project `kingmaker-mounted-combat`, plan `single`); draft PR #64 targeting `main`. Final review
  round 1 (on `2769f113`) returned `changes_required` with 16 findings, corrected in this candidate (section 3); the next round
  reviews the records head. Its result is recorded by the controller, not here.
- **Dependency:** PR #61 (Mounted Shield) was merged by the owner's instruction before enrollment (merge commit `783b2b3d`,
  tree identical to the accepted archive `122a9f94`). The branch starts from that main (`--target main`); the accepted Shield
  enrollment, branch and archives are untouched.
- **Package:** `artifacts/KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-3c52882a-diagnostic.zip` (section 5). The
  earlier `ea112a71` and `94ad290c` packages are superseded.
- **Native game launched: no.** Zero guarded starts: this session has no desktop-control tool and the guarded save-backed
  scenarios still require the historical qualification fixture suite. N01–N10 are `not_run`, assigned to the owner playtest.
- **Core blockers:** none known. MMR-07 is an investigated visual limitation.

## 2. Changes and evidence

| Issue | Cause / explanation (status) | Change | Offline evidence | Native/owner |
| --- | --- | --- | --- | --- |
| MMR-01 seating | **Hypothesis** (source-backed, no matched in-game comparison): the animated saddle froze `seatFromSource` once from the rider's pelvis in the attachment frame, so seat height followed the rider's animation when Mount Companion fired or a view's first frame after load. No accumulation (frame lease/release verified in source). | The Horse profile declares the measured preview.3 pelvis−Chest basis `(-0.1892, 0.8601, 0.0175)` its correction was tuned against (`Domain/AnimatedSaddleCalibration.cs`); Mammoth keeps the observed sample, the 24 Small profiles their fixed path; the seat line logs `seatCalibration` and the observed sample. | Component test through the production policy and `AnimatedSaddlePosition` (pose-independence, release/reacquire); contract checks of the compiled profile values and the runtime→adapter→policy calls. Unity transforms (view replacement, old-view restoration) are not executable offline; existing pose tests cover non-accumulation and restoration. | N01, N10 |
| MMR-02 TB approach | **Inferred from installed IL, not a trace:** `TurnController.IgnoreClick` drops a click whose visualized path (the mount's, under paired controls) does not end within the predicted command's approach radius. | A postfix admits only native's path refusal of its own predicted `UnitAttack` (read from `m_PredictionCommands`) by the rider on the highlighted living hostile, melee weapon, recomputed `IsCurrentPathForPoint` false, every earlier native refusal standing, and the mount's lawful remaining step/move distance (coordinator allocation) covering the straight-line gap to pair reach (`Domain/PairedAttackClickAdmissionPolicy.cs`). Both initiative arrangements address the rider's boundary under paired activation, so the same path applies. A longer/blocked route is refused by the pair command's existing approach. | Component test: reachable, eligible step, spent movement, beyond allowance, already in reach, other refusal reasons, non-attack/ranged/other-target command, unavailable measurements; contract tokens `06000C2F`, `04000691`, `0400068B`, `0400066E`, `06007018`. | N02, N03 |
| MMR-03 horse participation | Default paired TB excluded the horse from a rider click (`mountIsLegalActor`). | `MountJoinsIntent`: an ordinary **melee** rider order includes the horse only while addressable on that activation and while neither actor holds newer competing work; each actor is offered only if its own strike can start (`MountedStrikeReadinessPolicy`: Standard, capability, addressability), so a dazed or ended rider is skipped and the capable horse attacks alone. | Component tests with genuine `PairedActivation` states; bow vs melee; newer work; dazed/ended rider and the reciprocal incapable horse. | N04 |
| MMR-04 RT engagement | Most likely the accepted intent stalled on readiness (initiative); a refused or absent native proposal not excluded. | Real-time readiness covers native start waits (MMR-05); no new targeting (the rider brain proposes the target). A once-per-combat `Mounted real-time engagement: firstStop=…` line names no-proposal / refused:reason / accepted-not-dispatched / travelling / engaging. | Component tests of the first-stop classification; installed `CanActInCombat` contract. Native target comparison is not executable offline. | N05 |
| MMR-05 RT response | Movement waited for strike readiness (KMC `Wait`; native start gate before the wrapper's approach). | `ApproachWhileWaiting` while no actor's strike can start (initiative, Standard cooldown, busy hands, scheduled equipment update) and the target is outside the strike's own mount-origin reach or command LOS (`MountedPairSingleAttack.WithinPairMeleeReach`) and the mount can act and move. One controller-owned non-player move: released through `MountedOwnMovePolicy` before dispatch and on every intent end, the pair's own move for door arbitration, a Hold its arrival wrote restored, a newer order retires the intent quietly, a failed route ends the intent once (automatic: A05 unreachable). | Component tests: start waits travel, dispatch only when startable, restrictions stay put, exact release with/without successor and after failed admission. | N06 |
| MMR-06 horse Trip | Installed data: stock Mammoth `AddFacts` = `[TrippingBite, NaturalArmor7, TripDefenseFourLegs]`; `TrippingBite` (`f957b444…`) = `ManeuverOnAttack(Bite, Trip)`. | Only that exact stock grant is withheld from the Horse. A saved horse loses only a sourceless (no source, item or cutscene) stock TrippingBite, once per loaded object, only on a settled world (no load, save worker or pending restoration); failures are retried. | Contract: installed offsets; genuine-object filter (another mod's maneuver fact kept); sourced/item/cutscene copies kept. Component test: per-object ledger, protected phases, retry, reload with the same ID. | N07 |
| MMR-07 bite animation | Installed bundle `resource_2f179560…` (SHA-256 `3edb2116…`): `HorseAnimationSet_Bite` → clip `BiteAttack`; Hoof → `Right/Left_Hoof_Attack`; the stock Bite handle is adopted, never replaced; native gives a routine one `SpeedScale` over all clips. Visibility unobserved. | No animation/timing change. One `Horse strike:` line per native strike at its act event (`playback=no-handle/not-acted/interrupted/played`, action, speedScale, variant); the terminal line keeps its summary. | Component test of the strike classification (stock, missing, interrupted, hoof control); contract: bundle identity, handle `SpeedScale`. | N08 |
| Mounted Shield | Unchanged. | None. | Shield native contract 114/0, selection rollback 68/0, icon `fd519874…` in the packaged DLL. | N09 |

## 3. Final review round 1 (on `2769f113`): dispositions

All 16 findings were addressed in `59d94c4c`, `8a49c5de`, `f2c04e9a`, `3e76662f`, `3a1ce703`, `94ad290c` and `3c52882a`:
R1-F1/F2 (exact grant; settled per-object repair), R1-F3 (exact predicted-command binding and movement allowance), R1-F4/F5/F6
(melee-only, newer work, per-actor eligibility), R1-F7/F8/F9/F10 (exact release, door arbitration and Hold, strike geometry and
route failure, native start waits), R1-F11 (engagement first-stop line; native target choice retained), R1-F12 (compiled seat
wiring checks; hypothesis wording), R1-F13 (per-strike playback line), R1-F14 (`Test-MountedAlpha2Package.ps1` receipt in the
gate), R1-F15 (`.gitattributes` `-text` for the packet; the packet verifier passes on a fresh checkout), R1-F16 (this
handoff and PRIVATE-ALPHA: rollback to backed-up alpha-1 saves, unverified backward compatibility, restored comparisons).

## 4. Validation and limitations

- **Controller focused gate** on `3c52882a` (packaged source): PASS 54.5 s, receipt
  `gate-focused-3c52882adaa8-d43d449b6c4-ae9890c77f2-20261124T164517999557Z`; components 1535/0, Validate-Source 153/0,
  alpha-2 native contract 50/0 and the other focused children (presentation 239/0, Shield 114/0, rollback 68/0, Archery 54/0,
  Ranger 183/0, replay 29/0, localization, UMM reference, native loader 101/0, frozen source 23/0, UMM bindings 25/0 ×2).
  The records head adds the package receipt child (`Test-MountedAlpha2Package.ps1`) and is gated again.
- **Package:** `scripts/Package.ps1 -ArtifactQualifier mounted-alpha2-3c52882a`, Validate-Package 11/0; receipt 9/0.
- **Known baseline failures (not selected; identical on the accepted Shield build `122a9f94`):** `Test-MountedAttackStallRepair.ps1`
  (parameter count mismatch) and `Test-WorldOwnershipContracts.ps1` (missing field `pairSaveReady`).
- **Not run:** any game start, fixture, gameplay, visual or save/load observation; no native transaction, so no restoration.
- **Budget** `mounted-alpha2-melee-pose-20261124`: under 5 of 90 minutes.

## 5. Exact identities

- Base: main `783b2b3dea3303e10cc4379dfa0a36ceac66a76b`.
- Packaged and gated source: `3c52882adaa897008a474859b64cfca644371195`. Later commits are records and the receipt script only.
- ZIP: `KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-3c52882a-diagnostic.zip`, 4,287,279 bytes, SHA-256
  `e0a00993012176fc5b700c0d290c1269749315bfe2fd0270f86a75babc9667e3`; Info.json Version `0.2.16-alpha.2`, ManagerVersion `0.31.1`.
- DLL: SHA-256 `a2cecac0d65925a21429e12a13e5109a3185916c54c6f72acf09589dda7447b7`, MVID `42a2aa87-30dc-4717-a88f-04a8f99a2d72`.
- Embedded Mounted Shield icon: SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`.
- Previous alpha (rollback): `0.2.16-alpha.1`, ZIP `d7143ba3d8b297f5eacc2a71e9c5f4fd0ae1df57a4cbbdb52ff515c656b91545`.

## 6. Owner decisions

Playtest per PRIVATE-ALPHA.md; merge/release decisions are the owner's. No merge, tag, release or installation was done.
