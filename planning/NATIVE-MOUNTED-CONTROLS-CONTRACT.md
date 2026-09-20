# Native mounted controls contract

Status: `IN PROGRESS - PHASE 3D NATIVE-SHELL REPAIR OFFLINE-GREEN; FRESH RUNTIME REQUIRED`

Date: 2026-08-31

Branch: `codex/mounted-combat-phase3d-unified-combat`

## Accepted input

The immutable input is Horse stabilization HEAD `8ff5813b36eb1af04e1329a1993b2476ae6ad691`, version `0.1.0-phase3b-dev.27`, package SHA-256 `2b6629338b35d9f01fab607201fd999e6fad97bc63d73e42860c08c25c3870b7`. Human review confirms that target-selected Mount and the RT Rider/Horse attack implementations work, while ordinary TB clicks from the IMGUI overlay do not enter the accepted KMC target-command chain.

## Exact Kingmaker 2.1.7b contracts

The inspected `Assembly-CSharp.dll` remains SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

| Contract | Exact member | Consequence |
|---|---|---|
| native ability definition | `BlueprintAbility` type `0x020005FE`; fact/collection methods `0x06002CF1` / `0x06002CF2` | a runtime-owned `BlueprintAbility` becomes a normal `Ability` in `UnitDescriptor.Abilities` |
| availability and target predicates | `AbilityData.IsAvailable` `0x06002B48`; `CanTarget` `0x06002B63`; `GetUnavailableReason` `0x06002B66`; `IsVisible` `0x06002B69` | KMC can provide native disabled state, tooltip reason, visibility, and target highlighting through stock interfaces |
| native pointer pipeline | `ClickWithSelectedAbilityHandler.SetAbility` `0x060093F8`; `GetPriority` `0x060093F4`; `GetTarget` `0x060093F5`; `OnClick` `0x060093F6`; `DropAbility` `0x060093F9` | toolbar/drawer activation enters the stock ability cursor and physical unit-click path |
| native command | `UnitUseAbility.CreateCastCommand` `0x06002725`; `Init` `0x06002728`; `OnAction` `0x06002737` | the player click creates a stock ability command before KMC delegates to its already-qualified mounted command |
| action-bar refresh | `ActionBarManager.HandleAbilityAdded` / `HandleAbilityRemoved` `0x060044DD` / `0x060044DE` | direct ability facts refresh the ordinary selected-unit ability surface |
| TB disabled state | `MechanicActionBarSlotAbility.CanUseIfTurnBased` `0x06002F6D`; `WarningMessage` `0x06002F6B` | exact current-turn and action-state rules remain native, supplemented by KMC availability reasons |
| hotbar policy | `UnitUISettings.TryToInitialize` `0x06003005`; `SetSlotAutomatically` `0x06003001`; `SetSlotInternal` `0x06003002` | `ActionBarAutoFillIgnored=true` keeps KMC out of serialized user slots; abilities remain in the native abilities drawer for optional drag |
| fact lifecycle | `AbilityCollection.GetAbility` `0x06002B0D`; inherited `AddFact` `0x060096AE`; `RemoveFact` `0x06009699` | KMC can own exact reference leases, prevent duplicates, and remove only its facts |
| serialization boundary | `SaveManager.SaveRoutine` `0x06008029` | a guarded coroutine scope can suspend/remove runtime control facts before `Player.PreSave` and restore them only after the save routine exits |

## Phase 3D native-shell admission addendum

The immutable dev.3 RT run `20260831T062000Z-phase3d-dev3-rt-passA` proved that one exact Rider Primary physical target click reached the selected-ability handler and created one native cast request, but no `DispatchStarted` event or KMC child command followed before the 30-second leaf deadline. The relationship remained `Mounted`; target cancel and friendly-target rejection also retained it. This failed run is diagnostic evidence only and earns no gameplay credit.

Exact installed control flow adds two contracts:

| Contract | Exact member | Phase 3D consequence |
|---|---|---|
| RT command-start admission | `UnitActionController.ShouldStartCommand` `0x0600911F` | a runtime click is not attempted until the rider and Horse are prepared, `State.CanAct` and `CanActInCombat` are true, initiative is ready, command containers and hands are idle, equipment updates are idle, the exact action actor has Standard, and KMC has no active child/ground/stock transaction |
| native ability-shell approach and cost | `UnitUseAbility.Init` `0x06002728`; `UnitCommand.set_NeedLoS` `0x0600276D`; `UnitCommand.IgnoreCooldown` `0x060027BA`; `UnitActionController.UpdateCooldowns` `0x06009120` | stock initializes the KMC intent shell with LoS approach semantics, and RTWP maps an acted `Free` shell to rider Move cooldown; an exact-token postfix sets `NeedLoS=false` and `IgnoreCooldown=true` only for reference-identical KMC Rider Primary or Mount Primary cast by the exact active rider while the exact relationship is mounted |

This postfix changes only the Free native intent shell. `IgnoreCooldown` makes that shell resource-neutral; it neither grants nor refreshes an actor resource. This is required because exact RTWP `UpdateCooldowns` otherwise treats an acted `Free` command as Move, while TB already charges no resource for `Free`. The postfix does not dispatch directly, alter the child command, rewrite attack LoS/range, suppress a rule, or affect Mount Companion, Dismount, foreign abilities, unmounted actors, or the wrong caster. The existing mounted child transaction continues to give approach movement to the mount and uses native rider/mount `UnitAttack` range, LoS, weapon, and sole Standard-resource ownership.

Mount Companion and Dismount are intentionally different: their exact native shells are `Move`, `IgnoreCooldown=false`, and are the sole rider Move-cost owners in combat. Because native custom delivery can run after the admitted shell becomes acted and `UpdateCooldowns` commits the resource, the delivery callback marks only that exact shell as already admitted. Its second evaluator pass skips only the now-stale Move-availability predicate; every identity, target, turn, adjacency, body, ownership, lifecycle, control, view, and mode predicate remains active. This prevents a successfully admitted Move shell from rejecting its own delivery without weakening pre-input availability or editing cooldowns.

Runtime evidence records the two stable admission frames, every command/action/equipment gate, exact shell slot and executor, `NeedLoS=false`, `IgnoreCooldown=true`, approach state, cooldown state, one preparation delta, native selection/cast cardinality, terminal KMC outcome, and relationship state. The external validator fails closed if admission was premature or if the exact shell preparation was not observed.

## Selected surface

Four original KMC runtime blueprints are reserved, with fail-closed collision checks:

| Ability | GUID | Target | Native command type |
|---|---|---|---|
| `KMC_MountCompanionAbility` | `f053faad986631688defa003cd7bda0e` | exact owned supported companion | Move; native rider cost in combat, free exploration transition |
| `KMC_DismountAbility` | `3af2b81f4d72bbb30501fa730fcdf36e` | owner | Move; native rider cost in combat, exact cleanup path |
| `KMC_RiderPrimaryAbility` | `27364df661b3c121eabb97a31aa73a83` | one visible hostile unit | Free native handoff; rider KMC command owns Standard |
| `KMC_MountPrimaryAbility` | `f88a50d6fdbebbd709c3e323d2f52f5e` | one visible hostile unit | Free native handoff; Horse/Mammoth KMC command owns Standard |

The Free native handoff is deliberate: the existing KMC attack wrapper, not the UI activation shell, remains the reference-identical Standard-action owner. Availability refuses the ability unless the correct action actor owns the current RT/TB opportunity and has Standard available. No second Standard cost is introduced.

Both primary abilities are present on both pair members while mounted. On the rider turn the rider ability is enabled and the mount ability explains the wrong-turn boundary. On the mount turn the inverse applies. In RT the rider remains the visible selected unit and may request either native primary; the exact attack actor still owns its command and ledger.

## Persistence and hotbar policy

- No KMC control is automatically written into a user action-bar slot.
- No nonempty slot is overwritten and no serialized slot binding is removed.
- Ability facts are runtime leases. They are removed on disable, stale-unit replacement, respec/pet change, and process disposal.
- Save serialization suspends and removes every exact KMC control fact for the entire `SaveRoutine` enumeration, then rebuilds the current runtime surface afterward. Load reconstructs the surface from current owner/pet/relationship state.
- The Horse companion blueprint itself remains persistent content under the existing respec-before-uninstall policy; the new control facts and mounted relationship do not add persistent state.

## Feedback and telemetry

Native activation records blueprint identity, caster, active turn, Standard availability, pointer selection start/end, hover/target admission, physical click, native command, KMC command acceptance, and terminal result. Wrong turn, spent Standard, invalid target, unsupported weapon, lifecycle boundary, and active-command conflicts must produce a native disabled reason, warning, and/or precise KMC feedback. A click that never reaches this pipeline is not credited as human-input proof.

## Overlay disposition

The IMGUI overlay becomes default-hidden. A separate UMM diagnostic toggle may instantiate it as an emergency fallback. Guarded runtime scenarios may explicitly request it; ordinary play does not depend on it.

## Final qualification - 2026-08-30

Production implementation `e951fb5394ff4f8e791dd27f49b75d71d76a8b1f` and final diagnostic/package checkpoint `42debbb814823dbdcd3a39cdc4353a5c3ee3d12d` close the technical contract as version `0.1.0-phase3c-dev.13`.

Audited Horse aggregate `20260830T074300Z-phase3c-dev12-native-controls-passG` passed `66/0`. The actual Kingmaker selected-ability handler admitted exact Horse Mount, Dismount, TB Rider Primary, TB Horse Primary, RT Rider Primary, and RT Horse Primary clicks. Exact actor/command/resource ownership, Standard cost, one-chain cardinality, target selection, terminal success, control-fact save suspension, zero hotbar mutation, default-hidden overlay policy, and cleanup all passed. The final dev.13 diff is restricted to the Mammoth diagnostic engine, so qualification economy did not repeat this unaffected Horse aggregate.

Final same-package Mammoth TB regression `20260830T122900Z-phase3c-dev13-mammoth-tb-passI` passed `67/0`. It proves the policy-required Mammoth selection, native target click, Mammoth turn/command/resource ownership, exact PrimaryHand natural weapon, one attack/roll/damage chain, zero duplicate/repath, unchanged Mammoth profile, and exact cleanup.

No KMC fact is auto-filled into a serialized user slot. The ability drawer is the supported discovery surface; optional player drag to an empty slot is allowed. Physical pointer feel, toolbar discoverability, disabled-tooltip presentation, and ordinary human RT/TB usability remain manual gates.
