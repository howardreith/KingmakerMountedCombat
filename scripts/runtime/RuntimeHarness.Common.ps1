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
                if (-not $names.Add([string]$member.Name)) {
                    throw "$Description contains a duplicate or case-ambiguous JSON object member: $($member.Name)"
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
    (Get-Item -LiteralPath $workingPath).LastWriteTimeUtc = [DateTime]::new([long]$state.workingLastWriteTimeUtcTicks, [DateTimeKind]::Utc)
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
    $statePath = Get-KmcTransactionStatePath $StateRoot $runId
    if (Test-Path -LiteralPath $statePath) { throw "Run ID already has transaction state: $runId" }
    $backupRun = Assert-KmcChildPath (Join-Path ([IO.Path]::GetFullPath($BackupRoot)) $runId) $BackupRoot 'transaction backup'
    $stagingRun = Assert-KmcChildPath (Join-Path ([IO.Path]::GetFullPath($StagingRoot)) $runId) $StagingRoot 'transaction staging'
    if ((Test-Path -LiteralPath $backupRun) -or (Test-Path -LiteralPath $stagingRun)) { throw "Run ID already has backup or staging state: $runId" }
    New-Item -ItemType Directory -Path $backupRun | Out-Null; New-Item -ItemType Directory -Path $stagingRun | Out-Null
    $originalBackup = Join-Path $backupRun 'Mods-original'
    $ready = Join-Path $stagingRun 'Mods-ready'
    $stagedAfter = Join-Path $stagingRun 'Mods-staged-after'
    $frozenPackage = Join-Path $stagingRun 'package.zip'
    Copy-Item -LiteralPath $PackagePath -Destination $frozenPackage
    $packageHash = Get-KmcSha256 $PackagePath
    if ((Get-KmcSha256 $frozenPackage) -cne $packageHash) { throw 'Frozen runtime package hash differs from the qualified package.' }
    Expand-Archive -LiteralPath $frozenPackage -DestinationPath $ready
    $expectedRoot = Join-Path $ready 'KingmakerMountedCombat'
    if (-not (Test-Path -LiteralPath (Join-Path $expectedRoot 'Info.json')) -or -not (Test-Path -LiteralPath (Join-Path $expectedRoot 'KingmakerMountedCombat.dll'))) {
        throw 'Pre-staged package does not contain the exact KMC mod root.'
    }
    $sentinel = [ordered]@{ schemaVersion=1; runId=$runId; token=[string]$Lock.Token; packageSha256=$packageHash }
    Write-KmcJsonAtomic (Join-Path $ready '.kmc-runtime-sentinel.json') $sentinel
    $before = Get-KmcDirectoryManifest $fullLive; $staged = Get-KmcDirectoryManifest $ready
    $state = [ordered]@{
        schemaVersion=2; runId=$runId; token=[string]$Lock.Token; phase='prepared'; preparedAtUtc=[DateTime]::UtcNow.ToString('o')
        liveModsRoot=$fullLive; originalBackup=$originalBackup; stagedReady=$ready; stagedAfter=$stagedAfter
        frozenPackage=$frozenPackage; packageSha256=$packageHash
        beforeDigest=$before.digest; beforeFileCount=$before.fileCount; beforeDirectoryCount=$before.directoryCount; beforeTotalBytes=$before.totalBytes
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
        catch { throw new AggregateException('Mods transaction entry and guarded rollback both failed.', $entryException, $_.Exception) }
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
    $allowedState = @($requiredState + @('stagedAtUtc','stagedAfterDigest','stagedAfterFileCount','stagedAfterDirectoryCount','stagedAfterTotalBytes','stagedTreeChangedAtRuntime','restoredAtUtc','restoredDigest'))
    $actualState = @($state.PSObject.Properties.Name)
    if (@($requiredState | Where-Object { $_ -cnotin $actualState }).Count -ne 0 -or @($actualState | Where-Object { $_ -cnotin $allowedState }).Count -ne 0) {
        throw 'Transaction state property set is missing required fields or contains unknown fields.'
    }
    if ([int]$state.schemaVersion -ne 2 -or [string]$state.runId -cne [string]$Lock.RunId -or [string]$state.token -cne [string]$Lock.Token) { throw 'Transaction state ownership does not match the open lock.' }
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
        $allowed = ($relative -ceq 'movement-telemetry.jsonl' -and $kind -ceq 'telemetry') -or
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
