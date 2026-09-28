# Paused real-time Mount proof; the complete accepted command proof remains required.
Set-StrictMode -Version Latest
function Assert-KmcPausedHeld($First,$Sample) {
 if($null -eq $First -or $null -eq $Sample){throw 'Paused observation missing'}
 foreach($s in @($First,$Sample)){
  if($s.paused -ne $true -or $s.turnBased -ne $false -or $s.traceComplete -ne $true -or $null -eq $s.commandObject -or $s.commandObject -eq 0 -or
    [string]::IsNullOrEmpty($s.controlIdentity) -or [string]::IsNullOrEmpty($s.casterId) -or [string]::IsNullOrEmpty($s.targetId) -or
    $s.commandType -cne 'Move' -or $s.abilityGuid -cne 'f053faad986631688defa003cd7bda0e' -or $s.processObject -ne 0 -or $s.contextObject -ne 0 -or
    $s.inMoveSlot -ne $true -or $s.createdByPlayer -ne $true -or $s.started -ne $false -or $s.acted -ne $false -or $s.finished -ne $false){throw 'Paused command was not exactly held before native execution'}
  if($s.state.relationshipState -cne 'Unmounted' -or $s.state.generation -ne $s.generationAtInit -or @($s.state.selectedIds).Count -ne 1 -or
    $s.state.selectedIds[0] -cne $s.casterId){throw 'Paused relationship or selection differs'}
 }
 foreach($field in @('commandObject','controlIdentity','casterId','targetId','generationAtInit','commandType','abilityGuid','gameTicks')){
  if($null -eq $First.$field -or $First.$field -cne $Sample.$field){throw ('Paused identity/time changed: '+$field)}
 }
 foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
  $a=$First.state.$actor.$field;$b=$Sample.state.$actor.$field
  if($null -eq $a -or $null -eq $b -or [double]::IsNaN([double]$a) -or [double]::IsInfinity([double]$a) -or $a -ne $b){throw ('Paused resource changed: '+$actor+' '+$field)}
 }}
 if($null -eq $First.state.ledger -or $null -eq $Sample.state.ledger -or ($First.state.ledger|ConvertTo-Json -Depth 30 -Compress) -cne ($Sample.state.ledger|ConvertTo-Json -Depth 30 -Compress)){throw 'Paused ledger changed'}
 foreach($actor in @('riderPosition','horsePosition')){foreach($axis in @('x','y','z')){
  $a=$First.state.geometry.$actor.$axis;$b=$Sample.state.geometry.$actor.$axis
  if($null -eq $a -or $null -eq $b -or [double]::IsNaN([double]$a) -or [double]::IsInfinity([double]$a) -or $a -ne $b){throw 'Paused actor moved'}
 }}
}
function Assert-KmcPausedQueue($Hold,$Proof) {
 if($Hold.contract -cne 'one-native-rt-mount-held-paused-then-unpaused-once' -or $Hold.unpauseCount -ne 1 -or $Proof.pass -ne $true -or @($Hold.samples).Count -ne 11){throw 'Paused queue/terminal contract incomplete'}
 $first=$Hold.samples[0];$previous=[int]$first.frame
 $baseline=$first|ConvertTo-Json -Depth 100|ConvertFrom-Json;$baseline.state=$Proof.preClick.state;$baseline.gameTicks=$Proof.preClick.gameTicks;Assert-KmcPausedHeld $baseline $first
 if($Proof.preClick.frame -gt $first.frame -or $Proof.preClick.allocationSequence -gt $first.allocationSequence){throw "Paused admission precedes baseline"}
 for($i=0;$i -lt $Hold.samples.Count;$i++){
  $s=$Hold.samples[$i];Assert-KmcPausedHeld $first $s
  if($i -gt 0 -and [int]$s.frame -le $previous){throw 'Paused observations did not advance frames'};$previous=[int]$s.frame
 }
 if(($Hold.samples[-1]|ConvertTo-Json -Depth 100 -Compress) -cne ($Hold.beforeUnpause|ConvertTo-Json -Depth 100 -Compress)){throw 'Paused final boundary differs'}
 $r=$Hold.afterUnpause
 if($r.paused -ne $false){throw 'Native unpause absent'}
 $compare=$r|ConvertTo-Json -Depth 100|ConvertFrom-Json;$compare.paused=$true;Assert-KmcPausedHeld $first $compare
 if($r.frame -ne $Hold.beforeUnpause.frame){throw 'Unpause not observed synchronously'}
 foreach($key in @('commandObject','controlIdentity','casterId','targetId','generationAtInit','commandType','abilityGuid')){
  if($first.$key -cne $Proof.identity.$key){throw 'Unpaused terminal belongs to another command'}
 }
 if($null -eq $Hold.events -or $Hold.events -isnot [Array]){throw 'Paused native allocation events missing'}
 $sequence=[int]$first.allocationSequence
 foreach($event in $Hold.events){
  if($event.sequence -ne ++$sequence){throw 'Paused event sequence differs'}
  if($event.state.actor -cin @($first.casterId,$first.targetId) -and ([string]::IsNullOrEmpty($event.boundary) -or $event.boundary -cmatch 'cost|prepare|clear|opportunity')){throw 'Paused native resource callback occurred'}
 }
 if($sequence -ne $r.allocationSequence){throw 'Paused event sequence did not close'}
 foreach($s in $Hold.samples){if($s.allocationSequence -lt $first.allocationSequence -or $s.allocationSequence -gt $sequence){throw 'Paused sample sequence outside its window'}}
 $exactEvents=@($Proof.resourceWindow.events|Where-Object {$_.sequence -gt $first.allocationSequence -and $_.sequence -le $sequence})
 if((ConvertTo-Json -InputObject @($Hold.events) -Depth 100 -Compress) -cne (ConvertTo-Json -InputObject $exactEvents -Depth 100 -Compress)){throw 'Paused events differ from exact command trace'}
 $admitted=@($Proof.samples|Where-Object boundary -CEQ 'click-admission')
 if($admitted.Count -ne 1){throw 'Paused admission boundary absent'}
 foreach($key in @('frame','gameTicks','allocationSequence')){if($first.$key -ne $admitted[0].$key){throw 'Paused hold differs from exact click admission'}}
 $approach=@($Proof.samples|Where-Object boundary -CEQ 'approach-start')
 if($approach.Count -ne 1 -or $approach[0].frame -le $r.frame -or $approach[0].gameTicks -lt $r.gameTicks){throw 'Native approach did not follow unpause'}
}
