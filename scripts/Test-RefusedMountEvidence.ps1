param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RefusedMountEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeRefusedMountEvidence',$true).GetMethod('AssertInput',[Reflection.BindingFlags]'Static,NonPublic')
function Copy-Refused($v){$v|ConvertTo-Json -Depth 50|ConvertFrom-Json}
$r=[pscustomobject]@{standard=0.0;move=0.0;swift=0.0;reactions=1;reactionCooldown=0.0;initiativeCooldown=0.0;initiativeOrder=4;reactionsPerRound=1;nativePrepareCount=1}
$pos=[pscustomobject]@{x=1.0;y=2.0;z=3.0}
$before=[pscustomobject]@{frame=100;gameTicks=1000000L;allocationSequence=10;activationSequence=20;shellCount=2;processBindings=0;castCount=2;refusalCount=0;targetStartCount=2;targetEndCount=2;dispatchAccepted=2;dispatchRejected=0;targetSelectorEmpty=$true;riderCommandsEmpty=$true;mountCommandsEmpty=$true;
 state=[pscustomobject]@{rider=$r;mount=(Copy-Refused $r);selectedIds=@('mount');relationshipState='Unmounted';generation=1;ledger=[pscustomobject]@{forcedDetach=1;duplicateSuppressed=1};geometry=[pscustomobject]@{riderPosition=$pos;horsePosition=(Copy-Refused $pos)}}}
$after=Copy-Refused $before;$after.refusalCount++;$after.targetStartCount++;$after.targetEndCount++;$after.activationSequence+=3
$event=[pscustomobject]@{boundary='click-before';frame=$before.frame;gameTicks=$before.gameTicks;handlerObject=101;abilityObject=102;currentSelectorAbilityObject=102;abilityGuid='f053faad986631688defa003cd7bda0e';casterId='rider';targetId='mount';exactTargetObject=$true;exactTargetPoint=$true;button=0;simulate=$false;muteEvents=$false;result=$null}
$last=Copy-Refused $event;$last.boundary='click-after';$last.result=$false
$p=[pscustomobject]@{contract='synchronous-refused-native-mount-with-zero-command-construction';riderId='rider';mountId='mount';targetId='mount';traceComplete=$true;errors=@();constructedCommands=@();events=@($event,$last);before=$before;after=$after;allocationEvents=@();
 observerHooks=@(foreach($t in @('06002726','06002727','060093F6')){[pscustomobject]@{token=$t;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7';prefix=$(if($t -ceq '060093F6'){'ClickBefore'}else{$null});postfix=$(if($t -ceq '060093F6'){'ClickAfter'}else{'Constructed'});method=$(if($t -ceq '060093F6'){'Kingmaker.Controllers.Clicks.Handlers.ClickWithSelectedAbilityHandler.OnClick'}else{'Kingmaker.UnitLogic.Commands.UnitUseAbility..ctor'})}});
 activations=@([pscustomobject]@{sequence=21;phase='TargetSelectionStarted'},[pscustomobject]@{sequence=22;phase='CastRefused';casterId='rider';targetId='mount';selectedIds='mount';reason='Select the exact prospective rider.';frame=100;abilityGuid=$event.abilityGuid},[pscustomobject]@{sequence=23;phase='TargetSelectionEnded'})}
$script:checks=0
function Check-Refused($x,[bool]$expected){
 $producer=$true
 try{$args=[object[]]::new(5);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($x|ConvertTo-Json -Depth 50 -Compress));$args[1]='rider';$args[2]='mount';$args[3]='mount';$args[4]=[string[]]@('mount');$null=$method.Invoke($null,$args)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{Assert-KmcRefusedMountInput $x 'rider' 'mount' 'mount' @('mount')}catch{$external=$false;if($expected){throw}}
 if($producer -ne $expected -or $external -ne $expected){throw ('Refusal producer/external differs '+$producer+'/'+$external+' expected '+$expected)}
 $script:checks+=2
}
function Reject-Refused([scriptblock]$mutate){$x=Copy-Refused $p;& $mutate $x;Check-Refused $x $false}
Check-Refused $p $true
foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
 Reject-Refused {param($x)$x.after.state.$actor.$field++}
 Reject-Refused {param($x)$x.after.state.$actor.$field=$null}
}}
foreach($field in @('frame','gameTicks','shellCount','processBindings','castCount','dispatchAccepted','dispatchRejected','refusalCount','targetStartCount','targetEndCount','allocationSequence','activationSequence')){Reject-Refused {param($x)$x.after.$field++}}
foreach($field in @('targetSelectorEmpty','riderCommandsEmpty','mountCommandsEmpty')){Reject-Refused {param($x)$x.after.$field=$false}}
Reject-Refused {param($x)$x.after.state.selectedIds=@('rider')}
Reject-Refused {param($x)$x.after.state.relationshipState='Mounted'}
Reject-Refused {param($x)$x.after.state.generation++}
Reject-Refused {param($x)$x.after.state.ledger.duplicateSuppressed++}
Reject-Refused {param($x)$x.constructedCommands=@([pscustomobject]@{commandObject=111})}
Reject-Refused {param($x)$x.traceComplete=$false}
Reject-Refused {param($x)$x.errors=@('observer failed')}
Reject-Refused {param($x)$x.observerHooks=@($x.observerHooks|Select-Object -First 2)}
Reject-Refused {param($x)$x.observerHooks[0].moduleMvid='foreign'}
Reject-Refused {param($x)$x.events[1].result=$true}
Reject-Refused {param($x)$x.events[0].simulate=$true}
Reject-Refused {param($x)$x.events[0].muteEvents=$true}
Reject-Refused {param($x)$x.events[0].button=1}
Reject-Refused {param($x)$x.events[1].currentSelectorAbilityObject=0}
Reject-Refused {param($x)$x.events[1].targetId='foreign'}
Reject-Refused {param($x)$x.activations[1].reason=''}
Reject-Refused {param($x)$x.activations[1].selectedIds='rider'}
Reject-Refused {param($x)$x.activations[1].targetId='foreign'}
Reject-Refused {param($x)$x.activations[1].sequence++}
foreach($boundary in @('admission-before','cost-before','actor-cost-after','prepare-before','clear-after','opportunity-before','movement-tick')){
 Reject-Refused {param($x)$x.allocationEvents=@([pscustomobject]@{sequence=11;boundary=$boundary;state=[pscustomobject]@{actor='rider'}});$x.after.allocationSequence++}
}
foreach($actor in @('riderPosition','horsePosition')){foreach($axis in @('x','y','z')){Reject-Refused {param($x)$x.after.state.geometry.$actor.$axis+=0.01}}}
foreach($field in @('observerHooks','errors','constructedCommands','events','allocationEvents','activations')) {
 Reject-Refused {param($x)$x.$field=$null}
 Reject-Refused {param($x)$x.$field=''}
}
foreach($field in @('prefix','postfix','method')){Reject-Refused {param($x)$x.observerHooks[0].$field='wrong'}}
Reject-Refused {param($x)$x.events[0].result=$false}
Reject-Refused {param($x)$x.events[1].result='false'}
Reject-Refused {param($x)$x.before.state.selectedIds='mount'}
Reject-Refused {param($x)$x.after.frame=[string]$x.after.frame}
Reject-Refused {param($x)$x.before.castCount=-1;$x.after.castCount=-1}
Reject-Refused {param($x)$x.activations[0].phase='CastRefused'}
Write-Output ('REFUSED INPUT PASS='+$script:checks+' FAIL=0; synthetic external checks, no native qualification')
