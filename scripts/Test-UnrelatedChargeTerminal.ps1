$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/UnrelatedChargeTerminalEvidence.ps1')
$kmcLabRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'));$kmcTestRoot=Join-Path $kmcLabRoot ('analysis-cache/unrelated-terminal-tests/'+[Guid]::NewGuid().ToString('N'));[IO.Directory]::CreateDirectory($kmcTestRoot)|Out-Null
$results=@()
function Copy-Unrelated($v){$v|ConvertTo-Json -Depth 100 -Compress|ConvertFrom-Json}
foreach($mode in @('RT','TB')){
 $run=if($mode-ceq'RT'){'20260920-chunk4-GJ'}else{'20260920-chunk4-GK'}
 $source=(Join-Path $kmcLabRoot ('runtime-evidence/'+$run+'/phase3d-horse-scenario-evidence.json'))
 $hash=(Get-FileHash -LiteralPath $source).Hash
 $original=Get-Content -LiteralPath $source -Raw|ConvertFrom-Json
 $p=[pscustomobject]@{rows=@($original.rows|Where-Object name -CEQ 'C4-CHARGE-unrelated-actor');observations=[pscustomobject]@{'chargeTraceBefore-C4-CHARGE-queued-state-change'=$original.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'}}
 Assert-KmcUnrelatedChargeTerminal $p $mode
 $results+=@([pscustomobject]@{mode=$mode;case='historical-exact-terminal';pass=$true;sourceSha256=$hash.ToLowerInvariant()})
 $mutations=[ordered]@{
  'missing-row'={param($a)$a.rows=@()}
  'duplicate-row'={param($a)$a.rows+=@($a.rows[0])}
  'failed-row'={param($a)$a.rows[0].status='FAIL'}
  'wrong-mode'={param($a)$a.rows[0].evidence.mode='foreign'}
  'pair-actor'={param($a)$a.rows[0].evidence.actorId=$a.rows[0].evidence.before.rider.id}
  'pair-changed'={param($a)$a.rows[0].evidence.after.relationship='Unmounted'}
  'warnings'={param($a)$a.rows[0].evidence.warningDelta=1}
  'false-terminal-flag'={param($a)$a.rows[0].evidence.nativeChargeCompleted=$false}
  'string-terminal-flag'={param($a)$a.rows[0].evidence.nativeChargeCompleted='true'}
  'selector-input-identity'={param($a)$a.rows[0].evidence.inputWindow.actorsAfterInput.actor.raw[1].id++}
  'input-second-command'={param($a)$a.rows[0].evidence.inputWindow.actorsAfterInput.actor.queue=@($a.rows[0].evidence.inputWindow.actorsAfterInput.actor.raw[1])}
  'input-acted'={param($a)$a.rows[0].evidence.inputWindow.actorsAfterInput.actor.raw[1].acted=$true}
  'trace-missing'={param($a)$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'=$null}
  'trace-dropped'={param($a)$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.dropped=1}
  'trace-other-case'={param($a)foreach($v in $a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events){$v.caseId='foreign'}}
  'trace-index'={param($a)$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events[-1].index=-1}
  'trace-actor'={param($a)$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events[-1].actor='foreign'}
  'trace-time'={param($a)$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events[-1].gameTime=0}
  'missing-shell-end'={param($a)$t=$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change';$t.events=@($t.events|Where-Object {-not($_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility')})}
  'missing-attack-end'={param($a)$t=$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change';$t.events=@($t.events|Where-Object {-not($_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack')})}
  'unfinished-attack'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'}).finished=$false}
  'incomplete-attack'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'}).completed=0}
  'foreign-shell-end'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility'}).command++}
  'foreign-attack-end'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'}).command++}
  'bad-shell-result'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility'}).result='Interrupt'}
  'bad-attack-result'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'ended'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'}).result='Fail'}
  'second-cost'={param($a)$t=$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change';$v=Copy-Unrelated ($t.events|Where-Object boundary -CEQ 'cost-after'|Select-Object -First 1);$v.index=10000;$v.frame=1000000;$v.gameTime=[long]9999999999999;$t.events+=@($v)}
  'extra-standard'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'cost-after'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility'}).standard++}
  'extra-move'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'cost-after'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitUseAbility'}).move++}
  'attack-second-charge'={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object {$_.boundary-ceq'cost-after'-and$_.commandType-ceq'Kingmaker.UnitLogic.Commands.UnitAttack'}).standard++}
  'wrong-target'={param($a)$a.rows[0].evidence.samples[0].shell.targetId='foreign'}
  'missing-rule'={param($a)$a.rows[0].evidence.rules.attackRuleEvents=@()}
  'wrong-rule-actor'={param($a)$a.rows[0].evidence.rules.attackRuleEvents[0].actorId='foreign'}
  'not-charge-rule'={param($a)$a.rows[0].evidence.rules.attackRuleEvents[0].charge=$false}

  'string-charge-rule'={param($a)$a.rows[0].evidence.rules.attackRuleEvents[0].charge='true'}
  'string-opportunity-rule'={param($a)$a.rows[0].evidence.rules.attackRuleEvents[0].attackOfOpportunity='false'}
  'string-warning-count'={param($a)$a.rows[0].evidence.warningDelta='0'}
  'fractional-resolved-count'={param($a)$a.rows[0].evidence.rules.riderResolved=1.0}
  'string-resolved-count'={param($a)$a.rows[0].evidence.rules.riderResolved='1'}
  'string-forced-count'={param($a)$a.rows[0].evidence.rules.pairForcedD20='0'}
  'forced-roll'={param($a)$a.rows[0].evidence.rules.pairForcedD20=1}
 }
 if($mode-ceq'TB'){
  $mutations['missing-recovery']={param($a)$t=$a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change';$t.events=@($t.events|Where-Object boundary -CNE 'native-recovery-interrupt')}
  $mutations['foreign-recovery']={param($a)($a.observations.'chargeTraceBefore-C4-CHARGE-queued-state-change'.events|Where-Object boundary -CEQ 'native-recovery-interrupt').detail='cleanup stop'}
 }
 foreach($case in $mutations.Keys){
  $a=Copy-Unrelated $p;& $mutations[$case] $a;$errorText=$null
  try{Assert-KmcUnrelatedChargeTerminal $a $mode}catch{$errorText=$_.Exception.Message}
  if($null-eq$errorText){throw ('Accepted malformed unrelated terminal: '+$mode+'/'+$case)}
  $results+=@([pscustomobject]@{mode=$mode;case=$case;pass=$true;refusal=$errorText})
 }
 if((Get-FileHash -LiteralPath $source).Hash-cne$hash){throw 'Original artifact changed'}
}
[pscustomobject]@{status='PASS';scope='Historical parser fixtures and negative copies only; no native qualification';atUtc=[DateTime]::UtcNow.ToString('o');checks=$results.Count;results=$results}|ConvertTo-Json -Depth 7|Set-Content -LiteralPath (Join-Path $kmcTestRoot 'terminal-test-receipt.json') -Encoding UTF8
Write-Host ('UNRELATED EXACT TERMINAL PASS='+$results.Count+' FAIL=0; draft only')
