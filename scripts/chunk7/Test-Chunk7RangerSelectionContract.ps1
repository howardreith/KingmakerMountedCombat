[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('SourcePath')){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/RangerSelectionContractTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetFullPath((Join-Path $RepositoryRoot '../..'))) ('analysis-cache/chunk7-continuation/ranger-contract-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=[IO.File]::ReadAllText($SourcePath)
$signature='private static BlueprintFeature[] RequireExactRangerOptions'
$start=$source.IndexOf($signature,[StringComparison]::Ordinal);if($start-lt0){throw 'Actual Ranger guard missing.'}
$open=$source.IndexOf('{',$start);$end=$open+1;$depth=1
while($depth-and$end-lt$source.Length){if($source[$end]-ceq'{'){$depth++}elseif($source[$end]-ceq'}'){$depth--};$end++}
if($depth){throw 'Unbalanced actual Ranger guard.'}
$body=[IO.File]::ReadAllText($TemplatePath).Replace('/*GUARD*/',$source.Substring($start,$end-$start))
$generated=Join-Path $OutputDirectory 'actual.cs';[IO.File]::WriteAllText($generated,$body,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$executable=Join-Path $OutputDirectory 'actual.exe'
&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$executable" $generated
if($LASTEXITCODE-ne0){throw 'Actual Ranger selection guard compile failed.'}
&$executable
$exitCode=$LASTEXITCODE
@{source=$SourcePath;sha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();exitCode=$exitCode;nativeProof=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($exitCode-ne0){throw 'Actual Ranger selection guard regression failed.'}
