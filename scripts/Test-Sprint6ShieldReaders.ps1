# Mounted Shield reader (scripts/runtime/Sprint6ShieldEvidence.ps1 through the Sprint 6 reader): synthetic positive and
# negative checks. No game and no build. A reader-only assertion is never native evidence: it shows which observations the
# reader accepts and which it refuses, so a native run cannot pass on a wrong amount, a stacked or touch-leaking AC, a check
# that tested a total other than the shield-modified equality, a leak between pairs, or residue after teardown.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-MS($Value){ $Value | ConvertTo-Json -Depth 40 | ConvertFrom-Json }
$rider='11111111-1111-1111-1111-111111111111'; $mount='22222222-2222-2222-2222-222222222222'; $hostile='33333333-3333-3333-3333-333333333333'; $owner='44444444-4444-4444-4444-444444444444'; $pet='55555555-5555-5555-5555-555555555555'
$feat='671a301bd1774677a8bfa2873d33b191'; $focus='ac57069b6bf8c904086171683992a92a'; $combat='a7d2e94c6f1b4830b5c9e28d17f3a604'
function New-State($Spent=$false,$Owed=$false,$Window=$false){ [pscustomobject]@{spent=$Spent;owed=$Owed;window=$Window;windowIdentity=$(if($Window){7}else{0})} }
$script:id=100
function Add-Attack($M,[string]$Case,[int]$Step,[string]$Plan,[string]$Refusal,[string]$Commitment,$Before,$After,[double]$SwiftBefore,[double]$SwiftAfter,[bool]$RiderTurn,[string]$Mode,[bool]$Opportunity=$true){
 $script:id++
 $attempted=$Refusal -ceq 'None'
 $total=25; $stat=10; $bonus=$(if($attempted){3}else{0})
 $check=$(if(-not $attempted){0}elseif($Plan -ceq 'success'){26}elseif($Plan -ceq 'equal'){25}else{30})
 $negated=$attempted -and $check -gt $total
 $result=$(if($negated){'Parried'}else{'Hit'})
 $M.records+=[pscustomobject]@{case=$Case;step=$Step;plan=$Plan;ruleIdentity=$script:id;rider=$rider;mountId=$mount;attacker=$hostile;mode=$Mode;attackD20=15;attackBonus=10;attackResult=$total;resultBefore='Hit';resultAfter=$result;criticalBefore=$false;criticalAfter=$false;opportunity=$Opportunity;riderTurn=$RiderTurn;abilityGuid=$null;forcedHitContext=$false;refusal=$Refusal;commitment=$Commitment;attempted=$attempted;forced=($attempted -and $Plan -cne 'natural');negated=$negated;checkD20=$(if($attempted){$check-$stat-$bonus}else{0});checkStat=$(if($attempted){$stat}else{0});checkBonus=$bonus;checkResult=$check;checkDc=$(if($attempted){$total+1}else{0});shieldBonus=$bonus;swiftBefore=$SwiftBefore;swiftAfter=$SwiftAfter;stateBefore=$Before;stateAfter=$After;mountSwiftBefore=0;mountSwiftAfter=0;mountDamageAtRecord=0}
 $M.attackRules+=[pscustomobject]@{identity=$script:id;attacker=$hostile;target=$mount;d20=15;bonus=10;ac=24;autoHit=$false;opportunity=$Opportunity;charge=$false;attackerCommand=$(if($Opportunity){$null}else{'UnitAttack'});resultAtEvent=$result;isHitAtEvent=(-not $negated);criticalAtEvent=$false;sneakAtEvent=$false;resultAfterRule=$result;isHitAfterRule=(-not $negated);damageRules=$(if($negated){0}else{1});damage=$(if($negated){0}else{4})}
 if($attempted){ $M.skillChecks+=[pscustomobject]@{actor=$rider;d20=$check-$stat-$bonus;stat=$stat;bonus=$bonus;result=$check;dc=$total+1;passed=$negated;frame=$script:id;kmcShieldModifiers=1;kmcShieldTotal=3} }
}
function New-Measure($Scenario){ [pscustomobject]@{contract='native-mounted-combat-feat-v1';rider=$rider;mount=$mount;cases=@(Get-KmcSprint6Cases $Scenario);checkD20IsControlled=$true;experienceIsDisclosedSetup=$true;records=@();attackRules=@();skillChecks=@()
  mountedShield=[pscustomobject]@{contract='native-mounted-shield-v1';featGuid=$feat;setupDisclosure='disclosed'
  shieldBlueprints=[pscustomobject]@{mundane=[pscustomobject]@{buckler=(New-Stock);light=(New-Stock);heavy=(New-Stock);tower=(New-Stock)};enhancedCandidates=@((New-Stock),(New-Stock))}
  drain=[pscustomobject]@{attempts=1;setup=[pscustomobject]@{faults=@();remaining=@()};buffsLeft=@();rider=(New-Restored);owner=(New-Restored);clean=$true;final=$true}}} }
function New-Stock { [pscustomobject]@{guid='a';name='HeavyShieldItem';instanceId=1200;armorType='d1b05b901bab9524388ebfa0435902a6';stockLoaded=$true} }
function New-Restored { [pscustomobject]@{errors=@();createdLeft=0;handsChecked=4;set=0;originalSet=0} }
function New-Envelope($M,$Rows){ [pscustomobject]@{schemaVersion=50;status='PASS';errors=@();subscenarioFailCount=0;rows=$Rows;observations=[pscustomobject]@{riderId=$rider;horseId=$mount;sprint6Feat=$M}} }

# Snapshots: the shieldless reference is AC 20, touch 12, flat-footed 18, flat-footed touch 10.
function New-Snap([int]$Amount,[int]$HighestShield=-1,[string]$Who=$rider,[string]$On=$mount,[int]$Base=2,[bool]$Greater=$false,[int]$Set=0,[object[]]$Others=@(),[int]$Enhancement=0){
 if($HighestShield -lt 0){ $HighestShield=$Amount }
 $owned=@(); $mods=@()
 if($Amount -gt 0){ $owned=@([pscustomobject]@{value=$Amount;descriptor='Shield';sourceGuid=$feat;sourceIsRiderFact=$true;sourceName='Mounted Shield'}); $mods+=[pscustomobject]@{value=$Amount;kmc=$true;item=$false;sourceGuid=$feat} }
 foreach($o in $Others){ $mods+=[pscustomobject]@{value=$o;kmc=$false;item=$false;sourceGuid=$null} }
 [pscustomobject]@{rider=$Who;mount=$On;frame=1;amount=$Amount;serviceEnabled=$true
  facts=[pscustomobject]@{available=($Amount -gt 0);pair=$true;ownsFeat=$true;effectiveShield=($Amount -gt 0);base=$(if($Amount -gt 0){$Base}else{0});focus=$true;greater=$Greater}
  shield=$(if($Amount -gt 0){[pscustomobject]@{enhancement=$Enhancement;base=$Base}}else{$null})
  ac=20+$HighestShield;touch=12;flatFooted=18+$HighestShield;flatFootedTouch=10;ruleMelee=20+$HighestShield;ruleTouch=12
  owned=$owned;shieldModifiers=$mods;ledger=$(if($Amount -gt 0){[pscustomobject]@{mount=$On}}else{$null})
  riderMobility=12;riderMobilityKmc=0;riderShieldModifiers=@();riderSet=$Set;mounted=$true;state=(New-State)}
}
function New-Eligibility([string]$Moment,[string]$Selection,[bool]$Combat,[bool]$Focus){ [pscustomobject]@{moment=$Moment;selection=$Selection;offered=$true;selected=$false;mountedCombat=$Combat;shieldFocus=$Focus;eligible=($Combat -and $Focus);iconIsShieldIcon=$true} }
function New-Facts([int]$C,[int]$F,[int]$S){ [pscustomobject]@{unit=$rider;mountedCombat=$C;shieldFocus=$F;mountedShield=$S;mountedShieldRank=$S} }
function New-Selection([bool]$RealTime){
 $pairElig=@((New-Eligibility 'start' 'FighterFeatSelection' $false $false),(New-Eligibility 'after-MountedCombatFeature' 'BasicFeatSelection' $true $false),(New-Eligibility 'after-ShieldFocus' 'BasicFeatSelection' $true $true),(New-Eligibility 'back-MountedCombatFeature' 'BasicFeatSelection' $false $true),(New-Eligibility 'reselected' 'BasicFeatSelection' $true $true))
 $pairLevel=[pscustomobject]@{committed=$true;characterLevelBefore=5;characterLevelAfter=6;characterLevelAfterCancel=5;factsBefore=(New-Facts 0 0 0);factsAfterCancel=(New-Facts 0 0 0);factsAfter=(New-Facts 1 1 0)
  cancelled=[pscustomobject]@{eligibility=$pairElig;previewFacts=[pscustomobject]@{mountedCombat=$true;shieldFocus=$true;mountedShield=$false}}
  pass=[pscustomobject]@{choices=@('skill:SkillMobility','selection:FighterFeatSelection=MountedCombatFeature','selection:BasicFeatSelection=ShieldFocus','reselection:FighterFeatSelection=MountedCombatFeature');eligibility=$pairElig
   backOut=[pscustomobject]@{withdrawn='MountedCombatFeature';stillSelected=$true;previewHasWithdrawn=$false;previewHasOther=$true};afterBack=[pscustomobject]@{mountedCombat=$true;shieldFocus=$true;bothChosen=$true};previewFacts=[pscustomobject]@{mountedCombat=$true;shieldFocus=$true;mountedShield=$false}}}
 $shieldElig=@((New-Eligibility 'start' 'BasicFeatSelection' $true $true))
 $shieldLevel=[pscustomobject]@{committed=$true;characterLevelBefore=6;characterLevelAfter=7;characterLevelAfterCancel=6;factsBefore=(New-Facts 1 1 0);factsAfterCancel=(New-Facts 1 1 0);factsAfter=(New-Facts 1 1 1)
  cancelled=[pscustomobject]@{eligibility=$shieldElig;previewFacts=[pscustomobject]@{mountedCombat=$true;shieldFocus=$true;mountedShield=$true}}
  pass=[pscustomobject]@{choices=@('selection:BasicFeatSelection=MountedShieldFeature');eligibility=$shieldElig;previewFacts=[pscustomobject]@{mountedCombat=$true;shieldFocus=$true;mountedShield=$true}}}
 $e=[pscustomobject]@{
  shieldRegistration=[pscustomobject]@{failure=$null;withdrawn=$false;guid=$feat;groups=@('Feat','CombatFeat');ranks=1;isClassFeature=$true;hideInUi=$false
   prerequisites=@([pscustomobject]@{type='PrerequisiteFeature';group='All';hidden=$false;feature=$combat},[pscustomobject]@{type='PrerequisiteFeature';group='All';hidden=$false;feature=$focus},[pscustomobject]@{type='MountedShieldAvailabilityPrerequisite';group='All';hidden=$true;feature=$null})
   iconIsShieldIcon=$true;iconSprite='KMC_MountedShield_Feat_Icon';iconWidth=64;iconHeight=64;name='Mounted Shield';description='A shield bonus to the mount and to the Mounted Combat check; enhancement does not count.'}
  threeFeatSelections=@([pscustomobject]@{guid='a';combat=1;archery=1;shield=1;kmcTailExact=$true},[pscustomobject]@{guid='b';combat=1;archery=1;shield=1;kmcTailExact=$true})
  levels=@($pairLevel,$shieldLevel); riderFacts=(New-Facts 1 1 1)}
 if($RealTime){
  $e | Add-Member ownerLevels @([pscustomobject]@{committed=$true;pass=[pscustomobject]@{eligibility=@((New-Eligibility 'after-ShieldFocus' 'FighterFeatSelection' $false $true))}})
  $e | Add-Member ownerCompanion ([pscustomobject]@{acquired=$true;experienceBefore=5000;experienceSetup=23000})
  $e | Add-Member ownerSetupFacts ([pscustomobject]@{unit=$owner;mountedCombat=1;shieldFocus=1;mountedShield=1;mountedShieldRank=1})
 }
 $e
}
function New-Equipment {
 $rs=New-Snap 0; $rs.riderShieldModifiers=@([pscustomobject]@{value=4;item=$false})
 $u=New-Snap 0; $u | Add-Member shieldInInventory $true
 $steps=[pscustomobject]@{none=(New-Snap 0);buckler=(New-Snap 2 -Base 1);light=(New-Snap 2 -Base 1);heavy=(New-Snap 3);tower=(New-Snap 5 -Base 4);heavyEnhanced0=(New-Snap 3 -Enhancement 3);heavyEnhanced1=(New-Snap 3 -Enhancement 5)
  heavyAgain=(New-Snap 3);greater=(New-Snap 4 -Greater $true);greaterRemoved=(New-Snap 3);competing2=(New-Snap 3 3 -Others @(2));competing3=(New-Snap 3 3 -Others @(3));competing4=(New-Snap 3 4 -Others @(4))
  mountSpell=(New-Snap 3 4 -Others @(4));otherSet=(New-Snap 0 -Set 1);setRestored=(New-Snap 3);unequipped=$u;riderSpellOnly=$rs;final=(New-Snap 3)}
 [pscustomobject]@{steps=$steps;shieldSpell='9c0fa9b438ada3f43864be8dd8b3e741/MageShieldBuff';equipment=[pscustomobject]@{shieldSet=0}}
}
function New-Pairs {
 $second={ New-Snap 2 -Who $owner -On $pet -Base 1 }
 $cross1=New-Snap 0 -On $pet; $cross1.owned=@([pscustomobject]@{value=2;descriptor='Shield';sourceGuid=$feat;sourceIsRiderFact=$false})
 $cross2=New-Snap 0 -Who $owner; $cross2.owned=@([pscustomobject]@{value=3;descriptor='Shield';sourceGuid=$feat;sourceIsRiderFact=$false})
 $dis=New-Snap 0; $dis.mounted=$false
 $removed=New-Snap 0; $removed.facts.ownsFeat=$false
 $off1=New-Snap 0; $off1.serviceEnabled=$false; $off2=New-Snap 0 -Who $owner -On $pet; $off2.serviceEnabled=$false
 [pscustomobject]@{steps=[pscustomobject]@{bothFirst=(New-Snap 3);bothSecond=(& $second);uiSelectedSecondFirst=(New-Snap 3);uiSelectedSecondSecond=(& $second);dismountedFirst=$dis;dismountedSecond=(& $second)
   remounted=(New-Snap 3);featRemoved=$removed;featRemovedSecond=(& $second);featReacquired=(New-Snap 3);serviceDisabledFirst=$off1;serviceDisabledSecond=$off2;serviceEnabledFirst=(New-Snap 3);serviceEnabledSecond=(& $second)}
  crossFirstToSecondMount=$cross1;crossSecondToFirstMount=$cross2;serviceDisableAccepted=$true}
}
function New-Real {
 $m=New-Measure 'sprint6-shield-rt'; $free=New-State; $win=New-State $false $false $true; $c='MS-check-rt'
 Add-Attack $m $c 2 'equal' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-Attack $m $c 4 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-Attack $m $c 5 'natural' 'UsageSpent' 'None' $win $win 6 6 $false 'RT'
 Add-Attack $m $c 7 'natural' 'PreferenceOff' 'None' $free $free 0 0 $false 'RT'
 Add-Attack $m $c 9 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT'
 # The unrelated Mobility check, and a miss inside the shield's band (22 against AC 24 with +3).
 $m.skillChecks+=[pscustomobject]@{actor=$rider;d20=8;stat=12;bonus=0;result=20;dc=10;passed=$true;frame=900;kmcShieldModifiers=0;kmcShieldTotal=0}
 $script:id++; $bandId=$script:id; $m.attackRules+=[pscustomobject]@{identity=$script:id;attacker=$hostile;target=$mount;d20=10;bonus=12;ac=24;autoHit=$false;opportunity=$false;charge=$false;attackerCommand=$null;resultAtEvent='Miss';isHitAtEvent=$false;criticalAtEvent=$false;sneakAtEvent=$false;resultAfterRule='Miss';isHitAfterRule=$false;damageRules=0;damage=0}
 $check=[pscustomobject]@{steps=[pscustomobject]@{combatStart=(New-Snap 3);equal=(New-Snap 3);success=(New-Snap 3);spentRefused=(New-Snap 3);preferenceOffRefused=(New-Snap 3);competingBeforeCheck=(New-Snap 3 4 -Others @(4));competingCheck=(New-Snap 3 4 -Others @(4));bandDone=(New-Snap 3)}
  bandMiss=[pscustomobject]@{identity=$bandId;attacker=$hostile;target=$mount;d20=10;bonus=12;ac=24;isHitAtEvent=$false}
  equipmentDuringUse=[pscustomobject]@{before=$win;unequipped=$win;reequipped=$win};unrelatedMobility=[pscustomobject]@{stat=12;bonus=0;displayed=12;dc=10;result=20;frame=900;contributions=2}
  band=[pscustomobject]@{amount=3;ac=24;otherShield=0;effectiveDelta=3;acWithoutShield=21;attackBonusBefore=30;adjustment=-18};bandAttempts=9}
 New-Envelope $m @([pscustomobject]@{name='MS-selection';status='PASS';evidence=(New-Selection $true)},[pscustomobject]@{name='MS-equipment';status='PASS';evidence=(New-Equipment)},
  [pscustomobject]@{name='MS-pairs';status='PASS';evidence=(New-Pairs)},[pscustomobject]@{name='MS-check-rt';status='PASS';evidence=$check})
}
function New-Turn {
 $m=New-Measure 'sprint6-shield-tb'; $free=New-State; $spent=New-State $true; $owed=New-State $true $true; $c='MS-check-tb'
 Add-Attack $m $c 2 'success' 'None' 'SpendCurrentSwift' $free $spent 0 6 $true 'TB'
 Add-Attack $m $c 3 'natural' 'UsageSpent' 'None' $spent $spent 6 6 $true 'TB'
 Add-Attack $m $c 6 'equal' 'None' 'BorrowNextSwift' $free $owed 0 0 $false 'TB' $false
 $check=[pscustomobject]@{steps=[pscustomobject]@{ownTurnBefore=(New-Snap 3);ownTurnNegated=(New-Snap 3);ownTurnSecondRefused=(New-Snap 3);renewed=(New-Snap 3);offTurnEqual=(New-Snap 3)}}
 New-Envelope $m @([pscustomobject]@{name='MS-rider-feat';status='PASS';evidence=(New-Selection $false)},[pscustomobject]@{name='MS-check-tb';status='PASS';evidence=$check})
}
function Accept($Artifact,[string]$Scenario){ try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario=$Scenario}) -Artifact $Artifact -Status 'PASS'; $true } catch { Write-Output ('      reader: '+$_.Exception.Message); $false } }
function Reject($Artifact,[string]$Scenario,[string]$Name){ $threw=$false; try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario=$Scenario}) -Artifact $Artifact -Status 'PASS' } catch { $threw=$true }; Check $threw $Name }
function RowOf($Artifact,[string]$Name){ @($Artifact.rows | Where-Object name -CEQ $Name)[0].evidence }
function RecordOf($Artifact,[string]$Case,[int]$Step){ @($Artifact.observations.sprint6Feat.records | Where-Object { $_.case -ceq $Case -and $_.step -eq $Step })[0] }
function CheckOf($Artifact,$Record){ @($Artifact.observations.sprint6Feat.skillChecks | Where-Object { [int]$_.frame -eq [int]$Record.ruleIdentity })[0] }

$real=New-Real; $turn=New-Turn; $rs='sprint6-shield-rt'; $ts='sprint6-shield-tb'
$ra=@(Accept $real $rs); if($ra.Count -gt 1){ $ra[0..($ra.Count-2)] | ForEach-Object { Write-Output $_ } }; Check ($ra[-1] -eq $true) 'complete real-time Mounted Shield evidence accepted'
$ta=@(Accept $turn $ts); if($ta.Count -gt 1){ $ta[0..($ta.Count-2)] | ForEach-Object { Write-Output $_ } }; Check ($ta[-1] -eq $true) 'complete turn-based Mounted Shield evidence accepted'
Check ((Get-KmcPhase3dEvidenceDispatch 50 $rs).validator -ceq 'Assert-KmcSprint6FeatEvidence') 'schema 50 dispatches the real-time shield scenario to the Sprint 6 reader'
Check (Test-KmcPhase3dSchemaRegistration 50 $ts) 'schema 50 is registered for the turn-based shield scenario'
Check (-not (Test-KmcPhase3dSchemaRegistration 51 $rs)) 'another schema is refused for a shield scenario'
Check (-not (Test-KmcChildEntryExpectsMounted $rs)) 'a shield child starts from its unmounted fixture handoff'
Check ((@(Get-KmcSaveBackedRuntimeScenarios) -ccontains $rs) -and (@(Get-KmcSaveBackedRuntimeScenarios) -ccontains $ts)) 'both shield scenarios are save-backed (Working fixture only)'

# The check boundary: equality of the shield-modified total, never Mobility alone.
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 2; $x.checkResult=28; $x.checkD20=15; (CheckOf $r $x).result=28; Reject $r $rs 'an "equal" row three above equality (Mobility-only die) refuses'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 2; $x.shieldBonus=0; $x.checkBonus=0; $x.checkD20=15; Reject $r $rs 'a check without the shield addition refuses'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; $c=CheckOf $r $x; $c.kmcShieldModifiers=2; $c.kmcShieldTotal=6; Reject $r $rs 'the addition counted twice in the native roll refuses'
$r=Copy-MS $real; @($r.observations.sprint6Feat.skillChecks | Where-Object frame -EQ 900)[0].kmcShieldModifiers=1; Reject $r $rs 'a Mounted Shield modifier in an unrelated Mobility roll refuses'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 2; $x.negated=$true; Reject $r $rs 'equality that negates refuses'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; $x.forced=$false; Reject $r $rs 'an unlabeled boundary roll refuses'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').steps.preferenceOffRefused=(New-Snap 0); Reject $r $rs 'passive AC lost with the preference off refuses'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').steps.spentRefused=(New-Snap 0); Reject $r $rs 'passive AC lost while the use is spent refuses'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').equipmentDuringUse.unequipped=(New-State); Reject $r $rs 'an equipment change that refunded the use refuses'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').unrelatedMobility.bonus=3; Reject $r $rs 'an unrelated Mobility check with the addition refuses'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; $m.attackRules[-1].d20=5; Reject $r $rs 'no miss inside the shield band refuses'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; $m.records[2].ruleIdentity=$m.attackRules[-1].identity; Reject $r $rs 'a miss caused by the shield that was evaluated refuses'
# Equipment, amount and native AC.
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavyEnhanced1=(New-Snap 8 -Enhancement 5); Reject $r $rs 'an enhancement that passed through refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.buckler=(New-Snap 2 -Base 2); Reject $r $rs 'a base other than the stock buckler data refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.greater=(New-Snap 5 -Greater $true); Reject $r $rs 'Greater Shield Focus counted twice refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavy.touch=15; Reject $r $rs 'a shield bonus in touch AC refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavy.flatFooted=18; Reject $r $rs 'a flat-footed AC without the shield bonus refuses'
$r=Copy-MS $real; $s=(RowOf $r 'MS-equipment').steps.competing4; $s.ac=27; $s.flatFooted=25; $s.ruleMelee=27; Reject $r $rs 'shield bonuses that stacked refuse'
$r=Copy-MS $real; $s=(RowOf $r 'MS-equipment').steps.competing4; $s.owned=@(); $s.amount=0; Reject $r $rs 'no check amount when a stronger shield covers the AC refuses'
$r=Copy-MS $real; $s=(RowOf $r 'MS-equipment').steps.heavy; $s.owned=@($s.owned[0],$s.owned[0]); Reject $r $rs 'two contributions on one mount refuse'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavy.owned[0].sourceIsRiderFact=$false; Reject $r $rs 'a contribution not owned by the rider''s fact refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.otherSet=(New-Snap 3 -Set 1); Reject $r $rs 'a shield kept in an inactive weapon set refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.unequipped=(New-Snap 3); Reject $r $rs 'a contribution after unequipping refuses'
$r=Copy-MS $real; $s=(RowOf $r 'MS-equipment').steps.riderSpellOnly; $s.amount=4; $s.owned=@([pscustomobject]@{value=4;descriptor='Shield';sourceGuid=$feat;sourceIsRiderFact=$true}); Reject $r $rs 'a rider Shield spell counted as a physical shield refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavy.riderMobilityKmc=1; Reject $r $rs 'a persistent Mobility modifier refuses'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavy.ledger=$null; Reject $r $rs 'a contribution without its ledger refuses'
# Pairs and lifecycle.
$r=Copy-MS $real; (RowOf $r 'MS-pairs').crossFirstToSecondMount.amount=3; Reject $r $rs 'a rider''s amount on another pair''s mount refuses'
$r=Copy-MS $real; (RowOf $r 'MS-pairs').steps.uiSelectedSecondFirst=(New-Snap 2); Reject $r $rs 'a UI selection that changed the contribution refuses'
$r=Copy-MS $real; (RowOf $r 'MS-pairs').steps.dismountedFirst=(New-Snap 3); Reject $r $rs 'a contribution after dismount refuses'
$r=Copy-MS $real; (RowOf $r 'MS-pairs').steps.remounted.owned+=(RowOf $r 'MS-pairs').steps.remounted.owned[0]; Reject $r $rs 'a doubled contribution after remount refuses'
$r=Copy-MS $real; (RowOf $r 'MS-pairs').steps.featRemoved=(New-Snap 3); Reject $r $rs 'a contribution after the feat was removed refuses'
$r=Copy-MS $real; $s=(RowOf $r 'MS-pairs').steps.serviceDisabledSecond; $s.amount=2; $s.owned=@([pscustomobject]@{value=2;descriptor='Shield';sourceGuid=$feat;sourceIsRiderFact=$true}); Reject $r $rs 'a contribution while the service is disabled refuses'
$r=Copy-MS $real; (RowOf $r 'MS-pairs').steps.serviceEnabledFirst=(New-Snap 0); Reject $r $rs 'no contribution after re-enable refuses'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.rider.createdLeft=1; Reject $r $rs 'fixture shields left after the drain refuse'
# Selection, presentation and publication.
$r=Copy-MS $real; (RowOf $r 'MS-selection').levels[0].pass.eligibility[1].eligible=$true; Reject $r $rs 'eligible without Shield Focus refuses'
$r=Copy-MS $real; $e=RowOf $r 'MS-selection'; $e.levels[0].pass.eligibility=@($e.levels[0].pass.eligibility | Where-Object { $_.moment -cne 'back-MountedCombatFeature' }); $e.levels[0].cancelled.eligibility=$e.levels[0].pass.eligibility; $e.ownerLevels[0].pass.eligibility[0].shieldFocus=$false; Reject $r $rs 'no native refusal without Mounted Combat refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').levels[1].factsAfterCancel=(New-Facts 1 1 1); Reject $r $rs 'a cancelled level that kept the feat refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').levels[0].pass.PSObject.Properties.Remove('backOut'); Reject $r $rs 'no Back step refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').levels[0].pass.choices=@('selection:FighterFeatSelection=MountedCombatFeature'); Reject $r $rs 'prerequisites not chosen at one level refuse'
$r=Copy-MS $real; (RowOf $r 'MS-selection').shieldRegistration.iconWidth=512; Reject $r $rs 'an icon that is not 64x64 refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').shieldRegistration.prerequisites[1].feature='other'; Reject $r $rs 'a prerequisite other than Shield Focus refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').shieldRegistration.isClassFeature=$false; Reject $r $rs 'a feat that lost the donor IsClassFeature refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').threeFeatSelections[0].shield=2; Reject $r $rs 'a feat offered twice in one list refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').riderFacts.mountedShield=2; Reject $r $rs 'a duplicated feat refuses'
$r=Copy-MS $real; (RowOf $r 'MS-selection').ownerLevels[0].pass.eligibility[0].eligible=$true; Reject $r $rs 'the second rider eligible without Mounted Combat refuses'
# Supplemental review round 4.
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 2; $x.checkD20=24; $x.checkStat=-2; (CheckOf $r $x).d20=24; Reject $r $rs 'a planned total needing a die outside 1..20 refuses (PN4-F9)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 9; $x.shieldBonus=0; $x.checkBonus=0; $x.checkD20=16; (CheckOf $r $x).d20=16; (CheckOf $r $x).kmcShieldModifiers=0; Reject $r $rs 'a check under a stronger shield without the +3 refuses (PN4-F9)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').steps.competingBeforeCheck=(New-Snap 3); Reject $r $rs 'a "stronger shield" check with no competing +4 on the mount refuses (PN4-F9)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').bandMiss=$null; Reject $r $rs 'no identified band miss refuses (PN4-F10)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').bandMiss.identity=(RecordOf $r 'MS-check-rt' 2).ruleIdentity; Reject $r $rs 'a band miss identity that is a recorded hit refuses (PN4-F10)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').bandAttempts=0; Reject $r $rs 'a band miss without a counted strike refuses (PN4-F10)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.PSObject.Properties.Remove('drain'); Reject $r $rs 'missing drain evidence refuses (PN4-F4)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain | Add-Member calibrationError 'stuck'; Reject $r $rs 'a setup modifier left at the drain refuses (PN4-F4)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.PSObject.Properties.Remove('owner'); Reject $r $rs 'a second rider without restoration evidence refuses (PN4-F4)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.rider.errors=@('hand not restored'); Reject $r $rs 'an unrestored original hand refuses (PN4-F4)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').levels[0].pass.afterBack.bothChosen=$false; Reject $r $rs 'Back whose withdrawn choice was not chosen again refuses (PN4-F5)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').ownerCompanion.PSObject.Properties.Remove('experienceSetup'); Reject $r $rs 'undisclosed second-rider experience refuses (PN4-F6)'
# Supplemental review round 5.
$r=Copy-MS $real; $checks=@($r.observations.sprint6Feat.skillChecks | Where-Object { [int]$_.frame -ne 900 }); $checks[2].kmcShieldModifiers=0; $checks[2].kmcShieldTotal=0; Reject $r $rs 'a check under a stronger shield borrowing another roll''s +3 refuses (PN5-F6)'
$r=Copy-MS $real; $b=(RowOf $r 'MS-check-rt').band; $b.otherShield=2; $b.effectiveDelta=1; $b.acWithoutShield=23; Reject $r $rs 'a miss that already missed the AC without Mounted Shield refuses (PN5-F7)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').band.effectiveDelta=3; (RowOf $r 'MS-check-rt').band.otherShield=2; Reject $r $rs 'an effective AC change not derived from the native modifiers refuses (PN5-F7)'
$r=Copy-MS $real; (RecordOf $r 'MS-check-rt' 4).swiftAfter=0; Reject $r $rs 'a real-time success that charged no Swift action refuses (PN5-F8)'
$r=Copy-MS $real; (RecordOf $r 'MS-check-rt' 2).stateAfter.window=$false; Reject $r $rs 'a real-time equality that consumed no use refuses (PN5-F8)'
$r=Copy-MS $real; (RowOf $r 'MS-equipment') | Add-Member towerRefused 'item factory failed'; Reject $r $rs 'a tower skipped for anything but a native slot refusal refuses (PN5-F9)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.shieldBlueprints.enhancedCandidates[0].stockLoaded=$false; Reject $r $rs 'a shield that is not a loaded stock blueprint refuses (PN5-F9)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.clean=$false; Reject $r $rs 'a drain that is not clean refuses (PN5-F3)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.setup.remaining=@('KMC_Sprint6Shield_Setup_band'); Reject $r $rs 'a setup obligation left at the drain refuses (PN5-F3)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 2).swiftAfter=0; Reject $r $ts 'an own-turn success that spent no Swift action refuses (PN5-F8)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 6).stateAfter.owed=$false; Reject $r $ts 'an off-turn equality with no debt refuses (PN5-F8)'
# Turn-based.
$r=Copy-MS $turn; $x=RecordOf $r 'MS-check-tb' 6; $x.checkResult=28; $x.checkD20=15; (CheckOf $r $x).result=28; Reject $r $ts 'a turn-based "equal" three above equality refuses'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 6).commitment='SpendCurrentSwift'; Reject $r $ts 'an off-turn use that did not borrow refuses'
$r=Copy-MS $turn; (RowOf $r 'MS-check-tb').steps.ownTurnSecondRefused=(New-Snap 0); Reject $r $ts 'passive AC lost in turn-based spent state refuses'
# A failed producer stays failed.
$r=Copy-MS $turn; $r.status='FAIL'; $r.subscenarioFailCount=1; $threw=$false; try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario=$ts}) -Artifact $r -Status 'PASS' } catch { $threw=$true }; Check $threw 'a failed producer cannot become PASS'

# Persistence (mounted-shield-tb-manual).
function New-Persist([int]$Owned,[bool]$Cold){
 $s={ param($n) [pscustomobject]@{rider=$rider;mount=$mount;mounted=$true;hasFeat=$true;hasShieldFocus=$true;shieldBase=2;amount=3;ownedCount=$n;ownedValues=@(@(3)*$n);ownedByRiderFact=@(@($true)*$n);otherMountOwned=0;ledgerMount=$(if($n -gt 0){$mount}else{$null});ac=23} }
 $scan={ [pscustomobject]@{file='Manual_1.zks';area='b10e86516b6437d45a629931c30936fe';entries=@([pscustomobject]@{entry='party.json';present=$true;length=900},[pscustomobject]@{entry='b10e86516b6437d45a629931c30936fe.json';present=$true;length=4000});mountTags=0;checkTags=0;error=$null} }
 $continuation=[pscustomobject]@{actorIds=@($rider,$mount,$owner,$pet)}
 if($Cold){
  $restore=& $s 0; $restore | Add-Member archive (& $scan)
  return [pscustomobject]@{shieldColdAtRestore=$restore;shieldColdContinuation=(& $s $Owned);combatContinuation=$continuation}
 }
 $save=& $s 1; $save | Add-Member shieldIdentity (New-Stock)
 [pscustomobject]@{shieldAtSave=$save;shieldSavedArchive=(& $scan);shieldWarmContinuation=(& $s $Owned);combatContinuation=$continuation}
}
function Persist($Value,[bool]$Cold){ try { Assert-KmcSprint6ShieldPersistence ([pscustomobject]@{}) $Value $Cold; $true } catch { $false } }
Check (Persist (New-Persist 1 $true) $true) 'cold load with exactly one current contribution accepted'
Check (Persist (New-Persist 1 $false) $false) 'warm continuation with exactly one current contribution accepted'
Check (-not (Persist (New-Persist 2 $true) $true)) 'a doubled contribution after the cold load refuses'
Check (-not (Persist (New-Persist 0 $false) $false)) 'a lost contribution after the save refuses'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.ownedCount=2; Check (-not (Persist $p $true)) 'a stale serialized contribution at restore refuses'
$p=New-Persist 1 $true; $p.shieldColdContinuation.otherMountOwned=1; Check (-not (Persist $p $true)) 'a contribution on the other pair''s mount refuses'
$p=New-Persist 1 $true; $p.shieldColdContinuation.ownedByRiderFact=@($false); Check (-not (Persist $p $true)) 'a restored contribution not owned by the rider''s fact refuses'
$p=New-Persist 1 $true; $p | Add-Member shieldAtSave $p.shieldColdContinuation; Check (-not (Persist $p $true)) 'a cold artifact carrying producer state refuses (shapes are read separately)'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.archive.mountTags=1; Check (-not (Persist $p $true)) 'a loaded archive that serialized a Mounted Shield modifier refuses'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.archive.error='read failed'; Check (-not (Persist $p $true)) 'an unread loaded archive refuses'
$p=New-Persist 1 $true; $p.combatContinuation.actorIds=@($owner,$pet); Check (-not (Persist $p $true)) 'a cold pair outside the authenticated continuation refuses'
$p=New-Persist 1 $false; $p.PSObject.Properties.Remove('shieldSavedArchive'); Check (-not (Persist $p $false)) 'a producer without its written-archive scan refuses'
$p=New-Persist 1 $false; $p.shieldSavedArchive.checkTags=1; Check (-not (Persist $p $false)) 'a written archive holding a check modifier refuses'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.archive.entries=@($p.shieldColdAtRestore.archive.entries[1]); Check (-not (Persist $p $true)) 'a loaded archive scan without party.json refuses (PN5-F5)'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.archive.entries[1].length=0; Check (-not (Persist $p $true)) 'an empty saved-area member refuses (PN5-F5)'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.ownedCount=1; $p.shieldColdAtRestore.ownedValues=@(2); $p.shieldColdAtRestore.ownedByRiderFact=@($true); $p.shieldColdAtRestore.ledgerMount=$mount; Check (-not (Persist $p $true)) 'a wrong-valued contribution at restoration refuses (PN5-F5)'
$p=New-Persist 1 $true; $p.combatContinuation.actorIds=@($mount,$rider,$owner,$pet); Check (-not (Persist $p $true)) 'the pair in swapped roles refuses (PN5-F5)'
$p=New-Persist 1 $true; $p.combatContinuation.actorIds=@($owner,$pet,$rider,$mount); Check (-not (Persist $p $true)) 'the second pair''s roles refuse (PN5-F5)'
$p=New-Persist 1 $false; $p.shieldAtSave.shieldIdentity.stockLoaded=$false; Check (-not (Persist $p $false)) 'a persistence shield that is not a loaded stock blueprint refuses (PN5-F9)'

Write-Output ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
