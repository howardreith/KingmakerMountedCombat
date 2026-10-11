$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$draft=Join-Path $lab 'analysis-cache/chunk6a-area-tests';$run='20260920-chunk4-GZ'
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aPrimaryClaimEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aArtifactRowsEvidence.ps1')
$originalRoot=Join-Path $lab ('runtime-evidence/'+$run)
$original=@{};foreach($leaf in @('runtime-request.json','runtime-game-result.json','runtime-result.json','runtime-artifacts.json','orchestration.json')){$original[$leaf]=Get-Content -Raw -LiteralPath (Join-Path $originalRoot $leaf)}
$original.transaction=Get-Content -Raw -LiteralPath (Join-Path $lab ('runtime-state/run-transactions/'+$run+'.json'))
$original.records='['+((Get-Content -LiteralPath (Join-Path $originalRoot 'boundary-scenario-evidence.jsonl'))-join',')+']'
$r=$original['runtime-request.json']|ConvertFrom-Json
$payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256}
$scratch=Join-Path $draft ('synthetic-'+[Guid]::NewGuid().ToString('N'));$root=Join-Path $scratch ('runtime-evidence/'+$run);$trans=Join-Path $scratch 'runtime-state/run-transactions'
[IO.Directory]::CreateDirectory($root)|Out-Null;[IO.Directory]::CreateDirectory($trans)|Out-Null
$sourceHashes=@(foreach($leaf in @('runtime-request.json','runtime-game-result.json','runtime-result.json','runtime-artifacts.json','orchestration.json','boundary-scenario-evidence.jsonl')){[pscustomobject]@{path=Join-Path $originalRoot $leaf;hash=(Get-FileHash -LiteralPath (Join-Path $originalRoot $leaf)).Hash}})
function Write-AreaJson($value,[string]$path){[IO.File]::WriteAllText($path,($value|ConvertTo-Json -Depth 100 -Compress),[Text.UTF8Encoding]::new($false))}
function Hash-Area([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
$results=@()
function Case-Area([string]$name,[scriptblock]$change,[scriptblock]$changeBinding={},[bool]$pass=$false){
 $v=@{};foreach($key in $original.Keys){$v[$key]=$original[$key]|ConvertFrom-Json}
 $v['runtime-request.json'].evidenceRoot=$root
 & $change $v
 $artifactPath=Join-Path $root 'boundary-scenario-evidence.jsonl'
 $lines=@($v.records|ForEach-Object {$_|ConvertTo-Json -Depth 100 -Compress})
 if($name-ceq'duplicate-json-member'){$lines[0]=$lines[0].Insert(1,'"scenario":"chunk4-area-cleanup",')}
 [IO.File]::WriteAllText($artifactPath,($lines-join[Environment]::NewLine)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
 $entry=@($v['runtime-artifacts.json'].artifacts|Where-Object relativePath -CEQ 'boundary-scenario-evidence.jsonl')[0]
 $entry.sha256=Hash-Area $artifactPath;$entry.length=(Get-Item -LiteralPath $artifactPath).Length
 $manifest=Join-Path $root 'runtime-artifacts.json';Write-AreaJson $v['runtime-artifacts.json'] $manifest
 foreach($key in @('runtime-game-result.json','runtime-result.json')){$v[$key].evidenceManifestSha256=Hash-Area $manifest}
 foreach($key in @('runtime-request.json','runtime-game-result.json','orchestration.json')){Write-AreaJson $v[$key] (Join-Path $root $key)}
 $v['runtime-result.json'].gameResultSha256=Hash-Area (Join-Path $root 'runtime-game-result.json')
 Write-AreaJson $v['runtime-result.json'] (Join-Path $root 'runtime-result.json')
 Write-AreaJson $v.transaction (Join-Path $trans ($run+'.json'))
 $binding=Get-KmcSupportingBinding 'area-regression' $run @('native-area-clean-dismount') $scratch
 & $changeBinding $binding
 $errorText=$null;try{Assert-KmcChunk6aPrimaryClaim 'CM08-area-restoration' $binding;Assert-KmcSupportingRun $payload $binding $scratch}catch{$errorText=$_.Exception.Message}
 if(($null-eq$errorText)-ne$pass){throw ('Unexpected '+$name+': '+$errorText)}
 $script:results+=@([pscustomobject]@{case=$name;expectedPass=$pass;assertion=$errorText})
 Write-Host ('PASS '+$name)
}
Case-Area baseline {} {} $true
Case-Area configuration {param($v)$v.records[-1].pairedConfiguration.enablePairedActivation=$false}
Case-Area native-dispatch {param($v)$v.records[-1].triggerScope.realAreaReloadDispatched=$false}
Case-Area native-delivery {param($v)$v.records[-1].triggerScope.nativeDeliveryObserved=$false}
Case-Area fresh-world {param($v)$v.records[-1].freshWorld.allClean=$false}
Case-Area agent-restoration {param($v)$v.records[-1].cleanup.movementAgentReleased=$false}
Case-Area selection {param($v)$v.records[-1].relationship.selectedUnitIds=@('foreign')}
Case-Area working-identity {param($v)$v.records[-1].workingIdentity.observedSha256='0'*64}
Case-Area missing-phase {param($v)$v.records=@($v.records|Where-Object phase -CNE 'loading-start')}
Case-Area duplicate-terminal {param($v)$v.records+=@($v.records[-1])}
Case-Area native-count {param($v)$v['runtime-game-result.json'].subscenarioResults[0].assertionPassCount=46}
Case-Area outer-count {param($v)$v['runtime-result.json'].subscenarioResults[0].assertionPassCount=46}
Case-Area overall-failed {param($v)$v['runtime-result.json'].status='FAIL'}
Case-Area native-failed {param($v)$v['runtime-game-result.json'].status='FAIL'}
Case-Area restoration {param($v)$v.transaction.modsRestored=$false}
Case-Area suite {param($v)$v['runtime-request.json'].qualificationSuite.snapshotSha256='0'*64}
Case-Area payload {param($v)$v['runtime-game-result.json'].dllMvid='00000000-0000-0000-0000-000000000000'}
Case-Area manifest-kind {param($v)$v['runtime-artifacts.json'].artifacts[0].kind='foreign'}
Case-Area duplicate-json-member {}
Case-Area row-binding {} {param($b)$b.rows=@('unrelated')}
Case-Area scenario-binding {} {param($b)$b.scenario='chunk4-session-rt'}
Case-Area manifest-hash {} {param($b)$b.artifactManifestSha256='0'*64}
Case-Area artifact-hash {} {param($b)$b.evidenceSha256='0'*64}
foreach($source in $sourceHashes){if((Get-FileHash -LiteralPath $source.path).Hash-cne$source.hash){throw 'Original54 evidence changed'}}
[pscustomobject]@{status='PASS';atUtc=[DateTime]::UtcNow.ToString('o');checks=$results.Count;scope='Synthetic area reader tests from untouched54 archive; no current qualification';sourceRun=$run;sourceHashes=$sourceHashes;scratch=$scratch;results=$results}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $scratch 'test-receipt.json') -Encoding UTF8
'AREA REGRESSION READER PASS='+$results.Count+' FAIL=0; future synthetic draft only'
