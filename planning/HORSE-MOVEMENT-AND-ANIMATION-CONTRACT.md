# Horse movement and animation contract

Status: `IN PROGRESS`

Date: 2026-08-28

## Question split

The character sheet's `Speed 50` is not accepted as proof of live displacement or visual locomotion. Qualification measures separately:

1. a stock 30-foot humanoid alone;
2. the real KMC Horse unmounted;
3. the mounted Horse pair alone;
4. the mounted pair in a full-party group command;
5. a stock fast animal companion;
6. native `CR1_HorseRiding` when the guarded fixture can observe it safely;
7. RT and TB distance/resource behavior.

For each row record blueprint feet, movement-agent type, `Speed`, `MaxSpeedOverride`, desired velocity, actual velocity, horizontal displacement, path length, elapsed time, animation locomotion state, animator playback speed, and party formation context.

## Disposition rules

- If solo Horse or mounted-pair displacement is below its 50-foot contract, repair only Horse speed propagation or mounted movement authority.
- If displacement is correct but the view slides or animates slowly, repair the native Horse animation state/playback path without increasing rules speed.
- If only a mixed-party group is capped to its slowest member, preserve and document stock group behavior.
- TB movement must preserve exact Move ownership and distance/resource accounting; no timeout or reach gate is relaxed.

## Horse primary animation

The exact Horse primary remains Bite GUID `35dfad6517f401145af54111be04d6cf` at `AdditionalLimb[0]`. The investigation must correlate the child `UnitAttack`, selected weapon slot, `UnitAnimationActionAttack`, Horse animation manager handle, attack event, facing, rule attack/roll/damage, and terminal command. One plausible native Horse attack animation is required without duplicate attack/damage or an artificial command delay. No Wrath animation may be imported.

## Pose calibration boundary

Only `medium-humanoid-horse-v1` may change. Candidate B begins at pelvis `(0,-0.12,-0.02)`, feet `(+/-0.18,-0.58,0.11)`, knees `(+/-0.20,-0.14,0.16)`. One further cycle may evaluate no more than three small, interpretable parameter sets against side/front/three-quarter views, stirrup distance, clearance, idle/walk/run/turn/stop stability, drift, and jitter. Mammoth values and native Horse scale/assets remain fixed.

