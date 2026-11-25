# Mission: Mounted Combat melee and presentation repair

Mission ID: `kmc-mounted-alpha2-melee-pose-20261124`  
Prepared: 2026-11-24  
Project: `howardreith/KingmakerMountedCombat`  
Milestone: next installable private alpha, independently reviewed through the existing software factory  
Suggested branch: `codex/mounted-alpha2-melee-pose`  
Suggested version: `0.2.16-alpha.2`, only if unused and consistent with the repository's actual version policy

## 1. Governing task and acceptance scope

Implement the repairs requested in `OWNER_REQUEST.md`. Howie reports that Mounted Shield is working, but mounted melee, horse participation, and presentation have defects. Preserve the feat and repair the affected existing systems. Do not infer that introducing Mounted Shield caused every symptom; establish the relevant regression or pre-existing defect from evidence.

This is a **new private-alpha repair mission**, not a continuation or reset of the accepted Mounted Shield enrollment. Its required endpoint is current corrected source, meaningful focused verification, the actual installable package, independent factory acceptance, and a clear owner playtest handoff. Full native gameplay, UI, visual, compatibility, and save/load acceptance is explicitly assigned to subsequent owner alpha testing when not completed safely here. Such observations remain `not_run` or pending; they are never counted as passing.

The following companions are incorporated into this governing task: `OWNER_REQUEST.md`, `EVIDENCE.md` and its screenshot manifest, `CODE_MAP.md`, `RULES_AND_ASSETS.md`, `ACCEPTANCE.md`, `NATIVE_TESTING.md`, `FACTORY_WORKFLOW.md`, `PRE_NATIVE_REVIEW.md`, and `OWNER_PLAYTEST.md`. Commit the complete packet so the developer and independent reviewer can read them. The controller snapshots one mission file, so do not leave referenced materials only in an external ZIP.

Source-map explanations are investigation leads, not mandated diagnoses. The owner's functional outcomes and the actual Kingmaker contracts control implementation. Do not promote a hypothesis, a synthetic test, or a previous run's receipt into fresh runtime evidence.

## 2. Preserve the tested lineage

At packet preparation, live repository reads show:

| Identity | Value |
| --- | --- |
| Repository | `https://github.com/howardreith/KingmakerMountedCombat` |
| Mounted Shield PR | #61, open/draft/unmerged, branch `codex/mounted-shield` |
| Accepted archive/head A | `122a9f94680d1fbf512a0f9ec7386a1447d0c1f7` |
| Accepted reviewed head H | `7dd2adc136d334bc10f021c9d1469c2ed7354d33` |
| Alpha-1 packaged source | `cc17fe36f6fca17bcb7fafccf331e611454a2e4f` |
| Main at preparation | `fb743911200f150af407198c6c28519e522ef58c` |
| Build reference and declared UMM minimum | `0.31.1` |
| Previously recorded installed Windows UMM | `0.33.0`; re-read actual runtime before any native use |
| Existing Mounted Shield icon SHA-256 | `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` |

`references/baseline.json` records the prior alpha artifact identity. It is a comparison baseline, not proof of the binary loaded in the owner's screenshots.

Use a new isolated worktree and foreground enrollment. While PR61 remains unmerged, start from the verified Shield lineage and use the factory's supported `--target codex/mounted-shield` to create a stacked draft PR. Leave PR61, its source branch, archived review records, and accepted enrollment unchanged. Do not target current main from A as a fallback: inherited foreign review archives would be part of the new diff and fail transcript integrity.

If PR61 has already merged before enrollment, verify that current main contains the accepted Shield/UMM implementation, allowing a documented content relationship for a squash merge, and start from that main. Record the actual base and dependency. Do not merge the dependency, rewrite history, remove archives, or silently retarget an enrolled run.

## 3. Required repair scope

### MMR-01 — rider seating and mounting lifecycle

The rider appears unusually high above the horse; dismount/remount improves it. Diagnose first attachment, mounted-save restoration, view readiness/replacement, source-bone calibration, root-space offset, pose baseline, and attachment ownership. Distinguish a high attachment/root from a wrong skeleton pose. Use matched camera/profile/state observations when comparing height.

Make the supported Horse rider sit consistently without offset accumulation or stale calibration, including first mount and remount. Preserve clean unmounted restoration and stable rider/mount identities. A load/area/view transition that exercises the same lifecycle must not reintroduce a duplicate adapter or stale offset.

Do not begin with a global height reduction, scale change, or broad retuning of other mounts. The new owner report authorizes repairing this Horse seating defect; historical exhausted pose-tuning cycles do not prohibit a justified new repair. Change an existing calibration constant only after proving its coordinate basis and why lifecycle correction alone is insufficient. Preserve Mammoth and other profiles unless a demonstrated shared defect requires a narrowly justified correction.

### MMR-02 — turn-based attack approach and legal movement

An ordinary melee attack order on a reachable target outside legal reach should retain the target and move the mounted pair into legal attack range without requiring a separate ground-move click. Recheck the exact native attack plan, actor, movement grant, remaining actions, path, line of sight, stopping radius, mount/target corpulence, and mount-origin versus executor distance.

Use an eligible five-foot step when appropriate under the game's actual step mode and legal action state; otherwise use lawful available movement. Never grant a free step after incompatible movement, refund or duplicate Move/Standard actions, bypass turn/phase authority, increase weapon reach to hide a movement defect, or ignore LOS/path obstacles. If legal movement is insufficient, fail or wait in the same explainable way as the native control. The screenshot's `Move 1` display does not establish remaining entitlement or exact distance.

Exercise the current paired activation configuration, not just legacy experimental schedulers. Preserve both initiative arrangements and correct cancellation, target death, retargeting, and movement-carrier cleanup.

### MMR-03 — horse participation in ordinary pair attacks

An ordinary melee attack order for the mounted pair should carry the intended target through the **current paired activation** so that the horse follows through with its own legally available native attack or routine, without a redundant mount order. Sequential rider/horse execution is acceptable; simultaneous or free actions are not required.

The present source restricts default TB ordinary rider intent to the rider. Treat changing that user-facing order semantic as deliberate scope, with an explicit behavioral test. Preserve explicitly actor-specific commands and newer player overrides. At every dispatch, recheck that the horse is addressable, granted/prepared as required, not ended or already spent, capable, and in legal range. Do not turn this into indefinite cross-round TB autoattack, reactivate an already-used partner, call native Prepare to manufacture entitlement, or create a second action budget.

Retain the native distinction between a single attack and a full natural routine. Identify actual horse-owned attacks/rolls/damage by actor, target, and weapon; a hoof animation or rider damage is not proof of horse participation. Preserve bite and hoof attack availability/cardinality under the actual creature level, body, and native plan. Do not force all natural attacks after movement when only a single attack is legal.

### MMR-04 — real-time automatic melee engagement

When combat begins with an idle mounted melee pair, AI enabled, and no stronger player order or native restriction, the pair should acquire the nearest valid reachable hostile in its legitimate awareness/engagement set and approach/attack as ordinary native melee characters do.

First establish whether a native rider-brain proposal is absent, refused, or accepted but stalled. Inspect the existing automatic-intent policy and refusal telemetry before adding another targeting system. Preserve an explicit selected target until invalidated or replaced; define how nearest eligible candidates are compared and avoid repeated target oscillation. Do not acquire unseen or unreachable enemies solely because their entity positions exist.

Respect AI-off, Pause, Stop, Hold, explicit movement/casting orders, disabled/incapacitated states, queued player work, and targets that die or leave eligibility. Do not simply force-enable the mount's independent brain while the pair owns movement; preserve a single command/movement authority. If a native policy differs from the requested selection behavior, document the actual difference and implement only the narrow pair behavior justified by this request.

### MMR-05 — prompt real-time attack-command movement

Investigate the pause between a player attack order and the pair's first movement. The current source checks Standard-action readiness before dispatching the wrapper that later creates approach movement; this is a concrete lead, not a proven explanation.

Separate intent acceptance, legal movement start, and actual attack execution. Where a comparable native unmounted actor can move while its attack/initiative/cooldown is not yet ready, the mounted pair should also start legal movement promptly. Preserve the real attack's preparation, initiative, cooldown, range, animation event, reaction, and action expenditure. Do not reset timers or release damage early to make a graph look faster.

Measure input/acceptance, carrier creation, first nonzero movement, attack readiness, and first attack on a comparable control where runtime is available. Offline regressions must exercise the actual readiness/dispatch integration or shared production seam, not a standalone copy of the desired timing formula. A still image cannot establish a latency in milliseconds; do not invent a performance threshold from it.

### MMR-06 — unintended horse Trip

The supplied log shows a Horse Bite hit and damage, followed by a Trip maneuver. A plain Pathfinder horse companion has no inherent bite-triggered Trip; wolves and riding dogs legitimately have it. `RULES_AND_ASSETS.md` includes primary sources and actual construction details.

Bind the maneuver to the actual horse blueprint/instance, command, weapon, originating fact/component, and any relevant feat/item/buff/mod. Determine whether it is an unintended automatic trigger or a legitimately granted/selected maneuver. Do not assume a wolf clone, donor contamination, or a shared weapon bug without evidence.

Remove any unintended intrinsic Horse trigger at its narrow ownership/source boundary. Do not mutate the shared stock bite globally or veto every Trip by horse-type creatures. Preserve wolf/dog/Riding Dog trip, legitimate explicit maneuvers and separately granted effects. Preserve the horse's actual successful bite and damage; zero attacks is not a passing no-Trip test.

Consider existing created horses as well as fresh creation. If the defect persists as a saved fact, repair only the attributable KMC-owned or erroneously inherited fact with an idempotent, evidence-based cleanup; do not erase learned feats, foreign buffs, unrelated companions, or rewrite arbitrary save content. Do not undertake a general horse-stat or tabletop-maneuver rewrite.

### MMR-07 — visible native horse bite

The owner sees hoof animation but no bite animation. The repository already has `HorsePrimaryAttackAnimationAdapter` and historical native `HorseAnimationSet_Bite` evidence. The ordinary attack path reaches the adapter. Inspect the actual planned limb, special animation, selected action/variant/clip, handle creation/adoption/refresh, animator playback/blending, interruption, and attack event.

Connect the correct installed Kingmaker horse action to the real bite so it is readable at a normal play camera, retaining facing and the working hoof sequence. Native attack code must remain the sole owner of roll, damage, timing, and expenditure. An acted handle without a visible bite is not sufficient visual acceptance. Do not generate an extra attack event, stall the routine for cosmetic timing, remove the bite, or label a hoof kick as a new bite.

If fresh asset inspection establishes that no usable native bite presentation exists for the installed rig, deliver the bounded evidence and a precise visual limitation. A documented unavailable visual asset may remain optional for this private alpha; missing investigation or a broken existing playback path is not the same exception. Do not build a new animation/retargeting pipeline, import Wrath assets, or redistribute extracted game assets in this mission.

## 4. Regression and architecture constraints

- Preserve the single passive Mounted Shield feat and its prerequisites, feat-list publication/rollback behavior, existing icon bytes, equipment/lifecycle invalidation, nonstacking shield AC semantics, and independent Mounted Combat Mobility contribution. No additional toggle or icon is requested.
- The owner screenshots show `Mounted Shield +2` on Horse AC and a Mounted Combat Mobility total of 23 including `+2` against attack 19. Preserve those observations without treating them as proof of all shield types, enhancements, touch/flat-footed/Brilliant Energy, or save/load cases.
- Preserve the repository's .NET Framework 4.7/C# 7.3, Kingmaker 2.1.7b and Harmony12 contracts. Keep UMM 0.31.1 as build reference/minimum; do not downgrade or otherwise alter the installed loader. Compilation/binding checks are not Linux runtime certification.
- Keep native authority over actions, turns, preparation, reactions, attack planning and damage. Preserve current paired configuration and separate integration, domain, presentation and diagnostics responsibilities. Hooks delegate to the existing service layer.
- Preserve stable blueprint/save identities, exact lease ownership, unload/disable/dismount/exception cleanup, and unrelated actors/mods. A movement fix must not silently bypass the actual coordinator still responsible for movement accounting.
- Use existing libraries and behavior/contract infrastructure. No new dependency unless it replaces substantial code and is justified. Prefer real production paths and engine objects where available; mock only a genuine external boundary. Avoid source-text assertions or tests that merely restate implementation.

## 5. Work sequence and bounded validation

1. **Intake:** Read current `AGENTS.md`, `docs/VALIDATION-POLICY.md`, `CURRENT.md`, `AUTONOMOUS-RESUME.md`, this packet, actual factory installation metadata, and current source/PR state. Confirm the branch dependency and inspect real available native tools before promising a game run. Set the new task's budget ID in every validation shell.
2. **Reproduce and classify:** For each MMR item, record the observed symptom, exact code path/state, a discriminating reproduction or source-backed counterexample, and the smallest justified repair. Reuse existing diagnostics where possible. Investigate the related approach/participation/RT timing paths together, without presuming one cause fixes all of them.
3. **Repair and verify:** Make coherent focused changes with meaningful tests covering the concrete failure and affected neighbors. Select Fast/focused initially; broader qualification requires a remaining concrete risk or a required gate. Test changed actor-intent semantics explicitly, including the preserved actor-specific case. Do not rehabilitate unrelated old Sprint 6 forced-roll or missing-archive suites by rote.
4. **Optional native batch:** Follow `NATIVE_TESTING.md` and any required independent safety review. Newly created run-scoped disposable saves are authorized. No requirement to recover the vanished historical Working/Baseline bytes is added here. If safe native work is unavailable, record the precise limitation and continue the private-alpha source/package checkpoint.
5. **Package:** Build and validate the actual next alpha under the normal repository packaging policy. Do not overwrite earlier artifacts or select a duplicate version. If filenames remain `diagnostic`, explain the label. Record source, reviewed source, archive head, package SHA/size, DLL SHA/MVID, manifest version, UMM minimum, and embedded icon identity. Make the owner install/rollback instructions refer to this exact artifact.
6. **Formal review and handoff:** Use the actual new enrollment's focused gate and strict report, then the controller's independent final review. Fix returned actionable findings within the same run and limits. Only a real accepted `ready_for_owner_review` allows `finalize` and `status --remote`. Report the actual endpoint and deferred observations.

Use the distinct task budget `mounted-alpha2-melee-pose-20261124`, with the repository's default 90-minute cumulative validation allowance unless the actual authoritative record says otherwise. Preserve any existing spend under that task ID and all project/global budgets, locks, and formal limits; do not rotate IDs or reset old ledgers. Keep the planning target and per-block ceiling from current policy. Review wall time is distinct from validation spend.

Native setup is bounded to at most 30 minutes active fixture/setup work plus 10 minutes focused smoke/cold-load work, at most three guarded starts, all within remaining actual allowances. Stop earlier when a concrete tool, admission, or restoration obstacle makes the attempt unproductive. Complete any active save and restoration safely; a time ceiling never authorizes interrupting those operations.

Do not repeat the prior multi-round runner-requalification loop. Only plan a supplemental source/safety review when a concrete safe native route is available. Allow one coherent review and, if needed, one correction with targeted recheck for that optional route. A remaining native-route blocker defers that route; it does not authorize weakened safety or a fabricated review pass. Formal factory limits remain unchanged.

## 6. Evidence and completion

Keep one issue/acceptance ledger using `ACCEPTANCE.md` and its template. Separate:

- owner-reported baseline observations;
- current source and offline production-path verification;
- actual native observations with exact binary/fixture identities;
- owner-pending visual/gameplay/save-load confirmation.

For each MMR item, close the source checkpoint with a supported repair and relevant checks, or a supported explanation that the apparent failure was legal/native behavior and how the owner can distinguish it. A known unresolved core correctness defect is a blocker, not optional alpha polish. Lack of a safe game session alone does not block this expressly source/offline private-alpha milestone. If a symptom remains causally uncertain, say so; instrument and describe the discriminating owner check, without claiming a verified fix. The independent review judges whether the evidence supports the claimed source disposition.

Required deliverables:

- Focused production/test changes and current build/gate evidence in a new draft PR.
- Updated `docs/mounted-melee/HANDOFF.md`, issue/acceptance ledger, source/behavior explanation, and concise known limitations.
- Updated `docs/mounted-melee/PRIVATE-ALPHA.md` with exact package, install/rollback, dependency, and owner checks adapted from `OWNER_PLAYTEST.md`.
- Actual validated alpha ZIP in the lab's artifact location, with matching hashes and version records.
- New controller review/finalization records only when genuinely accepted, plus actual current remote status and handoff freshness. Preserve prior archives unchanged.
- Any native fixture recipe/provenance and restoration evidence if a native transaction actually occurred; otherwise a concise capability/blocker record.

Keep raw model streams, machine-local state, personal save data and private runtime evidence out of the repository. Commit sanitized results and stable references according to project policy. Never submit the packet's UNEXECUTED report as evidence; the new formal `dispositions` initially start empty and later use only real findings from the new enrollment.

No merge, tag, public release, permanent installation, controller installation/activation, mutation of the accepted Shield run, or messages to other people are part of this mission. Owner testing and merge/release decisions follow the reviewable alpha handoff.
