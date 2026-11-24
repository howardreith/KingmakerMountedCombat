[CmdletBinding()]
param(
    # The commit before the 0.31.1 baseline (default: origin/main), built once to record what the old project compiled against.
    [string]$BaselineCommit = 'origin/main',
    [string]$ReportPath
)

# MSBuild behavior of the pinned loader references on a temporary clone of the committed HEAD (its path contains a
# space; LocalGamePaths.props is copied from this checkout). Each MSBuild runs in its own process. Nothing is deployed,
# launched or written to the installation; this checkout's artifacts are not touched.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$source = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$commit = (& git -C $source rev-parse HEAD).Trim()
$baseline = (& git -C $source rev-parse $BaselineCommit).Trim()
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = [string](@(& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe') | Select-Object -First 1)
if (-not $msbuild) { throw 'MSBuild was not found.' }
[xml]$localPaths = Get-Content -LiteralPath (Join-Path $source 'LocalGamePaths.props') -Raw
$installDir = [string](@($localPaths.Project.PropertyGroup) | Where-Object { $_.PSObject.Properties['KingmakerInstallDir'] } | Select-Object -First 1).KingmakerInstallDir
$installedLoader = Join-Path $installDir 'Kingmaker_Data\Managed\UnityModManager'
$cases = [Collections.Generic.List[object]]::new()
function Add-Case([string]$Name, [bool]$Passed, [string]$Detail) {
    $cases.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    Write-Host ("{0} {1} :: {2}" -f $(if ($Passed) { 'PASS' } else { 'FAIL' }), $Name, $Detail)
}
function Invoke-Native([string]$File, [string[]]$Arguments) {
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $output = (($null | & $File @Arguments 2>&1) | ForEach-Object { "$_" }) -join "`n" } finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}
function Get-LoaderRefs([string]$Dll) {
    $result = Invoke-Native 'powershell.exe' @('-NoProfile', '-Command', "[Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes('$($Dll.Replace("'", "''"))')).GetReferencedAssemblies() | Where-Object { `$_.Name -match '^(UnityModManager|0Harmony12|0Harmony)$' } | Sort-Object Name | ForEach-Object { `$_.Name + ' ' + `$_.Version }")
    return (($result.Output -split "`n") | Where-Object { $_ }) -join ', '
}
function Get-Sha([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }

$temp = Join-Path ([IO.Path]::GetTempPath()) ('kmc-pinned-loader-' + [Guid]::NewGuid().ToString('N'))
$clone = Join-Path $temp 'clone with space'
try {
    & git clone -q --no-hardlinks $source $clone
    if ($LASTEXITCODE -ne 0) { throw 'git clone failed.' }
    Copy-Item -LiteralPath (Join-Path $source 'LocalGamePaths.props') -Destination $clone
    $project = Join-Path $clone 'src\KingmakerMountedCombat\KingmakerMountedCombat.csproj'
    $outDir = Join-Path $temp 'out'
    $dll = Join-Path $outDir 'KingmakerMountedCombat.dll'
    $build = { param([string[]]$Extra) Invoke-Native $msbuild (@($project, '/nologo', '/v:m', '/nr:false', '/t:Rebuild', '/p:Configuration=Release', "/p:OutDir=$outDir\") + $Extra) }

    # Before: the commit preceding the baseline, compiled with its own project defaults (the installed loader).
    & git -C $clone checkout -q --detach $baseline
    $before = & $build @()
    $beforeRefs = if ($before.ExitCode -eq 0 -and (Test-Path -LiteralPath $dll)) { Get-LoaderRefs $dll } else { 'no DLL' }
    $installedUmm = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $installedLoader 'UnityModManager.dll')).Version.ToString()
    Add-Case 'before-baseline-project-compiles-against-installed-loader' ($before.ExitCode -eq 0 -and $beforeRefs -match "UnityModManager $([regex]::Escape($installedUmm))") "baseline $($baseline.Substring(0, 10)): exit $($before.ExitCode); DLL references $beforeRefs (installed UnityModManager $installedUmm)"
    Remove-Item -LiteralPath $outDir -Recurse -Force -ErrorAction SilentlyContinue

    # After: this commit, without the pinned cache: refused before compilation with the acquisition instruction.
    & git -C $clone checkout -q --detach $commit
    $absent = & $build @()
    Add-Case 'direct-msbuild-without-pinned-cache-refused' ($absent.ExitCode -ne 0 -and $absent.Output -match 'Pinned loader reference missing' -and -not (Test-Path -LiteralPath $dll)) "exit $($absent.ExitCode)"

    # The verifier acquires and checks the pins; the product compiles against them.
    $acquire = Invoke-Native 'powershell.exe' @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $clone 'scripts\Get-PinnedLoaderReferences.ps1'))
    $after = & $build @()
    $afterRefs = if (Test-Path -LiteralPath $dll) { Get-LoaderRefs $dll } else { 'no DLL' }
    Add-Case 'direct-msbuild-compiles-against-pinned-loader' ($acquire.ExitCode -eq 0 -and $after.ExitCode -eq 0 -and $afterRefs -ceq '0Harmony12 1.2.0.1, UnityModManager 0.31.1.0') "acquire exit $($acquire.ExitCode); build exit $($after.ExitCode); DLL references $afterRefs"
    $dllSha = if (Test-Path -LiteralPath $dll) { Get-Sha $dll } else { '' }

    # A pinned file with one byte changed in the cache: refused before compilation; the output is not rebuilt.
    $cache = Join-Path $clone 'obj\loader-references'
    $harmony = Join-Path $cache '0Harmony12.dll'
    $original = [IO.File]::ReadAllBytes($harmony)
    $bytes = [byte[]]$original.Clone(); $bytes[$bytes.Length - 1] = $bytes[$bytes.Length - 1] -bxor 0xFF
    [IO.File]::WriteAllBytes($harmony, $bytes)
    $t = & $build @()
    [IO.File]::WriteAllBytes($harmony, $original)
    Add-Case 'direct-msbuild-altered-pinned-file-refused' ($t.ExitCode -ne 0 -and $t.Output -match 'hash mismatch|differs' -and (Get-Sha $dll) -ceq $dllSha) "exit $($t.ExitCode)"

    # The installed loader as UnityModManagerDir changes nothing: the product never reads it for compilation.
    $installedDir = & $build @("/p:UnityModManagerDir=$installedLoader")
    Add-Case 'installed-loader-dir-is-not-a-compile-input' ($installedDir.ExitCode -eq 0 -and (Get-LoaderRefs $dll) -ceq '0Harmony12 1.2.0.1, UnityModManager 0.31.1.0') "exit $($installedDir.ExitCode); DLL references $(Get-LoaderRefs $dll)"

    # A stray HintPath: a Directory.Build.targets replaces the UnityModManager reference with a byte-identical copy of
    # the pinned file in another folder. The cache check passes, so only the resolved-input check can refuse it.
    $strayDir = Join-Path $temp 'stray copy'
    New-Item -ItemType Directory -Path $strayDir -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $cache 'UnityModManager.dll') -Destination $strayDir
    $extraTargets = Join-Path $clone 'src\Directory.Build.targets'
    [IO.File]::WriteAllText($extraTargets, "<Project><ItemGroup><Reference Remove=`"UnityModManager`" /><Reference Include=`"UnityModManager`"><HintPath>$([Security.SecurityElement]::Escape((Join-Path $strayDir 'UnityModManager.dll')))</HintPath><Private>False</Private></Reference></ItemGroup></Project>")
    $dllBeforeStray = Get-Sha $dll
    $x = & $build @()
    Remove-Item -LiteralPath $extraTargets
    Add-Case 'stray-hintpath-to-unitymodmanager-copy-refused' ($x.ExitCode -ne 0 -and $x.Output -match 'Loader compiler references must be the verified files in the pinned directory' -and (Get-Sha $dll) -ceq $dllBeforeStray) ([regex]::Match($x.Output, 'Loader compiler references must be the verified files[^\n]{0,160}').Value)

    $failed = @($cases | Where-Object { -not $_.passed })
    $summary = [ordered]@{
        schema = 'kmc.pinned-loader-msbuild-tests.v1'
        commit = $commit
        baseline_commit = $baseline
        installed_loader_measured = [ordered]@{ unityModManager = $installedUmm; sha256 = Get-Sha (Join-Path $installedLoader 'UnityModManager.dll') }
        product_dll_sha256 = $dllSha
        passed = $cases.Count - $failed.Count
        total = $cases.Count
        cases = @($cases)
    }
    if ($ReportPath) { $summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
    Write-Host ("TOTAL PASS={0} FAIL={1}" -f $summary.passed, $failed.Count)
    if ($failed.Count -gt 0) { exit 1 }
}
finally { Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue }
