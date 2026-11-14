# Mounted Archery registration repair — handoff (2026-11-13)

Branch `codex/mounted-archery-registration-repair` from main `fef7b71e` (release 0.2.9). Scope: the player report of a
missing Mounted Archery feat. No merge, release, version bump or install.

## Report

- **Player setup:** Kingmaker 2.1.7b, UMM 0.33.0, KMC 0.2.9 active, with Call of the Wild and Tabletop Rules Expansion.
- **Feat screen:** a ranger at the level-9 ordinary feat choice already has Mounted Combat. Mounted Archery is absent,
  including from the unavailable feats.
- **Log:** `[KingmakerMountedCombat] [Exception] Mounted Archery feat blueprint registration: InvalidOperationException - Mounted Archery did not retain the single-rank combat-feat contract.`
- **Local logs:** the player's full log is not on this machine. The local `output_log.txt` holds no KMC line, because
  KMC is not installed here.

## Cause (installed game data plus production code on genuine blueprints; not observed in the player's process)

The 0.2.9 validator rejected `IsClassFeature == true`. Mounted Archery copies that flag from stock Power Attack, and
stock Power Attack holds `true`.

- **Construction:** `CopyFeatureContract(powerAttack, feature)` copies `Groups` and `IsClassFeature` and sets `Ranks = 1`.
  `SetFeaturePresentation` writes `m_Icon` (token 0x04006957).
- **Stock Power Attack values:** `PowerAttackFeature` `9972f33f977fc724c838e59641b2fca5` holds Groups `[CombatFeat, Feat]`,
  Ranks 1, `IsClassFeature` **true**, no Hide flags and a stock icon.
  - It is pathID 82145, offset 154062912, 252 bytes, in the pinned `sharedassets1.assets`
    (`cc779caf…`, the same pin as `Test-ChargeBuffContracts.ps1`).
  - Its script pointer resolves through MonoScript 6001 to `Kingmaker.Blueprints.Classes.BlueprintFeature`.
  - Field order comes from the installed Assembly-CSharp (MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`, SHA-256
    `3b6450ff…`). Unity aligns every C# `bool` field to 4 bytes.
  - The decoding ends exactly at the object's serialized end, which confirms the layout.
- **Why the other conditions held:**
  - The groups come from the same donor that Mounted Combat's registration already checks for Feat + CombatFeat.
  - Ranks is forced to 1.
  - `BlueprintUnitFact.get_Icon` returns `m_Icon` directly, and no subclass overrides it.
- **Effect:** `IsClassFeature` was the only failing invariant. Every installation with stock Power Attack threw, and
  the handler then removed the feat. This needs no other mod.
- **Live values:** not inspected in the player's process. `Test-MountedArcheryFeatContract.ps1` reproduces the failure
  on a genuine `BlueprintFeature`: it builds the feat with the product's own construction from the decoded donor, and
  the 0.2.9 condition rejects it.

## Native contract: `BlueprintFeature.IsClassFeature` in 2.1.7b

The flag does not mean "granted by a class".

- **Stock values:** every stock feat checked holds `true`. That covers Power Attack, Dodge, Mobility, Point Blank Shot,
  Rapid Shot, Toughness, and, in the offline decode, Precise Shot, Deadly Aim, Combat Reflexes and Iron Will. Class
  features such as Sneak Attack and Fighter Proficiencies hold `true` as well.
- **Native readers:** there are only two, both lambdas in `Player.RespecCompanion` (methods 0x0600A392 and 0x0600A3AF).
  - A feature counts as rebuilt if it has a source progression, class, race or item, or `IsClassFeature`. The other
    features are carried over to the respecced unit as extra facts.
  - Features with `!IsClassFeature` that the new unit lacks are handled separately.
  - So `true` means "rebuilt by the respec, not carried over". That is the stock feat behaviour.
- **KMC's own feats:** Mounted Combat already holds the copied value. Mounted Archery now matches it, Power Attack and
  every stock feat.

## Change

- **`Domain/MountedArcheryFeatContract.cs` (new):** the contract over plain facts.
  - Groups contain Feat and CombatFeat.
  - Ranks is 1.
  - `IsClassFeature` equals the stock donor's value. It was "must be false".
  - The icon is KMC's own sprite.
- **Integration, `HorseCompanionBlueprintService.MountedArcheryFeat.cs`:**
  - Construction and validation are `BuildMountedArcheryContract` and `MountedArcheryContractViolation`. They are
    internal static, so the focused script runs the same code. Construction is unchanged in effect.
  - `CopyFeatureContract` is untouched, so the companion features built with it are not affected.
- **Unchanged:** identity, groups, rank, icon, localization, prerequisites (one trained Mobility rank + KMC Mounted
  Combat, native ALL group), publication, withdrawal, the penalty gate and every movement and penalty calculation.
- **Visibility flags:** reported, not newly enforced. Stock Power Attack is not hidden.

## Diagnostics

- **On failure:** the exception names every failed invariant with its observed and expected values, then both fact
  sets (groups, ranks, `IsClassFeature`, icon absent/KMC/other, the three Hide flags). The existing message prefix is
  kept. Example:
  `Mounted Archery did not retain the single-rank combat-feat contract: ranks=2 (expected 1); icon is not KMC's Mounted Archery icon. Feature groups=[CombatFeat,Feat] ranks=2 IsClassFeature=True icon=other HideInUI=False …; donor PowerAttackFeature groups=[CombatFeat,Feat] ranks=1 IsClassFeature=True icon=other …`
- **On success:** one line per registration:
  `Mounted Archery feat contract: groups=[CombatFeat,Feat] ranks=1 IsClassFeature=True icon=KMC …; donor PowerAttackFeature …`.
  The existing summary still follows: `Mounted Combat feat selections: …; Mounted Archery offered; failure=none.`
- **Logging volume:** no per-frame logging.

## Regressions

- **`tests/MountedArcheryFeatContractTests.cs`:** 4 cases in the component runner. Input is the decoded stock donor
  facts.
  - The 0.2.9 rule rejects the stock-built feat, and the corrected contract accepts it.
  - The feat follows its donor's flag, including a changed donor.
  - Each invariant's diagnostic is checked.
  - Several failures are reported together.
- **`scripts/Test-MountedArcheryFeatContract.ps1`:** focused, 54 checks.
  1. Decodes six stock donors from the installed data. It checks pinned file identities, exact offsets and sizes from
     the serialized object table, MonoScript, name, GUID and exact end-of-object.
  2. Loads the built product DLL and the game's Assembly-CSharp in .NET Framework and creates genuine
     `BlueprintFeature`/`BlueprintFeatureSelection` objects without Unity. In this UnityEngine.CoreModule, `Equals`
     between two non-null objects is reference identity, so they compare as live objects do.
  3. Runs the product's `BuildMountedArcheryContract`, `MountedArcheryContractViolation`, `CopyFeatureContract` and
     `KmcCombatFeatPublicationSet` on those objects. Coverage:
     - The 0.2.9 condition rejects the stock-built feat. The product contract accepts it.
     - The feat is a visible single-rank Feat + CombatFeat with the stock class flag, KMC icon and base-game DLC.
     - Failures name their invariants.
     - Publication offers Mounted Combat then Mounted Archery once in the ordinary, fighter and another list. Foreign
       entries and order are kept, and disable restores the exact originals.
     - Availability, the ranged penalty's gate, is true. Without Mounted Archery, only Mounted Combat is offered and
       the penalty stays off.
- **Not covered offline:**
  - Unity object creation and the live blueprint library: `CreateOwned`, `AddExactBlueprintRegistration`, the
    `ResourcesLibrary` lookup of Power Attack in a modded library.
  - The level-up UI.
  - Native prerequisite evaluation.
  - The real `LeaseKmcCombatFeatSelections` target discovery (selections offering both Power Attack and Dodge).
- **Mutation check:** the 0.2.9 rule (`if (feature.IsClassFeature)`) was restored temporarily.
  - Components failed 4 (1213/4).
  - The focused script failed at "the product contract accepts the feat built from stock Power Attack".
  - The corrected file was then restored.

## Checks (budget `mounted-archery-registration`)

| Check | Result |
| --- | --- |
| Fast + focused, first run | build PASS; components 1217/0; Validate-Source 151/0; focused FAIL (script defect: PowerShell's adapter refuses blueprints with `name`/`Name`, fixed with reflection) |
| Fast + focused, mutation | build PASS; components 1213/4 (expected); the focused script, run directly on the mutated DLL, failed as expected |
| Fast + focused, final tree | `scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-MountedArcheryFeatContract.ps1 -BudgetId mounted-archery-registration`: TIER FAST PASS=4 FAIL=0 in 0.58 min (build 25 s; components 1217/0; Validate-Source 151/0; focused 54/0) |
| Product checked | `bin\Release\KingmakerMountedCombat.dll` SHA-256 `89761e1915b2d69f2b19f2f13a65ea128e6a6cb11ffc5a713ee7ee1a85148b20`, MVID `f5a7155e-8552-49a0-a857-185645bfc46c`; not packaged or installed |
| Budget | 1.9 of 90 min, 3 entries, none interrupted |
| Assembly contracts (Candidate) | not run: nothing native is newly pinned in Test-AssemblyContracts; the focused script binds its own tokens and identities |

## Native verification: NOT RUN

**Missing capability:** this machine has no KMC installation, no qualified KMC Baseline/Working fixture and no
`runtime-state\fixture-qualification.json`. The installed game is the owner's modded installation. Nothing was staged,
launched or restored. Compilation and offline passes do not show the in-game feat screen repaired.

| Row | Needed evidence | Result |
| --- | --- | --- |
| R1 | Fresh start: no Mounted Archery registration exception; `Mounted Archery feat contract:` line present | NOT RUN |
| R2 | Startup summary `Mounted Archery offered; failure=none` | NOT RUN |
| R3 | Eligible ranger (Mobility 1 rank, Mounted Combat) sees and selects it at an ordinary feat choice | NOT RUN |
| R4 | Offered in the fighter bonus-feat selection | NOT RUN |
| R5 | Mobility and Mounted Combat prerequisites pass when met and refuse when either is missing | NOT RUN |

The quickest owner check: install the branch DLL, start the game, and search the log for `Mounted Archery feat
contract:` and `Mounted Archery offered`. Then open the level-9 ranger feat choice. Rows N1–N6, C and C2 of
[the Mounted Archery handoff](MOUNTED-ARCHERY-HANDOFF.md) remain NOT RUN; they were unreachable while the feat was
removed.

**Remaining uncertainty:** the player's live values were not read. The cause is established from the installed stock
data, which matches the player's game version, and from the product code on genuine blueprints. Another mod could
change Power Attack in a player's library. The new diagnostics name the failed invariant and both fact sets if that
happens.
