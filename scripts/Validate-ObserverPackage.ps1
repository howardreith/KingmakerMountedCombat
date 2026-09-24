[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [switch]$PassThru
)

# The removal observer is a separate minimal UMM mod. Its package must carry
# exactly its own two files, its Info.json must be the observer's own identity,
# and its DLL must reference the installed game/UMM/Harmony12 surface and
# NEVER the KingmakerMountedCombat assembly.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$identityText = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'tools\KmcRemovalObserver\ObserverIdentity.cs')
$identityMatch = [Regex]::Match($identityText, 'internal const string ProductVersion = "([^"]+)";')
if (-not $identityMatch.Success) { throw 'Observer identity source lacks its product version.' }
$expectedVersion = $identityMatch.Groups[1].Value
$sourceInfo = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'tools\KmcRemovalObserver\Info.json') | ConvertFrom-Json
if ([string]$sourceInfo.Version -cne $expectedVersion) { throw 'Observer Info.json version differs from its identity source.' }
$resolvedPackage = [IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $resolvedPackage -PathType Leaf)) { throw "Observer package does not exist: $resolvedPackage" }
if ((Get-Item -LiteralPath $resolvedPackage).Length -gt 1MB) { throw 'Observer package exceeds the 1 MiB safety limit.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackage)
$temporaryRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot ('obj\observer-package-validation\' + [Guid]::NewGuid().ToString('N'))))
$safeTemporaryParent = [IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\observer-package-validation')).TrimEnd('\')
$entryRecords = @()
$checks = 0
try {
    $actual = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    $expected = @('KmcRemovalObserver/Info.json','KmcRemovalObserver/KmcRemovalObserver.dll')
    if ($actual.Count -ne $expected.Count -or @($actual | Sort-Object -Unique).Count -ne $actual.Count -or
        (($actual | Sort-Object) -join "`n") -cne (($expected | Sort-Object) -join "`n")) {
        throw "Observer package allowlist/duplicate mismatch. Actual entries: $($actual -join ', ')"
    }
    $checks++
    foreach ($entry in $archive.Entries) {
        $normalizedEntry = $entry.FullName.Replace('\','/')
        $entryLimit = if ($normalizedEntry -ceq 'KmcRemovalObserver/KmcRemovalObserver.dll') { 1MB } else { 64KB }
        if ($normalizedEntry -match '(^|/)\.\.(/|$)|^/|^[A-Za-z]:' -or $entry.Length -le 0 -or $entry.Length -gt $entryLimit) {
            throw "Observer package entry is unsafe, empty, or oversized: $($entry.FullName)"
        }
        $algorithm = [Security.Cryptography.SHA256]::Create(); $stream = $entry.Open()
        try { $hash = ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $stream.Dispose(); $algorithm.Dispose() }
        $entryRecords += [pscustomobject]@{ path=$normalizedEntry; length=[long]$entry.Length; sha256=$hash }
    }
    $checks++

    $infoEntry = @($archive.Entries | Where-Object { $_.FullName.Replace('\','/') -ceq 'KmcRemovalObserver/Info.json' })[0]
    $reader = New-Object IO.StreamReader($infoEntry.Open(), [Text.Encoding]::UTF8, $true)
    try { $info = $reader.ReadToEnd() | ConvertFrom-Json }
    finally { $reader.Dispose() }
    $requiredInfo = @('Id','DisplayName','Author','Version','ManagerVersion','GameVersion','AssemblyName','EntryMethod','Requirements')
    if ((@($info.PSObject.Properties.Name | Sort-Object) -join "`n") -cne (($requiredInfo | Sort-Object) -join "`n") -or
        [string]$info.Id -cne 'KmcRemovalObserver' -or [string]$info.Version -cne $expectedVersion -or
        [string]$info.ManagerVersion -cne '0.28.2' -or [string]$info.GameVersion -cne '2.1.7' -or
        [string]$info.AssemblyName -cne 'KmcRemovalObserver.dll' -or
        [string]$info.EntryMethod -cne 'KmcRemovalObserver.Main.Load' -or @($info.Requirements).Count -ne 0) {
        throw 'Packaged observer Info.json identity is not exact.'
    }
    $checks++

    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    $dllPath = Join-Path $temporaryRoot 'KmcRemovalObserver.dll'
    $dllEntry = @($archive.Entries | Where-Object { $_.FullName.Replace('\','/') -ceq 'KmcRemovalObserver/KmcRemovalObserver.dll' })[0]
    $dllInput = $dllEntry.Open(); $dllOutput = [IO.File]::Create($dllPath)
    try { $dllInput.CopyTo($dllOutput) }
    finally { $dllOutput.Dispose(); $dllInput.Dispose() }
    $powerShellExe = Join-Path $PSHOME 'powershell.exe'
    $identityJson = & $powerShellExe -NoLogo -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Get-AssemblyIdentity.ps1') -AssemblyPath $dllPath
    if ($LASTEXITCODE -ne 0) { throw 'Isolated observer assembly inspection failed.' }
    $identity = $identityJson | ConvertFrom-Json
    if ([string]$identity.name -cne 'KmcRemovalObserver' -or [string]$identity.version -cne '0.1.0.0') { throw 'Observer DLL assembly identity is not exact.' }
    if ([string]$identity.informationalVersion -cne $expectedVersion) { throw 'Observer DLL product version does not match its identity source.' }
    if ([string]$identity.targetFramework -cne '.NETFramework,Version=v4.7') { throw 'Observer DLL does not target exact .NET Framework 4.7.' }
    $checks += 3
    $allowedReferences = @('mscorlib','System','System.Core','Assembly-CSharp','Assembly-CSharp-firstpass','UnityEngine','UnityEngine.CoreModule','UnityModManager','Newtonsoft.Json','0Harmony12')
    $references = @($identity.references | Sort-Object)
    $unexpected = @($references | Where-Object { $_ -cnotin $allowedReferences })
    if ($unexpected.Count -ne 0 -or $references -contains '0Harmony' -or
        @($references | Where-Object { $_ -match '(?i)KingmakerMountedCombat|Wrath|BuffPlanner|Gunslinger|Tabletop|CallOfTheWild' }).Count -ne 0) {
        throw "Observer DLL dependency allowlist mismatch: $($unexpected -join ', ')"
    }
    $checks++
    $dllRecord = @($entryRecords | Where-Object path -ceq 'KmcRemovalObserver/KmcRemovalObserver.dll')[0]
    $result = [pscustomobject]@{
        schemaVersion=1; packagePath=$resolvedPackage; packageSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $resolvedPackage).Hash.ToLowerInvariant()
        dllSha256=[string]$dllRecord.sha256; dllMvid=[string]$identity.mvid; version=$expectedVersion; entries=@($entryRecords | Sort-Object path); references=$references
    }
}
finally {
    $archive.Dispose()
    if (Test-Path -LiteralPath $temporaryRoot) {
        if (-not $temporaryRoot.StartsWith($safeTemporaryParent + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Observer package-validation cleanup escaped its generated root.' }
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

Write-Host "OBSERVER PACKAGE PASS=$checks FAIL=0"
Write-Host "PACKAGE=$resolvedPackage"
Write-Host "SHA256=$($result.packageSha256)"
if ($PassThru) { return $result }
