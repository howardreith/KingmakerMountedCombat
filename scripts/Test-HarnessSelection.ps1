[CmdletBinding()]
param()
# PR-5 review R4-F1: the actual harness dispatcher (scripts/HarnessDispatch.ps1, dot-sourced by scripts/Test-Harness.ps1) on two
# neighboring cases: an exact -Only selection runs that case alone; an unknown selection runs nothing and is a failure; an ordinary
# invocation (no -Only, as Candidate and Full call it) runs both cases although the old KMC_HARNESS_ONLY variable names one of
# them. Also: Test-Harness.ps1 takes -Only, dot-sources this dispatcher, defines no dispatcher of its own, never reads
# KMC_HARNESS_ONLY and fails its invocation on an unsatisfied selection or zero executed cases. Offline only.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'HarnessDispatch.ps1')
$checks = 0
$failures = New-Object 'System.Collections.Generic.List[string]'
function Check([bool]$Value, [string]$Label) { $script:checks++; if ($Value) { Write-Host ('PASS ' + $Label) } else { $failures.Add($Label); Write-Host ('FAIL ' + $Label) } }

function Invoke-Selection([string]$Only, [switch]$Omit) {
    $script:passed = 0; $script:failed = 0; $script:ran = New-Object 'System.Collections.Generic.List[string]'
    if ($Omit) { Initialize-HarnessSelection } else { Initialize-HarnessSelection $Only -Supplied }
    Invoke-HarnessTest 'selection fixture case alpha' { $script:ran.Add('alpha') } | Out-Null
    Invoke-HarnessTest 'selection fixture case beta' { $script:ran.Add('beta') } | Out-Null
    [pscustomobject]@{ Ran = @($script:ran.ToArray()); Passed = $script:passed; Failure = (Get-HarnessSelectionFailure) }
}

$previous = $env:KMC_HARNESS_ONLY
try {
    $env:KMC_HARNESS_ONLY = $null
    $exact = Invoke-Selection 'selection fixture case beta'
    Check (($exact.Ran -join ',') -ceq 'beta' -and $exact.Passed -eq 1 -and $null -eq $exact.Failure) 'an exact -Only selection runs exactly that case and is satisfied'
    $unknown = Invoke-Selection 'selection fixture case gamma'
    Check ($exact.Ran.Count -eq 1 -and $unknown.Ran.Count -eq 0 -and $unknown.Passed -eq 0 -and $null -ne $unknown.Failure) ('an unknown -Only selection runs nothing and fails the invocation (' + $unknown.Failure + ')')
    $env:KMC_HARNESS_ONLY = 'selection fixture case alpha'
    $ordinary = Invoke-Selection -Omit
    Check (($ordinary.Ran -join ',') -ceq 'alpha,beta' -and $ordinary.Passed -eq 2 -and $null -eq $ordinary.Failure) 'an ordinary invocation (-Only omitted) with the old KMC_HARNESS_ONLY variable set still runs the neighboring case (both run)'
    # PR-5 review R5-F3: an explicitly empty name is refused before any case body runs.
    $script:ran = New-Object 'System.Collections.Generic.List[string]'; $emptyRefused = $false
    try { Invoke-Selection '' | Out-Null } catch { $emptyRefused = $true }
    Check ($emptyRefused -and $script:ran.Count -eq 0) 'an explicitly empty -Only is refused with zero case bodies executed'
}
finally { $env:KMC_HARNESS_ONLY = $previous }

$harness = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Test-Harness.ps1'))
$ast = [Management.Automation.Language.Parser]::ParseInput($harness, [ref]$null, [ref]$null)
$onlyParameter = @($ast.ParamBlock.Parameters | Where-Object { $_.Name.VariablePath.UserPath -ceq 'Only' })
$ownDispatcher = @($ast.FindAll({ param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Invoke-HarnessTest' }, $true))
Check ($onlyParameter.Count -eq 1 -and $ownDispatcher.Count -eq 0 -and $harness.Contains(". (Join-Path `$PSScriptRoot 'HarnessDispatch.ps1')") -and
    $harness.Contains("Initialize-HarnessSelection `$Only -Supplied:(`$PSBoundParameters.ContainsKey('Only'))") -and $harness.Contains('Get-HarnessSelectionFailure') -and -not $harness.Contains('KMC_HARNESS_ONLY') -and
    $harness.Contains('if ($failed -ne 0 -or $passed -eq 0) { exit 1 }')) 'Test-Harness.ps1 uses this dispatcher with an invocation-scoped -Only, never reads KMC_HARNESS_ONLY, and fails on an unsatisfied selection or zero executed cases'
# PR-5 review R5-F2: the legacy Horse case's compiled binding child receives the caller's Configuration and resolves that product.
$contract = Join-Path $PSScriptRoot 'Test-MountedAlpha2BiteContract.ps1'
$contractText = [IO.File]::ReadAllText($contract)
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
foreach ($configuration in @('Debug', 'Release')) {
    $arguments = Get-HarnessBindingContractArguments $contract $configuration
    $given = @(for ($i = 0; $i -lt $arguments.Count - 1; $i++) { if ($arguments[$i] -ceq '-Configuration') { $arguments[$i + 1] } })
    $product = Join-Path $repoRoot ('bin\' + $given[0] + '\KingmakerMountedCombat.dll')
    Write-Host ('  R5-F2 ' + $configuration + ': child=' + ($arguments -join ' ') + ' product=' + $product)
    Check ($given.Count -eq 1 -and $given[0] -ceq $configuration -and $product.EndsWith('bin\' + $configuration + '\KingmakerMountedCombat.dll')) ('the legacy Horse binding child receives -Configuration ' + $configuration + ' and resolves bin\' + $configuration)
}
$harnessParameters = (Get-Command (Join-Path $PSScriptRoot 'Test-Harness.ps1')).Parameters
$umbrella = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Test.ps1'))
Check ($contractText.Contains("('bin\' + `$Configuration + '\KingmakerMountedCombat.dll')") -and $harnessParameters.ContainsKey('Configuration') -and
    $harness.Contains('Get-HarnessBindingContractArguments $bindingContract $Configuration') -and
    $umbrella.Contains("& (Join-Path `$PSScriptRoot 'Test-Harness.ps1') -Configuration `$Configuration")) 'Test-Harness.ps1 declares Configuration (forwarded by Test-Tiers), the Full umbrella passes it, and the binding child resolves its product from it'
if ($failures.Count -ne 0) { Write-Host ('HARNESS SELECTION FAIL ' + $failures.Count + ' of ' + $checks); exit 1 }
Write-Host ('HARNESS SELECTION PASS ' + $checks + '/0 (offline)')
