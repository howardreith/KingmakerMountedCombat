[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$EvidenceDtoSource,[string]$FixtureScript,[string]$FixtureDirectory,[string]$ReaderDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedPairsDtoTests.cs'}
if(-not$PSBoundParameters.ContainsKey('EvidenceDtoSource')){$EvidenceDtoSource=Join-Path $DiagnosticSourceDirectory 'Chunk7EvidenceDto.cs'}
if(-not$PSBoundParameters.ContainsKey('FixtureScript')){$FixtureScript=Join-Path $RepositoryRoot 'scripts/chunk7/Test-Chunk7AcquiredManualEvidence.ps1'}
if(-not$PSBoundParameters.ContainsKey('ReaderDirectory')){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/owned-pairs-dto-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
[xml]$project=Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj')
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($node in $project.SelectNodes("//*[local-name()='Compile']")){
    $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $node.Include));$leaf=Split-Path -Leaf $path
    if($leaf-cin@('RuntimeProtocol.cs','PersistenceSaveAuthorization.cs','Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs','Chunk7ManualSeedSelection.cs','Chunk7ManualDerivedSelection.cs','Chunk7AcquiredManualSelection.cs','Chunk7AssistedFixtureSelection.cs','Chunk7AssistedColdInventorySelection.cs','Chunk7CumulativeAcquisitionSelection.cs','Chunk7OwnedPairsSelection.cs','PersistenceSaveAuthorization.AssistedFixture.cs','PersistenceSaveAuthorization.AssistedColdInventory.cs','PersistenceSaveAuthorization.OwnedPairs.cs')){
        $override=Join-Path $DiagnosticSourceDirectory $leaf;if(Test-Path -LiteralPath $override){$path=$override}
    };[void]$sources.Add($path)
}
foreach($leaf in @('Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs','Chunk7ManualSeedSelection.cs','Chunk7ManualDerivedSelection.cs','Chunk7AcquiredManualSelection.cs','Chunk7AssistedFixtureSelection.cs','Chunk7AssistedColdInventorySelection.cs','Chunk7CumulativeAcquisitionSelection.cs','Chunk7OwnedPairsSelection.cs','PersistenceSaveAuthorization.AssistedFixture.cs','PersistenceSaveAuthorization.AssistedColdInventory.cs','PersistenceSaveAuthorization.OwnedPairs.cs')){
    $path=Join-Path $DiagnosticSourceDirectory $leaf;if(-not(Test-Path -LiteralPath $path)){$path=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)};[void]$sources.Add($path)
}
[void]$sources.Add($TestSource);[void]$sources.Add($EvidenceDtoSource)
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$newtonsoft='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$dll=Join-Path $OutputDirectory 'owned-pairs-dto.dll';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:library','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+',('/out:'+$dll),('/reference:'+$newtonsoft))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Actual owned DTO compilation failed.'}
[void][Reflection.Assembly]::LoadFrom($newtonsoft);[void][Reflection.Assembly]::LoadFrom($dll)

. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
$fixtures=(Get-Content -Raw -LiteralPath (Join-Path $FixtureDirectory 'OwnedPairsEvidenceFixtures.json')|ConvertFrom-Json).fixtures
$pass=0
foreach($f in $fixtures){
 $raw=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($f.request|ConvertTo-Json -Depth 90));$actual=$raw|ConvertFrom-Json
 if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $f.request.ownedPairs)){throw ('Actual DTO casing/types/null keys differ: '+$f.name)};$pass++
 if($actual.scenario-cne(Get-KmcChunk7SelectedScenario $f.request)-or$actual.expectedWriteCount-ne(Get-KmcChunk7OwnedWriteCount $f.request)-or$actual.fixtureArea-cne$f.request.persistenceLoad.area){throw ('Selected native case/fixture/quota differs: '+$f.name)};$pass++
 $expected=if($f.request.ownedPairs.case-ceq'persistence-load'){Get-KmcChunk7PersistenceType $f.request.persistenceCase}else{'Manual'};if($actual.expectedLoadType-cne$expected){throw 'Native input category differs'};$pass++
 if($f.name-cin@('persistence-load-multi-exploration-manual','persistence-load-multi-rt-debt-manual')){
  if($actual.requiresCombatColdBind-cne($f.name-ceq'persistence-load-multi-rt-debt-manual')){throw ('Inherited cold bind mode differs: '+$f.name)};$pass++
 }
 $f.request.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $f.request;$pass++
}
# The actual serializer must leave every old closed selection unchanged while
# emitting the original source area only for the new area save/cold route.
foreach($cold in @($false,$true)){
 $r=($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-auto')[0].request|ConvertTo-Json -Depth 90|ConvertFrom-Json
 $r.persistenceCase='multi-area-autosave';$r.ownedPairs|Add-Member -NotePropertyName sourceArea -NotePropertyValue $r.persistenceLoad.area
 $r|Add-Member -NotePropertyName persistenceAreaTarget -NotePropertyValue ([pscustomobject]@{enterPoint=('e'*32);area=('f'*32);autoSaveMode='AfterEntry'})
 if($cold){$r.ownedPairs.case='persistence-load';$r.ownedPairs.checkpointSourceRunId='completed-owned-area';$r.ownedPairs.checkpointSourceArtifactSha256='9'*64;$r.ownedPairs.expectedNativeWriteCount=0;$r.persistenceLoad.fileName='Auto_1.zks';$r.persistenceLoad.internalName='Auto';$r.persistenceLoad.area=$r.persistenceAreaTarget.area}
 $actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($r|ConvertTo-Json -Depth 90))|ConvertFrom-Json
 if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $r.ownedPairs)){throw 'Area DTO lost exact original-area field or altered old keys'};$pass++
 $r.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $r;$pass++
 if($actual.expectedLoadType-cne$(if($cold){'Auto'}else{'Manual'})-or$actual.expectedWriteCount-ne$(if($cold){0}else{1})){throw 'Area DTO changed native source category or quota'};$pass++
}
$r=($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-auto')[0].request|ConvertTo-Json -Depth 90|ConvertFrom-Json
$r.ownedPairs|Add-Member -NotePropertyName sourceArea -NotePropertyValue $null
$actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($r|ConvertTo-Json -Depth 90))|ConvertFrom-Json
if($actual.selection.PSObject.Properties['sourceArea']){throw 'Legacy DTO added a null SourceArea key'};$pass++
$refused=$false;try{Assert-KmcChunk7OwnedPairsDescriptor $r}catch{$refused=$true};if(-not$refused){throw 'Raw legacy selection accepted an extra null SourceArea key'};$pass++
# Owner-seed kind: the actual serializer emits exactly its closed key set (xpAssistance
# present even when null) and never the producer/pet keys; the reader accepts the projection.
function New-KmcOwnerSeedDtoRequest([bool]$Assisted){
 $r=($fixtures|Where-Object name -CEQ 'exploration')[0].request|ConvertTo-Json -Depth 90|ConvertFrom-Json;$p=$r.ownedPairs
 $assist=if($Assisted){[pscustomobject][ordered]@{unitId=$p.riderBId;targetCharacterLevel=8}}else{$null}
 $r.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-exploration';seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$p.riderAId;riderBId=$p.riderBId;partyIds=@($p.riderAId,$p.riderBId,'50505050-5050-4050-8050-505050505050');xpAssistance=$assist;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $r.persistenceLoad.internalName='KMC_C7_OWNER_SEED';$r.persistenceLoad.fileName='Manual_302_KMC_C7_OWNER_SEED.zks';$r.persistenceLoad.sha256='9'*64;$r
}
foreach($assisted in @($true,$false)){
 $r=New-KmcOwnerSeedDtoRequest $assisted
 $actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($r|ConvertTo-Json -Depth 90))|ConvertFrom-Json
 if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $r.ownedPairs)){throw ('Owner-seed DTO keys/types differ (assisted='+$assisted+')')};$pass++
 if($actual.scenario-cne'chunk7-two-pair-exploration'-or$actual.expectedWriteCount-ne0-or$actual.expectedLoadType-cne'Manual'-or$actual.fixtureArea-cne$r.persistenceLoad.area){throw 'Owner-seed DTO selected another runtime scenario, quota or input'};$pass++
 $r.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $r;$pass++
}
$r=New-KmcOwnerSeedDtoRequest $true;$r.ownedPairs.seedId='KMC_C7_NO_MODS_OWNER_SEED';$r.persistenceLoad.internalName='KMC C7 NO MODS OWNER SEED';$r.persistenceLoad.fileName='Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks'
$actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($r|ConvertTo-Json -Depth 90))|ConvertFrom-Json;$r.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $r;$pass++
foreach($mutation in @('producer-pin-on-seed','seed-pin-on-producer')){
 if($mutation-ceq'producer-pin-on-seed'){$r=New-KmcOwnerSeedDtoRequest $true;$r.ownedPairs|Add-Member -NotePropertyName sourceRunId -NotePropertyValue 'synthetic-two-owner-manual'}
 else{$r=($fixtures|Where-Object name -CEQ 'exploration')[0].request|ConvertTo-Json -Depth 90|ConvertFrom-Json;$r.ownedPairs|Add-Member -NotePropertyName seedId -NotePropertyValue 'KMC_C7_OWNER_SEED'}
 $typed=$false;try{[void][KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($r|ConvertTo-Json -Depth 90))}catch{$typed=$true}
 $read=$false;try{Assert-KmcChunk7OwnedPairsDescriptor $r}catch{$read=$true}
 if(-not$typed-or-not$read){throw ('Owner-seed/producer pin crossing accepted: '+$mutation)};$pass++
}
# Owner-seed-derived persistence: the producer keeps the owner-seed key set with its one Manual
# write and checkpoint; the consumer carries producer pins, seed pins and the four roles only.
$r=New-KmcOwnerSeedDtoRequest $true;$r.ownedPairs.case='owner-seed-persistence-save';$r.ownedPairs.expectedNativeWriteCount=1;$r|Add-Member -NotePropertyName persistenceCase -NotePropertyValue 'multi-exploration-manual' -Force
$actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($r|ConvertTo-Json -Depth 90))|ConvertFrom-Json
if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $r.ownedPairs)){throw 'Owner-seed Manual producer DTO keys/types differ'};$pass++
if($actual.scenario-cne'chunk7-two-pair-exploration'-or$actual.expectedWriteCount-ne1-or$actual.expectedLoadType-cne'Manual'-or(Get-KmcChunk7OwnedWriteCount $r)-ne1){throw 'Owner-seed Manual producer DTO selected another runtime scenario, quota or input'};$pass++
$r.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $r;$pass++
$p=($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0].request|ConvertTo-Json -Depth 90|ConvertFrom-Json;$s=$p.ownedPairs
$p.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-persistence-load';sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);sourceArchiveSha256=$p.persistenceLoad.sha256;seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$s.riderAId;mountAId=$s.mountAId;riderBId=$s.riderBId;mountBId=$s.mountBId;partyIds=$s.partyIds;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
$p.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$p.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks'
$actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($p|ConvertTo-Json -Depth 90))|ConvertFrom-Json
if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $p.ownedPairs)){throw 'Owner-seed-derived consumer DTO keys/types differ'};$pass++
if($actual.scenario-cne'chunk7-persistence-load'-or$actual.expectedWriteCount-ne0-or$actual.expectedLoadType-cne'Manual'-or(Get-KmcChunk7OwnedWriteCount $p)-ne0){throw 'Owner-seed-derived consumer DTO selected another runtime scenario, quota or input'};$pass++
$p.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $p;$pass++
$m=$p|ConvertTo-Json -Depth 90|ConvertFrom-Json
$m.persistenceCase='manticore-fixture-cold-manual'
$m.ownedPairs.sourceRunId='synthetic-manticore-producer'
$m.ownedPairs|Add-Member -NotePropertyName sourceArea -NotePropertyValue '314e8c3b15b896a4f847c85f9250f02b'
$m.persistenceLoad.internalName='KMC_C7_AREA_FIXTURE'
$m.persistenceLoad.fileName='Manual_305_KMC_C7_AREA_FIXTURE.zks'
$m.persistenceLoad.area=$m.ownedPairs.sourceArea
$actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($m|ConvertTo-Json -Depth 90))|ConvertFrom-Json
if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $m.ownedPairs)-or$actual.scenario-cne'chunk7-persistence-load'-or
   $actual.expectedWriteCount-ne0-or$actual.expectedLoadType-cne'Manual'-or$actual.fixtureArea-cne$m.ownedPairs.sourceArea-or
   $actual.requiresCombatColdBind){throw 'Manticore cold DTO changed source, area, load category, write quota or exploration bind mode'};$pass++
$m.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $m;$pass++
foreach($mutation in @('wrong-area','wrong-name','extra-checkpoint')){
 $q=$m|ConvertTo-Json -Depth 90|ConvertFrom-Json
 if($mutation-ceq'wrong-area'){$q.ownedPairs.sourceArea='b10e86516b6437d45a629931c30936fe'}
 elseif($mutation-ceq'wrong-name'){$q.persistenceLoad.fileName='Manual_305_KMC_C7_SEED_WORLD.zks'}
 else{$q.ownedPairs|Add-Member -NotePropertyName checkpointSourceRunId -NotePropertyValue 'unrelated-checkpoint'}
 $typed=$false;try{[void][KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($q|ConvertTo-Json -Depth 90))}catch{$typed=$true}
 $reader=$false;try{Assert-KmcChunk7OwnedPairsDescriptor $q}catch{$reader=$true}
 if(($mutation-cne'wrong-name'-and-not$typed)-or-not$reader){throw ('Manticore cold DTO admitted '+$mutation)};$pass++
}
foreach($mutation in @('xp-on-consumer','checkpoint-on-consumer')){
 $q=$p|ConvertTo-Json -Depth 90|ConvertFrom-Json
 if($mutation-ceq'xp-on-consumer'){$q.ownedPairs|Add-Member -NotePropertyName xpAssistance -NotePropertyValue ([pscustomobject]@{unitId=$s.riderBId;targetCharacterLevel=8})}else{$q.ownedPairs|Add-Member -NotePropertyName checkpointSourceRunId -NotePropertyValue 'synthetic-owned-checkpoint'}
 $typed=$false;try{[void][KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($q|ConvertTo-Json -Depth 90))}catch{$typed=$true}
 $read=$false;try{Assert-KmcChunk7OwnedPairsDescriptor $q}catch{$read=$true}
 if(-not$typed-or-not$read){throw ('Owner-seed-derived consumer accepted '+$mutation)};$pass++
}
# Owner-seed checkpoint pair (Directive 4): both kinds carry the Manual producer, checkpoint-parent and
# seed pins with the four roles; the producer writes one Quick/Auto, the consumer loads that slot.
foreach($checkpoint in @('multi-exploration-quick','multi-exploration-auto','multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual')){
 # P2 combat checkpoints write and load one genuine native Manual under the distinct KMC_C7_SEED_CHECKPOINT name.
 $type=if($checkpoint-ceq'multi-exploration-quick'){'Quick'}elseif($checkpoint-ceq'multi-exploration-auto'){'Auto'}else{'Manual'}
 foreach($kind in @('save','load')){
  $c=($fixtures|Where-Object name -CEQ ('persistence-'+$kind+'-'+$checkpoint))[0].request|ConvertTo-Json -Depth 90|ConvertFrom-Json;$s=$c.ownedPairs;$save=$kind-ceq'save'
  $c.ownedPairs=[pscustomobject][ordered]@{case=('owner-seed-checkpoint-'+$kind);sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);sourceArchiveSha256=$(if($save){$c.persistenceLoad.sha256}else{'7'*64});checkpointSourceRunId=$(if($save){'synthetic-owner-seed-cold'}else{'synthetic-owner-seed-checkpoint'});checkpointSourceArtifactSha256=('8'*64);seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$s.riderAId;mountAId=$s.mountAId;riderBId=$s.riderBId;mountBId=$s.mountBId;partyIds=$s.partyIds;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=$(if($save){1}else{0})}
  if($save){$c.persistenceLoad.internalName='KMC_C7_SEED_WORLD';$c.persistenceLoad.fileName='Manual_303_KMC_C7_SEED_WORLD.zks'}elseif($type-ceq'Manual'){$c.persistenceLoad.internalName='KMC_C7_SEED_CHECKPOINT';$c.persistenceLoad.fileName='Manual_304_KMC_C7_SEED_CHECKPOINT.zks'}
  $actual=[KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($c|ConvertTo-Json -Depth 90))|ConvertFrom-Json
  if((ConvertTo-KmcChunk7Canonical $actual.selection)-cne(ConvertTo-KmcChunk7Canonical $c.ownedPairs)){throw ('Owner-seed checkpoint '+$kind+' DTO keys/types differ')};$pass++
  $scenario=if($save){'chunk7-persistence-save'}else{'chunk7-persistence-load'};$writes=if($save){1}else{0};$inputType=if($save){'Manual'}else{$type}
  if($actual.scenario-cne$scenario-or$actual.expectedWriteCount-ne$writes-or$actual.expectedLoadType-cne$inputType-or(Get-KmcChunk7OwnedWriteCount $c)-ne$writes){throw ('Owner-seed checkpoint '+$kind+' DTO selected another runtime scenario, quota or input')};$pass++
  $c.ownedPairs=$actual.selection;Assert-KmcChunk7OwnedPairsDescriptor $c;$pass++
  foreach($mutation in @('xp','companion','area')){
   $q=$c|ConvertTo-Json -Depth 90|ConvertFrom-Json
   switch($mutation){'xp'{$q.ownedPairs|Add-Member -NotePropertyName xpAssistance -NotePropertyValue ([pscustomobject]@{unitId=$s.riderBId;targetCharacterLevel=8})}'companion'{$q.ownedPairs|Add-Member -NotePropertyName companionId -NotePropertyValue $s.mountBId}'area'{$q.ownedPairs|Add-Member -NotePropertyName sourceArea -NotePropertyValue ('a'*32)}}
   $typed=$false;try{[void][KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($q|ConvertTo-Json -Depth 90))}catch{$typed=$true}
   $read=$false;try{Assert-KmcChunk7OwnedPairsDescriptor $q}catch{$read=$true}
   if(-not$typed-or-not$read){throw ('Owner-seed checkpoint '+$kind+' accepted '+$mutation)};$pass++
  }
 }
}
# The combat checkpoint consumer descriptor refuses every other Manual name: acquired ancestry and the derived
# input. The typed selection itself stays valid (the C# descriptor refusal is the full-request Validate in Wiring).
foreach($checkpoint in @('multi-rt-debt-manual','multi-tb-debt-manual','zero-mounted-tb-retained-manual')){foreach($name in @('KMC_C7_WORLD','KMC_C7_SEED_WORLD')){
 $c=($fixtures|Where-Object name -CEQ ('persistence-load-'+$checkpoint))[0].request|ConvertTo-Json -Depth 90|ConvertFrom-Json;$s=$c.ownedPairs
 $c.ownedPairs=[pscustomobject][ordered]@{case='owner-seed-checkpoint-load';sourceRunId='synthetic-owner-seed-manual';sourceArtifactSha256=('6'*64);sourceArchiveSha256=('7'*64);checkpointSourceRunId='synthetic-owner-seed-checkpoint';checkpointSourceArtifactSha256=('8'*64);seedId='KMC_C7_OWNER_SEED';seedSha256=('9'*64);riderAId=$s.riderAId;mountAId=$s.mountAId;riderBId=$s.riderBId;mountBId=$s.mountBId;partyIds=$s.partyIds;baseGameOnly=$true;expectedNativeLoadCount=1;expectedNativeWriteCount=0}
 $c.persistenceLoad.internalName=$name;$c.persistenceLoad.fileName='Manual_304_'+$name+'.zks'
 [void][KingmakerMountedCombat.Tests.OwnedPairsDtoTests]::Project(($c|ConvertTo-Json -Depth 90))
 $read=$false;try{Assert-KmcChunk7OwnedPairsDescriptor $c}catch{$read=$true}
 if(-not$read){throw ('Owner-seed combat checkpoint consumer accepted the Manual name '+$name+' for '+$checkpoint)};$pass++
}}
@{passed=$pass;failed=0;nativeInvoked=$false;archiveRead=$false;serializer='exact-installed-Newtonsoft';sourceHashes=@($sources|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('Owned DTO PASS='+$pass+' FAIL=0')
