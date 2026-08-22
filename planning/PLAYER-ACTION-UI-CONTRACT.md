# Player action and UI contract

Status: PASS — PRIVATE-ALPHA CORE CONTROLS

Manual review exposed one truthful projection defect: automation mounted the pair without passing through `MountedPlayerActionController.Activate`, so `Dismount` could be rendered beside stale readiness-to-mount feedback. The post-acceptance controller now retains operation feedback only while the complete availability projection is stable and immediately adopts the current evaluator feedback after an external action/state transition. Deterministic regressions cover both behaviors.

## Player intent

Expose one unambiguous action that reads `Mount` while eligible and unmounted and `Dismount` while mounted. The selected rider remains the player-facing principal. A rejected action returns exact ordered reason text and performs no partial mutation. Multiple independently failing requirements may be reported together so the player is not forced through one-error-at-a-time discovery.

## Minimum eligibility

- exact supported Medium rider/body profile;
- exact active owned `AnimalCompanionUnitMammoth` companion;
- current mount size strictly larger than rider;
- distinct, alive, conscious, directly controllable pair with valid views and ordinary agents;
- reciprocal active-companion relationship and same valid area state;
- no conflicting mounted relationship, unsupported polymorph/size state, transition, cutscene, loading, or blocked lifecycle boundary.

## Transition contract

Mount validates before mutation, acquires all owned leases transactionally, normalizes selection to rider, and rolls back on any partial failure. Dismount is idempotent and restores movement, avoidance, view parent/transform, pose, selection, and UI state. Double activation, stale action state, and lifecycle interruption fail closed.

## UI/camera observations

Runtime evidence must directly record selected unit identity, rider/mount click result, portrait highlight, selection-circle owner/position, action-bar owner, action label/availability/reason, camera subject while moving and after selection switches, party group routing, and cursor/ground-command recipient. Screenshots must include actual UI where a UI claim is made.

## Selected transient surface

The private alpha uses an owned bottom-right IMGUI overlay, not a blueprint or hotbar action. It is visible in a loaded area even when disabled so it can explain eligibility, becomes an enabled `Mount` only for the exact validated pair, becomes `Dismount` for active or fault-cleanup state, and is absent outside a loaded game. `MountedPlayerActionEvaluator` owns deterministic eligibility; `MountedPlayerActionController` projects exact Kingmaker state and delegates transitions; the `MonoBehaviour` draws and forwards only.

Offline checkpoint: component action tests `7 PASS / 0 FAIL`; complete component total `126 PASS / 0 FAIL`; harness `138 PASS / 0 FAIL`. The registered UMM-toggle native scenario now checks one owned overlay before disable, zero references/objects on the following disabled frame, and exactly one restored overlay after re-enable; live execution remains required twice.

Runtime controller qualification: exact clean commit `a344442fcf81de6ae49ce5770099d05874995de8`, package SHA-256 `7d2287f785d870c967c8d8ba54a1f458f989a030d2718f194e61808ebcd2ff2f`, DLL SHA-256/MVID `6e4a2d9b75f2e6a485b3f4da0234243b33c797df92191872724b393f912a784d` / `95e1f8fc-5aa3-4338-8921-bf5dff209d76`. Availability passed twice at `29/0`; mount/dismount passed twice at `44/0`. Each run used a fresh process, ended `Unmounted`, retained exact rider selection, produced zero relationship/movement/attachment residue, and restored Working, protected saves, and Mods exactly. These rows invoke `MountedPlayerActionController` directly and deliberately record `nativeDeliveryObserved=false`; actual IMGUI visibility/click delivery and UI capture remain `TODO` for Tranche B.

The accepted Phase 2A manual presentation review and later combat evidence establish the private-alpha overlay boundary. Rider melee and Mammoth primary clicks retain exact actor/target/weapon ownership, accepted rider-facing selection and UI projection, and zero duplicate command or rule chains. Invalid target, target death before child admission, repeated cleanup, cancellation/interruption, combat lifecycle, and non-mounted controls are repeatably qualified. This does not claim a native action-bar button, hotkey, serialized ability, or generalized UI integration; the transient IMGUI overlay remains the only supported player-facing control surface.
# Private-alpha stabilization addendum — 2026-08-21

Human playtest evidence from the exact qualified package, plus bounded inspection of the installed Kingmaker assembly, supersedes the earlier broad game-mode cleanup assumption. `GameModeType.Pause`, `GameModeType.FullScreenUi`, and `GameModeType.EscMode` are non-world interaction/overlay modes used by pause, character/inventory/spellbook/map/menu surfaces. Starting or stopping one of those modes is an observation boundary, not an area-unload boundary: an exact valid mounted pair, rider selection principal, presentation lease, and action ownership remain unchanged, while new mounted movement/combat admission remains blocked until `Default` world interaction resumes. Other exact modes remain fail-closed clean-dismount boundaries unless separately qualified; their feedback must identify a game-mode boundary rather than falsely report `AreaUnloading`.

Installed `Polymorph.TryReplaceView` creates the replacement `UnitEntityView`, parents it to the old view's current parent, copies world position/rotation, and only then calls `AttachToViewOnLoad`. While mounted, the old view's current parent is KMC's owned `KMC_RiderPositionAnchor`. Therefore a rider replacement view can temporarily inherit that owned parent. On exact replacement attachment KMC must first release only that replacement root from the owned anchor to the attachment lease's captured stock parent, preserving its world transform, active state, and stock renderer state; it then restores the old pose/root lease and destroys only its own anchor. A same-view `HandleUnitViewAttached` delivery is observational and must not dismount. KMC never enables renderers globally, keeps an obsolete humanoid view alive, or claims the replacement view.

The stabilization overlay retains the existing two-step ordinary input path: arm `Rider melee`, then use Kingmaker's real `ClickUnitHandler.OnClick` target click. One exact combat-button activation owns a one-shot world-click suppression lease for at most two frames so the IMGUI click cannot leak into the unit/ground handlers beneath the overlay and immediately cancel or dispatch the newly armed action. The lease is consumed by at most one propagated click and never suppresses a later deliberate target click. Every rejected admission or terminal failure must retain a bounded reason for the player and diagnostic evidence. Native stock `UnitAttack` commands from the exact mounted rider are outside this private-alpha surface and are rejected pair-locally; a ranged selected weapon reports `Mounted ranged attacks are not supported in this private alpha.` Unmounted attacks remain stock.

Native-toolbar Mount targeting remains backlog-only. No blueprint, hotbar fact, serialized action, or persistent residue is added by this stabilization tranche.
