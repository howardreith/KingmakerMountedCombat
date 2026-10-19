# Optional exact schema2 cold/resave diagnostic. No admission/assertion is relaxed.
Set-StrictMode -Version Latest
function Test-KmcChunk7CombatDebtDiagnosticRequest($Request){
 if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
 if($null-eq$Request-or$null-eq$Request.PSObject.Properties['scenario']-or$null-eq$Request.PSObject.Properties['ownedPairs']-or$null-eq$Request.PSObject.Properties['persistenceCase']){return $false}
 $s=$Request.ownedPairs;if($s-is[Collections.IDictionary]){$s=[pscustomobject]$s}
 if($null-eq$s-or$null-eq$s.PSObject.Properties['case']-or$null-eq$s.PSObject.Properties['expectedNativeLoadCount']-or$null-eq$s.PSObject.Properties['expectedNativeWriteCount']){return $false}
 # This exact owned case maps unconditionally to chunk7-persistence-load in
 # both the production C# selection and existing external selected-scenario map.
 $Request.scenario-is[string]-and$s.case-is[string]-and$Request.persistenceCase-is[string]-and
 $Request.scenario-ceq'chunk7-owned-two-pair-campaign'-and$s.case-ceq'owner-seed-checkpoint-load'-and$Request.persistenceCase-cin@('legacy-mounted-pending-tb-manual','pending-schema3-control-manual')-and
 (Test-KmcExactJsonInteger $s.expectedNativeLoadCount)-and$s.expectedNativeLoadCount-eq1-and(Test-KmcExactJsonInteger $s.expectedNativeWriteCount)-and$s.expectedNativeWriteCount-eq $(if($Request.persistenceCase-ceq'pending-schema3-control-manual'){0}else{1})
}
function Assert-KmcChunk7CombatDebtBoundaryProbe($Request,$Probe,$Archive){
 $selected=Test-KmcChunk7CombatDebtDiagnosticRequest $Request;$has=$null-ne$Probe.PSObject.Properties['debtDiagnostics']
 if(-not$selected){if($has){throw 'Only the exact debt diagnostic request may carry native debt windows.'};return}
 if(-not$has){throw 'Selected debt diagnostic requires its native method window journal.'}
 foreach($spec in @(@('Kingmaker.EntitySystem.Entities.UnitEntityData','JoinCombat','06008355','DebtJoinBefore','DebtJoinAfter'),@('Kingmaker.Controllers.Combat.UnitCombatPrepareController','Tick','0600936F','DebtPrepareBefore','DebtPrepareAfter'))){
  $hook=@($Probe.hooks|Where-Object token -CEQ $spec[2]);if($hook.Count-ne1){throw 'Selected debt native hook is missing or duplicated.'};$hook=$hook[0]
  Assert-KmcExactProperties $hook @('declaringType','method','token','moduleMvid','prefix','postfix') 'selected debt native hook'
  foreach($pair in @(@('declaringType',$spec[0]),@('method',$spec[1]),@('token',$spec[2]),@('prefix',$spec[3]),@('postfix',$spec[4]),@('moduleMvid','07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'))){Assert-KmcBootstrapInventoryString $hook.($pair[0]) $pair[1]}
 }
 $d=$Probe.debtDiagnostics
 $v2=$d.contract-ceq'chunk7-native-debt-boundary-observer-v2'
 Assert-KmcExactProperties $d @('contract','runId','events','maximumEvents','errors','dropped','openCalls','requestUnchanged','complete','scope','methodEntryMayBeRefusedByProductionPrefix') 'native debt window journal'
 $scope=if($Request.persistenceCase-ceq'pending-schema3-control-manual'){'exact-schema3-control-cold-after-presentation'}else{'exact-schema2-cold-after-presentation'}
 if($d.contract-cnotin@('chunk7-native-debt-boundary-observer-v1','chunk7-native-debt-boundary-observer-v2')){throw 'Unknown native debt observer contract.'}
 foreach($spec in @(@('runId',$Request.runId),@('scope',$scope))){Assert-KmcBootstrapInventoryString $d.($spec[0]) $spec[1]}
 Assert-KmcOrdinaryInteger $d.maximumEvents 128 128
 foreach($field in @('errors','dropped','openCalls')){Assert-KmcOrdinaryInteger $d.$field 0 0}
 foreach($field in @('requestUnchanged','complete','methodEntryMayBeRefusedByProductionPrefix')){Assert-KmcChunk7Boolean $d.$field $true}
 if($d.events-isnot[Array]-or$d.events.Count-gt128){throw 'Native debt journal is not a bounded event array.'}
 $ids=@($Archive.Combat.Actors|ForEach-Object{$_.Native.Id});$open=@{};$next=0;$priorFrame=-1L;$priorTicks=-1L
 foreach($e in $d.events){
  $fields=@('kind','edge','callId','actorId','frame','gameTicks','paused','nativePassing','restorePhase','restorationPending','actors')
  if($v2){$fields+=@('nativeRound','currentTurnActorId','callerFrames')}
  Assert-KmcExactProperties $e $fields 'native debt event'
  if($e.kind-isnot[string]-or$e.edge-isnot[string]-or$e.kind-cnotin@('native-join-combat','native-combat-prepare')-or$e.edge-cnotin@('before','after')){throw 'Unknown or nonscalar native debt event kind/edge.'}
  Assert-KmcOrdinaryInteger $e.callId 1 64;Assert-KmcOrdinaryInteger $e.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $e.gameTicks 0 ([long]::MaxValue)
  foreach($field in @('paused','nativePassing')){if($e.$field-isnot[bool]){throw 'Debt event native state flag is not Boolean.'}}
  Assert-KmcChunk7Boolean $e.restorationPending $false;Assert-KmcBootstrapInventoryString $e.restorePhase 'all-pair-presentation-restored'
  if($e.frame-lt$priorFrame-or$e.gameTicks-lt$priorTicks){throw 'Debt event time/frame order regressed.'};$priorFrame=$e.frame;$priorTicks=$e.gameTicks
  if($e.kind-ceq'native-join-combat'){if($e.actorId-isnot[string]-or$e.actorId-cnotin$ids){throw 'Native join event substituted a saved actor.'}}elseif($null-ne$e.actorId){throw 'Controller prepare event invented an actor argument.'}
  if($v2){
   Assert-KmcOrdinaryInteger $e.nativeRound 0 ([int]::MaxValue)
   if($null-ne$e.currentTurnActorId-and($e.currentTurnActorId-isnot[string]-or$e.currentTurnActorId-cnotin$ids)){throw 'Debt event current turn differs from the saved native actor graph.'}
   if($e.callerFrames-isnot[Array]-or$e.callerFrames.Count-gt24-or
      (($e.kind-ceq'native-join-combat'-and$e.edge-ceq'before')-ne($e.callerFrames.Count-gt0))){throw 'Native join caller path is missing, misplaced or unbounded.'}
   foreach($method in $e.callerFrames){if($method-isnot[string]-or$method.Length-lt3-or$method.Length-gt512-or$method-match'[\r\n]'){throw 'Native join caller method identity is invalid.'}}
  }
  if($e.actors-isnot[Array]-or$e.actors.Count-ne$ids.Count){throw 'Debt event omitted a native actor.'}
  for($i=0;$i-lt$ids.Count;$i++){$row=$e.actors[$i];Assert-KmcExactProperties $row @('native','inCombat','prepared') 'debt event actor';Assert-KmcChunk7PersistenceNativeActor $row.native
   Assert-KmcBootstrapInventoryString $row.native.Id $ids[$i];foreach($field in @('inCombat','prepared')){if($row.$field-isnot[bool]){throw 'Debt event actor state is not Boolean.'}}
  }
  $key=[string]$e.callId
  if($e.edge-ceq'before'){
   $next++;if($e.callId-ne$next-or$open.ContainsKey($key)){throw 'Debt call identity duplicated, skipped or reordered.'}
   if($e.kind-ceq'native-combat-prepare'-and@($e.actors|Where-Object{$_.inCombat-and-not$_.prepared}).Count-eq0){throw 'Filtered empty prepare tick fabricated a native window.'}
   $open[$key]=$e
  }else{
   if(-not$open.ContainsKey($key)){throw 'Debt native postfix has no exact prefix.'};$before=$open[$key]
   if($before.kind-cne$e.kind-or$before.actorId-cne$e.actorId){throw 'Debt native call changed kind/actor across its window.'};$open.Remove($key)
  }
 }
 if($open.Count-ne0){throw 'Debt native prefix has no matching postfix.'}
}
function Assert-KmcChunk7CombatDebtChecks($Request,$Observations){
 $selected=Test-KmcChunk7CombatDebtDiagnosticRequest $Request;$has=$null-ne$Observations.PSObject.Properties['combatDebtChecks']
 if(-not$selected){if($has){throw 'Only the selected diagnostic may carry combat debt checks.'};return}
 $count=if($Request.persistenceCase-ceq'pending-schema3-control-manual'){2}else{5}
 if(-not$has-or$Observations.combatDebtChecks-isnot[Array]-or$Observations.combatDebtChecks.Count-ne$count){throw 'Pending-only cold load requires its exact before-bind and assertion snapshots.'}
 $fields=@('Standard','Move','Swift','Initiative','Reaction','ReactionsRemaining','LastSurpriseTicks');$priorFrame=-1L;$priorTicks=-1L
 for($j=0;$j-lt$count;$j++){
  $record=$Observations.combatDebtChecks[$j];$archive=if($j-eq4){$Observations.nativeWrite.archive}else{$Observations.loadedArchive}
  Assert-KmcExactProperties $record @('contract','phase','frame','gameTicks','savedGameTicks','elapsedSeconds','windowAccepted','actors','passed','paused','archiveRestorePhase','restorationPending','actorStates') 'combat debt comparison'
  Assert-KmcBootstrapInventoryString $record.contract 'chunk7-native-combat-debt-observation-v1';Assert-KmcBootstrapInventoryString $record.phase $(if($j-eq0){'cold-observe-before-binding'}else{'assert-combat-debt'})
  Assert-KmcBootstrapInventoryString $record.archiveRestorePhase 'all-pair-presentation-restored';Assert-KmcChunk7Boolean $record.restorationPending $false
  Assert-KmcOrdinaryInteger $record.frame 0 ([int]::MaxValue);Assert-KmcOrdinaryInteger $record.gameTicks 0 ([long]::MaxValue);Assert-KmcOrdinaryInteger $record.savedGameTicks $archive.GameTimeTicks $archive.GameTimeTicks
  if($record.paused-isnot[bool]-or$record.frame-lt$priorFrame-or$record.gameTicks-lt$priorTicks){throw 'Combat debt observation flag/order differs.'};$priorFrame=$record.frame;$priorTicks=$record.gameTicks
  # Subtract exact integer ticks before conversion, matching the C# assertion.
  $elapsed=[double]([decimal]$record.gameTicks-[decimal]$record.savedGameTicks)/10000000.0
  if(-not(Test-KmcChunk7FiniteNumber $record.elapsedSeconds)-or[Math]::Abs([double]$record.elapsedSeconds-$elapsed)-gt0.000001-or$elapsed-lt0-or$elapsed-gt30){throw 'Combat debt observation escaped unchanged clock window.'}
  foreach($flag in @('windowAccepted','passed')){Assert-KmcChunk7Boolean $record.$flag $true}
  $saved=@($archive.Combat.Actors|ForEach-Object{$_.Native})
  if($record.actors-isnot[Array]-or$record.actors.Count-ne$saved.Count-or$record.actorStates-isnot[Array]-or$record.actorStates.Count-ne$saved.Count){throw 'Debt comparison omitted complete actor field/state rows.'}
  for($i=0;$i-lt$saved.Count;$i++){
   $row=$record.actors[$i];$state=$record.actorStates[$i];Assert-KmcExactProperties $row @('actorId','saved','actual','fields','passed') 'combat debt actor comparison';Assert-KmcExactProperties $state @('actorId','inCombat','prepared') 'combat debt actor state'
   foreach($value in @($row.actorId,$state.actorId)){Assert-KmcBootstrapInventoryString $value $saved[$i].Id};foreach($flag in @('inCombat','prepared')){if($state.$flag-isnot[bool]){throw 'Debt actor flag is not native Boolean.'}}
   Assert-KmcChunk7PersistenceNativeActor $row.saved;Assert-KmcChunk7PersistenceNativeActor $row.actual;Assert-KmcBootstrapInventoryString $row.actual.Id $saved[$i].Id
   if((ConvertTo-KmcChunk7Canonical $row.saved)-cne(ConvertTo-KmcChunk7Canonical $saved[$i])){throw 'Debt comparison substituted saved native baseline.'}
   if($row.fields-isnot[Array]-or$row.fields.Count-ne7){throw 'Debt comparison omitted one of seven native fields.'};Assert-KmcChunk7Boolean $row.passed $true
   for($k=0;$k-lt7;$k++){
    $f=$row.fields[$k];$name=$fields[$k];Assert-KmcExactProperties $f @('field','saved','actual','passed') 'native debt field comparison';Assert-KmcBootstrapInventoryString $f.field $name
    if($k-lt5){
     if(-not(Test-KmcChunk7FiniteNumber $f.saved)-or-not(Test-KmcChunk7FiniteNumber $f.actual)-or[double]$f.saved-ne[double]$row.saved.$name-or[double]$f.actual-ne[double]$row.actual.$name){throw 'Debt field attribution differs from its native rows.'}
    }elseif(-not(Test-KmcExactJsonInteger $f.saved)-or-not(Test-KmcExactJsonInteger $f.actual)-or[decimal]$f.saved-ne[decimal]$row.saved.$name-or[decimal]$f.actual-ne[decimal]$row.actual.$name){throw 'Integer debt field attribution differs from its exact native rows.'}
    $ok=if($k-lt5){
     # SavedNativeActor stores Single. Reconstruct that native value before the
     # C# predicate widens it to Double; its shortest JSON decimal is not exact.
     $nativeSaved=[double][single]$f.saved;$nativeActual=[double][single]$f.actual
     $nativeActual-le($nativeSaved+0.001)-and($nativeActual+$elapsed*1.1+0.001)-ge$nativeSaved
    }else{[decimal]$f.actual-eq[decimal]$f.saved}
    if(-not$ok){throw 'Passing diagnostic violates the unchanged native debt predicate.'};Assert-KmcChunk7Boolean $f.passed $true
   }
  }
 }
}
