# External mirror of NativeActionEconomyEvidence: the nine Chunk 6A action-economy rows
# (CM03 mount/rider expenditure and refusal, unrelated candidate; CM05 Dismount economy).
# Read-only; the enclosing envelope binds proofs, order evidence and the full trace.
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'NativeMountOrderEvidence.ps1')
. (Join-Path $PSScriptRoot 'FullTbDismountEvidence.ps1')
function Get-KmcActionEconomyVariants {
 @(
  [pscustomobject]@{scenario='chunk6a-mount-spent-move-tb';row='CM03-mount-spent-move';riderFirst=$false;unrelated=$false;adjacentMount=$false;longbow=$false;riderExhaust=$false;mountSlot='ground';riderEntry='ground';dismount='none';targetPlacement='default'},
  [pscustomobject]@{scenario='chunk6a-mount-spent-standard-tb';row='CM03-mount-spent-standard';riderFirst=$false;unrelated=$false;adjacentMount=$false;longbow=$false;riderExhaust=$false;mountSlot='single-attack';riderEntry='ground';dismount='none';targetPlacement='near-mount'},
  [pscustomobject]@{scenario='chunk6a-mount-spent-all-tb';row='CM03-mount-spent-all';riderFirst=$false;unrelated=$false;adjacentMount=$false;longbow=$false;riderExhaust=$false;mountSlot='full-attack';riderEntry='ground';dismount='none';targetPlacement='near-mount'},
  [pscustomobject]@{scenario='chunk6a-rider-without-move-tb';row='CM03-rider-without-move';riderFirst=$true;unrelated=$false;adjacentMount=$false;longbow=$true;riderExhaust=$true;mountSlot='none';riderEntry='ground';dismount='none';targetPlacement='default'},
  [pscustomobject]@{scenario='chunk6a-rider-other-action-tb';row='CM03-rider-other-action';riderFirst=$true;unrelated=$false;adjacentMount=$false;longbow=$true;riderExhaust=$false;mountSlot='none';riderEntry='attack';dismount='none';targetPlacement='default'},
  [pscustomobject]@{scenario='chunk6a-unrelated-candidate-between-tb';row='CM03-unrelated-candidate-between';riderFirst=$true;unrelated=$true;adjacentMount=$false;longbow=$false;riderExhaust=$false;mountSlot='none';riderEntry='ground';dismount='none';targetPlacement='default'},
  [pscustomobject]@{scenario='chunk6a-dismount-after-rider-expenditure-tb';row='CM05-after-rider-expenditure';riderFirst=$true;unrelated=$false;adjacentMount=$false;longbow=$true;riderExhaust=$false;mountSlot='none';riderEntry='ground';dismount='later-after-rider';targetPlacement='default'},
  [pscustomobject]@{scenario='chunk6a-dismount-after-mount-expenditure-tb';row='CM05-after-mount-expenditure';riderFirst=$true;unrelated=$false;adjacentMount=$false;longbow=$false;riderExhaust=$false;mountSlot='none';riderEntry='ground';dismount='later-after-mount';targetPlacement='dismount-ring'},
  [pscustomobject]@{scenario='chunk6a-dismount-immediately-after-mount-tb';row='CM05-immediately-after-mount';riderFirst=$true;unrelated=$false;adjacentMount=$true;longbow=$false;riderExhaust=$false;mountSlot='none';riderEntry='five-foot-step';dismount='immediate';targetPlacement='default'}
 )
}
function Get-KmcActionEconomyVariant([string]$Scenario){ $found=@(Get-KmcActionEconomyVariants|Where-Object {$_.scenario -ceq $Scenario}); if($found.Count-eq1){$found[0]}else{$null} }
function Get-KmcActionEconomyScenarios { @(Get-KmcActionEconomyVariants|ForEach-Object scenario) }
function Get-KmcActionEconomyRows { @(Get-KmcActionEconomyVariants|ForEach-Object row) }
function Assert-KmcActionEconomy($E,$Order,$MountProof,$DismountProof){
 function Fail($why){throw ('Action economy: '+$why)}
 function I($v){if($v-isnot[int]-and$v-isnot[long]){Fail 'integer missing'};[long]$v}
 function B($v){if($v-isnot[bool]){Fail 'boolean missing'};[bool]$v}
 function N($v){if($v-isnot[int]-and$v-isnot[long]-and$v-isnot[double]-and$v-isnot[single]-and$v-isnot[decimal]){Fail 'number missing'};$d=[double]$v;if([double]::IsNaN($d)-or[double]::IsInfinity($d)){Fail 'nonfinite number'};$d}
 function T($v){if($v-is[string]){$v}else{$null}}
 function Near($a,$b){[Math]::Abs([double]$a-[double]$b)-le0.0001}
 function Has($o,$n){$null-ne$o-and$null-ne$o.PSObject.Properties[$n]}
 function Prop($o,$n){if(Has $o $n){$o.$n}else{$null}}
 function Equal($a,$b){($a|ConvertTo-Json -Depth 100 -Compress)-ceq($b|ConvertTo-Json -Depth 100 -Compress)}
 function Cmd($e){$c=Prop $e 'command';if($c-is[int]-or$c-is[long]){[long]$c}else{0L}}
 function Res($b,$role){$b.($role+'Resources')}
 function Read-Debt($s){@{Actor=(T $s.actor);Object=(I $s.actorObject);InCombat=(B $s.inCombat);Grants=(I $s.grantSequence);Standard=(N $s.standard);Move=(N $s.move);Swift=(N $s.swift);InitiativeCooldown=(N $s.initiativeCooldown)}}
 function Debt-Matches($x,$y){$null-ne$y-and$x.Actor-ceq$y.Actor-and$x.Object-eq$y.Object-and$x.Object-ne0-and$x.Grants-eq$y.Grants-and(Near $x.Standard $y.Standard)-and(Near $x.Move $y.Move)-and(Near $x.Swift $y.Swift)}
 function Debt-Tick($d,$e){
  $delta=N $e.gameDeltaTime;if($delta-lt0){Fail 'negative native delta'}
  $tb=B $e.nativeTurnBased;$passing=B $e.nativePassing;$surprised=B $e.nativeSurprised;$waiting=B $e.state.waitingInitiative
  $decay=$delta
  if($tb-and$d.InCombat){if(-not$passing){$decay=0.0}elseif($d.InitiativeCooldown-gt0-and-not$surprised){$decay=[Math]::Max(0.0,$decay-$d.InitiativeCooldown)}}
  elseif(($tb-and-not$passing)-or$waiting){$decay=0.0}
  @{Actor=$d.Actor;Object=$d.Object;InCombat=$d.InCombat;Grants=$d.Grants;InitiativeCooldown=[Math]::Max(0.0,$d.InitiativeCooldown-$delta);
    Standard=$(if($decay-gt0){[Math]::Max(0.0,$d.Standard-$decay)}else{$d.Standard});Move=$(if($decay-gt0){[Math]::Max(0.0,$d.Move-$decay)}else{$d.Move});Swift=$(if($decay-gt0){[Math]::Max(0.0,$d.Swift-$decay)}else{$d.Swift})}
 }
 function Replay-Debt($before,$events,[long]$start,[long]$end,[string]$actor,$allowedCommand,[string]$label){
  $current=Read-Debt $before;if($current.Actor-cne$actor-or$current.Object-eq0){Fail ($label+': baseline actor differs')}
  $pending=$null;$sequence=$start
  foreach($e in @($events|Where-Object {$null-ne$_})){
   $sequence++;if((I $e.sequence)-ne$sequence){Fail ($label+': trace sequence gap')}
   $who=T (Prop (Prop $e 'state') 'actor');if($who-cne$actor){continue}
   $boundary=[string](Prop $e 'boundary');$actual=Read-Debt $e.state
   if($boundary.Contains('prepare')-or$boundary.Contains('clear')-or$boundary.Contains('remove-unit')){Fail ($label+': native preparation, clear or removal occurred for '+$actor+' at '+$boundary)}
   $command=Cmd $e;$owned=$null-ne$allowedCommand-and$command-eq[long]$allowedCommand-and$command-ne0
   if($boundary.Contains('cost')-or$boundary.Contains('admission')){
    $action=T (Prop $e 'actionType')
    if(-not($owned-or$action-ceq'Free'-or$null-eq$action)){Fail ($label+': undeclared native '+$action+' command callback for '+$actor+' at '+$boundary)}
    if($owned){$current=$actual;$pending=$null;continue}
   }
   if($boundary-ceq'cooldown-tick-after'){if($null-eq$pending-or-not(Debt-Matches $pending $actual)){Fail ($label+': native cooldown effect differs')};$current=$actual;$pending=$null;continue}
   if($null-ne$pending){if(-not(Debt-Matches $pending $actual)){Fail ($label+': nested native callback changed debt')}}
   elseif(-not((Debt-Matches $current $actual)-or$owned)){Fail ($label+': '+$actor+' debt changed without a native tick at '+$boundary)}
   if($owned){$current=$actual}
   if($boundary-ceq'cooldown-tick-before'){if($null-ne$pending){Fail ($label+': nested cooldown tick')};$pending=Debt-Tick $actual $e}
  }
  if($sequence-ne$end-or$null-ne$pending){Fail ($label+': trace did not close')}
  $current
 }
 # An empty event list stays an array through the pipeline (comma operator); a null element never reaches a replay.
 function Events($p){if(-not(Has $p 'events')-or$null-eq$p.events){Fail 'events missing'};,@($p.events|Where-Object {$null-ne$_})}
 function Assert-Pair($before,$after,[string]$actor,[string]$label){
  if((I $after.frame)-lt(I $before.frame)-or(I $after.gameTicks)-lt(I $before.gameTicks)-or(I $after.allocationSequence)-lt(I $before.allocationSequence)){Fail ($label+': boundary order differs')}
  if((I $before.turnObject)-eq0-or(I $after.turnObject)-ne(I $before.turnObject)-or(I $after.round)-ne(I $before.round)-or(T $before.currentActor)-cne$actor-or(T $after.currentActor)-cne$actor){Fail ($label+': native turn or actor differs')}
  if(-not(B $before.turnBased)-or-not(B $after.turnBased)){Fail ($label+': native mode differs')}
  if((I $before.controllerObject)-ne(I $after.controllerObject)-or(I $before.sessionObject)-ne(I $after.sessionObject)){Fail ($label+': encounter identity changed')}
 }
 function Assert-ActorCommand($c,[string]$actor,[string]$other,[string]$kind,[string]$label){
  $before=$c.before;$after=$c.after;$admitted=$c.admitted;$terminal=$c.terminal
  Assert-Pair $before $after $actor $label
  $id=I $admitted.id
  if($id-eq0-or(I $terminal.id)-ne$id-or(T $admitted.executor)-cne$actor-or(T $terminal.executor)-cne$actor-or-not(B $terminal.finished)-or-not(B $terminal.acted)-or(T $terminal.result)-cne'Success'){Fail ($label+': exact native command did not succeed')}
  $expectedType=if($kind-ceq'single-attack'-or$kind-ceq'full-attack'){'Kingmaker.UnitLogic.Commands.UnitAttack'}else{'Kingmaker.UnitLogic.Commands.UnitMoveTo'}
  if((T $admitted.type)-cne$expectedType-or(T $terminal.type)-cne$expectedType){Fail ($label+': command type differs')}
  if(-not(B $c.traceComplete)){Fail ($label+': allocation trace incomplete')}
  $input=Prop $c 'input';if($null-eq$input-or-not(B $input.clicked)){Fail ($label+': native input was not admitted')}
  $events=Events $c;$start=I $before.allocationSequence;$end=I $after.allocationSequence
  $costs=@();$admissions=0;$sequence=$start
  foreach($e in $events){
   $sequence++;if((I $e.sequence)-ne$sequence){Fail ($label+': event sequence gap')}
   $who=T (Prop (Prop $e 'state') 'actor');$boundary=[string](Prop $e 'boundary')
   if($who-cne$actor-and$who-cne$other){continue}
   if($boundary.Contains('prepare')-or$boundary.Contains('clear')-or$boundary.Contains('turn-end')-or$boundary.Contains('remove-unit')){Fail ($label+': undeclared native allocation event '+$boundary)}
   if($boundary.Contains('cost')-or$boundary.Contains('admission')){
    if($who-cne$actor-or(Cmd $e)-ne$id-or(T $e.commandActor)-cne$actor-or(B $e.simulatingClick)){Fail ($label+': another actor or command owns a native callback at '+$boundary)}
    if($boundary.Contains('admission')){$admissions++}
    if($boundary.Contains('cost')){$costs+=@($e)}
   }
  }
  if($sequence-ne$end-or$admissions-ne2){Fail ($label+': admission or trace terminal differs')}
  $boundaries=@('cost-before','actor-cost-before','actor-cost-after','cost-after')
  if($costs.Count-ne$boundaries.Count){Fail ($label+': native TB cost callback count differs')}
  for($i=0;$i-lt$costs.Count;$i++){if((T $costs[$i].boundary)-cne$boundaries[$i]-or-not(B $costs[$i].acted)){Fail ($label+': native cost boundary order differs')}}
  $b=$costs[0].state;$a=$costs[$costs.Count-1].state
  $beforeRole=if($actor-ceq(T (Prop (Prop $before 'riderResources') 'actor'))){'rider'}else{'mount'}
  $afterRole=if($actor-ceq(T (Prop (Prop $after 'riderResources') 'actor'))){'rider'}else{'mount'}
  $beforeRes=Res $before $beforeRole;$afterRes=Res $after $afterRole
  if((T (Prop $beforeRes 'actor'))-cne$actor-or(T (Prop $afterRes 'actor'))-cne$actor){Fail ($label+': actor resources missing')}
  if((I $afterRes.grantSequence)-ne(I $beforeRes.grantSequence)){Fail ($label+': native preparation repeated')}
  if($kind-ceq'ground'-or$kind-ceq'five-foot-step'){
   foreach($e in $costs){if(-not(B $e.ignoreCooldown)-or(T $e.actionType)-cne'Move'){Fail ($label+': native TB ground command charged at acted')}}
   foreach($field in @('standard','move','swift')){if(-not(Near (N $b.$field) (N $a.$field))){Fail ($label+': ground acted callback changed debt')}}
   if(-not(Near (N $afterRes.standard) (N $beforeRes.standard))-or-not(Near (N $afterRes.swift) (N $beforeRes.swift))){Fail ($label+': ground command changed another action')}
   $delta=(N $afterRes.move)-(N $beforeRes.move);$allowed=(N $afterRes.measuredAllowedTime)-(N $beforeRes.measuredAllowedTime)
   if($kind-ceq'ground'){if(-not($delta-gt0-and$allowed-gt0-and(Near $delta $allowed))){Fail ($label+': Move debt differs from the observed native allowed movement time')}}
   else{if(-not((Near $delta 0)-and(N $c.stepMetres)-gt0-and(N $c.stepMetres)-le((N $c.stepLimit)+0.01)-and(B $input.fiveFootStep))){Fail ($label+': five-foot step charged Move or exceeded the native step')}}
  } else {
   foreach($e in $costs){if((B $e.ignoreCooldown)-or(T $e.actionType)-cne'Standard'){Fail ($label+': attack cost is not one native Standard command')}}
   if(-not(Near (N $a.standard) ((N $b.standard)+6))){Fail ($label+': native Standard cost differs')}
   if(-not(Near (N $a.swift) (N $b.swift))){Fail ($label+': attack wrote Swift')}
   $full=$kind-ceq'full-attack'
   if((B $c.nativeFull)-ne$full-or(B $c.nativeSingle)-eq$full-or(B $input.fullEnabled)-ne$full){Fail ($label+': native attack mode differs')}
   $moveAdd=if($full){3.0}else{0.0}
   if(-not(Near (N $a.move) ((N $b.move)+$moveAdd))){Fail ($label+': native attack Move component differs')}
   if(-not(Near (N $afterRes.standard) ((N $beforeRes.standard)+6))-or-not(Near (N $afterRes.move) ((N $beforeRes.move)+$moveAdd))){Fail ($label+': attack terminal debt differs')}
  }
 }
 function Assert-Spent($s,[bool]$usedOneMove,[bool]$usedStandard,[bool]$hasMove,[bool]$hasStandard,[string]$label){
  if((B (Prop $s 'usedOneMoveAction'))-ne$usedOneMove-or(B $s.usedStandardAction)-ne$usedStandard-or(B $s.hasMoveAction)-ne$hasMove-or(B $s.hasStandardAction)-ne$hasStandard){Fail ($label+': native action availability differs')}
 }
 function Assert-MountSlot($E,$v,$Order){
  $slot=Prop $E 'mountSlot';if($null-eq$slot){Fail 'mount slot evidence missing'}
  $rider=T $E.riderId;$mount=T $E.mountId
  if((T $slot.kind)-cne$v.mountSlot){Fail 'mount slot kind differs'}
  Assert-ActorCommand $slot $mount $rider $v.mountSlot 'mount slot'
  $before=$slot.before;$after=$slot.after;$slotEnd=$slot.slotEnd
  if((T $before.status)-cne'Preparing'-or(T $after.status)-cne'Acting'){Fail 'mount slot did not enter Acting through its own command'}
  if((I (Res $before 'mount').grantSequence)-ne1-or(I (Res $before 'rider').grantSequence)-ne0){Fail 'mount slot preparation or rider order differs'}
  if((T $before.state.relationshipState)-cne'Unmounted'-or(T $after.state.relationshipState)-cne'Unmounted'){Fail 'mount slot relationship differs'}
  $spent=$slot.spent
  if($v.mountSlot-ceq'ground'){Assert-Spent $spent $true $false $true $true 'mount slot'}
  elseif($v.mountSlot-ceq'single-attack'){Assert-Spent $spent $false $true $true $false 'mount slot'}
  else{Assert-Spent $spent $true $true $false $false 'mount slot'}
  if((I $slotEnd.round)-ne(I $before.round)-or(I $slotEnd.allocationSequence)-lt(I $after.allocationSequence)-or(T $slotEnd.currentActor)-ceq$mount){Fail 'mount slot end boundary differs'}
  if(-not(Has $slot 'endEvents')-or$null-eq$slot.endEvents){Fail 'slot end events missing'}
  $endEvents=@($slot.endEvents)
  $ends=@($endEvents|Where-Object {(T $_.boundary)-ceq'turn-end-after'-and(T (Prop (Prop $_ 'state') 'actor'))-ceq$mount}).Count
  $mountCosts=@($endEvents|Where-Object {(T (Prop (Prop $_ 'state') 'actor'))-ceq$mount-and([string](Prop $_ 'boundary')).Contains('cost')}).Count
  if($ends-ne1-or$mountCosts-ne0){Fail 'mount slot did not end exactly once without another cost'}
  $debt=Replay-Debt (Res $after 'mount') $endEvents (I $after.allocationSequence) (I $slotEnd.allocationSequence) $mount $null 'mount slot end'
  if(-not(Debt-Matches $debt (Read-Debt (Res $slotEnd 'mount')))){Fail 'mount debt changed at its own native End'}
  if((I $Order.mountBefore.mount.nativePrepareCount)-ne1-or(T $Order.disposition)-cne'RetainPartnerParticipation'-or(I $Order.mounted.partnerContextObject)-ne0){Fail 'spent mount was not retained without preparation'}
  $slot
 }
 function Assert-Retention($E,$slot,$MountProof){
  $r=Prop $E 'retention';if($null-eq$r){Fail 'retention evidence missing'}
  $mount=T $E.mountId;$before=$r.before;$after=$r.after
  if(-not(Equal $before $slot.slotEnd)){Fail 'retention does not start at the mount slot end'}
  if(-not(B $r.traceComplete)-or(I $r.mountTurnsBetween)-ne0){Fail 'retention trace incomplete or mount acted again'}
  $debt=Replay-Debt (Res $before 'mount') (Events $r) (I $before.allocationSequence) (I $after.allocationSequence) $mount $null 'retention'
  if(-not(Debt-Matches $debt (Read-Debt (Res $after 'mount')))){Fail 'mount debt was refreshed before the Mount'}
  $pre=$MountProof.preClick
  if((I $pre.allocationSequence)-ne(I $after.allocationSequence)-or(I $pre.frame)-ne(I $after.frame)-or(I $pre.gameTicks)-ne(I $after.gameTicks)){Fail 'retention end is not the exact Mount pre-click'}
  $terminal=@($MountProof.samples|Where-Object {(T $_.boundary)-ceq'terminal'});if($terminal.Count-ne1){Fail 'Mount terminal sample absent'};$terminal=$terminal[0]
  foreach($field in @('standard','move','swift')){
   if(-not(Near (N $pre.state.mount.$field) (N (Res $after 'mount').$field))){Fail ('pre-click mount debt differs: '+$field)}
   if(-not(Near (N $terminal.state.mount.$field) (N $pre.state.mount.$field))){Fail ('Mount changed the retained mount debt: '+$field)}
   if(-not(Near (N $terminal.nativeAllocation.mount.$field) (N $terminal.state.mount.$field))){Fail ('terminal mount allocation differs: '+$field)}
  }
  if((I $terminal.state.mount.nativePrepareCount)-ne(I (Res $after 'mount').grantSequence)-or(I $MountProof.resourceWindow.mountPrepareDelta)-ne0-or(I $MountProof.resourceWindow.clearCount)-ne0-or(I $MountProof.resourceWindow.expectedMountPrepareDelta)-ne0){Fail 'Mount prepared or cleared the spent mount'}
 }
 function Assert-RiderEntry($E,$v,$MountProof){
  $entry=Prop $E 'riderEntry';if($null-eq$entry-or(T $entry.kind)-cne$v.riderEntry){Fail 'rider Acting entry differs from the variant'}
  $rider=T $E.riderId;$mount=T $E.mountId
  if($v.riderEntry-ceq'ground'){
   if((T $entry.setupContract)-cne'native-rider-ground-order-before-mount-baseline'-or(I $entry.turnObject)-eq0){Fail 'ground Acting entry binding missing'}
   return
  }
  $kind=if($v.riderEntry-ceq'attack'){'single-attack'}else{'five-foot-step'}
  Assert-ActorCommand $entry $rider $mount $kind 'rider entry'
  if((T $entry.before.status)-cne'Preparing'-or(T $entry.after.status)-cne'Acting'){Fail 'rider entry did not take the turn from Preparing to Acting'}
  $after=Res $entry.after 'rider'
  if($v.riderEntry-ceq'attack'){
   if(-not(B (Prop (Prop $entry 'weapon') 'ranged'))-or-not(B (Prop (Prop $entry 'spent') 'usedStandardAction'))-or-not(B $entry.spent.hasMoveAction)-or(B $entry.spent.usedOneMoveAction)){Fail 'rider other action did not leave exactly one lawful Move'}
  } else {
   if(-not(B (Prop (Prop $entry 'spent') 'hasMoveAction'))-or(B $entry.spent.usedOneMoveAction)-or(B $entry.spent.usedStandardAction)){Fail 'five-foot step spent an action'}
   if(-not(B $entry.after.state.geometry.isAdjacent)){Fail 'five-foot step did not end inside the Mount envelope'}
  }
  if($null-eq$MountProof){return}
  $pre=$MountProof.preClick
  if((I $pre.state.rider.nativeTurnObject)-ne(I $entry.after.turnObject)){Fail 'Mount belongs to another rider turn'}
  foreach($field in @('standard','move','swift')){if(-not(Near (N $pre.state.rider.$field) (N $after.$field))){Fail ('entry debt did not carry into the Mount baseline: '+$field)}}
  if($v.riderEntry-ceq'attack'){
   $terminal=@($MountProof.samples|Where-Object {(T $_.boundary)-ceq'terminal'})[0]
   if(-not(Near (N $terminal.state.rider.standard) (N $after.standard))-or-not(Near (N $after.standard) 6)){Fail "Mount changed the rider's prior Standard debt"}
  }
 }
 function Assert-Refusal($E){
  $rider=T $E.riderId;$mount=T $E.mountId
  $exhaustion=Prop $E 'exhaustion';if($null-eq$exhaustion){Fail 'exhaustion evidence missing'}
  Assert-ActorCommand $exhaustion $rider $mount 'single-attack' 'exhaustion'
  if((T $exhaustion.before.status)-cne'Acting'-or-not(B (Prop (Prop $exhaustion 'weapon') 'ranged'))){Fail 'exhaustion is not a Standard action on the Acting setup turn'}
  Assert-Spent $exhaustion.spent $true $true $false $false 'exhaustion'
  $r=Prop $E 'refusal';if($null-eq$r){Fail 'refusal evidence missing'}
  $before=$r.before;$after=$r.after
  Assert-Pair $before $after $rider 'refusal'
  if((I $before.turnObject)-ne(I $exhaustion.after.turnObject)-or(T $before.status)-cne'Acting'){Fail 'refusal is not on the exhausted Acting turn'}
  $availability=$r.availability
  if(-not(B $availability.visible)-or(B $availability.enabled)-or(B $availability.transitionReady)-or-not(([string](Prop $availability 'reason')).Contains('The rider has no Move action available to mount.'))){Fail 'availability did not refuse for the missing Move'}
  if((B $r.abilityAvailableForCast)-or(B $r.canTarget)){Fail 'native targeting admitted a rider without Move'}
  if((B $before.state.rider.hasMove)-or(B $after.state.rider.hasMove)){Fail 'rider held a Move at refusal'}
  $click=$r.click
  if((B $click.clicked)-or(B $click.nativeShell.present)-or(I $click.dispatchAcceptedDelta)-ne0-or(I $click.dispatchRejectedDelta)-ne0-or(I $click.nativePrimaryShellPrepareDelta)-ne0-or(I $click.nativeCastRequestDelta)-ne(I $click.nativeRefusalDelta)-or(I $click.nativeCastRequestDelta)-gt1-or(I $click.targetSelectionStartDelta)-ne(I $click.targetSelectionEndDelta)-or(I $click.targetSelectionStartDelta)-gt1){Fail 'native click created a command, shell or dispatch'}
  $controls=$r.controls
  foreach($field in @('shellCount','processBindings','dispatchAccepted','dispatchRejected')){if((I $controls.before.$field)-ne(I $controls.after.$field)){Fail ('refusal changed native control state: '+$field)}}
  if(-not(Equal $r.ledgerBefore $r.ledgerAfter)){Fail 'refusal changed the transition ledger'}
  if((T $before.state.relationshipState)-cne'Unmounted'-or(T $after.state.relationshipState)-cne'Unmounted'-or(I $before.state.generation)-ne(I $after.state.generation)){Fail 'refusal changed the relationship'}
  if(-not(B $r.riderCommandsEmptyAfter)-or-not(B $r.mountCommandsEmptyAfter)-or-not(B $r.traceComplete)){Fail 'a command survived the refusal'}
  foreach($pair in @(@('rider',$rider),@('mount',$mount))){
   $debt=Replay-Debt (Res $before $pair[0]) (Events $r) (I $before.allocationSequence) (I $after.allocationSequence) $pair[1] $null ('refusal '+$pair[0])
   if(-not(Debt-Matches $debt (Read-Debt (Res $after $pair[0])))){Fail ('refusal changed '+$pair[0]+' debt')}
  }
  $end=Prop $r 'end'
  if($null-eq$end-or(T $end.method)-cne'Kingmaker.Game.PauseBind'-or(T $end.token)-cne'06000CB7'-or(I $end.count)-ne1-or(I $end.beforeEndInput.turnObject)-ne(I $before.turnObject)-or(I $end.afterEnd.turnObject)-eq(I $before.turnObject)){Fail 'exhausted turn did not end through one native End input'}
 }
 function Assert-Unrelated($E,$Order){
  $u=Prop $E 'unrelated';if($null-eq$u){Fail 'unrelated candidate evidence missing'}
  $rider=T $E.riderId;$mount=T $E.mountId;$actor=T $u.actorId
  if([string]::IsNullOrEmpty($actor)-or$actor-ceq$rider-or$actor-ceq$mount-or(I $u.actorObject)-eq0){Fail 'unrelated candidate identity missing'}
  if((I $u.inputBase)-ne10-or-not(B $u.outsideCombat)-or-not(Equal $u.beforeResources $u.afterResources)){Fail 'unrelated initiative input differs or changed resources'}
  $roster=@($Order.mounted.roster|ForEach-Object {T $_.actor})
  $ri=[Array]::IndexOf($roster,$rider);$ui=[Array]::IndexOf($roster,$actor);$mi=[Array]::IndexOf($roster,$mount)
  if($ri-lt0-or$ui-lt0-or$mi-lt0-or$ri-ge$ui-or$ui-ge$mi-or(I $u.rosterIndexAtMount)-ne$ui){Fail 'unrelated candidate is not between rider and mount in the native roster'}
  $round=I $Order.mounted.round;$mountedSequence=I $Order.mounted.allocationSequence;$nextSequence=I $Order.nextRound.allocationSequence
  $turns=@($Order.turns|Where-Object {(T $_.currentActor)-ceq$actor-and(I $_.round)-eq$round-and(I $_.allocationSequence)-gt$mountedSequence}|ForEach-Object {I $_.turnObject}|Select-Object -Unique)
  if($turns.Count-ne1){Fail 'unrelated candidate did not take exactly one native turn after the Mount'}
  $events=@($Order.allocationTrace.events|Where-Object {(T (Prop (Prop $_ 'state') 'actor'))-ceq$actor-and(I $_.sequence)-gt$mountedSequence-and(I $_.sequence)-le$nextSequence})
  foreach($boundary in @('prepare-before','prepare-after','turn-end-after')){
   if(@($events|Where-Object {(T $_.boundary)-ceq$boundary-and(I $_.round)-eq$round}).Count-ne1){Fail 'unrelated candidate was skipped or duplicated'}
  }
  if(@($Order.turns|Where-Object {(T $_.currentActor)-ceq$actor-and(I $_.round)-eq$round-and(I $_.allocationSequence)-lt$mountedSequence}).Count-ne0){Fail 'unrelated candidate acted before the rider'}
 }
 function Assert-Release($E,[string]$rider,[string]$mount,$afterDismount){
  $r=Prop $E 'release';if($null-eq$r){Fail 'release observation missing'}
  $round=I $afterDismount.round;if((I $r.dismountRound)-ne$round){Fail 'release round differs'}
  $end=Prop $r 'endInput'
  if($null-eq$end-or(T $end.method)-cne'Kingmaker.Game.PauseBind'-or(T $end.token)-cne'06000CB7'-or(I $end.count)-ne1-or(I $end.beforeEndInput.turnObject)-ne(I $afterDismount.turnObject)-or(T $end.beforeEndInput.currentActor)-cne$rider){Fail 'dismounted rider did not end through one native End input'}
  if(-not(Has $r 'turns')-or$null-eq$r.turns){Fail 'release turn observations missing'}
  $turns=@($r.turns);if($turns.Count-eq0-or$turns.Count-gt128){Fail 'release turn observations missing'}
  if(@($turns|Where-Object {(T $_.currentActor)-ceq$mount-and(I $_.round)-eq$round}).Count-ne0){Fail 'the mount received a duplicate native turn in the release round'}
  $mountTurn=Prop $r 'mountTurn'
  if($null-eq$mountTurn-or(T $mountTurn.currentActor)-cne$mount-or(I $mountTurn.round)-ne($round+1)-or-not(B $mountTurn.turnBased)-or(T $mountTurn.state.relationshipState)-cne'Unmounted'-or(I $mountTurn.state.generation)-ne(I $afterDismount.state.generation)){Fail "the mount's separate participation did not resume in the following round"}
  if((I (Res $mountTurn 'mount').grantSequence)-ne((I (Res $afterDismount 'mount').grantSequence)+1)-or(I (Res $mountTurn 'rider').grantSequence)-ne((I (Res $afterDismount 'rider').grantSequence)+1)){Fail 'next-round preparations differ from exactly once each'}
  if(-not(B $r.traceComplete)){Fail 'release trace incomplete'}
  $events=Events $r;$sequence=I $afterDismount.allocationSequence;$mountPrepares=0;$riderPrepares=0
  foreach($x in $events){
   $sequence++;if((I $x.sequence)-ne$sequence){Fail 'release trace gap'}
   $who=T (Prop (Prop $x 'state') 'actor');$boundary=[string](Prop $x 'boundary')
   if($who-ceq$mount){
    if((I $x.round)-eq$round-and($boundary.Contains('cost')-or$boundary.Contains('prepare')-or$boundary.Contains('turn-end'))){Fail 'the mount acted, prepared or ended in the release round after the Dismount'}
    if($boundary-ceq'prepare-before'){if((I $x.round)-ne($round+1)){Fail 'mount prepared outside the following round'};$mountPrepares++}
   }
   if($who-ceq$rider-and$boundary-ceq'prepare-before'){if((I $x.round)-ne($round+1)){Fail 'rider prepared outside the following round'};$riderPrepares++}
  }
  if($sequence-ne(I $mountTurn.allocationSequence)-or$mountPrepares-ne1-or$riderPrepares-ne1){Fail 'release observation did not close on one separate preparation each'}
 }
 function Assert-Dismount($E,$v,$Order,$MountProof,$DismountProof){
  $d=Prop $E 'dismount';if($null-eq$d-or(T $d.kind)-cne$v.dismount){Fail 'dismount evidence or kind differs'}
  $rider=T $E.riderId;$mount=T $E.mountId
  if($null-eq$DismountProof-or-not(B $DismountProof.pass)-or(T $DismountProof.identity.abilityGuid)-cne'3af2b81f4d72bbb30501fa730fcdf36e'-or(T $DismountProof.identity.casterId)-cne$rider-or(T $DismountProof.identity.targetId)-cne$rider-or(T $DismountProof.window)-cne'combat-dismount'){Fail 'exact Dismount proof missing'}
  $pre=$d.preDismount;$afterDismount=$d.afterDismount;$dpre=$DismountProof.preClick
  if((I $dpre.allocationSequence)-ne(I $pre.allocationSequence)-or(I $dpre.frame)-ne(I $pre.frame)-or(I $dpre.gameTicks)-ne(I $pre.gameTicks)){Fail 'pre-Dismount boundary is not the exact pre-click'}
  foreach($role in @('rider','mount')){foreach($field in @('standard','move','swift')){if(-not(Near (N $dpre.state.$role.$field) (N (Res $pre $role).$field))){Fail ('pre-Dismount '+$role+' debt differs: '+$field)}}}
  $terminal=@($DismountProof.samples|Where-Object {(T $_.boundary)-ceq'terminal'});if($terminal.Count-ne1){Fail 'Dismount terminal sample absent'};$terminal=$terminal[0]
  foreach($field in @('standard','move','swift')){if(-not(Near (N $terminal.state.mount.$field) (N $dpre.state.mount.$field))){Fail ('Dismount changed mount debt: '+$field)}}
  if(-not(Near (N $terminal.state.rider.standard) (N $dpre.state.rider.standard))-or-not(Near (N $terminal.state.rider.swift) (N $dpre.state.rider.swift))-or-not(Near (N $terminal.state.rider.move) ((N $dpre.state.rider.move)+3))){Fail 'Dismount charged other than exactly one rider Move'}
  if((I $terminal.state.rider.nativePrepareCount)-ne(I $dpre.state.rider.nativePrepareCount)-or(I $terminal.state.mount.nativePrepareCount)-ne(I $dpre.state.mount.nativePrepareCount)){Fail 'Dismount repeated a native preparation'}
  if((T $pre.currentActor)-cne$rider-or(T $pre.status)-cne'Acting'-or-not(B $pre.pairIdle)-or-not(B $pre.state.rider.hasMove)){Fail 'Dismount was not requested on the idle acting rider with a lawful Move'}
  if((T $pre.state.relationshipState)-cne'Mounted'-or(T $afterDismount.state.relationshipState)-cne'Unmounted'-or-not(B $afterDismount.pairedSplit)-or(I $afterDismount.state.generation)-ne(I $pre.state.generation)-or(I $afterDismount.turnObject)-ne(I $pre.turnObject)-or(I $afterDismount.round)-ne(I $pre.round)){Fail 'Dismount did not split the activation on the same allocation'}
  if((I (Res $afterDismount 'mount').grantSequence)-ne(I (Res $pre 'mount').grantSequence)-or(I (Res $afterDismount 'rider').grantSequence)-ne(I (Res $pre 'rider').grantSequence)){Fail 'Dismount granted a preparation'}
  $mounted=$Order.mounted
  if($v.dismount-ceq'immediate'){
   if((I $pre.turnObject)-ne(I $mounted.turnObject)-or(I $pre.round)-ne(I $mounted.round)-or(I $pre.pairedSequence)-ne1){Fail "immediate Dismount is not on the Mount's own allocation"}
   if(-not(Near (N (Res $pre 'rider').move) 3)){Fail "immediate Dismount baseline is not exactly the Mount's one Move"}
   $mountTerminal=@($MountProof.samples|Where-Object {(T $_.boundary)-ceq'terminal'})[0]
   $bridge=$d.mountTerminalBridge;Assert-KmcNativePassiveResources $bridge
   foreach($key in @('frame','gameTicks','allocationSequence')){if((I $bridge.before.$key)-ne(I $mountTerminal.$key)-or(I $bridge.after.$key)-ne(I $pre.$key)){Fail 'immediate bridge clock differs'}}
   foreach($role in @('rider','mount')){if(-not(Equal $bridge.before.$role $mountTerminal.nativeAllocation.$role)-or-not(Equal $bridge.after.$role (Res $pre $role))){Fail 'immediate bridge resources differ'}}
  } else {
   $later=Prop $d 'laterTurn'
   if($null-eq$later-or(T $later.contract)-cne'full-tb-later-native-turn-dismount'-or(T $later.riderId)-cne$rider-or(T $later.mountId)-cne$mount){Fail 'later-turn evidence missing'}
   $next=$later.nextRound;$b=$later.beforeEndInput
   if((I $b.turnObject)-ne(I $mounted.turnObject)-or(I $next.round)-ne((I $mounted.round)+1)-or(I $next.turnObject)-eq(I $mounted.turnObject)-or(T $next.currentActor)-cne$rider-or(I $next.pairedSequence)-ne2-or(T $next.partnerActor)-cne$mount-or(I $next.partnerContextObject)-eq0-or(B $next.pairedSplit)){Fail 'later Dismount is not on the exact next paired allocation'}
   $continuation=$later.continuation;Assert-KmcAllocationContinuation $continuation
   if((I $continuation.before.allocationSequence)-ne(I $b.allocationSequence)-or(I $continuation.after.allocationSequence)-ne(I $next.allocationSequence)){Fail 'continuation interval differs'}
   if((I $pre.turnObject)-ne(I $next.turnObject)-or(I $pre.round)-ne(I $next.round)){Fail 'Dismount is not on the next rider allocation'}
   if($v.dismount-ceq'later-after-mount'){
    $ground=$later.groundSetup;Assert-KmcDismountGroundSetup $ground $rider $mount
    if((I $ground.before.turnObject)-ne(I $next.turnObject)){Fail 'mount expenditure is not on the next rider allocation'}
    if(-not((N (Res $ground.after 'mount').move)-gt(N (Res $ground.before 'mount').move))){Fail 'mount spent no movement'}
    $expenditureBefore=$ground.before;$expenditureAfter=$ground.after;$startBridge=Prop $later 'groundStartBridge'
   } else {
    $attack=Prop $later 'riderAttack';if($null-eq$attack){Fail 'rider expenditure missing'}
    Assert-ActorCommand $attack $rider $mount 'single-attack' 'rider expenditure'
    if((I $attack.before.turnObject)-ne(I $next.turnObject)-or(T $attack.before.status)-cne'Preparing'-or(T $attack.after.status)-cne'Acting'-or-not(B (Prop (Prop $attack 'weapon') 'ranged'))){Fail 'rider expenditure is not the next-turn Standard action'}
    if(-not(B $attack.spent.usedStandardAction)-or-not(B $attack.spent.hasMoveAction)){Fail 'rider expenditure left no lawful Move'}
    $expenditureBefore=$attack.before;$expenditureAfter=$attack.after;$startBridge=Prop $later 'attackStartBridge'
   }
   # The next paired allocation's resources are passive from its boundary to the expenditure's own pre-command boundary.
   if($null-eq$startBridge){Fail 'expenditure start bridge missing'};Assert-KmcNativePassiveResources $startBridge
   foreach($key in @('frame','gameTicks','allocationSequence')){if((I $startBridge.before.$key)-ne(I $next.$key)-or(I $startBridge.after.$key)-ne(I $expenditureBefore.$key)){Fail 'start bridge clock differs'}}
   foreach($role in @('rider','mount')){if(-not(Equal $startBridge.before.$role (Res $next $role))-or-not(Equal $startBridge.after.$role (Res $expenditureBefore $role))){Fail 'start bridge resources differ'}}
   $ready=$later.readyBridge;Assert-KmcNativePassiveResources $ready
   foreach($key in @('frame','gameTicks','allocationSequence')){if((I $ready.before.$key)-ne(I $expenditureAfter.$key)-or(I $ready.after.$key)-ne(I $pre.$key)){Fail 'ready bridge clock differs'}}
   foreach($role in @('rider','mount')){if(-not(Equal $ready.before.$role (Res $expenditureAfter $role))-or-not(Equal $ready.after.$role (Res $pre $role))){Fail 'ready bridge resources differ'}}
   foreach($field in @('standard','move','swift')){if(-not(Near (N (Res $pre 'rider').$field) (N (Res $expenditureAfter 'rider').$field))-or-not(Near (N (Res $pre 'mount').$field) (N (Res $expenditureAfter 'mount').$field))){Fail ('expenditure debt changed before the Dismount: '+$field)}}
   if(-not(B $pre.state.rider.hasMove)){Fail 'rider had no Move for the later Dismount'}
  }
  Assert-Release $E $rider $mount $afterDismount
 }
 # ---- AssertComplete ----
 if($null-eq$E-or(T (Prop $E 'contract'))-cne'chunk6a-action-economy-isolated-native-variant'){Fail 'contract differs'}
 $v=Get-KmcActionEconomyVariant ([string](Prop $E 'variant'))
 if($null-eq$v-or(T $E.scenario)-cne$v.scenario-or(T $E.row)-cne$v.row){Fail 'variant, scenario or row differs'}
 $rider=T $E.riderId;$mount=T $E.mountId
 if([string]::IsNullOrEmpty($rider)-or[string]::IsNullOrEmpty($mount)-or$rider-ceq$mount){Fail 'pair identity missing'}
 $auto=Prop $E 'automaticEnd';if($null-eq$auto-or-not(B $auto.leased)-or(B $auto.temporaryValue)){Fail 'automatic End preference was not leased false'}
 if($null-eq$Order-or(T $Order.riderId)-cne$rider-or(T $Order.mountId)-cne$mount-or(B $Order.riderFirst)-ne$v.riderFirst-or(T $Order.scenario)-cne$v.scenario){Fail 'order fixture binding differs'}
 Assert-KmcMountOrderFixture $Order $v.riderFirst
 $mounts=-not$v.riderExhaust;$dismounts=$v.dismount-cne'none';$orderCompletion=$mounts-and-not$dismounts
 if($mounts){
  if($null-eq$MountProof-or-not(B $MountProof.pass)-or(T $MountProof.identity.abilityGuid)-cne'f053faad986631688defa003cd7bda0e'-or(T $MountProof.identity.casterId)-cne$rider-or(T $MountProof.identity.targetId)-cne$mount-or(T $MountProof.window)-cne'positive-mount'){Fail 'exact Mount proof missing'}
  $expectedDisposition=if($v.riderFirst){'PreparePartnerThisRound'}else{'RetainPartnerParticipation'}
  if((T $Order.disposition)-cne$expectedDisposition){Fail 'adoption disposition differs from the declared order'}
 }
 if($orderCompletion){Assert-KmcMountOrderCore $Order $v.scenario $v.riderFirst}
 Assert-RiderEntry $E $v $(if($mounts){$MountProof}else{$null})
 if($v.mountSlot-cne'none'){$slot=Assert-MountSlot $E $v $Order;Assert-Retention $E $slot $MountProof}
 if($v.riderExhaust){Assert-Refusal $E}
 if($v.unrelated){Assert-Unrelated $E $Order}
 if($dismounts){Assert-Dismount $E $v $Order $MountProof $DismountProof}
 if($v.adjacentMount){
  if(-not(B $MountProof.preClick.state.geometry.isAdjacent)-or@($MountProof.resourceWindow.events|Where-Object {([string](Prop $_ 'boundary')).StartsWith('approach-movement')}).Count-ne0){Fail 'adjacent Mount moved'}
  $terminal=@($MountProof.samples|Where-Object {(T $_.boundary)-ceq'terminal'})[0]
  if(-not(Near (N $terminal.state.rider.move) ((N $MountProof.preClick.state.rider.move)+3))){Fail 'adjacent Mount charged other than exactly one Move'}
 }
}
function Assert-KmcActionEconomyRestoration($E){
 $auto=if($null-ne$E-and$null-ne$E.PSObject.Properties['automaticEnd']){$E.automaticEnd}else{$null}
 if($null-eq$auto-or$auto.leased-isnot[bool]-or-not$auto.leased-or$null-eq$auto.PSObject.Properties['restored']-or$auto.restored-isnot[bool]-or-not$auto.restored){throw 'Action economy: automatic End preference was not restored exactly'}
 if($null-ne$E.PSObject.Properties['unrelated']-and$null-ne$E.unrelated){
  $u=$E.unrelated;$r=if($null-ne$u.PSObject.Properties['restoration']){$u.restoration}else{$null}
  if($null-eq$r-or$r.exact-isnot[bool]-or-not$r.exact-or$r.outsideCombat-isnot[bool]-or-not$r.outsideCombat-or($r.base-isnot[int]-and$r.base-isnot[long])-or[long]$r.base-ne[long]$u.originalBase){throw 'Action economy: unrelated initiative input not restored exactly'}
 }
}
