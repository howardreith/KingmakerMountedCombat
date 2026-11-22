# Mounted Combat city toolbar and prone target mission

Implement these three items in **Mr. Bard's Kingmaker Mounted Combat**, using the existing Claude writer / Codex reviewer workflow. Continue through ordinary implementation, review findings, corrections and re-review until the candidate is ready for my final review or there is a precise blocker requiring my decision.

Repository: `howardreith/KingmakerMountedCombat`. Target the current authorized `main`. Use a dedicated `codex/*` branch and isolated worktree; suggested branch: `codex/mounted-city-and-prone-fixes`.

## 1. User reports and intended result

1. The Mount / Dismount icon disappears from the toolbar slot where I put it. I suspect visiting the home city triggers it, but have not established that.
2. I want animal companions accessible through the usual inventory and character interfaces while in the city. This might be related to the disappearing control.
3. A mounted character using a ranged weapon appears unable to attack a prone target.

Treat these as three separately tracked requirements. Investigate whether the first two share a cause; do not assume they do. Fix the underlying behavior with the smallest coherent changes. This is a focused maintenance mission, not another whole-mod remediation epic.

## 2. Use the installed writer and reviewer workflow

### Launcher instructions — initial interactive session only

Read the installed controller's operating guide/help and the repository's `AGENTS.md`, `docs/VALIDATION-POLICY.md`, current sections of `CURRENT.md` and `AUTONOMOUS-RESUME.md`. Check the actual checkout, origin, working state and existing enrollments before changing anything.

The intended existing configuration is:

| Setting | Value |
| --- | --- |
| Controller entry point | `C:/Dev/agent-review/agent_review.py` |
| Project | `kingmaker-mounted-combat` |
| Plan | `single` |
| Writer | Claude `opus`, high effort |
| Independent reviewer | Codex `gpt-6.2-astra`, high reasoning effort |
| Required validation gate | `focused` |
| Successful final workflow state | `AWAITING_OWNER_REVIEW` |

Verify that the installed version supports this configuration. Reuse it; do not create a replacement review loop, downgrade models, edit the installed controller, or change its permission policy. A missing optional `list` command alone is not a reason to upgrade the controller. Leave Black Tentacles and unrelated enrollments alone.

Prepare an isolated worktree from current `origin/main`, put this complete mission in a tracked Markdown file, and commit and push the clean new branch through the repository-approved route. Resolve the real paths rather than guessing the checkout location. Enrollment requires the local head and remote branch head to match, and creates the draft PR itself; do not create a duplicate PR manually.

With `$worktree` and `$mission` set to those resolved absolute paths:

```powershell
$ar = 'C:/Dev/agent-review/agent_review.py'
python $ar check-project --project kingmaker-mounted-combat --repo $worktree
python $ar enroll --project kingmaker-mounted-combat --worktree $worktree --plan single --mission $mission --mode headless --title '[draft] Fix city companion access, mount toolbar persistence and prone ranged targeting'
```

Capture the actual enrollment ID. Start that enrollment with `python $ar run --enrollment <returned-id>` and inspect it using `python $ar status --enrollment <returned-id> --remote`. Substitute the returned ID; never invent one. Once the controller owns the worktree, the launcher must stop editing it or running a second writer against it.

This mission authorizes its scoped implementation, focused checks, branch publication, draft PR and the controller's automated review exchange. It does not authorize merging, releases, tags, permanent installation or human reviewer requests. Preserve the established runtime guards and permissions.

### Worker instructions — controller-launched Claude

If you are already running inside an enrollment, skip the launcher steps. Never enroll yourself again, launch another controller, or create another PR. Use the enrollment's allowed `push` and `verify` helpers; let the controller publish the review exchange.

Implement the mission, commit a coherent candidate, push it through the guarded helper, and obtain the required `focused` gate receipt for that same clean commit. Use the actual supported focused script selection. If the existing entry points cannot cover these changes together, add a small repository-owned focused script that runs the necessary checks; do not loosen controller argument restrictions or validation rules.

Codex must inspect the mission, full feature diff, relevant surrounding code, evidence and prior findings independently. Address supported findings, explain any disagreement with evidence, and continue correction/review cycles automatically within the installed limits. Routine rounds do not need my approval. Do not reset budgets, repeatedly re-enroll, or relax requirements to force a pass. A final material change requires current validation and a fresh review.

## 3. Preserve the current architecture and compatibility

Use the repository's existing services, narrow game adapters and test infrastructure. Keep business rules out of patch callbacks. Verify the actual Kingmaker contracts; do not assume a Wrath API or visual transform represents gameplay authority. Preserve stable blueprint, actor, pair and save identities.

Retain the Tabletop Rules Expansion click-handler compatibility repair released in 0.2.13. Read the current X01 notes and relevant code. If this mission changes shared click/command handling, run the existing focused composition regression, including both patch orders. Do not replace or duplicate native command submissions that another mod also patches.

Native action budgets, initiative, targeting legality and resource spending remain authoritative. No action refunds, free attacks, fabricated preparation, or global targeting overrides. Add no dependency unless it replaces substantial necessary code and its benefit is justified.

## 4. Mount and dismount shortcut persistence

Trace what happens to the rider, companion, control ability and toolbar binding across entry to the home city, interior transitions, departure, forced dismount and save/load. Establish whether the failure is ability removal, roster eligibility, stale ability data, serialization, UI rebuilding, or another cause.

Useful existing landmarks include `NativeMountedControlService`, `CollectCurrentCandidateUnits`, `ShouldLease`, `ReconcileFacts`, `ReconcilePublicControlSlots`, `CapturePersistentSlots`, `RestorePersistentSlots`, and the one-time placement records. These are investigation starting points, not a proven diagnosis.

Required behavior:

- A deliberately placed Mount / Dismount shortcut retains its chosen supported toolbar slot across ordinary city visits and return to adventuring, including saving and reloading in either location.
- Temporary mount unavailability or a forced dismount does not erase the player's arrangement. Preserve the binding or its recoverable placement intent while showing ordinary unavailability when mounting is illegal. Never keep a stale callable reference to a discarded entity.
- Moving or deliberately removing the shortcut is respected. Do not keep auto-inserting it, overwrite another ability, duplicate it, or reset the toolbar.
- Handle another rider independently. Ordinary abilities and unrelated slots remain unchanged.
- Genuine companion removal, mod disable/removal and world disposal still perform their required cleanup. Temporary absence must be distinguished from permanent loss of eligibility.

Prefer repairing the existing ownership and persistence path. Do not add a second competing toolbar manager or a periodic re-add workaround. Preserve existing save compatibility; explain any unavoidable format change before adopting it.

## 5. Animal companion access in the city

Implement companion inspectability through the city's normal inventory/character selection interfaces. First determine which real character roster those interfaces use and why companions are missing.

Default scope: include legitimate animal companions of owners eligible for the city's normal character/inventory roster. Follow that roster's existing rules for reserve companions rather than assuming only the travelling party is eligible. Include ordinary animal companions as well as the mod's companions; being mountable is not a prerequisite for being inspectable.

Required behavior:

- Selecting an animal companion opens that exact companion's character details and normal equipment/inventory view, with its correct name, statistics, equipment and permitted interactions.
- Use the existing persistent companion instance. Do not create replacement animals, copy inventories, or leave selection pointing at an old instance after a load or transition.
- City entry/exit, save/load and repeated panel opening produce no duplicate portraits, phantom entries, lost equipment or broken selection.
- Handle owners without pets and dead or temporarily unavailable pets consistently with the native interface's rules. Exclude temporary summons, visual mount proxies, unrelated animals and companions whose owners are ineligible for that roster.
- Preserve the existing companion rename behavior and return selection sensibly if a selected companion ceases to be eligible.

UI visibility must not require recruiting the animal as a normal party member, consuming a party slot, changing ownership, spawning it into city scenes, altering formation, XP or combat participation, or overriding scripted party transitions. Prefer a bounded UI roster projection over changing the adventuring party. If engine contracts prevent a safe implementation within that boundary, finish the other independent work and report the exact constraint and proposed alternative; do not silently omit this requirement or label the whole mission complete.

## 6. Mounted ranged attacks against prone targets

Find the precise failure stage: mouse targeting, command admission, approach/pathing, range or line-of-sight evaluation, firing readiness, or execution. Compare mounted/dismounted and prone/standing under matched conditions: same attacker, weapon, enemy, position, available actions and other relevant state.

Use the reported weapon if identifiable from available evidence. Otherwise begin with an existing representative ranged fixture, record that limitation, and add another weapon only when the changed code or installed compatibility path warrants it.

Required behavior:

- An otherwise valid living prone enemy in legal range and clear line of sight can be attacked through ordinary player controls while mounted.
- The attack actually executes. A cursor, target highlight or queued command is insufficient; show the expected attack roll or normal resolution, with no endless approach, silent cancellation or wait for the target to stand up.
- Do not require the mount to reach melee range for a ranged shot, accidentally substitute mount attacks, or let graphical rider height corrupt the authoritative targeting calculation.
- Preserve normal prone modifiers, mounted ranged penalties, ammunition requirements and action expenditure. Do not remove prone status or grant additional attacks.
- Preserve standing-target attacks, unmounted ranged attacks and mounted melee. Walls, illegal targets and unavailable actions must still prevent attacks; ordinary approach behavior for out-of-range targets must remain valid.
- Cover real-time and turn-based behavior where the changed path is shared. If paired turn/action ownership changes, cover both initiative arrangements and ensure each native cost is paid exactly once.

## 7. Focused verification and evidence

Follow `docs/VALIDATION-POLICY.md`: choose changed behavior and affected neighbors, use the required gate, and keep the existing planning/budget limits. Candidate already includes Fast. Do not automatically launch Full, historical campaigns, whole-lab rehashes or broad release qualification for these fixes.

Use behavior-based tests with real cooperating components where practical; narrowly substitute external/native boundaries only where necessary and label them. Compilation and simulated command execution do not prove native UI or combat behavior.

Maintain a compact acceptance record covering:

| Area | Essential cases |
| --- | --- |
| Toolbar transitions | Chosen nondefault slot; repeated city entry/exit; mounted and dismounted arrival where valid; ordinary interior transition. |
| Toolbar persistence and intent | Save/load inside and outside the city; temporary mount absence; user moves/removes shortcut; occupied slots; another rider. |
| City companion UI | Correct owner/pet mapping; normal equipment inspection and permitted changes; eligible reserve-owner case; multiple pets; exclusions; repeated panels/transitions/save/load. |
| Prone ranged attack | Matched mounted/dismounted and prone/standing controls; ordinary input through actual resolution; representative near and normal firing distances; both relevant combat modes. |
| Negative controls and neighbors | Blocked line of sight, unavailable actions, normal range handling, standing targets, unmounted ranged, mounted melee; unchanged mounted penalties. |
| Shared regressions | Affected save/lifecycle paths and, when command seams change, existing Tabletop Rules Expansion patch-composition checks. |

Record each case's result and evidence type, with source/candidate identity and relevant binary/fixture identity. Distinguish PASS, FAIL, NOT RUN and BLOCKED. Do not turn an old result into evidence for changed bytes.

Perform focused native checks when the environment and existing authorization permit them, after the independent review required by repository policy. Use only the existing guarded runtime path and disposable fixtures, honor shared runtime locks, and restore the actual pre-run installation and saves. Do not interrupt a human game session or manually deploy a DLL to get around a blocked runner.

The `single` plan has one final checkpoint, not an automatic intermediate checkpoint. If native checks are feasible and selected, list their pending evidence in the first review request. Codex should review the production and safety changes and keep that required evidence gap open with `changes_required`; Claude can then run the reviewed changes through the guarded runtime path and submit the results for final review. A later production or safety correction needs independent review before live use. Do not finalize first and assume the controller will then return to testing.

The required deliverable is a complete source implementation and independently reviewed development candidate. Native cases that cannot be run may be explicitly deferred to owner verification under repository policy, with exact steps and the missing prerequisite. This is not permission to call those behaviors game-tested. Unresolved implementation defects or safety concerns remain blockers; do not weaken the mission to obtain approval.

## 8. Final handoff

Leave one draft PR and a durable, current review record. A successful workflow ends at `AWAITING_OWNER_REVIEW`, with no merge, release, tag or permanent install.

Report:

1. PR link, branch, enrollment ID, reviewed feature commit and final PR head; identify any controller-generated transcript-only archive commit separately.
2. Each issue's established cause, implemented fix and remaining uncertainty. State whether the city issues actually shared a cause.
3. Focused commands/results and the acceptance record, clearly separating offline evidence from native observations.
4. The independent Codex verdict and disposition of material findings, bound to the current candidate.
5. Any unrun game checks, a short owner playtest sequence, and restoration outcome if a native transaction ran.

Continue through routine review corrections without handing the work back to me between rounds. If blocked, preserve the useful completed work and report the specific decision, permission, missing input or failed requirement.

## Reference starting points

- [Controller operating guide](https://github.com/howardreith/development-process/blob/main/agent_review/README.md)
- [Mounted Combat workflow project](https://github.com/howardreith/development-process/blob/main/agent_review/projects/kingmaker-mounted-combat.json)
- [Generic single-mission plan](https://github.com/howardreith/development-process/blob/main/agent_review/plans/single.json)
- [Mounted Combat repository instructions](https://github.com/howardreith/KingmakerMountedCombat/blob/main/AGENTS.md)
- [Focused validation policy](https://github.com/howardreith/KingmakerMountedCombat/blob/main/docs/VALIDATION-POLICY.md)
- [Current project state](https://github.com/howardreith/KingmakerMountedCombat/blob/main/CURRENT.md)
- [Mount control and toolbar lifecycle](https://github.com/howardreith/KingmakerMountedCombat/blob/main/src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs)
- [0.2.13 compatibility repair](https://github.com/howardreith/KingmakerMountedCombat/blob/main/docs/RELEASE-NOTES-0.2.13.md)

These references were checked on November 21, 2026. Re-read the installed controller and current repository guidance when beginning the mission.


---

# Owner addendum (2026-11-22): bounded toolbar lifecycle repair

Everything above is the complete original mission, unchanged (also kept intact at
`docs/MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md`). This addendum adds requirements; it removes or
relaxes none. Where the two conflict, this addendum governs only on the points it states.

## A. Why this enrollment exists

The first enrollment of this branch was
`kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c` on draft PR #57. It ran 26 review rounds
and 27 writer invocations under the same writer and reviewer (reviewer effort `high` for rounds 1–16, `max` from round 17).
Its last reviewed candidate was `6298f2a5274306e35f8f0ceaf57869693cd0611f`, and its decision was `changes_required`.

The owner paused it after round 26. Rounds 20–26 each ended with 4–6 blocking toolbar lifecycle findings. Each fixed
one event ordering while an adjacent ordering failed. The toolbar component suite's private `Service` class
(`tests/MountedControlPlacementIntentTests.cs`, about lines 122–611 at `6298f2a5`) reimplemented the production
service's boundary orchestration. Passing tests therefore did not establish production behavior. Round 26's R26-F1
names one such divergence: a production reset the double omitted.

The owner authorized **one** cancellation and re-enrollment on PR #57. Its purpose is to change the repair strategy
and test architecture. Its complete review history is archived on this branch at commit `1edd82f6`
(`docs/agent-reviews/pr-57/`, records 001–114).

This exception does **not** authorize further re-enrollment, budget or limit increases, controller upgrades, state
edits or relaxed requirements. The original §2 restriction on re-enrollment applies again from here on.

Prior-run counts (26 rounds, 27 writer invocations) must stay visible in the handoff. This enrollment's counts are
additional.

## B. Scope

- **Repair now:** the toolbar persistence work of §4 only.
- **Keep stable:** the city companion roster (§5) and the mounted downed-target attack change (§6). Change them only
  if a concrete dependency requires it, and explain the dependency.
- **Correct the handoff's §6 wording** to state exactly: "The identified rejection of attacks against an
  already-unconscious target is corrected in source. The original prone-target report still needs matched in-game
  verification." The owner playtest must compare standing, conscious-prone (for example tripped) and
  already-unconscious enemies, mounted and dismounted. It must observe an actual attack roll in each case.

## C. Required design: one shared production lifecycle component

1. **One owner for the decisions.** Move the toolbar lifecycle decisions into a single production component. Both
   `NativeMountedControlService` and the tests must invoke it. The component owns the sequencing and actor selection
   at every boundary:
   - admission and first scan of a bar;
   - observation of player edits;
   - native slot invalidation (`IsBad` / `GetSlot` / `CleanupBadSlots`);
   - save-header capture and save-scope sanitation;
   - area suspension and restore;
   - selected load begin, abandon and complete;
   - unfenced world disposal or return to the main menu;
   - companion replacement and retirement;
   - disable.

   `NativeMountedControlService` keeps only the narrow adapter work: reading and writing native bars, facts, rosters
   and the player world. Relocating the existing conditionals while boundary behavior stays independently
   implemented in the test harness does **not** satisfy this.
2. **Tests use thin fakes only.** Replace the duplicated test `Service` orchestration with thin game-facing fakes:
   bar and slots, facts and AbilityData, roster membership (party, remote, detached, pets), player world identity,
   and serialization rows. Tests drive the real production component through those fakes. The duplicated test
   service does not need to survive.
3. **Separate placement from sanitation.** Keep rider placement eligibility (who can own a Mount / Dismount
   arrangement) distinct from sanitation coverage (whose managed references must be cleared).
   - Save, disable and every other applicable cleanup boundary must reach:
     - current riders;
     - reserve and detached owners' exact current-world persistent pets;
     - riders who returned to the party before the next update;
     - observed former candidates, under the existing exact-world checks.
   - Sanitation must preserve unrelated abilities, auto-use selections and equipment.
   - Placement eligibility stays scoped to riders.
4. **Keep the existing contracts.** Retain the existing save format (row kinds 6 and 7, the 128-row bound and
   validation). Retain the native contract pins, the Tabletop Rules Expansion click composition, stable identities
   and every requirement of the original §3–§4. Explain any unavoidable change before adopting it.

## D. Lifecycle properties to test through the production path

Each property needs behavioral tests that exercise the production component through the thin fakes:

1. Intent is recorded before cleanup destroys its native evidence.
2. The same player edits produce the same result whether saving or updating happens first.
3. Repeated scans or saves do not move or resurrect a shortcut.
4. A canceled load preserves the complete outgoing state, including duplicate identities and reserve-scan
   classification.
5. A successful world replacement releases the old world's state. So does an unfenced disposal or return to the main
   menu.
6. Native invalidation preserves recoverable intent; a deliberate player removal remains a removal.

## E. Regression preservation and mutation evidence

- **R1–R26 mapping.** Preserve every distinct concrete regression from findings R1–R26 of the first enrollment. The
  archived reviews in `docs/agent-reviews/pr-57/` list their triggers and requested evidence. Add a table to the
  handoff that maps each old finding ID to the test or subcase covering it. Equivalent cases may share one stronger
  test; say so in the table.
- **Production mutations.** Run focused mutations against production logic and wiring, not against test code.
  Demonstrate that the relevant behavioral tests fail when production:
  - skips pre-cleanup observation;
  - omits a required actor from a sanitation or observation sweep;
  - mishandles outgoing-world lifetime (abandon versus replacement versus unfenced disposal);
  - confuses a player edit with native invalidation.

  Record each mutation, the tests that failed and the restoration of the source. Do not commit mutated code.

## F. Inherited mandatory acceptance cases

These six cases were open at the end of the first enrollment. They are mandatory acceptance cases for this
mission.

**Provenance (applies to all six):**
- enrollment `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c`;
- review round 26, reviewed commit `6298f2a5274306e35f8f0ceaf57869693cd0611f`;
- archived review `docs/agent-reviews/pr-57/112-r26-review.md` and envelope `113-r26-review-envelope.json`.

Each case below gives the original trigger, expected behavior and requested evidence. Line locations refer to
`6298f2a5`.

**How to report them:**
- The old identifiers are **acceptance-case references**, not findings of this enrollment.
- Do not put them in structured fields that require findings pending in this enrollment, and do not insert them into
  the new controller ledger.
- The writer reports each case's outcome individually in the developer summary and handoff: PASS or FAIL, test name,
  evidence type.
- The reviewer assesses each case individually in its summary or evidence assessment.
- If a case remains broken, raise it as a new finding and name the old case it maps back to.

### AC-R26-F4 (was R26-F4, P1): save and disable cleanup skip a reserve owner's persistent pet
- **Location:** `NativeMountedControlService.cs:2681`.
- **Original sections:** §4 existing save compatibility, §4 mod disable/removal cleanup, §7 affected save/lifecycle
  paths.
- **Trigger:** An older capital save restores a remote owner whose horse holds a legacy Mount Primary reference. The
  player saves again or disables KMC before taking the owner adventuring.
- **Expected:** Cleanup reaches the current owner's persistent pet and clears the managed reference. Ordinary
  abilities and equipment are preserved.
- **Observed at `6298f2a5`:** Candidate collection follows only a travelling owner's pet. The reserve roster adds only
  the remote or detached entry itself, and the capital roster places the horse outside `RemoteCompanions`. On a cold
  load with no active pair or previous observation, the horse is missed by the slot, fact and auto-use sweeps.
- **Required evidence:**
  - Use the capital fixture's separate remote-owner and horse membership.
  - Put a stale Mount Primary slot and auto-use selection on the horse, with no backing fact.
  - Inspect both inside serialization and after disable, alongside an ordinary ability that must survive.

### AC-R22-F3 (was R22-F3, P2): the first reserve scan loses a fresh placement when the old shortcut had no duplicate
- **Location:** `NativeMountedControlService.cs:2487`.
- **Original sections:** §4 player moves, §4 older city saves, §4 save/load.
- **Trigger:** Native cleanup empties the rider's only shortcut. The player places the current control elsewhere
  before KMC first scans the reserve bar.
- **Expected:** The new placement at 11 becomes the recoverable choice. Leaving the bar untouched preserves 3.
- **Observed at `6298f2a5`:** Native invalidation records slot 3 as interrupted. With no sibling binding, the copy
  list is discarded, and the first-seen classification requires recorded copies to accept 11. The scan clears 11 and
  the archive carries 3.
- **Required evidence:**
  - Start with only slot 3 on a never-observed reserve rider.
  - Let `GetSlot` empty it, place the current live control at 11, and save before the first reserve scan.
  - Check that the archive holds 11 and one placement marker.
  - Check for exactly one shortcut at 11 after both a same-world return and a cold restore.
  - Keep the no-edit and duplicate controls.

### AC-R25-F3 (was R25-F3, P2): a scanned reserve rider cannot establish a fresh shortcut after companion replacement
- **Location:** `NativeMountedControlService.cs:2519`.
- **Original sections:** §4 player moves, §4 genuine companion removal, §4 city save/load.
- **Trigger:** A reserve rider's old arrangement has been retired, and the player places the live control for the
  replacement companion before saving.
- **Expected:** The former companion's slot stays retired. A deliberate placement made after that retirement
  establishes a new arrangement for the current companion.
- **Observed at `6298f2a5`:** After the old pet's arrangement retires, a new binding on the already-scanned bar is
  declined for the retired actor, and its slot is emptied. No recoverable slot row remains.
- **Required evidence:**
  - Scan a reserve rider with a live fact at 9.
  - Replace pet A with pet B, and update with the bar empty so A's arrangement retires.
  - Place the live control at 11, save, and cold-restore. Require one shortcut at 11 for B.
  - With no new placement, require no restoration at 9.

### AC-R26-F1 (was R26-F1, P2): canceling a load changes how the next save classifies an edited reserve bar
- **Location:** `NativeMountedControlService.cs:1778`.
- **Original sections:** §4 player moves, §4 save/load, §7 affected lifecycle paths.
- **Trigger:** KMC scans a reserve rider and clears slot 9. The player places the live ability at 11, starts a load,
  and returns to the same world before KMC observes the edit.
- **Expected:** Returning to the same player world preserves the player's edit and the information needed to
  recognize it.
- **Observed at `6298f2a5`:** Load begin resets the reserve-scan record, and abandonment restores placement records
  but not that classification. The next save takes the first-scan path, and 11 loses to the interrupted record at 9.
  The test double omitted the production reset.
- **Required evidence:**
  - Extend the scanned-reserve 9-to-11 case with a selected load abandoned before world replacement.
  - Exercise the production reset and abandonment sequence.
  - Verify that an immediate save carries 11 and one marker, and that a cold restore returns 11 once.
  - Verify that the no-edit control still carries 9.

### AC-R26-F2 (was R26-F2, P2): the duplicate registry retains a discarded Player at the main menu
- **Location:** `NativeMountedControlService.cs:2220`.
- **Original sections:** §4 world disposal, §4 discarded entity cleanup.
- **Trigger:** A world with a recorded duplicate is disposed without a selected load, and the player stays at the main
  menu.
- **Expected:** A temporarily missing Player during a selected load keeps the outgoing copies available for
  abandonment. An unfenced disposal releases them when the world goes away.
- **Observed at `6298f2a5`:** The copy registry keeps its world whenever the current Player is null, and departed-rider
  release returns on null. Placement maps are cleared on an unfenced main-menu transition, but the registry still holds
  slot objects and a strong reference to the discarded Player.
- **Required evidence:**
  - Record a duplicate on a never-observed reserve rider.
  - Return directly to the main menu before its reserve scan, and update with no replacement Player.
  - Assert zero retained copies and no held world while still at the menu.
  - Keep the fenced abandonment and real-transition controls.

### AC-R26-F3 (was R26-F3, P2): a newly returned party rider can carry a retired auto-use selection through save or disable
- **Location:** `NativeMountedControlService.cs:2937`.
- **Original sections:** §4 mod disable/removal cleanup, §4 existing save compatibility, §7 affected save paths.
- **Trigger:** A reserve rider rejoins the travelling party with a stale KMC auto-use selection, and save or disable
  runs before the next control update.
- **Expected:** The sanitation boundary clears an exact managed auto-use reference on the rider regardless of current
  roster membership. Ordinary auto-use selections survive.
- **Observed at `6298f2a5`:** Auto-use cleanup covers only reserve units, which exclude current candidates. A returning
  rider whose older Rider Primary selection has no backing fact or slot depends on a later update. An immediate save
  can serialize the selection first, and the disabled-save path skips ordinary reconciliation.
- **Required evidence:**
  - Move the stale Rider Primary fixture from reserve to the travelling party immediately before a save and,
    separately, before disable.
  - Inspect the selection inside serialization and after disable.
  - Extend the disabled-save isolation case with the same stale selection on a party rider, and keep an ordinary
    auto-use control.

## G. Limits and completion

- **Limits:** this enrollment uses five review rounds, the project's existing developer-invocation cap and
  no-progress rule, and the approved writer, reviewer and permission settings.
- **Ready for owner review only when all of these hold** at the required development-evidence level, with a passing
  `focused` gate receipt on the exact candidate:
  - the original mission;
  - this addendum, including §C–§E;
  - all six inherited acceptance cases.
- **Native checks:** cases that cannot run stay marked **NOT RUN** and are deferred to the owner playtest with exact
  steps. Do not describe them as game-tested.
- **Excluded:** no merge, release, tag or permanent installation.
