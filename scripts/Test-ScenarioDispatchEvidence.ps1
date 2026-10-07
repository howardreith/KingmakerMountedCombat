$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/Chunk6aHarnessIdentity.ps1')
$passed=0
function Check([bool]$ok,[string]$why) { if(-not$ok){throw $why};$script:passed++ }
foreach($scenario in @('chunk6b-charge-rt','chunk6b-charge-core-rt','chunk6b-charge-interruption-rt','chunk6b-charge-lifecycle-rt','chunk6b-charge-tb')) {
 Check (Test-KmcPhase3dSchemaRegistration 43 $scenario) ('Current charge schema refused: '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 42 $scenario)) ('Stale charge schema admitted: '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 43 $scenario).validator-ceq'Assert-KmcChunk6bChargeEvidence') ('Charge reader changed: '+$scenario)
}
foreach($scenario in @('chunk6b-charge-path-rt','chunk6b-charge-path-tb')) {
 Check (Test-KmcPhase3dSchemaRegistration 33 $scenario) ('Carrier schema refused: '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 43 $scenario)) ('Charge schema crosses carrier boundary: '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 33 $scenario).validator-ceq'Assert-KmcChunk6bChargePathEvidence') ('Carrier reader changed: '+$scenario)
}
foreach($scenario in @('chunk4-charge-safety-rt','chunk4-charge-safety-tb')) {
 Check (Test-KmcPhase3dSchemaRegistration 41 $scenario) ('Stock safety schema refused: '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 41 $scenario).validator-ceq'Assert-KmcChunk4ChargeEvidence') ('Stock safety reader changed: '+$scenario)
}
Check (-not(Test-KmcPhase3dSchemaRegistration 44 'chunk6b-charge-rt')) 'Unknown schema admitted'
Check (-not(Test-KmcPhase3dSchemaRegistration 43 'ordinary-attack-controls-tb')) 'Charge schema admitted for ordinary attacks'
Check ((Get-KmcPhase3dEvidenceDispatch 17 'ordinary-attack-controls-tb').validator-ceq'Assert-KmcOrdinaryAttackControlsEvidence') 'Ordinary reader changed'
Check ((Get-KmcPhase3dEvidenceDispatch 30 'chunk6a-combat-mount-rt').validator-ceq'Assert-KmcChunk6aCombatMountEvidence') 'Relationship reader changed'
Check ((Get-KmcPhase3dEvidenceDispatch 9 'phase3h-combat-loop-rt').validator-ceq'Assert-KmcPhase3hLoopEvidence') 'Historical loop reader changed'
Check (Test-KmcPhase3dSchemaRegistration 1 'phase3d-unified-combat-rt-suite') 'Legacy schema compatibility changed'
Check ($null-eq(Get-KmcPhase3dEvidenceDispatch 1 'phase3d-unified-combat-rt-suite')) 'Legacy fallback reader changed'
Check (Test-KmcChunk6aPureReaderPath 'scripts/runtime/ScenarioDispatchEvidence.ps1') 'Pure metadata is not under the existing reader guard'
Check (-not(Test-KmcChunk6aPureReaderPath 'scripts/runtime/RuntimeHarness.Common.ps1')) 'Protected Common module was whitelisted'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Check ('scripts/runtime/ScenarioDispatchEvidence.ps1'-cin@(Get-KmcChunk6aReaderFiles $root)) 'Metadata omitted from exact reader identity'
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')