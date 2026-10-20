[CmdletBinding()]
param([string]$RepositoryRoot,[string]$ReaderDirectory,[string]$FixtureDirectory,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$ReaderDirectory){$ReaderDirectory=Join-Path $RepositoryRoot 'scripts/runtime'}
if(-not$FixtureDirectory){$FixtureDirectory=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/owned-area-reader-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Owned area reader output must be fresh.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$testOutput=$OutputDirectory
. (Join-Path $ReaderDirectory 'RuntimeHarness.Common.ps1')
. (Join-Path $ReaderDirectory 'Chunk7OwnedPairsFixtures.ps1')
$fixtures=(Get-Content -Raw -LiteralPath (Join-Path $FixtureDirectory 'OwnedPairsEvidenceFixtures.json')|ConvertFrom-Json).fixtures
$script:areaLab=Join-Path $testOutput 'virtual-lab'
function Get-KmcLabRoot{$script:areaLab}
function Clone-Area($x){return ($x|ConvertTo-Json -Depth 90|ConvertFrom-Json)}
function Put-Area($x,[string]$name,$value){$x|Add-Member -NotePropertyName $name -NotePropertyValue $value -Force}
function New-AreaCase {
 $f=Clone-Area (($fixtures|Where-Object name -CEQ 'persistence-save-multi-exploration-auto')[0]);$r=$f.request;$o=$f.artifact.observations
 $r.persistenceCase='multi-area-autosave';$o.checkpoint=$r.persistenceCase
 Put-Area $r.ownedPairs sourceArea $r.persistenceLoad.area;Put-Area $r persistenceAreaTarget ([pscustomobject]@{enterPoint=('e'*32);area=('f'*32);autoSaveMode='AfterEntry'})
 $extra='50505050-5050-4050-8050-505050505050';$r.ownedPairs.partyIds+=@($extra)
 foreach($c in @($o.inheritedFixture,$o.inheritedCleanup)){$c.partyIds+=@($extra);$c.currentPartyIds+=@($extra);$c.currentCharacterIds+=@($extra);$actor=Clone-Area $c.arrangement.actors[0];$actor.actorId=$extra;$c.arrangement.actors+=@($actor)}
 $o.nativeWrite.archive.AreaId=$r.persistenceAreaTarget.area;$o.nativeWrite.nativeCallback=$false;$o.PSObject.Properties.Remove('saveRequested');$o.ownedOutputCompletion.callbackCount=0
 ($o.ownedOutputCompletion.nativeInventory|Where-Object nativeType -CEQ 'Auto').area=$r.persistenceAreaTarget.area
 $o.snapshots[0].gameTicks=10000000
 $area=[pscustomobject]@{mode='AfterEntry';enterPoint=$r.persistenceAreaTarget.enterPoint;sourceArea=$r.ownedPairs.sourceArea;destinationArea=$r.persistenceAreaTarget.area;explicitSaveGameDispatches=0;observerError=$null;nativeArchiveSha256=$o.nativeWrite.sha256;nativeSaveCallbackExpected=$false;suspensionObserved=$true;duplicateReadyNoOp=$true;loadingTicks=2}
 $labels=[Collections.Generic.HashSet[int]]::new()
 for($i=0;$i-lt4;$i++){
  $s=Clone-Area $o.snapshots[0];$s.label=@('before-native-transfer','native-header-starting','native-header-staged','after-native-transfer')[$i];$s.snapshots=if($i-lt2){0}else{1}
  Put-Area $s area $(if($i-eq0){$r.ownedPairs.sourceArea}else{$r.persistenceAreaTarget.area});Put-Area $s frame (10+$i);Put-Area $s sameWorld $true;Put-Area $s pending $false;Put-Area $s suspensions $(if($i-eq0){0}else{1});Put-Area $s resumes $(if($i-eq0){0}else{2});Put-Area $s activeOwners 2;Put-Area $s activeSaveScope ($i-eq1-or$i-eq2)
  $actors=@();$views=@();$characters=@();$ids=@($r.ownedPairs.partyIds)
  for($j=0;$j-lt$ids.Count;$j++){
   $id=$ids[$j];$pair=@($o.inheritedFixture.pairs|Where-Object{$_.riderId-ceq$id-or$_.mountId-ceq$id});$obj=if($j-eq4){1010}elseif($pair[0].riderId-ceq$id){$pair[0].rider}else{$pair[0].mount}
   $pet=if($j-eq0-or$j-eq2){$ids[$j+1]}else{$null};$master=if($j-eq1-or$j-eq3){$ids[$j-1]}else{$null}
   $arr=@($o.inheritedFixture.arrangement.actors|Where-Object actorId -CEQ $id)[0]
   $actors+=@([pscustomobject]@{id=$id;blueprint=$arr.blueprint;object=$obj;descriptor=(3000+$j);viewObject=(4000+$j);view=(5000+$j);petId=$pet;masterId=$master;inState=$true;inGame=$true;inCombat=$false;commandsEmpty=$true;queuedCommands=0})
   if($null-eq$master){$characters+=@([pscustomobject]@{id=$id;object=$obj})}
   if($j-lt4){$views+=@([pscustomobject]@{id=$id;viewId=(5000+$j);baselineViewId=(5000+$j);nativeMatches=1;viewBound=$true;commandsEmpty=$true})}
   foreach($n in @($obj,(3000+$j),(4000+$j))){[void]$labels.Add($n)}
  }
  $custody=Clone-Area $o.inheritedFixture;$custody.phase='replay-field';foreach($n in $custody.identities.ids){[void]$labels.Add([int]$n)}
  Put-Area $s ownedField ([pscustomobject]@{loadOrdinal=1;areaOrdinal=0;custody=$custody;area=$s.area;characters=$characters;actors=$actors})
  $world=[pscustomobject]@{game=1;player=2;manager=3;state=4;areaState=$(if($i-eq0){5}else{9});mainState=$(if($i-eq0){6}else{10});worldScope=$(if($i-eq0){7}else{8});authorityGeneration=1}
  Put-Area $s ownedWorld $world;foreach($key in @('game','player','manager','state','areaState','mainState','worldScope')){[void]$labels.Add([int]$world.$key)}
  Put-Area $s views $views;$movers=@()
  for($j=0;$j-lt2;$j++){$p=$o.inheritedFixture.pairs[$j];$m=if($i-eq0){6000+$j}else{7000+$j};$movers+=@([pscustomobject]@{riderId=$p.riderId;mountId=$p.mountId;baselineOverride=(6000+$j);runtimeOverride=$m;viewOverride=$m;overrideComponents=@($m);invariant=$null;saveSerializationSuspended=($i-eq2);stockEnabled=$false;mountStockEnabled=$true;mountOverride=0;mountOverrideComponents=@();riderViewBoundToOwner=$true;mountViewBoundToOwner=$true})};Put-Area $s movementAuthorities $movers
  if($i-eq1-or$i-eq2){Put-Area $s nativeSaveBoundary ([pscustomobject]@{operation=2001;requested=2002;prepared=2003;sourceDescriptor=2004;requestedType='Auto';requestedName=$o.nativeWrite.nativeName;preparedType='Auto';preparedName=$o.nativeWrite.nativeName;preparedLeaf='Auto_1.zks';preparedArea=$r.persistenceAreaTarget.area;preparedPath=$o.nativeWrite.path;completionCount=0;callbackCount=0;snapshotCount=($i-1)})}
  Put-Area $area @('before','headerStarting','headerStaged','after')[$i] $s
 }
 Put-Area $o.ownedOutputCompletion areaSaveIdentity ([pscustomobject]@{operation=2001;requested=2002;prepared=2003;sourceDescriptor=2004;completion=2005;path=$o.nativeWrite.path})
 foreach($n in @(2001,2002,2003,2004,2005)){[void]$labels.Add($n)}
 Put-Area $area identityRetention ([pscustomobject]@{capacity=128;ids=@($labels|Sort-Object);faults=0;released=$true;retainedAfterRelease=0});Put-Area $o areaTransfer $area
 return (Clone-Area $f)
}
$results=[Collections.Generic.List[object]]::new()
function Check-Area([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Reason=''){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and(-not$Reason-or$errorText-match$Reason)};$results.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
# Existing schema and category neighbors run their unchanged complete reader.
foreach($kind in @('manual','quick','auto')){Check-Area ('legacy owned '+$kind){$f=Clone-Area (($fixtures|Where-Object name -CEQ ('persistence-save-multi-exploration-'+$kind))[0]);Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}}
Check-Area 'owned exact restored area Auto with unrelated fifth actor' {$f=New-AreaCase;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
$bad=[ordered]@{
 'missing original area'={param($f)$f.request.ownedPairs.PSObject.Properties.Remove('sourceArea')}
 'source area override'={param($f)$f.request.ownedPairs.sourceArea='e'*32}
 'input already destination'={param($f)$f.request.persistenceLoad.area=$f.request.persistenceAreaTarget.area}
 'second owner omitted before header'={param($f)$f.artifact.observations.areaTransfer.headerStarting.owners=@($f.artifact.observations.areaTransfer.headerStarting.owners[0])}
 'only one native owner restored'={param($f)$f.artifact.observations.areaTransfer.headerStarting.resumes=1}
 'header in source area'={param($f)$f.artifact.observations.areaTransfer.headerStarting.area=$f.request.ownedPairs.sourceArea}
 'foreign requested object at staging'={param($f)$f.artifact.observations.areaTransfer.headerStaged.nativeSaveBoundary.requested=2010}
 'foreign prepared object at completion'={param($f)$f.artifact.observations.ownedOutputCompletion.areaSaveIdentity.prepared=2010}
 'completion aliases requested object'={param($f)$f.artifact.observations.ownedOutputCompletion.areaSaveIdentity.completion=2002}
 'forged explicit native callback'={param($f)$f.artifact.observations.ownedOutputCompletion.callbackCount=1}
 'worker unsettled'={param($f)$f.artifact.observations.ownedOutputCompletion.nativeWorkerSettled=$false}
 'native write wrong destination'={param($f)($f.artifact.observations.ownedOutputCompletion.nativeInventory|Where-Object nativeType -CEQ 'Auto').area=$f.request.ownedPairs.sourceArea}
 'unrelated field actor omitted'={param($f)$f.artifact.observations.areaTransfer.headerStaged.ownedField.actors=@($f.artifact.observations.areaTransfer.headerStaged.ownedField.actors|Select-Object -First 4)}
 'unrelated queued command'={param($f)$f.artifact.observations.areaTransfer.headerStarting.ownedField.actors[4].queuedCommands=1}
 'foreign same-ID actor'={param($f)$f.artifact.observations.areaTransfer.after.ownedField.actors[4].object=2010}
 'owner Game replaced'={param($f)$f.artifact.observations.areaTransfer.after.ownedWorld.game=2010}
 'wrong world authority generation'={param($f)$f.artifact.observations.areaTransfer.headerStaged.ownedWorld.authorityGeneration=2}
 'old scope reused'={param($f)$f.artifact.observations.areaTransfer.headerStarting.ownedWorld.worldScope=7}
 'lost observer custody'={param($f)$f.artifact.observations.areaTransfer.identityRetention.released=$false}
 'retained native objects'={param($f)$f.artifact.observations.areaTransfer.identityRetention.retainedAfterRelease=1}
 'staged native save scope missing'={param($f)$f.artifact.observations.areaTransfer.headerStaged.activeSaveScope=$false}
 'staged second lease missing'={param($f)$f.artifact.observations.areaTransfer.headerStaged.movementAuthorities[1].saveSerializationSuspended=$false}
 'settled lease retained'={param($f)$f.artifact.observations.areaTransfer.after.movementAuthorities[0].saveSerializationSuspended=$true}
 'serialized AI invariant failed'={param($f)$f.artifact.observations.areaTransfer.headerStaged.movementAuthorities[0].invariant='serialized AI mismatch'}
 'extra retained object'={param($f)$f.artifact.observations.areaTransfer.identityRetention.ids+=@(2010)}
 'native Prepare changed'={param($f)$f.artifact.observations.areaTransfer.headerStarting.actors[0].prepared=$true}
 'native debt refund'={param($f)$f.artifact.observations.areaTransfer.headerStarting.actors[3].native.Standard=0}
 'changed inherited money'={param($f)$f.artifact.observations.areaTransfer.headerStarting.ownedField.custody.arrangement.money++}
 'changed inherited second feature'={param($f)$f.artifact.observations.areaTransfer.headerStarting.ownedField.custody.pairs[1].sourceGuid='e'*32}
}
foreach($m in $bad.GetEnumerator()){Check-Area $m.Key {$f=New-AreaCase;&$m.Value $f;$f=Clone-Area $f;Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'} $true}
foreach($kind in @('manual','quick','auto')){Check-Area ('legacy rejects SourceArea '+$kind){$f=Clone-Area (($fixtures|Where-Object name -CEQ ('persistence-save-multi-exploration-'+$kind))[0]);Put-Area $f.request.ownedPairs sourceArea $null;Assert-KmcChunk7OwnedPairsSelection $f.request} $true}
function New-ColdArea {
 $source=New-AreaCase;$cold=Clone-Area (($fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-auto')[0]);$r=$cold.request;$o=$cold.artifact.observations
 $r.runId='synthetic-owned-area-cold';$cold.artifact.runId=$r.runId;$r.persistenceCase='multi-area-autosave';$o.checkpoint=$r.persistenceCase;$o.processId=124
 $r.ownedPairs=Clone-Area $source.request.ownedPairs;$r.ownedPairs.case='persistence-load';$r.ownedPairs.expectedNativeWriteCount=0;$r.ownedPairs.checkpointSourceRunId=$source.request.runId
 Put-Area $r persistenceAreaTarget (Clone-Area $source.request.persistenceAreaTarget);$r.persistenceLoad.area=$r.persistenceAreaTarget.area;$o.loadedDescriptor=Clone-Area $r.persistenceLoad;$o.loadedArchive=Clone-Area $source.artifact.observations.nativeWrite.archive
 $o.inheritedFixture=Clone-Area $source.artifact.observations.inheritedFixture;$o.inheritedCleanup=Clone-Area $source.artifact.observations.inheritedCleanup
 $dir=Join-Path $script:areaLab ('runtime-evidence/'+$source.request.runId);[void][IO.Directory]::CreateDirectory($dir);$p=Join-Path $dir 'chunk7-persistence-evidence.json'
 [IO.File]::WriteAllText($p,($source.artifact|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false));$sha=Get-KmcSha256 $p;$r.ownedPairs.checkpointSourceArtifactSha256=$sha
 [pscustomobject]@{parent=$source;request=$r;artifact=$cold.artifact;source=[pscustomobject]@{checkpointArtifactPath=$p;checkpointArtifactSha256=$sha;checkpointArtifactLength=[long](Get-Item -LiteralPath $p).Length}}
}
Check-Area 'exact destination Auto cold graph with original source area' {$f=New-ColdArea;Assert-KmcChunk7OwnedAreaColdSource $f.source $f.request $f.artifact}
$badCold=[ordered]@{
 'cold same process'={param($f)$f.artifact.observations.processId=$f.parent.artifact.observations.processId}
 'cold wrong descriptor area'={param($f)$f.request.persistenceLoad.area=$f.request.ownedPairs.sourceArea}
 'cold wrong transition point'={param($f)$f.request.persistenceAreaTarget.enterPoint='d'*32}
 'cold wrong source lineage'={param($f)$f.request.ownedPairs.checkpointSourceArtifactSha256='0'*64}
 'cold mutated graph owner'={param($f)$f.artifact.observations.loadedArchive.Pairs[1].Generation++}
 'cold lost second restored owner'={param($f)$f.artifact.observations.snapshots[0].presentationRestores=1}
 'cold native Prepare event'={param($f)$e=Clone-Area $f.artifact.observations.coldRestoration.events[0];$e.kind='prepare-before';$f.artifact.observations.coldRestoration.events+=@($e)}
 'cold inherited feature mismatch'={param($f)$f.artifact.observations.inheritedFixture.pairs[1].rank++;$f.artifact.observations.inheritedCleanup.pairs[1].rank++}
}
foreach($m in $badCold.GetEnumerator()){Check-Area $m.Key {$f=New-ColdArea;&$m.Value $f;Assert-KmcChunk7OwnedAreaColdSource $f.source $f.request $f.artifact} $true}
$passed=@($results|Where-Object pass).Count;$failed=$results.Count-$passed
[IO.File]::WriteAllText((Join-Path $testOutput 'receipt.json'),([pscustomobject]@{status=$(if($failed){'FAIL'}else{'PASS'});passed=$passed;failed=$failed;nativeInvoked=$false;archivesRead=0;scope='actual complete readers on generated native-shaped evidence; no source qualification or native proof';results=@($results)}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
Write-Output ('Owned area reader: '+$passed+' PASS / '+$failed+' FAIL');if($failed){exit 1}
