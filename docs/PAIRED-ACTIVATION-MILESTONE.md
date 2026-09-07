# Combined actor-allocation and paired-activation milestone

Status: **IN PROGRESS**. The three-activation movement/attack loop passes in both initiative arrangements. Consumed reactions and remaining accounting, transition and final regression gates are unqualified.

## Authority and configuration

Integration branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `45e3d276754257f4513342d5bce7626dd609d252` and Chunk2 source `c804ba052760063f747cde83265e660916984d72`. One supported pair mounted before encounter uses rider-principal participation; native actors retain health, defenses, commands and costs. CRPG transport spends mount Move; ordinary full/Single/Primary semantics remain. [Chunk2](CHUNK2-ACTOR-ALLOCATIONS.md), [earlier admission/K9](PHASE3E-PAIRED-SCHEDULER-IMPLEMENTATION.md), [reference map](../planning/WOTR-SHARED-TURN-MAP.md) and accepted preview.13 [human play](CHUNK1-HUMAN-PLAY.md) remain separate evidence.

Every paired result uses `EnablePairedActivation=true` and all three older experimental flags false. Human settings remain untouched. No permanent install, main merge, release, persistence or content expansion. Exact installed Kingmaker assembly SHA `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; Harmony12, .NET4.7, C#7.3. Bounded native contracts stay local in lab `analysis-cache/paired-activation-native/`.

## Implementation

The existing turn service owns encounter identity, an explicit native principal boundary and distinct actor granted/prepared/spent/ended state. Selection, relationship replacement and global rounds cannot grant resources. Before first selection, the native selector excludes the exact mount candidate while preserving native sorting, wrapping, unrelated turns and world participation.

The rider runs native preparation. A private native mount context runs the complete verified preparation once: interruption/reapplication, reactions, round/fact/AI/confusion/readiness. Only its final cursor/prediction UI tail is omitted. It never becomes CurrentTurn, starts or ticks a second driver. Its phase follows the actual native Tick assignment, preserving native command-end finalization. Exact owned partner commands gain eligibility; native Move's IgnoreCooldown convention still incurs real movement debit. Old partial preparation/scheduler/manual driver paths are bypassed.

Continuation checks both actors; explicit native End forfeits both and native End completes participation. Exhaustion yields to the original native completion/interruption branch. Per-actor movement and private-context state share the real debit. Cleanup retires supplemental references without clearing native debt. Split, mode conversion, delay and condition details still need the work listed below.

## Native ledger

Artifacts are under lab `runtime-evidence/<runId>/`. Campaign intake, independent restoration audits and copied native logs are in `analysis-cache/runtime-evidence/paired-activation-20260907/`. Detailed earlier investigation remains in source history; failed raw traces are unchanged.

| Run / candidate | Strict result | Finding / correction |
|---|---|---|
| `20260907-paired-A` / preview.1 | FAIL45/3 assertions | Native Move IgnoreCooldown convention rejected; encounter actor reference remained. Exact admission/retirement repaired. |
| B / preview.3 | FAIL46/2 | Both Primary attacks ran, but private context stayed Preparing. Setter postfix missed inlined transition. |
| C / preview.4 | FAIL46/2 | Tick injection fixed phase and native Move3 finalization. Net displacement was incorrectly equated to travelled path. |
| D / preview.5 | FAIL46/2 | Actual movement/time/debit agreed, but exhausted native Move remained unfinished. Original negative completion branch was skipped. |
| E / preview.6 | FAIL, native48/0 | Whole loop completed; strict validation rejected missing P01 registration and19 pre-init observer errors. |
| F / preview.7, mount-first | **PASS48/0** | Three activations/fourth refresh, exact callbacks/effects, unrelated turns, no mount turn, zero errors/drops. |
| G / same preview.7, rider-first | **PASS48/0** | Same complete loop and strict checks with opposite pre-pair initiative inputs. |
| H / preview.8, rider-first | FAIL46/2 | Native mount reaction consumed once, ordinary debt unchanged; enemy movement interrupted after1.5021044m, cost0.365368843/time0.3653693, with resources left. Condition/navigation cause was not recorded. |

F/G source `e9aad2228399feec1aa74096658c27cf89c67c15`. ZIP `KingmakerMountedCombat-0.1.0-paired-preview.7-trace-admission-diagnostic.zip`, SHA `7e2d44f2a18d3a1298dfdbdabc997342c251425d03600bdf66e21c1d16d08ff1`; DLL SHA `0ac6bad536bc362dcd2326f4eda22464d33689edb997b5cfaa60f863b476eb69`, MVID `244a78c4-4325-4c9c-b5df-ac9f9f917e5d`; suite7 SHA `f667cfd00a47b02b7ea2a94f836d2adad94752bf7859749282e36e1455ac0a8b`.

Each passing loop delivers partial rider-controlled transport, real rider and mount Primary attacks, native Move-plus-Standard exhaustion and rejected excess motion; next activation residual movement/Standard conversion, native exhausted interruption and refused unaffordable attack; third partial movement/early native End; fourth fresh grant. An enemy and unrelated friendlies participate every measured round. Nine actor/round A05 samples per run show one native clear/round/AI/healing effect and unique ordered round/readiness handler deliveries. Actual native shifts and travelled segments agree with allowed time/debit; rider Move stays zero. Reaction initialization is observed; actual consumed opportunity behavior is not yet proved by F/G.

## Restoration

Fresh intake2026-09-07T12:27:02Z, host DESKTOP-SRJJ623. Actual preview.13 DLL/cache SHA `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`; save digest `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`; Mods digest `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`. **All eight A-H transactions restored these actual digests**, including saves/current settings/caches/foreign Mods. No game or live lock remains after H.

F audit PASS14:56:25Z, native log SHA `80ee057b589517f6288edc745333168ff852beb4160649107b32af783344ae57`; G PASS15:07:58Z, log SHA `58d68720c70d03901baf1e55c33fb1cad49c4dfed5420ca8b19653fe82fd20f6`. Every live run used completed exact-candidate repository WhatIf, guarded transaction and independent audit in finally. Initial automatic approval rejection was resolved by that direct-launch preflight; no guard weakened.

H source `90a82027a6bc693d4841a350f4918808b2954c10`, DLL `ff8a95e739a96ffdd739d4ee0c4a6f127ba4d27ca15b66ba4baa136077df3684` / MVID `cde64e87-37c1-45ad-8bad-ef2e6df75437`, packageSHA `6db608ee338b4b3d41074502839e9cef06f92d74943bce02c8f7b55b0f6b74db`. Restoration PASS15:55:52Z, logSHA `c17ae354eade5a9b921584a5e8afd7e2b4c40f735994f3a48ff5eb11c579f092`.

## Active checkpoint / remaining gates

Preview.8 extends the same loop with native enemy-turn movement provoking one real mount reaction, rejecting repetition and checking next preparation refresh. Schema12 requires that evidence; schema11 preserves F/G's earlier contract. Ordinary TB intent is now bound to its addressed actor instead of enabling legacy automatic mount follow-up. Actual movement debit updates the existing actor spent-state observation.

Preview.9 changes only the reaction stimulus/observation: use the established native navmesh point search, lease a native maneuver-immunity condition on the disposable target, observe native maneuver/prone state and verify condition restoration before target destruction. This isolates repeated movement from trip/knockdown effects without forcing attack rolls, resources or turn ownership; maneuver gameplay is not qualified by this fixture. H remains FAIL. Protocol54/0; build/source22/0; deployment harness244/0.

Prior preview.8 focused checks: components354/0, source22/0, protocol53/0, structural lifecycle patch construction5/0, native movement signatures2/0. Detached checks do not qualify callbacks. Deployment harness244/0 after updating the exact schema expectation; old native and restoration assertions remain.

| Gate | Status | Remaining evidence |
|---|---|---|
| Three-activation loop, both arrangements | PASS | F/G on exact preview.7; repeat extended loop on current candidate. |
| Complete first gate | IN PROGRESS | Consumed reaction, duplicate rejection, native refresh. |
| A01-A09 | IN PROGRESS | Stop/approach, step/restrictions, automatic completion, split/death/lifetime, TB-RT-TB debt and delayed actions. |
| Final-path A05 | IN PROGRESS | F/G counts pass; reactions and final preparation changes require final candidate. |
| Final A10 / ordinary controls | TODO | Full/Single/Primary, Rapid Shot, mixed reach, hover/reclick/approach, accepted RT, pause/Stop/selection, unmounted, Mammoth/party. |
| Private candidate / guarded publication | TODO | After gameplay/regression status warrants delivery. |

Same-campaign cold-load debt rebinding is later persistence work. Deleting references does not preserve unrepresented debt. Safe visible rejection protects unsupported states but does not qualify their transitions. A passing loop is an intermediate result, not completion of this milestone.
