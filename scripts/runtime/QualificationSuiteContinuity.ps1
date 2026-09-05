Set-StrictMode -Version Latest

function Assert-KmcPhase3fStartingInstallation {
    param([Parameter(Mandatory=$true)][string]$KmcRoot)
    # Narrow standing-installation exception authorized by Phase 3F. The full
    # suite inventory additionally pins names, bytes and timestamps for restoration.
    $root = Get-Item -LiteralPath $KmcRoot -Force
    if (-not $root.PSIsContainer -or $root.Name -cne 'KingmakerMountedCombat') { throw 'Existing KMC installation has ambiguous casing or type.' }
    Assert-KmcDirectoryTreeCloneable $KmcRoot 'Phase 3F starting KMC installation'
    $pins = @{
        'Info.json'='f137e69d163967c4d5f36e3610be4b9270ac160923b029cc131d56cb32d24018'
        'KingmakerMountedCombat.dll'='5bcc3bc61bb1677ea81037fdc5a8ebd740ff4d0753d5255e37fcc789e6407f2f'
        'KingmakerMountedCombat.dll.65229.cache'='5bcc3bc61bb1677ea81037fdc5a8ebd740ff4d0753d5255e37fcc789e6407f2f'
    }
    $entries = @(Get-ChildItem -LiteralPath $KmcRoot -Force -Recurse)
    if ($entries.Count -ne $pins.Count) { throw 'Existing KMC tree differs from the exact Phase 3F starting payload.' }
    foreach ($entry in $entries) {
        if ($entry.PSIsContainer -or $entry.Name -cnotin @($pins.Keys) -or
            (Get-KmcSha256 $entry.FullName) -cne [string]$pins[$entry.Name]) {
            throw 'Existing KMC bytes differ from the exact Phase 3F fallback/cache pins.'
        }
    }
    return $true
}

function Assert-KmcQualificationAdmissionQuiescent {
    param(
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$ModsRoot
    )
    Assert-KmcNoGameProcesses
    foreach ($name in @('MSBuild','dotnet','git')) {
        if (@(Get-Process -Name $name -ErrorAction SilentlyContinue).Count -ne 0) {
            throw "Qualification admission found an active build or Git process: $name"
        }
    }
    $fullState=[IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    if (Test-Path -LiteralPath (Join-Path $fullState 'active-transaction.lock')) { throw 'Qualification admission found an active or stale runtime lock.' }
    if (Test-Path -LiteralPath (Join-Path $ModsRoot '.kmc-runtime-sentinel.json')) { throw 'Qualification admission found a live KMC sentinel.' }
    if (Test-Path -LiteralPath (Join-Path $ModsRoot 'KingmakerMountedCombat')) {
        [void](Assert-KmcPhase3fStartingInstallation -KmcRoot (Join-Path $ModsRoot 'KingmakerMountedCombat'))
    }
    foreach ($folder in @('run-transactions','transactions','save-transactions','fixture-recoveries')) {
        $root=Join-Path $fullState $folder
        if (-not (Test-Path -LiteralPath $root -PathType Container)) { continue }
        foreach ($file in @(Get-ChildItem -LiteralPath $root -File -Filter '*.json' -Force)) {
            $state=Read-KmcJson $file.FullName
            $terminalPhases = if ($folder -ceq 'fixture-recoveries') { @('recovered','rolled-back') } else { @('restored') }
            if ($null -eq $state.PSObject.Properties['phase'] -or [string]$state.phase -cnotin $terminalPhases) {
                throw "Qualification admission found a non-restored transaction record: $($file.FullName)"
            }
        }
    }
    return $true
}

function Get-KmcQualificationTreeInventory {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][ValidateSet('save-root','mods-root')][string]$Scope,
        [string[]]$ExcludeRelativeRoots = @()
    )
    $fullRoot = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) { throw "Qualification inventory root is missing: $fullRoot" }
    Assert-KmcNotReparsePoint $fullRoot "qualification $Scope root"
    $excluded = @($ExcludeRelativeRoots | ForEach-Object { ([string]$_).Replace('\','/').Trim('/') })
    $records = New-Object 'Collections.Generic.List[object]'
    $pending = New-Object 'Collections.Generic.Queue[string]'
    $pending.Enqueue($fullRoot)
    while ($pending.Count -ne 0) {
        $current = $pending.Dequeue()
        foreach ($item in @(Get-ChildItem -LiteralPath $current -Force | Sort-Object FullName)) {
            $relative = $item.FullName.Substring($fullRoot.Length + 1).Replace('\','/')
            $skip = $false
            foreach ($prefix in $excluded) {
                if ([string]::Equals($relative, $prefix, [StringComparison]::OrdinalIgnoreCase) -or
                    $relative.StartsWith($prefix + '/', [StringComparison]::OrdinalIgnoreCase)) { $skip = $true; break }
            }
            if ($skip) { continue }
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Qualification $Scope inventory contains a reparse point: $relative"
            }
            $linkType = $item.PSObject.Properties['LinkType']
            if (-not $item.PSIsContainer -and $null -ne $linkType -and [string]$linkType.Value -ceq 'HardLink') {
                throw "Qualification $Scope inventory contains a detectable hard link: $relative"
            }
            if ($item.PSIsContainer) {
                $records.Add([pscustomobject][ordered]@{
                    kind='directory';path=$relative;length=[long]0
                    lastWriteTimeUtcTicks=[long]$item.LastWriteTimeUtc.Ticks;sha256=$null
                })
                $pending.Enqueue($item.FullName)
            }
            else {
                $records.Add([pscustomobject][ordered]@{
                    kind='file';path=$relative;length=[long]$item.Length
                    lastWriteTimeUtcTicks=[long]$item.LastWriteTimeUtc.Ticks;sha256=Get-KmcSha256 $item.FullName
                })
            }
        }
    }
    $ordered = @($records | Sort-Object kind,path)
    $files = @($ordered | Where-Object kind -ceq 'file')
    $totalBytes = [long]0
    foreach ($file in $files) { $totalBytes += [long]$file.length }
    $canonical = ($ordered | ForEach-Object {
        '{0}|{1}|{2}|{3}|{4}' -f $_.kind,$_.path,$_.length,$_.lastWriteTimeUtcTicks,$_.sha256
    }) -join "`n"
    $contentCanonical = ($ordered | ForEach-Object {
        '{0}|{1}|{2}|{3}' -f $_.kind,$_.path,$_.length,$_.sha256
    }) -join "`n"
    return [pscustomobject][ordered]@{
        schemaVersion=1;scope=$Scope;root=$fullRoot
        directoryCount=@($ordered | Where-Object kind -ceq 'directory').Count
        fileCount=$files.Count;totalBytes=$totalBytes
        digest=Get-KmcTextSha256 $canonical
        contentDigest=Get-KmcTextSha256 $contentCanonical
        entries=$ordered
    }
}

function Assert-KmcQualificationTreeInventorySchema {
    param(
        [Parameter(Mandatory = $true)]$Inventory,
        [Parameter(Mandatory = $true)][ValidateSet('save-root','mods-root')][string]$ExpectedScope,
        [Parameter(Mandatory = $true)][string]$ExpectedRoot,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if ($Inventory -isnot [pscustomobject]) { throw "$Description is not an exact JSON object." }
    Assert-KmcExactProperties $Inventory @(
        'schemaVersion','scope','root','directoryCount','fileCount','totalBytes','digest','contentDigest','entries'
    ) $Description
    $fullRoot = [IO.Path]::GetFullPath($ExpectedRoot).TrimEnd('\')
    if ([long]$Inventory.schemaVersion -ne 1 -or [string]$Inventory.scope -cne $ExpectedScope -or
        -not [string]::Equals([IO.Path]::GetFullPath([string]$Inventory.root).TrimEnd('\'),$fullRoot,[StringComparison]::OrdinalIgnoreCase) -or
        [string]$Inventory.digest -cnotmatch '^[0-9a-f]{64}$' -or [string]$Inventory.contentDigest -cnotmatch '^[0-9a-f]{64}$' -or
        $Inventory.entries -isnot [Array]) { throw "$Description header is invalid." }
    $seen = New-Object 'Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $canonical = New-Object 'Collections.Generic.List[string]'
    $contentCanonical = New-Object 'Collections.Generic.List[string]'
    $files = [long]0; $directories = [long]0; $bytes = [long]0
    foreach ($entry in @($Inventory.entries)) {
        if ($entry -isnot [pscustomobject]) { throw "$Description contains a non-object entry." }
        Assert-KmcExactProperties $entry @('kind','path','length','lastWriteTimeUtcTicks','sha256') "$Description entry"
        $path = [string]$entry.path; $kind = [string]$entry.kind
        if ($kind -cnotin @('file','directory') -or [string]::IsNullOrWhiteSpace($path) -or [IO.Path]::IsPathRooted($path) -or
            $path.Contains('\') -or $path.StartsWith('../',[StringComparison]::Ordinal) -or $path.Contains('/../') -or
            -not $seen.Add($path)) {
            throw "$Description contains an invalid or ambiguous path: $path"
        }
        if ([long]$entry.length -lt 0 -or [long]$entry.lastWriteTimeUtcTicks -le 0) { throw "$Description contains invalid metadata: $path" }
        if ($kind -ceq 'file') {
            if ([string]$entry.sha256 -cnotmatch '^[0-9a-f]{64}$') { throw "$Description file hash is invalid: $path" }
            $files++; $bytes += [long]$entry.length
        }
        else {
            if ([long]$entry.length -ne 0 -or $null -ne $entry.sha256) { throw "$Description directory entry is invalid: $path" }
            $directories++
        }
        $canonical.Add(('{0}|{1}|{2}|{3}|{4}' -f $kind,$path,[long]$entry.length,[long]$entry.lastWriteTimeUtcTicks,$entry.sha256))
        $contentCanonical.Add(('{0}|{1}|{2}|{3}' -f $kind,$path,[long]$entry.length,$entry.sha256))
    }
    if ($files -ne [long]$Inventory.fileCount -or $directories -ne [long]$Inventory.directoryCount -or $bytes -ne [long]$Inventory.totalBytes -or
        (Get-KmcTextSha256 ($canonical -join "`n")) -cne [string]$Inventory.digest -or
        (Get-KmcTextSha256 ($contentCanonical -join "`n")) -cne [string]$Inventory.contentDigest) {
        throw "$Description counts or digests do not reconcile."
    }
    return $Inventory
}

function Assert-KmcQualificationTreeInventoriesEqual {
    param(
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if (($Expected | ConvertTo-Json -Depth 20 -Compress) -cne ($Actual | ConvertTo-Json -Depth 20 -Compress)) {
        throw "$Description changed."
    }
    return $true
}

function Get-KmcQualificationInventoryDifferences {
    param([Parameter(Mandatory = $true)]$Before,[Parameter(Mandatory = $true)]$After)
    $left=@{};foreach($entry in @($Before.entries)){$left[[string]$entry.path]=$entry}
    $right=@{};foreach($entry in @($After.entries)){$right[[string]$entry.path]=$entry}
    return @(@($left.Keys+$right.Keys|Sort-Object -Unique)|Where-Object{
        $a=$left[$_];$b=$right[$_]
        $null -eq $a -or $null -eq $b -or [string]$a.kind -cne [string]$b.kind -or
        [long]$a.length -ne [long]$b.length -or [long]$a.lastWriteTimeUtcTicks -ne [long]$b.lastWriteTimeUtcTicks -or
        [string]$a.sha256 -cne [string]$b.sha256
    })
}

function Assert-KmcPermanentFixtureIdentity {
    param(
        [Parameter(Mandatory = $true)]$Pair,
        [Parameter(Mandatory = $true)]$HistoricalAuthorityRecord
    )
    foreach ($name in @('baseline','working')) {
        $actual=$Pair.$name;$expected=$HistoricalAuthorityRecord.$name
        if (-not [string]::Equals([IO.Path]::GetFullPath([string]$actual.path),[IO.Path]::GetFullPath([string]$expected.path),[StringComparison]::OrdinalIgnoreCase) -or
            [string]$actual.fileName -cne [string]$expected.fileName -or [string]$actual.sha256 -cne [string]$expected.sha256 -or
            [long]$actual.length -ne [long]$expected.length -or [long]$actual.lastWriteTimeUtcTicks -ne [long]$expected.lastWriteTimeUtcTicks) {
            throw "Permanent KMC $name fixture identity differs from historical authority."
        }
    }
    return $true
}

function Read-KmcQualificationSuiteSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedSuiteId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSha256
    )
    $fullState=[IO.Path]::GetFullPath($StateRoot).TrimEnd('\')
    $suiteRoot=Assert-KmcChildPath (Join-Path $fullState 'qualification-suite-snapshots') $fullState 'qualification-suite snapshot root'
    $fullPath=Assert-KmcChildPath $Path $suiteRoot 'qualification-suite snapshot'
    Assert-KmcRecoveryLeafNoLinks $fullPath 'qualification-suite snapshot'
    if ((Get-KmcSha256 $fullPath) -cne $ExpectedSha256) { throw 'Qualification-suite snapshot SHA-256 differs.' }
    $record=Read-KmcJson $fullPath
    Assert-KmcExactProperties $record @(
        'schemaVersion','snapshotKind','suiteId','admittedAtUtc','stabilityIntervalMilliseconds','repository','package',
        'historicalAuthorities','permanentFixture','saveInventory','modsInventory','ownership'
    ) 'qualification-suite snapshot'
    Assert-KmcExactProperties $record.repository @('root','branch','commit') 'qualification-suite repository identity'
    Assert-KmcExactProperties $record.package @('path','sha256','manifestPath','manifestSha256','productVersion','dllSha256','dllMvid') 'qualification-suite package identity'
    Assert-KmcExactProperties $record.ownership @('writableSaveNames','baselineImmutable','foreignSavesWritable','foreignModsWritable','modsStagingMode') 'qualification-suite ownership'
    $admittedAt=[DateTimeOffset]::MinValue
    if ([long]$record.schemaVersion -ne 1 -or [string]$record.snapshotKind -cne 'stable-external-state-qualification-suite' -or
        [string]$record.suiteId -cne $ExpectedSuiteId -or [long]$record.stabilityIntervalMilliseconds -lt 250 -or
        -not [DateTimeOffset]::TryParseExact([string]$record.admittedAtUtc,'o',[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::RoundtripKind,[ref]$admittedAt) -or
        [string]$record.repository.root -cne (Get-KmcRepositoryRoot) -or [string]$record.repository.branch -cnotmatch '^[A-Za-z0-9._/-]{1,200}$' -or
        [string]$record.repository.commit -cnotmatch '^[0-9a-f]{40}$' -or [string]$record.package.sha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$record.package.manifestSha256 -cnotmatch '^[0-9a-f]{64}$' -or [string]$record.package.dllSha256 -cnotmatch '^[0-9a-f]{64}$' -or
        [string]$record.package.dllMvid -cnotmatch '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$' -or
        @($record.ownership.writableSaveNames).Count -ne 1 -or [string]@($record.ownership.writableSaveNames)[0] -cne 'KMC_AUTOMATION_WORKING' -or
        $record.ownership.baselineImmutable -ne $true -or $record.ownership.foreignSavesWritable -ne $false -or
        $record.ownership.foreignModsWritable -ne $false -or [string]$record.ownership.modsStagingMode -cne 'kmc-overlay-only-transactional') { throw 'Qualification-suite snapshot identity or ownership is invalid.' }
    [void](Assert-KmcQualificationTreeInventorySchema -Inventory $record.saveInventory -ExpectedScope save-root -ExpectedRoot ([string]$record.saveInventory.root) -Description 'qualification-suite save inventory')
    [void](Assert-KmcQualificationTreeInventorySchema -Inventory $record.modsInventory -ExpectedScope mods-root -ExpectedRoot ([string]$record.modsInventory.root) -Description 'qualification-suite Mods inventory')
    return [pscustomobject]@{path=$fullPath;sha256=$ExpectedSha256;record=$record}
}

function Assert-KmcQualificationSuiteHistoricalAuthorities {
    param([Parameter(Mandatory = $true)]$History,[Parameter(Mandatory = $true)][string]$StateRoot)
    Assert-KmcExactProperties $History @('protectedSaveAuthorities','modsAuthorities') 'suite historical authorities'
    if ($History.protectedSaveAuthorities -isnot [Array] -or $History.modsAuthorities -isnot [Array] -or
        @($History.protectedSaveAuthorities).Count -lt 1 -or @($History.modsAuthorities).Count -lt 1) {
        throw 'Suite historical authority collections are incomplete.'
    }
    foreach ($authority in @($History.protectedSaveAuthorities)) {
        Assert-KmcExactProperties $authority @('classification','path','sha256','epochId','schemaVersion') 'suite protected-save history entry'
        if ([string]$authority.classification -cnotin @('historical-suite-authority','historical-transition-authority','historical-protected-pin') -or
            [string]$authority.sha256 -cnotmatch '^[0-9a-f]{64}$' -or [long]$authority.schemaVersion -notin @(1,2)) {
            throw 'Suite protected-save history entry is invalid.'
        }
        $path=Assert-KmcChildPath ([string]$authority.path) (Join-Path ([IO.Path]::GetFullPath($StateRoot)) 'protected-save-authorities') 'historical protected-save authority'
        Assert-KmcRecoveryLeafNoLinks $path 'historical protected-save authority'
        if ((Get-KmcSha256 $path) -cne [string]$authority.sha256) { throw 'Historical protected-save authority changed.' }
    }
    foreach ($authority in @($History.modsAuthorities)) {
        Assert-KmcExactProperties $authority @('classification','digest','description') 'suite Mods history entry'
        if ([string]$authority.classification -cnotin @('historical-suite-authority','historical-transition-authority') -or
            [string]$authority.digest -cnotmatch '^[0-9a-f]{64}$' -or [string]::IsNullOrWhiteSpace([string]$authority.description)) {
            throw 'Suite Mods history entry is invalid.'
        }
    }
    return $true
}

function Assert-KmcQualificationSuiteExternalState {
    param(
        [Parameter(Mandatory = $true)]$Snapshot,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$ModsRoot
    )
    Assert-KmcNoGameProcesses
    $save=Get-KmcQualificationTreeInventory -Root $SaveRoot -Scope save-root
    $mods=Get-KmcQualificationTreeInventory -Root $ModsRoot -Scope mods-root
    [void](Assert-KmcQualificationTreeInventorySchema -Inventory $Snapshot.record.saveInventory -ExpectedScope save-root -ExpectedRoot $SaveRoot -Description 'suite recorded save inventory')
    [void](Assert-KmcQualificationTreeInventorySchema -Inventory $Snapshot.record.modsInventory -ExpectedScope mods-root -ExpectedRoot $ModsRoot -Description 'suite recorded Mods inventory')
    [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $Snapshot.record.saveInventory -Actual $save -Description 'qualification-suite save inventory')
    [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $Snapshot.record.modsInventory -Actual $mods -Description 'qualification-suite Mods inventory')
    return [pscustomobject]@{saveInventory=$save;modsInventory=$mods}
}

function Assert-KmcQualificationSuiteContinuity {
    param(
        [Parameter(Mandatory = $true)][string]$SnapshotPath,
        [Parameter(Mandatory = $true)][string]$StateRoot,
        [Parameter(Mandatory = $true)][string]$SaveRoot,
        [Parameter(Mandatory = $true)][string]$ModsRoot,
        [Parameter(Mandatory = $true)][string]$QualificationPath,
        [Parameter(Mandatory = $true)][string]$PackagePath,
        [Parameter(Mandatory = $true)]$PackageManifest,
        [Parameter(Mandatory = $true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedSuiteId,
        [Parameter(Mandatory = $true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedSnapshotSha256
    )
    $snapshot=Read-KmcQualificationSuiteSnapshot -Path $SnapshotPath -StateRoot $StateRoot -ExpectedSuiteId $ExpectedSuiteId -ExpectedSha256 $ExpectedSnapshotSha256
    $record=$snapshot.record;$packageHash=Get-KmcSha256 $PackagePath;$manifestPath=$PackagePath+'.manifest.json'
    if ([string]$record.package.path -cne [IO.Path]::GetFullPath($PackagePath) -or [string]$record.package.sha256 -cne $packageHash -or
        [string]$record.package.manifestPath -cne [IO.Path]::GetFullPath($manifestPath) -or [string]$record.package.manifestSha256 -cne (Get-KmcSha256 $manifestPath) -or
        [string]$record.package.dllSha256 -cne [string]$PackageManifest.dllSha256 -or [string]$record.package.dllMvid -cne [string]$PackageManifest.dllMvid -or
        [string]$record.repository.branch -cne [string]$PackageManifest.branch -or [string]$record.repository.commit -cne [string]$PackageManifest.commit -or
        [string]$record.package.productVersion -cne [string]$PackageManifest.version) { throw 'Qualification-suite snapshot package or repository binding differs.' }
    [void](Assert-KmcQualificationSuiteHistoricalAuthorities -History $record.historicalAuthorities -StateRoot $StateRoot)
    $pair=Assert-KmcFixturePair -SaveRoot $SaveRoot -QualificationPath $QualificationPath
    $fixture=New-KmcRuntimeFixturePayload $pair
    if (($fixture|ConvertTo-Json -Depth 10 -Compress) -cne ($record.permanentFixture|ConvertTo-Json -Depth 10 -Compress)) {
        throw 'Qualification-suite permanent KMC fixture payload differs.'
    }
    $external=Assert-KmcQualificationSuiteExternalState -Snapshot $snapshot -SaveRoot $SaveRoot -ModsRoot $ModsRoot
    $saveMetadata=Get-KmcSaveMetadataInventory $SaveRoot
    return [pscustomobject]@{snapshot=$snapshot;pair=$pair;fixture=$fixture;saveMetadata=$saveMetadata;saveInventory=$external.saveInventory;modsInventory=$external.modsInventory}
}

function Assert-KmcSameQualificationSuiteIdentity {
    param([Parameter(Mandatory = $true)]$First,[Parameter(Mandatory = $true)]$Second)
    foreach ($name in @('suiteId','snapshotSha256')) {
        if ([string]$First.$name -cne [string]$Second.$name) { throw 'A/B processes do not bind the same qualification-suite snapshot.' }
    }
    return $true
}

function Get-KmcQualificationSuiteDriftDisposition {
    param(
        [Parameter(Mandatory = $true)][bool]$ExternalStateExact,
        [Parameter(Mandatory = $true)][bool]$PermanentFixtureExact,
        [Parameter(Mandatory = $true)][bool]$TransactionActive,
        [Parameter(Mandatory = $true)][bool]$PriorProcessRestorationProven
    )
    if (-not $PermanentFixtureExact) { return 'stop-kmc-fixture-drift' }
    if ($ExternalStateExact) { return 'continue-current-suite' }
    if ($TransactionActive -or -not $PriorProcessRestorationProven) { return 'stop-unproven-active-transaction-drift' }
    return 'close-suite-and-restart-fresh-ab'
}
