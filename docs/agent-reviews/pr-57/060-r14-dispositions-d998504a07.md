<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r14-dispositions-d998504a07 part=1/1 sha256=d998504a07e01814e8526f7b2df2eb97255d733f2254c88cb4fca6bfd88e970b -->
### Claude Opus — finding dispositions — round 14

Candidate after corrections: `83c2aaada817ee2880143fa2b6a4f1a07c372b34`

- **R14-F1** — `fixed` — commits: `83c2aaada8`
  In the canonical emptying branch, ApplySlotInvalidation now adds the actor to placementRecorded when ObserveNativeCleanupOutcome records the invalidation. While a selected load is fenced, the new MountedControlLoadFence.ObserveOutgoingCleanupOutcome (also used by ObserveOutgoingSlotInvalidated) adds the actor to the outgoing placement set when it records. An abandoned load therefore restores that record too.

Regression FirstManualCleanupNoAutofill:
- Setup: a manual canonical shortcut at 9, no placement record, autofill on. The pet becomes unavailable, native cleanup empties slot 9 before any observation, and the player puts Fireball at 9.
- After the pet returns there is no Mount / Dismount shortcut anywhere and Fireball stays at 9.
- The save holds only the placement record and the save scope adds nothing.
- A reload adds no shortcut and keeps Fireball.

The mutation removing the placement record failed exactly this regression. The gate passed on 83c2aaad.
- **R14-F2** — `fixed` — commits: `83c2aaada8`
  MountedControlPlacementIntent.ObserveNativeAliasInvalidated now returns without change when the actor's entry is already interrupted at a different index, for example the canonical slot emptied earlier in the same pass; it still runs the companion-loss check. A later alias report cannot replace the canonical choice. A live (non-interrupted) recorded alias is still invalidated at its own index (R13-F1), and an actor with no entry still goes through AdmitSurvivingBinding.

This holds whether native processes the slots through CleanupBadSlots or one GetSlot call at a time, because the deferred reports resolve in index order.

Regression MixedCleanupPassKeepsCanonical: one cleanup pass over a stale canonical binding at 4 and a stale Mount alias at 9, with neither fact live, empties both. The save holds one public binding, at 4, and a reload then pet return restores one binding at 4 with slot 9 empty. The alias-only recovery control (AliasNativeCleanupFirst) still passes.

The mutation disabling the rule failed exactly this regression. The gate passed on 83c2aaad.
- **R14-F3** — `fixed` — commits: `83c2aaada8`
  New MountedControlSlotPolicy.RememberedSlotTaken(index, liveRows, occupied) is the decision AbsentRowsAfterLive uses. When it holds, CaptureAbsentIntent calls the new MountedControlPlacementIntent.GiveUpOccupiedSlot, which moves the arrangement to off-bar while keeping the pet identity, and adds the placement record. The continuing game and the saved archive then agree; the occupant is untouched.

Regression OccupiedSlotGivenUpEverywhere, run for both continuing and reloading:
- Setup: the same pet is temporarily unavailable, its interrupted slot 9 holds Fireball at save time, and the save records the shortcut off the bar.
- The player clears the occupant before the pet returns.
- The shortcut does not come back.

The mutation making the give-up a no-op failed exactly this regression (reload=False, Actual=9). The gate passed on 83c2aaad.
- **R14-F4** — `fixed` — commits: `83c2aaada8`
  MountedControlLoadFence.Begin gains a controlsActive argument. When it is false the fence is released and nothing is armed or referenced. BeginSelectedLoad passes enabled && !disposed, so a disabled service never holds the outgoing player; it would never reach the Update that lifts a fence on replacement. ObserveInvalidatedPublicSlot now returns while disabled, and BeginNativeBadSlotCleanup arms no bracket while disabled. The enabled service's abandoned-load recovery is unchanged.

Regression DisabledLifecycle:
- The production fence, begun with controlsActive=false, is not fenced, and its outgoing world is collected by GC (WeakReference). Control: with controlsActive=true it is fenced and the world stays alive.
- A disabled service that completes a load from world A to world B leaves no fence.
- Native cleanup while disabled, on a surviving legacy alias and on a canonical slot, creates no placement intent and no placement record.

The mutation removing the disabled guard failed exactly this regression. The gate passed on 83c2aaad.

Limit: the disabled IsBad gate is service code mirrored by the double.
- **R14-F5** — `fixed` — commits: `83c2aaada8`
  In the fenced branch, ApplySlotInvalidation now sends a rebound outcome to loadFence.ObserveOutgoingCleanupOutcome(rebound: true). That records the slot as live (ObserveBound) in the scope's outgoing intent and adds it to the outgoing placement set; the incoming world's records stay untouched. An abandoned load returns the live state, and a completed replacement drops the outgoing records.

Regression FencedRebindAbandoned:
- Setup: interrupted intent and stale data at 9. A selected load fences the world and native cleanup rebinds the slot; the live records stay empty.
- The load is abandoned and the player removes the repaired shortcut before reconciliation.
- The shortcut stays removed, the save holds only the placement record, and a reload leaves the slot empty.
- Isolation control: the same fenced rebind followed by a completed replacement gives the new world nothing.

The mutation dropping outgoing rebinds failed exactly this regression (Actual=9). The gate passed on 83c2aaad.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 56.4s | Result of record, on commit 83c2aaad (log gate-focused-83c2aaada817-d43d449b6c4-a030b738800-20261121T180744960315Z.log; exit 0; worktree unchanged). Covers the build, FAST components, Validate-Source and the five children of the focused wrapper. Offline only. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.81 min | Development run: components 1477/0, Validate-Source 153/0, assembly contracts 822/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The temporary tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1472/5. Mutations in the domain: the interrupted-alias rule disabled, GiveUpOccupiedSlot made a no-op, the disabled-fence guard removed, and outgoing rebinds dropped. Mutation in the double: no placement record on canonical emptying, mirroring the production line. Exactly the five new regressions failed. All mutations were reverted and a grep confirmed none remained. The R14-F1 placement line is service code mirrored by the double. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
