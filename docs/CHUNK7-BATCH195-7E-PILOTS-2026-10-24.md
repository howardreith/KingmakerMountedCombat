# Chunk 7 batch195: 7E development pilots, third pass

Status: **stopped, restoration blocked.** The acquired chain and the X4 Manual producer passed on one
candidate. The three-area cold pilot hung after its first area transfer: its game is still running
and the lab transaction lock is held. The process is not ended and nothing is recovered until the
owner or coordinator decides (see the recovery order below). Row 57, the two-pair native-life pilot
and the Small presentation item pilot have not run. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `6b80d9a6` (native-life derivation and control, reviews CLEAR), frozen worktree `worktrees/chunk7-7e-b195-source-6b80d9a6` |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b195-pilots-diagnostic.zip`, `443a6871…`, 3,673,767 bytes; `Info.json` and the DLL only |
| Manifest / DLL / MVID | `a53ff43c…` / `b5938ca1…` / `fa1df0a2-174b-4d89-a704-cab63b9bca45` |
| Candidate tier | 6/0 (3.6 min), receipt `b195-candidate-01.log` (`bb3c6503…`) |
| Suite | `chunk7e-b195-pilots-20261024`, `f7453b3e…` |
| Root `bin\Release` DLL | `b5938ca1…` before and after packaging |
| Helpers | `analysis-cache/chunk7-continuation/b195-stage.ps1`, `b195-run.ps1`, `b195-native-launch.ps1`; freeze record `b195-freeze.json` |

Every run passed its development-scoped WhatIf first.

## Runs

| Batch | Run (`20261024-c7e-b195-…`) | Verdict | Key evidence |
| --- | --- | --- | --- |
| 1 | `c1-ownerseed-cold` | **PASS** | artifact `ac11ef17…` |
| 1 | `c2-ownerfield` | **PASS** | Manual 304 `6384fb7a…`; Horse `45a37143…` |
| 1 | `c3-acquired-cold` | **PASS** | Manual 305 `43289978…` |
| 1 | `c4-cumulative` | **PASS** | Manual 306 `6d3ce75b…`; Small Ranger `7aac72b4…`, Horse `fbba2294…` |
| 1 | `c5-acquired-postfield` | **PASS** | Manual 307 `a0fced23…`, artifact `b9346ea3…`; two owners, seven-actor field |
| 2 | `x4s-mounted-save` | **PASS** | persistence artifact `df34f4dc…` |
| 2 | `x4c-three-area-cold` | **FAIL, restoration blocked** | see below |

Runs 1–6 restored Mods, save protection, Working and Baseline, with no game process or lock left.

### x4c: area restoration refused after the first transfer

The cold load restored both mounted pairs exactly (Horse and Small Halfling Horse attachments, no
Mount execution, no new preparation). The first native transfer (Spider Cave → Oleg's Trading Post,
enter point `fd225a93…`) suspended both relationships, then KMC logged:

> All-pair area restoration failed: Area relationship was not eligible: Both units must be directly
> controllable. Saving and new native participation remain blocked; load a valid save to resume.

The scenario's cleanup then exceeded its bounded drain and kept custody, fencing the process quit.
The runner's 900-second game deadline passed; it left Kingmaker running by design and did not
restore (`restoration-blocked`). The game log at that point is kept read-only at
`analysis-cache/chunk7-continuation/b195-x4c-blocked/output_log.txt` (`6003de64…`).

Not yet diagnosed: which unit was not directly controllable on arrival, and whether Oleg's area
entry (story state) or a restoration timing issue causes it. It may be a product defect: a player
entering such an area would lose the mounted pairs until a reload. The earlier owned area Auto route
used the other direction (enter point `00becd53…` into Spider Cave).

### Recovery order (pending a decision)

The game (`pid 48672`, the run's own process) must end first; ending it is the owner's call.
Then, per the recorded order: `Recover-KmcPersistenceProfile.ps1` first (it needs the stale lock),
then `Recover-KingmakerRuntimeTransaction.ps1` (WhatIf, then real), then verify the profile against
`runtime-backups/profile-<run>/snapshot.json`.

## Native-life control damage: recorded assumption

The unmounted control's damage identity (product and reader) is exact: damage delta = `nativeDamage`
minus the temporary HP it spent. It assumes `RuleDealDamage.Damage` includes the part that temporary
HP absorbs. That is unconfirmed; with zero temporary HP it does not matter, and if it is wrong a run
with temporary HP fails closed rather than passing.

## Not run

Row 57 (`owner-seed-native-life-cleanup`), the two-pair native-life pilot on the c5 lineage, and
`compiled-owner-seed-fixture-small-horse-presentation-rt`.
