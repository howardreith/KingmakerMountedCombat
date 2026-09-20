[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$labRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$intake = Get-Content -Raw -LiteralPath (Join-Path $labRoot 'environment-intake.json') | ConvertFrom-Json
$managed = Join-Path ([string]$intake.requestedLayout.kingmakerInstallDir) 'Kingmaker_Data\Managed'
$path = Join-Path $managed 'Assembly-CSharp.dll'
$passes = 0
function Assert-Phase3f([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "FAIL $Message" }
    $script:passes++
    Write-Host "PASS $Message"
}
Assert-Phase3f ((Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant() -ceq '3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb') 'exact installed Kingmaker resource-audit assembly'
[AppDomain]::CurrentDomain.add_ReflectionOnlyAssemblyResolve({param($sender,$eventArgs)
    $candidate = Join-Path $managed (($eventArgs.Name -split ',')[0] + '.dll')
    if (Test-Path -LiteralPath $candidate) { return [Reflection.Assembly]::ReflectionOnlyLoadFrom($candidate) }
    return $null
})
$assembly = [Reflection.Assembly]::ReflectionOnlyLoadFrom($path)
$flags = [Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
function Method([string]$Type, [string]$Name) { return $assembly.GetType($Type,$true).GetMethod($Name,$flags) }
function Contains-Token($Method, [int]$Token) {
    $bytes = $Method.GetMethodBody().GetILAsByteArray()
    $needle = [BitConverter]::GetBytes($Token)
    for ($index=0; $index -le $bytes.Length-4; $index++) {
        if ($bytes[$index] -eq $needle[0] -and $bytes[$index+1] -eq $needle[1] -and
            $bytes[$index+2] -eq $needle[2] -and $bytes[$index+3] -eq $needle[3]) { return $true }
    }
    return $false
}
$turn = 'TurnBased.Controllers.TurnController'
$combat = 'TurnBased.Controllers.CombatController'
$prepare = Method $turn 'Prepare'
$cooldownField = $assembly.GetType($turn).GetField('m_Cooldown',$flags)
$clear = $cooldownField.FieldType.GetMethod('Clear',$flags)
Assert-Phase3f ($prepare.MetadataToken -eq 0x06000C3C -and (Contains-Token $prepare $clear.MetadataToken)) 'native actor Prepare clears its own cooldown object (reconciliation blocker, not gameplay PASS)'
$choose = Method $combat 'ChooseNextUnit'
$startRound = Method $combat 'StartRound'
$setRound = Method $combat 'set_RoundNumber'
Assert-Phase3f ((Contains-Token $choose $startRound.MetadataToken) -and (Contains-Token $startRound $setRound.MetadataToken)) 'epoch audit binds round increment to native roster traversal'
$remaining = Method $turn 'GetRemainingTime'
$remainingMovement = Method $turn 'GetRemainingMovementTime'
$tickMovement = Method $turn 'TickMovement'
Assert-Phase3f ($tickMovement.MetadataToken -eq 0x06000C37 -and
    (Contains-Token $tickMovement $remainingMovement.MetadataToken) -and
    (Contains-Token $remainingMovement $remaining.MetadataToken)) 'movement audit binds delegated tick to native current-actor budget predicates'
foreach ($property in @('TimeMoved','TimeMovedByFiveFootStep','MetersMovedByFiveFootStep')) {
    $setter = Method $turn ('set_' + $property)
    Assert-Phase3f (Contains-Token $tickMovement $setter.MetadataToken) "native movement mutates $property beyond Move cooldown"
}
$guardPath = Join-Path $labRoot 'codex-policy\Manage-KingmakerMountedCombatDeployment.ps1'
$tokens=$null; $errors=$null
$ast = [Management.Automation.Language.Parser]::ParseFile($guardPath,[ref]$tokens,[ref]$errors)
Assert-Phase3f ($errors.Count -eq 0) 'deployment helper parses'
$guard = Get-Content -Raw -LiteralPath $guardPath
Assert-Phase3f ($guard.Contains("'codex/mounted-combat-phase3f-playable-core'") -and
    $guard.Contains("if (`$Operation -eq 'RuntimeTest')") -and
    $guard.Contains('& $launcher @runtimeArguments -WhatIf -Confirm:$false') -and
    $guard.Contains('RuntimeTest independent installed/foreign Mods restoration audit failed. Do not launch again.') -and
    -not $guard.Contains("'codex/*'")) 'Phase 3F guard retains exact branch, WhatIf and independent restoration audit'
Write-Host "TOTAL PASS=$passes FAIL=0"
