param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPrimaryClaimEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$method=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeRepeatedDismountEvidence',$true).GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
if($null-eq$method){throw 'Repeated Dismount producer validator method missing'}
$script:checks=0
function Copy-Repeated($value){$value|ConvertTo-Json -Depth 100 -Compress|ConvertFrom-Json}
function Put-Repeated($object,[string]$name,$value){if($object-is[Collections.IDictionary]){$object[$name]=$value}else{$object|Add-Member -NotePropertyName $name -NotePropertyValue $value -Force}}
function New-Resource([string]$actor,[int]$object,[double]$standard,[double]$move,[double]$swift){
 [ordered]@{actor=$actor;actorObject=$object;inCombat=$true;standard=$standard;move=$move;swift=$swift;reactionCooldown=0.0;initiativeCooldown=0.0;reactions=1;reactionsPerRound=1;initiativeOrder=12;grantSequence=1}
}
function New-Ledger([int]$accepted){
 [ordered]@{admittedMount=0;acceptedMount=0;admittedDismount=$accepted;acceptedDismount=$accepted;refusedVoluntary=0;forcedDetach=0;duplicateSuppressed=0;concurrentSuppressed=0}
}
function New-Causal($rider,$mount,[string]$relationship,[int]$accepted){
 $r=Copy-Repeated $rider;$m=Copy-Repeated $mount
 $r|Add-Member nativePrepareCount ([long]$r.grantSequence)
 $m|Add-Member nativePrepareCount ([long]$m.grantSequence)
 [ordered]@{rider=$r;mount=$m;selectedIds=@('rider');ledger=(New-Ledger $accepted);geometry=[ordered]@{seconds=1.0;rider=@(0.0,0.0,0.0);mount=@(1.0,0.0,0.0)};relationshipState=$relationship;generation=4}
}
function New-Controls([int]$selections,[int]$casts,[int]$accepted,[int]$activations){
 [ordered]@{targetSelectionStart=$selections;targetSelectionEnd=$selections;nativeCastRequest=$casts;nativeRefusal=0;dispatchAccepted=$accepted;dispatchRejected=0;activationCount=$activations}
}
# One native control-service activation ledger record, as the scenario projects it.
function New-ActivationRecord([long]$sequence,[long]$activationId,[string]$phase,[string]$terminal,[int]$frame=100){
 [ordered]@{sequence=$sequence;activationId=$activationId;phase=$phase;kind='Dismount';abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';frame=$frame;casterId='rider';activeSelectedUnitIds='rider';targetId='<none>';targetSelectionMode=$true;relationshipStateAtStart='Mounted';relationshipStateObserved='Mounted';riderIdAtStart='rider';mountIdAtStart='mount';riderViewChanged=$false;mountViewChanged=$false;inCombat=$true;turnBased=$false;gameMode='Default';currentTurnUnitId='<none>';lifecycleSequenceAtStart=3;lifecycleSequenceObserved=3;lifecycleDeliveries='<none>';cleanupTrigger=$null;dispatchAccepted=$null;relationshipEnded=$false;relationshipTransitionChanged=$false;relationshipTransitionResult='succeeded=True;state=Mounted';terminalResult=$terminal}
}
function New-Boundary([string]$name,[int]$frame,[long]$ticks,[int]$sequence,[string]$relationship,$rider,$mount,[int]$accepted,[int]$selections,[int]$casts,[int]$dispatch,[int]$activations){
 $mounted=$relationship-ceq'Mounted'
 [ordered]@{name=$name;frame=$frame;gameTicks=$ticks;allocationSequence=$sequence;traceComplete=$true;turnBased=$false;inCombat=$true;pairIdle=$true;riderId='rider';mountId='mount';selectedIds=@('rider');exactSingleRider=$true;selectedAbilityObject=0;riderCommandsEmpty=$true;mountCommandsEmpty=$true;shellCount=$dispatch;processBindings=$dispatch;controls=(New-Controls $selections $casts $dispatch $activations);state=(New-Causal $rider $mount $relationship $accepted);riderResources=(Copy-Repeated $rider);mountResources=(Copy-Repeated $mount);riderPosition=@(0.0,0.0,0.0);mountPosition=@(1.0,0.0,0.0);abilityPresent=$mounted;abilityObject=$(if($mounted){701}else{0});abilityDataObject=$(if($mounted){702}else{0});availabilityVisible=$mounted;availabilityEnabled=$mounted;availabilityReason=$(if($mounted){'Dismount is available.'}else{'Dismount is available only to the exact mounted rider.'})}
}
function New-DismountProof($preState,$terminalState,$terminalRider,$terminalMount){
 $identity=[ordered]@{commandObject=101;controlIdentity='shell-1';processObject=201;contextObject=301;casterId='rider';targetId='rider';generationAtInit=4;commandType='Move';abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e'}
 $before=[ordered]@{boundary='cost-before';sequence=1;command=101;commandActor='rider';actionType='Move';acted=$true;timeSinceStart=0.25;gameTicks=1010000000;state=[ordered]@{actor='rider';inCombat=$true;standard=3.0;move=0.0;swift=2.0;reactionCooldown=0.0;initiativeCooldown=0.0;reactions=1;reactionsPerRound=1;initiativeOrder=12}}
 $after=Copy-Repeated $before;$after.boundary='cost-after';$after.sequence=2;$after.state.move=2.75
 $samples=@()
 foreach($name in @('init','click-admission','move-slot-installation','approach-start','process-binding','acted','cost-before','cost-after','deliver','relationship-transition','terminal')){
  $id=Copy-Repeated $identity
  if($name-cin@('init','click-admission','move-slot-installation','approach-start')){$id.processObject=0;$id.contextObject=0}
  $terminal=$name-ceq'terminal'
  $sample=[ordered]@{boundary=$name;identity=$id;simulatingClick=$false;acted=($name-cin@('acted','cost-before','cost-after','deliver','relationship-transition','terminal'));nativeProcessBinding=($name-ceq'process-binding');deliveryContext=$(if($name-ceq'deliver'){301}else{0});frame=$(if($terminal){110}else{100});gameTicks=$(if($terminal){1020000000}else{1000000000});allocationSequence=$(if($terminal){2}else{0});state=$(if($terminal){Copy-Repeated $terminalState}else{Copy-Repeated $preState});finished=$terminal;processEnded=$terminal;result='Success'}
  if($terminal){$sample['nativeAllocation']=[ordered]@{rider=(Copy-Repeated $terminalRider);mount=(Copy-Repeated $terminalMount)}}
  $samples+=$sample
 }
 [ordered]@{pass=$true;identityComplete=$true;sameCommandAtEveryBoundary=$true;exactActedObserved=$true;nativeTerminal=$true;traceComplete=$true;initCount=1;errors=@();nativeResult='Success';identity=$identity;mountId='mount';ledgerDelta=(New-Ledger 1);predictionCommands=[ordered]@{contract='native-speculative-init-separated-from-one-committed-request';pass=$true;commands=@()};preClick=[ordered]@{frame=100;gameTicks=1000000000;allocationSequence=0;state=(Copy-Repeated $preState)};samples=$samples;resourceWindow=[ordered]@{pass=$true;reactionResources=[ordered]@{contract='native-time-and-declared-partner-preparation-only';pass=$true};inCombat=$true;turnBased=$false;events=@($before,$after)}}
}
function New-Hooks {
 @('0600934A','060093A1','060093A4','06000C3C','0600C3BE','06009120','0600838F','060026B2','06000C37')|ForEach-Object{[ordered]@{token=$_;moduleMvid='07fa1e4d-8618-41b3-9b8d-faa17d3b26f7'}}
}
function New-RepeatedCase {
 $preRider=New-Resource 'rider' 11 4.0 1.0 3.0
 $preMount=New-Resource 'mount' 12 5.0 4.0 3.0
 $terminalRider=New-Resource 'rider' 11 2.0 1.75 1.0
 $terminalMount=New-Resource 'mount' 12 3.0 2.0 1.0
 $cancelBefore=New-Boundary 'cancel-before' 100 1000000000 0 'Mounted' $preRider $preMount 0 0 0 0 8
 $cancelAfter=New-Boundary 'cancel-after' 100 1000000000 0 'Mounted' $preRider $preMount 0 1 0 0 10
 $proof=New-DismountProof $cancelAfter.state (New-Causal $terminalRider $terminalMount 'Unmounted' 1) $terminalRider $terminalMount
 $after=New-Boundary 'positive-terminal' 110 1020000000 2 'Unmounted' $terminalRider $terminalMount 1 2 1 1 16
 $terminal=@($proof.samples|Where-Object boundary -CEQ 'terminal')[0]
 $hooks=@(New-Hooks)
 $bridge=[ordered]@{contract='same-allocation-no-command-cost-with-observed-native-time-only';commandIdentity=(Copy-Repeated $proof.identity);riderId='rider';mountId='mount';turnBased=$false;traceComplete=$true;observerHooks=$hooks;before=[ordered]@{frame=$terminal.frame;gameTicks=$terminal.gameTicks;allocationSequence=$terminal.allocationSequence;rider=(Copy-Repeated $terminal.nativeAllocation.rider);mount=(Copy-Repeated $terminal.nativeAllocation.mount)};after=[ordered]@{frame=$after.frame;gameTicks=$after.gameTicks;allocationSequence=$after.allocationSequence;rider=(Copy-Repeated $after.riderResources);mount=(Copy-Repeated $after.mountResources)};events=@()}
 $e=[ordered]@{contract='cancel-once-one-native-dismount-then-repeat-refused';scenario='unmounted-attack-controls-rt';riderId='rider';mountId='mount';abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';cancellation=[ordered]@{setAbilityInvoked=$true;onClickInvoked=$false;dropAbilityInvoked=$true;before=$cancelBefore;selected=[ordered]@{handlerObject=700;abilityDataObject=702;selectedAbilityObject=702;abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';casterId='rider'};after=$cancelAfter;activationRecords=@((New-ActivationRecord 9 5 'TargetSelectionStarted' 'target-selection-started'),(New-ActivationRecord 10 5 'TargetSelectionEnded' 'target-selection-cancelled'));allocationEvents=@()};positiveClicked=$true;positiveInput=[ordered]@{clicked=$true;abilityGuid='3af2b81f4d72bbb30501fa730fcdf36e';clickedTargetId='rider';resolvedTargetId='rider'};positiveProof=$proof;afterPositive=$after;terminalBridge=$bridge;repeat=[ordered]@{before=(Copy-Repeated $after);clicked=$false;input=[ordered]@{abilityPresent=$false;handlerPresent=$true;targetViewPresent=$true;clicked=$false};inputBaseline=[ordered]@{availabilityReason='Dismount is available only to the exact mounted rider.';abilityAvailableForCast=$null};after=(Copy-Repeated $after);activationRecords=@();allocationEvents=@()}}
 $proof.window='focused-combat-dismount'
 $artifact=[ordered]@{scenario='unmounted-attack-controls-rt';observations=[ordered]@{chunk6aRepeatedDismount=(Copy-Repeated $e);chunk6aCommandProofs=@(Copy-Repeated $proof);actorAllocationTrace=[ordered]@{dropped=0;observationErrors=0;observerHooks=$hooks;events=@($proof.resourceWindow.events|ForEach-Object{Copy-Repeated $_})}};rows=@([ordered]@{name='CM05-repeated-input';status='PASS';evidence=(Copy-Repeated $e)})}
 [pscustomobject]@{evidence=$e;artifact=$artifact;request=[pscustomobject]@{scenario='unmounted-attack-controls-rt'}}
}
function Test-Producer($e){
 try{$json=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress));$invokeArgs=[object[]]::new(1);$invokeArgs[0]=$json;$null=$method.Invoke($null,$invokeArgs);$script:lastProducerError=$null;$true}catch{$script:lastProducerError=$_.Exception.ToString();$false}
}
function Test-External($e){try{Assert-KmcRepeatedDismountInput $e;$true}catch{$false}}
function Test-Envelope($case){try{Assert-KmcRepeatedDismountInputEnvelope $case.request $case.artifact;$script:lastEnvelopeError=$null;$true}catch{$script:lastEnvelopeError=$_.Exception.ToString();$false}}
function Assert-All($case,[bool]$expected,[string]$label){
 $producer=Test-Producer $case.evidence;$external=Test-External $case.evidence
 if($producer-ne$expected-or$external-ne$expected){throw "$label producer/external=$producer/$external expected=$expected producerError=$script:lastProducerError"}
 $script:checks+=2
}
$base=New-RepeatedCase
Assert-All $base $true 'valid repeated Dismount'
if(-not(Test-Envelope $base)){throw ('Valid repeated Dismount envelope rejected: '+$script:lastEnvelopeError)};$checks++

function Reject-All([string]$label,[scriptblock]$mutation){
 $case=Copy-Repeated $base;& $mutation $case
 Assert-All $case $false $label
}
Reject-All 'cancellation rider reaction allowance' {param($c)$c.evidence.cancellation.after.riderResources.reactions=0}
Reject-All 'cancellation rider reaction cooldown' {param($c)$c.evidence.cancellation.after.riderResources.reactionCooldown=0.5}
Reject-All 'cancellation causal reaction allowance' {param($c)$c.evidence.cancellation.after.state.rider.reactions=0}
Reject-All 'repeat mount reaction allowance' {param($c)$c.evidence.repeat.after.mountResources.reactions=0}
Reject-All 'repeat mount reaction cooldown' {param($c)$c.evidence.repeat.after.mountResources.reactionCooldown=0.5}
Reject-All 'repeat causal reaction cooldown' {param($c)$c.evidence.repeat.after.state.mount.reactionCooldown=0.5}
Reject-All 'positive terminal reaction allowance' {param($c)$c.evidence.positiveProof.samples[-1].state.rider.reactions=0}
Reject-All 'positive terminal reaction cooldown' {param($c)$c.evidence.positiveProof.samples[-1].state.rider.reactionCooldown=0.5}
Reject-All 'repeat click admitted' {param($c)$c.evidence.repeat.clicked=$true}
Reject-All 'exact acted missing' {param($c)$c.evidence.positiveProof.exactActedObserved=$false}
Reject-All 'cancellation allocation event' {param($c)$c.evidence.cancellation.allocationEvents=@([ordered]@{sequence=1})}
# Preview.144 c6a-repeated-dismount144-j-unmounted-controls: the cancellation lawfully appended the
# two passive target-selection ledger records (activationCount 8->10 with selection 2->3/2->3). The
# original flat-count model and the original record-less artifact shape must both stay rejected.
Reject-All 'cancellation activation records absent' {param($c)$c.evidence.cancellation.PSObject.Properties.Remove('activationRecords')}
Reject-All 'cancellation flat activation count (preview.144 validator model)' {param($c)$c.evidence.cancellation.after.controls.activationCount=$c.evidence.cancellation.before.controls.activationCount;$c.evidence.cancellation.activationRecords=@()}
Reject-All 'cancellation activation count without records (preview.144 artifact shape)' {param($c)$c.evidence.cancellation.activationRecords=@()}
Reject-All 'cancellation activation count without matching delta' {param($c)$c.evidence.cancellation.after.controls.activationCount=11}
Reject-All 'cancellation activation phase order' {param($c)$c.evidence.cancellation.activationRecords[0].phase='TargetSelectionEnded';$c.evidence.cancellation.activationRecords[1].phase='TargetSelectionStarted'}
Reject-All 'cancellation activation cast requested' {param($c)$c.evidence.cancellation.activationRecords[1].phase='CastRequested';$c.evidence.cancellation.activationRecords[1].terminalResult='native-cast-requested'}
Reject-All 'cancellation activation selection ended after cast' {param($c)$c.evidence.cancellation.activationRecords[1].terminalResult='selection-ended-after-cast'}
Reject-All 'cancellation activation dispatch accepted' {param($c)$c.evidence.cancellation.activationRecords[1].dispatchAccepted=$true}
Reject-All 'cancellation activation foreign kind' {param($c)$c.evidence.cancellation.activationRecords[0].kind='Mount'}
Reject-All 'cancellation activation foreign caster' {param($c)$c.evidence.cancellation.activationRecords[0].casterId='mount'}
Reject-All 'cancellation activation relationship ended' {param($c)$c.evidence.cancellation.activationRecords[1].relationshipEnded=$true;$c.evidence.cancellation.activationRecords[1].relationshipStateObserved='Unmounted'}
Reject-All 'cancellation activation second activation' {param($c)$c.evidence.cancellation.activationRecords[1].activationId=6}
Reject-All 'cancellation activation other frame' {param($c)$c.evidence.cancellation.activationRecords[1].frame=101}
Reject-All 'cancellation activation lifecycle delivery' {param($c)$c.evidence.cancellation.activationRecords[1].lifecycleDeliveries='4:terminal:native';$c.evidence.cancellation.activationRecords[1].lifecycleSequenceObserved=4}
Reject-All 'cancellation activation cleanup trigger' {param($c)$c.evidence.cancellation.activationRecords[1].cleanupTrigger='ForcedDetach'}
Reject-All 'repeat activation record' {param($c)$c.evidence.repeat.activationRecords=@((New-ActivationRecord 17 6 'TargetSelectionStarted' 'target-selection-started' 110));$c.evidence.repeat.after.controls.activationCount=17}
Reject-All 'repeat activation count without record' {param($c)$c.evidence.repeat.after.controls.activationCount=17}
Reject-All 'repeat activation records absent' {param($c)$c.evidence.repeat.PSObject.Properties.Remove('activationRecords')}
$bad=Copy-Repeated $base;$bad.artifact.observations.actorAllocationTrace.events=@()
if(Test-Envelope $bad){throw 'Envelope accepted a command trace omitted from the full allocation'};$checks++
$bad=Copy-Repeated $base;$bad.artifact.rows[0].evidence.repeat.clicked=$true
if(Test-Envelope $bad){throw 'Envelope accepted row evidence different from the observation'};$checks++
$contracts=@(Get-KmcChunk6aPrimaryContracts|Where-Object id -CEQ 'CM05-repeated-input')
if($contracts.Count-ne1-or$contracts[0].scenario-cne'unmounted-attack-controls-rt'-or($contracts[0].rows-join'|')-cne'CM05-repeated-input'){throw 'Repeated Dismount primary contract differs'};$checks++
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk6aRepeatedDismountScenario.cs')
$select=$source.IndexOf('EnsureChunk6aRiderSelection(Chunk6aRepeatedDismountRow)')
$baseline=$source.IndexOf('var before = CaptureChunk6aRepeatedDismountBoundary("cancel-before")')
$set=$source.IndexOf('handler.SetAbility(ability)')
$drop=$source.IndexOf('handler.DropAbility()')
if($select-lt0-or$baseline-le$select-or$set-le$baseline-or$drop-le$set){throw 'Cancellation selection/baseline/native input order differs'};$checks++
$activationBaseline=$source.IndexOf('var activationsBefore = LastChunk6aActivationSequence(nativeControls.SnapshotAbilityActivations())')
$activationCapture=$source.IndexOf('["activationRecords"] = CaptureChunk6aRepeatedDismountActivationRecords(activationsBefore)')
if($activationBaseline-lt0-or$activationBaseline-ge$baseline-or$activationCapture-le$drop){throw 'Cancellation activation ledger baseline/capture order differs'};$checks++
$repeatBaseline=$source.IndexOf('var repeatActivationsBefore = LastChunk6aActivationSequence(nativeControls.SnapshotAbilityActivations())')
$repeatClick=$source.IndexOf('var repeatedClicked = TryNativeAbilityTargetClick(')
$repeatCapture=$source.IndexOf('["activationRecords"] = CaptureChunk6aRepeatedDismountActivationRecords(repeatActivationsBefore)')
if($repeatBaseline-lt0-or$repeatClick-le$repeatBaseline-or$repeatCapture-le$repeatClick){throw 'Repeat activation ledger baseline/capture order differs'};$checks++
$tranche=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs')
$begin=$tranche.IndexOf('BeginChunk6aRepeatedDismountCommand();')
$click=$tranche.IndexOf('var dismountClicked = TryNativeAbilityTargetClick(')
$observe=$tranche.IndexOf('ObserveChunk6aRepeatedDismountClick(dismountClicked);')
if($begin-lt0-or$click-le$begin-or$observe-le$click){throw 'Positive Dismount command/input binding order differs'};$checks++
'NATIVE REPEATED DISMOUNT PASS='+$checks+' FAIL=0; synthetic producer/external/envelope only'
