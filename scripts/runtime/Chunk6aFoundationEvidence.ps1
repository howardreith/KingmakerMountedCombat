# Chunk 6A foundation readers: the five lifecycle/persistence foundation cases of the owner's
# 6A development-exit decision. A settled real-time combat Mount and Dismount (P04 checkpoints
# combat-mount-rt, combat-dismount-rt) and a settled turn-based combat Mount on the rider's
# own Acting turn (P02 checkpoint combat-mount-tb), each saved through the unchanged Chunk 5
# owned manual archive and reopened cold; and the two lifecycle boundaries during the exact
# Mount approach (native combat end; registered disable then re-enable). This file is the only
# acceptance authority for those cases: the compiled scenarios record facts and check structure,
# this reader decides. Read-only over immutable artifacts; no runtime mutation.
Set-StrictMode -Version Latest
function Get-KmcChunk6aFoundationCases { @('combat-mount-rt','combat-dismount-rt','combat-mount-tb') }
function Test-KmcChunk6aFoundationCase([string]$Case) { [string]$Case -cin (Get-KmcChunk6aFoundationCases) }
function Test-KmcChunk6aFoundationScenario([string]$Scenario) { [string]$Scenario -cmatch '^persistence-p0[24]-(save|load)$' }
function Get-KmcChunk6aLifecycleScenarios { @('chunk6a-combat-end-approach','chunk6a-disable-approach') }
function Get-KmcChunk6aFoundationIdMap {
 [ordered]@{
  'CM07-mount-save-rt'=@('persistence-p04-save','combat-mount-rt')
  'CM07-mount-load-rt'=@('persistence-p04-load','combat-mount-rt')
  'CM07-dismount-save'=@('persistence-p04-save','combat-dismount-rt')
  'CM07-dismount-load'=@('persistence-p04-load','combat-dismount-rt')
  'CM07-mount-save-tb'=@('persistence-p02-save','combat-mount-tb')
  'CM07-mount-load-tb'=@('persistence-p02-load','combat-mount-tb')
 }
}
function Get-KmcChunk6aFoundationLoadSources {
 @{'CM07-mount-load-rt'='CM07-mount-save-rt';'CM07-dismount-load'='CM07-dismount-save';'CM07-mount-load-tb'='CM07-mount-save-tb'}
}
function Get-KmcChunk6aFoundationRowName([string]$Scenario,[string]$Case) {
 if(-not(Test-KmcChunk6aFoundationScenario $Scenario)-or-not(Test-KmcChunk6aFoundationCase $Case)){throw 'Chunk 6A foundation: a row name requires a persistence foundation scenario and checkpoint'}
 $m=[regex]::Match($Scenario,'^persistence-p0([24])-(save|load)$')
 'P0'+$m.Groups[1].Value+'-'+$m.Groups[2].Value+'-'+$Case
}
function FoundationFail([string]$Message) { throw ('Chunk 6A foundation: '+$Message) }
function FoundationProp($Object,[string]$Name) { if($null-ne$Object-and$null-ne$Object.PSObject.Properties[$Name]){$Object.$Name}else{$null} }
function FoundationNear($A,$B,[double]$Tolerance=0.01) {
 if($null-eq$A-or$null-eq$B){return $false}
 $a=[double]$A;$b=[double]$B
 if([double]::IsNaN($a)-or[double]::IsInfinity($a)-or[double]::IsNaN($b)-or[double]::IsInfinity($b)){return $false}
 [Math]::Abs($a-$b)-le$Tolerance
}
# The foundation observation a row carries: directly, inside a combat observation, or inside a
# real-time 'actual' observation.
function Get-KmcChunk6aFoundationOf($Row) {
 $d=FoundationProp $Row 'detail'
 if($null-eq$d){return $null}
 $f=FoundationProp $d 'foundation';if($null-ne$f){return $f}
 $combat=FoundationProp $d 'combat';if($null-ne$combat){$f=FoundationProp $combat 'foundation';if($null-ne$f){return $f}}
 $actual=FoundationProp $d 'actual';if($null-ne$actual){$f=FoundationProp $actual 'foundation';if($null-ne$f){return $f}}
 $null
}
function Get-KmcChunk6aFoundationDebtOf($Row) {
 $d=FoundationProp $Row 'detail'
 if($null-eq$d){return $null}
 $debt=FoundationProp $d 'debt';if($null-ne$debt){return $debt}
 $null
}

# ---------------------------------------------------------------------------------------------
# Persistence: the complete acceptance of one foundation save or cold-load process.
# ---------------------------------------------------------------------------------------------
function Assert-KmcChunk6aFoundationPersistenceEvidence {
 param($Request,$Rows,$GameResult)
 $case=[string](FoundationProp $Request 'persistenceCase');$scenario=[string]$Request.scenario
 if(-not(Test-KmcChunk6aFoundationCase $case)-or-not(Test-KmcChunk6aFoundationScenario $scenario)){FoundationFail 'the request is not a foundation case'}
 $save=$scenario.EndsWith('-save',[StringComparison]::Ordinal);$rt=$scenario.StartsWith('persistence-p04-',[StringComparison]::Ordinal);$tb=-not$rt
 if($tb-and$case-cne'combat-mount-tb'){FoundationFail 'the turn-based foundation requires combat-mount-tb'}
 if($rt-and$case-ceq'combat-mount-tb'){FoundationFail 'the real-time foundation cannot run the turn-based checkpoint'}
 $mountCase=$case-cne'combat-dismount-rt'
 $rows=@($Rows)
 if($rows.Count-lt6-or$rows.Count-gt24){FoundationFail 'row count is out of bounds'}
 $initial=@($rows|Where-Object kind -CEQ 'initial');if($initial.Count-ne1){FoundationFail 'no unique initial row'}
 $riderId=[string]$initial[0].rider.Id;$mountId=[string]$initial[0].mount.Id
 if([string]::IsNullOrEmpty($riderId)-or[string]::IsNullOrEmpty($mountId)-or$riderId-ceq$mountId){FoundationFail 'initial row lacks the exact pair'}
 foreach($r in $rows){
  if($r.runId-cne$Request.runId-or$r.scenario-cne$scenario-or$r.source-cne$Request.commit-or$r.dll-cne$Request.dllSha256-or$r.processId-ne$GameResult.processId-or[string]$r.checkpoint-cne$case){FoundationFail ('row identity differs at '+$r.kind)}
  if($null-eq$r.controls-or$r.controls.DuplicateFactCount-ne0){FoundationFail ('a duplicate owned control exists at '+$r.kind)}
  if($rt){ if($r.native.tbSetting-ne$false-or$r.native.tbInitialized-ne$false){FoundationFail ('a real-time row ran in turn-based mode at '+$r.kind)} }
  else { if($r.native.tbSetting-ne$true){FoundationFail ('a turn-based row ran without the turn-based setting at '+$r.kind)} }
  if($null-eq$r.rider-or$null-eq$r.mount-or[string]$r.rider.Id-cne$riderId-or[string]$r.mount.Id-cne$mountId){FoundationFail ('row actors differ at '+$r.kind)}
 }
 $kinds=@($rows|ForEach-Object {[string]$_.kind})
 $clickKind=if($rt){if($mountCase){'rt-combat-mount-click'}else{'rt-combat-dismount-click'}}else{'combat-mount-click'}
 $settledKind=if($rt){if($mountCase){'rt-combat-mount-settled'}else{'rt-combat-dismount-settled'}}else{'combat-mount-settled'}
 $savedState=if($mountCase){'Mounted'}else{'Unmounted'};$sourceState=if($mountCase){'Unmounted'}else{'Mounted'}
 if($save){
  $clickIndex=[Array]::IndexOf($kinds,$clickKind);$settledIndex=[Array]::IndexOf($kinds,$settledKind)
  if($clickIndex-lt1-or$settledIndex-le$clickIndex){FoundationFail 'the click and settled rows are missing or out of order'}
  for($i=0;$i-lt$rows.Count;$i++){
   $state=[string]$rows[$i].relationship
   if($i-lt$clickIndex){ if($state-cne$sourceState){FoundationFail ('the relationship before the click is '+$state+' at '+$kinds[$i])} }
   elseif($i-ge$settledIndex){ if($state-cne$savedState){FoundationFail ('the relationship after settlement is '+$state+' at '+$kinds[$i])} }
   elseif($state-cnotin @($sourceState,$savedState)){FoundationFail ('the relationship between click and settlement is '+$state)}
  }
 } else {
  foreach($r in $rows){ if([string]$r.relationship-cne$savedState){FoundationFail ('a cold row relationship is '+$r.relationship+' at '+$r.kind)} }
 }
 $required=if($rt){
  if($save){@('initial','rt-foundation-combat-ready',$clickKind,$settledKind,'rt-before-save','rt-native-save-requested','native-write-complete','rt-foundation-continuation','rt-approach-first-delivery','rt-spent-attack-queued','rt-native-debt-wait','usable-continuation-complete')}
  else{@('initial','rt-cold-debt-restored','rt-foundation-continuation','rt-approach-first-delivery','rt-spent-attack-queued','rt-native-debt-wait','usable-continuation-complete')}
 } else {
  if($save){@('initial','combat-mount-acting-entry-dispatched','combat-mount-acting-entry-completed',$clickKind,$settledKind,'native-write-complete','attack-dispatched','attack-delivered','usable-continuation-complete')}
  else{@('initial','attack-dispatched','attack-delivered','usable-continuation-complete')}
 }
 foreach($k in $required){ if(@($rows|Where-Object kind -CEQ $k).Count-ne1){FoundationFail ('missing or repeated row '+$k)} }
 if($rt){ if(@($rows|Where-Object kind -CEQ 'rt-later-attack').Count-ne2){FoundationFail 'two later native attacks are required'} }
 else { if(@($rows|Where-Object kind -CEQ 'next-paired-activation').Count-ne2){FoundationFail 'two successive paired activations are required'} }
 if(@($rows|Where-Object kind -CIn @('assertion-failed','scenario-failed')).Count-ne0){FoundationFail 'the process recorded a failure'}
 if($save){
  $click=@($rows|Where-Object kind -CEQ $clickKind)[0];$settled=@($rows|Where-Object kind -CEQ $settledKind)[0]
  $c=$click.detail.click
  if($c.clicked-ne$true-or$c.nativeCastRequestDelta-ne1-or$c.nativeRefusalDelta-ne0-or$c.dispatchRejectedDelta-ne0){FoundationFail 'the native click was not admitted exactly once'}
  if([string]$c.abilityGuid-cne$(if($mountCase){'f053faad986631688defa003cd7bda0e'}else{'3af2b81f4d72bbb30501fa730fcdf36e'})){FoundationFail 'the click ability differs'}
  if([string]$c.clickedTargetId-cne$(if($mountCase){$mountId}else{$riderId})){FoundationFail 'the click target differs'}
  if($c.availableForCast-ne$true-or@($c.selectedIds).Count-ne1-or[string]$c.selectedIds[0]-cne$riderId){FoundationFail 'the click selection or availability differs'}
  if($click.detail.availability.enabled-ne$true){FoundationFail 'the control was not enabled at the click'}
  $b=$click.detail.before;$a=Get-KmcChunk6aFoundationOf $settled
  if($null-eq$b-or$null-eq$a){FoundationFail 'the transition baseline or settled observation is absent'}
  $expectedGeneration=if($mountCase){1}else{0}
  if(([long]$a.generation-[long]$b.generation)-ne$expectedGeneration){FoundationFail 'the relationship generation delta differs'}
  $acceptedField=if($mountCase){'acceptedMount'}else{'acceptedDismount'};$admittedField=if($mountCase){'admittedMount'}else{'admittedDismount'}
  if(([long]$a.transitionCounters.$acceptedField-[long]$b.transitionCounters.$acceptedField)-ne1-or([long]$a.transitionCounters.$admittedField-[long]$b.transitionCounters.$admittedField)-ne1){FoundationFail 'exactly one accepted voluntary transition is required'}
  foreach($f in @('refusedVoluntary','forcedDetach','duplicateSuppressed','concurrentSuppressed')){ if([long]$a.transitionCounters.$f-ne[long]$b.transitionCounters.$f){FoundationFail ('the transition ledger '+$f+' changed')} }
  if(([long]$a.dispatchAccepted-[long]$b.dispatchAccepted)-ne1-or[long]$a.dispatchRejected-ne[long]$b.dispatchRejected-or([long]$a.castRequests-[long]$b.castRequests)-ne1){FoundationFail 'dispatch or cast counters differ from one delivery'}
  if($a.transitionInFlight-ne$false-or$a.riderCommandsEmpty-ne$true-or$a.mountCommandsEmpty-ne$true-or$a.riderReallyMoving-eq$true-or$a.mountReallyMoving-eq$true){FoundationFail 'the settled row is not settled'}
  if([string]$a.relationship-cne$savedState){FoundationFail 'the settled relationship differs'}
  if([int]$settled.detail.settledFrames-lt10){FoundationFail 'settlement was observed for fewer than ten frames'}
  if($mountCase){
   if([long]$a.adoptionCount-ne([long]$b.adoptionCount+1)-or[string]::IsNullOrEmpty([string]$a.pairedIdentity)){FoundationFail 'the combat Mount did not adopt the running encounter exactly once'}
  }
  if($tb){ if([string]$settled.native.tbCurrent-cne$riderId-or[string]$click.native.tbCurrent-cne$riderId){FoundationFail 'the turn-based transition did not stay on the rider turn'} }
  $db=FoundationProp $b 'debt';$da=Get-KmcChunk6aFoundationDebtOf $settled
  if($null-eq$db-or$null-eq$da){FoundationFail 'the transition debt observations are absent'}
  if($tb){
   if(-not(FoundationNear $da.rider.Move ([double]$db.rider.Move+3))-or-not(FoundationNear $da.rider.Standard $db.rider.Standard)-or-not(FoundationNear $da.mount.Standard $db.mount.Standard)-or-not(FoundationNear $da.mount.Move $db.mount.Move)){FoundationFail 'the turn-based Mount cost differs from exactly one rider Move'}
  } else {
   if([double]$da.rider.Move-le0-or[double]$da.rider.Move-gt3.0001-or[double]$da.rider.Standard-gt([double]$db.rider.Standard+0.0001)-or[double]$da.mount.Standard-gt([double]$db.mount.Standard+0.0001)-or[double]$da.mount.Move-gt([double]$db.mount.Move+0.0001)){FoundationFail 'the real-time transition charged other than one rider Move'}
  }
  $written=@($rows|Where-Object kind -CEQ 'native-write-complete')[0];$d=$written.detail;$snapshot=$d.snapshot
  if($null-eq$snapshot){FoundationFail 'the write records no archive snapshot'}
  if($snapshot.Mounted-ne$mountCase-or[string]$snapshot.CampaignId-cne[string]$Request.fixture.working.gameId-or[string]$snapshot.AreaId-cne[string]$Request.fixture.working.area){FoundationFail 'the archive relationship or identity differs'}
  if($mountCase){ if([string]$snapshot.Rider.Id-cne$riderId-or[string]$snapshot.Mount.Id-cne$mountId){FoundationFail 'the archive pair differs'} }
  else { if($null-ne$snapshot.Rider-or$null-ne$snapshot.Mount-or$null-ne$snapshot.ProfileId){FoundationFail 'the dismount archive retains a pair'} }
  if($null-eq$snapshot.Combat-or$snapshot.Combat.TurnBased-ne$tb){FoundationFail 'the archive combat mode differs'}
  $riderActor=@($snapshot.Combat.Actors|Where-Object {[string]$_.Native.Id-ceq$riderId});$mountActor=@($snapshot.Combat.Actors|Where-Object {[string]$_.Native.Id-ceq$mountId})
  if($riderActor.Count-ne1-or$mountActor.Count-ne1){FoundationFail 'the archive combat actors omit the pair'}
  if($rt){ if($null-ne$snapshot.Combat.Current-or@($snapshot.Combat.Roster).Count-ne0){FoundationFail 'the real-time archive invented a turn'} }
  else {
   if($null-eq$snapshot.Combat.Current-or[string]$snapshot.Combat.Current.ActorId-cne$riderId-or$null-eq$snapshot.Combat.Paired-or$null-eq$snapshot.Combat.Paired.Activation-or[string]$snapshot.Combat.Paired.RiderId-cne$riderId-or[string]$snapshot.Combat.Paired.MountId-cne$mountId){FoundationFail 'the turn-based archive lacks the rider turn and the paired activation'}
  }
  if($rt-and$mountCase){ if($null-eq$snapshot.Combat.Paired-or$null-eq$snapshot.Combat.Paired.Activation){FoundationFail 'the real-time mounted archive lacks its adopted paired ownership'} }
  if([double]$riderActor[0].Native.Move-le0){FoundationFail 'the archive lost the charged rider Move'}
  if($tb-and-not(FoundationNear $riderActor[0].Native.Move $da.rider.Move)){FoundationFail 'the turn-based archive debt differs from the settled debt'}
  $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games');$path=Join-Path $root 'Manual_300_KMC_P01.zks'
  if([string]$d.path-cne$path-or[string]$d.nativeType-cne'Manual'-or$d.nativeCallback-ne$true-or[string]$d.operation-cne'None'-or(Get-KmcSha256 $path)-cne[string]$d.sha256-or(Get-Item -LiteralPath $path).Length-ne$d.length){FoundationFail 'the actual native manual archive was not completed'}
  if([string]$written.relationship-cne$savedState){FoundationFail 'the relationship at the write differs'}
  if($written.persistence.semantics-ne0-or$written.persistence.presentation-ne0){FoundationFail 'the source process restored a save'}
 } else {
  $i=$initial[0]
  $restored=if($rt){@($rows|Where-Object kind -CEQ 'rt-cold-debt-restored')[0]}else{$i}
  $snapshot=FoundationProp $restored.detail 'snapshot'
  if($null-eq$snapshot){FoundationFail 'the cold process records no selected archive snapshot'}
  if($snapshot.Mounted-ne$mountCase-or$snapshot.Combat.TurnBased-ne$tb-or[string]$snapshot.CampaignId-cne[string]$Request.fixture.working.gameId){FoundationFail 'the cold snapshot differs from the checkpoint'}
  if($i.persistence.semantics-ne@($snapshot.Combat.Actors).Count-or$i.persistence.presentation-ne$(if($mountCase){1}else{0})){FoundationFail 'the cold restoration duplicated semantic or presentation state'}
  # The fixture load itself leaves a baseline in the involuntary counters (the P02/P04 fixtures load with
  # forcedDetach 1 and duplicateSuppressed 1 in the save process and in its cold load alike), so the cold
  # process must replay no voluntary transition at all and must not move any counter after its initial
  # observation; the equality of that baseline with the source process is proved by the cold outcome
  # comparison (Assert-KmcChunk6aFoundationColdOutcome).
  $baseline=Get-KmcChunk6aFoundationOf $i
  if($null-eq$baseline){FoundationFail 'the cold initial row has no foundation observation'}
  $bt=$baseline.transitionCounters
  if([long]$baseline.castRequests-ne0-or[long]$baseline.adoptionCount-ne0-or[long]$bt.acceptedMount-ne0-or[long]$bt.admittedMount-ne0-or[long]$bt.acceptedDismount-ne0-or[long]$bt.admittedDismount-ne0-or[long]$bt.refusedVoluntary-ne0){FoundationFail 'the cold load replayed a voluntary transition before its initial observation'}
  $generations=@()
  foreach($r in $rows){
   if($r.controls.NativeCastRequestCount-ne0){FoundationFail ('the cold process cast a native control at '+$r.kind)}
   $f=Get-KmcChunk6aFoundationOf $r
   if($null-eq$f){continue}
   $t=$f.transitionCounters
   if([long]$f.castRequests-ne0-or[long]$f.adoptionCount-ne0){FoundationFail ('the cold process cast or adopted at '+$r.kind)}
   foreach($name in @('admittedMount','acceptedMount','admittedDismount','acceptedDismount','refusedVoluntary','forcedDetach','duplicateSuppressed','concurrentSuppressed')){ if([long]$t.$name-ne[long]$bt.$name){FoundationFail ('the cold process replayed or detached a transition at '+$r.kind)} }
   if($f.transitionInFlight-ne$false){FoundationFail ('a transition is in flight at '+$r.kind)}
   $generations+=[long]$f.generation
  }
  if($generations.Count-lt2-or@($generations|Select-Object -Unique).Count-ne1){FoundationFail 'the cold relationship generation changed during play'}
  $rf=Get-KmcChunk6aFoundationOf $restored
  if($null-eq$rf){FoundationFail 'the restored row has no foundation observation'}
  if($mountCase){ if([string]::IsNullOrEmpty([string]$rf.pairedIdentity)){FoundationFail 'the cold pair lacks its restored paired ownership'} }
  else { if(-not[string]::IsNullOrEmpty([string]$rf.partnerContextActor)){FoundationFail 'the cold unmounted actors retain a partner context'} }
  if($tb){ if([string]$i.native.tbCurrent-cne$riderId){FoundationFail 'the cold turn is not the rider turn'} }
 }
 $final=@($rows|Where-Object kind -CEQ 'usable-continuation-complete')[0]
 if([string]$final.relationship-cne$savedState){FoundationFail 'the continuation ended in another relationship'}
 $ff=Get-KmcChunk6aFoundationOf $final
 if($null-eq$ff-or$ff.transitionInFlight-ne$false){FoundationFail 'the continuation ended with a transition in flight'}
 if($rt){
  $first=@($rows|Where-Object kind -CEQ 'rt-approach-first-delivery')[0];$cont=@($rows|Where-Object kind -CEQ 'rt-foundation-continuation')[0]
  if($first.detail.resolved-ne1-or$cont.detail.resolved-ne0-or$cont.detail.ordinaryAttacks-ne0-or$first.detail.inputRequests-ne1-or$cont.detail.inputRequests-ne1-or$first.detail.riderRounds-ne$cont.detail.riderRounds-or$first.detail.forcedD20-ne0){FoundationFail 'the continuation replayed input, attack or preparation'}
  $queued=@($rows|Where-Object kind -CEQ 'rt-spent-attack-queued')[0];$wait=@($rows|Where-Object kind -CEQ 'rt-native-debt-wait')[0];$later=@($rows|Where-Object kind -CEQ 'rt-later-attack')
  if($queued.rider.Standard-le0-or$wait.gameTicks-ge$queued.detail.readyTicks){FoundationFail 'no real wait for the spent native debt'}
  for($k=0;$k-lt2;$k++){
   if($later[$k].gameTicks+100000-lt$queued.detail.readyTicks-or$later[$k].detail.resolved-ne($queued.detail.resolved+$k+1)-or$later[$k].detail.riderRounds-ne($queued.detail.riderRounds+$k+1)-or$later[$k].detail.forcedD20-ne0){FoundationFail 'later native work fired early, duplicated or refreshed incorrectly'}
  }
 } else {
  foreach($attack in @($rows|Where-Object kind -CEQ 'attack-delivered')){ if($attack.detail.rules-lt1-or$attack.detail.rolls-lt1-or[string]$attack.detail.actor-cne$riderId){FoundationFail 'no actual native rider attack outcome'} }
  $refresh=@($rows|Where-Object kind -CEQ 'next-paired-activation')
  if($refresh[1].detail.sequence-ne($refresh[0].detail.sequence+1)){FoundationFail 'the paired activations are not successive'}
  if(@($final.detail.turnVisits).Count-lt4){FoundationFail 'the continuation lacks observed unrelated participation'}
 }
}

# Evidence-only comparison of a cold foundation process with its source: native health and the
# relationship are the saved ones and nothing was replayed.
function Assert-KmcChunk6aFoundationColdOutcome {
 param($SourceRows,$ColdRows)
 $written=@($SourceRows|Where-Object kind -CEQ 'native-write-complete');$loaded=@($ColdRows|Where-Object kind -CEQ 'rt-cold-debt-restored')
 if($written.Count-ne1-or$loaded.Count-ne1-or$written[0].processId-eq$loaded[0].processId){FoundationFail 'the cold comparison lacks exact source/cold observations'}
 if([string]$written[0].checkpoint-cne[string]$loaded[0].checkpoint){FoundationFail 'the cold checkpoint differs from its source'}
 # The involuntary counters of a fresh process are the fixture-load baseline; the cold load may not add to it.
 $si=@($SourceRows|Where-Object kind -CEQ 'initial');$ci=@($ColdRows|Where-Object kind -CEQ 'initial')
 if($si.Count-ne1-or$ci.Count-ne1){FoundationFail 'the cold comparison lacks the initial observations'}
 $sf=Get-KmcChunk6aFoundationOf $si[0];$cf=Get-KmcChunk6aFoundationOf $ci[0]
 if($null-eq$sf-or$null-eq$cf){FoundationFail 'the cold comparison lacks the initial foundation observations'}
 foreach($name in @('forcedDetach','duplicateSuppressed','concurrentSuppressed')){ if([long]$sf.transitionCounters.$name-ne[long]$cf.transitionCounters.$name){FoundationFail ('the cold load moved the involuntary counter '+$name+' beyond the fixture-load baseline of its source process')} }
 $a=$written[0].detail.actual;$b=$loaded[0].detail.actual
 if([string]$a.target-cne[string]$b.target-or$a.targetDamage-ne$b.targetDamage-or$b.resolved-ne0-or$b.ordinaryAttacks-ne0-or$b.inputRequests-ne0-or$b.unresolvedProjectiles-ne$false-or[string]$a.foundation.relationship-cne[string]$b.foundation.relationship){FoundationFail 'the cold load changed native health or the relationship, or replayed delivery'}
}

# ---------------------------------------------------------------------------------------------
# Ledger projection of a foundation persistence run: one row named by scenario and checkpoint.
# ---------------------------------------------------------------------------------------------
function Get-KmcChunk6aFoundationRecords([string]$Path,$Game) {
 if([IO.Path]::GetFileName($Path)-cne'persistence-observations.jsonl'-or-not(Test-KmcChunk6aFoundationScenario ([string]$Game.scenario))){FoundationFail 'the reader requires the persistence JSONL of a foundation scenario'}
 $records=@(foreach($line in Get-Content -LiteralPath $Path -Encoding UTF8){
  if([string]::IsNullOrWhiteSpace($line)){continue}
  Assert-KmcJsonObjectMembersUnique $line 'bound foundation record'
  $line|ConvertFrom-Json
 })
 if($records.Count-lt1){FoundationFail 'the foundation artifact is empty'}
 foreach($r in $records){ if($r.runId-cne$Game.runId-or$r.scenario-cne$Game.scenario-or$r.source-cne$Game.commit-or$r.dll-cne$Game.dllSha256){FoundationFail 'a foundation record identity differs'} }
 $records
}
function Get-KmcChunk6aFoundationRows([string]$Path,[string]$Scenario,$Game) {
 if([string]$Game.scenario-cne$Scenario){FoundationFail 'the scenario differs from the native result'}
 $records=@(Get-KmcChunk6aFoundationRecords $Path $Game)
 $cases=@($records|ForEach-Object {[string]$_.checkpoint}|Select-Object -Unique)
 if($cases.Count-ne1-or-not(Test-KmcChunk6aFoundationCase $cases[0])){FoundationFail 'the artifact checkpoint is not unique or not a foundation checkpoint'}
 $final=@($records|Where-Object kind -CEQ 'usable-continuation-complete');$failed=@($records|Where-Object kind -CIn @('assertion-failed','scenario-failed'))
 $status=if($final.Count-eq1-and$failed.Count-eq0-and[string]$Game.status-ceq'PASS'){'PASS'}else{'FAIL'}
 [pscustomobject]@{name=(Get-KmcChunk6aFoundationRowName $Scenario $cases[0]);status=$status;assertionPassCount=$Game.assertionPassCount;assertionFailCount=$Game.assertionFailCount;errors=@($Game.errors)}
}
function Get-KmcChunk6aFoundationProjection($Binding,$Request,$Game,$Result,[string]$Root) {
 if(-not(Test-KmcChunk6aFoundationScenario ([string]$Binding.scenario))-or[string]$Binding.evidenceLeaf-cne'persistence-observations.jsonl'-or@($Binding.rows).Count-ne1){FoundationFail 'a foundation binding requires the persistence scenario, JSONL leaf and one row'}
 if([IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')-cne[IO.Path]::GetFullPath($Root).TrimEnd('\')){FoundationFail 'the request evidence root differs'}
 foreach($item in @($Request,$Game,$Result)){ if([string]$item.runId-cne[string]$Binding.runId-or[string]$item.scenario-cne[string]$Binding.scenario){FoundationFail 'the run identity differs'} }
 # A re-evaluated binding (an immutable artifact re-read under a later harness identity) carries the
 # earlier reader's refusal in its overall facet: that facet must then be exactly a FAIL made only of
 # this reader's own refusals while the native facet stays an exact PASS; the supporting binder proves
 # that the recorded original overall facet is the one re-evaluated.
 $reevaluated=$null-ne(FoundationProp $Binding 'reevaluation')
 foreach($item in @($Game,$Result)){
  if($reevaluated-and[object]::ReferenceEquals($item,$Result)){
   if([string]$item.status-cne'FAIL'-or-not(Test-KmcExactJsonInteger $item.assertionFailCount)-or$item.assertionFailCount-lt1-or$item.errors-isnot[Array]-or@($item.errors).Count-lt1-or@($item.errors|Where-Object {[string]$_-cnotmatch'^Chunk 6A foundation: '}).Count-ne0){FoundationFail 'the re-evaluated overall result is not exactly an earlier refusal of this reader'}
  } elseif([string]$item.status-cne'PASS'-or-not(Test-KmcExactJsonInteger $item.assertionPassCount)-or$item.assertionPassCount-lt1-or-not(Test-KmcExactJsonInteger $item.assertionFailCount)-or$item.assertionFailCount-ne0-or$item.errors-isnot[Array]-or@($item.errors).Count-ne0){FoundationFail 'the native/overall result is not an exact PASS'}
  if([string]$item.evidenceManifestSha256-cne[string]$Binding.artifactManifestSha256){FoundationFail 'the result manifest binding differs'}
 }
 Assert-KmcReadOnlyArtifactManifest -Request $Request -ExpectedSha256 $Binding.artifactManifestSha256
 $manifest=Get-KmcBoundJson (Join-Path $Root 'runtime-artifacts.json') $Binding.artifactManifestSha256
 $leaves=@($manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
 if($leaves.Count-ne1-or[string]$leaves[0].sha256-cne[string]$Binding.evidenceSha256-or[string]$leaves[0].kind-cne'persistence-evidence'){FoundationFail 'the artifact hash differs from the manifest'}
 $case=[string](FoundationProp $Request 'persistenceCase')
 $rowName=Get-KmcChunk6aFoundationRowName ([string]$Binding.scenario) $case
 if([string]$Binding.rows[0]-cne$rowName){FoundationFail 'the binding row differs from the request checkpoint'}
 # The complete persistence validator (identity, archive, foundation facts, continuation) re-runs on the immutable bytes.
 Assert-KmcPersistenceScenarioEvidence -Request $Request -Manifest $manifest -Status 'PASS' -GameResult $Game
 $row=Get-KmcChunk6aFoundationRows (Join-Path $Root 'persistence-observations.jsonl') ([string]$Binding.scenario) $Game
 if([string]$row.status-cne'PASS'-or[string]$row.name-cne$rowName-or$row.assertionPassCount-ne$Binding.passCount-or$row.assertionFailCount-ne0){FoundationFail 'the row totals differ'}
 $projection=[ordered]@{}
 foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){$projection[$field]=$Game.$field}
 $projection.status='PASS';$projection.errors=@();$projection.rows=@($row)
 [pscustomobject]$projection
}
# The isolated id-to-checkpoint contract: a foundation id is qualified only from its own
# scenario and checkpoint, never from a neighbouring one.
function Assert-KmcChunk6aFoundationIsolated([string]$Id,$Binding,[string]$LabRoot) {
 $map=Get-KmcChunk6aFoundationIdMap
 if(-not$map.Contains($Id)){FoundationFail ('not a foundation id: '+$Id)}
 $expected=$map[$Id]
 if([string]$Binding.scenario-cne$expected[0]){FoundationFail ($Id+' requires its exact scenario')}
 $root=Join-Path $LabRoot ('runtime-evidence/'+$Binding.runId)
 $request=Get-KmcBoundJson (Join-Path $root 'runtime-request.json') $Binding.requestSha256
 if([string](FoundationProp $request 'persistenceCase')-cne$expected[1]){FoundationFail ($Id+' requires its exact checkpoint')}
 $rowName=Get-KmcChunk6aFoundationRowName $expected[0] $expected[1]
 if(@($Binding.rows).Count-ne1-or[string]$Binding.rows[0]-cne$rowName){FoundationFail ($Id+' binding row differs')}
 if($expected[0].EndsWith('-load',[StringComparison]::Ordinal)-and$null-eq(FoundationProp $request 'persistenceLoad')){FoundationFail ($Id+' cold load lacks its archive descriptor')}
}
# A cold-load entry names its source save entry; the archive it opened is the byte-exact write
# of that PASS source run on the same frozen payload.
function Assert-KmcChunk6aFoundationLedgerPairing([string]$Id,$Entry,$Ids,[string]$LabRoot) {
 $sources=Get-KmcChunk6aFoundationLoadSources
 $sourceId=[string](FoundationProp $Entry 'sourceEntry')
 if(-not$sources.ContainsKey($Id)){ if(-not[string]::IsNullOrEmpty($sourceId)){FoundationFail ($Id+' names a source entry but is not a cold load')}; return }
 if($sourceId-cne$sources[$Id]){FoundationFail ($Id+' must name its source save entry '+$sources[$Id])}
 if(-not$Ids.ContainsKey($sourceId)-or[string]$Ids[$sourceId].status-cne'PASS'){FoundationFail ($Id+': source entry '+$sourceId+' is not PASS on this ledger')}
 $source=$Ids[$sourceId]
 $loadRequest=Get-Content -Raw -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$Entry.runId+'/runtime-request.json'))|ConvertFrom-Json
 $sourceRequest=Get-Content -Raw -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$source.runId+'/runtime-request.json'))|ConvertFrom-Json
 if([string](FoundationProp $loadRequest 'persistenceCase')-cne[string](FoundationProp $sourceRequest 'persistenceCase')){FoundationFail ($Id+': the cold checkpoint differs from its source')}
 $sourceRows=@(Get-Content -LiteralPath (Join-Path $LabRoot ('runtime-evidence/'+$source.runId+'/persistence-observations.jsonl')) -Encoding UTF8|ForEach-Object {$_|ConvertFrom-Json})
 $written=@($sourceRows|Where-Object kind -CEQ 'native-write-complete')
 if($written.Count-ne1){FoundationFail ($Id+': the source run lacks its write')}
 $load=FoundationProp $loadRequest 'persistenceLoad'
 if($null-eq$load-or[string]$load.sha256-cne[string]$written[0].detail.sha256-or[string]$load.fileName-cne'Manual_300_KMC_P01.zks'){FoundationFail ($Id+': the cold archive is not the source run write')}
 $archive=Join-Path $LabRoot ('runtime-staging/persistence-'+$source.runId+'/Saved Games/Manual_300_KMC_P01.zks')
 if((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()-cne[string]$load.sha256){FoundationFail ($Id+': the staged source archive differs from the loaded one')}
}

# ---------------------------------------------------------------------------------------------
# Lifecycle boundaries during the exact Mount approach (chunk6a-combat-end-approach and
# chunk6a-disable-approach). Exactly one lawful outcome shape, no residue, no refund.
# ---------------------------------------------------------------------------------------------
function Assert-KmcChunk6aLifecycleBoundary([string]$Scenario,$Artifact) {
 $combatEnd=$Scenario-ceq'chunk6a-combat-end-approach';$disable=$Scenario-ceq'chunk6a-disable-approach'
 if(-not($combatEnd-or$disable)){FoundationFail 'not a lifecycle boundary scenario'}
 $Case=FoundationProp $Artifact.observations 'chunk6aLifecycleBoundary'
 if($null-eq$Case){FoundationFail 'the lifecycle boundary evidence is absent'}
 $row=if($combatEnd){'CM04-combat-end'}else{'CM04-disable-unload'}
 $rows=@($Artifact.rows|Where-Object name -CEQ $row)
 if($rows.Count-ne1-or[string]$rows[0].status-cne'PASS'){FoundationFail ('the exact mandatory row '+$row+' is absent')}
 if((ConvertTo-Json -InputObject $rows[0].evidence -Depth 100 -Compress)-cne(ConvertTo-Json -InputObject $Case -Depth 100 -Compress)){FoundationFail 'the row evidence differs from the recorded case'}
 $contract=if($combatEnd){'native-combat-end-during-exact-mount-approach'}else{'registered-disable-during-exact-mount-approach'}
 if([string]$Case.contract-cne$contract-or[string]$Case.boundary-cne$(if($combatEnd){'combat-end'}else{'mod-disable'})){FoundationFail 'the contract differs'}
 if($Case.start.isAdjacent-ne$false-or[string]$Case.start.relationshipState-cne'Unmounted'){FoundationFail 'the Mount did not start outside transition reach from an unmounted pair'}
 $click=$Case.click
 if($null-eq$click-or$click.clicked-ne$true-or$click.nativeCastRequestDelta-ne1-or$click.nativeRefusalDelta-ne0-or$click.dispatchAcceptedDelta-ne0-or$click.dispatchRejectedDelta-ne0-or[string]$click.abilityGuid-cne'f053faad986631688defa003cd7bda0e'){FoundationFail 'the native Mount click was not admitted exactly once without delivery'}
 $before=$Case.before;$trigger=FoundationProp $Case 'trigger'
 if($null-eq$trigger-or$trigger.approachObserved-ne$true-or$trigger.riderReallyMoving-ne$true-or[double]$trigger.riderDisplacement-le0.25-or$trigger.started-ne$false-or$trigger.acted-ne$false-or$trigger.finished-ne$false-or$trigger.moveSlotObject-ne$trigger.commandObject-or$trigger.geometry.isAdjacent-ne$false){FoundationFail 'the boundary missed the exact pending non-adjacent approach'}
 if([string]$trigger.state.relationshipState-cne'Unmounted'){FoundationFail 'the pair was already mounted at the boundary'}
 $terminal=FoundationProp $Case 'terminal';$after=FoundationProp $Case 'after'
 if($null-eq$terminal-or$null-eq$after-or$Case.terminalCommand.finished-ne$true){FoundationFail 'no terminal was observed'}
 # Residue is decided from primitives: no transition in flight, both command containers empty, no paired
 # activation identity or partner context left behind, and exactly one relationship-shell registration for
 # the one click (NativeRelationshipShellCount is a monotonic registration counter that is never
 # decremented; the compiled noResidue flag expected it unchanged and is recorded, not trusted).
 if($after.transitionInFlight-ne$false-or$after.riderCommandsEmpty-ne$true-or$after.horseCommandsEmpty-ne$true-or$null-ne(FoundationProp $after 'pairedIdentity')-or$null-ne(FoundationProp $after 'partnerContextActor')-or$Case.relationshipShellsDelta-ne1-or$Case.allocationTraceComplete-ne$true-or[int]$Case.settledFrames-lt10){FoundationFail 'residue remained after the boundary'}
 if($Case.pairPrepareCallbacks-ne0){FoundationFail 'a native preparation ran during the boundary window'}
 if($Case.castRequestDelta-ne1){FoundationFail 'the window holds other than the one native cast request'}
 $outcome=[string]$Case.outcome;$delta=$Case.ledgerDelta
 switch -Regex -CaseSensitive ($outcome){
  '^unacted-(Interrupt|Fail)$' {
   # A registered disable is itself a forced-detach cleanup trigger (idempotent, no voluntary cost): its one
   # cleanup is counted exactly once in the transition ledger even with nothing attached; the combat-end
   # boundary detaches nothing.
   $expectedDetach=if($disable){1}else{0}
   if([string]$after.relationshipState-cne'Unmounted'-or$delta.acceptedMount-ne0-or$delta.forcedDetach-ne$expectedDetach-or$Case.generationDelta-ne0-or$Case.dispatchAcceptedDelta-ne0-or$Case.pairCostCallbacks-ne0-or$Case.terminalCommand.acted-ne$false){FoundationFail 'the unacted outcome carries a transition, cost or dispatch'}
   foreach($actor in @('rider','mount')){ foreach($f in @('standard','move','swift')){ if([double]$after.$actor.$f-gt([double]$before.$actor.$f+0.0001)){FoundationFail ('the unacted outcome raised '+$actor+' '+$f)} } }
  }
  '^delivered$' {
   if($disable){FoundationFail 'a disabled service delivered a Mount'}
   if([string]$after.relationshipState-cne'Mounted'-or$delta.acceptedMount-ne1-or$delta.admittedMount-ne1-or$delta.forcedDetach-ne0-or$delta.refusedVoluntary-ne0-or$Case.generationDelta-ne1-or$Case.dispatchAcceptedDelta-ne1-or[string]$Case.commandWindowKind-cne'positive'){FoundationFail 'the delivered outcome is not exactly one accepted transition'}
   # The native encounter ends on the engine's own schedule after the target is gone, and the pending Mount
   # may deliver on either side of that moment. Delivered after the party left combat: no encounter adoption.
   # Delivered while the party was still in combat: exactly one real-time adoption at the delivery frame,
   # retired by the combat-end cleanup (the residue rule above proves no activation or partner context is
   # left) and never a fresh grant afterwards (preview.155 c6a-foundation155-a-combat-end).
   if($after.partyInCombat-ne$false){FoundationFail 'the party was still in combat at the boundary observation'}
   $adoptions=[long]$after.adoptionCount-[long]$before.adoptionCount
   if($adoptions-eq1-and$combatEnd){
    $deliver=@(@(FoundationProp (FoundationProp $Case 'commandWindow') 'samples')|Where-Object {$null-ne$_-and[string](FoundationProp $_ 'boundary')-ceq'deliver'})
    $leftFrame=FoundationProp (FoundationProp $Case 'combatEnd') 'partyLeftCombatFrame'
    if($deliver.Count-ne1-or$null-eq$leftFrame-or$null-eq(FoundationProp $deliver[0] 'frame')-or[long]$deliver[0].frame-ge[long]$leftFrame){FoundationFail 'a Mount delivered after combat end adopted an encounter'}
   } elseif($adoptions-ne0){FoundationFail 'a Mount delivered after combat end adopted an encounter'}
  }
  '^acted-not-mounted$' {
   if([string]$after.relationshipState-cne'Unmounted'-or$delta.acceptedMount-ne0-or$delta.admittedMount-ne1-or$delta.refusedVoluntary-ne1-or$delta.forcedDetach-ne1-or$Case.generationDelta-ne1-or$after.compensatedMounts-ne($before.compensatedMounts+1)){FoundationFail 'the compensated outcome is not exactly one compensation'}
  }
  default { FoundationFail ('unlawful outcome '+$outcome) }
 }
 if($combatEnd){
  $ce=FoundationProp $Case 'combatEnd'
  if($null-eq$ce-or$ce.destroyVerified-ne$true-or$null-eq(FoundationProp $ce 'partyLeftCombatFrame')-or$ce.partyLeftCombat.partyInCombat-ne$false-or$after.partyInCombat-ne$false-or$after.riderInCombat-ne$false-or$after.horseInCombat-ne$false){FoundationFail 'the encounter did not end natively during the sequence'}
  if([long]$ce.partyLeftCombatFrame-lt[long]$trigger.frame){FoundationFail 'combat ended before the boundary trigger'}
 } else {
  $d=FoundationProp $Case 'disable';$r=FoundationProp $Case 'reEnable'
  if($null-eq$d-or$d.disableResult-ne$true-or$d.after.controls.enabled-ne$false-or[string]$d.after.relationshipState-cne'Unmounted'-or[int]$d.after.controls.exactFactCount-ge[int]$d.before.controls.exactFactCount){FoundationFail 'the registered disable did not clean up during the sequence'}
  if($null-eq$r-or$r.reEnableResult-ne$true-or$r.after.controls.enabled-ne$true-or$r.after.controls.duplicateFactCount-ne0-or[string]$r.after.relationshipState-cne'Unmounted'-or$r.after.castRequests-ne$r.before.castRequests-or$r.after.generation-ne$r.before.generation-or$r.mountAvailability.visible-ne$true){FoundationFail 'the registered re-enable did not return the services once without a pair'}
  if($after.partyInCombat-ne$true){FoundationFail 'the encounter did not stay live across the disable'}
 }
}
