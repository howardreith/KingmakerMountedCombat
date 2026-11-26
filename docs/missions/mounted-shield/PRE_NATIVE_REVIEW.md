# Supplemental pre-native review — exact supported CLI route

This is a **supplemental independent code/safety review**, required by the repository before live use. It is not an accepted factory review, does not alter the `single` plan or any checkpoint, and does not satisfy final acceptance. Keep `--plan single --mode foreground` for the actual factory enrollment.

The installed controller has no preliminary-only review command. Its `accept_report` refuses required nonpassing validation and stops on blocked reports before launching the reviewer. Do not fabricate `checkpoint_ready` to obtain a pre-native review. Use the standard Codex CLI route below for the coherent implementation and again for any production or safety correction before another native run. Final complete-candidate review remains controller-owned.

## Source and verification

The argument construction comes from `CodexReviewer.argv` at lines 879–892 of [the reviewed controller](https://github.com/howardreith/development-process/blob/41a185a71eff60ea83b8245ff367603109969429/agent_review/agent_review.py), using its Mounted Combat project model/config/disabled features. The only transport difference is omitting the controller's `--output-schema`: the supplemental result is a clearly labeled Markdown review, not a forged `agent-review.review.v1` envelope.

Locally inspected `codex --help` and `codex exec --help` from `codex-cli 0.172.0-alpha.2` on 2026-11-23 independently confirm `-a never`, `exec`, `--ignore-user-config`, `-m`, `-c`, `--sandbox read-only`, repeatable `--disable`, `-C`, `-o`, `--json` and stdin `-`. The actual Windows installation must support those same arguments; inspect its help if it differs. No model was launched during this package research.

## Before invocation

- Commit the coherent implementation, tests, source material and safety plan. Use the enrolled guarded push/gates as appropriate. Reject a dirty candidate.
- Freeze the exact full source SHA. Use a new detached worktree at that SHA for the independent reader. Creating this snapshot changes Git worktree bookkeeping only; it does not modify the enrolled branch or product files.
- Put prompt/result/events/stderr in a fresh ordinary evidence directory **outside** the detached snapshot and outside the controller-owned state/archive. Do not write into `docs/agent-reviews/`, private controller records or installed controller files.
- Render `templates/PRE_NATIVE_REVIEW_PROMPT.txt` by replacing every placeholder with observed identities and stable candidate-relative paths. Save that exact rendered prompt. Give the reviewer complete relevant source, current native safety plan, focused checks and previous safety findings/dispositions.
- Freeze first; do not develop in the candidate concurrently with its review.

## PowerShell transport

This is a command recipe with explicit input variables, not a command to paste with unresolved values. Set every path to a new/existing appropriate actual location before running. The supervising Claude performs the snapshot setup; the reviewer itself is read-only.

```powershell
# Set these to observed absolute paths first:
# $kmcWorktree       = the enrolled clean feature worktree
# $kmcSafetySnapshot = a NEW detached-review directory
# $kmcSafetyEvidence = a NEW ordinary evidence directory outside both worktrees
# $kmcSafetyPrompt   = fully rendered prompt in that evidence directory

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

Use the host's normal background process supervision and a 60-minute reviewer timeout; the reviewer is read-only, so a timed-out reviewer can be stopped without interrupting a game restoration. The commands above do not launch the game, install a candidate, change controller state, publish a comment or alter product source. They create only a detached Git worktree and ordinary evidence output. Do not force-remove a snapshot that became dirty.

PowerShell versions encode redirected event output differently; treat these as ordinary retained text logs, not controller-parsed envelopes. The reviewer result written by `-o` is the final narrative. Never display raw reasoning streams as progress; report observable tool/result activity only.

## Admission after the command

Exit zero and a nonempty result are necessary, not sufficient. Before native use, inspect the completed event stream and actual CLI-recorded model/effort/sandbox provenance. Confirm the reviewer ran Astra/max read-only without delegation and finished successfully; a failed/partial stream or absent provenance is a review limitation, not a pass. Confirm the result names the exact candidate and contains an explicit `PASS FOR GUARDED NATIVE TESTING` with no blocking production or safety finding. That verdict only clears code/safety inspection; existing platform pins, qualified fixtures, locks, backups and restoration gates still apply independently.

If the verdict is `CHANGES REQUIRED`, correct the findings in the main conversation, commit a new candidate, and obtain a new supplemental safety review before live use. Keep prior findings and actual dispositions. Do not repeat an unchanged failed approach or turn this into an unbounded parallel factory; a repeated unresolved blocker is reported honestly. If a functional or safety change follows any native run or final factory review, repeat this check before the next live use. Evidence-only changes can reuse it only with an explicit unchanged-production/safety relationship.

Record the exact prompt, candidate SHA, CLI version, requested and observed model/effort/sandbox, start/end/duration, exit status, review text and dispositions. Put a sanitized source-bound safety record in normal mission evidence available to the final reviewer; raw events stay private. Reference it from the real developer report's `evidence_refs`. Never fabricate an accepted factory round, decrement counters, alter a plan or treat this result as `ready_for_owner_review`.

The final factory `review --report` still evaluates the complete tested candidate under the original mission and limits. Supplemental safety review does not resolve the final native evidence gap.

