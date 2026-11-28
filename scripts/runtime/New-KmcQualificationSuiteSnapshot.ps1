[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$SuiteId,
    [Parameter(Mandatory=$true)][string]$PackagePath,
    [Parameter(Mandatory=$true)][string]$ProtectedSaveContinuityAuthorityPath,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9._-]{1,120}$')][string]$ExpectedProtectedSaveContinuityEpochId,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSaveContinuityAuthoritySha256,
    [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedProtectedSavePinSetSha256,
    [ValidateRange(250,10000)][int]$StabilityIntervalMilliseconds=1000
)

$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$requestedWhatIf=[bool]$WhatIfPreference
$WhatIfPreference=$false
. (Join-Path $PSScriptRoot 'RuntimeHarness.Common.ps1')
$repoRoot=Get-KmcRepositoryRoot;$labRoot=Get-KmcLabRoot
$intake=Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json')|ConvertFrom-Json
$stateRoot=Join-Path $labRoot 'runtime-state'
$saveRoot=[string]$intake.requestedLayout.kingmakerSaveRoot
$modsRoot=[string]$intake.requestedLayout.kingmakerModsRoot
$PackagePath=[IO.Path]::GetFullPath($PackagePath);$manifestPath=$PackagePath+'.manifest.json'
$manifest=Assert-KmcPackageManifest $PackagePath $manifestPath
[void](Assert-KmcQualificationAdmissionQuiescent -StateRoot $stateRoot -ModsRoot $modsRoot)

$authorityPath=Assert-KmcChildPath $ProtectedSaveContinuityAuthorityPath (Join-Path $stateRoot 'protected-save-authorities') 'protected-save authority history head'
Assert-KmcRecoveryLeafNoLinks $authorityPath 'protected-save authority history head'
if ((Get-KmcSha256 $authorityPath) -cne $ExpectedProtectedSaveContinuityAuthoritySha256) { throw 'Protected-save authority history head differs.' }
$authority=Read-KmcJson $authorityPath
if ([long]$authority.schemaVersion -ne 2 -or [string]$authority.epochId -cne $ExpectedProtectedSaveContinuityEpochId -or
    [string]$authority.currentProtectedSavePinsSha256 -cne $ExpectedProtectedSavePinSetSha256) { throw 'Protected-save authority history head identity differs.' }
$parentPath=Assert-KmcChildPath ([string]$authority.parentAuthority.path) (Join-Path $stateRoot 'protected-save-authorities') 'protected-save parent authority'
if ((Get-KmcSha256 $parentPath) -cne [string]$authority.parentAuthority.sha256) { throw 'Protected-save parent authority changed.' }
$parent=Read-KmcJson $parentPath
if ([string]$parent.epochId -cne [string]$authority.parentAuthority.epochId -or [long]$parent.schemaVersion -ne 1) { throw 'Protected-save parent authority linkage differs.' }

$qualificationPath=Join-Path $stateRoot 'fixture-qualification.json'
$pairFirst=Assert-KmcFixturePair -SaveRoot $saveRoot -QualificationPath $qualificationPath
[void](Assert-KmcPermanentFixtureIdentity -Pair $pairFirst -HistoricalAuthorityRecord $authority)
$saveFirst=Get-KmcQualificationTreeInventory -Root $saveRoot -Scope save-root
$modsFirst=Get-KmcQualificationTreeInventory -Root $modsRoot -Scope mods-root
Start-Sleep -Milliseconds $StabilityIntervalMilliseconds
[void](Assert-KmcQualificationAdmissionQuiescent -StateRoot $stateRoot -ModsRoot $modsRoot)
$pairSecond=Assert-KmcFixturePair -SaveRoot $saveRoot -QualificationPath $qualificationPath
[void](Assert-KmcPermanentFixtureIdentity -Pair $pairSecond -HistoricalAuthorityRecord $authority)
$saveSecond=Get-KmcQualificationTreeInventory -Root $saveRoot -Scope save-root
$modsSecond=Get-KmcQualificationTreeInventory -Root $modsRoot -Scope mods-root
[void](Assert-KmcQualificationTreeInventoriesEqual -Expected $saveFirst -Actual $saveSecond -Description 'qualification-suite double-scan save inventory')
[void](Assert-KmcQualificationTreeInventoriesEqual -Expected $modsFirst -Actual $modsSecond -Description 'qualification-suite double-scan Mods inventory')
$fixture=New-KmcRuntimeFixturePayload $pairSecond
$record=[ordered]@{
    schemaVersion=1;snapshotKind='stable-external-state-qualification-suite';suiteId=$SuiteId
    admittedAtUtc=[DateTimeOffset]::UtcNow.ToString('o');stabilityIntervalMilliseconds=$StabilityIntervalMilliseconds
    repository=[ordered]@{root=$repoRoot;branch=[string]$manifest.branch;commit=[string]$manifest.commit}
    package=[ordered]@{
        path=$PackagePath;sha256=Get-KmcSha256 $PackagePath;manifestPath=[IO.Path]::GetFullPath($manifestPath)
        manifestSha256=Get-KmcSha256 $manifestPath;productVersion=[string]$manifest.version
        dllSha256=[string]$manifest.dllSha256;dllMvid=[string]$manifest.dllMvid
    }
    historicalAuthorities=[ordered]@{
        protectedSaveAuthorities=@(
            [ordered]@{classification='historical-transition-authority';path=$authorityPath;sha256=$ExpectedProtectedSaveContinuityAuthoritySha256;epochId=$ExpectedProtectedSaveContinuityEpochId;schemaVersion=2},
            [ordered]@{classification='historical-suite-authority';path=$parentPath;sha256=[string]$authority.parentAuthority.sha256;epochId=[string]$authority.parentAuthority.epochId;schemaVersion=1}
        )
        modsAuthorities=@(
            [ordered]@{classification='historical-suite-authority';digest='e62320bbe7d4b83edc128a62f9f5852b0669c5549859602c335c649318578d47';description='last pre-standing-authority qualified whole-Mods inventory'},
            [ordered]@{classification='historical-transition-authority';digest='f91fe3ab6131837b0af285e18e6295fc7ded1486f2892277f7a11acdd5fa2597';description='user-attested external Bag of Tricks and Kingmaker Buff Planner transition'}
        )
    }
    permanentFixture=$fixture;saveInventory=$saveSecond;modsInventory=$modsSecond
    ownership=[ordered]@{
        writableSaveNames=@('KMC_AUTOMATION_WORKING');baselineImmutable=$true
        foreignSavesWritable=$false;foreignModsWritable=$false;modsStagingMode='kmc-overlay-only-transactional'
    }
}
$snapshotRoot=Join-Path $stateRoot 'qualification-suite-snapshots'
$snapshotPath=Join-Path $snapshotRoot ($SuiteId+'.json')
if (Test-Path -LiteralPath $snapshotPath) { throw "Qualification-suite snapshot already exists: $snapshotPath" }
$WhatIfPreference=$requestedWhatIf
if (-not $PSCmdlet.ShouldProcess($snapshotPath,'create append-only stable qualification-suite external-state snapshot')) {
    $WhatIfPreference=$false
    [void](Assert-KmcQualificationAdmissionQuiescent -StateRoot $stateRoot -ModsRoot $modsRoot)
    $saveWhatIf=Get-KmcQualificationTreeInventory -Root $saveRoot -Scope save-root
    $modsWhatIf=Get-KmcQualificationTreeInventory -Root $modsRoot -Scope mods-root
    [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $saveSecond -Actual $saveWhatIf -Description 'qualification-suite WhatIf save inventory')
    [void](Assert-KmcQualificationTreeInventoriesEqual -Expected $modsSecond -Actual $modsWhatIf -Description 'qualification-suite WhatIf Mods inventory')
    Write-Host 'Qualification-suite snapshot WhatIf PASS; both external trees and all project-owned state remain unmodified.'
    return
}
$WhatIfPreference=$false
if (-not (Test-Path -LiteralPath $snapshotRoot -PathType Container)) { New-Item -ItemType Directory -Path $snapshotRoot | Out-Null }
Assert-KmcNotReparsePoint $snapshotRoot 'qualification-suite snapshot root'
Write-KmcJsonCreateNewDurable -Path $snapshotPath -Value $record
$snapshotSha=Get-KmcSha256 $snapshotPath
$validated=Read-KmcQualificationSuiteSnapshot -Path $snapshotPath -StateRoot $stateRoot -ExpectedSuiteId $SuiteId -ExpectedSha256 $snapshotSha
[void](Assert-KmcQualificationSuiteHistoricalAuthorities -History $validated.record.historicalAuthorities -StateRoot $stateRoot)
$saveAfter=Get-KmcQualificationTreeInventory -Root $saveRoot -Scope save-root
$modsAfter=Get-KmcQualificationTreeInventory -Root $modsRoot -Scope mods-root
[void](Assert-KmcQualificationTreeInventoriesEqual -Expected $saveSecond -Actual $saveAfter -Description 'qualification-suite committed save inventory')
[void](Assert-KmcQualificationTreeInventoriesEqual -Expected $modsSecond -Actual $modsAfter -Description 'qualification-suite committed Mods inventory')
[pscustomobject]@{suiteId=$SuiteId;path=$snapshotPath;sha256=$snapshotSha;saveDigest=[string]$saveAfter.digest;modsDigest=[string]$modsAfter.digest;packageSha256=[string]$record.package.sha256;manifestSha256=[string]$record.package.manifestSha256;dllSha256=[string]$record.package.dllSha256;dllMvid=[string]$record.package.dllMvid}|ConvertTo-Json -Compress
