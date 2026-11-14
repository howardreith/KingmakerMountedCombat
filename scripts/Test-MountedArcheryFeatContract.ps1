[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Archery feat registration contract (0.2.9 player report: "Mounted Archery did not retain the single-rank
# combat-feat contract", feat absent from every list).
#  1. Reads the stock donor feats from the installed Kingmaker 2.1.7b data (pinned file identities, exact object offsets
#     and sizes from the serialized object table, managed script resolved through its MonoScript). BlueprintFeature's
#     serialized order comes from the installed Assembly-CSharp; every C# bool is a 4-byte aligned field.
#  2. Runs the compiled product's own Mounted Archery construction, validation and selection publication on genuine
#     Kingmaker blueprint objects created without Unity. In this UnityEngine.CoreModule, Object.Equals between two
#     non-null objects is reference identity and GetHashCode is the managed one, so they compare as live objects do.
# Not covered: Unity object creation, the blueprint library, the level-up UI and native prerequisite evaluation.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lab = [IO.Path]::GetFullPath((Join-Path $repoRoot '..\..'))
$layout = (Get-Content -Raw (Join-Path $lab 'environment-intake.json') | ConvertFrom-Json).requestedLayout
$data = Join-Path $layout.kingmakerInstallDir 'Kingmaker_Data'
$managed = Join-Path $data 'Managed'
$productDll = Join-Path $repoRoot ('bin\' + $Configuration + '\KingmakerMountedCombat.dll')
$script:passed = 0
function Check([bool]$Value, [string]$Label) { if (-not $Value) { throw ('Mounted Archery feat contract: ' + $Label) }; $script:passed++; Write-Host ('PASS ' + $Label) }

# ---- 1. Stock donors in the installed data ------------------------------------------------------------------------
$assets = Join-Path $data 'sharedassets1.assets'
$scripts = Join-Path $data 'globalgamemanagers.assets'
Check ((Get-FileHash -LiteralPath $assets).Hash.ToLowerInvariant() -ceq 'cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785') 'pinned sharedassets1 identity'
Check ((Get-FileHash -LiteralPath $scripts).Hash.ToLowerInvariant() -ceq '633c82b4869b8879556496b58efd1de78a5bee26ab5dd1d77643952913e574d2') 'pinned MonoScript asset identity'
function UnityString($Reader, [int]$Max) {
    $length = $Reader.ReadInt32(); if ($length -lt 0 -or $length -gt $Max) { throw 'Unexpected bounded Unity string' }
    $text = [Text.Encoding]::UTF8.GetString($Reader.ReadBytes($length))
    [void]$Reader.BaseStream.Seek((4 - $Reader.BaseStream.Position % 4) % 4, [IO.SeekOrigin]::Current)
    $text
}
function Bool32($Reader, [string]$Field) { $value = $Reader.ReadInt32(); if ($value -ne 0 -and $value -ne 1) { throw ('Unaligned or invalid bool ' + $Field) }; $value -eq 1 }
# PrototypeLink, Components, m_AssetGuid; m_AllowNonContextActions, m_DisplayName, m_Description, m_Icon; HideInUI,
# HideInCharacterSheetAndLevelUp, HideNotAvailibleInUI, DlcType; Groups, Ranks, ReapplyOnLevelUp, IsClassFeature.
function Read-StockFeature($Reader, [long]$Offset, [int]$Size, [string]$Name, [string]$Guid) {
    [void]$Reader.BaseStream.Seek($Offset + 16, [IO.SeekOrigin]::Begin)
    Check (($Reader.ReadInt32() -eq 1) -and ($Reader.ReadInt64() -eq 6001)) ($Name + ' managed script pointer is BlueprintFeature')
    Check ((UnityString $Reader 256) -ceq $Name) ($Name + ' authored name')
    [void]$Reader.ReadBytes(12)
    $components = $Reader.ReadInt32(); if ($components -lt 0 -or $components -gt 64) { throw 'Unexpected component count' }
    [void]$Reader.ReadBytes(12 * $components)
    Check ((UnityString $Reader 32) -ceq $Guid) ($Name + ' GUID ' + $Guid)
    [void](Bool32 $Reader 'm_AllowNonContextActions')
    for ($i = 0; $i -lt 2; $i++) { [void](UnityString $Reader 256); [void]$Reader.ReadBytes(12) }
    $iconFile = $Reader.ReadInt32(); $iconPath = $Reader.ReadInt64()
    $feature = [ordered]@{ Name = $Name; HasIcon = ($iconFile -ne 0 -or $iconPath -ne 0)
        HideInUI = (Bool32 $Reader 'HideInUI'); HideInCharacterSheetAndLevelUp = (Bool32 $Reader 'HideInCharacterSheetAndLevelUp')
        HideNotAvailibleInUI = (Bool32 $Reader 'HideNotAvailibleInUI'); DlcType = $Reader.ReadInt32() }
    $count = $Reader.ReadInt32(); if ($count -lt 0 -or $count -gt 16) { throw 'Unexpected group count' }
    $feature.Groups = @(for ($i = 0; $i -lt $count; $i++) { $Reader.ReadInt32() })
    $feature.Ranks = $Reader.ReadInt32()
    $feature.ReapplyOnLevelUp = Bool32 $Reader 'ReapplyOnLevelUp'
    $feature.IsClassFeature = Bool32 $Reader 'IsClassFeature'
    Check ($Reader.BaseStream.Position -eq $Offset + $Size) ($Name + ' decoding ends exactly at the serialized object end')
    [pscustomobject]$feature
}
$reader = [IO.BinaryReader]::new([IO.File]::OpenRead($assets))
$mono = [IO.BinaryReader]::new([IO.File]::OpenRead($scripts))
try {
    [void]$mono.BaseStream.Seek(925416, [IO.SeekOrigin]::Begin)
    $monoName = UnityString $mono 256; [void]$mono.BaseStream.Seek(20, [IO.SeekOrigin]::Current)
    $class = UnityString $mono 256; $namespace = UnityString $mono 256; $assembly = UnityString $mono 256
    Check ($monoName -ceq 'BlueprintFeature' -and $class -ceq 'BlueprintFeature' -and $namespace -ceq 'Kingmaker.Blueprints.Classes' -and $assembly -ceq 'Assembly-CSharp.dll') 'MonoScript 6001 is Kingmaker.Blueprints.Classes.BlueprintFeature'
    # Object offset, size (serialized object table) and identity.
    $stock = @{}
    foreach ($entry in @(
        @(154062912, 252, 'PowerAttackFeature', '9972f33f977fc724c838e59641b2fca5'),
        @(154020960, 276, 'Dodge', '97e216dbb46ae3c4faef90cf6bbe6fd5'),
        @(154057728, 288, 'Mobility', '2a6091b97ad940943b46262600eaeaeb'),
        @(154060432, 308, 'PointBlankShot', '0da0c194d6e1d43419eb8d990b28e0ab'),
        @(154077752, 236, 'RapidShotFeature', '9c928dc570bb9e54a9649b3ebfe47a41'),
        @(154213648, 264, 'Toughness', 'd09b20029e9abfe4480b356c92095623'))) {
        $stock[$entry[2]] = Read-StockFeature $reader $entry[0] $entry[1] $entry[2] $entry[3]
    }
} finally { $reader.Dispose(); $mono.Dispose() }
foreach ($feature in $stock.Values) {
    Write-Host ('  stock ' + $feature.Name + ': groups=[' + ($feature.Groups -join ',') + '] ranks=' + $feature.Ranks + ' IsClassFeature=' + $feature.IsClassFeature +
        ' hidden=' + $feature.HideInUI + '/' + $feature.HideInCharacterSheetAndLevelUp + '/' + $feature.HideNotAvailibleInUI + ' icon=' + $feature.HasIcon)
    Check $feature.IsClassFeature ('stock feat ' + $feature.Name + ' holds IsClassFeature=true')
}
$power = $stock['PowerAttackFeature']
Check ((($power.Groups | Sort-Object) -join ',') -ceq '1,2' -and $power.Ranks -eq 1 -and -not $power.ReapplyOnLevelUp -and $power.HasIcon -and
    -not $power.HideInUI -and -not $power.HideInCharacterSheetAndLevelUp -and -not $power.HideNotAvailibleInUI -and $power.DlcType -eq 0) 'stock Power Attack is a visible single-rank Feat and CombatFeat'

# ---- 2. Production construction, validation and publication on genuine blueprints ---------------------------------
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
$unity = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityEngine.CoreModule.dll'))
$kmc = [Reflection.Assembly]::LoadFile($productDll)
$featureType = $game.GetType('Kingmaker.Blueprints.Classes.BlueprintFeature', $true)
$selectionType = $game.GetType('Kingmaker.Blueprints.Classes.Selection.BlueprintFeatureSelection', $true)
$groupType = $game.GetType('Kingmaker.Blueprints.Classes.FeatureGroup', $true)
$spriteType = $unity.GetType('UnityEngine.Sprite', $true)
$iconField = $game.GetType('Kingmaker.Blueprints.Facts.BlueprintUnitFact', $true).GetField('m_Icon', [Reflection.BindingFlags]'Instance,NonPublic')
Check ($null -ne $iconField -and $iconField.MetadataToken -eq 0x04006957) 'exact native icon field'
function New-Native([Type]$Type) { [Runtime.Serialization.FormatterServices]::GetUninitializedObject($Type) }
function Groups([int[]]$Values) { $array = [Array]::CreateInstance($groupType, $Values.Count); for ($i = 0; $i -lt $Values.Count; $i++) { $array.SetValue([Enum]::ToObject($groupType, $Values[$i]), $i) }; , $array }
$feat = [Enum]::ToObject($groupType, 1); $combatFeat = [Enum]::ToObject($groupType, 2)
$service = $kmc.GetType('KingmakerMountedCombat.Integration.HorseCompanionBlueprintService', $true)
$static = [Reflection.BindingFlags]'Static,NonPublic'
$build = $service.GetMethod('BuildMountedArcheryContract', $static)
$validate = $service.GetMethod('MountedArcheryContractViolation', $static)
$copy = $service.GetMethod('CopyFeatureContract', $static)
Check ($null -ne $build -and $null -ne $validate -and $null -ne $copy) 'product exposes the registration construction and validation it runs'

function New-StockDonor($Stock) {
    $donor = New-Native $featureType
    $featureType.GetField('Groups').SetValue($donor, (Groups $Stock.Groups))
    foreach ($field in 'Ranks', 'ReapplyOnLevelUp', 'IsClassFeature', 'HideInUI', 'HideInCharacterSheetAndLevelUp', 'HideNotAvailibleInUI') { $featureType.GetField($field).SetValue($donor, $Stock.$field) }
    if ($Stock.HasIcon) { $iconField.SetValue($donor, (New-Native $spriteType)) }
    $donor
}
# Kingmaker blueprints have both "name" and "Name", which PowerShell's property adapter refuses; read them by reflection.
function F($Value, [string]$Field) { $featureType.GetField($Field).GetValue($Value) }
function SetF($Value, [string]$Field, $New) { $featureType.GetField($Field).SetValue($Value, $New) }
$iconGetter = $featureType.GetProperty('Icon')
$allFeatures = $selectionType.GetField('AllFeatures')
function Offered($Selection) { , $allFeatures.GetValue($Selection) }
$donor = New-StockDonor $power
$icon = New-Native $spriteType
$archery = New-Native $featureType
[void]$build.Invoke($null, @($donor, $archery, $null, $null, $icon))
$violation = $validate.Invoke($null, @($archery, $donor, $icon))
$groups = F $archery 'Groups'
Write-Host ('  built Mounted Archery: groups=[' + ($groups -join ',') + '] ranks=' + (F $archery 'Ranks') + ' IsClassFeature=' + (F $archery 'IsClassFeature') +
    ' hidden=' + (F $archery 'HideInUI') + '/' + (F $archery 'HideInCharacterSheetAndLevelUp') + '/' + (F $archery 'HideNotAvailibleInUI') + ' dlc=' + (F $archery 'DlcType'))
# The released 0.2.9 condition on the same object: the reported exception.
$release029Rejects = -not ($groups -contains $feat) -or -not ($groups -contains $combatFeat) -or (F $archery 'Ranks') -ne 1 -or
    (F $archery 'IsClassFeature') -or -not [object]::ReferenceEquals($iconGetter.GetValue($archery, $null), $icon)
Check $release029Rejects 'the released 0.2.9 rule rejects the feat built from stock Power Attack (reproduces the reported exception)'
Check ($null -eq $violation) ('the product contract accepts the feat built from stock Power Attack' + $(if ($null -ne $violation) { ': ' + $violation } else { '' }))
Check (($groups -contains $feat) -and ($groups -contains $combatFeat) -and (F $archery 'Ranks') -eq 1 -and (F $archery 'IsClassFeature') -and
    [object]::ReferenceEquals($iconGetter.GetValue($archery, $null), $icon) -and -not (F $archery 'HideInUI') -and -not (F $archery 'HideInCharacterSheetAndLevelUp') -and
    -not (F $archery 'HideNotAvailibleInUI') -and [string](F $archery 'DlcType') -ceq 'None') 'built feat: visible single-rank Feat and CombatFeat, stock class flag, KMC icon, base-game DLC'
Check (-not [object]::ReferenceEquals($groups, (F $donor 'Groups'))) 'built feat owns its group array (the stock donor is not aliased)'

# Failures on genuine objects name the failed invariant and the observed values.
SetF $archery 'IsClassFeature' $false
$diverged = [string]$validate.Invoke($null, @($archery, $donor, $icon))
SetF $archery 'IsClassFeature' $true; SetF $archery 'Ranks' 2; $iconField.SetValue($archery, (New-Native $spriteType))
$several = [string]$validate.Invoke($null, @($archery, $donor, $icon))
Write-Host ('  diagnostic sample: ' + $several)
Check ($diverged.StartsWith('Mounted Archery did not retain the single-rank combat-feat contract: IsClassFeature=False (expected the stock donor''s True). Feature ')) 'a diverged class flag is named with both values'
Check ($several.StartsWith('Mounted Archery did not retain the single-rank combat-feat contract: ranks=2 (expected 1); icon is not KMC''s Mounted Archery icon. Feature ') -and
    $several.Contains('donor PowerAttackFeature groups=[CombatFeat,Feat] ranks=1 IsClassFeature=True icon=other')) 'several failures are listed with feature and donor facts'
SetF $archery 'Ranks' 1; $iconField.SetValue($archery, $icon)
Check ($null -eq $validate.Invoke($null, @($archery, $donor, $icon))) 'restored feat is accepted again'

# Publication: Mounted Combat (same donor, production CopyFeatureContract) and Mounted Archery appended once to the
# ordinary, fighter and other combat lists; Mounted Archery's availability (the ranged penalty's gate) follows.
$combat = New-Native $featureType
[void]$copy.Invoke($null, @($donor, $combat))
$dodge = New-StockDonor $stock['Dodge']; $foreign = New-Native $featureType
$featureArrayType = $featureType.MakeArrayType()
function Feature-Array([object[]]$Items) { $array = [Array]::CreateInstance($featureType, $Items.Count); for ($i = 0; $i -lt $Items.Count; $i++) { $array.SetValue($Items[$i], $i) }; , $array }
function Selection-Array([object[]]$Items) { $array = [Array]::CreateInstance($selectionType, $Items.Count); for ($i = 0; $i -lt $Items.Count; $i++) { $array.SetValue($Items[$i], $i) }; , $array }
$ordinary = New-Native $selectionType; $fighter = New-Native $selectionType; $other = New-Native $selectionType
$originals = New-Object 'System.Collections.Generic.List[object]'
foreach ($selection in @($ordinary, $fighter, $other)) { $value = Feature-Array @($donor, $foreign, $dodge); $allFeatures.SetValue($selection, $value); $originals.Add($value) }
$read = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection) , $script:allFeatures.GetValue($selection) }, [Func``2].MakeGenericType($selectionType, $featureArrayType))
$write = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection, $value) $script:allFeatures.SetValue($selection, $value) }, [Action``2].MakeGenericType($selectionType, $featureArrayType))
$label = [Management.Automation.LanguagePrimitives]::ConvertTo([scriptblock]{ param($selection) 'selection' }, [Func``2].MakeGenericType($selectionType, [string]))
$setType = $kmc.GetType('KingmakerMountedCombat.Domain.KmcCombatFeatPublicationSet`2', $true).MakeGenericType($selectionType, $featureType)
function New-Publication($MountedArchery) {
    $setType.GetConstructors()[0].Invoke(@((Selection-Array @($ordinary, $fighter, $other)), (Selection-Array @($ordinary, $fighter)), $combat, $MountedArchery, $read, $write))
}
$publication = New-Publication $archery
Check ($null -eq $publication.Apply($true, $label)) 'publication writes every selection without drift'
foreach ($selection in @($ordinary, $fighter, $other)) {
    $now = Offered $selection
    Check ($now.Count -eq 5 -and [object]::ReferenceEquals($now[0], $donor) -and [object]::ReferenceEquals($now[1], $foreign) -and [object]::ReferenceEquals($now[2], $dodge) -and
        [object]::ReferenceEquals($now[3], $combat) -and [object]::ReferenceEquals($now[4], $archery) -and $publication.Offers($selection)) 'a selection keeps its entries and order and offers Mounted Combat then Mounted Archery once'
}
Check ($publication.MountedArcheryAvailable() -and -not $publication.MountedArcheryWithdrawn) 'Mounted Archery is available, so the ranged penalty and its reduction apply'
Check ($null -eq $publication.Apply($false, $label)) 'disable restores every selection without drift'
$index = 0
foreach ($selection in @($ordinary, $fighter, $other)) {
    $now = Offered $selection; $was = $originals[$index++]
    Check ($now.Count -eq 3 -and [object]::ReferenceEquals($now[0], $was[0]) -and [object]::ReferenceEquals($now[1], $was[1]) -and [object]::ReferenceEquals($now[2], $was[2])) 'a selection returns to its exact original'
}
# A registration failure removes Mounted Archery only: Mounted Combat is still offered and the penalty stays off.
$withoutArchery = New-Publication $null
$applied = $withoutArchery.Apply($true, $label); $now = Offered $ordinary
Check ($null -eq $applied -and -not $withoutArchery.MountedArcheryAvailable() -and $now.Count -eq 4 -and [object]::ReferenceEquals($now[3], $combat) -and
    [Array]::IndexOf($now, $archery) -lt 0) 'without Mounted Archery only Mounted Combat is offered and the penalty stays off'
[void]$withoutArchery.Apply($false, $label)
Write-Host ('Mounted Archery feat contract PASS=' + $script:passed + ' FAIL=0 (product ' + $productDll + ' sha256=' + (Get-FileHash -LiteralPath $productDll).Hash.ToLowerInvariant() + ')')
