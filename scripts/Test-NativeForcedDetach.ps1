param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# CM05-forced-detach: the producer validator (compiled DLL), the external PowerShell mirror and the
# artifact envelope are run over one synthetic observation per native life scenario and over
# mutations that each must reject. Source-order, registry and contract pins follow. No native
# qualification: these fixtures never substitute for the native runs.
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPrimaryClaimEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/NativeForcedDetachFixture.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'))
$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$type=$assembly.GetType('KingmakerMountedCombat.Diagnostics.NativeForcedDetachEvidence',$true)
$method=$type.GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')
if($null-eq$method){throw 'Forced detach producer validator method missing'}
foreach($pair in @(@('Row','CM05-forced-detach'),@('Contract','forced-detach-records-once-pays-no-voluntary-move'),@('Stimulus','labelled-native-damage-effect'))){
 $field=$type.GetField($pair[0],[Reflection.BindingFlags]'Static,NonPublic')
 if($null-eq$field-or[string]$field.GetRawConstantValue()-cne$pair[1]){throw ('Forced detach constant differs: '+$pair[0])}
}
$script:checks=0
function Copy-Forced($value){$value|ConvertTo-Json -Depth 100 -Compress|ConvertFrom-Json}
function Test-Producer($e){
 try{$json=[Newtonsoft.Json.Linq.JObject]::Parse(($e|ConvertTo-Json -Depth 100 -Compress));$invokeArgs=[object[]]::new(1);$invokeArgs[0]=$json;$null=$method.Invoke($null,$invokeArgs);$script:lastProducerError=$null;$true}catch{$script:lastProducerError=$_.Exception.ToString();$false}
}
function Test-External($e){try{Assert-KmcForcedDetach $e;$script:lastExternalError=$null;$true}catch{$script:lastExternalError=$_.Exception.ToString();$false}}
function Assert-All($e,[bool]$expected,[string]$label){
 $producer=Test-Producer $e;$external=Test-External $e
 if($producer-ne$expected-or$external-ne$expected){throw "$label producer/external=$producer/$external expected=$expected producerError=$script:lastProducerError externalError=$script:lastExternalError"}
 $script:checks+=2
}
function New-Event([int]$sequence,[string]$boundary,[string]$actor){[pscustomobject]@{sequence=$sequence;boundary=$boundary;frame=11;state=[pscustomobject]@{actor=$actor}}}

# 1. One accepted observation per native life scenario, plus the idempotent repeated delivery.
$bases=@{}
foreach($scenario in @('chunk4-rider-death-tb','chunk4-mount-death-tb','chunk4-rider-incapacitation-tb')){
 $bases[$scenario]=Copy-Forced (New-KmcForcedDetachFixture -Scenario $scenario)
 Assert-All $bases[$scenario] $true ('valid forced detach '+$scenario)
}
$death=$bases['chunk4-rider-death-tb'];$incapacitation=$bases['chunk4-rider-incapacitation-tb']
$idempotent=Copy-Forced $death
$repeat=Copy-Forced $idempotent.deliveries[0];$repeat.sequence=5;$repeat.boundary='UnitFinallyDead';$repeat.source='IUnitFinallyDeadHandler.HandleUnitBecameFinallyDead';$repeat.stateBefore='Unmounted';$repeat.stateAfter='Unmounted'
$idempotent.deliveries+=@($repeat);$idempotent.after.lifecycleSequence=5;$idempotent.after.ledger.duplicateSuppressed=1
Assert-All $idempotent $true 'idempotent repeated native delivery suppressed by the ledger'
# The native order of the life-state and death deliveries is observed, not assumed: either death boundary may end the pair.
foreach($delivery in @(@('UnitIncapacitated','Incapacitated','IUnitLifeStateChanged.HandleUnitLifeStateChanged'),@('UnitFinallyDead','Death','IUnitFinallyDeadHandler.HandleUnitBecameFinallyDead'))){
 $c=Copy-Forced $death;$c.deliveries[0].boundary=$delivery[0];$c.deliveries[0].cleanupTrigger=$delivery[1];$c.deliveries[0].source=$delivery[2]
 $c.after.lastTransition.trigger=$delivery[1];$c.after.lastTransitionResult.trigger=$delivery[1]
 Assert-All $c $true ('death delivered through '+$delivery[0])
}

# 2. Mutations each reader must reject.
function Reject-All([string]$label,[scriptblock]$mutation,$base=$death){
 $c=Copy-Forced $base;& $mutation $c
 Assert-All $c $false $label
}
Reject-All 'case id' {param($c)$c.caseId='CM05-repeated-input'}
Reject-All 'contract' {param($c)$c.contract='other'}
Reject-All 'mode' {param($c)$c.mode='RT'}
Reject-All 'foreign scenario' {param($c)$c.scenario='chunk4-charge-safety-tb'}
Reject-All 'stimulus' {param($c)$c.nativeStimulus='diagnostic-kill'}
Reject-All 'subject swapped' {param($c)$c.subject='mount';$c.survivor='rider'}
Reject-All 'before not mounted' {param($c)$c.before.relationshipState='Unmounted'}
Reject-All 'after still mounted' {param($c)$c.after.relationshipState='Mounted'}
Reject-All 'after boundary renamed' {param($c)$c.after.name='before-damage'}
Reject-All 'before without live pair command' {param($c)$c.before.pairCommand=$false}
Reject-All 'after retains pair command' {param($c)$c.after.pairCommand=$true}
Reject-All 'after retains rider command' {param($c)$c.after.riderCommandsEmpty=$false}
Reject-All 'before not in combat' {param($c)$c.before.inCombat=$false}
Reject-All 'before not turn based' {param($c)$c.before.turnBased=$false}
Reject-All 'subject alive after' {param($c)$c.after.subjectLife.dead=$false;$c.after.subjectLife.conscious=$true}
Reject-All 'subject already dead before' {param($c)$c.before.subjectLife.dead=$true;$c.before.subjectLife.conscious=$false}
Reject-All 'survivor unconscious after' {param($c)$c.after.survivorLife.conscious=$false}
Reject-All 'survivor life names other actor' {param($c)$c.after.survivorLife.actor='other'}
Reject-All 'forced detach not booked' {param($c)$c.after.ledger.forcedDetach=0}
Reject-All 'forced detach booked twice' {param($c)$c.after.ledger.forcedDetach=2}
Reject-All 'voluntary dismount admitted' {param($c)$c.after.ledger.admittedDismount=1}
Reject-All 'voluntary dismount accepted' {param($c)$c.after.ledger.acceptedDismount=1}
Reject-All 'voluntary mount admitted' {param($c)$c.after.ledger.admittedMount=2}
Reject-All 'voluntary refusal booked' {param($c)$c.after.ledger.refusedVoluntary=1}
Reject-All 'concurrent suppression booked' {param($c)$c.after.ledger.concurrentSuppressed=1}
Reject-All 'duplicate suppression without a repeat' {param($c)$c.after.ledger.duplicateSuppressed=1}
Reject-All 'native target selection started' {param($c)$c.after.controls.targetSelectionStart=3}
Reject-All 'native cast requested' {param($c)$c.after.controls.nativeCastRequest=2}
Reject-All 'native refusal booked' {param($c)$c.after.controls.nativeRefusal=1}
Reject-All 'native dispatch accepted' {param($c)$c.after.controls.dispatchAccepted=2}
Reject-All 'activation count moved without a record' {param($c)$c.after.controls.activationCount=7}
Reject-All 'activation sequence moved without a record' {param($c)$c.after.activationSequence=13}
Reject-All 'activation records absent' {param($c)$c.PSObject.Properties.Remove('activationRecords')}
Reject-All 'rider death with an appended record' {param($c)$c.activationRecords=@(New-KmcForcedDetachActivationRecord 13 11 'Death' 'UnitDeath' 'IUnitHandler.HandleUnitDeath');$c.after.activationSequence=13;$c.after.controls.activationCount=7}
Reject-All 'relationship shell created' {param($c)$c.after.shellCount=2}
Reject-All 'process binding created' {param($c)$c.after.processBindings=2}
Reject-All 'pair generation changed' {param($c)$c.after.generation=5}
Reject-All 'trace incomplete' {param($c)$c.after.traceComplete=$false}
Reject-All 'window runs backwards' {param($c)$c.after.frame=9}
Reject-All 'rider resources name other actor' {param($c)$c.before.rider.actor='other'}
Reject-All 'rider cost callback in window' {param($c)$c.allocationEvents+=@(New-Event 3 'cost-before' 'rider');$c.after.allocationSequence=3}
Reject-All 'mount cost callback in window' {param($c)$c.allocationEvents+=@(New-Event 3 'cost-after' 'mount');$c.after.allocationSequence=3}
Reject-All 'mount preparation in window' {param($c)$c.allocationEvents+=@(New-Event 3 'prepare-before' 'mount');$c.after.allocationSequence=3}
Reject-All 'rider preparation in window' {param($c)$c.allocationEvents+=@(New-Event 3 'prepare-after' 'rider');$c.after.allocationSequence=3}
Reject-All 'allocation window count differs' {param($c)$c.after.allocationSequence=5}
Reject-All 'allocation events not consecutive' {param($c)$c.allocationEvents[1].sequence=3}
Reject-All 'allocation events absent' {param($c)$c.PSObject.Properties.Remove('allocationEvents')}
Reject-All 'no lifecycle delivery' {param($c)$c.deliveries=@();$c.after.lifecycleSequence=3}
Reject-All 'lifecycle deliveries absent' {param($c)$c.PSObject.Properties.Remove('deliveries')}
Reject-All 'delivery count differs' {param($c)$c.after.lifecycleSequence=5}
Reject-All 'delivery sequence differs' {param($c)$c.deliveries[0].sequence=6}
Reject-All 'cleanup under the voluntary trigger' {param($c)$c.deliveries[0].cleanupTrigger='Manual';$c.after.lastTransition.trigger='Manual';$c.after.lastTransitionResult.trigger='Manual'}
Reject-All 'cleanup from a foreign boundary' {param($c)$c.deliveries[0].boundary='CombatEnded'}
Reject-All 'cleanup from a foreign source' {param($c)$c.deliveries[0].source='KMC diagnostic'}
Reject-All 'cleanup boundary and trigger mismatched' {param($c)$c.deliveries[0].boundary='UnitIncapacitated';$c.deliveries[0].source='IUnitLifeStateChanged.HandleUnitLifeStateChanged'}
Reject-All 'cleanup failed' {param($c)$c.deliveries[0].cleanupSucceeded=$false}
Reject-All 'cleanup with errors' {param($c)$c.deliveries[0].cleanupErrors=@('residue')}
Reject-All 'cleanup without state change' {param($c)$c.deliveries[0].stateAfter='Mounted'}
Reject-All 'cleanup not attempted' {param($c)$c.deliveries[0].cleanupAttempted=$false}
Reject-All 'second cleanup from mounted' {param($c)$second=Copy-Forced $c.deliveries[0];$second.sequence=5;$c.deliveries+=@($second);$c.after.lifecycleSequence=5;$c.after.ledger.duplicateSuppressed=1}
Reject-All 'repeat delivery not suppressed' {param($c)$c.after.ledger.duplicateSuppressed=0} $idempotent
Reject-All 'repeat delivery changes state' {param($c)$c.deliveries[1].stateAfter='Mounted'} $idempotent
Reject-All 'repeat delivery failed' {param($c)$c.deliveries[1].cleanupSucceeded=$false} $idempotent
Reject-All 'repeat delivery booked again' {param($c)$c.after.ledger.forcedDetach=2;$c.after.ledger.duplicateSuppressed=0} $idempotent
Reject-All 'last transition is voluntary' {param($c)$c.after.lastTransition.kind='VoluntaryDismount'}
Reject-All 'last transition names another pair' {param($c)$c.after.lastTransition.mountId='other'}
Reject-All 'last transition generation differs' {param($c)$c.after.lastTransition.generationBefore=3}
Reject-All 'last transition unsettled' {param($c)$c.after.lastTransition.settled=$false}
Reject-All 'last transition trigger differs' {param($c)$c.after.lastTransition.trigger='Incapacitated'}
Reject-All 'last transition absent' {param($c)$c.after.lastTransition=$null}
Reject-All 'relationship result faulted' {param($c)$c.after.lastTransitionResult.state='Faulted';$c.after.lastTransitionResult.succeeded=$false}
Reject-All 'relationship result residue' {param($c)$c.after.lastTransitionResult.presentationResidual=$true}
Reject-All 'relationship result errors' {param($c)$c.after.lastTransitionResult.errors=@('residue')}
Reject-All 'relationship result trigger differs' {param($c)$c.after.lastTransitionResult.trigger='Incapacitated'}
Reject-All 'incapacitation delivered as death' {param($c)$c.deliveries[0].boundary='UnitDeath';$c.deliveries[0].cleanupTrigger='Death';$c.deliveries[0].source='IUnitHandler.HandleUnitDeath';$c.after.lastTransition.trigger='Death';$c.after.lastTransitionResult.trigger='Death'} $incapacitation
# Mount death and rider incapacitation leave exactly one passive RelationshipEnded rider-primary record (preview.146 observation).
$mountDeath=$bases['chunk4-mount-death-tb']
Reject-All 'mount death without the appended record' {param($c)$c.activationRecords=@();$c.after.activationSequence=12;$c.after.controls.activationCount=6} $mountDeath
Reject-All 'record kept but count unchanged' {param($c)$c.after.controls.activationCount=6} $mountDeath
Reject-All 'record kept but sequence unchanged' {param($c)$c.after.activationSequence=12} $mountDeath
Reject-All 'two appended records' {param($c)$second=Copy-Forced $c.activationRecords[0];$second.sequence=14;$c.activationRecords+=@($second);$c.after.activationSequence=14;$c.after.controls.activationCount=8} $mountDeath
Reject-All 'record sequence not consecutive' {param($c)$c.activationRecords[0].sequence=14} $mountDeath
Reject-All 'record is a cast request' {param($c)$c.activationRecords[0].phase='CastRequested'} $mountDeath
Reject-All 'record is a dispatch' {param($c)$c.activationRecords[0].phase='DispatchStarted';$c.activationRecords[0].dispatchAccepted=$true} $mountDeath
Reject-All 'record is a command terminal' {param($c)$c.activationRecords[0].phase='CommandTerminal';$c.activationRecords[0].terminalResult='Success:completed'} $mountDeath
Reject-All 'record is a target selection' {param($c)$c.activationRecords[0].phase='TargetSelectionStarted';$c.activationRecords[0].targetSelectionMode=$true} $mountDeath
Reject-All 'record of a Mount control' {param($c)$c.activationRecords[0].kind='MountCompanion'} $mountDeath
Reject-All 'record of a Dismount control' {param($c)$c.activationRecords[0].kind='Dismount'} $mountDeath
Reject-All 'record of the mount primary' {param($c)$c.activationRecords[0].kind='MountPrimary'} $mountDeath
Reject-All 'record cast by the mount' {param($c)$c.activationRecords[0].casterId='mount'} $mountDeath
Reject-All 'record with a target' {param($c)$c.activationRecords[0].targetId='enemy'} $mountDeath
Reject-All 'record with a refused dispatch' {param($c)$c.activationRecords[0].dispatchAccepted=$false} $mountDeath
Reject-All 'record without the accepted dispatch' {param($c)$c.activationRecords[0].dispatchAccepted=$null} $mountDeath
Reject-All 'record transition unchanged' {param($c)$c.activationRecords[0].relationshipTransitionChanged=$false} $mountDeath
Reject-All 'record transition result differs' {param($c)$c.activationRecords[0].relationshipTransitionResult='succeeded=False;state=Faulted;trigger=Death'} $mountDeath
Reject-All 'record transition result under another trigger' {param($c)$c.activationRecords[0].relationshipTransitionResult='succeeded=True;state=Unmounted;trigger=Incapacitated'} $mountDeath
Reject-All 'record lifecycle deliveries omit the cleanup' {param($c)$c.activationRecords[0].lifecycleDeliveries='3:CombatEnded:IUnitCombatHandler.HandleUnitLeaveCombat'} $mountDeath
Reject-All 'record still mounted' {param($c)$c.activationRecords[0].relationshipStateObserved='Mounted';$c.activationRecords[0].relationshipEnded=$false} $mountDeath
Reject-All 'record under another trigger' {param($c)$c.activationRecords[0].cleanupTrigger='Incapacitated'} $mountDeath
Reject-All 'record with another terminal' {param($c)$c.activationRecords[0].terminalResult='no-active-command-observed'} $mountDeath
Reject-All 'record of another pair' {param($c)$c.activationRecords[0].mountIdAtStart='other'} $mountDeath
Reject-All 'record before the window' {param($c)$c.activationRecords[0].frame=9} $mountDeath
Reject-All 'record after the window' {param($c)$c.activationRecords[0].frame=12} $mountDeath
Reject-All 'record observed before the cleanup delivery' {param($c)$c.activationRecords[0].lifecycleSequenceObserved=3} $mountDeath
Reject-All 'incapacitation without the appended record' {param($c)$c.activationRecords=@();$c.after.activationSequence=12;$c.after.controls.activationCount=6} $incapacitation
Reject-All 'incapacitation subject dead' {param($c)$c.after.subjectLife.dead=$true} $incapacitation
Reject-All 'mount death subject is the rider' {param($c)$c.subject='rider';$c.survivor='mount'} $bases['chunk4-mount-death-tb']

# 3. Artifact envelope: the row carries the observation verbatim and the window is an exact slice of the life trace.
function New-Envelope($e,[string]$scenario){
 $leaf=if($scenario-ceq'chunk4-mount-death-tb'){'C4-LIFE-mount-death-live-command'}elseif($scenario-ceq'chunk4-rider-incapacitation-tb'){'C4-LIFE-rider-incapacitation'}else{'C4-LIFE-rider-death-live-command'}
 $trace=[ordered]@{dropped=0;observationErrors=0;events=@((New-Event 1 'turn-end-before' 'other1'),(New-Event 2 'turn-end-after' 'other1'),(New-Event 3 'prepare-before' 'other2'))}
 $life=[ordered]@{subject=$e.subject;survivor=$e.survivor;allocationSequenceBeforeDamage=0;beforeDamage=[ordered]@{frame=10};allocationTrace=$trace}
 $artifact=[ordered]@{scenario=$scenario;rows=@([ordered]@{name=$leaf;status='PASS';evidence=$life},[ordered]@{name='CM05-forced-detach';status='PASS';evidence=$e});observations=[ordered]@{chunk6aForcedDetach=$e}}
 [pscustomobject]@{request=[pscustomobject]@{scenario=$scenario};artifact=(Copy-Forced $artifact)}
}
function Test-Envelope($case){try{Assert-KmcForcedDetachEnvelope $case.request $case.artifact;$script:lastEnvelopeError=$null;$true}catch{$script:lastEnvelopeError=$_.Exception.ToString();$false}}
foreach($scenario in @($bases.Keys)){
 $case=New-Envelope $bases[$scenario] $scenario
 if(-not(Test-Envelope $case)){throw ('Valid forced detach envelope rejected for '+$scenario+': '+$script:lastEnvelopeError)};$checks++
}
function Reject-Envelope([string]$label,[scriptblock]$mutation){
 $case=New-Envelope $death 'chunk4-rider-death-tb';& $mutation $case
 if(Test-Envelope $case){throw ('Envelope accepted '+$label)};$script:checks++
}
Reject-Envelope 'row evidence different from the observation' {param($c)$c.artifact.rows[1].evidence.after.ledger.forcedDetach=2}
Reject-Envelope 'observation absent' {param($c)$c.artifact.observations.PSObject.Properties.Remove('chunk6aForcedDetach')}
Reject-Envelope 'a trace slice that differs from the window' {param($c)$c.artifact.rows[0].evidence.allocationTrace.events[0].frame=12}
Reject-Envelope 'a window opened after the damage baseline' {param($c)$c.artifact.rows[0].evidence.allocationSequenceBeforeDamage=1}
Reject-Envelope 'a window opened in another frame than the damage baseline' {param($c)$c.artifact.rows[0].evidence.beforeDamage.frame=11}
Reject-Envelope 'a life row with another subject' {param($c)$c.artifact.rows[0].evidence.subject='mount'}
Reject-Envelope 'a failed life row' {param($c)$c.artifact.rows[0].status='FAIL'}
Reject-Envelope 'no life row' {param($c)$c.artifact.rows=@($c.artifact.rows[1])}
Reject-Envelope 'a dropped native trace' {param($c)$c.artifact.rows[0].evidence.allocationTrace.dropped=1}
Reject-Envelope 'a foreign request scenario' {param($c)$c.request.scenario='chunk4-mount-death-tb'}
Reject-Envelope 'a failed forced-detach row' {param($c)$c.artifact.rows[1].status='FAIL'}

# 4. Registries and contracts.
if(@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq'CM05-forced-detach'}).Count-ne1){throw 'Shared runtime row registry lacks CM05-forced-detach exactly once'};$checks++
foreach($scenario in @('chunk4-rider-death-tb','chunk4-mount-death-tb','chunk4-rider-incapacitation-tb')){
 $leaf=@(Get-KmcChunk4CoreLeaves $scenario)
 if((@(Get-KmcChunk4CoreRequiredRows $scenario 32)-join'|')-cne(($leaf+@('CM05-forced-detach'))-join'|')){throw ('Schema 32 required rows differ for '+$scenario)}
 if((@(Get-KmcChunk4CoreRequiredRows $scenario 22)-join'|')-cne($leaf-join'|')){throw ('Schema 22 required rows differ for '+$scenario)}
 if((Get-KmcChunk4CoreSchemaVersion $scenario '0.1.0-chunk6a-preview.145')-ne22-or(Get-KmcChunk4CoreSchemaVersion $scenario '0.1.0-chunk6a-preview.146')-ne32-or(Get-KmcChunk4CoreSchemaVersion $scenario $null)-ne32){throw ('Native life schema identity differs for '+$scenario)}
 $checks++
}
if((Get-KmcChunk4CoreSchemaVersion 'chunk4-targeting-rider-rt' '0.1.0-chunk6a-preview.146')-ne22-or(@(Get-KmcChunk4CoreRequiredRows 'chunk4-targeting-rider-rt' 32)-join'|')-cne(@(Get-KmcChunk4CoreLeaves 'chunk4-targeting-rider-rt')-join'|')){throw 'Non-life core scenarios changed schema or rows'};$checks++
$contracts=@(Get-KmcChunk6aPrimaryContracts|Where-Object id -CEQ 'CM05-forced-detach')
if($contracts.Count-ne3-or(@($contracts|ForEach-Object scenario|Sort-Object)-join'|')-cne'chunk4-mount-death-tb|chunk4-rider-death-tb|chunk4-rider-incapacitation-tb'-or
   @($contracts|Where-Object {($_.rows-join'|')-cne'CM05-forced-detach'-or$_.evidenceLeaf-cne'phase3d-horse-scenario-evidence.json'}).Count-ne0){throw 'Forced detach primary contracts differ'};$checks++
$roles=@(Get-KmcChunk6aAdditionalRoles 'CM05-forced-detach')
if($roles.Count-ne3-or(@($roles|ForEach-Object {$_.name+'='+$_.scenario})-join'|')-cne'rider-death=chunk4-rider-death-tb|mount-death=chunk4-mount-death-tb|rider-incapacitation=chunk4-rider-incapacitation-tb'-or
   @($roles|Where-Object {($_.rows-join'|')-cne'CM05-forced-detach'}).Count-ne0){throw 'Forced detach additional roles differ'};$checks++
# The schema registration and dispatch moved from RuntimeHarness.Common.ps1 into ScenarioDispatchEvidence.ps1 (dot-sourced by Common,
# same reviewed reader identity) and the registered schema list now continues past 32L; the pin is read from the dispatch module.
$harness=(Get-Content -Raw (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1'))+(Get-Content -Raw (Join-Path $repo 'scripts/runtime/ScenarioDispatchEvidence.ps1'))
if(-not$harness.Contains("(`$phase3dSchemaVersion -eq 32L -and [string]`$Request.scenario -cnotin @('chunk4-rider-incapacitation-tb','chunk4-rider-death-tb','chunk4-mount-death-tb'))")-or-not$harness.Contains('31L, 32L, 33L')){throw 'Harness does not pin schema 32 to the native life scenarios'};$checks++
$supporting=Get-Content -Raw (Join-Path $repo 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
if(-not$supporting.Contains("if(`$Id-cne'CM05-forced-detach'-or`$artifact.schemaVersion-ne32){throw")-or-not$supporting.Contains("Assert-KmcChunk4CoreEvidence `$request `$artifact 'PASS'")){throw 'Additional native life qualification does not use the core reader on schema 32'};$checks++

# 5. Source order: the window opens immediately before the labelled damage dispatch and closes
# as soon as the cleanup has settled, before any ordinary End input or successor wait.
$source=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Chunk4NativeLifeScenario.cs')
$dispatch=$source.IndexOf('chunk4LifeEvidence["damageDispatches"] = 1;')
$begin=$source.IndexOf('BeginChunk6aForcedDetachWindow();')
$damage=$source.IndexOf('var damage = Rulebook.Trigger(new RuleDealDamage(target, subject,')
if($dispatch-lt0-or$begin-le$dispatch-or$damage-le$begin){throw 'Forced detach window does not open immediately before the native damage dispatch'};$checks++
$settled=$source.IndexOf('"Native damage did not produce permanent rider death under the selected native policy."')
$finish=$source.IndexOf('FinishChunk6aForcedDetachWindow();')
$turnWait=$source.IndexOf('if (turn == null) return;',$finish)
$endInput=$source.IndexOf('TryEndPhase3gFixtureTurn(turn);',$finish)
if($settled-lt0-or$finish-le$settled-or$turnWait-le$finish-or$endInput-le$turnWait){throw 'Forced detach window does not close at the settled cleanup before the ordinary End input'};$checks++
if(([regex]::Matches($source,[regex]::Escape('BeginChunk6aForcedDetachWindow();')).Count-ne1)-or([regex]::Matches($source,[regex]::Escape('FinishChunk6aForcedDetachWindow();')).Count-ne1)){throw 'Forced detach window is opened or closed in more than one place'};$checks++
foreach($needle in @('NativeForcedDetachEvidence.AssertComplete(evidence);','AddRow(Chunk6aForcedDetachRow, failure == null,','observations["chunk6aForcedDetach"] = evidence;','["lifecycleSequence"] = deliveries.Count == 0 ? 0L : deliveries[deliveries.Count - 1].Sequence,','["ledger"] = Chunk6aLedgerCounters(),','["activationSequence"] = LastChunk6aActivationSequence(nativeControls.SnapshotAbilityActivations()),','["activationRecords"] = new JArray(nativeControls.SnapshotAbilityActivations()','.Select(ProjectChunk6aActivationRecord))')){
 if(-not$source.Contains($needle)){throw ('Forced detach scenario lacks: '+$needle)};$checks++
}
$tranche=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs')
# The schema chain gained the 6B/6C/6D/6E families in front of the native life pin; the pin itself is unchanged.
if(-not$tranche.Contains('IsUnmountedAttackControls ? 31 : IsChunk4NativeLife ? 32 : IsChunk6aCombatMount ? 30 :')){throw 'Native life scenarios do not publish schema 32'};$checks++
$controls=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs')
if(-not$controls.Contains('internal IReadOnlyList<NativeLifecycleDeliveryRecord> SnapshotLifecycleDeliveries()')){throw 'Lifecycle delivery snapshot accessor missing'};$checks++
'NATIVE FORCED DETACH PASS='+$checks+' FAIL=0; synthetic producer/external/envelope only'
