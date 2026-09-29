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

2026-09-28 source127 ground eligibility: exact native BaseUnitCombatController.ShouldTickOnUnit06009343 requires living and IsInCombat; inherited TickUnit0600910C returns when false. PinnedMVID07fa1e4d, IL SHA768ca06c90b9d6989de327f8f8537d30aae9e92e3fef922a78929a5407b215cc and1b8d3a4b24c1e493b805d644238031e5396d44b59abb59724b1dce577718b8a5. Native126 TB fixture lacked this observation; no product adoption occurred. New diagnostic hook retains exact controller/actor/clock/result and resource invariants without writes. Desktop wrapper and synthetic proof PASS; actual Unity skip callbacks pending newcandidate. Bounded local IL remains analysis-cache/logs only.

## 2026-09-28T21:34:35.318Z — preview127 closure and source128 diagnostic repairs

**IN PROGRESS.** Parent/published HEAD c47bfbc9b6edba1c2fb97c6cfa77e1eccf49f68b on codex/mounted-combat-phase3f-playable-core preserves f05a265. Frozen127 is superseded by source128. Original127 purity PASS exit0 at2026-09-28T21:07:11.2549094Z, logSHA095f36fd14340247ceabf277771ad7859f73f0d67134f7249b8e6fd906455daa; never rerun it. Package77e3a990975afd44eb1bcdc8b3db9d684e3814d0ef8879b5c86bcde61c49bdf6, manifestb972f1eaa3f366675f45eec21b1f7e066d722d392cb20ea2129c1d5350b46879, DLLa57d9832aaea961e746afa84094467baf0f18cde01987013763f7041648490d3/MVID4cbf2c5f-97d8-44f6-82a7-f19a61cce0fa, suite20260928-chunk6a-skip-a/SHA425bf7c3e0c6a24cd1340e0ba57c5976ed6f0d0a9266093a93229e236a1fa451 remain immutable.

127 positive c6a-skip-a-approach PASS48/0 (gameSHAe183247ab67d973f411797e79afe18c668e20812e788701323cf0640ba2f86f9), RT compensation PASS46/0 (gameSHAe87f6576bf4238f2a82abc14f3742cd5cc24d68eadfe3c60dd668eef9d56deef), TB compensation FAIL43/2 (gameSHA8863856759cd45d851fb8631144a89565036189e491748c30875b6f4f6d547b9, artifactSHAbaf6d2d1661f8c68fb442b1f1677dea9f67de4b09ce2c1905955274995acb384). All five restoration checks PASS for all three runs. The ordered queue stopped before geometry. Historical ledger4PASS/1FAIL/82unqualified;23 original failures retained with exact assertions. No failed-run provisional PASS credited. Positive exact command833457920/process-146811392/context-500697088,6.251725m to2.82768822m within2.9m; all eleven boundaries once, actual acted and one native Move cost. All six Unity observers executed. Separate exploration Mount/Dismount action/reaction windows PASS. Carried forcedDetach=1 and duplicateSuppressed=1 each added zero in the positive window.

TB fixture failed before any combat allocation/adoption: phase3d-horse-runtime-exception, exact assertion InvalidOperationException: Native ground plan: in-flight path content read. Ground command1386049024 succeeded within0.0380457677m; actual eligibility callbacks148before/148after and action/reaction resourceWindow passed. Exact127 DLL reproduction showed pathState is a null-valued String JToken, serialized as null. Source128 constructs JValue.CreateNull for unavailable pathState. Strict path validation and all deadlines/thresholds remain. New direct in-memory regression FAIL on oldDLL, PASS on newDLL; serialization-only coverage had missed this distinction. Original failure remains unchanged.

Source128 also integrates mandatory CM06-ai-auto-use fixtures as separate fresh RT Mount/Dismount transactions, six read-only native UI/AI/constructor observers, exact resource/reference/UI restoration, canonical unchanged artifact-manifest guards, strict legacy JSONL projection and primary-claim binding. Unsupported mandatory claims cannot borrow unrelated PASS rows. Existing accepted compensation/positive stages are unchanged. Initial source guard failure retained; read-only stage comparison proved all prior dispatch semantics unchanged before exact route spelling was updated. No production resource write, new content, guard weakening or acceptance inference.

Release build/source127/0 PASS; DLL75a8972ed1d6ef88b10e704f86ccbe5d741a0d014b7b7b97d643b891616b662a/MVID225893fb-b686-433b-a3ad-3e46c8db77e9. New source remains unpackaged and unqualified. Focused gates and complete offline umbrella pending. Game closed, no proof/native process active. Steam guard exactclient1920/current-sessionofflinecloud PASS. Next finish gates, coherent commit/guarded publication, new immutable128 package/suite and fresh unchanged proof, then positive, isolated RT/TB compensation, geometry, obstruction, fullRT/freshTB and remaining mandatory6A. No6B before87/87 on one frozen candidate. Target ENGINEERING COMPLETE — OWNER ACCEPTANCE PENDING.

Focused source128 gates PASS: live path boundary17/0 and desktop patches30/0; auto-use3796/0, case848/0, envelope60/0, primary claims1148/0, shared manifest11/0, legacy reader47/0, row reader22/0, full legacy ledger7/0, composite registration68/0, original evidence history864/0 and ledger consistency87/0 with23 failures retained. All six auto-use signatures and IL hashes match the pinned engine and all six wrappers construct. DLL5069312 bytes under unchanged5242880-byte cap. These are offline contracts; native128 remains unqualified. Complete offline umbrella is next, with source frozen until its actual exit receipt.

## 2026-09-29 - preview128 closure and source129 preparation ownership repair

**IN PROGRESS.** Branch codex/mounted-combat-phase3f-playable-core; published parent aec84a95cf058b32aec10bb14718ecde63fd2e6b preserves f05a265. Source129 supersedes frozen128 and has no native qualification. Original128 purity PASS exit0 at2026-09-29T00:37:04.3208987Z, logSHA d0528188c1b5cc9e764edd35a90e5c2c02ad189bbde08ca3554af00b2dee2c45. Never repeat that proof. Package00557bbb35fab7249755e8f710884ccad2dc74a7ae990f10a03a83e9848e084a, manifestbdeef2dc8536ad3cbca555a853f34e85781f999d741cde22200859140bfbd988, DLL75a8972ed1d6ef88b10e704f86ccbe5d741a0d014b7b7b97d643b891616b662a/MVID225893fb-b686-433b-a3ad-3e46c8db77e9 and suite20260928-chunk6a-token-a/SHA9b96a040215153f8e40a9e859123381234886516d4864fffc4ce17ac2bacbd61 remain immutable.

Seven native transactions restored all five external-state checks. Positive c6a-token-a-approach PASS48/0: one combat command-70198272/process-1789597440/context297400320/shell3/generation1,6.374983m to2.83447075m within2.9m, all eleven boundaries once and all six Unity hooks, actual acted/one Move/nativeSuccess. Separate exploration Mount and Dismount action/reaction windows PASS; carried forcedDetach=1 and duplicateSuppressed=1 each added zero. Isolated compensationRT46/0 and TB47/0, geometry45/0, obstruction45/0 and fullRT52/0 PASS. The ledger retained ten mandatory PASS only from those overall-PASS transactions.

FullTB c6a-token-a-full-tb FAIL46/6 stopped the queue before remaining cases. Native gameSHAfdb25bf53c4a688c9f805c007a74d752eb99bc4d7155d8b5fe5d4e7df20fa15b; scenario artifactSHA55257492878e6debbc67e60205ff1399cce2424b5a4a511f002afdd8fa547122. Exact failures: CM01-combat-mount-accepted, CM03-combat-mount-conserves-debt and CM02-approach-arrival; complete original assertion text and payload identities are in the evidence history. Command1092947712/shell5/process1103652992/context-1102034816 reached every causal boundary and nativeSuccess, but partner prepare-before/after had no Clear and preserved initiativeCooldown4.49999952. Reaction replay reported "mount reaction effect differs from the exact native prepare-after event." Historical ledger10PASS/1FAIL/76unqualified,25 retained mandatory failures plus the associated exact native debt failure. No provisional PASS from that run is credited. Initial pending ledger attribution failures remain in lab logs; no ledger guard changed.

Source investigation found AdoptRunningEncounter reserved the partner grant before calling its private native TurnController.Prepare. BeginPairedPreparation then attempted that reservation again, returned false, and the Harmony prefix suppressed native preparation. Native Prepare06000C3C (pinned engineMVID07fa1e4d-8618-41b3-9b8d-faa17d3b26f7, IL SHA27c769eb310f4bccfa9db38e06a921260696b4d95c382586bea04e2f091af408) calls Cooldowns.Clear once. The narrow repair leaves reservation with the existing native prefix and requires observed partner completion on return. No production cooldown/reaction write, forced preparation of the rider, resource assertion relaxation or accepted positive/compensation scenario redesign.

The compiled-caller/domain regression fails on exact128 with "Native Prepare prefix rejected partner: adoption caller already reserved the same grant (observed caller reservations=1)" and passes16/0 on source129. Release build/source128/0 PASS; DLL2c1b23c6c170d5ad4e37eb0d1a8b2a42a180f481994dcaa4087c475d4864c2cb/MVID5e4cecda-893f-4a1f-bafb-6ef9c2a8b944,5073408bytes under unchanged5MiB. Initial source validation caught the stale BuildIdentity version; that failed log remains, and all three version declarations now agree.

Source129 also integrates the separately tested foreign-companion refusal case and fixed CM08 regression/pointer qualification bindings. These keep every original native body, manifest, candidate, suite, overall-PASS and restoration gate. Foreign ownership is observed without mutation. Integrated focused tests and complete offline umbrella remain pending; no new package, proof or native run exists. Game closed, all128 transactions reconciled. Next finish gates, coherent commit/guarded publish, new129 immutable candidate/suite/fresh proof, then the required ordered native campaign. No6B before87 mandatory PASS on one frozen candidate. Target ENGINEERING COMPLETE - OWNER ACCEPTANCE PENDING; no merge/tag/release/PR/permanent installation/HUMAN PLAY inference.

## 2026-09-29 — preview.129 complete offline gate

**IN PROGRESS.** The complete source129 offline umbrella passed with actual exit 0 at 2026-09-29T02:00:25.1510596Z. Receipt: analysis-cache/chunk6a-causal/offline-process-preview129-integrated-retry1.json; log SHA-256 0c84bf22b03f4aa04b2086f91d1a917dbd194c1d3bafcd2253143c5d1d4bed61. The separate original disk-full attempt remains FAIL with its original receipt and log. The owner freed 132 GiB; temporary synthetic-copy recovery is verified, and native evidence/backups were untouched.

The product repair removes the duplicate partner-preparation reservation from AdoptRunningEncounter; the existing native prefix owns the grant, and adoption requires completion. The compiled regression fails on the exact preview.128 DLL and passes **16/0** on preview.129. Components **553/0**, source **128/0**, history **980/0**, foreign-companion case **702/0**, and the complete reader/ledger/causal/resource umbrella pass. The prepared campaign generator separately passes **213/0** synthetic checks; it creates no qualification evidence.

Source parent and published head remain aec84a95cf058b32aec10bb14718ecde63fd2e6b on codex/mounted-combat-phase3f-playable-core, preserving accepted f05a265. Built DLL SHA-256 2c1b23c6c170d5ad4e37eb0d1a8b2a42a180f481994dcaa4087c475d4864c2cb, MVID 5e4cecda-893f-4a1f-bafb-6ef9c2a8b944, 5,073,408 bytes, within the unchanged 5 MiB limit.

Preview.128 is superseded. Its package, suite, completed purity proof and seven native results retain their identities; all seven restoration records pass 5/5. Its historical ledger is 10 PASS / 1 FAIL / 76 unqualified with 25 retained mandatory failures. Preview.129 has **0/87 native qualification** and no package or proof yet. Actual Steam safety passed against client 1920 with current-session offline/cloud evidence; Kingmaker is closed.

Next: coherent commit and guarded publication, immutable preview.129 package and new suite, one fresh unchanged purity proof, then positive approach, isolated compensation RT/TB, geometry, obstruction, full RT/fresh TB and the remaining mandatory 6A matrix. No 6B before all 87 mandatory rows pass on one frozen candidate. Target remains **ENGINEERING COMPLETE — OWNER ACCEPTANCE PENDING**. No merge, release, tag, PR, permanent installation or HUMAN PLAY acceptance.

## 2026-09-29 - preview129 closure and source130 precise Acting fixture

**IN PROGRESS.** Branch codex/mounted-combat-phase3f-playable-core; published/local parent c5c99f162bf67e08535b94b1c8ba830af7654e97 preserves f05a265. Source130 is unqualified and supersedes129. No native130 run, package or proof exists.

Preview129's original purity passed exit0 at2026-09-29T03:15:32.9960627Z; logSHA bf803c5fcee0315c7eadf85a6d3261ae6ea746b7cbc00064f12c0eae3a408eda. Never repeat it. Package acda9bb0e6d2894b030a05abf2a91f482245def26d1b6c3cb142ca4d18a6d3a8; manifest7c792cd58deaa2e5d5d1fb814497753c9477faab21e8ae62f59f55a03a47f470; DLL2c1b23c6c170d5ad4e37eb0d1a8b2a42a180f481994dcaa4087c475d4864c2cb/MVID5e4cecda-893f-4a1f-bafb-6ef9c2a8b944; suite20260929-chunk6a-preparation-a/SHA00b5ec62949b6bfd5856240a300866d8f2845c8e0bea4ad242d1949b1f3f15f1 remain immutable.

Seven native runs restored all five external-state checks. Positive c6a-preparation-a-approach passed48/0, compensation RT46/0 and TB47/0, geometry45/0, obstruction45/0, fullRT52/0. The positive combat request1922459136/process803058816/context-699568768/shell3/gen1 approached6.43669176m to2.82846713m inside2.9m, observed all eleven boundaries once, all six hooks, acted/one native Move/Success. Independent exploration windows and both actors' action/reaction proofs passed; carried forcedDetach1 and duplicateSuppressed1 each added zero.

FullTB c6a-preparation-a-full-tb failed44/2 before combat Mount. Native ground setup command-1948939776 reached Success on the same rider turn645278592, Preparing to Acting, but stopped0.285911083m from its destination under native0.3m approach, exceeding unchanged0.06m fixture tolerance. It carried actual Move debt0.1982034. GameSHA a1517d501a2df749b066b08dd2788431686256305e7ad271f1641ae052ca8e05; artifactSHA5bbf0a8693a73d9bfeae41d182ce83ff756893092ae8fa82eec957dd6b139282. Original assertions and all payload identities remain in docs/chunk6a-evidence-history.json and ledger. Historical129 ledger10PASS/1FAIL/76unqualified,26 retained failures; no provisional fullTB PASS credit. The129 partner-preparation repair was not exercised by this failed allocation.

Pinned native ground input accepts a commandRunner callback. Source130 uses that native selected-ground route and a diagnostic UnitMoveTo with0.03m approach, preserving0.06m terminal tolerance, exact single rider/turn/destination, all native path/movement/cost ownership and carried debt. Producer and external reader bind the exact callback command and observed admission/terminal/agent radii. Historical129-and-earlier evidence retains its original strict arrival checks;130 and unknown identities require the new block. No production resource writes, transition redesign or guard relaxation.

Source130 also integrates separately tested command-replacement, paired-policy refusal and area-restoration evidence. Build/source128/0 PASS; initial DLL1333265f2b55eaa6a1fc02ffaa80fa2a5fbe7a65da9851b71ab20947cc493b4e/MVIDeb59ff96-ce01-4262-ada2-6a95d094d65c,5092864bytes below unchanged5MiB. Focused precise input104/0, causal241/0, order envelope84/0 and history1094/0 PASS. Initial parser/reflection and history projection failures remain in lab logs; literal edit application, one-argument reflection invocation and artifact/row projection were corrected. No original evidence changed.

Next: complete offline umbrella; preserve actual exit receipt; coherent guarded publication; new immutable130 package/suite and fresh unchanged purity; corrected positive, isolated compensation RT/TB, geometry, obstruction, fullRT/freshTB and remaining mandatory87. Kingmaker closed; actual Steam safety passed before each129 guarded launch, human preview.105 restored. No6B before all mandatory6A rows PASS on one frozen candidate. Target ENGINEERING COMPLETE - OWNER ACCEPTANCE PENDING. No merge/tag/release/PR/permanent install/HUMAN PLAY inference.

## 2026-09-29 - preview.130 complete offline gate

**IN PROGRESS.** The complete source130 offline umbrella finished with actual exit 0 at 2026-09-29T04:11:50.0344050Z. Receipt: analysis-cache/chunk6a-causal/offline-process-preview130-integrated.json; log SHA-256 3b28ab83067f7f358dc536f65e9d9fab373e833ae7a3c45a0f15d38f0c80252e. Exact built DLL remains 1333265f2b55eaa6a1fc02ffaa80fa2a5fbe7a65da9851b71ab20947cc493b4e / MVID eb59ff96-ce01-4262-ada2-6a95d094d65c, 5,092,864 bytes under the unchanged 5 MiB limit. Focused precise Acting input 104/0, causal 241/0, history 1094/0, order envelope 84/0 and the complete offline gates PASS; native execution is still required.

All seven preview.129 runs and original proof have been independently reconciled by campaign-reconciliation-preview129.json. Each transaction restored all five external-state checks. Historical ledger remains 10 PASS / 1 FAIL / 76 unqualified with 26 retained failures. No previous PASS qualifies source130. The fullTB129 setup failure and all original assertion text, artifacts and payload identities remain intact.

Local/published parent c5c99f162bf67e08535b94b1c8ba830af7654e97 retains accepted f05a265 ancestry. Actual Steam guard PASS: sole client 1920, session 2026-09-27T10:45:18.2578304Z, offline 10:45:24Z, current-session cloud evidence 2026-09-29T03:32:46Z. Kingmaker closed; human preview.105 restored. C: has about 130 GiB free after owner cleanup.

Next: coherent commit and guarded publication; immutable preview.130 package (chunk6a-precision-a), new suite and one fresh unchanged proof; then positive, isolated compensation RT/TB, geometry, obstruction, full RT/fresh TB and remaining mandatory cases. No 6B until all 87 mandatory 6A rows PASS on one frozen candidate. Target ENGINEERING COMPLETE - OWNER ACCEPTANCE PENDING. No merge, tag, release, PR, permanent installation or HUMAN PLAY inference.
