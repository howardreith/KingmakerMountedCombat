[CmdletBinding(DefaultParameterSetName = 'Show')]
param(
    [string]$BudgetId = '',
    # Record what an interrupted or unreconciled entry actually cost (seconds), with the
    # observation it rests on (a transcript, a process receipt, a log timestamp).
    [Parameter(ParameterSetName = 'Reconcile', Mandatory = $true)][string]$EntryId,
    [Parameter(ParameterSetName = 'Reconcile', Mandatory = $true)][ValidateRange(0, 86400)][double]$Seconds,
    # Import work that ran before the record existed or outside a budget-aware wrapper.
    [Parameter(ParameterSetName = 'Import', Mandatory = $true)][ValidateSet('tier', 'native', 'other')][string]$ImportKind,
    [Parameter(ParameterSetName = 'Import', Mandatory = $true)][string]$ImportSelection,
    [Parameter(ParameterSetName = 'Import', Mandatory = $true)][ValidateRange(1, 86400)][double]$ImportSeconds,
    [Parameter(ParameterSetName = 'Import', Mandatory = $true)][string]$ImportStartedUtc,
    [Parameter(ParameterSetName = 'Reconcile', Mandatory = $true)][Parameter(ParameterSetName = 'Import', Mandatory = $true)][string]$Reason
)
# Explicit, append-only accounting for the shared sprint budget (scripts/WorkflowBudget.ps1).
# Without parameters it prints the record's current state after reconciling dead entries.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'WorkflowBudget.ps1')
$id = Get-KmcWorkflowBudgetId $BudgetId
switch ($PSCmdlet.ParameterSetName) {
    'Reconcile' { $summary = Set-KmcWorkflowEntryReconciliation -RepoRoot $repoRoot -BudgetId $id -EntryId $EntryId -Seconds $Seconds -Reason $Reason }
    'Import' { $summary = Add-KmcWorkflowImportedEntry -RepoRoot $repoRoot -BudgetId $id -Kind $ImportKind -Selection $ImportSelection -Seconds $ImportSeconds -StartedUtc $ImportStartedUtc -Reason $Reason }
    default {
        $now = Get-KmcWorkflowClock
        $summary = Update-KmcWorkflowBudgetRecord $repoRoot $id { param($budget); [void](Update-KmcWorkflowBudgetReconciliation $budget $now); Get-KmcWorkflowBudgetSummary $budget $now }
        $budget = Read-KmcWorkflowBudget $repoRoot $id
        foreach ($entry in $budget.Record.entries) {
            Write-Host ('  ' + $entry.entryId + ' ' + $entry.kind + ' ' + $entry.status + ' ' + $(if ($null -ne $entry.seconds) { [string]$entry.seconds + 's' } else { '?' }) + ' :: ' + $entry.selection)
        }
    }
}
Write-Host (Format-KmcWorkflowBudgetSummary $summary)
