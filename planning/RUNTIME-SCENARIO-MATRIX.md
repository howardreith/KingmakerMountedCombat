# Runtime scenario matrix

Status: IN PROGRESS

No runtime scenario has been launched. Save-backed scenarios are `DEFER — EVIDENCED` until distinct project-owned baseline and working fixtures are proven.

| Group | Scenario | Save needed | Status | Acceptance evidence |
|---|---|---:|---|---|
| Scaffold | mod-load-smoke | No | TODO | Two fresh processes, exact identity, exact Mods restoration |
| Forensics | export-mounted-contracts | No | TODO | Structured member/type export |
| Forensics | export-candidate-mount-rigs | No or working fixture, evidence-dependent | TODO | Native blueprint/view/rig metadata |
| Forensics | observe-mount-diagnostic-availability | Working fixture | DEFER — EVIDENCED | Valid rider/mount candidate and UI action |
| Lifecycle | mounted-pair-create-and-clear | Working fixture | DEFER — EVIDENCED | State transitions and zero residue |
| Lifecycle | mounted-pair-double-mount-rejected | Working fixture | DEFER — EVIDENCED | Exact rejection |
| Lifecycle | mounted-pair-invalid-pair-rejected | Working fixture | DEFER — EVIDENCED | Validation reasons |
| Lifecycle | mounted-pair-cleanup-idempotent | Working fixture | DEFER — EVIDENCED | Repeated cleanup, zero residue |
| Lifecycle | mounted-pair-death-cleanup | Working fixture | DEFER — EVIDENCED | Trigger and restored state |
| Lifecycle | mounted-pair-combat-start-cleanup | Working fixture | DEFER — EVIDENCED | Cleanup before ordinary combat |
| Lifecycle | mounted-pair-area-unload-cleanup | Working fixture | DEFER — EVIDENCED | No retained relation/view |
| Lifecycle | mounted-pair-mod-disable-cleanup | Working fixture | DEFER — EVIDENCED | Zero residue |
| Movement | mounted-pair-open-ground | Working fixture | DEFER — EVIDENCED | Destination, one mover, residual <= 0.10 |
| Movement | mounted-pair-stop-start | Working fixture | DEFER — EVIDENCED | No separation/oscillation |
| Movement | mounted-pair-turns-and-corners | Working fixture | DEFER — EVIDENCED | Reached, drift/stuck/repath counts |
| Movement | mounted-pair-doorway | Working fixture | DEFER — EVIDENCED | Matched unmounted control |
| Movement | mounted-pair-selection | Working fixture | DEFER — EVIDENCED | Switch away/back, zero loss |
| Movement | mounted-pair-party-formation | Working fixture | DEFER — EVIDENCED | No duplicate mover/interference |
| Movement | mounted-pair-pause-unpause | Working fixture | DEFER — EVIDENCED | Stable state and destination |
| Movement | mounted-pair-destination-cancel | Working fixture | DEFER — EVIDENCED | Both effective representations stop |
| Boundary | mounted-pair-turn-based-entry-cleanup | Working fixture | DEFER — EVIDENCED | Clean dismount accepted |
| Boundary | mounted-pair-realtime-entry-cleanup | Working fixture | DEFER — EVIDENCED | Clean dismount accepted |
| Boundary | mounted-pair-save-safety | Working fixture | DEFER — EVIDENCED | No relationship serialized |
| Boundary | mounted-pair-load-safety | Working fixture | DEFER — EVIDENCED | No relationship reconstructed |
| Boundary | mounted-pair-area-transition-safety | Working fixture | DEFER — EVIDENCED | No half-mounted state |

The runner must bind every result to branch, commit, version, package/DLL hash, MVID, platform hashes, run/evidence ID, and restoration result.
