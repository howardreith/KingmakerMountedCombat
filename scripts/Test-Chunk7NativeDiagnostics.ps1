[CmdletBinding()]
param()
# Bounded offline checks for the Chunk 7 diagnostic source, custody, protocol and readers.
# Source-family tests import their own lower-level fixtures in an isolated child process;
# those imported families are deliberately omitted as separate top-level invocations.
# This wrapper never selects native launches, packages, historical replay or qualification.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
# Children run non-interactively so a prompt fails fast instead of blocking a hidden console.
# Source-family checks that declare a mandatory OutputRoot receive a fresh path under the lab analysis cache.
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot = [IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($repositoryRoot))
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
function Test-KmcMandatoryOutputRoot([string]$Path) {
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$errors)
    if ($errors) { throw ('Focused check does not parse: ' + $Path) }
    if ($null -eq $ast.ParamBlock) { return $false }
    foreach ($parameter in $ast.ParamBlock.Parameters) {
        if ($parameter.Name.VariablePath.UserPath -cne 'OutputRoot') { continue }
        foreach ($attribute in $parameter.Attributes) {
            if ($attribute -isnot [Management.Automation.Language.AttributeAst] -or $attribute.TypeName.Name -cne 'Parameter') { continue }
            foreach ($named in $attribute.NamedArguments) {
                if ($named.ArgumentName -cne 'Mandatory') { continue }
                if ($named.ExpressionOmitted -or $named.Argument.Extent.Text -ceq '$true') { return $true }
            }
        }
    }
    return $false
}
$selected = @(
    'Test-Chunk7AssistedFixtureCore.ps1',
    'Test-Chunk7AssistedColdAuthority.ps1',
    'Test-Chunk7AssistedColdProtocol.ps1',
    'Test-Chunk7AssistedColdDto.ps1',
    'Test-Chunk7AssistedColdInventory.ps1',
    'Test-Chunk7AssistedColdWiring.ps1',
    'Test-Chunk7AssistedColdInventorySource.ps1',
    'Test-Chunk7AssistedColdInventoryReadiness.ps1',
    'Test-Chunk7AssistedFixtureAuthority.ps1',
    'Test-Chunk7AssistedFixtureReader.ps1',
    'Test-Chunk7AssistedFixtureSource.ps1',
    'Test-Chunk7AssistedFixtureArchive.ps1',
    'Test-Chunk7OrdinaryChoiceEvidence.ps1',
    'Test-Chunk7AssistedFixtureProbes.ps1',
    'Test-Chunk7AssistedFixtureDto.ps1',
    'Test-Chunk7AssistedFixtureWiring.ps1',
    'Test-Chunk7AssistedStoryKinds.ps1',
    'Test-Chunk7AreaPersistenceSource.ps1',
    'Test-Chunk7CanonicalEvidence.ps1',
    'Test-Chunk7DiagnosticsOff.ps1',
    'Test-Chunk7ForensicDiagnosticsOff.ps1',
    'Test-Chunk7ObservationLifetime.ps1',
    'Test-Chunk7ArtifactProducer.ps1',
    'Test-Chunk7CheckpointCleanupAdapter.ps1',
    'Test-Chunk7CleanupEvidence.ps1',
    'Test-Chunk7CombatCleanupAdapter.ps1',
    'Test-Chunk7RefusedPairProposals.ps1',
    'Test-Chunk7CombatPersistenceSource.ps1',
    'Test-Chunk7HostCompletion.ps1',
    'Test-Chunk7HostOwnedEngines.ps1',
    'Test-Chunk7HostTerminalBoundary.ps1',
    'Test-RuntimeStartupTrace.ps1',
    'Test-Chunk7NativeLifeEvidence.ps1',
    'Test-Chunk7ObserverAcquisition.ps1',
    'Test-Chunk7OverlayCustody.ps1',
    'Test-Chunk7ObserverDisposal.ps1',
    'Test-Chunk7RangerSelectionContract.ps1',
    'Test-Chunk7MatchedPool.ps1',
    'Test-Chunk7NativeMoveCallback.ps1',
    'Test-Chunk7WorldGroundInput.ps1',
    'Test-Chunk7NativeBootstrapProtocol.ps1',
    'Test-Chunk7NativeBootstrapAdapter.ps1',
    'Test-Chunk7NativeBootstrapProbe.ps1',
    'Test-Chunk7BootstrapDialogueObservation.ps1',
    'Test-Chunk7PassiveDialogueChoices.ps1',
    'Test-Chunk7BootstrapScheduledReadiness.ps1',
    'Test-Chunk7NativeBootstrapEvidence.ps1',
    'Test-Chunk7NativeBootstrapWiring.ps1',
    'Test-Chunk7BootstrapInventoryProtocol.ps1',
    'Test-Chunk7BootstrapInventoryAdapter.ps1',
    'Test-Chunk7BootstrapFixtureReadinessAdapter.ps1',
    'Test-Chunk7BootstrapInventoryEvidence.ps1',
    'Test-Chunk7BootstrapObjectIdentity.ps1',
    'Test-Chunk7BootstrapInventorySource.ps1',
    'Test-Chunk7BootstrapInventoryWiring.ps1',
    'Test-Chunk7AcquiredManualReservation.ps1',
    'Test-Chunk7AcquiredManualAdapter.ps1',
    'Test-Chunk7AcquiredManualSaveProbe.ps1',
    'Test-Chunk7AcquiredManualEvidence.ps1',
    'Test-Chunk7AcquiredManualIdentity.ps1',
    'Test-Chunk7AcquiredManualSource.ps1',
    'Test-Chunk7AcquiredManualWiring.ps1',
    'Test-Chunk7AcquiredManualDto.ps1',
    'Test-Chunk7CumulativeProtocol.ps1',
    'Test-Chunk7CumulativeEvidence.ps1',
    'Test-Chunk7CumulativeCustody.ps1',
    'Test-Chunk7CumulativeWiring.ps1',
    'Test-Chunk7CumulativeDto.ps1',
    'Test-Chunk7CumulativeSource.ps1',
    'Test-Chunk7CumulativeCopy.ps1',
    'Test-Chunk7AssistedAcquisitionConnection.ps1',
    'Test-Chunk7AssistedAcquisitionEvidence.ps1',
    'Test-Chunk7AssistedAcquisitionSource.ps1',
    'Test-Chunk7AssistedAcquisitionWiring.ps1',
    'Test-Chunk7TwoOwnerSource.ps1',
    'Test-Chunk7TwoOwnerCustody.ps1',
    'Test-Chunk7TwoOwnerManualEvidence.ps1',
    'Test-Chunk7OwnedPairsAuthority.ps1',
    'Test-Chunk7OwnedPairsCustody.ps1',
    'Test-Chunk7OwnedPairsEvidence.ps1',
    'Test-Chunk7OwnedPairsSource.ps1',
    'Test-Chunk7OwnedPairsWiring.ps1','Test-Chunk7OwnedPairsManifest.ps1',
    'Test-Chunk7OwnedPairsDto.ps1',
    'Test-Chunk7NativeFieldRoster.ps1',
    'Test-Chunk7MediumAcquisition.ps1',
    'Test-Chunk7MediumContinuation.ps1',
    'Test-Chunk7PaidBuildContract.ps1',
    'Test-Chunk7ManualDerivedAdmission.ps1',
    'Test-Chunk7ManualDerivedProtocol.ps1',
    'Test-Chunk7ManualDerivedPresentation.ps1',
    'Test-Chunk7ManualDerivedEvidence.ps1',
    'Test-Chunk7ManualDerivedDto.ps1',
    'Test-Chunk7ManualDerivedSource.ps1',
    'Test-Chunk7ManualDerivedWiring.ps1',
    'Test-Chunk7ManualSeedProtocol.ps1',
    'Test-Chunk7ManualSeedAdapter.ps1',
    'Test-Chunk7ManualSeedSaveProbe.ps1',
    'Test-Chunk7ManualSeedEvidence.ps1',
    'Test-Chunk7ManualSeedSource.ps1',
    'Test-Chunk7ManualSeedWiring.ps1',
    'Test-Chunk7OwnedProgressionProtocol.ps1',
    'Test-Chunk7OwnedProgressionRoster.ps1',
    'Test-Chunk7OwnedProgressionEvidence.ps1',
    'Test-Chunk7OwnedProgressionSource.ps1',
    'Test-Chunk7OwnedProgressionWiring.ps1',
    'Test-Chunk7OwnedProgressionDto.ps1',
    'Test-Chunk7ProgressionProtocol.ps1',
    'Test-Chunk7ProgressionEvidence.ps1',
    'Test-Chunk7ProgressionDto.ps1',
    'Test-Chunk7ProgressionWiring.ps1',
    'Test-Chunk7StaleLevelUpFamilies.ps1',
    'Test-Chunk7RetiredPersistenceCases.ps1',
    'Test-Chunk7RetiredSmallCalibrationCases.ps1',
    'Test-Chunk7UpgradeRehearsal.ps1',
    'Test-Chunk7ProgressionHeader.ps1',
    'Test-Chunk7ProgressionPreStartScope.ps1',
    'Test-Chunk7ProgressionPetBinding.ps1',
    'Test-Chunk7ProgressionObservers.ps1',
    'Test-Chunk7ProgressionCustody.ps1',
    'Test-Chunk7ProgressionSession.ps1',
    'Test-Chunk7ProgressionArchive.ps1',
    'Test-Chunk7ProgressionFixtures.ps1',
    'Test-Chunk7SmallProtocol.ps1',
    'Test-Chunk7SmallPrimary.ps1',
    'Test-Chunk7SmallCleanup.ps1',
    'Test-Chunk7SmallEvidence.ps1',
    'Test-Chunk7SmallTrace.ps1',
    'Test-Chunk7SmallSerialization.ps1',
    'Test-Chunk7SmallFixtures.ps1',
    'Test-Chunk7SmallWiring.ps1',
    'Test-Chunk7SmallManualProtocol.ps1',
    'Test-Chunk7SmallManualCopy.ps1',
    'Test-Chunk7SmallManualWiring.ps1',
    'Test-Chunk7SmallManualCleanup.ps1',
    'Test-Chunk7SmallManualReadiness.ps1',
    'Test-Chunk7SmallManualRoster.ps1',
    'Test-Chunk7FieldSelector.ps1',
    'Test-Chunk7FieldAuthority.ps1',
    'Test-Chunk7EvidenceDto.ps1',
    'Test-Chunk7FieldEvidence.ps1',
    'Test-Chunk7FieldFixtures.ps1',
    'Test-Chunk7FieldWiring.ps1',
    'Test-Chunk7FieldCleanup.ps1',
    'Test-Chunk7OrdinaryAuthority.ps1',
    'Test-Chunk7OrdinaryEvidence.ps1',
    'Test-Chunk7OrdinarySaveDescriptor.ps1',
    'Test-Chunk7OrdinarySaveDescriptorEvidence.ps1',
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
    'Test-Chunk7SaveBoundaryAdapter.ps1','Test-Chunk7SaveBoundaryAuthority.ps1','Test-Chunk7SaveBoundaryHeaders.ps1','Test-Chunk7SaveBoundaryWiring.ps1',
    'Test-Chunk7SaveBoundaryProtocol.ps1',
    'Test-Chunk7SaveBoundaryEvidence.ps1',
    'Test-Chunk7Schema1Persistence.ps1',
    'Test-Chunk7OwnedSchema1Evidence.ps1',
    'Test-Chunk7OwnedSchema1Native.ps1',
    'Test-Chunk7OwnedSchema1Input.ps1',
    'Test-Chunk7Schema2Resave.ps1',
    'Test-Chunk7ArchivePostLoadIdentity.ps1',
    'Test-WorkingLoadObservation.ps1',
    'Test-Chunk7PersistenceWiring.ps1',
    'Test-Chunk7RealtimeEvidence.ps1',
    'Test-Chunk7StagedGeometry.ps1',
    'Test-Chunk7RepeatedLoadAuthority.ps1',
    'Test-Chunk7RepeatedWorldAdapter.ps1',
    'Test-Chunk7RepeatedWorldProtocol.ps1',
    'Test-Chunk7RepeatedWorldEvidence.ps1',
    'Test-Chunk7CapitalArrivalEvidence.ps1',
    'Test-Chunk7OwnedReplayEpochs.ps1',
    'Test-Chunk7OwnedReplayEvidence.ps1',
    'Test-Chunk7OwnedAreaAutosaveEvidence.ps1',
    'Test-Chunk7OwnedAreaAutosaveBoundary.ps1',
    'Test-Chunk7QuietWindowContracts.ps1',
    'Test-Chunk7WorldCleanupAdapter.ps1',
    'Test-Chunk7DoorCleanupAdapter.ps1',
    'Test-Chunk7WorldResourcesAdapter.ps1',
    'Test-Chunk7WorldResourceEvidence.ps1'
)
$started = [Diagnostics.Stopwatch]::StartNew()
$completed = 0
foreach ($name in $selected) {
    $path = Join-Path (Join-Path $PSScriptRoot 'chunk7') $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw ('Missing focused check: ' + $name) }
    Write-Host ('RUN ' + $name)
    $arguments = @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', $path)
    if (Test-KmcMandatoryOutputRoot $path) {
        $arguments += @('-OutputRoot', (Join-Path $labRoot ('analysis-cache/chunk7-continuation/umbrella-' + $stamp + '/' + [IO.Path]::GetFileNameWithoutExtension($name))))
    }
    & powershell.exe @arguments
    if ($LASTEXITCODE -ne 0) { throw ($name + ' exited ' + $LASTEXITCODE) }
    $completed++
}
$started.Stop()
if ($completed -eq 0) { throw 'Empty Chunk 7 diagnostic selection.' }
Write-Host ('CHUNK7 DIAGNOSTIC SCRIPTS PASS=' + $completed + ' FAIL=0 (' + [Math]::Round($started.Elapsed.TotalSeconds, 3) + ' s); offline only; imported reader counts are not unique native cases.')
