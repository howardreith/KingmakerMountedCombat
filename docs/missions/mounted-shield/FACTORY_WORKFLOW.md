# Mounted Shield — foreground software factory workflow

## Authority and scope

This guide adapts the actual reviewed factory to one new feat. The source is [development-process, 41a185a71eff60ea83b8245ff367603109969429](https://github.com/howardreith/development-process/tree/41a185a71eff60ea83b8245ff367603109969429/agent_review). Its controller/config/plan/schema/prompt/README blobs match observed merged `main` at `a6aa2b1fcfa0f946d94183acca36b2b5ddd04c0b`.

The installed source was remembered as that commit, but the local machine was offline during preparation. Inspect the actual installed `current.json`/manifest and record full package, source and fingerprint at startup. Do not guess a full package hash, install a controller from this packet, activate another enrollment or alter an immutable pinned version. Use the reviewed installed capability and reconcile any genuine difference with this guide.

The user starts this mission by giving Claude the package to execute. Preparing the ZIP did not enroll or launch anything.

## Roles and existing limits

| Setting | Required behavior |
| --- | --- |
| Main developer | The interactive Claude conversation, Opus at high effort. |
| Mode | Explicit `foreground`; no headless writer or second writer. |
| Reviewer | Controller-managed Codex `gpt-6.2-astra`, max effort, independent read-only candidate. |
| Project selector / internal ID | `kingmaker-mounted-combat` / `kmc`. |
| Branch / target | Fresh isolated `codex/*` / current `main`. |
| Plan | Existing `single` plan; one final acceptance checkpoint. |
| Formal review limits | Five accepted rounds/checkpoint, three attempts/round, no-progress stop after three unchanged blocking rounds. |
| Other configured limits | Preserve the 30-developer-invocation setting and publication limits; foreground does not justify rewriting them. |
| Required controller gate | `focused`; the project requires it even with `single`. |
| Final endpoint | Actual `AWAITING_OWNER_REVIEW`, current handoff, open draft PR; owner decides merge/release. |

The controller owns identities, receipts, review acceptance, finding history, publication, archive and final handoff. Do not replace its final review with a normal chat opinion or manually written review JSON.

## Startup

Read current repository `AGENTS.md`, `docs/VALIDATION-POLICY.md`, `CURRENT.md`, and `AUTONOMOUS-RESUME.md`, then all package governing materials. Inspect current main, remotes, clean-tree status, existing worktrees and factory claims. Preserve unrelated work and existing enrollments.

Use a unique branch such as `codex/mounted-shield` with a suffix if already taken. Make the mission and required materials available in the branch before enrollment. The controller snapshots only the single mission file; it does not automatically include siblings from the original ZIP. A valid simple arrangement is the complete packet at `docs/missions/mounted-shield/`, with the runtime PNG copied byte-for-byte to its production asset path. Alternatively follow the current source-art layout while preserving all companions and accurate references.

Commit and push the initial clean branch according to the project's verified initial-push route. Enrollment requires local HEAD to equal the remote branch HEAD. Historical `Push-KingmakerFeatureBranch.ps1` may be lab-owned; discover it, do not invent a repository path. If absent, follow the current AGENTS-authorized payload/clean-origin/fast-forward audit for the initial ordinary push. After enrollment use the controller's guarded `push`. The controller creates or reuses the draft PR; no human reviewer request is needed.

Example PowerShell commands below contain placeholders. Replace them with observed paths and returned identifiers; angle-bracket placeholders are not runnable syntax.

```powershell
$kmcController = 'C:/Dev/agent-review/agent_review.py'
python $kmcController check-project --project kingmaker-mounted-combat --repo <actual-repository-path>
python $kmcController list

# After the fresh branch is committed and initially pushed:
python $kmcController enroll --project kingmaker-mounted-combat --worktree <actual-worktree-path> --plan single --mission <absolute-path-to-MISSION.md> --mode foreground --title "[Mounted Shield] Passive mount protection"
python $kmcController status --enrollment <returned-enrollment-id>
```

`foreground_session.problems` must be `[]`. Enrollment/review/status inspect the actual recorded model, effort and permission mode. There is no separate `verify-foreground` command. Foreground uses the owner's interactive permissions; the headless permission profile is not installed automatically. Do not enable bypass mode or edit transcripts/state to pass verification.

Record the selected pinned controller path at enrollment, then confirm it against `workflow.root` in the read-only `<dir>/state.json` under the `dir` returned by enrollment. Use that pinned path if the installed current selector changes later; do not edit state. `status` does not itself return `workflow.root`. Routine `preflight` is unnecessary: it launches real model/permission probes and is not a read-only version check.

## Development, verification and review sequence

1. Implement and run focused offline checks in the main conversation. Record exact candidate and scope. A test that mirrors the intended formula without exercising production is insufficient.
2. Before live native use, satisfy the repository's independent review of the coherent production/safety change. Use the concrete supplemental Codex read-only route in `PRE_NATIVE_REVIEW.md` and the supplied prompt, on the frozen SHA. Fix blockers and retain scope/verdict/dispositions. It is deliberately separate from accepted controller records and does not mark the final checkpoint ready. Repeat it only when later production/safety changes invalidate the prior scoped review; do not rewind or reset the `single` plan. This is not an owner-approval step.
3. If actual platform, fixture, lock and restoration gates allow, run the selected guarded native cases. Group acceptance rows into a small number of existing scenarios; rows are not instructions for separate launches. Record exact binary and restore protected state. If the platform remains blocked, finish useful source/offline work and report required native rows NOT_RUN with the reason.
4. On a complete candidate, commit, guarded-push, verify on those bytes, prepare the actual schema-valid developer report and invoke the independent final factory review. Do not present a prepared template as a receipt.

```powershell
python $kmcController push --enrollment <id>

# The proposed focused script must be implemented before this command is used.
python $kmcController verify --enrollment <id> --gate focused -- -Tier Fast -Focused Test-MountedShieldFocused.ps1

# When freezing an affected native candidate, select Candidate instead.
# Candidate already includes Fast; do not run both unchanged by rote.
python $kmcController verify --enrollment <id> --gate focused -- -Tier Candidate -AssemblyTarget Kingmaker -Focused Test-MountedShieldFocused.ps1

python $kmcController review --enrollment <id> --report <absolute-current-developer-report.json>
```

The project's extra-argument grammar is `-Tier (Fast|Candidate)( -AssemblyTarget Kingmaker)? -Focused [A-Za-z0-9._-]+`. It accepts one focused script name, not a comma list. Do not pass `-AssemblyTarget` with Fast: the repository rejects it. Use the actual appropriate tier and one real focused entry point. A successful controller receipt proves only the checks it ran.

Candidate packaging is governed by the repository; do not invent a `Package.ps1 -SkipBuild` escape. Required native evidence and the preparation icon's desktop checks are separate from build/gate evidence.

## Accepted continuation and visible progress

Use a background tool process for long reviewer/validation commands so the main conversation can report observable activity. Follow `status.live_log`, or the supported watch view:

```powershell
python $kmcController watch --enrollment <id> --once
Get-Content -Wait -Tail 40 <actual-live_log-path>
```

Report role, candidate, round, elapsed time, latest observed work and blockers when material. A running process without new events is not evidence of progress. If Claude's UI buffers output, provide a directly viewable tail. Keep progress separate from the accepted verdict.

The accepted `review` result includes `round`, `decision`, `candidate`, `review_reference`, `structured_review`, exact `continuation`, findings/pending findings, prior assessments, instruction bindings and `next`. Read them. Summarize a coherent correction batch, implement it in this same conversation, update dispositions and continue within the same enrollment. Ordinary repairs do not need Howie to relay messages or say continue.

The first final review assesses the complete feature and relevant failure paths. Correction rounds resolve remaining blockers and changed consumers; they are not a license for unrelated redesign. Optional polish remains optional. No-progress or a round cap requires the recorded blocker/owner boundary, not a reset.

## Reports and owner changes

The exact schema in `templates/developer-report.v1.schema.json` rejects extra properties. Required fields are `schema_version`, `status`, `checkpoint_id`, `candidate_commit`, `summary`, `validation`, `dispositions`, `evidence_refs`, `runtime_restoration`, `blocker`, and `owner_decision_needed`.

- Report status: `checkpoint_ready`, `blocked`, `needs_owner_decision`, or `paused`.
- Validation result: `pass`, `fail`, or `not_run`; explain PARTIAL/BLOCKED evidence in `scope_and_limits` rather than inventing enum values.
- Disposition: `fixed`, `rebutted`, `deferred_optional`, or `unresolved_blocker`.
- Restoration: `not_applicable`, `restored_confirmed`, or `pending`.

The starter is deliberately blocked/unexecuted with a null candidate. Fill it with real observations before use. Use `checkpoint_ready` only for the actual checkpoint contract; missing required native evidence is not erased by source completeness.

For a later explicit owner task amendment, save the exact owner text durably outside the worktree and apply it from the same enrolled foreground session between controller commands:

```powershell
python $kmcController resume --enrollment <id> --reason "Owner amendment: <brief description>" --amend-mission <saved-owner-text-file>
```

Read the entire effective task/delivery marker, following returned instruction-part commands if needed. Include its returned `report_citation` in the next report. `--guidance` is headless-only; do not use it to route foreground task changes. Amendments preserve findings, enrollment, PR and spent budgets, and invalidate obsolete readiness. Reviewer suggestions are not owner amendments.

## Finish or a genuine blocker

Only after an actual accepted `ready_for_owner_review`:

```powershell
python $kmcController finalize --enrollment <id>
python $kmcController status --enrollment <id> --remote
```

Verify the real endpoint and `handoff_current: true`. Distinguish reviewed source H from final archive/head A; the controller may add evidence-only records after review. Present both identities and their relationship. Use the handoff template and exact resource/package hashes.

If inaccessible native platform, missing qualified fixture, occupied runtime transaction, integrity/access failure or exhausted budget prevents required work, preserve completed work and give the precise blocked acceptance rows and current controller status. Never force `AWAITING_OWNER_REVIEW`, waive a platform pin, re-enroll to erase findings or extend limits without a distinct owner decision.

No merge, tag, release, normal-play installation, controller installation/activation or mutation of other enrollments is part of this mission.

## Source references

- [Current reviewed operating guide](https://github.com/howardreith/development-process/blob/41a185a71eff60ea83b8245ff367603109969429/agent_review/README.md)
- [Mounted Combat project config](https://github.com/howardreith/development-process/blob/41a185a71eff60ea83b8245ff367603109969429/agent_review/projects/kingmaker-mounted-combat.json)
- [Generic single plan](https://github.com/howardreith/development-process/blob/41a185a71eff60ea83b8245ff367603109969429/agent_review/plans/single.json)
- [Exact report schema](https://github.com/howardreith/development-process/blob/41a185a71eff60ea83b8245ff367603109969429/agent_review/developer-report.v1.schema.json)
- [Reviewer contract](https://github.com/howardreith/development-process/blob/41a185a71eff60ea83b8245ff367603109969429/agent_review/prompts/reviewer.md)
- [Repository validation policy](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/docs/VALIDATION-POLICY.md)
