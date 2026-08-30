# Horse movement and animation contract

Status: `PASS (bounded technical) - HUMAN MOVEMENT/ANIMATION REVIEW REQUIRED`

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

Superseding exact contract (2026-08-30): installed Kingmaker `AttackHandInfo.GetAnimationType` returns `SpecialAttack` only for a blueprint-marked special weapon, returns main/off-hand attack only for `HandSlot`, and otherwise returns `None`. The exact Horse Bite is `AdditionalLimb[0]`, so the stock plan can legitimately contain no handle. KMC may supply exactly one action only when the live relationship is the exact KMC Horse pair, the active action is `MountPrimaryNatural`, the actor/hand owner is that Horse, the `AttackHandInfo` and weapon are reference-identical to the planned Bite, and the Horse action set contains exactly one native `UnitAnimationActionSpecialAttack`. Supply occurs after stock `UnitAttack.Init` and before start. It may not replace the stock command, change rule timing, synthesize attack events, import an asset, affect Mammoth/non-Horse attacks, or conceal an animation interruption.

Superseding handle disposition (2026-08-30): exact `UnitAttack.Init` ordering calls `CreateAnimationHandleForAttack` before KMC's direct post-init seam. When stock returns a handle, KMC must preserve it and may bind telemetry only after proving the exact Horse animation manager, exact native action-set membership, `UnitAnimationActionSpecialAttack` type, and exact Bite special-animation type. Source is recorded as `stock-created`. KMC may create the single exact native fallback only when stock returned no handle; source is `kmc-supplied`. The postfix/direct pair must be idempotent, and evidence must distinguish create from adoption rather than relabel adoption as creation.

## Pose calibration boundary

Only `medium-humanoid-horse-v1` may change. Candidate B begins at pelvis `(0,-0.12,-0.02)`, feet `(+/-0.18,-0.58,0.11)`, knees `(+/-0.20,-0.14,0.16)`. One further cycle may evaluate no more than three small, interpretable parameter sets against side/front/three-quarter views, stirrup distance, clearance, idle/walk/run/turn/stop stability, drift, and jitter. Mammoth values and native Horse scale/assets remain fixed.

## Final bounded disposition - 2026-08-30

The live Horse blueprint remains 50 feet and the exact agent maximum remains `5.08`. The bounded solo control measured unmounted displacement `1.96599746` in `0.8531311` seconds (`2.30402027` average world units/second) and mounted displacement `1.95371664` in `0.6567715` seconds (`2.97469866` average). Mounted solo behavior was not slower than unmounted Horse behavior in this bound, so no rules-speed or global-party change was justified. Mixed-party formation pacing and long-run stock comparison remain human observations.

Exact Horse Bite `35dfad6517f401145af54111be04d6cf` remains `AdditionalLimb[0]`. The final adapter follows the refreshed stock-created handle, validates exact Horse manager/action-set membership, and records `HorseAnimationSet_Bite` / `SpecialAttack`. TB and RT each produced one successful Horse-owned command and one attack/roll/damage chain. The handle acted, finished, and was not interrupted; create/adopt/reject totals after both actions were `0/2/0`. Visual readability remains a human gate.

Candidate C is final for this bounded cycle: pelvis `(0,-0.17,-0.02)`, feet `(+/-0.15,-0.62,0.11)`, knees `(+/-0.16,-0.16,0.16)`. Left/right stirrup distances are `0.39243558` / `0.4600763`; clamps are zero; maximum foot/knee/segment residuals remain at microunit scale; maximum/average cost is `19.0` / `13.9` microseconds. Mammoth values, Horse scale/assets, and every other rider category are unchanged.
