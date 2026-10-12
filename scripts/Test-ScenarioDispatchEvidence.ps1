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
Check (-not(Test-KmcPhase3dSchemaRegistration 45 'chunk6b-charge-rt')) 'Staged schema admitted for a foreign scenario'
Check (-not(Test-KmcPhase3dSchemaRegistration 46 'chunk6c-casting-rt')) 'Reaction schema admitted for a casting root'
Check (-not(Test-KmcPhase3dSchemaRegistration 47 'chunk6b-charge-rt')) 'Unregistered schema admitted'
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
# Compound 6C request roots: child case rows only, one dedicated validator, registered exactly once.
foreach($scenario in @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb')) {
 Check (Test-KmcCompoundRuntimeScenario $scenario) ('Compound 6C request root not registered: '+$scenario)
 Check (@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$scenario}).Count-eq1) ('Compound root must stay registered exactly once, never duplicated: '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 44 $scenario).validator-ceq'Assert-KmcChunk6cCastingEvidence') ('Compound root lacks its dedicated validator: '+$scenario)
}
foreach($scenario in @('chunk6b-charge-core-rt','chunk6b-charge-rt','chunk6a-combat-mount-rt','persistence-p04-save','mod-load-smoke','C6C-rider-incapacity','CHUNK6C-CASTING-RT','')) {
 Check (-not(Test-KmcCompoundRuntimeScenario $scenario)) ('Individual scenario, row or inexact name treated as compound: '+$scenario)
}
foreach($scenario in @('chunk6d-staged-rt','chunk6d-staged-tb')) {
 Check (Test-KmcCompoundRuntimeScenario $scenario) ('Compound 6D request root not registered: '+$scenario)
 Check (@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$scenario}).Count-eq1) ('Compound 6D root must stay registered exactly once: '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 45 $scenario).validator-ceq'Assert-KmcChunk6dStagedEvidence') ('Compound 6D root lacks its dedicated validator: '+$scenario)
 Check (Test-KmcPhase3dSchemaRegistration 45 $scenario) ('6D schema not registered: '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 44 $scenario)) ('6D root admits the casting schema: '+$scenario)
}
foreach($scenario in @('chunk6e-reaction-rt','chunk6e-reaction-tb')) {
 Check (Test-KmcCompoundRuntimeScenario $scenario) ('Compound 6E request root not registered: '+$scenario)
 Check (@(Get-KmcPhase3dHorseRuntimeRows|Where-Object {$_-ceq$scenario}).Count-eq1) ('Compound 6E root must stay registered exactly once: '+$scenario)
 Check ((Get-KmcPhase3dEvidenceDispatch 46 $scenario).validator-ceq'Assert-KmcChunk6eReactionEvidence') ('Compound 6E root lacks its dedicated validator: '+$scenario)
 Check (Test-KmcPhase3dSchemaRegistration 46 $scenario) ('6E schema not registered: '+$scenario)
 Check (-not(Test-KmcPhase3dSchemaRegistration 45 $scenario)) ('6E root admits the staged schema: '+$scenario)
}
foreach($scenario in @('C6D-move-cast-move','C6E-reaction-window','CHUNK6D-STAGED-RT')) {
 Check (-not(Test-KmcCompoundRuntimeScenario $scenario)) ('Row or inexact 6D/6E name treated as compound: '+$scenario)
}
$chunk7Families = [ordered]@{
 'chunk7-native-main-bootstrap'='Test-KmcChunk7NativeBootstrapScenario'
 'chunk7-bootstrap-cold-inventory'='Test-KmcChunk7BootstrapInventoryScenario'
 'chunk7-two-pair-exploration'='world-fixed-root'
 'chunk7-small-profile-calibration'='Test-KmcChunk7SmallScenario'
 'chunk7-ordinary-acquisition'='Test-KmcChunk7OrdinaryScenario'
 'chunk7-ordinary-field-admission'='Test-KmcChunk7FieldScenario'
 'chunk7-native-progression'='Test-KmcChunk7ProgressionScenario'
 'chunk7-persistence-save'='Test-KmcChunk7PersistenceScenario'
 'chunk7-persistence-load'='Test-KmcChunk7PersistenceScenario'
 'chunk7-two-pair-combat-rider-first'='Test-KmcChunk7CombatScenario'
 'chunk7-two-pair-combat-mount-first'='Test-KmcChunk7CombatScenario'
 'chunk7-two-pair-realtime-core'='Test-KmcChunk7RealtimeScenario'
 'chunk7-two-pair-realtime-casting'='Test-KmcChunk7RealtimeScenario'
 'chunk7-two-pair-realtime-charge'='Test-KmcChunk7RealtimeScenario'
 'chunk7-two-pair-native-life-cleanup'='Test-KmcChunk7NativeLifeScenario'
}
$expectedCompound = @('chunk6c-casting-rt','chunk6c-casting-tb','chunk6c-casting-unmounted-rt','chunk6c-casting-unmounted-tb','chunk6d-staged-rt','chunk6d-staged-tb','chunk6e-reaction-rt','chunk6e-reaction-tb') + @($chunk7Families.Keys)
$actualCompound = @(Get-KmcCompoundRuntimeScenarios)
Check ($actualCompound.Count-eq$expectedCompound.Count-and$null-eq(Compare-Object $expectedCompound $actualCompound -CaseSensitive)) 'Compound registry differs from reviewed exact families'
foreach($scenario in $chunk7Families.Keys) {
 Check (Test-KmcCompoundRuntimeScenario $scenario) ('Selected Chunk 7 compound request missing: '+$scenario)
 Check (@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_-ceq$scenario}).Count-eq1) ('Selected Chunk 7 request must be registered once: '+$scenario)
 $readers=@($chunk7Families.Values|Where-Object {$_-cne'world-fixed-root'}|Select-Object -Unique|Where-Object {& $_ $scenario})
 if($scenario-ceq'chunk7-two-pair-exploration'){
  Check ($readers.Count-eq0-and@(Get-KmcChunk7WorldRows|Where-Object {$_-ceq$scenario}).Count-eq1) 'World fixed root crosses another reader family'
 }else{
  Check ($readers.Count-eq1-and$readers[0]-ceq$chunk7Families[$scenario]) ('Chunk 7 root lacks one exact dedicated evidence family: '+$scenario)
 }
 Check (-not(Test-KmcCompoundRuntimeScenario $scenario.ToUpperInvariant())) ('Inexact Chunk 7 root admitted: '+$scenario)
}

Check (Test-KmcCompoundRuntimeScenario 'chunk7-two-pair-exploration') 'Chunk 7 world compound request missing'
Check (@(Get-KmcSaveBackedRuntimeScenarios|Where-Object {$_-ceq'chunk7-two-pair-exploration'}).Count-eq1) 'World request must be registered once'
Check (-not(Test-KmcCompoundRuntimeScenario 'chunk7-two-pair-combat')) 'Unimplemented Chunk 7 combat case admitted'
Write-Host ('TOTAL PASS='+$passed+' FAIL=0')
