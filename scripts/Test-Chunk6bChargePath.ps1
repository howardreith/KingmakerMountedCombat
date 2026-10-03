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
function New-Turn([double]$Moved,[double]$Forced,[double]$Stepped,[bool]$Step){ [ordered]@{unit='rider';isRider=$true;status='Acting';acting=$true;timeMoved=$Moved;timeMovedInForceMode=$Forced;timeMovedByFiveFootStep=$Stepped;metersMovedByFiveFootStep=0.73;enabledFiveFootStep=$Step;enabledSingleActionMove=$false} }
function New-Row([string]$Case,[string]$Mode){
 $tb=$Mode-ceq'TB'
 $samples=@()
 for($i=0;$i-lt6;$i++){ $samples+=,([ordered]@{nativeSeconds=0.1*$i;frame=100+$i;distanceToTarget=9.0-1.4*$i;mountMoving=$true;forceMode=$true;speedMps=13.4;riderCommandsEmpty=$true;mountCommandsEmpty=$true;riderMove=0;riderStandard=0;mountMove=0;mountStandard=0;turnTimeMoved=$(if($tb){0.24+0.1*$i}else{$null});turnTimeForced=$(if($tb){0.1*$i}else{$null});pairMovement='mountMove=0->0';charging=$true;descriptorCharging=$false}) }
 $arrival=$Case-ceq'C6B-PATH-straight-arrival'
 $turnBefore=if($tb){New-Turn 0.24 0 0.24 $false}else{$null}
 $turnAfter=if($tb){New-Turn 0.74 0.5 0.24 $false}else{$null}
 [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{
  level='NATIVE MEASUREMENT';mode=$Mode;case=$Case;mounted=$true
  geometry=[ordered]@{distance3D=9.0;minimumRange=4.6;maximumRange=40.2336;mountCombatSpeedMps=6.7056;mountCorpulence=1.0;targetCorpulence=0.5;straightRoute=$true;landingBlocked=$false;customCanTarget=$true;mountAvoidanceDisabled=$false;riderAvoidanceDisabled=$true}
  lease=[ordered]@{chargingBefore=$false;speedOverrideBefore=$null;chargingApplied=$true;speedOverrideApplied=13.4112;forcePathApplied=$true;forceModeAfterApply=$true;riderAgentTouched=$false}
  before=[ordered]@{rider=(New-Actor 'rider' 0 0);mount=(New-Actor 'mount' 0 0);turn=$turnBefore}
  samples=$samples
  stop=[ordered]@{reason=$(if($arrival){'arrival'}else{'interrupt'});elapsedSeconds=$(if($arrival){0.6}else{0.2});settleSeconds=0.3;distanceToTarget=$(if($arrival){1.9}else{7.4});movedDistance=$(if($arrival){7.1}else{1.6});maximumLateralDeviation=0.12;peakSpeedMps=13.2}
  after=[ordered]@{forceMode=$false;mountMoving=$false;charging=$false;descriptorCharging=$false;speedOverride=$null;turn=$turnAfter}
  restoration=[ordered]@{chargingRestored=$true;speedOverrideRestored=$true;forceModeCleared=$true}
  costs=[ordered]@{riderStandardDelta=0;riderMoveDelta=0;mountStandardDelta=0;mountMoveDelta=0}
  reach=[ordered]@{radius=2.0;arrivalWithinReach=$arrival}
  commandsObservedEmptyThroughout=$true;chargingObservedThroughout=$true;attackRules=0 } }
}
function New-Artifact([string]$Mode){
 $scenario=if($Mode-ceq'TB'){'chunk6b-charge-path-tb'}else{'chunk6b-charge-path-rt'}
 Copy-Case ([ordered]@{schemaVersion=33;evidenceKind='phase3d-horse-scenario-evidence';scenario=$scenario;status='PASS';rows=@((New-Row 'C6B-PATH-straight-arrival' $Mode),(New-Row 'C6B-PATH-interrupt-stop' $Mode));observations=[ordered]@{chunk6bChargePath=[ordered]@{contract='chunk6b-pair-forced-path-measurement';mode=$Mode;cases=@('C6B-PATH-straight-arrival','C6B-PATH-interrupt-stop')}};subscenarioPassCount=2;subscenarioFailCount=0;errors=@()})
}
function Accept([string]$Name,[scriptblock]$Body){ & $Body; $script:checks++; Write-Host ('PASS '+$Name) }
function Reject([string]$Name,[scriptblock]$Body){ $failed=$false; try { & $Body } catch { $failed=$true }; if(-not$failed){ throw ('Invalid evidence accepted: '+$Name) }; $script:checks++; Write-Host ('PASS refuses '+$Name) }
foreach($mode in @('RT','TB')){
 $scenario=if($mode-ceq'TB'){'chunk6b-charge-path-tb'}else{'chunk6b-charge-path-rt'}
 $request=[pscustomobject]@{scenario=$scenario}
 Accept ($mode+' lawful forced path and interrupt') { Assert-KmcChunk6bChargePathEvidence $request (New-Artifact $mode) 'PASS' }
 function Mutate([string]$Name,[scriptblock]$Change){ $a=New-Artifact $mode; & $Change $a; Reject ($mode+': '+$Name) { Assert-KmcChunk6bChargePathEvidence $request $a 'PASS' } }
 Mutate 'wrong schema' {param($a) $a.schemaVersion=32}
 Mutate 'other mode row' {param($a) $a.rows[0].evidence.mode=$(if($mode-ceq'TB'){'RT'}else{'TB'})}
 Mutate 'unmounted pair' {param($a) $a.rows[0].evidence.mounted=$false}
 Mutate 'bent route' {param($a) $a.rows[0].evidence.geometry.straightRoute=$false}
 Mutate 'blocked landing' {param($a) $a.rows[0].evidence.geometry.landingBlocked=$true}
 Mutate 'inside minimum range' {param($a) $a.rows[0].evidence.geometry.distance3D=3.0}
 Mutate 'beyond maximum range' {param($a) $a.rows[0].evidence.geometry.distance3D=50.0}
 Mutate 'maximum range not six times the speed' {param($a) $a.rows[0].evidence.geometry.maximumRange=30.0}
 Mutate 'no forced path applied' {param($a) $a.rows[0].evidence.lease.forcePathApplied=$false}
 Mutate 'speed override below charge speed' {param($a) $a.rows[0].evidence.lease.speedOverrideApplied=7.0}
 Mutate 'rider agent touched' {param($a) $a.rows[0].evidence.lease.riderAgentTouched=$true}
 Mutate 'a command during the path' {param($a) $a.rows[0].evidence.samples[2].mountCommandsEmpty=$false}
 Mutate 'charging flag lost mid-path' {param($a) $a.rows[0].evidence.samples[3].charging=$false}
 Mutate 'rider move charged' {param($a) $a.rows[0].evidence.costs.riderMoveDelta=3.0}
 Mutate 'mount move charged' {param($a) $a.rows[0].evidence.costs.mountMoveDelta=0.5}
 Mutate 'charging not restored' {param($a) $a.rows[0].evidence.restoration.chargingRestored=$false}
 Mutate 'speed override not restored' {param($a) $a.rows[0].evidence.restoration.speedOverrideRestored=$false}
 Mutate 'force mode residue' {param($a) $a.rows[0].evidence.after.forceMode=$true;$a.rows[0].evidence.restoration.forceModeCleared=$false}
 Mutate 'still moving after the stop' {param($a) $a.rows[1].evidence.after.mountMoving=$true}
 Mutate 'charging state residue' {param($a) $a.rows[0].evidence.after.descriptorCharging=$true}
 Mutate 'an attack rule' {param($a) $a.rows[0].evidence.attackRules=1}
 Mutate 'left the straight line' {param($a) $a.rows[0].evidence.stop.maximumLateralDeviation=1.2}
 Mutate 'arrival outside reach' {param($a) $a.rows[0].evidence.reach.arrivalWithinReach=$false}
 Mutate 'arrival that barely moved' {param($a) $a.rows[0].evidence.stop.movedDistance=0.5}
 Mutate 'arrival at walking speed' {param($a) $a.rows[0].evidence.stop.peakSpeedMps=6.0}
 Mutate 'interrupt that arrived' {param($a) $a.rows[1].evidence.reach.arrivalWithinReach=$true}
 Mutate 'interrupt without movement' {param($a) $a.rows[1].evidence.stop.movedDistance=0}
 Mutate 'interrupt that settled slowly' {param($a) $a.rows[1].evidence.stop.settleSeconds=2.5}
 Mutate 'stalled path claimed as arrival' {param($a) $a.rows[0].evidence.stop.reason='stalled'}
 Mutate 'missing required row on PASS' {param($a) $a.rows=@($a.rows[0]);$a.subscenarioPassCount=1}
 Mutate 'duplicate row' {param($a) $a.rows=@($a.rows[0],$a.rows[0]);$a.subscenarioPassCount=2}
 Mutate 'unknown row' {param($a) $a.rows[1].name='C6B-PATH-other';$a.rows[1].evidence.case='C6B-PATH-other'}
 Mutate 'failure-only row claimed PASS' {param($a) $a.rows+=@([pscustomobject]@{name='phase3d-horse-runtime-exception';status='PASS';evidence=$null});$a.subscenarioPassCount=3}
 Mutate 'row counts differ' {param($a) $a.subscenarioPassCount=5}
 Mutate 'measurement contract absent' {param($a) $a.observations.chunk6bChargePath.contract='other'}
 if($mode-ceq'TB'){
  Mutate 'rider turn moved before the path' {param($a) $a.rows[0].evidence.before.turn.timeMoved=1.0}
  Mutate 'no forced time recorded on the rider turn' {param($a) $a.rows[0].evidence.after.turn.timeMovedInForceMode=0}
  Mutate 'movement outside force mode' {param($a) $a.rows[0].evidence.after.turn.timeMoved=2.0}
  Mutate 'entry that was not a five-foot step' {param($a) $a.rows[0].evidence.before.turn.timeMovedByFiveFootStep=0}
  Mutate 'forced time decreased mid-path' {param($a) $a.rows[0].evidence.samples[4].turnTimeForced=0.0}
  Mutate 'path off the rider turn' {param($a) $a.rows[0].evidence.before.turn.isRider=$false}
 }
 # A FAIL artifact may carry a failed row and need not be complete.
 $failed=New-Artifact $mode; $failed.status='FAIL'; $failed.rows=@($failed.rows[0]); $failed.rows[0].status='FAIL'; $failed.subscenarioPassCount=0; $failed.subscenarioFailCount=1
 Accept ($mode+' failed artifact retained without a verdict') { Assert-KmcChunk6bChargePathEvidence $request $failed 'FAIL' }
}
Write-Host ("CHUNK 6B CHARGE PATH READER PASS=$($script:checks) FAIL=0; synthetic acceptance and refusal only, no native qualification")
