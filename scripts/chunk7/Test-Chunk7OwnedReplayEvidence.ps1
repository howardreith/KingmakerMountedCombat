[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Execute the unchanged legacy repeated-world matrix first, then the new exact
# inherited branch against generated JSON only. No archive or native operation.
. (Join-Path $PSScriptRoot 'Test-Chunk7RepeatedWorldEvidence.ps1')
$legacyPassed=$passed
if(-not$OutputDirectory){$OutputDirectory=Join-Path $chunk7AnalysisRoot ('owned-replay-reader-'+[Guid]::NewGuid().ToString('N'))};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$clock=[Diagnostics.Stopwatch]::StartNew();$ownedResults=[Collections.Generic.List[object]]::new()
$seed=@(([IO.File]::ReadAllText((Join-Path $chunk7TestSource 'OwnedPairsEvidenceFixtures.json'))|ConvertFrom-Json).fixtures|Where-Object name -CEQ 'persistence-load-multi-exploration-manual')[0]
$co=Copy-Test $seed.artifact.observations;$archive=Copy-Test $co.loadedArchive;$native=@($archive.Pairs|ForEach-Object{$_.Rider;$_.Mount});$slots=Copy-Test $archive.Slots;$first=$native[0].Id;$ids=@($native|ForEach-Object Id)
$owners=@($archive.Pairs|ForEach-Object{[pscustomobject]@{ownerId=$_.PairId;generation=$_.Generation;riderId=$_.Rider.Id;mountId=$_.Mount.Id;state='Active'}})
$extra='50505050-5050-4050-8050-505050505050';$baseCustody=Copy-Test $co.inheritedFixture
$baseCustody.partyIds+=@($extra);$baseCustody.currentPartyIds+=@($extra);$baseCustody.currentCharacterIds+=@($extra)
$other=Copy-Test $baseCustody.arrangement.actors[0];$other.actorId=$extra;$other.blueprint='c'*32;$baseCustody.arrangement.actors+=@($other)
function Add-OwnedField($World){
 $load=[int]$World.nativeDisposals+1;$areaOrdinal=[int]$World.areaSuspensions
 $custody=Copy-Test $baseCustody;$custody.phase='replay-field';$actors=@();$characters=@();$labels=@()
 for($i=0;$i-lt5;$i++){
  $row=$baseCustody.arrangement.actors[$i];$v=if($i-lt4){$World.actors[$i]}else{[pscustomobject]@{object=$load*100+41;descriptor=$load*100+42;view=$load*100+43;viewObject=$load*100+44}}
  $actors+=[pscustomobject]@{id=$row.actorId;blueprint=$row.blueprint;object=$v.object;descriptor=$v.descriptor;viewObject=$v.viewObject;view=$v.view;petId=$(if($i-cin@(0,2)){$ids[$i+1]}else{$null});masterId=$(if($i-cin@(1,3)){$ids[$i-1]}else{$null});inState=$true;inGame=$true;inCombat=$false;commandsEmpty=$true;queuedCommands=0}
  if($i-cin@(0,2,4)){$characters+=[pscustomobject]@{id=$row.actorId;object=$v.object}}
 }
 for($i=0;$i-lt2;$i++){$p=$custody.pairs[$i];$p.rider=$World.actors[$i*2].object;$p.mount=$World.actors[$i*2+1].object;$p.feature=$load*10000+$i*10+1;$p.rankFact=$load*10000+$i*10+2;foreach($key in @('rider','mount','feature','rankFact')){$labels+=@($p.$key)}}
 $custody.identities.ids=@($labels|Sort-Object)
 $World|Add-Member NoteProperty ownedField ([pscustomobject]@{loadOrdinal=$load;areaOrdinal=$areaOrdinal;custody=$custody;area=$World.area;actors=$actors;characters=$characters})
}
function Change-OwnedReplayEpoch($Fixture,[string]$Kind){
 $r=$Fixture.artifact.observations.repeatedWorlds;$before=$r.worlds[0];$next=$r.worlds[1]
 switch($Kind){
  'actor'{$next.ownedField.actors[4].object=$before.ownedField.actors[0].object;$next.ownedField.characters[2].object=$next.ownedField.actors[4].object}
  'descriptor'{$next.actors[2].descriptor=$before.actors[0].descriptor;$next.ownedField.actors[2].descriptor=$next.actors[2].descriptor}
  'view'{$next.actors[2].viewObject=$before.actors[0].viewObject;$next.actors[2].view=$before.actors[0].view;$next.ownedField.actors[2].viewObject=$next.actors[2].viewObject;$next.ownedField.actors[2].view=$next.actors[2].view}
  'feature'{$old=$next.ownedField.custody.pairs[1].feature;$value=$before.ownedField.custody.pairs[0].feature;$next.ownedField.custody.pairs[1].feature=$value;$next.ownedField.custody.identities.ids=@($next.ownedField.custody.identities.ids|ForEach-Object{if($_-eq$old){$value}else{$_}}|Sort-Object)}
  'rankFact'{$old=$next.ownedField.custody.pairs[1].rankFact;$value=$before.ownedField.custody.pairs[0].rankFact;$next.ownedField.custody.pairs[1].rankFact=$value;$next.ownedField.custody.identities.ids=@($next.ownedField.custody.identities.ids|ForEach-Object{if($_-eq$old){$value}else{$_}}|Sort-Object)}
  default{throw 'Unknown exact parity mutation.'}
 }
 $r.requests[1].before=Copy-Test $next
 # The native disposal callback belongs to this prior world too. Update only
 # exact duplicated descriptor/view identity witnesses, preserving phase flags.
 if($Kind-ceq'descriptor'){$r.retention.events[1].state.actors[2].descriptor=$next.actors[2].descriptor}
 if($Kind-ceq'view'){$r.retention.events[1].state.actors[2].viewObject=$next.actors[2].viewObject;$r.retention.events[1].state.actors[2].view=$next.actors[2].view}
 $r.finalRetention=Copy-Test $r.retention
 $Fixture.artifact.observations.inheritedReplayRetirements[1].previousIds=@($next.ownedField.custody.identities.ids|ForEach-Object{[int]$_})
}
function Change-OwnedReplayOrder($Fixture,[string]$Kind){
 $r=$Fixture.artifact.observations.repeatedWorlds
 if($Kind-ceq'cleanup-order'){$values=@($Fixture.artifact.observations.inheritedCleanup.partyIds);[array]::Reverse($values);$Fixture.artifact.observations.inheritedCleanup.partyIds=$values;return}
 $field=switch($Kind){
  'initial-order'{$r.worlds[0].ownedField}
  'dispatch-order'{$r.requests[1].before.ownedField}
  'last-order'{$r.worlds[2].ownedField}
  default{$r.worlds[1].ownedField}
 }
 if($Kind-ceq'missing-id'){$field.custody.partyIds=@($field.custody.partyIds|Select-Object -Skip 1);return}
 if($Kind-ceq'duplicate-id'){$field.custody.partyIds[0]=$field.custody.partyIds[1];return}
 $byId=@{};foreach($actor in $field.actors){$byId[$actor.id]=$actor};$characters=@($field.characters|ForEach-Object id);[array]::Reverse($characters)
 $field.custody.partyIds=@($characters)+@(foreach($id in $characters){if($null-ne$byId[$id].petId){$byId[$id].petId}})
 if($Kind-cin@('later-order','area-order')){$r.requests[1].before=Copy-Test $r.worlds[1]}
 if($Kind-ceq'last-order'){$r.settled.ownedField=Copy-Test $field;$Fixture.artifact.observations.inheritedCleanup.partyIds=@($field.custody.partyIds|ForEach-Object{[string]$_})}
}
function New-OwnedReplay([bool]$Areas){
 $q=Copy-Test $seed.request;$q.ownedPairs.partyIds+=@($extra);$q.persistenceCase=if($Areas){'multi-exploration-three-area-transfers-manual'}else{'multi-exploration-three-loads-manual'};$q.ownedPairs.expectedNativeLoadCount=if($Areas){1}else{3}
 # The owned area is deliberately distinct from historical Working.
 $q.persistenceLoad.area='e'*32;$archive.AreaId=$q.persistenceLoad.area
 if($Areas){$q|Add-Member NoteProperty persistenceReplayTargets @(for($i=0;$i-lt3;$i++){[pscustomobject]@{enterPoint=@('1','2','3')[$i]*32;area=@('b','c','d')[$i]*32;autoSaveMode='None'}})}
 $a=New-ReplayArtifact $q $Areas;$o=$a.observations;$r=$o.repeatedWorlds;$o.sourceWorkingSha256=$null;$o.nativeProgressionRequiresExternalWorkingRestoration=$false
 $all=@($r.worlds)+@($r.requests|ForEach-Object{$_.before})+@($r.retention.events|ForEach-Object{$_.state})+@($r.settled)
 foreach($s in $all){if($s.area-ceq('a'*32)){$s.area=$q.persistenceLoad.area}}
 $r.finalRetention=Copy-Test $r.retention
 foreach($s in @($r.worlds)+@($r.requests|ForEach-Object{$_.before})+@($r.settled)){Add-OwnedField $s}
 $o.inheritedFixture=Copy-Test $r.worlds[0].ownedField.custody;$o.inheritedFixture.phase='admitted';$o.inheritedCleanup=Copy-Test $r.settled.ownedField.custody;$o.inheritedCleanup.phase='cleanup'
 $retired=@();if(-not$Areas){for($i=0;$i-lt2;$i++){$old=$r.worlds[$i];$next=$r.worlds[$i+1];$retired+=[pscustomobject]@{fromLoad=$i+1;toLoad=$i+2;oldPlayer=$old.playerObject;newPlayer=$next.playerObject;oldState=$old.stateObject;newState=$next.stateObject;previousIds=(Copy-Test $old.ownedField.custody.identities.ids);capacity=128;factRemovalCount=0;nativeDeletionRequested=$false;released=$true;retainedAfterRelease=0}}}
 $o|Add-Member NoteProperty inheritedReplayRetirements $retired
 $global=@($r.identityRetention.ids);foreach($w in @($r.worlds)+@($r.settled)){$f=$w.ownedField;$global+=@($f.custody.identities.ids);foreach($actor in $f.actors){foreach($key in @('object','descriptor','viewObject')){$global+=@($actor.$key)}}};$r.identityRetention.ids=@($global|Sort-Object -Unique)
 [pscustomobject]@{request=$q;artifact=$a}
}
function OwnedCheck([string]$Name,[scriptblock]$Body,[bool]$Refuse=$false,[string]$Pattern=''){$errorText=$null;$ok=$false;try{&$Body;$ok=-not$Refuse}catch{$errorText=$_.Exception.Message;$ok=$Refuse-and(-not$Pattern-or$errorText-match$Pattern)};$ownedResults.Add([pscustomobject]@{name=$Name;pass=$ok;expectedRefusal=$Refuse;error=$errorText});if(-not$ok){Write-Output ('FAIL '+$Name+': '+$errorText)}}
function Read-Owned($f){Assert-KmcChunk7PersistenceObservation $f.request $f.artifact 'PASS'}
$ownedLoad=New-OwnedReplay $false;$ownedAreas=New-OwnedReplay $true
foreach($pair in @(@('load',$ownedLoad),@('area',$ownedAreas))){$name=$pair[0];$f=$pair[1];OwnedCheck ('complete owned '+$name) {Read-Owned $f}}
foreach($orderCase in @('later-order','last-order')){OwnedCheck ('fresh load roster order '+$orderCase){$f=Copy-Test $ownedLoad;Change-OwnedReplayOrder $f $orderCase;Read-Owned $f}}
foreach($orderCase in @('initial-order','dispatch-order','cleanup-order','missing-id','duplicate-id')){OwnedCheck ('exact roster witness refuses '+$orderCase){$f=Copy-Test $ownedLoad;Change-OwnedReplayOrder $f $orderCase;Read-Owned $f} $true}
OwnedCheck 'area retained roster order remains exact' {$f=Copy-Test $ownedAreas;Change-OwnedReplayOrder $f 'area-order';Read-Owned $f} $true
OwnedCheck 'old Working area is not the owned initial route origin' {$q=Copy-Test $ownedAreas.request;$q.persistenceReplayTargets[0].area=$q.fixture.working.area;Assert-KmcChunk7RepeatedWorldRequest $q}
$mutations=[ordered]@{
 'loaded count shrank'={param($f)$f.request.ownedPairs.expectedNativeLoadCount=1}
 'load became save source'={param($f)$f.request.ownedPairs.case='persistence-save'}
 'unsupported lineage parent missing'={param($f)$f.request.ownedPairs.checkpointSourceRunId=$null}
 'inherited source pin changed'={param($f)$f.artifact.observations.inheritedManualSha256='0'*64}
 'claims historical Working'={param($f)$f.artifact.observations.sourceWorkingSha256=$f.request.fixture.working.sha256}
 'unrelated busy actor'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.actors[4].commandsEmpty=$false}
 'unrelated queued actor'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.actors[4].queuedCommands=1}
 'unrelated out of state'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.actors[4].inState=$false}
 'unrelated out of game'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.actors[4].inGame=$false}
 'unrelated entered combat'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.actors[4].inCombat=$true}
 'unrelated stale actor'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.actors[4].object=$r.worlds[0].ownedField.actors[4].object}
 'cross-role prior actor reused'={param($f)Change-OwnedReplayEpoch $f 'actor'}
 'cross-role prior descriptor reused'={param($f)Change-OwnedReplayEpoch $f 'descriptor'}
 'cross-role prior view reused'={param($f)Change-OwnedReplayEpoch $f 'view'}
 'cross-role prior feature reused'={param($f)Change-OwnedReplayEpoch $f 'feature'}
 'cross-role prior rank reused'={param($f)Change-OwnedReplayEpoch $f 'rankFact'}
 'raw character removed'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.characters=@($r.worlds[1].ownedField.characters|Select-Object -First 2)}
 'pet inserted into raw characters'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.characters[2].id=$f.request.ownedPairs.mountBId;$r.worlds[1].ownedField.characters[2].object=$r.worlds[1].ownedField.actors[3].object}
 'lost reciprocal pet'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.actors[0].petId=$null}
 'feature reused across load'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.custody.pairs[0].feature=$r.worlds[0].ownedField.custody.pairs[0].feature}
 'whole-field registry absent'={param($f)$r=$f.artifact.observations.repeatedWorlds;$id=$r.worlds[1].ownedField.actors[4].object;$r.identityRetention.ids=@($r.identityRetention.ids|Where-Object{$_-ne$id})}
 'world actor label collision'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.actors[4].object=$r.worlds[1].stateObject}
 'class XP changed'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.custody.arrangement.actors[4].experience++}
 'dispatch substituted full field'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.requests[0].before.ownedField.actors[4].view=777}
 'selection escaped full field'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].selectionRebinding[0].object=$r.worlds[0].actors[0].object}
 'missing local retirement'={param($f)$f.artifact.observations.inheritedReplayRetirements=@()}
 'wrong retirement epoch'={param($f)$f.artifact.observations.inheritedReplayRetirements[0].oldPlayer=$f.artifact.observations.repeatedWorlds.worlds[1].playerObject}
 'old registry retained'={param($f)$f.artifact.observations.inheritedReplayRetirements[0].retainedAfterRelease=1}
 'native deletion claimed'={param($f)$f.artifact.observations.inheritedReplayRetirements[0].nativeDeletionRequested=$true}
 'fact removed'={param($f)$f.artifact.observations.inheritedReplayRetirements[0].factRemovalCount=1}
 'cleanup uses first epoch'={param($f)$f.artifact.observations.inheritedCleanup.pairs=Copy-Test $f.artifact.observations.inheritedFixture.pairs}
 'final full field changed'={param($f)$f.artifact.observations.repeatedWorlds.settled.ownedField.actors[4].view=666}
 'checkpoint scalar array'={param($f)$f.artifact.observations.checkpoint=@($f.artifact.observations.checkpoint)}
 'pet scalar array'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.actors[0].petId=@($r.worlds[1].ownedField.actors[0].petId)}
 'master scalar array'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.actors[1].masterId=@($r.worlds[1].ownedField.actors[1].masterId)}
 'blueprint scalar array'={param($f)$r=$f.artifact.observations.repeatedWorlds;$r.worlds[1].ownedField.actors[0].blueprint=@($r.worlds[1].ownedField.actors[0].blueprint)}
 'load ordinal text'={param($f)$f.artifact.observations.repeatedWorlds.worlds[1].ownedField.loadOrdinal='2'}
 'retirement identity zero'={param($f)$f.artifact.observations.inheritedReplayRetirements[0].oldPlayer=0}
}
foreach($m in $mutations.GetEnumerator()){OwnedCheck $m.Key {$f=Copy-Test $ownedLoad;&$m.Value $f;Read-Owned $f} $true $(if($m.Key.StartsWith('cross-role ')){'previous native epoch'}else{''})}
foreach($name in @('area view replaced','area fact replaced','area new retirement','area origin reused','area mode array','area callback old object','area pending destroy foreign','area unsettled queue')){OwnedCheck $name {$f=Copy-Test $ownedAreas;$r=$f.artifact.observations.repeatedWorlds;switch($name){
 'area view replaced'{$r.worlds[1].ownedField.actors[4].view=999}
 'area fact replaced'{$r.worlds[1].ownedField.custody.pairs[0].feature=999}
 'area new retirement'{$f.artifact.observations.inheritedReplayRetirements=@($ownedLoad.artifact.observations.inheritedReplayRetirements[0])}
 'area origin reused'{$f.request.persistenceReplayTargets[0].area=$f.request.persistenceLoad.area}
 'area mode array'{$f.request.persistenceReplayTargets[0].autoSaveMode=@('None')}
 'area callback old object'{$r.retention.events[1].state.stateObject=222;$r.finalRetention=Copy-Test $r.retention}
 'area pending destroy foreign'{$r.retention.events[0].state.actors[0].overrides=@(999);$r.finalRetention=Copy-Test $r.retention}
 'area unsettled queue'{$r.requests[0].before.ownedField.actors[4].queuedCommands=1}
 };Read-Owned $f} $true}
function Relabel-Native($Value){
 if($null-eq$Value){return};if($Value-is[Array]){foreach($v in $Value){Relabel-Native $v};return};if($Value-isnot[pscustomobject]){return}
 foreach($p in @($Value.PSObject.Properties)){
  if($p.Name-cin@('object','descriptor','viewObject','view','agentOverride','identity','gameObject','playerObject','stateObject','areaStateObject','mainStateObject','rider','mount','feature','rankFact','oldPlayer','newPlayer','oldState','newState')-and(Test-KmcExactJsonInteger $p.Value)-and$p.Value-ne0){$p.Value=-100000-[int]$p.Value}
  elseif($p.Name-cin@('ids','previousIds','overrides')-and$p.Value-is[Array]-and@($p.Value|Where-Object{-not(Test-KmcExactJsonInteger $_)}).Count-eq0){$p.Value=@($p.Value|ForEach-Object{if($_-eq0){0}else{-100000-[int]$_}})}
  else{Relabel-Native $p.Value}
 }
}
foreach($a in @($false,$true)){OwnedCheck ('graph-preserving signed labels '+$a){$f=Copy-Test $(if($a){$ownedAreas}else{$ownedLoad});Relabel-Native $f;Read-Owned $f}}
$clock.Stop();$ok=@($ownedResults|Where-Object pass).Count;$bad=$ownedResults.Count-$ok
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'receipt.json'),([pscustomobject]@{legacyPassed=$legacyPassed;passed=$ok;failed=$bad;elapsedSeconds=$clock.Elapsed.TotalSeconds;nativeInvoked=$false;archivesRead=0;cases=$ownedResults}|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'load-fixture.json'),($ownedLoad|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText((Join-Path $OutputDirectory 'area-fixture.json'),($ownedAreas|ConvertTo-Json -Depth 90),[Text.UTF8Encoding]::new($false))
Write-Output ('OWNED REPLAY READERS legacy='+$legacyPassed+' PASS='+$ok+' FAIL='+$bad);if($bad-ne0){throw 'Owned replay real-reader regression failed.'}
