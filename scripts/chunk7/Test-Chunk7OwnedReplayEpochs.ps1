[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/OwnedReplayEpochTemplate.cs'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-owned-replay-epochs-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract-OwnedRelease([string]$Text){$a=$Text.IndexOf('internal void Release()',[StringComparison]::Ordinal);if($a-lt0){throw 'Actual inherited Release is missing.'};$o=$Text.IndexOf('{',$a);$d=1;$i=$o+1;while($d-gt0-and$i-lt$Text.Length){if($Text[$i]-ceq'{'){$d++}elseif($Text[$i]-ceq'}'){$d--};$i++};if($d-ne0){throw 'Unbalanced actual inherited Release.'};$Text.Substring($a,$i-$a)}
$partial=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairFixture.Replay.cs';$capital=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairFixture.Capital.cs';$owner=Join-Path $DiagnosticSourceDirectory 'Chunk7OwnedPairFixture.cs';$identity=Join-Path $DiagnosticSourceDirectory 'RetainedObjectIdentity.cs'
if(-not(Test-Path -LiteralPath $identity)){$identity=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics/RetainedObjectIdentity.cs'}
$release=Join-Path $OutputDirectory 'ActualRelease.cs';[IO.File]::WriteAllText($release,('namespace KingmakerMountedCombat.Diagnostics { internal sealed partial class Chunk7OwnedPairFixture {'+(Extract-OwnedRelease ([IO.File]::ReadAllText($owner)))+'}}'),[Text.UTF8Encoding]::new($false))
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll';[IO.File]::Copy($json,(Join-Path $OutputDirectory 'Newtonsoft.Json.dll'),$false)
$exe=Join-Path $OutputDirectory 'owned-replay-epochs.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp';$compilerArguments=@('/nologo','/target:exe','/langversion:7.3','/nowarn:0649','/nostdlib+','/main:KingmakerMountedCombat.Diagnostics.Chunk7OwnedPairFixture',('/out:'+$exe),('/reference:'+$json))
$compilerArguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)};$compilerArguments+=@($partial,$capital,$identity,$TestSource,$release)
[IO.File]::WriteAllLines($rsp,@($compilerArguments|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
$clock=[Diagnostics.Stopwatch]::StartNew();&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Owned replay actual epoch compile failed.'};&$exe;if($LASTEXITCODE-ne0){throw 'Owned replay actual epoch regression failed.'};$clock.Stop()
[ordered]@{status='PASS';durationSeconds=$clock.Elapsed.TotalSeconds;partialSha256=(Get-FileHash -LiteralPath $partial).Hash.ToLowerInvariant();ownerSha256=(Get-FileHash -LiteralPath $owner).Hash.ToLowerInvariant();identitySha256=(Get-FileHash -LiteralPath $identity).Hash.ToLowerInvariant();templateSha256=(Get-FileHash -LiteralPath $TestSource).Hash.ToLowerInvariant();nativeFacadesSubstituted=$true;fullBindAndScopeProof=$false;nativeInvoked=$false;archivesRead=0}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
