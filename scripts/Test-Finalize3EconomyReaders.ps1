# F3 reader: synthetic positive and targeted negative envelopes for both scenarios. No game, no build. A reader-only
# assertion is never native evidence.
param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime\PersistenceProfileAreasIsolation.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
function Copy-F3($Value){ $Value | ConvertTo-Json -Depth 40 | ConvertFrom-Json }
$ar='11111111-1111-1111-1111-111111111111'; $am='22222222-2222-2222-2222-222222222222'; $br='44444444-4444-4444-4444-444444444444'; $bm='55555555-5555-5555-5555-555555555555'; $hostile='33333333-3333-3333-3333-333333333333'
$id=Get-KmcFinalize3Identities
$speed=5.0; $r1=1
function New-F3Actor([string]$Id,[double]$Standard=0.0,[double]$Move=0.0,[double]$Swift=0.0,[bool]$UsedStandard=$false){ [ordered]@{id=$Id;standard=$Standard;move=$Move;swift=$Swift;usedStandard=$UsedStandard;inCombat=$true;hp=30} }
function New-F3Snap([int]$Round,[hashtable]$Actors=@{},[hashtable]$Change=@{}){
 $s=[ordered]@{frame=100;round=$Round;turnUnit=$ar;turnRole='aRider';turnStatus='Acting';activationA='e:1';activationB='f:1';aRelationship='Mounted';bRelationship='Mounted';selected=$ar;selectedRole='aRider';
  aRider=(New-F3Actor $ar);aMount=(New-F3Actor $am);bRider=(New-F3Actor $br);bMount=(New-F3Actor $bm)}
 foreach($k in $Actors.Keys){ $s[$k]=$Actors[$k] }
 foreach($k in $Change.Keys){ $s[$k]=$Change[$k] }
 $s
}
function New-F3Probe([string]$Role,[double]$Remaining,[double]$Total,[hashtable]$Change=@{}){
 $p=[ordered]@{frame=101;round=1;selectedRole=$(if($Role -ceq 'bMount'){'bRider'}else{'aRider'});unit=$(if($Role -ceq 'bMount'){$bm}else{$am});unitRole=$Role;unitIsMount=$true;moveActionCooldown=0.0;movePrediction='Available';standardPrediction='Available';
  remaining=$Remaining;remainingTotal=$Total;singleActionMove=$false;fiveFootStep=$false;mountSpeed=$speed;mountCombatSpeed=$speed;
  partner=[ordered]@{timeMoved=0.0;timeForced=0.0;timeStepped=0.0;metresStepped=0.0;modifyLimit=$false;enabledStep=$false;enabledSingle=$false;remaining=$Remaining;remainingTotal=$Total;stepRange=1.5};
  allocation=[ordered]@{timeMoved=0.0;timeForced=0.0;timeStepped=0.0;metresStepped=0.0};path=[ordered]@{state='Complete';points=4}}
 foreach($k in $Change.Keys){ $p[$k]=$Change[$k] }
 $p
}
function New-F3Click([string]$Role,[hashtable]$Extra=@{}){ $c=[ordered]@{clicked=$true;selectedRole=$Role;frame=120}; foreach($k in $Extra.Keys){ $c[$k]=$Extra[$k] }; $c }
function New-F3Step($Before,$After,[double]$Delta,[hashtable]$Extra=@{}){ $s=[ordered]@{before=$Before;after=$After;mountDelta=$Delta}; foreach($k in $Extra.Keys){ $s[$k]=$Extra[$k] }; $s }
function New-F3Tracker([string]$Label,[int]$Round,[int]$BMount=0){ [ordered]@{label=$Label;round=$Round;frame=200;source='live';entries=@($ar,$br);aRider=1;aMount=0;bRider=1;bMount=$BMount;hiddenAMount=$true;hiddenBMount=($BMount -eq 0);bRelationship='Mounted'} }
function New-F3Shot([string]$Name){ [ordered]@{name=$Name;requestedFrame=1;frame=3;ummClosedForCapture=$true;ummOpenAtCapture=$false;file=('movement-visuals/finalize3-'+$Name+'.png');sha256=('a'*64);bytes=100;width=1920;height=1080} }
function New-F3Measure([string]$Mode,$Cases,$Shots,[hashtable]$Extra=@{}){
 $m=[ordered]@{contract='finalize3-movement-economy-v1';mode=$Mode;rider=$ar;mount=$am;secondRider=$br;secondMount=$bm;secondPairSource='existing-pet';cases=@($Cases);startRelationship='Unmounted';
  names=[ordered]@{aRider='Valerie';aMount='Arrow';bRider='Harrim';bMount='Blaze'};stockCharge=$id.stockCharge;mountedChargeShell=$id.mountedChargeShell;
  disclosedSetup=@([ordered]@{case='x';frame=1;action='disclosed setup'});screenshots=$Shots;attackRules=@()}
 foreach($k in $Extra.Keys){ $m[$k]=$Extra[$k] }
 $m
}
function New-F3Envelope($Rows,$Measure){
 $list=@(); foreach($pair in $Rows){ $list+=[ordered]@{name=$pair[0];status='PASS';detail='recorded';evidence=$pair[1]} }
 Copy-F3 ([ordered]@{schemaVersion=54;status='PASS';errors=@();subscenarioPassCount=$list.Count;subscenarioFailCount=0;rows=$list;observations=[ordered]@{riderId=$ar;horseId=$am;finalize3Economy=$Measure}})
}

function New-F3TbArtifact {
 $zero=New-F3Snap $r1
 $setup=[ordered]@{case='F3-tb-setup';steps=[ordered]@{entry=(New-F3Snap 0 @{aRider=[ordered]@{id=$ar;standard=0.0;move=0.0;swift=0.0;usedStandard=$false;inCombat=$false}});
  combat=[ordered]@{snap=$zero;initiative=[ordered]@{aRider=52;aMount=-31;bRider=-35;bMount=47};sorted=@('aRider','bMount','other','aMount','bRider');turnBased=$true;targetId=$hostile}}}
 # Pair A, round one.
 $aMoved=New-F3Snap $r1 @{aMount=(New-F3Actor $am 0.0 1.5)}
 $aMountAttacked=New-F3Snap $r1 @{aMount=(New-F3Actor $am 6.0 1.5)}
 $aRiderAttacked=New-F3Snap $r1 @{aMount=(New-F3Actor $am 6.0 1.5);aRider=(New-F3Actor $ar 6.0 3.0)}
 $a=[ordered]@{case='F3-tb-a-round1';steps=[ordered]@{
  start=[ordered]@{snap=$zero;probe=(New-F3Probe 'aMount' 15.0 30.0);participation=[ordered]@{present=$true;mountPrepared=$true};tracker=(New-F3Tracker 'a-round1-start' $r1);surprise=$false};
  a2=(New-F3Step $zero $aMoved 7.4 @{probe=(New-F3Probe 'aMount' 7.5 22.5);requestedMetres=7.5});a2Click=(New-F3Click 'aRider');
  a3=(New-F3Step $aMoved $aMountAttacked 0.0 @{actorRules=1;attackObserved=$true});a3Click=(New-F3Click 'aMount');
  a4=(New-F3Step $aMountAttacked $aRiderAttacked 0.0 @{actorRules=2;attackObserved=$true});a4Click=(New-F3Click 'aRider');
  a4Refusal=(New-F3Step $aRiderAttacked $aRiderAttacked 0.0 @{actorRules=0;feedback='Valerie has no Standard action left this turn.';trace=('actor='+$ar+';riderStandard=6;riderMove=3;mountStandard=6;mountMove=1.5');feedbackBefore='x';traceBefore='not-observed'});
  a4RefusalClick=(New-F3Click 'aRider');end=[ordered]@{snap=$zero;endedNatively=$true}}}
 # Pair B, round one (mount-first arrangement).
 $bStart=New-F3Snap $r1 @{} @{turnRole='bRider';selected=$br;selectedRole='bRider'}
 $bFull=New-F3Snap $r1 @{bRider=(New-F3Actor $br 6.0 3.0)} @{turnRole='bRider'}
 $bMoved=New-F3Snap $r1 @{bRider=(New-F3Actor $br 6.0 3.0);bMount=(New-F3Actor $bm 0.0 1.2)} @{turnRole='bRider'}
 $b=[ordered]@{case='F3-tb-b-round1';steps=[ordered]@{
  start=[ordered]@{snap=$bStart;probe=(New-F3Probe 'bMount' 15.0 30.0);participation=[ordered]@{present=$true;mountPrepared=$true};tracker=(New-F3Tracker 'b-round1-start' $r1)};
  b1=(New-F3Step $bStart $bFull 0.0 @{probe=(New-F3Probe 'bMount' 15.0 30.0);actorRules=2;attackObserved=$true});b1Click=(New-F3Click 'bRider');
  b2=(New-F3Step $bFull $bMoved 6.0 @{probe=(New-F3Probe 'bMount' 9.0 24.0);requestedMetres=6.0});b2Click=(New-F3Click 'bRider');end=[ordered]@{snap=$bStart;endedNatively=$true}}}
 # Pair A, round two: five-foot step, then a refused ordinary move.
 $s0=New-F3Snap ($r1+1)
 $stepProbe=New-F3Probe 'aMount' 0.0 0.0 @{partner=[ordered]@{timeMoved=0.3;timeForced=0.0;timeStepped=0.3;metresStepped=1.0;modifyLimit=$false;enabledStep=$false;enabledSingle=$false;remaining=0.0;remainingTotal=0.0;stepRange=0.0}}
 $st=[ordered]@{case='F3-tb-a-step';steps=[ordered]@{
  start=[ordered]@{snap=$s0;probe=(New-F3Probe 'aMount' 15.0 30.0);tracker=(New-F3Tracker 'a-round2-start' ($r1+1))};
  s1=(New-F3Step $s0 $s0 1.0 @{changedContext='current';probe=$stepProbe;contextsBefore=[ordered]@{current=[ordered]@{modifyLimit=$false};partner=[ordered]@{modifyLimit=$false}}});s1Click=(New-F3Click 'aRider');
  s2=(New-F3Step $s0 $s0 0.0 @{probe=$stepProbe});s2Click=(New-F3Click 'aRider' @{clicked=$false});end=[ordered]@{snap=$s0;endedNatively=$true}}}
 # Pair B, round two: the ordinary Charge, nothing after it, then the mount's retained Standard.
 $c0=New-F3Snap ($r1+1) @{} @{turnRole='bRider'}
 $c1=New-F3Snap ($r1+1) @{bRider=(New-F3Actor $br 6.0 3.0);bMount=(New-F3Actor $bm 0.0 0.0)} @{turnRole='bRider'}
 $c4=New-F3Snap ($r1+1) @{bRider=(New-F3Actor $br 6.0 3.0);bMount=(New-F3Actor $bm 6.0 0.0)} @{turnRole='bRider'}
 $chargeProbe=New-F3Probe 'bMount' 0.0 0.0 @{partner=[ordered]@{timeMoved=2.0;timeForced=2.0;timeStepped=0.0;metresStepped=0.0;modifyLimit=$false;enabledStep=$false;enabledSingle=$false;remaining=0.0;remainingTotal=0.0;stepRange=0.0}}
 $c=[ordered]@{case='F3-tb-b-charge';steps=[ordered]@{
  start=[ordered]@{snap=$c0;probe=(New-F3Probe 'bMount' 15.0 30.0);tracker=(New-F3Tracker 'b-round2-start' ($r1+1))};
  c1=(New-F3Step $c0 $c1 10.0 @{click=[ordered]@{stockPresent=$true;slotSource='pre-existing';index=20;slotAbilityBefore=$id.stockCharge;slotAbilityAfter=$id.stockCharge;handlerAfter=$id.mountedChargeShell;handlerIsShell=$true;clicked=$true;castDelta=1;admittedDelta=0;shell=$true;shellTarget=$hostile;targetId=$hostile};
   chargeRules=1;riderRules=1;peakPartnerForced=2.0;peakAllocationForced=2.0;allocation=[ordered]@{timeMoved=2.0;timeForced=2.0;timeStepped=0.0;metresStepped=0.0};terminal=[ordered]@{result='Success';sequenceLawful=$true;revalidationFailed=$false}});
  c2=[ordered]@{snap=$c1;probe=$chargeProbe};
  c3Move=(New-F3Step $c1 $c1 0.0 @{probe=$chargeProbe});c3MoveClick=(New-F3Click 'bRider' @{clicked=$false});
  c3Step=(New-F3Step $c1 $c1 0.02 @{probe=$chargeProbe});c3StepClick=(New-F3Click 'bRider' @{clicked=$false});
  c4=(New-F3Step $c1 $c4 0.0 @{actorRules=1;attackObserved=$true});c4Click=(New-F3Click 'bMount');end=[ordered]@{snap=$c0;endedNatively=$true}}}
 # Pair A, round three: into the second Move, then the mount's refused attack.
 $d0=New-F3Snap ($r1+2)
 $d1=New-F3Snap ($r1+2) @{aMount=(New-F3Actor $am 6.0 4.2 0.0 $true)}
 $d=[ordered]@{case='F3-tb-a-double';steps=[ordered]@{
  start=[ordered]@{snap=$d0;probe=(New-F3Probe 'aMount' 15.0 30.0);tracker=(New-F3Tracker 'a-round3-start' ($r1+2))};
  d1=(New-F3Step $d0 $d1 21.0 @{probe=(New-F3Probe 'aMount' 0.0 9.0)});d1Click=(New-F3Click 'aRider');
  d2=(New-F3Step $d1 $d1 0.0 @{actorRules=0;feedback='Arrow has no Standard action left this turn.';trace=('actor='+$am+';riderStandard=0;riderMove=0;mountStandard=6;mountMove=4.2')});d2Click=(New-F3Click 'aMount');end=[ordered]@{snap=$d0;endedNatively=$true}}}
 # Pair B, round three: dismount; the released mount's own turn next round.
 $x0=New-F3Snap ($r1+2) @{} @{turnRole='bRider'}
 $x1=New-F3Snap ($r1+2) @{} @{turnRole='bRider';bRelationship='Unmounted'}
 $x=[ordered]@{case='F3-tb-b-dismount';dismountRound=($r1+2);steps=[ordered]@{
  start=[ordered]@{snap=$x0;probe=(New-F3Probe 'bMount' 15.0 30.0);participation=[ordered]@{present=$true;mountPrepared=$true};tracker=(New-F3Tracker 'b-round3-start' ($r1+2))};
  actingSetup=(New-F3Step $x0 $x0 2.0);actingSetupClick=(New-F3Click 'bRider');
  dismount=(New-F3Step $x0 $x1 0.0 @{route='dismount-shell';dismountRound=($r1+2);dismountFrame=5000;participation=[ordered]@{present=$true};tracker=(New-F3Tracker 'b-round3-after-dismount' ($r1+2))});
  end=[ordered]@{snap=$x1;endedNatively=$true};round4Tracker=(New-F3Tracker 'round-after-dismount' ($r1+3) 1);
  mountOwnTurn=[ordered]@{snap=$x1;round=($r1+3)};nextRiderTurn=[ordered]@{snap=$x1;round=($r1+3)}}}
 $turns=@(); $seq=0
 foreach($k in $r1..($r1+2)){ foreach($role in 'aRider','other','bRider'){ $seq++; $turns+=[ordered]@{sequence=$seq;round=$k;role=$role;unit='u';frame=$seq*10} } }
 foreach($role in 'bMount','aRider','other','bRider'){ $seq++; $turns+=[ordered]@{sequence=$seq;round=($r1+3);role=$role;unit='u';frame=$seq*10} }
 $prep=@(); foreach($k in $r1..($r1+2)){ foreach($role in 'aRider','aMount','bRider','bMount'){ $prep+=[ordered]@{round=$k;role=$role;unit='u';count=1;frame=$k*1000} } }
 $prep+=[ordered]@{round=($r1+3);role='bMount';unit='u';count=1;frame=9000}
 $trackers=@((New-F3Tracker 'a-round1-start' $r1),(New-F3Tracker 'b-round1-start' $r1),(New-F3Tracker 'a-round2-start' ($r1+1)),(New-F3Tracker 'b-round2-start' ($r1+1)),
  (New-F3Tracker 'a-round3-start' ($r1+2)),(New-F3Tracker 'b-round3-start' ($r1+2)),(New-F3Tracker 'b-round3-after-dismount' ($r1+2)),(New-F3Tracker 'round-after-dismount' ($r1+3) 1),(New-F3Tracker 'mount-own-turn' ($r1+3) 1))
 $shots=@(Get-KmcFinalize3ShotNames 'TB' | ForEach-Object { New-F3Shot $_ })
 $measure=New-F3Measure 'TB' (Get-KmcFinalize3TurnBasedCases) $shots @{turnStarts=$turns;preparations=$prep;trackers=$trackers;endTurns=@();initiativeSetup=[ordered]@{aRider=40;aMount=-40;bRider=-40;bMount=40}}
 New-F3Envelope @(@('F3-tb-setup',$setup),@('F3-tb-a-round1',$a),@('F3-tb-b-round1',$b),@('F3-tb-a-step',$st),@('F3-tb-b-charge',$c),@('F3-tb-a-double',$d),@('F3-tb-b-dismount',$x)) $measure
}

function New-F3RtArtifact {
 $before=New-F3Snap 0 @{} @{turnUnit=$null;turnRole=$null}
 $marker=[ordered]@{role='aMount';framesWatched=40;visibleFrames=40;maxAlpha=1.0;minAlpha=1.0;last=[ordered]@{present=$true;visible=$true;alpha=1.0}}
 $m=[ordered]@{case='F3-rt-mounted-marker';steps=[ordered]@{move=(New-F3Step $before $before 6.0 @{riderFrom=[ordered]@{x=0;y=0;z=0};whileMoving=[ordered]@{selected=$ar};marker=$marker;riderDelta=6.0;riderMountSeparation=0.2});moveClick=(New-F3Click 'aRider')}}
 $u=[ordered]@{case='F3-rt-unmounted-marker';steps=[ordered]@{controlMove=[ordered]@{before=$before;riderMounted=$false;whileMoving=[ordered]@{selected=$br};after=$before;riderDelta=6.0;marker=[ordered]@{role='bRider';framesWatched=30;visibleFrames=30;maxAlpha=1.0;minAlpha=1.0}};controlMoveClick=(New-F3Click 'bRider')}}
 $t=[ordered]@{case='F3-rt-attack';steps=[ordered]@{attack=(New-F3Step $before $before 0.0 @{actorRules=1;rule=[ordered]@{attacker=$ar;target=$hostile;charge=$false}});attackClick=(New-F3Click 'aRider')}}
 $measure=New-F3Measure 'RT' (Get-KmcFinalize3RealTimeCases) @(New-F3Shot 'rt-marker')
 New-F3Envelope @(@('F3-rt-mounted-marker',$m),@('F3-rt-unmounted-marker',$u),@('F3-rt-attack',$t)) $measure
}

$tbRequest=[pscustomobject]@{scenario='finalize3-economy-tb'}; $rtRequest=[pscustomobject]@{scenario='finalize3-economy-rt'}
function Accept($Artifact,$Request){ try{ $null=Assert-KmcFinalize3EconomyEvidence $Request $Artifact 'PASS'; $true }catch{ Write-Output ('    unexpected: '+$_.Exception.Message); $false } }
function Reject($Artifact,[string]$Name,$Request,[string]$Expect){
 try{ $null=Assert-KmcFinalize3EconomyEvidence $Request $Artifact 'PASS'; Check $false $Name }
 catch{ $m=$_.Exception.Message; Check ([string]::IsNullOrEmpty($Expect) -or $m.Contains($Expect)) ($Name+' ('+$m+')') }
}
function Row($Artifact,[string]$Name){ @($Artifact.rows | Where-Object name -CEQ $Name)[0].evidence }

$tb=New-F3TbArtifact; $rt=New-F3RtArtifact
Check (Accept $tb $tbRequest) 'complete turn-based F3 evidence accepted'
Check (Accept $rt $rtRequest) 'complete real-time F3 evidence accepted'
# Registration
foreach($s in 'finalize3-economy-tb','finalize3-economy-rt'){
 Check (Test-KmcPhase3dSchemaRegistration 54 $s) ($s+' registers schema 54')
 Check (-not (Test-KmcPhase3dSchemaRegistration 53 $s)) ('the F2 schema is refused for '+$s)
 Check ((Get-KmcPhase3dEvidenceDispatch 54 $s).validator -ceq 'Assert-KmcFinalize3EconomyEvidence') ('schema 54 dispatches '+$s+' to the F3 reader')
 Check (Test-KmcCompoundRuntimeScenario $s) ($s+' reports case rows')
 Check (-not (Test-KmcChildEntryExpectsMounted $s)) ($s+' starts from its unmounted fixture handoff')
 Check (Test-KmcChildEntryRequiresIdleParty $s) ($s+' requires an idle party')
 Check ($s -cin (Get-KmcSaveBackedRuntimeScenarios)) ($s+' is a save-backed runtime scenario')
 Check (Test-KmcPersistenceAreasIsolationSelection $s) ($s+' selects the persistence Areas isolation')
}
Check (-not (Test-KmcPhase3dSchemaRegistration 54 'finalize-f2-controls-rt')) 'the F3 schema is refused for the F2 scenario'
Check (Test-KmcPhase3dSchemaRegistration 53 'finalize-f2-controls-rt') 'the F2 registration is unchanged'
Check ((Get-KmcPhase3dEvidenceDispatch 53 'finalize-f2-controls-rt').validator -ceq 'Assert-KmcFinalize2ControlsEvidence') 'the F2 dispatch is unchanged'
$knownRows=@(Get-KmcPhase3dHorseRuntimeRows)
Check (@(@(Get-KmcFinalize3TurnBasedCases) + @(Get-KmcFinalize3RealTimeCases) + 'finalize3-economy-tb' + 'finalize3-economy-rt' | Where-Object { $_ -cnotin $knownRows }).Count -eq 0) 'the F3 rows and scenarios are known subscenarios'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$protocol=[IO.File]::ReadAllText((Join-Path $repo 'src\KingmakerMountedCombat\Diagnostics\RuntimeProtocol.cs'))
Check (([regex]::Matches($protocol,'"finalize3-economy-tb"')).Count -eq 2 -and ([regex]::Matches($protocol,'"finalize3-economy-rt"')).Count -eq 2 -and @(@(Get-KmcFinalize3TurnBasedCases) + @(Get-KmcFinalize3RealTimeCases) | Where-Object { -not $protocol.Contains('"'+$_+'"') }).Count -eq 0) 'the compiled protocol registers both scenarios and their rows'
$scenarioSource=[IO.File]::ReadAllText((Join-Path $repo 'src\KingmakerMountedCombat\Diagnostics\Finalize3EconomyScenario.cs'))
Check (@(@(Get-KmcFinalize3TurnBasedCases) + @(Get-KmcFinalize3RealTimeCases) + @(Get-KmcFinalize3ShotNames 'TB') + @(Get-KmcFinalize3ShotNames 'RT') | Where-Object { -not $scenarioSource.Contains('"'+$_+'"') }).Count -eq 0 -and $scenarioSource.Contains('Finalize3SchemaVersion = 54')) 'the scenario source names the reader''s rows, shots and schema'
$launcher=[IO.File]::ReadAllText((Join-Path $repo 'scripts\runtime\Invoke-KingmakerRuntimeScenario.ps1')); $requestValidator=[IO.File]::ReadAllText((Join-Path $repo 'scripts\runtime\Test-RuntimeRequest.ps1'))
Check ($launcher.Contains("'finalize3-economy-tb','finalize3-economy-rt'") -and $requestValidator.Contains("'finalize3-economy-tb','finalize3-economy-rt'")) 'the launcher and the external request validator admit both scenarios'
# Envelope
$r=Copy-F3 $tb; $r.schemaVersion=53; Reject $r 'a schema-53 artifact refuses' $tbRequest 'scenario/schema'
Reject $tb 'a turn-based artifact read as the real-time scenario refuses' $rtRequest
$r=Copy-F3 $tb; $r.status='FAIL'; $r.subscenarioFailCount=1; $r.errors=@('F3-tb-a-step: failed'); try { Assert-KmcFinalize3EconomyEvidence $tbRequest $r 'FAIL'; Check $true 'an explicit failed prefix is preserved without qualification' } catch { Check $false 'an explicit failed prefix is preserved without qualification' }
$r=Copy-F3 $tb; $r.status='FAIL'; $r.subscenarioFailCount=1; Reject $r 'a failed artifact cannot be reported PASS' $tbRequest
$r=Copy-F3 $tb; $r.rows=@($r.rows | Where-Object name -CNE 'F3-tb-b-charge'); Reject $r 'a missing row refuses' $tbRequest
$r=Copy-F3 $tb; $r.observations.finalize3Economy.disclosedSetup=@(); Reject $r 'undisclosed setup refuses' $tbRequest
$r=Copy-F3 $tb; $r.observations.finalize3Economy.secondRider=$ar; Reject $r 'a second pair that is the first pair refuses' $tbRequest
# Missing observations are failures, never passes
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.PSObject.Properties.Remove('a2'); Reject $r 'a missing step observation refuses' $tbRequest 'step observation missing: a2'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.start.probe.PSObject.Properties.Remove('remaining'); Reject $r 'a missing preview field refuses' $tbRequest 'A1 remaining'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.start.probe | Add-Member error 'TargetInvocationException: null' -Force; Reject $r 'a failed preview probe refuses' $tbRequest 'preview probe failed'
# HUD-01 / ECON-01 preview
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.start.probe.unitRole='aRider'; (Row $r 'F3-tb-a-round1').steps.start.probe.unitIsMount=$false; Reject $r 'preview Unit is the rider refuses' $tbRequest 'A1 preview unit is the mount'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-round1').steps.b1.probe.unitRole='bRider'; Reject $r 'B1 preview Unit is the rider refuses' $tbRequest 'B1 preview unit is the mount'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.start.probe.remaining=1.5; Reject $r 'a rider-side five-foot range at A1 refuses' $tbRequest 'A1 green range'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.a2.probe.remaining=15.0; Reject $r 'a green range that ignores the mount Move spent refuses' $tbRequest 'A2 green range'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.a2.after.aRider.move=1.5; Reject $r 'a rider Move debit for the mount travel refuses' $tbRequest 'A2: aRider move unchanged'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.a2.after.aMount.move=0.0; Reject $r 'an uncharged mount Move refuses' $tbRequest 'A2 mount Move charged'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-round1').steps.b1.probe.remaining=1.5; (Row $r 'F3-tb-b-round1').steps.b1.probe.fiveFootStep=$true; Reject $r 'a five-foot step after the rider full attack refuses' $tbRequest 'B1 green range'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-round1').steps.b2.mountDelta=1.4; Reject $r 'a rider-selected move held to a five-foot step refuses' $tbRequest 'B2 the mount travelled'
# ECON-02 step and D4 charge
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-step').steps.s1.probe.remaining=7.5; Reject $r 'step leaves range > 0 refuses' $tbRequest 'S1 no movement range'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-step').steps.s1.changedContext='partner'; Reject $r 'step key off the native input context refuses' $tbRequest 'S1 step key held on the native input context'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-step').steps.s1.after.aMount.move=0.3; Reject $r 'a step that spent Move refuses' $tbRequest 'S1: aMount move unchanged'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-step').steps.s2.mountDelta=4.0; Reject $r 'an ordinary move after the step refuses' $tbRequest 'S2 ordinary move refused'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-charge').steps.c2.probe.remaining=6.0; Reject $r 'charge leaves range > 0 refuses' $tbRequest 'C2 no movement range'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-charge').steps.c2.probe.fiveFootStep=$true; (Row $r 'F3-tb-b-charge').steps.c2.probe.partner.stepRange=1.5; Reject $r 'a five-foot step after the charge refuses' $tbRequest 'C2 no five-foot step'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-charge').steps.c1.allocation.timeForced=0.0; Reject $r 'an unforced charge travel refuses' $tbRequest 'C1 mount travel forced'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-charge').steps.c1.click.handlerIsShell=$false; Reject $r 'an ordinary Charge that missed the mounted shell refuses' $tbRequest 'C1 the ordinary Charge carried'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-charge').steps.c1.after.bMount.standard=6.0; Reject $r 'a mount that paid the rider Charge refuses' $tbRequest 'C1 mount Standard retained'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-charge').steps.c3Step.mountDelta=1.0; Reject $r 'a five-foot step after the charge that moved refuses' $tbRequest 'C3 c3Step refused'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-charge').steps.c4.actorRules=0; Reject $r 'a retained mount Standard that could not attack refuses' $tbRequest 'C4 the mount attacked'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-double').steps.d1.after.aMount.usedStandard=$false; Reject $r 'a double move without the native Standard conversion refuses' $tbRequest 'D1 native Standard conversion'
# Item 8 refusals
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.a4Refusal.feedback='No Standard action remains for this actor''s attack.'; Reject $r 'refusal not naming the actor refuses' $tbRequest 'A4 refusal names the rider'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-double').steps.d2.feedback='Valerie has no Standard action left this turn.'; Reject $r 'mount refusal naming the rider refuses' $tbRequest 'D2 refusal names the mount'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-double').steps.d2.trace=('actor='+$ar+';riderStandard=0'); Reject $r 'a refusal trace for the other actor refuses' $tbRequest 'D2 refusal trace names the mount'
$r=Copy-F3 $tb; (Row $r 'F3-tb-a-round1').steps.a4Refusal.actorRules=1; Reject $r 'a refused attack that attacked refuses' $tbRequest 'A4 second rider attack made no attack'
# TURN-01 tracker and native turns
$r=Copy-F3 $tb; $r.observations.finalize3Economy.trackers[2].aMount=1; Reject $r 'mount entry present in tracker while mounted refuses' $tbRequest 'no mount entry while mounted'
$r=Copy-F3 $tb; $r.observations.finalize3Economy.trackers[7].bMount=0; Reject $r 'a released mount missing from the next-round tracker refuses' $tbRequest 'the released mount is in the tracker again'
$r=Copy-F3 $tb; $r.observations.finalize3Economy.turnStarts+=[pscustomobject]@{sequence=99;round=2;role='aMount';unit='u';frame=1}; Reject $r 'mount native turn while mounted refuses' $tbRequest 'pair A mount never owned a native turn'
$r=Copy-F3 $tb; $r.observations.finalize3Economy.turnStarts+=[pscustomobject]@{sequence=99;round=3;role='bMount';unit='u';frame=1}; Reject $r 'released mount turn in its dismount round refuses' $tbRequest 'pair B mount owned no native turn'
$r=Copy-F3 $tb; $r.observations.finalize3Economy.turnStarts=@($r.observations.finalize3Economy.turnStarts | Where-Object { -not ($_.round -eq 4 -and $_.role -ceq 'bMount') }); Reject $r 'a released mount with no own turn next round refuses' $tbRequest 'exactly one native turn the next round'
$r=Copy-F3 $tb; $r.observations.finalize3Economy.turnStarts+=[pscustomobject]@{sequence=99;round=2;role='bRider';unit='u';frame=1}; Reject $r 'a second rider turn in one round refuses' $tbRequest 'pair B rider owned exactly one'
$r=Copy-F3 $tb; $r.observations.finalize3Economy.preparations+=[pscustomobject]@{round=3;role='bMount';unit='u';count=1;frame=6000}; Reject $r 'a second preparation after the dismount refuses' $tbRequest 'prepared at most once'
$r=Copy-F3 $tb; (Row $r 'F3-tb-setup').steps.combat.initiative.bMount=-50; Reject $r 'pair B not mount-first refuses' $tbRequest 'pair B arranged mount-first'
$r=Copy-F3 $tb; (Row $r 'F3-tb-b-round1').steps.start.snap.round=2; Reject $r 'pair B a round late refuses' $tbRequest 'pair B acts in the same round'
# Screenshots
$r=Copy-F3 $tb; $r.observations.finalize3Economy.screenshots[1].ummOpenAtCapture=$true; Reject $r 'a capture under the mod-manager window refuses' $tbRequest 'mod-manager window closed'
$r=Copy-F3 $tb; $r.observations.finalize3Economy.screenshots=@($r.observations.finalize3Economy.screenshots | Select-Object -First 4); Reject $r 'a missing screenshot refuses' $tbRequest 'screenshots recorded'
# HUD-03 real time
$r=Copy-F3 $rt; (Row $r 'F3-rt-mounted-marker').steps.move.marker.maxAlpha=0.5; Reject $r 'RT marker alpha 0.5 refuses' $rtRequest 'RT the mount marker uses the selected style'
$r=Copy-F3 $rt; (Row $r 'F3-rt-mounted-marker').steps.move.marker.visibleFrames=0; Reject $r 'an unshown RT marker refuses' $rtRequest 'was shown while moving'
$r=Copy-F3 $rt; (Row $r 'F3-rt-mounted-marker').steps.move.PSObject.Properties.Remove('marker'); Reject $r 'a missing RT marker observation refuses' $rtRequest
$r=Copy-F3 $rt; (Row $r 'F3-rt-unmounted-marker').steps.controlMove.marker.maxAlpha=0.5; Reject $r 'an unmounted control marker at 0.5 refuses' $rtRequest 'native selected marker'
$r=Copy-F3 $rt; (Row $r 'F3-rt-mounted-marker').steps.move.riderMountSeparation=5.0; Reject $r 'a rider left behind refuses' $rtRequest 'rider position follows'
$r=Copy-F3 $rt; (Row $r 'F3-rt-attack').steps.attack.actorRules=0; Reject $r 'an RT attack that never resolved refuses' $rtRequest 'RT ordinary rider attack completed'
# Screenshot files under an evidence root
$root=Join-Path ([IO.Path]::GetTempPath()) ('kmc-f3-reader-'+[guid]::NewGuid().ToString('N'))
try {
 New-Item -ItemType Directory -Path (Join-Path $root 'movement-visuals') -Force | Out-Null
 $withFiles=Copy-F3 $rt
 foreach($shot in $withFiles.observations.finalize3Economy.screenshots){
  $path=Join-Path $root ($shot.file -replace '/','\'); [IO.File]::WriteAllBytes($path,[Text.Encoding]::ASCII.GetBytes('png:'+$shot.name))
  $shot.sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
 }
 $rooted=[pscustomobject]@{scenario='finalize3-economy-rt';evidenceRoot=$root}
 Check (Accept $withFiles $rooted) 'screenshots on disk with their recorded hashes accepted'
 $r=Copy-F3 $withFiles; $r.observations.finalize3Economy.screenshots[0].sha256=('4'*64); Reject $r 'a screenshot hash that differs from the file refuses' $rooted 'hash matches'
 Remove-Item -LiteralPath (Join-Path $root 'movement-visuals\finalize3-rt-marker.png')
 Reject (Copy-F3 $withFiles) 'a screenshot file missing from the evidence root refuses' $rooted 'file present'
} finally { if(Test-Path -LiteralPath $root){ Remove-Item -LiteralPath $root -Recurse -Force } }
Write-Output ('F3 readers: PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
