# Sprint 6 reader: synthetic positive and negative checks. No game, no build. A reader-only assertion is never native evidence.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-S6($Value){ $Value | ConvertTo-Json -Depth 30 | ConvertFrom-Json }
$rider='11111111-1111-1111-1111-111111111111'; $mount='22222222-2222-2222-2222-222222222222'; $hostile='33333333-3333-3333-3333-333333333333'; $owner='44444444-4444-4444-4444-444444444444'; $pet='55555555-5555-5555-5555-555555555555'; $other='66666666-6666-6666-6666-666666666666'
function New-S6State($Spent=$false,$Owed=$false,$Window=$false){ [pscustomobject]@{spent=$Spent;owed=$Owed;window=$Window} }
$script:id=100
function Add-S6Attack($M,[string]$Case,[int]$Step,[string]$Plan,[string]$Refusal,[string]$Commitment,$Before,$After,[double]$SwiftBefore,[double]$SwiftAfter,[bool]$RiderTurn,[string]$Mode,[bool]$Opportunity=$true,[string]$Who=$rider,[string]$On=$mount){
 $script:id++
 $attempted=$Refusal -ceq 'None'
 $total=25;$check=$(if($Plan -ceq 'success'){26}elseif($Plan -ceq 'equal'){25}elseif($Plan -ceq 'low'){24}elseif($attempted){30}else{0})
 $negated=$attempted -and $check -gt $total
 $result=$(if($negated){'Parried'}else{'Hit'})
 $M.records+=[pscustomobject]@{case=$Case;step=$Step;plan=$Plan;ruleIdentity=$script:id;rider=$Who;mountId=$On;attacker=$hostile;mode=$Mode;attackD20=15;attackBonus=10;attackResult=$total;resultBefore='Hit';resultAfter=$result;criticalBefore=$false;criticalAfter=$false;opportunity=$Opportunity;riderTurn=$RiderTurn;refusal=$Refusal;commitment=$Commitment;attempted=$attempted;forced=($attempted -and $Plan -cne 'natural');negated=$negated;checkD20=$(if($attempted){10}else{0});checkResult=$check;checkDc=$(if($attempted){26}else{0});swiftBefore=$SwiftBefore;swiftAfter=$SwiftAfter;stateBefore=$Before;stateAfter=$After;mountSwiftBefore=6.0;mountSwiftAfter=6.0}
 $M.attackRules+=[pscustomobject]@{identity=$script:id;attacker=$hostile;target=$On;d20=15;bonus=10;autoHit=$false;opportunity=$Opportunity;attackerCommand=$(if($Opportunity){$null}else{'UnitAttack'});resultAtEvent=$result;isHitAtEvent=(-not $negated);criticalAtEvent=$false;sneakAtEvent=$false;resultAfterRule=$result;isHitAfterRule=(-not $negated);damageRules=$(if($negated){0}else{1});damage=$(if($negated){0}else{4})}
 if($attempted){ $M.skillChecks+=[pscustomobject]@{actor=$Who;d20=10;result=$check;dc=26;passed=$negated} }
}
function New-S6Envelope($Scenario,$M,$Rows){
 [pscustomobject]@{schemaVersion=50;status='PASS';errors=@();subscenarioFailCount=0;rows=$Rows;observations=[pscustomobject]@{riderId=$rider;horseId=$mount;sprint6Feat=$M}}
}
function New-S6Measure($Scenario){ [pscustomobject]@{contract='native-mounted-combat-feat-v1';rider=$rider;mount=$mount;cases=@(Get-KmcSprint6Cases $Scenario);checkD20IsControlled=$true;experienceIsDisclosedSetup=$true;records=@();attackRules=@();skillChecks=@()} }
function New-S6Choice($Selection){ [pscustomobject]@{selection=$Selection;committed=$true;fact=$true;eligibleBeforeRank=$false;selectAcceptedBeforeRank=$false;eligibleAfterRank=$true;offeredLast=$true;preview=[pscustomobject]@{ranks=0;total=20};previewAfterRank=[pscustomobject]@{ranks=1;total=24};after=[pscustomobject]@{ranks=1;total=4}} }
function New-S6Real {
 $m=New-S6Measure 'sprint6-feat-rt'; $free=New-S6State; $win=New-S6State $false $false $true
 $c='S6-rt-defense'
 Add-S6Attack $m $c 2 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-S6Attack $m $c 3 'natural' 'UsageSpent' 'None' $win $win 6 6 $false 'RT'
 Add-S6Attack $m $c 5 'equal' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-S6Attack $m $c 7 'low' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-S6Attack $m $c 9 'natural' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-S6Attack $m $c 11 'natural' 'PreferenceOff' 'None' $free $free 0 0 $false 'RT'
 Add-S6Attack $m $c 12 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT'
 $c='S6-rt-isolation'
 Add-S6Attack $m $c 3 'natural' 'RiderCannotAct' 'None' $free $free 0 0 $false 'RT'
 Add-S6Attack $m $c 5 'natural' 'RiderFlatFooted' 'None' $free $free 0 0 $false 'RT'
 Add-S6Attack $m $c 8 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT'
 Add-S6Attack $m $c 9 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $true $owner $pet
 $m.attackRules+=[pscustomobject]@{identity=9001;attacker=$hostile;target=$rider;autoHit=$false;opportunity=$true;resultAtEvent='Hit';isHitAtEvent=$true;sneakAtEvent=$false;damageRules=1;damage=3}
 $m.attackRules+=[pscustomobject]@{identity=9002;attacker=$rider;target=$hostile;autoHit=$false;opportunity=$false;resultAtEvent='Hit';isHitAtEvent=$true;sneakAtEvent=$false;damageRules=1;damage=2}
 $m.attackRules+=[pscustomobject]@{identity=9003;attacker=$hostile;target=$mount;autoHit=$false;opportunity=$true;resultAtEvent='Miss';isHitAtEvent=$false;sneakAtEvent=$false;damageRules=0;damage=0}
 $w=[pscustomobject]@{rider=[pscustomobject]@{window=$true;windowIdentity=7;windowEndTicks=70;charges=5;hasSwift=$false}}
 $sel=[pscustomobject]@{name='S6-selection';status='PASS';evidence=[pscustomobject]@{registration=[pscustomobject]@{failure=$null;ranks=1;groups=@('Feat','CombatFeat');prerequisite='SkillMobility=1'};selections=@([pscustomobject]@{name='BasicFeatSelection';lastIsFeat=$true;occurrences=1},[pscustomobject]@{name='FighterFeatSelection';lastIsFeat=$true;occurrences=1});ordinary=(New-S6Choice 'BasicFeatSelection');noCompanionAtChoice=$true;riderCombatChoice=(New-S6Choice 'FighterFeatSelection');riderToggle=[pscustomobject]@{present=$true;on=$true}}}
 $def=[pscustomobject]@{name='S6-rt-defense';status='PASS';evidence=[pscustomobject]@{steps=[pscustomobject]@{renewedByNativeWindow=[pscustomobject]@{rider=[pscustomobject]@{window=$false;hasSwift=$true}};naturalDie=$w;preferenceKeptUse=(Copy-S6 $w)}}}
 $iso=[pscustomobject]@{name='S6-rt-isolation';status='PASS';evidence=[pscustomobject]@{attemptsBeforeUnrelated=7;attemptsAfterUnrelated=7;unrelated=$other;stunApplied=$true;ownerAfterFirstRider=[pscustomobject]@{window=$false;spent=$false};ownerAfterOwnUse=[pscustomobject]@{window=$true;spent=$false};secondPair=[pscustomobject]@{rider=$owner;mount=$pet};beforeDismount=[pscustomobject]@{windowIdentity=9;windowEndTicks=90};afterDismount=[pscustomobject]@{windowIdentity=9;windowEndTicks=90};attemptsAtDismount=11;attemptsAfterDismountedStrike=11;afterRemount=[pscustomobject]@{windowIdentity=9;windowEndTicks=90}}}
 New-S6Envelope 'sprint6-feat-rt' $m @($sel,$def,$iso)
}
function New-S6Turn {
 $m=New-S6Measure 'sprint6-feat-tb'; $free=New-S6State; $spent=New-S6State $true; $owed=New-S6State $true $true
 $c='S6-tb-economy'
 Add-S6Attack $m $c 2 'success' 'None' 'SpendCurrentSwift' $free $spent 0 6 $true 'TB'
 Add-S6Attack $m $c 3 'natural' 'UsageSpent' 'None' $spent $spent 6 6 $true 'TB'
 Add-S6Attack $m $c 5 'natural' 'UsageSpent' 'None' $spent $spent 5 5 $false 'TB' $false
 Add-S6Attack $m $c 9 'equal' 'None' 'BorrowNextSwift' $free $owed 4 4 $false 'TB' $false
 Add-S6Attack $m $c 10 'natural' 'UsageSpent' 'None' $owed $owed 4 4 $false 'TB'
 Add-S6Attack $m $c 12 'natural' 'SwiftUnavailable' 'None' $free $free 6 6 $true 'TB'
 Add-S6Attack $m $c 14 'success' 'None' 'BorrowNextSwift' $free $owed 5 5 $false 'TB'
 $feat=[pscustomobject]@{name='S6-rider-feat';status='PASS';evidence=[pscustomobject]@{riderCombatChoice=(New-S6Choice 'FighterFeatSelection')}}
 $steps=[pscustomobject]@{renewedNoDebt=[pscustomobject]@{rider=[pscustomobject]@{spent=$false;owed=$false;hasSwift=$true;swift=0.0}};otherSwiftSpent=[pscustomobject]@{rider=[pscustomobject]@{hasSwift=$false}};offTurnEqual=[pscustomobject]@{rider=[pscustomobject]@{charges=3}};debtPaidAtPreparation=[pscustomobject]@{mountSwift=0.0;rider=[pscustomobject]@{spent=$false;owed=$false;hasSwift=$false;swiftGateBlocked=$true;swift=6.0;charges=4}}}
 New-S6Envelope 'sprint6-feat-tb' $m @($feat,[pscustomobject]@{name='S6-tb-economy';status='PASS';evidence=[pscustomobject]@{steps=$steps;payingTurnMove=[pscustomobject]@{sameTurn=$true;mounted=$true;metres=2.5}}})
}
function New-S6Forced {
 $m=New-S6Measure 'sprint6-feat-forced-rt'; $free=New-S6State; $win=New-S6State $false $false $true; $c='S6-forced-hit'; $ray='aaaa'
 Add-S6Attack $m $c 2 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-S6Attack $m $c 4 'equal' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-S6Attack $m $c 6 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT'
 $o=$m.records[-1]; $o.resultAfter='Miss'; $o | Add-Member abilityGuid $ray; $rule=$m.attackRules[-1]; $rule.resultAtEvent='Miss'; $rule.resultAfterRule='Miss'
 Add-S6Attack $m $c 8 'success' 'ForcedHit' 'None' $free $free 0 0 $false 'RT'
 $f=$m.records[-1]; $f | Add-Member abilityGuid $ray
 foreach($x in $m.records){ $x | Add-Member forcedHitContext $false }; $f.forcedHitContext=$true
 foreach($x in @($m.records[0],$m.records[1])){ $x | Add-Member abilityGuid $null }
 $settled=[pscustomobject]@{rider=[pscustomobject]@{spent=$false;owed=$false;window=$false;hasSwift=$true}}
 New-S6Envelope 'sprint6-feat-forced-rt' $m @([pscustomobject]@{name='S6-rider-feat';status='PASS';evidence=[pscustomobject]@{riderCombatChoice=(New-S6Choice 'FighterFeatSelection')}},[pscustomobject]@{name='S6-forced-hit';status='PASS';evidence=[pscustomobject]@{ability=[pscustomobject]@{name='RayOfFrost';guid=$ray};attemptsBeforeForced=3;attemptsAfterForced=3;chargesBeforeForced=3;chargesAfterForced=3;steps=[pscustomobject]@{forcedRaySettled=$settled}}})
}
function Accept($Artifact,[string]$Scenario){ try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario=$Scenario}) -Artifact $Artifact -Status 'PASS'; $true } catch { Write-Output ('      unexpected: '+$_.Exception.Message); $false } }
function Reject($Artifact,[string]$Scenario,[string]$Name){ $threw=$false; try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario=$Scenario}) -Artifact $Artifact -Status 'PASS' } catch { $threw=$true }; Check $threw $Name }
function RecordOf($Artifact,[string]$Case,[int]$Step){ @($Artifact.observations.sprint6Feat.records | Where-Object { $_.case -ceq $Case -and $_.step -eq $Step })[0] }
function RuleOf($Artifact,$Record){ @($Artifact.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $Record.ruleIdentity })[0] }
function RowOf($Artifact,[string]$Name){ @($Artifact.rows | Where-Object name -CEQ $Name)[0].evidence }

$real=New-S6Real; $turn=New-S6Turn; $d='S6-rt-defense'; $t='S6-tb-economy'
Check ((Accept $real 'sprint6-feat-rt')[-1] -eq $true) 'complete real-time evidence accepted'
Check ((Accept $turn 'sprint6-feat-tb')[-1] -eq $true) 'complete turn-based evidence accepted'
Check ((Get-KmcPhase3dEvidenceDispatch 50 'sprint6-feat-rt').validator -ceq 'Assert-KmcSprint6FeatEvidence') 'schema 50 dispatches to the Sprint 6 reader'
Check (Test-KmcPhase3dSchemaRegistration 50 'sprint6-feat-tb') 'schema 50 is registered for the turn-based scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 49 'sprint6-feat-rt')) 'a Sprint 5 schema is refused for a Sprint 6 scenario'
Check (-not (Test-KmcChildEntryExpectsMounted 'sprint6-feat-rt')) 'a Sprint 6 child starts from its unmounted fixture handoff'
Check (Test-KmcChildEntryRequiresIdleParty 'sprint6-feat-tb') 'a Sprint 6 child requires the idle party handoff'
Check ('mounted-combat-feat-tb-debt-manual' -cin (Get-KmcChunk7PersistenceCases)) 'the debt save case is registered'
Check (Test-KmcChunk7CombatPersistenceCase 'mounted-combat-feat-tb-debt-manual') 'the debt save case is a combat checkpoint'

# Selection
$r=Copy-S6 $real; (RowOf $r 'S6-selection').ordinary.eligibleBeforeRank=$true; Reject $r 'sprint6-feat-rt' 'zero ranks with a bonus accepted refuses'
$r=Copy-S6 $real; (RowOf $r 'S6-selection').ordinary.preview.total=0; Reject $r 'sprint6-feat-rt' 'a zero-rank control with no bonus refuses'
$r=Copy-S6 $real; (RowOf $r 'S6-selection').noCompanionAtChoice=$false; Reject $r 'sprint6-feat-rt' 'a choice made with a companion refuses'
$r=Copy-S6 $real; (RowOf $r 'S6-selection').riderCombatChoice.committed=$false; Reject $r 'sprint6-feat-rt' 'an uncommitted combat choice refuses'
$r=Copy-S6 $real; (RowOf $r 'S6-selection').selections=@((RowOf $r 'S6-selection').selections[0]); Reject $r 'sprint6-feat-rt' 'a missing fighter selection refuses'
$r=Copy-S6 $real; (RowOf $r 'S6-selection').selections[0].occurrences=2; Reject $r 'sprint6-feat-rt' 'a duplicate registration refuses'
$r=Copy-S6 $real; (RowOf $r 'S6-selection').riderToggle.present=$false; Reject $r 'sprint6-feat-rt' 'no player toggle refuses'
$r=Copy-S6 $real; (RowOf $r 'S6-selection').registration.prerequisite='SkillMobility=0'; Reject $r 'sprint6-feat-rt' 'a wrong prerequisite refuses'
# Hit defense
$r=Copy-S6 $real; $x=RecordOf $r $d 2; (RuleOf $r $x).damageRules=1; Reject $r 'sprint6-feat-rt' 'damage after a negation refuses'
$r=Copy-S6 $real; $x=RecordOf $r $d 2; (RuleOf $r $x).resultAtEvent='Hit'; Reject $r 'sprint6-feat-rt' 'a log event that saw the hit refuses'
$r=Copy-S6 $real; (RecordOf $r $d 2).resultAfter='Hit'; Reject $r 'sprint6-feat-rt' 'a success without the native result refuses'
$r=Copy-S6 $real; (RecordOf $r $d 5).negated=$true; Reject $r 'sprint6-feat-rt' 'equality that negates refuses'
$r=Copy-S6 $real; (RecordOf $r $d 5).checkResult=26; (RecordOf $r $d 5).negated=$false; Reject $r 'sprint6-feat-rt' 'a check that is not exact equality refuses'
$r=Copy-S6 $real; (RecordOf $r $d 7).resultAfter='Parried'; Reject $r 'sprint6-feat-rt' 'a failure that changed the attack refuses'
$r=Copy-S6 $real; (RuleOf $r (RecordOf $r $d 5)).damageRules=0; Reject $r 'sprint6-feat-rt' 'a standing hit with no damage refuses'
$r=Copy-S6 $real; $m=$r.observations.sprint6Feat; $m.skillChecks=@($m.skillChecks+$m.skillChecks[0]); Reject $r 'sprint6-feat-rt' 'a duplicate roll refuses'
$r=Copy-S6 $real; $m=$r.observations.sprint6Feat; $m.records[1].ruleIdentity=$m.records[0].ruleIdentity; Reject $r 'sprint6-feat-rt' 'one rule evaluated twice refuses'
$r=Copy-S6 $real; $m=$r.observations.sprint6Feat; $m.attackRules[-1].identity=$m.records[0].ruleIdentity; Reject $r 'sprint6-feat-rt' 'a miss that was evaluated refuses'
$r=Copy-S6 $real; $m=$r.observations.sprint6Feat; @($m.attackRules | Where-Object identity -EQ 9002)[0].identity=$m.records[0].ruleIdentity; @($m.attackRules | Where-Object { $_.identity -eq $m.records[0].ruleIdentity -and $_.target -ceq $mount })[0].identity=7777; Reject $r 'sprint6-feat-rt' 'an evaluated attack on a non-mount refuses'
$r=Copy-S6 $real; $m=$r.observations.sprint6Feat; @($m.attackRules | Where-Object identity -EQ 9002)[0].isHitAtEvent=$false; Reject $r 'sprint6-feat-rt' 'no unrelated hit refuses'
$r=Copy-S6 $real; $m=$r.observations.sprint6Feat; @($m.attackRules | Where-Object identity -EQ 9001)[0].isHitAtEvent=$false; Reject $r 'sprint6-feat-rt' 'no hit on the rider refuses'
$r=Copy-S6 $real; (RecordOf $r $d 2).attackD20=10; Reject $r 'sprint6-feat-rt' 'a comparison to something other than the attack result refuses'
$r=Copy-S6 $real; (RecordOf $r $d 9).negated=$false; Reject $r 'sprint6-feat-rt' 'a natural die against the strict rule refuses'
# Limits, renewal and debt
$r=Copy-S6 $real; (RecordOf $r $d 3).refusal='None'; Reject $r 'sprint6-feat-rt' 'a second use in the round refuses'
$r=Copy-S6 $real; (RowOf $r $d).steps.renewedByNativeWindow.rider.hasSwift=$false; Reject $r 'sprint6-feat-rt' 'no real-time renewal refuses'
$r=Copy-S6 $real; (RowOf $r $d).steps.preferenceKeptUse.rider.windowIdentity=8; Reject $r 'sprint6-feat-rt' 'a preference that reset the use refuses'
$r=Copy-S6 $turn; (RecordOf $r $t 2).commitment='BorrowNextSwift'; Reject $r 'sprint6-feat-tb' 'an own-turn use that borrowed refuses'
$r=Copy-S6 $turn; (RecordOf $r $t 2).swiftAfter=0; Reject $r 'sprint6-feat-tb' 'an own-turn use with no Swift cost refuses'
$r=Copy-S6 $turn; (RecordOf $r $t 9).refusal='SwiftUnavailable'; Reject $r 'sprint6-feat-tb' 'a Swift spent last turn treated as debt refuses'
$r=Copy-S6 $turn; (RecordOf $r $t 9).swiftAfter=9; Reject $r 'sprint6-feat-tb' 'an off-turn charge before the next turn refuses'
$r=Copy-S6 $turn; (RowOf $r $t).steps.debtPaidAtPreparation.rider.hasSwift=$true; Reject $r 'sprint6-feat-tb' 'a debt that left the Swift free refuses'
$r=Copy-S6 $turn; (RowOf $r $t).steps.debtPaidAtPreparation.rider.charges=5; Reject $r 'sprint6-feat-tb' 'a debt charged twice refuses'
$r=Copy-S6 $turn; (RowOf $r $t).steps.debtPaidAtPreparation.mountSwift=6.0; Reject $r 'sprint6-feat-tb' 'a mount that paid the debt refuses'
$r=Copy-S6 $turn; (RecordOf $r $t 14).commitment='None'; (RecordOf $r $t 14).refusal='SwiftOwed'; Reject $r 'sprint6-feat-tb' 'refusals only, with no positive renewal, refuse'
$r=Copy-S6 $turn; (RowOf $r $t).steps.renewedNoDebt.rider.spent=$true; Reject $r 'sprint6-feat-tb' 'no renewal at the next preparation refuses'
$r=Copy-S6 $turn; (RowOf $r $t).payingTurnMove.metres=0.0; Reject $r 'sprint6-feat-tb' 'a paying turn that could not move refuses'
$r=Copy-S6 $turn; $r.observations.sprint6Feat.records[0].mountSwiftAfter=12.0; Reject $r 'sprint6-feat-tb' 'mount resources in a record refuse'
$r=Copy-S6 $turn; foreach($row in @($r.observations.sprint6Feat.attackRules)){ $row.attackerCommand=$null }; Reject $r 'sprint6-feat-tb' 'no ordinary hostile attack in turn-based combat refuses'
# Isolation
$i='S6-rt-isolation'
$r=Copy-S6 $real; (RowOf $r $i).attemptsAfterUnrelated=8; Reject $r 'sprint6-feat-rt' 'an attack on the rider or an unrelated unit that was evaluated refuses'
$r=Copy-S6 $real; (RecordOf $r $i 3).refusal='UsageSpent'; Reject $r 'sprint6-feat-rt' 'an incapacitated rider with another refusal refuses'
$r=Copy-S6 $real; (RecordOf $r $i 5).refusal='None'; Reject $r 'sprint6-feat-rt' 'a flat-footed reaction refuses'
$r=Copy-S6 $real; (RowOf $r $i).ownerAfterFirstRider.window=$true; Reject $r 'sprint6-feat-rt' 'one rider spending another''s use refuses'
$r=Copy-S6 $real; (RecordOf $r $i 9).rider=$rider; Reject $r 'sprint6-feat-rt' 'a second pair that was not independent refuses'
$r=Copy-S6 $real; (RowOf $r $i).afterDismount.windowIdentity=0; Reject $r 'sprint6-feat-rt' 'a dismount that cleared the use refuses'
$r=Copy-S6 $real; (RowOf $r $i).attemptsAfterDismountedStrike=12; Reject $r 'sprint6-feat-rt' 'defending a former mount refuses'
$r=Copy-S6 $real; (RowOf $r $i).afterRemount.windowIdentity=10; Reject $r 'sprint6-feat-rt' 'a remount that minted a use refuses'
# Envelope
$threw=$false; $r=Copy-S6 $turn; $r.status='FAIL'; $r.subscenarioFailCount=1; try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario='sprint6-feat-tb'}) -Artifact $r -Status 'FAIL' } catch { $threw=$true }; Check (-not $threw) 'a failed prefix is preserved without qualification'
$threw=$false; try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario='sprint6-feat-tb'}) -Artifact $r -Status 'PASS' } catch { $threw=$true }; Check $threw 'a failed prefix cannot become PASS'
# Forced hit
$forced=New-S6Forced; $fc='S6-forced-hit'; $fs='sprint6-feat-forced-rt'
$acc=@(Accept $forced $fs); $acc | Where-Object { $_ -is [string] } | ForEach-Object { Write-Output $_ }; Check ($acc[-1] -eq $true) 'complete forced-hit evidence accepted'
Check (Test-KmcPhase3dSchemaRegistration 50 $fs) 'schema 50 is registered for the forced-hit scenario'
$r=Copy-S6 $forced; (RecordOf $r $fc 8).forcedHitContext=$false; Reject $r $fs 'a refusal without the native flag refuses'
$r=Copy-S6 $forced; (RecordOf $r $fc 8).refusal='None'; (RecordOf $r $fc 8).attempted=$true; Reject $r $fs 'a forced hit that committed refuses'
$r=Copy-S6 $forced; (RecordOf $r $fc 8).swiftAfter=6.0; Reject $r $fs 'a forced hit that charged Swift refuses'
$r=Copy-S6 $forced; (RecordOf $r $fc 8).stateAfter.window=$true; Reject $r $fs 'a forced hit that spent the use refuses'
$r=Copy-S6 $forced; (RuleOf $r (RecordOf $r $fc 8)).damageRules=0; Reject $r $fs 'a forced hit that dealt no damage refuses'
$r=Copy-S6 $forced; (RowOf $r $fc).attemptsAfterForced=4; Reject $r $fs 'a forced hit that rolled a check refuses'
$r=Copy-S6 $forced; (RowOf $r $fc).chargesAfterForced=4; Reject $r $fs 'a forced hit that charged the native writer refuses'
$r=Copy-S6 $forced; (RowOf $r $fc).steps.forcedRaySettled.rider.hasSwift=$false; Reject $r $fs 'a rider that lost its reaction to a forced hit refuses'
$r=Copy-S6 $forced; (RecordOf $r $fc 6).refusal='ForcedHit'; (RecordOf $r $fc 6).attempted=$false; Reject $r $fs 'an ordinary projectile that was refused refuses'
$r=Copy-S6 $forced; (RecordOf $r $fc 6).forcedHitContext=$true; Reject $r $fs 'an ordinary cast carrying the flag refuses'
$r=Copy-S6 $forced; (RecordOf $r $fc 6).resultAfter='Hit'; Reject $r $fs 'an ordinary projectile left as a hit refuses'
$r=Copy-S6 $forced; (RuleOf $r (RecordOf $r $fc 6)).damageRules=1; Reject $r $fs 'damage after a negated projectile refuses'
$r=Copy-S6 $forced; (RecordOf $r $fc 4).negated=$true; Reject $r $fs 'melee equality that negates refuses'
# Persistence
$goodSave=[pscustomobject]@{featDebt=[pscustomobject]@{commitment='BorrowNextSwift';riderTurn=$false;state=(New-S6State $true $true)}}
$good=[pscustomobject]@{featCold=[pscustomobject]@{hasFeat=$true;preferenceOn=$true;state=(New-S6State $true $true)};featRenewal=[pscustomobject]@{afterPreparation=[pscustomobject]@{spent=$false;owed=$false;hasSwift=$false;swift=6.0};chargesAtPreparation=1;commitment='BorrowNextSwift';attempted=$true;riderTurn=$false;stateAfter=(New-S6State $true $true)}}
function Persist($Value,[bool]$Cold){ try { Assert-KmcSprint6FeatPersistence ([pscustomobject]@{}) $Value $Cold; $true } catch { $false } }
Set-StrictMode -Version Latest
Check (Persist $goodSave $false) 'a committed save is accepted'
Check (Persist $good $true) 'a cold load without refund and with renewal is accepted'
$v=Copy-S6 $goodSave; $v.featDebt.state.owed=$false; Check (-not (Persist $v $false)) 'a save with no debt refuses'
$v=Copy-S6 $good; $v.featCold.state.owed=$false; Check (-not (Persist $v $true)) 'a load that refunded the debt refuses'
$v=Copy-S6 $good; $v.featCold.state.spent=$false; Check (-not (Persist $v $true)) 'a load that refunded the use refuses'
$v=Copy-S6 $good; $v.featRenewal.chargesAtPreparation=2; Check (-not (Persist $v $true)) 'a restored debt charged twice refuses'
$v=Copy-S6 $good; $v.featRenewal.afterPreparation.hasSwift=$true; Check (-not (Persist $v $true)) 'a restored debt left unpaid refuses'
$v=Copy-S6 $good; $v.featRenewal.attempted=$false; Check (-not (Persist $v $true)) 'no renewal after the load refuses'
Write-Output ('Sprint 6 reader checks: '+$script:pass+' passed, '+$script:fail+' failed')
if($script:fail -ne 0){ throw 'Sprint 6 reader checks failed.' }
