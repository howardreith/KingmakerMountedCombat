# Exploration repair sprint — review handoff (2026-11-12)

**Status: ENGINEERING PARTIAL — SOURCE REPAIRS FOR ISSUES 3–7 READY FOR REVIEW; ISSUES 1–2 DIAGNOSED ONLY; NO
NATIVE EVIDENCE (BLOCKED ON THIS MACHINE); OWNER ACCEPTANCE SEPARATE.** No merge, release, tag or install.

- **Branch:** `codex/kmc-exploration-markers-speed-events`, from main `aade80e5` (the 0.2.2 release merge; the remote
  head was rechecked at intake and was unchanged).
- **Product head:** `7b964c75`. Built DLL (Release, local, not packaged) SHA-256
  `5e84f835c602632dff8494b2c43eac728a1aa31f3a2469ea516f5436221d4bee`. Not a release build; version stays 0.2.2.
- **Installed package:** KMC is **not installed** on this machine (`Mods` has no KMC folder). The owner's installed
  DLL is therefore unknown here; nothing was staged, deployed or launched.

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
- **Limit:** a save made **on the world map** does not record the travel intent; loading it gives unmounted pairs
  (mount once). The archive format is unchanged.
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
- **Identity:** an in-area restore re-admits the same rider and mount (ids, names, profile) through the ordinary
  boundary admission; the internal pair-owner id/generation is new (reviewer finding 8, no entitlement impact).

### 4. Cannot camp while mounted — FIXED IN SOURCE

- **Cause (source):** native `RestInteraction.OnInteract` gives every party unit (pets included) a `UnitApproachCamp`
  to its own camp slot inside one `RestGroupCommand`, which starts only when every unit succeeds and has no timeout.
  The mounted rider's approach runs on its disabled stock agent and never succeeds, so Rest never starts.
- **Change:** a prefix on exact `RestInteraction.OnInteract` (`0x06001A7B`) suspends the party's mounted pairs with
  retained intent before native issues the orders; rider and horse then walk to their own slots natively. The intent
  holds while the `RestGroupCommand` is pending and while Rest runs, and restores after settled `Default` (rest
  completed or cancelled). Native rest restrictions, supplies, effects, ambushes and costs are untouched; an ambush
  (combat) cancels the remount.
- **Limit:** the native post-rest autosave runs before the settle window, so that autosave records the pairs
  unmounted.

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
| Fast after review fixes (`7b964c75`) | PASS 3/0, 0.13 min (components 1126/0, Validate-Source 151/0) |
| Independent review of the production diff | 1 high, 5 low/medium findings; all fixed except finding 8 (documented) |
| Native rows | NOT RUN — blocked (no fixture, no KMC install, no runtime-state on this machine) |

Budget record `exploration-repair`: 1.2 of 90 min. New offline tests: `tests/ExplorationRepairTests.cs` (7 groups) and
one corpse-loot group in `Finalize4OrdersAiTransitionsTests`. Candidate was not run: no native candidate was frozen.

## Issue table

| # | Issue | Source cause | Product change | Offline tests | Native evidence | Owner acceptance |
|---|---|---|---|---|---|---|
| 7 | World map blocks areas | Established (log + source) | Yes | Yes | Not run | Pending |
| 3 | Lever cart dismounts | Mode policy established; lever blueprint inferred | Yes | Yes | Not run | Pending |
| 4 | Cannot camp | Established (source) | Yes | Yes | Not run | Pending |
| 5 | Corpse loot | Established (source) | Yes | Yes | Not run | Pending |
| 6 | Cat swings (lowest) | Established (source) | Yes | Yes | Not run | Pending |
| 1 | Extra marker | Not established | Diagnostics only | — | Not run | Pending |
| 2 | Horse lags | Not established | Diagnostics only | — | Not run | Pending |

## Smallest remaining checks

1. Stage this branch's DLL through the guarded workflow on a machine with the Working fixture, or install it for owner
   play.
2. Owner session: leave a local area mounted → world map → enter another area (Old Sycamore → Oleg's), save and reload
   on the world map; pull the lever cart mounted; camp mounted through a completed rest and a cancel; loot an
   out-of-range corpse mounted; watch the cat; then send `output_log.txt`. Its "Mounted exploration observation" lines
   identify the marker/pace cause for issues 1–2.
3. Then fix 1–2 at the seam the observations name, and run the affected native rows (M1–M3, S1–S3) against unmounted
   controls.
