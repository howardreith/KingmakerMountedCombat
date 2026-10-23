[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ObserverResultPath,
    [Parameter(Mandatory = $true)][string]$RequestPath,
    [Parameter(Mandatory = $true)][string]$ObserverRequestPath,
    [Parameter(Mandatory = $true)][string]$GameResultPath,
    [Parameter(Mandatory = $true)][int]$ExpectedProcessId,
    [Parameter(Mandatory = $true)][DateTimeOffset]$NotBeforeUtc
)

# Validates the request-free normal-player observer's own result against the
# run's request and the observer request the process was bound to, then
# composes the run's game-result record. KMC wrote nothing: it received no
# automation request, so every verdict is the external observer's own check.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

Assert-KmcManifestJsonMembersUnique $ObserverResultPath 'normal-player observer result'
$result = Read-KmcJson $ObserverResultPath
$request = Read-KmcJson $RequestPath
$observerRequest = Read-KmcJson $ObserverRequestPath
Assert-KmcChunk7NormalPlayerDescriptor $request
if ([int]$observerRequest.schemaVersion -ne 3 -or [string]$observerRequest.runId -cne [string]$request.runId -or
    [string]$observerRequest.transactionToken -cne [string]$request.transactionToken) { throw 'Observer request is not this run''s normal-player request.' }
Assert-KmcChunk7NormalPlayerObserverResult $request $observerRequest $result (Get-KmcSha256 $ObserverRequestPath) $ExpectedProcessId $NotBeforeUtc
$evidenceManifestSha256 = Get-KmcValidatedOrchestrationArtifactManifestHash -Request $request -AllowIncompleteScenarioEvidence
$errors = @($result.errors | ForEach-Object { [string]$_ })
# A failure before any check still records one failed assertion, never an empty row.
$failedAssertions = [Math]::Max([int]$result.checkFailCount, $(if ($errors.Count -ne 0) { 1 } else { 0 }))
$game = [ordered]@{
    schemaVersion = 2; evidenceKind = 'kmc-normal-player-observer'; runId = [string]$request.runId; scenario = [string]$request.scenario
    status = [string]$result.status; branch = [string]$request.branch; commit = [string]$request.commit; productVersion = [string]$request.productVersion
    dllSha256 = [string]$request.dllSha256; dllMvid = [string]$request.dllMvid; kmcAutomationRequest = $false
    observer = $request.normalPlayer.observer; transactionToken = [string]$request.transactionToken; processId = [int]$result.processId
    startedAtUtc = [string]$result.startedAtUtc; completedAtUtc = [string]$result.completedAtUtc; fixture = $request.fixture
    observerResultSha256 = (Get-KmcSha256 $ObserverResultPath); observerRequestSha256 = (Get-KmcSha256 $ObserverRequestPath)
    evidenceManifestSha256 = $evidenceManifestSha256
    checkPassCount = [int]$result.checkPassCount; checkFailCount = [int]$result.checkFailCount; errors = $errors
    observations = $result.observations; checks = @($result.checks)
    subscenarioResults = @([ordered]@{ name = [string]$request.scenario; status = [string]$result.status
        assertionPassCount = [int]$result.checkPassCount; assertionFailCount = $failedAssertions; errors = $errors })
}
Write-KmcJsonAtomic $GameResultPath $game
Write-Host "NORMAL-PLAYER RESULT PASS status=$($result.status) checks=$($result.checkPassCount)/$($result.checkFailCount)"
