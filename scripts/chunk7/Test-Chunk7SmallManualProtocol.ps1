[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$FixturePath,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$RepositoryRoot){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$DiagnosticSourceDirectory){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$TestSource){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualProtocolTests.cs'}
if(-not$FixturePath){$FixturePath=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/SmallManualFixture.json'}
if(-not$OutputDirectory){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/small-manual-protocol-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Extract([string]$text,[string]$signature){$i=$text.IndexOf($signature,[StringComparison]::Ordinal);if($i-lt0){throw ('Missing exact method '+$signature)};$start=$text.IndexOf('{',$i);$depth=1;$end=$start+1;while($depth-gt0-and$end-lt$text.Length){if($text[$end]-ceq'{'){$depth++}elseif($text[$end]-ceq'}'){$depth--};$end++};if($depth){throw 'Unbalanced actual method'};$text.Substring($i,$end-$i)}
$authorityPath=Join-Path $DiagnosticSourceDirectory 'PersistenceSaveAuthorization.cs';$text=[IO.File]::ReadAllText($authorityPath)
# Only external filesystem calls are substituted. Actual authority/type/quota predicates execute unchanged.
$text=$text.Replace((Extract $text '        private static void RequireDirectory(string path)'),'        private static void RequireDirectory(string path) { KingmakerMountedCombat.Tests.SmallManualVirtual.RequireDirectory(path); }').Replace((Extract $text '        private static string ReadOwnedHash(string path)'),'        private static string ReadOwnedHash(string path) { return KingmakerMountedCombat.Tests.SmallManualVirtual.Hash(path); }')
$text='using Directory = KingmakerMountedCombat.Tests.SmallManualVirtual;'+[Environment]::NewLine+'using File = KingmakerMountedCombat.Tests.SmallManualVirtualFile;'+[Environment]::NewLine+$text
$actual=Join-Path $OutputDirectory 'ActualAuthority.cs';[IO.File]::WriteAllText($actual,$text,[Text.UTF8Encoding]::new($false))
$manualPath=Join-Path $DiagnosticSourceDirectory 'Chunk7SmallCalibrationScenario.Manual.cs';$manual=[IO.File]::ReadAllText($manualPath)
$method=Extract $manual '        internal static JObject RequireManualArtifact(RuntimeRequest request, JObject artifact)'
$method+=[Environment]::NewLine+(Extract $manual '        internal static void RequireAssistedSmallManual(JObject o, Chunk7SmallManualSourceSelection plan)')
$admission='using System; using System.Linq; using Newtonsoft.Json.Linq; namespace KingmakerMountedCombat.Diagnostics { internal sealed partial class Chunk7SmallCalibrationScenario { private static void Require(bool value,string reason) { if(!value)throw new InvalidOperationException(reason); }'+$method+'} internal static class Chunk7AcquiredManualScenario { internal const string EvidenceKind="chunk7-acquired-manual-evidence"; } }'
$admissionPath=Join-Path $OutputDirectory 'ActualSourceAdmission.cs';[IO.File]::WriteAllText($admissionPath,$admission,[Text.UTF8Encoding]::new($false))
[xml]$project=[IO.File]::ReadAllText((Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj'))
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($node in $project.SelectNodes("//*[local-name()='Compile']")){$p=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $node.Include));$leaf=Split-Path -Leaf $p;$candidate=Join-Path $DiagnosticSourceDirectory $leaf;if($leaf-ceq'PersistenceSaveAuthorization.cs'){$p=$actual}elseif(Test-Path -LiteralPath $candidate){$p=$candidate};[void]$sources.Add($p)}
foreach($leaf in @('Chunk7EvidenceDto.cs')){[void]$sources.Add((Join-Path $DiagnosticSourceDirectory $leaf))};[void]$sources.Add($admissionPath);[void]$sources.Add($TestSource)
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$json='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed/Newtonsoft.Json.dll'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe';$exe=Join-Path $OutputDirectory 'small-manual-protocol.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$args=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+','/main:KingmakerMountedCombat.Tests.SmallManualProtocolTests',('/out:'+$exe),('/reference:'+$json));$args+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($args+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Small Manual protocol/actual-admission compile failed.'}
&$exe $FixturePath $json;if($LASTEXITCODE-ne0){throw 'Small Manual actual-admission checks failed.'}
@{authoritySha256=(Get-FileHash -LiteralPath $authorityPath).Hash.ToLowerInvariant();manualSourceSha256=(Get-FileHash -LiteralPath $manualPath).Hash.ToLowerInvariant();filesystemSubstituted=$true;archiveReads=0;nativeInvoked=$false;compiledCatalogueUnchanged=$true}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'boundary-receipt.json') -Encoding UTF8
