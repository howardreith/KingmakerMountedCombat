# F2 reader: synthetic positive and targeted negative envelopes. No game, no build. A reader-only assertion is never native evidence.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-F2($Value){ $Value | ConvertTo-Json -Depth 40 | ConvertFrom-Json }
$rider='11111111-1111-1111-1111-111111111111'; $mount='22222222-2222-2222-2222-222222222222'; $hostile='33333333-3333-3333-3333-333333333333'
$id=Get-KmcFinalize2Identities
$g0='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'; $g1='bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'; $g3='cccccccccccccccccccccccccccccccc'
$cycle=2; $k2=4
function New-F2State([hashtable]$Change=@{}){
 $s=[ordered]@{frame=10;relationship='Mounted';generation=3;riderInCombat=$false;mounted=$true;riderStandard=0.0;riderMove=0.0;riderSwift=0.0;mountStandard=0.0;mountMove=0.0;mountSwift=0.0;
  chargeOwned=$false;activeCommand=$false;riderCharging=$false;mountCharging=$false;buff=$false;buffCount=0;riderAc=20;mountAc=23;distance=12.0;feedback='idle';admitted=0;refused=0;
  controlIndices=@($cycle);selectedAbility=$null;counters=[ordered]@{initialPlacement=1;legacySlotRepair=0}}
 foreach($k in $Change.Keys){ $s[$k]=$Change[$k] }
 $s
}
function New-F2Click([string]$SlotGuid,[string]$HandlerAfter,$Before,[int]$Index,[hashtable]$Extra=@{}){
 $c=[ordered]@{index=$Index;slotFound=$true;slotAbilityBefore=$SlotGuid;slotAbilityAfter=$SlotGuid;slotDataId=7;possible=$true;slotDataRestored=$true;slotStillBound=$true;
  handlerBefore=$null;handlerAfter=$HandlerAfter;handlerCaster=$rider;castDelta=0;refusalDelta=0;commands=@();before=$Before;afterClick=$Before}
 foreach($k in $Extra.Keys){ $c[$k]=$Extra[$k] }
 $c
}
function New-F2Bar([hashtable]$Keys){
 $k=@(); $t=@()
 for($i=0;$i -lt 140;$i++){ if($Keys.ContainsKey($i)){ $k+=$Keys[$i]; $t+='MechanicActionBarSlotAbility' } else { $k+=''; $t+='MechanicActionBarSlotEmpty' } }
 [ordered]@{keys=$k;types=$t}
}
function New-F2Main([hashtable]$Keys){ $k=@(); for($i=0;$i -lt 14;$i++){ $k+=$(if($Keys.ContainsKey($i)){$Keys[$i]}else{''}) }; ,$k }
function New-F2Ui([string]$Key,[int]$Index){
 $bar=@(); for($i=0;$i -lt 14;$i++){ $bar+=[ordered]@{active=$true;key=$(if($i -eq $Index){$Key}else{''});settingsIndex=$i} }
 [ordered]@{manager=$true;managerActive=$true;selectedIsRider=$true;mainBar=$bar}
}
function New-F2Artifact {
 $unmounted=New-F2State @{relationship='Unmounted';mounted=$false}
 $mounted=New-F2State
 $combat=New-F2State @{riderInCombat=$true}
 $placement=[ordered]@{case='F2-placement';steps=[ordered]@{};autofill=$true;
  placements=@([ordered]@{unitId=$rider;index=$cycle;frame=5;mainBarBefore=(New-F2Main @{0=$g0;1=$g1;3=$g3})});
  controlIndices=@($cycle);mainBarNow=(New-F2Main @{0=$g0;1=$g1;2=$id.publicControl;3=$g3});bar=(New-F2Bar @{0=$g0;1=$g1;2=$id.publicControl;3=$g3});
  factPresent=$true;factActive=$true;slotHoldsLiveFact=$true;placementRecorded=$true;placementSettled=$true;countersAtRow=[ordered]@{initialPlacement=1};waitFrames=0}
 $keys=@(
  [ordered]@{key='KMC.Native.Dismount.Name';expected='Dismount';present=$true;actual='Dismount';text='Dismount'},
  [ordered]@{key='KMC.Native.Empty';expected='';present=$true;actual='';text=''},
  [ordered]@{key='KMC.Native.Mount.Name';expected='Mount Companion';present=$true;actual='Mount Companion';text='Mount Companion'},
  [ordered]@{key='KMC.Native.MountedCharge.Description';expected='Charge an enemy.';present=$true;actual='Charge an enemy.';text='Charge an enemy.'},
  [ordered]@{key='KMC.Native.MountedCharge.Name';expected='Charge';present=$true;actual='Charge';text='Charge'},
  [ordered]@{key='KMC.Native.MountState.Description';expected='Mount or dismount.';present=$true;actual='Mount or dismount.';text='Mount or dismount.'},
  [ordered]@{key='KMC.Native.MountState.Name';expected='Mount / Dismount';present=$true;actual='Mount / Dismount';text='Mount / Dismount'})
 $controls=@(
  [ordered]@{kind='MountState';guid=$id.publicControl;name='Mount / Dismount';description='Mount or dismount.';hidden=$false},
  [ordered]@{kind='MountCompanion';guid=$id.mountShell;name='Mount Companion';description='Select your mount.';hidden=$true},
  [ordered]@{kind='Dismount';guid=$id.dismountShell;name='Dismount';description='Get off.';hidden=$true},
  [ordered]@{kind='MountedCharge';guid=$id.mountedChargeShell;name='Charge';description='Charge an enemy.';hidden=$true},
  [ordered]@{kind='RiderPrimary';guid='27364df661b3c121eabb97a31aa73a83';name='Rider Primary';description='Retired.';hidden=$false},
  [ordered]@{kind='MountPrimary';guid='f88a50d6fdbebbd709c3e323d2f52f5e';name='Mount Primary';description='Retired.';hidden=$false})
 $strings=[ordered]@{case='F2-strings';steps=[ordered]@{};tableFound=$true;packPresent=$true;locale='enGB';keys=$keys;controls=$controls}
 $watch=[ordered]@{index=$cycle;frames=240;absentFrames=0;factReplacedFrames=0;countNotOneFrames=0;slotDataNotFactFrames=0;indexMovedFrames=0;anomalies=@()}
 $cycleSteps=[ordered]@{
  mount1Click=(New-F2Click $id.publicControl $id.mountShell $unmounted $cycle @{handlerIsShell=$true});
  mount1Target=[ordered]@{clickedId=$mount;handlerAbility=$id.mountShell;clicked=$true};
  mount1Command=[ordered]@{found=$true;blueprint=$id.mountShell;targetId=$mount;fallback=$false};
  mounted1=[ordered]@{state=$mounted;shellBlueprint=$id.mountShell;shellFinished=$true;shellResult='Success'};
  dismountClick=(New-F2Click $id.publicControl $null $mounted $cycle @{handlerIsShell=$false});
  dismountCommand=[ordered]@{found=$true;blueprint=$id.dismountShell;targetId=$rider;fallback=$false};
  dismounted=[ordered]@{state=$unmounted;shellBlueprint=$id.dismountShell;shellFinished=$true;shellResult='Success'};
  mount2Click=(New-F2Click $id.publicControl $id.mountShell $unmounted $cycle @{handlerIsShell=$true});
  mount2Target=[ordered]@{clickedId=$mount;handlerAbility=$id.mountShell;clicked=$true};
  mount2Command=[ordered]@{found=$true;blueprint=$id.mountShell;targetId=$mount;fallback=$false};
  mounted2=[ordered]@{state=$mounted;shellBlueprint=$id.mountShell;shellFinished=$true;shellResult='Success'}}
 $cycleRow=[ordered]@{case='F2-mount-cycle';steps=$cycleSteps;start=[ordered]@{indices=@($cycle);index=$cycle;factId=11;dataId=12};fallbackUsed=$false;controlPlacedBySetup=$false;frameWatch=$watch}
 $removed=New-F2Bar @{0=$g0;1=$g1;3=$g3}
 $alias=[ordered]@{case='F2-alias-and-removal';steps=[ordered]@{};
  removal=[ordered]@{index=$cycle;indicesBefore=@($cycle);countersBefore=[ordered]@{initialPlacement=1;legacySlotRepair=0};barBefore=(New-F2Bar @{0=$g0;1=$g1;2=$id.publicControl;3=$g3});barAfterRemoval=$removed;frame=40};
  after60=[ordered]@{frames=60;indices=@();counters=[ordered]@{initialPlacement=1;legacySlotRepair=0}};
  removalDismountClicked=$true;removalDismounted=$unmounted;removalMountClicked=$true;removalMounted=$mounted;
  removalResult=[ordered]@{indices=@();counters=[ordered]@{initialPlacement=1;legacySlotRepair=0};placementRecorded=$true;bar=$removed};
  injection=[ordered]@{k1=$cycle;k2=$k2;emptyFound=@($cycle,$k2);outsideMainBar=$false;k1Guid=$id.dismountShell;k1Active=$true;k2Guid=$id.mountedChargeShell;k2Active=$true;
   stockChargePresent=$true;stockChargeOnBarBefore=$false;barBefore=$removed;countersBefore=[ordered]@{initialPlacement=1;legacySlotRepair=0};frame=50};
  repaired=[ordered]@{frames=10;bar=(New-F2Bar @{0=$g0;1=$g1;2=$id.publicControl;3=$g3;4=$id.stockCharge});k1Key=$id.publicControl;k2Key=$id.stockCharge;k1IsLiveControl=$true;k2IsLiveStock=$true;
   controlIndices=@($cycle);countersAfter=[ordered]@{initialPlacement=1;legacySlotRepair=2}}}
 $chargeBefore=New-F2State @{riderInCombat=$true;distance=12.0}
 $spent=New-F2State @{riderInCombat=$true;riderStandard=5.0;buff=$true;buffCount=1;riderAc=18;distance=3.9}
 $terminal=[ordered]@{state=$spent;sequence=4;sequenceLawful=$true;result='Success';revalidationFailed=$false;revalidationReason=$null;outcomeRejection='';rules=1;mountRules=0;closestDistance=4.0;
  peakSpeedOverride=10.16;mountCombatSpeed=5.08;riderWeaponRange=1.5;mountCorpulence=1.5;targetCorpulence=1.0;
  lease=[ordered]@{applied=$true;buffApplied=$true;forcedPathOutstanding=$false;riderAgentTouched=$false;restored=$true;chargingRestoredExactly=$true;speedOverrideRestoredExactly=$true;riderChargingRestoredExactly=$true;buffHandedToNative=$true;buffOutstanding=$false}}
 $chargeSteps=[ordered]@{
  tooNear=[ordered]@{available=$true;canTarget=$false;reason=$null;policyAllowed=$false;policyReason='internal';policyCode='OutsideSupportedRange';distance=3.0};
  selectionClick=(New-F2Click $id.stockCharge $id.mountedChargeShell $chargeBefore $k2);
  afterSelectionDrop=$chargeBefore;
  chargeProbe=[ordered]@{available=$true;canTarget=$true;reason=$null;policyAllowed=$true;distance=12.0};
  chargeClick=(New-F2Click $id.stockCharge $id.mountedChargeShell $chargeBefore $k2);
  chargeTarget=[ordered]@{clickedId=$hostile;handlerAbility=$id.mountedChargeShell;clicked=$true;castDelta=1;admittedDelta=1;shell=$true;shellTarget=$hostile}}
 $charge=[ordered]@{case='F2-charge-mounted';steps=$chargeSteps;chargeSlot=[ordered]@{index=$k2;source='repaired-old-binding';preexistingAtScenarioStart=$false;indicesBefore=@($k2);sameData=$true};
  combatEntered=$combat;terminal=$terminal;chargeRules=1;stockChargeCommandFrames=0;stockChargeWatchedFrames=180;
  spentRefusal=[ordered]@{riderStandard=5.5;available=$false;reason='Charge requires the rider''s standard action.'}}
 $refBefore=New-F2State @{riderInCombat=$true;riderStandard=1.5;riderMove=0.5}
 $refusal=[ordered]@{case='F2-refusals';steps=[ordered]@{};standardFreeAtStart=$true;
  publicCommand=[ordered]@{before=$refBefore;after=$refBefore;refusedDelta=1;started=$false;finished=$false;inCommands=$false};
  stockCommand=[ordered]@{before=$refBefore;after=$refBefore;feedbackBefore='x';feedbackAfter=$id.expectedFeedback;warningDelta=1;admittedDelta=0;started=$false;finished=$false;inCommands=$false};
  settled=$refBefore;warnings=@([ordered]@{text=$id.expectedFeedback;frame=900;addToLog=$true})}
 $unBefore=New-F2State @{riderInCombat=$true;relationship='Unmounted';mounted=$false}
 $unSteps=[ordered]@{
  dismountClick=(New-F2Click $id.publicControl $null (New-F2State @{riderInCombat=$true}) $cycle @{handlerIsShell=$false;shellCommand=$id.dismountShell});
  dismounted=[ordered]@{state=$unBefore;shellBlueprint=$id.dismountShell;shellFinished=$true;shellResult='Success';peakRiderMove=3.0};
  stockProbe=[ordered]@{available=$true;canTarget=$true;reason=$null};
  stockClick=(New-F2Click $id.stockCharge $id.stockCharge $unBefore $k2 @{handlerIsStock=$true;handlerIsShell=$false;handlerIsSlotData=$true});
  stockTarget=[ordered]@{clickedId=$hostile;handlerAbility=$id.stockCharge;clicked=$true;admittedDelta=0;commandFound=$true;commandBlueprint=$id.stockCharge;commandTarget=$hostile}}
 $unmountedRow=[ordered]@{case='F2-charge-unmounted';steps=$unSteps;dismountAttempts=1;placement=[ordered]@{radius=10.0};admittedBefore=1;
  buffSeen=(New-F2State @{riderInCombat=$true;relationship='Unmounted';buff=$true});chargeRule=[ordered]@{attacker=$rider;target=$hostile;charge=$true;d20=12};
  stockResult=[ordered]@{finished=$true;result='Success';admittedDelta=0;chargeRules=1}}
 $rows=@()
 foreach($pair in @(@('F2-placement',$placement),@('F2-strings',$strings),@('F2-mount-cycle',$cycleRow),@('F2-alias-and-removal',$alias),@('F2-charge-mounted',$charge),@('F2-refusals',$refusal),@('F2-charge-unmounted',$unmountedRow))){
  $rows+=[ordered]@{name=$pair[0];status='PASS';detail='recorded';evidence=$pair[1]}
 }
 $shots=@(
  [ordered]@{name='public-control-unmounted';requestedFrame=1;frame=1;state=$unmounted;ui=(New-F2Ui $id.publicControl $cycle);file='movement-visuals/finalize-f2-public-control-unmounted.png';sha256=('1'*64);bytes=100},
  [ordered]@{name='public-control-mounted';requestedFrame=2;frame=2;state=$mounted;ui=(New-F2Ui $id.publicControl $cycle);file='movement-visuals/finalize-f2-public-control-mounted.png';sha256=('2'*64);bytes=100},
  [ordered]@{name='charge-target-selection-mounted';requestedFrame=3;frame=3;state=(New-F2State @{riderInCombat=$true;selectedAbility=$id.mountedChargeShell});ui=(New-F2Ui $id.stockCharge $k2);file='movement-visuals/finalize-f2-charge-target-selection-mounted.png';sha256=('3'*64);bytes=100})
 $setup=@(); foreach($n in 1..8){ $setup+=[ordered]@{case='F2-charge-mounted';frame=$n;action=('disclosed action '+$n)} }
 $measure=[ordered]@{contract='finalize-f2-canonical-controls-v1';mode='RT';rider=$rider;mount=$mount;mountBlueprint='dddddddddddddddddddddddddddddddd';cases=@(Get-KmcFinalize2Cases);
  identities=[ordered]@{publicControl=$id.publicControl;mountShell=$id.mountShell;dismountShell=$id.dismountShell;mountedChargeShell=$id.mountedChargeShell;stockCharge=$id.stockCharge;expectedFeedback=$id.expectedFeedback;mainBarSlots=14};
  defaultFeatureEnabled=$true;startRelationship='Unmounted';disclosedSetup=$setup;screenshots=$shots;
  attackRules=@([ordered]@{attacker=$rider;target=$hostile;charge=$true;d20=11},[ordered]@{attacker=$rider;target=$hostile;charge=$true;d20=12})}
 Copy-F2 ([ordered]@{schemaVersion=53;status='PASS';errors=@();subscenarioPassCount=7;subscenarioFailCount=0;rows=$rows;observations=[ordered]@{riderId=$rider;horseId=$mount;finalize2Controls=$measure}})
}
$request=[pscustomobject]@{scenario='finalize-f2-controls-rt'}
function Accept($Artifact,$Request=$request){ try{ $null=Assert-KmcFinalize2ControlsEvidence $Request $Artifact 'PASS'; $true }catch{ Write-Output ('    unexpected: '+$_.Exception.Message); $false } }
function Reject($Artifact,[string]$Name,$Request=$request){ try{ $null=Assert-KmcFinalize2ControlsEvidence $Request $Artifact 'PASS'; Check $false $Name }catch{ Check $true ($Name+' ('+$_.Exception.Message+')') } }
function Row($Artifact,[string]$Name){ @($Artifact.rows | Where-Object name -CEQ $Name)[0].evidence }
function Shot($Artifact,[string]$Name){ @($Artifact.observations.finalize2Controls.screenshots | Where-Object name -CEQ $Name)[0] }

$base=New-F2Artifact
Check (Accept $base) 'complete F2 evidence accepted'
# The Working fixture's full main bar: nothing placed at load, then the first placement into the slot the player freed.
function New-F2FullBar {
 $f=Copy-F2 $base; $p=Row $f 'F2-placement'
 $full=@(); for($i=0;$i -lt 14;$i++){ $full+=('g'+$i) }
 $before=@($full); $before[$cycle]=''; $now=@($full); $now[$cycle]=$id.publicControl
 $p.placements[0].mainBarBefore=$before; $p.mainBarNow=$now; $p | Add-Member -NotePropertyName freedSlot -NotePropertyValue $cycle -Force
 $p | Add-Member -NotePropertyName loadObservation -NotePropertyValue ([pscustomobject]@{placements=@([pscustomobject]@{index=-1;frame=3;mainBarBefore=$full});controlIndices=@();mainBarNow=$full;fullBarCount=1;placedCount=0}) -Force
 $f
}
Check (Accept (New-F2FullBar)) 'full bar at load, then the first placement into the freed slot, accepted'
$r=New-F2FullBar; (Row $r 'F2-placement').loadObservation.mainBarNow[5]='overwritten'; Reject $r 'a full bar changed at load refuses'
$r=New-F2FullBar; (Row $r 'F2-placement').loadObservation.placements[0].index=4; Reject $r 'a load placement over a full bar refuses'
$r=New-F2FullBar; (Row $r 'F2-placement').freedSlot=7; Reject $r 'a first placement outside the freed slot refuses'
# Registration
Check (Test-KmcPhase3dSchemaRegistration 53 'finalize-f2-controls-rt') 'finalize-f2-controls-rt registers schema 53'
Check (-not (Test-KmcPhase3dSchemaRegistration 52 'finalize-f2-controls-rt')) 'the Sprint 8 schema is refused for the F2 scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 53 'sprint8-paladin-tb')) 'the F2 schema is refused for a Sprint 8 scenario'
Check (Test-KmcPhase3dSchemaRegistration 52 'sprint8-paladin-tb') 'the Sprint 8 registration is unchanged'
Check ((Get-KmcPhase3dEvidenceDispatch 53 'finalize-f2-controls-rt').validator -ceq 'Assert-KmcFinalize2ControlsEvidence') 'schema 53 dispatches to the F2 reader'
Check ((Get-KmcPhase3dEvidenceDispatch 52 'sprint8-paladin-tb').validator -ceq 'Assert-KmcSprint8PaladinEvidence') 'the Sprint 8 dispatch is unchanged'
Check (Test-KmcCompoundRuntimeScenario 'finalize-f2-controls-rt') 'the F2 scenario reports case rows'
Check (-not (Test-KmcChildEntryExpectsMounted 'finalize-f2-controls-rt')) 'the F2 child starts from its unmounted fixture handoff'
Check (Test-KmcChildEntryRequiresIdleParty 'finalize-f2-controls-rt') 'the F2 child requires an idle party'
Check ('finalize-f2-controls-rt' -cin (Get-KmcSaveBackedRuntimeScenarios)) 'the F2 scenario is a save-backed runtime scenario'
$knownRows=@(Get-KmcPhase3dHorseRuntimeRows)
Check (@(@(Get-KmcFinalize2Cases) + 'finalize-f2-controls-rt' | Where-Object { $_ -cnotin $knownRows }).Count -eq 0) 'the F2 rows and scenario are known subscenarios'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$protocol=[IO.File]::ReadAllText((Join-Path $repo 'src\KingmakerMountedCombat\Diagnostics\RuntimeProtocol.cs'))
Check ($protocol.Contains('!string.Equals(Branch, "codex/kmc-finalize-f2-canonical-controls", StringComparison.Ordinal)') -and $protocol.Contains('!string.Equals(Branch, "codex/kmc-finalize-f1-creation-ui-icons", StringComparison.Ordinal)') -and $protocol.Contains('Branch.StartsWith("codex/mounted-combat-", StringComparison.Ordinal)')) 'the compiled request admits the exact F2 branch beside F1 and the KMC prefix'
Check (([regex]::Matches($protocol,'"finalize-f2-controls-rt"')).Count -eq 2 -and @(Get-KmcFinalize2Cases | Where-Object { -not $protocol.Contains('"'+$_+'"') }).Count -eq 0) 'the compiled protocol registers the scenario and its rows'
$requestValidator=[IO.File]::ReadAllText((Join-Path $repo 'scripts\runtime\Test-RuntimeRequest.ps1'))
Check ($requestValidator.Contains("[string]`$request.branch -cne 'codex/kmc-finalize-f2-canonical-controls' -and") -and $requestValidator.Contains("'finalize-f2-controls-rt'")) 'the external request validator admits the exact F2 branch and scenario'
# Envelope
$r=Copy-F2 $base; $r.schemaVersion=52; Reject $r 'a schema-52 artifact refuses'
Reject $base 'another scenario refuses' ([pscustomobject]@{scenario='sprint8-paladin-tb'})
$r=Copy-F2 $base; $r.status='FAIL'; $r.subscenarioFailCount=1; $r.errors=@('F2-strings: failed'); try { Assert-KmcFinalize2ControlsEvidence $request $r 'FAIL'; Check $true 'an explicit failed prefix is preserved without qualification' } catch { Check $false 'an explicit failed prefix is preserved without qualification' }
$r=Copy-F2 $base; $r.status='FAIL'; $r.subscenarioFailCount=1; Reject $r 'a failed artifact cannot be reported PASS'
$r=Copy-F2 $base; $r.rows=@($r.rows | Where-Object name -CNE 'F2-refusals'); Reject $r 'a missing row refuses'
$r=Copy-F2 $base; $r.rows=@($r.rows)+@($r.rows[0]); Reject $r 'a duplicated row refuses'
$r=Copy-F2 $base; $r.observations.finalize2Controls.defaultFeatureEnabled=$false; Reject $r 'a row not under the ordinary default refuses'
$r=Copy-F2 $base; $r.observations.finalize2Controls.identities.publicControl=$id.mountShell; Reject $r 'a changed public identity refuses'
$r=Copy-F2 $base; $r.observations.finalize2Controls.disclosedSetup=@(); Reject $r 'undisclosed setup refuses'
$r=Copy-F2 $base; $r.observations.finalize2Controls.rider=$mount; Reject $r 'a measurement naming another pair refuses'
# Placement
$r=Copy-F2 $base; (Row $r 'F2-placement').autofill=$false; Reject $r 'autofill off refuses honestly'
$r=Copy-F2 $base; (Row $r 'F2-placement').placements[0].mainBarBefore[2]=$g3; Reject $r 'a placement over an occupied slot refuses'
$r=Copy-F2 $base; (Row $r 'F2-placement').placements[0].mainBarBefore[1]=''; (Row $r 'F2-placement').mainBarNow[1]=''; Reject $r 'a placement after an earlier empty slot refuses'
$r=Copy-F2 $base; (Row $r 'F2-placement').mainBarNow[3]=$g0; Reject $r 'a changed unrelated main-bar slot refuses'
$r=Copy-F2 $base; (Row $r 'F2-placement').placements=@((Row $r 'F2-placement').placements[0],(Row $r 'F2-placement').placements[0]); Reject $r 'two placements for one actor refuse'
$r=Copy-F2 $base; (Row $r 'F2-placement').placements=@(); Reject $r 'a migrated control with no placement record refuses'
$r=Copy-F2 $base; (Row $r 'F2-placement').controlIndices=@($cycle,7); Reject $r 'two public controls on the bar refuse'
$r=Copy-F2 $base; (Row $r 'F2-placement').placements[0].index=7; Reject $r 'a placement record for another slot refuses'
$r=Copy-F2 $base; (Row $r 'F2-placement').placementRecorded=$false; Reject $r 'an unrecorded placement refuses'
# Strings
$r=Copy-F2 $base; (Row $r 'F2-strings').controls[0].name='[unknown key: KMC.Native.MountState.Name]'; Reject $r 'an unresolved public control name refuses'
$r=Copy-F2 $base; (Row $r 'F2-strings').controls[3].name='Mounted Charge'; Reject $r 'the shell still named Mounted Charge refuses'
$r=Copy-F2 $base; (Row $r 'F2-strings').keys[6].actual='Mount'; Reject $r 'a key resolving to another text refuses'
$r=Copy-F2 $base; (Row $r 'F2-strings').keys[3].present=$false; Reject $r 'a key absent from the pack refuses'
$r=Copy-F2 $base; (Row $r 'F2-strings').keys=@((Row $r 'F2-strings').keys | Where-Object key -CNE 'KMC.Native.MountState.Name'); Reject $r 'a missing public control key refuses'
$r=Copy-F2 $base; (Row $r 'F2-strings').controls[1].hidden=$false; Reject $r 'a visible Mount shell refuses'
$r=Copy-F2 $base; (Row $r 'F2-strings').controls[3].description='[unknown key: KMC.Native.MountedCharge.Description]'; Reject $r 'an unresolved description refuses'
# Mount cycle
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.mount1Click.handlerAfter=$id.publicControl; Reject $r 'a slot click that did not select the Mount shell refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.mount2Click.slotDataRestored=$false; Reject $r 'a slot left on the delivery shell refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.dismountClick.handlerIsShell=$true; Reject $r 'a Dismount that waits for a target refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.dismountCommand.found=$false; Reject $r 'a slot click with no Dismount command refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').fallbackUsed=$true; Reject $r 'a hidden-shell fallback refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').frameWatch.absentFrames=1; Reject $r 'a frame without the public control refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').frameWatch.factReplacedFrames=2; Reject $r 'a regranted public fact refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').frameWatch.indexMovedFrames=1; Reject $r 'a moved public control refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.mounted1.state.riderMove=3.0; Reject $r 'an exploration Mount that spent Move refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.dismounted.shellResult='Interrupt'; Reject $r 'an interrupted Dismount refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.mounted2.state.relationship='Unmounted'; Reject $r 'a second Mount that did not mount refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').steps.mount1Target.clickedId=$hostile; Reject $r 'a Mount completed on another unit refuses'
# Removal and old bindings
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').after60.indices=@(9); Reject $r 'a removed control put back refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').removalResult.counters.initialPlacement=2; Reject $r 'a second placement after removal refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').after60.frames=30; Reject $r 'a removal held under sixty frames refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').repaired.countersAfter.legacySlotRepair=1; Reject $r 'a repair count other than two refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').repaired.k1Key=$id.dismountShell; Reject $r 'an old Dismount slot left in place refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').repaired.k2IsLiveStock=$false; Reject $r 'a Charge slot bound to a stale AbilityData refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').repaired.bar.keys[0]=''; Reject $r 'an unrelated slot cleared by the repair refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').repaired.bar.types[100]='MechanicActionBarSlotItem'; Reject $r 'an unrelated slot type changed by the repair refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').injection.stockChargeOnBarBefore=$true; Reject $r 'a second Charge beside an existing one refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').injection.outsideMainBar=$true; Reject $r 'old bindings off the main bar refuse'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').injection.k2Guid=$id.stockCharge; Reject $r 'an injected binding that is not the old shell refuses'
$r=Copy-F2 $base; (Row $r 'F2-alias-and-removal').removal.barAfterRemoval.keys[2]=$id.publicControl; Reject $r 'a removal that left the control refuses'
# Mounted Charge
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').steps.tooNear.reason='Mounted Charge requires a target beyond its minimum range.'; Reject $r 'a too-near reason naming Mounted Charge refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').steps.tooNear.reason='KMC.Native.MountedCharge.Name'; Reject $r 'a too-near reason showing a KMC key refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').steps.tooNear.canTarget=$true; Reject $r 'a targetable too-near hostile refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').steps.chargeClick.handlerAfter=$id.stockCharge; Reject $r 'a mounted Charge slot that selected the stock Charge refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').steps.selectionClick.slotAbilityAfter=$id.mountedChargeShell; Reject $r 'a Charge slot left on the shell refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').steps.chargeTarget.admittedDelta=2; Reject $r 'a mounted Charge admitted twice refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').steps.chargeProbe.available=$false; Reject $r 'an unavailable mounted ordinary Charge refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').stockChargeCommandFrames=1; Reject $r 'a stock Charge command on the mounted rider refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').terminal.lease.riderAgentTouched=$true; Reject $r 'a second mover refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').terminal.result='Interrupt'; Reject $r 'an undelivered mounted Charge refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').chargeRules=2; Reject $r 'two charge attacks refuse'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').spentRefusal.reason='Mounted Charge requires the rider''s standard action.'; Reject $r 'an internal Mounted Charge refusal on the ordinary Charge refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').spentRefusal.available=$true; Reject $r 'an ordinary Charge available with Standard spent refuses'
$r=Copy-F2 $base; (Row $r 'F2-mount-cycle').controlPlacedBySetup=$true; Reject $r 'a mount cycle on a setup-placed control refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').terminal.state.riderStandard=0.0; Reject $r 'a mounted Charge that spent no Standard refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-mounted').terminal.state.mountStandard=6.0; Reject $r 'a mount that paid the Charge refuses'
$r=Copy-F2 $base; $r.observations.finalize2Controls.attackRules=@(); Reject $r 'no observed charge attack rule refuses'
# Refusals
$r=Copy-F2 $base; (Row $r 'F2-refusals').publicCommand.refusedDelta=0; Reject $r 'an admitted public-control command refuses'
$r=Copy-F2 $base; (Row $r 'F2-refusals').publicCommand.after.riderMove=3.0; Reject $r 'a public-control command that spent Move refuses'
$r=Copy-F2 $base; (Row $r 'F2-refusals').publicCommand.after.relationship='Unmounted'; Reject $r 'a public-control command that changed the relationship refuses'
$r=Copy-F2 $base; (Row $r 'F2-refusals').stockCommand.feedbackAfter="While mounted, use Mounted Charge from the rider's abilities."; Reject $r 'the old stock Charge feedback refuses'
$r=Copy-F2 $base; (Row $r 'F2-refusals').stockCommand.admittedDelta=1; Reject $r 'a stock Charge command converted into the mounted Charge refuses'
$r=Copy-F2 $base; (Row $r 'F2-refusals').stockCommand.inCommands=$true; Reject $r 'a stock Charge command left on the rider refuses'
$r=Copy-F2 $base; (Row $r 'F2-refusals').stockCommand.after.riderStandard=6.0; Reject $r 'a refused stock Charge that spent Standard refuses'
$r=Copy-F2 $base; (Row $r 'F2-refusals').warnings=@(); Reject $r 'a silent cancellation refuses'
# Unmounted Charge
$r=Copy-F2 $base; (Row $r 'F2-charge-unmounted').steps.stockClick.handlerAfter=$id.mountedChargeShell; (Row $r 'F2-charge-unmounted').steps.stockClick.handlerIsShell=$true; Reject $r 'an unmounted Charge substituted with the shell refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-unmounted').steps.stockTarget.admittedDelta=1; Reject $r 'an unmounted Charge admitted as mounted refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-unmounted').buffSeen=$null; Reject $r 'no native charge buff refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-unmounted').chargeRule=$null; Reject $r 'no native charge attack refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-unmounted').steps.dismounted.peakRiderMove=0.0; Reject $r 'a free combat Dismount refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-unmounted').steps.dismountClick.shellCommand=$null; Reject $r 'a combat Dismount with no shell command refuses'
$r=Copy-F2 $base; (Row $r 'F2-charge-unmounted').steps.stockTarget.commandBlueprint=$id.mountedChargeShell; Reject $r 'a non-native unmounted command refuses'
# Screenshots
$r=Copy-F2 $base; $r.observations.finalize2Controls.screenshots=@($r.observations.finalize2Controls.screenshots[0],$r.observations.finalize2Controls.screenshots[1]); Reject $r 'a missing screenshot refuses'
$r=Copy-F2 $base; (Shot $r 'public-control-mounted') | Add-Member error 'IOException: denied'; Reject $r 'a failed screenshot refuses'
$r=Copy-F2 $base; (Shot $r 'public-control-unmounted').ui.selectedIsRider=$false; Reject $r 'a toolbar showing another unit refuses'
$r=Copy-F2 $base; (Shot $r 'public-control-mounted').ui.mainBar[$cycle].active=$false; Reject $r 'a hidden toolbar slot refuses'
$r=Copy-F2 $base; (Shot $r 'charge-target-selection-mounted').state.selectedAbility=$id.stockCharge; Reject $r 'a selection frame without the mounted Charge refuses'
$r=Copy-F2 $base; (Shot $r 'public-control-mounted').state.relationship='Unmounted'; Reject $r 'a mounted frame taken unmounted refuses'
# Screenshot files under an evidence root
$root=Join-Path ([IO.Path]::GetTempPath()) ('kmc-f2-reader-'+[guid]::NewGuid().ToString('N'))
try {
 New-Item -ItemType Directory -Path (Join-Path $root 'movement-visuals') -Force | Out-Null
 $withFiles=Copy-F2 $base
 foreach($shot in $withFiles.observations.finalize2Controls.screenshots){
  $path=Join-Path $root ($shot.file -replace '/','\'); [IO.File]::WriteAllBytes($path,[Text.Encoding]::ASCII.GetBytes('png:'+$shot.name))
  $shot.sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
 }
 $rooted=[pscustomobject]@{scenario='finalize-f2-controls-rt';evidenceRoot=$root}
 Check (Accept $withFiles $rooted) 'screenshots on disk with their recorded hashes accepted'
 $r=Copy-F2 $withFiles; (Shot $r 'public-control-unmounted').sha256=('4'*64); Reject $r 'a screenshot hash that differs from the file refuses' $rooted
 Remove-Item -LiteralPath (Join-Path $root 'movement-visuals\finalize-f2-public-control-mounted.png')
 Reject (Copy-F2 $withFiles) 'a screenshot file missing from the evidence root refuses' $rooted
} finally { if(Test-Path -LiteralPath $root){ Remove-Item -LiteralPath $root -Recurse -Force } }
Write-Output ('F2 readers: PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
