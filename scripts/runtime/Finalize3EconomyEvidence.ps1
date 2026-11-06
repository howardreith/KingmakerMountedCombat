# F3 (HUD-01/02/03, ECON-01/02, TURN-01, item 8) external reader for finalize3-economy-tb and finalize3-economy-rt. The rows
# only record native observations; PASS is decided here, strictly: a missing observation is a failure, never a pass. Every
# expectation is one named assertion, so a refusal names what the evidence did not show.
function Get-KmcFinalize3TurnBasedCases { @('F3-tb-setup','F3-tb-a-round1','F3-tb-b-round1','F3-tb-a-step','F3-tb-b-charge','F3-tb-a-double','F3-tb-b-dismount') }
function Get-KmcFinalize3RealTimeCases { @('F3-rt-mounted-marker','F3-rt-unmounted-marker','F3-rt-attack') }
function Get-KmcFinalize3ShotNames([string]$Mode) { if($Mode -ceq 'TB'){ @('tracker-round1','a2-after-move','b1-after-genuine-full-attack','b2-after-mount-move','s1-after-step','c2-after-charge') } else { @('rt-marker') } }
function Get-KmcFinalize3Identities { [ordered]@{ stockCharge='c78506dd0e14f7c45a599990e4e65038'; mountedChargeShell='d79eaec224a7a832e738eb81baef9d49'; noStandard=' has no Standard action left this turn.' } }
function Assert-KmcFinalize3EconomyEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 function Fail([string]$Why){ throw ('F3 '+$Why) }
 function P($O,[string]$N){ if($null -ne $O -and $O -isnot [string] -and $O -isnot [ValueType] -and $null -ne $O.PSObject.Properties[$N]){ $O.$N } else { $null } }
 function Num($V,[string]$What){ if($V -isnot [int] -and $V -isnot [long] -and $V -isnot [double] -and $V -isnot [single] -and $V -isnot [decimal]){ Fail ($What+' is missing or not a number') }; [double]$V }
 function Int($V,[string]$What){ if($V -isnot [int] -and $V -isnot [long]){ Fail ($What+' is missing or not an integer') }; [long]$V }
 function Text($V){ if($V -is [string]){ $V } else { $null } }
 function Need([bool]$Condition,[string]$Name){ if(-not $Condition){ Fail ('assertion failed: '+$Name) } }
 function Same([double]$A,[double]$B){ [math]::Abs($A-$B) -le 0.001 }
 # Preview ranges: within 5% of the expectation (0.1 m floor for the grid snap).
 function Near([double]$Actual,[double]$Expected){ [math]::Abs($Actual-$Expected) -le [math]::Max(0.05*[math]::Abs($Expected),0.1) }
 function Act($Snap,[string]$Role,[string]$Field,[string]$What){ $actor=P $Snap $Role; if($null -eq $actor){ Fail ($What+': no '+$Role+' observation') }; Num (P $actor $Field) ($What+' '+$Role+'.'+$Field) }
 function Step($Row,[string]$Name){ $s=P (P $Row 'steps') $Name; if($null -eq $s){ Fail ('step observation missing: '+$Name) }; $s }
 function Unchanged($StepRecord,[string]$Role,[string[]]$Fields,[string]$What){
  foreach($f in $Fields){ Need (Same (Act (P $StepRecord 'after') $Role $f $What) (Act (P $StepRecord 'before') $Role $f $What)) ($What+': '+$Role+' '+$f+' unchanged') }
 }
 function Probe($Record,[string]$Key,[string]$What){ $p=P $Record $Key; if($null -eq $p){ Fail ($What+': no preview probe') }; if($null -ne (P $p 'error')){ Fail ($What+': the preview probe failed: '+(P $p 'error')) }; $p }
 # Reader r3: a genuine native ground hover's live prediction panel. Its movement-facing slots must describe the mount,
 # its Swift the rider; returns those slots.
 function Panel($Row,[string]$Name){
  $h=Step $Row ($Name+'Panel')
  if($null -ne (P $h 'error')){ Fail ($Name+': the panel hover failed: '+(P $h 'error')) }
  $pn=P $h 'panel'
  if($null -eq $pn -or $null -ne (P $pn 'error')){ Fail ($Name+': no live prediction panel') }
  foreach($k in 'fiveFeet','move','standard','fullRound'){ if((Text (P (P $pn $k) 'owner')) -cne 'mount'){ Fail ($Name+' panel: the '+$k+' slot does not describe the mount') } }
  if((Text (P (P $pn 'swift') 'owner')) -cne 'rider'){ Fail ($Name+' panel: the Swift slot left the rider') }
  # The full-round slot sums its own Move and Standard reads: they must be the mount's, the same as the two slots.
  foreach($d in 'possible','remaining'){
   $vals=@((P (P $pn 'fullRound') $d),(P (P $pn 'move') $d),(P (P $pn 'standard') $d))
   if(@($vals | Where-Object { $null -eq $_ }).Count -eq 0 -and [math]::Abs([double]$vals[0]-([double]$vals[1]+[double]$vals[2])) -gt 0.5){ Fail ($Name+' panel: the full-round '+$d+' is not the mount Move plus Standard') }
  }
  [pscustomobject]@{ fiveFeet=(P $pn 'fiveFeet'); move=(P $pn 'move'); standard=(P $pn 'standard'); probe=(P $h 'probe') }
 }
 function Spent($Slot){ (Text (P $Slot 'moveActivity')) -cin @('Unable','Used','Lost') -or ($null -ne (P $Slot 'remaining') -and (Num (P $Slot 'remaining') 'panel remaining') -le 0) }
 function Count($Items,[string]$Role,[long]$Round){ @($Items | Where-Object { (Text (P $_ 'role')) -ceq $Role -and (P $_ 'round') -eq $Round }).Count }

 $tb=[string]$Request.scenario -ceq 'finalize3-economy-tb'
 if(([string]$Request.scenario -cnotin @('finalize3-economy-tb','finalize3-economy-rt')) -or $Artifact.schemaVersion -ne 54){ Fail 'scenario/schema mismatch' }
 if($Artifact.status -ceq 'FAIL'){
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ Fail 'incomplete observation cannot become PASS' }
  return # Preserve the producer's explicit failed prefix; grant no qualification.
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ Fail 'envelope differs' }
 $o=P (P $Artifact 'observations') 'finalize3Economy'
 $mode=if($tb){'TB'}else{'RT'}
 # Reader r3: TB evidence must carry the v2 contract (a genuine native full attack, the live prediction panel); the RT
 # rows are unchanged since v1, whose r04 evidence keeps its verdict.
 $contracts=if($tb){@('finalize3-movement-economy-v2')}else{@('finalize3-movement-economy-v1','finalize3-movement-economy-v2')}
 if($null -eq $o -or (Text (P $o 'contract')) -cnotin $contracts -or (Text (P $o 'mode')) -cne $mode){ Fail 'measurement contract differs' }
 $riderId=Text (P $o 'rider'); $mountId=Text (P $o 'mount'); $bRiderId=Text (P $o 'secondRider'); $bMountId=Text (P $o 'secondMount')
 if([string]::IsNullOrEmpty($riderId) -or [string]::IsNullOrEmpty($mountId) -or $riderId -ceq $mountId -or $riderId -cne [string](P $Artifact.observations 'riderId') -or $mountId -cne [string](P $Artifact.observations 'horseId')){ Fail 'measurement identity differs' }
 if([string]::IsNullOrEmpty($bRiderId) -or $bRiderId -cin @($riderId,$mountId)){ Fail 'the second rider is not a distinct actor' }
 if($tb -and ([string]::IsNullOrEmpty($bMountId) -or $bMountId -cin @($riderId,$mountId,$bRiderId))){ Fail 'the second mount is not a distinct actor' }
 if((Text (P $o 'startRelationship')) -cne 'Unmounted'){ Fail 'the child did not start from its unmounted handoff' }
 $expected=if($tb){@(Get-KmcFinalize3TurnBasedCases)}else{@(Get-KmcFinalize3RealTimeCases)}
 if((@(P $o 'cases') | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ Fail 'registered row order differs' }
 if(@($Artifact.rows).Count -ne $expected.Count){ Fail 'the artifact holds rows other than the registered F3 rows' }
 $rows=@{}
 foreach($name in $expected){
  $found=@($Artifact.rows | Where-Object name -CEQ $name)
  if($found.Count -ne 1){ Fail ('required row missing or duplicated: '+$name) }
  if((Text (P $found[0].evidence 'case')) -cne $name){ Fail ('row evidence names another case: '+$name) }
  $rows[$name]=$found[0].evidence
 }
 $setup=@(P $o 'disclosedSetup')
 if($setup.Count -lt 1 -or @($setup | Where-Object { [string]::IsNullOrWhiteSpace((Text (P $_ 'action'))) }).Count -ne 0){ Fail 'the setup actions were not disclosed' }
 $names=P $o 'names'; $id=Get-KmcFinalize3Identities

 if($tb){
  # ---- F3-tb-setup ------------------------------------------------------------------------------------------------
  $s=$rows['F3-tb-setup']; $entry=Step $s 'entry'; $combat=Step $s 'combat'; $init=P $combat 'initiative'
  Need ((Text (P $entry 'aRelationship')) -ceq 'Mounted' -and (Text (P $entry 'bRelationship')) -ceq 'Mounted' -and (P (P $entry 'aRider') 'inCombat') -eq $false) 'both pairs mounted before combat'
  Need ((P $combat 'turnBased') -eq $true -and (Text (P (P $combat 'snap') 'aRelationship')) -ceq 'Mounted' -and (Text (P (P $combat 'snap') 'bRelationship')) -ceq 'Mounted') 'turn-based combat began with both pairs mounted'
  Need ((Int (P $init 'aRider') 'aRider initiative') -gt (Int (P $init 'aMount') 'aMount initiative')) 'pair A arranged rider-first'
  Need ((Int (P $init 'bMount') 'bMount initiative') -gt (Int (P $init 'bRider') 'bRider initiative')) 'pair B arranged mount-first'
  Need ($null -ne (P $o 'initiativeSetup')) 'the initiative setup was disclosed'

  # ---- F3-tb-a-round1 ---------------------------------------------------------------------------------------------
  $a=$rows['F3-tb-a-round1']; $start=Step $a 'start'; $p=Probe $start 'probe' 'A1'; $snap=P $start 'snap'
  $round1=Int (P $snap 'round') 'pair A first round'
  $speed=Num (P $p 'mountSpeed') 'A1 mount speed'
  Need ((Text (P $p 'unitRole')) -ceq 'aMount' -and (P $p 'unitIsMount') -eq $true) 'A1 preview unit is the mount'
  Need ((Text (P $p 'selectedRole')) -ceq 'aRider') 'A1 the rider is the selection'
  Need (Near (Num (P $p 'remaining') 'A1 remaining') (3*$speed)) 'A1 green range is the mount Move (3 s of speed)'
  Need (Near (Num (P $p 'remainingTotal') 'A1 total') (6*$speed)) 'A1 total range is two mount Moves (6 s of speed)'
  foreach($role in 'aRider','aMount'){ foreach($f in 'standard','move','swift'){ Need ((Act $snap $role $f 'A1') -le 0.001) ('A1 '+$role+' '+$f+' starts at 0') } }
  $a2=Step $a 'a2'; $a2c=Step $a 'a2Click'
  Need ((P $a2c 'clicked') -eq $true -and (Text (P $a2c 'selectedRole')) -ceq 'aRider') 'A2 rider-selected ground click admitted'
  $mm=Act (P $a2 'after') 'aMount' 'move' 'A2'
  Need ($mm -gt 0.01 -and $mm -lt 2.99) 'A2 mount Move charged within one Move'
  Need ((Act (P $a2 'after') 'aMount' 'standard' 'A2') -le 0.001) 'A2 mount Standard untouched'
  Unchanged $a2 'aRider' @('standard','move','swift') 'A2'
  Need ((Num (P $a2 'mountDelta') 'A2 mount delta') -gt 0.5) 'A2 the mount travelled'
  $p2=Probe $a2 'probe' 'A2'
  Need ((Text (P $p2 'unitRole')) -ceq 'aMount') 'A2 preview unit is the mount'
  Need (Near (Num (P $p2 'remaining') 'A2 remaining') ((3-$mm)*$speed)) 'A2 green range is the mount Move left'
  Need (Near (Num (P $p2 'remainingTotal') 'A2 total') ((6-$mm)*$speed)) 'A2 total range is the mount movement left'
  $pa2=Panel $a 'a2'
  Need (-not (Spent $pa2.move)) 'A2 panel: the mount keeps its Move'
  Need ((Num (P $pa2.move 'remaining') 'A2 panel Move remaining') -lt (Num (P $pa2.move 'possible') 'A2 panel Move possible') - 0.01) 'A2 panel: the partial mount move is not shown as an untouched Move'
  $a3=Step $a 'a3'; $a3c=Step $a 'a3Click'
  Need ((P $a3c 'clicked') -eq $true -and (Text (P $a3c 'selectedRole')) -ceq 'aMount') 'A3 mount-selected stock attack dispatched'
  Need ((Int (P $a3 'actorRules') 'A3 mount attack rules') -ge 1) 'A3 the mount attacked natively'
  Need ((Act (P $a3 'after') 'aMount' 'standard' 'A3') -ge 5.99) 'A3 mount Standard spent'
  Unchanged $a3 'aRider' @('standard','move') 'A3'
  $a4=Step $a 'a4'; $a4c=Step $a 'a4Click'
  Need ((P $a4c 'clicked') -eq $true -and (Text (P $a4c 'selectedRole')) -ceq 'aRider') 'A4 rider stock attack dispatched'
  Need ((Int (P $a4 'actorRules') 'A4 rider attack rules') -ge 1) 'A4 the rider attacked natively'
  Need ((Act (P $a4 'after') 'aRider' 'standard' 'A4') -ge 5.99) 'A4 rider Standard spent'
  $r=Step $a 'a4Refusal'
  Need ((Int (P $r 'actorRules') 'A4 refusal attack rules') -eq 0) 'A4 second rider attack made no attack'
  $feedback=[string](Text (P $r 'feedback'))
  Need ($feedback.StartsWith([string](Text (P $names 'aRider'))+$id.noStandard,[StringComparison]::Ordinal) -and -not [string]::IsNullOrEmpty((Text (P $names 'aRider')))) 'A4 refusal names the rider'
  Need ([string](Text (P $r 'trace')) -clike ('actor='+$riderId+';*')) 'A4 refusal trace names the rider'
  Unchanged $r 'aRider' @('standard','move') 'A4 refusal'
  Need ((P (Step $a 'end') 'endedNatively') -eq $true) 'pair A round one ended through the native End Turn'

  # ---- F3-tb-b-round1 ---------------------------------------------------------------------------------------------
  $b=$rows['F3-tb-b-round1']; $bs=Step $b 'start'; $bp=Probe $bs 'probe' 'B start'
  Need ((Int (P (P $bs 'snap') 'round') 'pair B first round') -eq $round1) 'pair B acts in the same round as pair A'
  $speedB=Num (P $bp 'mountSpeed') 'B mount speed'
  # Reader r3: B1 is a genuine native full attack (r03's single attack, read by r2, does not satisfy it).
  $b1=Step $b 'b1'
  Need ((P $b1 'clicked') -eq $true -and (P $b1 'enabledFullAttack') -eq $true -and (P $b1 'hoverChangedState') -eq $false) 'B1 ordinary Full input: hover changed nothing, native Full variant, left click admitted'
  Need ((Text (P $b1 'selectedRole')) -ceq 'bRider') 'B1 the rider is the selection at the attack input'
  Need ((Int (P $b1 'estimatedFullAttacks') 'B1 native full-attack estimate') -ge 2 -and (P $b1 'riderHasFullRound') -eq $true) 'B1 the rider natively had a full round of at least two attacks'
  $atk=P $b1 'attack'; $atk0=P (P $b1 'attackStarted') 'facts'
  Need ($null -ne $atk -and $null -ne $atk0 -and (Text (P $atk 'executor')) -ceq $bRiderId) 'B1 the rider attack command was observed'
  Need ((P $atk0 'isFullAttack') -eq $true -and (P $atk0 'isSingleAttack') -eq $false -and (P $atk0 'shouldApproach') -eq $false) 'B1 native full attack without an approach'
  $planned=Int (P $atk 'plannedAttacks') 'B1 planned attacks'; $delivered=Int (P $atk 'deliveredAttacks') 'B1 delivered attacks'
  Need ($planned -ge 2 -and $delivered -ge 2 -and $delivered -le $planned) 'B1 at least two planned strikes, at least two delivered'
  Need ((Int (P $b1 'actorRules') 'B1 rider attack rules') -ge 2) 'B1 at least two native attack rolls by the rider at the hostile'
  Need ((Act (P $b1 'after') 'bRider' 'standard' 'B1') -ge 5.99 -and (Act (P $b1 'after') 'bRider' 'move' 'B1') -ge 2.99) 'B1 rider full round spent (Standard and Move)'
  Need ((P $b1 'riderHasNormalMovement') -eq $false) 'B1 the rider itself has no normal movement left'
  Unchanged $b1 'bMount' @('standard','move') 'B1'
  Need ((Act (P $b1 'before') 'bMount' 'move' 'B1') -le 0.001 -and (Act (P $b1 'before') 'bMount' 'standard' 'B1') -le 0.001) 'B1 the mount started unmoved and unspent'
  Need ((Num (P $b1 'mountDelta') 'B1 mount delta') -lt 0.1) 'B1 the mount stayed put'
  $b1p=Probe $b1 'probe' 'B1'
  Need ((Text (P $b1p 'unitRole')) -ceq 'bMount' -and (P $b1p 'unitIsMount') -eq $true) 'B1 preview unit is the mount'
  Need (Near (Num (P $b1p 'remaining') 'B1 remaining') (3*$speedB)) 'B1 green range is the mount Move, not a forced rider step'
  Need ((P $b1p 'fiveFootStep') -eq $false) 'B1 step mode is off'
  $activation=Text (P (P $b1 'before') 'activationB'); $roundB=Int (P (P $b1 'before') 'round') 'B1 round'
  Need (-not [string]::IsNullOrEmpty($activation)) 'B1 the paired activation is identified'
  $b2=Step $b 'b2'; $b2c=Step $b 'b2Click'
  Need ((P $b2c 'clicked') -eq $true -and (Text (P $b2c 'selectedRole')) -ceq 'bRider') 'B2 rider-selected ground click admitted'
  Need ((Text (P (P $b2 'after') 'activationB')) -ceq $activation -and (Int (P (P $b2 'after') 'round') 'B2 round') -eq $roundB) 'B2 the same paired activation and round (no new turn or preparation)'
  Need ((Num (P $b2 'mountDelta') 'B2 mount delta') -gt 2.5) 'B2 the mount travelled beyond a five-foot step'
  $bm0=Act (P $b2 'before') 'bMount' 'move' 'B2'; $bm1=Act (P $b2 'after') 'bMount' 'move' 'B2'
  Need ($bm1 -gt $bm0+0.01 -and $bm1 -le 3.001) 'B2 mount Move charged within its Move'
  Need ((Act (P $b2 'after') 'bMount' 'standard' 'B2') -le 0.001) 'B2 mount Standard untouched'
  Unchanged $b2 'bRider' @('standard','move','swift') 'B2'
  # The live panel during genuine ground hovers: movement-facing slots describe the mount, Swift stays the rider's.
  $pb1=Panel $b 'b1'
  Need ((Text (P $pb1.move 'moveActivity')) -cnotin @('Unable','Used','Lost')) 'B1 panel: the full attack did not make the mount Move look spent'
  Need ((Num (P $pb1.move 'remaining') 'B1 panel Move remaining') -gt 0) 'B1 panel: the mount Move has range'
  $pb2=Panel $b 'b2'
  Need ((Num (P $pb2.move 'remaining') 'B2 panel Move remaining') -lt (Num (P $pb2.move 'possible') 'B2 panel Move possible') - 0.01) 'B2 panel: the partial mount move is not shown as an untouched Move'

  # ---- F3-tb-a-step -----------------------------------------------------------------------------------------------
  $st=$rows['F3-tb-a-step']
  Need ((Int (P (P (Step $st 'start') 'snap') 'round') 'pair A second round') -eq $round1+1) 'the step activation is pair A next round'
  $s1=Step $st 's1'; $s1c=Step $st 's1Click'
  # The step key stays on the native input context; execution mirrors it to the mount (the step outcome below).
  Need ((Text (P $s1 'changedContext')) -cin @('current','both')) 'S1 step key held on the native input context'
  Need ((P $s1c 'clicked') -eq $true) 'S1 step click admitted'
  $s1p=Probe $s1 'probe' 'S1'
  $stepped=[math]::Max([double](Num (P (P $s1p 'partner') 'metresStepped') 'S1 partner metres stepped'),[double]$(if($null -ne (P $s1p 'allocation')){ Num (P (P $s1p 'allocation') 'metresStepped') 'S1 allocation metres stepped' }else{0}))
  Need ($stepped -gt 0) 'S1 the mount stepped'
  Need ((Num (P $s1 'mountDelta') 'S1 mount delta') -gt 0.1) 'S1 the mount moved'
  Unchanged $s1 'aMount' @('move','standard') 'S1'
  Unchanged $s1 'aRider' @('standard','move','swift') 'S1'
  Need ((Num (P $s1p 'remaining') 'S1 remaining') -le 0.01 -and (Num (P $s1p 'remainingTotal') 'S1 total') -le 0.01) 'S1 no movement range after the step'
  $ps1=Panel $st 's1'
  Need ((Text (P $ps1.fiveFeet 'state')) -cnotin @('Available','WillBeUsed')) 'S1 panel: no five-foot step left'
  Need ((Spent $ps1.move) -and (Spent $ps1.standard)) 'S1 panel: no ordinary movement left'
  $s2=Step $st 's2'
  Need ((Num (P $s2 'mountDelta') 's2 mount delta') -lt 0.1) 'S2 ordinary move refused after the step'
  Unchanged $s2 'aMount' @('move','standard') 'S2'

  # ---- F3-tb-b-charge ---------------------------------------------------------------------------------------------
  $c=$rows['F3-tb-b-charge']
  Need ((Int (P (P (Step $c 'start') 'snap') 'round') 'pair B second round') -eq $round1+1) 'the charge activation is pair B next round'
  $c1=Step $c 'c1'; $click=P $c1 'click'
  Need ((P $click 'stockPresent') -eq $true -and (Text (P $click 'slotAbilityBefore')) -ceq $id.stockCharge -and (Text (P $click 'slotAbilityAfter')) -ceq $id.stockCharge) 'C1 the ordinary Charge slot was clicked'
  Need ((P $click 'handlerIsShell') -eq $true -and (P $click 'clicked') -eq $true) 'C1 the ordinary Charge carried the mounted Charge to the hostile'
  Need ((Int (P $c1 'chargeRules') 'C1 charge rules') -ge 1) 'C1 charge attack delivered'
  $alloc=P $c1 'allocation'
  Need ($null -ne $alloc -and (Num (P $alloc 'timeForced') 'C1 allocation forced time') -gt 0) 'C1 mount travel forced in the allocation'
  Need ((Act (P $c1 'after') 'bMount' 'standard' 'C1') -le 0.001) 'C1 mount Standard retained'
  Need ((Act (P $c1 'after') 'bRider' 'standard' 'C1') -ge 5.99) 'C1 rider Standard spent'
  $c2=Probe (Step $c 'c2') 'probe' 'C2'
  Need ((Num (P $c2 'remaining') 'C2 remaining') -le 0.01 -and (Num (P $c2 'remainingTotal') 'C2 total') -le 0.01) 'C2 no movement range after the charge'
  Need ((P $c2 'fiveFootStep') -eq $false -or (Num (P (P $c2 'partner') 'stepRange') 'C2 step range') -le 0.01) 'C2 no five-foot step after the charge'
  $pc2=Panel $c 'c2'
  Need ((Spent $pc2.move) -and (Spent $pc2.standard)) 'C2 panel: no ground travel after the delivered Charge'
  Need ((Text (P $pc2.fiveFeet 'state')) -cnotin @('Available','WillBeUsed')) 'C2 panel: no five-foot step after the delivered Charge'
  foreach($n in 'c3Move','c3Step'){
   $x=Step $c $n
   Need ((Num (P $x 'mountDelta') ($n+' mount delta')) -lt 0.1) ('C3 '+$n+' refused (no position change)')
   Unchanged $x 'bMount' @('move','standard') ('C3 '+$n)
   Unchanged $x 'bRider' @('standard','move') ('C3 '+$n)
  }
  $c4=Step $c 'c4'; $c4c=Step $c 'c4Click'
  Need ((P $c4c 'clicked') -eq $true -and (Text (P $c4c 'selectedRole')) -ceq 'bMount') 'C4 mount stock attack dispatched'
  Need ((Int (P $c4 'actorRules') 'C4 mount attack rules') -ge 1) 'C4 the mount attacked with its retained Standard'
  Need ((Act (P $c4 'after') 'bMount' 'standard' 'C4') -ge 5.99) 'C4 mount Standard spent'

  # ---- F3-tb-a-double ---------------------------------------------------------------------------------------------
  $d=$rows['F3-tb-a-double']
  Need ((Int (P (P (Step $d 'start') 'snap') 'round') 'pair A third round') -eq $round1+2) 'the double-move activation is pair A third round'
  $d1=Step $d 'd1'; $d1c=Step $d 'd1Click'
  Need ((P $d1c 'clicked') -eq $true) 'D1 ground click admitted'
  Need ((Act (P $d1 'after') 'aMount' 'move' 'D1') -gt 3.01) 'D1 mount moved into its second Move'
  # Reader r2: native UsedStandardAction does not count the two-move conversion (MoveAction above 3, asserted above);
  # native HasStandardAction does, which the D2 refusal below proves.
  Unchanged $d1 'aRider' @('standard','move','swift') 'D1'
  $pd1=Panel $d 'd1'
  Need (Spent $pd1.move) 'D1 panel: the mount Move is spent by the double move'
  Need ((Text (P $pd1.standard 'attackActivity')) -cin @('Lost','WillBeLost','Unable')) 'D1 panel: the converted Standard offers no attack'
  $conversion=Num (P (Probe ([pscustomobject]@{ probe=$pd1.probe }) 'probe' 'D1 panel') 'remainingTotal') 'D1 panel total range'
  if($conversion -gt 0.1){ Need (-not (Spent $pd1.standard)) 'D1 panel: the remaining conversion travel is shown on the mount Standard' }
  else { Need (Spent $pd1.standard) 'D1 panel: no conversion travel is advertised once exhausted' }
  $d2=Step $d 'd2'
  Need ((Int (P $d2 'actorRules') 'D2 mount attack rules') -eq 0) 'D2 the mount made no attack'
  $feedback=[string](Text (P $d2 'feedback'))
  Need ($feedback.StartsWith([string](Text (P $names 'aMount'))+$id.noStandard,[StringComparison]::Ordinal) -and -not [string]::IsNullOrEmpty((Text (P $names 'aMount')))) 'D2 refusal names the mount'
  Need ([string](Text (P $d2 'trace')) -clike ('actor='+$mountId+';*')) 'D2 refusal trace names the mount'

  # ---- F3-tb-b-dismount -------------------------------------------------------------------------------------------
  $x=$rows['F3-tb-b-dismount']; $xs=Step $x 'start'
  Need ((P (P $xs 'participation') 'mountPrepared') -eq $true) 'the dismount activation had its mount prepared'
  $dm=Step $x 'dismount'
  Need ((Text (P (P $dm 'after') 'bRelationship')) -cin @('Unmounted','None')) 'pair B dismounted in its activation'
  $dRound=Int (P $dm 'dismountRound') 'dismount round'
  Need ($dRound -eq $round1+2) 'the dismount is in pair B third round'
  $own=Step $x 'mountOwnTurn'
  Need ((Int (P $own 'round') 'mount own-turn round') -eq $dRound+1) 'the released mount owns a native turn the next round'
  Need ((Int (P (Step $x 'nextRiderTurn') 'round') 'next rider round') -eq $dRound+1) 'the second rider next turn closes the observed round'

  # ---- native turns, preparations, tracker ------------------------------------------------------------------------
  $turns=@(P $o 'turnStarts')
  Need ($turns.Count -gt 0) 'native turn starts were observed'
  Need (@($turns | Where-Object { (Text (P $_ 'role')) -ceq 'aMount' }).Count -eq 0) 'pair A mount never owned a native turn while mounted'
  for($k=$round1;$k -le $round1+2;$k++){
   Need ((Count $turns 'aRider' $k) -eq 1) ('round '+$k+': pair A rider owned exactly one native turn')
   Need ((Count $turns 'bRider' $k) -eq 1) ('round '+$k+': pair B rider owned exactly one native turn')
   Need ((Count $turns 'bMount' $k) -eq 0) ('round '+$k+': pair B mount owned no native turn while mounted or in its dismount round')
  }
  Need ((Count $turns 'bMount' ($dRound+1)) -eq 1) 'the released mount owned exactly one native turn the next round'
  $prep=@(P $o 'preparations')
  $dFrame=Int (P $dm 'dismountFrame') 'dismount frame'
  foreach($k in $round1..($round1+2)){ foreach($role in 'aMount','bMount'){
   $n=(@($prep | Where-Object { (Text (P $_ 'role')) -ceq $role -and (P $_ 'round') -eq $k }) | ForEach-Object { Int (P $_ 'count') 'preparation count' } | Measure-Object -Sum).Sum
   Need ([long]$n -le 1) ('round '+$k+': '+$role+' prepared at most once')
  } }
  Need (@($prep | Where-Object { (Text (P $_ 'role')) -ceq 'bMount' -and (P $_ 'round') -eq $dRound -and (Int (P $_ 'frame') 'preparation frame') -gt $dFrame }).Count -eq 0) 'no second preparation of the released mount in its dismount round'
  $after=(@($prep | Where-Object { (Text (P $_ 'role')) -ceq 'bMount' -and (P $_ 'round') -eq ($dRound+1) }) | ForEach-Object { Int (P $_ 'count') 'preparation count' } | Measure-Object -Sum).Sum
  Need ([long]$after -eq 1) 'the released mount prepared once for its own next-round turn'
  $trackers=@(P $o 'trackers')
  $mounted=@($trackers | Where-Object { (Int (P $_ 'round') 'tracker round') -ge $round1 -and (Int (P $_ 'round') 'tracker round') -le $round1+2 -and (Text (P $_ 'label')) -cne 'b-round3-after-dismount' })
  Need ($mounted.Count -ge 5) 'the tracker was read at every mounted activation'
  foreach($t in $mounted){
   $label=Text (P $t 'label')
   Need ($null -eq (P $t 'error') -and (Text (P $t 'source')) -cin @('live','constructed')) ('tracker '+$label+' was read')
   Need ((Int (P $t 'aMount') ('tracker '+$label+' aMount')) -eq 0 -and (Int (P $t 'bMount') ('tracker '+$label+' bMount')) -eq 0) ('tracker '+$label+': no mount entry while mounted')
   Need ((Int (P $t 'aRider') ('tracker '+$label+' aRider')) -eq 1 -and (Int (P $t 'bRider') ('tracker '+$label+' bRider')) -eq 1) ('tracker '+$label+': one entry per rider')
  }
  $back=@($trackers | Where-Object { (Text (P $_ 'label')) -ceq 'round-after-dismount' })
  Need ($back.Count -eq 1 -and $null -eq (P $back[0] 'error')) 'the tracker was read the round after the dismount'
  Need ((Int (P $back[0] 'round') 'tracker round after dismount') -eq $dRound+1 -and (Int (P $back[0] 'bMount') 'released mount entries') -eq 1 -and (Int (P $back[0] 'aMount') 'pair A mount entries') -eq 0) 'the released mount is in the tracker again, pair A mount still is not'
 } else {
  # ---- real time ------------------------------------------------------------------------------------------------
  $m=$rows['F3-rt-mounted-marker']; $mv=Step $m 'move'; $mc=Step $m 'moveClick'
  Need ((Text (P (P $mv 'before') 'aRelationship')) -ceq 'Mounted' -and (Text (P (P $mv 'before') 'selected')) -ceq $riderId) 'RT the mounted rider is the selection'
  Need ((P $mc 'clicked') -eq $true) 'RT rider-selected ground click admitted'
  Need ((Num (P $mv 'mountDelta') 'RT mount delta') -gt 3 -and (Num (P $mv 'riderDelta') 'RT rider delta') -gt 3) 'RT the mount carried the rider'
  Need ((Num (P $mv 'riderMountSeparation') 'RT rider-mount separation') -lt 1.5) 'RT the rider position follows the mount'
  $mk=P $mv 'marker'
  Need ((Int (P $mk 'framesWatched') 'RT marker frames') -ge 1 -and (Int (P $mk 'visibleFrames') 'RT visible marker frames') -ge 1) 'RT the mount destination marker was shown while moving'
  Need ((Num (P $mk 'maxAlpha') 'RT marker alpha') -ge 0.95) 'RT the mount marker uses the selected style'
  Need ((Text (P (P $mv 'whileMoving') 'selected')) -ceq $riderId) 'RT the rider stayed selected while the mount moved'
  $u=$rows['F3-rt-unmounted-marker']; $cm=Step $u 'controlMove'; $cc=Step $u 'controlMoveClick'
  Need ((P $cm 'riderMounted') -eq $false -and (P $cc 'clicked') -eq $true) 'RT unmounted control: the second rider ground click admitted'
  Need ((Num (P $cm 'riderDelta') 'RT control rider delta') -gt 3) 'RT unmounted control: the rider moved'
  $ck=P $cm 'marker'
  Need ((Int (P $ck 'visibleFrames') 'RT control visible marker frames') -ge 1 -and (Num (P $ck 'maxAlpha') 'RT control marker alpha') -ge 0.95) 'RT unmounted control: native selected marker'
  Need ((Text (P (P $cm 'whileMoving') 'selected')) -ceq $bRiderId) 'RT unmounted control: its own rider selected'
  $at=$rows['F3-rt-attack']; $ac=Step $at 'attackClick'; $ax=Step $at 'attack'
  Need ((P $ac 'clicked') -eq $true -and (Text (P $ac 'selectedRole')) -ceq 'aRider') 'RT rider stock attack clicked'
  Need ((Int (P $ax 'actorRules') 'RT rider attack rules') -ge 1 -and (Text (P (P $ax 'rule') 'attacker')) -ceq $riderId) 'RT ordinary rider attack completed natively'
 }

 # ---- screenshots with the game UI ------------------------------------------------------------------------------------
 $shots=@(P $o 'screenshots'); $wanted=@(Get-KmcFinalize3ShotNames $mode)
 Need ($shots.Count -eq $wanted.Count) ('exactly '+$wanted.Count+' screenshots recorded')
 $root=if(-not [string]::IsNullOrEmpty($EvidenceRoot)){ $EvidenceRoot } else { Text (P $Request 'evidenceRoot') }
 foreach($name in $wanted){
  $shot=@($shots | Where-Object { (Text (P $_ 'name')) -ceq $name })
  Need ($shot.Count -eq 1) ('screenshot recorded once: '+$name)
  $shot=$shot[0]
  Need ($null -eq (P $shot 'error')) ('screenshot succeeded: '+$name)
  Need ((Text (P $shot 'file')) -ceq ('movement-visuals/finalize3-'+$name+'.png') -and (Text (P $shot 'sha256')) -cmatch '^[0-9a-f]{64}$' -and (Int (P $shot 'bytes') ($name+' bytes')) -gt 0) ('screenshot file record: '+$name)
  Need ((P $shot 'ummOpenAtCapture') -eq $false) ('mod-manager window closed for the frame: '+$name)
  if(-not [string]::IsNullOrEmpty($root)){
   $path=Join-Path $root ((Text (P $shot 'file')) -replace '/','\')
   Need (Test-Path -LiteralPath $path -PathType Leaf) ('screenshot file present: '+$name)
   Need ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ceq (Text (P $shot 'sha256'))) ('screenshot file hash matches: '+$name)
  }
 }
}
