# Phase 2 human acceptance

Recorded: `2026-08-25T12:00:39Z`

Status: `PASS` for the Mammoth private-alpha foundation. This is not public-release acceptance and is not a claim of Wrath parity.

## Exact artifact accepted

The user's regression was performed against the installed Unity Mod Manager payload below. The installed `Info.json` and DLL are byte-identical to the immutable package, and the UMM cache contains the same DLL hash.

| Field | Accepted identity |
|---|---|
| Repository | `howardreith/KingmakerMountedCombat` |
| Source branch at packaging | `codex/mounted-combat-phase2-alpha` |
| Product version | `0.1.0-phase2b-dev.1` |
| Accepted implementation commit | `1241222459209aea1e6127bedd7d630df3940b99` |
| Packaging/documentation commit | `49c970f9445d435c530a8ce949bdfddee4e1ef03` |
| Package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase2b-dev.1-round2-manual-regression.zip` |
| Package SHA-256 | `de25bb753a7b902206e474a59d23fe1474b4a7c631aea19caa678fd12962a427` |
| Adjacent manifest SHA-256 | `d6992dc8e28431512477f8ace5bdec8c05ada57b690f502aaedb5b94ab618cd8` |
| DLL SHA-256 | `41680c98b90f1d941693e2b42d01c58cbc09f7b197f842acccb2673b4eb630ba` |
| DLL MVID | `2dc5c5b3-559b-4e54-870f-8c76a911ec08` |
| `Info.json` ID / version | `KingmakerMountedCombat` / `0.1.0-phase2b-dev.1` |
| `Info.json` SHA-256 | `d81524ab54de7f4b22ad815ef3e69e4718d13da8c7dc4f328c02a589a35d8003` |
| Installed location | `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Mods\KingmakerMountedCombat` |
| Install record | `20260825T1133116242550Z-3a4e8b0897d54e9bbeeb4ab1b83ca508.json` |
| Install postcondition | `installed-and-verified` |

The package contains only:

- `KingmakerMountedCombat/Info.json`;
- `KingmakerMountedCombat/KingmakerMountedCombat.dll`.

No code or uncommitted change newer than the accepted implementation was present at intake. The closure work after `49c970f` is documentation-only and does not alter the accepted DLL.

## Human-confirmed behavior

| Result | Behavior |
|---|---|
| `PASS` | Basic mounting |
| `PASS` | Explicit dismount |
| `PASS` | Mounted ordinary movement |
| `PASS` | Menus can be opened while mounted |
| `PASS` | Unsupported Wild Shape cleanly dismounts |
| `PASS` | Transformed and reverted rider remain visible |
| `PASS` | A distant door click makes the Mammoth approach and opens the door correctly |
| `PASS` | Saving cleanly dismounts |
| `PASS` | Reload remains unmounted |
| `PASS` | Area transition cleanly dismounts |
| `PASS` | No duplicate command, attack, or damage behavior was observed |
| `PASS` | Rider-turn movement routes through the Mammoth in turn-based mode |
| `PASS` | Turn-based mode is controllable |
| `PASS` | Rider primary works when invoked through the KMC control |
| `PASS` | Mammoth primary works when invoked through the KMC control |

This human result complements, but does not replace, the qualified automated evidence. Automation established exact command, ownership, resource, cleanup, and restoration contracts. The user established ordinary playability and the player-facing result of the door interaction.

## Accepted private-alpha limitations

| Disposition | Limitation |
|---|---|
| `LIMITATION` | Normal stock attack/right-click does not initiate mounted attacks |
| `LIMITATION` | Stock mounted auto-attack is not implemented |
| `LIMITATION` | The KMC Rider primary and Mammoth primary overlay controls are required |
| `LIMITATION` | Rider and Mammoth currently receive separate turn-based turns |
| `LIMITATION` | Mammoth turn-based movement may slide without appropriate locomotion animation |
| `LIMITATION` | Mounted ranged attacks remain unsupported |
| `LIMITATION` | The mounted relationship remains transient and nonserialized |
| `LIMITATION` | Save/load and area transitions do not preserve mounted state |
| `LIMITATION` | Automatic remount is not implemented |
| `LIMITATION` | Mammoth presentation retains a seat gap, stiffness, and no saddle or reins |

These limitations are accepted for Phase 2 private-alpha closure. They remain backlog or unimplemented work, not hidden PASS claims.

## Door diagnostic disposition

The user confirmed that the door click works well: the Mammoth approaches, the door opens, and the pair can use the interaction successfully. Raw post-open native path-object replacements and their `TileHandler.LastUpdateFrame` relation remain preserved as historical diagnostic telemetry.

A raw native path-object identity change is retired as a standalone product-failure criterion when the same player-owned command remains healthy, the door opens once, the Mammoth remains pathfinder, the rider owns the interaction, the target is reached, there is no visible oscillation or duplication, and cleanup is exact. Command replacement, path failure, target failure, excessive unattributed churn, visible oscillation, duplicate interaction, non-mounted regression, and restoration failure remain genuine failures.

## Claims expressly not made

Phase 2 does not claim:

- normal Kingmaker attack-command or right-click integration while mounted;
- normal mounted auto-attack integration;
- Wrath-style combined turn behavior;
- animated Mammoth turn-based locomotion;
- mounted ranged combat;
- persistent mounting or automatic remount;
- public-release quality or full Wrath parity.

Final human disposition: Mammoth private-alpha foundation accepted. Horse development is separately authorized and must preserve this exact Mammoth baseline.
