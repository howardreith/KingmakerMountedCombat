[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RequestPath,
    [string]$PackageManifestPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

function Assert-NoDuplicateJsonObjectProperties {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Description)
    Add-Type -AssemblyName System.Runtime.Serialization
    $reader = $null
    try {
        $reader = [Runtime.Serialization.Json.JsonReaderWriterFactory]::CreateJsonReader(
            [IO.File]::ReadAllBytes([IO.Path]::GetFullPath($Path)),
            [Xml.XmlDictionaryReaderQuotas]::Max)
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

function Test-JsonInteger($Value) {
    return $Value -is [byte] -or $Value -is [sbyte] -or $Value -is [int16] -or $Value -is [uint16] -or
        $Value -is [int32] -or $Value -is [uint32] -or $Value -is [int64] -or $Value -is [uint64]
}

function Assert-RuntimeSaveDescriptor {
    param($Descriptor, [string]$Kind)
    Assert-KmcExactProperties $Descriptor @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') "runtime $Kind descriptor"
    $expectedName = if ($Kind -ceq 'baseline') { 'KMC_AUTOMATION_BASELINE' } else { 'KMC_AUTOMATION_WORKING' }
    $escapedName = [Regex]::Escape($expectedName)
    if ([string]$Descriptor.internalName -cne $expectedName) { throw "Runtime $Kind internal name is not exact." }
    if ([string]$Descriptor.fileName -cnotmatch "^Manual_[0-9]+_$escapedName\.zks$") { throw "Runtime $Kind filename is not a canonical fixture leaf." }
    if ([string]$Descriptor.sha256 -cnotmatch '^[0-9a-f]{64}$') { throw "Runtime $Kind SHA-256 is invalid." }
    if (-not (Test-JsonInteger $Descriptor.length) -or [long]$Descriptor.length -le 0) { throw "Runtime $Kind length is invalid." }
    if (-not (Test-JsonInteger $Descriptor.lastWriteTimeUtcTicks) -or [long]$Descriptor.lastWriteTimeUtcTicks -le 0 -or [long]$Descriptor.lastWriteTimeUtcTicks -gt [DateTime]::MaxValue.Ticks) { throw "Runtime $Kind timestamp ticks are invalid." }
    $parsedGameId = [Guid]::Empty
    if (-not [Guid]::TryParse([string]$Descriptor.gameId, [ref]$parsedGameId)) { throw "Runtime $Kind GameId is invalid." }
    if ([string]::IsNullOrWhiteSpace([string]$Descriptor.gameName)) { throw "Runtime $Kind GameName is empty." }
    if ([string]$Descriptor.area -cnotmatch '^[0-9a-f]{32}$') { throw "Runtime $Kind Area is not an exact lowercase blueprint GUID." }
}

function Assert-RuntimeFixture {
    param($Fixture, [Parameter(Mandatory = $true)][string]$Scenario)
    Assert-KmcExactProperties $Fixture @('baseline','working','writeAuthorization') 'runtime fixture'
    Assert-RuntimeSaveDescriptor $Fixture.baseline 'baseline'
    Assert-RuntimeSaveDescriptor $Fixture.working 'working'
    if ([string]::Equals([string]$Fixture.baseline.fileName, [string]$Fixture.working.fileName, [StringComparison]::OrdinalIgnoreCase)) { throw 'Runtime fixture leaves are not distinct.' }
    foreach ($name in @('gameId','gameName','area')) {
        if ([string]$Fixture.baseline.$name -cne [string]$Fixture.working.$name) { throw "Runtime fixture raw $name values do not match exactly." }
    }
    $authorization = $Fixture.writeAuthorization
    Assert-KmcExactProperties $authorization @('mode','allowedInternalName','allowedFileName','baselineImmutable') 'runtime fixture write authorization'
    if ($Scenario -ceq 'manual-visual-review') {
        if ([string]$authorization.mode -cne 'read-only' -or
            $null -ne $authorization.allowedInternalName -or
            $null -ne $authorization.allowedFileName -or
            $authorization.baselineImmutable -ne $true) {
            throw 'Manual review fixture is not exact read-only authorization.'
        }
    }
    elseif ([string]$authorization.mode -cne 'working-only' -or
        [string]$authorization.allowedInternalName -cne 'KMC_AUTOMATION_WORKING' -or
        [string]$authorization.allowedFileName -cne [string]$Fixture.working.fileName -or
        $authorization.baselineImmutable -ne $true) {
        throw 'Automated runtime fixture does not authorize writes solely to the exact Working descriptor.'
    }
}

Assert-NoDuplicateJsonObjectProperties $RequestPath 'runtime request'
$request = Read-KmcJson $RequestPath
$schemaVersion = [int]$request.schemaVersion
$commonRequired = @('schemaVersion','runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken','evidenceRoot')
if ($schemaVersion -eq 1) {
    Assert-KmcExactProperties $request @($commonRequired + @('saveAccessAllowed','saveName')) 'runtime request v1'
}
elseif ($schemaVersion -eq 2) {
    Assert-KmcExactProperties $request @($commonRequired + @('fixture','qualificationSuite')) 'runtime request v2'
}
else { throw 'Runtime request schemaVersion must be 1 or 2.' }

$missionScenarios = @(
    'mod-load-smoke', 'export-mounted-contracts', 'export-candidate-mount-rigs', 'observe-mount-diagnostic-availability', 'horse-native-asset-audit', 'horse-companion-blueprint-registration', 'horse-companion-unmounted-suite', 'horse-mounted-alpha-suite',
    'player-action-availability', 'mount-dismount-user-flow',
    'mounted-pair-create-and-clear', 'mounted-pair-double-mount-rejected', 'mounted-pair-invalid-pair-rejected',
    'mounted-pair-cleanup-idempotent', 'mounted-pair-death-cleanup', 'mounted-pair-combat-start-cleanup',
    'mounted-pair-area-unload-cleanup', 'mounted-pair-mod-disable-cleanup',
    'mounted-pair-combat-start-retained', 'mounted-pair-combat-end-retained',
    'mounted-pair-rider-death-cleanup', 'mounted-pair-mount-death-cleanup',
    'mounted-pair-rider-incapacitated-cleanup', 'mounted-pair-mount-incapacitated-cleanup',
    'mounted-pair-rider-native-incapacitated-cleanup', 'mounted-pair-mount-native-incapacitated-cleanup',
    'mounted-pair-companion-removal-cleanup', 'mounted-pair-view-destroyed-cleanup', 'mounted-pair-exception-cleanup',
    'mounted-pair-open-ground',
    'mounted-pair-stop-start', 'mounted-pair-turns-and-corners', 'mounted-pair-doorway', 'mounted-distance-door-interaction', 'mounted-pair-selection',
    'mounted-pair-party-formation', 'mounted-pair-pause-unpause', 'mounted-pair-destination-cancel',
    'mounted-pair-turn-based-entry-cleanup', 'mounted-pair-realtime-entry-cleanup', 'mounted-pair-save-safety',
    'mounted-pair-load-safety', 'mounted-pair-area-transition-safety',
    'native-save-clean-dismount', 'native-area-clean-dismount', 'native-mode-transition-cleanup',
    'presentation-residue-and-uninstall-safety', 'pose-idle', 'pose-walk-run', 'pose-turn-stop',
    'pose-doorway-formation', 'pose-equipment-variants', 'ui-selection-portrait-actionbar',
    'camera-follow-and-command-routing', 'mounted-rider-melee-hit-rt', 'mounted-rider-melee-hit-tb',
    'mounted-rider-melee-miss-rt', 'mounted-mammoth-primary-hit-rt', 'mounted-mammoth-primary-hit-tb',
    'mounted-rider-melee-move-to-attack-rt', 'mounted-rider-melee-move-to-attack-tb',
    'mounted-rider-melee-command-cancel-rt', 'mounted-rider-melee-command-cancel-tb',
    'mounted-rider-melee-command-interrupt-rt', 'mounted-rider-melee-command-interrupt-tb',
    'mounted-rider-melee-combat-end-rt', 'mounted-rider-melee-combat-end-tb',
    'mounted-rider-melee-human-play-path-rt', 'mounted-rider-melee-human-play-path-tb'
)
$aggregateScenarios = @('fixture-intake','lifecycle-suite','combat-lifecycle-suite','movement-suite','boundary-suite','presentation-suite','combat-core-control-suite')
$interactiveScenarios = @('manual-visual-review')

if ([string]$request.runId -cnotmatch '^[A-Za-z0-9._-]{1,120}$') { throw 'Runtime request runId is invalid.' }
if ([string]$request.branch -cnotmatch '^codex/mounted-combat-[A-Za-z0-9._/-]+$') { throw 'Runtime request branch is outside the KMC prefix.' }
if ([string]$request.commit -cnotmatch '^[0-9a-f]{40}$') { throw 'Runtime request commit must be a full lowercase Git SHA.' }
$expectedProductVersion = [string](Read-KmcJson (Join-Path (Get-KmcRepositoryRoot) 'version.json')).productVersion
if ([string]$request.productVersion -cne $expectedProductVersion) { throw 'Runtime request product version is not exact.' }
if ([string]$request.dllSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'Runtime request DLL SHA-256 is invalid.' }
if ([string]$request.transactionToken -cnotmatch '^[0-9a-f]{64}$') { throw 'Runtime request transaction token is invalid.' }
$parsedMvid = [Guid]::Empty
if (-not [Guid]::TryParse([string]$request.dllMvid, [ref]$parsedMvid)) { throw 'Runtime request DLL MVID is invalid.' }

$evidenceRoot = [IO.Path]::GetFullPath([string]$request.evidenceRoot)
$permittedEvidence = [IO.Path]::GetFullPath((Join-Path (Get-KmcLabRoot) 'runtime-evidence')).TrimEnd('\')
if (-not $evidenceRoot.StartsWith($permittedEvidence + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Runtime request evidenceRoot escaped the KMC runtime-evidence root.' }

if ($schemaVersion -eq 1) {
    if ([string]$request.scenario -cne 'mod-load-smoke') { throw 'Schema-v1 runtime is restricted to mod-load-smoke.' }
    if ($request.saveAccessAllowed -ne $false -or $null -ne $request.saveName) { throw 'Schema-v1 runtime request is not an exact no-save request.' }
}
else {
    if (@($missionScenarios + $aggregateScenarios + $interactiveScenarios | Where-Object { $_ -ceq [string]$request.scenario }).Count -ne 1 -or [string]$request.scenario -ceq 'mod-load-smoke') { throw 'Schema-v2 scenario is outside the save-backed mission allowlist.' }
    Assert-RuntimeFixture $request.fixture ([string]$request.scenario)
    Assert-KmcExactProperties $request.qualificationSuite @('suiteId','snapshotSha256') 'runtime qualification-suite identity'
    if ([string]$request.qualificationSuite.suiteId -cnotmatch '^[A-Za-z0-9._-]{1,120}$' -or
        [string]$request.qualificationSuite.snapshotSha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Runtime qualification-suite identity is invalid.'
    }
}

if (-not [string]::IsNullOrWhiteSpace($PackageManifestPath)) {
    Assert-NoDuplicateJsonObjectProperties $PackageManifestPath 'package manifest'
    $manifest = Read-KmcJson $PackageManifestPath
    if ([string]$manifest.commit -cne [string]$request.commit -or
        [string]$manifest.branch -cne [string]$request.branch -or
        [string]$manifest.version -cne [string]$request.productVersion -or
        [string]$manifest.dllSha256 -cne [string]$request.dllSha256 -or
        [string]$manifest.dllMvid -cne [string]$request.dllMvid -or
        $manifest.worktreeClean -ne $true -or $manifest.qualificationEligible -ne $true -or [int]$manifest.schemaVersion -ne 2) {
        throw 'Runtime request does not match the clean package manifest.'
    }
}

$passCount = if ($schemaVersion -eq 1) { 12 } else { 33 }
Write-Host "TOTAL PASS=$passCount FAIL=0"
