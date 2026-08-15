# Phase 2A presentation report

Status: IN PROGRESS

Authority: `planning/MOUNTED-COMBAT-PHASE-2-MASTER-MISSION.md`.

The Phase 2A presentation, persistence/uninstall, UI, and camera tranches are not yet qualified. Phase 1 establishes only a mechanically stable attachment with `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED` presentation.

Tranche A is partially runtime-qualified at version `0.1.0-phase2a-dev.1`: the deterministic player-action controller passes availability twice at `29/0` and mount/dismount twice at `44/0`; actual IMGUI delivery remains open. Native save passes twice at `63/0` from clean published commit `3781a56f5fa000c89fca1e0809b4eb9fa821734d`, proving exactly one real `SaveRoutine` prefix delivery, synchronous clean dismount, one bounded no-write suppression, unchanged Working identity, and exact restoration. It does not claim stock serialization or a save round trip.

The first native-area process is preserved as `FAIL` `48/3`: `Game.ReloadArea` returned before native unload delivery, so the producer captured its latch prematurely even though the exact successful unload and all later area stages arrived asynchronously. The corrected state machine is offline-green at source `21/0`, Release build PASS, component `128/0`, visual-contract `12/0`, harness `138/0`, and assembly `75/0`; the area row remains unqualified pending a clean packaged A/B rerun. Mode, disable/uninstall, pose, actual UI, and camera rows also remain open. No visual or uninstall PASS is claimed.

This report will bind the accepted pose strategy, exact supported rider/Mammoth profile, technical gate totals, two fresh-process results for every Phase 2A scenario, UI/camera observations, lifecycle claim limits, frame cost, known visual compromises, package/DLL hashes and MVID, protected-save/Mods restoration, and the exact manual-review checkpoint.
