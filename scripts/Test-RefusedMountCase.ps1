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
  'policy-disabled'{$selected=@('rider');$objects=@(201);$row='CM06-combat-mount-requires-qualified-paired-policy';$reason='Mounting during combat requires paired activation to be enabled.'}
  'foreign-companion'{$selected=@('rider');$objects=@(201);$target='other';$targetObject=203;$row='CM02-foreign-companion';$reason="Mount target rejected: click the selected rider's exact active Horse."}
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
 $caseEvidence=[pscustomobject]@{contract='one-native-refused-mount-in-fresh-rt-allocation';case=$Case;scenario=('chunk6a-refused-'+$Case);row=$row;
 identity=[pscustomobject]@{riderId='rider';mountId='mount';unrelatedId='other';riderObject=201;mountObject=202;unrelatedObject=203;reciprocalPair=$true;mountProfile='Horse';unrelatedLivePartyActor=$true;unrelatedIsPet=$false;unrelatedSupportedMount=$false};
 legal=[pscustomobject]@{frame=$input.before.frame;gameTicks=$input.before.gameTicks;inCombat=$true;turnBased=$false;visible=$true;enabled=$true;canTargetOwnedMount=$true;exactRiderSelected=$true;state=$state};
 condition=[pscustomobject]@{frame=$input.before.frame;gameTicks=$input.before.gameTicks;selectionVerified=$true;inCombat=$true;turnBased=$false;visible=$true;enabled=($Case -cin @('wrong-creature-target','foreign-companion'));canTargetOwnedMount=($Case -cin @('wrong-creature-target','foreign-companion'));canTargetRequested=$false;selectedIds=$selected;selectedObjects=$objects;targetId=$target;targetObject=$targetObject;expectedReason=$reason;state=(Copy-Refused $input.before.state)};input=$input}
 if($Case-ceq'foreign-companion'){
  $caseEvidence.identity.unrelatedIsPet=$true;$caseEvidence.identity.unrelatedSupportedMount=$true
  $ownerResources=Copy-Refused $r;$ownerResources|Add-Member -NotePropertyName actor -NotePropertyValue 'owner'
  $targetResources=Copy-Refused $r;$targetResources|Add-Member -NotePropertyName actor -NotePropertyValue 'other'
  $native=[pscustomobject]@{ownerId='owner';ownerObject=204;targetId='other';targetObject=203;masterId='owner';masterObject=204;ownerPetId='other';ownerPetObject=203;ownerLiveParty=$true;targetLiveParty=$true;ownerCommandsEmpty=$true;targetCommandsEmpty=$true;targetBlueprint='e7aa96d15a45238438ae4cfb476f6bb9';targetProfile='Mammoth';ownerResources=$ownerResources;targetResources=$targetResources}
  $caseEvidence|Add-Member -NotePropertyName foreignCompanionBefore -NotePropertyValue $native
  $caseEvidence|Add-Member -NotePropertyName foreignCompanionAfter -NotePropertyValue (Copy-Refused $native)
  $caseEvidence|Add-Member foreignCompanionPreCombat ([pscustomobject]@{frame=($input.before.frame-1);gameTicks=($input.before.gameTicks-1);inCombat=$false;pair=(Copy-Refused $native)})
 }

 if($Case-ceq'policy-disabled'){
  $sample=[pscustomobject]@{frame=$input.before.frame;gameTicks=$input.before.gameTicks;allocationSequence=$input.before.allocationSequence;rider=(Copy-Refused $input.before.state.rider);mount=(Copy-Refused $input.before.state.mount)}
  foreach($actor in @('rider','mount')){
   foreach($kv in @(@('actor',$actor),@('actorObject',$(if($actor-ceq'rider'){201}else{202})),@('inCombat',$true),@('grantSequence',1))){
    $sample.$actor|Add-Member -NotePropertyName $kv[0] -NotePropertyValue $kv[1]
   }
  }
  $resources=[pscustomobject]@{contract='same-allocation-no-command-cost-with-observed-native-time-only';pass=$true;riderId='rider';mountId='mount';turnBased=$false;traceComplete=$true;before=$sample;after=(Copy-Refused $sample);events=@();observerHooks=@(foreach($token in @('0600934A','060093A1','060093A4','06000C3C','0600C3BE','06009120','0600838F','060026B2','06000C37')){[pscustomobject]@{token=$token;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}})}
  $policy=[ordered]@{contract='synchronous-paired-policy-disabled-refusal-restored';resources=$resources}
  foreach($name in @('before','disabled','restored')){
   $policy[$name]=[pscustomobject]@{frame=$sample.frame;gameTicks=$sample.gameTicks;allocationSequence=$sample.allocationSequence;state=(Copy-Refused $input.before.state);settings=[pscustomobject]@{movement=$true;paired=($name-cne'disabled');unified=$false;scheduler=$false;overlay=$false}}
  }
  $caseEvidence|Add-Member -NotePropertyName policy -NotePropertyValue ([pscustomobject]$policy)
 }

 $caseEvidence
}
$script:caseChecks=0
function Check-Case($e,[bool]$expected){
 $producer=$true;try{$arg=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 50 -Compress));$args=[object[]]::new(1);$args[0]=$arg;$null=$caseMethod.Invoke($null,$args)}catch{$producer=$false;if($expected){throw}}
 $external=$true;try{Assert-KmcRefusalCase $e}catch{$external=$false;if($expected){throw}}
 if($producer -ne $expected -or $external -ne $expected){throw ('Refusal case producer/external differs '+$producer+'/'+$external+' expected '+$expected)}
 $script:caseChecks+=2
}
foreach($case in @('wrong-creature-target','mount-selected','multiple-selection','foreign-selection','foreign-companion','policy-disabled')){
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
$case='foreign-companion';$baseline=New-RefusalCase $case
Reject-Case {param($e)$e.foreignCompanionPreCombat=$null}
Reject-Case {param($e)$e.foreignCompanionPreCombat.inCombat=$true}
Reject-Case {param($e)$e.foreignCompanionPreCombat.frame=$e.legal.frame}
Reject-Case {param($e)$e.foreignCompanionPreCombat.pair.ownerId='changed'}
Reject-Case {param($e)$e.foreignCompanionPreCombat.pair.targetObject++}
foreach($field in @('ownerId','masterId','ownerPetId','targetId','targetBlueprint','targetProfile')){Reject-Case {param($e)$e.foreignCompanionBefore.$field='invalid';$e.foreignCompanionAfter.$field='invalid'}}
foreach($field in @('ownerObject','masterObject','ownerPetObject','targetObject')){
 Reject-Case {param($e)$e.foreignCompanionBefore.$field=0;$e.foreignCompanionAfter.$field=0}
 Reject-Case {param($e)$e.foreignCompanionBefore.$field=[string]$e.foreignCompanionBefore.$field;$e.foreignCompanionAfter.$field=$e.foreignCompanionBefore.$field}
}
foreach($field in @('ownerLiveParty','targetLiveParty','ownerCommandsEmpty','targetCommandsEmpty')){Reject-Case {param($e)$e.foreignCompanionBefore.$field=$false;$e.foreignCompanionAfter.$field=$false}}
foreach($field in @('ownerResources','targetResources')){
 Reject-Case {param($e)$e.foreignCompanionBefore.$field.actor='wrong';$e.foreignCompanionAfter.$field.actor='wrong'}
 foreach($resource in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','reactionsPerRound','nativePrepareCount')){
  Reject-Case {param($e)$e.foreignCompanionAfter.$field.$resource++}
  Reject-Case {param($e)$e.foreignCompanionBefore.$field.$resource='0';$e.foreignCompanionAfter.$field.$resource='0'}
 }
}

$case='policy-disabled';$baseline=New-RefusalCase $case
Check-Case $baseline $true
# The observed131 failure changed only diagnostic capture seconds within one native frame.
$wallClock=Copy-Refused $baseline
$i=0
foreach($boundary in @($wallClock.policy.before,$wallClock.policy.disabled,$wallClock.policy.restored,$wallClock.legal,$wallClock.input.before)){
 $boundary.state.geometry|Add-Member seconds (0.25+(++$i)*0.001) -Force
}
$wallClock.condition.state=Copy-Refused $wallClock.input.before.state
Check-Case $wallClock $true
foreach($name in @('before','disabled','restored')){
 foreach($field in @('movement','paired','unified','scheduler','overlay')){
  Reject-Case {param($e)$e.policy.$name.settings.$field=-not$e.policy.$name.settings.$field}
  Reject-Case {param($e)$e.policy.$name.settings.$field='False'}
 }
 foreach($field in @('frame','gameTicks','allocationSequence')){Reject-Case {param($e)$e.policy.$name.$field++}}
 foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','nativePrepareCount')){
  Reject-Case {param($e)$e.policy.$name.state.$actor.$field++}
 }}
 Reject-Case {param($e)$e.policy.$name.state.selectedIds=@('mount')}
 Reject-Case {param($e)$e.policy.$name.state.geometry.riderPosition.x+=0.01}
 Reject-Case {param($e)$e.policy.$name.state.geometry|Add-Member shellState 'undeclared shell change' -Force}
}
foreach($field in @('riderId','mountId','contract')){Reject-Case {param($e)$e.policy.resources.$field='foreign'}}
Reject-Case {param($e)$e.policy.resources.traceComplete=$false}
Reject-Case {param($e)$e.policy.resources.pass=$false}
Reject-Case {param($e)$e.policy.resources.observerHooks=@()}
foreach($actor in @('rider','mount')){foreach($field in @('standard','move','swift','reactions','reactionCooldown','initiativeCooldown','initiativeOrder','grantSequence','reactionsPerRound')){
 Reject-Case {param($e)$e.policy.resources.after.$actor.$field++}
}}

Reject-Case {param($e)$e.policy.resources.pass='True'}
Write-Output ('REFUSAL CASE PRODUCER+EXTERNAL PASS='+$script:caseChecks+' FAIL=0; synthetic only, no native qualification')
