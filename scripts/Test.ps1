[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'Build-Local.ps1') -Configuration $Configuration
}

$runner = Join-Path $repoRoot "bin\Tests\$Configuration\KingmakerMountedCombat.Tests.exe"
if (-not (Test-Path -LiteralPath $runner -PathType Leaf)) {
    throw "Test runner missing: $runner"
}

& $runner
$componentExit = $LASTEXITCODE
if ($componentExit -ne 0) {
    exit $componentExit
}

& (Join-Path $PSScriptRoot 'Test-VisualCapture.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $PSScriptRoot 'Test-InventoryHashing.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $PSScriptRoot 'Test-Harness.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $PSScriptRoot 'Test-Phase3gProtocol.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'Test-Phase3hProtocol.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $PSScriptRoot 'Test-AssemblyContracts.ps1')
