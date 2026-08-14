Set-StrictMode -Version Latest

function Get-KmcRepositoryRoot {
    $root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
    $actual = (& git -C $root rev-parse --show-toplevel 2>$null).Trim()
    if ([IO.Path]::GetFullPath($actual) -ne $root) {
        throw 'Runtime harness is not inside the standalone KingmakerMountedCombat repository.'
    }
    return $root
}

function Get-KmcLabRoot {
    return [IO.Path]::GetFullPath((Join-Path (Get-KmcRepositoryRoot) '..\..'))
}

function Get-KmcSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)
    return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
}

function Get-KmcTextSha256 {
    param([Parameter(Mandatory = $true)][AllowEmptyString()][string]$Text)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text)))).Replace('-', '').ToLowerInvariant()
    }
    finally { $algorithm.Dispose() }
}

function New-KmcRandomToken {
    $bytes = New-Object byte[] 32
    $random = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($bytes) }
    finally { $random.Dispose() }
    return ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

function Write-KmcJsonAtomic {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )
    $fullPath = [IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $fullPath
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    $temporary = Join-Path $parent ('.' + [IO.Path]::GetFileName($fullPath) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        [IO.File]::WriteAllText($temporary, ($Value | ConvertTo-Json -Depth 30), (New-Object Text.UTF8Encoding($false)))
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $replacementBackup = Join-Path $parent ('.' + [IO.Path]::GetFileName($fullPath) + '.' + [Guid]::NewGuid().ToString('N') + '.bak')
            try { [IO.File]::Replace($temporary, $fullPath, $replacementBackup) }
            finally { if (Test-Path -LiteralPath $replacementBackup) { Remove-Item -LiteralPath $replacementBackup -Force } }
        }
        else { [IO.File]::Move($temporary, $fullPath) }
    }
    finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force } }
}

function Read-KmcJson {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "JSON file is missing: $Path" }
    return Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
}

function Assert-KmcExactProperties {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string[]]$Required,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $actual = @($Value.PSObject.Properties.Name | Sort-Object)
    $expected = @($Required | Sort-Object)
    if (($actual -join "`n") -cne ($expected -join "`n")) {
        throw "$Description property set is not exact. Actual: $($actual -join ', ')"
    }
}

function Assert-KmcJsonObjectMembersUnique {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Json,
        [Parameter(Mandatory = $true)][string]$Description
    )
    Add-Type -AssemblyName System.Runtime.Serialization
    $reader = $null
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Json)
        $reader = [Runtime.Serialization.Json.JsonReaderWriterFactory]::CreateJsonReader(
            $bytes,
            [Xml.XmlDictionaryReaderQuotas]::Max)
        $document = New-Object Xml.XmlDocument
        $document.Load($reader)
        $objects = @($document.SelectNodes('//*[@type="object"]'))
        foreach ($object in $objects) {
            $names = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
            foreach ($member in @($object.ChildNodes | Where-Object NodeType -eq ([Xml.XmlNodeType]::Element))) {
                $memberName = if ($member.LocalName -ceq 'item' -and $null -ne $member.Attributes['item']) {
                    [string]$member.Attributes['item'].Value
                } else {
                    [string]$member.LocalName
                }
                if (-not $names.Add($memberName)) {
                    throw "$Description contains a duplicate or case-ambiguous JSON object member: $memberName"
                }
            }
        }
    }
    catch [Xml.XmlException] {
        throw "$Description is not valid bounded JSON: $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
    }
}

function Assert-KmcNotHardLink {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Description)
    $item = Get-Item -LiteralPath $Path -Force
    $linkTypeProperty = $item.PSObject.Properties['LinkType']
    if ($null -ne $linkTypeProperty -and [string]$linkTypeProperty.Value -ceq 'HardLink') {
        throw "$Description is a hard link and cannot establish independent fixture identity: $($item.FullName)"
    }
}

function Assert-KmcDirectoryTreeCloneable {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) { throw "$Description is missing: $fullRoot" }
    Assert-KmcNotReparsePoint $fullRoot $Description
    $pending = New-Object 'Collections.Generic.Queue[string]'
    $pending.Enqueue($fullRoot)
    while ($pending.Count -ne 0) {
        $current = $pending.Dequeue()
        foreach ($item in @(Get-ChildItem -LiteralPath $current -Force | Sort-Object Name)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "$Description contains a descendant reparse point: $($item.FullName)"
            }
            if ($item.PSIsContainer) {
                $pending.Enqueue($item.FullName)
                continue
            }
            $linkTypeProperty = $item.PSObject.Properties['LinkType']
            if ($null -ne $linkTypeProperty -and [string]$linkTypeProperty.Value -ceq 'HardLink') {
                throw "$Description contains a detectable descendant hard link: $($item.FullName)"
            }
        }
    }
}

function Copy-KmcDirectoryTreeExact {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][string]$DestinationRoot
    )
    $fullSource = [IO.Path]::GetFullPath($SourceRoot).TrimEnd('\')
    $fullDestination = [IO.Path]::GetFullPath($DestinationRoot).TrimEnd('\')
    if (Test-Path -LiteralPath $fullDestination) { throw "Exact clone destination already exists: $fullDestination" }
    Assert-KmcDirectoryTreeCloneable $fullSource 'exact clone source'
    New-Item -ItemType Directory -Path $fullDestination | Out-Null
    $pending = New-Object Collections.Queue
    $pending.Enqueue([pscustomobject]@{ Source = $fullSource; Destination = $fullDestination })
    while ($pending.Count -ne 0) {
        $pair = $pending.Dequeue()
        foreach ($item in @(Get-ChildItem -LiteralPath ([string]$pair.Source) -Force | Sort-Object Name)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Exact clone source gained a descendant reparse point: $($item.FullName)"
            }
            $target = Assert-KmcChildPath (Join-Path ([string]$pair.Destination) $item.Name) $fullDestination 'exact clone entry'
            if ($item.PSIsContainer) {
                New-Item -ItemType Directory -Path $target | Out-Null
                $pending.Enqueue([pscustomobject]@{ Source = $item.FullName; Destination = $target })
                continue
            }
            $linkTypeProperty = $item.PSObject.Properties['LinkType']
            if ($null -ne $linkTypeProperty -and [string]$linkTypeProperty.Value -ceq 'HardLink') {
                throw "Exact clone source gained a detectable descendant hard link: $($item.FullName)"
            }
            Copy-Item -LiteralPath $item.FullName -Destination $target
        }
    }
    Assert-KmcDirectoryTreeCloneable $fullDestination 'exact clone result'
    return (Get-KmcDirectoryManifest $fullDestination)
}

function Assert-KmcDirectoryManifestsEqual {
    param(
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ([string]$Actual.digest -cne [string]$Expected.digest -or
        [int]$Actual.fileCount -ne [int]$Expected.fileCount -or
        [int]$Actual.directoryCount -ne [int]$Expected.directoryCount -or
        [long]$Actual.totalBytes -ne [long]$Expected.totalBytes) {
        throw "$Description manifests differ."
    }
}

function Get-KmcDirectoryManifest {
    param([Parameter(Mandatory = $true)][string]$Root)
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) { throw "Manifest root is missing: $fullRoot" }
    $records = New-Object 'System.Collections.Generic.List[object]'
    foreach ($directory in @(Get-ChildItem -LiteralPath $fullRoot -Directory -Recurse -Force | Sort-Object FullName)) {
        $records.Add([pscustomobject]@{ kind = 'directory'; path = $directory.FullName.Substring($fullRoot.Length + 1).Replace('\', '/'); length = 0; sha256 = $null })
    }
    foreach ($file in @(Get-ChildItem -LiteralPath $fullRoot -File -Recurse -Force | Sort-Object FullName)) {
        $records.Add([pscustomobject]@{ kind = 'file'; path = $file.FullName.Substring($fullRoot.Length + 1).Replace('\', '/'); length = [long]$file.Length; sha256 = Get-KmcSha256 $file.FullName })
    }
    $ordered = @($records | Sort-Object kind, path)
    $fileRecords = @($ordered | Where-Object kind -eq 'file')
    $totalBytes = [long]0
    foreach ($fileRecord in $fileRecords) { $totalBytes += [long]$fileRecord.length }
    $canonical = ($ordered | ForEach-Object { '{0}|{1}|{2}|{3}' -f $_.kind, $_.path, $_.length, $_.sha256 }) -join "`n"
    return [pscustomobject]@{
        schemaVersion = 1; root = $fullRoot
        directoryCount = @($ordered | Where-Object kind -eq 'directory').Count
        fileCount = $fileRecords.Count
        totalBytes = $totalBytes
        digest = Get-KmcTextSha256 $canonical; entries = $ordered
    }
}

function Get-KmcSaveMetadataInventory {
    param([Parameter(Mandatory = $true)][string]$SaveRoot)
    $fullRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) { throw "Save root is missing: $fullRoot" }
    $records = @()
    # Kingmaker save slots and DotNetZip recovery artifacts are direct children.
    # Recording directory entries (without traversing them) detects unknown/reparse
    # additions without reading any foreign save payload.
    foreach ($item in @(Get-ChildItem -LiteralPath $fullRoot -Force | Sort-Object FullName)) {
        $kind = if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { 'reparse' }
            elseif ($item.PSIsContainer) { 'directory' }
            else { 'file' }
        $records += [pscustomobject]@{
            kind = $kind
            path = $item.FullName.Substring($fullRoot.Length + 1).Replace('\', '/')
            length = if ($kind -ceq 'file') { [long]$item.Length } else { [long]0 }
            lastWriteTimeUtcTicks = $item.LastWriteTimeUtc.Ticks
        }
    }
    $totalBytes = [long]0
    foreach ($record in @($records | Where-Object kind -eq 'file')) { $totalBytes += [long]$record.length }
    $canonical = ($records | ForEach-Object { '{0}|{1}|{2}|{3}' -f $_.kind, $_.path, $_.length, $_.lastWriteTimeUtcTicks }) -join "`n"
    return [pscustomobject]@{
        schemaVersion = 2
        root = $fullRoot
        fileCount = @($records | Where-Object kind -eq 'file').Count
        totalBytes = $totalBytes
        digest = Get-KmcTextSha256 $canonical
        entries = @($records)
    }
}

function Get-KmcProtectedSaveMetadata {
    param([Parameter(Mandatory = $true)][string]$SaveRoot)
    $inventory = Get-KmcSaveMetadataInventory $SaveRoot
    return [pscustomobject]@{
        schemaVersion = 2
        fileCount = $inventory.fileCount
        totalBytes = $inventory.totalBytes
        digest = $inventory.digest
    }
}

function Get-KmcFixtureCandidateAudit {
    param([Parameter(Mandatory = $true)][string]$SaveRoot)
    $fullRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) { throw "Save root is missing: $fullRoot" }
    Assert-KmcNotReparsePoint $fullRoot 'Kingmaker save root'

    $files = @(Get-ChildItem -LiteralPath $fullRoot -File -Force)
    $baseline = @($files | Where-Object { $_.Name -cmatch '^Manual_[0-9]+_KMC_AUTOMATION_BASELINE\.zks$' })
    $working = @($files | Where-Object { $_.Name -cmatch '^Manual_[0-9]+_KMC_AUTOMATION_WORKING\.zks$' })
    $recognized = @($baseline + $working | ForEach-Object FullName)
    $rejected = @($files | Where-Object {
        $_.Name -match 'KMC_AUTOMATION' -and $_.FullName -cnotin $recognized
    } | ForEach-Object Name | Sort-Object)

    return [pscustomobject]@{
        schemaVersion = 1
        saveRoot = $fullRoot
        baselineCount = $baseline.Count
        workingCount = $working.Count
        baselinePaths = @($baseline | ForEach-Object FullName)
        workingPaths = @($working | ForEach-Object FullName)
        rejectedKmcLookingNames = $rejected
    }
}

function Read-KmcFixtureHeader {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][ValidateSet('baseline','working')][string]$Kind,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [string]$PermittedFileNamePattern
    )
    $fullRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $fullPath = Assert-KmcChildPath $Path $fullRoot "KMC $Kind fixture"
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { throw "KMC $Kind fixture is missing: $fullPath" }
    Assert-KmcNotReparsePoint $fullPath "KMC $Kind fixture"
    Assert-KmcNotHardLink $fullPath "KMC $Kind fixture"

    $expectedName = if ($Kind -ceq 'baseline') { 'KMC_AUTOMATION_BASELINE' } else { 'KMC_AUTOMATION_WORKING' }
    $expectedFilePattern = if (-not [string]::IsNullOrEmpty($PermittedFileNamePattern)) {
        if ($Kind -cne 'working') { throw 'Only a Working artifact may use an alternate guarded filename pattern.' }
        $PermittedFileNamePattern
    }
    elseif ($Kind -ceq 'baseline') {
        '^Manual_[0-9]+_KMC_AUTOMATION_BASELINE\.zks$'
    }
    else {
        '^Manual_[0-9]+_KMC_AUTOMATION_WORKING\.zks$'
    }
    $leaf = [IO.Path]::GetFileName($fullPath)
    $filenameMatches = if (-not [string]::IsNullOrEmpty($PermittedFileNamePattern)) {
        $leaf -match $expectedFilePattern
    }
    else { $leaf -cmatch $expectedFilePattern }
    if (-not $filenameMatches) { throw "KMC $Kind fixture filename is not exact: $leaf" }

    $before = Get-Item -LiteralPath $fullPath
    if ($before.Length -le 0 -or $before.Length -gt 256MB) { throw "KMC $Kind fixture size is outside the guarded range." }
    # Windows PowerShell 5.1 does not load the assembly that owns
    # ZipArchive/ZipArchiveMode when only FileSystem is requested. The broad
    # harness tests previously masked this by exercising Compress-Archive first.
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = $null
    $fileStream = $null
    $reader = $null
    try {
        $fileStream = New-Object IO.FileStream($fullPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        $archive = New-Object IO.Compression.ZipArchive($fileStream, [IO.Compression.ZipArchiveMode]::Read, $false)
        $headers = @($archive.Entries | Where-Object { $_.FullName -ceq 'header.json' })
        if ($headers.Count -ne 1) { throw "KMC $Kind fixture must contain exactly one root header.json entry." }
        $headerEntry = $headers[0]
        if ($headerEntry.Length -le 0 -or $headerEntry.Length -gt 1MB) { throw "KMC $Kind header.json size is outside the guarded range." }
        $reader = New-Object IO.StreamReader($headerEntry.Open(), (New-Object Text.UTF8Encoding($false, $true)), $true)
        $headerJson = $reader.ReadToEnd()
        Assert-KmcJsonObjectMembersUnique -Json $headerJson -Description "KMC $Kind header"
        $header = $headerJson | ConvertFrom-Json
    }
    catch [IO.InvalidDataException] {
        throw "KMC $Kind fixture archive is unreadable: $leaf"
    }
    finally {
        if ($null -ne $reader) { $reader.Dispose() }
        if ($null -ne $archive) { $archive.Dispose() }
        if ($null -ne $fileStream) { $fileStream.Dispose() }
    }

    if ($header -isnot [pscustomobject]) { throw "KMC $Kind header root is not a JSON object." }
    $required = @('Name','GameName','GameId','Area','Type','CompatibilityVersion')
    $actual = @($header.PSObject.Properties.Name)
    if (@($required | Where-Object { $_ -cnotin $actual }).Count -ne 0) { throw "KMC $Kind header is missing an identity field." }
    foreach ($stringField in @('Name','GameName','GameId','Area','Type')) {
        if ($header.$stringField -isnot [string]) { throw "KMC $Kind header field $stringField is not an exact JSON string." }
    }
    $compatibility = $header.CompatibilityVersion
    if (($compatibility -isnot [int] -and $compatibility -isnot [long]) -or [long]$compatibility -ne 1) {
        throw "KMC $Kind compatibility version is not the exact integral value 1."
    }
    $name = [string]$header.Name
    $gameName = [string]$header.GameName
    $gameId = [string]$header.GameId
    $area = [string]$header.Area
    if ($name -cne $expectedName) { throw "KMC $Kind internal save name is not exact." }
    if ([string]::IsNullOrWhiteSpace($gameName) -or [string]::IsNullOrWhiteSpace($gameId) -or [string]::IsNullOrWhiteSpace($area)) {
        throw "KMC $Kind header contains an empty campaign identity field."
    }
    $parsedGameId = [Guid]::Empty
    if (-not [Guid]::TryParse($gameId, [ref]$parsedGameId)) { throw "KMC $Kind GameId is not a GUID." }
    if ([string]$header.Type -cne 'Manual') { throw "KMC $Kind save type is not Manual." }
    if ($area -cnotmatch '^[0-9a-f]{32}$') { throw "KMC $Kind Area is not an exact lowercase blueprint GUID." }

    $after = Get-Item -LiteralPath $fullPath
    if ($after.Length -ne $before.Length -or $after.LastWriteTimeUtc.Ticks -ne $before.LastWriteTimeUtc.Ticks) {
        throw "KMC $Kind fixture changed while its descriptor was being read."
    }
    $hash = Get-KmcSha256 $fullPath
    $final = Get-Item -LiteralPath $fullPath
    if ($final.Length -ne $before.Length -or $final.LastWriteTimeUtc.Ticks -ne $before.LastWriteTimeUtc.Ticks) {
        throw "KMC $Kind fixture changed while it was being hashed."
    }

    return [pscustomobject]@{
        schemaVersion = 1
        kind = $Kind
        name = $name
        fileName = $leaf
        path = $fullPath
        sha256 = $hash
        length = [long]$final.Length
        lastWriteTimeUtcTicks = [long]$final.LastWriteTimeUtc.Ticks
        gameName = $gameName
        gameId = $gameId
        area = $area
    }
}

function Get-KmcValidatedFixturePair {
    param(
        [Parameter(Mandatory = $true)][string]$SaveRoot
    )
    $audit = Get-KmcFixtureCandidateAudit $SaveRoot
    if ($audit.baselineCount -ne 1 -or $audit.workingCount -ne 1 -or $audit.rejectedKmcLookingNames.Count -ne 0) {
        $rejected = if ($audit.rejectedKmcLookingNames.Count -eq 0) { '<none>' } else { $audit.rejectedKmcLookingNames -join ', ' }
        throw "Exact KMC filename audit failed: baseline=$($audit.baselineCount); working=$($audit.workingCount); rejected KMC-looking names=$rejected."
    }

    $baseline = Read-KmcFixtureHeader -Path $audit.baselinePaths[0] -Kind baseline -SaveRoot $audit.saveRoot
    $working = Read-KmcFixtureHeader -Path $audit.workingPaths[0] -Kind working -SaveRoot $audit.saveRoot
    if ([string]::Equals($baseline.path, $working.path, [StringComparison]::OrdinalIgnoreCase)) { throw 'KMC baseline and working fixture paths are not distinct.' }
    if ($baseline.gameId -cne $working.gameId -or $baseline.gameName -cne $working.gameName -or $baseline.area -cne $working.area) {
        throw 'KMC fixture GameId, GameName, and Area identities do not match exactly.'
    }

    return [pscustomobject]@{
        schemaVersion = 1
        baseline = $baseline
        working = $working
        expectedGameName = $working.gameName
        expectedGameId = $working.gameId
        expectedArea = $working.area
        writableSaveNames = @('KMC_AUTOMATION_WORKING')
    }
}

function Assert-KmcFixtureQualification {
    param(
        [Parameter(Mandatory = $true)]$Pair,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [switch]$InitializeQualification
    )
    $baseline = $Pair.baseline
    $working = $Pair.working

    $fullQualification = [IO.Path]::GetFullPath($QualificationPath)
    $qualification = [ordered]@{
        schemaVersion = 1
        baselineName = $baseline.name
        baselineFileName = $baseline.fileName
        baselinePath = $baseline.path
        baselineSha256 = $baseline.sha256
        baselineLength = $baseline.length
        baselineLastWriteTimeUtcTicks = $baseline.lastWriteTimeUtcTicks
        workingName = $working.name
        workingFileName = $working.fileName
        workingPath = $working.path
        initialWorkingSha256 = $working.sha256
        initialWorkingLength = $working.length
        initialWorkingLastWriteTimeUtcTicks = $working.lastWriteTimeUtcTicks
        expectedGameName = $working.gameName
        expectedGameId = $working.gameId
        expectedArea = $working.area
        qualifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        writableSaveNames = @('KMC_AUTOMATION_WORKING')
    }
    if (Test-Path -LiteralPath $fullQualification -PathType Leaf) {
        $recorded = Read-KmcJson $fullQualification
        Assert-KmcExactProperties $recorded @($qualification.Keys) 'KMC fixture qualification'
        if ([int]$recorded.schemaVersion -ne 1 -or
            [string]$recorded.baselineName -cne $baseline.name -or
            [string]$recorded.baselineFileName -cne $baseline.fileName -or
            -not [string]::Equals([string]$recorded.baselinePath, $baseline.path, [StringComparison]::OrdinalIgnoreCase) -or
            [string]$recorded.baselineSha256 -cne $baseline.sha256 -or
            [long]$recorded.baselineLength -ne $baseline.length -or
            [long]$recorded.baselineLastWriteTimeUtcTicks -ne $baseline.lastWriteTimeUtcTicks -or
            [string]$recorded.workingName -cne $working.name -or
            [string]$recorded.workingFileName -cne $working.fileName -or
            -not [string]::Equals([string]$recorded.workingPath, $working.path, [StringComparison]::OrdinalIgnoreCase) -or
            [string]$recorded.initialWorkingSha256 -cne $working.sha256 -or
            [long]$recorded.initialWorkingLength -ne $working.length -or
            [long]$recorded.initialWorkingLastWriteTimeUtcTicks -ne $working.lastWriteTimeUtcTicks -or
            [string]$recorded.expectedGameName -cne $working.gameName -or
            [string]$recorded.expectedGameId -cne $working.gameId -or
            [string]$recorded.expectedArea -cne $working.area -or
            @($recorded.writableSaveNames).Count -ne 1 -or [string]@($recorded.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING') {
            throw 'KMC fixture pair differs from the durable qualification or the baseline is no longer immutable.'
        }
    }
    elseif ($InitializeQualification) {
        Write-KmcJsonAtomic $fullQualification $qualification
    }
    else {
        throw "KMC fixture qualification is missing: $fullQualification"
    }

    $Pair | Add-Member -NotePropertyName qualificationPath -NotePropertyValue $fullQualification -Force
    return $Pair
}

function Assert-KmcFixturePair {
    param(
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [switch]$InitializeQualification
    )
    $pair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
    return Assert-KmcFixtureQualification -Pair $pair -QualificationPath $QualificationPath -InitializeQualification:$InitializeQualification
}

function Assert-KmcSaveWriteAllowlist {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After,
        [Parameter(Mandatory = $true)][string]$WorkingPath
    )
    if ([int]$Before.schemaVersion -ne 2 -or [int]$After.schemaVersion -ne 2 -or
        -not [string]::Equals([string]$Before.root, [string]$After.root, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Save metadata inventories are not comparable.'
    }
    $root = [IO.Path]::GetFullPath([string]$Before.root).TrimEnd('\')
    $working = Assert-KmcChildPath $WorkingPath $root 'writable KMC working save'
    $workingRelative = $working.Substring($root.Length + 1).Replace('\','/')
    $beforeMap = @{}; foreach ($entry in @($Before.entries)) { $beforeMap[[string]$entry.path] = $entry }
    $afterMap = @{}; foreach ($entry in @($After.entries)) { $afterMap[[string]$entry.path] = $entry }
    $paths = @($beforeMap.Keys + $afterMap.Keys | Sort-Object -Unique)
    $changed = New-Object 'System.Collections.Generic.List[string]'
    foreach ($path in $paths) {
        $left = $beforeMap[$path]; $right = $afterMap[$path]
        if ($null -eq $left -or $null -eq $right -or [string]$left.kind -cne [string]$right.kind -or [long]$left.length -ne [long]$right.length -or
            [long]$left.lastWriteTimeUtcTicks -ne [long]$right.lastWriteTimeUtcTicks) { $changed.Add($path) }
    }
    $prohibited = @($changed | Where-Object { -not [string]::Equals($_, $workingRelative, [StringComparison]::OrdinalIgnoreCase) })
    if ($prohibited.Count -ne 0) { throw "Save write allowlist violation: $($prohibited -join ', ')" }
    return [pscustomobject]@{ schemaVersion=1; workingChanged=($changed.Count -eq 1); changedPaths=@($changed) }
}

function Get-KmcSaveTransactionStatePath {
    param([Parameter(Mandatory = $true)][string]$StateRoot, [Parameter(Mandatory = $true)][string]$RunId)
    $transactionRoot = Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'save-transactions'
    return Assert-KmcChildPath (Join-Path $transactionRoot ($RunId + '.json')) $transactionRoot 'save transaction state'
}

function Write-KmcJsonDurable {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )
    Write-KmcJsonAtomic -Path $Path -Value $Value
    $stream = New-Object IO.FileStream([IO.Path]::GetFullPath($Path), [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
    try { $stream.Flush($true) }
    finally { $stream.Dispose() }
}

function Get-KmcRuntimeArchiveWriteLimit {
    param([Parameter(Mandatory = $true)][string]$Scenario)
    if ($Scenario -ceq 'phase-1-runtime-suite') {
        throw 'phase-1-runtime-suite has no proven bounded LoadRoutine write count and cannot own a save transaction.'
    }
    if (@(Get-KmcSaveBackedRuntimeScenarios | Where-Object { $_ -ceq $Scenario }).Count -ne 1) {
        throw "Scenario is not an exact save-backed runtime scenario: $Scenario"
    }
    if ($Scenario -cin @('mounted-pair-load-safety','boundary-suite')) { return 2 }
    return 1
}

function Get-KmcSaveArtifactPlanDigest {
    param([Parameter(Mandatory = $true)]$ArtifactPlan)
    $lines = @(@($ArtifactPlan) | Sort-Object sourceRelativePath | ForEach-Object {
        '{0}|{1}|{2}|{3}|{4}|{5}' -f [string]$_.kind, [string]$_.sourceRelativePath,
            [string]$_.quarantinePath, [long]$_.length, [long]$_.lastWriteTimeUtcTicks, [string]$_.sha256
    })
    return Get-KmcTextSha256 ($lines -join "`n")
}

function Test-KmcInventoryEntryExact {
    param($Left, $Right)
    return $null -ne $Left -and $null -ne $Right -and
        [string]$Left.kind -ceq [string]$Right.kind -and [string]$Left.path -ceq [string]$Right.path -and
        [long]$Left.length -eq [long]$Right.length -and
        [long]$Left.lastWriteTimeUtcTicks -eq [long]$Right.lastWriteTimeUtcTicks
}

function Get-KmcWorkingSaveRecoveryPlan {
    param(
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $workingPath = Assert-KmcChildPath ([string]$State.workingPath) $fullSaveRoot 'recorded KMC working save'
    $workingLeaf = [IO.Path]::GetFileName($workingPath)
    $workingRelative = $workingLeaf
    $maximumWrites = [int]$State.maxRuntimeArchiveWrites
    if ($maximumWrites -lt 1 -or $maximumWrites -gt 2) { throw 'Recorded runtime archive-write bound is outside the Phase 1 range.' }

    $before = $State.beforeInventory
    $current = Get-KmcSaveMetadataInventory $fullSaveRoot
    $beforeMap = @{}; foreach ($entry in @($before.entries)) { $beforeMap[[string]$entry.path] = $entry }
    $currentMap = @{}; foreach ($entry in @($current.entries)) { $currentMap[[string]$entry.path] = $entry }
    $allPaths = @($beforeMap.Keys + $currentMap.Keys | Sort-Object -Unique)
    $changed = @($allPaths | Where-Object { -not (Test-KmcInventoryEntryExact $beforeMap[$_] $currentMap[$_]) })

    $artifactRoot = Assert-KmcChildPath ([string]$State.artifactQuarantineRoot) (Join-Path ([IO.Path]::GetFullPath($StagingRoot)) 'save-transactions') 'recorded save-artifact quarantine root'
    $plan = New-Object 'System.Collections.Generic.List[object]'
    $unknown = New-Object 'System.Collections.Generic.List[string]'
    $counts = @{ 'working-current'=0; 'dotnetzip-temp'=0; 'working-sidecar'=0 }
    $workingDisposition = 'unchanged'
    $sidecarPattern = '^' + [Regex]::Escape($workingLeaf) + '\.[a-z0-9]{8}\.[a-z0-9]{3}$'
    $canonicalPattern = '^Manual_[0-9]+_KMC_AUTOMATION_WORKING\.zks$'
    $tempPattern = '^DotNetZip-[a-z0-9]{8}\.tmp$'

    # Metadata inventories deliberately avoid reading foreign saves. Working is
    # the single authorized content boundary, so close the same-size/same-time
    # substitution gap by hashing it explicitly.
    if ($currentMap.ContainsKey($workingRelative) -and
        -not ($changed -contains $workingRelative) -and
        (Get-KmcSha256 $workingPath) -cne [string]$State.workingSha256) {
        $changed = @($changed + $workingRelative | Sort-Object -Unique)
    }

    foreach ($relativePath in $changed) {
        $beforeEntry = $beforeMap[$relativePath]
        $currentEntry = $currentMap[$relativePath]
        if ([string]::Equals([string]$relativePath, $workingRelative, [StringComparison]::OrdinalIgnoreCase)) {
            if ($null -eq $currentEntry) {
                $workingDisposition = 'missing'
                continue
            }
            $kind = 'working-current'
            $workingDisposition = 'modified'
        }
        elseif ($null -ne $beforeEntry) {
            $unknown.Add([string]$relativePath)
            continue
        }
        elseif ([string]$relativePath -match '/') {
            $unknown.Add([string]$relativePath)
            continue
        }
        elseif ([string]$relativePath -match $tempPattern) { $kind = 'dotnetzip-temp' }
        elseif ([string]$relativePath -match $sidecarPattern) { $kind = 'working-sidecar' }
        elseif ([string]$relativePath -cmatch $canonicalPattern) {
            # Phase 1 denies every SaveRoutine call. Unlike DotNetZip's bounded
            # LoadRoutine temp/sidecar files, a second canonical slot can never
            # be attributed to this transaction.
            $unknown.Add([string]$relativePath)
            continue
        }
        else {
            $unknown.Add([string]$relativePath)
            continue
        }

        $sourcePath = Assert-KmcChildPath (Join-Path $fullSaveRoot ([string]$relativePath)) $fullSaveRoot 'runtime save artifact'
        if (-not [string]::Equals((Split-Path -Parent $sourcePath), $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Runtime save artifact is not a direct child of the save root: $relativePath"
        }
        Assert-KmcNotReparsePoint $sourcePath 'runtime save artifact'
        Assert-KmcNotHardLink $sourcePath 'runtime save artifact'
        $item = Get-Item -LiteralPath $sourcePath -Force
        if ($item.PSIsContainer -or $item.Length -gt 256MB) { throw "Runtime save artifact is not a bounded ordinary file: $relativePath" }

        if ($kind -cne 'dotnetzip-temp') {
            $permittedPattern = if ($kind -ceq 'working-sidecar') { $sidecarPattern } else { $canonicalPattern }
            $descriptor = Read-KmcFixtureHeader -Path $sourcePath -Kind working -SaveRoot $fullSaveRoot -PermittedFileNamePattern $permittedPattern
            if ([string]$descriptor.gameName -cne [string]$State.expectedGameName -or
                [string]$descriptor.gameId -cne [string]$State.expectedGameId -or
                [string]$descriptor.area -cne [string]$State.expectedArea) {
                throw "Runtime Working artifact has a foreign campaign identity: $relativePath"
            }
            $sha256 = [string]$descriptor.sha256
        }
        else { $sha256 = Get-KmcSha256 $sourcePath }

        $counts[$kind]++
        $index = $plan.Count
        $quarantinePath = Assert-KmcChildPath (Join-Path $artifactRoot ('{0:D2}-{1}' -f $index, [IO.Path]::GetFileName($sourcePath))) $artifactRoot 'planned save-artifact quarantine'
        $plan.Add([pscustomobject][ordered]@{
            kind = $kind
            sourcePath = $sourcePath
            sourceRelativePath = [string]$relativePath
            quarantinePath = $quarantinePath
            length = [long]$item.Length
            lastWriteTimeUtcTicks = [long]$item.LastWriteTimeUtc.Ticks
            sha256 = $sha256
        })
    }
    if ($unknown.Count -ne 0) { throw "Save inventory contains non-owned drift: $($unknown -join ', ')" }
    foreach ($boundedKind in @('dotnetzip-temp','working-sidecar')) {
        if ([int]$counts[$boundedKind] -gt $maximumWrites) {
            throw "Runtime save artifact count exceeds the scenario bound for $boundedKind`: $($counts[$boundedKind]) > $maximumWrites."
        }
    }
    if ([int]$counts['working-current'] -gt 1 -or $plan.Count -gt (1 + (2 * $maximumWrites))) {
        throw 'Runtime Working slot-family artifact count exceeds the Phase 1 transaction bound.'
    }

    $afterValidation = Get-KmcSaveMetadataInventory $fullSaveRoot
    if ([string]$afterValidation.digest -cne [string]$current.digest) {
        throw 'Save inventory changed while its recovery plan was being validated.'
    }
    return [pscustomobject]@{
        currentInventoryDigest = [string]$current.digest
        workingDisposition = $workingDisposition
        artifacts = $plan.ToArray()
        digest = Get-KmcSaveArtifactPlanDigest -ArtifactPlan ($plan.ToArray())
    }
}

function Initialize-KmcWorkingSaveRecoveryPlan {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)][string]$StatePath,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot
    )
    [void](Assert-KmcRuntimeLockOwner $Lock)
    Assert-KmcNoGameProcesses
    $state = Read-KmcJson $StatePath
    if ($state.PSObject.Properties['artifactPlan']) { return $state }
    $recovery = Get-KmcWorkingSaveRecoveryPlan -State $state -SaveRoot $SaveRoot -StagingRoot $StagingRoot
    $state.phase = 'recovery-planned'
    foreach ($entry in ([ordered]@{
        restoreStartedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        saveWriteAllowlistPassed = $true
        runtimeInventoryDigest = [string]$recovery.currentInventoryDigest
        workingDisposition = [string]$recovery.workingDisposition
        artifactPlan = @($recovery.artifacts)
        artifactPlanDigest = [string]$recovery.digest
        recoveryPlannedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    }).GetEnumerator()) { $state | Add-Member -NotePropertyName $entry.Key -NotePropertyValue $entry.Value -Force }
    Write-KmcJsonDurable -Path $StatePath -Value $state
    return Read-KmcJson $StatePath
}

function Enter-KmcWorkingSaveTransaction {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)]$Pair,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot,
        [Parameter(Mandatory = $true)][string]$Scenario
    )
    [void](Assert-KmcRuntimeLockOwner $Lock)
    Assert-KmcNoGameProcesses
    $runId = [string]$Lock.RunId
    $maximumWrites = Get-KmcRuntimeArchiveWriteLimit $Scenario
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $workingPath = Assert-KmcChildPath ([string]$Pair.working.path) $fullSaveRoot 'qualified KMC working save'
    $baselinePath = Assert-KmcChildPath ([string]$Pair.baseline.path) $fullSaveRoot 'qualified KMC baseline save'
    if ([string]::Equals($workingPath, $baselinePath, [StringComparison]::OrdinalIgnoreCase)) { throw 'Save transaction paths are not distinct.' }
    Assert-KmcNotReparsePoint $workingPath 'qualified KMC working save'
    Assert-KmcNotReparsePoint $baselinePath 'qualified KMC baseline save'
    Assert-KmcNotHardLink $workingPath 'qualified KMC working save'
    Assert-KmcNotHardLink $baselinePath 'qualified KMC baseline save'

    # Re-read only the two exact KMC descriptors immediately before freezing Working.
    # This closes the gap between initial qualification and transaction ownership.
    $freshBaseline = Read-KmcFixtureHeader -Path $baselinePath -Kind baseline -SaveRoot $fullSaveRoot
    $freshWorking = Read-KmcFixtureHeader -Path $workingPath -Kind working -SaveRoot $fullSaveRoot
    foreach ($descriptorName in @('baseline','working')) {
        $recorded = $Pair.$descriptorName
        $fresh = if ($descriptorName -ceq 'baseline') { $freshBaseline } else { $freshWorking }
        if ([string]$recorded.name -cne [string]$fresh.name -or
            [string]$recorded.fileName -cne [string]$fresh.fileName -or
            -not [string]::Equals([string]$recorded.path, [string]$fresh.path, [StringComparison]::OrdinalIgnoreCase) -or
            [string]$recorded.sha256 -cne [string]$fresh.sha256 -or
            [long]$recorded.length -ne [long]$fresh.length -or
            [long]$recorded.lastWriteTimeUtcTicks -ne [long]$fresh.lastWriteTimeUtcTicks -or
            [string]$recorded.gameName -cne [string]$fresh.gameName -or
            [string]$recorded.gameId -cne [string]$fresh.gameId -or
            [string]$recorded.area -cne [string]$fresh.area) {
            throw "KMC $descriptorName fixture changed after qualification and before transaction entry."
        }
    }

    $statePath = Get-KmcSaveTransactionStatePath $StateRoot $runId
    if (Test-Path -LiteralPath $statePath) { throw "Run ID already has save transaction state: $runId" }
    $backupParent = Join-Path ([IO.Path]::GetFullPath($BackupRoot)) 'save-transactions'
    $stagingParent = Join-Path ([IO.Path]::GetFullPath($StagingRoot)) 'save-transactions'
    $backupRun = Assert-KmcChildPath (Join-Path $backupParent $runId) $backupParent 'save transaction backup'
    $stagingRun = Assert-KmcChildPath (Join-Path $stagingParent $runId) $stagingParent 'save transaction staging'
    if ((Test-Path -LiteralPath $backupRun) -or (Test-Path -LiteralPath $stagingRun)) { throw "Run ID already has save backup or staging state: $runId" }
    New-Item -ItemType Directory -Path $backupRun -Force | Out-Null
    New-Item -ItemType Directory -Path $stagingRun -Force | Out-Null
    $backupPath = Join-Path $backupRun 'KMC_AUTOMATION_WORKING.original.zks'
    $artifactQuarantineRoot = Join-Path $stagingRun ('save-artifacts-' + [string]$Lock.Token)
    Copy-Item -LiteralPath $workingPath -Destination $backupPath
    Assert-KmcNotReparsePoint $backupPath 'frozen KMC working-save backup'
    Assert-KmcNotHardLink $backupPath 'frozen KMC working-save backup'
    $backupHash = Get-KmcSha256 $backupPath
    $backupLength = [long](Get-Item -LiteralPath $backupPath).Length
    if ($backupHash -cne [string]$freshWorking.sha256 -or $backupLength -ne [long]$freshWorking.length) {
        throw 'Frozen KMC working-save backup differs from the freshly revalidated fixture.'
    }
    $workingAfterCopy = Read-KmcFixtureHeader -Path $workingPath -Kind working -SaveRoot $fullSaveRoot
    $baselineAfterCopy = Read-KmcFixtureHeader -Path $baselinePath -Kind baseline -SaveRoot $fullSaveRoot
    if ([string]$workingAfterCopy.sha256 -cne [string]$freshWorking.sha256 -or
        [long]$workingAfterCopy.lastWriteTimeUtcTicks -ne [long]$freshWorking.lastWriteTimeUtcTicks -or
        [string]$baselineAfterCopy.sha256 -cne [string]$freshBaseline.sha256 -or
        [long]$baselineAfterCopy.lastWriteTimeUtcTicks -ne [long]$freshBaseline.lastWriteTimeUtcTicks) {
        throw 'A KMC fixture changed while the Working backup was being frozen.'
    }
    $beforeInventory = Get-KmcSaveMetadataInventory $fullSaveRoot
    $state = [ordered]@{
        schemaVersion = 2
        runId = $runId
        token = [string]$Lock.Token
        phase = 'prepared'
        preparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        scenario = $Scenario
        maxRuntimeArchiveWrites = $maximumWrites
        saveRoot = $fullSaveRoot
        baselinePath = $baselinePath
        baselineSha256 = [string]$freshBaseline.sha256
        baselineLength = [long]$freshBaseline.length
        baselineLastWriteTimeUtcTicks = [long]$freshBaseline.lastWriteTimeUtcTicks
        expectedGameName = [string]$freshWorking.gameName
        expectedGameId = [string]$freshWorking.gameId
        expectedArea = [string]$freshWorking.area
        workingPath = $workingPath
        workingSha256 = [string]$freshWorking.sha256
        workingLength = [long]$freshWorking.length
        workingLastWriteTimeUtcTicks = [long]$freshWorking.lastWriteTimeUtcTicks
        backupPath = $backupPath
        backupSha256 = $backupHash
        backupLength = $backupLength
        artifactQuarantineRoot = $artifactQuarantineRoot
        beforeInventory = $beforeInventory
    }
    Write-KmcJsonDurable $statePath $state
    return $statePath
}

function Restore-KmcWorkingSaveTransaction {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)][string]$StatePath,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot
    )
    [void](Assert-KmcRuntimeLockOwner $Lock)
    Assert-KmcNoGameProcesses
    $state = Read-KmcJson $StatePath
    $required = @(
        'schemaVersion','runId','token','phase','preparedAtUtc','scenario','maxRuntimeArchiveWrites','saveRoot',
        'baselinePath','baselineSha256','baselineLength','baselineLastWriteTimeUtcTicks',
        'expectedGameName','expectedGameId','expectedArea',
        'workingPath','workingSha256','workingLength','workingLastWriteTimeUtcTicks',
        'backupPath','backupSha256','backupLength','artifactQuarantineRoot','beforeInventory'
    )
    $recoveryFields = @(
        'restoreStartedAtUtc','runtimeInventoryDigest','workingDisposition','artifactPlan','artifactPlanDigest',
        'recoveryPlannedAtUtc','artifactsQuarantinedAtUtc','workingRestoredAtUtc','baselineImmutable',
        'saveWriteAllowlistPassed','restoredInventoryDigest','restoredAtUtc'
    )
    $allowed = @($required + $recoveryFields)
    $actual = @($state.PSObject.Properties.Name)
    if (@($required | Where-Object { $_ -cnotin $actual }).Count -ne 0 -or @($actual | Where-Object { $_ -cnotin $allowed }).Count -ne 0) {
        throw 'Save transaction state property set is missing required fields or contains unknown fields.'
    }
    if ([int]$state.schemaVersion -ne 2 -or [string]$state.runId -cne [string]$Lock.RunId -or [string]$state.token -cne [string]$Lock.Token) {
        throw 'Save transaction state ownership does not match the open lock.'
    }
    if ([string]$state.phase -cnotin @('prepared','recovery-planned','artifacts-quarantined','restored') -or
        [int]$state.maxRuntimeArchiveWrites -ne (Get-KmcRuntimeArchiveWriteLimit ([string]$state.scenario))) {
        throw 'Save transaction scenario, phase, or archive-write bound is invalid.'
    }
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    if (-not [string]::Equals($fullSaveRoot, [IO.Path]::GetFullPath([string]$state.saveRoot).TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Save transaction root does not match.'
    }
    $workingPath = Assert-KmcChildPath ([string]$state.workingPath) $fullSaveRoot 'recorded KMC working save'
    $baselinePath = Assert-KmcChildPath ([string]$state.baselinePath) $fullSaveRoot 'recorded KMC baseline save'
    $backupParent = Join-Path ([IO.Path]::GetFullPath($BackupRoot)) 'save-transactions'
    $stagingParent = Join-Path ([IO.Path]::GetFullPath($StagingRoot)) 'save-transactions'
    $backupPath = Assert-KmcChildPath ([string]$state.backupPath) $backupParent 'recorded working-save backup'
    $artifactRoot = Assert-KmcChildPath ([string]$state.artifactQuarantineRoot) $stagingParent 'recorded save-artifact quarantine root'
    Assert-KmcNotReparsePoint $baselinePath 'recorded KMC baseline save'
    Assert-KmcNotHardLink $baselinePath 'recorded KMC baseline save'
    if (Test-Path -LiteralPath $workingPath) {
        Assert-KmcNotReparsePoint $workingPath 'recorded KMC working save'
        Assert-KmcNotHardLink $workingPath 'recorded KMC working save'
    }
    Assert-KmcNotReparsePoint $backupPath 'recorded working-save backup'
    Assert-KmcNotHardLink $backupPath 'recorded working-save backup'
    Assert-KmcNotReparsePoint $artifactRoot 'recorded save-artifact quarantine root'

    $baselineImmutable = $false
    if (Test-Path -LiteralPath $baselinePath -PathType Leaf) {
        $baselineFile = Get-Item -LiteralPath $baselinePath
        $baselineImmutable = $baselineFile.Length -eq [long]$state.baselineLength -and
            $baselineFile.LastWriteTimeUtc.Ticks -eq [long]$state.baselineLastWriteTimeUtcTicks -and
            (Get-KmcSha256 $baselinePath) -ceq [string]$state.baselineSha256
    }
    $state | Add-Member -NotePropertyName baselineImmutable -NotePropertyValue $baselineImmutable -Force

    if (-not $baselineImmutable) {
        Write-KmcJsonDurable $StatePath $state
        throw 'KMC baseline immutability verification failed; no save artifact was moved and the baseline was not restored or overwritten.'
    }

    if (-not (Test-Path -LiteralPath $backupPath -PathType Leaf) -or
        (Get-KmcSha256 $backupPath) -cne [string]$state.backupSha256 -or
        (Get-Item -LiteralPath $backupPath).Length -ne [long]$state.backupLength -or
        [string]$state.backupSha256 -cne [string]$state.workingSha256 -or
        [long]$state.backupLength -ne [long]$state.workingLength) {
        Write-KmcJsonDurable $StatePath $state
        throw 'KMC working-save backup is missing or corrupt; live save state was not changed.'
    }

    $state = Initialize-KmcWorkingSaveRecoveryPlan -Lock $Lock -StatePath $StatePath -SaveRoot $fullSaveRoot -StagingRoot $StagingRoot
    $state | Add-Member -NotePropertyName baselineImmutable -NotePropertyValue $true -Force
    $plan = @($state.artifactPlan)
    if ((Get-KmcSaveArtifactPlanDigest $plan) -cne [string]$state.artifactPlanDigest) {
        throw 'Durable save-artifact recovery plan digest is invalid.'
    }
    $planCounts = @{ 'working-current'=0; 'dotnetzip-temp'=0; 'working-sidecar'=0 }
    foreach ($artifact in $plan) {
        if ([string]$artifact.kind -cnotin @($planCounts.Keys)) { throw 'Durable recovery plan contains an unknown artifact kind.' }
        $planCounts[[string]$artifact.kind]++
    }
    foreach ($boundedKind in @('dotnetzip-temp','working-sidecar')) {
        if ([int]$planCounts[$boundedKind] -gt [int]$state.maxRuntimeArchiveWrites) {
            throw "Durable recovery plan exceeds its recorded bound for $boundedKind."
        }
    }
    if ([int]$planCounts['working-current'] -gt 1 -or $plan.Count -gt (1 + (2 * [int]$state.maxRuntimeArchiveWrites))) {
        throw 'Durable recovery plan exceeds the Phase 1 slot-family artifact bound.'
    }
    $expectedArtifactRoot = Join-Path (Join-Path $stagingParent ([string]$state.runId)) ('save-artifacts-' + [string]$state.token)
    if (-not [string]::Equals($artifactRoot, [IO.Path]::GetFullPath($expectedArtifactRoot), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Save-artifact quarantine root is not owned by the exact run and token.'
    }

    # Re-check the complete direct-child inventory before moving anything. A retry
    # may legitimately find planned sources already moved and Working restored,
    # but every non-Working preflight entry must remain metadata-exact and every
    # new live entry must be named in the durable plan.
    $beforeMap = @{}; foreach ($entry in @($state.beforeInventory.entries)) { $beforeMap[[string]$entry.path] = $entry }
    $currentInventory = Get-KmcSaveMetadataInventory $fullSaveRoot
    $currentMap = @{}; foreach ($entry in @($currentInventory.entries)) { $currentMap[[string]$entry.path] = $entry }
    $plannedRelative = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($artifact in $plan) { [void]$plannedRelative.Add([string]$artifact.sourceRelativePath) }
    $workingRelative = [IO.Path]::GetFileName($workingPath)
    foreach ($beforeEntry in @($state.beforeInventory.entries)) {
        if ([string]::Equals([string]$beforeEntry.path, $workingRelative, [StringComparison]::OrdinalIgnoreCase)) { continue }
        if (-not (Test-KmcInventoryEntryExact $beforeEntry $currentMap[[string]$beforeEntry.path])) {
            throw "A protected save inventory entry drifted after recovery planning: $($beforeEntry.path)"
        }
    }
    foreach ($currentEntry in @($currentInventory.entries)) {
        if ($beforeMap.ContainsKey([string]$currentEntry.path)) { continue }
        if (-not $plannedRelative.Contains([string]$currentEntry.path)) {
            throw "An unplanned save inventory entry appeared after recovery planning: $($currentEntry.path)"
        }
    }
    if (-not (Test-Path -LiteralPath $artifactRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $artifactRoot | Out-Null
    }
    Assert-KmcNotReparsePoint $artifactRoot 'recorded save-artifact quarantine root'

    $seenSources = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $seenQuarantines = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($artifact in $plan) {
        Assert-KmcExactProperties $artifact @('kind','sourcePath','sourceRelativePath','quarantinePath','length','lastWriteTimeUtcTicks','sha256') 'save-artifact recovery entry'
        if ([string]$artifact.kind -cnotin @('working-current','dotnetzip-temp','working-sidecar') -or
            [string]$artifact.sha256 -cnotmatch '^[0-9a-f]{64}$' -or [long]$artifact.length -lt 0 -or [long]$artifact.length -gt 256MB) {
            throw 'Save-artifact recovery entry contains an invalid kind or fingerprint.'
        }
        $sourcePath = Assert-KmcChildPath ([string]$artifact.sourcePath) $fullSaveRoot 'planned runtime save artifact'
        $quarantinePath = Assert-KmcChildPath ([string]$artifact.quarantinePath) $artifactRoot 'planned runtime save-artifact quarantine'
        $expectedSourcePath = [IO.Path]::GetFullPath((Join-Path $fullSaveRoot ([string]$artifact.sourceRelativePath).Replace('/','\')))
        if (-not [string]::Equals($sourcePath, $expectedSourcePath, [StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals((Split-Path -Parent $sourcePath), $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not [string]::Equals((Split-Path -Parent $quarantinePath), $artifactRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not $seenSources.Add($sourcePath) -or -not $seenQuarantines.Add($quarantinePath)) {
            throw 'Save-artifact recovery plan contains a duplicate or non-direct-child path.'
        }
        $sourcePresent = Test-Path -LiteralPath $sourcePath -PathType Leaf
        $quarantinePresent = Test-Path -LiteralPath $quarantinePath -PathType Leaf
        if ($sourcePresent) {
            Assert-KmcNotReparsePoint $sourcePath 'planned runtime save artifact'
            Assert-KmcNotHardLink $sourcePath 'planned runtime save artifact'
        }
        if ($quarantinePresent) {
            Assert-KmcNotReparsePoint $quarantinePath 'quarantined runtime save artifact'
            Assert-KmcNotHardLink $quarantinePath 'quarantined runtime save artifact'
        }

        function Test-PlannedArtifact([string]$Path, $PlanEntry) {
            if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return $false }
            $file = Get-Item -LiteralPath $Path -Force
            return $file.Length -eq [long]$PlanEntry.length -and
                $file.LastWriteTimeUtc.Ticks -eq [long]$PlanEntry.lastWriteTimeUtcTicks -and
                (Get-KmcSha256 $Path) -ceq [string]$PlanEntry.sha256
        }

        if ($quarantinePresent -and -not (Test-PlannedArtifact $quarantinePath $artifact)) {
            throw "Quarantined runtime save artifact differs from its durable plan: $($artifact.sourceRelativePath)"
        }
        if ($sourcePresent -and $quarantinePresent) {
            $sourceIsRestoredWorking = [string]::Equals($sourcePath, $workingPath, [StringComparison]::OrdinalIgnoreCase) -and
                (Get-Item -LiteralPath $sourcePath).Length -eq [long]$state.workingLength -and
                (Get-KmcSha256 $sourcePath) -ceq [string]$state.workingSha256
            if (-not $sourceIsRestoredWorking) { throw "A planned runtime artifact exists both live and quarantined: $($artifact.sourceRelativePath)" }
            continue
        }
        if ($sourcePresent) {
            if (-not (Test-PlannedArtifact $sourcePath $artifact)) {
                throw "Runtime save artifact changed after its recovery plan was persisted: $($artifact.sourceRelativePath)"
            }
            Move-Item -LiteralPath $sourcePath -Destination $quarantinePath
            if (-not (Test-PlannedArtifact $quarantinePath $artifact)) {
                throw "Runtime save artifact quarantine verification failed: $($artifact.sourceRelativePath)"
            }
        }
        elseif (-not $quarantinePresent) {
            throw "A planned runtime save artifact is missing from both live and quarantine paths: $($artifact.sourceRelativePath)"
        }
    }
    $state.phase = 'artifacts-quarantined'
    $state | Add-Member -NotePropertyName artifactsQuarantinedAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
    Write-KmcJsonDurable $StatePath $state

    $workingIsOriginal = $false
    if (Test-Path -LiteralPath $workingPath -PathType Leaf) {
        Assert-KmcNotReparsePoint $workingPath 'restored KMC working save'
        Assert-KmcNotHardLink $workingPath 'restored KMC working save'
        $workingFile = Get-Item -LiteralPath $workingPath
        $workingIsOriginal = $workingFile.Length -eq [long]$state.workingLength -and (Get-KmcSha256 $workingPath) -ceq [string]$state.workingSha256
        if (-not $workingIsOriginal) { throw 'An unplanned live Working save remains after artifact quarantine.' }
    }
    if (-not $workingIsOriginal) { Copy-Item -LiteralPath $backupPath -Destination $workingPath }
    $workingFile = Get-Item -LiteralPath $workingPath
    if ($workingFile.LastWriteTimeUtc.Ticks -ne [long]$state.workingLastWriteTimeUtcTicks) {
        $workingFile.LastWriteTimeUtc = [DateTime]::new([long]$state.workingLastWriteTimeUtcTicks, [DateTimeKind]::Utc)
    }
    $restoredWorking = Get-Item -LiteralPath $workingPath
    if ($restoredWorking.Length -ne [long]$state.workingLength -or
        $restoredWorking.LastWriteTimeUtc.Ticks -ne [long]$state.workingLastWriteTimeUtcTicks -or
        (Get-KmcSha256 $workingPath) -cne [string]$state.workingSha256) {
        throw 'KMC working-save restoration verification failed.'
    }
    $state | Add-Member -NotePropertyName workingRestoredAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
    $restoredInventory = Get-KmcSaveMetadataInventory $fullSaveRoot
    $state | Add-Member -NotePropertyName restoredInventoryDigest -NotePropertyValue $restoredInventory.digest -Force
    if ($restoredInventory.digest -cne [string]$state.beforeInventory.digest) {
        Write-KmcJsonAtomic $StatePath $state
        throw 'Save-root metadata differs after exact Working restoration; protected state was not altered by recovery.'
    }
    $state.phase = 'restored'
    $state | Add-Member -NotePropertyName restoredAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
    Write-KmcJsonDurable $StatePath $state
    return [pscustomobject]@{
        schemaVersion = 1
        baselineImmutable = $true
        workingRestored = $true
        saveWriteAllowlistPassed = $true
        restoredInventoryDigest = $restoredInventory.digest
    }
}

function Assert-KmcNoGameProcesses {
    $kingmaker = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
    $wrath = @(Get-Process -Name Wrath -ErrorAction SilentlyContinue)
    $installers = @(Get-Process -Name UnityModManager -ErrorAction SilentlyContinue)
    if ($kingmaker.Count -ne 0 -or $wrath.Count -ne 0 -or $installers.Count -ne 0) {
        throw "Runtime state is ambiguous: Kingmaker=$($kingmaker.Count), Wrath=$($wrath.Count), UnityModManager=$($installers.Count)."
    }
}

function Wait-KmcStableNoKingmakerProcess {
    param(
        [ValidateRange(0, 2147483647)][int]$ExpectedProcessId = 0,
        [ValidateRange(1, 100)][int]$StableSamples = 8,
        [ValidateRange(1, 10000)][int]$IntervalMilliseconds = 250,
        [ValidateRange(1, 60)][int]$TimeoutSeconds = 10
    )
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($TimeoutSeconds)
    $stable = 0
    do {
        $remaining = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
        if ($remaining.Count -gt 1 -or ($remaining.Count -eq 1 -and ($ExpectedProcessId -eq 0 -or $remaining[0].Id -ne $ExpectedProcessId))) {
            throw 'An unexpected Kingmaker process appeared while waiting for a stable post-exit state.'
        }
        if (@(Get-KmcSuspiciousWindows).Count -ne 0) {
            throw 'Unexpected Steam/account UI appeared while waiting for a stable post-exit state.'
        }
        if ($remaining.Count -eq 0) { $stable++ } else { $stable = 0 }
        if ($stable -ge $StableSamples) { return $true }
        if ([DateTimeOffset]::UtcNow -ge $deadline) { return $false }
        Start-Sleep -Milliseconds $IntervalMilliseconds
    } while ($true)
}

function Assert-KmcChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $fullPath = [IO.Path]::GetFullPath($Path)
    $fullParent = [IO.Path]::GetFullPath($Parent).TrimEnd('\')
    if (-not $fullPath.StartsWith($fullParent + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description escaped its permitted root: $fullPath"
    }
    return $fullPath
}

function Assert-KmcNotReparsePoint {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Description)
    $current = [IO.Path]::GetFullPath($Path)
    while (-not (Test-Path -LiteralPath $current) -and (Split-Path -Parent $current) -ne $current) { $current = Split-Path -Parent $current }
    if ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw "$Description resolves through a reparse point: $current"
    }
}

function Get-KmcTransactionStatePath {
    param([Parameter(Mandatory = $true)][string]$StateRoot, [Parameter(Mandatory = $true)][string]$RunId)
    $transactionRoot = Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'transactions'
    return Assert-KmcChildPath (Join-Path $transactionRoot ($RunId + '.json')) $transactionRoot 'transaction state'
}

function Read-KmcOpenLockPayload {
    param([Parameter(Mandatory = $true)]$Lock)
    if ($null -eq $Lock.Stream -or -not $Lock.Stream.CanRead) { throw 'Runtime lock handle is not readable.' }
    $Lock.Stream.Flush($true)
    $Lock.Stream.Position = 0
    $bytes = New-Object byte[] ([int]$Lock.Stream.Length)
    [void]$Lock.Stream.Read($bytes, 0, $bytes.Length)
    $Lock.Stream.Position = $Lock.Stream.Length
    return ([Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json)
}

function Assert-KmcRuntimeLockOwner {
    param([Parameter(Mandatory = $true)]$Lock)
    $payload = Read-KmcOpenLockPayload $Lock
    Assert-KmcExactProperties $payload @('schemaVersion','runId','token','ownerProcessId','createdAtUtc') 'runtime lock'
    if ([int]$payload.schemaVersion -ne 1 -or [string]$payload.runId -cne [string]$Lock.RunId -or
        [string]$payload.token -cne [string]$Lock.Token -or [int]$payload.ownerProcessId -ne $PID) {
        throw 'Runtime lock ownership does not match the current harness process.'
    }
    return $payload
}

function Open-KmcRuntimeLock {
    param(
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId
    )
    $fullState = [IO.Path]::GetFullPath($StateRoot)
    if (-not (Test-Path -LiteralPath $fullState)) { New-Item -ItemType Directory -Path $fullState -Force | Out-Null }
    Assert-KmcNotReparsePoint $fullState 'runtime-state root'
    $lockPath = Join-Path $fullState 'active-transaction.lock'
    if (Test-Path -LiteralPath $lockPath) { throw "A runtime lock already exists and is stale or active: $lockPath" }
    $token = New-KmcRandomToken
    $payload = [ordered]@{ schemaVersion = 1; runId = $RunId; token = $token; ownerProcessId = $PID; createdAtUtc = [DateTime]::UtcNow.ToString('o') }
    $stream = New-Object IO.FileStream($lockPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Compress))
        $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true)
        $lock = [pscustomobject]@{ Path = $lockPath; Stream = $stream; RunId = $RunId; Token = $token }
        [void](Assert-KmcRuntimeLockOwner $lock)
        return $lock
    }
    catch { $stream.Dispose(); throw }
}

function Close-KmcRuntimeLock {
    param([Parameter(Mandatory = $true)]$Lock)
    [void](Assert-KmcRuntimeLockOwner $Lock)
    $path = [string]$Lock.Path
    $Lock.Stream.Dispose()
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
}

function Abandon-KmcRuntimeLock {
    param([Parameter(Mandatory = $true)]$Lock)
    [void](Assert-KmcRuntimeLockOwner $Lock)
    $Lock.Stream.Dispose()
}

function Adopt-KmcStaleRuntimeLock {
    param([Parameter(Mandatory = $true)][string]$StateRoot)
    Assert-KmcNoGameProcesses
    $lockPath=Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'active-transaction.lock'
    if(-not(Test-Path -LiteralPath $lockPath -PathType Leaf)){throw 'No stale KMC runtime lock exists.'}
    $stream=New-Object IO.FileStream($lockPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try{
        $probe=[pscustomobject]@{Path=$lockPath;Stream=$stream;RunId='';Token=''}
        $payload=Read-KmcOpenLockPayload $probe
        Assert-KmcExactProperties $payload @('schemaVersion','runId','token','ownerProcessId','createdAtUtc') 'stale runtime lock'
        if([int]$payload.schemaVersion-ne1-or[string]$payload.runId-notmatch'^[A-Za-z0-9._-]{1,120}$'-or[string]$payload.token-notmatch'^[0-9a-f]{64}$'){throw 'Stale runtime lock payload is invalid.'}
        if($null-ne(Get-Process -Id ([int]$payload.ownerProcessId) -ErrorAction SilentlyContinue)){throw 'Recorded runtime-lock owner process is still active.'}
        $payload.ownerProcessId=$PID
        $bytes=[Text.Encoding]::UTF8.GetBytes(($payload|ConvertTo-Json -Compress));$stream.SetLength(0);$stream.Position=0;$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)
        $lock=[pscustomobject]@{Path=$lockPath;Stream=$stream;RunId=[string]$payload.runId;Token=[string]$payload.token}
        [void](Assert-KmcRuntimeLockOwner $lock);return $lock
    }catch{$stream.Dispose();throw}
}

function Get-KmcSuspiciousWindows {
    return @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -in @('steam','steamwebhelper','Kingmaker') -and
        $_.MainWindowHandle -ne [IntPtr]::Zero -and
        $_.MainWindowTitle -match '(?i)login|steam guard|cloud conflict|purchase|update required|account|remote play'
    })
}

function Assert-KmcSteamSafety {
    param([Parameter(Mandatory = $true)][string]$SteamPath)
    Assert-KmcNoGameProcesses
    $fullSteam = [IO.Path]::GetFullPath($SteamPath)
    if (-not (Test-Path -LiteralPath $fullSteam -PathType Leaf)) { throw "Steam executable is missing: $fullSteam" }
    $clients = @(Get-Process -Name steam -ErrorAction SilentlyContinue)
    if ($clients.Count -ne 1) { throw 'Exactly one already-running Steam client is required.' }
    if (-not $clients[0].Path.Equals($fullSteam, [StringComparison]::OrdinalIgnoreCase)) { throw 'The running Steam executable path is unexpected.' }
    if (@(Get-KmcSuspiciousWindows).Count -ne 0) { throw 'Unexpected Steam/account UI is already visible.' }
    $steamRoot = Split-Path -Parent $fullSteam
    $connectionLog = Join-Path $steamRoot 'logs\connection_log.txt'
    $cloudLog = Join-Path $steamRoot 'logs\cloud_log.txt'
    $appManifest = Join-Path $steamRoot 'steamapps\appmanifest_640820.acf'
    foreach ($required in @($connectionLog,$cloudLog,$appManifest)) { if (-not (Test-Path -LiteralPath $required)) { throw "Steam safety evidence is missing: $required" } }
    function Read-OrderedSessionLines([string]$Path, [DateTime]$NotBefore) {
        $sequence = 0; $result = @()
        foreach ($line in @(Get-Content -LiteralPath $Path)) {
            $sequence++
            if ($line -match '^\[(?<stamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] (?<message>.*)$') {
                $stamp = [DateTime]::ParseExact($Matches.stamp,'yyyy-MM-dd HH:mm:ss',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::AssumeLocal)
                if ($stamp -ge $NotBefore) { $result += [pscustomobject]@{ stamp=$stamp; sequence=$sequence; message=[string]$Matches.message } }
            }
        }
        return @($result)
    }
    $connection = @(Read-OrderedSessionLines $connectionLog $clients[0].StartTime)
    $cloud = @(Read-OrderedSessionLines $cloudLog $clients[0].StartTime | Where-Object message -match '\[AppID 640820\]')
    $lastConnectionState = @($connection | Where-Object message -match '\[(Logged On|Logging On|Connected|Logged Off|Logging Off),' | Sort-Object stamp,sequence | Select-Object -Last 1)
    if ($lastConnectionState.Count -ne 1 -or $lastConnectionState[0].message -notmatch '\[(Logged Off|Logging Off),') { throw 'Steam Offline Mode is not the final observed current-session connection state.' }
    $lastCloudState = @($cloud | Where-Object message -match 'offlineMode=(true|false)' | Sort-Object stamp,sequence | Select-Object -Last 1)
    if ($lastCloudState.Count -ne 1 -or $lastCloudState[0].message -notmatch 'offlineMode=true') { throw 'App 640820 offline-cloud mode is not the final observed cloud state.' }
    $manifestText = Get-Content -Raw -LiteralPath $appManifest
    if ($manifestText -notmatch '"StateFlags"\s+"4"' -or $manifestText -notmatch '"buildid"\s+"6757524"') { throw 'Steam App 640820 is not the qualified fully installed build 6757524.' }
    return [pscustomobject]@{
        processId=$clients[0].Id; processStartedAtUtc=$clients[0].StartTime.ToUniversalTime().ToString('o')
        offlineAtUtc=$lastConnectionState[0].stamp.ToUniversalTime().ToString('o')
        offlineCloudAtUtc=$lastCloudState[0].stamp.ToUniversalTime().ToString('o')
        appManifestSha256=Get-KmcSha256 $appManifest
    }
}

function Assert-KmcPackageManifest {
    param([Parameter(Mandatory = $true)][string]$PackagePath, [Parameter(Mandatory = $true)][string]$ManifestPath)
    $repoRoot = Get-KmcRepositoryRoot; $labRoot = Get-KmcLabRoot
    $resolvedPackage = [IO.Path]::GetFullPath($PackagePath)
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $labRoot 'artifacts')).TrimEnd('\')
    if (-not $resolvedPackage.StartsWith($artifactsRoot + '\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Runtime package is outside the KMC artifacts root.' }
    $manifest = Read-KmcJson $ManifestPath
    Assert-KmcExactProperties $manifest @('schemaVersion','generator','generatedAtUtc','branch','commit','worktreeClean','qualificationEligible','version','packagePath','packageSha256','dllSha256','dllMvid','entries') 'package manifest'
    $head = (& git -C $repoRoot rev-parse HEAD).Trim(); $branch = (& git -C $repoRoot branch --show-current).Trim()
    $status = @(& git -C $repoRoot status --porcelain --untracked-files=all)
    if ($status.Count -ne 0) { throw 'Runtime qualification requires a clean Git worktree.' }
    if ([int]$manifest.schemaVersion -ne 2 -or [string]$manifest.generator -cne 'scripts/Package.ps1' -or
        $manifest.worktreeClean -ne $true -or $manifest.qualificationEligible -ne $true -or
        [string]$manifest.commit -cne $head -or [string]$manifest.branch -cne $branch -or
        [string]$manifest.packagePath -cne $resolvedPackage -or [string]$manifest.packageSha256 -cne (Get-KmcSha256 $resolvedPackage)) {
        throw 'Package manifest does not bind the exact clean branch, HEAD, and diagnostic ZIP.'
    }
    return $manifest
}

function Read-KmcLiveSentinel {
    param([Parameter(Mandatory = $true)][string]$LiveModsRoot)
    $path = Join-Path $LiveModsRoot '.kmc-runtime-sentinel.json'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    $value = Read-KmcJson $path
    Assert-KmcExactProperties $value @('schemaVersion','runId','token','packageSha256') 'live Mods sentinel'
    return $value
}

function Assert-KmcManifestMatchesState {
    param($Manifest, $State, [string]$Prefix)
    if ($Manifest.digest -cne [string]$State.($Prefix + 'Digest') -or
        $Manifest.fileCount -ne [int]$State.($Prefix + 'FileCount') -or
        $Manifest.directoryCount -ne [int]$State.($Prefix + 'DirectoryCount') -or
        $Manifest.totalBytes -ne [long]$State.($Prefix + 'TotalBytes')) {
        throw "$Prefix tree manifest differs from durable transaction state."
    }
}

function Enter-KmcModsTransaction {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)][string]$LiveModsRoot,
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot
    )
    [void](Assert-KmcRuntimeLockOwner $Lock); Assert-KmcNoGameProcesses
    $runId = [string]$Lock.RunId; $fullLive = [IO.Path]::GetFullPath($LiveModsRoot).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $fullLive -PathType Container)) { throw "Live Mods root is missing: $fullLive" }
    Assert-KmcNotReparsePoint $fullLive 'live Mods root'
    if ($null -ne (Read-KmcLiveSentinel $fullLive)) { throw 'Live Mods already contains a KMC transaction sentinel.' }
    Assert-KmcDirectoryTreeCloneable $fullLive 'live Mods tree'
    $kmcCollisions = @(Get-ChildItem -LiteralPath $fullLive -Force | Where-Object {
        [string]::Equals($_.Name, 'KingmakerMountedCombat', [StringComparison]::OrdinalIgnoreCase)
    })
    if ($kmcCollisions.Count -ne 0) { throw 'Live Mods already contains a case-insensitive KingmakerMountedCombat entry; overlay identity is ambiguous.' }
    $before = Get-KmcDirectoryManifest $fullLive
    $statePath = Get-KmcTransactionStatePath $StateRoot $runId
    if (Test-Path -LiteralPath $statePath) { throw "Run ID already has transaction state: $runId" }
    $backupRun = Assert-KmcChildPath (Join-Path ([IO.Path]::GetFullPath($BackupRoot)) $runId) $BackupRoot 'transaction backup'
    $stagingRun = Assert-KmcChildPath (Join-Path ([IO.Path]::GetFullPath($StagingRoot)) $runId) $StagingRoot 'transaction staging'
    if ((Test-Path -LiteralPath $backupRun) -or (Test-Path -LiteralPath $stagingRun)) { throw "Run ID already has backup or staging state: $runId" }
    New-Item -ItemType Directory -Path $backupRun | Out-Null; New-Item -ItemType Directory -Path $stagingRun | Out-Null
    $originalBackup = Join-Path $backupRun 'Mods-original'
    $ready = Join-Path $stagingRun 'Mods-ready'
    $stagedAfter = Join-Path $stagingRun 'Mods-staged-after'
    $packageOverlay = Join-Path $stagingRun 'package-overlay'
    $frozenPackage = Join-Path $stagingRun 'package.zip'
    $packageHash = Get-KmcSha256 $PackagePath
    Copy-Item -LiteralPath $PackagePath -Destination $frozenPackage
    if ((Get-KmcSha256 $frozenPackage) -cne $packageHash -or (Get-KmcSha256 $PackagePath) -cne $packageHash) {
        throw 'Frozen runtime package hash differs from the qualified package or the package changed while freezing.'
    }
    Expand-Archive -LiteralPath $frozenPackage -DestinationPath $packageOverlay
    Assert-KmcDirectoryTreeCloneable $packageOverlay 'frozen package overlay'
    $overlayManifest = Get-KmcDirectoryManifest $packageOverlay
    $actualOverlayEntries = @($overlayManifest.entries | ForEach-Object { '{0}|{1}' -f $_.kind, $_.path } | Sort-Object)
    $expectedOverlayEntries = @(
        'directory|KingmakerMountedCombat',
        'file|KingmakerMountedCombat/Info.json',
        'file|KingmakerMountedCombat/KingmakerMountedCombat.dll'
    ) | Sort-Object
    if (($actualOverlayEntries -join "`n") -cne ($expectedOverlayEntries -join "`n")) {
        throw "Frozen package overlay entry set is not exact: $($actualOverlayEntries -join ', ')"
    }
    $expectedRoot = Join-Path $packageOverlay 'KingmakerMountedCombat'
    if (-not (Test-Path -LiteralPath (Join-Path $expectedRoot 'Info.json')) -or -not (Test-Path -LiteralPath (Join-Path $expectedRoot 'KingmakerMountedCombat.dll'))) {
        throw 'Pre-staged package does not contain the exact KMC mod root.'
    }
    $cloneBase = Copy-KmcDirectoryTreeExact -SourceRoot $fullLive -DestinationRoot $ready
    Assert-KmcDirectoryManifestsEqual $before $cloneBase 'Pre-overlay live Mods clone'
    Assert-KmcDirectoryTreeCloneable $fullLive 'live Mods tree after cloning'
    Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $fullLive) 'Live Mods source after cloning'
    $stagedKmcRoot = Join-Path $ready 'KingmakerMountedCombat'
    if (Test-Path -LiteralPath $stagedKmcRoot) { throw 'Pre-overlay clone unexpectedly contains a KingmakerMountedCombat collision.' }
    Move-Item -LiteralPath $expectedRoot -Destination $stagedKmcRoot
    $sentinel = [ordered]@{ schemaVersion=1; runId=$runId; token=[string]$Lock.Token; packageSha256=$packageHash }
    Write-KmcJsonAtomic (Join-Path $ready '.kmc-runtime-sentinel.json') $sentinel
    $staged = Get-KmcDirectoryManifest $ready
    Assert-KmcDirectoryTreeCloneable $fullLive 'live Mods tree before activation'
    Assert-KmcDirectoryManifestsEqual $before (Get-KmcDirectoryManifest $fullLive) 'Live Mods source immediately before activation'
    $state = [ordered]@{
        schemaVersion=3; runId=$runId; token=[string]$Lock.Token; phase='prepared'; preparedAtUtc=[DateTime]::UtcNow.ToString('o')
        stagingMode='live-clone-plus-kmc-overlay'
        liveModsRoot=$fullLive; originalBackup=$originalBackup; stagedReady=$ready; stagedAfter=$stagedAfter
        frozenPackage=$frozenPackage; packageSha256=$packageHash
        beforeDigest=$before.digest; beforeFileCount=$before.fileCount; beforeDirectoryCount=$before.directoryCount; beforeTotalBytes=$before.totalBytes
        cloneBaseDigest=$cloneBase.digest; cloneBaseFileCount=$cloneBase.fileCount; cloneBaseDirectoryCount=$cloneBase.directoryCount; cloneBaseTotalBytes=$cloneBase.totalBytes
        stagedDigest=$staged.digest; stagedFileCount=$staged.fileCount; stagedDirectoryCount=$staged.directoryCount; stagedTotalBytes=$staged.totalBytes
    }
    Write-KmcJsonAtomic $statePath $state
    try {
        Move-Item -LiteralPath $fullLive -Destination $originalBackup
        $state.phase='original-moved'; Write-KmcJsonAtomic $statePath $state
        Assert-KmcManifestMatchesState (Get-KmcDirectoryManifest $originalBackup) $state 'before'
        Move-Item -LiteralPath $ready -Destination $fullLive
        $liveSentinel = Read-KmcLiveSentinel $fullLive
        if ($null -eq $liveSentinel -or [string]$liveSentinel.runId -cne $runId -or [string]$liveSentinel.token -cne [string]$Lock.Token) { throw 'Activated live Mods sentinel does not prove transaction ownership.' }
        Assert-KmcManifestMatchesState (Get-KmcDirectoryManifest $fullLive) $state 'staged'
        $state.phase='staged'; $state['stagedAtUtc']=[DateTime]::UtcNow.ToString('o'); Write-KmcJsonAtomic $statePath $state
        return $statePath
    }
    catch {
        $entryException = $_.Exception
        try { Restore-KmcModsTransaction -Lock $Lock -StatePath $statePath -LiveModsRoot $fullLive -BackupRoot $BackupRoot -StagingRoot $StagingRoot | Out-Null }
        catch {
            throw "Mods transaction entry failed ($($entryException.Message)) and guarded rollback failed ($($_.Exception.Message))."
        }
        throw $entryException
    }
}

function Restore-KmcModsTransaction {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)][string]$StatePath,
        [Parameter(Mandatory = $true)][string]$LiveModsRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot
    )
    [void](Assert-KmcRuntimeLockOwner $Lock); Assert-KmcNoGameProcesses
    $state = Read-KmcJson $StatePath
    $requiredState = @('schemaVersion','runId','token','phase','preparedAtUtc','liveModsRoot','originalBackup','stagedReady','stagedAfter','frozenPackage','packageSha256','beforeDigest','beforeFileCount','beforeDirectoryCount','beforeTotalBytes','stagedDigest','stagedFileCount','stagedDirectoryCount','stagedTotalBytes')
    $schemaVersion = [int]$state.schemaVersion
    if ($schemaVersion -eq 3) {
        $requiredState = @($requiredState + @('stagingMode','cloneBaseDigest','cloneBaseFileCount','cloneBaseDirectoryCount','cloneBaseTotalBytes'))
    }
    elseif ($schemaVersion -ne 2) { throw 'Transaction state schema is neither historical schema 2 nor current schema 3.' }
    $allowedState = @($requiredState + @('stagedAtUtc','stagedAfterDigest','stagedAfterFileCount','stagedAfterDirectoryCount','stagedAfterTotalBytes','stagedTreeChangedAtRuntime','restoredAtUtc','restoredDigest'))
    $actualState = @($state.PSObject.Properties.Name)
    if (@($requiredState | Where-Object { $_ -cnotin $actualState }).Count -ne 0 -or @($actualState | Where-Object { $_ -cnotin $allowedState }).Count -ne 0) {
        throw 'Transaction state property set is missing required fields or contains unknown fields.'
    }
    if ([string]$state.runId -cne [string]$Lock.RunId -or [string]$state.token -cne [string]$Lock.Token) { throw 'Transaction state ownership does not match the open lock.' }
    if ($schemaVersion -eq 3 -and ([string]$state.stagingMode -cne 'live-clone-plus-kmc-overlay' -or
        [string]$state.cloneBaseDigest -cne [string]$state.beforeDigest -or
        [int]$state.cloneBaseFileCount -ne [int]$state.beforeFileCount -or
        [int]$state.cloneBaseDirectoryCount -ne [int]$state.beforeDirectoryCount -or
        [long]$state.cloneBaseTotalBytes -ne [long]$state.beforeTotalBytes)) {
        throw 'Schema-3 transaction state does not prove an exact pre-overlay clone of live Mods.'
    }
    $fullLive=[IO.Path]::GetFullPath($LiveModsRoot).TrimEnd('\')
    if (-not $fullLive.Equals([IO.Path]::GetFullPath([string]$state.liveModsRoot).TrimEnd('\'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Transaction live Mods path does not match.' }
    $backup=Assert-KmcChildPath ([string]$state.originalBackup) $BackupRoot 'recorded transaction backup'
    $stagedAfter=Assert-KmcChildPath ([string]$state.stagedAfter) $StagingRoot 'recorded staged quarantine'
    if ([string]$state.phase -ceq 'restored') {
        $restored=Get-KmcDirectoryManifest $fullLive; Assert-KmcManifestMatchesState $restored $state 'before'; return $restored
    }
    if (Test-Path -LiteralPath $backup -PathType Container) { Assert-KmcManifestMatchesState (Get-KmcDirectoryManifest $backup) $state 'before' }
    else {
        if (Test-Path -LiteralPath $fullLive -PathType Container) {
            $current=Get-KmcDirectoryManifest $fullLive
            if ($current.digest -ceq [string]$state.beforeDigest) {
                $state.phase='restored'; $state | Add-Member -NotePropertyName restoredAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force; $state | Add-Member -NotePropertyName restoredDigest -NotePropertyValue $current.digest -Force; Write-KmcJsonAtomic $StatePath $state; return $current
            }
        }
        throw 'Original Mods backup is absent and live Mods is not the proven original tree.'
    }
    if (Test-Path -LiteralPath $fullLive -PathType Container) {
        $sentinel=Read-KmcLiveSentinel $fullLive
        if ($null -eq $sentinel -or [string]$sentinel.runId -cne [string]$Lock.RunId -or [string]$sentinel.token -cne [string]$Lock.Token) { throw 'Live Mods is occupied by an unknown tree; restoration refused.' }
        if (Test-Path -LiteralPath $stagedAfter) { throw 'Owned staged-after quarantine already exists; restoration is ambiguous.' }
        $runtimeStaged=Get-KmcDirectoryManifest $fullLive
        $state | Add-Member -NotePropertyName stagedAfterDigest -NotePropertyValue $runtimeStaged.digest -Force
        $state | Add-Member -NotePropertyName stagedAfterFileCount -NotePropertyValue $runtimeStaged.fileCount -Force
        $state | Add-Member -NotePropertyName stagedAfterDirectoryCount -NotePropertyValue $runtimeStaged.directoryCount -Force
        $state | Add-Member -NotePropertyName stagedAfterTotalBytes -NotePropertyValue $runtimeStaged.totalBytes -Force
        $state | Add-Member -NotePropertyName stagedTreeChangedAtRuntime -NotePropertyValue ($runtimeStaged.digest -cne [string]$state.stagedDigest) -Force
        Write-KmcJsonAtomic $StatePath $state
        Move-Item -LiteralPath $fullLive -Destination $stagedAfter
    }
    Move-Item -LiteralPath $backup -Destination $fullLive
    $restored=Get-KmcDirectoryManifest $fullLive; Assert-KmcManifestMatchesState $restored $state 'before'
    $state.phase='restored'; $state | Add-Member -NotePropertyName restoredAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force; $state | Add-Member -NotePropertyName restoredDigest -NotePropertyValue $restored.digest -Force; Write-KmcJsonAtomic $StatePath $state
    return $restored
}

function Get-KmcSaveBackedRuntimeScenarios {
    return @(
        'export-mounted-contracts', 'export-candidate-mount-rigs', 'observe-mount-diagnostic-availability',
        'mounted-pair-create-and-clear', 'mounted-pair-double-mount-rejected', 'mounted-pair-invalid-pair-rejected',
        'mounted-pair-cleanup-idempotent', 'mounted-pair-death-cleanup', 'mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup', 'mounted-pair-mod-disable-cleanup', 'mounted-pair-open-ground',
        'mounted-pair-stop-start', 'mounted-pair-turns-and-corners', 'mounted-pair-doorway', 'mounted-pair-selection',
        'mounted-pair-party-formation', 'mounted-pair-pause-unpause', 'mounted-pair-destination-cancel',
        'mounted-pair-turn-based-entry-cleanup', 'mounted-pair-realtime-entry-cleanup', 'mounted-pair-save-safety',
        'mounted-pair-load-safety', 'mounted-pair-area-transition-safety', 'fixture-intake', 'lifecycle-suite',
        'movement-suite', 'boundary-suite'
    )
}

function Get-KmcLifecycleRuntimeRows {
    return @(
        'mounted-pair-create-and-clear',
        'mounted-pair-double-mount-rejected',
        'mounted-pair-invalid-pair-rejected',
        'mounted-pair-cleanup-idempotent',
        'mounted-pair-death-cleanup',
        'mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup',
        'mounted-pair-mod-disable-cleanup'
    )
}

function Get-KmcLifecycleExpectedCleanupTrigger {
    param([Parameter(Mandatory = $true)][string]$Row)
    switch -CaseSensitive ($Row) {
        'mounted-pair-death-cleanup' { return 'Death' }
        'mounted-pair-combat-start-cleanup' { return 'CombatStarted' }
        'mounted-pair-area-unload-cleanup' { return 'AreaUnloading' }
        'mounted-pair-mod-disable-cleanup' { return 'ModDisabled' }
        default { return 'Manual' }
    }
}

function Get-KmcLifecycleInvocationPath {
    param([Parameter(Mandatory = $true)][string]$Row)
    if ([string]$Row -cin @(
        'mounted-pair-death-cleanup',
        'mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup')) {
        return 'lifecycle-handler-direct'
    }
    return 'relationship-service-direct'
}

function Test-KmcLifecycleRuntimeScenario {
    param([AllowNull()][string]$Scenario)
    return [string]$Scenario -ceq 'lifecycle-suite' -or
        @(Get-KmcLifecycleRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1
}

function Get-KmcMovementRuntimeRows {
    # This order is part of the evidence contract. The doorway row must retain
    # its nearby unmounted control before any prior row moves the fixture away.
    return @(
        'mounted-pair-doorway',
        'mounted-pair-open-ground',
        'mounted-pair-stop-start',
        'mounted-pair-turns-and-corners',
        'mounted-pair-selection',
        'mounted-pair-party-formation',
        'mounted-pair-pause-unpause',
        'mounted-pair-destination-cancel'
    )
}

function Test-KmcMovementRuntimeScenario {
    param([AllowNull()][string]$Scenario)
    return [string]$Scenario -ceq 'movement-suite' -or
        @(Get-KmcMovementRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1
}

function Test-KmcExactJsonInteger {
    param($Value)
    return $Value -is [sbyte] -or $Value -is [byte] -or
        $Value -is [int16] -or $Value -is [uint16] -or
        $Value -is [int32] -or $Value -is [uint32] -or
        $Value -is [int64] -or $Value -is [uint64]
}

function Test-KmcJsonNumber {
    param($Value)
    return (Test-KmcExactJsonInteger $Value) -or $Value -is [single] -or
        $Value -is [double] -or $Value -is [decimal]
}

function Assert-KmcNullableJsonBoolean {
    param($Value, [Parameter(Mandatory = $true)][string]$Description)
    if ($null -ne $Value -and $Value -isnot [bool]) { throw "$Description must be a JSON boolean or null." }
}

function Assert-KmcJsonStringArray {
    param($Value, [Parameter(Mandatory = $true)][string]$Description)
    if ($Value -isnot [Array]) { throw "$Description must be an actual JSON array." }
    foreach ($item in @($Value)) {
        if ($item -isnot [string]) { throw "$Description must contain only JSON strings." }
    }
}

function Assert-KmcLifecyclePosition {
    param($Value, [Parameter(Mandatory = $true)][string]$Description, [switch]$AllowNull)
    if ($null -eq $Value) {
        if ($AllowNull) { return }
        throw "$Description is required."
    }
    Assert-KmcExactProperties $Value @('x','y','z') $Description
    foreach ($name in @('x','y','z')) {
        if (-not (Test-KmcJsonNumber $Value.$name)) { throw "$Description.$name must be a JSON number." }
    }
}

function Assert-KmcLifecycleRotation {
    param($Value, [Parameter(Mandatory = $true)][string]$Description, [switch]$AllowNull)
    if ($null -eq $Value) {
        if ($AllowNull) { return }
        throw "$Description is required."
    }
    Assert-KmcExactProperties $Value @('x','y','z','w') $Description
    foreach ($name in @('x','y','z','w')) {
        if (-not (Test-KmcJsonNumber $Value.$name)) { throw "$Description.$name must be a JSON number." }
    }
}

function Assert-KmcLifecycleUnitEvidence {
    param($Value, [Parameter(Mandatory = $true)][string]$Description, [switch]$RequireComplete)
    if ($null -eq $Value) {
        if ($RequireComplete) { throw "$Description is required for a PASS lifecycle record." }
        return
    }
    Assert-KmcExactProperties $Value @(
        'uniqueId','sizeOrdinal','inCombat','stockAgentEnabled','avoidanceDisabled','forbidRotation','agentOverrideType',
        'overrideComponentCount','entityPosition','entityRotationDegrees','viewPosition','viewRotation',
        'moveCommandType','moveTarget','activeCommandTypes','selected') $Description
    if ($Value.uniqueId -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Value.uniqueId)) {
        throw "$Description.uniqueId must be a nonempty JSON string."
    }
    if (-not (Test-KmcExactJsonInteger $Value.sizeOrdinal)) { throw "$Description.sizeOrdinal must be an exact JSON integer." }
    foreach ($name in @('inCombat','stockAgentEnabled','avoidanceDisabled','forbidRotation','selected')) {
        Assert-KmcNullableJsonBoolean $Value.$name "$Description.$name"
    }
    foreach ($name in @('agentOverrideType','moveCommandType')) {
        if ($null -ne $Value.$name -and $Value.$name -isnot [string]) { throw "$Description.$name must be a JSON string or null." }
    }
    if ($null -ne $Value.overrideComponentCount -and -not (Test-KmcExactJsonInteger $Value.overrideComponentCount)) {
        throw "$Description.overrideComponentCount must be an exact JSON integer or null."
    }
    if (-not (Test-KmcJsonNumber $Value.entityRotationDegrees)) { throw "$Description.entityRotationDegrees must be a JSON number." }
    Assert-KmcLifecyclePosition $Value.entityPosition "$Description.entityPosition"
    Assert-KmcLifecyclePosition $Value.viewPosition "$Description.viewPosition" -AllowNull
    Assert-KmcLifecycleRotation $Value.viewRotation "$Description.viewRotation" -AllowNull
    Assert-KmcLifecyclePosition $Value.moveTarget "$Description.moveTarget" -AllowNull
    Assert-KmcJsonStringArray $Value.activeCommandTypes "$Description.activeCommandTypes"
}

function Assert-KmcLifecycleEvidenceRecord {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$ExpectedSequence,
        [Parameter(Mandatory = $true)][string[]]$ExpectedRows,
        [Parameter(Mandatory = $true)][bool]$RequireComplete
    )
    Assert-KmcExactProperties $Record @(
        'schemaVersion','runId','scenario','row','phase','utcTimestamp','branch','commit','productVersion',
        'dllSha256','dllMvid','sequence','frame','relationshipState','triggerScope','rowStatus','assertionPassCount',
        'assertionFailCount','cleanup','partyCombat','riderCombat','mountCombat','turnBased','paused',
        'currentGameMode','rider','mount','selection','spine','anchor','attachment','recordErrors') 'lifecycle evidence record'
    if (-not (Test-KmcExactJsonInteger $Record.schemaVersion) -or [long]$Record.schemaVersion -ne 2) {
        throw 'Lifecycle evidence schemaVersion must be the exact integral value 2.'
    }
    foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if ($Record.$name -isnot [string] -or [string]$Record.$name -cne [string]$Request.$name) {
            throw "Lifecycle evidence identity mismatch: $name"
        }
    }
    if ([string]$Record.dllSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Lifecycle evidence DLL SHA-256 is not exact lowercase hexadecimal.' }
    if ([string]$Record.dllMvid -cnotmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') { throw 'Lifecycle evidence DLL MVID is not exact lowercase GUID text.' }
    if (-not (Test-KmcExactJsonInteger $Record.sequence) -or [long]$Record.sequence -ne $ExpectedSequence) {
        throw "Lifecycle evidence sequence is not contiguous at $ExpectedSequence."
    }
    if (-not (Test-KmcExactJsonInteger $Record.frame) -or [long]$Record.frame -lt 0) { throw 'Lifecycle evidence frame must be a nonnegative exact integer.' }
    if ($Record.row -isnot [string] -or @($ExpectedRows | Where-Object { $_ -ceq [string]$Record.row }).Count -ne 1) { throw "Lifecycle evidence row is outside the exact scenario row set: $($Record.row)" }
    if ($Record.phase -isnot [string] -or [string]$Record.phase -cnotin @('pre-mount','mounted-next-frame','cleanup-next-frame','row-finish','engine-finalization')) { throw "Lifecycle evidence phase is invalid: $($Record.phase)" }
    if ($Record.utcTimestamp -isnot [string]) { throw 'Lifecycle evidence UTC timestamp must be a JSON string.' }
    $timestamp = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$Record.utcTimestamp, [ref]$timestamp) -or $timestamp.Offset -ne [TimeSpan]::Zero) { throw 'Lifecycle evidence UTC timestamp is invalid or not UTC.' }
    if ($Record.relationshipState -isnot [string] -or [string]$Record.relationshipState -cnotin @('Unmounted','Validating','Mounting','Mounted','Dismounting','Faulted','Disposed')) { throw 'Lifecycle evidence relationshipState is invalid.' }

    if ($null -eq $Record.triggerScope) { throw 'Lifecycle evidence triggerScope object is required.' }
    Assert-KmcExactProperties $Record.triggerScope @('expectedCleanupTrigger','invocationPath','nativeDeliveryObserved','claimLimit') 'lifecycle evidence triggerScope'
    $expectedTrigger = Get-KmcLifecycleExpectedCleanupTrigger ([string]$Record.row)
    $expectedInvocationPath = Get-KmcLifecycleInvocationPath ([string]$Record.row)
    if ($Record.triggerScope.expectedCleanupTrigger -isnot [string] -or [string]$Record.triggerScope.expectedCleanupTrigger -cne $expectedTrigger -or
        $Record.triggerScope.invocationPath -isnot [string] -or [string]$Record.triggerScope.invocationPath -cne $expectedInvocationPath -or
        $Record.triggerScope.nativeDeliveryObserved -isnot [bool] -or $Record.triggerScope.nativeDeliveryObserved -ne $false -or
        $Record.triggerScope.claimLimit -isnot [string] -or
        [string]$Record.triggerScope.claimLimit -cne 'Direct service/handler invocation only; native EventBus/UMM delivery was not exercised.') {
        throw "Lifecycle evidence trigger scope or truthful native-delivery claim is wrong for $($Record.row)."
    }
    foreach ($name in @('partyCombat','riderCombat','mountCombat','turnBased','paused')) { Assert-KmcNullableJsonBoolean $Record.$name "lifecycle evidence $name" }
    if ($null -ne $Record.currentGameMode -and $Record.currentGameMode -isnot [string]) { throw 'Lifecycle evidence currentGameMode must be a JSON string or null.' }

    if ($null -eq $Record.cleanup) { throw 'Lifecycle evidence cleanup object is required.' }
    Assert-KmcExactProperties $Record.cleanup @('trigger','result','succeeded','state','movementAuthorityResidual','presentationResidual','errors') 'lifecycle evidence cleanup'
    Assert-KmcJsonStringArray $Record.cleanup.errors 'lifecycle evidence cleanup.errors'
    if ($null -eq $Record.cleanup.result) {
        if ($null -ne $Record.cleanup.trigger -or $null -ne $Record.cleanup.succeeded -or $null -ne $Record.cleanup.state -or
            $null -ne $Record.cleanup.movementAuthorityResidual -or $null -ne $Record.cleanup.presentationResidual -or
            @($Record.cleanup.errors).Count -ne 0) { throw 'Lifecycle evidence null cleanup result contains non-null transition state.' }
    }
    else {
        if ($Record.cleanup.result -isnot [string] -or [string]$Record.cleanup.result -cnotin @('PASS','FAIL') -or
            $Record.cleanup.trigger -isnot [string] -or $Record.cleanup.succeeded -isnot [bool] -or
            $Record.cleanup.state -isnot [string] -or $Record.cleanup.movementAuthorityResidual -isnot [bool] -or
            $Record.cleanup.presentationResidual -isnot [bool]) { throw 'Lifecycle evidence cleanup result has invalid primitive types.' }
        $isCleanUnmounted = $Record.cleanup.succeeded -eq $true -and [string]$Record.cleanup.state -ceq 'Unmounted' -and
            $Record.cleanup.movementAuthorityResidual -eq $false -and $Record.cleanup.presentationResidual -eq $false
        if (([string]$Record.cleanup.result -ceq 'PASS') -ne $isCleanUnmounted) { throw 'Lifecycle evidence cleanup result does not exactly represent successful residue-free Unmounted cleanup.' }
    }

    if ([string]$Record.phase -ceq 'row-finish') {
        if ($Record.rowStatus -isnot [string] -or [string]$Record.rowStatus -cnotin @('PASS','FAIL') -or
            -not (Test-KmcExactJsonInteger $Record.assertionPassCount) -or
            -not (Test-KmcExactJsonInteger $Record.assertionFailCount) -or
            [long]$Record.assertionPassCount -lt 0 -or [long]$Record.assertionFailCount -lt 0) { throw 'Lifecycle row-finish status or assertion totals are invalid.' }
    }
    elseif ($null -ne $Record.rowStatus -or $null -ne $Record.assertionPassCount -or $null -ne $Record.assertionFailCount) {
        throw 'Lifecycle non-row-finish record contains row result fields.'
    }
    Assert-KmcJsonStringArray $Record.recordErrors 'lifecycle evidence recordErrors'
    Assert-KmcLifecycleUnitEvidence $Record.rider 'lifecycle evidence rider' -RequireComplete:$RequireComplete
    Assert-KmcLifecycleUnitEvidence $Record.mount 'lifecycle evidence mount' -RequireComplete:$RequireComplete

    if ($null -eq $Record.selection) { throw 'Lifecycle evidence selection object is required.' }
    Assert-KmcExactProperties $Record.selection @('available','riderSelected','mountSelected','selectedUnitIds') 'lifecycle evidence selection'
    if ($Record.selection.available -isnot [bool]) { throw 'Lifecycle evidence selection.available must be a JSON boolean.' }
    Assert-KmcNullableJsonBoolean $Record.selection.riderSelected 'lifecycle evidence selection.riderSelected'
    Assert-KmcNullableJsonBoolean $Record.selection.mountSelected 'lifecycle evidence selection.mountSelected'
    Assert-KmcJsonStringArray $Record.selection.selectedUnitIds 'lifecycle evidence selection.selectedUnitIds'

    if ($null -ne $Record.spine) {
        Assert-KmcExactProperties $Record.spine @('name','worldPosition','worldRotation') 'lifecycle evidence Spine'
        if ($Record.spine.name -isnot [string]) { throw 'Lifecycle evidence Spine name must be a JSON string.' }
        Assert-KmcLifecyclePosition $Record.spine.worldPosition 'lifecycle evidence Spine worldPosition'
        Assert-KmcLifecycleRotation $Record.spine.worldRotation 'lifecycle evidence Spine worldRotation'
    }
    elseif ($RequireComplete) { throw 'Lifecycle PASS evidence does not contain the Spine world transform.' }

    if ($null -eq $Record.anchor) { throw 'Lifecycle evidence anchor object is required.' }
    Assert-KmcExactProperties $Record.anchor @('name','expectedPosition','expectedRotation','currentPositionResidualWorldUnits','currentRotationResidualDegrees','preCorrectionPositionResidualWorldUnits','preCorrectionRotationResidualDegrees','postCorrectionPositionResidualWorldUnits','postCorrectionRotationResidualDegrees') 'lifecycle evidence anchor'
    if ($null -ne $Record.anchor.name -and $Record.anchor.name -isnot [string]) { throw 'Lifecycle evidence anchor name must be a JSON string or null.' }
    Assert-KmcLifecyclePosition $Record.anchor.expectedPosition 'lifecycle evidence anchor.expectedPosition' -AllowNull
    Assert-KmcLifecycleRotation $Record.anchor.expectedRotation 'lifecycle evidence anchor.expectedRotation' -AllowNull
    foreach ($name in @('currentPositionResidualWorldUnits','currentRotationResidualDegrees','preCorrectionPositionResidualWorldUnits','preCorrectionRotationResidualDegrees','postCorrectionPositionResidualWorldUnits','postCorrectionRotationResidualDegrees')) {
        if ($null -ne $Record.anchor.$name -and -not (Test-KmcJsonNumber $Record.anchor.$name)) { throw "Lifecycle evidence anchor.$name must be a JSON number or null." }
    }

    if ($null -eq $Record.attachment) { throw 'Lifecycle evidence attachment object is required.' }
    Assert-KmcExactProperties $Record.attachment @(
        'leaseContract','leaseActive','restoreVerified','residue','riderParentMatchesAttachment',
        'currentRiderParent','originalRiderParent','riderParentMatchesOriginal','currentRiderSiblingIndex',
        'originalRiderSiblingIndex','riderSiblingIndexMatchesOriginal','currentRiderLocalScale',
        'originalRiderLocalScale','riderLocalScaleMatchesOriginal','attachmentParent','sourceAnchor','riskState') 'lifecycle evidence attachment'
    if ($Record.attachment.leaseContract -isnot [string] -or
        [string]$Record.attachment.leaseContract -cne 'parent+sibling+world-position+world-rotation+local-scale') {
        throw 'Lifecycle attachment evidence does not name the exact scoped lease contract.'
    }
    foreach ($name in @('leaseActive','restoreVerified','residue','riderParentMatchesAttachment','riderParentMatchesOriginal',
        'riderSiblingIndexMatchesOriginal','riderLocalScaleMatchesOriginal')) {
        if ($Record.attachment.$name -isnot [bool]) { throw "Lifecycle evidence attachment.$name must be a JSON boolean." }
    }
    foreach ($name in @('currentRiderParent','originalRiderParent','attachmentParent','sourceAnchor','riskState')) {
        if ($null -ne $Record.attachment.$name -and $Record.attachment.$name -isnot [string]) {
            throw "Lifecycle evidence attachment.$name must be a JSON string or null."
        }
    }
    foreach ($name in @('currentRiderSiblingIndex','originalRiderSiblingIndex')) {
        if ($null -ne $Record.attachment.$name -and -not (Test-KmcExactJsonInteger $Record.attachment.$name)) {
            throw "Lifecycle evidence attachment.$name must be an exact JSON integer or null."
        }
    }
    Assert-KmcLifecyclePosition $Record.attachment.currentRiderLocalScale 'lifecycle evidence attachment.currentRiderLocalScale' -AllowNull
    Assert-KmcLifecyclePosition $Record.attachment.originalRiderLocalScale 'lifecycle evidence attachment.originalRiderLocalScale' -AllowNull
}

function Assert-KmcLifecycleCleanupAbsent {
    param([Parameter(Mandatory = $true)]$Record)
    if ($null -ne $Record.cleanup.result -or $null -ne $Record.cleanup.trigger) {
        throw "Lifecycle $($Record.row)/$($Record.phase) unexpectedly contains a cleanup transition."
    }
}

function Assert-KmcLifecycleCleanupExact {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)][string]$ExpectedTrigger
    )
    if ([string]$Record.cleanup.trigger -cne $ExpectedTrigger -or [string]$Record.cleanup.result -cne 'PASS' -or
        $Record.cleanup.succeeded -ne $true -or [string]$Record.cleanup.state -cne 'Unmounted' -or
        $Record.cleanup.movementAuthorityResidual -ne $false -or $Record.cleanup.presentationResidual -ne $false -or
        @($Record.cleanup.errors).Count -ne 0) {
        throw "Lifecycle $($Record.row)/$($Record.phase) does not prove exact $ExpectedTrigger residue-free cleanup."
    }
}

function Assert-KmcLifecycleBaselineUnitState {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if (-not (Test-KmcExactJsonInteger $Record.rider.overrideComponentCount) -or
        -not (Test-KmcExactJsonInteger $Record.mount.overrideComponentCount) -or
        $Record.rider.stockAgentEnabled -ne $true -or $Record.mount.stockAgentEnabled -ne $true -or
        $Record.rider.avoidanceDisabled -ne $false -or $Record.mount.avoidanceDisabled -ne $false -or
        $Record.rider.forbidRotation -ne $false -or $Record.mount.forbidRotation -ne $false -or
        $null -ne $Record.rider.agentOverrideType -or $null -ne $Record.mount.agentOverrideType -or
        [long]$Record.rider.overrideComponentCount -ne 0 -or [long]$Record.mount.overrideComponentCount -ne 0) {
        throw "$Description does not expose the exact stock-agent/avoidance/override/component baseline."
    }
}

function Assert-KmcLifecycleMountedUnitState {
    param([Parameter(Mandatory = $true)]$Record)
    if (-not (Test-KmcExactJsonInteger $Record.rider.overrideComponentCount) -or
        -not (Test-KmcExactJsonInteger $Record.mount.overrideComponentCount) -or
        $Record.rider.stockAgentEnabled -ne $false -or $Record.mount.stockAgentEnabled -ne $true -or
        $Record.rider.avoidanceDisabled -ne $true -or $Record.mount.avoidanceDisabled -ne $false -or
        $Record.rider.forbidRotation -ne $true -or $Record.mount.forbidRotation -ne $false -or
        [string]$Record.rider.agentOverrideType -cne 'KingmakerMountedCombat.Integration.RiderMovementAgent' -or
        $null -ne $Record.mount.agentOverrideType -or [long]$Record.rider.overrideComponentCount -ne 1 -or
        [long]$Record.mount.overrideComponentCount -ne 0) {
        throw "Lifecycle mounted-next-frame does not expose exactly one rider override and one authoritative stock mount agent for $($Record.row)."
    }
}

function Assert-KmcLifecycleAttachmentBaseline {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)][string]$Description,
        [switch]$RequireVerifiedRestore
    )
    $attachment = $Record.attachment
    if (-not (Test-KmcExactJsonInteger $attachment.currentRiderSiblingIndex) -or
        -not (Test-KmcExactJsonInteger $attachment.originalRiderSiblingIndex) -or
        $null -eq $attachment.currentRiderLocalScale -or $null -eq $attachment.originalRiderLocalScale -or
        $attachment.leaseActive -ne $false -or $attachment.residue -ne $false -or
        $attachment.riderParentMatchesAttachment -ne $false -or $attachment.riderParentMatchesOriginal -ne $true -or
        $attachment.riderSiblingIndexMatchesOriginal -ne $true -or $attachment.riderLocalScaleMatchesOriginal -ne $true -or
        $null -ne $attachment.attachmentParent -or $null -ne $attachment.sourceAnchor -or
        [string]$attachment.riskState -cne 'none' -or
        [string]$attachment.currentRiderParent -cne [string]$attachment.originalRiderParent -or
        [long]$attachment.currentRiderSiblingIndex -ne [long]$attachment.originalRiderSiblingIndex) {
        throw "$Description does not prove an inactive, residue-free rider attachment at its captured parent."
    }
    if ($RequireVerifiedRestore -and $attachment.restoreVerified -ne $true) {
        throw "$Description does not carry the scoped attachment lease's verified restore result."
    }
    foreach ($axis in @('x','y','z')) {
        if ([math]::Abs([double]$attachment.currentRiderLocalScale.$axis - [double]$attachment.originalRiderLocalScale.$axis) -gt 0.0001) {
            throw "$Description current and captured rider local scale differ."
        }
    }
}

function Assert-KmcLifecycleAttachmentMounted {
    param([Parameter(Mandatory = $true)]$Record)
    $attachment = $Record.attachment
    if ($attachment.leaseActive -ne $true -or $attachment.restoreVerified -ne $false -or
        $attachment.residue -ne $true -or $attachment.riderParentMatchesAttachment -ne $true -or
        $attachment.riderParentMatchesOriginal -ne $false -or
        [string]$attachment.attachmentParent -cne 'KMC_RiderPositionAnchor' -or
        [string]$attachment.sourceAnchor -cne 'Spine' -or
        [string]$attachment.riskState -cne 'active and internally consistent' -or
        [string]$attachment.currentRiderParent -cnotmatch '(^|/)KMC_RiderPositionAnchor$') {
        throw "Lifecycle mounted-next-frame does not prove the exact active scoped rider attachment for $($Record.row)."
    }
}

function Assert-KmcLifecycleEvidenceSemantics {
    param(
        [Parameter(Mandatory = $true)][object[]]$Records,
        [Parameter(Mandatory = $true)][string[]]$ExpectedRows
    )
    $nonFinalRecords = @($Records | Where-Object { [string]$_.phase -cne 'engine-finalization' })
    $stableRiderId = $null
    $stableMountId = $null
    foreach ($row in $ExpectedRows) {
        $rowRecords = @($nonFinalRecords | Where-Object { [string]$_.row -ceq $row })
        [string[]]$expectedPhases = if ($row -ceq 'mounted-pair-invalid-pair-rejected') {
            @('pre-mount','cleanup-next-frame','row-finish')
        } else {
            @('pre-mount','mounted-next-frame','cleanup-next-frame','row-finish')
        }
        [string[]]$actualPhases = @($rowRecords | ForEach-Object { [string]$_.phase })
        if (($actualPhases -join '|') -cne ($expectedPhases -join '|')) {
            throw "PASS lifecycle evidence phase set/order is not exact for $row."
        }

        $pre = $rowRecords[0]
        $cleanup = $rowRecords[$rowRecords.Count - 2]
        $finish = $rowRecords[$rowRecords.Count - 1]
        $mounted = if ($row -ceq 'mounted-pair-invalid-pair-rejected') { $null } else { $rowRecords[1] }
        if ([string]$pre.relationshipState -cne 'Unmounted' -or [string]$cleanup.relationshipState -cne 'Unmounted' -or
            [string]$finish.relationshipState -cne 'Unmounted') {
            throw "Lifecycle relationship-state progression is not Unmounted -> cleanup Unmounted for $row."
        }
        if ($null -ne $mounted -and [string]$mounted.relationshipState -cne 'Mounted') {
            throw "Lifecycle mounted-next-frame relationship state is not Mounted for $row."
        }
        if ([long]$cleanup.frame -le [long]$pre.frame) { throw "Lifecycle cleanup was not observed on a later frame for $row." }
        if ($null -ne $mounted -and ([long]$mounted.frame -le [long]$pre.frame -or [long]$cleanup.frame -le [long]$mounted.frame)) {
            throw "Lifecycle mount/cleanup frame progression is not strictly later for $row."
        }
        if ($row -ceq 'mounted-pair-cleanup-idempotent') {
            if ([long]$finish.frame -le [long]$cleanup.frame) { throw 'Idempotent cleanup row did not observe its repeated cleanup on a second later frame.' }
        }
        elseif ([long]$finish.frame -ne [long]$cleanup.frame) {
            throw "Lifecycle row-finish was not atomically recorded with cleanup-next-frame for $row."
        }

        $expectedTrigger = Get-KmcLifecycleExpectedCleanupTrigger $row
        Assert-KmcLifecycleCleanupAbsent $pre
        if ($null -ne $mounted) { Assert-KmcLifecycleCleanupAbsent $mounted }
        Assert-KmcLifecycleCleanupExact $cleanup $expectedTrigger
        Assert-KmcLifecycleCleanupExact $finish $expectedTrigger
        if ([string]$finish.rowStatus -cne 'PASS' -or [long]$finish.assertionFailCount -ne 0 -or
            [long]$finish.assertionPassCount -le 0 -or @($finish.recordErrors).Count -ne 0) {
            throw "Lifecycle row-finish is not an error-free PASS for $row."
        }

        foreach ($record in $rowRecords) {
            if ($record.partyCombat -ne $false -or $record.riderCombat -ne $false -or $record.mountCombat -ne $false -or
                $record.turnBased -ne $false -or $record.paused -ne $false -or [string]$record.currentGameMode -cne 'Default') {
                throw "Lifecycle direct-call row crossed an unclaimed combat, turn-based, pause, or game-mode boundary: $row/$($record.phase)."
            }
            if ([string]$record.rider.uniqueId -ceq [string]$record.mount.uniqueId) { throw "Lifecycle rider and mount IDs are not distinct for $row." }
            if ($null -eq $stableRiderId) {
                $stableRiderId = [string]$record.rider.uniqueId
                $stableMountId = [string]$record.mount.uniqueId
            }
            elseif ([string]$record.rider.uniqueId -cne $stableRiderId -or [string]$record.mount.uniqueId -cne $stableMountId) {
                throw 'Lifecycle evidence changed exact rider or mount identity across phases/rows.'
            }
        }

        Assert-KmcLifecycleBaselineUnitState $pre "$row pre-mount"
        Assert-KmcLifecycleAttachmentBaseline $pre "$row pre-mount"
        if ($null -ne $mounted) {
            Assert-KmcLifecycleMountedUnitState $mounted
            Assert-KmcLifecycleAttachmentMounted $mounted
            Assert-KmcLifecycleBaselineUnitState $cleanup "$row cleanup-next-frame"
            Assert-KmcLifecycleAttachmentBaseline $cleanup "$row cleanup-next-frame" -RequireVerifiedRestore
            Assert-KmcLifecycleBaselineUnitState $finish "$row row-finish"
            Assert-KmcLifecycleAttachmentBaseline $finish "$row row-finish" -RequireVerifiedRestore
        }
        else {
            Assert-KmcLifecycleBaselineUnitState $cleanup "$row cleanup-next-frame"
            Assert-KmcLifecycleAttachmentBaseline $cleanup "$row cleanup-next-frame"
            Assert-KmcLifecycleBaselineUnitState $finish "$row row-finish"
            Assert-KmcLifecycleAttachmentBaseline $finish "$row row-finish"
        }
    }

    $final = $Records[$Records.Count - 1]
    $finalRow = $ExpectedRows[$ExpectedRows.Count - 1]
    $finalFinish = @($Records | Where-Object { [string]$_.row -ceq $finalRow -and [string]$_.phase -ceq 'row-finish' })[0]
    if ([long]$final.frame -le [long]$finalFinish.frame -or [string]$final.relationshipState -cne 'Unmounted' -or
        @($final.recordErrors).Count -ne 0) {
        throw 'Lifecycle engine-finalization was not recorded later in residue-free Unmounted state.'
    }
    $finalTrigger = Get-KmcLifecycleExpectedCleanupTrigger $finalRow
    Assert-KmcLifecycleCleanupExact $final $finalTrigger
    Assert-KmcLifecycleBaselineUnitState $final 'lifecycle engine-finalization'
    if ($finalRow -ceq 'mounted-pair-invalid-pair-rejected') {
        Assert-KmcLifecycleAttachmentBaseline $final 'lifecycle engine-finalization'
    }
    else {
        Assert-KmcLifecycleAttachmentBaseline $final 'lifecycle engine-finalization' -RequireVerifiedRestore
    }
}

function Assert-KmcKnownRuntimeArtifactsManifested {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceRoot,
        [Parameter(Mandatory = $true)]$Manifest
    )
    $manifested = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($artifact in @($Manifest.artifacts)) { [void]$manifested.Add([string]$artifact.relativePath) }
    foreach ($leaf in @('lifecycle-scenario-evidence.jsonl','movement-telemetry.jsonl','movement-scenario-evidence.jsonl')) {
        if ((Test-Path -LiteralPath (Join-Path $EvidenceRoot $leaf) -PathType Leaf) -and -not $manifested.Contains($leaf)) {
            throw "Known runtime artifact exists without a manifest record: $leaf"
        }
    }
    $visualRoot = Join-Path $EvidenceRoot 'movement-visuals'
    if (Test-Path -LiteralPath $visualRoot -PathType Container) {
        foreach ($path in @(Get-ChildItem -LiteralPath $visualRoot -File -Force)) {
            $relative = 'movement-visuals/' + $path.Name
            if (-not $manifested.Contains($relative)) { throw "Known runtime artifact exists without a manifest record: $relative" }
        }
    }
}

function Assert-KmcLifecycleScenarioEvidence {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$Manifest,
        [AllowNull()][string]$Status,
        $SubscenarioResults
    )
    $evidenceRoot = [IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')
    Assert-KmcKnownRuntimeArtifactsManifested $evidenceRoot $Manifest
    $isLifecycle = Test-KmcLifecycleRuntimeScenario ([string]$Request.scenario)
    $lifecycleRecords = @($Manifest.artifacts | Where-Object { [string]$_.relativePath -ceq 'lifecycle-scenario-evidence.jsonl' })
    $requireComplete = $isLifecycle -and [string]$Status -ceq 'PASS'
    if ($requireComplete -and $lifecycleRecords.Count -ne 1) { throw 'PASS lifecycle scenario requires exactly one manifested lifecycle JSONL artifact.' }
    if ($lifecycleRecords.Count -eq 0) { return }
    if ($lifecycleRecords.Count -ne 1 -or [string]$lifecycleRecords[0].kind -cne 'scenario-evidence') { throw 'Lifecycle JSONL manifest identity is not exact.' }
    if (-not $isLifecycle) { throw 'Lifecycle JSONL is present for a non-lifecycle runtime scenario.' }

    $allRows = @(Get-KmcLifecycleRuntimeRows)
    [string[]]$expectedRows = if ([string]$Request.scenario -ceq 'lifecycle-suite') { @($allRows) } else { @([string]$Request.scenario) }
    $path = Assert-KmcChildPath (Join-Path $evidenceRoot 'lifecycle-scenario-evidence.jsonl') $evidenceRoot 'lifecycle scenario evidence'
    Assert-KmcNotReparsePoint $path 'lifecycle scenario evidence'
    Assert-KmcNotHardLink $path 'lifecycle scenario evidence'
    $lines = @([IO.File]::ReadAllLines($path, (New-Object Text.UTF8Encoding($false, $true))))
    $records = New-Object 'Collections.Generic.List[object]'
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        Assert-KmcJsonObjectMembersUnique $line 'lifecycle scenario evidence line'
        try { $record = $line | ConvertFrom-Json }
        catch { throw "Lifecycle scenario evidence line is malformed JSON: $($_.Exception.Message)" }
        Assert-KmcLifecycleEvidenceRecord $record $Request $records.Count $expectedRows $requireComplete
        $records.Add($record)
    }
    if ($records.Count -eq 0) { throw 'Lifecycle scenario evidence contains no nonblank JSON records.' }

    $lastRowIndex = -1
    $lastFrame = -1
    $lastPhaseByRow = @{}
    $phasesByRow = @{}
    $engineFinalizationCount = 0
    for ($index = 0; $index -lt $records.Count; $index++) {
        $record = $records[$index]
        if ([long]$record.frame -lt $lastFrame) { throw 'Lifecycle evidence frame order regressed.' }
        $lastFrame = [long]$record.frame
        $rowIndex = [Array]::IndexOf([string[]]$expectedRows, [string]$record.row)
        if ($rowIndex -lt $lastRowIndex) { throw 'Lifecycle scenario evidence row order regressed.' }
        $lastRowIndex = $rowIndex
        if ([string]$record.phase -ceq 'engine-finalization') {
            $engineFinalizationCount++
            if ($index -ne $records.Count - 1) { throw 'Lifecycle engine-finalization must be the final JSONL record.' }
            continue
        }
        $phaseOrder = [Array]::IndexOf(@('pre-mount','mounted-next-frame','cleanup-next-frame','row-finish'), [string]$record.phase)
        $prior = if ($lastPhaseByRow.ContainsKey([string]$record.row)) { [int]$lastPhaseByRow[[string]$record.row] } else { -1 }
        if ($phaseOrder -le $prior) { throw "Lifecycle evidence phase order or uniqueness failed for row $($record.row)." }
        $lastPhaseByRow[[string]$record.row] = $phaseOrder
        if (-not $phasesByRow.ContainsKey([string]$record.row)) { $phasesByRow[[string]$record.row] = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal) }
        [void]$phasesByRow[[string]$record.row].Add([string]$record.phase)
    }

    if ($requireComplete) {
        if ($engineFinalizationCount -ne 1 -or [string]$records[$records.Count - 1].row -cne [string]$expectedRows[$expectedRows.Count - 1]) { throw 'PASS lifecycle evidence lacks one final engine-finalization bound to the final expected row.' }
        foreach ($row in $expectedRows) {
            if (-not $phasesByRow.ContainsKey($row) -or -not $phasesByRow[$row].Contains('pre-mount') -or -not $phasesByRow[$row].Contains('row-finish')) { throw "PASS lifecycle evidence lacks pre-mount or row-finish coverage for $row." }
            if ($row -cne 'mounted-pair-invalid-pair-rejected' -and
                (-not $phasesByRow[$row].Contains('mounted-next-frame') -or -not $phasesByRow[$row].Contains('cleanup-next-frame'))) { throw "PASS lifecycle evidence lacks mounted-next-frame or cleanup-next-frame coverage for $row." }
        }
        Assert-KmcLifecycleEvidenceSemantics -Records $records.ToArray() -ExpectedRows $expectedRows
    }

    if ($null -ne $SubscenarioResults) {
        $subresults = @($SubscenarioResults)
        if ($requireComplete) {
            if ($subresults.Count -ne $expectedRows.Count) { throw 'PASS lifecycle subresult count does not match the exact selected row set.' }
            for ($subresultIndex = 0; $subresultIndex -lt $expectedRows.Count; $subresultIndex++) {
                if ([string]$subresults[$subresultIndex].name -cne [string]$expectedRows[$subresultIndex]) {
                    throw 'PASS lifecycle subresults do not preserve the exact selected row order.'
                }
            }
        }
        foreach ($record in @($records | Where-Object { [string]$_.phase -ceq 'row-finish' })) {
            $matches = @($subresults | Where-Object { [string]$_.name -ceq [string]$record.row })
            if ($matches.Count -ne 1) { throw "Lifecycle row-finish does not map to exactly one game subresult: $($record.row)" }
            $subresult = $matches[0]
            if ([string]$record.rowStatus -cne [string]$subresult.status -or
                [long]$record.assertionPassCount -ne [long]$subresult.assertionPassCount -or
                [long]$record.assertionFailCount -ne [long]$subresult.assertionFailCount -or
                (@($record.recordErrors) -join "`n") -cne (@($subresult.errors) -join "`n")) { throw "Lifecycle row-finish result does not reconcile with the game subresult: $($record.row)" }
        }
    }
}

function Test-KmcFiniteNonnegativeJsonNumber {
    param($Value)
    if (-not (Test-KmcJsonNumber $Value)) { return $false }
    $number = [double]$Value
    return -not [double]::IsNaN($number) -and -not [double]::IsInfinity($number) -and $number -ge 0.0
}

function Test-KmcFiniteJsonNumber {
    param($Value)
    if (-not (Test-KmcJsonNumber $Value)) { return $false }
    $number = [double]$Value
    return -not [double]::IsNaN($number) -and -not [double]::IsInfinity($number)
}

function Test-KmcApproximatelyEqual {
    param(
        [Parameter(Mandatory = $true)][double]$Left,
        [Parameter(Mandatory = $true)][double]$Right,
        [double]$Tolerance = 0.000000001
    )
    return [math]::Abs($Left - $Right) -le $Tolerance
}

function Assert-KmcMovementVector3 {
    param($Value, [Parameter(Mandatory = $true)][string]$Description, [switch]$AllowNull)
    if ($null -eq $Value) {
        if ($AllowNull) { return }
        throw "$Description is required."
    }
    Assert-KmcExactProperties $Value @('x','y','z') $Description
    foreach ($name in @('x','y','z')) {
        if (-not (Test-KmcJsonNumber $Value.$name)) { throw "$Description.$name must be a JSON number." }
    }
}

function Assert-KmcMovementCommonIdentity {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$ExpectedSequence,
        [Parameter(Mandatory = $true)][string[]]$ExpectedRows,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if (-not (Test-KmcExactJsonInteger $Record.schemaVersion) -or [long]$Record.schemaVersion -ne 1) {
        throw "$Description schemaVersion must be the exact integral value 1."
    }
    foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if ($Record.$name -isnot [string] -or [string]$Record.$name -cne [string]$Request.$name) {
            throw "$Description identity mismatch: $name"
        }
    }
    if ([string]$Record.dllSha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$Record.dllMvid -cnotmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') {
        throw "$Description DLL identity format is invalid."
    }
    if (-not (Test-KmcExactJsonInteger $Record.sequence) -or [long]$Record.sequence -ne $ExpectedSequence) {
        throw "$Description sequence is not contiguous at $ExpectedSequence."
    }
    if ($Record.row -isnot [string] -or @($ExpectedRows | Where-Object { $_ -ceq [string]$Record.row }).Count -ne 1) {
        throw "$Description row is outside the exact scenario row set: $($Record.row)"
    }
    if ($Record.utcTimestamp -isnot [string]) { throw "$Description utcTimestamp must be a JSON string." }
    $timestamp = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$Record.utcTimestamp, [ref]$timestamp) -or $timestamp.Offset -ne [TimeSpan]::Zero) {
        throw "$Description utcTimestamp is invalid or not UTC."
    }
}

function Assert-KmcMovementCleanupState {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Description,
        [Parameter(Mandatory = $true)][ValidateSet('before','after')][string]$Phase,
        [Parameter(Mandatory = $true)][bool]$RequireComplete
    )
    Assert-KmcExactProperties $Value @(
        'trigger','relationshipState','hasMountedResidual','riderStockAgentEnabled','mountStockAgentEnabled',
        'riderAvoidanceDisabled','mountAvoidanceDisabled','riderOverridePresent','mountOverridePresent',
        'riderSelected','mountSelected','selectedUnitIds','paused','riderForbidRotation','attachmentLeaseActive','attachmentRestoreVerified',
        'attachmentResidue','riderParentMatchesAttachment','riderParent','attachmentParent','sourceAnchor','attachmentRiskState') $Description
    if ($Value.trigger -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Value.trigger)) { throw "$Description.trigger must be a nonempty JSON string." }
    if ($Value.relationshipState -isnot [string] -or [string]$Value.relationshipState -cnotin @('Unmounted','Validating','Mounting','Mounted','Dismounting','Faulted','Disposed')) {
        throw "$Description.relationshipState is invalid."
    }
    foreach ($name in @('hasMountedResidual','riderOverridePresent','mountOverridePresent','riderSelected','mountSelected',
        'attachmentLeaseActive','attachmentRestoreVerified','attachmentResidue','riderParentMatchesAttachment')) {
        if ($Value.$name -isnot [bool]) { throw "$Description.$name must be a JSON boolean." }
    }
    foreach ($name in @('riderStockAgentEnabled','mountStockAgentEnabled','riderAvoidanceDisabled','mountAvoidanceDisabled','paused','riderForbidRotation')) {
        Assert-KmcNullableJsonBoolean $Value.$name "$Description.$name"
    }
    Assert-KmcJsonStringArray $Value.selectedUnitIds "$Description.selectedUnitIds"
    foreach ($name in @('riderParent','attachmentParent','sourceAnchor','attachmentRiskState')) {
        if ($null -ne $Value.$name -and $Value.$name -isnot [string]) { throw "$Description.$name must be a JSON string or null." }
    }
    if ($RequireComplete -and $Phase -ceq 'before') {
        if ([string]$Value.relationshipState -cne 'Mounted' -or $Value.hasMountedResidual -ne $true -or
            $Value.riderStockAgentEnabled -ne $false -or $Value.mountStockAgentEnabled -ne $true -or
            $Value.riderAvoidanceDisabled -ne $true -or $Value.riderOverridePresent -ne $true -or
            $Value.mountOverridePresent -ne $false -or $Value.attachmentLeaseActive -ne $true -or
            $Value.riderForbidRotation -ne $true -or
            $Value.attachmentRestoreVerified -ne $false -or $Value.attachmentResidue -ne $true -or
            $Value.riderParentMatchesAttachment -ne $true -or [string]$Value.attachmentParent -cne 'KMC_RiderPositionAnchor' -or
            [string]$Value.sourceAnchor -cne 'Spine' -or [string]$Value.attachmentRiskState -cne 'active and internally consistent') {
            throw 'PASS movement cleanup-before evidence does not prove the exact mounted movement-authority lease.'
        }
    }
    if ($RequireComplete -and $Phase -ceq 'after') {
        if ([string]$Value.relationshipState -cne 'Unmounted' -or $Value.hasMountedResidual -ne $false -or
            $Value.riderOverridePresent -ne $false -or $Value.mountOverridePresent -ne $false -or
            $Value.riderForbidRotation -ne $false -or
            $Value.attachmentLeaseActive -ne $false -or $Value.attachmentRestoreVerified -ne $true -or
            $Value.attachmentResidue -ne $false -or $Value.riderParentMatchesAttachment -ne $false -or
            $null -ne $Value.attachmentParent -or $null -ne $Value.sourceAnchor -or
            [string]$Value.attachmentRiskState -cne 'none') {
            throw 'PASS movement cleanup-after evidence does not prove residue-free Unmounted cleanup.'
        }
    }
}

function Assert-KmcLatestPositionPhaseSemantics {
    param([Parameter(Mandatory = $true)]$Record)

    $required = @(
        'latestSynchronizationFrame','latestAuthoritativePositionSequence','latestCurrentAuthoritativeAnchorX',
        'latestCurrentAuthoritativeAnchorY','latestCurrentAuthoritativeAnchorZ','latestPreviousAuthoritativePositionReferenceKind',
        'latestPreviousAuthoritativePositionSameFrame','latestPreviousAuthoritativePositionReferenceEligible',
        'latestAuthoritativePositionDeltaWorldUnits','latestViewCurrentPositionResidualWorldUnits',
        'latestEntityRawCurrentPositionResidualWorldUnits','latestEntityPhaseAdjustedPositionResidualWorldUnits',
        'latestEntityRawPositionLagBoundWorldUnits','latestEntityRawPositionLagExcessWorldUnits',
        'latestPositionPhaseLagObserved','latestPositionPhaseLagPermitted','latestPositionPhaseLagViolation',
        'latestPositionRecoveryRequiredBeforeSample','latestPositionRecoveryUpdateObserved',
        'latestPositionRecoverySatisfied','latestPositionRecoveryViolation','latestPositionRecoveryPendingAfterSample',
        'latestPositionStationaryAuthority','latestStationaryPositionCorrectionViolation')
    foreach ($name in $required) {
        if ($null -eq $Record.$name) { throw "PASS movement telemetry latest position phase-order field $name is null." }
    }

    $phase = [string]$Record.synchronizationPhase
    if ($phase -cnotin @('InitialConfiguration','Update','LateUpdate')) {
        throw 'PASS movement telemetry synchronizationPhase is invalid.'
    }
    $calibrated = $phase -ceq 'Update' -or $phase -ceq 'LateUpdate'
    $rawCurrent = [double]$Record.latestEntityRawCurrentPositionResidualWorldUnits
    $viewCurrent = [double]$Record.latestViewCurrentPositionResidualWorldUnits
    $authoritativeDelta = [double]$Record.latestAuthoritativePositionDeltaWorldUnits

    $previousNames = @(
        'latestPreviousAuthoritativePositionSequence','latestPreviousAuthoritativeAnchorX',
        'latestPreviousAuthoritativeAnchorY','latestPreviousAuthoritativeAnchorZ',
        'latestPreviousAuthoritativePositionFrame','latestPreviousAuthoritativePositionPhase',
        'latestEntityPreviousAuthoritativePositionResidualWorldUnits')
    $previousPresent = @($previousNames | Where-Object { $null -ne $Record.$_ }).Count
    if ($previousPresent -ne 0 -and $previousPresent -ne $previousNames.Count) {
        throw 'PASS movement telemetry previous position-authority reference fields are only partially populated.'
    }
    $hasPrevious = $previousPresent -eq $previousNames.Count
    if ($phase -cne 'LateUpdate' -and $hasPrevious) {
        throw 'PASS movement telemetry exposes a previous position Update reference outside LateUpdate.'
    }

    $authorityAge = $null
    if (-not $hasPrevious) {
        if ([string]$Record.latestPreviousAuthoritativePositionReferenceKind -cne 'none' -or
            $Record.latestPreviousAuthoritativePositionSameFrame -ne $false -or
            $Record.latestPreviousAuthoritativePositionReferenceEligible -ne $false) {
            throw 'PASS movement telemetry null previous position-authority fields have inconsistent reference flags.'
        }
    }
    else {
        if ([string]$Record.latestPreviousAuthoritativePositionPhase -cne 'Update') {
            throw 'PASS movement telemetry previous position-authority reference is not a LateUpdate-to-Update reference.'
        }
        $authorityAge = [long]$Record.latestAuthoritativePositionSequence - [long]$Record.latestPreviousAuthoritativePositionSequence
        if ($authorityAge -lt 0) {
            throw 'PASS movement telemetry previous position-authority sequence is newer than the current sequence.'
        }
        $sameFrame = [long]$Record.latestPreviousAuthoritativePositionFrame -eq [long]$Record.latestSynchronizationFrame
        $eligible = $sameFrame -and $authorityAge -eq 1L
        $expectedKind = if ($sameFrame) { 'same-frame-update' } else { 'prior-frame-update' }
        if ($Record.latestPreviousAuthoritativePositionSameFrame -ne $sameFrame -or
            $Record.latestPreviousAuthoritativePositionReferenceEligible -ne $eligible -or
            [string]$Record.latestPreviousAuthoritativePositionReferenceKind -cne $expectedKind) {
            throw 'PASS movement telemetry previous position-authority kind, frame, eligibility, or sequence age is inconsistent.'
        }
        if ($sameFrame) {
            $dx = [double]$Record.latestCurrentAuthoritativeAnchorX - [double]$Record.latestPreviousAuthoritativeAnchorX
            $dy = [double]$Record.latestCurrentAuthoritativeAnchorY - [double]$Record.latestPreviousAuthoritativeAnchorY
            $dz = [double]$Record.latestCurrentAuthoritativeAnchorZ - [double]$Record.latestPreviousAuthoritativeAnchorZ
            $threeDimensionalAdvance = [math]::Sqrt(($dx * $dx) + ($dy * $dy) + ($dz * $dz))
            if (-not (Test-KmcApproximatelyEqual $authoritativeDelta $threeDimensionalAdvance)) {
                throw 'PASS movement telemetry authoritative position delta is not the 3-D displacement from its same-frame Update anchor.'
            }
        }
    }

    $previousResidual = if ($hasPrevious) { [double]$Record.latestEntityPreviousAuthoritativePositionResidualWorldUnits } else { $null }
    $expectedObserved = $calibrated -and $rawCurrent -gt 0.10
    $expectedPermitted = $expectedObserved -and $phase -ceq 'LateUpdate' -and $hasPrevious -and
        $Record.latestPreviousAuthoritativePositionReferenceEligible -eq $true -and $authorityAge -eq 1L -and
        $previousResidual -le 0.10 -and $viewCurrent -le 0.10 -and
        [double]$Record.latestEntityRawPositionLagExcessWorldUnits -le 0.0001 -and
        $Record.latestPositionRecoveryRequiredBeforeSample -eq $false
    $expectedViolation = $expectedObserved -and -not $expectedPermitted
    if ($Record.latestPositionPhaseLagObserved -ne $expectedObserved -or
        $Record.latestPositionPhaseLagPermitted -ne $expectedPermitted -or
        $Record.latestPositionPhaseLagViolation -ne $expectedViolation) {
        throw 'PASS movement telemetry latest position observed/permitted/violation flags are inconsistent with its raw residual and reference.'
    }

    $expectedAdjusted = if ($expectedPermitted) { [math]::Min($rawCurrent, $previousResidual) } else { $rawCurrent }
    if (-not (Test-KmcApproximatelyEqual ([double]$Record.latestEntityPhaseAdjustedPositionResidualWorldUnits) $expectedAdjusted)) {
        throw 'PASS movement telemetry latest phase-adjusted entity position does not follow its raw/permitted state.'
    }
    if (-not (Test-KmcApproximatelyEqual ([double]$Record.latestEntityRawPositionLagBoundWorldUnits) $authoritativeDelta) -or
        -not (Test-KmcApproximatelyEqual ([double]$Record.latestEntityRawPositionLagExcessWorldUnits) ([math]::Max(0.0, $rawCurrent - $authoritativeDelta)))) {
        throw 'PASS movement telemetry latest position raw-lag bound or excess does not reconcile with authoritative displacement.'
    }

    $expectedEntityAge = if ($rawCurrent -le 0.10) { 0L }
        elseif ($hasPrevious -and $previousResidual -le 0.10) { $authorityAge }
        else { $null }
    if ($null -eq $expectedEntityAge) {
        if ($null -ne $Record.latestEntityPositionAuthorityAgeSteps) {
            throw 'PASS movement telemetry assigns an authority age to an unmatched logical position.'
        }
    }
    elseif ($null -eq $Record.latestEntityPositionAuthorityAgeSteps -or
        [long]$Record.latestEntityPositionAuthorityAgeSteps -ne [long]$expectedEntityAge) {
        throw 'PASS movement telemetry logical-position authority age is inconsistent with its current/previous residuals.'
    }

    $recoveryRequired = $Record.latestPositionRecoveryRequiredBeforeSample -eq $true
    $expectedRecoveryUpdate = $phase -ceq 'Update' -and $recoveryRequired
    $expectedRecoverySatisfied = $expectedRecoveryUpdate -and $viewCurrent -le 0.10 -and $rawCurrent -le 0.10
    $expectedRecoveryViolation = $recoveryRequired -and ($phase -cne 'Update' -or -not $expectedRecoverySatisfied)
    $expectedRecoveryPending = $expectedPermitted -or ($recoveryRequired -and $phase -cne 'Update')
    if ($Record.latestPositionRecoveryUpdateObserved -ne $expectedRecoveryUpdate -or
        $Record.latestPositionRecoverySatisfied -ne $expectedRecoverySatisfied -or
        $Record.latestPositionRecoveryViolation -ne $expectedRecoveryViolation -or
        $Record.latestPositionRecoveryPendingAfterSample -ne $expectedRecoveryPending) {
        throw 'PASS movement telemetry latest position recovery flags are inconsistent with phase, prior obligation, and current residuals.'
    }

    $expectedStationary = $calibrated -and $authoritativeDelta -le 0.000001
    $expectedStationaryViolation = $expectedStationary -and ($viewCurrent -gt 0.10 -or $rawCurrent -gt 0.10)
    if ($Record.latestPositionStationaryAuthority -ne $expectedStationary -or
        $Record.latestStationaryPositionCorrectionViolation -ne $expectedStationaryViolation) {
        throw 'PASS movement telemetry latest position stationary-authority flags are inconsistent with displacement and residuals.'
    }

    if (-not (Test-KmcApproximatelyEqual ([double]$Record.preCorrectionViewCurrentPositionResidualWorldUnits) $viewCurrent) -or
        -not (Test-KmcApproximatelyEqual ([double]$Record.preCorrectionPositionResidualWorldUnits) ([math]::Max($viewCurrent, $expectedAdjusted))) -or
        -not (Test-KmcApproximatelyEqual ([double]$Record.preCorrectionRawCurrentPositionResidualWorldUnits) ([math]::Max($viewCurrent, $rawCurrent)))) {
        throw 'PASS movement telemetry latest pre-correction position fields do not reconcile with split view/raw/effective phase evidence.'
    }
    if ($viewCurrent -gt 0.10 -or $expectedAdjusted -gt 0.10 -or
        [double]$Record.latestEntityRawPositionLagExcessWorldUnits -gt 0.0001) {
        throw 'PASS movement telemetry latest effective position or raw-lag arithmetic exceeds its bound.'
    }
}

function Assert-KmcMovementTelemetryRecord {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$ExpectedSequence,
        [Parameter(Mandatory = $true)][string[]]$ExpectedRows,
        [Parameter(Mandatory = $true)][bool]$RequireComplete
    )
    Assert-KmcExactProperties $Record @(
        'schemaVersion','scenario','row','runId','branch','commit','productVersion','dllSha256','dllMvid','sequence','utcTimestamp',
        'riderId','mountId','relationshipState','combat','partyCombat','currentGameMode','paused','turnBased','authoritativeMover',
        'requestedDestination','riderStockAgentEnabled','mountStockAgentEnabled','riderAvoidanceDisabled','mountAvoidanceDisabled',
        'riderEntityPosition','mountEntityPosition','riderEntityOrientation','mountEntityOrientation','riderViewPosition','mountViewPosition',
        'riderViewRotation','mountViewRotation','anchor','sourceAnchor','attachmentLeaseActive','attachmentParent',
        'riderParentMatchesAttachment','attachmentRiskState','riderViewParent','presentationPositionStrategy','presentationRotationStrategy',
        'expectedAnchorPosition','expectedAnchorRotation','residualPositionWorldUnits','residualRotationDegrees',
        'riderViewPositionResidualWorldUnits','riderEntityPositionResidualWorldUnits','riderViewRotationResidualDegrees',
        'riderEntityRotationResidualDegrees','latestAuthoritativePositionSequence','latestCurrentAuthoritativeAnchorX',
        'latestCurrentAuthoritativeAnchorY','latestCurrentAuthoritativeAnchorZ','latestPreviousAuthoritativePositionSequence',
        'latestPreviousAuthoritativeAnchorX','latestPreviousAuthoritativeAnchorY','latestPreviousAuthoritativeAnchorZ',
        'latestPreviousAuthoritativePositionFrame','latestPreviousAuthoritativePositionPhase',
        'latestPreviousAuthoritativePositionReferenceKind','latestPreviousAuthoritativePositionSameFrame',
        'latestPreviousAuthoritativePositionReferenceEligible','latestAuthoritativePositionDeltaWorldUnits',
        'latestViewCurrentPositionResidualWorldUnits','latestEntityRawCurrentPositionResidualWorldUnits',
        'latestEntityPreviousAuthoritativePositionResidualWorldUnits','latestEntityPhaseAdjustedPositionResidualWorldUnits',
        'latestEntityRawPositionLagBoundWorldUnits','latestEntityRawPositionLagExcessWorldUnits',
        'latestEntityPositionAuthorityAgeSteps','latestPositionPhaseLagObserved','latestPositionPhaseLagPermitted',
        'latestPositionPhaseLagViolation','latestPositionRecoveryRequiredBeforeSample','latestPositionRecoveryUpdateObserved',
        'latestPositionRecoverySatisfied','latestPositionRecoveryViolation','latestPositionRecoveryPendingAfterSample',
        'latestPositionStationaryAuthority','latestStationaryPositionCorrectionViolation',
        'latestSynchronizationFrame','latestAuthoritativeYawSequence',
        'latestCurrentAuthoritativeYawDegrees','latestCurrentMountEntityAuthoritativeYawDegrees','latestMountEntityRootYawResidualDegrees',
        'latestPreviousAuthoritativeYawSequence','latestPreviousAuthoritativeYawDegrees','latestPreviousAuthoritativeFrame',
        'latestPreviousAuthoritativePhase','latestPreviousAuthoritativeReferenceKind','latestPreviousAuthoritativeSameFrame',
        'latestPreviousAuthoritativeReferenceEligible','latestAuthoritativeYawDeltaDegrees','latestViewCurrentYawResidualDegrees',
        'latestFullViewCurrentRotationResidualDegrees',
        'latestEntityRawCurrentYawResidualDegrees','latestEntityPreviousAuthoritativeYawResidualDegrees',
        'latestEntityPhaseAdjustedYawResidualDegrees','latestEntityRawLagBoundDegrees','latestEntityRawLagExcessDegrees',
        'latestEntityYawAuthorityAgeSteps','latestPhaseLagObserved','latestPhaseLagPermitted','latestPhaseLagViolation',
        'latestRecoveryRequiredBeforeSample','latestRecoveryUpdateObserved','latestRecoverySatisfied','latestRecoveryViolation',
        'latestRecoveryPendingAfterSample','latestStationaryAuthority','latestStationaryYawCorrectionViolation',
        'riderSelected','mountSelected','selectedUnitIds','riderCommandCount','mountCommandCount',
        'riderActiveCommandTypes','mountActiveCommandTypes','mountIsReallyMoving','mountVelocity','mountSpeed','mountMoveDirection',
        'mountPathId','mountPathFailed','mountRepathNeeded','mountPathError','mountPathErrorLog','mountPathPointCount','mountPathLength',
        'synchronizationPhase','synchronizationSampleCount','synchronizationCorrectionCount',
        'initialConfigurationSynchronizationSampleCount','initialConfigurationSynchronizationCorrectionCount',
        'updateSynchronizationSampleCount','updateSynchronizationCorrectionCount','lateUpdateSynchronizationSampleCount',
        'lateUpdateSynchronizationCorrectionCount','preCorrectionPositionResidualWorldUnits',
        'preCorrectionRawCurrentPositionResidualWorldUnits','preCorrectionViewCurrentPositionResidualWorldUnits',
        'preCorrectionRotationResidualDegrees','postCorrectionPositionResidualWorldUnits','postCorrectionRotationResidualDegrees',
        'maximumPreCorrectionPositionResidualWorldUnits','maximumPreCorrectionRawCurrentPositionResidualWorldUnits',
        'maximumPreCorrectionRotationResidualDegrees','maximumPostCorrectionPositionResidualWorldUnits','maximumPostCorrectionRotationResidualDegrees',
        'maximumInitialConfigurationPreCorrectionPositionResidualWorldUnits','maximumUpdatePreCorrectionPositionResidualWorldUnits',
        'maximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits','maximumUpdatePreCorrectionRotationResidualDegrees',
        'maximumUpdatePostCorrectionPositionResidualWorldUnits',
        'maximumUpdatePostCorrectionRotationResidualDegrees','maximumLateUpdatePreCorrectionPositionResidualWorldUnits',
        'maximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits','maximumLateUpdatePreCorrectionRotationResidualDegrees',
        'maximumLateUpdatePostCorrectionPositionResidualWorldUnits','maximumLateUpdatePostCorrectionRotationResidualDegrees',
        'maximumCalibratedViewCurrentPositionResidualWorldUnits','maximumCalibratedEntityRawCurrentPositionResidualWorldUnits',
        'maximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits',
        'maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits','maximumAuthoritativePositionDeltaWorldUnits',
        'maximumEntityRawPositionLagExcessWorldUnits','entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits',
        'positionPhaseLagObservedCount','positionPhaseLagPermittedCount','positionPhaseLagSameFrameUpdateReferenceCount',
        'positionPhaseLagEligibleReferenceCount','positionPhaseLagViolationCount','positionPhaseLagRecoveryRequiredRawCount',
        'positionPhaseLagRecoveryUpdateRawCount','positionPhaseLagRecoverySatisfiedRawCount',
        'positionPhaseLagRecoveryRequiredEffectiveCount','positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount',
        'positionPhaseLagRecoverySatisfiedEffectiveCount','positionPhaseLagRecoveryViolationCount',
        'stationaryPositionCorrectionViolationCount','outstandingPositionPhaseLagRecoveryCount',
        'maximumConsecutiveUnrecoveredPositionPhaseLagCount','maximumCalibratedViewCurrentYawResidualDegrees',
        'maximumCalibratedFullViewCurrentRotationResidualDegrees',
        'maximumCalibratedMountEntityRootYawResidualDegrees','maximumCalibratedEntityRawCurrentYawResidualDegrees',
        'maximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees','maximumCalibratedEntityPhaseAdjustedYawResidualDegrees',
        'maximumAuthoritativeYawDeltaDegrees','maximumEntityRawLagExcessDegrees','entityRawLagArithmeticCoherenceEpsilonDegrees',
        'phaseLagObservedCount','phaseLagPermittedCount',
        'phaseLagSameFrameUpdateReferenceCount','phaseLagEligibleReferenceCount','phaseLagViolationCount',
        'phaseLagRecoveryRequiredCount','phaseLagRecoveryUpdateCount','phaseLagRecoverySatisfiedCount',
        'phaseLagRecoveryRequiredRawCount','phaseLagRecoveryUpdateRawCount','phaseLagRecoverySatisfiedRawCount',
        'phaseLagRecoveryRequiredEffectiveCount','phaseLagRecoveryUpdateOrBoundaryEffectiveCount',
        'phaseLagRecoverySatisfiedEffectiveCount',
        'phaseLagRecoveryViolationCount','stationaryYawCorrectionViolationCount','outstandingPhaseLagRecoveryCount',
        'maximumConsecutiveUnrecoveredPhaseLagCount','stationaryBoundaryClosureAttemptCount',
        'stationaryBoundaryClosureSucceededCount','stationaryBoundaryClosureFailedCount',
        'yawPhaseLagStationaryBoundaryClosureCount','positionPhaseLagStationaryBoundaryClosureCount',
        'maximumResidualWorldUnits','maximumRotationResidualDegrees') 'movement telemetry record'
    Assert-KmcMovementCommonIdentity $Record $Request $ExpectedSequence $ExpectedRows 'Movement telemetry'
    foreach ($name in @('riderId','mountId','relationshipState','authoritativeMover','anchor','sourceAnchor','attachmentParent',
        'attachmentRiskState','riderViewParent','presentationPositionStrategy','presentationRotationStrategy','synchronizationPhase')) {
        if ($Record.$name -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Record.$name)) { throw "Movement telemetry $name must be a nonempty JSON string." }
    }
    foreach ($name in @('combat','turnBased','riderSelected','mountSelected','attachmentLeaseActive','riderParentMatchesAttachment')) {
        if ($Record.$name -isnot [bool]) { throw "Movement telemetry $name must be a JSON boolean." }
    }
    foreach ($name in @('partyCombat','paused','riderStockAgentEnabled','mountStockAgentEnabled','riderAvoidanceDisabled','mountAvoidanceDisabled','mountIsReallyMoving','mountPathFailed','mountRepathNeeded')) {
        Assert-KmcNullableJsonBoolean $Record.$name "movement telemetry $name"
    }
    foreach ($name in @('latestPreviousAuthoritativeSameFrame','latestPreviousAuthoritativeReferenceEligible','latestPhaseLagObserved',
        'latestPhaseLagPermitted','latestPhaseLagViolation','latestRecoveryRequiredBeforeSample','latestRecoveryUpdateObserved',
        'latestRecoverySatisfied','latestRecoveryViolation','latestRecoveryPendingAfterSample','latestStationaryAuthority',
        'latestStationaryYawCorrectionViolation','latestPreviousAuthoritativePositionSameFrame',
        'latestPreviousAuthoritativePositionReferenceEligible','latestPositionPhaseLagObserved',
        'latestPositionPhaseLagPermitted','latestPositionPhaseLagViolation','latestPositionRecoveryRequiredBeforeSample',
        'latestPositionRecoveryUpdateObserved','latestPositionRecoverySatisfied','latestPositionRecoveryViolation',
        'latestPositionRecoveryPendingAfterSample','latestPositionStationaryAuthority',
        'latestStationaryPositionCorrectionViolation')) {
        Assert-KmcNullableJsonBoolean $Record.$name "movement telemetry $name"
    }
    if ($null -ne $Record.currentGameMode -and $Record.currentGameMode -isnot [string]) { throw 'Movement telemetry currentGameMode must be a JSON string or null.' }
    if ($null -ne $Record.latestPreviousAuthoritativePhase -and $Record.latestPreviousAuthoritativePhase -isnot [string]) { throw 'Movement telemetry latestPreviousAuthoritativePhase must be a JSON string or null.' }
    if ($null -ne $Record.latestPreviousAuthoritativePositionPhase -and $Record.latestPreviousAuthoritativePositionPhase -isnot [string]) { throw 'Movement telemetry latestPreviousAuthoritativePositionPhase must be a JSON string or null.' }
    foreach ($name in @('latestPreviousAuthoritativeReferenceKind','latestPreviousAuthoritativePositionReferenceKind')) {
        if ($null -ne $Record.$name -and ($Record.$name -isnot [string] -or [string]$Record.$name -cnotin @('none','same-frame-update','prior-frame-update'))) {
            throw "Movement telemetry $name is invalid."
        }
    }
    foreach ($name in @('requestedDestination','riderEntityPosition','mountEntityPosition','riderViewPosition','mountViewPosition','riderViewRotation','mountViewRotation','expectedAnchorPosition','expectedAnchorRotation','mountVelocity','mountMoveDirection')) {
        Assert-KmcMovementVector3 $Record.$name "movement telemetry $name" -AllowNull:($name -in @('requestedDestination','riderViewPosition','mountViewPosition','riderViewRotation','mountViewRotation','mountVelocity','mountMoveDirection'))
    }
    foreach ($name in @('riderEntityOrientation','mountEntityOrientation')) {
        if ($null -ne $Record.$name -and -not (Test-KmcFiniteJsonNumber $Record.$name)) { throw "Movement telemetry $name must be a finite JSON number or null." }
    }
    foreach ($name in @('residualPositionWorldUnits','residualRotationDegrees','riderViewPositionResidualWorldUnits',
        'riderEntityPositionResidualWorldUnits','riderViewRotationResidualDegrees','riderEntityRotationResidualDegrees','mountSpeed','mountPathLength',
        'preCorrectionPositionResidualWorldUnits','preCorrectionRotationResidualDegrees','postCorrectionPositionResidualWorldUnits','postCorrectionRotationResidualDegrees',
        'maximumPreCorrectionPositionResidualWorldUnits','maximumPreCorrectionRotationResidualDegrees','maximumPostCorrectionPositionResidualWorldUnits',
        'maximumPostCorrectionRotationResidualDegrees','maximumInitialConfigurationPreCorrectionPositionResidualWorldUnits',
        'maximumUpdatePreCorrectionPositionResidualWorldUnits','maximumUpdatePreCorrectionRotationResidualDegrees','maximumUpdatePostCorrectionPositionResidualWorldUnits',
        'maximumUpdatePostCorrectionRotationResidualDegrees','maximumLateUpdatePreCorrectionPositionResidualWorldUnits',
        'maximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits','maximumLateUpdatePreCorrectionRotationResidualDegrees',
        'maximumLateUpdatePostCorrectionPositionResidualWorldUnits',
        'maximumLateUpdatePostCorrectionRotationResidualDegrees','latestMountEntityRootYawResidualDegrees',
        'latestAuthoritativePositionDeltaWorldUnits','latestViewCurrentPositionResidualWorldUnits',
        'latestEntityRawCurrentPositionResidualWorldUnits','latestEntityPreviousAuthoritativePositionResidualWorldUnits',
        'latestEntityPhaseAdjustedPositionResidualWorldUnits','latestEntityRawPositionLagBoundWorldUnits',
        'latestEntityRawPositionLagExcessWorldUnits','preCorrectionRawCurrentPositionResidualWorldUnits',
        'preCorrectionViewCurrentPositionResidualWorldUnits','maximumPreCorrectionRawCurrentPositionResidualWorldUnits',
        'maximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits',
        'maximumCalibratedViewCurrentPositionResidualWorldUnits','maximumCalibratedEntityRawCurrentPositionResidualWorldUnits',
        'maximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits',
        'maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits','maximumAuthoritativePositionDeltaWorldUnits',
        'maximumEntityRawPositionLagExcessWorldUnits','entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits',
        'latestAuthoritativeYawDeltaDegrees','latestViewCurrentYawResidualDegrees','latestFullViewCurrentRotationResidualDegrees',
        'latestEntityRawCurrentYawResidualDegrees',
        'latestEntityPreviousAuthoritativeYawResidualDegrees','latestEntityPhaseAdjustedYawResidualDegrees','latestEntityRawLagBoundDegrees',
        'latestEntityRawLagExcessDegrees','maximumCalibratedViewCurrentYawResidualDegrees',
        'maximumCalibratedFullViewCurrentRotationResidualDegrees','maximumCalibratedMountEntityRootYawResidualDegrees',
        'maximumCalibratedEntityRawCurrentYawResidualDegrees','maximumCalibratedEntityPreviousAuthoritativeYawResidualDegrees',
        'maximumCalibratedEntityPhaseAdjustedYawResidualDegrees','maximumAuthoritativeYawDeltaDegrees','maximumEntityRawLagExcessDegrees',
        'entityRawLagArithmeticCoherenceEpsilonDegrees',
        'maximumResidualWorldUnits','maximumRotationResidualDegrees')) {
        if ($null -ne $Record.$name -and -not (Test-KmcFiniteNonnegativeJsonNumber $Record.$name)) { throw "Movement telemetry $name must be a finite nonnegative JSON number or null." }
    }
    foreach ($name in @('latestCurrentAuthoritativeYawDegrees','latestCurrentMountEntityAuthoritativeYawDegrees','latestPreviousAuthoritativeYawDegrees')) {
        if ($null -ne $Record.$name -and -not (Test-KmcFiniteJsonNumber $Record.$name)) { throw "Movement telemetry $name must be a finite JSON number or null." }
    }
    foreach ($name in @('preCorrectionRawCurrentPositionResidualWorldUnits','preCorrectionViewCurrentPositionResidualWorldUnits',
        'maximumPreCorrectionRawCurrentPositionResidualWorldUnits','maximumUpdatePreCorrectionRawCurrentPositionResidualWorldUnits',
        'maximumLateUpdatePreCorrectionRawCurrentPositionResidualWorldUnits',
        'maximumCalibratedViewCurrentPositionResidualWorldUnits','maximumCalibratedEntityRawCurrentPositionResidualWorldUnits',
        'maximumCalibratedEntityPreviousAuthoritativePositionResidualWorldUnits',
        'maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits','maximumAuthoritativePositionDeltaWorldUnits',
        'maximumEntityRawPositionLagExcessWorldUnits','entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits')) {
        if (-not (Test-KmcFiniteNonnegativeJsonNumber $Record.$name)) { throw "Movement telemetry $name must be a finite nonnegative JSON number." }
    }
    foreach ($name in @('latestCurrentAuthoritativeAnchorX','latestCurrentAuthoritativeAnchorY','latestCurrentAuthoritativeAnchorZ',
        'latestPreviousAuthoritativeAnchorX','latestPreviousAuthoritativeAnchorY','latestPreviousAuthoritativeAnchorZ')) {
        if ($null -ne $Record.$name -and -not (Test-KmcFiniteJsonNumber $Record.$name)) { throw "Movement telemetry $name must be a finite JSON number or null." }
    }
    foreach ($name in @('riderCommandCount','mountCommandCount','mountPathId','mountPathPointCount',
        'latestSynchronizationFrame','latestAuthoritativeYawSequence','latestPreviousAuthoritativeYawSequence','latestPreviousAuthoritativeFrame',
        'latestEntityYawAuthorityAgeSteps','latestAuthoritativePositionSequence','latestPreviousAuthoritativePositionSequence',
        'latestPreviousAuthoritativePositionFrame','latestEntityPositionAuthorityAgeSteps')) {
        if ($null -ne $Record.$name -and (-not (Test-KmcExactJsonInteger $Record.$name) -or [long]$Record.$name -lt 0)) { throw "Movement telemetry $name must be a nonnegative exact JSON integer or null." }
    }
    foreach ($name in @('synchronizationSampleCount','synchronizationCorrectionCount',
        'initialConfigurationSynchronizationSampleCount','initialConfigurationSynchronizationCorrectionCount','updateSynchronizationSampleCount',
        'updateSynchronizationCorrectionCount','lateUpdateSynchronizationSampleCount','lateUpdateSynchronizationCorrectionCount',
        'positionPhaseLagObservedCount','positionPhaseLagPermittedCount','positionPhaseLagSameFrameUpdateReferenceCount',
        'positionPhaseLagEligibleReferenceCount','positionPhaseLagViolationCount','positionPhaseLagRecoveryRequiredRawCount',
        'positionPhaseLagRecoveryUpdateRawCount','positionPhaseLagRecoverySatisfiedRawCount',
        'positionPhaseLagRecoveryRequiredEffectiveCount','positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount',
        'positionPhaseLagRecoverySatisfiedEffectiveCount','positionPhaseLagRecoveryViolationCount',
        'stationaryPositionCorrectionViolationCount','outstandingPositionPhaseLagRecoveryCount',
        'maximumConsecutiveUnrecoveredPositionPhaseLagCount','phaseLagObservedCount','phaseLagPermittedCount',
        'phaseLagSameFrameUpdateReferenceCount','phaseLagEligibleReferenceCount','phaseLagViolationCount',
        'phaseLagRecoveryRequiredCount','phaseLagRecoveryUpdateCount','phaseLagRecoverySatisfiedCount',
        'phaseLagRecoveryRequiredRawCount','phaseLagRecoveryUpdateRawCount','phaseLagRecoverySatisfiedRawCount',
        'phaseLagRecoveryRequiredEffectiveCount','phaseLagRecoveryUpdateOrBoundaryEffectiveCount',
        'phaseLagRecoverySatisfiedEffectiveCount','phaseLagRecoveryViolationCount','stationaryYawCorrectionViolationCount',
        'outstandingPhaseLagRecoveryCount','maximumConsecutiveUnrecoveredPhaseLagCount',
        'stationaryBoundaryClosureAttemptCount','stationaryBoundaryClosureSucceededCount','stationaryBoundaryClosureFailedCount',
        'yawPhaseLagStationaryBoundaryClosureCount','positionPhaseLagStationaryBoundaryClosureCount')) {
        if (-not (Test-KmcExactJsonInteger $Record.$name) -or [long]$Record.$name -lt 0) { throw "Movement telemetry $name must be a nonnegative exact JSON integer." }
    }
    Assert-KmcJsonStringArray $Record.selectedUnitIds 'movement telemetry selectedUnitIds'
    Assert-KmcJsonStringArray $Record.riderActiveCommandTypes 'movement telemetry riderActiveCommandTypes'
    Assert-KmcJsonStringArray $Record.mountActiveCommandTypes 'movement telemetry mountActiveCommandTypes'
    if ($null -ne $Record.mountPathError -and $Record.mountPathError -isnot [bool] -and $Record.mountPathError -isnot [string] -and -not (Test-KmcExactJsonInteger $Record.mountPathError)) { throw 'Movement telemetry mountPathError must be a boolean, string, integer, or null.' }
    if ($null -ne $Record.mountPathErrorLog -and $Record.mountPathErrorLog -isnot [string]) { throw 'Movement telemetry mountPathErrorLog must be a string or null.' }
    if ($RequireComplete) {
        if ([string]$Record.relationshipState -cne 'Mounted' -or [string]$Record.authoritativeMover -cne 'mount' -or
            $Record.combat -ne $false -or $Record.partyCombat -ne $false -or $Record.turnBased -ne $false -or
            [string]$Record.currentGameMode -cne 'Default' -or $Record.riderStockAgentEnabled -ne $false -or
            $Record.mountStockAgentEnabled -ne $true -or $Record.riderAvoidanceDisabled -ne $true -or
            $Record.attachmentLeaseActive -ne $true -or $Record.riderParentMatchesAttachment -ne $true -or
            [string]$Record.anchor -cne 'KMC_RiderPositionAnchor' -or [string]$Record.sourceAnchor -cne 'Spine' -or
            [string]$Record.attachmentParent -cne 'KMC_RiderPositionAnchor' -or
            [string]$Record.riderViewParent -cne 'KMC_RiderPositionAnchor' -or
            [string]$Record.attachmentRiskState -cne 'active and internally consistent' -or
            [string]$Record.presentationPositionStrategy -cne 'Mammoth-root static point projected from Spine at lease acquisition' -or
            [string]$Record.presentationRotationStrategy -cne 'upright authoritative-mount-root yaw plus configured rider yaw') {
            throw 'PASS movement telemetry does not prove the exact safe mounted movement-authority state.'
        }
        if ([double]$Record.maximumUpdatePreCorrectionPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumLateUpdatePreCorrectionPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumCalibratedViewCurrentPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumCalibratedEntityPhaseAdjustedPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumEntityRawPositionLagExcessWorldUnits -gt 0.0001 -or
            [double]$Record.maximumUpdatePreCorrectionRotationResidualDegrees -gt 0.10 -or
            [double]$Record.maximumLateUpdatePreCorrectionRotationResidualDegrees -gt 0.10 -or
            [double]$Record.maximumCalibratedViewCurrentYawResidualDegrees -gt 0.10 -or
            [double]$Record.maximumCalibratedFullViewCurrentRotationResidualDegrees -gt 0.10 -or
            [double]$Record.maximumCalibratedMountEntityRootYawResidualDegrees -gt 0.10 -or
            [double]$Record.maximumCalibratedEntityPhaseAdjustedYawResidualDegrees -gt 0.10 -or
            [double]$Record.maximumEntityRawLagExcessDegrees -gt 0.0001 -or
            [double]$Record.maximumPostCorrectionPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumPostCorrectionRotationResidualDegrees -gt 0.10) {
            throw 'PASS movement telemetry exceeds the calibrated residual thresholds.'
        }
        if ([math]::Abs([double]$Record.entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits - 0.0001) -gt 0.000000000001 -or
            [math]::Abs([double]$Record.entityRawLagArithmeticCoherenceEpsilonDegrees - 0.0001) -gt 0.000000000001) {
            throw 'PASS movement telemetry changed a fixed raw-lag arithmetic coherence epsilon.'
        }
        if ([long]$Record.positionPhaseLagViolationCount -ne 0 -or [long]$Record.positionPhaseLagRecoveryViolationCount -ne 0 -or
            [long]$Record.stationaryPositionCorrectionViolationCount -ne 0 -or
            [long]$Record.outstandingPositionPhaseLagRecoveryCount -gt 1 -or
            [long]$Record.maximumConsecutiveUnrecoveredPositionPhaseLagCount -gt 1 -or
            [long]$Record.positionPhaseLagObservedCount -ne [long]$Record.positionPhaseLagPermittedCount -or
            [long]$Record.positionPhaseLagPermittedCount -ne [long]$Record.positionPhaseLagSameFrameUpdateReferenceCount -or
            [long]$Record.positionPhaseLagPermittedCount -ne [long]$Record.positionPhaseLagEligibleReferenceCount -or
            [long]$Record.positionPhaseLagRecoveryRequiredRawCount -ne [long]$Record.positionPhaseLagRecoveryUpdateRawCount -or
            [long]$Record.positionPhaseLagRecoveryUpdateRawCount -ne [long]$Record.positionPhaseLagRecoverySatisfiedRawCount -or
            [long]$Record.positionPhaseLagRecoveryRequiredEffectiveCount -ne
                ([long]$Record.positionPhaseLagRecoveryRequiredRawCount + [long]$Record.positionPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount -ne
                ([long]$Record.positionPhaseLagRecoveryUpdateRawCount + [long]$Record.positionPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.positionPhaseLagRecoverySatisfiedEffectiveCount -ne
                ([long]$Record.positionPhaseLagRecoverySatisfiedRawCount + [long]$Record.positionPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.positionPhaseLagPermittedCount -ne
                ([long]$Record.positionPhaseLagRecoverySatisfiedEffectiveCount + [long]$Record.outstandingPositionPhaseLagRecoveryCount)) {
            throw 'PASS movement telemetry violates the bounded same-frame position-lag or recovery contract.'
        }
        if ([long]$Record.phaseLagRecoveryRequiredCount -ne [long]$Record.phaseLagRecoveryRequiredRawCount -or
            [long]$Record.phaseLagRecoveryUpdateCount -ne [long]$Record.phaseLagRecoveryUpdateRawCount -or
            [long]$Record.phaseLagRecoverySatisfiedCount -ne [long]$Record.phaseLagRecoverySatisfiedRawCount -or
            [long]$Record.phaseLagViolationCount -ne 0 -or [long]$Record.phaseLagRecoveryViolationCount -ne 0 -or
            [long]$Record.stationaryYawCorrectionViolationCount -ne 0 -or [long]$Record.outstandingPhaseLagRecoveryCount -gt 1 -or
            [long]$Record.maximumConsecutiveUnrecoveredPhaseLagCount -gt 1 -or
            [long]$Record.phaseLagObservedCount -ne [long]$Record.phaseLagPermittedCount -or
            [long]$Record.phaseLagPermittedCount -ne [long]$Record.phaseLagSameFrameUpdateReferenceCount -or
            [long]$Record.phaseLagPermittedCount -ne [long]$Record.phaseLagEligibleReferenceCount -or
            [long]$Record.phaseLagRecoveryRequiredRawCount -ne [long]$Record.phaseLagRecoveryUpdateRawCount -or
            [long]$Record.phaseLagRecoveryUpdateRawCount -ne [long]$Record.phaseLagRecoverySatisfiedRawCount -or
            [long]$Record.phaseLagRecoveryRequiredEffectiveCount -ne
                ([long]$Record.phaseLagRecoveryRequiredRawCount + [long]$Record.yawPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.phaseLagRecoveryUpdateOrBoundaryEffectiveCount -ne
                ([long]$Record.phaseLagRecoveryUpdateRawCount + [long]$Record.yawPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.phaseLagRecoverySatisfiedEffectiveCount -ne
                ([long]$Record.phaseLagRecoverySatisfiedRawCount + [long]$Record.yawPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.phaseLagPermittedCount -ne
                ([long]$Record.phaseLagRecoverySatisfiedEffectiveCount + [long]$Record.outstandingPhaseLagRecoveryCount)) {
            throw 'PASS movement telemetry violates the bounded same-frame phase-lag or recovery contract.'
        }
        if ([long]$Record.stationaryBoundaryClosureAttemptCount -ne
                ([long]$Record.stationaryBoundaryClosureSucceededCount + [long]$Record.stationaryBoundaryClosureFailedCount) -or
            [long]$Record.stationaryBoundaryClosureFailedCount -ne 0 -or
            [long]$Record.stationaryBoundaryClosureSucceededCount -gt 1 -or
            [long]$Record.yawPhaseLagStationaryBoundaryClosureCount -gt 1 -or
            [long]$Record.positionPhaseLagStationaryBoundaryClosureCount -gt 1) {
            throw 'PASS movement telemetry has inconsistent stationary-boundary closure counts.'
        }
        Assert-KmcLatestPositionPhaseSemantics $Record
        if ($Record.latestMountEntityRootYawResidualDegrees -ne $null -and [double]$Record.latestMountEntityRootYawResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest mount entity/root yaw is incoherent.' }
        if ($Record.latestViewCurrentYawResidualDegrees -ne $null -and [double]$Record.latestViewCurrentYawResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest rider view yaw is not current.' }
        if ($Record.latestFullViewCurrentRotationResidualDegrees -ne $null -and [double]$Record.latestFullViewCurrentRotationResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest rider view quaternion is not current.' }
        if ($Record.latestEntityPhaseAdjustedYawResidualDegrees -ne $null -and [double]$Record.latestEntityPhaseAdjustedYawResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest logical entity yaw is not current or an eligible immediate prior yaw.' }
        if ($Record.latestPhaseLagPermitted -eq $true) {
            if ([string]$Record.synchronizationPhase -cne 'LateUpdate' -or [string]$Record.latestPreviousAuthoritativePhase -cne 'Update' -or
                [string]$Record.latestPreviousAuthoritativeReferenceKind -cne 'same-frame-update' -or
                $Record.latestPreviousAuthoritativeSameFrame -ne $true -or $Record.latestPreviousAuthoritativeReferenceEligible -ne $true -or
                [long]$Record.latestPreviousAuthoritativeFrame -ne [long]$Record.latestSynchronizationFrame -or
                [long]$Record.latestEntityYawAuthorityAgeSteps -ne 1 -or [double]$Record.latestEntityPreviousAuthoritativeYawResidualDegrees -gt 0.10 -or
                [double]$Record.latestEntityRawLagExcessDegrees -gt 0.0001 -or $Record.latestRecoveryPendingAfterSample -ne $true) {
                throw 'PASS movement telemetry permitted a yaw lag without the exact same-frame Update reference.'
            }
        }
        $requiredLatest = @(
            'latestSynchronizationFrame','latestAuthoritativeYawSequence','latestCurrentAuthoritativeYawDegrees',
            'latestCurrentMountEntityAuthoritativeYawDegrees','latestMountEntityRootYawResidualDegrees',
            'latestAuthoritativeYawDeltaDegrees','latestViewCurrentYawResidualDegrees','latestFullViewCurrentRotationResidualDegrees',
            'latestEntityRawCurrentYawResidualDegrees','latestEntityPhaseAdjustedYawResidualDegrees','latestEntityRawLagBoundDegrees',
            'latestEntityRawLagExcessDegrees','latestEntityYawAuthorityAgeSteps','latestPhaseLagObserved','latestPhaseLagPermitted',
            'latestPhaseLagViolation','latestRecoveryRequiredBeforeSample','latestRecoveryUpdateObserved','latestRecoverySatisfied',
            'latestRecoveryViolation','latestRecoveryPendingAfterSample','latestStationaryAuthority',
            'latestStationaryYawCorrectionViolation')
        foreach ($name in $requiredLatest) {
            if ($null -eq $Record.$name) { throw "PASS movement telemetry latest phase-order field $name is null." }
        }

        $phase = [string]$Record.synchronizationPhase
        $calibrated = $phase -ceq 'Update' -or $phase -ceq 'LateUpdate'
        if ($phase -cnotin @('InitialConfiguration','Update','LateUpdate')) { throw 'PASS movement telemetry synchronizationPhase is invalid.' }
        $rawCurrent = [double]$Record.latestEntityRawCurrentYawResidualDegrees
        $viewCurrent = [double]$Record.latestViewCurrentYawResidualDegrees
        $authoritativeDelta = [double]$Record.latestAuthoritativeYawDeltaDegrees
        $expectedObserved = $calibrated -and $rawCurrent -gt 0.10

        $previousNames = @('latestPreviousAuthoritativeYawSequence','latestPreviousAuthoritativeYawDegrees',
            'latestPreviousAuthoritativeFrame','latestPreviousAuthoritativePhase','latestEntityPreviousAuthoritativeYawResidualDegrees')
        $previousPresent = @($previousNames | Where-Object { $null -ne $Record.$_ }).Count
        if ($previousPresent -ne 0 -and $previousPresent -ne $previousNames.Count) {
            throw 'PASS movement telemetry previous-authority reference fields are only partially populated.'
        }
        $hasPrevious = $previousPresent -eq $previousNames.Count
        if ($phase -cne 'LateUpdate' -and $hasPrevious) { throw 'PASS movement telemetry exposes a previous Update reference outside LateUpdate.' }
        if (-not $hasPrevious) {
            if ([string]$Record.latestPreviousAuthoritativeReferenceKind -cne 'none' -or
                $Record.latestPreviousAuthoritativeSameFrame -ne $false -or
                $Record.latestPreviousAuthoritativeReferenceEligible -ne $false) {
                throw 'PASS movement telemetry null previous-authority fields have inconsistent reference flags.'
            }
        }
        else {
            if ($phase -cne 'LateUpdate' -or [string]$Record.latestPreviousAuthoritativePhase -cne 'Update') {
                throw 'PASS movement telemetry previous-authority reference is not a LateUpdate-to-Update reference.'
            }
            $authorityAge = [long]$Record.latestAuthoritativeYawSequence - [long]$Record.latestPreviousAuthoritativeYawSequence
            if ($authorityAge -lt 0) { throw 'PASS movement telemetry previous-authority sequence is newer than the current sequence.' }
            $sameFrame = [long]$Record.latestPreviousAuthoritativeFrame -eq [long]$Record.latestSynchronizationFrame
            $eligible = $sameFrame -and $authorityAge -eq 1
            $expectedKind = if ($sameFrame) { 'same-frame-update' } else { 'prior-frame-update' }
            if ($Record.latestPreviousAuthoritativeSameFrame -ne $sameFrame -or
                $Record.latestPreviousAuthoritativeReferenceEligible -ne $eligible -or
                [string]$Record.latestPreviousAuthoritativeReferenceKind -cne $expectedKind) {
                throw 'PASS movement telemetry previous-authority reference kind, frame, eligibility, or sequence age is inconsistent.'
            }
        }

        $previousResidual = if ($hasPrevious) { [double]$Record.latestEntityPreviousAuthoritativeYawResidualDegrees } else { $null }
        $expectedPermitted = $expectedObserved -and $phase -ceq 'LateUpdate' -and $hasPrevious -and
            $Record.latestPreviousAuthoritativeReferenceEligible -eq $true -and $previousResidual -le 0.10 -and
            $viewCurrent -le 0.10 -and [double]$Record.latestEntityRawLagExcessDegrees -le 0.0001 -and
            $Record.latestRecoveryRequiredBeforeSample -eq $false
        $expectedViolation = $expectedObserved -and -not $expectedPermitted
        if ($Record.latestPhaseLagObserved -ne $expectedObserved -or
            $Record.latestPhaseLagPermitted -ne $expectedPermitted -or
            $Record.latestPhaseLagViolation -ne $expectedViolation) {
            throw 'PASS movement telemetry latest observed/permitted/violation phase-lag flags are inconsistent with its raw residual and reference.'
        }

        $expectedAdjusted = if ($expectedPermitted) { [math]::Min($rawCurrent, $previousResidual) } else { $rawCurrent }
        if (-not (Test-KmcApproximatelyEqual ([double]$Record.latestEntityPhaseAdjustedYawResidualDegrees) $expectedAdjusted)) {
            throw 'PASS movement telemetry latest phase-adjusted entity yaw does not follow its raw/permitted state.'
        }
        if (-not (Test-KmcApproximatelyEqual ([double]$Record.latestEntityRawLagBoundDegrees) $authoritativeDelta) -or
            -not (Test-KmcApproximatelyEqual ([double]$Record.latestEntityRawLagExcessDegrees) ([math]::Max(0.0, $rawCurrent - $authoritativeDelta)))) {
            throw 'PASS movement telemetry latest raw-lag bound or excess does not reconcile with authoritative yaw advance.'
        }

        $expectedEntityAge = if ($rawCurrent -le 0.10) { 0L }
            elseif ($hasPrevious -and $previousResidual -le 0.10) {
                [long]$Record.latestAuthoritativeYawSequence - [long]$Record.latestPreviousAuthoritativeYawSequence
            }
            else { $null }
        if ($null -eq $expectedEntityAge) {
            if ($null -ne $Record.latestEntityYawAuthorityAgeSteps) { throw 'PASS movement telemetry assigns an authority age to an unmatched logical yaw.' }
        }
        elseif ($null -eq $Record.latestEntityYawAuthorityAgeSteps -or [long]$Record.latestEntityYawAuthorityAgeSteps -ne $expectedEntityAge) {
            throw 'PASS movement telemetry logical-yaw authority age is inconsistent with its current/previous residuals.'
        }

        $recoveryRequired = $Record.latestRecoveryRequiredBeforeSample -eq $true
        $expectedRecoveryUpdate = $phase -ceq 'Update' -and $recoveryRequired
        $expectedRecoverySatisfied = $expectedRecoveryUpdate -and $viewCurrent -le 0.10 -and $rawCurrent -le 0.10
        $expectedRecoveryViolation = $recoveryRequired -and ($phase -cne 'Update' -or -not $expectedRecoverySatisfied)
        $expectedRecoveryPending = $expectedPermitted -or ($recoveryRequired -and $phase -cne 'Update')
        if ($Record.latestRecoveryUpdateObserved -ne $expectedRecoveryUpdate -or
            $Record.latestRecoverySatisfied -ne $expectedRecoverySatisfied -or
            $Record.latestRecoveryViolation -ne $expectedRecoveryViolation -or
            $Record.latestRecoveryPendingAfterSample -ne $expectedRecoveryPending) {
            throw 'PASS movement telemetry latest recovery flags are inconsistent with phase, prior obligation, and current residuals.'
        }

        $expectedStationary = $calibrated -and $authoritativeDelta -le 0.000001
        $expectedStationaryViolation = $expectedStationary -and ($viewCurrent -gt 0.10 -or $rawCurrent -gt 0.10)
        if ($Record.latestStationaryAuthority -ne $expectedStationary -or
            $Record.latestStationaryYawCorrectionViolation -ne $expectedStationaryViolation) {
            throw 'PASS movement telemetry latest stationary-authority flags are inconsistent with its authoritative delta and residuals.'
        }
        if ([long]$Record.updateSynchronizationCorrectionCount -gt [long]$Record.updateSynchronizationSampleCount -or
            [long]$Record.lateUpdateSynchronizationCorrectionCount -gt [long]$Record.lateUpdateSynchronizationSampleCount) {
            throw 'PASS movement telemetry correction counts exceed their phase sample counts.'
        }
    }
}

function Assert-KmcMovementScenarioRecord {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$ExpectedSequence,
        [Parameter(Mandatory = $true)][string[]]$ExpectedRows,
        [Parameter(Mandatory = $true)][bool]$RequireComplete,
        [Parameter(Mandatory = $true)]$Manifest
    )
    $common = @('schemaVersion','runId','scenario','row','branch','commit','productVersion','dllSha256','dllMvid','sequence','utcTimestamp','kind')
    if ($Record.kind -isnot [string] -or [string]$Record.kind -cnotin @('path-probe','movement-row-result')) { throw 'Movement scenario evidence kind is invalid.' }
    if ([string]$Record.kind -ceq 'path-probe') {
        Assert-KmcExactProperties $Record ($common + @('requested','endpoint','pathLength','accepted','strictDoor')) 'movement path-probe record'
    }
    else {
        Assert-KmcExactProperties $Record ($common + @(
            'status','assertionPassCount','assertionFailCount','maximumPreCorrectionResidualWorldUnits',
            'maximumInitialConfigurationResidualWorldUnits','maximumUpdatePreCorrectionResidualWorldUnits',
            'maximumLateUpdatePreCorrectionResidualWorldUnits','maximumUpdatePreCorrectionRotationResidualDegrees',
            'maximumLateUpdatePreCorrectionRotationResidualDegrees','maximumPostCorrectionResidualWorldUnits',
            'maximumPostCorrectionRotationResidualDegrees','maximumRawCurrentPositionResidualWorldUnits',
            'maximumUpdateRawCurrentPositionResidualWorldUnits','maximumLateUpdateRawCurrentPositionResidualWorldUnits',
            'maximumViewCurrentPositionResidualWorldUnits','maximumEntityRawCurrentPositionResidualWorldUnits',
            'maximumEntityPreviousAuthoritativePositionResidualWorldUnits',
            'maximumEntityPhaseAdjustedPositionResidualWorldUnits','maximumAuthoritativePositionDeltaWorldUnits',
            'maximumEntityRawPositionLagExcessWorldUnits','entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits',
            'positionPhaseLagObservedCount','positionPhaseLagPermittedCount','positionPhaseLagSameFrameUpdateReferenceCount',
            'positionPhaseLagEligibleReferenceCount','positionPhaseLagViolationCount','positionPhaseLagRecoveryRequiredRawCount',
            'positionPhaseLagRecoveryUpdateRawCount','positionPhaseLagRecoverySatisfiedRawCount',
            'positionPhaseLagRecoveryRequiredEffectiveCount','positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount',
            'positionPhaseLagRecoverySatisfiedEffectiveCount','positionPhaseLagRecoveryViolationCount',
            'stationaryPositionCorrectionViolationCount','outstandingPositionPhaseLagRecoveryCount',
            'maximumConsecutiveUnrecoveredPositionPhaseLagCount','maximumViewCurrentYawResidualDegrees',
            'maximumFullViewCurrentRotationResidualDegrees',
            'maximumMountEntityRootYawResidualDegrees','maximumEntityRawCurrentYawResidualDegrees',
            'maximumEntityPreviousAuthoritativeYawResidualDegrees','maximumEntityPhaseAdjustedYawResidualDegrees',
            'maximumAuthoritativeYawDeltaDegrees','maximumEntityRawLagExcessDegrees','entityRawLagArithmeticCoherenceEpsilonDegrees',
            'phaseLagObservedCount','phaseLagPermittedCount',
            'phaseLagSameFrameUpdateReferenceCount','phaseLagEligibleReferenceCount','phaseLagViolationCount',
            'phaseLagRecoveryRequiredCount','phaseLagRecoveryUpdateCount','phaseLagRecoverySatisfiedCount',
            'phaseLagRecoveryRequiredRawCount','phaseLagRecoveryUpdateRawCount','phaseLagRecoverySatisfiedRawCount',
            'phaseLagRecoveryRequiredEffectiveCount','phaseLagRecoveryUpdateOrBoundaryEffectiveCount',
            'phaseLagRecoverySatisfiedEffectiveCount',
            'phaseLagRecoveryViolationCount','stationaryYawCorrectionViolationCount','outstandingPhaseLagRecoveryCount',
            'maximumConsecutiveUnrecoveredPhaseLagCount','finalSynchronizationSnapshotCaptured','finalSynchronizationSnapshotStage',
            'finalSynchronizationSnapshotFrame','finalSynchronizationAgentFrame','finalSynchronizationSampleCount',
            'finalSynchronizationOutstandingRecoveryCount','finalSynchronizationOutstandingPositionRecoveryCount',
            'finalSynchronizationQualificationPassed',
            'finalSynchronizationMovementStoppedBeforeSnapshot','finalSynchronizationBoundaryPositionResidualWorldUnits',
            'finalSynchronizationBoundaryViewPositionResidualWorldUnits','finalSynchronizationBoundaryEntityPositionResidualWorldUnits',
            'finalSynchronizationBoundaryFullViewRotationResidualDegrees','finalSynchronizationBoundaryViewYawResidualDegrees',
            'finalSynchronizationBoundaryEntityCurrentYawResidualDegrees','finalSynchronizationBoundaryMountEntityRootYawResidualDegrees',
            'finalSynchronizationBoundaryAuthoritativePositionAdvanceWorldUnits',
            'finalSynchronizationBoundaryAuthoritativeYawAdvanceDegrees','finalSynchronizationBoundaryMovementCommandAbsent',
            'finalSynchronizationBoundaryWantsToMove','finalSynchronizationBoundaryIsReallyMoving',
            'finalSynchronizationBoundaryClosureAttempted','finalSynchronizationBoundaryClosureSucceeded',
            'finalSynchronizationBoundaryClosureReason','finalSynchronizationBoundaryYawPendingBefore',
            'finalSynchronizationBoundaryPositionPendingBefore','finalSynchronizationBoundaryYawClosedCount',
            'finalSynchronizationBoundaryPositionClosedCount','finalSynchronizationBoundaryYawPendingAfter',
            'finalSynchronizationBoundaryPositionPendingAfter','stationaryBoundaryClosureAttemptCount',
            'stationaryBoundaryClosureSucceededCount','stationaryBoundaryClosureFailedCount',
            'yawPhaseLagStationaryBoundaryClosureCount','positionPhaseLagStationaryBoundaryClosureCount',
            'synchronizationObservationCount','updateSynchronizationSampleCount',
            'lateUpdateSynchronizationSampleCount','updateSynchronizationCorrectionCount','lateUpdateSynchronizationCorrectionCount',
            'maximumStationaryDriftWorldUnits','maximumStuckSeconds','oscillationCount','unexpectedRepathCount',
            'commandReplacementCount','selectionLossCount','waypointCount','endpointQualifiedWaypointCount',
            'maximumCompletedLegFinalTargetDistanceWorldUnits','maximumCompletedLegBestTargetDistanceWorldUnits',
            'maximumTurnDegrees','nonPairInterferenceCount',
            'nonPairUnitId',
            'mountFinalTargetDistanceWorldUnits','nonPairBestTargetDistanceWorldUnits','nonPairFinalTargetDistanceWorldUnits',
            'minimumPairNonPairSeparationWorldUnits','requiredPairNonPairSeparationWorldUnits','unmountedDoorControlPassed',
            'doorApproachSkipped','stopCommandIssuedCount','restartCompleted','selectionMountNormalized',
            'selectionSwitchedAway','selectionSwitchedBack','formationSelectionNormalized','pauseEntered',
            'pauseObservationSeconds','pauseMaximumDriftWorldUnits','pauseExited','destinationCancelCommandAbsent',
            'destinationCancelRelationshipPreserved',
            'cleanupTrigger','cleanupSucceeded','cleanupResult','cleanupResidual','cleanupBefore','cleanupAfter',
            'selectionCoverage','formationCoverage','door','doorNear','doorFar','screenshots','screenshotCaptureErrors','errors')) 'movement row-result record'
    }
    Assert-KmcMovementCommonIdentity $Record $Request $ExpectedSequence $ExpectedRows 'Movement scenario evidence'
    if ([string]$Record.kind -ceq 'path-probe') {
        Assert-KmcMovementVector3 $Record.requested 'movement path-probe requested'
        Assert-KmcMovementVector3 $Record.endpoint 'movement path-probe endpoint'
        if (-not (Test-KmcFiniteNonnegativeJsonNumber $Record.pathLength) -or $Record.accepted -isnot [bool] -or $Record.strictDoor -isnot [bool]) { throw 'Movement path-probe primitive fields are invalid.' }
        if ($RequireComplete -and $Record.accepted -ne $true) { throw 'PASS movement path-probe was not accepted.' }
        return
    }

    if ($Record.status -isnot [string] -or [string]$Record.status -cnotin @('PASS','FAIL')) { throw 'Movement row-result status is invalid.' }
    foreach ($name in @('assertionPassCount','assertionFailCount','synchronizationObservationCount','updateSynchronizationSampleCount',
        'lateUpdateSynchronizationSampleCount','updateSynchronizationCorrectionCount','lateUpdateSynchronizationCorrectionCount',
        'positionPhaseLagObservedCount','positionPhaseLagPermittedCount','positionPhaseLagSameFrameUpdateReferenceCount',
        'positionPhaseLagEligibleReferenceCount','positionPhaseLagViolationCount','positionPhaseLagRecoveryRequiredRawCount',
        'positionPhaseLagRecoveryUpdateRawCount','positionPhaseLagRecoverySatisfiedRawCount',
        'positionPhaseLagRecoveryRequiredEffectiveCount','positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount',
        'positionPhaseLagRecoverySatisfiedEffectiveCount','positionPhaseLagRecoveryViolationCount',
        'stationaryPositionCorrectionViolationCount','outstandingPositionPhaseLagRecoveryCount',
        'maximumConsecutiveUnrecoveredPositionPhaseLagCount',
        'phaseLagObservedCount','phaseLagPermittedCount','phaseLagSameFrameUpdateReferenceCount','phaseLagEligibleReferenceCount',
        'phaseLagViolationCount','phaseLagRecoveryRequiredCount','phaseLagRecoveryUpdateCount','phaseLagRecoverySatisfiedCount',
        'phaseLagRecoveryRequiredRawCount','phaseLagRecoveryUpdateRawCount','phaseLagRecoverySatisfiedRawCount',
        'phaseLagRecoveryRequiredEffectiveCount','phaseLagRecoveryUpdateOrBoundaryEffectiveCount',
        'phaseLagRecoverySatisfiedEffectiveCount',
        'phaseLagRecoveryViolationCount','stationaryYawCorrectionViolationCount','outstandingPhaseLagRecoveryCount',
        'maximumConsecutiveUnrecoveredPhaseLagCount','finalSynchronizationSnapshotFrame','finalSynchronizationAgentFrame',
        'finalSynchronizationSampleCount','finalSynchronizationOutstandingRecoveryCount',
        'finalSynchronizationOutstandingPositionRecoveryCount','finalSynchronizationBoundaryYawPendingBefore',
        'finalSynchronizationBoundaryPositionPendingBefore','finalSynchronizationBoundaryYawClosedCount',
        'finalSynchronizationBoundaryPositionClosedCount','finalSynchronizationBoundaryYawPendingAfter',
        'finalSynchronizationBoundaryPositionPendingAfter','stationaryBoundaryClosureAttemptCount',
        'stationaryBoundaryClosureSucceededCount','stationaryBoundaryClosureFailedCount',
        'yawPhaseLagStationaryBoundaryClosureCount','positionPhaseLagStationaryBoundaryClosureCount','oscillationCount',
        'unexpectedRepathCount','commandReplacementCount','selectionLossCount','waypointCount','endpointQualifiedWaypointCount',
        'nonPairInterferenceCount',
        'stopCommandIssuedCount')) {
        if (-not (Test-KmcExactJsonInteger $Record.$name) -or [long]$Record.$name -lt 0) { throw "Movement row-result $name must be a nonnegative exact JSON integer." }
    }
    foreach ($name in @('maximumPreCorrectionResidualWorldUnits','maximumInitialConfigurationResidualWorldUnits',
        'maximumUpdatePreCorrectionResidualWorldUnits','maximumLateUpdatePreCorrectionResidualWorldUnits',
        'maximumUpdatePreCorrectionRotationResidualDegrees','maximumLateUpdatePreCorrectionRotationResidualDegrees',
        'maximumPostCorrectionResidualWorldUnits','maximumPostCorrectionRotationResidualDegrees',
        'maximumRawCurrentPositionResidualWorldUnits','maximumUpdateRawCurrentPositionResidualWorldUnits',
        'maximumLateUpdateRawCurrentPositionResidualWorldUnits','maximumViewCurrentPositionResidualWorldUnits',
        'maximumEntityRawCurrentPositionResidualWorldUnits','maximumEntityPreviousAuthoritativePositionResidualWorldUnits',
        'maximumEntityPhaseAdjustedPositionResidualWorldUnits','maximumAuthoritativePositionDeltaWorldUnits',
        'maximumEntityRawPositionLagExcessWorldUnits','entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits',
        'maximumViewCurrentYawResidualDegrees',
        'maximumFullViewCurrentRotationResidualDegrees',
        'maximumMountEntityRootYawResidualDegrees','maximumEntityRawCurrentYawResidualDegrees',
        'maximumEntityPreviousAuthoritativeYawResidualDegrees','maximumEntityPhaseAdjustedYawResidualDegrees',
        'maximumAuthoritativeYawDeltaDegrees','maximumEntityRawLagExcessDegrees','entityRawLagArithmeticCoherenceEpsilonDegrees',
        'finalSynchronizationBoundaryPositionResidualWorldUnits','finalSynchronizationBoundaryViewPositionResidualWorldUnits',
        'finalSynchronizationBoundaryEntityPositionResidualWorldUnits','finalSynchronizationBoundaryFullViewRotationResidualDegrees',
        'finalSynchronizationBoundaryViewYawResidualDegrees','finalSynchronizationBoundaryEntityCurrentYawResidualDegrees',
        'finalSynchronizationBoundaryMountEntityRootYawResidualDegrees',
        'finalSynchronizationBoundaryAuthoritativePositionAdvanceWorldUnits',
        'finalSynchronizationBoundaryAuthoritativeYawAdvanceDegrees','maximumStationaryDriftWorldUnits',
        'maximumStuckSeconds','maximumCompletedLegFinalTargetDistanceWorldUnits',
        'maximumCompletedLegBestTargetDistanceWorldUnits','maximumTurnDegrees','mountFinalTargetDistanceWorldUnits','nonPairBestTargetDistanceWorldUnits',
        'nonPairFinalTargetDistanceWorldUnits','minimumPairNonPairSeparationWorldUnits','requiredPairNonPairSeparationWorldUnits',
        'pauseObservationSeconds','pauseMaximumDriftWorldUnits')) {
        if (-not (Test-KmcFiniteNonnegativeJsonNumber $Record.$name)) { throw "Movement row-result $name must be a finite nonnegative JSON number." }
    }
    foreach ($name in @('unmountedDoorControlPassed','cleanupSucceeded','cleanupResidual','finalSynchronizationSnapshotCaptured',
        'finalSynchronizationQualificationPassed','finalSynchronizationMovementStoppedBeforeSnapshot',
        'finalSynchronizationBoundaryMovementCommandAbsent','finalSynchronizationBoundaryWantsToMove',
        'finalSynchronizationBoundaryIsReallyMoving','finalSynchronizationBoundaryClosureAttempted',
        'finalSynchronizationBoundaryClosureSucceeded','doorApproachSkipped','restartCompleted','selectionMountNormalized',
        'selectionSwitchedAway','selectionSwitchedBack','formationSelectionNormalized','pauseEntered','pauseExited',
        'destinationCancelCommandAbsent','destinationCancelRelationshipPreserved')) {
        if ($Record.$name -isnot [bool]) { throw "Movement row-result $name must be a JSON boolean." }
    }
    foreach ($name in @('cleanupTrigger','cleanupResult','selectionCoverage','formationCoverage','finalSynchronizationSnapshotStage',
        'finalSynchronizationBoundaryClosureReason')) {
        if ($Record.$name -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Record.$name)) { throw "Movement row-result $name must be a nonempty JSON string." }
    }
    if ($null -ne $Record.door -and $Record.door -isnot [string]) { throw 'Movement row-result door must be a string or null.' }
    if ($null -ne $Record.nonPairUnitId -and ($Record.nonPairUnitId -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Record.nonPairUnitId))) {
        throw 'Movement row-result nonPairUnitId must be a nonempty string or null.'
    }
    Assert-KmcMovementVector3 $Record.doorNear 'movement row-result doorNear'
    Assert-KmcMovementVector3 $Record.doorFar 'movement row-result doorFar'
    Assert-KmcMovementCleanupState $Record.cleanupBefore 'movement row-result cleanupBefore' 'before' $RequireComplete
    Assert-KmcMovementCleanupState $Record.cleanupAfter 'movement row-result cleanupAfter' 'after' $RequireComplete
    Assert-KmcJsonStringArray $Record.screenshotCaptureErrors 'movement row-result screenshotCaptureErrors'
    Assert-KmcJsonStringArray $Record.errors 'movement row-result errors'
    if ($Record.screenshots -isnot [Array]) { throw 'Movement row-result screenshots must be an actual JSON array.' }
    foreach ($screenshot in @($Record.screenshots)) {
        Assert-KmcExactProperties $screenshot @('milestone','relativePath','length','sha256') 'movement screenshot evidence'
        if ($screenshot.milestone -isnot [string] -or $screenshot.relativePath -isnot [string] -or
            [string]$screenshot.relativePath -cnotmatch '^movement-visuals/[A-Za-z0-9._-]+\.png$' -or
            -not (Test-KmcExactJsonInteger $screenshot.length) -or [long]$screenshot.length -le 0 -or
            [string]$screenshot.sha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Movement screenshot evidence identity is invalid.' }
        $matches = @($Manifest.artifacts | Where-Object { [string]$_.relativePath -ceq [string]$screenshot.relativePath -and [string]$_.kind -ceq 'screenshot' })
        if ($matches.Count -ne 1 -or [long]$matches[0].length -ne [long]$screenshot.length -or [string]$matches[0].sha256 -cne [string]$screenshot.sha256) {
            throw 'Movement screenshot evidence does not reconcile with exactly one manifest record.'
        }
    }
    if ($RequireComplete) {
        if ([string]$Record.status -cne 'PASS' -or [long]$Record.assertionPassCount -le 0 -or
            [long]$Record.assertionFailCount -ne 0 -or @($Record.errors).Count -ne 0 -or
            $Record.cleanupSucceeded -ne $true -or $Record.cleanupResidual -ne $false) {
            throw 'PASS movement row-result contains failed assertions, errors, or cleanup residue.'
        }
        if ([double]$Record.maximumUpdatePreCorrectionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumLateUpdatePreCorrectionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumViewCurrentPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumEntityPhaseAdjustedPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumEntityRawPositionLagExcessWorldUnits -gt 0.0001 -or
            [double]$Record.maximumUpdatePreCorrectionRotationResidualDegrees -gt 0.10 -or
            [double]$Record.maximumLateUpdatePreCorrectionRotationResidualDegrees -gt 0.10 -or
            [double]$Record.maximumViewCurrentYawResidualDegrees -gt 0.10 -or
            [double]$Record.maximumFullViewCurrentRotationResidualDegrees -gt 0.10 -or
            [double]$Record.maximumMountEntityRootYawResidualDegrees -gt 0.10 -or
            [double]$Record.maximumEntityPhaseAdjustedYawResidualDegrees -gt 0.10 -or
            [double]$Record.maximumEntityRawLagExcessDegrees -gt 0.0001 -or
            [double]$Record.finalSynchronizationBoundaryPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.finalSynchronizationBoundaryViewPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.finalSynchronizationBoundaryEntityPositionResidualWorldUnits -gt 0.10 -or
            [double]$Record.finalSynchronizationBoundaryFullViewRotationResidualDegrees -gt 0.10 -or
            [double]$Record.finalSynchronizationBoundaryViewYawResidualDegrees -gt 0.10 -or
            [double]$Record.finalSynchronizationBoundaryEntityCurrentYawResidualDegrees -gt 0.10 -or
            [double]$Record.finalSynchronizationBoundaryMountEntityRootYawResidualDegrees -gt 0.10 -or
            [double]$Record.maximumPostCorrectionResidualWorldUnits -gt 0.10 -or
            [double]$Record.maximumPostCorrectionRotationResidualDegrees -gt 0.10) {
            throw 'PASS movement row-result exceeds the calibrated residual thresholds.'
        }
        if ([math]::Abs([double]$Record.entityRawPositionLagArithmeticCoherenceEpsilonWorldUnits - 0.0001) -gt 0.000000000001 -or
            [math]::Abs([double]$Record.entityRawLagArithmeticCoherenceEpsilonDegrees - 0.0001) -gt 0.000000000001) {
            throw 'PASS movement row-result changed a fixed raw-lag arithmetic coherence epsilon.'
        }
        if ([long]$Record.positionPhaseLagViolationCount -ne 0 -or
            [long]$Record.positionPhaseLagRecoveryViolationCount -ne 0 -or
            [long]$Record.stationaryPositionCorrectionViolationCount -ne 0 -or
            [long]$Record.outstandingPositionPhaseLagRecoveryCount -ne 0 -or
            [long]$Record.maximumConsecutiveUnrecoveredPositionPhaseLagCount -gt 1 -or
            [long]$Record.positionPhaseLagObservedCount -ne [long]$Record.positionPhaseLagPermittedCount -or
            [long]$Record.positionPhaseLagPermittedCount -ne [long]$Record.positionPhaseLagSameFrameUpdateReferenceCount -or
            [long]$Record.positionPhaseLagPermittedCount -ne [long]$Record.positionPhaseLagEligibleReferenceCount -or
            [long]$Record.positionPhaseLagRecoveryRequiredRawCount -ne [long]$Record.positionPhaseLagRecoveryUpdateRawCount -or
            [long]$Record.positionPhaseLagRecoveryUpdateRawCount -ne [long]$Record.positionPhaseLagRecoverySatisfiedRawCount -or
            [long]$Record.positionPhaseLagRecoveryRequiredEffectiveCount -ne
                ([long]$Record.positionPhaseLagRecoveryRequiredRawCount + [long]$Record.positionPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount -ne
                ([long]$Record.positionPhaseLagRecoveryUpdateRawCount + [long]$Record.positionPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.positionPhaseLagRecoverySatisfiedEffectiveCount -ne
                ([long]$Record.positionPhaseLagRecoverySatisfiedRawCount + [long]$Record.positionPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.positionPhaseLagPermittedCount -ne [long]$Record.positionPhaseLagRecoveryRequiredEffectiveCount -or
            [long]$Record.positionPhaseLagPermittedCount -ne [long]$Record.positionPhaseLagRecoveryUpdateOrBoundaryEffectiveCount -or
            [long]$Record.positionPhaseLagPermittedCount -ne [long]$Record.positionPhaseLagRecoverySatisfiedEffectiveCount) {
            throw 'PASS movement row-result does not prove complete same-frame position-lag recovery.'
        }
        if ([long]$Record.phaseLagRecoveryRequiredCount -ne [long]$Record.phaseLagRecoveryRequiredRawCount -or
            [long]$Record.phaseLagRecoveryUpdateCount -ne [long]$Record.phaseLagRecoveryUpdateRawCount -or
            [long]$Record.phaseLagRecoverySatisfiedCount -ne [long]$Record.phaseLagRecoverySatisfiedRawCount -or
            [long]$Record.phaseLagViolationCount -ne 0 -or [long]$Record.phaseLagRecoveryViolationCount -ne 0 -or
            [long]$Record.stationaryYawCorrectionViolationCount -ne 0 -or [long]$Record.outstandingPhaseLagRecoveryCount -ne 0 -or
            [long]$Record.maximumConsecutiveUnrecoveredPhaseLagCount -gt 1 -or
            [long]$Record.phaseLagObservedCount -ne [long]$Record.phaseLagPermittedCount -or
            [long]$Record.phaseLagPermittedCount -ne [long]$Record.phaseLagSameFrameUpdateReferenceCount -or
            [long]$Record.phaseLagPermittedCount -ne [long]$Record.phaseLagEligibleReferenceCount -or
            [long]$Record.phaseLagRecoveryRequiredRawCount -ne [long]$Record.phaseLagRecoveryUpdateRawCount -or
            [long]$Record.phaseLagRecoveryUpdateRawCount -ne [long]$Record.phaseLagRecoverySatisfiedRawCount -or
            [long]$Record.phaseLagRecoveryRequiredEffectiveCount -ne
                ([long]$Record.phaseLagRecoveryRequiredRawCount + [long]$Record.yawPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.phaseLagRecoveryUpdateOrBoundaryEffectiveCount -ne
                ([long]$Record.phaseLagRecoveryUpdateRawCount + [long]$Record.yawPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.phaseLagRecoverySatisfiedEffectiveCount -ne
                ([long]$Record.phaseLagRecoverySatisfiedRawCount + [long]$Record.yawPhaseLagStationaryBoundaryClosureCount) -or
            [long]$Record.phaseLagPermittedCount -ne [long]$Record.phaseLagRecoveryRequiredEffectiveCount -or
            [long]$Record.phaseLagPermittedCount -ne [long]$Record.phaseLagRecoveryUpdateOrBoundaryEffectiveCount -or
            [long]$Record.phaseLagPermittedCount -ne [long]$Record.phaseLagRecoverySatisfiedEffectiveCount) {
            throw 'PASS movement row-result does not prove complete same-frame phase-lag recovery.'
        }
        if ($Record.finalSynchronizationSnapshotCaptured -ne $true -or
            [string]$Record.finalSynchronizationSnapshotStage -cne 'pre-dismount-after-captures' -or
            $Record.finalSynchronizationQualificationPassed -ne $true -or
            $Record.finalSynchronizationMovementStoppedBeforeSnapshot -ne $true -or
            [long]$Record.finalSynchronizationSnapshotFrame -le 0 -or
            [long]$Record.finalSynchronizationAgentFrame -le 0 -or
            [long]$Record.finalSynchronizationSampleCount -le 0 -or
            [long]$Record.finalSynchronizationOutstandingRecoveryCount -ne 0 -or
            [long]$Record.finalSynchronizationOutstandingPositionRecoveryCount -ne 0 -or
            $Record.finalSynchronizationBoundaryMovementCommandAbsent -ne $true -or
            $Record.finalSynchronizationBoundaryWantsToMove -ne $false -or
            $Record.finalSynchronizationBoundaryIsReallyMoving -ne $false -or
            [double]$Record.finalSynchronizationBoundaryAuthoritativePositionAdvanceWorldUnits -gt 0.000001 -or
            [double]$Record.finalSynchronizationBoundaryAuthoritativeYawAdvanceDegrees -gt 0.000001) {
            throw 'PASS movement row-result does not prove an exact stopped stationary snapshot immediately before deconfiguration.'
        }
        $expectedBoundaryPositionResidual = [math]::Max(
            [double]$Record.finalSynchronizationBoundaryViewPositionResidualWorldUnits,
            [double]$Record.finalSynchronizationBoundaryEntityPositionResidualWorldUnits)
        if (-not (Test-KmcApproximatelyEqual ([double]$Record.finalSynchronizationBoundaryPositionResidualWorldUnits) $expectedBoundaryPositionResidual)) {
            throw 'PASS movement row-result final position boundary does not reconcile with its split view/entity residuals.'
        }
        $yawPendingBefore = [long]$Record.finalSynchronizationBoundaryYawPendingBefore
        $positionPendingBefore = [long]$Record.finalSynchronizationBoundaryPositionPendingBefore
        $expectedClosureAttempted = $yawPendingBefore -gt 0L -or $positionPendingBefore -gt 0L
        if ($yawPendingBefore -gt 1L -or $positionPendingBefore -gt 1L -or
            [long]$Record.finalSynchronizationBoundaryYawClosedCount -ne $yawPendingBefore -or
            [long]$Record.finalSynchronizationBoundaryPositionClosedCount -ne $positionPendingBefore -or
            [long]$Record.finalSynchronizationBoundaryYawPendingAfter -ne 0L -or
            [long]$Record.finalSynchronizationBoundaryPositionPendingAfter -ne 0L -or
            $Record.finalSynchronizationBoundaryClosureAttempted -ne $expectedClosureAttempted -or
            $Record.finalSynchronizationBoundaryClosureSucceeded -ne $true -or
            [string]$Record.finalSynchronizationBoundaryClosureReason -cne
                $(if ($expectedClosureAttempted) { 'closed-at-stationary-boundary' } else { 'no-pending-recovery' }) -or
            [long]$Record.stationaryBoundaryClosureAttemptCount -ne $(if ($expectedClosureAttempted) { 1L } else { 0L }) -or
            [long]$Record.stationaryBoundaryClosureSucceededCount -ne $(if ($expectedClosureAttempted) { 1L } else { 0L }) -or
            [long]$Record.stationaryBoundaryClosureFailedCount -ne 0L -or
            [long]$Record.yawPhaseLagStationaryBoundaryClosureCount -ne $yawPendingBefore -or
            [long]$Record.positionPhaseLagStationaryBoundaryClosureCount -ne $positionPendingBefore) {
            throw 'PASS movement row-result stationary-boundary closure does not reconcile exactly.'
        }
        if ([long]$Record.synchronizationObservationCount -le 0 -or [long]$Record.updateSynchronizationSampleCount -le 0 -or
            [long]$Record.lateUpdateSynchronizationSampleCount -le 0 -or
            [long]$Record.updateSynchronizationCorrectionCount -gt [long]$Record.updateSynchronizationSampleCount -or
            [long]$Record.lateUpdateSynchronizationCorrectionCount -gt [long]$Record.lateUpdateSynchronizationSampleCount -or
            [long]$Record.waypointCount -le 0) { throw 'PASS movement row-result lacks bounded synchronization or navigation samples.' }
        $maximumPhaseSamples = [long]$Record.synchronizationObservationCount + 2L
        $maximumCalibratedCorrections = $maximumPhaseSamples * 2L
        if ([long]$Record.updateSynchronizationSampleCount -gt $maximumPhaseSamples -or
            [long]$Record.lateUpdateSynchronizationSampleCount -gt $maximumPhaseSamples -or
            ([long]$Record.updateSynchronizationCorrectionCount + [long]$Record.lateUpdateSynchronizationCorrectionCount) -gt $maximumCalibratedCorrections) {
            throw 'PASS movement row-result exceeds the bounded synchronization callback cadence.'
        }
        if ([long]$Record.commandReplacementCount -ne 0 -or [long]$Record.selectionLossCount -ne 0 -or
            [long]$Record.nonPairInterferenceCount -ne 0) {
            throw 'PASS movement row permits a command replacement, selection loss, or unselected non-pair interference event.'
        }

        $expectedWaypointCount = switch ([string]$Record.row) {
            'mounted-pair-doorway' { if ($Record.doorApproachSkipped) { 2L } else { 3L }; break }
            'mounted-pair-open-ground' { 1L; break }
            'mounted-pair-stop-start' { 2L; break }
            'mounted-pair-turns-and-corners' { 3L; break }
            'mounted-pair-selection' { 1L; break }
            'mounted-pair-party-formation' { 1L; break }
            'mounted-pair-pause-unpause' { 1L; break }
            'mounted-pair-destination-cancel' { 1L; break }
            default { throw 'PASS movement row has no exact waypoint contract.' }
        }
        if ([long]$Record.waypointCount -ne $expectedWaypointCount) {
            throw "PASS movement row does not contain its exact $expectedWaypointCount-leg navigation proof."
        }

        $expectedEndpointQualifiedWaypointCount = switch ([string]$Record.row) {
            'mounted-pair-stop-start' { 1L; break }
            'mounted-pair-destination-cancel' { 0L; break }
            default { $expectedWaypointCount; break }
        }
        if ([long]$Record.endpointQualifiedWaypointCount -ne $expectedEndpointQualifiedWaypointCount) {
            throw "PASS movement row does not contain its exact $expectedEndpointQualifiedWaypointCount endpoint-qualified leg proof."
        }
        if ($expectedEndpointQualifiedWaypointCount -eq 0L) {
            if ([double]$Record.maximumCompletedLegFinalTargetDistanceWorldUnits -ne 0.0 -or
                [double]$Record.maximumCompletedLegBestTargetDistanceWorldUnits -ne 0.0) {
                throw 'PASS destination-cancel row retained endpoint evidence for its intentionally interrupted leg.'
            }
        }
        elseif ([double]$Record.maximumCompletedLegFinalTargetDistanceWorldUnits -gt 1.25 -or
            [double]$Record.maximumCompletedLegBestTargetDistanceWorldUnits -gt 1.25 -or
            [double]$Record.mountFinalTargetDistanceWorldUnits -gt
                [double]$Record.maximumCompletedLegFinalTargetDistanceWorldUnits) {
            throw 'PASS movement row does not prove every endpoint-qualified leg finished within the fixed 1.25-unit final/best target-distance gate.'
        }

        $isDoorway = [string]$Record.row -ceq 'mounted-pair-doorway'
        $isStopStart = [string]$Record.row -ceq 'mounted-pair-stop-start'
        $isTurns = [string]$Record.row -ceq 'mounted-pair-turns-and-corners'
        $isSelection = [string]$Record.row -ceq 'mounted-pair-selection'
        $isFormation = [string]$Record.row -ceq 'mounted-pair-party-formation'
        $isPause = [string]$Record.row -ceq 'mounted-pair-pause-unpause'
        $isCancel = [string]$Record.row -ceq 'mounted-pair-destination-cancel'
        if ($isDoorway) {
            if ($Record.unmountedDoorControlPassed -ne $true -or [string]::IsNullOrWhiteSpace([string]$Record.door)) {
                throw 'PASS doorway row does not contain the required matched unmounted Mammoth control and exact unchanged door identity.'
            }
        }
        elseif ($Record.unmountedDoorControlPassed -ne $false -or $Record.doorApproachSkipped -ne $false -or $null -ne $Record.door) {
            throw 'Non-doorway PASS row retained doorway-only semantic evidence.'
        }
        if ($isStopStart) {
            if ([long]$Record.stopCommandIssuedCount -ne 1 -or $Record.restartCompleted -ne $true) {
                throw 'PASS stop/start row does not prove exactly one routed stop followed by a completed restart.'
            }
        }
        elseif ($isCancel) {
            if ([long]$Record.stopCommandIssuedCount -ne 1 -or $Record.restartCompleted -ne $false -or
                $Record.destinationCancelCommandAbsent -ne $true -or $Record.destinationCancelRelationshipPreserved -ne $true) {
                throw 'PASS destination-cancel row does not prove exactly one stop, absent destination, and preserved mounted relationship.'
            }
        }
        elseif ([long]$Record.stopCommandIssuedCount -ne 0 -or $Record.restartCompleted -ne $false) {
            throw 'PASS movement row retained stop/start-only semantic evidence.'
        }
        if (-not $isCancel -and ($Record.destinationCancelCommandAbsent -ne $false -or $Record.destinationCancelRelationshipPreserved -ne $false)) {
            throw 'Non-cancel PASS row retained destination-cancel-only semantic evidence.'
        }
        if ($isTurns) {
            if ([double]$Record.maximumTurnDegrees -lt 75.0) { throw 'PASS turns/corners row lacks the required measured 75-degree turn.' }
        }
        elseif ([double]$Record.maximumTurnDegrees -ne 0.0) {
            throw 'Non-turn PASS row retained turns/corners-only semantic evidence.'
        }
        if ($isSelection) {
            if ($Record.selectionMountNormalized -ne $true -or $Record.selectionSwitchedAway -ne $true -or
                $Record.selectionSwitchedBack -ne $true -or [string]::IsNullOrWhiteSpace([string]$Record.nonPairUnitId)) {
                throw 'PASS selection row does not prove mount normalization and the exact non-pair away/back switch.'
            }
        }
        elseif ($Record.selectionMountNormalized -ne $false -or $Record.selectionSwitchedAway -ne $false -or $Record.selectionSwitchedBack -ne $false) {
            throw 'Non-selection PASS row retained selection-only semantic evidence.'
        }
        if ($isFormation) {
            if ($Record.formationSelectionNormalized -ne $true -or [string]::IsNullOrWhiteSpace([string]$Record.nonPairUnitId) -or
                [double]$Record.mountFinalTargetDistanceWorldUnits -gt 1.25 -or
                [double]$Record.nonPairBestTargetDistanceWorldUnits -gt 1.25 -or
                [double]$Record.nonPairFinalTargetDistanceWorldUnits -gt 1.25 -or
                [double]$Record.requiredPairNonPairSeparationWorldUnits -le 0.0 -or
                [double]$Record.minimumPairNonPairSeparationWorldUnits -lt [double]$Record.requiredPairNonPairSeparationWorldUnits) {
                throw 'PASS formation row does not prove normalized rider/non-pair selection, both recipients at target, and corpulence clearance.'
            }
        }
        elseif ($Record.formationSelectionNormalized -ne $false) {
            throw 'Non-formation PASS row retained formation-only semantic evidence.'
        }
        if (-not $isSelection -and -not $isFormation -and $null -ne $Record.nonPairUnitId) {
            throw 'PASS movement row retained a non-pair identity outside selection or formation qualification.'
        }
        if ($isPause) {
            if ($Record.pauseEntered -ne $true -or $Record.pauseExited -ne $true -or
                [double]$Record.pauseObservationSeconds -lt 1.0 -or [double]$Record.pauseMaximumDriftWorldUnits -gt 0.15) {
                throw 'PASS pause/unpause row does not prove entry, a one-second real-clock observation, bounded drift, and exit.'
            }
        }
        elseif ($Record.pauseEntered -ne $false -or $Record.pauseExited -ne $false -or
            [double]$Record.pauseObservationSeconds -ne 0.0 -or [double]$Record.pauseMaximumDriftWorldUnits -ne 0.0) {
            throw 'Non-pause PASS row retained pause-only semantic evidence.'
        }

        [string[]]$expectedScreenshotMilestones = switch ([string]$Record.row) {
            'mounted-pair-doorway' {
                if ($Record.doorApproachSkipped) { @('door-control','door-mounted','door-mounted','dismounted') }
                else { @('door-control','door-control','door-mounted','door-mounted','dismounted') }
                break
            }
            'mounted-pair-open-ground' { @('mounted-idle','moving','stopped','dismounted'); break }
            'mounted-pair-stop-start' { @('mounted-idle','moving','stopped','restarted','dismounted'); break }
            'mounted-pair-turns-and-corners' { @('mounted-idle','moving','corner','corner','dismounted'); break }
            'mounted-pair-selection' { @('mounted-idle','selection','moving','dismounted'); break }
            'mounted-pair-party-formation' { @('mounted-idle','formation','formation','dismounted'); break }
            'mounted-pair-pause-unpause' { @('mounted-idle','moving','paused','dismounted'); break }
            'mounted-pair-destination-cancel' { @('mounted-idle','moving','cancelled','dismounted'); break }
        }
        [string[]]$actualScreenshotMilestones = @($Record.screenshots | ForEach-Object { [string]$_.milestone })
        if ($actualScreenshotMilestones.Count -ne $expectedScreenshotMilestones.Count -or
            ($actualScreenshotMilestones -join "`n") -cne ($expectedScreenshotMilestones -join "`n") -or
            @($Record.screenshotCaptureErrors).Count -ne 0) {
            throw 'PASS movement row lacks its exact ordered screenshot milestone/count coverage or contains a capture error.'
        }
        $screenshotMilestoneCounts = @{}
        $screenshotRowToken = ([string]$Record.row).Substring('mounted-pair-'.Length)
        foreach ($screenshot in @($Record.screenshots)) {
            $milestone = [string]$screenshot.milestone
            $milestoneCount = if ($screenshotMilestoneCounts.ContainsKey($milestone)) { [int]$screenshotMilestoneCounts[$milestone] + 1 } else { 1 }
            $screenshotMilestoneCounts[$milestone] = $milestoneCount
            $expectedRelativePath = 'movement-visuals/' + $screenshotRowToken + '-' + $milestone + '-' + $milestoneCount.ToString('00') + '.png'
            if ([string]$screenshot.relativePath -cne $expectedRelativePath) {
                throw 'PASS movement screenshot is not bound to its exact row, milestone, and deterministic capture ordinal.'
            }
        }
    }
}

function Assert-KmcMovementScenarioEvidence {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$Manifest,
        [AllowNull()][string]$Status,
        $SubscenarioResults
    )
    $evidenceRoot = [IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')
    Assert-KmcKnownRuntimeArtifactsManifested $evidenceRoot $Manifest
    $isMovement = Test-KmcMovementRuntimeScenario ([string]$Request.scenario)
    $requireComplete = $isMovement -and [string]$Status -ceq 'PASS'
    $telemetryArtifacts = @($Manifest.artifacts | Where-Object { [string]$_.relativePath -ceq 'movement-telemetry.jsonl' })
    $scenarioArtifacts = @($Manifest.artifacts | Where-Object { [string]$_.relativePath -ceq 'movement-scenario-evidence.jsonl' })
    if (-not $isMovement -and ($telemetryArtifacts.Count -ne 0 -or $scenarioArtifacts.Count -ne 0)) { throw 'Movement evidence is present for a non-movement runtime scenario.' }
    if ($requireComplete -and ($telemetryArtifacts.Count -ne 1 -or $scenarioArtifacts.Count -ne 1)) {
        throw 'PASS movement scenario requires exactly one manifested telemetry JSONL and one manifested scenario-evidence JSONL.'
    }
    if ($telemetryArtifacts.Count -gt 1 -or ($telemetryArtifacts.Count -eq 1 -and [string]$telemetryArtifacts[0].kind -cne 'telemetry')) { throw 'Movement telemetry manifest identity is not exact.' }
    if ($scenarioArtifacts.Count -gt 1 -or ($scenarioArtifacts.Count -eq 1 -and [string]$scenarioArtifacts[0].kind -cne 'scenario-evidence')) { throw 'Movement scenario-evidence manifest identity is not exact.' }
    if (-not $isMovement -or ($telemetryArtifacts.Count -eq 0 -and $scenarioArtifacts.Count -eq 0)) { return }

    [string[]]$expectedRows = if ([string]$Request.scenario -ceq 'movement-suite') { @(Get-KmcMovementRuntimeRows) } else { @([string]$Request.scenario) }
    $telemetryRecords = New-Object 'Collections.Generic.List[object]'
    if ($telemetryArtifacts.Count -eq 1) {
        $telemetryPath = Assert-KmcChildPath (Join-Path $evidenceRoot 'movement-telemetry.jsonl') $evidenceRoot 'movement telemetry'
        Assert-KmcNotReparsePoint $telemetryPath 'movement telemetry'; Assert-KmcNotHardLink $telemetryPath 'movement telemetry'
        foreach ($line in @([IO.File]::ReadAllLines($telemetryPath, (New-Object Text.UTF8Encoding($false, $true))))) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            Assert-KmcJsonObjectMembersUnique $line 'movement telemetry line'
            try { $record = $line | ConvertFrom-Json } catch { throw "Movement telemetry line is malformed JSON: $($_.Exception.Message)" }
            Assert-KmcMovementTelemetryRecord $record $Request $telemetryRecords.Count $expectedRows $requireComplete
            $telemetryRecords.Add($record)
        }
    }
    if ($requireComplete -and $telemetryRecords.Count -eq 0) { throw 'PASS movement telemetry contains no nonblank JSON records.' }

    $scenarioRecords = New-Object 'Collections.Generic.List[object]'
    if ($scenarioArtifacts.Count -eq 1) {
        $scenarioPath = Assert-KmcChildPath (Join-Path $evidenceRoot 'movement-scenario-evidence.jsonl') $evidenceRoot 'movement scenario evidence'
        Assert-KmcNotReparsePoint $scenarioPath 'movement scenario evidence'; Assert-KmcNotHardLink $scenarioPath 'movement scenario evidence'
        foreach ($line in @([IO.File]::ReadAllLines($scenarioPath, (New-Object Text.UTF8Encoding($false, $true))))) {
            if ([string]::IsNullOrWhiteSpace($line)) { continue }
            Assert-KmcJsonObjectMembersUnique $line 'movement scenario evidence line'
            try { $record = $line | ConvertFrom-Json } catch { throw "Movement scenario evidence line is malformed JSON: $($_.Exception.Message)" }
            Assert-KmcMovementScenarioRecord $record $Request $scenarioRecords.Count $expectedRows $requireComplete $Manifest
            $scenarioRecords.Add($record)
        }
    }
    if ($requireComplete -and $scenarioRecords.Count -eq 0) { throw 'PASS movement scenario evidence contains no nonblank JSON records.' }

    $lastTelemetryRow = -1
    $telemetryRows = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $stableRider = $null; $stableMount = $null
    foreach ($record in $telemetryRecords) {
        $rowPosition = [Array]::IndexOf($expectedRows, [string]$record.row)
        if ($rowPosition -lt $lastTelemetryRow) { throw 'Movement telemetry row order regressed.' }
        $lastTelemetryRow = $rowPosition; [void]$telemetryRows.Add([string]$record.row)
        if ($null -eq $stableRider) { $stableRider = [string]$record.riderId; $stableMount = [string]$record.mountId }
        elseif ([string]$record.riderId -cne $stableRider -or [string]$record.mountId -cne $stableMount) { throw 'Movement telemetry rider or mount stable identity changed within the run.' }
    }

    $lastScenarioRow = -1
    $rowResults = New-Object 'Collections.Generic.List[object]'
    $pathProbeRows = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($record in $scenarioRecords) {
        $rowPosition = [Array]::IndexOf($expectedRows, [string]$record.row)
        if ($rowPosition -lt $lastScenarioRow) { throw 'Movement scenario evidence row order regressed.' }
        $lastScenarioRow = $rowPosition
        if ([string]$record.kind -ceq 'movement-row-result') { $rowResults.Add($record) } else { [void]$pathProbeRows.Add([string]$record.row) }
    }
    if ($requireComplete) {
        if ($rowResults.Count -ne $expectedRows.Count) { throw 'PASS movement evidence does not contain exactly one row-result for every expected row.' }
        for ($index = 0; $index -lt $expectedRows.Count; $index++) {
            if ([string]$rowResults[$index].row -cne [string]$expectedRows[$index] -or -not $telemetryRows.Contains([string]$expectedRows[$index]) -or
                -not $pathProbeRows.Contains([string]$expectedRows[$index])) { throw "PASS movement evidence lacks exact ordered row, telemetry, or path-probe coverage for $($expectedRows[$index])." }
            $rowRecord = $rowResults[$index]
            $rowPathProbes = @($scenarioRecords | Where-Object { [string]$_.kind -ceq 'path-probe' -and [string]$_.row -ceq [string]$rowRecord.row })
            if ($rowPathProbes.Count -ne [long]$rowRecord.waypointCount) {
                throw "PASS movement evidence path-probe count does not equal the exact completed waypoint count for $($rowRecord.row)."
            }
            if ([string]$rowRecord.row -ceq 'mounted-pair-doorway') {
                [bool[]]$expectedStrictDoor = if ($rowRecord.doorApproachSkipped) { @($true,$true) } else { @($false,$true,$true) }
                $expectedDoorTargets = if ($rowRecord.doorApproachSkipped) {
                    @($rowRecord.doorFar,$rowRecord.doorNear)
                }
                else {
                    @($rowRecord.doorNear,$rowRecord.doorFar,$rowRecord.doorNear)
                }
                if ($rowPathProbes.Count -ne $expectedStrictDoor.Count) { throw 'PASS doorway path-probe count does not match its bounded approach policy.' }
                for ($probeIndex = 0; $probeIndex -lt $rowPathProbes.Count; $probeIndex++) {
                    $probe = $rowPathProbes[$probeIndex]
                    $target = $expectedDoorTargets[$probeIndex]
                    if ($probe.strictDoor -ne $expectedStrictDoor[$probeIndex] -or
                        -not (Test-KmcApproximatelyEqual ([double]$probe.requested.x) ([double]$target.x) 0.000001) -or
                        -not (Test-KmcApproximatelyEqual ([double]$probe.requested.y) ([double]$target.y) 0.000001) -or
                        -not (Test-KmcApproximatelyEqual ([double]$probe.requested.z) ([double]$target.z) 0.000001)) {
                        throw 'PASS doorway path probes do not prove the exact near/far/near same-geometry unmounted and mounted traversal sequence.'
                    }
                }
            }
            elseif (@($rowPathProbes | Where-Object { $_.strictDoor -ne $false }).Count -ne 0) {
                throw "Non-doorway PASS row contains a strict-door path probe: $($rowRecord.row)."
            }
        }
        $referencedScreenshots = @($rowResults | ForEach-Object { @($_.screenshots) })
        $manifestScreenshots = @($Manifest.artifacts | Where-Object { [string]$_.kind -ceq 'screenshot' })
        if ($referencedScreenshots.Count -ne $manifestScreenshots.Count) {
            throw 'PASS movement screenshot evidence is not bidirectionally complete with the manifested screenshot artifacts.'
        }
        foreach ($artifact in $manifestScreenshots) {
            $matches = @($referencedScreenshots | Where-Object {
                [string]$_.relativePath -ceq [string]$artifact.relativePath -and
                [long]$_.length -eq [long]$artifact.length -and
                [string]$_.sha256 -ceq [string]$artifact.sha256
            })
            if ($matches.Count -ne 1) { throw 'PASS movement manifest contains an orphan, duplicate, or mismatched screenshot artifact.' }
        }
    }
    if ($null -ne $SubscenarioResults) {
        $subresults = @($SubscenarioResults)
        if ($requireComplete -and $subresults.Count -ne $expectedRows.Count) { throw 'PASS movement subresult count does not equal the exact expected row count.' }
        for ($index = 0; $index -lt $rowResults.Count; $index++) {
            $record = $rowResults[$index]
            $matches = @($subresults | Where-Object { [string]$_.name -ceq [string]$record.row })
            if ($matches.Count -ne 1) { throw "Movement row-result does not map to exactly one game subresult: $($record.row)" }
            $subresult = $matches[0]
            if ([string]$record.status -cne [string]$subresult.status -or
                [long]$record.assertionPassCount -ne [long]$subresult.assertionPassCount -or
                [long]$record.assertionFailCount -ne [long]$subresult.assertionFailCount -or
                (@($record.errors) -join "`n") -cne (@($subresult.errors) -join "`n")) { throw "Movement row-result does not reconcile with the game subresult: $($record.row)" }
        }
        if ($requireComplete) {
            for ($index = 0; $index -lt $expectedRows.Count; $index++) {
                if ([string]$subresults[$index].name -cne [string]$expectedRows[$index]) { throw 'PASS movement game subresults are not in the exact expected row order.' }
            }
        }
    }
}

function Get-KmcValidatedOrchestrationArtifactManifestHash {
    param([Parameter(Mandatory = $true)]$Request)
    $evidenceRoot = [IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $evidenceRoot -PathType Container)) {
        New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
    }
    Assert-KmcNotReparsePoint $evidenceRoot 'runtime evidence root'
    $manifestPath = Assert-KmcChildPath (Join-Path $evidenceRoot 'runtime-artifacts.json') $evidenceRoot 'orchestration artifact manifest'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        $manifest = [ordered]@{
            schemaVersion = 1
            runId = [string]$Request.runId
            scenario = [string]$Request.scenario
            createdAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
            artifacts = @()
        }
        Write-KmcJsonDurable -Path $manifestPath -Value $manifest
    }
    Assert-KmcNotReparsePoint $manifestPath 'orchestration artifact manifest'
    Assert-KmcNotHardLink $manifestPath 'orchestration artifact manifest'
    $before = Get-Item -LiteralPath $manifestPath -Force
    $manifestValue = Read-KmcJson $manifestPath
    Assert-KmcExactProperties $manifestValue @('schemaVersion','runId','scenario','createdAtUtc','artifacts') 'orchestration artifact manifest'
    if ([int]$manifestValue.schemaVersion -ne 1 -or [string]$manifestValue.runId -cne [string]$Request.runId -or
        [string]$manifestValue.scenario -cne [string]$Request.scenario -or $manifestValue.artifacts -isnot [Array]) {
        throw 'Orchestration artifact manifest identity or shape is invalid.'
    }
    $createdAt = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$manifestValue.createdAtUtc, [ref]$createdAt)) {
        throw 'Orchestration artifact manifest timestamp is invalid.'
    }
    $seen = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($artifact in @($manifestValue.artifacts)) {
        Assert-KmcExactProperties $artifact @('relativePath','kind','length','sha256') 'orchestration artifact manifest record'
        $relative = [string]$artifact.relativePath
        $kind = [string]$artifact.kind
        $allowed = ($relative -ceq 'lifecycle-scenario-evidence.jsonl' -and $kind -ceq 'scenario-evidence') -or
            ($relative -ceq 'movement-telemetry.jsonl' -and $kind -ceq 'telemetry') -or
            ($relative -ceq 'movement-scenario-evidence.jsonl' -and $kind -ceq 'scenario-evidence') -or
            ($relative -cmatch '^movement-visuals/[A-Za-z0-9._-]+\.png$' -and $kind -ceq 'screenshot')
        if (-not $seen.Add($relative) -or -not $allowed -or [long]$artifact.length -le 0 -or
            [string]$artifact.sha256 -cnotmatch '^[0-9a-f]{64}$') {
            throw 'Orchestration artifact manifest contains an invalid or duplicate record.'
        }
        $artifactPath = Assert-KmcChildPath (Join-Path $evidenceRoot $relative.Replace('/','\')) $evidenceRoot 'orchestration runtime artifact'
        if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) { throw "Orchestration runtime artifact is missing: $relative" }
        Assert-KmcNotReparsePoint $artifactPath 'orchestration runtime artifact'
        Assert-KmcNotHardLink $artifactPath 'orchestration runtime artifact'
        $file = Get-Item -LiteralPath $artifactPath -Force
        if ($file.Length -ne [long]$artifact.length -or (Get-KmcSha256 $artifactPath) -cne [string]$artifact.sha256) {
            throw "Orchestration runtime artifact differs from its manifest: $relative"
        }
    }
    Assert-KmcLifecycleScenarioEvidence -Request $Request -Manifest $manifestValue
    Assert-KmcMovementScenarioEvidence -Request $Request -Manifest $manifestValue
    $hash = Get-KmcSha256 $manifestPath
    $after = Get-Item -LiteralPath $manifestPath -Force
    if ($after.Length -ne $before.Length -or $after.LastWriteTimeUtc.Ticks -ne $before.LastWriteTimeUtc.Ticks) {
        throw 'Orchestration artifact manifest changed while being validated.'
    }
    return $hash
}

function New-KmcRuntimeFixturePayload {
    param([Parameter(Mandatory = $true)]$Pair)

    function New-Descriptor($Value) {
        return [ordered]@{
            internalName = [string]$Value.name
            fileName = [string]$Value.fileName
            sha256 = [string]$Value.sha256
            length = [long]$Value.length
            lastWriteTimeUtcTicks = [long]$Value.lastWriteTimeUtcTicks
            gameId = [string]$Value.gameId
            gameName = [string]$Value.gameName
            area = [string]$Value.area
        }
    }

    return [ordered]@{
        baseline = New-Descriptor $Pair.baseline
        working = New-Descriptor $Pair.working
        writeAuthorization = [ordered]@{
            mode = 'working-only'
            allowedInternalName = 'KMC_AUTOMATION_WORKING'
            allowedFileName = [string]$Pair.working.fileName
            baselineImmutable = $true
        }
    }
}

function Get-KmcRunTransactionStatePath {
    param(
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$RunId
    )
    $transactionRoot = Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'run-transactions'
    return Assert-KmcChildPath (Join-Path $transactionRoot ($RunId + '.json')) $transactionRoot 'combined runtime transaction state'
}

function New-KmcRunTransactionState {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)][ValidateSet('no-save-v1','save-backed-v2')][string]$Mode,
        [Parameter(Mandatory = $true)][string]$LiveModsRoot,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)]$ModsBefore,
        [Parameter(Mandatory = $true)]$SavesBefore
    )
    [void](Assert-KmcRuntimeLockOwner $Lock)
    Assert-KmcNoGameProcesses
    $statePath = Get-KmcRunTransactionStatePath $StateRoot ([string]$Lock.RunId)
    if (Test-Path -LiteralPath $statePath) { throw "Run ID already has combined transaction state: $($Lock.RunId)" }
    $state = [ordered]@{
        schemaVersion = 1
        runId = [string]$Lock.RunId
        token = [string]$Lock.Token
        mode = $Mode
        phase = 'prepared'
        preparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        liveModsRoot = [IO.Path]::GetFullPath($LiveModsRoot).TrimEnd('\')
        saveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
        modsDigestBefore = [string]$ModsBefore.digest
        saveInventoryDigestBefore = [string]$SavesBefore.digest
    }
    Write-KmcJsonAtomic $statePath $state
    return $statePath
}

function Read-KmcRunTransactionState {
    param(
        [Parameter(Mandatory = $true)][string]$StatePath,
        $Lock
    )
    $state = Read-KmcJson $StatePath
    $required = @(
        'schemaVersion','runId','token','mode','phase','preparedAtUtc','liveModsRoot','saveRoot',
        'modsDigestBefore','saveInventoryDigestBefore'
    )
    $allowed = @($required + @(
        'restoreAttemptedAtUtc','modsRestored','saveProtectionPassed','baselineImmutable','workingRestored',
        'saveWriteAllowlistPassed','restoredModsDigest','restoredSaveInventoryDigest','restorationErrors','restoredAtUtc'
    ))
    $actual = @($state.PSObject.Properties.Name)
    if (@($required | Where-Object { $_ -cnotin $actual }).Count -ne 0 -or
        @($actual | Where-Object { $_ -cnotin $allowed }).Count -ne 0) {
        throw 'Combined runtime transaction state is missing required fields or contains unknown fields.'
    }
    if ([int]$state.schemaVersion -ne 1 -or [string]$state.runId -cnotmatch '^[A-Za-z0-9._-]{1,120}$' -or
        [string]$state.token -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$state.mode -cnotin @('no-save-v1','save-backed-v2') -or
        [string]$state.phase -cnotin @('prepared','restoration-attempted','restored') -or
        [string]$state.modsDigestBefore -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$state.saveInventoryDigestBefore -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Combined runtime transaction state contains an invalid identity, mode, phase, or digest.'
    }
    if ($null -ne $Lock -and ([string]$state.runId -cne [string]$Lock.RunId -or [string]$state.token -cne [string]$Lock.Token)) {
        throw 'Combined runtime transaction state ownership does not match the lock.'
    }
    return $state
}

function Restore-KmcRuntimeTransactions {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)][string]$CombinedStatePath,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$BackupRoot,
        [Parameter(Mandatory = $true)][string]$StagingRoot
    )
    [void](Assert-KmcRuntimeLockOwner $Lock)
    Assert-KmcNoGameProcesses
    $state = Read-KmcRunTransactionState -StatePath $CombinedStatePath -Lock $Lock
    $liveModsRoot = [IO.Path]::GetFullPath([string]$state.liveModsRoot).TrimEnd('\')
    $saveRoot = [IO.Path]::GetFullPath([string]$state.saveRoot).TrimEnd('\')
    $modsStatePath = Get-KmcTransactionStatePath $StateRoot ([string]$Lock.RunId)
    $saveStatePath = Get-KmcSaveTransactionStatePath $StateRoot ([string]$Lock.RunId)
    $errors = New-Object 'System.Collections.Generic.List[string]'
    $modsRestored = $false
    $baselineImmutable = $false
    $workingRestored = $false
    $saveWriteAllowlistPassed = $false
    $restoredModsDigest = '0' * 64
    $restoredSaveDigest = '0' * 64

    # Save and Mods restoration are deliberately independent. Failure in one must
    # not prevent the other owned external tree from being put back when safe.
    try {
        if (Test-Path -LiteralPath $saveStatePath -PathType Leaf) {
            $save = Restore-KmcWorkingSaveTransaction -Lock $Lock -StatePath $saveStatePath -SaveRoot $saveRoot -BackupRoot $BackupRoot -StagingRoot $StagingRoot
            $baselineImmutable = [bool]$save.baselineImmutable
            $workingRestored = [bool]$save.workingRestored
            $saveWriteAllowlistPassed = [bool]$save.saveWriteAllowlistPassed
            $restoredSaveDigest = [string]$save.restoredInventoryDigest
        }
        else {
            $currentSaves = Get-KmcSaveMetadataInventory $saveRoot
            $restoredSaveDigest = [string]$currentSaves.digest
            $saveExact = $restoredSaveDigest -ceq [string]$state.saveInventoryDigestBefore
            $baselineImmutable = $saveExact
            $workingRestored = $saveExact
            $saveWriteAllowlistPassed = $saveExact
            if (-not $saveExact) { throw 'Save metadata changed although no owned Working-save transaction state exists.' }
        }
    }
    catch { $errors.Add('Working-save restoration failed: ' + $_.Exception.Message) }

    try {
        if (Test-Path -LiteralPath $modsStatePath -PathType Leaf) {
            $mods = Restore-KmcModsTransaction -Lock $Lock -StatePath $modsStatePath -LiveModsRoot $liveModsRoot -BackupRoot $BackupRoot -StagingRoot $StagingRoot
        }
        else { $mods = Get-KmcDirectoryManifest $liveModsRoot }
        $restoredModsDigest = [string]$mods.digest
        $modsRestored = $restoredModsDigest -ceq [string]$state.modsDigestBefore
        if (-not $modsRestored) { throw 'Restored Mods digest differs from the combined preflight digest.' }
    }
    catch { $errors.Add('Mods restoration failed: ' + $_.Exception.Message) }

    $saveProtectionPassed = $baselineImmutable -and $workingRestored -and $saveWriteAllowlistPassed -and
        $restoredSaveDigest -ceq [string]$state.saveInventoryDigestBefore
    $state.phase = if ($modsRestored -and $saveProtectionPassed -and $errors.Count -eq 0) { 'restored' } else { 'restoration-attempted' }
    foreach ($entry in ([ordered]@{
        restoreAttemptedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        modsRestored = $modsRestored
        saveProtectionPassed = $saveProtectionPassed
        baselineImmutable = $baselineImmutable
        workingRestored = $workingRestored
        saveWriteAllowlistPassed = $saveWriteAllowlistPassed
        restoredModsDigest = $restoredModsDigest
        restoredSaveInventoryDigest = $restoredSaveDigest
        restorationErrors = $errors.ToArray()
    }).GetEnumerator()) { $state | Add-Member -NotePropertyName $entry.Key -NotePropertyValue $entry.Value -Force }
    if ([string]$state.phase -ceq 'restored') {
        $state | Add-Member -NotePropertyName restoredAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.ToString('o')) -Force
    }
    Write-KmcJsonAtomic $CombinedStatePath $state
    return [pscustomobject]@{
        schemaVersion = 1
        modsRestored = $modsRestored
        saveProtectionPassed = $saveProtectionPassed
        baselineImmutable = $baselineImmutable
        workingRestored = $workingRestored
        saveWriteAllowlistPassed = $saveWriteAllowlistPassed
        restoredModsDigest = $restoredModsDigest
        restoredSaveInventoryDigest = $restoredSaveDigest
        errors = $errors.ToArray()
    }
}

function New-KmcRuntimeResultV2 {
    param(
        [Parameter(Mandatory = $true)]$Request,
        $ValidatedGameResult,
        [Parameter(Mandatory = $true)][DateTimeOffset]$StartedAtUtc,
        [Parameter(Mandatory = $true)][bool]$ModsRestored,
        [Parameter(Mandatory = $true)][bool]$BaselineImmutable,
        [Parameter(Mandatory = $true)][bool]$WorkingRestored,
        [Parameter(Mandatory = $true)][bool]$SaveWriteAllowlistPassed,
        [Parameter(Mandatory = $true)][string]$RestoredSaveInventoryDigest,
        [AllowNull()][string]$GameResultSha256,
        [string[]]$Errors = @()
    )
    $subscenarios = New-Object 'System.Collections.Generic.List[object]'
    if ($null -ne $ValidatedGameResult) {
        if ([string]$ValidatedGameResult.evidenceManifestSha256 -cnotmatch '^[0-9a-f]{64}$') {
            throw 'Structurally validated game result lacks an exact evidence-manifest SHA-256.'
        }
        $evidenceManifestSha256 = [string]$ValidatedGameResult.evidenceManifestSha256
        foreach ($item in @($ValidatedGameResult.subscenarioResults)) {
            $subscenarios.Add([ordered]@{
                name = [string]$item.name
                status = [string]$item.status
                assertionPassCount = [int]$item.assertionPassCount
                assertionFailCount = [int]$item.assertionFailCount
                errors = @($item.errors | ForEach-Object { [string]$_ })
            })
        }
        $fixture = $ValidatedGameResult.fixture
    }
    else {
        $fallbackName = if (@(Get-KmcSaveBackedRuntimeScenarios | Where-Object { $_ -ceq [string]$Request.scenario }).Count -eq 1 -and
            [string]$Request.scenario -notin @('fixture-intake','lifecycle-suite','movement-suite','boundary-suite')) {
            [string]$Request.scenario
        } else { 'observe-mount-diagnostic-availability' }
        $fallbackErrors = if (@($Errors).Count -eq 0) { @('Runtime game result was unavailable or invalid.') } else { @($Errors) }
        $subscenarios.Add([ordered]@{
            name = $fallbackName
            status = 'FAIL'
            assertionPassCount = 0
            assertionFailCount = 1
            errors = @($fallbackErrors)
        })
        $fixture = $Request.fixture
        $evidenceManifestSha256 = Get-KmcValidatedOrchestrationArtifactManifestHash $Request
    }
    $subscenarioArray = $subscenarios.ToArray()
    $subscenarioPassCount = @($subscenarioArray | Where-Object { [string]$_.status -ceq 'PASS' }).Count
    $subscenarioFailCount = $subscenarios.Count - $subscenarioPassCount
    $assertionPassCount = 0
    $assertionFailCount = 0
    foreach ($item in $subscenarioArray) {
        $assertionPassCount += [int]$item.assertionPassCount
        $assertionFailCount += [int]$item.assertionFailCount
    }
    $saveProtectionPassed = $BaselineImmutable -and $WorkingRestored -and $SaveWriteAllowlistPassed
    $status = if ($null -ne $ValidatedGameResult -and [string]$ValidatedGameResult.status -ceq 'PASS' -and
        $ModsRestored -and $saveProtectionPassed -and @($Errors).Count -eq 0 -and
        $subscenarioFailCount -eq 0 -and $assertionFailCount -eq 0) { 'PASS' } else { 'FAIL' }
    return [ordered]@{
        schemaVersion = 2
        runId = [string]$Request.runId
        scenario = [string]$Request.scenario
        status = $status
        branch = [string]$Request.branch
        commit = [string]$Request.commit
        productVersion = [string]$Request.productVersion
        dllSha256 = [string]$Request.dllSha256
        dllMvid = [string]$Request.dllMvid
        transactionToken = [string]$Request.transactionToken
        startedAtUtc = $StartedAtUtc.ToString('o')
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        modsRestored = $ModsRestored
        saveProtectionPassed = $saveProtectionPassed
        gameResultSha256 = $GameResultSha256
        errors = @($Errors)
        fixture = $fixture
        baselineImmutable = $BaselineImmutable
        workingRestored = $WorkingRestored
        saveWriteAllowlistPassed = $SaveWriteAllowlistPassed
        restoredSaveInventoryDigest = $RestoredSaveInventoryDigest
        subscenarioTotal = $subscenarios.Count
        subscenarioPassCount = $subscenarioPassCount
        subscenarioFailCount = $subscenarioFailCount
        assertionPassCount = $assertionPassCount
        assertionFailCount = $assertionFailCount
        evidenceManifestSha256 = $evidenceManifestSha256
        subscenarioResults = $subscenarioArray
    }
}
