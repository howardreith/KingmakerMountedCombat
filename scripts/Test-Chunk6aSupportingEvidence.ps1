# Supporting-run regressions read historical artifacts and mutate only synthetic lab copies.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
$lab=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$payload=(Get-Content -Raw (Join-Path $lab 'analysis-cache/chunk6a-causal/ledger-preview120.json')|ConvertFrom-Json).payload
function Hash([string]$p) { (Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant() }
function Bind([string]$mode) {
 $run='c6a-interruption-a-'+$mode;$root=Join-Path $lab ('runtime-evidence/'+$run)
 $r=Get-Content -Raw (Join-Path $root 'runtime-result.json')|ConvertFrom-Json
 [pscustomobject]@{role=$mode;runId=$run;scenario=$r.scenario;passCount=$r.assertionPassCount;failCount=0;
 rows=@('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases');evidenceLeaf='phase3d-horse-scenario-evidence.json';
 evidenceSha256=Hash (Join-Path $root 'phase3d-horse-scenario-evidence.json');resultSha256=Hash (Join-Path $root 'runtime-result.json');
 gameResultSha256=Hash (Join-Path $root 'runtime-game-result.json');requestSha256=Hash (Join-Path $root 'runtime-request.json');
 orchestrationSha256=Hash (Join-Path $root 'orchestration.json');transactionSha256=Hash (Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json'))}
}
$bindings=@((Bind rt),(Bind tb))
$roles=@([pscustomobject]@{name='rt';scenario='chunk6a-adoption-compensation-rt';rows=@('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases')},
 [pscustomobject]@{name='tb';scenario='chunk6a-adoption-compensation-tb';rows=@('CM02-adoption-plan-invalidated','CM02-adoption-compensation-releases')})
$script:checks=0
function Reject([scriptblock]$Action,[string]$Expected) {
 $rejected=$false
 try { & $Action } catch { if($_.Exception.Message.IndexOf($Expected,[StringComparison]::Ordinal)-lt0){throw};$rejected=$true }
 if(-not$rejected){throw "Validator accepted invalid evidence: $Expected"};$script:checks++
}
function Copy-Json($Value) { $Value | ConvertTo-Json -Depth 100 | ConvertFrom-Json }
Assert-KmcCompositeRuns $payload $bindings $roles $lab;$script:checks++
Reject { Assert-KmcCompositeRuns $payload @($bindings[0]) $roles $lab } 'lacks required roles'
$b=Copy-Json $bindings;$b[1].runId=$b[0].runId
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'separate native transactions'
$b=Copy-Json $bindings;$b[1].role='rt'
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'exactly one'
$b=Copy-Json $bindings;$b[1].scenario='chunk6a-adoption-compensation-rt'
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'declared scenario'
$b=Copy-Json $bindings;$b[1].rows=@('CM02-adoption-plan-invalidated')
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'omits a required row'
foreach($key in @('commit','version','dllSha256','dllMvid')) {
 $p=Copy-Json $payload;$p.$key='wrong'
 Reject { Assert-KmcCompositeRuns $p $bindings $roles $lab } 'another payload'
}
$p=Copy-Json $payload;$p.suiteId='wrong'
Reject { Assert-KmcCompositeRuns $p $bindings $roles $lab } 'another qualification suite'
$b=Copy-Json $bindings;$b[1].evidenceSha256='0'*64
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'bytes differ'
$b=Copy-Json $bindings;$b[1].rows+=@('missing mandatory row')
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'lacks exact PASS row'
$b=Copy-Json $bindings;$b[1].evidenceLeaf='../runtime-result.json'
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'one JSON leaf'
$b=Copy-Json $bindings;$b[1].passCount++
Reject { Assert-KmcCompositeRuns $payload $b $roles $lab } 'not an exact PASS'

# Semantic corruptions are rehashed in an isolated lab fixture so byte binding alone
# cannot hide a missing semantic assertion. Original runtime evidence remains intact.
$fixtureParent=Join-Path (Join-Path $lab 'analysis-cache/chunk6a-causal') ('supporting-protocol-fixtures-'+[Guid]::NewGuid().ToString('N'))
function Fixture-Reject([scriptblock]$Mutation,[string]$Expected) {
 $fixture=Join-Path $fixtureParent ([Guid]::NewGuid().ToString('N'))
 $run=$bindings[0].runId;$root=Join-Path $fixture ('runtime-evidence/'+$run)
 $transactions=Join-Path $fixture 'runtime-state/run-transactions'
 [void](New-Item -ItemType Directory -Path $root -Force)
 [void](New-Item -ItemType Directory -Path $transactions -Force)
 $original=Join-Path $lab ('runtime-evidence/'+$run)
 $bundle=@{}
 foreach($leaf in @('runtime-result.json','runtime-game-result.json','runtime-request.json','orchestration.json','phase3d-horse-scenario-evidence.json')) {
  $bundle[$leaf]=Get-Content -Raw (Join-Path $original $leaf)|ConvertFrom-Json
 }
 $bundle['transaction']=Get-Content -Raw (Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json'))|ConvertFrom-Json
 & $Mutation $bundle
 foreach($leaf in @('runtime-game-result.json','runtime-request.json','orchestration.json','phase3d-horse-scenario-evidence.json')) {
  $bundle[$leaf]|ConvertTo-Json -Depth 100|Set-Content -LiteralPath (Join-Path $root $leaf) -Encoding UTF8
 }
 $bundle['runtime-result.json'].gameResultSha256=Hash (Join-Path $root 'runtime-game-result.json')
 $bundle['runtime-result.json']|ConvertTo-Json -Depth 100|Set-Content -LiteralPath (Join-Path $root 'runtime-result.json') -Encoding UTF8
 $bundle['transaction']|ConvertTo-Json -Depth 100|Set-Content -LiteralPath (Join-Path $transactions ($run+'.json')) -Encoding UTF8
 $bound=Copy-Json $bindings[0]
 foreach($pair in @(@('resultSha256','runtime-result.json'),@('gameResultSha256','runtime-game-result.json'),@('requestSha256','runtime-request.json'),@('orchestrationSha256','orchestration.json'),@('evidenceSha256','phase3d-horse-scenario-evidence.json'))) {
  $bound.($pair[0])=Hash (Join-Path $root $pair[1])
 }
 $bound.transactionSha256=Hash (Join-Path $transactions ($run+'.json'))
 Reject { Assert-KmcSupportingRun $payload $bound $fixture } $Expected
}
Fixture-Reject {param($v) $v['runtime-result.json'].status='FAIL'} 'not an exact PASS'
Fixture-Reject {param($v) $v['runtime-game-result.json'].status='FAIL'} 'not an exact PASS'
Fixture-Reject {param($v) $v['runtime-request.json'].dllMvid='00000000-0000-0000-0000-000000000000'} 'another payload'
Fixture-Reject {param($v) $v['runtime-request.json'].qualificationSuite.snapshotSha256='0'*64} 'another qualification suite'
foreach($field in @('modsRestored','saveProtectionPassed','baselineImmutable','workingRestored','saveWriteAllowlistPassed')) {
 Fixture-Reject {param($v) $v['runtime-result.json'].$field=$false} 'Supporting restoration failed'
 Fixture-Reject {param($v) $v['transaction'].$field=$false} 'Supporting restoration failed'
}
Fixture-Reject {param($v) $v['transaction'].phase='restoring'} 'not settled and restored'
Fixture-Reject {param($v) $v['transaction'].token='wrong'} 'transaction token differs'
Fixture-Reject {param($v) $v['transaction'].restoredSaveInventoryDigest='0'*64} 'restoration inventory differs'
Fixture-Reject {param($v) $v['orchestration.json'].stage='restore-failed'} 'not settled and restored'
Fixture-Reject {param($v) $v['phase3d-horse-scenario-evidence.json'].rows=@($v['phase3d-horse-scenario-evidence.json'].rows|Where-Object { $_.name -cne 'CM02-adoption-plan-invalidated' })} 'lacks exact PASS row'
Fixture-Reject {param($v) $row=@($v['phase3d-horse-scenario-evidence.json'].rows|Where-Object { $_.name -ceq 'CM02-adoption-plan-invalidated' })[0];$v['phase3d-horse-scenario-evidence.json'].rows+=@($row)} 'lacks exact PASS row'
Write-Host ('Synthetic test fixtures retained: '+$fixtureParent)

Fixture-Reject {param($v) $v['phase3d-horse-scenario-evidence.json'].commit='wrong'} 'artifact payload or run differs'
Fixture-Reject {param($v) $v['phase3d-horse-scenario-evidence.json'].status='FAIL'} 'artifact is not PASS'
# Roles are an exact set, never a chronology: the declared order only sorts the ledger.
$sorted=Sort-KmcCompositeBindings @($bindings[1],$bindings[0]) $roles
if((($sorted|ForEach-Object role) -join ',') -cne 'rt,tb'){throw 'Composite bindings were not sorted into the canonical declared order.'};$script:checks++
Assert-KmcCompositeRoleSet $bindings $roles;$script:checks++
Reject { Assert-KmcCompositeRoleSet @($bindings[1],$bindings[0]) $roles } 'canonical declared role order'
$b=Copy-Json $bindings;$b[1].role='foreign'
Reject { Assert-KmcCompositeRoleSet $b $roles } 'foreign role'
Reject { Sort-KmcCompositeBindings $b $roles } 'exactly one tb run'
$b=Copy-Json $bindings;$b[0].role='tb'
Reject { Assert-KmcCompositeRoleSet $b $roles } 'exactly one rt run'
Reject { Assert-KmcCompositeRuns $payload @($bindings[1],$bindings[0]) $roles $lab } 'canonical declared role order'
if($null -ne (Get-Command -Name Assert-KmcCompositeRunOrder -CommandType Function -ErrorAction SilentlyContinue)){throw 'Chronological composite order rule must not exist.'};$script:checks++

# Fixed six/seven-role obligations cannot be selected by the ledger writer.
$campaignRoles=@(Get-KmcChunk6aCampaignRoles $true)
if(($campaignRoles.name -join ',') -cne 'positive,compensation-rt,compensation-tb,geometry,obstruction,full-rt,full-tb' -or
    @(Get-KmcChunk6aCampaignRoles $false).Count -ne 6) { throw 'Fixed campaign obligations changed.' };$script:checks++
$p121=(Get-Content -Raw (Join-Path $lab 'analysis-cache/chunk6a-causal/ledger-preview121.json')|ConvertFrom-Json).payload
Assert-KmcChunk6aFrozenPayload $p121 $lab;$script:checks++
foreach($key in @('commit','dllSha256','dllMvid','version')) {
    $wrong=Copy-Json $p121;$wrong.$key='wrong'
    Reject {Assert-KmcChunk6aFrozenPayload $wrong $lab} 'manifest payload differs'
}
$wrong=Copy-Json $p121;$wrong.packageSha256='0'*64
Reject {Assert-KmcChunk6aFrozenPayload $wrong $lab} 'package bytes differ'
$wrong=Copy-Json $p121;$wrong.suiteSha256='0'*64
Reject {Assert-KmcChunk6aFrozenPayload $wrong $lab} 'bytes differ'
$runNames=@('approach','rt','tb','geometry','obstruction','full-rt','full-tb')
$campaign=@(for($index=0;$index -lt $campaignRoles.Count;$index++) {
    Get-KmcSupportingBinding $campaignRoles[$index].name ('c6a-envelope-a-'+$runNames[$index]) $campaignRoles[$index].rows $lab
})
# Role-set validation alone is not qualification: both full cases still FAILED.
Assert-KmcCompositeRoleSet $campaign $campaignRoles;$script:checks++
foreach($missing in @('geometry','obstruction','full-rt','full-tb')) {
    $incomplete=@($campaign|Where-Object role -CNE $missing)
    Reject {Assert-KmcChunk6aCampaign $p121 $incomplete $true $lab} 'lacks required roles'
}
Reject {Assert-KmcChunk6aCampaign $p121 $campaign $true $lab} 'not an exact PASS'

Write-Host "SUPPORTING-RUN CHECKS PASS=$script:checks FAIL=0; historical regression only; no current qualification."
