# Known issues remediation, Sprint 7 — mounted presentation: post-Charge distance, targeting slot, bond rows, badge and tracker hover (2026-11-19)

Sprint 7 of the Mounted Combat Known Issues Remediation Epic: U01–U04. A presentation sprint. Offline engineering
only: nothing was launched, staged or installed, no screen was viewed, and nothing here is native gameplay or visual
evidence. Sprint 8 was not started.

**Corrected after the owner review of `292cd23c` (2026-11-19).** The review accepted U01's forced-travel projection,
U02's weak slot/delivery targeting identity, U03's read-fresh copy-only projection, U04's pure pair, badge and hover
identity policies, and T09. It found U04's native delivery and cleanup lifecycle incomplete (R1, R2). Both are
corrected below (see "Review of 292cd23c"), and the full P1–P27 campaign ran on the new head. Withdrawn from the
`292cd23c` records:
- that the badge was refreshed every frame through `HealthUpdate` (native calls it only after a hit-point change);
- that U04's hover needed no owned lifetime (native's own pass recomputes every entry, but nothing released KMC's
  redirect at a tracker rebuild, a pair cleanup, a generation change, load, area, world discard, disable or disposal);
- that badge objects were removed at world boundaries (only disable and disposal removed them);
- the open question whether a party-portrait hover reaches the tracker: it does not (installed IL).

**Corrected again after the owner review of `e7720f39` (2026-11-19).** The review accepted U01 and U02 completely at the
offline engineering checkpoint, U03 for the character-screen class panel, U04's pair identity policies, its native
hover-source contract, the `UpdateCrippled`/`Initialize` portrait seams and the existing lifecycle wiring, and T09
unchanged. It found two failure-containment defects (R3, R4), corrected in `6ff46a2a` (see "Review of e7720f39"), and the
full P1–P32 campaign ran on it. Withdrawn from the `e7720f39` records:
- that every badge object is removed at a boundary, disable or disposal and that the bound always empties: a failed
  destruction left the badge alive and active while its only ownership was cleared;
- that the hover redirect was released exactly: the lease was discarded before either cleanup step, so a failing
  `HoveredUnit` write skipped the row's false, and a failing row false left a highlighted row nobody retried.

**Corrected again after the owner review of `89ace80f` (2026-11-19).** The review accepted R3 and R4 and P1–P32 as
evidence for `6ff46a2a`, and found one cross-pair defect (R5), corrected in `e54009e9` (see "Review of 89ace80f"); the
P1–P34 campaign ran on it. Withdrawn from the earlier records: that every pair cleanup the world raises ends the active
hover redirect. Only the exact owning pair's cleanup ends it; an unrelated pair's cleanup only retries the cleanup still
owed; load, area unload, world discard, disable and disposal release it whatever its pair.

## Identity

| Item | Value |
|---|---|
| Base | `f835c503515fcc80ff1033bf50f4f771e34de15c`, the accepted Sprint 6 checkpoint. PR #51 is unmerged; the base was verified as the live head of `codex/kmc-known-issues-sprint6` (the parent has not advanced). Accepted Sprint 6 product build `7898b1a4` |
| Branch and worktree | `codex/kmc-known-issues-sprint7`, `worktrees/known-issues-sprint7` |
| Draft PR target and parent | `codex/kmc-known-issues-sprint6` (parent PR #51) |
| Commits | `ee5c20dc`: U01–U04, the Domain policies, component tests and the presentation contract script. `292cd23c`: the first records and the strengthened probe. `92310c52`: review of `292cd23c`, R1 and R2. `e7720f39`: its records (reviewed head). `6ff46a2a`: review of `e7720f39`, R3 and R4, with the extended probe. `89ace80f`: its records (reviewed head). `e54009e9`: review of `89ace80f`, R5, with the predicate's component test and the probe's R5 section (the product-build commit). Then the records commit carrying this version, which is the branch head |
| Product-build commit | `e54009e9` |
| Product DLL (clean Release rebuild of `e54009e9`) | SHA-256 `851c44d6473210afd82630f33871356681460f2337b95a63a843b8849a7df21d`, MVID `6551bd65-dc7e-4ba5-bbfb-3bf84ed277c2`; frozen copy `artifacts/known-issues-sprint7/review3/head-e54009e9/`. The Fast tier's own build is byte-identical; assembly references unchanged from `6ff46a2a` (12) |
| Reviewed DLL (`6ff46a2a`, head `89ace80f`) | SHA-256 `b28652182b6f1a937d9e3f25b597adca23c5873f2abddcdb1c70421124f13aa0`, MVID `bd10f3e4-16c4-445b-9202-d4605e4b50ef`; frozen copy `artifacts/known-issues-sprint7/review2/head-6ff46a2a/` |
| Earlier reviewed DLL (`92310c52`, head `e7720f39`) | SHA-256 `f9f4e71e921d70f2402f71b4a87515a05d1ca7db5413501a2265d81e592d0271`, MVID `e9bda246-9e40-48c1-af73-98caef770a58`; frozen copy `artifacts/known-issues-sprint7/review1/head-92310c52/` (hash re-verified) |
| First reviewed DLL (`ee5c20dc`, head `292cd23c`) | SHA-256 `16b446fd5590990f3fec918f0705b31650199f0275de421f3cf9ab005b9f7a7f`, MVID `3b7cc9f0-9d90-42b7-907c-75a10a461855`; frozen copy `artifacts/known-issues-sprint7/candidate/head-ee5c20dc/` |
| Base DLL used for before-evidence | Sprint 6 `7898b1a4`: SHA-256 `7da6d2c006ff3d71b9d51e29db729a514c6beda826f4611768c03347581dbc64`, MVID `19fba84e-efe2-4a0f-a82c-da26f79e60a4` (frozen at `artifacts/known-issues-sprint6/review4/head-7898b1a4/`, hash re-verified) |
| Workflow budget | `known-issues-sprint7` |

## Dispositions

| ID | Disposition | What changed |
|---|---|---|
| U01 | **Implemented and verified offline** | The mount's Move and Standard prediction actions show zero usable distance, and the rider-selected fill promises no five-foot step, once the mount's allocation for the exact live activation has recorded forced (charge) travel. Native Move unavailability, cooldowns, `TimeMoved`, the allocation and the path are unchanged |
| U02 | **Implemented and verified offline** | The exact public slot whose click handed native target selection to a hidden delivery ability is drawn with native's own selected mark, through an exact value-identity mapping that ends with the selection operation |
| U03 | **Implemented and verified offline** | The Paladin class panel builds its rows from projected copies of the shown character's progression data: a Divine Mount character, or one whose bond is not yet chosen, is not promised the weapon-bond-only upgrades. Grants, blueprints and the unit's data are unchanged |
| U04 | **Implemented and verified offline (native delivery and lifecycle corrected after the review of `292cd23c`; failure containment corrected after the review of `e7720f39`)** | A mounted rider's party portrait carries the project's saddle badge, reconciled at the portrait's native per-frame refresh and bind; every world boundary, disable and disposal hides and destroys every badge, and one whose removal fails stays owned as a hidden residue that is retried. Hovering a mount whose tracker entry is folded into its pair's paired activation highlights its own rider's existing entry under one owned lease, ended by its own pair's cleanup (never another pair's) and by every world boundary; its release is a bounded, weak cleanup obligation kept until each cleanup step has completed or native's own pass has recomputed the tracker |
| T09 (new) | **Open investigation: retained evidence absent** | `Test-Finalize2UpgradeReaders.ps1` reads run r05's retained receipts from the lab's `runtime-evidence`, which this machine does not hold. It fails identically at the Sprint 6 base. Not a product failure; see "New independent findings" |

## Sprint-wide presentation boundary

Every Sprint 7 change is a projection of current authoritative state onto native UI. None alters native action
availability, spending or refunds, native cooldowns, `TimeMoved` or movement allocations, command admission or
routing, initiative order or scheduling, paired activation identity, saved ability identity, blueprint GUIDs,
permanent feature facts, companion progression, the save format, selection ownership, combat participation, or any
accepted Sprint 1–6 behaviour. Decisions live in Domain (`MountedPresentationPolicy.cs`, `DivineMountPolicy`); the
Harmony patches are thin and contained (a presentation fault is reported once per seam and never reaches native).
No new asset or dependency: the badge reuses the embedded `MountSaddleIcon.png`, and the DLL's references are
unchanged and inside the package allowlist (no `UnityEngine.UI`, TMPro or UniRx reference; their members are reached
by exact field and method tokens).

## Reconciled behaviour at the base (before any edit)

Read from source and the installed 2.1.7b IL (`Assembly-CSharp` SHA `3b6450ff…`, MVID `07fa1e4d…`).

### Movement display (U01)

- **Panels:** the turn-based prediction panel's slot view models read native `CombatAction`s. Their distance label
  comes from `CombatAction.RemainingMoveDistance` (and `MaxMoveDistance`, `PredictedMoveDistance`), read in
  `PredictionPanelActionVM.UpdateDistance` / `PredictionPanelFullRoundActionVM.UpdateDistance` when native's
  `MoveDistanceUpdated` fires.
- **Writes:** every distance write goes through one native method, `CombatAction.UpdateRemainingDistance(float?
  remainingDistance, bool hasMovePossibility)` (0x0600704F): from `ActionsState.UpdateMoveDistance` (0x06007009,
  `GetRemainingActionMovementRangeFeet`, cooldowns only) and `PathVisualizer.UpdateActionStatesFromPath` (0x06007021).
  Nothing outside prediction/UI reads these fields (callers enumerated from IL).
- **Rider versus mount:** the activation's principal is always the rider and its partner the mount (`SavedCombatData`
  requires the partner context to be the mount). A rider-selected ground preview shows the mount's own actions
  (`partnerContext.ActionsStates`, refreshed by `FillPairedMountStates`); a mount-selected panel runs native on the same
  partner context. Other previews show the rider's own actions. Turn-based only; real time never reaches these paths.
- **Authority:** forced (charge) travel is recorded on the mount's allocation (`MountedMovementState.TimeForced`, from
  the agent's force mode or native `TimeMovedInForceMode`); `Remaining` then refuses all ordinary travel and the step.
  The allocation is created per activation by `BeginGrantedPreparation(turn, activation.Identity)` and persisted.
  Native `ShouldRestrictNormalMovement` reads the same fact, so Move is Unable, but `UpdateMoveDistance` still writes
  the cooldown-only distance: the 32/50 ft the F3 r05 c2 capture recorded.
- **Charge outcomes:** a delivered Charge and one that travelled then was cancelled both record forced time (nothing is
  refunded, F3 D4). A refused Charge and one that never obtained movement ownership record none.
- **Five-foot:** the fill rebased the mount's step from native only through `HasFiveFootStep`; native restricts the
  step after forced travel, but nothing tied the promise to the activation's closure.
- **Later repair:** none touched this after F3. The `Finalize3EconomyEvidence` reader accepts Unable with a positive
  distance, which is why F3 passed with the misleading number.

### Action-bar targeting (U02)

- **Identities:** the public control `KMC_MountStateAbility` (`26151090…`) and the stock Charge slot (`c78506dd…`)
  are the only public slots that swap in a hidden delivery: the Mount shell `f053faad…` (targets a friend, so it enters
  targeting), the Dismount shell `3af2b81f…` (self, no targeting) and the Mounted Charge shell `d79eaec2…` (targets an
  enemy). `ResolvePublicSlotDelivery` picks the shell.
- **Click:** `PublicSlotClickPrefix` binds the shell's `AbilityData` into `MechanicActionBarSlotAbility.Ability`;
  native `OnClick` calls `ClickWithSelectedAbilityHandler.SetAbility(shell)` (which raises the target-selection start
  event; `DropAbility` raises the end event); the postfix restores the public data.
- **Glow:** native `MechanicActionBarSlotAbility.UpdateSlotInternal` (0x06002F68) shows `ActiveMark` only when the
  slot's ability has a target anchor and `SelectedAbilityHandler.Ability == slot.Ability`. The handler holds the
  shell, the slot the public data: never equal, so no glow.
- **Owner and cleanup:** target selection, delivery, cancellation and replacement are native. A slot left swapped by an
  exception is repaired by `ReconcilePublicControlSlots`. The saved slot is captured only from the live public fact's
  data (kinds 6/7), and the native hotbar never stores a KMC identity.

### Paladin progression (U03)

- **Grants:** the level-5 entry is leased to the KMC Divine Bond selection; `FilterLevelEntryFeatures` (prefix on
  `LevelUpHelper.AddFeatures`, the only filtered path) drops the six weapon-bond upgrades (`WeaponBondUpgradeGuids`) for
  a character holding the Divine Mount selection. The upgrades stay in the stock blueprint for everyone.
- **Panel:** the character screen's class block (`CharSComponentClassBlock.FillData` → `GetUIProgressionLayout`
  0x06004A32) builds `UIProgressionLayout` from `unit.Progression.GetClassProgressions(class)`, the unit's own
  `ProgressionData` objects, whose `LevelEntries` include the upgrades. The descriptor passed is whichever native shows
  (the committed unit, or the level-up/respec preview). Nothing in the mod touched this path; the Sprint 8 handoff
  recorded the panel as not rewritten.
- **Other displays, not changed:** the per-level tooltips (`SetupLevelTooltips` → `DescriptionTemplatesLevelup`) and
  the console/gamepad character screen (`CharInfoProgressionClassVM`) read the blueprint's level entries.

### Portrait and turn tracker (U04)

- **Portraits:** `GroupCharacterPortraitController` (one per party portrait). `CharacterBase.Update` returns during
  loading and for an unbound portrait; otherwise it calls `UpdateCrippled` (0x06003F02) every frame and `HealthUpdate`
  (0x06003F01) only after the hit-point check finds a change. The unit is bound and rebound through `Initialize`
  (0x06003EF6, from `CharacterBase.Initialize`, `HandleLevelUpComplete` and `HandlePortraitChanged`). The mod had no
  portrait seam. (Corrected after the review of `292cd23c`: the first records said `HealthUpdate` ran every frame.)
- **Tracker:** `InitiativeTrackerVM.UpdateUnits` runs inside the mod's projection scope; `FilterTrackerSortedUnits`
  omits a mount whose entry is folded into its pair's live paired activation (`HidesPairedTrackerEntry`, or the legacy
  `ShouldSuppressMount`). A mount keeps its own entry when paired activation is off, after a dismount split from the
  release round, after its rider is removed, or while it is native `CurrentTurn.Unit`.
- **Hover:** native `InitiativeTrackerVM.HandleHoverChange(UnitEntityView, bool)` (0x06004F16, the tracker's
  `IUnitDirectHoverUIHandler`) highlights the entry whose unit's view is the hovered view. Only the world hover of a unit
  view raises it; a party-portrait hover does not (see "Review of 292cd23c"). A folded mount has no entry, so nothing
  highlights.
- **Relationship identity:** `MountedPairOwner` (id, rider, mount, generation), `GameMountedRelationshipService`
  (`State`, `MountedPairGeneration`, `Rider`, `Mount`), reached through `MountedWorldService.Pairs` / `ForActor`.

## U01 — unusable movement distance after Charge

- **Authority read:** `MountedMovementState.ForcedTravelRecorded` (`TimeForced > 0`), which `Remaining` now also reads, on
  the mount's allocation whose `GrantIdentity` equals the live activation's identity and is prepared
  (`MountedMovementDisplayPolicy.LocomotionClosed`).
- **Seam:** a prefix on native `CombatAction.UpdateRemainingDistance` asks the one pair whose partner context owns
  that exact `Move` or `Standard` action (`ProjectUsableMoveDistance`); the remaining distance native is about to store
  and announce becomes 0 (an absent value stays absent). Native's own `hasMovePossibility`, Unable state and every
  other action are untouched. The rider-selected fill offers native's step only through
  `PromisesFiveFootStep(closed, HasFiveFootStep)`.
- **Next activation:** a new activation has a new identity and a new allocation, so the projection ends by itself.

| Case | Result (compiled hook through the real native method, detached) |
|---|---|
| 1 delivered Charge | Move and Standard show 0 (native wrote 32 ft); `hasMovePossibility` unchanged; `TimeMoved`/`TimeForced` unchanged |
| 2 travelled then cancelled | 0 for the rest of the activation |
| 3 refused Charge | native 32 ft |
| 4 never obtained movement ownership | native 32 ft |
| 5 next lawful activation | native distance |
| 6–8 rider, mount, both selected | the mount's actions (shown for the rider-selected ground preview and the mount-selected panel) read the projection; the rider's own actions stay native |
| 9 another pair | native; a closure never crosses pairs |
| 10 unmounted actor | native |
| 11 initiative arrangements | the projected actions are the activation partner's (the mount) under either arrangement |
| 12 load/area boundary | no live activation projects nothing; the restored allocation of the same activation keeps the truthful closure |
| 13 projection failure | contained; native's write stands; paired activation disabled projects nothing |

## U02 — the public slot while its delivery targets

- **Identity (`MountedTargetingSlotIdentity`):** slot object, the public `AbilityData` it holds (both by weak serial),
  pair owner id, pair generation, relationship state, action owner (unit id), public and delivery blueprint ids, the
  delivery `AbilityData` (weak serial) and the native target-selection operation. Never a name, title, icon or bare
  index. A public or delivery ability whose own fact is no longer live reads as no ability.
- **Record:** the click prefix clears any mapping; the postfix restores the public data and records only when
  `MayRecord` holds: a delivery was swapped in, the public identity is back, native selection holds exactly that
  delivery, and the service is active. A throwing click runs no postfix (Harmony 1), so it records nothing.
- **Draw:** a postfix on native `UpdateSlotInternal` asks `PresentsTargetingSelection(slot)`; only the mapped slot
  whose live identity equals the record is drawn with native's own selected branch (the active mark in its default
  colour, shown where the slot is possible).
- **Lifetime:** every native target-selection start and end (any ability) advances the operation and clears the
  mapping (delivery, native cancellation, right-click/back, replacement). A changed identity of the mapped slot
  (recycled row, rebuilt fact, destroyed ability or shell, dismount, new generation) clears it. Load
  (`BeginLoadedWorld`), area unload, world discard, disable and disposal clear it.

| Case | Result |
|---|---|
| 1 one public slot targeting | that slot is drawn selected |
| 2 another ordinary ability | unchanged |
| 3 two mounted-control slots, a duplicate slot of the same ability | only the initiating slot; drawing others never takes its mark |
| 4 another pair | cannot inherit |
| 5–6 delivery, cancellation | clears |
| 7 replacement | old clears; the new exact mounted-control slot records its own |
| 8 public ability or shell destroyed | clears |
| 9 load, area, disable | clears (disposal and world discard pinned on IL) |
| 10 click routing throws | no mapping, old or new |
| 11 recycled row, or the same row rebound to a new fact's data | no glow |
| 12 save identity | the slot keeps its public canonical ability; nothing writes a shell into it |

Invariants: no cast, control, cost, refund, target replay, save-kind change, ability or targeting-authority change.
The mapping holds its slot only weakly (a dropped slot is collected).

## U03 — the class panel's bond rows

- **Seam:** a transpiler on `CharSComponentClassBlock.GetUIProgressionLayout` replaces its one
  `GetClassProgressions` call; native's read runs exactly once and its result is projected by
  `ProjectClassProgressions`.
- **Decision:** `DivineMountPolicy.ProgressionDisplayPath` reads the shown descriptor's own facts each time: Divine
  Mount selection held → Divine Mount (as the grant filter, even with the choice not applied); choice not applied or a
  character outside the party → Stock; Weapon Bond held → Weapon Bond; neither → Undecided. `ShowsProgressionRow`
  keeps an upgrade row only for Stock and Weapon Bond. Undecided promises neither path's upgrades; the level-5 Divine
  Bond row names both choices.
- **Projection:** only progression data with a row to omit is copied (shallow copy, new level entries and feature
  lists); the array, data objects and entries native returned are never modified. Nothing is cached, so a preview,
  its cancellation, another character and a replaced preview each read only their own descriptor.

| Case | Result |
|---|---|
| 1 new Paladin chooses Divine Mount; 4 existing Divine Mount | upgrades omitted, every other row kept |
| 2 chooses Weapon Bond; 5 existing Weapon Bond; 6 older stock-bond save | native rows |
| 3 not yet chosen | no path's upgrades; the choice row kept |
| 7–8 respec preview either way | follows the preview descriptor |
| 9 cancelled preview | the committed character shows its own rows |
| 10–11 switching characters, two contexts | no leakage |
| 12 preview destroyed or replaced | no filter state exists to go stale |

Invariants: native grants, companion progression, blueprints, GUIDs, facts and saves unchanged.

## U04 — mounted badge and folded mount hover

- **Facts:** `MountedPairPresentation.Facts()` reads each live relationship afresh: owner id, generation, rider and mount
  (the owner's and the relationship's actors must agree), mounted state, and whether the mount's own tracker entry is
  folded right now (`FoldsTrackerEntry`, the tracker filter's own predicate without its bookkeeping).
- **Badge:** `MountedPairPresentationPolicy.Badge` names the one live mounted relationship whose rider is exactly the
  portrait's unit. Postfixes on the portrait's native per-frame refresh (`UpdateCrippled`) and bind (`Initialize`) show
  or hide a child image under the portrait art using the embedded saddle icon (corrected from `HealthUpdate` after the
  review of `292cd23c`). A badge whose removal fails stays owned as a hidden cleanup residue (review of `e7720f39`). The image never takes a pointer event, so portrait clicks, hover and selection stay native. No
  tooltip was added (one would need the badge to receive pointer events, which would take them from the portrait).
- **Hover:** a prefix releases KMC's owned redirect before native's pass; after it, a postfix asks
  `FoldedMountHoverEntry`: only when native's own pass found no entry of exactly the hovered unit, exactly one live
  mounted relationship folds that mount, and exactly one entry is its rider's, native's own `HoveredUnit` and the
  entry's `HandleHoverChange(true)` are set, under a lease (see "Review of 292cd23c").

| Case | Result |
|---|---|
| 1 mount | badge on the exact rider |
| 2, 4, 5, 8 dismount, death, disposal | cleared (the relationship is no longer mounted) |
| 3 remount | new generation |
| 6–7 load, area/world | no pair, no badge |
| 9, 11 pair replacement, same rider with another mount | the new mount |
| 10 two pairs | badges and hover targets never cross |
| 12 same mount with another rider | an inconsistent relationship shows nothing |
| 13 independently acting mount | keeps its own native entry and hover |
| 14 folded shared mount | highlights its rider's existing entry |
| 15 tracker rebuilt | maps through the entries as they are now; none is held |
| 16 selection | never read or changed |
| 17 disable/re-enable | no badge or redirect while disabled; derived afresh after |
| 18 callback failure | contained |

Invariants: no initiative entry, order, scheduler, turn-ownership, selection or saved change; no synthetic actor.

## Reference lifetime and cleanup

- **U01, U03:** no state at all. Each native call reads the authority afresh.
- **U02:** one mapping per service instance (not static): an immutable value identity plus one `WeakReference` to the
  slot; object serials come from a `ConditionalWeakTable`, which keeps no key alive. Cleared at every selection
  operation and every lifecycle boundary listed above.
- **U04:** the presentation keeps the world service, the sprite source, the logger, the badge surface, at most one
  hover lease and at most four unfinished hover cleanup records (each: weak tracker, entry and rows references, the
  pair identity, the hovering unit's id and the operations still owed) and the badge records (a `WeakReference` to the
  object and flags), badges in use and cleanup residues together bounded at 32. Badge objects are children of the
  portrait art and die with it; load, area unload, world discard, disable and disposal hide and destroy them, and a
  record is dropped only once its object is no longer alive or has been collected. No tracker entry, view, unit or pair
  is held strongly.
- **Statics:** the patch bridge's `Presentation` reference is set and cleared with the patch controller, as every
  other bridge member. The once-per-seam fault set holds only the fixed seam names.
- **Checks:** the U02 and R1 GC checks (a dropped slot, a dropped tracker are collected), and structural field checks
  on both state holders and the lease.

## Validation (budget `known-issues-sprint7`), the reviewed `ee5c20dc`

| Check | Result |
|---|---|
| Clean Release rebuild of `ee5c20dc` | PASS; identity above (log `candidate/05-clean-release-build.log`); byte-identical to the Fast tier's build |
| Components | 1410/0 (1399 at the base) (log `candidate/01-components.log`) |
| `Validate-Source.ps1` | 151/0 |
| `Test-MountedPresentationContracts.ps1` (new; compiled seams detached) | **112/0** on the frozen candidate (log `candidate/02-presentation-probe-head-ee5c20dc.log`) |
| The same probe on the accepted Sprint 6 build | **every Sprint 7 section fails** on its own code: U01 case 1 (native writes 32 ft), U02 case 1 (no mark), U03 cases 1/4 (the upgrade rows shown), U04 (no presentation), registration (log `candidate/04-presentation-probe-sprint6-7898b1a4.log`) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint7` (18 focused) | **PASS 21/0** (0.85 min; budget 1.9 of 90 min, 2 entries; the first run, before the probe was strengthened, is kept as `06a-fast-tier-before-probe-strengthening.log`) (log `candidate/06-fast-tier.log`) |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `candidate/07-assembly-contracts.log`) |
| Mutation campaign P1–P20 | **20/20 caught** (log `candidate/03-mutation-campaign.log`; see below) |

- **Fast tier coverage:** the presentation contracts; command slot; paired activation (40 detached patch
  constructions); F3 economy readers (movement display); F2 controls readers; persistence data (kinds 6/7); Sprint 8
  Paladin readers; Sprint 7 charge admission and readers; post-load view readiness; persistence; world ownership;
  marker ownership; native input semantics; stall repair (273/0); turn-completion wrappers; ranged movement;
  continuous drive.
- **Run separately, same results as the base:** `Test-ChargeOwnershipContracts.ps1` exits 1 at the known T04 drift
  after 71 checks; `Test-Finalize2UpgradeReaders.ps1` stops at the absent r05 receipts (T09).
- **Not run:** Full, Candidate, historical campaigns, release qualification, native gameplay, physical UI acceptance,
  the resolution/input-device matrix, and the stale harnesses recorded elsewhere.

## Mutation campaign P1–P20 on `ee5c20dc` (serial; log `candidate/03-mutation-campaign.log`)

Each mutant: one disposable `git worktree add --detach` of `ee5c20dc`, its own `MSBuild /t:Rebuild /p:Configuration=Release
/m:1 /nodeReuse:false /p:UseSharedCompilation=false`, the components, then the presentation probe reading that DLL by
path and printing its MVID; the worktree is removed before the next. All 26 anchors were checked to occur exactly once.
The candidate DLL is the separate frozen copy.

| ID | Mutation | Caught by |
|---|---|---|
| P1 | U01 native remaining distance displayed while locomotion is activation-forbidden | components; probe U01 case 1 |
| P2 | U01 `TimeMoved` cleared to obtain the display | probe: "TimeMoved and TimeForced are never written" |
| P3 | U01 projection survives into the next activation | components; probe U01 case 5 |
| P4 | U01 projection reaches another pair or actor | probe U01 cases 6-8 (the rider's own actions) and 9 |
| P20 | U01 a five-foot step promised after charge travel | components |
| P5 | U02 slots matched by the ability they show, not the exact slot | components; probe U02 case 3 (duplicate slot) |
| P6 | U02 another slot of the same owner receives the glow | components; probe U02 case 3 |
| P7 | U02 mapping survives a native load | probe U02 case 9; IL check of `BeginLoadedWorld` |
| P8 | U02 hidden delivery replaces the public slot (no restore) | probe U02 case 1 |
| P9 | U02 recycled row inherits the glow (binding by blueprint, not ability data) | probe U02 case 11 (rebound to a rebuilt fact's data) |
| P10 | U03 weapon-bond rows removed from the shared progression data itself | probe U03 case 9 (committed rows after a preview) |
| P11 | U03 Divine Mount preview shows weapon-bond-only upgrades | components; probe U03 cases 1/4 |
| P12 | U03 Weapon Bond character loses its valid rows | components; probe U03 cases 2/5/6 |
| P13 | U03 cancelled preview leaks into the committed presentation (sticky first path) | probe U03 cases 2/5/6 (later reads keep the first path) |
| P14 | U03 another character inherits an earlier projection | probe U03 cases 2/5/6 |
| P15 | U04 two pairs share one badge | components; probe U04 cases 1/10 |
| P16 | U04 stale pair generation keeps its presentation (cached badge) | probe U04 cases 2, 4, 5 and 8 |
| P17 | U04 independently acting mount folded | probe U04 case 13 (the projected fold fact) |
| P18 | U04 hover reorders the tracker | probe: "the hover adds no entry and keeps the tracker's order" |
| P19 | U04 a tracker row retained strongly | probe: lifetime field check |

**First pass, kept** (`candidate/03a-mutation-campaign-first-pass.log`): 17 caught, P9 and P17 survived, P13 did not
build (one parenthesis too many in the mutant). P9 survived because the probe's rebound-row case clicked a slot still
holding an earlier "recycled" ability, so it never matched for an unrelated reason; P17 survived because the hover
policy's second guard (a unit with its own entry is never redirected) masked the fold fact. The probe was corrected
(the rebound case restores the public slot first; the projected fold fact and an out-of-view independent mount are
checked directly), the mutant was fixed, and the whole campaign was rerun on the corrected probe: 20/20.

## Review of 292cd23c: R1–R3 (U04 native delivery and lifecycle, corrected)

### Native hover contract (installed 2.1.7b IL; executed where noted)

- **Raiser:** `IUnitDirectHoverUIHandler.HandleHoverChange` is called by exactly one method in the assembly, the event
  closure of `UnitEntityView.HandleHoverChange(bool)` (0x0600185A), which first sets the view's `MouseHighlighted`.
- **Source:** `UnitEntityView.HandleHoverChange` is the view's `IDetectHover` implementation. `PointerController.OnHoverChanged
  (oldHover, newHover)` calls `IDetectHover.HandleHoverChange(false)` on the old object's detectors and then
  `HandleHoverChange(true)` on the new object's (order pinned from the IL). `PointerController2D` (the kingdom map) does
  the same.
- **Subscribers:** the tracker's `InitiativeTrackerVM.HandleHoverChange` (0x06004F16), `InspectController` and
  `ActionBarInspectVm`.
- **Party portrait:** `GroupCharacterPortraitController.OnPointerEnter/Exit` set `IsMouseOver`, which sets the unit view's
  `MouseHighlighted` and, in turn-based combat, raises `IUnitPortraitHighlightChangedHandler`, implemented only by
  `TurnController` and `PathVisualizer`. It never calls `UnitEntityView.HandleHoverChange`, so a portrait hover never
  reaches the tracker. The redirect therefore stays on the tracker's own seam, for the mount's world hover.
- **Native pass, executed detached on genuine tracker view models:**
  - a true sets `HoveredUnit` to the entry whose unit's view is the hovered view, or to none, and sets every entry's hover
    to whether it is that entry;
  - a true for another unit recomputes every entry;
  - an unmatched unit leaves no hovered entry and no highlight;
  - a false sets `HoveredUnit` to none and every entry's hover to false.
- **Consequence:** native's own true and false passes already clear any entry KMC highlighted. What native does not do
  is end KMC's redirect when the tracker rebuilds, the pair changes, or a world boundary passes while the pointer
  stays on the mount.
- **Inference, not IL:** a folded mount (no tracker row) still produces both events, because the view raises them
  regardless of the tracker's rows; which collider the pointer picks over a mounted pair was not observed.

### R1 — the hover redirect lease

- **Prefix** (`FoldedMountHoverPrefix` → `BeforeTrackerHover`): before every native hover pass, true or false, any unit,
  any tracker, KMC's lease is released exactly. A pass nested inside KMC's own apply or release is native's alone.
- **Postfix** (`RedirectFoldedMountHover`): only for a true, only when native's pass found no entry of exactly the hovered
  unit (`HoveredUnit`'s own unit is compared, not just its presence), only for a mount folded into its live mounted
  pair, and only onto the one existing entry of that pair's rider. The lease is taken before the first write, so a
  failure part-way is released exactly and rethrown into the patch's containment.
- **Lease:** weak references to the tracker, the entry and the tracker's rows list; the pair identity (owner,
  generation, rider, mount); the hovering unit's id.
- **Release:** `false` on the exact entry, only while it is still a row of the same rows list (an entry of a rebuilt
  tracker is never touched again); `HoveredUnit` cleared only if it is still that entry; the lease cleared first.
  *Superseded by R3 (review of `e7720f39`): clearing the lease first lost the cleanup when a step threw; release is now
  a retained cleanup obligation.*
- **Ends:** every native hover pass; a tracker rebuild (`UpdateUnits` postfix validates the rows list, the entry, the pair's
  owner, generation, mounted and folded state); every pair cleanup the world raises (dismount, death, load, area
  unload, disable, teardown); load start; area unload; world discard; disable; disposal. *Corrected by R5 (review of
  `89ace80f`): a pair cleanup ends the redirect only when it is the redirect's own pair; another pair's cleanup only
  retries owed cleanup.*

| Case (native pass executed under the product's real Harmony12 prefix and postfix) | `ee5c20dc` | `92310c52` |
|---|---|---|
| true then false for one folded mount | behaviour passes (native's false clears); no lease to release | passes; lease released |
| pair A true then pair B true, no false between | behaviour passes (native's true recomputes); no lease | passes; the lease is pair B's |
| folded mount, then an ordinary native entry, and back | passes | passes |
| tracker rebuilt during hover | **fails** (nothing ends the redirect at the rebuild) | passes; the old entry is never touched again |
| pair-generation change during hover | **fails** (rider stays highlighted) | passes |
| dismount, death (pair cleanup) during hover | **fail** | pass |
| load, area unload, world discard | **fail** | pass |
| disable; disposal | **fails**; disposal cannot run detached | pass |
| independent mount | no redirect | no redirect, no lease |
| another pair on another tracker | — | A released, B applied |
| nested native hover inside KMC's apply | — | native's nested pass stands; the next false leaves nothing |
| throwing native callback and throwing logger | — | no entry stuck, no lease, nothing escapes |
| collected tracker | — | collected (weak); later hovers work |

The reviewed build is not wrong for an ordinary true-then-false: native's own pass owns that cleanup, now pinned by
executing it. It is wrong at every boundary that ends the redirect while the pointer stays.

### R2 — the badge lifecycle

- **Refresh:** postfixes on `UpdateCrippled` (every frame for every bound portrait outside loading) and `Initialize`
  (bind and rebind, including to no unit). Not `HealthUpdate`. No polling and no object scan: each call reconciles
  only its own portrait.
- **Reconcile:** the badge is active exactly while the portrait's unit is the rider of a live mounted relationship;
  otherwise an existing badge is deactivated. A dismount, death, generation change, pair replacement or rebind
  therefore shows within one frame of the next native refresh.
- **Create:** only with a sprite and under the 32-object bound; dead objects never count (the engine's destroyed objects are pruned before
  counting). A badge that cannot be completed is destroyed before the failure propagates; the failure is contained.
- **Remove:** load start, area unload and world discard (`ReleaseAtWorldBoundary`, wired at the persistence load,
  area-suspension and world-discard sites), disable (`ReleaseAtWorldBoundary("disabled")`) and disposal remove every
  badge object. Each removal is tried on its own; the logger's own failure is contained; the bound always empties.
  *Superseded by R4 (review of `e7720f39`): emptying the bound after a failed removal left that badge alive, active and
  unowned; a failed removal now stays owned as a hidden residue.*
  Badges remain transient and are never saved.
- **Engine surface:** the six engine operations of a badge object (find, create, alive, active, set active, destroy) are
  one `MountedBadgeSurface`; production uses Unity, and the detached probe substitutes it to run the lifecycle.

| Case | `ee5c20dc` | `92310c52` |
|---|---|---|
| seam | `HealthUpdate` only (hit-point changes): **fails** registration and has no lifecycle seam | `UpdateCrippled` + `Initialize` |
| 1 mounted rider; 10 two pairs | — | one active badge each, never crossed |
| 2 dismount; 4 death | — | inactive at the next refresh; the other pair keeps its own |
| 3 remount, new generation | — | active again, no duplicate |
| 5 pair replacement | — | one badge on the rider |
| 6 portrait rebound | — | hidden at the bind |
| 7 load, area, world | removed only at disable/disposal | every object removed, bound empty; destroyed objects never count |
| 8 disable, disposal; 9 re-enable | — | removed; derived afresh |
| 11 no sprite | — | no object; the portrait unchanged |
| 12 construction or destruction failure, throwing logger | `Reset` called the logger uncontained | contained; every other removal runs; the bound empties (*superseded by R4: the failed badge stayed live and active*) |

### R3 — records

U04 was not described as fully implemented until R1 and R2 passed; it is now recorded as implemented and verified
offline with its native delivery and lifecycle corrected. The no-screen and no-native limits stand. The mount-name
tooltip stays omitted. T09 is unchanged.

### Validation for `92310c52` (budget `known-issues-sprint7`; logs `artifacts/known-issues-sprint7/review1/`)

| Check | Result |
|---|---|
| Clean Release rebuild of `92310c52` | PASS; identity above (log `05-clean-release-build.log`); byte-identical to the Fast tier's build; references unchanged |
| Components | 1410/0 (log `01-components.log`) |
| `Validate-Source.ps1` | 151/0 |
| `Test-MountedPresentationContracts.ps1` (extended: native contracts, native hover executed, R1 and R2 lifecycles) | **166/0** (log `02-presentation-probe-head-92310c52.log`) |
| The same probe on the reviewed `ee5c20dc` | 13 failures: the R1 lease and every boundary release, R2 (no lifecycle seam), registration (log `04-presentation-probe-reviewed-ee5c20dc.log`) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint7` (18 focused) | **PASS 21/0** (0.9 min; budget 2.8 of 90 min, 3 entries) (log `06-fast-tier.log`) |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `07-assembly-contracts.log`) |
| `Test-Harness.ps1` (turn-tracker projection pins and the wider harness) | 277 pass, 4 fail, and the same failing set on the Sprint 6 base `f835c503` under matched conditions (logs `08-harness.log`, `08b-harness-base-f835c503.log`): two need a historical package zip absent from the lab, one pins a source string absent at the base, one is the package validator's size check. Pre-existing; none involves Sprint 7. The tracker filter pins pass |
| Mutation campaign P1–P27 | **27/27 caught** (log `03-mutation-campaign.log`) |

- **Harmony12 construction:** the hover prefix and postfix were constructed and executed on native
  `HandleHoverChange`; `UpdateCrippled`, `Initialize` and `UpdateSlotInternal` reference engine internal calls and
  cannot be compiled detached (their postfix bindings are checked by name and type).
- **Neighbours inside the Fast tier:** paired activation (40 constructions), world ownership, persistence, post-load view
  readiness, command slot (action-bar controls), F3 economy readers (movement display), Sprint 8 Paladin readers,
  marker ownership, native input, stall repair, Charge, ranged movement, continuous drive.
- **Not run:** Full, Candidate, historical campaigns, release qualification, native gameplay, physical visual
  acceptance, resolution/input matrices, unrelated stale harnesses.

### Mutation campaign P1–P27 (serial; log `review1/03-mutation-campaign.log`)

Each mutant: one disposable `git worktree add --detach` of `92310c52`, its own `MSBuild /t:Rebuild /p:Configuration=Release
/m:1 /nodeReuse:false /p:UseSharedCompilation=false`, the components, then the presentation probe reading that DLL by
path and printing its MVID; the worktree is removed before the next. All 34 anchors were checked to occur exactly once.
The candidate DLL is the separate frozen copy. **27/27 caught.**

| ID | Mutation | Caught by |
|---|---|---|
| P1–P20 | as listed for `ee5c20dc` above, rerun on `92310c52` | each caught again, for the same reason |
| P21 | a folded mount's false does not release the redirected rider (the lease survives the false) | probe R1 true->false (the lease remains) and the nested-pass follow-up |
| P22 | hovering pair B leaves pair A's lease in place and refuses B's redirect | probe R1 A->B (B's rider not highlighted) and the direct native hover case |
| P23 | a tracker rebuild keeps the lease on a stale entry and rows reference | probe R1 tracker rebuilt during hover |
| P24 | a dismount or generation change leaves the badge active | probe R2 cases 2 and 4 |
| P25 | a load or world replacement leaves old badge objects tracked and active | probe R2 case 7 (load, area, world) |
| P26 | a throwing badge-removal logger stops later cleanup and escapes | probe R2 case 12 |
| P27 | the hover redirect registered on another subscriber of the view hover event (`InspectController`) | probe registration (the tracker's 0x06004F16) |

After the campaign, one probe check in the badge section that still aborted its section (it followed a `;`) was made a
reporting case like its neighbours; no verdict changed (the frozen candidate 166/0, `ee5c20dc` 13 failures, logs
refreshed).

## Review of e7720f39: R3–R4 (U04 failure containment, corrected)

The review accepted U01 and U02 completely at the offline engineering checkpoint, U03 for the character-screen class
panel, U04's pair identity policies, its installed native hover-source contract, the `UpdateCrippled` and `Initialize`
portrait seams and the lifecycle wiring (load, area unload, world discard, disable, disposal), and T09 unchanged. It
found two failure-containment defects. Both are corrected in `6ff46a2a`.

### Native facts the design rests on (installed IL; executed detached)

- `InitiativeTrackerVM.HandleHoverChange` (0x06004F16) writes `HoveredUnit.Value` first and then calls
  `InitiativeTrackerUnitVM.HandleHoverChange(bool)` (0x06004ECF) on **every** current row (true for the found row,
  false for the rest; false for all on a false). A completed native pass therefore recomputes the whole tracker.
- `HoveredUnit` is a UniRx `ReactiveProperty`: its setter stores the value and then notifies its subscribers, so a
  throwing subscriber is the realistic failure of the `HoveredUnit` write. The row callback sets the row's own
  `IsHover` property, whose subscribers can fail the same way.
- Harmony runs no postfix after the original throws, so the product's hover postfix runs only after a completed pass.

### R3 — the hover cleanup obligation

- **Release is custody, not discard.** Releasing the lease (`ReleaseHover`) turns it into a cleanup record owing two
  operations: clear `HoveredUnit` if it is still KMC's entry, and false on the exact entry KMC lit while it is still a
  row of the same rows list. The record is the lease itself: weak tracker, entry and rows references; pair owner,
  generation, rider and mount; the hovering unit's id; the two owed flags; a once-only report flag.
- **Independent operations.** Each is attempted in its own `try`; a failure of one never prevents the other; each
  failure is reported once per record through a logger whose own failure is contained.
- **Settled when, and only when:** each owed operation has completed; or its target no longer exists (tracker or entry
  collected) or is no longer KMC's (`HoveredUnit` holds something else; the entry is no longer a row of the same rows
  list, so a rebuilt tracker's old entry is never touched and its new rows are never confused with it); or native's
  own completed hover pass on that same tracker has recomputed every row (the postfix observes completion).
- **Retried at:** the next tracker hover prefix (before native's pass, which is never skipped), tracker rebuild
  validation (`UpdateUnits` postfix), every pair cleanup (dismount, death, load, area, disable, teardown; any pair's
  cleanup retries owed obligations, only the owning pair's ends the live lease, R5), load start,
  area unload, world discard, disable and disposal.
- **New redirects never overwrite unfinished cleanup.** The live lease and the unfinished records are separate; the
  records are bounded at four. On the same tracker, native's completed pass settles any record before the postfix may
  redirect; on another tracker a new redirect is applied beside the retained record with its own identity; at the
  bound a redirect is not applied and native's own result stands (`HoverRedirectRefusedCount`).
- **Unchanged:** no gameplay, tracker order, entry creation, initiative, scheduler, selection or relationship state.
  A disposal whose cleanup still fails leaves the row as native left it; native's own next pass on that tracker
  recomputes it (executed).

| Case (production adapter, native tracker objects, native hover pass under the real Harmony12 patches) | `92310c52` | `6ff46a2a` |
|---|---|---|
| 1 `HoveredUnit` write throws (its subscriber) at release | **fails**: the row's false is never attempted; the rider stays lit; nothing owed | the row's false still runs; the failed write stays owed; tracker-rebuild validation settles it |
| 2 the rider row's false throws at release | **fails**: `HoveredUnit` cleared, the row stays lit, the lease is gone, no retry can settle it | `HoveredUnit` cleared; the row stays owed through a still-failing retry; a later retry settles it |
| 3 both throw | **fails** | nothing escapes; one weak record remains; a later world boundary settles it |
| 4 dismount with a failing false | **fails** | owed; the next pair cleanup settles it |
| 5 load and world boundaries with a failing false | **fail** | owed through both; a later load settles it |
| 6 disable, then disposal, with a failing false | **fail** | disable keeps it; disposal settles it; a disposal that still fails completes and native's next pass recomputes the row |
| 7 tracker rebuild while cleanup is pending | passes (it never retries anything) | the old entry is never touched, the new rider row never confused with it; custody settles |
| 8 pair A's cleanup pending, then pair B | **fails** on another tracker (A's row stays lit forever) | B applied beside A's retained record, identities never cross; the next prefix settles A and releases B exactly; on the same tracker B follows A's settlement |
| bound: four unfinished records | **fails** (no bound, no custody) | a fifth redirect is not applied; native's result stands; all settle once the callback succeeds |
| 9 collected tracker and entry | no record to hold | weak record; the tracker is collected; settles without touching another tracker's native hover |
| 10 healthy true/false, A-to-B | passes | passes (and the full R1 section) |
| logger throws throughout | — | every failure is reported to it; nothing escapes |

### R4 — badge cleanup residues

- **Record:** each badge object this presentation made is a `BadgeRecord`: a weak reference to the object, and flags
  (residue, hidden, destroy requested, reported). Badges in use and residues share the one bound of 32. No portrait,
  GameObject, component, unit, pair or world reference is held strongly.
- **Cleanup** (load start, area unload, world discard, disable, disposal): every record becomes a residue; each is first
  deactivated, then destroyed, each operation in its own `try` and reported once. A record is removed only when its
  object is collected or the engine reports it no longer alive; otherwise it stays, hidden where deactivation worked
  and still needing hiding where it did not.
- **Retry:** every portrait refresh and bind (the same postfix), every boundary, disable and disposal settle residues
  and prune records whose objects the engine destroyed or the collector reclaimed, so a later valid portrait obtains its
  badge once capacity returns.
- **Never reused:** a residue is never found as a portrait's badge (`Find` takes the presentation's predicate), so a
  portrait whose old badge could not be destroyed gets a new object of the same name, and the residue's retry acts
  only on its own weak object.
- **Creation:** `Create` makes the bare object, which is recorded at once; `Build` completes it. A failure part-way makes
  the part-built object a residue (deactivated, destroyed, kept if destruction fails) and rethrows into the patch's
  once-per-seam containment.
- **Unowned objects:** a badge object found under a portrait that this presentation does not own is adopted under the
  bound, or deactivated when the bound is full.

| Case (production adapter; the badge's engine operations substituted) | `92310c52` | `6ff46a2a` |
|---|---|---|
| 1 A's destroy throws; B and C | B and C removed | B and C removed |
| 2 A after the boundary | **fails**: alive, active, untracked (bound reported empty) | inactive, one owned residue |
| 3 later retry | **fails**: nothing retries | the next portrait refresh destroys A; the residue clears |
| 4 deactivation and destruction both throw | **fails** | nothing escapes; owned, still needing hiding; a later boundary hides and destroys it |
| 5 logger throws throughout | contained | contained; every failure reported to it |
| 6 disable with a failed destroy | **fails** (left active) | completes; hidden and owned; not shown while disabled |
| residue under a re-enabled portrait | **fails** (the failed object is reactivated) | never reused; the portrait gets its own new badge; the retry destroys only the residue |
| 7 disposal with a failed destroy | **fails** (left active) | completes; the failed object hidden, every other removed |
| 8 load and world replacement, every removal failing | **fails** (all visible) | none visible, all owned; the next boundary removes them |
| 9 construction fails after the object exists, destruction fails | **fails**: an active orphan | hidden and owned; a later refresh destroys it and the portrait gets a complete badge |
| 10 forty failing constructions | **fails**: forty active orphans | at most 32 owned, none visible |
| 11 destroyed and collected residues | — | pruned |
| 12 a valid portrait once capacity returns | **fails** | waits at the bound; obtains its badge once the residues are destroyed |
| R2 case 12 (replaced assertion) | **fails**: the failed badges stay live and active while `TrackedBadgeCount` is 0 | hidden and counted |

### Before and after against `92310c52`

`Test-MountedPresentationContracts.ps1` (220 checks in all) on the frozen `92310c52` DLL (MVID `e9bda246…`):
**179 pass, 41 fail**: 20 of the 29 R3 checks, 19 of the 25 R4 checks, R2 case 12 and the disposal check after it.
The R3 checks it passes are those where nothing needs retrying (case 7, the same-tracker case 8, case 9's collected
tracker, case 10, the completion of a failing disposal, the logger); the R4 checks it passes are cases 1, 5 and 11 and
the disabled refresh. Everything before (U01–U03, the native contracts, R1, the rest of R2 and registration) passes. On `6ff46a2a`: ****220/0**** (log `review2/02-presentation-probe-head-6ff46a2a.log`;
the old run `review2/04-presentation-probe-reviewed-92310c52.log`).

### Validation for `6ff46a2a` (budget `known-issues-sprint7`; logs `artifacts/known-issues-sprint7/review2/`)

| Check | Result |
|---|---|
| Clean Release rebuild of `6ff46a2a` (`/m:1 /nodeReuse:false /p:UseSharedCompilation=false`) | PASS; identity above (log `05-clean-release-build.log`); byte-identical to the Fast tier's build; references unchanged |
| Components | 1410/0 (log `01-components.log`) |
| `Validate-Source.ps1` | 151/0 (log `01b-validate-source.log`) |
| `Test-MountedPresentationContracts.ps1` (native contracts, native hover pass executed under the product's real Harmony12 prefix and postfix, portrait seam bindings, R1-R4) | **220/0** on the frozen DLL (log `02-presentation-probe-head-6ff46a2a.log`) |
| The same probe on the reviewed `92310c52` | 179/41 (log `04-presentation-probe-reviewed-92310c52.log`) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint7` (18 focused) | **PASS 21/0** (0.95 min; budget 3.73 of 90 min, 4 entries) (log `06-fast-tier.log`) |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `07-assembly-contracts.log`) |
| `Test-Harness.ps1` (turn-tracker projection pins and the wider harness) | 277 pass, 4 fail: the same four as at the Sprint 6 base `f835c503` (`review1/08b-harness-base-f835c503.log`): two need a historical package zip absent from the lab, one pins a source string absent at the base, one is the package validator's size check. The tracker filter pins pass (log `08-harness.log`) |
| Mutation campaign P1-P32 | **32/32 caught** (log `03-mutation-campaign.log`) |

- **Neighbours inside the Fast tier:** paired activation (40 Harmony12 patch constructions), world ownership, persistence and load boundaries, post-load view readiness, command slot (action-bar controls), F3 economy readers (movement display), Sprint 8 Paladin readers (class-panel projection), marker ownership, native input, stall repair, Charge, ranged movement, continuous drive.
- **Harmony12:** the hover prefix and postfix were constructed and executed on native `HandleHoverChange`; `UpdateCrippled`, `Initialize` and `UpdateSlotInternal` reference engine internal calls and cannot be compiled detached (their postfix bindings are checked by name and type).
- **Not run:** Full, Candidate, historical campaigns, release qualification, native gameplay, physical visual acceptance, resolution/input matrices, unrelated stale harnesses.


### Mutation campaign P1–P32 (serial; log `review2/03-mutation-campaign.log`)

Each mutant: one disposable `git worktree add --detach` of `6ff46a2a`, its own `MSBuild /t:Rebuild /p:Configuration=Release
/m:1 /nodeReuse:false /p:UseSharedCompilation=false`, the components, then the presentation probe reading that DLL by
path and printing its MVID; the worktree is removed before the next. All 39 anchors were checked to occur exactly once
(P22 and P23 re-anchored where R3 moved their lines; the mutations are unchanged). The candidate DLL is the separate
frozen copy `review2/head-6ff46a2a/`. **32/32 caught.**

| ID | Mutation | Caught by |
|---|---|---|
| P1–P27 | as listed above, rerun on `6ff46a2a` | each caught again; P21 by R1 true->false (4 failures), P22 by R1 A->B (4), P23 by R1 rebuild (1), P24 by R2 cases 2, 4 and 6 (3), P25 by R2 case 7 and the R3/R4 boundary cases (23), P26 by the R3 failure cases whose logger throws (8), P27 by registration |
| P28 | clearing `HoveredUnit` throws and prevents the row's false | probe R3 case 1 (the row stays lit; 1 failure) |
| P29 | the row's false throws and the obligation is discarded | probe R3 case 2 and the cases that need a retried false (16 failures) |
| P30 | a failed badge destroy leaves an active badge untracked (the old `Reset`) | probe R2 case 12, its disposal check and R4 case 2 onward (17 failures) |
| P31 | a failed partial badge creation leaves an active orphan (the old creation catch) | probe R4 cases 9 and 10 (5 failures) |
| P32 | failed badge cleanup permanently consumes capacity (a settled residue is never pruned) | probe R2 case 7 and the R4 retry and capacity cases (18 failures) |

## Review of 89ace80f: R5 (one pair's cleanup and another pair's hover redirect)

The review accepted U01, U02, U03 for the character-screen class panel, U04's native hover and portrait seams, R3's
bounded weak hover-cleanup obligations, R4's bounded weak badge-cleanup residues, P1–P32 as evidence for `6ff46a2a`,
and T09 unchanged. It found one cross-pair defect, corrected in `e54009e9`.

- **Defect (in `6ff46a2a`):** the world raises `PairDismounting(relationship, trigger)` once per pair, before that pair's
  state changes. `HandlePairCleanup` ignored the relationship and released whichever redirect was live, so pair A's
  dismount or death cleared pair B's tracker highlight while the pointer stayed on pair B's mount, with no hover
  transition to re-apply it.
- **Owning-pair predicate:** `MountedPairPresentationPolicy.CleanupOwnsRedirect(cleanupPair, redirectPair)` is true only
  when both identities exist, the cleanup's pair owner is set, its rider and mount ids are non-empty, and the pair owner
  id, mounted-pair generation, rider id and mount id are all equal (`MountedPairPresentationIdentity.Equals`, ordinal).
  `cleanupPair` is read from the event's relationship exactly as `Facts()` reads every pair: `PairOwner`, `Rider` and
  `Mount`, which must agree with the owner's own actors, and `MountedPairGeneration`; otherwise it is null. The
  redirect's identity is the one the lease captured when it was taken. Nothing reads names, selection, tracker
  position or the last active pair.
- **Handler:** if a lease is live and the predicate holds, the lease becomes its retained cleanup obligation and settles
  (R3, unchanged). Otherwise, for an unrelated pair, a mismatched generation or owner, or an identity that cannot be
  read, the active lease and its visible redirect are untouched and only the obligations already owed are retried, without
  reinterpreting their identities. An identity read that throws is reported once to a logger whose own failure is
  contained; the handler never throws.
- **Global boundaries stay global:** load start, area unload, world discard, disable and disposal still release the
  active lease whatever its pair (`ReleaseAtWorldBoundary`, `Dispose`).
- **Unchanged:** the cleanup bound (4), the badge, tracker order, selection, initiative, scheduler, relationship state
  and the native hover pass.

| Case (production adapter, native tracker objects, native hover pass under the real Harmony12 patches; logger throws throughout) | `6ff46a2a` | `e54009e9` |
|---|---|---|
| 1 pair B live; pair A manual dismount | **fails**: pair B's row, `HoveredUnit` and lease cleared | pair B's rider highlighted, `HoveredUnit` pair B's rider, lease live |
| 2 pair B live; pair A death | **fails** | unchanged |
| 8 two unrelated cleanups in succession (A, then a third pair) | **fails** | pair B active |
| 7 a cleanup whose identity read throws (even naming pair B's owner) | **fails** (releases pair B) | pair B active; nothing escapes |
| 5 the same actors under another mounted-pair generation | **fails** | cannot release the current lease |
| 10 no hover event after the unrelated cleanups (and an unchanged tracker's validation) | **fails** | highlight remains; no release counted |
| 6 pair A owes a failed cleanup while pair B is live; pair A's cleanup | **fails** (pair B released too) | A's residue retried and settled; pair B live |
| 3 pair B's own manual dismount | passes | released; cleanup settles |
| 4 pair B's own death | passes | released |
| pair B's own cleanup with a failing false | passes | the lease becomes its retained obligation; an unrelated cleanup then settles it |
| 9 load, area unload, world discard, disable, disposal | pass | each releases pair B |
| 11, 12 R3 false-callback, setter-failure and capacity cases; healthy true/false, A-to-B, rebuild, independent mount | pass | pass (the R1, R3 and U04 sections unchanged) |

`Test-MountedPresentationContracts.ps1` on the frozen `6ff46a2a` DLL (MVID `bd10f3e4…`): **232 pass, 7 fail**: exactly
cases 1, 2, 5, 6, 7, 8 and 10 (log `review3/04-presentation-probe-reviewed-6ff46a2a.log`). On `e54009e9`: **239/0**
(log `review3/02-presentation-probe-head-e54009e9.log`). Components gain one test of the predicate (owner, generation,
rider, mount and unestablished identities): 1411/0.

### Validation for `e54009e9` (budget `known-issues-sprint7`; logs `artifacts/known-issues-sprint7/review3/`)

| Check | Result |
|---|---|
| Clean Release rebuild of `e54009e9` (`/m:1 /nodeReuse:false /p:UseSharedCompilation=false`) | PASS; identity above (log `05-clean-release-build.log`); byte-identical to the Fast tier's build; references unchanged |
| Components | 1411/0, one new test of the owning-pair predicate (log `01-components.log`) |
| `Validate-Source.ps1` | 151/0 (log `01b-validate-source.log`) |
| `Test-MountedPresentationContracts.ps1` (native contracts, native hover pass under the product's real Harmony12 prefix and postfix, portrait seam bindings, R1-R5) | **239/0** on the frozen DLL (log `02-presentation-probe-head-e54009e9.log`) |
| The same probe on the reviewed `6ff46a2a` | 232/7, the unrelated-pair cases (log `04-presentation-probe-reviewed-6ff46a2a.log`) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint7` (18 focused) | **PASS 21/0** (0.91 min; budget 4.63 of 90 min, 5 entries) (log `06-fast-tier.log`) |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `07-assembly-contracts.log`) |
| `Test-Harness.ps1` (turn-tracker projection pins and the wider harness) | 277 pass, 4 fail: the same four as at the Sprint 6 base (`review1/08b-harness-base-f835c503.log`); the tracker filter pins pass (log `08-harness.log`) |
| Mutation campaign P1-P34 | **34/34 caught** (log `03-mutation-campaign.log`) |

- **Neighbours inside the Fast tier:** paired activation (40 Harmony12 patch constructions), world ownership, persistence and load boundaries, post-load view readiness, command slot (action-bar controls), F3 economy readers (movement display), Sprint 8 Paladin readers (class-panel projection), marker ownership, native input, stall repair, Charge, ranged movement, continuous drive.
- **Harmony12:** the hover prefix and postfix were constructed and executed on native `HandleHoverChange`; `UpdateCrippled`, `Initialize` and `UpdateSlotInternal` cannot be compiled detached (their postfix bindings are checked by name and type).
- **Not run:** Full, Candidate, historical campaigns, release qualification, native gameplay, physical visual acceptance, resolution/input matrices, unrelated stale harnesses.


### Mutation campaign P1–P34 (serial; log `review3/03-mutation-campaign.log`)

Each mutant: one disposable `git worktree add --detach` of `e54009e9`, its own `MSBuild /t:Rebuild /p:Configuration=Release
/m:1 /nodeReuse:false /p:UseSharedCompilation=false`, the components, then the presentation probe reading that DLL by
path and printing its MVID; the worktree is removed before the next. All 41 anchors were checked to occur exactly once.
The candidate DLL is the separate frozen copy `review3/head-e54009e9/`. **34/34 caught.**

| ID | Mutation | Caught by |
|---|---|---|
| P1–P32 | as listed above, rerun on `e54009e9` | each caught again (P28 by R3 case 1; P29 by R3 case 2 onward, 20 failures; P30 by R2 case 12 and R4, 17; P31 by R4 cases 9 and 10, 5; P32 by R2 case 7 and the R4 retry and capacity cases, 18) |
| P33 | an unrelated pair cleanup releases the current pair's active redirect (the old handler) | probe R5 cases 1, 2, 5, 6, 7, 8 and 10 (7 failures) |
| P34 | the owning pair's cleanup fails to release its own redirect | probe R5 cases 3 and 4, the R1 dismount and death cases and the R3 cases released by the pair's own cleanup (14 failures) |

## Evidence kinds

| Kind | What |
|---|---|
| Presentation projection | U01: the distance native is about to show, and the step promise. U02: native's selected mark on one slot. U03: copies of the unit's progression data handed to the panel. U04: a portrait badge object and native's hover setters |
| Authoritative state read | U01: the mount allocation's forced travel, grant identity and preparation, the live activation's identity. U02: the slot, its ability data, live ability facts, the relationship's owner, generation and state, native selection and its operations. U03: the shown descriptor's facts and the choice's application. U04: the relationships, their owners and the tracker filter's predicate |
| Gameplay state unchanged | cooldowns, `TimeMoved`/`TimeForced`, allocations, actions, commands, routing, targeting authority, saved slots, facts, grants, blueprints, initiative, scheduler, selection (asserted by the probe where each applies, and by IL checks that no seam calls an action, cost, command or fact member) |
| Native UI inspected from IL | `UpdateRemainingDistance` and its callers; `UpdateSlotInternal`, `IsCasting`, `SetAbility`, `DropAbility`; `GetUIProgressionLayout`, `UIProgressionLayout.BuildFeatureIds`, `GetClassProgressions`, `FactCollection.HasFact`; `InitiativeTrackerVM.HandleHoverChange`, `InitiativeTrackerUnitVM.HandleHoverChange`, `GroupCharacterPortraitController.Initialize`/`OnPointerEnter`/`HealthUpdate` and its caller |
| Production code executed offline | the four policies (components); `ProjectUsableMoveDistance` through the real native `UpdateRemainingDistance` with a real Harmony12 prefix; the targeting mapping, the real click postfix and lifecycle methods; `ProjectClassProgressions` over genuine progression data, level entries, feature collections and descriptors, and the product transpiler on the installed IL; `MountedPairPresentation` facts, badge decision and hover redirect over genuine tracker view models and units; real Harmony12 patch construction for the distance, layout, hover and portrait seams |
| Modelled UI orchestration | the native click body between the prefix and postfix (the swap, `SetAbility`'s start event and selection); native's hover pass outcome (no entry for a folded mount); the drawing branch of `UpdateSlotInternal` and the badge's Unity objects (their bodies call engine internals and cannot run detached; `UpdateSlotInternal`'s patched body cannot even be compiled detached, so its postfix binding is checked by name and type instead) |
| Substituted engine boundary | the game instance getter (its static constructor needs the engine), whether turn-based combat is on, `IsPlayerFaction`, native `GetClassProgressions` (it walks feature sources a detached unit lacks; the projection's own call to it is pinned on IL), ability-fact liveness, the selected-ability handler; live-object pointers on blank engine objects so Unity's own null test reads them as alive |
| Screen viewed | **none** |

## New independent findings

- **T09 — `Test-Finalize2UpgradeReaders.ps1` needs retained native evidence that is absent here.** Its last section
  reads `runtime-evidence\<r05 producer run>\runtime-request.json`; this lab holds no such run. It stops with
  `PathNotFound` on this branch and identically on the Sprint 6 worktree. Not a product failure and not repaired here.
- **Observation, not established as a defect:** `PairedMovementPredictionState` reports the mount's Move as Available
  from its cooldown alone during a rider-selected path preview, without the forced-travel closure. After a Charge the
  native path preview draws no line (F3 r05 c2), so no visible effect is known; it is noted for a future movement
  sprint, not changed.

## Remaining native and visual evidence limits

- No screen was viewed. Whether the zero distance, the selected mark, the class rows, the badge's size and position
  and the tracker highlight look right in the running game is not shown.
- The drawing code (`ShowTargetingSelection`, the badge object) never ran: Unity internals cannot run detached.
- `UpdateSlotInternal`'s Harmony12 patch could not be constructed detached; the other four new seams were.
- The native click, `SetAbility`/`DropAbility` event order and hover pass were modelled from IL, not run.
- U03 covers the character screen's class block only. The per-level tooltips and the console/gamepad character screen
  still read the blueprint's level entries and may still list the upgrades for a Divine Mount character.
- U04's tracker hover covers the world hover of the mount's view, the only source native routes to the tracker's hover
  handler. A party-portrait hover never reaches it (installed IL); this was not changed, as no exact portrait-to-tracker
  seam exists. Which collider the pointer picks over a mounted pair was not observed.
- The portrait seams (`UpdateCrippled`, `Initialize`) and `UpdateSlotInternal` cannot be patched detached; their
  postfix bindings are checked by name and type, and the badge lifecycle and its residues ran with the badge object's
  engine operations substituted (Unity's deferred `Object.Destroy` keeps a destroyed object alive until the end of
  its frame; a residue then stays owned until the next retry sees it gone, which was not observed in the engine).
- The R3 failures were produced through native failure paths (a throwing `HoveredUnit` subscriber, a throwing prefix on
  the row's own hover callback); which failures occur in the running game was not observed.
- Disposal is final: an obligation or residue whose cleanup still fails at disposal is left as the engine leaves it
  (hidden where deactivation worked; a tracker row is recomputed by native's next hover pass on that tracker).
- Native gameplay: NOT RUN for every package.
