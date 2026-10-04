[CmdletBinding()]
param()
# Synthetic acceptance/refusal regression for the Chunk 6B charge-delivery reader
# (scripts/runtime/Chunk6bChargeEvidence.ps1). Never launches the game; no native qualification.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6bChargeEvidence.ps1')
$script:checks=0
$script:fixtureMode='RT'
$kmc=ChargeKmcAbilityGuid
$stock=ChargeStockAbilityGuid
function Copy-Case($x){ $x|ConvertTo-Json -Depth 40|ConvertFrom-Json }
# Injected sections must round-trip exactly as the artifact did, so the reader sees the same object shape.
function Json($x){ $x|ConvertTo-Json -Depth 30|ConvertFrom-Json }
function New-Identity([bool]$Present,[bool]$Setting){
 [ordered]@{setting=$Setting;kmcChargePresent=$Present;kmcChargeBlueprint=$(if($Present){$kmc}else{$null})
  kmcChargeActionType=$(if($Present){'Standard'}else{$null});kmcChargeFullRound=$(if($Present){$true}else{$null})
  kmcChargeRequiresFullRound=$(if($Present){$true}else{$null});kmcChargeMinRange=$(if($Present){4.65}else{$null})
  kmcChargeComponent=$(if($Present){'KingmakerMountedCombat.Integration.MountedChargeAbilityLogic'}else{$null})
  kmcChargeIsStockLogic=$false;stockChargePresent=$true;stockChargeIsStockLogic=$true;abilityCount=12}
}
function New-Actor([string]$Id,[double]$Standard,[double]$Move){ [ordered]@{id=$Id;standard=$Standard;move=$Move;swift=0;position=@(0,0,0);commands=1;raw=@();queue=@();previous=$null;group=0} }
function New-State([string]$Kind,[double]$Distance,[double]$RiderStandard,[bool]$Charging){
 [ordered]@{kind=$Kind;frame=100;nativeSeconds=10.0;rider=(New-Actor 'rider' $RiderStandard 0);mount=(New-Actor 'mount' 0 0)
  relationship='Mounted';distanceToTarget=$Distance;riderDistanceToTarget=$Distance;mountCharging=$Charging
  mountSpeedOverride=$(if($Charging){10.16}else{$null});mountMoving=$Charging;mountCombatSpeedMps=5.08
  riderStateCharging=$Charging;mountStateCharging=$false;chargeBuffPresent=$Charging;chargeBuffRounds=6.0
  pairCommandActive=$Charging;pairMovement='mountMove=0->0'
  turn=$(if($script:fixtureMode-ceq'TB'){[ordered]@{unit='rider';isRider=$true;status='Acting';acting=$true;timeMoved=0.0;timeMovedInForceMode=0.0}}else{$null})}
}
function New-Rules([int]$NonOpportunity,[int]$Opportunity,[bool]$Charge){
 $events=@()
 for($i=0;$i-lt$NonOpportunity;$i++){ $events+=,([ordered]@{sequence=$i+1;frame=120+$i;actorId='rider';targetId='target';attackOfOpportunity=$false;firstAttack=$true;fullAttack=$false;charge=$Charge;attackNumber=1;attacksCount=1;weaponGuid='w'}) }
 for($i=0;$i-lt$Opportunity;$i++){ $events+=,([ordered]@{sequence=$NonOpportunity+$i+1;frame=140+$i;actorId='rider';targetId='target';attackOfOpportunity=$true;firstAttack=$true;fullAttack=$false;charge=$false;attackNumber=1;attacksCount=1;weaponGuid='w'}) }
 [ordered]@{riderAttackRules=($NonOpportunity+$Opportunity);riderResolved=$NonOpportunity;mountResolved=0;mountAttackRules=0
  riderNonOpportunityAttackRules=$NonOpportunity;mountNonOpportunityAttackRules=0;pairNonOpportunityAttackRules=$NonOpportunity
  riderOpportunityAttackRules=$Opportunity;mountOpportunityAttackRules=0;pairOpportunityAttackRules=$Opportunity
  pairAttackRolls=$NonOpportunity;pairOpportunityAttackRolls=$Opportunity;pairDamageRules=$NonOpportunity;pairForcedD20=0;pairDamage=4
  firstPairActorId='rider';lastPairActorId='rider';lastRiderAttackType='Standard';lastRiderAttackDoNotProvoke=$false
  attackRuleEvents=$events;attackRollEvents=@()}
}
function New-Lease{
 [ordered]@{applied=$true;buffApplied=$true;chargingBefore=$false;speedOverrideBefore=$null;speedOverrideApplied=10.16
  mountCombatSpeedMps=5.08;chargingAppliedExactly=$true;chargingObservedThroughout=$true;forceModeAfterApply=$true
  forcedPathCount=2;riderAgentTouched=$false;restored=$true;chargingRestoredExactly=$true;speedOverrideRestoredExactly=$true
  riderChargingRestoredExactly=$true;forceModeAtRestore=$true;riderChargingBefore=$false;observations=@('applied:x')}
}
function New-Intervention([string]$Kind){
 [ordered]@{kind=$Kind;frame=130;nativeSeconds=0.4;mountDistanceAtIntervention=4.0;pairCommandActiveBefore=$true
  riderInCombatBefore=$true;riderStandardBefore=6.0;before=(New-State 'intervention-before' 5.0 6.0 $true)
  after=(New-State 'intervention-after' 5.0 6.0 $true);riderInCombatAfter=$($Kind-ceq'native-command-interrupt')
  riderStandardAfter=$(if($Kind-ceq'native-command-interrupt'){5.8}else{0.0})}
}
function New-TerminatedRow([string]$Case,[string]$Kind){
 [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{
  level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=$true
  before=[ordered]@{identity=(New-Identity $true $true);state=(New-State 'before' 9.0 0 $false)
   available=$true;unavailableReason=$null;canTarget=$true;minRangeMeters=4.65;approachDistance=99.0
   requireFullRound=$true;commandType='Standard';pairCommandState=[ordered]@{frame=100}}
  input=[ordered]@{clicked=$true;hoverPure=$true;frame=110;shell=[ordered]@{present=$true};shellCount=1
   feedback='Mounted charge accepted: the Horse carries the charge.';rejectionCodes=@();chargeAdmitted=0;chargeRefused=0;lastRefusal=$null
   after=(New-State 'input-after' 9.0 0 $false)}
  samples=@()
  after=(New-State 'after' 5.0 4.0 $false)
  identityAfter=(New-Identity $true $true)
  movement=[ordered]@{mountDistance=4.0;riderDistance=4.0;peakSpeedMps=10.3;mountCombatSpeedMps=5.08
   chargingObserved=$true;chargeModeObserved=$true;riderChargeStateObserved=$true}
  economy=[ordered]@{riderStandardMax=6.0;riderMoveMax=0.0;mountStandardMax=0.0;mountMoveMax=0.0
   riderStandardNow=4.0;riderMoveNow=0.0;mountStandardNow=0.0;mountMoveNow=0.0}
  lease=(New-Lease)
  intervention=(New-Intervention $Kind)
  delivery=[ordered]@{chargeAdmitted=1;chargeRefused=0;lastRefusal=$null;feedback='Mounted charge accepted: the Horse carries the charge.';rejectionCodes=@()}
  rules=(New-Rules 0 0 $false)
  attackRules=0;attackRulesOpportunity=0
  pairCommandState=[ordered]@{frame=200}
  terminal=[ordered]@{action='RiderMelee';actorId='rider';resourceOwnerId='rider';targetId='target';result='Interrupted';childAttackStartCount=0;singleAttackMode=$true;nativeFullAttack=$false;nativePlannedAttackCount=1;nativeCompletedAttackCount=0;repathCount=0}
 }}
}
function New-Row([string]$Case){
 if($Case-ceq'C6B-CHARGE-interrupted'){ return New-TerminatedRow $Case 'native-command-interrupt' }
 if($Case-ceq'C6B-CHARGE-combat-ended'){ return New-TerminatedRow $Case 'native-combat-end' }
 $refusal=$Case-cin @('C6B-CHARGE-below-minimum','C6B-CHARGE-spent-standard','C6B-CHARGE-stock-rejected')
 if($Case-ceq'C6B-CHARGE-default-off'){
  return [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=$true
   settingOff=(New-Identity $false $false);settingOn=(New-Identity $true $true);abilityGuid=$kmc}}
 }
 $distance=if($Case-ceq'C6B-CHARGE-below-minimum'){3.5}else{9.0}
 $riderStandard=if($Case-ceq'C6B-CHARGE-spent-standard'){6.0}else{0.0}
 $input=[ordered]@{clicked=(-not$refusal);hoverPure=$true;frame=110
  shell=[ordered]@{present=(-not$refusal)};shellCount=$(if($refusal){0}else{1})
  feedback=$(if($Case-ceq'C6B-CHARGE-stock-rejected'){'Charge is not yet supported while mounted.'}else{'Mounted charge accepted: the Horse carries the charge.'})
  rejectionCodes=@();chargeAdmitted=$(if($refusal){0}else{1});chargeRefused=$(if($refusal){1}else{0});lastRefusal=$null
  after=(New-State 'input-after' $distance $riderStandard (-not$refusal))}
 if($Case-ceq'C6B-CHARGE-stock-rejected'){
  $input['stockBlueprint']=$stock;$input['stockAvailable']=$false;$input['stockCanTarget']=$false
  $input['safetyFeedback']='Charge is not yet supported while mounted.'
  $input['before']=(New-State 'stock-before' $distance 0 $false);$input['after']=(New-State 'stock-after' $distance 0 $false)
 }
 [ordered]@{name=$Case;status='PASS';evidence=[ordered]@{
  level='NATIVE DELIVERY';mode=$script:fixtureMode;case=$Case;mounted=$true
  before=[ordered]@{identity=(New-Identity $true $true);state=(New-State 'before' $distance $riderStandard $false)
   available=$(if($refusal){$false}else{$true});unavailableReason=$null
   canTarget=$(if($refusal){$false}else{$true});minRangeMeters=4.65;approachDistance=99.0
   requireFullRound=$true;commandType='Standard';pairCommandState=[ordered]@{frame=100}}
  input=$input
  samples=@()
  after=(New-State 'after' $distance $riderStandard $false)
  identityAfter=(New-Identity $true $true)
  movement=[ordered]@{mountDistance=$(if($refusal){0.0}else{6.8});riderDistance=$(if($refusal){0.0}else{6.8})
   peakSpeedMps=$(if($refusal){0.0}else{10.3});mountCombatSpeedMps=5.08;chargingObserved=(-not$refusal)
   chargeModeObserved=(-not$refusal);riderChargeStateObserved=(-not$refusal)}
  economy=[ordered]@{riderStandardMax=$(if($refusal){0.0}else{6.0});riderMoveMax=$(if($refusal){0.0}else{3.0})
   mountStandardMax=0.0;mountMoveMax=0.0;riderStandardNow=$riderStandard;riderMoveNow=0.0;mountStandardNow=0.0;mountMoveNow=0.0}
  lease=$(if($refusal){$null}else{New-Lease})
  delivery=[ordered]@{chargeAdmitted=$(if($refusal){0}else{1});chargeRefused=0;lastRefusal=$null
   feedback=$(if($Case-ceq'C6B-CHARGE-stock-rejected'){'Charge is not yet supported while mounted.'}else{'Mounted charge accepted: the Horse carries the charge.'});rejectionCodes=@()}
  rules=(New-Rules $(if($refusal){0}else{1}) 0 $true)
  attackRules=$(if($refusal){0}else{1});attackRulesOpportunity=0
  pairCommandState=[ordered]@{frame=200}
  terminal=$(if($refusal){$null}else{[ordered]@{action='RiderMelee';actorId='rider';resourceOwnerId='rider';targetId='target';result='Success';childAttackStartCount=1;singleAttackMode=$true;nativeFullAttack=$false;nativePlannedAttackCount=1;nativeCompletedAttackCount=1;repathCount=0}})
 }}
}
function New-Artifact([string]$Mode='RT'){
 $script:fixtureMode=$Mode
 $rows=@(Get-KmcChunk6bChargeRows $Mode|ForEach-Object { New-Row $_ })
 Copy-Case ([ordered]@{schemaVersion=34;evidenceKind='phase3d-horse-scenario-evidence';scenario='chunk6b-charge-rt';status='PASS'
  rows=$rows
  observations=[ordered]@{chunk6bCharge=[ordered]@{contract='chunk6b-pair-charge-delivery';mode=$Mode;cases=@(Get-KmcChunk6bChargeRows);abilityGuid=$kmc;stockChargeBlueprint=$stock;beyondMaximumReachable=$false;spawnEnvelopeMinimum=3.0;spawnEnvelopeMaximum=20.0;settingBefore=$false;settingAfter=$false;settingRestored=$true}}
  subscenarioPassCount=$rows.Count;subscenarioFailCount=0;errors=@()})
}
$request=[pscustomobject]@{scenario='chunk6b-charge-rt'}
function Accept([string]$Name,[scriptblock]$Body){ & $Body; $script:checks++; Write-Host ('PASS '+$Name) }
function Reject([string]$Name,[scriptblock]$Body){ $failed=$false; try { & $Body } catch { $failed=$true }; if(-not$failed){ throw ('Invalid evidence accepted: '+$Name) }; $script:checks++; Write-Host ('PASS refuses '+$Name) }
function Mutate([string]$Name,[scriptblock]$Change){ $a=New-Artifact; & $Change $a; Reject $Name { Assert-KmcChunk6bChargeEvidence $request $a 'PASS' } }
function Row($Artifact,[string]$Case){ @($Artifact.rows|Where-Object {$_.name-ceq$Case})[0].evidence }

Accept 'a lawful pair-owned mounted charge delivery' { Assert-KmcChunk6bChargeEvidence $request (New-Artifact) 'PASS' }
# A native attack of opportunity alongside the one deliberate charge attack is the engine's own reflex.
$withOpportunity=New-Artifact; (Row $withOpportunity 'C6B-CHARGE-positive').rules=(Json (New-Rules 1 1 $true)); (Row $withOpportunity 'C6B-CHARGE-positive').attackRules=2; (Row $withOpportunity 'C6B-CHARGE-positive').attackRulesOpportunity=1
Accept 'a native opportunity attack during the charge' { Assert-KmcChunk6bChargeEvidence $request $withOpportunity 'PASS' }
# The native charge buff may lawfully outlive the transaction for its own duration.
$buffRemains=New-Artifact; (Row $buffRemains 'C6B-CHARGE-positive').after.chargeBuffPresent=$true
Accept 'the native charge buff outliving the transaction' { Assert-KmcChunk6bChargeEvidence $request $buffRemains 'PASS' }

Mutate 'wrong schema' {param($a) $a.schemaVersion=33}
Mutate 'another delivery contract' {param($a) $a.observations.chunk6bCharge.contract='other'}
Mutate 'another ability in the contract' {param($a) $a.observations.chunk6bCharge.abilityGuid=$stock}
Mutate 'an unrestored mod setting' {param($a) $a.observations.chunk6bCharge.settingRestored=$false}
Mutate 'a setting left on' {param($a) $a.observations.chunk6bCharge.settingAfter=$true}
Mutate 'a missing required row' {param($a) $a.rows=@($a.rows[0],$a.rows[1]);$a.subscenarioPassCount=2}
Mutate 'a duplicate row' {param($a) $a.rows=@($a.rows[0],$a.rows[0],$a.rows[2],$a.rows[3],$a.rows[4]);$a.subscenarioPassCount=5}
Mutate 'an unknown row' {param($a) $a.rows[1].name='C6B-CHARGE-other';$a.rows[1].evidence.case='C6B-CHARGE-other'}
Mutate 'a failure-only row claimed PASS' {param($a) $a.rows+=@([pscustomobject]@{name='phase3d-horse-runtime-exception';status='PASS';evidence=$null});$a.subscenarioPassCount=6}
Mutate 'row counts that differ' {param($a) $a.subscenarioPassCount=9}
Mutate 'an unmounted row' {param($a) (Row $a 'C6B-CHARGE-positive').mounted=$false}
Mutate 'the stock Charge replaced' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.stockChargePresent=$false}
Mutate 'the stock Charge logic changed' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.stockChargeIsStockLogic=$false}
Mutate 'a charge that is not a full-round action' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeFullRound=$false}
Mutate 'a charge that is not a standard action' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeActionType='Free'}
Mutate 'a charge carrying the stock component' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeIsStockLogic=$true}
Mutate 'a charge with another component' {param($a) (Row $a 'C6B-CHARGE-positive').before.identity.kmcChargeComponent='Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge'}
Mutate 'a charge ability leased while the setting is off' {param($a) (Row $a 'C6B-CHARGE-default-off').settingOff.kmcChargePresent=$true}
Mutate 'a charge ability absent once the setting is on' {param($a) (Row $a 'C6B-CHARGE-default-off').settingOn.kmcChargePresent=$false}
Mutate 'a default-off row that started with the setting on' {param($a) (Row $a 'C6B-CHARGE-default-off').settingOff.setting=$true}
Mutate 'an impure hover' {param($a) (Row $a 'C6B-CHARGE-positive').input.hoverPure=$false}
Mutate 'a click that was not admitted' {param($a) (Row $a 'C6B-CHARGE-positive').input.clicked=$false}
Mutate 'two native shells' {param($a) (Row $a 'C6B-CHARGE-positive').input.shellCount=2}
Mutate 'a rejection code on the lawful charge' {param($a) (Row $a 'C6B-CHARGE-positive').input.rejectionCodes=@('WrongTurn')}
Mutate 'a charge the controller never admitted' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.chargeAdmitted=0}
Mutate 'a lawful charge with no post-settlement delivery section' {param($a) (Row $a 'C6B-CHARGE-positive').delivery=$null}
Mutate 'a charge admitted twice' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.chargeAdmitted=2}
Mutate 'a charge the controller refused at delivery' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.chargeRefused=1}
Mutate 'a delivered charge with no rejection-code context' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.PSObject.Properties.Remove('rejectionCodes')}
Mutate 'a delivered charge leaving a refusal reason behind' {param($a) (Row $a 'C6B-CHARGE-positive').delivery.lastRefusal='Mounted Charge requires the rider standard action.'}
Mutate 'a refusal that reached the controller delivery' {param($a) (Row $a 'C6B-CHARGE-below-minimum').delivery.chargeAdmitted=1}
Mutate 'a refusal refused at the controller delivery' {param($a) (Row $a 'C6B-CHARGE-below-minimum').delivery.chargeRefused=1}
Mutate 'a refusal with no post-settlement delivery section' {param($a) (Row $a 'C6B-CHARGE-below-minimum').delivery=$null}
Mutate 'a charge where the mount never moved' {param($a) (Row $a 'C6B-CHARGE-positive').movement.mountDistance=0.2}
Mutate 'a charge at walking speed' {param($a) (Row $a 'C6B-CHARGE-positive').movement.peakSpeedMps=5.1}
Mutate 'a charge where the mount never charged' {param($a) (Row $a 'C6B-CHARGE-positive').movement.chargingObserved=$false}
Mutate 'a charge with no lease' {param($a) (Row $a 'C6B-CHARGE-positive').lease=$null}
Mutate 'a lease that was not restored' {param($a) (Row $a 'C6B-CHARGE-positive').lease.restored=$false}
Mutate 'a lease that did not restore the charging flag' {param($a) (Row $a 'C6B-CHARGE-positive').lease.chargingRestoredExactly=$false}
Mutate 'a lease that did not restore the speed override' {param($a) (Row $a 'C6B-CHARGE-positive').lease.speedOverrideRestoredExactly=$false}
Mutate 'a lease that left the rider charging' {param($a) (Row $a 'C6B-CHARGE-positive').lease.riderChargingRestoredExactly=$false}
Mutate 'a lease that touched the rider agent' {param($a) (Row $a 'C6B-CHARGE-positive').lease.riderAgentTouched=$true}
Mutate 'a lease that forced no path' {param($a) (Row $a 'C6B-CHARGE-positive').lease.forcedPathCount=0}
Mutate 'a lease that did not double the mount speed' {param($a) (Row $a 'C6B-CHARGE-positive').lease.speedOverrideApplied=6.0}
Mutate 'a lease without the charge buff' {param($a) (Row $a 'C6B-CHARGE-positive').lease.buffApplied=$false}
Mutate 'a charge that spent no rider standard action' {param($a) (Row $a 'C6B-CHARGE-positive').economy.riderStandardMax=0}
Mutate 'a mount charged a standard action' {param($a) (Row $a 'C6B-CHARGE-positive').economy.mountStandardMax=6.0}
Mutate 'a mount charged a move action' {param($a) (Row $a 'C6B-CHARGE-positive').economy.mountMoveMax=3.0}
Mutate 'a charge with two deliberate attacks' {param($a) (Row $a 'C6B-CHARGE-positive').rules=(Json (New-Rules 2 0 $true));(Row $a 'C6B-CHARGE-positive').attackRules=2}
Mutate 'a charge with no attack' {param($a) (Row $a 'C6B-CHARGE-positive').rules=(Json (New-Rules 0 0 $true));(Row $a 'C6B-CHARGE-positive').attackRules=0}
Mutate 'an attack without the native charge rule' {param($a) (Row $a 'C6B-CHARGE-positive').rules=(Json (New-Rules 1 0 $false))}
Mutate 'a mount-initiated attack' {param($a) (Row $a 'C6B-CHARGE-positive').rules.mountAttackRules=1}
Mutate 'a charge terminal with two child attacks' {param($a) (Row $a 'C6B-CHARGE-positive').terminal.childAttackStartCount=2}
Mutate 'a charge terminal that was a full attack' {param($a) (Row $a 'C6B-CHARGE-positive').terminal.nativeFullAttack=$true}
Mutate 'charge residue on the mount agent' {param($a) (Row $a 'C6B-CHARGE-positive').after.mountCharging=$true}
Mutate 'charge residue on the rider state' {param($a) (Row $a 'C6B-CHARGE-positive').after.riderStateCharging=$true}
Mutate 'a speed override left behind' {param($a) (Row $a 'C6B-CHARGE-positive').after.mountSpeedOverride=10.16}
Mutate 'an active pair command left behind' {param($a) (Row $a 'C6B-CHARGE-positive').after.pairCommandActive=$true}
Mutate 'a targetable target inside the minimum distance' {param($a) (Row $a 'C6B-CHARGE-below-minimum').before.canTarget=$true}
Mutate 'a minimum-range row whose target was not inside the minimum' {param($a) (Row $a 'C6B-CHARGE-below-minimum').before.state.distanceToTarget=9.0}
Mutate 'a refusal that admitted a shell' {param($a) (Row $a 'C6B-CHARGE-below-minimum').input.shellCount=1}
Mutate 'a refusal that moved the pair' {param($a) (Row $a 'C6B-CHARGE-below-minimum').movement.mountDistance=4.0}
Mutate 'a refusal that spent an action' {param($a) (Row $a 'C6B-CHARGE-below-minimum').economy.riderStandardMax=6.0}
Mutate 'a refusal that attacked' {param($a) (Row $a 'C6B-CHARGE-below-minimum').attackRules=1}
Mutate 'a refusal that applied a lease' {param($a) (Row $a 'C6B-CHARGE-below-minimum').lease=(Json (New-Lease))}
Mutate 'a refusal that left charge state behind' {param($a) (Row $a 'C6B-CHARGE-below-minimum').after.mountCharging=$true}
Mutate 'a repeated charge with an unspent standard action' {param($a) (Row $a 'C6B-CHARGE-spent-standard').before.state.rider.standard=0}
Mutate 'a repeated charge that was available and targetable' {param($a) (Row $a 'C6B-CHARGE-spent-standard').before.available=$true;(Row $a 'C6B-CHARGE-spent-standard').before.canTarget=$true}
Mutate 'a stock rejection row that clicked another ability' {param($a) (Row $a 'C6B-CHARGE-stock-rejected').input.stockBlueprint=$kmc}
Mutate 'an interrupted charge that was never admitted' {param($a) (Row $a 'C6B-CHARGE-interrupted').delivery.chargeAdmitted=0}
Mutate 'an interrupted charge that never carried the pair' {param($a) (Row $a 'C6B-CHARGE-interrupted').movement.mountDistance=0.4}
Mutate 'an interrupted charge at walking speed' {param($a) (Row $a 'C6B-CHARGE-interrupted').movement.peakSpeedMps=5.1}
Mutate 'an interrupted charge that delivered an attack' {param($a) (Row $a 'C6B-CHARGE-interrupted').rules=(Json (New-Rules 1 0 $true)); (Row $a 'C6B-CHARGE-interrupted').attackRules=1}
Mutate 'an interrupted charge whose terminal started a child attack' {param($a) (Row $a 'C6B-CHARGE-interrupted').terminal.childAttackStartCount=1}
Mutate 'an interrupted charge that did not restore its lease' {param($a) (Row $a 'C6B-CHARGE-interrupted').lease.restored=$false}
Mutate 'an interrupted charge that left charge residue' {param($a) (Row $a 'C6B-CHARGE-interrupted').after.mountCharging=$true}
Mutate 'an interrupted charge with no intervention record' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention=$null}
Mutate 'an interrupted charge whose intervention had no live pair command' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.pairCommandActiveBefore=$false}
Mutate 'an interrupted charge that intervened before the pair moved' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.mountDistanceAtIntervention=0.2}
Mutate 'an interrupted charge whose shell had not paid' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.riderStandardBefore=0.0}
Mutate 'an interrupted charge that refunded the rider standard action' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.riderStandardAfter=0.0}
Mutate 'an interrupted charge that left combat instead' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.riderInCombatAfter=$false}
Mutate 'a combat-end charge that never left combat' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.riderInCombatAfter=$true}
Mutate 'a combat-end charge with no recorded cooldown after' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.riderStandardAfter='unknown'}
Mutate 'an interrupted charge recorded as a combat end' {param($a) (Row $a 'C6B-CHARGE-interrupted').intervention.kind='native-combat-end'}
Mutate 'a combat-end charge recorded as an interrupt' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.kind='native-command-interrupt'}
Mutate 'a combat-end charge that never started in combat' {param($a) (Row $a 'C6B-CHARGE-combat-ended').intervention.riderInCombatBefore=$false}
Mutate 'a combat-end charge that charged the mount' {param($a) (Row $a 'C6B-CHARGE-combat-ended').economy.mountMoveMax=3.0}
Mutate 'a stock Charge that stayed available and unrefused' {param($a) (Row $a 'C6B-CHARGE-stock-rejected').input.stockAvailable=$true;(Row $a 'C6B-CHARGE-stock-rejected').input.feedback='something else'}

# A FAIL artifact may carry a failed row and need not be complete.
$failed=New-Artifact; $failed.status='FAIL'; $failed.rows=@($failed.rows[0]); $failed.rows[0].status='FAIL'; $failed.subscenarioPassCount=0; $failed.subscenarioFailCount=1
Accept 'a failed artifact retained without a verdict' { Assert-KmcChunk6bChargeEvidence $request $failed 'FAIL' }

# Turn-based mode: the delivery and refusal core only, on the rider own unmoved turn.
$tbRequest=[pscustomobject]@{scenario='chunk6b-charge-tb'}
function MutateTb([string]$Name,[scriptblock]$Change){ $a=New-Artifact 'TB'; & $Change $a; Reject ('TB: '+$Name) { Assert-KmcChunk6bChargeEvidence $tbRequest $a 'PASS' } }
Accept 'TB lawful pair-owned mounted charge delivery' { Assert-KmcChunk6bChargeEvidence $tbRequest (New-Artifact 'TB') 'PASS' }
MutateTb 'a turn-based artifact read as real time' {param($a) $a.observations.chunk6bCharge.mode='RT'}
MutateTb 'a turn-based charge with no recorded turn' {param($a) (Row $a 'C6B-CHARGE-positive').before.state.turn=$null}
MutateTb 'a turn-based charge off the rider own turn' {param($a) (Row $a 'C6B-CHARGE-positive').before.state.turn.isRider=$false}
MutateTb 'a turn-based charge after the turn had moved' {param($a) (Row $a 'C6B-CHARGE-positive').before.state.turn.timeMoved=1.0}
MutateTb 'a turn-based charge cast on a preparing turn' {param($a) (Row $a 'C6B-CHARGE-positive').before.state.turn.acting=$false}
MutateTb 'a turn-based artifact carrying a real-time only row' {param($a) $a.rows=@($a.rows)+@((Json (New-Row 'C6B-CHARGE-interrupted'))); $a.subscenarioPassCount=$a.rows.Count}
Accept 'TB failed artifact retained without a verdict' { $f=New-Artifact 'TB'; $f.status='FAIL'; $f.rows=@($f.rows[0]); $f.rows[0].status='FAIL'; $f.subscenarioPassCount=0; $f.subscenarioFailCount=1; Assert-KmcChunk6bChargeEvidence $tbRequest $f 'FAIL' }
Write-Host ("CHUNK 6B CHARGE READER PASS=$($script:checks) FAIL=0; synthetic acceptance and refusal only, no native qualification")
