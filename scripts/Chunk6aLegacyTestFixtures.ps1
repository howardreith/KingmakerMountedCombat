$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/LegacyCombatProjectionEvidence.ps1')
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
# Historical records stay immutable. The current reader must reject incomplete
# old instrumentation and an overall FAIL, even when its native row passed.
foreach($archivedRun in @('final105-p08-mammoth-tb','c6a-later-turn-a-reg-mammoth-tb')) {
 $archivedBinding=Get-KmcSupportingBinding mammoth $archivedRun @('mounted-mammoth-primary-hit-tb') $lab
 $archivedRequest=Read-KmcJson (Join-Path $lab ('runtime-evidence/'+$archivedRun+'/runtime-request.json'))
 $archivedPayload=[pscustomobject]@{version=$archivedRequest.productVersion;commit=$archivedRequest.commit;branch=$archivedRequest.branch;dllSha256=$archivedRequest.dllSha256;dllMvid=$archivedRequest.dllMvid;suiteId=$archivedRequest.qualificationSuite.suiteId;suiteSha256=$archivedRequest.qualificationSuite.snapshotSha256}
 $rejected=$false;try {Assert-KmcSupportingRun $archivedPayload $archivedBinding $lab}catch{$rejected=$true}
 if(-not$rejected){throw ('Current reader credited incomplete/failed archive '+$archivedRun)}
}
# Synthetic parser fixture ONLY, based on the modern trace. No native result,
# historical result, campaign entry or original hash is changed.
$sourceRun='c6a-later-turn-a-reg-mammoth-tb';$sourceRoot=Join-Path $lab ('runtime-evidence/'+$sourceRun)
$scratch=Join-Path $lab ('analysis-cache/chunk6a-reader-tests/'+[Guid]::NewGuid().ToString('N'))
$run='harness-test-legacy-'+[Guid]::NewGuid().ToString('N');$root=Join-Path $lab ('runtime-evidence/'+$run)
[IO.Directory]::CreateDirectory($scratch)|Out-Null;[IO.Directory]::CreateDirectory($root)|Out-Null
$v=@{}
foreach($leaf in @('runtime-request.json','runtime-game-result.json','runtime-result.json','runtime-artifacts.json','orchestration.json','combat-scenario-evidence.jsonl')) {
 $v[$leaf]=(Get-Content -Raw -LiteralPath (Join-Path $sourceRoot $leaf)).Replace($sourceRun,$run)|ConvertFrom-Json
}
$v['runtime-request.json'].evidenceRoot=$root
$game=$v['runtime-game-result.json'];$overall=$v['runtime-result.json']
foreach($field in @('status','errors','subscenarioTotal','subscenarioPassCount','subscenarioFailCount','assertionPassCount','assertionFailCount','subscenarioResults')){$overall.$field=$game.$field}
$v['orchestration.json'].status='PASS'
$transaction=(Get-Content -Raw -LiteralPath (Join-Path $lab ('runtime-state/run-transactions/'+$sourceRun+'.json'))).Replace($sourceRun,$run)|ConvertFrom-Json
function Write-LegacyFixtureJson($value,[string]$path){[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 100 -Compress),[Text.UTF8Encoding]::new($false))}
$evidence=Join-Path $root 'combat-scenario-evidence.jsonl';Write-LegacyFixtureJson $v['combat-scenario-evidence.jsonl'] $evidence
$v['runtime-artifacts.json'].artifacts[0].sha256=Get-KmcSha256 $evidence
$v['runtime-artifacts.json'].artifacts[0].length=(Get-Item -LiteralPath $evidence).Length
$manifest=Join-Path $root 'runtime-artifacts.json';Write-LegacyFixtureJson $v['runtime-artifacts.json'] $manifest
foreach($value in @($game,$overall)){$value.evidenceManifestSha256=Get-KmcSha256 $manifest}
foreach($leaf in @('runtime-request.json','runtime-game-result.json','orchestration.json')){Write-LegacyFixtureJson $v[$leaf] (Join-Path $root $leaf)}
$overall.gameResultSha256=Get-KmcSha256 (Join-Path $root 'runtime-game-result.json')
Write-LegacyFixtureJson $overall (Join-Path $root 'runtime-result.json')
Write-LegacyFixtureJson $transaction (Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json'))
$b=Get-KmcSupportingBinding mammoth $run @('mounted-mammoth-primary-hit-tb') $lab
$r=$v['runtime-request.json']
$payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256}
Assert-KmcSupportingRun $payload $b $lab
Write-Host 'SYNTHETIC modern legacy parser PASS; original105 insufficient and original131 overall FAIL both rejected; no qualification.'
function Remove-LegacyTestFixture {
 $parent=Join-Path $lab 'runtime-evidence';$owned=Assert-KmcChildPath ([IO.Path]::GetFullPath($root)) $parent 'owned synthetic legacy fixture'
 if($run-cnotmatch '^harness-test-legacy-[0-9a-f]{32}$'){throw 'Synthetic cleanup run differs'}
 Remove-Item -LiteralPath $owned -Recurse -Force
 Remove-Item -LiteralPath (Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json')) -Force
}
