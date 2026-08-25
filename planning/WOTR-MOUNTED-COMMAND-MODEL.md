# Exact Wrath mounted command and turn model

Status: PASS — assembly-backed recommendation; no Wrath runtime launch

Date: 2026-08-25

## Evidence identity and restrictions

Authority is the exact installed read-only Wrath `Assembly-CSharp.dll`, SHA-256 `2cb7160b7154d4ffacc77b9c51b1eb26199e1294300f04fdfc073367b2ef8953`, MVID `90a9869c-2792-4c7b-bfb7-5a8b33da7c82`. Evidence came from bounded type decompilation and reflection-only metadata inspection under ignored `analysis-cache/wrath-bounded`. Wrath was not launched or modified, and no Wrath source or asset is committed or shipped.

## Relationship and initiative

`UnitPartRider` and `UnitPartSaddled` store reciprocal relationship references. `CombatController.SortedUnits` may contain both actors, but `ChooseNextUnit` (`0x06000E88`) skips a unit whose `GetRider()` is non-null. `UnitCombatState.IsWaitingInitiative` likewise returns false for a saddled mount. The mount therefore has no separately chosen turn-order entry.

`CombatController.StartTurn` (`0x06000E91`) creates one `TurnController` for the rider. `TurnController.Start` (`0x06000F08`) records `Rider` and the rider's current `Mount`. This is a literally shared controller/turn, not merely a UI illusion.

## Separate ledgers inside one paired turn

The shared `TurnController` retains separate command sets, combat states, cooldowns, action states, movement statistics, and attack modes for Rider and Mount. `IsAllActed` (`0x06000F22`) requires the rider and, when present, the mount to be acted. Start/reset/end operations prepare or cap both ledgers. `HandleUnitCommandDidStart` and `HandleUnitCommandDidEnd` (`0x06000F33`/`0x06000F34`) accept commands from either actor and account against that actor.

Conclusion: Wrath coordinates two resource owners inside one turn controller. It does not collapse them into one undifferentiated movement/action ledger.

## Player command routing

`SaddledUnitController.TickDelegateRiderToMount` (`0x0600AB06`) inspects the rider's unacted non-AoO command and may create a mount `UnitMoveTo`, continuous movement, or `UnitAttack`. It copies player/AI and line-of-sight context, links both commands through `UnitCommand.AddMountCommand` and `AddRiderCommand` (`0x0600CA6C`/`0x0600CA6B`), and runs the linked mount command on the mount.

`UnitCommand.TickApproaching` (`0x0600CA7E`) does not path the rider when the executor has a rider part; the linked mount owns physical approach. A missing required mount path interrupts the paired rider command in turn-based mode.

`TickDelegateMountToRider` (`0x0600AB05`) handles an unpaired, player-originated mount command. When it targets an enemy, the stock click command factory creates and links a rider command; charge intent can propagate. This means a visible enemy-target command may drive both actor commands when each is eligible. It is not evidence that every request is a single attack by a single actor.

`UnitCommands.FixTargetIfTargetOnMount` (`0x0600C935`) redirects hostile attacks/abilities aimed at a rider to the rider's mount. Paired Standard/Move command interruption and `UnitCommand.Interrupt` (`0x0600CA86`) propagate cancellation through the explicit links.

## Full attacks and action prediction

The controller retains `m_AttackMode` and `m_MountAttackMode`. Smart prediction simulates the selected command, force-ticks mounted delegation, then records rider and mount action use separately. Approach can reduce a full attack to the appropriate single-attack form. The command surface is unified, but attack execution and accounting remain actor-specific.

## Selection, portrait, action bar, camera, and AI

`TurnController.SelectedUnit` can be rider or mount within the paired turn; direct-control selection prefers the rider and may switch to the mount. `UnitEntityData.IsCurrentUnit` (`0x0600A123`) recognizes both members of the active pair.

`ActionBarVM.UpdateSelection` (`0x06005ADD`) binds its selected unit to the exact single directly controllable current selection and rebuilds slots, abilities, items, and weapon state from that unit. Party portrait view models bind portrait data to their exact unit. Therefore one turn controller does not imply one immutable actor action bar: the presented bar follows the currently selected pair member.

Turn start releases the prior camera follower and scrolls to the rider; command starts may scroll to the command executor. The unit-move controller follows the current pair member under the current-turn predicate.

No exact evidence shows a global "disable mount AI" switch. The action bar AI toggle remains per selected unit. Mounted command coordination instead occurs in `SaddledUnitController`; it distinguishes AI-originated commands, requires direct controllability for player delegation, and reconciles conflicting paired commands. This is a controller boundary, not proof that the mount brain ceases to exist.

## RT/TB transitions

Relationship parts are independent of the mode switch. `CombatController.Disable` converts the ordered turn state to real-time initiative cooldowns, restores navmesh participation, resets the turn controller, and can pause when combat remains active. Combat end clears/cancels party and pets and restores selection/camera state. These responsibilities cross initiative, navmesh, selection, camera, and commands.

## Recommendation for Kingmaker

Do not port or imitate the whole Wrath controller during horse asset/companion work. The exact model spans relationship serialization, initiative filtering, two action ledgers, prediction, selection, action bar, camera, navmesh, AI arbitration, command pairing, and cancellation. Kingmaker lacks the native rider/saddled parts and paired-command fields.

Retain the accepted Phase 2 separate-turn/overlay model as the horse-alpha fallback. Later, stage a dedicated generic control-model tranche that can prove a pair-local coordinated-turn seam with:

1. one visible rider-led turn selection;
2. separately accounted rider and mount actions;
3. one physical movement owner;
4. explicit paired command identities and symmetric interruption;
5. selection/action-bar switching without duplicate initiative;
6. deterministic RT/TB conversion; and
7. Mammoth and horse A/B regression.

Until that full contract is independently qualified, a broad turn rewrite would risk the already accepted private-alpha behavior and is not justified by the horse tranche.

