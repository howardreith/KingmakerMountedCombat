$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
. (Join-Path $repo 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aRegressionEvidence.ps1')
$run='final105-p08-ordinary-tb';$originalRoot=Join-Path $lab ('runtime-evidence/'+$run)
$role=@(Get-KmcChunk6aRegressionRoles 'CM08-ordinary-attack-controls-tb')[0]
$original=@{};foreach($leaf in @('runtime-request.json','runtime-game-result.json','runtime-result.json','orchestration.json','runtime-artifacts.json','phase3d-horse-scenario-evidence.json')){$original[$leaf]=[IO.File]::ReadAllText((Join-Path $originalRoot $leaf))}
$original.transaction=[IO.File]::ReadAllText((Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json')))
$r=$original['runtime-request.json']|ConvertFrom-Json
$payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256}
$scratch=Join-Path $lab ('analysis-cache/chunk6a-regression-tests/synthetic-'+[Guid]::NewGuid().ToString('N'));$root=Join-Path $scratch ('runtime-evidence/'+$run);$trans=Join-Path $scratch 'runtime-state/run-transactions'
[IO.Directory]::CreateDirectory($root)|Out-Null;[IO.Directory]::CreateDirectory($trans)|Out-Null
$originalManifest=$original['runtime-artifacts.json']|ConvertFrom-Json
foreach($record in $originalManifest.artifacts){
 $dest=[IO.Path]::GetFullPath((Join-Path $root $record.relativePath));if(-not$dest.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Synthetic copy escaped root'}
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest))|Out-Null
 [IO.File]::Copy((Join-Path $originalRoot $record.relativePath),$dest,$false)
}
function Hash-Regression([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Write-RegressionJson($v,[string]$p){[IO.File]::WriteAllText($p,($v|ConvertTo-Json -Depth 100 -Compress),[Text.UTF8Encoding]::new($false))}
$checks=0;$results=@()
function Fixture([string]$name,[scriptblock]$Change,[bool]$pass=$false){
 $v=@{};foreach($key in $original.Keys){$v[$key]=$original[$key]|ConvertFrom-Json}
 $v['runtime-request.json'].evidenceRoot=$root;& $Change $v
 $artifactPath=Join-Path $root 'phase3d-horse-scenario-evidence.json';Write-RegressionJson $v['phase3d-horse-scenario-evidence.json'] $artifactPath
 $entry=@($v['runtime-artifacts.json'].artifacts|Where-Object relativePath -CEQ 'phase3d-horse-scenario-evidence.json')[0]
 $entry.sha256=Hash-Regression $artifactPath;$entry.length=(Get-Item -LiteralPath $artifactPath).Length
 $manifestPath=Join-Path $root 'runtime-artifacts.json';Write-RegressionJson $v['runtime-artifacts.json'] $manifestPath
 foreach($key in @('runtime-game-result.json','runtime-result.json')){$v[$key].evidenceManifestSha256=Hash-Regression $manifestPath}
 foreach($key in @('runtime-game-result.json','runtime-request.json','orchestration.json')){Write-RegressionJson $v[$key] (Join-Path $root $key)}
 $v['runtime-result.json'].gameResultSha256=Hash-Regression (Join-Path $root 'runtime-game-result.json')
 Write-RegressionJson $v['runtime-result.json'] (Join-Path $root 'runtime-result.json')
 Write-RegressionJson $v.transaction (Join-Path $trans ($run+'.json'))
 $binding=Get-KmcChunk6aRegressionBinding $role.name $run $role.rows $scratch
 $errorText=$null;try{Assert-KmcChunk6aRegressionRun $payload $binding $scratch}catch{$errorText=$_.Exception.Message}
 if(($null-eq$errorText)-ne$pass){throw ('Unexpected '+$name+' result: '+$errorText)}
 $script:checks++;$script:results+=@([pscustomobject]@{case=$name;expectedPass=$pass;assertion=$errorText;evidenceSha256=$binding.evidenceSha256});Write-Host ('PASS '+$name)
}
Fixture baseline {param($v)} $true
Fixture native-configuration {param($v)$v['phase3d-horse-scenario-evidence.json'].observations.phase3fActualConfiguration.enablePairedActivation=$false}
Fixture native-command-executor {param($v)$v['phase3d-horse-scenario-evidence.json'].rows[0].evidence.nativeCommand.executor='foreign'}
Fixture native-row-count {param($v)@($v['runtime-game-result.json'].subscenarioResults|Where-Object name -CEQ 'C01-B')[0].assertionPassCount=2}
Fixture native-row-missing {param($v)$v['runtime-game-result.json'].subscenarioResults=@($v['runtime-game-result.json'].subscenarioResults|Where-Object name -CNE 'C01-B')}
Fixture outer-row-failed {param($v)@($v['runtime-result.json'].subscenarioResults|Where-Object name -CEQ 'C01-B')[0].status='FAIL'}
Fixture overall-failed {param($v)$v['runtime-result.json'].status='FAIL'}
Fixture manifest-kind {param($v)@($v['runtime-artifacts.json'].artifacts|Where-Object relativePath -CEQ 'phase3d-horse-scenario-evidence.json')[0].kind='foreign'}
Fixture restoration {param($v)$v.transaction.modsRestored=$false}
Fixture suite {param($v)$v['runtime-request.json'].qualificationSuite.snapshotSha256='0'*64}
$afterManifest=Hash-Regression (Join-Path $originalRoot 'runtime-artifacts.json');$beforeBytes=[Text.Encoding]::UTF8.GetBytes($original['runtime-artifacts.json']);$sha=[Security.Cryptography.SHA256]::Create();try{$beforeManifest=([BitConverter]::ToString($sha.ComputeHash($beforeBytes))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
if($beforeManifest-cne$afterManifest){throw 'Original archive manifest changed'}
[pscustomobject]@{status='PASS';checks=$checks;nativeQualification=$false;scope='Synthetic rebinding of immutable105 archive; no current qualification';scratch=$scratch;results=$results}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $scratch 'reader-receipt.json') -Encoding UTF8
'REGRESSION FULL READER PASS='+$checks+' FAIL=0; synthetic only'