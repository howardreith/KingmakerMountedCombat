# Exploration repair sprint — review handoff (2026-11-12)

**Status: ENGINEERING PARTIAL — SOURCE CORRECTIONS FOR ISSUES 3–7 AFTER THE OWNER'S REVIEW (REQUEST CHANGES, 4
FINDINGS) ARE IMPLEMENTED AND PASS FAST; NONE IS DEMONSTRATED IN GAME; ISSUES 1–2 PARKED BY THE OWNER; OWNER ACCEPTANCE
SEPARATE.** No merge, release, tag or install. "Fixed in source" below means a source correction with offline tests,
not a demonstrated outcome.

- **Branch:** `codex/kmc-exploration-markers-speed-events`, from main `aade80e5` (the 0.2.2 release merge; the remote
  head was rechecked at intake and was unchanged). Draft PR #27.
- **Heads:** first repairs `2977de16`, first-review fixes `7b964c75`, owner-review corrections in the commit that adds
  this paragraph. Local Release DLLs only; not packaged; version stays 0.2.2.
- **Installed package:** KMC is **not installed** on this machine (`Mods` has no KMC folder). The owner's installed
  DLL is therefore unknown here; nothing was staged, deployed or launched.
- **Owner direction (2026-11-12):** issues 1 and 2 cannot currently be reproduced by the owner and are parked until
  the owner sends details; their log-only observations stay in the build.

## Owner review (request changes) — resolution

| Finding | Resolution | Offline regression |
|---|---|---|
| P1 failed restoration reported as a clean dismount | Every retained/carried restore now goes through one helper. A failed attempt is a clean refusal only when the relationship is observed `Unmounted` with no movement-authority, presentation, prepared-pair or attachment residue (`ClassifyRestore`). Otherwise the faulted relationship keeps its cleanup ownership and retry, and the existing `areaRestoreFailure` latch blocks saving (`NativeCombatBlocksSave` → `CombatRestorationPending`) and new participation until a valid load. A participation-import failure after attachment is also residue. | The real `MountedRelationshipCoordinator.RestoreSaved` with an injected attach failure (rolled back → clean) and attach + rollback failure (Faulted/presentation residue → latch), plus the world-map mode refusal as the clean control |
| P2 suspended intent not saved | New archive member `kmc-retained-intent`, written at the save header barrier only when retained intent exists. It holds campaign, area, native game time, kind, and per pair rider, mount, profile, owner id and generation. It is bounded (4 KiB, 6 pairs) and strictly parsed, and is separate from the strict schema-3 `kmc-mounted-state`, so existing saves and older readers are unchanged. A load reads it at selection and adopts it at native `OnAreaLoaded` only for the exact campaign, area and game time (stale member refused) and never for a pair the mounted-state member already holds. A world-map save keeps the intent until the next local arrival; a local save (the post-rest autosave) restores it in ordinary play. Carried pairs of a pending before-exit transfer are recorded too. Removal preparation drops intent before its cleanup save, and the removal reference scan reads the new member. | Two-pair round trip with a physically mounted control excluded, owner ids and generations preserved, world-map hold then arrival restore, Camp record reload, exact-world adoption and nine malformed or oversized records refused |
| P2 event → camp misses camp rules | `PromoteRetainedIntent` runs before the empty-mounted early return and from the lifecycle for every event mode even when nothing is physically mounted. It updates only this world's retained pairs (ScriptedEvent → Camp; world-map travel unchanged). | Suspend as ScriptedEvent, promote on camp, 8 m separation restores (control without promotion refuses) |
| P2 owner identity discarded | Retained and carried restores use the established lifecycle: `ReleaseSettledAdmissions`, `CreateRestoredBoundary`, `ImportSavedOwnership(ownerId, generation)`, `RestoreSavedPair`, participation restore, `CompleteSavedParticipationImport`; `CancelSavedOwnershipImport` on failure. An import refused because the previous owner has not yet retired is a clean refusal and is retried. Event suspension now also captures settled participation like the area transfer. | Registry: the exact id and generation import after the old owner retires and refuse cleanly while it still holds obligations |
| Uncertain: carried arrival discards transient refusals | Carried arrival now drops only an invalidated pair (missing actor, changed profile or pairing, dead) or an arrival in combat. Any other refusal at `OnAreaLoaded` is retained and retried in ordinary play under the bounded patience. | Arrival decision cases, including the original world-map refusal text |

## Why native evidence is blocked

The guarded native workflow needs the qualified disposable fixture (`KMC_AUTOMATION_BASELINE` / `KMC_AUTOMATION_WORKING`)
and the lab's `runtime-state`. On this machine the save root has neither KMC fixture (only other projects' fixtures and
early-campaign human saves), the lab has no `runtime-state`, and no save sits near Old Sycamore, a campsite with corpses,
or a world-map exit. Human saves are protected and are not rewritten to make evidence. Every native row (M1–M3, S1–S3,
E1–E4, C1–C3, L1–L3, P1–P2, and the world-map rows) is **NOT RUN**.

## Causes and changes, per issue

Native contracts were read from the installed 2.1.7b `Assembly-CSharp` (size matches the recorded fingerprint);
decompiled text stayed in a private scratch directory and is not in the repository. Exact metadata tokens were read by
reflection and re-verified by the reviewer.

### 7. World map refuses every area after leaving mounted (progression blocker) — FIXED IN SOURCE

- **Cause (source + the owner's log):** `BeginAreaTransition` captured the mounted pairs for any destination. On the
  world map, native `GlobalMapRules` starts `GlobalMap` mode during scene init, so the post-`OnAreaLoaded` restore
  refused ("Mounting is available only in Default or Pause game mode"), set `areaRestoreFailure`, and
  `AdmitChargeAreaTransition` then refused every later area change.
- **Change:** a transfer to the world map (custom UI scene = `SceneName.CustomUIGlobalMap`) is marked `WorldMap`; on
  arrival its pairs become **retained intent** (no restore attempt, nothing latched), so saving and area entry stay
  admitted. The next local area's transfer carries them and restores them at arrival without Mount or preparation.
  A carried pair the destination refuses (actor missing, profile changed, not attachable, arrival in combat) stays
  dismounted **on its own**; it never latches the all-pair failure. Only the carried pairs' own control rows travel.
- **Saving:** a save made on the world map records the travel intent in `kmc-retained-intent` (owner review finding
  2), so a cold load followed by entering an area restores the pairs once.
- **Recovery for an already-stuck campaign:** load a save from inside a local area (loading clears the latch).

### 3. Miner's Lever cart dismounts — FIXED IN SOURCE (exact event not observed)

- **Cause (source):** `MountedGameModePolicy` cleanly dismounts on every mode except Default/Pause/FullScreenUi/EscMode.
  A native cutscene that locks control (`CommandLockControls`) enters `Cutscene`; dialogs enter `Dialog`. The exact
  lever blueprint was not inspected (blueprints are not decompiled), so its mode sequence is inferred, not observed.
- **Change:** at the start of `Dialog`, `Cutscene` or `Rest`, the mounted pairs are suspended with a new
  `CleanupTrigger.EventSuspension` (an ordinary dismount) and their intent is retained. Native events can move,
  translocate or hide units (`CommandMoveParty`, `TranslocateUnit`, camp translocation), so the attachment is not kept
  through them. Restoration runs from `Update` only when: same world and campaign, not in combat, not on the world
  map, `Default` is current and no `Dialog`/`Cutscene`/`Rest` is on the mode stack (nested dialogs), no loading, no
  owned save/area boundary, no pending camp assembly, and ordinary play has held for 5 frames and 0.3 s.
- **Per pair:** restored through `RestoreSavedPair` (no public Mount, no native Prepare, no refund, no turn). Dropped
  (stays dismounted, one feedback line) if removed, dead/unconscious, re-paired, already remounted, separated by more
  than 6 m (25 m for a camp) or across navmesh areas; retried for up to 10 s of settled play if transiently not
  attachable. Combat, a world change, loading, mod disable or an update failure cancel the intent.
- **Unchanged:** `MountedGameModePolicy.Classify` still returns CleanDismount for these modes (the attachment never
  survives them); an initially unmounted pair is never captured; a refused suspension keeps the clean dismount.
- **Identity:** restoration imports the captured pair-owner id and generation (owner review finding 4).

### 4. Cannot camp while mounted — FIXED IN SOURCE

- **Cause (source):** native `RestInteraction.OnInteract` gives every party unit (pets included) a `UnitApproachCamp`
  to its own camp slot inside one `RestGroupCommand`, which starts only when every unit succeeds and has no timeout.
  The mounted rider's approach runs on its disabled stock agent and never succeeds, so Rest never starts.
- **Change:** a prefix on exact `RestInteraction.OnInteract` (`0x06001A7B`) suspends the party's mounted pairs with
  retained intent before native issues the orders; rider and horse then walk to their own slots natively. The intent
  holds while the `RestGroupCommand` is pending and while Rest runs, and restores after settled `Default` (rest
  completed or cancelled). Native rest restrictions, supplies, effects, ambushes and costs are untouched; an ambush
  (combat) cancels the remount.
- **Saving:** the native post-rest autosave runs before the settle window; it records the pairs as retained camp
  intent, which reloads and restores once (owner review finding 2).

### 5. Corpse looting — FIXED IN SOURCE

- **Cause (source):** a corpse click issues exact native `UnitLootUnit` (Standard, `ApproachRadius` 1 m) on the nearest
  selected unit. The carried-approach classifier accepted only `UnitInteractWithObject`, so the rider's native approach
  ran on its disabled agent and the pair never moved.
- **Change:** exact `UnitLootUnit` is classified `UnitLoot` → the existing container kind: the mount carries the rider
  into range, the native command then acts and opens the native loot window (rider as looter). The order also binds the
  pair's own earlier travel for the existing single replacement rule. In-range, AI, unmounted and derived commands stay
  native.
- **Native risk:** if dead units still take part in avoidance, the horse could stop short of the 0.5 m delegated radius
  and the bounded approach would cancel. Not observable offline.

### 6. Inventory cat swings around (lowest priority) — FIXED IN SOURCE

- **Cause (source):** native `Familiar.TeleportToMaster` sets `transform.parent = Master.View.transform.parent`; while
  mounted that is KMC's `KMC_RiderPositionAnchor` (a child of the mount view), so after every area load or combat the
  cat is carried and swung by the mount. Also `UpdateMovingToMaster` follows the master's stock path end (null while
  mounted) or entity position (the moving saddle point), re-pathing every frame to a random free spot.
- **Change:** postfix on `TeleportToMaster` (`0x060013C5`) moves a mounted rider's familiar to the mount view's own
  container (world pose kept); prefix on `UpdateMovingToMaster` (`0x060013BF`) runs the native rule with the mount's
  complete path end, or its ground position, as the follow point. Anchor teardown re-parents any foreign child instead
  of destroying it. No spawn, hide, destroy or re-own; inert if any exact member is missing.

### 1 and 2. Extra green marker; horse falls behind — NOT FIXED, DIAGNOSTICS ADDED

- **Findings:** one routed click yields exactly one mount-keyed marker; markers are hidden only when the key's
  `UnitMoveTo`/`UnitAreaTransition` finishes. The rider's speed does not lower the group limit; the gait code is
  presentation only. A tempting native lead (`MoveSelectedUnitsToPoint` gives the current unit with an `AgentOverride`
  the raw click point) was **ruled out**: that `CurrentSelectUnit` exists only for gamepad control.
- **Remaining hypotheses (unproven):** the mount takes the rider's formation slot, laid out for a Medium footprint, so
  it can stop short (its unfinished move keeps the marker) and slow against neighbours under a shared pace cap; or a
  rider-keyed live move command that bypassed routing.
- **Change:** `MountedExplorationObserver` (log only, 4 Hz, Default mode out of combat) writes three keyed lines:
  a rider-held live move command while mounted; the mount stopped short of its marked destination (remaining distance,
  corpulences, nearest other unit); and one pace summary per mount group order (limit, speeds, travelled, still
  fraction, maximum distance from the party centroid). The next owner `output_log` settles the cause.

## Validation

| Check | Result |
|---|---|
| Baseline Fast on `aade80e5` (unchanged) | PASS 3/0, 0.78 min (components 1118/0, Validate-Source 151/0) |
| Fast after the repairs (`2977de16`) | PASS 3/0, 0.29 min (components 1126/0) |
| Fast after first-review fixes (`7b964c75`) | PASS 3/0, 0.13 min (components 1126/0, Validate-Source 151/0) |
| Fast after owner-review corrections | PASS 3/0, 0.51 min (components 1131/0, Validate-Source 151/0) |
| First independent review | 1 high, 5 low/medium findings, all fixed; its finding 8 (new owner identity) is now resolved by owner finding 4 |
| Owner review (request changes) | 4 findings plus 1 uncertain case, all corrected in source with offline regressions (table above) |
| Native rows | NOT RUN — blocked (no fixture, no KMC install, no runtime-state on this machine) |

The owner-review corrections were not given a second independent review; they were self-reviewed against the review
text and the installed contracts. Offline tests exercise domain decisions, the real coordinator, the registry and the
record codec; the integration wiring (save staging, load adoption, Harmony hooks) is verified only by compilation and
review until a native run.

## Issue table

| # | Issue | Source cause | Product change | Offline tests | Native evidence | Owner acceptance |
|---|---|---|---|---|---|---|
| 7 | World map blocks areas | Established (log + source) | Yes, with saved intent | Yes | Not run | Pending |
| 3 | Lever cart dismounts | Mode policy established; lever blueprint inferred | Yes | Yes | Not run | Pending |
| 4 | Cannot camp | Established (source) | Yes | Yes | Not run | Pending |
| 5 | Corpse loot | Established (source) | Yes | Yes | Not run | Pending |
| 6 | Cat swings (lowest) | Established (source) | Yes | Yes | Not run | Pending |
| 1 | Extra marker | Not established | Diagnostics only; parked by the owner | — | Not run | Pending |
| 2 | Horse lags | Not established | Diagnostics only; parked by the owner | — | Not run | Pending |

## Smallest remaining checks

1. Stage this branch's DLL through the guarded workflow on a machine with the Working fixture, or install it for owner
   play.
2. Run these mounted, in an owner session:
   - leave a local area for the world map and enter another area (Old Sycamore → Oleg's);
   - save on the world map, reload it, then enter an area;
   - pull the lever cart;
   - camp through a completed rest, reload the post-rest autosave, and cancel a camp;
   - loot an out-of-range corpse;
   - watch the cat.
3. Issues 1–2: wait for the owner's details, then read the "Mounted exploration observation" log lines.
