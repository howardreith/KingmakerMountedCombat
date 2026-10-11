param([ValidateSet('Debug','Release')][string]$Configuration='Release',[switch]$TransactionFunctionsOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'Test-NativeGroundPlan.ps1') -FunctionsOnly -Configuration $Configuration
. (Join-Path $PSScriptRoot 'Test-NativeGroundFixtures.ps1')

. (Join-Path $PSScriptRoot 'runtime/NativePassiveResourceEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/NativePreCombatGroundEvidence.ps1')
function New-GroundTransaction([bool]$Joint=$false,$Plan=$null,$Resources=$null,[int]$CommandId=333){
 if($null-eq$Plan){$Plan=New-PlanFixture $Joint};$mover=$Plan.actorId;if($null-eq$Resources){$Resources=New-GroundResources $mover};$Resources.groundCommand.commandObject=$CommandId;foreach($e in $Resources.events){if($e.boundary-cmatch 'admission|cost'){$e.command=$CommandId}}
 foreach($pair in @(@('type','Kingmaker.UnitLogic.Commands.UnitMoveTo'),@('createdByPlayer',$true),@('finished',$true),@('nativeResult','Success'))){$resources.groundCommand|Add-Member $pair[0] $pair[1]}
 $destination=Copy-GroundPlan $plan.candidates[$plan.selectedIndex].point
 $geometry=[pscustomobject]@{riderPosition=$plan.origin;horsePosition=$plan.center};$contract='native-ground-positioning-before-fresh-encounter';$role='riderPosition'
 if($Joint){$geometry.riderPosition=$plan.center;$geometry.horsePosition=$plan.origin;$contract='native-mount-ground-positioning-before-rider-staging';$role='horsePosition'}
 $states=@{}
 foreach($boundary in @('before','after')){
  $state=[pscustomobject]@{rider=(Copy-Passive $resources.$boundary.rider);mount=(Copy-Passive $resources.$boundary.mount);geometry=(Copy-GroundPlan $geometry);selectedIds=@($mover);relationshipState='Unmounted';generation=1;ledger=@{acceptedMount=1;acceptedDismount=1;forcedDetach=1}}
  foreach($actor in @('rider','mount')){$state.$actor|Add-Member nativePrepareCount 0}
  if($boundary-ceq'after'){$state.geometry.$role=Copy-GroundPlan $destination};$states[$boundary]=$state
 }
 $command=[pscustomobject]@{id=$CommandId;type='Kingmaker.UnitLogic.Commands.UnitMoveTo';executor=$mover;acted=$false;finished=$false;result='None'};$terminal=Copy-GroundPlan $command;$terminal.acted=$true;$terminal.finished=$true;$terminal.result='Success'
 $events=@();$i=0
 foreach($boundary in @('command-bound','path-request','path-complete-before','path-complete-after','command-ended')){
  $callback=$boundary-cin@('path-complete-before','path-complete-after');$ended=$boundary-ceq'command-ended'
  $e=[pscustomobject]@{boundary=$boundary;sequence=(++$i);commandObject=$CommandId;actorId=$mover;gameTicks=([long]$Resources.before.gameTicks+$i);pathObject=222;requestSequence=1;destination=(Copy-GroundPlan $destination);points=$null;pathError=$null;pathState=$null;acted=$ended;finished=$ended;result='None'}
  if($callback){$e.pathError=$false;$e.pathState='Complete';$e.points=@((Copy-GroundPlan $plan.origin),(Copy-GroundPlan $destination))};if($ended){$e.result='Success'};$events+=@($e)
 }
 $path=[pscustomobject]@{complete=$true;errors=@();commandObject=$CommandId;actorId=$mover;events=$events;observerHooks=@(@('060018A3','060018B9','0600184F','06001850','060027B2')|ForEach-Object{[pscustomobject]@{token=$_;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}
 [pscustomobject]@{contract=$contract;moverId=$mover;plan=$plan;selection=@{exact=$true;actorId=$mover;selectedIds=@($mover);frame=$Resources.before.frame;gameTicks=$Resources.before.gameTicks};frameBefore=$Resources.before.frame;frameAfter=$Resources.after.frame;gameTicksBefore=$Resources.before.gameTicks;gameTicksAfter=$Resources.after.gameTicks;beforeInCombat=$false;afterInCombat=$false;traceComplete=$true;minimumTravel=0.25;maximumTravel=4;clearanceRadius=$plan.clearanceRadius;candidates=$plan.candidates;destination=$destination;before=$states.before;after=$states.after;admittedCommand=$command;terminalCommand=$terminal;createdByPlayer=$true;originNavigation=@{position=$plan.origin};terminalNavigation=@{position=$destination};resourceWindow=$resources;events=$resources.events;residual=0.0;path=$path}
}
if($TransactionFunctionsOnly){return}
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativePreCombatGroundEvidence',$true).GetMethod('AssertTransaction',[Reflection.BindingFlags]'Static,NonPublic')
$script:checks=0
function Check-Transaction($p,[bool]$Expected){
 $producer=$true;try{$arguments=[object[]]::new(3);$arguments[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 90 -Compress));$arguments[1]='rider';$arguments[2]='mount';$null=$method.Invoke($null,$arguments)}catch{$producer=$false;if($Expected){throw}}
 $external=$true;try{Assert-KmcNativeGroundTransaction $p rider mount}catch{$external=$false;if($Expected){throw}}
 if($producer-ne$Expected-or$external-ne$Expected){throw ('Transaction producer/external '+$producer+'/'+$external+' expected '+$Expected)};$script:checks+=2
}
foreach($joint in @($false,$true)){
 $p=New-GroundTransaction $joint;Check-Transaction $p $true
 function Reject-Transaction([scriptblock]$Mutation){$x=Copy-GroundPlan $p;& $Mutation $x;try{Check-Transaction $x $false}catch{throw ('Mutation '+$Mutation.ToString()+': '+$_)}}
 foreach($field in @('contract','moverId')){Reject-Transaction {param($x)$x.$field='foreign'}}
 foreach($field in @('minimumTravel','maximumTravel','clearanceRadius','residual')){Reject-Transaction {param($x)$x.$field++}}
 foreach($field in @('frameBefore','frameAfter','gameTicksBefore','gameTicksAfter')){Reject-Transaction {param($x)$x.$field++}}
 Reject-Transaction {param($x)$x.selection.exact=$false};Reject-Transaction {param($x)$x.selection.selectedIds=@('foreign')}
 Reject-Transaction {param($x)$x.selection.frame=102};Reject-Transaction {param($x)$x.selection.gameTicks=3000000L}
 Reject-Transaction {param($x)$x.beforeInCombat=$true};Reject-Transaction {param($x)$x.afterInCombat=$true};Reject-Transaction {param($x)$x.traceComplete=$false}
 foreach($boundary in @('before','after')){
  Reject-Transaction {param($x)$x.$boundary.relationshipState='Mounted'};Reject-Transaction {param($x)$x.$boundary.generation++};Reject-Transaction {param($x)$x.$boundary.ledger.forcedDetach++}
  Reject-Transaction {param($x)$x.$boundary.selectedIds=@('foreign')}
  foreach($actor in @('rider','mount')){
   foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){Reject-Transaction {param($x)$x.$boundary.$actor.$field++}}
   $position='riderPosition';if($actor-ceq'mount'){$position='horsePosition'};Reject-Transaction {param($x)$x.$boundary.geometry.$position.x++}
  }
 }
 Reject-Transaction {param($x)$x.destination.x++};Reject-Transaction {param($x)$x.originNavigation.position.x++};Reject-Transaction {param($x)$x.terminalNavigation.position.x++}
 Reject-Transaction {param($x)$x.createdByPlayer=$false};Reject-Transaction {param($x)$x.admittedCommand.acted=$true};Reject-Transaction {param($x)$x.terminalCommand.acted=$false};Reject-Transaction {param($x)$x.terminalCommand.finished=$false}
 foreach($field in @('id','executor','type','result')){Reject-Transaction {param($x)$x.terminalCommand.$field='foreign'}}
 foreach($field in @('type','casterId','nativeResult')){Reject-Transaction {param($x)$x.resourceWindow.groundCommand.$field='foreign'}}
 Reject-Transaction {param($x)$x.resourceWindow.groundCommand.commandObject++};Reject-Transaction {param($x)$x.resourceWindow.groundCommand.createdByPlayer=$false}
 Reject-Transaction {param($x)$x.events=@()};Reject-Transaction {param($x)$x.candidates=@()}
 foreach($i in 0..4){Reject-Transaction {param($x)$x.path.events[$i].commandObject++};Reject-Transaction {param($x)$x.path.events[$i].gameTicks=3000000L}}
 Reject-Transaction {param($x)$x.path.observerHooks=@()};Reject-Transaction {param($x)$x.path.events[1].destination.x++}
 foreach($i in 2..3){Reject-Transaction {param($x)$x.path.events[$i].pathObject++};Reject-Transaction {param($x)$x.path.events[$i].pathError=$true};Reject-Transaction {param($x)$x.path.events[$i].points[-1].x++}}
 Reject-Transaction {param($x)$x.path.events[4].result='Interrupt'};Reject-Transaction {param($x)$x.path.events[4].acted=$false}
}
'GROUND TRANSACTION PASS='+$checks+' FAIL=0; synthetic producer/external only'
