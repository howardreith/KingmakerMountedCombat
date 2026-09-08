# Combined actor-allocation and paired-activation milestone

Status: **IN PROGRESS**. The first loop passes in both initiative arrangements. Both native condition continuations pass on preview30; its death fixture is being corrected for actual damage scaling. P06, paired Mammoth TB, remaining supported accounting/transition cases and final A05/A10 are not complete.

## Authority and configuration

Branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `45e3d276754257f4513342d5bce7626dd609d252` and Chunk2 source `c804ba052760063f747cde83265e660916984d72`. [Frozen Chunk2](CHUNK2-ACTOR-ALLOCATIONS.md), [earlier admission/K9](PHASE3E-PAIRED-SCHEDULER-IMPLEMENTATION.md), [reference map](../planning/WOTR-SHARED-TURN-MAP.md) and accepted owner preview.13 [human play](CHUNK1-HUMAN-PLAY.md) remain separate evidence. This mission authorizes the paired lifecycle, guarded temporary testing, private packages and branch publication. No permanent installation, main merge, public release, persistence or content expansion.

Measured configuration: **EnablePairedActivation=true**; EnableUnifiedMountedTurn, EnablePairedCommandScheduler and EnableDiagnosticOverlay **false**. Human settings remain unchanged. Exact Kingmaker assembly SHA `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; Harmony12, .NET4.7, C#7.3. Local signatures/IL and original analysis stay in lab `analysis-cache/paired-activation-native/`; proprietary material is not shipped or committed.

## Implemented lifecycle

The existing UnifiedMountedTurnCoordinator is the sole authority on the new path. Encounter identity, actual native principal boundary and sequence bind distinct actor granted/prepared/spent/ended/finalized state. Selection, relationship replacement and global rounds cannot grant. A pre-combat pair uses rider initiative; exclusion in the actual native candidate loop suppresses its mount while retaining native sorting, wrapping, unrelated order and world participation. Mid-combat mounting remains rejected.

Rider and private mount context each execute complete native preparation once: interruption/reapplication, clearing/reactions, round/fact/AI/condition processing and readiness. The private context omits only its final UI tail; it never becomes CurrentTurn, starts or ticks a second driver. Native phase transitions synchronize it. Authorized commands retain native planning, timing and actor costs. Carried movement spends mount resources and adds no rider Move cost. The old partial preparation and manually driven scheduler are bypassed.

Continuation considers both actors and native automatic-End preferences. Explicit End forfeits both grants. Same-round unused Delay resumes the same identity without preparation/effect replay. Mode exit forfeits the pair, preserves greater native debt and requires both native readiness and at least six native seconds before a new grant. Split retains mount participation through the current round. Removal/disable finalizes live contexts before retiring references. Deleting references does not preserve unrepresented cold-load debt.

Normal mount selection, actor-specific path/prediction, Full/Single and explicit Primary use narrow verified native seams. Get-up admission retains native CanStandUp and Move affordability. Exact condition commands retain scoped ownership and actor-local native ForceToEnd through forced detachment. No general off-turn exception, CurrentTurn/IsCurrentUnit spoof, ignored-cooldown attack child or request-time resource reset is added.

## Native evidence and current correction

Raw runs: `runtime-evidence/20260907-paired-<letter>/`. Exact package manifests, dry-run receipts, copied native logs and independent restoration audits: `analysis-cache/runtime-evidence/paired-activation-20260907/`. Earlier investigations remain in Git history and their original raw artifacts; the report at `e65bf41bd09930aaeaa21a8b5f1199fe55fe7057` retains A-AB details.

| Evidence | Status | Demonstrated behavior |
|---|---|---|
| I/J preview9, rider-first/mount-first | PASS48/0 each | Three complete paired activations plus fourth refresh, with unrelated friendly/enemy turns. |
| N13 rider-first | PASS49/0 | Same-grant Delay, partial Stop, TB-RT-TB, step/rejection and split participation. |
| R17 mount-first | PASS50/0 | Selected-mount ordinary Full3/Single1, rider Full4, pure hover, residual movement and automatic completion without End input. |
| U20/V21/Y24/Z25/AA26 | FAIL overall | Native P01/A05/P03/P04/P02 pass; later condition continuation fails. Each raw failure remains visible. |
| AB27 | FAIL | Native DoNothing admission, mount actor-end and forced split; rider continuation assertion fails. Host300-second deadline interrupts cleanup before detailed export. |
| AC28 | FAIL | P01/A05/P03/P04/P02 pass. Detailed failure proves completed native recovery interruption plus premature rule sampling; cleanup retains the fixture-created control part. |
| AD29 | FAIL | P01/A05/P03/P04/P02 and complete DoNothing continuation pass. SelfHarm succeeds and damages its actor once, but the assertion finds Standard12 before native End. |
| AE30 | FAIL overall | P01/A05/P03/P04/P02/P05 pass, including independent strict P05 validation. P06 requests27 native damage but receives5; mount remains conscious and the leaf times out. |

First-gate source `5468c24a2b7716442b7233becb585dd62f49ed3c`, ZIP SHA `7c0832cef5034f63f22a865cfb9745418f691d67ba7fa7bd5eee14185353e3b3`. Both arrangements demonstrate actual rider-requested partial transport, charged rider/mount Primary actions, mount exhaustion/refusal, fresh residual movement, Standard conversion followed by attack refusal, early End and once-only renewal. Each run has nine actor/round callback samples (rider, mount, unrelated friend over three rounds), one clear/effect/AI/heal and ordered callbacks, zero drops/errors. Four real enemy movement legs consume one mount reaction, reject a duplicate and prove next-preparation renewal. No independent mount activation occurs.

R17 selected-mount travel0.9999945m costs0.191891417 native Move seconds with rider Move0. N13 Stop preserves0.403796852m/0.07948685 native seconds; step1.00000513m costs0 and rejects ordinary movement afterward. U20 get-up invokes one callback and Move3/Standard0 with rider0/0; native get-up interrupts its temporary move. Staggered movement travels0.4724121m and costs0.1766603 native seconds. Disabled-mount automatic completion refreshes both actors once. Spent mount Primary6/3 precedes the mode-conversion debt observation.

Z25/AA26 isolate why a native-body admission transpiler could not work: installed CallOfTheWild prefix06001584/MVID8caab254-aacf-4811-8093-44b9184e6e53 replaces Confusion TickOnUnit and returns at its own independent-current-actor guard. The actual native predicate invocation count is zero. Preview27 therefore implements an original partner-local adapter for the verified Kingmaker core condition sequence, using native parts, D100 rule, control retention/release, elapsed-time/Standard gates, factories06009132-34 and Run. The rider retains its installed callback. No foreign assembly, setting or Harmony registration is changed. Third-party custom confusion-choice restrictions remain unqualified.

AC28 source `e65bf41bd09930aaeaa21a8b5f1199fe55fe7057`, ZIP SHA `3d53f577006052eb52c326832a54e59900de6a3d7be8afd95717a25da1a70886`, DLL SHA `634187865d9c1149b328eb5f3c83e44560ec75dc2f830a7614862f35909b787d`, MVID `3b6df316-4556-492a-aaa3-73553a532034`. P05 identity `a5ae73cbac194478b63737c2749fffdc:1`: native DoNothing succeeds, mount ends with6/3, rider remains unended0/0, and forced split occurs. Rider command1283175168 dispatches its one planned attack, charges6/0 and enters native OnTick recovery at frame15181 with prior Success and no pending attack. At frame15182 it is Interrupt, while resolved rules are still0. The fixture aborts before waiting for projectile resolution. Mount6/3 remains unchanged. Cleanup then reports nativeTB=false, empty commands, rider restored, but mount direct control/AI restoration false. External restoration still passes.

Preview29 reuses the accepted ordinary fixture's recovery provenance and waits for actual resolved rules/hands-free state while retaining the earlier terminal cost snapshot. Only an exact plain UnitAttack with its native recovery observation can qualify Interrupt. The stimulus lease removes only its exact newly created native Confusion part through native OnRemove, checking restored control and unchanged costs. AD29 proves DoNothing continuation, one rider projectile rule at14761 after recovery14755, native6/0 rider cost, unrelated successor and restored condition control without cost changes. Source364c04fdeaac6e61613c71aac7c0a43d027185e2, ZIP034b65eadaa37bdf6416f84c3a2e535e7d1cdd8413f9315c1d0b1b9cff9a8edd, DLL396be1ce687de32f803f4983ee654ec9c77f591113f5a4ababd27c260d768d97/MVID6eb4e7d9-05e8-433b-8efb-57496c99c7e2.

AD SelfHarm245268480 in identitybcb3530dbdb4465299d5cfffe71ec5e6:1 succeeds, deals5 native self-damage and ends only the mount. Its exact actor-cost event4390/4391 at15341 changes Standard6 to12 with Move3 unchanged. Installed SelfHarm.OnAction0600270C calls ForceToEnd06000C47 before the actual Standard charge; native End06000C46 later writes6. The paired maximum-debt adapter incorrectly preserves that temporary forfeit-plus-charge total. Working30 records only the observed Standard contribution of the exact owned SelfHarm forfeiture and settles it once at native End, retaining native End's floor and any additional debt. It leaves the action cost callback untouched. Strict protocol requires native6-to12 charge and12-to6 final settlement, rejects missing/duplicate cost and End events, and still requires the full rider continuation. Two domain service tests cover once-only settlement, additional debt and new-grant isolation. Exceptional fixture cleanup now releases its exact condition control before AI restoration. Full P05 remains unqualified until native30 passes.

AE30 independently qualifies both condition continuations on source2514243ff5f33f2315b5206576bd1ae9df5aa4f8, ZIP9e35dd1daf13a7f201e9d3e663035a2e6264061acca1c6eac35c26fca1ad64af, DLL8f2b26550984f7c1dcb712f9efaea129321464e10d95b2012284c57f6325345e/MVIDfb9a27b5-f027-40ce-8a6d-26b768f72b2b. Each rider resolves one rule; SelfHarm retains native Standard12 through continuation and settles to6 at one native End. P06 then reaches real partial movement and mount Primary6/3, but its27-point enemy damage stimulus delivers only5, leaving the mount conscious. No native death/removal is qualified by subsequent cleanup. Installed ApplyDifficultyModifiers060073FF reads GameDifficulty.DamageToParty06000CFB and truncates the scaled damage. Working31 sizes only the stimulus through that actual multiplier, records the native damage before/after difficulty, and rejects insufficient damage or changed settings. Native IsDead, one life event, conserved costs and correct unrelated selection remain mandatory. Native31 is pending.

The allocation host now allows720seconds around its existing660-second engine. Native leaves remain30seconds. Failed cleanup after30seconds exports a failed ledger, returns to parent cleanup and retries best-effort cleanup on disposal. AC demonstrates failure evidence survives; this does not qualify local cleanup or weaken external restoration.

## Remaining acceptance

| Gate | Status | Remaining evidence |
|---|---|---|
| First loop, both arrangements | PASS | I/J establishes architecture; later runs preserve the loop. |
| A01 exhaustion / A02 residual / A03 conversion / A04 refresh | IN PROGRESS | Core P01/P03 pass; reconcile supported catalog on exact final candidate. |
| A05 complete callbacks | IN PROGRESS | Final candidate, both arrangements and unmounted control; P05 condition composition passes on30. |
| A06 Stop/approach/interruption | IN PROGRESS | Native Stop passes; remaining approach/interruption cases. |
| A07 step/restrictions | IN PROGRESS | Step, staggered, get-up, disabled completion and reaction evidence present; final-path reconciliation required. |
| A08 lifecycle | IN PROGRESS | Split/record retirement and P05 forced-split continuation pass; P06 death/removal pending. |
| A09 mode/debt | IN PROGRESS | Partial and spent-Standard TB-RT-TB evidence present; broader supported transition review remains. |
| A10 final regression | TODO | Exact final candidate: ordinary TB19, accepted phase3h RT9, unmounted RT2, Mammoth RT1, party1; also paired Mammoth TB. |
| Private testing candidate / final publication | TODO | Deliver when gameplay/regression status warrants testing. |

The existing Mammoth TB fixture now observes natural rider preparation, native Primary, intervening actors and a second grant. Schema57 preserves target/AI/reach/cost/cleanup assertions without forced StartTurn or old scheduler enablement. Native qualification remains TODO.

Cross-round Delay is **DEFER — EVIDENCED**: its native activatable processing/disposal timing is unqualified and visibly rejected. Full mounted save restoration and same-campaign cold-load debt rebinding remain later persistence work. A safe rejection does not qualify a transition or the complete feature.

## Checks and external restoration

Working31: build/source22/0, components364/0, native patch construction30/0, condition factory/adapter observer contracts1/0 each, movement signatures2/0, death/removal and native condition observer1/0 each; new native SelfHarm/End contract1/0, allocation82/0, restrictions41/0, conditions55/0, death26/0; native enemy damage difficulty contract1/0. Full harness28 safety245/0 remains the latest complete run. Mammoth focused/outer protocol18/0 is component evidence. These checks do not substitute for native gameplay or final regression.

All31 A-AE transactions independently restore actual intake: owner preview13 DLL/cache SHA `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`; protected save digest `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`; Mods digest `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`. Latest AE audit2026-09-08T04:05:31.006Z, copied native log SHA `981bf710a0aa1cc03935312e1391d3ff439cda26a5e283b3da01a333c1575067`. Only disposable KMC_AUTOMATION_WORKING is mutable. No game/lock remains at this checkpoint; no permanent installation. Guarded publication is verified through `2514243ff5f33f2315b5206576bd1ae9df5aa4f8`; newer descendants await publication.
