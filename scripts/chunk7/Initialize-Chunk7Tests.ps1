$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$kmcRepo=$repo
$chunk7TestSource=Join-Path $repo 'tests/Chunk7Diagnostics'
. (Join-Path $repo 'scripts/runtime/RuntimeHarness.Common.ps1')
$chunk7AnalysisRoot=Join-Path (Get-KmcLabRoot) 'analysis-cache/chunk7-continuation'
[void][IO.Directory]::CreateDirectory($chunk7AnalysisRoot)
