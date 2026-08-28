# Horse private-alpha playtest

## Current hold - dev.24 stabilization package not yet installed

The user-confirmed dev.23 result is now recorded: real Ranger Horse creation, visibility, control, Mount, and mounted relationship are PASS. The current package remains dev.23 while one bounded dev.24 stock-lifecycle comparison and Horse-only seat/leg calibration are qualified. Continue using dev.23 only as historical human evidence; wait for the next exact install command before final lifecycle and pose review.

## Dev.23 focused test installation - 2026-08-28T00:55:52Z

Status: INSTALLED FOR FOCUSED HUMAN TEST. Automated lifecycle qualification remains blocked; do not proceed into mounted Horse presentation testing yet.

Installed identity:

- version: `0.1.0-phase3b-dev.23`;
- UMM ID: `KingmakerMountedCombat`;
- package SHA-256: `e2a903503f415fb96c69104731e1ebedc517ff4db45b1014a6aa4e460add67a2`;
- DLL SHA-256: `eb946528bd7e0518dee61217de584f5c6ab8413de6242facc79143f9d5c6f9b1`;
- DLL MVID: `511f3511-2392-4f7e-a7ca-643701ced087`.

Focused checklist:

1. Use a real Ranger level-up and choose `Hunter's Bond -> Animal Companion -> Horse`.
2. Commit the level-up and confirm the Horse appears, remains visible after leaving the level-up UI, has a portrait, is owned by the Ranger, can be selected, and can move independently.
3. Save while unmounted, reload, and confirm the same Horse relationship remains usable.
4. In ordinary combat, observe whether damage can make the Horse unconscious or dead; record whether normal recovery or resurrection works.
5. Respec away from Horse and confirm the Horse is removed without an orphan or duplicate; respec back only if desired.
6. Stop and report these results before testing mounting or mounted presentation.

Known automated failure: the guarded dev.23 probe applied `27` direct damage to an `11`-HP Horse but received no exact non-conscious life-state event within 30 seconds. The manual ordinary-combat check is intended to distinguish a diagnostic injection limitation from an actual player-facing lifecycle defect.

## Dev.23 lifecycle qualification blocker - 2026-08-27T23:34:08Z

Status: NOT READY. Do not install dev.23, resume mounted Horse review, merge the horse branch, or begin Paladin work.

Automated evidence now proves that a real Ranger level-up can select, create, and retain the exact Horse companion with reciprocal ownership, an active native view, direct control, stock movement, and exact RT/TB Bite behavior. However, the final bounded lifecycle process did not observe the exact Horse entering `Unconscious` or `Dead` after its direct lethal-damage probe, so recovery and respec cleanup were not qualified. The guarded deployment helper confirms that no KMC version is currently installed.

A human test artifact requires either a separately authorized lifecycle investigation/repair that passes cleanly or an explicit user decision accepting this unqualified lifecycle boundary. No such decision is inferred from the creation/spawn stabilization request.

## Dev.20 qualification hold - 2026-08-27T16:01:32Z

Status: NOT READY. Dev.19 is preserved as an independently restored registration failure and must not be installed. It stopped before Ranger level-up because its new dual-array hardening imposed the seven-entry AllFeatures shape on the distinct live `Features` field.

Dev.20 leaves `Features` untouched and leases only Kingmaker's Items-authoritative `AllFeatures`. Offline gates pass, but no dev.20 package is a playtest artifact until one clean native Ranger-level-up run and audit pass. Do not resume mounted horse review, merge the horse branch, or begin Paladin work.

## Stabilization hold - 2026-08-27T14:16:40Z

Status: NOT READY. The dev.17 artifact below failed the human Ranger companion-creation gate and has been removed through the guarded deployment helper. Do not reinstall it, continue mounted presentation review, merge the horse branch, or begin Paladin work.

Dev.18 proved that the exact native Ranger level-up path can commit the Horse feature and create the exact pet; its two failures were incorrect rank/selection-level assertions. Dev.19 hardens canonical registration and corrects the Ranger-4 contract. A new install command and focused Ranger level-up/save/reload/respec checklist will replace this hold only after one clean independently audited unmounted qualification.

The older ready-artifact section is retained solely as historical identity and is superseded by this hold.

## Ready artifact - 2026-08-27T03:40:27Z

Status: READY - exact package installed; manual visual and gameplay review required.

The exact dev.17 implementation passed in-game horse qualification `51/0` and same-package targeted Mammoth regression `62/0`, with independent audit-before-evidence restoration. Automation proves command/action ownership and bounded behavior; it does not claim ordinary mouse feel, presentation quality, or real save/reload usability.

Technical note (2026-08-27T00:31:21Z): dev.16 is historical independently audited restored `FAIL 43/1`. It proves registration `13/0`, horse behavior `30/1`, and the complete mounted Rider-primary production chain. Its only failure is the Horse-primary admission gate treating the qualified first additional-limb Bite as ineligible because of a retained Mammoth primary-hand assumption. Dev.17 narrowly accepts exact first-limb Bite while preserving exact slot/weapon identity and rejecting every broader candidate. It is offline-green but is not a playtest artifact until the one authorized clean mounted aggregate passes.

Technical note (2026-08-26T22:58:55Z): dev.15 is historical independently audited restored `FAIL 41/1`. It proves registration `13/0`, horse behavior `28/1`, target-selected Mount, the horse-specific profile, RT/TB mounted movement, and pair retention. Its only failure is the guarded scenario clicking Rider primary while native post-TB mode remained `Pause`, producing the intended `LifecycleBoundary` rejection. Dev.16 releases time within the existing pause-restoration lease before that click. It is offline-green but is not a playtest artifact until the continued mounted aggregate passes.

Technical note (2026-08-26T21:30:54Z): dev.14 is historical independently audited restored `FAIL 33/1`. It re-proves registration `13/0`, horse behavior `20/1`, and exact RT/TB Bite chains; its only failure is the guarded scenario waiting for exploration while exact native post-TB game mode remained `Pause`. Dev.15 releases time within the scenario's existing pause-restoration lease, then waits for unchanged production Mount availability. It is offline-green but is not a playtest artifact until the single final mounted aggregate passes.

Technical note (2026-08-26T20:02:37Z): dev.13 is historical restored `FAIL 33/1`. It proves registration `13/0`, horse behavior `20/1`, and exact RT/TB Bite chains with zero duplicate pair attacks. The only failure is the diagnostic attempting target-selected Mount on the same frame it requested native combat/mode exit; production correctly rejected that unsafe frame. Dev.14 waits for the exact production Mount availability before arm/click. It is not a playtest artifact until the continued mounted aggregate passes.

Technical note (2026-08-26T15:03:02Z): dev.10 is historical restored `FAIL 32/1`. It proves registration `13/0`, unmounted behavior `19/1`, and exact TB Standard-slot command admission. The only failure is the native controller's queued next-unit handoff replacing the first diagnostic horse turn before Bite start. Dev.11 requires a two-frame stable exact horse turn and one bounded public reassertion only after observing that replacement. Production horse/Mammoth behavior remains unchanged. It is not a playtest artifact until the complete unmounted aggregate and mounted horse qualification pass.

Technical note (2026-08-26T13:26:50Z): dev.9 is historical restored `FAIL 31/1`. It proves registration `13/0`, unmounted pre-TB behavior `18/1`, and exact RT Bite `1/1/1`, forced D20 `3`, zero unexpected pair attacks, and `16` damage. The only failure is a TB Bite dispatched before the complete native turn/action readiness boundary, yielding `0/0/0`. Dev.10 repairs only that guarded scenario, requires exact Standard ownership and terminal success, and leaves production horse/Mammoth behavior unchanged. It is not a playtest artifact until the complete unmounted aggregate and mounted horse qualification pass.

Technical note (2026-08-26T11:20:50Z): dev.8 is historical restored `FAIL 28/1`, but it proves the dispatch repair and one correct RT Bite chain (`1` attack, `1` roll, `1` damage rule, `15` damage). The remaining failure is an invalid diagnostic requirement for exactly one D20 event; credited stock critical evidence records multiple D20 events for one attack chain. Dev.9 corrects that validator, exposes the RT/TB forced-roll and duplicate counters, and retains the zero-duplicate requirement. It is not a playtest artifact until the complete unmounted aggregate and mounted horse qualification pass.

Technical note (2026-08-26T09:47:04Z): dev.7 is historical restored `FAIL 28/1`; it established a guarded-scenario identity error, not a horse product failure. Kingmaker merged the requested same-target Bite into an already active native attack, so the submitted object never owned Standard while native combat continued. Dev.8 deterministically isolates the temporary horse's explicit RT/TB test commands. It is offline-green but not a playtest artifact until the complete unmounted aggregate and mounted horse qualification pass.

Technical note (2026-08-26T08:08:44Z): dev.6 proves exact Bite/Hoof/Hoof enumeration and every preceding unmounted gate, but remains historical restored `FAIL 28/1` because its first stock RT Bite did not terminate before the old global deadline. Dev.7 observes the exact native start/approach boundary with a 20-second diagnostic-only snapshot. No package is a playtest artifact until unmounted and mounted horse qualification pass.

Technical note (2026-08-26T06:33:22Z): dev.5 reached combat but remains historical `FAIL 26/1` because its hands-enabled body made stock full attack enumerate Bite twice. Dev.6 uses the exact native horse no-hands topology with ordered Bite/Hoof/Hoof natural limbs. It is offline-green but not yet a playtest artifact; one clean unmounted aggregate remains required.

Technical note (2026-08-26T05:10:00Z): dev.4 runtime proves the exact Call of the Wild target-XP handoff and zero duplicate progression retries, but aggregate `20260826T043600Z-horse-companion-unmounted-dev4-passC` remains historical `FAIL 25/1` after a later guarded diagnostic-target placement defect. Dev.5 repairs only that owner-relative test boundary. No package is a playtest artifact until the complete aggregate and mounted-horse qualification pass.

Historical status below: TODO (superseded by the ready artifact above)

## Artifact identity

- branch: `codex/mounted-combat-phase3-horse`
- implementation commit: `04a86870322f136bc3d7423b2e0ef31cf06d4145`
- closure documentation/harness commit: `a5c5d513f0cb4e281803a0e7c895119377610611`
- version / UMM ID: `0.1.0-phase3b-dev.17` / `KingmakerMountedCombat`
- package: `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3b-dev.17-diagnostic.zip`
- package SHA-256: `5d61b8febad67637954ad52f7e0bf8f6081fc2ffa87266407721fc00b4d5585e`
- manifest SHA-256: `75bb23b5289cce77799f8001f6966346cd324ac80c787fe1bcf49ea4e0963ced`
- DLL SHA-256 / MVID: `505c5c983ad94bbfc7e287284743427bc331d90fd6d4f9d6aacc16fe653e6875` / `4d9fff51-a040-41d4-b642-6f433c7a4b6a`
- package contents: 335-byte `Info.json`; 1,339,904-byte `KingmakerMountedCombat.dll`
- horse runtime: `20260827T014000Z-horse-mounted-dev17-passF`, in-game `51/0` (`13/0` registration, `38/0` behavior)
- Mammoth regression: `20260827T030300Z-mammoth-primary-dev17-passA`, `62/0`
- offline gates: source `21/0`, Release, component `254/0`, visual/source-order `17/0`, harness `232/0`, assembly `349/0`, PowerShell parser `28/0`, JSON parser `7/0`, diff/package/prohibited-payload PASS

## Installation and uninstall

The exact package is already installed and byte-verified in the local Kingmaker UMM Mods directory. Do not manually copy or edit the live payload.

Fresh install when KMC is absent:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Install -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3b-dev.17-diagnostic.zip" -Confirm:$false
```

Guarded replacement when another KMC version is installed:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Replace -PackagePath "C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3b-dev.17-diagnostic.zip" -Confirm:$false
```

Guarded uninstall:

```powershell
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "C:\Dev\KingmakerMountedCombatLab\codex-policy\Manage-KingmakerMountedCombatDeployment.ps1" -Operation Uninstall -Confirm:$false
```

Before uninstalling a save that selected the KMC horse companion, respec to a stock companion choice and save unmounted. Removing a mod-defined companion blueprint from a save that still references it is unsupported.

## Manual checklist

- create a Ranger horse companion and confirm portrait, ownership, selection, level, size, and party position;
- move and fight with the horse unmounted in RT and TB; exercise bite/hoof attacks;
- save/reload, area transition, death/recovery, and respec away/back while unmounted;
- mount with the overlay fallback and with activate-Mount-then-click targeting;
- reject an ineligible target with a useful reason;
- inspect idle, walk, run, turns, stop, reverse, doorway, and group movement from ordinary camera angles;
- inspect seat, pelvis, knees, feet/stirrups, scale, one-handed weapon clearance, clipping, and selection circle;
- test RT movement and Rider/Horse primary;
- test TB horse turn movement, rider-turn delegated movement, Rider primary, and Horse primary;
- transition RT→TB→RT and inspect selection, portrait, action bar, camera, pose, and control;
- click a distant door, observe horse approach, one rider interaction, opening, traversal, and target reach;
- open menus and observe fog/world flash or rider disappearance;
- use Wild Shape and revert; confirm clean dismount and visible rider;
- verify mounted ranged rejection and stock unmounted ranged control;
- save and area-transition while mounted; confirm intentional clean dismount and no automatic remount;
- explicitly Dismount and inspect complete cleanup;
- repeat the focused Mammoth controls to detect shared-subsystem regression.

Report visual feel separately from rule/control failures. Horse acceptance does not authorize Paladin Divine Steed or merge to `main`.
