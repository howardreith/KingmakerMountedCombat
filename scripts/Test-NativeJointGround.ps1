param([ValidateSet('Debug','Release')][string]$Configuration='Release',[switch]$JointFunctionsOnly)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-NativeGroundTransaction.ps1') -TransactionFunctionsOnly -Configuration $Configuration
. (Join-Path $PSScriptRoot 'runtime/NativePreCombatGroundEvidence.ps1')
function New-JointFixture {
 $horse=New-GroundTransaction $true;$plan=$horse.plan
 $original=New-PlanSearch rider mount $plan.center $plan.origin 1.25 $false $true $plan.occupants $null
 $initial=Copy-GroundPlan $horse.before;$initial.selectedIds=@('rider')
 $selected=$plan.candidates[$plan.selectedIndex].riderSearch;$inventory=@($selected.occupants|ForEach-Object{Copy-GroundPlan $_})
 $actual=New-PlanSearch rider mount $horse.after.geometry.riderPosition $horse.after.geometry.horsePosition 1.25 $false $false $inventory $null
 $resources=New-GroundResources rider;$resources.before=Copy-GroundPlan $horse.resourceWindow.after;$resources.after=Copy-GroundPlan $resources.before
 $resources.after.frame++;$resources.after.gameTicks+=1000000L;$resources.after.allocationSequence+=8
 # Native cooldown eligibility rejects outside-combat actors; all resources stay unchanged.
 $index=0
 foreach($e in $resources.events){
  $e.sequence=$resources.before.allocationSequence+(++$index)
  if($e.boundary-ceq'cooldown-tick-after'){$e.state=Copy-GroundPlan $resources.after.($e.state.actor)}else{$e.state=Copy-GroundPlan $resources.before.($e.state.actor)}
  if($index-le4){$e.frame=$resources.before.frame;$e.gameTicks=$resources.before.gameTicks}else{$e.frame=$resources.after.frame;$e.gameTicks=$resources.after.gameTicks}
 }
 $rider=New-GroundTransaction $false $actual $resources 334
 $joint=[pscustomobject]@{contract='bounded-joint-plan-before-two-separate-native-ground-inputs';maximumMountCandidates=72;maximumRiderCandidatesPerMount=72;maximumJointCandidates=5256;initialRiderFailure=$original;initialState=$initial;jointPlan=$plan;ground=$horse}
 [pscustomobject]@{joint=$joint;rider=$rider}
}
if($JointFunctionsOnly){return}
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativePreCombatGroundEvidence',$true).GetMethod('AssertJoint',[Reflection.BindingFlags]'Static,NonPublic')
$script:checks=0
function Check-Joint($fixture,[bool]$Expected){
 $producer=$true;try{$arguments=[object[]]::new(4);$arguments[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($fixture.joint|ConvertTo-Json -Depth 90 -Compress));$arguments[1]=[Newtonsoft.Json.Linq.JObject]::Parse(($fixture.rider|ConvertTo-Json -Depth 90 -Compress));$arguments[2]='rider';$arguments[3]='mount';$null=$method.Invoke($null,$arguments)}catch{$producer=$false;if($Expected){throw}}
 $external=$true;try{Assert-KmcNativeJointGround $fixture.joint $fixture.rider rider mount}catch{$external=$false;if($Expected){throw}}
 if($producer-ne$Expected-or$external-ne$Expected){throw ('Joint producer/external '+$producer+'/'+$external+' expected '+$Expected)};$script:checks+=2
}
$p=New-JointFixture;Check-Joint $p $true
function Reject-Joint([scriptblock]$Mutation){$x=Copy-GroundPlan $p;& $Mutation $x;try{Check-Joint $x $false}catch{throw ('Mutation '+$Mutation.ToString()+': '+$_)}}
 foreach($field in @('maximumMountCandidates','maximumRiderCandidatesPerMount','maximumJointCandidates')){Reject-Joint {param($x)$x.joint.$field++}}
 Reject-Joint {param($x)$x.joint.initialRiderFailure.candidates=@($x.joint.initialRiderFailure.candidates|Select-Object -First 71)}
 Reject-Joint {param($x)$x.joint.initialRiderFailure.selectedIndex=0}
 Reject-Joint {param($x)$x.joint.initialRiderFailure.origin.x++}
 Reject-Joint {param($x)$x.joint.initialState.ledger.forcedDetach++}
 Reject-Joint {param($x)$x.joint.initialState.generation++}
 Reject-Joint {param($x)$x.joint.initialState.geometry.riderPosition.x++}
 Reject-Joint {param($x)$x.joint.jointPlan.candidates[0].riderSearch=$null}
 Reject-Joint {param($x)$x.rider.plan.partnerPositionOverride=Copy-GroundPlan $x.rider.plan.center}
 foreach($boundary in @('before','after')){foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound')){
  Reject-Joint {param($x)$x.rider.resourceWindow.$boundary.$actor.$field++}
  Reject-Joint {param($x)$x.joint.ground.resourceWindow.$boundary.$actor.$field++}
 }}}
 # Each transaction is valid independently, but a shifted second trace/time must not bridge.
 Reject-Joint {param($x)
  $x.rider.resourceWindow.before.allocationSequence++;$x.rider.resourceWindow.after.allocationSequence++
  foreach($e in $x.rider.resourceWindow.events){$e.sequence++};foreach($e in $x.rider.events){$e.sequence++}
 }
 Reject-Joint {param($x)
  $x.rider.gameTicksBefore++;$x.rider.gameTicksAfter++;$x.rider.selection.gameTicks++
  $x.rider.resourceWindow.before.gameTicks++;$x.rider.resourceWindow.after.gameTicks++
  foreach($e in $x.rider.resourceWindow.events){$e.gameTicks++};foreach($e in $x.rider.events){$e.gameTicks++};foreach($e in $x.rider.path.events){$e.gameTicks++}
 }
'JOINT GROUND ENVELOPE PASS='+$checks+' FAIL=0; synthetic producer/external only'
