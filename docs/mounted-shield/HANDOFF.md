# Mounted Shield — handoff (KMC-MOUNTED-SHIELD-2026-11-23)

Mission: [docs/missions/mounted-shield/MISSION.md](../missions/mounted-shield/MISSION.md) (packet byte-identical to the
supplied ZIP; `tools/verify_packet.py` passes in place). Acceptance ledger: [ACCEPTANCE_LEDGER.json](ACCEPTANCE_LEDGER.json).

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
