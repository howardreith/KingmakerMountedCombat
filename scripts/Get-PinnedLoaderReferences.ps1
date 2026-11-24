[CmdletBinding()]
param(
    [string]$ReferenceDirectory = '',
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pins = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'loader-reference-pins.json') | ConvertFrom-Json
if ($pins.schemaVersion -ne 1 -or @($pins.references).Count -eq 0 -or @($pins.manifests).Count -eq 0 -or
    -not ($pins.minimumManagerVersion -is [string]) -or $pins.minimumManagerVersion -cnotmatch '\A[0-9]+([.][0-9]+){2}\z') { throw 'Unknown or empty loader reference pin file.' }
# Exactly the two required compiler references under their canonical cache names; a missing, duplicate or mismapped
# record is refused before any cache file is read or trusted.
$requiredPins = [ordered]@{ 'UnityModManager' = 'UnityModManager.dll'; '0Harmony12' = '0Harmony12.dll' }
if (@($pins.references).Count -ne $requiredPins.Count) { throw "The pin file must define exactly: $($requiredPins.Keys -join ', ')." }
foreach ($requiredName in $requiredPins.Keys) {
    $record = @($pins.references | Where-Object { $_.Name -is [string] -and $_.Name -ceq $requiredName })
    if ($record.Count -ne 1 -or -not ($record[0].File -is [string]) -or $record[0].File -cne $requiredPins[$requiredName]) {
        throw "The pin file must define $requiredName exactly once as $($requiredPins[$requiredName])."
    }
}
$manager = @($pins.references | Where-Object Name -CEQ 'UnityModManager')
if ($manager.Count -ne 1 -or [string]$manager[0].Version -cne ([string]$pins.minimumManagerVersion + '.0')) {
    throw 'The UnityModManager compile reference must equal the declared minimum.'
}
foreach ($manifest in $pins.manifests) {
    $info = Get-Content -Raw -LiteralPath (Join-Path $repoRoot $manifest) | ConvertFrom-Json
    if (-not ($info.ManagerVersion -is [string]) -or $info.ManagerVersion -cne [string]$pins.minimumManagerVersion) {
        throw "$manifest ManagerVersion must equal the pinned baseline $($pins.minimumManagerVersion)."
    }
}
if ([string]::IsNullOrWhiteSpace($ReferenceDirectory)) { $ReferenceDirectory = Join-Path $repoRoot 'obj/loader-references' }
$ReferenceDirectory = [IO.Path]::GetFullPath($ReferenceDirectory)

function Assert-PinnedReference([string]$Path, $Pin) {
    $hash = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -cne [string]$Pin.Sha256) { throw "Pinned loader reference hash mismatch: $Path. Preserve or remove the mismatched file explicitly before retrying." }
    $identity = [Reflection.AssemblyName]::GetAssemblyName($Path)
    if ($identity.Name -cne [string]$Pin.Name -or $identity.Version.ToString() -cne [string]$Pin.Version) {
        throw "Pinned loader assembly identity mismatch: $Path ($($identity.FullName))."
    }
}

$seen = @{}
foreach ($pin in $pins.references) {
    # Every pin string is matched through its absolute end (\z: a trailing line feed is refused), and the archive fields
    # come as a pair: both null for a direct download, or an archive entry with the archive's SHA-256.
    foreach ($field in @('Name', 'File', 'Version', 'Url', 'Sha256')) {
        if (-not $pin.PSObject.Properties[$field] -or -not ($pin.$field -is [string])) { throw 'Invalid or duplicate loader reference pin.' }
    }
    $uri = $null
    if ($pin.File -cnotmatch '\A[A-Za-z0-9._-]+[.]dll\z' -or
        $pin.Sha256 -cnotmatch '\A[0-9a-f]{64}\z' -or
        $pin.Version -cnotmatch '\A[0-9]+([.][0-9]+){3}\z' -or
        $pin.Name -cnotmatch '\A[A-Za-z0-9._-]+\z' -or
        -not [Uri]::TryCreate($pin.Url, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -cne 'https' -or
        $seen.ContainsKey($pin.File)) { throw 'Invalid or duplicate loader reference pin.' }
    $seen[$pin.File] = $true
    $archiveEntry = if ($pin.PSObject.Properties['ArchiveEntry']) { $pin.ArchiveEntry } else { $null }
    $archiveHash = if ($pin.PSObject.Properties['ArchiveSha256']) { $pin.ArchiveSha256 } else { $null }
    if ($null -ne $archiveEntry -and $null -eq $archiveHash) { throw "Missing archive hash for $($pin.File)." }
    if ($null -eq $archiveEntry -and $null -ne $archiveHash) { throw "Archive hash without an archive entry for $($pin.File)." }
    if ($null -ne $archiveEntry -and (-not ($archiveEntry -is [string]) -or $archiveEntry -cnotmatch '\A[A-Za-z0-9._-]+(/[A-Za-z0-9._-]+)*[.]dll\z' -or
            -not ($archiveHash -is [string]) -or $archiveHash -cnotmatch '\A[0-9a-f]{64}\z')) { throw "Invalid archive entry or archive hash for $($pin.File)." }
    $target = Join-Path $ReferenceDirectory $pin.File
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        Assert-PinnedReference $target $pin
        continue
    }
    if ($VerifyOnly) { throw "Pinned loader reference missing: $target. Run scripts/Get-PinnedLoaderReferences.ps1 first." }
    [IO.Directory]::CreateDirectory($ReferenceDirectory) | Out-Null
    $work = Join-Path $ReferenceDirectory ('.download-' + [Guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($work) | Out-Null
    try {
        $download = Join-Path $work 'source.download'
        $candidate = Join-Path $work $pin.File
        Invoke-WebRequest -Uri $pin.Url -OutFile $download -UseBasicParsing
        if ($pin.ArchiveEntry) {
            if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToLowerInvariant() -cne [string]$pin.ArchiveSha256) { throw "Pinned archive hash mismatch: $($pin.Url)." }
            Add-Type -AssemblyName System.IO.Compression
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [IO.Compression.ZipFile]::OpenRead($download)
            try {
                $entries = @($archive.Entries | Where-Object FullName -CEQ $pin.ArchiveEntry)
                if ($entries.Count -ne 1) { throw "Pinned archive entry is missing or duplicated: $($pin.ArchiveEntry)." }
                [IO.Compression.ZipFileExtensions]::ExtractToFile($entries[0], $candidate, $false)
            }
            finally { $archive.Dispose() }
        }
        else { Copy-Item -LiteralPath $download -Destination $candidate }
        Assert-PinnedReference $candidate $pin
        Move-Item -LiteralPath $candidate -Destination $target
    }
    finally { Remove-Item -LiteralPath $work -Recurse -Force }
}
Write-Output $ReferenceDirectory
