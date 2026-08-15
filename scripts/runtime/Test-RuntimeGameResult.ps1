[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameResultPath,
    [Parameter(Mandatory = $true)][string]$RequestPath,
    [Parameter(Mandatory = $true)][string]$FingerprintPath,
    [Parameter(Mandatory = $true)][int]$ExpectedProcessId,
    [Parameter(Mandatory = $true)][DateTimeOffset]$NotBeforeUtc,
    [switch]$RequirePass,
    [switch]$VerifyLiveWorkingIdentity,
    [AllowNull()][string]$ExpectedLiveWorkingPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')

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

function Test-ExactJsonInteger {
    param($Value)
    return $Value -is [sbyte] -or $Value -is [byte] -or
        $Value -is [int16] -or $Value -is [uint16] -or
        $Value -is [int32] -or $Value -is [uint32] -or
        $Value -is [int64] -or $Value -is [uint64]
}

function Assert-RuntimeArtifactManifest {
    param(
        [Parameter(Mandatory = $true)]$Request,
        [Parameter(Mandatory = $true)]$ExpectedSha256
    )

    if ($ExpectedSha256 -isnot [string] -or $ExpectedSha256 -cnotmatch '^[0-9a-f]{64}$') {
        throw 'Runtime game-result evidenceManifestSha256 is not an exact lowercase SHA-256.'
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
        throw 'Runtime artifact manifest hash does not match the runtime game result.'
    }

    Assert-NoDuplicateJsonObjectProperties $manifestPath 'runtime artifact manifest'
    $manifest = Read-KmcJson $manifestPath
    Assert-KmcExactProperties $manifest @('schemaVersion','runId','scenario','createdAtUtc','artifacts') 'runtime artifact manifest'
    if (-not (Test-ExactJsonInteger $manifest.schemaVersion) -or [long]$manifest.schemaVersion -ne 1) {
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

        $allowed = ($relativePath -ceq 'lifecycle-scenario-evidence.jsonl' -and $kind -ceq 'scenario-evidence') -or
            ($relativePath -ceq 'movement-telemetry.jsonl' -and $kind -ceq 'telemetry') -or
            ($relativePath -ceq 'movement-scenario-evidence.jsonl' -and $kind -ceq 'scenario-evidence') -or
            ($relativePath -ceq 'boundary-scenario-evidence.jsonl' -and $kind -ceq 'boundary-evidence') -or
            ($relativePath -cmatch '^movement-visuals/[A-Za-z0-9._-]+\.png$' -and $kind -ceq 'screenshot')
        if (-not $allowed) { throw "Runtime artifact manifest record is outside the exact allowlist: $relativePath ($kind)" }
        if (-not (Test-ExactJsonInteger $artifact.length) -or [long]$artifact.length -le 0 -or
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

function Assert-FixtureEcho {
    param($Actual, $Expected)
    Assert-KmcExactProperties $Actual @('baseline','working','writeAuthorization') 'runtime game-result fixture'
    foreach ($kind in @('baseline','working')) {
        Assert-KmcExactProperties $Actual.$kind @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') "runtime game-result $kind descriptor"
        foreach ($name in @('internalName','fileName','sha256','gameId','gameName','area')) {
            if ([string]$Actual.$kind.$name -cne [string]$Expected.$kind.$name) { throw "Runtime game-result fixture mismatch: $kind.$name" }
        }
        foreach ($name in @('length','lastWriteTimeUtcTicks')) {
            if ([long]$Actual.$kind.$name -ne [long]$Expected.$kind.$name) { throw "Runtime game-result fixture mismatch: $kind.$name" }
        }
    }
    Assert-KmcExactProperties $Actual.writeAuthorization @('mode','allowedInternalName','allowedFileName','baselineImmutable') 'runtime game-result write authorization'
    foreach ($name in @('mode','allowedInternalName','allowedFileName')) {
        if ([string]$Actual.writeAuthorization.$name -cne [string]$Expected.writeAuthorization.$name) { throw "Runtime game-result fixture mismatch: writeAuthorization.$name" }
    }
    if ($Actual.writeAuthorization.baselineImmutable -ne $Expected.writeAuthorization.baselineImmutable) { throw 'Runtime game-result fixture mismatch: writeAuthorization.baselineImmutable' }
}

function Assert-SubscenarioResults {
    param($Game)
    $missionScenarios = @(
        'mod-load-smoke', 'export-mounted-contracts', 'export-candidate-mount-rigs', 'observe-mount-diagnostic-availability',
        'player-action-availability', 'mount-dismount-user-flow',
        'mounted-pair-create-and-clear', 'mounted-pair-double-mount-rejected', 'mounted-pair-invalid-pair-rejected',
        'mounted-pair-cleanup-idempotent', 'mounted-pair-death-cleanup', 'mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup', 'mounted-pair-mod-disable-cleanup', 'mounted-pair-open-ground',
        'mounted-pair-stop-start', 'mounted-pair-turns-and-corners', 'mounted-pair-doorway', 'mounted-pair-selection',
        'mounted-pair-party-formation', 'mounted-pair-pause-unpause', 'mounted-pair-destination-cancel',
        'mounted-pair-turn-based-entry-cleanup', 'mounted-pair-realtime-entry-cleanup', 'mounted-pair-save-safety',
        'mounted-pair-load-safety', 'mounted-pair-area-transition-safety'
    )
    if ($null -eq $Game.subscenarioResults -or $Game.subscenarioResults -is [string]) { throw 'Runtime game-result subscenarioResults must be an array.' }
    $items = @($Game.subscenarioResults)
    if ($items.Count -eq 0) { throw 'Runtime game-result contains no named subscenarios.' }
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $pass = 0; $fail = 0; $assertionPass = 0; $assertionFail = 0
    foreach ($item in $items) {
        Assert-KmcExactProperties $item @('name','status','assertionPassCount','assertionFailCount','errors') 'runtime game-result subscenario'
        if (@($missionScenarios | Where-Object { $_ -ceq [string]$item.name }).Count -ne 1) { throw "Runtime game-result contains an unknown subscenario: $($item.name)" }
        if (-not $names.Add([string]$item.name)) { throw "Runtime game-result contains a duplicate subscenario: $($item.name)" }
        if ([string]$item.status -cnotin @('PASS','FAIL')) { throw 'Runtime game-result subscenario status is invalid.' }
        if ([int]$item.assertionPassCount -lt 0 -or [int]$item.assertionFailCount -lt 0 -or ([int]$item.assertionPassCount + [int]$item.assertionFailCount) -eq 0) { throw 'Runtime game-result subscenario assertion totals are invalid.' }
        if ($null -eq $item.errors -or $item.errors -is [string]) { throw 'Runtime game-result subscenario errors must be an array.' }
        if ([string]$item.status -ceq 'PASS') {
            $pass++
            if ([int]$item.assertionFailCount -ne 0 -or @($item.errors).Count -ne 0) { throw 'PASS runtime subscenario contains failures or errors.' }
        }
        else {
            $fail++
            if ([int]$item.assertionFailCount -eq 0) { throw 'FAIL runtime subscenario lacks a failed assertion.' }
        }
        $assertionPass += [int]$item.assertionPassCount
        $assertionFail += [int]$item.assertionFailCount
    }
    if ([int]$Game.subscenarioTotal -ne $items.Count -or [int]$Game.subscenarioPassCount -ne $pass -or
        [int]$Game.subscenarioFailCount -ne $fail -or $pass + $fail -ne $items.Count -or
        [int]$Game.assertionPassCount -ne $assertionPass -or [int]$Game.assertionFailCount -ne $assertionFail) {
        throw 'Runtime game-result subscenario totals do not match the named results.'
    }
    if (@($missionScenarios | Where-Object { $_ -ceq [string]$Game.scenario }).Count -eq 1 -and -not $names.Contains([string]$Game.scenario)) { throw 'Individual runtime scenario did not report its own named result.' }
}

& (Join-Path $PSScriptRoot 'Test-RuntimeRequest.ps1') -RequestPath $RequestPath
Assert-NoDuplicateJsonObjectProperties $GameResultPath 'runtime game result'
$game = Read-KmcJson $GameResultPath
$request = Read-KmcJson $RequestPath
$fingerprint = Read-KmcJson $FingerprintPath
$schemaVersion = [int]$request.schemaVersion
$commonRequired = @('schemaVersion','runId','scenario','status','branch','commit','productVersion','dllSha256','dllMvid','transactionToken','startedAtUtc','completedAtUtc','loadedModId','gameVersion','gameAssemblySha256','gameAssemblyMvid','ummVersion','ummSha256','harmony12Version','harmony12Sha256','relationshipState','movementExperimentEnabled','processId','currentGameMode','loadedAreaPresent','saveRequestCount','loadRequestCount','frameCount','elapsedSeconds','errors')
if ($schemaVersion -eq 1) {
    Assert-KmcExactProperties $game $commonRequired 'runtime game result v1'
}
elseif ($schemaVersion -eq 2) {
    $v2Fields = @('fixture','fixtureIdentityVerified','baselineLoadRequestCount','workingLoadRequestCount','workingSaveRequestCount','suppressedWorkingSaveRequestCount','unauthorizedLoadRequestCount','unauthorizedSaveRequestCount','subscenarioTotal','subscenarioPassCount','subscenarioFailCount','assertionPassCount','assertionFailCount','evidenceManifestSha256','subscenarioResults')
    Assert-KmcExactProperties $game @($commonRequired + $v2Fields) 'runtime game result v2'
}
else { throw 'Runtime game-result request schema is unsupported.' }
if ([int]$game.schemaVersion -ne $schemaVersion) { throw 'Runtime game-result schemaVersion does not match the request.' }
foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')) {
    if ([string]$game.$name -cne [string]$request.$name) { throw "Game result identity mismatch: $name" }
}
$started = [DateTimeOffset]::MinValue; $completed = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$game.startedAtUtc,[ref]$started) -or
    -not [DateTimeOffset]::TryParse([string]$game.completedAtUtc,[ref]$completed) -or
    $started -lt $NotBeforeUtc.AddSeconds(-5) -or $completed -lt $started -or
    $completed -gt [DateTimeOffset]::UtcNow.AddMinutes(1)) { throw 'Runtime game-result timestamps are invalid or outside the run.' }
$gameAuthority = @($fingerprint.kingmaker.files | Where-Object role -eq 'gameplayAssembly')[0]
$umm = @($fingerprint.kingmaker.files | Where-Object role -eq 'umm')[0]
$harmony = @($fingerprint.kingmaker.files | Where-Object role -eq 'harmony')[0]
if ([string]$game.status -cnotin @('PASS','FAIL') -or [string]$game.loadedModId -cne 'KingmakerMountedCombat' -or
    [string]$game.gameVersion -cne [string]$fingerprint.kingmaker.displayVersion -or
    [string]$game.gameAssemblySha256 -cne [string]$gameAuthority.sha256 -or [string]$game.gameAssemblyMvid -cne [string]$gameAuthority.mvid -or
    [string]$game.ummVersion -cne '0.28.2.0' -or [string]$game.ummSha256 -cne [string]$umm.sha256 -or
    [string]$game.harmony12Version -cne '1.2.0.1' -or [string]$game.harmony12Sha256 -cne [string]$harmony.sha256) { throw 'Runtime game-result platform identity is not exact.' }
if ([int]$game.processId -ne $ExpectedProcessId -or [string]::IsNullOrWhiteSpace([string]$game.currentGameMode) -or
    [int]$game.frameCount -lt 0 -or [double]$game.elapsedSeconds -lt 0.0 -or
    $null -eq $game.errors -or $game.errors -is [string]) { throw 'Runtime game-result process, timing, mode, or error shape is invalid.' }
if ([string]$game.status -ceq 'PASS' -and
    ([int]$game.frameCount -lt 10 -or [double]$game.elapsedSeconds -lt 1.0 -or @($game.errors).Count -ne 0)) {
    throw 'PASS runtime game result does not satisfy timing or zero-error qualification.'
}
if ([string]$game.status -ceq 'FAIL' -and @($game.errors).Count -eq 0) {
    throw 'FAIL runtime game result does not contain a structured error.'
}
if ($RequirePass -and [string]$game.status -cne 'PASS') { throw 'Runtime game result did not qualify as PASS.' }

if ($schemaVersion -eq 1) {
    if ([string]$game.relationshipState -cne 'Unmounted' -or $game.movementExperimentEnabled -ne $false -or
        $game.loadedAreaPresent -ne $false -or [int]$game.saveRequestCount -ne 0 -or [int]$game.loadRequestCount -ne 0) { throw 'Runtime game-result safety state is not an unmounted no-save smoke PASS.' }
    Write-Host 'TOTAL PASS=24 FAIL=0'
    return
}

Assert-FixtureEcho $game.fixture $request.fixture
Assert-RuntimeArtifactManifest $request $game.evidenceManifestSha256
if ([int]$game.loadRequestCount -ne ([int]$game.baselineLoadRequestCount + [int]$game.workingLoadRequestCount + [int]$game.unauthorizedLoadRequestCount) -or
    [int]$game.saveRequestCount -ne ([int]$game.workingSaveRequestCount + [int]$game.suppressedWorkingSaveRequestCount + [int]$game.unauthorizedSaveRequestCount)) { throw 'Save-backed runtime aggregate save/load counters do not reconcile.' }
Assert-SubscenarioResults $game
$validatedArtifactManifest = Read-KmcJson (Join-Path ([IO.Path]::GetFullPath([string]$request.evidenceRoot)) 'runtime-artifacts.json')
Assert-KmcLifecycleScenarioEvidence -Request $request -Manifest $validatedArtifactManifest -Status ([string]$game.status) -SubscenarioResults $game.subscenarioResults
Assert-KmcMovementScenarioEvidence -Request $request -Manifest $validatedArtifactManifest -Status ([string]$game.status) -SubscenarioResults $game.subscenarioResults
Assert-KmcBoundaryScenarioEvidence -Request $request -Manifest $validatedArtifactManifest -Status ([string]$game.status) -SubscenarioResults $game.subscenarioResults -GameResult $game -VerifyLiveWorkingIdentity:$VerifyLiveWorkingIdentity -ExpectedLiveWorkingPath $ExpectedLiveWorkingPath
if ([string]$game.status -ceq 'PASS') {
    if ($game.fixtureIdentityVerified -ne $true -or [string]$game.relationshipState -cne 'Unmounted') { throw 'Save-backed PASS did not finish with verified fixture identity and an unmounted relationship.' }
    $expectedWorkingLoads = if ([string]$game.scenario -cin @('mounted-pair-load-safety','boundary-suite')) { 2 } else { 1 }
    if ([int]$game.baselineLoadRequestCount -ne 0 -or [int]$game.unauthorizedLoadRequestCount -ne 0 -or
        [int]$game.unauthorizedSaveRequestCount -ne 0 -or [int]$game.workingLoadRequestCount -ne $expectedWorkingLoads -or
        [int]$game.loadRequestCount -ne $expectedWorkingLoads -or [int]$game.workingSaveRequestCount -ne 0) {
        throw 'Save-backed PASS crossed its exact scenario-bound load/save quota.'
    }
    $expectedSuppressedSaves = if ([string]$game.scenario -ceq 'native-save-clean-dismount') { 1 } else { 0 }
    if ([int]$game.suppressedWorkingSaveRequestCount -ne $expectedSuppressedSaves -or
        [int]$game.saveRequestCount -ne $expectedSuppressedSaves) {
        throw 'Save-backed PASS crossed its exact suppressed-save request quota.'
    }
    if ($game.movementExperimentEnabled -ne $false -or $game.loadedAreaPresent -ne $true -or
        [string]$game.currentGameMode -cne 'Default') { throw 'Save-backed PASS did not restore its exact game-mode and diagnostic-setting boundary.' }
    if ([int]$game.subscenarioFailCount -ne 0 -or [int]$game.assertionFailCount -ne 0) { throw 'PASS runtime game result contains subscenario failures.' }
}
else {
    if ([int]$game.subscenarioFailCount -lt 1 -or [int]$game.assertionFailCount -lt 1) { throw 'FAIL runtime game result lacks a failed subscenario assertion.' }
}
Write-Host "TOTAL PASS=$(if([string]$game.status -ceq 'PASS'){39}else{34}) FAIL=0"
