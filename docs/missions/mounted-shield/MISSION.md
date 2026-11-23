# Mission: Mounted Shield

**Mission ID:** KMC-MOUNTED-SHIELD-2026-11-23  
**Repository:** `howardreith/KingmakerMountedCombat`  
**Target:** current `main`, isolated fresh `codex/*` worktree  
**Developer:** main interactive Claude Opus/high  
**Reviewer:** controller-managed Codex `gpt-6.2-astra` / max  
**Plan:** existing `single`, explicit `foreground` mode  
**Successful endpoint:** actual controller `AWAITING_OWNER_REVIEW`; owner retains merge/release authority

## 1. Deliver the feature

Implement **Mounted Shield** as a single passive combat feat for the rider, requiring **Mounted Combat AND Shield Focus**. Use the supplied original feat icon. No new player-facing activation, toggle, resource, action-bar button or second icon is requested.

While an eligible rider is genuinely mounted and using an active physical shield, the feat supplies the same eligible shield amount to two different consumers:

- The current mount's native AC, as a shield bonus with normal stacking and attack-context behavior.
- Only the rider's existing Mounted Combat hit-negation Mobility rule, before that rule resolves.

The existing Mounted Combat reaction toggle and usage state continue to govern reaction attempts. Turning that reaction off, or spending its use, does **not** disable passive Mounted Shield AC. The new feat does not grant a general Mobility increase.

Published rules: [d20PFSRD, user-supplied reference](https://www.d20pfsrd.com/feats/combat-feats/mounted-shield-combat/); [Archives of Nethys, Advanced Player's Guide p. 165](https://aonprd.com/FeatDisplay.aspx?ItemName=Mounted%20Shield). Companion `RULES_AND_DECISIONS.md` gives the research and explanations. The following decisions form the implementation contract.

## 2. Fixed mechanics and explicit adaptations

1. Read the current, effective **physical shield** from the rider's active equipment, through verified Kingmaker 2.1.7b APIs. A shield in inventory or an inactive equipment set does not qualify. Generic Shield spells or arbitrary `Shield`-descriptor buffs on the rider are not substitute equipment.
2. The eligible amount is the physical shield's non-enhancement base defense, plus Shield Focus once, plus Greater Shield Focus once if present. Use native base data, not an item-name table. Do not read aggregate rider AC and subtract a guessed enhancement. Exclude item/spell enhancement, weapon enhancement, other item powers, cover and unrelated temporary AC effects.
3. **Greater Shield Focus inclusion is a deliberate mission interpretation:** its +1 increases the same non-enhancement shield defense. This is not a claim of an official Mounted Shield FAQ. Do not block implementation to ask this question again.
4. With Shield Focus alone, buckler/light/heavy/tower examples are +2/+2/+3/+5. A heavy shield +5 still supplies +3. A heavy shield with both focus feats supplies +4. No available shield means zero, including the focus contributions.
5. Follow the engine's existing effective shield availability, suppression and restoration. Shield bashing/buckler handling should use verified native behavior; do not build a separate tabletop shield subsystem. Improved Shield Bash retains eligibility wherever native behavior retains protection.
6. Use native **shield** semantics on the mount: same-type bonuses compete rather than adding. Normal touch AC ignores the contribution; normal flat-footed AC retains it. Keep armor/natural armor/deflection and other independent sources intact. Verify the actual Kingmaker descriptor representation rather than assuming a Wrath enum.
7. The check addition is the eligible shield amount, **not the mount's net AC increase**. A rider supplying +3 still gets +3 on an eligible hit-negation check when the mount already has a stronger +4 shield effect. A touch attack's disregard for shield AC does not itself cancel the separate check addition.
8. Keep the existing Mobility adaptation, native D20, difficulty handling, strict `checkResult > attackResult`, attempt limits, failure consumption, Swift expenditure/debt, crit cleanup and TB/RT timing. Equality still fails. An attack that already misses after AC calculation does not spend a reaction. Shield/equipment/mount changes do not refresh an already-spent reaction.
9. The optional Combat Stamina trick on the rules page is excluded. No stamina integration, enhancement sharing, extra reactions, new blanket settings or unrelated mounted-combat changes are included.

## 3. Production design requirements

### One source of truth

Use one production-owned shield eligibility/value calculation for both AC and the designated check. Read actual current rider, mount relationship, feat ownership, effective shield and permitted bonuses. Keep native adapters thin; test the production policy and actual adapter behavior. Do not create a parallel test-only implementation or mount-time snapshot.

The observed defense seam is `MountedCombatDefenseService.RollCheck`. Apply a named contribution to that actual `RuleSkillCheck` **before** `Rulebook.Trigger`; confirm the installed API and native breakdown. A log-only arithmetic change or a persistent rider skill modifier fails the mission.

For AC, choose the smallest verified native integration that gives both real attack behavior and an accurate mount character-sheet/tooltip value. A rule-event implementation must prove the sheet uses it. An owned stat modifier must have exact source identity, prompt reconciliation and exhaustive ownership cleanup. Do not prescribe an unverified API or allow every-frame remove/re-add churn as the default design.

### Relationships and cleanup

Use the authoritative mounted pair, with exact rider/mount identity and `RelationshipState.Mounted`. `MountedWorldService.Selected` is UI selection, not ownership. `ForActor` can return admission bindings; merely finding a pair is insufficient.

Refresh or invalidate on active-set/equipment changes, shield suppression, feat acquisition/removal, respec, mount/dismount, mount swap, rider/mount invalidation, area transitions, load, disable, rollback and disposal. Remove only this feature's state. Repeated reconciliation must be idempotent; no stale old-mount benefit, saved duplication or cross-pair leakage is allowed. Two simultaneous pairs with different shields are required coverage. Keep cleanup reachable even when save/area/restoration gates suspend ordinary updates.

### Registration and selection

Follow the current passive Mounted Archery pattern. Use one stable, fresh blueprint GUID, native ALL prerequisites referencing Mounted Combat and the actual Shield Focus blueprint, accurate localized name/description and the supplied icon. Validate prerequisite behavior through native level-up preview, including same-level picks, Back/cancel and respec. Do not add an independent rank/BAB/mount-ownership prerequisite that the rule does not state.

Copy the stock combat-feat donor contract: one rank, `Feat` and `CombatFeat` groups, normal visible flags, and the donor's `IsClassFeature` value. **Do not force `IsClassFeature=false`: that previously prevented registration in 2.1.7b.** Detect GUID collisions and an already-installed competing Mounted Shield choice rather than silently creating duplicates.

The current `KmcCombatFeatPublication` owns **two** coordinated feats. Extend that same ownership mechanism to three. Do not bolt on an independent lease over the same selection arrays. Preserve foreign entries and order, each owned feat once, ordinary/fighter required paths and the existing supported extra combat selections. Independently handle Archery and Shield registration/readiness/withdrawal. Losing Shield must not disable Archery; losing Archery must not disable Shield. Handle foreign append/reorder, in-place changes/reintroduction, disable/re-enable and exact restoration when safe. Never leave a selectable feat whose mechanic was withdrawn.

New icon/blueprint failure must be diagnosed and confined to the new feature's registration/publication where possible. Preserve Mounted Combat, Mounted Archery and other mods' entries. Do not quietly substitute artwork and call the required icon verified.

### Dependencies and scope

Target Kingmaker 2.1.7b, .NET Framework 4.7, C# 7.3 and Harmony12. Follow repository conventions and explicit project includes. No new library is needed. Do not require CotW, Gunslinger/Tabletop Rules, Buff Planner or Wrath. Compatibility should follow existing neutral native boundaries.

Do not fix the unrelated X05 save/door lock, redesign movement/projectiles/turn scheduling, change existing action costs, weaken validation policy, alter the controller, or modify protected saves as part of this feat.

## 4. Artwork and build contract

The supplied implementation candidate is `assets/MountedShieldFeat.png`: 64×64, 8-bit RGBA PNG, transparent corners and internal negative space. This is one original feat image; the larger files are source/review versions of the same image. The owner's color correction is authoritative: ring, shield and horse use one uniform #9B4D3A ink sampled from the supplied Mounted Archery reference; no gold, gradient or color variation.

Copy runtime bytes to `src/KingmakerMountedCombat/Assets/MountedShieldFeat.png`; add the explicit embedded resource with logical name `KingmakerMountedCombat.Assets.MountedShieldFeat.png`. Reuse `CreateEmbeddedSprite`, sprite name `KMC_MountedShield_Feat_Icon`, and `SetFeaturePresentation`. Suggested symbol/localization keys are `MountedShieldFeature`, `KMC.MountedShield.Name`, and `KMC.MountedShield.Description`. Verify unique blueprint identity before first registration, then keep it stable.

Retain the original master, 512px export, prompt, review sheet and provenance under the source-art convention. Do not regenerate, recolor, crop or replace the supplied runtime PNG during implementation. No ImageGen/ImageMagick/build-time art generation, DDS conversion, Unity asset bundle, new image dependency or second toggle asset is required. The runtime DLL must embed only the 64px runtime asset, not the review/master images.

Compare SHA-256 of packet PNG, repository PNG and extracted final DLL resource. Verify the loaded blueprint points to the intended sprite, and inspect the actual selection row/details/character-sheet feat with normal selected/unavailable UI treatment. Desktop previews do not prove native UI behavior. Howie's aesthetic approval remains separate and does not block implementation work.

## 5. Required behavioral acceptance

`ACCEPTANCE.md` and `templates/ACCEPTANCE_LEDGER.json` expand the cases below. All feature evidence begins NOT_RUN. The mission is complete only when required implementation/build/native cases are supported, or a genuine blocker is explicitly reported rather than converted to a pass.

- Correct ordinary/fighter eligibility, same-level preview/cancel/respec, single-rank donor flags and one visible feat/icon.
- Actual base-shield/focus calculation and enhancement exclusion across supported shield types; zero without active shield; explicit Greater Shield Focus decision.
- Immediate equip/unequip/set/suppression changes without remount; no generic magical-shield substitution or reaction recharge.
- Native competing-shield stacking, normal/touch/flat-footed AC and character-sheet attribution; a stronger mount shield leaves the check addition intact.
- Actual check-local contribution with native roll/log agreement, equality/failure/success boundaries, no effect on unrelated Mobility and existing reaction semantics retained in TB/RT.
- Passive AC retained when the existing reaction preference is off or spent; ordinary AC misses do not invoke the reaction.
- Two-pair isolation; dismount/mount-swap/death/removal; exact disable/disposal cleanup; load/area/respec restore exactly one current contribution.
- Third-feat publication regressions, foreign selection changes, isolated readiness failures and no selectable-but-inoperative feat.
- Genuine target build, exact embedded icon, normal package path, loaded native blueprint and UI, unchanged relevant neighboring behavior and confirmed runtime restoration.

Use realistic behavior-oriented tests. Prefer genuine native objects/compiled production paths where available and thin boundary fakes where the engine cannot be hosted. Avoid whole-module mocking, source-regex tests presented as behavior, call-count-only assertions and duplicate test logic. Any fault injection must exercise the actual decision path and remain absent from ordinary play.

## 6. Focused validation and native boundaries

Read current validation policy, cumulative budget and runtime readiness. Record a short impact-to-check map before running commands. Add a focused production-path entry point such as `scripts/Test-MountedShieldFocused.ps1` only when it actually exists and exercises this feature. The name in this package is proposed, not a claim that the script already exists.

During edits use the relevant Fast checks and directly affected defense/publication/donor neighbors. Candidate includes Fast: select it when freezing a native candidate rather than repeating both on unchanged bytes. The controller accepts one focused script name per call; aggregate necessary focused checks through the real entry point or use separately valid calls. Do not widen controller configuration. Avoid the historical Full campaign for an ordinary feature checkpoint.

The game API is not established by an offline calculation test. Verify installed shield base/enhancement/suppression contracts, native modifier behavior and real UI explicitly. Before live use, follow the concrete supplemental read-only Codex review in `PRE_NATIVE_REVIEW.md`; this fulfills the repository's scoped production/safety review without pretending the final factory checkpoint is complete. Retain its frozen SHA/verdict as ordinary evidence, separate from controller acceptance. Repeat only if later production/safety changes invalidate it. Use guarded scripts, one-executor locks, qualified disposable fixtures, backups and exact restoration. Finish restoration even if a budget expires. No normal-play install is authorized.

Research observed 0.2.15 records that preserve a **UMM 0.33.0/0.33.2 pin discrepancy** and recent lack of a qualified Working fixture. Recheck actual current local facts. Do not assume those issues persist, silently waive them or change a loader pin in this feature. If they remain blockers, finish useful source/offline work, retain the draft and evidence, and return the exact required native/UI rows as BLOCKED/NOT_RUN. A passed source gate is not a native pass or final feature acceptance.

Record source/offline/native/UI/restoration separately, with exact candidate, commands, results, actual elapsed times, hashes, fixture/platform identity and observations. Do not reuse evidence from a different binary after a functional change without a documented valid relationship. Follow existing qualification packaging; `Package.ps1` does not authorize a fabricated `-SkipBuild` shortcut.

## 7. Execute the new factory workflow

`FACTORY_WORKFLOW.md` gives checked syntax and provenance. Its essential requirements are part of this mission:

1. Develop in this same interactive Claude Opus/high conversation. Use explicit `--mode foreground`, project `kingmaker-mounted-combat`, plan `single`, and the actual installed/pinned controller. Do not start a headless writer or second writer.
2. Read current repository instructions/state, compare current main with the research baseline, inspect existing enrollments and installed controller identity, and create one clean isolated `codex/*` branch/worktree. Preserve unrelated work. Use the documented initial-push audit/helper; enrollment expects local and remote branch heads to match and creates/reuses the draft PR.
3. The controller snapshots **only the mission file**. Before enrollment, preserve this complete governing mission and commit the required companions/art so they are available inside the reviewed candidate. A simple valid layout is the complete packet under `docs/missions/mounted-shield/`, with the runtime icon also copied to its production path. If adapting to the established source-art directory, preserve all materials and update companion links while leaving the governing requirements intact. Do not leave required context only outside the worktree.
4. Confirm `status` reports `foreground_session.problems: []`; record actual package/source/fingerprint. This checks the real session. Never edit transcripts, permissions or controller state to bypass it. Routine `preflight` is unnecessary; it launches real probes.
5. Commit correction batches; use guarded `push`, `verify` and `review --report` with the exact report schema. The independent reviewer assesses the complete candidate against this mission, not only a fix narrative. Keep live validation/reviewer activity observable using returned `live_log`/`watch`; do not infer progress from a running PID.
6. Read the accepted decision, candidate, review reference, findings and **continuation** returned by the controller. Summarize the actual correction batch briefly and continue in the foreground. Track every pending finding as fixed, rebutted, deferred_optional or unresolved_blocker with evidence. Do not ask Howie to relay normal review results.
7. Preserve five review rounds/checkpoint, three attempts/round and the three-round no-progress stop from the pinned project. Do not reset findings, re-enroll to erase history, extend budgets, relax permissions, or update controller versions mid-mission. The installed configuration is authoritative if genuinely different; explain any material mismatch.
8. Capture later explicit owner task changes verbatim through the supported same-session `resume --amend-mission` path between commands. Read the full effective task and include its returned `report_citation`. Amendments do not extend budgets. Do not reinterpret a reviewer suggestion as an owner amendment.
9. After the actual accepted final `ready_for_owner_review`, run `finalize`, then `status --remote`. Report `AWAITING_OWNER_REVIEW` only if actually returned with a current handoff. Source-only completion with required native evidence missing is a documented blocker, not an implicit waiver.

## 8. Owner handoff

Provide the draft PR, enrollment, exact pinned controller identity, actual endpoint, accepted review/round/continuation, reviewed source H and final archive/head A, source-to-artifact relationship, gate receipts, completed acceptance ledger, native/UI evidence and limits, package/DLL/icon SHA-256 values and DLL MVID where built, restoration receipts, optional remaining items, and pending owner aesthetic/merge/release decisions. Use `templates/OWNER_HANDOFF.md`; keep machine reports within the supplied strict schema.

Do not merge, tag, release, request human reviewers, install for normal play, change a platform pin, or alter other enrollments. Finish at the real owner-review boundary or clearly identify the preserved work and exact blocker. Continue all ordinary authorized in-scope work without unnecessary confirmation.

## 9. Preparation provenance

Research baseline: [Mounted Combat fb743911](https://github.com/howardreith/KingmakerMountedCombat/tree/fb743911200f150af407198c6c28519e522ef58c). Factory guide: [development-process 41a185a7](https://github.com/howardreith/development-process/blob/41a185a71eff60ea83b8245ff367603109969429/agent_review/README.md); its controller-directory blobs match observed merged main `a6aa2b1fcfa0f946d94183acca36b2b5ddd04c0b`. Local installation/build/runtime was not available during package preparation. This is a current-source research record, not a claim about the machine when Claude starts.
