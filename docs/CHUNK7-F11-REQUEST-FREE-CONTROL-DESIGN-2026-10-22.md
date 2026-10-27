# Chunk 7 F11 — request-free normal-player control: design

Status: **design checkpoint.** Nothing is implemented yet, and no native run exists. The
standing completion mission remains IN PROGRESS.

## Gap

The mission's F11 row requires request-free, diagnostics-off normal-player controls. Every
existing KMC launch passes `-kmcRuntimeRequest`; even batch137's startup control was a
request-based `mod-load-smoke`. The only observer mod, `tools/KmcRemovalObserver`, proves KMC is
*absent*: the runner stages it with KMC removed (`live-clone-minus-kmc-plus-observer`).

KMC's no-request path needs no change. `RuntimeAutomationHost.CreateFromCommandLine` returns
null without the argument, so there is:
- no telemetry writer or overlay;
- no persistence isolation;
- no `MountedRuntimeMeasurement`.

The offline quiet-defaults (28/0) and diagnostics-off (29/0) contracts cover this. It has never
been observed natively with a loaded world and ordinary input.

## Planned run

KMC is staged from the frozen package, exactly as for a player, with **no KMC arguments**. The
observer is staged beside it and is the only consumer of a request (`-kmcObserver*`). On a
disposable copy of an admitted fixture (candidate: the batch185 cold-qualified six-Horse
Manual308, whose riders already own native Horses), the observer:

1. **At the main menu, proves:**
   - the KMC UMM entry is active from the staged path;
   - the KMC assembly and Harmony owners are present;
   - no `-kmcRuntime*` argument is in the process;
   - no KMC overlay object exists.
2. **Loads** the declared archive through the native main-menu path, with the existing
   `NativeSaveRouting` keeping every save root inside the owned profile.
3. **Drives ordinary native input only:**
   - select a rider;
   - the native Mount ability (by blueprint GUID) through `SelectedAbilityHandler`, targeting
     its own Horse;
   - one ground click on the mounted rider;
   - the native Dismount ability.

   Mounted state is observed natively: the rider view under a `KMC_RiderPositionAnchor` child
   of its Horse's view, and both actors moving together. The observer never references KMC
   types.
4. **Records its own evidence**, because KMC measures nothing without a request:
   - Unity/UMM log lines attributable to KMC;
   - KMC hotbar entries before and after;
   - whole-frame time in idle windows at zero and one pair;
   - no save written.
5. **Writes** a new `kmc-normal-player-observer` result and quits.

## Changes required (from the read-only survey)

- **Observer:**
  - a request schema 3 with kind `normal-player` that accepts the declared Working descriptor
    instead of the `KMC_CLEANUP` gate;
  - a separate session class with presence checks and the drives above;
  - the package allowlist is unchanged, and the observer stays free of KMC references.
- **Runner:**
  - a new selected scenario/case, separate from the removal `$isObserver` cases;
  - added to the save-backed scenario list and `Test-RuntimeRequest.ps1`;
  - an owned-profile copy of the fixture;
  - launch with only the observer arguments.
- **Mods transaction:** a fourth staging mode, `live-clone-plus-kmc-and-observer`, overlaying
  both frozen packages, and threaded through enter, restore and recovery.
- **Profile:** the UMM Params delta must allow the observer entry while the KMC entry remains.
- **Reader:** a new result reader with an exact check set, bound to both package identities and
  the same commit.

## Guards that refuse it today (each to be extended, never weakened)

- **Runner:** observer only for removal cases; observer and base-game-only are exclusive.
- **Observer:** `ObserverRequest.Validate` (schema, `KMC_CLEANUP` archive); the absence checks.
- **Mods transaction:** the `Enter-KmcModsTransaction` mode set, the single-root overlay, and
  the KMC-residue throw in observer mode; the `Restore`/`Recover` mode lists.
- **Readers and profile:**
  - `Assert-KmcBaseGameNativeInventory`, which allows exactly one UMM entry; the run will
    therefore use the live-clone path, not base-only;
  - `Assert-KmcNativeUmmStartupDelta`;
  - `Test-KmcObserverResult.ps1`.

## Not claimed

This record is a plan. Performance acceptance needs the final package and a predeclared
threshold; this run would supply request-free whole-frame and log evidence, not KMC Update
timing.
