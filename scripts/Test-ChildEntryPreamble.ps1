param([ValidateSet('Debug','Release')][string]$Configuration='Release')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
# The structured parent-to-child handoff snapshot under one acceptance authority: the compiled
# producer (AssertStructure, through reflection) accepts every structurally valid snapshot
# without deciding which handoffs are lawful; the external reader decides (the recorded admission
# mode as observed, the zero-command/zero-cost handoff, the idle-party requirement); structural
# contradictions are refused by both; the tranche captures and structurally checks the snapshot at
# child entry; the harness requires it from preview.150. Synthetic objects only; no native qualification.
. (Join-Path $PSScriptRoot 'runtime/ChildEntryPreambleEvidence.ps1')
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$paths=[xml](Get-Content -Raw (Join-Path $repo 'LocalGamePaths.props'));$managed=Join-Path $paths.Project.PropertyGroup.KingmakerInstallDir 'Kingmaker_Data/Managed'
[Reflection.Assembly]::LoadFrom((Join-Path $managed 'Newtonsoft.Json.dll'))|Out-Null
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $repo ('bin/'+$Configuration+'/KingmakerMountedCombat.dll')))
$type=$assembly.GetType('KingmakerMountedCombat.Diagnostics.ChildEntryPreambleEvidence',$true)
$method=$type.GetMethod('AssertStructure',[Reflection.BindingFlags]'Static,NonPublic')
if($null-eq$method-or$method.GetParameters().Count-ne3){throw 'Producer structural check is missing or decides more than structure'}
if($null-ne$type.GetMethod('AssertComplete',[Reflection.BindingFlags]'Static,NonPublic')){throw 'The compiled producer still carries a behavioral acceptance method'}
$checks=0
function Copy-Preamble($x){$x|ConvertTo-Json -Depth 40|ConvertFrom-Json}
function New-Preamble([bool]$Mounted){
 $actor={param($id,$object)[pscustomobject]@{id=$id;object=$object;standard=0.0;move=0.0;swift=0.0;initiative=0.0;reactionCooldown=0.0;reactions=1;prepared=$false;inCombat=$false;canAct=$true;hasMove=$true;hasStandard=$true;commandRunning=$false;handsBusy=$false}}
 [pscustomobject]@{
  contract='structured-child-entry-preamble-snapshot';parentScenario='horse-companion-unmounted-suite';parentEngine='KingmakerMountedCombat.Diagnostics.HorseCompanionUnmountedScenarioEngine'
  childScenario=$(if($Mounted){'chunk4-rider-death-tb'}else{'chunk6a-mount-spent-move-tb'});childTranche='KingmakerMountedCombat.Diagnostics.Phase3dHorseScenarioTranche';runId='synthetic-run'
  sessionObject=4242;areaGuid='0123456789abcdef0123456789abcdef';frame=120;gameTicks=5000000;capturedAtUtc='2026-10-02T00:00:00.0000000Z'
  riderId='rider';riderObject=101;mountId='mount';mountObject=102;pairAlreadyMounted=$Mounted
  relationshipState=$(if($Mounted){'Mounted'}else{'Unmounted'});relationshipGeneration=3;relationshipRiderId=$(if($Mounted){'rider'}else{$null});relationshipMountId=$(if($Mounted){'mount'}else{$null})
  admissionMode=$(if($Mounted){'Exploration'}else{'<none>'});admissionModeExplicit=$Mounted
  commands=[pscustomobject]@{riderCommandsEmpty=$true;mountCommandsEmpty=$true;riderRelationshipCommands=0;mountRelationshipCommands=0;riderMoveSlot=$null}
  control=[pscustomobject]@{transitionInFlight=$false;riderOwnsUnsettledShell=$false;shellCount=2;processBindings=2;dispatchAccepted=2;dispatchRejected=0;transitionLedger='admittedMount=1;acceptedMount=1';acceptedMountCount=1;acceptedDismountCount=$(if($Mounted){0}else{1});forcedDetachCount=0;refusedVoluntaryCount=0}
  actors=[pscustomobject]@{rider=(& $actor 'rider' 101);mount=(& $actor 'mount' 102)}
  party=[pscustomobject]@{playerInCombat=$false;members=6;membersInCombat=0;idle=$true;memberIds=@('rider','mount','a','b','c','d')}
  turnBased=$false;currentTurnActor=$null;paused=$false
 }
}
# $Expected is the external verdict; the producer accepts unless the corruption is structural.
function Check($p,[string]$Scenario,[bool]$Mounted,[bool]$Idle,[bool]$Expected,[bool]$Structural=$false){
 $producerExpected=$Expected-or-not$Structural
 $producer=$true;$producerError=$null
 try{$invokeArgs=[object[]]::new(3);$invokeArgs[0]=[Newtonsoft.Json.Linq.JObject]::Parse(($p|ConvertTo-Json -Depth 40 -Compress));$invokeArgs[1]=$Scenario;$invokeArgs[2]=$Mounted;$null=$method.Invoke($null,$invokeArgs)}
 catch{$producer=$false;$producerError=$_.Exception.InnerException;if($producerExpected){throw ('producer: '+$(if($null-ne$producerError){$producerError.Message}else{$_.Exception.Message}))}}
 $external=$true;try{Assert-KmcChildEntryPreamble $p $Scenario $Mounted $Idle}catch{$external=$false;if($Expected){throw ('external reader: '+$_.Exception.Message)}}
 if($producer-ne$producerExpected-or$external-ne$Expected){throw ('Child entry preamble producer/external '+$producer+'/'+$external+' expected '+$producerExpected+'/'+$Expected+$(if($null-ne$producerError){'; producer='+$producerError.Message}else{''}))};$script:checks+=2
}
foreach($mounted in @($true,$false)){
 $p=New-Preamble $mounted;$scenario=$p.childScenario
 Check $p $scenario $mounted $true $true
 function Reject([scriptblock]$Mutate,[string]$Scenario=$scenario,[bool]$M=$mounted,[bool]$Idle=$true,[switch]$Structural){$x=Copy-Preamble $p;& $Mutate $x;try{Check $x $Scenario $M $Idle $false ([bool]$Structural)}catch{throw ('Mutation '+$Mutate.ToString()+': '+$_)}}
 # Structural contradictions: refused by the producer and the reader.
 Reject {param($x)$x.contract='wrong'} -Structural
 Reject {param($x)$x.parentScenario='other'} -Structural
 Reject {param($x)$x.parentEngine=''} -Structural
 Reject {param($x)$x.childScenario='other'} -Structural
 Reject {param($x)$x.runId=''} -Structural
 Reject {param($x)$x.sessionObject=0} -Structural
 Reject {param($x)$x.areaGuid=''} -Structural
 Reject {param($x)$x.frame=-1} -Structural
 Reject {param($x)$x.capturedAtUtc=''} -Structural
 Reject {param($x)$x.mountId='rider'} -Structural
 Reject {param($x)$x.mountObject=101} -Structural
 Reject {param($x)$x.pairAlreadyMounted=-not$x.pairAlreadyMounted} -Structural
 Reject {param($x)$x.relationshipState=$(if($mounted){'Unmounted'}else{'Mounted'})} -Structural
 Reject {param($x)$x.relationshipGeneration=-1} -Structural
 Reject {param($x)$x.admissionMode=''} -Structural
 Reject {param($x)$x.admissionModeExplicit=-not$x.admissionModeExplicit} -Structural
 Reject {param($x)$x.control.transitionLedger=''} -Structural
 Reject {param($x)$x.control.dispatchRejected=-1} -Structural
 Reject {param($x)$x.actors.rider.id='other'} -Structural
 Reject {param($x)$x.actors.mount.move=-1} -Structural
 Reject {param($x)$x.actors.mount.PSObject.Properties.Remove('prepared')} -Structural
 Reject {param($x)$x.party.members=1} -Structural
 Reject {param($x)$x.PSObject.Properties.Remove('turnBased')} -Structural
 Reject {param($x)} 'another-scenario' -Structural
 Reject {param($x)} $scenario (-not$mounted) -Structural
 # Behavioral decisions: the producer records them, the reader alone refuses them.
 Reject {param($x)$x.commands.riderCommandsEmpty=$false}
 Reject {param($x)$x.commands.mountRelationshipCommands=1}
 Reject {param($x)$x.control.transitionInFlight=$true}
 Reject {param($x)$x.control.riderOwnsUnsettledShell=$true}
 Reject {param($x)$x.actors.rider.commandRunning=$true}
 Reject {param($x)$x.party.playerInCombat=$true}
 Reject {param($x)$x.party.membersInCombat=1}
 Reject {param($x)$x.party.idle=$false}
 if($mounted){
  Reject {param($x)$x.admissionMode='<none>';$x.admissionModeExplicit=$false}
  Reject {param($x)$x.admissionMode='SavedRestore';$x.admissionModeExplicit=$true}
  # A mounted handoff through the parent's lawful voluntary combat Mount is validated as observed
  # (preview.150 c6a-action-economy150-a-ordinary-tb-600); no exploration Mount is repeated.
  $q=Copy-Preamble $p;$q.admissionMode='VoluntaryCombat';$q.admissionModeExplicit=$true;Check $q $scenario $true $true $true
 } else {
  Reject {param($x)$x.admissionMode='VoluntaryCombat';$x.admissionModeExplicit=$true}
  # An unmounted child may follow an exploration preamble that already dismounted.
  $q=Copy-Preamble $p;$q.admissionMode='Exploration';$q.admissionModeExplicit=$true;Check $q $scenario $false $true $true
 }
 # Party combat is contradictory only when the child requires the idle-party handoff.
 $busy=Copy-Preamble $p;$busy.party.playerInCombat=$true;$busy.party.membersInCombat=2;$busy.party.idle=$false
 Check $busy $scenario $mounted $false $true
}
# Scenario expectations and the version gate.
foreach($s in @('chunk6a-mount-spent-move-tb','chunk6a-combat-mount-rt','chunk6a-allocation-rider-first-tb')){if(Test-KmcChildEntryExpectsMounted $s){throw "Chunk 6A child $s must start unmounted"};$checks++}
foreach($s in @('chunk4-rider-death-tb','unmounted-attack-controls-rt','actor-allocation-rider-first-tb','ordinary-attack-controls-tb','phase3g-native-controls-tb')){if(-not(Test-KmcChildEntryExpectsMounted $s)){throw "child $s must start mounted"};$checks++}
foreach($s in @('chunk4-rider-death-tb','unmounted-attack-controls-rt','actor-allocation-rider-first-tb','chunk6a-rider-without-move-tb')){if(-not(Test-KmcChildEntryRequiresIdleParty $s)){throw "child $s requires the idle party"};$checks++}
foreach($s in @('ordinary-attack-controls-tb','phase3g-native-controls-tb','phase3d-horse-presentation-suite')){if(Test-KmcChildEntryRequiresIdleParty $s){throw "child $s does not require the idle party"};$checks++}
if(Test-KmcChildEntryPreambleRequired '0.1.0-chunk6a-preview.149'){throw 'preamble must not be required before preview.150'};$checks++
if(-not(Test-KmcChildEntryPreambleRequired '0.1.0-chunk6a-preview.150')){throw 'preamble must be required from preview.150'};$checks++
foreach($other in @('0.1.0-chunk4-preview.54','0.1.0-chunk5-preview.105','0.1.0-paired-preview.37','0.1.0-phase2b-dev.1','0.1.0-phase2a-review.2','0.0.1-feasibility','0.1.0-parser-only')){if(Test-KmcChildEntryPreambleRequired $other){throw ('only Chunk 6A candidates from preview.150 require the preamble: '+$other)};$checks++}
if(-not(Test-KmcChildEntryPreambleRequired '0.1.0-chunk6a-preview.151')){throw 'later Chunk 6A candidates require the preamble'};$checks++
# Tranche source pins: captured once at child entry, structurally checked before any scenario
# begins, with the exact admission mode and the zero-command/shell/process/dispatch facts.
$tranche=Get-Content -Raw (Join-Path $repo 'src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs')
foreach($pin in @(
 'observations\["childEntryPreamble"\] = childEntryPreamble;',
 'ChildEntryPreambleEvidence\.AssertStructure\(childEntryPreamble, request\.Scenario, pairAlreadyMounted\);',
 '\["admissionMode"\] = playerAction\.LastRelationshipDispatchAdmission \?\? "<none>"',
 '\["transitionInFlight"\] = nativeControls\.HasUnsettledRelationshipTransition',
 '\["riderOwnsUnsettledShell"\] = moveSlot != null && nativeControls\.OwnsUnsettledRelationshipShell\(moveSlot\)',
 '\["riderRelationshipCommands"\] = relationshipCommands\(rider\)',
 '\["membersInCombat"\] = members\.Count\(member => member\.IsInCombat\)')){
 if($tranche -notmatch $pin){throw ('Tranche preamble capture pin missing: '+$pin)};$checks++
}
if($tranche -match 'ChildEntryPreambleEvidence\.AssertComplete\('){throw 'The tranche still calls a behavioral preamble validator'};$checks++
$start=[regex]::Match($tranche,'(?s)internal void Start\(bool pairAlreadyMounted\)(.*?)if \(IsChunk6aCombatMount\) \{ BeginChunk6aCombatMount\(\); return; \}')
if(-not$start.Success-or$start.Value-notmatch 'CaptureChildEntryPreamble\(pairAlreadyMounted\)'){throw 'The preamble is not captured inside Start before any scenario begins'};$checks++
$harness=Get-Content -Raw (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
if($harness -notmatch "Assert-KmcChildEntryPreamble \`$preambleProperty\.Value \(\[string\]\`$Request\.scenario\) \(Test-KmcChildEntryExpectsMounted \(\[string\]\`$Request\.scenario\)\) \(Test-KmcChildEntryRequiresIdleParty \(\[string\]\`$Request\.scenario\)\)"){throw 'Harness does not validate the child entry preamble'};$checks++
if($harness -notmatch 'Test-KmcChildEntryPreambleRequired \(\[string\]\$artifact\.productVersion\)'){throw 'Harness does not gate the preamble by product version'};$checks++
Write-Output ('CHILD ENTRY PREAMBLE PRODUCER(STRUCTURE)+EXTERNAL(AUTHORITY) PASS='+$checks+' FAIL=0; synthetic snapshot only, no native qualification')
