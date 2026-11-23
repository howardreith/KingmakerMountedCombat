# MOUNTED SHIELD CONTINUATION (2026-11-23, OWNER AMENDMENT 1, PR #61)

The UMM 0.31.1 baseline below (companion commit `5be0f737`) is incorporated in `codex/mounted-shield`. Production
`cb74ad58` adds the complete selection rollback (PN3-F3 remainder), planned final check totals for forced rolls, and the
Mounted Shield gameplay driver (`sprint6-shield-rt`, `sprint6-shield-tb`, persistence case `mounted-shield-tb-manual`)
with its reader; PN3-F5 is recorded as an optional compatibility limitation. Offline gates pass (focused Fast on
`cb74ad58`); the diagnostic package validates. Native acceptance is still BLOCKED, now only by the fixture: the registered
owner-seed masters are absent and no KMC fixture exists. Read section 0 of
[docs/mounted-shield/HANDOFF.md](docs/mounted-shield/HANDOFF.md) for identities, receipts, the review and the exact input needed.

# UMM COMPATIBILITY BASELINE (2026-11-23, OWNER DECISION)

The owner confirmed the older baseline used by the sibling mods: **UMM 0.31.1**. This development branch pins that
build reference and declares that minimum. The exact current Windows native target is separately **UMM 0.33.0**,
using its already reviewed installed-file identity; a working newer installation need not downgrade.

Read [the dated loader decision](docs/CHUNK7-NATIVE-LOADER-IDENTITY.md#2026-11-23-owner-selected-0311-build-baseline-and-exact-runtime-target).
The prior Mounted Shield record below is the frozen run at `f19c8333`; its missing native fixture, gameplay driver,
current independent review and native acceptance remain outstanding. Its earlier loader-pin owner decision is resolved
by this change. Historical FAIL/blocked evidence stays unchanged. This patch has no native or Linux/Proton acceptance.

The companion patch must enter the existing factory enrollment through its guarded import/development/push flow.
No merge, release, installation, controller state edit or new enrollment is part of this checkpoint.

# MOUNTED SHIELD FEAT (2026-11-23, BRANCH codex/mounted-shield, PR #61)

Start here. The sections below this one are dated history, newest first.

- **Mission:** [docs/missions/mounted-shield/MISSION.md](docs/missions/mounted-shield/MISSION.md), the owner's packet,
  byte-identical. Software factory, foreground: enrollment `kmc-codex-mounted-shield-20261123114646-4433`, plan `single`.
- **Feature:** a passive combat feat (prerequisites Mounted Combat and Shield Focus). The rider's shield base bonus plus
  Shield Focus and Greater Shield Focus, never enhancement, becomes a native shield bonus on the current mount's AC and an
  addition to the Mounted Combat hit-negation Mobility check. Three-feat coordinated publication with independent
  withdrawal. Supplied icon embedded byte-for-byte.
- **Offline:** controller focused Fast gate PASS on `9bbd6112` (components 1507/0, Validate-Source 153/0, focused 7 of 7:
  native contract 74/0, Mounted Archery donor 54/0, ranged movement 5/0, continuous drive 59/0, replay subscriber 21/0,
  Ranger selection 183/0, localization keys). DLL `3cc05893…`, MVID `9f36d10f…`.
- **Independent review:** three supplemental read-only Codex rounds, each CHANGES REQUIRED (5, 6 and 7 findings; all but
  PN3-F5 fixed, last in `9bbd6112`, which has not been reviewed again); see [docs/mounted-shield/PRE_NATIVE_REVIEW.md](docs/mounted-shield/PRE_NATIVE_REVIEW.md).
- **Native:** BLOCKED. UMM 0.33.0 installed against the 0.33.2 pin (owner decision) and no KMC Working fixture. No game
  launch, staging or install.
- **Read:** [docs/mounted-shield/HANDOFF.md](docs/mounted-shield/HANDOFF.md) and its
  [acceptance ledger](docs/mounted-shield/ACCEPTANCE_LEDGER.json).
- **Factory endpoint:** BLOCKED (developer report `blocked`; required native evidence), not AWAITING_OWNER_REVIEW.
- **Not authorized:** merge, release, install.

# RELEASE 0.2.15: SAVE-WAIT DIAGNOSTICS AND ACCURATE SAVE MESSAGES (2026-11-22)

- **Release:** 0.2.15, by owner order on 2026-11-22, from main after PR #59 (merged as `d409bffc`). Notes:
  [docs/RELEASE-NOTES-0.2.15.md](docs/RELEASE-NOTES-0.2.15.md). The package identity is recorded on the GitHub release.
- **Version:** 0.2.15 in `version.json`, `Info.json` and `BuildIdentity.cs`; the assembly version stays 0.1.0.0.
- **Content:** X05's diagnostics and accurate save messages, plus records. Save format unchanged from 0.2.14.
- **Open:**
  - X05 itself (cause not established; needs an owner log with 0.2.15 or a disposable save);
  - nothing in 0.2.15 was run in the game, startup included;
  - the UMM 0.33.0/0.33.2 platform pin (owner decision).
- **Offline (release commit `804b85ba`, budget `release-0.2.15`):** Fast tier PASS in 1.22 minutes.
  - Components 1488/0, Validate-Source 153/0.
  - Focused gate `Test-SaveDoorLockFocused.ps1`, 2 of 2 children: wait regression 16/0 with 8/8 mutations killed;
    readiness equivalence against 0.2.14 over 1,024 states, 0 failures; persistence contracts 228/0.

  Log `artifacts\release-0.2.15\gate-fast-804b85ba.log` (sha256 `2c579740…`).
- **Package:** `KingmakerMountedCombat-0.2.15-release-diagnostic.zip`, SHA-256
  `0b650bb3dea5003e36606a217d1f2269f1b42b69050d35b5f1785bc18d228af8`. DLL
  `dc52d015ad6b4a80075ab22d8a1ddde6d4de52f09158e66f3c870d625becb515`, MVID `efc09c78-4831-424e-9aad-2d898d1cc88c`.
  Package validator 11/0. Built from `804b85ba`, log `artifacts\release-0.2.15\package-804b85ba.log` (sha256 `91e3e1a0…`).
- **Native:** not run, startup included.
- **Previous release:** `v0.2.14`, merge `61519031`.
- **Not done:** no install, staging or loader replacement.

# POST-COMBAT SAVE AND DOOR LOCK: DIAGNOSTIC CHECKPOINT (2026-11-22, BRANCH codex/mounted-save-and-door-lock)

- **Mission:** [docs/MOUNTED-SAVE-AND-DOOR-LOCK-MISSION.md](docs/MOUNTED-SAVE-AND-DOOR-LOCK-MISSION.md). Manual review
  procedure by owner direction (Claude develops; Howie takes the handoff to Pro). No controller enrollment.
- **Status:** diagnostic checkpoint plus accurate failure reasons, revised after the first Pro review (`0b4b8fa4`, four P2
  findings, all addressed). **Not a repair**, because the blocking condition is not identified.
  - All four quicksave requests entered the deferred wait, so readiness was false at each activation. The log does not
    identify the blocking condition, establish that it was the same across requests, or measure any wait.
  - Three requests then failed the wait's world/mode/loading check. On the ordinary path that is a disallowed mode or a
    manual loading screen, not a world change; which one is unknown. The fourth was still waiting when the log ends.
  - The save wait refuses ordinary command starts, door commands included: a source-supported explanation for the door
    symptom. Recovery after the wait is not yet verified in the game.
- **Changed:**
  - `SaveEffectsReady` is now a single evaluation returning the first refusing term and its owner (same terms, order and
    short-circuit);
  - each wait, timed from activation, reports changes of its blocking owner or refusing condition (bounded), and ends with
    the latest snapshot and its outcome (`ReadyToSerialize` only once native serialization started; faults are `Faulted`);
  - refusals name a menu/dialog/cutscene, a loading screen or a replaced world, and the blocker.

  No readiness, hold, timeout, ordering or save-format change.
- **Offline (round 2):**
  - Fast PASS: components 1487/0, Validate-Source 153/0.
  - Focused `Test-SaveDoorLockFocused.ps1`: wait regression 15/0 with 8/8 mutations; readiness equivalence against base
    1,024 states, 0 failures; persistence contracts 228/0.
  - The menu assertion fails on base `61519031` with the incident's text.
  - Three existing harnesses already fail on base, identically on the candidate.
- **Package (for the owner reproduction):** `KingmakerMountedCombat-0.2.14-save-door-lock-diagnostic-diagnostic.zip`, sha256
  `8aebca4c…`, DLL `49b91e73…`, MVID `8ba02420…`, from clean `3cd7975d`. Pro approved this checkpoint at `079f4e9e`; its
  non-blocking notes are fixed in `3cd7975d` (Fast PASS: components 1488/0, wait regression 16/0, 8/8 mutations).
- **Native:** not run (no qualified fixture; incident save not here; no startup smoke of this DLL yet).
- **Needed:** an owner log with this build (the `Mounted save wait` lines name the blocker) or a disposable copy of the save.
- **Read:** [docs/SAVE-AND-DOOR-LOCK-HANDOFF.md](docs/SAVE-AND-DOOR-LOCK-HANDOFF.md).

# RELEASE 0.2.14: TOOLBAR PLACEMENT, CAPITAL COMPANION SCREENS, DOWNED-TARGET ATTACK (2026-11-22)

- **Release:** 0.2.14, by owner order on 2026-11-22, from main after PR #57 (merged as `4e5c9c0c`). Notes:
  [docs/RELEASE-NOTES-0.2.14.md](docs/RELEASE-NOTES-0.2.14.md). The package identity (zip and DLL SHA-256, MVID,
  release-source commit, tagged merge) is recorded on the GitHub release.
- **Version:** 0.2.14 in `version.json`, `Info.json` and `BuildIdentity.cs`; the assembly version stays 0.1.0.0.
- **Content:** X02 (toolbar placement), X03 (capital companion screens) and X04 (explicit attack on an already-unconscious
  enemy), plus records. Save format unchanged from 0.2.13.
- **Open:**
  - none of the three was played in the game;
  - X04's original prone-target report is unreproduced;
  - the owner playtest steps are in [the handoff](docs/CITY-AND-PRONE-HANDOFF.md), section 12;
  - the UMM 0.33.0/0.33.2 platform pin (owner decision).
- **Offline (release commit `55ea6603`, budget `release-0.2.14`):** Fast tier 4/0 in 1.75 minutes: components 1473/0,
  Validate-Source 153/0, focused gate 8 of 8 children (41/41 mutations killed). Log
  `artifacts\release-0.2.14\gate-fast-55ea6603.log` (sha256 `ca83b567…`).
- **Package:** `KingmakerMountedCombat-0.2.14-release-diagnostic.zip`, SHA-256
  `8dbcebc9112a4eac5f0135a9a5b9713593c0aa41fc5d49295ee359031f95ee07`. DLL
  `b077dbf7594602d14acc3592d252bc41c414687c8c526d2e9466a61288e6efb1`, MVID `6a4f8ce8-aec4-484b-b249-36b41763276d`.
  Package validator 11/0. Built from `55ea6603`.
- **Native (startup only):** run `release-0.2.14-smoke-140` with the release DLL, packaged deterministically at
  `55ea6603` on the local alias branch `codex/mounted-combat-release-0.2.14-native` (zip `025dffff…`). Results:
  - game side PASS, all 17 mods `active=true` (Tabletop Rules Expansion 0.0.140 included), every exact-token patch
    installed, `failure=none`, no Mounted Combat exception;
  - harness verdict FAIL only on the UMM 0.33.0/0.33.2 platform pin (not re-pinned);
  - restoration exact: Mods digest `51a2e474…` and save digest `6a8a8ad1…` unchanged;
  - log copy `analysis-cache\release-0.2.14-native\output_log.txt` (`b47df211…`).

  Budget `release-0.2.14`: 3.23 of 90 minutes.
- **Previous release:** `v0.2.13`, merge `dbf63702`.
- **Not done:** no install, staging or loader replacement.

# CITY TOOLBAR, CITY COMPANION ACCESS AND PRONE RANGED TARGET (2026-11-21, PR #57; MERGED AND RELEASED IN 0.2.14)

Dated history: the state before the merge.

- **Branch:** `codex/mounted-city-and-prone-fixes` from main `dbf63702` (0.2.13). Development candidate only: no merge,
  release or install; native startup smoke only.
- **Mode (2026-11-22):** ordinary interactive Claude development under
  [the manual continuation mission](docs/MOUNTED-COMBAT-CITY-AND-PRONE-MANUAL-CONTINUATION-MISSION.md). The second
  controller enrollment stopped after its review round 3 (`changes_required`, ten findings). Its records are archived
  (`6c1a61d7`, records 115–126) and it is cancelled (PR comment `status-9-cancelled`). Claude answered all ten findings
  (source `e429d290`). The owner's Pro review of `74fa9931` closed seven at source review and kept three open (R2-F2,
  R2-F3, R2-F5). Claude corrected those three (`397fb226`). The second Pro review closed R2-F2, accepted the R2-F5
  correction in source and asked for an executed hook check, and kept R2-F3 open. Claude answered both (`2d0bde62`). The
  third Pro review accepted those in source and raised one new P2 (R6-F1, the pin's authority). Claude corrected it in
  source candidate `36f95d53`, where the focused Fast gate passes (8 children). The fourth Pro review (of `fb8f7a34`)
  closed R6-F1 in source and accepted the checkpoint for guarded native verification.
- **Native (2026-11-22):**
  - Candidate checks on `fb8f7a34`: the focused gate passes; `Test-Harness` 278/3 is baseline-identical; the F2 reader
    stops at T09.
  - Package `KingmakerMountedCombat-0.2.13-pr57-city-prone-native-diagnostic.zip` (`5033e156…`), whose DLL is the gate's
    `8de5a104…`.
  - One guarded startup smoke, `pr57-city-prone-smoke-1`: game side PASS, all 17 mods active, every new patch installed;
    harness FAIL only on the UMM 0.33.0/0.33.2 pin; restoration exact.
  - No gameplay case ran. No qualified KMC Working fixture exists here, and no driver covers the three outcomes. They stay
    owner-playtest PENDING.
  - Budget `development` 79.63 of 90 minutes.
- **Fixes:** Mount / Dismount keeps the player's toolbar arrangement through temporary absence (area changes, the
  capital's reserve roster, saves there). One production component, `MountedToolbarLifecycle`, owns every toolbar
  boundary; `NativeMountedControlService` is its narrow adapter. The capital's Inventory/Character/Spellbook roster
  lists reserve owners' own animal companions (a UI projection of `UIUtility.GetGroup`). The identified rejection of an
  explicit mounted attack on a living target that was already unconscious is corrected in source. The original
  prone-target report remains unreproduced; whether the reported target was conscious and prone or already unconscious
  remains unknown. Save format unchanged.
- **Read:** [the handoff and acceptance record](docs/CITY-AND-PRONE-HANDOFF.md). Gameplay cases are deferred to owner
  verification there, with the playtest steps and the exact missing native prerequisites (section 11).

# RELEASE 0.2.13: TABLETOP RULES EXPANSION STARTUP CONFLICT (X01) FIXED (2026-11-20)

- **Release:** 0.2.13, by owner order on 2026-11-20, from main after PR #55 (merged as `a4d25e45`). Notes:
  [docs/RELEASE-NOTES-0.2.13.md](docs/RELEASE-NOTES-0.2.13.md). The package identity (zip and DLL SHA-256, MVID,
  release-source commit, tagged merge) is recorded on the GitHub release.
- **Version:** 0.2.13 in `version.json`, `Info.json` and `BuildIdentity.cs`; the assembly version stays 0.1.0.0.
- **Content:** only the X01 repair (below) and records. Save format unchanged from 0.2.12.
- **Previous release:** `v0.2.12`, merge `12527350`. It does not load beside Tabletop Rules Expansion.
- **Native (startup only, beside the installed Tabletop Rules Expansion 0.0.140):** the release DLL `197053e3…`
  (MVID `401084a4…`), packaged deterministically at the release commit on a local `codex/mounted-combat-*` alias
  branch, run `release-0.2.13-smoke-140`: game-side PASS, all 17 mods `Active`, `failure=none`, no structural
  exception; harness verdict FAIL only on the UMM 0.33.0/0.33.2 platform pin (owner decision; not re-pinned);
  restoration exact. The reviewed repair build gave the same result (`gunslinger-click-compat-smoke-140b`). Not
  verified: 0.0.191 natively, the owner's `Quicksave2`, mounted-click and firearm gameplay.
- **Not done:** no install, staging or loader replacement.

# 0.2.12 STARTUP CONFLICT WITH TABLETOP RULES EXPANSION: REPAIRED, MERGED AS PR #55 (2026-11-20)

- **Defect (release-blocking, in released 0.2.12):** with Tabletop Rules Expansion (mod id `KingmakerGunslinger`)
  installed, 0.2.12 is `Not loaded`: both mods transpile `ClickUnitHandler.OnClick`'s command submissions and
  whichever runs second throws. A save that references Mounted Combat content (toggle
  `3c81f5a02be74d96a4e07d1c92b6f385`) then fails to load in that session. Reproduced offline with the released 0.2.12
  DLL and Tabletop Rules Expansion 0.0.191 (and 0.0.140). 0.2.11 and earlier are not affected (prefix only).
- **Repair:** branch `codex/gunslinger-click-approach-compat` from `12527350` (`v0.2.12`), PR #55, merged into main
  as `a4d25e45` and released in 0.2.13.
  The transpiler guards the submission instead of replacing it; both submission shapes are recognised exactly; both
  Harmony orders compose to the same method. Read the
  [repair handoff](docs/GUNSLINGER-CLICK-COMPAT-REPAIR-HANDOFF.md).
- **Review:** accepted for verification, no blocking defect; the `Validate-Source.ps1` exception (exactly one
  constant line naming the other mod's type, asserted unique and confined) is accepted.
- **Evidence:** new `Test-GunslingerClickComposition.ps1` (real transpilers of both mods through the installed loader,
  all four orders, executed composed slice): released 0.2.12 fails with the incident's exception; the repair passes
  21/0 after one independent review (no blocking findings; corrections in `80dc8f78`). Fast 5/0; Candidate checks
  pass apart from baseline-identical or evidence-absent failures (Test-Harness 278/3 = baseline). Package
  `KingmakerMountedCombat-0.2.12-gunslinger-click-compat-diagnostic.zip` (SHA-256 `c4b9de66…2cec3`, DLL `5d79c78c…`).
- **Native startup (installed Tabletop Rules Expansion 0.0.140, not the incident's 0.0.191):** run
  `gunslinger-click-compat-smoke-140b` with the reviewed DLL `5d79c78c…`: game-side PASS, both mods `Active`,
  `failure=none`, no structural exception. Harness verdict FAIL only on the platform pin: this machine runs UMM 0.33.0,
  the fingerprint pins 0.33.2 (not changed; owner decision). Restoration exact. The empty `runtime-backups` and
  `runtime-staging` roots were created first (cleared lab, no interrupted transaction).
- **Not verified:** native startup with 0.0.191 (staging it needs an owner-approved pinned input in the launcher's
  retained-dependency guard), the owner's `Quicksave2` recovery (save not available), mounted click and firearm
  gameplay. The composition evidence substitutes ground admission and native command execution.
- **Not done:** no permanent installation.

- **Release:** 0.2.12, by owner order on 2026-11-19, from the merged Known Issues Remediation Epic (PRs #45–#53, merged
  into main as `446985b7`). Notes: [docs/RELEASE-NOTES-0.2.12.md](docs/RELEASE-NOTES-0.2.12.md). The package identity
  (zip and DLL SHA-256, MVID, release-source commit, tagged merge) is recorded on the GitHub release.
- **Version:** 0.2.12 in `version.json`, `Info.json` and `BuildIdentity.cs`; the assembly version stays 0.1.0.0. UMM's
  `ParseVersion` compares integer parts, so 0.2.12 reads newer than 0.2.11.
- **Previous release:** `v0.2.11`, historical release merge `7a5cd2c3`, built from `86ad9cc1`. (`22945394` is the
  earlier merge of the mounted-attack-stall repair, PR #43.)
- **Save change:** a save made while a pair waits to remount after a world-map arrival writes `kmc-retained-intent`
  version 2; 0.2.3–0.2.11 load that pair dismounted. Every other save is written as before.
- **Read:** the [epic closeout](docs/KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md), the
  [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md) and [current support, removal and rollback](docs/CURRENT-SUPPORT.md).
- **Engineering complete offline, now released:** T01–T03, S02, M01–M03, M05, C02, C04, A01, A02, A04 (automatic
  churn), A05 and U01–U04; M04's investigation (all identified marker defects repaired); R01 (not reproducible), A03
  (explanation settled); D01–D03.
- **Documented intentional behaviour:** C03's safe bounds (its notice repaired), R02's ranged rule, A04's real-time wait.
- **Unresolved:** S01 (saving report after the Ratnook door, cause not established); M04's original incident (not
  attributable); C01 (in-combat scripted rider movement, unsupported with its blocker); T04–T08 (stale harnesses, open
  correction); T09 (needs retained native evidence, open investigation).
- **Excluded:** the epic's manual/native acceptance list and feature expansions (Cavalier, barding, enemy mounted AI,
  exotic mounts, full celestial template, Run, Mythic Mounted Archery, real-time ranged rule, general content removal,
  six-pair combat).
- **Not run in the game before release.** No known-issues fix, gamepad session or interface screen was checked
  natively. Owner/HUMAN PLAY acceptance remains pending.
- **Package limit (owner decision):** packaging first failed: the DLL (9,488,896 bytes) exceeded the validator's 9 MiB
  DLL entry bound, which the epic's growth had crossed (0.2.11 was 9,352,704). The owner raised the bound to 10 MiB in
  `scripts/Validate-Package.ps1` and its harness guard; the 5 MiB ZIP bound, allowlist and hashes are unchanged.
- **Not done:** no install, staging or loader replacement.

# KNOWN ISSUES REMEDIATION EPIC MERGED INTO MAIN, BEFORE RELEASE 0.2.12 (2026-11-19); RELEASE THEN v0.2.11; NATIVE NOT RUN

Dated history: the state after the integration and before release 0.2.12.

- **Release:** the latest release is still `v0.2.11`: historical release merge `7a5cd2c3` (2026-11-15), package built
  from release-source commit `86ad9cc1`. (`22945394` is the earlier merge of the mounted-attack-stall repair, PR #43.)
- **Main:** contains the merged Known Issues Remediation Epic after `v0.2.11`. PRs #45–#52 were merged in order on
  2026-11-19 with ordinary merge commits (#45 `faf1195d`, #46 `1fb225a1`, #47 `3d51199e`, #48 `c458042d`, #49 `4342596e`, #50 `1c88d070`, #51 `dd52f359`, #52 `4b2c0a52`); PR #53 delivers this documentation and closeout and lands after them.
  No new version or release was made: main still declares 0.2.11 and assembly version 0.1.0.0, and a build of main is
  a different binary from the `v0.2.11` package. Main's product source is the accepted Sprint 7 product build
  `e54009e9`.
- **Release status of main:** unreleased, not release-qualified, native acceptance absent.
- **Read:** the [epic closeout](docs/KNOWN-ISSUES-REMEDIATION-EPIC-CLOSEOUT.md) (identity, package accounting,
  integration, validation, unresolved and excluded work), the [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md) (one
  disposition per ID) and [current support, removal and rollback](docs/CURRENT-SUPPORT.md).
- **Engineering complete offline:** T01–T03, S02, M01–M03, M05, C02, C04, A01, A02, A04 (automatic churn), A05 and
  U01–U04; M04's investigation (all identified marker defects repaired); R01 (not reproducible), A03 (explanation
  settled); D01–D03.
- **Documented intentional behaviour:** C03's safe bounds (its notice repaired), R02's ranged rule, A04's real-time wait.
- **Unresolved:** S01 (saving report after the Ratnook door, cause not established); M04's original incident (not
  attributable); C01 (in-combat scripted rider movement, unsupported with its blocker); T04–T08 (stale harnesses, open
  correction); T09 (needs retained native evidence, open investigation).
- **Excluded:** the epic's manual/native acceptance list and feature expansions (Cavalier, barding, enemy mounted AI,
  exotic mounts, full celestial template, Run, Mythic Mounted Archery, real-time ranged rule, general content removal,
  six-pair combat).
- **Native:** NOT RUN by any known-issues sprint or by the integration; no Sprint 7 screen viewed. Owner/HUMAN PLAY
  acceptance of the releases remains pending.
- **Not authorized:** release, tag, version bump, package, install, staging, loader replacement, reviewer request or a
  further sprint.

# KNOWN ISSUES REMEDIATION, SPRINT 7 (CORRECTED AFTER THE REVIEWS OF 292CD23C, E7720F39 AND 89ACE80F) — MOUNTED PRESENTATION: U01-U04 IMPLEMENTED AND VERIFIED OFFLINE; NATIVE NOT RUN, NO SCREEN VIEWED (2026-11-19)

Branch `codex/kmc-known-issues-sprint7` (draft PR stacked on `codex/kmc-known-issues-sprint6`, PR #51), from the
accepted Sprint 6 checkpoint `f835c503`. Read the [Sprint 7 handoff](docs/KNOWN-ISSUES-SPRINT7-HANDOFF.md) and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Presentation only:** every change projects current authoritative state onto native UI. No action, cost, cooldown,
  `TimeMoved`, allocation, command, routing, initiative, scheduling, activation, saved identity, GUID, fact, grant,
  progression, save format, selection or combat participation changes.
- **U01:** after forced (charge) travel recorded on the mount's allocation for the exact live activation, the mount's
  Move and Standard prediction actions show 0 usable distance and no five-foot step is promised. Native's Unable state
  and every cost are unchanged; the next activation is native again.
- **U02:** the exact public slot whose click handed native target selection to a hidden delivery ability is drawn with
  native's own selected mark, through a weakly held value-identity mapping that ends with the selection operation and at
  every lifecycle boundary.
- **U03:** the Paladin class panel omits the weapon-bond upgrades for a Divine Mount character or a bond not yet chosen,
  from projected copies of the shown descriptor's progression data. Grants and data unchanged.
- **U04:** a mounted rider's party portrait carries the embedded saddle badge; a folded mount's tracker hover
  highlights its own rider's existing entry. No entry, order, scheduler or selection change.
- **Review of `292cd23c` (`92310c52`):**
  - R1: the hover is the world hover of the mount's view (a portrait hover never reaches the tracker, installed IL).
    KMC's redirect is one owned lease of weak references and identities, released exactly before every native hover
    pass and at a tracker rebuild, its own pair's cleanup (R5), generation change, load, area unload, world discard,
    disable and disposal. Native's own pass already clears an ordinary true-then-false; it ran detached under the real patches.
  - R2: the badge follows the native per-frame portrait refresh (`UpdateCrippled`) and bind (`Initialize`), not
    `HealthUpdate` (hit-point changes only, withdrawn), and is removed at load, area, world discard, disable and
    disposal; failed removals and a throwing logger never stop cleanup.
- **Review of `e7720f39` (`6ff46a2a`):**
  - R3: releasing the hover redirect is a bounded, weak cleanup obligation. Clearing `HoveredUnit` and the rider row's
    false run independently; the obligation is kept until each completes, its target is gone or replaced, or native's
    own completed pass recomputes that tracker, and is retried at every hover prefix, rebuild, pair cleanup, boundary,
    disable and disposal.
  - R4: a badge whose removal fails is hidden and stays owned as a residue under the same bound of 32, retried at every
    portrait refresh and boundary, never reused as a portrait's badge; a part-built badge is owned from birth.
- **Review of `89ace80f` (`e54009e9`):**
  - R5: a pair's cleanup ends the active hover redirect only when it is exactly the redirect's pair (owner, generation,
    rider, mount, read from the relationship); any other pair's cleanup, or one whose identity cannot be read, only
    retries owed cleanup. Load, area unload, world discard, disable and disposal still release it whatever its pair.
- **T09 (new):** `Test-Finalize2UpgradeReaders.ps1` needs retained r05 native evidence absent from this lab; identical
  at the base.
- **Checks:**
  - Fast PASS 21/0 (0.91 min) (budget `known-issues-sprint7`, 4.63 of 90 min).
  - Components 1411/0; presentation contracts 239/0. The reviewed `6ff46a2a` fails the 7 unrelated-pair R5 cases.
  - Kingmaker assembly contracts 815/0; Validate-Source 151/0; harness 277/4, identical to the Sprint 6 base.
  - Mutation campaign P1-P34: 34/34 caught (serial, isolated).
  - Product-build commit `e54009e9`, DLL SHA-256 `851c44d6473210afd82630f33871356681460f2337b95a63a843b8849a7df21d`, MVID `6551bd65-dc7e-4ba5-bbfb-3bf84ed277c2`.
- **Native:** NOT RUN, and no screen was viewed. Nothing was launched, staged or installed. Sprint 8 was not started.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 8.

# KNOWN ISSUES REMEDIATION, SPRINT 6 (CORRECTED AFTER THE REVIEWS OF 2AE4CC8B, FA533E97, DFFD3E72 AND 95AE0B30) — ATTACK REQUESTS AND TERMINALS: A01/A02/A04/A05 CORRECTED, A03 SETTLED; NATIVE NOT RUN (2026-11-18)

Branch `codex/kmc-known-issues-sprint6` (draft PR stacked on `codex/kmc-known-issues-sprint5`, PR #50), from the
accepted Sprint 5 checkpoint `772543e1`. Read the [Sprint 6 handoff](docs/KNOWN-ISSUES-SPRINT6-HANDOFF.md) and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **A01:** a same-target click after the dispatched command ended is a fresh request (a new generation and its own
  answer). A click while it runs still continues.
- **A02:** a typed end cause, or an exact native capability branch, decides whether a condition explains a terminal:
  Stop, replacement, boundaries, faults and native non-capability ends are no longer "cannot act". Terminal logging
  and warning delivery are contained.
- **A03:** settled from decoded assets. Fascinate is `FascinateEffectCombatBuff` (Dazed) from the Lady of Shallows'
  aura, read by the current reader. Serenity adds no condition. No product change; live state not observed.
- **A04:** automatic churn for a `CanNotAttack` rider established and refused (stateless, automatic only). The
  player's real-time wait is unchanged.
- **A05 (new):** only a typed `ApproachFailed` end, recorded at the exact movement boundary with the approach's own
  evidence, marks an automatic target unreachable.
- **Review of `2ae4cc8b` (`ce9b1383`):**
  - every command end records a typed cause (audit table in the handoff);
  - start and tick faults record before any diagnostic, and the terminal is always delivered;
  - the capability boundary's exact block is the durable cause;
  - native `Run` replacement scopes are token-exact and exception-safe (a transpiler and a `finally`);
  - the A01 counter counts only the same request after its command ended.
- **Review of `fa533e97` (`1d0054f0`):**
  - R18: a native end is a capability end only on the exact native branch that took it (turn-based cannot-act,
    running cannot-act, attack ban), recorded by capture-and-wrap transpilers on `TickCommandTurnBased`,
    `TickCommand` and `UnitAttack.ShouldBeInterrupted`; the terminal snapshot is diagnostic only;
  - R19: only the innermost open native-Run token on the ending command's container decides a replacement;
  - R20: the retired scheduler records fault state, the typed cause and the interrupt before any diagnostic; the
    terminal handler and charge cleanup are no-throw.
- **Review of `dffd3e72` (`0e1125fa`):**
  - R21 (review-discovered lifecycle defect): the native terminal facts live in the native invocation's own locals;
    no static owns a command;
  - R22: the retired scheduler's lease release is deferred, as designed: the lease stays only while the finished
    command holds the mount's native slot, and the next update after slot removal, relationship cleanup or disposal
    releases it. The earlier "always releases" wording is withdrawn.
- **Review of `95ae0b30` (`7898b1a4`):**
  - R23: the CanNotAttack term of each wrapped `ShouldBeInterrupted` call has its own scalar frame (opened and closed in
    a `finally` around native's one getter call), so nested and exceptional observations cannot exchange terms;
  - R24: a failed terminal observation faults the scheduler lease and records the scheduler's interrupt bookkeeping; it does not re-end the already-finished command, interrupt it again, or replace its existing terminal cause.
- **Checks:**
  - Fast PASS 13/0 (budget `known-issues-sprint6`).
  - Components 1399/0; stall-repair probe 273/0. The Sprint 5 build fails 19 Sprint 6 sections; the reviewed
    `0e1125fa` build fails 4 (R23 behaviourally).
  - Kingmaker assembly contracts 815/0.
  - Mutation campaign Q1–Q26: 26/26 caught (serial, isolated).
  - Product-build commit `7898b1a4`, DLL SHA-256 `7da6d2c006ff3d71b9d51e29db729a514c6beda826f4611768c03347581dbc64`, MVID `19fba84e-efe2-4a0f-a82c-da26f79e60a4`.
- **Sprint 5 note:** the `MountedRangedMovementTests.Lifetime` wording now describes a temporary applicability gate,
  not a dismount and reattachment. Ranged-movement behaviour is unchanged.
- **Native:** NOT RUN. Nothing was launched, staged or installed. Sprint 7 was not started.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 7.

# KNOWN ISSUES REMEDIATION, SPRINT 5 — MOUNTED RANGED MOVEMENT: R01 NOT REPRODUCIBLE, R02 DOCUMENTED; NATIVE NOT RUN (2026-11-17)

Branch `codex/kmc-known-issues-sprint5` (draft PR stacked on `codex/kmc-known-issues-sprint3-closeout`, PR #49), from
the accepted closeout head `af5b2bc7`. Read the [Sprint 5 handoff](docs/KNOWN-ISSUES-SPRINT5-HANDOFF.md) and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **R01:** closed as not reproducible.
  - Under the qualified paired authority a mounted pair's mount never moves on its own initiative slot while carrying
    its rider: the candidate is suppressed, the only split is the dismount's, a combat mount forms only on the rider's
    turn, and a retained partner is ended.
  - A mount's own `native:` record holds only riderless or pre-mount travel, and stays excluded.
  - No separate-initiative authority was built (it would reintroduce the riderless-travel defect). No product change.
- **R02:** intentional adaptation documented: the exact rule (3.25 s, −4 / −2, per roll, turn-based only, steps and
  Charge excluded, no Run, no midpoint, no Mythic, native floor, log labels). README clarified (steps and Charge).
- **Checks:**
  - Fast PASS 8/0 (0.82 min) (budget `known-issues-sprint5`).
  - Components 1387/0; ranged movement contract 5/0; Kingmaker assembly contracts 815/0.
  - Mutations S1–S11, each caught.
  - Product build unchanged at `cdfbe509`, DLL SHA-256 `f584c1c6e0678a9447a6082552dda57c5fcc4a95e475430c20fb5a93210f5035`, MVID `6cb7b950-e97c-4c14-9d83-764b074a3439`.
- **Native:** NOT RUN. Nothing was launched, staged or installed. Sprint 6 was not started.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 6.

# KNOWN ISSUES REMEDIATION, SPRINT 3 CLOSEOUT — M04 DESTINATION-MARKER OWNERSHIP: ENGINEERING COMPLETE; NATIVE NOT RUN (2026-11-17)

Branch `codex/kmc-known-issues-sprint3-closeout` (draft PR stacked on `codex/kmc-known-issues-sprint4`, PR #48), from
the accepted Sprint 4 head `14f1b474`. Read the [closeout](docs/KNOWN-ISSUES-SPRINT3-CLOSEOUT.md) and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **M04:** engineering investigation complete; all identified product marker defects repaired. The original incident
  is not attributable; native and manual verification remain.
- **New repairs (`c64c695f`, corrected by `cdfbe509` after the review of `b1a88b4e`):**
  - The mount's door approach no longer asks for a marker (a native door click shows none).
  - The native cannot-attack click approach a mounted rider was given (`ClickUnitHandler.OnClick`'s one `UnitMoveTo`)
    goes to the mount through the ground-order lifecycle. Before, it stayed on the rider with its marker visible,
    under later orders too.
  - Its authority is the one native call site: a transpiler replaces only that `Run`. The first version's frame-scoped
    flag is withdrawn: it authorised unrelated, nested, post-throw and other-pair rider moves.
  - The mount runs the original native command, never `CreatedByPlayer`, recognised by identity in the ground order.
    The first version's six-field copy is withdrawn: its native equivalence was never established, and it forced
    `CreatedByPlayer` true.
  - Placements are recorded at `AddPointer` itself. The first version's inference from a visible pointer after
    `OnMovementStarted` is withdrawn.
- **Audit:**
  - The native pointer contract is pinned.
  - All nineteen native creators of marker-capable commands are classified.
  - The complete product inventory has six play sites, none of which asks for a marker. The click-approach routing
    builds no command.
- **Observation:** `MountedMarkerOwnershipPolicy` classifies each pair's pointers. One observer line is written per
  anomaly transition, contained, and cleared at boundaries.
- **Checks:**
  - Fast PASS 13/0 (0.59 min) (budget `known-issues-sprint3-closeout`).
  - Components 1385/0; marker ownership contract 18/0 (reviewed `b1a88b4e` build 10/8, accepted Sprint 4 build 10/8).
  - Kingmaker assembly contracts 815/0.
  - Six closure and eleven review mutations, each caught.
  - Product-build commit `cdfbe509`, DLL SHA-256 `f584c1c6e0678a9447a6082552dda57c5fcc4a95e475430c20fb5a93210f5035`, MVID `6cb7b950-e97c-4c14-9d83-764b074a3439`.
- **Sprint 3 engineering is closed.** S01 and T04–T08 keep their dispositions. Sprint 5 was not started.
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 5.

# KNOWN ISSUES REMEDIATION, SPRINT 4 — REVIEW OF 813d00a1: R10 SCENE OWNERSHIP CORRECTED OFFLINE; NATIVE NOT RUN (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48, stacked on `codex/kmc-known-issues-sprint3` at `e5b92474`),
continued from the reviewed head `813d00a1`. Read the [handoff](docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md), its R10 section
first, and the [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Accepted at `813d00a1`:** R4, R7 (when scene-owned), R8, R9, C04's observations and C01.
- **R10, C02 (`03685720`):** scene ownership was the global mode read after the operation, so a live background
  cutscene's operations in Default play were swallowed.
  - The commands' prefixes now take the original `CutscenePlayerData player`.
  - `NativeSceneOperationOwnership` decides at entry whether a live player has this exact command current on an
    unfinished started track.
  - For `TranslocateUnit`, it decides whether the action runs inside a cutscene's parameter scope.
  - The typed capture stores the decision, and the core consumes it.
  - Background move, rider-only teleport and standalone turn are now each held and handed off once.
  - Null, finished, replaced and unrelated players are not owned.
- **Checks:**
  - Fast PASS 10/0: components 1370/0, Validate-Source 151/0, scene placement 24/0 (reviewed build 21/3), Candlemere
    11/0, and five neighbours.
  - Kingmaker assembly contracts 815/0.
  - Three targeted mutations, each caught.
  - Product-build commit `03685720`, DLL SHA-256 `7ce8f88a…01efd59c8`, MVID `efdc12b6-…`.
- **Remaining limit:** an action run by a non-cutscene handler inside a cutscene's open scope counts as the
  cutscene's.
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Unchanged:** S01, M04 and T04–T08; accepted Sprint 3 behaviour; C01's guards.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 5.

# KNOWN ISSUES REMEDIATION, SPRINT 4 — REVIEW OF e1c0c7ad: R7, R8, R9 CORRECTED OFFLINE; NATIVE NOT RUN (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48, stacked on `codex/kmc-known-issues-sprint3` at `e5b92474`),
continued from the reviewed head `e1c0c7ad`. Read the [handoff](docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md), its R7–R9
section first, and the [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Accepted at `e1c0c7ad`:**
  - R4: owed versus spent, the no-receiver retry and the loading gate;
  - R5's supersession while a placement is pending;
  - R6;
  - C04's observations;
  - C01.
- **R7, C02 (`ed83339c`):** an exact rider-only turn with nothing pending was discarded. It is now held at the rider's
  position with the authored orientation and handed off once. In combat: one refusal and one return to the saddle.
- **R8, C02:** the installed `CommandTeleportParty.OnRun` writes `Position` and `Orientation` to every unit of
  `Player.GetCharactersList(m_UnitsList)`, unconditionally.
  - Its prefix now records whether the exact rider and mount are in that set.
  - A proven write of both is whole-pair even when one actor was already at its authored transform.
  - Single-unit operations are unchanged.
- **R9:** the C02 attribution and the C04 zone pass now run through Domain loops. Their success logging cannot
  truncate the remaining pairs or zones.
- **Records:** `DeliveredCount` counts notice deliveries with at least one successful receiver. R5 is complete only
  with R7 and R8.
- **Checks:**
  - Fast PASS 10/0: components 1367/0, Validate-Source 151/0, scene placement 19/0 (reviewed build 12/7), Candlemere
    11/0 (reviewed build 9/2), and five neighbours.
  - Kingmaker assembly contracts 815/0.
  - Four targeted mutations, each caught.
  - Product-build commit `ed83339c`, DLL SHA-256 `e3448787…a390c95bc5`, MVID `c94292d4-…`.
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Unchanged:** S01, M04 and T04–T08; accepted Sprint 3 behaviour; C01's guards.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 5.

# KNOWN ISSUES REMEDIATION, SPRINT 4 — REVIEW OF 263ac3ea: R4, R5, R6 CORRECTED OFFLINE; NATIVE NOT RUN (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48, stacked on `codex/kmc-known-issues-sprint3` at `e5b92474`),
continued from the reviewed head `263ac3ea`. Read the [handoff](docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md), its R4–R6
section first, and the [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Accepted at `263ac3ea`:** C04's mounting-instance observations, C02's different-endpoint supersession and
  whole-pair clear, C03's deduplication and native warning interface, and C01's unsupported disposition.
- **R4, C03 (`e62cad39`):** the notice was spent before any receiver existed.
  - Native `EventBus.RaiseEvent` returns nothing and invokes the action once per subscribed receiver.
  - The warning UI subscribes only on `OnAreaDidLoad`, in the last loading step.
  - The notice is now never attempted during a load, and spent only once a receiver was reached.
  - `DeliveredCount` counts notice deliveries with at least one successful receiver.
- **R5, C02 (`e62cad39`):** the installed `TranslocateUnit` (copy rotation) and `CommandTeleportParty` write
  `Orientation` without any position comparison.
  - The capture and classification now see turns, and a same-endpoint placement's final orientation stands.
  - A rider-only turn alone started nothing (swallowed; corrected by R7 of the review of `e1c0c7ad`).
- **R6 (`e62cad39`):** C04 and C02 hooks have separate bounded fault reports.
- **Checks:**
  - Fast PASS 10/0 under budget `known-issues-sprint4`: components 1362/0, Validate-Source 151/0, scene placement
    contracts 14/0 (reviewed build 8/6), Candlemere 9/0, and five neighbours.
  - Kingmaker assembly contracts 815/0.
  - Three targeted mutations, each caught.
  - Product-build commit `e62cad39`, DLL SHA-256 `c1bec351…1dfa4728`, MVID `9b5abe93-…`.
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Unchanged:** S01, M04 and T04–T08 keep their dispositions; accepted Sprint 3 behaviour and C01's guards are
  untouched.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 5.

# KNOWN ISSUES REMEDIATION, SPRINT 4 — REVIEW REPAIR OF b30c24f8: R1, R2, R3 CORRECTED OFFLINE; C01 STILL UNSUPPORTED; NATIVE NOT RUN (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR #48, stacked on `codex/kmc-known-issues-sprint3` at `e5b92474`),
continued from the reviewed head `b30c24f8`. Read the [handoff](docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md), its review
section first, and the [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **R1, C04 (`60ba91ae`):** zone observations were keyed by the rider and outlived a normal dismount, whose cleanup
  clears the references the adapter read first. A remount at the place last checked kept the stale membership. They
  now belong to the mounting instance (`MountedScriptZoneObservations`, the coordinator's `MountedPair`).
- **R2, C02 (`3cbdf077`):** the first of two placements before the poll won. `MountedScenePlacementHold` now keeps the
  scene's latest placement (endpoint and orientation), clears a rider-only one on a whole-pair placement, and applies
  the unchanged 3 m rule to unattributed writes since the hold, at sample and poll.
- **R3, C03 (`fb7f048d`, `b80534f9`):** the notice went to a Report nothing displays. `MountedCombatHoldNotifier` now
  delivers it through the native warning notification. Executed through the installed native `EventBus` to a
  subscribed sink: once per custody and encounter, and contained on failure.
- **C01:** still explicitly unsupported. The wording is narrowed: the staging lifecycle cannot take an actively
  participating pair out of combat with its restoration intent under its present contract, and other designs are not
  excluded.
- **Checks:** Fast PASS 10/0 under budget `known-issues-sprint4` (components 1356/0, Validate-Source 151/0, both
  Sprint 4 contract scripts 9/0 and the five neighbours). Kingmaker assembly contracts 815/0. Six targeted mutations
  each caught. Product-build commit `b80534f9`, DLL SHA-256 `70bbe6f7…a15099`, MVID `fc4bb6b3-…`.
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Unchanged:** S01, M04 and T04–T08 keep their dispositions; accepted Sprint 3 behaviour is untouched.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 5.

# KNOWN ISSUES REMEDIATION, SPRINT 4 — SCENE CONTINUITY: C04, C02 IMPLEMENTED OFFLINE; C01 UNSUPPORTED WITH ITS BLOCKER; C03 LIMITS DOCUMENTED; NATIVE NOT RUN (2026-11-17)

Branch `codex/kmc-known-issues-sprint4` (draft PR stacked on `codex/kmc-known-issues-sprint3`, PR #47, open), created
from the accepted Sprint 3 head `e5b92474`. Read the [handoff](docs/KNOWN-ISSUES-SPRINT4-HANDOFF.md) and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Sprint 3 accepted at `e5b92474`:** a review checkpoint of offline work. Native is not run. M04's original report,
  S01 and T04–T08 keep their dispositions.
- **C04 (`ef264ff3`), implemented and verified offline:** the crossing's Conditional reads the rider's script-zone
  membership, which native updates only for units with native motion. A seated rider's membership is now rechecked
  with native `ScriptZone.CheckUnit` (postfix of `ScriptZoneController.Tick`). The owner's `output_log(9).txt` is not
  on this machine and was not read.
- **C01, explicitly unsupported:** in combat the only lifecycle that keeps restoration intent refuses behind the
  settled-participation guard. The scene's command still waits for its own timeout; the rider then returns to the
  saddle once.
- **C02 (`eabbd8c8`), implemented and verified offline:** `TranslocateUnit`, a timed-out `CommandMoveUnit` and
  `CommandTeleportParty` attribute a rider-only write to the scene, so out of combat 0.5, 2 and 3 m placements keep
  their endpoint. The 3 m rule for unattributed writes is unchanged.
- **C03 (`9a371910`), intentional limit documented:** limits unchanged. Separate repair: the combat-hold notice is
  now once per custody and encounter, and reaches the player.
- **Checks:** Fast PASS 10/0 under budget `known-issues-sprint4` (components 1349/0, Validate-Source 151/0, seven
  focused scripts). Kingmaker assembly contracts 815/0. Product DLL SHA-256 `3ef14c3a…0558968`, MVID `495b82b0-…`.
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Not authorized:** merge, release, tag, version bump, install, loader replacement, reviewer request or Sprint 5.

# KNOWN ISSUES REMEDIATION, SPRINT 3 — OWNER REVIEW OF 119088ed: ARRIVAL OVERRIDE OWNERSHIP CORRECTED OFFLINE; NATIVE NOT RUN (2026-11-16)

Branch `codex/kmc-known-issues-sprint3` (draft PR #47), continued from the reviewed head `119088ed`, still on the
Sprint 2 head `1dffc3e0` and still targeting `codex/kmc-known-issues-sprint2` (draft PR #46, open). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT3-HANDOFF.md), its last section first, and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Accepted at `119088ed` (source review):** the registry, admission-discard and load-session correction.
  Unchanged.
- **The finding, corrected (`7a657838`, `29f987b8`, `f6e86858`):** the arrival's override-only exception took any
  installed mount override for the pair's own retained agent. A foreign agent (on the captured view, or on a
  replacement view while the captured agent was still held) turned a departing Fail into Defer, so another eligible
  pair restored, and gave a carried pair the cleanup placement.
  - Now two distinct facts are required. First, the mount's current view and override must be the captured view and
    agent of a retirement still owed by the pair's exact earlier owner (identity, generation, rider, mount). That is
    asked of that owner's own stick-movement owner (`RetainsCapturedAgent`; the residue keeps its binding generation).
    Second, that override must be the only refusal.
  - `OwnsMountOverride` (the invariant's live-drive question) is unchanged and still owns no residue.
- **Owned and foreign cases:**
  - The owned agent on its captured view defers, keeps its cleanup custody, and imports with its identity and
    generation afterwards.
  - Foreign B on the captured view, or on V2, keeps the departing Fail. With an eligible neighbour, the all-pair
    failure stands and neither pair is attached.
  - A carried pair with B keeps its ordinary Retain without the cleanup placement.
- **Checks:** Fast PASS 11/0 (components 1338/0, Validate-Source 151/0, continuous drive 59, physical mover pace 27,
  post-load view readiness 26, persistence 223, world boundary 8, paired activation, world ownership 29, command
  slot 24). Kingmaker assembly contracts 815/0.
  - The reviewed head fails 2 of the 59 continuous-drive checks. With its own compiled parts, its classification
    defers both foreign cases and grants the carried placement on B.
  - 11 targeted mutations removing parts of the ownership distinction, run in a separate worktree, each fail.
- **Executed vs modelled:**
  - Executed: the real world service, a registered owner, and the production ownership question.
  - Scripted: the native interrupt and end, and the views and agents.
  - Modelled and pinned from IL: the arrival's all-pair preflight and attachment.
- **Wording corrected:** an installed override does not prove ownership, and an ownership import is not a full
  restoration.
- **Open:** native acceptance of M01–M03 and M05; M04; T04–T08 unchanged.
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Next:** Sprint 4, ordered C04 (Candlemere's "wade through" failure), C01, C02, C03. Not started.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 4.

# KNOWN ISSUES REMEDIATION, SPRINT 3 — OWNER REVIEW OF fe22563a: WORLD BOUNDARIES AFTER THE GUARD CORRECTED OFFLINE; NATIVE NOT RUN (2026-11-16)

Branch `codex/kmc-known-issues-sprint3` (draft PR #47), continued from the reviewed head `fe22563a`, still on the
Sprint 2 head `1dffc3e0` and still targeting `codex/kmc-known-issues-sprint2` (draft PR #46, open). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT3-HANDOFF.md), its last section first, and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Accepted by the owner at `fe22563a`:** the lifecycle correction (retained stick cleanup in `Dismount`'s result,
  strict disable/shutdown/guard removal, recovery before the update gate, the pending runtime-error stop, owner
  retention, bounded retries). R2 stays accepted. Stationary rider actions stay deliberately preserved.
- **The remaining P2, corrected (`7fa9f74e`, `ee8cca11`; tests `56cae55f`, `1ad306ce`):** the world-boundary guard passed, but the
  admission discard right after it refused the pair's retained owner. Area suspension latched a failed transfer and
  load housekeeping threw.
  - The registry tells detached stick cleanup apart from what holds a world. Owners declare a second predicate: their
    obligations without the stick cleanup the boundary keeps with its exact references.
  - The admission discard accepts an owner that owes only that cleanup. The owner stays registered, holding its
    actors and identity, and the pair stays dismounted.
  - The arrival defers a pair still held this way (departing or carried), with its owner identity and arrival
    placement. Only Restore, Defer, or a refusal by its own retained agent defer it (ownership fact added at the
    review of `119088ed`); any other refusal keeps the
    all-pair failure, and the capital rule still retires.
  - The in-area retry restores it once the owner retires, with the same identity, generation, session and campaign.
    Until then the true reason is given.
  - A load sets such owners aside from the old session. The new world imports its pairs (same characters, same
    identities) under its own session. The old cleanup touches only old objects and retires its owner when done.
- **Unchanged:** disable, shutdown and guard removal are strict on every boundary; retries are bounded; voluntary
  dismount results stay strict (their wording is a non-blocking refinement left as it is).
- **Checks:** Fast PASS 11/0 (components 1337/0, Validate-Source 151/0, continuous drive 58, physical mover pace 27,
  post-load view readiness 26, persistence 223, world boundary 8, paired activation, world ownership 29, command
  slot 24). Kingmaker assembly contracts 815/0.
  - The reviewed head `fe22563a` fails 5 of the 58 continuous-drive checks. On the real world service with a
    registered owner, the admission discard after the guard refuses for the area, the load and the persistent case.
  - 37 mutations of the correction, run in a separate worktree, each fail the suite (one only after the IL pin of `1ad306ce`).
- **What ran, what was modelled:**
  - Ran: the real `MountedWorldService` offline, with its real registry and boundaries, the production import seam
    and the actual retained-obligation predicate.
  - Modelled and pinned from IL: the persistence service's area, arrival, retry, event and load sequence.
- **Review:** one independent read. No blocker; the major point (a held pair overrode unrelated refusals) and the
  minor ones were corrected.
- **Open:** native acceptance of M01–M03 and M05; M04; T04–T08 unchanged.
- **Native:** NOT RUN. Nothing was launched, staged or installed. No gamepad session has ever been run in this
  project.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 4.

# KNOWN ISSUES REMEDIATION, SPRINT 3 — OWNER REVIEW OF 25cbdbe2: R1 LIFECYCLE CORRECTED OFFLINE, R2 ACCEPTED; NATIVE NOT RUN (2026-11-16)

Branch `codex/kmc-known-issues-sprint3` (draft PR #47), continued from the reviewed head `25cbdbe2`, still on the
Sprint 2 head `1dffc3e0` and still targeting `codex/kmc-known-issues-sprint2` (draft PR #46, open). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT3-HANDOFF.md), its last section first, and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **R2: accepted** by the owner at `25cbdbe2`. Unchanged.
- **R1, the remaining finding, corrected (`f4369506`, `899fe393`):** the stick-movement owner kept a failed
  cleanup, but the lifecycle around it could report the cleanup complete and stop the only path that retried it.
  - `Dismount` now reports retained stick-movement cleanup in its result. The disable, the runtime-error cleanup,
    shutdown, Harmony guard removal, owner settlement and boundary reuse all read it and wait.
  - The root retries retained cleanup before its enabled gate, so recovery does not depend on the ordinary pair
    update. A runtime-error stop held back by this cleanup alone is taken again once it is established.
  - A readiness refusal (the charge barrier) still retries retained cleanup. No pre-gate that runs before the
    cleanup reads the obligation.
  - A record past the automatic limit still leaves once the facts establish its retirement. Nothing latches.
- **Retry rule (wording corrected):** three automatic attempts per failing retirement, counting the first.
  Settlements, the root's retry, new bindings and every dismount that can repeat per frame stay within that limit.
  Explicit releases (disable, shutdown, guard removal, a boundary, a voluntary dismount) retry deliberately.
- **For the owner to confirm:** area suspension, load and event suspension are **not** failed by a stick-only
  obligation (`GuardWorldBoundary`). Failing them would latch a failed area transfer or throw after the old world
  is unloaded. The reviewed head passed them too. Making them strict is three call sites.
- **Deliberately preserved (owner decision):** a stationary rider order runs on while the stick moves the mount.
  The native Move/Standard pairing is not mirrored onto the rider. Not an open question.
- **Unchanged:** the earlier R1 and R2 corrections (`e98eac31`), the M05 routing repair, follower acceleration, M01,
  M03, the two marker corrections, the bounded fault report, and C04 (first in Sprint 4; order C04, C01, C02, C03).
  M04's original report stays unresolved.
- **Checks:** Fast PASS 11/0 (components 1332/0, Validate-Source 151/0, continuous drive 52, physical mover pace 27,
  post-load view readiness 26, persistence contracts 223, world boundary, paired activation, world ownership,
  command slot 24). Kingmaker assembly contracts 815/0.
  - The reviewed head `25cbdbe2` fails 8 of the 52 continuous-drive checks. Its real `Dismount` returns a complete
    result with one obligation retained, for the disable and for the runtime-error cleanup.
  - Two failed Fast runs are kept. One caught a real conflict with an existing post-load contract (fixed in
    `899fe393` without touching that contract). The other is the ledgered T04, unchanged.
  - 35 mutations of the correction, run in a separate worktree, each fail the suite.
- **What ran and what was read:** the real `GameMountedRelationshipService` ran offline, unmounted, with its
  stick-movement owner on a scripted host: `Dismount`, `ValidateActivePair`, both guards, `Shutdown`, boundary
  recycling. The real ownership registry ran in the component tests. The composition root and the lifecycle
  subscriber cannot be constructed offline: their sequence is modelled and their IL is pinned.
- **Review:** one independent read. No blocker; two major points corrected (a per-frame dismount that would have
  retried deliberately; world boundaries latching a session failure), two minor ones applied.
- **Open:** native acceptance of M01–M03 and M05; M04; T04–T08 unchanged.
- **Native:** NOT RUN. Nothing was launched, staged or installed. No gamepad session has ever been run in this
  project.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 4.

# KNOWN ISSUES REMEDIATION, SPRINT 3 — OWNER REVIEW OF 5e27c9bd: R1 AND R2 CORRECTED OFFLINE; NATIVE NOT RUN (2026-11-16)

Branch `codex/kmc-known-issues-sprint3` (draft PR #47), continued from the reviewed head `5e27c9bd`, still on the
Sprint 2 head `1dffc3e0` and still targeting `codex/kmc-known-issues-sprint2` (draft PR #46, open). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT3-HANDOFF.md), its review section first, and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **R1, corrected (`e98eac31`):** retiring the pair's owned stick movement no longer clears a foreign override.
  - Native ends a continuous command by clearing the override of its executor's *current* view. The reviewed
    release could clear an unrelated override on a replaced mount view, and a command abandoned after its agent
    was replaced later cleared the foreign override.
  - Now a guard inside that native method lets a pair-bound command clear only the agent it was bound with, on the
    view it was bound on. The command and its captured agent are retired separately.
  - The record is kept, with its exact command, view and agent, until the retirement is established, and
    retried. A terminal command alone is not taken as proof that its override is gone.
- **R2, corrected (`e98eac31`):** a rider order admitted through native queue promotion now ends the stick
  movement, exactly once. Admission is observed after the internal `Run` that the public `Run` and promotion both
  reach. Enqueueing alone, a refused promotion, a Free order and another unit's order change nothing.
- **Kept on purpose:** a stationary rider Standard order runs on while the stick drives the mount, as beside a
  routed ground order. The native slot rules were read and pinned first; the test model, not the product, had
  been wrong.
- **Unchanged:** the M05 routing repair, follower acceleration, M01, M03, the two marker corrections, the bounded
  fault report, and C04 (first in Sprint 4; order C04, C01, C02, C03). M04's original report stays unresolved.
- **Checks:** Fast PASS 9/0 (components 1326/0, Validate-Source 151/0, continuous drive 43, physical mover pace 27,
  post-load view readiness 26, persistence contracts 223, world boundary 8, paired activation). Kingmaker assembly
  contracts 815/0.
  - The reviewed head `5e27c9bd` fails 12 of the 43 continuous-drive checks. Its compiled owner fails both required
    reproductions: the release clears the unrelated override, and the abandoned command later clears the foreign
    one.
  - 27 mutations of the corrections, run in a separate worktree, each fail the suite.
- **What ran and what was read:** the compiled owner ran both reproductions and the retry cases on a scripted host;
  the compiled guard ran with its command table; native queue promotion ran on a genuine `UnitCommands` container;
  the native interrupt and slot rules were read from IL. The guarded native write and the adapter's answers that
  need a live pair were not executed.
- **Review:** one independent read of the corrections. No blocker; its hardening points (re-entrancy, a record
  superseded while bound, forgetting finished commands) were applied.
- **Open:** native acceptance of M01–M03 and M05; M04; T04–T08 unchanged.
- **Native:** NOT RUN. Nothing was launched, staged or installed. No gamepad session has ever been run in this
  project.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 4.

# KNOWN ISSUES REMEDIATION, SPRINT 3 CONTINUATION — DIRECT GAMEPAD CONTROL OF A MOUNTED PAIR REPAIRED OFFLINE; NATIVE NOT RUN (2026-11-16)

Branch `codex/kmc-known-issues-sprint3` (draft PR #47), continued from the reviewed head `1223eb2f`, still on the
Sprint 2 head `1dffc3e0` and still targeting `codex/kmc-known-issues-sprint2` (draft PR #46, open). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT3-HANDOFF.md), continuation section first, and the
[current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Owner decision:** direct gamepad control of a mounted pair is in scope. M05 stays its own item.
- **M05: implemented and verified offline (`99b632f2`).**
  - The defect, by source: stick input for a mounted rider was refused by native `Run`, left the mount an
    override nothing owned, dismounted the pair and blocked remounting.
  - The correction: the stick drives the pair's mount, on which native initialises, runs, looks up, accelerates
    and ends its command. The mount's continuous agent is admitted by the mounted invariant only as the override
    of that exact command, for that pair generation, while it runs in the mount's Move slot.
  - Anything else is retired or still rejected: a command `Run` did not admit, combat, a new rider order,
    dismount and disposal each end the owned movement; a foreign or stale override is still a fault.
  - Also corrected, same root: gamepad ground orders now reach a mounted current unit that is not being driven.
- **M02: implemented and verified offline, in two parts** (a following pair, Sprint 3; a directly driven pair,
  through M05). M01, M03 and M04 are unchanged; M04's original report stays unresolved.
- **Fault report (`b65977d8`):** a logger that throws can no longer stop a hook from returning its native
  fallback.
- **C04 recorded (`8e4e3a69`):** the Candlemere crossing while mounted, open investigation, first in Sprint 4.
  Sprint 4's order is C04, C01, C02, C03. The owner's `output_log(9).txt` is not on this machine.
- **Checks:** Fast PASS 9/0 (components 1313/0, Validate-Source 151/0, continuous drive 32, physical mover
  pace 27, post-load view readiness 26, persistence contracts 223, world boundary 8, paired activation).
  Kingmaker assembly contracts 814/0.
  - At the reviewed head `1223eb2f`: components 1290/0; that build fails 20 of the new script's product
    checks, the throwing-logger one with the logger's own exception.
  - 30 mutations of the new production code, run in a separate worktree, each fail the suite.
- **What ran and what was read:** native `UnitCommands.Run`'s refusal was executed offline on genuine engine
  objects. The production transpilers, hooks, ownership code and fault report were executed. The rest of the native
  chain and the game adapter were read from IL. The component tests drive production code through a model of the
  native chain.
- **Review:** one independent read. It found a drive that would have lingered into turn-based combat and four
  smaller gaps; all were corrected, and the tests now execute the production ownership code instead of a copy.
- **Open:** native acceptance of M01–M03 and M05; M04; T04–T08 unchanged.
- **Native:** NOT RUN. Nothing was launched, staged or installed. No gamepad session has ever been run in this
  project.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 4.

# KNOWN ISSUES REMEDIATION, SPRINT 3 — MOUNTED PACE REPAIRED OFFLINE; MARKER REPORT UNRESOLVED; NATIVE NOT RUN (2026-11-16)

Branch `codex/kmc-known-issues-sprint3` (draft PR #47), created from the reviewed Sprint 2 head `1dffc3e0` and targeting
`codex/kmc-known-issues-sprint2` (draft PR #46, still open). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT3-HANDOFF.md) and the [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Scope:** M01–M04 of the owner's Known Issues Remediation Epic. Gameplay code changed: three native pace reads
  are answered with a mounted pair's mount, one native marker placement is skipped for a carried rider, and one
  internal move lost its marker request. A build of this branch is a new binary. No save format changed.
- **M01, entrance and exit orders: implemented and verified offline.** The mount's carried entrance command now
  gets the same native `Accelerate()` offer its selected rider's order gets. The carrier is issued as before.
- **M02, gamepad: implemented and verified offline for a following pair.** The native follower pass now reaches
  the mount that executes a following rider's order.
- **M05 (new at this head): the stick-driven mounted pair.** By source, stick input for a mounted rider moves
  nothing and dismounts the pair: native `Run` refuses the continuous command the mod redirected. Never observed;
  not repaired at this head. Repaired offline by the continuation above.
- **M03, group pace: implemented and verified offline.** The native group speed limit reads each pair's mount
  instead of its rider. The native minimum, the 30 ft floor and the selection are untouched, so a mover slower than
  30 ft is still bounded by its own speed while the party walks at the floor.
- **M04, the marker report: investigation complete, issue unresolved.**
  - The report's original log is not available and no cause is established.
  - Two adjacent defects were found in the installed marker contract and repaired: a second, rider-keyed marker
    during a carried entrance approach, and a marker under the rider for the scene regroup walk.
  - Neither is shown to be what the owner saw. Needed: when the marker appears, or a log of the native pointer
    table at that moment.
- **Checks:** Fast PASS 7/0 (components 1290/0, Validate-Source 151/0, physical mover pace 27, post-load view
  readiness 26, persistence contracts 223, world boundary 8). Kingmaker assembly contracts 814/0.
  - At the reviewed base `1dffc3e0`: components 1257/0; the base build fails 16 of the 27 pace contract checks.
  - Fourteen mutations of the new decisions and wiring, run in a separate worktree, each fail the suite.
- **Review:** one independent read of the diff. No production defect; its points on test strength, a missing
  generation check and a retained reference were corrected or recorded (the handoff lists each).
- **Open:** M04, M05; T04–T08 unchanged (none blocked a Sprint 3 check).
- **Native:** NOT RUN. Nothing was launched, staged or installed. No fixture exists on this machine.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 4.

# KNOWN ISSUES REMEDIATION, SPRINT 2 — POST-LOAD DISMOUNT REPAIRED OFFLINE; SAVING REPORT UNRESOLVED; NATIVE NOT RUN (2026-11-16)

Branch `codex/kmc-known-issues-sprint2` (draft PR #46), created from the reviewed Sprint 1 head `b62c59a8` and
targeting `codex/kmc-known-issues-sprint1` (draft PR #45, still open). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT2-HANDOFF.md) and the [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Owner review of head `f71fabe7` (2026-11-16), both findings corrected:**
  - **R1:** a pair deferred at its arrival placement lost that placement in the after-entry autosave, so a valid
    pair more than 6 m apart restored if the player continued and was dropped if the player loaded. The placement
    now travels with the row through the save and its load. Ordinary retained intent, a relocated pair and every
    existing record keep the 6 m rule.
  - **R1 format:** the `kmc-retained-intent` text has a version 2, written only for such a pair. Every other save
    writes the same bytes as before. A build from 0.2.3 to 0.2.11 refuses a version 2 text and loads its pairs
    dismounted.
  - **R2:** the S01 record no longer says that no save ran or that stranded save ownership was ruled out.
- **Scope:** S01 and S02 of the owner's Known Issues Remediation Epic. Gameplay code changed for S02 (area arrival
  restoration); S01 adds log lines only. A build of this branch is a new binary.
- **S02, premature dismount after a load: implemented and verified offline.**
  - Recorded once, on a world-map arrival: the pair was attached inside the native load while its views were
    still inactive, and the first invariant check after the loader went idle dismounted it.
  - World-map mode puts every unit to sleep, a sleeping unit's view is inactive, and no game mode ticks during a
    load (read from the installed assembly's IL). That sleeping was the reason in the recorded session is an
    inference.
  - Now restoration reads the exact current views first and attaches only if the mounted invariant would keep the
    pair. Otherwise the pair waits in the existing retained-intent custody and attaches once after ordinary play
    has settled, bounded by the existing ten-second patience.
  - A fight that starts before that remount cancels it (existing world-map rule); the pair fights dismounted.
  - The mounted invariant is unchanged. Inactive views in settled play still dismount a mounted pair.
  - An explicit player dismount now ends that pair's retained intent at once.
- **S01, saving report: investigation complete, issue unresolved.**
  - 0.2.3 session: saving was refused by the area-restore failure latch the door set. 0.2.4 already stopped that
    door from setting it.
  - 0.2.4 session: the log holds no save line, only refused loads. That does not show that no save was attempted
    or ran (requests, refusals, queued and waiting saves were silent at that version). No cause is established.
  - Not established and not ruled out: stranded save ownership, a queued save that never began, a save stuck
    before the native header. A later logged save attempt would distinguish them; none is on record.
  - Needed from the owner: which save action was used, and whether the campaign runs in the native one-save mode.
    The Last Azlanti preserver mod is active in both sessions; it matters only if the campaign is in that mode
    and the action was a manual save or a quicksave (native then refuses those, not an autosave).
  - Added, observation only: a line when the mod's restoration state starts or stops refusing saving, the native
    outcome of each save request, and a line when a save operation begins. Not a fix.
- **Checks:** Fast PASS 6/0 (components 1257/0, Validate-Source 151/0, persistence contracts 223, world boundary 8,
  post-load view readiness 26).
  - At the reviewed head `f71fabe7`: components 1250/0, post-load view readiness 20/0.
  - Before and after for R1: the `f71fabe7` build fails 6 of the 26 contract checks; with the reviewed row
    conversion put back, 4 of the 7 new tests fail.
  - Before and after for S02 itself: with the prior decision behaviour 11 of the 14 original S02 tests fail; the
    base build failed 9 of the then 20 contract checks.
- **Review:** two independent reads, one per iteration. Neither found a crash or latch defect. The first led to six
  corrections; the second to the placement naming its area and to one copy method for carried rows (the handoff
  lists every point).
- **Persistence effect:** the after-entry autosave of a world-map arrival now records the pair as retained intent,
  with its arrival placement (record version 2), and loading it restores the pair in ordinary play.
- **New open items:** T07 and T08, two more chunk7 harnesses that do not compile (failing before this sprint).
  T04–T06 unchanged.
- **Native:** NOT RUN. Nothing was launched, staged or installed. S02 is not native-verified.
- **Not authorized:** merge, release, tag, install, loader replacement, reviewer request or Sprint 3.

# KNOWN ISSUES REMEDIATION, SPRINT 1 — VALIDATION TOOLS REPAIRED OFFLINE; NATIVE NOT RUN (2026-11-15)

Branch `codex/kmc-known-issues-sprint1` (draft PR #45) from main `7a5cd2c3` (0.2.11). Read the
[handoff](docs/KNOWN-ISSUES-SPRINT1-HANDOFF.md) and the new [current issue ledger](docs/CURRENT-KNOWN-ISSUES.md).

- **Scope:** T01–T03 of the owner's Known Issues Remediation Epic, and the ledger. Validation tooling only. No
  gameplay code changed; T02 changes diagnostic evidence code, so a build of this branch is a new binary.
- **T01, charge capacity probe:** `Test-ChargeOwnershipContracts.ps1` now states the six-pair contract in an
  isolated exercise on the compiled product. 71 checks run where 29 did.
  - The script still exits 1. Behind the old assertion it calls charge members whose shape changed on 2026-11-03
    (ledger **T04**). That failure is preserved and not repaired here.
- **T02, repeated-world evidence:** the reader selects a reviewed subscriber contract by evidence contract and
  producer identity. Historical evidence keeps its 13-interface contract; current evidence is
  `chunk7-repeated-worlds-v3` declaring `kmc-global-subscribers-r2`. Anything else is refused as unsupported.
  - The producer's own check had the same stale literals and is corrected with it.
  - Reader repaired and verified offline. No native repeated-world qualification.
- **T03, UMM:** loader identity (one selected reviewed profile, exact) and API compatibility (the reference
  itself) are separate results. Profiles: `planning/UMM-LOADER-PROFILES.json`.
  - Kingmaker assembly contracts: 810 pass, 0 fail on this machine's genuine 0.33.0 (801 pass, 3 fail before).
  - The declared minimum stays 0.28.2 and is unassessed: no genuine 0.28.2 binary is available.
- **Checks:** Fast PASS 9/0 (components 1234/0, Validate-Source 151/0, six focused scripts). Three mutations caught.
- **Review:** one independent read. No defect in T01 or the T02 producer; five in the T02 reader and T03, all
  corrected with a check each (the handoff lists them).
- **Owner review of `fdd93249`:** two T03 findings, corrected in `e52a59f0`. The umbrella now hands its configuration
  to the assembly checker, and the loader matrix reports an inspection without a result as an error instead of
  blank cells. Fast PASS 11/0 on that head.
- **New open items:** T04, and two stale neighbour scripts that fail identically on main (T05, T06).
- **Native:** NOT RUN. Nothing was launched, staged or installed.
- **Not authorized:** merge, release, tag, install, reviewer request or Sprint 2.
# RELEASE 0.2.11 (OWNER ORDER, 2026-11-15)

The owner ordered PR #43, the mounted attack stall repair, merged and released. Merge `22945394`, tree identical to the
reviewed `3cf24379`. Notes: [docs/RELEASE-NOTES-0.2.11.md](docs/RELEASE-NOTES-0.2.11.md).

- **Version:** 0.2.11, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
  UMM's `ParseVersion` compares integer parts, so 0.2.11 reads newer than 0.2.10.
- **Package:** `KingmakerMountedCombat-0.2.11-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Behaviour note:** a turn-based attack order that the game ends before it starts is now dropped once with a message.
  0.2.10 happened to retry it until it went through.
- **Not run in the game before release.** Rows S1–S9 of the stall repair handoff remain NOT RUN, and the Serenity or
  Fascinate trigger is unidentified. This machine has no KMC fixture or fixture qualification.
- **Not done:** no install.

# MOUNTED ATTACK STALL REPAIR — SOURCE FIX WITH OFFLINE CHECKS; NATIVE NOT RUN (2026-11-15)

Branch `codex/mounted-attack-stall-repair` from main `474b8667` (0.2.10). Read the
[handoff](docs/MOUNTED-ATTACK-STALL-REPAIR-HANDOFF.md).

- **Report:** in 0.2.10, turn-based, mounted Royon has Serenity and Fascinate and cannot act. Ordering him to attack
  hangs the combat. The reported log repeats `command accepted` / `command terminal: result=Success; reason=Success;
  childAttacks=0` 4,922 times. That log (`output_log(8).txt`) is not on this machine and no other log was substituted.
- **Cause (source plus the installed 2.1.7b assembly; not observed in game):**
  - Native `UnitActionController.TickCommandTurnBased` ends an unstarted command with `ForceFinishForTurnBased(Success)`
    when its executor cannot act, waits for initiative or has no action for it. Nothing is executed or spent.
  - The pair command's transaction is then cancelled with the reason `Success`, which is the logged signature.
  - The terminal handler kept the retained order for any native Success, and `Update()` dispatched it again.
  - Which Serenity or Fascinate condition stopped Royon is not verified.
- **Fix:**
  - **Terminal:** a command is classified by its own facts (transaction state, observed native attack rule, delivered
    attacks). Native Success without a delivered attack retires that order only, and is no longer reported as
    "Mounted pair attack completed."
  - **Admission:** in turn-based combat a new order is refused when the acting actor's own native conditions forbid it
    (`IsHelpless`, Dazed, Stunned, `CantAct`, `CanNotAttack`). Real time keeps the native pending behaviour. The
    partner is never judged.
  - **Execution boundary:** the same facts are re-read before the attack is released.
  - **Diagnostics:** one `Mounted attack intent retired without execution:` line per retired order, with identity,
    progress and the native capability readings.
  - Nothing is granted, spent, refunded or cleared, and no suppression is kept against a later click.
- **Checks:**
  - Fast PASS 4/0: components 1229/0, Validate-Source 151/0.
  - New focused `Test-MountedAttackStallRepair.ps1` 59/0, detached: native contract pins, the compiled controller's
    own terminal handler, and the capability reader on genuine `UnitState` objects.
  - The same probe fails at its central check on a build of `474b8667`. Two mutations of the new rules each fail
    2 component regressions and the probe.
  - One independent review: no blocking defect; its corrections are applied.
- **Native:** NOT RUN (no KMC install, fixture or fixture qualification). Rows S1–S9 in the handoff.
- **Not authorized:** merge, release, tag, version bump or install.

# RELEASE 0.2.10 (OWNER ORDER, 2026-11-13)

The owner approved PR #41, the Mounted Archery registration repair, and ordered it merged and released. Merge
`81b3a30a`, tree identical to the reviewed `821b8f2d`. Notes: [docs/RELEASE-NOTES-0.2.10.md](docs/RELEASE-NOTES-0.2.10.md).

- **Version:** 0.2.10, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
  UMM's `ParseVersion` splits on `.` and parses each part as an integer, so 0.2.10 reads newer than 0.2.9.
- **Package:** `KingmakerMountedCombat-0.2.10-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Behaviour note:** the turn-based mounted ranged-movement penalty is gated on Mounted Archery's availability, so it
  first takes effect in 0.2.10.
- **Not run in the game before release.** Rows R1–R5 of the registration repair handoff and N1–N6, C and C2 of the
  Mounted Archery handoff remain NOT RUN. This machine has no KMC fixture or fixture qualification.
- **Not done:** no install.

# MOUNTED ARCHERY REGISTRATION REPAIR — SOURCE FIX WITH OFFLINE CHECKS; NATIVE NOT RUN (2026-11-13)

Branch `codex/mounted-archery-registration-repair` from main `fef7b71e` (0.2.9). Read the
[handoff](docs/MOUNTED-ARCHERY-REGISTRATION-REPAIR-HANDOFF.md).

- **Report:** in 0.2.9, Mounted Archery is absent from every feat list. The log reads "Mounted Archery did not retain
  the single-rank combat-feat contract".
- **Cause (installed game data plus product code on genuine blueprints; not read from the player's process):** the
  validator required `IsClassFeature == false`.
  - Mounted Archery copies the flag from stock Power Attack, which holds `true`, as every stock 2.1.7b feat does. The
    values were decoded from the pinned `sharedassets1.assets` with exact object framing.
  - The flag's only native readers are in `Player.RespecCompanion`, where `true` means "rebuilt, not carried over".
  - Every installation therefore threw, and the feat was removed. No other mod is needed.
- **Fix:** the validator now requires the donor's value, which Mounted Combat already holds.
  - The new Domain `MountedArcheryFeatContract` reports each failed invariant with observed values and both fact sets.
  - Registration logs one `Mounted Archery feat contract:` line.
  - Construction, identity, prerequisites, publication, withdrawal and the penalty are unchanged.
  - `CopyFeatureContract` is untouched.
- **Checks:**
  - Fast PASS 4/0: components 1217/0, Validate-Source 151/0.
  - New focused `Test-MountedArcheryFeatContract.ps1` 54/0: stock donors decoded from the install, then the product's
    construction, validation and publication on genuine `BlueprintFeature`/`BlueprintFeatureSelection` objects.
  - Mutation check: restoring the 0.2.9 rule fails 4 component regressions and the focused script.
- **Native:** NOT RUN (no KMC install, fixture or fixture qualification). Rows R1–R5 in the handoff cover registration,
  the summary line, ranger and fighter selection, and prerequisites. The feat screen is not shown repaired in the game.
- **Not authorized:** merge, release, tag, version bump or install.

# RELEASE 0.2.9 (OWNER ORDER, 2026-11-13)

The owner approved PR #39 and ordered it merged and released. PR #39 covers the Mounted Archery feat, the turn-based
mounted ranged-movement penalty and every review correction. It was merged as `e06337b0`, with a tree identical to the
reviewed `d1c35129`. Notes: [docs/RELEASE-NOTES-0.2.9.md](docs/RELEASE-NOTES-0.2.9.md).

- **Version:** 0.2.9, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.9-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Not run in the game before release.** Native rows N1–N6, C and C2 in the Mounted Archery handoff remain NOT RUN,
  and owner art acceptance is pending. This machine has no KMC fixture or fixture qualification.
- **Not done:** no install.

# MOUNTED ARCHERY — FEAT AND TB MOUNTED RANGED PENALTY; OFFLINE CHECKS PASS; NATIVE NOT RUN (2026-11-13)

Branch `codex/mounted-archery` from main `6b6a3e6a` (0.2.8). Read the [handoff](docs/MOUNTED-ARCHERY-HANDOFF.md).
Source: the owner's Mounted_Archery_Mission_Packet (checksums verified; icon and provenance in
`assets-source/mounted-archery/`).

- **Behaviour:** new base rule for every mounted rider, turn-based only: a ranged weapon attack takes -4 once the
  mount's normal travel in the rider's own paired activation exceeds one Move (3 s + 0.25 s native borrowed-tick
  tolerance); Mounted Archery reduces it to -2. Read at each roll; earlier shots are never revised. Real time unchanged;
  no Run. This is a balance change for mounted shooters without the feat.
- **Feat:** `MountedArcheryFeature` `b81926bca1d24a3997cdc8471a84014b`, Mobility 1 trained rank + KMC Mounted Combat,
  combat feat, original icon. Mounted Combat and Mounted Archery now share one coordinated lease per selection array.
- **Seam:** global about-to-trigger on the roll's own `RuleCalculateAttackBonus` (inside `RuleAttackRoll.OnTrigger`),
  one native `BonusSource`; `AttackBonusPenalty` untouched. 15 new native contract pins.
- **Review:** independent; one medium finding (riderless mount travel counted for a later rider) and three low ones,
  all corrected with a regression.
- **Checks:** Fast PASS 3/0 (components 1205/0, Validate-Source 151/0); contracts 799/3 (the 3 UMM identity failures
  are pre-existing); Sprint 6 readers 84/0; six mutations caught.
- **Owner review of `e6844536` (request changes), 3 P2 findings, all corrected:**
  - Currency now follows the pair's current paired activation, not the rider's record. A mid-encounter mount
    (`PreparePartnerThisRound`) is penalized; earlier riderless travel is not counted.
  - A qualifying weapon is a wielded ranged weapon in the same attacker's unresolved roll. Gunslinger Dead Shot's
    probes and its prefix confirmation are covered.
  - Mounted Archery's availability follows the required lists (ordinary and fighter). A drifted required list
    withdraws the feat from every KMC list.
  - **Checks:** Fast PASS 3/0 (components 1210/0, Validate-Source 151/0); contracts 801/3; 5 mutations caught.
- **Owner re-review of `d1379d8f` (request changes, one P2): corrected.**
  - Findings 1 and 2 were closed in source.
  - Mounted Archery's availability is now membership in the required lists, so another mod's unrelated appends or
    reorders change nothing.
  - Withdrawal removes only KMC's own entry from every list that holds it, foreign-changed lists included, and
    removes it again if it reappears.
  - **Checks:** Fast PASS 3/0 (components 1212/0, Validate-Source 151/0); 3 mutations caught.
- **Owner re-review of `713dd621` (one remaining finding): corrected.** While withdrawn, the per-frame refresh now
  scans every array's contents, so an in-place reintroduction of Mounted Archery is removed. Regression
  `InPlaceReintroductionWhileWithdrawn` fails against `713dd621`'s rule.
- **Native:** NOT RUN (no KMC install, fixture or fixture qualification). Owner art acceptance PENDING.
- **Not authorized:** merge, release, tag, version bump or install.

# RELEASE 0.2.8 (OWNER ORDER, 2026-11-12)

The owner approved PR #37 and ordered it merged and released. PR #37 covers the cutscene continuity repair, the Bald
Hilltop Mount lockout fix and every review correction. It was merged as `2dbf62c5`, with a tree identical to the
reviewed `e397167a`. Notes: [docs/RELEASE-NOTES-0.2.8.md](docs/RELEASE-NOTES-0.2.8.md).

- **Version:** 0.2.8, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.8-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Not run in the game before release.** Native acceptance rows in the handoff remain NOT RUN, Bald Hilltop included.
  This machine has no KMC fixture or fixture qualification.
- **Not done:** no install.

# CUTSCENE CONTINUITY REPAIR — SOURCE FIX WITH OFFLINE CHECKS; NATIVE BLOCKED (2026-11-12)

Branch `codex/kmc-cutscene-mounted-continuity` from main `0754c44a` (0.2.7). Read the [handoff](docs/CUTSCENE-CONTINUITY-REPAIR-HANDOFF.md).

- **Cause (source; the owner's occurrence is not reproduced):** three dismount paths.
  - Every Dialog/Cutscene mode start suspended (physically dismounted) each pair. A refused suspension, as in combat,
    fell back to a clean dismount.
  - Dialog/Cutscene were also `CleanDismount` in the mode policy, which formed a second path through invariant
    validation.
  - Combat cancelled retained intent.
- **Fix:**
  - Dialog/Cutscene keep the attachment. Mounted input stays refused.
  - Only a scripted operation on the rider (`UnitCommands.Run`), or a scene removing a pair member from the game, hands
    that one pair to native staging.
  - Restoration reads staging ownership from native state per pair. Scene-staging intent holds through combat.
  - A mount left behind regroups with a bounded native move.
- **Checks:**
  - Fast PASS 3/0 (components 1183/0, Validate-Source 151/0).
  - Kingmaker contracts 781/3: all 16 new pins pass; the 3 UMM identity failures are pre-existing.
  - Mutation check: the old decisions fail 5 regressions.
- **Self-review:** all 10 findings are corrected. The handoff's top section lists them.
  - Regroup uses game time, stops on arrival and keeps its second attempt.
  - Scripted commands on an attached pair run natively.
  - Staging patience is bounded at 60 s.
  - Staging never joins camp or world-map custody.
  - A scene teleport of the rider is now honoured (handoff at the scene's placement).
  - After the corrections: Fast PASS (components 1186/0, Validate-Source 151/0); contracts 781/3, failures pre-existing.
- **Bald Hilltop Mount lockout (added by owner request):**
  - **Cause (installed-assembly contracts plus elimination; not observed):** the refusal read the presence of native
    `UnitEntityData.CutceneControlledUnit`. That holder is lazily created by `MarkUnit` the first time any scene
    controls the unit, never cleared, and not saved. One earlier scene therefore disabled Mount for the rest of the
    session.
  - **Fix:** Mount and Dismount now refuse only for live scene control (active entry, live unfinished cutscene player),
    loading, the lock and the cutscene modes.
  - **Diagnostic:** a change-of-reason line with the native facts.
  - **Checks:** Fast PASS (components 1190/0, Validate-Source 151/0); the mutation fails 4 regressions; contracts 784/3.
- **Owner review of `00131270` (request changes):** all four findings are corrected.
  - **P1:** the scene placement is reapplied through the rider captured before suspension, and verified. A lost
    placement is reported.
  - **P2:** scene staging restores and counts regroup arrival only within 2 m, so a staged rider moves at most 2 m.
  - The diagnostic now logs the final native Mount/Dismount refusal, per action and caster, with per-evaluation facts.
  - **Checks:** Fast PASS (components 1192/0, Validate-Source 151/0); both mutations caught.
- **Native:** NOT RUN, Bald Hilltop included. This machine has no KMC install, fixture or fixture qualification.
- **Not authorized:** merge, release, tag, version bump or install.

# RELEASE 0.2.7 (OWNER ORDER, 2026-11-12)

The owner accepted PR #35, the horse group pace repair, and ordered it merged and released. Merge `2e97cbd4`, tree
identical to the reviewed `ed82ec27`. Notes: [docs/RELEASE-NOTES-0.2.7.md](docs/RELEASE-NOTES-0.2.7.md).

- **Version:** 0.2.7, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.7-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Not run in the game before release.** Native acceptance rows in the handoff remain NOT RUN: this machine has no
  KMC fixture or fixture qualification.
- **Not done:** no install.

# HORSE GROUP PACE REPAIR — SOURCE FIX WITH OFFLINE CHECKS; NATIVE BLOCKED (2026-11-12)

Branch `codex/kmc-horse-group-pace` from main `bd22a045` (0.2.6). Read the [handoff](docs/HORSE-GROUP-PACE-REPAIR-HANDOFF.md).

- **Cause (installed-assembly source; not shown in game):** native long group moves accelerate in a specific way.
  - `UnitCommand.AccelerateIfPathIsLong` sets the Charge gait and multiplies the limit by 1.8, which turns 3.048 into 5.49.
  - It then offers `Accelerate()` only to the commands of units in `SelectedUnits`.
  - KMC projects the mount onto its rider and runs the rider's order on the mount, so the offer skipped the horse.
  - The horse therefore kept the 3.05 group walk while the party ran. The log fits this: 103 legs at 3.05 and 2 own-path accelerated legs at 5.49, with `modifiedSpeed` 4.06 throughout.
- **Fix:** an exact-token postfix on `AccelerateIfPathIsLong` offers the same native `Accelerate()` to each pair's routed ground order. This applies only when the rider is selected, the mount is not, and play is real time.
  - Native `CanAccelerate` keeps every refusal: setting, combat, speed-affecting conditions, encumbrance and gait.
  - Owned approaches and charge are untouched.
  - Bounded diagnostics were added.
- **Checks:**
  - Fast PASS 3/0 (components 1175/0, Validate-Source 151/0).
  - Kingmaker assembly contracts: all 8 new pins pass. The 3 pre-existing UMM identity failures are unchanged.
  - A mutation check confirms the regression detects the defect.
- **Native:** NOT RUN. This machine has no KMC install, fixture or fixture qualification.
- **Not authorized:** merge, release, tag or install.

# RELEASE 0.2.6 (OWNER ORDER, 2026-11-12)

The owner approved PR #33, the attack completion warnings repair (merge `b8359e31`, tree identical to the reviewed
`f46946a5`), and ordered it merged and released. Notes: [docs/RELEASE-NOTES-0.2.6.md](docs/RELEASE-NOTES-0.2.6.md).

- **Version:** 0.2.6, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.6-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Not run in the game before release.** Native acceptance rows in the handoff remain NOT RUN: this machine has no
  KMC fixture or fixture qualification.
- **Not done:** no install.

# ATTACK COMPLETION WARNINGS — SOURCE FIX REFINED; FAST PASS ON GENUINE REFERENCES; NATIVE BLOCKED (2026-11-12)

Branch `codex/kmc-attack-completion-warnings` (draft PR #33) from main `680d02cb` (0.2.5). Read the
[handoff](docs/ATTACK-COMPLETION-WARNINGS-HANDOFF.md), top section.

- **Cause (source only):** successful attack cleanup clears the command reference but retains its intent. The next
  exhausted-action update treated that missing reference as a fresh refused click and displayed the no-Standard
  warning. No action-economy defect was found; the warning followed legitimate native spending.
- **Fix:** retain per-intent dispatch history. Completion is quiet; genuinely new unavailable player requests keep
  their feedback.
- **Review correction:** later dispatch refusals of accepted work are quiet only when their codes are routine
  reassessment. Admission faults, configuration and uncoded refusals still answer once. Every stock dispatch
  rejection is now logged, because three admission-fault paths had no other diagnostic.
- **Checks:** Fast PASS 3/0 against the installed Kingmaker/UMM references (net47 build; components 1168/0;
  Validate-Source 151/0). The unchanged PR head also passed (1167/0, 151/0). A mutation check confirms the new
  regression detects the blanket rule. Budget `attack-completion-warnings`: 1.25 of 90 min.
- **Native:** BLOCKED, NOT RUN. This machine has no KMC install, no KMC Baseline/Working fixture and no fixture
  qualification. Nothing was staged, launched or restored.
- **Delivery:** review branch only; no merge, release, tag, package or installation.

# RELEASE 0.2.5 (OWNER ORDER, 2026-11-12)

The owner ordered PR #31, the save/load and entrance repair (merge `af138c57`, tree identical to the reviewed
`d512db91`), merged and released. Notes: [docs/RELEASE-NOTES-0.2.5.md](docs/RELEASE-NOTES-0.2.5.md).

- **Version:** 0.2.5, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.5-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Not run in the game before release.** Native acceptance rows in the handoff remain NOT RUN. Saving after the
  Ratnook door remains unresolved (diagnostics only). `Test-ChargeOwnershipContracts.ps1` still fails its pre-existing
  two-pair reservation check.
- **Not done:** no install.

# SAVE/LOAD AND ENTRANCE REPAIR — LOAD AND ENTRANCE FIXED IN SOURCE; SAVING UNRESOLVED (DIAGNOSTICS ONLY); NATIVE BLOCKED (2026-11-12)

Branch `codex/kmc-save-load-entrance-repair` from main `bcc176ec` (v0.2.4). Read the
[handoff](docs/SAVE-LOAD-ENTRANCE-REPAIR-HANDOFF.md).

- **Running-game loading:** `Game.LoadGame` reaches the full `LoadArea` with the selected `SaveInfo`. KMC's prefix
  applied the exploration travel guard first, so loading during a paired combat turn was refused. That fits the
  owner's report and log 3's four "Area change refused" lines, though the message does not name its caller. A
  saved-world replacement now uses its own admission. It releases nothing itself: the established load path releases
  the outgoing world. Writers, queued saves, charge fences, retained charge cleanup on any pair (review of PR #31) and
  incompatible archives still refuse.
- **Entrance approach:** KMC's selection normalization left the mounted horse, a native group member, without its
  entrance command, so the native group never started. The horse now receives its native member command in the same
  group.
- **Saving:** open. The log shows no save execution after the door, which does not establish where an attempt stopped
  (never reaching `Game.SaveGame`, refused by `IsSaveAllowed`, queued and never started, or started and unfinished).
  The traced code admits a paired-combat save. Bounded request and completion diagnostics were added; the completion
  line is written after cleanup and cannot block saving if logging fails. Save behaviour is unchanged.
- **Native:** NOT RUN. This machine has no KMC install or fixture.
- **Not authorized:** merge, release, tag or install.

# RELEASE 0.2.4 (OWNER ORDER, 2026-11-12)

The owner accepted PR #29, the Ratnook door repair (merge `9d7f575e`, tree identical to the reviewed `da917b68`), and
ordered it merged and released. Notes: [docs/RELEASE-NOTES-0.2.4.md](docs/RELEASE-NOTES-0.2.4.md).

- **Version:** 0.2.4, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.4-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Not run in the game before release.** Native Ratnook acceptance remains NOT RUN; the owner is testing it in the
  campaign where the bug was found. The arrival view-activity dismount and issues 1–2 remain open.
- **Not done:** no install.

# RATNOOK HILL DOOR REPAIR — SOURCE FIX WITH OFFLINE AND CONTRACT CHECKS; NATIVE BLOCKED; OWNER ACCEPTANCE SEPARATE (2026-11-12)

Branch `codex/kmc-ratnook-door-repair` from main `44b6f606` (v0.2.3). Read the
[handoff](docs/RATNOOK-DOOR-REPAIR-HANDOFF.md).

- **Cause (installed-assembly contract plus log elimination):** a door whose enter point is in the loaded area goes
  `ExecuteTransition` → party-leave → `LoadArea 06000CC9` → `Game.Teleport`. That route never reaches
  `OnAreaLoaded`, which was KMC's only restore point. The idle watchdog therefore latched `areaRestoreFailure`, and the
  global command guard blocked the whole party.
- **Fix:** the transfer records its observed native route. A local teleport restores at
  `ITeleportHandler.HandlePartyTeleport` through the existing all-pair restore. A full load still restores at
  `OnAreaLoaded`. A transfer that was never dispatched restores in place. A dispatched route that never placed the party
  keeps the unchanged protective latch.
- **Not fixed here:** the separate arrival view-activity dismount (log 11213–11214).
- **Native:** NOT RUN. This machine has no KMC install, fixture or disposable Ratnook save.
- **Not authorized:** merge, release, tag or install.

# RELEASE 0.2.3 (OWNER ORDER, 2026-11-12)

The owner accepted PR #27, the exploration repairs (merge `ff183945`, tree identical to the reviewed `3a547ad8`), and
ordered a release. Notes: [docs/RELEASE-NOTES-0.2.3.md](docs/RELEASE-NOTES-0.2.3.md).

- **Version:** 0.2.3, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.3-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Not run in the game before release.** The owner is testing it in the campaign where the bugs were found.
  Issues 1–2 (extra marker, horse lag) remain parked.
- **Not done:** no install.

# EXPLORATION REPAIR SPRINT — OWNER REVIEW CORRECTIONS IN SOURCE; NATIVE BLOCKED; OWNER ACCEPTANCE SEPARATE (2026-11-12)

Branch `codex/kmc-exploration-markers-speed-events` from main `aade80e5`, draft PR #27. Read the
[handoff](docs/EXPLORATION-REPAIR-HANDOFF.md).

- **Issues 3–7 corrected in source:** the world map, scripted events and the lever cart, camping, corpse looting, and
  the cat (lowest priority). None is demonstrated in the game.
- **Owner review (request changes):** every finding is corrected.
  - Restore residue is never reported as a clean dismount.
  - Suspended intent is saved in a new `kmc-retained-intent` member.
  - A camp that follows a dialog adopts the camp rules.
  - The owner identity is imported on restore.
  - A transient arrival refusal is retried.
- **Second owner review (3 P2):** corrected.
  - An after-arrival save records only the still-deferred pairs.
  - A carried capital arrival retires through the native rule.
  - A world-map save keeps the Mount/Dismount slot rows.
  - A pending record is bound to the load that selected it.
- **Third owner review (1 P2):** corrected. The departure merge keeps a retained hotbar slot behind a live placement
  marker.
- **Issues 1–2:** parked by the owner until they can reproduce them. The log-only observations stay in the build.
- **Checks:** Fast PASS 3/0 (components 1135/0, Validate-Source 151/0).
- **Native:** NOT RUN. This machine has no KMC install, no KMC fixture and no runtime-state.
- **Not authorized:** merge, release, tag or install.

# RELEASE 0.2.2 (OWNER ORDER, 2026-11-07)

The owner accepted and merged the UI polish (PR #25, merge `617f2e63`, tree identical to the reviewed `9d63a0bf`), then
ordered a release with the version incremented. Notes: [docs/RELEASE-NOTES-0.2.2.md](docs/RELEASE-NOTES-0.2.2.md).

- **Version:** 0.2.2, changed in `version.json`, `Info.json` and `BuildIdentity.cs`. The assembly version stays 0.1.0.0.
- **Package:** `KingmakerMountedCombat-0.2.2-release-diagnostic.zip`, built from the release branch. Its identity is
  recorded on the GitHub release.
- **Recipe:**
  1. Open the version PR.
  2. The owner merges it.
  3. Tag the merge `v0.2.2`.
  4. Publish the prebuilt package and its manifest, marked Latest.
- **Not done:** no install.

# UI POLISH ENGINEERING COMPLETE — READY FOR OWNER REVIEW; OWNER ACCEPTANCE SEPARATE (2026-11-07)

Branch `codex/mounted-combat-ui-polish` from main `98bac4bc`. Read the [handoff](docs/UI-POLISH-REVIEW-HANDOFF.md).

**Changes (owner playtest follow-up):**
- **Rename control:** one Rename button sits at the right end of the companion name line, with the name keeping its
  slot clear. Default Name is removed.
- **UMM:** the options panel is gone. Mounted Charge is always on. Removal preparation now runs from Ctrl+Shift+F11 in
  game, documented in the new `README.md`.
- **Public name:** **Mr. Bard's Kingmaker Mounted Combat**. Every internal identity is unchanged.

**Native evidence** (frozen ui-polish-b at `1d98bf68`, DLL `b70709a2…`): producer r02 PASS, cold consumer r03 PASS and
charge row r04 PASS. Producer r01 is an ORIGINAL OUTER FAIL from a reader check; its game side passed and its corrected
re-evaluation passes.

**Spend:** budget `ui-polish` at 19.93 of 90 min.

**Not verified in game:** the Ctrl+Shift+F11 route, physical mouse states, other resolutions and an owner campaign.

**Not authorized:** merge, release, tag or install.

# F6 ENGINEERING ACCEPTED IN REVIEW AT 4b8a5f02; SELECTED REFINEMENT ENGINEERING COMPLETE WITH WORLD-02 DEFERRED; OWNER ACCEPTANCE SEPARATE (2026-11-07, closeout)

The owner merged PR #21 as `eb16d2f1` (tree identical to the reviewed `4b8a5f02`). Read the [handoff](docs/FINALIZE-F6-REVIEW-HANDOFF.md), section "Engineering acceptance and integration closeout".

**Status:**
- **Accepted candidate f6f:** DLL `87fed161…`, MVID `a7638ec4…`.
  - Package `74baa915…`, re-verified read-only. It is a lab file, not a download link.
  - Suite `finalize-f6f-5fe80073-20261107`.
- **Verdicts:**
  - r05: ORIGINAL OUTER FAIL, native PASS, corrected re-evaluation PASS.
  - r03: separately re-evaluated. r04: PASS. Both on `b18cc056…`.
  - r01 and r02: FAIL, kept.
- **Spend:** finalize-f6 at 42.75 of 90 minutes and 5 of 6 starts. Closeout spent nothing.

**Release 0.2.0 (owner order, 2026-11-07):** the first full version. Notes: [docs/RELEASE-NOTES-0.2.0.md](docs/RELEASE-NOTES-0.2.0.md).
- **Why 0.2.0:** UMM 0.33.2's own `ParseVersion` reads every alpha label as `0.1.x`, at most `0.1.7.103`, so `0.2.0` reads newer than all of them. The assembly version stays `0.1.0.0`.
- **Package:** `KingmakerMountedCombat-0.2.0-release-diagnostic.zip`, SHA-256 `ab2d51028303576802956e2b1e80dc0c4eb30b70c68abb7ad8a0bb87b25118fd`.
  - DLL `634f0da67853604f6c0cc090e4f1e578a38b321f83489e7057765f992c2200ab`, MVID `72cad97c-5b3d-4903-993c-50fbeb73833a`.
  - Built from `3971e487`.
- **Relation to the tested f6f DLL `87fed161…` (ildasm comparison):** the only differences are
  - the version string, in five places;
  - the MVID and image base;
  - static-data offsets shifted by the shorter string. Every content-hashed field and data blob is identical.
- **Not run in the game.** Gameplay source is identical to the accepted F6 landing.
- **Checks:** Fast passed 3/0 on budget `release-0.2.0` (0.5 min); Validate-Package passed 11/0.
- **Published:** release [v0.2.0](https://github.com/howardreith/KingmakerMountedCombat/releases/tag/v0.2.0), not draft and not prerelease, marked Latest.
  - The tag is on the owner's PR #22 merge `c5848306`, whose tree equals the PR head. It differs from the package's build commit `3971e487` only in this file.
  - Assets: the package and its manifest. The downloaded asset's hash matches `ab2d5102…`.
  - A release is not an install.

**After 0.2.0 (owner order): final-release wording.**
- Player-visible and log text no longer calls the mod a "private alpha" or an "experiment". That covers:
  - refusal messages;
  - the Horse description;
  - the load and enable log lines;
  - the assembly description.
- Test and audit pins move with the text, and the readers' keys ("… <version> loaded", "diagnostic overlay=False") are kept.
- The text changes alter the DLL, so this is not the 0.2.0 binary. It ships with the next version.
- **Checks** (budget `release-wording`):
  - Fast passed 3/0: components 1118/0, Validate-Source 149/0.
  - Candidate passed 7/0, including Test-Harness. A first Candidate attempt lost its shell host while starting Test-Harness and was rerun detached.

**Remaining:**
- WORLD-02 stays deferred and not passed.
- Owner style and full-stack play acceptance are separate.
- No install or further planned engineering.

# F6 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER ACCEPTANCE SEPARATE (2026-11-07, completion mission)

Branch `codex/kmc-finalize-f6-falls-final-acceptance` from main `730f72d1`. Read the [handoff](docs/FINALIZE-F6-REVIEW-HANDOFF.md), top section.

**Status:**
- **r05 completion run** `20261107-finalize-f6f-completion-05`, on the `finalize6-falls-completion-tb` setup plus fall rows. It showed natively:
  - the TB death hard landing, then the native stand-up on the rider's own next turn: Move 0 → 3 s, Standard untouched, no travel, no replay, and "Stand up" shown in the Move slot;
  - the TB prone safe landing at the exact threshold (11 vs DC 11), with no damage and no prone;
  - prone then death with no second throw.
- **r05 verdict:** ORIGINAL OUTER FAIL from reader-only row registration and two reader expectations, now corrected. The corrected re-evaluation passes over the untouched evidence.
- **Retained:**
  - r03: the RT hard landing and the post-fall save, with a separate re-evaluation.
  - r04: the cold reload PASS. Its Manual holds a standing rider; no prone-save test happened.
  - r01: the TB death at its own build, FAIL kept.
  - r02: FAIL in an unrelated F3 row.
- **Candidate:** frozen head `5fe80073`, DLL `87fed161…`, MVID `a7638ec4…`, package f6f `74baa915…`. Gameplay code is unchanged since `c7ca5989`.

**Spend:** 42.75 of 90 minutes, 5 of 6 starts (ceiling 4 → 6 append-only). The contingency is unused.

**Next:** final review. WORLD-02 stays deferred. Owner art and full-stack acceptance are separate. No merge, release or install.

# F6 ENGINEERING PARTIAL — FALLS SHOWN NATIVELY (TB DEATH, RT PRONE), POST-FALL SAVE/RELOAD PASS; SAFE LANDING AND TB PRONE NOT SHOWN NATIVELY; OWNER ACCEPTANCE SEPARATE (2026-11-07)

Branch `codex/kmc-finalize-f6-falls-final-acceptance` from main `730f72d1` (the F5 landing). Read the [handoff](docs/FINALIZE-F6-REVIEW-HANDOFF.md).

**Status:**
- **Product (FALL-01/FALL-02):**
  - When the exact ridden mount dies, collapses or newly falls prone, its rider is unseated once and resolves D5 natively: Mobility vs DC 15 (native difficulty adjustment kept), native 1d6 and prone on a failure, and one log line.
  - Nothing is written to actions, Swift, reactions or feats.
  - Reviewed independently; the confirmed findings were fixed before any start.
- **Native:**
  - r01: the TB mount death fell hard as designed. The run is recorded FAIL because its stand-up row met the existing forced activation end.
  - r02: FAIL in an unchanged F3 row.
  - r03: the RT Riding Dog prone fell hard beside the identically named Mastodon pair, and the post-fall Manual `b33eda46…` was written. ORIGINAL OUTER FAIL from a reader-only row name; the corrected readers accept its evidence.
  - r04: the cold consumer PASS (no replay, damage, names and tack kept, ordinary remount), and the cross-run reader passes.
- **Not shown natively:** the safe-landing branch, the TB prone, prone-then-death and the TB stand-up cost.
- **Candidate:** f6e `fbef8638`, DLL `b18cc056…`, MVID `2a9d61d7…`, package `7109cdfd…`.

**Spend:** 32.77 of 90 minutes, 4 of 4 starts. A fifth start is not authorized.

**Next (owner):** either one TB start on f6e unchanged, or accept the shown scope. WORLD-02 stays deferred. No merge, release or install.

**F5 closeout:** accepted and merged by the owner as `730f72d1` (tree identical to the reviewed `badef7f0`). Package `b078d870…` verified. See the F5 handoff's integration closeout.

# F5 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER ACCEPTANCE SEPARATE (2026-11-07, producer/consumer completion mission)

Branch codex/kmc-finalize-f5-names-dog-tack. Frozen and launched at d87b148e (compiled source fd8bb820; DLL `8459126c…`, MVID e90f71ee…, byte-identical to r06). Base main 3f19e9bd. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md), top section.

**Status:**
- **Producer r07** `20261107-finalize-f5h-producer-07`: PASS.
  - Every naming row passes, including the duplicate Unicode `Ébène l'Ancien`.
  - The corrected saddle is coherent, with clearance −0.0036…0.0508 and 0 unsupported.
  - Native Manual `d39f8b46…`, 858008 bytes. Restored, digest `60e3d6cc…`.
- **Cold consumer r08** `20261107-finalize-f5h-consumer-08`: PASS, in a fresh process loading r07's exact Manual.
  - The saved IDs, masters, blueprints and both names are intact, with zero naming-service writes.
  - One saddle was re-fitted in the new process and survived the remount. Dog statistics are unchanged.
  - No save was written, and restoration is complete.
  - The cross-run reader `Test-Finalize5ColdAgainstSource.ps1` passes.
- **r06:** ORIGINAL OUTER FAIL / NATIVE GAME PASS, kept. It was not used as a source.

**Spend:** 74.9 of 90 minutes, 8 of 8 starts, on the same finalize-f5 record (ceiling 7 → 8 append-only). A ninth start is refused.

**Next:** final review. Owner art and full-stack acceptance are separate. WORLD-02 stays deferred; F6 is not started; no merge, release or install.

# Superseded F5 state after the saddle-diagnosis mission (2026-11-07)

Branch codex/kmc-finalize-f5-names-dog-tack. Product at fd8bb820 (r06); harness reader fix at d6c88978; records commit after it. Base main 3f19e9bd. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md), top section.

**Status:**
- **Saddle (PET-02):** the cause is proven by an offline replay of a private real-dog specimen (r05). The first failing stage was sampling: the back reference fell on chest-underside vertices where the dog's back has no nearby vertex. Conversion and skinning are exact.
  - Corrected: the back is sampled on the surface, the girth sits behind the elbows, and the fit measures per-vertex clearance, which the reader requires.
  - r06 shows one coherent pad, seat and girth, unmounted, mounted and walking, with numbers identical to the replay.
- **Naming (PET-01):** every producer row passes game-side again in r06 (Manual `c673673c…`, 376897 bytes).
- **Cold consumer:** not run. r06's recorded verdict is FAIL because the harness read the game's UTF-8 evidence as ANSI and mis-compared `Ébène`. A UTF-8 re-read passes its whole reader chain. A FAIL is no source.

**Spend:** 63.97 of 90 minutes, 6 of 7 starts, on the same finalize-f5 record. An eighth start is refused.

**Next:** owner decision. Recommended: start ceiling 7 → 8 (no time increase), then a Candidate at d6c88978, one producer and its consumer. WORLD-02 stays deferred; F6 is not started; no merge, release or install.

# Superseded F5 state after the completion mission (2026-11-07)

Branch codex/kmc-finalize-f5-names-dog-tack (head after the records commit; product/harness at d32fdc28), from main 3f19e9bd. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md), top section.

**Status:**
- **Naming (PET-01):** demonstrated on the producer side in four runs. r04 also covers the ordinary overtip label on both companions.
- **Saddle (PET-02):** single, adopted, mechanically neutral. The fit along the centre line is now on design, but r04's reader and frames both show the pad rendering as shards and a flank strip, not a drape. The cause is not diagnosed.
- **Cold consumer:** not run. A FAIL producer is no source.

**Start 4:** `20261106-finalize-f5d-producer-04` (f5d, DLL 119e7a98…, MVID 31f3c7c9…). Game side PASS and the Manual was written (3e70c359…). Reader FAIL: a pad/seat point 0.544 m above the back. Restoration complete.

**Spend:** finalize-f5 is at 42.52/60 min, 4 of 6 starts. This includes 430 s of F5 Fast tiers cross-referenced from the development record. Starts 5 and 6 are unused: with 17.48 min left, a corrected candidate plus two 10-minute admissions does not fit.

**Next (owner):** diagnose the saddle render, likely from per-vertex fit data of the real dog, then decide on time and starts for one producer and one consumer. No game, lock or waiter remains. No merge, release, install or F6.

**F4:** accepted for integration with WORLD-02 deferred, not passed ([deferred items](docs/FINALIZATION-DEFERRED-ITEMS.md)).

# Superseded F5 state at review (2026-11-07)

Branch codex/kmc-finalize-f5-names-dog-tack, worktree worktrees/finalize-f5, from main 3f19e9bd (the F4 landing). Requirements PET-01 and PET-02. Read the [handoff](docs/FINALIZE-F5-REVIEW-HANDOFF.md).

**Status:**
- **Naming (PET-01):** fully exercised in the game on the producer side through the real sheet button and native dialog, in all three runs.
- **Tack (PET-02):** attached, adopted, single and mechanically neutral in every run. Its fitted shape failed the reader in r03 and has been corrected offline only.
- **Cold consumer:** not run.

**Runs (all FAIL, verdicts kept, causes corrected; restoration complete each time):**
- r01 (f5a): a pre-F2 slot assertion.
- r02 (f5b): F2's control migration of a stale harness Dismount binding.
- r03 (f5c): game side PASS on every row and the Manual written; the reader refused the tack shape.

**Next:** two more starts on this record (producer + consumer) from the branch head, which needs owner authorization. Spend: finalize-f5 26.83/60 min, 3 of 3 starts. No game, lock or waiter remains. No merge, release, install or F6.

**F4 closeout:** F4 ACCEPTED FOR INTEGRATION WITH WORLD-02 NATIVE VALIDATION DEFERRED; OWNER ACCEPTANCE SEPARATE. The owner merged PR #19 at the reviewed head 46f76f7e as 3f19e9bd. WORLD-02 is deferred, not passed: see [docs/FINALIZATION-DEFERRED-ITEMS.md](docs/FINALIZATION-DEFERRED-ITEMS.md) and the F4 handoff's integration closeout.

# F4 record at review, kept: ENGINEERING PARTIAL — WORLD-02 INPUT BLOCKED ONLY; OWNER ACCEPTANCE SEPARATE (2026-11-06)

Branch codex/kmc-finalize-f4-orders-ai-transitions, from main a92b043a. Read the [handoff](docs/FINALIZE-F4-REVIEW-HANDOFF.md), current section first.

**Entrance → Auto → reload chain, complete on one binary:**
- **Binary:** DLL 3240963f…7f3d, MVID cc86d1d6-502e-4991-8805-618755a9ae47.
- **Producer:** r08 `20261106-finalize-f4f-dungeon-08` PASS (package f4f 4efc6665…). It wrote `Auto_1.zks` with SHA-256 b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5, 395809 bytes: native Auto, campaign "cvb", on the floor.
- **Reload:** r10 `20261106-finalize-f4g-dungeon-reload-10` PASS. Package f4g e1f195c1… was built from the records head, docs-only delta, with the identical DLL.
  - It cold-loaded that exact Auto on its floor and campaign, with the same rider and mount, unique bound views and exact restore counts.
  - No inherited transfer state.
  - Then an ordinary move and stock attack (a native miss).
  - 0 writes, and the source is unchanged.

**Earlier results, at their own binaries:** the Hold control (r04 rows), and the chest-during-travel and gait results (r05).

**Sole outstanding item: WORLD-02 trap, INPUT BLOCKED.** It needs an owner Manual save beside a revealed, reachable trap.

Verdicts:
- r01–r04, r06, r07 and start 9 (pre-staging refusal, my detached-HEAD error): FAIL, as recorded.
- r05, r08 and r10: PASS.

Spend: finalize-f4 68.17/80 min, 10 of 10 starts. All adjustments are append-only. No game, lock or waiter remains. No merge, release, install or F5.

F3 closeout: the F3 review was accepted at PR #18 head 5dd64825; the owner merged it as a92b043a.

