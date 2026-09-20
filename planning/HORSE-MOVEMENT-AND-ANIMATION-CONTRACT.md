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

## Phase 3D final seat-lowering cycle - 2026-08-30

The Phase 3C human screenshot supersedes the earlier visual disposition: Candidate C is mechanically stable but the pelvis remains visibly suspended above the stock saddle. Phase 3D therefore authorizes exactly one final Horse-only vertical cycle. The coordinate seam is the rider `Pelvis.localPosition` inside the rider root leased at the native Horse `Chest`; the exact `L_Stirrup` and `R_Stirrup` remain authored children of that Chest at approximately `(+/-0.305183,-0.12273,-0.04402)`. This is not a blind mount-root translation.

The bounded candidates are:

| Candidate | Pelvis local offset | Delta from Phase 3C | Disposition |
|---|---:|---:|---|
| Phase3D-A | `(0,-0.25,-0.02)` | `-0.08 Y` | evidence boundary |
| Phase3D-B | `(0,-0.27,-0.02)` | `-0.10 Y` | intermediate |
| Phase3D-C | `(0,-0.29,-0.02)` | `-0.12 Y` | selected for runtime/manual review |

Phase3D-C is selected because the authoritative screenshot shows material suspension and the user explicitly prioritizes seat contact over a small amount of clipping. The procedural pose applies the pelvis translation before resolving both thigh-root-relative leg chains, so the rider body and existing stable Horse-only foot/knee targets descend together. Lateral, longitudinal, bend, rotation, Horse scale, source anchor, and native animation values remain unchanged.

The accepted Mammoth constants remain exactly pelvis `(0,0.04,-0.05)`, feet `(+/-0.32,-0.50,0.10)`, and knees `(+/-0.42,-0.08,0.42)`; component tests lock those values. Runtime idle/walk/run/turn/stop/reverse, clipping, jitter, stirrup distance, and Mammoth behavior still require fresh evidence. No additional pose candidate cycle is authorized after this one.

## Phase 3D runtime coordinate correction - 2026-08-31

The first exact dev.2 runtime invalidated the assumption behind the table above without weakening its acceptance limit. Applying the selected `-0.12` through `Pelvis.localPosition` did not produce a stable vertical world/root translation: the animated parent-bone basis projected it into a diagonal displacement of about `0.29` world units. The persisted observation measured left/right foot-to-stirrup distances `0.491643876` / `0.5325693`; the right side therefore failed the existing `0.5` gate. Solver health was not the cause: clamps remained zero and maximum foot/knee/segment residuals were below `0.000004`, `0.000004`, and `0.000003`.

The single attributable repair keeps the accepted Phase 3C procedural pose byte-for-value at pelvis `(0,-0.17,-0.02)`, feet `(+/-0.15,-0.62,0.11)`, and knees `(+/-0.16,-0.16,0.16)`. It applies the conservative requested lowering as an exact Horse-only `(0,-0.08,0)` translation in the mount root after resolving the animated Chest point. This coordinate is stable under pelvis animation and lowers the complete rider rather than narrowing the legs. Mammoth receives exact mount-root offset `(0,0,0)` and retains every prior pose constant.

This correction consumes the final Horse calibration repair. Fresh runtime must pass the unchanged mechanical gate; human review still decides saddle contact, clipping, and ordinary-camera appearance. A further failed pose result is an evidence disposition, not authorization for another tuning loop.
