param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# Chunk 6A action-economy rows: the compiled producer validator (through reflection) and the
# external reader accept the same synthetic variant evidence for all nine variants and reject
# the same corruptions. Synthetic fixtures composed from the existing deterministic order,
# continuation and ground factories; no Unity, no envelope, no native qualification.
. (Join-Path $PSScriptRoot 'runtime/NativeActionEconomyEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$economyType=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeActionEconomyEvidence',$true)
$economyMethod=$economyType.GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
$restorationMethod=$economyType.GetMethod('AssertRestoration',[Reflection.BindingFlags]'Static,NonPublic')
if($null-eq$economyMethod-or$null-eq$restorationMethod){throw 'Producer validators are missing'}
function Import-Factory([string]$File,[string]$Name,[scriptblock]$Transform=$null){
 $tokens=$null;$errors=$null;$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $File),[ref]$tokens,[ref]$errors)
 if($errors.Count-ne0){throw 'Fixture parse failed'}
 $f=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$Name},$true));if($f.Count-ne1){throw $Name}
 $text=$f[0].Extent.Text;if($null-ne$Transform){$text=& $Transform $text}
 . ([scriptblock]::Create($text.Replace(('function '+$Name),('function script:'+$Name))))
}
Import-Factory 'Test-NativeAllocationContinuation.ps1' 'Copy-Continuation'
Import-Factory 'Test-NativeAllocationContinuation.ps1' 'New-Continuation'
Import-Factory 'Test-NativeMountOrder.ps1' 'New-Order' {param($s)$s.Replace('$actor|Add-Member nativePrepareCount $actor.grantSequence','$actor|Add-Member -Force nativePrepareCount $actor.grantSequence').Replace('$actor|Add-Member nativeTurnObject $(','$actor|Add-Member -Force nativeTurnObject $(')}
Import-Factory 'Test-FullTbDismountTurn.ps1' 'Copy-GroundCase'
Import-Factory 'Test-FullTbDismountTurn.ps1' 'New-DismountGroundCase'
function Copy-Value($x){$x|ConvertTo-Json -Depth 100|ConvertFrom-Json}
function Put($o,[string]$n,$v){if($null-ne$o.PSObject.Properties[$n]){$o.$n=$v}else{$o|Add-Member -NotePropertyName $n -NotePropertyValue $v}}
$MountGuid='f053faad986631688defa003cd7bda0e';$DismountGuid='3af2b81f4d72bbb30501fa730fcdf36e'
$Hooks=@(foreach($token in @('0600934A','060093A1','060093A4','06000C3C','0600C3BE','06009120','0600838F','060026B2','06000C37','060018A9','06000C5E')){[pscustomobject]@{token=$token;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';method='m';prefix='p';postfix='q'}})
function New-Res([string]$actor,[int]$object,[int]$grants,[double]$standard,[double]$move,[double]$swift,[double]$allowed=0.0){
 [pscustomobject]@{actor=$actor;actorObject=$object;inCombat=$true;grantSequence=$grants;standard=$standard;move=$move;swift=$swift;initiativeCooldown=0.0;initiativeOrder=5;reactionCooldown=0.0;reactions=1;reactionsPerRound=1;waitingInitiative=$false;measuredAllowedTime=$allowed}
}
function New-State([string]$relationship,[int]$generation,[bool]$riderHasMove,[bool]$adjacent){
 [pscustomobject]@{relationshipState=$relationship;generation=$generation;rider=[pscustomobject]@{hasMove=$riderHasMove;hasStandard=$true};mount=[pscustomobject]@{hasMove=$true;hasStandard=$true};geometry=[pscustomobject]@{isAdjacent=$adjacent};ledger=[pscustomobject]@{acceptedMount=1};selectedIds=@('rider')}
}
function New-Boundary([long]$seq,[int]$frame,[long]$ticks,[int]$turn,[string]$actor,[string]$status,[int]$round,$rider,$mount,$state,[int]$pairedSequence=0,[bool]$split=$false){
 [pscustomobject]@{frame=$frame;gameTicks=$ticks;allocationSequence=$seq;turnObject=$turn;currentActor=$actor;status=$status;round=$round;controllerObject=901;sessionObject=902;turnBased=$true;pairedSequence=$pairedSequence;pairedSplit=$split;partnerContextObject=0;partnerActor=$null;riderResources=(Copy-Value $rider);mountResources=(Copy-Value $mount);state=(Copy-Value $state);pairIdle=$true}
}
function New-Event([long]$seq,[string]$boundary,$snapshot,[int]$round,[long]$command=0,[string]$commandActor=$null,[string]$actionType=$null,$acted=$null,$ignoreCooldown=$null,[int]$frame=100,[long]$ticks=1000000L){
 $s=Copy-Value $snapshot
 [pscustomobject]@{sequence=$seq;boundary=$boundary;state=$s;round=$round;frame=$frame;gameTicks=$ticks;command=$command;commandActor=$commandActor;actionType=$actionType;acted=$acted;ignoreCooldown=$ignoreCooldown;simulatingClick=$false;nativeTurnBased=$true;nativePassing=$false;nativeSurprised=$false;gameDeltaTime=0.0}
}
# One ordinary native command block: before/after boundaries, admission and cost callbacks.
function New-ActorCommand([string]$kind,[string]$actor,[int]$turn,[int]$round,[long]$startSeq,$riderBefore,$mountBefore,$riderAfter,$mountAfter,$stateBefore,$stateAfter,[string]$beforeStatus,[string]$afterStatus,[int]$commandId,[int]$frame=100,[long]$ticks=1000000L){
 $before=New-Boundary $startSeq $frame $ticks $turn $actor $beforeStatus $round $riderBefore $mountBefore $stateBefore
 $attack=$kind-ceq'single-attack'-or$kind-ceq'full-attack';$type=if($attack){'Kingmaker.UnitLogic.Commands.UnitAttack'}else{'Kingmaker.UnitLogic.Commands.UnitMoveTo'}
 $snapBefore=if($actor-ceq'rider'){$riderBefore}else{$mountBefore};$snapAfter=if($actor-ceq'rider'){$riderAfter}else{$mountAfter}
 $action=if($attack){'Standard'}else{'Move'};$ignore=-not$attack
 $events=@(
  (New-Event ($startSeq+1) 'admission-before' $snapBefore $round $commandId $actor $action $false $ignore $frame $ticks),
  (New-Event ($startSeq+2) 'admission-after' $snapBefore $round $commandId $actor $action $false $ignore $frame $ticks),
  (New-Event ($startSeq+3) 'cost-before' $(if($attack){$snapBefore}else{$snapAfter}) $round $commandId $actor $action $true $ignore ($frame+1) $ticks),
  (New-Event ($startSeq+4) 'actor-cost-before' $(if($attack){$snapBefore}else{$snapAfter}) $round $commandId $actor $action $true $ignore ($frame+1) $ticks),
  (New-Event ($startSeq+5) 'actor-cost-after' $snapAfter $round $commandId $actor $action $true $ignore ($frame+1) $ticks),
  (New-Event ($startSeq+6) 'cost-after' $snapAfter $round $commandId $actor $action $true $ignore ($frame+1) $ticks))
 $after=New-Boundary ($startSeq+6) ($frame+2) $ticks $turn $actor $afterStatus $round $riderAfter $mountAfter $stateAfter
 $full=$kind-ceq'full-attack'
 $c=[pscustomobject]@{kind=$kind;turnObject=$turn;round=$round;before=$before;after=$after;events=$events;traceComplete=$true;
  input=[pscustomobject]@{kind=$kind;clicked=$true;cursorCycles=1;fullEnabled=$full;fiveFootStep=($kind-ceq'five-foot-step');singleActionMove=$false};
  admitted=[pscustomobject]@{id=$commandId;type=$type;executor=$actor;started=$true;acted=$false;finished=$false;result='None'};
  terminal=[pscustomobject]@{id=$commandId;type=$type;executor=$actor;started=$true;acted=$true;finished=$true;result='Success'};
  stepMetres=$(if($kind-ceq'five-foot-step'){0.6}else{0.0});stepLimit=1.5;timeMoved=0.0;
  spent=[pscustomobject]@{actor=$actor;usedOneMoveAction=$false;usedTwoMoveAction=$false;usedStandardAction=$false;hasMoveAction=$true;hasStandardAction=$true;moveRestricted=$false}}
 if($attack){Put $c 'nativeFull' $full;Put $c 'nativeSingle' (-not$full);Put $c 'planned' $(if($full){3}else{1});Put $c 'completed' $(if($full){3}else{1});Put $c 'weapon' ([pscustomobject]@{ranged=($actor-ceq'rider');category=$(if($actor-ceq'rider'){'Longbow'}else{'Bite'});leaseReady=($actor-ceq'rider')})}
 $c
}
function New-CompactMountProof($order,$preClick,$terminal){
 [pscustomobject]@{window='positive-mount';pass=$true;identity=[pscustomobject]@{casterId='rider';targetId='mount';abilityGuid=$MountGuid;commandType='Move';generationAtInit=7};
  preClick=$preClick;samples=@($terminal);resourceWindow=[pscustomobject]@{pass=$true;mountPrepareDelta=0;clearCount=0;expectedMountPrepareDelta=0;events=@()}}
}
function New-Passive($before,$after){
 [pscustomobject]@{contract='same-allocation-no-command-cost-with-observed-native-time-only';riderId='rider';mountId='mount';turnBased=$true;traceComplete=$true;observerHooks=@($Hooks|ForEach-Object {Copy-Value $_});before=$before;after=$after;events=@()}
}
function Resources($b){[pscustomobject]@{frame=$b.frame;gameTicks=$b.gameTicks;allocationSequence=$b.allocationSequence;round=$b.round;rider=(Copy-Value $b.riderResources);mount=(Copy-Value $b.mountResources)}}
function Terminal-Sample($proofTerminal,$boundary,[int]$generation){
 # The exact command terminal sample: state.rider/mount carry nativePrepareCount and nativeTurnObject; nativeAllocation carries the snapshots.
 $t=[pscustomobject]@{boundary='terminal';frame=$boundary.frame;gameTicks=$boundary.gameTicks;allocationSequence=$boundary.allocationSequence;acted=$true;finished=$true;processEnded=$true;result='Success';simulatingClick=$false;
  state=[pscustomobject]@{relationshipState=$boundary.state.relationshipState;generation=$generation;ledger=(Copy-Value $boundary.state.ledger);rider=(Copy-Value $boundary.riderResources);mount=(Copy-Value $boundary.mountResources)};
  nativeAllocation=[pscustomobject]@{rider=(Copy-Value $boundary.riderResources);mount=(Copy-Value $boundary.mountResources)}}
 foreach($role in @('rider','mount')){Put $t.state.$role 'nativePrepareCount' $t.state.$role.grantSequence;Put $t.state.$role 'nativeTurnObject' $(if($role-ceq'rider'){$boundary.turnObject}else{0})}
 $t
}
function Shift-Sequences($value,[long]$threshold,[long]$delta){
 if($null-eq$value){return}
 if($value-is[array]){foreach($v in $value){Shift-Sequences $v $threshold $delta};return}
 if($value-isnot[pscustomobject]){return}
 foreach($property in @($value.PSObject.Properties)){
  if($property.Name-cin@('sequence','allocationSequence')){if([long]$property.Value-gt$threshold){$property.Value=[long]$property.Value+$delta}}
  else{Shift-Sequences $property.Value $threshold $delta}
 }
}
$script:checks=0
function Check-Economy($e,$order,$mp,$dp,[bool]$Expected,[string]$Label){
 $producer=$true;$producerError=$null
 # A JObject is enumerable: assign parsed objects directly, never through a subexpression.
 try{$a=[object[]]::new(4);$a[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress));$a[1]=[Newtonsoft.Json.Linq.JObject]::Parse(($order|ConvertTo-Json -Depth 100 -Compress))
  if($null-ne$mp){$a[2]=[Newtonsoft.Json.Linq.JObject]::Parse(($mp|ConvertTo-Json -Depth 100 -Compress))}
  if($null-ne$dp){$a[3]=[Newtonsoft.Json.Linq.JObject]::Parse(($dp|ConvertTo-Json -Depth 100 -Compress))}
  $null=$economyMethod.Invoke($null,$a)}catch{$producer=$false;$producerError=$_.Exception.InnerException;if($Expected){throw ($Label+': producer: '+$(if($null-ne$producerError){$producerError.Message}else{$_.Exception.Message}))}}
 $external=$true;$externalError=$null
 try{Assert-KmcActionEconomy $e $order $mp $dp}catch{$external=$false;$externalError=$_;if($Expected){throw ($Label+': external reader: '+$_.Exception.Message+' @ '+$_.ScriptStackTrace)}}
 if($producer-ne$Expected-or$external-ne$Expected){throw ($Label+': producer/external '+$producer+'/'+$external+' expected '+$Expected+'; producer='+$producerError+'; external='+$externalError)}
 $script:checks+=2
}
function Build-Base([bool]$first,[string]$scenario){
 $order=New-Order $first;$order.scenario=$scenario
 $e=[pscustomobject]@{contract='chunk6a-action-economy-isolated-native-variant';variant=$scenario;scenario=$scenario;row=(Get-KmcActionEconomyVariant $scenario).row;riderId='rider';mountId='mount';riderFirst=$first;
  automaticEnd=[pscustomobject]@{leased=$true;temporaryValue=$false;frame=1;restored=$true}}
 [pscustomobject]@{order=$order;e=$e}
}
# ---------------------------------------------------------------- mount slot variants ----
function Build-Spent([string]$scenario,[string]$kind){
 $base=Build-Base $false $scenario;$order=$base.order;$e=$base.e
 $mountBefore=New-Res 'mount' 102 1 0.0 0.0 0.0 0.0;$riderBefore=New-Res 'rider' 101 0 0.0 0.0 0.0 0.0
 $mountAfter=Copy-Value $mountBefore
 switch($kind){
  'ground'{$mountAfter.move=0.3;$mountAfter.measuredAllowedTime=0.3}
  'single-attack'{$mountAfter.standard=6.0}
  'full-attack'{$mountAfter.standard=6.0;$mountAfter.move=3.0}
 }
 $state=New-State 'Unmounted' 7 $true $false
 $slot=New-ActorCommand $kind 'mount' 702 1 2 $riderBefore $mountBefore $riderBefore $mountAfter $state $state 'Preparing' 'Acting' 777
 switch($kind){
  'ground'{$slot.spent.usedOneMoveAction=$true}
  'single-attack'{$slot.spent.usedStandardAction=$true;$slot.spent.hasStandardAction=$false}
  'full-attack'{$slot.spent.usedOneMoveAction=$true;$slot.spent.usedStandardAction=$true;$slot.spent.hasMoveAction=$false;$slot.spent.hasStandardAction=$false}
 }
 $endEvents=@((New-Event 9 'turn-end-before' $mountAfter 1 0 $null $null $null $null 103),(New-Event 10 'turn-end-after' $mountAfter 1 0 $null $null $null $null 103))
 $slotEnd=New-Boundary 10 104 1000000L 501 'rider' 'Preparing' 1 $riderBefore $mountAfter $state
 Put $slot 'slotEnd' $slotEnd;Put $slot 'endEvents' $endEvents
 $riderPre=Copy-Value $riderBefore;$riderPre.grantSequence=1;$riderPre.move=0.3;$riderPre.measuredAllowedTime=0.3
 $preClickBoundary=New-Boundary 12 110 1000000L 501 'rider' 'Acting' 1 $riderPre $mountAfter $state
 $tickBefore=New-Event 11 'cooldown-tick-before' $mountAfter 1 0 $null $null $null $null 110
 $tickAfter=New-Event 12 'cooldown-tick-after' $mountAfter 1 0 $null $null $null $null 110
 $retention=[pscustomobject]@{contract='mount-debt-retained-from-its-own-slot-end-to-the-rider-mount-request';before=(Copy-Value $slotEnd);after=$preClickBoundary;events=@($tickBefore,$tickAfter);traceComplete=$true;mountTurnsBetween=0}
 $preClick=[pscustomobject]@{frame=110;gameTicks=1000000L;allocationSequence=12;state=[pscustomobject]@{rider=(Copy-Value $riderPre);mount=(Copy-Value $mountAfter);geometry=[pscustomobject]@{isAdjacent=$false};generation=7;selectedIds=@('rider')}}
 Put $preClick.state.rider 'nativeTurnObject' 501;Put $preClick.state.rider 'nativePrepareCount' 1;Put $preClick.state.mount 'nativePrepareCount' 1
 $riderTerminal=Copy-Value $riderPre;$riderTerminal.move=3.3
 $terminalBoundary=New-Boundary 20 120 1000000L 501 'rider' 'Acting' 1 $riderTerminal $mountAfter (New-State 'Mounted' 8 $false $true)
 $terminal=Terminal-Sample $null $terminalBoundary 8
 $mp=New-CompactMountProof $order $preClick $terminal
 Put $e 'riderEntry' ([pscustomobject]@{kind='ground';setupContract='native-rider-ground-order-before-mount-baseline';turnObject=501})
 Put $e 'mountSlot' $slot;Put $e 'retention' $retention
 [pscustomobject]@{e=$e;order=$order;mp=$mp;dp=$null}
}
function Build-RiderOther {
 $scenario='chunk6a-rider-other-action-tb';$base=Build-Base $true $scenario;$order=$base.order;$e=$base.e
 $riderBefore=New-Res 'rider' 101 1 0.0 0.0 0.0;$mountBefore=New-Res 'mount' 102 0 0.0 0.0 0.0
 $riderAfter=Copy-Value $riderBefore;$riderAfter.standard=6.0
 $state=New-State 'Unmounted' 7 $true $false
 $entry=New-ActorCommand 'single-attack' 'rider' 501 1 5 $riderBefore $mountBefore $riderAfter $mountBefore $state $state 'Preparing' 'Acting' 778
 $entry.kind='attack';$entry.spent.usedStandardAction=$true;$entry.spent.hasStandardAction=$false
 $preClick=[pscustomobject]@{frame=110;gameTicks=1000000L;allocationSequence=14;state=[pscustomobject]@{rider=(Copy-Value $riderAfter);mount=(Copy-Value $mountBefore);geometry=[pscustomobject]@{isAdjacent=$false};generation=7;selectedIds=@('rider')}}
 Put $preClick.state.rider 'nativeTurnObject' 501;Put $preClick.state.rider 'nativePrepareCount' 1;Put $preClick.state.mount 'nativePrepareCount' 0
 $riderTerminal=Copy-Value $riderAfter;$riderTerminal.move=4.1;$mountTerminal=Copy-Value $mountBefore;$mountTerminal.grantSequence=1
 $terminal=Terminal-Sample $null (New-Boundary 24 120 1000000L 501 'rider' 'Acting' 1 $riderTerminal $mountTerminal (New-State 'Mounted' 8 $false $true)) 8
 $mp=New-CompactMountProof $order $preClick $terminal
 Put $e 'riderEntry' $entry
 [pscustomobject]@{e=$e;order=$order;mp=$mp;dp=$null}
}
function Build-Unrelated {
 $scenario='chunk6a-unrelated-candidate-between-tb';$base=Build-Base $true $scenario;$e=$base.e
 # The order factory shares object references (turns/nextRound, trace/continuation events); copy before shifting.
 $order=Copy-Value $base.order
 $mountedSeq=[long]$order.mounted.allocationSequence
 # Three unrelated native events follow the Mount inside the same round; every later sequence shifts.
 Shift-Sequences $order $mountedSeq 3
 $first=@($order.continuation.events)[0]
 $unrelatedSnapshot=[pscustomobject]@{actor='unrelated';actorObject=103;inCombat=$true;grantSequence=1;standard=0.0;move=0.0;swift=0.0;initiativeCooldown=0.0;waitingInitiative=$false}
 $inserted=@()
 $i=0;foreach($boundary in @('prepare-before','prepare-after','turn-end-after')){$i++;$inserted+=@((New-Event ($mountedSeq+$i) $boundary $unrelatedSnapshot 1 0 $null $null $null $null $first.frame $first.gameTicks))}
 $order.allocationTrace.events=@($order.allocationTrace.events|Where-Object {$_.sequence-le$mountedSeq})+$inserted+@($order.allocationTrace.events|Where-Object {$_.sequence-gt$mountedSeq})
 $order.continuation.events=$inserted+@($order.continuation.events)
 $order.mounted.roster=@([pscustomobject]@{actor='rider'},[pscustomobject]@{actor='unrelated'},[pscustomobject]@{actor='mount'})
 $order.mountBefore.mountRosterIndex=2
 $unrelatedTurn=Copy-Value $order.mounted;$unrelatedTurn.currentActor='unrelated';$unrelatedTurn.turnObject=700;$unrelatedTurn.allocationSequence=$mountedSeq+2
 $order.turns=@($order.turns|Where-Object {$_.round-ne2})+@($unrelatedTurn)+@($order.turns|Where-Object {$_.round-eq2})
 $riderBefore=New-Res 'rider' 101 1 0.0 0.3 0.0 0.3;$mountBefore=New-Res 'mount' 102 0 0.0 0.0 0.0
 $preClick=[pscustomobject]@{frame=99;gameTicks=1000000L;allocationSequence=$mountedSeq-1;state=[pscustomobject]@{rider=(Copy-Value $riderBefore);mount=(Copy-Value $mountBefore);geometry=[pscustomobject]@{isAdjacent=$false};generation=7;selectedIds=@('rider')}}
 Put $preClick.state.rider 'nativeTurnObject' 501;Put $preClick.state.rider 'nativePrepareCount' 1;Put $preClick.state.mount 'nativePrepareCount' 0
 $terminal=Terminal-Sample $null (New-Boundary $mountedSeq 100 1000000L 501 'rider' 'Acting' 1 (New-Res 'rider' 101 1 0.0 3.3 0.0 0.3) (New-Res 'mount' 102 1 0.0 0.0 0.0) (New-State 'Mounted' 8 $false $true)) 8
 $mp=New-CompactMountProof $order $preClick $terminal
 Put $e 'riderEntry' ([pscustomobject]@{kind='ground';setupContract='native-rider-ground-order-before-mount-baseline';turnObject=501})
 Put $e 'unrelated' ([pscustomobject]@{actorId='unrelated';actorObject=103;originalBase=2;inputBase=10;modifiedInput=12;outsideCombat=$true;beforeResources=(Copy-Value $unrelatedSnapshot);afterResources=(Copy-Value $unrelatedSnapshot);rosterIndexAtMount=1;riderRosterIndex=0;mountRosterIndex=2;restoration=[pscustomobject]@{outsideCombat=$true;base=2;exact=$true}})
 [pscustomobject]@{e=$e;order=$order;mp=$mp;dp=$null}
}
function Build-WithoutMove {
 $scenario='chunk6a-rider-without-move-tb';$base=Build-Base $true $scenario;$order=$base.order;$e=$base.e
 $riderBefore=New-Res 'rider' 101 1 0.0 0.3 0.0 0.3;$mountBefore=New-Res 'mount' 102 0 0.0 0.0 0.0
 $riderAfter=Copy-Value $riderBefore;$riderAfter.standard=6.0
 $stateHas=New-State 'Unmounted' 7 $true $false;$stateNo=New-State 'Unmounted' 7 $false $false
 $exhaustion=New-ActorCommand 'single-attack' 'rider' 501 1 5 $riderBefore $mountBefore $riderAfter $mountBefore $stateHas $stateNo 'Acting' 'Acting' 779
 $exhaustion.spent.usedOneMoveAction=$true;$exhaustion.spent.usedStandardAction=$true;$exhaustion.spent.hasMoveAction=$false;$exhaustion.spent.hasStandardAction=$false
 $before=New-Boundary 12 104 1000000L 501 'rider' 'Acting' 1 $riderAfter $mountBefore $stateNo
 $after=New-Boundary 12 104 1000000L 501 'rider' 'Acting' 1 $riderAfter $mountBefore $stateNo
 $controls=[pscustomobject]@{shellCount=2;processBindings=2;dispatchAccepted=2;dispatchRejected=0;activationCount=6}
 $ledger=[pscustomobject]@{admittedMount=1;acceptedMount=1;admittedDismount=1;acceptedDismount=1;refusedVoluntary=0;forcedDetach=0;duplicateSuppressed=0;concurrentSuppressed=0}
 $refusal=[pscustomobject]@{contract='mount-refused-at-availability-and-native-targeting-before-commitment';before=$before;
  availability=[pscustomobject]@{visible=$true;enabled=$false;transitionReady=$false;reason='The rider has no Move action available to mount.'};canTarget=$false;abilityAvailableForCast=$false;targetRejection='no Move';
  click=[pscustomobject]@{clicked=$false;nativeShell=[pscustomobject]@{present=$false};dispatchAcceptedDelta=0;dispatchRejectedDelta=0;nativePrimaryShellPrepareDelta=0;nativeCastRequestDelta=0;nativeRefusalDelta=0;targetSelectionStartDelta=1;targetSelectionEndDelta=1};clicked=$false;
  after=$after;controls=[pscustomobject]@{before=(Copy-Value $controls);after=(Copy-Value $controls)};ledgerBefore=(Copy-Value $ledger);ledgerAfter=(Copy-Value $ledger);riderCommandsEmptyAfter=$true;mountCommandsEmptyAfter=$true;events=@();traceComplete=$true;
  end=[pscustomobject]@{method='Kingmaker.Game.PauseBind';token='06000CB7';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';count=1;beforeEndInput=(Copy-Value $after);afterEndInput=(Copy-Value $after);afterEnd=(New-Boundary 13 105 1000000L 777 'other' 'Preparing' 1 $riderAfter $mountBefore $stateNo)}}
 Put $e 'riderEntry' ([pscustomobject]@{kind='ground';setupContract='native-rider-ground-order-before-mount-baseline';turnObject=501})
 Put $e 'exhaustion' $exhaustion;Put $e 'refusal' $refusal
 [pscustomobject]@{e=$e;order=$order;mp=$null;dp=$null}
}
# ------------------------------------------------------------------- dismount variants ----
function New-DismountProof($preBoundary,$afterRiderMove,[int]$generation){
 $preClick=[pscustomobject]@{frame=$preBoundary.frame;gameTicks=$preBoundary.gameTicks;allocationSequence=$preBoundary.allocationSequence;state=[pscustomobject]@{rider=(Copy-Value $preBoundary.riderResources);mount=(Copy-Value $preBoundary.mountResources);generation=$generation;selectedIds=@('rider')}}
 foreach($role in @('rider','mount')){Put $preClick.state.$role 'nativePrepareCount' $preClick.state.$role.grantSequence}
 $terminalBoundary=Copy-Value $preBoundary;$terminalBoundary.allocationSequence+=8;$terminalBoundary.frame+=8;$terminalBoundary.riderResources.move=$afterRiderMove;$terminalBoundary.state.relationshipState='Unmounted'
 $terminal=Terminal-Sample $null $terminalBoundary $generation
 [pscustomobject]@{window='combat-dismount';pass=$true;identity=[pscustomobject]@{casterId='rider';targetId='rider';abilityGuid=$DismountGuid;commandType='Move';generationAtInit=$generation};preClick=$preClick;samples=@($terminal);resourceWindow=[pscustomobject]@{pass=$true;events=@()}}
}
function New-Release([int]$dismountRound,$afterDismount,$riderNext,$mountNext,[int]$generation){
 $afterSeq=[long]$afterDismount.allocationSequence
 $riderTurn=New-Boundary ($afterSeq+2) ($afterDismount.frame+10) $afterDismount.gameTicks 601 'rider' 'Preparing' ($dismountRound+1) $riderNext $mountNext (New-State 'Unmounted' $generation $true $false)
 $mountTurn=New-Boundary ($afterSeq+4) ($afterDismount.frame+20) $afterDismount.gameTicks 602 'mount' 'Preparing' ($dismountRound+1) $riderNext $mountNext (New-State 'Unmounted' $generation $true $false)
 $events=@(
  (New-Event ($afterSeq+1) 'prepare-before' $riderNext ($dismountRound+1) 0 $null $null $null $null ($afterDismount.frame+10) $afterDismount.gameTicks),
  (New-Event ($afterSeq+2) 'prepare-after' $riderNext ($dismountRound+1) 0 $null $null $null $null ($afterDismount.frame+10) $afterDismount.gameTicks),
  (New-Event ($afterSeq+3) 'prepare-before' $mountNext ($dismountRound+1) 0 $null $null $null $null ($afterDismount.frame+20) $afterDismount.gameTicks),
  (New-Event ($afterSeq+4) 'prepare-after' $mountNext ($dismountRound+1) 0 $null $null $null $null ($afterDismount.frame+20) $afterDismount.gameTicks))
 [pscustomobject]@{contract='split-release-observed-through-the-mount-separate-turn-of-the-following-round';dismountRound=$dismountRound;
  endInput=[pscustomobject]@{method='Kingmaker.Game.PauseBind';token='06000CB7';moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';count=1;beforeEndInput=(Copy-Value $afterDismount);afterEndInput=(Copy-Value $afterDismount)};
  turns=@($riderTurn,$mountTurn);mountTurn=$mountTurn;events=$events;traceComplete=$true;duplicateMountTurn=$false}
}
function Build-Immediate {
 $scenario='chunk6a-dismount-immediately-after-mount-tb';$base=Build-Base $true $scenario;$order=$base.order;$e=$base.e
 $riderBefore=New-Res 'rider' 101 1 0.0 0.0 0.0;$mountBefore=New-Res 'mount' 102 0 0.0 0.0 0.0
 $stateAdjacent=New-State 'Unmounted' 7 $true $true
 $entry=New-ActorCommand 'five-foot-step' 'rider' 501 1 5 $riderBefore $mountBefore $riderBefore $mountBefore $stateAdjacent $stateAdjacent 'Preparing' 'Acting' 780
 $preClick=[pscustomobject]@{frame=110;gameTicks=1000000L;allocationSequence=14;state=[pscustomobject]@{rider=(Copy-Value $riderBefore);mount=(Copy-Value $mountBefore);geometry=[pscustomobject]@{isAdjacent=$true};generation=7;selectedIds=@('rider')}}
 Put $preClick.state.rider 'nativeTurnObject' 501;Put $preClick.state.rider 'nativePrepareCount' 1;Put $preClick.state.mount 'nativePrepareCount' 0
 $riderMounted=Copy-Value $riderBefore;$riderMounted.move=3.0;$mountMounted=Copy-Value $mountBefore;$mountMounted.grantSequence=1
 $mountedState=New-State 'Mounted' 8 $true $true
 $terminalBoundary=New-Boundary 20 120 1000000L 501 'rider' 'Acting' 1 $riderMounted $mountMounted $mountedState 1
 $terminal=Terminal-Sample $null $terminalBoundary 8
 $mp=New-CompactMountProof $order $preClick $terminal
 $pre=New-Boundary 20 120 1000000L 501 'rider' 'Acting' 1 $riderMounted $mountMounted $mountedState 1
 $bridge=New-Passive (Resources $terminalBoundary) (Resources $pre)
 $dp=New-DismountProof $pre 6.0 8
 $riderAfter=Copy-Value $riderMounted;$riderAfter.move=6.0
 $afterDismount=New-Boundary 30 130 1000000L 501 'rider' 'Acting' 1 $riderAfter $mountMounted (New-State 'Unmounted' 8 $false $true) 1 $true
 $riderNext=Copy-Value $riderAfter;$riderNext.grantSequence=2;$riderNext.move=0.0;$mountNext=Copy-Value $mountMounted;$mountNext.grantSequence=2
 Put $e 'riderEntry' $entry
 Put $e 'dismount' ([pscustomobject]@{kind='immediate';mountTurnObject=501;mountRound=1;preDismount=$pre;mountTerminalBridge=$bridge;proofWindow='combat-dismount';proofPass=$true;afterDismount=$afterDismount})
 Put $e 'release' (New-Release 1 $afterDismount $riderNext $mountNext 8)
 [pscustomobject]@{e=$e;order=$order;mp=$mp;dp=$dp}
}
function Build-Later([string]$kind){
 $scenario=if($kind-ceq'later-after-mount'){'chunk6a-dismount-after-mount-expenditure-tb'}else{'chunk6a-dismount-after-rider-expenditure-tb'}
 $base=Build-Base $true $scenario;$order=$base.order;$e=$base.e
 $order.nextRound.status='Preparing'
 $riderBefore=New-Res 'rider' 101 1 0.0 0.3 0.0 0.3;$mountBefore=New-Res 'mount' 102 0 0.0 0.0 0.0
 $preClick=[pscustomobject]@{frame=99;gameTicks=1000000L;allocationSequence=9;state=[pscustomobject]@{rider=(Copy-Value $riderBefore);mount=(Copy-Value $mountBefore);geometry=[pscustomobject]@{isAdjacent=$false};generation=7;selectedIds=@('rider')}}
 Put $preClick.state.rider 'nativeTurnObject' 501;Put $preClick.state.rider 'nativePrepareCount' 1;Put $preClick.state.mount 'nativePrepareCount' 0
 $terminal=Terminal-Sample $null (New-Boundary 10 100 1000000L 501 'rider' 'Acting' 1 (New-Res 'rider' 101 1 0.0 4.4 0.0 0.3) (New-Res 'mount' 102 1 0.0 0.0 0.0) (New-State 'Mounted' 8 $false $true) 1) 8
 $mp=New-CompactMountProof $order $preClick $terminal
 Put $e 'riderEntry' ([pscustomobject]@{kind='ground';setupContract='native-rider-ground-order-before-mount-baseline';turnObject=501})
 # The next paired allocation: resources from the order continuation's own after state.
 $next=Copy-Value $order.nextRound
 $riderNext=Copy-Value $order.continuation.after.rider;Put $riderNext 'measuredAllowedTime' 0.0;$mountNext=Copy-Value $order.continuation.after.mount;Put $mountNext 'measuredAllowedTime' 0.0
 $next.riderResources=Copy-Value $riderNext;$next.mountResources=Copy-Value $mountNext
 $nextState=New-State 'Mounted' 8 $true $true;Put $next 'state' $nextState
 $later=[pscustomobject]@{contract='full-tb-later-native-turn-dismount';riderId='rider';mountId='mount';reason='The rider has no Move action available to dismount.';
  beforeEndInput=(Copy-Value $order.beforeEndInput);afterEndInput=(Copy-Value $order.afterEndInput);endInput=(Copy-Value $order.input);nextRound=$next;continuation=(Copy-Value $order.continuation);
  mountTerminalBridge=(Copy-Value $order.terminalBridge)}
 $later.continuation.before.allocationSequence=[long]$later.beforeEndInput.allocationSequence
 $later.continuation.events=@($later.continuation.events|Where-Object {$_.sequence-gt$later.beforeEndInput.allocationSequence})
 $nextSeq=[long]$next.allocationSequence
 if($kind-ceq'later-after-mount'){
  $ground=New-DismountGroundCase
  $ground.before=New-Boundary $nextSeq ($next.frame+1) $next.gameTicks 601 'rider' 'Preparing' 2 $riderNext $mountNext $nextState 2
  $ground.before.partnerActor='mount';$ground.before.partnerContextObject=602;Put $ground.before 'adoptionCount' 1
  $ground.after=Copy-Value $ground.before;$ground.after.frame+=10;$ground.after.allocationSequence+=9;$ground.after.status='Acting'
  $ground.after.mountResources.move+=0.2;$ground.after.mountResources.measuredAllowedTime+=0.2
  $ground.observerHooks=@($Hooks|ForEach-Object {Copy-Value $_})
  $ground.admittedCommand.executor='mount';$ground.terminalCommand.executor='mount'
  for($i=0;$i-lt$ground.events.Count;$i++){
   $x=$ground.events[$i];$x.sequence=$ground.before.allocationSequence+$i+1;$x.frame=$ground.before.frame+$i+1;$x.gameTicks=$ground.before.gameTicks
   $x.state=Copy-Value $ground.before.mountResources;if($i-ge2){$x.state=Copy-Value $ground.after.mountResources}
   $x.round=2;$x.turn=601;$x.currentActor='rider'
  }
  Put $later 'groundSetup' $ground
  Put $later 'groundStartBridge' (New-Passive (Resources $next) (Resources $ground.before))
  $expenditureAfter=$ground.after
 } else {
  $riderSpent=Copy-Value $riderNext;$riderSpent.standard=6.0
  $attack=New-ActorCommand 'single-attack' 'rider' 601 2 $nextSeq $riderNext $mountNext $riderSpent $mountNext $nextState $nextState 'Preparing' 'Acting' 781 ($next.frame+1) $next.gameTicks
  $attack.spent.usedStandardAction=$true;$attack.spent.hasStandardAction=$false
  Put $later 'riderAttack' $attack
  Put $later 'attackStartBridge' (New-Passive (Resources $next) (Resources $attack.before))
  $expenditureAfter=$attack.after
 }
 $pre=Copy-Value $expenditureAfter;$pre.frame+=2;Put $pre 'pairIdle' $true;$pre.state.rider.hasMove=$true
 Put $later 'ready' (Copy-Value $pre)
 Put $later 'readyBridge' (New-Passive (Resources $expenditureAfter) (Resources $pre))
 $dp=New-DismountProof $pre ([double]$pre.riderResources.move+3.0) 8
 $riderAfter=Copy-Value $pre.riderResources;$riderAfter.move=[double]$riderAfter.move+3.0
 $afterDismount=New-Boundary ([long]$pre.allocationSequence+10) ($pre.frame+10) $pre.gameTicks 601 'rider' 'Acting' 2 $riderAfter $pre.mountResources (New-State 'Unmounted' 8 $false $true) 2 $true
 $riderR3=Copy-Value $riderAfter;$riderR3.grantSequence=[int]$riderR3.grantSequence+1;$riderR3.move=0.0;$riderR3.standard=0.0;$mountR3=Copy-Value $pre.mountResources;$mountR3.grantSequence=[int]$mountR3.grantSequence+1;$mountR3.move=0.0
 Put $e 'dismount' ([pscustomobject]@{kind=$kind;mountTurnObject=501;mountRound=1;preDismount=$pre;proofWindow='combat-dismount';proofPass=$true;afterDismount=$afterDismount;laterTurn=$later})
 Put $e 'release' (New-Release 2 $afterDismount $riderR3 $mountR3 8)
 [pscustomobject]@{e=$e;order=$order;mp=$mp;dp=$dp}
}
# ----------------------------------------------------------------------------- cases ----
$cases=[ordered]@{
 'chunk6a-mount-spent-move-tb'={Build-Spent 'chunk6a-mount-spent-move-tb' 'ground'}
 'chunk6a-mount-spent-standard-tb'={Build-Spent 'chunk6a-mount-spent-standard-tb' 'single-attack'}
 'chunk6a-mount-spent-all-tb'={Build-Spent 'chunk6a-mount-spent-all-tb' 'full-attack'}
 'chunk6a-rider-without-move-tb'={Build-WithoutMove}
 'chunk6a-rider-other-action-tb'={Build-RiderOther}
 'chunk6a-unrelated-candidate-between-tb'={Build-Unrelated}
 'chunk6a-dismount-after-rider-expenditure-tb'={Build-Later 'later-after-rider'}
 'chunk6a-dismount-after-mount-expenditure-tb'={Build-Later 'later-after-mount'}
 'chunk6a-dismount-immediately-after-mount-tb'={Build-Immediate}
}
$registered=@(Get-KmcActionEconomyScenarios)
if(($registered -join ',') -cne (@($cases.Keys) -join ',')){throw 'Variant registry differs from the fixture set'};$script:checks++
foreach($name in $cases.Keys){
 $c=& $cases[$name]
 Check-Economy $c.e $c.order $c.mp $c.dp $true ($name+' valid')
 $restoreArgs=[object[]]::new(1);$restoreArgs[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($c.e|ConvertTo-Json -Depth 100 -Compress));$null=$restorationMethod.Invoke($null,$restoreArgs);Assert-KmcActionEconomyRestoration $c.e;$script:checks+=2
 function Reject([scriptblock]$Mutate,[string]$Label){$x=Copy-Value $c.e;$o=Copy-Value $c.order;$m=$(if($null-eq$c.mp){$null}else{Copy-Value $c.mp});$d=$(if($null-eq$c.dp){$null}else{Copy-Value $c.dp});& $Mutate $x $o $m $d;try{Check-Economy $x $o $m $d $false ($name+' '+$Label)}catch{throw ('Mutation '+$Label+' on '+$name+': '+$_)}}
 Reject {param($x,$o,$m,$d)$x.contract='other'} 'contract'
 Reject {param($x,$o,$m,$d)$x.row='CM03-other'} 'row'
 Reject {param($x,$o,$m,$d)$x.automaticEnd.temporaryValue=$true} 'auto end'
 Reject {param($x,$o,$m,$d)$o.riderFirst=-not$o.riderFirst} 'order declared order'
 Reject {param($x,$o,$m,$d)$o.scenario='chunk6a-allocation-rider-first-tb'} 'order scenario'
 Reject {param($x,$o,$m,$d)$x.riderEntry.kind='other'} 'entry kind'
 $v=Get-KmcActionEconomyVariant $name
 if($v.mountSlot-cne'none'){
  Reject {param($x,$o,$m,$d)$x.mountSlot.after.mountResources.grantSequence=2} 'slot preparation'
  Reject {param($x,$o,$m,$d)$x.mountSlot.before.status='Acting'} 'slot preparing'
  Reject {param($x,$o,$m,$d)$x.mountSlot.endEvents=@($x.mountSlot.endEvents[0])} 'slot end missing'
  Reject {param($x,$o,$m,$d)$x.mountSlot.slotEnd.mountResources.move=9.0} 'slot end debt'
  Reject {param($x,$o,$m,$d)$x.retention.after.mountResources.standard=[double]$x.retention.after.mountResources.standard+1} 'retention refresh'
  Reject {param($x,$o,$m,$d)$x.retention.mountTurnsBetween=1} 'retention second turn'
  Reject {param($x,$o,$m,$d)$x.retention.events[0].boundary='prepare-before'} 'retention prepare'
  Reject {param($x,$o,$m,$d)$m.resourceWindow.clearCount=1} 'mount clear'
  Reject {param($x,$o,$m,$d)$m.preClick.allocationSequence++} 'pre-click binding'
  Reject {param($x,$o,$m,$d)$m.samples[0].state.mount.move=[double]$m.samples[0].state.mount.move+1} 'terminal mount debt'
  Reject {param($x,$o,$m,$d)$o.disposition='PreparePartnerThisRound'} 'disposition'
  Reject {param($x,$o,$m,$d)$x.mountSlot.events[2].ignoreCooldown=-not$x.mountSlot.events[2].ignoreCooldown} 'cost kind'
  Reject {param($x,$o,$m,$d)$x.mountSlot.spent.hasMoveAction=-not$x.mountSlot.spent.hasMoveAction} 'spent flags'
  if($v.mountSlot-ceq'ground'){Reject {param($x,$o,$m,$d)$x.mountSlot.after.mountResources.measuredAllowedTime=0.0} 'allowed time'}
  else{Reject {param($x,$o,$m,$d)$x.mountSlot.nativeFull=-not$x.mountSlot.nativeFull} 'attack mode'}
 }
 if($v.riderEntry-ceq'attack'){
  Reject {param($x,$o,$m,$d)$x.riderEntry.spent.usedOneMoveAction=$true} 'other action Move spent'
  Reject {param($x,$o,$m,$d)$x.riderEntry.weapon.ranged=$false} 'other action weapon'
  Reject {param($x,$o,$m,$d)$m.samples[0].state.rider.standard=0.0} 'other action prior debt'
  Reject {param($x,$o,$m,$d)$x.riderEntry.before.status='Acting'} 'other action entry'
 }
 if($v.riderEntry-ceq'five-foot-step'){
  Reject {param($x,$o,$m,$d)$x.riderEntry.after.riderResources.move=0.4} 'step charged'
  Reject {param($x,$o,$m,$d)$x.riderEntry.after.state.geometry.isAdjacent=$false} 'step envelope'
  Reject {param($x,$o,$m,$d)$x.riderEntry.stepMetres=1.6} 'step limit'
  Reject {param($x,$o,$m,$d)$m.resourceWindow.events=@([pscustomobject]@{boundary='approach-movement-before';sequence=15})} 'adjacent approach'
 }
 if($v.riderExhaust){
  Reject {param($x,$o,$m,$d)$x.refusal.availability.enabled=$true} 'availability'
  Reject {param($x,$o,$m,$d)$x.refusal.availability.reason='other'} 'reason'
  Reject {param($x,$o,$m,$d)$x.refusal.click.clicked=$true} 'click admitted'
  Reject {param($x,$o,$m,$d)$x.refusal.click.dispatchAcceptedDelta=1} 'dispatch'
  Reject {param($x,$o,$m,$d)$x.refusal.controls.after.shellCount=3} 'shell'
  Reject {param($x,$o,$m,$d)$x.refusal.ledgerAfter.acceptedMount=2} 'ledger'
  Reject {param($x,$o,$m,$d)$x.refusal.after.state.generation=8} 'generation'
  Reject {param($x,$o,$m,$d)$x.refusal.end.count=2} 'end count'
  Reject {param($x,$o,$m,$d)$x.refusal.end.afterEnd.turnObject=501} 'end turn'
  Reject {param($x,$o,$m,$d)$x.exhaustion.spent.hasMoveAction=$true} 'exhaustion move'
  Reject {param($x,$o,$m,$d)$x.refusal.before.state.rider.hasMove=$true} 'refusal move'
 }
 if($v.unrelated){
  Reject {param($x,$o,$m,$d)$x.unrelated.inputBase=20} 'unrelated input'
  Reject {param($x,$o,$m,$d)$o.mounted.roster=@($o.mounted.roster[1],$o.mounted.roster[0],$o.mounted.roster[2])} 'unrelated roster'
  Reject {param($x,$o,$m,$d)$o.allocationTrace.events=@($o.allocationTrace.events|ForEach-Object {if($_.state.actor-ceq'unrelated'-and$_.boundary-ceq'prepare-before'){$_.boundary='fixture-observation'};$_});$o.continuation.events=@($o.continuation.events|ForEach-Object {if($_.state.actor-ceq'unrelated'-and$_.boundary-ceq'prepare-before'){$_.boundary='fixture-observation'};$_})} 'unrelated skipped'
  Reject {param($x,$o,$m,$d)$o.turns=@($o.turns|Where-Object {$_.currentActor-cne'unrelated'})} 'unrelated turn'
  Reject {param($x,$o,$m,$d)$x.unrelated.actorId='rider'} 'unrelated identity'
 }
 if($v.dismount-cne'none'){
  Reject {param($x,$o,$m,$d)$d.samples[0].state.rider.move=[double]$d.samples[0].state.rider.move+3} 'dismount cost'
  Reject {param($x,$o,$m,$d)$d.samples[0].state.mount.standard=6.0} 'dismount mount debt'
  Reject {param($x,$o,$m,$d)$x.dismount.afterDismount.pairedSplit=$false} 'split'
  Reject {param($x,$o,$m,$d)$x.dismount.preDismount.state.rider.hasMove=$false} 'dismount move'
  Reject {param($x,$o,$m,$d)$x.release.turns+=@((Copy-Value $x.release.mountTurn));$x.release.turns[-1].round=$x.release.dismountRound} 'duplicate mount turn'
  Reject {param($x,$o,$m,$d)$x.release.mountTurn.mountResources.grantSequence=[int]$x.release.mountTurn.mountResources.grantSequence+1} 'mount preparation count'
  Reject {param($x,$o,$m,$d)$x.release.events[2].round=$x.release.dismountRound} 'mount prepared in release round'
  Reject {param($x,$o,$m,$d)$x.release.endInput.count=2} 'release end'
  Reject {param($x,$o,$m,$d)$x.release.mountTurn.state.relationshipState='Mounted'} 'release relationship'
  if($v.dismount-ceq'immediate'){
   Reject {param($x,$o,$m,$d)$x.dismount.preDismount.riderResources.move=3.5;$d.preClick.state.rider.move=3.5;$x.dismount.mountTerminalBridge.after.rider.move=3.5} 'immediate baseline'
   Reject {param($x,$o,$m,$d)$x.dismount.preDismount.turnObject=601;$x.dismount.afterDismount.turnObject=601;$x.release.endInput.beforeEndInput.turnObject=601} 'immediate allocation'
  } else {
   Reject {param($x,$o,$m,$d)$x.dismount.laterTurn.nextRound.pairedSequence=3} 'later paired sequence'
   Reject {param($x,$o,$m,$d)$x.dismount.preDismount.turnObject=501;$x.dismount.afterDismount.turnObject=501;$x.release.endInput.beforeEndInput.turnObject=501} 'later allocation'
   if($v.dismount-ceq'later-after-mount'){Reject {param($x,$o,$m,$d)$x.dismount.laterTurn.groundSetup.after.mountResources.move=$x.dismount.laterTurn.groundSetup.before.mountResources.move;$x.dismount.laterTurn.groundSetup.after.mountResources.measuredAllowedTime=$x.dismount.laterTurn.groundSetup.before.mountResources.measuredAllowedTime} 'mount spent nothing'}
   else{Reject {param($x,$o,$m,$d)$x.dismount.laterTurn.riderAttack.spent.hasMoveAction=$false} 'rider expenditure move'}
   Reject {param($x,$o,$m,$d)$x.dismount.laterTurn.readyBridge.before.allocationSequence++} 'ready bridge'
   $startBridge=$(if($v.dismount-ceq'later-after-mount'){'groundStartBridge'}else{'attackStartBridge'})
   Reject {param($x,$o,$m,$d)$x.dismount.laterTurn.$startBridge.after.rider.move=[double]$x.dismount.laterTurn.$startBridge.after.rider.move+1} 'start bridge resources'
   Reject {param($x,$o,$m,$d)$x.dismount.laterTurn.$startBridge.observerHooks=@()} 'start bridge hooks'
   Reject {param($x,$o,$m,$d)$x.dismount.laterTurn.PSObject.Properties.Remove($startBridge)} 'start bridge missing'
  }
 }
 if($v.mountSlot-eq'none'-and-not$v.riderExhaust-and$v.dismount-ceq'none'){
  Reject {param($x,$o,$m,$d)$o.nextRound.pairedSequence=3} 'order next activation'
 }
}
# Restoration rejections.
$restored=(& $cases['chunk6a-unrelated-candidate-between-tb']).e
foreach($mutate in @({param($x)$x.automaticEnd.restored=$false},{param($x)$x.unrelated.restoration.exact=$false},{param($x)$x.unrelated.restoration.base=3})){
 $x=Copy-Value $restored;& $mutate $x
 $producer=$true;try{$ra=[object[]]::new(1);$ra[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($x|ConvertTo-Json -Depth 100 -Compress));$null=$restorationMethod.Invoke($null,$ra)}catch{$producer=$false}
 $external=$true;try{Assert-KmcActionEconomyRestoration $x}catch{$external=$false}
 if($producer-or$external){throw 'Restoration corruption accepted'};$script:checks+=2
}
# Registry pins: nine scenarios, nine rows, every registration site.
$scenarios=@(Get-KmcActionEconomyScenarios);$rows=@(Get-KmcActionEconomyRows)
if($scenarios.Count-ne9-or$rows.Count-ne9){throw 'Variant table size differs'};$script:checks++
$variantsField=$economyType.GetField('Variants',[Reflection.BindingFlags]'Static,NonPublic');$compiled=@($variantsField.GetValue($null))
if($compiled.Count-ne9){throw 'Compiled variant table size differs'}
for($i=0;$i-lt9;$i++){
 $cv=$compiled[$i];$pv=(Get-KmcActionEconomyVariants)[$i]
 foreach($pair in @(@('Scenario','scenario'),@('Row','row'),@('MountSlot','mountSlot'),@('RiderEntry','riderEntry'),@('Dismount','dismount'),@('TargetPlacement','targetPlacement'))){
  if([string]$cv.($pair[0])-cne[string]$pv.($pair[1])){throw ('Compiled/external variant table differs: '+$pair[0]+' of '+$pv.scenario)}
 }
 foreach($pair in @(@('RiderFirst','riderFirst'),@('Unrelated','unrelated'),@('AdjacentMount','adjacentMount'),@('Longbow','longbow'),@('RiderExhaust','riderExhaust'))){
  if([bool]$cv.($pair[0])-ne[bool]$pv.($pair[1])){throw ('Compiled/external variant flag differs: '+$pair[0]+' of '+$pv.scenario)}
 }
 $script:checks++
}
$sources=@{
 launcher=Get-Content -Raw (Join-Path $repo 'scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1')
 request=Get-Content -Raw (Join-Path $repo 'scripts/runtime/Test-RuntimeRequest.ps1')
 harness=Get-Content -Raw (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
 protocol=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/RuntimeProtocol.cs')
 policy=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/HorseCompanionRegistrationScenarioPolicy.cs')
 claims=Get-Content -Raw (Join-Path $repo 'scripts/runtime/Chunk6aPrimaryClaimEvidence.ps1')
 supporting=Get-Content -Raw (Join-Path $repo 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
}
foreach($s in $scenarios){foreach($k in $sources.Keys){if($sources[$k].IndexOf("'"+$s+"'",[StringComparison]::Ordinal)-lt0-and$sources[$k].IndexOf('"'+$s+'"',[StringComparison]::Ordinal)-lt0){throw ('Scenario '+$s+' is not registered in '+$k)}};$script:checks++}
foreach($r in $rows){foreach($k in @('harness','protocol','claims','supporting')){if($sources[$k].IndexOf("'"+$r+"'",[StringComparison]::Ordinal)-lt0-and$sources[$k].IndexOf('"'+$r+'"',[StringComparison]::Ordinal)-lt0){throw ('Row '+$r+' is not registered in '+$k)}};$script:checks++}
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
foreach($v in @(Get-KmcActionEconomyVariants)){
 $binding=[pscustomobject]@{scenario=$v.scenario;rows=@($v.row)}
 if(-not(Assert-KmcIsolatedScenarioRows $v.row $binding)){throw ('Isolated qualification mapping missing for '+$v.row)}
 $bad=[pscustomobject]@{scenario='chunk6a-mount-approach';rows=@($v.row)};$rejected=$false;try{$null=Assert-KmcIsolatedScenarioRows $v.row $bad}catch{$rejected=$true};if(-not$rejected){throw 'Isolated mapping accepted another scenario'}
 $script:checks+=2
}
Write-Output ('ACTION ECONOMY PRODUCER+EXTERNAL PASS='+$script:checks+' FAIL=0; nine synthetic native variants, no Unity or envelope qualification')
