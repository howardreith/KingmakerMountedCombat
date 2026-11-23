[CmdletBinding()]
param([string]$Configuration = 'Release')
# Selection enable/disable rollback (owner amendment 1, section 3; supplemental review PN3-F3 remainder).
# Runs the compiled product's own HorseCompanionBlueprintService.SetSelectionEnabled, constructed through its public
# constructor, on genuine Kingmaker BlueprintFeatureSelection/BlueprintFeature objects created without Unity. The service
# holds the production leases it builds at registration: the exact Ranger AllFeatures lease, the three-feat combat-feat
# publication set and one Riding Dog selection lease. A refusal is produced the way the game produces it: another mod
# changes the Ranger companion selection, so the Ranger lease refuses to append or restore. CompositionRoot.SetEnabled
# keeps its services exactly as they were when SetSelectionEnabled returns false, so after every call each KMC surface
# (Mounted Combat, Mounted Archery, Mounted Shield on every combat-feat list; the Riding Dog list) must match the root's
# retained state, and foreign entries keep their places.
# Covers: two consecutive refused disables, two consecutive refused enables, a successful transition after each refusal
# run, a refusal by a service that is no longer Registered, the same for a registration where Mounted Archery did not register, and the stored desired state reported by the
# service's own snapshot field. Not covered: the root itself (its services need the live game), a foreign change to a
# combat-feat list during a refusal (its report names the list through UnityEngine.Object.name, a Unity internal call;
# the component tests cover that path on the same publication set), native selection UI. Offline only.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$layout = (Get-Content -Raw -LiteralPath (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$managed = Join-Path (Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data') 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
if (-not (Test-Path -LiteralPath $productDll -PathType Leaf)) { throw ('Built product missing: ' + $productDll) }
$script:passed = 0
$script:failures = New-Object 'System.Collections.Generic.List[string]'
function Check([bool]$Value, [string]$Label) {
    if ($Value) { $script:passed++; Write-Host ('PASS ' + $Label) } else { $script:failures.Add($Label); Write-Host ('FAIL ' + $Label) }
}

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
$any = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic'
function GameType([string]$Name) { $game.GetType($Name, $true) }
function KmcType([string]$Name) { $kmc.GetType($Name, $true) }

# A recording IModLogger. The interface is internal to the product, so the recorder is emitted into a dynamic assembly
# that declares IgnoresAccessChecksTo for it (the runtime's supported access exemption for emitted code).
$loggerInterface = KmcType 'KingmakerMountedCombat.Logging.IModLogger'
$builder = [AppDomain]::CurrentDomain.DefineDynamicAssembly((New-Object Reflection.AssemblyName 'KmcSelectionRollbackRecorder'), [Reflection.Emit.AssemblyBuilderAccess]::Run)
$module = $builder.DefineDynamicModule('KmcSelectionRollbackRecorder')
$ignores = $module.DefineType('System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute', 'Public,Class', [Attribute])
$ignoresCtor = $ignores.DefineConstructor('Public', 'Standard', [Type[]]@([string]))
$il = $ignoresCtor.GetILGenerator(); $il.Emit([Reflection.Emit.OpCodes]::Ldarg_0)
$il.Emit([Reflection.Emit.OpCodes]::Call, [Attribute].GetConstructor([Reflection.BindingFlags]'Instance,NonPublic', $null, [Type[]]@(), $null)); $il.Emit([Reflection.Emit.OpCodes]::Ret)
$ignoresType = $ignores.CreateType()
$builder.SetCustomAttribute((New-Object Reflection.Emit.CustomAttributeBuilder ($ignoresType.GetConstructor([Type[]]@([string]))), ([object[]]@('KingmakerMountedCombat'))))
$recorderBuilder = $module.DefineType('KmcSelectionRollbackRecorder.Recorder', 'Public,Class,Sealed', [object], [Type[]]@($loggerInterface))
$linesField = $recorderBuilder.DefineField('Lines', [Collections.Generic.List[string]], 'Public,Static')
foreach ($method in $loggerInterface.GetMethods()) {
    $parameters = @($method.GetParameters() | ForEach-Object { $_.ParameterType })
    $impl = $recorderBuilder.DefineMethod($method.Name, 'Public,Virtual,HideBySig,NewSlot,Final', [void], [Type[]]$parameters)
    $g = $impl.GetILGenerator()
    $g.Emit([Reflection.Emit.OpCodes]::Ldsfld, $linesField)
    $g.Emit([Reflection.Emit.OpCodes]::Ldstr, $method.Name + ': ')
    $g.Emit([Reflection.Emit.OpCodes]::Ldarg, $parameters.Count)
    if ($parameters[$parameters.Count - 1] -ne [string]) { $g.Emit([Reflection.Emit.OpCodes]::Callvirt, [object].GetMethod('ToString')) }
    $g.Emit([Reflection.Emit.OpCodes]::Call, [string].GetMethod('Concat', [Type[]]@([string], [string])))
    $g.Emit([Reflection.Emit.OpCodes]::Callvirt, [Collections.Generic.List[string]].GetMethod('Add'))
    $g.Emit([Reflection.Emit.OpCodes]::Ret)
    $recorderBuilder.DefineMethodOverride($impl, $method)
}
$recorderType = $recorderBuilder.CreateType()
$lines = New-Object 'Collections.Generic.List[string]'
$recorderType.GetField('Lines').SetValue($null, $lines)

# Genuine blueprint objects. A Unity object created without the engine has no native pointer, which Unity's own equality
# reads as destroyed; each object gets a distinct nonzero cached pointer, so the product's null checks and the leases'
# Array.IndexOf (installed UnityEngine.Object.Equals) see distinct live objects. Nothing dereferences the pointer: no
# native member (name, ToString, instance id) is reached on these paths.
$featureType = GameType 'Kingmaker.Blueprints.Classes.BlueprintFeature'
$selectionType = GameType 'Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection'
$unityObject = $featureType; while ($unityObject.FullName -ne 'UnityEngine.Object') { $unityObject = $unityObject.BaseType }
$cachedPtr = $unityObject.GetField('m_CachedPtr', [Reflection.BindingFlags]'Instance,NonPublic')
$script:nextId = 1000
function New-Live([Type]$Type) {
    $value = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($Type)
    $script:nextId++; $cachedPtr.SetValue($value, [IntPtr]$script:nextId)
    $value
}
$allFeatures = $selectionType.GetField('AllFeatures')
function Feature-Array([object[]]$Items) { $array = [Array]::CreateInstance($featureType, $Items.Count); for ($i = 0; $i -lt $Items.Count; $i++) { $array.SetValue($Items[$i], $i) }; , $array }
function Selection-Array([object[]]$Items) { $array = [Array]::CreateInstance($selectionType, $Items.Count); for ($i = 0; $i -lt $Items.Count; $i++) { $array.SetValue($Items[$i], $i) }; , $array }
function Same([object[]]$Actual, [object[]]$Expected) {
    if ($null -eq $Actual -or $Actual.Count -ne $Expected.Count) { return $false }
    for ($i = 0; $i -lt $Expected.Count; $i++) { if (-not [object]::ReferenceEquals($Actual[$i], $Expected[$i])) { return $false } }
    $true
}
$featureArrayType = $featureType.MakeArrayType()
$read = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection) , $script:allFeatures.GetValue($selection) }, [Func``2].MakeGenericType($selectionType, $featureArrayType))
$write = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection, $value) $script:allFeatures.SetValue($selection, $value) }, [Action``2].MakeGenericType($selectionType, $featureArrayType))
$serviceType = KmcType 'KingmakerMountedCombat.Integration.HorseCompanionBlueprintService'
$stateType = KmcType 'KingmakerMountedCombat.Integration.HorseCompanionBlueprintState'
$leaseType = (KmcType 'KingmakerMountedCombat.Domain.ExactAppendOnlyArrayLease`1').MakeGenericType($featureType)
$setType = (KmcType 'KingmakerMountedCombat.Domain.KmcCombatFeatPublicationSet`2').MakeGenericType($selectionType, $featureType)
$setCtor = @($setType.GetConstructors() | Where-Object { $_.GetParameters().Count -eq 7 })[0]
function Field([string]$Name) { $info = $serviceType.GetField($Name, $any); if ($null -eq $info) { throw ('Missing service field ' + $Name) }; $info }
$setSelectionEnabled = $serviceType.GetMethod('SetSelectionEnabled', $any)

# One registered service: the leases registration builds, over genuine selections holding foreign entries.
function New-Registered([bool]$WithArchery) {
    $fixture = @{}
    $fixture.foreignA = New-Live $featureType; $fixture.foreignB = New-Live $featureType; $fixture.foreignC = New-Live $featureType
    $fixture.horse = New-Live $featureType; $fixture.dog = New-Live $featureType; $fixture.ridingDog = New-Live $featureType
    $fixture.combat = New-Live $featureType; $fixture.shield = New-Live $featureType
    $fixture.archery = if ($WithArchery) { New-Live $featureType } else { $null }
    $fixture.kmc = @(@($fixture.combat, $fixture.archery, $fixture.shield) | Where-Object { $null -ne $_ })
    $fixture.ranger = New-Live $selectionType
    $fixture.rangerOriginal = Feature-Array @($fixture.foreignA, $fixture.dog)
    $allFeatures.SetValue($fixture.ranger, $fixture.rangerOriginal)
    $fixture.dogSelection = New-Live $selectionType
    $fixture.dogOriginal = Feature-Array @($fixture.dog, $fixture.foreignB)
    $allFeatures.SetValue($fixture.dogSelection, $fixture.dogOriginal)
    $fixture.combatLists = @((New-Live $selectionType), (New-Live $selectionType), (New-Live $selectionType))
    $fixture.combatOriginal = @{}
    foreach ($list in $fixture.combatLists) {
        $original = Feature-Array @($fixture.foreignA, $fixture.foreignC)
        $allFeatures.SetValue($list, $original); $fixture.combatOriginal[[Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($list)] = @($original)
    }
    $lines.Clear()
    $service = $serviceType.GetConstructors()[0].Invoke(@([Activator]::CreateInstance($recorderType)))
    (Field 'state').SetValue($service, [Enum]::Parse($stateType, 'Registered'))
    (Field 'rangerSelection').SetValue($service, $fixture.ranger)
    (Field 'selectionLease').SetValue($service, $leaseType.GetConstructor([Type[]]@($featureArrayType, $featureArrayType)).Invoke(@($fixture.rangerOriginal, (Feature-Array @($fixture.horse)))))
    $dogLease = $leaseType.GetConstructor([Type[]]@($featureArrayType, $featureType)).Invoke(@($fixture.dogOriginal, $fixture.ridingDog))
    $pairType = [Collections.Generic.KeyValuePair``2].MakeGenericType($selectionType, $leaseType)
    (Field 'ridingDogSelections').GetValue($service).Add($pairType.GetConstructor([Type[]]@($selectionType, $leaseType)).Invoke(@($fixture.dogSelection, $dogLease)))
    $required = Selection-Array @($fixture.combatLists[0], $fixture.combatLists[1])
    (Field 'mountedCombatPublication').SetValue($service, $setCtor.Invoke(@((Selection-Array $fixture.combatLists), $required, $fixture.combat, $fixture.archery, $fixture.shield, $read, $write)))
    $fixture.service = $service
    $fixture
}
function Set-Enabled($Fixture, [bool]$Enabled) { [bool]$setSelectionEnabled.Invoke($Fixture.service, @($Enabled)) }
function Desired($Fixture) { [bool](Field 'selectionDesired').GetValue($Fixture.service) }
# The root's retained state: every KMC surface published exactly once after its foreign entries, or every surface exactly
# original, and the service's stored desired state equal to it.
function Assert-Matches($Fixture, [bool]$Retained, [string]$Label) {
    $ok = $true; $detail = @()
    foreach ($list in $Fixture.combatLists) {
        $original = $Fixture.combatOriginal[[Runtime.CompilerServices.RuntimeHelpers]::GetHashCode($list)]
        $expected = if ($Retained) { @($original) + $Fixture.kmc } else { $original }
        if (-not (Same $allFeatures.GetValue($list) $expected)) { $ok = $false; $detail += ('combat list holds ' + $allFeatures.GetValue($list).Count + ' entries') }
    }
    $dogExpected = if ($Retained) { @($Fixture.dogOriginal) + @($Fixture.ridingDog) } else { @($Fixture.dogOriginal) }
    if (-not (Same $allFeatures.GetValue($Fixture.dogSelection) $dogExpected)) { $ok = $false; $detail += 'Riding Dog list differs' }
    if ((Desired $Fixture) -ne $Retained) { $ok = $false; $detail += ('stored desired state ' + (Desired $Fixture)) }
    Check $ok ($Label + ' (root retains ' + $(if ($Retained) { 'enabled' } else { 'disabled' }) + ')' + $(if ($detail) { ': ' + ($detail -join '; ') } else { '' }))
}
function Drift-Ranger($Fixture) {
    # Another mod replaces the Ranger companion selection with its own contents.
    $current = @($allFeatures.GetValue($Fixture.ranger))
    $Fixture.rangerBeforeDrift = $allFeatures.GetValue($Fixture.ranger)
    $Fixture.drifted = Feature-Array (@($Fixture.foreignC) + $current)
    $allFeatures.SetValue($Fixture.ranger, $Fixture.drifted)
}
function Drift-Kept($Fixture) { [object]::ReferenceEquals($allFeatures.GetValue($Fixture.ranger), $Fixture.drifted) }
function Undo-Drift($Fixture) { $allFeatures.SetValue($Fixture.ranger, $Fixture.rangerBeforeDrift) }

foreach ($withArchery in @($true, $false)) {
    $variant = if ($withArchery) { 'three feats' } else { 'Mounted Archery not registered' }
    # Two consecutive refused disables, then a successful disable.
    $f = New-Registered $withArchery
    Check (Set-Enabled $f $true) ($variant + ': the first enable succeeds')
    Assert-Matches $f $true ($variant + ': enabled publishes each KMC feat once after the foreign entries')
    Drift-Ranger $f
    Check (-not (Set-Enabled $f $false)) ($variant + ': disable is refused while the Ranger selection is changed outside KMC')
    Assert-Matches $f $true ($variant + ': after the first refused disable')
    Check (-not (Set-Enabled $f $false)) ($variant + ': a second disable is refused')
    Assert-Matches $f $true ($variant + ': after the second refused disable')
    Check (@($lines | Where-Object { $_ -like 'Error: Ranger AllFeatures restore rejected*' }).Count -eq 2) ($variant + ': each refused disable reports the Ranger drift')
    Check (Drift-Kept $f) ($variant + ': the other mod''s Ranger selection is never overwritten by a refused disable')
    Undo-Drift $f
    Check (Set-Enabled $f $false) ($variant + ': once the drift is gone the disable succeeds')
    Assert-Matches $f $false ($variant + ': after the successful disable every list is exactly its original')
    # Two consecutive refused enables, then a successful enable.
    Drift-Ranger $f
    Check (-not (Set-Enabled $f $true)) ($variant + ': enable is refused while the Ranger selection is changed outside KMC')
    Assert-Matches $f $false ($variant + ': after the first refused enable')
    Check (-not (Set-Enabled $f $true)) ($variant + ': a second enable is refused')
    Assert-Matches $f $false ($variant + ': after the second refused enable')
    Check (Drift-Kept $f) ($variant + ': the other mod''s Ranger selection is never overwritten by a refused enable')
    Undo-Drift $f
    Check (Set-Enabled $f $true) ($variant + ': once the drift is gone the enable succeeds')
    Assert-Matches $f $true ($variant + ': after the successful enable each KMC feat is offered once')
    Check (Same $allFeatures.GetValue($f.ranger) (@($f.rangerOriginal) + @($f.horse))) ($variant + ': the Ranger selection holds its original entries and one Horse')
    # Refused enable from a disabled start followed by refused disable is not reachable (a refused enable keeps the root
    # disabled, and the root does not disable twice); a refused disable after the refused enables is covered above.
    Check (Set-Enabled $f $true) ($variant + ': a repeated enable is idempotent')
    Assert-Matches $f $true ($variant + ': a repeated enable adds no duplicate')
    # A service that is no longer Registered refuses without keeping the requested state.
    (Field 'state').SetValue($f.service, [Enum]::Parse($stateType, 'Faulted'))
    Check (-not (Set-Enabled $f $false)) ($variant + ': a faulted service refuses the disable')
    Assert-Matches $f $true ($variant + ': after a faulted refusal the stored state and every list are unchanged')
    (Field 'state').SetValue($f.service, [Enum]::Parse($stateType, 'Registered'))
}

Write-Host ('TOTAL PASS=' + $script:passed + ' FAIL=' + $script:failures.Count)
if ($script:failures.Count -ne 0) { exit 1 }
