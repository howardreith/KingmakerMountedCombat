[CmdletBinding()]
param([string]$Configuration = 'Release')
# Mounted Combat alpha 2 (docs/missions/mounted-alpha2-melee-pose/MISSION.md), MMR-01 Horse seat: installed-contract and
# production-path checks, split by area from scripts/Test-MountedAlpha2NativeContract.ps1 at 46444b30 (PR #64, not accepted);
# every check line and label below is that script's, verbatim, with the shared preamble.
# MMR-01: the compiled profile-to-adapter seat path and the installed animation/pause members the settle gate reads.
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
# ---- 4. MMR-01 seat wiring (review R1-F12): the compiled profile-to-adapter path, not a copy of it -------------------
$profiles = KmcType 'KingmakerMountedCombat.Integration.SupportedMountedProfiles'
$profileType = KmcType 'KingmakerMountedCombat.Integration.SupportedMountedProfile'
$settles = $profileType.GetProperty('SettlesSeatFromIdlePose')
$calibrationType = KmcType 'KingmakerMountedCombat.Domain.AnimatedSaddleCalibration'
$horseProfile = $profiles.GetField('Horse', $any).GetValue($null); $mammothProfile = $profiles.GetField('Mammoth', $any).GetValue($null)
Check ($null -eq $profileType.GetProperty('DeclaredSeatFromSource') -and @($calibrationType.GetFields($any) | Where-Object { $_.FieldType.Name -eq 'PoseVector3' }).Count -eq 0) 'MMR-01: no literal seat basis remains in the product (the Horse basis is the rider''s own settled pose, measured live)'
Check ([bool]$settles.GetValue($horseProfile)) 'MMR-01: the Horse profile settles its seat on the rider''s own still pose'
Check (-not [bool]$settles.GetValue($mammothProfile)) 'MMR-01: the Mammoth keeps its own calibration (no animated saddle)'
$small = @($profiles.GetField('small', $any).GetValue($null))
Check ($small.Count -gt 0 -and @($small | Where-Object { [bool]$settles.GetValue($_) }).Count -eq 0) ('MMR-01: all ' + $small.Count + ' Small profiles keep their own fixed-from-source seat')
function CallsMethod([Type]$Type, [Reflection.MethodBase]$Target) {
    foreach ($t in @($Type) + @($Type.GetNestedTypes($any))) {
        foreach ($m in $t.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly)) {
            $body = $m.GetMethodBody(); if ($null -eq $body) { continue }
            $il = $body.GetILAsByteArray()
            for ($i = 0; $i -le $il.Length - 5; $i++) {
                if (($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) -and [BitConverter]::ToInt32($il, $i + 1) -eq $Target.MetadataToken) {
                    try { if ($m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) -eq $Target) { return $true } } catch { }
                }
            }
        }
    }
    $false
}
$runtimeType = KmcType 'KingmakerMountedCombat.Integration.KingmakerMountedPairRuntime'
$adapterType = KmcType 'KingmakerMountedCombat.Integration.MountedRiderPoseAdapter'
$observe = (KmcType 'KingmakerMountedCombat.Domain.SettledSeatSampler').GetMethod('Observe')
$notice = $calibrationType.GetMethod('FormatNotice')
Check (CallsMethod $runtimeType $settles.GetGetMethod()) 'MMR-01: pair attachment passes the profile''s settled-seat flag to the pose adapter'
Check (CallsMethod $adapterType $observe) 'MMR-01: the adapter''s seat frames decide the settled sample through SettledSeatSampler.Observe'
Check (CallsMethod $runtimeType $notice) 'MMR-01: pair attachment logs the attachment and settled seat samples (seatCalibration lines) on the normal path'
# R5-F3/F4: the normalized seat sample and the animation-advance gate bind installed members.
Check (CallsMethod $adapterType ($calibrationType.GetMethod('NormalizedSeatFromSource'))) 'MMR-01 (R5-F3): the adapter samples the seat through the normalized anchor-relation-plus-rider-local formula'
# Cross-module call sites carry MemberRef tokens: resolve each call site and compare the resolved member.
function CallsResolved([Reflection.MethodBase]$Method, [Reflection.MethodBase]$Target) {
    $il = $Method.GetMethodBody().GetILAsByteArray()
    for ($i = 0; $i -le $il.Length - 5; $i++) { if ($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) { try { if ($Method.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)) -eq $Target) { return $true } } catch { } } }
    $false
}
$advance = $adapterType.GetMethod('SeatAnimationAdvanceSeconds', [Reflection.BindingFlags]'Instance,NonPublic')
$nativeAnimation = GameType 'Kingmaker.Visual.Animation.AnimationManager'
$disabled = $nativeAnimation.GetProperty('Disabled').GetGetMethod()
$unityTime = [Type]::GetType('UnityEngine.Time, UnityEngine.CoreModule', $true)
$deltaTime = $unityTime.GetProperty('deltaTime').GetGetMethod()
Check ((CallsResolved $advance $disabled) -and (CallsResolved $advance $deltaTime)) 'MMR-01 (R5-F4): the settle gate reads the rider AnimationManager.Disabled flag and Unity Time.deltaTime'
$updateDt = $nativeAnimation.GetMethod('Update', [Type[]]@([single]))
Check (CallsResolved $updateDt $disabled) 'MMR-01 (R5-F4): installed AnimationManager advances its animations only while not Disabled'
$suspend = (GameType 'Kingmaker.Controllers.TimeController').GetMethod('Suspend')
$setScale = $unityTime.GetProperty('timeScale').GetSetMethod()
Check (CallsResolved $suspend $setScale) 'MMR-01 (R5-F4): installed TimeController.Suspend (pause) sets Time.timeScale, so a paused frame has no animation delta'
Write-Host ('Mounted alpha 2 native contract (seat) PASS=' + $script:passed + ' FAIL=0')
