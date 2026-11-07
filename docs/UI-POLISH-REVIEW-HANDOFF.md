# UI polish: rename control, UMM entry, public name — review handoff (2026-11-07)

**Status: ENGINEERING COMPLETE — READY FOR OWNER REVIEW.** Owner play acceptance is separate. No merge, release,
tag or install.

- **Branch:** `codex/mounted-combat-ui-polish`, from main `98bac4bc` (the 0.2.1 release merge). Native requests need
  the `codex/mounted-combat-` prefix, so the branch was renamed before any push.
- **Product head:** `ba166bb2`. The records commits after it change only readers and documentation.
- **Frozen candidate (native runs):** ui-polish-b at `1d98bf68`.
  - DLL `b70709a299a020745969cf721c40d4af103a2bddc6d77307dd3cf27fde6ff250`, MVID `92114a79-f88a-4a91-92a6-edafbc0f963e`.
  - Package `KingmakerMountedCombat-0.2.1-ui-polish-b-diagnostic.zip`, SHA-256 `cc4993a0…`.
  - Suite `ui-polish-b-1d98bf68-20261107`.
  - The ui-polish-a package (`0b9fb7d0…`, built at `ba166bb2`) contains the byte-identical DLL.
- **Version:** 0.2.1. The assembly version stays 0.1.0.0. This is not a release build.

## What changed

### 1. Rename control

The character sheet now has one **Rename** button, a copy of the native message box's own button. It is fixed at the
right end of the name line, vertically centred on that line.

- **Name slot:** while a companion is shown, the name label (the sheet's one `CharName` TMP text) gets an equal margin
  on both sides, the width of the button plus a gap.
  - The name therefore stays centred where the sheet puts it.
  - It never wraps. It shrinks to at most 70% of its size, then ends in an ellipsis.
  - Every other character gets the label's own captured settings back.
- **Layout:** anchoring is in the name line's own units, so canvas scaling moves the control with the line.
- **Icon:** no native quill or pencil sprite was located, so the native-style text button stays (the request's
  fallback). No tooltip is needed.
- **Removed:** **Default Name**, its confirm prompt and the service's reset path. A blank or empty entry is refused with
  the existing message and the dialog reopens. It never restores the default.
- **Kept:** Confirm/Cancel/Esc, the per-unit `UnitDescriptor.CustomName` storage and the existing saved names.

### 2. UMM entry

- **Removed:** `modEntry.OnGUI` is no longer registered and `CompositionRoot.DrawGui` is deleted. The panel removed
  had:
  - the "Enable Mounted Charge (this session…)" toggle and its explanation;
  - the "Show diagnostics" switch and its internal pair counts and routine feedback;
  - the removal-preparation button and status;
  - the version heading and help text.
  UMM shows only its standard row (name, version, on/off, status).
- **Mounted Charge** is always on while the mod is enabled. Its eligibility and gameplay checks are unchanged. Nothing
  stored can turn it off:
  - The mod never had a settings file, and UMM stores only enabled/disabled.
  - `DiagnosticSettings.EnableMountedCharge` defaults to true. Only lab scenarios under `Diagnostics/` write it, and
    they restore it.
  - `Validate-Source.ps1` now fails if any non-diagnostic source assigns it, if `Main` registers `OnGUI`, or if the
    default changes.
- **Lab-only refusal texts:** two texts that named a mod setting now read "Mounted Charge is not available." and
  "Mounted controls are not available." They are reachable only in lab-disabled states.
- **Maintenance route:** removal preparation is still necessary, because it is the only verified clean-removal path.
  It is now reached in game with **Ctrl+Shift+F11**:
  - A native Yes/No box asks first.
  - The same `MountedRemovalPreparation.Begin` runs on the next frame.
  - Its verdict is shown in a native message box when the cleanup save settles.
  - The chord is ignored while a text field holds the keyboard.
  - A Yes older than five frames is discarded, which covers a mod disabled in between.
  - A failure is logged and reported, never escalated into the update boundary that would disable the mod.
- **Logging:** internal log lines are unchanged.

### 3. Public name

**Mr. Bard's Kingmaker Mounted Combat** now appears in:
- `Info.json` `DisplayName`;
- the DLL's `AssemblyTitle` and `AssemblyProduct`;
- the Horse companion's in-game description (and its audit pin);
- the new player README.

**Unchanged:**
- the mod ID `KingmakerMountedCombat` and the folder name;
- the `KingmakerMountedCombat.dll` assembly name and the 0.1.0.0 assembly version;
- the entry point `KingmakerMountedCombat.Main.Load`, the namespaces and the Harmony identities;
- every blueprint GUID and serialized key;
- the log prefix and the reader-keyed log line "Kingmaker Mounted Combat <version> loaded";
- `version.json` and the historical release notes.

### 4. Documentation

The new root `README.md` is the player guide. It covers:
- install;
- play, including Charge being automatic and how to rename;
- no settings;
- **before removing the mod:** which campaigns still need it, the Ctrl+Shift+F11 steps and the meaning of each result;
- logs.

## Verification

### In game (Working fixture, guarded launcher, budget `ui-polish`)

The fixture also stages Call of the Wild and Craft Magic Items.

| Run | Build | Verdict | What it showed |
| --- | --- | --- | --- |
| `20261107-ui-polish-a-producer-01` | a (`ba166bb2`) | **ORIGINAL OUTER FAIL**, game PASS 4/4; corrected re-evaluation PASS | The reader refused a check I had added. It compared the native-size name's glyph box with the "Level" text's box, and an accented capital reaches 2 UI units into it with nothing visible. Recorded FAIL kept. |
| `20261107-ui-polish-b-producer-02` | b (`1d98bf68`, same DLL) | **PASS** | The naming rows listed below, the UMM capture, then a native Manual `bb7c4fec…` (857504 bytes). |
| `20261107-ui-polish-b-consumer-03` | b | **PASS** | Fresh process, cold load of r02's Manual. |
| `20261107-ui-polish-b-charge-rt-04` | b | **PASS** 4/4 | `sprint7-charge-rt`: a real-time Mounted Charge and its cancel through the ordinary ability, with `defaultFeatureEnabled=true` after startup. |

**Producer r02 rows:**
- **Sheet:** default-named Mammoth, renamed, 24-character name, the Riding Dog after switching characters, and the
  rider's own sheet.
- **Control geometry, same in every case:** exactly one control, the same rectangle, inside the line, clear of the
  name text and of every level/experience text.
- **Fonts:** native 35 for ordinary names, 24.5 plus an ellipsis for `Wolfgang Maximilian Wynn`.
- **Rider sheet:** the rider's label matches its captured native style.
- **Dialog:** OK applies; Esc and the **Cancel button** leave the name unchanged; a markup name and a blank name are
  refused, explained and reopened.
- **Per mount:** a distinct name, then a duplicate name, each stays on its own unit.
- **UMM Mods tab:** `Mr. Bard's Kingmaker Mounted Combat`, active, no options page, Mounted Charge on.

**Consumer r03:**
- Both custom names, identities, owners and the tack survive, with zero naming-service writes.
- On both cold sheets the control is the same single rectangle, clear of the level line.
- `Test-Finalize5ColdAgainstSource.ps1` passes.

**Restoration:** every run reports Mods and Working restored and the save digest `60e3d6cc…`.

**Frames** (in each run's `movement-visuals/`, not committed):
- `finalize5-f5-sheet-mammoth*.png`, `-long`, `-dog-distinct`;
- `finalize5-f5-umm-entry.png`;
- the consumer's `finalize5-f5-cold-sheet-*.png`. In these the dog's native **Level Up** bar sits clear under the
  control; it was covered before this change.

### Offline

| Check | Commit | Result |
| --- | --- | --- |
| Fast with the F5 readers | `5694d70e` | PASS 4/0 (0.8 min) |
| Fast with the F5, Chunk 7 realtime and Sprint 7 readers | `9b4c22af` | PASS 6/0 (0.7 min) |
| Candidate with the F5 readers | `9b4c22af` | PASS 8/0 (3.68 min) |
| Candidate with the F5 readers | `ba166bb2` | PASS 8/0 (3.9 min) |

- **Fast detail:** components 1118/0, Validate-Source 151/0.
- **Packages:** both passed `Validate-Package` 11/0.
- **F5 reader test:** 68 checks at the head.
- **Budget `ui-polish`:** 19.93 of 90 min; four native starts, one of them the r01 outer FAIL.

### Review

- **First review:** one independent review of `5694d70e` found no blocking defect. Its confirmed findings were
  corrected in `9b4c22af`:
  - the shortcut's input-field test;
  - the stale confirmation;
  - contained failures;
  - label-restore evidence;
  - UI-unit geometry;
  - the stale refusal text and the reader comment.
- **Re-review:** a re-review of `9b4c22af` found no defect. Its one minor point (keep a running cleanup save's report)
  is `ba166bb2`.

## Not verified

- **Ctrl+Shift+F11 in game:**
  - The route was not run in game: no synthetic key chord, dialog or cleanup save.
  - Its underlying `MountedRemovalPreparation` is unchanged and keeps its earlier native evidence.
  - The route itself is established by review and compilation only.
- **Physical mouse:** hover, pressed and focus states were not exercised with a real mouse. The control is the native
  button with its own transition, pressed through its click event.
- **Screen sizes:**
  - Only the lab's resolution and UI scale ran.
  - Other sizes rely on the control being anchored in the line's own units.
  - The 24-character worst case ends in an ellipsis on the sheet. The full name still shows in overtips and dialogs.
- **Owner saves:** an existing owner campaign was not loaded. Save compatibility rests on three things:
  - the unchanged IDs;
  - every run starting from the pre-existing Working fixture save;
  - the r03 cold load of a save written by this build.
- **Old evidence:** the F5 reader now enforces the new contract and refuses pre-polish F5 evidence. Those runs' recorded
  verdicts stand.
