# Chunk 4: sustained play and core safety

**PASS - native engineering gates. TODO - targeted visual/physical-input and HUMAN PLAY checks.** September 20, 2026 UTC. Branch `codex/mounted-combat-phase3f-playable-core`; tested source `429377d707a9976be65639e0c27954d8b4ff3717`, version `0.1.0-chunk4-preview.54`.

Scope: one pre-combat Horse or Mammoth pair, rider-principal activation, separate native budgets. Actual tested configuration: `EnablePairedActivation=true`, `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`. The [paired milestone](PAIRED-ACTIVATION-MILESTONE.md) is accepted baseline engineering evidence. Earlier preview.13 human feedback retains its scope; no preview.37 or candidate human approval is inferred.

The owner subsequently accepted this scope and authorized default-branch merge, alpha publication and local UMM installation on September 20. The qualified payload stays unchanged. Local UMM installation completed at 2026-09-20T15:53:37.1343622Z with actual preview.37 backed up and protected saves/settings/foreign Mods unchanged. Preview.54 is now the human installation. Qualification restoration statements below describe the earlier temporary campaign, not the new installation target. [Alpha setup and checklist](ALPHA-PLAYTEST.md).

## New native execution

Every row is a new execution on this exact candidate. GH-HB comprise 20 required new scenario roots plus fresh Horse capture; HC-HL are final regressions. Counts are native assertions and independent outer-validator assertions, not summed as separate gameplay cases. Run IDs are `20260920-chunk4-<Run>` under `C:/Dev/KingmakerMountedCombatLab/runtime-evidence/`.

| Run | Stable scenario ID | Native PASS/FAIL | Outer PASS/FAIL |
|---|---|---:|---:|
| GH | `chunk4-interrupt-melee-rt` | 52/0 | 52/0 |
| GI | `chunk4-interrupt-ranged-rt` | 53/0 | 53/0 |
| GJ | `chunk4-charge-safety-rt` | 51/0 | 51/0 |
| GK | `chunk4-charge-safety-tb` | 51/0 | 51/0 |
| GL | `chunk4-sustained-melee-rt` | 50/0 | 50/0 |
| GM | `chunk4-sustained-ranged-rt` | 50/0 | 50/0 |
| GN | `chunk4-sustained-tb` | 52/0 | 52/0 |
| GO | `chunk4-obstruction-ranged-rt` | 47/0 | 47/0 |
| GP | `chunk4-targeting-mount-rt` | 48/0 | 48/0 |
| GQ | `chunk4-targeting-rider-rt` | 49/0 | 49/0 |
| GR | `chunk4-targeting-area-unmounted-rt` | 47/0 | 47/0 |
| GS | `chunk4-rider-death-tb` | 47/0 | 47/0 |
| GT | `chunk4-rider-incapacitation-tb` | 47/0 | 47/0 |
| GU | `chunk4-mount-death-tb` | 47/0 | 47/0 |
| GV | `chunk4-inspection-rt` | 48/0 | 48/0 |
| GW | `chunk4-horse-strike-comparison-rt` | 48/0 | 48/0 |
| GX | `chunk4-traversal-core` | 257/0 | 257/0 |
| GY | `chunk4-traversal-slope` | 50/0 | 50/0 |
| GZ | `chunk4-area-cleanup` | 47/0 | 47/0 |
| HA | `chunk4-session-rt` | 49/0 | 49/0 |
| HB | `chunk4-session-tb` | 49/0 | 49/0 |
| HC | `actor-allocation-rider-first-tb` | 53/0 | 53/0 |
| HD | `actor-allocation-mount-first-tb` | 53/0 | 53/0 |
| HE | `actor-allocation-rider-first-unmounted-tb` | 49/0 | 49/0 |
| HF | `actor-allocation-mount-first-unmounted-tb` | 49/0 | 49/0 |
| HG | `ordinary-attack-controls-tb` | 64/0 | 64/0 |
| HH | `phase3h-combat-loop-rt` | 54/0 | 54/0 |
| HI | `unmounted-attack-controls-rt` | 47/0 | 47/0 |
| HJ | `mounted-mammoth-primary-hit-rt` | 62/0 | 62/0 |
| HK | `mounted-pair-party-formation` | 60/0 | 60/0 |
| HL | `mounted-mammoth-primary-hit-tb` | 66/0 | 66/0 |

HC/HD each prove three paired activations and subsequent refresh in both initiative arrangements. Final **A05** is HC-HF: 36 matched actor/preparation samples, 36 preparations/clears/fact effects/heals, 72 round handlers, 360 readiness handlers and 540 dependent callbacks. Final **A10** is HG-HK: 32 accepted cases / 287 native assertions / zero failures. HL separately qualifies Mammoth TB. These are current executions, not retained-result revalidation.

Local result/artifact/restoration identities are indexed in `analysis-cache/chunk4-native/final54-evidence-ledger.json`, SHA256 `d875e78297dff52d17ceada33630c42b72722a0a47a72717b2fc9bed4ea27272`. Earlier failures, rejected hypotheses and version identities remain in the [journal](../MOUNTED-COMBAT-JOURNAL.md).

## Behavior and changes

**Charge safety.** The owner's missing feature was reproduced on the exact native path: blueprint `c78506dd0e14f7c45a599990e4e65038`, `UnitUseAbility` -> `AbilityCustomCharge` -> forced path/charged `UnitAttack`. September 8 runs A/B spent approximately 6 Standard in RT and 6 Standard/3 Move in TB without moving the pair. They did not prove an indefinite hang. Exact-identity, pair-local guards now reject mounted Charge before approach or costs, give player feedback, keep hover/paused input pure and revalidate queued orders after mounting. GJ/GK prove both modes, actual unmounted/unrelated Charge, preserved prior costs, legal follow-up and TB End/unrelated turns. **Full mounted Charge remains FEATURE-NOT-PRESENT, assigned to Chunk 6.** Ordinary approach/attack is not Charge.

**Sustained combat.** Reproduced defects led to three other gameplay repairs: ordinary RT continuation after native mixed-weapon melee-tail range termination; pair-local stale-frame movement position/yaw correction; and dispatch of a ready waiting Horse after a rider routine. Before the last repair, CM44 measured a skipped ready Horse and a 17.36-second interval versus 11.35 seconds in the control. GL now completes three full rider and Horse routines per held/repeated adjacent/approach case. Its 133/139 repeated clicks yield one intent start and zero duplicate dispatches; periods of 11.311-11.376 seconds pass the unchanged 0.10-second tolerance with observed 0.04-second native frame variation. GM also preserves native ranged prefixes when melee tails are out of reach; these are not full-plan completions. GH/GI cover Pause/Stop, target movement, deliberate retargeting and death before/during delivery, including released arrows and genuine costs. GN covers both actor orders, exhausted actors with remaining partner work, early End and fresh preparation.

**Independent actors and native death.** GP/GQ deliver targeted native healing with slot expenditure and hostile attacks against separate actors/AC 15 and 24. An actual area effect applies one entry Reflex save per eligible actor at DC 16; native round callbacks are distinguished from entry events. GR supplies the unmounted control. GS applies 119 native damage: rider Dead/FinallyDead persists after cleanup and policy restoration, a live Horse command interrupts, the survivor remains visible, unrelated turns continue and records retire at encounter exit. GT applies 105 native damage and observes Unconscious followed by native recovery to 92 wounds. GU preserves the mount-death path: 28 damage/Dead with FinallyDead=false, followed by native post-combat recovery to 10 wounds. No mod resurrection or manufactured final death flag qualifies these results.

**Traversal and sessions.** GX/GY retain the native Mammoth footprint/collision through doors, blocked/narrow routes, turns, party movement and 11.254 m of actual slope elevation. GZ qualifies supported same-area reload cleanup, not persistence. HA/HB each complete three mount/encounter/dismount cycles: full 4/4 rider and 3/3 Horse routines, real enemy deaths and encounter exits, zero retained actor records/private partner context, stable 3 owned subscribers/16 entries, and zero dropped trace events (RT 314, TB 578).

**Fixture and harness corrections.** Native readiness, durable targets without replenishment, AI isolation, actual effects and strict bounds remain required. FI53's target stopped short while its rider completed 4/4; preview.54 qualifies the target's native path/full footprint and retains failure diagnostics. GI rejects a clipped endpoint and succeeds at an interior endpoint. FB52's matched mounted/unmounted edge movements remain investigative FAILs; they do not justify a mounted-only navigation repair. No arrival/cadence threshold, collision radius, cooldown or attack mode was relaxed. The measured harness optimization moves unused historical archive scans out of live preflight (707.6 to 14.7 seconds); actual WhatIf53 still checks all five complete trees. Preview.54 leaves that workflow unchanged.

Native Full/Single/Primary, current-weapon reach, conditions, targeting, cancellation, projectiles and costs remain authoritative. Transport adds no rider Move tax or tabletop melee restriction. No TB scheduler redesign, new dependency, content expansion or permanent installation was introduced.

## Evidence categories and private package

**COMPONENT:** Full Release checks exit 0: source 22/0; components 388/0, harness 250/0, core 375/0, ground 61/0, play 123/0, outer-play 10/0, metadata 2/0, extended 382/0, obstruction 106/0, traversal 149/0, outer 50/0, Charge 460/0. Full log SHA256 `70e6e100846ba42fa607e3667c51dd596c2c518a837bb125e024a458ab96bfda`. Package 11/0; four exact-package request checks 33/0 each. Actual WhatIf53 purity PASS covers the unchanged deployment workflow, not new gameplay.

**ASSEMBLY CONTRACT:** Exact Kingmaker 2.1.7b/.NET 4.7/C# 7.3/Harmony12: 577/0; Kingmaker MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Read-only Wrath contracts 24/0; detached patch construction 30/0. No proprietary assembly/source is shipped.

**NATIVE INTEGRATION:** The new runs above qualify the stated mechanics and native controls. **HUMAN PLAY: TODO.** GV verifies native selection/character-sheet controls, not physical input. GW captured 62 real 1280x720 scene PNGs: three mounted Primaries and one unmounted three-attack routine, with camera restored. Eleven frames were inspected: seated rider, visible pair and separate dismounted rider; rear angle limits bite-contact assessment. Full motion, HUD/countdown and physical-input approval remain pending. Unavailable desktop capture was not replaced with another automation system.

Private ZIP: `C:/Dev/KingmakerMountedCombatLab/artifacts/KingmakerMountedCombat-0.1.0-chunk4-preview.54-moving-target-footprint-diagnostic.zip`.

| Identity | Value |
|---|---|
| ZIP SHA256 | `2d8bcaa5aca509d07ac4e020ee277babc530bba0591b77b3b09a0ade8ff1d4a6` |
| Manifest SHA256 | `4f4a3d8e610924aa0a40c97b1e6aad3a9ad92fe77e7caf88cf3c0c05eaab8bd5` |
| DLL SHA256 | `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9` |
| DLL MVID | `355cce06-9341-4f47-a2ae-d3e20546ef6f` |
| Qualification suite SHA256 | `498378edc49baef23f6fe77b210d4de57f14afa6d0c5ca848d76780387987f9e` |

## Restoration and targeted manual checklist

All 31 final transactions independently restored actual intake: preview.37 DLL/cache `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`; saves `bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`; full Mods `02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`; UMM Params `dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Protected campaigns/BASELINE, current settings/caches/foreign Mods and the separate preview.13 backup are preserved. Game and transaction lock are absent; the user's Steam client remains open. No permanent candidate deployment, main merge or public release.

- Physically check Charge warning/hover/paused queue, legal follow-up/End and unmounted Charge in both modes.
- Review seated motion, Horse strike/recovery from a clearer side view, native countdown/feedback and rider/mount selection/inspection.
- Play an ordinary encounter per mode with held/repeated attacks, Stop/retarget, healing/area effects, varied actor order/early End and supported traversal.

Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Chunk 5 persistence/cold-load work is the next roadmap step under its own mission; implementation has not begun. Full Charge, moving casting and feat expansion remain Chunk 6.
