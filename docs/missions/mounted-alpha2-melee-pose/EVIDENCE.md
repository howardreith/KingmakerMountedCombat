# Owner evidence and limits

The six original 1920 × 1200 PNGs are included byte-for-byte, with friendly packet filenames. `evidence/screenshots/manifest.json` maps each to its original filename, byte count, and SHA-256. These are owner-provided gameplay screenshots, not new tests executed during mission preparation.

## Screenshot index

| ID | Packet file / original filename | Direct observation | What it does not establish |
| --- | --- | --- | --- |
| E01 | `evidence/screenshots/01-initial-mount.png` / `image(20261124-144421).png` | Rider appears elevated in relation to the saddle; log includes Mount Companion and saves. | Exact anchor offset, root cause, loaded DLL identity, or whether the pose began on mount versus load. |
| E02 | `evidence/screenshots/02-after-remount.png` / `image(20261124-144510).png` | Mounted view after log entries for Dismount and Mount Companion; owner says seating improved. | A quantitative before/after height change: the camera angle differs from E01. |
| E03 | `evidence/screenshots/03-turn-based-kobold.png` / `image(20261124-144615).png` | Turn-based encounter with a close Kobold, Sword n Board at 47/47, Move display 1, and Attack available in the UI. | Actual click sequence, body/weapon distances, LOS, remaining native movement, exact paired phase, or entitlement to a step/full attack. Owner reports moving closer made the attack work. |
| E04 | `evidence/screenshots/04-mounted-shield-ac.png` / `image(20261124-144748).png` | Horse AC tooltip totals 18, including `Shield: +2 [Mounted Shield]`, natural armor +4, Dex +2, size -1 and Dodge +1. | The exact equipped shield or focus feats, all AC modes, enhancement exclusion, competing shield effects, or lifecycle/save/load behavior. |
| E05 | `evidence/screenshots/05-mounted-shield-mobility.png` / `image(20261124-144956).png` | Paused real-time view; log says Mobility 23 including `Mounted Shield +2`, against attack 19, negates the hit on the mount. | Complete frequency/boundary coverage, reader provenance, or any new defect inferred from the neighboring critical-hit log line. |
| E06 | `evidence/screenshots/06-horse-bite-trip.png` / `image(20261124-145033).png` | Horse Bite hits Kobold Soldier, deals 5 damage, followed by Combat Maneuver: Trip; detail shows 15 = 10 + 5 against CMD 5. | Which fact/component produced Trip, whether a separate ability was involved, exact horse blueprint, visible bite animation, or TB horse participation. It does prove the horse attacked in this captured flow. |

## Reported observations without a complete visual trace

The owner reports a delay before advancing on an explicit attack order, failure to acquire/approach automatically when real-time combat starts, absent bite animation despite visible hooves, and uncertainty about horse attacks in turn-based mode. Treat these as actionable reproduction reports. A screenshot cannot establish input-to-motion latency, all animation frames, or a permanently stalled command.

The supplied material contains no raw game logs, save files, settings dump, or runtime DLL hash. Do not fabricate them. If accessible during execution, inspect the current relevant logs/configuration and verify the actual installed package before reproducing. Existing owner saves may be used only through protected copies under the authorized workflow; supplying a historical save is not a precondition to starting the repair.

## Mounted Shield regression interpretation

E04 and E05 are positive evidence for two concrete +2 observations. Preserve them as owner baseline evidence. Do not relabel them as a fresh alpha-2 test pass or a complete Mounted Shield acceptance matrix. For a controlled future character, calculate the expected amount from its actual shield base bonus and focus feats; do not demand +3 merely because the previous handoff used a heavy-shield example.

## Provenance boundary

The expected comparison package is the alpha-1 artifact recorded in `references/baseline.json`; screenshot pixels do not prove that exact binary was loaded. The source audit used PR61 archive A, which is newer than its packaged production commit. The new implementation must state the exact source/package relationship, and any native evidence must identify the binary actually loaded.

The primary rules and code sources are in `RULES_AND_ASSETS.md`, `CODE_MAP.md`, and `references/source-index.json`. No repair, new package build, native session, factory enrollment, or gameplay acceptance was performed while creating this mission packet.
