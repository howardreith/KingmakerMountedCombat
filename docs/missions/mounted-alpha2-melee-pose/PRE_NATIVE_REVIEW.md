# Supplemental pre-native review — conditional, bounded route

Use this procedure only when the mission has a **concrete, admissible live path** and you intend to attempt it within the native budget. First inspect actual tooling, runtime locks, fixture admission and complete save/profile protection without launching. If those prerequisites are unavailable, record the specific native rows as unrun and continue the source/offline private-alpha milestone. Do not start repeated reviews to qualify a runner you cannot use.

This is an independent source/safety review before live use. It is separate from the actual factory's `single` final checkpoint and does not alter its state, rounds or acceptance. The installed controller has no preliminary-only review command. Do not fabricate `checkpoint_ready` to obtain one.

## Scope and limit

Allow **one focused review** and, if necessary, **one coherent correction batch with a targeted recheck**. Keep prior findings and evidence. A failed/partial transport is not a verdict and does not justify an indefinite retry series. If valid clearance cannot be established within this bound, do not start the optional live attempt; continue the source/offline milestone and report applicable unresolved defects honestly.

The reviewer assesses the current coherent production change and the actual planned runner/fixture/restoration path. The new mission deliberately assigns the full gameplay and visual matrix to owner alpha testing. Missing full native acceptance must not become a prerequisite for this preliminary inspection.

## Prepare an exact candidate and complete prompt

- Commit the coherent implementation, tests, relevant source materials and native plan. Use the new enrollment's guarded push and applicable gates. Reject a dirty candidate.
- Freeze the exact full source SHA. Do not develop concurrently in the candidate during review.
- Choose a **new detached worktree** for the reviewer and a **new ordinary evidence directory outside both worktrees and controller-owned state/archive paths**.
- Render [templates/PRE_NATIVE_REVIEW_PROMPT.txt](templates/PRE_NATIVE_REVIEW_PROMPT.txt), replacing every placeholder with observed identities and real, readable paths. Use the actual merge base with the new PR's selected target. Include every prior finding/disposition for a recheck.
- Make the concrete native plan and executed offline evidence readable to the reviewer. The plan must identify candidate/package identity, real input provenance, actual writable surfaces, locks, launch limits and exact restoration of current intake.
- Save the exact rendered prompt in that evidence directory. Do not write into `docs/agent-reviews/`, private controller records or installed controller files.

## Verified PowerShell transport

The argument construction follows `CodexReviewer.argv` in [the current reviewed controller](https://github.com/howardreith/development-process/blob/a6aa2b1fcfa0f946d94183acca36b2b5ddd04c0b/agent_review/agent_review.py), with the Mounted Combat project's model/config/disabled features. The supplemental result is Markdown, so this invocation omits the controller's structured `--output-schema`; it must never be presented as a controller envelope.

Confirm the actual installed `codex` supports the arguments below using its read-only version/help output if needed. Do not install or upgrade it to proceed, substitute a different model, weaken sandbox/approval settings, or enable tools disabled in this recipe. A mismatch is a transport limitation to report.

Set all variables to actual paths first. The supervisor creates the detached snapshot; the reviewer is read-only.

```powershell
# $kmcWorktree       = the new enrollment's clean feature worktree
# $kmcSafetySnapshot = a NEW detached-review directory
# $kmcSafetyEvidence = a NEW ordinary evidence directory outside both worktrees
# $kmcSafetyPrompt   = the fully rendered prompt saved in that evidence directory

$kmcCandidate = (& git -C $kmcWorktree rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $kmcCandidate -notmatch '^[0-9a-f]{40}$') {
    throw 'Cannot resolve the exact candidate.'
}
$kmcDirty = @(& git -C $kmcWorktree status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0 -or $kmcDirty.Count -ne 0) {
    throw 'The candidate must be clean and committed.'
}
if (Test-Path -LiteralPath $kmcSafetySnapshot) {
    throw 'Use a fresh snapshot directory; do not overwrite one.'
}
if (-not (Test-Path -LiteralPath $kmcSafetyPrompt -PathType Leaf)) {
    throw 'Save the complete rendered safety prompt first.'
}
$kmcPromptText = Get-Content -LiteralPath $kmcSafetyPrompt -Raw -Encoding UTF8
if ($kmcPromptText -notmatch [regex]::Escape($kmcCandidate) -or $kmcPromptText -match '__[A-Z0-9_]+__') {
    throw 'The safety prompt must name this candidate and have no unresolved placeholders.'
}

& git -C $kmcWorktree worktree add --detach $kmcSafetySnapshot $kmcCandidate
if ($LASTEXITCODE -ne 0) { throw 'Could not create the detached candidate snapshot.' }

$kmcSafetyResult = Join-Path $kmcSafetyEvidence 'pre-native-review.md'
$kmcSafetyEvents = Join-Path $kmcSafetyEvidence 'pre-native-events.jsonl'
$kmcSafetyStderr = Join-Path $kmcSafetyEvidence 'pre-native-stderr.txt'
foreach ($kmcEvidenceFile in @($kmcSafetyResult, $kmcSafetyEvents, $kmcSafetyStderr)) {
    if (Test-Path -LiteralPath $kmcEvidenceFile) { throw 'Use fresh evidence output paths.' }
}

$kmcSafetyArgs = @(
    '-a', 'never', 'exec', '--ignore-user-config',
    '-m', 'gpt-6.2-astra',
    '-c', 'model_reasoning_effort="max"',
    '-c', 'windows.sandbox="elevated"',
    '--sandbox', 'read-only',
    '--disable', 'multi_agent',
    '--disable', 'apps',
    '--disable', 'plugins',
    '--disable', 'image_generation',
    '--disable', 'computer_use',
    '--disable', 'browser_use',
    '--disable', 'in_app_browser',
    '--disable', 'hooks',
    '--disable', 'memories',
    '-C', $kmcSafetySnapshot,
    '-o', $kmcSafetyResult,
    '--json', '-'
)

$kmcPromptText | & codex @kmcSafetyArgs 1> $kmcSafetyEvents 2> $kmcSafetyStderr
$kmcSafetyExitCode = $LASTEXITCODE
if ($kmcSafetyExitCode -ne 0 -or -not (Test-Path -LiteralPath $kmcSafetyResult -PathType Leaf)) {
    throw 'The independent safety review did not complete successfully; native use remains blocked.'
}
$kmcCurrentCandidate = (& git -C $kmcWorktree rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot recheck the enrolled candidate.' }
$kmcCurrentDirty = @(& git -C $kmcWorktree status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw 'Cannot recheck the enrolled worktree cleanliness.' }
$kmcSnapshotCandidate = (& git -C $kmcSafetySnapshot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot recheck the detached candidate.' }
$kmcSnapshotDirty = @(& git -C $kmcSafetySnapshot status --porcelain --untracked-files=all)
if ($LASTEXITCODE -ne 0) { throw 'Cannot recheck the detached snapshot cleanliness.' }
if ($kmcCurrentCandidate -ne $kmcCandidate -or $kmcSnapshotCandidate -ne $kmcCandidate -or
    $kmcCurrentDirty.Count -ne 0 -or $kmcSnapshotDirty.Count -ne 0) {
    throw 'Candidate identity or cleanliness changed; this review cannot admit native use.'
}
```

Use the host's normal background supervision and the existing 60-minute reviewer timeout. Do not force-remove a dirty snapshot. The reviewer timeout does not permit interrupting a game transaction or its restoration.

The commands create only the detached worktree and ordinary evidence output; they do not launch the game, install a product, modify source, publish comments or change controller state. PowerShell redirection encoding varies; retain the files as ordinary evidence. The `-o` result is the final narrative. Report observable progress; do not display raw reasoning streams.

## Decide what the result actually permits

Before live use, verify completion and the CLI-recorded model, effort and sandbox provenance. Confirm Astra/max, read-only, no delegation, exact candidate identity and a nonempty final result. Exit zero alone is insufficient.

Require an explicit **PASS FOR GUARDED NATIVE TESTING**, with no supported blocking production or safety finding. This admits only the stated source/safety plan. Platform identity, fixture qualification, locks, backups and restoration remain independent gates; a pass cannot repair a missing prerequisite.

For CHANGES REQUIRED, make one coherent correction and use a fresh snapshot/evidence directory for the targeted recheck. Supply all findings and actual dispositions. If a later functional/safety edit invalidates the clearance, obtain a valid recheck within the same bound or leave the optional live work unrun. Evidence-only changes may reuse a result only with an explicit unchanged-production/safety relationship.

Retain unresolved findings. A still-applicable product defect remains a formal correctness obligation. A path deliberately not executed must be described as unused/unqualified, not fixed or tested. No native observation is promoted to PASS by this review.

Record prompt, candidate/base, CLI version, requested and observed model/effort/sandbox, start/end/duration, exit status, verdict and dispositions. Put a sanitized source-bound summary in the new mission's ordinary evidence and reference it from the real developer report's `evidence_refs`; raw events stay private.

The actual final factory review evaluates the complete candidate against this mission's private-alpha milestone. Supplemental evidence does not consume or fabricate a formal round, reset any budget, grant owner gameplay/visual acceptance, or authorize merge/release.

