# Known issues remediation, Sprint 5 — mounted ranged movement (R01, R02), 2026-11-17

Sprint 5 of the Mounted Combat Known Issues Remediation Epic. Offline engineering only: nothing was launched, staged or
installed, and nothing here is native gameplay evidence. Sprint 6 was not started.

## Identity

| Item | Value |
|---|---|
| Base | `af5b2bc7bb0881706c9236411367364f973c715c`, the accepted Sprint 3 closeout head (draft PR #49, unmerged), stacked on the accepted Sprint 4 branch. Verified as the live head of `codex/kmc-known-issues-sprint3-closeout` before branching; the parent had not advanced |
| Branch and worktree | `codex/kmc-known-issues-sprint5`, `worktrees/known-issues-sprint5` |
| Draft PR target and parent | `codex/kmc-known-issues-sprint3-closeout` (parent PR #49), so the PR shows only Sprint 5's delta |
| Commits | `95fd7120`: R01 reconciliation tests and the focused contract. Then the records commit carrying this file, which is the branch head |
| Product-build commit | `cdfbe509`, the accepted closeout product. **Sprint 5 changes no product source**: the clean Release build of the head is byte-identical to it |
| Product DLL (clean Release build of the head) | SHA-256 `f584c1c6e0678a9447a6082552dda57c5fcc4a95e475430c20fb5a93210f5035`, MVID `6cb7b950-e97c-4c14-9d83-764b074a3439`; frozen copy `artifacts/known-issues-sprint5/head/` |
| Budget | `known-issues-sprint5` |
| Preserved | PRs #45–#49, their branches, the main checkout and every other worktree are untouched |

## Dispositions

| ID | Disposition | Summary |
|---|---|---|
| R01 | **Closed: not reproducible at the accepted base; no reachable instance; no product change** | The epic expected a rider's ranged attacks to miss travel the mount made on its own separate initiative slot while carrying the rider. Under the qualified production authority the mount never moves on its own slot while carrying its rider (chain below). A mount's own native allocation (`native:` grant) only ever holds riderless or pre-mount travel, which the accepted rule must and does exclude. No separate-initiative authority was built: accepting those records is exactly the riderless-travel defect the 2026-11-13 review corrected (mutations S1 and S2 below). |
| R02 | **Intentional adaptation documented** | The accepted rule is reconciled from source and stated exactly below, in the Mounted Archery handoff's Sprint 5 section and, where the player text was ambiguous, in the README (five-foot steps and Charge do not count). The feat name, description, prerequisites, GUID, blueprint, rank, publication, localisation and icon are unchanged. |

No new independent defect was found. One documentation inaccuracy is corrected under R02: the 0.2.9 release notes and
the 2026-11-13 handoff describe "rider and mount on separate initiative turns" as a live arrangement that gets no
penalty. In play under the qualified authority that arrangement does not occur while mounted. The historical text is
not rewritten; this record and the handoff's superseding section state the current fact.

## Reconciled behaviour at the accepted base

Read from the accepted source (`af5b2bc7`) before any edit. The 2026-11-13 handoff matches the source, except for the
separate-initiative statement above.

| Aspect | Current implementation |
|---|---|
| Turn authority | Production settings are fixed: paired activation on, unified mounted turn and paired command scheduler off (`DiagnosticSettings` defaults; no player switch, no settings file). Only `TryConfigurePairedActivation` writes the flag. It requires a dismounted pair outside combat, and nothing outside the Diagnostics namespace calls it. `MountedAuthorityPolicy` qualifies only this combination for combat mounting. |
| Native turn and round identity | `MountedMovementStateAdapter.Allocation` records the native `CombatController`, `RoundNumber`, `RoundStartTime.Ticks` and a grant identity. |
| Shared paired grant | `PairedActivation.Identity` = `EncounterId:Sequence`. `Begin` at the rider's slot increments `Sequence` (a new grant per round). Delay (`Suspend`/`Resume`) keeps it. The partner context's preparation calls `BeginGrantedPreparation(partner, activation.Identity)`, a fresh mount record at travel 0. |
| Standalone mount grant | `HandleTurnPreparing` gives a mount its own `native:<guid>` grant only when it holds no part of an unsplit activation (no activation, not in it, or split). |
| Record creation and replacement | A new record at each granted preparation. `Get` replaces an ungranted record from another controller or round. |
| Record retirement | `MaintainLifetimes`: a session change, and settled actors out of combat. Also destroyed actors, a completed encounter and `Clear`. All read-only while in combat. |
| Exact travel | The mount's native `TurnController` `TimeMoved`, `TimeMovedInForceMode` and `TimeMovedByFiveFootStep`, copied by `ObserveNativeMovement`. The delegated path accumulates the same three through `MountedMovementState.Advance`, which mirrors `TickMovement`. |
| Inferred fallback | **None.** The rule reads only the allocation's recorded native movement time. The legacy "ungranted record of the current round" branch of `IsAllocationCurrent` applies only when paired activation is off, which production never is. |
| Normal movement | `TimeMoved − TimeForced − TimeStepped`. |
| Five-foot step | Excluded (step time). |
| Charge | Excluded (forced-mode time). |
| First and second Move | A Move is 3 s of movement time. The second Move is entered past 3.25 s. |
| Mid-encounter mounting | `MidEncounterAdoptionPolicy` admits it only on the exact rider's running turn, with two outcomes depending on the mount's slot. A later slot gives `PreparePartnerThisRound`: the mount is prepared as the partner and its record carries the shared grant. An earlier slot gives `RetainPartnerParticipation`: the mount already acted riderless, and is recorded granted, prepared and ended, so it cannot be addressed or moved for the rest of that round. |
| Pair owner and generation | A remount after a dismount is a new `PairedActivation` (new `EncounterId`), so the earlier generation's grant can never equal the new one. In turn-based play a remount is refused until the split's release round has passed. |
| Round changes | A new `Begin` gives a new grant, so the previous round's record is not current. |
| Combat exit and re-entry | `MaintainPairedLifetime` retires the activation when the party leaves combat. A still-mounted pair is re-armed, and the next encounter creates a fresh activation. |
| Load | The activation snapshot (same `EncounterId` and `Sequence`) and `SavedMovementValues` (movement time, grant and round) are restored together. No format change. |
| Area, world or session replacement | Another controller is never current. A session change retires records. |
| Attack seam | `MountedRangedMovementService`, a global about-to-trigger handler on `RuleCalculateAttackBonus`. It acts when the previous event is the same attacker's unresolved `RuleAttackRoll` (same target and weapon), the attacker is the active rider of a Mounted pair, and the weapon is ranged, non-natural, wielded by the rider and not a spell proxy (Ray, Touch, KineticBlast, Bomb). It then reads travel once through `TryReadCurrentMountTravel` and adds one `BonusSource`, refusing a second. |
| Dead Shot | A probe shape and a prefix confirmation are committed calculations; a preview, a parry or a resolved roll is not. |
| Feat and publication | `IsMountedArcheryAvailable` gates the rule: the feat is registered, its labels present, it is published, and both required lists hold it. A withdrawal removes the feat everywhere and turns the rule off. |
| Native floor | With `GameDifficulty.TrueDeath` false, native lifts a player-faction attack total to at least BAB − aggregate penalty − 2 after every contribution. Unchanged. |
| Log | The breakdown lists "Mounted movement -4" or "Mounted movement (Mounted Archery) -2" (ASCII hyphen), two label facts. The service logs one `Mounted ranged movement:` line per turn-based mounted ranged attack. |

### The twelve distinctions

| # | Case | Handling at the accepted base |
|---|---|---|
| 1 | Shared paired activation | The mount's record carries the pair's grant: counted. |
| 2 | Mount's independent native initiative | It does not occur while mounted. When it occurs the mount is riderless or not yet mounted, and the record's `native:` grant is excluded. |
| 3 | Rider's independent native initiative | The rider is the activation's principal. A rider-only turn (split, or before adoption) has no current pair grant and no active Mounted relationship, so no contribution. |
| 4 | Riderless mount movement | `native:` grant: excluded. |
| 5 | Movement before mounting | Riderless on the mount's own slot (`RetainPartnerParticipation`), excluded. Or replaced by the partner's fresh record (`PreparePartnerThisRound`). |
| 6 | Movement from a prior pair generation | Different `EncounterId`: excluded. |
| 7 | Movement from another round | Different `Sequence`: excluded. |
| 8 | Real-time movement | No contribution in real time (`NotTurnBased`). |
| 9 | Charge | Forced time excluded. |
| 10 | Five-foot step | Step time excluded. |
| 11 | Ordinary normal movement | Counted. Penalised once past 3.25 s. |
| 12 | Exact and inferred travel | Exact only; there is no inferred fallback. |

## R01 — why the separate-initiative case has no reachable instance

Each link, with its evidence kind:

1. **The production authority is paired activation.** Production executed: the `DiagnosticSettings` defaults and
   `MountedAuthorityPolicy`. Product IL: no writer of the flag outside Diagnostics except the dismounted,
   out-of-combat `TryConfigurePairedActivation`, and no non-diagnostic caller of it.
2. **A mounted pair's mount has no slot of its own.** Product IL: the selector transpiler's hook `SkipPairedCandidate`
   asks `SuppressPairedCandidate`, which suppresses the activation's `Partner` unless the activation is split and its
   release round has passed. The transpiler's patch construction is pinned by `Test-PairedActivationContracts.ps1`.
3. **A live pair always has an unsplit activation in turn-based combat.** Read from source:
   - it is armed when mounting outside combat, and created at encounter start (`BeginNativeEncounter`);
   - mid-encounter adoption creates it;
   - load restores it;
   - leaving combat retires it and re-arms a still-mounted pair.
4. **The only split is the dismount's, and a dismount never leaves the pair Mounted.**
   - Product IL: `PairedActivation.Detach` is called only by `SplitPairedActivationAtRound`, which only
     `SplitPairedActivation` calls, which only `HandleDismounting` calls. `HandleDismounting` is referenced only by its
     subscription to the relationship's `Dismounting` event. `Dismount` raises that event before the coordinator's
     own `Dismount`.
   - Production executed (existing `MountedRelationshipTests`): cleanup leaves `Unmounted` or `Faulted`, never
     `Mounted`.
5. **A pair cannot form on the mount's own slot.** Production executed: `MidEncounterAdoptionPolicy.Resolve` returns
   `Unavailable` unless the current turn is the exact rider's.
6. **A retained partner cannot move again carrying the rider.** Production executed:
   `AdoptRunningBoundary(RetainPartnerParticipation)` marks the mount ended, and `CanAddress` is false.

**Consequence:** every `native:` mount record holds riderless or pre-mount travel. "The same horse moved this round"
would charge exactly that travel to the rider. The accepted exclusion is therefore correct and complete, and there is
no separate allocation whose identity is missing.

### Precedence and double counting

- **One authority:** a record is current only when its grant equals the pair's current paired grant
  (`IsAllocationCurrent`).
  - A shared record therefore cannot also count as separate travel, since no separate authority exists.
  - A standalone `native:` grant is never converted into a paired one.
- **One reading per roll:** the attack seam reads one record once and adds one source, refusing a second
  (contract C4).
- **Reading does not consume:** the reader writes no field and calls no setter or allocation writer (contract C4), and
  the policy is a pure function of the record (component test).

### R01 matrix

| Row group | Result | Evidence |
|---|---|---|
| A shared activation | Unchanged: stationary 0; one Move 0; second Move −4 / −2. Mid-encounter `PreparePartnerThisRound` counts mounted travel. Riderless travel before adoption and the rider's own retained native grant are excluded | Existing `Contributions`, `MidEncounterAdoption`, `AllocationCurrency`; new `OwnSlotRecordsStayExcluded` |
| B separate mount initiative | **No reachable instance** (the chain above). Each row's scenario requires the mount to move on its own slot while carrying the rider, which cannot happen. A rider attacking before or after the mount's own slot sees only shared-grant travel, which is per roll, never revised and never consumed | New `SeparateSlotCarriedTravelNotReachable`; contract C1–C3 |
| C pair and lifetime | Excluded: before mounting, dismount/remount, a new generation, the previous round, another encounter or controller, a stale or foreign grant, another pair, the same rider on another mount, another rider on the same mount. An unmounted rider gets nothing (`NotActiveRider`) | New tests; existing `Gates`, `Lifetime` |
| D classification | Step 0, Charge 0, first Move 0, second Move penalised. No inferred fallback. Real time 0. No Run. Speed, gait and acceleration do not turn one Move into two | Existing `BorrowedTimeBoundary`, `LegsAndSpeed`, `NonQualifyingTravel`, `AutoStopAndSecondClick` |
| E attack shapes | Ordinary ranged and firearm (touch AC) qualify. Dead Shot probe and confirmation are committed rolls. Every attack of a full attack reads separately. Crit confirmation reuses the bonus. Excluded: preview, sheet, parry, melee, natural, the mount's attack, spell, ray, touch, kinetic, bomb, and an unwielded or foreign weapon | Existing `WeaponAttacks`, `DeadShotShapes`, `PerRollTiming`, `Gates`; native seam pinned by `Test-AssemblyContracts.ps1` |
| F floor and log | Raw −4 / −2 (`DoubleMovePenalty`, `MountedArcheryDoubleMovePenalty`). The native floor may reduce the net effect without changing the recorded source. The labels are as above, and the reduction names the feat without implying it imposes the penalty | Contract C5; existing `Contributions` |

**Before and after:** there is no old-fails/new-passes pair, because no product behaviour changed and the scenario
does not occur. The accepted base excludes a `native:` record, which is correct. The two ways to make the
separate-initiative rows "pass" both reintroduce riderless travel and are caught:
- S1 accepts a `native:` grant;
- S2 is a same-round rule when no shared grant exists.

## The final ranged-movement rule (R02)

- **Who:** the active rider of a Mounted pair, in **turn-based** combat only. Real time is unchanged native.
- **What:** a ranged weapon attack with a ranged, non-natural weapon the rider wields: bows, crossbows, slings, thrown
  weapons and firearms (touch AC included), however the roll is delivered, including Gunslinger Dead Shot.
- **Excluded attacks:**
  - spell rays, touch spells, kinetic blasts and bomb proxies;
  - melee and natural attacks, and the mount's own attacks;
  - previews, the character sheet and tooltips, parries, and an unwielded or foreign weapon.
- **Travel counted:** the mount's normal locomotion in the pair's current shared activation:
  `TimeMoved − TimeMovedInForceMode − TimeMovedByFiveFootStep`.
  - Five-foot steps and Charge are not normal movement.
  - Standing up spends a Move but no travel.
  - The mount's own riderless slot, travel before mounting, an earlier generation, an earlier round, another pair and
    another encounter never count.
- **Threshold:** a Move is 3 s of movement time. The penalty applies once travel exceeds **3.25 s**.
  - The 0.25 s tolerance is one native turn-based tick (`TimeController`: scale clamped to 3, tick = scale / 12). Native
    may grant it once as "borrowed time" at the end of a move reaching the boundary (`TickMovement` within
    `NearTheEnd`).
  - So travel up to 3.25 s may be one move plus that correction.
  - At most 0.25 s (about 1.3 m for a 50 ft horse) of genuine second-Move travel goes unpenalised.
  - Speed, haste, acceleration, gait and path length do not change the rule: it is movement time, not distance.
- **Penalty:**
  - **−4**, or **−2** with Mounted Archery (Mobility 1 rank and Mounted Combat);
  - one contribution per attack roll, determined when that roll is made;
  - earlier rolls stay final, and later movement affects only later rolls.
- **Not implemented:**
  - Run (Kingmaker has none);
  - PF1's midpoint resolution (no scheduling);
  - Mythic Mounted Archery;
  - any movement restriction, action, preparation or refund.
- **Native attack-bonus floor:** unchanged. On a low-bonus rider it can absorb part of the penalty. The breakdown still
  lists the raw source.
- **Combat log:** "Mounted movement -4" or "Mounted movement (Mounted Archery) -2".

**This is a deliberate Kingmaker/KMC adaptation, not a reproduction of every PF1 mounted-archery timing rule.**

## R02 documentation changes

- `docs/MOUNTED-ARCHERY-HANDOFF.md`: a superseding Sprint 5 section. The history below it is not rewritten.
- `README.md`: the shooting-from-the-saddle line now says five-foot steps and Charge do not count. That exclusion was
  in the feat text but not the README.
- Feat name, description and prerequisite text, and the log labels: reviewed and unchanged; they are accurate.
- `docs/RELEASE-NOTES-0.2.9.md`: historical, not rewritten. Its separate-initiative line is superseded by this record.

## Validation (budget `known-issues-sprint5`)

| Check | Result |
|---|---|
| Clean Release build of the head (outputs removed first) | PASS; identity above, byte-identical to the accepted `cdfbe509` build (log `05-clean-release-build.log`) |
| Components | 1387/0 (1385 at the base; +2 Sprint 5 reconciliation tests) |
| `Validate-Source.ps1` | 151/0 |
| `Test-MountedRangedMovementContracts.ps1` (new, 5 checks) | 5/0 (log `02-ranged-movement-contracts.log`). The script is new because no existing script pinned the ranged rule's product seams or the separate-slot exclusion chain |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint5`, focused: ranged movement, paired activation, Sprint 6 feat readers, physical mover pace, persistence | PASS 8/0 (0.82 min) (log `04-fast-tier.log`); budget spent 0.82 of 90 minutes over 1 entry |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `06-assembly-contracts.log`) |
| Mutations (disposable detached worktree with its own binaries; the contract reads each mutant DLL by path; log `03-mutations.log`) | S1 a mount-own native grant counted (riderless). S2 a same-round rule when no shared grant exists (before mounting). S3 an old pair generation (sequence only). S4 a stale round (encounter only). S5 a foreign controller. S6 another pair. S7 a Charge counted. S8 a second contribution on one roll. S9 real time. S10a a spell, kinetic or bomb proxy. S10b a resolved roll (parry). S11 the record consumed after a read. All 12 caught: S1-S7, S9 and S10a-b by component tests (S2 also by contract C5), S8 and S11 by contract C4; each contract run names its mutant MVID |

- **Not run:** Full, Candidate, historical campaigns, release qualification and unrelated stale harnesses.
- **Persistence:** no new lifetime exists, so only the existing persistence contracts are run, as unchanged neighbours.

## Evidence kinds

| Kind | What |
|---|---|
| Production code executed offline | `MountedRangedMovementPolicy`, `MountedMovementState.Advance`, `PairedActivation`, `MidEncounterAdoptionPolicy`, `MountedAuthorityPolicy`, `DiagnosticSettings`, `MountedRelationshipCoordinator` (existing tests); the compiled constants and decisions (contract C5) |
| Installed native IL inspected | The attack-roll seam, movement time and the TB tick bound (existing `Test-AssemblyContracts.ps1` pins); no new native pin |
| Native methods executed detached | None |
| Compiled product IL inspected | The split chain, candidate suppression, authority writers, attack seam and read-only reader (contract C1–C4) |
| Represented engine behaviour | Native-shaped movement ticks in the existing tests |
| Modelled orchestration | None new |
| Native gameplay | **NOT RUN** |

## Remaining native evidence limits

- **Not observed in play:**
  - the penalty itself (N1–N6 and C, C2 of the Mounted Archery handoff);
  - the suppression of a mounted mount's slot in a live round;
  - a remount after a split.
- **Not executed offline:** the attack seam and `TryReadCurrentMountTravel`. Both are pinned by IL only, because the
  test host has no game types.

Sprint 6 was not started. This is the Sprint 5 review checkpoint.
