[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [switch]$PassThru
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$resolvedPackage = [IO.Path]::GetFullPath($PackagePath)
if (-not (Test-Path -LiteralPath $resolvedPackage -PathType Leaf)) { throw "Package does not exist: $resolvedPackage" }
if ((Get-Item -LiteralPath $resolvedPackage).Length -gt 5MB) { throw 'Diagnostic package exceeds the 5 MiB safety limit.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($resolvedPackage)
$temporaryRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot ('obj\package-validation\' + [Guid]::NewGuid().ToString('N'))))
$safeTemporaryParent = [IO.Path]::GetFullPath((Join-Path $repoRoot 'obj\package-validation')).TrimEnd('\')
$entryRecords = @()
try {
    $actual = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    $expected = @('KingmakerMountedCombat/Info.json','KingmakerMountedCombat/KingmakerMountedCombat.dll')
    if ($actual.Count -ne $expected.Count -or @($actual | Sort-Object -Unique).Count -ne $actual.Count -or
        (($actual | Sort-Object) -join "`n") -cne (($expected | Sort-Object) -join "`n")) {
        throw "Package allowlist/duplicate mismatch. Actual entries: $($actual -join ', ')"
    }
    foreach ($entry in $archive.Entries) {
        $normalizedEntry = $entry.FullName.Replace('\','/')
        if ($normalizedEntry -match '(^|/)\.\.(/|$)|^/|^[A-Za-z]:' -or $entry.Length -le 0 -or $entry.Length -gt 4MB) {
            throw "Package entry is unsafe, empty, or oversized: $($entry.FullName)"
        }
        $algorithm = [Security.Cryptography.SHA256]::Create(); $stream = $entry.Open()
        try { $hash = ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
        finally { $stream.Dispose(); $algorithm.Dispose() }
        $entryRecords += [pscustomobject]@{ path=$normalizedEntry; length=[long]$entry.Length; sha256=$hash }
    }

    $infoEntry = @($archive.Entries | Where-Object { $_.FullName.Replace('\','/') -ceq 'KingmakerMountedCombat/Info.json' })[0]
    $reader = New-Object IO.StreamReader($infoEntry.Open(), [Text.Encoding]::UTF8, $true)
    try { $info = $reader.ReadToEnd() | ConvertFrom-Json }
    finally { $reader.Dispose() }
    $requiredInfo = @('Id','DisplayName','Author','Version','ManagerVersion','GameVersion','AssemblyName','EntryMethod','Requirements')
    if ((@($info.PSObject.Properties.Name | Sort-Object) -join "`n") -cne (($requiredInfo | Sort-Object) -join "`n") -or
        [string]$info.Id -cne 'KingmakerMountedCombat' -or [string]$info.Version -cne '0.0.1-feasibility' -or
        [string]$info.ManagerVersion -cne '0.28.2' -or [string]$info.GameVersion -cne '2.1.7' -or
        [string]$info.AssemblyName -cne 'KingmakerMountedCombat.dll' -or
        [string]$info.EntryMethod -cne 'KingmakerMountedCombat.Main.Load' -or @($info.Requirements).Count -ne 0) {
        throw 'Packaged Info.json identity is not exact.'
    }

    New-Item -ItemType Directory -Path $temporaryRoot -Force | Out-Null
    $dllPath = Join-Path $temporaryRoot 'KingmakerMountedCombat.dll'
    $dllEntry = @($archive.Entries | Where-Object { $_.FullName.Replace('\','/') -ceq 'KingmakerMountedCombat/KingmakerMountedCombat.dll' })[0]
    $dllInput = $dllEntry.Open(); $dllOutput = [IO.File]::Create($dllPath)
    try { $dllInput.CopyTo($dllOutput) }
    finally { $dllOutput.Dispose(); $dllInput.Dispose() }
    $assemblyName = [Reflection.AssemblyName]::GetAssemblyName($dllPath)
    $assembly = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($dllPath))
    if ([string]$assemblyName.Name -cne 'KingmakerMountedCombat' -or [string]$assemblyName.Version -cne '0.0.1.0') { throw 'Packaged DLL assembly identity is not exact.' }
    $targetFramework = @($assembly.CustomAttributes | Where-Object AttributeType -eq ([Runtime.Versioning.TargetFrameworkAttribute]) | Select-Object -First 1)
    if ($targetFramework.Count -ne 1 -or [string]$targetFramework[0].ConstructorArguments[0].Value -cne '.NETFramework,Version=v4.7') { throw 'Packaged DLL does not target exact .NET Framework 4.7.' }
    $allowedReferences = @('mscorlib','System','System.Core','Assembly-CSharp','Assembly-CSharp-firstpass','UnityEngine','UnityEngine.CoreModule','UnityEngine.IMGUIModule','UnityModManager','Newtonsoft.Json','0Harmony12')
    $references = @($assembly.GetReferencedAssemblies() | ForEach-Object Name | Sort-Object)
    $unexpected = @($references | Where-Object { $_ -cnotin $allowedReferences })
    if ($unexpected.Count -ne 0 -or $references -contains '0Harmony' -or @($references | Where-Object { $_ -match '(?i)Wrath|BuffPlanner|Gunslinger|Tabletop|CallOfTheWild' }).Count -ne 0) {
        throw "Packaged DLL dependency allowlist mismatch: $($unexpected -join ', ')"
    }
    $dllRecord = @($entryRecords | Where-Object path -ceq 'KingmakerMountedCombat/KingmakerMountedCombat.dll')[0]
    $result = [pscustomobject]@{
        schemaVersion=1; packagePath=$resolvedPackage; packageSha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $resolvedPackage).Hash.ToLowerInvariant()
        dllSha256=[string]$dllRecord.sha256; dllMvid=$assembly.ManifestModule.ModuleVersionId.ToString(); entries=@($entryRecords | Sort-Object path); references=$references
    }
}
finally {
    $archive.Dispose()
    if (Test-Path -LiteralPath $temporaryRoot) {
        if (-not $temporaryRoot.StartsWith($safeTemporaryParent + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Package-validation cleanup escaped its generated root.' }
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}

Write-Host 'TOTAL PASS=10 FAIL=0'
Write-Host "PACKAGE=$resolvedPackage"
Write-Host "SHA256=$($result.packageSha256)"
if ($PassThru) { return $result }
