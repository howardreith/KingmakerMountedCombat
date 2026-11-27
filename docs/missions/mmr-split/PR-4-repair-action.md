# PR-4 existing-horse repair: the confirmed in-game action (round 12)

This document is written under OWNER AMENDMENT revision 1 (owner instruction 2026-11-27b section 4). It describes the lifecycle that replaces
the withdrawn file-request protocol. Code: `Domain/HorseTripRepairPolicy.cs` (`HorseTripRepairConsole`, `HorseTripRemoval`) and
`Integration/HorseCompanionBlueprintService.InheritedFacts.cs`.

| Step | Where | What it may touch | Guarantee |
|---|---|---|---|
| Discovery | the service's ordinary update (`ProcessTripRepairConsole` -> `Observe`) | reads the loaded party horses and their candidate facts | Immutable snapshots (horse name, id, owner, candidate count, opaque token) go to the UI. The live unit object, its stable id, the exact fact instance and the session epoch stay private to the console. A horse with more than one candidate is shown but not offered. |
| Intent | Unity Mod Manager window, `DrawTripRepairGui` (`Main.OnMaintenanceGui`) | the console's `Select`, `Confirm` and `Cancel` only | The UI never reads or changes a unit or fact. The confirmation text names the horse and the exact fact, and says the save cannot prove where a sourceless copy came from. |
| Execution | the next ordinary update (`Execute`) | one native `RemoveFact` through `HorseTripRemoval.RemoveExactly` | The consent is consumed before anything fallible. The session marker, the world-write gate, the live object and its exact single candidate are rechecked. The outcome is classified (Removed / Unchanged / NotAttempted / Uncertain) and stored before it is logged or shown, so a reporting failure never repeats a removal. |
| Invalidation | a new session marker (a new Player object or loaded area), `Invalidate` on disable/unload, a changed candidate | the consent and the bindings | Consent is never persisted. After a load the owner confirms again. |
| Old files | none | nothing | `horse-trip-repair-request.txt`, its claim/archive/retiring entries and the result file are never read, written, moved or deleted. |

Platform scope: the action uses Unity IMGUI and the same native removal on Windows, Linux and macOS. No file operation and no platform-specific
code remain. Actual UI appearance and gameplay save/load are pending the owner's integrated-alpha playtest.
