[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7ProgressionEvidence.ps1')
$path=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261021-c7b-world121-b117s4-medium-mammoth-cold/chunk7-progression-evidence.json'
if((Get-KmcSha256 $path)-cne'69ded47ea48604bd099cc4d26639b1a4e89cce987d2faa5253ba33b2c039e09f'){
    throw 'Immutable batch117 Mammoth cold source changed.'
}
$source=Read-KmcJson $path;$s=$source.observations.selection
$s.case='medium-horse-native-retrain-field'
$owner=$source.observations.beforeOwners.owners[0]
function New-Observation {
    [pscustomobject]@{beforeOwners=[pscustomobject]@{owners=@($owner)};
        fieldInputs=@(
            [pscustomobject]@{label='mount-mammoth';frame=100;casterId=$s.actorId;targetId=$s.petId;
                abilityGuid='f053faad986631688defa003cd7bda0e';clicked=$true;nativeCastDelta=1;
                nativeRefusalDelta=0;shellControl=('shell:1:mount:'+$s.actorId);generationAtInit=1;shellTerminal=$true;
                processTerminal=$true;settledFrame=101},
            [pscustomobject]@{label='mammoth-ground';frame=102;selectedId=$s.actorId;executorId=$s.petId;
                nativeCallEntered=$true;nativeCallReturned=$true;commandValidated=$true},
            [pscustomobject]@{label='dismount-mammoth';frame=110;casterId=$s.actorId;targetId=$s.actorId;
                abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';clicked=$true;nativeCastDelta=1;
                nativeRefusalDelta=0;shellControl=('shell:2:dismount:'+$s.actorId);generationAtInit=2;shellTerminal=$true;
                processTerminal=$true;settledFrame=111});
        fieldMounted=[pscustomobject]@{frame=101;targetOwnerId='33333333333333333333333333333333';targetGeneration=3;
            targetProfileId='medium-humanoid-mammoth-v1';releasedOwnerId='33333333333333333333333333333333';
            releasedOwnerState='Active';releasedBoundaryState='Mounted';controlOwnerId=$owner.ownerId;
            controlGeneration=$owner.generation;owners=2;nativeCasts=1;saveSnapshots=0;completedSaves=0};
        fieldMovement=[pscustomobject]@{commandFinished=$true;createdByPlayer=$true;result='Success';executorId=$s.petId;
            ownerId='33333333333333333333333333333333';ownerGeneration=3;
            distance=1.5;startFrame=102;endFrame=109};
        fieldDismountBoundary=[pscustomobject]@{frame=111;shellResult='Success';shellFinished=$true;processEnded=$true;
            targetOwnerId=$null;targetGeneration=$null;targetState=$null;
            releasedOwnerId='33333333333333333333333333333333';releasedOwnerState='Detached';releasedBoundaryState='Unmounted';
            targetCustodyOwnerId='33333333333333333333333333333333';targetCustodyState='Detached';
            mountCustodyOwnerId='33333333333333333333333333333333';mountCustodyState='Detached';
            controlOwnerId=$owner.ownerId;controlGeneration=$owner.generation;owners=2;activeOwners=1;
            riderPetId=$s.petId;riderPetIsMammoth=$true;mountMasterId=$s.actorId;mountMasterIsRider=$true;
            nativeCasts=2;nativeRefusals=0;dispatchAccepted=2;dispatchRejected=0;
            shellLifecycle=('DeliverConsumed;control=shell:2:dismount:'+$s.actorId+';predicate=consumed-success;kind=Dismount')};
        fieldDismounted=[pscustomobject]@{frame=112;targetOwnerId=$null;targetGeneration=$null;targetProfileId=$null;
            releasedOwnerId='33333333333333333333333333333333';releasedOwnerState='Retired';releasedBoundaryState='Unmounted';
            controlOwnerId=$owner.ownerId;controlGeneration=$owner.generation;owners=1;
            nativeCasts=2;saveSnapshots=0;completedSaves=0}}
}
Assert-KmcChunk7MammothFieldOutcome $s (New-Observation)
Write-Host 'PASS source-bound native Mammoth field outcome shape'
foreach($name in @('wrong profile','wrong moving actor','no travel','failed move','refunded cast','wrong ability','foreign shell','neighbor replaced','changed target owner','retained Mammoth','extra save','reordered input','late movement','late mount observation','refused dismount dispatch','foreign detached custody','split mount custody','faulted boundary','unretired Mammoth','owner not retired')){
    $o=New-Observation
    switch($name){
        'wrong profile'{$o.fieldMounted.targetProfileId='medium-humanoid-horse-v1'}
        'wrong moving actor'{$o.fieldMovement.executorId=$s.controlPetId}
        'no travel'{$o.fieldMovement.distance=0.1}
        'failed move'{$o.fieldMovement.result='Interrupt'}
        'refunded cast'{$o.fieldInputs[2].nativeCastDelta=0}
        'wrong ability'{$o.fieldInputs[0].abilityGuid='0'*32}
        'foreign shell'{$o.fieldInputs[0].shellControl='shell:2:mount:foreign'}
        'neighbor replaced'{$o.fieldDismounted.controlOwnerId='0'*32}
        'changed target owner'{$o.fieldMovement.ownerId='0'*32}
        'retained Mammoth'{$o.fieldDismounted.targetOwnerId=$o.fieldMounted.targetOwnerId}
        'extra save'{$o.fieldMounted.completedSaves=1}
        'reordered input'{$o.fieldInputs[1].frame=99}
        'late movement'{$o.fieldMovement.endFrame=110}
        'late mount observation'{$o.fieldMounted.frame=103}
        'refused dismount dispatch'{$o.fieldDismountBoundary.dispatchRejected=1}
        'foreign detached custody'{$o.fieldDismountBoundary.targetCustodyOwnerId='0'*32}
        'split mount custody'{$o.fieldDismountBoundary.mountCustodyOwnerId='0'*32}
        'faulted boundary'{$o.fieldDismountBoundary.releasedBoundaryState='Faulted'}
        'unretired Mammoth'{$o.fieldDismounted.owners=2}
        'owner not retired'{$o.fieldDismounted.releasedOwnerState='Detached'}
    }
    $refused=$false;try{Assert-KmcChunk7MammothFieldOutcome $s $o}catch{$refused=$true}
    if(-not$refused){throw ('Mammoth field reader accepted '+$name)}
    Write-Host ('PASS refuses '+$name)
}
Write-Host 'TOTAL PASS 21 FAIL 0; synthetic field outcome, no native qualification'
