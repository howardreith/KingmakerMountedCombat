[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$TestSource)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
# Windows PowerShell -File resolves script-root defaults after parameter binding.
if(-not$PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Join-Path $PSScriptRoot '../..'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not$PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $PSScriptRoot '../../tests/Chunk7Diagnostics/Chunk7OrdinaryAuthorityTests.cs'}
$TestSource=[IO.Path]::GetFullPath($TestSource)
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$output=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-continuation/ordinary-authority-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($output)
[xml]$project=Get-Content -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj') -Raw
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($item in $project.SelectNodes("//*[local-name()='Compile']")){
    $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $item.Include))
    if($SourceOverlayRoot-and(Split-Path -Leaf $path)-cin@('RuntimeProtocol.cs','RuntimeSaveAuthorization.cs','PersistenceSaveAuthorization.cs')){$path=Join-Path $SourceOverlayRoot ('integration-overlays/'+(Split-Path -Leaf $path))}
    [void]$sources.Add($path)
}
foreach($leaf in @('Chunk7OrdinaryAcquisitionSelection.cs','Chunk7HostCompletionGate.cs')){
    $path=Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)
    if($SourceOverlayRoot){$path=Join-Path $SourceOverlayRoot $(if($leaf-ceq'Chunk7HostCompletionGate.cs'){'integration-overlays/'+$leaf}else{$leaf})}
    [void]$sources.Add([IO.Path]::GetFullPath($path))
}
[void]$sources.Add([IO.Path]::GetFullPath($TestSource))
$framework='C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$exe=Join-Path $output 'OrdinaryAuthority.exe';$rsp=Join-Path $output 'compile.rsp'
$compilerArguments=@('/nologo','/target:exe','/langversion:7.3','/deterministic+','/optimize+','/warnaserror+','/nostdlib+','/main:KingmakerMountedCombat.Tests.Chunk7OrdinaryAuthorityTests',('/out:'+$exe))
$compilerArguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
[IO.File]::WriteAllLines($rsp,@(($compilerArguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
& $compiler ('@'+$rsp);if($LASTEXITCODE-ne0){throw 'Focused ordinary authority/protocol compilation failed.'}
& $exe $output;if($LASTEXITCODE-ne0){throw 'Focused ordinary authority/protocol behavior failed.'}
Write-Host ('Private ordinary fixture receipt root: '+$output)
