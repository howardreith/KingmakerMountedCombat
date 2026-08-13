[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ResultPath,
    [Parameter(Mandatory = $true)][string]$RequestPath
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

function Assert-FixtureEcho {
    param($Actual, $Expected)
    Assert-KmcExactProperties $Actual @('baseline','working','writeAuthorization') 'runtime result fixture'
    foreach ($kind in @('baseline','working')) {
        Assert-KmcExactProperties $Actual.$kind @('internalName','fileName','sha256','length','lastWriteTimeUtcTicks','gameId','gameName','area') "runtime result $kind descriptor"
        foreach ($name in @('internalName','fileName','sha256','gameId','gameName','area')) {
            if ([string]$Actual.$kind.$name -cne [string]$Expected.$kind.$name) { throw "Runtime result fixture mismatch: $kind.$name" }
        }
        foreach ($name in @('length','lastWriteTimeUtcTicks')) {
            if ([long]$Actual.$kind.$name -ne [long]$Expected.$kind.$name) { throw "Runtime result fixture mismatch: $kind.$name" }
        }
    }
    Assert-KmcExactProperties $Actual.writeAuthorization @('mode','allowedInternalName','allowedFileName','baselineImmutable') 'runtime result write authorization'
    foreach ($name in @('mode','allowedInternalName','allowedFileName')) {
        if ([string]$Actual.writeAuthorization.$name -cne [string]$Expected.writeAuthorization.$name) { throw "Runtime result fixture mismatch: writeAuthorization.$name" }
    }
    if ($Actual.writeAuthorization.baselineImmutable -ne $Expected.writeAuthorization.baselineImmutable) { throw 'Runtime result fixture mismatch: writeAuthorization.baselineImmutable' }
}

function Assert-SubscenarioResults {
    param($Result)
    $missionScenarios = @(
        'mod-load-smoke', 'export-mounted-contracts', 'export-candidate-mount-rigs', 'observe-mount-diagnostic-availability',
        'mounted-pair-create-and-clear', 'mounted-pair-double-mount-rejected', 'mounted-pair-invalid-pair-rejected',
        'mounted-pair-cleanup-idempotent', 'mounted-pair-death-cleanup', 'mounted-pair-combat-start-cleanup',
        'mounted-pair-area-unload-cleanup', 'mounted-pair-mod-disable-cleanup', 'mounted-pair-open-ground',
        'mounted-pair-stop-start', 'mounted-pair-turns-and-corners', 'mounted-pair-doorway', 'mounted-pair-selection',
        'mounted-pair-party-formation', 'mounted-pair-pause-unpause', 'mounted-pair-destination-cancel',
        'mounted-pair-turn-based-entry-cleanup', 'mounted-pair-realtime-entry-cleanup', 'mounted-pair-save-safety',
        'mounted-pair-load-safety', 'mounted-pair-area-transition-safety'
    )
    if ($null -eq $Result.subscenarioResults -or $Result.subscenarioResults -is [string]) { throw 'Runtime result subscenarioResults must be an array.' }
    $items = @($Result.subscenarioResults)
    if ($items.Count -eq 0) { throw 'Runtime result contains no named subscenarios.' }
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $pass = 0; $fail = 0; $assertionPass = 0; $assertionFail = 0
    foreach ($item in $items) {
        Assert-KmcExactProperties $item @('name','status','assertionPassCount','assertionFailCount','errors') 'runtime result subscenario'
        if (@($missionScenarios | Where-Object { $_ -ceq [string]$item.name }).Count -ne 1) { throw "Runtime result contains an unknown subscenario: $($item.name)" }
        if (-not $names.Add([string]$item.name)) { throw "Runtime result contains a duplicate subscenario: $($item.name)" }
        if ([string]$item.status -cnotin @('PASS','FAIL')) { throw 'Runtime result subscenario status is invalid.' }
        if ([int]$item.assertionPassCount -lt 0 -or [int]$item.assertionFailCount -lt 0 -or ([int]$item.assertionPassCount + [int]$item.assertionFailCount) -eq 0) { throw 'Runtime result subscenario assertion totals are invalid.' }
        if ($null -eq $item.errors -or $item.errors -is [string]) { throw 'Runtime result subscenario errors must be an array.' }
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
    if ([int]$Result.subscenarioTotal -ne $items.Count -or [int]$Result.subscenarioPassCount -ne $pass -or
        [int]$Result.subscenarioFailCount -ne $fail -or $pass + $fail -ne $items.Count -or
        [int]$Result.assertionPassCount -ne $assertionPass -or [int]$Result.assertionFailCount -ne $assertionFail) {
        throw 'Runtime result subscenario totals do not match the named results.'
    }
    if (@($missionScenarios | Where-Object { $_ -ceq [string]$Result.scenario }).Count -eq 1 -and -not $names.Contains([string]$Result.scenario)) { throw 'Individual runtime scenario did not report its own named result.' }
}

& (Join-Path $PSScriptRoot 'Test-RuntimeRequest.ps1') -RequestPath $RequestPath
Assert-NoDuplicateJsonObjectProperties $ResultPath 'runtime result'
$result = Read-KmcJson $ResultPath
$request = Read-KmcJson $RequestPath
$schemaVersion = [int]$request.schemaVersion
$commonRequired = @('schemaVersion','runId','scenario','status','branch','commit','productVersion','dllSha256','dllMvid','transactionToken','startedAtUtc','completedAtUtc','modsRestored','saveProtectionPassed','gameResultSha256','errors')
if ($schemaVersion -eq 1) {
    Assert-KmcExactProperties $result $commonRequired 'runtime result v1'
}
elseif ($schemaVersion -eq 2) {
    $v2Fields = @('fixture','baselineImmutable','workingRestored','saveWriteAllowlistPassed','restoredSaveInventoryDigest','subscenarioTotal','subscenarioPassCount','subscenarioFailCount','assertionPassCount','assertionFailCount','subscenarioResults')
    Assert-KmcExactProperties $result @($commonRequired + $v2Fields) 'runtime result v2'
}
else { throw 'Runtime result request schema is unsupported.' }
if ([int]$result.schemaVersion -ne $schemaVersion) { throw 'Runtime result schemaVersion does not match its request.' }
foreach ($name in @('runId','scenario','branch','commit','productVersion','dllSha256','dllMvid','transactionToken')) {
    if ([string]$result.$name -cne [string]$request.$name) { throw "Runtime result identity mismatch: $name" }
}
if ([string]$result.status -cnotin @('PASS','FAIL')) { throw 'Runtime result status must be PASS or FAIL.' }
$started = [DateTimeOffset]::MinValue; $completed = [DateTimeOffset]::MinValue
if (-not [DateTimeOffset]::TryParse([string]$result.startedAtUtc,[ref]$started) -or
    -not [DateTimeOffset]::TryParse([string]$result.completedAtUtc,[ref]$completed) -or $completed -lt $started) { throw 'Runtime result timestamps are invalid or reversed.' }
if ($result.modsRestored -ne $true) { throw 'Runtime result does not prove exact Mods restoration.' }
if ($null -eq $result.errors -or $result.errors -is [string]) { throw 'Runtime result errors must be an array.' }
if ([string]$result.status -ceq 'PASS' -and @($result.errors).Count -ne 0) { throw 'PASS runtime result contains errors.' }
if ([string]$result.status -ceq 'PASS' -and [string]$result.gameResultSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'PASS runtime result lacks the atomic game-result hash.' }

if ($schemaVersion -eq 1) {
    if ($result.saveProtectionPassed -ne $true) { throw 'Runtime result does not prove no-save protection.' }
    Write-Host 'TOTAL PASS=11 FAIL=0'
    return
}

Assert-FixtureEcho $result.fixture $request.fixture
if ($result.baselineImmutable -ne $true -or $result.workingRestored -ne $true -or $result.saveWriteAllowlistPassed -ne $true) { throw 'Runtime result does not prove Baseline immutability, exact Working restoration, and the write allowlist.' }
if ($result.saveProtectionPassed -ne ($result.baselineImmutable -and $result.workingRestored -and $result.saveWriteAllowlistPassed)) { throw 'Runtime result saveProtectionPassed does not equal its three fixture safety proofs.' }
if ([string]$result.restoredSaveInventoryDigest -cnotmatch '^[0-9a-f]{64}$') { throw 'Runtime result lacks the restored save inventory digest.' }
Assert-SubscenarioResults $result
if ([string]$result.status -ceq 'PASS' -and ([int]$result.subscenarioFailCount -ne 0 -or [int]$result.assertionFailCount -ne 0)) { throw 'PASS runtime result contains subscenario failures.' }
Write-Host 'TOTAL PASS=29 FAIL=0'
