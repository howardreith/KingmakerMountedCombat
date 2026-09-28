param([ValidateSet('Debug','Release')][string]$Configuration='Release',[switch]$BaselineOnly)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeAutoUseCaseEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
. (Join-Path $PSScriptRoot 'runtime/NativeAutoUseCaseEvidence.ps1')
$script:checks=0
function Copy-AutoCase($v){$v|ConvertTo-Json -Depth 100|ConvertFrom-Json}
function Check-Case($e,[bool]$expected,[string]$label){
 $producer=$true;$external=$true;$producerWhy='';$externalWhy=''
 try{$args=[object[]]::new(1);$args[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress));$null=$method.Invoke($null,$args)}catch{$producer=$false;$producerWhy=$_.Exception.ToString()}
 try{Assert-KmcAutoUseCase $e}catch{$external=$false;$externalWhy=$_.Exception.ToString()}
 if($producer-ne$expected-or$external-ne$expected){throw ($label+' expected='+$expected+' producer='+$producer+' external='+$external+"`nProducer: "+$producerWhy+"`nExternal: "+$externalWhy)}
 $script:checks+=2
}
foreach($kind in @('mount','dismount')){
 $p=Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot ('fixtures/chunk6a-auto-use/case-'+$kind+'.json'))|ConvertFrom-Json
 Check-Case $p $true ('baseline '+$kind)
 if($BaselineOnly){continue}
 function Reject-Case([scriptblock]$Change){$x=Copy-AutoCase $p;& $Change $x;Check-Case $x $false ($kind+' '+$Change.ToString())}
 foreach($name in @('contract','case','scenario')){Reject-Case {param($x)$x.$name='invalid'}}
 Reject-Case {param($x)$x.pauseRestored=$false};Reject-Case {param($x)$x.input.pass=$false}
 foreach($end in @('beforeSetup','beforeInput','afterInput','afterRestoration')){
  foreach($key in @('frame','gameTicks','allocationSequence','riderObject','mountObject','targetObject')){Reject-Case {param($x)$x.$end.$key++}}
  foreach($key in @('pairIdle','targetInCombat','hostileTarget')){Reject-Case {param($x)$x.$end.$key=$false}}
  Reject-Case {param($x)$x.$end.turnBased=$true};Reject-Case {param($x)$x.$end.paused=-not$x.$end.paused};Reject-Case {param($x)$x.$end.targetId='other'}
  Reject-Case {param($x)$x.$end.state.generation++};Reject-Case {param($x)$x.$end.state.ledger.forcedDetach++}
  foreach($role in @('rider','mount')){foreach($key in @('reactions','reactionCooldown','initiativeOrder','initiativeCooldown','standard','move','swift','reactionsPerRound')){
   Reject-Case {param($x)$x.$end.($role+'Resources').$key++};Reject-Case {param($x)$x.$end.state.$role.$key++}
  }}
 }
 foreach($key in @('dropped','observationErrors')){Reject-Case {param($x)$x.allocationTrace.$key++}}
 Reject-Case {param($x)$x.allocationTrace.events=@()};Reject-Case {param($x)$x.allocationTrace.observerHooks=@()}
 foreach($end in @('before','after')){foreach($key in @('frame','gameTicks','allocationSequence')){Reject-Case {param($x)$x.setupResources.$end.$key++}}}
 Reject-Case {param($x)$x.ui.restored=$false};Reject-Case {param($x)$x.ui.managerObject++};Reject-Case {param($x)$x.ui.before[0].toggle=$true};Reject-Case {param($x)$x.ui.opened[0].groupObject++};Reject-Case {param($x)$x.ui.current[0].toggle=$false};Reject-Case {param($x)$x.ui.matchingSlotObjects=@()};Reject-Case {param($x)$x.input.slotRegistration.ownerObject++}
 Reject-Case {param($x)$x.ordinaryCandidates=@()};Reject-Case {param($x)$x.ordinaryCandidates[0].object++};Reject-Case {param($x)$x.ordinaryCandidates[0].suitable=$false}
 if($kind-ceq'mount'){Reject-Case {param($x)$x.mountProof=[pscustomobject]@{pass=$true}}}else{
  Reject-Case {param($x)$x.mountProof.pass=$false};Reject-Case {param($x)$x.mountTerminalBridge=$null};Reject-Case {param($x)$x.mountProof.identity.commandObject++}
 }
}
Write-Output ('AUTO USE CASE PASS='+$script:checks+' FAIL=0; synthetic only, no native qualification')
