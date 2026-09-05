# Phase 3F playable core

Current preparation checkpoint (21:19Z): preview.4 passes source `22/0`, Release, components `335/0`, visual/source `23/0`, inventory hashing `10/0`, harness `243/0`, assembly `402/0`, resource/guard audit `9/0`. Preview.3's first live run was stopped through the guard after discovering its inherited RT suite contained a shared-TB tail. It had reached that tail; it is FAIL/aborted with no atomic game result or gameplay PASS. Complete external restoration was independently verified before evidence inspection. Preview.4 removes that tail from C0 RT execution and introduces strict schema-7 RT/presentation evidence; historical schemas retain their original shared-turn and icon requirements. Current candidate packaging/new suite and final runtime runs are pending. The earlier preview.2 identity/ledger below remains historical and must not be used as the current installation instruction.

Resumed runtime checkpoint (2026-09-05): preview.3 is being prepared from parent `eb6b4ced307f48cddd09b02bcc7487ab12a4dce3`. It additionally preserves default native opportunities, honors overlay-off in the four exact guarded scenarios, and records bounded render-frame motion evidence. Ten real-filesystem regressions verify a full-byte streaming inventory optimization; the historical 125,816-file scan now takes 144.39929 seconds without reducing integrity coverage. Complete gate passes source `22/0`, Release, components `335/0`, visual `21/0`, hashing `10/0`, harness `243/0`, assembly `402/0`, resource audit `9/0`; final overlay-focused rebuild/components/visual pass `335/0` and `22/0`. Clean package/new suite and live evidence are pending. Steam is now verified. The preview.2 ledger below is retained as historical build-specific evidence until current runtime closure; it grants no preview.3 gameplay credit. TB resources and native pointer/human motion/UI acceptance remain blockers.

Disposition: **Partial candidate with TB resource and runtime/native-pointer/visual blockers.** Version `0.1.0-phase3f-preview.2`, branch `codex/mounted-combat-phase3f-playable-core`. Implementation, offline checks and private packaging are complete. Gameplay qualification is NOT RUN. One active mounted pair; separate native turns remain an intermediate constraint. This is not a finished Wrath-equivalent mod.

## Exact candidate identity

The gameplay implementation is commit `71bb139ba2547cec0d42690d7f09af6a5e26a218`. Final binary source is its tested descendant `a4f54bf959d32caa871baeba3117b081362552ed`, which updates the version and stale icon diagnostic. Later documentation commits do not change that binary source.

| Identity | Value |
|---|---|
| Package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3f-preview.2-playable-core-private-preview-diagnostic.zip` |
| ZIP SHA-256 | `57fe254d5d39773750cba22ef47380d36d69fc5e92c6be5f759ae4d7ab6e2aa2` |
| Sidecar manifest SHA-256 | `751b72b3b52424c492b698128a0adc030c29ae2a08cdbe3b4ef28eff5500b201` |
| DLL SHA-256 | `617eba209f3adf459a796d6d5f84ea96648491b1c178e7955d761409e50d6275` |
| DLL MVID | `d3acb334-ba00-4767-a3a0-f923dbf4c963` |
| DLL bytes | `4180992`, below the unchanged 4 MiB limit |
| Configuration C0 | `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false` |
| Installed candidate verification | NOT RUN; candidate has never been installed or loaded by this mission |

The clean-source manifest is qualification-eligible packaging metadata, not a gameplay PASS. ZIP contains only KMC `Info.json` and KMC DLL; no proprietary DLLs/assets, saves, credentials or raw logs. The old fallback ZIP remains unchanged at `9451787c08d39ec2164d75f1c36fb4d54245e4228ff12855950fc26798be6698`. Preview.1 also remains immutable and was never loaded.

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

Every **F2** row binds source `a4f54bf959d32caa871baeba3117b081362552ed`, the exact preview.2 artifact above, and configuration **C0**. Evidence root **E** is `C:\Dev\KingmakerMountedCombatLab\analysis-cache\phase3f-intake-20260905T1341077001555Z`. No historical gameplay credit transfers. A missing runtime path means no scenario ran, not missing successful evidence.

| Acceptance / expected behavior | Build | Scenario or evidence | Observed outcome | Status |
|---|---|---|---|---|
| Fallback-default RT admission, both weapon modes | F2 | E/preview2-release-gate.txt; original red in E/red-components.txt | Repaired production admission policy passes deterministic regressions | PASS component only |
| Adjacent/distant ordinary melee; native attack after mount approach | F2 | No runtime scenario | Pointer bridge unavailable; no game launched | NOT RUN |
| Persistent melee/ranged; surviving target and native cooldown waits | F2 | E/preview2-release-gate.txt (policy only) | Cooldown persistence covered offline; real repetition unobserved | NOT RUN gameplay |
| Ranged range/LoS; no forced Bite approach; ammo/reload/misses | F2 | No runtime scenario | No current native weapon/rule/damage evidence | NOT RUN |
| Party selection; one pair intent and isolated ally commands | F2 | E/preview2-release-gate.txt (selection policy) | Reference-exact principal tested; no real group click | NOT RUN gameplay |
| Stop/Hold/ground/retarget/Primary precedence; no stale dispatch | F2 | E/preview2-release-gate.txt (intent generations) | Deterministic cancellation passes; native pending effects/AoOs unobserved | NOT RUN gameplay |
| Precombat and paused hostile initiation | F2 | E/preview2-release-gate.txt (combat-boundary policy) | Initial precombat state distinguished from combat end; timing race not runtime-qualified | NOT RUN gameplay |
| Separate TB rider/mount input; only current actor attacks | F2 | E/preview2-release-gate.txt (actor policy) | Off-turn dispatch excluded; native selection/turn input unobserved | NOT RUN gameplay |
| Movement epochs, both orders/two rounds; one legal mount budget | F2 | E/preview2-native-resource-guard-audit.txt; resource contract | Reconciliation absent; exact native reset and current-actor projection counterexamples; no measured runtime exploit trace | FAIL contract |
| Combat Mount/Dismount, five-foot step, mode/remount costs | F2 | No runtime scenario | Both-order/epoch/resource-laundering regressions remain outstanding | NOT RUN |
| Expected target invalidation; actual native terminal result or clean cancellation | F2 | E/preview2-release-gate.txt (termination policy) | Released/terminal/dead/despawn/hostility cases pass offline; live effects unobserved | NOT RUN gameplay |
| Animated idle/gait/turn/stop/Bite/rider attacks and return to idle | F2 | No motion evidence | Cached projection/cleanup tested offline; HUMAN VALIDATION PENDING | NOT RUN |
| Indoor/outdoor front/side/rear; motion sequence and seat-relative error | F2 | No frame sequence or clip | Numeric post-write residual is not motion proof | NOT RUN |
| Native icons, bindings and normal/hover/disabled/cooldown readability | F2 | Original embedded assets; corrected native sprite assertion | Distinct art built; actual action-bar states unobserved | NOT RUN |
| Mammoth regression and unmounted negative control | F2 | E/preview2-release-gate.txt (profile coverage) | Mammoth values retained; neither native control ran | NOT RUN gameplay |
| Lifecycle/save/load/view replacement; transient cleanup and persistent Horse preserved | F2 | E/preview2-release-gate.txt (existing domain/lease tests) | Offline cleanup passes; affected native boundaries unobserved | NOT RUN gameplay |

## Build, runtime and restoration

The final applicable gate in E/preview2-release-gate.txt passes source `22/0`, Release build `0 warnings / 0 errors`, components `333/0`, visual/source contracts `19/0`, harness/protocol `243/0`, assembly-backed contracts `402/0` (`378` Kingmaker and `24` Wrath). Additional exact native resource/guard audit is `9/0`; clean package validation is `10/0`. The complete gate includes real filesystem transaction tests proving exact fallback/cache/foreign-settings restoration and rejection of mutated incumbent bytes. These are local harness tests, not live game transactions. Earlier size/stale-assertion failures remain preserved; thresholds were not lowered.

Computer-use initialization was attempted correctly; two `sky.list_apps()` attempts reported native pipe unavailable (os error 2). The read-only Steam safety check also stopped: `Exactly one already-running Steam client is required`. Steam was not running. The user was asked to start it in Offline Mode; no such state was observed before closure. No credential, update or account action was attempted. Direct ClickUnitHandler invocation, if later run, must be labeled integration evidence.

The named deployment guard gained an exact Phase 3F branch allowance and a bounded RuntimeTest route through the existing harness. An initial automated-call/manual-pin mismatch was rejected before mutation and corrected to use immutable suite pins. The subsequent runtime WhatIf inventory pass was stopped after the Steam prerequisite failed; **runtime WhatIf is NOT RUN to completion**, not PASS. Its failed/stopped traces are E/rt-A-whatif.txt and E/rt-A-whatif-repair.txt. The short candidate installation `Replace -WhatIf` does pass in E/preview2-install-whatif.txt; this authorizes no permanent installation and is distinct from runtime purity.

No game launched: **0/8 live transactions**, neither final fresh-process run executed. Starting installed fallback includes its exact DLL and UMM cache, KMC inventory digest `22b580f2482c8111c1110979bfc6748b6f3ef8004bfcfd57922eeed462193687`. Independent post-stop audit in E/phase3f-preview1-readonly-preflight-aborted-independent-restoration.json proves exact saves/Mods and no game/lock/sentinel/non-restored transaction. Initial save/Mods digests are `b42a7a30ed0b9e8b75ddb2c7f2511b5b488f35ef914aa0319897cd336388e19d` / `67601f2186e5e4ffc81cff0abc4ee376322e2e269ccb3a0daa3d91c571ef950c`; final audit E/phase3f-preview2-final-audit.json binds those inventories to the final artifact. The original fallback remains installed; no candidate payload was left behind.

Live continuation requires an existing verified Offline Steam session and a fresh suite for the final source/package. Preview.1 suite1 is not valid for preview.2. The current runtime harness requires its package's exact source HEAD; the deployment helper's documentation-descendant allowance applies to installation and does not override that runtime check. The legacy manual-review scenario establishes a Mammoth/overlay/read-only-save state and cannot qualify the requested Horse/native-controls/save-write checklist. Prepare the proper guarded human fixture before using that checklist; do not bypass these checks.

## Remaining path to the finished experience

1. Close Phase 3F pointer, native UI, animation and lifecycle acceptance on the exact private build; fix any attributable defects within a separately explicit remaining budget. Finish separate-turn resource reconciliation before calling TB playable.
2. Design a genuinely pair-aware native turn lifecycle, command ownership and resource epoch around the documented K9 selector boundary. Preserve the useful command-eligibility evidence; do not retry a narrow scheduler shell as a substitute for that architecture.
3. Qualify remaining combat-rule/UX/lifecycle gaps and prioritize full attacks, reach/AoOs, mounted spell/item actions, charge/feats and multiple mounted party members explicitly. One active pair remains the current boundary.
4. Add Paladin Divine Steed through the existing companion architecture under a new mission. Broader mount support and relationship persistence require explicit prioritization and qualification. Persistent Horse companion content already exists separately from transient seating; removing its mod-defined blueprints is not automatically uninstall-safe.
