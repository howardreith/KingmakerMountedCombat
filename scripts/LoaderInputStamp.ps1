# The receipt a build writes next to its outputs and a run that reuses them (-SkipBuild) must find unchanged: the inputs
# that decide what was compiled (the loader pins, their verifier, the MSBuild guards, every project file, every compiled
# source under src/, tools/KmcRemovalObserver/ and tests/, the cached loader bytes) and the bytes of the outputs it
# produced (both shipping DLLs and the component runner). Reuse is refused when the receipt is missing or any of them
# differs, so a replaced or removed output, a replaced runner or a changed source cannot be reused.
Set-StrictMode -Version Latest
function Get-KmcBuildOutputPaths([string]$RepositoryRoot, [string]$Configuration) {
    return @(
        (Join-Path $RepositoryRoot "bin\$Configuration\KingmakerMountedCombat.dll"),
        (Join-Path $RepositoryRoot "bin\Observer\$Configuration\KmcRemovalObserver.dll"),
        (Join-Path $RepositoryRoot "bin\Tests\$Configuration\KingmakerMountedCombat.Tests.exe"))
}
function Get-KmcLoaderInputStamp([string]$RepositoryRoot, [string]$Configuration = 'Release') {
    $root = [IO.Path]::GetFullPath($RepositoryRoot)
    $sources = @(& git -C $root ls-files --cached --others --exclude-standard -- src tools/KmcRemovalObserver tests)
    $inputs = @('scripts/loader-reference-pins.json', 'scripts/LoaderPins.ps1', 'scripts/Get-PinnedLoaderReferences.ps1', 'LoaderReferences.targets',
        'LoaderReferences.Final.targets', 'KingmakerMountedCombat.sln') + @(& git -C $root ls-files -- '*.csproj' '*.props' '*.targets' | Where-Object { $_ -notlike 'docs/*' }) + $sources
    $lines = foreach ($relative in @($inputs | Sort-Object -Unique)) {
        $full = Join-Path $root $relative
        if (Test-Path -LiteralPath $full -PathType Leaf) { $relative + "`t" + (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() } else { $relative + "`tabsent" }
    }
    foreach ($name in @('UnityModManager.dll', '0Harmony12.dll')) {
        $cached = Join-Path $root "obj\loader-references\$name"
        $lines += 'cache/' + $name + "`t" + $(if (Test-Path -LiteralPath $cached -PathType Leaf) { (Get-FileHash -LiteralPath $cached -Algorithm SHA256).Hash.ToLowerInvariant() } else { 'absent' })
    }
    foreach ($output in (Get-KmcBuildOutputPaths $root $Configuration)) {
        $lines += 'output/' + $output.Substring($root.Length + 1) + "`t" + $(if (Test-Path -LiteralPath $output -PathType Leaf) { (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant() } else { 'absent' })
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-KmcLoaderInputStampPath([string]$RepositoryRoot, [string]$Configuration) { return (Join-Path $RepositoryRoot "bin\$Configuration\.loader-input-stamp") }
function Write-KmcLoaderInputStamp([string]$RepositoryRoot, [string]$Configuration) {
    foreach ($output in (Get-KmcBuildOutputPaths $RepositoryRoot $Configuration)) {
        if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw "The build did not produce $output; no reuse receipt is written." }
    }
    $path = Get-KmcLoaderInputStampPath $RepositoryRoot $Configuration
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    [IO.File]::WriteAllText($path, (Get-KmcLoaderInputStamp $RepositoryRoot $Configuration))
}
function Test-KmcLoaderInputStamp([string]$RepositoryRoot, [string]$Configuration) {
    $path = Get-KmcLoaderInputStampPath $RepositoryRoot $Configuration
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return 'missing' }
    if ([IO.File]::ReadAllText($path).Trim() -cne (Get-KmcLoaderInputStamp $RepositoryRoot $Configuration)) { return 'changed' }
    return 'current'
}
function Assert-KmcLoaderInputStamp([string]$RepositoryRoot, [string]$Configuration) {
    switch (Test-KmcLoaderInputStamp $RepositoryRoot $Configuration) {
        'missing' { throw '-SkipBuild cannot reuse these outputs: no build receipt was recorded by the build that produced them. Build again.' }
        'changed' { throw '-SkipBuild cannot reuse these outputs: the loader pins, verifier, MSBuild guards, project files, compiled sources, cached loader bytes or the outputs themselves changed since they were built. Build again.' }
    }
}
