[CmdletBinding()] param()
# F6 readers: synthetic (offline, not game) turn-based and persistence observations, one accepted shape each and the
# refusals that matter for the D5 fall, the exactly-once event and the post-fall save/reload.
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$passes=0
function Check([bool]$Condition,[string]$Message){ if(-not$Condition){throw ('FAIL '+$Message)}; $script:passes++; Write-Host ('PASS '+$Message) }
function Refuses([scriptblock]$Body,[string]$Message){ $threw=$false; try{ & $Body }catch{ $threw=$true }; Check $threw $Message }
function Clone($Value){ $Value|ConvertTo-Json -Depth 30|ConvertFrom-Json }
$shared=$script:KmcF5SharedName

# ---- the turn-based child --------------------------------------------------------------------------------------------
$aR='a1000000-0000-4000-8000-000000000001'; $aM='a1000000-0000-4000-8000-000000000002'; $bR='b1000000-0000-4000-8000-000000000003'; $bM='b1000000-0000-4000-8000-000000000004'
function U([string]$Id,[string]$Name,[int]$Damage=0,[bool]$Prone=$false,[string]$Life='Conscious',[bool]$HasMove=$true,[bool]$HasStandard=$true){
    [pscustomobject]@{id=$Id;name=$Name;damage=$Damage;hpLeft=30-$Damage;life=$Life;prone=$Prone;proneQueued=$false;standard=0;move=0;swift=0;hasMove=$HasMove;hasStandard=$HasStandard;hasSwift=$true;commandsEmpty=$true;inCombat=$true;canStandUp=$true;mountedCombat=[pscustomobject]@{spent=$false;owed=$false;window=$false}}
}
function Pair([string]$State,[long]$Generation,[string]$Owner){ [pscustomobject]@{state=$State;generation=$Generation;ownerId=$Owner;riderId=$null;mountId=$null} }
function Svc([long]$Begun,[long]$Resolved){ [pscustomobject]@{begun=$Begun;resolved=$Resolved;abandoned=0;pending=0;lastRefusal='None'} }
function Snap($Svc,$APair,$BPair,$ARider,$AMount,$BRider,$BMount){ [pscustomobject]@{frame=1;round=2;service=$Svc;aPair=$APair;bPair=$BPair;aRider=$ARider;aMount=$AMount;bRider=$BRider;bMount=$BMount;healCount=0} }
function FallRecord([string]$Cause,[string]$Rider,[string]$Mount,[string]$RiderName,[string]$MountName,[long]$Generation,[bool]$Pass,[string]$Mode){
    $stat=7; $dc=15; $d20=if($Pass){$dc-$stat}else{$dc-$stat-1}; $result=$d20+$stat
    $text=if($Pass){$MountName+' falls prone; '+$RiderName+' is thrown: Mobility '+$result+' vs DC '+$dc+' - lands safely.'}else{$MountName+' dies; '+$RiderName+' is thrown: Mobility '+$result+' vs DC '+$dc+' - falls hard, 4 damage, knocked prone.'}
    [pscustomobject]@{sequence=1;generation=$Generation;cause=$Cause;riderId=$Rider;mountId=$Mount;riderName=$RiderName;mountName=$MountName;mode=$Mode;round=2;captureFrame=1;resolveFrame=1;resolved=$true;abandonedReason=$null
        inability='None';attempted=$true;forced=$true;forcedPass=$Pass;passed=$Pass;thresholdAgrees=$true;checkD20=$d20;checkStat=$stat;checkBonus=0;checkResult=$result;checkDc=$dc;checkDcAdjustment=0
        outcome=$(if($Pass){'SafeLanding'}else{'HardLanding'});damageDealt=(-not$Pass);damageRolled=$(if($Pass){0}else{4});damageBeforeDifficulty=$(if($Pass){0}else{4});damageApplied=$(if($Pass){0}else{4});damageImmune=$false
        riderDamageBefore=0;riderDamageAfter=$(if($Pass){0}else{4});proneImposed=(-not$Pass);proneRefusal=$null;riderProneBefore=$false
        standardBefore=0;standardAfter=0;moveBefore=0;moveAfter=0;swiftBefore=0;swiftAfter=0;logText=$text}
}
$deathRecord=FallRecord 'MountDied' $aR $aM 'Valerie' 'Mastodon' 1 $false 'TB'
$proneRecord=FallRecord 'MountFellProne' $bR $bM 'Linzi' 'Steed' 1 $true 'TB'
$cases=@(Get-KmcFinalize6TurnBasedCases)
function Row([string]$Name,$Steps){ [pscustomobject]@{name=$Name;status='PASS';evidence=[pscustomobject]@{case=$Name;steps=$Steps}} }
$mounted=Pair 'Mounted' 1 'aa'; $bMounted=Pair 'Mounted' 1 'bb'; $none=Pair 'Unmounted' 1 $null
$aRow=[pscustomobject]@{
    fallBefore=(Snap (Svc 0 0) $mounted $bMounted (U $aR 'Valerie') (U $aM 'Mastodon') (U $bR 'Linzi') (U $bM 'Steed'))
    kill=[pscustomobject]@{requested=90;applied=90}
    fallAfter=(Snap (Svc 1 1) (Pair 'Unmounted' 1 $null) $bMounted (U $aR 'Valerie' 4) (U $aM 'Mastodon' 90 $true 'Dead') (U $bR 'Linzi') (U $bM 'Steed'))
    placement=[pscustomobject]@{riderToMount=1.9;riderCorpulence=0.4;mountCorpulence=1.5}
    fallSettled=(Snap (Svc 1 1) (Pair 'Unmounted' 1 $null) $bMounted (U $aR 'Valerie' 4 $true) (U $aM 'Mastodon' 90 $true 'Dead') (U $bR 'Linzi') (U $bM 'Steed'))
    standUpBefore=(Snap (Svc 1 1) $none $bMounted (U $aR 'Valerie' 4 $true) $null (U $bR 'Linzi') (U $bM 'Steed'))
    standUpAfter=(Snap (Svc 1 1) $none $bMounted (U $aR 'Valerie' 4 $false 'Conscious' $false $true) $null (U $bR 'Linzi') (U $bM 'Steed'))
    end=[pscustomobject]@{endedNatively=$true}}
$bRow=[pscustomobject]@{
    fallBefore=(Snap (Svc 1 1) $none $bMounted (U $aR 'Valerie' 4) $null (U $bR 'Linzi') (U $bM 'Steed'))
    knockOff=[pscustomobject]@{canBeKnockedOff=$true;wasProne=$false}
    fallAfter=(Snap (Svc 2 2) $none (Pair 'Unmounted' 1 $null) (U $aR 'Valerie' 4) $null (U $bR 'Linzi') (U $bM 'Steed' 0 $true))
    placement=[pscustomobject]@{riderToMount=1.1;riderCorpulence=0.4;mountCorpulence=0.6}
    fallSettled=(Snap (Svc 2 2) $none $none (U $aR 'Valerie' 4) $null (U $bR 'Linzi') (U $bM 'Steed' 0 $true))
    repeatKnockOff=[pscustomobject]@{canBeKnockedOff=$false;wasProne=$true}
    deathAfterProne=[pscustomobject]@{requested=60;applied=60}
    afterDeath=(Snap (Svc 2 2) $none $none (U $aR 'Valerie' 4) $null (U $bR 'Linzi') (U $bM 'Steed' 60 $true 'Dead'))
    begunSinceUnseated=0;end=[pscustomobject]@{endedNatively=$true}}
$aRow | Add-Member deathActivationEnded $true; $bRow | Add-Member proneActivationEnded $true
$aRow | Add-Member standUpTurn ([pscustomobject]@{sameTurnAsNext=$true;unit=$aR;round=3;status='Acting'}); $aRow | Add-Member standUpTravel 0.1
$aRow | Add-Member standUpBoundary (Snap (Svc 1 1) $none $bMounted (U $aR 'Valerie' 4 $false 'Conscious' $false $true) $null (U $bR 'Linzi') (U $bM 'Steed'))
$aRow.standUpBoundary.aRider.move=3; $aRow.fallBefore | Add-Member turnRole 'aRider'; $bRow.fallBefore | Add-Member turnRole 'bRider'
$nt=(Snap (Svc 1 1) $none $bMounted (U $aR 'Valerie' 4 $true) $null (U $bR 'Linzi') (U $bM 'Steed')); $nt.round=3; $nt | Add-Member turnRole 'aRider'; $aRow | Add-Member nextTurn $nt
$rows=@($cases|ForEach-Object{ if($_ -ceq 'F6-tb-a-death-fall'){Row $_ $aRow}elseif($_ -ceq 'F6-tb-b-prone-fall'){Row $_ $bRow}elseif($_ -ceq 'F3-tb-setup'){Row $_ ([pscustomobject]@{entry=[pscustomobject]@{aRelationship='Mounted';bRelationship='Mounted'};combat=[pscustomobject]@{turnBased=$true;initiative=[pscustomobject]@{aRider=40;aMount=-40;bRider=-40;bMount=40}}})}else{Row $_ ([pscustomobject]@{})} })
$tbArtifact=[pscustomobject]@{schemaVersion=55;status='PASS';errors=@();subscenarioFailCount=0;rows=$rows;observations=[pscustomobject]@{riderId=$aR;horseId=$aM;finalize6Falls=[pscustomobject]@{
    contract='finalize6-falls-v1';mode='TB';rider=$aR;mount=$aM;secondRider=$bR;secondMount=$bM;cases=$cases
    fallPolicy=[pscustomobject]@{dc=15;damageDice=1;damageDieSides=6;stat='SkillMobility'};fallServiceAtStart=(Svc 0 0)
    disclosedSetup=@([pscustomobject]@{action='The aRider soft fall is labelled to fail (the d20 one below the native DC).'},[pscustomobject]@{action='The bRider soft fall is labelled to pass (the d20 that meets the native DC exactly).'})
    falls=@([pscustomobject]@{label='tb-a-death';record=$deathRecord;log=@('Valerie: Mobility check',$deathRecord.logText)},[pscustomobject]@{label='tb-b-prone';record=$proneRecord;log=@($proneRecord.logText)})
    preparations=@([pscustomobject]@{role='aRider';round=2;count=1},[pscustomobject]@{role='aRider';round=3;count=1})
    screenshots=@(Get-KmcFinalize6ShotNames|ForEach-Object{[pscustomobject]@{name=$_;file=('movement-visuals/finalize3-'+$_+'.png');sha256=('a'*64);bytes=10}})}}}
$tbRequest=[pscustomobject]@{scenario='finalize6-falls-tb'}
Assert-KmcFinalize6FallsEvidence -Request $tbRequest -Artifact $tbArtifact -Status 'PASS'; Check $true 'the turn-based reader accepts the complete synthetic death and prone falls'
Check ((Get-KmcPhase3dEvidenceDispatch 55 'finalize6-falls-tb').validator -ceq 'Assert-KmcFinalize6FallsEvidence' -and (Test-KmcPhase3dSchemaRegistration 55 'finalize6-falls-tb') -and
    -not(Test-KmcPhase3dSchemaRegistration 54 'finalize6-falls-tb') -and -not(Test-KmcPhase3dSchemaRegistration 55 'finalize3-economy-tb')) 'schema 55 belongs to the F6 reader alone and F3 keeps schema 54'
function TbRefuses([scriptblock]$Mutate,[string]$Message){ $a=Clone $tbArtifact; & $Mutate $a; Refuses { Assert-KmcFinalize6FallsEvidence -Request $tbRequest -Artifact $a -Status 'PASS' } $Message }
TbRefuses { param($a) $a.schemaVersion=54 } 'another schema refuses'
TbRefuses { param($a) $a.rows[6].evidence.steps.begunSinceUnseated=1 } 'a second fall from the prone mount''s death refuses'
TbRefuses { param($a) $a.observations.finalize6Falls.falls[0].record.swiftAfter=6 } 'a Swift spent by the fall refuses (no Mounted Combat reaction)'
TbRefuses { param($a) $a.observations.finalize6Falls.falls[0].record.moveBefore=3; $a.observations.finalize6Falls.falls[0].record.moveAfter=0 } 'a refunded Move refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.fallAfter.bPair.generation=2 } 'the other pair changing generation refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.fallAfter.bPair.state='Unmounted' } 'the other pair unseated by this fall refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.fallAfter.service.begun=2 } 'two falls for one death refuse'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[0].record; $r.passed=$true } 'a labelled fail that passed refuses'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[1].record; $r.checkDc=14 } 'a DC other than 15 plus the native adjustment refuses'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[1].record; $r.checkResult=$r.checkResult+1 } 'a result that is not d20 plus Mobility plus bonus refuses'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[0].record; $r.damageDealt=$false } 'a hard landing without the native 1d6 refuses'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[0].record; $r.damageRolled=7 } 'a fall die above six refuses'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[1].record; $r.damageDealt=$true } 'a safe landing that hurt the rider refuses'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[1].record; $r.proneImposed=$true } 'a safe landing that knocked the rider prone refuses'
TbRefuses { param($a) $a.observations.finalize6Falls.falls[0].log=@('Valerie: Mobility check') } 'an explanation missing from the native battle log refuses'
TbRefuses { param($a) $a.observations.finalize6Falls.falls[0].record.riderId=$bR } 'a fall on another rider refuses'
TbRefuses { param($a) $a.observations.finalize6Falls.falls[0].record.generation=2 } 'a fall on another pair generation refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpAfter.aRider.commandsEmpty=$false } 'a stand-up that left the triggering order running refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpAfter.aRider.hasStandard=$false } 'a stand-up that took the Standard action refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.placement.riderToMount=1.2 } 'a rider placed inside the dead mount body refuses'
TbRefuses { param($a) $a.rows[6].evidence.steps.placement.riderToMount=0.3 } 'a rider placed inside the prone mount body refuses'
TbRefuses { param($a) $r=$a.observations.finalize6Falls.falls[0].record; $r.proneImposed=$false; $r.proneRefusal='getting up' } 'a hard landing that imposed no prone refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpBefore.aRider.prone=$false } 'a stand-up row whose rider was never prone refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.nextTurn.round=2 } 'a stand-up not on a later native turn of the rider refuses'
TbRefuses { param($a) $a.observations.finalize6Falls.preparations+=[pscustomobject]@{role='aRider';round=2;count=1} } 'a second native preparation in a round refuses'
TbRefuses { param($a) $a.observations.finalize6Falls.disclosedSetup=@() } 'undisclosed labelled outcomes refuse'
$f=Clone $tbArtifact; $f.status='FAIL'; $f.errors=@('x'); Assert-KmcFinalize6FallsEvidence -Request $tbRequest -Artifact $f -Status 'FAIL'; Check $true 'a failed child keeps its FAIL without qualification'
Refuses { $f=Clone $tbArtifact; $f.status='FAIL'; Assert-KmcFinalize6FallsEvidence -Request $tbRequest -Artifact $f -Status 'PASS' } 'a failed child cannot become PASS'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpBoundary.aRider.move=0 } 'a free stand-up (no Move charged when prone cleared) refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpTravel=2.4 } 'a stand-up whose cost a following walk could explain refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpBoundary.aRider.hasStandard=$false } 'a stand-up that took the Standard action refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpBoundary.aRider.move=6 } 'a stand-up charged more than one Move refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.fallBefore.turnRole='other' } 'a death measured on another actor''s turn refuses'
TbRefuses { param($a) $a.rows[0].evidence.steps.combat.initiative.aMount=50 } 'an encounter without pair A rider-first refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpTurn.sameTurnAsNext=$false } 'a stand-up order outside the rider''s own next turn refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.standUpBoundary.service.begun=2 } 'a stand-up that replayed a fall refuses'
TbRefuses { param($a) $a.rows[5].evidence.steps.fallAfter.aRider.mountedCombat.spent=$true } 'a fall that spent the Mounted Combat reaction refuses'
TbRefuses { param($a) $a.rows[6].evidence.steps.afterDeath.bRider.damage=7 } 'a former rider hurt again through the death callbacks refuses'
TbRefuses { param($a) $a.observations.finalize6Falls.falls[1].record.attempted=$false } 'a safe landing with no actual check refuses'

# ---- the completion variant: the same setup and fall rows without the F3 action rows ---------------------------------
$compRequest=[pscustomobject]@{scenario='finalize6-falls-completion-tb'}
$comp=Clone $tbArtifact; $o=$comp.observations.finalize6Falls; $o.contract='finalize6-falls-completion-v1'; $o.cases=@(Get-KmcFinalize6CompletionCases)
$comp.rows=@($comp.rows | Where-Object { $_.name -cin @(Get-KmcFinalize6CompletionCases) })
Assert-KmcFinalize6FallsEvidence -Request $compRequest -Artifact $comp -Status 'PASS'; Check $true 'the completion reader accepts setup, the death fall with its next-turn stand-up and the safe prone fall'
Check ((Get-KmcPhase3dEvidenceDispatch 55 'finalize6-falls-completion-tb').validator -ceq 'Assert-KmcFinalize6FallsEvidence' -and (Test-KmcPhase3dSchemaRegistration 55 'finalize6-falls-completion-tb')) 'the completion variant dispatches to the F6 reader under schema 55'
function CompRefuses([scriptblock]$Mutate,[string]$Message){ $a=Clone $comp; & $Mutate $a; Refuses { Assert-KmcFinalize6FallsEvidence -Request $compRequest -Artifact $a -Status 'PASS' } $Message }
CompRefuses { param($a) $a.rows=@($a.rows[0],$a.rows[1]) } 'a missing terminal fall row refuses'
CompRefuses { param($a) $a.observations.finalize6Falls.contract='finalize6-falls-v1' } 'the full-route contract under the completion name refuses'
CompRefuses { param($a) $a.observations.finalize6Falls.cases=@(Get-KmcFinalize6TurnBasedCases) } 'claiming the F3 action rows it did not run refuses'
CompRefuses { param($a) $a.observations.finalize6Falls.falls[1].record.riderId=$aR } 'a safe landing on the wrong rider refuses'
CompRefuses { param($a) $r=$a.observations.finalize6Falls.falls[1].record; $r.damageDealt=$true; $r.riderDamageAfter=3 } 'a success that dealt damage refuses'
CompRefuses { param($a) $a.rows[2].evidence.steps.begunSinceUnseated=1 } 'a duplicate prone or death resolution refuses'
CompRefuses { param($a) $a.observations.finalize6Falls.preparations+=[pscustomobject]@{role='bRider';round=2;count=1}; $a.observations.finalize6Falls.preparations+=[pscustomobject]@{role='bRider';round=2;count=1} } 'an extra actor preparation refuses'

# ---- the persistence route ------------------------------------------------------------------------------------------
$dogId='0f6c2a1e-1d3b-4c5a-9e8f-7a6b5c4d3e2f'; $mammothId='9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d'
$dogRider='d17c8fd0-6627-40c4-bf9a-024cf001c1f7'; $mammothRider='2b4d6f80-1a3c-4e5f-8a9b-0c1d2e3f4a5b'
function Ident([string]$Id,[string]$Master,[string]$Blueprint){ [pscustomobject]@{id=$Id;blueprint=$Blueprint;master=$Master;masterOwnsPet=$true;customName=$shared;characterName=$shared;defaultName='x';eligible=$true} }
function Labels { [pscustomobject]@{dog=[pscustomobject]@{id=$dogId;characterName=$shared;customName=$shared};mammoth=[pscustomobject]@{id=$mammothId;characterName=$shared;customName=$shared};writes=0;lastOutcome=$null} }
function Overtip([string]$Id){ [pscustomobject]@{id=$Id;controllerUnit=$Id;characterName=$shared;text=('<font="Saber_Dist32">'+$shared[0]+'</font>'+$shared.Substring(1))} }
function Overtips { [pscustomobject]@{dog=(Overtip $dogId);mammoth=(Overtip $mammothId);writes=0;lastOutcome=$null} }
$tack=[pscustomobject]@{dogBindings=1;mammothBindings=0;adopted=$true;rendererEnabled=$true;sameBones=$true;vertices=600;triangles=900;casts=410;misses=3;meshBuilds=1;attachments=1;backAtSeat=0.89;seatTop=0.913;cantleTop=0.95;pommelTop=0.94
    maxRise=0.05;minClearance=-0.003;maxClearance=0.05;unsupported=0;dogStats=[pscustomobject]@{id=$dogId;size='Medium';armorClass=19;speed=40}}
function PShots($Names){ @($Names|ForEach-Object{ [pscustomobject]@{name=$_;file=('movement-visuals/finalize5-'+$_+'.png');bytes=10;sha256=('0'*64)} }) }
function FUnit([string]$Id,[int]$Damage=0,[bool]$Prone=$false){ [pscustomobject]@{id=$Id;name=$shared;damage=$Damage;life='Conscious';prone=$Prone} }
function FPair([bool]$Mounted,[string]$Owner,$Generation,$RelGen){ [pscustomobject]@{mounted=$Mounted;ownerId=$Owner;generation=$Generation;state=$(if($Mounted){'Mounted'}else{'None'});relationshipGeneration=$RelGen} }
function FWorld($Svc,$First,$Second,[int]$Damage=0,[bool]$RiderProne=$false,[bool]$DogProne=$false,$Log=@()){ [pscustomobject]@{service=$Svc;firstPair=$First;secondPair=$Second;firstRider=(FUnit $mammothRider);firstMount=(FUnit $mammothId);secondRider=(FUnit $dogRider $Damage $RiderProne);secondMount=(FUnit $dogId 0 $DogProne);log=$Log} }
function PSvc([long]$B){ [pscustomobject]@{begun=$B;resolved=$B;abandoned=0;pending=0;records=@()} }
$first=FPair $true 'aaaa' 1 1
$fall=FallRecord 'MountFellProne' $dogRider $dogId 'Linzi' $shared 4 $false 'RT'
$fall.logText=$shared+' falls prone; Linzi is thrown: Mobility '+$fall.checkResult+' vs DC 15 - falls hard, 4 damage, knocked prone.'
$archive=[pscustomobject]@{SchemaVersion=3;CampaignId='camp';AreaId='area';GameTimeTicks=5;Policy='rider-principal-distinct-native-v1';RulesId='crpg-transport-v1'
    Pairs=@([pscustomobject]@{PairId='aaaa';Generation=1;ProfileId='medium-humanoid-mammoth-v1';Rider=[pscustomobject]@{Id=$mammothRider};Mount=[pscustomobject]@{Id=$mammothId}})
    Participations=@();Slots=@([pscustomobject]@{ActorId=$mammothRider;Index=3;Kind=3},[pscustomobject]@{ActorId=$dogRider;Index=4;Kind=3},[pscustomobject]@{ActorId=$dogRider;Index=0;Kind=7});Combat=$null}
$cleanup=[pscustomobject]@{pairsClear=$true;sourceHorseRemoved=$true;controlsSettled=$true;saveCustodyClear=$true;selectionRestored=$true;nativeProgressionRequiresExternalWorkingRestoration=$true;ordinaryRespecProven=$false;world=[pscustomobject]@{owners=@()}}
$base=@{checkpoint='rider-fall-integration-manual';sourceWorkingSha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6';nativeSchema=3;nativeCapacity=6;processId=42
    nativeProgressionRequiresExternalWorkingRestoration=$true;ordinaryRespecProven=$false;combatDebtProof=$false;cleanup=$cleanup}
$prodObs=[pscustomobject]($base+@{cold=$false
    companionNames=[pscustomobject]@{dog=(Ident $dogId $dogRider 'b7a14f25d3c94e0d8e6f1a2c5d90e431');mammoth=(Ident $mammothId $mammothRider 'm');dogDuplicate=(Ident $dogId $dogRider 'b7a14f25d3c94e0d8e6f1a2c5d90e431')
        duplicateLabels=(Labels);atSave=(Labels);overtipsAtSave=(Overtips);tackMounted=$tack;shots=(PShots $script:KmcF6RiderFallProducerShots)}
    riderFall=[pscustomobject]@{contract='finalize6-rider-fall-persistence-v1';policy=[pscustomobject]@{dc=15;damageDice=1;damageDieSides=6;stat='SkillMobility'}
        disclosedSetup=@([pscustomobject]@{action='The Riding Dog pair''s soft fall is labelled to fail (the d20 one below the native DC).'},[pscustomobject]@{action='The Riding Dog was knocked prone by the native knock-off writer; allowed=True.'})
        voluntaryRoute='public-control';voluntaryBefore=(FWorld (PSvc 0) $first (FPair $true 'bbbb' 2 2));voluntaryAfter=(FWorld (PSvc 0) $first (FPair $false $null $null $null))
        remounted=(FWorld (PSvc 0) $first (FPair $true 'cccc' 4 4));fallBefore=(FWorld (PSvc 0) $first (FPair $true 'cccc' 4 4))
        knockOff=[pscustomobject]@{canBeKnockedOff=$true};fallAfter=(FWorld (PSvc 1) $first (FPair $false $null $null $null) 4 $false $true @('Linzi: Mobility check',$fall.logText))
        fall=$fall;placement=[pscustomobject]@{riderToMount=1.0;riderCorpulence=0.3;mountCorpulence=0.6};fallSettled=(FWorld (PSvc 1) $first (FPair $false $null $null $null) 4 $true $true)
        standing=(FWorld (PSvc 1) $first (FPair $false $null $null $null) 4);atSave=(FWorld (PSvc 1) $first (FPair $false $null $null $null) 4)}
    nativeWrite=[pscustomobject]@{nativeType='Manual';sha256=('b'*64);archive=$archive}})
$fixture=[pscustomobject]@{working=[pscustomobject]@{gameId='camp';area='area';sha256='5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6'}}
$saveRequest=[pscustomobject]@{scenario='chunk7-persistence-save';persistenceCase='rider-fall-integration-manual';fixture=$fixture}
$prod=[pscustomobject]@{status='PASS';observations=$prodObs}
Assert-KmcFinalize6RiderFallObservation $saveRequest $prod; Check $true 'the post-fall reader accepts the complete synthetic producer'
Check ((Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot 'runtime\Chunk7PersistenceEvidence.ps1')).Contains('if($Request.persistenceCase-ceq''rider-fall-integration-manual''){Assert-KmcFinalize6RiderFallObservation $Request $Artifact;return}')) 'the persistence dispatch routes the case to the post-fall reader before any two-pair assertion'
function ProdRefuses([scriptblock]$Mutate,[string]$Message){ $a=Clone $prod; & $Mutate $a.observations; Refuses { Assert-KmcFinalize6RiderFallObservation $saveRequest $a } $Message }
ProdRefuses { param($o) $o.riderFall.voluntaryAfter.service.begun=1 } 'a voluntary Dismount that began a fall refuses'
ProdRefuses { param($o) $o.riderFall.remounted.secondPair.relationshipGeneration=2 } 'a remount without a new generation refuses'
ProdRefuses { param($o) $o.riderFall.fallAfter.firstPair.generation=2 } 'the identically named Mastodon pair changing generation refuses'
ProdRefuses { param($o) $o.riderFall.fallAfter.firstPair.mounted=$false } 'the identically named Mastodon pair unseated refuses'
ProdRefuses { param($o) $o.riderFall.standing.service.begun=2 } 'a second fall while standing up refuses'
ProdRefuses { param($o) $o.riderFall.atSave.secondPair.mounted=$true } 'a save that is not the post-fall world refuses'
ProdRefuses { param($o) $o.riderFall.atSave.secondRider.damage=0 } 'a save without the fall damage refuses'
ProdRefuses { param($o) $o.nativeWrite.archive.Pairs+=$o.nativeWrite.archive.Pairs[0] } 'a two-pair archive refuses'
ProdRefuses { param($o) $o.riderFall.fall.logText=$o.riderFall.fall.logText.Replace($shared,'Riding Dog'); $o.riderFall.fallAfter.log=@($o.riderFall.fall.logText) } 'an explanation without the pet''s current name refuses'
ProdRefuses { param($o) $o.cleanup.pairsClear=$false } 'incomplete cleanup refuses'
ProdRefuses { param($o) $o.riderFall.placement.riderToMount=0.4 } 'a rider placed inside the Riding Dog body refuses'
ProdRefuses { param($o) $o.riderFall.fallSettled.secondRider.prone=$false } 'a hard landing that left the rider standing refuses'

$coldObs=[pscustomobject]($base+@{cold=$true
    companionNamesCold=[pscustomobject]@{dog=(Ident $dogId $dogRider 'b7a14f25d3c94e0d8e6f1a2c5d90e431');mammoth=(Ident $mammothId $mammothRider 'm');labels=(Labels);overtipsCold=(Overtips)
        tackCold=$tack;tackRemounted=$tack;sheetDogCold=[pscustomobject]@{renameActive=$true;resetActive=$true};sheetMammothCold=[pscustomobject]@{renameActive=$true;resetActive=$true};sheetClosed=$true
        shots=(PShots $script:KmcF6RiderFallConsumerShots)}
    riderFall=[pscustomobject]@{contract='finalize6-rider-fall-persistence-v1';cold=(FWorld (PSvc 0) $first (FPair $false $null $null $null) 4)
        coldBeforeRemount=(FWorld (PSvc 0) $first (FPair $false $null $null $null) 4);coldRemounted=(FWorld (PSvc 0) $first (FPair $true 'dddd' 5 5) 4)}
    loadedArchive=$archive})
$loadRequest=[pscustomobject]@{scenario='chunk7-persistence-load';persistenceCase='rider-fall-integration-manual';fixture=$fixture}
$coldArtifact=[pscustomobject]@{status='PASS';observations=$coldObs}
Assert-KmcFinalize6RiderFallObservation $loadRequest $coldArtifact; Check $true 'the post-fall reader accepts the complete synthetic consumer'
function ColdRefuses([scriptblock]$Mutate,[string]$Message){ $a=Clone $coldArtifact; & $Mutate $a.observations; Refuses { Assert-KmcFinalize6RiderFallObservation $loadRequest $a } $Message }
ColdRefuses { param($o) $o.riderFall.cold.service.begun=1 } 'a fall replayed by loading the resolved state refuses'
ColdRefuses { param($o) $o.riderFall.cold.secondPair.mounted=$true } 'a cold world that remounted the fallen rider without input refuses'
ColdRefuses { param($o) $o.companionNamesCold.labels.writes=1 } 'names written test-side before observation refuse'
ColdRefuses { param($o) $o.riderFall.coldRemounted.secondPair.mounted=$false } 'a cold Mount control that did not seat the rider refuses'
ColdRefuses { param($o) $o.riderFall.coldRemounted.service.begun=1 } 'a cold remount that began a fall refuses'

$prodRun=[pscustomobject]@{runId='synthetic-producer';scenario='chunk7-persistence-save';persistenceCase='rider-fall-integration-manual';commit='c';branch='b';dllSha256='d';dllMvid='m'}
$coldRun=[pscustomobject]@{scenario='chunk7-persistence-load';persistenceCase='rider-fall-integration-manual';commit='c';branch='b';dllSha256='d';dllMvid='m'
    persistenceLoad=[pscustomobject]@{sha256=('b'*64);length=10;fileName='Manual_300_KMC_C7_WORLD.zks'}}
$prod.observations.nativeWrite | Add-Member length 10; $prod.observations.nativeWrite | Add-Member path 'x\Manual_300_KMC_C7_WORLD.zks'
$prod.observations.riderFall.atSave.firstPair.ownerId='aaaa'
Assert-KmcFinalize6ColdAgainstSource $prodRun $prod $coldRun $coldArtifact; Check $true 'the cross-run reader accepts the consumer of its exact producer'
$c=Clone $coldArtifact; $c.observations.riderFall.cold.secondRider.damage=8; Refuses { Assert-KmcFinalize6ColdAgainstSource $prodRun $prod $coldRun $c } 'fall damage applied again after the reload refuses'
$q=Clone $coldRun; $q.persistenceLoad.sha256=('c'*64); Refuses { Assert-KmcFinalize6ColdAgainstSource $prodRun $prod $q $coldArtifact } 'another source Manual refuses'
$q=Clone $coldRun; $q.dllMvid='other'; Refuses { Assert-KmcFinalize6ColdAgainstSource $prodRun $prod $q $coldArtifact } 'another binary refuses'
$q=Clone $coldRun; $q.commit='other'; Refuses { Assert-KmcFinalize6ColdAgainstSource $prodRun $prod $q $coldArtifact } 'another commit refuses for a producer that is not the pinned one'
Write-Host ('TOTAL PASS='+$passes+' FAIL=0')
