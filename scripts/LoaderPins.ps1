# The loader pin definition (scripts/loader-reference-pins.json), parsed and validated without side effects: no download,
# no cache access. Shared by the acquisition/verification helper, the compiler-input check and both standalone package
# validators, so every consumer refuses the same malformed definitions.
Set-StrictMode -Version Latest

function Read-KmcLoaderPinDefinition([string]$Path = (Join-Path $PSScriptRoot 'loader-reference-pins.json')) {
    $pins = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    if ($pins.schemaVersion -ne 1 -or @($pins.references).Count -eq 0 -or @($pins.manifests).Count -eq 0 -or
        -not ($pins.minimumManagerVersion -is [string]) -or $pins.minimumManagerVersion -cnotmatch '\A[0-9]+([.][0-9]+){2}\z') { throw 'Unknown or empty loader reference pin file.' }
    # Exactly the two required compiler references under their canonical cache names.
    $required = [ordered]@{ 'UnityModManager' = 'UnityModManager.dll'; '0Harmony12' = '0Harmony12.dll' }
    if (@($pins.references).Count -ne $required.Count) { throw "The pin file must define exactly: $($required.Keys -join ', ')." }
    foreach ($name in $required.Keys) {
        $record = @($pins.references | Where-Object { $_.Name -is [string] -and $_.Name -ceq $name })
        if ($record.Count -ne 1 -or -not ($record[0].File -is [string]) -or $record[0].File -cne $required[$name]) {
            throw "The pin file must define $name exactly once as $($required[$name])."
        }
    }
    $seen = @{}
    foreach ($pin in $pins.references) {
        # Every string through its absolute end (a trailing line feed is refused); archive fields as a pair.
        foreach ($field in @('Name', 'File', 'Version', 'Url', 'Sha256')) {
            if (-not $pin.PSObject.Properties[$field] -or -not ($pin.$field -is [string])) { throw 'Invalid or duplicate loader reference pin.' }
        }
        $uri = $null
        if ($pin.File -cnotmatch '\A[A-Za-z0-9._-]+[.]dll\z' -or $pin.Sha256 -cnotmatch '\A[0-9a-f]{64}\z' -or
            $pin.Version -cnotmatch '\A[0-9]+([.][0-9]+){3}\z' -or $pin.Name -cnotmatch '\A[A-Za-z0-9._-]+\z' -or
            -not [Uri]::TryCreate($pin.Url, [UriKind]::Absolute, [ref]$uri) -or $uri.Scheme -cne 'https' -or
            $seen.ContainsKey($pin.File)) { throw 'Invalid or duplicate loader reference pin.' }
        $seen[$pin.File] = $true
        $entry = if ($pin.PSObject.Properties['ArchiveEntry']) { $pin.ArchiveEntry } else { $null }
        $hash = if ($pin.PSObject.Properties['ArchiveSha256']) { $pin.ArchiveSha256 } else { $null }
        if ($null -ne $entry -and $null -eq $hash) { throw "Missing archive hash for $($pin.File)." }
        if ($null -eq $entry -and $null -ne $hash) { throw "Archive hash without an archive entry for $($pin.File)." }
        if ($null -ne $entry -and (-not ($entry -is [string]) -or $entry -cnotmatch '\A[A-Za-z0-9._-]+(/[A-Za-z0-9._-]+)*[.]dll\z' -or
                -not ($hash -is [string]) -or $hash -cnotmatch '\A[0-9a-f]{64}\z')) { throw "Invalid archive entry or archive hash for $($pin.File)." }
    }
    $manager = @($pins.references | Where-Object Name -CEQ 'UnityModManager')
    if ($manager[0].Version -cne ($pins.minimumManagerVersion + '.0')) { throw 'The UnityModManager compile reference must equal the declared minimum.' }
    return $pins
}
