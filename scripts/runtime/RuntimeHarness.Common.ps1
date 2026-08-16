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

function Write-KmcBytesDurableAtomic {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][byte[]]$Bytes
    )
    $fullPath = [IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $fullPath
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    Assert-KmcNotReparsePoint $parent 'durable byte-write parent'
    $temporary = Join-Path $parent ('.' + [IO.Path]::GetFileName($fullPath) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        [IO.File]::WriteAllBytes($temporary, $Bytes)
        $temporaryStream = New-Object IO.FileStream($temporary, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
        try { $temporaryStream.Flush($true) }
        finally { $temporaryStream.Dispose() }
        if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
            $replacementBackup = Join-Path $parent ('.' + [IO.Path]::GetFileName($fullPath) + '.' + [Guid]::NewGuid().ToString('N') + '.bak')
            try { [IO.File]::Replace($temporary, $fullPath, $replacementBackup) }
            finally { if (Test-Path -LiteralPath $replacementBackup) { Remove-Item -LiteralPath $replacementBackup -Force } }
        }
        else { [IO.File]::Move($temporary, $fullPath) }
        $targetStream = New-Object IO.FileStream($fullPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
        try { $targetStream.Flush($true) }
        finally { $targetStream.Dispose() }
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

function Assert-KmcSaveMetadataInventoriesEqual {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ([int]$Before.schemaVersion -ne 2 -or [int]$After.schemaVersion -ne 2 -or
        -not [string]::Equals([string]$Before.root, [string]$After.root, [StringComparison]::OrdinalIgnoreCase) -or
        [int]$Before.fileCount -ne [int]$After.fileCount -or
        [long]$Before.totalBytes -ne [long]$After.totalBytes -or
        [string]$Before.digest -cne [string]$After.digest) {
        throw "$Description changed."
    }
}

function Assert-KmcSaveMetadataInventorySchema {
    param(
        [Parameter(Mandatory = $true)]$Inventory,
        [Parameter(Mandatory = $true)][string]$ExpectedSaveRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedDigest,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ($Inventory -isnot [pscustomobject]) { throw "$Description is not an exact JSON object." }
    Assert-KmcExactProperties $Inventory @('schemaVersion','root','fileCount','totalBytes','digest','entries') $Description
    if ((($Inventory.schemaVersion -isnot [int]) -and ($Inventory.schemaVersion -isnot [long])) -or
        [long]$Inventory.schemaVersion -ne 2 -or
        $Inventory.root -isnot [string] -or
        (($Inventory.fileCount -isnot [int]) -and ($Inventory.fileCount -isnot [long])) -or
        (($Inventory.totalBytes -isnot [int]) -and ($Inventory.totalBytes -isnot [long])) -or
        $Inventory.digest -isnot [string] -or
        $Inventory.entries -isnot [Array]) {
        throw "$Description schema types are not exact."
    }

    $fullSaveRoot = [IO.Path]::GetFullPath($ExpectedSaveRoot).TrimEnd('\')
    $recordedRoot = $null
    try { $recordedRoot = [IO.Path]::GetFullPath([string]$Inventory.root).TrimEnd('\') }
    catch { throw "$Description save root is not a valid absolute path." }
    if (-not [IO.Path]::IsPathRooted([string]$Inventory.root) -or
        -not [string]::Equals($recordedRoot, $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description save root does not match the caller-pinned save root."
    }

    $seen = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $fileCount = [long]0
    $totalBytes = [long]0
    foreach ($entry in @($Inventory.entries)) {
        if ($entry -isnot [pscustomobject]) { throw "$Description contains a non-object entry." }
        Assert-KmcExactProperties $entry @('kind','path','length','lastWriteTimeUtcTicks') "$Description entry"
        if ($entry.kind -isnot [string] -or $entry.path -isnot [string] -or
            (($entry.length -isnot [int]) -and ($entry.length -isnot [long])) -or
            (($entry.lastWriteTimeUtcTicks -isnot [int]) -and ($entry.lastWriteTimeUtcTicks -isnot [long]))) {
            throw "$Description contains an entry with non-exact schema types."
        }
        $kind = [string]$entry.kind
        $relativePath = [string]$entry.path
        if ($kind -cnotin @('file','directory','reparse') -or
            [string]::IsNullOrWhiteSpace($relativePath) -or
            [IO.Path]::IsPathRooted($relativePath) -or
            $relativePath.Contains('/') -or $relativePath.Contains('\') -or
            [IO.Path]::GetFileName($relativePath) -cne $relativePath) {
            throw "$Description contains an invalid direct-child entry: $relativePath"
        }
        $resolvedPath = Assert-KmcChildPath (Join-Path $fullSaveRoot $relativePath) $fullSaveRoot "$Description entry"
        if (-not [string]::Equals((Split-Path -Parent $resolvedPath), $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not $seen.Add($relativePath)) {
            throw "$Description contains an ambiguous or duplicate entry path: $relativePath"
        }
        if ([long]$entry.length -lt 0 -or [long]$entry.lastWriteTimeUtcTicks -le 0 -or
            ($kind -cne 'file' -and [long]$entry.length -ne 0)) {
            throw "$Description contains invalid metadata for entry: $relativePath"
        }
        if ($kind -ceq 'file') {
            $fileCount++
            $totalBytes += [long]$entry.length
        }
    }

    $canonical = (@($Inventory.entries) | ForEach-Object {
        '{0}|{1}|{2}|{3}' -f [string]$_.kind, [string]$_.path, [long]$_.length, [long]$_.lastWriteTimeUtcTicks
    }) -join "`n"
    $actualDigest = Get-KmcTextSha256 $canonical
    if ([long]$Inventory.fileCount -ne $fileCount -or
        [long]$Inventory.totalBytes -ne $totalBytes -or
        [string]$Inventory.digest -cne $actualDigest -or
        [string]$Inventory.digest -cne $ExpectedDigest) {
        throw "$Description counts, byte total, canonical digest, or caller digest pin do not match."
    }
    return $Inventory
}

function Read-KmcPriorSaveTransactionAuthority {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedRunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedStateSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedInventoryDigest,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedBaselineSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)]$CurrentPair
    )
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $transactionRoot = Assert-KmcChildPath (Join-Path $fullStateRoot 'save-transactions') $fullStateRoot 'prior save-transaction authority root'
    if (-not (Test-Path -LiteralPath $transactionRoot -PathType Container)) {
        throw "Prior save-transaction authority root is missing: $transactionRoot"
    }
    Assert-KmcNotReparsePoint $transactionRoot 'prior save-transaction authority root'
    $expectedPath = Assert-KmcChildPath (Join-Path $transactionRoot ($ExpectedRunId + '.json')) $transactionRoot 'prior save-transaction authority'
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not [string]::Equals($fullPath, $expectedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Prior save-transaction authority path does not match the exact caller-pinned run identity.'
    }
    Assert-KmcRecoveryLeafNoLinks $fullPath 'prior save-transaction authority'
    $metadataBefore = Get-Item -LiteralPath $fullPath -Force
    if ($metadataBefore.Length -le 0 -or $metadataBefore.Length -gt 4MB -or
        (Get-KmcSha256 $fullPath) -cne $ExpectedStateSha256) {
        throw 'Prior save-transaction authority size or SHA-256 differs from the explicit pin.'
    }
    $authorityBytes = [IO.File]::ReadAllBytes($fullPath)
    if ($authorityBytes.Length -ne $metadataBefore.Length) {
        throw 'Prior save-transaction authority length changed while its bytes were being captured.'
    }
    $strictUtf8 = New-Object Text.UTF8Encoding($false, $true)
    try { $json = $strictUtf8.GetString($authorityBytes) }
    catch [Text.DecoderFallbackException] { throw 'Prior save-transaction authority is not strict UTF-8.' }
    Assert-KmcJsonObjectMembersUnique -Json $json -Description 'prior save-transaction authority'
    $state = $json | ConvertFrom-Json
    $metadataAfter = Get-Item -LiteralPath $fullPath -Force
    if ($metadataAfter.Length -ne $metadataBefore.Length -or
        $metadataAfter.LastWriteTimeUtc.Ticks -ne $metadataBefore.LastWriteTimeUtc.Ticks -or
        (Get-KmcSha256 $fullPath) -cne $ExpectedStateSha256) {
        throw 'Prior save-transaction authority changed while it was being validated.'
    }

    $properties = @(
        'schemaVersion','runId','token','phase','preparedAtUtc','scenario','maxRuntimeArchiveWrites','saveRoot',
        'baselinePath','baselineSha256','baselineLength','baselineLastWriteTimeUtcTicks',
        'expectedGameName','expectedGameId','expectedArea','workingPath','workingSha256','workingLength',
        'workingLastWriteTimeUtcTicks','backupPath','backupSha256','backupLength','artifactQuarantineRoot',
        'beforeInventory','restoreStartedAtUtc','saveWriteAllowlistPassed','runtimeInventoryDigest','workingDisposition',
        'artifactPlan','artifactPlanDigest','recoveryPlannedAtUtc','baselineImmutable','artifactsQuarantinedAtUtc',
        'workingRestoredAtUtc','restoredInventoryDigest','restoredAtUtc'
    )
    if ($state -isnot [pscustomobject]) { throw 'Prior save-transaction authority is not an exact JSON object.' }
    Assert-KmcExactProperties $state $properties 'prior save-transaction authority'
    if ((($state.schemaVersion -isnot [int]) -and ($state.schemaVersion -isnot [long])) -or
        [long]$state.schemaVersion -ne 2 -or
        $state.runId -isnot [string] -or [string]$state.runId -cne $ExpectedRunId -or
        $state.phase -isnot [string] -or [string]$state.phase -cne 'restored' -or
        $state.saveRoot -isnot [string] -or
        $state.baselinePath -isnot [string] -or $state.workingPath -isnot [string] -or
        $state.baselineSha256 -isnot [string] -or $state.workingSha256 -isnot [string] -or
        $state.restoredInventoryDigest -isnot [string] -or
        $state.baselineImmutable -isnot [bool] -or -not [bool]$state.baselineImmutable -or
        $state.saveWriteAllowlistPassed -isnot [bool] -or -not [bool]$state.saveWriteAllowlistPassed) {
        throw 'Prior save-transaction authority schema, terminal phase, identity, or restoration flags are invalid.'
    }

    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $recordedSaveRoot = $null
    try { $recordedSaveRoot = [IO.Path]::GetFullPath([string]$state.saveRoot).TrimEnd('\') }
    catch { throw 'Prior save-transaction authority contains an invalid save root.' }
    if (-not [string]::Equals($recordedSaveRoot, $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$state.baselinePath), [string]$CurrentPair.baseline.path, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$state.workingPath), [string]$CurrentPair.working.path, [StringComparison]::OrdinalIgnoreCase) -or
        [string]$state.baselineSha256 -cne $ExpectedBaselineSha256 -or
        [string]$CurrentPair.baseline.sha256 -cne $ExpectedBaselineSha256 -or
        [string]$state.workingSha256 -cne $ExpectedSupersededWorkingSha256 -or
        [string]$state.backupSha256 -cne $ExpectedSupersededWorkingSha256 -or
        [long]$state.backupLength -ne [long]$state.workingLength -or
        [string]$state.restoredInventoryDigest -cne $ExpectedInventoryDigest) {
        throw 'Prior save-transaction authority root, paths, Baseline, Working, backup, or inventory pins do not match.'
    }

    $inventory = Assert-KmcSaveMetadataInventorySchema `
        -Inventory $state.beforeInventory `
        -ExpectedSaveRoot $fullSaveRoot `
        -ExpectedDigest $ExpectedInventoryDigest `
        -Description 'prior save-transaction metadata inventory'
    $map = @{}
    foreach ($entry in @($inventory.entries)) { $map[[string]$entry.path] = $entry }
    $baselineRelative = [IO.Path]::GetFileName([string]$CurrentPair.baseline.path)
    $workingRelative = [IO.Path]::GetFileName([string]$CurrentPair.working.path)
    $priorBaseline = $map[$baselineRelative]
    $priorWorking = $map[$workingRelative]
    if ($null -eq $priorBaseline -or $null -eq $priorWorking -or
        [string]$priorBaseline.kind -cne 'file' -or [string]$priorBaseline.path -cne $baselineRelative -or
        [long]$priorBaseline.length -ne [long]$state.baselineLength -or
        [long]$priorBaseline.lastWriteTimeUtcTicks -ne [long]$state.baselineLastWriteTimeUtcTicks -or
        [long]$priorBaseline.length -ne [long]$CurrentPair.baseline.length -or
        [long]$priorBaseline.lastWriteTimeUtcTicks -ne [long]$CurrentPair.baseline.lastWriteTimeUtcTicks -or
        [string]$priorWorking.kind -cne 'file' -or [string]$priorWorking.path -cne $workingRelative -or
        [long]$priorWorking.length -ne [long]$state.workingLength -or
        [long]$priorWorking.lastWriteTimeUtcTicks -ne [long]$state.workingLastWriteTimeUtcTicks) {
        throw 'Prior save-transaction inventory does not contain the exact pinned Baseline and superseded Working metadata.'
    }
    return [pscustomobject]@{
        schemaVersion = 1
        statePath = $fullPath
        stateSha256 = $ExpectedStateSha256
        runId = $ExpectedRunId
        inventory = $inventory
        inventoryDigest = $ExpectedInventoryDigest
        baselineSha256 = $ExpectedBaselineSha256
        supersededWorkingSha256 = $ExpectedSupersededWorkingSha256
        workingRelativePath = $workingRelative
        priorWorkingLength = [long]$state.workingLength
        priorWorkingLastWriteTimeUtcTicks = [long]$state.workingLastWriteTimeUtcTicks
    }
}

function Assert-KmcWorkingOnlyPriorInventoryTransition {
    param(
        [Parameter(Mandatory = $true)]$PriorAuthority,
        [Parameter(Mandatory = $true)]$CurrentInventory,
        [Parameter(Mandatory = $true)]$CurrentPair,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedRevisedWorkingSha256
    )
    [void](Assert-KmcSaveMetadataInventorySchema `
        -Inventory $CurrentInventory `
        -ExpectedSaveRoot $SaveRoot `
        -ExpectedDigest ([string]$CurrentInventory.digest) `
        -Description 'current save metadata inventory')
    if ([string]$CurrentPair.working.sha256 -cne $ExpectedRevisedWorkingSha256) {
        throw 'Current Working SHA-256 differs from the explicit revised pin during inventory continuity validation.'
    }
    $allowlist = Assert-KmcSaveWriteAllowlist `
        -Before $PriorAuthority.inventory `
        -After $CurrentInventory `
        -WorkingPath ([string]$CurrentPair.working.path)
    if (-not [bool]$allowlist.workingChanged -or @($allowlist.changedPaths).Count -ne 1 -or
        [string]@($allowlist.changedPaths)[0] -cne [string]$PriorAuthority.workingRelativePath) {
        throw 'Pinned prior-to-current save metadata does not contain exactly one Working-path transition.'
    }

    $priorMap = @{}
    foreach ($entry in @($PriorAuthority.inventory.entries)) { $priorMap[[string]$entry.path] = $entry }
    $currentMap = @{}
    foreach ($entry in @($CurrentInventory.entries)) { $currentMap[[string]$entry.path] = $entry }
    $allPaths = @($priorMap.Keys + $currentMap.Keys | Sort-Object -Unique)
    $changed = @($allPaths | Where-Object {
        $prior = $priorMap[$_]
        $current = $currentMap[$_]
        $null -eq $prior -or $null -eq $current -or
            [string]$prior.kind -cne [string]$current.kind -or
            [string]$prior.path -cne [string]$current.path -or
            [long]$prior.length -ne [long]$current.length -or
            [long]$prior.lastWriteTimeUtcTicks -ne [long]$current.lastWriteTimeUtcTicks
    })
    $workingRelative = [string]$PriorAuthority.workingRelativePath
    if ($changed.Count -ne 1 -or [string]$changed[0] -cne $workingRelative) {
        $description = if ($changed.Count -eq 0) { '<none>' } else { $changed -join ', ' }
        throw "Pinned prior-to-current save metadata contains unauthorized or ambiguous drift; expected only $workingRelative, changed: $description"
    }
    $currentWorking = $currentMap[$workingRelative]
    if ($null -eq $currentWorking -or [string]$currentWorking.kind -cne 'file' -or
        [string]$currentWorking.path -cne $workingRelative -or
        [long]$currentWorking.length -ne [long]$CurrentPair.working.length -or
        [long]$currentWorking.lastWriteTimeUtcTicks -ne [long]$CurrentPair.working.lastWriteTimeUtcTicks) {
        throw 'Current save inventory does not contain the exact revised Working metadata.'
    }
    return [pscustomobject]@{
        schemaVersion = 1
        priorInventoryDigest = [string]$PriorAuthority.inventoryDigest
        currentInventoryDigest = [string]$CurrentInventory.digest
        changedPath = $workingRelative
    }
}

function Assert-KmcRuntimeContinuityPinCombination {
    param(
        [Parameter(Mandatory = $true)][bool]$IsSaveBacked,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$BoundContinuityPinNames,
        [string]$ExpectedCurrentQualificationSha256,
        [string]$ExpectedSupersededWorkingSha256,
        [string]$PriorSaveTransactionStatePath,
        [string]$ExpectedPriorSaveTransactionRunId,
        [string]$ExpectedPriorSaveTransactionStateSha256,
        [string]$ExpectedPriorSaveMetadataDigest,
        [string]$ProtectedSaveContinuityAuthorityPath,
        [string]$ExpectedProtectedSaveContinuityEpochId,
        [string]$ExpectedProtectedSaveContinuityAuthoritySha256,
        [string]$ExpectedProtectedAutoSaveName,
        [string]$ExpectedProtectedAutoSaveSha256,
        [string]$ExpectedProtectedQuickSaveName,
        [string]$ExpectedProtectedQuickSaveSha256,
        [string]$ExpectedProtectedSavePinSetSha256
    )
    $commonValues = @(
        $ExpectedCurrentQualificationSha256,
        $ExpectedSupersededWorkingSha256,
        $PriorSaveTransactionStatePath,
        $ExpectedPriorSaveTransactionRunId,
        $ExpectedPriorSaveTransactionStateSha256,
        $ExpectedPriorSaveMetadataDigest,
        $ProtectedSaveContinuityAuthorityPath,
        $ExpectedProtectedSaveContinuityEpochId,
        $ExpectedProtectedSaveContinuityAuthoritySha256
    )
    $legacyValues = @(
        $ExpectedProtectedAutoSaveName,
        $ExpectedProtectedAutoSaveSha256,
        $ExpectedProtectedQuickSaveName,
        $ExpectedProtectedQuickSaveSha256
    )
    $commonPinNames = @(
        'ExpectedCurrentQualificationSha256',
        'ExpectedSupersededWorkingSha256',
        'PriorSaveTransactionStatePath',
        'ExpectedPriorSaveTransactionRunId',
        'ExpectedPriorSaveTransactionStateSha256',
        'ExpectedPriorSaveMetadataDigest',
        'ProtectedSaveContinuityAuthorityPath',
        'ExpectedProtectedSaveContinuityEpochId',
        'ExpectedProtectedSaveContinuityAuthoritySha256'
    )
    $legacyPinNames = @(
        'ExpectedProtectedAutoSaveName',
        'ExpectedProtectedAutoSaveSha256',
        'ExpectedProtectedQuickSaveName',
        'ExpectedProtectedQuickSaveSha256'
    )
    $chainedPinNames = @('ExpectedProtectedSavePinSetSha256')
    $pinNames = @($commonPinNames + $legacyPinNames + $chainedPinNames)
    $unknownOrDuplicateNames = @($BoundContinuityPinNames | Group-Object | Where-Object {
        $group = $_
        $group.Count -ne 1 -or @($pinNames | Where-Object { $_ -ceq [string]$group.Name }).Count -ne 1
    })
    if ($unknownOrDuplicateNames.Count -ne 0) {
        throw 'Runtime fixture-continuity bound-parameter identity is invalid or ambiguous.'
    }
    $commonPresent = @($commonValues | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count
    $legacyPresent = @($legacyValues | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count
    $chainedPresent = if ([string]::IsNullOrWhiteSpace($ExpectedProtectedSavePinSetSha256)) { 0 } else { 1 }
    $boundCommon = @($commonPinNames | Where-Object { $BoundContinuityPinNames -ccontains $_ }).Count
    $boundLegacy = @($legacyPinNames | Where-Object { $BoundContinuityPinNames -ccontains $_ }).Count
    $boundChained = @($chainedPinNames | Where-Object { $BoundContinuityPinNames -ccontains $_ }).Count
    if ($IsSaveBacked -and ($commonPresent -ne $commonValues.Count -or $boundCommon -ne $commonPinNames.Count)) {
        throw 'A save-backed runtime scenario requires every common fixture-continuity pin.'
    }
    if ($IsSaveBacked -and -not (
        ($legacyPresent -eq $legacyValues.Count -and $boundLegacy -eq $legacyPinNames.Count -and $chainedPresent -eq 0 -and $boundChained -eq 0) -or
        ($legacyPresent -eq 0 -and $boundLegacy -eq 0 -and $chainedPresent -eq 1 -and $boundChained -eq 1))) {
        throw 'A save-backed runtime scenario requires exactly one complete schema-v1 or schema-v2 protected-save pin mode.'
    }
    if (-not $IsSaveBacked -and ($commonPresent -ne 0 -or $legacyPresent -ne 0 -or $chainedPresent -ne 0 -or
        $BoundContinuityPinNames.Count -ne 0)) {
        throw 'A no-save runtime scenario rejects every fixture-continuity pin.'
    }
    return $true
}

function Assert-KmcManualReviewArtifactPinCombination {
    param(
        [Parameter(Mandatory = $true)][bool]$IsManualReview,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][string[]]$BoundArtifactPinNames,
        [string]$ExpectedPackageSha256,
        [string]$ExpectedPackageManifestSha256,
        [string]$ExpectedDllSha256,
        [string]$ExpectedBranch,
        [string]$ExpectedCommit
    )
    $pinNames = @(
        'ExpectedPackageSha256','ExpectedPackageManifestSha256','ExpectedDllSha256','ExpectedBranch','ExpectedCommit'
    )
    $values = @($ExpectedPackageSha256,$ExpectedPackageManifestSha256,$ExpectedDllSha256,$ExpectedBranch,$ExpectedCommit)
    $unknownOrDuplicateNames = @($BoundArtifactPinNames | Group-Object | Where-Object {
        $group = $_
        $group.Count -ne 1 -or @($pinNames | Where-Object { $_ -ceq [string]$group.Name }).Count -ne 1
    })
    if ($unknownOrDuplicateNames.Count -ne 0) { throw 'Manual-review artifact bound-parameter identity is invalid or ambiguous.' }
    $present = @($values | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count
    if ($IsManualReview -and ($present -ne $values.Count -or $BoundArtifactPinNames.Count -ne $pinNames.Count)) {
        throw 'Manual visual review requires every exact package, manifest, DLL, branch, and commit pin.'
    }
    if (-not $IsManualReview -and ($present -ne 0 -or $BoundArtifactPinNames.Count -ne 0)) {
        throw 'Non-manual runtime scenarios reject manual-review artifact pins.'
    }
    return $true
}

function Assert-KmcQualifiedWorkingPriorInventoryContinuity {
    param(
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedCurrentQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)][string]$PriorSaveTransactionStatePath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    Assert-KmcPathsDoNotOverlap -First $fullSaveRoot -Second $fullStateRoot -Description 'KMC save and runtime-state roots'
    $fullQualificationPath = [IO.Path]::GetFullPath($QualificationPath)
    $expectedQualificationPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'fixture-qualification.json') $fullStateRoot 'fixture qualification'
    if (-not [string]::Equals($fullQualificationPath, $expectedQualificationPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Fixture-continuity qualification path is not the exact runtime-state fixture-qualification.json.'
    }
    Assert-KmcNoGameProcesses
    Assert-KmcRecoveryLeafNoLinks $fullQualificationPath 'current KMC fixture qualification'
    $qualificationBefore = Get-Item -LiteralPath $fullQualificationPath -Force
    if ((Get-KmcSha256 $fullQualificationPath) -cne $ExpectedCurrentQualificationSha256) {
        throw 'Current KMC fixture qualification SHA-256 differs from the explicit runtime pin.'
    }

    $pair = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $fullQualificationPath
    if ([string]$pair.working.sha256 -ceq $ExpectedSupersededWorkingSha256) {
        throw 'Current and superseded Working SHA-256 pins do not establish a revised fixture.'
    }
    $authority = Read-KmcPriorSaveTransactionAuthority `
        -Path $PriorSaveTransactionStatePath `
        -StateRoot $fullStateRoot `
        -SaveRoot $fullSaveRoot `
        -ExpectedRunId $ExpectedPriorSaveTransactionRunId `
        -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest `
        -ExpectedBaselineSha256 ([string]$pair.baseline.sha256) `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -CurrentPair $pair
    $currentInventory = Get-KmcSaveMetadataInventory $fullSaveRoot
    $transition = Assert-KmcWorkingOnlyPriorInventoryTransition `
        -PriorAuthority $authority `
        -CurrentInventory $currentInventory `
        -CurrentPair $pair `
        -SaveRoot $fullSaveRoot `
        -ExpectedRevisedWorkingSha256 ([string]$pair.working.sha256)

    Assert-KmcNoGameProcesses
    $pairAfter = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $fullQualificationPath
    $currentInventoryAfter = Get-KmcSaveMetadataInventory $fullSaveRoot
    Assert-KmcSaveMetadataInventoriesEqual `
        -Before $currentInventory `
        -After $currentInventoryAfter `
        -Description 'fixture-continuity live save metadata'
    if ((New-KmcRuntimeFixturePayload $pairAfter | ConvertTo-Json -Depth 10 -Compress) -cne
        (New-KmcRuntimeFixturePayload $pair | ConvertTo-Json -Depth 10 -Compress)) {
        throw 'KMC fixture identity changed while prior-inventory continuity was being validated.'
    }
    $qualificationAfter = Get-Item -LiteralPath $fullQualificationPath -Force
    if ((Get-KmcSha256 $fullQualificationPath) -cne $ExpectedCurrentQualificationSha256 -or
        $qualificationAfter.Length -ne $qualificationBefore.Length -or
        $qualificationAfter.LastWriteTimeUtc.Ticks -ne $qualificationBefore.LastWriteTimeUtc.Ticks) {
        throw 'Current KMC fixture qualification changed while prior-inventory continuity was being validated.'
    }
    return [pscustomobject]@{
        schemaVersion = 1
        pair = $pair
        saveMetadata = $currentInventory
        priorInventoryDigest = [string]$transition.priorInventoryDigest
        currentInventoryDigest = [string]$transition.currentInventoryDigest
        changedPath = [string]$transition.changedPath
    }
}

function New-KmcProtectedSaveContinuityAuthorityRecord {
    param(
        [Parameter(Mandatory = $true)]$CurrentPair,
        [Parameter(Mandatory = $true)]$PriorAuthority,
        [Parameter(Mandatory = $true)]$CurrentInventory,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$CurrentQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$EpochId,
        [Parameter(Mandatory = $true)][string]$AuthorizedAtUtc,
        [Parameter(Mandatory = $true)][string]$AutoSaveName,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$AutoSaveSha256,
        [Parameter(Mandatory = $true)][long]$AutoSaveLength,
        [Parameter(Mandatory = $true)][long]$AutoSaveLastWriteTimeUtcTicks,
        [Parameter(Mandatory = $true)][string]$QuickSaveName,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$QuickSaveSha256,
        [Parameter(Mandatory = $true)][long]$QuickSaveLength,
        [Parameter(Mandatory = $true)][long]$QuickSaveLastWriteTimeUtcTicks
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $fullQualificationPath = [IO.Path]::GetFullPath($QualificationPath)
    $authorizedAt = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParseExact(
        $AuthorizedAtUtc,
        'o',
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind,
        [ref]$authorizedAt)) {
        throw 'Protected-save continuity authority timestamp is not an exact round-trip timestamp.'
    }
    if ($AutoSaveName -cnotmatch '^Auto_[0-9]+\.zks$' -or
        $QuickSaveName -cnotmatch '^Quick_[0-9]+\.zks$' -or
        [string]::Equals($AutoSaveName, $QuickSaveName, [StringComparison]::OrdinalIgnoreCase) -or
        $AutoSaveLength -le 0 -or $QuickSaveLength -le 0 -or
        $AutoSaveLastWriteTimeUtcTicks -le 0 -or $QuickSaveLastWriteTimeUtcTicks -le 0) {
        throw 'Protected-save continuity authority requires one exact distinct autosave and quicksave fingerprint.'
    }
    [void](Assert-KmcSaveMetadataInventorySchema `
        -Inventory $CurrentInventory `
        -ExpectedSaveRoot $fullSaveRoot `
        -ExpectedDigest ([string]$CurrentInventory.digest) `
        -Description 'protected-save continuity current inventory')
    if ([int]$CurrentPair.schemaVersion -ne 1 -or
        @($CurrentPair.writableSaveNames).Count -ne 1 -or
        [string]@($CurrentPair.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING' -or
        [string]$CurrentPair.baseline.name -cne 'KMC_AUTOMATION_BASELINE' -or
        [string]$CurrentPair.working.name -cne 'KMC_AUTOMATION_WORKING') {
        throw 'Protected-save continuity authority requires the exact qualified pair and Working-only allowlist.'
    }
    Assert-KmcRecoveryLeafNoLinks $fullQualificationPath 'protected-save continuity fixture qualification'
    if ((Get-KmcSha256 $fullQualificationPath) -cne $CurrentQualificationSha256) {
        throw 'Protected-save continuity fixture qualification differs from its explicit SHA-256 pin.'
    }

    $priorMap = @{}
    foreach ($entry in @($PriorAuthority.inventory.entries)) { $priorMap[[string]$entry.path] = $entry }
    $currentMap = @{}
    foreach ($entry in @($CurrentInventory.entries)) { $currentMap[[string]$entry.path] = $entry }
    $allPaths = @($priorMap.Keys + $currentMap.Keys | Sort-Object -Unique)
    $changedPaths = @($allPaths | Where-Object {
        $prior = $priorMap[$_]
        $current = $currentMap[$_]
        $null -eq $prior -or $null -eq $current -or
            [string]$prior.kind -cne [string]$current.kind -or
            [string]$prior.path -cne [string]$current.path -or
            [long]$prior.length -ne [long]$current.length -or
            [long]$prior.lastWriteTimeUtcTicks -ne [long]$current.lastWriteTimeUtcTicks
    })
    $workingName = [string]$PriorAuthority.workingRelativePath
    $expectedChanges = @(@($AutoSaveName, $workingName, $QuickSaveName) | Sort-Object)
    if (($changedPaths -join "`n") -cne ($expectedChanges -join "`n")) {
        $description = if ($changedPaths.Count -eq 0) { '<none>' } else { $changedPaths -join ', ' }
        throw "Protected-save epoch transition must contain exactly revised Working plus the attested Auto/Quick files; changed: $description"
    }

    $currentWorking = $currentMap[$workingName]
    if ($null -eq $currentWorking -or [string]$currentWorking.kind -cne 'file' -or
        [string]$currentWorking.path -cne [IO.Path]::GetFileName([string]$CurrentPair.working.path) -or
        [long]$currentWorking.length -ne [long]$CurrentPair.working.length -or
        [long]$currentWorking.lastWriteTimeUtcTicks -ne [long]$CurrentPair.working.lastWriteTimeUtcTicks) {
        throw 'Protected-save epoch transition does not contain the exact revised Working metadata.'
    }

    $transitionRecords = New-Object 'Collections.Generic.List[object]'
    foreach ($specification in @(
        [pscustomobject]@{ name=$AutoSaveName; sha256=$AutoSaveSha256; length=$AutoSaveLength; ticks=$AutoSaveLastWriteTimeUtcTicks },
        [pscustomobject]@{ name=$QuickSaveName; sha256=$QuickSaveSha256; length=$QuickSaveLength; ticks=$QuickSaveLastWriteTimeUtcTicks }
    )) {
        $prior = $priorMap[[string]$specification.name]
        $current = $currentMap[[string]$specification.name]
        if ($null -eq $prior -or $null -eq $current -or
            [string]$prior.kind -cne 'file' -or [string]$current.kind -cne 'file' -or
            [string]$prior.path -cne [string]$specification.name -or
            [string]$current.path -cne [string]$specification.name -or
            [long]$current.length -ne [long]$specification.length -or
            [long]$current.lastWriteTimeUtcTicks -ne [long]$specification.ticks) {
            throw "Protected-save epoch metadata differs for $($specification.name)."
        }
        $protectedPath = Assert-KmcChildPath (Join-Path $fullSaveRoot ([string]$specification.name)) $fullSaveRoot 'attested protected save'
        if (-not [string]::Equals((Split-Path -Parent $protectedPath), $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Attested protected save is not an exact direct child of the save root.'
        }
        Assert-KmcRecoveryLeafNoLinks $protectedPath 'attested protected save'
        $file = Get-Item -LiteralPath $protectedPath -Force
        if ($file.Length -ne [long]$specification.length -or
            $file.LastWriteTimeUtc.Ticks -ne [long]$specification.ticks -or
            (Get-KmcSha256 $protectedPath) -cne [string]$specification.sha256) {
            throw "Attested protected save bytes or filesystem metadata differ for $($specification.name)."
        }
        $transitionRecords.Add([ordered]@{
            fileName = [string]$specification.name
            priorKind = [string]$prior.kind
            priorLength = [long]$prior.length
            priorLastWriteTimeUtcTicks = [long]$prior.lastWriteTimeUtcTicks
            currentKind = [string]$current.kind
            currentLength = [long]$current.length
            currentLastWriteTimeUtcTicks = [long]$current.lastWriteTimeUtcTicks
            currentSha256 = [string]$specification.sha256
        })
    }

    return [ordered]@{
        schemaVersion = 1
        authorityKind = 'user-attested-protected-save-continuity'
        epochId = $EpochId
        authorizedAtUtc = $AuthorizedAtUtc
        attestationScope = 'external-user-fixture-preparation-auto-quicksave-baseline-only'
        saveRoot = $fullSaveRoot
        priorAuthority = [ordered]@{
            statePath = [string]$PriorAuthority.statePath
            runId = [string]$PriorAuthority.runId
            stateSha256 = [string]$PriorAuthority.stateSha256
            inventoryDigest = [string]$PriorAuthority.inventoryDigest
            baselineSha256 = [string]$PriorAuthority.baselineSha256
            supersededWorkingSha256 = [string]$PriorAuthority.supersededWorkingSha256
        }
        currentQualification = [ordered]@{ path=$fullQualificationPath; sha256=$CurrentQualificationSha256 }
        baseline = [ordered]@{
            path=[string]$CurrentPair.baseline.path;fileName=[string]$CurrentPair.baseline.fileName
            sha256=[string]$CurrentPair.baseline.sha256;length=[long]$CurrentPair.baseline.length
            lastWriteTimeUtcTicks=[long]$CurrentPair.baseline.lastWriteTimeUtcTicks
        }
        working = [ordered]@{
            path=[string]$CurrentPair.working.path;fileName=[string]$CurrentPair.working.fileName
            sha256=[string]$CurrentPair.working.sha256;length=[long]$CurrentPair.working.length
            lastWriteTimeUtcTicks=[long]$CurrentPair.working.lastWriteTimeUtcTicks
        }
        writableSaveNames = @('KMC_AUTOMATION_WORKING')
        authorizedProtectedTransitions = $transitionRecords.ToArray()
        currentInventory = $CurrentInventory
    }
}

function Read-KmcProtectedSaveContinuityAuthority {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedEpochId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedAuthoritySha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedCurrentQualificationSha256,
        [Parameter(Mandatory = $true)][string]$ExpectedPriorSaveTransactionStatePath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)][string]$ExpectedAutoSaveName,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedAutoSaveSha256,
        [Parameter(Mandatory = $true)][string]$ExpectedQuickSaveName,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedQuickSaveSha256
    )
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $authorityRoot = Assert-KmcChildPath (Join-Path $fullStateRoot 'protected-save-authorities') $fullStateRoot 'protected-save authority root'
    if (-not (Test-Path -LiteralPath $authorityRoot -PathType Container)) { throw 'Protected-save authority root is missing.' }
    Assert-KmcNotReparsePoint $authorityRoot 'protected-save authority root'
    $expectedPath = Assert-KmcChildPath (Join-Path $authorityRoot ($ExpectedEpochId + '.json')) $authorityRoot 'protected-save authority'
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not [string]::Equals($fullPath, $expectedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Protected-save authority path differs from the caller-pinned epoch identity.'
    }
    Assert-KmcRecoveryLeafNoLinks $fullPath 'protected-save authority'
    $before = Get-Item -LiteralPath $fullPath -Force
    if ($before.Length -le 0 -or $before.Length -gt 2MB -or (Get-KmcSha256 $fullPath) -cne $ExpectedAuthoritySha256) {
        throw 'Protected-save authority size or SHA-256 differs from the explicit pin.'
    }
    $bytes = [IO.File]::ReadAllBytes($fullPath)
    $strictUtf8 = New-Object Text.UTF8Encoding($false, $true)
    try { $json = $strictUtf8.GetString($bytes) }
    catch [Text.DecoderFallbackException] { throw 'Protected-save authority is not strict UTF-8.' }
    Assert-KmcJsonObjectMembersUnique -Json $json -Description 'protected-save authority'
    $record = $json | ConvertFrom-Json
    $after = Get-Item -LiteralPath $fullPath -Force
    if ($after.Length -ne $before.Length -or $after.LastWriteTimeUtc.Ticks -ne $before.LastWriteTimeUtc.Ticks -or
        (Get-KmcSha256 $fullPath) -cne $ExpectedAuthoritySha256) {
        throw 'Protected-save authority changed while it was being read.'
    }
    if ($record -isnot [pscustomobject]) { throw 'Protected-save authority is not an exact JSON object.' }
    Assert-KmcExactProperties $record @(
        'schemaVersion','authorityKind','epochId','authorizedAtUtc','attestationScope','saveRoot','priorAuthority',
        'currentQualification','baseline','working','writableSaveNames','authorizedProtectedTransitions','currentInventory'
    ) 'protected-save authority'
    if ((($record.schemaVersion -isnot [int]) -and ($record.schemaVersion -isnot [long])) -or
        [long]$record.schemaVersion -ne 1 -or $record.authorityKind -isnot [string] -or
        [string]$record.authorityKind -cne 'user-attested-protected-save-continuity' -or
        $record.epochId -isnot [string] -or [string]$record.epochId -cne $ExpectedEpochId -or
        $record.authorizedAtUtc -isnot [string] -or $record.attestationScope -isnot [string] -or
        [string]$record.attestationScope -cne 'external-user-fixture-preparation-auto-quicksave-baseline-only' -or
        $record.saveRoot -isnot [string] -or $record.priorAuthority -isnot [pscustomobject] -or
        $record.currentQualification -isnot [pscustomobject] -or $record.baseline -isnot [pscustomobject] -or
        $record.working -isnot [pscustomobject] -or $record.writableSaveNames -isnot [Array] -or
        $record.authorizedProtectedTransitions -isnot [Array]) {
        throw 'Protected-save authority top-level schema or types are invalid.'
    }
    Assert-KmcExactProperties $record.priorAuthority @(
        'statePath','runId','stateSha256','inventoryDigest','baselineSha256','supersededWorkingSha256'
    ) 'protected-save authority prior source'
    Assert-KmcExactProperties $record.currentQualification @('path','sha256') 'protected-save authority qualification'
    foreach ($name in @('baseline','working')) {
        Assert-KmcExactProperties $record.$name @('path','fileName','sha256','length','lastWriteTimeUtcTicks') "protected-save authority $name"
    }
    if (@($record.writableSaveNames).Count -ne 1 -or [string]@($record.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING' -or
        @($record.authorizedProtectedTransitions).Count -ne 2) {
        throw 'Protected-save authority allowlist or authorized transition count is invalid.'
    }
    foreach ($transition in @($record.authorizedProtectedTransitions)) {
        if ($transition -isnot [pscustomobject]) { throw 'Protected-save authority transition is not an exact object.' }
        Assert-KmcExactProperties $transition @(
            'fileName','priorKind','priorLength','priorLastWriteTimeUtcTicks','currentKind','currentLength',
            'currentLastWriteTimeUtcTicks','currentSha256'
        ) 'protected-save authority transition'
        foreach ($field in @('fileName','priorKind','currentKind','currentSha256')) {
            if ($transition.$field -isnot [string]) { throw "Protected-save authority transition $field type is invalid." }
        }
        foreach ($field in @('priorLength','priorLastWriteTimeUtcTicks','currentLength','currentLastWriteTimeUtcTicks')) {
            if (($transition.$field -isnot [int]) -and ($transition.$field -isnot [long])) {
                throw "Protected-save authority transition $field type is invalid."
            }
        }
    }
    foreach ($container in @($record.priorAuthority,$record.currentQualification,$record.baseline,$record.working)) {
        foreach ($property in @($container.PSObject.Properties)) {
            if ($property.Name -in @('length','lastWriteTimeUtcTicks')) {
                if (($property.Value -isnot [int]) -and ($property.Value -isnot [long])) { throw 'Protected-save authority identity integral type is invalid.' }
            }
            elseif ($property.Value -isnot [string]) { throw 'Protected-save authority identity string type is invalid.' }
        }
    }

    $pair = Assert-KmcFixturePair -SaveRoot $SaveRoot -QualificationPath $QualificationPath
    if ((Get-KmcSha256 $QualificationPath) -cne $ExpectedCurrentQualificationSha256 -or
        [string]$record.currentQualification.sha256 -cne $ExpectedCurrentQualificationSha256 -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$record.currentQualification.path), [IO.Path]::GetFullPath($QualificationPath), [StringComparison]::OrdinalIgnoreCase) -or
        [string]$record.priorAuthority.statePath -cne [IO.Path]::GetFullPath($ExpectedPriorSaveTransactionStatePath) -or
        [string]$record.priorAuthority.runId -cne $ExpectedPriorSaveTransactionRunId -or
        [string]$record.priorAuthority.stateSha256 -cne $ExpectedPriorSaveTransactionStateSha256 -or
        [string]$record.priorAuthority.inventoryDigest -cne $ExpectedPriorSaveMetadataDigest -or
        [string]$record.priorAuthority.supersededWorkingSha256 -cne $ExpectedSupersededWorkingSha256) {
        throw 'Protected-save authority qualification or prior-source caller pins do not reconcile.'
    }
    $priorAuthority = Read-KmcPriorSaveTransactionAuthority `
        -Path $ExpectedPriorSaveTransactionStatePath `
        -StateRoot $StateRoot `
        -SaveRoot $SaveRoot `
        -ExpectedRunId $ExpectedPriorSaveTransactionRunId `
        -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest `
        -ExpectedBaselineSha256 ([string]$pair.baseline.sha256) `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -CurrentPair $pair
    $autoTransition = @($record.authorizedProtectedTransitions | Where-Object { [string]$_.fileName -ceq $ExpectedAutoSaveName })
    $quickTransition = @($record.authorizedProtectedTransitions | Where-Object { [string]$_.fileName -ceq $ExpectedQuickSaveName })
    if ($autoTransition.Count -ne 1 -or $quickTransition.Count -ne 1 -or
        [string]$autoTransition[0].currentSha256 -cne $ExpectedAutoSaveSha256 -or
        [string]$quickTransition[0].currentSha256 -cne $ExpectedQuickSaveSha256) {
        throw 'Protected-save authority does not contain the exact caller-pinned Auto/Quick fingerprints.'
    }
    $liveInventory = Get-KmcSaveMetadataInventory $SaveRoot
    $expectedRecord = New-KmcProtectedSaveContinuityAuthorityRecord `
        -CurrentPair $pair -PriorAuthority $priorAuthority -CurrentInventory $liveInventory -SaveRoot $SaveRoot `
        -QualificationPath $QualificationPath -CurrentQualificationSha256 $ExpectedCurrentQualificationSha256 `
        -EpochId $ExpectedEpochId -AuthorizedAtUtc ([string]$record.authorizedAtUtc) `
        -AutoSaveName $ExpectedAutoSaveName -AutoSaveSha256 $ExpectedAutoSaveSha256 `
        -AutoSaveLength ([long]$autoTransition[0].currentLength) `
        -AutoSaveLastWriteTimeUtcTicks ([long]$autoTransition[0].currentLastWriteTimeUtcTicks) `
        -QuickSaveName $ExpectedQuickSaveName -QuickSaveSha256 $ExpectedQuickSaveSha256 `
        -QuickSaveLength ([long]$quickTransition[0].currentLength) `
        -QuickSaveLastWriteTimeUtcTicks ([long]$quickTransition[0].currentLastWriteTimeUtcTicks)
    if (($record | ConvertTo-Json -Depth 30 -Compress) -cne ($expectedRecord | ConvertTo-Json -Depth 30 -Compress)) {
        throw 'Protected-save authority content does not exactly reconcile to prior, current, and attested state.'
    }
    return [pscustomobject]@{
        schemaVersion=1;path=$fullPath;sha256=$ExpectedAuthoritySha256;epochId=$ExpectedEpochId
        pair=$pair;saveMetadata=$liveInventory;record=$record
    }
}

function Assert-KmcQualifiedWorkingProtectedSaveContinuityV1 {
    param(
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedCurrentQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)][string]$PriorSaveTransactionStatePath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest,
        [Parameter(Mandatory = $true)][string]$ProtectedSaveContinuityAuthorityPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedProtectedSaveContinuityEpochId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSaveContinuityAuthoritySha256,
        [Parameter(Mandatory = $true)][string]$ExpectedProtectedAutoSaveName,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedAutoSaveSha256,
        [Parameter(Mandatory = $true)][string]$ExpectedProtectedQuickSaveName,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedQuickSaveSha256
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    Assert-KmcPathsDoNotOverlap -First $fullSaveRoot -Second $fullStateRoot -Description 'KMC save and runtime-state roots'
    Assert-KmcNoGameProcesses
    $authorityArguments = @{
        Path=$ProtectedSaveContinuityAuthorityPath;StateRoot=$StateRoot;SaveRoot=$SaveRoot;QualificationPath=$QualificationPath
        ExpectedEpochId=$ExpectedProtectedSaveContinuityEpochId
        ExpectedAuthoritySha256=$ExpectedProtectedSaveContinuityAuthoritySha256
        ExpectedCurrentQualificationSha256=$ExpectedCurrentQualificationSha256
        ExpectedPriorSaveTransactionStatePath=$PriorSaveTransactionStatePath
        ExpectedPriorSaveTransactionRunId=$ExpectedPriorSaveTransactionRunId
        ExpectedPriorSaveTransactionStateSha256=$ExpectedPriorSaveTransactionStateSha256
        ExpectedPriorSaveMetadataDigest=$ExpectedPriorSaveMetadataDigest
        ExpectedSupersededWorkingSha256=$ExpectedSupersededWorkingSha256
        ExpectedAutoSaveName=$ExpectedProtectedAutoSaveName;ExpectedAutoSaveSha256=$ExpectedProtectedAutoSaveSha256
        ExpectedQuickSaveName=$ExpectedProtectedQuickSaveName;ExpectedQuickSaveSha256=$ExpectedProtectedQuickSaveSha256
    }
    $first = Read-KmcProtectedSaveContinuityAuthority @authorityArguments
    Assert-KmcNoGameProcesses
    $second = Read-KmcProtectedSaveContinuityAuthority @authorityArguments
    Assert-KmcSaveMetadataInventoriesEqual -Before $first.saveMetadata -After $second.saveMetadata -Description 'protected-save continuity live metadata'
    if ((New-KmcRuntimeFixturePayload $first.pair | ConvertTo-Json -Depth 10 -Compress) -cne
        (New-KmcRuntimeFixturePayload $second.pair | ConvertTo-Json -Depth 10 -Compress)) {
        throw 'KMC fixture identity changed during protected-save continuity validation.'
    }
    return $second
}

function Assert-KmcPathsDoNotOverlap {
    param(
        [Parameter(Mandatory = $true)][string]$First,
        [Parameter(Mandatory = $true)][string]$Second,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $firstFull = [IO.Path]::GetFullPath($First).TrimEnd('\')
    $secondFull = [IO.Path]::GetFullPath($Second).TrimEnd('\')
    if ([string]::Equals($firstFull, $secondFull, [StringComparison]::OrdinalIgnoreCase) -or
        $firstFull.StartsWith($secondFull + '\', [StringComparison]::OrdinalIgnoreCase) -or
        $secondFull.StartsWith($firstFull + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description overlap: $firstFull / $secondFull"
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

function New-KmcWorkingFixtureRequalification {
    param(
        [Parameter(Mandatory = $true)]$Pair,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedExistingQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedBaselineSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedRevisedWorkingSha256
    )
    $fullQualification = [IO.Path]::GetFullPath($QualificationPath)
    if (-not (Test-Path -LiteralPath $fullQualification -PathType Leaf)) {
        throw "Existing KMC fixture qualification is missing: $fullQualification"
    }
    Assert-KmcNotReparsePoint $fullQualification 'existing KMC fixture qualification'
    Assert-KmcNotHardLink $fullQualification 'existing KMC fixture qualification'
    $qualificationBefore = Get-Item -LiteralPath $fullQualification -Force
    if ($qualificationBefore.Length -le 0 -or $qualificationBefore.Length -gt 64KB) {
        throw 'Existing KMC fixture qualification size is outside the guarded range.'
    }
    $actualQualificationSha256 = Get-KmcSha256 $fullQualification
    if ($actualQualificationSha256 -cne $ExpectedExistingQualificationSha256) {
        throw 'Existing KMC fixture qualification SHA-256 differs from the explicit requalification pin.'
    }
    $recorded = Read-KmcJson $fullQualification
    $qualificationAfter = Get-Item -LiteralPath $fullQualification -Force
    if ($qualificationAfter.Length -ne $qualificationBefore.Length -or
        $qualificationAfter.LastWriteTimeUtc.Ticks -ne $qualificationBefore.LastWriteTimeUtc.Ticks -or
        (Get-KmcSha256 $fullQualification) -cne $actualQualificationSha256) {
        throw 'Existing KMC fixture qualification changed while it was being validated.'
    }

    $properties = @(
        'schemaVersion','baselineName','baselineFileName','baselinePath','baselineSha256','baselineLength',
        'baselineLastWriteTimeUtcTicks','workingName','workingFileName','workingPath','initialWorkingSha256',
        'initialWorkingLength','initialWorkingLastWriteTimeUtcTicks','expectedGameName','expectedGameId',
        'expectedArea','qualifiedAtUtc','writableSaveNames'
    )
    Assert-KmcExactProperties $recorded $properties 'existing KMC fixture qualification'
    if (($recorded -isnot [pscustomobject]) -or
        (($recorded.schemaVersion -isnot [int]) -and ($recorded.schemaVersion -isnot [long])) -or
        [long]$recorded.schemaVersion -ne 1 -or
        $recorded.writableSaveNames -isnot [Array]) {
        throw 'Existing KMC fixture qualification schema types are not exact.'
    }
    foreach ($field in @(
        'baselineName','baselineFileName','baselinePath','baselineSha256','workingName','workingFileName','workingPath',
        'initialWorkingSha256','expectedGameName','expectedGameId','expectedArea','qualifiedAtUtc'
    )) {
        if ($recorded.$field -isnot [string]) {
            throw "Existing KMC fixture qualification field $field is not an exact JSON string."
        }
    }
    foreach ($field in @('baselineLength','baselineLastWriteTimeUtcTicks','initialWorkingLength','initialWorkingLastWriteTimeUtcTicks')) {
        if (($recorded.$field -isnot [int]) -and ($recorded.$field -isnot [long])) {
            throw "Existing KMC fixture qualification field $field is not an exact integral JSON value."
        }
    }
    $priorQualifiedAt = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParseExact(
        [string]$recorded.qualifiedAtUtc,
        'o',
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind,
        [ref]$priorQualifiedAt)) {
        throw 'Existing KMC fixture qualification timestamp is not an exact round-trip timestamp.'
    }
    if ([string]$recorded.baselineSha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$recorded.initialWorkingSha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [long]$recorded.baselineLength -le 0 -or [long]$recorded.initialWorkingLength -le 0 -or
        [long]$recorded.baselineLastWriteTimeUtcTicks -le 0 -or [long]$recorded.initialWorkingLastWriteTimeUtcTicks -le 0 -or
        @($recorded.writableSaveNames).Count -ne 1 -or
        [string]@($recorded.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING') {
        throw 'Existing KMC fixture qualification pins or Working-only allowlist are invalid.'
    }

    if ([int]$Pair.schemaVersion -ne 1 -or
        @($Pair.writableSaveNames).Count -ne 1 -or
        [string]@($Pair.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING' -or
        [string]$Pair.baseline.name -cne 'KMC_AUTOMATION_BASELINE' -or
        [string]$Pair.working.name -cne 'KMC_AUTOMATION_WORKING' -or
        [string]::Equals([string]$Pair.baseline.path, [string]$Pair.working.path, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Revised KMC fixture pair does not retain exact distinct identities and the Working-only allowlist.'
    }
    if ([string]$Pair.baseline.gameId -cne [string]$Pair.working.gameId -or
        [string]$Pair.baseline.gameName -cne [string]$Pair.working.gameName -or
        [string]$Pair.baseline.area -cne [string]$Pair.working.area) {
        throw 'Revised KMC fixture pair does not retain exact shared campaign identity.'
    }

    if ([string]$recorded.baselineSha256 -cne $ExpectedBaselineSha256 -or
        [string]$Pair.baseline.sha256 -cne $ExpectedBaselineSha256 -or
        [string]$recorded.baselineName -cne [string]$Pair.baseline.name -or
        [string]$recorded.baselineFileName -cne [string]$Pair.baseline.fileName -or
        -not [string]::Equals([string]$recorded.baselinePath, [string]$Pair.baseline.path, [StringComparison]::OrdinalIgnoreCase) -or
        [long]$recorded.baselineLength -ne [long]$Pair.baseline.length -or
        [long]$recorded.baselineLastWriteTimeUtcTicks -ne [long]$Pair.baseline.lastWriteTimeUtcTicks) {
        throw 'KMC Baseline differs from the explicit immutable pin or existing durable qualification.'
    }
    if ([string]$recorded.workingName -cne [string]$Pair.working.name -or
        [string]$recorded.workingFileName -cne [string]$Pair.working.fileName -or
        -not [string]::Equals([string]$recorded.workingPath, [string]$Pair.working.path, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Revised KMC Working stable name, filename, or path differs from the existing durable qualification.'
    }
    if ([string]$recorded.expectedGameName -cne [string]$Pair.expectedGameName -or
        [string]$recorded.expectedGameId -cne [string]$Pair.expectedGameId -or
        [string]$recorded.expectedArea -cne [string]$Pair.expectedArea) {
        throw 'Revised KMC Working campaign identity differs from the existing durable qualification.'
    }
    if ([string]$recorded.initialWorkingSha256 -cne $ExpectedSupersededWorkingSha256) {
        throw 'Superseded KMC Working SHA-256 differs from the explicit prior pin.'
    }
    if ([string]$Pair.working.sha256 -cne $ExpectedRevisedWorkingSha256) {
        throw 'Revised KMC Working SHA-256 differs from the explicit replacement pin.'
    }
    if ($ExpectedSupersededWorkingSha256 -ceq $ExpectedRevisedWorkingSha256) {
        throw 'KMC Working requalification requires a distinct revised SHA-256.'
    }

    $replacement = [ordered]@{
        schemaVersion = $recorded.schemaVersion
        baselineName = $recorded.baselineName
        baselineFileName = $recorded.baselineFileName
        baselinePath = $recorded.baselinePath
        baselineSha256 = $recorded.baselineSha256
        baselineLength = $recorded.baselineLength
        baselineLastWriteTimeUtcTicks = $recorded.baselineLastWriteTimeUtcTicks
        workingName = $recorded.workingName
        workingFileName = $recorded.workingFileName
        workingPath = $recorded.workingPath
        initialWorkingSha256 = [string]$Pair.working.sha256
        initialWorkingLength = [long]$Pair.working.length
        initialWorkingLastWriteTimeUtcTicks = [long]$Pair.working.lastWriteTimeUtcTicks
        expectedGameName = $recorded.expectedGameName
        expectedGameId = $recorded.expectedGameId
        expectedArea = $recorded.expectedArea
        qualifiedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
        writableSaveNames = @($recorded.writableSaveNames)
    }
    Assert-KmcExactProperties ([pscustomobject]$replacement) $properties 'replacement KMC fixture qualification'
    $allowedChanges = @(
        'initialWorkingSha256','initialWorkingLength','initialWorkingLastWriteTimeUtcTicks','qualifiedAtUtc'
    )
    foreach ($property in $properties) {
        if ($property -notin $allowedChanges -and
            (($recorded.$property | ConvertTo-Json -Depth 5 -Compress) -cne ($replacement[$property] | ConvertTo-Json -Depth 5 -Compress))) {
            throw "KMC Working requalification attempted to change protected qualification field $property."
        }
    }

    return [pscustomobject]@{
        schemaVersion = 1
        existingQualificationSha256 = $actualQualificationSha256
        priorQualifiedAtUtc = [string]$recorded.qualifiedAtUtc
        supersededWorkingSha256 = [string]$recorded.initialWorkingSha256
        supersededWorkingLength = [long]$recorded.initialWorkingLength
        supersededWorkingLastWriteTimeUtcTicks = [long]$recorded.initialWorkingLastWriteTimeUtcTicks
        revisedWorkingSha256 = [string]$Pair.working.sha256
        qualification = $replacement
    }
}

function Invoke-KmcWorkingFixtureRequalificationTransaction {
    param(
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedExistingQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedBaselineSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedRevisedWorkingSha256,
        [Parameter(Mandatory = $true)][string]$PriorSaveTransactionStatePath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedPriorSaveTransactionRunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveTransactionStateSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorSaveMetadataDigest,
        [scriptblock]$BeforeReplacementProbe,
        [scriptblock]$AfterReplacementWriteBeforeStateProbe,
        [scriptblock]$PostWriteProbe,
        [scriptblock]$AfterCommittedStateProbe,
        [scriptblock]$BeforeRollbackProbe
    )
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $fullQualificationPath = [IO.Path]::GetFullPath($QualificationPath)
    $expectedQualificationPath = [IO.Path]::GetFullPath((Join-Path $fullStateRoot 'fixture-qualification.json'))
    Assert-KmcPathsDoNotOverlap -First $SaveRoot -Second $fullStateRoot -Description 'KMC save and runtime-state roots'
    if (-not [string]::Equals($fullQualificationPath, $expectedQualificationPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Working fixture requalification path is not the exact runtime-state fixture-qualification.json.'
    }
    # The caller performs every pure preflight before ShouldProcess. Opening the
    # exclusive runtime lock is deliberately this transaction's first mutation.
    $lock = Open-KmcRuntimeLock -StateRoot $StateRoot -RunId $RunId -Purpose 'fixture-requalification'
    $writeAttempted = $false
    $replacementProven = $false
    $state = $null
    $statePath = $null
    $priorBackupPath = $null
    $saveMetadataBefore = $null
    $qualificationMetadataBefore = $null
    $qualificationBytesBefore = $null
    try {
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        Assert-KmcPathsDoNotOverlap -First $SaveRoot -Second $StateRoot -Description 'KMC save and runtime-state roots'
        $saveMetadataBefore = Get-KmcSaveMetadataInventory $SaveRoot
        $pair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
        $requalification = New-KmcWorkingFixtureRequalification `
            -Pair $pair `
            -QualificationPath $QualificationPath `
            -ExpectedExistingQualificationSha256 $ExpectedExistingQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256
        $priorAuthority = Read-KmcPriorSaveTransactionAuthority `
            -Path $PriorSaveTransactionStatePath `
            -StateRoot $StateRoot `
            -SaveRoot $SaveRoot `
            -ExpectedRunId $ExpectedPriorSaveTransactionRunId `
            -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
            -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -CurrentPair $pair
        if ([long]$priorAuthority.priorWorkingLength -ne [long]$requalification.supersededWorkingLength -or
            [long]$priorAuthority.priorWorkingLastWriteTimeUtcTicks -ne [long]$requalification.supersededWorkingLastWriteTimeUtcTicks) {
            throw 'Prior save-transaction Working metadata differs from the superseded durable qualification.'
        }
        [void](Assert-KmcWorkingOnlyPriorInventoryTransition `
            -PriorAuthority $priorAuthority `
            -CurrentInventory $saveMetadataBefore `
            -CurrentPair $pair `
            -SaveRoot $SaveRoot `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)

        $qualificationMetadataBefore = Get-Item -LiteralPath $QualificationPath -Force
        $qualificationBytesBefore = [IO.File]::ReadAllBytes([IO.Path]::GetFullPath($QualificationPath))
        if ($qualificationBytesBefore.Length -ne $qualificationMetadataBefore.Length -or
            (Get-KmcSha256 $QualificationPath) -cne $ExpectedExistingQualificationSha256) {
            throw 'Existing KMC fixture qualification changed while its rollback bytes were being captured.'
        }

        $transactionRoot = Assert-KmcChildPath `
            (Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'fixture-requalifications') `
            ([IO.Path]::GetFullPath($StateRoot)) `
            'Working fixture requalification transaction root'
        if (-not (Test-Path -LiteralPath $transactionRoot -PathType Container)) {
            New-Item -ItemType Directory -Path $transactionRoot | Out-Null
        }
        Assert-KmcNotReparsePoint $transactionRoot 'Working fixture requalification transaction root'
        $statePath = Assert-KmcChildPath (Join-Path $transactionRoot ($RunId + '.json')) $transactionRoot 'Working fixture requalification state'
        $priorBackupPath = Assert-KmcChildPath (Join-Path $transactionRoot ($RunId + '.prior.json')) $transactionRoot 'prior fixture qualification backup'
        if ((Test-Path -LiteralPath $statePath) -or (Test-Path -LiteralPath $priorBackupPath)) {
            throw 'Working fixture requalification run ID already has durable state or prior bytes.'
        }
        Write-KmcBytesDurableAtomic -Path $priorBackupPath -Bytes $qualificationBytesBefore
        if ((Get-KmcSha256 $priorBackupPath) -cne $ExpectedExistingQualificationSha256 -or
            (Get-Item -LiteralPath $priorBackupPath -Force).Length -ne $qualificationMetadataBefore.Length) {
            throw 'Durable prior fixture qualification backup does not match the explicit existing pin.'
        }
        $state = [ordered]@{
            schemaVersion = 1
            runId = $RunId
            token = [string]$lock.Token
            phase = 'prepared'
            preparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
            saveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
            qualificationPath = [IO.Path]::GetFullPath($QualificationPath)
            priorQualificationBackupPath = $priorBackupPath
            priorQualificationSha256 = $ExpectedExistingQualificationSha256
            priorQualificationLength = [long]$qualificationMetadataBefore.Length
            priorQualificationLastWriteTimeUtcTicks = [long]$qualificationMetadataBefore.LastWriteTimeUtc.Ticks
            baselineSha256 = $ExpectedBaselineSha256
            supersededWorkingSha256 = $ExpectedSupersededWorkingSha256
            revisedWorkingSha256 = $ExpectedRevisedWorkingSha256
            saveMetadataDigestBefore = [string]$saveMetadataBefore.digest
        }
        Write-KmcJsonDurable -Path $statePath -Value $state

        if ($null -ne $BeforeReplacementProbe) { & $BeforeReplacementProbe $lock $statePath }
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $saveMetadataBefore `
            -After (Get-KmcSaveMetadataInventory $SaveRoot) `
            -Description 'Working fixture requalification pre-write save metadata'
        $pair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
        $requalification = New-KmcWorkingFixtureRequalification `
            -Pair $pair `
            -QualificationPath $QualificationPath `
            -ExpectedExistingQualificationSha256 $ExpectedExistingQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256

        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        Assert-KmcRecoveryLeafNoLinks $QualificationPath 'Working fixture requalification qualification before replacement'
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        $writeAttempted = $true
        Write-KmcJsonDurable -Path $QualificationPath -Value $requalification.qualification
        if ($null -ne $AfterReplacementWriteBeforeStateProbe) { & $AfterReplacementWriteBeforeStateProbe $lock $statePath }
        $state['phase'] = 'replacement-written'
        $state['replacementWrittenAtUtc'] = [DateTimeOffset]::UtcNow.ToString('o')
        $state['replacementQualificationSha256'] = Get-KmcSha256 $QualificationPath
        $replacementProven = $true
        Write-KmcJsonDurable -Path $statePath -Value $state
        if ($null -ne $PostWriteProbe) { & $PostWriteProbe $lock $statePath }

        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        $pair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
        $pair = Assert-KmcFixtureQualification -Pair $pair -QualificationPath $QualificationPath
        $saveMetadataAfter = Get-KmcSaveMetadataInventory $SaveRoot
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $saveMetadataBefore `
            -After $saveMetadataAfter `
            -Description 'Working fixture requalification final save metadata'
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        $state['phase'] = 'committed'
        $state['committedAtUtc'] = [DateTimeOffset]::UtcNow.ToString('o')
        $state['committedQualificationSha256'] = Get-KmcSha256 $QualificationPath
        $state['saveMetadataDigestAfter'] = [string]$saveMetadataAfter.digest
        Write-KmcJsonDurable -Path $statePath -Value $state
        if ($null -ne $AfterCommittedStateProbe) { & $AfterCommittedStateProbe $lock $statePath }
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        $postCommitContinuity = Assert-KmcQualifiedWorkingPriorInventoryContinuity `
            -SaveRoot $SaveRoot `
            -StateRoot $StateRoot `
            -QualificationPath $QualificationPath `
            -ExpectedCurrentQualificationSha256 ([string]$state.committedQualificationSha256) `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -PriorSaveTransactionStatePath $PriorSaveTransactionStatePath `
            -ExpectedPriorSaveTransactionRunId $ExpectedPriorSaveTransactionRunId `
            -ExpectedPriorSaveTransactionStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
            -ExpectedPriorSaveMetadataDigest $ExpectedPriorSaveMetadataDigest
        Assert-KmcSaveMetadataInventoriesEqual `
            -Before $saveMetadataAfter `
            -After $postCommitContinuity.saveMetadata `
            -Description 'Working fixture requalification post-commit save metadata'
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        Close-KmcRuntimeLock $lock
        $lock = $null
        return [pscustomobject]@{
            schemaVersion = 1
            pair = $pair
            supersededWorkingSha256 = $ExpectedSupersededWorkingSha256
            revisedWorkingSha256 = $ExpectedRevisedWorkingSha256
            qualificationSha256 = [string]$state.committedQualificationSha256
            saveMetadataDigest = [string]$state.saveMetadataDigestAfter
            transactionStatePath = $statePath
            priorQualificationBackupPath = $priorBackupPath
        }
    }
    catch {
        $primaryError = $_.Exception.Message
        if ($writeAttempted) {
            try {
                [void](Assert-KmcRuntimeLockOwner $lock)
                Assert-KmcNoGameProcesses
                if ($null -ne $BeforeRollbackProbe) { & $BeforeRollbackProbe $lock $statePath }
                [void](Assert-KmcRuntimeLockOwner $lock)
                Assert-KmcNoGameProcesses
                Assert-KmcRecoveryLeafNoLinks $QualificationPath 'Working fixture requalification qualification immediately before rollback'
                Write-KmcBytesDurableAtomic -Path $QualificationPath -Bytes $qualificationBytesBefore
                [IO.File]::SetLastWriteTimeUtc(
                    [IO.Path]::GetFullPath($QualificationPath),
                    [DateTime]$qualificationMetadataBefore.LastWriteTimeUtc)
                $restoredQualification = Get-Item -LiteralPath $QualificationPath -Force
                if ($restoredQualification.Length -ne $qualificationMetadataBefore.Length -or
                    $restoredQualification.LastWriteTimeUtc.Ticks -ne $qualificationMetadataBefore.LastWriteTimeUtc.Ticks -or
                    (Get-KmcSha256 $QualificationPath) -cne $ExpectedExistingQualificationSha256) {
                    throw 'Prior KMC fixture qualification bytes or metadata were not restored exactly.'
                }
                [void](Assert-KmcRuntimeLockOwner $lock)
                Assert-KmcNoGameProcesses
                Assert-KmcSaveMetadataInventoriesEqual `
                    -Before $saveMetadataBefore `
                    -After (Get-KmcSaveMetadataInventory $SaveRoot) `
                    -Description 'Working fixture requalification rollback save metadata'
                $revisedPair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
                [void](New-KmcWorkingFixtureRequalification `
                    -Pair $revisedPair `
                    -QualificationPath $QualificationPath `
                    -ExpectedExistingQualificationSha256 $ExpectedExistingQualificationSha256 `
                    -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
                    -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
                    -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
                $normalQualificationAccepted = $false
                try {
                    [void](Assert-KmcFixtureQualification -Pair $revisedPair -QualificationPath $QualificationPath)
                    $normalQualificationAccepted = $true
                }
                catch { }
                if ($normalQualificationAccepted) {
                    throw 'Rolled-back prior qualification unexpectedly admitted revised Working.'
                }
                $rollbackPhase = if ($replacementProven) { 'rolled-back' } else { 'replacement-write-attempt-rolled-back' }
                $state = New-KmcWorkingFixtureRequalificationPhaseState `
                    -SourceState $state `
                    -Phase $rollbackPhase `
                    -AdditionalValues ([ordered]@{
                        rolledBackAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
                        failure = $primaryError
                        restoredQualificationSha256 = Get-KmcSha256 $QualificationPath
                    })
                [void](Assert-KmcRuntimeLockOwner $lock)
                Assert-KmcNoGameProcesses
                Write-KmcJsonDurable -Path $statePath -Value $state
                [void](Assert-KmcRuntimeLockOwner $lock)
                Close-KmcRuntimeLock $lock
                $lock = $null
                throw "Working fixture requalification failed after replacement; the prior qualification was restored exactly: $primaryError"
            }
            catch {
                $rollbackError = $_.Exception.Message
                if ($rollbackError -like 'Working fixture requalification failed after replacement; the prior qualification was restored exactly:*') {
                    throw
                }
                try {
                    # A rollback can fail specifically because ownership was lost or a
                    # game process appeared.  In that case the last proven durable
                    # phase is already sufficient for recovery and must not be raced
                    # or regressed by an unauthorised failure-marker write.
                    [void](Assert-KmcRuntimeLockOwner $lock)
                    Assert-KmcNoGameProcesses
                    if ($null -ne $state -and $null -ne $statePath) {
                        $rollbackFailurePhase = if ($replacementProven) { 'rollback-failed' } else { 'replacement-write-attempt-rollback-failed' }
                        $state = New-KmcWorkingFixtureRequalificationPhaseState `
                            -SourceState $state `
                            -Phase $rollbackFailurePhase `
                            -AdditionalValues ([ordered]@{
                                rollbackFailedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
                                failure = $primaryError
                                rollbackFailure = $rollbackError
                            })
                        [void](Assert-KmcRuntimeLockOwner $lock)
                        Assert-KmcNoGameProcesses
                        Write-KmcJsonDurable -Path $statePath -Value $state
                    }
                }
                catch { $rollbackError += '; durable rollback-failure state also failed: ' + $_.Exception.Message }
                if ($null -ne $lock) {
                    try { Abandon-KmcRuntimeLock $lock }
                    catch { $rollbackError += '; runtime lock abandonment also failed: ' + $_.Exception.Message }
                    finally { $lock = $null }
                }
                throw "Working fixture requalification failed after replacement: $primaryError; prior qualification rollback failed: $rollbackError. The active runtime lock was retained to block runtime."
            }
        }

        if ($null -ne $state -and $null -ne $statePath) {
            try {
                [void](Assert-KmcRuntimeLockOwner $lock)
                Assert-KmcNoGameProcesses
                $state = New-KmcWorkingFixtureRequalificationPhaseState `
                    -SourceState $state `
                    -Phase 'aborted-before-replacement' `
                    -AdditionalValues ([ordered]@{
                        abortedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
                        failure = $primaryError
                    })
                [void](Assert-KmcRuntimeLockOwner $lock)
                Assert-KmcNoGameProcesses
                Write-KmcJsonDurable -Path $statePath -Value $state
            }
            catch { $primaryError += '; durable abort state also failed: ' + $_.Exception.Message }
        }
        if ($null -ne $lock) {
            try { Close-KmcRuntimeLock $lock }
            catch {
                $primaryError += '; runtime lock cleanup failed after the pre-replacement abort: ' + $_.Exception.Message
                try { Abandon-KmcRuntimeLock $lock }
                catch { $primaryError += '; runtime lock abandonment also failed: ' + $_.Exception.Message }
            }
            finally { $lock = $null }
        }
        throw $primaryError
    }
    finally {
        # Any unanticipated control path fails closed. Abandon keeps the durable
        # sentinel so no runtime scenario can enter without an explicit audit.
        if ($null -ne $lock) {
            try { Abandon-KmcRuntimeLock $lock }
            catch { }
            finally { $lock = $null }
        }
    }
}

function Get-KmcWorkingFixtureRequalificationStatePropertyNames {
    param([Parameter(Mandatory = $true)][string]$Phase)
    $base = @(
        'schemaVersion','runId','token','phase','preparedAtUtc','saveRoot','qualificationPath',
        'priorQualificationBackupPath','priorQualificationSha256','priorQualificationLength',
        'priorQualificationLastWriteTimeUtcTicks','baselineSha256','supersededWorkingSha256',
        'revisedWorkingSha256','saveMetadataDigestBefore'
    )
    $replacement = @('replacementWrittenAtUtc','replacementQualificationSha256')
    $extra = switch -CaseSensitive ($Phase) {
        'prepared' { @(); break }
        'replacement-written' { $replacement; break }
        'committed' { @($replacement + @('committedAtUtc','committedQualificationSha256','saveMetadataDigestAfter')); break }
        'rolled-back' { @($replacement + @('rolledBackAtUtc','failure','restoredQualificationSha256')); break }
        'rollback-failed' { @($replacement + @('rollbackFailedAtUtc','failure','rollbackFailure')); break }
        'replacement-write-attempt-rolled-back' { @('rolledBackAtUtc','failure','restoredQualificationSha256'); break }
        'replacement-write-attempt-rollback-failed' { @('rollbackFailedAtUtc','failure','rollbackFailure'); break }
        'aborted-before-replacement' { @('abortedAtUtc','failure'); break }
        'purpose-bound-recovery-prepared' { @('recoveryPreparedAtUtc'); break }
        'recovery-restore-prepared' { @('recoveryPreparedAtUtc'); break }
        'recovered-rolled-back' { @('recoveredAtUtc','recoveryAction','recoveryQualificationSha256','recoverySaveMetadataDigest'); break }
        'recovered-committed' {
            @($replacement + @(
                'committedAtUtc','committedQualificationSha256','saveMetadataDigestAfter',
                'recoveredAtUtc','recoveryAction','recoveryQualificationSha256','recoverySaveMetadataDigest'
            ))
            break
        }
        default { throw "Unknown Working fixture requalification phase: $Phase" }
    }
    return @($base + @($extra))
}

function Assert-KmcWorkingFixtureRequalificationStateSchema {
    param([Parameter(Mandatory = $true)]$State)
    if ($State -isnot [pscustomobject]) {
        throw 'Working fixture requalification recovery state is not an exact JSON object.'
    }
    $phaseProperty = $State.PSObject.Properties['phase']
    if ($null -eq $phaseProperty -or $phaseProperty.Value -isnot [string]) {
        throw 'Working fixture requalification recovery phase is not an exact JSON string.'
    }
    $phase = [string]$phaseProperty.Value
    $expectedProperties = @(Get-KmcWorkingFixtureRequalificationStatePropertyNames $phase)
    Assert-KmcExactProperties $State $expectedProperties "Working fixture requalification $phase state"
    if ((($State.schemaVersion -isnot [int]) -and ($State.schemaVersion -isnot [long])) -or
        [long]$State.schemaVersion -ne 1 -or
        (($State.priorQualificationLength -isnot [int]) -and ($State.priorQualificationLength -isnot [long])) -or
        (($State.priorQualificationLastWriteTimeUtcTicks -isnot [int]) -and ($State.priorQualificationLastWriteTimeUtcTicks -isnot [long])) -or
        [long]$State.priorQualificationLength -le 0 -or
        [long]$State.priorQualificationLastWriteTimeUtcTicks -le 0) {
        throw 'Working fixture requalification recovery state numeric fields are invalid.'
    }
    foreach ($name in @($expectedProperties | Where-Object {
        $_ -notin @('schemaVersion','priorQualificationLength','priorQualificationLastWriteTimeUtcTicks')
    })) {
        if ($State.$name -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$State.$name)) {
            throw "Working fixture requalification recovery state field $name is not an exact non-empty JSON string."
        }
    }
    if ([string]$State.runId -cnotmatch '^[A-Za-z0-9._-]{1,120}$' -or
        [string]$State.token -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Working fixture requalification recovery state run or token identity is invalid.'
    }
    foreach ($name in @(
        'priorQualificationSha256','baselineSha256','supersededWorkingSha256','revisedWorkingSha256',
        'saveMetadataDigestBefore','replacementQualificationSha256','committedQualificationSha256',
        'saveMetadataDigestAfter','restoredQualificationSha256','recoveryQualificationSha256',
        'recoverySaveMetadataDigest'
    )) {
        if ($null -ne $State.PSObject.Properties[$name] -and [string]$State.$name -cnotmatch '^[0-9a-f]{64}$') {
            throw "Working fixture requalification recovery state field $name is not a lowercase SHA-256."
        }
    }
    foreach ($name in @($expectedProperties | Where-Object { $_ -clike '*AtUtc' })) {
        $parsed = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParseExact(
            [string]$State.$name,
            'o',
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$parsed) -or $parsed.Offset -ne [TimeSpan]::Zero) {
            throw "Working fixture requalification recovery state timestamp $name is not exact UTC round-trip form."
        }
    }
    if ($phase -ceq 'recovered-rolled-back' -and
        [string]$State.recoveryAction -cnotin @('prior-restored','prior-confirmed','prior-retained-state-less')) {
        throw 'Recovered rolled-back Working fixture state has an invalid recovery action.'
    }
    if ($phase -ceq 'recovered-committed' -and [string]$State.recoveryAction -cne 'committed') {
        throw 'Recovered committed Working fixture state has an invalid recovery action.'
    }
    return $State
}

function Assert-KmcRecoveryLeafNoLinks {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Description)
    if (-not (Test-Path -LiteralPath $Path)) { throw "$Description is missing: $Path" }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Description is not an exact file: $Path" }
    Assert-KmcNotReparsePoint $Path $Description
    Assert-KmcNotHardLink $Path $Description
}

function New-KmcWorkingFixtureRequalificationPhaseState {
    param(
        [Parameter(Mandatory = $true)]$SourceState,
        [Parameter(Mandatory = $true)][string]$Phase,
        [Collections.IDictionary]$AdditionalValues = @{}
    )
    $result = [ordered]@{}
    foreach ($name in @(Get-KmcWorkingFixtureRequalificationStatePropertyNames $Phase)) {
        if ($name -ceq 'phase') { $result[$name] = $Phase; continue }
        if ($AdditionalValues.Contains($name)) { $result[$name] = $AdditionalValues[$name]; continue }
        if ($SourceState -is [Collections.IDictionary] -and $SourceState.Contains($name)) {
            $result[$name] = $SourceState[$name]
            continue
        }
        $property = $SourceState.PSObject.Properties[$name]
        if ($null -eq $property) { throw "Cannot construct Working fixture phase $Phase without field $name." }
        $result[$name] = $property.Value
    }
    $value = [pscustomobject]$result
    [void](Assert-KmcWorkingFixtureRequalificationStateSchema $value)
    return $value
}

function Get-KmcWorkingFixtureRequalificationAtomicDebris {
    param([Parameter(Mandatory = $true)][string[]]$TargetPaths)
    $records = New-Object 'Collections.Generic.List[object]'
    foreach ($targetPathValue in @($TargetPaths | Select-Object -Unique)) {
        $targetPath = [IO.Path]::GetFullPath($targetPathValue)
        $parent = Split-Path -Parent $targetPath
        if (-not (Test-Path -LiteralPath $parent -PathType Container)) { continue }
        Assert-KmcNotReparsePoint $parent 'Working fixture requalification atomic-debris parent'
        $leaf = [IO.Path]::GetFileName($targetPath)
        $escapedLeaf = [Regex]::Escape($leaf)
        foreach ($item in @(Get-ChildItem -LiteralPath $parent -Force | Where-Object {
            $_.Name -cmatch ('^\.' + $escapedLeaf + '\..+\.(tmp|bak)$')
        })) {
            if ($item.Name -cnotmatch ('^\.' + $escapedLeaf + '\.[0-9a-f]{32}\.(tmp|bak)$')) {
                throw "Working fixture requalification has unrecognized atomic debris: $($item.FullName)"
            }
            $kind = [string]$Matches[1]
            if ($item.PSIsContainer) { throw "Working fixture requalification atomic debris is not a file: $($item.FullName)" }
            Assert-KmcNotReparsePoint $item.FullName 'Working fixture requalification atomic debris'
            Assert-KmcNotHardLink $item.FullName 'Working fixture requalification atomic debris'
            if ([long]$item.Length -gt 256KB) { throw "Working fixture requalification atomic debris is oversized: $($item.FullName)" }
            if ($kind -ceq 'bak' -and -not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
                throw "Working fixture requalification backup debris lacks its canonical target: $($item.FullName)"
            }
            $records.Add([pscustomobject]@{
                schemaVersion = 1; targetPath = $targetPath; path = [IO.Path]::GetFullPath($item.FullName)
                kind = $kind; length = [long]$item.Length
                lastWriteTimeUtcTicks = [long]$item.LastWriteTimeUtc.Ticks
                sha256 = Get-KmcSha256 $item.FullName
            })
        }
    }
    if ($records.Count -gt 6) { throw 'Working fixture requalification atomic debris exceeds the bounded recovery limit.' }
    return @($records | Sort-Object path)
}

function Remove-KmcWorkingFixtureRequalificationAtomicDebris {
    param(
        [Parameter(Mandatory = $true)]$Lock,
        [Parameter(Mandatory = $true)][array]$Debris
    )
    [void](Assert-KmcRuntimeLockOwner $Lock)
    Assert-KmcNoGameProcesses
    $targets = New-Object 'Collections.Generic.List[string]'
    foreach ($record in @($Debris)) {
        Assert-KmcExactProperties $record @('schemaVersion','targetPath','path','kind','length','lastWriteTimeUtcTicks','sha256') 'Working fixture requalification atomic-debris record'
        if ([int]$record.schemaVersion -ne 1 -or [string]$record.kind -cnotin @('tmp','bak') -or
            [string]$record.sha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Working fixture requalification atomic-debris record is invalid.' }
        $path = [IO.Path]::GetFullPath([string]$record.path)
        $targetPath = [IO.Path]::GetFullPath([string]$record.targetPath)
        $parent = Split-Path -Parent $targetPath
        if (-not [string]::Equals((Split-Path -Parent $path), $parent, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Working fixture requalification atomic debris escaped its canonical target parent.'
        }
        Assert-KmcRecoveryLeafNoLinks $path 'Working fixture requalification atomic debris before removal'
        $item = Get-Item -LiteralPath $path -Force
        if ([long]$item.Length -ne [long]$record.length -or
            [long]$item.LastWriteTimeUtc.Ticks -ne [long]$record.lastWriteTimeUtcTicks -or
            (Get-KmcSha256 $path) -cne [string]$record.sha256) {
            throw 'Working fixture requalification atomic debris changed after its guarded snapshot.'
        }
        if ([string]$record.kind -ceq 'bak' -and -not (Test-Path -LiteralPath $targetPath -PathType Leaf)) {
            throw 'Working fixture requalification backup debris lost its canonical target.'
        }
        [void](Assert-KmcRuntimeLockOwner $Lock)
        Assert-KmcNoGameProcesses
        Remove-Item -LiteralPath $path -Force
        $targets.Add($targetPath)
    }
    $remaining = @(Get-KmcWorkingFixtureRequalificationAtomicDebris @($targets))
    if ($remaining.Count -ne 0) { throw 'Working fixture requalification atomic debris was not reconciled completely.' }
}

function New-KmcRecoveredWorkingFixtureRequalificationState {
    param(
        [Parameter(Mandatory = $true)]$SourceState,
        [Parameter(Mandatory = $true)][ValidateSet('recovered-rolled-back','recovered-committed')][string]$Phase,
        [Parameter(Mandatory = $true)][string]$RecoveryAction,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$QualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$SaveMetadataDigest
    )
    $baseNames = @(Get-KmcWorkingFixtureRequalificationStatePropertyNames 'prepared')
    $terminal = [ordered]@{}
    foreach ($name in $baseNames) {
        $terminal[$name] = if ($name -ceq 'phase') { $Phase } else { $SourceState.$name }
    }
    if ($Phase -ceq 'recovered-committed') {
        foreach ($name in @(
            'replacementWrittenAtUtc','replacementQualificationSha256','committedAtUtc',
            'committedQualificationSha256','saveMetadataDigestAfter'
        )) { $terminal[$name] = $SourceState.$name }
    }
    $terminal['recoveredAtUtc'] = [DateTimeOffset]::UtcNow.ToString('o')
    $terminal['recoveryAction'] = $RecoveryAction
    $terminal['recoveryQualificationSha256'] = $QualificationSha256
    $terminal['recoverySaveMetadataDigest'] = $SaveMetadataDigest
    $result = [pscustomobject]$terminal
    [void](Assert-KmcWorkingFixtureRequalificationStateSchema $result)
    return $result
}

function Get-KmcWorkingFixtureRequalificationRecoveryPlan {
    param(
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedBaselineSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedRevisedWorkingSha256,
        $OwnedLock
    )
    Assert-KmcNoGameProcesses
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $fullQualificationPath = [IO.Path]::GetFullPath($QualificationPath)
    $expectedQualificationPath = [IO.Path]::GetFullPath((Join-Path $fullStateRoot 'fixture-qualification.json'))
    Assert-KmcPathsDoNotOverlap -First $SaveRoot -Second $fullStateRoot -Description 'KMC save and runtime-state roots'
    if (-not (Test-Path -LiteralPath $fullStateRoot)) { throw "Runtime state root is missing: $fullStateRoot" }
    if (-not (Test-Path -LiteralPath $fullStateRoot -PathType Container)) { throw 'Runtime state root is not an exact directory.' }
    Assert-KmcNotReparsePoint $fullStateRoot 'runtime state root'
    if (-not [string]::Equals($fullQualificationPath, $expectedQualificationPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Working fixture requalification recovery path is not the exact runtime-state fixture-qualification.json.'
    }
    Assert-KmcRecoveryLeafNoLinks $fullQualificationPath 'Working fixture requalification recovery qualification'

    $transactionRoot = Assert-KmcChildPath (Join-Path $fullStateRoot 'fixture-requalifications') $fullStateRoot 'Working fixture requalification transaction root'
    $transactionRootExists = Test-Path -LiteralPath $transactionRoot
    if ($transactionRootExists) {
        if (-not (Test-Path -LiteralPath $transactionRoot -PathType Container)) {
            throw 'Working fixture requalification transaction root is not an exact directory.'
        }
        Assert-KmcNotReparsePoint $transactionRoot 'Working fixture requalification transaction root'
    }
    $statePath = Assert-KmcChildPath (Join-Path $transactionRoot ($RunId + '.json')) $transactionRoot 'Working fixture requalification recovery state'
    $priorBackupPath = Assert-KmcChildPath (Join-Path $transactionRoot ($RunId + '.prior.json')) $transactionRoot 'Working fixture prior qualification backup'
    $stateExists = Test-Path -LiteralPath $statePath
    $backupExists = Test-Path -LiteralPath $priorBackupPath
    if ($stateExists) { Assert-KmcRecoveryLeafNoLinks $statePath 'Working fixture requalification recovery state' }
    if ($backupExists) { Assert-KmcRecoveryLeafNoLinks $priorBackupPath 'Working fixture prior qualification backup' }
    $atomicDebris = @(Get-KmcWorkingFixtureRequalificationAtomicDebris @(
        $fullQualificationPath,$statePath,$priorBackupPath
    ))

    $lockPath = Assert-KmcChildPath (Join-Path $fullStateRoot 'active-transaction.lock') $fullStateRoot 'Working fixture requalification recovery lock'
    $lockExists = Test-Path -LiteralPath $lockPath
    $rawLock = $null
    if ($lockExists) {
        Assert-KmcRecoveryLeafNoLinks $lockPath 'Working fixture requalification recovery lock'
        $rawLock = if ($null -ne $OwnedLock) {
            [void](Assert-KmcRuntimeLockOwner $OwnedLock)
            Read-KmcOpenLockPayload $OwnedLock
        } else { Read-KmcJson $lockPath }
        Assert-KmcExactProperties $rawLock @('schemaVersion','runId','token','ownerProcessId','createdAtUtc','purpose') 'Working fixture requalification recovery lock'
        if ((($rawLock.schemaVersion -isnot [int]) -and ($rawLock.schemaVersion -isnot [long])) -or
            [long]$rawLock.schemaVersion -ne 1 -or $rawLock.runId -isnot [string] -or
            [string]$rawLock.runId -cne $RunId -or $rawLock.token -isnot [string] -or
            [string]$rawLock.token -cnotmatch '^[0-9a-f]{64}$' -or
            (($rawLock.ownerProcessId -isnot [int]) -and ($rawLock.ownerProcessId -isnot [long])) -or
            $rawLock.createdAtUtc -isnot [string] -or $rawLock.purpose -isnot [string] -or
            [string]$rawLock.purpose -cne 'fixture-requalification') {
            throw 'Working fixture requalification recovery lock identity or types are invalid.'
        }
        $lockCreatedAt = [DateTimeOffset]::MinValue
        if ([int]$rawLock.ownerProcessId -le 0 -or -not [DateTimeOffset]::TryParseExact(
            [string]$rawLock.createdAtUtc,
            'o',
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$lockCreatedAt) -or $lockCreatedAt.Offset -ne [TimeSpan]::Zero) {
            throw 'Working fixture requalification recovery lock process or timestamp is invalid.'
        }
        if ($null -ne $OwnedLock) {
            if (-not [string]::Equals([IO.Path]::GetFullPath([string]$OwnedLock.Path), $lockPath, [StringComparison]::OrdinalIgnoreCase) -or
                [string]$OwnedLock.RunId -cne $RunId -or [string]$OwnedLock.Token -cne [string]$rawLock.token) {
                throw 'Adopted Working fixture requalification lock does not match the recovery request.'
            }
        }
        elseif ($null -ne (Get-Process -Id ([int]$rawLock.ownerProcessId) -ErrorAction SilentlyContinue) -or
            [int]$rawLock.ownerProcessId -eq $PID) {
            throw 'Working fixture requalification recovery lock owner is not proven dead.'
        }
    }
    elseif ($null -ne $OwnedLock) { throw 'Adopted Working fixture requalification lock disappeared.' }
    if (-not $lockExists -and $atomicDebris.Count -ne 0) {
        throw 'Working fixture requalification terminal state has atomic debris but no stale lock for guarded reconciliation.'
    }

    $pair = Get-KmcValidatedFixturePair -SaveRoot $SaveRoot
    $saveMetadata = Get-KmcSaveMetadataInventory $SaveRoot
    $qualificationHash = Get-KmcSha256 $fullQualificationPath
    $qualificationMetadata = Get-Item -LiteralPath $fullQualificationPath -Force

    if (-not $stateExists) {
        if (-not $lockExists) { throw 'Working fixture requalification recovery has neither a stale lock nor a terminal state.' }
        foreach ($otherRootName in @('transactions','save-transactions','run-transactions')) {
            $otherState = Join-Path (Join-Path $fullStateRoot $otherRootName) ($RunId + '.json')
            if (Test-Path -LiteralPath $otherState) {
                throw "State-less fixture requalification recovery collides with another runtime transaction record: $otherState"
            }
        }
        if ($qualificationHash -cne $ExpectedPriorQualificationSha256) {
            throw 'A state-less stale requalification lock is recoverable only while the prior qualification remains exact.'
        }
        [void](New-KmcWorkingFixtureRequalification `
            -Pair $pair `
            -QualificationPath $fullQualificationPath `
            -ExpectedExistingQualificationSha256 $ExpectedPriorQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
        if ($backupExists) {
            $backupMetadata = Get-Item -LiteralPath $priorBackupPath -Force
            if ((Get-KmcSha256 $priorBackupPath) -cne $ExpectedPriorQualificationSha256 -or
                $backupMetadata.Length -ne $qualificationMetadata.Length) {
                throw 'State-less stale requalification prior backup differs from the explicit prior qualification.'
            }
            [void](New-KmcWorkingFixtureRequalification `
                -Pair $pair `
                -QualificationPath $priorBackupPath `
                -ExpectedExistingQualificationSha256 $ExpectedPriorQualificationSha256 `
                -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
                -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
        }
        return [pscustomobject]@{
            schemaVersion = 1; action = $(if ($backupExists) { 'clear-prepared-lock' } else { 'prepare-purpose-bound-lock' }); pair = $pair; lock = $rawLock
            saveMetadata = $saveMetadata; qualificationSha256 = $qualificationHash
            qualificationLength = [long]$qualificationMetadata.Length
            qualificationLastWriteTimeUtcTicks = [long]$qualificationMetadata.LastWriteTimeUtc.Ticks
            state = $null; statePath = $statePath; priorBackupPath = $priorBackupPath
            atomicDebris = $atomicDebris
        }
    }

    $state = Read-KmcJson $statePath
    [void](Assert-KmcWorkingFixtureRequalificationStateSchema $state)
    $phase = [string]$state.phase
    if ([string]$state.runId -cne $RunId -or
        ($lockExists -and [string]$state.token -cne [string]$rawLock.token) -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$state.saveRoot).TrimEnd('\'), [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$state.qualificationPath), $fullQualificationPath, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$state.priorQualificationBackupPath), $priorBackupPath, [StringComparison]::OrdinalIgnoreCase) -or
        [string]$state.priorQualificationSha256 -cne $ExpectedPriorQualificationSha256 -or
        [string]$state.baselineSha256 -cne $ExpectedBaselineSha256 -or
        [string]$state.supersededWorkingSha256 -cne $ExpectedSupersededWorkingSha256 -or
        [string]$state.revisedWorkingSha256 -cne $ExpectedRevisedWorkingSha256 -or
        [string]$state.saveMetadataDigestBefore -cne [string]$saveMetadata.digest) {
        throw 'Working fixture requalification recovery state identity, pins, roots, or save digest are invalid.'
    }
    if (-not $backupExists -and $phase -cne 'purpose-bound-recovery-prepared') {
        throw 'Working fixture requalification recovery state lacks its exact prior qualification backup.'
    }
    if ($backupExists) {
        if ((Get-KmcSha256 $priorBackupPath) -cne $ExpectedPriorQualificationSha256 -or
            (Get-Item -LiteralPath $priorBackupPath -Force).Length -ne [long]$state.priorQualificationLength) {
            throw 'Working fixture prior qualification backup differs from its durable pins.'
        }
        [void](New-KmcWorkingFixtureRequalification `
            -Pair $pair `
            -QualificationPath $priorBackupPath `
            -ExpectedExistingQualificationSha256 $ExpectedPriorQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
    }

    $qualificationDisposition = 'unknown'
    if ($qualificationHash -ceq $ExpectedPriorQualificationSha256) {
        if ([long]$qualificationMetadata.Length -ne [long]$state.priorQualificationLength -or
            [long]$qualificationMetadata.LastWriteTimeUtc.Ticks -ne [long]$state.priorQualificationLastWriteTimeUtcTicks) {
            if ($lockExists -and $phase -cnotin @('recovered-rolled-back','recovered-committed')) {
                $qualificationDisposition = 'prior-metadata-incomplete'
            }
            else { throw 'Prior Working fixture qualification length or timestamp differs from its durable state pins.' }
        }
        else {
            [void](New-KmcWorkingFixtureRequalification `
                -Pair $pair `
                -QualificationPath $fullQualificationPath `
                -ExpectedExistingQualificationSha256 $ExpectedPriorQualificationSha256 `
                -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
                -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256)
            $qualificationDisposition = 'prior'
        }
    }
    else {
        try {
            [void](Assert-KmcFixtureQualification -Pair $pair -QualificationPath $fullQualificationPath)
            if ([string]$pair.working.sha256 -ceq $ExpectedRevisedWorkingSha256) {
                $qualificationDisposition = 'revised'
            }
        }
        catch { }
    }
    if ($null -ne $state.PSObject.Properties['replacementQualificationSha256'] -and
        [string]$state.replacementQualificationSha256 -ceq $ExpectedPriorQualificationSha256) {
        throw 'Replacement-written Working fixture state records the prior qualification as its replacement.'
    }
    if ($phase -ceq 'purpose-bound-recovery-prepared') {
        if ($qualificationDisposition -cne 'prior') {
            throw 'Purpose-bound prepared recovery requires the exact prior qualification and metadata.'
        }
        $action = if ($backupExists) { 'confirm-prior' } else { 'prepare-purpose-bound-lock' }
    }
    elseif ($phase -cin @('committed','recovered-committed')) {
        if ($qualificationDisposition -cne 'revised' -or
            [string]$state.replacementQualificationSha256 -cne $qualificationHash -or
            [string]$state.committedQualificationSha256 -cne $qualificationHash -or
            [string]$state.saveMetadataDigestAfter -cne [string]$saveMetadata.digest) {
            throw 'Committed Working fixture requalification state does not match an exact normally qualified revised fixture.'
        }
        if ($phase -ceq 'recovered-committed') {
            if ([string]$state.recoveryQualificationSha256 -cne $qualificationHash -or
                [string]$state.recoverySaveMetadataDigest -cne [string]$saveMetadata.digest) {
                throw 'Recovered committed Working fixture state does not match its terminal recovery proof.'
            }
            $action = if ($lockExists) { 'complete-terminal-committed' } else { 'already-recovered-committed' }
        }
        else { $action = if ($lockExists) { 'accept-committed' } else { 'already-committed' } }
    }
    elseif ($phase -ceq 'recovered-rolled-back') {
        if ($qualificationDisposition -cne 'prior' -or
            [string]$state.recoveryQualificationSha256 -cne $qualificationHash -or
            [string]$state.recoverySaveMetadataDigest -cne [string]$saveMetadata.digest) {
            throw 'Recovered rolled-back Working fixture state does not match its exact terminal recovery proof.'
        }
        $action = if ($lockExists) { 'complete-terminal-prior' } else { 'already-recovered-prior' }
    }
    elseif (-not $lockExists) {
        throw 'Nonterminal Working fixture requalification state has no exact stale runtime lock.'
    }
    elseif ($phase -cin @('rolled-back','replacement-write-attempt-rolled-back','aborted-before-replacement') -and
        $qualificationDisposition -cnotin @('prior','prior-metadata-incomplete')) {
        throw "Working fixture requalification phase $phase requires the exact prior qualification."
    }
    elseif ($phase -cin @('rolled-back','replacement-write-attempt-rolled-back') -and
        [string]$state.restoredQualificationSha256 -cne $ExpectedPriorQualificationSha256) {
        throw 'Rolled-back Working fixture requalification state lacks the exact restored prior hash.'
    }
    elseif ($qualificationDisposition -ceq 'prior') { $action = 'confirm-prior' }
    else { $action = 'restore-prior' }

    return [pscustomobject]@{
        schemaVersion = 1; action = $action; pair = $pair; lock = $rawLock
        saveMetadata = $saveMetadata; qualificationSha256 = $qualificationHash
        qualificationLength = [long]$qualificationMetadata.Length
        qualificationLastWriteTimeUtcTicks = [long]$qualificationMetadata.LastWriteTimeUtc.Ticks
        state = $state; statePath = $statePath; priorBackupPath = $priorBackupPath
        atomicDebris = $atomicDebris
    }
}

function Invoke-KmcWorkingFixtureRequalificationRecovery {
    param(
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedPriorQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedBaselineSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSupersededWorkingSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedRevisedWorkingSha256,
        [scriptblock]$AfterAdoptBeforeOwnedPlanProbe,
        [scriptblock]$AfterDebrisReconciliationProbe,
        [scriptblock]$AfterPurposeBoundBackupProbe,
        [scriptblock]$BeforeRestoreProbe,
        [scriptblock]$AfterRecoveryBytesBeforeTimestampProbe,
        [scriptblock]$BeforeRecoveryStateWriteProbe
    )
    Assert-KmcPathsDoNotOverlap -First $SaveRoot -Second $StateRoot -Description 'KMC save and runtime-state roots'
    $initialPlan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
        -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
        -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
        -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
        -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256
    if ([string]$initialPlan.action -cin @('already-recovered-prior','already-recovered-committed','already-committed')) {
        return [pscustomobject]@{
            schemaVersion = 1; disposition = [string]$initialPlan.action
            qualificationSha256 = [string]$initialPlan.qualificationSha256
            saveMetadataDigest = [string]$initialPlan.saveMetadata.digest
            statePath = [string]$initialPlan.statePath
        }
    }
    $lock = Adopt-KmcStaleRuntimeLock -StateRoot $StateRoot -ExpectedPurpose 'fixture-requalification'
    try {
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        if ($null -ne $AfterAdoptBeforeOwnedPlanProbe) { & $AfterAdoptBeforeOwnedPlanProbe $lock }
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        $plan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
            -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
            -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 `
            -OwnedLock $lock
        $ownedSaveMetadataBefore = $plan.saveMetadata
        $wasStateLess = $null -eq $plan.state
        if ($wasStateLess) {
            $transactionRoot = Split-Path -Parent ([string]$plan.statePath)
            if (Test-Path -LiteralPath $transactionRoot) {
                if (-not (Test-Path -LiteralPath $transactionRoot -PathType Container)) { throw 'Purpose-bound recovery transaction root is not an exact directory.' }
            }
            else { New-Item -ItemType Directory -Path $transactionRoot | Out-Null }
            Assert-KmcNotReparsePoint $transactionRoot 'Working fixture requalification transaction root'
            $purposeSourceState = [pscustomobject][ordered]@{
                schemaVersion = 1; runId = $RunId; token = [string]$lock.Token; phase = 'prepared'
                preparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
                saveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
                qualificationPath = [IO.Path]::GetFullPath($QualificationPath)
                priorQualificationBackupPath = [string]$plan.priorBackupPath
                priorQualificationSha256 = $ExpectedPriorQualificationSha256
                priorQualificationLength = [long]$plan.qualificationLength
                priorQualificationLastWriteTimeUtcTicks = [long]$plan.qualificationLastWriteTimeUtcTicks
                baselineSha256 = $ExpectedBaselineSha256
                supersededWorkingSha256 = $ExpectedSupersededWorkingSha256
                revisedWorkingSha256 = $ExpectedRevisedWorkingSha256
                saveMetadataDigestBefore = [string]$ownedSaveMetadataBefore.digest
            }
            $purposePreparedState = New-KmcWorkingFixtureRequalificationPhaseState `
                -SourceState $purposeSourceState -Phase 'purpose-bound-recovery-prepared' `
                -AdditionalValues ([ordered]@{ recoveryPreparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o') })
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            Write-KmcJsonDurable -Path ([string]$plan.statePath) -Value $purposePreparedState
            $plan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
                -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
                -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
                -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 -OwnedLock $lock
            Assert-KmcSaveMetadataInventoriesEqual -Before $ownedSaveMetadataBefore -After $plan.saveMetadata -Description 'Working fixture purpose-bound recovery-state save metadata'
        }
        if (@($plan.atomicDebris).Count -ne 0) {
            Remove-KmcWorkingFixtureRequalificationAtomicDebris -Lock $lock -Debris @($plan.atomicDebris)
            if ($null -ne $AfterDebrisReconciliationProbe) { & $AfterDebrisReconciliationProbe $lock }
            $plan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
                -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
                -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
                -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 -OwnedLock $lock
            Assert-KmcSaveMetadataInventoriesEqual -Before $ownedSaveMetadataBefore -After $plan.saveMetadata -Description 'Working fixture requalification debris-recovery save metadata'
            if (@($plan.atomicDebris).Count -ne 0) {
                throw 'Working fixture requalification atomic debris changed during guarded reconciliation.'
            }
        }
        if ([string]$plan.action -ceq 'prepare-purpose-bound-lock') {
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            $transactionRoot = Split-Path -Parent ([string]$plan.priorBackupPath)
            if (Test-Path -LiteralPath $transactionRoot) {
                if (-not (Test-Path -LiteralPath $transactionRoot -PathType Container)) { throw 'Purpose-bound recovery transaction root is not an exact directory.' }
            }
            else { New-Item -ItemType Directory -Path $transactionRoot | Out-Null }
            Assert-KmcNotReparsePoint $transactionRoot 'Working fixture requalification transaction root'
            Assert-KmcRecoveryLeafNoLinks $QualificationPath 'purpose-bound prior qualification source'
            $priorMetadata = Get-Item -LiteralPath $QualificationPath -Force
            if ((Get-KmcSha256 $QualificationPath) -cne $ExpectedPriorQualificationSha256 -or
                [long]$priorMetadata.Length -ne [long]$plan.qualificationLength -or
                [long]$priorMetadata.LastWriteTimeUtc.Ticks -ne [long]$plan.qualificationLastWriteTimeUtcTicks) {
                throw 'Purpose-bound prior qualification changed before durable backup creation.'
            }
            $priorBytes = [IO.File]::ReadAllBytes([IO.Path]::GetFullPath($QualificationPath))
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            Write-KmcBytesDurableAtomic -Path ([string]$plan.priorBackupPath) -Bytes $priorBytes
            if ($null -ne $AfterPurposeBoundBackupProbe) { & $AfterPurposeBoundBackupProbe $lock }
            $plan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
                -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
                -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
                -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
                -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 -OwnedLock $lock
            Assert-KmcSaveMetadataInventoriesEqual -Before $ownedSaveMetadataBefore -After $plan.saveMetadata -Description 'Working fixture purpose-bound backup save metadata'
            if ([string]$plan.action -cne 'confirm-prior') { throw 'Purpose-bound requalification backup did not revalidate exactly.' }
            if (@($plan.atomicDebris).Count -ne 0) { throw 'Purpose-bound requalification backup left atomic debris.' }
        }
        if ([string]$plan.action -ceq 'restore-prior') {
            if ($null -ne $BeforeRestoreProbe) { & $BeforeRestoreProbe $lock ([string]$plan.statePath) }
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            if ([string]$plan.state.phase -cne 'recovery-restore-prepared') {
                $restorePreparedState = New-KmcWorkingFixtureRequalificationPhaseState `
                    -SourceState $plan.state `
                    -Phase 'recovery-restore-prepared' `
                    -AdditionalValues ([ordered]@{ recoveryPreparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o') })
                $restoreStateParent = Split-Path -Parent ([string]$plan.statePath)
                Assert-KmcNotReparsePoint $restoreStateParent 'Working fixture requalification transaction root'
                Assert-KmcRecoveryLeafNoLinks ([string]$plan.statePath) 'Working fixture requalification recovery state'
                [void](Assert-KmcRuntimeLockOwner $lock)
                Assert-KmcNoGameProcesses
                Write-KmcJsonDurable -Path ([string]$plan.statePath) -Value $restorePreparedState
                $plan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
                    -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
                    -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
                    -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
                    -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
                    -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 -OwnedLock $lock
                if ([string]$plan.action -cne 'restore-prior') { throw 'Prepared qualification restoration did not remain exact.' }
            }
            Assert-KmcRecoveryLeafNoLinks $QualificationPath 'Working fixture requalification recovery qualification'
            Assert-KmcRecoveryLeafNoLinks ([string]$plan.priorBackupPath) 'Working fixture prior qualification backup'
            $priorBytes = [IO.File]::ReadAllBytes([string]$plan.priorBackupPath)
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            Assert-KmcRecoveryLeafNoLinks $QualificationPath 'Working fixture requalification recovery qualification immediately before restore'
            Write-KmcBytesDurableAtomic -Path $QualificationPath -Bytes $priorBytes
            if ($null -ne $AfterRecoveryBytesBeforeTimestampProbe) { & $AfterRecoveryBytesBeforeTimestampProbe $lock ([string]$plan.statePath) }
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            [IO.File]::SetLastWriteTimeUtc(
                [IO.Path]::GetFullPath($QualificationPath),
                (New-Object DateTime -ArgumentList ([long]$plan.state.priorQualificationLastWriteTimeUtcTicks), ([DateTimeKind]::Utc)))
        }
        $finalPlan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
            -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
            -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 `
            -OwnedLock $lock
        if (@($finalPlan.atomicDebris).Count -ne 0) { throw 'Working fixture requalification recovery produced unresolved atomic debris.' }
        if ([string]$plan.action -cin @('accept-committed','complete-terminal-committed')) {
            if ([string]$finalPlan.action -cnotin @('accept-committed','complete-terminal-committed')) { throw 'Committed requalification recovery did not remain exact.' }
            $recoveryDisposition = 'committed'
        }
        else {
            if ([string]$finalPlan.action -cnotin @('confirm-prior','clear-prepared-lock','complete-terminal-prior')) {
                throw 'Working fixture requalification recovery did not restore or retain the exact prior qualification.'
            }
            $recoveryDisposition = 'prior-restored'
        }
        Assert-KmcSaveMetadataInventoriesEqual -Before $ownedSaveMetadataBefore -After $finalPlan.saveMetadata -Description 'Working fixture requalification recovery save metadata'
        $terminalAlreadyWritten = [string]$finalPlan.action -cin @('complete-terminal-prior','complete-terminal-committed')
        if (-not $terminalAlreadyWritten) {
            $sourceState = $finalPlan.state
            $recoveryAction = if ($recoveryDisposition -ceq 'committed') { 'committed' }
                elseif ($wasStateLess) { 'prior-retained-state-less' }
                elseif ([string]$plan.action -ceq 'restore-prior') { 'prior-restored' }
                else { 'prior-confirmed' }
            if ($null -eq $sourceState) {
                $sourceState = [pscustomobject][ordered]@{
                    schemaVersion = 1; runId = $RunId; token = [string]$lock.Token; phase = 'prepared'
                    preparedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
                    saveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
                    qualificationPath = [IO.Path]::GetFullPath($QualificationPath)
                    priorQualificationBackupPath = [string]$finalPlan.priorBackupPath
                    priorQualificationSha256 = $ExpectedPriorQualificationSha256
                    priorQualificationLength = [long]$finalPlan.qualificationLength
                    priorQualificationLastWriteTimeUtcTicks = [long]$finalPlan.qualificationLastWriteTimeUtcTicks
                    baselineSha256 = $ExpectedBaselineSha256
                    supersededWorkingSha256 = $ExpectedSupersededWorkingSha256
                    revisedWorkingSha256 = $ExpectedRevisedWorkingSha256
                    saveMetadataDigestBefore = [string]$finalPlan.saveMetadata.digest
                }
            }
            $terminalPhase = if ($recoveryDisposition -ceq 'committed') { 'recovered-committed' } else { 'recovered-rolled-back' }
            $terminalState = New-KmcRecoveredWorkingFixtureRequalificationState `
                -SourceState $sourceState -Phase $terminalPhase -RecoveryAction $recoveryAction `
                -QualificationSha256 (Get-KmcSha256 $QualificationPath) `
                -SaveMetadataDigest ([string]$finalPlan.saveMetadata.digest)
            if ($null -ne $BeforeRecoveryStateWriteProbe) { & $BeforeRecoveryStateWriteProbe $lock ([string]$finalPlan.statePath) }
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            $transactionRoot = Split-Path -Parent ([string]$finalPlan.statePath)
            if (-not (Test-Path -LiteralPath $transactionRoot -PathType Container)) { throw 'Recovery transaction root disappeared before terminal state write.' }
            Assert-KmcNotReparsePoint $transactionRoot 'Working fixture requalification transaction root'
            if (Test-Path -LiteralPath ([string]$finalPlan.statePath)) {
                Assert-KmcRecoveryLeafNoLinks ([string]$finalPlan.statePath) 'Working fixture requalification recovery state'
            }
            [void](Assert-KmcRuntimeLockOwner $lock)
            Assert-KmcNoGameProcesses
            Write-KmcJsonDurable -Path ([string]$finalPlan.statePath) -Value $terminalState
        }
        $verifiedPlan = Get-KmcWorkingFixtureRequalificationRecoveryPlan `
            -SaveRoot $SaveRoot -StateRoot $StateRoot -QualificationPath $QualificationPath -RunId $RunId `
            -ExpectedPriorQualificationSha256 $ExpectedPriorQualificationSha256 `
            -ExpectedBaselineSha256 $ExpectedBaselineSha256 `
            -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 `
            -ExpectedRevisedWorkingSha256 $ExpectedRevisedWorkingSha256 -OwnedLock $lock
        $expectedTerminalAction = if ($recoveryDisposition -ceq 'committed') { 'complete-terminal-committed' } else { 'complete-terminal-prior' }
        if ([string]$verifiedPlan.action -cne $expectedTerminalAction) { throw 'Working fixture recovery terminal state did not revalidate exactly.' }
        if (@($verifiedPlan.atomicDebris).Count -ne 0) { throw 'Working fixture requalification terminal proof retained atomic debris.' }
        Assert-KmcSaveMetadataInventoriesEqual -Before $ownedSaveMetadataBefore -After $verifiedPlan.saveMetadata -Description 'Working fixture requalification terminal save metadata'
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcNoGameProcesses
        Close-KmcRuntimeLock $lock
        $lock = $null
        return [pscustomobject]@{
            schemaVersion = 1; disposition = $recoveryDisposition
            qualificationSha256 = [string]$verifiedPlan.qualificationSha256
            saveMetadataDigest = [string]$verifiedPlan.saveMetadata.digest
            statePath = [string]$verifiedPlan.statePath
        }
    }
    catch {
        $recoveryError = $_.Exception.Message
        if ($null -ne $lock) {
            try { Abandon-KmcRuntimeLock $lock }
            catch { $recoveryError += '; adopted lock abandonment also failed: ' + $_.Exception.Message }
            finally { $lock = $null }
        }
        throw "Working fixture requalification recovery failed and retained the runtime lock: $recoveryError"
    }
    finally {
        if ($null -ne $lock) {
            try { Abandon-KmcRuntimeLock $lock }
            catch { }
            finally { $lock = $null }
        }
    }
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
    $json = $Value | ConvertTo-Json -Depth 30
    Write-KmcBytesDurableAtomic -Path $Path -Bytes ([Text.Encoding]::UTF8.GetBytes($json))
}

function Write-KmcJsonCreateNewDurable {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )
    $fullPath = [IO.Path]::GetFullPath($Path)
    $parent = Split-Path -Parent $fullPath
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { throw "Create-new durable JSON parent is missing: $parent" }
    Assert-KmcNotReparsePoint $parent 'create-new durable JSON parent'
    if (Test-Path -LiteralPath $fullPath) { throw "Create-new durable JSON target already exists: $fullPath" }
    $temporary = Join-Path $parent ('.' + [IO.Path]::GetFileName($fullPath) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes(($Value | ConvertTo-Json -Depth 30))
        $stream = New-Object IO.FileStream($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
        try { $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true) }
        finally { $stream.Dispose() }
        [IO.File]::Move($temporary, $fullPath)
        $target = New-Object IO.FileStream($fullPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::Read)
        try { $target.Flush($true) }
        finally { $target.Dispose() }
    }
    finally {
        if (Test-Path -LiteralPath $temporary -PathType Leaf) { Remove-Item -LiteralPath $temporary -Force }
    }
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
    while (-not [string]::IsNullOrWhiteSpace($current)) {
        if ((Get-Item -LiteralPath $current -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "$Description resolves through a reparse point: $current"
        }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $current, [StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent
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
    $purposeProperty = $Lock.PSObject.Properties['Purpose']
    $expectedProperties = @('schemaVersion','runId','token','ownerProcessId','createdAtUtc')
    if ($null -ne $purposeProperty) { $expectedProperties += 'purpose' }
    Assert-KmcExactProperties $payload $expectedProperties 'runtime lock'
    if ([int]$payload.schemaVersion -ne 1 -or [string]$payload.runId -cne [string]$Lock.RunId -or
        [string]$payload.token -cne [string]$Lock.Token -or [int]$payload.ownerProcessId -ne $PID -or
        ($null -ne $purposeProperty -and [string]$payload.purpose -cne [string]$purposeProperty.Value)) {
        throw 'Runtime lock ownership does not match the current harness process.'
    }
    return $payload
}

function Open-KmcRuntimeLock {
    param(
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$RunId,
        [ValidateSet('fixture-requalification')][string]$Purpose
    )
    $fullState = [IO.Path]::GetFullPath($StateRoot)
    if (-not (Test-Path -LiteralPath $fullState)) { New-Item -ItemType Directory -Path $fullState -Force | Out-Null }
    Assert-KmcNotReparsePoint $fullState 'runtime-state root'
    $lockPath = Join-Path $fullState 'active-transaction.lock'
    if (Test-Path -LiteralPath $lockPath) { throw "A runtime lock already exists and is stale or active: $lockPath" }
    $token = New-KmcRandomToken
    $payload = [ordered]@{ schemaVersion = 1; runId = $RunId; token = $token; ownerProcessId = $PID; createdAtUtc = [DateTime]::UtcNow.ToString('o') }
    if (-not [string]::IsNullOrWhiteSpace($Purpose)) { $payload['purpose'] = $Purpose }
    $stream = New-Object IO.FileStream($lockPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes(($payload | ConvertTo-Json -Compress))
        $stream.Write($bytes, 0, $bytes.Length); $stream.Flush($true)
        $lock = [pscustomobject]@{ Path = $lockPath; Stream = $stream; RunId = $RunId; Token = $token }
        if (-not [string]::IsNullOrWhiteSpace($Purpose)) {
            $lock | Add-Member -NotePropertyName Purpose -NotePropertyValue $Purpose
        }
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
    param(
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [ValidateSet('fixture-requalification')][string]$ExpectedPurpose
    )
    Assert-KmcNoGameProcesses
    $fullStateRoot=[IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    if(-not(Test-Path -LiteralPath $fullStateRoot -PathType Container)){throw 'Runtime-state root is missing during stale lock adoption.'}
    Assert-KmcNotReparsePoint $fullStateRoot 'runtime-state root during stale lock adoption'
    $lockPath=Assert-KmcChildPath (Join-Path $fullStateRoot 'active-transaction.lock') $fullStateRoot 'stale runtime lock'
    Assert-KmcRecoveryLeafNoLinks $lockPath 'stale runtime lock'
    $stream=New-Object IO.FileStream($lockPath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    try{
        Assert-KmcNotReparsePoint $fullStateRoot 'runtime-state root during stale lock adoption'
        Assert-KmcRecoveryLeafNoLinks $lockPath 'opened stale runtime lock'
        $probe=[pscustomobject]@{Path=$lockPath;Stream=$stream;RunId='';Token=''}
        $payload=Read-KmcOpenLockPayload $probe
        $expectedProperties = @('schemaVersion','runId','token','ownerProcessId','createdAtUtc')
        if (-not [string]::IsNullOrWhiteSpace($ExpectedPurpose)) { $expectedProperties += 'purpose' }
        Assert-KmcExactProperties $payload $expectedProperties 'stale runtime lock'
        if([int]$payload.schemaVersion-ne1-or[string]$payload.runId-notmatch'^[A-Za-z0-9._-]{1,120}$'-or[string]$payload.token-notmatch'^[0-9a-f]{64}$'){throw 'Stale runtime lock payload is invalid.'}
        if (-not [string]::IsNullOrWhiteSpace($ExpectedPurpose) -and [string]$payload.purpose -cne $ExpectedPurpose) { throw 'Stale runtime lock purpose is invalid.' }
        if($null-ne(Get-Process -Id ([int]$payload.ownerProcessId) -ErrorAction SilentlyContinue)){throw 'Recorded runtime-lock owner process is still active.'}
        Assert-KmcNoGameProcesses
        Assert-KmcRecoveryLeafNoLinks $lockPath 'opened stale runtime lock'
        $payload.ownerProcessId=$PID
        $bytes=[Text.Encoding]::UTF8.GetBytes(($payload|ConvertTo-Json -Compress));$stream.SetLength(0);$stream.Position=0;$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)
        $lock=[pscustomobject]@{Path=$lockPath;Stream=$stream;RunId=[string]$payload.runId;Token=[string]$payload.token}
        if (-not [string]::IsNullOrWhiteSpace($ExpectedPurpose)) {
            $lock | Add-Member -NotePropertyName Purpose -NotePropertyValue $ExpectedPurpose
        }
        [void](Assert-KmcRuntimeLockOwner $lock)
        Assert-KmcRecoveryLeafNoLinks $lockPath 'adopted runtime lock'
        return $lock
    }catch{$stream.Dispose();throw}
}

function Get-KmcSuspiciousWindows {
    return @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.ProcessName -in @('steam','steamwebhelper','Kingmaker') -and
        $_.MainWindowHandle -ne [IntPtr]::Zero -and
        $_.MainWindowTitle -match '(?i)login|steam guard|cloud conflict|purchase|update required|account|remote play'
    })
}

function Get-KmcOfflineCloudEvidenceDisposition {
    param(
        [AllowNull()][string]$CurrentSessionMessage,
        [AllowNull()][string]$HistoricalMessage,
        [switch]$AllowHistoricalBootstrap
    )
    if (-not [string]::IsNullOrWhiteSpace($CurrentSessionMessage)) {
        if ($CurrentSessionMessage -notmatch 'offlineMode=true') {
            throw 'App 640820 offline-cloud mode is not the final observed cloud state.'
        }
        return 'current-session'
    }
    if (-not $AllowHistoricalBootstrap) {
        throw 'App 640820 offline-cloud mode is not the final observed cloud state.'
    }
    if ([string]::IsNullOrWhiteSpace($HistoricalMessage) -or $HistoricalMessage -notmatch 'offlineMode=true') {
        throw 'Offline-cloud bootstrap lacks a prior exact App 640820 offlineMode=true observation.'
    }
    return 'historical-bootstrap-only'
}

function Assert-KmcSteamSafety {
    param(
        [Parameter(Mandatory = $true)][string]$SteamPath,
        [switch]$AllowMissingCurrentSessionCloudState
    )
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
    $historicalCloud = @(Read-OrderedSessionLines $cloudLog ([DateTime]::MinValue) | Where-Object message -match '\[AppID 640820\]')
    $lastConnectionState = @($connection | Where-Object message -match '\[(Logged On|Logging On|Connected|Logged Off|Logging Off),' | Sort-Object stamp,sequence | Select-Object -Last 1)
    if ($lastConnectionState.Count -ne 1 -or $lastConnectionState[0].message -notmatch '\[(Logged Off|Logging Off),') { throw 'Steam Offline Mode is not the final observed current-session connection state.' }
    $lastCloudState = @($cloud | Where-Object message -match 'offlineMode=(true|false)' | Sort-Object stamp,sequence | Select-Object -Last 1)
    $historicalLastCloudState = @($historicalCloud | Where-Object message -match 'offlineMode=(true|false)' | Sort-Object stamp,sequence | Select-Object -Last 1)
    $cloudEvidenceScope = Get-KmcOfflineCloudEvidenceDisposition `
        -CurrentSessionMessage $(if($lastCloudState.Count -eq 1){[string]$lastCloudState[0].message}else{$null}) `
        -HistoricalMessage $(if($historicalLastCloudState.Count -eq 1){[string]$historicalLastCloudState[0].message}else{$null}) `
        -AllowHistoricalBootstrap:$AllowMissingCurrentSessionCloudState
    $manifestText = Get-Content -Raw -LiteralPath $appManifest
    if ($manifestText -notmatch '"StateFlags"\s+"4"' -or $manifestText -notmatch '"buildid"\s+"6757524"') { throw 'Steam App 640820 is not the qualified fully installed build 6757524.' }
    return [pscustomobject]@{
        processId=$clients[0].Id; processStartedAtUtc=$clients[0].StartTime.ToUniversalTime().ToString('o')
        offlineAtUtc=$lastConnectionState[0].stamp.ToUniversalTime().ToString('o')
        offlineCloudAtUtc=$(if($lastCloudState.Count -eq 1){$lastCloudState[0].stamp.ToUniversalTime().ToString('o')}else{$historicalLastCloudState[0].stamp.ToUniversalTime().ToString('o')})
        offlineCloudEvidenceScope=$cloudEvidenceScope
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
        'player-action-availability', 'mount-dismount-user-flow',
        'mounted-pair-create-and-clear', 'mounted-pair-double-mount-rejected', 'mounted-pair-invalid-pair-rejected',
        'mounted-pair-cleanup-idempotent', 'mounted-pair-death-cleanup', 'mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup', 'mounted-pair-mod-disable-cleanup', 'mounted-pair-open-ground',
        'mounted-pair-stop-start', 'mounted-pair-turns-and-corners', 'mounted-pair-doorway', 'mounted-pair-selection',
        'mounted-pair-party-formation', 'mounted-pair-pause-unpause', 'mounted-pair-destination-cancel',
        'mounted-pair-turn-based-entry-cleanup', 'mounted-pair-realtime-entry-cleanup', 'mounted-pair-save-safety',
        'mounted-pair-load-safety', 'mounted-pair-area-transition-safety', 'fixture-intake', 'lifecycle-suite',
        'native-save-clean-dismount', 'native-area-clean-dismount', 'native-mode-transition-cleanup',
        'presentation-residue-and-uninstall-safety', 'pose-idle', 'pose-walk-run', 'pose-turn-stop',
        'pose-doorway-formation', 'pose-equipment-variants', 'ui-selection-portrait-actionbar',
        'camera-follow-and-command-routing', 'movement-suite', 'boundary-suite', 'presentation-suite',
        'mounted-rider-melee-hit-rt',
        'manual-visual-review'
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
    if ([string]$Row -cin @('player-action-availability','mount-dismount-user-flow')) {
        return 'player-action-controller-direct'
    }
    return 'relationship-service-direct'
}

function Get-KmcLifecycleClaimLimit {
    param([Parameter(Mandatory = $true)][string]$Row)
    if ([string]$Row -cin @('player-action-availability','mount-dismount-user-flow')) {
        return 'Runtime player-action controller invocation; Unity OnGUI button delivery remains separately observed.'
    }
    return 'Direct service/handler invocation only; native EventBus/UMM delivery was not exercised.'
}

function Get-KmcPlayerActionRuntimeRows {
    return @('player-action-availability','mount-dismount-user-flow')
}

function Test-KmcLifecycleRuntimeScenario {
    param([AllowNull()][string]$Scenario)
    return [string]$Scenario -ceq 'lifecycle-suite' -or
        @(Get-KmcLifecycleRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1 -or
        @(Get-KmcPlayerActionRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1
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
    return [string]$Scenario -ceq 'movement-suite' -or [string]$Scenario -ceq 'presentation-suite' -or
        @(Get-KmcMovementRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1 -or
        @(Get-KmcPresentationRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1
}

function Get-KmcPresentationRuntimeRows {
    return @(
        'pose-doorway-formation',
        'pose-idle',
        'pose-walk-run',
        'pose-turn-stop',
        'pose-equipment-variants',
        'ui-selection-portrait-actionbar',
        'camera-follow-and-command-routing'
    )
}

function Get-KmcBoundaryRuntimeRows {
    return @(
        'mounted-pair-turn-based-entry-cleanup',
        'mounted-pair-realtime-entry-cleanup',
        'mounted-pair-save-safety',
        'mounted-pair-load-safety',
        'mounted-pair-area-transition-safety'
    )
}

function Get-KmcNativeLifecycleBoundaryRuntimeRows {
    return @(
        'native-save-clean-dismount',
        'native-area-clean-dismount',
        'native-mode-transition-cleanup',
        'presentation-residue-and-uninstall-safety'
    )
}

function Test-KmcBoundaryRuntimeScenario {
    param([AllowNull()][string]$Scenario)
    return [string]$Scenario -ceq 'boundary-suite' -or
        @(Get-KmcBoundaryRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1 -or
        @(Get-KmcNativeLifecycleBoundaryRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1
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
    $expectedClaimLimit = Get-KmcLifecycleClaimLimit ([string]$Record.row)
    if ($Record.triggerScope.expectedCleanupTrigger -isnot [string] -or [string]$Record.triggerScope.expectedCleanupTrigger -cne $expectedTrigger -or
        $Record.triggerScope.invocationPath -isnot [string] -or [string]$Record.triggerScope.invocationPath -cne $expectedInvocationPath -or
        $Record.triggerScope.nativeDeliveryObserved -isnot [bool] -or $Record.triggerScope.nativeDeliveryObserved -ne $false -or
        $Record.triggerScope.claimLimit -isnot [string] -or
        [string]$Record.triggerScope.claimLimit -cne $expectedClaimLimit) {
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
        [string[]]$expectedPhases = if ($row -cin @('mounted-pair-invalid-pair-rejected','player-action-availability')) {
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
        $mounted = if ($row -cin @('mounted-pair-invalid-pair-rejected','player-action-availability')) { $null } else { $rowRecords[1] }
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
    if ($finalRow -cin @('mounted-pair-invalid-pair-rejected','player-action-availability')) {
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
    foreach ($leaf in @('lifecycle-scenario-evidence.jsonl','movement-telemetry.jsonl','movement-scenario-evidence.jsonl','boundary-scenario-evidence.jsonl','combat-scenario-evidence.jsonl')) {
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
            if ($row -cnotin @('mounted-pair-invalid-pair-rejected','player-action-availability') -and
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

function Get-KmcBoundaryExpectedCleanupTrigger {
    param([Parameter(Mandatory = $true)][string]$Row)
    switch -CaseSensitive ($Row) {
        'mounted-pair-turn-based-entry-cleanup' { return 'TurnBasedModeChanged' }
        'mounted-pair-realtime-entry-cleanup' { return 'RealtimeModeChanged' }
        'mounted-pair-save-safety' { return 'SaveRequested' }
        'mounted-pair-load-safety' { return 'LoadRequested' }
        'mounted-pair-area-transition-safety' { return 'AreaUnloading' }
        'native-save-clean-dismount' { return 'SaveRequested' }
        'native-area-clean-dismount' { return 'AreaUnloading' }
        'native-mode-transition-cleanup' { return $null }
        'presentation-residue-and-uninstall-safety' { return 'ModDisabled' }
        default { throw "No exact boundary cleanup-trigger contract exists for $Row." }
    }
}

function Get-KmcBoundaryInvocationPath {
    param([Parameter(Mandatory = $true)][string]$Row)
    switch -CaseSensitive ($Row) {
        'mounted-pair-turn-based-entry-cleanup' { return 'mounted-lifecycle-handler-direct' }
        'mounted-pair-realtime-entry-cleanup' { return 'mounted-lifecycle-handler-direct' }
        'mounted-pair-save-safety' { return 'relationship-guard-boundary-direct' }
        'mounted-pair-load-safety' { return 'game-loadgame-exact-working' }
        'mounted-pair-area-transition-safety' { return 'lifecycle-area-precleanup-direct+game-reloadarea' }
        'native-save-clean-dismount' { return 'game-savegame-exact-working+one-shot-prefix-suppression' }
        'native-area-clean-dismount' { return 'game-reloadarea+native-eventbus-area-stages' }
        'native-mode-transition-cleanup' { return 'settings-oninvokeupdatecallback+gamesettingscontroller-eventbus' }
        'presentation-residue-and-uninstall-safety' { return 'registered-umm-ontoggle-delegate(false)+registered-umm-ontoggle-delegate(true)' }
        default { throw "No exact boundary invocation-path contract exists for $Row." }
    }
}

function Get-KmcBoundaryClaimLimit {
    param([Parameter(Mandatory = $true)][string]$Row)
    switch -CaseSensitive ($Row) {
        'mounted-pair-turn-based-entry-cleanup' { return 'Direct HandleTurnBasedModeStateChanged(true) invocation only; native mode-event delivery was not exercised.' }
        'mounted-pair-realtime-entry-cleanup' { return 'Direct HandleTurnBasedModeStateChanged(false) invocation only; native mode-event delivery was not exercised.' }
        'mounted-pair-save-safety' { return 'Direct GuardBoundary(SaveRequested) service invocation only; stock SaveRoutine and serialization were not exercised.' }
        'mounted-pair-load-safety' { return 'Real Game.LoadGame of the exact Working descriptor exercised the native LoadRoutine prefix; no UI load request was exercised.' }
        'mounted-pair-area-transition-safety' { return 'Direct OnAreaBeginUnloading cleanup was latched before real Game.ReloadArea; native area-event delivery was not independently observed or qualified.' }
        'native-save-clean-dismount' { return 'Real Game.SaveGame entered the exact SaveRoutine Harmony12 prefix and callback pipeline; a one-shot exact-Working diagnostic guard suppressed the stock iterator body before serialization, so no disk write or save UI delivery is claimed.' }
        'native-area-clean-dismount' { return 'Real Game.ReloadArea exercised native EventBus area-unload and loading-stage delivery in the exact Working fixture; no cross-area destination transition or UI command was exercised.' }
        'native-mode-transition-cleanup' { return 'Diagnostic-only in-memory SettingsEntityBool cache substitution invoked the exact registered GameSettingsController callback and EventBus path, then restored it; no SettingsProvider/PlayerPrefs write or settings-UI click is claimed.' }
        'presentation-residue-and-uninstall-safety' { return 'The exact registered Unity Mod Manager OnToggle delegate was invoked diagnostically for disable and re-enable; a user click in the UMM manager and physical file deletion were not exercised.' }
        default { throw "No exact boundary claim-limit contract exists for $Row." }
    }
}

function Get-KmcBoundaryExpectedPhases {
    param([Parameter(Mandatory = $true)][string]$Row)
    if ([string]$Row -cin @('mounted-pair-load-safety','mounted-pair-area-transition-safety','native-area-clean-dismount')) {
        return @('row-start','mounted','pre-boundary','cleanup-latch','loading-start','loading-stop','fresh-world','row-result')
    }
    return @('row-start','mounted','pre-boundary','cleanup-latch','post-boundary','row-result')
}

function Test-KmcBoundaryObservedIdentityEqual {
    param($Left, $Right)
    return $null -ne $Left -and $null -ne $Right -and
        [long]$Left.observedLength -eq [long]$Right.observedLength -and
        [long]$Left.observedLastWriteTimeUtcTicks -eq [long]$Right.observedLastWriteTimeUtcTicks -and
        [string]$Left.observedSha256 -ceq [string]$Right.observedSha256
}

function Test-KmcBoundaryObservedEqualsPostInitial {
    param($Value)
    return $null -ne $Value -and
        [long]$Value.observedLength -eq [long]$Value.postInitialLoadLength -and
        [long]$Value.observedLastWriteTimeUtcTicks -eq [long]$Value.postInitialLoadLastWriteTimeUtcTicks -and
        [string]$Value.observedSha256 -ceq [string]$Value.postInitialLoadSha256
}

function Test-KmcBoundaryStringArrayOrdinalEqual {
    param($Left, $Right)
    [object[]]$leftItems = @($Left)
    [object[]]$rightItems = @($Right)
    if ($leftItems.Count -ne $rightItems.Count) { return $false }
    for ($index = 0; $index -lt $leftItems.Count; $index++) {
        if ([string]$leftItems[$index] -cne [string]$rightItems[$index]) { return $false }
    }
    return $true
}

function Assert-KmcBoundaryRelationshipEvidence {
    param($Value, [Parameter(Mandatory = $true)][string]$Description)
    if ($null -eq $Value) { throw "$Description is required." }
    Assert-KmcExactProperties $Value @(
        'state','riderUniqueId','mountUniqueId','ownerReferencesPresent','movementAgentPresent',
        'riderStockAgentEnabled','mountStockAgentEnabled','riderAvoidanceDisabled','mountAvoidanceDisabled',
        'riderOverridePresent','mountOverridePresent','riderForbidRotation','mountForbidRotation',
        'riderMoveCommandPresent','mountMoveCommandPresent','riderMovementAgentComponentCount',
        'mountMovementAgentComponentCount','kmcRiderMovementAgentComponentCount','attachmentLeaseActive','attachmentRestoreVerified','attachmentResidue',
        'riderParentMatchesAttachment','attachmentParent','sourceAnchor','kmcAnchorObjectCount','selectedUnitIds') $Description
    if ($Value.state -isnot [string] -or [string]$Value.state -cnotin @('Unmounted','Validating','Mounting','Mounted','Dismounting','Faulted','Disposed')) {
        throw "$Description.state is invalid."
    }
    foreach ($name in @('riderUniqueId','mountUniqueId','attachmentParent','sourceAnchor')) {
        if ($null -ne $Value.$name -and $Value.$name -isnot [string]) { throw "$Description.$name must be a JSON string or null." }
    }
    foreach ($name in @('ownerReferencesPresent','movementAgentPresent','attachmentLeaseActive','attachmentRestoreVerified',
        'attachmentResidue','riderParentMatchesAttachment')) {
        if ($Value.$name -isnot [bool]) { throw "$Description.$name must be a JSON boolean." }
    }
    foreach ($name in @('riderStockAgentEnabled','mountStockAgentEnabled','riderAvoidanceDisabled','mountAvoidanceDisabled',
        'riderOverridePresent','mountOverridePresent','riderForbidRotation','mountForbidRotation',
        'riderMoveCommandPresent','mountMoveCommandPresent')) {
        Assert-KmcNullableJsonBoolean $Value.$name "$Description.$name"
    }
    foreach ($name in @('riderMovementAgentComponentCount','mountMovementAgentComponentCount')) {
        if ($null -ne $Value.$name -and (-not (Test-KmcExactJsonInteger $Value.$name) -or [long]$Value.$name -lt 0L)) {
            throw "$Description.$name must be a nonnegative exact JSON integer or null."
        }
    }
    if (-not (Test-KmcExactJsonInteger $Value.kmcRiderMovementAgentComponentCount) -or
        [long]$Value.kmcRiderMovementAgentComponentCount -lt 0L) {
        throw "$Description.kmcRiderMovementAgentComponentCount must be a nonnegative exact JSON integer."
    }
    if (-not (Test-KmcExactJsonInteger $Value.kmcAnchorObjectCount) -or [long]$Value.kmcAnchorObjectCount -lt 0L) {
        throw "$Description.kmcAnchorObjectCount must be a nonnegative exact JSON integer."
    }
    Assert-KmcJsonStringArray $Value.selectedUnitIds "$Description.selectedUnitIds"
    if (($null -eq $Value.riderUniqueId) -ne ($null -eq $Value.mountUniqueId)) {
        throw "$Description must expose both pair IDs or neither."
    }
    if ($null -ne $Value.riderUniqueId) {
        if ([string]::IsNullOrWhiteSpace([string]$Value.riderUniqueId) -or
            [string]::IsNullOrWhiteSpace([string]$Value.mountUniqueId) -or
            [string]$Value.riderUniqueId -ceq [string]$Value.mountUniqueId) {
            throw "$Description rider and mount identities must be nonempty and distinct."
        }
    }
}

function Assert-KmcBoundaryMountedRelationship {
    param($Value, [Parameter(Mandatory = $true)][string]$Description)
    if ([string]$Value.state -cne 'Mounted' -or $null -eq $Value.riderUniqueId -or
        $Value.ownerReferencesPresent -ne $true -or $Value.movementAgentPresent -ne $true -or
        $Value.riderStockAgentEnabled -ne $false -or $Value.mountStockAgentEnabled -ne $true -or
        $Value.riderAvoidanceDisabled -ne $true -or $Value.mountAvoidanceDisabled -ne $false -or
        $Value.riderOverridePresent -ne $true -or $Value.mountOverridePresent -ne $false -or
        $Value.riderForbidRotation -ne $true -or $Value.mountForbidRotation -ne $false -or
        $Value.riderMoveCommandPresent -ne $false -or $Value.mountMoveCommandPresent -ne $false -or
        -not (Test-KmcExactJsonInteger $Value.riderMovementAgentComponentCount) -or
        -not (Test-KmcExactJsonInteger $Value.mountMovementAgentComponentCount) -or
        [long]$Value.riderMovementAgentComponentCount -ne 1L -or
        [long]$Value.mountMovementAgentComponentCount -ne 0L -or
        [long]$Value.kmcRiderMovementAgentComponentCount -ne 1L -or
        $Value.attachmentLeaseActive -ne $true -or $Value.attachmentRestoreVerified -ne $false -or
        $Value.attachmentResidue -ne $true -or $Value.riderParentMatchesAttachment -ne $true -or
        [string]$Value.attachmentParent -cne 'KMC_RiderPositionAnchor' -or [string]$Value.sourceAnchor -cne 'Spine' -or
        [long]$Value.kmcAnchorObjectCount -ne 1L) {
        throw "$Description does not prove the exact mounted authority and attachment lease."
    }
}

function Assert-KmcBoundaryCleanRelationship {
    param(
        $Value,
        [Parameter(Mandatory = $true)][string]$Description,
        [switch]$RequireRestore,
        [switch]$AllowDeferredAnchor
    )
    $anchorStateClean = if ($AllowDeferredAnchor) {
        [long]$Value.kmcAnchorObjectCount -le 1L
    } else {
        [long]$Value.kmcAnchorObjectCount -eq 0L
    }
    $componentStateClean = if ($AllowDeferredAnchor) {
        [long]$Value.kmcRiderMovementAgentComponentCount -le 1L
    } else {
        [long]$Value.kmcRiderMovementAgentComponentCount -eq 0L
    }
    $riderComponentStateClean = if ($AllowDeferredAnchor) {
        [long]$Value.riderMovementAgentComponentCount -le 1L
    } else {
        [long]$Value.riderMovementAgentComponentCount -eq 0L
    }
    if ([string]$Value.state -cne 'Unmounted' -or $null -eq $Value.riderUniqueId -or
        $Value.ownerReferencesPresent -ne $false -or $Value.movementAgentPresent -ne $false -or
        $Value.riderStockAgentEnabled -ne $true -or $Value.mountStockAgentEnabled -ne $true -or
        $Value.riderAvoidanceDisabled -ne $false -or $Value.mountAvoidanceDisabled -ne $false -or
        $Value.riderOverridePresent -ne $false -or $Value.mountOverridePresent -ne $false -or
        $Value.riderForbidRotation -ne $false -or $Value.mountForbidRotation -ne $false -or
        $Value.riderMoveCommandPresent -ne $false -or $Value.mountMoveCommandPresent -ne $false -or
        -not (Test-KmcExactJsonInteger $Value.riderMovementAgentComponentCount) -or
        -not (Test-KmcExactJsonInteger $Value.mountMovementAgentComponentCount) -or
        -not $riderComponentStateClean -or
        [long]$Value.mountMovementAgentComponentCount -ne 0L -or
        -not $componentStateClean -or
        $Value.attachmentLeaseActive -ne $false -or $Value.attachmentResidue -ne $false -or
        $Value.riderParentMatchesAttachment -ne $false -or $null -ne $Value.attachmentParent -or
        $null -ne $Value.sourceAnchor -or -not $anchorStateClean) {
        throw "$Description does not prove exact residue-free Unmounted relationship state."
    }
    if ($RequireRestore -and $Value.attachmentRestoreVerified -ne $true) {
        throw "$Description does not prove the scoped attachment lease was restored."
    }
}

function Assert-KmcBoundaryRowStartRelationship {
    param($Value, [Parameter(Mandatory = $true)][string]$Description)
    if ([string]$Value.state -cne 'Unmounted' -or $null -eq $Value.riderUniqueId -or $null -eq $Value.mountUniqueId -or
        $Value.ownerReferencesPresent -ne $false -or $Value.movementAgentPresent -ne $false -or
        $Value.riderStockAgentEnabled -ne $true -or $Value.mountStockAgentEnabled -ne $true -or
        $Value.riderAvoidanceDisabled -ne $false -or $Value.mountAvoidanceDisabled -ne $false -or
        $Value.riderOverridePresent -ne $false -or $Value.mountOverridePresent -ne $false -or
        $Value.riderForbidRotation -ne $false -or $Value.mountForbidRotation -ne $false -or
        $Value.riderMoveCommandPresent -ne $false -or $Value.mountMoveCommandPresent -ne $false -or
        -not (Test-KmcExactJsonInteger $Value.riderMovementAgentComponentCount) -or
        -not (Test-KmcExactJsonInteger $Value.mountMovementAgentComponentCount) -or
        [long]$Value.riderMovementAgentComponentCount -ne 0L -or
        [long]$Value.mountMovementAgentComponentCount -ne 0L -or
        [long]$Value.kmcRiderMovementAgentComponentCount -ne 0L -or
        $Value.attachmentLeaseActive -ne $false -or $Value.attachmentResidue -ne $false -or
        $Value.riderParentMatchesAttachment -ne $false -or $null -ne $Value.attachmentParent -or
        $null -ne $Value.sourceAnchor -or [long]$Value.kmcAnchorObjectCount -ne 0L) {
        throw "$Description does not prove the exact resolved-pair, unowned, residue-free row-start state."
    }
}

function Assert-KmcBoundaryLoadingRelationship {
    param($Value, [Parameter(Mandatory = $true)][string]$Description)
    $nullablePairFields = @('riderStockAgentEnabled','mountStockAgentEnabled','riderAvoidanceDisabled','mountAvoidanceDisabled',
        'riderOverridePresent','mountOverridePresent','riderForbidRotation','mountForbidRotation',
        'riderMoveCommandPresent','mountMoveCommandPresent','riderMovementAgentComponentCount','mountMovementAgentComponentCount')
    if ([string]$Value.state -cne 'Unmounted' -or $null -ne $Value.riderUniqueId -or $null -ne $Value.mountUniqueId -or
        $Value.ownerReferencesPresent -ne $false -or $Value.movementAgentPresent -ne $false -or
        @($nullablePairFields | Where-Object { $null -ne $Value.$_ }).Count -ne 0 -or
        [long]$Value.kmcRiderMovementAgentComponentCount -ne 0L -or
        $Value.attachmentLeaseActive -ne $false -or $Value.attachmentRestoreVerified -ne $true -or
        $Value.attachmentResidue -ne $false -or $Value.riderParentMatchesAttachment -ne $false -or
        $null -ne $Value.attachmentParent -or $null -ne $Value.sourceAnchor -or
        [long]$Value.kmcAnchorObjectCount -ne 0L -or @($Value.selectedUnitIds).Count -ne 0) {
        throw "$Description does not prove exact detached-old-world cleanup during loading."
    }
}

function Assert-KmcBoundaryTriggerScope {
    param(
        $Value,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][string]$Phase
    )
    if ($null -eq $Value) { throw 'Boundary triggerScope is required.' }
    Assert-KmcExactProperties $Value @(
        'expectedCleanupTrigger','invocationPath','nativeDeliveryObserved','stockSaveRoutineInvoked',
        'realWorkingSaveDispatched','realWorkingLoadDispatched','realAreaReloadDispatched','claimLimit') 'boundary triggerScope'
    foreach ($name in @('expectedCleanupTrigger','invocationPath','claimLimit')) {
        if ($Value.$name -isnot [string]) { throw "Boundary triggerScope.$name must be a JSON string." }
    }
    foreach ($name in @('nativeDeliveryObserved','stockSaveRoutineInvoked','realWorkingSaveDispatched','realWorkingLoadDispatched','realAreaReloadDispatched')) {
        if ($Value.$name -isnot [bool]) { throw "Boundary triggerScope.$name must be a JSON boolean." }
    }
    $expectedCleanup = Get-KmcBoundaryExpectedCleanupTrigger $Row
    if (($Row -ceq 'native-mode-transition-cleanup' -and
            [string]$Value.expectedCleanupTrigger -cnotin @('TurnBasedModeChanged','RealtimeModeChanged')) -or
        ($Row -cne 'native-mode-transition-cleanup' -and [string]$Value.expectedCleanupTrigger -cne $expectedCleanup) -or
        [string]$Value.invocationPath -cne (Get-KmcBoundaryInvocationPath $Row) -or
        [string]$Value.claimLimit -cne (Get-KmcBoundaryClaimLimit $Row)) {
        throw "Boundary trigger scope or claim limit is not exact for $Row."
    }
    if ($Value.stockSaveRoutineInvoked -ne $Value.realWorkingSaveDispatched -or
        ($Row -cne 'native-save-clean-dismount' -and $Value.realWorkingSaveDispatched)) {
        throw 'Boundary SaveRoutine/pipeline dispatch claims are not exact.'
    }
    if ($Row -cne 'mounted-pair-load-safety' -and $Value.realWorkingLoadDispatched -ne $false) {
        throw "Boundary evidence makes a false native load-delivery claim for $Row/$Phase."
    }
    if ($Row -cnotin @('mounted-pair-area-transition-safety','native-area-clean-dismount') -and $Value.realAreaReloadDispatched -ne $false) {
        throw "Boundary evidence makes a false real-area-reload claim for $Row/$Phase."
    }
    if ($Row -cnotin @('mounted-pair-load-safety','native-save-clean-dismount','native-area-clean-dismount',
            'native-mode-transition-cleanup','presentation-residue-and-uninstall-safety') -and
        $Value.nativeDeliveryObserved -ne $false) {
        throw "Boundary evidence makes a false native-delivery claim for $Row/$Phase."
    }
}

function Get-KmcCombatRuntimeRows {
    return @('mounted-rider-melee-hit-rt')
}

function Test-KmcCombatRuntimeScenario {
    param([AllowNull()][string]$Scenario)
    return @(Get-KmcCombatRuntimeRows | Where-Object { $_ -ceq [string]$Scenario }).Count -eq 1
}

function Assert-KmcBoundaryWorkingIdentity {
    param(
        $Value,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][string]$Phase
    )
    if ($null -eq $Value) { throw 'Boundary workingIdentity is required.' }
    Assert-KmcExactProperties $Value @(
        'internalName','fileName','path','gameId','gameName','area','requestLength','requestLastWriteTimeUtcTicks',
        'requestSha256','postInitialLoadLength','postInitialLoadLastWriteTimeUtcTicks','postInitialLoadSha256',
        'preDispatchLength','preDispatchLastWriteTimeUtcTicks','preDispatchSha256','observedLength',
        'observedLastWriteTimeUtcTicks','observedSha256','observedSource','matchesPostInitialLoad','descriptorVerified',
        'descriptorInternalName','descriptorFileName','descriptorPath','descriptorGameId','descriptorGameName','descriptorArea',
        'descriptorSaveType','descriptorCompatibilityVersion') 'boundary workingIdentity'
    foreach ($name in @('internalName','fileName','path','gameId','gameName','area','requestSha256','postInitialLoadSha256')) {
        if ($Value.$name -isnot [string]) { throw "Boundary workingIdentity.$name must be a JSON string." }
    }
    foreach ($name in @('requestLength','requestLastWriteTimeUtcTicks','postInitialLoadLength','postInitialLoadLastWriteTimeUtcTicks')) {
        if (-not (Test-KmcExactJsonInteger $Value.$name) -or [long]$Value.$name -le 0L) {
            throw "Boundary workingIdentity.$name must be a positive exact JSON integer."
        }
    }
    if ([string]$Value.requestSha256 -cnotmatch '^[0-9a-f]{64}$' -or [string]$Value.postInitialLoadSha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Boundary Working request/post-initial-load SHA-256 must be exact lowercase hexadecimal.'
    }
    $expected = $Request.fixture.working
    foreach ($name in @('internalName','fileName','gameId','gameName','area')) {
        if ([string]$Value.$name -cne [string]$expected.$name) { throw "Boundary Working identity mismatch: $name" }
    }
    if ([long]$Value.requestLength -ne [long]$expected.length -or
        [long]$Value.requestLastWriteTimeUtcTicks -ne [long]$expected.lastWriteTimeUtcTicks -or
        [string]$Value.requestSha256 -cne [string]$expected.sha256) {
        throw 'Boundary Working request file identity does not match the qualified request.'
    }
    if (-not [IO.Path]::IsPathRooted([string]$Value.path) -or
        [IO.Path]::GetFileName([string]$Value.path) -cne [string]$expected.fileName -or
        [IO.Path]::GetFullPath([string]$Value.path) -cne [string]$Value.path) {
        throw 'Boundary Working path is not an exact absolute path ending in the canonical Working leaf.'
    }
    foreach ($name in @('observedLength','observedLastWriteTimeUtcTicks')) {
        if (-not (Test-KmcExactJsonInteger $Value.$name) -or [long]$Value.$name -le 0L) {
            throw "Boundary workingIdentity.$name must be a positive exact JSON integer."
        }
    }
    if ($Value.observedSha256 -isnot [string] -or [string]$Value.observedSha256 -cnotmatch '^[0-9a-f]{64}$' -or
        $Value.observedSource -isnot [string] -or $Value.matchesPostInitialLoad -isnot [bool]) {
        throw 'Boundary observed Working file identity has invalid primitive types.'
    }
    foreach ($name in @('preDispatchLength','preDispatchLastWriteTimeUtcTicks')) {
        if ($null -ne $Value.$name -and (-not (Test-KmcExactJsonInteger $Value.$name) -or [long]$Value.$name -le 0L)) {
            throw "Boundary workingIdentity.$name must be a positive exact JSON integer or null."
        }
    }
    if ($null -ne $Value.preDispatchSha256 -and
        ($Value.preDispatchSha256 -isnot [string] -or [string]$Value.preDispatchSha256 -cnotmatch '^[0-9a-f]{64}$')) {
        throw 'Boundary workingIdentity.preDispatchSha256 must be an exact lowercase SHA-256 or null.'
    }
    if (($null -eq $Value.preDispatchLength) -ne ($null -eq $Value.preDispatchLastWriteTimeUtcTicks) -or
        ($null -eq $Value.preDispatchLength) -ne ($null -eq $Value.preDispatchSha256)) {
        throw 'Boundary Working pre-dispatch identity must be wholly present or wholly null.'
    }
    $expectedSource = if ($Row -ceq 'mounted-pair-load-safety' -and
        [string]$Phase -cin @('cleanup-latch','loading-start')) {
        'cached-immediate-pre-dispatch'
    }
    elseif ($Row -cin @('mounted-pair-area-transition-safety','native-area-clean-dismount') -and [string]$Phase -ceq 'loading-start') {
        'cached-row-start'
    }
    else {
        switch -CaseSensitive ($Phase) {
            'pre-boundary' { 'immediate-pre-dispatch'; break }
            'cleanup-latch' { 'immediate-post-dispatch'; break }
            default { $Phase; break }
        }
    }
    if ([string]$Value.observedSource -cne $expectedSource) {
        throw "Boundary Working observedSource is not exact for $Row/${Phase}: observed '$($Value.observedSource)', expected '$expectedSource'."
    }
    $matches = [long]$Value.observedLength -eq [long]$Value.postInitialLoadLength -and
        [long]$Value.observedLastWriteTimeUtcTicks -eq [long]$Value.postInitialLoadLastWriteTimeUtcTicks -and
        [string]$Value.observedSha256 -ceq [string]$Value.postInitialLoadSha256
    if ($Value.matchesPostInitialLoad -ne $matches) { throw 'Boundary Working matchesPostInitialLoad does not equal its raw length/time/hash comparison.' }
    Assert-KmcNullableJsonBoolean $Value.descriptorVerified 'boundary workingIdentity.descriptorVerified'
    $descriptorStringNames = @('descriptorInternalName','descriptorFileName','descriptorPath','descriptorGameId','descriptorGameName',
        'descriptorArea','descriptorSaveType')
    foreach ($name in $descriptorStringNames) {
        if ($null -ne $Value.$name -and $Value.$name -isnot [string]) { throw "Boundary workingIdentity.$name must be a JSON string or null." }
    }
    if ($null -ne $Value.descriptorCompatibilityVersion -and -not (Test-KmcExactJsonInteger $Value.descriptorCompatibilityVersion)) {
        throw 'Boundary workingIdentity.descriptorCompatibilityVersion must be an exact JSON integer or null.'
    }
    if ($null -eq $Value.descriptorVerified) {
        if (@($descriptorStringNames | Where-Object { $null -ne $Value.$_ }).Count -ne 0 -or
            $null -ne $Value.descriptorCompatibilityVersion) {
            throw 'Boundary unverified descriptor state contains descriptor identity fields.'
        }
    }
    elseif ($Value.descriptorVerified -ne $true -or
        [string]$Value.descriptorInternalName -cne [string]$expected.internalName -or
        [string]$Value.descriptorFileName -cne [string]$expected.fileName -or
        [string]$Value.descriptorGameId -cne [string]$expected.gameId -or
        [string]$Value.descriptorGameName -cne [string]$expected.gameName -or
        [string]$Value.descriptorArea -cne [string]$expected.area -or
        [string]$Value.descriptorSaveType -cne 'Manual' -or
        -not (Test-KmcExactJsonInteger $Value.descriptorCompatibilityVersion) -or
        [long]$Value.descriptorCompatibilityVersion -ne 1L -or
        -not [IO.Path]::IsPathRooted([string]$Value.descriptorPath) -or
        [IO.Path]::GetFullPath([string]$Value.descriptorPath) -ine [IO.Path]::GetFullPath([string]$Value.path)) {
        throw 'Boundary verified descriptor does not match exact Working name/path/campaign/Manual/compatibility identity.'
    }
}

function Assert-KmcBoundaryAuthorizationEvidence {
    param($Value)
    if ($null -eq $Value) { throw 'Boundary authorization evidence is required.' }
    $names = @(
        'authorizedLoadsBefore','authorizedLoadsAfter','authorizedLoadsDelta',
        'authorizedWritesBefore','authorizedWritesAfter','authorizedWritesDelta',
        'unauthorizedLoadsBefore','unauthorizedLoadsAfter','unauthorizedLoadsDelta',
        'unauthorizedWritesBefore','unauthorizedWritesAfter','unauthorizedWritesDelta',
        'baselineLoadsBefore','baselineLoadsAfter','baselineLoadsDelta',
        'fatalViolationsBefore','fatalViolationsAfter','fatalViolationsDelta',
        'suppressedWorkingWritesBefore','suppressedWorkingWritesAfter','suppressedWorkingWritesDelta',
        'oneShotWorkingWriteSuppressionArmed')
    Assert-KmcExactProperties $Value $names 'boundary authorization'
    foreach ($name in @($names | Where-Object { $_ -cne 'oneShotWorkingWriteSuppressionArmed' })) {
        if (-not (Test-KmcExactJsonInteger $Value.$name) -or [long]$Value.$name -lt 0L) {
            throw "Boundary authorization.$name must be a nonnegative exact JSON integer."
        }
    }
    if ($Value.oneShotWorkingWriteSuppressionArmed -isnot [bool]) {
        throw 'Boundary authorization.oneShotWorkingWriteSuppressionArmed must be a JSON boolean.'
    }
    foreach ($prefix in @('authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','baselineLoads','fatalViolations','suppressedWorkingWrites')) {
        $before = [long]$Value.($prefix + 'Before')
        $after = [long]$Value.($prefix + 'After')
        $delta = [long]$Value.($prefix + 'Delta')
        if ($after -lt $before -or $delta -ne ($after - $before)) {
            throw "Boundary authorization $prefix counters do not reconcile exactly."
        }
    }
}

function Assert-KmcBoundaryLoadingEvidence {
    param($Value, [AllowNull()][string]$Row)
    if ($null -eq $Value) { throw 'Boundary loading evidence is required.' }
    Assert-KmcExactProperties $Value @('observed','startObserved','stopObserved','callbackObserved') 'boundary loading'
    foreach ($name in @('observed','startObserved','stopObserved','callbackObserved')) {
        if ($Value.$name -isnot [bool]) { throw "Boundary loading.$name must be a JSON boolean." }
    }
    if ($Value.stopObserved -and -not $Value.startObserved) { throw 'Boundary loading stop was reported without a start.' }
    if ($Value.callbackObserved -and -not $Value.startObserved -and $Row -cne 'native-save-clean-dismount') {
        throw 'Boundary loading callback was reported without a start.'
    }
}

function Assert-KmcBoundaryCleanupEvidence {
    param(
        $Value,
        [Parameter(Mandatory = $true)][string]$ExpectedTrigger
    )
    if ($null -eq $Value) { throw 'Boundary cleanup evidence is required.' }
    Assert-KmcExactProperties $Value @(
        'captured','captureFrame','expectedTrigger','actualTrigger','transitionSucceeded','movementAuthorityResidual',
        'presentationResidual','relationshipUnmounted','ownerReferencesReleased','movementAgentReleased',
        'stockAgentsRestored','avoidanceRestored','overridesRestored','riderMovementAgentComponentsRestored',
        'forbidRotationRestored','attachmentRestored','selectionRestored','moveCommandsRestored','kmcAnchorObjectsAbsent',
        'allRestored') 'boundary cleanup'
    if ($Value.captured -isnot [bool]) { throw 'Boundary cleanup.captured must be a JSON boolean.' }
    if ($null -ne $Value.captureFrame -and (-not (Test-KmcExactJsonInteger $Value.captureFrame) -or [long]$Value.captureFrame -lt 0L)) {
        throw 'Boundary cleanup.captureFrame must be a nonnegative exact JSON integer or null.'
    }
    foreach ($name in @('expectedTrigger','actualTrigger')) {
        if ($null -ne $Value.$name -and $Value.$name -isnot [string]) { throw "Boundary cleanup.$name must be a JSON string or null." }
    }
    $booleanNames = @('transitionSucceeded','movementAuthorityResidual','presentationResidual','relationshipUnmounted',
        'ownerReferencesReleased','movementAgentReleased','stockAgentsRestored','avoidanceRestored','overridesRestored',
        'riderMovementAgentComponentsRestored','forbidRotationRestored','attachmentRestored','selectionRestored',
        'moveCommandsRestored','kmcAnchorObjectsAbsent','allRestored')
    foreach ($name in $booleanNames) { Assert-KmcNullableJsonBoolean $Value.$name "boundary cleanup.$name" }
    if (-not $Value.captured) {
        if ($null -ne $Value.captureFrame -or $null -ne $Value.actualTrigger -or
            @($booleanNames | Where-Object { $null -ne $Value.$_ }).Count -ne 0 -or
            ($null -ne $Value.expectedTrigger -and [string]$Value.expectedTrigger -cne $ExpectedTrigger)) {
            throw 'Boundary uncaptured cleanup contains post-cleanup state.'
        }
        return
    }
    if ($null -eq $Value.captureFrame -or [string]$Value.expectedTrigger -cne $ExpectedTrigger -or
        [string]$Value.actualTrigger -cne $ExpectedTrigger -or
        @($booleanNames | Where-Object { $Value.$_ -isnot [bool] }).Count -ne 0) {
        throw 'Boundary captured cleanup lacks exact trigger, frame, or boolean state.'
    }
    $derived = $Value.transitionSucceeded -and -not $Value.movementAuthorityResidual -and
        -not $Value.presentationResidual -and $Value.relationshipUnmounted -and $Value.ownerReferencesReleased -and
        $Value.movementAgentReleased -and $Value.stockAgentsRestored -and $Value.avoidanceRestored -and
        $Value.overridesRestored -and $Value.forbidRotationRestored -and
        $Value.attachmentRestored -and $Value.selectionRestored -and
        $Value.moveCommandsRestored
    if ($Value.allRestored -ne $derived) { throw 'Boundary cleanup.allRestored does not equal its raw restoration fields.' }
}

function Assert-KmcBoundaryFreshWorldEvidence {
    param($Value, [Parameter(Mandatory = $true)]$Request)
    if ($null -eq $Value) { throw 'Boundary freshWorld evidence is required.' }
    $booleanNames = @('worldReady','pairResolved','gameIdMatches','gameNameMatches','areaMatches','relationshipClean',
        'stockAgentsEnabled','avoidanceOrdinary','overridesAbsent','riderMovementAgentComponentsAbsent',
        'forbidRotationOrdinary','attachmentResidueAbsent','selectionRestored','moveCommandsAbsent','kmcAnchorObjectsAbsent','allClean')
    Assert-KmcExactProperties $Value @(@('observed','gameId','gameName','area') + $booleanNames) 'boundary freshWorld'
    if ($Value.observed -isnot [bool]) { throw 'Boundary freshWorld.observed must be a JSON boolean.' }
    foreach ($name in $booleanNames) { Assert-KmcNullableJsonBoolean $Value.$name "boundary freshWorld.$name" }
    foreach ($name in @('gameId','gameName','area')) {
        if ($null -ne $Value.$name -and $Value.$name -isnot [string]) { throw "Boundary freshWorld.$name must be a JSON string or null." }
    }
    if (-not $Value.observed) {
        if (@($booleanNames | Where-Object { $null -ne $Value.$_ }).Count -ne 0 -or
            $null -ne $Value.gameId -or $null -ne $Value.gameName -or $null -ne $Value.area) {
            throw 'Unobserved boundary freshWorld contains claimed state.'
        }
        return
    }
    if (@($booleanNames | Where-Object { $Value.$_ -isnot [bool] }).Count -ne 0) { throw 'Observed boundary freshWorld lacks exact boolean state.' }
    $derived = $true
    foreach ($name in @($booleanNames | Where-Object { $_ -cne 'allClean' })) { $derived = $derived -and [bool]$Value.$name }
    if ($Value.allClean -ne $derived) { throw 'Boundary freshWorld.allClean does not equal its raw clean-state fields.' }
    $expected = $Request.fixture.working
    if ($Value.gameIdMatches -ne ([string]$Value.gameId -ceq [string]$expected.gameId) -or
        $Value.gameNameMatches -ne ([string]$Value.gameName -ceq [string]$expected.gameName) -or
        $Value.areaMatches -ne ([string]$Value.area -ceq [string]$expected.area)) {
        throw 'Boundary freshWorld campaign match booleans do not equal their raw GameId/GameName/Area comparison.'
    }
}

function Assert-KmcBoundaryNativeLifecycleEvidence {
    param($Value)
    if ($null -eq $Value) { throw 'Boundary nativeLifecycle evidence is required.' }
    Assert-KmcExactProperties $Value @('baselineSequence','deliveryCount','deliveries') 'boundary nativeLifecycle'
    if (-not (Test-KmcExactJsonInteger $Value.baselineSequence) -or [long]$Value.baselineSequence -lt 0L -or
        -not (Test-KmcExactJsonInteger $Value.deliveryCount) -or [long]$Value.deliveryCount -lt 0L -or
        $Value.deliveries -isnot [Array] -or [long]$Value.deliveryCount -ne @($Value.deliveries).Count) {
        throw 'Boundary nativeLifecycle baseline/count/deliveries are invalid.'
    }
    $prior = [long]$Value.baselineSequence
    $knownBoundaries = @(
        'SaveRequest','LoadStart','AreaBeginUnload','AreaScenesLoaded','AreaDidLoad','AreaLoadingComplete',
        'TurnBasedEnabled','RealtimeEnabled','GameModeStarted','GameModeStopped','CombatStarted','CombatEnded',
        'ViewAttached','ViewDetachedOrUnitDestroyed','PartyRemoved','InGameStateChanged','UnitIncapacitated',
        'UnitDeath','UnitFinallyDead','ModDisable')
    foreach ($delivery in @($Value.deliveries)) {
        Assert-KmcExactProperties $delivery @(
            'sequence','boundary','source','stateBefore','stateAfter','cleanupTrigger','cleanupAttempted','cleanupSucceeded') `
            'boundary nativeLifecycle delivery'
        if (-not (Test-KmcExactJsonInteger $delivery.sequence) -or [long]$delivery.sequence -le $prior -or
            $delivery.boundary -isnot [string] -or [string]$delivery.boundary -cnotin $knownBoundaries -or
            $delivery.source -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$delivery.source) -or
            $delivery.stateBefore -isnot [string] -or [string]$delivery.stateBefore -cnotin @('Unmounted','Mounting','Mounted','Dismounting','Faulted','Disposed') -or
            $delivery.stateAfter -isnot [string] -or [string]$delivery.stateAfter -cnotin @('Unmounted','Mounting','Mounted','Dismounting','Faulted','Disposed') -or
            $delivery.cleanupAttempted -isnot [bool] -or $delivery.cleanupSucceeded -isnot [bool]) {
            throw 'Boundary nativeLifecycle delivery primitive identity or order is invalid.'
        }
        if (($delivery.cleanupAttempted -and ($delivery.cleanupTrigger -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$delivery.cleanupTrigger))) -or
            (-not $delivery.cleanupAttempted -and $null -ne $delivery.cleanupTrigger) -or
            (-not $delivery.cleanupAttempted -and -not $delivery.cleanupSucceeded)) {
            throw 'Boundary nativeLifecycle cleanup claim is ambiguous.'
        }
        $prior = [long]$delivery.sequence
    }
}

function Test-KmcBoundaryExactNativeCleanupDelivery {
    param($Record)
    $row = [string]$Record.row
    if ($row -cnotin @(Get-KmcNativeLifecycleBoundaryRuntimeRows)) { return $false }
    $trigger = [string]$Record.triggerScope.expectedCleanupTrigger
    $boundary = switch -CaseSensitive ($row) {
        'native-save-clean-dismount' { 'SaveRequest'; break }
        'native-area-clean-dismount' { 'AreaBeginUnload'; break }
        'native-mode-transition-cleanup' { if ($trigger -ceq 'TurnBasedModeChanged') { 'TurnBasedEnabled' } else { 'RealtimeEnabled' }; break }
        'presentation-residue-and-uninstall-safety' { 'ModDisable'; break }
    }
    $source = switch -CaseSensitive ($row) {
        'native-save-clean-dismount' { 'SaveManager.SaveRoutine Harmony12 prefix'; break }
        'native-area-clean-dismount' { 'ISceneHandler.OnAreaBeginUnloading'; break }
        'native-mode-transition-cleanup' { 'ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(' + ($trigger -ceq 'TurnBasedModeChanged').ToString() + ')'; break }
        'presentation-residue-and-uninstall-safety' { 'UnityModManager.ModEntry.OnToggle(false)/shutdown'; break }
    }
    $matches = @($Record.nativeLifecycle.deliveries | Where-Object {
        [string]$_.boundary -ceq $boundary -and [string]$_.source -ceq $source -and
        [string]$_.cleanupTrigger -ceq $trigger -and [string]$_.stateBefore -ceq 'Mounted' -and
        [string]$_.stateAfter -ceq 'Unmounted' -and $_.cleanupAttempted -eq $true -and $_.cleanupSucceeded -eq $true
    })
    return $matches.Count -eq 1
}

function Assert-KmcBoundaryNativeModeEvidence {
    param($Value, [Parameter(Mandatory = $true)][string]$Row)
    if ($null -eq $Value) { throw 'Boundary nativeMode evidence is required.' }
    Assert-KmcExactProperties $Value @(
        'executed','originalValue','temporaryValue','originalRawCacheHadValue','persistedValueBefore','persistedValueAfter',
        'temporaryDeliveryAttempted','restoreDeliveryCompleted','persistedValueUnchanged') 'boundary nativeMode'
    if ($Value.executed -isnot [bool]) { throw 'Boundary nativeMode.executed must be a JSON boolean.' }
    if ($Row -cne 'native-mode-transition-cleanup') {
        $optionalFields = @('originalValue','temporaryValue','originalRawCacheHadValue','persistedValueBefore','persistedValueAfter',
            'temporaryDeliveryAttempted','restoreDeliveryCompleted','persistedValueUnchanged')
        if ($Value.executed -or @($optionalFields | Where-Object { $null -ne $Value.$_ }).Count -ne 0) {
            throw 'Boundary nativeMode evidence is populated outside the native mode row.'
        }
        return
    }
    if (-not $Value.executed) { throw 'Native mode row did not execute its exact probe.' }
    foreach ($name in @('originalValue','temporaryValue','originalRawCacheHadValue','temporaryDeliveryAttempted','restoreDeliveryCompleted','persistedValueUnchanged')) {
        Assert-KmcNullableJsonBoolean $Value.$name "boundary nativeMode.$name"
    }
    if ($Value.originalValue -isnot [bool] -or $Value.temporaryValue -isnot [bool] -or
        $Value.temporaryValue -eq $Value.originalValue -or $Value.originalRawCacheHadValue -isnot [bool]) {
        throw 'Native mode original/temporary/cache evidence is invalid.'
    }
    foreach ($name in @('persistedValueBefore','persistedValueAfter')) {
        if ($null -ne $Value.$name -and $Value.$name -isnot [string]) { throw "Boundary nativeMode.$name must be string or null." }
    }
}

function Assert-KmcBoundaryModDisableEvidence {
    param($Value, [Parameter(Mandatory = $true)][string]$Row, [Parameter(Mandatory = $true)][string]$Phase)
    if ($null -eq $Value) { throw 'Boundary modDisable evidence is required.' }
    $fields = @('overlayPresentBeforeDisable','overlayObjectCountBeforeDisable','disableCallbackSucceeded',
        'overlayReferenceAbsentImmediately','overlayPresentOnDisabledFrame','overlayObjectCountOnDisabledFrame',
        'reenableCallbackSucceeded','overlayPresentAfterReenable','overlayObjectCountAfterReenable')
    $allFields = @('executed') + $fields
    Assert-KmcExactProperties $Value $allFields 'boundary modDisable'
    if ($Value.executed -isnot [bool]) { throw 'Boundary modDisable.executed must be a JSON boolean.' }
    if ($Row -cne 'presentation-residue-and-uninstall-safety') {
        if ($Value.executed -or @($fields | Where-Object { $null -ne $Value.$_ }).Count -ne 0) {
            throw 'Boundary modDisable evidence is populated outside the uninstall-safety row.'
        }
        return
    }
    if ($Phase -cin @('row-start','mounted')) {
        if ($Value.executed -or @($fields | Where-Object { $null -ne $Value.$_ }).Count -ne 0) {
            throw 'Uninstall-safety evidence claims registered-toggle execution before pre-boundary dispatch.'
        }
        return
    }
    if (-not $Value.executed) { throw 'Uninstall-safety row did not execute its registered-toggle probe.' }
    foreach ($name in @('overlayPresentBeforeDisable','disableCallbackSucceeded','overlayReferenceAbsentImmediately',
            'overlayPresentOnDisabledFrame','reenableCallbackSucceeded','overlayPresentAfterReenable')) {
        Assert-KmcNullableJsonBoolean $Value.$name "boundary modDisable.$name"
    }
    foreach ($name in @('overlayObjectCountBeforeDisable','overlayObjectCountOnDisabledFrame','overlayObjectCountAfterReenable')) {
        if ($null -ne $Value.$name -and (-not (Test-KmcExactJsonInteger $Value.$name) -or [long]$Value.$name -lt 0L)) {
            throw "Boundary modDisable.$name must be a nonnegative exact JSON integer or null."
        }
    }
}

function Assert-KmcBoundaryEvidenceRecord {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)][long]$ExpectedSequence,
        [Parameter(Mandatory = $true)][string[]]$ExpectedRows
    )
    Assert-KmcExactProperties $Record @(
        'schemaVersion','artifactKind','runId','scenario','row','phase','utcTimestamp','branch','commit','productVersion',
        'dllSha256','dllMvid','sequence','rowIndex','frame','executed','suppressed','rowStatus','assertionPassCount',
        'assertionFailCount','triggerScope','workingIdentity','authorization','loading','relationship','cleanup','freshWorld',
        'nativeLifecycle','nativeMode','modDisable','recordErrors') 'boundary evidence record'
    if (-not (Test-KmcExactJsonInteger $Record.schemaVersion) -or [long]$Record.schemaVersion -ne 2L -or
        $Record.artifactKind -isnot [string] -or [string]$Record.artifactKind -cne 'boundary-scenario-evidence') {
        throw 'Boundary evidence schemaVersion or artifactKind is not exact.'
    }
    foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if ($Record.$name -isnot [string] -or [string]$Record.$name -cne [string]$Request.$name) {
            throw "Boundary evidence identity mismatch: $name"
        }
    }
    if ([string]$Record.dllSha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$Record.dllMvid -cnotmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') {
        throw 'Boundary evidence DLL hash or MVID is not exact lowercase identity text.'
    }
    if (-not (Test-KmcExactJsonInteger $Record.sequence) -or [long]$Record.sequence -ne $ExpectedSequence) {
        throw "Boundary evidence sequence is not contiguous at $ExpectedSequence."
    }
    if ($Record.row -isnot [string]) { throw 'Boundary evidence row must be a JSON string.' }
    $expectedRowIndex = [Array]::IndexOf($ExpectedRows, [string]$Record.row)
    if ($expectedRowIndex -lt 0 -or -not (Test-KmcExactJsonInteger $Record.rowIndex) -or [long]$Record.rowIndex -ne $expectedRowIndex) {
        throw 'Boundary evidence row or rowIndex is outside the exact selected row set.'
    }
    if ($Record.phase -isnot [string] -or [string]$Record.phase -cnotin @(Get-KmcBoundaryExpectedPhases ([string]$Record.row))) {
        throw "Boundary evidence phase is invalid for $($Record.row): $($Record.phase)"
    }
    if (-not (Test-KmcExactJsonInteger $Record.frame) -or [long]$Record.frame -lt 0L -or
        $Record.executed -isnot [bool] -or $Record.suppressed -isnot [bool] -or
        ($Record.executed -and $Record.suppressed) -or (-not $Record.executed -and -not $Record.suppressed)) {
        throw 'Boundary evidence frame/executed/suppressed state is invalid.'
    }
    if ($Record.utcTimestamp -isnot [string]) { throw 'Boundary evidence UTC timestamp must be a JSON string.' }
    $timestamp = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParse([string]$Record.utcTimestamp, [ref]$timestamp) -or $timestamp.Offset -ne [TimeSpan]::Zero) {
        throw 'Boundary evidence UTC timestamp is invalid or not UTC.'
    }
    Assert-KmcJsonStringArray $Record.recordErrors 'boundary evidence recordErrors'
    if ([string]$Record.phase -ceq 'row-result') {
        if ($Record.rowStatus -isnot [string] -or [string]$Record.rowStatus -cnotin @('PASS','FAIL') -or
            -not (Test-KmcExactJsonInteger $Record.assertionPassCount) -or
            -not (Test-KmcExactJsonInteger $Record.assertionFailCount) -or
            [long]$Record.assertionPassCount -lt 0L -or [long]$Record.assertionFailCount -lt 0L -or
            [long]$Record.assertionPassCount + [long]$Record.assertionFailCount -le 0L) {
            throw 'Boundary row-result status or assertion totals are invalid.'
        }
        if ([string]$Record.rowStatus -ceq 'PASS' -and
            ([long]$Record.assertionFailCount -ne 0L -or @($Record.recordErrors).Count -ne 0 -or $Record.suppressed)) {
            throw 'Boundary PASS row-result contains a failure, error, or suppression claim.'
        }
        if ([string]$Record.rowStatus -ceq 'FAIL' -and
            ([long]$Record.assertionFailCount -lt 1L -or @($Record.recordErrors).Count -lt 1)) {
            throw 'Boundary FAIL row-result lacks a failed assertion and structured error.'
        }
        if ($Record.suppressed -and ($Record.executed -or [long]$Record.assertionPassCount -ne 0L -or
            [long]$Record.assertionFailCount -ne 1L -or [string]$Record.rowStatus -cne 'FAIL')) {
            throw 'Boundary suppressed row-result is not an exact unexecuted 0/1 FAIL.'
        }
    }
    elseif ($null -ne $Record.rowStatus -or $null -ne $Record.assertionPassCount -or
        $null -ne $Record.assertionFailCount -or $Record.suppressed) {
        throw 'Boundary non-row-result record contains result fields or suppression state.'
    }
    Assert-KmcBoundaryTriggerScope $Record.triggerScope ([string]$Record.row) ([string]$Record.phase)
    Assert-KmcBoundaryWorkingIdentity $Record.workingIdentity $Request ([string]$Record.row) ([string]$Record.phase)
    Assert-KmcBoundaryAuthorizationEvidence $Record.authorization
    Assert-KmcBoundaryNativeLifecycleEvidence $Record.nativeLifecycle
    Assert-KmcBoundaryNativeModeEvidence $Record.nativeMode ([string]$Record.row)
    Assert-KmcBoundaryModDisableEvidence $Record.modDisable ([string]$Record.row) ([string]$Record.phase)
    $nativeObserved = if ([string]$Record.row -ceq 'mounted-pair-load-safety') {
        [long]$Record.authorization.authorizedLoadsDelta -gt 0L
    }
    elseif ([string]$Record.row -cin @(Get-KmcNativeLifecycleBoundaryRuntimeRows)) {
        Test-KmcBoundaryExactNativeCleanupDelivery $Record
    }
    else { $false }
    if ($Record.triggerScope.nativeDeliveryObserved -ne $nativeObserved) {
        throw 'Boundary native-delivery claim does not equal its independent authorization/ledger signal.'
    }
    Assert-KmcBoundaryLoadingEvidence $Record.loading ([string]$Record.row)
    Assert-KmcBoundaryRelationshipEvidence $Record.relationship "boundary relationship $($Record.row)/$($Record.phase)"
    $cleanupTrigger = Get-KmcBoundaryExpectedCleanupTrigger ([string]$Record.row)
    if ([string]$Record.row -ceq 'native-mode-transition-cleanup') {
        $cleanupTrigger = [string]$Record.triggerScope.expectedCleanupTrigger
    }
    Assert-KmcBoundaryCleanupEvidence $Record.cleanup $cleanupTrigger
    Assert-KmcBoundaryFreshWorldEvidence $Record.freshWorld $Request
}

function Assert-KmcBoundaryPassRowSemantics {
    param(
        [Parameter(Mandatory = $true)][object[]]$Records,
        [Parameter(Mandatory = $true)][string]$Row
    )
    [string[]]$expectedPhases = @(Get-KmcBoundaryExpectedPhases $Row)
    [string[]]$actualPhases = @($Records | ForEach-Object { [string]$_.phase })
    if (($actualPhases -join '|') -cne ($expectedPhases -join '|')) { throw "PASS boundary phase set/order is not exact for $Row." }
    if (@($Records | Where-Object { -not $_.executed -or $_.suppressed }).Count -ne 0) { throw "PASS boundary row was not fully executed: $Row" }
    $result = $Records[$Records.Count - 1]
    if ([string]$result.rowStatus -cne 'PASS' -or [long]$result.assertionPassCount -le 0L -or
        [long]$result.assertionFailCount -ne 0L -or @($result.recordErrors).Count -ne 0) {
        throw "PASS boundary row-result is not an error-free PASS for $Row."
    }
    $rowStart = $Records[0]
    $mounted = @($Records | Where-Object { [string]$_.phase -ceq 'mounted' })[0]
    $preBoundary = @($Records | Where-Object { [string]$_.phase -ceq 'pre-boundary' })[0]
    $cleanupLatch = @($Records | Where-Object { [string]$_.phase -ceq 'cleanup-latch' })[0]
    Assert-KmcBoundaryRowStartRelationship $rowStart.relationship "$Row row-start"
    Assert-KmcBoundaryMountedRelationship $mounted.relationship "$Row mounted"
    Assert-KmcBoundaryMountedRelationship $preBoundary.relationship "$Row pre-boundary"
    Assert-KmcBoundaryCleanRelationship $cleanupLatch.relationship "$Row cleanup-latch" -RequireRestore -AllowDeferredAnchor
    if (-not $cleanupLatch.cleanup.captured -or $cleanupLatch.cleanup.allRestored -ne $true) {
        throw "$Row cleanup-latch does not prove exact residue-free boundary cleanup."
    }
    Assert-KmcBoundaryCleanRelationship $result.relationship "$Row row-result" -RequireRestore
    if (-not $result.cleanup.captured -or $result.cleanup.allRestored -ne $true) {
        throw "$Row row-result does not retain the exact cleanup latch."
    }
    foreach ($record in $Records) {
        $mustMatchPostInitial = $Row -cin @(
            'mounted-pair-turn-based-entry-cleanup','mounted-pair-realtime-entry-cleanup','mounted-pair-save-safety',
            'native-save-clean-dismount','native-mode-transition-cleanup','presentation-residue-and-uninstall-safety',
            'native-area-clean-dismount') -or
            ($Row -ceq 'mounted-pair-load-safety' -and [string]$record.phase -cin @('row-start','mounted','pre-boundary'))
        if ($mustMatchPostInitial -and $record.workingIdentity.matchesPostInitialLoad -ne $true) {
            throw "$Row changed the current post-initial-load Working identity before an authorized boundary at $($record.phase)."
        }
        if ([long]$record.authorization.authorizedLoadsBefore -lt 1L -or
            [long]$record.authorization.authorizedWritesBefore -ne 0L -or
            [long]$record.authorization.authorizedWritesAfter -ne 0L -or
            [long]$record.authorization.authorizedWritesDelta -ne 0L -or
            [long]$record.authorization.unauthorizedLoadsBefore -ne 0L -or
            [long]$record.authorization.unauthorizedLoadsAfter -ne 0L -or
            [long]$record.authorization.unauthorizedLoadsDelta -ne 0L -or
            [long]$record.authorization.unauthorizedWritesBefore -ne 0L -or
            [long]$record.authorization.unauthorizedWritesAfter -ne 0L -or
            [long]$record.authorization.unauthorizedWritesDelta -ne 0L -or
            [long]$record.authorization.baselineLoadsBefore -ne 0L -or
            [long]$record.authorization.baselineLoadsAfter -ne 0L -or
            [long]$record.authorization.baselineLoadsDelta -ne 0L -or
            [long]$record.authorization.fatalViolationsBefore -ne 0L -or
            [long]$record.authorization.fatalViolationsAfter -ne 0L -or
            [long]$record.authorization.fatalViolationsDelta -ne 0L -or
            [long]$record.authorization.suppressedWorkingWritesBefore -ne 0L -or
            [long]$record.authorization.suppressedWorkingWritesAfter -notin $(if ($Row -ceq 'native-save-clean-dismount') { @(0L,1L) } else { @(0L) }) -or
            [long]$record.authorization.suppressedWorkingWritesDelta -notin $(if ($Row -ceq 'native-save-clean-dismount') { @(0L,1L) } else { @(0L) }) -or
            $record.authorization.oneShotWorkingWriteSuppressionArmed -ne $false) {
            throw "$Row crossed a forbidden save-authorization boundary at $($record.phase)."
        }
    }
    $before = $rowStart.authorization
    foreach ($record in $Records) {
        foreach ($prefix in @('authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','baselineLoads','fatalViolations','suppressedWorkingWrites')) {
            if ([long]$record.authorization.($prefix + 'Before') -ne [long]$before.($prefix + 'Before')) {
                throw "$Row changed its captured authorization baseline within the row."
            }
        }
    }
    $expectedLoadDelta = if ($Row -ceq 'mounted-pair-load-safety') { 1L } else { 0L }
    $expectedSuppressedDelta = if ($Row -ceq 'native-save-clean-dismount') { 1L } else { 0L }
    if ([long]$result.authorization.authorizedLoadsDelta -ne $expectedLoadDelta -or
        [long]$result.authorization.authorizedWritesDelta -ne 0L -or
        [long]$result.authorization.suppressedWorkingWritesDelta -ne $expectedSuppressedDelta) {
        throw "$Row row-result does not report its exact authorized load/write delta."
    }
    $loadOrArea = $Row -cin @('mounted-pair-load-safety','mounted-pair-area-transition-safety','native-area-clean-dismount')
    foreach ($record in $Records) {
        $descriptorExpected = $Row -cin @('mounted-pair-save-safety','mounted-pair-load-safety','native-save-clean-dismount') -and
            [Array]::IndexOf($expectedPhases, [string]$record.phase) -ge [Array]::IndexOf($expectedPhases, 'pre-boundary')
        if ($descriptorExpected) {
            if ($record.workingIdentity.descriptorVerified -ne $true) { throw "$Row lacks successful exact Working descriptor verification at $($record.phase)." }
        }
        elseif ($null -ne $record.workingIdentity.descriptorVerified) {
            throw "$Row reports descriptor verification outside its exact post-validation phases."
        }
        $preDispatchExpected = ($Row -ceq 'mounted-pair-load-safety' -and
            [Array]::IndexOf($expectedPhases, [string]$record.phase) -ge [Array]::IndexOf($expectedPhases, 'cleanup-latch')) -or
            ($Row -ceq 'native-save-clean-dismount' -and
            [Array]::IndexOf($expectedPhases, [string]$record.phase) -ge [Array]::IndexOf($expectedPhases, 'pre-boundary'))
        if ($preDispatchExpected) {
            if ([long]$record.workingIdentity.preDispatchLength -ne [long]$record.workingIdentity.postInitialLoadLength -or
                [long]$record.workingIdentity.preDispatchLastWriteTimeUtcTicks -ne [long]$record.workingIdentity.postInitialLoadLastWriteTimeUtcTicks -or
                [string]$record.workingIdentity.preDispatchSha256 -cne [string]$record.workingIdentity.postInitialLoadSha256) {
                throw "Load-safety pre-dispatch identity is absent or differs at $($record.phase)."
            }
        }
        elseif ($null -ne $record.workingIdentity.preDispatchLength -or
            $null -ne $record.workingIdentity.preDispatchLastWriteTimeUtcTicks -or
            $null -ne $record.workingIdentity.preDispatchSha256) {
            throw "$Row reports a pre-dispatch Working identity outside exact load post-dispatch phases."
        }
        if (-not $loadOrArea -and $Row -cne 'native-save-clean-dismount' -and
            ($record.loading.observed -or $record.loading.startObserved -or $record.loading.stopObserved -or $record.loading.callbackObserved)) {
            throw "$Row reports a loading pipeline that it did not exercise."
        }
        if ($Row -ceq 'native-save-clean-dismount' -and
            ($record.loading.observed -or $record.loading.startObserved -or $record.loading.stopObserved -or
             ($record.loading.callbackObserved -and [string]$record.phase -cin @('row-start','mounted','pre-boundary','cleanup-latch')))) {
            throw 'Native save evidence reports impossible loading/callback progression.'
        }
    }
    if ($Row -ceq 'mounted-pair-load-safety') {
        $loadingStart = @($Records | Where-Object { [string]$_.phase -ceq 'loading-start' })[0]
        $loadingStop = @($Records | Where-Object { [string]$_.phase -ceq 'loading-stop' })[0]
        $fresh = @($Records | Where-Object { [string]$_.phase -ceq 'fresh-world' })[0]
        Assert-KmcBoundaryLoadingRelationship $loadingStart.relationship 'load-safety loading-start'
        Assert-KmcBoundaryLoadingRelationship $loadingStop.relationship 'load-safety loading-stop'
        Assert-KmcBoundaryCleanRelationship $fresh.relationship 'load-safety fresh-world' -RequireRestore
        if ($result.triggerScope.nativeDeliveryObserved -ne $true -or $result.triggerScope.realWorkingLoadDispatched -ne $true -or
            $result.triggerScope.realAreaReloadDispatched -ne $false -or -not $loadingStart.loading.startObserved -or
            -not $loadingStop.loading.stopObserved -or -not $loadingStop.loading.callbackObserved) {
            throw 'Load-safety row does not prove real exact-Working dispatch, native prefix delivery, and completed callback.'
        }
        if (@($Records | Where-Object { [string]$_.phase -cin @('row-start','mounted','pre-boundary') -and
            ($_.triggerScope.nativeDeliveryObserved -or $_.triggerScope.realWorkingLoadDispatched) }).Count -ne 0) {
            throw 'Load-safety pre-dispatch evidence claims native delivery before it occurred.'
        }
        if (@($Records | Where-Object { [string]$_.phase -cin @('cleanup-latch','loading-start','loading-stop','fresh-world','row-result') -and
            (-not $_.triggerScope.nativeDeliveryObserved -or -not $_.triggerScope.realWorkingLoadDispatched) }).Count -ne 0) {
            throw 'Load-safety post-dispatch evidence lost its real/native dispatch claim.'
        }
        foreach ($record in $Records) {
            $beforeLoading = [string]$record.phase -cin @('row-start','mounted','pre-boundary','cleanup-latch')
            $atLoadingStart = [string]$record.phase -ceq 'loading-start'
            $afterLoadingStop = [string]$record.phase -cin @('loading-stop','fresh-world','row-result')
            if (($beforeLoading -and ($record.loading.observed -or $record.loading.startObserved -or
                    $record.loading.stopObserved -or $record.loading.callbackObserved)) -or
                ($atLoadingStart -and (-not $record.loading.observed -or -not $record.loading.startObserved -or
                    $record.loading.stopObserved -or $record.loading.callbackObserved)) -or
                ($afterLoadingStop -and (-not $record.loading.observed -or -not $record.loading.startObserved -or
                    -not $record.loading.stopObserved -or -not $record.loading.callbackObserved))) {
                throw "Load-safety loading flags do not follow exact sticky start/stop/callback progression at $($record.phase)."
            }
        }
        if (-not $fresh.freshWorld.observed -or $fresh.freshWorld.allClean -ne $true -or
            -not $result.freshWorld.observed -or $result.freshWorld.allClean -ne $true) {
            throw 'Load-safety row lacks exact clean fresh-world evidence.'
        }
    }
    elseif ($Row -cin @('mounted-pair-area-transition-safety','native-area-clean-dismount')) {
        $loadingStart = @($Records | Where-Object { [string]$_.phase -ceq 'loading-start' })[0]
        $loadingStop = @($Records | Where-Object { [string]$_.phase -ceq 'loading-stop' })[0]
        $fresh = @($Records | Where-Object { [string]$_.phase -ceq 'fresh-world' })[0]
        Assert-KmcBoundaryLoadingRelationship $loadingStart.relationship 'area-transition loading-start'
        Assert-KmcBoundaryLoadingRelationship $loadingStop.relationship 'area-transition loading-stop'
        Assert-KmcBoundaryCleanRelationship $fresh.relationship 'area-transition fresh-world' -RequireRestore
        $expectedNative = $Row -ceq 'native-area-clean-dismount'
        if ($result.triggerScope.nativeDeliveryObserved -ne $expectedNative -or $result.triggerScope.realWorkingLoadDispatched -ne $false -or
            $result.triggerScope.realWorkingSaveDispatched -ne $false -or
            $result.triggerScope.realAreaReloadDispatched -ne $true -or -not $loadingStart.loading.startObserved -or
            -not $loadingStop.loading.stopObserved -or $loadingStop.loading.callbackObserved) {
            throw "$Row does not prove its exact cleanup source plus completed real ReloadArea with truthful native-delivery scope."
        }
        $preDispatchAreaPhases = if ($expectedNative) { @('row-start','mounted','pre-boundary') } else { @('row-start','mounted','pre-boundary','cleanup-latch') }
        if (@($Records | Where-Object { [string]$_.phase -cin $preDispatchAreaPhases -and
            $_.triggerScope.realAreaReloadDispatched }).Count -ne 0) {
            throw "$Row claims ReloadArea before its exact dispatch point."
        }
        $postDispatchAreaPhases = if ($expectedNative) { @('cleanup-latch','loading-start','loading-stop','fresh-world','row-result') } else { @('loading-start','loading-stop','fresh-world','row-result') }
        if (@($Records | Where-Object { [string]$_.phase -cin $postDispatchAreaPhases -and
            -not $_.triggerScope.realAreaReloadDispatched }).Count -ne 0) {
            throw "$Row post-dispatch evidence lost its real ReloadArea claim."
        }
        foreach ($record in $Records) {
            $beforeLoading = [string]$record.phase -cin @('row-start','mounted','pre-boundary','cleanup-latch')
            $atLoadingStart = [string]$record.phase -ceq 'loading-start'
            $afterLoadingStop = [string]$record.phase -cin @('loading-stop','fresh-world','row-result')
            if (($beforeLoading -and ($record.loading.observed -or $record.loading.startObserved -or
                    $record.loading.stopObserved -or $record.loading.callbackObserved)) -or
                ($atLoadingStart -and (-not $record.loading.observed -or -not $record.loading.startObserved -or
                    $record.loading.stopObserved -or $record.loading.callbackObserved)) -or
                ($afterLoadingStop -and (-not $record.loading.observed -or -not $record.loading.startObserved -or
                    -not $record.loading.stopObserved -or $record.loading.callbackObserved))) {
                throw "Area-transition loading flags do not follow exact sticky start/stop progression at $($record.phase)."
            }
        }
        if (-not $fresh.freshWorld.observed -or $fresh.freshWorld.allClean -ne $true -or
            -not $result.freshWorld.observed -or $result.freshWorld.allClean -ne $true) {
            throw 'Area-transition row lacks exact clean fresh-world evidence.'
        }
        if ($expectedNative) {
            $deliveries = @($result.nativeLifecycle.deliveries)
            $unload = @($deliveries | Where-Object { [string]$_.boundary -ceq 'AreaBeginUnload' -and [string]$_.source -ceq 'ISceneHandler.OnAreaBeginUnloading' })
            $scenes = @($deliveries | Where-Object { [string]$_.boundary -ceq 'AreaScenesLoaded' -and [string]$_.source -ceq 'IAreaLoadingStagesHandler.OnAreaScenesLoaded' })
            $didLoad = @($deliveries | Where-Object { [string]$_.boundary -ceq 'AreaDidLoad' -and [string]$_.source -ceq 'ISceneHandler.OnAreaDidLoad' })
            $complete = @($deliveries | Where-Object { [string]$_.boundary -ceq 'AreaLoadingComplete' -and [string]$_.source -ceq 'IAreaLoadingStagesHandler.OnAreaLoadingComplete' })
            if ($unload.Count -ne 1 -or $scenes.Count -ne 1 -or $didLoad.Count -ne 1 -or $complete.Count -ne 1 -or
                [long]$unload[0].sequence -ge [long]$scenes[0].sequence -or [long]$scenes[0].sequence -ge [long]$didLoad[0].sequence -or
                [long]$didLoad[0].sequence -ge [long]$complete[0].sequence) {
                throw 'Native area row lacks exact ordered unload/scenes/did-load/loading-complete delivery.'
            }
        }
    }
    elseif ($Row -ceq 'native-save-clean-dismount') {
        $post = @($Records | Where-Object { [string]$_.phase -ceq 'post-boundary' })[0]
        Assert-KmcBoundaryCleanRelationship $post.relationship 'native save post-boundary' -RequireRestore
        if ($result.triggerScope.nativeDeliveryObserved -ne $true -or
            $result.triggerScope.realWorkingSaveDispatched -ne $true -or
            $result.triggerScope.stockSaveRoutineInvoked -ne $true -or
            $result.triggerScope.realWorkingLoadDispatched -ne $false -or
            $result.triggerScope.realAreaReloadDispatched -ne $false -or
            [long]$result.authorization.suppressedWorkingWritesDelta -ne 1L -or
            $result.loading.callbackObserved -ne $true) {
            throw 'Native save row does not bind real SaveGame/prefix/callback delivery to one exact nonfatal serialization suppression.'
        }
        if (@($Records | Where-Object { [string]$_.phase -cin @('row-start','mounted','pre-boundary') -and
            ($_.triggerScope.nativeDeliveryObserved -or $_.triggerScope.realWorkingSaveDispatched -or
             $_.triggerScope.stockSaveRoutineInvoked -or [long]$_.authorization.suppressedWorkingWritesDelta -ne 0L) }).Count -ne 0) {
            throw 'Native save evidence claims dispatch or suppression before the prefix boundary.'
        }
        if (@($Records | Where-Object { [string]$_.phase -cin @('cleanup-latch','post-boundary','row-result') -and
            (-not $_.triggerScope.nativeDeliveryObserved -or -not $_.triggerScope.realWorkingSaveDispatched -or
             -not $_.triggerScope.stockSaveRoutineInvoked -or [long]$_.authorization.suppressedWorkingWritesDelta -ne 1L) }).Count -ne 0) {
            throw 'Native save post-dispatch evidence lost its exact native/suppression claim.'
        }
        if ([string]$post.workingIdentity.observedSha256 -cne [string]$rowStart.workingIdentity.observedSha256 -or
            [long]$post.workingIdentity.observedLength -ne [long]$rowStart.workingIdentity.observedLength -or
            [long]$post.workingIdentity.observedLastWriteTimeUtcTicks -ne [long]$rowStart.workingIdentity.observedLastWriteTimeUtcTicks) {
            throw 'Native save changed exact Working bytes or metadata.'
        }
    }
    elseif ($Row -ceq 'native-mode-transition-cleanup') {
        $post = @($Records | Where-Object { [string]$_.phase -ceq 'post-boundary' })[0]
        Assert-KmcBoundaryCleanRelationship $post.relationship 'native mode post-boundary' -RequireRestore
        if ($result.triggerScope.nativeDeliveryObserved -ne $true -or
            $result.nativeMode.temporaryDeliveryAttempted -ne $true -or
            $result.nativeMode.restoreDeliveryCompleted -ne $true -or
            $result.nativeMode.persistedValueUnchanged -ne $true -or
            [string]$result.nativeMode.persistedValueBefore -cne [string]$result.nativeMode.persistedValueAfter -or
            $result.nativeMode.temporaryValue -ne (-not [bool]$result.nativeMode.originalValue)) {
            throw 'Native mode row does not prove temporary EventBus delivery plus exact cache/persistence restoration.'
        }
        $restoreTrigger = if ($result.nativeMode.originalValue) { 'TurnBasedModeChanged' } else { 'RealtimeModeChanged' }
        $restoreBoundary = if ($result.nativeMode.originalValue) { 'TurnBasedEnabled' } else { 'RealtimeEnabled' }
        $restoreSource = 'ITurnBasedModeEnabledHandler.HandleTurnBasedModeStateChanged(' + ([bool]$result.nativeMode.originalValue).ToString() + ')'
        $restore = @($result.nativeLifecycle.deliveries | Where-Object {
            [string]$_.boundary -ceq $restoreBoundary -and [string]$_.source -ceq $restoreSource -and
            [string]$_.cleanupTrigger -ceq $restoreTrigger -and [string]$_.stateBefore -ceq 'Unmounted' -and
            [string]$_.stateAfter -ceq 'Unmounted' -and $_.cleanupAttempted -eq $true -and $_.cleanupSucceeded -eq $true })
        if ($restore.Count -ne 1) { throw 'Native mode row lacks the exact clean restore EventBus delivery.' }
    }
    elseif ($Row -ceq 'presentation-residue-and-uninstall-safety') {
        $post = @($Records | Where-Object { [string]$_.phase -ceq 'post-boundary' })[0]
        Assert-KmcBoundaryCleanRelationship $post.relationship 'mod-disable post-boundary' -RequireRestore
        if ($result.triggerScope.nativeDeliveryObserved -ne $true -or
            $result.modDisable.overlayPresentBeforeDisable -ne $true -or
            [long]$result.modDisable.overlayObjectCountBeforeDisable -ne 1L -or
            $result.modDisable.disableCallbackSucceeded -ne $true -or
            $result.modDisable.overlayReferenceAbsentImmediately -ne $true -or
            $result.modDisable.overlayPresentOnDisabledFrame -ne $false -or
            [long]$result.modDisable.overlayObjectCountOnDisabledFrame -ne 0L -or
            $result.modDisable.reenableCallbackSucceeded -ne $true -or
            $result.modDisable.overlayPresentAfterReenable -ne $true -or
            [long]$result.modDisable.overlayObjectCountAfterReenable -ne 1L) {
            throw 'Uninstall-safety row does not prove exact registered disable cleanup, zero disabled-frame overlay residue, and clean re-enable.'
        }
    }
    else {
        $post = @($Records | Where-Object { [string]$_.phase -ceq 'post-boundary' })[0]
        Assert-KmcBoundaryCleanRelationship $post.relationship "$Row post-boundary" -RequireRestore
        if (-not $post.cleanup.captured -or $post.cleanup.allRestored -ne $true -or
            $post.freshWorld.observed -or $result.freshWorld.observed) {
            throw "$Row post-boundary evidence does not prove direct cleanup without an unclaimed fresh-world transition."
        }
        if ($result.triggerScope.nativeDeliveryObserved -ne $false -or $result.triggerScope.realWorkingSaveDispatched -ne $false -or
            $result.triggerScope.realWorkingLoadDispatched -ne $false -or
            $result.triggerScope.realAreaReloadDispatched -ne $false) {
            throw "$Row row-result exceeds its direct-call claim scope."
        }
    }
}

function Assert-KmcBoundaryLiveTerminalWorkingIdentity {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)][string]$ExpectedWorkingPath
    )
    $path = [IO.Path]::GetFullPath($ExpectedWorkingPath)
    if (-not [string]::Equals(
        [IO.Path]::GetFullPath([string]$Record.workingIdentity.path),
        $path,
        [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Boundary evidence Working path differs from the exact transaction-owned live Working path.'
    }
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw 'Live terminal Working identity cannot be verified because the exact evidence path is missing.'
    }
    Assert-KmcNotReparsePoint $path 'live terminal boundary Working save'
    Assert-KmcNotHardLink $path 'live terminal boundary Working save'
    $before = Get-Item -LiteralPath $path -Force
    $sha256 = Get-KmcSha256 $path
    $after = Get-Item -LiteralPath $path -Force
    if ($before.Length -ne $after.Length -or
        $before.LastWriteTimeUtc.Ticks -ne $after.LastWriteTimeUtc.Ticks) {
        throw 'Live terminal Working changed while its identity was being verified.'
    }
    if ($after.Length -ne [long]$Record.workingIdentity.observedLength -or
        $after.LastWriteTimeUtc.Ticks -ne [long]$Record.workingIdentity.observedLastWriteTimeUtcTicks -or
        [string]$sha256 -cne [string]$Record.workingIdentity.observedSha256) {
        throw 'Live terminal Working length/timestamp/SHA-256 differs from the terminal boundary evidence.'
    }
}

function Assert-KmcBoundaryScenarioEvidence {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$Manifest,
        [AllowNull()][string]$Status,
        $SubscenarioResults,
        $GameResult,
        [switch]$VerifyLiveWorkingIdentity,
        [AllowNull()][string]$ExpectedLiveWorkingPath
    )
    $evidenceRoot = [IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')
    Assert-KmcKnownRuntimeArtifactsManifested $evidenceRoot $Manifest
    $isBoundary = Test-KmcBoundaryRuntimeScenario ([string]$Request.scenario)
    $artifacts = @($Manifest.artifacts | Where-Object { [string]$_.relativePath -ceq 'boundary-scenario-evidence.jsonl' })
    $requireComplete = $isBoundary -and [string]$Status -ceq 'PASS'
    $requireEvidence = $isBoundary -and -not [string]::IsNullOrWhiteSpace($Status)
    if ($requireEvidence -and $artifacts.Count -ne 1) { throw 'A terminal boundary scenario requires exactly one manifested boundary JSONL artifact.' }
    if ($artifacts.Count -eq 0) { return }
    if ($artifacts.Count -ne 1 -or [string]$artifacts[0].kind -cne 'boundary-evidence') { throw 'Boundary JSONL manifest identity is not exact.' }
    if (-not $isBoundary) { throw 'Boundary JSONL is present for a non-boundary runtime scenario.' }
    [string[]]$expectedRows = if ([string]$Request.scenario -ceq 'boundary-suite') { @(Get-KmcBoundaryRuntimeRows) } else { @([string]$Request.scenario) }
    $path = Assert-KmcChildPath (Join-Path $evidenceRoot 'boundary-scenario-evidence.jsonl') $evidenceRoot 'boundary scenario evidence'
    Assert-KmcNotReparsePoint $path 'boundary scenario evidence'
    Assert-KmcNotHardLink $path 'boundary scenario evidence'
    $lines = @([IO.File]::ReadAllLines($path, (New-Object Text.UTF8Encoding($false, $true))))
    $records = New-Object 'Collections.Generic.List[object]'
    foreach ($line in $lines) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        Assert-KmcJsonObjectMembersUnique $line 'boundary scenario evidence line'
        try { $record = $line | ConvertFrom-Json }
        catch { throw "Boundary scenario evidence line is malformed JSON: $($_.Exception.Message)" }
        Assert-KmcBoundaryEvidenceRecord $record $Request $records.Count $expectedRows
        $records.Add($record)
    }
    if ($records.Count -eq 0) { throw 'Boundary scenario evidence contains no nonblank JSON records.' }
    $lastFrame = -1L
    $lastRowIndex = -1
    $rowResults = New-Object 'Collections.Generic.List[object]'
    $failureSeen = $false
    $expectedAuthorizationBefore = @{
        authorizedLoads = 1L
        authorizedWrites = 0L
        unauthorizedLoads = 0L
        unauthorizedWrites = 0L
        baselineLoads = 0L
        fatalViolations = 0L
        suppressedWorkingWrites = 0L
    }
    $expectedRestoreHistory = $false
    $stableRiderId = $null
    $stableMountId = $null
    $workingAuthority = $records[0].workingIdentity
    $previousTerminalIdentity = $null
    $previousTerminalSelection = $null
    foreach ($record in $records) {
        if ([long]$record.frame -lt $lastFrame) { throw 'Boundary evidence frame order regressed.' }
        $lastFrame = [long]$record.frame
        if ([long]$record.rowIndex -lt $lastRowIndex) { throw 'Boundary evidence row order regressed.' }
        $lastRowIndex = [long]$record.rowIndex
        if ([string]$record.workingIdentity.path -cne [string]$workingAuthority.path -or
            [long]$record.workingIdentity.postInitialLoadLength -ne [long]$workingAuthority.postInitialLoadLength -or
            [long]$record.workingIdentity.postInitialLoadLastWriteTimeUtcTicks -ne [long]$workingAuthority.postInitialLoadLastWriteTimeUtcTicks -or
            [string]$record.workingIdentity.postInitialLoadSha256 -cne [string]$workingAuthority.postInitialLoadSha256) {
            throw 'Boundary evidence changed its absolute Working path or post-initial-load identity authority.'
        }
        if ($null -ne $record.relationship.riderUniqueId) {
            if ($null -eq $stableRiderId) {
                $stableRiderId = [string]$record.relationship.riderUniqueId
                $stableMountId = [string]$record.relationship.mountUniqueId
            }
            elseif ([string]$record.relationship.riderUniqueId -cne $stableRiderId -or
                [string]$record.relationship.mountUniqueId -cne $stableMountId) {
                throw 'Boundary evidence changed stable rider or mount identity across phases/rows.'
            }
        }
        if ([string]$record.phase -ceq 'row-result') { $rowResults.Add($record) }
    }
    if ($rowResults.Count -ne $expectedRows.Count) { throw 'Boundary evidence does not contain exactly one row-result per selected row.' }
    for ($rowIndex = 0; $rowIndex -lt $expectedRows.Count; $rowIndex++) {
        $row = [string]$expectedRows[$rowIndex]
        $rowRecords = @($records | Where-Object { [string]$_.row -ceq $row })
        if ($rowRecords.Count -eq 0 -or [string]$rowRecords[$rowRecords.Count - 1].phase -cne 'row-result') {
            throw "Boundary row does not end in its exact row-result: $row"
        }
        $rowResult = $rowRecords[$rowRecords.Count - 1]
        $rowPassed = [string]$rowResult.rowStatus -ceq 'PASS'
        $nativeBaseline = [long]$rowRecords[0].nativeLifecycle.baselineSequence
        $priorNativeSequences = @()
        foreach ($record in $rowRecords) {
            if ([long]$record.nativeLifecycle.baselineSequence -ne $nativeBaseline) {
                throw "$row changed its native lifecycle baseline within the row."
            }
            [long[]]$sequences = @($record.nativeLifecycle.deliveries | ForEach-Object { [long]$_.sequence })
            if ($sequences.Count -lt $priorNativeSequences.Count -or
                ($priorNativeSequences -join '|') -cne (@($sequences | Select-Object -First $priorNativeSequences.Count) -join '|')) {
                throw "$row native lifecycle evidence is not a monotonic cumulative prefix at $($record.phase)."
            }
            $priorNativeSequences = $sequences
        }
        if ($rowPassed -and $row -cin @(Get-KmcNativeLifecycleBoundaryRuntimeRows) -and
            @($rowRecords[0].nativeLifecycle.deliveries).Count -ne 0) {
            throw "$row began with post-baseline native lifecycle deliveries."
        }
        $priorDeltas = @{}
        foreach ($prefix in @('authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','baselineLoads','fatalViolations','suppressedWorkingWrites')) {
            $priorDeltas[$prefix] = 0L
        }
        foreach ($record in $rowRecords) {
            foreach ($prefix in @('authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','baselineLoads','fatalViolations','suppressedWorkingWrites')) {
                $beforeName = $prefix + 'Before'
                $afterName = $prefix + 'After'
                $deltaName = $prefix + 'Delta'
                $delta = [long]$record.authorization.$deltaName
                if ([long]$record.authorization.$beforeName -ne [long]$expectedAuthorizationBefore[$prefix] -or
                    [long]$record.authorization.$afterName -ne ([long]$expectedAuthorizationBefore[$prefix] + $delta) -or
                    $delta -lt [long]$priorDeltas[$prefix] -or ($record.suppressed -and $delta -ne 0L)) {
                    throw "$row does not preserve reconciled monotonic/cross-row $prefix authorization counters at $($record.phase)."
                }
                $priorDeltas[$prefix] = $delta
            }
            if ($rowPassed) {
                $loadDelta = [long]$record.authorization.authorizedLoadsDelta
                if (($row -ceq 'mounted-pair-load-safety' -and $loadDelta -notin @(0L,1L)) -or
                    ($row -cne 'mounted-pair-load-safety' -and $loadDelta -ne 0L)) {
                    throw "$row has an impossible authorized-load delta in PASS evidence at $($record.phase)."
                }
            }
        }
        if ([string]$rowResult.rowStatus -ceq 'PASS') {
            if ($failureSeen) { throw 'Boundary suite executed a PASS row after a prior failure.' }
            Assert-KmcBoundaryPassRowSemantics $rowRecords $row
            $rowStart = $rowRecords[0]
            if ($null -eq $previousTerminalIdentity) {
                if (-not (Test-KmcBoundaryObservedEqualsPostInitial $rowStart.workingIdentity)) {
                    throw "$row row-start does not equal the captured post-initial-load Working identity."
                }
            }
            elseif (-not (Test-KmcBoundaryObservedIdentityEqual $rowStart.workingIdentity $previousTerminalIdentity)) {
                throw "$row row-start Working identity does not continue from the preceding row-result."
            }
            if ($null -ne $previousTerminalSelection -and
                -not (Test-KmcBoundaryStringArrayOrdinalEqual $rowStart.relationship.selectedUnitIds $previousTerminalSelection)) {
                throw "$row row-start selected-unit identities do not continue from the preceding row-result."
            }
            if ($row -ceq 'mounted-pair-load-safety') {
                foreach ($phase in @('row-start','mounted','pre-boundary','cleanup-latch','loading-start')) {
                    $phaseRecord = @($rowRecords | Where-Object { [string]$_.phase -ceq $phase })[0]
                    if (-not (Test-KmcBoundaryObservedIdentityEqual $phaseRecord.workingIdentity $rowStart.workingIdentity)) {
                        throw "Load-safety Working identity changed outside the exact completed-load recapture window at $phase."
                    }
                }
                $loadingStop = @($rowRecords | Where-Object { [string]$_.phase -ceq 'loading-stop' })[0]
                foreach ($phase in @('fresh-world','row-result')) {
                    $phaseRecord = @($rowRecords | Where-Object { [string]$_.phase -ceq $phase })[0]
                    if (-not (Test-KmcBoundaryObservedIdentityEqual $phaseRecord.workingIdentity $loadingStop.workingIdentity)) {
                        throw "Load-safety Working identity was not stable from loading-stop through $phase."
                    }
                }
            }
            else {
                foreach ($record in $rowRecords) {
                    if (-not (Test-KmcBoundaryObservedIdentityEqual $record.workingIdentity $rowStart.workingIdentity)) {
                        throw "$row changed Working identity despite exercising no authorized archive write/load boundary at $($record.phase)."
                    }
                }
            }
            [string[]]$selectionPhases = if ($row -cin @('mounted-pair-load-safety','mounted-pair-area-transition-safety','native-area-clean-dismount')) {
                @('row-start','mounted','pre-boundary','cleanup-latch','fresh-world','row-result')
            } else {
                @(Get-KmcBoundaryExpectedPhases $row)
            }
            foreach ($phase in $selectionPhases) {
                $phaseRecord = @($rowRecords | Where-Object { [string]$_.phase -ceq $phase })[0]
                if (-not (Test-KmcBoundaryStringArrayOrdinalEqual $phaseRecord.relationship.selectedUnitIds $rowStart.relationship.selectedUnitIds)) {
                    throw "$row did not preserve exact selected-unit identities at $phase."
                }
            }
            if ($rowRecords[0].relationship.attachmentRestoreVerified -ne $expectedRestoreHistory) {
                throw "$row row-start attachment restore history does not continue exactly from the preceding boundary row."
            }
            $expectedRestoreHistory = $true
            $previousTerminalIdentity = $rowResult.workingIdentity
            $previousTerminalSelection = @($rowResult.relationship.selectedUnitIds)
        }
        elseif ($rowResult.suppressed) {
            if (-not $failureSeen -or $rowRecords.Count -ne 1) { throw 'Boundary suppressed row is not the sole row-result after a prior failure.' }
        }
        else {
            if ($failureSeen) { throw 'Boundary suite executed more than one failed row.' }
            $failureSeen = $true
            [string[]]$expectedPhases = @(Get-KmcBoundaryExpectedPhases $row)
            [string[]]$phasePrefix = @($rowRecords | Select-Object -First ($rowRecords.Count - 1) | ForEach-Object { [string]$_.phase })
            if ($phasePrefix.Count -gt $expectedPhases.Count - 1 -or
                ($phasePrefix -join '|') -cne (@($expectedPhases | Select-Object -First $phasePrefix.Count) -join '|')) {
                throw "Executed failed boundary row is not an ordered phase prefix: $row"
            }
        }
        foreach ($prefix in @('authorizedLoads','authorizedWrites','unauthorizedLoads','unauthorizedWrites','baselineLoads','fatalViolations','suppressedWorkingWrites')) {
            $afterName = $prefix + 'After'
            $expectedAuthorizationBefore[$prefix] = [long]$rowResult.authorization.$afterName
        }
    }
    if ($requireComplete -and $failureSeen) { throw 'PASS boundary scenario contains a failed or suppressed row.' }
    if ([string]$Status -ceq 'FAIL' -and -not $failureSeen) { throw 'FAIL boundary scenario evidence contains no failed row.' }
    if ($null -ne $SubscenarioResults) {
        $subresults = @($SubscenarioResults)
        if ($subresults.Count -ne $expectedRows.Count) { throw 'Boundary subresult count does not match the exact selected row set.' }
        for ($index = 0; $index -lt $expectedRows.Count; $index++) {
            if ([string]$subresults[$index].name -cne [string]$expectedRows[$index]) { throw 'Boundary subresults do not preserve exact row order.' }
            $rowResult = $rowResults[$index]
            if ([string]$rowResult.row -cne [string]$subresults[$index].name -or
                [string]$rowResult.rowStatus -cne [string]$subresults[$index].status -or
                [long]$rowResult.assertionPassCount -ne [long]$subresults[$index].assertionPassCount -or
                [long]$rowResult.assertionFailCount -ne [long]$subresults[$index].assertionFailCount -or
                (@($rowResult.recordErrors) -join "`n") -cne (@($subresults[$index].errors) -join "`n")) {
                throw "Boundary row-result does not reconcile with the game subresult: $($expectedRows[$index])"
            }
        }
    }
    if ($null -ne $GameResult) {
        $aggregateNames = @('workingLoadRequestCount','workingSaveRequestCount','suppressedWorkingSaveRequestCount','unauthorizedLoadRequestCount',
            'unauthorizedSaveRequestCount','baselineLoadRequestCount')
        foreach ($name in $aggregateNames) {
            if ($GameResult.PSObject.Properties.Name -cnotcontains $name -or
                -not (Test-KmcExactJsonInteger $GameResult.$name) -or [long]$GameResult.$name -lt 0L) {
                throw "Boundary game-result aggregate is missing or invalid: $name"
            }
        }
        $terminalAuthorization = $rowResults[$rowResults.Count - 1].authorization
        if ([long]$terminalAuthorization.authorizedLoadsAfter -ne [long]$GameResult.workingLoadRequestCount -or
            [long]$terminalAuthorization.authorizedWritesAfter -ne [long]$GameResult.workingSaveRequestCount -or
            [long]$terminalAuthorization.suppressedWorkingWritesAfter -ne [long]$GameResult.suppressedWorkingSaveRequestCount -or
            [long]$terminalAuthorization.unauthorizedLoadsAfter -ne [long]$GameResult.unauthorizedLoadRequestCount -or
            [long]$terminalAuthorization.unauthorizedWritesAfter -ne [long]$GameResult.unauthorizedSaveRequestCount -or
            [long]$terminalAuthorization.baselineLoadsAfter -ne [long]$GameResult.baselineLoadRequestCount) {
            throw 'Boundary terminal authorization evidence does not reconcile with final game-result aggregates.'
        }
    }
    if ($VerifyLiveWorkingIdentity) {
        if ([string]::IsNullOrWhiteSpace($ExpectedLiveWorkingPath)) {
            throw 'Opt-in live terminal Working verification requires the exact transaction-owned Working path.'
        }
        Assert-KmcBoundaryLiveTerminalWorkingIdentity $rowResults[$rowResults.Count - 1] $ExpectedLiveWorkingPath
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
        'attachmentResidue','riderParentMatchesAttachment','riderParent','attachmentParent','sourceAnchor','attachmentRiskState',
        'poseConfigured','poseHealthy','poseFrameApplied','poseBaselineRestoreVerified','poseComponentCount','poseBoneCount',
        'poseProfileId','poseBoneInventory','poseFailure') $Description
    if ($Value.trigger -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Value.trigger)) { throw "$Description.trigger must be a nonempty JSON string." }
    if ($Value.relationshipState -isnot [string] -or [string]$Value.relationshipState -cnotin @('Unmounted','Validating','Mounting','Mounted','Dismounting','Faulted','Disposed')) {
        throw "$Description.relationshipState is invalid."
    }
    foreach ($name in @('hasMountedResidual','riderOverridePresent','mountOverridePresent','riderSelected','mountSelected',
        'attachmentLeaseActive','attachmentRestoreVerified','attachmentResidue','riderParentMatchesAttachment',
        'poseConfigured','poseHealthy','poseFrameApplied','poseBaselineRestoreVerified')) {
        if ($Value.$name -isnot [bool]) { throw "$Description.$name must be a JSON boolean." }
    }
    foreach ($name in @('riderStockAgentEnabled','mountStockAgentEnabled','riderAvoidanceDisabled','mountAvoidanceDisabled','paused','riderForbidRotation')) {
        Assert-KmcNullableJsonBoolean $Value.$name "$Description.$name"
    }
    Assert-KmcJsonStringArray $Value.selectedUnitIds "$Description.selectedUnitIds"
    if ($null -ne $Value.poseComponentCount -and (-not (Test-KmcExactJsonInteger $Value.poseComponentCount) -or [long]$Value.poseComponentCount -lt 0)) {
        throw "$Description.poseComponentCount must be a nonnegative exact JSON integer or null."
    }
    if (-not (Test-KmcExactJsonInteger $Value.poseBoneCount) -or [long]$Value.poseBoneCount -lt 0) {
        throw "$Description.poseBoneCount must be a nonnegative exact JSON integer."
    }
    foreach ($name in @('riderParent','attachmentParent','sourceAnchor','attachmentRiskState','poseProfileId','poseBoneInventory','poseFailure')) {
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
            [string]$Value.sourceAnchor -cne 'Spine' -or [string]$Value.attachmentRiskState -cne 'active and internally consistent' -or
            $Value.poseConfigured -ne $true -or $Value.poseHealthy -ne $true -or $Value.poseFrameApplied -ne $true -or
            $Value.poseBaselineRestoreVerified -ne $false -or [long]$Value.poseComponentCount -ne 1 -or
            [long]$Value.poseBoneCount -ne 7 -or [string]$Value.poseProfileId -cne 'medium-humanoid-mammoth-v1' -or
            [string]::IsNullOrWhiteSpace([string]$Value.poseBoneInventory) -or $null -ne $Value.poseFailure) {
            throw 'PASS movement cleanup-before evidence does not prove the exact mounted movement-authority and pose leases.'
        }
    }
    if ($RequireComplete -and $Phase -ceq 'after') {
        if ([string]$Value.relationshipState -cne 'Unmounted' -or $Value.hasMountedResidual -ne $false -or
            $Value.riderOverridePresent -ne $false -or $Value.mountOverridePresent -ne $false -or
            $Value.riderForbidRotation -ne $false -or
            $Value.attachmentLeaseActive -ne $false -or $Value.attachmentRestoreVerified -ne $true -or
            $Value.attachmentResidue -ne $false -or $Value.riderParentMatchesAttachment -ne $false -or
            $null -ne $Value.attachmentParent -or $null -ne $Value.sourceAnchor -or
            [string]$Value.attachmentRiskState -cne 'none' -or
            $Value.poseConfigured -ne $false -or $Value.poseHealthy -ne $false -or $Value.poseFrameApplied -ne $false -or
            $Value.poseBaselineRestoreVerified -ne $true -or [long]$Value.poseComponentCount -ne 0 -or
            [long]$Value.poseBoneCount -ne 0 -or $null -ne $Value.poseProfileId -or $null -ne $Value.poseBoneInventory -or
            $null -ne $Value.poseFailure) {
            throw 'PASS movement cleanup-after evidence does not prove residue-free Unmounted movement, attachment, and pose cleanup.'
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
    $expectedAlignedLateUpdateCarry = $recoveryRequired -and $phase -ceq 'LateUpdate' -and
        $viewCurrent -le 0.10 -and $rawCurrent -le 0.10
    $expectedRecoveryViolation = $recoveryRequired -and
        -not ($expectedRecoverySatisfied -or $expectedAlignedLateUpdateCarry)
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
    if ($calibrated -and ($viewCurrent -gt 0.10 -or $expectedAdjusted -gt 0.10 -or
        [double]$Record.latestEntityRawPositionLagExcessWorldUnits -gt 0.0001)) {
        throw 'PASS movement telemetry latest effective position or raw-lag arithmetic exceeds its bound.'
    }
    if (-not $calibrated -and [double]$Record.postCorrectionPositionResidualWorldUnits -gt 0.10) {
        throw 'PASS movement telemetry InitialConfiguration position was not corrected within its fixed bound.'
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
        $isPauseRow = [string]$Record.row -ceq 'mounted-pair-pause-unpause'
        $gameModeAndPauseAreCoherent = if ($isPauseRow) {
            $Record.paused -is [bool] -and
                (($Record.paused -eq $true -and [string]$Record.currentGameMode -ceq 'Pause') -or
                 ($Record.paused -eq $false -and [string]$Record.currentGameMode -ceq 'Default'))
        }
        else {
            $Record.paused -eq $false -and [string]$Record.currentGameMode -ceq 'Default'
        }
        if ([string]$Record.relationshipState -cne 'Mounted' -or [string]$Record.authoritativeMover -cne 'mount' -or
            $Record.combat -ne $false -or $Record.partyCombat -ne $false -or $Record.turnBased -ne $false -or
            -not $gameModeAndPauseAreCoherent -or $Record.riderStockAgentEnabled -ne $false -or
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
        $latestPhase = [string]$Record.synchronizationPhase
        if ($latestPhase -cnotin @('InitialConfiguration','Update','LateUpdate')) { throw 'PASS movement telemetry synchronizationPhase is invalid.' }
        $latestCalibrated = $latestPhase -ceq 'Update' -or $latestPhase -ceq 'LateUpdate'
        if ($Record.latestMountEntityRootYawResidualDegrees -ne $null -and [double]$Record.latestMountEntityRootYawResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest mount entity/root yaw is incoherent.' }
        if ($latestCalibrated -and $Record.latestViewCurrentYawResidualDegrees -ne $null -and [double]$Record.latestViewCurrentYawResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest rider view yaw is not current.' }
        if ($latestCalibrated -and $Record.latestFullViewCurrentRotationResidualDegrees -ne $null -and [double]$Record.latestFullViewCurrentRotationResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest rider view quaternion is not current.' }
        if ($latestCalibrated -and $Record.latestEntityPhaseAdjustedYawResidualDegrees -ne $null -and [double]$Record.latestEntityPhaseAdjustedYawResidualDegrees -gt 0.10) { throw 'PASS movement telemetry latest logical entity yaw is not current or an eligible immediate prior yaw.' }
        if (-not $latestCalibrated -and [double]$Record.postCorrectionRotationResidualDegrees -gt 0.10) { throw 'PASS movement telemetry InitialConfiguration rotation was not corrected within its fixed bound.' }
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
            'latestEntityRawLagExcessDegrees','latestPhaseLagObserved','latestPhaseLagPermitted',
            'latestPhaseLagViolation','latestRecoveryRequiredBeforeSample','latestRecoveryUpdateObserved','latestRecoverySatisfied',
            'latestRecoveryViolation','latestRecoveryPendingAfterSample','latestStationaryAuthority',
            'latestStationaryYawCorrectionViolation')
        foreach ($name in $requiredLatest) {
            if ($null -eq $Record.$name) { throw "PASS movement telemetry latest phase-order field $name is null." }
        }

        $phase = $latestPhase
        $calibrated = $latestCalibrated
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
        $expectedAlignedLateUpdateCarry = $recoveryRequired -and $phase -ceq 'LateUpdate' -and
            $viewCurrent -le 0.10 -and $rawCurrent -le 0.10
        $expectedRecoveryViolation = $recoveryRequired -and
            -not ($expectedRecoverySatisfied -or $expectedAlignedLateUpdateCarry)
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
            'poseProfileId','poseBoneInventory','poseObservationCount','poseHealthyObservationCount',
            'poseFrameAppliedObservationCount','poseApplicationFrameCount','poseFootTargetClampCount',
            'poseMaximumFootTargetResidualWorldUnits','poseMaximumKneeTargetResidualWorldUnits',
            'poseMaximumSegmentLengthResidualWorldUnits','poseMaximumApplyMicroseconds','poseAverageApplyMicroseconds',
            'poseMaximumPelvisLocalFrameDeltaWorldUnits','poseMaximumLeftFootLocalFrameDeltaWorldUnits',
            'poseMaximumRightFootLocalFrameDeltaWorldUnits','poseMaximumComponentCount','poseMaximumBoneCount','poseFailure',
            'walkMovingSampleCount','runMovingSampleCount','walkMaximumSpeedWorldUnitsPerSecond','runMaximumSpeedWorldUnitsPerSecond',
            'equipmentSets','uiObservations','uiRiderPortraitSelected','uiRiderSelectionCircleSelected','uiRiderActionBarOwned',
            'uiMountNormalized','uiAwayOwned','uiBackOwned','uiOverlayRendered','uiOverlayRepaintCountBefore',
            'uiOverlayRepaintCountAfter','uiOverlayLabel','uiOverlayEnabled','uiOverlayVisible','uiOverlayButtonActivationCount',
            'uiObservationFailure','cameraFollowAccepted','cameraObservationCount','cameraMinimumTargetResidualWorldUnits',
            'cameraMaximumTargetResidualWorldUnits','cameraFinalTargetResidualWorldUnits','cameraMinimumRigResidualWorldUnits',
            'cameraMaximumRigResidualWorldUnits','cameraAwayObserved','cameraBackObserved',
            'cleanupTrigger','cleanupSucceeded','cleanupResult','cleanupResidual','cleanupBefore','cleanupAfter',
            'selectionCoverage','formationCoverage','poseCoverage','door','doorNear','doorFar','screenshots','screenshotCaptureErrors','errors')) 'movement row-result record'
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
        'nonPairInterferenceCount','stopCommandIssuedCount','poseObservationCount','poseHealthyObservationCount',
        'poseFrameAppliedObservationCount','poseApplicationFrameCount','poseFootTargetClampCount','poseMaximumComponentCount',
        'poseMaximumBoneCount','walkMovingSampleCount','runMovingSampleCount','uiOverlayRepaintCountBefore',
        'uiOverlayRepaintCountAfter','uiOverlayButtonActivationCount','cameraObservationCount')) {
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
        'pauseObservationSeconds','pauseMaximumDriftWorldUnits','poseMaximumFootTargetResidualWorldUnits',
        'poseMaximumKneeTargetResidualWorldUnits','poseMaximumSegmentLengthResidualWorldUnits','poseMaximumApplyMicroseconds',
        'poseAverageApplyMicroseconds','poseMaximumPelvisLocalFrameDeltaWorldUnits','poseMaximumLeftFootLocalFrameDeltaWorldUnits',
        'poseMaximumRightFootLocalFrameDeltaWorldUnits','walkMaximumSpeedWorldUnitsPerSecond','runMaximumSpeedWorldUnitsPerSecond',
        'cameraMinimumTargetResidualWorldUnits','cameraMaximumTargetResidualWorldUnits','cameraFinalTargetResidualWorldUnits',
        'cameraMinimumRigResidualWorldUnits','cameraMaximumRigResidualWorldUnits')) {
        if (-not (Test-KmcFiniteNonnegativeJsonNumber $Record.$name)) { throw "Movement row-result $name must be a finite nonnegative JSON number." }
    }
    foreach ($name in @('unmountedDoorControlPassed','cleanupSucceeded','cleanupResidual','finalSynchronizationSnapshotCaptured',
        'finalSynchronizationQualificationPassed','finalSynchronizationMovementStoppedBeforeSnapshot',
        'finalSynchronizationBoundaryMovementCommandAbsent','finalSynchronizationBoundaryWantsToMove',
        'finalSynchronizationBoundaryIsReallyMoving','finalSynchronizationBoundaryClosureAttempted',
        'finalSynchronizationBoundaryClosureSucceeded','doorApproachSkipped','restartCompleted','selectionMountNormalized',
        'selectionSwitchedAway','selectionSwitchedBack','formationSelectionNormalized','pauseEntered','pauseExited',
        'destinationCancelCommandAbsent','destinationCancelRelationshipPreserved','uiRiderPortraitSelected',
        'uiRiderSelectionCircleSelected','uiRiderActionBarOwned','uiMountNormalized','uiAwayOwned','uiBackOwned',
        'uiOverlayRendered','uiOverlayEnabled','uiOverlayVisible','cameraFollowAccepted','cameraAwayObserved','cameraBackObserved')) {
        if ($Record.$name -isnot [bool]) { throw "Movement row-result $name must be a JSON boolean." }
    }
    foreach ($name in @('cleanupTrigger','cleanupResult','selectionCoverage','formationCoverage','poseCoverage','finalSynchronizationSnapshotStage',
        'finalSynchronizationBoundaryClosureReason')) {
        if ($Record.$name -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Record.$name)) { throw "Movement row-result $name must be a nonempty JSON string." }
    }
    if ($null -ne $Record.door -and $Record.door -isnot [string]) { throw 'Movement row-result door must be a string or null.' }
    if ($null -ne $Record.nonPairUnitId -and ($Record.nonPairUnitId -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$Record.nonPairUnitId))) {
        throw 'Movement row-result nonPairUnitId must be a nonempty string or null.'
    }
    foreach ($name in @('poseProfileId','poseBoneInventory','poseFailure','uiOverlayLabel','uiObservationFailure')) {
        if ($null -ne $Record.$name -and $Record.$name -isnot [string]) { throw "Movement row-result $name must be a JSON string or null." }
    }
    if ($Record.equipmentSets -isnot [Array]) { throw 'Movement row-result equipmentSets must be an actual JSON array.' }
    foreach ($set in @($Record.equipmentSets)) {
        Assert-KmcExactProperties $set @('index','isOriginal','isEmpty','primaryType','primaryBlueprintGuid','secondaryType','secondaryBlueprintGuid','oneHandedWeapon','twoHandedWeapon','shield','poseHealthy','poseFrameCount') 'movement equipment-set evidence'
        if (-not (Test-KmcExactJsonInteger $set.index) -or [long]$set.index -lt 0 -or
            -not (Test-KmcExactJsonInteger $set.poseFrameCount) -or [long]$set.poseFrameCount -lt 0) {
            throw 'Movement equipment-set evidence indices/counts must be nonnegative exact JSON integers.'
        }
        foreach ($name in @('isOriginal','isEmpty','oneHandedWeapon','twoHandedWeapon','shield','poseHealthy')) {
            if ($set.$name -isnot [bool]) { throw "Movement equipment-set evidence $name must be a JSON boolean." }
        }
        foreach ($name in @('primaryType','primaryBlueprintGuid','secondaryType','secondaryBlueprintGuid')) {
            if ($null -ne $set.$name -and $set.$name -isnot [string]) { throw "Movement equipment-set evidence $name must be a JSON string or null." }
        }
    }
    if ($Record.uiObservations -isnot [Array]) { throw 'Movement row-result uiObservations must be an actual JSON array.' }
    foreach ($observation in @($Record.uiObservations)) {
        Assert-KmcExactProperties $observation @('phase','expectedUnitId','isExactlySelected','actionBarSelectedUnitId','actionBarActive','actionBarOwned','portraitControllerCount','portraitSelected','selectionCircleCount','selectionCircleSelected','error') 'movement UI observation'
        foreach ($name in @('phase','expectedUnitId')) {
            if ($observation.$name -isnot [string] -or [string]::IsNullOrWhiteSpace([string]$observation.$name)) { throw "Movement UI observation $name must be a nonempty JSON string." }
        }
        foreach ($name in @('actionBarSelectedUnitId','error')) {
            if ($null -ne $observation.$name -and $observation.$name -isnot [string]) { throw "Movement UI observation $name must be a JSON string or null." }
        }
        foreach ($name in @('isExactlySelected','actionBarActive','actionBarOwned','portraitSelected','selectionCircleSelected')) {
            if ($observation.$name -isnot [bool]) { throw "Movement UI observation $name must be a JSON boolean." }
        }
        foreach ($name in @('portraitControllerCount','selectionCircleCount')) {
            if (-not (Test-KmcExactJsonInteger $observation.$name) -or [long]$observation.$name -lt 0) { throw "Movement UI observation $name must be a nonnegative exact JSON integer." }
        }
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
        $row = [string]$Record.row
        $isPresentation = $row -cin @(Get-KmcPresentationRuntimeRows)
        $isPoseIdle = $row -ceq 'pose-idle'
        $isPoseWalkRun = $row -ceq 'pose-walk-run'
        $isPoseTurnStop = $row -ceq 'pose-turn-stop'
        $isPoseDoorwayFormation = $row -ceq 'pose-doorway-formation'
        $isPoseEquipment = $row -ceq 'pose-equipment-variants'
        $isUiPresentation = $row -ceq 'ui-selection-portrait-actionbar'
        $isCameraPresentation = $row -ceq 'camera-follow-and-command-routing'
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
        $stationaryPresentationRow = @(
            'pose-idle',
            'pose-equipment-variants',
            'ui-selection-portrait-actionbar'
        ) -ccontains [string]$Record.row
        if ([long]$Record.synchronizationObservationCount -le 0 -or
            (-not $stationaryPresentationRow -and [long]$Record.updateSynchronizationSampleCount -le 0) -or
            [long]$Record.lateUpdateSynchronizationSampleCount -le 0 -or
            [long]$Record.updateSynchronizationCorrectionCount -gt [long]$Record.updateSynchronizationSampleCount -or
            [long]$Record.lateUpdateSynchronizationCorrectionCount -gt [long]$Record.lateUpdateSynchronizationSampleCount) {
            throw 'PASS movement row-result lacks bounded synchronization samples.'
        }
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
            'pose-idle' { 0L; break }
            'pose-walk-run' { 2L; break }
            'pose-turn-stop' { 3L; break }
            'pose-doorway-formation' { if ($Record.doorApproachSkipped) { 3L } else { 4L }; break }
            'pose-equipment-variants' { 0L; break }
            'ui-selection-portrait-actionbar' { 0L; break }
            'camera-follow-and-command-routing' { 1L; break }
            default { throw 'PASS movement row has no exact waypoint contract.' }
        }
        if ([long]$Record.waypointCount -ne $expectedWaypointCount) {
            throw "PASS movement row does not contain its exact $expectedWaypointCount-leg navigation proof."
        }

        $expectedEndpointQualifiedWaypointCount = switch ([string]$Record.row) {
            'mounted-pair-stop-start' { 1L; break }
            'mounted-pair-destination-cancel' { 0L; break }
            'pose-turn-stop' { 2L; break }
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

        $isDoorway = $row -ceq 'mounted-pair-doorway' -or $isPoseDoorwayFormation
        $isStopStart = $row -ceq 'mounted-pair-stop-start'
        $isTurns = $row -ceq 'mounted-pair-turns-and-corners' -or $isPoseTurnStop
        $isSelection = $row -ceq 'mounted-pair-selection'
        $isFormation = $row -ceq 'mounted-pair-party-formation' -or $isPoseDoorwayFormation
        $isPause = $row -ceq 'mounted-pair-pause-unpause'
        $isCancel = $row -ceq 'mounted-pair-destination-cancel'
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
        elseif ($isPoseTurnStop) {
            if ([long]$Record.stopCommandIssuedCount -ne 1 -or $Record.restartCompleted -ne $false) {
                throw 'PASS pose turn/stop row does not prove exactly one scoped routed stop before its turn/reversal legs.'
            }
        }
        elseif ([long]$Record.stopCommandIssuedCount -ne 0 -or $Record.restartCompleted -ne $false) {
            throw 'PASS movement row retained stop/start-only semantic evidence.'
        }
        if (-not $isCancel -and ($Record.destinationCancelCommandAbsent -ne $false -or $Record.destinationCancelRelationshipPreserved -ne $false)) {
            throw 'Non-cancel PASS row retained destination-cancel-only semantic evidence.'
        }
        $maximumTurnDegrees = [double]$Record.maximumTurnDegrees
        if ($maximumTurnDegrees -gt 180.0) {
            throw 'PASS movement row contains an impossible measured turn greater than 180 degrees.'
        }
        if ($isTurns) {
            if ($maximumTurnDegrees -lt 75.0) { throw 'PASS turns/corners row lacks the required measured 75-degree turn.' }
        }
        elseif ($isPoseWalkRun) {
            if ($maximumTurnDegrees -le 0.0) { throw 'PASS pose walk/run row lacks the measured direction change between its two deliberately divergent legs.' }
        }
        elseif ($maximumTurnDegrees -ne 0.0) {
            throw 'Non-turn PASS row retained turns/corners-only semantic evidence.'
        }
        if ($isSelection) {
            if ($Record.selectionMountNormalized -ne $true -or $Record.selectionSwitchedAway -ne $true -or
                $Record.selectionSwitchedBack -ne $true -or [string]::IsNullOrWhiteSpace([string]$Record.nonPairUnitId)) {
                throw 'PASS selection row does not prove mount normalization and the exact non-pair away/back switch.'
            }
        }
        elseif ($isCameraPresentation) {
            if ($Record.selectionMountNormalized -ne $true -or $Record.selectionSwitchedAway -ne $false -or $Record.selectionSwitchedBack -ne $false) {
                throw 'PASS camera row does not prove exact mounted-Mammoth selection normalization without mislabeling the camera switches as legacy selection-row evidence.'
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
        if (-not $isSelection -and -not $isFormation -and -not $isUiPresentation -and -not $isCameraPresentation -and $null -ne $Record.nonPairUnitId) {
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

        if ($isPresentation) {
            if ([string]$Record.poseProfileId -cne 'medium-humanoid-mammoth-v1' -or
                [string]$Record.poseBoneInventory -cne 'Pelvis,L_Up_leg,L_leg,L_foot,R_Up_leg,R_leg,R_foot' -or
                [long]$Record.poseObservationCount -le 0 -or
                [long]$Record.poseHealthyObservationCount -ne [long]$Record.poseObservationCount -or
                [long]$Record.poseFrameAppliedObservationCount -le 0 -or
                [long]$Record.poseApplicationFrameCount -le 0 -or
                [long]$Record.poseMaximumComponentCount -ne 1 -or [long]$Record.poseMaximumBoneCount -ne 7 -or
                $null -ne $Record.poseFailure) {
                throw 'PASS presentation row does not prove one healthy exact seven-bone pose profile with applied-frame telemetry.'
            }
            if ([double]$Record.poseMaximumFootTargetResidualWorldUnits -gt 0.025 -or
                [double]$Record.poseMaximumKneeTargetResidualWorldUnits -gt 0.025 -or
                [double]$Record.poseMaximumSegmentLengthResidualWorldUnits -gt 0.001 -or
                [double]$Record.poseMaximumApplyMicroseconds -gt 2000.0 -or
                [double]$Record.poseAverageApplyMicroseconds -gt 500.0) {
                throw 'PASS presentation row exceeds the fixed analytical pose-residual or frame-cost bounds.'
            }
        }

        if ($isPoseIdle -and ([double]$Record.poseMaximumPelvisLocalFrameDeltaWorldUnits -gt 0.15 -or
            [double]$Record.poseMaximumLeftFootLocalFrameDeltaWorldUnits -gt 0.15 -or
            [double]$Record.poseMaximumRightFootLocalFrameDeltaWorldUnits -gt 0.15)) {
            throw 'PASS pose-idle row exceeds the fixed gross local-frame oscillation bound.'
        }
        if ($isPoseWalkRun) {
            if ([long]$Record.walkMovingSampleCount -le 0 -or [long]$Record.runMovingSampleCount -le 0 -or
                [double]$Record.runMaximumSpeedWorldUnitsPerSecond -lt ([double]$Record.walkMaximumSpeedWorldUnitsPerSecond + 0.35)) {
                throw 'PASS pose-walk-run row lacks distinct moving samples and measured walk/run speed separation.'
            }
        }
        elseif ([long]$Record.walkMovingSampleCount -ne 0 -or [long]$Record.runMovingSampleCount -ne 0 -or
            [double]$Record.walkMaximumSpeedWorldUnitsPerSecond -ne 0.0 -or [double]$Record.runMaximumSpeedWorldUnitsPerSecond -ne 0.0) {
            throw 'Non-walk/run PASS row retained walk/run-only evidence.'
        }

        if ($isPoseEquipment) {
            if (@($Record.equipmentSets).Count -le 0 -or
                @($Record.equipmentSets | Where-Object { $_.isOriginal -eq $true }).Count -ne 1 -or
                @($Record.equipmentSets | Where-Object { $_.poseHealthy -ne $true -or [long]$_.poseFrameCount -le 0 }).Count -ne 0 -or
                @($Record.equipmentSets | Group-Object -Property index | Where-Object { $_.Count -ne 1 }).Count -ne 0) {
                throw 'PASS equipment-pose row lacks one exact original set, unique bounded set identities, or healthy applied pose frames.'
            }
        }
        elseif (@($Record.equipmentSets).Count -ne 0) {
            throw 'Non-equipment PASS row retained equipment-only evidence.'
        }

        if ($isUiPresentation) {
            [string[]]$expectedUiPhases = @('rider-selected','mount-selection-normalized-to-rider','selection-away','selection-back')
            [string[]]$actualUiPhases = @($Record.uiObservations | ForEach-Object { [string]$_.phase })
            if ($actualUiPhases.Count -ne $expectedUiPhases.Count -or
                ($actualUiPhases -join "`n") -cne ($expectedUiPhases -join "`n") -or
                @($Record.uiObservations | Where-Object {
                    $_.isExactlySelected -ne $true -or $_.actionBarActive -ne $true -or $_.actionBarOwned -ne $true -or
                    $_.portraitSelected -ne $true -or [long]$_.portraitControllerCount -le 0 -or
                    $_.selectionCircleSelected -ne $true -or [long]$_.selectionCircleCount -le 0 -or $null -ne $_.error
                }).Count -ne 0 -or
                $Record.uiRiderPortraitSelected -ne $true -or $Record.uiRiderSelectionCircleSelected -ne $true -or
                $Record.uiRiderActionBarOwned -ne $true -or $Record.uiMountNormalized -ne $true -or
                $Record.uiAwayOwned -ne $true -or $Record.uiBackOwned -ne $true -or $Record.uiOverlayRendered -ne $true -or
                [long]$Record.uiOverlayRepaintCountAfter -le [long]$Record.uiOverlayRepaintCountBefore -or
                [string]$Record.uiOverlayLabel -cne 'Dismount' -or $Record.uiOverlayEnabled -ne $true -or
                $Record.uiOverlayVisible -ne $true -or [long]$Record.uiOverlayButtonActivationCount -ne 0 -or
                $null -ne $Record.uiObservationFailure) {
                throw 'PASS UI presentation row lacks exact rider/mount-normalized/away/back ownership or actual repaint evidence.'
            }
        }
        elseif (@($Record.uiObservations).Count -ne 0 -or $Record.uiRiderPortraitSelected -ne $false -or
            $Record.uiRiderSelectionCircleSelected -ne $false -or $Record.uiRiderActionBarOwned -ne $false -or
            $Record.uiMountNormalized -ne $false -or $Record.uiAwayOwned -ne $false -or $Record.uiBackOwned -ne $false -or
            $Record.uiOverlayRendered -ne $false -or $null -ne $Record.uiObservationFailure) {
            throw 'Non-UI PASS row retained UI-ownership-only evidence.'
        }

        if ($isCameraPresentation) {
            if ($Record.cameraFollowAccepted -ne $true -or [long]$Record.cameraObservationCount -le 0 -or
                [double]$Record.cameraMinimumTargetResidualWorldUnits -gt [double]$Record.cameraMaximumTargetResidualWorldUnits -or
                [double]$Record.cameraMaximumTargetResidualWorldUnits -gt 1.50 -or
                [double]$Record.cameraFinalTargetResidualWorldUnits -gt 0.50 -or
                [double]$Record.cameraMinimumRigResidualWorldUnits -gt [double]$Record.cameraMaximumRigResidualWorldUnits -or
                $Record.cameraAwayObserved -ne $true -or $Record.cameraBackObserved -ne $true) {
                throw 'PASS camera presentation row lacks bounded native moving/away/back follow evidence.'
            }
        }
        elseif ($Record.cameraFollowAccepted -ne $false -or [long]$Record.cameraObservationCount -ne 0 -or
            $Record.cameraAwayObserved -ne $false -or $Record.cameraBackObserved -ne $false) {
            throw 'Non-camera PASS row retained camera-follow-only evidence.'
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
            'pose-idle' { @('mounted-idle','pose-idle','dismounted'); break }
            'pose-walk-run' { @('mounted-idle','pose-walk','pose-stopped','pose-run','dismounted'); break }
            'pose-turn-stop' { @('mounted-idle','pose-stop-motion','pose-stopped','pose-turn','pose-reversal','pose-stopped','dismounted'); break }
            'pose-doorway-formation' {
                if ($Record.doorApproachSkipped) { @('door-control','door-mounted','door-mounted','formation','formation','dismounted') }
                else { @('door-control','door-control','door-mounted','door-mounted','formation','formation','dismounted') }
                break
            }
            'pose-equipment-variants' {
                @('mounted-idle') + @(0..(@($Record.equipmentSets).Count - 1) | ForEach-Object { 'pose-equipment' }) + @('dismounted')
                break
            }
            'ui-selection-portrait-actionbar' { @('mounted-idle','ui-rider','ui-mount-normalized','ui-away','ui-back','dismounted'); break }
            'camera-follow-and-command-routing' { @('mounted-idle','camera-moving','camera-away','camera-back','dismounted'); break }
        }
        [string[]]$actualScreenshotMilestones = @($Record.screenshots | ForEach-Object { [string]$_.milestone })
        if ($actualScreenshotMilestones.Count -ne $expectedScreenshotMilestones.Count -or
            ($actualScreenshotMilestones -join "`n") -cne ($expectedScreenshotMilestones -join "`n") -or
            @($Record.screenshotCaptureErrors).Count -ne 0) {
            throw 'PASS movement row lacks its exact ordered screenshot milestone/count coverage or contains a capture error.'
        }
        $screenshotMilestoneCounts = @{}
        $screenshotRowToken = if ($row.StartsWith('mounted-pair-', [StringComparison]::Ordinal)) {
            $row.Substring('mounted-pair-'.Length)
        }
        else { $row }
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

    [string[]]$expectedRows = if ([string]$Request.scenario -ceq 'movement-suite') {
        @(Get-KmcMovementRuntimeRows)
    } elseif ([string]$Request.scenario -ceq 'presentation-suite') {
        @(Get-KmcPresentationRuntimeRows)
    } else { @([string]$Request.scenario) }
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
            $rowRecord = $rowResults[$index]
            if ([string]$rowRecord.row -cne [string]$expectedRows[$index] -or
                -not $telemetryRows.Contains([string]$expectedRows[$index]) -or
                ([long]$rowRecord.waypointCount -gt 0 -and -not $pathProbeRows.Contains([string]$expectedRows[$index]))) {
                throw "PASS movement evidence lacks exact ordered row, telemetry, or required path-probe coverage for $($expectedRows[$index])."
            }
            $rowPathProbes = @($scenarioRecords | Where-Object { [string]$_.kind -ceq 'path-probe' -and [string]$_.row -ceq [string]$rowRecord.row })
            if ($rowPathProbes.Count -ne [long]$rowRecord.waypointCount) {
                throw "PASS movement evidence path-probe count does not equal the exact completed waypoint count for $($rowRecord.row)."
            }
            if ([string]$rowRecord.row -cin @('mounted-pair-doorway','pose-doorway-formation')) {
                $isPresentationDoorway = [string]$rowRecord.row -ceq 'pose-doorway-formation'
                [bool[]]$expectedStrictDoor = if ($rowRecord.doorApproachSkipped) {
                    if ($isPresentationDoorway) { @($true,$true,$false) } else { @($true,$true) }
                } else {
                    if ($isPresentationDoorway) { @($false,$true,$true,$false) } else { @($false,$true,$true) }
                }
                $expectedDoorTargets = if ($rowRecord.doorApproachSkipped) {
                    @($rowRecord.doorFar,$rowRecord.doorNear)
                }
                else {
                    @($rowRecord.doorNear,$rowRecord.doorFar,$rowRecord.doorNear)
                }
                if ($rowPathProbes.Count -ne $expectedStrictDoor.Count) { throw 'PASS doorway path-probe count does not match its bounded approach policy.' }
                for ($probeIndex = 0; $probeIndex -lt $rowPathProbes.Count; $probeIndex++) {
                    $probe = $rowPathProbes[$probeIndex]
                    $isFormationProbe = $isPresentationDoorway -and $probeIndex -eq ($rowPathProbes.Count - 1)
                    $target = if ($isFormationProbe) { $null } else { $expectedDoorTargets[$probeIndex] }
                    if ($probe.strictDoor -ne $expectedStrictDoor[$probeIndex] -or (-not $isFormationProbe -and (
                        -not (Test-KmcApproximatelyEqual ([double]$probe.requested.x) ([double]$target.x) 0.000001) -or
                        -not (Test-KmcApproximatelyEqual ([double]$probe.requested.y) ([double]$target.y) 0.000001) -or
                        -not (Test-KmcApproximatelyEqual ([double]$probe.requested.z) ([double]$target.z) 0.000001)))) {
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

function Assert-KmcCombatScenarioEvidence {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$Manifest,
        [AllowNull()][string]$Status,
        $SubscenarioResults
    )

    $isCombat = Test-KmcCombatRuntimeScenario ([string]$Request.scenario)
    $artifacts = @($Manifest.artifacts | Where-Object { [string]$_.relativePath -ceq 'combat-scenario-evidence.jsonl' })
    if (-not $isCombat) {
        if ($artifacts.Count -ne 0) { throw 'Combat evidence is present for a non-combat runtime scenario.' }
        return
    }
    if ($artifacts.Count -ne 1 -or [string]$artifacts[0].kind -cne 'combat-evidence') {
        throw 'Combat runtime scenario requires exactly one combat-evidence JSONL artifact.'
    }

    $evidenceRoot = [IO.Path]::GetFullPath([string]$Request.evidenceRoot).TrimEnd('\')
    $path = Assert-KmcChildPath (Join-Path $evidenceRoot 'combat-scenario-evidence.jsonl') $evidenceRoot 'combat scenario evidence'
    Assert-KmcNotReparsePoint $path 'combat scenario evidence'
    Assert-KmcNotHardLink $path 'combat scenario evidence'
    [string[]]$lines = @(Get-Content -LiteralPath $path | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($lines.Count -ne 1) { throw 'Combat scenario evidence must contain exactly one bounded row record.' }
    Assert-KmcJsonObjectMembersUnique $lines[0] 'combat scenario evidence line'
    try { $record = $lines[0] | ConvertFrom-Json }
    catch { throw "Combat scenario evidence line is malformed JSON: $($_.Exception.Message)" }

    $recordFields = @(
        'schemaVersion','artifactKind','runId','scenario','row','rowIndex','sequence','frame','utcTimestamp',
        'branch','commit','productVersion','dllSha256','dllMvid','status','mode','action','expectedActor',
        'riderId','mountId','targetId','targetProvisioning','clickAccepted','pairApproachRadius','targetDistanceAtClick',
        'riderPositionAtClick','mountPositionAtClick','targetPositionAtClick','resources','command','rules',
        'movement','pose','cleanup','selection','assertionPassCount','assertionFailCount','errors')
    $combatSchemaVersionIsExact = Test-KmcExactJsonInteger $record.schemaVersion
    if ($combatSchemaVersionIsExact -and [long]$record.schemaVersion -ge 2) {
        $recordFields = @($recordFields + 'dispatch')
    }
    if ($combatSchemaVersionIsExact -and [long]$record.schemaVersion -eq 3) {
        $recordFields = @($recordFields + 'combatEntry')
    }
    Assert-KmcExactProperties $record $recordFields 'combat evidence record'
    if (-not (Test-KmcExactJsonInteger $record.schemaVersion) -or [long]$record.schemaVersion -notin @(1,2,3) -or
        [string]$record.artifactKind -cne 'combat-scenario-evidence') {
        throw 'Combat evidence schemaVersion or artifactKind is not exact.'
    }
    foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid')) {
        if ($record.$name -isnot [string] -or [string]$record.$name -cne [string]$Request.$name) {
            throw "Combat evidence identity mismatch: $name"
        }
    }
    if ([string]$record.row -cne [string]$Request.scenario -or
        @(Get-KmcCombatRuntimeRows | Where-Object { $_ -ceq [string]$record.row }).Count -ne 1 -or
        -not (Test-KmcExactJsonInteger $record.rowIndex) -or [long]$record.rowIndex -ne 0 -or
        -not (Test-KmcExactJsonInteger $record.sequence) -or [long]$record.sequence -ne 0 -or
        -not (Test-KmcExactJsonInteger $record.frame) -or [long]$record.frame -le 0) {
        throw 'Combat evidence row, sequence, or frame identity is invalid.'
    }
    $timestamp = [DateTimeOffset]::MinValue
    if ($record.utcTimestamp -isnot [string] -or
        -not [DateTimeOffset]::TryParse([string]$record.utcTimestamp, [ref]$timestamp) -or
        $timestamp.Offset -ne [TimeSpan]::Zero) {
        throw 'Combat evidence timestamp is not exact UTC.'
    }
    if ([string]$record.status -cnotin @('PASS','FAIL') -or
        -not (Test-KmcExactJsonInteger $record.assertionPassCount) -or
        -not (Test-KmcExactJsonInteger $record.assertionFailCount) -or
        [long]$record.assertionPassCount -lt 0 -or [long]$record.assertionFailCount -lt 0 -or
        ([long]$record.assertionPassCount + [long]$record.assertionFailCount) -le 0 -or
        $null -eq $record.errors -or $record.errors -is [string] -or
        $null -eq $record.selection -or $record.selection -is [string]) {
        throw 'Combat evidence status, assertion totals, errors, or selection shape is invalid.'
    }

    foreach ($positionName in @('riderPositionAtClick','mountPositionAtClick','targetPositionAtClick')) {
        Assert-KmcExactProperties $record.$positionName @('x','y','z') "combat $positionName"
        foreach ($axis in @('x','y','z')) {
            if (-not (Test-KmcJsonNumber $record.$positionName.$axis)) { throw "Combat $positionName.$axis is not numeric." }
        }
    }
    if ([long]$record.schemaVersion -ge 2) {
        Assert-KmcExactProperties $record.dispatch @(
            'originalPaused','unpausedForRealTime','pausedAtClick','riderCanActInCombat','riderHandsBusy',
            'equipmentControllerAvailable','equipmentUpdateScheduled','pauseRestored') 'combat dispatch evidence'
        foreach ($name in $record.dispatch.PSObject.Properties.Name) {
            if ($record.dispatch.$name -isnot [bool]) { throw "Combat dispatch evidence is not Boolean: $name" }
        }
    }
    if ([long]$record.schemaVersion -eq 3) {
        $combatEntryBooleanFields = @(
            'memoryQueued','playerGroupMemoryContainsTarget','targetGroupMemoryContainsRider',
            'riderInCombat','mountInCombat','targetInCombat','playerInCombat','riderPrepared','riderAwake',
            'defaultGameMode','memoryRemovedAtCleanup')
        Assert-KmcExactProperties $record.combatEntry @($combatEntryBooleanFields + @('riderInitiative','gameDeltaTime')) 'combat entry evidence'
        foreach ($name in $combatEntryBooleanFields) {
            if ($record.combatEntry.$name -isnot [bool]) { throw "Combat entry evidence is not Boolean: $name" }
        }
        foreach ($name in @('riderInitiative','gameDeltaTime')) {
            if (-not (Test-KmcJsonNumber $record.combatEntry.$name)) { throw "Combat entry evidence is not numeric: $name" }
        }
    }
    Assert-KmcExactProperties $record.resources @(
        'riderStandardBefore','riderStandardAfter','riderMoveBefore','riderMoveAfter',
        'mountStandardBefore','mountStandardAfter','mountMoveBefore','mountMoveAfter') 'combat resource evidence'
    foreach ($name in $record.resources.PSObject.Properties.Name) {
        if (-not (Test-KmcJsonNumber $record.resources.$name)) { throw "Combat resource evidence is not numeric: $name" }
    }
    Assert-KmcExactProperties $record.movement @(
        'authoritativeMover','repathCount','riderStockAgentEnabledAtEnd','mountStockAgentEnabledAtEnd',
        'riderAvoidanceDisabledAtEnd','mountAvoidanceDisabledAtEnd') 'combat movement evidence'
    Assert-KmcExactProperties $record.targetProvisioning @(
        'targetBlueprintId','runtimeGroupId','blueprintEmptyHandWeaponBlueprintId','targetNativeSingleAttackWeaponBlueprintId',
        'targetNativeSingleAttackSlot','targetPrimaryMainAttacks','targetSecondaryMainAttacks',
        'additionalLimbCountBefore','additionalLimbCountAfter','noWeaponProvisioningMutation','noLoot','rawAiDisabled',
        'targetPrimaryHandHasItem','targetWeaponUsesEmptyHandFallback',
        'targetNativeSingleAttackWeaponIsNatural','targetNativeSingleAttackWeaponIsMelee',
        'bidirectionalHostility','noExperienceReward') 'combat target provisioning evidence'
    Assert-KmcExactProperties $record.pose @(
        'profileId','healthyAtOutcome','configuredAtEnd','attachmentLeaseAtEnd','residueAtEnd') 'combat pose evidence'
    Assert-KmcExactProperties $record.cleanup @(
        'targetRemoved','targetEntityRemoved','runtimeGroupRemoved','runtimeFactionRemoved',
        'relationshipClean','combatCleared','relationshipState','residualState','presentationResidual') 'combat cleanup evidence'

    $requirePass = [string]$Status -ceq 'PASS'
    if ($requirePass) {
        if ([long]$record.schemaVersion -ne 3 -or
            [string]$record.status -cne 'PASS' -or [long]$record.assertionFailCount -ne 0 -or
            [long]$record.assertionPassCount -le 0 -or @($record.errors).Count -ne 0) {
            throw 'PASS combat evidence does not contain an error-free schema-v3 PASS row.'
        }
        if ([string]$record.row -cne 'mounted-rider-melee-hit-rt' -or
            [string]$record.mode -cne 'real-time' -or [string]$record.action -cne 'RiderMelee' -or
            [string]$record.expectedActor -cne 'rider' -or $record.clickAccepted -ne $true) {
            throw 'PASS combat evidence does not prove the exact real-time rider action path.'
        }
        if ($record.dispatch.originalPaused -isnot [bool] -or
            $record.dispatch.unpausedForRealTime -ne $true -or
            $record.dispatch.pausedAtClick -ne $false -or
            $record.dispatch.riderCanActInCombat -ne $true -or
            $record.dispatch.riderHandsBusy -ne $false -or
            $record.dispatch.equipmentControllerAvailable -ne $true -or
            $record.dispatch.equipmentUpdateScheduled -ne $false -or
            $record.dispatch.pauseRestored -ne $true) {
            throw 'PASS combat evidence does not prove an unpaused native-ready dispatch and exact pause restoration.'
        }
        if ($record.combatEntry.memoryQueued -ne $true -or
            $record.combatEntry.playerGroupMemoryContainsTarget -ne $true -or
            $record.combatEntry.targetGroupMemoryContainsRider -ne $true -or
            $record.combatEntry.riderInCombat -ne $true -or
            $record.combatEntry.mountInCombat -ne $true -or
            $record.combatEntry.targetInCombat -ne $true -or
            $record.combatEntry.playerInCombat -ne $true -or
            $record.combatEntry.riderPrepared -ne $true -or
            $record.combatEntry.riderAwake -ne $true -or
            $record.combatEntry.defaultGameMode -ne $true -or
            [Math]::Abs([double]$record.combatEntry.riderInitiative) -gt 0.000001 -or
            [double]$record.combatEntry.gameDeltaTime -le 0 -or
            $record.combatEntry.memoryRemovedAtCleanup -ne $true) {
            throw 'PASS combat evidence does not prove native bidirectional memory, combat preparation, live Default-mode time, and memory cleanup.'
        }
        if ([string]::IsNullOrWhiteSpace([string]$record.riderId) -or
            [string]::IsNullOrWhiteSpace([string]$record.mountId) -or
            [string]::IsNullOrWhiteSpace([string]$record.targetId) -or
            [string]$record.riderId -ceq [string]$record.mountId -or
            [string]$record.riderId -ceq [string]$record.targetId -or
            [string]$record.mountId -ceq [string]$record.targetId) {
            throw 'PASS combat evidence actor and target identities are missing or not distinct.'
        }
        if ([string]$record.targetProvisioning.targetBlueprintId -cne 'e7aa96d15a45238438ae4cfb476f6bb9' -or
            [string]$record.targetProvisioning.runtimeGroupId -cne ('KMC.RuntimeHostile.' + [string]$Request.runId) -or
            [string]$record.targetProvisioning.blueprintEmptyHandWeaponBlueprintId -cnotmatch '^[0-9a-f]{32}$' -or
            [string]$record.targetProvisioning.targetNativeSingleAttackWeaponBlueprintId -cnotmatch '^[0-9a-f]{32}$' -or
            [string]$record.targetProvisioning.targetNativeSingleAttackSlot -cne 'PrimaryHand' -or
            (($record.targetProvisioning.targetPrimaryMainAttacks -isnot [int]) -and ($record.targetProvisioning.targetPrimaryMainAttacks -isnot [long])) -or
            (($record.targetProvisioning.targetSecondaryMainAttacks -isnot [int]) -and ($record.targetProvisioning.targetSecondaryMainAttacks -isnot [long])) -or
            (($record.targetProvisioning.additionalLimbCountBefore -isnot [int]) -and ($record.targetProvisioning.additionalLimbCountBefore -isnot [long])) -or
            (($record.targetProvisioning.additionalLimbCountAfter -isnot [int]) -and ($record.targetProvisioning.additionalLimbCountAfter -isnot [long])) -or
            [int]$record.targetProvisioning.targetPrimaryMainAttacks -lt 1 -or
            [int]$record.targetProvisioning.targetSecondaryMainAttacks -lt 0 -or
            [int]$record.targetProvisioning.additionalLimbCountBefore -ne [int]$record.targetProvisioning.additionalLimbCountAfter -or
            $record.targetProvisioning.noWeaponProvisioningMutation -ne $true -or
            $record.targetProvisioning.targetPrimaryHandHasItem -isnot [bool] -or
            $record.targetProvisioning.targetWeaponUsesEmptyHandFallback -isnot [bool] -or
            [bool]$record.targetProvisioning.targetPrimaryHandHasItem -eq [bool]$record.targetProvisioning.targetWeaponUsesEmptyHandFallback -or
            ([bool]$record.targetProvisioning.targetWeaponUsesEmptyHandFallback -and
                [string]$record.targetProvisioning.targetNativeSingleAttackWeaponBlueprintId -cne [string]$record.targetProvisioning.blueprintEmptyHandWeaponBlueprintId) -or
            $record.targetProvisioning.targetNativeSingleAttackWeaponIsNatural -ne $true -or
            $record.targetProvisioning.targetNativeSingleAttackWeaponIsMelee -ne $true -or
            $record.targetProvisioning.noLoot -ne $true -or $record.targetProvisioning.rawAiDisabled -ne $true -or
            $record.targetProvisioning.bidirectionalHostility -ne $true -or
            $record.targetProvisioning.noExperienceReward -ne $true) {
            throw 'PASS combat target provisioning evidence does not prove exact native primary-hand selection without mutation.'
        }
        if (-not (Test-KmcJsonNumber $record.pairApproachRadius) -or
            -not (Test-KmcJsonNumber $record.targetDistanceAtClick) -or
            [double]$record.pairApproachRadius -le 0.17 -or [double]$record.targetDistanceAtClick -le 0.05 -or
            [Math]::Abs([double]$record.targetDistanceAtClick - ([double]$record.pairApproachRadius - 0.12)) -gt 0.060001 -or
            [double]$record.targetDistanceAtClick -gt ([double]$record.pairApproachRadius + 0.05)) {
            throw 'PASS combat evidence does not prove the exact bounded mounted range contract.'
        }

        Assert-KmcExactProperties $record.command @(
            'action','actorId','targetId','result','childAttackStartCount','repathCount',
            'riderStandardCharged','nativeAttackRuleObserved') 'combat command evidence'
        if ([string]$record.command.action -cne 'RiderMelee' -or
            [string]$record.command.actorId -cne [string]$record.riderId -or
            [string]$record.command.targetId -cne [string]$record.targetId -or
            [string]$record.command.result -cne 'Success' -or
            [long]$record.command.childAttackStartCount -ne 1 -or [long]$record.command.repathCount -ne 0 -or
            $record.command.riderStandardCharged -ne $true -or $record.command.nativeAttackRuleObserved -ne $true) {
            throw 'PASS combat command evidence does not prove one successful native rider attack.'
        }

        Assert-KmcExactProperties $record.rules @(
            'forcedD20','forcedD20Count','attackRuleCount','attackRollCount','damageRuleCount',
            'unexpectedPairAttackCount','totalDamage','lastInitiatorId','lastTargetId','lastAttackResult') 'combat rule evidence'
        if ([long]$record.rules.forcedD20 -ne 20 -or [long]$record.rules.forcedD20Count -lt 1 -or
            [long]$record.rules.attackRuleCount -ne 1 -or [long]$record.rules.attackRollCount -ne 1 -or
            [long]$record.rules.damageRuleCount -lt 0 -or [long]$record.rules.damageRuleCount -gt 1 -or
            [long]$record.rules.unexpectedPairAttackCount -ne 0 -or
            [string]$record.rules.lastInitiatorId -cne [string]$record.riderId -or
            [string]$record.rules.lastTargetId -cne [string]$record.targetId -or
            [string]$record.rules.lastAttackResult -cnotin @('Hit','CriticalHit')) {
            throw 'PASS combat rule evidence does not prove one deterministic rider hit without duplication.'
        }

        if ([double]$record.resources.riderStandardAfter -le [double]$record.resources.riderStandardBefore -or
            [math]::Abs([double]$record.resources.riderMoveAfter - [double]$record.resources.riderMoveBefore) -gt 0.01 -or
            [math]::Abs([double]$record.resources.mountStandardAfter - [double]$record.resources.mountStandardBefore) -gt 0.01 -or
            [math]::Abs([double]$record.resources.mountMoveAfter - [double]$record.resources.mountMoveBefore) -gt 0.01) {
            throw 'PASS combat resource evidence does not prove rider-only Standard action charging.'
        }
        if ([string]$record.movement.authoritativeMover -cne 'mount' -or [long]$record.movement.repathCount -ne 0 -or
            $record.movement.riderStockAgentEnabledAtEnd -ne $true -or
            $record.movement.mountStockAgentEnabledAtEnd -ne $true -or
            $record.movement.riderAvoidanceDisabledAtEnd -ne $false -or
            $record.movement.mountAvoidanceDisabledAtEnd -ne $false) {
            throw 'PASS combat movement evidence does not prove stationary mount authority and exact baseline restoration.'
        }
        if ([string]$record.pose.profileId -cne 'medium-humanoid-mammoth-v1' -or
            $record.pose.healthyAtOutcome -ne $true -or $record.pose.configuredAtEnd -ne $false -or
            $record.pose.attachmentLeaseAtEnd -ne $false -or $record.pose.residueAtEnd -ne $false) {
            throw 'PASS combat pose evidence does not retain the accepted Mammoth profile through outcome and cleanup.'
        }
        if ($record.cleanup.targetRemoved -ne $true -or $record.cleanup.targetEntityRemoved -ne $true -or
            $record.cleanup.runtimeGroupRemoved -ne $true -or $record.cleanup.runtimeFactionRemoved -ne $true -or
            $record.cleanup.relationshipClean -ne $true -or
            $record.cleanup.combatCleared -ne $true -or [string]$record.cleanup.relationshipState -cne 'Unmounted' -or
            $record.cleanup.residualState -ne $false -or $record.cleanup.presentationResidual -ne $false) {
            throw 'PASS combat cleanup evidence is not exact and residue-free.'
        }
    }

    if ($null -ne $SubscenarioResults) {
        $subresults = @($SubscenarioResults)
        $matches = @($subresults | Where-Object { [string]$_.name -ceq [string]$record.row })
        if ($matches.Count -ne 1) { throw 'Combat evidence does not map to exactly one game subresult.' }
        $subresult = $matches[0]
        if ([string]$record.status -cne [string]$subresult.status -or
            [long]$record.assertionPassCount -ne [long]$subresult.assertionPassCount -or
            [long]$record.assertionFailCount -ne [long]$subresult.assertionFailCount -or
            (@($record.errors) -join "`n") -cne (@($subresult.errors) -join "`n")) {
            throw 'Combat evidence does not reconcile with its game subresult.'
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
            ($relative -ceq 'boundary-scenario-evidence.jsonl' -and $kind -ceq 'boundary-evidence') -or
            ($relative -ceq 'combat-scenario-evidence.jsonl' -and $kind -ceq 'combat-evidence') -or
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
    Assert-KmcBoundaryScenarioEvidence -Request $Request -Manifest $manifestValue
    Assert-KmcCombatScenarioEvidence -Request $Request -Manifest $manifestValue
    $hash = Get-KmcSha256 $manifestPath
    $after = Get-Item -LiteralPath $manifestPath -Force
    if ($after.Length -ne $before.Length -or $after.LastWriteTimeUtc.Ticks -ne $before.LastWriteTimeUtc.Ticks) {
        throw 'Orchestration artifact manifest changed while being validated.'
    }
    return $hash
}

function New-KmcRuntimeFixturePayload {
    param(
        [Parameter(Mandatory = $true)]$Pair,
        [switch]$ReadOnly
    )

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
            mode = if ($ReadOnly) { 'read-only' } else { 'working-only' }
            allowedInternalName = if ($ReadOnly) { $null } else { 'KMC_AUTOMATION_WORKING' }
            allowedFileName = if ($ReadOnly) { $null } else { [string]$Pair.working.fileName }
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
            [string]$Request.scenario -notin @('fixture-intake','lifecycle-suite','movement-suite','boundary-suite','presentation-suite')) {
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

. (Join-Path $PSScriptRoot 'ProtectedSaveContinuityV2.ps1')
