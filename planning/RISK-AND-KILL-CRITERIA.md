# Risk and kill criteria

Status: IN PROGRESS

| ID | Risk / kill criterion | Current evidence | Status | Required control |
|---|---|---|---|---|
| K1 | Two active nav agents collide, oscillate, or diverge | Not run | TODO | Prove one authoritative mover before slice |
| K2 | No scoped authoritative-mover control point | Member trace incomplete | IN PROGRESS | Reject global speculative patch |
| K3 | Pair fails valid turns/doorways passed by unmounted control | Not run | TODO | Matched control and mounted telemetry |
| K4 | Selection/formation cannot restore | Contracts incomplete | IN PROGRESS | Snapshot/restore tests and runtime scenarios |
| K5 | Cleanup leaves movement, avoidance, selection, command, or view residue | Domain not implemented | TODO | Idempotent best-effort cleanup with residue report |
| K6 | Broad global movement patch affects other units | No patch exists | PASS | Maintain relationship guard and patch audit |
| K7 | Save/load/area retains half-mounted state | No mounted state exists | TODO | Runtime-only state and forced boundary cleanup |
| K8 | Presentation requires Wrath assets | Wrath assets prohibited and unused | PASS | Kingmaker-native candidate only |
| K9 | No native candidate is remotely plausible | Asset inventory incomplete | IN PROGRESS | One horse preferred, one fallback only |
| K10 | Fresh-process tests remain flaky | Not run | TODO | Two consecutive passes required |
| K11 | Solution depends on Wrath runtime assembly | No production project exists | PASS | Production-reference validator |
| K12 | External state cannot be restored exactly | KMC harness not implemented; no active transaction | IN PROGRESS | WhatIf, lock, backup, finally restore, manifest equality |

## Intake risks

- Installed UMM is exact `0.28.2.0`, not the expected `0.32.x`. This is an observed target difference, not a reason to substitute binaries.
- No unique `KMC_AUTOMATION_BASELINE` or `KMC_AUTOMATION_WORKING` fixture is proven. Save-backed runtime work is unauthorized.
- The live Mods tree contains valued third-party/user state and must be transactionally restored byte-for-byte.
- The reference harness is incomplete and unlicensed as a snapshot; KMC will implement original tooling.

No critical hard stop is presently proven. The pre-code gate remains closed.
