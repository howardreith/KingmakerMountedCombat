[CmdletBinding()]
param()
# Sprint 2 (r07): the Working-fixture traversal suite must run under the existing Areas isolation (intake tree held,
# engine-generated tree quarantined with its digest), and no unrelated route gains that write authority by accident.
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'runtime/RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime/PersistenceProfileAreasIsolation.ps1')
$pass=0;$fail=0
function Check([string]$Name,[bool]$Condition){if($Condition){$script:pass++;Write-Host ('PASS '+$Name)}else{$script:fail++;Write-Host ('FAIL '+$Name)}}
Check 'traversal-core suite selects Areas isolation' (Test-KmcPersistenceAreasIsolationSelection -Scenario 'chunk4-traversal-core' -PersistenceCase '' -ObserveWorkingDeserialization $false)
Check 'two-pair owner-seed exploration (isolated save root) does not' (-not(Test-KmcPersistenceAreasIsolationSelection -Scenario 'chunk7-owned-two-pair-campaign' -PersistenceCase '' -ObserveWorkingDeserialization $false))
Check 'other Working-fixture movement suites are not widened' (-not(Test-KmcPersistenceAreasIsolationSelection -Scenario 'chunk4-traversal-slope' -PersistenceCase '' -ObserveWorkingDeserialization $false))
Check 'working deserialization observation still excludes it' (-not(Test-KmcPersistenceAreasIsolationSelection -Scenario 'chunk4-traversal-core' -PersistenceCase '' -ObserveWorkingDeserialization $true))
Check 'P07 load keeps its selection' (Test-KmcPersistenceAreasIsolationSelection -Scenario 'persistence-p07-load' -PersistenceCase '' -ObserveWorkingDeserialization $false)
# The r07 retained inventory: the only profile additions were the engine's unpack of the loaded archive under Areas/.
$r07='C:\Dev\KingmakerMountedCombatLab\runtime-backups\profile-20261030-sprint2-r07-traversal-core-dungeon-door\snapshot.json'
if(Test-Path -LiteralPath $r07){
    $snapshot=[IO.File]::ReadAllText($r07,[Text.Encoding]::UTF8)|ConvertFrom-Json
    $residue=Get-ChildItem -LiteralPath 'C:\Dev\KingmakerMountedCombatLab\runtime-backups\20261030-sprint2-r07-traversal-core-dungeon-door\engine-areas-residue' -File -ErrorAction SilentlyContinue
    $areasEntries=@($snapshot.inventory.entries|Where-Object{([string]$_.path).StartsWith('Areas/')})
    Check 'r07 intake held no Areas leaves (empty engine cache at intake)' ($areasEntries.Count-eq0)
    Check 'r07 residue is exactly the seven archive members the engine unpacked' ($residue.Count-eq7-and@($residue|Where-Object{$_.Name-in@('header.png','history','player.json','party.json','9d1278a2f599b2a4daab53abdfe88d2e.json','9d1278a2f599b2a4daab53abdfe88d2e.fog.png','c49315fe499f0e5468af6f19242499a2.json')}).Count-eq7)
}
Write-Host ('TOTAL PASS='+$pass+' FAIL='+$fail+'; Areas isolation selection contracts only, no native qualification')
if($fail-ne0){exit 1}
