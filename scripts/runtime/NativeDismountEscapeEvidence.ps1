Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativePassiveResourceEvidence.ps1')
. (Join-Path $PSScriptRoot 'NativeTerminalBridgeEvidence.ps1')

function Assert-KmcDismountEscape($E,[switch]$StimulusOnly) {
 function Int($x){if($x -isnot [int] -and $x -isnot [long]){throw 'Escape integer missing'};[long]$x}
 function Number($x){if($x -isnot [int] -and $x -isnot [long] -and $x -isnot [double] -and $x -isnot [single] -and $x -isnot [decimal]){throw 'Escape number missing'};$n=[double]$x;if([double]::IsNaN($n)-or[double]::IsInfinity($n)){throw 'Escape nonfinite'};$n}
 function Yes($x){$x -is [bool] -and $x}
 function No($x){$x -is [bool] -and -not $x}
 function Same($a,$b){if($null -eq $a -or $null -eq $b -or ($a|ConvertTo-Json -Depth 80 -Compress) -cne ($b|ConvertTo-Json -Depth 80 -Compress)){throw 'Escape object differs'}}
 function SameState($a,$b){
  $x=$a|ConvertTo-Json -Depth 80|ConvertFrom-Json;$y=$b|ConvertTo-Json -Depth 80|ConvertFrom-Json
  foreach($state in @($x,$y)){if($null-ne$state.PSObject.Properties['geometry']){$state.geometry.PSObject.Properties.Remove('seconds')}}
  Same $x $y
 }
 function Settings($s,[bool]$movement,[bool]$paired){if($s.movement -isnot [bool] -or $s.movement -ne $movement -or $s.paired -isnot [bool] -or $s.paired -ne $paired -or -not(No $s.legacyUnified) -or -not(No $s.legacyScheduler) -or -not(No $s.overlay)){throw 'Escape settings differ'}}
 function Resources($a,$b,[bool]$allocation){
  foreach($f in @('actor','reactions','reactionsPerRound','initiativeOrder')){Same $a.$f $b.$f}
  foreach($f in @('standard','move','swift','reactionCooldown','initiativeCooldown')){if([Math]::Abs((Number $a.$f)-(Number $b.$f)) -gt 0.0001){throw ('Escape resource differs '+$f)}}
  $count=if($allocation){$b.grantSequence}else{$b.nativePrepareCount}
  if((Int $a.grantSequence) -ne (Int $count) -or (Int $a.actorObject) -eq 0 -or -not(Yes $a.inCombat)){throw 'Escape actor/preparation differs'}
  if($allocation -and ((Int $a.actorObject) -ne (Int $b.actorObject) -or -not(Yes $b.inCombat))){throw 'Escape allocation object differs'}
 }
 function Boundary($b,[string]$rider,[string]$mount,[long]$generation,[string]$state){
  if(-not(No $b.turnBased)-or-not(Yes $b.inCombat)-or-not(Yes $b.pairIdle)){throw 'Escape needs idle RT encounter'}
  $s=$b.state;if($s.relationshipState -cne $state -or (Int $s.generation) -ne $generation -or $s.rider.actor -cne $rider -or $s.mount.actor -cne $mount){throw 'Escape pair/generation/state differs'}
  if($s.selectedIds -isnot [array] -or $s.selectedIds.Count -ne 1 -or $s.selectedIds[0] -cne $rider){throw 'Escape exact selection missing'}
  Resources $b.riderResources $s.rider $false;Resources $b.mountResources $s.mount $false
 }
 $mountGuid='f053faad986631688defa003cd7bda0e';$dismountGuid='3af2b81f4d72bbb30501fa730fcdf36e'
 $ledgerFields=@('admittedMount','acceptedMount','admittedDismount','acceptedDismount','refusedVoluntary','forcedDetach','duplicateSuppressed','concurrentSuppressed')
 function Proof($p,[string]$ability,[string]$rider,[string]$target,[string]$state){
  foreach($f in @('pass','identityComplete','sameCommandAtEveryBoundary','exactActedObserved','nativeTerminal')){if(-not(Yes $p.$f)){throw ('Escape command flag absent '+$f)}}
  if($p.nativeResult -cne 'Success' -or -not(Yes $p.resourceWindow.pass) -or -not(Yes $p.resourceWindow.reactionResources.pass) -or (Int $p.initCount) -ne 1 -or $p.errors -isnot [array] -or $p.errors.Count -ne 0){throw 'Escape exact command resource proof missing'}
  $id=$p.identity;if($id.abilityGuid -cne $ability -or $id.casterId -cne $rider -or $id.targetId -cne $target -or $id.commandType -cne 'Move' -or [string]::IsNullOrEmpty($id.controlIdentity)){throw 'Escape command binding differs'}
  foreach($f in @('commandObject','processObject','contextObject')){if((Int $id.$f) -eq 0){throw 'Escape incomplete command identity'}}
  $terminal=@($p.samples|Where-Object boundary -CEQ 'terminal');if($terminal.Count -ne 1 -or $terminal[0].state.relationshipState -cne $state){throw 'Escape terminal differs'}
  foreach($f in $ledgerFields){$cost=if($ability -ceq $mountGuid){$f -cin @('admittedMount','acceptedMount')}else{$f -cin @('admittedDismount','acceptedDismount')};if((Int $p.ledgerDelta.$f) -ne [int]$cost){throw 'Escape command ledger differs'}}
 }
 if($E.contract -cne 'settled-positive-rt-mount-disabled-setting-native-dismount' -or $E.case -cnotin @('feature','policy') -or $E.scenario -cne ('chunk6a-dismount-'+$E.case+'-disabled-rt')){throw 'Escape case identity differs'}
 $mp=$E.mountProof;$rider=[string]$mp.identity.casterId;$mount=[string]$mp.mountId
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider -ceq $mount){throw 'Escape pair missing'}
 Proof $mp $mountGuid $rider $mount 'Mounted'
 $generation=(Int $mp.identity.generationAtInit)+1L;$before=$E.beforeDisable;$disabled=$E.afterDisable;$click=$E.beforeClick;$wait=$E.waitResources
 if(-not(Yes $wait.pass)-or-not(No $wait.turnBased)-or$wait.riderId -cne $rider -or $wait.mountId -cne $mount){throw 'Escape passive identity differs'}
 Assert-KmcNativePassiveResources $wait
 Assert-KmcNativeTerminalBridge $mp $before $E.mountTerminalBridge $false 'Mounted'
 Settings $before.settings $true $true
 foreach($b in @($before,$disabled,$click)){
  Boundary $b $rider $mount $generation 'Mounted'
  if(-not(Yes $b.abilityPresent)-or(Int $b.abilityObject) -ne (Int $before.abilityObject)-or(Int $b.abilityObject) -eq 0 -or $b.abilityGuid -cne $dismountGuid -or $b.abilityCasterId -cne $rider -or -not(Yes $b.visible)){throw 'Escape leased Dismount changed'}
  Same $b.state.ledger $before.state.ledger
 }
 Settings $disabled.settings ($E.case -ceq 'policy') ($E.case -ceq 'feature');Settings $click.settings ($E.case -ceq 'policy') ($E.case -ceq 'feature')
 if(-not(Yes $click.enabled) -or (Int $click.frame) -lt (Int $disabled.frame)+10L){throw 'Escape has no enabled input after disabled frames'}
 foreach($f in @('frame','gameTicks','allocationSequence')){if((Int $before.$f) -ne (Int $disabled.$f) -or (Int $before.$f) -ne (Int $wait.before.$f) -or (Int $click.$f) -ne (Int $wait.after.$f)){throw 'Escape window boundary gap'}}
 foreach($role in @('rider','mount')){$resource=$role+'Resources';Resources $before.$resource $disabled.$resource $true;Resources $before.$resource $wait.before.$role $true;Resources $wait.after.$role $click.$resource $true}
 if($StimulusOnly){return}
 $p=$E.dismountProof;Proof $p $dismountGuid $rider $rider 'Unmounted'
 if(-not(Yes $E.clicked)-or-not(Yes $E.input.clicked)-or$E.input.abilityGuid -cne $dismountGuid-or$E.input.clickedTargetId -cne $rider-or$E.input.resolvedTargetId -cne $rider){throw 'Escape native click missing'}
 if((Int $p.identity.generationAtInit) -ne $generation){throw 'Escape Dismount generation differs'}
 foreach($f in @('commandObject','processObject','contextObject','controlIdentity')){if($p.identity.$f -ceq $mp.identity.$f){throw 'Escape command aliases Mount'}}
 foreach($f in @('frame','gameTicks','allocationSequence')){if((Int $click.$f) -ne (Int $p.preClick.$f)){throw 'Escape click boundary gap'}}
 SameState $click.state $p.preClick.state
 $after=$E.afterDismount;$restored=$E.restored
 Boundary $after $rider $mount $generation 'Unmounted';Boundary $restored $rider $mount $generation 'Unmounted'
 Settings $after.settings ($E.case -ceq 'policy') ($E.case -ceq 'feature');Settings $restored.settings $true $true
 Assert-KmcNativeTerminalBridge $p $after $E.dismountTerminalBridge $false 'Unmounted'
 $restoredState=$restored.state|ConvertTo-Json -Depth 80|ConvertFrom-Json
 if($E.case-ceq'feature'){
  $absent=';mountAbilityFactPresent=False;';$present=';mountAbilityFactPresent=True;'
  $previous=$after.state.geometry.shellState;$current=$restoredState.geometry.shellState
  if($previous-isnot[string]-or$previous.Split(@(';mountAbilityFactPresent='),[StringSplitOptions]::None).Count-ne2-or-not$previous.Contains($absent)-or
     $current-cne$previous.Replace($absent,$present)){throw 'Escape exact Mount fact regrant differs'}
  $restoredState.geometry.shellState=$previous
 }
 SameState $after.state $restoredState
 foreach($f in @('frame','gameTicks','allocationSequence')){if((Int $after.$f) -ne (Int $restored.$f)){throw 'Escape restoration gap'}}
 foreach($f in $ledgerFields){$expected=if($f -cin @('admittedDismount','acceptedDismount')){1}else{0};if((Int $after.state.ledger.$f)-(Int $click.state.ledger.$f) -ne $expected){throw 'Escape Dismount transition count differs'}}
}
