[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2 (docs/missions/mounted-alpha2-melee-pose/MISSION.md), MMR-03/04/05 attack orders and real-time melee: installed-contract and
# production-path checks, split by area from scripts/Test-MountedAlpha2NativeContract.ps1 at 46444b30 (PR #64, not accepted);
# every check line and label below is that script's, verbatim, with the shared preamble.
# MMR-04/05: the installed members the real-time readiness gate binds (CanActInCombat reads the initiative wait and
# combat state) and the native brain target choice the automatic engagement reads (DecisionContext.Enemies,
# TargetInfo.Sort/FillWeight).
# Not covered: any game run.
# chain, and a saved horse's actual facts on load.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$layout = (Get-Content -Raw -LiteralPath (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$data = Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data'
$managed = Join-Path $data 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted alpha 2 native contract: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }
Check (Test-Path -LiteralPath $productDll -PathType Leaf) ('built product present: ' + $productDll)
$script:resolving = @{}
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($sender, $eventArgs)
    $short = ($eventArgs.Name -split ',')[0]
    if ($short.EndsWith('.resources') -or $script:resolving.ContainsKey($short)) { return $null }
    $loaded = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $short }) | Select-Object -First 1
    if ($loaded) { return $loaded }
    $script:resolving[$short] = $true
    try {
        foreach ($dir in @($managed, (Join-Path $managed 'UnityModManager'))) { $path = Join-Path $dir ($short + '.dll'); if (Test-Path -LiteralPath $path) { return [Reflection.Assembly]::LoadFile($path) } }
        return $null
    } finally { $script:resolving.Remove($short) } })
$game = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$kmc = [Reflection.Assembly]::LoadFile($productDll)
function GameType([string]$Name) { $game.GetType($Name, $true) }
function KmcType([string]$Name) { $kmc.GetType($Name, $true) }
function New-Native([Type]$Type) { [Runtime.Serialization.FormatterServices]::GetUninitializedObject($Type) }
$any = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic'
function SetField($Target, [Type]$Type, [string]$Field, $Value) {
    $info = $null; $t = $Type
    while ($null -ne $t -and $null -eq $info) { $info = $t.GetField($Field, [Reflection.BindingFlags]'Instance,Public,NonPublic,DeclaredOnly'); $t = $t.BaseType }
    if ($null -eq $info) { throw ('Missing native field ' + $Type.FullName + '.' + $Field) }
    $info.SetValue($Target, $Value)
}
$module = $game.ManifestModule
function MemberAt([int]$Token) { $module.ResolveMember($Token) }
function IlTokens([Reflection.MethodBase]$Method) { $il = $Method.GetMethodBody().GetILAsByteArray(); $set = @{}; for ($i = 0; $i -le $il.Length - 4; $i++) { $set[[BitConverter]::ToInt32($il, $i)] = $true }; $set }
$combatState = GameType 'Kingmaker.Controllers.Combat.UnitCombatState'
$canAct = $combatState.GetProperty('CanActInCombat').GetGetMethod()
$waiting = $combatState.GetProperty('IsWaitingInitiative', [Reflection.BindingFlags]'Instance,Public,NonPublic').GetGetMethod($true)
$inCombat = $combatState.GetField('m_InCombat', [Reflection.BindingFlags]'Instance,NonPublic')
$canActTokens = IlTokens $canAct
Check ($canActTokens.ContainsKey($waiting.MetadataToken) -and $canActTokens.ContainsKey($inCombat.MetadataToken)) 'MMR-04/05: native CanActInCombat reads m_InCombat and IsWaitingInitiative (the initiative wait before a real-time strike can start)'

# MMR-04 native target choice (review R1-F11): the rider brain's automatic attack (Character_Brain -> AttackAiAction ->
# AttackTargetsPriority: StandStill, UnconsciousPenalty, NotImpatient) scores candidates from DecisionContext.Enemies, which
# reads the group's memory (perceived enemies), filters dead/untargetable/aggro, and sorts with TargetInfo.Sort; FillWeight
# ranks a player's manual target first and, in real time, subtracts 0.001 x distance, so the nearest eligible perceived enemy
# wins. KMC passes that proposal through unchanged (no target selection of its own).
$decision = GameType 'Kingmaker.Controllers.Brain.DecisionContext'; $targetInfo = GameType 'Kingmaker.Controllers.Brain.TargetInfo'
$enemiesTokens = IlTokens ($decision.GetProperty('Enemies', [Reflection.BindingFlags]'Instance,Public,NonPublic').GetGetMethod($true))
$memoryEnemies = (GameType 'Kingmaker.UnitLogic.UnitGroupMemory').GetProperty('Enemies').GetGetMethod()
$sortMethod = $targetInfo.GetMethod('Sort', [Reflection.BindingFlags]'Static,Public,NonPublic')
Check ($enemiesTokens.ContainsKey($memoryEnemies.MetadataToken) -and $enemiesTokens.ContainsKey($sortMethod.MetadataToken)) 'MMR-04: native brain candidates come from the group memory and are sorted by TargetInfo.Sort'
$fill = $targetInfo.GetMethod('FillWeight', [Reflection.BindingFlags]'Instance,Public,NonPublic')
$fillIl = $fill.GetMethodBody().GetILAsByteArray()
$fillTokens = IlTokens $fill
$manual = (GameType 'Kingmaker.Controllers.Combat.UnitCombatState').GetProperty('ManualTarget').GetGetMethod()
$distanceField = $targetInfo.GetField('Distance')
$minusMilli = [BitConverter]::GetBytes([single]-0.001); $hasMinusMilli = $false
for ($i = 0; $i -le $fillIl.Length - 5; $i++) { if ($fillIl[$i] -eq 0x22 -and $fillIl[$i + 1] -eq $minusMilli[0] -and $fillIl[$i + 2] -eq $minusMilli[1] -and $fillIl[$i + 3] -eq $minusMilli[2] -and $fillIl[$i + 4] -eq $minusMilli[3]) { $hasMinusMilli = $true } }
Check ($fillTokens.ContainsKey($manual.MetadataToken) -and $fillTokens.ContainsKey($distanceField.MetadataToken) -and $hasMinusMilli) 'MMR-04: native FillWeight keeps a manual target first and prefers the nearer enemy (-0.001 x distance)'
Write-Host ('Mounted alpha 2 native contract (orders) PASS=' + $script:passed + ' FAIL=0')
