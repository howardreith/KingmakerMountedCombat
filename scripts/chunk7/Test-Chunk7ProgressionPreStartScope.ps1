[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourcePath,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('SourcePath')){$SourcePath=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7ProgressionScenario.cs'}
if(-not$PSBoundParameters.ContainsKey('TemplatePath')){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ProgressionPreStartScopeTemplate.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/progression-scope-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=[IO.File]::ReadAllText($SourcePath);$signature='        private void RequireScope()';$start=$source.IndexOf($signature,[StringComparison]::Ordinal)
if($start-lt0-or$source.IndexOf($signature,$start+$signature.Length,[StringComparison]::Ordinal)-ge0){throw 'Exact progression RequireScope method is absent or duplicated.'}
$open=$source.IndexOf('{',$start);$end=$open+1;$depth=1;while($depth-and$end-lt$source.Length){if($source[$end]-ceq'{'){$depth++}elseif($source[$end]-ceq'}'){$depth--};$end++};if($depth){throw 'Unbalanced actual progression scope method.'}
$method=$source.Substring($start,$end-$start);$generated=Join-Path $OutputDirectory 'actual.cs'
[IO.File]::WriteAllText($generated,([IO.File]::ReadAllText($TemplatePath).Replace('/*REQUIRE_SCOPE*/',$method)),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'actual.exe'
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" $generated
if($LASTEXITCODE-ne0){throw 'Actual progression scope adapter compile failed.'}
$timer=[Diagnostics.Stopwatch]::StartNew();&$exe *> (Join-Path $OutputDirectory 'run.log');$exit=$LASTEXITCODE;$timer.Stop();Get-Content -LiteralPath (Join-Path $OutputDirectory 'run.log')
[ordered]@{source=$SourcePath;sourceSha256=(Get-FileHash -LiteralPath $SourcePath).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();exitCode=$exit;seconds=$timer.Elapsed.TotalSeconds;nativeProof=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($exit-ne0){throw 'Actual progression pre-start binding regression failed.'}
