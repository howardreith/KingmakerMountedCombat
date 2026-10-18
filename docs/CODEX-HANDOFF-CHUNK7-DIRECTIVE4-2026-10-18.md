# Codex handoff: Chunk 7 Directive 4 checkpoint (2026-10-18)

Claude stopped here at the owner's request (quota). No native transaction is live. The successor
worktree is clean at `f1e54002`. Nothing has been merged, released, tagged or installed.

- **Branch:** `codex/mounted-combat-chunk7-successor`
- **Draft PR:** #5 (still a draft)
- **Active assignment:** Directive 4, recorded verbatim in
  `docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md`, together with its PR #5 review at 6c0cd7b9. All of its
  prohibitions still apply.

## 1. Where things stand

| Item | State |
|---|---|
| R1 real-time proposal/player-input defect | **Fixed** in 4da0bec4 (`ObservedPlayerAttackRequest`, bound per frame to the exact player command). Brain proposals are refused silently, and only a real click gets feedback. **Native PASS** on preview.35 (b8s1): 5 refused proposals, one click → one attack, no warnings. |
| First-action Mount/Dismount (R2/R3) | The owner decided to **admit in Preparing** (06ec7180, 7930313e). The 6A probe emits `CM01-combat-mount-preparing-admitted`. **Native PASS** on preview.35: rider-first first-action Mount PreparePartnerThisRound (b8s2) and mount-first RetainPartnerParticipation (b8s3). |
| Owner-seed checkpoint pair (Quick/Auto) | **Native PASS** on preview.36: Quick save/load (b9s1/b9s2), Auto save/load (b9s3/b9s4). |
| RT combat-debt checkpoint | **Native PASS:** save b10s1 on preview.37 (Manual_305 2730b30b…, evidence 8739e6eb…); separate-process cold load b10s3 on preview.38 (evidence 95abaee9…). |
| TB combat-debt checkpoint save | **Native PASS** b11s1 on preview.40 (Manual_305 80bf232d…, evidence f952f194…). Completed A was saved at 5.0, its native End value 6.0 decayed by 1.0 s; current B at 6.0; Horse B's approach move 0.74; one current boundary. |
| TB combat-debt cold load | **FAIL** b11s2 on preview.40. The game reported PASS, but the restoration reader found a **real product defect**, fixed in f1e54002, which is **unreviewed and not yet run natively**. See §3. |
| Zero-mounted retained TB save/load | **NOT RUN.** Batch 11 slots 3–5 were declared but never launched. The launcher `Run-b11s3-zerosave.ps1` is ready (preview.40 package). |
| Pending survivor, save barrier, worker cancel, area-transfer autosave, disable/removal (refusal on the Horse derivative + positive removal), schema-1/2 migration, doors | **NOT RUN** on the owner-seed lineage. |

Directive 4's endpoint is **not met**: the TB cold load, the zero-mounted pair and the remaining
families are still open. No completion claim should be made.

## 2. Native batches (ledgers under `analysis-cache/chunk7-continuation/owner-seed-batchNN/`)

**Batch 10 (closed, 5/5):**

| Slot | Run | Result |
|---|---|---|
| s1 | RT save | PASS |
| s2 | RT load | FAIL (reader staging; fixed 786a2384) |
| s3 | RT load rerun | PASS |
| s4 | TB save | FAIL (driver's never-exercised `>= 5.9` expectation; fixed 18641924) |
| s5 | TB save rerun | FAIL (reader refused the TB paired approach; fixed 97a34168, 78bc5a17, 126e5256) |

**Batch 11 (declared 5, used 2):**

| Slot | Run | Result |
|---|---|---|
| s1 | TB save | PASS |
| s2 | TB load | FAIL (product defect, fixed f1e54002) |
| s3–s5 | — | Unused. The ledger records the stop and the planned order. |

Failed runs stay FAIL. Replaying a corrected reader on immutable raw data is diagnostic only. A cold
load requires a terminal-PASS producer.

## 3. Open product fix f1e54002 (do this first)

**Defect.** `PairedActivationPersistence.RestoreParticipation` restored completed pair A's ended,
non-current boundary exactly. It then called `CopyGrantedMovementToContext` on it, which overwrote
`MetresStepped` 2.286 (the step that native ForceToEnd spent) with rider A's allocation value of 0.
A resave would therefore drift.

**Fix.** The allocation copy now applies only to the current boundary. A non-current boundary is an
identity marker and is disposed in the `finally` block; the live partner context keeps the copy.

**Offline checks:** build/Validate-Source 147/0, unit 834/0, Harness 281/0, all chunk7 suites green.

**Next steps:**
1. One independent review of f1e54002. This is a real product change, so review must come before live use.
2. Bump to preview.41 (4 files: `Info.json`, `BuildIdentity.cs`,
   `tests/Chunk7Diagnostics/AssistedAcquisitionConnectionFixture.json`, `version.json`).
3. In a new batch folder, run `Prepare-Candidate.ps1`.
4. Rerun the TB cold load against the **accepted b11s1 producer**. The selection is
   `owner-seed-batch11/b11s2-tbload-selection.json`, which pins producer
   `20261018-c7b-world40-ownerseed-b11s1-tbsave` / `f952f194…`. Cross-candidate consumers are allowed.
5. Then run the zero-mounted save and its cold load on the same candidate.

## 4. Reader/driver corrections made in this stretch (all reviewed)

| Change | What it does | Review |
|---|---|---|
| 786a2384 | Staged archive restoration: owners are Reserved with `MountedPairId` null at shared-world-restored. | `d4-staging-reader-review01` PASS |
| 18641924 | Completed TB A is bounded by its single observed native End decayed by elapsed time (×1.1). Cleanup tolerates an absent `nativeWrite`. | `d4-tb-decay-review01` PASS |
| 97a34168 / 78bc5a17 / 126e5256 | A mounted rider's paired approach may only raise its **own** mount's move. The increase must equal that mount's native `timeMoved` increase, with ≥0.05 m of real displacement, within 6 s. The attacker's own swift, reactions and reaction cooldown stay exact, and a mounted rider's own move too. Zero-mounted riders get no allowance. | `d4-tb-approach-review01` FAIL → `review02` PASS |

**Disclosed limits:**
- Move and `timeMoved` forged together with real displacement would pass; no trace-level tie exists yet.
- A mount's move above 3, up to the native 6 ceiling, is not checked any tighter.
- A mount attacker's own move is unchecked.
- The new exactness for the attacker's own fields also applies to the legacy and zero cases, which have
  not been replayed on accepted native artifacts.

## 5. Owner-seed support changes and limitations

- **Restored-start admission:** `RequireOwnedSaveAdmission` matches file, folder, name and gameId (2965e16f).
- **Checkpoint cases:** `owner-seed-checkpoint-save` / `-load` (e1a20ec2, 1bdd7f61, 193288df).
  - The combat output is a distinct native Manual, `KMC_C7_SEED_CHECKPOINT`.
  - The derived-copy target basis is granted only after the verified restored start.
  - Cold comparisons are made against the producer's own records.
- **Lineage:**
  - Seed: `3405d7c8…` (registered immutable master; never loaded or written).
  - Manual_304: `b4d1ede7…`, from producer b3s1d (`01438210…`).
  - Accepted consumer: b4s1 (`253f5f8c…`).
- **Limitations:**
  - The leased runtime-only stock Mammoth is inside the disposable lab checkpoints (never an owner or human save; its leases settle at cleanup).
  - The forced TB initiative values are part of the saved checkpoint world.
  - A cold load needs a terminal-PASS producer.

## 6. Branches, commits, artifacts

- **Successor:** `codex/mounted-combat-chunk7-successor`.
  - Pushed through this handoff via the guarded copy `scratchpad/push-successor/Push-Successor.ps1`.
  - In a new session, re-derive it from `codex-policy/Push-KingmakerMountedCombat.ps1`; the only change is the root path.
- **Product candidates:**

  | Preview | Commit | MVID / package |
  |---|---|---|
  | .38 | b2114b7b | — |
  | .39 | db1692c7 | — |
  | .40 | 39c062ae | MVID 5b509915…, package 0832ffba…, suite `chunk7b-world40-20261018` 29f0dedc… |

- **Evidence:** `runtime-evidence/20261018-c7b-world3[7-9]*`, `…world40*`. Private raw receipts and saves stay local; do not publish them.
- **Reviews:** `analysis-cache/chunk7-continuation/owner-seed-review/d4-*.json`.

## 7. Remaining gates

1. Review f1e54002 → preview.41 → TB cold load → zero-mounted save and load.
2. Pending survivor, save barrier and schema-1 (these need pair B unmounted with no owner; investigate a native exploration Dismount of B).
3. Worker cancel.
4. Area-transfer autosave.
5. Disable/removal refusal on the Horse derivative, plus a positive removal on a reference-free fixture.
6. Schema-1/2 migration.
7. Doors (opportunistic).
8. Update `CURRENT.md`, `AUTONOMOUS-RESUME.md`, `CHUNK7-OWNER-SEED-EVIDENCE.md` and `CHUNK7-KNOWN-PRODUCT-DEBT.md` (the f1e54002 defect) at the next coherent boundary.
9. Final report per Directive 4 §8.
10. Still not authorized: main merge, release/tag, installation, full 7E, HUMAN PLAY.

## 8. Working notes for the next executor

- Launch native runs detached via WMI `Win32_Process.Create`. Tell the owner before each launch. Never edit the worktree while a run is live.
- The launchers carry `-Confirm:$false`. Use the PowerShell tool for anything involving `$false`.
- Offline checks:
  - `scripts/Build-Local.ps1`
  - `bin/Tests/Release/KingmakerMountedCombat.Tests.exe`
  - `scripts/Test-Harness.ps1`
  - the `scripts/chunk7/Test-Chunk7*.ps1` suites
- Edit fixture JSON only with a Newtonsoft round-trip that touches just the target nodes, using `.Value` on JValue (never `[string]`).
- After two equivalent failures without new observations, change the diagnosis. Every failure in this stretch brought a new native observation.
