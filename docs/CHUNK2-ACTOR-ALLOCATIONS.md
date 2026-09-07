# Chunk 2: Actor allocations and movement conservation

Status: **BLOCKED — CRITICAL** for milestone completion. Narrow preparation, pacing and record-lifetime repairs are implemented; the existing pre-Prepare delegation contract still cannot establish a legitimate actor grant. The global-round allocation key remains unchanged. No complete A01–A09 or coordinated-activation claim is made. A10 final results: IN PROGRESS. Preview8 addresses the evidenced RT target-exhaustion fixture defect; final native checks remain open.

## Accepted baseline and actual installation

Integration branch `codex/mounted-combat-phase3f-playable-core`; reviewed `b3f063337644215312de97d9736892212777ac1c` and legitimate intake descendant `aa0bdc41110923a0aae3bd1ac49322e5f2b75c02` are preserved. **OWNER-REPORTED HUMAN PLAY** on installed/enabled Chunk 1 preview.13: TB ordinary/full bow attacks, Horse approach/attack, no extra move in the tested rider-to-Horse switch, pure hover, and RT combat/controls/mount/dismount worked well. This does not certify exhaustive allocations, precise Rapid Shot modifiers, visible Bite recovery, formal charge or cold load. [Human installation/play record](CHUNK1-HUMAN-PLAY.md); [frozen Chunk 1 engineering evidence](CHUNK1-ORDINARY-ATTACKS.md).

Actual host: DESKTOP-SRJJ623; Kingmaker 2.1.7b, UMM0.28.2, Harmony12 1.2.0.1. All three experimental flags remain false. Actual preview.13 source is `a8745640e18ce068e412b4e360c7b0a3d46c738a`; installed DLL/cache SHA256 `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`. Transactions snapshot current human saves/settings/caches and foreign Mods; historical preview.7 is never restoration authority.

## Repairs and native contract

- **Preparation correction precedes callbacks.** Old matched trace-D (and failed fixture-B's preserved measurements) showed round/AI/readiness callbacks seeing Move0 before the postfix restored delivered mount movement. Reconciliation now enters at exact `UnitCombatState.OnNewRound` after native cooldown clearing/acting-command reapplication and before round, AI, feature and readiness callbacks. No callback replay or transpiler was added. In final rider-R round1, sequences126/127 show Move0 after clear, then retained Move.1449457 before round state. Native feature observations135/136 retain that expenditure and heal damage6→5; readiness and final preparation see the same cost.
- **Delegated movement preserves native pacing.** Old D's matched .75m path cost about.319 action-time units through the rider versus.143 under native Horse control. The delegation prefix had skipped native minimum-speed/warm-up/slowdown handling. Exact typed fields now mirror that permitted non-forced movement boundary; native next-tick restoration remains authoritative. R measures .1425–.1449 through the rider versus .1431–.1435 on the Horse at native speed5.08m/s. In mount-first S, raw Move0→.2866 includes reinstating earlier .1435 expenditure plus the new .1431 movement; it is not a second charge. Rider transport adds no rider Move.
- **Actor references have explicit retirement.** Records survive selection/dismount and cannot settle merely because an unprepared owed debit decays. Destruction, settled out-of-combat actors and replacement player/campaign identity retire obsolete references. R/S exercise real allocation records and finish removal/cleanup with zero records. Five service-level lifetime tests cover preservation and selective retirement. Different-campaign native loading and same-campaign debt restoration are not qualified.

The exact native `Prepare` (`06000C3C`) sequence is: interrupt as applicable; AI force tick; cooldown clear (`0600C3BE`); acting-command cost reapplication; reaction reset; round state (`0600939D`); round handlers; AI round tick; `Unit.Logic` each-round facts; confusion/readiness/UI. Native actors/commands remain the cost authority. Reaction state is observed; a consumed-reaction/opportunity scenario remains unqualified. Exact installed assembly SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` gates the patches with own-patch rollback. Foreign commands/unpaired actors retain native paths.

An actor's next observed native grant begins at its own preparation, not selection, pairing, a global-round number or cooldown decay. A carried path spends mount movement, while native rider attacks retain their own native costs. The supplemental record can reinstate already delivered expenditure at later preparation; it is not another action bank. Native movement uses action time, actor speed and `MetersOfFiveFootStep` (2.286 in this installation); local formulas establish the 3/6 time boundaries. Native End Turn via `Game.PauseBind` forfeits unused actions and is distinct from a delivered debit. No measured turn/readiness/initiative/cooldown rewrite was used.

**Remaining activation dependency:** R's first rider move occurs with mount grant0, `CanActInCombat=false` and five time units until its native turn. R rounds4/5 enter with the previous mount grant, Standard≈5.14494/Move≈2.14494; the rider delivers ≈.85506 time units then stalls. Actual mount Prepare later preserves that debit and allows movement through Move6. S exhausts native movement in rounds4/5 and subsequent rider input moves0 despite raw cooldown decay. These two orders distinguish retained expenditure from entitlement. No legal early reservation against an exact upcoming allocation has been proved. Merely changing the key or clearing the old Standard would invent a grant. Restricting all transport to the mount's current turn would lose the accepted carried-movement-plus-rider-action sequence unless preparation/continuation ownership is coordinated. The remaining Chunk 3 seam must declare that readiness/grant/continuation contract, then close A01–A09; this is not justification for another attack shell, pooled bank, selector retry or whole-controller rewrite.

## Evidence and acceptance

Stable top-level allocation IDs are `actor-allocation-{rider-first,mount-first}{,-unmounted}-tb` (the actual unmounted suffix is `-unmounted-tb`). Four matched conditions use pre-encounter deterministic initiative inputs, qualified AI isolation, durable targets, three complete native rounds/six short paths, then two further exhaustion/refresh rounds. Native End Turn processes unrelated actors. Input evidence is **scripted native handler/prediction integration**, not physical desktop or HUMAN PLAY.

All run IDs below begin `20260907-chunk2-`. Raw immutable artifacts: lab `runtime-evidence/<run-id>/`; intake, independent audits and compact derived summaries: `analysis-cache/runtime-evidence/chunk2-20260906/`.

| Run suffix | Source/candidate | Result |
|---|---|---|
| rider-trace-A | `5fa84f0`, preview1 | FAIL before moves: subminimum navigation fixture; cleanup timeout |
| rider-trace-B | `b1fff5a`, preview2 | FAIL serialization/party cleanup; three rounds/six moves preserve causal callback evidence |
| mount-trace-C; rider-trace-D; unmounted-mount-E; unmounted-rider-F | `f524624`, preview3 | Each46/0 trace coverage; no gameplay-gate qualification |
| callback-rider-G | `7781827`, preview4 | FAIL46/2 before encounter: bare buff lacked native FX defaults |
| pacing-rider-H | `b015977`, preview5 | FAIL48/2: five measured rounds; buffs heal via a timed collection outside Prepare |
| native-facts-rider-I | `1ec9535`, preview6 | FAIL46/2 before encounter: feature-template lookup in wrong collection |
| native-facts-rider-R; native-facts-mount-S | `a3f6926`, preview7 | Each49/0; T01/T02 coverage and A05 representative native callbacks/effects PASS |
| native-facts-unmounted-mount-T | same preview7 | 49/0 matched unmounted control |
| native-facts-unmounted-rider-U | same preview7 | 49/0 matched unmounted control |
| regression-ordinary-V | same preview7 | 19/0 attack cases, 64/0 total assertions |
| regression-rt-W | same preview7 | FAIL: operator selected legacy `phase3g-native-controls-rt`, not the actual accepted Chunk1 RT scenario. Three paused rows pass; first longbow case ends3/4 then times out. Game17/1; outer validation rejects missing named completion. |
| regression-rt-AA | same preview7 | FAIL: correct phase3h RT scenario; seven rows pass before target temporary HP128 is exhausted. Horse completes2/3 then interrupts on target-invalid (trace461-463). Actual restoration PASS. |

The temporary feature probe clones an existing loaded native feature, replaces all components with native `AddFactContextActions`/`ContextActionHealTarget`, and restores exact owned facts/health after measurement. It does not invoke `OnNewRound` itself. Each measured preparation observes one native clear/round/AI/fact boundary, one-point healing, two distinct round handlers and ten distinct readiness handlers, in order. R/S include an unrelated party actor; native callback floor violations are zero. `T01`/`T02` explicitly set `gameplayQualified=false`. Their PASS labels certify observation coverage, not the unresolved rows below.

| Gate | Status | Scope and remaining evidence |
|---|---|---|
| A01 exhaustion, both orders | TODO | S preserves fully spent movement under both surfaces; movement-plus-attack exhaustion and admission feedback remain unqualified. |
| A02 legal residual movement | TODO | Short carried/native paths preserve debit; complete control-surface switching within the declared allocation remains unqualified. |
| A03 Standard conversion | TODO | Native double movement reaches Move6; matched move-plus-mount-attack and later attack rejection remain unqualified. |
| A04 refresh | FAIL | Both orders traced across five rounds; rider-first pre-Prepare readiness/grant identity remains unresolved and produces partial motion/stall. |
| A05 callbacks | PASS | Both mounted orders and matched unmounted controls pass real feature effects/order/resource observations: 36 actor/preparation measurements over the first three rounds, including unrelated actors. |
| A06 split/cancel/interruption | TODO | R's real partial Stop retains delivered movement although command `IsStarted` is false; full attack/interruption/rejection matrix remains open. |
| A07 step/restrictions/endpoints | TODO | Native endpoint handling measured; step, forced movement, speed restrictions, autostop and opportunity rules are not certified. |
| A08 relationship/session | TODO | Real record cleanup/removal and component lifetime transitions pass; dismount/re-pair and different-campaign native cases remain open. Unsupported combat remount is not enabled or counted. |
| A09 TB→RT→TB | TODO | No full spent-allocation conversion trace; cleanup/mode entry is not this gate. |
| A10 exact Chunk 1 regression | IN PROGRESS | V's19 TB ordinary cases pass; RT/unmounted/Mammoth/party regression pending. |

Same-campaign mid-combat load has no supplemental-debt persistence/rebinding implementation. Retiring obsolete entity references does not restore owed metadata to new entities. That save integration dependency remains explicit for Chunk5; cleanup is not counted as persistence qualification. No A09 PASS is inferred from clearing records or dismounting.

## Candidate, checks and restoration

Frozen measured preview7 binary source: `a3f6926a4a2914f30b49bfb956e72f73e607cb16`; version `0.1.0-chunk2-preview.7`. Later documentation/publication identity is separate.

| Artifact | SHA256 / identity |
|---|---|
| Private package `KingmakerMountedCombat-0.1.0-chunk2-preview.7-native-feature-template-diagnostic.zip` | `344b894f52b68ef3bfb0eaf9ff87bc6b22913ac082dc0cd932bf18162d3a674a` |
| Package manifest | `d1cb0d38067414f3a7f01fe2b977b7e9483151b0fe34a308d1b9686fe8379c10` |
| DLL | `446d65c2f6530cbeba0495edd394a0a1d7b47b1ddf014306428f0c9d9eea5d34` |
| DLL MVID | `8593f401-364d-4ee3-93d9-73e58400d1c4` |
| Fresh suite7 snapshot | `0cc3eda86f99fca30d1d77fefa79eb738b1978797d4d795fc209e48b28875c6e` |

Frozen preview7 local checks PASS/0: COMPONENT350; source22; ASSEMBLY CONTRACT461 (437 Kingmaker/24 read-only Wrath); visual23; inventory10; harness243; Phase3G14; Phase3H29; ordinary protocol39; allocation protocol28; package10. New hooks use exact local contracts, no dependency or loader downgrade. Earlier build/fixture failures are retained with their corrected reruns.

All completed transactions independently restored the actual intake. All16 completed transactions audited PASS through AA at 2026-09-07T06:53:46.4966948Z. AA log SHA256 `28b7e21d7430da1225a4b5f986bd63f8d8aa7ba1922019ad42f9736891941f24`. Protected saves digest `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`; full Mods `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`. No permanent install, main merge or release. Guarded publication: TODO.

Preview8 changes only the repeated RT diagnostic target lease to4096 temporary HP before measurement, preserves default128 in other scenarios, and removes the exact owned modifier at cleanup. Schema10 requires that exact provisioning evidence; historical schema9 remains valid. No damage refill, target-invalid suppression, attack-mode forcing or attack/range/prediction change is introduced. Next: freeze source/package, run the proper RT regression first, then exact-candidate A10. Preserve matched native allocation evidence on7 separately:8 changes only the repeated RT fixture branch. Full local8 suite passes (same counts as7 except Phase3H36).

## Focused manual checklist for the next grant repair

- Exercise rider-first and mount-first initiative; confirm exactly when the mount becomes ready.
- Spend a partial path, switch control surface and use the legitimate remainder; repeat after exhaustion.
- Compare move-plus-mount-attack with double movement, including the subsequent attack attempt.
- Repeat across three native rounds and check that each actor refreshes once.

Accepted Chunk 1 owner play need not be repeated as a prerequisite. Coordinated activations are the next engineering dependency; content expansion remains outside scope.
