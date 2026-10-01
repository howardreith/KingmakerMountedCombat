# Additional case binding; future caller must also run the unchanged full Chunk 6A envelope.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativeDismountEscapeEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativeMountOrderEvidence.ps1')
. (Join-Path $PSScriptRoot 'EarlyEndMountEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativeRepeatedDismountEvidence.ps1')
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
