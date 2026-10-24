# Chunk 7 batch195: 7E development pilots, third pass

Status: **stopped; recovered, diagnosed.** The acquired chain and the X4 Manual producer passed on one
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

## Recovery (authorized), 2026-10-24

- **Process:** PID 48672 verified as `Kingmaker.exe` launched by this run (command line names
  `…b195-x4c-three-area-cold\runtime-request.json` and the lock token `5121848a…`), then stopped by PID.
- **Order deviation, forced by the tools:** `Recover-KmcPersistenceProfile.ps1` refuses unless the
  combined run transaction is already `restored` (here it was `prepared`/`staged`), and its `-WhatIf`
  path cannot hash under WhatIf. So `Recover-KingmakerRuntimeTransaction.ps1` ran first: WhatIf purity
  PASS, then live PASS (Mods `310c6180…`, saves `95fabf2e…`, baseline immutable, Working restored,
  allowlist passed). It closed the lock.
- **Profile:** `Params.xml` equals the run's captured original (`7ff07d21…`; the force-stop avoided
  UMM's exit rewrite). `KingdomDifficulty` is unchanged. **Residual:** two Unity analytics PlayerPrefs
  differed from the snapshot, `unity.player_session_count` (2005 → 2006) and `unity.player_sessionid`,
  both in the reviewed native-startup set. The reviewed restore needs the lock, which the transaction
  recovery had closed, so a reviewed one-off followed the b3s2 precedent:
  `analysis-cache/chunk7-continuation/b195-x4c-recovery/restore-prefs.ps1` (`271bfcaf…`), pinned to the
  snapshot (`c433ebb1…`) and the current PlayerPrefs (`3c6ade26…`), refusing unless no game or lock
  exists, the transaction is restored, Params.xml equals the captured original and the only deltas are
  those two keys. WhatIf passed; the coordinator reviewed it and ran it live: "PASS restored PlayerPrefs
  exactly to the b195 x4c pre-launch snapshot". Profile restoration is now exact.
- No Kingmaker process and no lock remain.

## Diagnosis

**Which actor and why.** A single-method IL member-reference listing of the native 2.1.7b getter
`UnitEntityData.get_IsDirectlyControllable` shows that, when `Game.CurrentlyLoadedArea.IsCapital`, it
returns true only for the main character and units whose `Master` is the main character. Native
evidence (run `20261019-c7b-world48-ownerseed-b23s3-areaauto`) observed Oleg's Trading Post
(`ead426a6…`, entry `fd225a93…`) with `destinationIsCapital: true`. **Inferred from that rule, not
observed per unit:** pair A (main Ranger and its Horse) stays controllable and pair B (Small Ranger
`7aac72b4…` and Horse `fbba2294…`) does not, which explains the all-pair refusal; the log names no unit. The project already avoids the capital for
fixtures (batch23 capital refusal; Manticore and Tomb routes require `!IsCapital`).

**The X4 route was invalid.** I chose Oleg's from earlier Chunk 7 area pins without checking
`IsCapital`. A replacement route uses areas the owner seed honestly reveals and that are not capitals:
Spider Cave → Manticore Lair (`dfbc2423…` / `314e8c3b…`, the batch54–56 entry) → Spider Cave
(`00becd53…`, the accepted Manticore return entry) → Manticore Lair. The repeated-area route now refuses
a capital destination before transferring (diagnostic guard plus source regression).

**Product question: is it a defect?** Yes, for players. Entering the capital with a non-main pair
mounted makes KMC refuse restoration and then block saving and new participation until a reload
("load a valid save to resume"). In the capital that means the player cannot save there, and the
pair is lost until reload. A minimal fix is **not** small:
- The transfer object is cancelled after placement, and `areaRestoreFailure` blocks saving and every
  later area change, so a deferral has to keep a suspended transfer alive across the capital visit
  and the next transition.
- Saving while deferred is the real decision: either keep it blocked (honest, but no saving in the
  capital) or write the suspended pairs into the archive as pending, which is new schema behavior.
- The trigger is a native rule (capital), not a transient cutscene, so "until controllable" means
  "until the party leaves the capital".

Proposed design for review (not implemented):
1. In `RestoreAreaPair`, when the only refusal is native controllability **and** the destination is
   a capital, keep the transfer suspended as a `DeferredAreaTransfer` (not failed), keep both pairs
   unmounted with no presentation, and report it.
2. Retry restoration at the next `AreaEntitiesReady` whose area is not a capital, with the same
   all-pair admission; any other refusal fails as today.
3. Save policy while deferred is the owner's call: (a) block with an explicit reason, or (b) admit the
   save and persist the deferred pairs as schema-3 pending custody, restoring on load in a non-capital
   area. (b) needs archive, reader and migration work and its own native proof.

**Owner decision and implementation (offline, needs review): dismount on arrival.**
`MountedAreaPersistence.RestoreAreaPair` now classifies each saved pair before attaching any:
- a pair with no refusal restores as before;
- a pair refused **only** by the native capital control rule is retired: the area is a capital, the
  refusal is exactly the control refusal, a hypothetical re-validation with control assumed refuses
  nothing else, every uncontrollable unit is neither the main character nor the main character's
  pet, and any unit the rule admits is still controllable (`Domain/MountedAreaControlRule`, which the
  pair candidate now also uses for its refusal text);
- any other refusal throws, so the all-pair failure, fail-closed and save blocking stay as today.
Before anything attaches, each retired pair must show observed separation: settled saved
participation (no activation, boundary, partner or pending split), no active registry owner, no
world relationship still holding either unit or owned residue, distinct live views with neither
parented under the other, finite positions and no installed agent override. A residue throws
("Capital-arrival retirement was not clean") and fails closed. Nothing is granted or fabricated:
suspension had already separated the pair through the normal `AreaSuspension` cleanup, and
retirement only declines to re-attach it. Saving stays allowed. `Report` keeps a single feedback line,
so the review follow-up names the dismounted pairs in the summary UMM shows: "Dismounted on arrival
because the companion is not directly controllable in this area: <rider> and <mount>[, …]. Mount again
after leaving. <n> of <m> mounted relationships restored without Mount or preparation."
`AreaRetiredPairCount` counts them. The player guide has one line on it. Unit tests: eight new cases.
The Fast components run reported 877/0 in the repository; in a fresh archive copy the reviewer saw
876/1, the one failure being "queued native requests… Missing or oversized save identity", which is
pre-existing at the parent commit and depends on the path length.

**Rows 58–59 implemented offline (2026-10-23, after `10fbeab8`; native not run).** Case names
`multi-exploration-capital-arrival-manual` (row 58) and `multi-exploration-capital-arrival-cold-manual` (row 59),
both owned `persistence-load` selections; the capital admission is scoped to them in the runner
(`Assert-KmcChunk7CapitalArrivalRoute`), the request reader and the in-game route, and every other route still refuses
capitals. Row 58's lineage is same-candidate: it consumes an x4s-shaped Manual produced on the same candidate.

**Native test design (matrix rows 58–59, original record):** the old Oleg's route is the stimulus. A
new owned persistence case cold-loads a two-owner mounted Manual (x4s shape), makes one native
transfer to Oleg's (`fd225a93…`) and writes one genuine Manual there. Its reader requires one
retired pair (count 1 and the UMM line), the main pair restored without Mount or preparation, the
non-main pair separated with no owner or residue, and the Manual written. A separate cold consumer
loads that Manual: one mounted pair, the other unmounted, no residue. It needs a new case in the
repeated-world or area engine plus a reader, so it is designed into the matrix rather than built here.

**Why cleanup overran and the game could not quit.** After the refusal the pairs stayed in failed
custody, so the scenario's settlement predicates could not complete. `Chunk7HostCompletionGate`
records "exceeded its bounded drain" after 45 s and keeps the quit fenced by design (fail-closed: a
terminal result never abandons custody). The bounded unattended exit added in batch188
(`RuntimeAutomationHost.TryQuitRetainedSixPairFailure`) applies only to six-pair lineage selections
(`RetainedSixPairFailureExitReady` requires `selection.SixPairLineage`), so this two-pair persistence
run waited for the runner's 900 s deadline and ended restoration-blocked.

**Follow-up (`d0f842a0` plus its review fix):** the retained-failure exit now also covers an owned
persistence selection that is not six-pair lineage, bound to its own declared load (1–3) and write (0–2)
quotas and a committed retained-failure artifact, with its own reason ("Disposable owned-pair
persistence run failed…"). The review found that the scope check ended in `RequireCurrent`, which
throws once a refused area restore has changed the world, so x4c itself would still have hung. The fix
(option a): when the persistence service records a refused area restore and the current area is the
engine's declared replay destination, the scope is verified through the existing
`RequireDeclaredAreaDestination` check (same game, player, state, campaign and main character, the
declared area blueprint and scene state, every tracked actor unreplaced); otherwise `RequireCurrent`
applies unchanged. Each distinct reason the exit declines is logged once after the drain deadline.

Checks run for these commits (all from the repository root):
- `scripts\chunk7\Test-Chunk7SixPairFailureExit.ps1`: 94/0 (16 two-pair cases).
- `scripts\chunk7\Test-Chunk7TwoPairRetainedScope.ps1` (new, runs the real `RequireRetainedFailureScope`
  against stand-ins): 11/0.
- `scripts\chunk7\Test-Chunk7SixPairExitReader.ps1`: 25/0.
- `scripts\chunk7\Test-Chunk7RepeatedWorldEvidence.ps1`: 133/0 (capital pin refusals added).
- `scripts\chunk7\Test-Chunk7RepeatedWorldProtocol.ps1`: 27/0, 8/0, 9/0.
- `scripts\Test-Tiers.ps1` (Fast): 3/0.

**Route guard (`d40b3719` plus review fix):** besides the in-game refusal, the runner now refuses the
reviewed capital pins before intake (`Assert-KmcChunk7RouteAvoidsKnownCapitals`, list derived from the
b23s3 native observation above), and the request reader refuses them too. The replacement Manticore
route passes the request reader; its draft plan is `analysis-cache/chunk7-continuation/b196-x4c-route-draft.json`
(`4c33e242…`), and the old Oleg's route is refused by it.

## Native-life control damage: recorded assumption

The unmounted control's damage identity (product and reader) is exact: damage delta = `nativeDamage`
minus the temporary HP it spent. It assumes `RuleDealDamage.Damage` includes the part that temporary
HP absorbs. That is unconfirmed; with zero temporary HP it does not matter, and if it is wrong a run
with temporary HP fails closed rather than passing.

## Not run

Row 57 (`owner-seed-native-life-cleanup`), the two-pair native-life pilot on the c5 lineage, and
`compiled-owner-seed-fixture-small-horse-presentation-rt`.
