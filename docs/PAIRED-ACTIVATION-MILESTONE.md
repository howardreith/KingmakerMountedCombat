# Combined actor-allocation and paired-activation milestone

Status: **PASS for the supported pre-combat pair**, final preview.37, 2026-09-08. The lifecycle, supported accounting/transition gates, final A05/A10 and Mammoth TB pass native integration. Persistence and the unqualified states below remain outside this claim.

## Authority and implementation

Work continues on `codex/mounted-combat-phase3f-playable-core`, preserving reviewed `45e3d276754257f4513342d5bce7626dd609d252` and Chunk 2 source `c804ba052760063f747cde83265e660916984d72`. [Frozen Chunk 2, including R/S](CHUNK2-ACTOR-ALLOCATIONS.md), [earlier command admission/K9](PHASE3E-PAIRED-SCHEDULER-IMPLEMENTATION.md), [reference map](../planning/WOTR-SHARED-TURN-MAP.md) and preview.13 [human play](CHUNK1-HUMAN-PLAY.md) retain their scope. [Earlier investigation](https://github.com/howardreith/KingmakerMountedCombat/blob/3bf84a6445c3743c3a0bcb69a2d4bc63f1078041/docs/PAIRED-ACTIVATION-MILESTONE.md) preserves intermediate failures and repairs. AGENTS.md now states the combined authority explicitly.

One valid Horse/Mammoth pair mounted before combat uses rider initiative. The existing UnifiedMountedTurnCoordinator, including its [paired lifecycle](../src/KingmakerMountedCombat/Integration/PairedActivationLifecycle.cs), is the sole authority. Encounter GUID, native principal boundary and sequence identify the grant; distinct actor records retain preparation, expenditure and completion. Selection, relationship replacement and global round changes cannot grant resources.

The mount is excluded inside the native ChooseNextUnit candidate loop, fixing selection at its semantic boundary. It remains in combat/world/effect systems; native sorting, wrapping and unrelated actors remain authoritative. Rider and private mount contexts each execute complete native preparation once: interruption/reapplication, reactions, round/fact/AI/condition processing and readiness. Only the private context's final UI tail is omitted. It never becomes CurrentTurn or starts/ticks another driver. The old partial preparation and scheduler paths are bypassed.

Existing selection, prediction, movement and attack controls admit only commands authorized for the live pair. The mount pays for transport; carried motion adds no rider Move cost. Native Full/Single/Primary meaning, weapon-specific reach, planning, timing, conditions, costs and cancellation remain. Both actors determine automatic completion; explicit End forfeits both grants. A disabled mount cannot trap completion. There is no generic off-turn exception, player-turn spoof, ignored-cost attack child or request-time cooldown clearing.

Unused same-round Delay resumes the same grant. Mode exit forfeits remaining resources, preserves greater debt, and requires native readiness plus at least six native seconds before renewal. Dismount retains current-round participation. Removal/disable finalizes contexts before retiring references; still-mounted pairs re-arm outside combat after native removal without preparing or changing debt.

The installed foreign Confusion prefix bypasses native processing for an off-turn partner. An original pair-local adapter uses verified Kingmaker parts, rules and native factories at the preparation boundary. Owned condition commands retain actor-local completion through forced detachment. SelfHarm settles only its observed temporary forfeit-before-charge contribution at native End, once. No foreign mod was changed.

The existing developer panel exposes **Enable paired activation prototype (before mounting)**. The service rejects changes while mounted, in combat or while participation remains owned, and rejects enabling alongside either legacy authority. Final native probes reject switches while mounted before combat and after actual partial movement, preserving actor snapshots and activation/context identity. Defaults remain false.

## Exact final candidate

| Identity | Value |
|---|---|
| Product / tested source | `0.1.0-paired-preview.37` / `ec5d44e6eddc9839d273176b345f7c9701520450` |
| Private ZIP, relative to lab | `artifacts/KingmakerMountedCombat-0.1.0-paired-preview.37-guarded-developer-controls-diagnostic.zip` |
| ZIP SHA256 | `2215156d43679913ee134c563f704cee37ce564ab70dc50d7009781250ee3738` |
| Manifest SHA256 | `41672972f2a02914dc7a400c55c9fa93e1037c0b0830f97c691522cd8cb58995` |
| DLL SHA256 / MVID | `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb` / `2ee2c106-2b00-45c7-bb99-c7d65f0228a7` |
| Suite / snapshot SHA256 | `20260908-paired-loop-suite37` / `ebafc19b6842ca410d242f6799e3bbe93a4c496671e4ba40786cc09a2713103b` |

Every final run uses `EnablePairedActivation=true`, `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false`, with actual overlay presence false. Horse/control envelopes record the configuration; Mammoth/party setup rejects incompatible settings and logs it. The private ZIP is unchanged; the final documentation checkpoint is a descendant of its tested source.

Exact Kingmaker authority: assembly SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; Kingmaker 2.1.7b, Harmony12, .NET Framework 4.7 and C# 7.3. Local IL/reference analysis stays outside Git and packages. No Wrath code, assemblies or assets ship.

## Final native evidence

Runs are `runtime-evidence/20260908-paired-<ID>/`, relative to the lab. Every original game result and outer validator is PASS, with zero failed assertions. Copied logs, hashes, restoration audits and `preview37-qualification-index.json` are under `analysis-cache/runtime-evidence/paired-activation-20260907/`.

| Runs | Result | Scope |
|---|---|---|
| AU / AV, both pre-pair initiative arrangements | 7 paired rows / 53 assertions each | P01-P06 and A05: rider-principal full loops and subsequent transitions. |
| AW / AX, matched unmounted controls | 3 rows / 49 assertions each | Native callback controls. T01/T02 coverage rows are not paired gameplay proof. |
| AY, ordinary TB | 19 accepted cases / 64 assertions | Full/Single/Primary, Rapid Shot/haste/BAB smoke, restrictions, costs, carried movement, mixed reach, hover and repeated input; ten paired encounters, 3,413 trace events, zero drops. |
| AZ, accepted phase3h RT | 9 cases / 54 assertions | Melee/ranged/Horse ordinary and Primary attacks, approach and paused controls/Stop. No obsolete phase3g RT gameplay substitution. |
| BA, unmounted RT | 2 cases / 47 assertions | Ordinary melee and ranged controls. |
| BB / BC | 1 case each / 62 and 60 assertions | Mammoth RT and party formation. |
| BD, paired Mammoth TB | 1 case / 66 assertions | Rider principal; mount Primary Standard 0→6, rider Standard/Move unchanged; two paired grants, no independent mount turn. |

**First gate:** AU/AV each execute three full paired activations, fourth refresh after early End, unrelated friendly/enemy turns and four real enemy movement legs for reaction consumption/refusal/renewal. AU records 13 native turn visits. Its first carried path travels **0.749993 m / 0.146352589 native seconds**, charging mount Move only. Rider and mount Primary each then spend native Standard 6; mount move-plus-attack ends at Move 3, and the exhausted request moves/costs zero. Fresh residual/conversion movement travels **30.767398 m / 6.000000139 native seconds**, reaches Move 6, leaves rider Move zero and rejects the subsequent unaffordable attack. The final path is clamped to remaining native time. Early End then renews each actor once.

**Final A05:** AU/AV/AW/AX contain **36 matched actor/preparation samples**: 36 preparations, clears, fact effects and healing applications; 72 round-handler and 360 readiness-handler deliveries, 540 dependent callbacks. Every matched sample has the exact ordered counts; all four traces have zero drops and observer errors. These are matched three-round intervals, not every preparation throughout P01-P06. Native reaction consumption and renewal are separately exercised in the paired loop.

**Final A10:** AY/AZ/BA/BB/BC total **32 accepted cases / 287 assertions / zero failures** on this exact candidate. Historical preview.35 A10 is not used to certify preview.37. AT requested preview.36 failed at mod load because its compiled identity was preview.35; no gameplay was qualified. Preview.37 fixes that identity, makes source-validation failure terminate callers, and validates compiled package version, with repeatable negative regressions. Original failed artifacts remain intact.

## Supported accounting and transition gates

| Gate | Status and evidence |
|---|---|
| A01-A04 exhaustion, residual movement, conversion, refresh | PASS — AU/AV P01/P03; distinct actor costs and actual travel/time, excess requests refused, subsequent grants renewed once. |
| A05 preparation/effects/readiness/reactions | PASS — final 36-sample matrix and native reaction loop. |
| A06 Stop, approach, interruption | PASS for tested paths — P02/P05 and final ordinary/RT controls. AU partial Stop retains 0.409348726 m / 0.0805807039 native Move seconds. |
| A07 step, restrictions, get-up, disabled completion | PASS for tested paths — P02/P04 and reactions. AU step: 1.00001025 m, Move cost 0, excess ordinary movement rejected; native get-up costs Move 3 once. |
| A08 split, death, record lifetime, next encounter | PASS for tested paths — P02 dismount, P05 condition-command/forced detach, P06 real mount damage/death/native clear/two removal notifications with one End per actor and correct unrelated successor; AY ten encounters. |
| A09 TB-RT-TB debt | PASS for tested transition — P02 retains spent Standard/Move, forbids immediate renewal and grants once after native time/readiness; AU observes 9.6 native seconds. Switching modes does not refund actions. |
| A10 and Mammoth TB | PASS — exact final candidate results above. |

Cross-round Delay is **DEFER — EVIDENCED**, visibly rejected; unused same-round Delay is qualified separately. Full mounted save restoration and same-campaign cold-load debt rebinding remain later persistence work: deleting actor references does not preserve unrepresented debt. Third-party custom confusion-choice restrictions, broader forced displacement, native rider-death qualification and different-campaign native loading remain unqualified. Mid-combat mounting remains rejected. Safe cleanup/rejection does not qualify those transitions or a complete feature. New-candidate human play and safe mod-absent certification remain separate; preview.13 human play retains its accepted scope.

## Final checks and external state

`scripts/Test.ps1 -SkipBuild` exits 0: components **364**, visual **23**, inventory **10**, harness **247**; protocols Phase3G **20**, Phase3H **42**, ordinary **42**, allocation **82**, restrictions **41**, condition commands **67**, death **32**, Mammoth **18**; assembly contracts **437 Kingmaker + 24 read-only Wrath = 461**; patch construction **30**, actual principal evidence serialization **3**. Final source **22/0**, package **11/0**. No final check failed. The intentional invalid-source regression prints its expected rejection before passing; nested totals are not added together. Log: `analysis-cache/paired-activation-native/final37-all-checks.txt`, SHA256 `1a42391061363065608586f7bd6fbcc2ef2b503e77387667c4662523312945d1`. These checks supplement native evidence; they do not establish gameplay themselves.

All **56 A-BD transactions** independently restore actual intake: preview.13 DLL/cache SHA256 `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`; protected saves digest `7332daa55136ab2e7d8ad2c4c4fe496a35e0059a55a48cd07c550c1c810f1d92`; full Mods digest `a4985d9881558608802427bc7758ed631830f61b4978774b3d549473fb1da58b`, including foreign-mod settings and KMC loader caches. BD restoration: `2026-09-08T12:12:18.1098375Z`; copied log SHA256 `7a3089db545817fde82f0d388d91dc8811b04197980814c454c0b772d538ca2f`.

Separate UMM Params.xml bytes match the retained human-installation receipt: SHA256 `dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`, checked after BA and BB/BC/BD. Native sessions rewrite its timestamp. This comparison is separate from save/Mods audits; no historical settings were restored over human data. Fixtures restore temporary settings caches. At campaign end, no game or transaction lock remained and preview.13 was restored. The campaign made no permanent deployment, human campaign edit, foreign-mod change, main merge or public release.

Guarded source publication is verified through `ec5d44e6eddc9839d273176b345f7c9701520450`; final qualification documentation was published at `aeb56f9e787bd633639de849a34ef108070501cf`. Later documentation publication uses the same direct project-owned helper; its exact branch HEAD and receipt are recorded in local `ACTIVE-RUN.json`.

Read-only reconciliation later on 2026-09-08 found the milestone already implemented and published, with both reviewed ancestors preserved and only documentation changed since the tested source. The historical restoration statements above describe the completed campaign. A separate deployment receipt at `runtime-state/deployment-operations/20260908T1829484707310Z-872bbc6c34384b33b48eb1a768ef47eb.json` records preview.37 installed at 18:29 UTC. Its current DLL/Info match the exact candidate; all three preview.13 backup files retain their recorded hashes under `runtime-backups/deployment-management/20260908T1829478028551Z-3d5f303a69b54b95a373fb430cf1762e/KingmakerMountedCombat/`. Current saves and several foreign-mod settings differ from the campaign intake; that newer state is preserved. UMM Params.xml bytes remain `dd22dc5a…`; no game or transaction lock is present. Reconciliation receipts are under `analysis-cache/paired-activation-native/reconciliation37-*`. This check neither deploys nor restores anything, and adds no human-play or native-gameplay claim.

The existing `Test-RuntimeResult.ps1` revalidates all ten original final results, **29 PASS / 0 FAIL each**, including their artifact hashes and scenario assertions. Seven retained package/suite/check/publication identities match; current source validation is **22/0**. The passing original full-test log and payload remain unchanged. Stale resume/blocker headings now point to this result instead of the resolved Chunk 2 dependency.

## Short private human checklist

The exact private ZIP above warrants focused review; the later installation is recorded separately above. Use an expendable play save, separate from protected automation fixtures. Mounted cold-load restoration is unsupported.

1. With the candidate running, dismounted and outside combat, enable **Enable paired activation prototype (before mounting)** in UMM. Leave both legacy options and diagnostic overlay off. Mount one Horse/Mammoth before combat.
2. Follow three rider-principal activations. Move partly through rider control, attack with each actor where affordable, and check unrelated turns and no second mount turn. Early End forfeits both actors' remainder; next activation refreshes once.
3. On a fresh activation, use residual movement and Standard conversion, then check that an unaffordable attack is refused. Compare full, native Single and explicit Primary attacks, including selected-mount control. There is no universal full-attack count.
4. Check hover/repeated clicks, weapon-specific bow/natural reach, approach, pause/Stop/selection and RT controls. Dismount after spending and confirm it creates no fresh mount grant. Include Mammoth if available.

For failures, report actor, RT/TB, weapon/action, movement already spent, expected/observed result and a short combat-log excerpt. New-candidate review is separate from the owner's accepted preview.13 play.
