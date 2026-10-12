[CmdletBinding()]
param()
# Bounded offline checks for the Chunk 7 diagnostic source, custody, protocol and readers.
# Source-family tests import their own lower-level fixtures in an isolated child process;
# those imported families are deliberately omitted as separate top-level invocations.
# This wrapper never selects native launches, packages, historical replay or qualification.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$selected = @(
    'Test-Chunk7AreaPersistenceSource.ps1',
    'Test-Chunk7CanonicalEvidence.ps1',
    'Test-Chunk7CheckpointCleanupAdapter.ps1',
    'Test-Chunk7CleanupEvidence.ps1',
    'Test-Chunk7CombatCleanupAdapter.ps1',
    'Test-Chunk7CombatPersistenceSource.ps1',
    'Test-Chunk7HostCompletion.ps1',
    'Test-Chunk7HostOwnedEngines.ps1',
    'Test-Chunk7NativeLifeEvidence.ps1',
    'Test-Chunk7ObserverAcquisition.ps1',
    'Test-Chunk7ObserverDisposal.ps1',
    'Test-Chunk7OrdinaryAuthority.ps1',
    'Test-Chunk7OrdinaryEvidence.ps1',
    'Test-Chunk7OrdinaryFixtures.ps1',
    'Test-Chunk7OrdinaryArchive.ps1',
    'Test-Chunk7OrdinaryWiring.ps1',
    'Test-Chunk7OrdinaryCleanupWorld.ps1',
    'Test-Chunk7OrdinaryUiLifecycle.ps1',
    'Test-Chunk7PersistenceCleanupAdapter.ps1',
    'Test-Chunk7PersistenceProtocol.ps1',
    'Test-Chunk7PersistenceSource.ps1',
    'Test-Chunk7PersistenceTypes.ps1',
    'Test-Chunk7SourceReceiptTypes.ps1',
    'Test-Chunk7Schema1Persistence.ps1',
    'Test-Chunk7PersistenceWiring.ps1',
    'Test-Chunk7RealtimeEvidence.ps1',
    'Test-Chunk7WorldCleanupAdapter.ps1',
    'Test-Chunk7WorldResourcesAdapter.ps1'
)
$started = [Diagnostics.Stopwatch]::StartNew()
$completed = 0
foreach ($name in $selected) {
    $path = Join-Path (Join-Path $PSScriptRoot 'chunk7') $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw ('Missing focused check: ' + $name) }
    Write-Host ('RUN ' + $name)
    & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $path
    if ($LASTEXITCODE -ne 0) { throw ($name + ' exited ' + $LASTEXITCODE) }
    $completed++
}
$started.Stop()
if ($completed -eq 0) { throw 'Empty Chunk 7 diagnostic selection.' }
Write-Host ('CHUNK7 DIAGNOSTIC SCRIPTS PASS=' + $completed + ' FAIL=0 (' + [Math]::Round($started.Elapsed.TotalSeconds, 3) + ' s); offline only; imported reader counts are not unique native cases.')
