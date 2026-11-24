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
 $M.records+=[pscustomobject]@{case=$Case;step=$Step;plan=$Plan;ruleIdentity=$script:id;rider=$rider;mountId=$mount;attacker=$hostile;mode=$Mode;attackD20=15;attackBonus=10;attackResult=$total;resultBefore='Hit';resultAfter=$result;criticalBefore=$false;criticalAfter=$false;opportunity=$Opportunity;riderTurn=$RiderTurn;abilityGuid=$null;forcedHitContext=$false;refusal=$Refusal;commitment=$Commitment;attempted=$attempted;forced=($attempted -and $Plan -cne 'natural');plannedUnattainable=$false;negated=$negated;checkD20=$(if($attempted){$check-$stat-$bonus}else{0});checkStat=$(if($attempted){$stat}else{0});checkBonus=$bonus;checkResult=$check;checkDc=$(if($attempted){$total+1}else{0});shieldBonus=$bonus;swiftBefore=$SwiftBefore;swiftAfter=$SwiftAfter;stateBefore=$Before;stateAfter=$After;mountSwiftBefore=0;mountSwiftAfter=0;mountDamageAtRecord=0}
 $M.attackRules+=[pscustomobject]@{identity=$script:id;attacker=$hostile;target=$mount;d20=15;bonus=10;ac=24;autoHit=$false;opportunity=$Opportunity;charge=$false;attackerCommand=$(if($Opportunity){$null}else{'UnitAttack'});resultAtEvent=$result;isHitAtEvent=(-not $negated);criticalAtEvent=$false;sneakAtEvent=$false;resultAfterRule=$result;isHitAfterRule=(-not $negated);damageRules=$(if($negated){0}else{1});damage=$(if($negated){0}else{4})}
 if($attempted){ $M.skillChecks+=[pscustomobject]@{actor=$rider;d20=$check-$stat-$bonus;stat=$stat;bonus=$bonus;result=$check;dc=$total+1;passed=$negated;frame=$script:id;kmcShieldModifiers=1;kmcShieldTotal=3} }
}
function New-Measure($Scenario){ [pscustomobject]@{contract='native-mounted-combat-feat-v1';rider=$rider;mount=$mount;cases=@(Get-KmcSprint6Cases $Scenario);checkD20IsControlled=$true;experienceIsDisclosedSetup=$true;records=@();attackRules=@();skillChecks=@()
  mountedShield=[pscustomobject]@{contract='native-mounted-shield-v1';featGuid=$feat;setupDisclosure='disclosed'
  shieldBlueprints=(New-StockTable)
  drain=[pscustomobject]@{attempts=1;setup=[pscustomobject]@{faults=@();allFaults=@();remaining=@()};buffsLeft=@();rider=(New-Restored);owner=(New-Restored);clean=$true;final=$true}}} }
$stockRows=[ordered]@{buckler=@('78aef04821150bd479314bc974ea73e2','26fcc43f7d20374498d2e1643381d345',0,'Buckler');light=@('a85d51d0fb905f940b951eec60388bac','d38e8ea23ce653c4582eb3e002555483',0,'LightShield')
 heavy=@('f4cef3ba1a15b0f4fa7fd66b602ff32b','d1b05b901bab9524388ebfa0435902a6',0,'HeavyShield');tower=@('6a1a429f023e5134fb16a4962cb79257','5f0f4b6e480e7054b8592b5a8b55854a',0,'TowerShield')
 heavyPlus1=@('5c7b898a1bfb6cb4f8c14a0ebc143abe','d1b05b901bab9524388ebfa0435902a6',1,'HeavyShieldItemPlus1');heavyPlus3=@('7e23918977b18524591204c93fae0a37','d1b05b901bab9524388ebfa0435902a6',3,'HeavyShieldItemPlus3');heavyPlus5=@('0b09d902ff100524dbec6e3d3d93dc3b','d1b05b901bab9524388ebfa0435902a6',5,'HeavyShieldItemPlus5')}
function New-StockRow([string]$Key){ $r=$stockRows[$Key]; [pscustomobject]@{guid=$r[0];name=$r[3];expectedName=$r[3];armorType=$r[1];expectedArmorType=$r[1];requestedEnhancement=$r[2];instanceId=1200;stockVerified=$true} }
function New-StockTable { $t=[pscustomobject]@{}; foreach($k in $stockRows.Keys){ $t | Add-Member $k (New-StockRow $k) }; $t }
function New-Stock { New-StockRow 'heavy' }
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
  ac=20+$HighestShield;touch=12;flatFooted=18+$HighestShield;flatFootedTouch=10;ruleMelee=20+$HighestShield;ruleMeleeFlatFooted=$false;ruleTouch=12;ruleTouchFlatFooted=$false
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
 $shieldElig=@((New-Eligibility 'start' 'FighterFeatSelection' $true $true),(New-Eligibility 'start' 'BasicFeatSelection' $true $true))
 $shieldLevel=[pscustomobject]@{committed=$true;characterLevelBefore=6;characterLevelAfter=7;characterLevelAfterCancel=6;factsBefore=(New-Facts 1 1 0);factsAfterCancel=(New-Facts 1 1 0);factsAfter=(New-Facts 1 1 1)
  cancelled=[pscustomobject]@{eligibility=$shieldElig;previewFacts=[pscustomobject]@{mountedCombat=$true;shieldFocus=$true;mountedShield=$true}}
  pass=[pscustomobject]@{choices=@('selection:BasicFeatSelection=MountedShieldFeature');eligibility=$shieldElig;previewFacts=[pscustomobject]@{mountedCombat=$true;shieldFocus=$true;mountedShield=$true}}}
 $e=[pscustomobject]@{
  shieldRegistration=[pscustomobject]@{failure=$null;withdrawn=$false;guid=$feat;groups=@('Feat','CombatFeat');ranks=1;isClassFeature=$true;hideInUi=$false
   prerequisites=@([pscustomobject]@{type='PrerequisiteFeature';group='All';hidden=$false;feature=$combat},[pscustomobject]@{type='PrerequisiteFeature';group='All';hidden=$false;feature=$focus},[pscustomobject]@{type='MountedShieldAvailabilityPrerequisite';group='All';hidden=$true;feature=$null})
   iconIsShieldIcon=$true;iconSprite='KMC_MountedShield_Feat_Icon';iconWidth=64;iconHeight=64;name='Mounted Shield';description='A shield bonus to the mount and to the Mounted Combat check; enhancement does not count.'}
  selections=@([pscustomobject]@{name='BasicFeatSelection';guid='247a4068296e8be42890143f451b4b45'},[pscustomobject]@{name='FighterFeatSelection';guid='41c8486641f7d6d4283ca9dae4147a9f'})
  threeFeatSelections=@([pscustomobject]@{guid='247a4068296e8be42890143f451b4b45';name='BasicFeatSelection';combat=1;archery=1;shield=1;kmcTailExact=$true;kmcTail=@('a7d2e94c6f1b4830b5c9e28d17f3a604','b81926bca1d24a3997cdc8471a84014b','671a301bd1774677a8bfa2873d33b191')},[pscustomobject]@{guid='41c8486641f7d6d4283ca9dae4147a9f';name='FighterFeatSelection';combat=1;archery=1;shield=1;kmcTailExact=$true;kmcTail=@('a7d2e94c6f1b4830b5c9e28d17f3a604','b81926bca1d24a3997cdc8471a84014b','671a301bd1774677a8bfa2873d33b191')})
  levels=@($pairLevel,$shieldLevel); riderFacts=(New-Facts 1 1 1)}
 if($RealTime){
  $e | Add-Member ownerLevels @([pscustomobject]@{committed=$true;pass=[pscustomobject]@{eligibility=@((New-Eligibility 'after-ShieldFocus' 'FighterFeatSelection' $false $true))}})
  $e | Add-Member ownerCompanion ([pscustomobject]@{acquired=$true;experienceBefore=5000;experienceSetup=23000})
  $e | Add-Member ownerSetupFacts ([pscustomobject]@{unit=$owner;mountedCombat=1;shieldFocus=1;mountedShield=1;mountedShieldRank=1})
 }
 $e
}
function New-Equipment {
 $rs=New-Snap 0
 $u=New-Snap 0; $u | Add-Member shieldInInventory $true
 $steps=[pscustomobject]@{none=(New-Snap 0);buckler=(New-Snap 2 -Base 1);light=(New-Snap 2 -Base 1);heavy=(New-Snap 3);tower=(New-Snap 5 -Base 4);heavyEnhanced0=(New-Snap 3 -Enhancement 1);heavyEnhanced1=(New-Snap 3 -Enhancement 3);heavyEnhanced2=(New-Snap 3 -Enhancement 5)
  heavyAgain=(New-Snap 3);greater=(New-Snap 4 -Greater $true);greaterRemoved=(New-Snap 3);competing2=(New-Snap 3 3 -Others @(2));competing3=(New-Snap 3 3 -Others @(3));competing4=(New-Snap 3 4 -Others @(4))
  mountSpell=(New-Snap 3 4 -Others @(4));otherSet=(New-Snap 0 -Set 1);setRestored=(New-Snap 3);unequipped=$u;riderSpellOnly=$rs;final=(New-Snap 3)}
 foreach($k in 'buckler','light','heavy','tower'){ $steps.$k.shield | Add-Member blueprint $stockRows[$k][0]; $steps.$k.shield | Add-Member armorEnhancement 0 }
 $i=0; foreach($want in 1,3,5){ $s=$steps.('heavyEnhanced'+$i); $s.shield | Add-Member blueprint $stockRows['heavyPlus'+$want][0]; $s.shield | Add-Member armorEnhancement $want; $s | Add-Member requestedEnhancement $want; $i++ }
 $steps.mountSpell.shieldModifiers=@($steps.mountSpell.shieldModifiers | ForEach-Object { if($_.kmc -ne $true){ $_.sourceGuid='9c0fa9b438ada3f43864be8dd8b3e741' }; $_ })
 $steps.riderSpellOnly.riderShieldModifiers=@([pscustomobject]@{value=4;item=$false;sourceGuid='9c0fa9b438ada3f43864be8dd8b3e741'})
 [pscustomobject]@{steps=$steps;mountSpell=[pscustomobject]@{guid='9c0fa9b438ada3f43864be8dd8b3e741';unit=$mount;value=4;applied=$true};riderSpell=[pscustomobject]@{guid='9c0fa9b438ada3f43864be8dd8b3e741';unit=$rider;value=4;applied=$true};equipment=[pscustomobject]@{shieldSet=0}}
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
  crossFirstToSecondMount=$cross1;crossSecondToFirstMount=$cross2;serviceDisableAccepted=$true;selectedBeforeSwitch=$rider;selectedAtSecondSnapshots=$owner;selectedAfterReturn=$rider}
}
# The driver's record of the record that completed each step, and of each planned attempt (review PN10-F3).
function Add-Completion($Check,$M,[string]$Case){
 $steps=[pscustomobject]@{}; $planned=[pscustomobject]@{}
 foreach($x in @($M.records | Where-Object { [string]$_.case -ceq $Case -and [string]$_.rider -ceq $rider })){
  $k=[string]$x.step; if($null -ne $steps.PSObject.Properties[$k]){ continue }
  $steps | Add-Member -MemberType NoteProperty -Name $k -Value $x.ruleIdentity
  if([string]$x.plan -cin @('success','equal')){ $planned | Add-Member -MemberType NoteProperty -Name $k -Value ([pscustomobject]@{identity=$x.ruleIdentity;attacker=$x.attacker;rider=$x.rider;mount=$x.mountId;plan=$x.plan}) }
 }
 $Check | Add-Member stepRecords $steps; $Check | Add-Member plannedAttempts $planned; $Check | Add-Member hostile $hostile
}
function New-UseSample { [pscustomobject]@{spent=$false;owed=$false;window=$true;windowIdentity=7;swift=6;windowEndTicks=638000000000000000} }
function New-Real {
 $m=New-Measure 'sprint6-shield-rt'; $free=New-State; $win=New-State $false $false $true; $c='MS-check-rt'
 Add-Attack $m $c 2 'equal' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-Attack $m $c 4 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT' $false
 Add-Attack $m $c 5 'natural' 'UsageSpent' 'None' $win $win 6 6 $false 'RT'
 Add-Attack $m $c 7 'natural' 'PreferenceOff' 'None' $free $free 0 0 $false 'RT'
 Add-Attack $m $c 9 'success' 'None' 'RealTimeWindow' $free $win 0 6 $false 'RT'
 $m.attackRules[-1] | Add-Member acContext ([pscustomobject]@{owned=3;ownedCount=1;otherShield=4;effective=0;acResult=24;acAttackType='Melee';attackType='Melee'})
 # The unrelated Mobility check, and a miss inside the shield's band (22 against AC 24 with +3).
 $m.skillChecks+=[pscustomobject]@{actor=$rider;d20=8;stat=12;bonus=0;result=20;dc=10;passed=$true;frame=900;kmcShieldModifiers=0;kmcShieldTotal=0}
 $script:id++; $bandId=$script:id; $m.attackRules+=[pscustomobject]@{identity=$script:id;attacker=$hostile;target=$mount;d20=10;bonus=12;ac=24;acContext=[pscustomobject]@{owned=3;ownedCount=1;otherShield=0;effective=3;acResult=24;acAttackType='Melee';attackType='Melee'};autoHit=$false;opportunity=$false;charge=$false;attackerCommand=$null;resultAtEvent='Miss';isHitAtEvent=$false;criticalAtEvent=$false;sneakAtEvent=$false;resultAfterRule='Miss';isHitAfterRule=$false;damageRules=0;damage=0}
 $check=[pscustomobject]@{steps=[pscustomobject]@{combatStart=(New-Snap 3);equal=(New-Snap 3);success=(New-Snap 3);spentRefused=(New-Snap 3);preferenceOffRefused=(New-Snap 3);competingBeforeCheck=(New-Snap 3 4 -Others @(4));competingCheck=(New-Snap 3 4 -Others @(4));bandDone=(New-Snap 3)}
  bandMiss=[pscustomobject]@{identity=$bandId;attacker=$hostile;target=$mount;d20=10;bonus=12;ac=24;acContext=[pscustomobject]@{owned=3;ownedCount=1;otherShield=0;effective=3;acResult=24;acAttackType='Melee';attackType='Melee'};isHitAtEvent=$false}
  equipmentDuringUse=[pscustomobject]@{before=(New-UseSample);unequipped=(New-UseSample);reequipped=(New-UseSample)};unrelatedMobility=[pscustomobject]@{stat=12;bonus=0;displayed=12;dc=10;result=20;frame=900;contributions=2}
  band=[pscustomobject]@{amount=3;ac=24;otherShield=0;effectiveDelta=3;acWithoutShield=21;attackBonusBefore=30;adjustment=-18};bandAttempts=9}
 Add-Completion $check $m $c
 New-Envelope $m @([pscustomobject]@{name='MS-selection';status='PASS';evidence=(New-Selection $true)},[pscustomobject]@{name='MS-equipment';status='PASS';evidence=(New-Equipment)},
  [pscustomobject]@{name='MS-pairs';status='PASS';evidence=(New-Pairs)},[pscustomobject]@{name='MS-check-rt';status='PASS';evidence=$check})
}
function New-Turn {
 $m=New-Measure 'sprint6-shield-tb'; $free=New-State; $spent=New-State $true; $owed=New-State $true $true; $c='MS-check-tb'
 Add-Attack $m $c 2 'success' 'None' 'SpendCurrentSwift' $free $spent 0 6 $true 'TB'
 Add-Attack $m $c 3 'natural' 'UsageSpent' 'None' $spent $spent 6 6 $true 'TB'
 Add-Attack $m $c 6 'equal' 'None' 'BorrowNextSwift' $free $owed 0 0 $false 'TB' $false
 $check=[pscustomobject]@{steps=[pscustomobject]@{ownTurnBefore=(New-Snap 3);ownTurnNegated=(New-Snap 3);ownTurnSecondRefused=(New-Snap 3);renewed=(New-Snap 3);offTurnEqual=(New-Snap 3)}}
 Add-Completion $check $m $c
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
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').bandAttempts=121; Reject $r $rs 'a band miss beyond the 120-strike bound refuses (PN4-F10; zero strikes is accepted since PN11-F7)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.PSObject.Properties.Remove('drain'); Reject $r $rs 'missing drain evidence refuses (PN4-F4)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain | Add-Member calibrationError 'stuck'; Reject $r $rs 'a setup modifier left at the drain refuses (PN4-F4)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.PSObject.Properties.Remove('owner'); Reject $r $rs 'a second rider without restoration evidence refuses (PN4-F4)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.rider.errors=@('hand not restored'); Reject $r $rs 'an unrestored original hand refuses (PN4-F4)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').levels[0].pass.afterBack.bothChosen=$false; Reject $r $rs 'Back whose withdrawn choice was not chosen again refuses (PN4-F5)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').ownerCompanion.PSObject.Properties.Remove('experienceSetup'); Reject $r $rs 'undisclosed second-rider experience refuses (PN4-F6)'
# Supplemental review round 5.
$r=Copy-MS $real; $checks=@($r.observations.sprint6Feat.skillChecks | Where-Object { [int]$_.frame -ne 900 }); $checks[2].kmcShieldModifiers=0; $checks[2].kmcShieldTotal=0; Reject $r $rs 'a check under a stronger shield borrowing another roll''s +3 refuses (PN5-F6)'
function Set-BandContext($R,$C){ $row=RowOf $R 'MS-check-rt'; $row.bandMiss.acContext=$C; ($R.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $row.bandMiss.identity }).acContext=($C | ConvertTo-Json | ConvertFrom-Json) }
$r=Copy-MS $real; $r2=RowOf $r 'MS-check-rt'; $r2.bandMiss.bonus=11; ($r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $r2.bandMiss.identity }).bonus=11; Set-BandContext $r ([pscustomobject]@{owned=3;ownedCount=1;otherShield=2;effective=1}); Reject $r $rs 'a miss that already missed the AC without Mounted Shield refuses (PN5-F7)'
$r=Copy-MS $real; Set-BandContext $r ([pscustomobject]@{owned=3;ownedCount=1;otherShield=2;effective=3}); Reject $r $rs 'an effective AC change not derived from the modifiers at the attack refuses (PN5-F7)'
$r=Copy-MS $real; (RecordOf $r 'MS-check-rt' 4).swiftAfter=0; Reject $r $rs 'a real-time success that charged no Swift action refuses (PN5-F8)'
$r=Copy-MS $real; (RecordOf $r 'MS-check-rt' 2).stateAfter.window=$false; Reject $r $rs 'a real-time equality that consumed no use refuses (PN5-F8)'
$r=Copy-MS $real; (RowOf $r 'MS-equipment') | Add-Member towerRefused 'item factory failed'; Reject $r $rs 'a tower skipped for anything but a native slot refusal refuses (PN5-F9)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.shieldBlueprints.heavyPlus3.stockVerified=$false; Reject $r $rs 'a shield that is not a verified stock blueprint refuses (PN5-F9)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.clean=$false; Reject $r $rs 'a drain that is not clean refuses (PN5-F3)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.setup.remaining=@('KMC_Sprint6Shield_Setup_band'); Reject $r $rs 'a setup obligation left at the drain refuses (PN5-F3)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 2).swiftAfter=0; Reject $r $ts 'an own-turn success that spent no Swift action refuses (PN5-F8)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 6).stateAfter.owed=$false; Reject $r $ts 'an off-turn equality with no debt refuses (PN5-F8)'
# Supplemental review round 6.
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.mountSpell.shieldModifiers[1].sourceGuid='other'; Reject $r $rs 'a competing bonus not from the stock Shield spell refuses (PN6-F3)'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.riderSpellOnly.riderShieldModifiers[0].sourceGuid=$null; Reject $r $rs 'an anonymous rider Shield modifier in place of the spell refuses (PN6-F3)'
$r=Copy-MS $real; $e=RowOf $r 'MS-equipment'; $e.mountSpell | Add-Member unavailable 'not loaded'; $e.steps.mountSpell.shieldModifiers=@($e.steps.mountSpell.shieldModifiers[0]); $e.steps.mountSpell.ac=23; $e.steps.mountSpell.flatFooted=21; $e.steps.mountSpell.ruleMelee=23; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'an explicitly unavailable Shield spell observation is accepted as not run (PN6-F3)'
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.shieldBlueprints.heavy.guid='a'; Reject $r $rs 'a heavy shield that is not the verified stock item refuses (PN6-F4)'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavyEnhanced1.shield.armorEnhancement=0; Reject $r $rs 'a priced item with no native armor enhancement refuses (PN6-F4)'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.PSObject.Properties.Remove('heavyEnhanced2'); Reject $r $rs 'a missing +5 observation refuses (PN6-F4)'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.light.shield.blueprint=$stockRows['heavy'][0]; Reject $r $rs 'a light observation on another item refuses (PN6-F4)'
$r=Copy-MS $real; $c=(RowOf $r 'MS-check-rt').bandMiss.acContext; $c.otherShield=2; $c.effective=1; ($r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq (RowOf $r 'MS-check-rt').bandMiss.identity }).acContext=$c; Reject $r $rs 'a miss whose own AC context leaves Mounted Shield one point refuses (PN6-F6)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').bandMiss.acContext.otherShield=1; Reject $r $rs 'a recorded miss context that differs from the attack''s refuses (PN6-F6)'
$r=Copy-MS $real; (RecordOf $r 'MS-check-rt' 4).swiftAfter=12; Reject $r $rs 'a real-time use charged twice refuses (PN6-F7)'
$r=Copy-MS $real; (RecordOf $r 'MS-check-rt' 2).stateBefore=(New-State $false $false $true); Reject $r $rs 'a real-time use with a window already open refuses (PN6-F7)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 2).swiftAfter=12; Reject $r $ts 'an own-turn use charged twice refuses (PN6-F7)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 6).stateBefore=(New-State $false $true); Reject $r $ts 'an off-turn use with a debt already owed refuses (PN6-F7)'
# Supplemental review round 10.
function Add-OtherPair($R,[int]$Step,[double]$SwiftAfter){ $m=$R.observations.sprint6Feat; Add-Attack $m 'MS-check-rt' $Step 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 $SwiftAfter $false 'RT'; $m.records[-1].rider=$owner; $m.records[-1].mountId=$pet; $m.attackRules[-1].target=$pet; $m.records[-1].shieldBonus=2; $m.skillChecks[-1].actor=$owner; $m.skillChecks[-1].kmcShieldTotal=2; $m.records[-1].forced=$false; $m }
$r=Copy-MS $real; $m=Add-OtherPair $r 4 6; $m.attackRules[-1].target=$mount; Reject $r $rs 'another pair''s defense whose attack rule targets the measured mount refuses (PN10-F3)'
$r=Copy-MS $real; $m=Add-OtherPair $r 4 12; Reject $r $rs 'a double Swift charge on another rider''s natural defense refuses (PN10-F4)'
$r=Copy-MS $real; $m=Add-OtherPair $r 4 6; $m.records[-1].mode='TB'; $m.records[-1].commitment='BorrowNextSwift'; $m.records[-1].stateAfter=(New-State $true $true); $m.records[-1].swiftAfter=0; Reject $r $rs 'another rider''s turn-based commitment on the real-time row refuses (PN10-F4)'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; Add-Attack $m 'MS-check-rt' 4 'natural' 'UsageSpent' 'None' (New-State $false $false $true) (New-State $false $false $true) 6 6 $false 'RT'; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'a legitimate follow-up strike refused as UsageSpent in the planned step is accepted (PN10-F3)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').stepRecords.'4'=99999; Reject $r $rs 'a step completed by an unrecorded attack refuses (PN10-F3)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; (RowOf $r 'MS-check-rt').plannedAttempts.'4'.identity=12345; Reject $r $rs 'a planned attempt that is not the record completing the step refuses (PN10-F3)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 2; ($r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $x.ruleIdentity }).target=$pet; Reject $r $rs 'a measured record whose attack rule targets another mount refuses (PN10-F3)'
$r=Copy-MS $real; $sel=RowOf $r 'MS-selection'; $sel.selections=@($sel.selections)+@([pscustomobject]@{name='ExtraFeatSelection';guid='0123456789abcdef0123456789abcdef'}); $sel.threeFeatSelections=@($sel.threeFeatSelections)+@([pscustomobject]@{guid='0123456789abcdef0123456789abcdef';name='ExtraFeatSelection';combat=1;archery=1;shield=1;kmcTailExact=$true;kmcTail=@('a7d2e94c6f1b4830b5c9e28d17f3a604','b81926bca1d24a3997cdc8471a84014b','671a301bd1774677a8bfa2873d33b191')}); $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'a third supported selection with its own valid tail is accepted (PN10-F5 control)'
$sel.threeFeatSelections=@($sel.threeFeatSelections | Select-Object -First 2); Reject $r $rs 'a third supported selection without its observation refuses (PN10-F5)'
# Supplemental review round 11.
function Add-FollowUp($R,[double]$SwiftAfter){ $m=$R.observations.sprint6Feat; Add-Attack $m 'MS-check-rt' 4 'natural' 'UsageSpent' 'None' (New-State $false $false $true) (New-State $false $false $true) 6 $SwiftAfter $false 'RT'; $m }
$r=Copy-MS $real; $m=Add-FollowUp $r 12; Reject $r $rs 'a refused follow-up that charged the rider another Swift refuses (PN11-F4)'
$r=Copy-MS $real; $m=Add-FollowUp $r 6; $m.attackRules[-1].target=$pet; Reject $r $rs 'a refused follow-up whose attack rule targets another mount refuses (PN11-F4)'
$r=Copy-MS $real; $m=Add-FollowUp $r 6; $m.records[-1].resultAfter='Parried'; Reject $r $rs 'a refused follow-up whose hit did not stand refuses (PN11-F4)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; $x.plan='natural'; (RowOf $r 'MS-check-rt').plannedAttempts.PSObject.Properties.Remove('4'); Reject $r $rs 'a forced boundary record labeled natural without its planned attempt refuses (PN11-F5)'
$r=Copy-MS $real; Add-Attack $r.observations.sprint6Feat 'MS-check-rt' 11 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT'; $r.observations.sprint6Feat.records[-1].forced=$true; Reject $r $rs 'a forced extra hit of the measured pair outside the plan refuses (PN11-F5)'
$r=Copy-MS $real; $m=Add-OtherPair $r 4 6; $m.records[-1].shieldBonus=3; $m.skillChecks[-1].kmcShieldTotal=3; Reject $r $rs 'the second rider''s check carrying the first rider''s +3 refuses (PN11-F6)'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; Add-Attack $m 'MS-check-rt' 11 'natural' 'UsageSpent' 'None' (New-State $false $false $true) (New-State $false $false $true) 6 6 $false 'RT'; $m.records[-1].attacker='second-hostile'; $m.attackRules[-1].attacker='second-hostile'; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'a second hostile''s legitimate attack on the measured pair is accepted (PN11-F7)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').bandAttempts=0; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'a qualifying miss before the driver''s first strike (0 strikes) is accepted (PN11-F7)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').hostile='second-hostile'; Reject $r $rs 'a miss by another hostile than the designated one refuses (PN11-F7)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').PSObject.Properties.Remove('hostile'); Reject $r $rs 'a row without its designated hostile refuses (PN11-F7)'
# Supplemental review round 12.
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; $x.plannedUnattainable=$true; $x.forced=$false; Reject $r $rs 'a planned total that was unattainable at the native roll fails the row (PN12-F1)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; $c4=CheckOf $r $x; $x.checkBonus=1; $x.checkD20=[int]$x.checkD20+2; $c4.bonus=1; $c4.d20=[int]$c4.d20+2; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'an in-range check with a genuine native -2 (observed in the native roll too) and the identified +3 is accepted (PN12-F1, PN13-F3)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; $x.checkBonus=1; $x.checkStat=12; Reject $r $rs 'a penalty present only in the service record, absent from the native roll, refuses (PN13-F3)'
$r=Copy-MS $real; $m=Add-FollowUp $r 6; $m.attackRules[-1].resultAtEvent='Parried'; $m.attackRules[-1].resultAfterRule='Parried'; $m.attackRules[-1].isHitAtEvent=$false; $m.attackRules[-1].isHitAfterRule=$false; $m.attackRules[-1].damageRules=0; Reject $r $rs 'a refused follow-up whose native attack was suppressed refuses (PN12-F2)'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; Add-Attack $m 'MS-pairs' 3 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT' $false; $m.records[-1].forced=$true; Reject $r $rs 'a forced defense outside the check rows refuses (PN12-F3)'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; Add-Attack $m $null 0 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT' $false; $m.records[-1].forced=$true; Reject $r $rs 'a forced defense with no case (cleanup) refuses (PN12-F3)'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; Add-Attack $m 'MS-pairs' 3 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT' $false; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'a natural defense outside the check rows is accepted (PN12-F3 control)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 4; ($r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $x.ruleIdentity }).bonus=12; Reject $r $rs 'a planned check against an attack total that differs from the native attack refuses (PN12-F4)'
$r=Copy-MS $real; $m=Add-OtherPair $r 4 6; $m.records[-1].mountId=$mount; $m.attackRules[-1].target=$mount; $m.records[-1].shieldBonus=0; $m.skillChecks[-1].kmcShieldModifiers=0; $m.skillChecks[-1].kmcShieldTotal=0; Reject $r $rs 'the second rider defending the first mount refuses (PN12-F5)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').equipmentDuringUse.reequipped.swift=0; Reject $r $rs 'an equipment change that refunded the Swift cooldown refuses (PN12-F6)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').equipmentDuringUse.reequipped.windowEndTicks=638000000100000000; Reject $r $rs 'an equipment change that extended the window refuses (PN12-F6)'
# Supplemental review round 13.
$r=Copy-MS $real; $m=Add-FollowUp $r 6; $m.records[-1].resultBefore='CriticalHit'; $m.records[-1].resultAfter='CriticalHit'; $m.records[-1].criticalBefore=$true; $m.records[-1].criticalAfter=$true; $m.attackRules[-1].resultAtEvent='CriticalHit'; $m.attackRules[-1].resultAfterRule='CriticalHit'; $m.attackRules[-1].criticalAtEvent=$true; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'a refused follow-up whose critical hit stands natively is accepted (PN13-F2 control)'
$r=Copy-MS $real; $m=Add-FollowUp $r 6; $m.records[-1].resultBefore='CriticalHit'; $m.records[-1].resultAfter='CriticalHit'; $m.records[-1].criticalBefore=$true; $m.records[-1].criticalAfter=$true; Reject $r $rs 'a refused follow-up whose native observer saw the critical downgraded to a hit refuses (PN13-F2)'
$r=Copy-MS $real; (RowOf $r 'MS-check-rt').unrelatedMobility.bonus=2; $u=$r.observations.sprint6Feat.skillChecks | Where-Object { [int]$_.frame -eq 900 }; $u.bonus=2; $u.result=22; (RowOf $r 'MS-check-rt').unrelatedMobility.result=22; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'an unrelated Mobility check with an ordinary native modifier and no KMC contribution is accepted (PN13-F3)'
$r=Copy-MS $real; $m=Add-OtherPair $r 4 6; $m.records[-1].mountId=$rider; $m.attackRules[-1].target=$rider; $m.records[-1].shieldBonus=0; $m.skillChecks[-1].kmcShieldModifiers=0; $m.skillChecks[-1].kmcShieldTotal=0; Reject $r $rs 'the second rider paying to defend the first rider refuses (PN13-F4)'
$r=Copy-MS $turn; $m=$r.observations.sprint6Feat; Add-Attack $m 'MS-check-tb' 8 'natural' 'None' 'SpendCurrentSwift' (New-State) (New-State $true) 0 6 $true 'TB'; $m.records[-1].mountId=$owner; $m.attackRules[-1].target=$owner; $m.records[-1].shieldBonus=0; $m.skillChecks[-1].kmcShieldModifiers=0; $m.skillChecks[-1].kmcShieldTotal=0; Reject $r $ts 'the measured rider paying to defend another target on the TB row refuses (PN13-F4)'
# Supplemental review round 9.
$r=Copy-MS $real; (RowOf $r 'MS-pairs').selectedAtSecondSnapshots=$rider; Reject $r $rs 'a selection switch that left the first rider selected refuses (PN9-F6)'
$r=Copy-MS $real; (RowOf $r 'MS-pairs').PSObject.Properties.Remove('selectedBeforeSwitch'); Reject $r $rs 'an unobserved selection before the switch refuses (PN9-F6)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').threeFeatSelections[1].guid='247a4068296e8be42890143f451b4b45'; (RowOf $r 'MS-selection').threeFeatSelections[1].name='BasicFeatSelection'; Reject $r $rs 'a duplicate list standing for the fighter list refuses (PN9-F5)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').threeFeatSelections[0].guid='a'; Reject $r $rs 'a three-feat row outside the producer''s selection inventory refuses (PN9-F5)'
$r=Copy-MS $real; foreach($l in (RowOf $r 'MS-selection').levels){ foreach($e in @($l.pass.eligibility)+@($l.cancelled.eligibility)){ if([string]$e.selection -ceq 'FighterFeatSelection' -and $e.mountedCombat -eq $true -and $e.shieldFocus -eq $true){ $e.eligible=$false } } }; Reject $r $rs 'a fighter list that always refuses an eligible rider refuses (PN9-F5)'
$r=Copy-MS $turn; Add-Attack $r.observations.sprint6Feat 'MS-check-tb' 8 'natural' 'None' 'SpendCurrentSwift' (New-State) (New-State $true) 0 6 $false 'TB'; Reject $r $ts 'an off-turn Swift spend refuses (PN9-F4)'
$r=Copy-MS $turn; Add-Attack $r.observations.sprint6Feat 'MS-check-tb' 8 'natural' 'None' 'BorrowNextSwift' (New-State) (New-State $true $true) 0 0 $true 'TB'; Reject $r $ts 'an own-turn borrow refuses (PN9-F4)'
$r=Copy-MS $real; Add-Attack $r.observations.sprint6Feat 'MS-check-rt' 11 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT'; $r.observations.sprint6Feat.skillChecks[-1].kmcShieldModifiers=0; $r.observations.sprint6Feat.skillChecks[-1].kmcShieldTotal=0; Reject $r $rs 'a natural roll of the measured pair without the native +3 refuses (PN9-F4)'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; Add-Attack $m 'MS-check-rt' 4 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT'; $m.records[-1].rider=$owner; $m.records[-1].mountId=$pet; $m.attackRules[-1].target=$pet; $m.records[-1].shieldBonus=2; $m.skillChecks[-1].actor=$owner; $m.skillChecks[-1].kmcShieldTotal=2; $m.records[-1].forced=$true; Reject $r $rs 'a planned roll on another pair''s defense refuses (PN9-F3)'
$r=Copy-MS $real; $m=$r.observations.sprint6Feat; Add-Attack $m 'MS-check-rt' 4 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT'; $m.records[-1].rider=$owner; $m.records[-1].mountId=$pet; $m.attackRules[-1].target=$pet; $m.records[-1].shieldBonus=2; $m.skillChecks[-1].actor=$owner; $m.skillChecks[-1].kmcShieldTotal=2; $m.records[-1].forced=$false; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'another pair''s natural defense during a planned step is accepted and does not replace the measured record (PN9-F3)'
# Supplemental review round 8.
$r=Copy-MS $real; $x=(RowOf $r 'MS-selection').threeFeatSelections[0]; $x.archery=0; $x.kmcTail=@('a1','a7d2e94c6f1b4830b5c9e28d17f3a604','671a301bd1774677a8bfa2873d33b191'); Reject $r $rs 'a three-feat observation with Mounted Archery absent refuses (PN8-F5)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').threeFeatSelections[0].archery=0; Reject $r $rs 'archery 0 with an exact tail claimed refuses (PN8-F5)'
$r=Copy-MS $real; (RowOf $r 'MS-selection').threeFeatSelections[1].kmcTail=@('a7d2e94c6f1b4830b5c9e28d17f3a604','671a301bd1774677a8bfa2873d33b191','b81926bca1d24a3997cdc8471a84014b'); Reject $r $rs 'a three-feat tail in another order refuses (PN8-F5)'
$r=Copy-MS $real; foreach($s in @((RowOf $r 'MS-equipment').steps.PSObject.Properties | ForEach-Object { $_.Value })){ if($null -ne $s.ruleMelee){ $s.ruleMeleeFlatFooted=$true; $s.ruleMelee=$s.flatFooted; $s.ruleTouchFlatFooted=$true; $s.ruleTouch=$s.flatFootedTouch } }; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'native flat-footed AC chosen by the rules in exploration is accepted against the flat-footed stat (PN8-F3)'
$r=Copy-MS $real; $s=(RowOf $r 'MS-equipment').steps.heavy; $s.ruleMeleeFlatFooted=$true; Reject $r $rs 'a flat-footed melee rule that returned the ordinary AC refuses (PN8-F3)'
$r=Copy-MS $real; (RowOf $r 'MS-equipment').steps.heavy.PSObject.Properties.Remove('ruleTouchFlatFooted'); Reject $r $rs 'an AC probe without its flat-footed choice refuses (PN8-F3)'
$r=Copy-MS $real; $x=RecordOf $r 'MS-check-rt' 9; $x.mode='TB'; $x.commitment='BorrowNextSwift'; $x.stateAfter=(New-State $true $true); $x.swiftAfter=0; Reject $r $rs 'a turn-based borrow certified on the real-time row refuses (PN8-F4)'
$r=Copy-MS $real; Add-Attack $r.observations.sprint6Feat 'MS-check-rt' 11 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 6 $false 'RT'; $ra=@(Accept $r $rs); Check ($ra[-1] -eq $true) 'an additional natural real-time use with exactly one Swift charge is accepted (PN8-F4)'
$r=Copy-MS $real; Add-Attack $r.observations.sprint6Feat 'MS-check-rt' 11 'natural' 'None' 'RealTimeWindow' (New-State) (New-State $false $false $true) 0 12 $false 'RT'; Reject $r $rs 'a double Swift charge on an additional natural hit refuses (PN8-F4)'
$r=Copy-MS $turn; Add-Attack $r.observations.sprint6Feat 'MS-check-tb' 8 'natural' 'None' 'SpendCurrentSwift' (New-State) (New-State $true) 0 6 $true 'TB'; $ta=@(Accept $r $ts); Check ($ta[-1] -eq $true) 'an additional own-turn use with exactly one Swift charge is accepted (PN8-F4 control)'
$r=Copy-MS $turn; Add-Attack $r.observations.sprint6Feat 'MS-check-tb' 8 'natural' 'None' 'SpendCurrentSwift' (New-State) (New-State $true) 6 12 $true 'TB'; Reject $r $ts 'an additional turn-based use from an unavailable Swift refuses (PN8-F4)'
# Supplemental review round 7 (gameplay).
$r=Copy-MS $real; $r.observations.sprint6Feat.mountedShield.drain.setup.allFaults=@('KMC_Sprint6Shield_Setup_competing callback: boom'); Reject $r $rs 'a setup removal fault kept from an earlier release refuses (PN7-F3)'
$r=Copy-MS $real; $id=(RecordOf $r 'MS-check-rt' 9).ruleIdentity; $a=$r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $id }; $a.acContext.otherShield=0; $a.acContext.effective=3; Reject $r $rs 'a check whose attack was calculated without the competing +4 refuses (PN7-F4)'
$r=Copy-MS $real; $id=(RecordOf $r 'MS-check-rt' 9).ruleIdentity; ($r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $id }).PSObject.Properties.Remove('acContext'); Reject $r $rs 'a check whose attack carries no AC-calculation terms refuses (PN7-F4)'
$r=Copy-MS $real; $row=RowOf $r 'MS-check-rt'; $row.bandMiss.acContext.acResult=22; ($r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $row.bandMiss.identity }).acContext.acResult=22; Reject $r $rs 'a band miss whose shield terms are from another AC calculation than its TargetAC refuses (PN7-F4)'
$r=Copy-MS $real; $row=RowOf $r 'MS-check-rt'; $row.bandMiss.acContext.acAttackType='Touch'; $row.bandMiss.acContext.attackType='Touch'; ($r.observations.sprint6Feat.attackRules | Where-Object { $_.identity -eq $row.bandMiss.identity }).acContext=$row.bandMiss.acContext; Reject $r $rs 'a band miss against touch AC refuses (PN7-F4)'
$r=Copy-MS $turn; $x=RecordOf $r 'MS-check-tb' 2; $x.swiftBefore=6; $x.swiftAfter=12; Reject $r $ts 'an own-turn use spending an unavailable Swift (6 to 12) refuses (PN7-F5)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 2).stateBefore=(New-State $false $false $true); Reject $r $ts 'an own-turn use with a real-time window already open refuses (PN7-F5)'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 6).stateBefore=(New-State $false $false $true); Reject $r $ts 'an off-turn borrow with a real-time window already open refuses (PN7-F5)'
$r=Copy-MS $real; (RecordOf $r 'MS-check-rt' 4).stateAfter=(New-State $false $true $true); Reject $r $rs 'a real-time use that also incurred a debt refuses (PN7-F5)'
$r=Copy-MS $turn; $x=RecordOf $r 'MS-check-tb' 6; $x.swiftBefore=6; $x.swiftAfter=6; $ta=@(Accept $r $ts); Check ($ta[-1] -eq $true) 'an off-turn borrow after a Swift legitimately spent on the own turn is accepted (PN7-F5)'
# Turn-based.
$r=Copy-MS $turn; $x=RecordOf $r 'MS-check-tb' 6; $x.checkResult=28; $x.checkD20=15; (CheckOf $r $x).result=28; Reject $r $ts 'a turn-based "equal" three above equality refuses'
$r=Copy-MS $turn; (RecordOf $r 'MS-check-tb' 6).commitment='SpendCurrentSwift'; Reject $r $ts 'an off-turn use that did not borrow refuses'
$r=Copy-MS $turn; (RowOf $r 'MS-check-tb').steps.ownTurnSecondRefused=(New-Snap 0); Reject $r $ts 'passive AC lost in turn-based spent state refuses'
# A failed producer stays failed.
$r=Copy-MS $turn; $r.status='FAIL'; $r.subscenarioFailCount=1; $threw=$false; try { Assert-KmcSprint6FeatEvidence -Request ([pscustomobject]@{scenario=$ts}) -Artifact $r -Status 'PASS' } catch { $threw=$true }; Check $threw 'a failed producer cannot become PASS'

# Persistence (mounted-shield-tb-manual).
function New-Persist([int]$Owned,[bool]$Cold){
 $s={ param($n) [pscustomobject]@{rider=$rider;mount=$mount;mounted=$true;hasFeat=$true;hasShieldFocus=$true;shieldBase=2;observedShieldBlueprint='f4cef3ba1a15b0f4fa7fd66b602ff32b';observedShieldItem=4711;observedShieldEnhancement=0;amount=3;ownedCount=$n;ownedValues=@(@(3)*$n);ownedByRiderFact=@(@($true)*$n);otherMountOwned=0;ledgerMount=$(if($n -gt 0){$mount}else{$null});ac=23} }
 $scan={ [pscustomobject]@{file='Manual_1.zks';area='b10e86516b6437d45a629931c30936fe';entries=@([pscustomobject]@{entry='party.json';present=$true;length=900},[pscustomobject]@{entry='b10e86516b6437d45a629931c30936fe.json';present=$true;length=4000});mountTags=0;checkTags=0;error=$null} }
 $continuation=[pscustomobject]@{actorIds=@($rider,$mount,$owner,$pet)}
 if($Cold){
  $restore=& $s 0; $restore | Add-Member archive (& $scan)
  return [pscustomobject]@{shieldColdAtRestore=$restore;shieldColdContinuation=(& $s $Owned);combatContinuation=$continuation;loadedDescriptor=[pscustomobject]@{fileName='Manual_1.zks'}}
 }
 $save=& $s 1; $save | Add-Member shieldIdentity (New-Stock); $save | Add-Member requestedShieldBlueprint 'f4cef3ba1a15b0f4fa7fd66b602ff32b'
 [pscustomobject]@{shieldAtSave=$save;shieldSavedArchive=(& $scan);shieldWarmContinuation=(& $s $Owned);combatContinuation=$continuation;nativeWrite=[pscustomobject]@{path='C:\run\Saved Games\Manual_1.zks'}
  shieldEquipmentCleanup=[pscustomobject]@{errors=@();createdLeft=0;handsChecked=4;set=0;originalSet=0}}
}
$script:archive=[pscustomobject]@{AreaId='b10e86516b6437d45a629931c30936fe';Pairs=@([pscustomobject]@{ProfileId='medium-humanoid-mammoth-v1';Rider=[pscustomobject]@{Id=$rider};Mount=[pscustomobject]@{Id=$mount}},[pscustomobject]@{ProfileId='medium-humanoid-horse-v1';Rider=[pscustomobject]@{Id=$owner};Mount=[pscustomobject]@{Id=$pet}})}
function Persist($Value,[bool]$Cold,$Archive=$script:archive){ try { Assert-KmcSprint6ShieldPersistence ([pscustomobject]@{}) $Value $Cold $Archive; $true } catch { $false } }
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
$p=New-Persist 1 $false; $p.shieldAtSave.shieldIdentity.stockVerified=$false; Check (-not (Persist $p $false)) 'a persistence shield that is not the verified stock item refuses (PN5-F9)'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.archive.area='party'; $p.shieldColdAtRestore.archive.entries=@($p.shieldColdAtRestore.archive.entries[0]); Check (-not (Persist $p $true)) 'a scan whose area is party.json refuses (PN6-F5)'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.archive.file='Other.zks'; Check (-not (Persist $p $true)) 'a scan of another archive file refuses (PN6-F5)'
$p=New-Persist 1 $true; $a=$script:archive | ConvertTo-Json -Depth 6 | ConvertFrom-Json; $a.AreaId='0123456789abcdef0123456789abcdef'; Check (-not (Persist $p $true $a)) 'a scan of another area than the validated archive refuses (PN6-F5)'
$p=New-Persist 1 $true; foreach($x in $p.shieldColdAtRestore,$p.shieldColdContinuation){ $x.rider=$owner; $x.mount=$pet; $x.ledgerMount=$(if($x.ownedCount -gt 0){$pet}else{$null}) }; $p.combatContinuation.actorIds=@($owner,$pet,$rider,$mount); Check (-not (Persist $p $true)) 'consistently swapped roles refuse against the validated archive (PN6-F5)'
$p=New-Persist 1 $true; $p.shieldColdAtRestore.ownedCount=1; $p.shieldColdAtRestore.ownedValues=@(3); $p.shieldColdAtRestore.ownedByRiderFact=@($true); $p.shieldColdAtRestore.ledgerMount=$mount; $p.shieldColdAtRestore.amount=0; Check (-not (Persist $p $true)) 'a stale contribution at restoration while the amount is zero refuses (PN6-F5)'
$p=New-Persist 1 $true; Check (-not (Persist $p $true $null)) 'persistence without the validated archive refuses (PN6-F5)'
# Supplemental review round 7 (persistence).
$p=New-Persist 1 $false; foreach($x in $p.shieldAtSave,$p.shieldWarmContinuation){ $x.observedShieldBlueprint='5c7b898a1bfb6cb4f8c14a0ebc143abe' }; Check (-not (Persist $p $false)) 'a same-base heavy shield of another item (the +1) at the save refuses (PN7-F6)'
$p=New-Persist 1 $true; foreach($x in $p.shieldColdAtRestore,$p.shieldColdContinuation){ $x.observedShieldBlueprint='5c7b898a1bfb6cb4f8c14a0ebc143abe' }; Check (-not (Persist $p $true)) 'a same-base heavy shield of another item after the cold load refuses (PN7-F6)'
$p=New-Persist 1 $false; $p.shieldWarmContinuation.observedShieldItem=4712; Check (-not (Persist $p $false)) 'a warm continuation holding another item than the one saved refuses (PN7-F6)'
$p=New-Persist 1 $true; $p.shieldColdContinuation.observedShieldEnhancement=1; Check (-not (Persist $p $true)) 'a cold continuation shield with an armor enhancement refuses (PN7-F6)'
$p=New-Persist 1 $false; $p.PSObject.Properties.Remove('shieldEquipmentCleanup'); Check (-not (Persist $p $false)) 'a producer without its equipment restoration refuses (PN7-F2)'
$p=New-Persist 1 $false; $p.shieldEquipmentCleanup.createdLeft=1; Check (-not (Persist $p $false)) 'a producer that left a created shield refuses (PN7-F2)'
$p=New-Persist 1 $false; $p.shieldEquipmentCleanup.errors=@('hand secondary still wrong'); Check (-not (Persist $p $false)) 'a producer with an unrestored hand refuses (PN7-F2)'

Write-Output ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
