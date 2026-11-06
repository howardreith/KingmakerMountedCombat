# F2 consumer: the isolation bootstrap's actual declaration and enumeration decision, exercised through the compiled DLL
# with the real consumer request shape and ordinary copies of run 20261105-finalize-f2-r05's two Manuals. No game.
param([string]$DllPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'runtime\RuntimeHarness.Common.ps1')
. (Join-Path $PSScriptRoot 'runtime\PersistenceSaveFixtures.ps1')
$script:pass=0;$script:fail=0
function Check([bool]$Condition,[string]$Name){ if($Condition){$script:pass++;Write-Output ('    PASS '+$Name)}else{$script:fail++;Write-Output ('    FAIL '+$Name)} }
$repo=Split-Path -Parent $PSScriptRoot; $lab=Get-KmcLabRoot
$bridge=Get-KmcFinalize2ProducerBridge
$base=Get-Content -LiteralPath (Join-Path $lab 'runtime-evidence\20261104-sprint9-e07-upgrade-cold\runtime-request.json') -Raw | ConvertFrom-Json
$kept=Get-KmcPersistenceSource -SourceRunId $bridge.producerRunId -ExpectedSha256 $bridge.kept.sha256 -Fixture $base.fixture -NativeCase quick -ArtifactRole upgrade-resave-manual
$removed=Get-KmcPersistenceSource -SourceRunId $bridge.producerRunId -ExpectedSha256 $bridge.removed.sha256 -Fixture $base.fixture -NativeCase quick -ArtifactRole upgrade-removed-manual
$json=[IO.File]::ReadAllText((Join-Path $lab 'runtime-evidence\20261104-sprint9-e07-upgrade-cold\runtime-request.json'))
Add-Type -Path 'C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed\Newtonsoft.Json.dll' -ErrorAction SilentlyContinue
# The installed game's assemblies resolve the diagnostic types' native references (metadata only; no game method runs).
$managed='C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed'
[AppDomain]::CurrentDomain.add_AssemblyResolve({ param($sender,$e) $f=Join-Path $managed ((New-Object Reflection.AssemblyName($e.Name)).Name+'.dll'); if(Test-Path -LiteralPath $f){ [Reflection.Assembly]::LoadFrom($f) } else { $null } })
$asm=[Reflection.Assembly]::LoadFrom($(if($DllPath){$DllPath}else{Join-Path $repo 'bin\Release\KingmakerMountedCombat.dll'}))
$requestType=$asm.GetType('KingmakerMountedCombat.Diagnostics.RuntimeRequest',$true)
$descriptorType=$asm.GetType('KingmakerMountedCombat.Diagnostics.RuntimeSaveDescriptor',$true)
$bootstrap=$asm.GetType('KingmakerMountedCombat.Diagnostics.PersistenceIsolationBootstrap',$true)
$authorityType=$asm.GetType('KingmakerMountedCombat.Diagnostics.PersistenceSaveAuthorization',$true)
$targetType=$asm.GetType('KingmakerMountedCombat.Diagnostics.RuntimeSaveTarget',$true)
$operationType=$asm.GetType('KingmakerMountedCombat.Diagnostics.RuntimeSaveOperation',$true)
$static=[Reflection.BindingFlags]'NonPublic,Public,Static'; $instance=[Reflection.BindingFlags]'NonPublic,Public,Instance'
function Request([bool]$Finalize2){
 $r=$json | ConvertFrom-Json
 $r.persistenceLoad=[pscustomobject]$kept.descriptor; $r.branch='codex/kmc-finalize-f2-canonical-controls'
 $r.productVersion=[string]($asm.GetCustomAttributes([Reflection.AssemblyInformationalVersionAttribute],$false) | Select-Object -First 1).InformationalVersion
 if($Finalize2){ $r | Add-Member -NotePropertyName finalize2Controls -NotePropertyValue $true; $r | Add-Member -NotePropertyName persistenceAlternate -NotePropertyValue ([pscustomobject]$removed.descriptor) }
 [Newtonsoft.Json.JsonConvert]::DeserializeObject(($r | ConvertTo-Json -Depth 30),$requestType)
}
function Entries($Native){ ,($bootstrap.GetMethod('DeclareSaveEntries',$static).Invoke($null,@($Native,$requestType.GetProperty('PersistenceLoad').GetValue($Native)))) }
function Count($Native){ [int]$bootstrap.GetMethod('ExpectedEnumerationCount',$static).Invoke($null,@($Native)) }
$temp=Join-Path ([IO.Path]::GetTempPath()) ('kmc-f2-enumeration-'+[Guid]::NewGuid().ToString('N'))
try {
 $saves=Join-Path $temp 'Saved Games'; [void](New-Item -ItemType Directory -Force $saves)
 # Ordinary copies, as the runner stages them; never links to r05's evidence.
 foreach($s in @($kept,$removed)){ $to=Join-Path $saves $s.descriptor.fileName; Copy-Item -LiteralPath $s.path -Destination $to; [IO.File]::SetLastWriteTimeUtc($to,[IO.File]::GetLastWriteTimeUtc($s.path)) }
 Check ((Get-KmcSha256 (Join-Path $saves $bridge.kept.fileName)) -ceq $bridge.kept.sha256 -and (Get-KmcSha256 (Join-Path $saves $bridge.removed.fileName)) -ceq $bridge.removed.sha256) 'the staged copies are exactly r05''s two declared Manuals'
 $consumer=Request $true
 $errs=@($requestType.GetMethod('Validate').Invoke($consumer,@())); if($errs.Count){ $errs | ForEach-Object { Write-Output ('      validate: '+$_) } }
 Check ($errs.Count -eq 0) 'the compiled request admits the real consumer shape'
 $entries=Entries $consumer
 Check (@($entries).Count -eq 2 -and @($entries | Where-Object { $_.Writable }).Count -eq 0 -and
  @($entries | Where-Object { $_.FileName -ceq $bridge.kept.fileName -and $_.InitialSha256 -ceq $bridge.kept.sha256 }).Count -eq 1 -and
  @($entries | Where-Object { $_.FileName -ceq $bridge.removed.fileName -and $_.InitialSha256 -ceq $bridge.removed.sha256 }).Count -eq 1) 'production declares exactly the two inputs, both read-only and pinned by hash, and no writable output'
 Check ((Count $consumer) -eq 2) 'the consumer route expects two saves in its isolated root'
 $single=Request $false
 Check ((Count $single) -eq 1 -and @(Entries $single).Count -eq 1) 'the ordinary single-input upgrade consumer still expects exactly one save'
 $authority=$authorityType.GetConstructors($instance) | Where-Object { $_.GetParameters().Count -eq 5 } | Select-Object -First 1
 $fixture=$requestType.GetProperty('PersistenceLoad').GetValue($consumer)
 function NewAuthority($List){ $authority.Invoke(@([string]$temp,[string]$fixture.GameId,[string]$fixture.GameName,[string]$base.fixture.baseline.sha256,$List)) }
 $auth=NewAuthority $entries
 $load=[Enum]::Parse($operationType,'Load')
 $validateMethod=$authorityType.GetMethod('Validate',$instance)
 function Target([string]$Leaf,[string]$Name){ $x=[Activator]::CreateInstance($targetType,$true); $x.InternalName=$Name; $x.FileName=$Leaf; $x.FullPath=(Join-Path $saves $Leaf); $x.SaveType='Manual'; $x.GameId=$fixture.GameId; $x.GameName=$fixture.GameName; $x.Area=$fixture.Area; $x }
 $funcType=[Type]'System.Func`2'; $funcType=$funcType.MakeGenericType(@($targetType,[string]))
 # A compiled delegate: a PowerShell script block would turn the authority's null (admitted) into an empty string.
 $E=[Linq.Expressions.Expression]; $param=$E::Parameter($targetType,'target')
 $call=$E::Call($E::Constant($auth),$validateMethod,[Linq.Expressions.Expression[]]@($E::Constant($load),$param,$E::Constant([string]$saves)))
 $validate=$E::Lambda($funcType,$call,[Linq.Expressions.ParameterExpression[]]@($param)).Compile()
 $listType=[Collections.Generic.List[object]].GetGenericTypeDefinition().MakeGenericType([Type[]]@($targetType))
 function Admits($Native,[object[]]$Targets){ $l=[Activator]::CreateInstance($listType); foreach($x in $Targets){ $l.Add($x) }; [bool]$bootstrap.GetMethod('AdmitsEnumeration',$static).Invoke($null,@($Native,$l,$validate)) }
 $tk=Target $bridge.kept.fileName $bridge.kept.internalName; $tr=Target $bridge.removed.fileName $bridge.removed.internalName
 Check (Admits $consumer @($tk,$tr)) 'the actual decision admits exactly the two authorized Manuals'
 Check (-not (Admits $consumer @($tk))) 'one missing input refuses'
 Check (-not (Admits $consumer @($tk,$tr,(Target 'Manual_3_KMC_EXTRA.zks' 'KMC_EXTRA')))) 'an extra archive refuses'
 Check (-not (Admits $consumer @($tk,$tk))) 'a duplicated input refuses'
 Copy-Item -LiteralPath (Join-Path $saves $bridge.removed.fileName) -Destination (Join-Path $saves 'Manual_3_KMC_ALIAS.zks')
 Check (-not (Admits $consumer @($tk,(Target 'Manual_3_KMC_ALIAS.zks' $bridge.removed.internalName)))) 'an alias of the removed bytes under another leaf refuses'
 Remove-Item -LiteralPath (Join-Path $saves 'Manual_3_KMC_ALIAS.zks')
 $foreign=Target $bridge.removed.fileName $bridge.removed.internalName; $foreign.GameId='00000000-0000-0000-0000-000000000001'
 Check (-not (Admits $consumer @($tk,$foreign))) 'a foreign-campaign descriptor refuses'
 [IO.File]::AppendAllText((Join-Path $saves $bridge.removed.fileName),'x')
 Check (-not (Admits $consumer @($tk,$tr))) 'a wrong-hash input refuses'
 Check (-not (Admits $single @($tk,$tr))) 'the single-input route refuses a second archive'
} finally { if(Test-Path -LiteralPath $temp){ Remove-Item -LiteralPath $temp -Recurse -Force } }
$quota=[IO.File]::ReadAllText((Join-Path $repo 'scripts\runtime\Test-RuntimeGameResult.ps1'))
Check ($quota.Contains("elseif(`$f2Upgrade-and`$request.persistenceCase-ceq'manual'){2}") -and $quota.Contains("elseif(`$f2Upgrade-and`$request.persistenceCase-ceq'quick'){2}")) 'the game-result quotas agree: two native loads for the consumer (zero writes: the consumer is not a writing case)'
Write-Output ('F2 consumer enumeration: PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail -ne 0){ exit 1 }
