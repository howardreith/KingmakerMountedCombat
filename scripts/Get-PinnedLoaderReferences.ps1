[CmdletBinding()]
param(
    [string]$ReferenceDirectory = '',
    [switch]$VerifyOnly,
    # The exact compiler inputs MSBuild resolved (UnityModManager.dll|0Harmony12.dll): verified against the parsed pins.
    [string]$VerifyCompilerInputs
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
. (Join-Path $PSScriptRoot 'LoaderPins.ps1')
$pins = Read-KmcLoaderPinDefinition
foreach ($manifest in $pins.manifests) {
    $info = Get-Content -Raw -LiteralPath (Join-Path $repoRoot $manifest) | ConvertFrom-Json
    if (-not ($info.ManagerVersion -is [string]) -or $info.ManagerVersion -cne [string]$pins.minimumManagerVersion) {
        throw "$manifest ManagerVersion must equal the pinned baseline $($pins.minimumManagerVersion)."
    }
}
if ($VerifyCompilerInputs) {
    $inputs = @($VerifyCompilerInputs.Split('|') | Where-Object { $_ })
    foreach ($pin in $pins.references) {
        $match = @($inputs | Where-Object { [IO.Path]::GetFileName($_) -ceq $pin.File })
        if ($match.Count -ne 1) { throw "The compiler input for $($pin.File) is missing or duplicated." }
        if ((Get-FileHash -LiteralPath $match[0] -Algorithm SHA256).Hash.ToLowerInvariant() -cne $pin.Sha256) { throw "The compiler input $($match[0]) differs from its pin." }
    }
    if ($inputs.Count -ne @($pins.references).Count) { throw 'Unexpected compiler inputs were supplied for verification.' }
    Write-Output 'Loader compiler inputs: PASS'
    return
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
