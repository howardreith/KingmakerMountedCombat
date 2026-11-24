# Mounted Shield — source map and implementation hazards

Prepared 2026-11-23. Read-only preparation, not implementation or verification.

## Observed baseline

- Repository: [howardreith/KingmakerMountedCombat](https://github.com/howardreith/KingmakerMountedCombat).
- Default branch: `main`.
- Observed commit: `fb743911200f150af407198c6c28519e522ef58c`.
- Current release recorded by `CURRENT.md`, `AUTONOMOUS-RESUME.md`, and `Info.json`: 0.2.15.
- No open pull requests returned from the repository's open-PR collection at inspection.
- 0.2.15 is an owner-released diagnostic checkpoint for X05 (post-combat save/door lock), not the lock's repair. Native startup/gameplay for 0.2.15 is recorded NOT RUN. Preserve that distinction; this feat mission must not claim to solve X05 or adopt unrelated save logic.
- Current records also retain a UMM 0.33.0/0.33.2 platform-pin discrepancy requiring an owner decision. Do not repin or waive it in a feat task.
- Engine target: Kingmaker 2.1.7b, .NET Framework 4.7, C# 7.3, legacy Harmony12. Explicitly no required dependence on Wrath, CotW, Gunslinger/Tabletop Rules, or Buff Planner.
- GitHub reads succeeded. The available Desktop Commander device was offline, so installed native assemblies, local lab state, builds, runtime, and local push helpers were not inspected or run.

Baseline source: [CURRENT.md](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/CURRENT.md), [AUTONOMOUS-RESUME.md](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/AUTONOMOUS-RESUME.md), [AGENTS.md](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/AGENTS.md), [project](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/src/KingmakerMountedCombat/KingmakerMountedCombat.csproj).

## Concrete production source map

All paths below are relative to that repository/commit. Line numbers are navigation hints for this baseline, not a required patch layout.

| Path and entry point | Observed contract and relevance |
|---|---|
| `Integration/HorseCompanionBlueprintService.MountedCombatFeat.cs`, `RegisterMountedCombatFeatReported` (52), `RegisterMountedCombatFeat` (82), `LeaseKmcCombatFeatSelections` (156) | Creates Mounted Combat first, then Mounted Archery, then one coordinated publication set for both. New Mounted Shield registration belongs in this sequence before leases are captured. A child feat registration failure must leave Mounted Combat and unrelated feats usable. |
| `Integration/HorseCompanionBlueprintService.MountedArcheryFeat.cs`, `RegisterMountedArcheryFeat` (74) | Best existing passive-feat reference: unique GUID, duplicate installed feat detection, stock Power Attack donor contract, original embedded icon, localization, native prerequisites using preview unit, exact registration and cleanup. Has only the feature, no toggle. Do not copy Archery-specific Mobility prerequisite mechanically; Mounted Shield's prerequisites are Mounted Combat and Shield Focus. |
| `Domain/MountedArcheryFeatContract.cs` | `KmcFeatContractFacts` and donor-based checks. Critical historical trap: stock 2.1.7b combat feats have `IsClassFeature = true`; forcing false previously caused total registration failure. Retain donor's value, one rank, Feat + CombatFeat groups, visible UI flags and correct icon. Generalize a small shared check if useful without inventing a new framework. |
| `Domain/KmcCombatFeatPublication.cs` | Existing implementation is explicitly specialized for two feats: `Items(mountedCombat, mountedArchery)`, `Original/MountedCombatOnly/BothFeats`, availability and narrow withdrawal for Archery. It must be extended coherently to the third feat. A second independent array lease would overwrite the others. |
| `Integration/HorseCompanionBlueprintService.cs`, localization dictionary (near 128), `Register` sequence (641), `ApplySelectionDesired` (786), `TryRestoreSelection` (854), `CopyFeatureContract` (1059), `SetFeaturePresentation` (1075), `CreateEmbeddedSprite` (1103), `RollBackPartialRegistration` (1250) | Add new localization/resource/cleanup consistently. Existing sprite loader and owned-object destruction should be reused. New blueprint registration must participate in existing rollback/disposal. |
| `Integration/MountedCombatDefenseService.cs`, `ResolveAttack` (100), `Resolve` (112), `RollCheck` (156) | Precise defense-check seam. Finds the active pair whose mount is the attack target, preserves once-per-round/Swift decisions, then creates a native `RuleSkillCheck(rider, StatType.SkillMobility, attackResult + 1)`. Add the Mounted Shield contribution to this exact rule instance before triggering it through the verified Kingmaker rule API; do not modify the rider's global Mobility stat or add only to a displayed/logged total. |
| `Domain/MountedCombatDefensePolicy.cs` | Existing eligibility/action/debt contract and strict `checkResult > attackResult`. This feat should not rewrite it. Same D20/DC/strict comparison, refusal handling, crit cleanup, own-turn spend, off-turn debt, real-time six-second window, and mode-change accounting stay intact. |
| `Integration/MountedWorldService.cs`, `Pairs` (50), `PairDismounting` (60), `ForActor` (206), `UpdatePairs` (361) | Pair ownership boundary. `ForActor` resolves actual ownership and also admission bindings; callers must still test `RelationshipState.Mounted` and exact rider/mount identity. `Selected` is a presentation projection and must never drive bonuses. Multi-pair behavior is required. |
| `Integration/MountedLifecycleSubscriber.cs` | Existing boundary dispatch for life/death, mount prone, area unload/load, view attachment, removal, in-game state, disable. Preserve its ownership/cleanup contracts. A persistent AC modifier solution needs cleanup tied to real boundaries, including failure/disposal; an event-local solution needs valid live pair reads and UI-equivalent calculation. |
| `CompositionRoot.cs` | Current defense/ranged services are constructed at 83-84, enabled at 205-206, disabled at 255-256, disposed in rollback and final disposal, and defense observes in `UpdateCore` (379). New service lifecycle must be symmetrical. Update has save/area/restoration gates; do not add cleanup solely below a gate that will suppress the necessary cleanup. |
| `Integration/MountedRangedMovementService.cs` | Useful pattern for bounded global rulebook handler + disposable subscription + exact pair + labeled native contribution + duplicate-source guard. This service's attack-only preview exclusion is specific to ranged movement; copying it would be wrong for a passive AC bonus that should appear on the mount's sheet. |
| `KingmakerMountedCombat.csproj` | Explicit source includes and embedded-resource entries. Existing Mounted Combat/Archery icons use `EmbeddedResource` with exact logical names, not external runtime files. New .cs files need includes; a PNG merely copied into Assets is insufficient. |

Paths in the table start at `src/KingmakerMountedCombat/` unless otherwise stated. Source directory: [Integration](https://github.com/howardreith/KingmakerMountedCombat/tree/fb743911200f150af407198c6c28519e522ef58c/src/KingmakerMountedCombat/Integration); [Domain](https://github.com/howardreith/KingmakerMountedCombat/tree/fb743911200f150af407198c6c28519e522ef58c/src/KingmakerMountedCombat/Domain).

## Implementation constraints inferred from the actual code

1. Use one production-owned bonus policy/read path for both benefits. At any moment it must read the actual current active rider, feat ownership, effective wielded shield and permitted base shield contribution. A snapshot taken at mount time would permit equip/swap exploits and stale bonuses after retraining.
2. Verify the installed Kingmaker shield item/armor modifier, Shield Focus blueprint, effective shield suppression, shield-typed AC and RuleSkillCheck contribution APIs locally before choosing the narrow seam. These engine members were not established by repository reads. Do not assume Wrath's APIs, use localized text matching, sum the rider's total shield AC, or subtract a guessed enhancement from unrelated modifier totals.
3. Native check bonus must exist before `Rulebook.Trigger<RuleSkillCheck>`. The existing record uses native `check.Bonus`, `check.RollResult`, `check.DC`; use a named source so evidence/logs reconcile with mechanics. A manual addition only to `record.CheckResult` is incorrect.
4. Passive mount AC must be effective in ordinary attacks and inspectable on the mount's character sheet/tooltip. If choosing event-local `RuleCalculateAC`, prove its actual UI consumers and shield-stacking behavior; if choosing an owned modifier, prove prompt refresh, exact ownership cleanup, disabled/unmounted transitions and no saved/doubled residue. Do not prescribe an unverified engine API in the mission.
5. Existing Mounted Combat toggle controls reaction preference. Turning it off prevents the reaction/check; it must not remove Mounted Shield's passive AC. No new visible ability or toggle is needed.
6. Extending publication must keep native selection arrays coherent: fixed deterministic append order, each feat once, preserve foreign entries and order, independent registration failures, required ordinary/fighter selection membership, supported extra combat selections, narrow removal of only the unavailable owned feat, in-place foreign modifications, disable/re-enable and exact restoration when the array is still an owned value. Losing Shield from a required path must not disable Archery; losing Archery must not disable Shield. Do not tie Shield availability to `IsMountedArcheryAvailable`.
7. Keep rider reaction state rider-owned: swapping mounts, shield, feat, mode, or save/load must not refund/recharge the underlying Mounted Combat use or Swift action. The new feat contributes a value; it does not introduce new spending.
8. Preserve current known issues and existing diagnostics. Do not attempt unrelated repairs or broaden to save/projectile redesign, mounted scheduling, unmounted rules or other feats.

## Existing tests and efficient validation

- `tests/MountedCombatDefensePolicyTests.cs` tests strict comparison, refusals, own/off-turn spending, once-per-round renewal, debt, real-time windows and mode changes.
- `tests/MountedRangedMovementTests.cs` includes coordinated feat publication, explicit proof that independent leases conflict, foreign append/reorder, required/non-required list loss, withdrawal across foreign arrays, in-place reintroduction, and disable/re-enable.
- `tests/MountedArcheryFeatContractTests.cs` covers donor-shaped combat-feat contract.
- `scripts/Test-MountedArcheryFeatContract.ps1` is particularly valuable: reads stock feat metadata from pinned installed Kingmaker assets, invokes compiled production construction/validation/publication on genuine blueprint objects. Its scope explicitly excludes Unity object creation, live library, native prerequisites and level-up UI. Extend/reuse the mechanism for Mounted Shield rather than copying a disconnected model of the logic.
- `scripts/Test-Sprint6FeatReaders.ps1` and `scripts/runtime/Sprint6FeatEvidence.ps1` are existing reaction evidence helpers; use affected portions only after inspecting exact current dependencies.
- `scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MountedShieldFocused.ps1` is a proposed focused command only after that script is actually created and behavior-bound. The existing target is `Test-MountedArcheryFeatContract.ps1`; it is appropriate as a directly affected neighbor when donor/publication code changes.
- `Test-Tiers.ps1` accepts comma-separated focused names, rejects missing/zero/traversal/link selections, and runs the component runner. Candidate includes Fast: choose one, not both on identical bytes. Fast forbids an explicit AssemblyTarget; Candidate may select Kingmaker.
- Production solution builds with installed MSBuild/game references. A Linux textual audit is not build/native evidence.

High-value new behavioral coverage:

- Actual shield-derived value used in both paths; mundane/enhanced variants with equal base produce equal contribution.
- Equip, unequip, switch weapon sets and replace shields while staying mounted; read changed value immediately on next relevant calculation.
- Feat acquisition/removal/retraining and shield suppression; all actual adapters use the production policy.
- Two active pairs with different shields; bonus goes only to each matching mount.
- Mount swap/dismount/death/area/disable/dispose/reload removes or rebuilds exactly once.
- Passive AC with reaction preference off and after the once-per-round check is spent.
- Ordinary AC vs touch/flat-footed and competing shield bonus behavior according to verified rule contract.
- Native check equality fails; one higher succeeds after including Shield; unrelated Mobility checks remain unchanged.
- Failed checks still spend exactly the existing use/action; no extra reaction attempted.
- Donor flags, both prerequisites through native preview unit, cancellation, respec, ordinary/fighter lists, and new icon binding.
- Third-feat publication regression cases above, keeping preexisting Archery tests green.

Offline policy tests alone cannot prove equipment reading, shield suppression, native stacking, serialization cleanup, or actual feat UI. Use meaningful integration-style tests through production adapters with real available native objects/thin external boundaries, not source-regex tests as behavior proof. Do not expand to broad unrelated campaigns.

## Runtime and package boundaries

[AGENTS.md](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/AGENTS.md) and [VALIDATION-POLICY.md](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/docs/VALIDATION-POLICY.md) require guarded repository/lab runtime scripts, one native executor, real current-state backup, exact restoration of exposed surfaces, protected human saves, isolated qualified disposable fixtures, independent coherent-change review before live use, and truthful source/offline/native/art distinctions. Budget exhaustion never interrupts restoration. Ordinary validation is planned at 15 minutes with a 30-minute ceiling; inspect current cumulative budget before running. Full historical campaigns are not selected by an ordinary feat checkpoint.

Current records say the prior tasks lacked a qualified KMC Working fixture on that machine. Claude must inspect live local readiness, not assume this remains true or fabricate an available fixture. If blocked, finish all source/offline work and hand off precisely named NOT RUN native rows. Do not launch/modify Wrath, change installed loader, waive a platform pin, install for normal play, merge, tag or release.

`scripts/Package.ps1` explicitly refuses `-SkipBuild` and delegates to qualification packaging. Do not promise package-creation reuse flags it does not support. The present mission ZIP is preparation material, not a compiled mod ZIP. The checked factory workflow is in `FACTORY_WORKFLOW.md`.

No push helper exists in this repository's current tree. Historical `Push-KingmakerFeatureBranch.ps1` may be lab-owned; discover it locally. AGENTS permits an ordinary push only after payload, clean-tree, approved-origin and fast-forward audit when no project-owned helper is available. Controller initial enrollment/push requirements must follow the new factory workflow, not historical PR57/59 manual continuation rules.

## Icon evidence and source references

The following repository-owned references were retrieved and visually inspected during package preparation. They are source references, not additional assets distributed in this packet:

| Inspected reference | Dimensions/mode | SHA-256 |
|---|---|---|
| `MountedCombatFeat.png` | 64×64 RGBA | `328f8e6ab637cdce11bf518cd19b2bf886382eb70e4ba63f104717e99c2ced9a` |
| `MountedCombatFeat.512.png` | 512×512 RGBA | `387f7f3c5ce2f416dbe4e0f7150eec6b03ae68cd78875246cda307cc2279605f` |
| `MountedArcheryFeat.png` | 64×64 RGBA | `161aa0604f6967f2d550d0dc4e056f68d15c8a651a8a1380551ec92f11c6aeab` |
| `MountedArcheryFeat.512.png` | 512×512 RGBA | `d3dab7274f5a9a8a77d2730dc04e16d5a54f7f2deacb69c973e6cd40350b4de3` |

MountedCombatFeat.512.png Git blob: `798d682ad55138a594e4e8d64a8adbda176119a3`.
Source: [Mounted Combat 512 reference](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/assets-source/finalize-f1/MountedCombatFeat.512.png), [Mounted Archery art/provenance](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/assets-source/mounted-archery/ART.md), [existing export manifest](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/assets-source/mounted-archery/icon-manifest.json).

Art guide: flat muted terra-cotta circular combat emblem, transparent corners and internal negative space, broad readable silhouette, no baked square frame, no resource/selected/disabled overlay. The older Rapid Reload reference used #A6533F. For THIS mission, Howie supplied Mounted Archery as the authoritative color reference; the final uniform ink is #9B4D3A as documented in ART.md. The older reference must not override that correction. Existing Mounted Combat already depicts rider, horse and shield; distinguish Mounted Shield with a more prominent protective shield composition.

Recommended integration identity (new proposal, no GUID assigned by this audit):

- Runtime file: `src/KingmakerMountedCombat/Assets/MountedShieldFeat.png`.
- Embed logical name: `KingmakerMountedCombat.Assets.MountedShieldFeat.png`.
- Sprite name: `KMC_MountedShield_Feat_Icon`.
- Source/provenance: `assets-source/mounted-shield/`.
- Localization: `KMC.MountedShield.Name` / `KMC.MountedShield.Description`.
- Blueprint: `MountedShieldFeature`, fresh stable GUID with collision check before first registration.
- One runtime PNG only; high-resolution master/review sheet excluded from runtime embedding.

Existing loader: `Texture2D(2,2,TextureFormat.ARGB32,false)`, Clamp, Bilinear, aniso 1; `ImageConversion.LoadImage(...,true)`; full rect Sprite centered at (0.5,0.5), 100 pixels/unit, FullRect, HideAndDontSave. Thus 64×64 8-bit RGBA PNG is the established consumable format; no DDS, Unity editor import, asset bundle or extra image library is required. Runtime file should be copied byte-for-byte and embedded; the build must not generate artwork or require ImageMagick.

