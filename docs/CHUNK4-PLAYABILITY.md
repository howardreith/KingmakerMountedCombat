# Chunk 4: sustained play and core safety

**IN PROGRESS.** Active new mission on `codex/mounted-combat-phase3f-playable-core`. Reviewed documentation `e6d89bff8c44ecbc21104b733703be9401c3671e`, qualified baseline source `ec5d44e6eddc9839d273176b345f7c9701520450`. The [completed paired milestone](PAIRED-ACTIVATION-MILESTONE.md) is accepted engineering history. No preview.37 human approval is inferred. The owner’s actual Charge failure remains a missing feature; safe rejection does not implement Charge.

Test configuration is paired activation **true**, unified turn/scheduler/overlay **false**. One Horse/Mammoth pair mounted before combat, rider-principal activation, separate native actor budgets, native Full/Single/Primary attacks and current weapon reach. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Persistence is Chunk 5; full Charge and other advanced features are Chunk 6. Neither next chunk is authorized here.

## New gate ledger

| Gate | Status | New evidence and remaining work |
|---|---|---|
| Charge safety | PASS | Preview.6 E/RT and F/TB each 50/0, five cases per mode: both mounted actors reject early; hover/pause/queue remain pure; execution revalidates; real unmounted/unrelated Charge and legal recovery work. Repeat on final candidate. |
| Sustained ordinary combat | IN PROGRESS | G melee RT 39/0: adjacent/approaching held/repeat, three full rider routines each and eligible mount attacks. H/K ranged approach fails after two adjacent cases pass. I/L TB reach later cases; L proves partner paid movement after rider exhaustion. Remaining ranged continuation, six TB activations and interruptions need new evidence. |
| Independent targeting | TODO | Native rider/mount inspection, prepared targeted heals, independent hostile attacks and area effect/defenses. New bounded fixtures are registered, not qualified. |
| Native rider life/death | TODO | Actual damage effects causing incapacitation/death during live commands, survivor visibility/context cleanup and unrelated turns; mount-death regression. Registered, not qualified. |
| Traversal and controls | TODO | Normal door, blocked/narrow route, slope/turn, party movement and supported real area/cutscene cleanup. |
| Encounter/session cleanup | TODO | Representative ordinary session in each mode and three repeated native mount/encounter/dismount cycles with bounded records, contexts, subscriptions and logs. Registered, not qualified. |
| Essential presentation | TODO | Native Horse mounted/unmounted strike/recovery camera comparison and seated motion/countdown/selection; physical input and HUMAN PLAY remain separate. |
| Exact final regression | TODO | All new gates, paired full-round controls in both initiative arrangements, final A05 and accepted A10 with Horse/Mammoth; mandatory build/deployment/package checks and guarded publication. |

## Causes and changes

**Charge:** exact installed blueprint `c78506dd0e14f7c45a599990e4e65038`, logic `Kingmaker.UnitLogic.Abilities.Components.AbilityCustomCharge`. New A/RT and B/TB demonstrated unsafe acceptance and genuine action expenditure without Charge movement. The pair-local policy rejects this exact unsupported path before command creation/approach/start, with “Charge is not yet supported while mounted.” Native query, ordinary input, queue and execution checks preserve all unrelated abilities/actors and any costs already executed. Installed Call of the Wild replaces the target query, requiring restriction of its final result; no foreign mod is changed or required. E/F prove real queued unmounted Charge crosses native Mount and is rejected on the following approach callback before movement or expenditure. Native controls still complete movement, attacks, Stop and TB End.

**Ranged repetition:** H interrupted after three bow deliveries when the fourth native planned bite was correctly outside melee reach. Generic failure cleanup discarded the held RT order. Preview.8 preserves only the measured synchronous ranged-tail termination for a later native-ready routine, retaining the actual Interrupt, full plan, costs and released shots. J’s vanilla unmounted UnitAttack confirms the native four-entry plan and three-delivery range termination. K showed the additional cached `UnitEntityData.HasLOS` restriction disagreed with the native command predicate: cache false, `IsUnitEnoughClose` true. Preview.9 uses the exact `LineOfSightGeometry.HasObstacle(EyePosition, ApproachPoint, GetTargetLOSObjectId())` authority and records both observations. Reach, native timing, obstruction geometry and budgets are unchanged. Fresh ranged approach/repeat and obstruction evidence remain required.

**TB fixture:** I read partner Move before the ground-command terminal sweep. L waits for that real boundary and proves paid native movement `.1408288`, with rider Standard/Move unchanged at 6/3. L then exposed invalid positioning for the next control: the preceding outward step left rider distance 2.55960846 versus reach 2.2096 after Horse Full spent its budget. Native approach interrupts with no rider cost. Preview.9 uses a paid lateral step to retain the next legal melee control; it does not widen reach, grant movement or raise the repath cap.

Preview.9 also registers focused ordinary interruption, native character selection and repeated-session cases in the existing harness. Native projectiles retain their original target and actual RuleAttackWithWeaponResolve completion. Life and session stimuli use labelled native damage effects, never death flags, resurrection, replenished targets or fixture cooldown resets. Inspection observes the real character-sheet binding through native group selection; it does not assign the sheet’s actor directly.

## Evidence and identities

**12 new native transactions A-L.** All retained failures remain immutable. A-D are the focused Charge discovery/compatibility/observation history in [the journal](../MOUNTED-COMBAT-JOURNAL.md) and report history at `a1a6b737`; they do not qualify passing gameplay. E/F, G and J are new passing evidence, not replayed preview.37 results.

| Run | Candidate source | Native assertions | Scope/result |
|---|---|---|---|
| E/F | preview.6 `a2f89b3a397e54cc94c05d09088f6f96cec0c5af` | 50/0 each | Charge safety RT/TB, five cases each PASS |
| G | preview.7 `b915102eb74883ee15a83d05ff312f6b138b29c1` | 39/0 | Four sustained melee conditions PASS; 12 rider deliveries/resolutions each, pure repeated clicks, native cadence and Stop |
| H/I | same preview.7 | 47/2 each | Ranged continuation / TB observation FAIL; two earlier cases each PASS |
| J | preview.8 `a1a6b737ce921a31f0379640fb4c74daf48ba094` | 46/0 | One unmounted native mixed-range routine PASS; not three-routine qualification |
| K/L | same preview.8 | 47/2; 48/2 | Ranged visibility mismatch / TB next-control range FAIL; two and three earlier cases PASS respectively |

Evidence: `runtime-evidence/20260908-chunk4-{A..L}` and independent receipts in `analysis-cache/chunk4-native`, outside Git/packages. G’s measured subscription count stayed 3 unique/16 interface entries and actor records 0 after four encounters; that alone is not repeated mount/dismount smoke.

Frozen preview.8 private diagnostic ZIP SHA256 `b550afee89c9442a8ba5803a5f8adbe83f05df442f9d4405650dc28e3d80b2bc`; DLL `d2d5e7626b74113aeaebe1f95f096a475cf6dabf7d403c87d9e4704434216033`, MVID `913dd8c9-f521-4859-a287-e1fd6d783d93`. Preview.9 is the next unqualified candidate; exact eventual commit/package/suite identities are recorded in the journal and `analysis-cache/chunk4-native/ACTIVE-RUN.json` before launch.

**COMPONENT:** preview.9 build/source22/0, deterministic runner374/0, sustained protocol106/0, extended protocol271/0 including full artifact dispatch, core110/0, Charge86/0 and harness247/0. **ASSEMBLY CONTRACT:** 490/0 against installed game SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`. **NATIVE INTEGRATION:** only the actual runs above. **HUMAN PLAY:** earlier preview.13 feedback retains its scope; preview.37/current-candidate approval and physical-input/visual checks remain pending. Desktop capture approval timed out; no replacement automation system was built.

## External state and targeted manual review

All **12 actual-intake restorations PASS**. Latest L receipt `2026-09-09T04:29:01.2205663Z`, retained game log SHA256 `24ba8ad4edd6a096185cc347a0d8f01fd992b479c702518cd4eb4412a1684c11`. Complete saves digest `bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`; complete Mods digest `02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878`; UMM Params `dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`. Actual preview.37 DLL/cache remains `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`. No game/lock remains after L. Preview.13 is a separate backup. No permanent deployment, protected-save edits or foreign-mod changes are authorized/performed.

Targeted HUMAN PLAY after native gates: ordinary mouse Charge feedback and recovery in both modes; held/repeated melee/bow orders and deliberate retarget/Stop; rider/mount sheet and targeting usability; unobstructed seated Horse strike/recovery and native countdown; normal door/narrow route and party selection. These checks remain pending and do not replace the mandatory native gates. Continue Chunk 4 automatically; draft only the later Chunk 5 roadmap after completion.
