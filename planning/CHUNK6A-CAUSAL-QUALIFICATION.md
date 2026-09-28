# Chunk 6A causal qualification repair

Status: IN PROGRESS. Owner instruction received 2026-09-26.

Intake branch: codex/mounted-combat-phase3f-playable-core.
Intake HEAD: 14966a7c65921797395ecbc4a5b7f9c0c631f991, a clean descendant of
requested 0a92076da39515b40203ff7b8f7dfc9ef311f5af. Preserve all descendants.

The positive approach combined geometry from before a compensated Mount with
the successful transition of a later Mount. It does not qualify CM02-approach-arrival.
The acted boundary must be observed for the exact command, never inferred from
cooldown endpoints. All existing run artifacts remain unchanged.

Required order: split positive and compensation RT/TB; exact rider selection before
baseline; immutable command/shell/process/context identity; native callback resource
windows; separate exploration Mount/Dismount; retain failures and correct unsupported
ledger claims; focused regressions and complete offline umbrella; guarded publication;
new immutable package and unchanged fresh purity proof; positive approach; isolated
compensation RT/TB; geometry change; obstruction; full RT and fresh TB; every mandatory
6A row on one frozen candidate. No 6B before that gate, no PR/merge/tag/release/permanent
install/HUMAN PLAY acceptance.

At intake no Kingmaker or project proof process was running. Locate and preserve
prior purity receipts; do not infer a proof result from absence of a process.

## 2026-09-27 reaction-resource observation contract

Pinned Kingmaker Assembly-CSharp SHA-256 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, MVID 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7. Bounded local inspections are in analysis-cache/chunk6a-causal (UnitCombatState, UnitCombatCooldownsController and TurnController); no proprietary implementation is committed.

- Discrete allowance: UnitCombatState.AttackOfOpportunityCount, getter/setter 0x06009377/0x06009378. It does not decay with time. Native AttackOfOpportunity 0x060093A1 can consume one; on the first reaction of an allocation it sets Cooldowns.AttackOfOpportunity to approximately 5.4 seconds. The conservation windows declare no permitted opportunity consumption, including consumption later hidden by a refresh.
- AoO cooldown: Cooldowns.AttackOfOpportunity, getter/setter 0x0600C3BC/0x0600C3BD. UnitCombatCooldownsController.TickOnUnit 0x0600934A owns normal RT decay. If waiting for initiative, the tick decreases only the initiative cooldown. Otherwise it decreases the AoO cooldown and, at zero, restores the allowance to AttackOfOpportunityPerRound only when the maximum is positive and the old allowance is no larger than that maximum.
- TB tick branch 0x06009349: active combat ticks do nothing except during native passing time. Passing first consumes a nonsurprised unit's positive initiative delay, then decays the other timers with the remaining delta. It does not replenish the discrete reaction allowance. Out-of-combat units take the RT branch only while passing.
- Initiative ordering is UnitCombatState.Initiative (integer getter 0x06009379), distinct from Cooldowns.Initiative (float getter 0x0600C3B4). Ordering must remain exact throughout every measured window. A cooldown change requires the observed native tick or declared preparation; elapsed wall time alone is insufficient.
- Cooldowns.Clear 0x0600C3BE clears both cooldowns but leaves the discrete allowance and ordering intact. TurnController.Prepare 0x06000C3C performs that clear, then conditionally restores the discrete allowance using the same positive-maximum/old-allowance condition. Only the declared pending partner preparation is permitted; no rider preparation is allowed.

Schema 30 retains the accepted command, selection and exploration structure. The allocation trace observes exact tick and opportunity entry/exit only during a command window. Producer and external validator replay ordered native event pairs and command-boundary samples for both actors. Missing callback ends, unexplained intermediate changes, consumption/refund/refresh without an allowed event, changed ordering and undeclared clears/preparations fail. Native event clock/phase metadata is evidence, never authority to write a resource. No production resource write was added.


## 2026-09-27 - released diagnostic observer

IN PROGRESS: c6a-reaction-a-approach on preview.111 reached cleanup, then its liveness sampler dereferenced the released allocation trace and prevented exact command evidence export. All external restoration PASS; no native qualification credited. Preserve its unchanged purity PASS and overall native FAIL. Preview.112 adds a cleanup guard and first-exception evidence, with source and compiled-entry regressions. Exact identities and assertions: [Chunk 6A report](../docs/CHUNK6A-COMBAT-MOUNT.md). Fresh immutable candidate/proof is required before another positive run; no inference of callback qualification from the UMM log.


## 2026-09-27T18:50:25.941Z - geometry approach evidence

Preview.115 positive/RT compensation/TB compensation all PASS with exact restoration;7/87 only on that payload. Preview.116 adds isolated chunk6a-geometry-change: native observed TickApproaching before IsActed, actual rider motion, separately declared exact Horse UnitMoveTo and pre-attachment Deliver geometry. Pinned RT UpdateCooldowns performs no writes for IgnoreCooldown; exact auxiliary callbacks must preserve every action/reaction/initiative field. The default positive contract still rejects auxiliary commands. No resource or position writes are introduced. Native qualification pending new immutable candidate/fresh proof; old Stop-based obstruction cannot qualify actual obstruction. See docs/CHUNK6A-COMBAT-MOUNT.md and lab NEXT-CASE-AUDIT.md for pinned method tokens and bounded findings.


## 2026-09-27T20:24:29.262Z - preview.117 geometry observer repair

**IN PROGRESS.** Published parent 0a70c9ef0ba01bcabbc669449cdc00ace693d08d on codex/mounted-combat-phase3f-playable-core is preserved. Preview.116 completed its unchanged purity proof (log SHA-256 9e3543a7ef563914e31613d32c014af9c561aa4a835de593d048693c96428c64), positive approach PASS48/0, isolated RT compensation PASS46/0, and isolated TB compensation PASS47/0. All exact identity/action/reaction windows and all five external restoration checks passed. Its separate geometry run c6a-geometry-a-geometry remains FAIL44/2; no provisional PASS from it qualifies a row. Exact original artifacts and assertions are retained in docs/chunk6a-evidence-history.json and lab settled records.

The failed mandatory CM02-geometry-change assertion was: "The ground order missed the exact uncommitted non-adjacent Mount approach." Evidence SHA-256 8e7011393575a44b355388fc5aac30571dd1e1e984a535bc85da1c64c9c28257. The rider was actually approaching outside the 2.9m envelope (distance6.183507m, command -702943232, unacted/unfinished). The diagnostic used UnitCommands.Move, a UnitMoveTo-only convenience getter; Mount occupies the Move slot as UnitUseAbility. The existing positive probe already uses GetCommand(CommandType.Move).

Preview.117 makes that same accessor correction and records the exact trigger/slot before refusing. No production behavior or resource write changes. A detached pinned-native component reproduces the accessor behavior, and its compiled caller check fails116 then passes117 (6/0). This is an offline regression, not Unity qualification. The accepted selection, causal identity, exploration windows and action/reaction accounting structures remain intact.

Kingmaker is closed; actual Steam guard passed client1920 with current-session offline/cloud evidence. Completed116 proof and all four native runs were rehashed and retained. Ledger116 records7PASS/1FAIL/79unqualified and seven retained mandatory failures; it qualifies only116. Source117 supersedes116 for further qualification and has0/87 native rows. Next full offline umbrella, guarded publication, immutable117 candidate/suite/fresh unchanged proof, then positive, isolated RT/TB and geometry before actual obstruction/fullRT/freshTB/remaining87. No6B before every mandatory6A row on one frozen candidate. No merge/release/tag/PR/permanent install/HUMAN PLAY inference. Final mission target remains ENGINEERING COMPLETE - OWNER ACCEPTANCE PENDING.


## 2026-09-27T22:18:07.328Z ? preview.118 genuine obstruction instrument

Published parent a3b6061 is preserved. Preview.117 geometry now PASS45/0: one exact Mount -757525504 approached from6.25955153m, target moved2.95288634m, pre-attachment arrival2.845683m inside2.9m, native Success with exact action/reaction proof. EvidenceSHA166c5e9cc8357801f4523e3b72d998fa50cbfbd7719566c59d9f3ef9d5b06deb; package61820b0107afa69457c915ac16325cc20738e3d779bedc6c9341b292bee04faf; DLL a1edad7b3067f4906d545f46f594617779b9a7c994ba96fb2279ff27e6058a99/MVID f2cc1bdd-46db-4bf9-aa99-a6869e68c504; suite20260927-chunk6a-geometry-slot-a/fe9644165293dd421a9817baca5b55b80ce6319c78077349e359ec31d8194e90. All four117 native cases/restoration PASS. Completed117 purity log856e0d12a512591c4705d24372a6155dc4bed711d4988e96f4984f5a36dd4a07 stays exact. Ledger8/87 applies only117.

Source118 replaces Stop-as-obstruction with a real closed StandardDoor fixture and read-only exact native path observer, using a separate pre-acted negative contract. Both reaction fields and initiative ordering/cooldown remain event-backed; positive strictness is unchanged. Focused causal208/0; native observer signatures6/0, five new wrappers construct, PathTo desktop ECall construction DEFER pending Unity. Production unchanged. No native118 result exists. Next full offline/publish/candidate/fresh proof.


## 2026-09-27T23:57:09.604Z - preview.119 path observation and fixture repair

IN PROGRESS. Branch codex/mounted-combat-phase3f-playable-core, published parent4322f7f89a84027a80320ab1d53411468a62e60a preserved. Preview118 completed its unchanged purity proof (exit0, raw log SHA2566561d40dde73bead1a9e8ef292a0a312a13d1459fbdb5cf9c14d8201c5114933); never rerun it. Candidate package1fa293f66b34f5f34db43ffa3871fee7fa0cc6d9d12f5184c33db2e68de4f991, DLL39206e1e05f6de41e3ec76cb0195276d862203d0c12fc3f34b13757338c50c5a/MVID44696e8f-fa25-4ee3-a77e-d682a094874a, suite20260927-chunk6a-obstruction-a/SHA2632085ec7c422dda81702aa70605f0f8e2b00243184e07d499097fdbfa48e89.

Positive c6a-obstruction-a-approach PASS48/0; compensation c6a-obstruction-a-rt PASS46/0 and c6a-obstruction-a-tb PASS47/0. All exact command/action/reaction windows and all five external restoration checks passed. Ledger118 has7/87 mandatoryPASS.

Geometry c6a-obstruction-a-geometry remains FAIL43/2. Its exact mandatory CM02-approach-arrival assertion starts "Pre-encounter native separation did not succeed:" and includes the complete captured command/geometry JSON; the full original string is retained verbatim in docs/chunk6a-evidence-history.json and the ledger, bound to runtime-game-result SHA8f0204a00754d1b347406d108fbb4fec3aa72ee7d0193e040cf2c3874f30c909. The setup Horse UnitMoveTo486617088 ended Interrupt0.526910067m from its destination (native approach0.3m). No combat allocation or combatMount request existed. The recorded endpoint alone cannot identify the exact route failure. Do not rerun unchanged or credit the provisional exploration rows.

Independent obstruction diagnostic c6a-obstruction-a-obstruction remains FAIL43/2. Exact CM02-obstruction assertion: "InvalidOperationException: Native door setup did not prove terminal arrival and its required open-door crossing: riderNearArrival". Runtime-game-result SHAd5562910f8a03af5d627cc9a60eff44015c0e860180d7481ed8db96c4aeb5d51. All five new wrappers installed in Unity, including previously deferred PathTo. Horse crossed the open door with nativeSuccess; rider also arrived with nativeSuccess. The request-time observer enumerated the worker-owned vectorPath and threw "Collection was modified; enumeration operation may not execute." No closed-door Mount was attempted. This is instrumentation failure, not a product obstruction outcome. Door and all five external restoration checks passed; Kingmaker closed. Exact five settled runs and checkpoint-preview118-after-obstruction.json remain in the lab; nine failed mandatory rows retained.

Source119 records only path object/request/command/destination at PathTo and reads contents at completed callbacks. External validation rejects request-time contents. Compiled producer regression requires absent contents at four unsafe boundaries and immutable completion snapshots after list reuse. Bounded ground setup additionally requires native route reachability, the Horse's full endpoint footprint and actor clearance, and now records its exact path callbacks. Original nativeSuccess requirement,30-second bounds, positive identity/cost checks and selection-before-baseline remain intact. This strengthens fixture selection; it does not claim the old separation cause is proven or alter production movement/resources.

Focused component551/0, causal211/0, snapshot5/0, source112/0; buildPASS. Next final build and complete offline umbrella, guarded publication, new immutable119/suite/fresh unchanged proof. Then positive, separate compensationRT/TB, geometry, obstruction, fullRT/freshTB and remaining87. Source119 has0 native qualification. No6B before all mandatory6A pass one frozen candidate. Final mission target ENGINEERING COMPLETE - OWNER ACCEPTANCE PENDING. No merge/tag/release/PR/permanent installation/HUMAN PLAY inference.


## 2026-09-28T01:48:01.223Z - exact native unacted Mount interruption

Pinned Kingmaker Assembly-CSharp SHA2563b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb / MVID07fa1e4d-8618-41b3-9b8d-faa17d3b26f7. OnMovementInterrupted(0x0600184F) uses the UnitMoveTo-only Move getter0x0600269F; GetCommand(CommandType)0x060026A9 observes the base slot. Native Interrupt0x060027AC owns ResultInterrupt and calls OnEnded0x060027B2, which owns completion; neither charges or marks acted. Preview119 c6a-path-snapshot-a-obstruction directly observed the exact unacted UnitUseAbility left pending after this callback. Source120 adds prefix capture/postfix same-command revalidation and shell retirement before native Interrupt, with no resource writes. Detached ownership negatives24/0, native assembly629/0 and wrapper constructionPASS; actual new callback execution remains a Unity gate. Full cause, original assertion and artifact hashes: docs/CHUNK6A-COMBAT-MOUNT.md and docs/chunk6a-evidence-history.json. Observation caps, deadlines and qualification assertions unchanged.


## 2026-09-28T03:23:07.787Z - obstruction envelope and external result facets

Preview120 directly observed NativeMountedControlService's exact unacted interruption callback in Unity: command381715712, same shell, nativeInterrupt/OnEnded at frame1946, zero cost/transition and preserved action/reaction window. Overall qualification nevertheless FAIL: Assert-KmcChunk6aObstruction used corpulence +1.0 instead of the frozen CombatMountDismountPolicy.NativeAdjacentReachMeters1.5. Source121 fixes only the validator formula; real Horse corpulence0.9 plus rider0.5 must derive2.9. Source114/0 and causal216/0 regressions retain the same tolerances and closed-door observations; the historical parser replay never changes120's overallFAIL. Exact original artifacts and both native/external result facets are retained in docs/chunk6a-evidence-history.json. New121 requires fulloffline/newcandidate/newproof/nativequalification.


## 2026-09-28T07:59:05.791Z - exact TB approach observation (preview.123)

Pinned Kingmaker Assembly-CSharp SHA256 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, MVID07fa1e4d-8618-41b3-9b8d-faa17d3b26f7. Local bounded inspection only; no proprietary source copied into Git.

| Native boundary | Observation / confidence |

| --- | --- |

| UnitCommand.TickApproaching 060027A6 | TB first uses ForcedPath or CurrentPathForUnit for the executor view. The preview branch copies vectorPath into a caller-owned ForcedPath, then calls FollowPrecomputedPath with ApproachRadius. A distant endpoint is logged but still consumed. Exact local body; actual123 callback pending. |

| PathVisualizer.CurrentPathForUnit 0600700F | Returns the current actor preview when view ownership matches; does not validate this command target. Exact local body. This permits the stale-preview hypothesis but does not prove122 consumed it. |

| UnitMovementAgent.FollowPrecomputedPath 060018B6 | Synchronous caller-owned consumed-path boundary; immutable copied points and exact agent ownership may be observed safely. Desktop wrapper PASS; Unity execution pending. |

| UnitMovementAgent.PathTo 060018A3 | Direct request branch records only receiver, command and destination; worker-owned requested path points remain unread. Desktop wrapper DEFER - EVIDENCED (Unity ECall); Unity execution pending. |

| TurnController.TickMovement 06000C37 | Native accepted ref delta changes TimeMoved and MoveAction. Capture prefix/postfix, exact turn/slot/command and all action/reaction fields; independent replay requires continuity to acted cost. Desktop wrapper PASS; native123 execution pending. |

Normal UI IgnoreClick validates current prediction/point. The diagnostic selected-ability path currently calls SetAbility then OnClick without hover/prediction settling. Do not clear, force or write a path based on this hypothesis. Source123 observes first; retain122 failure and unchanged30s deadline.

Positive TB path evidence requires exact native getter and consumed or requested path callbacks for the one admitted unacted Mount. Capture before click, bind the actual Move command, retain failure evidence before cleanup, then unpatch. Preparation, reactions and the exactly-once acted Move cost keep their existing stricter event contracts.
