[CmdletBinding()]
param(
    [ValidateSet('Fast','Candidate','Full')][string]$Tier = 'Fast',
    # Additional focused tests for the work in hand (directly affected evidence validators,
    # contract tests and the focused regression for the current defect), by script name.
    [string[]]$Focused = @(),
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [switch]$SkipBuild,
    # Lab root holding the immutable preview.150 evidence and ledger; when given, CANDIDATE
    # re-evaluates those artifacts under the current reader (Test-Chunk6aImmutableReplay.ps1).
    [string]$LabRoot = ''
)
# Three explicit verification tiers for Chunk 6 development (owner workflow amendment,
# 2026-10-02). Every product, safety, restoration and final-acceptance requirement is kept;
# the tiers only decide how much of the existing suite runs at each point in the loop.
#
#   FAST       during implementation: build, the component/domain and directly affected
#              source/contract tests, the directly affected evidence validators (-Focused) and the
#              focused regression for the current defect (-Focused).
#   CANDIDATE  once, before freezing a native candidate: FAST plus the runtime safety/harness core
#              (which includes the package validator), the assembly contracts, the cross-feature
#              regression readers and the ledger protocol.
#   FULL       at a milestone, at final qualification, or when a shared foundation change affects
#              the whole repository: the complete umbrella (scripts/Test.ps1).
#
# The frozen-candidate cycle itself (package, suite, purity proof, native campaign) is unchanged.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$started = [DateTime]::UtcNow
$results = @()
function Invoke-Tiered([string]$Name, [scriptblock]$Body) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $ok = $true; $detail = ''
    try { & $Body } catch { $ok = $false; $detail = $_.Exception.Message }
    $sw.Stop()
    $script:results += [pscustomobject]@{ step = $Name; pass = $ok; seconds = [Math]::Round($sw.Elapsed.TotalSeconds, 1); detail = $detail }
    Write-Host (('{0,-4} {1} ({2}s){3}' -f ($(if ($ok) { 'PASS' } else { 'FAIL' })), $Name, [Math]::Round($sw.Elapsed.TotalSeconds, 1), $(if ($ok) { '' } else { ': ' + $detail })))
    if (-not $ok) { throw ('Tier step failed: ' + $Name) }
}
function Invoke-TestScript([string]$Name, [string[]]$Extra = @()) {
    $path = Join-Path $PSScriptRoot $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw ('Test script is missing: ' + $Name) }
    $takesConfiguration = (Get-Command $path).Parameters.ContainsKey('Configuration')
    $arguments = @('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',$path)
    if ($takesConfiguration) { $arguments += @('-Configuration',$Configuration) }
    if ($Extra.Count -gt 0) { $arguments += $Extra }
    & powershell.exe @arguments | ForEach-Object { Write-Host ('    ' + $_) }
    if ($LASTEXITCODE -ne 0) { throw ($Name + ' exited ' + $LASTEXITCODE) }
}
if ($Tier -ceq 'Full') {
    Invoke-Tiered 'FULL umbrella (scripts/Test.ps1)' { Invoke-TestScript 'Test.ps1' }
} else {
    if (-not $SkipBuild) {
        Invoke-Tiered 'build' {
            $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
            $msbuild = @(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1
            if ([string]::IsNullOrWhiteSpace($msbuild)) { throw 'MSBuild not found.' }
            & $msbuild (Join-Path $repoRoot 'KingmakerMountedCombat.sln') /t:Build "/p:Configuration=$Configuration" /m:1 /v:minimal /nologo | ForEach-Object { Write-Host ('    ' + $_) }
            if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
        }
    }
    # The loop variable must not collide with Invoke-Tiered's own parameters (PowerShell variable
    # names are case-insensitive and the step body resolves names dynamically).
    $fast = @('Validate-Source.ps1','Test-Chunk6aAdditionalRegistration.ps1','Test-Chunk6aRegressionBindings.ps1','Test-Chunk6aPrimaryClaims.ps1')
    foreach ($testScript in $fast) { Invoke-Tiered ('FAST ' + $testScript) { Invoke-TestScript $testScript } }
    # A -File invocation passes a comma-joined list as one string; accept both spellings.
    $focusedScripts = @($Focused | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ } | Select-Object -Unique)
    foreach ($testScript in $focusedScripts) { Invoke-Tiered ('FOCUSED ' + $testScript) { Invoke-TestScript $testScript } }
    if ($Tier -ceq 'Candidate') {
        $candidate = @('Test-Harness.ps1','Test-AssemblyContracts.ps1','Test-PairedActivationContracts.ps1','Test-RuntimeArtifactManifestContract.ps1',
            'Test-Chunk6aSupportingEvidence.ps1','Test-Chunk6aRegressionReader.ps1','Test-Chunk6aRegressionArchives.ps1','Test-Chunk6aRegressionLedger.ps1','Test-Chunk6aLedgerProtocol.ps1')
        foreach ($testScript in $candidate) { Invoke-Tiered ('CANDIDATE ' + $testScript) { Invoke-TestScript $testScript } }
        if (-not [string]::IsNullOrWhiteSpace($LabRoot)) {
            Invoke-Tiered 'CANDIDATE Test-Chunk6aImmutableReplay.ps1' { Invoke-TestScript 'Test-Chunk6aImmutableReplay.ps1' @('-LabRoot',$LabRoot) }
        } else {
            Write-Host 'SKIP CANDIDATE Test-Chunk6aImmutableReplay.ps1 (no -LabRoot: the immutable preview.150 replay was not evaluated)'
        }
    }
}
$elapsed = [Math]::Round(([DateTime]::UtcNow - $started).TotalMinutes, 2)
Write-Host ('TIER ' + $Tier.ToUpperInvariant() + ' PASS=' + @($results | Where-Object pass).Count + ' FAIL=' + @($results | Where-Object { -not $_.pass }).Count + ' (' + $elapsed + ' min; offline verification only, no native qualification)')
