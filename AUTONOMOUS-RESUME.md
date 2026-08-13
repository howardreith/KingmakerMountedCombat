# Autonomous resume

Updated: 2026-08-13T23:18:00Z

- Exact checkpoint branch / HEAD: `codex/mounted-combat-feasibility` / `48f434718f0953eea0ed29dcaa56a182c3c6eb8a`; native-main-menu loading repair and first-failure evidence are committed. Durable records are being updated with the native-bootstrap retry.
- Working status at checkpoint: schema-v3 clone-live-plus-KMC transaction code/tests and durable records are modified pending commit. `runtime-state/fixture-qualification.json` plus restored/quarantined run evidence are intentional project-owned state. No runtime transaction is active.
- Active version: `0.0.1-feasibility`.
- Last successful gate: source 21/0, build 1/0, pure/component 59/0, guarded harness/protocol 65/0, assembly-backed 58/0 (Kingmaker 47, Wrath 11); overlay transaction fault tests PASS; and independent post-failure Mods/save restoration PASS.
- Current failure/hypothesis: the repaired native bootstrap still reaches the same `Player.PostLoad` invariant, while bounded raw player/party identity is consistent and Working contains a `CraftMagicItems` serialized marker. Package-only staging likely prevents party deserialization. The next bounded experiment is schema-v3 transactional cloning of the exact live Mods baseline plus the qualified KMC overlay.
- Exact files being changed: `scripts/runtime/RuntimeHarness.Common.ps1`, `scripts/Test-Harness.ps1`, `AUTONOMOUS-BLOCKERS.md`, `MOUNTED-COMBAT-JOURNAL.md`, and `AUTONOMOUS-RESUME.md`.
- Exact next command: commit the overlay transaction checkpoint, rebuild/repackage on the clean commit, run save-backed WhatIf, then run one fixture-intake overlay pilot.
- Analysis profile: exact bounded decompilation and the qualified-fixture consistency script remain only under ignored analysis/obj paths; no proprietary save content, source, or assets are committed or packaged.
- Runtime profile: `fixture-intake` runs `20260813T225000Z-fixture-intake-pass1` and `20260813T231300Z-fixture-intake-native-pass2` are structured FAILs with exact restoration. The second proves native bootstrap plus watchdog behavior, but no area/fixture verification or mounted samples. Each authorized exactly one Working load and zero Baseline/save/unauthorized operations.
- Package profile: current diagnostic ZIP SHA-256 `60c34f9dbf304f6ea35c3dd9c58378084fc0249966abd61e57c2f4cec3aa13aa`; DLL SHA-256 `c873334c867bbc90f21bbade83dbfc3581677164fbf8d19c3d82a6819eca11dc`, MVID `4d3dad6f-553b-4325-89fc-739cb1a3f9c6`, bound to clean commit `48f434718f0953eea0ed29dcaa56a182c3c6eb8a`.
- Publication profile: project guard SHA-256 `57551c6c4359b4fbb8a0c5b0ba6f0e5e50553dc62c3a88e2aaa0dc92a6f72846`; remote branch remains behind local work. No public release was created.
- External state: restored exactly. Live Mods digest is `c292c5c62a232a0ad7b32ed489139a8d135caa18e38151677f619bf7555c70cb`; full save metadata digest is `58a54974423316041e29d32b1093c5d6775dbd67fe2b37ffaea3c9ae27de19be`. Baseline and Working match qualified hashes/timestamps; `fixture-qualification.json` remains SHA-256 `c1e33c75004212039258d6d90ba20e43c37c50966c94e685dbad1ca0e653654f`. No process, lock, or sentinel remains. Unrestored external state: none.
