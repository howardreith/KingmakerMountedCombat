param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-Chunk6aFullTbFixture.ps1') -Configuration $Configuration
. (Join-Path $PSScriptRoot 'runtime/Chunk6aAdditionalEvidence.ps1')
. (Join-Path $PSScriptRoot 'Test-NativeGroundTransaction.ps1') -TransactionFunctionsOnly -Configuration $Configuration
Import-TbFixtureFunctions 'scripts/Test-Chunk6aCausalProtocol.ps1' @('New-ActingSetupCase')
$parent=$PSScriptRoot
function Import-OrderFactory([string]$File,[string]$Name,[scriptblock]$Transform){
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $parent $File),[ref]$tokens,[ref]$errors)
 $f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($f.Count-ne1){throw $Name}
 $text=$f[0].Extent.Text;if($null-ne$Transform){$text=& $Transform $text}
 . ([scriptblock]::Create($text.Replace(('function '+$Name),('function script:'+$Name))))
}
Import-OrderFactory 'Test-NativeAllocationContinuation.ps1' 'Copy-Continuation' $null
Import-OrderFactory 'Test-NativeAllocationContinuation.ps1' 'New-Continuation' {
 param($s)
 $start=$s.IndexOf(' $r=[pscustomobject]');$end=$s.IndexOf(' $script:cresources=', $start)
 if($start-lt0-or$end-lt0){throw 'Continuation resource fixture anchor'}
 $s=$s.Substring(0,$start)+' $r=Copy-Continuation $script:orderStarting.rider;$m=Copy-Continuation $script:orderStarting.mount'+[Environment]::NewLine+$s.Substring($end)
 $s=$s.Replace('$s.standard=6.0;$s.swift=6.0;$s.move=4.2','$s.standard=[Math]::Max(6.0,$s.standard);$s.swift=[Math]::Max(6.0,$s.swift);$s.move=[Math]::Max(3.0,$s.move)').Replace('else{$s.standard=6.0}','else{$s.standard=[Math]::Max(6.0,$s.standard)}')
 $s=$s.Replace('$s=$script:cresources[$actor];$s.initiativeCooldown=0.0;$s.reactionCooldown=0.0','$s=$script:cresources[$actor];$decay=6.0-[Math]::Min(6.0,[double]$s.initiativeCooldown);$s.initiativeCooldown=[Math]::Max(0.0,$s.initiativeCooldown-6.0);$s.reactionCooldown=[Math]::Max(0.0,$s.reactionCooldown-$decay)').Replace('$s.$f-5.8','$s.$f-$decay')
 $s
}
Import-OrderFactory 'Test-NativeMountOrder.ps1' 'New-Order' {param($s)$s.Replace('$actor|Add-Member nativePrepareCount $actor.grantSequence','$actor|Add-Member -Force nativePrepareCount $actor.grantSequence').Replace('$actor|Add-Member nativeTurnObject $(','$actor|Add-Member -Force nativeTurnObject $(')}
function Set-TraceOffset($Value,[long]$SequenceOffset,[int]$FrameOffset,[long]$TickOffset){
 if($null-eq$Value){return}
 if($Value -is [array]){foreach($v in $Value){Set-TraceOffset $v $SequenceOffset $FrameOffset $TickOffset};return}
 if($Value -isnot [pscustomobject]){return}
 foreach($property in @($Value.PSObject.Properties)){
  if($property.Name-cin@('sequence','allocationSequence')){$property.Value=[long]$property.Value+$SequenceOffset}
  elseif($property.Name-ceq'frame'){$property.Value=[int]$property.Value+$FrameOffset}
  elseif($property.Name-ceq'gameTicks'){$property.Value=[long]$property.Value+$TickOffset}
  else{Set-TraceOffset $property.Value $SequenceOffset $FrameOffset $TickOffset}
 }
}
function New-ExactExplorationProof([bool]$Dismount,[int]$Sequence){
 $p=New-CommandProof $false $false;$id=$p.identity;$offset=if($Dismount){2000}else{1000}
 $id.commandObject+=$offset;$id.processObject+=$offset;$id.contextObject+=$offset;$id.controlIdentity='exploration-'+$offset;$id.generationAtInit=if($Dismount){6}else{5}
 $id.abilityGuid=if($Dismount){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'};$id.targetId=if($Dismount){'rider'}else{'mount'}
 Put-Value $p window $(if($Dismount){'exploration-dismount'}else{'exploration-mount'})
 $p.preClick.allocationSequence=$Sequence;$p.preClick.gameTicks=10000L+$offset;Put-Value $p.preClick frame $(if($Dismount){20}else{10});$p.preClick.state.generation=$id.generationAtInit
 foreach($role in @('rider','mount')){foreach($f in @('standard','move','swift')){$p.preClick.state.$role.$f=0.0}}
 foreach($s in $p.samples){$s.identity=Copy-Value $id;if($s.boundary-cin@('init','click-admission','move-slot-installation','approach-start')){$s.identity.processObject=0;$s.identity.contextObject=0};$s.deliveryContext=if($s.boundary-ceq'deliver'){$id.contextObject}else{0};$s.gameTicks=$p.preClick.gameTicks;Put-Value $s frame $p.preClick.frame;$s.state=Copy-Value $p.preClick.state;$s.allocationSequence=$Sequence;if($s.boundary-ceq'terminal'){$s.state.generation++;$s.allocationSequence+=2}}
 $n=$Sequence;foreach($e in $p.resourceWindow.events){$e.command=$id.commandObject;$e.sequence=++$n;$e.gameTicks=$p.preClick.gameTicks;Put-Value $e frame $p.preClick.frame;foreach($f in @('standard','move','swift')){$e.state.$f=0.0}}
 Assert-KmcRelationshipCommandProof $p $false $false 0 $false 0 $true;$p
}
function New-OrderEnvelope([bool]$First){
 $proof=New-FullTbProof $First;$terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal')[0]
 foreach($role in @('rider','mount')){
  Put-Value $terminal.state.$role actor $role;Put-Value $terminal.state.$role actorObject $(if($role-ceq'rider'){101}else{102});Put-Value $terminal.state.$role inCombat $true
  Put-Value $terminal.state.$role grantSequence 1;Put-Value $terminal.state.$role waitingInitiative $false;Put-Value $terminal.state.$role nativePrepareCount 1;Put-Value $terminal.state.$role nativeTurnObject $(if($role-ceq'rider'){501}elseif($First){502}else{0})
 }
 $script:orderStarting=[pscustomobject]@{rider=(Copy-Value $terminal.state.rider);mount=(Copy-Value $terminal.state.mount)}
 $order=New-Order $First
 $count=@($proof.resourceWindow.events).Count+40
 # The first ten observations reserve exploration and actual pre-pair turn order.
 $pathCopy=Copy-Value $proof.nativeApproachPath;Set-TraceOffset $proof 50 0 4000000L;Set-TraceOffset $pathCopy 0 0 4000000L;$proof.nativeApproachPath=$pathCopy
 $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal')[0]
 # Move only the continuation interval and its copies to the Mount terminal.
 foreach($key in @('mounted','beforeEndInput','afterEndInput','nextRound','completion','continuation','terminalBridge')){Set-TraceOffset $order.$key $count 4 8000000L}
 foreach($turn in $order.turns){Set-TraceOffset $turn $count 4 4000000L}
 # Mounted is shared with turns in the original fixture; normalize from authoritative continuation.
 foreach($key in @('mounted','beforeEndInput','afterEndInput')){$order.$key.frame=$terminal.frame;$order.$key.gameTicks=$terminal.gameTicks;$order.$key.allocationSequence=$terminal.allocationSequence}
 $order.nextRound.frame=$order.continuation.after.frame;$order.nextRound.gameTicks=$order.continuation.after.gameTicks;$order.nextRound.allocationSequence=$order.continuation.after.allocationSequence
 $order.positiveProof=$proof
 Put-Value $terminal state (Copy-Value $terminal.state)
 Put-Value $terminal.state relationshipState 'Mounted';Put-Value $terminal.state ledger (Copy-Value $order.mounted.state.ledger)
 Put-Value $terminal nativeAllocation ([pscustomobject]@{rider=(Copy-Value $script:orderStarting.rider);mount=(Copy-Value $script:orderStarting.mount)})
 $order.terminalBridge.before.frame=$terminal.frame;$order.terminalBridge.before.gameTicks=$terminal.gameTicks;$order.terminalBridge.before.allocationSequence=$terminal.allocationSequence
 $order.terminalBridge.after=Copy-Value $order.terminalBridge.before
 $prefix=@();$windows=@();$seq=0
 foreach($window in @('exploration-mount','exploration-dismount')){
  $explore=New-ExactExplorationProof ($window-ceq'exploration-dismount') $seq
  foreach($e in $explore.resourceWindow.events){Put-Value $e round 0};$prefix+=@($explore.resourceWindow.events);$seq+=2;$windows+=@($explore)
 }
 $ground=Copy-Value (New-GroundTransaction)
 foreach($boundary in @('before','after')){$ground.$boundary.generation=7}
 $localPath=Copy-Value $ground.path;Set-TraceOffset $ground -6 -50 0;$ground.path=$localPath
 # The allocation sequence moved from10 to4; ground path sequence is independently local.
 $ground.frameBefore=$ground.resourceWindow.before.frame;$ground.frameAfter=$ground.resourceWindow.after.frame
 Put-Value $ground pass $true;$foot=$ground.plan.candidates[$ground.plan.selectedIndex].footprint
 foreach($navigation in @('originNavigation','terminalNavigation')){Put-Value $ground.$navigation footprint $foot;Put-Value $ground.$navigation actingClearance $foot}
 foreach($e in $ground.resourceWindow.events){Put-Value $e round 0};$ground.events=@($ground.resourceWindow.events|ForEach-Object{Copy-Value $_});$prefix+=@($ground.resourceWindow.events);$seq=$ground.resourceWindow.after.allocationSequence
 if(-not$First){foreach($boundary in @('prepare-before','prepare-after','turn-end-after')){$prefix+=@([pscustomobject]@{sequence=(++$seq);boundary=$boundary;round=1;state=@{actor='mount'}})}}
 foreach($boundary in @('prepare-before','prepare-after')){$prefix+=@([pscustomobject]@{sequence=(++$seq);boundary=$boundary;round=1;state=@{actor='rider'}})}
 while($seq-lt50){$prefix+=@([pscustomobject]@{sequence=(++$seq);boundary='fixture-observation';round=1;state=@{actor='fixture'}})}
 foreach($e in $proof.resourceWindow.events){Put-Value $e round 1}
 $order.allocationTrace.events=@($prefix)+@($proof.resourceWindow.events)+@($order.continuation.events)
 $order.completion=Copy-Value $order.continuation.completion
 # Actual observed turn order, then exact adopted turn and next pair.
 $turns=@();if(-not$First){$earlier=Copy-Value $order.mounted;$earlier.currentActor='mount';$earlier.allocationSequence=($ground.resourceWindow.after.allocationSequence+2);$turns+=@($earlier)};$turns+=@((Copy-Value $order.mounted),(Copy-Value $order.nextRound));$order.turns=$turns
 Put-Value $proof window 'positive-mount';Put-Value $order.mountBefore kind 'mount-before'
 $baseline=$order.mountBefore
 foreach($pair in @(@('relationshipGeneration',7),@('dispatchAccepted',2),@('acceptedMountCount',1),@('forcedDetachCount',0),@('mountTurnsWhileMounted',0))){Put-Value $baseline $pair[0] $pair[1]}
 $mountAfter=Copy-Value $baseline;Put-Value $mountAfter kind 'mount-after';$mountAfter.relationshipGeneration=8;$mountAfter.dispatchAccepted=3;$mountAfter.acceptedMountCount=2
 foreach($pair in @(@('relationshipState','Mounted'),@('pairedSequence',1),@('adoptionCount',1))){Put-Value $mountAfter $pair[0] $pair[1]};$mountAfter.mount.nativePrepareCount=1
 $setup=(New-ActingSetupCase).setup
 foreach($phase in @('before','after')){$setup.$phase.relationshipGeneration=7;$setup.$phase.mount.nativePrepareCount=$(if($First){0}else{1})}
 Put-Value $proof.preClick.state.rider nativePrepareCount 1;Put-Value $proof.preClick.state.mount nativePrepareCount $(if($First){0}else{1})
 Put-Value $order.allocationTrace observerHooks $ground.resourceWindow.observerHooks
 $without=Copy-Value $order;$without.PSObject.Properties.Remove('restoration')
 $rows=@();foreach($name in @($(if($First){'CM03-rider-before-mount-slot'}else{'CM03-mount-slot-before-rider'}),'CM03-next-round-activation')){$rows+=@([pscustomobject]@{name=$name;status='PASS';evidence=(Copy-Value $without)})}
 foreach($name in @('CM01-exploration-dismount-costs-nothing','CM01-exploration-free','CM01-combat-mount-cancel-costs-nothing','CM01-combat-mount-accepted','CM02-approach-arrival','CM03-combat-mount-conserves-debt','CM03-combat-mount-adoption-preparations','CM01-combat-mount-preparing-refused')){$rows+=@([pscustomobject]@{name=$name;status='PASS'})}
 [pscustomobject]@{productVersion='0.1.0-chunk6a-preview.126';scenario=$order.scenario;observations=[pscustomobject]@{chunk6aMountOrder=$order;chunk6aCommandProofs=(@($windows)+@($proof));chunk6aAdoptionDisposition=@{disposition=$order.disposition};chunk6aCombatMount=@((Copy-Value $order.mountBefore),$mountAfter);chunk6aNativeActingSetup=$setup;chunk6aPreCombatPositioning=$ground;actorAllocationTrace=(Copy-Value $order.allocationTrace);phase3fActualConfiguration=[pscustomobject]@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}};rows=$rows}
}
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeMountOrderEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
$script:checks=0
function Sync-OrderCopies($a){
 $order=$a.observations.chunk6aMountOrder;$a.observations.chunk6aCommandProofs[-1]=Copy-Value $order.positiveProof
 $a.observations.chunk6aCombatMount[0]=Copy-Value $order.mountBefore;$a.observations.chunk6aAdoptionDisposition.disposition=$order.disposition
 $copy=Copy-Value $order;$copy.PSObject.Properties.Remove('restoration');foreach($row in @($a.rows|Where-Object name -CIn @('CM03-rider-before-mount-slot','CM03-mount-slot-before-rider','CM03-next-round-activation'))){$row.evidence=Copy-Value $copy}
}
foreach($first in @($false,$true)){
 $a=New-OrderEnvelope $first;$request=[pscustomobject]@{scenario=$a.scenario}
 Assert-KmcChunk6aCombatMountEvidence $request $a 'PASS';$script:checks++
 $arguments=[object[]]::new(1);$arguments[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($a.observations.chunk6aMountOrder|ConvertTo-Json -Depth 100 -Compress));$null=$method.Invoke($null,$arguments);$script:checks++
 function Reject-OrderEnvelope([scriptblock]$Mutation){
  $x=Copy-Value $a;& $Mutation $x;$failed=$false;try{Assert-KmcChunk6aCombatMountEvidence $request $x 'PASS'}catch{$failed=$true}
  if(-not$failed){throw ('Order envelope accepted corruption '+$Mutation)};$script:checks++
 }
 Reject-OrderEnvelope {param($x)$x.scenario='foreign'}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aNativeActingSetup=$null}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aNativeActingSetup.afterTurnObject++}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aPreCombatPositioning=$null}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aPreCombatPositioning.resourceWindow.after.mount.reactions++}
 Reject-OrderEnvelope {param($x)$x.observations.actorAllocationTrace.observationErrors=1}
 Reject-OrderEnvelope {param($x)$x.observations.actorAllocationTrace.events[0].state.reactions++}
 Reject-OrderEnvelope {param($x)$x.observations.actorAllocationTrace.events=@()}
 Reject-OrderEnvelope {param($x)$x.observations.actorAllocationTrace.observerHooks=@()}
 foreach($index in @(0,1)){
  Reject-OrderEnvelope {param($x)$x.observations.chunk6aCommandProofs[$index].samples=@($x.observations.chunk6aCommandProofs[$index].samples|Where-Object boundary -CNE 'acted')}
  Reject-OrderEnvelope {param($x)$x.observations.chunk6aCommandProofs[$index].identity.abilityGuid='foreign'}
 }
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aCommandProofs[-1].identity.commandObject++}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aAdoptionDisposition.disposition='foreign'}
 Reject-OrderEnvelope {param($x)$x.rows[0].evidence.riderFirst=-not$x.rows[0].evidence.riderFirst}
 Reject-OrderEnvelope {param($x)$x.rows=@()}
 Reject-OrderEnvelope {param($x)$x.rows+=@($x.rows[0])}
 Reject-OrderEnvelope {param($x)$x.rows[0].status='FAIL'}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aCombatMount=@()}
 Reject-OrderEnvelope {param($x)$p=$x.observations.chunk6aMountOrder.positiveProof;$p.samples=@($p.samples|Where-Object boundary -CNE 'acted');Sync-OrderCopies $x}
 foreach($field in @('reactions','reactionCooldown','initiativeCooldown','initiativeOrder')){
  Reject-OrderEnvelope {param($x)$p=$x.observations.chunk6aMountOrder.positiveProof;@($p.resourceWindow.events|Where-Object boundary -CEQ 'cost-before')[0].state.$field++;Sync-OrderCopies $x}
 }
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.positiveProof.nativePointerInput.samples[2].ignored=$false;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.positiveProof.nativeApproachPath.events[3].path.points[-1].x-=18;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)@($x.observations.chunk6aMountOrder.positiveProof.resourceWindow.events|Where-Object boundary -CEQ 'approach-movement-before')[0].movementTurn++;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.positiveProof.resourceWindow.nativeApproachMovement.observerHooks=@();Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.terminalBridge.events=@([pscustomobject]@{sequence=1});Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.terminalBridge.before.rider.reactions++;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.allocationTrace.observationErrors=1;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.allocationTrace.events[0].state.move++;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.nextRound.riderResources.reactions++;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.completion.calls[0].before.forfeitState.forfeitRecorded=$true;Sync-OrderCopies $x}
 Reject-OrderEnvelope {param($x)$x.observations.chunk6aMountOrder.restoration.exact=$false;Sync-OrderCopies $x}
 foreach($row in @('CM02-adoption-plan-invalidated','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted')){Reject-OrderEnvelope {param($x)$x.rows+=@([pscustomobject]@{name=$row;status='PASS'})}}
}
'ORDER COMPLETE ENVELOPE PASS='+$script:checks+' FAIL=0; complete native TB command validators with synthetic observations; no native qualification'
