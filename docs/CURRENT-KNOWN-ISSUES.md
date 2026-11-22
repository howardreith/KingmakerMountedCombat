# Current known issues

The canonical current issue ledger for Mr. Bard's Kingmaker Mounted Combat, kept by the Mounted Combat Known Issues
Remediation Epic (owner document of 2026-11-15, revised 2026-11-16, prepared against main `7a5cd2c3`, release 0.2.11).
Current material comes first; the detail and the reconciliation history follow it.

- **Last updated:** 2026-11-19, Sprint 8 (D01–D03): every ID reconciled to one current disposition; the epic is closed
  out in [KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md); the support, removal
  and rollback contract is [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md).
- **Released:** every correction in this ledger is **released in 0.2.12** (2026-11-19, [notes](RELEASE-NOTES-0.2.12.md)),
  after PRs #45–#53 were merged into main; 0.2.11 and earlier releases do not contain them. The four shipped fixes the epic inherited are listed
  under [Superseded by an identified fix](#superseded-by-an-identified-fix-carried-forward).
- **Native status is a separate column.** Nothing in this ledger was run in the game by Sprints 1 to 8, and no Sprint 7
  screen was viewed. "Implemented and verified offline" never means a player scenario passed.
- **Sprint records:** [Sprint 1](KNOWN-ISSUES-SPRINT1-HANDOFF.md), [Sprint 2](KNOWN-ISSUES-SPRINT2-HANDOFF.md),
  [Sprint 3](KNOWN-ISSUES-SPRINT3-HANDOFF.md), [Sprint 4](KNOWN-ISSUES-SPRINT4-HANDOFF.md),
  [Sprint 3 closeout](KNOWN-ISSUES-SPRINT3-CLOSEOUT.md), [Sprint 5](KNOWN-ISSUES-SPRINT5-HANDOFF.md),
  [Sprint 6](KNOWN-ISSUES-SPRINT6-HANDOFF.md), [Sprint 7](KNOWN-ISSUES-SPRINT7-HANDOFF.md),
  [Sprint 8 and epic closeout](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md).
- **After release (2026-11-20):** X01, the 0.2.12 startup conflict with Tabletop Rules Expansion, is recorded under
  [After release 0.2.12](#after-release-0212); repaired and released in 0.2.13.
- **After release 0.2.13 (2026-11-22):** X02–X04 are recorded under [After release 0.2.13](#after-release-0213). X02 is the
  toolbar shortcut's placement, X03 the capital's companion screens and X04 the mounted attack on a downed enemy. All
  three are released in 0.2.14 and none was played in the game. X04's original prone-target report stays open.
- **After release 0.2.14 (2026-11-22):** X05, the post-combat save and door lock, is recorded under
  [After release 0.2.14](#after-release-0214). 0.2.15 releases its diagnostics and accurate save messages; the cause is
  not established and the lock is not repaired.
- Dated handoffs and old failure records are history and are not rewritten. This ledger links to them.

## Current status (Sprint 8, 2026-11-19)

### Owners

| Owner | PR | Branch | Accepted head | Product-build commit | Merge into main |
|---|---|---|---|---|---|
| Sprint 1 | [#45](https://github.com/howardreith/KingmakerMountedCombat/pull/45) | `codex/kmc-known-issues-sprint1` | `b62c59a8` | `3c64128a` (diagnostic evidence code) | `faf1195d` |
| Sprint 2 | [#46](https://github.com/howardreith/KingmakerMountedCombat/pull/46) | `codex/kmc-known-issues-sprint2` | `1dffc3e0` | `5d62a289` | `1fb225a1` |
| Sprint 3 | [#47](https://github.com/howardreith/KingmakerMountedCombat/pull/47) | `codex/kmc-known-issues-sprint3` | `e5b92474` | `f6e86858` | `3d51199e` |
| Sprint 4 | [#48](https://github.com/howardreith/KingmakerMountedCombat/pull/48) | `codex/kmc-known-issues-sprint4` | `14f1b474` | `03685720` | `c458042d` |
| Sprint 3 closeout | [#49](https://github.com/howardreith/KingmakerMountedCombat/pull/49) | `codex/kmc-known-issues-sprint3-closeout` | `af5b2bc7` | `cdfbe509` | `4342596e` |
| Sprint 5 | [#50](https://github.com/howardreith/KingmakerMountedCombat/pull/50) | `codex/kmc-known-issues-sprint5` | `772543e1` | none (`cdfbe509` unchanged) | `1c88d070` |
| Sprint 6 | [#51](https://github.com/howardreith/KingmakerMountedCombat/pull/51) | `codex/kmc-known-issues-sprint6` | `f835c503` | `7898b1a4` | `dd52f359` |
| Sprint 7 | [#52](https://github.com/howardreith/KingmakerMountedCombat/pull/52) | `codex/kmc-known-issues-sprint7` | `1ba1841a` | `e54009e9` | `4b2c0a52` |
| Sprint 8 | [#53](https://github.com/howardreith/KingmakerMountedCombat/pull/53) | `codex/kmc-known-issues-sprint8` | the final head of #53 | none (documentation only) | lands after #52 |

Merged into main in that order on 2026-11-19 with ordinary merge commits, each merged tree equal to the accepted
head; #53 carries this documentation and lands last. Released in 0.2.12. DLL identities and the integration
record are in the [epic closeout](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md#baseline-and-stack).

### Closure categories

| Category | Meaning |
|---|---|
| Engineering complete | The package's engineering work is done offline: a correction with a meaningful regression, a settled investigation, or delivered documentation. Native gameplay stays a separate field. |
| Unresolved | A report, unsupported path or stale tool that remains open, with its precise gap. |
| Documented intentional behaviour | An existing design or safety boundary is kept and explained. |
| Excluded | Outside the epic; kept visible, no sprint task. |

### Every ID

"Affected versions" lists only what is verified. "By source" means read from code, not observed in play. Detail for
each ID is in its sprint section below and in its handoff.

| ID | Symptom or question | Classification | Verified affected versions | Current disposition | Closure | Owner (PR) | Native | Physical UI | Remaining uncertainty |
|---|---|---|---|---|---|---|---|---|---|
| T01 | Charge probe asserted two pairs against six-pair production | Test/harness defect | The probe at `7a5cd2c3` (stale since `af08a59d`, 2026-10-22); no product version | Implemented and verified offline | Engineering complete | Sprint 1 (#45) | Not applicable (detached probe) | — | The script still exits 1 at T04 |
| T02 | Repeated-world reader and producer check expected the chunk7b subscriber set | Test/harness defect | Reader and producer check at `7a5cd2c3`; by source the 0.2.0–0.2.11 producer cannot pass its own check (diagnostic path only) | Implemented and verified offline | Engineering complete | Sprint 1 (#45) | Not run (no repeated-world native run) | — | Live subscribers at a settled repeated world not observed on a current build |
| T03 | UMM identity pins conflated with API compatibility | Test/harness defect | The validator at `7a5cd2c3`; no product version | Implemented and verified offline | Engineering complete | Sprint 1 (#45) | Not applicable (detached validator; no loader run natively) | — | Declared minimum 0.28.2 unassessed |
| T04 | Charge probe calls members whose shapes changed on 2026-11-03 | Test/harness defect | The probe at `7a5cd2c3` and every stack head (drift since `93d925e2`) | Open correction | Unresolved | Sprint 1 (#45), not repaired | Not applicable | — | Checks after the second drift point not assessed |
| T05 | Six-pair native ownership harness does not compile | Test/harness defect | Fails identically at `7a5cd2c3` (since `56b07ff0`, 2026-11-05) | Open correction | Unresolved | Sprint 1 (#45), not repaired | Not applicable | — | Three session-reading wrappers have no behavioural test |
| T06 | Repeated-world protocol script stops at an undefined variable | Test/harness defect | Fails identically at `7a5cd2c3` (since `3a7f8a84`, 2026-11-05) | Open correction | Unresolved | Sprint 1 (#45), not repaired | Not applicable | — | Checks after that line did not run |
| T07 | Area restore preflight adapter harness does not compile | Test/harness defect | Fails identically at `b62c59a8`; breaking commits not traced | Open correction | Unresolved | Sprint 2 (#46), not repaired | Not applicable | — | Facade also lacks `AreaPair.CarriedCopy` |
| T08 | Six-pair archive adapter harness does not compile | Test/harness defect | Fails identically at `b62c59a8`; breaking commits not traced | Open correction | Unresolved | Sprint 2 (#46), not repaired | Not applicable | — | — |
| T09 | `Test-Finalize2UpgradeReaders.ps1` stops at `PathNotFound` | Missing native/manual evidence (harness needs retained evidence) | Observed at `f835c503` and the Sprint 7 head; main not checked | Open investigation | Unresolved | Sprint 7 (#52), not repaired | Not applicable | — | Whether the r05 evidence can be recovered |
| S01 | Saving reported broken after the Ratnook door | Investigation | 0.2.4 report (cause unknown). The 0.2.3 occurrence had a different, explained cause fixed in 0.2.4. Later releases unknown | Investigation complete, issue unresolved | Unresolved | Sprint 2 (#46) | Not run | — | Whether, how and with what gate state a save was attempted in that session |
| S02 | After an area arrival the inactive-view check dismounts the pair | Product defect | Owner logs reporting 0.2.3 (Ratnook Hill) and 0.2.11 (Candlemere); neither DLL byte-matched; others unknown | Implemented and verified offline | Engineering complete | Sprint 2 (#46) | Not run | — | Integration order read from IL, not executed; native load placement within 0.1 m assumed |
| M01 | Entrance and exit orders miss the native acceleration offer | Product defect | 0.2.11 by source (the entrance carry exists since 0.2.5); not observed | Implemented and verified offline | Engineering complete | Sprint 3 (#47) | Not run | — | Harmony installation not run natively |
| M02 | Gamepad acceleration does not reach the mounted mover | Product defect | 0.2.11 by source; the 0.2.7 notes list gamepad running as unchanged; not observed | Implemented and verified offline | Engineering complete | Sprint 3 (#47) | Not run (no gamepad session ever) | — | — |
| M03 | Group pace uses the rider's speed | Product defect | 0.2.11 by source; the 0.2.7 notes list the rider-based walking pace; not observed | Implemented and verified offline | Engineering complete | Sprint 3 (#47) | Not run | — | — |
| M04 | An extra or stale green movement marker | Investigation | Reported before 0.2.3 (0.2.3 notes); exact version unknown | Engineering investigation complete; all identified product marker defects repaired | Engineering complete; original incident unresolved | Sprint 3 (#47) and closeout (#49) | Not run | Not viewed | The original incident is not attributable |
| M05 | Stick input for a mounted rider moves nothing, dismounts the pair and blocks remounting | Product defect (found 2026-11-16) | Sprint 3 base `1dffc3e0` (0.2.11 code) by source; never observed | Implemented and verified offline | Engineering complete | Sprint 3 (#47) | Not run (no gamepad session ever) | — | Native still faults on a command its `Run` did not admit; followers' speed cap reads the selected rider |
| C04 | Candlemere "wade through" fades but does not cross while mounted | Product defect | Owner report and a log reporting 0.2.11 (not byte-matched); others unknown | Implemented and verified offline | Engineering complete | Sprint 4 (#48) | Not run (owner's log absent) | — | The awake order in the owner's game |
| C01 | Scripted movement of a seated rider during combat waits for the scene timeout | Unsupported path | 0.2.8–0.2.11 (0.2.8 notes) | Explicitly unsupported, with its blocker | Unresolved (unsupported path documented) | Sprint 4 (#48) | Not run | — | Another design would need its own investigation |
| C02 | Rider-only scene placements of 3 m or less can be swallowed | Product defect | 0.2.8–0.2.11 (0.2.8 notes) | Implemented and verified offline | Engineering complete | Sprint 4 (#48) | Not run | — | Another handler inside a cutscene's open scope counts as the cutscene's |
| C03 | Restoration fallbacks need truthful feedback and clean retirement | Intentional limitation; one product defect (notice latch) | Bounds since 0.2.8; the notice latch at `e5b92474` by source | Intentional limit documented; one separate repair | Documented intentional behaviour; repair engineering complete | Sprint 4 (#48) | Not run | Not viewed (native warning) | — |
| R01 | Carried travel in a separate initiative slot gives no ranged contribution | Investigation | None established | Closed: not reproducible; no product change | Engineering complete | Sprint 5 (#50) | Not applicable (no gameplay change) | — | — |
| R02 | The accepted ranged rules need precise documentation | Intentional limitation | Rule effective in 0.2.10–0.2.11 (0.2.9 never applied it) | Intentional adaptation documented | Documented intentional behaviour | Sprint 5 (#50) | Not applicable | — | The native attack floor can absorb part of the penalty |
| A01 | A fresh same-target click just after attack completion gets inconsistent feedback | Product defect | Documented at 0.2.6; 0.2.11 by source; not observed | Implemented and verified offline | Engineering complete | Sprint 6 (#51) | Not run | — | — |
| A02 | Terminal feedback can describe a cancellation as "cannot act" | Product defect | 0.2.11 by source; not observed | Implemented and verified offline | Engineering complete | Sprint 6 (#51) | Not run | — | — |
| A03 | Which Serenity or Fascinate condition stopped the rider | Investigation | Report against 0.2.10 | Settled explanation; no product change | Engineering complete (explanation settled) | Sprint 6 (#51) | Not applicable (no gameplay change) | — | Royon's live buffs, saves and original log not observed |
| A04 | Real-time blocked orders wait; automatic proposals may churn | Intentional limitation (wait) and product defect (churn) | Churn: Sprint 5 base `772543e1` by source; the wait is native behaviour | Intentional wait documented; churn established and corrected (automatic only) | Documented intentional behaviour (wait); engineering complete (churn) | Sprint 6 (#51) | Not run | — | — |
| A05 | Any non-success automatic end marked its target unreachable | Product defect (found in A04) | Sprint 5 base by source; released versions not assessed | Implemented and verified offline | Engineering complete | Sprint 6 (#51) | Not run | — | — |
| U01 | After Charge the Move slot still shows usable distance | Product defect (UI) | 0.2.0–0.2.11 (0.2.0 notes) | Implemented and verified offline | Engineering complete | Sprint 7 (#52) | Not run | **Not viewed** | — |
| U02 | No selection glow on the public slot while its delivery ability targets | Product defect (UI) | 0.2.0–0.2.11 (0.2.0 notes) | Implemented and verified offline | Engineering complete | Sprint 7 (#52) | Not run | **Not viewed** | — |
| U03 | The Paladin class panel promises Weapon Bond upgrades to Divine Mount characters | Product defect (UI) | Documented in the [Divine Mount record](SPRINT8-PALADIN-MOUNT.md) before 0.2.0; releases not individually checked | Implemented and verified offline (character-screen class panel only) | Engineering complete | Sprint 7 (#52) | Not run | **Not viewed** | Per-level tooltips and the console screen unchanged |
| U04 | No mounted badge on the rider portrait; a folded mount's hover highlights nothing | Product defect (UI omission) | 0.2.0–0.2.11 (0.2.0 notes) | Implemented and verified offline | Engineering complete | Sprint 7 (#52) | Not run | **Not viewed** | Which collider the pointer picks over a pair; badge engine calls substituted offline |
| D01 | Current status fragmented across dated handoffs and stale entry points | Documentation | Records at `7a5cd2c3` | Implemented (documentation) | Engineering complete | Sprint 8 | Not applicable | — | — |
| D02 | Support, removal and rollback limits need one current description | Documentation | Records at `7a5cd2c3` | Implemented: [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md) | Engineering complete | Sprint 8 | Not applicable | — | — |
| D03 | Final handoff and completion accounting | Documentation | — | Delivered: [epic closeout](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md) | Engineering complete | Sprint 8 | Not applicable | — | — |

Counts: 26 epic work packages (T01–T03, S01–S02, M01–M04, C01–C04, R01–R02, A01–A04, U01–U04, D01–D03) and eight IDs
found during the epic (T04–T09, M05, A05). Unresolved: S01, M04's original incident, C01, T04–T08, T09.

### Coverage map (the epic's audit rows)

| Audit item | Owner | Epic disposition | Current disposition |
|---|---|---|---|
| Saving failure after the door | S01 | Investigate then repair if established | Investigation complete, issue unresolved |
| Arrival restore followed by inactive-view dismount | S02 | Fix | Implemented and verified offline |
| Extra or stale green movement marker | M04 | Investigate then repair if established | Engineering investigation complete; identified marker defects repaired; incident unattributed |
| Exit and entrance acceleration omission | M01 | Fix | Implemented and verified offline |
| Gamepad acceleration omission | M02 | Fix | Implemented and verified offline |
| Rider speed used for group pace | M03 | Fix with native floor preserved | Implemented and verified offline; native 30 ft floor unchanged |
| Candlemere wade-through fails while mounted | C04 | Investigate then repair the native interaction path | Implemented and verified offline |
| Scripted rider movement during combat | C01 | Implement safe supported command handling | Explicitly unsupported, with its blocker (no safe adapter exists under the present contract) |
| Short rider-only scene placement | C02 | Fix attributable placements | Implemented and verified offline |
| Combat begins while already staged | C03 | Retain post-combat restoration fallback | Intentional limit documented; notice repaired |
| Distance navigation and regroup bounds | C03 | Retain safe bounds and truthful cleanup | Intentional limit documented |
| Separate-initiative carried travel omitted | R01 | Correct attribution without riderless travel | Closed: not reproducible; no product change |
| Quarter-second movement tolerance | R02 | Retain and explain | Intentional adaptation documented |
| Native difficulty attack floor | R02 | Retain and explain | Intentional adaptation documented |
| Turn-based-only per-roll rule and no Run branch | R02 | Retain scope and timing | Intentional adaptation documented |
| One-frame same-target click feedback | A01 | Fix explicit new-click handling | Implemented and verified offline |
| Cancellation described as cannot act | A02 | Fix diagnostic cause selection | Implemented and verified offline |
| Unidentified Serenity or Fascinate trigger | A03 | Investigate available evidence | Settled explanation; no product change; live state unobserved |
| Healthy transient unstarted order retirement | A03 | Preserve one-drop behavior; investigate proven anomalies | One-drop behaviour preserved (an unstarted native Success still retires once, Sprint 6 A02 table); no proven anomaly was presented, so none was investigated |
| Blocked real-time rider command waiting | A04 | Retain native waiting and actor independence | Intentional wait documented |
| Possible automatic real-time proposal churn | A04 | Investigate; fix only if established | Established and corrected (automatic only) |
| Post-Charge unusable distance display | U01 | Fix presentation | Implemented and verified offline; not viewed |
| Missing targeting slot glow | U02 | Fix presentation | Implemented and verified offline; not viewed |
| Misleading Paladin upgrade panel | U03 | Fix presentation | Implemented and verified offline (class panel); not viewed |
| Missing mounted badge and hover affordance | U04 | Implement scoped UI feedback | Implemented and verified offline; not viewed |
| Two-pair charge probe against six-pair product | T01 | Fix stale test | Implemented and verified offline; T04 exposed |
| Old repeated-world subscriber expectations | T02 | Version reader; preserve old evidence | Implemented and verified offline |
| UMM identity and compatibility conflation | T03 | Separate profile and API results | Implemented and verified offline |
| Stale resume and fragmented known issues | D01 | Consolidate current state | Implemented (this ledger, the entry points) |
| Removal and rollback limitations | D02 | Document existing restrictions | Implemented ([CURRENT-SUPPORT.md](CURRENT-SUPPORT.md#removal-and-save-dependencies)) |
| Mount breadth and two versus six pair support | D02 | Document; no expanded feature support | Implemented ([CURRENT-SUPPORT.md](CURRENT-SUPPORT.md#riders-mounts-and-pairs)) |
| Cavalier barding enemy riders exotic mounts | D02 | Excluded feature expansion | Excluded; documented ([CURRENT-SUPPORT.md](CURRENT-SUPPORT.md#not-supported-and-excluded-from-the-epic)) |
| Already shipped major repair code | D01, D02 | Record identified fixes; no duplicate repair | Recorded below as superseded by identified fixes |
| Manual and native acceptance-only backlog | D01, D03 | Excluded from sprint commitments | Excluded; listed below and in the closeout |

### Review-discovered defects folded into accepted corrections

Defects found by a review of a sprint's own head, each corrected before that sprint was accepted. They belong to the
parent ID and have no row of their own.

| Parent | Review | Defect found | Corrected in |
|---|---|---|---|
| T02, T03 | Independent review of Sprint 1 | Five: inaccessible loader members still bound; the sweep read a build not under test; a local profile applied to any reference; culture-sensitive name comparison; unlabelled unsupported refusals | `a80a28b0` |
| T03 | Owner review of `fdd93249` | The umbrella did not forward its configuration; the loader matrix showed an incomplete inspection as a finished row | `e52a59f0` |
| S02 | Owner review R1 of `f71fabe7` | A deferred pair's arrival placement was lost across a save and load | `5d62a289` |
| M05 | Owner review of `5e27c9bd` (R1, R2) | Retiring the owned command could clear a foreign override; queue promotion bypassed the observer | `e98eac31` |
| M05 | Owner review of `25cbdbe2` | The dismount reported complete cleanup while a retirement was still owed | `f4369506`, `899fe393` |
| M05 | Owner review of `fe22563a` | World boundaries after the guard refused the pair's retained owner | `7fa9f74e`, `ee8cca11`, `56cae55f`, `1ad306ce` |
| M05 | Owner review of `119088ed` | The arrival's override exception accepted a foreign agent | `7a657838`, `29f987b8`, `f6e86858` |
| M01–M03 | Sprint 3 continuation | A throwing logger in the group-pace fault report escaped the hook | `b65977d8` |
| C04, C02, C03 | Review of `b30c24f8` (R1–R3) | Zone observations outlived the dismount; the first of two placements kept; the notice reached no player | `60ba91ae`, `3cbdf077`, `fb7f048d`, `b80534f9` |
| C03, C02 | Review of `263ac3ea` (R4–R6) | The notice was spent before a receiver existed; orientation-only placements; one fault latch for two hooks | `e62cad39` |
| C02, C04 | Review of `e1c0c7ad` (R7–R9) | A standalone rider turn swallowed; whole-pair writes missed; a throwing success logger truncated the loops | `ed83339c` |
| C02 | Review of `813d00a1` (R10) | Scene ownership read the global mode after the operation | `03685720` |
| M04 | Review of `b1a88b4e` (R1–R3) | Frame-scoped authority, a copied command and an inferred placement record | `cdfbe509` |
| A01, A02, A05 | Review of `2ae4cc8b` (findings 1–5) | Untyped command ends, fault ordering, durable capability cause, Run-token scope, the A01 counter | `ce9b1383` |
| A02 | Review of `fa533e97` (R18–R20) | Capability ends not tied to the exact native branch; the innermost Run token; a throwing scheduler | `1d0054f0` |
| A02 | Review of `dffd3e72` (R21, R22) | R21: native terminal captures rooted commands in thread-static fields (lifecycle defect). R22: "always releases" wording withdrawn | `0e1125fa` |
| A02 | Review of `95ae0b30` (R23, R24) | The CanNotAttack term could cross nested observations; the terminal-observation record | `7898b1a4` |
| U04 | Owner review of `292cd23c` (R1, R2) | The hover and badge used the wrong native seams and lifecycle | `92310c52` |
| U04 | Owner review of `e7720f39` (R3, R4) | A failed hover release or badge removal was lost | `6ff46a2a` |
| U04 | Owner review of `89ace80f` (R5) | One pair's cleanup cleared another pair's active hover redirect | `e54009e9` |

### Other recorded observations (no ID)

- **Designer `RunCommand`:** moving a seated rider outside any scene cannot complete while mounted. An unsupported
  path in the same class as C01, found by the Sprint 3 closeout's marker audit; unexercised.
- **Baseline-identical failures:** `Test-Harness.ps1` fails 4 of 281 at Sprint 7 and at the Sprint 6 base (two need a
  historical package zip, one source pin absent at the base, one package-size check);
  `Test-Chunk7CapitalArrivalEvidence.ps1` fails 3 of 165, which need native evidence for batches 196–198. Not
  harness defects established by this epic; not investigated. **Corrected 2026-11-19 (release 0.2.12):** the package-size failure was not unrelated to the epic. Its DLL grew
  from 9,352,704 bytes (0.2.11) past the validator's 9 MiB DLL entry bound (9,488,896 bytes), which blocked packaging.
  The owner raised the bound to 10 MiB for release 0.2.12; the other three failures are unchanged.

### After release 0.2.12

| ID | Report | Disposition | Native |
|---|---|---|---|
| X01 | 0.2.12 is `Not loaded` beside Tabletop Rules Expansion (`KingmakerGunslinger`) 0.0.191; a save using the Mounted Combat toggle `3c81f5a02be74d96a4e07d1c92b6f385` then fails to load (owner log `output_log(20261120-043652).txt`) | **Repaired; released in 0.2.13** (PR #55, merged as `a4d25e45`). Cause: the M04 closeout's `ClickUnitHandler.OnClick` transpiler (`cdfbe509`) replaced a native `Run` that the other mod also rewrites; whichever ran second threw. The transpiler now guards the submission instead. Composition repaired and verified offline with both released binaries ([handoff](GUNSLINGER-CLICK-COMPAT-REPAIR-HANDOFF.md)) | Native `mod-load-smoke` beside the installed 0.0.140: game-side PASS, both `Active`, `failure=none` (harness FAIL only on the UMM 0.33.0/0.33.2 platform pin). Not run with 0.0.191; the owner's save recovery and gameplay are not verified |

### After release 0.2.13

Engineering record: [CITY-AND-PRONE-HANDOFF.md](CITY-AND-PRONE-HANDOFF.md) (PR #57, merged as `4e5c9c0c`).

| ID | Report | Disposition | Native |
|---|---|---|---|
| X02 | The Mount / Dismount shortcut disappears from, or moves away from, the slot the player chose: after the capital, the reserve roster, saves there, or temporary companion unavailability | **Repaired; released in 0.2.14.** One production lifecycle (`MountedToolbarLifecycle`) keeps each rider's arrangement through temporary absence and respects moves, removals, occupied slots and companion replacement. Save format unchanged. | Startup only: every patch installs (`pr57-city-prone-smoke-1`; the release DLL's own smoke is in the 0.2.14 notes). Behaviour NOT RUN: owner playtests 1–2 in the handoff. |
| X03 | A companion's animal companion cannot be selected in the capital's Inventory and Character screens | **Repaired; released in 0.2.14.** A capital-only UI projection of each listed owner's own persistent pet into native `GetGroup`; nothing else changes. | Startup only (the `GetGroup` patch installs). Behaviour NOT RUN: owner playtest 3. |
| X04 | A mounted ranged character cannot attack a prone target | **Partly repaired; released in 0.2.14.** The identified rejection of an explicit mounted attack on a living target that was already unconscious is corrected in source. The original prone-target report remains unreproduced; whether the reported target was conscious and prone or already unconscious remains unknown. **Open.** | NOT RUN: owner playtests 4–5. |

### After release 0.2.14

Engineering record: [SAVE-AND-DOOR-LOCK-HANDOFF.md](SAVE-AND-DOOR-LOCK-HANDOFF.md) (PR #59, merged as `d409bffc`).

| ID | Report | Disposition | Native |
|---|---|---|---|
| X05 | After mounted ranged combat, quicksaves keep waiting ("Saving after current actions and projectiles finish") and fail, "projectiles appear to keep firing", and doors cannot be opened (owner log `output_log(20261122-180838).txt`, sha256 `8b0e422f…`) | **Diagnosed in part; diagnostics and accurate messages released in 0.2.15. Cause not established; not repaired. Open.** All four requests entered the deferred wait (readiness false at each activation). Three failed the wait's world/mode/loading check, misreported as "the native world changed" (a disallowed mode or manual loading screen on the ordinary path). The fourth was still waiting at the end of the log. The save wait refuses ordinary command starts, which explains the door symptom from source. 0.2.15 names the blocking owner and refusing condition of every held save and the real end of each wait. The readiness check, the hold and the 30 s limit are unchanged (readiness proven equivalent to 0.2.14 over 1,024 states). Needs an owner log with 0.2.15 or a disposable save. | NOT RUN (no startup either). Owner reproduction steps in the handoff, section 11. |

## Reconciliation history (superseded by the current status above)

The bullets below are the ledger header as it stood at the end of Sprint 7, kept for history. "Every row of Sprint 8
is a carried-forward planning finding" was true until Sprint 8 reconciled D01–D03.

- **Last updated:** 2026-11-19, Sprint 7 (U01-U04 implemented and verified offline as presentation projections; new T09 recorded), after 2026-11-18, Sprint 6 (A01, A02 and A04 corrected; A03 settled; new A05 corrected), after 2026-11-17, Sprint 5 (R01 closed as not reproducible, R02 documented), after the Sprint 3 closeout (M04 destination-marker ownership) and its review of `b1a88b4e` (R1–R3), after the review of `813d00a1` (R10 C02 scene ownership of the exact operation, captured at
  entry: a live background cutscene's commands and a TranslocateUnit inside a cutscene's scope), after the review of
  `e1c0c7ad` (R7 C02 standalone rider turn, R8 C02 whole-pair writes from
  CommandTeleportParty's proven target set, R9 contained success logging in the C02 and C04 loops), after the review
  of `263ac3ea` (R4 C03 notice spent only on receipt and never during a load, R5 C02 orientation-only placements, R6
  separate C02 and C04 fault reports), after the review repair of
  `b30c24f8` (R1 C04 observation lifetime, R2 C02 final placement, R3 C03 notice delivery; C01 wording narrowed),
  after Sprint 4 (C04, C01, C02, C03 reconciled and
  dispositioned). Branch
  `codex/kmc-known-issues-sprint4`, draft PR stacked on `codex/kmc-known-issues-sprint3`, from the accepted Sprint 3
  head `e5b92474`. Sprint 3's last change before that: the owner review of `119088ed` (the registry, discard and load-session correction
  accepted; the arrival's override-only exception now requires the override to be the pair's own retained agent),
  after the owner review of `fe22563a` (the lifecycle correction accepted; the operations after
  the world-boundary guard corrected), after the owner review of `25cbdbe2` (R2 accepted; the lifecycle around R1's
  retained cleanup corrected), after the review of `5e27c9bd` (R1 and R2 corrected in M05's ownership code) and the
  continuation (M05 repaired offline, the fault report, C04 recorded) and Sprint 3 (M01–M04). Branch `codex/kmc-known-issues-sprint3`, draft PR #47,
  stacked on Sprint 2 (branch `codex/kmc-known-issues-sprint2`, draft PR #46, reviewed head `1dffc3e0`), which is
  stacked on Sprint 1 (branch `codex/kmc-known-issues-sprint1`, draft PR #45).
- **Sprint records:** [Sprint 1](KNOWN-ISSUES-SPRINT1-HANDOFF.md), [Sprint 2](KNOWN-ISSUES-SPRINT2-HANDOFF.md),
  [Sprint 3](KNOWN-ISSUES-SPRINT3-HANDOFF.md), [Sprint 4](KNOWN-ISSUES-SPRINT4-HANDOFF.md),
  [Sprint 3 closeout](KNOWN-ISSUES-SPRINT3-CLOSEOUT.md), [Sprint 5](KNOWN-ISSUES-SPRINT5-HANDOFF.md),
  [Sprint 6](KNOWN-ISSUES-SPRINT6-HANDOFF.md), [Sprint 7](KNOWN-ISSUES-SPRINT7-HANDOFF.md).
- **Sprint 7 (2026-11-19):** U01 (no usable distance or step shown after forced charge travel), U02 (the exact public
  slot drawn selected while its delivery targets), U03 (the class panel omits weapon-bond upgrades a character's bond
  never receives) and U04 (a mounted badge on the rider's portrait; a folded mount's hover highlights its rider's
  entry) implemented and verified offline as presentation projections. New T09 (a reader harness needs retained native
  evidence absent here). Branch `codex/kmc-known-issues-sprint7`, stacked on Sprint 6 (PR #51).
- **Sprint 6 (2026-11-18):** A01 and A02 implemented and verified offline; A03 settled (Fascinate is Dazed, read by
  the current reader; Serenity adds no condition); A04 established and corrected for automatic proposals; new A05 (a
  capability-ended automatic command marked its target unreachable) corrected. Branch `codex/kmc-known-issues-sprint6`,
  stacked on Sprint 5 (PR #50).
- **Sprint 5 (2026-11-17):** R01 closed as not reproducible at the accepted base: under the qualified paired authority
  a mounted pair's mount never moves on its own initiative slot, so a mount's own record only ever holds riderless or
  pre-mount travel. No product change. R02 closed as intentional adaptation documented. Branch
  `codex/kmc-known-issues-sprint5`, stacked on the Sprint 3 closeout (PR #49).
- **Sprint 3 closeout (2026-11-17):** M04 is closed as "engineering investigation complete; all identified product
  marker defects repaired". The original incident is not attributable; native and manual verification remain.
  Sprint 3 engineering is closed. Branch `codex/kmc-known-issues-sprint3-closeout`, stacked on Sprint 4. After the
  review of `b1a88b4e`, the cannot-attack routing is bound to its one native call site, runs the original native
  command, and placements are recorded at `AddPointer` (`cdfbe509`).
- **Sprint 3 accepted (owner, 2026-11-16, at `e5b92474`):** a review checkpoint of offline work. M01–M03 and M05 are
  implemented and verified offline, and their native acceptance is still not run. M04's original report stays
  unresolved; S01 and T04–T08 keep their dispositions. The acceptance does not mark every Sprint 3 report resolved.
- **Reconciled so far:** T01–T09, S01, S02, M01–M05, C01–C04, R01, R02, A01–A05 and U01–U04. Every row of Sprint 8 is a *carried-forward planning
  finding*: it is copied from the epic's audit of 2026-11-15 and has not been rechecked against the source since.
  A sprint reconciles its own rows when it starts.
- **Native status is a separate column.** Nothing in this ledger was run in the game by Sprints 1 to 7, and no Sprint 7 screen was viewed.
- Dated handoffs and old failure records are history and are not rewritten. This ledger links to them.

## Dispositions

| Status | Meaning |
|---|---|
| Open correction | An established defect, or an approved gap, still needs its change. |
| Open investigation | Reported behaviour whose cause or safe correction is not established. |
| Implemented and verified offline | The change exists and focused automated checks ran. Native evidence is reported separately. |
| Investigation complete, issue unresolved | The diagnosis pass is done; the report stays open with a precise evidence gap. |
| Intentional limit documented | An existing design or safety boundary is kept and explained. |
| Manual/native verification only, excluded from this epic | The only outstanding task is play or native acceptance. |
| Superseded by an identified fix | A later source correction already addresses the old finding. |

A row whose planned closure is "intentional limit documented" stays an open correction until its documentation
or feedback work is actually done.

## Sprint 1 — validation tools (reconciled 2026-11-15)

| ID | Purpose | Classification | Status | Source | Implementation and offline evidence | Native |
|---|---|---|---|---|---|---|
| T01 | The charge ownership probe asserted a two-pair capacity; production admits six. The assertion threw and hid the rest of the script. | Stale test | **Implemented and verified offline** (the capacity probe). The script as a whole still exits 1 at T04. | [SAVE-LOAD handoff](SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md) (validation table); epic T01 | Commit `07edd2fe`. `scripts/Test-ChargeOwnershipContracts.ps1`: 71 checks run (29 before). A build with capacity 5 fails the probe. | Not applicable: detached probe |
| T02 | The repeated-world evidence reader, and the producer's own check, required the chunk7b subscriber set (13 lifecycle interfaces, 15 rows). | Stale reader and producer | **Implemented and verified offline** (reader repaired and verified offline) | [RATNOOK handoff](RATNOOK-DOOR-REPAIR-HANDOFF.md) (review item P3); epic T02 | Commits `3c64128a`, `a80a28b0`. Reader tests 220 pass; compiled-contract check 21 pass; 5 component tests. | **Not run.** No repeated-world native qualification was made or claimed. |
| T03 | The assembly checks pinned UMM 0.33.2 in line, so a genuine 0.33.0 loader reported three failures that said nothing about API compatibility. | Validator conflation | **Implemented and verified offline** | [SAVE-LOAD handoff](SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md), [RATNOOK handoff](RATNOOK-DOOR-REPAIR-HANDOFF.md) (assembly-contract failures); epic T03 | Commits `42fdf52d`, `a80a28b0`, `e52a59f0`. Kingmaker assembly contracts 810 pass, 0 fail (801/3 before); loader checks 22 pass, 1 unassessed; checker configuration 6 pass. | **Not run** on any loader by this sprint |
| T04 | Behind the T01 assertion, the same script calls production members by reflection with shapes that changed on 2026-11-03. | Stale test (probe drift), not a product failure as far as established | **Open correction** | Exposed by T01 on 2026-11-15 | See "T04" below. Not repaired in Sprint 1 by the sprint's own rule. | Not applicable |
| T05 | `scripts/chunk7/Test-Chunk7SixPairNativeOwnership.ps1` no longer compiles its source list. | Stale test harness | **Open correction** | Found as a T01 neighbour on 2026-11-15 | See "T05" below. Fails identically on main `7a5cd2c3`. | Not applicable |
| T06 | `scripts/chunk7/Test-Chunk7RepeatedWorldProtocol.ps1` stops at an undefined variable. | Stale test harness | **Open correction** | Found as a T02 neighbour on 2026-11-15 | See "T06" below. Fails identically on main `7a5cd2c3`. | Not applicable |

T04–T06 are not among the epic's 25 work packages. They were found while running Sprint 1's own checks and
are recorded here so they are not lost. No other script was audited: the offline scripts outside the Fast tier
were not surveyed, and more of them may be stale. Sprint 2 found two more of the same kind (T07, T08) and left
T04–T06 as they were: none of them blocks an S01 or S02 check.

### T01 — what the probe now establishes

- Production admission, the archive format and the registry all declare six, and the probe asserts the
  reviewed literal six, so a capacity change fails until it is reviewed.
- The capacity each compiled service wrapper passes to the registry (`TryReserveOwnership`,
  `NativeAdmissionRefusal`, `CaptureNativeRequest`) is read from its IL and must be that same six.
- In an isolated registry built by the production service constructor, with compiled services as boundaries and
  genuine `UnitEntityData` containers: six pairs reserve and commit; overlap in either role is refused while
  capacity remains; the seventh is refused with no owner, request or actor index and the six registrations
  unchanged; the production `Dismount` retires one; the refused pair takes the released capacity; the bound
  holds again.
- **Limit:** the three wrappers read `Kingmaker.Game`, whose type initializer needs the Unity player, so they
  are not executed detached. The harness that would exercise them with a facade session is T05.

### T02 — what the contract now is

- Two reviewed revisions of the KMC-owned global `EventBus` subscribers in a settled repeated world:
  - **r1**, historical and implicit: 13 lifecycle interfaces and the two control interfaces; Horse owners are the
    Medium Horse and the first Small Halfling Horse profile. It describes `chunk7-repeated-worlds-v1/v2` evidence
    of chunk7b previews up to .99.
  - **r2**, declared by `chunk7-repeated-worlds-v3` producers (builds from this change on; product version 0.2.11 or later): 15 lifecycle interfaces, controls,
    the defence service and the tack service; every profile that mounts the KMC Horse blueprint (13).
- chunk7b preview.100–.103 and releases 0.2.0–0.2.11 as shipped are **unsupported**: the set changed there
  without a revision (inside preview.100 and again inside preview.103), their producer still emits v2, and it
  could not pass its own check.
- Selection is by evidence contract and producer identity. The expected set is never derived from the inventory.
  Names are compared ordinally, character for character.
- **Also found and fixed inside T02:** the reader's Horse-owner profile list predated the Gnome, female and
  build-variant Small profiles (2026-11-01/02). Its own test had been failing on that since then.
- **Production change:** diagnostic evidence protocol only (`Diagnostics/Chunk7ReplaySubscriberContract.cs`, and
  `Chunk7PersistenceScenario.Replay.cs` now emits v3 with the revision name and checks against it). No gameplay
  code and no save format changed. The product binary changes, as any diagnostic change does.
- **Not established:** which subscribers are live at a settled repeated world is read from source
  (`CompositionRoot`) and from compiled metadata. It has not been observed in the game on a current build.
- **Contract boundary:** a Paladin's Divine Mount fact (`DivineMountSpellResistance`) is a global subscriber too.
  r2 does not include it, because the repeated-world fixtures hold no Paladin; evidence from a party that has one
  is refused as a foreign subscriber, not silently accepted.

### T03 — loader matrix on the development machine, 2026-11-15

| Loader | Genuine binary | Identity | API (binding surface, callbacks, logger, UI, 20 compiled member references) | Native |
|---|---|---|---|---|
| 0.28.2 (declared minimum) | **Unavailable** | Unassessed | Unassessed | Not run by this sprint |
| 0.33.0 | Installed Kingmaker loader | PASS under `umm-0.33.0` | PASS | Not run by this sprint |
| 0.33.2 | NuGet copy held outside the repository | PASS under `umm-0.33.2` | PASS | Not run by this sprint |
| 0.32.4, 0.32.3, 0.31.1, 0.27.12 | NuGet copies held outside the repository | Not asserted (no reviewed profile) | PASS | Not run |

- The declared minimum stays **0.28.2** (`Info.json`). It was not raised and it was not assessed: no genuine
  0.28.2 binary is available, and API compatibility at other versions does not establish it.
- The native runner's own loader pin (`planning/ENVIRONMENT-FINGERPRINT.json`, 0.33.2) is unchanged. A native
  transaction on a 0.33.0 machine is a separate decision and was not attempted.
- Profiles and provenance: `planning/UMM-LOADER-PROFILES.json`.

### T04 — charge ownership probe drift behind the capacity assertion

- **Met again on 2026-11-16 at `899fe393`** (the script reads ownership code the Sprint 3 lifecycle correction
  touches): unchanged, 71 checks pass and then the same exception. Still open.
- **Exact result (committed state):** `scripts/Test-ChargeOwnershipContracts.ps1` passes 71 checks, then
  `TargetParameterCountException: Parameter count mismatch` at its first `AdmitChargeCommand` call. Exit 1.
- **Cause, first point:** `MountedCombatController.AdmitChargeCommand` gained an optional third parameter
  (`MountedChargeRequestRoute route`) in `93d925e2` (2026-11-03). The probe still passes two arguments by
  reflection. The script had been dead at the capacity assertion since `af08a59d` (2026-10-22), so nobody saw it.
- **Second point, from an uncommitted scratch run** that supplied the declared default route: 128 checks pass,
  then `NullReferenceException` where the probe looks up `MountedChargeLease.RequireChargeBuffSurface`, which
  `2c49bd13` (2026-11-03) removed.
- **Not assessed:** anything after the second point. This is a chain of probe drift of unknown length.
- **Classification:** test drift against the Sprint 7 charge changes. No product failure is established, and
  none is ruled out for the checks that have not run.
- **Next step:** bring the probe's reflected calls back in line with the charge code of 2026-11-03, keeping every
  assertion, and classify whatever then fails.

### T05 — six-pair native ownership harness does not compile

- `scripts/chunk7/Test-Chunk7SixPairNativeOwnership.ps1` compiles the real ownership partial with a facade
  session. It fails with `CS0234`: `MountedArchiveData.cs` uses `Domain.MountedControlSlotPolicy`
  (`56b07ff0`, 2026-11-05), which is not in the script's source list.
- Until it runs, the three session-reading ownership wrappers have no behavioural test (see T01's limit).

### T06 — repeated-world protocol script stops at an undefined variable

- `scripts/chunk7/Test-Chunk7RepeatedWorldProtocol.ps1` passes its 27 compiled protocol checks, then evaluates
  one line of `Test-RuntimeGameResult.ps1` that now reads `$f2Upgrade` (`3a7f8a84`, 2026-11-05), which the script
  does not define.
- The checks after that line did not run. They include two source pins on the repeated-world producer; the lines
  they pin were left intact by T02.

## Sprint 2 — saving report and post-load dismount (reconciled 2026-11-15)

Full record: [KNOWN-ISSUES-SPRINT2-HANDOFF.md](KNOWN-ISSUES-SPRINT2-HANDOFF.md).

| ID | Symptom | Classification | Status | Source | Implementation and offline evidence | Native |
|---|---|---|---|---|---|---|
| S01 | Saving reported broken after the Ratnook door. | Reported defect. One occurrence explained and already superseded; the other has no established cause. | **Investigation complete, issue unresolved** | [SAVE-LOAD handoff](SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md); epic S01 | Diagnostics only (see "S01" below). No save decision, guard, latch or retry changed. Persistence contracts 223 pass (214 before). | **Not run.** No log exists from a build with the save diagnostics. |
| S02 | After an area arrival the view check finds inactive views and the pair dismounts. | Established occurrence (one, a world-map arrival) | **Implemented and verified offline** | [RATNOOK handoff](RATNOOK-DOOR-REPAIR-HANDOFF.md) (arrival view check); epic S02 | A pair whose exact views are not active is not attached at arrival and restores through the retained-intent retry. 14 component tests (11 fail with the prior decision behaviour); contract script 20 pass on the reviewed build and 11 pass, 9 fail on the base build; one independent review, corrected. Owner review R1 (2026-11-16): a deferred pair's arrival placement now survives a save and its load (retained-intent record version 2); 7 more component tests (4 fail with the reviewed row conversion); contract script 26 pass on this build and 20 pass, 6 fail on the reviewed build; one independent review, corrected. | **Not run.** Not native-verified. |
| T07 | `scripts/chunk7/Test-Chunk7AreaRestorePreflightAdapter.ps1` does not compile its source list. | Stale test harness | **Open correction** | Found as an S02 neighbour on 2026-11-15 | See "T07 and T08" below. Fails identically on the Sprint 2 base `b62c59a8`. | Not applicable |
| T08 | `scripts/chunk7/Test-Chunk7SixPairArchiveAdapter.ps1` does not compile. | Stale test harness | **Open correction** | Found as an S02 neighbour on 2026-11-15 | See "T07 and T08" below. Fails identically on the Sprint 2 base `b62c59a8`. | Not applicable |

### S01 — what is established, and what is not

- **0.2.3 occurrence: explained, and superseded by an identified fix.** In the owner's 0.2.3 log the Ratnook door
  set the area-restore failure latch ("Native area placement did not complete before the mounted restore
  boundary"). That latch makes the mod's term of the native save gate refuse, so `IsSaveAllowed` was false until a
  load. 0.2.4 (the Ratnook door repair) stopped this door from setting the latch. The latch itself
  is protective and unchanged.
- **0.2.4 report: cause not established.** That session's log holds no save line of any kind. Its last lines are
  four refused *loads* around the native save/load window opening, which is the load defect 0.2.5 repaired.
  - "No save line" is not "no save ran": at 0.2.4 a request, an admission refusal, a queued operation and an
    operation waiting before the native header each wrote nothing.
  - Earliest failure boundary consistent with the log: the action never reached `Game.SaveGame`. A refusal at
    admission cannot be told apart from it, because 0.2.4 logged neither.
  - Ruled out: a save that waited for effects, reached the native header or was reported failed (each wrote a line
    at that version); the 0.2.3 latch; a refused load leaving native save state behind.
  - Not established, and not ruled out: stranded save ownership (an active save scope or an admission fence left
    set); a save queued behind another loading process; a save that began and stuck in the native wait before the
    header. A read of the current source found no path that strands a scope or fence, which is not proof for that
    session. (Corrected 2026-11-16: the first version of this entry listed a stranded scope or fence as ruled out.)
  - What would distinguish them: a later save attempt on a build that logs it. No later attempt is established in
    the 0.2.4 log, so the absence of a failure report there is not evidence.
- **The missing observation:** whether a save was attempted in that session, by which action (Esc-menu Save,
  quicksave or an expected autosave), and the state of the native gate and of the one-save setting at that moment.
- **For the owner:** the Last Azlanti preserver mod is active in both sessions. It bears on the report only if
  both of these hold, and neither is known: the campaign runs in the native one-save mode, and the action that
  failed was a manual save or a quicksave. In that mode native refuses those two with or without this mod; it
  does not refuse an autosave to the campaign's own slot. No save or settings file was opened.
- **Diagnostics added, observation only:** a line when this mod's restoration state starts and stops refusing
  saving; the native outcome of each request (including the silent one-save refusals and the disabled-autosave
  skip) and what a queued save waits behind; a line when a save operation begins. They are not a fix.
- **To close:** the owner's answer on the save action and the one-save mode, or a log of a recurrence on a build
  with these lines, ideally with a second save attempt after the first one fails.

### S02 — what changed and what it rests on

- **Order established:** the arrival restoration runs inside the native load; a party arriving from the world
  map still has inactive views there, because world-map mode puts every unit to sleep and no game mode ticks during
  a load; the first invariant check after the loader goes idle found them inactive. The native side is read from
  the installed assembly's IL.
- **Inference, not observation:** that sleeping was the reason in the recorded session. The correction acts on
  view activity itself and does not depend on it.
- **Correction:** restoration reads the exact current views before attaching, and attaches if and only if the
  mounted invariant would keep the pair. A pair whose views are not active waits, unattached, in the existing
  retained-intent custody and attaches once after ordinary play has settled. The mounted invariant is unchanged;
  no allowance exists for a mounted pair.
- **Bounded:** the existing ten seconds of settled play, then the pair is released with a message and stays
  dismounted. Nothing is latched and nothing is owned while waiting.
- **Also changed:** an explicit player dismount now releases that pair's retained intent at once.
- **Persistence effect:** the after-entry autosave of a world-map arrival records the pair as retained intent
  rather than as mounted; loading it restores the pair in ordinary play.
- **Format (owner review R1, 2026-11-16):** the `kmc-retained-intent` member has a version 2 text: version 1 plus
  one arrival row for each pair deferred at its arrival placement. It is written only when the save holds such a
  pair; every other save writes the version 1 bytes as before, and every existing save reads as before.
  - A deferred pair still standing where arrival placed it restores the same way whether the player continues
    or loads a save written before the remount. A relocated pair, ordinary retained intent and every version 1
    record keep the 6 m rule.
  - A malformed or unsupported version 2 text is refused whole; its pairs load dismounted.
  - A build older than this change (0.2.3 to 0.2.11) refuses a version 2 text by its header, and every pair in
    it loads dismounted there. That matters only if the mod is downgraded on such a save.
  - Size-limit exception: if the arrival rows would take the text past the member's bound (4096 characters), the
    writer falls back to the version 1 text instead of losing the record. Every pair of that save then loads
    without an arrival placement and gets the ordinary 6 m rule. (Recorded here at the Sprint 3 intake,
    2026-11-16; the behaviour is Sprint 2's and is unchanged.)
- **Limits:**
  - A fight that starts before the remount cancels it (existing rule for world-map custody); the pair fights
    dismounted.
  - A deferred pair keeps the arrival formation's spacing only while neither unit has moved more than 0.1 m;
    otherwise the retry's 6 m rule applies.
  - That a native load puts each unit back where the save holds it, within that tolerance, is assumed.
  - The integration sequence is checked from IL, not executed. Nothing ran in the game.

### T07 and T08 — two more harnesses that do not compile

- `Test-Chunk7AreaRestorePreflightAdapter.ps1` compiles the real area persistence partial against a facade. It
  fails with `CS0103` (`MountedAreaControlRule` is not in its source list) and with facade members the transfer
  no longer matches (`Lifecycle`, `WorldMap`).
- `Test-Chunk7SixPairArchiveAdapter.ps1` fails with `CS0234` (`Domain.MountedControlSlotPolicy`, as T05) and a
  stale `Report` call.
- Both were failing before Sprint 2 and were not needed for an S01 or S02 check, so they were left as found.
  Which commits broke them was not traced.
- Since the owner-review correction of 2026-11-16 the first one's facade also lacks `AreaPair.CarriedCopy`, the
  method the three carried-row copies now go through. That is one more member to add when T07 is repaired.
- Not a harness defect, for the record: `Test-Chunk7CapitalArrivalEvidence.ps1` reports 162 pass, 3 fail; the
  three require native evidence for batches 196–198 that is not on this machine.

## Sprint 3 — mounted pace and the marker report (reconciled 2026-11-16)

Full record: [KNOWN-ISSUES-SPRINT3-HANDOFF.md](KNOWN-ISSUES-SPRINT3-HANDOFF.md).

| ID | Symptom | Classification | Status | Source | Implementation and offline evidence | Native |
|---|---|---|---|---|---|---|
| M01 | Entrance and exit orders do not receive the native acceleration offer. | Source-established omission | **Implemented and verified offline** | [HORSE-GROUP-PACE handoff](HORSE-GROUP-PACE-REPAIR-HANDOFF.md); epic M01 | Commit `c5b52387`. The mount's carried member command is classified beside the routed ground order and gets the same native `Accelerate()` call. 8 component tests; contract script 27 pass (the base build fails its 16 product checks); mutants A, H and N fail. | **Not run.** |
| M02 | Gamepad stick acceleration does not reach the mounted mover. | Source-established omission | **Implemented and verified offline, in two parts.** A following pair: the native follower pass reaches its mount (`c5b52387`). A directly driven pair: native accelerates and decelerates the mount's own continuous command once M05 lets it run (`99b632f2`). | [HORSE-GROUP-PACE handoff](HORSE-GROUP-PACE-REPAIR-HANDOFF.md); epic M02 | Commit `c5b52387`. The native follower pass is answered with the mount that executes the selected rider's routed order. 6 component tests; the production transpiler run on the installed `UpdateMovement`; mutants B, G and L fail. | **Not run.** No gamepad session has ever been run in this project. |
| M03 | Group pace is computed from the rider's speed, not the horse's. | Source-established mismatch | **Implemented and verified offline** | [HORSE-GROUP-PACE handoff](HORSE-GROUP-PACE-REPAIR-HANDOFF.md); epic M03 | Commit `c5b52387`. The native speed selection reads each pair's mount, each actor once; the native selector, `Min`, 30 ft floor and selection are untouched. 8 component tests; the production transpiler run on the installed `MoveSelectedUnitsToPoint`; mutants C, F and M fail. | **Not run.** |
| M04 | An extra or stale green movement marker. | Reported defect, cause unknown | **Engineering investigation complete; all identified product marker defects repaired** (Sprint 3 closeout). The original incident is not attributable; native and manual verification only. | [EXPLORATION handoff](EXPLORATION-REPAIR-HANDOFF.md); epic M04; [closeout](KNOWN-ISSUES-SPRINT3-CLOSEOUT.md) | Sprint 3 `26b4169e` (entrance, regroup); closeout `c64c695f` (door approach marker; the cannot-attack click approach given to the pair; ownership classification and bounded observation) and review `cdfbe509` (call-site authority, the original native command, the `AddPointer` placement record). 26 component tests; `Test-MarkerOwnershipContracts.ps1` 18/0; six closure and eleven review mutations caught. | **Not run.** |
| M05 | Stick input for a mounted rider moves nothing, and by source dismounts the pair and blocks remounting. | Source-established defect, never observed | **Implemented and verified offline** | Found while reconciling M02 on 2026-11-16; owner put direct gamepad control in scope the same day | Commits `99b632f2`; after the owner's review of `5e27c9bd`, `e98eac31` (R1, R2); after the review of `25cbdbe2`, `f4369506` and `899fe393` (the lifecycle around retained cleanup); after the review of `fe22563a`, `7fa9f74e`, `ee8cca11`, `56cae55f` and `1ad306ce` (world boundaries after the guard); after the review of `119088ed`, `7a657838`, `29f987b8` and `f6e86858` (who owns the mount's override at the arrival). The stick drives the pair's mount; the mount's continuous agent is owned by the exact command that installed it; retiring that command never clears a foreign override. See "M05" below. 47 component tests; contract script 59 / 0 (head `5e27c9bd` fails 12 of its 43, both required reproductions among them; head `25cbdbe2` fails 8 of 52, the disable and runtime-error regressions among them; head `fe22563a` fails 5 of 58, the registered-owner area, load and persistent-cleanup regressions among them; head `119088ed` fails 2 of the 59, its classification taking foreign overrides for the pair's own agent); 27 mutants of the R1/R2 corrections, 35 of the lifecycle correction and 37 of the world-boundary correction fail. | **Not run.** No gamepad session has ever been run in this project. |

### M01–M03 — what changed and what it rests on

- **One idea:** three native passes read the selected units for pace, while a mounted pair's order runs on its
  unselected mount. Each pass is now answered with the unit that moves. Native still decides and applies
  everything: `Accelerate`, `CanAccelerate`, `Deaccelerate`, the group algorithm and its floor.
- **M01 ownership:** only the exact member command the entrance carry issued, for its group and pair generation,
  in the mount's Move slot, while the rider still holds its own member command of that group. The carrier is
  issued as before and bound only after its `Run`.
- **M02 boundary:** the gamepad's follower pass (every selected unit's `Commands.Move` gets `Accelerate()` or
  `Deaccelerate()` at each repath). Only the routed ground order of a selected rider is answered for. No state.
- **M03 inputs:** a mounted pair's rider is answered by its mount in the native minimum, real time only. The
  floor is the native 30 ft per round (3.048 m/s): a mover slower than that is bounded by its own speed and the
  party does not wait below the floor.
- **Limits:** the methods that gather these facts from the game are read from IL, not executed. The Harmony
  installation of the new patches was not run. Nothing ran in the game.

### M04 — what is established, and what is not

**Superseded by the [Sprint 3 closeout](KNOWN-ISSUES-SPRINT3-CLOSEOUT.md) (2026-11-17).** The closeout found and repaired two more
product marker defects, classified every native and product marker path, and closed M04 as engineering complete. The
paragraphs below are the Sprint 3 state.

- **The report's cause is not established.** Its original log is not available. Three later owner logs (0.2.3 to
  0.2.5) hold 119 mount pace summaries, no "stopped short" line, and one rider-held entrance command (0.2.3, the
  defect 0.2.5 repaired).
- **Native contract:** one pointer per unit, placed when that unit's stock agent completes a plan, shown while
  the unit holds a live marker move or a live `UnitAreaTransition`. A command is planned by the stock agent
  whether or not that agent is enabled, so a mounted rider's own commands are still planned and marked.
- **Established by source and repaired:**
  - A carried entrance approach showed two markers for one pair (the mount's and the rider's). The rider's
    placement is now skipped only while the mount executes the carrier of that same rider command, and a pointer
    the rider already had is retired when the carrier is admitted.
  - The scene regroup walk asked for a marker under the rider though no player ordered it. It asks for none now.
- **Kept:** a rider command nothing carries keeps its native marker; a live order that stopped short keeps its
  marker. Nothing is cleared globally and no command is completed or hidden.
- **Not established:** that either path is what the owner saw. The pre-0.2.5 entrance state is consistent with
  the report; no log ties the report to an entrance click.
- **The missing observation:** at the moment an extra marker is visible, the keys of the native pointer table,
  and for each key its position and the command that keeps it visible.
- **To close:** the owner's description of when the marker appears (after an exit click, after a scene, during an
  ordinary walk), or a log from a build that records the pointer table.

### M05 — the stick-driven mounted pair (repaired offline by the continuation, 2026-11-16)

- **The defect, by source:** with a mounted rider as the gamepad's driven unit, the mod rewrote the continuous
  command's executor to the mount, native `Run` then refused that command on the rider's own container, and the
  native layer dereferenced the missing command. The mount kept the continuous agent as an override nothing
  owned, the mounted invariant dismounted the pair, and that leftover override refused every later mount attempt.
- **How it is known:** the `Run` refusal was executed offline on genuine engine objects; the rest is read from
  the installed assembly and the product. It has never been observed in play.
- **The correction:** the stick drives the pair's mount. Native initialises, runs, looks up, accelerates and ends
  the command on that one unit. The pair owns exactly the agent that command installed, for its pair generation
  and only while the command runs in the mount's Move slot; the invariant admits that and nothing else.
- **Nothing is left behind:** a command native `Run` did not admit is ended and its agent retired at once. The
  drive also ends when the party enters combat, when the rider is given a Move or Standard order of its own, and
  with the pair at dismount and disposal.
- **Same root, also corrected:** native takes a non-null override on the current unit to mean "stick-driven". A
  mounted rider always has one, so by source a mounted current unit received no ground order in gamepad mode.
  That read now looks at the mount.
- **Owner review of `5e27c9bd`, R1 (corrected):** native ends a continuous command by clearing the override of the
  executor's *current* view. Retiring the owned command could therefore clear an unrelated override on a replaced
  view, and a command abandoned after its agent was replaced later cleared the foreign override. Now a guard
  inside that native method lets a pair-bound command clear only its own agent on its own view; the command and
  its captured agent are retired separately; and the record is kept, with its exact command, view and agent,
  until the retirement is established, and retried.
- **Owner review of `5e27c9bd`, R2 (corrected):** a rider order admitted by native queue promotion bypassed the
  observer on the public `Run`. Admission is now observed after the internal `Run` that both routes reach.
  Enqueueing alone, a refused promotion, a Free order and another unit's order leave the stick movement.
- **Owner review of `25cbdbe2`, the lifecycle around R1 (corrected; R2 accepted):** the owner kept a failed
  retirement, but the dismount still reported complete cleanup, so a disable or a runtime-error cleanup could stop
  the pair updates that were its only retry. Now the dismount reports the retained cleanup. The disable, the
  runtime-error stop, shutdown, guard removal, owner settlement and boundary reuse all wait for it, and the root
  retries it before its enabled gate. Automatic attempts stay within three; explicit releases retry deliberately.
  Area, load and event boundaries are not failed by a stick-only obligation (kept by the owner at the review of
  `fe22563a`).
- **Owner review of `fe22563a`, world boundaries after the guard (corrected):** the guard passed, but the admission
  discard right after it refused the pair's retained owner, so area suspension latched a failed transfer and load
  housekeeping threw. Now the registry tells detached stick cleanup apart from what holds a world.
  - The discard accepts an owner that owes only that cleanup, and the owner stays registered with its actors and
    identity.
  - The arrival defers a pair still held this way, with its identity and arrival placement. It is restored once the
    owner retires, with the same identity, generation, session and campaign; until then the true reason is given.
  - A load sets such owners aside from the old session, so the new world imports its pairs under its own session.
  - Unrelated refusals keep the all-pair failure, and the capital rule still retires.
- **Owner review of `119088ed`, who owns the mount's override at the arrival (corrected):** the override-only
  exception took any installed override for the pair's own retained agent. A foreign agent on the captured view or on
  a replacement view changed a departing Fail into Defer, and gave a carried pair the cleanup placement. Now the
  exception needs two facts:
  - the mount's current view and override are the captured view and agent of a retirement still owed by the pair's
    exact earlier owner (identity, generation, rider, mount);
  - that override is the only refusal.

  Foreign overrides keep the established outcomes.
- **Deliberately preserved (owner decision, 2026-11-16):** a *stationary* rider Standard order runs on while the
  stick drives the mount, as it does beside a routed ground order. For one native unit the same push would end it
  through the Move/Standard pairing; that pairing is not mirrored onto the rider. Approaching rider orders are
  ended as before.
- **Not repaired:** native still faults on the command its `Run` did not admit, for a pair as for any unit; the
  followers' native speed cap and slow-down exemption still read the selected rider.
- **Open:** native acceptance. Nothing about gamepad play has ever been run in this project.

### The group-pace fault report (corrected by the continuation)

- A logger that threw inside a Sprint 3 hook's fault report escaped the hook, so the hook did not return its
  native fallback. The report now contains a failing logger (`b65977d8`). Executed check: an operational failure
  with a throwing logger; the reviewed head fails it with the logger's exception.

## Sprint 4 — scene continuity (reconciled 2026-11-17)

Order C04, C01, C02, C03 (owner amendment of 2026-11-16). Record: [Sprint 4 handoff](KNOWN-ISSUES-SPRINT4-HANDOFF.md).
Product-build commit `03685720` (after the review of `813d00a1`), clean Release DLL SHA-256
`7ce8f88a99aaa74d142e6835590d3051a9a48ce984589090b9407eb01efd59c8`, MVID `efdc12b6-0098-4027-ad49-f8c8f33851bc`.

| ID | Symptom | Classification | Status | Implementation and offline evidence | Native |
|---|---|---|---|---|---|
| C04 | At Candlemere Tower, "wade through" fades the screen but does not cross while mounted; dismounting by hand permits the crossing. | Mounted traversal defect | Implemented and verified offline | `ef264ff3`: a seated rider's script-zone membership is rechecked with native `ScriptZone.CheckUnit` when native did not check it. Review R1 (`60ba91ae`): observations belong to the mounting instance, so a remount is checked again. Review R9 (`ed83339c`): a throwing success logger never stops the remaining zones. `CandlemereCrossingTests` (8); `Test-CandlemereCrossingContracts.ps1` 11/0, reviewed `e1c0c7ad` build 9/2 | Not run; the owner's `output_log(9).txt` is not on this machine |
| C01 | A scripted movement command on a seated rider during combat waits for the scene timeout. | Unsupported command path | Explicitly unsupported, with its blocker | No change. Blocker: the existing scene-staging lifecycle cannot take an actively participating pair out of combat while preserving the restoration intent under its present contract. Fallback pinned and bounded by C02 | Not run |
| C02 | Rider-only scene placements of three metres or less can be swallowed. | Known omission | Implemented and verified offline | `eabbd8c8`: the three native placement operations attribute a rider-only write to the scene. Review R2 (`3cbdf077`): the scene's latest placement stands; a whole-pair placement clears a pending rider-only one. Review R5 (`e62cad39`): the installed operations' orientation writes are observed, so a same-endpoint placement's final orientation stands. Review R6: C02's hooks have their own fault report. Review R7 (`ed83339c`): a standalone rider-only turn is held at the rider's position and handed off once (R5's "starts nothing" was the swallowed outcome). Review R8: CommandTeleportParty's proven target set makes a whole-pair write even when one actor was already at its authored transform. Review R9: a throwing success logger never truncates the pairs of one operation. Review R10 (`03685720`): scene ownership is the exact operation's, captured at its entry (a live background player's `CommandMoveUnit` and `CommandTeleportParty`; `TranslocateUnit` inside a cutscene's parameter scope; the foreground rule unchanged); remaining limit: an action run by another handler inside a cutscene's open scope counts as the cutscene's. `ScenePlacementAttributionTests` (20); `Test-ScenePlacementAttributionContracts.ps1` 24/0, reviewed `813d00a1` build 21/3 | Not run |
| C03 | Restoration fallbacks need truthful feedback and clean retirement. | Intentional boundaries | Intentional limit documented; one separate repair | Limits unchanged and stated in the handoff. Repair `9a371910`: the combat-hold notice is once per custody and encounter. Review R3 (`fb7f048d`, `b80534f9`): it is delivered through the native warning notification. Review R4 (`e62cad39`): never attempted during a load, and spent only once native invoked a warning receiver; the compiled notifier was run through the installed native `EventBus` with no receiver, then a receiver, and during a load (`CombatHoldNoticeTests` (4)) | Not run |

### C04 — what is established, and what is not

- **Inspected:** the interaction (`FindWay`'s `InteractionSkillCheck`), its passed actions (a Conditional on
  `UnitInScriptZone(InteractingUnit, "SortcutBefore")` choosing between two `TeleportParty` destinations), the
  geometry, and native's zone membership, which is updated only for units with native motion this tick.
- **Inferred, not observed:** a seated rider processed before its mount never shows native motion. Its stale
  membership sends the party to the enter point where it already stands.
- **Not established:** the awake order in the owner's game. The original log was not read; it is absent.
- **Kept apart:** the authored crossing distance (8.26 m), C02's recognition distance and C03's regroup limits.

### C01 — the exact blocker

The existing scene-staging lifecycle cannot take an actively participating pair out of combat while preserving the
required restoration intent under its present contract:
- `TryHandOffForStaging` refuses during an encounter.
- Beneath it, `SuspensionRefusal` requires settled participation (`HasAreaParticipationObligations`), which cannot
  settle while an actor is in combat.
- A plain in-combat dismount would lose the restoration intent.
- Forwarding to the mount and early completion are not permitted.

Capturing and restoring a live activation is not established as the only possible design. A combination of the
existing native split with a separate deferred remount intent would need its own investigation and verification.
C01 remains unsupported, not fixed.

## Sprint 5 — mounted ranged movement (reconciled 2026-11-17)

Record: [Sprint 5 handoff](KNOWN-ISSUES-SPRINT5-HANDOFF.md). No product change: the product build stays `cdfbe509`
(clean Release DLL SHA-256 `f584c1c6e0678a9447a6082552dda57c5fcc4a95e475430c20fb5a93210f5035`, MVID `6cb7b950-e97c-4c14-9d83-764b074a3439`).

| ID | Symptom | Classification | Status | Implementation and offline evidence | Native |
|---|---|---|---|---|---|
| R01 | Carried travel in a separate initiative slot gives no ranged-movement contribution. | Planned omission, not reproduced | **Closed: not reproducible; no reachable instance; no product change** | Under the qualified authority the mount's candidate is suppressed while the pair's unsplit activation exists. The only split is the dismount's, which never leaves the pair Mounted. A combat mount forms only on the rider's turn, and a retained partner is ended. A mount's own `native:` record is therefore always riderless or pre-mount, and stays excluded. `95fd7120`: 2 production-executed tests; `Test-MountedRangedMovementContracts.ps1` 5/0; S1–S11 mutations caught | Not run |
| R02 | The accepted ranged rules need precise documentation. | Intentional adaptations | **Intentional adaptation documented** | Exact rule in the Sprint 5 handoff and the Mounted Archery handoff's superseding section. README: steps and Charge do not count. Feat text unchanged | Not run |

## Sprint 6 — attack requests and terminals (reconciled 2026-11-18)

Record: [Sprint 6 handoff](KNOWN-ISSUES-SPRINT6-HANDOFF.md). Corrected after the reviews of `2ae4cc8b`, `fa533e97`,
`dffd3e72` and `95ae0b30`. Product-build commit `7898b1a4`, clean Release DLL SHA-256 `7da6d2c006ff3d71b9d51e29db729a514c6beda826f4611768c03347581dbc64`, MVID `19fba84e-efe2-4a0f-a82c-da26f79e60a4`.
- **Review-discovered lifecycle defect (R21), corrected:** the native terminal captures of `1d0054f0` rooted commands in
  strong thread-static fields. The facts now live in the native invocation's own locals, and no static owns a command.
- **R22:** the retired scheduler's lease release is deferred to an owned cleanup (next update after native slot
  removal, relationship cleanup, disposal) and now proven; the earlier "always releases" wording was withdrawn.
- **R23, corrected:** the CanNotAttack term of each wrapped `ShouldBeInterrupted` call has its own scalar frame, so nested
  and exceptional observations cannot exchange terms.
- **R24, records corrected:** a failed terminal observation faults the scheduler lease and records the scheduler's interrupt bookkeeping; it does not re-end the already-finished command, interrupt it again, or replace its existing terminal cause.

| ID | Symptom | Classification | Status | Implementation and offline evidence | Native |
|---|---|---|---|---|---|
| A01 | A fresh same-target click just after attack completion gets inconsistent feedback. | Request-lifecycle defect | **Implemented and verified offline** | `MountedAttackIntent.IsFreshRequestAfterTerminal`: a click after the dispatched command ended is a fresh request (a new generation and its own answer); a live command still continues. Probe section A01: the accepted Sprint 5 build fails, this build passes | Not run |
| A02 | Terminal feedback can describe a player cancellation as "cannot act". | Diagnostic and feedback defect | **Implemented and verified offline** | Typed `MountedAttackEndCause` recorded at each end. `CausalCapability` takes only the recorded `ActorCapability` block or the block of a native capability branch recorded where native takes it (turn-based cannot-act, running cannot-act, attack ban; review of `fa533e97`, R18). Native target, InterruptAsSoonAsPossible and unknown ends are never read as a condition. The terminal handler and the retired scheduler are no-throw (R20). Probe: the Sprint 5 build and the reviewed `ce9b1383` build fail the A02/R18/R20 sections; this build passes | Not run |
| A03 | The Serenity or Fascinate condition that stopped the rider is unidentified. | Open explanation | **Settled explanation; no product change** | Decoded assets: Fascinate = `FascinateEffectCombatBuff` (`8a59ee05…`, AddCondition Dazed) from the Lady of Shallows' beguiling aura; Serenity (`SerenityBuff`) adds no condition. Dazed → `CanAct` false (installed IL), read by `NativeActorCapability`. Live state not observed | Not run |
| A04 | Real-time blocked orders wait natively (intended); automatic proposal churn was a hypothesis. | Intended behaviour plus established churn | **Intentional wait documented; churn established and corrected (automatic only)** | Installed brain and AI: no `CanNotAttack` exclusion; a re-proposal every 0.3–0.4 s; native interrupts. `Decide` now refuses with `RiderCannotAttack`, which is stateless. Probe A04: base 30/30 admitted, now 0/30 | Not run |
| A05 | Any non-success automatic end with no strike (target death, capability, cancellation, an in-range interruption) marks its target unreachable for the rider's brain. | Product defect (found in A04) | **Implemented and verified offline** | Only the typed `ApproachFailed` cause, thrown at the repath-exhaustion or approach-time boundary, with approach evidence (approach required, delegated move started, no strike, no success or tail), records it. Probe A05 (12 cases): the Sprint 5 build and the reviewed `2ae4cc8b` build mark target death; this build marks only case 1 | Not run |

## Sprint 7 — mounted presentation (reconciled 2026-11-19)

Record: [Sprint 7 handoff](KNOWN-ISSUES-SPRINT7-HANDOFF.md). Corrected after the owner review of `292cd23c` (U04's native
delivery and lifecycle, R1 and R2), of `e7720f39` (U04's failure containment, R3 and R4) and of `89ace80f` (exact pair
cleanup, R5). Product-build commit `e54009e9`, clean Release DLL SHA-256
`851c44d6473210afd82630f33871356681460f2337b95a63a843b8849a7df21d`, MVID `6551bd65-dc7e-4ba5-bbfb-3bf84ed277c2`. Every
change is a projection of current authoritative state onto native UI; no gameplay, save, grant or scheduling state
changes. No screen was viewed.

| ID | Symptom | Classification | Status | Implementation and offline evidence | Native |
|---|---|---|---|---|---|
| U01 | After Charge the Move slot is unavailable but still shows usable distance. | UI defect | **Implemented and verified offline** | A prefix on native `CombatAction.UpdateRemainingDistance` shows 0 for the mount's Move and Standard actions once its allocation for the exact live activation records forced travel (the fact that already refuses the travel); the rider-selected fill promises no step then. `TimeMoved`, cooldowns, the allocation and native's Unable state unchanged. Presentation probe: the Sprint 6 build writes 32 ft, this build 0; 13 cases | Not run; not viewed |
| U02 | The public action slot shows no selection glow while its delivery ability targets. | UI defect | **Implemented and verified offline** | An exact value-identity mapping (slot, its ability data, pair owner, generation and state, owner, public and delivery abilities, delivery data, selection operation), held weakly, recorded by the click postfix and drawn through native's own selected branch in `UpdateSlotInternal`; every selection start or end and every lifecycle boundary clears it. Probe: 12 cases; the Sprint 6 build draws no mark | Not run; not viewed |
| U03 | The Paladin class panel promises Weapon Bond upgrades to Divine Mount characters. | UI defect | **Implemented and verified offline** | The class block's one `GetClassProgressions` read is replaced by projected copies that omit the six upgrades for a Divine Mount or not-yet-chosen bond, read afresh from the shown descriptor (committed or preview). Unit data, blueprints and grants untouched. Probe: 12 cases on genuine progression data; the Sprint 6 build shows the upgrades. Per-level tooltips and the console screen not changed | Not run; not viewed |
| U04 | No mounted badge on the rider portrait; hovering a folded mount highlights nothing. | UI omission | **Implemented and verified offline (native delivery and lifecycle corrected after the review of `292cd23c`; failure containment corrected after the review of `e7720f39`; exact pair cleanup after the review of `89ace80f`)** | Identity policies as accepted. The badge is reconciled at the portrait's native per-frame refresh (`UpdateCrippled`; `HealthUpdate` runs only on a hit-point change) and bind (`Initialize`), and hidden and destroyed at load, area unload, world discard, disable and disposal; a badge whose removal fails stays owned as a hidden residue under the same bound of 32 and is retried at every refresh and boundary (R4). The folded mount's world hover (the only source native routes to the tracker; a portrait hover does not reach it) highlights its rider's existing entry under one owned lease (weak tracker, entry and rows; pair identity; hovering unit), released before every native hover pass and at a tracker rebuild, its own pair's cleanup (another pair's cleanup only retries owed cleanup, R5), a generation change and every world boundary; the release is a bounded (4), weak cleanup obligation whose two steps run independently and which is kept until each completes, its target is gone or replaced, or native's own completed pass recomputes that tracker (R3). Native's own hover pass ran detached under the product's real Harmony12 patches. The reviewed `92310c52` fails 41 of the R3/R4 cases; the reviewed `6ff46a2a` fails the 7 unrelated-pair cases of R5 | Not run; not viewed |
| T09 | `scripts/Test-Finalize2UpgradeReaders.ps1` stops at `PathNotFound`. | Harness needing retained native evidence | **Open investigation** | Its last section reads run r05's `runtime-request.json` from the lab's `runtime-evidence`, absent on this machine. Identical on the Sprint 6 worktree. Found as a U02 neighbour on 2026-11-19 | Not applicable |

## Sprint 8 — documentation (reconciled 2026-11-19)

Record: [epic closeout](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md). No product change.

| ID | Symptom or purpose | Classification | Status | Delivered | Native |
|---|---|---|---|---|---|
| D01 | Current status is fragmented across dated handoffs and stale entry points. | Documentation | **Implemented** | The [current status](#current-status-sprint-8-2026-11-19) section above: one row per ID with the canonical fields, the coverage map, the review-discovered defects; `CURRENT.md`, `AUTONOMOUS-RESUME.md` and `README.md` now open with the same current summary | Not applicable |
| D02 | Support, removal and rollback limits need one current description. | Documentation | **Implemented** | [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md); `README.md` points to it; dated superseding notes on the 0.2.0 notes and the two Chunk 7 guides | Not applicable |
| D03 | Final sprint handoff and completion accounting. | Documentation | **Delivered** | [KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md](KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md) | Not applicable |

### As planned on 2026-11-15 (kept for history)

Each row is the epic's finding as planned. "Planned closure" is what the epic expects, not a result.

| Sprint | ID | Symptom or purpose | Classification | Status | Planned closure | Source |
|---|---|---|---|---|---|---|
| 8 | D01 | Current status is fragmented across dated handoffs and stale entry points. | Documentation | Open correction | This ledger reconciled for every ID | `CURRENT.md`, `AUTONOMOUS-RESUME.md`, `README.md` |
| 8 | D02 | Support, removal and rollback limits need one current description. | Documentation | Open correction | Documented | `README.md`, [0.2.0 notes](RELEASE-NOTES-0.2.0.md), [deferred items](FINALIZATION-DEFERRED-ITEMS.md), [UI polish handoff](UI-POLISH-REVIEW-HANDOFF.md) |
| 8 | D03 | Final sprint handoff and completion accounting. | Documentation | Open correction | Delivered | Sprint PRs and this ledger |

### C04 — as recorded on 2026-11-16, before Sprint 4 (kept for history; the current disposition is in the Sprint 4 section)

Recorded from the owner's amendment. Nothing below was investigated or reproduced by Sprint 3.

- **Report:** at Candlemere Tower, choosing "wade through" fades the screen but the party does not cross while a
  pair is mounted. After dismounting by hand, the crossing works.
- **Log evidence, as the owner states it:** `output_log(9).txt` reports Mounted Combat 0.2.11 and records a
  successful manual dismount. It does not identify the crossing action and does not establish a cause.
- **Where that log is:** it is not on the development machine (searched 2026-11-16); the two statements above are
  the owner's and were not re-read from the file here. Sprint 4 needs the file itself.
- **Status:** open investigation; a reported mounted traversal defect. No cause is established and no correction
  is selected.
- **To establish first:** the native action behind "wade through", its actors, the checks it makes, its
  destination and its completion boundary.
- **Keep apart:** the native crossing distance, the mod's recognition of a placement, and the later regrouping
  limits are three different things and are not to be merged into one explanation.
- **Not an explanation of C04:** the same log also holds a separate dismount on arrival. That is additional
  evidence for S02 (the post-load and arrival dismount), recorded here so it is not mistaken for the crossing's
  cause.

This table is a scaffold. D01–D03 are not done because their rows exist: the full reconciliation of current
documentation is Sprint 8's work. (Superseded 2026-11-19: Sprint 8 did that
reconciliation; see the Sprint 8 section above.)

## Superseded by an identified fix (carried forward)

Shipped corrections the epic records as fixes. They are not reopened by lack of native acceptance.

| Finding | Fix | Offline evidence | Native |
|---|---|---|---|
| Mounted attack retried without end when the actor could not act | 0.2.11, PR #43, merge `22945394` | [ATTACK-STALL handoff](MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md) | Not run (rows S1–S9) |
| Mounted Archery missing from every feat list | 0.2.10, PR #41, merge `81b3a30a` | [REGISTRATION handoff](MOUNTED-ARCHERY-REGISTRATION-REPAIR-HANDOFF.md) | Not run (rows R1–R5) |
| No-Standard warning shown after an ordinary completed attack | 0.2.6, PR #33, merge `b8359e31` | [ATTACK-COMPLETION handoff](ATTACK-COMPLETION-WARNINGS-HANDOFF.md) | Not run |
| Every dialogue or cutscene dismounted the pairs | 0.2.8, PR #37, merge `2dbf62c5` | [CUTSCENE handoff](CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md) | Not run |

## Manual/native verification only, excluded from this epic (carried forward)

These stay visible as evidence gaps. They create no sprint task and no acceptance gate here.

- WORLD-02 mounted trap disarm ([deferred items](FINALIZATION-DEFERRED-ITEMS.md)).
- Turn-based doors.
- Paladin and Riding Dog gameplay without Call of the Wild.
- Six-pair combat qualification.
- Prone-rider save/load and unusual fall combinations.
- Physical UI, other resolutions and the cleanup shortcut ([UI polish handoff](UI-POLISH-REVIEW-HANDOFF.md)).
- The owner's full personal mod stack.
- Native acceptance of the released exploration, door, load, pace, cutscene, Mounted Archery and attack-stall
  repairs (0.2.3–0.2.11), as recorded in `CURRENT.md`.

Feature expansion is also out of scope and unchanged: no Cavalier class, barding, enemy mounted AI, borrowed,
summoned, flying or exotic mounts, full celestial template, Run action, Mythic feat, real-time ranged rule, or
general removal of KMC content from dependent saves.
