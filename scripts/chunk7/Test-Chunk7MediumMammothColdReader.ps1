[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $RepositoryRoot 'scripts/runtime/Chunk7ProgressionEvidence.ps1')
$path=Join-Path (Get-KmcLabRoot) 'runtime-evidence/20261021-c7b-world120-b116s3-medium-mammoth-retrain/chunk7-progression-evidence.json'
if((Get-KmcSha256 $path)-cne'68e4b456d9601f97c79f34c8a523a90d716fdcfe5eebd524032af1a308c64336'){
    throw 'Immutable batch116 Mammoth producer changed.'
}
$observed=(Read-KmcJson $path).observations
$saved=$observed.session.nativeWrite.archive
$terminal=@($observed.session.events|Where-Object{$_.PSObject.Properties['kind']-and$_.kind-ceq'native-progression-builder-complete'})
$rank=@($observed.session.events|Where-Object{$_.PSObject.Properties['event']-and$_.event-ceq'native-retrain-mammoth-rank-seven'})
if($terminal.Count-ne1-or$rank.Count-ne1-or$saved.Pairs.Count-ne1){throw 'Historical Mammoth producer shape changed.'}
$body=$observed.nativeBodyBefore
$selection=[pscustomobject]@{case='medium-horse-native-retrain-cold';actorId=$observed.selection.actorId;
    petId=$observed.ownedRosterAfter.currentPetId;formerPetId=$observed.selection.petId;
    controlActorId=$observed.selection.controlActorId;controlPetId=$observed.selection.controlPetId}
$owner=[pscustomobject]@{ownerId=$saved.Pairs[0].PairId;generation=$saved.Pairs[0].Generation}
$expected=[pscustomobject]@{targetLevel=$terminal[0].level;targetXp=$terminal[0].xp;newPetLevel=$terminal[0].petLevel;
    nativeMoney=$terminal[0].money;targetBlueprint=$observed.source.inventoryActor.blueprint;
    newPetBlueprint=$rank[0].petGuid;
    nativeBody=[pscustomobject]@{raceGuid=$body.RaceGuid;presetGuid=$body.PresetGuid;skeletonName=$body.SkeletonName;
        gender=$body.Gender;leftHanded=$body.LeftHanded;originalSizeOrdinal=$body.OriginalSizeOrdinal;
        effectiveSizeOrdinal=$body.EffectiveSizeOrdinal}}
function New-State {
    [pscustomobject]@{targetId=$selection.actorId;newPetId=$selection.petId;targetLevel=$expected.targetLevel;
        targetXp=$expected.targetXp;targetBlueprint=$expected.targetBlueprint;newPetLevel=$expected.newPetLevel;
        newPetBlueprint=$expected.newPetBlueprint;nativeBody=$expected.nativeBody;oldPetAbsent=$true;targetUnpaired=$true;
        controlRiderId=$selection.controlActorId;controlMountId=$selection.controlPetId;controlOwnerId=$owner.ownerId;
        controlGeneration=$owner.generation;nativeMoney=$expected.nativeMoney;
        nativeMammoth=[pscustomobject]@{rangerLevel=$rank[0].rangerLevel;companionRank=$rank[0].companionRank;
            featureGuid=$rank[0].featureGuid;featureSourceGuid='97261d609529d834eba4fd4da1bc44dc';
            upgradeGuid=$rank[0].upgradeGuid;petGuid=$rank[0].petGuid;
            riderSize=$rank[0].riderSize;mountSize=$rank[0].mountSize};
        slots=@($saved.Slots|ForEach-Object{[pscustomobject]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}});
        actors=@(@($selection.actorId,$selection.petId,$selection.controlActorId,$selection.controlPetId)|Sort-Object|ForEach-Object{
            [pscustomobject]@{id=$_;inCombat=$false;prepared=$false}});playerInCombat=$false;
        snapshots=0;completedSaves=0;nativeCasts=0}
}
function Refuses([string]$name,[scriptblock]$mutation){
    $state=New-State;&$mutation $state;$refused=$false
    try{Assert-KmcChunk7RetrainColdState $state $selection $saved $owner $expected}catch{$refused=$true}
    if(-not$refused){throw ('Cold Mammoth snapshot accepted '+$name)}
    Write-Host ('PASS refuses '+$name)
}
Assert-KmcChunk7RetrainColdState (New-State) $selection $saved $owner $expected
Write-Host 'PASS batch116 producer-bound Mammoth cold snapshot'
$fieldSelection=[pscustomobject]@{case='medium-horse-native-retrain-field';actorId=$selection.actorId;petId=$selection.petId;
    formerPetId=$selection.formerPetId;controlActorId=$selection.controlActorId;controlPetId=$selection.controlPetId}
Assert-KmcChunk7RetrainColdState (New-State) $fieldSelection $saved $owner $expected
Write-Host 'PASS Mammoth field case retains the same pre-input native cold contract'
Refuses 'lost Large upgrade' {param($s)$s.nativeMammoth.mountSize='Medium'}
Refuses 'rank below seven' {param($s)$s.nativeMammoth.companionRank=6}
Refuses 'rank inflated during cold load' {param($s)$s.nativeMammoth.companionRank=8}
Refuses 'wrong Mammoth unit' {param($s)$s.nativeMammoth.petGuid='0'*32}
Refuses 'changed target XP' {param($s)$s.targetXp++}
Refuses 'changed surviving owner' {param($s)$s.controlOwnerId='0'*32}
Refuses 'unexpected native preparation' {param($s)$s.actors[0].prepared=$true}
$interval=[pscustomobject]@{coldInitialFrame=100;coldContinuationFrame=105;coldInitial=(New-State);coldContinuation=(New-State)}
Assert-KmcChunk7RetrainColdInterval $interval
Write-Host 'PASS five-frame unchanged Mammoth continuation'
$interval.coldContinuation.nativeMammoth.companionRank=8;$refused=$false
try{Assert-KmcChunk7RetrainColdInterval $interval}catch{$refused=$true}
if(-not$refused){throw 'Changed Mammoth rank after five frames was accepted.'}
Write-Host 'PASS refuses changed continuation rank'
Write-Host 'TOTAL PASS 11 FAIL 0; synthetic cold snapshot anchored to immutable native producer, no native field qualification'
