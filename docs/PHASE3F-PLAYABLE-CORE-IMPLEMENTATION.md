# Phase 3F playable core

Status: IN PROGRESS — private candidate preparation. Version `0.1.0-phase3f-preview.1`, branch `codex/mounted-combat-phase3f-playable-core`. One active mounted pair. This is not a finished Wrath-equivalent mod.

## Confirmed causes and changes

The supplied human log is locally available with exact SHA-256 `bb4dacc6e7bd85de1c3339b53394090a4f4842392448feb405d98ef5d5a315a0`. The handoff archive and associated screenshots were not found in the lab; their bytes have not been reopened. Native Primary successes in that log remain distinct from ordinary hostile clicks. Human failures of ordinary RT melee/ranged, static/forward Horse seating and low-contrast art supersede current fallback capability claims; historical successes retain their original artifacts/settings.

The fallback's unified-mode guard blocked ordinary RT input. Extracting that exact guard into a production admission policy reproduced two failing component regressions (`322 PASS / 2 FAIL`). The repaired policy admits ordinary RT independently of both experimental settings and separate TB only for the native current actor. Request owner, target, exact stock command type and frame-bounded native event remain mandatory. Stock `CreateAttackCommand` does not itself set CreatedByPlayer; that value is recorded, while the matching native event is the admission authority.

Group selection now admits the exact principal without demanding a singleton. RT mount requests from the same group click are consumed without a second pair dispatch; unrelated owners pass through. An intent generation binds target and exact TB turn. Legal RT cooldown waits preserve repetition; cancellation/retarget invalidates the old generation. Paused input records intent without dispatch. Exact ordinary RT hostile input can initiate combat through native command/rule flow; no combat flag is written. A first precombat frame is distinguished from subsequent combat end. Pointer proof remains a runtime gate.

Separate TB ordinary rider input cannot append an off-turn mount attack. Mount-turn ordinary input has equally narrow native provenance. Explicit native primaries remain; forced mount-turn completion after an attack/cancellation was removed so native remaining actions stay available. Stop/Hold and replacement movement from an unrelated selection no longer cancel this pair. Expected target invalidation retires repetition, then lets an already released native child determine its actual terminal result. Death/unconsciousness before release, despawn and changed hostility cancel cleanly. Pair invariant failures and arbitrary exceptions still fail. No projectiles, effects or unrelated native AoOs are deleted.

Horse visual seating now projects the posed pelvis from cached animated Chest position after native animation. Mechanics stay in the existing static mount-root frame; the seven-bone lease restores before animation and at teardown. Chest rotation is never inherited. A 0.18 backward visual correction addresses the reported forward seat; its calibration is provisional. Mammoth profile values and static attachment remain unchanged. See the [position/resource contract](../planning/PHASE3F-PLAYABLE-CORE-CONTRACT.md).

The original saddle icon was revised using image generation from the project's own art: ivory saddle against blue-black, cyan up arrow for Mount, gold down arrow for Dismount. Embedded 96px PNG SHA-256: Mount `cb5c699f38818a4dff07056a98a68c03d9fceeb91a41027febb0f752b4f3f3de`; Dismount `3f945bc01e0816448f42f8a8960cd1d308d425638c2ec4021efcc7e4afc0fa23`. Source generations remain local under `codex-home/generated_images/01a071cb-5f6c-7570-90f9-b50f2d9feffc`; only original derived small control assets are embedded. The first 128px variant exceeded the unchanged 4 MiB DLL package limit; downscaling preserves that gate. Native action-bar states still require inspection.

## Resource blocker

TB movement is UNQUALIFIED. Exact assembly-backed audit confirms native Prepare clears actor cooldowns and TickMovement changes TimeMoved/five-foot state beyond the projected Move field. The inherited projection also uses rider Standard/restriction readiness for mount movement. A mount with Standard spent can therefore be evaluated against the wrong actor's remaining capacity; rider-before-mount movement is exposed to the mount's later native reset. Reverse order and subsequent rounds require a verified epoch across native prepare/delay/surprise boundaries. No guessed round counter, arbitrary rider tax, early refresh or turn-controller replacement was added. Both-turn-order/two-round resource scenarios remain outstanding; this is a blocker, not a gameplay PASS.

## Qualification ledger

All Phase 3F gameplay rows require the final clean-source package and settings `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`. Source/package identities and runtime paths will be bound at packaging. `NOT RUN` means no Phase 3F gameplay credit; deterministic/source checks are listed separately.

| Acceptance | Expected behavior | Outcome | Evidence |
|---|---|---|---|
| Fallback default admission | RT melee/ranged request admitted without unified mode | PASS component only | intake `red-components.txt`, `core-components3.txt` |
| Adjacent/distant ordinary melee | Real pointer, mount approach, actor-native attack | NOT RUN | Pointer helper unavailable |
| Persistent melee/ranged | One click, repeated legal actor cooldowns | NOT RUN | New deterministic policy coverage only |
| Ranged position/LoS | Genuine rider range, no forced Bite approach | NOT RUN | Historical results do not transfer |
| Party selection | One pair intent, unrelated commands preserved | NOT RUN | Deterministic selection policy only |
| Stop/ground/retarget/Primary | No canceled generation dispatch; released effects attributed | NOT RUN | Deterministic generation/lifecycle coverage only |
| Precombat/paused initiation | Native combat entry/unpause with retained input | NOT RUN | Deterministic combat boundary only |
| Separate TB rider/mount input | Current actor only; independent Standards | NOT RUN | Deterministic dispatch admission only |
| Movement epochs, both orders/two rounds | One legal mount budget across all control surfaces | FAIL contract; NOT RUN runtime | Exact native reset/projection counterexamples above |
| Mount/Dismount, step/mode/remount costs | No resource laundering or extra grants | NOT RUN | Resource blocker |
| Expected target invalidation | Native terminal result or clean cancellation, no exception storm | NOT RUN | Deterministic termination policy only |
| Animated idle/gait/turn/stop/Bite/attacks | Convincing saddle following and return to idle | NOT RUN | HUMAN VALIDATION PENDING |
| Indoor/outdoor front/side/rear | Useful motion sequence and bounded relative error | NOT RUN | HUMAN VALIDATION PENDING |
| Native icons/states/bindings | Distinct readable normal/hover/disabled/cooldown controls | NOT RUN | Revised assets only |
| Mammoth/unmounted negative control | Existing profile/native behavior retained | NOT RUN | Existing deterministic Mammoth profile tests |
| Lifecycle/save/load/view replacement | Transient leases/facts clear, companion content retained | NOT RUN | Existing offline coverage plus new saddle cleanup |

## Build, runtime and restoration

Focused source/build/component checks currently pass `22/0`, Release and `333/0`. New exact-assembly/resource/guard audit passes `9/0`. Complete release gate, exact package and runtime results are pending. Computer-use initialization was attempted correctly; two `sky.list_apps()` attempts reported native pipe unavailable (os error 2). Direct ClickUnitHandler invocation, if run, is integration evidence only.

Starting installed fallback includes its exact DLL and UMM cache; KMC inventory digest `22b580f2482c8111c1110979bfc6748b6f3ef8004bfcfd57922eeed462193687`. No game has been launched in Phase 3F. Runtime budget: 0/8 transactions, two reserved for final fresh processes. Temporary testing uses the named deployment helper's exact Phase 3F RuntimeTest route to the existing transactional fixture harness; no permanent deployment is authorized.

## Remaining path to the finished experience

1. Close Phase 3F pointer, native UI, animation and lifecycle acceptance on the exact private build; fix any attributable defects within a separately explicit remaining budget. Finish separate-turn resource reconciliation before calling TB playable.
2. Design a genuinely pair-aware native turn lifecycle, command ownership and resource epoch around the documented K9 selector boundary. Preserve the useful command-eligibility evidence; do not retry a narrow scheduler shell as a substitute for that architecture.
3. Qualify remaining combat-rule/UX/lifecycle gaps and prioritize full attacks, reach/AoOs, mounted spell/item actions, charge/feats and multiple mounted party members explicitly. One active pair remains the current boundary.
4. Add Paladin Divine Steed through the existing companion architecture under a new mission. Broader mount support and relationship persistence require explicit prioritization and qualification. Persistent Horse companion content already exists separately from transient seating; removing its mod-defined blueprints is not automatically uninstall-safe.
