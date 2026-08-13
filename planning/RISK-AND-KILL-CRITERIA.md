# Risk and kill criteria

Status: IN PROGRESS

| ID | Risk / kill criterion | Current evidence | Status | Required control |
|---|---|---|---|---|
| K1 | Two active nav agents collide, oscillate, or diverge | Default-off adapter stops/disables rider `AgentASP`, owns one avoidance lease, and leaves only the mount stock agent authoritative; runtime behavior not run | IN PROGRESS | Movement telemetry and fresh-process fixture scenarios |
| K2 | No scoped authoritative-mover control point | Exact `AgentOverride`, rider ASP suppression, and private command-recipient rewrite seam established | PASS | Runtime-qualify exact active-pair guards; reject global tick patch |
| K3 | Pair fails valid turns/doorways passed by unmounted control | Not run | TODO | Matched control and mounted telemetry |
| K4 | Selection/formation cannot restore | Exact public selection APIs and formation-row behavior mapped; runtime unproven | IN PROGRESS | Snapshot/restore tests and runtime scenarios |
| K5 | Cleanup leaves movement, avoidance, selection, command, or view residue | Explicit coordinator, rollback, retryable ownership records, and deterministic cleanup tests pass; game residue not measured | IN PROGRESS | Runtime lifecycle suite and post-cleanup telemetry |
| K6 | Broad global movement patch affects other units | Current default-off prototype patches eight exact entry/control seams with active-pair guards; 28 exact assembly checks pass; no global movement tick is patched | IN PROGRESS | Runtime non-mounted isolation proof |
| K7 | Save/load/area retains half-mounted state | Relationship is ordinary in-memory state; exact save/load and lifecycle guards force cleanup and block save/load on residue; runtime timing unproven | IN PROGRESS | Runtime boundary scenarios with exact fixture |
| K8 | Presentation requires Wrath assets | Wrath assets prohibited and unused; native Mammoth metadata supplies a bounded experiment but no acceptable presentation is proven | IN PROGRESS | Kingmaker-native candidate only; runtime/manual visual review |
| K9 | No native candidate is remotely plausible | Riding horse has strong rig but is not a companion and is rejected; native Mammoth has exact AddPet linkage and a plausible back-anchor hypothesis | IN PROGRESS | Metadata gate passed; runtime visual review remains mandatory; do not weaken companion invariant |
| K10 | Fresh-process tests remain flaky | Not run | TODO | Two consecutive passes required |
| K11 | Solution depends on Wrath runtime assembly | Production project references exact Kingmaker/UMM/Harmony12 assemblies only; source validator passes | PASS | Keep package/reference allowlists |
| K12 | External state cannot be restored exactly | Token/sentinel-owned pre-stage, backup verification, unknown-tree refusal, idempotent restore, stale recovery, and fault tests pass; live WhatIf/runtime unproven | IN PROGRESS | Prove zero-mutation WhatIf, then two live restore passes |

## Intake risks

- Installed UMM is exact `0.28.2.0`, not the expected `0.32.x`. This is an observed target difference, not a reason to substitute binaries.
- No unique `KMC_AUTOMATION_BASELINE` or `KMC_AUTOMATION_WORKING` fixture is proven. Save-backed runtime work is unauthorized.
- The live Mods tree contains valued third-party/user state and must be transactionally restored byte-for-byte.
- The reference harness is incomplete and unlicensed as a snapshot; KMC will implement original tooling.

No critical hard stop is presently proven. The exact contract plus native-candidate pre-code gate is PASS for a default-off, bounded relationship/movement experiment. Runtime enablement still requires pure state-machine tests, package/harness gates, and exact fixture availability.
