[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$SourcePath){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7SmallCalibrationActions.cs'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallPrimaryAdapterTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-primary-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=[IO.File]::ReadAllText($SourcePath);$signature='        private bool FinishPrimary(UnitEntityData executor)'
$start=$source.IndexOf($signature,[StringComparison]::Ordinal);if($start-lt0){throw 'Actual primary method missing.'};$open=$source.IndexOf('{',$start);$end=$open+1;$depth=1
while($depth-and$end-lt$source.Length){if($source[$end]-ceq'{'){$depth++}elseif($source[$end]-ceq'}'){$depth--};$end++}
if($depth){throw 'Unbalanced actual primary method.'}
$body=[IO.File]::ReadAllText($TemplatePath).Replace('/*ACTUAL*/',$source.Substring($start,$end-$start));$generated=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($generated,$body)
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$dll=Join-Path $OutputDirectory 'actual.dll'
&$compiler /nologo /target:library /langversion:7.3 /nowarn:0649 "/reference:$json" "/out:$dll" $generated
if($LASTEXITCODE-ne0){throw 'Actual primary adapter compile failed.'};[void][Reflection.Assembly]::LoadFrom($json);[void][Reflection.Assembly]::LoadFrom($dll)
$failures=[ActualSmallPrimary]::Verify()
@{source=$SourcePath;sha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();failureCount=$failures;nativeProof=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json')
if($failures-ne0){throw 'Actual primary behavior regression failed.'}
