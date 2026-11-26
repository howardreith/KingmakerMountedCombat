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
# ---- PR-3 (reviews R5-F4, R7-F2, R7-F3): the installed members behind fresh evaluation and rest eligibility ------------------
$unitView = GameType 'Kingmaker.View.UnitEntityView'
$animatorType = [Type]::GetType('UnityEngine.Animator, UnityEngine.AnimationModule', $true)
$setCulling = $animatorType.GetProperty('cullingMode').GetSetMethod()
$attached = $unitView.GetMethod('OnDataAttached', $any)
$attachedIl = $attached.GetMethodBody().GetILAsByteArray(); $alwaysAnimate = $false
for ($i = 1; $i -le $attachedIl.Length - 5; $i++) { if ($attachedIl[$i] -eq 0x6F -and $attachedIl[$i - 1] -eq 0x16) { try { if ($attached.Module.ResolveMethod([BitConverter]::ToInt32($attachedIl, $i + 1)) -eq $setCulling) { $alwaysAnimate = $true } } catch { } } }
Check $alwaysAnimate 'MMR-01 (R5-F4): installed UnitEntityView.OnDataAttached sets every unit Animator.cullingMode = 0 (AlwaysAnimate): off-screen culling does not stop the skeleton writes'
$graphType = [Type]::GetType('UnityEngine.Playables.PlayableGraph, UnityEngine.CoreModule', $false); if ($null -eq $graphType) { $graphType = [Type]::GetType('UnityEngine.Playables.PlayableGraph, UnityEngine.DirectorModule', $true) }
$outputType = [Type]::GetType('UnityEngine.Animations.AnimationPlayableOutput, UnityEngine.AnimationModule', $true)
$onEnable = $nativeAnimation.GetMethod('OnEnable', $any)
Check ((CallsResolved $onEnable ($graphType.GetMethod('Play'))) -and (CallsResolved $onEnable ($outputType.GetMethod('Create', [Reflection.BindingFlags]'Static,Public')))) 'MMR-01 (R5-F4): installed AnimationManager.OnEnable creates and plays its PlayableGraph with an AnimationPlayableOutput bound to the unit Animator'
$unitAnimation = GameType 'Kingmaker.Visual.Animation.Kingmaker.UnitAnimationManager'
$canIdle = $unitAnimation.GetMethod('CanRunIdleAction', $any)
$handleType = GameType 'Kingmaker.Visual.Animation.Actions.AnimationActionHandle'
$actionBase = GameType 'Kingmaker.Visual.Animation.Actions.AnimationActionBase'
$canIdleTokens = IlTokens $canIdle
Check ($canIdle.IsPublic -and (CallsResolved $canIdle ($unitAnimation.GetProperty('Speed').GetGetMethod())) -and (CallsResolved $canIdle ($nativeAnimation.GetProperty('ActiveActions').GetGetMethod())) -and
    (CallsResolved $canIdle ($handleType.GetProperty('IsReleased').GetGetMethod())) -and (CallsResolved $canIdle ($actionBase.GetProperty('IsAdditive').GetGetMethod())) -and
    $canIdleTokens.ContainsKey($unitAnimation.GetField('m_LocoMotionHandle', $any).MetadataToken)) 'MMR-01 (R7-F2/F3): installed UnitAnimationManager.CanRunIdleAction is the native idle stage: animation Speed <= 0 and every active action is the locomotion handle, additive or released'
$tickOnUnit = (GameType 'Kingmaker.Controllers.Units.UnitAnimationController').GetMethod('TickOnUnit', $any)
Check ((CallsResolved $tickOnUnit ((GameType 'Kingmaker.View.UnitMovementAgentBase').GetProperty('Speed').GetGetMethod())) -and (CallsResolved $tickOnUnit ($unitAnimation.GetProperty('Speed').GetSetMethod()))) 'MMR-01 (R7-F3): installed UnitAnimationController.TickOnUnit sets the animation Speed from the unit view''s movement agent Speed'
$riderAgent = KmcType 'KingmakerMountedCombat.Integration.RiderMovementAgent'
$agentBase = GameType 'Kingmaker.View.UnitMovementAgentBase'
$readsMountSpeed = $false
foreach ($m in $riderAgent.GetMethods($any + [Reflection.BindingFlags]::DeclaredOnly)) { if ($m.Name -cne 'UpdateVelocity') { continue }; $il = $m.GetMethodBody().GetILAsByteArray(); for ($i = 0; $i -le $il.Length - 5; $i++) { if ($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) { try { $r = $m.Module.ResolveMethod([BitConverter]::ToInt32($il, $i + 1)); if ($r.Name -ceq 'get_Speed' -and $agentBase.IsAssignableFrom($r.DeclaringType)) { $readsMountSpeed = $true } } catch { } } } }
Check $readsMountSpeed 'MMR-01 (R7-F3): the carried rider''s movement agent (RiderMovementAgent.UpdateVelocity) reports the mount agent''s Speed, so the pair moving is not native idle'
$rest = $adapterType.GetMethod('SeatRestEligible', [Reflection.BindingFlags]'Instance,NonPublic')
$animationBase = GameType 'Kingmaker.Visual.Animation.AnimationBase'
Check ((CallsResolved $rest $canIdle) -and (CallsResolved $rest ($animationBase.GetMethod('GetWeight', [Type[]]@()))) -and (CallsResolved $rest ($handleType.GetProperty('IsReleased').GetGetMethod()))) 'MMR-01 (R7-F2/F3): the adapter''s rest eligibility is native CanRunIdleAction plus no released action still blending out'
Check ((CallsResolved $advance ($animatorType.GetProperty('cullingMode').GetGetMethod())) -and (CallsResolved $advance ($graphType.GetMethod('IsPlaying'))) -and (CallsResolved $advance ($graphType.GetMethod('IsValid')))) 'MMR-01 (R5-F4): the adapter''s fresh-evaluation gate reads the Animator culling mode and the native graph''s IsValid/IsPlaying'
# ---- PR-3 review R1-F1 (round 2): the native resting animation the rest predicate counts --------------------------------
$declared = [Reflection.BindingFlags]'Instance,Static,Public,NonPublic,DeclaredOnly'
$tickInternal = $unitAnimation.GetMethod('TickInternal', $declared)
$tickIl = $tickInternal.GetMethodBody().GetILAsByteArray()
$locoField = $unitAnimation.GetField('m_LocoMotionHandle', $any)
$unitType = GameType 'Kingmaker.Visual.Animation.Kingmaker.UnitAnimationType'
$createByType = $unitAnimation.GetMethod('CreateHandle', $any, $null, [Type[]]@($unitType, [bool]), $null)
$execute = $nativeAnimation.GetMethod('Execute', [Type[]]@($handleType))
$createsLoco = $false
for ($i = 2; $i -le $tickIl.Length - 10; $i++) {
    if ($tickIl[$i] -eq 0x28 -and $tickIl[$i - 2] -eq 0x17 -and [int][Enum]::Parse($unitType, 'LocoMotion') -eq 1) {
        try { if ($tickInternal.Module.ResolveMethod([BitConverter]::ToInt32($tickIl, $i + 1)) -eq $createByType -and $tickIl[$i + 5] -eq 0x7D -and $tickInternal.Module.ResolveField([BitConverter]::ToInt32($tickIl, $i + 6)) -eq $locoField) { $createsLoco = $true } } catch { }
    }
}
Check ($createsLoco -and (CallsResolved $tickInternal $execute) -and (CallsResolved $tickInternal ($nativeAnimation.GetProperty('AnimationSet').GetGetMethod()))) 'MMR-01 (PR-3 R1-F1): installed UnitAnimationManager.TickInternal, once an AnimationSet is bound, creates m_LocoMotionHandle with CreateHandle(UnitAnimationType.LocoMotion = 1) and Executes it'
$unitAction = GameType 'Kingmaker.Visual.Animation.Kingmaker.UnitAnimationAction'
$typeGetter = $unitAction.GetProperty('Type').GetGetMethod()
$locoActions = @()
foreach ($t in $game.GetTypes()) {
    if ($t.IsAbstract -or -not $unitAction.IsAssignableFrom($t)) { continue }
    $g = $t.GetMethod('get_Type', [Reflection.BindingFlags]'Instance,Public,NonPublic'); if ($null -eq $g -or $null -eq $g.GetMethodBody()) { continue }
    $il = $g.GetMethodBody().GetILAsByteArray(); if ($il.Length -eq 2 -and $il[0] -eq 0x17 -and $il[1] -eq 0x2A) { $locoActions += $t.Name }
}
Check ($typeGetter.IsPublic -and (($locoActions | Sort-Object) -join ',') -ceq 'UnitAnimationActionLocoMotion,UnitAnimationActionLocoMotionHuman') ('MMR-01 (PR-3 R1-F1): installed public UnitAnimationAction.Type is LocoMotion for exactly the locomotion actions: ' + ($locoActions -join ', '))
$human = GameType 'Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionLocoMotionHuman'
$humanStart = $human.GetMethod('OnStart', $declared); $humanClips = $human.GetMethod('UpdateCurrentClips', $declared)
$addClip = @($nativeAnimation.GetMethods($any) | Where-Object { $_.Name -ceq 'AddAnimationClip' })
$addControllers = @($nativeAnimation.GetMethods($declared) | Where-Object { $_.Name -ceq 'AddAnimatorController' -and $null -ne $_.GetMethodBody() })
$setActive = $handleType.GetProperty('ActiveAnimation').GetSetMethod($true)
$activeList = $nativeAnimation.GetField('m_ActiveAnimations', $any)
function StoresActive([Reflection.MethodBase]$Method) { (CallsResolved $Method $setActive) -and (IlTokens $Method).ContainsKey($activeList.MetadataToken) }
$clipAdds = @($addClip | Where-Object { $null -ne $_.GetMethodBody() -and (StoresActive $_) })
$controllerStores = @($addControllers | Where-Object { StoresActive $_ })
# Each overload either stores the composition itself or delegates to the overload that does.
function ReachesStore([Reflection.MethodBase]$Method, $Stores) { (StoresActive $Method) -or @($Stores | Where-Object { CallsResolved $Method $_ }).Count -gt 0 }
$clipsCalled = @($addClip | Where-Object { CallsResolved $humanClips $_ })
$locoStart = (GameType 'Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionLocoMotion').GetMethod('OnStart', $declared)
$startController = @($handleType.GetMethods($declared) | Where-Object { $_.Name -ceq 'StartController' }) | Select-Object -First 1
Check ((CallsResolved $humanStart $humanClips) -and $clipsCalled.Count -gt 0 -and @($clipsCalled | Where-Object { -not (ReachesStore $_ $clipAdds) }).Count -eq 0 -and (IlTokens $humanClips).ContainsKey($human.GetNestedType('ActionData', $any).GetField('IsIdle', $any).MetadataToken) -and
    $controllerStores.Count -gt 0 -and (CallsResolved $locoStart $startController) -and @($addControllers | Where-Object { (CallsResolved $startController $_) -and (ReachesStore $_ $controllerStores) }).Count -gt 0) 'MMR-01 (PR-3 R1-F1): installed UnitAnimationActionLocoMotionHuman.OnStart selects its clips (ActionData.IsIdle while stationary) and adds them through AnimationManager.AddAnimationClip; UnitAnimationActionLocoMotion.OnStart reaches AddAnimatorController through handle.StartController; both paths reach the overload that puts the composition in ActiveAnimations and sets handle.ActiveAnimation'
$updateInternal = $animationBase.GetMethod('UpdateInternal', $declared)
Check ((CallsResolved $updateInternal ($animationBase.GetMethod('SetWeight', [Type[]]@([single])))) -and (CallsResolved $updateInternal ($animationBase.GetProperty('TransitionIn', $any).GetGetMethod($true)))) 'MMR-01 (PR-3 R1-F1): installed AnimationBase.UpdateInternal ramps the animation weight in over TransitionIn (weight 0 contributes nothing)'
Check ((CallsResolved $rest $typeGetter) -and (CallsResolved $rest ($handleType.GetProperty('ActiveAnimation').GetGetMethod())) -and (CallsResolved $rest ($animationBase.GetMethod('GetWeight', [Type[]]@())))) 'MMR-01 (PR-3 R1-F1): the adapter''s rest eligibility counts only the unreleased locomotion handle''s own current animation (UnitAnimationAction.Type, handle.ActiveAnimation) with weight above zero'
# ---- PR-3 review R1-F1 (round 4): the stationary humanoid idle's registration and weight, by operand ---------------------
# Annotated trace: docs/missions/mmr-split/evidence/PR-3-idle-weight-trace.md. Each check below matches an exact IL sequence
# in one installed method (bytes, or a token resolved to the named member); the assembly identity binds them to 2.1.7b.
$gameAssemblyPath = Join-Path $managed 'Assembly-CSharp.dll'
Check ((Get-FileHash -Algorithm SHA256 -LiteralPath $gameAssemblyPath).Hash -ceq '3B6450FFEC440E296E586F71C711B195AED144B28D53E1CBB29406D18FEF5AFB' -and $game.ManifestModule.ModuleVersionId.ToString() -ceq '07fa1e4d-8618-41b3-9b8d-faa17d3b26f7') 'MMR-01 (PR-3 R1-F1): the idle-weight trace is bound to the installed 2.1.7b Assembly-CSharp (sha256 3B6450FF...EF5AFB, MVID 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7)'
function MemberName([Reflection.MemberInfo]$M) { if ($M -is [Type]) { return $M.Name }; $n = $M.DeclaringType.Name + '::' + $M.Name; if ($M -is [Reflection.MethodBase]) { $n += '(' + (($M.GetParameters() | ForEach-Object { $_.ParameterType.Name }) -join ',') + ')' }; $n }
# True when the method's IL contains the items contiguously: an [int] is one byte; a [string] is a 4-byte token that resolves to
# that member (MemberName form, prefix match allowed with a trailing '*').
function HasSequence([Reflection.MethodBase]$Method, [object[]]$Items) {
    $il = $Method.GetMethodBody().GetILAsByteArray()
    for ($start = 0; $start -lt $il.Length; $start++) {
        $p = $start; $ok = $true
        foreach ($item in $Items) {
            if ($item -is [string]) {
                if ($p + 4 -gt $il.Length) { $ok = $false; break }
                $name = $null; try { $name = MemberName ($Method.Module.ResolveMember([BitConverter]::ToInt32($il, $p))) } catch { }
                if ($null -eq $name -or -not ($(if ($item.EndsWith('*')) { $name.StartsWith($item.TrimEnd('*'), [StringComparison]::Ordinal) } else { $name -ceq $item }))) { $ok = $false; break }
                $p += 4
            } else { if ($p -ge $il.Length -or $il[$p] -ne [byte]$item) { $ok = $false; break }; $p++ }
        }
        if ($ok) { return $true }
    }
    $false
}
function Declared([Type]$Type, [string]$Name, [int]$Arity = -1) { @($Type.GetMethods($declared) + $Type.GetConstructors($declared) | Where-Object { $_.Name -ceq $Name -and ($Arity -lt 0 -or $_.GetParameters().Count -eq $Arity) }) | Select-Object -First 1 }
$playableInfo = GameType 'Kingmaker.Visual.Animation.PlayableInfo'
$composition = GameType 'Kingmaker.Visual.Animation.AnimationComposition'
$mixerInfo = GameType 'Kingmaker.Visual.Animation.MixerInfo'
$aacDuration = @($nativeAnimation.GetMethods($declared) | Where-Object { $_.Name -ceq 'AddAnimationClip' -and $_.GetParameters().Count -eq 3 -and $_.GetParameters()[2].ParameterType.Name -ceq 'ClipDurationType' }) | Select-Object -First 1
$aac7 = 'AnimationManager::AddAnimationClip(AnimationActionHandle,AnimationClip,List`1,Boolean,Boolean,ClipDurationType,IAddableAnimation)'
# 1. The stationary branch and its registration call.
Check ((HasSequence $humanClips @(0x6F, 'UnitAnimationManager::get_Speed()', 0x22, 0xCD, 0xCC, 0xCC, 0x3D, 0xFE, 0x04, 0x7D, 'ActionData::IsIdle')) -and
    (HasSequence (Declared $human 'SelectClip') @(0x6F, 'UnitAnimationManager::get_Speed()', 0x22, 0xCD, 0xCC, 0xCC, 0x3D, 0xFE, 0x04, 0x0B)) -and
    (HasSequence (Declared $human 'SelectClip') @(0x07, 0x2C, 0x07, 0x02, 0x7B, 'UnitAnimationActionLocoMotionHuman::NonCombatIdle', 0x2A)) -and
    (HasSequence (Declared $human 'SelectClip') @(0x09, 0x7B, 'WeaponStyleLayer::Idle', 0x2A)) -and
    (HasSequence $humanClips @(0x07, 0x18, 0x6F, 'AnimationManager::AddAnimationClip(AnimationActionHandle,AnimationClip,ClipDurationType)')) -and
    (HasSequence $humanClips @(0x03, 0x73, 'AnimationComposition::.ctor(AnimationActionHandle)', 0x6F, $aac7))) 'MMR-01 (PR-3 R1-F1) trace 1: installed UpdateCurrentClips sets ActionData.IsIdle = Speed < 0.1; SelectClip then returns NonCombatIdle (or the weapon-style Idle in combat); the clip is registered with AddAnimationClip(handle, clip, ClipDurationType), or, with an off-hand clip, with a new AnimationComposition passed to the seven-argument AddAnimationClip'
# 2. The concrete object: the ClipDurationType overload forwards (masks null, default-mask true, addable null), which the seven-argument
# overload turns into one PlayableInfo from the four-argument AddAnimationClip; an AnimationComposition is cast through otherwise.
Check ((HasSequence $aacDuration @(0x02, 0x03, 0x04, 0x14, 0x17, 0x16, 0x05, 0x14, 0x28, $aac7, 0x2A)) -and
    (HasSequence (Declared $nativeAnimation 'AddAnimationClip' 7) @(0x02, 0x03, 0x04, 0x09, 0x0E, 0x05, 0x28, 'AnimationManager::AddAnimationClip(AnimationActionHandle,AnimationClip,AvatarMask,Boolean)', 0x0B)) -and
    (HasSequence (Declared $nativeAnimation 'AddAnimationClip' 7) @(0x11, 0x04, 0x74, 'AnimationBase', 0x0B)) -and
    (HasSequence (Declared $mixerInfo 'AddPlayable') @(0x73, 'PlayableInfo::.ctor(AnimationActionHandle,Playable,MixerInfo,Int32)')) -and
    (HasSequence (Declared $mixerInfo 'AddPlayableFromCache') @(0x6F, 'PlayableInfo::Reset(AnimationActionHandle)'))) 'MMR-01 (PR-3 R1-F1) trace 2: the stationary clip becomes one PlayableInfo (MixerInfo.AddPlayable constructs it, or AddPlayableFromCache Resets a cached one) held in local 1 of the seven-argument AddAnimationClip; with an off-hand clip, local 1 is the AnimationComposition'
# 3. The same object becomes the handle's ActiveAnimation and the ActiveAnimations entry.
$activeBacking = $handleType.GetField('<ActiveAnimation>k__BackingField', $any)
Check ((HasSequence (Declared $nativeAnimation 'AddAnimationClip' 7) @(0x03, 0x07, 0x6F, 'AnimationActionHandle::set_ActiveAnimation(AnimationBase)')) -and
    (HasSequence (Declared $nativeAnimation 'AddAnimationClip' 7) @(0x02, 0x7B, 'AnimationManager::m_ActiveAnimations', 0x03, 0x6F, 'AnimationActionHandle::get_ActiveAnimation()', 0x6F, 'List`1::Add*')) -and
    (HasSequence (Declared $handleType 'get_ActiveAnimation') @(0x02, 0x7B, 'AnimationActionHandle::<ActiveAnimation>k__BackingField', 0x2A)) -and
    (HasSequence (Declared $handleType 'set_ActiveAnimation') @(0x02, 0x03, 0x7D, 'AnimationActionHandle::<ActiveAnimation>k__BackingField', 0x2A)) -and $null -ne $activeBacking) 'MMR-01 (PR-3 R1-F1) trace 3: the seven-argument AddAnimationClip stores local 1 with handle.set_ActiveAnimation and then adds handle.get_ActiveAnimation (a plain backing-field read of that value) to m_ActiveAnimations: one object in both places'
# 4. The concrete GetWeight.
Check ((HasSequence (Declared $playableInfo 'GetWeight') @(0x02, 0x7B, 'PlayableInfo::m_Weight', 0x2A)) -and (HasSequence (Declared $playableInfo 'SetWeight') @(0x02, 0x03, 0x7D, 'PlayableInfo::m_Weight', 0x2A)) -and
    (HasSequence (Declared $composition 'GetWeight') @(0x7B, 'AnimationComposition::m_PlayableInfos', 0x16, 0x6F, 'List`1::get_Item*', 0x6F, 'AnimationBase::GetWeight()', 0x2A)) -and
    (HasSequence (Declared $composition 'SetWeight') @(0x6F, 'List`1::get_Item*', 0x03, 0x6F, 'AnimationBase::SetWeight(Single)'))) 'MMR-01 (PR-3 R1-F1) trace 4: PlayableInfo.GetWeight returns m_Weight (SetWeight writes only m_Weight); AnimationComposition.GetWeight returns its first PlayableInfo''s weight and SetWeight writes every PlayableInfo'
# 5. Initialization, transition writes, and the weight's relation to the graph contribution.
$multiplierCallers = @()
$setMultiplier = Declared $playableInfo 'SetWeightMultiplier'
foreach ($t in $game.GetTypes()) { foreach ($m in @($t.GetMethods($declared)) + @($t.GetConstructors($declared))) { $b = $null; try { $b = $m.GetMethodBody() } catch { }; if ($null -eq $b) { continue }; $il = $b.GetILAsByteArray()
    for ($i = 0; $i -le $il.Length - 5; $i++) { if (($il[$i] -eq 0x28 -or $il[$i] -eq 0x6F) -and [BitConverter]::ToInt32($il, $i + 1) -eq $setMultiplier.MetadataToken) { $multiplierCallers += ($t.Name + '::' + $m.Name) } } } }
Check ((HasSequence (Declared $playableInfo 'Reset') @(0x02, 0x22, 0x00, 0x00, 0x80, 0x3F, 0x7D, 'PlayableInfo::m_WeightMultiplier', 0x02, 0x22, 0x00, 0x00, 0x00, 0x00, 0x7D, 'PlayableInfo::m_Weight')) -and
    (HasSequence (Declared $playableInfo '.ctor') @(0x02, 0x03, 0x28, 'PlayableInfo::Reset(AnimationActionHandle)')) -and $multiplierCallers.Count -eq 0 -and
    (HasSequence (Declared $playableInfo 'GetAdjustedWeight') @(0x02, 0x7B, 'PlayableInfo::m_Weight', 0x02, 0x7B, 'PlayableInfo::m_WeightMultiplier', 0x5A, 0x2A)) -and
    (HasSequence $updateInternal @(0x02, 0x04, 0x07, 0x5A, 0x6F, 'AnimationBase::SetWeight(Single)')) -and
    (HasSequence $updateInternal @(0x02, 0x17, 0x28, 'AnimationBase::set_State(AnimationState)', 0x02, 0x04, 0x6F, 'AnimationBase::SetWeight(Single)')) -and
    (HasSequence (Declared $mixerInfo 'NormalizeWeights') @(0x6F, 'PlayableInfo::GetAdjustedWeight()', 0x13, 0x05, 0x11, 0x05, 0x22, 0x00, 0x00, 0x00, 0x00, 0x36)) -and
    (HasSequence (Declared $mixerInfo 'NormalizeWeights') @(0x7B, 'PlayableInfo::InputIndex', 0x11, 0x05, 0x28, 'PlayableExtensions::SetInputWeight*'))) ('MMR-01 (PR-3 R1-F1) trace 5: PlayableInfo.Reset (from the constructor and cache reuse) writes m_WeightMultiplier = 1 and m_Weight = 0; no installed method calls SetWeightMultiplier (' + $multiplierCallers.Count + ' callers); UpdateInternal writes SetWeight(w * t / TransitionIn) while transitioning in and SetWeight(w) when Playing; MixerInfo.NormalizeWeights sets the input weight from GetAdjustedWeight = m_Weight * m_WeightMultiplier, positive only when it is above zero: GetWeight() > 0 is exactly a positive mixer contribution of the registered clip')
Write-Host ('Mounted alpha 2 native contract (seat) PASS=' + $script:passed + ' FAIL=0 nativeProof=false')
