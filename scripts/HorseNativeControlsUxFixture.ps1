# The complete Horse native-controls UX fixture (schema 8), shared by the Test-Harness.ps1 validator case and the PR-5 bite contract
# (scripts/Test-MountedAlpha2BiteContract.ps1), which passes producer-generated Horse fields into this same envelope (PR-5 review
# R4-F2, round 7). Requires the functions of scripts/runtime/RuntimeHarness.Common.ps1 (Write-KmcJsonDurable, Write-KmcJsonAtomic,
# Get-KmcSha256, Read-KmcJson, Assert-KmcHorseNativeControlsUxEvidence). Synthetic envelope data; not native evidence.
Set-StrictMode -Version Latest

function New-KmcHorseNativeControlsUxFixture {
    param([Parameter(Mandatory = $true)][string]$EvidenceRoot, [Parameter(Mandatory = $true)][string]$ProductVersion)
    New-Item -ItemType Directory -Path $EvidenceRoot -Force | Out-Null
    $nativeRequest = [pscustomobject]@{
        runId='horse-native-controls-ux-validator';scenario='horse-native-controls-ux-suite'
        branch='codex/mounted-combat-phase3c-native-controls';commit=('c'*40)
        productVersion=$ProductVersion;dllSha256=('d'*64)
        dllMvid='33333333-4444-5555-6666-777777777777';evidenceRoot=$EvidenceRoot
    }
    $nativeRequired = @(
        'original-horse-portrait-and-icon','legacy-overlay-default-hidden','legacy-overlay-debug-fallback',
        'native-mount-ability-present-no-slot-overwrite',
        'native-control-disable-reenable','native-control-save-load-presence',
        'native-saddle-up-invalid-target',
        'native-saddle-up-exact-ability-fact','native-saddle-up-handler-holds-exact-ability',
        'native-saddle-up-priority-admits-horse','native-saddle-up-resolved-target-is-exact-horse',
        'native-saddle-up-click-accepted','native-saddle-up-native-command-created',
        'native-saddle-up-command-ability-is-exact','native-saddle-up-command-executor-is-exact-rider',
        'native-saddle-up-command-target-is-exact-horse','native-saddle-up-single-command-no-duplicate',
        'native-saddle-up-native-provenance','native-saddle-up-shell-registered-once',
        'native-saddle-up-shell-identity-exact','native-saddle-up-one-cast-request-no-refusal',
        'native-saddle-up-transition-not-yet-delivered','native-saddle-up-drop-ability-preserves-command',
        'native-mounted-control-surface','inventory-horse-preview-no-ik-exception',
        'mounted-turn-based-rider-movement','human-input-tb-rider-primary-rider-turn',
        'human-input-tb-target-click-admitted','human-input-tb-horse-primary-horse-turn',
        'horse-primary-animation-tb','human-input-rt-rider-primary','human-input-rt-horse-primary',
        'mounted-rider-primary-outcome','mounted-horse-primary-outcome','native-dismount-ability',
        'entity-and-target-restoration','mode-pause-selection-restoration','non-horse-isolation'
    )
    $nativeAssertions = @($nativeRequired | ForEach-Object {
        [ordered]@{name=$_;status='PASS';detail="Synthetic exact native UX contract for $_."}
    })
    $newControlSnapshot = {
        param([bool]$Suspended,[int]$FactCount)
        [ordered]@{
            registered=$true;enabled=$true;serializationSuspended=$Suspended;exactFactCount=$FactCount
            duplicateFactCount=0;managedHotbarSlotCount=0;targetSelectionStartCount=1
            targetSelectionEndCount=1;nativeCastRequestCount=1;nativeRefusalCount=0
            dispatchAcceptedCount=1;dispatchRejectedCount=0
        }
    }
    $newClick = {
        param([string]$Ability,[string]$Caster,[string]$Clicked,[string]$Resolved,[bool]$Accepted)
        [ordered]@{
            abilityGuid=$Ability;casterId=$Caster;clickedTargetId=$Clicked;resolvedTargetId=$Resolved
            priority='Ability';clicked=$Accepted;targetSelectionStartDelta=1;targetSelectionEndDelta=1
            nativeCastRequestDelta=$(if($Accepted){1}else{0});nativeRefusalDelta=$(if($Accepted){0}else{1})
            dispatchAcceptedDelta=$(if($Accepted){1}else{0});dispatchRejectedDelta=0
        }
    }
    $newOutcome = {
        param([string]$Action,[string]$Actor,[bool]$Natural,[bool]$Animation)
        [ordered]@{
            action=$Action;actorId=$Actor;commandOwnerId=$Actor;resourceOwnerId=$Actor
            targetId='target';result='Success';childAttackStartCount=1;repathCount=0
            attackWeaponBlueprintId=('7'*32);attackWeaponIsNatural=$Natural;attackWeaponIsRanged=$false
            attackWeaponSlot=$(if($Natural){'AdditionalLimb'}else{'EquippedMelee'})
            delegatedMoveExecutorId='horse';delegatedMoveExecutorIsExactMount=$true
            riderStandardCharged=(-not $Natural);actionStandardCharged=$true;terminalReason=$null
            attackAnimationHandleCreated=$Animation
            attackAnimationHandleSource=$(if($Animation){'stock-created'}else{$null})
            attackAnimationActionName=$(if($Animation){'SpecialAttack'}else{$null})
            attackAnimationActionType=$(if($Animation){'SpecialAttack'}else{$null})
            attackAnimationActed=$Animation;attackAnimationFinished=$Animation;attackAnimationInterrupted=$false
        }
    }
    $nativeObservations = [ordered]@{
        ownerId='owner';horseId='horse'
        legacyOverlay=[ordered]@{
            automationPresentBeforeExplicitPolicy=$true;automationObjectCountBeforeExplicitPolicy=1
            defaultHiddenPresent=$false;defaultHiddenObjectCount=0
            debugFallbackPresent=$true;debugFallbackObjectCount=1
            finalHiddenPresent=$false;finalHiddenObjectCount=0
        }
        mountedRiderDollRoomIk=[ordered]@{
            exactBindingCount=0;exactSetupStartCount=1;exactSetupCompleteCount=1
            lastUnitId='<none>';lastUnitRole='<none>'
        }
        mountedHorseDollRoomExpectedPath='simple-unit-view'
        mountedHorseDollRoomIk=[ordered]@{
            exactBindingCount=0;exactSetupStartCount=1;exactSetupCompleteCount=1
            lastUnitId='<none>';lastUnitRole='<none>'
        }
        mountedHorseDollRoomPreview=[ordered]@{
            mode='simple-unit-view';sourceCharacterAvatarPresent=$false
            simpleAvatarFieldToken='0x04002F58';simpleAvatarPresent=$true
            simpleAvatarActiveInHierarchy=$true;dollRoomVisible=$true
            dollRoomPublicAvatarPresent=$false;dollRoomPublicUnitPresent=$false
            setupStartDelta=0;setupCompleteDelta=0;bindingDelta=0;stableFrameCount=3
        }
        nativeControlsBeforeMount=(& $newControlSnapshot $false 1)
        nativeControlsDuringSaveScope=(& $newControlSnapshot $true 0)
        nativeControlsAfterSaveScope=(& $newControlSnapshot $false 1)
        nativeControlsMounted=(& $newControlSnapshot $false 5)
        nativeControlsAfterDismount=(& $newControlSnapshot $false 1)
        nativeMountInvalidTarget=(& $newClick ('1'*32) 'owner' 'owner' 'owner' $false)
        nativeMountValidHorse=(& $newClick ('1'*32) 'owner' 'horse' 'horse' $true)
        nativeTbRiderPrimaryClick=(& $newClick ('2'*32) 'owner' 'target' 'target' $true)
        nativeTbHorsePrimaryClick=(& $newClick ('3'*32) 'horse' 'target' 'target' $true)
        nativeRtRiderPrimaryClick=(& $newClick ('2'*32) 'owner' 'target' 'target' $true)
        nativeRtHorsePrimaryClick=(& $newClick ('3'*32) 'owner' 'target' 'target' $true)
        nativeDismountClick=(& $newClick ('4'*32) 'owner' 'owner' 'owner' $true)
        mountedTurnRiderOutcome=(& $newOutcome 'RiderMelee' 'owner' $false $false)
        mountedTurnHorseOutcome=(& $newOutcome 'MountPrimaryNatural' 'horse' $true $true)
        mountedTurnHorseAnimation=[ordered]@{
            delegatedLocomotionRestoreCount=1;lastDelegatedLocomotionSource='attack-approach';lastDelegatedLocomotionSpeed=1.0
            horsePrimaryHandleCreateCount=0;horsePrimaryHandleAdoptCount=1;horsePrimaryHandleRejectCount=0
            lastHorsePrimaryHandleSource='stock-created';lastHorsePrimaryActionName='SpecialAttack';lastHorsePrimaryActionType='SpecialAttack'
        }
        mountedRiderOutcome=(& $newOutcome 'RiderMelee' 'owner' $false $false)
        mountedHorseOutcome=(& $newOutcome 'MountPrimaryNatural' 'horse' $true $true)
        mountedHorseAnimation=[ordered]@{
            delegatedLocomotionRestoreCount=1;lastDelegatedLocomotionSource='attack-approach';lastDelegatedLocomotionSpeed=1.0
            horsePrimaryHandleCreateCount=0;horsePrimaryHandleAdoptCount=2;horsePrimaryHandleRejectCount=0
            lastHorsePrimaryHandleSource='stock-created';lastHorsePrimaryActionName='SpecialAttack';lastHorsePrimaryActionType='SpecialAttack'
        }
        unmountedHorseBlueprintSpeedFeet=50;mountedHorseBlueprintSpeedFeet=50
        unmountedHorseAgentMaxSpeed=4.0;mountedHorseAgentMaxSpeed=4.0
        unmountedHorseAverageWorldSpeed=3.5;mountedRealTimeAverageWorldSpeed=3.4
        horseProfileId='medium-humanoid-horse-v1';horsePoseProfileId='medium-humanoid-horse-v1'
        horseSourceAnchor='Chest';relationshipState='Unmounted';horseRemoved=$true;targetRemoved=$true
        unrelatedPartyPetsPreserved=$true
        horsePoseCalibration=[ordered]@{
            candidateCount=3;candidateId='phase3d-horse-root-minus-0.08'
            selectedPelvisPositionOffset=[ordered]@{x=0.0;y=-0.17;z=-0.02}
            selectedMountRootPositionOffset=[ordered]@{x=0.0;y=-0.08;z=0.0}
            selectedLeftFootTargetFromThigh=[ordered]@{x=-0.15;y=-0.62;z=0.11}
            selectedRightFootTargetFromThigh=[ordered]@{x=0.15;y=-0.62;z=0.11}
            leftFootToAssignedStirrup=0.2;rightFootToAssignedStirrup=0.2
        }
    }
    $nativeArtifact = [ordered]@{
        schemaVersion=8;evidenceKind='horse-native-controls-ux';runId=$nativeRequest.runId
        scenario=$nativeRequest.scenario;branch=$nativeRequest.branch;commit=$nativeRequest.commit
        productVersion=$nativeRequest.productVersion;dllSha256=$nativeRequest.dllSha256
        dllMvid=$nativeRequest.dllMvid;createdAtUtc=[DateTimeOffset]::UtcNow.ToString('o')
        status='PASS';assertions=$nativeAssertions;observations=$nativeObservations
        assertionPassCount=$nativeAssertions.Count;assertionFailCount=0;errors=@()
    }
    $nativePath = Join-Path $EvidenceRoot 'horse-native-controls-ux.json'
    Write-KmcJsonDurable -Path $nativePath -Value $nativeArtifact
    $nativeRecord = [ordered]@{
        relativePath='horse-native-controls-ux.json';kind='horse-native-controls-ux'
        length=(Get-Item -LiteralPath $nativePath).Length;sha256=(Get-KmcSha256 $nativePath)
    }
    $nativeSubresult = [pscustomobject]@{
        name='horse-native-controls-ux-suite';status='PASS'
        assertionPassCount=$nativeAssertions.Count;assertionFailCount=0;errors=@()
    }
    $fixture = [pscustomobject]@{ Root = $EvidenceRoot; Request = $nativeRequest; Artifact = $nativeArtifact; Path = $nativePath; Record = $nativeRecord; Subresult = $nativeSubresult; Manifest = $null }
    Write-KmcHorseNativeControlsUxManifest $fixture
    return $fixture
}

# The run manifest naming the fixture artifact (the Test-Harness.ps1 New-TestArtifactManifest shape).
function Write-KmcHorseNativeControlsUxManifest($Fixture) {
    $path = Join-Path $Fixture.Root 'runtime-artifacts.json'
    Write-KmcJsonDurable -Path $path -Value ([ordered]@{
        schemaVersion = 1; runId = $Fixture.Request.runId; scenario = $Fixture.Request.scenario
        createdAtUtc = [DateTimeOffset]::UtcNow.ToString('o'); artifacts = @($Fixture.Record)
    })
    $Fixture.Manifest = Read-KmcJson $path
}

# Rewrites the (changed) artifact and its manifest record.
function Update-KmcHorseNativeControlsUxFixture($Fixture) {
    Write-KmcJsonAtomic -Path $Fixture.Path -Value $Fixture.Artifact
    $Fixture.Record.length = (Get-Item -LiteralPath $Fixture.Path).Length
    $Fixture.Record.sha256 = (Get-KmcSha256 $Fixture.Path)
    Write-KmcHorseNativeControlsUxManifest $Fixture
}

# The complete production reader on the fixture: $true when Assert-KmcHorseNativeControlsUxEvidence accepts it.
function Test-KmcHorseNativeControlsUxFixture($Fixture) {
    try { Assert-KmcHorseNativeControlsUxEvidence -Request $Fixture.Request -Manifest $Fixture.Manifest -Status PASS -SubscenarioResults @($Fixture.Subresult); return $true }
    catch { return $false }
}
