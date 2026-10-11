[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$boundary = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) ('kmc-tier-selection-' + [Guid]::NewGuid().ToString('N'))))
$previousTrace = $env:KMC_WORKFLOW_TEST_TRACE
New-Item -ItemType Directory -Path (Join-Path $boundary 'scripts') -Force | Out-Null
try {
    $scripts = Join-Path $boundary 'scripts'
    $trace = Join-Path $boundary 'calls.jsonl'
    $env:KMC_WORKFLOW_TEST_TRACE = $trace
    foreach ($name in @('Test-Tiers.ps1','Test.ps1')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $scripts }
    # Discover the actual umbrella's children rather than a claimed plan.
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $scripts 'Test.ps1'), [ref]$tokens, [ref]$errors)
    if ($errors.Count -ne 0) { throw 'Umbrella parse failed.' }
    $children = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Value -like '*.ps1' }, $true) | ForEach-Object { $_.Value })
    $children += @('Validate-Source.ps1','Test-SelectedFixture.ps1')
    foreach ($name in @($children | Select-Object -Unique)) {
        $path = Join-Path $scripts $name
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        @'
[CmdletBinding()]
param([string]$Configuration='Release', [string]$Target='Both', [Parameter(ValueFromRemainingArguments=$true)][object[]]$Rest)
@{ script=[IO.Path]::GetFileName($PSCommandPath); configuration=$Configuration; target=$Target } | ConvertTo-Json -Compress | Add-Content -LiteralPath $env:KMC_WORKFLOW_TEST_TRACE
$global:LASTEXITCODE=0
'@ | Set-Content -LiteralPath $path -Encoding ASCII
    }
    $runner = Join-Path $boundary 'bin/Tests/Debug/KingmakerMountedCombat.Tests.exe'
    New-Item -ItemType Directory -Path (Split-Path -Parent $runner) -Force | Out-Null
    Add-Type -OutputAssembly $runner -OutputType ConsoleApplication -TypeDefinition @'
using System;
using System.IO;
public static class TierFixture {
  public static int Main() {
    File.AppendAllText(Environment.GetEnvironmentVariable("KMC_WORKFLOW_TEST_TRACE"), "{\"script\":\"components\"}" + Environment.NewLine);
    return 0;
  }
}
'@
    $selector = Join-Path $scripts 'Test-Tiers.ps1'
    $passed = 0
    foreach ($arguments in @(
        @{ Tier='Full'; Focused='Test-SelectedFixture.ps1' },
        @{ Tier='Fast'; Focused='Test-NotPresent.ps1' },
        @{ Tier='Fast'; Focused=' , , ' },
        @{ Tier='Fast'; AssemblyTarget='Kingmaker' },
        @{ Tier='Candidate'; LabRoot=$boundary }
    )) {
        $refused = $false
        try { & $selector @arguments } catch { $refused = $true }
        if (-not $refused -or (Test-Path -LiteralPath $trace)) { throw 'Unsupported selection was not refused before work.' }
        $passed++
    }
    & $selector -Tier Candidate -SkipBuild -Configuration Debug -AssemblyTarget Kingmaker -Focused 'Test-SelectedFixture.ps1,Test-Harness.ps1,Test-AssemblyContracts.ps1,Test-SelectedFixture.ps1'
    $calls = @(Get-Content -LiteralPath $trace | ForEach-Object { $_ | ConvertFrom-Json })
    foreach ($group in @($calls | Group-Object script)) { if ($group.Count -ne 1) { throw "Repeated child: $($group.Name)" } }
    $assembly = @($calls | Where-Object script -eq 'Test-AssemblyContracts.ps1')
    if ($assembly.Count -ne 1 -or $assembly[0].target -cne 'Kingmaker' -or $assembly[0].configuration -cne 'Debug' -or
        @($calls | Where-Object script -eq 'components').Count -ne 1 -or
        @($calls | Where-Object script -eq 'Build-Local.ps1').Count -ne 0 -or
        @($calls | Where-Object script -eq 'Test-SelectedFixture.ps1').Count -ne 1) { throw 'Candidate propagation/deduplication failed.' }
    $passed++
    Remove-Item -LiteralPath $trace
    # Both real entry points execute; compiler/game/evidence leaf tests are fixtures.
    & $selector -Tier Full -SkipBuild -Configuration Debug -AssemblyTarget Kingmaker
    $calls = @(Get-Content -LiteralPath $trace | ForEach-Object { $_ | ConvertFrom-Json })
    $assembly = @($calls | Where-Object script -eq 'Test-AssemblyContracts.ps1')
    if ($assembly.Count -ne 1 -or $assembly[0].target -cne 'Kingmaker' -or
        @($calls | Where-Object script -eq 'Build-Local.ps1').Count -ne 0 -or
        @($calls | Where-Object script -eq 'components').Count -ne 1) { throw 'Full did not forward target/configuration/SkipBuild through the actual umbrella.' }
    $passed++
    Write-Host "Tier selection: PASS=$passed FAIL=0; observed Full leaf calls=$($calls.Count), offline orchestration only."
}
finally {
    $env:KMC_WORKFLOW_TEST_TRACE = $previousTrace
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    if (-not $boundary.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe cleanup path.' }
    Remove-Item -LiteralPath $boundary -Recurse -Force
}
