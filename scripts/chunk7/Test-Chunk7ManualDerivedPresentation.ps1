[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TemplatePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TemplatePath){$TemplatePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ManualDerivedPresentationTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-derived-presentation-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve existing focused output.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Member([string]$Text,[string]$Signature){$a=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($a-lt0-or$Text.IndexOf($Signature,$a+$Signature.Length,[StringComparison]::Ordinal)-ge0){throw 'Missing/ambiguous actual member.'};$open=$Text.IndexOf('{',$a);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unbalanced actual member.'};$Text.Substring($a,$end-$a)}
$sourcePath=Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedScenario.Build.cs';$source=[IO.File]::ReadAllText($sourcePath);$template=[IO.File]::ReadAllText($TemplatePath)
$actual=$template.Replace('@@CLEANUP_GUARD@@',(Member $source '        private void AssertCleanupInputAuthority()')).Replace('@@PRESENTATION@@',(Member $source '        private void RestoreAcquisitionPresentation()'))
$path=Join-Path $OutputDirectory 'Actual.cs';[IO.File]::WriteAllText($path,$actual,[Text.UTF8Encoding]::new($false));[IO.File]::WriteAllText((Join-Path $OutputDirectory 'Source.cs'),$source,[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'presentation.exe';$clock=[Diagnostics.Stopwatch]::StartNew()
&$compiler /nologo /target:exe /langversion:7.3 /nowarn:0649 "/out:$exe" $path;if($LASTEXITCODE-ne0){throw 'Actual presentation adapter compilation failed.'}
$output=&$exe;$code=$LASTEXITCODE;$output|Tee-Object -FilePath (Join-Path $OutputDirectory 'test.log')|Write-Host
@{schemaVersion=1;exit=$code;seconds=$clock.Elapsed.TotalSeconds;sourceSha256=(Get-FileHash -LiteralPath $sourcePath).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TemplatePath).Hash.ToLowerInvariant();externalBoundariesSubstituted=$true;nativeExecution=$false;archiveAccess=$false}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual presentation regression failed.'}
