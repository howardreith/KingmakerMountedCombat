[CmdletBinding()]
param([string]$RepositoryRoot)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
. (Join-Path $PSScriptRoot 'Test-Chunk7SmallProgressionEvidence.ps1') -RepositoryRoot $RepositoryRoot -FixturesOnly
$lab=[IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))
$old=Join-Path $lab 'runtime-evidence/20261021-c7b-world112-b100s4-small-retrain-cold/chunk7-progression-evidence.json'
$inventoryPath=Join-Path $lab 'runtime-evidence/20261020-c7b-world90-b77s2-small-mounted-cold/chunk7-persistence-evidence.json'
if((Get-KmcSha256 $old)-cne'aca2e32fc80783df7c303c8cb41991fab2c4f145d9fa72f53344623811f34b86'-or
   (Get-KmcSha256 $inventoryPath)-cne'7620c298c91f33f5bc456a91c4beef2a7ccb66ef4758a13605d58cc682e16f3f'){
    throw 'Immutable native cold shape or Small source changed; synthetic test cannot borrow its fields.'
}
$scratch=Join-Path $lab ('analysis-cache/chunk7-continuation/small-level-cold-reader-'+[Guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $scratch)
try{
    $producer=New-SmallProgressionEvidenceFixture 'small-horse-native-level'
    $producerPath=Join-Path $scratch 'producer.json'
    [IO.File]::WriteAllText($producerPath,(ConvertTo-KmcChunk7Canonical $producer.artifact),[Text.UTF8Encoding]::new($false))
    $p=$producer.artifact.observations;$saved=$p.session.nativeWrite.archive
    $r=Copy-O $producer.request;$r.runId='synthetic-small-level-cold';$r.evidenceRoot=Join-Path $scratch 'run/evidence'
    $r.progression=[pscustomobject]@{case='small-horse-native-level-cold';sourceRunId=$producer.request.runId;
        sourceArtifactSha256=(Get-KmcSha256 $producerPath);sourceArchiveSha256=$p.session.nativeWrite.sha256;
        inventoryRunId=$producer.request.progression.inventoryRunId;inventoryArtifactSha256=$producer.request.progression.inventoryArtifactSha256;
        actorId=$r.progression.actorId;petId=$r.progression.petId;controlActorId=$r.progression.controlActorId;
        controlPetId=$r.progression.controlPetId;raceGuid=$null;presetGuid=$null;skeletonName=$null;gender=$null;
        leftHanded=$null;bodySize=$null;minimumRangerLevel=0;expectedNativeWriteCount=0;nativeWriteLeaf=$null}
    $r.persistenceLoad=[pscustomobject]@{internalName=$p.session.nativeWrite.name;fileName=$p.session.nativeWrite.leaf;
        sha256=$p.session.nativeWrite.sha256;length=$p.session.nativeWrite.length;lastWriteTimeUtcTicks=638907120000000000L;
        gameId=$p.session.nativeWrite.gameId;gameName=$p.session.nativeWrite.gameName;area=$p.session.nativeWrite.area}
    $a=Get-Content -Raw -LiteralPath $old|ConvertFrom-Json
    foreach($key in @('scenario','branch','commit','productVersion','dllSha256','dllMvid')){$a.$key=$r.$key}
    $a.runId=$r.runId;$a.status='PASS';$o=$a.observations
    $o.selection=Copy-O $r.progression;$o.loadedDescriptor=Copy-O $r.persistenceLoad;$o.loadedArchive=Copy-O $saved
    $o.processId=$p.processId+1;$o.source=[pscustomobject]@{sourceRunId=$r.progression.sourceRunId;
        sourceArtifactSha256=$r.progression.sourceArtifactSha256;sourceArchiveSha256=$r.progression.sourceArchiveSha256;
        inventoryRunId=$r.progression.inventoryRunId;inventoryArtifactSha256=$r.progression.inventoryArtifactSha256;
        sourceProcessId=$p.processId;sameCandidate=$true}
    $o.beforeOwners=Copy-O $p.beforeOwners;$o.coldRestoration=Copy-O $p.coldRestoration
    $terminal=@($p.session.events|Where-Object{$_.PSObject.Properties['kind']-and$_.kind-ceq'native-progression-builder-complete'})[0]
    $builder=@($p.session.events|Where-Object{$_.PSObject.Properties['event']-and$_.event-ceq'native-builder-complete'})[0]
    $o.producerTerminal=[pscustomobject]@{targetLevel=$terminal.level;targetXp=$terminal.xp;petLevel=$terminal.petLevel;money=$terminal.money}
    $o|Add-Member -Force NoteProperty producerNativeLevelOutcome (Copy-O $p.nativeLevelOutcome)
    $sourceRows=(Get-Content -Raw -LiteralPath $inventoryPath|ConvertFrom-Json).observations.inheritedFixture.arrangement.actors
    $roster=Copy-O $sourceRows
    @($roster|Where-Object actorId -CEQ $r.progression.actorId)[0].characterLevel=5
    @($roster|Where-Object actorId -CEQ $r.progression.actorId)[0].experience=15000
    @($roster|Where-Object actorId -CEQ $r.progression.petId)[0].characterLevel=$terminal.petLevel
    @($roster|Where-Object actorId -CEQ $r.progression.petId)[0].experience=$builder.actor.petExperience
    $field=@($roster|Sort-Object actorId|ForEach-Object{[pscustomobject]@{actorId=$_.actorId;blueprint=$_.blueprint;
        characterLevel=$_.characterLevel;experience=$_.experience}})
    $slots=@($saved.Slots|Sort-Object ActorId,Index|ForEach-Object{[pscustomobject]@{actorId=$_.ActorId;index=$_.Index;kind=$_.Kind}})
    $actors=@(@($r.progression.actorId,$r.progression.petId,$r.progression.controlActorId,$r.progression.controlPetId)|Sort-Object|ForEach-Object{[pscustomobject]@{id=$_;inCombat=$false;prepared=$false}})
    $control=@($sourceRows|Where-Object actorId -CEQ $r.progression.controlActorId)[0]
    $controlPet=@($sourceRows|Where-Object actorId -CEQ $r.progression.controlPetId)[0]
    $state=[pscustomobject]@{targetLevel=$terminal.level;targetXp=$terminal.xp;petLevel=$terminal.petLevel;
        petXp=$builder.actor.petExperience;controlLevel=$control.characterLevel;controlXp=$control.experience;
        controlPetLevel=$controlPet.characterLevel;controlPetXp=$controlPet.experience;money=$terminal.money;
        nativeLevelOutcome=(Copy-O $p.nativeLevelOutcome);owners=(Copy-O $p.beforeOwners.owners);fieldRoster=$field;
        slots=$slots;actors=$actors;snapshots=0;completedSaves=0;nativeCasts=0}
    $o.coldInitial=Copy-O $state;$o.coldContinuation=Copy-O $state;$o.coldInitialFrame=10;$o.coldContinuationFrame=15
    $sourceRoot=Join-Path $scratch ('runtime-staging/persistence-'+$r.runId+'/Saved Games')
    $o.authorityBefore.root=$sourceRoot;$o.cleanup.authority.root=$sourceRoot
    $script:fakeLevelSource=[pscustomobject]@{artifactPath=$producerPath;inventory=[pscustomobject]@{path=$inventoryPath};sourceProcessId=$p.processId}
    function Get-KmcChunk7ProgressionColdSource($Selection,$Fixture,$Candidate){$script:fakeLevelSource}
    Assert-KmcChunk7LevelColdEvidence $r $a
    Write-Host 'PASS synthetic two-pair Small level cold reader accepts exact producer/native continuation shape'
    foreach($name in @('same process','changed Manual hash','missing owner','missing field actor','changed Ranger rank','changed field XP')){
        $bad=Copy-O $a;$v=$bad.observations
        switch($name){
            'same process'{$v.processId=$p.processId}
            'changed Manual hash'{$v.source.sourceArchiveSha256='0'*64}
            'missing owner'{$v.coldInitial.owners=@($v.coldInitial.owners[0])}
            'missing field actor'{$v.coldInitial.fieldRoster=@($v.coldInitial.fieldRoster|Select-Object -Skip 1)}
            'changed Ranger rank'{$v.coldInitial.nativeLevelOutcome.horseRank++}
            'changed field XP'{$v.coldInitial.fieldRoster[0].experience++}
        }
        $refused=$false;try{Assert-KmcChunk7LevelColdEvidence $r $bad}catch{$refused=$true}
        if(-not$refused){throw ('Small level cold reader accepted '+$name)}
        Write-Host ('PASS refuses '+$name)
    }
    Write-Host 'TOTAL PASS 7 FAIL 0; generated two-pair evidence only, no native cold qualification'
}finally{
    $resolved=[IO.Path]::GetFullPath($scratch);$allowed=[IO.Path]::GetFullPath((Join-Path $lab 'analysis-cache/chunk7-continuation'))
    if(-not$resolved.StartsWith($allowed+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Synthetic test cleanup escaped the lab cache.'}
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
