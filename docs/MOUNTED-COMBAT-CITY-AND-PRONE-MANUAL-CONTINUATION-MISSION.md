# Mission: finish PR #57 in manual Claude development mode (owner assignment, 2026-11-22)

Recorded verbatim as assigned by the owner (Howie) on 2026-11-22, replacing the execution model and remaining work plan of
[the original mission](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION.md) and
[the mission with the toolbar repair addendum](MOUNTED-COMBAT-CITY-AND-PRONE-MISSION-WITH-TOOLBAR-REPAIR-ADDENDUM.md).
Their product acceptance criteria, archive compatibility, historical regressions and repository safety requirements remain
applicable. Execution record: [the handoff](CITY-AND-PRONE-HANDOFF.md).

---

## Owner direction and objective

For the remainder of this ticket, return to ordinary interactive Claude development. Claude implements and runs focused
checks, then returns a concrete commit and handoff to Howie. Howie obtains Pro's independent review and brings any
corrections back to Claude. Do not invoke Codex, resume or create an enrollment, schedule a reviewer, or run an autonomous
writer/reviewer loop.

Complete the remaining work on the existing mounted-combat ticket:

1. Preserve the player's chosen Mount / Dismount toolbar placement across city, area, party/reserve, save/load and companion
   lifecycle changes, while respecting deliberate moves and removals.
2. Make the owner's actual animal companion accessible in the city's Inventory and Character interfaces.
3. Resolve and accurately verify the report that a mounted ranged character cannot attack a prone target.

This mission replaces the execution model and remaining work plan in the previous missions. Their product acceptance
criteria, archive compatibility, historical regressions and repository safety requirements remain applicable. Preserve the
existing implementation and history. Produce the complete source correction and focused evidence before the manual review
checkpoint; an outline or another enrollment is not the deliverable.

## 1. Verified starting point

- Repository: howardreith/KingmakerMountedCombat
- Draft PR #57
- Branch: codex/mounted-city-and-prone-fixes; target: main
- Evaluated candidate: 6251885809dfcb7f036974ba1ae98eb96f6817af
- Evaluated base: dbf637021db42cdc9bf27f3d79fc255d4569c788
- Latest review: replacement enrollment round 3, changes_required.
- Latest controller status: BLOCKED, three of five rounds completed; R1-F10 and R1-F13 persisted across three reviews.
- Current enrollment: kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f.

Verify the actual branch, worktree, origin, head, latest comments and controller state before starting. If they advanced,
reconcile that evidence without reverting newer work. Archival may add a transcript-only successor commit; record both the
reviewed source and the actual starting head.

Finding namespaces matter. R1-F10, R1-F13, R2-F1–F5 and R3-F1–F3 below refer to the replacement enrollment. The six AC-R...
cases and archived R17/R19 cases refer to the original 26-round enrollment.

### What is already accomplished

The proposed architectural repair now exists. Domain/MountedToolbarLifecycle.cs owns production sequencing and placement
decisions; MountedControlPlacementIntent.cs holds the arrangement state; NativeMountedControlService.Toolbar.cs supplies the
game adapter. Component tests execute that lifecycle. The adapter fixture compiles the actual toolbar adapter, and the
mutation script mutates production sources.

Keep this structure. The current work is to complete its boundary behavior and coverage, not to restart the refactor.

The round-3 submission records a passing focused Fast gate on the evaluated candidate: 1,463 component checks, including 43
toolbar tests; five adapter wiring checks; 18 production mutations killed. All six inherited acceptance cases passed at
component level. Those results do not cover the additional sequences below.

The city roster projection and the explicit already-unconscious-target correction remain in place. Neither has
candidate-native acceptance evidence. The original prone-target report remains unreproduced; whether the reported target was
conscious and prone or already unconscious remains unknown.

## 2. Read and take ownership safely

Read AGENTS.md, docs/VALIDATION-POLICY.md, the current sections of CURRENT.md and AUTONOMOUS-RESUME.md, and
docs/CITY-AND-PRONE-HANDOFF.md. Consult both previous mission files for product contracts and the repair addendum, but follow
this mission's manual execution instructions.

Before production edits, finish the controller handover using the installed, enrollment-pinned controller:

1. Inspect its supported status --enrollment <id> command and actual activity. If a writer or transaction is still active,
   use the supported safe stop request and allow its current work and restoration to settle.
2. Preserve unknown local edits. The controller's archive --enrollment <id> requires a clean worktree at the pushed head.
   Resolve understood discrepancies through the supported workflow; do not erase work or bypass a claim. If unexplained
   changes or an ownership/version mismatch block archival, preserve the state and report the concrete blocker; do not
   resume or re-enroll to make archival succeed.
3. Archive the replacement enrollment's published requests, reviews and statuses before canceling it. Use its supported
   archive command, which has no --reason option. Preserve the original enrollment's 114 archived records and the private
   evidence. Do not hand-edit controller-owned archives.
4. Run supported cancel --enrollment <id> --reason "Owner-directed return to manual development for the remainder of PR #57".
   Verify cancellation and release of the branch/worktree claims. Preserve the cancellation receipt in the current handoff.
5. Continue on the same branch and draft PR. If this handover is already complete, verify the records and continue without
   repeating it.

Cancellation alone does not archive outstanding reviews. Do not delete locks or state, kill active helpers, change controller
code, raise limits, or re-enroll to bypass an obstacle.

After cancellation, use ordinary repository validation entrypoints and the repository-approved guarded push route. The
canceled enrollment's verify and push helpers no longer grant authority. Where no project push helper exists, follow
AGENTS.md's audited ordinary push requirements: tracked payload, clean worktree, approved origin and fast-forward
relationship.

Add this continuation mission as a new document if repository instructions call for a durable assignment. Keep the older
hashed missions and review records intact.

## 3. Complete the toolbar repair

Eight functional findings remain. Work through the following related groups with failing behavioral reproductions and
coherent corrections. Preserve save kinds, existing accepted readers, stable actor/pair identities and the 128-row archive
bound. Avoid adding a parallel policy implementation or a broad new abstraction.

### A. Authoritative capture across every production entry point

Replacement R1-F10 and R3-F1

Current paths: MountedToolbarLifecycle.CaptureRows / CaptureLiveRows, NativeMountedControlService.CapturePersistentSlots /
CaptureOtherLiveSlots, and ToolbarHost.HoldsLiveBinding. Area transfer and retained remount both call the in-world capture
path.

Apply exact current-instance authority and a fresh observation before constructing public and other managed rows. A former
rider object retained by pair custody remains eligible for sanitation, but must not author the replacement rider's
persistence. Ensure capture preparation happens before either category of rows is emitted.

The production host's public-binding predicate must accept only canonical Mount / Dismount or supported Mount/Dismount
aliases. A callable Mounted Charge is not a public binding. Correct the source of wrong rows; deduplicating a corrupted result
is insufficient.

Required evidence:
- Keep the former rider object in pair custody with slot 9. Give its current replacement, with the same actor ID and
  companion, a fresh placement at 11. Capture before Update, through owned-save, area-transfer and retained-remount entry
  points. Require current placement 11 once and one placement marker.
- Give the former object a callable managed control at the replacement's index 11. The complete owned archive must validate
  without duplicate keys.
- Through the actual adapter, put callable Mounted Charge at the current rider's former public index 9. Test public control
  removed, then public control moved to 11. Capture before reconciliation: retain Charge correctly and emit no false kind-6
  row at 9.

### B. Deferred load selection and observation provenance

Replacement R2-F1 and R2-F2

Current paths: SeedFromRows, SelectSeededPublicRow, incoming-world observation and archive restoration.

Retain usable alternatives from older archives until the actor and toolbar are readable enough to evaluate occupancy. Do not
permanently choose the first row merely because every occupancy check is unavailable.

Reconcile an observation made before archive seeding with the archived choice and the binding's provenance. A native read of
a leftover legacy alias is not evidence of a player move. Neither "the observation always wins" nor "the archive always
wins" handles every required case.

Required evidence:
- With autofill off, seed kind 6 at 3, a legacy Mount/Dismount row at 9 and one marker while the rider's bar is unavailable.
  Reveal Fireball at 3 and empty 9. Require Fireball unchanged and one shortcut at 9 after repeated updates, resave and cold
  restore. Keep the marker-only control.
- For a reserve rider, use saved choice 3, empty native slot 3 and a lone stale alias at 9. Native GetSlot(9) empties that
  alias. Run the call immediately before seeding and immediately after seeding. Both orders must preserve only 3 and one
  marker on immediate save, party return and cold restore.
- Retain genuine marker-only native recovery and deliberate-removal controls.

### C. Companion retirement while the real bar is hidden

Replacement R2-F3

Record a known companion link independently of bar availability. Preserve sufficient retirement/binding provenance to
sanitize the previous companion's surviving shortcut when the bar returns, while accepting a subsequent deliberate placement
for the new companion.

Required evidence:
- Hide and later reattach the same underlying bar array, with A's original slot-9 object intact. Replace companion A with B
  while the bar is unreadable, observe retirement, reveal the bar, then update, save and cold-load. No old slot 9 may be
  inherited.
- Seed saved slot 9 while A is known but the bar is hidden; replace A with B; reveal an empty bar. Again, B inherits no 9.
- Same-pet temporary unavailability restores 9 once. A genuinely fresh placement for B at 11 survives once.
- Do not "hide" the fixture by first replacing the bar with an empty array; that removes the failure trigger.

### D. Native callback ownership, exception outcomes and bar ownership

Replacement R2-F4, R2-F5 and R3-F2

Current paths: lifecycle BeginNativeSlotOperation, EndNativeSlotOperation, SettleNativeOperation, ApplyNativeOperation;
adapter BeginNativeBadSlotCleanup, EndNativeBadSlotCleanup, ResolveBarUnit; Harmony forwarders in MountedPatchController.

A postfix must complete only its own invocation. A prefix that opens no operation cannot consume an earlier deferred
operation. Preserve the outgoing world's operation through a selected-load null-player interval, and release it on genuine
world replacement, unfenced disposal or disable.

Distinguish GetSlot from CleanupBadSlots. Capture completed writes at a supported exception-safe boundary, or retain
sufficient method-specific evidence to distinguish those writes from later player edits and fact changes. Preserve the native
exception. Include the ordinary occupancy needed to decide fallback at the time of the native operation.

Verify the installed Harmony12 and Kingmaker contracts before selecting a hook strategy; do not assume Harmony 2 finalizer
APIs exist.

Identify the current bar's owner from the verified native unit/settings relationship. GetSlot(int index, UnitEntityData
unit) supplies a unit argument that the present prefix omits. A stale slot's former owner must not suppress observation of
the current bar or stop a whole-bar search before later valid bindings.

Required evidence through the real adapter where callback forwarding matters:
- Native empties 9 and throws; a selected load begins; Player becomes null; a complete ordinary/empty-slot GetSlot call
  runs; the load is abandoned back to the outgoing world. Require row 9, one marker and one recovered shortcut. Preserve
  replacement, disposal and disable controls.
- With 3 pending and a known copy at 9, cleanup genuinely rebinds 9 and throws; deactivate the fact before deferred
  settlement. Choice remains 3.
- In the same arrangement, GetSlot throws before writing; the player places a new live control at 9 before settlement. The
  genuine new choice is 9.
- Fireball occupies pending choice 3; native empties fallback 9; Fireball moves before deferred settlement. Preserve the
  fallback already earned at 9 through save and reload.
- A current rider's bar contains a stale public slot naming a discarded rider object. Native empties it before the first
  scan. Recover for the current rider only.
- In whole-bar cleanup, place a correctly owned chosen binding after a stale-owner alias. The stale alias must not prevent
  observation and recovery of the valid binding.

## 4. Restore exact regressions and preserve accepted behavior

Replacement R1-F13: three missing historical sequences

Compare the original round-19 review, neighboring dispositions and the handoff map. Add the complete sequences, not tests
that separately exercise their ingredients:

1. Original R17-F1, round 19: interrupted chosen 4; Fireball occupies 4; alias fallback 9; begin selected load; native
   empties 9; abandon; save and cold restore. Fireball remains at 4 and exactly one public shortcut returns at 9.
2. Original R17-F2: an aborted native call immediately followed by a successful GetSlot(9) before Update. The successful
   invalidation remains recoverable. Keep the no-write/read-only and deliberate-removal controls.
3. Original R19-F3: retire companion A, assign B, deliberately place B's fresh control, then native invalidates it before
   the first ordinary observation. Save and cold restore preserve B's new placement and reject A's retired state.

Retain every distinct existing historical regression. Map each finding to the actual test/subcase and asserted outcome. A
suite name alone is insufficient when that suite omits the event order.

Six inherited acceptance cases are currently passing; preserve and report them individually:

| Original case | Existing test name | Required behavior |
| --- | --- | --- |
| AC-R26-F4 | AcR26F4ReservePetSanitized; PairCustodyOwnerPetSanitized | Reserve owner's horse loses old managed slots and auto-use on save/disable; ordinary controls survive. |
| AC-R22-F3 | AcR22F3FreshPlacementAfterNativeEmpty | Fresh reserve placement after native invalidation wins and restores once. |
| AC-R25-F3 | AcR25F3FreshShortcutAfterReplacement | Replacement companion accepts a fresh placement without inheriting retired intent. |
| AC-R26-F1 | AcR26F1CanceledLoadKeepsEdit | Canceling a selected load preserves the outgoing reserve edit. |
| AC-R26-F2 | AcR26F2MainMenuReleases | Discarded world and duplicate identities are released at the main menu; cancellation retains the outgoing world. |
| AC-R26-F3 | AcR26F3RejoinedRiderAutoUse | Save/disable clears a returning rider's stale managed auto-use while preserving ordinary auto-use. |

Preserve the shared lifecycle properties: observe intent before destructive cleanup; equivalent save-first/update-first
outcomes; repeated scans/saves are idempotent; cancellation preserves outgoing state including copy identity; successful
replacement releases old-world state; native invalidation remains recoverable while deliberate removal stays removed.

Use realistic state and the real production component. Fake only the game-facing objects needed by the existing harness.
Assert live toolbar contents, archive rows/validity, restoration and resource lifetime. The existing fixture still mirrors
some main-service members and Harmony forwarders: keep that limitation visible, and add focused production-boundary/contract
coverage for the forwarding changed by this repair.

Retain useful production mutations, adapting them to the corrected design. Add only mutations needed to demonstrate the
specific repaired boundaries remain connected. Do not use test-only policy mutations or weakened thresholds to manufacture a
pass.

## 5. Preserve the city and combat work; complete honest product verification

### City companion access

Keep the narrow capital UI projection of the owner's exact persistent, reciprocal companion into the native
Inventory/Character group. This feature concerns viewing and managing that unit; it must not spawn another animal, change
actual combat party membership, transfer inventory or alter formation/XP behavior.

Native acceptance still needs selection of the actual reserve pet, correct details and equipment slots, equipping an
identifiable item, reopening the screens, and save/load confirmation that the same animal retains that item. Check absence
of duplicate portraits and correct disable/re-enable behavior.

### Mounted ranged attack

Use this status wording unless stronger evidence is obtained:

The identified rejection of an explicit mounted attack on a living target that was already unconscious is corrected in
source. The original prone-target report remains unreproduced; whether the reported target was conscious and prone or
already unconscious remains unknown.

Preserve the narrow allowance for the exact explicitly ordered target. Keep dead-target rejection, automatic-target rules,
Charge/continuation restrictions, line of sight, range, actions, penalties and unrelated actors intact.

After independent source review, use the existing guarded native route or a concrete owner playtest to compare the same
rider/weapon/positions mounted and dismounted against standing, conscious-prone and already-unconscious targets, at near and
ordinary firing distances, in real-time and turn-based modes. For a prone body, distinguish clicking the lying body from its
standing footprint. Observe actual rolls, expected action expenditure, native modifiers and whether a click becomes
movement.

If conscious-prone targeting fails, locate the failing stage—hit testing/selection, command admission, path/range, or
execution—and make only the demonstrated correction, with a focused regression and another manual source review before live
use. Do not broaden eligibility speculatively or call the original report resolved from a policy test.

## 6. Validation and the manual review checkpoint

Follow the repository's focused validation policy. Start with the failing reproductions and affected neighbors, then use the
existing focused entrypoint on the coherent correction:

    .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1

Inspect its selected children and current budget before running it. Use the actual repository command if the entrypoint
changed. Reuse unchanged qualified artifacts and bind evidence to the source it exercised. Preserve the cumulative
workflow-budget ledger; changing development mode does not reset it. Follow the ordinary 15-minute planning target and
30-minute ceiling, with safe settlement/restoration always completed.

A development checkpoint does not select Full qualification or the historical 109-stage/105-row campaign. Candidate includes
Fast; do not run both on unchanged inputs. Broaden checks only for a concrete remaining dependency or risk.

Report the area adapter's existing 63/63 result accurately: it matched compiler diagnostics and did not execute the
area-restoration assertions. Keep UNIT, adapter/contract execution, source TRACE and NATIVE evidence distinct. Do not present
old receipts as execution on changed code.

Manual checkpoint: finish the source repairs, tests, dispositions and handoff; commit and safely push the reviewable
candidate to the same draft PR; then return the package to Howie for Pro review. Do not start an automated review or another
work cycle on an invented approval.

After that independent review, assess the normal repository-approved build/package and guarded native route. The old
headless enrollment's command allowlist was one reason packaging could not run; it is not proof that manual verification is
impossible. Use the smallest supported verification path. Any runner/reader changes should be narrowly scoped and reviewed
where required; do not rebuild the whole native campaign.

Only guarded repository scripts may stage or launch. Preserve actual current installation/settings/caches and protected
saves, use qualified disposable fixtures, and verify exact restoration. Preserve installed Kingmaker/Harmony contracts and
Tabletop Rules Expansion click composition. No merge, release, tag, force-push, history rewrite or permanent installation is
selected.

If native work is unavailable or remains owner-run, supply executable playtest steps, the exact remaining prerequisites and
honest NOT RUN/PENDING statuses. A source checkpoint can be ready for review while product acceptance remains pending.

## 7. Required handoff

Update docs/CITY-AND-PRONE-HANDOFF.md and the current project summary. Correct stale statements that round 3 is pending or
that every round-2 finding was accepted as fixed. Preserve earlier reports as dated history.

Return:
1. Branch, exact source commit, draft PR URL, concise change summary and clean/pushed state.
2. One disposition table for all ten replacement-round-3 findings: R1-F10, R2-F1–F5, R3-F1–F2, R1-F13 and R3-F3. Give
   correction, specific test/subcase, outcome and evidence type. Distinguish implementation claimed by Claude from
   independently verified closure.
3. The six inherited cases individually, with PASS/FAIL/NOT RUN, test name and evidence type, in both the handoff and
   developer summary. This closes the reporting requirement R3-F3.
4. The historical regression map, including the three previously missing sequences.
5. Actual validation commands, durations, counts, logs, artifact/source identities and material limitations.
6. Separate status for each original user issue, distinguishing source completion from native behavior verified.
7. Any native package/playtest evidence and restoration record, or precise pending steps. The original prone report remains
   open until the relevant behavior is verified or its exact unresolved status is explicitly accepted by the owner.

The next deliverable is a coherent corrected development candidate for manual review. Completion of the entire ticket
additionally requires accurate disposition of the original gameplay behaviors; passing component counts alone is not that
acceptance.
