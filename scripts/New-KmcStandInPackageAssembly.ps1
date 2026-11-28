[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$OutputDirectory)

# Self-test prerequisite only: builds a stand-in KingmakerMountedCombat.dll that carries exactly the identity the package
# validator reads (assembly name and version, informational product version, target framework and the pinned loader
# reference identities from scripts/loader-reference-pins.json). It contains no product code. Used by
# scripts/Test-NativeReservationGate.ps1 so Test-Harness.ps1 runs its validator cases in an unbuilt checkout; it never
# replaces a real build output.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$version = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'version.json') | ConvertFrom-Json
$pins = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'loader-reference-pins.json') | ConvertFrom-Json
$compiler = Join-Path ([Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) 'csc.exe'
$out = [IO.Path]::GetFullPath($OutputDirectory)
[void][IO.Directory]::CreateDirectory($out)
$stubs = Join-Path $out 'reference-stubs'
[void][IO.Directory]::CreateDirectory($stubs)
$references = @(); $uses = @(); $index = 0
foreach ($pin in @($pins.references)) {
    $index++
    $source = Join-Path $stubs ([string]$pin.Name + '.cs')
    [IO.File]::WriteAllText($source, ('[assembly: System.Reflection.AssemblyVersion("' + [string]$pin.Version + '")]' + "`n" + 'namespace KmcStandInReference' + $index + ' { public static class Marker { public static int Value; } }'))
    $dll = Join-Path $stubs ([string]$pin.File)
    & $compiler -nologo -target:library ('-out:' + $dll) $source | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The stand-in reference stub for $($pin.Name) did not compile." }
    $references += ('-reference:' + $dll); $uses += ('KmcStandInReference' + $index + '.Marker.Value')
}
$product = Join-Path $out 'KingmakerMountedCombat.cs'
[IO.File]::WriteAllText($product, @(
    ('[assembly: System.Reflection.AssemblyVersion("' + [string]$version.assemblyVersion + '")]'),
    ('[assembly: System.Reflection.AssemblyInformationalVersion("' + [string]$version.productVersion + '")]'),
    '[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.7")]',
    ('namespace KingmakerMountedCombat { public static class Main { public static int Load() { return ' + ($(if ($uses.Count) { $uses -join ' + ' } else { '0' })) + '; } } }')
) -join "`n")
$dllPath = Join-Path $out 'KingmakerMountedCombat.dll'
& $compiler -nologo -target:library ('-out:' + $dllPath) @references $product | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The stand-in package assembly did not compile.' }
Write-Output $dllPath
