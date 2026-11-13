# Mounted Archery — handoff (2026-11-13)

Branch `codex/mounted-archery` from main `6b6a3e6a` (release 0.2.8, the research pin of the owner's
Mounted_Archery_Mission_Packet, checksums verified). Mission: add the Mounted Archery combat feat, with the supplied
original icon, and the missing mounted ranged-movement penalty that it reduces.

**Status:** source implementation complete, offline checks pass, independent source review done (see the review
section). Native mechanics, native UI and owner art acceptance are NOT RUN: this machine has no KMC install, no
qualified KMC fixture and no `runtime-state\fixture-qualification.json`. No merge, release, version change or install.

## Behaviour

- **The base rule (new for every rider).** In turn-based combat, a mounted rider's ranged weapon attack takes **-4**
  once the mount's normal travel in its current native movement allocation is beyond one Move. With **Mounted Archery**
  the rider takes **-2** instead. Riding needs neither feat; a rider with neither takes the -4. This is a balance
  change for mounted shooters who do not take the feat.
- **Per roll.** Each attack roll reads the travel already made. Earlier rolls are never revised and later movement is
  never restricted.

  | Existing legal sequence | Mounted-movement contribution |
  |---|---|
  | second-Move travel, then shoot | -4, or -2 with the feat |
  | move, shoot, move again | the earlier shot 0; a later shot after the threshold -4/-2 |
  | shoot, then double move | the resolved shot stays 0 |
  | one move at any speed, a five-foot step, a charge, standing still | 0 |
  | real-time attack | 0 (unchanged) |

- **Ranged weapon attacks** only: bows, crossbows, slings, thrown weapons, and firearms delivered as weapon attacks
  (Gunslinger's touch-AC firearms included; no Gunslinger reference). Spell rays, touch spells, kinetic blasts and
  alchemist bombs are excluded, as are melee, natural weapons, the mount's own attacks, unmounted actors and other pairs.
- **The feat.** `MountedArcheryFeature` `b81926bca1d24a3997cdc8471a84014b`: one rank, nonclass, Feat + CombatFeat (the
  stock Power Attack contract), native `PrerequisiteStatValue` SkillMobility 1 (trained ranks, KMC's Ride adaptation)
  and native `PrerequisiteFeature` KMC Mounted Combat (`a7d2e94c6f1b4830b5c9e28d17f3a604`), both in the ALL group. No
  toggle, action, resource or extra attack. The benefit reads the rider's feature fact; the Mounted Combat Reaction
  toggle is unrelated.
- **Player text:** "In turn-based combat, this feat reduces the penalty on your ranged weapon attacks from -4 to -2
  after your mount has used more than one move action's worth of normal movement in its current turn. Five-foot steps
  and charges are not normal movement. The penalty is determined when each attack is made; it does not change attacks
  already resolved. It does not apply in real-time combat." ASCII hyphens: the native font's coverage of U+2212 was not
  checked.
- **Combat log:** the attack breakdown lists "Mounted movement -4" or "Mounted movement (Mounted Archery) -2". These are
  two label keys (`KMC.MountedRangedMovement.Name`, `.Reduced.Name`) rather than one, so that the reduced value names
  its cause without implying that the feat imposes a penalty.

### Tabletop comparison and why this is an adaptation

| Mounted movement | PF1 without feat | PF1 with feat | This mod |
|---|---:|---:|---|
| Stationary or one move | 0 | 0 | 0 |
| Double move | -4 | -2 | -4 / -2 after travel enters the second Move |
| Run | -8 | -4 | **not implemented**: Kingmaker has no Run action |

PF1 resolves mounted shots at the midpoint of the mount's movement. KMC keeps its accepted transport model (the mount
moves, the rider attacks normally in the same activation; rider and mount keep independent budgets). So the penalty is
read at each roll, after travel already made. There is no midpoint scheduling, no Run branch, no real-time window, and
no Mythic Mounted Archery. Acceleration, gait, haste, Charge and the queue method `UnitCommand.Run` are not Run.

## Installed contracts (Kingmaker 2.1.7b, Assembly-CSharp MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`)

Verified from the installed assembly (ILSpy listing) and pinned in `scripts/Test-AssemblyContracts.ps1`:

- **One attack-bonus rule per roll.** `RuleAttackRoll.OnTrigger` (0x06007173) stores its `ACRule`, then triggers one
  new `RuleCalculateAttackBonus` (ctor 0x060071DD), then stores it as `AttackBonusRule`. A critical confirmation reuses
  `AttackBonus`. Manyshot's extra projectile reuses the whole `AttackRoll`
  (`RuleAttackWithWeapon.LaunchProjectiles`). A parry triggers its own rule with the defender as initiator.
- **Additive, log-visible source.** `RuleCalculateAttackBonus.AddBonus` (0x060071DF) adds to the inner target-free rule
  (0x060071D2), which records a `BonusSource(int, Fact)`. The combat log's breakdown lists `BonusSources` by the fact's
  name. The aggregate `AttackBonusPenalty` and every other modifier are untouched.
- **Previews never see it.** The character sheet and tooltips trigger the target-free rule or a free-standing
  `RuleCalculateAttackBonus`. `PowerAttackWatcher` (non-player melee AI) triggers one while its enclosing roll is still
  about to trigger, before `ACRule` exists.
- **Weapon attacks are linked.** `RuleAttackWithWeapon.OnTrigger` sets `RuleAttackRoll.RuleAttackWithWeapon`. Spell
  deliveries (`AbilityDeliverProjectile`, `AbilityDeliverTouch`) trigger bare rolls with proxy weapons.
- **Movement time.** A Move is 3 s of movement time. `TurnController.TickMovement` (0x06000C37) clamps every tick to
  the remaining allowance (`GetRemainingMovementTime`, which bounds an unspent first allowance at the 3 s mark) except
  within `UnitMovementAgent.NearTheEnd` (0.1 m of the endpoint): "moving on borrowed time". Forced-mode time adds to
  `TimeMoved` and `TimeMovedInForceMode`; step time to `TimeMoved` and `TimeMovedByFiveFootStep`. Standing up spends Move
  but no `TimeMoved`.
- **Tick bound.** `TimeController.Tick` (0x060090CF) clamps the TB time scale to 3 and the tick to scale/12, so 0.25 s.
- **Native floor (not changed).** With "dead companions rise after combat" on (`GameDifficulty.TrueDeath` false), the
  native rule lifts a player-faction attack total to at least BAB - aggregate penalty - 2 after every contribution.
  On a low-bonus rider this can absorb part of -4/-2, as it absorbs shooting into melee. The breakdown still lists the
  contribution.

## Design and seams

- `Domain/MountedRangedMovementPolicy.cs` — pure rule. Normal travel = `TimeMoved - TimeForced - TimeStepped`. Second
  Move entered when travel > 3 s + 0.25 s. Travel up to 3.25 s may be one move plus native borrowed endpoint time, so it
  counts as one move; at most 0.25 s (about 1.3 m for a 50 ft horse) of genuine second-Move travel can go unpenalized.
  `IsAllocationCurrent`: same native controller, and either the mount's grant equals the rider's (one paired
  activation, including Delay into a later round) or neither has a grant and the record is of the current round (the
  legacy turn modes). A mount's standalone `native:` grant is never current for a rider (review finding, below).
- `Integration/MountedMovementStateAdapter.RangedMovement.cs` — `TryReadCurrentMountTravel`: the narrow authoritative
  reading. It applies `MaintainLifetimes` like every other adapter entry, then reads; it never creates, replaces or
  writes an allocation. `PeekMovement`/`PeekMountAllocation` remain diagnostic copies.
- `Integration/MountedRangedMovementService.cs` — global `IGlobalRulebookHandler<RuleCalculateAttackBonus>`. Acts only
  when the previous rulebook event is the enclosing `RuleAttackRoll` (same initiator, target and weapon; `ACRule` set,
  `AttackBonusRule` not yet stored), the initiator is the active rider of a Mounted pair, and the attack qualifies. Adds
  one `BonusSource`, refusing a second. Logs one line per turn-based mounted ranged attack (`Mounted ranged movement:`
  with pair, allocation, travel, ownership, outcome, contribution); real-time attacks and a withdrawn rule log nothing.
  A withdrawn rule (feat registered but not offered everywhere) warns once. Faults are logged once and leave the roll
  native.
- `Integration/HorseCompanionBlueprintService.MountedArcheryFeat.cs` — the feat, and two unregistered, componentless,
  hidden label blueprints whose detached `Fact`s are the log sources (never attached to a unit, never saved).
- **Coordinated publication.** Mounted Combat and Mounted Archery share one `ExactAppendOnlyArrayLease` per shared
  selection array (`KmcCombatFeatPublication.Items`: Mounted Combat, then Mounted Archery). Selections are those offering
  both stock Power Attack and Dodge (ordinary, fighter and every combat-bonus list), unchanged. Only `AllFeatures` is
  leased: installed `BlueprintFeatureSelection.Items` enumerates `AllFeatures` (the Ranger lease records the same
  finding). `MountedFeatSelectorRefreshService` keeps its scope: every array with Mounted Archery also offers Mounted
  Combat.
- **Failure containment.** Mounted Archery registers after Mounted Combat. Its fault removes only itself. A Mounted
  Combat fault skips it. A lease fault removes both. `MountedArcheryReady` (feat registered, labels present, every
  leased array still offering it) gates the penalty, so the base rule never runs without an available reduction, and
  foreign drift or mod disable withdraws both.
- **Removal preparation** lists the new GUID in the archive scan and names a live owner, like Mounted Combat.
- **Sprint 6 diagnostic.** Mounted Combat is no longer the last entry. The observer adds `kmcTailExact` /
  `offeredInKmcTail` (the array ends with exactly the KMC tail). The reader accepts old evidence (`lastIsFeat`) or the
  new exact tail; reader checks cover both and refuse an inexact tail. No old verdict changes.

## Lifetime

| Event | Result |
|---|---|
| Selection change, preview, defensive toggle | no change |
| Dismount | no modifier (no active rider); travel retained in the allocation |
| Legal reattachment to the same allocation | its travel still counts |
| Another pair, mount switch, mid-round mount | only travel in this rider's own paired activation; the mount's earlier riderless or other-rider turn never counts |
| Mount on its own separate-initiative turn | not counted (the mount's own `native:` grant); counted again once the pair shares an activation |
| Genuine new native grant (`BeginGrantedPreparation`) | fresh record, travel 0 |
| Delay/resume | same grant, travel kept |
| Save/load | `SavedMovementValues` already persists TimeMoved/TimeForced/TimeStepped with grant and round; no schema change |
| Real time, mode change | no contribution; existing forfeiture/renewal unchanged |
| Encounter end, destroyed actor, area, disable/removal | existing lifetime rules; nothing written |

## Independent review (2026-11-13)

A read-only review against the installed decompile confirmed the seam (previous event is the enclosing roll in every
path, restored contexts included; previews, `PowerAttackWatcher`, parry, crit confirmation, Manyshot and spell rolls
never receive it), the additive bonus, the travel formula, the 0.25 s bound, `MaintainLifetimes` being read-only in
combat, the shared paired grant, registration/lease containment, the detached label facts and the removal scan.

Findings and corrections:

- **Medium, corrected — riderless travel counted for a later rider.** The earlier rule also accepted a mount record
  "of the current round". A horse that double-moved riderless (or under another rider) on its own turn, then was
  mounted later in the same round (`MidEncounterAdoptionPolicy` retains the mount's own slot), would have penalized the
  new rider's shot. Now only a shared paired grant, or the legacy no-grant round records, is current. Consequence: no
  contribution while a pair acts on separate initiative slots (the mount's own grant); conservative, never a penalty
  for travel not attributable to the rider's mounted activation. Regression: `AllocationCurrency`.
- **Low, corrected — log noise.** Real-time and withdrawn-rule attacks no longer log.
- **Low, corrected — silent withdrawal.** One warning when the feat is registered but the rule is withdrawn.
- **Test quality, corrected.** Removed a re-assertion of an unchanged local and hand-added constants; the fresh-grant
  check reads a new production record. The seam and `TryReadCurrentMountTravel` remain unexecuted offline (no game
  types in the test host); native rows cover them.
- **Not applicable.** The reviewer could not see a guarded push helper's asset allowlist; this repository has none.

## Checks (2026-11-13)

| Check | Result |
|---|---|
| `Test-Tiers.ps1 -Tier Fast` (budget `mounted-archery`), after the review corrections | PASS 3/0: build, components 1205/0 (13 new), Validate-Source 151/0; 0.76 min; budget 2.93 of 90 min |
| Mutations (scratch copy, policy only) | each caught: no borrowed-time tolerance, raw `TimeMoved`, feat ignored, any grant current, real time applies, the pre-review round fallback |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 799/3: all 15 new pins pass; the 3 UnityModManager identity failures are pre-existing |
| `Test-Sprint6FeatReaders.ps1` | 84/0 (4 new) |
| DLL (Release, uncommitted tree equal to the commit) | SHA-256 `a3936a6c69b0688c906df549340f54b5619858a885ef696619c565e823d03cd5`, MVID `5cce588b-5913-4da2-99a4-bb4291f701db` |
| Embedded icon | resource `KingmakerMountedCombat.Assets.MountedArcheryFeat.png` in the built DLL, SHA-256 `161aa060…6aeab` (= packet) |

New regressions (`tests/MountedRangedMovementTests.cs`) drive the production `MountedMovementState.Advance` with
native-shaped ticks: the 0.25 s worst borrowed tick at the boundary, split legs, speed change, stand-up cost, a spent
Standard, a step, a charge, auto-stop and second clicks, per-roll ordering, gates, weapon kinds, allocation currency,
persistence/dismount/fresh grant, and coordinated versus independent leases.

**Limits of the offline evidence:** the attack seam, the allocation query and blueprint registration are compiled and
contract-pinned but not executed offline (no game types in the test host). A numerically correct helper does not show
that native attacks reach it.

## Native evidence

| ID | Row | Status |
|---|---|---|
| N1 | feat UI: prerequisites in creation/level-up, ordinary and fighter lists, Back/cancel, icon, text, sheet | NOT RUN |
| N2 | penalty A/B: 0 after one move, -4 / -2 after a double move, defensive toggle off | NOT RUN |
| N3 | timing and full attack: early versus late shot, per-attack contribution, attack count, ammunition, reload | NOT RUN |
| N4 | allocation lifetime: save/load, next grant, dismount | NOT RUN |
| N5 | controls: on-foot ranged, mounted melee, mount attacks, second pair, RT | NOT RUN |
| C | crossbow, thrown weapon, Gunslinger pistol/musket (touch AC), scatter | NOT RUN |

Reason: no KMC install, Working fixture or fixture qualification on this machine. Next step: owner play with the branch
DLL, or a qualified fixture, taking N1–N5 from the packet's ACCEPTANCE.md. The log line `Mounted ranged movement:`
gives the pair, allocation, travel, ownership and contribution for each mounted ranged attack.

## Art

Runtime icon `src/KingmakerMountedCombat/Assets/MountedArcheryFeat.png` (64×64 RGBA, SHA-256
`161aa0604f6967f2d550d0dc4e056f68d15c8a651a8a1380551ec92f11c6aeab`), sprite `KMC_MountedArchery_Feat_Icon`.
Provenance, masters, review sheet, prompts and the packet manifest are in `assets-source/mounted-archery/` (outside the
embedded payload). Owner aesthetic acceptance: PENDING. Native rendering: NOT RUN.
