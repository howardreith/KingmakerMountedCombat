[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[switch]$FixturesOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
$smallFixturesOnly=$FixturesOnly.IsPresent
. (Join-Path $PSScriptRoot 'Test-Chunk7ProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -ReaderDirectory $ReaderDirectory -FixturesOnly
$lab=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))
$coldRoot=Join-Path $lab 'runtime-evidence/20261020-c7b-world90-b77s2-small-mounted-cold'
$q=Get-Content -Raw -LiteralPath (Join-Path $coldRoot 'runtime-request.json')|ConvertFrom-Json
$c=Get-Content -Raw -LiteralPath (Join-Path $coldRoot 'chunk7-persistence-evidence.json')|ConvertFrom-Json
if((Get-KmcSha256 (Join-Path $coldRoot 'chunk7-persistence-evidence.json'))-cne'7620c298c91f33f5bc456a91c4beef2a7ccb66ef4758a13605d58cc682e16f3f'){
    throw 'Historical Small cold observation changed; this read-only reader test cannot transfer a verdict.'
}
function New-SmallProgressionEvidenceFixture([string]$Case='small-horse-native-dismiss') {
    $base=if($Case-ceq'small-horse-native-retrain-horse'){'owned-horse-native-retrain-horse'}elseif($Case-ceq'small-horse-native-level'){'owned-horse-native-level'}else{'owned-horse-native-dismiss'}
    $f=New-ProgressionFixture $base;$s=$f.request.progression;$n=$q.ownedPairs
    $map=[ordered]@{};$map[$s.controlActorId]=$n.riderAId;$map[$s.controlPetId]=$n.mountAId
    $map[$s.actorId]=$n.riderBId;$map[$s.petId]=$n.mountBId
    $map[$f.artifact.observations.source.inventoryWorld.mainActorId]=$n.partyIds[1]
    $raw=ConvertTo-KmcChunk7Canonical $f
    foreach($entry in $map.GetEnumerator()){$raw=$raw.Replace($entry.Key,$entry.Value)}
    $f=$raw|ConvertFrom-Json;$r=$f.request;$o=$f.artifact.observations;$s=$r.progression
    $s.case=$Case;$s.ownedSource=Copy-O $n
    if($Case-ceq'small-horse-native-retrain-horse'){$s.raceGuid='b0c3ef2729c498f47970bb50fa1acd30';$s.presetGuid='639cebe8cbc098345af429d62fd79578';$s.skeletonName='MaleHalfling';$s.gender='Male';$s.leftHanded=$false;$s.bodySize='Small'}
    if($Case-ceq'small-horse-native-level'){$s.minimumRangerLevel=4;$s.expectedNativeWriteCount=1;$s|Add-Member NoteProperty fixtureExperienceGrant 6000}
    $s.sourceRunId=$n.checkpointSourceRunId;$s.sourceArtifactSha256=$n.checkpointSourceArtifactSha256
    $s.sourceArchiveSha256=$q.persistenceLoad.sha256;$s.inventoryRunId=$q.runId
    $s.inventoryArtifactSha256=(Get-KmcSha256 (Join-Path $coldRoot 'chunk7-persistence-evidence.json'))
    $r.persistenceLoad=Copy-O $q.persistenceLoad;$o.loadedDescriptor=Copy-O $r.persistenceLoad;$o.selection=Copy-O $s
    $o.loadedArchive.CampaignId=$r.persistenceLoad.gameId;$o.loadedArchive.AreaId=$r.persistenceLoad.area
    @($o.loadedArchive.Pairs|Where-Object{$_.Rider.Id-ceq$s.actorId})[0].ProfileId='small-default-halfling-horse-v1'
    foreach($owner in $o.beforeOwners.owners){$pair=@($o.loadedArchive.Pairs|Where-Object PairId -CEQ $owner.ownerId)[0];$owner.riderId=([Guid]$pair.Rider.Id).ToString('N');$owner.mountId=([Guid]$pair.Mount.Id).ToString('N')}
    $o.session.pairCustody.riderId=([Guid]$s.actorId).ToString('N');$o.session.pairCustody.mountId=([Guid]$s.petId).ToString('N')
    $source=$o.source
    foreach($key in @('sourceRunId','sourceArtifactSha256','sourceArchiveSha256','inventoryRunId','inventoryArtifactSha256')){$source.$key=$s.$key}
    $source.ownedSource=Copy-O $s.ownedSource
    $source.inventoryActor=Copy-O @($c.observations.inheritedFixture.arrangement.actors|Where-Object{$_.actorId-ceq$s.actorId})[0]
    $source|Add-Member NoteProperty smallColdFixture (Copy-O $c.observations.inheritedFixture)
    $source|Add-Member NoteProperty smallFixtureSetupOnly $true
    $source.inventoryWorld.mainActorId=$n.riderAId
    $source.inventoryWorld.party=@($source.smallColdFixture.arrangement.actors|ForEach-Object{[pscustomobject]@{id=$_.actorId;blueprint=$_.blueprint}})
    $o.nativeBodyBefore.OriginalSizeOrdinal=3;$o.nativeBodyBefore.EffectiveSizeOrdinal=3
    if($Case-cin@('small-horse-native-retrain-horse','small-horse-native-level')){
        foreach($key in @('RaceGuid','PresetGuid','SkeletonName','Gender','LeftHanded')){$o.nativeBodyBefore.$key=$s.($key.Substring(0,1).ToLowerInvariant()+$key.Substring(1))}
        $write=$o.session.nativeWrite;$write.gameId=$r.persistenceLoad.gameId;$write.gameName=$r.persistenceLoad.gameName;$write.area=$r.persistenceLoad.area
        $write.archive.CampaignId=$r.persistenceLoad.gameId;$write.archive.AreaId=$r.persistenceLoad.area
        if($Case-ceq'small-horse-native-level'){@($write.archive.Pairs|Where-Object{$_.Rider.Id-ceq$s.actorId})[0].ProfileId='small-default-halfling-horse-v1'}
        function Match-SmallXp($Value,[int]$Xp){
            if($null-eq$Value-or$Value-is[string]-or$Value.GetType().IsValueType){return}
            if($Value-is[Array]){foreach($item in $Value){Match-SmallXp $item $Xp};return}
            if($Value.PSObject.Properties['xp']-and$Value.xp-eq20000){$Value.xp=$Xp}
            foreach($property in $Value.PSObject.Properties){Match-SmallXp $property.Value $Xp}
        }
        $operationXp=if($Case-ceq'small-horse-native-level'){15000}else{$source.inventoryActor.experience}
        Match-SmallXp $o.session $operationXp
    }
    foreach($row in $o.session.events){if($row.PSObject.Properties['xp']){$row.xp=$(if($Case-ceq'small-horse-native-level'){15000}else{$source.inventoryActor.experience})}}
    if($Case-ceq'small-horse-native-level'){
        $actors=$source.smallColdFixture.arrangement.actors
        $sourcePair=@($source.smallColdFixture.pairs|Where-Object riderId -CEQ $s.actorId)[0]
        $controlPair=@($source.smallColdFixture.pairs|Where-Object riderId -CEQ $s.controlActorId)[0]
        $pet=@($actors|Where-Object actorId -CEQ $s.petId)[0]
        $control=@($actors|Where-Object actorId -CEQ $s.controlActorId)[0]
        $controlPet=@($actors|Where-Object actorId -CEQ $s.controlPetId)[0]
        $o|Add-Member NoteProperty fixtureExperienceSetup ([pscustomobject]@{
            contract='small-native-level-eligibility-setup-v1';methodToken='06001F80';methodModuleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';
            sourceXp=9000;sourceLevel=4;nextThreshold=15000;nativeGrant=6000;afterXp=15000;afterLevel=4;
            petXpBefore=$pet.experience;petXpAfter=$pet.experience;petLevelBefore=$pet.characterLevel;petLevelAfter=$pet.characterLevel;
            controlXpBefore=$control.experience;controlXpAfter=$control.experience;controlPetXpBefore=$controlPet.experience;controlPetXpAfter=$controlPet.experience;
            moneyBefore=$source.smallColdFixture.arrangement.money;moneyAfter=$source.smallColdFixture.arrangement.money;
            snapshotsBefore=0;snapshotsAfter=0;completedSavesBefore=0;completedSavesAfter=0;castsBefore=0;castsAfter=0;
            sourceInitiallyUnready=$true;targetReadyAfterGrant=$true;ordinaryLevelChoicesStarted=$false})
        $o|Add-Member NoteProperty nativeLevelOutcome ([pscustomobject]@{
            rangerLevel=5;horseFeatureGuid='7db7c50677e39f09feef56f3831fc723';
            horseFeatureSourceGuid=$sourcePair.sourceGuid;horseRankGuid='1670990255e4fe948a863bafd5dbda5d';
            horseRank=$sourcePair.rank;petBlueprintGuid=$pet.blueprint;
            controlRangerLevel=4;controlHorseFeatureSourceGuid=$controlPair.sourceGuid;controlHorseRank=$controlPair.rank})
    }
    foreach($roster in @($o.ownedRosterBefore,$o.ownedRosterAfter)){
        $roster.originalPartyIds=Copy-O $n.partyIds;$roster.originalCharacterIds=Copy-O $c.observations.inheritedFixture.currentCharacterIds
        $roster.characterIds=@($roster.originalCharacterIds|Where-Object{!$roster.targetRemoved-or$_-cne$s.actorId})
        $roster.party+=@([pscustomobject]@{id=$n.partyIds[2];object=31;descriptor=131;view=231},
            [pscustomobject]@{id=$n.partyIds[3];object=32;descriptor=132;view=232})
    }
    return $f
}
if($smallFixturesOnly){return}
$script:passes=0
Good 'Small seven-actor dismissal public reader accepts exact cold fixture and real native-operation shape' {
    $f=New-SmallProgressionEvidenceFixture;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact
}
Good 'Small Horse retrain public reader accepts exact cold fixture and native Manual shape' {
    $f=New-SmallProgressionEvidenceFixture 'small-horse-native-retrain-horse';Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact
}
Good 'Small Horse later-level public reader separates saved XP from disclosed native setup and native Manual' {
    $f=New-SmallProgressionEvidenceFixture 'small-horse-native-level';Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact
}
foreach($name in @('changed grant','changed source XP','changed control XP','fake readiness','fabricated level choice','lost Ranger class','foreign Horse source','lost control rank')){
    $f=New-SmallProgressionEvidenceFixture 'small-horse-native-level'
    switch($name){
        'changed grant'{$f.artifact.observations.fixtureExperienceSetup.nativeGrant=6001}
        'changed source XP'{$f.artifact.observations.fixtureExperienceSetup.sourceXp=9001}
        'changed control XP'{$f.artifact.observations.fixtureExperienceSetup.controlXpAfter++}
        'fake readiness'{$f.artifact.observations.fixtureExperienceSetup.targetReadyAfterGrant=$false}
        'fabricated level choice'{$f.artifact.observations.fixtureExperienceSetup.ordinaryLevelChoicesStarted=$true}
        'lost Ranger class'{$f.artifact.observations.nativeLevelOutcome.rangerLevel=4}
        'foreign Horse source'{$f.artifact.observations.nativeLevelOutcome.horseFeatureSourceGuid='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'}
        'lost control rank'{$f.artifact.observations.nativeLevelOutcome.controlHorseRank=0}
    }
    $refused=$false;try{Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}catch{$refused=$true}
    if(-not$refused){throw ('Small Horse later-level accepted '+$name)}
    $script:passes++;Write-Host ('PASS Small Horse later-level refuses '+$name)
}
Bad 'Small Horse retrain refuses changed observed body' {
    $f=New-SmallProgressionEvidenceFixture 'small-horse-native-retrain-horse'
    $f.artifact.observations.nativeBodyBefore.RaceGuid='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
    Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact
}
Bad 'Small Horse retrain refuses Auto substituted for native Manual' {
    $f=New-SmallProgressionEvidenceFixture 'small-horse-native-retrain-horse'
    $f.artifact.observations.session.nativeWrite.type='Auto'
    Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact
}
$mutations=[ordered]@{
    'coherent foreign paid-companion blueprint'={param($f)
        $o=$f.artifact.observations;$id=$f.request.progression.actorId;$foreign='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
        $o.source.inventoryActor.blueprint=$foreign
        @($o.source.smallColdFixture.arrangement.actors|Where-Object actorId -CEQ $id)[0].blueprint=$foreign
        @($o.source.inventoryWorld.party|Where-Object id -CEQ $id)[0].blueprint=$foreign}
    'duplicate arrangement actor'={param($f)
        $rows=$f.artifact.observations.source.smallColdFixture.arrangement.actors
        $rows[2].actorId=$rows[1].actorId}
    'substituted Small pair rider'={param($f)
        $f.artifact.observations.source.smallColdFixture.pairs[1].riderId=$f.request.progression.controlActorId}
    'foreign sourced Horse fact'={param($f)
        $f.artifact.observations.source.smallColdFixture.pairs[1].sourceGuid='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'}
    'foreign Small rank'={param($f)
        $f.artifact.observations.source.smallColdFixture.pairs[1].rank=2}
    'foreign Small profile'={param($f)
        $f.artifact.observations.source.smallColdFixture.pairs[1].profileId='medium-humanoid-horse-v1'}
    'foreign fixture origin'={param($f)
        $f.artifact.observations.source.smallColdFixture.sourceCase='acquired-field'}
    'fixture singleton array'={param($f)
        $source=$f.artifact.observations.source;$source.smallColdFixture=@($source.smallColdFixture)}
    'arrangement singleton array'={param($f)
        $fixture=$f.artifact.observations.source.smallColdFixture;$fixture.arrangement=@($fixture.arrangement)}
    'Medium target body'={param($f)
        $f.artifact.observations.nativeBodyBefore.EffectiveSizeOrdinal=4}
}
foreach($entry in $mutations.GetEnumerator()){
    Bad $entry.Key {$f=New-SmallProgressionEvidenceFixture;&$entry.Value $f;Assert-KmcChunk7ProgressionEvidence $f.request $f.artifact}
}
Write-Host ('TOTAL PASS '+$script:passes+' FAIL 0; synthetic native operation, historical cold fixture facts, no new native qualification')
