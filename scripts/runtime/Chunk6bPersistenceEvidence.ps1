Set-StrictMode -Version Latest
# One external acceptance authority for the P04 charge checkpoints; immutable facts only.
function Get-KmcChargePersistenceCases { @('mounted-charge-pending','mounted-charge-settled','mounted-charge-cancelled','mounted-charge-failed','mounted-charge-drained') }
function Test-KmcChargePersistenceCase([string]$Case) { $Case -cin (Get-KmcChargePersistenceCases) }
function Get-KmcChargeLifecycleCases { @('mounted-charge-area','mounted-charge-session','mounted-charge-disable','mounted-charge-removal') }
function Test-KmcChargeLifecycleCase([string]$Case) { $Case -cin (Get-KmcChargeLifecycleCases) }
function Get-KmcChargeRemovalSourceProof([string]$RunId,[string]$ArchiveSha,[string]$Commit,[string]$Dll) {
 if($RunId-cnotmatch'^[A-Za-z0-9._-]{1,120}$'){ChargeSaveFail 'invalid charge removal source run'}
 $root=Join-Path (Get-KmcLabRoot) ('runtime-evidence/'+$RunId)
 $request=Read-KmcJson (Join-Path $root 'runtime-request.json')
 $result=Read-KmcJson (Join-Path $root 'runtime-result.json')
 if($request.runId-cne$RunId-or$request.scenario-cne'persistence-p07-save'-or$request.persistenceCase-cne'mounted-charge-removal'-or
    $result.runId-cne$RunId-or$result.scenario-cne$request.scenario-or$result.transactionToken-cne$request.transactionToken-or
    $request.commit-cne$Commit-or$request.dllSha256-cne$Dll-or$result.commit-cne$Commit-or$result.dllSha256-cne$Dll-or
    $result.status-cne'PASS'-or$result.modsRestored-ne$true-or$result.workingRestored-ne$true-or$result.saveProtectionPassed-ne$true-or
    $result.baselineImmutable-ne$true-or$result.saveWriteAllowlistPassed-ne$true-or@($result.errors).Count-ne0){ChargeSaveFail 'charge no-DLL source is not the exact restored product and removal case'}
    $path=Join-Path $root 'persistence-observations.jsonl'
    $sha=Get-KmcSha256 $path;$rows=@(Get-Content -LiteralPath $path|ForEach-Object{$_|ConvertFrom-Json})
    $gamePath=Join-Path $root 'runtime-game-result.json';$game=Read-KmcJson $gamePath
    $manifestPath=Join-Path $root 'runtime-artifacts.json';$manifest=Read-KmcJson $manifestPath
    $artifact=@($manifest.artifacts|Where-Object relativePath -CEQ 'persistence-observations.jsonl')
    if((Get-KmcSha256 $gamePath)-cne$result.gameResultSha256-or$game.status-cne'PASS'-or
       (Get-KmcSha256 $manifestPath)-cne$game.evidenceManifestSha256-or$manifest.runId-cne$RunId-or
       $artifact.Count-ne1-or$artifact[0].kind-cne'persistence-evidence'-or$artifact[0].sha256-cne$sha-or
       $artifact[0].length-ne(Get-Item -LiteralPath $path).Length){ChargeSaveFail 'charge source facts are not bound to the immutable native result and manifest'}
    Assert-KmcChargeLifecycleEvidence $request $rows $game
 $done=(ChargeSaveRow $rows 'charge-lifecycle-complete').detail
 if($done.cleanupSha256-cne$ArchiveSha-or$done.cleanupLeaf-cne'Manual_301_KMC_CLEANUP.zks'-or
    $done.charge.chargeBuffGuid-cnotmatch'^[0-9a-f]{32}$'-or$done.riderId-ceq$done.mountId){ChargeSaveFail 'charge source archive or native identities differ'}
 Assert-KmcChargeDrained $done.charge.lastDrained
 if((Get-KmcSha256 $path)-cne$sha){ChargeSaveFail 'charge source observations changed during inspection'}
 [ordered]@{runId=$RunId;observationsSha256=$sha;riderId=$done.riderId;mountId=$done.mountId;chargeBuffGuid=$done.charge.chargeBuffGuid}
}
function Assert-KmcChargeAbsentFacts($ObserverRequest,$Observations) {
 if($ObserverRequest.schemaVersion-ne2-or$null-eq$ObserverRequest.chargeSource){ChargeSaveFail 'no-DLL request lacks the charge source schema'}
 $source=$ObserverRequest.chargeSource
 if([string]::IsNullOrEmpty($source.riderId)-or[string]::IsNullOrEmpty($source.mountId)-or$source.riderId-ceq$source.mountId-or
    $source.observationsSha256-cnotmatch'^[0-9a-f]{64}$'-or$source.chargeBuffGuid-cnotmatch'^[0-9a-f]{32}$'){ChargeSaveFail 'no-DLL charge source actors or identity differ'}
 $expected=@('4016c7db400ab721ff125aef9e65e202','7db7c50677e39f09feef56f3831fc723','98e651899e6278d938de77af1d69bd32','6874a165bf8bda3531ee4e2abc10c899','f053faad986631688defa003cd7bda0e','3af2b81f4d72bbb30501fa730fcdf36e','27364df661b3c121eabb97a31aa73a83','f88a50d6fdbebbd709c3e323d2f52f5e','d79eaec224a7a832e738eb81baef9d49')
 if(@($ObserverRequest.kmcBlueprintGuids).Count-ne$expected.Count-or
    (@($ObserverRequest.kmcBlueprintGuids|Sort-Object)-join'|')-cne(@($expected|Sort-Object)-join'|')){ChargeSaveFail 'no-DLL blueprint inventory omits a KMC reference'}
 foreach($phase in @('chargeLoaded','chargeSettled')){
  $facts=$Observations.$phase
  if($null-eq$facts-or(ConvertTo-KmcChargeCanonical $facts.source)-cne(ConvertTo-KmcChargeCanonical $source)-or
     $facts.dropped-ne0-or@($facts.attacks).Count-ne0-or@($facts.processes|Where-Object ended -NE $true).Count-ne0){ChargeSaveFail 'no-DLL process resumed a charge or changed source identity'}
  foreach($pair in @(@('rider',$source.riderId),@('mount',$source.mountId))){
   $actor=$facts.($pair[0])
   if($actor.id-cne$pair[1]-or$actor.matches-ne1-or$actor.viewPresent-ne$true-or$actor.agentPresent-ne$true-or
      @($actor.raw).Count-ne0-or@($actor.queue).Count-ne0-or$null-ne$actor.standard-or$null-ne$actor.move-or
      $actor.pathPresent-ne$false-or$actor.moving-ne$false-or$actor.mountCharging-ne$false-or$actor.riderCharging-ne$false-or
      $null-ne$actor.speedOverride-or$actor.chargeBuffs-ne0-or@($actor.kmcAbilities).Count-ne0){ChargeSaveFail 'no-DLL native actor has charge command/path/flag/speed/buff/blueprint residue'}
  }
 }
 if($Observations.chargeSettled.gameTicks-lt$Observations.chargeLoaded.gameTicks){ChargeSaveFail 'no-DLL observation clock reversed'}
}
function ChargeSaveFail([string]$Message) { throw ('Charge persistence: '+$Message) }
function Assert-KmcChargeNativeFactDrained($Fact,[string]$Kind) {
 if($null-eq$Fact){ChargeSaveFail 'missing retained native fact postconditions'}
 foreach($field in @('identity','collection','listening','statModifiers','attachedModifiers','componentCount','storedFacts','storedModifiers')){
  if($Fact.$field-isnot[int]-and$Fact.$field-isnot[long]){ChargeSaveFail ('native fact lacks integer '+$field)}
 }
 foreach($field in @('inCollection','active','turnedOn','activating','deactivating','recalculating','parentContext','currentContext')){
  if($Fact.$field-isnot[bool]-or$Fact.$field-ne$false){ChargeSaveFail ('native fact retains '+$field)}
 }
 foreach($field in @('listening','statModifiers','attachedModifiers','storedFacts','storedModifiers')){
  if($Fact.$field-ne0){ChargeSaveFail ('native fact retains '+$field)}
 }
 if($Kind-ceq'buff'-and($Fact.disposed-ne$true-or$Fact.componentCount-ne0-or$Fact.componentData-ne$false)){ChargeSaveFail 'native buff disposal remains incomplete'}
 if($Kind-ceq'enchantment'-and$Fact.componentCount-ne1){ChargeSaveFail 'retained native enchantment listener inventory differs'}
}
function Assert-KmcChargeBuffGraphDrained($Lease) {
 $graph=$Lease.buffChildren
 if($null-eq$graph-or$graph.schema-ne1-or$graph.surface-cnotin@('native-base','native-cotw-1.14.4c-2.1')-or
    [string]::IsNullOrEmpty($graph.rider)-or$graph.retiring-ne$true-or$graph.scopeSettled-ne$true-or$graph.drained-ne$true-or$null-ne$graph.fault){ChargeSaveFail 'loaded Charge lifetime graph is not retained and drained'}
 Assert-KmcChargeNativeFactDrained $Lease.buffNative 'buff'
 if($graph.rootIdentity-ne$Lease.buffNative.identity-or$graph.rootCollection-ne$Lease.buffNative.collection){ChargeSaveFail 'root fact identity differs from retained graph'}
 $nodes=@($graph.facts)
 if($nodes.Count-gt16-or($graph.surface-ceq'native-base'-and$nodes.Count-ne0)){ChargeSaveFail 'unexpected or unbounded charge lifetime graph'}
 $buffs=@('6683a35444eb42ddbd21f87c3441a50a','b0439659723f4a8da680965c78a8fbf5','61aff33f69d84391b49782fb976cf870')
 $enchantments=@('30f90becaaac51f41bf56641966c4121','3f032a3cd54e57649a0cdad0434bf221')
 $seen=@($graph.rootIdentity);$sequence=0
 foreach($node in $nodes){
  $sequence++
  foreach($field in @('id','parent')){if($node.$field-isnot[int]-and$node.$field-isnot[long]){ChargeSaveFail 'child identity is not an exact integer'}}
  foreach($field in @('created','acquiring','identityUncertain','settled','drained')){if($node.$field-isnot[bool]){ChargeSaveFail 'child ownership facts lack exact booleans'}}
  if($node.id-ne$sequence-or$node.created-ne$true-or$node.acquiring-ne$false-or$node.identityUncertain-ne$false-or
     $node.settled-ne$true-or$node.drained-ne$true-or$node.kind-cnotin@('buff','enchantment')){ChargeSaveFail 'child acquisition identity or settlement differs'}
  Assert-KmcChargeNativeFactDrained $node.native $node.kind
  if($node.native.identity-in$seen){ChargeSaveFail 'native lifetime fact was counted twice'};$seen+=@($node.native.identity)
  if($node.kind-ceq'buff'){
   if($node.blueprint-cnotin$buffs-or$node.parent-ne0-or$node.native.collection-ne$graph.rootCollection-or$null-ne$node.visuals){ChargeSaveFail 'child buff escaped exact original rider collection or root'}
  }else{
   if($node.blueprint-cnotin$enchantments-or$node.parent-lt1-or$node.parent-ge$sequence-or
      $nodes[$node.parent-1].blueprint-cne$buffs[1]){ChargeSaveFail 'enchantment lacks exact retained Hellfire parent'}
   $fx=$node.visuals
   foreach($field in @('scopes','roots','copies','pendingRoots','pendingCopies')){if($null-eq$fx-or($fx.$field-isnot[int]-and$fx.$field-isnot[long])){ChargeSaveFail 'visual ownership lacks exact counts'}}
   if($null-eq$fx-or$fx.scopes-ne0-or$fx.pendingRoots-ne0-or$fx.pendingCopies-ne0-or$fx.retiring-ne$true-or$fx.attached-ne$false-or$fx.drained-ne$true-or$null-ne$fx.fault){ChargeSaveFail 'enchantment FX ownership remains live or ambiguous'}
   $acquisitions=@($fx.acquisitions);$roots=@($fx.rootFacts)
   if($fx.roots-lt0-or$fx.roots-gt16-or$fx.copies-lt0-or$fx.copies-gt(2*$fx.roots)-or
      $acquisitions.Count-ne($fx.roots+$fx.copies)-or$roots.Count-ne$fx.roots){ChargeSaveFail 'visual lifetime counts differ from finite native graph'}
   $generation=0
   foreach($acquisition in $acquisitions){$generation++;if(($acquisition.generation-isnot[int]-and$acquisition.generation-isnot[long])-or$acquisition.generation-ne$generation-or$acquisition.returned-isnot[bool]-or$acquisition.returned-ne$true){ChargeSaveFail 'effect acquisition has no observed destruction or pool transfer'}}
   if(@($roots|Select-Object -ExpandProperty generation -Unique).Count-ne$roots.Count){ChargeSaveFail 'FX root acquisition duplicated'}
   foreach($root in $roots){if($root.generation-lt1-or$root.generation-gt$acquisitions.Count-or$root.returned-ne$true-or$root.custodyUncertain-isnot[bool]-or$root.custodyUncertain-ne$false-or$root.controllerResidueAbsent-ne$true){ChargeSaveFail 'pooled root retains actor/controller residue or uncertain custody'}}
   if($node.blueprint-ceq$enchantments[1]-and$acquisitions.Count-ne0){ChargeSaveFail 'Burst acquired an unexpected visual'}
  }
 }
}
function Assert-KmcChargeBuffDrained($Lease) {
 if($null-eq$Lease-or$Lease.buffAcquisitionStarted-ne$true-or$Lease.buffAcquisitionObserved-ne$true-or
    $Lease.buffOutstanding-ne$false-or$null-ne$Lease.buffCallbackDebt-or$Lease.buffRuleDispatchSettled-ne$true){ChargeSaveFail 'native Charge buff acquisition or dispatch cleanup is unproven'}
 $types=@('Kingmaker.UnitLogic.FactLogic.AddStatBonus','Kingmaker.UnitLogic.FactLogic.AddCondition','Kingmaker.Designers.Mechanics.Facts.AttackOfOpportunityAttackBonus')
 Assert-KmcChargeBuffGraphDrained $Lease
 if($Lease.buffChildren.surface-ceq'native-cotw-1.14.4c-2.1'){
  $types=@('Kingmaker.UnitLogic.Mechanics.Components.AddFactContextActions')+$types+@('Kingmaker.UnitLogic.FactLogic.AddContextStatBonus','Kingmaker.UnitLogic.Mechanics.Components.ContextRankConfig')
 }
 if((@($Lease.buffComponentTypes)-join'|')-cne($types-join'|')){ChargeSaveFail 'native Charge buff component inventory differs'}
 $condition=$Lease.buffCondition
 foreach($field in @('condition','contributions','additions','removals','nativeExceptions')){
  if($null-eq$condition-or($condition.$field-isnot[int]-and$condition.$field-isnot[long])){ChargeSaveFail 'condition summary lacks exact integers'}
 }
 if($null-eq$condition-or$condition.componentObserved-ne$true-or$condition.condition-ne40-or$condition.drained-ne$true-or
    $condition.contributions-ne0-or$null-ne$condition.fault-or$condition.additions-lt1-or$condition.additions-ne$condition.removals){ChargeSaveFail 'owned native condition contribution was not drained'}
 $operations=@($condition.operations);$adds=0;$removes=0;$exceptions=0
 if($operations.Count-lt2-or$operations.Count-gt16){ChargeSaveFail 'condition mutation observations are missing or unbounded'}
 foreach($operation in $operations){
  if($operation.addition-isnot[bool]-or$operation.returnedNormally-isnot[bool]-or$operation.completed-ne$true-or$operation.mutationObserved-ne$true-or
     ($operation.before-isnot[int]-and$operation.before-isnot[long])-or($operation.after-isnot[int]-and$operation.after-isnot[long])-or
     $operation.before-lt0-or$operation.before-gt127-or$operation.after-lt0-or$operation.after-gt127){ChargeSaveFail 'condition mutation marker or native counter is unproven'}
  if($operation.addition){$adds++;if($operation.after-ne($operation.before+1)){ChargeSaveFail 'native condition acquisition delta differs'}}
  else{$removes++;if($operation.after-ne($operation.before-1)-or$removes-gt$adds){ChargeSaveFail 'native condition release delta or ownership differs'}}
  if(-not$operation.returnedNormally){$exceptions++}
 }
 if($adds-ne$condition.additions-or$removes-ne$condition.removals-or$exceptions-ne$condition.nativeExceptions){ChargeSaveFail 'condition mutation summary differs from raw facts'}
}
function Assert-KmcChargeLifecycleRows($Request,$Rows,$GameResult) {
 if($Request.scenario-cne'persistence-p07-save'-or-not(Test-KmcChargeLifecycleCase $Request.persistenceCase)-or$Rows.Count-lt10-or$Rows.Count-gt28){ChargeSaveFail 'lifecycle case or bounded observations differ'}
 $initial=ChargeSaveRow $Rows 'initial'
 $before=ChargeSaveRow $Rows 'charge-lifecycle-before';$held=ChargeSaveRow $Rows 'charge-lifecycle-held'
 $retained=ChargeSaveRow $Rows 'charge-lifecycle-debt-retained';$drained=ChargeSaveRow $Rows 'charge-lifecycle-drained'
 $retryReady=ChargeSaveRow $Rows 'charge-lifecycle-retry-ready';$cleanupComplete=ChargeSaveRow $Rows 'charge-lifecycle-cleanup-complete'
 $done=ChargeSaveRow $Rows 'charge-lifecycle-complete';$live=ChargeSaveRow $Rows 'charge-live-before-boundary'
 $ready=ChargeSaveRow $Rows 'charge-readiness';$input=ChargeSaveRow $Rows 'charge-input'
 $rider=$initial.rider.Id;$mount=$initial.mount.Id;$target=$initial.detail.target
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider-ceq$mount-or[string]::IsNullOrEmpty($target)){ChargeSaveFail 'lifecycle actors are not distinct exact native actors'}
 foreach($row in $Rows){
  if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.checkpoint-cne$Request.persistenceCase-or
     $row.source-cne$Request.commit-or$row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or
     ($null-ne$row.rider-and$row.rider.Id-cne$rider)-or($null-ne$row.mount-and$row.mount.Id-cne$mount)-or
     $row.controls.DuplicateFactCount-ne0-or$row.native.tbSetting-ne$false-or$row.native.tbInitialized-ne$false){ChargeSaveFail 'lifecycle process, actor, mode or source identity changed'}
 }
 $previous=-1
 foreach($row in @($ready,$input,$live,$before,$held,$retained,$retryReady,$cleanupComplete,$drained,$done)){
  $index=[Array]::IndexOf($Rows,$row)
  if($index-le$previous){ChargeSaveFail 'lifecycle observation order differs'};$previous=$index
 }
 $rd=$ready.detail.charge.readiness
 if($rd.prepared-ne$true-or$rd.canAct-ne$true-or$rd.riderCommandsEmpty-ne$true-or$rd.mountCommandsEmpty-ne$true-or
    $rd.available-ne$true-or$rd.canTarget-ne$true-or$ready.rider.Standard-gt0.001-or$ready.rider.Move-gt0.001){ChargeSaveFail 'lifecycle input lacks a legitimate native action'}
 $click=$input.detail.charge.input;$shell=$live.detail.charge.shell
 if($click.clicked-ne$true-or$click.availableForCast-ne$true-or$click.abilityGuid-cne'd79eaec224a7a832e738eb81baef9d49'-or
    $shell.acted-ne$true-or$shell.executor-cne$rider-or$shell.target-cne$target-or$shell.abilityGuid-cne$click.abilityGuid-or
    $live.detail.charge.shellFullRound-ne$true-or$live.detail.charge.shellActionType-cne'Standard'-or
    $live.detail.charge.command.finished-ne$false-or$live.detail.approach.moving-ne$true-or$live.detail.charge.mountCharging-ne$true-or
    $live.detail.ordinaryAttacks-ne0){ChargeSaveFail 'lifecycle boundary did not interrupt a real committed moving charge'}
 $identity=$before.detail.charge.owner.identity
 if($before.detail.charge.owner.owned-ne$true-or$identity-ne$input.detail.charge.owner.identity){ChargeSaveFail 'lifecycle owner exchanged before boundary'}
 foreach($row in @($before,$held,$retained,$drained,$done)){
  $d=$row.detail
  if($d.riderId-cne$rider-or$d.mountId-cne$mount-or$d.riderAttacks-ne0-or$d.riderResolved-ne0-or$d.mountAttacks-ne0-or
     $d.charge.workerRunning-ne$false){ChargeSaveFail 'lifecycle identity, attack or worker activity differs'}
 }
 foreach($row in @($held,$retained)){
  $d=$row.detail;$o=$d.charge.owner
  if($d.sameWorld-ne$true-or$d.area-cne$d.sourceArea-or$d.loading-ne$false-or$d.snapshotCount-ne0-or
     $o.owned-ne$true-or$o.identity-ne$identity-or$o.state-cne'FaultedCleanup'-or
     $d.charge.faultFired-ne$true-or$d.charge.mountSpeedOverride-le0-or[string]$o.debt-notmatch'mount-speed-override'-or
     $d.enabled-ne$true-or$d.patchesInstalled-ne$true){ChargeSaveFail 'boundary did not refuse with exact retained cleanup debt and intact world/services'}
 }
 Assert-KmcChargeNativeRemainder $live.rider $retained.rider (($retained.gameTicks-$live.gameTicks)/10000000.0)
 Assert-KmcChargeNativeRemainder $live.mount $retained.mount (($retained.gameTicks-$live.gameTicks)/10000000.0)
 foreach($row in @($live,$retained,$retryReady,$cleanupComplete)) { Assert-KmcChargeCommitment $row.detail.charge.costMax }
 if($retryReady.detail.sameWorld-ne$true-or$cleanupComplete.detail.sameWorld-ne$true-or
    $retryReady.detail.charge.owner.identity-ne$identity-or$cleanupComplete.detail.charge.owner.owned-ne$false-or
    $cleanupComplete.detail.charge.lastDrained.identity-ne$identity){ChargeSaveFail 'successful cleanup retry lacks its old world and exact owner'}
 Assert-KmcChargeDrained $cleanupComplete.detail.charge.lastDrained
 foreach($actor in @('rider','mount')){
  $a=@($retryReady.detail.charge.actors|Where-Object {$_.id-ceq$retryReady.$actor.Id})
  $b=@($cleanupComplete.detail.charge.actors|Where-Object {$_.id-ceq$retryReady.$actor.Id})
  if($a.Count-ne1-or$b.Count-ne1-or$a[0].prepared-ne$b[0].prepared-or$a[0].inCombat-ne$b[0].inCombat){ChargeSaveFail 'cleanup retry crossed an unobserved native actor boundary'}
  Assert-KmcChargeNativeRemainder $retryReady.$actor $cleanupComplete.$actor (($cleanupComplete.gameTicks-$retryReady.gameTicks)/10000000.0)
 }
 foreach($row in @($drained,$done)){
  $c=$row.detail.charge
  Assert-KmcChargeDrained $c.lastDrained
  if($c.owner.owned-ne$false-or$c.lastDrained.identity-ne$identity-or$c.lastDrained.rider-cne$rider-or$c.lastDrained.mount-cne$mount-or
     $c.lastDrained.target-cne$target-or$c.lastDrained.processObserved-ne$true-or$c.shellProcessEnded-ne$true-or
     $c.faultObserved-ne$true){ChargeSaveFail 'lifecycle retry did not drain the exact committed owner'}
  if($null-ne$row.rider-and($c.chargeBuffCount-ne0-or$c.mountCharging-ne$false-or$c.riderCharging-ne$false-or
     $null-ne$c.mountSpeedOverride-or$c.mountPathPresent-ne$false-or$c.mountMoving-ne$false-or
     $null-ne$c.riderStandardSlot-or$null-ne$c.mountMoveSlot-or$c.riderCommandsEmpty-ne$true-or$c.mountCommandsEmpty-ne$true)){ChargeSaveFail 'lifecycle left native command/path/lease residue'}
 }
 if($done.detail.charge.nativeBoundaries.pending-ne$false-or$null-ne$done.detail.charge.nativeBoundaries.fault){ChargeSaveFail 'native boundary remained pending or faulted'}
 switch -CaseSensitive ($Request.persistenceCase){
  'mounted-charge-area' {
   $retry=ChargeSaveRow $Rows 'charge-lifecycle-area-retry';$d=$done.detail
   if($held.detail.areaRefused-lt1-or$held.detail.request.method-cne'Game.ReloadArea:06000CD6'-or$d.loadingObserved-ne$true-or
      $d.sameWorld-ne$true-or$d.area-cne$d.sourceArea-or$d.snapshotCount-ne0){ChargeSaveFail 'native area refusal/retry/reload was not observed'}
   $carry=$retry.detail.request.carryEligibleAtRetry
   if($carry-ne$true-and$carry-ne$false){ChargeSaveFail 'area carry eligibility was not observed'}
   if($carry){if($d.relationship-cne'Mounted'-or$d.areaSuspensions-ne1-or$d.areaResumes-ne1){ChargeSaveFail 'settled exploration carry differs'}}
   elseif($d.relationship-cne'Unmounted'-or$d.areaSuspensions-ne0-or$d.areaResumes-ne0){ChargeSaveFail 'combat area departure resurrected a relationship'}
  }
  'mounted-charge-session' {
   if($held.detail.resetPending-ne$true-or$held.detail.resetDeferred-lt1-or$held.detail.request.method-cne'Game.ResetToMainMenu:06000CDD'-or
      $null-ne$done.detail.area-or$done.detail.resetPending-ne$false-or$done.detail.relationship-cne'Unmounted'-or
      $done.detail.fixtureReleased-ne$true-or$done.detail.snapshotCount-ne0-or$null-ne$done.rider-or$null-ne$done.mount){ChargeSaveFail 'native session departure did not defer and settle without a world or charge'}
  }
  'mounted-charge-disable' {
   $disabled=ChargeSaveRow $Rows 'charge-lifecycle-disabled'
   if($held.detail.request.disableAccepted-ne$false-or$held.detail.request.unloadAccepted-ne$false-or
      $disabled.detail.request.settledDisableAccepted-ne$true-or$disabled.detail.enabled-ne$false-or$disabled.detail.relationship-cne'Unmounted'-or
      $done.detail.request.reenableAccepted-ne$true-or$done.detail.enabled-ne$true-or$done.detail.relationship-cne'Unmounted'-or
      $done.detail.patchesInstalled-ne$true-or$done.detail.snapshotCount-ne0){ChargeSaveFail 'registered UMM refusal/disable/reenable contract differs'}
  }
  'mounted-charge-removal' {
   [void](ChargeSaveRow $Rows 'charge-removal-opening-write');$d=$done.detail
   $assessment=$retained.detail.request.retainedDebtAssessment
   if($held.detail.request.removalAccepted-ne$false-or$held.detail.request.nativeStopSent-ne$true-or
      $held.detail.request.nativeStopRefused-ne$true-or$held.detail.request.nativeStopError-notlike'Mounted charge cleanup remains owned:*'-or
      $assessment.safe-ne$false-or$assessment.activeCommand-ne$false-or$assessment.ownedCharge-ne$true-or$assessment.worldActiveCommand-ne$true-or
      $d.removalState-cne'Ready'-or$d.cleanupSaves-ne1-or$d.cleanupLeaf-cne'Manual_301_KMC_CLEANUP.zks'-or
      $d.cleanupBinding-cne'bound'-or$d.cleanupScannedMembers-lt1-or@($d.cleanupReferences).Count-ne0-or
      $d.snapshotCount-ne2-or$d.relationship-cne'Unmounted'-or$d.fixtureReleased-ne$true){ChargeSaveFail 'removal readiness lacks its own drained charge and exact cleanup archive'}
  }
 }
}
function Assert-KmcChargeLifecycleEvidence($Request,$Rows,$GameResult) {
 Assert-KmcChargeLifecycleRows $Request $Rows $GameResult
 if($Request.persistenceCase-ceq'mounted-charge-removal'){
  $root=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games')
  $first=(ChargeSaveRow $Rows 'charge-removal-opening-write').detail
  $done=(ChargeSaveRow $Rows 'charge-lifecycle-complete').detail
  if($first.path-cne(Join-Path $root 'Manual_300_KMC_P01.zks')-or(Get-KmcSha256 $first.path)-cne$first.sha256-or
     (Get-Item -LiteralPath $first.path).Length-ne$first.length-or$done.cleanupPath-cne(Join-Path $root 'Manual_301_KMC_CLEANUP.zks')-or
     (Get-KmcSha256 $done.cleanupPath)-cne$done.cleanupSha256){ChargeSaveFail 'removal archive changed after its own completed write'}
  $archive=Read-KmcCampaignArchiveMembers $done.cleanupPath
  if($archive.kmc.Mounted-ne$false-or$null-ne$archive.kmc.Combat-or$null-ne$archive.kmc.Rider-or$null-ne$archive.kmc.Mount-or
     @($archive.kmc.Slots).Count-ne0-or$archive.header.GameId-cne$Request.fixture.working.gameId){ChargeSaveFail 'removal archive serialized a pair or combat transaction'}
 }
}
function ConvertTo-KmcChargeCanonical($Value) {
 if($null-eq$Value){return 'null'}
 if($Value-is[pscustomobject]){
  $parts=@($Value.PSObject.Properties|Sort-Object Name|ForEach-Object {($_.Name|ConvertTo-Json -Compress)+':'+(ConvertTo-KmcChargeCanonical $_.Value)})
  return '{'+($parts-join',')+'}'
 }
 if($Value-is[Array]){return '['+(@($Value|ForEach-Object {ConvertTo-KmcChargeCanonical $_})-join',')+']'}
 return ($Value|ConvertTo-Json -Depth 80 -Compress)
}
function ChargeSaveRow($Rows,[string]$Kind) {
 $found=@($Rows|Where-Object kind -CEQ $Kind)
 if($found.Count-ne1){ChargeSaveFail ('expected one '+$Kind+' row')}
 $found[0]
}
function Assert-KmcChargeNativeRemainder($Before,$After,[double]$Elapsed) {
 if($Elapsed-lt0-or$Before.Id-cne$After.Id-or$Before.ReactionsRemaining-ne$After.ReactionsRemaining-or$Before.LastSurpriseTicks-ne$After.LastSurpriseTicks){ChargeSaveFail 'native actor/reaction continuity differs'}
 $slack=0.001+[Math]::Max(0,$Elapsed)*0.1
 foreach($field in @('Standard','Move','Swift','Initiative','Reaction')){
  $a=[double]$Before.$field;$b=[double]$After.$field
  if([double]::IsNaN($a)-or[double]::IsInfinity($a)-or[double]::IsNaN($b)-or[double]::IsInfinity($b)-or$b-gt$a+0.001-or$b+$Elapsed+$slack-lt$a){ChargeSaveFail ('native action debt changed outside clock: '+$field)}
 }
}
function Assert-KmcChargeDrained($Owner,[bool]$Committed=$true) {
 if($null-eq$Owner-or$Owner.state-cne'FullyDrained'-or$Owner.attempts-lt1-or$Owner.committed-isnot[bool]-or$Owner.committed-ne$Committed){ChargeSaveFail 'no fully drained exact owner with expected native commitment'}
 foreach($fact in @('commandTerminal','riderSlotReleased','riderContainerReleased','schedulerAbsent','carrierDrained','leaseDrained','shellTerminal','shellContainerReleased','processEnded','manualTargetReleased')){
  if($Owner.$fact-ne$true){ChargeSaveFail ('unresolved owner postcondition '+$fact)}
 }
 if($Owner.manualTargetOwned-isnot[bool]-or$Owner.manualTargetOwned-ne$false-or$Owner.manualTargetReleased-isnot[bool]){ChargeSaveFail 'native manual attack target remains owned or unobserved'}
 if(-not[string]::IsNullOrEmpty([string]$Owner.debt)){ChargeSaveFail 'retained lease debt'}
 if($null-eq$Owner.PSObject.Properties['lease']){ChargeSaveFail 'drained owner omitted exact lease facts'}
 if($null-ne$Owner.lease){
  if($Owner.lease.buffAcquisitionStarted-eq$true){Assert-KmcChargeBuffDrained $Owner.lease}
  elseif($Owner.lease.buffAcquisitionStarted-ne$false-or$Owner.lease.buffApplied-ne$false-or$Owner.lease.buffAcquisitionObserved-ne$false-or
         $Owner.lease.buffOutstanding-ne$false-or$null-ne$Owner.lease.buffCondition){ChargeSaveFail 'unstarted buff acquisition has unexplained native ownership'}
 }
}
function Assert-KmcChargeActionFailure($Fault,$Owner,[bool]$Registered) {
 $live=$Fault.ownerAtFault
 foreach($field in @('owned','committed','nativeActionInProgress','nativeRuleObserved','nativeExecutorObserved','shellProcessAssigned','ruleProcessAssigned','processObserved')){
  if($live.$field-isnot[bool]){ChargeSaveFail ('native action fault omitted boolean '+$field)}
 }
 foreach($field in @('nativeActionFailed','nativeActionInProgress','processObservationPending','processObserved','shellProcessAssigned')){
  if($Owner.$field-isnot[bool]){ChargeSaveFail ('drained native action omitted boolean '+$field)}
 }
 foreach($field in @('rider','mount','target')){
  if([string]::IsNullOrEmpty([string]$Owner.$field)-or$live.$field-cne$Owner.$field){ChargeSaveFail 'native action fault changed its original actors'}
 }
 if($live.identity-isnot[int]-or$Owner.identity-isnot[int]-or$live.processCount-isnot[int]-or$Owner.processCount-isnot[int]-or$null-ne$Owner.lease){ChargeSaveFail 'native action fault acquired unexpected charge ownership'}
 foreach($field in @('shellIdentity','ruleIdentity','executorIdentity','contextIdentity')){
  if($live.$field-isnot[int]-or$Owner.$field-isnot[int]-or$live.$field-ne$Owner.$field){ChargeSaveFail ('native action changed exact '+$field)}
 }
 $beforeProcesses=@($live.processes);$afterProcesses=@($Owner.processes)
 if($beforeProcesses.Count-ne$live.processCount-or$afterProcesses.Count-ne$Owner.processCount){ChargeSaveFail 'native action omitted registered process identities'}
 if($Registered-and($beforeProcesses.Count-ne1-or$beforeProcesses[0].identity-isnot[int]-or$afterProcesses[0].identity-isnot[int]-or
    $beforeProcesses[0].identity-ne$afterProcesses[0].identity-or$beforeProcesses[0].contextIdentity-ne$live.contextIdentity-or
    $afterProcesses[0].contextIdentity-ne$Owner.contextIdentity-or$beforeProcesses[0].ended-ne$false-or$afterProcesses[0].ended-ne$true)){
  ChargeSaveFail 'native action process identity, context or completion changed'
 }
 if($Fault.armed-ne$true-or$Fault.fired-ne$true-or$Fault.seamCleared-ne$true-or
    $Fault.boundary-cne$(if($Registered){'after-rule'}else{'before-rule'})-or
    $live.identity-ne$Owner.identity-or$live.owned-ne$true-or$live.committed-ne$false-or
    $live.nativeActionInProgress-ne$true-or$live.nativeRuleObserved-ne$true-or$live.nativeExecutorObserved-ne$true-or
    $live.shellProcessAssigned-ne$false-or$live.ruleProcessAssigned-ne$Registered-or
    $live.processObserved-ne$Registered-or$live.processCount-ne$(if($Registered){1}else{0})-or
    $Owner.nativeActionFailed-ne$true-or$Owner.nativeActionInProgress-ne$false-or
    $Owner.processObservationPending-ne$false-or$Owner.processObserved-ne$Registered-or
    $Owner.processCount-ne$(if($Registered){1}else{0})-or$Owner.shellProcessAssigned-ne$false-or
    -not[string]::IsNullOrEmpty([string]$Owner.processObservationError)){ChargeSaveFail 'exceptional native action ownership or registration boundary differs'}
 Assert-KmcChargeDrained $Owner $false
}
function Assert-KmcChargeCommitment($Cost) {
 foreach($field in @('riderStandard','riderMove','mountStandard','mountMove')){
  if($null-eq$Cost.$field-or[double]::IsNaN([double]$Cost.$field)-or[double]::IsInfinity([double]$Cost.$field)-or$Cost.$field-lt0){ChargeSaveFail 'nonfinite or negative native cost'}
 }
 if($Cost.riderStandard-lt5.8-or$Cost.riderStandard-gt6.001-or$Cost.riderMove-gt0.001-or$Cost.mountStandard-gt0.001-or$Cost.mountMove-gt0.001){ChargeSaveFail 'native RT charge commitment or mount cost differs'}
}
function Assert-KmcChargeSaveResidue($Actual,[switch]$Cold) {
 $c=$Actual.charge
 if($null-eq$c-or$c.owner.owned-ne$false-or$c.mountCharging-ne$false-or$c.riderCharging-ne$false-or
    $null-ne$c.mountSpeedOverride-or$c.chargeBuffCount-ne0-or$null-ne$c.riderStandardSlot-or$null-ne$c.mountMoveSlot-or
    $Actual.riderCommandsEmpty-ne$true-or$Actual.mountCommandsEmpty-ne$true-or$Actual.unresolvedAbilities-ne$false-or
    $Actual.unresolvedProjectiles-ne$false){ChargeSaveFail 'charge command, process, carrier, charging, speed or buff residue'}
 if($Cold){
  if($null-ne$c.shell-or$null-ne$c.command-or$null-ne$c.lastDrained-or$c.admitted-ne0-or$c.refused-ne0-or$Actual.resolved-ne0-or$Actual.ordinaryAttacks-ne0){ChargeSaveFail 'cold process resumed or recreated charge work'}
 }else{
  Assert-KmcChargeDrained $c.lastDrained
  if($c.shellProcessEnded-ne$true-or$c.lastDrained.processObserved-ne$true){ChargeSaveFail 'native shell process was unobserved or remained live'}
 }
}
function Assert-KmcChargePersistenceRows($Request,$Rows,$GameResult) {
 if(-not(Test-KmcChargePersistenceCase ([string]$Request.persistenceCase))-or$Request.scenario-cnotin @('persistence-p04-save','persistence-p04-load')){ChargeSaveFail 'case/scenario mismatch'}
 if($Rows.Count-lt7-or$Rows.Count-gt32){ChargeSaveFail 'unbounded or incomplete observations'}
 $source=$Request.scenario-ceq'persistence-p04-save'
 $initial=ChargeSaveRow $Rows 'initial'
 foreach($row in $Rows){
  if($row.runId-cne$Request.runId-or$row.scenario-cne$Request.scenario-or$row.checkpoint-cne$Request.persistenceCase-or
     $row.source-cne$Request.commit-or$row.dll-cne$Request.dllSha256-or$row.processId-ne$GameResult.processId-or
     $row.relationship-cne'Mounted'-or$row.rider.Id-cne$initial.rider.Id-or$row.mount.Id-cne$initial.mount.Id-or
     $row.controls.DuplicateFactCount-ne0-or$row.native.tbSetting-ne$false-or$row.native.tbInitialized-ne$false){ChargeSaveFail 'native identity, pair, mode or controls differ'}
 }
 $saved=ChargeSaveRow $Rows $(if($source){'native-write-complete'}else{'rt-cold-debt-restored'})
 $d=$saved.detail;$s=$d.snapshot;$actual=$d.actual
 $ids=@($s.Combat.Actors|ForEach-Object {$_.Native.Id})
 $observedIds=@($actual.charge.actors|ForEach-Object {$_.id})
 if([string]::IsNullOrEmpty($initial.rider.Id)-or$initial.rider.Id-ceq$initial.mount.Id-or
    $ids.Count-ne@($ids|Sort-Object -Unique).Count-or$observedIds.Count-ne2-or
    @($observedIds|Where-Object {$_-ceq$initial.rider.Id}).Count-ne1-or@($observedIds|Where-Object {$_-ceq$initial.mount.Id}).Count-ne1-or
    $s.Rider.Id-cne$initial.rider.Id-or$s.Mount.Id-cne$initial.mount.Id){ChargeSaveFail 'pair/snapshot actor identities are missing or duplicated'}
 $actors=@($s.Combat.Actors|Where-Object {$_.Native.Id-ceq$initial.rider.Id})
 if($actors.Count-ne1-or$s.Mounted-ne$true-or$s.Combat.TurnBased-ne$false-or$actors[0].Native.Standard-le0.1-or
    $null-ne$s.Combat.Current-or$s.Combat.Roster.Count-ne0-or($null-ne$s.Combat.Paired-and$null-ne$s.Combat.Paired.Activation)-or
    $s.CampaignId-cne$Request.fixture.working.gameId-or$s.AreaId-cne$Request.fixture.working.area){ChargeSaveFail 'snapshot lost real debt or invented a turn/pair'}
 # Exact schema remains charge-free; native residue is proved separately at the serialization barrier.
 if(@($s.PSObject.Properties.Name|Where-Object {$_-match 'charge|transaction|cleanupOwner'}).Count-ne0){ChargeSaveFail 'serialized charge ownership'}
 Assert-KmcChargeSaveResidue $actual -Cold:(-not$source)
 foreach($observed in @($actual.charge.actors)){
  $actor=@($s.Combat.Actors|Where-Object {$_.Native.Id-ceq$observed.id})
  if($actor.Count-ne1-or$actor[0].Prepared-ne$observed.prepared-or$actor[0].InCombat-ne$observed.inCombat){ChargeSaveFail 'native preparation or participation replayed'}
  Assert-KmcChargeNativeRemainder $actor[0].Native $observed.native (($saved.gameTicks-$s.GameTimeTicks)/10000000.0)
 }
 if(@($actual.charge.actors).Count-ne2){ChargeSaveFail 'both native actors were not observed'}
 if($source){
  $before=ChargeSaveRow $Rows 'charge-before-input';$input=ChargeSaveRow $Rows 'charge-input'
  $ready=(ChargeSaveRow $Rows 'charge-readiness').detail.charge.readiness
  if($ready.prepared-ne$true-or$ready.canAct-ne$true-or$ready.riderCommandsEmpty-ne$true-or$ready.mountCommandsEmpty-ne$true-or
     $ready.available-ne$true-or$ready.canTarget-ne$true){ChargeSaveFail 'native charge readiness was not established before the single input'}
  foreach($kind in @('rt-before-save','rt-native-save-requested')){[void](ChargeSaveRow $Rows $kind)}
  $click=$input.detail.charge.input
  if($before.rider.Standard-gt0.01-or$before.detail.charge.straightRoute-ne$true-or$before.detail.charge.landingBlocked-ne$false-or
     $click.clicked-ne$true-or$click.availableForCast-ne$true-or$click.abilityGuid-cne'd79eaec224a7a832e738eb81baef9d49'-or
     $input.detail.charge.owner.owned-ne$true){ChargeSaveFail 'no lawful native charge shell admission'}
  $last=$actual.charge.lastDrained
  $committed=ChargeSaveRow $Rows 'charge-commit-observed'
  $shell=$committed.detail.charge.shell
  if($shell.acted-ne$true-or$committed.rider.Standard-le0-or$shell.type-cne'UnitUseAbility'-or
     $shell.abilityGuid-cne'd79eaec224a7a832e738eb81baef9d49'-or$shell.executor-cne$initial.rider.Id-or$shell.target-cne$initial.detail.target-or
     $committed.detail.charge.shellActionType-cne'Standard'-or$committed.detail.charge.shellFullRound-ne$true){ChargeSaveFail 'no exact rider-owned full-round native commitment'}
  Assert-KmcChargeNativeRemainder $committed.rider $saved.rider (($saved.gameTicks-$committed.gameTicks)/10000000.0)
  Assert-KmcChargeNativeRemainder $committed.mount $saved.mount (($saved.gameTicks-$committed.gameTicks)/10000000.0)
  if($last.identity-ne$input.detail.charge.owner.identity-or$last.rider-cne$initial.rider.Id-or$last.mount-cne$initial.mount.Id-or$last.target-cne$initial.detail.target){ChargeSaveFail 'cleanup owner identity changed'}
  $barrier=$actual.charge.snapshot
  if($null-eq$barrier-or$barrier.owner.owned-ne$false-or$barrier.riderCommandsEmpty-ne$true-or$barrier.mountCommandsEmpty-ne$true-or
     $barrier.unresolvedAbilities-ne$false-or$barrier.gameTicks-ne$s.GameTimeTicks){ChargeSaveFail 'half-owned native snapshot'}
  Assert-KmcChargeDrained $barrier.lastDrained
  Assert-KmcChargeCommitment $actual.charge.costMax
  $expected=if($Request.persistenceCase-ceq'mounted-charge-settled'){1}else{0}
  if($actual.resolved-ne$expected-or$actual.ordinaryAttacks-ne$expected-or$actual.forcedD20-ne0){ChargeSaveFail 'charge delivery duplicated or occurred after interruption'}
  $attacks=@($actual.rules.attackRuleEvents|Where-Object {$_.attackOfOpportunity-eq$false})
  if($attacks.Count-ne$expected-or$actual.rules.mountAttackRules-ne0){ChargeSaveFail 'unexpected rider or mount attack'}
  if($expected-eq1-and($attacks[0].charge-ne$true-or$attacks[0].fullAttack-ne$false-or$attacks[0].actorId-cne$initial.rider.Id)){ChargeSaveFail 'settled delivery was not one rider-owned native charge attack'}
  if($Request.persistenceCase-cne'mounted-charge-failed'-and$Request.persistenceCase-cne'mounted-charge-settled'){
   $live=ChargeSaveRow $Rows 'charge-live-before-boundary'
   if($live.detail.charge.shell.acted-ne$true-or$live.detail.charge.command.finished-ne$false-or$live.detail.approach.moving-ne$true-or
      $live.detail.charge.mountCharging-ne$true-or$live.detail.ordinaryAttacks-ne0-or$live.detail.snapshotCount-ne0){ChargeSaveFail 'boundary was not a real pending charge'}
  }
  if($Request.persistenceCase-ceq'mounted-charge-cancelled'-and$actual.charge.nativeStopSent-ne$true){ChargeSaveFail 'no native Stop cancellation'}
  if($Request.persistenceCase-ceq'mounted-charge-failed'-and$actual.charge.faultFired-ne$true){ChargeSaveFail 'post-commit fault never fired'}
  if($Request.persistenceCase-ceq'mounted-charge-drained'){
   $held=ChargeSaveRow $Rows 'charge-cleanup-debt-held';$h=$held.detail
   if($h.charge.faultFired-ne$true-or$h.charge.owner.owned-ne$true-or$h.charge.owner.state-cne'FaultedCleanup'-or
      [string]$h.charge.owner.debt-notmatch'mount-speed-override'-or$h.snapshotCount-ne0-or$h.charge.workerRunning-ne$false-or
      $h.charge.callback-ne$false-or$h.charge.mountSpeedOverride-le0-or$barrier.faultObserved-ne$true){ChargeSaveFail 'fault did not retain observable debt and block capture until retry'}
  }
 }else{
  if($initial.persistence.semantics-ne$s.Combat.Actors.Count-or$initial.persistence.presentation-ne1-or$initial.controls.NativeCastRequestCount-ne0){ChargeSaveFail 'cold restoration replayed preparation or Mount'}
 }
 $queued=ChargeSaveRow $Rows 'rt-spent-attack-queued';$wait=ChargeSaveRow $Rows 'rt-native-debt-wait'
 $later=@($Rows|Where-Object kind -CEQ 'rt-later-attack');$done=ChargeSaveRow $Rows 'usable-continuation-complete'
 if($later.Count-ne2-or$wait.detail.resolved-ne$queued.detail.resolved-or$wait.detail.riderRounds-ne$queued.detail.riderRounds){ChargeSaveFail 'ordinary continuation skipped debt or replayed preparation'}
 for($i=0;$i-lt2;$i++){
  if($later[$i].detail.resolved-ne($queued.detail.resolved+$i+1)-or$later[$i].detail.riderRounds-ne($queued.detail.riderRounds+$i+1)-or$later[$i].detail.forcedD20-ne0){ChargeSaveFail 'later native round or attack duplicated'}
 }
 if($done.detail.charge.owner.owned-ne$false-or$done.detail.resolved-ne$queued.detail.resolved+2){ChargeSaveFail 'continuation retained a charge owner or duplicate attack'}
}
function Assert-KmcChargePersistenceEvidence($Request,$Rows,$GameResult) {
 Assert-KmcChargePersistenceRows $Request $Rows $GameResult
 if($Request.scenario-ceq'persistence-p04-save'){
  $d=(ChargeSaveRow $Rows 'native-write-complete').detail
  $path=Join-Path (Get-KmcLabRoot) ('runtime-staging/persistence-'+$Request.runId+'/Saved Games/Manual_300_KMC_P01.zks')
  if($d.path-cne$path-or$d.nativeType-cne'Manual'-or$d.nativeCallback-ne$true-or$d.operation-cne'None'-or
     (Get-KmcSha256 $path)-cne$d.sha256-or(Get-Item -LiteralPath $path).Length-ne$d.length){ChargeSaveFail 'actual native archive identity differs'}
  Assert-KmcChargeArchiveSnapshot $path $d.sha256 $d.length $d.snapshot
 }
}
function Assert-KmcChargeArchiveSnapshot([string]$Path,[string]$Sha,[long]$Length,$Snapshot) {
 if((Get-KmcSha256 $Path)-cne$Sha-or(Get-Item -LiteralPath $Path).Length-ne$Length){ChargeSaveFail 'archive identity changed before snapshot inspection'}
 Add-Type -AssemblyName System.IO.Compression
 $stream=$null;$archive=$null;$reader=$null
 try {
  $stream=[IO.FileStream]::new($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
  $archive=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Read,$false)
  $entries=@($archive.Entries|Where-Object FullName -CEQ 'kmc-mounted-state')
  if($entries.Count-ne1-or$entries[0].Length-le0-or$entries[0].Length-gt128KB){ChargeSaveFail 'archive snapshot missing, duplicated or oversized'}
  $reader=[IO.StreamReader]::new($entries[0].Open(),[Text.UTF8Encoding]::new($false,$true))
  $json=$reader.ReadToEnd()
  Assert-KmcJsonObjectMembersUnique -Json $json -Description 'charge archive snapshot'
  if((ConvertTo-KmcChargeCanonical ($json|ConvertFrom-Json))-cne(ConvertTo-KmcChargeCanonical $Snapshot)){ChargeSaveFail 'hashed archive snapshot differs from observed snapshot'}
 }finally{if($reader){$reader.Dispose()};if($archive){$archive.Dispose()};if($stream){$stream.Dispose()}}
 if((Get-KmcSha256 $Path)-cne$Sha){ChargeSaveFail 'archive changed during snapshot inspection'}
}
function Assert-KmcChargeColdOutcome($SourceRows,$ColdRows,$Load) {
 $saved=ChargeSaveRow $SourceRows 'native-write-complete';$cold=ChargeSaveRow $ColdRows 'rt-cold-debt-restored'
 if($Load.sha256-cne$saved.detail.sha256-or$Load.length-ne$saved.detail.length){ChargeSaveFail 'cold archive differs from immutable native write'}
 if($saved.processId-eq$cold.processId-or$saved.checkpoint-cne$cold.checkpoint-or
    (ConvertTo-KmcChargeCanonical $saved.detail.snapshot)-cne(ConvertTo-KmcChargeCanonical $cold.detail.snapshot)-or
    $saved.rider.Id-cne$cold.rider.Id-or$saved.mount.Id-cne$cold.mount.Id-or
    $saved.detail.actual.target-cne$cold.detail.actual.target-or$saved.detail.actual.targetDamage-ne$cold.detail.actual.targetDamage){ChargeSaveFail 'cold process did not load the exact settled actors/target/charge outcome'}
 Assert-KmcChargeSaveResidue $cold.detail.actual -Cold
}
