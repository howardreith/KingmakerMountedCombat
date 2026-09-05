# Phase 3F playable core

Disposition: **Partial candidate with TB resource, unmounted ranged regression, native pointer and human presentation blockers.** Private version `0.1.0-phase3f-preview.4` contains implemented ordinary-control, animated Horse seating, native opportunity and icon improvements. Six guarded live transactions completed with exact restoration. Both final same-candidate RT suites failed at the same unmounted ranged readiness deadline; that investigation is stopped. This build is available for bounded human review, not accepted ordinary-play qualification or a finished Wrath-equivalent mod.

One active mounted pair is supported. Architecture B remains: the mount AgentASP owns physical movement, rider navigation is disabled by an owned lease, and rider/mount weapon, rule, command and resource identities remain distinct. Separate native TB turns are an intermediate constraint. Both experimental features remain disabled; no turn-controller replacement or further shared-TB qualification is delivered.

## Exact candidate and provenance

Branch: `codex/mounted-combat-phase3f-playable-core`. Clean binary source **F4**: `207ebf249db075cae8f09ebe97f33f16663262bf`, guarded-published. A later documentation-only closure does not change this binary source. Main gameplay changes are in `71bb139ba2547cec0d42690d7f09af6a5e26a218`; resumed native-opportunity/observation/harness changes are in `e06cd3f47510a6ac7149efeff886f1745725d190`; F4 corrects the inherited runtime fixture scope/protocol and version.

| Identity | Exact value |
|---|---|
| Package | `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3f-preview.4-playable-core-private-preview-diagnostic.zip` |
| ZIP SHA-256 | `c557b9b8404f32b5e81c2d5594274a8bee80c3f604a58fdb7ca96e2e74a977dd` |
| Sidecar `.zip.manifest.json` SHA-256 | `805bf4c8d7dc92e0889f598d3caaca90c912a2e0046f2e0500af8c5bc884dc0f` |
| DLL SHA-256 | `0dd6913ae2fb21b94d0681d355fc293747123508d32bebe9145aac153e975c07` |
| DLL MVID | `b3541fbb-9ad7-44d6-b561-1ac341046927` |
| DLL bytes | `4184064`, below the unchanged 4 MiB cap |
| Shipped configuration C0 | `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false` |
| Qualification suite | `runtime-state/qualification-suite-snapshots/20260905-phase3f-preview4-runtime-suite4.json` |
| Suite SHA-256 | `7e1ea410c0434aeecaa5574378df3fdc7196984bca05111a4c83461e9ab9737f` |
| Installed payload during testing | Exact package/DLL/MVID/configuration verified by the guard and result manifests; restored after every transaction |

The ZIP contains only KMC `Info.json` and KMC DLL. It contains no proprietary game/Wrath DLLs, extracted assets, saves, credentials or raw logs. The diagnostic filename does not mean an overlay is required. Runtime automation is separate from ordinary installation.

Intake verified clean local/upstream/remote `db7eebbf27f8a05dc48febb138218f0df8ef859f`, the documentation descendant of `d63d05579cc7dbaf72a270decbad334a8ddb992d`; no reset or inherited-work discard occurred. The supplied human log was reopened locally with exact SHA-256 `bb4dacc6e7bd85de1c3339b53394090a4f4842392448feb405d98ef5d5a315a0`. The handoff archive and supplied screenshots were not found locally; their bytes/manifest were not reopened. Human Primary successes are not ordinary-click evidence. Buff Planner UI exceptions, BugReportCanvas errors and shader warnings remain separate observations, with no demonstrated cause of KMC input failure.

Old fallback ZIP SHA-256 remains `9451787c08d39ec2164d75f1c36fb4d54245e4228ff12855950fc26798be6698`. Earlier previews remain unchanged. Their historical identities/results are retained in the journal and Git history; none grants F4 qualification.

## Confirmed causes and implementation

### Ordinary controls and ownership

The fallback's unified-mode guard rejected ordinary RT requests with both shipped experimental settings false. Extracting the exact condition into a production admission policy reproduced two behavioral failures (`322 PASS / 2 FAIL`). The repair admits RT independently of either experiment and admits separate TB only for the native current pair actor. It retains exact active-pair ownership, stock command type, target and a frame-bounded native request. The native path is `ClickUnitHandler.OnClick -> OnAttackRequested -> stock UnitAttack -> UnitCommands.Run`; `CreatedByPlayer` alone is insufficient because stock creation does not itself set it.

Group selection now checks for the exact principal instead of requiring a singleton. The mount request from the same RT group click cannot create a second pair intent; unrelated party owners pass through. AI/foreign/unobserved requests do not gain ordinary-player provenance. Runtime group-pointer isolation remains untested.

Owned target generations survive legal RT cooldown waits and invalidate on cancellation/retargeting. Paused input retains intent without dispatch. Initial precombat input is distinguished from a later combat-end boundary; no combat flag is written. Ranged approach uses rider weapon range and native admission/LoS, and cannot close just to obtain Horse Bite. Native targeting, cooldowns, attack rules, damage and single-child limits remain authoritative. Stop/Hold or ground movement from an unrelated selection no longer cancels this pair. Explicit Primary use replaces ordinary intent. Full attacks and iterative parity are not implemented or claimed.

Separate TB ordinary rider input cannot append an off-turn Horse attack. Mount-turn ordinary admission is equally actor-local; explicit Mount Primary remains available on its native turn. Forced turn completion after an attack/cancel was removed, leaving remaining native actions to Kingmaker. Native selection/action availability and cross-turn costs still require qualification. Unsupported selected-actor/target input uses the existing native warning-notification event; the runtime friendly-click row only proves absence of fabricated attack intent, not visible warning rendering.

The inherited active-pair opportunity suppression also affected C0. Two new default-configuration regressions failed (`333/2`) before the repair (`335/0`). That suppression now applies only to the disabled unified experiment. Ordinary native rider/mount opportunities pass through in C0; no opportunity budget, projectile or effect is deleted to make cancellation pass. This is not full mounted reach/AoO qualification.

### Expected target invalidation

The old `target-not-finally-dead` predicate represented a target viability requirement; loss of that requirement was routed through `RequireLiveExactPair` as an exception. The new termination policy distinguishes expected death/unconsciousness, missing/despawned view/entity, changed hostility and cancellation from a broken exact-pair invariant. Before release, invalidation cancels cleanly. After release, ordinary repetition ends while the existing native child/effect lifecycle determines its terminal result. Child creation or `childAttacks=1` is not success. Arbitrary exceptions and pair invariant failures still fail; unrelated dismount and resource grants are not manufactured.

Deterministic cases cover these terminal distinctions. Repeated activation log entries alone are not counted as duplicate attacks. A current live death/despawn/hostility matrix with pending projectiles remains NOT RUN; the final unmounted-target failure does not establish that this policy is correct or incorrect.

### Animated Horse seating and original controls

The Horse previously sampled a bone once into a static root-local seat, avoiding the old approximately 161-degree roll while discarding animated displacement. The new owned seven-bone pose lease projects the posed pelvis from a cached animated Chest point after native animation, at execution order 32000. It restores the prior frame's bone baselines before animation and restores them on teardown. Exact transform destruction/reparenting invalidates health; no scene-wide per-frame lookup is added.

Anatomical seating correction, animated displacement and orientation are separate. A Horse-only 0.18 backward root-basis correction is provisional seat calibration. Chest quaternion inheritance is zero; stable mount heading and existing rider pose orientation are retained. There is no uncorrected bone rotation, added rider nav agent, animated gameplay root, scale write, frozen Horse animation or animated reach/LoS change. Mammoth's independent profile/static attachment is unchanged. See [the full position/resource contract](../planning/PHASE3F-PLAYABLE-CORE-CONTRACT.md).

Original saddle art now uses an ivory silhouette on blue-black, with cyan up for Mount and gold down for Dismount. Distinct 96px sprites bind to the original native abilities. Mount PNG SHA-256 is `cb5c699f38818a4dff07056a98a68c03d9fceeb91a41027febb0f752b4f3f3de`; Dismount is `3f945bc01e0816448f42f8a8960cd1d308d425638c2ec4021efcc7e4afc0fa23`. The first 128px variant exceeded the unchanged package cap; downsizing preserved it. No Wrath art or new image dependency was shipped. Normal/hover/disabled/cooldown readability on the brown action bar remains HUMAN VALIDATION PENDING.

### Resource contract remains failed

Physical mounted movement belongs to the mount's native legal budget; rider nonlocomotion actions belong to the rider. Rider and mount Standards are not pooled. Unchanged rider Move alone is not an exploit: the inherited adapter projects mount Move into the rider, invokes native movement accounting, restores rider Move, then transfers the result back.

The bounded exact-assembly audit identified incomplete side effects and native reset semantics. Native `Prepare` clears the current actor's cooldown object. `TickMovement` consults current-rider Standard/restriction/readiness, movement limits, speed and AgentASP state; it changes TimeMoved, five-foot distances/time, immunity/auto-stop state and may issue native Stop. Restoring Move alone does not restore these effects, including on exceptions. Standard-to-movement capacity is implicit in native remaining-time predicates, not a direct Standard write.

Concrete source counterexamples remain: a mount with Standard spent can be evaluated using an unspent rider Standard; movement charged before the mount's native turn can be erased by its later Prepare; reverse order/new round can consult a previous cooldown before native reset. Native roster `StartRound`/`RoundNumber` was inspected, but no lawful epoch across prepare/delay/surprise/mode/remount boundaries was proven. The human log is a concern, not a measured exploit trace.

No guessed once-per-round counter, arbitrary rider Move tax, cooldown refresh or broader turn-controller ownership was added. **TB resource reconciliation is FAIL contract / NOT RUN runtime.** Both orders over two rounds, partial movement across ground/attack/door/mount surfaces, Standard conversion, five-foot-step, Stop/retarget/remount/mode/reselection laundering and combat Mount/Dismount costs remain blockers. Passing RT ledgers does not change this disposition.

## Evidence and acceptance ledger

Every current row below binds **F4** source/package identities above and **C0**. Evidence root **E** is `C:\Dev\KingmakerMountedCombatLab\analysis-cache\phase3f-resume-20260905T2001137617443Z`; **R** is `C:\Dev\KingmakerMountedCombatLab\runtime-evidence`. Runtime aliases resolve as follows:

| Alias | Scenario and exact evidence |
|---|---|
| A | `phase3d-unified-combat-rt-suite`; R/`20260905-phase3f-preview4-rt-final-A/phase3d-horse-scenario-evidence.json`; SHA `87c4a7663ddbad541fc7df30fd927ae338e0f70418bad7b68d86ea8768bf2c62` |
| B | Same C0 RT scenario; R/`20260905-phase3f-preview4-rt-final-B/phase3d-horse-scenario-evidence.json`; SHA `d0b06f9e09a35a4c3213763b504add23e6f8226844abd6867f38cdec754be9da` |
| P | `phase3d-horse-presentation-suite`; R/`20260905-phase3f-preview4-presentation-A/phase3d-horse-scenario-evidence.json`; SHA `8842a64b71babf3ca6e8b6b3e061dcbcedabaacef0ac224a82a607971458342c` |
| M | `mounted-mammoth-primary-hit-rt`; R/`20260905-phase3f-preview4-mammoth-A/combat-scenario-evidence.jsonl`; SHA `1d1bcff2a03f5e2c5c5796bd80fe9e7369bfc5c535ce586b501dbd6836e4bcd1` |
| L | `combat-lifecycle-suite`; R/`20260905-phase3f-preview4-lifecycle-A/lifecycle-scenario-evidence.jsonl`; SHA `b71c65008e325f3a291eb13e8b87d285ed423aafac85602c7fb3549bf7a96057` |

The scenario's historical name does not enable unified mode. Schema 7 requires actual false unified/paired/overlay/overlay-present values and excludes the old shared-TB rows. A/B used scripted native `ClickUnitHandler.OnClick`, selected/equipped fixture actors, controlled targets and diagnostic roll/AI setup. The adjacent AoO control explicitly simulates readiness. These are **integration evidence**, not unassisted pointer/ordinary-play qualification. The recorder itself only observes after rendering. No row below promotes controlled-roll results into random misses, resource epochs or human PASS.

| Acceptance / expected behavior | Build/config | Scenario/evidence | Observed outcome | Status |
|---|---|---|---|---|
| Default RT admission, melee and ranged | F4/C0 | E/preview4-release-gate.txt; A/B admission rows | Production policy green; native-handler requests admitted with both flags false | PASS integration |
| Adjacent and distant melee, mount-owned approach | F4/C0 | A/B melee adjacent/approach rows | Exact native attacks and mount approach observed through scripted input | PASS integration |
| Persistent melee after cooldown, repeated completed rider attacks | F4/C0 | A/B auto-repeat/cancel rows | One request; two rider admissions, one completed non-opportunity rider rule and one Horse rule before deliberate cancellation; no duplicate admission. A second completed rider attack was not established | DEFERRED completed-repetition gate |
| Repeated ranged attacks from one request | F4/C0 | A/B bow auto-fire rows | Two non-opportunity rider attack rules, zero mount dispatches; separate controlled native rolls/damage | PASS integration |
| Genuine ranged range/LoS and no Bite approach | F4/C0 | A/B bow/range/LoS/adjacent rows | Rider range/native admission retained; no forced Horse melee | PASS integration |
| Crossbow and sling native ownership | F4/C0 | A/B variant rows | Native ranged weapon/rule/resource identity observed; core reports no separate ammo/reload state | PASS integration; extra ammo/reload systems NOT RUN |
| Random misses and unmodified full attack/effect lifecycle | F4/C0 | A/B controlled-roll fixtures | Forced d20 evidence cannot qualify random miss distributions or full/iterative attacks | NOT RUN |
| Actual pointer path and party/group selection isolation | F4/C0 | E/preview4-release-gate.txt policies; no desktop run | Exact-principal/deduplication tests pass; approved desktop bridge unavailable | NOT RUN gameplay |
| Ground cancellation; native opportunities remain separate | F4/C0 | A/B melee cancel rows | Intent canceled with no later ordinary rule; independent rider/Horse AoOs separately attributed | PASS integration |
| Ranged explicit cancellation | F4/C0 | A/B bow cancel rows | Intent ends with no later observed rule in the bounded window | PASS integration |
| Stop/Hold/retarget/Primary precedence, pending projectile distinctions | F4/C0 | E/preview4-release-gate.txt intent policies; no complete native matrix | Generation/selection policies pass; all native surfaces/in-flight boundaries unobserved | NOT RUN full gameplay matrix |
| Friendly/invalid input and visible native feedback | F4/C0 | A/B invalid-target row; warning-event adapter | Friendly Horse click creates no hostile intent; row does not inspect rendered warning text | PASS non-hostile integration; UI NOT RUN |
| Paused and out-of-combat hostile initiation | F4/C0 | E/preview4-release-gate.txt boundary policies | Deterministic combat-start/end handling passes; real timing not qualified | NOT RUN gameplay |
| Separate native rider/mount TB actor admission | F4/C0 | E/preview4-release-gate.txt actor/turn policies | Exact native actor/turn binding passes offline; no current TB gameplay qualification | NOT RUN gameplay |
| One legal mount movement budget across actors/rounds | F4/C0 | E/preview4-native-resource-guard-audit.txt; resource contract above | Proven source reset/projection counterexamples; no bounded reconciliation | FAIL contract |
| Both orders/two rounds, steps, conversions and laundering boundaries | F4/C0 | Resource contract; no current runtime scenario | No verified native resource epoch or gameplay trace | NOT RUN |
| Expected death/unconsciousness/despawn/hostility terminal outcome | F4/C0 | E/preview4-release-gate.txt termination cases | Native released-child vs cancellation policy passes offline; full live effects matrix absent | NOT RUN gameplay |
| Horse static pose/ability bindings | F4/C0 | P | Healthy settled pose; distinct native 96px sprites and ability binding checks | PASS integration |
| Animated idle/gait/turn/stop/Bite/rider attacks/return idle | F4/C0 | A/B `movement-visuals` and E frame viewers/CSVs | 64 timestamped frames each; projection follows numerically, but later subjects leave camera and Bite phase is sparse | DEFERRED; HUMAN VALIDATION PENDING |
| Useful indoor/outdoor front/side/rear and calibrated saddle error | F4/C0 | A/B/P captured indoor view only | No complete viewpoints/outdoor sample or anatomical calibration | NOT RUN complete visual gate |
| Native icon normal/hover/disabled/cooldown readability | F4/C0 | P native sprites; screenshots exclude UI chrome | Exact installed art/bindings verified; actual bar states unobserved | NOT RUN visual gate |
| Mammoth natural attack/profile/cleanup regression | F4/C0 | M | 62/0 assertions, exact mount command/rule/resource ownership; forced native critical | PASS integration |
| Unmounted melee negative control | F4/C0 | A/B unmounted-stock-attack-control | Stock unmounted melee completes | PASS integration |
| Unmounted ranged negative control | F4/C0 | A/B leaf deadline and readiness records | Fresh target has damage 20, target/combat-memory readiness false; same 30-second failure twice | FAIL; investigation stopped |
| Combat start/end and death/incapacity/removal/view/exception cleanup | F4/C0 | L | Nine fixture-driven Mammoth rows, 400/0 assertions, no retained lease residue | PASS integration |
| Horse-specific view replacement, save/load/area and persistent companion survival | F4/C0 | E offline cleanup tests; no full native scenario | General cleanup tests do not establish all affected Horse/save boundaries | NOT RUN complete gate |
| Two fresh-process final RT suites | F4/C0 | A/B full runtime-result/game-result | Each 24/2 subscenarios, 68/2 assertions; 23 completed tranche rows, leaf and parent failure rollup | FAIL |

### Motion observations

Each A/B sequence has 64 original PNGs with verified lengths/hashes, timestamps and post-render observations; zero capture failures. Maximum measured current pelvis-to-projected-seat error is `9.5367431640625e-7` world units in each. This is consistency with the cached visual projection, not distance to an independently calibrated saddle, no-lag proof, or gameplay position error.

Local viewers and CSVs are E/`20260905-phase3f-preview4-rt-final-A-frame-review.html`, E/`20260905-phase3f-preview4-rt-final-B-frame-review.html` and corresponding `-frames.csv`. They reference the immutable original PNGs in R and do not drive the game. Original selected A frames 009, 016, 027, 034 and 037 were inspected: the rider is upright in the visible side view and the pair changes location, but later frames are partly occluded/off-screen at the lower edge. No anatomical calibration or complete attack-cycle PASS follows. P captured only one settled indoor frame; its legacy idle/walk/run/turn/stop row name is not motion evidence. Neither set shows action-bar chrome. HUMAN VALIDATION PENDING.

### Repeated final failure

A and B fail at `AwaitUnmountedRangedAdmissionRt` after the same 23 completed tranche rows. Each fresh sling target records `targetDamage=20`, `targetReady=false`, `combatMemoryReady=false`. The pair is already unmounted; real-time/unpaused/selected/weapon/readiness/idle/previous-target-cleanup checks are true. Final artifacts do not attribute the damage initiator. Stale projectiles, AI, opportunities and target setup are unproven hypotheses; none is silently suppressed or altered to force a pass. Native opportunity events are kept separate from ordinary attack counts. Two repetitions end this investigation, with no additional repair or telemetry build. Even the passing rows do not waive the full-suite failure.

## Build, transaction history and restoration

The one complete final applicable gate passes source `22/0`, Release build `0 warnings / 0 errors`, components `335/0`, visual/source contracts `23/0`, real-filesystem inventory hashing `10/0`, harness/protocol `243/0`, and exact assembly contracts `402/0` (378 Kingmaker, 24 read-only Wrath). Additional native-resource/guard audit `9/0` and clean package validation `10/0` pass. Commands/evidence: `scripts/Test.ps1` in E/preview4-release-gate.txt, E/preview4-native-resource-guard-audit.txt, E/preview4-package.txt. No threshold, package allowlist or legacy assertion was weakened.

Preflight full-byte inventory hashing initially exceeded twenty minutes over 125,816 historical staging files. A bounded BCL implementation now reads every byte with at most four workers, preserves canonical ordering/digests and propagates unreadable/changed-file errors. Ten real-filesystem regressions cover literal/empty/binary/changed-same-size-preserved-time/locked/missing inputs. The complete 26,167,342,326-byte scan took 144.39929 seconds. No evidence was deleted or integrity root omitted.

Steam's user-started current session passed the existing Offline/cloud guard. The approved desktop bridge still returned native pipe unavailable (`os error 2`); no pointer E2E test occurred. No credentials, updates or account-state prompts were handled.

The sole deployment helper was inspected and used with its exact Phase 3F branch and four-scenario allowance. Its SHA-256 is `421a50e19d41afd7032e35f973507216058af4c123e655aa24560e68cd5beb39`. Each admitted live transaction completed WhatIf before mutation and used KMC-owned fixtures with source/package/config/suite continuity. History:

| Live # | Build/run | Gameplay result | Independent restoration, UTC |
|---|---|---|---|
| 1 | preview.3 / `20260905-phase3f-preview3-rt-A` | FAIL/guarded-aborted; inherited RT fixture reached its legacy shared-TB tail before closure, no atomic game result | PASS 21:06:40.5333438Z |
| 2 | F4 / `20260905-phase3f-preview4-mammoth-A` | PASS integration, 62/0 | PASS 21:30:23.8828586Z |
| 3 | F4 / `20260905-phase3f-preview4-presentation-A` | PASS limited pose/binding integration, 49/0 | PASS 21:38:12.1930748Z |
| 4 | F4 / `20260905-phase3f-preview4-lifecycle-A` | PASS fixture lifecycle integration, 400/0 | PASS 21:46:11.8470044Z |
| 5 | F4 / `20260905-phase3f-preview4-rt-final-A` | FAIL, 68/2 | PASS 21:56:16.5409491Z |
| 6 | F4 / `20260905-phase3f-preview4-rt-final-B` | FAIL, 68/2, same terminal failure | PASS 22:10:58.3185215Z |

No more gameplay launches in this bounded mission. The two reserved final runs used the same unchanged candidate. Preview.3's failed trace remains preserved; F4's C0 fixture bypasses the legacy shared-TB transition before any turn request, and schema 7 rejects the six shared-TB rows while retaining every applicable RT check. Historical schemas 1-6 retain their original requirements. This correction does not establish unified TB support or transfer preview.3 credit.

Independent audit occurred before evaluating each run's gameplay artifacts. E contains each `<runId>-independent-restoration.json`, summaries, guard output, exact suite pins and frame records. Starting saves digest is `b42a7a30ed0b9e8b75ddb2c7f2511b5b488f35ef914aa0319897cd336388e19d`; full Mods/settings digest is `67601f2186e5e4ffc81cff0abc4ee376322e2e269ccb3a0daa3d91c571ef950c`. All checks match the original intake inventories, including immutable Baseline and restored Working, foreign Mods/settings, and installed fallback/cache. No active runtime transaction, lock, sentinel or game process remains.

The actual starting fallback is intentionally still installed: `0.1.0-phase3e-fallback.1`, Info hash `f137e69d163967c4d5f36e3610be4b9270ac160923b029cc131d56cb32d24018`, DLL and `.65229.cache` hash `5bcc3bc61bb1677ea81037fdc5a8ebd740ff4d0753d5255e37fcc789e6407f2f`; KMC inventory digest `22b580f2482c8111c1110979bfc6748b6f3ef8004bfcfd57922eeed462193687`. The candidate is not left installed. `Replace -AllowDocumentationDescendant -WhatIf` passes in E/preview4-install-whatif.txt; no permanent Replace was run. Exact instructions are in [the short playtest](PHASE3F-PLAYTEST.md).

Every launch preserved the previous Windows log uniquely before launching. The final B Windows log is separately immutable at `analysis-cache/runtime-evidence/phase3f-preview4-rt-final-B-log-20260905T2212084391848Z/output_log.txt`, SHA `72553f980bf36915653f9f3814ae3f1958254bbfb4d861605b122da4e4f0393a`. It is not substituted for the supplied human log. Large logs, saves, frames and backups remain local and outside Git/packages.

## Remaining path to the finished experience

1. Obtain an appropriately guarded Horse/native-control human session and close ordinary pointer/group/pause/retarget, completed melee repetition, target termination, saddle calibration, UI states and affected save/area boundaries. Carry the repeated unmounted ranged counterexample into a separately bounded investigation; do not treat this partial preview as core acceptance.
2. Establish legal pair-local movement epochs and native Standard conversion before claiming separate-turn TB playable. The next architecture milestone is genuinely pair-aware native turn lifecycle, command ownership and resources informed by the documented K9 selection failure. Preserve narrow Phase 3E command-execution evidence without retrying a scheduler shell as the architecture.
3. Qualify remaining combat-rule/UX/lifecycle gaps explicitly: full attacks, reach/AoOs, mounted spell/item actions, charge/feats and multiple mounted party members are separate priorities. One active pair remains the implemented boundary.
4. Add Paladin Divine Steed through the existing companion architecture under its own mission. Preserve original Horse Ranger selection/AddPet, presentation and Bite, plus independent Mammoth support. Broader mounts and relationship persistence need explicit qualification. Transient mounting does not make uninstall-safe saves while legitimate Horse companion/feature blueprints remain referenced.
