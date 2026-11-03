# Sprint 7 reader: synthetic positive and negative checks. No game, no build. A reader-only assertion is never native evidence.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-S7($Value){ $Value | ConvertTo-Json -Depth 30 | ConvertFrom-Json }
$rider='11111111-1111-1111-1111-111111111111'; $mount='22222222-2222-2222-2222-222222222222'; $hostile='33333333-3333-3333-3333-333333333333'
function New-S7State([hashtable]$Change=@{}){
 $s=[ordered]@{riderStandard=0.0;riderMove=0.0;riderSwift=0.0;mountStandard=0.0;mountMove=0.0;mountSwift=0.0;partnerTimeMoved=0.0;partnerTimeForced=0.0;partnerCanMove=$true;turnUnit=$rider;round=2;activation='a:1';activationSequence=1;riderAc=20;mountAc=23;buff=$false;buffCount=0;riderCharging=$false;mountCharging=$false;speedOverride=$null;mountMoving=$false;distance=12.0;riderCommandsEmpty=$true;mountCommandsEmpty=$true;chargeOwned=$false;mounted=$true}
 foreach($k in $Change.Keys){ $s[$k]=$Change[$k] }
 [pscustomobject]$s
}
function New-S7Click($Before,[bool]$Clicked,[string]$Code,$After=$null){
 [pscustomobject]@{abilityPresent=$true;clicked=$Clicked;policyAllowed=$Clicked;policyCode=$Code;policyReason=$(if($Clicked){'Mounted Charge is available.'}else{'refused'});castDelta=$(if($Clicked){1}else{0});refusalDelta=0;shell=$Clicked;shellTarget=$(if($Clicked){$hostile}else{$null});before=$Before;afterClick=$(if($After){$After}else{$Before})}
}
function New-S7Terminal($State,[string]$Result,[int]$Rules,[bool]$Handed,[double]$Closest=4.0,[string]$Outcome=''){
 [pscustomobject]@{outcomeRejection=$Outcome;state=$State;sequenceLawful=$true;result=$Result;revalidationFailed=$false;rules=$Rules;mountRules=0;closestDistance=$Closest;peakSpeedOverride=10.16;mountCombatSpeed=5.08;riderWeaponRange=1.5;mountCorpulence=1.5;targetCorpulence=1.0;
  lease=[pscustomobject]@{applied=$true;buffApplied=$true;forcedPathOutstanding=$false;riderAgentTouched=$false;restored=$true;chargingRestoredExactly=$true;speedOverrideRestoredExactly=$true;riderChargingRestoredExactly=$true;buffHandedToNative=$Handed;buffOutstanding=$false}}
}
function New-S7Rule([int]$Id,[string]$Attacker,[bool]$Charge,[int]$D20,[int]$Bonus,[bool]$Crit,[int]$Damage){ [pscustomobject]@{identity=$Id;attacker=$Attacker;target=$hostile;d20=$D20;bonus=$Bonus;charge=$Charge;opportunity=$false;resultAtEvent=$(if($Damage -gt 0){'Hit'}else{'Miss'});criticalAtEvent=$Crit;damageRules=$(if($Damage -gt 0){1}else{0});damage=$Damage} }
function New-S7Envelope($M,$Rows){ [pscustomobject]@{schemaVersion=51;status='PASS';errors=@();subscenarioFailCount=0;rows=$Rows;observations=[pscustomobject]@{riderId=$rider;horseId=$mount;sprint7Charge=$M}} }
function New-S7Row([string]$Name,$Evidence){ [pscustomobject]@{name=$Name;status='PASS';evidence=$Evidence} }
function New-S7Measure([string]$Scenario,[string]$Order,$Rules){ [pscustomobject]@{contract='native-mounted-charge-v1';rider=$rider;mount=$mount;defaultFeatureEnabled=$true;order=$Order;cases=@(Get-KmcSprint7Cases $Scenario);attackRules=$Rules;attackD20IsForced=$(if($Scenario -ceq 'sprint7-charge-rt'){$true}else{$null})} }
function New-S7Tb([string]$Scenario){
 $riderFirst=$Scenario -ceq 'sprint7-charge-tb'
 $before=New-S7State
 $after=New-S7State @{riderStandard=6.0;riderMove=3.0;mountMove=1.2;partnerTimeMoved=1.2;partnerTimeForced=1.2;partnerCanMove=$false;buff=$true;buffCount=1;riderAc=18;distance=3.9}
 $f=[pscustomobject]@{order=[pscustomobject]@{riderInitiative=$(if($riderFirst){40}else{-40});mountInitiative=$(if($riderFirst){-40}else{40})};sameTurn=$true;
  whileCharging=(New-S7State @{buff=$true;buffCount=1;riderCharging=$true;riderAc=18;mountCharging=$true;mountMoving=$true;speedOverride=10.16});
  terminal=(New-S7Terminal $after 'Success' 1 $true);secondTerminal=(New-S7Terminal (New-S7State @{buff=$true;buffCount=1;riderAc=18;riderStandard=6.0;riderMove=3.0}) 'Success' 1 $true);mountOwnAttackRules=@(New-S7Rule 7 $mount $false 12 6 $false 5);
  steps=[pscustomobject]@{tooNear=(New-S7Click $before $false 'OutsideSupportedRange');charge=(New-S7Click $before $true $null);spentRefused=(New-S7Click $after $false 'WrongActionState');
   secondCharge=(New-S7Click (New-S7State @{activationSequence=3;round=4}) $true $null);nextTurn=(New-S7State @{activationSequence=2;round=3;distance=3.9;buffSecondsLeft=0.0});partnerSpentRefused=(New-S7Click (New-S7State @{mountStandard=6.0;activationSequence=2;round=3}) $false 'WrongActionState')}}
 New-S7Envelope (New-S7Measure $Scenario $(if($riderFirst){'rider-first'}else{'mount-first'}) @((New-S7Rule 1 $rider $true 11 9 $false 0),(New-S7Rule 7 $mount $false 12 6 $false 5),(New-S7Rule 8 $rider $true 9 9 $false 0))) @(New-S7Row 'S7-tb-charge' $f)
}
function New-S7Rt {
 $before=New-S7State; $spent=New-S7State @{riderStandard=5.0;buff=$true;buffCount=1;riderAc=18;distance=3.9}
 $f=[pscustomobject]@{obstruction=[pscustomobject]@{none=$false;point=1};forcedDice=2;ordinaryRule=(New-S7Rule 2 $rider $false 10 7 $false 4);
  whileCharging=(New-S7State @{buff=$true;buffCount=1;riderCharging=$true;riderAc=18;mountCharging=$true;mountMoving=$true});terminal=(New-S7Terminal $spent 'Success' 1 $true);
  steps=[pscustomobject]@{tooNear=(New-S7Click $before $false 'OutsideSupportedRange');obstructed=(New-S7Click $before $false 'NoPath');charge=(New-S7Click $before $true $null);spentRefused=(New-S7Click $spent $false 'WrongActionState');buffExpired=(New-S7State @{distance=3.9})}}
 $cancelled=New-S7State @{riderStandard=5.5;distance=8.0}
 $c=[pscustomobject]@{stopTerminal=(New-S7Terminal $cancelled 'Interrupt' 0 $false 8.0);deathTerminal=(New-S7Terminal $cancelled 'Interrupt' 0 $false 8.0 'Mounted rider melee ended: the exact target became invalid before completion.');
  steps=[pscustomobject]@{stopCharge=(New-S7Click $before $true $null);deathCharge=(New-S7Click $before $true $null)}}
 New-S7Envelope (New-S7Measure 'sprint7-charge-rt' $null @((New-S7Rule 1 $rider $true 20 9 $true 12),(New-S7Rule 2 $rider $false 10 7 $false 4))) @((New-S7Row 'S7-rt-charge' $f),(New-S7Row 'S7-rt-cancel' $c))
}
function Accept($Artifact,[string]$Scenario){ try{ $null=Assert-KmcSprint7ChargeEvidence ([pscustomobject]@{scenario=$Scenario}) $Artifact 'PASS'; $true }catch{ Write-Host ('    unexpected: '+$_.Exception.Message); $false } }
function Reject($Artifact,[string]$Scenario,[string]$Name){ try{ $null=Assert-KmcSprint7ChargeEvidence ([pscustomobject]@{scenario=$Scenario}) $Artifact 'PASS'; Check $false $Name }catch{ Check $true $Name } }
$rt=New-S7Rt; $tb=New-S7Tb 'sprint7-charge-tb'; $mf=New-S7Tb 'sprint7-charge-tb-mount-first'
Check (Accept $rt 'sprint7-charge-rt') 'complete real-time evidence accepted'
Check (Accept $tb 'sprint7-charge-tb') 'complete rider-first evidence accepted'
Check (Accept $mf 'sprint7-charge-tb-mount-first') 'complete mount-first evidence accepted'
Check ((Get-KmcPhase3dEvidenceDispatch 51 'sprint7-charge-tb').validator -ceq 'Assert-KmcSprint7ChargeEvidence') 'schema 51 dispatches to the Sprint 7 reader'
Check (-not (Test-KmcPhase3dSchemaRegistration 50 'sprint7-charge-rt')) 'a Sprint 6 schema is refused for a Sprint 7 scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 51 'sprint6-feat-rt')) 'the Sprint 7 schema is refused for a Sprint 6 scenario'
Check (-not (Test-KmcChildEntryExpectsMounted 'sprint7-charge-rt')) 'a Sprint 7 child starts from its unmounted fixture handoff'
function Facts($Artifact,[string]$Row){ @($Artifact.rows | Where-Object name -CEQ $Row)[0].evidence }
$r=Copy-S7 $rt; $r.observations.sprint7Charge.defaultFeatureEnabled=$false; Reject $r 'sprint7-charge-rt' 'a row not under the ordinary default refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').steps.tooNear.clicked=$true; Reject $r 'sprint7-charge-rt' 'an admitted too-near charge refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').steps.tooNear.afterClick.riderStandard=6.0; Reject $r 'sprint7-charge-rt' 'a refusal that spent the action refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').steps.obstructed.policyCode='AlreadyActiveCommand'; Reject $r 'sprint7-charge-rt' 'an obstruction refused for another reason refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').steps.charge.shell=$false; Reject $r 'sprint7-charge-rt' 'a charge with no native shell refuses'
$r=Copy-S7 $rt; $r.observations.sprint7Charge.attackRules=@($r.observations.sprint7Charge.attackRules[0],$r.observations.sprint7Charge.attackRules[0],$r.observations.sprint7Charge.attackRules[1]); Reject $r 'sprint7-charge-rt' 'a second charge attack refuses'
$r=Copy-S7 $rt; $r.observations.sprint7Charge.attackRules[0].charge=$false; Reject $r 'sprint7-charge-rt' 'an attack without the charge flag refuses'
$r=Copy-S7 $rt; $r.observations.sprint7Charge.attackRules[0].criticalAtEvent=$false; Reject $r 'sprint7-charge-rt' 'a forced natural 20 with no critical refuses'
$r=Copy-S7 $rt; $r.observations.sprint7Charge.attackRules[0].damageRules=0; Reject $r 'sprint7-charge-rt' 'a hit with no native damage refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-cancel').deathTerminal.outcomeRejection='Mounted pair attack ended: Interrupt.'; Reject $r 'sprint7-charge-rt' 'a target-loss control that lost no target refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').ordinaryRule.bonus=9; Reject $r 'sprint7-charge-rt' 'an ordinary attack keeping the charge bonus refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.state.buff=$false; Reject $r 'sprint7-charge-rt' 'a delivered charge with no native penalty refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.state.buffCount=2; Reject $r 'sprint7-charge-rt' 'a duplicated modifier refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').steps.buffExpired.riderAc=18; Reject $r 'sprint7-charge-rt' 'a penalty that outlives its round refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').whileCharging.mountAc=21; Reject $r 'sprint7-charge-rt' 'the rider''s modifier on the mount refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.closestDistance=7.0; Reject $r 'sprint7-charge-rt' 'stopping outside the rider''s reach refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.closestDistance=1.0; Reject $r 'sprint7-charge-rt' 'overrunning into the target refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.peakSpeedOverride=5.08; Reject $r 'sprint7-charge-rt' 'a charge at ordinary speed refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.lease.riderAgentTouched=$true; Reject $r 'sprint7-charge-rt' 'a second mover refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.state.chargeOwned=$true; Reject $r 'sprint7-charge-rt' 'retained ownership refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-cancel').stopTerminal.lease.buffHandedToNative=$true; Reject $r 'sprint7-charge-rt' 'Stop keeping the penalty refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-cancel').stopTerminal.state.riderStandard=0.0; Reject $r 'sprint7-charge-rt' 'Stop refunding the action refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-cancel').deathTerminal.state.mountCharging=$true; Reject $r 'sprint7-charge-rt' 'a mount still charging after target loss refuses'
$r=Copy-S7 $rt; (Facts $r 'S7-rt-charge').terminal.mountRules=1; Reject $r 'sprint7-charge-rt' 'a mount attack during the charge refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').order.riderInitiative=-40; (Facts $r 'S7-tb-charge').order.mountInitiative=40; Reject $r 'sprint7-charge-tb' 'the wrong initiative arrangement refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').terminal.state.activationSequence=2; Reject $r 'sprint7-charge-tb' 'a minted activation refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').sameTurn=$false; Reject $r 'sprint7-charge-tb' 'a charge leaving its turn refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').terminal.state.riderMove=0.0; Reject $r 'sprint7-charge-tb' 'a rider that paid less than the full round refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').terminal.state.mountMove=0.0; Reject $r 'sprint7-charge-tb' 'a mount that did not pay its travel refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').terminal.state.mountStandard=6.0; Reject $r 'sprint7-charge-tb' 'carrying that takes the mount''s action refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').terminal.state.partnerCanMove=$true; Reject $r 'sprint7-charge-tb' 'a mount left with more movement refuses'
$r=Copy-S7 $tb; $r.observations.sprint7Charge.attackD20IsForced=$true; Reject $r 'sprint7-charge-tb' 'a forced die on the turn-based charge refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').mountOwnAttackRules[0].charge=$true; Reject $r 'sprint7-charge-tb' 'a mount attack that inherits the charge refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').steps.nextTurn.buff=$true; (Facts $r 'S7-tb-charge').steps.nextTurn.buffCount=1; (Facts $r 'S7-tb-charge').steps.nextTurn.buffSecondsLeft=4.0; Reject $r 'sprint7-charge-tb' 'a penalty well into the next turn refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').secondTerminal.state.buffCount=2; Reject $r 'sprint7-charge-tb' 'a second charge duplicating the modifier refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').steps.secondCharge.clicked=$false; Reject $r 'sprint7-charge-tb' 'a refused next-round charge refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').steps.nextTurn.activationSequence=3; Reject $r 'sprint7-charge-tb' 'a skipped activation refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').steps.partnerSpentRefused.before.mountStandard=0.0; (Facts $r 'S7-tb-charge').steps.partnerSpentRefused.afterClick.mountStandard=0.0; Reject $r 'sprint7-charge-tb' 'a spent partner that was reset refuses'
$r=Copy-S7 $tb; (Facts $r 'S7-tb-charge').steps.partnerSpentRefused.clicked=$true; Reject $r 'sprint7-charge-tb' 'a charge admitted over a spent partner refuses'
$r=Copy-S7 $rt; $r.status='FAIL'; $r.subscenarioFailCount=1; try { Assert-KmcSprint7ChargeEvidence ([pscustomobject]@{scenario='sprint7-charge-rt'}) $r 'FAIL'; Check $true 'an explicit failed prefix is preserved without qualification' } catch { Check $false 'an explicit failed prefix is preserved without qualification' }
$r=Copy-S7 $rt; $r.status='FAIL'; $r.subscenarioFailCount=1; Reject $r 'sprint7-charge-rt' 'a failed artifact cannot be reported PASS'
Check ('mounted-charge-tb-committed-manual' -cin (Get-KmcChunk7PersistenceCases)) 'the committed-charge save case is registered'
Check (Test-KmcChunk7CombatPersistenceCase 'mounted-charge-tb-committed-manual') 'the committed-charge save case is a combat checkpoint'
$st=[pscustomobject]@{riderStandard=6.0;riderMove=3.0;mountStandard=0.0;mountMove=1.2;buffCount=1;chargeOwned=$false;activeCommand=$false;riderCharging=$false;mountCharging=$false;available=$false;turnUnit=$rider}
$obs=[pscustomobject]@{chargeCommit=[pscustomobject]@{clicked=$true;result='Success';sequenceLawful=$true;lease=[pscustomobject]@{buffHandedToNative=$true};rules=@([pscustomobject]@{charge=$true});after=$st};chargeCold=[pscustomobject]@{hasShell=$false;state=$st;castRequests=0};chargeRenewal=[pscustomobject]@{castRequests=0;rulesAfterLoad=@();state=[pscustomobject]@{riderStandard=0.0;riderMove=0.0;turnUnit=$rider}}}
function Persist($O,[bool]$Cold){ try{ $null=Assert-KmcSprint7ChargePersistence $O $null $Cold; $true }catch{ $false } }
Check (Persist $obs $false) 'a delivered charge before the save is accepted'
Check (Persist $obs $true) 'a clean cold load is accepted'
$o=Copy-S7 $obs; $o.chargeCold.state.riderStandard=0.0; Check (-not (Persist $o $true)) 'a cold refund refuses'
$o=Copy-S7 $obs; $o.chargeCold.state.buffCount=2; Check (-not (Persist $o $true)) 'a duplicated penalty after the load refuses'
$o=Copy-S7 $obs; $o.chargeCold.state.chargeOwned=$true; Check (-not (Persist $o $true)) 'a resumed charge refuses'
$o=Copy-S7 $obs; $o.chargeCold.state.available=$true; Check (-not (Persist $o $true)) 'a free charge after the load refuses'
$o=Copy-S7 $obs; $o.chargeRenewal.rulesAfterLoad=@([pscustomobject]@{charge=$true}); Check (-not (Persist $o $true)) 'a duplicate delivery after the load refuses'
$o=Copy-S7 $obs; $o.chargeCommit.lease.buffHandedToNative=$false; Check (-not (Persist $o $false)) 'a save with no native penalty refuses'
Write-Output ('Sprint 7 readers: PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
