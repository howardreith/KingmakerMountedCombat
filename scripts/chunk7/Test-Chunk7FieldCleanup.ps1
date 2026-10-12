[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('SourcePath')){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryFieldScenario.cs'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/FieldCleanupTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/field-cleanup-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=[IO.File]::ReadAllText($SourcePath)
function Exact([string]$signature){$start=$source.IndexOf($signature,[StringComparison]::Ordinal);if($start-lt0){throw 'Actual field method missing.'};$open=$source.IndexOf('{',$start);$end=$open+1;$depth=1;while($depth-and$end-lt$source.Length){if($source[$end]-ceq'{'){$depth++}elseif($source[$end]-ceq'}'){$depth--};$end++};if($depth){throw 'Unbalanced actual field method.'};$source.Substring($start,$end-$start)}
$body=[IO.File]::ReadAllText($TemplatePath).Replace('/*POLL*/',(Exact '        private void PollCleanup()')).Replace('/*RETAIN*/',(Exact '        private void Retain(string reason)'))
$generated=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($generated,$body,[Text.UTF8Encoding]::new($false))
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$dll=Join-Path $OutputDirectory 'actual.dll'
&$compiler /nologo /target:library /langversion:7.3 /nowarn:0649 "/reference:$json" "/out:$dll" $generated
if($LASTEXITCODE-ne0){throw 'Actual field cleanup adapter compile failed.'}
[void][Reflection.Assembly]::LoadFrom($json);[void][Reflection.Assembly]::LoadFrom($dll)
$failures=[ActualFieldCleanup]::Verify()
@{source=$SourcePath;sha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();failureCount=$failures;nativeProof=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($failures-ne0){throw 'Actual field cleanup boundary regression failed.'}
