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
