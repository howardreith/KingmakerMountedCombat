param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aAdditionalEvidence.ps1')
# Original synthetic fixture functions only; no native archive is edited or credited.
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $repo 'scripts/Test-Chunk6aCausalProtocol.ps1'),[ref]$tokens,[ref]$errors)
foreach($name in @('Copy-Value','Put-Value','New-CommandProof')){$f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($f.Count-ne1){throw $name};. ([scriptblock]::Create($f[0].Extent.Text))}
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-NativeDismountEscape.ps1'),[ref]$tokens,[ref]$errors)
foreach($name in @('Copy-Escape','New-Boundary','Add-EscapeBridge')){$f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($f.Count-ne1){throw $name};. ([scriptblock]::Create($f[0].Extent.Text))}
. (Join-Path $PSScriptRoot 'Test-NativeGroundFixtures.ps1')
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Test-Chunk6aOrderEnvelope.ps1'),[ref]$tokens,[ref]$errors)
$f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq'New-ExactExplorationProof'},$true));. ([scriptblock]::Create($f[0].Extent.Text))
$ledger=[pscustomobject]@{admittedMount=2;acceptedMount=2;admittedDismount=1;acceptedDismount=1;refusedVoluntary=0;forcedDetach=3;duplicateSuppressed=2;concurrentSuppressed=0}
function New-FullSyntheticControl([bool]$Dismount,$Before,$After,[long]$Sequence,[int]$Frame,[long]$Ticks){
 $p=New-CommandProof;$id=$p.identity;$offset=if($Dismount){10}else{0};$id.commandObject+= $offset;$id.processObject+=$offset;$id.contextObject+=$offset;$id.controlIdentity='synthetic-control-'+$offset
 $id.generationAtInit=$Before.generation;$id.abilityGuid=if($Dismount){'3af2b81f4d72bbb30501fa730fcdf36e'}else{'f053faad986631688defa003cd7bda0e'};$id.targetId=if($Dismount){'rider'}else{'mount'}
 $p.preClick=[pscustomobject]@{frame=$Frame;gameTicks=$Ticks;allocationSequence=$Sequence;state=(Copy-Value $Before)}
 foreach($s in $p.samples){$s.identity=Copy-Value $id;if($s.boundary-cin@('init','click-admission','move-slot-installation','approach-start')){$s.identity.processObject=0;$s.identity.contextObject=0};$s.deliveryContext=if($s.boundary-ceq'deliver'){$id.contextObject}else{0};$s.gameTicks=$Ticks;Put-Value $s frame $(if($s.boundary-ceq'terminal'){$Frame+1}else{$Frame});$s.allocationSequence=if($s.boundary-ceq'terminal'){$Sequence+2L}else{$Sequence};$s.state=Copy-Value $(if($s.boundary-ceq'terminal'){$After}else{$Before})}
 $events=@();foreach($phase in @('before','after')){$state=Copy-Value $(if($phase-ceq'before'){$Before.rider}else{$After.rider});Put-Value $state inCombat $true
  $events+=@([pscustomobject]@{boundary=('cost-'+$phase);sequence=($Sequence+$(if($phase-ceq'before'){1L}else{2L}));frame=$Frame;gameTicks=$Ticks;command=$id.commandObject;commandActor='rider';actionType='Move';acted=$true;timeSinceStart=0.0;state=$state})}
 $p.resourceWindow.events=$events;Put-Value $p.resourceWindow pass $true
 $delta=Copy-Value $ledger;foreach($f in $delta.PSObject.Properties.Name){$delta.$f=0};if($Dismount){$delta.admittedDismount=1;$delta.acceptedDismount=1}else{$delta.admittedMount=1;$delta.acceptedMount=1};Put-Value $p ledgerDelta $delta
 Put-Value $p window $(if($Dismount){'disabled-setting-dismount'}else{'positive-mount'});$p
}
function New-EscapeEnvelope([string]$Case){
 $wait=New-Passive -Delta 3.1 -ExpectedDecay 3.1;Put-Value $wait pass $true;$wait.after.frame=110;$wait.after.gameTicks=32000000L
 $wait.before.rider.move=3.0;$wait.after.rider.move=0.0
 foreach($v in $wait.events){$v.gameTicks=$wait.after.gameTicks;if($v.state.actor-ceq'rider'){$v.state.move=$(if($v.boundary-ceq'cooldown-tick-before'){3.0}else{0.0})}}
 $before=New-Boundary $wait.before $true $true;$disabled=New-Boundary $wait.before ($Case-ceq'policy') ($Case-ceq'feature');$click=New-Boundary $wait.after ($Case-ceq'policy') ($Case-ceq'feature')
 $last=Copy-Value $wait.after;$last.frame++;$last.allocationSequence+=2;$last.rider.move+=3.0
 $after=New-Boundary $last ($Case-ceq'policy') ($Case-ceq'feature') 'Unmounted';$after.state.ledger.admittedDismount++;$after.state.ledger.acceptedDismount++
 $restored=Copy-Value $after;$restored.settings.movement=$true;$restored.settings.paired=$true
 if($Case-ceq'feature'){$restored.state.geometry.shellState=$restored.state.geometry.shellState.Replace(';mountAbilityFactPresent=False;',';mountAbilityFactPresent=True;')}
 $pre=Copy-Value $before.state;$pre.generation=1;$pre.relationshipState='Unmounted';$pre.rider.move-=3.0;$pre.ledger.admittedMount--;$pre.ledger.acceptedMount--
 $mp=New-FullSyntheticControl $false $pre $before.state 8 99 $before.gameTicks
 $dp=New-FullSyntheticControl $true $click.state $after.state $click.allocationSequence $click.frame $click.gameTicks
 $caseEvidence=[pscustomobject]@{contract='settled-positive-rt-mount-disabled-setting-native-dismount';case=$Case;scenario=('chunk6a-dismount-'+$Case+'-disabled-rt');mountProof=$mp;dismountProof=$dp;beforeDisable=$before;afterDisable=$disabled;beforeClick=$click;waitResources=$wait;afterDismount=$after;restored=$restored;clicked=$true;input=[pscustomobject]@{clicked=$true;abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';clickedTargetId='rider';resolvedTargetId='rider'}}
 $caseEvidence|Add-Member mountTerminalBridge (Add-EscapeBridge $mp $before $wait.observerHooks)
 $caseEvidence|Add-Member dismountTerminalBridge (Add-EscapeBridge $dp $after $wait.observerHooks)
 $exploreMount=New-ExactExplorationProof $false 0;$exploreDismount=New-ExactExplorationProof $true 2
 $trace=[pscustomobject]@{dropped=0;observationErrors=0;observerHooks=@($wait.observerHooks);events=(@($exploreMount.resourceWindow.events)+@($exploreDismount.resourceWindow.events)+@($mp.resourceWindow.events)+@($wait.events)+@($dp.resourceWindow.events))}
 $beforeSample=[pscustomobject]@{kind='mount-before';relationshipGeneration=1;dispatchAccepted=1;acceptedMountCount=1;forcedDetachCount=3;mountTurnsWhileMounted=0;rider=@{nativePrepareCount=1};mount=@{nativePrepareCount=1}}
 $afterSample=Copy-Value $beforeSample;$afterSample.kind='mount-after';$afterSample.relationshipGeneration=2;$afterSample.dispatchAccepted=2;$afterSample.acceptedMountCount=2
 foreach($pair in @(@('relationshipState','Mounted'),@('adoptionCount',1))){Put-Value $afterSample $pair[0] $pair[1]}
 $rows=@([pscustomobject]@{name='CM05-dismount-survives-feature-policy-disable';status='PASS';evidence=(Copy-Value $caseEvidence)})
 foreach($name in @('CM01-exploration-dismount-costs-nothing','CM01-exploration-free','CM01-combat-mount-cancel-costs-nothing','CM01-combat-mount-accepted','CM02-approach-arrival','CM03-combat-mount-conserves-debt','CM03-combat-mount-adoption-preparations')){$rows+=@([pscustomobject]@{name=$name;status='PASS'})}
 [pscustomobject]@{productVersion='0.1.0-chunk6a-preview.126';scenario=$caseEvidence.scenario;observations=[pscustomobject]@{actorAllocationTrace=$trace;chunk6aDismountEscape=$caseEvidence;chunk6aCommandProofs=@((Copy-Value $mp),(Copy-Value $dp),$exploreMount,$exploreDismount);chunk6aCombatMount=@($beforeSample,$afterSample);chunk6aAdoptionDisposition=@{disposition='RealTimeOwnership'};phase3fActualConfiguration=[pscustomobject]@{enablePairedActivation=$true;enableUnifiedMountedTurn=$false;enablePairedCommandScheduler=$false;enableDiagnosticOverlay=$false;overlayPresent=$false}};rows=$rows}
}
$checks=0
foreach($case in @('feature','policy')){
 $e=New-EscapeEnvelope $case;$request=[pscustomobject]@{scenario=$e.scenario};Assert-KmcChunk6aCombatMountEvidence $request $e 'PASS';$checks++
 function Reject-Envelope([scriptblock]$mutate){$x=Copy-Value $e;& $mutate $x;$failed=$false;try{Assert-KmcChunk6aCombatMountEvidence $request $x 'PASS'}catch{$failed=$true};if(-not$failed){throw ('Corrupt draft envelope accepted: '+$mutate)};$script:checks++}
 Reject-Envelope {param($x)$x.observations.actorAllocationTrace.observationErrors=1}
 Reject-Envelope {param($x)$x.observations.actorAllocationTrace.events=@()}
 Reject-Envelope {param($x)$x.observations.actorAllocationTrace.observerHooks=@()}
 foreach($bridge in @('mountTerminalBridge','dismountTerminalBridge')){
  Reject-Envelope {param($x)$x.observations.chunk6aDismountEscape.$bridge=$null;$x.rows[0].evidence.$bridge=$null}
 }
 foreach($field in @('reactions','reactionCooldown','initiativeCooldown','initiativeOrder')){
  Reject-Envelope {param($x)$x.observations.actorAllocationTrace.events[0].state.$field++}
  Reject-Envelope {param($x)$e=$x.observations.chunk6aDismountEscape;$e.mountTerminalBridge.after.rider.$field++;$e.beforeDisable.riderResources.$field++;$e.beforeDisable.state.rider.$field++;$x.rows[0].evidence=Copy-Value $e}
 }
 Reject-Envelope {param($x)$x.observations.chunk6aDismountEscape.case='wrong'}
 Reject-Envelope {param($x)$x.observations.chunk6aCommandProofs[0].identity.commandObject++}
 Reject-Envelope {param($x)$x.observations.chunk6aCommandProofs[1].identity.commandObject++}
 Reject-Envelope {param($x)$x.rows=@()}
 Reject-Envelope {param($x)$x.rows+=@($x.rows[0])}
 Reject-Envelope {param($x)$x.rows[0].status='FAIL'}
 Reject-Envelope {param($x)$x.rows[0].evidence.clicked=$false}
 foreach($window in @('mountProof','dismountProof')){
  foreach($field in @('reactions','reactionCooldown')){Reject-Envelope {param($x)$p=$x.observations.chunk6aDismountEscape.$window;$p.resourceWindow.events[0].state.$field++;$index=if($window-ceq'mountProof'){0}else{1};$x.observations.chunk6aCommandProofs[$index]=Copy-Value $p;$x.rows[0].evidence.$window=Copy-Value $p}}
  # Keep all case flags and three copies consistent: original native proof must still reject absent acted.
  Reject-Envelope {param($x)$p=$x.observations.chunk6aDismountEscape.$window;$p.samples=@($p.samples|Where-Object boundary -CNE 'acted');$index=if($window-ceq'mountProof'){0}else{1};$x.observations.chunk6aCommandProofs[$index]=Copy-Value $p;$x.rows[0].evidence.$window=Copy-Value $p}
 }
 foreach($row in @('CM02-adoption-plan-invalidated','CM02-geometry-change','CM02-obstruction','CM05-combat-dismount-accepted')){Reject-Envelope {param($x)$x.rows+=@([pscustomobject]@{name=$row;status='PASS'})}}
}
'DISMOUNT ESCAPE COMPLETE ENVELOPE PASS='+$checks+' FAIL=0; complete outer validation with synthetic observations; no native qualification'
