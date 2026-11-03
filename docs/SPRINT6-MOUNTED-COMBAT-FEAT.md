# Sprint 6: the Mounted Combat feat (FEAT-01)

**SPRINT 6 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

## Identity

| | |
|---|---|
| Baseline | `main` `5cebdcef` (the real merge of PR #10) |
| Compiled source | `95b2945a`, candidate sprint6i. Later commits are docs only |
| DLL | SHA-256 `24167ae815aaca828019333a5151073b26c8ef0300c32cd2d4f74859c5b0db15`, MVID `c4fd7d09-25e0-43b2-99e2-413314d5355d`, 8,690,176 bytes (under the 9 MiB bound) |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint6i-diagnostic.zip`, SHA-256 `2d9062c117dec173945759505a2ef7698e5914e7b54dd183df64299b9e082358` |
| Suite | `sprint6i-95b2945a-20261103`, snapshot `runtime-state\qualification-suite-snapshots\` |
| Earlier accepted candidate | sprint6g, `83e56374`, DLL `91a9b6dfea5d7679f0c83c8cd07c5b9282a00d7f4779f19558dc0d8a72b1e455`, MVID `faf505bd-6d2a-47bd-930f-b67e825318c1`, package `db11708bcc868986f5ba8fbf33e6ee0a261db2bcb752cb62e9fdb36c2d9db99d`. Four of the native runs below belong to it |

## Rule

Official text: combat feat, prerequisite Ride 1 rank, "Once per round when your mount is hit in combat, you may attempt a Ride check (as an immediate action) to negate the hit." The selected adaptation uses Mobility. The Immediate Actions page came back truncated when fetched, so the immediate-action phases below follow the mission's statement of that rule.

## Registration and the trained-rank rule

- The feat `MountedCombatFeature` has groups `Feat` and `CombatFeat`, one rank and a stable GUID. It is registered in `HorseCompanionBlueprintService.MountedCombatFeat.cs`, beside the Riding Dog.
- It is appended, last and once, through `ExactAppendOnlyArrayLease` to every selection whose `AllFeatures` offers both Power Attack and Dodge. On the base game that is 7 selections, including `BasicFeatSelection` and `FighterFeatSelection` (r01 log). With Call of the Wild it is 21. Nothing is reordered, and the leases follow the mod's enable and disable.
- The prerequisite is the native `PrerequisiteStatValue`. For a skill it reads `BaseValue`, which is trained ranks alone. No companion is required.
- If an installed feature already offers Mounted Combat, or any registration step faults, nothing is made selectable and the failure is recorded. No conflict was seen on either mod set.
- The player preference is a native activatable ability, **Mounted Combat Reaction**, granted by the feat and on by default. It saves natively as a preference and never touches a spent use.

## Attack, skill check and negation

- **Boundary.** A narrow transpiler in `RuleAttackRoll.OnTrigger` (token `0x06007173`, exact anchor `IsSneakAttack &= IsHit`) calls the service. The result and any native parry are final there. The sneak-attack mask, the `IAttackHandler` log event and every later consumer have not yet run. A mismatched anchor rolls back the whole Harmony installation.
- **Eligibility.** It applies only to a genuine rolled hit on the mount of a `Mounted` pair. `AutoHit` has no numeric result to exceed, so it is refused, as native parry does. A hit the engine will force afterward is refused too (below). Policy lives in `Domain/MountedCombatDefensePolicy.cs` with its own tests.
- **Check.** One native `RuleSkillCheck` on Mobility, DC = attack result + 1, ignoring the difficulty adjustment. The native pass and the strict rule therefore agree, and equality fails. The complete attack result is `d20 + AttackBonus`, never the die or the AC.
- **Negation.** A greater check sets `AttackResult.Parried`, the engine's own negated hit, and clears the critical flags. A projectile, or an attack an ally Duelist already failed to parry, takes `Miss` instead: projectiles have no Parried path, and that parry's riposte and log key on Parried. No damage is healed and no log line is suppressed afterward. The battle log shows the native skill line plus one Mounted Combat line.
- **Commitment.** The use is recorded before the roll, so a failed check still spends it.

## The forced hit

The final review found that a reaction could roll, spend its use and Swift, and still have no effect. Read from the installed 2.1.7b engine:

- **One producer.** `CommandUnitCastSpell.RunCommand`, a cutscene command, is the only code setting `ForceAlwaysHit = true`. It goes `UnitUseAbility.ForceAlwaysHit` to `RuleCastSpell.Context.ForceAlwaysHit`. Ordinary casts leave it false.
- **One consumer.** `AbilityDeliverProjectile`: after `context.TriggerRule(new RuleAttackRoll(...))`, `if (context.ForceAlwaysHit) attackRoll.SetFake(AttackResult.Hit)`. So there is a real numeric roll, and the projectile hits even if it missed. It is a scripted, intentionally unavoidable hit.
- **Readable before commit.** `AbilityExecutionContext.TriggerRule` sets `rule.Reason = this` before the roll, so `rule.Reason.Context` is that context at the hook.
- **The other post-roll override is not reachable.** `CommandUnitAttack` calls `SetFake` on a `RuleAttackRoll` it never triggers, so the hook never runs for it.

**Decision: refuse, do not repair.** Forcing the hit is the scripted cast's purpose, and overturning it would mean patching the cutscene's result. The service refuses with `ForcedHit` before any state, check or Swift write. Only the context's own flag refuses: an ordinary rolled projectile stays eligible, and takes the native miss path when negated. `context.AttackRoll` reuse does not arise, since the cutscene's fresh `RuleCastSpell` context has none.

`20261103-sprint6-f02-forced` (Ray of Frost, one mount, one rider):

| Attack | Refusal or commitment | Check | Swift | Final result | Damage rules |
|---|---|---|---|---|---|
| Melee `UnitAttack`, check above | Real-time window | 36 vs 35 | 0 to 6 | Hit to Parried | 0 |
| Melee `UnitAttack`, equal | Real-time window | 39 vs 39 | 0 to 6 | Hit stands | 1 or more |
| Ordinary ray | Real-time window | 43 vs 42 | 0 to 6 | Hit to Miss, seen as Miss by the log event | 0 |
| Same ray, flag set | `ForcedHit` | none rolled | 0, unchanged | Hit stands | 1 (1 HP) |

Across the forced cast, attempts stayed 3 and native Swift charges stayed 3. The rider then still held the use and the Swift. The row triggers `RuleCastSpell` directly, as `UnitUseAbility.OnAction` does, because that command interrupts a non-cutscene cast of an ability the caster does not own (f01). So a real cutscene playing its command was not driven; everything from that rule on is the native path.

## Immediate/Swift action and round semantics

The once-per-round use and the Swift opportunity are separate.

| Phase | What happens |
|---|---|
| TB, rider's own turn | Needs that turn's Swift. Spends it now through the unit's own `UpdateCooldowns`. No debt |
| TB, off-turn | Needs no Swift cooldown check: a Swift spent last turn is history, not debt. Records the use and the debt, and charges nothing yet |
| TB, rider's next native preparation (`IUnitNewCombatRoundHandler`, after `Cooldowns.Clear`) | The borrowed Swift is charged once, and the use renews |
| TB, the turn paying a debt | No reaction: refused `SwiftUnavailable` |
| RT | Needs the native Swift cooldown free. Spends it (6 s) and holds the use for a native 6 s buff duration |
| Mode change, or leaving combat | Nothing is refunded. Owed Swift is charged and the use runs a whole native window. A window carried into TB spends the first turn's Swift |

The native command itself cannot run off-turn (6E). The Swift charge therefore goes through a never-run Swift-typed command object passed to the native writer, so KMC never writes the cooldown field. The mount pays nothing.

**Reaction restrictions.** Conscious, `CanAct`, not helpless and `CanActInCombat`, plus the native `RuleCheckTargetFlatFooted` from the attacker to the rider.

**Accepted consequence, stated for review.** A turn paying a debt starts with Swift spent, which the engine treats as "acted": native Delay is unavailable that turn, and the ground-move cursor icon does not show (`CanMove` is cursor-only). The turn still moves: the t02 and t01 rows click the ground and move three metres on it. Keeping Delay would need a read-side change to native action checks, which I did not make. It is the same native state as a turn that opened with a quickened spell.

## Persistence

No archive change and no new persistence subsystem. State is three ordinary non-dispellable rider buffs: spent, Swift owed and the running real-time window. They serialize natively with the unit, follow the rider rather than the mount, and restore before commands are admitted. The native Swift cooldown is already restored by the existing combat archive. Old saves load unaffected, because they simply lack the facts.

Removing KMC is the same class as the Horse feature: `MountedRemovalPreparation` lists the six GUIDs and reports a character holding the feat.

## Native results

The forced-hit run above is on the final DLL. The four below are retained on sprint6g's DLL ``91a9b6df…`` and were not rerun: the correction adds one refusal for a flag only a cutscene sets, and the f02 melee and projectile controls exercise the changed decision on the final bytes.

Launched with the Working fixture's retained Call of the Wild and Craft Magic Items staged beside KMC, as Sprint 5 did. Every run: `modsRestored`, `workingRestored` and save protection true.

| Run | Scenario | Result |
|---|---|---|
| `20261103-sprint6-r07-rt` | `sprint6-feat-rt` (S6-selection, S6-rt-defense, S6-rt-isolation) | PASS |
| `20261103-sprint6-t02-tb` | `sprint6-feat-tb` (S6-rider-feat, S6-tb-economy) | PASS |
| `20261103-sprint6-s03-save` | `chunk7-persistence-save`, `mounted-combat-feat-tb-debt-manual` | PASS |
| `20261103-sprint6-c01-cold` | `chunk7-persistence-load`, same case, separate process | PASS |

| Outcome | Native observation |
|---|---|
| Selection | Native `LevelUpController`. Ordinary list: 0 ranks with a +19 total refused, 1 rank accepted, committed with no companion. Fighter bonus list: committed by the rider. Toggle present and on |
| Hit defense | A hostile's `UnitAttack`, attack 25, check 26: Hit to Parried. Log event already saw Parried, no damage rule. Equal 29 vs 29 and lower 28 vs 29: hit stands, damage dealt, use spent. A natural die 17 vs 39 also stands |
| Limits and renewal | Same-round attacks refused without a roll. Positive renewal after the RT window, after the next TB preparation, and after a paid debt |
| Action debt | TB own turn: Swift 0 to 6, no debt. Quickened spell on the rider's turn, then off-turn borrow admitted. Next preparation: Swift 6, one charge. Paying-turn hit refused `SwiftUnavailable`. Mount Swift unchanged in every record |
| Isolation | Stunned refused `RiderCannotAct`. Lost Dex refused `RiderFlatFooted`. Hits on the rider and the hostile evaluated by nothing. A second rider's use unaffected by the first's, then its own. A dismount kept the same buff object and end time, and a former mount was not defended |
| Persistence | Save after a real off-turn borrow (attack 22, natural check 18). Cold load: spent and owed both still set. Next preparation: one charge, Swift 6. Then a further legal borrow |

Offline: 8 policy tests, one now covering the forced hit, and 80 reader checks, positive and negative. Candidate tier passed on `83e56374` and again on `f709989b` with the refusal in it: components, source validator, harness, paired contracts, manifest contract and assembly contracts. The only compiled change after that is the diagnostic row's cast path, `95b2945a`. The two retained rows re-evaluate PASS under the corrected reader: `analysis-cache\sprint6\`.

## Setup disclosed in the evidence

These are setup and prove nothing by themselves:

- Experience for the native levels.
- The disposable owner's Mobility ranks set to 0, because every fixture party member had some.
- A +20 modifier on the zero-rank control and on the hostile's attack. The hostile hit AC 23 only on a natural 20; its die still rolls and decides.
- The mount healed between measured hits.
- Forced check dice in the S6 rows, from a seam the rows set and ordinary play leaves null.
- The persistence case adds the feat directly (the native choice is the S6 row's) and strikes with a native attack roll, since it needs only the committed state.

Some plain `RuleAttackWithWeapon` strikes reach an exact phase: the rows have a real `UnitAttack` in both modes and the engine's own attack of opportunity in RT, recorded per rule.

## Executed versus not executed

- **Not exercised natively:** on-screen level-up and action-bar clicks (the native controller and the toggle's `IsOn` were driven directly); a projectile or touch negation; a failed Duelist parry; dispel; cutscene; mode change mid-combat; leaving combat with state held; switching to a different mount (the state is on the rider by construction, and a dismount and remount kept it); a critical hit negated natively.
- **Offline only:** mode-change and combat-end transitions (policy tests).
- **Natural die:** a real die never happened to beat the attack in these runs. Native negations used the forced die; the natural attempts failed and agreed with the strict rule.
- **Base-game-only:** r01 proved registration and showed no conflict, but the Working fixture cannot load without its mods, so combat rows ran with them.
- **Full attacks (correcting the first report):** the hostile's `UnitAttack` routines do carry a second attack. r07 and f02 show it refused `UsageSpent` while the routine continued (for example r07 step 4), so this was exercised, though I had listed it as not.
- **Neighbors:** existing mounted rows were not rerun beyond these and the Candidate tier, as the mission selected.

## Failures retained

| Run | Why | Change |
|---|---|---|
| r01 | Working fixture needs its mods; `Player.PostLoad` `Single` | Staged the retained dependencies |
| r02 | Every party member had Mobility ranks | Zero-rank setup. Batch guard then refused a third attempt, so a new declared batch |
| r04 | Selection passed. Hostile hit AC 23 only on a 20; leaf deadline | Hostile attack setup |
| r05 | Second pair mounted before the first pair's AI isolation | Reordered |
| r06 | Game PASS. Reader required a third party member | Reader-only fix. Original FAIL kept, re-evaluated PASS on the same evidence: `analysis-cache\sprint6\r06-rt-reader-reevaluation.json` |
| s01 | Case lacked the experience setup | Added |
| s02 | The two-pair AI lease refused an empty candidate set; game left running, restoration blocked | Lease accepts "nothing to lease" only when other declared pairs account for the party. Reviewed recovery run, verified |
| f01 | Melee controls passed. The hostile's command cast was interrupted before rolling (`UnitUseAbility.Init`) | Row triggers the cast rule directly |

The independent review of `2aef21e2` found six items, all handled: two reader defects (mount Swift, cold read), leaving combat refunding state, a failed parry credited as success, dispellable state, and the Delay consequence above.

## Costs and restoration

Budget `sprint6`: **74.43 of 90** minutes across 32 entries. Batches `sprint6:batch1`, `sprint6:feat-2` to `feat-4`, `sprint6:forced-1`. Package and suite are imported entries; settlement and restoration are not charged.

At closeout, after f02: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction lock or game process; no candidate installed. In s02 I stopped that run's own game process (PID verified against its orchestration record), then ran the reviewed transaction and profile recovery.

## Owner test

1. Install the Sprint 6 build. On a character with no Mobility rank and no companion, level up: the feat shows as unavailable. Put one rank in Mobility in that same level: it can be chosen.
2. Check a fighter level offers it as a bonus feat.
3. Mount up, get hit in real time. The battle log shows a Mobility line and a Mounted Combat line, and the mount takes no damage on a success. A "Reaction Used" buff shows for six seconds.
4. In turn-based, let a hit land off-turn. "Swift Action Owed" shows. On your next turn Swift is spent and the buff is gone. Try to Delay, and try a Swift spell, on that turn.
5. Save with "Swift Action Owed" showing, quit, reload: it is still there.
6. Turn the **Mounted Combat Reaction** toggle off from the abilities list: hits stand with no roll. Toggling never restores a used reaction.
