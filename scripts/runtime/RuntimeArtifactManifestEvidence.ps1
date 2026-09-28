# Original read-only manifest validation, extracted without semantic changes; future source draft.
function Assert-KmcManifestJsonMembersUnique {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Description)
    Add-Type -AssemblyName System.Runtime.Serialization
    $reader = $null
    try {
        $reader = [Runtime.Serialization.Json.JsonReaderWriterFactory]::CreateJsonReader([IO.File]::ReadAllBytes([IO.Path]::GetFullPath($Path)), [Xml.XmlDictionaryReaderQuotas]::Max)
        $document = New-Object Xml.XmlDocument
        $document.Load($reader)
        function Test-JsonObjectNode([Xml.XmlNode]$Node, [string]$Location) {
            if ($Node.NodeType -eq [Xml.XmlNodeType]::Element -and [string]$Node.Attributes['type'].Value -ceq 'object') {
                $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
                foreach ($child in @($Node.ChildNodes | Where-Object NodeType -eq ([Xml.XmlNodeType]::Element))) {
                    $name = if ($child.LocalName -ceq 'item' -and $null -ne $child.Attributes['item']) { [string]$child.Attributes['item'].Value } else { [string]$child.LocalName }
                    if (-not $names.Add($name)) { throw "$Description contains duplicate JSON property '$name' at $Location." }
                }
            }
            foreach ($child in @($Node.ChildNodes | Where-Object NodeType -eq ([Xml.XmlNodeType]::Element))) { Test-JsonObjectNode $child ($Location + '/' + $child.LocalName) }
        }
        Test-JsonObjectNode $document.DocumentElement '$'
    }
    finally { if ($null -ne $reader) { $reader.Dispose() } }
}
function Test-KmcManifestJsonInteger {
    param($Value)
    return $Value -is [sbyte] -or $Value -is [byte] -or
        $Value -is [int16] -or $Value -is [uint16] -or
        $Value -is [int32] -or $Value -is [uint32] -or
        $Value -is [int64] -or $Value -is [uint64]
}
function Assert-KmcReadOnlyArtifactManifest {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$ExpectedSha256,
        [switch]$GameResult
    )

    if ($ExpectedSha256 -isnot [string] -or $ExpectedSha256 -cnotmatch '^[0-9a-f]{64}$') {
        if($GameResult){throw 'Runtime game-result evidenceManifestSha256 is not an exact lowercase SHA-256.'}; throw 'Runtime result evidenceManifestSha256 is not an exact lowercase SHA-256.'
    }

    if ($Request.evidenceRoot -isnot [string] -or $Request.runId -isnot [string] -or
        $Request.scenario -isnot [string]) {
        throw 'Runtime artifact manifest request context must contain exact JSON strings.'
    }

    $evidenceRoot = [IO.Path]::GetFullPath($Request.evidenceRoot).TrimEnd('\')
    Assert-KmcNotReparsePoint $evidenceRoot 'runtime evidence root'
    $manifestPath = Assert-KmcChildPath (Join-Path $evidenceRoot 'runtime-artifacts.json') $evidenceRoot 'runtime artifact manifest'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw 'Runtime artifact manifest is missing.'
    }
    Assert-KmcNotReparsePoint $manifestPath 'runtime artifact manifest'
    Assert-KmcNotHardLink $manifestPath 'runtime artifact manifest'
    if ((Get-KmcSha256 $manifestPath) -cne $ExpectedSha256) {
        if($GameResult){throw 'Runtime artifact manifest hash does not match the runtime game result.'}; throw 'Runtime artifact manifest hash does not match the runtime result.'
    }

    Assert-KmcManifestJsonMembersUnique $manifestPath 'runtime artifact manifest'
    $manifest = Read-KmcJson $manifestPath
    Assert-KmcExactProperties $manifest @('schemaVersion','runId','scenario','createdAtUtc','artifacts') 'runtime artifact manifest'
    if (-not (Test-KmcManifestJsonInteger $manifest.schemaVersion) -or [long]$manifest.schemaVersion -ne 1) {
        throw 'Runtime artifact manifest schemaVersion must be the exact integral value 1.'
    }
    if ($manifest.runId -isnot [string] -or $manifest.scenario -isnot [string] -or
        $manifest.createdAtUtc -isnot [string] -or
        $manifest.runId -cne [string]$Request.runId -or
        $manifest.scenario -cne [string]$Request.scenario) {
        throw 'Runtime artifact manifest identity does not match its request.'
    }
    $createdAt = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$manifest.createdAtUtc, [ref]$createdAt)) {
        throw 'Runtime artifact manifest createdAtUtc is invalid.'
    }
    if ($manifest.artifacts -isnot [Array]) {
        throw 'Runtime artifact manifest artifacts must be an actual JSON array.'
    }

    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($artifact in @($manifest.artifacts)) {
        if ($null -eq $artifact) { throw 'Runtime artifact manifest contains a null artifact.' }
        Assert-KmcExactProperties $artifact @('relativePath','kind','length','sha256') 'runtime artifact manifest record'
        if ($artifact.relativePath -isnot [string] -or $artifact.kind -isnot [string] -or
            $artifact.sha256 -isnot [string]) {
            throw 'Runtime artifact manifest record paths, kinds, and hashes must be JSON strings.'
        }
        $relativePath = $artifact.relativePath
        $kind = $artifact.kind
        if (-not $seen.Add($relativePath)) { throw "Runtime artifact manifest contains duplicate path: $relativePath" }

        $allowed = ($relativePath -ceq 'persistence-observations.jsonl' -and $kind -ceq 'persistence-evidence') -or
            ($relativePath -ceq 'lifecycle-scenario-evidence.jsonl' -and $kind -ceq 'scenario-evidence') -or
            ($relativePath -ceq 'movement-telemetry.jsonl' -and $kind -ceq 'telemetry') -or
            ($relativePath -ceq 'movement-scenario-evidence.jsonl' -and $kind -ceq 'scenario-evidence') -or
            ($relativePath -ceq 'boundary-scenario-evidence.jsonl' -and $kind -ceq 'boundary-evidence') -or
            ($relativePath -ceq 'combat-scenario-evidence.jsonl' -and $kind -ceq 'combat-evidence') -or
            ($relativePath -ceq 'horse-native-asset-audit.json' -and $kind -ceq 'horse-asset-audit') -or
            ($relativePath -ceq 'horse-companion-blueprint-registration.json' -and $kind -ceq 'horse-companion-blueprint-registration') -or
            ($relativePath -ceq 'horse-companion-unmounted.json' -and $kind -ceq 'horse-companion-unmounted') -or
            ($relativePath -ceq 'horse-mounted-alpha.json' -and $kind -ceq 'horse-mounted-alpha') -or
            ($relativePath -ceq 'horse-native-controls-ux.json' -and $kind -ceq 'horse-native-controls-ux') -or
            ($relativePath -ceq 'chunk6a-mount-preamble.json' -and $kind -ceq 'chunk6a-mount-preamble') -or
            ($relativePath -ceq 'chunk6a-native-mammoth-profile.json' -and $kind -ceq 'chunk6a-native-mammoth-profile') -or
            ($relativePath -ceq 'phase3d-horse-scenario-evidence.json' -and $kind -ceq 'phase3d-horse-scenario-evidence') -or
            ($relativePath -cmatch '^movement-visuals/[A-Za-z0-9._-]+\.png$' -and $kind -ceq 'screenshot')
        if (-not $allowed) { throw "Runtime artifact manifest record is outside the exact allowlist: $relativePath ($kind)" }
        if (-not (Test-KmcManifestJsonInteger $artifact.length) -or [long]$artifact.length -le 0 -or
            $artifact.sha256 -cnotmatch '^[0-9a-f]{64}$') {
            throw "Runtime artifact manifest record has invalid length or SHA-256: $relativePath"
        }

        $artifactPath = Assert-KmcChildPath (Join-Path $evidenceRoot $relativePath.Replace('/', '\')) $evidenceRoot 'runtime artifact'
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) { throw "Runtime artifact is missing: $relativePath" }
        Assert-KmcNotReparsePoint (Split-Path -Parent $artifactPath) "runtime artifact parent $relativePath"
        Assert-KmcNotReparsePoint $artifactPath "runtime artifact $relativePath"
        Assert-KmcNotHardLink $artifactPath "runtime artifact $relativePath"
        $artifactFile = Get-Item -LiteralPath $artifactPath -Force
        if ([long]$artifactFile.Length -ne [long]$artifact.length -or (Get-KmcSha256 $artifactPath) -cne [string]$artifact.sha256) {
            throw "Runtime artifact bytes do not match the manifest: $relativePath"
        }
    }
}