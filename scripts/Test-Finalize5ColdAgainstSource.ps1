[CmdletBinding()] param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ProducerRunId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ConsumerRunId,
    [string]$EvidenceRoot='C:\Dev\KingmakerMountedCombatLab\runtime-evidence')
# F5 final cross-run reader: the cold consumer's companions against its producer's recorded values (read-only).
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\Finalize5CompanionEvidence.ps1')
function Load([string]$RunId,[string]$Name){ Get-Content -LiteralPath (Join-Path (Join-Path $EvidenceRoot $RunId) $Name) -Raw -Encoding UTF8 | ConvertFrom-Json }
Assert-KmcFinalize5ColdAgainstSource (Load $ProducerRunId 'runtime-request.json') (Load $ProducerRunId 'chunk7-persistence-evidence.json') `
    (Load $ConsumerRunId 'runtime-request.json') (Load $ConsumerRunId 'chunk7-persistence-evidence.json')
Write-Host ('PASS F5 cold consumer '+$ConsumerRunId+' matches producer '+$ProducerRunId)
