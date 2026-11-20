# Current known issues

The current issue ledger for Mr. Bard's Kingmaker Mounted Combat. It was started by Sprint 1 of the Mounted Combat
Known Issues Remediation Epic (owner document of 2026-11-15, prepared against main `7a5cd2c3`, release 0.2.11).

- **Last updated:** 2026-11-16, Sprint 2 (S01, S02) after the owner's review of head `f71fabe7` (R1: the arrival
  placement through saving and loading; R2: the S01 wording). Branch `codex/kmc-known-issues-sprint2`, draft PR
  #46, stacked on Sprint 1 (branch `codex/kmc-known-issues-sprint1`, draft PR #45).
- **Sprint records:** [Sprint 1](KNOWN-ISSUES-SPRINT1-HANDOFF.md), [Sprint 2](KNOWN-ISSUES-SPRINT2-HANDOFF.md).
- **Reconciled so far:** T01–T08, S01 and S02. Every row of Sprints 3–8 is a *carried-forward planning finding*:
  it is copied from the epic's audit of 2026-11-15 and has not been rechecked against the source since. A sprint
  reconciles its own rows when it starts.
- **Native status is a separate column.** Nothing in this ledger was run in the game by Sprint 1 or Sprint 2.
- Dated handoffs and old failure records are history and are not rewritten. This ledger links to them.

## Dispositions

| Status | Meaning |
|---|---|
| Open correction | An established defect, or an approved gap, still needs its change. |
| Open investigation | Reported behaviour whose cause or safe correction is not established. |
| Implemented and verified offline | The change exists and focused automated checks ran. Native evidence is reported separately. |
| Investigation complete, issue unresolved | The diagnosis pass is done; the report stays open with a precise evidence gap. |
| Intentional limit documented | An existing design or safety boundary is kept and explained. |
| Manual/native verification only, excluded from this epic | The only outstanding task is play or native acceptance. |
| Superseded by an identified fix | A later source correction already addresses the old finding. |

A row whose planned closure is "intentional limit documented" stays an open correction until its documentation
or feedback work is actually done.

## Sprint 1 — validation tools (reconciled 2026-11-15)

| ID | Purpose | Classification | Status | Source | Implementation and offline evidence | Native |
|---|---|---|---|---|---|---|
| T01 | The charge ownership probe asserted a two-pair capacity; production admits six. The assertion threw and hid the rest of the script. | Stale test | **Implemented and verified offline** (the capacity probe). The script as a whole still exits 1 at T04. | [SAVE-LOAD handoff](SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md) (validation table); epic T01 | Commit `07edd2fe`. `scripts/Test-ChargeOwnershipContracts.ps1`: 71 checks run (29 before). A build with capacity 5 fails the probe. | Not applicable: detached probe |
| T02 | The repeated-world evidence reader, and the producer's own check, required the chunk7b subscriber set (13 lifecycle interfaces, 15 rows). | Stale reader and producer | **Implemented and verified offline** (reader repaired and verified offline) | [RATNOOK handoff](RATNOOK-DOOR-REPAIR-HANDOFF.md) (review item P3); epic T02 | Commits `3c64128a`, `a80a28b0`. Reader tests 220 pass; compiled-contract check 21 pass; 5 component tests. | **Not run.** No repeated-world native qualification was made or claimed. |
| T03 | The assembly checks pinned UMM 0.33.2 in line, so a genuine 0.33.0 loader reported three failures that said nothing about API compatibility. | Validator conflation | **Implemented and verified offline** | [SAVE-LOAD handoff](SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md), [RATNOOK handoff](RATNOOK-DOOR-REPAIR-HANDOFF.md) (assembly-contract failures); epic T03 | Commits `42fdf52d`, `a80a28b0`, `e52a59f0`. Kingmaker assembly contracts 810 pass, 0 fail (801/3 before); loader checks 22 pass, 1 unassessed; checker configuration 6 pass. | **Not run** on any loader by this sprint |
| T04 | Behind the T01 assertion, the same script calls production members by reflection with shapes that changed on 2026-11-03. | Stale test (probe drift), not a product failure as far as established | **Open correction** | Exposed by T01 on 2026-11-15 | See "T04" below. Not repaired in Sprint 1 by the sprint's own rule. | Not applicable |
| T05 | `scripts/chunk7/Test-Chunk7SixPairNativeOwnership.ps1` no longer compiles its source list. | Stale test harness | **Open correction** | Found as a T01 neighbour on 2026-11-15 | See "T05" below. Fails identically on main `7a5cd2c3`. | Not applicable |
| T06 | `scripts/chunk7/Test-Chunk7RepeatedWorldProtocol.ps1` stops at an undefined variable. | Stale test harness | **Open correction** | Found as a T02 neighbour on 2026-11-15 | See "T06" below. Fails identically on main `7a5cd2c3`. | Not applicable |

T04–T06 are not among the epic's 25 work packages. They were found while running Sprint 1's own checks and
are recorded here so they are not lost. No other script was audited: the offline scripts outside the Fast tier
were not surveyed, and more of them may be stale. Sprint 2 found two more of the same kind (T07, T08) and left
T04–T06 as they were: none of them blocks an S01 or S02 check.

### T01 — what the probe now establishes

- Production admission, the archive format and the registry all declare six, and the probe asserts the
  reviewed literal six, so a capacity change fails until it is reviewed.
- The capacity each compiled service wrapper passes to the registry (`TryReserveOwnership`,
  `NativeAdmissionRefusal`, `CaptureNativeRequest`) is read from its IL and must be that same six.
- In an isolated registry built by the production service constructor, with compiled services as boundaries and
  genuine `UnitEntityData` containers: six pairs reserve and commit; overlap in either role is refused while
  capacity remains; the seventh is refused with no owner, request or actor index and the six registrations
  unchanged; the production `Dismount` retires one; the refused pair takes the released capacity; the bound
  holds again.
- **Limit:** the three wrappers read `Kingmaker.Game`, whose type initializer needs the Unity player, so they
  are not executed detached. The harness that would exercise them with a facade session is T05.

### T02 — what the contract now is

- Two reviewed revisions of the KMC-owned global `EventBus` subscribers in a settled repeated world:
  - **r1**, historical and implicit: 13 lifecycle interfaces and the two control interfaces; Horse owners are the
    Medium Horse and the first Small Halfling Horse profile. It describes `chunk7-repeated-worlds-v1/v2` evidence
    of chunk7b previews up to .99.
  - **r2**, declared by `chunk7-repeated-worlds-v3` producers (builds from this change on; product version 0.2.11 or later): 15 lifecycle interfaces, controls,
    the defence service and the tack service; every profile that mounts the KMC Horse blueprint (13).
- chunk7b preview.100–.103 and releases 0.2.0–0.2.11 as shipped are **unsupported**: the set changed there
  without a revision (inside preview.100 and again inside preview.103), their producer still emits v2, and it
  could not pass its own check.
- Selection is by evidence contract and producer identity. The expected set is never derived from the inventory.
  Names are compared ordinally, character for character.
- **Also found and fixed inside T02:** the reader's Horse-owner profile list predated the Gnome, female and
  build-variant Small profiles (2026-11-01/02). Its own test had been failing on that since then.
- **Production change:** diagnostic evidence protocol only (`Diagnostics/Chunk7ReplaySubscriberContract.cs`, and
  `Chunk7PersistenceScenario.Replay.cs` now emits v3 with the revision name and checks against it). No gameplay
  code and no save format changed. The product binary changes, as any diagnostic change does.
- **Not established:** which subscribers are live at a settled repeated world is read from source
  (`CompositionRoot`) and from compiled metadata. It has not been observed in the game on a current build.
- **Contract boundary:** a Paladin's Divine Mount fact (`DivineMountSpellResistance`) is a global subscriber too.
  r2 does not include it, because the repeated-world fixtures hold no Paladin; evidence from a party that has one
  is refused as a foreign subscriber, not silently accepted.

### T03 — loader matrix on the development machine, 2026-11-15

| Loader | Genuine binary | Identity | API (binding surface, callbacks, logger, UI, 20 compiled member references) | Native |
|---|---|---|---|---|
| 0.28.2 (declared minimum) | **Unavailable** | Unassessed | Unassessed | Not run by this sprint |
| 0.33.0 | Installed Kingmaker loader | PASS under `umm-0.33.0` | PASS | Not run by this sprint |
| 0.33.2 | NuGet copy held outside the repository | PASS under `umm-0.33.2` | PASS | Not run by this sprint |
| 0.32.4, 0.32.3, 0.31.1, 0.27.12 | NuGet copies held outside the repository | Not asserted (no reviewed profile) | PASS | Not run |

- The declared minimum stays **0.28.2** (`Info.json`). It was not raised and it was not assessed: no genuine
  0.28.2 binary is available, and API compatibility at other versions does not establish it.
- The native runner's own loader pin (`planning/ENVIRONMENT-FINGERPRINT.json`, 0.33.2) is unchanged. A native
  transaction on a 0.33.0 machine is a separate decision and was not attempted.
- Profiles and provenance: `planning/UMM-LOADER-PROFILES.json`.

### T04 — charge ownership probe drift behind the capacity assertion

- **Exact result (committed state):** `scripts/Test-ChargeOwnershipContracts.ps1` passes 71 checks, then
  `TargetParameterCountException: Parameter count mismatch` at its first `AdmitChargeCommand` call. Exit 1.
- **Cause, first point:** `MountedCombatController.AdmitChargeCommand` gained an optional third parameter
  (`MountedChargeRequestRoute route`) in `93d925e2` (2026-11-03). The probe still passes two arguments by
  reflection. The script had been dead at the capacity assertion since `af08a59d` (2026-10-22), so nobody saw it.
- **Second point, from an uncommitted scratch run** that supplied the declared default route: 128 checks pass,
  then `NullReferenceException` where the probe looks up `MountedChargeLease.RequireChargeBuffSurface`, which
  `2c49bd13` (2026-11-03) removed.
- **Not assessed:** anything after the second point. This is a chain of probe drift of unknown length.
- **Classification:** test drift against the Sprint 7 charge changes. No product failure is established, and
  none is ruled out for the checks that have not run.
- **Next step:** bring the probe's reflected calls back in line with the charge code of 2026-11-03, keeping every
  assertion, and classify whatever then fails.

### T05 — six-pair native ownership harness does not compile

- `scripts/chunk7/Test-Chunk7SixPairNativeOwnership.ps1` compiles the real ownership partial with a facade
  session. It fails with `CS0234`: `MountedArchiveData.cs` uses `Domain.MountedControlSlotPolicy`
  (`56b07ff0`, 2026-11-05), which is not in the script's source list.
- Until it runs, the three session-reading ownership wrappers have no behavioural test (see T01's limit).

### T06 — repeated-world protocol script stops at an undefined variable

- `scripts/chunk7/Test-Chunk7RepeatedWorldProtocol.ps1` passes its 27 compiled protocol checks, then evaluates
  one line of `Test-RuntimeGameResult.ps1` that now reads `$f2Upgrade` (`3a7f8a84`, 2026-11-05), which the script
  does not define.
- The checks after that line did not run. They include two source pins on the repeated-world producer; the lines
  they pin were left intact by T02.

## Sprint 2 — saving report and post-load dismount (reconciled 2026-11-15)

Full record: [KNOWN-ISSUES-SPRINT2-HANDOFF.md](KNOWN-ISSUES-SPRINT2-HANDOFF.md).

| ID | Symptom | Classification | Status | Source | Implementation and offline evidence | Native |
|---|---|---|---|---|---|---|
| S01 | Saving reported broken after the Ratnook door. | Reported defect. One occurrence explained and already superseded; the other has no established cause. | **Investigation complete, issue unresolved** | [SAVE-LOAD handoff](SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md); epic S01 | Diagnostics only (see "S01" below). No save decision, guard, latch or retry changed. Persistence contracts 223 pass (214 before). | **Not run.** No log exists from a build with the save diagnostics. |
| S02 | After an area arrival the view check finds inactive views and the pair dismounts. | Established occurrence (one, a world-map arrival) | **Implemented and verified offline** | [RATNOOK handoff](RATNOOK-DOOR-REPAIR-HANDOFF.md) (arrival view check); epic S02 | A pair whose exact views are not active is not attached at arrival and restores through the retained-intent retry. 14 component tests (11 fail with the prior decision behaviour); contract script 20 pass on the reviewed build and 11 pass, 9 fail on the base build; one independent review, corrected. Owner review R1 (2026-11-16): a deferred pair's arrival placement now survives a save and its load (retained-intent record version 2); 7 more component tests (4 fail with the reviewed row conversion); contract script 26 pass on this build and 20 pass, 6 fail on the reviewed build; one independent review, corrected. | **Not run.** Not native-verified. |
| T07 | `scripts/chunk7/Test-Chunk7AreaRestorePreflightAdapter.ps1` does not compile its source list. | Stale test harness | **Open correction** | Found as an S02 neighbour on 2026-11-15 | See "T07 and T08" below. Fails identically on the Sprint 2 base `b62c59a8`. | Not applicable |
| T08 | `scripts/chunk7/Test-Chunk7SixPairArchiveAdapter.ps1` does not compile. | Stale test harness | **Open correction** | Found as an S02 neighbour on 2026-11-15 | See "T07 and T08" below. Fails identically on the Sprint 2 base `b62c59a8`. | Not applicable |

### S01 — what is established, and what is not

- **0.2.3 occurrence: explained, and superseded by an identified fix.** In the owner's 0.2.3 log the Ratnook door
  set the area-restore failure latch ("Native area placement did not complete before the mounted restore
  boundary"). That latch makes the mod's term of the native save gate refuse, so `IsSaveAllowed` was false until a
  load. 0.2.4 (the Ratnook door repair) stopped this door from setting the latch. The latch itself
  is protective and unchanged.
- **0.2.4 report: cause not established.** That session's log holds no save line of any kind. Its last lines are
  four refused *loads* around the native save/load window opening, which is the load defect 0.2.5 repaired.
  - "No save line" is not "no save ran": at 0.2.4 a request, an admission refusal, a queued operation and an
    operation waiting before the native header each wrote nothing.
  - Earliest failure boundary consistent with the log: the action never reached `Game.SaveGame`. A refusal at
    admission cannot be told apart from it, because 0.2.4 logged neither.
  - Ruled out: a save that waited for effects, reached the native header or was reported failed (each wrote a line
    at that version); the 0.2.3 latch; a refused load leaving native save state behind.
  - Not established, and not ruled out: stranded save ownership (an active save scope or an admission fence left
    set); a save queued behind another loading process; a save that began and stuck in the native wait before the
    header. A read of the current source found no path that strands a scope or fence, which is not proof for that
    session. (Corrected 2026-11-16: the first version of this entry listed a stranded scope or fence as ruled out.)
  - What would distinguish them: a later save attempt on a build that logs it. No later attempt is established in
    the 0.2.4 log, so the absence of a failure report there is not evidence.
- **The missing observation:** whether a save was attempted in that session, by which action (Esc-menu Save,
  quicksave or an expected autosave), and the state of the native gate and of the one-save setting at that moment.
- **For the owner:** the Last Azlanti preserver mod is active in both sessions. It bears on the report only if
  both of these hold, and neither is known: the campaign runs in the native one-save mode, and the action that
  failed was a manual save or a quicksave. In that mode native refuses those two with or without this mod; it
  does not refuse an autosave to the campaign's own slot. No save or settings file was opened.
- **Diagnostics added, observation only:** a line when this mod's restoration state starts and stops refusing
  saving; the native outcome of each request (including the silent one-save refusals and the disabled-autosave
  skip) and what a queued save waits behind; a line when a save operation begins. They are not a fix.
- **To close:** the owner's answer on the save action and the one-save mode, or a log of a recurrence on a build
  with these lines, ideally with a second save attempt after the first one fails.

### S02 — what changed and what it rests on

- **Order established:** the arrival restoration runs inside the native load; a party arriving from the world
  map still has inactive views there, because world-map mode puts every unit to sleep and no game mode ticks during
  a load; the first invariant check after the loader goes idle found them inactive. The native side is read from
  the installed assembly's IL.
- **Inference, not observation:** that sleeping was the reason in the recorded session. The correction acts on
  view activity itself and does not depend on it.
- **Correction:** restoration reads the exact current views before attaching, and attaches if and only if the
  mounted invariant would keep the pair. A pair whose views are not active waits, unattached, in the existing
  retained-intent custody and attaches once after ordinary play has settled. The mounted invariant is unchanged;
  no allowance exists for a mounted pair.
- **Bounded:** the existing ten seconds of settled play, then the pair is released with a message and stays
  dismounted. Nothing is latched and nothing is owned while waiting.
- **Also changed:** an explicit player dismount now releases that pair's retained intent at once.
- **Persistence effect:** the after-entry autosave of a world-map arrival records the pair as retained intent
  rather than as mounted; loading it restores the pair in ordinary play.
- **Format (owner review R1, 2026-11-16):** the `kmc-retained-intent` member has a version 2 text: version 1 plus
  one arrival row for each pair deferred at its arrival placement. It is written only when the save holds such a
  pair; every other save writes the version 1 bytes as before, and every existing save reads as before.
  - A deferred pair still standing where arrival placed it restores the same way whether the player continues
    or loads a save written before the remount. A relocated pair, ordinary retained intent and every version 1
    record keep the 6 m rule.
  - A malformed or unsupported version 2 text is refused whole; its pairs load dismounted.
  - A build older than this change (0.2.3 to 0.2.11) refuses a version 2 text by its header, and every pair in
    it loads dismounted there. That matters only if the mod is downgraded on such a save.
- **Limits:**
  - A fight that starts before the remount cancels it (existing rule for world-map custody); the pair fights
    dismounted.
  - A deferred pair keeps the arrival formation's spacing only while neither unit has moved more than 0.1 m;
    otherwise the retry's 6 m rule applies.
  - That a native load puts each unit back where the save holds it, within that tolerance, is assumed.
  - The integration sequence is checked from IL, not executed. Nothing ran in the game.

### T07 and T08 — two more harnesses that do not compile

- `Test-Chunk7AreaRestorePreflightAdapter.ps1` compiles the real area persistence partial against a facade. It
  fails with `CS0103` (`MountedAreaControlRule` is not in its source list) and with facade members the transfer
  no longer matches (`Lifecycle`, `WorldMap`).
- `Test-Chunk7SixPairArchiveAdapter.ps1` fails with `CS0234` (`Domain.MountedControlSlotPolicy`, as T05) and a
  stale `Report` call.
- Both were failing before Sprint 2 and were not needed for an S01 or S02 check, so they were left as found.
  Which commits broke them was not traced.
- Since the owner-review correction of 2026-11-16 the first one's facade also lacks `AreaPair.CarriedCopy`, the
  method the three carried-row copies now go through. That is one more member to add when T07 is repaired.
- Not a harness defect, for the record: `Test-Chunk7CapitalArrivalEvidence.ps1` reports 162 pass, 3 fail; the
  three require native evidence for batches 196–198 that is not on this machine.

## Sprints 3–8 — carried forward, pending reconciliation

Each row is the epic's finding as planned. "Planned closure" is what the epic expects, not a result.

| Sprint | ID | Symptom or purpose | Classification | Status | Planned closure | Source |
|---|---|---|---|---|---|---|
| 3 | M01 | Entrance and exit orders do not receive the native acceleration offer. | Source-established omission | Open correction | Implemented and verified offline | [HORSE-GROUP-PACE handoff](HORSE-GROUP-PACE-REPAIR-HANDOFF.md) |
| 3 | M02 | Gamepad stick acceleration does not reach the mounted mover. | Source-established omission | Open correction | Implemented and verified offline | [HORSE-GROUP-PACE handoff](HORSE-GROUP-PACE-REPAIR-HANDOFF.md) |
| 3 | M03 | Group pace is computed from the rider's speed, not the horse's. | Source-established mismatch | Open correction | Implemented and verified offline | [HORSE-GROUP-PACE handoff](HORSE-GROUP-PACE-REPAIR-HANDOFF.md) |
| 3 | M04 | An extra or stale green movement marker. Parked by the owner; no cause established. | Reported defect, cause unknown | Open investigation | Repair if a cause is established, else unresolved | [EXPLORATION handoff](EXPLORATION-REPAIR-HANDOFF.md) |
| 4 | C01 | A scripted movement command on a seated rider during combat waits for the scene timeout. | Unsupported command path | Open correction | Implemented offline, or explicitly unsupported with its blocker | [CUTSCENE handoff](CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md) |
| 4 | C02 | Rider-only scene placements of three metres or less can be swallowed. | Known omission | Open correction | Implemented and verified offline | [CUTSCENE handoff](CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md) |
| 4 | C03 | Restoration fallbacks (combat starts while staged; 30 m, same navigation, bounded regroup) need truthful feedback and clean retirement. | Intentional boundaries | Open correction | Intentional limit documented | [CUTSCENE handoff](CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md) |
| 5 | R01 | Carried travel in a separate initiative slot gives no ranged-movement contribution. | Conservative omission | Open correction | Implemented offline, or documented unresolved design fact | [MOUNTED-ARCHERY handoff](MOUNTED-ARCHERY-HANDOFF.md) |
| 5 | R02 | The accepted ranged rules (3.25 s threshold, native difficulty floor, turn-based only, per roll, no Run) need precise documentation. | Intentional adaptations | Open correction | Intentional limit documented | [MOUNTED-ARCHERY handoff](MOUNTED-ARCHERY-HANDOFF.md) |
| 6 | A01 | A fresh same-target click just after attack completion gets inconsistent feedback. | One-frame feedback edge | Open correction | Implemented and verified offline | [ATTACK-COMPLETION handoff](ATTACK-COMPLETION-WARNINGS-HANDOFF.md) |
| 6 | A02 | Terminal feedback can describe a player cancellation as "cannot act". | Diagnostic ambiguity | Open correction | Implemented and verified offline | [ATTACK-STALL handoff](MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md) |
| 6 | A03 | The Serenity or Fascinate condition that stopped the rider is unidentified. | Open explanation | Open investigation | Investigation complete; explanation open unless evidence settles it | [ATTACK-STALL handoff](MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md) |
| 6 | A04 | Real-time blocked orders wait natively (intended). Automatic proposal churn is a hypothesis. | Intended behaviour plus unconfirmed hypothesis | Open investigation | Intentional limit documented; fix only if churn is established | [ATTACK-STALL handoff](MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md) |
| 7 | U01 | After Charge the Move slot is unavailable but still shows usable distance. | UI defect | Open correction | Implemented and verified offline | [F3 handoff](FINALIZE-F3-REVIEW-HANDOFF.md) |
| 7 | U02 | The public action slot shows no selection glow while its delivery ability targets. | UI defect | Open correction | Implemented and verified offline | [F2 handoff](FINALIZE-F2-REVIEW-HANDOFF.md) |
| 7 | U03 | The Paladin class panel promises Weapon Bond upgrades to Divine Mount characters. | UI defect | Open correction | Implemented and verified offline | [SPRINT8-PALADIN-MOUNT](SPRINT8-PALADIN-MOUNT.md) |
| 7 | U04 | No mounted badge on the rider portrait; hovering a folded mount highlights nothing. | UI omission | Open correction | Implemented and verified offline | [F3 handoff](FINALIZE-F3-REVIEW-HANDOFF.md) |
| 8 | D01 | Current status is fragmented across dated handoffs and stale entry points. | Documentation | Open correction | This ledger reconciled for every ID | `CURRENT.md`, `AUTONOMOUS-RESUME.md`, `README.md` |
| 8 | D02 | Support, removal and rollback limits need one current description. | Documentation | Open correction | Documented | `README.md`, [0.2.0 notes](RELEASE-NOTES-0.2.0.md), [deferred items](FINALIZATION-DEFERRED-ITEMS.md), [UI polish handoff](UI-POLISH-REVIEW-HANDOFF.md) |
| 8 | D03 | Final sprint handoff and completion accounting. | Documentation | Open correction | Delivered | Sprint PRs and this ledger |

This table is a scaffold. D01–D03 are not done because their rows exist: the full reconciliation of current
documentation is Sprint 8's work.

## Superseded by an identified fix (carried forward)

Shipped corrections the epic records as fixes. They are not reopened by lack of native acceptance.

| Finding | Fix | Offline evidence | Native |
|---|---|---|---|
| Mounted attack retried without end when the actor could not act | 0.2.11, PR #43, merge `22945394` | [ATTACK-STALL handoff](MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md) | Not run (rows S1–S9) |
| Mounted Archery missing from every feat list | 0.2.10, PR #41, merge `81b3a30a` | [REGISTRATION handoff](MOUNTED-ARCHERY-REGISTRATION-REPAIR-HANDOFF.md) | Not run (rows R1–R5) |
| No-Standard warning shown after an ordinary completed attack | 0.2.6, PR #33, merge `b8359e31` | [ATTACK-COMPLETION handoff](ATTACK-COMPLETION-WARNINGS-HANDOFF.md) | Not run |
| Every dialogue or cutscene dismounted the pairs | 0.2.8, PR #37, merge `2dbf62c5` | [CUTSCENE handoff](CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md) | Not run |

## Manual/native verification only, excluded from this epic (carried forward)

These stay visible as evidence gaps. They create no sprint task and no acceptance gate here.

- WORLD-02 mounted trap disarm ([deferred items](FINALIZATION-DEFERRED-ITEMS.md)).
- Turn-based doors.
- Paladin and Riding Dog gameplay without Call of the Wild.
- Six-pair combat qualification.
- Prone-rider save/load and unusual fall combinations.
- Physical UI, other resolutions and the cleanup shortcut ([UI polish handoff](UI-POLISH-REVIEW-HANDOFF.md)).
- The owner's full personal mod stack.
- Native acceptance of the released exploration, door, load, pace, cutscene, Mounted Archery and attack-stall
  repairs (0.2.3–0.2.11), as recorded in `CURRENT.md`.

Feature expansion is also out of scope and unchanged: no Cavalier class, barding, enemy mounted AI, borrowed,
summoned, flying or exotic mounts, full celestial template, Run action, Mythic feat, real-time ranged rule, or
general removal of KMC content from dependent saves.
