# Known Issues Remediation Epic — closeout (2026-11-19)

The final engineering accounting of the Mounted Combat Known Issues Remediation Epic (owner document of 2026-11-15,
revised 2026-11-16), written by Sprint 8 (D01–D03). This record also serves as Sprint 8's handoff.

- **Current issue dispositions:** [CURRENT-KNOWN-ISSUES.md](CURRENT-KNOWN-ISSUES.md). This closeout links to it and to
  each sprint handoff instead of repeating their detail.
- **Current support, removal and rollback contract:** [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md).
- **Native gameplay:** not run by any known-issues sprint. No Sprint 7 screen was viewed.
- **Release:** the epic was merged into main on 2026-11-19 (PRs #45–#53) and then **released as 0.2.12** the same day
  by owner order ([notes](RELEASE-NOTES-0.2.12.md); package identity on the GitHub release). The previous release,
  `v0.2.11`, stays at its historical release merge `7a5cd2c3`. The release was not native-qualified.

## Identity

### Baseline and stack

- **Epic planning baseline:** main `7a5cd2c393fad80fe09700ecbc40949245335449`, release `v0.2.11`. Main did not
  advance during the epic; the stack was integrated afterwards (see [Integration into main](#integration-into-main-completed-2026-11-19)).
- **Sprint 8 base:** `1ba1841a3fdcbb9f56a94e55713311d38928b12c`, the accepted Sprint 7 checkpoint (head of PR #52).
- **Sprint 8 branch:** `codex/kmc-known-issues-sprint8`, worktree `worktrees/known-issues-sprint8` in the lab, PR
  [#53](https://github.com/howardreith/KingmakerMountedCombat/pull/53), first targeting `codex/kmc-known-issues-sprint7`,
  retargeted to `main` after #52 merged. Product source unchanged.
- **Version:** every sprint branch, and main after integration, declared 0.2.11 (assembly version 0.1.0.0) and built a
  different binary from the `v0.2.11` package; release 0.2.12 changed the version label to 0.2.12.

| Sprint | Packages | PR | Branch | Original target | Accepted head | Product-build commit | Product DLL (clean Release) | Kind | Merge into main |
|---|---|---|---|---|---|---|---|---|---|
| 1 | T01–T03 | [#45](https://github.com/howardreith/KingmakerMountedCombat/pull/45) | `codex/kmc-known-issues-sprint1` | `main` | `b62c59a8` | `3c64128a` (T02 diagnostic code) | not recorded in the handoff | validation tooling; diagnostic evidence code only | `faf1195d` |
| 2 | S01, S02 | [#46](https://github.com/howardreith/KingmakerMountedCombat/pull/46) | `codex/kmc-known-issues-sprint2` | sprint 1 | `1dffc3e0` | `5d62a289` | not recorded in the handoff | product | `1fb225a1` |
| 3 | M01–M05 | [#47](https://github.com/howardreith/KingmakerMountedCombat/pull/47) | `codex/kmc-known-issues-sprint3` | sprint 2 | `e5b92474` | `f6e86858` | SHA-256 `FCBBA76E…4C57`, MVID `16c76ae9-7416-4e49-8de5-937b61be2926` | product | `3d51199e` |
| 4 | C04, C01–C03 | [#48](https://github.com/howardreith/KingmakerMountedCombat/pull/48) | `codex/kmc-known-issues-sprint4` | sprint 3 | `14f1b474` | `03685720` | SHA-256 `7ce8f88a…01efd59c8`, MVID `efdc12b6-0098-4027-ad49-f8c8f33851bc` | product | `c458042d` |
| 3 closeout | M04 | [#49](https://github.com/howardreith/KingmakerMountedCombat/pull/49) | `codex/kmc-known-issues-sprint3-closeout` | sprint 4 | `af5b2bc7` | `cdfbe509` | SHA-256 `f584c1c6…f5035`, MVID `6cb7b950-e97c-4c14-9d83-764b074a3439` | product | `4342596e` |
| 5 | R01, R02 | [#50](https://github.com/howardreith/KingmakerMountedCombat/pull/50) | `codex/kmc-known-issues-sprint5` | sprint 3 closeout | `772543e1` | none (unchanged `cdfbe509`) | as Sprint 3 closeout | tests and documentation only | `1c88d070` |
| 6 | A01–A05 | [#51](https://github.com/howardreith/KingmakerMountedCombat/pull/51) | `codex/kmc-known-issues-sprint6` | sprint 5 | `f835c503` | `7898b1a4` | SHA-256 `7da6d2c0…dbc64`, MVID `19fba84e-efe2-4a0f-a82c-da26f79e60a4` | product | `dd52f359` |
| 7 | U01–U04 | [#52](https://github.com/howardreith/KingmakerMountedCombat/pull/52) | `codex/kmc-known-issues-sprint7` | sprint 6 | `1ba1841a` | `e54009e9` | SHA-256 `851c44d6473210afd82630f33871356681460f2337b95a63a843b8849a7df21d`, MVID `6551bd65-dc7e-4ba5-bbfb-3bf84ed277c2` | product | `4b2c0a52` |
| 8 | D01–D03 | [#53](https://github.com/howardreith/KingmakerMountedCombat/pull/53) | `codex/kmc-known-issues-sprint8` | sprint 7, then `main` | the final head of #53 | none | as Sprint 7 | documentation only | lands after #52 |

Full identities, reviewed builds and frozen artifacts are in each handoff's identity section. Accepted heads are the
heads the owner accepted as the next sprint's base; the Sprint 7 head was accepted as an offline engineering
checkpoint.

### Integration into main (completed 2026-11-19)

By owner order after the final Sprint 8 review, the linear stack was merged into main in order, without release:

| PR | Accepted head | Merge commit | First parent (main before) |
|---|---|---|---|
| #45 | `b62c59a8` | `faf1195d1bb7afa2d808175f82385a77536e96fc` | `7a5cd2c3` |
| #46 | `1dffc3e0` | `1fb225a1a0083758c0da085281be348b10a67a31` | `faf1195d` |
| #47 | `e5b92474` | `3d51199e22f9b77530beff538836afa04bac9870` | `1fb225a1` |
| #48 | `14f1b474` | `c458042da48d5b043008faa45aafc6a8294aac1e` | `3d51199e` |
| #49 | `af5b2bc7` | `4342596e53d4136f7508098a5f9fd485444bcbc5` | `c458042d` |
| #50 | `772543e1` | `1c88d070cff02ddfb976614bb0fa53b17ac41d85` | `4342596e` |
| #51 | `f835c503` | `dd52f359e252ca7b5afd310cb0b5dd1082bf1739` | `1c88d070` |
| #52 | `1ba1841a` | `4b2c0a5204904cec653908b42cf216e7f9582bff` | `dd52f359` |
| #53 | the final head of #53 | lands after #52 | `4b2c0a52` |

- Each PR was checked at its accepted head, retargeted to `main` after its predecessor landed, compared (its diff equal
  to the sprint's accepted delta, and main's tree equal to the accepted parent's), marked ready for review (required to
  merge a draft) and merged with an ordinary merge commit pinned to the accepted head. No reviewer was requested.
- No conflict arose. Each merge commit's second parent is the accepted head and its tree equals that head's tree, so
  no accepted correction was lost; main's product source equals the accepted Sprint 7 product build `e54009e9`.
- Nothing was squashed, rebased or force-pushed, and no branch was deleted.
- Merging to main is not a release: no tag, version, package or release was made, and `v0.2.11` still targets
  `7a5cd2c3`.

## Package accounting

Final disposition of every work package, in the epic's order, with the IDs found during the epic. No package was
run in the game by this epic; whether native execution applies to a package at all is the ledger's per-ID native
field. The ledger holds the canonical status fields and the detail; the handoff holds the evidence.

| ID | Final disposition | Result for the player or developer | Cause or conclusion | Evidence | Remaining uncertainty |
|---|---|---|---|---|---|
| T01 | Implemented and verified offline | The charge probe states the six-pair contract; 71 checks run (29 before) | Stale two-pair assertion | [Sprint 1](KNOWN-ISSUES-SPRINT1-HANDOFF.md); capacity-5 mutant fails | The script still exits at T04 |
| T02 | Implemented and verified offline | Repeated-world reader selects a reviewed subscriber revision; producer emits v3 | Stale 13-interface expectation | Reader 220/0; compiled contract 21/0 | No native repeated-world run on a current build |
| T03 | Implemented and verified offline | Loader identity and API reported separately | Version pins conflated with API | Assembly contracts 810/0 (801/3 before); loader checks 22 | 0.28.2 unassessed; no loader run natively |
| S01 | Investigation complete, issue unresolved | Save diagnostics added (observation only) | 0.2.3 occurrence explained and fixed in 0.2.4; 0.2.4 report cause not established | [Sprint 2](KNOWN-ISSUES-SPRINT2-HANDOFF.md); persistence contracts 223/0 | Whether and how a save was attempted in that session |
| S02 | Implemented and verified offline | A world-map arrival's pair waits for active views and remounts instead of dismounting; its placement survives save and load | Restoration ran before native view activation (inference from IL for the recorded session) | 21 component tests; contract script 26/0 (base and reviewed builds fail) | Integration order read from IL, not executed |
| M01 | Implemented and verified offline | Entrance and exit orders get native acceleration | Omitted command path | [Sprint 3](KNOWN-ISSUES-SPRINT3-HANDOFF.md); contract 27/0; mutants caught | Harmony installation not run natively |
| M02 | Implemented and verified offline | Gamepad acceleration reaches the mounted mover | Follower pass answered with the rider | Production transpiler on installed `UpdateMovement` | No gamepad session ever run |
| M03 | Implemented and verified offline | Group pace uses the horse's speed | Selected rider stood in for the mover | Production transpiler on installed `MoveSelectedUnitsToPoint` | — |
| M04 | Engineering investigation complete; all identified product marker defects repaired | Four marker sources removed; marker ownership observed | Original incident not attributable | [Closeout](KNOWN-ISSUES-SPRINT3-CLOSEOUT.md); marker contract 18/0 | The owner's original incident; native and manual verification |
| M05 (new) | Implemented and verified offline | Stick input drives a mounted pair's mount instead of dismounting it | Executor rewritten to the mount; native refused the command | Contract script 59/0; 27, 35, 37 and 11 targeted mutants caught across its reviews | Never observed in play; native still faults on a command its `Run` did not admit |
| C04 | Implemented and verified offline | A mounted party crosses Candlemere's wade-through | Stale script-zone membership of a seated rider (inferred) | [Sprint 4](KNOWN-ISSUES-SPRINT4-HANDOFF.md); contract 11/0 | The owner's awake order; original log absent |
| C01 | Explicitly unsupported, with its blocker | Unchanged: waits for the scene timeout, then returns to the saddle once | Staging cannot leave active combat and keep restoration intent | Fallback pinned on both builds | Another design would need its own investigation |
| C02 | Implemented and verified offline | Attributable short rider-only scene placements are honoured | Distance-only recognition | Contract 24/0 (reviewed build 21/3) | Another handler inside a cutscene's open scope counts as the cutscene's |
| C03 | Intentional limit documented; one separate repair | Safe regroup and restoration bounds kept; combat-hold notice once per custody and encounter through native warnings | Boundaries intentional; notice latch defect repaired | `CombatHoldNoticeTests` (4) | — |
| R01 | Closed: not reproducible; no product change | No separate-slot carried travel exists to attribute | Mount never moves on its own slot while carrying | [Sprint 5](KNOWN-ISSUES-SPRINT5-HANDOFF.md); contract 5/0; S1–S11 caught | — |
| R02 | Intentional adaptation documented | Exact rule published (3.25 s, −4/−2, per roll, turn-based only) | Deliberate adaptation | Existing boundary tests | Native floor can absorb part of the penalty |
| A01 | Implemented and verified offline | A click after a finished attack is a fresh request | Finished work inherited dispatched state | [Sprint 6](KNOWN-ISSUES-SPRINT6-HANDOFF.md); Sprint 5 build fails probe | — |
| A02 | Implemented and verified offline | Terminal text reports the actual end cause | Capability snapshot read for every end | Probe sections; Q1–Q26 caught | — |
| A03 | Settled explanation; no product change | — | Fascinate is Dazed (decoded assets); Serenity adds no condition | Asset evidence summary | Royon's live state not observed |
| A04 | Intentional wait documented; churn established and corrected (automatic only) | Automatic proposals for a `CanNotAttack` rider refused | Brain re-proposes every 0.3–0.4 s | Probe 30/30 admitted before, 0/30 after | — |
| A05 (new) | Implemented and verified offline | Only a genuine approach failure marks a target unreachable | Every non-success end was recorded as failed approach | Probe A05 (12 cases) | — |
| U01 | Implemented and verified offline | No usable distance shown after Charge | The display did not reflect the existing forced-travel restriction | [Sprint 7](KNOWN-ISSUES-SPRINT7-HANDOFF.md); 13 probe cases | Not viewed |
| U02 | Implemented and verified offline | Public slot glows while its delivery ability targets | The selected mark followed the hidden delivery ability, not the public slot | 12 probe cases | Not viewed |
| U03 | Implemented and verified offline (class panel only) | Class panel omits Weapon Bond upgrades for Divine Mount | The panel read the stock progression, which lists the upgrades for every Paladin | 12 cases on genuine progression data | Tooltips and console screen unchanged; not viewed |
| U04 | Implemented and verified offline | Saddle badge on rider portrait; folded-mount hover highlights the rider's entry | Omission | Presentation contracts 239/0; P1–P34 caught | Not viewed; pointer collider over a pair not observed |
| T04–T08 | Open correction (stale harnesses, unchanged) | — | Probe or harness drift against later product changes | [Ledger](CURRENT-KNOWN-ISSUES.md#sprint-1--validation-tools-reconciled-2026-11-15) | Checks behind T04 not assessed |
| T09 | Open investigation | — | Needs retained r05 native evidence absent from this lab | Identical at `f835c503` | Whether it is a harness or an evidence-custody question |
| D01 | Implemented (documentation) | One current ledger with canonical fields; entry points agree | Status fragmented across handoffs | Sprint 8 consistency checks (below) | — |
| D02 | Implemented (documentation) | [Current support, removal and rollback](CURRENT-SUPPORT.md) | Support scattered across release notes and Chunk 7 guides | Statements cite source, handoffs and notes | — |
| D03 | Delivered (this record) | Final accounting | Required closeout | This record | — |

Review-discovered defects that became part of an accepted correction are listed in the
[ledger](CURRENT-KNOWN-ISSUES.md#review-discovered-defects-folded-into-accepted-corrections).

## Integration and compatibility

- **Save format:** one change across the epic. Sprint 2 review R1 added version 2 of the `kmc-retained-intent` text,
  written only when a save holds a pair deferred at its arrival placement. Released 0.2.3–0.2.11 refuse it by its
  header, and its pairs load dismounted there. Every other save writes the version 1 bytes as before. `kmc-mounted-state`
  stays schema 3. No other sprint changed what is saved.
- **Migration:** none added. No uninstall migration, no save stripping.
- **Loader and game:** engine 2.1.7b, Kingmaker assembly MVID `07fa1e4d…` checked by the assembly contracts (815/0 at
  Sprint 7). UMM API compatible offline with genuine 0.33.0 and 0.33.2; declared minimum 0.28.2 unassessed.
- **Downgrade:** see [Rollback and downgrade](CURRENT-SUPPORT.md#rollback-and-downgrade). Because development builds between `v0.2.11`
  and `v0.2.12` said 0.2.11, a downgrade must be matched by DLL hash, not by version.
- **Integration:** see [Integration into main](#integration-into-main-completed-2026-11-19). Merged is not released.

## Validation summary

Only checks actually run. Each handoff has the commands, durations and logs.

| Sprint | Focused Fast tier | Components | Focused contracts | Kingmaker assembly contracts | Mutations |
|---|---|---|---|---|---|
| 1 | 11/0 | 1234/0 | reader 220, compiled contract 21, loader checks 22 | 810/0 | 4 runs, each caught |
| 2 | 6/0 | 1257/0 | persistence 223, world boundary 8, post-load view readiness 26 | see handoff | before/after on reviewed builds |
| 3 | 11/0 (final review) | 1338/0 | continuous drive 59, physical mover pace 27, slot 24 | 815/0 | 11 at the last review; earlier reviews 27, 35, 37 |
| 4 | 10/0 | 1370/0 | scene placement 24, Candlemere 11 | 815/0 | 3 at the last review, more at earlier ones |
| 3 closeout | 13/0 | 1385/0 | marker ownership 18 | 815/0 | 6 closure, 11 review |
| 5 | 8/0 | 1387/0 | ranged movement 5 | 815/0 | S1–S11 |
| 6 | 13/0 | 1399/0 | stall repair 273 | 815/0 | Q1–Q26 |
| 7 | 21/0 | 1411/0 | presentation 239 | 815/0 | P1–P34 |
| 8 | not run (no product change) | not run | documentation checks (below) | not run | none |

- **Known baseline-identical failures, not assigned IDs:** `Test-Harness.ps1` 277 pass, 4 fail at Sprint 7 and at the
  Sprint 6 base under matched conditions: two need a historical package zip absent from the lab, one pins a source
  string absent at the base, one is the package validator's size check. `Test-Chunk7CapitalArrivalEvidence.ps1` 162
  pass, 3 fail: the three need native evidence for batches 196–198 not on this machine. Neither involves a
  known-issues change; neither was investigated.
- **Native runs genuinely performed:** in historical work before this epic (the Chunk 7 F7 qualification, the
  finalization sprints F1–F6, the UI polish review). Their verdicts stay in their own handoffs.
- **Native runs not performed:** none by Sprints 1–8. No release from 0.2.3 to 0.2.11 was run in the game before
  release.
- **Physical UI:** no screen was viewed in Sprint 7; U01–U04 are not visually accepted.

### Sprint 8 checks

Documentation only, so no product rebuild, component run or mutation campaign. These rows were run before the
integration (at `d1eff342` and `feacf154`); "unmerged" was then true.

| Check | Result |
|---|---|
| `git diff --check` against the Sprint 7 head | clean |
| `scripts/Validate-Source.ps1` (source-only, no build; 20 s) | 151/0 |
| Every local link in each changed or new Markdown file, resolved against the tree, with heading anchors; PR links counted | 241 links, all resolve |
| Ledger coverage: one canonical row for each of the 26 epic packages and the 8 later IDs, every canonical field filled, no other package-style ID in the ledger; the 34 coverage-map rows equal the epic's rows (item, owner, epic disposition) and each has a current disposition | pass |
| Current-status agreement: CURRENT.md's and AUTONOMOUS-RESUME.md's first sections, README.md, the ledger and this record name the same unresolved set, the current release and an unmerged stack; none calls an item native-verified | pass |
| PR and commit identity: PRs #45–#52 open, draft, unmerged, at the recorded heads and targets; origin `main` still `7a5cd2c3`; each product-build commit is in the accepted history | pass |
| Changed files are Markdown only, CRLF without BOM; every table row has its header's column count | pass |

The ledger, link and identity checks were one scratch script (435 checks, 0 failures; PR #53 included), kept outside the repository with
its log at `artifacts/known-issues-sprint8/03-documentation-consistency-final.log` in the lab; `Validate-Source` at
`02-validate-source.log`. No repository script was added: these checks serve this one reconciliation, and a durable
documentation checker was not asked for by a concrete risk.

### Owner review of `feacf154` (2026-11-19)

Three documentation findings, corrected; product source, tests and scripts unchanged.

| Finding | Correction |
|---|---|
| R1 (P2): the support contract stated as fact that a save with KMC content will not load without KMC, while also saying that load was never tested | [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md#which-campaigns-need-kmc) now says KMC treats such a save as dependent, the removal preparation refuses to certify it, removal is unsupported while the references remain, and loading it without KMC was not tested (outcome unknown). README's equivalent absolute headline was corrected to match. The hand-edit warning, cleanup save, written-save scan, historical clean-removal evidence and the absence of any migration are unchanged |
| R2 (P2): "both initiative arrangements (shared and separate) are respected" contradicted R01 | The [turn-based section](CURRENT-SUPPORT.md#turn-based-combat) now says the paired authority supports actors that entered combat at the same or different native initiative positions; while mounted, the mount's independent turn candidate is suppressed and the pair acts through one shared activation, so carried movement on a separate mount slot does not occur. The Mounted Archery section uses the same terms |
| R3 (P3): the conclusion required native and manual qualification for all implemented packages, including ones where native execution does not apply | The [final conclusion](#final-engineering-conclusion) and "Missing native evidence" now scope native/manual qualification to the gameplay and presentation corrections, and name D01–D03, T01, T03, R02, R01 and A03 as native not applicable. The ledger's native field for T03, R01 and A03 now says "Not applicable" to agree |

Found while checking the same findings: CURRENT-SUPPORT.md and README.md did not name the full unresolved set; both now
do (S01, M04's original incident, C01, T04–T08, T09).

The scratch checker gained 62 semantic assertions (no cannot-load claim beside an unknown outcome; the initiative
terms in both sections; native not applicable for test, documentation and no-change packages; the same unresolved set
in all six current documents; release 0.2.11 versus stack builds also labelled 0.2.11, with no release claim).

| Check | Result |
|---|---|
| Scratch checker on this head | 507/0 (log `05-documentation-consistency-review1.log`) |
| The same checker on the reviewed `feacf154` (negative control, temporary detached worktree, removed) | 16 failures, covering R1, R2, R3 and the unresolved-set gap (log `06-negative-control-reviewed-feacf154.log`) |
| `git diff --check` against `1ba1841a` | clean |
| `Validate-Source.ps1` | 151/0 (log `07-validate-source-review1.log`) |
| Links and heading anchors; table columns; CRLF without BOM; PRs #45–#53 heads, targets, draft state, #53 without reviewers | 249 links resolve; all pass (inside the 507) |

### Owner re-review of `9a84d8a6` (2026-11-19)

| Finding | Correction |
|---|---|
| R4 (P2): CURRENT-SUPPORT.md gave the PR #43 mounted-attack-stall repair merge, `22945394`, as the 0.2.11 release identity | [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md#two-builds-to-keep-apart) now says: tag `v0.2.11` at merge `7a5cd2c3` on 2026-11-15, built from release-source commit `86ad9cc1`; the mounted-attack-stall repair (PR #43) was merged earlier at `22945394`. CURRENT.md's current section had the same error and is corrected the same way. The dated "RELEASE 0.2.11" section of CURRENT.md and the ledger's PR #43 row describe `22945394` accurately and are unchanged |

| Check | Result |
|---|---|
| GitHub's latest release; origin `main` | tag `v0.2.11`, target `7a5cd2c393fad80fe09700ecbc40949245335449`; `main` is the same commit; its parents are `86ad9cc1` (release source) and `22945394` (the PR #43 mounted-attack-stall repair merge) |
| Scratch checker on this head (20 release-identity assertions) | 530/0 (log `08-documentation-consistency-review2.log`) |
| The same checker on the reviewed `9a84d8a6` (negative control, temporary detached worktree, removed) | 6 failures, all release identity, in CURRENT-SUPPORT.md and CURRENT.md (log `09-negative-control-reviewed-9a84d8a6.log`) |
| `git diff --check` against `1ba1841a`; `Validate-Source.ps1` | clean; 151/0 (log `10-validate-source-review2.log`) |
| Links and anchors, table columns, CRLF without BOM, PRs #45–#53 | 250 links resolve; all pass (inside the 530) |

## Unresolved and excluded work

### Unresolved product reports

- **S01:** the saving report after the Ratnook door (0.2.4) has no established cause. Missing: whether a save was
  attempted, by which action, and the native gate and one-save setting at that moment.
- **M04:** the owner's original extra-marker incident is not attributable. Every identified product marker defect is
  repaired; native and manual verification remain.

### Unsupported paths

- **C01:** in-combat scripted movement of a seated rider (waits for the scene timeout).
- **Designer `RunCommand`** moving a seated rider outside any scene (same class as C01; unexercised).
- **Not repaired beside M05:** native still faults on a command its `Run` did not admit, for any unit; the followers'
  native speed cap and slow-down exemption still read the selected rider.

### Stale test and harness rows

- **T04–T08** (open correction) and **T09** (open investigation). See the ledger.

### Missing native evidence

Every gameplay and presentation correction (no sprint of this epic ran the game): S02's arrival ordering, M01–M05
(no gamepad session ever run), C02, C03's notice and C04 (the owner's Candlemere log is absent), A01, A02, A04, A05,
and U01–U04 (no screen viewed). T02's repeated-world producer and C01's bounded fallback have no native run either.
Native execution is not applicable to D01–D03, T01, T03, R02, R01 and A03 (see the ledger's native field).

### Manual acceptance only (excluded from the epic)

WORLD-02 mounted trap disarm; turn-based doors; Paladin and Riding Dog gameplay without Call of the Wild; six-pair
combat qualification; prone-rider save/load and unusual fall combinations; physical UI, other resolutions and the
cleanup shortcut; the owner's full personal mod stack; native acceptance of the released 0.2.3–0.2.11 repairs.

### Excluded expansions

Cavalier; barding; enemy mounted AI; borrowed, summoned, flying or exotic mounts; full celestial template; Run; Mythic
Mounted Archery; a real-time mounted ranged penalty; general removal of KMC content from dependent saves. None is a
commitment.

## Final engineering conclusion

- **Completed:** the scheduled engineering pass over all 26 work packages and the eight IDs found during it (T04–T09,
  M05, A05). Every ID has exactly one disposition.
- **Engineering-complete offline:** T01–T03, S02, M01–M03, M05, C02, C04, A01, A02, A04, A05 and U01–U04 implemented and
  verified offline; M04 engineering investigation complete with all identified marker defects repaired; C03's notice
  repaired.
- **Unresolved:** S01 (saving report) and M04's original incident; T04–T08 open corrections; T09 open investigation.
- **Native and manual qualification:** every gameplay and presentation correction on main (S02, M01–M05, C02,
  C03's notice, C04, A01, A02, A04, A05, U01–U04) remains native/manual-unqualified; no genuine historical native
  evidence for them is identified, and no sprint ran the game. Native execution is not applicable to the
  documentation-only D01–D03, the detached test and harness corrections (T01, T03), R02's documentation-only
  adaptation, or investigation conclusions that made no gameplay change (R01, A03). The per-ID native field in the
  [ledger](CURRENT-KNOWN-ISSUES.md#every-id) is authoritative.
- **Intentionally retained:** C03's safe bounds; R02's ranged rule; A04's real-time native wait; R01's conservative
  authority (closed as not reproducible); A03's explanation settled without product change.
- **Unsupported:** C01 and the designer `RunCommand` path.
- **Excluded:** the manual/native acceptance list and the feature expansions above.

The epic has completed its scheduled engineering pass. It has not fixed every reported defect: S01 and M04's original
incident remain unresolved. At closeout, nothing was released or release-ready: the repository's release workflow (owner order,
version change, package build and validation, release notes) was not started, and no native acceptance exists for
the merged epic.

**Release 0.2.12 (added 2026-11-19).** After the integration the owner ordered a release. 0.2.12 ships the merged
epic with the version label changed and nothing else; it was not run in the game and no release-qualification
campaign was run. See [the release notes](RELEASE-NOTES-0.2.12.md) and the GitHub release for its identity.

## Sprint 8 scope record

- **Changed:** this record; [CURRENT-SUPPORT.md](CURRENT-SUPPORT.md) (new); [CURRENT-KNOWN-ISSUES.md](CURRENT-KNOWN-ISSUES.md)
  (canonical table and coverage map first; D01–D03 closed); `README.md`; `CURRENT.md`; `AUTONOMOUS-RESUME.md`; dated
  superseding notes at the top of the [0.2.0 notes](RELEASE-NOTES-0.2.0.md) and the two Chunk 7 guides.
- **Not changed:** product source, tests, scripts, historical handoffs and their verdicts, totals and identities.
- **Budget:** `known-issues-sprint8` was not opened: no tier, build or launch ran.
- **Not done during Sprint 8:** merge, release, tag, version bump, install, staging, loader replacement, reviewer
  request, native or visual runs, another sprint. The later integration merged the stack by owner order; it made no
  release, tag, version bump, package, install, staging, loader replacement or reviewer request.
