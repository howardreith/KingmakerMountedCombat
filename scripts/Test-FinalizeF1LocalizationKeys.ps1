[CmdletBinding()] param()
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$files=@('NativeMountedControlService.cs','HorseCompanionBlueprintService.cs','HorseCompanionBlueprintService.MountedCombatFeat.cs')
$text=($files|ForEach-Object {Get-Content -Raw (Join-Path $repo ('src\KingmakerMountedCombat\Integration\'+$_))}) -join "`n"
$registered=@{};foreach($m in [regex]::Matches($text,'\{\s*"(KMC\.[^"]+)"\s*,')){$registered[$m.Groups[1].Value]=$true}
$consumers=@([regex]::Matches($text,'NewLocalizedString\("(KMC\.[^"]+)"\)')|ForEach-Object {$_.Groups[1].Value}|Sort-Object -Unique)
if($consumers.Count-lt1){throw 'No real key consumers selected'}
foreach($key in $consumers){if(!$registered.ContainsKey($key)){throw "Unregistered player-facing localization key: $key"}}
foreach($key in @('KMC.Native.MountedCharge.Name','KMC.Native.MountedCharge.Description')){if(!$registered.ContainsKey($key)-or$key-notin$consumers){throw "Charge key regression: $key"}}
Write-Host ('PASS '+$consumers.Count+' literal native and feat localization consumers have registrations, including both Charge keys')
