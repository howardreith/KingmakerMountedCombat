$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'));$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'))
. (Join-Path $repo 'scripts/runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aRegressionEvidence.ps1')
foreach($case in @(@('CM08-ordinary-attack-controls-tb','final105-p08-ordinary-tb'),@('CM08-chunk4-sustained-tb','final105-p08-sustained-tb'))){
 $role=@(Get-KmcChunk6aRegressionRoles $case[0])[0]
 $b=Get-KmcChunk6aRegressionBinding $role.name $case[1] $role.rows $lab
 $r=Get-KmcBoundJson (Join-Path $lab ('runtime-evidence/'+$case[1]+'/runtime-request.json')) $b.requestSha256
 $payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256}
 Assert-KmcChunk6aRegressionBindings $case[0] $b @($b)
 Assert-KmcChunk6aRegressionRun $payload $b $lab
 'PASS archived105 exact regression reader '+$case[0]+'; no current qualification'
}