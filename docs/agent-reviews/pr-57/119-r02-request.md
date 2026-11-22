<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f key=r02-request part=1/1 sha256=37433cfbbbc4a75fd0a548f5189909129434a1c812470985483c63ff463a1d5a -->
### Claude Opus — review request — round 2 — candidate `b34616ffa2`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f-r02-c1dd22` |
| Checkpoint | `final` (final) |
| Candidate H | `b34616ffa234916e04bfb821ca82bf88cd565082` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission-with-toolbar-repair-addendum` sha256 `2faba57367` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `b3b4495c-89c0-495f-9967-40a664b6eb37`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 2 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

All 16 review round 1 findings are fixed in new commits 9ecc4a77, 56366c41 and b34616ff. No history was rewritten.

The focused gate passed on the pushed HEAD b34616ff:
- components 1456/0, including 36 toolbar lifecycle tests;
- Validate-Source 153/0;
- Kingmaker assembly contracts 825/0;
- Tabletop Rules Expansion click composition 21/0, both patch orders;
- archive bound 7/0; six-pair adapter 22/0; area adapter baseline matched; target liveness 3/0;
- mutation child: lifecycle baseline 36/0, wiring baseline 5/0, all 16 production mutations killed.

Design change, from R1-F1: the arrangement is again one chosen slot. Every other public binding seen on the bar is a known copy, identified by slot object. A copy yields to the recoverable choice; a binding that is not a known copy is the player's fresh placement and wins; when the player fills the pending slot, the first surviving copy (an older alias included) becomes the fallback; a live control keeps one shortcut. The set semantics of writer invocation 1 is retracted, and the archived outcomes of R12-F1, R15-F1, R17-F1, R18-F1, R19-F2, R21-F1, R22-F1, R22-F3, R23-F1 and R23-F3 are restored in the tests, the archive check and the handoff.

Lifecycle and adapter corrections (each with a named reproduction):
- R1-F2: a missing player during an open selected load now holds everything; the update does nothing until the world outcome is known.
- R1-F3: a native rebind of the chosen slot makes it live; a rebound copy keeps copy identity.
- R1-F4: the alias fallback is resolved before migration clears it.
- R1-F5: sanitation reaches the persistent pet of every swept unit, including a pair-custody remote or detached owner.
- R1-F6: an older Mounted Charge becomes the stock Charge before any sweep destroys it, so save-first matches update-first.
- R1-F7: departure clears the exact KMC auto-use selection.
- R1-F8: the adapter's native-operation field is removed; the lifecycle is the operation's only owner.
- R1-F9: an unavailable bar defers the restore instead of consuming it.
- R1-F10: an observed former instance never authors over a current reserve or detached instance.
- R1-F11: archive rows naming a resolvable pet own no arrangement and are not saved again.

Evidence fixes:
- R1-F12: the disabled-save leg installs and asserts the stale selection after disabling; the AC-R22-F3 and AC-R26-F1 edits place the current live control and assert its identity.
- R1-F13: R25-F1, R24-F2 and R9-F1 now run to their archived boundaries; R9-F1 drives Disable through the production ResolveExactKind classifier and checks ordinary slot-object identity.
- R1-F14: a new offline wiring fixture compiles the actual NativeMountedControlService.Toolbar.cs against labeled Kingmaker facades and drives the service's own entry points. Five wiring mutations are killed: save-scope forwarding, detached roster, native prefix forwarding, persistent pet, reserve owners.
- R1-F15: the F2 reader now reads the capture from the toolbar partial, and the U02 probe creates the lifecycle on its blank service.
- R1-F16: the handoff carries the exact two-sentence status and restores the concrete melee, dismounted ranged, expenditure and ammunition playtest steps.

Inherited acceptance cases, each PASS, evidence type UNIT (the production lifecycle through thin fakes), all on b34616ff:
- AC-R26-F4: AcR26F4ReservePetSanitized, plus PairCustodyOwnerPetSanitized.
- AC-R22-F3: AcR22F3FreshPlacementAfterNativeEmpty. Only 11 after the live placement, only 3 untouched, also with a known copy at 7.
- AC-R25-F3: AcR25F3FreshShortcutAfterReplacement.
- AC-R26-F1: AcR26F1CanceledLoadKeepsEdit, with a live edit.
- AC-R26-F2: AcR26F2MainMenuReleases. The fenced control saves 3 only; the wiring fixture checks the adapter reference.
- AC-R26-F3: AcR26F3RejoinedRiderAutoUse, including the corrected disabled-save leg.

Limits:
- The F2 reader still exits non-zero overall: it stops at the independently missing r05 retained evidence (T09), after all its synthetic and removal-safeguard checks pass.
- The wiring fixture compiles the adapter partial only; the main file's SetEnabled, Update and Dispose, and the one-line Harmony forwarders, are mirrored, not compiled.
- Native cases remain NOT RUN, because no command available to this enrollment builds the commit-bound package the guarded runner needs. They are deferred to the owner playtest; no runtime transaction ran.

Counts: the first enrollment ran 26 review rounds and 27 writer invocations; this enrollment has had review round 1 and writer invocation 2.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (candidate b34616ff) | yes | pass | 32.9s | Fast tier: build; components 1456/0; Validate-Source 153/0; wrapper 7/7 children: assembly contracts 825/0 (Kingmaker only), TRE click composition 21/0 both orders, archive row bound 7/0, six-pair adapter 22/0, area adapter baseline 63/63 matched (compile only), target liveness 3/0 (source), toolbar mutations (lifecycle 36/0, wiring 5/0, 16/16 killed). Offline only; no Candidate, Full, package or native run. |
| agent_review.py verify --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (candidate 56366c41) | no | pass | 33.1s | Same code as b34616ff except the handoff; source of the mutation table in the handoff. |
| agent_review.py verify --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (candidate 9ecc4a77) | no | fail | 47.4s | The wiring fixture baseline did not compile (CS0649 under warnings-as-errors; the diagnostics were swallowed by the script function). Everything else passed: components 1456/0, lifecycle baseline 36/0. Fixed in 56366c41; retained as failed evidence. |
| agent_review.py verify --gate focused -- -Tier Fast -Focused Test-MountedPresentationContracts.ps1 (candidate 56366c41) | no | pass | 15.3s | R1-F15: U02 public-slot presentation probe on the split service; mounted presentation contracts 239/0, detached; not native or visual evidence. |
| agent_review.py verify --gate focused -- -Tier Fast -Focused Test-Finalize2UpgradeReaders.ps1 (candidate 56366c41) | no | fail | 16.8s | R1-F15: all synthetic producer, consumer and bridge checks and the four removal-safeguard checks (capture now located in the toolbar partial) passed. The script then stops at the missing C:/Dev/KingmakerMountedCombatLab/runtime-evidence/20261105-finalize-f2-r05/runtime-request.json, the already-documented T09 historical-evidence gap, independent of this change. |
| dotnet build src/KingmakerMountedCombat/KingmakerMountedCombat.csproj -c Release; dotnet build tests/KingmakerMountedCombat.Tests.csproj -c Release | no | pass | 17s / 1s | Development compiles only. |
| Native in-game toolbar, city companion and prone attack cases | no | not_run | 0 | The guarded runner requires a commit-bound candidate package that no command available to this enrollment can build. Deferred to the owner playtest (handoff section 11); not described as game-tested. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-b34616ffa234-d43d449b6c4-a030b738800-20261122T031530794855Z.log
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-56366c41f337-d43d449b6c4-a030b738800-20261122T031113243294Z.log
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-56366c41f337-d43d449b6c4-ae72bf52208-20261122T031142732979Z.log
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-56366c41f337-d43d449b6c4-a3e6a8d75da-20261122T031211757859Z.log
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-9ecc4a771cff-d43d449b6c4-a030b738800-20261122T030930361890Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.Toolbar.cs
- tests/MountedControlPlacementIntentTests.cs
- tests/Chunk7Diagnostics/ToolbarAdapterWiringFixture.cs
- scripts/Test-ToolbarLifecycleMutations.ps1
- commits 9ecc4a77, 56366c41, b34616ff

**Prior findings to assess:** `R1-F1`, `R1-F10`, `R1-F11`, `R1-F12`, `R1-F13`, `R1-F14`, `R1-F15`, `R1-F16`, `R1-F2`, `R1-F3`, `R1-F4`, `R1-F5`, `R1-F6`, `R1-F7`, `R1-F8`, `R1-F9`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
