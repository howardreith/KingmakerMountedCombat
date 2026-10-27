[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7ProgressionEvidence.ps1')
$lab=Get-KmcLabRoot
$path=Join-Path $lab 'runtime-evidence/20261021-c7b-world111-b99s3-small-native-retrain/chunk7-progression-evidence.json'
if((Get-KmcSha256 $path)-cne'bc5ab7d3ede7d591dc56f96fc704fd1cb01ce9983f1eb65ed8d80cff1730a7ea'){
    throw 'Historical batch99 producer changed; read-only snapshot regression cannot transfer a verdict.'
}
$producer=Read-KmcJson $path;$observed=$producer.observations;$saved=$observed.session.nativeWrite.archive
$terminal=@($observed.session.events|Where-Object{$null-ne$_.PSObject.Properties['kind']-and$_.kind-ceq'native-progression-builder-complete'})
if($terminal.Count-ne1-or$saved.Pairs.Count-ne1){throw 'Historical retrain producer shape changed.'}
$body=$observed.nativeBodyBefore
$selection=[pscustomobject]@{case='small-horse-native-retrain-cold';actorId=$observed.selection.actorId;petId=$observed.ownedRosterAfter.currentPetId;
    controlActorId=$observed.selection.controlActorId;controlPetId=$observed.selection.controlPetId}
$owner=[pscustomobject]@{ownerId=$saved.Pairs[0].PairId;generation=$saved.Pairs[0].Generation}
$expected=[pscustomobject]@{targetLevel=$terminal[0].level;targetXp=$terminal[0].xp;newPetLevel=$terminal[0].petLevel;
    nativeMoney=$terminal[0].money;targetBlueprint=$observed.source.inventoryActor.blueprint;newPetBlueprint=$body.MountBlueprintGuid;
    nativeBody=[pscustomobject]@{raceGuid=$body.RaceGuid;presetGuid=$body.PresetGuid;skeletonName=$body.SkeletonName;
        gender=$body.Gender;leftHanded=$body.LeftHanded;originalSizeOrdinal=$body.OriginalSizeOrdinal;
        effectiveSizeOrdinal=$body.EffectiveSizeOrdinal}}
function New-State {
    [pscustomobject]@{targetId=$selection.actorId;newPetId=$selection.petId;targetLevel=$expected.targetLevel;
        targetXp=$expected.targetXp;targetBlueprint=$expected.targetBlueprint;newPetLevel=$expected.newPetLevel;
        newPetBlueprint=$expected.newPetBlueprint;nativeBody=$expected.nativeBody;oldPetAbsent=$true;targetUnpaired=$true;
        controlRiderId=$selection.controlActorId;controlMountId=$selection.controlPetId;controlOwnerId=$owner.ownerId;
        controlGeneration=$owner.generation;nativeMoney=$expected.nativeMoney;
        slots=@($saved.Slots|ForEach-Object{[pscustomobject]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}});
        actors=@(@($selection.actorId,$selection.petId,$selection.controlActorId,$selection.controlPetId)|Sort-Object|ForEach-Object{
            [pscustomobject]@{id=$_;inCombat=$false;prepared=$false}});playerInCombat=$false;
        snapshots=0;completedSaves=0;nativeCasts=0}
}
function Clone($value){$value|ConvertTo-Json -Depth 20|ConvertFrom-Json}
function Refuses([string]$name,$value){$refused=$false;try{Assert-KmcChunk7RetrainColdState $value $selection $saved $owner $expected}catch{$refused=$true};if(-not$refused){throw ('Cold snapshot accepted '+$name)};Write-Host ('PASS refuses '+$name)}
Assert-KmcChunk7RetrainColdState (New-State) $selection $saved $owner $expected
Write-Host 'PASS producer-bound retrain cold snapshot'
$altered=New-State;$altered.nativeMoney++;Refuses 'changed native money' $altered
$altered=New-State;$altered.targetXp++;Refuses 'changed rider XP' $altered
$altered=New-State;$altered.newPetLevel++;Refuses 'changed new-pet level' $altered
$altered=New-State;$altered.newPetBlueprint=@($altered.newPetBlueprint);Refuses 'array new-pet blueprint' $altered
$altered=New-State;$altered.slots[0].index=[string]$altered.slots[0].index;Refuses 'text native slot index' $altered
$altered=New-State;$altered.slots[0].kind=[string]$altered.slots[0].kind;Refuses 'text native slot kind' $altered
$altered=New-State;$altered.slots[1]=Clone $altered.slots[0];Refuses 'duplicate surviving owner slot' $altered
$altered=New-State;$altered.nativeBody=(Clone $altered.nativeBody);$altered.nativeBody.raceGuid='0'*32;Refuses 'changed native body' $altered
$altered=New-State;$altered.actors[0].inCombat=$true;Refuses 'actor entered combat' $altered
$altered=New-State;$altered.actors[0].prepared='false';Refuses 'text Prepared flag' $altered
$altered=New-State;$altered.actors[0].prepared=$true;Refuses 'unexpected native preparation' $altered
$interval=[pscustomobject]@{coldInitialFrame=100;coldContinuationFrame=105;coldInitial=(New-State);coldContinuation=(New-State)}
Assert-KmcChunk7RetrainColdInterval $interval
Write-Host 'PASS five-frame unchanged continuation'
$interval.coldContinuationFrame=104;$refused=$false;try{Assert-KmcChunk7RetrainColdInterval $interval}catch{$refused=$true};if(-not$refused){throw 'Immediate cold snapshot was accepted.'}
Write-Host 'PASS refuses early continuation'
$events=@(foreach($id in @($selection.controlActorId,$selection.controlPetId)){foreach($kind in @('restore-debt-before','restore-debt-after')){[pscustomobject]@{kind=$kind;actorId=$id}}})
Assert-KmcChunk7RetrainColdObserver $events $selection
Write-Host 'PASS exactly-once native control debt restoration'
foreach($entry in @([pscustomobject]@{kind='restore-debt-before';actorId=$selection.actorId},
                 [pscustomobject]@{kind='prepare-before';actorId=$selection.petId})){
    $wrong=@($events)+@($entry);$refused=$false
    try{Assert-KmcChunk7RetrainColdObserver $wrong $selection}catch{$refused=$true}
    if(-not$refused){throw ('Extra native observer event accepted: '+$entry.kind)}
    Write-Host ('PASS refuses extra '+$entry.kind)
}
Write-Host 'TOTAL PASS 17 FAIL 0; strict reader snapshot/observer regression, no native cold proof'
