# Combined actor-allocation and paired-activation milestone

Status: **IN PROGRESS**. The complete paired loop is implemented and has passed in both initiative arrangements. Preview35 passes the 32 accepted A10 cases. Final37 native allocation/A05/A10 and Mammoth TB qualification remains required.

## Authority and tested policy

Work continues on `codex/mounted-combat-phase3f-playable-core`, preserving reviewed `45e3d276754257f4513342d5bce7626dd609d252` and Chunk2 source `c804ba052760063f747cde83265e660916984d72`. [Frozen Chunk2](CHUNK2-ACTOR-ALLOCATIONS.md), [earlier command admission/K9](PHASE3E-PAIRED-SCHEDULER-IMPLEMENTATION.md), [reference map](../planning/WOTR-SHARED-TURN-MAP.md) and accepted preview13 [human play](CHUNK1-HUMAN-PLAY.md) retain their original scope.

Configuration: `EnablePairedActivation=true`; `EnableUnifiedMountedTurn`, `EnablePairedCommandScheduler`, `EnableDiagnosticOverlay` and actual overlay presence false. The human installation remains preview13 with its settings unchanged. One pair, existing Horse/Mammoth, mounted before combat; rider initiative is principal. Transport spends mount movement without adding rider Move cost. Native Full/Single/Primary meanings, weapon-specific reach, actor defenses, commands and costs remain.

Exact Kingmaker assembly SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; Harmony12, .NET4.7, C#7.3. Bounded local IL and interoperability analysis remain outside Git/packages.

## Implemented lifecycle

The existing UnifiedMountedTurnCoordinator is the sole activation authority, with encounter GUID, actual native principal boundary and sequence. Separate actor records track grants, preparation, expenditure and completion. Selection, a new relationship object and global round changes cannot grant resources.

The mount is excluded inside the native ChooseNextUnit candidate loop. Native sorting, wrapping, unrelated actors, effects and world participation remain. The rider and a private mount context each execute complete native preparation once, including interruption/reapplication, reactions, round/fact/AI/condition processing and readiness. The private context omits its final UI tail; it never becomes CurrentTurn or starts/ticks a second driver. The old partial preparation and scheduler are bypassed.

Command eligibility and existing selection/prediction controls admit only the live pair's authorized native commands. Both actors determine continuation; explicit End forfeits both grants. Normal mount selection and Full/Single/Primary retain native planning, timing, conditions, cancellation and costs. No generic off-turn exception, player-turn spoof, ignored-cost attack child or request-time cooldown clearing is added.

Unused same-round Delay resumes the same grant. Mode exit forfeits remaining resources, preserves greater debt, and requires both native readiness and at least six native seconds before renewal. Dismount retains current-round participation; removal/disable finalizes contexts before retiring references. Still-mounted pairs re-arm outside combat after native removal, without preparation or debt changes.

The exact installed foreign Confusion prefix returns before native code for an off-turn partner. An original pair-local adapter uses the verified Kingmaker core parts, rules and native factories at the authoritative preparation boundary. Owned condition commands retain actor-local completion through forced detachment. No foreign mod is changed. SelfHarm's native forfeit-before-charge sequence settles only its observed temporary contribution at native End, once.

Preview36 exposed the previously automation-only setting through the existing developer panel. The activation service rejects changes while mounted, in combat or while participation remains owned; enabling rejects either legacy authority. Fixtures use this same entry point and record rejected switches before combat and after actual partial movement. Defaults remain false. Preview37 carries this implementation with corrected build identity and fail-closed source/package validation.

## Evidence

Raw runs are under `runtime-evidence/20260907-paired-*` and `runtime-evidence/20260908-paired-*`. Manifests, copied native logs, restoration audits and qualification indexes are in `analysis-cache/runtime-evidence/paired-activation-20260907/`. Original failures are preserved. The report at commit `3bf84a6445c3743c3a0bcb69a2d4bc63f1078041` retains the detailed A-AQ investigation; this report records the current result without duplicating that journal.

| Evidence | Status | What it proves |
|---|---|---|
| I/J preview9, both pre-pair orders | PASS,48/0 each | Three full paired activations and fourth refresh, unrelated friendly/enemy turns, actual partial transport, charged actor attacks, exhaustion/refusal, residual movement, Standard conversion/refused attack and early End. |
| AG/AH preview32 paired; AI/AJ unmounted | PASS native7/0 each paired,3/0 each control | Complete P01-P06 and36 A05 actor/round samples. AH original outer failure remains beside strict corrected revalidation. |
| AM preview34 Mammoth TB | PASS,1/0;66 assertions | Native Primary, rider principal, mount Standard6/rider0, two paired grants and no extra mount turn. Final-candidate rerun required. |
| AO-AS preview35 accepted A10 | PASS,32 cases/287 assertions | Ordinary TB19, accepted phase3h RT9, unmounted RT2, Mammoth RT1 and party1. AP/AQ original outer configuration-schema failures remain beside strict corrected validation. |
| AT requested preview36 | FAIL at mod load;0 gameplay assertions | Compiled identity remained preview35. Source validation reported21/1 but its child exit did not stop callers. No game result was produced; independent restoration PASS. |
| Final preview37 | IN PROGRESS | Corrected identity, guarded developer configuration, final native qualification pending. |

First-gate source `5468c24a2b7716442b7233becb585dd62f49ed3c`, ZIP SHA256 `7c0832cef5034f63f22a865cfb9745418f691d67ba7fa7bd5eee14185353e3b3`. Each run has nine ordered actor/round callback samples: one clear, round/AI/fact effect and healing application, distinct round/readiness handlers, zero observer errors/drops. Four real enemy movement legs consume one mount reaction, reject duplicates and prove renewal at the next preparation.

Representative later observations: N13 Stop retains0.403796852m/0.07948685 native Move seconds; step1.00000513m costs0 and rejects subsequent ordinary movement. R17 selected-mount travel0.9999945m costs0.191891417 seconds, rider Move0; mount Full3/Single1 and rider Full4 resolve natively, automatic completion uses no End input. U20 get-up costs Move3 once, and disabled-mount completion refreshes both actors. P05 qualifies DoNothing/SelfHarm continuation and forced split; P06 qualifies real native damage/death, native Clear, two native removal notifications, one End per actor, conserved final debt and the correct unrelated successor.

AO35 records10 distinct paired encounters and3411 trace events without drops, proving the next-encounter re-arming repair. Its DLL SHA256 is `fd386fadc5dc21e4325de5059cc38497a8da7526799b80a81d6eedfd6eb2c248`, MVID `11633cee-0a94-480f-91b2-da7382ad0422`. Script-only packages preserve the exact Info/DLL payload; equivalence receipts and original source/package identities are retained in `preview35-qualification-index.json`. Full preserved AO/AP/AQ result revalidation passes39/0 each with strict five-field configuration validation.

## Remaining supported acceptance

| Gate | Status | Final qualification needed |
|---|---|---|
| A01-A04 exhaustion, residual movement, conversion, renewal | IN PROGRESS | Final37 P01/P03 in both arrangements. |
| A05 complete preparation, effects/readiness/reactions | IN PROGRESS | Both paired and both unmounted final37 runs; historical36-sample evidence does not certify37. |
| A06 Stop, approach, interruption | IN PROGRESS | Final P02/P05 plus accepted ordinary/RT controls. |
| A07 step, restrictions, get-up, disabled completion | IN PROGRESS | Final P02/P04 and real opportunity-reaction loop. |
| A08 split, death, record lifetime, new encounter | IN PROGRESS | Final P02/P05/P06 and ordinary repeated encounters. |
| A09 TB-RT-TB debt | IN PROGRESS | Final spent-Standard/movement mode transition, elapsed native time and once-only renewal. |
| A10 regression and Mammoth TB | IN PROGRESS | All32 accepted cases and paired Mammoth TB on final37. |
| Final checks, private candidate and publication | TODO | Follow final gameplay qualification. |

Cross-round Delay is **DEFER — EVIDENCED**, visibly rejected; unused same-round Delay is separately tested. Full mounted save restoration and same-campaign cold-load debt rebinding remain later persistence work. Deleting actor references cannot preserve unrepresented debt. Third-party custom confusion-choice restrictions, broader forced displacement and different-campaign native loading remain unqualified. Safe cleanup/rejection does not qualify those transitions or the complete feature.

## Checks and external state

Preview37: build/source22/0, components364/0, patch construction30/0, actual principal evidence serialization3/0, harness247/0. Actual stale-source negative checks stop Build-Local before output mutation and the runtime launcher before evidence/lock/game creation. The actual old packaged DLL is rejected against candidate metadata. Repeatable harness regressions exercise caller failure propagation and missing/stale compiled product identity. These are component/safety evidence, not gameplay PASS.

Working37 DLL SHA256 `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`, MVID `2ee2c106-2b00-45c7-bb99-c7d65f0228a7`; isolated inspection confirms informational version `0.1.0-paired-preview.37`. Final package/native records remain pending.

All46 A-AT transactions independently restore actual intake: preview13 DLL/cache SHA256 `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`; protected saves digest `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`; full Mods digest `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`. Latest AT audit2026-09-08T09:16:40.462Z, copied log SHA256 `cb67a7b23ae74f77e2fdfc08297d795061f3177dc5fa00a4c000ad4b91f792f5`. No game or lock remains. Only disposable KMC_AUTOMATION_WORKING may be changed by guarded transactions; no permanent installation, foreign-mod change, human campaign edit, main merge or public release.

Guarded direct-helper publication is verified through `b96464da2947ff7c0cf1b5f104ed16c070c15886`; newer coherent descendants await publication. Exact current work and next command are recorded locally in campaign `ACTIVE-RUN.json`.
