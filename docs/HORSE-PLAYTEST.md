# Horse private-alpha playtest

Technical note (2026-08-25T23:15:42Z): dev.2 aggregate `20260825T222800Z-horse-companion-unmounted-passA` remains historical `FAIL 22/1` with exact external restoration. It is not a playtest artifact. Dev.3's single guarded native progression retry must pass before this checklist is activated.

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
