# F6 (FALL-01/FALL-02) external reader for finalize6-falls-tb and its completion variant finalize6-falls-completion-tb, which
# runs the same setup and fall rows without the F3 action rows and claims none of them. finalize6-falls-tb runs the F3 rows first
# (recorded, and required to complete in the game; their detailed F3 verdicts belong to the F3 reader and are not
# re-asserted here), then the two fall rows, which this reader owns: one soft fall per genuine mount death or new prone,
# for the exact rider and pair generation, through the native check, damage and prone, with no action written, the other
# pair untouched, no fall replayed by later callbacks, the explanatory line in the native battle log, and the native
# stand-up paying its Move.
function Get-KmcFinalize6TurnBasedCases { @('F3-tb-setup','F3-tb-a-round1','F3-tb-b-round1','F3-tb-a-step','F3-tb-b-charge','F6-tb-a-death-fall','F6-tb-b-prone-fall') }
function Get-KmcFinalize6CompletionCases { @('F3-tb-setup','F6-tb-a-death-fall','F6-tb-b-prone-fall') }
function Get-KmcFinalize6ShotNames { @('f6-a-death-fall','f6-a-stood-up','f6-b-prone-fall') }
# The existing nav-safe dismount placement asks native FreePlaceSelector for a second place of radius max(corpulence)+0.25 m
# around the mount, so a landed rider's centre is at least the larger body radius from the mount's.
function Assert-KmcFinalize6Placement($Placement,[string]$What){
 foreach($f in 'riderToMount','riderCorpulence','mountCorpulence'){ $v=$Placement.$f; if($v -isnot [int] -and $v -isnot [long] -and $v -isnot [double] -and $v -isnot [single] -and $v -isnot [decimal]){ throw ('F6 placement '+$f+' is missing: '+$What) } }
 $bound=[math]::Max([double]$Placement.riderCorpulence,[double]$Placement.mountCorpulence)
 if([double]$Placement.mountCorpulence -le 0 -or [double]$Placement.riderToMount -lt $bound){ throw ('F6 assertion failed: '+$What+' (distance '+$Placement.riderToMount+' under '+$bound+')') }
}
function Assert-KmcFinalize6FallRecord {
 param([Parameter(Mandatory=$true)]$Record,[Parameter(Mandatory=$true)][string]$What,[Parameter(Mandatory=$true)][string]$Cause,
  [Parameter(Mandatory=$true)][string]$RiderId,[Parameter(Mandatory=$true)][string]$MountId,[Parameter(Mandatory=$true)][bool]$Pass,
  [Parameter(Mandatory=$true)][string]$Mode,$Generation)
 function Fail([string]$Why){ throw ('F6 '+$What+': '+$Why) }
 function P($O,[string]$N){ if($null -ne $O -and $O -isnot [string] -and $O -isnot [ValueType] -and $null -ne $O.PSObject.Properties[$N]){ $O.$N } else { $null } }
 function Int($V,[string]$N){ if($V -isnot [int] -and $V -isnot [long]){ Fail ($N+' is missing or not an integer') }; [long]$V }
 function Num($V,[string]$N){ if($V -isnot [int] -and $V -isnot [long] -and $V -isnot [double] -and $V -isnot [single] -and $V -isnot [decimal]){ Fail ($N+' is missing or not a number') }; [double]$V }
 function Need([bool]$C,[string]$N){ if(-not $C){ Fail ('assertion failed: '+$N) } }
 $r=$Record
 Need ((P $r 'cause') -ceq $Cause) ('cause is '+$Cause)
 Need ((P $r 'riderId') -ceq $RiderId -and (P $r 'mountId') -ceq $MountId) 'the exact rider and mount'
 if($null -ne $Generation){ Need ((Int (P $r 'generation') 'generation') -eq [long]$Generation) 'the pair generation that was mounted' }
 Need ((P $r 'mode') -ceq $Mode) ('captured in '+$Mode)
 Need ((P $r 'resolved') -eq $true -and $null -eq (P $r 'abandonedReason')) 'resolved, not abandoned'
 Need ((P $r 'inability') -ceq 'None' -and (P $r 'attempted') -eq $true) 'an able rider attempted the native check'
 Need ((P $r 'forced') -eq $true -and (P $r 'forcedPass') -eq $Pass) 'the labelled outcome is disclosed in the record'
 $d20=Int (P $r 'checkD20') 'd20'; $stat=Int (P $r 'checkStat') 'stat'; $bonus=Int (P $r 'checkBonus') 'bonus'
 $result=Int (P $r 'checkResult') 'result'; $dc=Int (P $r 'checkDc') 'DC'; $adj=Int (P $r 'checkDcAdjustment') 'DC adjustment'
 Need ($d20 -ge 1 -and $d20 -le 20) 'the d20 is a die face'
 Need ($result -eq $d20+$stat+$bonus) 'the result is the native d20 plus Mobility plus bonus'
 Need ($dc -eq 15+$adj) 'DC 15 with the native difficulty adjustment'
 Need ((P $r 'thresholdAgrees') -eq $true) 'the native pass agrees with result >= DC'
 Need ((P $r 'passed') -eq ($result -ge $dc)) 'passed is result >= DC'
 Need ((P $r 'passed') -eq $Pass) ('the native check '+$(if($Pass){'passed'}else{'failed'}))
 if($Pass -and $d20 -gt 1){ Need ($result -eq $dc) 'the labelled pass met the DC exactly' }
 if(-not $Pass -and $d20 -lt 20){ Need ($result -eq $dc-1) 'the labelled fail was one below the DC' }
 foreach($f in 'standard','move','swift'){ Need ([math]::Abs((Num (P $r ($f+'Before')) $f)-(Num (P $r ($f+'After')) $f)) -le 0.0001) ('the rider '+$f+' cooldown was neither spent nor refunded') }
 $before=Int (P $r 'riderDamageBefore') 'damage before'; $after=Int (P $r 'riderDamageAfter') 'damage after'
 if($Pass){
  Need ((P $r 'outcome') -ceq 'SafeLanding') 'a safe landing'
  Need ((P $r 'damageDealt') -eq $false -and $after -eq $before) 'no fall damage'
  Need ((P $r 'proneImposed') -eq $false -and $null -eq (P $r 'proneRefusal')) 'no new prone'
 } else {
  Need ((P $r 'outcome') -ceq 'HardLanding') 'a hard landing'
  $rolled=Int (P $r 'damageRolled') 'rolled'; $applied=Int (P $r 'damageApplied') 'applied'
  Need ((P $r 'damageDealt') -eq $true -and $rolled -ge 1 -and $rolled -le 6) 'one native 1d6'
  Need ($applied -ge 0 -and $applied -le $rolled -and $applied -eq (Int (P $r 'damageBeforeDifficulty') 'before difficulty')) 'native mitigation only reduces the roll; the party rider''s own fall is not difficulty-scaled'
  Need (($after-$before) -ge 0 -and ($after-$before) -le $applied) 'hit point loss is the applied damage (temporary hit points may absorb it)'
  Need ((P $r 'proneImposed') -eq $true -and $null -eq (P $r 'proneRefusal')) 'native prone imposed (the fixture riders are not prone-immune)'
 }
 $text=[string](P $r 'logText')
 Need (-not [string]::IsNullOrEmpty($text) -and $text.Contains([string](P $r 'mountName')) -and $text.Contains([string](P $r 'riderName')) -and $text.Contains('Mobility '+$result+' vs DC '+$dc)) 'the explanation names mount, rider, check and DC'
 Need ($(if($Pass){$text.EndsWith(' - lands safely.')}else{$text.Contains(' - falls hard')})) 'the explanation states the outcome'
}
function Assert-KmcFinalize6FallsEvidence {
 param([Parameter(Mandatory=$true)]$Request,[Parameter(Mandatory=$true)]$Artifact,[AllowNull()][string]$Status,[string]$EvidenceRoot)
 function Fail([string]$Why){ throw ('F6 '+$Why) }
 function P($O,[string]$N){ if($null -ne $O -and $O -isnot [string] -and $O -isnot [ValueType] -and $null -ne $O.PSObject.Properties[$N]){ $O.$N } else { $null } }
 function Int($V,[string]$What){ if($V -isnot [int] -and $V -isnot [long]){ Fail ($What+' is missing or not an integer') }; [long]$V }
 function Num($V,[string]$What){ if($V -isnot [int] -and $V -isnot [long] -and $V -isnot [double] -and $V -isnot [single] -and $V -isnot [decimal]){ Fail ($What+' is missing or not a number') }; [double]$V }
 function Text($V){ if($V -is [string]){ $V } else { $null } }
 function Need([bool]$Condition,[string]$Name){ if(-not $Condition){ Fail ('assertion failed: '+$Name) } }
 function Step($Row,[string]$Name){ $s=P (P $Row 'steps') $Name; if($null -eq $s){ Fail ('step observation missing: '+$Name) }; $s }
 function Svc($Snap,[string]$Field){ Int (P (P $Snap 'service') $Field) ('fall service '+$Field) }

 function J($V){ if($null -eq $V){ 'null' } else { $V | ConvertTo-Json -Compress -Depth 5 } }
 function Reaction($Snap,[string]$Role,[string]$What){ $m=P (P $Snap $Role) 'mountedCombat'; if($null -eq $m -or $null -ne (P $m 'error')){ Fail ($What+': no Mounted Combat reaction state') }; J $m }
 # F6 completion: the same encounter setup and fall rows without the F3 action rows (r02's refused full attack); it claims no F3 row.
 $completion=[string]$Request.scenario -ceq 'finalize6-falls-completion-tb'
 if(([string]$Request.scenario -cnotin @('finalize6-falls-tb','finalize6-falls-completion-tb')) -or $Artifact.schemaVersion -ne 55){ Fail 'scenario/schema mismatch' }
 if($Artifact.status -ceq 'FAIL'){
  if($Status -ceq 'PASS' -or (@($Artifact.errors).Count -eq 0 -and $Artifact.subscenarioFailCount -lt 1)){ Fail 'incomplete observation cannot become PASS' }
  return
 }
 if($Artifact.status -cne 'PASS' -or @($Artifact.errors).Count -ne 0 -or @($Artifact.rows | Where-Object status -CNE 'PASS').Count -ne 0 -or $Artifact.subscenarioFailCount -ne 0){ Fail 'envelope differs' }
 $o=P (P $Artifact 'observations') 'finalize6Falls'
 $contract=if($completion){'finalize6-falls-completion-v1'}else{'finalize6-falls-v1'}
 if($null -eq $o -or (Text (P $o 'contract')) -cne $contract -or (Text (P $o 'mode')) -cne 'TB'){ Fail 'measurement contract differs' }
 $riderId=Text (P $o 'rider'); $mountId=Text (P $o 'mount'); $bRiderId=Text (P $o 'secondRider'); $bMountId=Text (P $o 'secondMount')
 if([string]::IsNullOrEmpty($riderId) -or [string]::IsNullOrEmpty($mountId) -or $riderId -ceq $mountId -or $riderId -cne [string](P $Artifact.observations 'riderId') -or $mountId -cne [string](P $Artifact.observations 'horseId')){ Fail 'measurement identity differs' }
 if([string]::IsNullOrEmpty($bRiderId) -or [string]::IsNullOrEmpty($bMountId) -or @($riderId,$mountId,$bRiderId,$bMountId | Select-Object -Unique).Count -ne 4){ Fail 'the two pairs are not four distinct actors' }
 $policy=P $o 'fallPolicy'
 Need ((Int (P $policy 'dc') 'policy DC') -eq 15 -and (Int (P $policy 'damageDice') 'dice') -eq 1 -and (Int (P $policy 'damageDieSides') 'sides') -eq 6 -and (Text (P $policy 'stat')) -ceq 'SkillMobility') 'the D5 policy constants'
 $expected=if($completion){@(Get-KmcFinalize6CompletionCases)}else{@(Get-KmcFinalize6TurnBasedCases)}
 if((@(P $o 'cases') | ConvertTo-Json -Compress) -cne ($expected | ConvertTo-Json -Compress)){ Fail 'registered row order differs' }
 if(@($Artifact.rows).Count -ne $expected.Count){ Fail 'the artifact holds rows other than the registered rows' }
 $rows=@{}
 foreach($name in $expected){
  $found=@($Artifact.rows | Where-Object name -CEQ $name)
  if($found.Count -ne 1){ Fail ('required row missing or duplicated: '+$name) }
  if((Text (P $found[0].evidence 'case')) -cne $name){ Fail ('row evidence names another case: '+$name) }
  $rows[$name]=$found[0].evidence
 }
 $setup=@(P $o 'disclosedSetup')
 Need (@($setup | Where-Object { (Text (P $_ 'action')) -like '*labelled to fail*' }).Count -eq 1 -and @($setup | Where-Object { (Text (P $_ 'action')) -like '*labelled to pass*' }).Count -eq 1) 'both labelled outcomes were disclosed'
 # ---- the F3 encounter rows ran first, on both mounted pairs --------------------------------------------------------
 $entry=Step $rows['F3-tb-setup'] 'entry'; $combat=Step $rows['F3-tb-setup'] 'combat'
 Need ((Text (P $entry 'aRelationship')) -ceq 'Mounted' -and (Text (P $entry 'bRelationship')) -ceq 'Mounted' -and (P $combat 'turnBased') -eq $true) 'both pairs mounted into turn-based combat'
 $init=P $combat 'initiative'
 Need ((Int (P $init 'aRider') 'aRider initiative') -gt (Int (P $init 'aMount') 'aMount initiative') -and (Int (P $init 'bMount') 'bMount initiative') -gt (Int (P $init 'bRider') 'bRider initiative')) 'pair A arranged rider-first and pair B mount-first'
 $start=P $o 'fallServiceAtStart'
 Need ((Int (P $start 'begun') 'begun at start') -eq 0 -and (Int (P $start 'pending') 'pending at start') -eq 0) 'no fall before the fall rows'
 $falls=@(P $o 'falls')
 Need ($falls.Count -eq 2) 'exactly two falls recorded'

 # ---- F6-tb-a-death-fall ------------------------------------------------------------------------------------------
 $a=$rows['F6-tb-a-death-fall']; $ab=Step $a 'fallBefore'; $aa=Step $a 'fallAfter'; $as=Step $a 'fallSettled'
 Need ((Text (P (P $ab 'aPair') 'state')) -ceq 'Mounted' -and (Text (P (P $ab 'bPair') 'state')) -ceq 'Mounted') 'both pairs mounted before the death'
 Need ((Text (P $ab 'turnRole')) -ceq 'aRider') 'the death was measured on pair A''s own activation'
 Need ((Text (P (P $ab 'aMount') 'life')) -ceq 'Conscious') 'pair A mount alive before'
 $kill=Step $a 'kill'
 Need ((Int (P $kill 'applied') 'kill applied') -ge 1) 'the native lethal rule applied damage'
 Need ((Text (P (P $aa 'aMount') 'life')) -ceq 'Dead') 'pair A mount died natively'
 Need ((Text (P (P $aa 'aPair') 'state')) -ceq 'Unmounted') 'pair A rider unseated'
 Need ((Text (P (P $aa 'bPair') 'state')) -ceq 'Mounted' -and (Int (P (P $aa 'bPair') 'generation') 'B generation after') -eq (Int (P (P $ab 'bPair') 'generation') 'B generation before') -and
  (Text (P (P $aa 'bPair') 'ownerId')) -ceq (Text (P (P $ab 'bPair') 'ownerId'))) 'the other pair stays mounted under the same owner and generation'
 Need ((Svc $aa 'begun') -eq (Svc $ab 'begun')+1 -and (Svc $aa 'resolved') -eq (Svc $ab 'resolved')+1 -and (Svc $aa 'pending') -eq 0 -and (Svc $aa 'abandoned') -eq 0) 'exactly one fall for the death'
 $fa=$falls[0]
 Need ((Text (P $fa 'label')) -ceq 'tb-a-death') 'the first fall is the death'
 Assert-KmcFinalize6FallRecord -Record (P $fa 'record') -What 'TB death' -Cause 'MountDied' -RiderId $riderId -MountId $mountId -Pass $false -Mode 'TB' -Generation (P (P $ab 'aPair') 'generation')
 $text=Text (P (P $fa 'record') 'logText')
 Need (@(P $fa 'log') -ccontains $text) 'the explanation is in the native battle log'
 Assert-KmcFinalize6Placement (Step $a 'placement') 'the rider landed beside the dead mount, not inside it'
 Need ((P (P $as 'aRider') 'prone') -eq $true) 'the hard landing left the rider natively prone'
 Need ((Text (P (P $as 'aRider') 'life')) -ceq 'Conscious') 'the rider survived the fall'
 $dae=Step $a 'deathActivationEnded'; Need ($dae -is [bool]) 'whether the death ended its activation is recorded'
 $nt=Step $a 'nextTurn'
 Need ((Int (P $nt 'round') 'next turn round') -gt (Int (P $ab 'round') 'fall round') -and (Text (P $nt 'turnRole')) -ceq 'aRider' -and (P (P $nt 'aRider') 'prone') -eq $true) 'the still prone rider reached its own next native turn'
 $sb=Step $a 'standUpBefore'; $su=Step $a 'standUpAfter'
 Need ((P (P $sb 'aRider') 'prone') -eq $true -and (P (P $su 'aRider') 'prone') -eq $false) 'the prone rider stood up natively'
 # Native TurnController.HandleUnitWillGetUp adds the turn's Move (+3 s) and cancels the triggering order. The cost is read on the Move
 # cooldown itself at the frame prone clears: with the Standard unused, native HasMoveAction() is !(MoveAction > 3), so it stays
 # true after exactly one Move and cannot show the charge. No travel may follow, so a later walk can neither pay for nor hide it.
 $tu=Step $a 'standUpTurn'; $sbd=Step $a 'standUpBoundary'
 Need ((P $tu 'sameTurnAsNext') -eq $true -and (Text (P $tu 'unit')) -ceq $riderId) 'the stand-up order was given on the rider''s own next native turn'
 Need ((P (P $sb 'aRider') 'canStandUp') -eq $true -and (Num (P (P $sb 'aRider') 'move') 'move before stand-up') -le 0.5) 'native readiness permitted the stand-up on a fresh Move'
 $moveCost=(Num (P (P $sbd 'aRider') 'move') 'move at stand-up')-(Num (P (P $sb 'aRider') 'move') 'move before stand-up')
 Need ((P (P $sbd 'aRider') 'prone') -eq $false -and $moveCost -ge 2.5 -and $moveCost -le 3.0001) 'at the frame prone cleared, the native stand-up had charged one Move (+3 s)'
 Need ([math]::Abs((Num (P (P $sbd 'aRider') 'standard') 'standard at stand-up')-(Num (P (P $sb 'aRider') 'standard') 'standard before stand-up')) -le 0.0001 -and (P (P $sbd 'aRider') 'hasStandard') -eq (P (P $sb 'aRider') 'hasStandard')) 'the stand-up left the Standard action untouched'
 Need ((Num (Step $a 'standUpTravel') 'stand-up travel') -lt 0.5 -and (P (P $su 'aRider') 'commandsEmpty') -eq $true) 'the native stand-up cancelled the triggering order: no travel follows it'
 Need ((Svc $sbd 'begun') -eq (Svc $aa 'begun') -and (Svc $sbd 'resolved') -eq (Svc $aa 'resolved')) 'the stand-up replayed no fall, check or damage'
 Need ((Reaction $ab 'aRider' 'A before') -ceq (Reaction $aa 'aRider' 'A after')) 'the hard landing left the Mounted Combat reaction state untouched'
 Need ((P (P $su 'aRider') 'hasStandard') -eq (P (P $sb 'aRider') 'hasStandard')) 'the stand-up did not touch the Standard action'
 Need ((Svc $su 'begun') -eq (Svc $aa 'begun')) 'standing up replayed no fall'
 Need ((P (Step $a 'end') 'endedNatively') -eq $true) 'pair A activation ended natively'

 # ---- F6-tb-b-prone-fall ------------------------------------------------------------------------------------------
 $b=$rows['F6-tb-b-prone-fall']; $bb=Step $b 'fallBefore'; $ba=Step $b 'fallAfter'; $bs=Step $b 'fallSettled'; $bd=Step $b 'afterDeath'
 Need ((Text (P (P $bb 'bPair') 'state')) -ceq 'Mounted') 'pair B mounted before its fall'
 Need ((Text (P $bb 'turnRole')) -ceq 'bRider') 'the prone was measured on pair B''s own activation'
 Need ((P (Step $b 'knockOff') 'canBeKnockedOff') -eq $true) 'the native knock-off rule allowed the prone'
 Need ((Text (P (P $ba 'bPair') 'state')) -ceq 'Unmounted' -and (P (P $ba 'bMount') 'prone') -eq $true -and (Text (P (P $ba 'bMount') 'life')) -ceq 'Conscious') 'the conscious mount fell prone and unseated its rider'
 Need ((Svc $ba 'begun') -eq (Svc $bb 'begun')+1 -and (Svc $ba 'resolved') -eq (Svc $bb 'resolved')+1 -and (Svc $ba 'pending') -eq 0) 'exactly one fall for the prone'
 $fb=$falls[1]
 Need ((Text (P $fb 'label')) -ceq 'tb-b-prone') 'the second fall is the prone'
 Assert-KmcFinalize6FallRecord -Record (P $fb 'record') -What 'TB prone' -Cause 'MountFellProne' -RiderId $bRiderId -MountId $bMountId -Pass $true -Mode 'TB' -Generation (P (P $bb 'bPair') 'generation')
 Need (@(P $fb 'log') -ccontains (Text (P (P $fb 'record') 'logText'))) 'the safe-landing explanation is in the native battle log'
 Assert-KmcFinalize6Placement (Step $b 'placement') 'the rider landed beside the prone mount, not inside it'
 Need ((P (P $bs 'bRider') 'prone') -eq $false) 'the safe landing left the rider standing (unhurt: the fall record)'
 Need ((P (Step $b 'repeatKnockOff') 'canBeKnockedOff') -eq $false) 'the repeat knock-off found the mount already prone'
 Need ((Text (P (P $bd 'bMount') 'life')) -ceq 'Dead') 'the unseated prone mount then died natively'
 Need ((Int (Step $b 'begunSinceUnseated') 'begun since unseated') -eq 0) 'prone then death began no second fall'
 Need ((Svc $bd 'begun') -eq (Svc $ba 'begun') -and (Svc $bd 'resolved') -eq (Svc $ba 'resolved')) 'the former rider was not thrown or hurt again by the fall service'
 Need ((Int (P (P $bd 'bRider') 'damage') 'B rider damage after death') -eq (Int (P (P $bs 'bRider') 'damage') 'B rider damage before the kill') -and (P (P $bd 'bRider') 'prone') -eq $false) 'the former rider kept its damage and stayed standing through the death callbacks'
 Need ((Reaction $bb 'bRider' 'B before') -ceq (Reaction $ba 'bRider' 'B after')) 'the safe landing left the Mounted Combat reaction state untouched'
 Need ((P (Step $b 'end') 'endedNatively') -eq $true -or (Step $b 'proneActivationEnded') -eq $true) 'pair B activation ended natively (End Turn, or the paired split ending it)'

 # ---- no extra native preparation ------------------------------------------------------------------------------------
 $prep=@(P $o 'preparations')
 foreach($g in ($prep | Group-Object { [string](P $_ 'role')+'@'+[string](P $_ 'round') })){
  $n=($g.Group | ForEach-Object { Int (P $_ 'count') 'preparation count' } | Measure-Object -Sum).Sum
  Need ([long]$n -le 1) ('at most one native preparation per actor and round: '+$g.Name)
 }

 # ---- inspected frames --------------------------------------------------------------------------------------------
 $shots=@(P $o 'screenshots')
 $root=if(-not [string]::IsNullOrEmpty($EvidenceRoot)){ $EvidenceRoot } else { Text (P $Request 'evidenceRoot') }
 foreach($name in Get-KmcFinalize6ShotNames){
  $shot=@($shots | Where-Object { (Text (P $_ 'name')) -ceq $name })
  Need ($shot.Count -eq 1 -and $null -eq (P $shot[0] 'error')) ('screenshot recorded once: '+$name)
  Need ((Text (P $shot[0] 'file')) -ceq ('movement-visuals/finalize3-'+$name+'.png') -and (Text (P $shot[0] 'sha256')) -cmatch '^[0-9a-f]{64}$') ('screenshot file record: '+$name)
  if(-not [string]::IsNullOrEmpty($root)){
   $path=Join-Path $root ((Text (P $shot[0] 'file')) -replace '/','\')
   Need ((Test-Path -LiteralPath $path -PathType Leaf) -and (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ceq (Text (P $shot[0] 'sha256'))) ('screenshot file present and matching: '+$name)
  }
 }
}
