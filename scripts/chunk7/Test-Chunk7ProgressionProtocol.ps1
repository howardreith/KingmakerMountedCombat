[CmdletBinding()]
param([string]$RepositoryRoot,[string]$DiagnosticSourceDirectory,[string]$TestSource,[string]$OutputDirectory)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))}
if(-not$PSBoundParameters.ContainsKey('DiagnosticSourceDirectory')){$DiagnosticSourceDirectory=Join-Path $RepositoryRoot 'src/KingmakerMountedCombat/Diagnostics'}
if(-not$PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/ProgressionProtocolTests.cs'}
if(-not$PSBoundParameters.ContainsKey('OutputDirectory')){$OutputDirectory=Join-Path ([IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName($RepositoryRoot))) ('analysis-cache/chunk7-continuation/progression-protocol-'+[Guid]::NewGuid().ToString('N'))}
[void][IO.Directory]::CreateDirectory($OutputDirectory)
[xml]$project=Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj')
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($node in $project.SelectNodes("//*[local-name()='Compile']")){
    $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $node.Include));$leaf=Split-Path -Leaf $path
    if($leaf-cin@('RuntimeProtocol.cs','PersistenceSaveAuthorization.cs','Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs')){
        $override=Join-Path $DiagnosticSourceDirectory $leaf;if(Test-Path -LiteralPath $override){$path=$override}
    }
    [void]$sources.Add($path)
}
foreach($leaf in @('Chunk7OrdinaryAcquisitionSelection.cs','Chunk7OrdinaryFieldSelection.cs','Chunk7SmallCalibrationSelection.cs','Chunk7ProgressionSelection.cs','Chunk7NativeBootstrapSelection.cs','Chunk7BootstrapInventorySelection.cs')){
    $path=Join-Path $DiagnosticSourceDirectory $leaf
    if(-not(Test-Path -LiteralPath $path)){$path=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)}
    [void]$sources.Add($path)
}
[void]$sources.Add($TestSource)
$framework='C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7'
$compiler='C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe'
$exe=Join-Path $OutputDirectory 'progression-protocol.exe';$rsp=Join-Path $OutputDirectory 'compile.rsp'
$arguments=@('/nologo','/target:exe','/langversion:7.3','/warnaserror+','/optimize+','/deterministic+','/nostdlib+','/main:KingmakerMountedCombat.Tests.ProgressionProtocolTests',('/out:'+$exe))
$arguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($arguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
&$compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Progression pure authority/protocol compile failed.'}
&$exe $OutputDirectory;if($LASTEXITCODE-ne0){throw 'Progression pure authority/protocol regression failed.'}
