# Phase 3D Unified Mounted Turn Contract

Status: BLOCKED — CRITICAL

Dev.20 focused Mammoth observation confirms an important distinction: under the unified model, raw `mount.CombatState.CanActInCombat` may be false precisely because the mount's redundant native turn is suppressed. That raw flag is not by itself authority to reject a separately ledgered mount action during the rider-owned shared turn. Dev.21 diagnostic schema v55 therefore records raw native actionability unchanged and separately requires pair-local shared admission, the rider as current native turn principal, and the mount's own available Standard. The rider turn remains active after a mount-only Standard action. This is an evidence-fixture correction, not a production controller or action-ledger change.

Fresh dev.21 runtime proves that admission is not execution. Kingmaker accepted the in-range Mammoth attack into the Mammoth Standard slot while the rider owned the shared turn, but left it unstarted for the complete 30-second outcome bound. Exact installed `UnitActionController` advances a non-AoO TB command only when its executor is `CurrentTurn.Unit`. Keeping the rider principal and keeping the mount as command/resource/rule owner cannot both be expressed through the stock scheduler. Wrath's two-actor turn controller and paired command links are the missing primitives.

The remaining implementation options require a new architecture mission: manually drive a non-current actor command, introduce a synthetic/proxy command while preserving exact ownership, or replace a broader portion of the TB controller. None is authorized after the bounded repair cycle. `EnableUnifiedMountedTurn=false` preserves the accepted separate-turn fallback; the unified model receives no alpha qualification credit.

## Authority and boundary

This contract implements the user-authorized Phase 3D private alpha on `codex/mounted-combat-phase3d-unified-combat`. The accepted Phase 3C branch and package remain immutable evidence. This contract does not authorize a shared Standard-action pool, persistent mounted state, mounted spellcasting, charge, feats, enemy riders, Paladin Divine Steed, or a public release.

The rider is the mounted pair's initiative, selection, portrait, action-bar, and camera principal. Rider and mount retain distinct native `UnitCombatState.Cooldowns`, command containers, attack actors, and rule events.

## Exact installed Kingmaker seams

Authority is `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

| Responsibility | Exact local member | Phase 3D contract |
|---|---|---|
| Ordered roster | `CombatController.SortedUnits` `0x06000BC7` | Keep the native roster intact; suppress only the active mount's redundant turn and tracker view. |
| Next actor | private `CombatController.ChooseNextUnit` `0x06000BD2` | A scoped postfix may advance past only the exact active mount while merged. No unrelated actor may be skipped. |
| Turn construction | `CombatController.StartTurn(UnitEntityData)` `0x06000BDA` | The rider remains the real `TurnController.Unit`; no synthetic unit is created. |
| Rider turn setup | `TurnController.Prepare()` `0x06000C3C` | Initialize the mount ledger once alongside a naturally starting rider turn; never clear it during a mid-turn merge. |
| Turn continuation | private `TurnController.ContinueActing()` `0x06000C3D` | Keep the rider-led turn open while either exact actor has a legal remaining action or a pair command is active. Explicit native End Turn remains authoritative. |
| Movement accounting | private `TurnController.TickMovement(ref float,bool)` `0x06000C37` | Physical motion remains the mount's; Phase 3D transfers ordinary Move cost to the mount ledger and retains exact five-foot-step state. |
| Explicit end | `TurnController.ForceToEnd(bool)` `0x06000C47` | Never invoke merely because the rider spent its resources; honor only player/native end or terminal shared-turn state. |
| Initiative events | `CombatController.HandleUnitRollsInitiative` `0x06000BEE` | Pair placement and visible initiative use the rider result/bonus. Mount state is synchronized without refreshing actions. |
| Tracker projection | private `InitiativeTrackerVM.UpdateUnits()` `0x06004F0E` | The exact method returns without populating when `CurrentTurn?.Unit` is null. Observe only after a stable native rider turn exists; then remove only the exact active mount VM so the current entry and portrait remain the rider. |

Kingmaker has no native rider/mount pair fields in `TurnController`. The implementation must therefore be a pair-local coordinator behind exact-token Harmony12 adapters, not a global replacement combat controller.

Exact dev.6 transition attribution confirms the tracker constructor calls `UpdateUnits()` immediately, and that method exits before enumerating `SortedUnits` when there is no current turn. A newly constructed empty VM at the RT-to-TB initialization boundary is therefore not evidence that projection failed. Qualification waits for Kingmaker to consume its native pending next-unit handoff, establishes at most one public `StartTurn(rider)` request if that settled turn belongs to another fixture actor, requires two stable rider-turn frames, and only then constructs and inspects the tracker. No private next-unit, status, initiative, cooldown, or tracker collection is written.

## State model

The coordinator owns only transient, nonserialized control state:

- exact rider and mount identities;
- merge round and initiative observation;
- pending split boundary after dismount;
- mount movement/five-foot-step measurements needed because Kingmaker exposes only rider movement stats;
- exact turn preparation generation, so both actors initialize once;
- skip, tracker-filter, and turn-retention cardinalities;
- independent rider/mount resource snapshots before and after each action.

It does not own HP, weapons, ammunition, action cooldowns, initiative rolls for unrelated units, or a replacement turn roster.

## Merge and split

### Combat starts while mounted

1. Retain the rider's real initiative result and bonus as the pair authority.
2. Synchronize the hidden mount initiative placement to the rider only as needed for native ordering and RT/TB conversion.
3. Choose the rider once and skip only the redundant mount candidate.
4. Prepare rider and mount cooldown/new-round state once.
5. Display one tracker entry using the rider portrait.

### Mount during combat

Mount is admitted only on the rider's current turn with a rider Move action available. The transition preserves both actors' existing Standard, Move, Swift, and Initiative cooldowns. It synchronizes placement and suppresses the redundant mount entry without starting, clearing, or granting a turn.

### Dismount or invalidation during combat

The current round's spent resources remain untouched. The former mount remains suppressed through the current round and returns to separate participation at the next safe native round boundary. Death/removal may let native roster removal supersede this pending split. No immediate second turn is created.

## Shared-turn completion

The rider-led turn remains actionable while either actor can legally act. Rider Standard/Move and mount Standard/Move are never interchangeable. A stock pair attack may sequence rider then mount; explicit Rider Primary or Mount Primary spends only that actor's Standard action. Mount movement spends only mount movement time/Move action. Native End Turn ends the shared turn even if resources remain.

## Safety and fallback

The coordinator is enabled only for the exact active supported pair and exact installed MVID. Every reflection member is token/name/signature checked before patches install. A mismatch prevents the coordinator from installing.

If one scoped implementation plus one attributable repair cannot prevent duplicate/skipped turns or action refresh, retain the accepted Phase 3C separate-turn behavior behind a fallback setting, preserve evidence, and record `BLOCKED — CRITICAL`. Do not layer broader global turn-controller patches.

## Qualification gates

Credit requires real input and rendered tracker evidence in addition to internal state. Runtime telemetry must prove one rider entry, rider initiative authority, separate cooldown deltas, no mount turn start, no unrelated skip, merge/split resource preservation, exact event cardinality, and RT/TB reconciliation. Pointer and portrait usability remain a focused manual gate when they cannot be automated truthfully.
