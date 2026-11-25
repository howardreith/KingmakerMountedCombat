# Factory workflow — Mounted Combat alpha-2 repairs

Use a **new foreground enrollment** for this mission. Preserve the accepted Mounted Shield alpha, PR #61, its branch and its enrollment. The checkpoint is the private-alpha repair milestone defined in [MISSION.md](MISSION.md); broader gameplay and visual acceptance remain with the owner's playtest.

## 1. Inspect the current dependency and create the new branch

Read current repository `AGENTS.md`, `docs/VALIDATION-POLICY.md`, `CURRENT.md` and `AUTONOMOUS-RESUME.md`, then this complete packet. Inspect the actual installed controller, repository origin, worktrees, branch state, and factory claims.

```powershell
$kmcController = 'C:/Dev/agent-review/agent_review.py'
python $kmcController list
```

Record the actual selected controller package/source/fingerprint. Do not install, update or activate another controller, edit its configuration, or run routine `preflight` probes.

At packet preparation on 2026-11-24:

| Dependency | Verified state |
| --- | --- |
| PR #61 | Open, draft, unmerged; branch `codex/mounted-shield`, target `main` |
| Accepted Shield source | `7dd2adc136d334bc10f021c9d1469c2ed7354d33` |
| Accepted archive / branch head | `122a9f94680d1fbf512a0f9ec7386a1447d0c1f7` |
| Packaged alpha-1 production | `cc17fe36f6fca17bcb7fafccf331e611454a2e4f` |
| Main | `fb743911200f150af407198c6c28519e522ef58c` |

Recheck those facts before setup. While PR #61 remains unmerged, create a new branch from the accepted alpha and **target the new draft PR at `codex/mounted-shield`**. The controller supports this through `enroll --target`; the project's default target remains unchanged.

Use these task-specific names:

| Purpose | Value |
| --- | --- |
| New branch | `codex/mounted-alpha2-melee-pose` |
| Suggested new worktree | `C:/Dev/KingmakerMountedCombatLab/worktrees/mounted-alpha2-melee-pose` |
| Complete committed packet | `docs/missions/mounted-alpha2-melee-pose/` |
| Governing task file | `docs/missions/mounted-alpha2-melee-pose/MISSION.md` |
| Ordinary private evidence | `C:/Dev/KingmakerMountedCombatLab/analysis-cache/mounted-alpha2-melee-pose-20261124/` |
| New workflow budget | `mounted-alpha2-melee-pose-20261124` |

Discover the actual existing repository root. Verify the suggested destinations are unused; preserve unknown or legitimate existing work. If a branch or enrollment for this **new** task already exists, inspect and continue it appropriately instead of creating duplicates.

After resolving `$kmcRepository` and inspecting the approved origin:

```powershell
$kmcBranch = 'codex/mounted-alpha2-melee-pose'
$kmcTarget = 'codex/mounted-shield'
$kmcBase = '122a9f94680d1fbf512a0f9ec7386a1447d0c1f7'
$kmcWorktree = 'C:/Dev/KingmakerMountedCombatLab/worktrees/mounted-alpha2-melee-pose'

git -C $kmcRepository remote get-url origin
git -C $kmcRepository worktree list --porcelain
git -C $kmcRepository fetch origin main codex/mounted-shield
git -C $kmcRepository rev-parse origin/codex/mounted-shield
git -C $kmcRepository merge-base --is-ancestor $kmcBase origin/codex/mounted-shield
```

Check every exit code and reconcile changed facts before proceeding. If the target advanced after archive A, inspect that delta, preserve legitimate later work and set `$kmcBase` to the verified current target commit before creating the worktree. Do not silently discard later changes or claim the preparation-time A is still current.

```powershell
git -C $kmcRepository worktree add -b $kmcBranch $kmcWorktree $kmcBase
```

Copy the **complete** new packet into the new mission folder. The controller snapshots only the governing mission file; companion instructions, report schemas and task materials must also be committed so the reviewer can read them.

Stage only the intended payload, commit, and perform the initial push through the existing project-owned wrapper if available. If none exists, current AGENTS permits an ordinary initial push after the payload, clean-tree, approved-origin and fast-forward audit. Do not invent a wrapper path.

```powershell
git -C $kmcWorktree add -- docs/missions/mounted-alpha2-melee-pose
git -C $kmcWorktree commit -m "Add mounted alpha 2 regression mission"
# Only after the approved initial-push audit, when no project wrapper exists:
git -C $kmcWorktree push --set-upstream origin $kmcBranch
```

**Do not fall back to targeting main from the unmerged Shield archive.** Its inherited PR61 review records would appear as foreign additions in the new review diff and fail the controller's transcript-integrity guard. Do not remove records or weaken that guard. If PR61 has merged before enrollment, prove current main contains the accepted alpha implementation, account for subsequent changes, and start the new branch from that main with `--target main`. Do not merge PR61 yourself.

## 2. Enroll once, in the developing Claude session

Use the existing project, existing `single` plan, and explicit `foreground` mode. The interactive Claude Opus/high session remains the sole writer; Codex GPT-6.2 Astra/max remains the independent reviewer.

```powershell
$kmcMission = Join-Path $kmcWorktree 'docs/missions/mounted-alpha2-melee-pose/MISSION.md'
$env:KMC_WORKFLOW_BUDGET_ID = 'mounted-alpha2-melee-pose-20261124'

python $kmcController enroll --project kingmaker-mounted-combat --worktree $kmcWorktree --plan single --mission $kmcMission --mode foreground --target $kmcTarget --title "Mounted Combat alpha 2: melee, rider poses and companion animation"
```

Capture the returned **new** `enrollment` and PR URL. Set `$kmcEnrollment` to that actual ID; do not substitute the Shield enrollment or use `--pr 61`.

```powershell
python $kmcController status --enrollment $kmcEnrollment
```

`foreground_session.problems` must be `[]`. No `verify-foreground` command exists. Never edit transcripts, permissions, controller state or another enrollment to pass a check.

Record the pinned controller path selected by enrollment. Confirm `workflow.root` in the read-only `state.json` under the returned enrollment `dir`, and use that pinned controller if the installed selector changes later; do not edit state. `status` itself does not return `workflow.root`.

After enrollment, push this branch through the controller:

```powershell
python $kmcController push --enrollment $kmcEnrollment
```

Do not retarget the enrolled PR silently. If its dependency changes, inspect the real freshness result and preserve the original accepted work.

## 3. Keep validation and native work bounded

Read the actual workflow-budget record before checks. Use the new task's default **90-minute cumulative validation allowance**, preserving any spend already recorded for this task and every project/global limit. Set `KMC_WORKFLOW_BUDGET_ID` in **every** executing shell. Do not reset or reuse the earlier `mounted-shield` or shared `development` ledgers, rotate IDs on exhaustion, or increase allowances.

Make a brief impact-to-check map. Follow the repository's 15-minute planning target and 30-minute ordinary validation-block ceiling. Reproduce concrete failures, test changed behavior and affected neighbors, and retain baseline failures with their source relationship. Do not rerun the historical full campaign.

Use a real task-focused entry point, such as `scripts/Test-MountedAlpha2Focused.ps1`, after creating it to invoke meaningful existing/new checks. This filename is proposed; do not invoke it before it exists.

```powershell
$env:KMC_WORKFLOW_BUDGET_ID = 'mounted-alpha2-melee-pose-20261124'
python $kmcController verify --enrollment $kmcEnrollment --gate focused -- -Tier Fast -Focused Test-MountedAlpha2Focused.ps1
```

The declared gate accepts one simple focused-script filename. Bundle appropriate neighbors in that script. When deliberately freezing an admissible native candidate, select Candidate **instead of** Fast for unchanged inputs:

```powershell
python $kmcController verify --enrollment $kmcEnrollment --gate focused -- -Tier Candidate -AssemblyTarget Kingmaker -Focused Test-MountedAlpha2Focused.ps1
```

Candidate includes Fast. Preserve any actual mandatory gate failure; this mission cannot turn it into a pass.

### Native readiness and optional fixture work

First inspect actual tooling, available input, runtime locks and save/profile protection without launching. The prior alpha recorded a missing historical fixture suite and incomplete settings/profile protection on the no-save smoke route. Recheck current facts; do not assume either a safe path or a continuing blocker.

If safe, supported native work is feasible, stay within these ceilings and the remaining cumulative allowance:

- Up to **30 minutes** of active fixture creation, preparation and narrowly necessary setup.
- Up to **10 additional minutes** of focused smoke/cold-load work.
- No more than **three guarded game starts**.
- No repeat of an equivalent failed attempt without a new observation or changed experiment.

These are maximums, not targets or extra allowance. Finish active saves and exact restoration. Keep immutable masters outside writable roots and use fresh ordinary copies. Record any agent-generated seed and its preparation honestly.

If tooling or safety prevents a useful attempt within those bounds, record the specific unperformed observations and continue the private-alpha source/package milestone. Do not demand vanished historical save hashes, impersonate another fixture, or build a general automation system. An observed product defect remains a correctness obligation.

## 4. Independent review before actual live use

Repository policy requires independent review of coherent production/safety changes before live use. Use [PRE_NATIVE_REVIEW.md](PRE_NATIVE_REVIEW.md) **only when a concrete admissible native path will actually be attempted**.

Allow one focused supplemental review and, if necessary, one coherent correction batch with a targeted recheck. Do not start another broad supplemental loop. If no native path is usable, complete source/offline work and the ordinary formal factory review; do not qualify an unusable runner through successive reviews.

Supplemental review does not create a controller round or final acceptance. A code/safety pass does not waive platform identity, fixture admission, locks, backups or restoration. Later functional/safety edits invalidate clearance for affected live use; obtain a valid recheck within the bound or defer that optional native work. Keep applicable source defects in the formal review obligations.

## 5. Submit the actual private-alpha checkpoint

Build/package the current candidate through the supported repository path, validate it, and read back the archive's identities. Keep the source, alpha version, DLL hash/MVID, ZIP hash/size and handoff consistent. Do not present an older package as current or invent skip-build/skip-validation flags.

Write the developer report outside the worktree and controller-owned records:

```powershell
$kmcEvidenceRoot = 'C:/Dev/KingmakerMountedCombatLab/analysis-cache/mounted-alpha2-melee-pose-20261124'
$kmcReport = Join-Path $kmcEvidenceRoot 'developer-report.final.json'
```

Render the packet's UNEXECUTED template against `templates/developer-report.v1.schema.json`; do not submit the placeholder template. Use:

- `schema_version: "agent-review.developer.v1"`.
- `checkpoint_id: "final"` for the existing single plan.
- The actual clean pushed candidate SHA.
- `checkpoint_ready` only when this mission's required source, focused-check, reviewable package and handoff conditions are satisfied.
- Actual validation commands/results/durations. Native work deferred by MISSION may be `required:false`, `result:"not_run"`, with its limits and owner-playtest assignment explicit.
- `dispositions: []` until this **new** controller has findings; thereafter answer its real pending IDs. Old Shield findings belong in referenced history, not this field.
- `runtime_restoration: "not_applicable"` if no transaction ran, or `"restored_confirmed"` only with real evidence. Pending restoration must finish.
- Current evidence references. This initial mission has no amendment citation to invent.

The milestone does not waive an observed correctness/safety defect, failed restoration or mandatory gate. Keep such blockers real. A passed source gate does not prove gameplay or visual acceptance.

```powershell
python $kmcController review --enrollment $kmcEnrollment --report $kmcReport
```

Supervise long review/validation commands using the normal background mechanism. Observe the returned live log or:

```powershell
python $kmcController watch --enrollment $kmcEnrollment --once
```

Report observable activity, not raw reasoning streams or an assumed running PID. Read the accepted decision, candidate, review reference, findings and continuation. Continue ordinary repairs in this same foreground session; do not ask the owner to relay routine findings.

Preserve the installed formal limits: **five accepted rounds per checkpoint, three attempts per round, the three-round no-progress rule and thirty developer invocations**. If actual pinned configuration differs, record it. Do not reset counters, re-enroll to erase findings, raise budgets, or relax controls.

## 6. Finish at the real owner-review boundary

Only after actual accepted `ready_for_owner_review`:

```powershell
python $kmcController finalize --enrollment $kmcEnrollment
python $kmcController status --enrollment $kmcEnrollment --remote
```

Report `AWAITING_OWNER_REVIEW` only when actually returned with a current handoff. Include the new draft PR, its dependency on PR61, enrollment, accepted review, source/package/archive relationship, checks actually performed, unresolved native/UI observations, install/rollback instructions and owner test priorities. Label an unrun game plainly.

If acceptance remains BLOCKED or PAUSED, preserve useful work and artifacts and report the exact cause. Do not manufacture a passing status. Do not merge, tag, release, install permanently, mutate the accepted Shield branch/enrollment, or change another project.

For later explicit owner scope changes, save the exact text in a new UTF-8 file outside the worktree and use the supported same-session `resume --amend-mission` route between commands. Read all returned effective-task parts and carry the returned citation. Reviewer suggestions are not owner amendments.

## Source basis

This recipe was checked against [development-process a6aa2b1](https://github.com/howardreith/development-process/tree/a6aa2b1fcfa0f946d94183acca36b2b5ddd04c0b), controller blob `afb03a1de147f5e9095dc74211a5bab59e88b483`, its Mounted Combat project and `single` plan. In `agent_review.py`, explicit target support is at 5537–5561/6557, transcript-delta protection at 2603–2616, report admission at 2816–2881, and target freshness at 2568–2585/4740–4755. Actual local installation, permissions and runtime facts must still be observed when the mission starts.

