[CmdletBinding()]
param([string]$RepositoryRoot,[string]$SourceOverlayRoot,[string]$TestSource,[switch]$IncludeOrdinaryNeighbors)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
if(-not $PSBoundParameters.ContainsKey('RepositoryRoot')){$RepositoryRoot=Join-Path $PSScriptRoot '../..'}
$RepositoryRoot=[IO.Path]::GetFullPath($RepositoryRoot)
if(-not $PSBoundParameters.ContainsKey('TestSource')){$TestSource=Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/Chunk7RepeatedLoadAuthorityTests.cs'}
$TestSource=[IO.Path]::GetFullPath($TestSource)
. (Join-Path $RepositoryRoot 'scripts/runtime/RuntimeHarness.Common.ps1')
$output=Join-Path (Get-KmcLabRoot) ('analysis-cache/chunk7-continuation/hardening-next/authority/check-'+[Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($output)
[xml]$project=Get-Content -LiteralPath (Join-Path $RepositoryRoot 'tests/KingmakerMountedCombat.Tests.csproj') -Raw
$sources=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($item in $project.SelectNodes("//*[local-name()='Compile']")){
    $path=[IO.Path]::GetFullPath((Join-Path (Join-Path $RepositoryRoot 'tests') $item.Include))
    if($SourceOverlayRoot -and (Split-Path -Leaf $path) -ceq 'PersistenceSaveAuthorization.cs'){
        $path=Join-Path $SourceOverlayRoot 'src/KingmakerMountedCombat/Diagnostics/PersistenceSaveAuthorization.cs'
    }
    [void]$sources.Add($path)
}
foreach($leaf in @('Chunk7OrdinaryAcquisitionSelection.cs','Chunk7HostCompletionGate.cs')){
    [void]$sources.Add((Join-Path $RepositoryRoot ('src/KingmakerMountedCombat/Diagnostics/'+$leaf)))
}
[void]$sources.Add((Join-Path $RepositoryRoot 'tests/Chunk7Diagnostics/Chunk7OrdinaryAuthorityTests.cs'))
[void]$sources.Add($TestSource)
$framework='C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7'
$compiler='C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'
$selectedMains=@('Chunk7RepeatedLoadAuthorityTests')
if($IncludeOrdinaryNeighbors){$selectedMains+='Chunk7OrdinaryAuthorityTests'}
foreach($main in $selectedMains){
    $exe=Join-Path $output ($main+'.exe')
    $rsp=Join-Path $output ($main+'.rsp')
    $compilerArguments=@('/nologo','/target:exe','/langversion:7.3','/deterministic+','/optimize+','/warnaserror+','/nostdlib+',('/main:KingmakerMountedCombat.Tests.'+$main),('/out:'+$exe))
    $compilerArguments+=@('mscorlib.dll','System.dll','System.Core.dll')|ForEach-Object{'/reference:'+(Join-Path $framework $_)}
    [IO.File]::WriteAllLines($rsp,@(($compilerArguments+@($sources))|ForEach-Object{'"'+$_+'"'}),[Text.UTF8Encoding]::new($false))
    & $compiler ('@'+$rsp)
    if($LASTEXITCODE-ne0){throw ('Focused authority compilation failed: '+$main)}
    & $exe $output
    if($LASTEXITCODE-ne0){throw ('Focused authority behavior failed: '+$main)}
}
$authority=@($sources|Where-Object{(Split-Path -Leaf $_)-ceq'PersistenceSaveAuthorization.cs'})[0]
[ordered]@{authoritySource=$authority;authoritySha256=(Get-FileHash -LiteralPath $authority -Algorithm SHA256).Hash.ToLowerInvariant();testSource=$TestSource;testSha256=(Get-FileHash -LiteralPath $TestSource -Algorithm SHA256).Hash.ToLowerInvariant();selected=$selectedMains;nativeOperations=0}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $output 'authority-source-receipt.json')
Write-Host ('Private authority receipt root: '+$output)
