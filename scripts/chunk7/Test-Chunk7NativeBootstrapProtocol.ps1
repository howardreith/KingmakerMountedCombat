[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/NativeBootstrapProtocolTests.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/native-bootstrap-protocol-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
function Get-BootstrapMethod([string]$Text,[string]$Signature){$start=$Text.IndexOf($Signature,[StringComparison]::Ordinal);if($start-lt0){throw ('Missing actual boundary: '+$Signature)};$open=$Text.IndexOf('{',$start);$depth=1;$end=$open+1;while($depth-gt0-and$end-lt$Text.Length){if($Text[$end]-ceq'{'){$depth++}elseif($Text[$end]-ceq'}'){$depth--};$end++};if($depth-ne0){throw 'Unbalanced method.'};$Text.Substring($start,$end-$start)}
$authorityPath=Join-Path $DiagnosticSourceDirectory 'PersistenceSaveAuthorization.cs';$authority=[IO.File]::ReadAllText($authorityPath)
# Actual constructor, admission, quota, window and identity logic is unchanged.
# Only OS file/directory boundaries are replaced; no archive exists or is read.
$directory=Get-BootstrapMethod $authority '        private static void RequireDirectory(string path)'
$hash=Get-BootstrapMethod $authority '        private static string ReadOwnedHash(string path)'
$authority=$authority.Replace($directory,'        private static void RequireDirectory(string path) { KingmakerMountedCombat.Tests.BootstrapVirtualStorage.RequireDirectory(path); }').Replace($hash,'        private static string ReadOwnedHash(string path) { return KingmakerMountedCombat.Tests.BootstrapVirtualStorage.Hash(path); }')
$authority='using Directory = KingmakerMountedCombat.Tests.BootstrapVirtualDirectory;'+[Environment]::NewLine+'using File = KingmakerMountedCombat.Tests.BootstrapVirtualFile;'+[Environment]::NewLine+$authority
$actual=Join-Path $OutputDirectory 'ActualPersistenceSaveAuthorization.cs';[IO.File]::WriteAllText($actual,$authority,[Text.UTF8Encoding]::new($false))
[xml]$project=Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj')
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$overrides=@('RuntimeProtocol.cs','Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs','Chunk7ManualSeedSelection.cs','Chunk7ManualDerivedSelection.cs','Chunk7AcquiredManualSelection.cs')
foreach($node in $project.SelectNodes("//*[local-name()='Compile']")){
 $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $node.Include));$leaf=Split-Path -Leaf $path
 if($leaf-ceq'PersistenceSaveAuthorization.cs'){$path=$actual}elseif($leaf-cin$overrides){$candidate=Join-Path $DiagnosticSourceDirectory $leaf;if(Test-Path -LiteralPath $candidate){$path=$candidate}}
 [void]$sources.Add($path)
}
foreach($leaf in $overrides){$path=Join-Path $DiagnosticSourceDirectory $leaf;if(-not(Test-Path -LiteralPath $path)){$path=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)};[void]$sources.Add($path)}
[void]$sources.Add($TestSource)
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7';$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'native-bootstrap-protocol.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+','/main:KingmakerMountedCombat.Tests.NativeBootstrapProtocolTests',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Native bootstrap actual-authority compile failed.'}
&$exe;if($LASTEXITCODE-ne0){throw 'Native bootstrap actual-authority regression failed.'}
@{authoritySourceSha256=(Get-FileHash -LiteralPath $authorityPath).Hash.ToLowerInvariant();filesystemSubstituted=$true;archivesCreated=0;archivesRead=0}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $OutputDirectory 'boundary-receipt.json') -Encoding UTF8
