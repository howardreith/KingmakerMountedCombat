# Horse private-alpha playtest

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

Status: TODO — do not use until an exact qualified package is recorded below

## Artifact identity

Branch, commit, version, package path/hash, manifest hash, DLL hash/MVID, gate totals, and credited runtime rows will be filled only after technical qualification. A locally built or ad hoc DLL is not a playtest artifact.

## Installation and uninstall

Exact guarded deployment-helper commands will be recorded for the immutable package. Do not manually copy files into the live Mods directory.

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
