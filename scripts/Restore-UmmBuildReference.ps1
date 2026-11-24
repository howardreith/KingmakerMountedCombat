[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pin = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'planning\UMM-BUILD-REFERENCE.json') | ConvertFrom-Json
if ($pin.schemaVersion -ne 1 -or [string]$pin.version -cne '0.31.1' -or [string]$pin.assemblyVersion -cne '0.31.1.0' -or
    [string]$pin.url -cne 'https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.31.1/unitymodmanager.0.31.1.nupkg' -or
    [string]$pin.archiveEntry -cne 'lib/net35/UnityModManager.dll' -or
    [string]$pin.archiveSha256 -cnotmatch '^[0-9a-f]{64}$' -or [string]$pin.sha256 -cnotmatch '^[0-9a-f]{64}$') {
    throw 'The UMM build-reference pin is missing or differs from the supported 0.31.1 reference contract.'
}
$info = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'Info.json') | ConvertFrom-Json
if ([string]$info.ManagerVersion -cne [string]$pin.version) {
    throw 'Info.json ManagerVersion must match the pinned UMM 0.31.1 build reference.'
}

function Assert-UmmReferenceHash([string]$Path, [string]$Expected) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "UMM reference file is missing: $Path" }
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
    if ($actual -cne $Expected) {
        throw "UMM reference hash mismatch; refusing to replace existing bytes: $Path (expected $Expected; actual $actual)"
    }
}

# This cache is ignored by Git. Nothing is written into a game installation or a mod package.
$cache = Join-Path $repoRoot 'obj\references\umm-0.31.1'
$archivePath = Join-Path $cache 'unitymodmanager.0.31.1.nupkg'
$dllPath = Join-Path $cache 'UnityModManager.dll'
if (Test-Path -LiteralPath $archivePath) { Assert-UmmReferenceHash $archivePath ([string]$pin.archiveSha256) }
if (Test-Path -LiteralPath $dllPath) {
    Assert-UmmReferenceHash $dllPath ([string]$pin.sha256)
    Write-Host "PASS pinned UMM $($pin.version) build reference: $dllPath"
    return
}

New-Item -ItemType Directory -Path $cache -Force | Out-Null
if (-not (Test-Path -LiteralPath $archivePath)) {
    $download = Join-Path $cache ([Guid]::NewGuid().ToString('N') + '.download')
    $previousProtocol = [Net.ServicePointManager]::SecurityProtocol
    try {
        [Net.ServicePointManager]::SecurityProtocol = $previousProtocol -bor [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest -UseBasicParsing -Uri ([string]$pin.url) -OutFile $download -TimeoutSec 60
        Assert-UmmReferenceHash $download ([string]$pin.archiveSha256)
        [IO.File]::Move($download, $archivePath)
    }
    finally {
        [Net.ServicePointManager]::SecurityProtocol = $previousProtocol
        if (Test-Path -LiteralPath $download) { Remove-Item -LiteralPath $download -Force }
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
$extracted = Join-Path $cache ([Guid]::NewGuid().ToString('N') + '.dll')
try {
    $entries = @($archive.Entries | Where-Object FullName -CEQ ([string]$pin.archiveEntry))
    if ($entries.Count -ne 1) { throw 'The pinned UMM archive does not contain exactly one expected reference DLL.' }
    $dllInput = $entries[0].Open()
    $dllOutput = [IO.File]::Create($extracted)
    try { $dllInput.CopyTo($dllOutput) }
    finally { $dllOutput.Dispose(); $dllInput.Dispose() }
    Assert-UmmReferenceHash $extracted ([string]$pin.sha256)
    [IO.File]::Move($extracted, $dllPath)
}
finally {
    $archive.Dispose()
    if (Test-Path -LiteralPath $extracted) { Remove-Item -LiteralPath $extracted -Force }
}
Write-Host "PASS pinned UMM $($pin.version) build reference: $dllPath"
