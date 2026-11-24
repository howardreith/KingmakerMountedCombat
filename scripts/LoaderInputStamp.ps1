# Identity of the inputs that decide which loader files the compiled outputs were built against: the pins, their
# verifier, the MSBuild guard, every project file of the solution and the verified cache bytes. Written next to the
# outputs by each build; a run that reuses outputs (-SkipBuild) must find the same identity.
Set-StrictMode -Version Latest
function Get-KmcLoaderInputStamp([string]$RepositoryRoot) {
    $root = [IO.Path]::GetFullPath($RepositoryRoot)
    $paths = @('scripts/loader-reference-pins.json', 'scripts/Get-PinnedLoaderReferences.ps1', 'LoaderReferences.targets', 'KingmakerMountedCombat.sln') +
        @(& git -C $root ls-files -- '*.csproj' '*.props' '*.targets' | Where-Object { $_ -notlike 'docs/*' })
    $lines = foreach ($relative in @($paths | Sort-Object -Unique)) {
        $full = Join-Path $root $relative
        if (Test-Path -LiteralPath $full -PathType Leaf) { $relative + "`t" + (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant() } else { $relative + "`tabsent" }
    }
    foreach ($name in @('UnityModManager.dll', '0Harmony12.dll')) {
        $cached = Join-Path $root "obj\loader-references\$name"
        $lines += 'cache/' + $name + "`t" + $(if (Test-Path -LiteralPath $cached -PathType Leaf) { (Get-FileHash -LiteralPath $cached -Algorithm SHA256).Hash.ToLowerInvariant() } else { 'absent' })
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant() } finally { $sha.Dispose() }
}
function Get-KmcLoaderInputStampPath([string]$RepositoryRoot, [string]$Configuration) { return (Join-Path $RepositoryRoot "bin\$Configuration\.loader-input-stamp") }
function Write-KmcLoaderInputStamp([string]$RepositoryRoot, [string]$Configuration) {
    $path = Get-KmcLoaderInputStampPath $RepositoryRoot $Configuration
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    [IO.File]::WriteAllText($path, (Get-KmcLoaderInputStamp $RepositoryRoot))
}
function Assert-KmcLoaderInputStamp([string]$RepositoryRoot, [string]$Configuration) {
    $path = Get-KmcLoaderInputStampPath $RepositoryRoot $Configuration
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw '-SkipBuild cannot reuse these outputs: no loader-input identity was recorded by the build that produced them. Build again.' }
    if ([IO.File]::ReadAllText($path).Trim() -cne (Get-KmcLoaderInputStamp $RepositoryRoot)) { throw '-SkipBuild cannot reuse these outputs: the loader pins, verifier, MSBuild guard, project files or cached loader bytes changed since they were built. Build again.' }
}
