Set-StrictMode -Version Latest

$script:KmcMetadataOnlyPriorHashStatus = 'UNAVAILABLE-SCHEMA-V1-METADATA-ONLY'
$script:KmcKnownPriorHashStatus = 'AVAILABLE-PARENT-CONTENT-PIN'
$script:KmcExternalTransitionReason = 'explicit user-attested external Kingmaker activity'
$script:KmcMetadataOnlyExceptionPath = 'Quick_3.zks'

function Get-KmcProtectedSaveAuthorityChangedPaths {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )
    $beforeMap = @{}
    foreach ($entry in @($Before.entries)) { $beforeMap[[string]$entry.path] = $entry }
    $afterMap = @{}
    foreach ($entry in @($After.entries)) { $afterMap[[string]$entry.path] = $entry }
    $paths = @($beforeMap.Keys + $afterMap.Keys | Sort-Object -Unique)
    return @($paths | Where-Object {
        $prior = $beforeMap[$_]
        $current = $afterMap[$_]
        $null -eq $prior -or $null -eq $current -or
            [string]$prior.kind -cne [string]$current.kind -or
            [string]$prior.path -cne [string]$current.path -or
            [long]$prior.length -ne [long]$current.length -or
            [long]$prior.lastWriteTimeUtcTicks -ne [long]$current.lastWriteTimeUtcTicks
    })
}

function Get-KmcProtectedSavePinSetSha256 {
    param([Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Pins)
    $previousPath = $null
    $canonical = New-Object 'Collections.Generic.List[string]'
    foreach ($pin in @($Pins)) {
        if ($pin -isnot [pscustomobject]) { throw 'Protected-save pin set contains a non-object entry.' }
        Assert-KmcExactProperties $pin @('path','length','lastWriteTimeUtcTicks','sha256','sourceEpochId') 'protected-save pin'
        if ($pin.path -isnot [string] -or
            (($pin.length -isnot [int]) -and ($pin.length -isnot [long])) -or
            (($pin.lastWriteTimeUtcTicks -isnot [int]) -and ($pin.lastWriteTimeUtcTicks -isnot [long])) -or
            $pin.sha256 -isnot [string] -or $pin.sourceEpochId -isnot [string]) {
            throw 'Protected-save pin set contains invalid schema types.'
        }
        $path = [string]$pin.path
        if ([string]::IsNullOrWhiteSpace($path) -or [IO.Path]::IsPathRooted($path) -or
            $path.Contains('/') -or $path.Contains('\') -or [IO.Path]::GetFileName($path) -cne $path -or
            [long]$pin.length -le 0 -or [long]$pin.lastWriteTimeUtcTicks -le 0 -or
            [string]$pin.sha256 -cnotmatch '^[0-9a-f]{64}$' -or
            [string]$pin.sourceEpochId -cnotmatch '^[A-Za-z0-9._-]{1,120}$') {
            throw "Protected-save pin set contains an invalid identity: $path"
        }
        if ($null -ne $previousPath -and [string]::CompareOrdinal($previousPath, $path) -ge 0) {
            throw 'Protected-save pin set paths are not in strict ordinal order.'
        }
        $canonical.Add(('{0}|{1}|{2}|{3}|{4}' -f
            $path, [long]$pin.length, [long]$pin.lastWriteTimeUtcTicks,
            [string]$pin.sha256, [string]$pin.sourceEpochId))
        $previousPath = $path
    }
    return Get-KmcTextSha256 ($canonical -join "`n")
}

function Read-KmcPinnedProtectedSaveAuthorityJson {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedEpochId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedAuthoritySha256,
        [Parameter(Mandatory = $true)][string]$Description
    )
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $authorityRoot = Assert-KmcChildPath (Join-Path $fullStateRoot 'protected-save-authorities') $fullStateRoot 'protected-save authority root'
    if (-not (Test-Path -LiteralPath $authorityRoot -PathType Container)) { throw 'Protected-save authority root is missing.' }
    Assert-KmcNotReparsePoint $authorityRoot 'protected-save authority root'
    $expectedPath = Assert-KmcChildPath (Join-Path $authorityRoot ($ExpectedEpochId + '.json')) $authorityRoot $Description
    $fullPath = [IO.Path]::GetFullPath($Path)
    if (-not [string]::Equals($fullPath, $expectedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description path differs from the caller-pinned epoch identity."
    }
    Assert-KmcRecoveryLeafNoLinks $fullPath $Description
    $before = Get-Item -LiteralPath $fullPath -Force
    if ($before.Length -le 0 -or $before.Length -gt 2MB -or (Get-KmcSha256 $fullPath) -cne $ExpectedAuthoritySha256) {
        throw "$Description size or SHA-256 differs from the explicit pin."
    }
    $bytes = [IO.File]::ReadAllBytes($fullPath)
    if ($bytes.Length -ne $before.Length) { throw "$Description length changed while its bytes were captured." }
    $strictUtf8 = New-Object Text.UTF8Encoding($false, $true)
    try { $json = $strictUtf8.GetString($bytes) }
    catch [Text.DecoderFallbackException] { throw "$Description is not strict UTF-8." }
    Assert-KmcJsonObjectMembersUnique -Json $json -Description $Description
    $record = $json | ConvertFrom-Json
    $after = Get-Item -LiteralPath $fullPath -Force
    if ($after.Length -ne $before.Length -or $after.LastWriteTimeUtc.Ticks -ne $before.LastWriteTimeUtc.Ticks -or
        (Get-KmcSha256 $fullPath) -cne $ExpectedAuthoritySha256) {
        throw "$Description changed while it was being read."
    }
    return [pscustomobject]@{
        path = $fullPath
        sha256 = $ExpectedAuthoritySha256
        metadata = [pscustomobject]@{ length=[long]$before.Length;lastWriteTimeUtcTicks=[long]$before.LastWriteTimeUtc.Ticks }
        record = $record
    }
}

function Read-KmcHistoricalProtectedSaveContinuityAuthorityV1 {
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
        [Parameter(Mandatory = $true)]$CurrentPair
    )
    $pinned = Read-KmcPinnedProtectedSaveAuthorityJson `
        -Path $Path -StateRoot $StateRoot -ExpectedEpochId $ExpectedEpochId `
        -ExpectedAuthoritySha256 $ExpectedAuthoritySha256 -Description 'historical schema-v1 protected-save authority'
    $record = $pinned.record
    if ($record -isnot [pscustomobject]) { throw 'Historical schema-v1 authority is not an exact JSON object.' }
    Assert-KmcExactProperties $record @(
        'schemaVersion','authorityKind','epochId','authorizedAtUtc','attestationScope','saveRoot','priorAuthority',
        'currentQualification','baseline','working','writableSaveNames','authorizedProtectedTransitions','currentInventory'
    ) 'historical schema-v1 protected-save authority'
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
        throw 'Historical schema-v1 authority top-level schema or types are invalid.'
    }
    $authorizedAt = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParseExact(
        [string]$record.authorizedAtUtc, 'o', [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind, [ref]$authorizedAt)) {
        throw 'Historical schema-v1 authority timestamp is not exact round-trip form.'
    }
    Assert-KmcExactProperties $record.priorAuthority @(
        'statePath','runId','stateSha256','inventoryDigest','baselineSha256','supersededWorkingSha256'
    ) 'historical schema-v1 authority prior source'
    Assert-KmcExactProperties $record.currentQualification @('path','sha256') 'historical schema-v1 authority qualification'
    foreach ($name in @('baseline','working')) {
        Assert-KmcExactProperties $record.$name @('path','fileName','sha256','length','lastWriteTimeUtcTicks') "historical schema-v1 authority $name"
    }
    if (@($record.writableSaveNames).Count -ne 1 -or [string]@($record.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING' -or
        @($record.authorizedProtectedTransitions).Count -ne 2) {
        throw 'Historical schema-v1 authority allowlist or transition count is invalid.'
    }
    foreach ($container in @($record.priorAuthority,$record.currentQualification,$record.baseline,$record.working)) {
        foreach ($property in @($container.PSObject.Properties)) {
            if ($property.Name -in @('length','lastWriteTimeUtcTicks')) {
                if (($property.Value -isnot [int]) -and ($property.Value -isnot [long])) { throw 'Historical schema-v1 integral identity type is invalid.' }
            }
            elseif ($property.Value -isnot [string]) { throw 'Historical schema-v1 string identity type is invalid.' }
        }
    }
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    if (-not [string]::Equals([IO.Path]::GetFullPath([string]$record.saveRoot).TrimEnd('\'), $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$record.currentQualification.path), [IO.Path]::GetFullPath($QualificationPath), [StringComparison]::OrdinalIgnoreCase) -or
        [string]$record.currentQualification.sha256 -cne $ExpectedCurrentQualificationSha256 -or
        (Get-KmcSha256 $QualificationPath) -cne $ExpectedCurrentQualificationSha256) {
        throw 'Historical schema-v1 authority save root or qualification does not match current caller pins.'
    }
    foreach ($name in @('baseline','working')) {
        $expected = $CurrentPair.$name
        if (-not [string]::Equals([IO.Path]::GetFullPath([string]$record.$name.path), [IO.Path]::GetFullPath([string]$expected.path), [StringComparison]::OrdinalIgnoreCase) -or
            [string]$record.$name.fileName -cne [string]$expected.fileName -or
            [string]$record.$name.sha256 -cne [string]$expected.sha256 -or
            [long]$record.$name.length -ne [long]$expected.length -or
            [long]$record.$name.lastWriteTimeUtcTicks -ne [long]$expected.lastWriteTimeUtcTicks) {
            throw "Historical schema-v1 authority $name identity differs from the qualified fixture."
        }
    }
    if ([string]$record.priorAuthority.statePath -cne [IO.Path]::GetFullPath($ExpectedPriorSaveTransactionStatePath) -or
        [string]$record.priorAuthority.runId -cne $ExpectedPriorSaveTransactionRunId -or
        [string]$record.priorAuthority.stateSha256 -cne $ExpectedPriorSaveTransactionStateSha256 -or
        [string]$record.priorAuthority.inventoryDigest -cne $ExpectedPriorSaveMetadataDigest -or
        [string]$record.priorAuthority.baselineSha256 -cne [string]$CurrentPair.baseline.sha256 -or
        [string]$record.priorAuthority.supersededWorkingSha256 -cne $ExpectedSupersededWorkingSha256) {
        throw 'Historical schema-v1 authority prior-source pins do not reconcile.'
    }
    $priorAuthority = Read-KmcPriorSaveTransactionAuthority `
        -Path $ExpectedPriorSaveTransactionStatePath -StateRoot $StateRoot -SaveRoot $fullSaveRoot `
        -ExpectedRunId $ExpectedPriorSaveTransactionRunId -ExpectedStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedInventoryDigest $ExpectedPriorSaveMetadataDigest -ExpectedBaselineSha256 ([string]$CurrentPair.baseline.sha256) `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 -CurrentPair $CurrentPair
    $inventory = Assert-KmcSaveMetadataInventorySchema `
        -Inventory $record.currentInventory -ExpectedSaveRoot $fullSaveRoot `
        -ExpectedDigest ([string]$record.currentInventory.digest) -Description 'historical schema-v1 current inventory'
    $priorMap = @{}
    foreach ($entry in @($priorAuthority.inventory.entries)) { $priorMap[[string]$entry.path] = $entry }
    $currentMap = @{}
    foreach ($entry in @($inventory.entries)) { $currentMap[[string]$entry.path] = $entry }
    $transitionNames = New-Object 'Collections.Generic.List[string]'
    $protectedPins = New-Object 'Collections.Generic.List[object]'
    $baselineName = [string]$record.baseline.fileName
    $baselineEntry = $currentMap[$baselineName]
    if ($null -eq $baselineEntry -or [string]$baselineEntry.kind -cne 'file' -or
        [long]$baselineEntry.length -ne [long]$record.baseline.length -or
        [long]$baselineEntry.lastWriteTimeUtcTicks -ne [long]$record.baseline.lastWriteTimeUtcTicks) {
        throw 'Historical schema-v1 authority inventory does not contain its exact Baseline metadata.'
    }
    $protectedPins.Add([pscustomobject][ordered]@{
        path=$baselineName;length=[long]$record.baseline.length
        lastWriteTimeUtcTicks=[long]$record.baseline.lastWriteTimeUtcTicks
        sha256=[string]$record.baseline.sha256;sourceEpochId=$ExpectedEpochId
    })
    $transitionIndex = 0
    foreach ($transition in @($record.authorizedProtectedTransitions)) {
        if ($transition -isnot [pscustomobject]) { throw 'Historical schema-v1 authority transition is not an exact object.' }
        Assert-KmcExactProperties $transition @(
            'fileName','priorKind','priorLength','priorLastWriteTimeUtcTicks','currentKind','currentLength',
            'currentLastWriteTimeUtcTicks','currentSha256'
        ) 'historical schema-v1 authority transition'
        foreach ($field in @('fileName','priorKind','currentKind','currentSha256')) {
            if ($transition.$field -isnot [string]) { throw "Historical schema-v1 transition $field type is invalid." }
        }
        foreach ($field in @('priorLength','priorLastWriteTimeUtcTicks','currentLength','currentLastWriteTimeUtcTicks')) {
            if (($transition.$field -isnot [int]) -and ($transition.$field -isnot [long])) {
                throw "Historical schema-v1 transition $field type is invalid."
            }
        }
        $name = [string]$transition.fileName
        if (($transitionIndex -eq 0 -and $name -cnotmatch '^Auto_[0-9]+\.zks$') -or
            ($transitionIndex -eq 1 -and $name -cnotmatch '^Quick_[0-9]+\.zks$') -or
            [string]$transition.priorKind -cne 'file' -or [string]$transition.currentKind -cne 'file' -or
            [string]$transition.currentSha256 -cnotmatch '^[0-9a-f]{64}$') {
            throw 'Historical schema-v1 authority transition identity is invalid.'
        }
        $prior = $priorMap[$name]
        $current = $currentMap[$name]
        if ($null -eq $prior -or $null -eq $current -or
            [string]$prior.path -cne $name -or [string]$current.path -cne $name -or
            [string]$prior.kind -cne [string]$transition.priorKind -or
            [long]$prior.length -ne [long]$transition.priorLength -or
            [long]$prior.lastWriteTimeUtcTicks -ne [long]$transition.priorLastWriteTimeUtcTicks -or
            [string]$current.kind -cne [string]$transition.currentKind -or
            [long]$current.length -ne [long]$transition.currentLength -or
            [long]$current.lastWriteTimeUtcTicks -ne [long]$transition.currentLastWriteTimeUtcTicks) {
            throw "Historical schema-v1 authority transition metadata differs for $name."
        }
        $transitionNames.Add($name)
        $protectedPins.Add([pscustomobject][ordered]@{
            path=$name;length=[long]$transition.currentLength
            lastWriteTimeUtcTicks=[long]$transition.currentLastWriteTimeUtcTicks
            sha256=[string]$transition.currentSha256;sourceEpochId=$ExpectedEpochId
        })
        $transitionIndex++
    }
    $changedPaths = @(Get-KmcProtectedSaveAuthorityChangedPaths -Before $priorAuthority.inventory -After $inventory)
    $expectedChanges = @($transitionNames.ToArray() + [string]$priorAuthority.workingRelativePath | Sort-Object)
    if (($changedPaths -join "`n") -cne ($expectedChanges -join "`n")) {
        throw 'Historical schema-v1 authority does not contain exactly its recorded Working/Auto/Quick transition set.'
    }
    $orderedPins = @($protectedPins.ToArray() | Sort-Object path)
    return [pscustomobject]@{
        schemaVersion = 1
        path = [string]$pinned.path
        sha256 = $ExpectedAuthoritySha256
        epochId = $ExpectedEpochId
        record = $record
        currentInventory = $inventory
        currentInventoryDigest = [string]$inventory.digest
        currentProtectedSavePins = $orderedPins
        currentProtectedSavePinsSha256 = Get-KmcProtectedSavePinSetSha256 -Pins $orderedPins
        metadata = $pinned.metadata
    }
}

function ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson {
    param([Parameter(Mandatory = $true)][ValidateLength(2,65536)][string]$Json)
    if (-not $Json.TrimStart().StartsWith('[', [StringComparison]::Ordinal)) {
        throw 'Protected-save transition specifications must be an exact JSON array.'
    }
    Assert-KmcJsonObjectMembersUnique -Json $Json -Description 'protected-save transition specifications'
    try { $parsed = $Json | ConvertFrom-Json }
    catch { throw "Protected-save transition specifications are not valid JSON: $($_.Exception.Message)" }
    $transitions = @($parsed)
    if ($transitions.Count -lt 1 -or $transitions.Count -gt 64) {
        throw 'Protected-save transition specification count is outside the bounded range.'
    }
    $result = New-Object 'Collections.Generic.List[object]'
    $previousPath = $null
    foreach ($transition in $transitions) {
        if ($transition -isnot [pscustomobject]) { throw 'Protected-save transition specification contains a non-object entry.' }
        Assert-KmcExactProperties $transition @(
            'priorPath','priorLength','priorLastWriteTimeUtcTicks','priorSha256','priorHashStatus',
            'currentPath','currentLength','currentLastWriteTimeUtcTicks','currentSha256','transitionReason'
        ) 'protected-save transition specification'
        foreach ($field in @('priorPath','priorHashStatus','currentPath','currentSha256','transitionReason')) {
            if ($transition.$field -isnot [string]) { throw "Protected-save transition $field type is invalid." }
        }
        foreach ($field in @('priorLength','priorLastWriteTimeUtcTicks','currentLength','currentLastWriteTimeUtcTicks')) {
            if (($transition.$field -isnot [int]) -and ($transition.$field -isnot [long])) {
                throw "Protected-save transition $field type is invalid."
            }
        }
        $priorPath = [string]$transition.priorPath
        $currentPath = [string]$transition.currentPath
        if ([string]::IsNullOrWhiteSpace($priorPath) -or [IO.Path]::IsPathRooted($priorPath) -or
            $priorPath.Contains('/') -or $priorPath.Contains('\') -or [IO.Path]::GetFileName($priorPath) -cne $priorPath -or
            $currentPath -cne $priorPath -or $currentPath -cnotmatch '^[A-Za-z0-9._-]+\.zks$' -or
            [long]$transition.priorLength -le 0 -or [long]$transition.priorLastWriteTimeUtcTicks -le 0 -or
            [long]$transition.currentLength -le 0 -or [long]$transition.currentLastWriteTimeUtcTicks -le 0 -or
            [string]$transition.currentSha256 -cnotmatch '^[0-9a-f]{64}$' -or
            [string]$transition.transitionReason -cne $script:KmcExternalTransitionReason) {
            throw "Protected-save transition identity or current pin is invalid for $priorPath."
        }
        if ($null -ne $previousPath -and [string]::CompareOrdinal($previousPath, $priorPath) -ge 0) {
            throw 'Protected-save transitions are not in strict ordinal path order.'
        }
        $status = [string]$transition.priorHashStatus
        if ($status -ceq $script:KmcMetadataOnlyPriorHashStatus) {
            if ($priorPath -cne $script:KmcMetadataOnlyExceptionPath -or $null -ne $transition.priorSha256) {
                throw 'The schema-v1 metadata-only prior-hash marker is restricted to exact Quick_3.zks with priorSha256 null.'
            }
        }
        elseif ($status -ceq $script:KmcKnownPriorHashStatus) {
            if ($transition.priorSha256 -isnot [string] -or [string]$transition.priorSha256 -cnotmatch '^[0-9a-f]{64}$') {
                throw "Known protected-save prior hash is missing or invalid for $priorPath."
            }
        }
        else { throw "Protected-save prior-hash status is invalid for $priorPath." }
        $result.Add([pscustomobject][ordered]@{
            priorPath=$priorPath
            priorLength=[long]$transition.priorLength
            priorLastWriteTimeUtcTicks=[long]$transition.priorLastWriteTimeUtcTicks
            priorSha256=$(if ($null -eq $transition.priorSha256) { $null } else { [string]$transition.priorSha256 })
            priorHashStatus=$status
            currentPath=$currentPath
            currentLength=[long]$transition.currentLength
            currentLastWriteTimeUtcTicks=[long]$transition.currentLastWriteTimeUtcTicks
            currentSha256=[string]$transition.currentSha256
            transitionReason=[string]$transition.transitionReason
        })
        $previousPath = $priorPath
    }
    return $result.ToArray()
}

function New-KmcChainedProtectedSaveContinuityAuthorityRecord {
    param(
        [Parameter(Mandatory = $true)]$CurrentPair,
        [Parameter(Mandatory = $true)]$ParentAuthority,
        [Parameter(Mandatory = $true)]$CurrentInventory,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$CurrentQualificationSha256,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$EpochId,
        [Parameter(Mandatory = $true)][string]$AuthorizedAtUtc,
        [Parameter(Mandatory = $true)][object[]]$Transitions
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $fullQualificationPath = [IO.Path]::GetFullPath($QualificationPath)
    $authorizedAt = [DateTimeOffset]::MinValue
    if (-not [DateTimeOffset]::TryParseExact(
        $AuthorizedAtUtc, 'o', [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind, [ref]$authorizedAt) -or $authorizedAt.Offset -ne [TimeSpan]::Zero) {
        throw 'Schema-v2 protected-save authority timestamp is not exact UTC round-trip form.'
    }
    if ([long]$ParentAuthority.schemaVersion -ne 1 -or
        [string]$ParentAuthority.epochId -ceq $EpochId -or
        [string]$ParentAuthority.sha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Schema-v2 authority requires one exact immutable schema-v1 parent.'
    }
    [void](Assert-KmcSaveMetadataInventorySchema `
        -Inventory $ParentAuthority.currentInventory -ExpectedSaveRoot $fullSaveRoot `
        -ExpectedDigest ([string]$ParentAuthority.currentInventoryDigest) -Description 'schema-v2 parent inventory')
    [void](Assert-KmcSaveMetadataInventorySchema `
        -Inventory $CurrentInventory -ExpectedSaveRoot $fullSaveRoot `
        -ExpectedDigest ([string]$CurrentInventory.digest) -Description 'schema-v2 current inventory')
    if ([int]$CurrentPair.schemaVersion -ne 1 -or @($CurrentPair.writableSaveNames).Count -ne 1 -or
        [string]@($CurrentPair.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING' -or
        [string]$CurrentPair.baseline.name -cne 'KMC_AUTOMATION_BASELINE' -or
        [string]$CurrentPair.working.name -cne 'KMC_AUTOMATION_WORKING') {
        throw 'Schema-v2 authority requires the exact qualified pair and Working-only allowlist.'
    }
    $transitionArray = @($Transitions)
    if ($transitionArray.Count -lt 1 -or $transitionArray.Count -gt 64) { throw 'Schema-v2 transition count is outside the bounded range.' }
    $parentMap = @{}
    foreach ($entry in @($ParentAuthority.currentInventory.entries)) { $parentMap[[string]$entry.path] = $entry }
    $currentMap = @{}
    foreach ($entry in @($CurrentInventory.entries)) { $currentMap[[string]$entry.path] = $entry }
    $parentPinMap = @{}
    foreach ($pin in @($ParentAuthority.currentProtectedSavePins)) { $parentPinMap[[string]$pin.path] = $pin }
    if ((Get-KmcProtectedSavePinSetSha256 -Pins @($ParentAuthority.currentProtectedSavePins)) -cne
        [string]$ParentAuthority.currentProtectedSavePinsSha256) {
        throw 'Schema-v2 parent protected-save pin set digest is invalid.'
    }
    $transitionPaths = @($transitionArray | ForEach-Object { [string]$_.currentPath })
    $changedPaths = @(Get-KmcProtectedSaveAuthorityChangedPaths -Before $ParentAuthority.currentInventory -After $CurrentInventory)
    if (($changedPaths -join "`n") -cne ($transitionPaths -join "`n")) {
        $description = if ($changedPaths.Count -eq 0) { '<none>' } else { $changedPaths -join ', ' }
        throw "Schema-v2 actual changed-path set does not equal the explicit ordered transition set; changed: $description"
    }
    $normalizedTransitions = New-Object 'Collections.Generic.List[object]'
    $previousPath = $null
    foreach ($transition in $transitionArray) {
        $path = [string]$transition.currentPath
        if ($null -ne $previousPath -and [string]::CompareOrdinal($previousPath, $path) -ge 0) {
            throw 'Schema-v2 transitions are not in strict ordinal path order.'
        }
        if ([string]$transition.priorPath -cne $path -or
            [string]::Equals($path, [string]$CurrentPair.baseline.fileName, [StringComparison]::OrdinalIgnoreCase) -or
            [string]::Equals($path, [string]$CurrentPair.working.fileName, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Schema-v2 transition path is renamed, ambiguous, or targets a KMC fixture: $path"
        }
        $prior = $parentMap[$path]
        $current = $currentMap[$path]
        if ($null -eq $prior -or $null -eq $current -or
            [string]$prior.kind -cne 'file' -or [string]$current.kind -cne 'file' -or
            [string]$prior.path -cne $path -or [string]$current.path -cne $path -or
            [long]$prior.length -ne [long]$transition.priorLength -or
            [long]$prior.lastWriteTimeUtcTicks -ne [long]$transition.priorLastWriteTimeUtcTicks -or
            [long]$current.length -ne [long]$transition.currentLength -or
            [long]$current.lastWriteTimeUtcTicks -ne [long]$transition.currentLastWriteTimeUtcTicks -or
            [string]$transition.currentSha256 -cnotmatch '^[0-9a-f]{64}$' -or
            [string]$transition.transitionReason -cne $script:KmcExternalTransitionReason) {
            throw "Schema-v2 transition metadata or current hash differs for $path."
        }
        $parentPin = $parentPinMap[$path]
        if ([string]$transition.priorHashStatus -ceq $script:KmcMetadataOnlyPriorHashStatus) {
            if ($path -cne $script:KmcMetadataOnlyExceptionPath -or $null -ne $transition.priorSha256 -or
                $null -ne $parentPin -or [long]$ParentAuthority.schemaVersion -ne 1) {
                throw 'Schema-v2 metadata-only prior hash is not an exact path-specific schema-v1 evidence gap.'
            }
        }
        elseif ([string]$transition.priorHashStatus -ceq $script:KmcKnownPriorHashStatus) {
            if ($null -eq $parentPin -or $transition.priorSha256 -isnot [string] -or
                [string]$transition.priorSha256 -cne [string]$parentPin.sha256 -or
                [long]$transition.priorLength -ne [long]$parentPin.length -or
                [long]$transition.priorLastWriteTimeUtcTicks -ne [long]$parentPin.lastWriteTimeUtcTicks) {
                throw "Schema-v2 known prior hash does not equal the authoritative parent pin for $path."
            }
        }
        else { throw "Schema-v2 prior-hash status is invalid for $path." }
        $normalizedTransitions.Add([pscustomobject][ordered]@{
            priorPath=$path
            priorLength=[long]$transition.priorLength
            priorLastWriteTimeUtcTicks=[long]$transition.priorLastWriteTimeUtcTicks
            priorSha256=$(if ($null -eq $transition.priorSha256) { $null } else { [string]$transition.priorSha256 })
            priorHashStatus=[string]$transition.priorHashStatus
            currentPath=$path
            currentLength=[long]$transition.currentLength
            currentLastWriteTimeUtcTicks=[long]$transition.currentLastWriteTimeUtcTicks
            currentSha256=[string]$transition.currentSha256
            transitionReason=[string]$transition.transitionReason
        })
        $previousPath = $path
    }
    $currentPinMap = @{}
    foreach ($pin in @($ParentAuthority.currentProtectedSavePins)) {
        $currentPinMap[[string]$pin.path] = [pscustomobject][ordered]@{
            path=[string]$pin.path;length=[long]$pin.length
            lastWriteTimeUtcTicks=[long]$pin.lastWriteTimeUtcTicks
            sha256=[string]$pin.sha256;sourceEpochId=[string]$pin.sourceEpochId
        }
    }
    foreach ($transition in @($normalizedTransitions.ToArray())) {
        $currentPinMap[[string]$transition.currentPath] = [pscustomobject][ordered]@{
            path=[string]$transition.currentPath;length=[long]$transition.currentLength
            lastWriteTimeUtcTicks=[long]$transition.currentLastWriteTimeUtcTicks
            sha256=[string]$transition.currentSha256;sourceEpochId=$EpochId
        }
    }
    $currentPins = @($currentPinMap.Keys | Sort-Object | ForEach-Object { $currentPinMap[$_] })
    $currentPinSetSha256 = Get-KmcProtectedSavePinSetSha256 -Pins $currentPins
    return [ordered]@{
        schemaVersion = 2
        authorityKind = 'user-attested-protected-save-continuity-chain'
        epochId = $EpochId
        authorizedAtUtc = $AuthorizedAtUtc
        attestationScope = 'external-user-game-activity-continuity-only-no-save-write-authority'
        saveRoot = $fullSaveRoot
        parentAuthority = [ordered]@{
            path=[string]$ParentAuthority.path;epochId=[string]$ParentAuthority.epochId
            sha256=[string]$ParentAuthority.sha256;schemaVersion=[long]$ParentAuthority.schemaVersion
            currentInventoryDigest=[string]$ParentAuthority.currentInventoryDigest
        }
        attestation = [ordered]@{
            kind='explicit-user-attested-external-kingmaker-activity'
            transitionReason=$script:KmcExternalTransitionReason
            metadataOnlyPriorHashExceptionPath=$script:KmcMetadataOnlyExceptionPath
            metadataOnlyPriorHashStatus=$script:KmcMetadataOnlyPriorHashStatus
            metadataOnlyPriorHashExplanation='schema v1 did not capture a content hash for this exact path; null does not establish content equivalence'
            grantsSaveWriteAuthority=$false
        }
        currentQualification = [ordered]@{ path=$fullQualificationPath;sha256=$CurrentQualificationSha256 }
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
        authorizedProtectedTransitions = $normalizedTransitions.ToArray()
        currentProtectedSavePins = $currentPins
        currentProtectedSavePinsSha256 = $currentPinSetSha256
        currentInventory = $CurrentInventory
    }
}

function Assert-KmcChainedProtectedSaveContinuityLiveState {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)]$LiveInventory
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    Assert-KmcSaveMetadataInventoriesEqual -Before $Record.currentInventory -After $LiveInventory -Description 'schema-v2 live protected-save inventory'
    foreach ($entry in @($LiveInventory.entries)) {
        $path = Assert-KmcChildPath (Join-Path $fullSaveRoot ([string]$entry.path)) $fullSaveRoot 'schema-v2 protected inventory entry'
        if ([string]$entry.kind -ceq 'reparse') { throw "Schema-v2 protected inventory contains a reparse point: $($entry.path)" }
        if ([string]$entry.kind -ceq 'file') { Assert-KmcRecoveryLeafNoLinks $path 'schema-v2 protected inventory file' }
        elseif ([string]$entry.kind -ceq 'directory') { Assert-KmcNotReparsePoint $path 'schema-v2 protected inventory directory' }
        else { throw "Schema-v2 protected inventory contains an invalid entry kind: $($entry.path)" }
    }
    $pinSetSha256 = Get-KmcProtectedSavePinSetSha256 -Pins @($Record.currentProtectedSavePins)
    if ($pinSetSha256 -cne [string]$Record.currentProtectedSavePinsSha256) {
        throw 'Schema-v2 protected-save pin-set digest differs from the record.'
    }
    foreach ($pin in @($Record.currentProtectedSavePins)) {
        $path = Assert-KmcChildPath (Join-Path $fullSaveRoot ([string]$pin.path)) $fullSaveRoot 'schema-v2 protected content pin'
        Assert-KmcRecoveryLeafNoLinks $path 'schema-v2 protected content pin'
        $file = Get-Item -LiteralPath $path -Force
        if ([long]$file.Length -ne [long]$pin.length -or
            [long]$file.LastWriteTimeUtc.Ticks -ne [long]$pin.lastWriteTimeUtcTicks -or
            (Get-KmcSha256 $path) -cne [string]$pin.sha256) {
            throw "Schema-v2 protected content pin differs for $($pin.path)."
        }
    }
    return [pscustomobject]@{ inventory=$LiveInventory;pinSetSha256=$pinSetSha256 }
}

function Read-KmcChainedProtectedSaveContinuityAuthority {
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
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSavePinSetSha256
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $pair = Assert-KmcFixturePair -SaveRoot $fullSaveRoot -QualificationPath $QualificationPath
    if ((Get-KmcSha256 $QualificationPath) -cne $ExpectedCurrentQualificationSha256) {
        throw 'Schema-v2 current fixture qualification differs from the explicit caller pin.'
    }
    $pinned = Read-KmcPinnedProtectedSaveAuthorityJson `
        -Path $Path -StateRoot $StateRoot -ExpectedEpochId $ExpectedEpochId `
        -ExpectedAuthoritySha256 $ExpectedAuthoritySha256 -Description 'schema-v2 protected-save authority'
    $record = $pinned.record
    if ($record -isnot [pscustomobject]) { throw 'Schema-v2 protected-save authority is not an exact JSON object.' }
    Assert-KmcExactProperties $record @(
        'schemaVersion','authorityKind','epochId','authorizedAtUtc','attestationScope','saveRoot','parentAuthority',
        'attestation','currentQualification','baseline','working','writableSaveNames','authorizedProtectedTransitions',
        'currentProtectedSavePins','currentProtectedSavePinsSha256','currentInventory'
    ) 'schema-v2 protected-save authority'
    if ((($record.schemaVersion -isnot [int]) -and ($record.schemaVersion -isnot [long])) -or
        [long]$record.schemaVersion -ne 2 -or $record.authorityKind -isnot [string] -or
        [string]$record.authorityKind -cne 'user-attested-protected-save-continuity-chain' -or
        $record.epochId -isnot [string] -or [string]$record.epochId -cne $ExpectedEpochId -or
        $record.authorizedAtUtc -isnot [string] -or $record.attestationScope -isnot [string] -or
        [string]$record.attestationScope -cne 'external-user-game-activity-continuity-only-no-save-write-authority' -or
        $record.saveRoot -isnot [string] -or $record.parentAuthority -isnot [pscustomobject] -or
        $record.attestation -isnot [pscustomobject] -or $record.currentQualification -isnot [pscustomobject] -or
        $record.baseline -isnot [pscustomobject] -or $record.working -isnot [pscustomobject] -or
        $record.writableSaveNames -isnot [Array] -or $record.authorizedProtectedTransitions -isnot [Array] -or
        $record.currentProtectedSavePins -isnot [Array] -or $record.currentProtectedSavePinsSha256 -isnot [string]) {
        throw 'Schema-v2 protected-save authority top-level schema or types are invalid.'
    }
    Assert-KmcExactProperties $record.parentAuthority @('path','epochId','sha256','schemaVersion','currentInventoryDigest') 'schema-v2 parent authority'
    Assert-KmcExactProperties $record.attestation @(
        'kind','transitionReason','metadataOnlyPriorHashExceptionPath','metadataOnlyPriorHashStatus',
        'metadataOnlyPriorHashExplanation','grantsSaveWriteAuthority'
    ) 'schema-v2 authority attestation'
    Assert-KmcExactProperties $record.currentQualification @('path','sha256') 'schema-v2 authority qualification'
    foreach ($name in @('baseline','working')) {
        Assert-KmcExactProperties $record.$name @('path','fileName','sha256','length','lastWriteTimeUtcTicks') "schema-v2 authority $name"
    }
    if ($record.parentAuthority.path -isnot [string] -or $record.parentAuthority.epochId -isnot [string] -or
        $record.parentAuthority.sha256 -isnot [string] -or
        (($record.parentAuthority.schemaVersion -isnot [int]) -and ($record.parentAuthority.schemaVersion -isnot [long])) -or
        [long]$record.parentAuthority.schemaVersion -ne 1 -or $record.parentAuthority.currentInventoryDigest -isnot [string] -or
        [string]$record.parentAuthority.sha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$record.parentAuthority.currentInventoryDigest -cnotmatch '^[0-9a-f]{64}$' -or
        $record.attestation.kind -isnot [string] -or
        [string]$record.attestation.kind -cne 'explicit-user-attested-external-kingmaker-activity' -or
        $record.attestation.transitionReason -isnot [string] -or
        [string]$record.attestation.transitionReason -cne $script:KmcExternalTransitionReason -or
        $record.attestation.metadataOnlyPriorHashExceptionPath -isnot [string] -or
        [string]$record.attestation.metadataOnlyPriorHashExceptionPath -cne $script:KmcMetadataOnlyExceptionPath -or
        $record.attestation.metadataOnlyPriorHashStatus -isnot [string] -or
        [string]$record.attestation.metadataOnlyPriorHashStatus -cne $script:KmcMetadataOnlyPriorHashStatus -or
        $record.attestation.metadataOnlyPriorHashExplanation -isnot [string] -or
        [string]$record.attestation.metadataOnlyPriorHashExplanation -cne 'schema v1 did not capture a content hash for this exact path; null does not establish content equivalence' -or
        $record.attestation.grantsSaveWriteAuthority -isnot [bool] -or [bool]$record.attestation.grantsSaveWriteAuthority -or
        @($record.writableSaveNames).Count -ne 1 -or [string]@($record.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING') {
        throw 'Schema-v2 parent, attestation, or Working-only authority is invalid.'
    }
    if (-not [string]::Equals([IO.Path]::GetFullPath([string]$record.saveRoot).TrimEnd('\'), $fullSaveRoot, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$record.currentQualification.path), [IO.Path]::GetFullPath($QualificationPath), [StringComparison]::OrdinalIgnoreCase) -or
        [string]$record.currentQualification.sha256 -cne $ExpectedCurrentQualificationSha256) {
        throw 'Schema-v2 save root or qualification identity differs from caller pins.'
    }
    $parent = Read-KmcHistoricalProtectedSaveContinuityAuthorityV1 `
        -Path ([string]$record.parentAuthority.path) -StateRoot $StateRoot -SaveRoot $fullSaveRoot `
        -QualificationPath $QualificationPath -ExpectedEpochId ([string]$record.parentAuthority.epochId) `
        -ExpectedAuthoritySha256 ([string]$record.parentAuthority.sha256) `
        -ExpectedCurrentQualificationSha256 $ExpectedCurrentQualificationSha256 `
        -ExpectedPriorSaveTransactionStatePath $ExpectedPriorSaveTransactionStatePath `
        -ExpectedPriorSaveTransactionRunId $ExpectedPriorSaveTransactionRunId `
        -ExpectedPriorSaveTransactionStateSha256 $ExpectedPriorSaveTransactionStateSha256 `
        -ExpectedPriorSaveMetadataDigest $ExpectedPriorSaveMetadataDigest `
        -ExpectedSupersededWorkingSha256 $ExpectedSupersededWorkingSha256 -CurrentPair $pair
    if ([string]$record.parentAuthority.currentInventoryDigest -cne [string]$parent.currentInventoryDigest) {
        throw 'Schema-v2 parent inventory digest differs from the immutable parent authority.'
    }
    $transitionJson = ConvertTo-Json -InputObject @($record.authorizedProtectedTransitions) -Depth 20 -Compress
    $transitions = @(ConvertFrom-KmcProtectedSaveTransitionSpecificationsJson -Json $transitionJson)
    [void](Assert-KmcSaveMetadataInventorySchema `
        -Inventory $record.currentInventory -ExpectedSaveRoot $fullSaveRoot `
        -ExpectedDigest ([string]$record.currentInventory.digest) -Description 'schema-v2 recorded current inventory')
    $liveInventory = Get-KmcSaveMetadataInventory $fullSaveRoot
    Assert-KmcSaveMetadataInventoriesEqual -Before $record.currentInventory -After $liveInventory -Description 'schema-v2 recorded/live save inventory'
    $expectedRecord = New-KmcChainedProtectedSaveContinuityAuthorityRecord `
        -CurrentPair $pair -ParentAuthority $parent -CurrentInventory $liveInventory -SaveRoot $fullSaveRoot `
        -QualificationPath $QualificationPath -CurrentQualificationSha256 $ExpectedCurrentQualificationSha256 `
        -EpochId $ExpectedEpochId -AuthorizedAtUtc ([string]$record.authorizedAtUtc) -Transitions $transitions
    if (($record | ConvertTo-Json -Depth 30 -Compress) -cne ($expectedRecord | ConvertTo-Json -Depth 30 -Compress)) {
        throw 'Schema-v2 authority content does not exactly reconcile to parent, transitions, current pins, and live metadata.'
    }
    if ([string]$record.currentProtectedSavePinsSha256 -cne $ExpectedProtectedSavePinSetSha256) {
        throw 'Schema-v2 protected-save pin-set digest differs from the explicit caller pin.'
    }
    [void](Assert-KmcChainedProtectedSaveContinuityLiveState -Record $record -SaveRoot $fullSaveRoot -LiveInventory $liveInventory)
    return [pscustomobject]@{
        schemaVersion=2;path=[string]$pinned.path;sha256=$ExpectedAuthoritySha256;epochId=$ExpectedEpochId
        pair=$pair;saveMetadata=$liveInventory;record=$record
        protectedSavePinSetSha256=[string]$record.currentProtectedSavePinsSha256
    }
}

function Assert-KmcQualifiedWorkingProtectedSaveContinuity {
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
        [string]$ExpectedProtectedAutoSaveName,
        [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedAutoSaveSha256,
        [string]$ExpectedProtectedQuickSaveName,
        [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedQuickSaveSha256,
        [ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSavePinSetSha256
    )
    $fullSaveRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    $fullStateRoot = [IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    Assert-KmcPathsDoNotOverlap -First $fullSaveRoot -Second $fullStateRoot -Description 'KMC save and runtime-state roots'
    Assert-KmcNoGameProcesses
    $common = @{
        Path=$ProtectedSaveContinuityAuthorityPath;StateRoot=$fullStateRoot;SaveRoot=$fullSaveRoot;QualificationPath=$QualificationPath
        ExpectedEpochId=$ExpectedProtectedSaveContinuityEpochId;ExpectedAuthoritySha256=$ExpectedProtectedSaveContinuityAuthoritySha256
        ExpectedCurrentQualificationSha256=$ExpectedCurrentQualificationSha256
        ExpectedPriorSaveTransactionStatePath=$PriorSaveTransactionStatePath
        ExpectedPriorSaveTransactionRunId=$ExpectedPriorSaveTransactionRunId
        ExpectedPriorSaveTransactionStateSha256=$ExpectedPriorSaveTransactionStateSha256
        ExpectedPriorSaveMetadataDigest=$ExpectedPriorSaveMetadataDigest
        ExpectedSupersededWorkingSha256=$ExpectedSupersededWorkingSha256
    }
    $usesChained = -not [string]::IsNullOrWhiteSpace($ExpectedProtectedSavePinSetSha256)
    $legacyValues = @($ExpectedProtectedAutoSaveName,$ExpectedProtectedAutoSaveSha256,$ExpectedProtectedQuickSaveName,$ExpectedProtectedQuickSaveSha256)
    $legacyPresent = @($legacyValues | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) }).Count
    if ($usesChained) {
        if ($legacyPresent -ne 0) { throw 'Schema-v2 continuity validation rejects schema-v1 Auto/Quick caller pins.' }
        $common.ExpectedProtectedSavePinSetSha256 = $ExpectedProtectedSavePinSetSha256
        $first = Read-KmcChainedProtectedSaveContinuityAuthority @common
        Assert-KmcNoGameProcesses
        $second = Read-KmcChainedProtectedSaveContinuityAuthority @common
    }
    else {
        if ($legacyPresent -ne 4) { throw 'Schema-v1 continuity validation requires all four exact Auto/Quick caller pins.' }
        $legacy = @{
            SaveRoot=$fullSaveRoot;StateRoot=$fullStateRoot;QualificationPath=$QualificationPath
            ExpectedCurrentQualificationSha256=$ExpectedCurrentQualificationSha256
            ExpectedSupersededWorkingSha256=$ExpectedSupersededWorkingSha256
            PriorSaveTransactionStatePath=$PriorSaveTransactionStatePath
            ExpectedPriorSaveTransactionRunId=$ExpectedPriorSaveTransactionRunId
            ExpectedPriorSaveTransactionStateSha256=$ExpectedPriorSaveTransactionStateSha256
            ExpectedPriorSaveMetadataDigest=$ExpectedPriorSaveMetadataDigest
            ProtectedSaveContinuityAuthorityPath=$ProtectedSaveContinuityAuthorityPath
            ExpectedProtectedSaveContinuityEpochId=$ExpectedProtectedSaveContinuityEpochId
            ExpectedProtectedSaveContinuityAuthoritySha256=$ExpectedProtectedSaveContinuityAuthoritySha256
            ExpectedProtectedAutoSaveName=$ExpectedProtectedAutoSaveName
            ExpectedProtectedAutoSaveSha256=$ExpectedProtectedAutoSaveSha256
            ExpectedProtectedQuickSaveName=$ExpectedProtectedQuickSaveName
            ExpectedProtectedQuickSaveSha256=$ExpectedProtectedQuickSaveSha256
        }
        $first = Assert-KmcQualifiedWorkingProtectedSaveContinuityV1 @legacy
        Assert-KmcNoGameProcesses
        $second = Assert-KmcQualifiedWorkingProtectedSaveContinuityV1 @legacy
    }
    Assert-KmcSaveMetadataInventoriesEqual -Before $first.saveMetadata -After $second.saveMetadata -Description 'protected-save continuity live metadata'
    if ((New-KmcRuntimeFixturePayload $first.pair | ConvertTo-Json -Depth 10 -Compress) -cne
        (New-KmcRuntimeFixturePayload $second.pair | ConvertTo-Json -Depth 10 -Compress)) {
        throw 'KMC fixture identity changed during protected-save continuity validation.'
    }
    return $second
}
