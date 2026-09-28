# Independent lab-only empty-command/refusal proof. The dedicated scenario and exact refusal-cause contract remain to be wired.
Set-StrictMode -Version Latest
function Assert-KmcRefusedMountInput($InputProof,[string]$Rider,[string]$Mount,[string]$Target,[string[]]$Selection) {
 $p=$InputProof
 foreach($name in @('observerHooks','errors','constructedCommands','events','allocationEvents','activations')) {
  if($p.$name -isnot [Array]){throw 'Refused input requires typed evidence arrays'}
 }
 if($p.traceComplete -isnot [bool] -or $p.events.Count -ne 2 -or $p.activations.Count -ne 3){throw 'Refused input schema or activation count differs'}
 foreach($snapshot in @($p.before,$p.after)) {
  if($snapshot.state.selectedIds -isnot [Array]){throw 'Refused input selection is not an exact array'}
  foreach($field in @('frame','gameTicks','allocationSequence','activationSequence','shellCount','processBindings','castCount','refusalCount','targetStartCount','targetEndCount','dispatchAccepted','dispatchRejected')) {
   $n=$snapshot.$field
   if($n -isnot [int] -and $n -isnot [long]){throw 'Refused input counter is not an exact integer'}
   if($n -lt 0){throw 'Refused input counter is negative'}
  }
 }
 if(($p.activations.phase -join ',') -cne 'TargetSelectionStarted,CastRefused,TargetSelectionEnded'){throw 'Refused input native activation phases differ'}
 if($null -ne $p.events[0].result -or $p.events[1].result -isnot [bool]){throw 'Refused input native result type differs'}

 if($p.contract -cne 'synchronous-refused-native-mount-with-zero-command-construction' -or $p.riderId -cne $Rider -or $p.mountId -cne $Mount -or $p.targetId -cne $Target -or
    $p.traceComplete -ne $true -or @($p.errors).Count -ne 0 -or @($p.constructedCommands).Count -ne 0 -or @($p.events).Count -ne 2){throw 'Refused input constructed a command or lacks complete observation'}
 $tokens=@('06002726','06002727','060093F6')
 if(@($p.observerHooks).Count -ne 3){throw 'Refused input observer installation incomplete'}
 foreach($token in $tokens){$h=@($p.observerHooks|Where-Object token -CEQ $token);if($h.Count -ne 1 -or $h[0].moduleMvid -cne '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'){throw 'Refused input hook identity differs'}
  $beforeHook=if($token -ceq '060093F6'){'ClickBefore'}else{$null}
  $afterHook=if($token -ceq '060093F6'){'ClickAfter'}else{'Constructed'}
  $method=if($token -ceq '060093F6'){'Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler.OnClick'}else{'Kingmaker.UnitLogic.Commands.UnitUseAbility..ctor'}
  if($h[0].prefix -cne $beforeHook -or $h[0].postfix -cne $afterHook -or $h[0].method -cne $method){throw 'Refused input observer method or wrappers differ'}
 }
 $b=$p.before;$a=$p.after
 foreach($s in @($b,$a)){
  if($s.targetSelectorEmpty -ne $true -or $s.riderCommandsEmpty -ne $true -or $s.mountCommandsEmpty -ne $true -or $s.state.relationshipState -cne 'Unmounted' -or
    (($s.state.selectedIds -join ',') -cne ($Selection -join ','))){throw 'Refused input did not retain its empty command slots, target selector or exact declared selection'}
 }
 foreach($field in @('frame','gameTicks','shellCount','processBindings','castCount','dispatchAccepted','dispatchRejected')){if($null -eq $b.$field -or $b.$field -ne $a.$field){throw ('Refused input changed '+$field)}}
 foreach($field in @('refusalCount','targetStartCount','targetEndCount')){if($a.$field -ne $b.$field+1){throw ('Refused input requires one exact native '+$field)}}
 if($a.state.generation -ne $b.state.generation -or ($a.state.ledger|ConvertTo-Json -Depth 30 -Compress) -cne ($b.state.ledger|ConvertTo-Json -Depth 30 -Compress)){throw 'Refused input changed relationship generation or ledger'}
 foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
  $x=$b.state.$actor.$field;$y=$a.state.$actor.$field
  if($null -eq $x -or $null -eq $y -or [double]::IsNaN([double]$x) -or [double]::IsInfinity([double]$x) -or $x -ne $y){throw ('Refused input changed '+$actor+' '+$field)}
 }}
 foreach($actor in @('riderPosition','horsePosition')){foreach($axis in @('x','y','z')){if($null -eq $b.state.geometry.$actor.$axis -or $b.state.geometry.$actor.$axis -ne $a.state.geometry.$actor.$axis){throw 'Refused input moved an actor'}}}
 if($p.events[0].boundary -cne 'click-before' -or $p.events[1].boundary -cne 'click-after' -or $p.events[1].result -ne $false){throw 'Native target click was not exactly refused'}
 foreach($e in $p.events){
  if($e.frame -ne $b.frame -or $e.gameTicks -ne $b.gameTicks -or $e.casterId -cne $Rider -or $e.targetId -cne $Target -or
    $e.abilityGuid -cne 'f053faad986631688defa003cd7bda0e' -or $e.handlerObject -eq 0 -or $e.abilityObject -eq 0 -or
    $e.handlerObject -ne $p.events[0].handlerObject -or $e.abilityObject -ne $p.events[0].abilityObject -or $e.currentSelectorAbilityObject -ne $e.abilityObject -or
    $e.exactTargetObject -ne $true -or $e.exactTargetPoint -ne $true -or $e.button -ne 0 -or $e.simulate -ne $false -or $e.muteEvents -ne $false){throw 'Refused native click receiver, arguments or clock differs'}
 }
 $seq=[int]$b.allocationSequence
 foreach($e in $p.allocationEvents){
  if($e.sequence -ne ++$seq){throw 'Refusal allocation trace sequence has a gap'}
  if($e.state.actor -cin @($Rider,$Mount) -and $e.boundary -cmatch 'admission|cost|prepare|clear|opportunity|movement'){throw 'Refused input observed a native command/resource/movement event'}
 }
 if($seq -ne $a.allocationSequence){throw 'Refusal allocation window did not close'}
 $refused=@($p.activations|Where-Object phase -CEQ 'CastRefused')
 if($refused.Count -ne 1 -or $refused[0].casterId -cne $Rider -or $refused[0].targetId -cne $Target -or $refused[0].selectedIds -cne ($Selection -join ',') -or
    [string]::IsNullOrWhiteSpace($refused[0].reason) -or $refused[0].frame -ne $b.frame -or $refused[0].abilityGuid -cne 'f053faad986631688defa003cd7bda0e'){throw 'Exact native refusal reason or identities missing'}
 $seq=[long]$b.activationSequence
 foreach($e in $p.activations){if($e.sequence -ne ++$seq -or $e.phase -cnotin @('TargetSelectionStarted','CastRefused','TargetSelectionEnded')){throw 'Refusal native activation sequence differs'}}
 if($seq -ne $a.activationSequence){throw 'Refusal activation sequence did not close'}
}
