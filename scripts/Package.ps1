[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')]
    [string]$ArtifactQualifier,
    [switch]$IncludeRemovalObserver
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($SkipBuild) { throw '-SkipBuild is prohibited for qualification packages.' }
. (Join-Path $PSScriptRoot 'QualificationPackaging.ps1')
$kind = if ($IncludeRemovalObserver) { 'Both' } else { 'Main' }
$arguments = @{ PackageKind=$kind }
if ($PSBoundParameters.ContainsKey('ArtifactQualifier')) { $arguments.ArtifactQualifier=$ArtifactQualifier }
Invoke-KmcQualificationPackaging @arguments
