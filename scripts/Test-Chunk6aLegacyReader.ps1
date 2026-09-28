. (Join-Path $PSScriptRoot 'Chunk6aLegacyTestFixtures.ps1')
$script:checks=1;$script:failures=@()
function Clone($value){$value|ConvertTo-Json -Depth 100|ConvertFrom-Json}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Reject([string]$name,[scriptblock]$action){
 $errorText=$null;try{&$action}catch{$errorText=$_.Exception.Message}
 if($null-eq$errorText){throw ('Reader accepted negative: '+$name)}
 $script:checks++;$script:failures+=@([pscustomobject]@{name=$name;assertion=$errorText});Write-Host ('PASS reject '+$name)
}
foreach($key in @('resultSha256','gameResultSha256','requestSha256','orchestrationSha256','transactionSha256','evidenceSha256','artifactManifestSha256')) {
 $wrong=Clone $b;$wrong.$key='0'*64;Reject $key {Assert-KmcSupportingRun $payload $wrong $lab}
}
foreach($key in @('version','commit','branch','dllSha256','dllMvid','suiteId','suiteSha256')) {
 $wrong=Clone $payload;$wrong.$key='wrong';Reject $key {Assert-KmcSupportingRun $wrong $b $lab}
}
foreach($leaf in @('phase3d-horse-scenario-evidence.json','other.jsonl','../combat-scenario-evidence.jsonl')) {
 $wrong=Clone $b;$wrong.evidenceLeaf=$leaf;Reject ('leaf '+$leaf) {Assert-KmcSupportingRun $payload $wrong $lab}
}
$wrong=Clone $b;$wrong.rows=@('unrelated');Reject row {Assert-KmcSupportingRun $payload $wrong $lab}
$wrong=Clone $b;$wrong.scenario='chunk6a-mount-approach';Reject scenario {Assert-KmcSupportingRun $payload $wrong $lab}
$original=@{}
foreach($leaf in @('runtime-result.json','runtime-game-result.json','runtime-request.json','orchestration.json','runtime-artifacts.json','combat-scenario-evidence.jsonl')) {
 $original[$leaf]=Get-Content -LiteralPath (Join-Path $root $leaf) -Raw|ConvertFrom-Json
}
$original.transaction=Get-Content -LiteralPath (Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json')) -Raw|ConvertFrom-Json
$fixtureRoot=Join-Path $scratch ('fixtures-'+[Guid]::NewGuid().ToString('N'))
function Write-TestJson($value,[string]$path,[bool]$compact=$false){[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 100 -Compress:$compact),[Text.UTF8Encoding]::new($false))}
function Fixture([string]$name,[scriptblock]$mutate,[bool]$positive=$false){
 $base=Join-Path $fixtureRoot $name;$path=Join-Path $base ('runtime-evidence/'+$run);$trans=Join-Path $base 'runtime-state/run-transactions'
 New-Item -ItemType Directory -Path $path,$trans -Force|Out-Null
 $v=@{};foreach($key in $original.Keys){$v[$key]=Clone $original[$key]}
 $v['runtime-request.json'].evidenceRoot=$path
 &$mutate $v
 $evidencePath=Join-Path $path 'combat-scenario-evidence.jsonl'
 Write-TestJson $v['combat-scenario-evidence.jsonl'] $evidencePath $true
 $v['runtime-artifacts.json'].artifacts[0].sha256=Hash $evidencePath
 $v['runtime-artifacts.json'].artifacts[0].length=(Get-Item -LiteralPath $evidencePath).Length
 $manifestPath=Join-Path $path 'runtime-artifacts.json'
 Write-TestJson $v['runtime-artifacts.json'] $manifestPath
 foreach($key in @('runtime-game-result.json','runtime-result.json')){$v[$key].evidenceManifestSha256=Hash $manifestPath}
 foreach($key in @('runtime-game-result.json','runtime-request.json','orchestration.json')){Write-TestJson $v[$key] (Join-Path $path $key)}
 $v['runtime-result.json'].gameResultSha256=Hash (Join-Path $path 'runtime-game-result.json')
 Write-TestJson $v['runtime-result.json'] (Join-Path $path 'runtime-result.json')
 Write-TestJson $v.transaction (Join-Path $trans ($run+'.json'))
 $binding=Get-KmcSupportingBinding mammoth $run @('mounted-mammoth-primary-hit-tb') $base
 if($positive){Assert-KmcSupportingRun $payload $binding $base;$script:checks++;Write-Host ('PASS '+$name)}else{Reject $name {Assert-KmcSupportingRun $payload $binding $base}}
}
Fixture baseline {param($v)} $true
Fixture native-field {param($v) $v['combat-scenario-evidence.jsonl'].pose.healthyAtOutcome=$false}
Fixture action-resource {param($v) $v['combat-scenario-evidence.jsonl'].resources.riderStandardAfter=3}
Fixture cleanup {param($v) $v['combat-scenario-evidence.jsonl'].cleanup.residualState=$true}
Fixture native-row {param($v) $v['combat-scenario-evidence.jsonl'].row='wrong'}
Fixture native-frame {param($v) $v['combat-scenario-evidence.jsonl'].frame=0}
Fixture native-subscenario {param($v) $v['runtime-game-result.json'].subscenarioResults[0].name='wrong'}
Fixture overall-subscenario {param($v) $v['runtime-result.json'].subscenarioResults[0].status='FAIL'}
Fixture overall-fail-native-pass {param($v) $v['runtime-result.json'].status='FAIL'}
Fixture native-fail {param($v) $v['runtime-game-result.json'].status='FAIL'}
Fixture evidence-payload {param($v) $v['combat-scenario-evidence.jsonl'].commit='wrong'}
Fixture request-suite {param($v) $v['runtime-request.json'].qualificationSuite.snapshotSha256='0'*64}
Fixture request-payload {param($v) $v['runtime-request.json'].dllMvid='wrong'}
Fixture transaction-suite {param($v) $v.transaction.qualificationSuiteSnapshotSha256='0'*64}
Fixture transaction-token {param($v) $v.transaction.token='wrong'}
Fixture transaction-not-restored {param($v) $v.transaction.phase='restoring'}
Fixture inventory {param($v) $v.transaction.restoredModsDigest='0'*64}
foreach($field in @('modsRestored','saveProtectionPassed','baselineImmutable','workingRestored','saveWriteAllowlistPassed')) {
 Fixture ('result-'+$field) {param($v) $v['runtime-result.json'].$field=$false}
 Fixture ('transaction-'+$field) {param($v) $v.transaction.$field=$false}
}
[pscustomobject]@{status='PASS';checks=$script:checks;fail=0;scope='Draft parser/component regression only; original105 artifact, no current qualification';originalEvidenceSha256=$b.evidenceSha256;fixtures=$fixtureRoot;rejected=$script:failures}|ConvertTo-Json -Depth 15|Set-Content -LiteralPath (Join-Path $scratch 'Legacy-DRAFT-receipt.json') -Encoding UTF8
Write-Host ('LEGACY READER PASS='+$script:checks+' FAIL=0')
