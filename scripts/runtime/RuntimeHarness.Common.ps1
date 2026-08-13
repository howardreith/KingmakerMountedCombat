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
    param([Parameter(Mandatory = $true)][string]$Text)
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
    $canonical = ($ordered | ForEach-Object { '{0}|{1}|{2}|{3}' -f $_.kind, $_.path, $_.length, $_.sha256 }) -join "`n"
    return [pscustomobject]@{
        schemaVersion = 1; root = $fullRoot
        directoryCount = @($ordered | Where-Object kind -eq 'directory').Count
        fileCount = @($ordered | Where-Object kind -eq 'file').Count
        totalBytes = [long](($ordered | Where-Object kind -eq 'file' | Measure-Object length -Sum).Sum)
        digest = Get-KmcTextSha256 $canonical; entries = $ordered
    }
}

function Get-KmcProtectedSaveMetadata {
    param([Parameter(Mandatory = $true)][string]$SaveRoot)
    $fullRoot = [IO.Path]::GetFullPath($SaveRoot).TrimEnd('\')
    if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) { throw "Save root is missing: $fullRoot" }
    $records = @()
    foreach ($file in @(Get-ChildItem -LiteralPath $fullRoot -File -Recurse -Force | Sort-Object FullName)) {
        $records += [pscustomobject]@{
            path = $file.FullName.Substring($fullRoot.Length + 1).Replace('\', '/')
            length = [long]$file.Length
            lastWriteTimeUtcTicks = $file.LastWriteTimeUtc.Ticks
        }
    }
    $canonical = ($records | ForEach-Object { '{0}|{1}|{2}' -f $_.path, $_.length, $_.lastWriteTimeUtcTicks }) -join "`n"
    return [pscustomobject]@{ schemaVersion = 1; fileCount = $records.Count; totalBytes = [long](($records | Measure-Object length -Sum).Sum); digest = Get-KmcTextSha256 $canonical }
}

function Assert-KmcNoGameProcesses {
    $kingmaker = @(Get-Process -Name Kingmaker -ErrorAction SilentlyContinue)
    $wrath = @(Get-Process -Name Wrath -ErrorAction SilentlyContinue)
    $installers = @(Get-Process -Name UnityModManager -ErrorAction SilentlyContinue)
    if ($kingmaker.Count -ne 0 -or $wrath.Count -ne 0 -or $installers.Count -ne 0) {
        throw "Runtime state is ambiguous: Kingmaker=$($kingmaker.Count), Wrath=$($wrath.Count), UnityModManager=$($installers.Count)."
    }
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
    $allowedState = @($requiredState + @('stagedAtUtc','restoredAtUtc','restoredDigest'))
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
        Assert-KmcManifestMatchesState (Get-KmcDirectoryManifest $fullLive) $state 'staged'
        if (Test-Path -LiteralPath $stagedAfter) { throw 'Owned staged-after quarantine already exists; restoration is ambiguous.' }
        Move-Item -LiteralPath $fullLive -Destination $stagedAfter
    }
    Move-Item -LiteralPath $backup -Destination $fullLive
    $restored=Get-KmcDirectoryManifest $fullLive; Assert-KmcManifestMatchesState $restored $state 'before'
    $state.phase='restored'; $state | Add-Member -NotePropertyName restoredAtUtc -NotePropertyValue ([DateTime]::UtcNow.ToString('o')) -Force; $state | Add-Member -NotePropertyName restoredDigest -NotePropertyValue $restored.digest -Force; Write-KmcJsonAtomic $StatePath $state
    return $restored
}
