[CmdletBinding()]
# The passing default exercises managed partial-install cleanup. InstalledHooks
# is a diagnostic of the detached CLR boundary; Unity ECall targets require the
# guarded native process for a real three-hook installation verdict.
param(
 [Parameter(Mandatory=$true)][string]$ProductDll,
 [Parameter(Mandatory=$true)][ValidatePattern('^[0-9a-f]{64}$')][string]$ExpectedDllSha256,
 [Parameter(Mandatory=$true)][string]$OutputDirectory,
 [ValidateSet('InstalledHooks','ManagedCleanup')][string]$Mode='ManagedCleanup',
 [switch]$Worker
)
$ErrorActionPreference='Stop';Set-StrictMode -Version Latest
$lab=[IO.Path]::GetFullPath('C:/Dev/KingmakerMountedCombatLab')
$analysis=Join-Path $lab 'analysis-cache'
$ProductDll=[IO.Path]::GetFullPath($ProductDll);$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if(-not$ProductDll.StartsWith($lab+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)-or
 -not$OutputDirectory.StartsWith($analysis+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
 throw 'Probe test DLL must be lab-owned and output must be private analysis-cache.'
}
if((Get-FileHash -LiteralPath $ProductDll).Hash.ToLowerInvariant()-cne$ExpectedDllSha256){throw 'Exact product DLL hash differs.'}
if(-not$Worker){
 if(Test-Path -LiteralPath $OutputDirectory){throw 'A fresh installed-probe test output directory is required.'}
 [void][IO.Directory]::CreateDirectory($OutputDirectory)
 & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -ProductDll $ProductDll -ExpectedDllSha256 $ExpectedDllSha256 -OutputDirectory $OutputDirectory -Mode $Mode -Worker *> (Join-Path $OutputDirectory 'worker.log')
 if($LASTEXITCODE-ne0){throw 'Exact-DLL installed probe regression failed; preserve worker log and receipt.'}
 Get-Content -LiteralPath (Join-Path $OutputDirectory 'worker.log')
 exit 0
}
# This disposable process loads installed assemblies read-only. It never creates
# Game, invokes native save methods, starts Unity or opens a profile/save root.
$managed='C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Kingmaker_Data/Managed'
$umm=Join-Path $managed 'UnityModManager'
$json=Join-Path $managed 'Newtonsoft.Json.dll';$harmonyPath=Join-Path $umm '0Harmony12.dll';$native=Join-Path $managed 'Assembly-CSharp.dll'
$inputs=@($ProductDll,$PSCommandPath,$json,$harmonyPath,$native)|ForEach-Object{[pscustomobject]@{path=$_;sha256=(Get-FileHash -LiteralPath $_).Hash.ToLowerInvariant()}}
$script:probe=$null;$script:faultHarmony=$null;$script:faultType=$null;$script:pass=0;$script:fail=0;$rows=@();$watch=[Diagnostics.Stopwatch]::StartNew()
$flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$probeId='KingmakerMountedCombat.Diagnostics.SmallFixtureSave';$faultId='KingmakerMountedCombat.Tests.SmallFixtureSave.InstallFault'
function Check([string]$Name,[scriptblock]$Body){try{&$Body;$script:pass++;$script:rows+=[pscustomobject]@{name=$Name;status='PASS'};Write-Host ('PASS '+$Name)}catch{$script:fail++;$script:rows+=[pscustomobject]@{name=$Name;status='FAIL';error=$_.Exception.ToString()};Write-Host ('FAIL '+$Name+': '+$_);throw}}
function Need([bool]$Value,[string]$Reason){if(-not$Value){throw $Reason}}
function Read-Field($Object,[string]$Name){$f=$Object.GetType().GetField($Name,$flags);if($null-eq$f){throw ('Missing exact field '+$Name)};return ,$f.GetValue($Object)}
function Set-Field($Object,[string]$Name,$Value){$f=$Object.GetType().GetField($Name,$flags);if($null-eq$f){throw ('Missing exact field '+$Name)};$f.SetValue($Object,$Value)}
function Invoke-Probe($Object,[string]$Name){$m=$Object.GetType().GetMethod($Name,$flags);Need ($null-ne$m) ('Probe method missing '+$Name);[void]$m.Invoke($Object,@())}
function Owner-Count($Method,[string]$Owner){
 # Installed Harmony12 is a compatibility adapter whose GetPatchInfo converter
 # throws for an entirely unpatched method. Its actual patched-method inventory
 # distinguishes that state before querying owners; no exception is suppressed.
 $methods=@($script:getPatched.Invoke($script:inspectionHarmony,@()))
 if(@($methods|Where-Object{[object]::Equals($_,$Method)}).Count-eq0){return 0}
 $info=$script:getInfo.Invoke($script:inspectionHarmony,[object[]]@($Method.PSObject.BaseObject));if($null-eq$info){return 0};@($info.Owners|Where-Object{$_-ceq$Owner}).Count
}
function Event-Count($Service,[string]$Name){$d=Read-Field $Service $Name;if($null-eq$d){return 0};$d.GetInvocationList().Length}
function New-Probe {
 $service=[Runtime.Serialization.FormatterServices]::GetUninitializedObject($script:serviceType)
 $p=[Runtime.Serialization.FormatterServices]::GetUninitializedObject($script:probeType)
 Set-Field $p 'persistence' $service
 Set-Field $p 'events' ([Activator]::CreateInstance($script:arrayType))
 Set-Field $p 'hooks' ([Activator]::CreateInstance($script:arrayType))
 Set-Field $p 'errors' ([Collections.Generic.List[string]]::new())
 Set-Field $p 'fields' ([Collections.Generic.Dictionary[string,Reflection.FieldInfo]]::new())
 [pscustomobject]@{probe=$p;service=$service}
}
function Managed-Cleanup {
 Add-Type -TypeDefinition 'public static class KmcSmallFixtureManagedFault { public static bool Armed; public static int Calls; public static void Before() { if (Armed && ++Calls == 1) throw new System.InvalidOperationException("F09 test-only first Patch fault"); } [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)] public static int Target(int value) { return value + 1; } }'
 $script:faultType=[KmcSmallFixtureManagedFault]
 $script:faultHarmony=$create.Invoke($null,[object[]]@($faultId))
 $faultHm=[Activator]::CreateInstance($hm,[object[]]@($script:faultType.GetMethod('Before')))
 [void]$patch.Invoke($script:faultHarmony,[object[]]@($patch.PSObject.BaseObject,$faultHm.PSObject.BaseObject,$null,$null))
 $first=New-Probe;$script:probe=$first.probe
 Check 'managed-only actual Install retains attempted custody when first patch boundary throws' {
  $script:faultType.GetField('Armed').SetValue($null,$true);$caught=$false
  try{Invoke-Probe $script:probe 'Install'}catch{$caught=$true}finally{$script:faultType.GetField('Armed').SetValue($null,$false)}
  Need ($caught-and$script:faultType.GetField('Calls').GetValue($null)-eq1) 'The external first-Patch fault did not execute.'
  Need ((Read-Field $script:probe 'attempted')-and-not(Read-Field $script:probe 'installed')-and(Read-Field $script:probe 'hooks').Count-eq0) 'First-boundary failure state differs.'
  Need ([object]::ReferenceEquals($script:probeType.GetField('active',$flags).GetValue($null),$script:probe)) 'Attempted probe lost cleanup custody.'
  foreach($event in @('SaveSnapshotStarting','SaveSnapshotStaged')){Need ((Event-Count $first.service $event)-eq0) 'First-boundary failure acquired later events.'}
 }
 Check 'managed-only actual Dispose releases first-boundary attempt and permits idempotent retry' {
  Invoke-Probe $script:probe 'Dispose';Invoke-Probe $script:probe 'Dispose'
  Need ((Read-Field $script:probe 'disposed')-and$null-eq$script:probeType.GetField('active',$flags).GetValue($null)) 'Attempted probe custody leaked.'
 }
 $script:probe=$null
 # Construct an explicitly labelled partial managed acquisition: one real CLR
 # Harmony hook plus both actual service-event delegates. This validates the
 # candidate's actual Dispose method, without representing native hook Install.
 $held=New-Probe;$script:probe=$held.probe;$owned=$create.Invoke($null,[object[]]@($probeId));$target=$script:faultType.GetMethod('Target')
 Check 'managed-only partial acquisition owns a real CLR hook and two actual event delegates' {
  [void]$patch.Invoke($owned,[object[]]@($target.PSObject.BaseObject,$faultHm.PSObject.BaseObject,$null,$null))
  Set-Field $script:probe 'harmony' $owned;Set-Field $script:probe 'attempted' $true
  $script:probeType.GetField('active',$flags).SetValue($null,$script:probe)
  foreach($pair in @(@('SaveSnapshotStarting','Starting','startingOwned'),@('SaveSnapshotStaged','Staged','stagedOwned'))){
   $method=$script:probeType.GetMethod($pair[1],$flags);$handler=[Delegate]::CreateDelegate([Action],$script:probe,$method)
   $event=$script:serviceType.GetEvent($pair[0],$flags);[void]$event.GetAddMethod($true).Invoke($held.service,[object[]]@($handler));Set-Field $script:probe $pair[2] $true
   Need ((Event-Count $held.service $pair[0])-eq1) 'Actual partial service event was not acquired.'
  }
  Need ((Owner-Count $target $probeId)-eq1) 'Real managed partial hook was not acquired.'
 }
 Check 'managed-only actual Dispose retains all custody during its callback' {
  Set-Field $script:probe 'inside' 1;$caught=$false
  try{Invoke-Probe $script:probe 'Dispose'}catch{$caught=$true}finally{Set-Field $script:probe 'inside' 0}
  Need ($caught-and-not(Read-Field $script:probe 'disposed')-and(Owner-Count $target $probeId)-eq1) 'Callback custody was released.'
  foreach($event in @('SaveSnapshotStarting','SaveSnapshotStaged')){Need ((Event-Count $held.service $event)-eq1) 'Callback released an owned event.'}
 }
 Check 'managed-only actual Dispose removes partial CLR hook, both events and static owner' {
  Invoke-Probe $script:probe 'Dispose';Invoke-Probe $script:probe 'Dispose'
  Need ((Owner-Count $target $probeId)-eq0-and$null-eq$script:probeType.GetField('active',$flags).GetValue($null)) 'Partial managed hook/static owner leaked.'
  foreach($event in @('SaveSnapshotStarting','SaveSnapshotStaged')){Need ((Event-Count $held.service $event)-eq0) 'Partial actual event leaked.'}
 }
 $script:probe=$null
 Check 'managed-only external fault hook is removed' {
  [void]$unpatch.Invoke($script:faultHarmony,[object[]]@($faultId));Need ((Owner-Count $patch $faultId)-eq0) 'External fault hook leaked.';$script:faultHarmony=$null
 }
}
try{
 $script:resolveRoots=@($managed,$umm,[IO.Path]::GetDirectoryName($ProductDll))
 $script:resolver=[ResolveEventHandler]{param($sender,$eventArgs)
  $name=[Reflection.AssemblyName]::new($eventArgs.Name).Name+'.dll'
  foreach($root in $script:resolveRoots){$path=Join-Path $root $name;if(Test-Path -LiteralPath $path){return [Reflection.Assembly]::LoadFrom($path)}}
  return $null
 }
 [AppDomain]::CurrentDomain.add_AssemblyResolve($script:resolver)
 $jsonAssembly=[Reflection.Assembly]::LoadFrom($json);$ha=[Reflection.Assembly]::LoadFrom($harmonyPath)
 $na=[Reflection.Assembly]::LoadFrom($native);$pa=[Reflection.Assembly]::LoadFrom($ProductDll)
 $script:probeType=$pa.GetType('KingmakerMountedCombat.Diagnostics.Chunk7SmallFixtureSaveProbe',$true)
 $script:serviceType=$pa.GetType('KingmakerMountedCombat.Integration.MountedPersistenceService',$true)
 $script:arrayType=$jsonAssembly.GetType('Newtonsoft.Json.Linq.JArray',$true)
 $hi=$ha.GetType('Harmony12.HarmonyInstance',$true);$hm=$ha.GetType('Harmony12.HarmonyMethod',$true)
 $script:getInfo=$hi.GetMethod('GetPatchInfo',$flags)
 $script:getPatched=$hi.GetMethod('GetPatchedMethods',$flags)
 $create=$hi.GetMethod('Create',$flags);$patch=$hi.GetMethods($flags)|Where-Object{$_.Name-ceq'Patch'-and$_.GetParameters().Length-eq4}
 $script:inspectionHarmony=$create.Invoke($null,[object[]]@('KingmakerMountedCombat.Tests.SmallFixtureSave.ReadOnlyInspection'))
 Need (@($patch).Count-eq1) 'Installed Harmony Patch signature is ambiguous.'
 $unpatch=$hi.GetMethod('UnpatchAll',$flags)
 $saveManager=$na.GetType('Kingmaker.EntitySystem.Persistence.SaveManager',$true)
 $saveInfo=$na.GetType('Kingmaker.EntitySystem.Persistence.SaveInfo',$true)
 $scope=$script:serviceType.GetNestedType('SaveScope',[Reflection.BindingFlags]::NonPublic)
 $script:targets=@($saveManager.GetMethod('SaveRoutine',$flags,$null,[type[]]@($saveInfo,[bool]),$null),
  $saveManager.GetMethod('PrepareSave',$flags,$null,[type[]]@($saveInfo),$null),
  $script:serviceType.GetMethod('RecordCompletedSave',$flags,$null,[type[]]@($scope),$null))
 Check 'exact native MVID and all three unowned production hook targets' {
  Need ($na.ManifestModule.ModuleVersionId.ToString('D')-ceq'07fa1e4d-8618-41b3-9b8d-faa17d3b26f7') 'Installed native MVID differs.'
  Need ($script:targets.Count-eq3-and@($script:targets|Where-Object{$null-eq$_}).Count-eq0) 'Exact save hook target is missing.'
  Need ($script:targets[0].MetadataToken-eq0x06008029-and$script:targets[1].MetadataToken-eq0x06008025) 'Native hook token differs.'
  foreach($target in $script:targets){Need ((Owner-Count $target $probeId)-eq0) 'F09 probe owner pre-exists in the isolated process.'}
  Need ($null-eq$script:probeType.GetField('active',$flags).GetValue($null)) 'Active probe pre-exists.'
 }
 if($Mode-ceq'ManagedCleanup'){Managed-Cleanup}else{
 $full=New-Probe;$script:probe=$full.probe
 Check 'actual complete Install patches three real methods and owns both actual events' {
  Invoke-Probe $script:probe 'Install'
  Need ((Read-Field $script:probe 'attempted')-and(Read-Field $script:probe 'installed')) 'Actual full installation flags differ.'
  Need ((Read-Field $script:probe 'hooks').Count-eq3) 'Actual installed hook count differs.'
  foreach($target in $script:targets){Need ((Owner-Count $target $probeId)-eq1) 'Actual hook owner absent or duplicated.'}
  foreach($event in @('SaveSnapshotStarting','SaveSnapshotStaged')){Need ((Event-Count $full.service $event)-eq1) 'Actual service event subscription differs.'}
 }
 Check 'actual complete Dispose removes all hooks, both events and static custody' {
  Invoke-Probe $script:probe 'Dispose'
  foreach($target in $script:targets){Need ((Owner-Count $target $probeId)-eq0) 'Actual hook owner leaked.'}
  foreach($event in @('SaveSnapshotStarting','SaveSnapshotStaged')){Need ((Event-Count $full.service $event)-eq0) 'Actual service event leaked.'}
  Need ((Read-Field $script:probe 'disposed')-and$null-eq$script:probeType.GetField('active',$flags).GetValue($null)) 'Actual static custody leaked.'
  Invoke-Probe $script:probe 'Dispose'
 }
 $script:probe=$null
 # An external Harmony prefix faults the second real Patch call. The first real
 # F09 hook is already installed: this is actual partial acquisition and actual
 # disposal, rather than a signature or source-text surrogate.
 Add-Type -TypeDefinition 'public static class KmcSmallFixtureInstallFault { public static bool Armed; public static int Calls; public static void Before() { if (Armed && ++Calls == 2) throw new System.InvalidOperationException("F09 test-only second Harmony patch fault"); } }'
 $script:faultType=[KmcSmallFixtureInstallFault]
 $script:faultHarmony=$create.Invoke($null,[object[]]@($faultId))
 $faultMethod=$script:faultType.GetMethod('Before')
 $faultHm=[Activator]::CreateInstance($hm,[object[]]@($faultMethod))
 [void]$patch.Invoke($script:faultHarmony,[object[]]@($patch.PSObject.BaseObject,$faultHm.PSObject.BaseObject,$null,$null))
 $partial=New-Probe;$script:probe=$partial.probe
 Check 'actual partial Install fails after one real hook before event acquisition' {
  $script:faultType.GetField('Calls').SetValue($null,0);$script:faultType.GetField('Armed').SetValue($null,$true)
  $caught=$false
  try{Invoke-Probe $script:probe 'Install'}catch{$caught=$true}finally{$script:faultType.GetField('Armed').SetValue($null,$false)}
  Need $caught 'Injected second real Harmony patch was accepted.'
  Need ($script:faultType.GetField('Calls').GetValue($null)-eq2) 'Fault did not occur at the exact second patch acquisition.'
  Need ((Read-Field $script:probe 'attempted')-and-not(Read-Field $script:probe 'installed')-and(Read-Field $script:probe 'hooks').Count-eq1) 'Partial acquisition state differs.'
  Need ((Owner-Count $script:targets[0] $probeId)-eq1) 'First real partial hook was not acquired.'
  Need ((Owner-Count $script:targets[1] $probeId)-eq0-and(Owner-Count $script:targets[2] $probeId)-eq0) 'Faulted or later hook was acquired.'
  foreach($event in @('SaveSnapshotStarting','SaveSnapshotStaged')){Need ((Event-Count $partial.service $event)-eq0) 'Partial install acquired a later service event.'}
 }
 Check 'actual partial Dispose removes acquired hook and static custody with idempotent retry' {
  Invoke-Probe $script:probe 'Dispose';Invoke-Probe $script:probe 'Dispose'
  foreach($target in $script:targets){Need ((Owner-Count $target $probeId)-eq0) 'Partial hook leaked.'}
  foreach($event in @('SaveSnapshotStarting','SaveSnapshotStaged')){Need ((Event-Count $partial.service $event)-eq0) 'Partial event leaked.'}
  Need ($null-eq$script:probeType.GetField('active',$flags).GetValue($null)) 'Partial static custody leaked.'
 }
 $script:probe=$null
 Check 'test fault hook is removed from actual Harmony Patch' {
  [void]$unpatch.Invoke($script:faultHarmony,[object[]]@($faultId))
  Need ((Owner-Count $patch $faultId)-eq0) 'Test-only fault hook leaked.'
  $script:faultHarmony=$null
 }
 }
}catch{if($script:fail-eq0){$script:fail++;$rows+=[pscustomobject]@{name='test infrastructure';status='FAIL';error=$_.Exception.ToString()}};Write-Host $_}finally{
 if($null-ne$script:faultType){$script:faultType.GetField('Armed').SetValue($null,$false)}
 if($null-ne$script:probe){try{Invoke-Probe $script:probe 'Dispose'}catch{$script:fail++;$rows+=[pscustomobject]@{name='finally actual probe cleanup';status='FAIL';error=$_.Exception.ToString()}}}
 if($null-ne$script:faultHarmony){try{[void]$unpatch.Invoke($script:faultHarmony,[object[]]@($faultId))}catch{$script:fail++;$rows+=[pscustomobject]@{name='finally fault hook cleanup';status='FAIL';error=$_.Exception.ToString()}}}
 if($null-ne$script:resolver){[AppDomain]::CurrentDomain.remove_AssemblyResolve($script:resolver)}
}
$unchanged=$true;foreach($pin in $inputs){if((Get-FileHash -LiteralPath $pin.path).Hash.ToLowerInvariant()-cne$pin.sha256){$unchanged=$false;$script:fail++}}
$watch.Stop()
$expectedPass=if($Mode-ceq'ManagedCleanup'){7}else{6}
[ordered]@{schemaVersion=1;selectedMode=$Mode;status=$(if($script:fail-eq0-and$script:pass-eq$expectedPass){'PASS'}else{'FAIL'});pass=$script:pass;fail=$script:fail;elapsedSeconds=$watch.Elapsed.TotalSeconds;productDllSha256=$ExpectedDllSha256;inputs=$inputs;inputsUnchanged=$unchanged;cases=$rows;
 nativeGameInvoked=$false;nativeSaveMethodInvoked=$false;profileOrSaveOpened=$false;sharedBuildOutputsTouched=$false;
 boundaries=$(if($Mode-ceq'ManagedCleanup'){'Managed-cleanup mode: actual Install first-Patch fault and actual Dispose with explicitly constructed partial custody, real CLR test hook and actual production event delegates. Native hook install/remove is NOT proven. Probe/service are uninitialized test objects; no game/world/save callbacks are invoked.'}else{'Actual candidate DLL probe Install/Dispose, installed Harmony12 patch creation/removal and actual production service events in a disposable managed process. Game/manager/constructor/world callbacks are not invoked. Service and probe managed instances are uninitialized test objects. Partial failure is an external test-only second-Patch exception.'})}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'receipt.json') -Encoding UTF8
Write-Host ('TOTAL PASS='+$script:pass+' FAIL='+$script:fail)
if($script:fail-ne0-or$script:pass-ne$expectedPass){exit 1}
