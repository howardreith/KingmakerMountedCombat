[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$TemplatePath)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Windows PowerShell -File resolves script-root defaults after parameter binding.
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Join-Path $PSScriptRoot '../..'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not$PSBoundParameters.ContainsKey('SourcePath')){$SourcePath=Join-Path $PSScriptRoot '../../src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionScenario.cs'}
$SourcePath=[IO.Path]::GetFullPath($SourcePath)
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $PSScriptRoot '../../tests/Chunk7Diagnostics/OrdinaryUiLifecycleTemplate.cs'}
$TemplatePath=[IO.Path]::GetFullPath($TemplatePath)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$private=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-continuation/ordinary-ui-'+[Guid]::NewGuid().ToString('N'));[void][IO.Directory]::CreateDirectory($private)
$source=[IO.File]::ReadAllText($SourcePath)
function Exact-Method([string]$Signature){$start=$source.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Actual method unavailable: '+$Signature)};$open=$source.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$source.Length){if($source[$end]-ceq'{'){$depth++}elseif($source[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unbalanced actual method.'};return $source.Substring($start,$end-$start)}
$ready=$source.Contains('        private bool FieldSelectionReady()')
$template=[IO.File]::ReadAllText($TemplatePath)
$body=$template.Replace('/*ACTUAL_OPEN*/',(Exact-Method '        private void OpenFieldSelection()')).Replace('/*ACTUAL_CLEANUP*/',(Exact-Method '        private void PollCleanup()'))
$body=$body.Replace('/*HAS_READINESS*/',$(if($ready){'true'}else{'false'}))
if($ready){
    $active=[regex]::Match($source,'private bool GroupActive\(\) =>[^;]+;').Value;if(-not$active){throw 'Actual activeSelf predicate missing.'}
    $body=$body.Replace('/*ACTUAL_ACTIVE*/',$active).Replace('/*ACTUAL_READY*/',(Exact-Method '        private bool FieldSelectionReady()')).Replace('/*ACTUAL_COMPLETE*/',(Exact-Method '        private void CompleteFieldSelection()'))
    if($source-cnotmatch 'else if \(stage == 2\)\s*\{\s*AssertAuthority\(\);\s*if \(!FieldSelectionReady\(\)\) return;'){throw 'Actual ordinary selector stage can act before animation readiness.'}
}else{$body=$body.Replace('/*ACTUAL_ACTIVE*/','').Replace('/*ACTUAL_READY*/','private bool FieldSelectionReady()=>true;').Replace('/*ACTUAL_COMPLETE*/','private void CompleteFieldSelection(){}')}
$generated=Join-Path $private 'actual.cs';[IO.File]::WriteAllText($generated,$body,[Text.UTF8Encoding]::new($false))
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed';$json=Join-Path $managed 'Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$dll=Join-Path $private 'actual.dll'
& $compiler /nologo /target:library /langversion:7.3 /nowarn:0649,0169,0414,0162 "/reference:$json" "/out:$dll" $generated
if($LASTEXITCODE-ne0){throw 'Ordinary actual UI adapter compilation failed.'}
[void][Reflection.Assembly]::LoadFrom($json);[void][Reflection.Assembly]::LoadFrom($dll)
$result=[OrdinaryUiLifecycleAdapter]::Verify()
[ordered]@{source=$SourcePath;sourceSha256=(Get-FileHash -LiteralPath $SourcePath -Algorithm SHA256).Hash.ToLowerInvariant();nativeReadiness=$ready;failureCount=$result;nativeProof=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $private 'receipt.json') -Encoding UTF8
Write-Host ('Actual UI fixture receipt root: '+$private)
if($result-ne0){throw 'Native selector lifecycle regression failed.'}
