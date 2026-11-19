# Known issues remediation, Sprint 7 — mounted presentation: post-Charge distance, targeting slot, bond rows, badge and tracker hover (2026-11-19)

Sprint 7 of the Mounted Combat Known Issues Remediation Epic: U01–U04. A presentation sprint. Offline engineering
only: nothing was launched, staged or installed, no screen was viewed, and nothing here is native gameplay or visual
evidence. Sprint 8 was not started.

## Identity

| Item | Value |
|---|---|
| Base | `f835c503515fcc80ff1033bf50f4f771e34de15c`, the accepted Sprint 6 checkpoint. PR #51 is unmerged; the base was verified as the live head of `codex/kmc-known-issues-sprint6` (the parent has not advanced). Accepted Sprint 6 product build `7898b1a4` |
| Branch and worktree | `codex/kmc-known-issues-sprint7`, `worktrees/known-issues-sprint7` |
| Draft PR target and parent | `codex/kmc-known-issues-sprint6` (parent PR #51) |
| Commits | `ee5c20dc`: U01–U04, the Domain policies, component tests and the presentation contract script (the product-build commit). Then the records commit carrying this document and the strengthened presentation probe (script only; the product bytes are unchanged), which is the branch head |
| Product-build commit | `ee5c20dc` |
| Product DLL (clean Release rebuild of `ee5c20dc`) | SHA-256 `16b446fd5590990f3fec918f0705b31650199f0275de421f3cf9ab005b9f7a7f`, MVID `3b7cc9f0-9d90-42b7-907c-75a10a461855`; frozen copy `artifacts/known-issues-sprint7/candidate/head-ee5c20dc/`. The Fast tier's own build is byte-identical |
| Base DLL used for before-evidence | Sprint 6 `7898b1a4`: SHA-256 `7da6d2c006ff3d71b9d51e29db729a514c6beda826f4611768c03347581dbc64`, MVID `19fba84e-efe2-4a0f-a82c-da26f79e60a4` (frozen at `artifacts/known-issues-sprint6/review4/head-7898b1a4/`, hash re-verified) |
| Workflow budget | `known-issues-sprint7` |

## Dispositions

| ID | Disposition | What changed |
|---|---|---|
| U01 | **Implemented and verified offline** | The mount's Move and Standard prediction actions show zero usable distance, and the rider-selected fill promises no five-foot step, once the mount's allocation for the exact live activation has recorded forced (charge) travel. Native Move unavailability, cooldowns, `TimeMoved`, the allocation and the path are unchanged |
| U02 | **Implemented and verified offline** | The exact public slot whose click handed native target selection to a hidden delivery ability is drawn with native's own selected mark, through an exact value-identity mapping that ends with the selection operation |
| U03 | **Implemented and verified offline** | The Paladin class panel builds its rows from projected copies of the shown character's progression data: a Divine Mount character, or one whose bond is not yet chosen, is not promised the weapon-bond-only upgrades. Grants, blueprints and the unit's data are unchanged |
| U04 | **Implemented and verified offline** | A mounted rider's party portrait carries the project's saddle badge, derived each refresh from the live relationship. Hovering a mount whose tracker entry is folded into its pair's paired activation highlights its own rider's existing entry |
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

- **Portraits:** `GroupCharacterPortraitController` (one per party portrait) is refreshed each frame through
  `HealthUpdate` (0x06003F01, from `CharacterBase.Update`). The mod had no portrait seam.
- **Tracker:** `InitiativeTrackerVM.UpdateUnits` runs inside the mod's projection scope; `FilterTrackerSortedUnits`
  omits a mount whose entry is folded into its pair's live paired activation (`HidesPairedTrackerEntry`, or the legacy
  `ShouldSuppressMount`). A mount keeps its own entry when paired activation is off, after a dismount split from the
  release round, after its rider is removed, or while it is native `CurrentTurn.Unit`.
- **Hover:** native `InitiativeTrackerVM.HandleHoverChange(UnitEntityView, bool)` (0x06004F16, the
  `IUnitDirectHoverUIHandler` the world and portrait hovers raise) highlights the entry whose unit's view is the hovered
  view. A folded mount has none, so nothing highlights.
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
  portrait's unit. A postfix on `HealthUpdate` shows or hides a child image under the portrait art using the embedded
  saddle icon. The image never takes a pointer event, so portrait clicks, hover and selection stay native. No tooltip
  was added (one would need the badge to receive pointer events, which would take them from the portrait).
- **Hover:** after native's hover pass, a postfix asks `FoldedMountHoverEntry`: only when native found no entry of the
  hovered unit's own, exactly one live mounted relationship folds that mount, and exactly one entry is its rider's,
  native's own `HoveredUnit` and the entry's `HandleHoverChange(true)` are set. Native's un-hover clears both.

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
- **U04:** the presentation keeps the world service, the sprite source and the logger, and the badge objects only as
  `WeakReference`s, bounded at 32. Badge objects are children of the portrait art and die with it; disable and disposal
  destroy them. No tracker entry, view, unit or pair is held.
- **Statics:** the patch bridge's `Presentation` reference is set and cleared with the patch controller, as every
  other bridge member. The once-per-seam fault set holds only the fixed seam names.
- **Checks:** the U02 GC check (a dropped slot is collected), and structural field checks on both new state holders.

## Validation (budget `known-issues-sprint7`)

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

## Mutation campaign P1–P20 (serial; log `candidate/03-mutation-campaign.log`)

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
- U04's tracker hover covers the hovers native routes through `IUnitDirectHoverUIHandler`; whether a party-portrait
  hover of the mount raises it was not observed.
- Native gameplay: NOT RUN for every package.
