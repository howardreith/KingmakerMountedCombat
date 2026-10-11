param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-FullTbDismountTurn.ps1') -Configuration $Configuration
# Reuse the existing deterministic continuation/order fixtures. The separate
# ground test above already binds the immutable native full-TB failure.
function Import-LaterTurnFixture([string]$File,[string[]]$Names){
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $File),[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw 'Existing fixture parser failed'}
 foreach($name in $Names){$f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true));if($f.Count-ne1){throw $name};. ([scriptblock]::Create($f[0].Extent.Text.Replace(('function '+$name),('function script:'+$name))))}
}
Import-LaterTurnFixture 'Test-NativeAllocationContinuation.ps1' @('Copy-Continuation','New-Continuation')
Import-LaterTurnFixture 'Test-NativeMountOrder.ps1' @('New-Order')
$order=New-Order $true
$order.positiveProof|Add-Member window 'positive-mount'
$order.nextRound.status='Preparing'
$hooks=@($order.continuation.observerHooks)+@([pscustomobject]@{token='060018A9';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'},[pscustomobject]@{token='06000C5E';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'})
$order.allocationTrace|Add-Member observerHooks $hooks
$order.continuation.observerHooks=@($hooks|ForEach-Object {Copy-GroundCase $_})
$order.terminalBridge.observerHooks=@($hooks|ForEach-Object {Copy-GroundCase $_})
foreach($role in @('rider','mount')){
 $resources=Copy-GroundCase $order.nextRound.($role+'Resources')
 $resources|Add-Member nativePrepareCount $resources.grantSequence
 $resources|Add-Member nativeTurnObject $(if($role-ceq'rider'){$order.nextRound.turnObject}else{$order.nextRound.partnerContextObject})
 $resources|Add-Member measuredAllowedTime 0.0
 $order.nextRound.($role+'Resources')|Add-Member measuredAllowedTime 0.0
 $order.continuation.after.$role|Add-Member measuredAllowedTime 0.0
 $order.nextRound.state|Add-Member $role $resources
}
foreach($b in @($order.mounted,$order.beforeEndInput,$order.afterEndInput,$order.nextRound)){$b.state|Add-Member selectedIds @($order.riderId)}
Assert-KmcMountOrder $order
function Resources($b){
 [pscustomobject]@{frame=$b.frame;gameTicks=$b.gameTicks;allocationSequence=$b.allocationSequence;round=$b.round;rider=(Copy-GroundCase $b.riderResources);mount=(Copy-GroundCase $b.mountResources)}
}
function Passive($a,$b,$hooks){
 [pscustomobject]@{contract='same-allocation-no-command-cost-with-observed-native-time-only';riderId=$order.riderId;mountId=$order.mountId;turnBased=$true;traceComplete=$true;observerHooks=$hooks;before=$a;after=$b;events=@()}
}
$ground=New-DismountGroundCase
$ground.before=Copy-GroundCase $order.nextRound
$ground.before.frame++;$ground.before.state.selectedIds=@($order.riderId)
$ground.after=Copy-GroundCase $ground.before
$ground.after.frame+=10;$ground.after.allocationSequence+=9;$ground.after.status='Acting'
$ground.after.mountResources.move+=0.2;$ground.after.mountResources.measuredAllowedTime+=0.2
$ground.after.state.mount.move=$ground.after.mountResources.move
$ground.observerHooks=@($order.allocationTrace.observerHooks|ForEach-Object {Copy-GroundCase $_})
$ground.admittedCommand.executor=$order.mountId;$ground.terminalCommand.executor=$order.mountId
for($i=0;$i-lt$ground.events.Count;$i++){
 $x=$ground.events[$i];$x.sequence=$ground.before.allocationSequence+$i+1;$x.frame=$ground.before.frame+$i+1;$x.gameTicks=$ground.before.gameTicks
 $x.state=Copy-GroundCase $ground.before.mountResources;if($i-ge2){$x.state=Copy-GroundCase $ground.after.mountResources}
 $x.round=$ground.before.round;$x.turn=$ground.before.turnObject;$x.currentActor=$order.riderId
}
Assert-KmcDismountGroundSetup $ground $order.riderId $order.mountId
$ready=Copy-GroundCase $ground.after
$e=[pscustomobject]@{contract='full-tb-later-native-turn-dismount';riderId=$order.riderId;mountId=$order.mountId;reason='The rider has no Move action available to dismount.';
 beforeEndInput=(Copy-GroundCase $order.beforeEndInput);afterEndInput=(Copy-GroundCase $order.afterEndInput);endInput=(Copy-GroundCase $order.input);
 nextRound=(Copy-GroundCase $order.nextRound);continuation=(Copy-GroundCase $order.continuation);groundSetup=$ground;ready=$ready;
 mountTerminalBridge=(Copy-GroundCase $order.terminalBridge);
 groundStartBridge=(Passive (Resources $order.nextRound) (Resources $ground.before) @($ground.observerHooks|ForEach-Object {Copy-GroundCase $_}));
 readyBridge=(Passive (Resources $ground.after) (Resources $ready) @($ground.observerHooks|ForEach-Object {Copy-GroundCase $_}))}
# The native order case observes mounted one frame before actual input. Bind the
# passive bridge and continuation to the exact observed input with its same resources.
$e.mountTerminalBridge.after=Resources $e.beforeEndInput
$e.continuation.before=Resources $e.beforeEndInput
$e.continuation.events=@($e.continuation.events|Where-Object {$_.sequence-gt$e.beforeEndInput.allocationSequence})
$e.mountTerminalBridge.events=@($order.allocationTrace.events|Where-Object {$_.sequence-gt$e.mountTerminalBridge.before.allocationSequence-and$_.sequence-le$e.mountTerminalBridge.after.allocationSequence})
$trace=Copy-GroundCase $order.allocationTrace;$trace.events+=@($ground.events)
$pre=[pscustomobject]@{frame=$ready.frame;gameTicks=$ready.gameTicks;allocationSequence=$ready.allocationSequence;state=(Copy-GroundCase $ready.state)}
$aCase=[pscustomobject]@{productVersion='0.1.0-chunk6a-preview.131';scenario='chunk6a-combat-mount-tb';
 observations=[pscustomobject]@{chunk6aDismountTurn=$e;actorAllocationTrace=$trace;chunk6aCommandProofs=@((Copy-GroundCase $order.positiveProof),[pscustomobject]@{window='combat-dismount';preClick=$pre})}}
$script:envelopeChecks=0
function Check-LaterTurn($a,[bool]$Expected){$ok=$true;try{Assert-KmcFullTbDismountTurn $a}catch{$ok=$false;if($Expected){throw}};if($ok-ne$Expected){throw 'Later-turn envelope accepted corrupted evidence'};$script:envelopeChecks++}
Check-LaterTurn $aCase $true
function Reject-LaterTurn([scriptblock]$Mutate){$a=Copy-GroundCase $aCase;& $Mutate $a;try{Check-LaterTurn $a $false}catch{throw ('Mutation '+$Mutate+': '+$_)}}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.endInput.count=2}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.endInput.method='TurnController.ForceToEnd'}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.beforeEndInput.turnObject++}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.beforeEndInput.currentActor='mount'}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.beforeEndInput.state.selectedIds=@('wrong')}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.nextRound.round++}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.nextRound.turnObject=$a.observations.chunk6aDismountTurn.beforeEndInput.turnObject}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.nextRound.riderResources.grantSequence++}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.nextRound.mountResources.grantSequence++}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.groundSetup.after.currentActor=$order.mountId}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.groundSetup.terminalCommand.result='Interrupt'}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.groundSetup.after.riderResources.move=0.1}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.groundSetup.after.mountResources.reactions=0}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.ready.frame++}
Reject-LaterTurn {param($a)$a.observations.chunk6aCommandProofs[1].preClick.frame--}
Reject-LaterTurn {param($a)$a.observations.chunk6aCommandProofs[1].preClick.state.selectedIds=@($order.mountId)}
foreach($role in @('rider','mount')){
 foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder')){
  Reject-LaterTurn {param($a)$a.observations.chunk6aCommandProofs[1].preClick.state.$role.$field++}
 }
}
Reject-LaterTurn {param($a)$a.observations.chunk6aDismountTurn.readyBridge.events=@($a.observations.chunk6aDismountTurn.groundSetup.events[-1])}
Reject-LaterTurn {param($a)$a.observations.actorAllocationTrace.dropped=1}
Reject-LaterTurn {param($a)$a.observations.actorAllocationTrace.events[-1].command++}
Reject-LaterTurn {param($a)$a.observations.PSObject.Properties.Remove('chunk6aDismountTurn')}
# Ordinary same-turn Dismount remains legal when the exact native context and
# preparation are unchanged; the enclosing command reader owns its actual cost.
$same=Copy-GroundCase $aCase;$same.observations.PSObject.Properties.Remove('chunk6aDismountTurn')
$terminal=@($same.observations.chunk6aCommandProofs[0].samples|Where-Object boundary -CEQ 'terminal')[0]
$same.observations.chunk6aCommandProofs[1].preClick.state=Copy-GroundCase $terminal.state
Check-LaterTurn $same $true
'FULL TB LATER-TURN ENVELOPE PASS='+$script:envelopeChecks+' FAIL=0; synthetic composition of existing continuation/order fixtures, no qualification'
