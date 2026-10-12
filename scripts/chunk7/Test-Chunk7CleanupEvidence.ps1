$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'Initialize-Chunk7Tests.ps1')
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $repo 'scripts/runtime/Chunk7CleanupEvidence.ps1')
$passed=0
function Check([string]$Name,[scriptblock]$Body){&$Body;$script:passed++;Write-Host ('PASS '+$Name)}
function Reject([string]$Name,[scriptblock]$Body){$caught=$false;try{&$Body}catch{$caught=$true};if(-not$caught){throw ('Unexpected acceptance: '+$Name)};$script:passed++;Write-Host ('PASS refuses '+$Name)}
$testRoot=Join-Path (Get-KmcLabRoot) ('analysis-cache/c7-cleanup-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($testRoot)
function Fixture([string]$Scenario,[string]$Family){
 $script:q=[pscustomobject]@{runId='synthetic-cleanup';scenario=$Scenario;branch='codex/test';commit='1'*40;productVersion='synthetic';dllSha256='2'*64;dllMvid='33333333-3333-4333-8333-333333333333';evidenceRoot=(Join-Path $testRoot ([Guid]::NewGuid().ToString('N')))}
 [void][IO.Directory]::CreateDirectory($q.evidenceRoot);$script:kind='chunk7-'+$Family+'-cleanup-pending';$script:leaf=$kind+'.json'
 $obs=[pscustomobject]@{cleanupPendingAtHostBoundary=$true}
 if($Family-ceq'combat'){$obs=[pscustomobject]@{cleanupPending=[pscustomobject]@{reason='native process retained';serializationBusy=$false;loading=$false;queuedLoads=@();pendingNativeWrites=$false;preparationPending=$true;relationshipTransitionPending=$false;settled=$false}}}
 $script:a=[pscustomobject]@{schemaVersion=1;evidenceKind=$kind;runId=$q.runId;scenario=$q.scenario;branch=$q.branch;commit=$q.commit;productVersion=$q.productVersion;dllSha256=$q.dllSha256;dllMvid=$q.dllMvid;createdAtUtc='2026-10-12T00:00:00Z';status='FAIL';observations=$obs;errors=@('native cleanup retains exact custody')}
}
function Publish {
 $path=Join-Path $q.evidenceRoot $leaf;$a|ConvertTo-Json -Depth 30|Set-Content -LiteralPath $path
 $script:m=[pscustomobject]@{artifacts=@([pscustomobject]@{relativePath=$leaf;kind=$kind;length=(Get-Item -LiteralPath $path).Length;sha256=(Get-KmcSha256 $path)})}
}
foreach($pair in @(@('chunk7-two-pair-exploration','world'),@('chunk7-persistence-save','persistence'),@('chunk7-persistence-load','persistence'),@('chunk7-two-pair-combat-rider-first','combat'),@('chunk7-two-pair-combat-mount-first','combat'),@('chunk7-two-pair-realtime-core','combat'),@('chunk7-two-pair-realtime-casting','combat'),@('chunk7-two-pair-realtime-charge','combat'),@('chunk7-two-pair-native-life-cleanup','combat'))){
 Fixture $pair[0] $pair[1];Publish;Check ('exact failed pending '+$pair[0]) {Assert-KmcChunk7CleanupArtifacts $q $m 'FAIL'};Reject ('pending cannot become PASS '+$pair[0]) {Assert-KmcChunk7CleanupArtifacts $q $m 'PASS'}
}
foreach($field in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')){Fixture 'chunk7-persistence-save' 'persistence';$a.$field='foreign';Publish;Reject ('candidate identity '+$field){Assert-KmcChunk7CleanupArtifacts $q $m 'FAIL'}}
foreach($mutation in @('schema-text','pass-artifact','empty-errors','blank-error','numeric-error','false-retained','text-retained','missing-observation','foreign-family','wrong-kind','wrong-leaf','duplicate-role','hash-substitution','length-substitution','unknown-member')){Fixture 'chunk7-persistence-save' 'persistence';
 switch($mutation){'schema-text'{$a.schemaVersion='1'};'pass-artifact'{$a.status='PASS';$a.errors=@()};'empty-errors'{$a.errors=@()};'blank-error'{$a.errors=@(' ')};'numeric-error'{$a.errors=@(4)};'false-retained'{$a.observations.cleanupPendingAtHostBoundary=$false};'text-retained'{$a.observations.cleanupPendingAtHostBoundary='true'};'missing-observation'{$a.observations=$null};'foreign-family'{$q.scenario='chunk7-two-pair-exploration';$a.scenario=$q.scenario};'unknown-member'{$a|Add-Member NoteProperty extra 1}}
 Publish;switch($mutation){'wrong-kind'{$m.artifacts[0].kind='chunk7-world-cleanup-pending'};'wrong-leaf'{$m.artifacts[0].relativePath='chunk7-world-cleanup-pending.json'};'duplicate-role'{$m.artifacts+= $m.artifacts[0]};'hash-substitution'{$m.artifacts[0].sha256='f'*64};'length-substitution'{$m.artifacts[0].length++}}
 Reject $mutation {Assert-KmcChunk7CleanupArtifacts $q $m 'FAIL'}
}
foreach($mutation in @('settled','numeric-boundary','missing-reason','queue-text','blank-queue')){Fixture 'chunk7-two-pair-native-life-cleanup' 'combat';switch($mutation){'settled'{$a.observations.cleanupPending.settled=$true};'numeric-boundary'{$a.observations.cleanupPending.preparationPending=1};'missing-reason'{$a.observations.cleanupPending.reason=''};'queue-text'{$a.observations.cleanupPending.queuedLoads='queued'};'blank-queue'{$a.observations.cleanupPending.queuedLoads=@('')}};Publish;Reject ('combat '+$mutation){Assert-KmcChunk7CleanupArtifacts $q $m 'FAIL'}}
Fixture 'persistence-p01' 'persistence';Publish;Reject 'historical scenario cannot claim new pending evidence'{Assert-KmcChunk7CleanupArtifacts $q $m 'FAIL'}
Check 'optional pending artifact absent on ordinary PASS'{Assert-KmcChunk7CleanupArtifacts $q ([pscustomobject]@{artifacts=@()}) 'PASS'}
Write-Host ('CHUNK7 CLEANUP EVIDENCE PASS='+$passed+' FAIL=0; actual private manifest/hash/envelope fixtures='+$testRoot)
