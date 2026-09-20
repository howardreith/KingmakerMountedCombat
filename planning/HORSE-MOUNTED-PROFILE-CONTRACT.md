# Horse-mounted profile contract

Status: PASS (technical) - mandatory human visual/gameplay review remains.

Final superseding checkpoint (2026-08-27T03:40:27Z): exact dev.17 aggregate `20260827T014000Z-horse-mounted-dev17-passF` passes all reached automated horse-alpha rows, `51/0` including registration. It proves target-selected Mount, independent `medium-humanoid-horse-v1` selection, horse-only movement/path authority, mounted RT/TB routing and transitions, exact Rider primary, exact Horse Bite primary, explicit dismount, cleanup, and non-horse isolation. Horse primary uses exact Bite `35dfad6517f401145af54111be04d6cf` at `AdditionalLimb[0]`, with horse actor/command/resource ownership, terminal Success, one child, one attack/roll/damage, and zero repaths. Same-package targeted Mammoth regression `20260827T030300Z-mammoth-primary-dev17-passA` passes `62/0` and proves the accepted Mammoth primary-hand path is unchanged. Doorway/pointer feel, seat and gait presentation, menus/Wild Shape appearance, actual save/reload, and ordinary gameplay flow remain mandatory human rows rather than inferred automation claims.

Superseding checkpoint (2026-08-27T00:31:21Z): dev.16 independently audited aggregate `20260826T230500Z-horse-mounted-dev16-passE` reached `30/1` horse rows after registration `13/0`. It proves the independent profile and mounted movement/control chain plus exact Rider primary. The sole remaining failure is not pose or routing: the already-qualified no-hands horse stores Bite at additional-limb index `0`, while inherited controller/command checks required a Mammoth-style primary hand. Dev.17 admits only that exact first-limb natural attack (or the unchanged exact primary hand), keeps click-to-child slot/weapon reference identity, and rejects every broader category. One clean audited aggregate is required; visual feel remains human-gated.

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
