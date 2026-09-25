[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ObserverResultPath,
    [Parameter(Mandatory = $true)][string]$RequestPath,
    [Parameter(Mandatory = $true)][string]$ObserverRequestPath,
    [Parameter(Mandatory = $true)][string]$GameResultPath,
    [Parameter(Mandatory = $true)][int]$ExpectedProcessId,
    [Parameter(Mandatory = $true)][DateTimeOffset]$NotBeforeUtc,
    [Parameter(Mandatory = $true)][string]$SourceArchivePath
)

# Validates the removal observer's own result (the genuine no-DLL cleanup-save
# observation) against the run's request, the observer request the process was
# bound to, and the archive bytes as they lie on disk afterwards, then composes
# the run's game-result record in the shape the harness result binds. Nothing
# here scores the observation: every verdict below is the observer's own check,
# re-read and cross-checked against the launcher's own records.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'PersistenceSaveFixtures.ps1')
. (Join-Path $PSScriptRoot 'PersistenceValidationFixtures.ps1')

function Assert-NoDuplicateJsonObjectProperties {
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

$checks = 0
Assert-NoDuplicateJsonObjectProperties $ObserverResultPath 'observer result'
$result = Read-KmcJson $ObserverResultPath
$request = Read-KmcJson $RequestPath
$observerRequest = Read-KmcJson $ObserverRequestPath
if ([string]$request.scenario -cne 'persistence-p07-load' -or [string]$request.persistenceCase -cne 'removal-no-dll') { throw 'The observer result belongs only to the no-DLL removal case.' }
Assert-KmcExactProperties $result @('schemaVersion','evidenceKind','runId','transactionToken','processId','startedAtUtc','completedAtUtc','status',
    'observerVersion','observerAssembly','requestSha256','stage','checks','checkPassCount','checkFailCount','errors','observations') 'observer result'
$started = [DateTimeOffset]::MinValue; $completed = [DateTimeOffset]::MinValue
if ([int]$result.schemaVersion -ne 1 -or [string]$result.evidenceKind -cne 'kmc-removal-observer' -or
    [string]$result.runId -cne [string]$request.runId -or [string]$result.transactionToken -cne [string]$request.transactionToken -or
    [int]$result.processId -ne $ExpectedProcessId -or
    -not [DateTimeOffset]::TryParse([string]$result.startedAtUtc, [ref]$started) -or -not [DateTimeOffset]::TryParse([string]$result.completedAtUtc, [ref]$completed) -or
    $started -lt $NotBeforeUtc.AddSeconds(-5) -or $completed -lt $started -or
    [string]$result.observerAssembly -cne 'KmcRemovalObserver' -or [string]$result.observerVersion -cne [string]$observerRequest.observer.version -or
    [string]$result.requestSha256 -cne (Get-KmcSha256 $ObserverRequestPath) -or [string]$result.status -cnotin @('PASS','FAIL')) {
    throw 'Observer result identity does not bind this run, process, token and observer request.'
}
$checks++
$checkRows = @($result.checks)
$passedRows = @($checkRows | Where-Object { $_.passed -eq $true })
$failedRows = @($checkRows | Where-Object { $_.passed -ne $true })
foreach ($row in $checkRows) { Assert-KmcExactProperties $row @('name','passed') 'observer check' }
if ([int]$result.checkPassCount -ne $passedRows.Count -or [int]$result.checkFailCount -ne $failedRows.Count -or
    (([string]$result.status -ceq 'PASS') -ne ($failedRows.Count -eq 0 -and @($result.errors).Count -eq 0))) {
    throw 'Observer result status does not follow its own checks and errors.'
}
$checks++
if ([string]$result.status -cne 'PASS') {
    Write-Host ('OBSERVER RESULT FAIL: ' + (@($result.errors) -join ' | '))
}
else {
    $required = @('no-KMC-directory-or-DLL-under-Mods','no-KMC-assembly-loaded-in-the-process','UMM-lists-the-observer-and-no-KMC-entry',
        'no-KMC-Harmony-patch-owner','native-save-and-stash-paths-resolve-inside-the-owned-profile','cleanup-archive-bytes-are-exactly-the-prepared-ones',
        'engine-reads-the-cleanup-archive-header-as-declared','the-cleanup-archive-opened-its-own-campaign-and-area','the-world-holds-a-party-and-no-KMC-blueprint-unit',
        'the-engine-raised-no-exception-while-loading-without-KMC','the-engine-logged-nothing-about-KMC-or-its-save-member',
        'the-main-character-moved-through-ordinary-native-input-without-KMC')
    $names = @($passedRows | ForEach-Object { [string]$_.name })
    foreach ($name in $required) { if ($names -cnotcontains $name) { throw "Observer PASS lacks its required check $name." } }
    if ($names.Count -ne $required.Count) { throw 'Observer PASS carries checks outside its declared set.' }
    $checks++
    $o = $result.observations
    Assert-KmcExactProperties $o @('routing','absence','descriptor','world','movement','archive','log') 'observer observations'
    $a = $o.absence
    if ($a.kmcAssemblyLoaded -ne $false -or $a.kmcModsDirectoryPresent -ne $false -or @($a.kmcModsDirectories).Count -ne 0 -or @($a.kmcDllFiles).Count -ne 0 -or
        @($a.kmcAssemblies).Count -ne 0 -or @($a.kmcModEntries).Count -ne 0 -or $a.observerModEntryPresent -ne $true -or @($a.kmcHarmonyOwners).Count -ne 0 -or
        -not [string]::Equals([string]$a.modsRoot, [IO.Path]::GetFullPath([string]$observerRequest.modsRoot).TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase) -or
        @($a.modDirectories | Where-Object { $_ -match '(?i)KingmakerMountedCombat' }).Count -ne 0 -or @($a.modDirectories) -cnotcontains 'KmcRemovalObserver') {
        throw 'The observer did not prove KMC genuinely absent from the Mods tree, the process, UMM and Harmony.'
    }
    $checks++
    $profileRoot = [IO.Path]::GetFullPath([string]$observerRequest.profileRoot).TrimEnd('\')
    if ([string]$o.routing.savePath -cne (Join-Path $profileRoot 'Saved Games') -or [string]$o.routing.stashFolder -cne (Join-Path $profileRoot 'Areas')) {
        throw 'Native save routing did not resolve inside the owned profile.'
    }
    $checks++
    $load = $request.persistenceLoad
    $d = $o.descriptor
    if ($null -eq $d -or [string]$d.name -cne [string]$load.internalName -or [string]$d.type -cne 'Manual' -or [string]$d.gameId -cne [string]$load.gameId -or
        [string]$d.gameName -cne [string]$load.gameName -or [string]$d.area -cne [string]$load.area -or [string]$d.fileName -cne [string]$load.fileName -or
        [string]$d.path -cne (Join-Path ([string]$o.routing.savePath) ([string]$load.fileName))) {
        throw 'The engine did not read the declared cleanup archive header.'
    }
    $checks++
    $w = $o.world
    if ([string]$w.gameId -cne [string]$load.gameId -or [string]$w.area -cne [string]$load.area -or [string]$w.mode -cne 'Default' -or
        [int]$w.party -lt 1 -or @($w.partyIds).Count -ne [int]$w.party -or [int]$w.kmcBlueprintUnits -ne 0 -or @($w.kmcBlueprintUnitIds).Count -ne 0 -or
        [string]::IsNullOrEmpty([string]$w.mainCharacter) -or @($w.partyIds) -cnotcontains [string]$w.mainCharacter -or $w.inCombat -ne $false) {
        throw 'The loaded world is not the cleanup archive own campaign, area and party with no KMC unit.'
    }
    $checks++
    $m = $o.movement
    if ([string]$m.mover -cne [string]$w.mainCharacter -or [double]$m.displacement -lt 1.5 -or $m.inCombat -ne $false -or [string]$m.mode -cne 'Default') {
        throw 'The main character did not move through ordinary native input without KMC.'
    }
    $checks++
    $g = $o.log
    if ([int]$g.loadWindowExceptions -ne 0 -or @($g.kmcRelated).Count -ne 0 -or [int]$g.exceptions -lt 0 -or [int]$g.errors -lt [int]$g.exceptions) {
        throw 'The engine raised an exception while loading without KMC, or logged something about KMC.'
    }
    $checks++
    $ar = $o.archive
    $archivePath = [string]$ar.path
    if ([string]$ar.fileName -cne [string]$load.fileName -or [string]$ar.sha256Before -cne [string]$load.sha256 -or
        $archivePath -cne (Join-Path ([string]$o.routing.savePath) ([string]$load.fileName)) -or -not (Test-Path -LiteralPath $archivePath -PathType Leaf) -or
        (Get-KmcSha256 $archivePath) -cne [string]$ar.sha256After -or $ar.changedOnDisk -ne ([string]$ar.sha256After -cne [string]$ar.sha256Before)) {
        throw 'The observed archive is not the prepared cleanup save as it lies on disk.'
    }
    $checks++
}
# The archive after the engine's own load: every native member the source run
# wrote must be byte-identical; only the engine's own header (its LoadedTimes
# update) may differ, and the KMC member is reported as retained or dropped.
if ((Get-KmcSha256 $SourceArchivePath) -cne [string]$request.persistenceLoad.sha256) { throw 'The source cleanup archive changed since it was copied.' }
$before = Get-KmcValidationMemberHashes $SourceArchivePath
$archiveOnDisk = Join-Path ([string]$observerRequest.profileRoot) ('Saved Games\' + [string]$request.persistenceLoad.fileName)
$after = if (Test-Path -LiteralPath $archiveOnDisk -PathType Leaf) { Get-KmcValidationMemberHashes $archiveOnDisk } else { $null }
$changed = @(); $missing = @(); $added = @()
if ($null -ne $after) {
    foreach ($name in $before.Keys) { if (-not $after.ContainsKey($name)) { $missing += $name } elseif ($after[$name] -cne $before[$name]) { $changed += $name } }
    foreach ($name in $after.Keys) { if (-not $before.ContainsKey($name)) { $added += $name } }
}
$kmcRetained = $null -ne $after -and $after.ContainsKey('kmc-mounted-state') -and $before.ContainsKey('kmc-mounted-state') -and $after['kmc-mounted-state'] -ceq $before['kmc-mounted-state']
if ([string]$result.status -ceq 'PASS') {
    if ($null -eq $after -or $missing.Count -ne 0 -or $added.Count -ne 0 -or @($changed | Where-Object { $_ -cne 'header.json' }).Count -ne 0) {
        throw ('The engine changed a native member of the cleanup archive during the no-DLL load: changed=' + ($changed -join ',') + ' missing=' + ($missing -join ',') + ' added=' + ($added -join ','))
    }
    $checks++
}
$evidenceManifestSha256 = Get-KmcValidatedOrchestrationArtifactManifestHash -Request $request -AllowIncompleteScenarioEvidence
# Schema 2 is what the run result binds (Test-RuntimeResult): the same shape
# every KMC-hosted save-backed game result carries, with the observer's fields.
$game = [ordered]@{
    schemaVersion = 2; evidenceKind = 'kmc-removal-observer'; runId = [string]$request.runId; scenario = [string]$request.scenario
    persistenceCase = [string]$request.persistenceCase; status = [string]$result.status
    branch = [string]$request.branch; commit = [string]$request.commit; productVersion = [string]$request.productVersion
    dllSha256 = [string]$request.dllSha256; dllMvid = [string]$request.dllMvid; kmcDllInstalled = $false
    observer = $observerRequest.observer; transactionToken = [string]$request.transactionToken; processId = [int]$result.processId
    startedAtUtc = [string]$result.startedAtUtc; completedAtUtc = [string]$result.completedAtUtc; fixture = $request.fixture
    observerResultSha256 = (Get-KmcSha256 $ObserverResultPath); observerRequestSha256 = (Get-KmcSha256 $ObserverRequestPath)
    evidenceManifestSha256 = $evidenceManifestSha256
    archiveMembers = [ordered]@{ sourcePath = [IO.Path]::GetFullPath($SourceArchivePath); changed = @($changed); missing = @($missing); added = @($added); kmcMemberRetained = $kmcRetained }
    checkPassCount = [int]$result.checkPassCount; checkFailCount = [int]$result.checkFailCount; errors = @($result.errors | ForEach-Object { [string]$_ })
    observations = $result.observations; checks = @($result.checks)
    subscenarioResults = @([ordered]@{ name = [string]$request.scenario; status = [string]$result.status
        assertionPassCount = [int]$result.checkPassCount; assertionFailCount = [int]$result.checkFailCount; errors = @($result.errors | ForEach-Object { [string]$_ }) })
}
Write-KmcJsonAtomic $GameResultPath $game
Write-Host "OBSERVER RESULT CHECKS PASS=$checks FAIL=0 status=$($result.status)"
