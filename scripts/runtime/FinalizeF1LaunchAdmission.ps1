# Admission's launch flag reserves a possible launch. Only the explicit start boundary counts.
function Assert-KmcFinalizeF1LaunchAllowance($Record, [bool]$WhatIfOnly) {
    if ([string]$Record.budgetId -cne 'finalize-f1' -or $WhatIfOnly) { return }
    $launches=@($Record.entries | Where-Object {
        $null -ne $_.PSObject.Properties['actualGameStartAttemptUtc'] -and
        -not [string]::IsNullOrWhiteSpace([string]$_.actualGameStartAttemptUtc)
    }).Count
    # Howie's forwarded stale-row mission raised the cap once, from three to exactly four.
    if ($launches -ge 4) { throw 'F1 has used its maximum four real launch attempts; no new launch is authorized.' }
}

function Invoke-KmcFinalizeF1GameStart {
    param([string]$RepoRoot, $Admission, [scriptblock]$StartGame)
    if ([string]$Admission.BudgetId -ceq 'finalize-f1') {
        $attemptId = if ($Admission.Nested) { [string]$Admission.AttemptEntryId } else { [string]$Admission.EntryId }
        [void](Update-KmcWorkflowBudgetRecord $RepoRoot $Admission.BudgetId {
            param($budget)
            $entry = Find-KmcWorkflowEntry $budget $attemptId
            if ($null -eq $entry -or $entry.status -cne 'running' -or $entry.kind -cne 'native' -or -not $entry.launch) {
                throw 'F1 game start has no live native admission.'
            }
            if ($null -ne $entry.PSObject.Properties['actualGameStartAttemptUtc']) { throw 'F1 admission already attempted a game start.' }
            Assert-KmcFinalizeF1LaunchAllowance $budget.Record $false
            $entry | Add-Member actualGameStartAttemptUtc (Get-KmcWorkflowClock).ToString('o')
        })
    }
    # Commit immediately before invoking the actual start, including a start call that throws.
    & $StartGame
}
