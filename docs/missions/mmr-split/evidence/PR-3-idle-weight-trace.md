# PR-3 R1-F1: the stationary humanoid idle's registration and weight (installed Kingmaker 2.1.7b)

Purpose: establish, from the installed assembly, what `MountedRiderPoseAdapter.SeatRestEligible` reads when it requires a
positively weighted current locomotion composition. Owner direction: `docs/missions/mmr-split/owner-authorization-2026-11-26b.md`
section 4. Evidence kind: native-binding (installed IL read offline). It is not live gameplay; the timing of the first
contributing frame in a running game is not measured, and the rendered seat remains an owner observation.

Assembly: `Kingmaker_Data/Managed/Assembly-CSharp.dll`, sha256 `3B6450FFEC440E296E586F71C711B195AED144B28D53E1CBB29406D18FEF5AFB`,
MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. Each step below is pinned by an exact operand-sequence check in
`scripts/Test-MmrHorseSeatNativeContract.ps1` (labels `PR-3 R1-F1 trace 1` to `trace 5`, plus the assembly identity check), so
a different build fails the contract instead of silently reusing this trace.

Types are in `Kingmaker.Visual.Animation` unless qualified. IL offsets are from the method body.

## 1. The branch selecting the stationary clip, and its registration call

`Kingmaker.Actions.UnitAnimationActionLocoMotionHuman::UpdateCurrentClips(UnitAnimationActionHandle)` 0x0600167B (OnStart calls it):

- IL_0046..0052: `callvirt UnitAnimationManager::get_Speed; ldc.r4 0.1; clt; stfld ActionData::IsIdle`. A stationary rider
  (Speed 0) has `IsIdle = true`.
- IL_006B / IL_0074: `SelectClip(handle, false)` into local 1 (main hand), `SelectClip(handle, true)` into local 2 (off hand).
- `SelectClip` 0x0600167D, IL_001F..002B: the same `Speed < 0.1` into its local 1; IL_0118..0121: out of combat,
  `ldloc.1; brfalse.s; ldarg.0; ldfld NonCombatIdle; ret`; IL_0107..010D: in combat, the weapon-style layer's `Idle`.
- No off-hand clip (IL_0080 `brfalse.s IL_00AF`): IL_00B6..00B8 `ldloc.1; ldc.i4.2; callvirt
  AnimationManager::AddAnimationClip(AnimationActionHandle, AnimationClip, ClipDurationType)` 0x06001529.
- Off-hand clip present: IL_008E..0094 `ldarg.1; newobj AnimationComposition::.ctor(AnimationActionHandle); callvirt
  AddAnimationClip(handle, clip, List<AvatarMask>, bool, bool, ClipDurationType, IAddableAnimation)` 0x0600152A, then
  AddClipToComposition for the off-hand clip.

## 2. The concrete object created

- 0x06001529 is `ldarg.0; ldarg.1; ldarg.2; ldnull; ldc.i4.1; ldc.i4.0; ldarg.3; ldnull; call 0x0600152A; ret`: masks null,
  default-mask true, addable null.
- In 0x0600152A, local 0 = mask count (0) + 1 for the default mask = 1; with addable null, IL_008A..0095
  `ldarg.0; ldarg.1; ldarg.2; ldloc.3; ldarg.s 5; call AddAnimationClip(AnimationActionHandle, AnimationClip, AvatarMask, bool)
  0x0600152C; stloc.1`. 0x0600152C obtains the `PlayableInfo` from `MixerInfo::AddPlayableFromCache` 0x0600157A (which calls
  `PlayableInfo::Reset`) or `MixerInfo::AddPlayable` 0x0600157B (which constructs `PlayableInfo::.ctor(handle, playable, mixer,
  inputIndex)` 0x0600157E). So without an off-hand clip the registered object is one `PlayableInfo`.
- With an addable (the off-hand case), IL_0098..00A4 keeps the supplied `AnimationComposition` in local 4, adds the main-hand
  PlayableInfo to it, and IL_0100..0107 `ldloc.s 4; castclass AnimationBase; stloc.1`.

## 3. One object in ActiveAnimations and in the handle's ActiveAnimation

In 0x0600152A, after local 1 is set:

- IL_0108..0114: the handle's previous ActiveAnimation (if any) gets `StartTransitionOut()`.
- IL_0119..011B: `ldarg.1; ldloc.1; callvirt AnimationActionHandle::set_ActiveAnimation(AnimationBase)`.
- IL_0120..0144: TransitionIn and TransitionOut are set on `handle.ActiveAnimation`.
- IL_01A8..01B4: `ldarg.0; ldfld m_ActiveAnimations; ldarg.1; callvirt get_ActiveAnimation; callvirt List<AnimationBase>::Add`.
- `get_ActiveAnimation` 0x060016CD is `ldarg.0; ldfld <ActiveAnimation>k__BackingField; ret`, and `set_ActiveAnimation`
  writes that backing field, so the entry added to `m_ActiveAnimations` is the object just stored as the handle's
  ActiveAnimation.

When the clip later changes (another `UpdateCurrentClips`), the old object stays in `m_ActiveAnimations` while it transitions
out, but it is no longer the handle's ActiveAnimation. The adapter's `ReferenceEquals(handle.ActiveAnimation, animation)` counts
only the current one.

## 4. The concrete GetWeight and its value

- `PlayableInfo::GetWeight` 0x06001587: `ldarg.0; ldfld PlayableInfo::m_Weight; ret`. `SetWeight` 0x06001585:
  `ldarg.0; ldarg.1; stfld m_Weight; ret` (only m_Weight).
- `AnimationComposition::GetWeight` 0x06001508: if `m_PlayableInfos.Count > 0`, `m_PlayableInfos[0].GetWeight()` (the main-hand
  PlayableInfo's m_Weight), else 0. `SetWeight` 0x06001506 writes the same weight to every PlayableInfo.

## 5. Weight initialization, transition writes and the graph contribution

- `PlayableInfo::Reset` 0x06001580 (called by the constructor at IL_004B and on cache reuse): `m_WeightMultiplier = 1`
  (IL_007E..0084), `m_Weight = 0` (IL_0089..008F). No method in Assembly-CSharp calls `SetWeightMultiplier` (the contract scans
  every method body), so the multiplier stays 1.
- `AnimationBase::UpdateInternal(dt, w)` 0x060014E9, State TransitioningIn (0): when `TransitionIn > 0` and
  `m_Time < TransitionIn`, IL_0049..005B `SetWeight(w * m_Time / TransitionIn)`; otherwise IL_0098..00A1 `State = Playing` and
  `SetWeight(w)`. In Playing it writes `SetWeight(w)` each update; TransitioningOut fades by `1 - progress`.
- `AnimationManager::UpdateAnimations` 0x0600151E passes `w = 1` for additive animations and for the current action, and
  `w = 1 - currentAction.ActiveAnimation.GetWeight()` for other non-additive animations when a current action is not finished
  and does not set DoNotZeroOtherAnimations (otherwise 1).
- `MixerInfo::NormalizeWeights` 0x0600157D: `GetAdjustedWeight()` 0x06001588 is `m_Weight * m_WeightMultiplier`. Each
  PlayableInfo's mixer input gets `SetInputWeight(InputIndex, a)`, where a is its adjusted weight plus the adjusted weights of
  higher inputs when its own is above zero (IL_00AF..011C, `ble.un.s` at IL_00BF). If the adjusted weights sum above 1, a
  `UnitAnimationActionLocoMotion` PlayableInfo is reduced through SetWeight, so m_Weight still holds the value written.

Result: for the registered stationary clip, `GetWeight() > 0` holds exactly when its m_Weight, and so its mixer input weight,
is positive. From registration (m_Weight 0) it becomes positive at the first UpdateInternal of the transition-in.

## Relation to the adapter's predicate

The trace supports the current code, which is unchanged:

1. Native idle eligibility: `UnitAnimationManager.CanRunIdleAction` (Speed <= 0, every active action the locomotion handle,
   additive or released).
2. A positively weighted current locomotion composition: an animation whose unreleased handle's action is a
   `UnitAnimationAction` of Type LocoMotion (installed: only `UnitAnimationActionLocoMotion` and
   `UnitAnimationActionLocoMotionHuman`), whose `handle.ActiveAnimation` is that animation, and whose GetWeight() is above zero.
3. No outgoing released non-additive contribution: no released non-additive handle's animation with weight above zero.

It does not require the transition-in to have finished. A partly faded-in idle counts. The settled-seat sampler's stability
window (`SettledSeatSampler`: 2 s of evaluated animation, then 0.5 s within 2 mm) is what rejects a pose that is still moving. That
includes the old locomotion composition still fading out after a clip change: it belongs to the unreleased locomotion handle, so
condition 3 does not exclude it.

Harness correspondence (`tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs`, `BaseStage`): Absent is before TickInternal
executes the locomotion handle; ZeroWeight is registration with m_Weight 0; Contributing is a positive m_Weight; Stale is a
replaced composition at weight with the current one at 0. NullAction and NonLocomotion are shapes the predicate must not count.
