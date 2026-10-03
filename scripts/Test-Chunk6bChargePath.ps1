[CmdletBinding()]
param()
# Synthetic acceptance/refusal regression for the Chunk 6B charge-path reader
# (scripts/runtime/Chunk6bChargePathEvidence.ps1). Never launches the game; no native qualification.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6bChargePathEvidence.ps1')
$script:checks=0
function Copy-Case($x){ $x|ConvertTo-Json -Depth 40|ConvertFrom-Json }
function New-Actor([string]$Id,[double]$Standard,[double]$Move){ [ordered]@{id=$Id;standard=$Standard;move=$Move;swift=0;position=@(0,0,0);commands=1;raw=@();queue=@();previous=$null;group=0} }
function New-Turn([double]$Moved,[double]$Forced,[double]$Stepped,[bool]$Step,[bool]$Acting){ [ordered]@{unit='rider';isRider=$true;status=$(if($Acting){'Acting'}else{'Preparing'});acting=$Acting;timeMoved=$Moved;timeMovedInForceMode=$Forced;timeMovedByFiveFootStep=$Stepped;metersMovedByFiveFootStep=0.73;enabledFiveFootStep=$Step;enabledSingleActionMove=$false} }
function New-Order([string]$Kind,[string]$Executor){ [ordered]@{kind=$Kind;input=[ordered]@{kind='ground';clicked=$true;cursorCycles=0;frame=100;fiveFootStep=$false;singleActionMove=$false;point=@(1,0,1);feedback='Mounted ground movement active: Horse owns movement.';rejectionCodes=@()};admitted=[ordered]@{type='UnitMoveTo';executor=$Executor;createdByPlayer=$true};executor=$Executor;admitted2=$null} }
function New-Row([string]$Case,[string]$Mode){
 $tb=$Mode-ceq'TB'
 $samples=@()
 for($i=0;$i-lt6;$i++){ $samples+=,([ordered]@{nativeSeconds=0.1*$i;frame=100+$i;distanceToTarget=9.0-1.4*$i;mountMoving=$true;forceMode=$true;speedMps=13.4;carrierRunning=$true;carrierStarted=$true;reforces=1;riderCommandsEmpty=$true;mountOnlyCarrier=$true;riderMove=0;riderStandard=0;mountMove=0;mountStandard=0;turnTimeMoved=$(if($tb){0}else{$null});turnTimeForced=$(if($tb){0}else{$null});pairMovement='mountMove=0->0;mountTime=0.0110121;mountStepMetres=0;nativePrepared=True;round=1';charging=$true;descriptorCharging=$false}) }
 $arrival=$Case-ceq'C6B-PATH-straight-arrival'
 # The carrier is the rider turn's first movement, so nothing had moved before the path.
 $turnBefore=if($tb){New-Turn 0 0 0 $false $false}else{$null}
 $turnAfter=if($tb){New-Turn 0 0 0 $false $true}else{$null}
 $carrier=New-Order 'delegated-ground-move' 'mount'
 $carrier['destination']=@(8,0,8);$carrier['admitted']=$true;$carrier['startedMoving']=$true;$carrier['interruptedByMeasurement']=$true;$carrier['terminal']=[ordered]@{finished=$true}
 $probe=New-Order 'residue-probe' 'mount'
 $probe['forceModeBefore']=$true;$probe['admitted']=$true;$probe['firstMove']=[ordered]@{frame=140;forceMode=$false;nativeSeconds=0.05};$probe['movedDistance']=1.4;$probe['timedOut']=$false;$probe['forceModeAfter']=$false
 [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{
  level='NATIVE MEASUREMENT';mode=$Mode;case=$Case;mounted=$true
  geometry=[ordered]@{distance3D=9.0;minimumRange=4.6;maximumRange=40.2336;mountCombatSpeedMps=6.7056;mountCorpulence=1.0;targetCorpulence=0.5;straightRoute=$true;landingBlocked=$false;customCanTarget=$true;mountAvoidanceDisabled=$false;riderAvoidanceDisabled=$true}
  carrier=$carrier
  lease=[ordered]@{chargingBefore=$false;speedOverrideBefore=$null;chargingApplied=$true;speedOverrideApplied=13.4112;forcePathApplied=$true;forcePaths=@([ordered]@{reason='initial';frame=100;forceModeAfterApply=$true});forceModeAfterApply=$true;riderAgentTouched=$false}
  before=[ordered]@{rider=(New-Actor 'rider' 0 0);mount=(New-Actor 'mount' 0 0);turn=$turnBefore}
  samples=$samples
  stop=[ordered]@{reason=$(if($arrival){'arrival'}else{'interrupt'});elapsedSeconds=$(if($arrival){0.6}else{0.2});settleSeconds=0.3;distanceToTarget=$(if($arrival){1.9}else{7.4});movedDistance=$(if($arrival){7.1}else{1.6});maximumLateralDeviation=0.12;peakSpeedMps=13.2;reforces=1}
  after=[ordered]@{forceMode=$true;mountMoving=$false;charging=$false;descriptorCharging=$false;speedOverride=$null;turn=$turnAfter}
  restoration=[ordered]@{chargingRestored=$true;speedOverrideRestored=$true;forceModeAfterStop=$true;carrierFinished=$true;settleSeconds=0.3;mountCommandsEmpty=$true}
  costs=[ordered]@{riderStandardDelta=0;riderMoveDelta=0;mountStandardDelta=0;mountMoveDelta=0}
  costsAfterProbe=[ordered]@{riderStandardDelta=0;riderMoveDelta=$(if($tb){0.4}else{0});mountStandardDelta=0;mountMoveDelta=0}
  residueProbe=$probe
  reach=[ordered]@{radius=2.0;arrivalWithinReach=$arrival;distanceNow=$(if($arrival){3.2}else{8.6})}
  riderCommandsEmptyThroughout=$true;mountOnlyCarrierThroughout=$true;chargingObservedThroughout=$true
  attackRules=0;attackRulesOpportunity=0;attackRulesNonOpportunity=0
  attackRuleEvidence=[ordered]@{riderAttackRules=0;mountAttackRules=0;pairNonOpportunityAttackRules=0;pairOpportunityAttackRules=0;firstPairActorId=$null;attackRuleEvents=@()} } }
}
function New-Artifact([string]$Mode){
 $scenario=if($Mode-ceq'TB'){'chunk6b-charge-path-tb'}else{'chunk6b-charge-path-rt'}
 Copy-Case ([ordered]@{schemaVersion=33;evidenceKind='phase3d-horse-scenario-evidence';scenario=$scenario;status='PASS';rows=@((New-Row 'C6B-PATH-straight-arrival' $Mode),(New-Row 'C6B-PATH-interrupt-stop' $Mode));observations=[ordered]@{chunk6bChargePath=[ordered]@{contract='chunk6b-pair-forced-path-measurement';mode=$Mode;cases=@('C6B-PATH-straight-arrival','C6B-PATH-interrupt-stop');carrier='delegated-ground-move'}};subscenarioPassCount=2;subscenarioFailCount=0;errors=@()})
}
function Accept([string]$Name,[scriptblock]$Body){ & $Body; $script:checks++; Write-Host ('PASS '+$Name) }
function Reject([string]$Name,[scriptblock]$Body){ $failed=$false; try { & $Body } catch { $failed=$true }; if(-not$failed){ throw ('Invalid evidence accepted: '+$Name) }; $script:checks++; Write-Host ('PASS refuses '+$Name) }
foreach($mode in @('RT','TB')){
 $scenario=if($mode-ceq'TB'){'chunk6b-charge-path-tb'}else{'chunk6b-charge-path-rt'}
 $request=[pscustomobject]@{scenario=$scenario}
 Accept ($mode+' lawful forced path under the admitted carrier, interrupt and residue probe') { Assert-KmcChunk6bChargePathEvidence $request (New-Artifact $mode) 'PASS' }
 # Several re-forces under the carrier's native approach are lawful when recorded exactly.
 $reforced=New-Artifact $mode; $reforced.rows[0].evidence.lease.forcePaths=@($reforced.rows[0].evidence.lease.forcePaths[0],[pscustomobject]@{reason='re-force';frame=110;forceModeAfterApply=$true},[pscustomobject]@{reason='re-force';frame=120;forceModeAfterApply=$true}); $reforced.rows[0].evidence.stop.reforces=3
 Accept ($mode+' re-forced path recorded exactly') { Assert-KmcChunk6bChargePathEvidence $request $reforced 'PASS' }
 # Force mode cleared already at the stop is equally lawful when the probe still runs.
 $cleared=New-Artifact $mode; $cleared.rows[0].evidence.after.forceMode=$false; $cleared.rows[0].evidence.restoration.forceModeAfterStop=$false; $cleared.rows[0].evidence.residueProbe.forceModeBefore=$false
 Accept ($mode+' force mode already clear at the stop') { Assert-KmcChunk6bChargePathEvidence $request $cleared 'PASS' }
 function Mutate([string]$Name,[scriptblock]$Change){ $a=New-Artifact $mode; & $Change $a; Reject ($mode+': '+$Name) { Assert-KmcChunk6bChargePathEvidence $request $a 'PASS' } }
 Mutate 'wrong schema' {param($a) $a.schemaVersion=32}
 Mutate 'other carrier contract' {param($a) $a.observations.chunk6bChargePath.carrier='direct-force-path'}
 Mutate 'other mode row' {param($a) $a.rows[0].evidence.mode=$(if($mode-ceq'TB'){'RT'}else{'TB'})}
 Mutate 'unmounted pair' {param($a) $a.rows[0].evidence.mounted=$false}
 Mutate 'bent route' {param($a) $a.rows[0].evidence.geometry.straightRoute=$false}
 Mutate 'blocked landing' {param($a) $a.rows[0].evidence.geometry.landingBlocked=$true}
 Mutate 'inside minimum range' {param($a) $a.rows[0].evidence.geometry.distance3D=3.0}
 Mutate 'beyond maximum range' {param($a) $a.rows[0].evidence.geometry.distance3D=50.0}
 Mutate 'maximum range not six times the speed' {param($a) $a.rows[0].evidence.geometry.maximumRange=30.0}
 Mutate 'carrier not admitted' {param($a) $a.rows[0].evidence.carrier.admitted=$false}
 Mutate 'carrier executed by the rider' {param($a) $a.rows[0].evidence.carrier.executor='rider'}
 Mutate 'carrier not a ground click' {param($a) $a.rows[0].evidence.carrier.input.kind='five-foot-step'}
 Mutate 'carrier never moved' {param($a) $a.rows[0].evidence.carrier.startedMoving=$false}
 Mutate 'carrier ended on its own' {param($a) $a.rows[0].evidence.carrier.interruptedByMeasurement=$false}
 Mutate 'no forced path applied' {param($a) $a.rows[0].evidence.lease.forcePathApplied=$false}
 Mutate 'no force mode after the first forced path' {param($a) $a.rows[0].evidence.lease.forceModeAfterApply=$false}
 Mutate 'forced-path count not recorded exactly' {param($a) $a.rows[0].evidence.stop.reforces=4}
 Mutate 'speed override below charge speed' {param($a) $a.rows[0].evidence.lease.speedOverrideApplied=7.0}
 Mutate 'rider agent touched' {param($a) $a.rows[0].evidence.lease.riderAgentTouched=$true}
 Mutate 'a command beside the carrier during the path' {param($a) $a.rows[0].evidence.samples[2].mountOnlyCarrier=$false}
 Mutate 'a rider command during the path' {param($a) $a.rows[0].evidence.samples[2].riderCommandsEmpty=$false}
 Mutate 'charging flag lost mid-path' {param($a) $a.rows[0].evidence.samples[3].charging=$false}
 Mutate 'rider standard charged' {param($a) $a.rows[0].evidence.costs.riderStandardDelta=3.0}
 Mutate 'a refunded rider move' {param($a) $a.rows[0].evidence.costs.riderMoveDelta=-0.5}
 Mutate 'mount move charged' {param($a) $a.rows[0].evidence.costs.mountMoveDelta=0.5}
 Mutate 'charging not restored' {param($a) $a.rows[0].evidence.restoration.chargingRestored=$false}
 Mutate 'speed override not restored' {param($a) $a.rows[0].evidence.restoration.speedOverrideRestored=$false}
 Mutate 'carrier left running' {param($a) $a.rows[0].evidence.restoration.carrierFinished=$false}
 Mutate 'a mount command left behind' {param($a) $a.rows[0].evidence.restoration.mountCommandsEmpty=$false}
 Mutate 'force mode after the stop not recorded' {param($a) $a.rows[0].evidence.restoration.forceModeAfterStop='unknown'}
 Mutate 'still moving after the stop' {param($a) $a.rows[1].evidence.after.mountMoving=$true}
 Mutate 'charging state residue' {param($a) $a.rows[0].evidence.after.descriptorCharging=$true}
 Mutate 'residue probe not admitted' {param($a) $a.rows[0].evidence.residueProbe.admitted=$false}
 Mutate 'residue probe executed by the rider' {param($a) $a.rows[0].evidence.residueProbe.executor='rider'}
 Mutate 'next lawful path left force mode latched' {param($a) $a.rows[0].evidence.residueProbe.firstMove.forceMode=$true}
 Mutate 'residue probe never moved' {param($a) $a.rows[0].evidence.residueProbe.firstMove=$null}
 Mutate 'residue probe moved nothing' {param($a) $a.rows[0].evidence.residueProbe.movedDistance=0.1}
 Mutate 'residue probe timed out' {param($a) $a.rows[0].evidence.residueProbe.timedOut=$true}
 Mutate 'a pair attack delivered by the measurement' {param($a) $a.rows[0].evidence.attackRules=1;$a.rows[0].evidence.attackRulesNonOpportunity=1}
 Mutate 'attack-rule counts that do not reconcile' {param($a) $a.rows[0].evidence.attackRules=2;$a.rows[0].evidence.attackRulesOpportunity=1}
 Mutate 'a negative attack-rule count' {param($a) $a.rows[0].evidence.attackRules=-1;$a.rows[0].evidence.attackRulesOpportunity=-1}
 Mutate 'missing compiled attack-rule evidence' {param($a) $a.rows[0].evidence.attackRuleEvidence=$null}
 Mutate 'left the straight line' {param($a) $a.rows[0].evidence.stop.maximumLateralDeviation=1.2}
 Mutate 'arrival outside reach' {param($a) $a.rows[0].evidence.reach.arrivalWithinReach=$false}
 Mutate 'arrival that barely moved' {param($a) $a.rows[0].evidence.stop.movedDistance=0.5}
 Mutate 'arrival at walking speed' {param($a) $a.rows[0].evidence.stop.peakSpeedMps=6.0}
 Mutate 'interrupt that arrived' {param($a) $a.rows[1].evidence.reach.arrivalWithinReach=$true}
 Mutate 'interrupt without movement' {param($a) $a.rows[1].evidence.stop.movedDistance=0}
 Mutate 'interrupt that settled slowly' {param($a) $a.rows[1].evidence.stop.settleSeconds=2.5}
 Mutate 'stalled path claimed as arrival' {param($a) $a.rows[0].evidence.stop.reason='stalled'}
 Mutate 'carrier ended claimed as arrival' {param($a) $a.rows[0].evidence.stop.reason='carrier-ended'}
 Mutate 'missing required row on PASS' {param($a) $a.rows=@($a.rows[0]);$a.subscenarioPassCount=1}
 Mutate 'duplicate row' {param($a) $a.rows=@($a.rows[0],$a.rows[0]);$a.subscenarioPassCount=2}
 Mutate 'unknown row' {param($a) $a.rows[1].name='C6B-PATH-other';$a.rows[1].evidence.case='C6B-PATH-other'}
 Mutate 'failure-only row claimed PASS' {param($a) $a.rows+=@([pscustomobject]@{name='phase3d-horse-runtime-exception';status='PASS';evidence=$null});$a.subscenarioPassCount=3}
 Mutate 'row counts differ' {param($a) $a.subscenarioPassCount=5}
 Mutate 'measurement contract absent' {param($a) $a.observations.chunk6bChargePath.contract='other'}
 # A native attack of opportunity against the armed target is the engine's own reflex and is recorded, not refused.
 $opportunity=New-Artifact $mode; $opportunity.rows[0].evidence.attackRules=1; $opportunity.rows[0].evidence.attackRulesOpportunity=1; $opportunity.rows[0].evidence.attackRuleEvidence.pairOpportunityAttackRules=1
 Accept ($mode+' a native opportunity attack recorded during the path') { Assert-KmcChunk6bChargePathEvidence $request $opportunity 'PASS' }
 if($mode-ceq'TB'){
  # The carrier is an ordinary pair ground move, so the rider turn's own movement is lawfully spent in turn-based mode.
  $spent=New-Artifact $mode; $spent.rows[0].evidence.costs.riderMoveDelta=3.0
  Accept 'TB carrier spent the rider turn movement' { Assert-KmcChunk6bChargePathEvidence $request $spent 'PASS' }
  Mutate 'rider turn moved before the path' {param($a) $a.rows[0].evidence.before.turn.timeMoved=1.0}
  Mutate 'a turn-based path with no paired movement observation' {param($a) foreach($s in $a.rows[0].evidence.samples){$s.pairMovement='not-observed'}}
  Mutate 'a paired observation whose mount time never advanced' {param($a) foreach($s in $a.rows[0].evidence.samples){$s.pairMovement='mountMove=0->0;mountTime=0;mountStepMetres=0'}}
  Mutate 'a rider turn missing its movement counters' {param($a) $a.rows[0].evidence.after.turn.timeMovedInForceMode='unknown'}
  Mutate 'forced time that went backwards mid-path' {param($a) $a.rows[0].evidence.samples[4].turnTimeForced=-1.0}
  Mutate 'path off the rider turn' {param($a) $a.rows[0].evidence.before.turn.isRider=$false}
  Mutate 'a mount move charged for carrying' {param($a) $a.rows[0].evidence.costs.mountMoveDelta=3.0}
 } else {
  Mutate 'RT rider move charged at all' {param($a) $a.rows[0].evidence.costs.riderMoveDelta=0.03}
 }
 # A FAIL artifact may carry a failed row and need not be complete.
 $failed=New-Artifact $mode; $failed.status='FAIL'; $failed.rows=@($failed.rows[0]); $failed.rows[0].status='FAIL'; $failed.subscenarioPassCount=0; $failed.subscenarioFailCount=1
 Accept ($mode+' failed artifact retained without a verdict') { Assert-KmcChunk6bChargePathEvidence $request $failed 'FAIL' }
}
Write-Host ("CHUNK 6B CHARGE PATH READER PASS=$($script:checks) FAIL=0; synthetic acceptance and refusal only, no native qualification")
