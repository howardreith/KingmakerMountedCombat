# F4 dungeon entry reload: the isolation bootstrap's actual leaf declaration through the compiled DLL, for the real P07 cold
# request shape. The reload declares exactly its one Auto input, read-only and hash-pinned, and no writable output; the
# cross-area AfterEntry autosave consumer keeps its one writable resave leaf. Metadata only; no game method runs.
param([string]$DllPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
$repo=Split-Path -Parent $PSScriptRoot; $lab=Get-KmcLabRoot
$json=[IO.File]::ReadAllText((Join-Path $lab 'runtime-evidence\final211-p07-area-cross-entry\runtime-request.json'))
$managed='C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed'
Add-Type -Path (Join-Path $managed 'Newtonsoft.Json.dll') -ErrorAction SilentlyContinue
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($sender,$e) $f=Join-Path $managed ((New-Object Reflection.AssemblyName($e.Name)).Name+'.dll'); if(Test-Path -LiteralPath $f){ [Reflection.Assembly]::LoadFrom($f) } else { $null } })
$asm=[Reflection.Assembly]::LoadFrom($(if($DllPath){$DllPath}else{Join-Path $repo 'bin\Release\KingmakerMountedCombat.dll'}))
$requestType=$asm.GetType('KingmakerMountedCombat.Diagnostics.RuntimeRequest',$true)
$bootstrap=$asm.GetType('KingmakerMountedCombat.Diagnostics.PersistenceIsolationBootstrap',$true)
$static=[Reflection.BindingFlags]'NonPublic,Public,Static'
$floor='9d1278a2f599b2a4daab53abdfe88d2e';$hub='c49315fe499f0e5468af6f19242499a2'
function Request([string]$Case){
 $r=$json | ConvertFrom-Json
 $r.scenario='persistence-p07-load';$r.persistenceCase=$Case
 $r.productVersion=[string]($asm.GetCustomAttributes([Reflection.AssemblyInformationalVersionAttribute],$false) | Select-Object -First 1).InformationalVersion
 $f=$r.fixture.working
 if($Case-ceq'area-dungeon-entry-auto'){
  $r.persistenceAreaTarget=[pscustomobject][ordered]@{enterPoint='d27d1d11f23ba9b46b6cdf00584ad08c';area=$hub;autoSaveMode='AfterEntry'};$area=$floor
 } else { $area=[string]$r.persistenceAreaTarget.area }
 $r | Add-Member -NotePropertyName persistenceLoad -NotePropertyValue ([pscustomobject][ordered]@{internalName='Autosave1';fileName='Auto_1.zks';sha256=('c'*64)
  length=1024;lastWriteTimeUtcTicks=[long]$f.lastWriteTimeUtcTicks;gameId=[string]$f.gameId;gameName=[string]$f.gameName;area=$area}) -Force
 [Newtonsoft.Json.JsonConvert]::DeserializeObject(($r | ConvertTo-Json -Depth 30),$requestType)
}
function Entries($Native){ ,($bootstrap.GetMethod('DeclareSaveEntries',$static).Invoke($null,@($Native,$requestType.GetProperty('PersistenceLoad').GetValue($Native)))) }
$reload=Request 'area-dungeon-entry-auto'
$errs=@($requestType.GetMethod('Validate').Invoke($reload,@())); if($errs.Count){ $errs | ForEach-Object { Write-Output ('      validate: '+$_) } }
Check ($errs.Count -eq 0) 'the compiled request admits the real dungeon entry reload shape'
$entries=@(foreach($x in (Entries $reload)){ $x })
Check ($entries.Count -eq 1 -and $entries[0].FileName -ceq 'Auto_1.zks' -and $entries[0].SaveType -ceq 'Auto' -and -not $entries[0].Writable -and
 $entries[0].InitialSha256 -ceq ('c'*64) -and $entries[0].Area -ceq $floor) 'the reload declares exactly its Auto input, read-only, hash-pinned, on the floor, and no writable output'
$cross=Request 'area-cross-entry-auto'
$crossEntries=@(foreach($x in (Entries $cross)){ $x })
Check (@($crossEntries | Where-Object { $_.Writable -and $_.FileName -ceq 'Manual_1_KMC_P01.zks' }).Count -eq 1 -and @($crossEntries | Where-Object { -not $_.Writable -and $_.FileName -ceq 'Auto_1.zks' }).Count -eq 1) 'the cross-area AfterEntry autosave consumer keeps its one writable resave leaf'
Write-Output ('F4 dungeon reload declaration: PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
