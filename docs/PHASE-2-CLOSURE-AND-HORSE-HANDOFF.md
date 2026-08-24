# Phase 2 Round 2 closure and horse-ready handoff

Recorded: `2026-08-24T23:47:53Z`

Status: automated technical boundary `PASS`; focused human regression `IN PROGRESS`.

Final disposition: `PRIVATE ALPHA STABILIZATION ROUND 2 COMPLETE  MANUAL REGRESSION REQUIRED`.

This document closes the bounded Mammoth stabilization investigation. It does not record human acceptance, authorize horse implementation, authorize a public release, or authorize a merge to `main`.

## Identity and immutable evidence

| Field | Value |
|---|---|
| Repository | `howardreith/KingmakerMountedCombat` |
| Branch | `codex/mounted-combat-phase2-alpha` |
| Version | `0.1.0-phase2b-dev.1` |
| Final behavioral/diagnostic implementation commit | `1241222459209aea1e6127bedd7d630df3940b99` |
| Immutable manual-regression package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip` |
| Qualification package SHA-256 | `c7fa0773865d7f3d769159f8689e3fe462d72302b97c88a40854435a8ff9c3cf` |
| Qualification package manifest SHA-256 | `4395ab9244273b792bd89ac5787934d8258bb3808acbd497dcca8a0d2e675acc` |
| DLL SHA-256 | `41680c98b90f1d941693e2b42d01c58cbc09f7b197f842acccb2673b4eb630ba` |
| DLL MVID | `2dc5c5b3-559b-4e54-870f-8c76a911ec08` |
| Final documentation commit and deployable manual package | Recorded after this self-referential document is committed in `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression-identity.json` |

The final manual-regression ZIP is generated from the clean final documentation HEAD. Its adjacent manifest must name that same HEAD and retain the DLL SHA-256/MVID above. The external identity record is the authoritative non-circular binding for the final documentation commit, ZIP SHA-256, manifest SHA-256, DLL identity, package contents, credited rows, gate totals, and restoration result. The earlier qualification ZIP is preserved under a commit-qualified historical name before the canonical package path is rebound.

The package contains exactly:

- `KingmakerMountedCombat/Info.json`;
- `KingmakerMountedCombat/KingmakerMountedCombat.dll`.

No game DLL, decompiled source, proprietary asset, save, credential, runtime backup, evidence payload, or another mod is packaged.

## Bounded door investigation disposition

Final run `20260824T233600Z-round2-final-tile-refresh-distance-door` passes `59/0`. Final/game/movement/telemetry/manifest SHA-256 values are respectively `21a5020d1beb5db57cf510640f97f89d15ca8d31ea76504882037140a46a6871`, `f9d75e31e9971043624db204ac815af5bc44b56545b09c07c919bcdda0d444e5`, `2016dddc2cec23c8f3a05cecb4ab9e8cd9b3ca08fd14ffd540ce46dc76f44301`, `cabeedb80810e4435afd755852f69f853aedfc6c940e20182677dacbca4469be`, and `27d07d9c6cfd155c0a131196599b6874419c6b947eaf21d14adbbb16b9a5c1ae`.

The door opened exactly once through the rider-owned stock interaction after a Mammoth-only approach. Stock `NavmeshCut` readiness completed in `0.0889026` seconds at Unity frame `4103`, newer than `TileHandler.LastUpdateFrame=4102`. The unchanged strict far-side path was accepted and the pair reached the target at `1.20530235` distance with zero oscillation, backtracking, command replacement, path failure, duplicate interaction, or selection loss.

Five native path objects were replaced. Their prior request/replacement/last-update frame triples were:

| Replacement | Frames |
|---|---|
| `8 -> 9` | `4107 / 4120 / 4118` |
| `9 -> 10` | `4120 / 4128 / 4126` |
| `10 -> 11` | `4128 / 4136 / 4134` |
| `11 -> 12` | `4136 / 4144 / 4142` |
| `12 -> 13` | `4144 / 4161 / 4159` |

Every replacement retained the reference-identical player-owned `UnitMoveTo`; `RepathNeeded=false`, `PathFailed=false`, the path was healthy, `AstarPath` was present, and the public graph queue was empty. The raw count and every record remain telemetry. Only the exact healthy TileHandler-frame-attributed subset is excluded from fatal churn. Any unattributed churn, command replacement, path failure, target failure, oscillation/backtracking, duplicate interaction, non-mounted regression, or restoration failure remains fatal.

The evidence rejects a second readiness wait: the initial readiness predicate already passed, but later stock tile work advanced `LastUpdateFrame` while the same healthy command continued. No global graph update, broad pathfinding patch, stock-command replacement, reach relaxation, failure acceptance, or timeout increase was added.

## Qualification ledger

The final implementation gate is:

- source `21/0`;
- Release build `PASS`;
- component `240/0`;
- visual/source-order `17/0`;
- harness/protocol `227/0`;
- exact assembly contracts `299/0` (`288/0` Kingmaker and `11/0` Wrath);
- PowerShell parser `26/0`;
- JSON parser `7/0`;
- diff and prohibited-payload checks `PASS`.

The final runtime credit is `443/0` assertions:

| Scenario | Run | Result |
|---|---|---|
| RT Pass A | `20260824T132154Z-round2-v52-navmesh-readiness-suite2-rt-passA` | `81/0` |
| RT Pass B | `20260824T134633Z-round2-v52-navmesh-readiness-suite2-rt-passB` | `81/0` |
| TB Pass A | `20260824T141121Z-round2-v52-navmesh-readiness-suite2-tb-passA` | `111/0` |
| TB Pass B | `20260824T143535Z-round2-v52-navmesh-readiness-suite2-tb-passB` | `111/0` |
| Distant door | `20260824T233600Z-round2-final-tile-refresh-distance-door` | `59/0` |

The RT/TB A/B rows are credited across the final diagnostic-only classification diff under the explicit qualification-economy rule. Source review and deterministic regressions prove that diff cannot change production movement, command routing, interaction, action economy, turn ownership, selection, UI, lifecycle, reach, or cleanup. The directly affected door row was rerun on the final implementation package. Historical failed and observation processes remain immutable and uncredited.

## Round 2 acceptance map

| Requirement | Disposition |
|---|---|
| RT mounted movement and supported rider melee | `PASS` — fresh RT A/B |
| TB Mammoth-turn control | `PASS` — fresh TB A/B; physical pointer feel remains human-gated |
| TB rider-turn Mammoth-routed movement | `PASS` — fresh TB A/B |
| TB supported rider melee | `PASS` — fresh TB A/B |
| RT/TB transitions and coherent pair/UI state | `PASS` structurally; rendered usability remains human-gated |
| Mammoth independently owned primary and resources | `PASS` — qualified combat evidence retained |
| Mounted ranged rejection | `PASS` deterministically; visible ordinary-mouse feedback remains human-gated |
| Unmounted ranged stock behavior | `PASS` deterministically; ordinary-mouse control remains human-gated |
| Wild Shape cleanup and transformed/reverted visibility | `PASS` in the prior human regression and retained automation |
| Menus preserve pair without disappearance | `PASS` structurally; fog/world flash and subjective latency remain human-gated |
| Distant approach and exactly one rider interaction | `PASS` — final door row |
| Stock post-open traversal | `PASS` — final door row |
| Explicit Dismount | `PASS` — retained human and automated evidence |
| Save/load/area transient clean dismount | `PASS` — qualified lifecycle evidence; no persistence/remount claim |
| No duplicate movement/command/turn/attack/roll/damage/interaction/opportunity/resource chain | `PASS` across credited rows |
| Exact save/Mods/Baseline/Working/process/lock/sentinel/transaction restoration | `PASS` after every credited process and final audit |

Internal ownership fields do not prove human usability. Ordinary mouse behavior, rendered portrait/action-bar/selection, physical pointer feel, menu fog/world flash, and subjective Mammoth pose/presentation remain the focused final human regression.

## External restoration

Final suite `20260824T223200Z-round2-final-tile-refresh-suite1` has snapshot SHA-256 `e920b716bbcaa46fc8e0a405c5a8b9e593363f7aa543346579b839c429ef8664`. Its complete save/Mods inventory and Mods-content identities are `ee70e270a366029de4259c47f924d8e45ace4b8562e837f543f8400473b51b50`, `c21c2f466330a800108d274955bdad87e5115a5b73c74dbef3ba5aabf85b9016`, and `f91fe3ab6131837b0af285e18e6295fc7ded1486f2892277f7a11acdd5fa2597`. Immutable Baseline and restored Working identities are `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512` and `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`.

The final post-process independent audit passed before gameplay evidence was read. The run, Mods, and save transaction records are restored; no restoration error, Kingmaker/KMC launcher process, live KMC deployment, runtime lock, sentinel, or active transaction remains.

## Guarded installation and removal

Run these only with Kingmaker and Unity Mod Manager closed and only after matching the final ZIP and adjacent manifest to the external identity record.

Install when KMC is absent:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Install -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip" -Confirm:$false
```

Replace an existing KMC deployment:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Replace -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip" -Confirm:$false
```

Verify the installed identity:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation VerifyInstalled -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip"
```

Uninstall and then verify absence:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Uninstall -Confirm:$false
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation VerifyAbsent
```

The helper owns backup, staging, verification, and exact-target mutation. Do not manually merge, curate, copy over, or delete the live `Mods` tree.

## Human regression and horse boundary

The focused checklist and stop/report conditions are in `docs/PRIVATE-ALPHA-PLAYTEST.md`. Human acceptance must be explicit and must name the exact final package identity. Until then:

- do not merge to `main`;
- do not begin a horse asset audit or horse implementation;
- do not add Ranger horse selection or Paladin Divine Steed;
- do not claim public-release readiness.

After explicit human acceptance, `docs/PHASE-3-HORSE-MISSION-DRAFT.md` is available as a proposal for separate authorization. It is not execution authority.
