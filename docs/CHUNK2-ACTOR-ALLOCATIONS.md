# Chunk 2: Actor allocations and movement conservation

Status: **BLOCKED — CRITICAL** for milestone completion. Preparation timing, movement pacing and record lifetime have narrow repairs. Native actor entitlement before preparation remains unresolved; A01-A09 are not complete. Final-candidate A10: **PASS, 32 cases / 0 failures**. The owner's working preview.13 installation remains intact.

## Baseline and scope

Branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `b3f063337644215312de97d9736892212777ac1c`, intake descendant `aa0bdc41110923a0aae3bd1ac49322e5f2b75c02`, and legitimate descendants. Accepted Chunk 1 binary source is `a8745640e18ce068e412b4e360c7b0a3d46c738a`, `0.1.0-chunk1-preview.13`.

**OWNER-REPORTED HUMAN PLAY:** preview.13 installed/enabled; TB ordinary/full bow attacks, Horse approach/attack, no extra move in the tested rider-to-Horse switch, pure hover, and RT combat/controls/mount/dismount worked well. This does not establish exhaustive allocations, precise Rapid Shot modifiers, visible Bite recovery, formal charge or cold load. [Human installation/play record](CHUNK1-HUMAN-PLAY.md); [frozen Chunk 1 engineering evidence](CHUNK1-ORDINARY-ATTACKS.md). No repeat of those human results was required.

Actual host is DESKTOP-SRJJ623, Kingmaker 2.1.7b, UMM 0.28.2 and Harmony12 1.2.0.1. The three experimental flags remain false. One pair, separate native turns, Horse/Mammoth, native attacks and explicit Primary remain. No scheduler, persistence, content, new UI, main merge, public release or permanent replacement is included.

## Repairs and allocation contract

1. **Restore expenditure before dependent preparation callbacks.** Old trace-D and failed fixture-B showed round/AI/readiness callbacks observing Move0 before the old postfix restored delivered Horse movement. The correction now enters at exact `UnitCombatState.OnNewRound`, after native clearing and acting-command cost reapplication, before round/AI/fact/readiness processing. There is no callback replay or transpiler. Rider-R sequences126/127 show clear-to0 then retained Move.1449457 before round state; native feature observations135/136 preserve that cost while damage6 becomes5. Readiness and final preparation see the same retained movement.
2. **Match native delegated pacing.** Old D's matched .75m path cost about.319 action-time units through rider control versus.143 through native Horse control. The adapter now mirrors native permitted, non-forced minimum-speed/warm-up/slowdown handling; native restoration remains authoritative. R measures .1425-.1449 versus native Horse.1431-.1435 at5.08m/s. S's raw Move0-to.2866 includes reinstated earlier.1435 plus new.1431; the whole raw delta is not a fresh debit. Transport adds no rider Move.
3. **Retire obsolete actor references explicitly.** Records survive selection/dismount and do not settle solely through cooldown decay while unprepared expenditure remains owed. Destruction, settled encounter exit and replacement player/campaign identity retire obsolete references. Five service-level lifetime tests pass; native R/S remove the disposable Horse and finish with zero records. Different-campaign loading and same-campaign debt persistence are not qualified.

The exact installed `Prepare` (`06000C3C`) order is interruption as applicable, AI force tick, cooldown clear (`0600C3BE`), acting-command costs, reaction reset, round state (`0600939D`), round handlers, AI round tick, `Unit.Logic` each-round facts, confusion/readiness/UI. Native actors and commands remain the charging authority; the supplemental record reinstates measured expenditure and is not a second action bank. Exact assembly SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` gates the patches with own-patch rollback. [Contract matrix](../planning/ASSEMBLY-CONTRACT-MATRIX.md).

An actor's next observed native grant starts at its own preparation. Selection, pairing, a global round number and cooldown decay are not grants. Carried paths spend mount movement; native rider attacks keep native rider costs. Native speed/action-time formulas establish the local3/6 boundaries and `MetersOfFiveFootStep`2.286; these are not assumed tabletop metres. Native End Turn through `Game.PauseBind` processes the real boundary and forfeits unused actions. No measured initiative, readiness, turn or cooldown rewrite was used.

**Remaining preparation/continuation dependency:** R's first rider move happens with mount grant0, `CanActInCombat=false` and five time units until its native turn. R rounds4/5 enter with the previous mount grant, Standard about5.14494/Move2.14494; the rider delivers about.85506 time units then stalls. Later native Horse Prepare preserves that debit and permits movement through Move6. Mount-first S preserves exhausted movement and later rider input moves0 despite raw cooldown decay. The old controller/global-round/start-time key is deliberately unchanged and remains insufficient as actor entitlement.

No legal reservation against an exact upcoming allocation has been proved. `GetTimeToNextTurn` (`0600837C`) is initiative plus max(Standard,Move), and native next-actor selection/time advancement uses it: clearing stale Standard also changes readiness. Preparing early invokes native round/fact/reaction/readiness processing and requires exactly-once later participation. Restricting all transport to the Horse's current turn would lose the accepted carried-movement-plus-rider-action sequence without coordinated continuation. The remaining Chunk3 seam must declare that grant/preparation/continuation contract and then close A01-A09; this does not justify another attack shell, pooled bank, selector retry or whole-controller rewrite.

## Native evidence and acceptance

All new run IDs begin `20260907-chunk2-`. Raw immutable files are under lab `runtime-evidence/<run-id>/`; intake, audits and compact summaries are in `analysis-cache/runtime-evidence/chunk2-20260906/`. Input is **scripted native handler/prediction integration**, not desktop input or HUMAN PLAY.

Matched native allocation scenarios on frozen preview7/source `a3f6926a4a2914f30b49bfb956e72f73e607cb16`: rider-first R, mount-first S, unmounted mount-first T and unmounted rider-first U, each49/0. Each uses deterministic initiative setup before encounter, qualified AI isolation, three complete native rounds/six short paths, then two exhaustion/refresh rounds. T01/T02 explicitly say `gameplayQualified=false`; their coverage PASS does not qualify incomplete gameplay gates.

A05 observes 36 actor/preparation measurements across those four conditions, including unrelated actors: one native clear/round/AI/fact boundary, one native healing effect, two distinct round handlers and ten readiness handlers, in order. Total 5287 trace events, zero drops/observer errors. The temporary fact clones a loaded native feature, replaces its components with native fact-action/healing components, and restores owned facts/HP after measurement; it never calls `OnNewRound` manually. Reaction state is observed; consumed-reaction/opportunity behavior remains unqualified. Allocation production code is unchanged in previews8-10, whose changes concern diagnostic durability and cleanup. A05's native source identity remains7, separate from final A10.

| Gate | Status | Actual scope / missing evidence |
|---|---|---|
| A01 exhaustion | TODO | Spent movement is retained in S; movement-plus-mount-attack exhaustion and explicit refusal remain open. |
| A02 residual movement | TODO | Short carried/native paths preserve debit; full switching within a declared actor allocation remains open. |
| A03 Standard conversion | TODO | Native double movement reaches Move6; matched mount attack/conversion/rejection remains open. |
| A04 refresh | FAIL | Five rounds in both orders expose rider-first pre-Prepare stale Standard and partial movement/stall. |
| A05 callbacks | PASS |36 native representative actor/preparation measurements on7, with effects/order/resource observations. |
| A06 split/cancel/interruption | TODO | R's partial Stop retains delivered movement even with command `IsStarted=false`; full matrix remains open. |
| A07 step/restrictions/endpoints | TODO | Endpoint movement observed; step, forced movement, restrictions, autostop and opportunity behavior remain unqualified. |
| A08 relationship/session | TODO | Real removal/record cleanup and lifetime components pass; dismount/re-pair and different-campaign native cases remain open. Unsupported combat remount is excluded. |
| A09 TB-RT-TB | TODO | No spent-allocation mode-conversion trace. Fixture teardown is not this gate. |
| A10 final candidate | PASS | Exact preview10: HH unmounted 2/0, II RT 9/0, JJ TB ordinary 19/0, KK Mammoth 1/0 and LL party 1/0; 32 cases and 287 total assertions, 0 failures. |

Same-campaign mid-combat load has no supplemental-debt persistence/rebinding. Removing obsolete entity references does not restore owed metadata onto newly loaded entities. That Chunk5 save-integration dependency remains explicit; cleanup is not persistence qualification.

Final A10 source/version/hash identity is below. Ordinary/full bow and native Single/Primary, Rapid Shot off/on smoke, mixed bow/Bite range rejection, prediction purity, same-target continuity, RT melee/ranged/Horse approach, paused controls/Stop/selection, unmounted controls and Mammoth smoke pass the unchanged strict scenario validators. This is native integration, not a new human-play or cold-load claim. HH records two originally idle party members still in combat after the targets are removed, restores their exact idle state through existing native cleanup, and then observes parent mode restoration/completion. All cleanup occurs after measurement. This fixes the evidenced fixture timeout and does not qualify A09.

Historical failures remain intact:

| Runs | Result and disposition |
|---|---|
| A / B | A fails subminimum navigation before moves; B fails serialization/party cleanup but retains the causal three-round callback measurements. |
| C-D-E-F, preview3 | Each46/0 trace coverage; no completed allocation gate. |
| G / H / I | G46/2: bare buff lacks FX defaults. H48/2: five measured rounds, wrong timed-buff collection for Prepare. I46/2: feature template sought in the wrong collection. R/S/T/U qualify the corrected native feature probe. |
| V, preview7 |19/0 TB cases,64/0 assertions. |
| W, preview7 | FAIL17/1: operator selected legacy phase3g RT, not the actual accepted phase3h scenario; first longbow ends3/4. |
| AA, preview7 | FAIL21/1: seven rows pass, then128 temporary HP are exhausted and target-invalid interrupts Horse2/3. |
| BB / CC, preview8 | BB RT9/0 (54/0 assertions), CC TB19/0 (64/0). Bounded4096 temporary HP fixes repeated RT target durability; default128 remains elsewhere. |
| DD / GG, previews8/9 | Both16/1 parent timeouts despite two passing unmounted rows/tranche cleanup. GG proves target/Horse removed, game unpaused, actualTB true versus expectedfalse. HH regresses the scoped idle-party cleanup fix. |

The RT durability lease is acquired before measurement and its exact modifier is removed during cleanup. No health refill, target-invalid suppression, forced attack mode or attack/range/prediction repair was introduced. Parent observation rejected paused teardown and Horse-removal theories before the fixture cleanup repair.

## Final candidate, checks and restoration

Binary source `c804ba052760063f747cde83265e660916984d72`; version `0.1.0-chunk2-preview.10`. Later documentation/publication identity is separate: the current Git HEAD identifies the documentation revision, and the campaign receipt records guarded remote verification.

| Artifact | SHA256 / identity |
|---|---|
| Private package `KingmakerMountedCombat-0.1.0-chunk2-preview.10-idle-party-cleanup-diagnostic.zip` | `398460f7772a53b4529c42ff3bd8aedd0ecee86ae29f69c45730f24a99206332` |
| Manifest | `993933189a4a87383a614d94380beb6eddea26b3b454050d54e487f813fbb5e1` |
| DLL | `98d71a590068c4d8bdadddff8350187288108c3aeba2efdf72235c021b92452b` |
| DLL MVID | `fd087322-de6f-4130-96e6-4dafc3a51220` |
| Fresh suite10 snapshot | `96468720a242f20edd2021e9cde135cf0f85e6dd5570ab6c85d3ca8575308c77` |

Local10 checks PASS/0: COMPONENT 350, source 22/build, ASSEMBLY CONTRACT 461 (437 Kingmaker/24 read-only Wrath), visual 23, inventory 10, harness 243, Phase 3G 14, Phase 3H 36, ordinary protocol 39, allocation protocol 28, package 10. No dependency or loader downgrade. The unchanged host guard requires and proves source/package checks and WhatIf purity before each live transaction.

Actual installed preview.13 DLL/cache SHA256 is `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`. Actual intake saves digest `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`; full Mods digest `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`. All completed transactions are independently audited against that intake, never historical preview.7. All 25 transactions audited PASS, 0 restoration failures; latest LL at 2026-09-07T09:02:05.9665477Z, log SHA256 `5a1b49572ce55797ba282eef6b1a466791f99197da461698100368d51929bd87`. No game or live lock remains. Final evidence index SHA256 `cc740726bbe4744abd455dc036755812b3f0b79dd76acb8e0b8d30420926da75` (`analysis-cache/runtime-evidence/chunk2-20260906/final-qualification-index.json`) binds all native/source identities and audits. The exact later documentation HEAD, guarded push receipt and verified remote identity are recorded separately in campaign `ACTIVE-RUN.json`.

## Focused manual checklist for the next grant repair

- Exercise rider-first and mount-first initiative and check when the mount becomes ready.
- Spend a partial path, switch control surface and use the remainder; repeat after exhaustion.
- Compare move-plus-mount-attack with double movement, including the later attack attempt.
- Repeat over three native rounds and check that each actor refreshes once.

Accepted Chunk1 human play is not a prerequisite to repeat. Coordinated activations are the remaining engineering dependency; content expansion stays outside scope.