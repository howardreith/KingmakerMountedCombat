# Chunk 4: sustained play and core safety

Status: **IN PROGRESS**. New owner-authorized implementation and qualification mission, 2026-09-08. Branch `codex/mounted-combat-phase3f-playable-core`; clean intake documentation HEAD `e6d89bff8c44ecbc21104b733703be9401c3671e`, qualified source `ec5d44e6eddc9839d273176b345f7c9701520450`. [Completed paired milestone](PAIRED-ACTIVATION-MILESTONE.md) remains accepted engineering evidence. No preview.37 human approval is inferred.

The actual installation reports `0.1.0-paired-preview.37`; no game process was present at intake. Temporary transactions must snapshot and restore current installation, settings/caches, saves and foreign Mods. Historical preview.13 backups remain separate. No permanent deployment is authorized.

## Contract and new gate ledger

Test `EnablePairedActivation=true`, `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`. One pre-combat-mounted Horse/Mammoth pair; rider-principal activation, independent native budgets and native Full/Single/Primary behavior. Transport alone does not charge rider Move.

| Gate | Status | New evidence required |
|---|---|---|
| C4-CHARGE | IN PROGRESS | Identify exact native Charge; mounted RT/TB attempts, early safe rejection if unsupported, paused/queued execution revalidation and recovery; unmounted/unrelated and ordinary approach controls. |
| C4-SUSTAINED | TODO | RT one-order versus repeated input over at least three routines; rider melee/ranged/mount, approach and lifecycle interruptions; several varied TB paired activations. |
| C4-TARGETING | TODO | Native independent inspection/heal/hostile targeting and area effect once per eligible actor. |
| C4-DEATH | TODO | Actual native rider incapacitation/death, including live command interruption and unrelated successor; mount-death regression. |
| C4-TRAVERSAL | TODO | Door, blocked/narrow route, slope/turn, party movement and supported cleanup boundary. |
| C4-SESSION | TODO | Representative encounter/session in each mode and bounded repeated mount/encounter/dismount resource cleanup. |
| C4-VISUAL | TODO | Unobstructed mounted/unmounted Horse strike/recovery, seated motion/countdown/selection; capture availability and HUMAN PLAY recorded separately. |
| C4-FINAL | TODO | All new supported cases on exact final candidate/configuration, paired full-round gate, final A05/A10, Horse/Mammoth, mandatory offline/package/deployment checks. |

Owner-reported missing feature: actual mounted Charge does not work. New RT run A confirms unsafe acceptance: Standard action spent, with no observed movement or attack. Ordinary approach-and-attack is not Charge. Safe rejection will not close full Charge; that remains Chunk 6. Mounted persistence/cold-load debt belongs to Chunk 5, which is not authorized for implementation here. Mid-combat mounting, cross-round Delay and conservative mode switching retain their documented limits.

## Evidence and delivery

COMPONENT, ASSEMBLY CONTRACT, NATIVE INTEGRATION and HUMAN PLAY remain separate. New native runs: **1**. No retained preview.37 result is counted as a new Chunk 4 scenario.

### New Charge reproduction A

`20260908-chunk4-A`, `chunk4-charge-safety-rt`, source `0724bda0c1bd82a993c71e9387380012a2426da0`, preview.2: **31 PASS / 2 FAIL** assertions; **0 PASS / 2 FAIL** new Charge rows. Actual native identity is `ChargeAbility`, blueprint `c78506dd0e14f7c45a599990e4e65038`, logic `Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge`, installed game MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

Mounted native targeting and availability were true. Paused native selected-ability input admitted a command, which started, acted and finished Success. Observed rider Standard reached 5.9851346; rider/mount displacement, rider Move and attack-rule count were zero. Hover was pure. This is a reproduced safety defect; frame sampling alone does not establish the exact custom-delivery failure. Unmounted input failed native target eligibility before creating a command; this is an unresolved fixture control, not an unmounted gameplay failure. Preview.3 adds native straight-route/landing placement and Charge callback observations before the TB reproduction. No gameplay repair yet.

ZIP `KingmakerMountedCombat-0.1.0-chunk4-preview.2-charge-observation-diagnostic.zip` SHA256 `bb6e4067a7b419eea8ee9a74704b32c41bf98f7e8be6d0ca007bd68dbdd960ee`; DLL `f65fa3778aa052f75b0ab345e21882394e49a57c40dfdbd77fd17dc13d42f231`, MVID `cc2d38bb-b0d5-4c25-8a10-3beceee4b487`. Native evidence is under `runtime-evidence/20260908-chunk4-A`. Independent `analysis-cache/chunk4-native/20260908-chunk4-A-restoration.json`: **PASS**, actual preview.37/complete Mods and saves match intake, UMM Params unchanged, no game/lock remains. Desktop inspection is pending after a Steam app capture approval timeout; no desktop input was sent and no human approval is inferred.

### Prelaunch protocol correction — 2026-09-08

The focused game-envelope regression reproduced missing C4 leaf registration (364 PASS / 1 FAIL); preview.2 registers both native Charge leaves and passes 365/0 components and 22/0 Charge protocol checks. Preview.1 was packaged but never launched; its read-only WhatIf is superseded. The final preview.1 harness check passed 247/0. No external write or gameplay evidence exists yet. Preview.2 is a fixture/protocol correction only; it preserves gameplay and the full safety contract.
