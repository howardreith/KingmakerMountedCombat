# Mounted melee repair — handoff (0.2.16-alpha.2 private alpha)

Mission: [docs/missions/mounted-alpha2-melee-pose/MISSION.md](../missions/mounted-alpha2-melee-pose/MISSION.md).
Install, rollback and playtest: [PRIVATE-ALPHA.md](PRIVATE-ALPHA.md). Issue ledger: [ACCEPTANCE_LEDGER.json](ACCEPTANCE_LEDGER.json).
Package record: [package-identity.json](package-identity.json).

## 1. Outcome

- **Factory:** foreground enrollment `kmc-codex-mounted-alpha2-melee-pose-20261124102246-64c1` (pinned controller
  `versions/85dece1c…`, project `kingmaker-mounted-combat`, plan `single`); draft PR #64 targeting `main`. Final review
  rounds 1 (`2769f113`) and 2 (`b108dcea`) returned `changes_required`; the round-2 corrections are in this candidate
  (section 3). The controller records the next round's result.
- **Dependency:** PR #61 (Mounted Shield) was merged by the owner's instruction before enrollment (merge commit `783b2b3d`,
  tree identical to the accepted archive `122a9f94`); the branch starts from that main (`--target main`).
- **Package:** `artifacts/KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-c5b147a6-diagnostic.zip` (section 5). The
  earlier `ea112a71`, `94ad290c` and `3c52882a` packages are superseded.
- **Native game launched: no.** Zero guarded starts: this session has no desktop-control tool and the guarded save-backed
  scenarios still require the historical qualification fixture suite. N01–N10 are `not_run`, assigned to the owner playtest.
- **Scope after review:** MMR-01, MMR-03 and MMR-06 are repaired at source; MMR-02 and MMR-05 keep alpha 1 behavior with a
  diagnostic (their repairs could not be made verifiably safe without a game run); MMR-04 is explained from installed code with
  a diagnostic; MMR-07 is an investigated visual limitation with per-strike telemetry.

## 2. Issues

| Issue | Finding (status) | In this alpha | Offline evidence | Native/owner |
| --- | --- | --- | --- | --- |
| MMR-01 seating | **Hypothesis** from source: the animated saddle froze `seatFromSource` from the rider's pelvis in the attachment frame, so the seat followed the rider's animation when Mount Companion fired or a view's first frame after load. | Horse calibrates from the measured preview.3 pelvis−Chest basis `(-0.1892, 0.8601, 0.0175)` (`Domain/AnimatedSaddleCalibration.cs`); Mammoth and the 24 Small profiles unchanged. | Component tests: pose independence; the production lease and saddle across attach, frames, dismount (exact baseline restoration), remount from a different baseline and view replacement (single owner, old view restored), undeclared profile isolation. Contract: compiled profile values and runtime→adapter→policy calls. The Unity adapter and rendered pose are not exercised offline. | N01, N10 |
| MMR-02 TB approach | **Inferred, untraced:** installed `TurnController.IgnoreClick` drops a click whose visualized path (the mount's, under paired controls) does not end within the predicted command's approach radius. | **Diagnostic only** (review R1-F3: a straight-line allowance cannot establish a legal route before native's validation). A read-only postfix logs native's path refusal of its own predicted rider melee `UnitAttack` on the highlighted hostile, with the mount's remaining allowance, the gap to reach and the approach radius. Native's decision is unchanged. | Component test of the logged case; contract tokens `06000C2F`, `04000691`, `0400068B`, `0400066E`, `06007018`. | N02, N03 |
| MMR-03 horse participation | Default paired TB excluded the horse from a rider click (`mountIsLegalActor`). | One ordinary melee rider order includes the horse while addressable on that activation; selection uses the dispatcher's own admission (evaluator: Standard, native capability block, addressability); the order's accepted attack kind is fixed at acceptance; newer player work recorded when it entered either actor's container overrides it. | Component tests with genuine `PairedActivation` states and the production evaluator: ended or CanNotAttack rider → horse alone, reciprocal, bow order kept bow after a weapon change, recorded newer work, Free/Swift/reaction/own-command controls. | N04 |
| MMR-04 RT engagement | **Explained from installed code:** `Character_Brain` → `AttackAiAction` → `AttackTargetsPriority`; candidates are the group memory's perceived enemies (dead, untargetable, aggro filtered), sorted by `TargetInfo.Sort`; `FillWeight` ranks a manual target first and, in real time, subtracts 0.001 × distance (nearest wins). KMC passes the proposal through. Why the owner's pair stood still is not established offline. | No behavior change. A once-per-combat line names the first stop: no-proposal, refused:reason, accepted-not-dispatched, dispatched-not-moved, dispatched-moved, player-command (automatic deltas only; mount displacement). | Contract pins the native selection IL; component test of the classification. | N05 |
| MMR-05 RT response | **Confirmed cause:** movement waits for strike readiness (KMC `Wait`; native start gate before the wrapper's approach). | **Withdrawn repair; alpha 1 behavior.** The round-1/2 early approach raised move-ownership, Hold, perception, successor and queue-promotion hazards (R1-F7/F8/F9, R2-F2/F3/F5) that cannot be verified without a game run. | None (unchanged behavior). | N06 |
| MMR-06 horse Trip | Installed data: stock Mammoth `AddFacts` = `[TrippingBite, NaturalArmor7, TripDefenseFourLegs]`; `TrippingBite` (`f957b444…`) = `ManeuverOnAttack(Bite, Trip)`. | Only that exact grant (identity and Bite/Trip shape) is withheld from the Horse. A saved horse loses a sourceless copy only when it sits immediately before its sourceless donor successor feature (creation order), once per loaded object, under the composition root's write gate (enabled, no save suspension, area transition, restoration, save worker or load); failures retry. | Contract on genuine objects: other GUID, Disarm, Claw and trigger-less same-GUID facts kept; legacy copy removed; a later direct grant, a non-adjacent copy, a sourced successor and an unknown successor all kept. Component test: every protected phase removes nothing; one repair per loaded object; retry; reload. | N07 |
| MMR-07 bite animation | Installed bundle `resource_2f179560…` (SHA-256 `3edb2116…`): `HorseAnimationSet_Bite` → clip `BiteAttack`; the stock Bite handle is adopted, never replaced; native gives a routine one `SpeedScale`. Visibility unobserved. | No animation change. One `Horse strike:` line per strike of the exact KMC Horse at its act event: horse, index, weapon, special type, handle facts (none / started / acted / interrupted), action, speedScale, variant, and the active `AnimationBase`'s clip, weight and state. | Component test (stock handle, missing, interrupted, hoof control); contract: bundle identity, handle `SpeedScale`. | N08 |
| Mounted Shield | Unchanged. | The shield write gate now calls the same `MountedWorldWriteGate` function with the same inputs. | Shield native contract 114/0, selection rollback 68/0, icon `fd519874…` in the packaged DLL. | N09 |

## 3. Review dispositions

- **Round 1** (16 findings on `2769f113`): F4, F10, F14, F15, F16 verified in round 2.
- **Round 2** (on `b108dcea`): R1-F1/F2 → exact shape, positional provenance and the root write gate (`6637b8e7`); R1-F3,
  R1-F7, R1-F8, R1-F9, R2-F2, R2-F3, R2-F5 → the admission and the early approach were withdrawn (`58622497`); R1-F5, R1-F6,
  R2-F1 → recorded overrides, dispatch-evaluator selection, accepted attack kind (`58622497`); R1-F11 → native selection traced
  and pinned, attributable engagement line (`58622497`, `c5b147a6`); R1-F12 → lifecycle test on the production lease and saddle
  (`c5b147a6`); R1-F13, R2-F4 → literal, exact-Horse strike facts with the active clip (`877bf868`); R2-F6 → section 4 cites the
  receipt.

## 4. Validation and limitations

- **Controller focused gate** on `c5b147a6` (packaged source): PASS 66.0 s, receipt
  `gate-focused-c5b147a62b51-d43d449b6c4-ae9890c77f2-20261124T172741540261Z`; components 1533/0, Validate-Source 153/0,
  alpha-2 native contract 57/0, presentation 239/0, Shield 114/0, rollback 68/0, Archery 54/0, Ranger 183/0, replay 29/0,
  localization, UMM reference, native loader 101/0, frozen source 23/0, UMM bindings 25/0 ×2. The records head is gated again
  with the package receipt child for the `c5b147a6` ZIP.
- **Budget** `mounted-alpha2-melee-pose-20261124`: spent 6.32 of 90 minutes after the `c5b147a6` gate (receipt
  `gate-focused-c5b147a62b51-d43d449b6c4-ae9890c77f2-20261124T172741540261Z`); the records-head gate adds its own spend.
- **Known baseline failures (not selected; identical on the accepted Shield build `122a9f94`):** `Test-MountedAttackStallRepair.ps1`
  and `Test-WorldOwnershipContracts.ps1`.
- **Not run:** any game start, fixture, gameplay, visual or save/load observation; no native transaction, so no restoration.

## 5. Exact identities

- Base: main `783b2b3dea3303e10cc4379dfa0a36ceac66a76b`.
- Packaged and gated source: `c5b147a62b511c7ca21e3481e95e3d2ec3e33cc0`. Later commits are records only.
- ZIP: `KingmakerMountedCombat-0.2.16-alpha.2-mounted-alpha2-c5b147a6-diagnostic.zip`, 4,286,856 bytes, SHA-256
  `d82368ce3f9f8b7d9fc4d6fd4f151c40d0947aaed43c76dd3bf54aa1257a9b05`; Info.json Version `0.2.16-alpha.2`, ManagerVersion `0.31.1`.
- DLL: SHA-256 `b2d3788e26d0556a164c824644b63b492e50504112b105bad2032b01f63b6a75`, MVID `0bfff03f-3c34-4e72-8414-b0eba5b842ed`.
- Embedded Mounted Shield icon: SHA-256 `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`.
- Previous alpha (rollback): `0.2.16-alpha.1`, ZIP `d7143ba3d8b297f5eacc2a71e9c5f4fd0ae1df57a4cbbdb52ff515c656b91545`.

## 6. Owner decisions

Playtest per PRIVATE-ALPHA.md; merge/release decisions are the owner's. No merge, tag, release or installation was done.
