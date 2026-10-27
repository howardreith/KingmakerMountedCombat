[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ManualDerivedAdmissionTests.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/manual-derived-admission-'+[Guid]::NewGuid().ToString('N'))}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve existing focused output.'};[void][IO.Directory]::CreateDirectory($OutputDirectory)
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'admission.exe'
$ordinarySelection=Join-Path $DiagnosticSourceDirectory 'Chunk7OrdinaryAcquisitionSelection.cs'
if(-not(Test-Path -LiteralPath $ordinarySelection)){$ordinarySelection=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/Chunk7OrdinaryAcquisitionSelection.cs'}
$inputs=@((Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedSelection.cs'),(Join-Path $DiagnosticSourceDirectory 'Chunk7ManualDerivedAdmission.cs'),$ordinarySelection,$TestSource)
$clock=[Diagnostics.Stopwatch]::StartNew();&$compiler /nologo /target:exe /langversion:7.3 /warnaserror+ "/out:$exe" @inputs;if($LASTEXITCODE-ne0){throw 'Admission compilation failed.'}
$output=&$exe;$code=$LASTEXITCODE;$output|Tee-Object -FilePath (Join-Path $OutputDirectory 'test.log')|Write-Host
@{schemaVersion=1;exit=$code;seconds=$clock.Elapsed.TotalSeconds;inputs=@($inputs|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}});nativeExecution=$false;archiveAccess=$false}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
if($code-ne0){throw 'Admission checks failed.'}
