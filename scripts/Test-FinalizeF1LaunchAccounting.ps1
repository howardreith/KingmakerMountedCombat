[CmdletBinding()] param()
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'WorkflowBudget.ps1')
. (Join-Path $PSScriptRoot 'runtime\FinalizeF1LaunchAdmission.ps1')
$repo=Split-Path $PSScriptRoot -Parent
$priorRoot=$env:KMC_WORKFLOW_BUDGET_ROOT
$priorEntry=$env:KMC_WORKFLOW_BUDGET_ENTRY
$fixture=Join-Path $repo ('obj\f1-launch-accounting\'+[Guid]::NewGuid().ToString('N'))
$env:KMC_WORKFLOW_BUDGET_ROOT=$fixture
try {
    Update-KmcWorkflowBudgetRecord $repo 'finalize-f1' {
        param($budget)
        $budget.Record.entries=@([pscustomobject]@{entryId='refused-before-start';kind='native';launch=$true;status='failed';seconds=17},
            [pscustomobject]@{entryId='start-fails';kind='native';launch=$true;status='running';startedUtc=[DateTime]::UtcNow.AddSeconds(-3).ToString('o');seconds=$null;excludedSeconds=0;stepStartedUtc=$null;suspendedUtc=$null})
    }
    $record=(Read-KmcWorkflowBudget $repo 'finalize-f1').Record
    Assert-KmcFinalizeF1LaunchAllowance $record $false
    if($record.entries[0].status-cne'failed'-or$record.entries[0].seconds-ne17){throw 'Prelaunch failure/time lost'}
    Write-Host 'PASS prelaunch failure and measured spend retained without a real launch'
    $admission=[pscustomobject]@{BudgetId='finalize-f1';Nested=$false;EntryId='start-fails'}
    $threw=$false
    try { Invoke-KmcFinalizeF1GameStart $repo $admission { throw 'Simulated external process-start failure' } } catch { $threw=$true }
    if(!$threw){throw 'Start failure did not propagate'}
    Complete-KmcWorkflowEntry $repo $admission -Status failed
    $record=(Read-KmcWorkflowBudget $repo 'finalize-f1').Record
    if($null-eq$record.entries[1].PSObject.Properties['actualGameStartAttemptUtc']-or$record.entries[1].status-cne'failed'-or$record.entries[1].seconds-lt3){throw 'Failed start or its spend was lost'}
    Write-Host 'PASS actual attempted start that throws remains counted and failed'
    Update-KmcWorkflowBudgetRecord $repo 'finalize-f1' {
        param($budget)
        $budget.Record.entries+=@([pscustomobject]@{entryId='start2';launch=$true;actualGameStartAttemptUtc='2026-11-04T00:01:00Z'},
            [pscustomobject]@{entryId='start3';launch=$true;actualGameStartAttemptUtc='2026-11-04T00:02:00Z'},
            [pscustomobject]@{entryId='start4';kind='native';launch=$true;status='running'},
            [pscustomobject]@{entryId='start5';kind='native';launch=$true;status='running'})
    }
    $admission.EntryId='start4'; $script:called=$false
    Invoke-KmcFinalizeF1GameStart $repo $admission { $script:called=$true }
    $record=(Read-KmcWorkflowBudget $repo 'finalize-f1').Record
    if(!$script:called-or$null-eq$record.entries[4].PSObject.Properties['actualGameStartAttemptUtc']){throw 'Authorized fourth actual attempt was not admitted and marked'}
    Write-Host 'PASS the authorized fourth actual attempt is admitted and marked before process start'
    $admission.EntryId='start5'; $script:called=$false; $refused=$false
    try { Invoke-KmcFinalizeF1GameStart $repo $admission { $script:called=$true } } catch { $refused=$true }
    if(!$refused-or$script:called){throw 'Fifth actual attempt was not refused before process start'}
    $record=(Read-KmcWorkflowBudget $repo 'finalize-f1').Record
    if($record.entries[0].seconds-ne17-or$null-ne$record.entries[5].PSObject.Properties['actualGameStartAttemptUtc']){throw 'Refusal rewrote history or consumed an attempt'}
    Assert-KmcFinalizeF1LaunchAllowance $record $true
    Write-Host 'PASS four actual attempts refuse a fifth; history and WhatIf preserved'
} finally {
    $env:KMC_WORKFLOW_BUDGET_ROOT=$priorRoot
    $env:KMC_WORKFLOW_BUDGET_ENTRY=$priorEntry
    # Retain the tiny test fixture for failure inspection; never touches the standing budget.
}
