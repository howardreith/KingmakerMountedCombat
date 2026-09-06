# Phase 3G implementation and private candidate

Disposition: partial private candidate. Native separate turns remain. TB movement/resource reconciliation is not qualified; this is not a finished Wrath-equivalent mod. Only one mounted pair is supported.

## Exact candidate and protected starting state

Branch `codex/mounted-combat-phase3f-playable-core`. Intake local/upstream/host remote were clean at `6d59b5f42ff5d675e390e55e64e89029c96255ec`; work proceeded forward without discarding inherited state. Binary source `b8c206331f34a1aa873bfef86a37ab1066c267fc`, version **0.1.0-phase3g-preview.7**. Later documentation commits do not change this source identity.

Package: `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3g-preview.7-native-controls-private-preview-diagnostic.zip`.

| Identity | Value |
| --- | --- |
| ZIP SHA-256 | `04e86be27d55f3ee948de322363a461a39c59ccd0a30acca15972cd6155f0efd` |
| Sidecar manifest SHA-256 | `edb8643dfa8f76a5b8d4b04f1666c7d6a622f70acaed12fc9f69d0941df570f7` |
| DLL SHA-256 | `6bc01b982309125963d2355983b37391f244cfea314093fe4b008ad21a241ff3` |
| DLL MVID | `b0069610-4fed-4e03-ac77-db00d146f37c` |
| Info.json SHA-256 | `1cc079143b82efa6fdd2342e23af98f87ab00a84ec861fa3f5c51d397c29c5f6` |
| Configuration C0 | `EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, `EnableDiagnosticOverlay=false` |
| Final suite | `20260906-phase3g-preview7-suite7`, SHA `4bf2ddcaf733892d49f4ddd15a1f35321d8f2027afe0d9123207727c643cae96` |

The clean package contains only Info.json and the original mod DLL (3576320 bytes). No game/Wrath assemblies, extracted assets, private raw logs, credentials or saves are packaged. The old fallback and Phase 3F preview.4 ZIPs remain unchanged at their supplied hashes.

The starting installation is intentionally **Phase 3F preview.4**, including its legitimate `.dll.20370.cache`. The human-updated saves/settings were captured at intake, not replaced by old suite4 pins. Evidence root E below means `C:\Dev\KingmakerMountedCombatLab\analysis-cache\phase3g-intake-20260905T233500Z`. Full intake `E/full-current-intake.json` SHA `a82030112415f489303fbe8d959a4ae726564becc3e859478e3f3318ac68b6f7`; saves digest `c8b2bcc1e5922f26b7219dab6acb09dfffbdcc787eb6320d8ea81c33dbaf6b0b`, Mods digest `9142fdfcaff6c8e088a7032123ea3b0e176e2f93b1f2f47d35155a72b0a91017`. No new permanent replacement is authorized or performed.

## Human baseline and provenance

The local Phase 3G packet's seven manifest payloads were verified; all three unique screenshots were inspected. They show unmounted controls, the Wrath saddle style reference and rider height, not motion. Current human log `E/current-human-output_log.txt`, SHA `4d34d97f2cefabca55676f770d6eabc008765fc1cdb09918a920baf9a9c66084`, identifies Phase 3F preview.4 and its exact MVID. Source last-write `2026-09-05T23:20:41.2421162Z`; captured `23:35:31.7126097Z`, 141171 bytes, immutable copy with provenance. It is not the historical fallback log.

Howie's HUMAN-REPORTED preview.4 successes remain valid for that session: unmounted longbow, Mount/Dismount, RTWP approach and rider melee, one-click longbow repetition, whole-party selection, Stop/replacement/retarget/Primary precedence, and dismount followed by unmounted attacks. His unmounted wording is "Successfully did some ranged attacks and reloaded." No bow reload mechanic or full save/load matrix is inferred. The older unmounted Sling fixture timeout remains a separate unqualified case, not a blanket unmounted ranged defect.

Reported failures: all three TB attacks, same-target clicks delaying the next longbow shot, paused Mount unavailable, Horse mechanical attacks without visible animation, rider slightly too high, and aesthetically rejected saddle icons. The requested seating correction is downward Y only; horizontal placement was close. Buff Planner UI exceptions and other unrelated warnings are recorded separately, without causal attribution or changes to those mods.

## Repairs and contracts

| Defect / first boundary | Implemented repair | Evidence limit |
| --- | --- | --- |
| TB pointer prediction entered real command interception; blanket pause/mode gates also rejected native order admission | Exclude exact native simulated pointer requests from production routing. Separate loaded-world Default/Pause order admission from native execution. Admit only the exact native turn actor; correct Horse Primary caster/shell ownership. | Native handler/effect results below. No forced current actor, unpause or off-turn scheduler in production. |
| Repeated equivalent hostile request canceled and recreated its ordinary generation | Continue the exact living target/pair/actor/turn/weapon/action context before native Run can replace the owned attack. Stop, ground, explicit Primary, invalidation, changed weapon and retarget cancel that equivalence. Other selected party members keep their own commands. | Deterministic generation/cancellation tests pass; actual cadence scope is listed below. |
| Pause UI availability was conflated with execution eligibility | Native UnitUseAbility queues exact caster/target; no mounted leases or resource spend before native execution. Revalidate ownership/body/life/adjacency then. Cancel pending exact controls on lifecycle/serialization boundaries. Default/Pause world admission excludes modal modes; native gameplay advances itself. | Does not add combat Mount permission. Combat Dismount keeps its existing native cost gate. |
| Native Mount blueprint was Unlimited while execution now required adjacency | After verified native Init, narrow only that exact owned Mount command through `set_ApproachRadius` (`0x06002767`) to stop 0.05 m inside corpulence plus 1.5 m. Never enlarge range, teleport or bypass LoS/cooldowns. | Runtime preview.6 is the preserved red case. |
| UI advertised combat Mount although the relationship domain always rejected it | Match availability and tooltip to the existing outside-combat restriction. New exhausted ordinary TB click gets native warning feedback without ending the turn. | Red component regression 338/2, repaired before final gate. |
| Native natural TB combat shutdown reenabled AI on all controllable actors; existing repair listened only to disabling TB mode | Verified `CombatController.HandleCombatEnd` (`0x06000BE3`) prefix captures the owned Horse; postfix reacquires only that exact existing lease after native clear. | Preview.5 exercised two shutdowns without dismount. Does not change native roster, selection authority or cooldowns. |
| Single native attack tail could be interrupted by the next nonexistent attack on RT UnitAttack tick | Extend the existing one-attack terminal animation wait to RT. Let native command/animation completion finish the action; no additional attack/effect is created. | Clip handles and damage alone do not prove visible Horse animation. |
| Horse rider too high | Named Horse-only animated visual correction lowers 0.08 m in mount-root up, retains prior Z=-0.18, orientation/scale and fixed mechanics anchor. Cached animated projection and restoration remain intact. | No gameplay/nav/reach/LoS movement; Mammoth calibration unchanged. Anatomical motion approval pending. |
| Saddle art aesthetically rejected | Two new original painted leather/brass saddle masters with distinct viewpoint/stirrup/strap silhouettes. Existing tooling creates embedded 96px icons; stable ability IDs/bindings retained. | Original raster masters and generation provenance in `src/KingmakerMountedCombat/Assets/SADDLE-ART.md`. No Wrath pixels used. Actual UI states and human aesthetic approval pending. |

### Pair-local movement work remains partial

`MountedMovePrepayment` retains movement paid before the mount's verified native preparation and reapplies that debit once at that preparation. Its epoch is bound to the exact native controller, mount, RoundNumber and RoundStartTime; native assembly inspection maps those fields to roster progression. Tests cover both orders and two epochs, partial debits and no duplicate reconciliation. State is service-owned, not Harmony static state.

The movement adapter now projects the **mount's Standard as well as Move**, restores both rider fields in `finally`, and transfers actual movement to the mount. This removes the specific rider-Standard borrowing defect and attempts to preserve a pre-Prepare mount debit. Rider and mount Standards remain independent; no arbitrary rider Move charge is added.

This does **not** qualify the full resource contract. Native TimeMoved, forced movement time, five-foot-step time/metres, movement limits, autostop/AoO decisions and next-round rider-first readiness still require complete pair-local handling and native traces. No current two-round/both-order ground/approach/mount-turn trace certifies remount, cancellation and mode boundaries. Known broader counterexamples remain visible; TB must not be advertised as playable until this gate closes. If the fix requires broader turn ownership, move to a genuinely pair-aware lifecycle design informed by K9, not another narrow scheduler shell.

## Final acceptance

Every row below is scoped to final binary source `b8c206331f34a1aa873bfef86a37ab1066c267fc`, the exact preview.7 package/configuration C0 above. T means `runtime-evidence/20260906-phase3g-preview7-tb-final/phase3d-horse-scenario-evidence.json`; R means the equivalent file in `20260906-phase3g-preview7-rt-final`. Row names are exact. These are **scripted native-handler integration**, through ClickUnitHandler or the native selected-ability surface, not physical desktop pointer validation. The approved desktop bridge failed twice (native pipe error 2); no substitute input driver was introduced.

| Check and expected behavior | Observation / evidence | Status |
| --- | --- | --- |
| Stationary ordinary TB longbow: rider turn, one native shot and own Standard | T `3g-rider-longbow-ordinary`: hit 14; rider Standard 0->6, other 3.50000119 unchanged; one rule/effect, no movement or off-turn attack | PASS |
| Stationary TB Rider Primary longbow: same ownership | T `3g-rider-longbow-primary`: hit 16; rider 0->6, other 2.99999928 unchanged; one rule/effect, no movement | PASS |
| Stationary ordinary TB rider melee | T `3g-rider-melee-ordinary`: **attack completed**, hit 18, rider 0->6, mount Standard unchanged; Horse moved 0.401330382 m, exceeding 0.1 m stationary bound | FAIL |
| Stationary TB Rider Primary melee | T `3g-rider-melee-primary`: **attack completed**, hit 22, rider 0->6, mount Standard unchanged; Horse moved 0.3029086 m | FAIL |
| Stationary ordinary TB Horse Bite: Horse turn and own Standard | T `3g-horse-bite-ordinary`: hit 10; Horse 0->6, rider Standard unchanged 0; one rule/effect, no movement | PASS |
| Stationary TB Mount Primary Bite: same ownership | T `3g-horse-bite-primary`: hit 16; Horse 0->6, rider unchanged 0; one rule/effect, no movement | PASS |
| Same-target RT longbow clicks must not starve shots | R `3g-rider-longbow-ordinary`: 34 clicks, one intent generation, two resolved shots, no Horse attack or approach, damage 6, no forced rolls | PASS within this two-shot sequence |
| RT Rider Primary longbow retains native effect | R `3g-rider-longbow-primary`: one native shot, damage 4 | PASS |
| Same-target distant RT melee retains approach and repeated attacks | R `3g-rider-melee-ordinary`: 22 clicks, one generation, two rider and one Horse resolved attacks, damage 68, Horse-owned approach 4.24869251 m | PASS within this sequence |
| RT Rider Primary melee | R `3g-rider-melee-primary`: native attack/effect, damage 20, approach 1.68504369 m | PASS |
| Ordinary RT pair attack includes an independently legal Horse Bite | R `3g-horse-bite-ordinary`: 12 clicks, one generation; rider and Horse each resolved once, damage 31 | PASS |
| Explicit RT Mount Primary requiring approach | R `3g-horse-bite-primary`: Interrupt, child starts 0, rules/effects 0, Standard uncharged; exact command-container failure below | FAIL |
| Paused Dismount queues, then executes once after unpause | R `3g-paused-dismount`: unstarted while paused; one dispatch, native Success, unmounted | PASS |
| Stop cancels queued paused Mount | R `3g-paused-mount-stop`: zero dispatches, native Interrupt, unacted, remains unmounted | PASS |
| Paused Mount queues, then executes once after unpause | R `3g-paused-mount-execute`: unstarted while paused; one dispatch, native Success, mounted | PASS |
| Same-target cadence equals an untouched one-click sequence across all phases | Anti-starvation is demonstrated above. No new matched one-click timing control or exhaustive approach/windup/cooldown/release matrix | NOT RUN |
| Wrong-turn/exhausted/invalid feedback; party commands and ordinary Stop/retarget precedence | Deterministic policies passed; Howie's preview.4 successes preserved. Full native regression of these cases on preview.7 was not run | NOT RUN (native) |
| Queued target invalidation, repeated pause toggles, replacement and every modal state | Policy/lifecycle cleanup tests passed; only the three native paused rows above ran | NOT RUN (full native matrix) |
| RT->TB and fresh native TB encounter entry | T longbow case 0 enters from mounted RT; case 1 starts with TB already enabled. All six case shutdowns preserved the pair until intended cleanup | PASS for those boundaries |
| Mode switch with an existing RT target intent, remount and two-round/both-order resource accounting | Partial production reconciliation implemented; complete native side effects and cross-surface refresh remain unqualified | DEFERRED / TB BLOCKER |
| Visible Horse ordinary and explicit Bite, stop/idle return | Native Bite handles acted/finished without interruption in both TB cases and ordinary RT. Only 3 TB ordinary Horse frames, 1 explicit, and 1 per RT Horse action; framing/occlusion inadequate for full motion acceptance | HUMAN VALIDATION PENDING |
| Small Horse Y-only seating correction, complete motion/front/side/rear/indoor/outdoor | Source/transform/cleanup regressions passed. T 38 frames, max projected write residual 9.6109602054639254e-7 m; observed residual 0. R 43 frames. Useful indoor RT side image 025 inspected; this does not establish anatomical seat accuracy or full animation following | HUMAN VALIDATION PENDING |
| Original Mount/Dismount art at actual UI normal/hover/disabled/cooldown | Original masters, 96px assets, bindings and enlarged/48px preview checked. Actual HUD state and aesthetic acceptance not obtained | HUMAN VALIDATION PENDING |
| Unmounted longbow before/after, unmounted Horse animation, independently mounted Mammoth regression | Historical human longbow baseline and offline independent-profile tests preserved; no fresh candidate runtime control | NOT RUN |
| Transient cleanup / save-load / view replacement / area and incapacity boundaries | Final local cleanup and exact external restoration pass. One Working load, zero saves per final run. This does not certify a save/load or complete lifecycle matrix | PASS cleanup; broader matrix NOT RUN |

The melee stationary failures have a concrete setup explanation, not an attack-admission failure: setup stopped around 2.54 m, beyond the equipped attack's 2.2096 m radius. Native delegated movement then approached before the attack. Mount Move became 0.0871079 / 0.066071704 while rider Move stayed 0. These traces support narrow movement ownership, not full accounting. Assertions were not weakened or relabeled.

The final RT explicit Horse approach failed in `MountedPairAttackCommand.BeginDelegatedMove`: it requires `mount.Commands.Empty` even when the exact mount-owned attack occupies its Standard slot. Initial pair distance 3.84622431 m exceeded Bite radius 3.42879987 m; it threw the historical message "Mammoth command container was not empty before exact delegated movement ownership." This was a Horse, not a Mammoth-profile test. It ended without an attack, resource charge or unrelated dismount. The next narrow repair must recognize the exact owned Standard command while retaining empty Move/queue and excluding foreign commands. No ninth transaction or untested replacement package was produced.

### Bounded runs and restoration

All eight runs used the guarded helper with exact source/package/suite identity and internal WhatIf before mutation, KMC-owned fixtures, exclusive process ownership and restoration in `finally`. Exact installed candidate bytes were checked by the harness against the manifest before launch; final runtime startup reported matching version/DLL/MVID and C0/overlay absent. Guard allowance was narrowed to the two named Phase 3G scenarios; guard SHA `cf7943150c0086db4475a5cb7abfba08115697a7402788eb23052376f87a5313`.

| Run under `runtime-evidence/` | Source | Preserved outcome |
| --- | --- | --- |
| `20260906-phase3g-preview1-tb-A` | `e0aab35e135f95ca0c8498be6abf0110b43adefd` | FAIL initial Mount timeout; no attack case. Blocking UMM UI observed, not attributed to a foreign mod. |
| `20260906-phase3g-preview2-tb-A` | `25632150a70cb5bd7bc6dd852e3d388526e3b427` | FAIL fixture target spawn below unchanged 3 m minimum; Mount succeeded. |
| `20260906-phase3g-preview3-tb-A` | `2c4f4dae83208465a95344394249e37a237dc4d2` | One ordinary TB longbow native attack/effect, then FAIL native combat-end AI lease and cleanup. Before-ledger JSON fields missing; no exported cost qualification. |
| `20260906-phase3g-preview4-rt-A` | `2e84228aefc1e7518c2a2bfc9bfb7ac4816e01db` | FAIL UI-admitted combat Mount rejected by unchanged domain. |
| `20260906-phase3g-preview5-tb-A` | `582f47fa1abd851b040750ffc7a92183e00b7b7d` | Two TB longbow rows PASS with exact ledgers, then FAIL RT setup wait before melee. Native combat-end lease repair exercised twice without dismount. |
| `20260906-phase3g-preview6-rt-A` | `040ddd81e3c0c4a00047048974112551d637927b` | FAIL Unlimited Mount command reached execution outside adjacency. |
| `20260906-phase3g-preview7-tb-final` | final source above | Overall FAIL: attack rows 4 PASS / 2 FAIL; native aggregate 49 PASS / 4 FAIL (includes parent accounting). All three attack types nevertheless completed on their proper turns. |
| `20260906-phase3g-preview7-rt-final` | final source above | Overall FAIL: control/attack rows 8 PASS / 1 FAIL; native aggregate 53 PASS / 2 FAIL. Paused controls and repeated attacks passed; explicit Horse approach failed. |

Each has an immutable `E/<run-id>-output_log.txt` and independent `E/<run-id>-restoration.json`; source-specific packages/manifests remain in artifacts. Failed traces and incomplete historical protocol outcomes are preserved, not transferred to the final build as PASS. All eight independent audits restored the exact intake saves, preview.4 plus cache, foreign Mods and settings; no game or runtime locks remain. Final TB audit `2026-09-06T03:27:24.1191531Z`, log SHA `070bd9c53c3e2bbf4222f95a6ee7c63493c218a30b5d376a2b959ca0751d6249`; final RT audit `2026-09-06T03:36:26.6890486Z`, log SHA `0cadebb498c5c9fc9fd0c81413c26a7d62de287a7f5ea67f9bc05a355f714369`. Neither final run wrote a save or requested an unauthorized load/save.

## Offline gate actually run

Final source code: Release build PASS; source 22/0; components 341/0; visual/source contracts 23/0; inventory hashing 10/0; harness 243/0 (plus its historical protocol subprocesses); Phase 3G protocol mutation checks 14/0; exact assembly checks 404/0; native resource/guard contracts 9/0; exact package validation 10/0. Logs: `E/final-preview7-release-gate.txt`, `E/final-preview7-native-resource-contracts.txt`, `E/final-preview7-package.txt`. Source/policy/assembly checks are not gameplay or visual certification.

The Horse large portrait was losslessly recompressed to retain the unchanged 4 MB DLL cap while adding original icons. Every ARGB pixel, including transparency, was verified equal; decoded pixel SHA `80b990fec63fd32088034e240cf5184a2030df5dbb8b1b3f48f932f059ff049f`, proof `E/portrait-repack-evidence.json`. No threshold or package allowlist was weakened.

## Next dependencies and human check

Use [the short native checklist](PHASE3G-PLAYTEST.md) for changed controls, Horse motion, the small Y correction and icon aesthetics. No HUMAN PASS is claimed for this candidate.

1. Close any remaining native attack/pause/cadence failures listed above; capture visible Horse ordinary/explicit attacks and actual HUD icon states. Preserve the established RTWP human successes.
2. Finish pair-local TB resource side effects and qualify both native turn orders over two rounds. If that requires broader ownership, specify and implement the next pair-aware turn lifecycle/command/resource architecture using the documented K9 boundary. Separate turns remain an intermediate constraint.
3. Complete affected lifecycle/save and independent Mammoth/unmounted controls, then close prioritized combat-rule/UX gaps. Full attacks, reach/AoOs, spell/item actions, charge/feats, multiple mounted party members, wider mount bodies and relationship persistence require explicit qualification.
4. Add Paladin Divine Steed through the existing companion architecture after the core is accepted. Preserve the original Horse/Ranger/AddPet content and independently supported Mammoth; no reskin or product dependency.

## Publication and optional guarded installation

Implementation/test commits through `b8c206331f34a1aa873bfef86a37ab1066c267fc` were host-published and the helper verified the remote SHA. Receipt `E/final-source-publish.txt`; documentation publication is recorded in `E/final-documentation-publish.txt` and the final handoff. No public release, tag, merge or force push was made.

**Preview.4 remains installed.** The new partial preview has not been permanently deployed. After explicitly deciding to install it and closing Kingmaker/UMM normally, use PowerShell on the host. The first command is the required dry run; execute the second only for an authorized permanent replacement:

```powershell
$kmcGuard = 'C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1'
$kmcPackage = 'C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3g-preview.7-native-controls-private-preview-diagnostic.zip'
& $kmcGuard -Operation Replace -PackagePath $kmcPackage -AllowDocumentationDescendant -WhatIf
& $kmcGuard -Operation Replace -PackagePath $kmcPackage -AllowDocumentationDescendant -Confirm:$false
& $kmcGuard -Operation VerifyInstalled -PackagePath $kmcPackage -AllowDocumentationDescendant
```

The guard verifies the exact clean branch/source or permitted documentation descendant, preserves the existing KMC payload/cache in an exact backup and checks foreign Mods. Do not manually curate Mods or replace a loaded DLL. Keep both experimental flags and the diagnostic overlay false. A new runtime campaign or permanent deployment needs its own current-state continuity; old suite pins do not authorize overwriting later human saves/settings.
