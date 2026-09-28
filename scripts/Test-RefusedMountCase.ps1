param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Test-RefusedMountEvidence.ps1') -Configuration $Configuration
. (Join-Path $PSScriptRoot 'runtime/RefusedMountCaseEvidence.ps1')
$caseMethod=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeRefusedMountCaseEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
function New-RefusalCase([string]$Case) {
 $input=Copy-Refused $p
 $reason='Select the exact prospective rider.';$target='mount';$targetObject=202
 switch($Case){
  'wrong-creature-target'{$selected=@('rider');$objects=@(201);$target='other';$targetObject=203;$row='CM02-wrong-creature-target';$reason="Mount target rejected: click the selected rider's exact active Horse."}
  'mount-selected'{$selected=@('mount');$objects=@(202);$row='CM06-mount-selected'}
  'multiple-selection'{$selected=@('rider','mount');$objects=@(201,202);$row='CM06-multiple-selection'}
  'foreign-selection'{$selected=@('other');$objects=@(203);$row='CM06-foreign-selection'}
 }
 $input.targetId=$target
 foreach($s in @($input.before,$input.after)){$s.state.selectedIds=$selected}
 foreach($s in $input.events){$s.targetId=$target}
 $input.activations[1].targetId=$target;$input.activations[1].selectedIds=$selected -join ',';$input.activations[1].reason=$reason
 $state=Copy-Refused $input.before.state;$state.selectedIds=@('rider')
 [pscustomobject]@{contract='one-native-refused-mount-in-fresh-rt-allocation';case=$Case;scenario=('chunk6a-refused-'+$Case);row=$row;
 identity=[pscustomobject]@{riderId='rider';mountId='mount';unrelatedId='other';riderObject=201;mountObject=202;unrelatedObject=203;reciprocalPair=$true;mountProfile='Horse';unrelatedLivePartyActor=$true;unrelatedIsPet=$false;unrelatedSupportedMount=$false};
 legal=[pscustomobject]@{frame=$input.before.frame;gameTicks=$input.before.gameTicks;inCombat=$true;turnBased=$false;visible=$true;enabled=$true;canTargetOwnedMount=$true;exactRiderSelected=$true;state=$state};
 condition=[pscustomobject]@{frame=$input.before.frame;gameTicks=$input.before.gameTicks;selectionVerified=$true;inCombat=$true;turnBased=$false;visible=$true;enabled=($Case -ceq 'wrong-creature-target');canTargetOwnedMount=($Case -ceq 'wrong-creature-target');canTargetRequested=$false;selectedIds=$selected;selectedObjects=$objects;targetId=$target;targetObject=$targetObject;expectedReason=$reason;state=(Copy-Refused $input.before.state)};input=$input}
}
$script:caseChecks=0
function Check-Case($e,[bool]$expected){
 $producer=$true;try{$arg=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 50 -Compress));$args=[object[]]::new(1);$args[0]=$arg;$null=$caseMethod.Invoke($null,$args)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{Assert-KmcRefusalCase $e}catch{$external=$false;if($expected){throw}}
 if($producer -ne $expected -or $external -ne $expected){throw ('Refusal case producer/external differs '+$producer+'/'+$external+' expected '+$expected)}
 $script:caseChecks+=2
}
foreach($case in @('wrong-creature-target','mount-selected','multiple-selection','foreign-selection')){
 $baseline=New-RefusalCase $case
 Check-Case $baseline $true
 function Reject-Case([scriptblock]$Mutate){$e=Copy-Refused $baseline;& $Mutate $e;try{Check-Case $e $false}catch{throw ('Case '+$case+' mutation '+$Mutate.ToString()+' field '+$field+': '+$_)}}
 foreach($field in @('contract','case','scenario','row')){Reject-Case {param($e)$e.$field='other'}}
 foreach($field in @('riderId','mountId','unrelatedId','mountProfile')){Reject-Case {param($e)$e.identity.$field=if($field -ceq 'riderId'){'mount'}else{'rider'}}}
 foreach($field in @('riderObject','mountObject','unrelatedObject')){Reject-Case {param($e)$e.identity.$field=0}}
 foreach($field in @('reciprocalPair','unrelatedLivePartyActor','unrelatedIsPet','unrelatedSupportedMount')){Reject-Case {param($e)$e.identity.$field=-not $e.identity.$field}}
 foreach($field in @('inCombat','turnBased','visible','enabled','canTargetOwnedMount','exactRiderSelected')){Reject-Case {param($e)$e.legal.$field=-not $e.legal.$field}}
 foreach($field in @('selectionVerified','inCombat','turnBased','visible','enabled','canTargetOwnedMount','canTargetRequested')){Reject-Case {param($e)$e.condition.$field=-not $e.condition.$field}}
 foreach($field in @('frame','gameTicks')){Reject-Case {param($e)$e.legal.$field++};Reject-Case {param($e)$e.condition.$field++};Reject-Case {param($e)$e.condition.$field=[string]$e.condition.$field}}
 Reject-Case {param($e)$e.legal.state.selectedIds=@('mount')}
 Reject-Case {param($e)$e.condition.selectedIds=@('bad')}
 Reject-Case {param($e)$e.condition.selectedObjects=@(999)}
 Reject-Case {param($e)$e.condition.selectedObjects=@($e.condition.selectedObjects|ForEach-Object {[string]$_})}
 Reject-Case {param($e)$e.condition.targetId='bad'}
 Reject-Case {param($e)$e.condition.targetObject=999}
 Reject-Case {param($e)$e.condition.expectedReason='A different refusal'}
 Reject-Case {param($e)$e.input.activations[1].reason='A different refusal'}
 Reject-Case {param($e)$e.input.constructedCommands=@([pscustomobject]@{commandObject=999})}
 foreach($actor in @('rider','mount')){foreach($field in @('reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
  Reject-Case {param($e)$e.legal.state.$actor.$field++}
 }}
 Reject-Case {param($e)$e.legal.state.generation++}
 Reject-Case {param($e)$e.legal.state.ledger.forcedDetach++}
 Reject-Case {param($e)$e.legal.state.geometry.riderPosition.x+=0.01}
}
Write-Output ('REFUSAL CASE PRODUCER+EXTERNAL PASS='+$script:caseChecks+' FAIL=0; synthetic only, no native qualification')
