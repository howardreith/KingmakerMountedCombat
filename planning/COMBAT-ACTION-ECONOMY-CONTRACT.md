# Combat action economy contract

Status: IN PROGRESS

Authority: the user accepted exact presentation commit `09a63729e0847c540ae7e79e9e3876d005ee9afe` and opened Tranches C-F of `planning/MOUNTED-COMBAT-PHASE-2-MASTER-MISSION.md`. This contract is scoped to one transient Medium-humanoid/`AnimalCompanionUnitMammoth` pair and ordinary melee only.

## Exact Kingmaker evidence

The authority assembly is Kingmaker `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

| Concern | Exact local contract | Consequence |
|---|---|---|
| Attack construction | `UnitAttack(UnitEntityData)` token `0x06002678`; `IsSingleAttack` accessors `0x0600265C/5D`; `Init` `0x06002679`; `LastAttackRule` `0x06002666` | A single stock attack preserves native animation/rule execution. `IsSingleAttack=true` prevents iterative, off-hand, and additional-limb expansion. |
| Attack identity | `UnitAttack.TriggerAttackRule` token `0x06002686` creates `RuleAttackWithWeapon` using `Executor`, `Target`, and the planned weapon. Rule constructor token `0x0600719C` | The child attack executor, not the UI or pair coordinator, is the combat-log/rule initiator. |
| Command ownership | `UnitCommands.Run` token `0x060026B2`; `UnitActionController` ticks commands only for their executor. In turn-based mode a command other than `UnitAttackOfOpportunity` advances only when its executor is `CurrentTurn.Unit`. | A Mammoth command placed on the Mammoth queue cannot execute during the rider turn and would charge the wrong cooldown. |
| Real-time start readiness | `UnitActionController.ShouldStartCommand` token `0x0600911F` waits for idle hands, no scheduled hand-equipment update, `State.CanAct`, `UnitCombatState.CanActInCombat` token `0x0600938F`, and no command cooldown. `UnitEntityData.AreHandsBusyWithAnimation` is token `0x06008330`; `UnitHandEquipmentController.IsUpdateScheduledFor` is `0x06009154`; initiative cooldown getter is `0x0600C3B4`. | A command issued during Kingmaker's combat-entry auto-pause must remain admitted and wait. Real-time qualification explicitly leases pause state, unpauses after combat entry, waits for every stock start gate, and restores the exact prior pause state. Production does not bypass hands, equipment, initiative, or cooldown. |
| Diagnostic combat entry | `Game.UnitMemoryController` field `0x040006BE`; `UnitMemoryController.AddToMemory` `0x0600916F`; public `UnitGroupMemory.Contains(UnitEntityData)` `0x06001F31`; `UnitCombatPrepareController.Tick` `0x0600936F`; `UnitCombatCooldownsController.TickOnUnit` `0x0600934A`; `UnitCombatLeaveController.TickGroup` `0x06009368`. | The runtime-only target must be visible and queued into exact bidirectional native memory before stock group combat entry. Qualification waits for memory, group combat, initiative preparation, an awake rider, Default mode, and positive game time. Cleanup removes only the project-owned pair memory before destroying the target and proves no memory residue. KMC never calls `JoinCombat` or edits initiative/cooldown state for the row. |
| Mammoth-origin native admission | `UnitCommand.IsUnitEnoughClose` getter `0x06002784` compares the command executor position to `ApproachPoint`; `GeometryUtils.MechanicsDistance` is `0x06001C68`. Probe K proves that setting only the Mammoth-derived stopping radius leaves the offset rider executor outside stock admission even when the Mammoth is in range. | The exact Mammoth-origin range gate remains authoritative. Only after that gate passes may the scoped child radius bridge the measured rider-executor offset by `0.001`, capped at `0.75`; outside-pair range returns zero native admission. The bridge is re-applied through the existing exact child-only `UnitAttack.GetApproachRadius` postfix so stock `Start` and `UpdateTarget` agree. Delegated pathfinding continues to use the unexpanded Mammoth stopping radius. |
| Cooldown charging | `UnitEntityData.UpdateCooldowns` token `0x0600838F` adds six seconds for Standard and three seconds for Move; `UnitActionController.UpdateCooldowns` charges the command executor when it first acts. | The pair must expose one rider-executed Standard wrapper. The actual actor's child attack is manually driven with `IgnoreCooldown` token `0x060027BA`; otherwise the pair receives two costs. |
| Turn accounting | `CombatController.CurrentTurn` `0x06000BBE`, `StartTurn` `0x06000BDA`, and `CurrentUnit` `0x06000BFA`; `TurnController.ForceToEnd` `0x06000C47` | Rider is the pair initiative principal. An exact active Mammoth native turn is immediately ended with normal cooldown finalization, so it never supplies a second usable turn. |
| Movement accounting | `UnitMovementAgent.CanMoveInTurnBased(ref float)` token `0x060018A9` calls `CurrentTurn.TickMovement`; stock admission requires the moving unit to be current. | One exact scoped prefix may admit only the active Mammoth while the active rider owns the current turn. The stock `TickMovement` call remains the source of rider movement expenditure. |
| Movement command | `UnitMoveTo(Vector3,float)` is already `IgnoreCooldown`; `UnitCommand.TickApproaching` otherwise moves its executor. | A pair coordinator manually drives one Mammoth `UnitMoveTo`. The rider stock agent remains disabled and never paths. |
| Cancellation | `UnitCommand.Interrupt` `0x060027AC`; `UnitCommands.InterruptAll` `0x060026C1`; `InterruptMove` `0x060026C2` | Cancelling the rider wrapper interrupts its child attack, interrupts the delegated Mammoth move, and stops the Mammoth agent. |
| Native attack availability | `HasStandardAction` `0x0600837E`, `HasMoveAction` `0x0600837F`, and `HasCooldownForCommand` enforce current resource state. | KMC validates the rider's resource before accepting a pair command. It never edits cooldowns to create availability. |
| AoO | `UnitCombatState.AttackOfOpportunity` `0x060093A1` owns a per-unit count and creates `UnitAttackOfOpportunity`; `ShouldAttackOnDisengage` is `0x060093A2`. | Rider and Mammoth have independent stock engagement/AoO state. Combining it safely is stretch work; core does not synthesize or duplicate AoOs. |

## Bounded Wrath reference

The exact installed Wrath reference assembly remains read-only. Its `SaddledUnitController.TickDelegateRiderToMount` links a rider command to a mount command, its `UnitCommand.RiderCommand` propagates interruption, and its turn-based controller treats a rider and mount as one turn while excluding a saddled mount from the ordinary next-unit choice. Those responsibilities are evidence for the required behavior only. Kingmaker has none of those types or members, and no Wrath code, assembly, unit part, asset, or serialization model is imported.

## Selected pair policy

| Resource/behavior | Private-alpha owner and rule |
|---|---|
| Initiative and turn | Rider. Mammoth's separately scheduled TB turn is exact-pair suppressed by immediate native `ForceToEnd`. |
| Standard action | Rider. One rider or Mammoth single attack consumes exactly one rider Standard action. |
| Move actions | Rider. Mammoth moves as the sole pathfinding authority; TB distance/time is charged through the rider's current `TurnController`. |
| Swift action | Rider stock state, untouched. KMC exposes no mounted swift action. |
| Full-round/full attack | Unsupported while mounted in this alpha. Every KMC attack sets `IsSingleAttack=true`; no automatic rider-plus-mount sequence exists. |
| Five-foot step | No custom mounted five-foot-step surface. Movement-to-attack uses the current native rider movement state; KMC never grants a step or AoO immunity. A native state that cannot safely admit the Mammoth is rejected. |
| Rider attack | One rider Standard wrapper manually drives one rider-owned stock single attack. |
| Mammoth attack | One rider Standard wrapper manually drives one Mammoth-owned stock single attack with ignored Mammoth cooldown. |
| AI | Mammoth companion AI receives a scoped effective-state lease while mounted. Any native Mammoth TB turn is ended before action. The lease restores exactly on dismount. |
| Turn end | Stock rider turn completion. An active wrapper keeps the rider command slot occupied; completion or interruption releases it. |
| Cancellation/interruption | One transaction owns wrapper, delegated move, and child attack. Any invalid target, death, unconsciousness, dismount, combat/mode boundary, new pair command, or manual cancellation interrupts all owned representations once. |

## Invariants and rejection rules

- The pair has one and only one actionable turn and one resource ledger.
- The Mammoth remains the only stock pathfinding agent. The rider agent is never enabled to approach.
- The attack wrapper is inert unless its executor is the exact active rider and its actor is that rider or exact active Mammoth.
- A child attack is stock `UnitAttack`, single only, and ignored for child cooldown. Its exact initiator/target/weapon/rule identities are recorded.
- A command cannot contain both rider and Mammoth child attacks.
- Ranged weapons, auto-use abilities, spells, full attacks, combined attacks, and unsupported profiles are rejected with visible feedback.
- Every scoped AI, command, movement, and turn lease restores on cleanup. Non-pair commands and non-mounted behavior remain stock.

Qualification remains `IN PROGRESS` until deterministic tests plus two fresh-process real-time and turn-based passes prove exact resource deltas, turn identity, one attack roll, at most one damage event, cancellation, AI suppression, and non-mounted isolation.
