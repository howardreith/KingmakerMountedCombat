# Mounted Shield — handoff (KMC-MOUNTED-SHIELD-2026-11-23)

Mission: [docs/missions/mounted-shield/MISSION.md](../missions/mounted-shield/MISSION.md) (packet byte-identical to the
supplied ZIP; `tools/verify_packet.py` passes in place). Acceptance ledger: [ACCEPTANCE_LEDGER.json](ACCEPTANCE_LEDGER.json).

## 0. Current state — continuation under owner amendment 1 (2026-11-23)

Sections 1–9 below are the original run on production `9bbd6112` and are kept as recorded; this section supersedes them
where they differ (the UMM pin and the gameplay driver above all). Amendment: owner amendments revision 1, sha256
`8ee86965ae34836f17108c6a0d17faf19d06257ea8eff899492eda57b7b1dd6a`, recorded with `resume --amend-mission` in this
foreground session; the verbatim prompt is kept outside the worktree at
`analysis-cache/mounted-shield-owner-continuation-20261123.md` (sha256 `5fefaedb…`).

**Identities.** PR #61 (draft), enrollment `kmc-codex-mounted-shield-20261123114646-4433`, controller
`C:/Dev/agent-review/versions/85dece1c…` (fingerprint `6d613def…`). Current production source
`bb0c53d5c9847bb76f11636b6533337d67f3d364` (round-4 corrections; later commits are records only): DLL (gate build,
deterministic) SHA-256 `5978e285ccc0d6cc3163bd176e5205728b68fb57d2fd52087c36c5e9c63ff6f4`, MVID
`94c4e4d2-c673-4afd-b4f8-50845118888d`. The final identities and package are in the closing records below. The first
continuation source `cb74ad58` (DLL `1a80bc57…`, MVID `d7587812…`) was packaged as
`artifacts/KingmakerMountedCombat-0.2.15-mounted-shield-cb74ad58-diagnostic.zip`, SHA-256
`ba7f4e4ddaa69fa7f21dde5329ad5b2edcbcd7fb75b172dce9bab0b7106b5e0a`, Info.json `ManagerVersion` 0.31.1. Icon: packet,
repository and the packaged DLL's single `KingmakerMountedCombat.Assets.MountedShieldFeat.png` resource all SHA-256
`fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0` (4,408 bytes).

**UMM baseline (companion PR #63).** Commit `5be0f737b1d40df6df4da886707d721801557ee9` (parent `f19c8333`, this branch's
head) was incorporated by fast-forward, unchanged, and pushed through the enrollment's guarded push. Policy, as the owner
set it: UMM 0.31.1 is the pinned build reference and declared minimum (planning/UMM-BUILD-REFERENCE.json; archive and
DLL hashes checked on restore and in MSBuild, no installed-loader fallback); UMM 0.33.0 is the recorded Windows native
target (planning/ENVIRONMENT-FINGERPRINT.json); the stale 0.33.2 expectation is gone. The installed loader was not
changed. Verified here on Windows: the restore helper on the genuine archive under Windows PowerShell 5.1 (9/0), a fresh
product build against the restored 0.31.1 reference, the compiled product's loader bindings against 0.31.1 and against the
installed 0.33.0 with their reviewed profiles (25/0 each), the native inventory reader (101/0), build provenance
(frozen-product source 23/0), and packaging (Validate-Package 11/0). A 0.31.1 build does not prove native gameplay on 0.31.1.

**Selection rollback (PN3-F3 remainder).** Fixed in `cb74ad58` and reproduced through the compiled method; see
[PRE_NATIVE_REVIEW.md](PRE_NATIVE_REVIEW.md), continuation table. **PN3-F5:** reassessed as an optional compatibility
limitation, not a blocking violation, with the installed `CanSelect` order as evidence (same table).

**Gameplay driver.** In the Sprint 6 feat family (schema 50), reusing its fixture, level-up controller, rule observer,
defense record and readers:

| Scenario / case | Covers (ledger rows) |
| --- | --- |
| `sprint6-shield-rt`: MS-selection | Native eligibility without each prerequisite and with both; Mounted Combat and Shield Focus at one level with a Back; every level chosen and cancelled first; ordinary and fighter lists; registration, icon binding, three-feat tails; a second pair (Shield Focus natively, native Ranger companion) (MS-01, MS-02, MS-20, MS-23) |
| MS-equipment | Stock buckler/light/heavy/tower, enhanced heavy shields, Greater Shield Focus on/off, competing +2/+3/+4 and the stock Shield spell on the mount, inactive weapon set, unequip, rider-only Shield effect; native AC, touch, flat-footed and AC rules each step (MS-03–MS-07, MS-09, MS-10, MS-12) |
| MS-pairs | Two pairs (+3/+2), cross-reads, UI selection, dismount/remount, feat removal/reacquisition, service disable/enable (MS-16, MS-17, MS-19) |
| MS-check-rt | Planned equality fails and +1 succeeds on the shield-modified total with exactly one +3 modifier in the native roll; spent and preference-off refusals keep the passive AC; equipment changes during a use refund nothing; an unrelated Mobility check gets nothing; a miss caused by the shield's AC gets no check (MS-11–MS-15) |
| `sprint6-shield-tb`: MS-rider-feat, MS-check-tb | Turn-based own-turn success, refusal and off-turn borrowed equality (MS-15) |
| `mounted-shield-tb-manual` (persistence) | One contribution before a native Manual save, never two at cold restore, exactly one after (MS-18) |

Not driven: native respec (it rebuilds the unit through the character-generation UI), bash/buckler suppression (MS-08),
temporary and shield-weapon enhancement, death and a different mount, area transition, the mount sheet and feat UI (MS-24).
The reader (`scripts/runtime/Sprint6ShieldEvidence.ps1`) derives every expectation from the mission's rule and is proven on
synthetic evidence by `scripts/Test-Sprint6ShieldReaders.ps1` (63/0), including the refusal of an "equal" row three above
equality. None of this is native evidence.

**Fixture: the remaining blocker.** Inspected: the save root (95 files; only KMG_/KBP_ project fixtures and human saves),
`runtime-state/` (no `fixture-qualification.json`), `scripts/runtime/Chunk7OwnerSeedRegistry.json` (five owner-admitted
seeds, among them `KMC_C7_NO_MODS_OWNER_SEED` `3405d7c8…`), and a search of the lab and user profile. The registered
immutable masters under `fixtures/owner-supplied/` no longer exist, nor does any KMC fixture elsewhere; other projects'
fixtures and play saves are not relabeled. Native rows therefore stay BLOCKED.

**Exact external input needed** (corrected after review round 4). The Sprint 6 family, and so `sprint6-shield-rt`,
`sprint6-shield-tb` and the persistence case, runs save-backed on the canonical qualified KMC fixture pair: one
`KMC_AUTOMATION_BASELINE` and one `KMC_AUTOMATION_WORKING` in the save root, with the qualification record
(`runtime-state/fixture-qualification.json`) and suite snapshot the runner's preflight checks
(`Assert-KmcQualificationSuiteContinuity`), and the exact stock Mammoth pair idle, unmounted and outside combat that the
parent intake requires (`Chunk6aMammothScenarioEngine.Start`). The last recorded pair was Baseline `c29d965c…` and Working
`5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6` (docs/CHUNK5-PERSISTENCE.md). The input is **that exact
historical pair** with its qualification records: the persistence route pins the Working SHA-256 in its compiled intake and in
its reader (scripts/runtime/Chunk7CombatPersistenceEvidence.ps1), so a newly qualified save with the same actors and names is
refused there, and the guard is kept (review PN5-F4). An owner seed (for example `Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks`) is admissible
only through its own owner-seed route and families; it is not a substitute for this pair. The persistence case runs as
`chunk7-persistence-save` then `chunk7-persistence-load` with `-PersistenceCase mounted-shield-tb-manual` on that Working
fixture, as `mounted-combat-feat-tb-debt-manual` did (docs/SPRINT6-MOUNTED-COMBAT-FEAT.md); the owned-pairs and owner-seed
checkpoint routes do not admit it (review PN4-F7). With the pair in place, the batch is a guarded startup smoke, then
`sprint6-shield-rt`, `sprint6-shield-tb`, and the save/cold-load pair, each through
`scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1` and its guards.

**Impact-to-check map (before testing; budgets read first).** Rollback → Test-MountedShieldSelectionRollback, Ranger
selection contract, Mounted Archery contract. Planned check total → native contract (planned total on the production
class), Sprint 6 readers, components. Driver, persistence case, registries → Sprint 6 shield readers, Sprint 6 readers,
chunk7 combat persistence and persistence readers, profile-area isolation, Validate-Source, Candidate harness. UMM import →
restore helper, both loader binding checks, native inventory reader, frozen-product source, packaging. All are in
`scripts/Test-MountedShieldFocused.ps1` (18 children) except packaging and the Candidate scripts.

**Receipts.**
- Controller focused gate, Fast, on pushed `cb74ad58`: PASS, 50.1 s (build, components 1507/0, Validate-Source 153/0,
  focused 18/18 in 35.6 s); receipt `gate-focused-cb74ad587476-…-20261123T203236957932Z`. Budget `mounted-shield`: 2.32 of
  90 min after it.
- Controller focused gate, Candidate, on `cb74ad58`: FAIL at `Test-Harness.ps1` (280 pass, 2 fail), 247.6 s. Both failures
  open `artifacts/KingmakerMountedCombat-0.1.0-phase3e-fallback.1-…-diagnostic.zip`, a historical package absent from the
  lab (archives were cleared before 2026-11-20); no code change touches them. The tier stops there, so the remaining
  Candidate scripts were run directly: paired activation 40/0, assembly contracts (Kingmaker) 837/0,
  `Test-RuntimeArtifactManifestContract.ps1` fails identically on the unmodified base `5be0f737` (cleared run directory).
  This run was charged to the shared `development` budget (79.63 → 83.75 of 90 min), because `KMC_WORKFLOW_BUDGET_ID` was
  not set in this shell; later runs set it to `mounted-shield`. The spent allowance is recorded, not reassigned.
- Neighbors run locally (not in the gate): Sprint 7 readers 92/0, chunk7 combat persistence observation 190/0,
  chunk7 persistence source 25/0. `chunk7/Test-Chunk7PersistenceWiring.ps1` fails identically on base `5be0f737`
  (`$f2Upgrade` unset).

**Review and controller.** Supplemental round 4 on `f4d0078a`: CHANGES REQUIRED, ten findings (PN4-F1–F10), nine fixed
in `bb0c53d5` and PN4-F7 resolved by documenting the persistence route; see [PRE_NATIVE_REVIEW.md](PRE_NATIVE_REVIEW.md).
Fast gate on `bb0c53d5`: PASS, 61.6 s (components 1508/0, Validate-Source 153/0, focused 18/18). Round 5 and the controller
endpoint follow in the same record.

## 1. Outcome

- **Source and offline work: complete.** Both benefits, registration, three-feat publication, lifecycle and the icon are
  implemented and pass the component runner, the source validator, the new native-contract script and the controller's
  focused Fast gate.
- **Native and UI evidence: BLOCKED** on this machine (section 6): UMM 0.33.0 is installed while the repository pins
  0.33.2 (an owner decision, not changed here), and there is no `KMC_AUTOMATION_WORKING`/`BASELINE` fixture. Required
  native rows are reported BLOCKED or PARTIAL in the ledger; none is converted to a pass.
- **Factory:** enrollment `kmc-codex-mounted-shield-20261123114646-4433`, foreground, plan `single`, draft PR #61.
  Because required native evidence is missing, the developer report is `blocked`, which the controller records as BLOCKED
  without launching its final reviewer. That is the documented endpoint, not `AWAITING_OWNER_REVIEW`. An independent
  read-only Codex review of the coherent change (section 5) was run instead, as the repository requires before live use.
- **Not done:** no merge, tag, release, package for play, installation, staging or game launch.

## 2. What changed and why

**One calculation.** `MountedShieldPolicy.Amount` (Domain) over `MountedShieldService.ReadFacts` (Integration) is the
only source of the amount for both benefits. It reads the live rider each time; nothing is captured at mount time.

- The exact authoritative pair: `world.ForActor(rider)` is `Mounted` with exactly this rider and this mount, both in the
  game and alive. UI selection is never read.
- The rider's Mounted Shield fact is active and turned on.
- An effective physical shield: an `ItemEntityShield` in the off-hand of the current weapon set, in an active slot, whose
  own armor component's native `Shield` modifier is on the rider's AC. That is the engine's own availability (an inactive
  set or a disabled slot unequips the item natively). A shield in the inventory or a Shield spell never qualifies.
- Base: `ArmorComponent.Blueprint.ArmorBonus`, the armor-type value `ItemEntityArmor.RecalculateStats` adds before it
  adds `GameHelper.GetItemEnhancementBonus`. Item and temporary enhancement, the shield weapon's enhancement and other
  item powers are never read.
- Plus Shield Focus once and Greater Shield Focus once (the mission's explicit interpretation), resolved by GUID and name.
  Without an effective shield the amount is zero, focus included.

**Mount AC.** One native `ModifiableValue` modifier on the mount's AC stat, descriptor `Shield`, source = the rider's own
Mounted Shield fact, source component `KMC_MountedShield_MountShieldBonus`. The engine applies shield semantics: the
highest shield bonus counts (a stronger shield on the mount leaves AC unchanged), touch AC excludes it, flat-footed AC
keeps it, and the sheet and tooltip read the same stat and name the source fact.

**Ownership and lifecycle.** `MountedShieldContributionLedger` records the exact modifier per rider and writes only when
the wanted mount or amount changes (or the recorded value was removed by something else); repeated reconciliation never
accumulates. Release paths:

- every ordinary frame, the ledger is reconciled against all mounted pairs (shield, set, feat, pair, death changes);
- immediately for one rider's pair when the mount's `RuleCalculateAC` is about to read its AC, so no attack sees the
  previous equipment's value (the sheet follows within a frame; no plain EventBus subscriber is added, which keeps the
  reviewed global-subscriber inventory unchanged);
- an ungated removal-only pass, ahead of the save/area/restoration update gates, so a stale value never waits for them
  (skipped only while an archive worker serializes or an owned load replaces the world);
- the pair's `PairDismounting` event;
- the feat fact's own `MountedShieldFeatureLogic.OnTurnOff`: feat removal, respec, unit teardown, and the native save
  path, which turns the party and loaded area off before serializing them, so the value is never written into a save;
- disable, update-failure cleanup, dispose and construction rollback (`ReleaseAll`, which also sweeps loaded units for
  this feature's modifier identity only).

Every removal goes through the owning stat by exact reference and is verified, so a write the engine left half-done (a
stat-change callback threw before `AppliedTo` was set) is still found and removed. Disable, dispose and update-failure
cleanup are refused while a Mounted Shield modifier remains, keeping the record for a retry; the update-failure stop
disables the service (both benefits and the selection together) and never runs during an owned load. A value that still reached
a save would be dropped by the native load cleanup (`CharacterStats.CleanupModifiers` removes a modifier whose source
fact the mount does not own). Another source's shield bonus is never touched.

**Check.** `MountedCombatDefenseService.RollCheck` adds the same amount to its own `RuleSkillCheck.Bonus` (untyped,
stacking, named by the rider's fact) before `Rulebook.Trigger`, so the native roll, log and the strict
`checkResult > attackResult` comparison include it. It is computed from the rider's shield, not the mount's net AC change,
and a touch attack does not remove it. A fault there costs only the addition, never the committed check. The battle-log
line names the addition (`(Mounted Shield +3)`). No rider stat, reaction state, toggle, cost or timing changed.

**Registration.** `MountedShieldFeature`, fresh GUID `671a301bd1774677a8bfa2873d33b191`, built from the stock Power Attack
donor (one rank, Feat + CombatFeat, the donor's `IsClassFeature = true` retained), prerequisites exactly Mounted Combat
and Shield Focus in the native ALL group (plus the hidden availability check above, which shows no text), localized `KMC.MountedShield.Name/Description`, sprite
`KMC_MountedShield_Feat_Icon` from the embedded `KingmakerMountedCombat.Assets.MountedShieldFeat.png`. Registered after
Mounted Archery; its faults (icon, prerequisite blueprints, GUID collision, a competing Mounted Shield in any list KMC
publishes to) withdraw only this feat with a logged diagnostic. Removal preparation knows the new GUID and reference.

**Publication.** The existing `KmcCombatFeatPublicationSet` now owns Mounted Combat, Mounted Archery and Mounted Shield in
one lease per array, in that order, with independent withdrawal: a required list (ordinary or fighter) losing one
optional feat withdraws only that feat everywhere; Mounted Combat, the other optional feat and foreign entries stay. The
two-feat constructor and Mounted Archery's API and diagnostics are unchanged. The per-frame refresh is a full membership
check, so another mod's in-place edit of a required list withdraws within a frame, and it collapses a copied second
reference to a KMC feat. Disabling restores KMC-owned arrays exactly and removes only KMC's own entries from an array
another mod changed; that stripped foreign value becomes the list's original, so re-enabling republishes after it. A
refused enable or disable returns the combat-feat publication to agree with the services. A hidden native prerequisite
refuses Mounted Shield at the selection decision whenever its rule is unavailable (never affecting existing owners).

**Icon.** The supplied 64x64 RGBA PNG, byte-for-byte (SHA-256 `fd519874…`), is the only new embedded resource. The full
packet (master, 512px export, generated image, review sheet, prompts, rejected revision, manifest) is preserved under
`docs/missions/mounted-shield/assets/` instead of `assets-source/`, as the mission's simple layout allows.

## 3. Installed contracts verified (Kingmaker 2.1.7b, read-only)

Read from the installed `Assembly-CSharp` (decompiled in a scratch directory, not committed) and `sharedassets1.assets`:

- `ItemEntityShield` builds its `ArmorComponent` with `ModifierDescriptor.Shield`; `ItemEntityArmor.RecalculateStats`
  adds one AC modifier of `Blueprint.ArmorBonus + GameHelper.GetItemEnhancementBonus(this)`.
- `ShieldFocus` (component) adds +1 `Focus` while `SecondaryHand.HasShield`; stock blueprints `ShieldFocus`
  `ac57069b6bf8c904086171683992a92a` and `ShieldFocusGreater` `afd05ca5363036c44817c071189b67e1`.
- Stock shield types: Buckler +1, Light +1, Heavy +2, Tower +4 (`BucklerType`, `LightShieldType`, `HeavyShieldType`,
  `TowerShieldType`).
- `ModifiableValue`: `Shield` is not a default stacking descriptor (highest wins); `GetDisplayModifiers` lists one
  non-stacking bonus per descriptor; modifiers serialize with the stat; `CleanupModifiers` (unit `PostLoad`) drops a
  modifier whose source fact the owner lacks.
- `ModifiableValueArmorClass`: touch excludes positive Armor/Shield/Focus-class modifiers, flat-footed excludes Dodge and
  Dexterity; the character sheet and `StatModifiersBreakdown` read the stat and `Modifier.Source`.
- `SaveManager` turns `LoadedAreaState`, `CrossSceneState` and `Player` off before `PreSave` and serialization; facts call
  each component's `OnTurnOff`.
- `ItemSlot.OnActiveChanged` calls `OnWillUnequip`/`OnDidEquipped`; switching sets raises
  `IUnitActiveEquipmentSetHandler`. `HandSlot.MaybeWeapon` exposes the shield's weapon component for a bash without
  touching its armor modifiers: 2.1.7b has no bash-time shield suppression, so a bash keeps eligibility.
- `RuleSkillCheck.Bonus` is a `ModifiableValue`; `RollResult = D20 + StatValue + Bonus`; `IsPassed` is `>= DC`.

## 4. Impact map and validation

| Change | Risk | Check |
| --- | --- | --- |
| Amount policy, ledger | arithmetic, ownership, accumulation, isolation | component `MountedShieldTests` (14 cases) |
| Publication set generalized | Mounted Archery regressions, independent withdrawal | existing Archery publication tests unchanged + new three-feat tests; Archery donor script; production set on genuine selections |
| Contract generalized | Archery diagnostics text | existing `MountedArcheryFeatContractTests` unchanged + Shield contract tests |
| Base read, AC write, check write | native semantics | `Test-MountedShieldNativeContract.ps1` on genuine engine objects |
| Defense seam | reaction policy unchanged | `MountedCombatDefensePolicyTests` (components) |
| Ranged rule's availability consumer | Archery availability | `Test-MountedRangedMovementContracts.ps1` |
| Composition, project file | build, source rules | MSBuild Release, `Validate-Source.ps1` |

Executed (all offline; none is native evidence):

- Controller gate: `verify --gate focused -- -Tier Fast -Focused Test-MountedShieldFocused.ps1`.
  - On `801e1cd3` (first candidate): PASS, 19.6 s; components 1502/0, Validate-Source 153/0, focused 3 of 3 (native
    contract 68/0, Mounted Archery donor 54/0, ranged movement 5/0). Receipt `gate-focused-801e1cd34fc8-…-20261123T170855771092Z.log`.
  - On `16a4cc56` (after the review corrections): PASS, 23.7 s; components 1506/0, Validate-Source 153/0, focused 3 of 3
    (native contract 72/0, Mounted Archery donor 54/0, ranged movement 5/0). Receipt
    `gate-focused-16a4cc5625ff-…-20261123T173406517396Z.log`.
  - On `1f1bb814` (after the round-2 corrections): PASS, 21.7 s; components 1507/0, Validate-Source 153/0, focused 3 of
    3 (native contract 74/0, Mounted Archery donor 54/0, ranged movement 5/0). Receipt
    `gate-focused-1f1bb814ca5e-…-20261123T175456604438Z.log`.
  - On `9bbd6112` (after the round-3 corrections): PASS, 26.5 s; components 1507/0, Validate-Source 153/0, focused 7 of 7
    (native contract 74/0, Mounted Archery donor 54/0, ranged movement 5/0, continuous drive 59/0, chunk7 replay
    subscriber 21/0, chunk7 Ranger selection 183/0, localization keys). Receipt
    `gate-focused-9bbd611288ee-…-20261123T182119188344Z.log`.
  - Budget `mounted-shield`: 1.48 of 90 minutes after all four.
- Other contract scripts that read changed code, run once on `9bbd6112` and once on a detached build of base `fb743911`:
  `Test-MarkerOwnershipContracts` 18/0, `Test-MountedPresentationContracts` 239/0, `Test-ScenePlacementAttributionContracts`
  24/0, `chunk7\Test-Chunk7RepeatedWorldEvidence` 220/0 pass; `Test-Finalize2UpgradeReaders`, `Test-MountedAttackStallRepair`,
  `Test-WorldOwnershipContracts`, `chunk7\Test-Chunk7ObservationLifetime`, `chunk7\Test-Chunk7RegisteredDisableContracts`
  and `chunk7\Test-RuntimeStartupTrace` fail with the same errors on base (cleared historical artifacts, stale splices,
  a removed field): pre-existing stale harnesses, not regressions.
- `docs/missions/mounted-shield/tools/verify_packet.py`: 26 files, runtime PNG checks PASS.

Not selected: the Candidate tier (it stops on the recorded `Test-Harness.ps1` 278/3 baseline failures, docs/CITY-AND-
PRONE-HANDOFF.md section 10), Full, packaging and every native campaign.

## 5. Independent review of the coherent change

Three rounds of the read-only Codex review (gpt-6.2-astra, max), each CHANGES REQUIRED: round 1 on `1c49cb20` (five
findings, fixed in `16a4cc56`), round 2 on `70ac70c0` (six, fixed in `1f1bb814`), round 3 on `54de0408` (seven: six fixed
in `9bbd6112`, PN3-F5 open). The round-3 corrections are gated but have not had a further review (bounded at three
rounds). Records and dispositions: [PRE_NATIVE_REVIEW.md](PRE_NATIVE_REVIEW.md).

## 6. Native evidence: blocked, and what would run

Rechecked on 2026-11-23:

- `Kingmaker_Data/Managed/UnityModManager/UnityModManager.dll` is 0.33.0 (SHA-256 `63e5baf7…`); the harness pins 0.33.2
  (`scripts/runtime/Test-RuntimeGameResult.ps1`). Every native verdict therefore fails on platform identity, as the
  0.2.13–0.2.15 records show. The pin is the owner's decision and was not changed.
- The save root holds no `KMC_AUTOMATION_WORKING` or `KMC_AUTOMATION_BASELINE` save (only other projects' `KMG_`/`KBP_`
  fixtures). Every gameplay scenario needs a qualified Working fixture.
- No driver exists for level-up feat selection, equipment changes or the mount sheet; adding one is diagnostic code
  compiled into the product (a new DLL identity and another independent review).

Required rows blocked by this: MS-01, MS-18, MS-24, MS-27 entirely; the native part of MS-02–MS-17, MS-19, MS-22,
MS-23 and MS-25 (ledger `PARTIAL`). When the owner resolves the pin and supplies a disposable seed, a small batch covers
them: (1) startup smoke (registration line, selections, icon decode); (2) level-up with Mounted Combat and Shield Focus
picked the same level, Back/cancel, respec; (3) one mounted shield fixture: heavy shield with Shield Focus, mount sheet and
tooltip, touch/flat-footed attacks, a Shield spell on the mount, +5 shield, set switch, unequip, Greater Shield Focus,
forced d20 equality/success, toggle off and spent reaction, a second pair; (4) save, cold load and area transition. The
forced-roll driver must target the final native check total: the existing Sprint 6 helper derives its "equal" roll from
general Mobility alone and would be three high with a +3 shield (review note).

## 7. Artifact identities

- Production source `9bbd611288ee673f935a16f602fe47414abf850c` (after three review correction batches); later commits are
  records only. Superseded production sources: `801e1cd3` (DLL `ae4f8a92…`), `16a4cc56` (`30bf64ab…`), `1f1bb814`
  (`8c40a66c…`).
- DLL (gate build, `bin\Release`): SHA-256 `3cc058936d6cb7828d183b990a70d7eb97dc3d6b8025c7f79dc469a95ce4773a`, MVID
  `9f36d10f-dee2-43a7-bb64-4c9a56a4ab69`, 9,587,200 bytes.
- Icon: packet, `src/KingmakerMountedCombat/Assets/MountedShieldFeat.png` and the extracted resource all SHA-256
  `fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0`.
- No package was built (no native candidate).

## 8. State preservation

No runtime transaction, staging, installation or game launch ran, so there was nothing to restore. The installed
controller, other enrollments, other worktrees, the owner's Mods, settings and saves were not touched. X05 (save/door
lock) is unchanged by this feature and nothing here claims to affect it.

## 9. Review, controller status and remaining decisions

- **Supplemental review:** section 5. Open: PN3-F5 (a foreign prerequisite-ignoring selection). The round-3 corrections
  have not had a further supplemental review.
- **Controller:** the developer report for the final checkpoint is `blocked` (required native/UI acceptance cannot run:
  the UMM pin and the missing fixture). The controller records that as BLOCKED without launching its final reviewer, so
  this run ends at a documented blocker, not `AWAITING_OWNER_REVIEW`; no accepted factory review exists. The PR carries
  the controller's disposition and status comments.
- **Owner decisions:** the UMM 0.33.0/0.33.2 platform pin; a disposable KMC seed for a qualified Working fixture (or owner
  playtests of the blocked rows); a guarded gameplay driver (new DLL identity and review); then the factory's final review
  can resume on this enrollment. Also: aesthetic acceptance of the icon, PN3-F5, merge and release.
- **Owner playtest, once installed by the owner:** take Mounted Combat and Shield Focus, then Mounted Shield; ride with a
  heavy shield and check the mount's AC tooltip shows "Mounted Shield +3" (Shield); equip a +N shield (still +3); add
  Greater Shield Focus (+4); unequip or switch weapon sets (bonus gone at once); give the mount a stronger shield bonus
  (AC unchanged) and check a Mounted Combat roll still reads "(Mounted Shield +3)"; dismount (mount AC back); save and
  reload (one bonus).
