# Chunk 7 — original scope and acceptance matrices

Reference only. The excerpts below are copied without substantive edits from the owner-supplied oversight package's extracted September 6, 2026 revised project plan. Extraction artifacts are retained. This reference is not a new current-state report or execution authorization.

## Current interpretation belongs to the standing mission

Use `KMC_Chunk7_Standing_Completion_Mission.md` and later explicit owner decisions for authority. In particular, the original F05 defensive feat and TB Charge delivery are subsequently deferred, native action/turn rules have approved later corrections, and HUMAN PLAY remains a separate owner gate. Do not use an old matrix sentence to reopen an expressly deferred feature. Do not silently remove an otherwise mandatory row. The full engineering finish includes an exact-package rehearsal; public promotion remains separate.

The primary briefing identifies the source as `Kingmaker_Mounted_Combat_Revised_Project_Plan.docx`, Revision 2, September 6, 2026; Chunk 7 pages 16–17 and acceptance matrices pages 18–19. These are text excerpts, not a reproduced DOCX layout.

## Original Chunk 7 chapter — exact extracted text

```text
Chunk 7 — breadth and release
Expand a working core, then certify one exact supported product.
Multiple pairs
Generalize relationship lookup from one active pair to independent pair records. Commands carry
pair generation and actor allocation identity. No shared selected-pair field may control another pair’s
movement, action budget or restoration. Test two pairs in combat and six pairs in
lifecycle/performance stress where the fixture permits.
Keep actor budgets native and pair lookup bounded. Measure hot-path allocation, frame cost, log
volume and subscriptions through repeated mount/unmount/area cycles. Remove unconditional per￾frame reflection, broad scans and verbose tracing where measurements show a problem; do not
redesign the whole mod for a speculative benchmark.
Certified profiles and content
Certify the existing Medium Horse and Mammoth profiles, plus at least one Small-rider profile.
Publish supported body/equipment/form combinations and explicit exclusions. Effective size and
native companion ownership remain authoritative; changing render scale is not eligibility.
Retain the existing horse acquisition architecture, but verify new character, existing campaign, level
progression, dismissal/replacement and retraining paths. Paladin Divine Steed and other class grants
remain after this release boundary, not a reason to hold the core.
Release configuration
Ship one supported rules preset and one accepted turn architecture. Remove normal-player access to
failed historical experiments. Keep diagnostic tooling opt-in and inert unless explicitly invoked; the
release must not need a diagnostic overlay or console.
Certify the pinned base-game/loader/Harmony configuration first; add one explicitly pinned high￾value mod combination only if it fits the remaining budget. Preserve unrelated actors and other mods.
Current installed versions must be read locally; never downgrade a functioning loader to match stale
notes.
Candidate promotion
Build from an identified commit; record source/tree, package manifest, DLL identity, configuration
and test results. Exclude game/Wrath assemblies, decompiled output, private saves and raw personal
logs. Keep the previous package unchanged and document supported rollback.
Run fresh install, upgrade, real manual/quick/auto save/load, documented removal, both-mode play
sessions and the complete acceptance matrix on the delivered package. Reconcile repository
instructions and one concise changelog. No main merge, public release or permanent human
installation is implied by an engineering milestone; those remain explicit promotion decisions.
<PARSED TEXT FOR PAGE: 17 / 23>
KINGMAKER / MOUNTED COMBAT
REVISED DELIVERY PLAN • 6 SEPTEMBER 2026 | 17
Feature complete means all required release rows pass or have a disclosed, previously agreed scope
exclusion. Newly discovered mandatory failures change the date, not the definition. Do not substitute
a high total test count for an unresolved gameplay gate.
```

## Original acceptance matrices — exact extracted text

```text
Core acceptance matrix
Stable scenario IDs; parameterize combat mode, mount, initiative order and loadout instead of creating new phase protocols.
ID Scenario Required observation
C01 Stationary ordinary attack Mounted/unmounted native mode, sequence and charged costs agree
where rules require. Primary remains single.
C02 Prediction and normal input Hover/simulated prediction causes no live mutation; actual command
follows the same valid decision or rejects stale state.
C03 Attack-mode controls Rapid Shot off/on, representative BAB/haste and native restricted/spent￾action controls yield legal native plans and modifiers.
C04 Movement exhaustion Full movement and legitimate Standard conversion exhaust exactly one
mount allowance; split paths cannot extend it.
C05 Actor order and rounds Both starting initiative orders over at least three rounds preserve the
same entitlements and exactly-once ticks.
C06 Complete paired activation Both actors act; early End Turn and normal completion select the correct
unrelated combatant without an extra mount turn.
C07 Merge, split and mode Dismount/re-pair and safe RT/TB transitions preserve expenditure,
readiness and reaction/step commitments.
C08 Approach, range and obstacles Adjacent/distant melee and ranged stop legally; blocked or moving
targets do not cause free transport or wall phasing.
C09 Sustained RT and repeated clicks At least three native cycles; one-click and repeated-click timing match;
pause freezes progress and Stop cancels intent.
C10 Targets and incoming effects Each actor is selectable/attackable/healable; reach, LoS and AoE resolve
correctly without duplicates or immunity.
C11 Interruptions and reactions Target loss, foreign commands, native opportunities and step handling
remain legal; no stale pair command or budget refund.
C12 Human core play and visuals Normal pointer/portrait/hotbar control, readable seat/strike/recovery,
useful feedback and one representative session per mode.
Evidence levels
Record COMPONENT, ASSEMBLY CONTRACT, NATIVE INTEGRATION and HUMAN PLAY
separately. A case may have one level passing and another pending. NOT RUN and BLOCKED are not
PASS. Count one scenario once; parent/child assertions do not inflate product coverage.
Every integration result identifies source/package/configuration, fixture, command origin, expected
behavior, observed native events and costs, and artifacts. Compare observed outcomes to native
controls; do not hard-code the historical five-shot fixture as the rule.
<PARSED TEXT FOR PAGE: 19 / 23>
KINGMAKER / MOUNTED COMBAT
REVISED DELIVERY PLAN • 6 SEPTEMBER 2026 | 19
12 / DELIVERY REVISION
Feature-complete acceptance matrix
These gates extend the core; they are not silently optional when core repair overruns.
ID Scenario Required observation
F01 Combat mount and dismount Legal approach, eligibility recheck and rider cost; no free allocation or
duplicate participation.
F02 Charge and endpoint attacks One legal path, correct participant costs, actor-owned damage, distinct
reach, clean interruption.
F03 Mounted spells and items Native spell/cast resources, reach, held touch, metamagic and
interruptions remain correct.
F04 Staged moving actions Real cast/shot location, declared motion checks/penalties, reserved
movement and no after-the-fact check bypass.
F05 Mounted Combat defense Check boundary and hit negation before consequences; once-per-round
usage and legal reaction debt persist.
F06 Save and cold-load continuity Manual/quick/auto save and cold load preserve pair state, native costs
and supplemental commitments.
F07 Save identity and migration Copied/renamed saves carry their own state; unknown schemas and
missing actors degrade without overwriting data.
F08 Exceptional lifecycle Death, prone, loss of control, form/size changes, displacement, scenes,
rest and ownership changes leave valid native actors.
F09 Multiple pairs and profiles At least two pairs in combat; six-pair stress; Medium Horse/Mammoth
and the declared Small-rider profile pass.
F10 Progression and removal Acquisition, leveling, respec, replacement, documented cleanup and
mod-absent load where claimed are actually verified.
F11 Performance and compatibility No accumulating registrations/view hooks or unbounded logging;
unmounted controls and declared mod combinations pass.
F12 Exact-package release rehearsal Fresh install/upgrade/rollback and both-mode player sessions use the
delivered artifact and supported configuration.
A small reproducible fixture set
Use a low-level and multi-attack rider, the existing Horse plus Mammoth, melee/reach/ranged/caster
equipment, and enemies covering direct damage, AoE, opportunities, conditions and obstruction.
Store recipes and permitted fixture identifiers; keep private save bytes outside the public repository.
Keep only a few reusable native fixtures with parameters. A dedicated fixture reset must not change
production cooldown/selection/attack-mode behavior. Historical build-specific logs remain immutable
evidence, not active test specifications.
```
