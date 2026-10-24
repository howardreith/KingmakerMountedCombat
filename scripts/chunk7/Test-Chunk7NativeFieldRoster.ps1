[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestTemplate,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestTemplate){$TestTemplate=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeFieldRosterTestTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/field-roster-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve existing roster evidence.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$source=Join-Path $DiagnosticSourceDirectory 'Chunk7AcquiredManualScenario.Field.cs';$helper=Join-Path $DiagnosticSourceDirectory 'Chunk7NativeFieldRoster.cs'
$raw=[IO.File]::ReadAllText($source);$start=$raw.IndexOf('            RequireAcquiredOwner(); var current =',[StringComparison]::Ordinal);$end=$raw.IndexOf('            actors = current;',$start,[StringComparison]::Ordinal)
if($start-lt0-or$end-le$start){throw 'Actual field predicate boundary not found.'};$predicate=$raw.Substring($start,$end-$start)
$template=[IO.File]::ReadAllText($TestTemplate);if(($template.Split(@('__ACTUAL_FIELD_PREDICATE__'),[StringSplitOptions]::None)).Length-ne2){throw 'Template has ambiguous method marker.'}
$generated=Join-Path $OutputDirectory 'ActualFieldRosterTests.cs';[IO.File]::WriteAllText($generated,$template.Replace('__ACTUAL_FIELD_PREDICATE__',$predicate),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'roster-tests.exe';$timer=[Diagnostics.Stopwatch]::StartNew()
&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ ('/out:'+$exe) $helper $generated;if($LASTEXITCODE-ne0){throw 'Actual roster predicate/helper compilation failed.'}
&$exe 2>&1|Tee-Object -FilePath (Join-Path $OutputDirectory 'test.log')|Write-Host;$code=$LASTEXITCODE;$timer.Stop()
@{schemaVersion=1;exitCode=$code;seconds=$timer.Elapsed.TotalSeconds;sourceSha256=(Get-FileHash -LiteralPath $source).Hash.ToLowerInvariant();helperSha256=(Get-FileHash -LiteralPath $helper).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TestTemplate).Hash.ToLowerInvariant();generatedSha256=(Get-FileHash -LiteralPath $generated).Hash.ToLowerInvariant();nativeExecution=$false;archiveAccess=$false;substitutes='external native Player/actor containers only; actual helper and actual field predicate compiled'}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Actual field predicate/helper tests failed.'}
