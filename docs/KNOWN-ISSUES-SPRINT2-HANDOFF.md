# Known issues remediation, Sprint 2 — saving report and post-load dismount (2026-11-15)

Sprint 2 of the owner's Mounted Combat Known Issues Remediation Epic: **S01** (the saving report) and **S02** (the
premature dismount after an area load). The current state of every issue is in
[CURRENT-KNOWN-ISSUES.md](CURRENT-KNOWN-ISSUES.md).

## Identity

- **Branch:** `codex/kmc-known-issues-sprint2`, draft PR #46, no reviewers requested.
- **Base:** `b62c59a861671eb2955fe0edf0ade793d690bf25`, the reviewed head of Sprint 1 (draft PR #45, still open and
  unmerged when this sprint started and when it was pushed). The Sprint 2 branch was created from that commit and
  its PR targets `codex/kmc-known-issues-sprint1`, so the PR shows only the Sprint 2 change. PR #45 is unchanged.
  `origin/main` was `7a5cd2c3` throughout.
- **Product:** this branch changes gameplay code (area arrival restoration) and diagnostic log lines. A build of it
  is a new binary. The version in `Info.json` is unchanged (0.2.11).
- **Save format:** one deliberate extension, added by review R1 on 2026-11-16. The `kmc-retained-intent` member has
  a version 2 text, written only for a save that holds a pair deferred at its arrival placement. Every other save
  writes the same bytes as before, and every existing save reads as before. Details below.
- **Native:** nothing was launched, staged, installed or run in the game. No qualified disposable fixture exists
  on this machine.

## Dispositions

| ID | Disposition | In one line |
|---|---|---|
| S01 | **Investigation complete, issue unresolved** | The 0.2.3 occurrence is explained by a latch that 0.2.4 already stopped that door from setting. The 0.2.4 log holds no save line, which shows neither that a save was attempted nor that none was, so no cause is established for it. Three observation gaps are closed; nothing about saving was changed. |
| S02 | **Implemented and verified offline** | A pair whose exact views are not active yet is no longer attached at arrival; it restores through the existing retained-intent retry once ordinary play has settled. Native not run. |

Neither item is native-verified. S01 is not fixed and is not described as fixed anywhere in this branch.

## Owner review of 2026-11-16 — R1 and R2

The owner reviewed head `f71fabe7` (parent `b62c59a8`) and returned two findings. Both PRs were where the review
left them when this work started: #46 at `f71fabe7` on `codex/kmc-known-issues-sprint1`, #45 at `b62c59a8` on
`main`, `origin/main` at `7a5cd2c3`. Sprint 1 was not touched. The sections below this one are the record of the
reviewed head; where a statement in them is no longer true it says so in place and points here. Earlier results
and verdicts are kept as they were.

### R1 — the arrival placement did not survive saving and loading

**The defect.** A pair deferred at its arrival placement kept that placement's geometry in the session of the
arrival, but the placement was transient. The after-entry autosave recorded the pair as retained intent with its
identity fields only, the load rebuilt the custody row without the placement, and the retry then applied the
ordinary 6 m rule. The same valid pair nine metres apart restored if the player continued and was dropped if the
player loaded that autosave.

**The correction.** The arrival placement is now one small immutable record that travels with the row.

- `MountedRetainedArrivalPlacement` (Domain) holds where native arrival placed the rider and the mount, and the
  area it was observed in. It is comparison evidence and never a destination: nothing is moved to it.
- The custody row (`AreaPair.Arrival`), the save row (`MountedRetainedIntentPairRecord.Arrival`) and the loaded
  custody row hold that same record. `MountedRetainedIntentPolicy.StandsAtArrivalPlacement` judges it against the
  current positions, with the same 0.1 m tolerance as before, in the session of the arrival and after a load, and
  only while the area it names is the area loaded.
- **Saving.** `MountedRetainedIntentCapture.Row` converts a custody row to a save row, and
  `MountedRetainedIntentCapture.SaveText` produces the member text. `CaptureRetainedIntentRecord` only projects its
  rows through them.
- **Loading.** `MountedRetainedIntentAdoption.SelectingLoadIsCurrent`, `Pairs` and `Slots` decide what a load
  adopts. `AdoptLoadedRetainedIntent` only projects the result into its custody.

**What stays constrained.**

- **Ordinary retained intent.** A row has a placement only if `MarkArrivalDeferred` gave it one (an eligible pair
  that nothing but its views held back at arrival) or a saved row held one. Every other row, in memory or loaded,
  has none and is judged by the ordinary separation and navigation-area rule. `DecidePair` is unchanged.
- **A relocated pair.** The saved placement is the arrival placement, not the position at the time of the save.
  A pair that moved before the save loads at its new position, differs from the placement and gets the ordinary
  rule. A pair that moves after the load loses it the same way.
- **Stale identity.** The record is still adopted only by the load that selected it, while that load is current,
  in the exact campaign, area and native game time it was written into, and never for a pair the mounted-state
  member already restores. A placement is part of that record and has no other way in.
- **A newer transfer.** A row carried into a newer transfer, the world-map custody or a deferral is a new row
  made by one method (`AreaPair.CarriedCopy`), which copies identity and captured participation and nothing else.
  The contract script runs that compiled method on a row that has a placement, checks that exactly those three
  sites use it, and checks from IL that the field has exactly two writers: the arrival deferral and the adoption
  projection.
- **Another area.** The placement names its area. The save conversion leaves it out of a save of any other area,
  the codec refuses a record that holds one, and the retry does not honour it while another area is loaded. A row
  of a transfer still in flight never carries one.
- **Unchanged:** view readiness, the single attachment, the explicit-dismount release, the ten-second patience,
  the protective latches, other pairs and their control rows. Restoration is still a saved restore: no Mount
  action, preparation, refund or new participation.

**The format, deliberately.** The member is still `kmc-retained-intent`, bounded at 4096 bytes. Its text gains a
second version.

| | Version 1 (`KMC-RETAINED-INTENT 1`) | Version 2 (`KMC-RETAINED-INTENT 2`) |
|---|---|---|
| Content | Campaign, area, game time, kind, pair rows, slot rows. | Version 1, plus one `arrival <rider> <mount> x y z x y z` row after the pair rows for each pair deferred at its arrival placement. |
| Written when | No recorded pair has an arrival placement. The bytes are exactly what earlier builds wrote. | At least one recorded pair has one. In practice: a save made between a world-map arrival and the remount, which includes that arrival's after-entry autosave. |
| Read by this build | As before. No row gains a placement. | Each arrival row must name exactly one recorded pair, once, with six finite decimal coordinates, and at least one such row must exist. |
| Read by an older build (0.2.3 to 0.2.11) | As before. | Refused whole by its header: "could not be read; the pairs load dismounted". Checked by running the released decoder (unchanged since 0.2.3 in this file) on a version 2 text. |

- **Malformed or unsupported data.** Any defect in a version 2 text (an arrival row for no recorded pair or for one
  pair twice, a missing or extra coordinate, a value that is not a finite number, an arrival row under the version
  1 header or after the slot rows, a version 2 header with no arrival row, a later version number) refuses the
  whole record, as every malformed record already was. Its pairs load dismounted; nothing is latched.
- **The bound.** If the arrival rows alone would push a record past 4096 characters, the save writes the version 1
  text instead, and those pairs reload as ordinary retained intent. A record that fits without them is never
  lost because of them. This needs six pairs and about fifty control rows.
- **Cost for an older reader.** Before this sprint that autosave held the pair as mounted. At the reviewed head it
  held a version 1 record an older build could read. Now an older build refuses it, and every pair in that
  record, deferred or not, loads dismounted there. This affects only a downgrade of the mod on such a save.
  Keeping older readers able to read it would need a second archive member beside the first; that was not done,
  because it adds a storage path that can only be exercised in the game.
- **Not changed:** the storage code (`NativeMountedSaveStorage`), the member name and bound, and the mounted-state
  member.

**Evidence, all offline.**

- *The regression.* `tests/DeferredArrivalSaveLoadTests.cs`, seven component tests. The positive case runs the
  six steps of the review: a nine-metre arrival with inactive views; deferred, nothing attached; captured and
  encoded as the after-entry autosave; decoded and adopted in the matching loaded world; still at the placement;
  restored exactly once, with the saved owner identity and generation, by a saved restore. The conversions and the
  codec it runs are the production ones named above.
- *Failing before.*
  - The compiled product of the reviewed head: `Test-PostLoadViewReadinessContracts.ps1 -ProductDll` on the
    `f71fabe7` build gives 20 pass, 6 fail. The check that drives the reviewed build's own row conversion and
    codec on a row deferred at arrival reports that the saved row has nowhere to hold the placement and prints
    the version 1 text it wrote.
  - The reviewed conversion inside the corrected code (the placement dropped in `MountedRetainedIntentCapture.Row`,
    nothing else changed): 1253 pass, 4 fail. The positive case fails with "The save of a deferred arrival pair
    did not carry its arrival placement."
  - One of the seven tests keeps the reviewed conversion as a case and asserts the reported result: the pair
    restores in the session of the arrival and is dropped as separated after loading.
- *Passing after.* Components 1257 pass, 0 fail (1250 at the reviewed head). The contract script 26 pass, 0 fail
  (20 checks at the reviewed head), on the build of this change.
- *The guards are exercised.* Two more mutations of the corrected code, each run once:
  - a placement honoured and saved whatever area it names: 2 component tests and 2 contract checks fail;
  - a carried copy that keeps the placement: 2 contract checks fail (the component tests cannot see it, because
    the copy is integration code).
- *Negative cases* (each in the seven tests; the version 1 text, a custody of another area, another campaign and
  the ordinary rule also on the compiled product):
  - ordinary retained intent nine metres apart is saved as version 1 and dropped after loading; four metres apart
    it restores;
  - a version 1 text written by hand decodes with no placement, is re-encoded to the same bytes, and behaves the
    same;
  - another campaign, area or game time adopts nothing; a replaced load adopts nothing; an archived pair is not
    adopted twice;
  - a missing actor is dropped; a pair the player already mounted again is not re-attached;
  - a newer transfer does not use an earlier placement;
  - beside a deferred pair, another pair's row and every control row are written byte for byte as before and
    judged by the ordinary rule.
- *The compiled chain.* The contract script drives custody row → `ToRecords` → `SaveText` → `Decode` →
  `Adoption.Pairs` → `ToAdoptedPairs` on the compiled product, and checks from IL that the save and the load
  call these conversions in order, with retained rows allowed a placement and transfer rows not.

**Independent review of this change.** One read of the uncommitted diff, given the code and the requirements but
not the conclusions. It found no production defect. Its points and what was done:

| Finding | Disposition |
|---|---|
| The "custody of the saved area" guard was a comparison in the integration that no check executed; replacing it with `true` passed everything. | Corrected: the placement now names its area and the guard is Domain code (`ObservedIn`), executed by the component tests and on the compiled product. A mutation that removes it fails four checks. |
| The "newer transfer" test passed because of the harness's own line, not production's copies; the IL writer scan could not see a row reused by reference. | Corrected in part: the three production copies now go through `AreaPair.CarriedCopy`, which the contract script runs and whose call sites it counts. The test's comment says what the harness stands for, and its last assertion now takes a save of the later custody. A row reused by reference at a new site would still be invisible to these checks. |
| `Decode` accepts coordinates that are not in the written form (`+9`, `009`, `1e1`). | Kept, and stated: a coordinate is read as any finite decimal number of at most 16 characters, like the record's integer fields. It is only ever compared with a position. A test pins the behaviour. A canonical-text check was not added because it would depend on the game runtime's number formatting, which cannot be run offline. |
| The retry did not tie a placement to the custody's area, where the save did (reachable only if a custody outlives an area change without a transfer). | Corrected by the same area binding; a test covers a custody that strays into another area. |
| A downgraded build refuses a version 2 record whole, so ordinary pairs in it also load dismounted. | Stated above under "Cost for an older reader". |

**Limits.**

- The save and the load themselves (`CaptureRetainedIntentRecord`, `AdoptLoadedRetainedIntent`, the archive
  storage) read the live game and do not run detached. Their two field projections are executed on the compiled
  product; their call order is checked from IL; the rest is unexecuted, as before.
- That a native load puts each unit back within 0.1 m of where the save holds it is assumed, not observed. If it
  does not, the loaded pair is judged by the ordinary rule, which is the reviewed behaviour.
- Coordinates are written with the round-trip format and read with the invariant culture. That was run on the
  .NET Framework test host, not on the game's Mono runtime.
- The component tests stand a harness where the integration stands, as the other S02 tests do. The harness line
  that models a newer transfer's copy is the harness's own; the production copy is checked on the compiled
  product, not in the flow.

**What was run for this correction.** All offline, Release configuration, budget record `known-issues-sprint2`.
Commands are run from the repository root with Windows PowerShell
(`powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File …`).

| Check | Reviewed `f71fabe7` | After this correction |
|---|---|---|
| `scripts/Build-Local.ps1 -Configuration Release`, then `bin/Tests/Release/KingmakerMountedCombat.Tests.exe` | 1250 pass, 0 fail | 1257 pass, 0 fail |
| `scripts/Validate-Source.ps1` | 151 pass, 0 fail | 151 pass, 0 fail |
| `scripts/Test-PersistenceContracts.ps1 -Configuration Release` | 223 pass, 0 fail | 223 pass, 0 fail |
| `scripts/Test-WorldBoundaryChargeContracts.ps1` | 8 pass, 0 fail | 8 pass, 0 fail |
| `scripts/Test-PostLoadViewReadinessContracts.ps1 -Configuration Release` | 20 pass, 0 fail (20 checks) | 26 pass, 0 fail |
| The same script with `-ProductDll` on the `f71fabe7` build | — | 20 pass, 6 fail |
| `scripts/Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint2 -Focused Test-PersistenceContracts.ps1,Test-WorldBoundaryChargeContracts.ps1,Test-PostLoadViewReadinessContracts.ps1` | PASS 6/0 | PASS 6/0 |

- **One failed tier run, caused by the evidence work and not by the change.** The first Fast run after the
  mutation checks reported the contract script at 24 pass, 2 fail. The mutated source files had been restored
  from copies that kept their old timestamps, so the incremental build skipped and the tier tested the binary of
  the last mutation (the same module ID as that mutation's own run). With the two files touched, the build ran and
  the tier passed 6/0. Both logs are kept.
- **Neighbours.** `Test-Chunk7AreaRestorePreflightAdapter.ps1` (T07), `Test-Chunk7SixPairArchiveAdapter.ps1` (T08)
  and `Test-Chunk7CapitalArrivalEvidence.ps1` end as at the reviewed head: the first two do not compile, the third
  reports 162 pass, 3 fail for native evidence that is not on this machine. T07's facade now also lacks the new
  `CarriedCopy` method, which is one more line for whoever repairs it; it did not compile before.
- **Older reader.** A throwaway probe wrote a version 2 and a version 1 text with this build and read both with
  the `f71fabe7` build, whose record decoder is the released 0.2.11 one (the file is unchanged since 0.2.3): the
  version 2 text is refused ("unknown header or shape"), the version 1 text is accepted.
- **Not run:** native gameplay, staging, installation, Candidate and Full tiers. T04–T08 were not worked on.
- **Evidence logs** (local, not committed): `artifacts/known-issues-sprint2-review/` in the lab.

### R2 — the S01 conclusions were stronger than the evidence

The "Ruled out" list excluded a stranded save scope or fence because "no save ran in the session", while the same
handoff kept a queued or silently waiting save possible. Corrected in "S01 — the saving report" below, in the
ledger and in `CURRENT.md`:

- "no save line was recorded" is no longer written as "no save ran";
- stranded save ownership is "not established, and not ruled out";
- the next-attempt failure report is described as an observation that could distinguish the cases, not as proof
  by its absence;
- the Last Azlanti explanation is conditional on the campaign's one-save setting and on the action having been a
  manual save or a quicksave.

S01 stays **investigation complete, issue unresolved**. No investigation was reopened, no diagnostic was added and
no save decision changed in this correction.

## S02 — premature dismount after an area load

### The recorded ordering

From the owner's 0.2.3 session log (`output_log(2).txt`, kept local and not committed; line numbers as in the
earlier handoffs):

| Lines | Event |
|---|---|
| 11086–11087 | The pair is suspended for the area change that leaves the local area. |
| 11112–11113 | World map: `OnAreaActivated` with `mode=GlobalMap`; "1 mounted relationship carried across the world map". |
| 11156–11159 | The pair's intent leaves the world map with the next area transfer. |
| 11181, 11186 | Arrival at Ratnook Hill: `OnAreaDidLoad` and `OnAreaActivated`, both `mode=Default`, inside native `Game.OnAreaLoaded`. |
| 11187–11193 | KMC restores in its `OnAreaLoaded` postfix: movement authority, presentation, `state=Mounted`, "All 1 mounted relationships restored before area autosave". |
| 11200 | Another mod reports the loading screen still on top. |
| 11211–11212 | `OnAreaLoadingComplete`. |
| 11213 | The first invariant check: `mode=Default`, `riderSelf=False; mountSelf=False; riderHierarchy=False; mountHierarchy=False; nativePartySelectorActive=False`. |
| 11214 | `state=Unmounted, trigger=CompanionInvalidated`. The player mounts again by hand at 11221–11233. |

This is the only recorded occurrence. It is a world-map arrival. The 0.2.4 log has one restoration (a same-area
door teleport, 10858) and no invalidation after it.

### Order of placement, restoration, view activation and validation

Read from the installed `Assembly-CSharp` (MVID `07fa1e4d-…`) and from the mod's source. The native items are
checked from IL by `scripts/Test-PostLoadViewReadinessContracts.ps1`.

1. **A sleeping unit has an inactive view.** `UnitEntityData.IsViewActive` (`060082F4`) is false when the unit is
   out of the game or `IsSleeping`. `EntityViewBase.UpdateViewActive` (`0600176F`) sets the view object's active
   state to exactly that value, and the `IsSleeping` setter calls it.
2. **The world map puts every unit to sleep.** `AllUnitsSleepingController.Tick` (`06008FE3`) sets
   `IsSleeping = true` on every unit of the loaded state. It is registered for exactly the GlobalMap, Kingdom and
   KingdomSettlement game modes.
3. **Only a Default-family mode tick wakes a unit.** `SleepingUnitsController` (Default, Dialog, Pause, Cutscene,
   Rest) writes the flag in `Tick`; its `Activate` is empty.
4. **No mode ticks during a load.** `Game.Tick` (`06000CBA`) returns before any game-mode tick while
   `LoadingProcess.IsLoadingInProcess`. `Game.OnAreaLoaded` (`06000CD7`) starts the arrival mode but ticks none and
   does not write the sleeping flag.
5. **Placement and restoration happen inside the load.** KMC's arrival restoration (`RestoreAreaPair`) runs at the
   `OnAreaLoaded` postfix. The preload, the after-entry autosave and `AreaLoadingComplete` follow in the same
   loading queue.
6. **Validation waits for the loader, and then runs at once.** `CompositionRoot.UpdateCore` skips pair updates
   while an area transfer is pending. `MountedPersistenceService.Update` releases the transfer in the first update
   that finds the loader idle, and `MountedWorldService.UpdatePairs` validates every pair later in that same
   update.

So a party that arrives from the world map still has inactive views from `OnAreaLoaded` until the first Default
tick after the load. The old restoration attached to those views at step 5. The check at step 6 then found them
inactive in ordinary `Default` mode and dismounted the pair.

**What is and is not established.** The ordering and the four `False` values are recorded. That the views were
inactive *because the units were still sleeping* is an inference: the pair attached at 11187–11193, so both units
were in the game (the candidate's control term requires it), which leaves the sleeping term of `IsViewActive`.
It was not observed in the game, and the correction does not depend on it: it acts on the view activity itself.

### The correction

- **Arrival.** `RestoreAreaPair` reads the activity of the exact current rider and mount views, through one
  read-only adapter (`KingmakerMountedPairRuntime.RestoreViewReadinessRefusal`), only when nothing else refuses the
  pair. An eligible pair whose views are not all active is not attached:
  - a carried pair (world-map travel or an earlier event) is **retained** for the in-area retry
    (`MountedRetainedIntentPolicy.DecideCarriedArrival`);
  - a pair that was mounted at departure is **deferred** into the same retained custody, with its exact owner
    identity, participation and control rows (`DecideDepartingArrival`). No native route is known to produce this
    case (a party that leaves from Default-family play is awake); it is there so that both kinds of pair follow one
    rule.
- **In the area.** The existing retained-intent retry (`RestoreRetainedIntentCore`) reads the views again at every
  evaluation and treats "not active" as one more transient refusal (`DecidePair`). It attaches once, after ordinary
  `Default` play has held for five frames and 0.3 s, through the established saved-ownership import and
  `RestoreSavedPair`.
- **The arrival placement keeps its geometry.** The old arrival restoration had no separation rule; the retry has
  one (6 m, same navigation area, for world-map custody). A pair deferred at arrival records where native placed
  its rider and mount. While neither has moved more than 0.1 m the retry restores it as the arrival would have.
  Once either has moved, the ordinary rule applies. At the reviewed head `f71fabe7` this record was transient and
  no save carried it; since review R1 a save written before the remount carries it (see "Owner review of
  2026-11-16").
- **Explicit dismount.** A player dismount (`CleanupTrigger.Manual`) now releases that pair's retained row at the
  dismount (`ObservePairDismounting`). Before, a pair that the player mounted and dismounted again while its
  restoration was still pending (inside the settle window, or scene-staged intent held through an encounter) could
  be re-attached afterwards.
- **Readiness is the invariant.** `MountedRestoreViewReadinessPolicy.Refusal` is what
  `MountedViewActivityPolicy.IsAdmissible` accepts in ordinary `Default` play, the only mode restoration attaches
  in, from the same inputs. A pair is attached if and only if the next invariant check would keep it. That includes
  the invariant's one `Default` allowance, the native party-selector overlay, under which restoration attached
  before this change and still does.
- **One message.** A retained record loaded in a local area now says the pair remounts "when ordinary play
  resumes" whatever its kind. That is what the restoration decision already did.
- **Diagnostics.** One line per deferred pair per arrival: `Mounted arrival restoration deferred until the exact
  views are active: …` with the four activity values. The existing "Retained mounted pair restored" and "Retained
  mounted restoration refused" lines report the end.

Two inline bounds moved into policy functions without a behaviour change (`Settled`, `PatienceExpired`), so the
tests drive time through the production predicates.

### Why this separates temporary unreadiness from genuine invalidation

- The mounted invariant is not changed. `ValidateMountedInvariants` and `ValidateActivePair` are untouched, and
  the contract script checks from the compiled IL that neither consults the new readiness policy. A mounted pair
  whose view goes inactive in settled `Default` play is dismounted exactly as before.
- Temporary unreadiness is only ever a property of a pair that is **not attached**. Nothing is exempted, suppressed
  or delayed for a mounted pair, so there is no allowance a genuine invalidation could pass through.
- The wait is the existing custody, with its existing ends:
  - **completed** by the single attachment;
  - **cancelled** by a replaced world or campaign, a loaded save, combat (world-map custody), mod disable, an
    update failure, removal preparation, and now an explicit dismount;
  - **handed on** to a newer area transfer, which decides the pair again at its own placement;
  - **bounded** by the existing ten seconds of settled, unowned play, after which the pair is released with the
    reason in its message and stays dismounted.
- Nothing is remembered about a view. Readiness is read from the current views each time, so a replaced view is
  judged as itself.
- No global grace period, no blanket exemption, no extra remount attempts, no synthetic action, refund or native
  preparation, and no new latch. A waiting pair owns nothing: saving, teardown and a manual Mount stay admitted.

### Automated acceptance

`tests/PostLoadViewReadinessTests.cs`, 14 component tests. They run the production decisions
(`MountedRetainedIntentPolicy`, `MountedRestoreViewReadinessPolicy`, `MountedViewActivityPolicy`), the production
transfer lifecycle, the real relationship coordinator and the real pair registry. The harness around them holds
units, views and retained rows, and calls those pieces in the order the integration calls them.

| Required case | Test |
|---|---|
| 1 Recorded ordering | Arrival from the world map with sleeping views: nothing attaches during the load or in the first idle update; after the views wake and play settles the pair attaches exactly once, keeps its owner identity and generation, captures the active views and stays valid for 200 further frames. |
| 2 Idempotence | Repeated placement callbacks, repeated restore requests, flickering readiness and a later transfer: one attachment, one owner. |
| 3 Cancellation | Explicit dismount inside the settle window, and during an encounter that holds scene-staged intent (voluntary combat mount, then dismount); combat before the views are ready; a replaced world; a newer area transfer (nothing restores under it, and its own arrival decides the pair once). |
| 4 Stale identity | Replaced views (readiness of the old views revives nothing; the attachment captures the current ones); a removed actor; another world (policy and registry); an owner identity already in use after the player remounted. |
| 5 Genuine invalidation | The invariant table for 9 modes × 32 observations is unchanged; each of the four values going false in settled play dismounts with `CompanionInvalidated`, creates no retained intent and is not restored; a pair restored through the deferral gets no later allowance; the party-selector overlay behaves as before for a mounted pair and for restoration, and views still inactive when it closes are a genuine invalidation. |
| 6 Never-ready views | Released at the patience bound with the view refusal as the reason; nothing attached, nothing latched, nothing owned while waiting; the player can mount again. |
| 7 Failed cleanup | A restoration whose rollback fails keeps the latch through 300 further frames; waiting never reaches the failing attachment; a fully rolled-back attempt latches nothing. |
| 8 Isolation | A ready pair restores at arrival while another waits; the late restoration does not touch it; an unmounted pair is never captured, attached or owned; every restoration is a saved restore, never a voluntary mount. |

Also: readiness equals what the invariant accepts for all thirty-two observations; the arrival placement's
geometry (kept while neither unit moved, lost on relocation, never given to a pair retained for another refusal or
carried into a newer transfer); the departing-pair decision; every refusal (capital retirement included) is decided
before view readiness; the settle and patience bounds.

`scripts/Test-PostLoadViewReadinessContracts.ps1` also drives the compiled `ReleaseRetainedPair` and
`ObservePairDismounting` on a detached service with real custody rows: only rows and control rows that share an
actor are released, an emptied custody ends, a non-explicit trigger releases nothing, and a failure inside the
release with a logger that also throws does not escape the notification.

**Before and after.**

- *Prior decision behaviour.* With the three decisions ignoring the view term and everything else on this branch
  unchanged, 11 of the 14 tests fail (1239 pass, 11 fail). The first failure is "A pair was attached to inactive
  views at arrival". With the correction: 1250 pass, 0 fail.
- *Prior product binary.* `scripts/Test-PostLoadViewReadinessContracts.ps1 -ProductDll` against the Release build of
  the base commit `b62c59a8`: 11 pass, 9 fail. All ten native contracts and the unchanged-invariant check pass; the
  nine product wiring, behaviour and decision checks fail. Against this branch: 20 pass, 0 fail.
- One of the 14 tests also replays the recorded order through the prior call shape (the arrival decision without a
  view term, which is what the old code passed) and asserts the recorded result: attached during the load,
  `CompanionInvalidated` at the first check, never remounted.

**Limit of this evidence.** The two restoration methods read `Kingmaker.Game` and Unity view objects, so they do
not run detached. What the component tests execute is the production policy, lifecycle, coordinator and registry
code under a harness that restates the integration's sequence; that the compiled integration calls them in that
order (views read before each decision, each decision before any import or attachment) is checked from IL, by
presence and order only. No automated check executes the integration's own copy of a deferred departing pair, its
control-row exclusion or the custody kind it is given. The whole flow has not run in the game.

### Compatibility and persistence effects

- **After-entry autosave of a world-map arrival.** The pair is still dismounted when that autosave is written, so
  the autosave carries it as retained intent (the existing `kmc-retained-intent` member) instead of as a mounted
  pair. Loading that autosave restores the pair in ordinary play through the same retry. The member's text has a
  second version for exactly this case since review R1; the format, its versioning and what older readers do are
  in "Owner review of 2026-11-16". (The first version of this handoff said "No format changed", which was true of
  the reviewed head and is no longer.)
- **What the player sees.** After a world-map arrival the rider stands beside the mount when the loading screen
  lifts and is seated about a third of a second into ordinary play. With pause-on-load the pair mounts after the
  game is unpaused.
- **Separation.** A pair deferred at arrival that has not moved restores whatever the formation's spacing, as the
  old arrival did, in the session of the arrival and (since review R1) after loading a save written before the
  remount. Once a unit has moved, the retry's ordinary rule applies (6 m, same navigation area) and a pair farther
  apart stays dismounted with a message. Native formation distances were not measured. At the reviewed head the
  placement was not saved, so loading the after-entry autosave dropped a pair more than 6 m apart; that was the
  regression review R1 reported.
- **A fight before the remount.** World-map custody never adopts an encounter (existing rule). If combat starts
  between the arrival and the remount (the rest of the load and about a third of a second of play), the automatic
  remount is cancelled and the pair fights dismounted. The old restoration attached before that window; in the
  recorded case it was then dismounted anyway.
- **Diagnostic scenario engines** that use a `Manual` dismount as cleanup now also release a retained row of that
  pair. No production caller uses `Manual` for anything but a player dismount.
- **Unchanged.** Arrivals whose views are active (ordinary doors, the same-area teleport, restore-in-place),
  restoration under the party-selector overlay, the capital retirement, arrival in combat, and every refusal.

## S01 — the saving report

### Evidence available

- The original incident logs are present locally: `output_log(2).txt` (KMC 0.2.3, 2,431,057 bytes) and
  `output_log(3).txt` (KMC 0.2.4, 2,357,817 bytes), from the owner's two repair packages. They are not committed.
- No log exists from 0.2.5 or later, where the save request and completion lines were added.

### What the logs show

**0.2.3 session.**

- Seven saves complete before the door (serialization and snapshot lines at 10899, 10964, 11088, 11151, 11236,
  11256, 11315).
- 11461–11462: the Ratnook door suspends the pair. 11475: "Native area placement did not complete before the
  mounted restore boundary." No save line follows; the log ends at 11484.
- That message is the area-restore failure latch. In the code of that version, as now, the latch makes
  `CombatSemanticRestorationPending` true, `NativeCombatBlocksSave` then returns true, and the transpiled native
  `SaveManager.IsSaveAllowed` returns false until a save is loaded.
- So in this session saving was refused **at admission (boundary 2)**, and through the Esc menu the request never
  left the menu (boundary 1), because `EscMenuWindow.OnShow` disables its Save button when `IsSaveAllowed` is
  false. This agrees with the Ratnook handoff ("Saving was blocked by the same condition").
- 0.2.4 stopped this door from setting the latch (the same-area teleport repair). The latch itself is a protective
  state and was not changed, then or now.

**0.2.4 session.**

- There is no save line of any kind in the whole session.
- 10847–10858: the door teleport and the remount. 10911: a paired activation is prepared (a fight is starting).
- 10912–10914 and 10916: four "Area change refused" lines; between them, at 10915, the native line `Show`
  (`SaveLoadWindow.Show`). The log ends there.
- At 0.2.4 "Area change refused" had one emitter, the admission in the full `Game.LoadArea` prefix. Each line is
  therefore a refused **load**: three before the save/load window opened and one after. That is the load defect
  0.2.5 repaired.
- Nothing in the log records a save attempt. That is an absence of record, not a record of absence: at 0.2.4 a
  save request, a refusal by `IsSaveAllowed`, an operation queued behind another loading process and an operation
  waiting before the native header each wrote nothing. The log does not show that no save was attempted or ran.

### Boundary classification for the 0.2.4 report

| Boundary | Supported by the evidence? |
|---|---|
| 1 The action never reaches `Game.SaveGame` | **Earliest boundary consistent with the log.** Not distinguishable from 2. |
| 2 Native or KMC admission refuses | Possible. At 0.2.4 a refusal by `IsSaveAllowed` left only a UI warning, and neither it nor the request was logged. |
| 3 Queued but never begins | Not excluded: a save queued behind another loading process was silent. |
| 4 Begins but never completes | Partly excluded. A save that waited for effects, reached the header or was reported failed would each have written a line that is absent. A save that began and sat in the native wait before the header was silent, and is not excluded. |

Boundaries 3 and 4 would have left the game in its loading state; nothing in the log suggests that, but it does
not record the screen.

### Ownership and cleanup, traced in the current source

A source read, spot-checked against the checks that execute these paths (`Test-PersistenceContracts.ps1`,
`Test-WorldBoundaryChargeContracts.ps1`). No path was found that sets a save-blocking state on one route and
releases it only on another. That is what a read of the current source found; it does not show what state the
0.2.4 session was in, and it is not proof that no such path exists.

- **Area-restore failure latch** (`areaRestoreFailure`): set by a failed suspension, a failed all-pair
  restoration, an idle transfer without placement, or retained residue; each logs. Cleared only by load
  housekeeping. It refuses saving by design.
- **Combat restore failure, abandoned combat, unrestored selected combat:** the same gate, cleared by load
  housekeeping or by the restoration itself.
- **Active save scope** (`activeSave`): set when the operation begins; cleared in `ReleaseSaveScope`. An early
  disposal with a live worker moves it to the draining scope, drained every frame; a failed release is retried
  every frame. A stranded scope would make the next save that begins throw "Overlapping native save enumerations",
  which is reported. That report is an observation a later attempt could supply; the 0.2.4 log establishes no
  later attempt, so its absence there shows nothing either way.
- **Save admission fence** (`admittedSave`): acquired before the first log line; released with the world fence,
  or by the terminal callback of an operation that never started.
- **Per-pair charge fences and retained charge ownership on any pair:** acquired and released for every recorded
  owner; retained cleanup ownership defers the save with a notice, bounded at 30 s, then fails it and releases the
  fence.
- **Serialization suspension:** set at the header; cleared in `ReleaseSaveScope`, including when the completion
  report throws.
- **The door sequence of the 0.2.4 log:** the four refusals change no state. Nothing save-blocking is left set by
  them.

### Ruled out, and not established

Corrected on 2026-11-16 after the owner's review of this handoff. The first version listed "a stranded save scope
or fence" as ruled out, on the grounds that "no save ran in the session" and that a stranded one produces a logged
failure on the next save. Neither ground holds: the log has no save *line*, which does not show that no save ran
(see the boundary table), and no next save attempt is established. That item is moved below.

**Ruled out for the 0.2.4 session** (each would have written a line at that version, and none is present, or the
source excludes it):

- a save that reached deferral-with-wait, the header or a reported failure;
- the 0.2.3 latch (its message is logged when it is set; there is no such line);
- a refused load leaving native save state behind (the native operation state and commit flag are written only on
  save paths).

**Not established, and not ruled out:**

- **Stranded save ownership** (an active save scope or an admission fence left set). No stranded scope or fence was
  established: the source read above found no path that strands one, and the log has no line that shows one. It is
  not ruled out either. A save that began silently and never reached the header (boundary 4) would hold exactly
  that ownership, and the log could not show it.
- **A save queued behind another loading process, or waiting before the native header** (boundaries 3 and 4).
  Both were silent at 0.2.4.
- **Whether any save was attempted at all.**

**What would distinguish these cases** is an observation the 0.2.4 log does not contain: a later save attempt on a
build that logs it. A stranded scope makes the next save that begins fail with a reported error; the request line
now names what a queued save waits behind; the "began" line separates a save that never started from one that
started and did not reach the header. Until such an attempt is recorded, the absence of those lines is not
evidence for or against any of the three.

**Also considered:**

- From the earlier handoffs, still standing: `CombatRestorationPending` as the cause in the 0.2.4 session, and the
  Gunslinger cross-mod cause, were excluded there.
- A source question examined and not supported: that a save started from the save window is activated
  synchronously inside `Game.SaveGame`. `LoadingProcess.StartLoadingProcess` only stores the operation; its first
  step runs from the loader's own update.

### The missing observation

Whether a save was attempted at all in the 0.2.4 session, by which action (Esc-menu Save, quicksave, or an
expected autosave), and what the native gate and the one-save setting were at that moment.

One fact bears on this and needs the owner: *Mr. Bard's Second to Last Azlanti* (`KingmakerLastAzlantiPreserver`
0.1.3) is active in both sessions, and its README says it "does not enable manual saving or quicksaving". That is
a possible explanation only under two conditions, neither of which is known:

- **the campaign runs in the native one-save mode.** Only then does `Game.SaveGame` refuse a manual or quick save
  and the Esc menu disable Save, with or without this mod. No save or settings file was opened, so the setting is
  not known;
- **the action that failed was a manual save or a quicksave.** The one-save rule does not refuse an autosave to
  the campaign's own slot, so it would not explain a missing autosave.

If either condition is false, this explains nothing about the report. It is a question for the owner, not a
finding.

### Diagnostics added (observation only)

No save decision, guard, latch or retry changed. Each line is guarded so that a logger that throws cannot reach
the save path.

| Gap | Now |
|---|---|
| A refusal by this mod's restoration state stops a save before `Game.SaveGame`, so the request line never appears. | `Mounted save admission held: …` once when the mod starts refusing saving for its restoration state, with each term, and `Mounted save admission hold ended` once when it stops. Never per query. |
| The request line printed `nativeAllowed=True` for requests native then refused silently (one-save mode) or skipped (autosave disabled). | The request line adds `nativeOutcome`, `onlyOneSave`, `oneSaveSlot` (reported only in one-save mode), `autosaveEnabled` and `queuedBehind`. The outcome follows the native order, which is checked against the installed `Game.SaveGame` IL. If any of these reads fails, the line is still written with the fields it always had. |
| A queued save that never began and one that began and never reached the header were both silent. | `Mounted save began: …` at the operation's first step, after it owns the save. |

**Reading a recurrence.** No "admission held" line and no request line: the action did not reach `Game.SaveGame`
for a reason that is not this mod's restoration state. A request line gives the native outcome. A request with
`nativeOutcome=queued` and no "began" line: never started (`queuedBehind` names what it waited behind) or held by
this mod's deferral, which reports itself. "Began" without a serialization or completion line: started and did not
reach the header.

These diagnostics are not a saving fix.

## What was run

All offline, on the development machine, Release configuration. Budget record `known-issues-sprint2`.

| Check | Base `b62c59a8` | This branch |
|---|---|---|
| Component tests | 1234 pass, 0 fail | 1250 pass, 0 fail |
| `Validate-Source.ps1` | 151 pass, 0 fail | 151 pass, 0 fail |
| `Test-PersistenceContracts.ps1` | 214 pass, 0 fail | 223 pass, 0 fail |
| `Test-WorldBoundaryChargeContracts.ps1` | 8 pass, 0 fail | 8 pass, 0 fail |
| `Test-PostLoadViewReadinessContracts.ps1` | 11 pass, 9 fail (run on the base build) | 20 pass, 0 fail |
| Fast tier with those three focused scripts | — | PASS 6/0 |

- Component tests under the prior decision behaviour: 1239 pass, 11 fail (described above).
- The nine added persistence checks: the native `Game.SaveGame` order; the save-began report and the admission
  hold report driven on the compiled service with a logger that throws; every write the hold report makes (calls
  and fields); the guarded helper behind the request line's new fields.

## Independent review

One independent read of the uncommitted change, given the diff and the constraints but not the conclusions. It
confirmed the native claims against the decompile, the equivalence of the moved bounds, the unchanged evaluation
order and the exception paths, and found no crash or latch defect. Its findings and what was done:

| Finding | Disposition |
|---|---|
| Every world-map arrival now passes the retry's 6 m separation rule, which the old arrival restoration never had. A formation that puts a pet farther from its rider would leave S02 unfixed for that party. | Corrected: a deferred pair that has not moved keeps the arrival placement's geometry (above). Tests and compiled-decision checks added. |
| The same arrivals are now cancelled by combat that starts before the remount. | Kept: the existing rule for world-map custody. Stated under compatibility effects. |
| The explicit-dismount test mounted under a pause, which production refuses (mounting is admitted in `Default` only). | Corrected: the test now uses the two reachable cases (inside the settle window; scene-staged intent held through an encounter with a voluntary combat mount) and asserts the admission modes. The compiled release is also driven directly. |
| The "writes nothing but its own counter" contract counted setter calls only and could not see a field write. | Corrected: it now lists every field and setter write of the observation and requires exactly its own flag and counter. |
| Readiness was stricter than the invariant under the party-selector overlay: a pair pending there would have been dropped after ten seconds where it used to attach. | Corrected: readiness is now exactly what the invariant accepts in `Default`, overlay included. |
| The new request-line reads ran before the single log call, so a failed read would have lost the whole line; `oneSaveSlot=True` printed for ordinary campaigns. | Corrected: the new fields come from one guarded helper; the slot is reported only in one-save mode. |
| The "can never throw" check only looked for a catch clause. | Corrected: replaced by a run of the compiled notification with failing custody and a logger that throws. |
| The component tests restate the integration; nothing executes the departing-pair copy, the control-row exclusion or the kind assignment. | Acknowledged as a limit (above). Not closable without the game. |
| Diagnostic engines use `Manual` dismounts as cleanup. | Acknowledged under compatibility effects. |

**Not run:** the Candidate and Full tiers, package, suite, purity and native campaigns, any game launch.

## Stale harnesses found and left open

Run as neighbours of the changed files. Each fails identically on the base commit; none is needed for an S01 or
S02 check, so none was touched (ledger T07, T08).

- `scripts/chunk7/Test-Chunk7AreaRestorePreflightAdapter.ps1` does not compile its source list (`CS0103`,
  `MountedAreaControlRule`; its facade lacks the transfer lifecycle members).
- `scripts/chunk7/Test-Chunk7SixPairArchiveAdapter.ps1` does not compile (`CS0234`, `MountedControlSlotPolicy`; a
  stale `Report` call).
- `scripts/chunk7/Test-Chunk7CapitalArrivalEvidence.ps1`: 162 pass, 3 fail. The three are "native evidence is
  present" for batches 196–198, which are not on this machine. Not a harness defect.

T04–T06 are unchanged and still open.

## Not run, and not claimed

- No native execution of any kind. S02 is not native-verified; S01 is unresolved.
- An offline regression passing is not a reproduction or a resolution of the live incident.
- No merge, release, tag, installation, loader replacement or reviewer request. Sprint 3 is not started.

## Evidence

Local logs under `C:\Dev\KingmakerMountedCombatLab\artifacts\known-issues-sprint2\` (not committed): baseline
contract runs, the first build and component run, the prior-decision mutation run, the contract script on the new
and on the base build, the persistence contracts, the neighbour harness comparisons, and the Fast tier.

## For the next session

- **Seam.** `KingmakerMountedPairRuntime.RestoreViewReadinessRefusal` is the only place restoration reads view
  activity. Any new restoration path that attaches a pair must consult it before attaching;
  `Test-PostLoadViewReadinessContracts.ps1` holds the two existing paths to that.
- **Native acceptance of S02** needs a world-map arrival with a mounted pair: expect the "deferred" line during the
  load, the "Retained mounted pair restored" line shortly after it, and no `CompanionInvalidated`.
- **S01** needs the owner's answer on the save action and the one-save mode, or a log from this build or later.
- The arrival placement of a deferred pair is transient. If native acceptance shows pairs left dismounted after
  loading an after-entry autosave, that is where to look.
- `[IO.File]` calls in PowerShell resolve relative paths against the process directory, not the shell location.
  Use absolute paths when working in a worktree.
