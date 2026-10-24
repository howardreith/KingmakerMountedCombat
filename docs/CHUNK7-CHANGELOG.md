# Kingmaker Mounted Combat: Chunk 7 changelog (since preview.211)

Status: DRAFT for a private engineering candidate. It is not a public release.
Baseline: released `v0.1.0-chunk6-preview.211` (package version `0.1.0-chunk6d-preview.211`).
Current development build: `0.1.0-chunk7b-preview.94` at `bd3f9012`. The final 7E candidate is not frozen
yet; its package identity is [TBD after 7E freeze].

Note on history: the Chunk 7 branch does not descend from the preview.211 release merge (`7fa667b2`).
This list compares product source with `7fa667b2` and cites the Chunk 7 commits that made each change.

## For players

### Controls
- **Mount and Dismount are now ordinary game abilities only.** Select the rider, open the ability
  drawer, use **Mount Companion** on the rider's own animal companion, and **Dismount** on the rider.
  The old UMM-page buttons ("Mount selected rider", "Dismount / clear", "Arm Rider Primary", "Arm Mount
  Primary") and the experimental turn-mode toggles are gone (`947b8a2d`). **Rider Primary** and
  **Mount Primary** remain abilities.
- **KMC does not put these abilities on your action bar.** Drag them to an empty slot yourself.
- These controls work in an ordinary session with no test tooling attached: Mount, a mounted move,
  Dismount and an unmounted move were checked in game (batch191, development evidence).
- **Paired turns are always on.** The paired configuration is the default and no longer a toggle (`947b8a2d`).
- Turn-based: a voluntary Mount or Dismount in the rider's own prepared turn is accepted (`06ec7180`).
- Real-time: a ground order given while paused is queued like an unmounted one (`308e7734`).
- A hostile click by the player is bound to its own attack command (`4da0bec4`).

### More pairs and riders
- **Several mounted pairs at once**, up to six (`947b8a2d` two pairs, `a23b40e8` six). Ordinary
  two-pair combat is the tested combat breadth. Six pairs are tested for exploration, Manual save
  and reload only.
- **One Small rider profile:** the stock default male Halfling on the KMC Horse (`470d7847`). Other
  Small races, presets, sexes or altered gear, and Small on a Mammoth, are not supported.

### Horse companion on a base-game install
- **Fixed:** preview.211 logged
  "[Exception] Horse companion blueprint registration: InvalidOperationException - The exact Ranger
  companion AllFeatures surface no longer has seven stock options." at start on any install without
  Call of the Wild. It only recognized the Call of the Wild Ranger companion list. Chunk 7 recognizes
  both the base-game list and that list, and adds the Horse to either (`81c191af`). After upgrading, the
  start-up log instead says "Registered the original KMC horse companion blueprint trio and exact Ranger
  append transaction" (batch200 u2).

### Doors
- Clicking an ordinary door with a mounted rider selected works for each pair: the mount walks the pair
  to the door and the door is used once, including doors that keep their navigation cut when open
  (batch192d, development evidence). The door command itself dates from earlier chunks. Chunk 7 adds no
  new door feature; it verified the behaviour for several pairs.

### The capital
- **Dismount on arrival.** In an area where the game does not let you control a companion directly
  (the capital, where only your main character and its own companion can be controlled), KMC dismounts
  that pair on arrival instead of refusing the whole area. Your main character's own pair stays
  mounted. Saving stays allowed (`9f79ba8a`, `107814bb`). The KMC page in UMM shows:

  > Dismounted on arrival because the companion is not directly controllable in this area:
  > *Rider* and *Mount*. Mount again after leaving. *N* of *M* mounted relationships restored without
  > Mount or preparation.

  Mount again after you leave.

### Saving, loading and cleanup
- One save format now holds every pair (schema 3). Valid schema-1 and schema-2 data from earlier
  previews is migrated when it is loaded (`58abe1a9`, `947b8a2d`, `43928cb1`).
- Saving waits until unfinished mounted work for every pair has settled.
- KMC's own multi-pair saves are accepted at load, and a cancelled load is cleaned up (`0ccda7cc`).
- A rider whose turn was left pending in a save cannot take a second turn in the same round
  after loading (`d96a4b06`).
- Opening the party selector keeps mounted riders seated (`61625d41`). Dismissing a mounted companion
  no longer merges inventories (`7c6ba455`).
- "Prepare to disable / remove KMC" covers every pair (`947b8a2d`).
- Progression (tested behaviour, not a new feature): leveling keeps a pair; dismissing, retraining or
  replacing the companion retires that pair. This was observed in development runs (batches 89, 99–100,
  108 and 116–120; 7E rows 48–54 re-run it on the final candidate). No Chunk 7 product commit is specific to
  leveling, retraining or replacement. Retirement goes through the native lifecycle cleanup: a removed
  or invalidated companion dismounts its pair (the handler predates preview.211; `58abe1a9` made it clean
  up only the affected pair instead of all pairs) and the per-pair registry then retires the settled
  owner (`58abe1a9`). Dismissal also has its own product fix, `7c6ba455` (no inventory merge, above).

### Settings
- The UMM page has one setting: **Enable optional real-time Mounted Charge**, off by default. You can
  change it only outside combat, with every pair dismounted. It is not saved between game launches.

## Engineering summary

- Pair ownership registry and versioned archive foundation (`58abe1a9`); shared world, turn and
  persistence authority for several pairs (`947b8a2d`); bounded six-pair admission (`a23b40e8`).
- Exact Ranger AllFeatures admission for the base-game twelve-option list and the Call of the Wild
  seven-option list (`81c191af`; see `docs/CHUNK7-RANGER-BOOTSTRAP-REPAIR.md`).
- Capital control rule `MountedAreaControlRule` and per-pair area retirement (`9f79ba8a`, `107814bb`).
- Small rider body declaration and presentation readiness (`470d7847` and its predecessors).
- Fewer idle allocations: `f3ff874e`, `7f18a593`, `6106d22f`.
- The paired command scheduler, legacy unified turn and diagnostic overlay are no longer user
  settings. `UnifiedMountedTurnCoordinator` remains in use for movement accounting.
- Development pilots: on DLL `ec20ee8f…`, the X5 upgrade and rollback rehearsal u1, u2 and u3 PASS
  (batch200) and capital rows 58 and 59 PASS (batch199). Incapacitation row 57 PASS was on an earlier
  candidate (batch197, DLL `c0fa0ebe…`). Death proper, Rapid Shot attack modes, the disengagement
  attack of opportunity, staged moving actions and real-time Charge with a melee weapon PASS on DLL
  `feca06e9…` (batch205). **Exact-package 7E qualification has not run.**
