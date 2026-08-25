# Horse-mounted profile contract

Status: TODO — blocked on unmounted horse qualification

Profile ID: `medium-humanoid-horse-v1`

## Isolation

This profile is independent of `medium-humanoid-mammoth-v1`. It must not reuse Mammoth anchor offsets, pelvis translations/rotations, leg rotations, scale, grounding compensation, footprint constants, or profile-selection shortcuts. A regression test must prove that introducing the horse profile does not change any Mammoth profile value.

The relationship remains transient and nonserialized. The horse remains the sole movement/pathfinding authority; the rider remains the rider-melee and target-interaction owner; the horse retains its independently owned primary natural attack and ledger.

## Evidence inputs

The starting anchor hypothesis is the native horse `Chest` because both authored stirrups are its children. Runtime measurement, not the transform name alone, decides the final seat anchor. Required evidence includes:

- exact Chest and left/right stirrup world transforms at idle, walk, run, turn, stop, and reverse;
- rider pelvis and leg-chain baselines for each admitted Medium humanoid rig;
- horse root scale, view orientation, movement phase, collider/selection geometry, and doorway footprint;
- weapon/shield clearance for a one-handed rider;
- exact cleanup baselines and restoration.

## Eligibility

Admit only one directly controllable Medium humanoid rider and the rider's exact active KMC horse companion. Reject a missing/ambiguous rig, wrong blueprint, non-owner, wrong size, dead/incapacitated unit, missing view/agent, invalid game mode, ongoing shapechange, or existing relationship with deterministic visible feedback.

## Pose ownership

Own only the minimum proven rider transforms. Preserve upper-body, arms, hands, equipment, facial animation, and stock attack animation unless evidence requires a narrower explicit lease. Every modified transform is snapshotted and restored exactly. Horse animation/controller state is observed, not replaced.

## Qualification matrix

Technical rows must cover mount/dismount, idle, walk, run, turn, stop, reverse, open ground, doorway and distant door, selection, portrait/action bar, camera, group movement, menus, Wild Shape cleanup, save/area cleanup, RT movement, TB horse turn movement, TB rider turn routing, Rider primary, Horse primary, RT/TB transitions, cancellation/interruption, death/incapacitation, and non-mounted controls.

Fatal outcomes include duplicate commands/attacks/resources/turns, rider pathfinding, horse movement ownership loss, target or door failure, visible automation-detectable oscillation, relationship residue, Mammoth profile mutation, non-mounted regression, or external restoration failure.

Human review remains mandatory for seat gap, silhouette, pelvis/leg posture, stirrup placement, gait feel, turn/stop/reverse animation, clipping, weapon clearance, selection/camera feel, menus/fog flash, and ordinary physical-pointer usability. Internal fields alone do not prove human usability.

