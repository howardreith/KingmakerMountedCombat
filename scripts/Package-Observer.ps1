[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$')]
    [string]$ArtifactQualifier
)

# This entry point retains observer-only behavior. Both entry points use the same
# synchronous clean solution build and unchanged observer manifest contract.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($SkipBuild) { throw '-SkipBuild is prohibited for qualification packages.' }
. (Join-Path $PSScriptRoot 'QualificationPackaging.ps1')
$arguments = @{ PackageKind='Observer' }
if ($PSBoundParameters.ContainsKey('ArtifactQualifier')) { $arguments.ArtifactQualifier=$ArtifactQualifier }
Invoke-KmcQualificationPackaging @arguments
