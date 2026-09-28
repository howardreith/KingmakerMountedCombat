$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aSupportingEvidence.ps1')
. (Join-Path $PSScriptRoot 'runtime/LegacyCombatProjectionEvidence.ps1')
$lab=[IO.Path]::GetFullPath((Join-Path $repo '../..'));$run='final105-p08-mammoth-tb';$root=Join-Path $lab ('runtime-evidence/'+$run)
$b=Get-KmcSupportingBinding mammoth $run @('mounted-mammoth-primary-hit-tb') $lab
$r=Get-Content -Raw -LiteralPath (Join-Path $root 'runtime-request.json')|ConvertFrom-Json
$payload=[pscustomobject]@{version=$r.productVersion;commit=$r.commit;branch=$r.branch;dllSha256=$r.dllSha256;dllMvid=$r.dllMvid;suiteId=$r.qualificationSuite.suiteId;suiteSha256=$r.qualificationSuite.snapshotSha256}
Assert-KmcSupportingRun $payload $b $lab
Write-Host 'Archived105 complete legacy parser PASS; no current qualification.'

$scratch=Join-Path $lab ('analysis-cache/chunk6a-reader-tests/'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch)|Out-Null
