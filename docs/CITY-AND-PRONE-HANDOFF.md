# City toolbar, city companion access and prone ranged target: handoff (2026-11-21)

Mission: [MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md). Branch
`codex/mounted-city-and-prone-fixes` from `main` `dbf63702` (0.2.13). Draft PR #57. Development candidate only: no
merge, release, tag, install or native run.

Native contracts below were read from the installed Kingmaker 2.1.7b assembly (MVID `07fa1e4d…`) through the owner's
private local decompilation (read-only reference, nothing imported). The members this change depends on are pinned in
`scripts/Test-AssemblyContracts.ps1` (section "City companion roster" / "Toolbar persistence").

## 1. Mount / Dismount shortcut disappears (toolbar persistence)

**Established cause.** The public control `KMC_MountStateAbility` is a runtime fact KMC leases; a toolbar slot binds
its `AbilityData`. Native `UnitUISettings.GetSlot` and `CleanupBadSlots` replace any ability slot whose fact is
removed or inactive (`MechanicActionBarSlotAbility.IsBad`) with an empty slot, unless a live fact of the same blueprint
exists at that moment. Several ordinary paths make the fact temporarily absent:

- area unload with a mounted pair: `SuspendAreaControls` removes the facts **and empties the KMC slots itself**; only
  the narrow capture/restore rows put the slot back, and they skip carried pairs that stay dismounted or are retained
  for retry ("keeps its current control bindings", which by then are empty) and every row after a failed restoration;
- the capital (`CapitalCompanionLogic.RemoveAllCompanions`) moves every active companion to `RemoteCompanions` and takes
  custom companions out of the game, so their riders leave KMC's candidate roster and lose the fact;
- a save made in the capital writes no KMC row for such a rider (`CapturePersistentSlots` only reads live facts of
  current candidates) while the native save keeps a stale KMC slot.

Once the slot is empty, the one-time placement record stops any re-placement, so the shortcut is gone for good.
Which of these paths the owner hit is not established (no owner log or save); the city path matches the report.

**Fix (existing ownership and persistence path, no second manager).** `MountedControlPlacementIntent` (domain)
remembers each rider's chosen slot by actor id and index only. `NativeMountedControlService`:
- records the live control's slot every reconcile (a move updates it);
- marks the record interrupted whenever KMC removes the facts (area unload, save scope, disable) or the rider leaves
  the candidate roster while still on the player's roster (party, detached or reserve);
- when the control is live again and no slot holds it, puts it back only after such an interruption and only into an
  empty slot; a player who removed the live control, or filled that slot meanwhile, is respected and the record is
  forgotten; one copy only;
- forgets the record on permanent loss (owner no longer gets the control; rider gone from every roster), on disable,
  disposal and a new load;
- empties the KMC slots of a rider that leaves the roster (native would, and no native save holds a KMC identity);
- writes the remembered slot of a temporarily absent rider into the owned save archive as the existing kind-6 row plus
  placement record (only for a rider that resolves uniquely in `Player.AllCharacters`), and seeds records from saved
  rows on load. **No save format change**: same row kinds, same bounds and validation; older builds read these rows
  as ordinary slot rows of an actor they skip when it is not a candidate.

## 2. Animal companions in the city's inventory and character screens

**Established cause.** In a capital the full-screen Inventory, Character and Spellbook screens use `GroupController`
with `WithRemote` and `WithPet`, i.e. `UIUtility.GetGroup(withRemote: true, withPet: true)`
(token `06004281`). It lists every party and reserve companion, in or out of the game, but adds pets only from
`Player.ControllableCharacters`, which requires `IsInGame`. `CapitalCompanionLogic` sets custom companions (and
companions without a capital spawn place or inactive dual companions) out of the game, and `OnIsInGameChanged` takes
their pets out with them. Those owners appear; their animal companions do not. The main character's pet and in-game
companions' pets were already listed natively.

**Fix.** A bounded UI roster projection: an exact-token Harmony postfix on `UIUtility.GetGroup` delegates to
`CityCompanionRosterAdapter` → `CityCompanionRosterProjection` (domain). Only in a capital with both flags (the
full-screen pet roster; Vendor and field rosters unchanged), each listed non-pet owner's own pet is inserted right
after it when the link is reciprocal (`Descriptor.Pet` / `Master`), the pet is not a summon, is one of the player's
cross-scene characters, and the owner is not detached or an ex-companion (native's own pet rules); never twice. The
existing persistent pet instance is listed, so native selection, inventory, doll and character sheet bind that unit.
Nothing changes party membership, ownership, `IsInGame`, scenes, formation, XP or combat. A native-rule failure falls
back to the native list (logged once). Dead pets follow native (not filtered). Selection fallback when a selected pet
leaves the roster is native (`GroupController.UpdateSelectedUnits` picks the first listed unit). Rename keeps working
(eligibility reads `Player.AllCharacters`).

**Shared cause?** The two city issues share a **trigger** (the capital's native roster change, `CapitalCompanionLogic`)
but not a defect: issue 1 is KMC's slot persistence through temporary fact absence; issue 2 is native `GetGroup`'s
in-game pet filter.

## 3. Mounted ranged attack against a prone target

**Established mechanism (code).** An unconscious unit is prone (`UnitProneController.ShouldBeProne`), lying on the
ground. Native `UnitAttack.Init` records `m_IsUnconsciousAtStartOfCommand`; `UpdateTarget` keeps an explicit order on
an enemy that was already unconscious when ordered (and abandons one that falls unconscious later), so an unmounted
player can shoot a downed enemy. The mounted pair path required a conscious target at every gate: the stock-click
classification, the intent driver (`MountedCombatController` → `IsValidTarget` → `CancelInvalidIntent`, silent,
observation `intent-cancel-target-invalid`), the admission context ("Choose one valid living target.") and the pair
command (`ValidateNativeSequenceTarget`, `TryEndExpectedTargetInvalidation`, ranged-tail and routine continuation).
Stage: command admission / intent validation, before any approach or firing; real time and turn based alike (shared
path). Range, line of sight and distance have no prone term in native or KMC (rider eye height only raises the
eye point).

**Fix.** `UnifiedMountedStockAttackPolicy.TargetLifeAdmits` mirrors the native rule; `MountedAttackIntent` records
`TargetUnconsciousWhenOrdered` for a player order only; the controller passes it to the click, driver and admission
gates, and `MountedPairAttackCommand.AdmitTargetUnconsciousWhenOrdered` keeps that exact target valid while it lives.
Unchanged: automatic (party-brain) proposals, routine continuation to the next target, Charge, a target that falls
unconscious after the order (abandoned as native does), dead targets, prone/mounted modifiers, ammunition, action
costs, LoS/range, approach rules.

**Remaining uncertainty.** This explains the report if the "prone" enemy was downed (lying unconscious, as most
lying enemies are). For a conscious, tripped enemy no mounted-specific failure was found in code; candidates needing
native observation are mouse targeting (a conscious prone unit keeps its standing click capsule) and delayed ranged
combat-log presentation. The reported weapon is not identifiable from the available evidence.

## Validation (offline, this candidate)

| Command | Result | Limits |
| --- | --- | --- |
| `.\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1` | see the final receipt | Build, components, Validate-Source, focused wrapper |
| components (`bin\Tests`) | 1427/0 before the token pin | includes the 16 new tests |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` (via wrapper) | new rows pass; GetGroup token pinned `06004281` | reflection-only contracts |
| `Test-GunslingerClickComposition.ps1` (via wrapper) | 21/0, both patch orders | substituted native execution, not gameplay |

`Test-CityToolbarProneFocused.ps1` is a new repository-owned wrapper (the controller's focused gate takes one script
name) that runs exactly the two checks above; nothing broader.

## Acceptance record

Evidence types: UNIT = component test with labeled test doubles of native slots/units; CONTRACT = installed-assembly
reflection; COMPOSITION = real transpilers, substituted engine boundaries; NATIVE = in-game structured evidence.

| Area | Case | Result | Evidence |
| --- | --- | --- | --- |
| Toolbar transitions | Chosen nondefault slot; repeated city entry/exit; interior transition | PASS | UNIT (`MountedControlPlacementIntentTests`) |
| Toolbar transitions | Mounted and dismounted arrival in the real capital | NOT RUN | NATIVE deferred |
| Toolbar persistence | Save/load while the rider is absent; live control writes only its live row | PASS | UNIT |
| Toolbar persistence | Moved shortcut; removed shortcut never re-added; occupied slot not overwritten; no duplicate | PASS | UNIT |
| Toolbar persistence | Another rider independent; unrelated slots unchanged; permanent loss and disable forget | PASS | UNIT |
| Toolbar persistence | Native bad-slot replacement this relies on | PASS | CONTRACT |
| Toolbar persistence | Save/load in and out of the capital, forced dismount, temporary pet unavailability in game | NOT RUN | NATIVE deferred |
| City companion UI | Owner/pet mapping, order, exact instance, multiple pets, no duplicates, repeated opening | PASS | UNIT (`CityCompanionRosterProjectionTests`) |
| City companion UI | Exclusions: summons, non-reciprocal, foreign animals, ex/detached/unlisted owners; only capital pet roster | PASS | UNIT |
| City companion UI | GroupController uses GetGroup; GetGroup pets from ControllableCharacters; exact token | PASS | CONTRACT |
| City companion UI | Equipment inspection and changes on a projected pet; reserve owner in a real capital; save/load | NOT RUN | NATIVE deferred |
| Prone ranged attack | Explicit order on a downed enemy valid while it lives; falls-unconscious-later abandoned; dead cancelled | PASS | UNIT (`MountedDownedTargetOrderTests`) |
| Prone ranged attack | Rider's own shot dispatched, no mount substitution (RT and TB decisions) | PASS | UNIT |
| Prone ranged attack | Matched mounted/dismounted, prone/standing, RT/TB, near and normal distance, actual roll | NOT RUN | NATIVE deferred |
| Negative controls | Brain proposals, Charge and continuation keep the conscious-only rule; downed ally refused | PASS | UNIT |
| Negative controls | Blocked LoS, unavailable actions, out-of-range approach, standing targets, unmounted ranged, mounted melee, mounted penalties | NOT RUN | NATIVE deferred (paths unchanged in code) |
| Shared regressions | Tabletop Rules Expansion click composition, both orders | PASS | COMPOSITION (21/0) |
| Shared regressions | Existing component suite | PASS | UNIT (1427/0) |

## Native checks: deferred to owner verification

Not run. No existing guarded scenario or qualified fixture covers these cases: the capital-arrival scenarios use
Oleg's Trading Post (an `IsCapital` area without `CapitalCompanionLogic`, where companions stay in the party), there
is no fixture with a reserve custom companion owning a pet, and no scenario downs an enemy and issues a mounted
ranged click. Building them is new diagnostic code and readers (a separately selected task). Short owner playtest:

1. Main character or a companion with a Horse: put Mount / Dismount in a non-default slot (e.g. slot 9). Travel to the
   capital (throne room), walk to an interior and back, leave, return to adventuring. The shortcut stays in slot 9.
   Repeat mounted. Save and load inside the capital and outside; the slot holds.
2. Remove the shortcut from the bar; visit the capital and return: it does not come back. Put another ability where
   it was before a visit: it is not overwritten.
3. In the capital open Inventory (I) and Character (C): each mercenary/custom companion's animal companion appears
   after its owner; select it, check its name, statistics and barding/neck slots, equip and unequip an item; close and
   reopen; save, load and reopen. No duplicate portraits.
4. In combat, mounted with a bow: knock an enemy unconscious (it lies on the ground), then click it. The rider shoots
   (attack roll in the log) without the horse walking into melee. Try a tripped, conscious enemy too, and real time
   and turn based. Compare unmounted.

Restoration: no native transaction ran.
