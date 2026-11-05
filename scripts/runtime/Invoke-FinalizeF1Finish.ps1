[CmdletBinding()]
param([switch]$Launch,[ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId='20261105-finalize-f1d-native-01')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot '../WorkflowBudget.ps1')
. (Join-Path $PSScriptRoot 'FinalizeF1LaunchAdmission.ps1')
$lab=Get-KmcLabRoot;$repo=Get-KmcRepositoryRoot
$env:KMC_WORKFLOW_BUDGET_ROOT=Join-Path $lab 'runtime-state/workflow-budgets'
$env:KMC_WORKFLOW_BUDGET_ID='finalize-f1'
$env:KMC_WORKFLOW_NATIVE_BATCH='finalize-f1:capture-restored-1'
if($Launch){
 $budget=Read-KmcWorkflowBudget $repo 'finalize-f1';$summary=Get-KmcWorkflowBudgetSummary $budget ([DateTime]::UtcNow)
 Assert-KmcFinalizeF1LaunchAllowance $budget.Record $false
 if($summary.RemainingSeconds-lt1320){throw 'F1 finishing admission requires full1200s plus120s preparation remaining; no staging or game start.'}
}
$options=@{Scenario='finalize-f1-finish';RunId=$RunId;TimeoutSeconds=1200;BaseGameOnly=$true;SaveAccessAllowed=$true;Confirm=$false
 PackagePath=(Join-Path $lab 'artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f1d-diagnostic-diagnostic.zip')
 ObserverPackagePath=(Join-Path $lab 'artifacts/KmcRemovalObserver-0.1.0-observer.8-finalize-f1d-diagnostic.zip')
 RetainedDependencySelectionPath=(Join-Path $lab 'analysis-cache/finalize-f1-contracts/owner-scroll-selection.json')
 QualificationSuiteSnapshotPath=(Join-Path $lab 'runtime-state/qualification-suite-snapshots/finalize-f1d-8c5be488-20261105.json')
 ExpectedQualificationSuiteId='finalize-f1d-8c5be488-20261105';ExpectedQualificationSuiteSnapshotSha256='13dec54e92437da65e9c3e46ce6887a2e7502900561feb1daf0013f4bd543198'
 FrozenSourceRoot=(Join-Path $lab 'worktrees/finalize-f1d-8c5be488');FrozenSourceFreezePath=(Join-Path $lab 'analysis-cache/finalize-f1-contracts/f1d-product-freeze.json')
 ExpectedFrozenSourceFreezeSha256='b77c58bc0f12c5d4ac0ecbb44934d9b70db3db6003acda155c8d60a04b311f35'}
# The runner forbids DevelopmentScopedAudit on live runs. Historical archive
# hashing exists only in its WhatIf/ShouldProcess-false branch; live keeps its
# ordinary full current-state guards without that dry-run archive audit.
if($Launch){& (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeScenario.ps1') @options}
else{& (Join-Path $PSScriptRoot 'Invoke-KingmakerRuntimeScenario.ps1') @options -WhatIf -DevelopmentScopedAudit}
