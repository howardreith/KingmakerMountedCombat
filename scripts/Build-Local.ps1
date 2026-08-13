[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$SkipSourceValidation
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (-not $SkipSourceValidation) {
    & (Join-Path $PSScriptRoot 'Validate-Source.ps1')
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
    throw 'vswhere.exe was not found.'
}

$msbuild = @(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($msbuild) -or -not (Test-Path -LiteralPath $msbuild -PathType Leaf)) {
    throw 'MSBuild.exe was not found through vswhere.'
}

& $msbuild (Join-Path $repoRoot 'KingmakerMountedCombat.sln') /t:Rebuild "/p:Configuration=$Configuration" /m:1 /v:minimal
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$dll = Join-Path $repoRoot "bin\$Configuration\KingmakerMountedCombat.dll"
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
    throw "Expected build output missing: $dll"
}

$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $dll).Hash.ToLowerInvariant()
$assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($dll)
Write-Host "PASS build $Configuration"
Write-Host "DLL=$dll"
Write-Host "SHA256=$hash"
Write-Host "MVID=$($assembly.ManifestModule.ModuleVersionId)"
