# Chunk 7 owner-seed evidence summary

Tracked summary of each accepted native checkpoint on the owner-seed route ([owner directives](CHUNK7-OWNER-DIRECTIVES-2026-10-16.md)). Raw receipts, saves and logs stay in the private lab (`C:\Dev\KingmakerMountedCombatLab`). Paths below are provenance only; they are not remotely readable proof. Counts are per run, not summed across parent and child suites.

## Seed

| Field | Value |
|---|---|
| Seed id | `KMC_C7_NO_MODS_OWNER_SEED` |
| Owner file | `Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks` (left untouched) |
| SHA-256 | `3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f` |
| Bytes | 1,239,025 |
| Master | `fixtures/owner-supplied/KMC_C7_NO_MODS_OWNER_SEED.master.zks`: read-only, an ordinary copy, never loaded |
| Header | main campaign, build 2.1.7b, GameId `3f7381f7-aba6-43bf-bae6-39c6a66a7180`, area `b10e86516b6437d45a629931c30936fe` (the spider cave near Fangberry), DLC Tieflings, Varnhold, Endless and Arcane, no KMC metadata, no foreign `$type` |
| Superseded | `Manual_302_KMC_C7_OWNER_SEED` carried Call of the Wild and Gunslinger types. It is preserved but not used. |

## Checkpoint 1: seed qualification by an isolated cold load

| Field | Value |
|---|---|
| Run | `20261017-c7b-world18-ownerseed-s4-cold`: batch 1, scenario 4 |
| Scenario | `chunk7-assisted-cold-inventory`, selection `owner-seed-readonly-inventory`, base-game-only, one load, zero writes |
| Product | `0.1.0-chunk7b-preview.18`, commit `e09924b4`, DLL MVID `b7c9af78-da5d-4bd6-9db3-c98995c61c58` |
| Harness | the same commit (unified successor worktree); suite `chunk7b-world18-20261017` |
| Loader | UMM 0.33.2.0; KMC staged alone |
| Terminal result | **PASS** |

**Observed:**
- A fresh copy loaded natively through the missing-metadata path.
- Party: Human Ranger 3 at 9000 XP (the main character), Valerie 4, Linzi 4, Amiri Barbarian 4. No pets.
- All four header DLC were recognised and enabled.
- No unresolved blueprint, failed type bind, game-logged exception or foreign declared type.
- Party controllable, out of combat. Autosave on (1 slot), OnlyOneSave off, XP rate 100.

**Restoration:** Mods restored, save protection passed, profile restored exactly. The next qualification suite read unchanged save and Mods digests.

**Excluded:** this checkpoint proves no mounted restoration, because the seed has no KMC metadata.

**Earlier attempts (batch 1, all restored):**
- Scenario 1: an ambiguous `GetMethod` in the observer.
- Scenario 2: a JValue-null check.
- Scenario 3: reader assumptions about a null PostLoad `Player.GameId` and the `OnNewRound` hook name. The native artifact was PASS; the reader refused it.

## Checkpoint 2: two-pair movement, selection and control slice

| Field | Value |
|---|---|
| Run | `20261017-c7b-world22-ownerseed-b2s4-twopair`: batch 2, scenario 4 |
| Scenario | `chunk7-owned-two-pair-campaign`, selection `owner-seed-exploration`, base-game-only, one load, zero writes |
| Product | `0.1.0-chunk7b-preview.22`, commit `162a6d14`, DLL MVID `21a80e18-e6d0-4d0a-859e-8fc5bc370da4`, package SHA-256 `de56294ef94cb3e437256632c423981b2ad6f36793f16e53f745c1896e9a1732` |
| Harness | the same commit; suite `chunk7b-world22-20261017` |
| Terminal result | **PASS** |

**Disclosed setup** (fixture assistance; `acquisitionProof` is false):
- Rider A, the main character `73681ef2-…`, took an unassisted native Ranger level 4 through the game's level-up UI. Choices: Hunter's Bond, the Ranger companion selection, then the KMC Horse feature. Result: a Hunter's Bond KMC Horse.
- Rider B, Amiri `fd213210-…`, received native `GainExperience` to exactly the level-8 threshold, then took four native Ranger levels and got her own Horse.
- No save was written. The installed game never registers `LevelUpAutoSaveController`, so a level-up does not autosave.
- Setup ended at a ledgered SETUP-COMPLETE boundary. Money and the shared inventory were unchanged across setup.

**Measured:**
- Two native Mount inputs.
- An independent native ground move for each pair, each executed by its own Horse, with selection switching.
- Rider A's native Dismount while pair B stayed mounted under the same owner and generation.
- Rider A's unmounted control move.
- Three remount cycles.

**Door:** not applicable. No qualifying door lies within 28 m.

**Restoration:**
- Live `Mods` digest `b592f400…` before and after.
- Save-inventory digest `df434471…` before and after.
- `runtimeUnexpectedWrites` empty. The only staged-tree change was UMM's `KingmakerMountedCombat.dll.<N>.cache`, which was quarantined.
- Profile bytes restored exactly. Protected fixtures unchanged.

**Excluded:** acquisition, door behaviour, combat, mounted save/load.

**Earlier attempts (batch 2, all restored):**
- Scenario 1: setup waited for the nonexistent level-up autosave.
- Scenario 2: a paused ground order was refused. This was a product defect, fixed in `308e7734`.
- Scenario 3: the readers compared Guid text forms and nested a list. The native artifact was PASS.

## Reviews

The lab receipts are under `analysis-cache/chunk7-continuation/owner-seed-review/`:

| Receipt | Verdict |
|---|---|
| `owner-seed-safety-review01` | PASS |
| `umm033-safety-review01` | FAIL |
| `umm033-safety-review02` | FAIL |
| `umm033-safety-review03` | PASS |
| `twopair-safety-review01` | FAIL |
| `twopair-safety-review02` | FAIL |
| `twopair-safety-review03` | FAIL |
| `twopair-safety-review04` | PASS |
| `postreview-delta-review01` (`928987f2..162a6d14`) | PASS |

An external targeted review of PR #5 is recorded in Directive 3.
