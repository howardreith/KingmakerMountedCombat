# Additional case binding; future caller must also run the unchanged full Chunk 6A envelope.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativeDismountEscapeEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativeMountOrderEvidence.ps1')
. (Join-Path $PSScriptRoot 'EarlyEndMountEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativeRepeatedDismountEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativeActionEconomyEvidence.ps1')
. (Join-Path $PSScriptRoot 'ChildEntryPreambleEvidence.ps1')
function Assert-KmcSameEvidence($A,$B,[string]$Label){
 if($null-eq$A-or$null-eq$B-or(ConvertTo-Json -InputObject $A -Depth 100 -Compress)-cne(ConvertTo-Json -InputObject $B -Depth 100 -Compress)){throw ('Additional Chunk6A binding differs: '+$Label)}
}
function Get-KmcExactWindow($Artifact,[string]$Window){
 $proofs=@($Artifact.observations.chunk6aCommandProofs|Where-Object window -CEQ $Window)
 if($proofs.Count-ne1){throw ('Additional Chunk6A window absent/duplicate: '+$Window)};$proofs[0]
}
function Assert-KmcDismountEscapeEnvelope($Request,$Artifact){
 $e=$Artifact.observations.chunk6aDismountEscape
 if($e.scenario-cne$Request.scenario-or$Artifact.scenario-cne$Request.scenario){throw 'Dismount escape request binding differs'}
 $mp=Get-KmcExactWindow $Artifact 'positive-mount';$dp=Get-KmcExactWindow $Artifact 'disabled-setting-dismount'
 Assert-KmcSameEvidence $e.mountProof $mp 'escape positive command'
 Assert-KmcSameEvidence $e.dismountProof $dp 'escape Dismount command'
 Assert-KmcRelationshipCommandProof $mp $true $false 0 $true 0 $true
 Assert-KmcRelationshipCommandProof $dp $true $false 0 $false 0 $true
 Assert-KmcDismountEscape $e
 $trace=$Artifact.observations.actorAllocationTrace
 if(($trace.dropped-isnot[int]-and$trace.dropped-isnot[long])-or$trace.dropped-ne0-or($trace.observationErrors-isnot[int]-and$trace.observationErrors-isnot[long])-or$trace.observationErrors-ne0){throw 'Escape complete native trace missing'}
 foreach($window in @($e.mountTerminalBridge,$e.waitResources,$e.dismountTerminalBridge)){
  Assert-KmcSameEvidence @($window.observerHooks) @($trace.observerHooks) 'escape native observer hooks'
  $slice=@($trace.events|Where-Object{$_.sequence-gt$window.before.allocationSequence-and$_.sequence-le$window.after.allocationSequence})
  Assert-KmcSameEvidence @($window.events) $slice 'escape terminal/wait full trace'
 }
 foreach($proof in @($Artifact.observations.chunk6aCommandProofs)){
  $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal')[0]
  $slice=@($trace.events|Where-Object{$_.sequence-gt$proof.preClick.allocationSequence-and$_.sequence-le$terminal.allocationSequence})
  Assert-KmcSameEvidence @($proof.resourceWindow.events) $slice 'escape exact command full trace'
 }
 $rows=@($Artifact.rows|Where-Object name -CEQ 'CM05-dismount-survives-feature-policy-disable')
 if($rows.Count-ne1-or$rows[0].status-cne'PASS'){throw 'Dismount escape exact mandatory row absent'}
 Assert-KmcSameEvidence $rows[0].evidence $e 'escape row'
 foreach($row in @('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted')){
  if(@($Artifact.rows|Where-Object name -CEQ $row).Count-ne0){throw 'Dismount escape inherited another allocation'}
 }
}
function Assert-KmcMountOrderEnvelope($Request,$Artifact,[bool]$RequireEarlyEnd=$false){
 $e=$Artifact.observations.chunk6aMountOrder
 if($e.scenario-cne$Request.scenario-or$Artifact.scenario-cne$Request.scenario){throw 'Mount order request binding differs'}
 $p=Get-KmcExactWindow $Artifact 'positive-mount'
 Assert-KmcSameEvidence $e.positiveProof $p 'order positive command'
 $disposition=$Artifact.observations.chunk6aAdoptionDisposition.disposition
 if($e.disposition-cne$disposition){throw 'Order disposition differs from exact transition'}
 $prepares=if($disposition-ceq'PreparePartnerThisRound'){1}else{0}
 Assert-KmcRelationshipCommandProof $p $true $true $prepares $true 0 $true
 Assert-KmcMountOrder $e
 Assert-KmcMountOrderRestoration $e
 $before=@($Artifact.observations.chunk6aCombatMount|Where-Object kind -CEQ 'mount-before')
 if($before.Count-ne1){throw 'Mount order baseline missing'}
 Assert-KmcSameEvidence $e.mountBefore $before[0] 'order baseline'
 $row=if($e.riderFirst){'CM03-rider-before-mount-slot'}else{'CM03-mount-slot-before-rider'}
 $withoutRestoration=$e|ConvertTo-Json -Depth 100|ConvertFrom-Json
 $withoutRestoration.PSObject.Properties.Remove('restoration')
 $early=@($Artifact.rows|Where-Object name -CEQ 'CM03-early-end-turn')
 if($RequireEarlyEnd-or$early.Count-gt0){
  if($early.Count-ne1-or$early[0].status-cne'PASS'){throw 'Mount order exact early End row absent'}
  Assert-KmcEarlyEndMount $e
  Assert-KmcSameEvidence $early[0].evidence $withoutRestoration 'early End row'
 }

 foreach($name in @($row,'CM03-next-round-activation')){
  $matched=@($Artifact.rows|Where-Object name -CEQ $name)
  if($matched.Count-ne1-or$matched[0].status-cne'PASS'){throw 'Mount order exact mandatory row absent'}
  # AddRow freezes its evidence before cleanup; restoration is a separate mandatory observation.
  Assert-KmcSameEvidence $matched[0].evidence $withoutRestoration ('order row '+$name)
 }
 if(@($Artifact.rows|Where-Object {$_.name-cin@('CM03-rider-before-mount-slot','CM03-mount-slot-before-rider')}).Count-ne1){throw 'One order allocation cannot qualify both orders'}
 $fullTrace=$Artifact.observations.actorAllocationTrace
 if(($fullTrace.dropped-isnot[int]-and$fullTrace.dropped-isnot[long])-or$fullTrace.dropped-ne0-or($fullTrace.observationErrors-isnot[int]-and$fullTrace.observationErrors-isnot[long])-or$fullTrace.observationErrors-ne0){throw 'Order complete native trace missing'}
 Assert-KmcSameEvidence @($e.allocationTrace.observerHooks) @($fullTrace.observerHooks) 'order full trace hooks'
 Assert-KmcSameEvidence @($e.allocationTrace.events) @($fullTrace.events|Where-Object {$_.sequence-le$e.nextRound.allocationSequence}) 'order full trace prefix'
 $trace=@($e.allocationTrace.events)
 foreach($window in @('exploration-mount','exploration-dismount','positive-mount')){
  $proof=Get-KmcExactWindow $Artifact $window
  $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal')
  if($terminal.Count-ne1){throw 'Order bound command terminal absent'}
  $events=@($trace|Where-Object {$_.sequence-gt$proof.preClick.allocationSequence-and$_.sequence-le$terminal[0].allocationSequence})
  Assert-KmcSameEvidence @($proof.resourceWindow.events) $events ('order full trace '+$window)
 }
 $continued=@($trace|Where-Object {$_.sequence-gt$e.continuation.before.allocationSequence-and$_.sequence-le$e.continuation.after.allocationSequence})
 Assert-KmcSameEvidence @($e.continuation.events) $continued 'order continuation trace'
 foreach($row in @('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted')){
  if(@($Artifact.rows|Where-Object name -CEQ $row).Count-ne0){throw 'Mount order inherited another allocation'}
 }
}
function Assert-KmcRepeatedDismountInputEnvelope($Request,$Artifact){
 $e=$Artifact.observations.chunk6aRepeatedDismount
 if($null-eq$e-or$e.scenario-cne$Request.scenario-or$Artifact.scenario-cne$Request.scenario){throw 'Repeated Dismount request binding differs'}
 $proof=Get-KmcExactWindow $Artifact 'focused-combat-dismount'
 Assert-KmcSameEvidence $e.positiveProof $proof 'repeated Dismount exact command'
 Assert-KmcRelationshipCommandProof $proof $true $false 0 $false 0 $true
 Assert-KmcRepeatedDismountInput $e
 $rows=@($Artifact.rows|Where-Object name -CEQ 'CM05-repeated-input')
 if($rows.Count-ne1-or$rows[0].status-cne'PASS'){throw 'Repeated Dismount exact mandatory row absent'}
 Assert-KmcSameEvidence $rows[0].evidence $e 'repeated Dismount row'
 $trace=$Artifact.observations.actorAllocationTrace
 if(($trace.dropped-isnot[int]-and$trace.dropped-isnot[long])-or$trace.dropped-ne0-or
    ($trace.observationErrors-isnot[int]-and$trace.observationErrors-isnot[long])-or$trace.observationErrors-ne0){throw 'Repeated Dismount complete native trace missing'}
 foreach($window in @($e.cancellation,$e.repeat)){
  $slice=@($trace.events|Where-Object{$_.sequence-gt$window.before.allocationSequence-and$_.sequence-le$window.after.allocationSequence})
  Assert-KmcSameEvidence @($window.allocationEvents) $slice 'repeated Dismount synchronous full trace'
 }
 $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal')
 if($terminal.Count-ne1){throw 'Repeated Dismount command terminal absent'}
 $commandSlice=@($trace.events|Where-Object{$_.sequence-gt$proof.preClick.allocationSequence-and$_.sequence-le$terminal[0].allocationSequence})
 Assert-KmcSameEvidence @($proof.resourceWindow.events) $commandSlice 'repeated Dismount exact command full trace'
 Assert-KmcSameEvidence @($e.terminalBridge.observerHooks) @($trace.observerHooks) 'repeated Dismount terminal hooks'
 $bridgeSlice=@($trace.events|Where-Object{$_.sequence-gt$e.terminalBridge.before.allocationSequence-and$_.sequence-le$e.terminalBridge.after.allocationSequence})
 Assert-KmcSameEvidence @($e.terminalBridge.events) $bridgeSlice 'repeated Dismount terminal full trace'
}
# One isolated action-economy transaction: the variant evidence bound to its exact command
# proofs, the shared order fixture and the complete native trace, judged by the external
# validator. Returns the bound facts; the row verdict is the envelope's own step so that the
# same facts can be re-evaluated on an immutable artifact whatever its compiled row said.
function Assert-KmcActionEconomyEnvelopeFacts($Request,$Artifact){
 $o=$Artifact.observations
 if($null-eq$o.PSObject.Properties['chunk6aActionEconomy']){throw 'Action economy evidence absent'}
 $e=$o.chunk6aActionEconomy
 if($null-eq$e-or$e.scenario-cne$Request.scenario-or$Artifact.scenario-cne$Request.scenario){throw 'Action economy request binding differs'}
 $v=Get-KmcActionEconomyVariant ([string]$Request.scenario);if($null-eq$v){throw 'Action economy variant is not registered'}
 if($null-eq$o.PSObject.Properties['chunk6aMountOrder']){throw 'Action economy order fixture absent'}
 $order=$o.chunk6aMountOrder
 if($null-eq$order-or$order.scenario-cne$Request.scenario){throw 'Action economy order fixture binding differs'}
 $mountProof=$null;$dismountProof=$null
 if(-not$v.riderExhaust){
  $mountProof=Get-KmcExactWindow $Artifact 'positive-mount'
  $disposition=$o.chunk6aAdoptionDisposition.disposition
  if($order.disposition-cne$disposition){throw 'Action economy disposition differs from exact transition'}
  $prepares=if($disposition-ceq'PreparePartnerThisRound'){1}else{0}
  Assert-KmcRelationshipCommandProof $mountProof $true $true $prepares (-not [bool]$v.adjacentMount) 0 $true
  $before=@($o.chunk6aCombatMount|Where-Object kind -CEQ 'mount-before');if($before.Count-ne1){throw 'Action economy mount baseline missing'}
  Assert-KmcSameEvidence $order.mountBefore $before[0] 'action economy baseline'
  Assert-KmcSameEvidence $order.positiveProof $mountProof 'action economy positive command'
 }
 if([string]$v.dismount-cne'none'){
  $dismountProof=Get-KmcExactWindow $Artifact 'combat-dismount'
  Assert-KmcRelationshipCommandProof $dismountProof $true $true 0 $false 0 $true
 }
 Assert-KmcActionEconomy $e $order $mountProof $dismountProof
 Assert-KmcMountOrderRestoration $order
 $trace=$o.actorAllocationTrace
 if(($trace.dropped-isnot[int]-and$trace.dropped-isnot[long])-or$trace.dropped-ne0-or($trace.observationErrors-isnot[int]-and$trace.observationErrors-isnot[long])-or$trace.observationErrors-ne0){throw 'Action economy complete native trace missing'}
 foreach($proof in @($o.chunk6aCommandProofs)){
  $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal');if($terminal.Count-ne1){throw 'Action economy bound command terminal absent'}
  $slice=@($trace.events|Where-Object {$_.sequence-gt$proof.preClick.allocationSequence-and$_.sequence-le$terminal[0].allocationSequence})
  Assert-KmcSameEvidence @($proof.resourceWindow.events) $slice ('action economy full trace '+$proof.window)
 }
 $sliceOf={param($w) @($trace.events|Where-Object {$_.sequence-gt$w.before.allocationSequence-and$_.sequence-le$w.after.allocationSequence})}
 foreach($name in @('mountSlot','riderEntry','exhaustion','refusal','retention')){
  if($null-eq$e.PSObject.Properties[$name]-or$null-eq$e.$name-or$null-eq$e.$name.PSObject.Properties['events']){continue}
  Assert-KmcSameEvidence @($e.$name.events) @(& $sliceOf $e.$name) ('action economy window '+$name)
 }
 if($null-ne$e.PSObject.Properties['mountSlot']-and$null-ne$e.mountSlot){
  $s=$e.mountSlot
  $endSlice=@($trace.events|Where-Object {$_.sequence-gt$s.after.allocationSequence-and$_.sequence-le$s.slotEnd.allocationSequence})
  Assert-KmcSameEvidence @($s.endEvents) $endSlice 'action economy slot end'
 }
 if([string]$v.dismount-cne'none'){
  $d=$e.dismount;$r=$e.release
  $releaseSlice=@($trace.events|Where-Object {$_.sequence-gt$d.afterDismount.allocationSequence-and$_.sequence-le$r.mountTurn.allocationSequence})
  Assert-KmcSameEvidence @($r.events) $releaseSlice 'action economy release trace'
  if([string]$v.dismount-ceq'immediate'){
   Assert-KmcSameEvidence @($d.mountTerminalBridge.observerHooks) @($trace.observerHooks) 'action economy bridge hooks'
   Assert-KmcSameEvidence @($d.mountTerminalBridge.events) @(& $sliceOf $d.mountTerminalBridge) 'action economy terminal bridge'
  } else {
   $later=$d.laterTurn
   Assert-KmcSameEvidence $later $o.chunk6aDismountTurn 'action economy later-turn observation'
   foreach($w in @($later.mountTerminalBridge,$later.continuation,$later.readyBridge)){
    Assert-KmcSameEvidence @($w.observerHooks) @($trace.observerHooks) 'action economy later-turn hooks'
    Assert-KmcSameEvidence @($w.events) @(& $sliceOf $w) 'action economy later-turn window'
   }
   if([string]$v.dismount-ceq'later-after-mount'){
    Assert-KmcSameEvidence @($later.groundSetup.events) @(& $sliceOf $later.groundSetup) 'action economy ground window'
    Assert-KmcSameEvidence @($later.groundStartBridge.events) @(& $sliceOf $later.groundStartBridge) 'action economy ground start bridge'
    Assert-KmcNativePassiveResources $later.groundStartBridge
   } else {
    Assert-KmcSameEvidence @($later.riderAttack.events) @(& $sliceOf $later.riderAttack) 'action economy rider attack window'
    Assert-KmcSameEvidence @($later.attackStartBridge.events) @(& $sliceOf $later.attackStartBridge) 'action economy attack start bridge'
    Assert-KmcNativePassiveResources $later.attackStartBridge
   }
   Assert-KmcNativePassiveResources $later.mountTerminalBridge
  }
 }
 if(-not$v.riderExhaust-and[string]$v.dismount-ceq'none'){
  Assert-KmcSameEvidence @($order.allocationTrace.observerHooks) @($trace.observerHooks) 'action economy order trace hooks'
  Assert-KmcSameEvidence @($order.allocationTrace.events) @($trace.events|Where-Object {$_.sequence-le$order.nextRound.allocationSequence}) 'action economy order trace prefix'
  $continued=@($trace.events|Where-Object {$_.sequence-gt$order.continuation.before.allocationSequence-and$_.sequence-le$order.continuation.after.allocationSequence})
  Assert-KmcSameEvidence @($order.continuation.events) $continued 'action economy continuation trace'
  Assert-KmcSameEvidence @($order.terminalBridge.events) @(& $sliceOf $order.terminalBridge) 'action economy terminal bridge trace'
 }
 [pscustomobject]@{e=$e;v=$v;order=$order;mountProof=$mountProof;dismountProof=$dismountProof}
}
function Assert-KmcActionEconomyEnvelope($Request,$Artifact){
 $facts=@(Assert-KmcActionEconomyEnvelopeFacts $Request $Artifact)[-1]
 $e=$facts.e;$v=$facts.v
 Assert-KmcActionEconomyRestoration $e
 $rows=@($Artifact.rows|Where-Object name -CEQ $v.row)
 if($rows.Count-ne1-or$rows[0].status-cne'PASS'){throw 'Action economy exact mandatory row absent'}
 # AddRow freezes the evidence before cleanup; the lease and input restorations are separate observations.
 $frozen=$e|ConvertTo-Json -Depth 100|ConvertFrom-Json
 $frozen.automaticEnd.PSObject.Properties.Remove('restored')
 if($null-ne$frozen.PSObject.Properties['unrelated']-and$null-ne$frozen.unrelated){$frozen.unrelated.PSObject.Properties.Remove('restoration')}
 Assert-KmcSameEvidence $rows[0].evidence $frozen 'action economy row'
 $allowed=@('CM01-exploration-dismount-costs-nothing','CM01-exploration-free','CM01-combat-mount-cancel-costs-nothing','CM01-combat-mount-preparing-refused',[string]$v.row)
 if(-not$v.riderExhaust){$allowed+=@('CM01-combat-mount-accepted','CM03-combat-mount-conserves-debt','CM03-combat-mount-adoption-preparations');if(-not$v.adjacentMount){$allowed+='CM02-approach-arrival'}}
 if(@($Artifact.rows|Where-Object {$_.name-clike'CM*'-and$_.name-cnotin$allowed}).Count-ne0){throw 'Action economy allocation credited another combat row'}
}
