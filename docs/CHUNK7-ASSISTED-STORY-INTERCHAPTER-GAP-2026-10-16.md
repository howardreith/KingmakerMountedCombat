# Chunk 7 assisted story driver: interchapter gap — 16 October 2026

Native attempt 11 (`20261015-chunk7b-world10-assisted-fixture-02`, preview10 product
`0e1ce2d92a368866ce0803d070a6f3b7b96bd971`) is the first assisted producer run that reached the
game: the fresh isolated campaign was created, its own initial autosave was the single authorized
write (inside the run-owned isolated profile; no Working, baseline or human save file was written),
and the derived target was recorded. The run then failed inside the product's
assisted story driver with "Assisted story setup made no observed native progress within its
bound." The harness, restoration and settlement all completed; the exact receipts are in
[the native observations](CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md).

## What the driver observed

The story driver's last observation (frame 6577, 30.1 s after the story stage began) was mode
`Dialog`, `uiStatus` `unsupported-dialog-kind`, `managerKind` 2, `transitionPending` true,
`dialogActive` true, `mainViewReady` true, the PC/mouse controller present, no unexpected popup,
no cutscene, zero offers and zero answer rows, zero attempts and zero native clicks. The dialog
blueprint was `92fb2c819ba16a24eaa73351e9708523` with current cue `7c0daa9d52ff5f443aeca3cfaa086ffe`.

The native boundary (`Chunk7AssistedFixtureNativeBoundary`) reports `unsupported-dialog-kind`
with `managerKind` equal to the blueprint's `DialogType` whenever that type is not `Common`.
Read by reflection-only load from the installed `Assembly-CSharp.dll`, the enum
`Kingmaker.DialogSystem.Blueprints.DialogType` is:

| Value | Name |
| --- | --- |
| 0 | Common |
| 1 | Book |
| 2 | Interchapter |

So the fresh main campaign opens on an **Interchapter** page.

## Why the driver stalled instead of refusing

`Chunk7AssistedStoryDriver.Tick` returns `Waiting` while `TransitionPending` is true, before it
reaches the explicit refusal "Book/interchapter story input is not selected." The boundary counted
`DialogController.m_CueToPlay` and `m_ContinueCue` as pending transitions. By the installed IL
(verified by the independent review and by a field-store scan of the controller's methods),
`ScheduleCue` stores `m_CueToPlay` and `m_CuePlayScheduled`, `Tick` clears only `m_CuePlayScheduled`
and merely loads `m_CueToPlay`, `m_CueToPlay` is cleared only by `Clear()` at dialog start/stop, and
`PlayCue`/`AddAnswers` set `m_ContinueCue` whenever a continue affordance is displayed. So every
dialog looked pending from its first cue, common pages included, and the driver waited until its
30-second progress bound expired. Only a scheduled dialog start, a scheduled cue play or a
scheduled stop actually defers input. The driver only ever selects ordinary PC dialog answer rows
(`uiStatus` `pc-cached-rows` on the `DialogBookEventUIManager.Dialog` controller); it has no
input path for the book-event controllers.

## Native surfaces for the successor candidate

`Kingmaker.UI.Dialog.DialogBookEventUIManager` (read-only field tokens from the installed assembly):

| Field | Type | Token |
| --- | --- | --- |
| `Dialog` | `Kingmaker.UI.Dialog.DialogController` | `0x04002EB9` |
| `BookEvent` | `Kingmaker.UI.BookEvent.BookEventController` | `0x04002EBA` |
| `Interchapter` | `Kingmaker.UI.BookEvent.BookEventInterchapterController` | `0x04002EBB` |
| `m_State` | `DialogType` | `0x04002EBC` |
| `m_IsShow` | `bool` | `0x04002EBD` |

`BookEventInterchapterController` (base `BookEventBaseController`): fields `m_Title`
(`0x04002D97`), `m_TitleContainer` (`0x04002D98`), `m_Cue` (`0x04002D99`), `m_Answer`
(`TempOptionUI`, `0x04002D9A`), `m_Portrait` (`0x04002D9B`), `m_ForeImageCanvasGroup`
(`0x04002D9C`); methods `SetPage(BlueprintBookPage, List, List)` (`0x0600443B`),
`OnPageShow()` (`0x0600443C`), `Clear(bool)` (`0x0600443D`).

`BookEventController`: `Cues` (`0x04002D8C`), `Cue` (`0x04002D8D`), `Answers` (`Transform`,
`0x04002D8F`), `Answer` (`TempOptionUI`, `0x04002D90`), `ChooseCharacter` (`0x04002D92`),
`m_MemorizedCues` (`0x04002D96`); `SetPage` (`0x06004432`), `Clear` (`0x06004433`).

The existing PC dialog path already owns the native click: the boundary requires
`TempOptionUI.OnClick()` (`0x06004616`), reads each row's `m_Answer` (`0x04002EE0`), `m_Index`
(`0x04002EE1`), `m_ChooseCharacterInit` (`0x04002EE2`), `m_IsInit` (`0x04002EE3`), `m_Disabled`
(`0x04002EE4`) and `m_Interchapter` (`0x04002EE8`), and `Chunk7AssistedFixtureNativeBoundary.Click`
re-verifies the snapshot before calling `OnClick` on the selected row. The interchapter option and
the book answers are the same `TempOptionUI` type, so the same verified click path applies.

## Product change (implemented on `codex/mounted-combat-chunk7-story-driver`, commits d44a386a, 30a0cdbf and 3d2aff60; independent review `f141ef8c0f35a7dedc6e4801f5e6382de4cf40b399f12ea5f9c2132ab133fac6` PASS with the pending-transition finding, addressed by the third commit)

1. Extend the boundary read to report the interchapter and book controllers (shown state,
   the single interchapter `m_Answer` option, book answer options and their enabled state)
   with the same identity and custody checks the PC dialog path already applies, reading the
   option rows with the existing `TempOptionUI` field contract.
2. Let the driver advance an Interchapter page by selecting its single option, and a Book page by
   the same first-cached-enabled-answer policy it uses for common dialogs, through the existing
   verified `Click` path, counting each as an attempt under the existing choice cap and progress
   bound, and refusing character-chooser pages.
3. Offline checks: boundary classification for all three `DialogType` values, driver transitions
   for pending-transition interchapter pages, and the assisted-fixture evidence rows.
4. Offline results after the third commit: assisted core 100/0, affected 19/0, native facade 81/0 (200 total), reader kinds 13/0,
   wiring 87/0, source 54/0, authority 154/0, probes 50/0, passive choices 69/0.
5. A successor product candidate (build, Candidate tier, freeze, suite continuity) and a fresh
   numbered reviewed full audit before native attempt 12; eleven of 25 attempts are used.

The composed owned schema1 and area Auto drafts (branch `codex/mounted-combat-chunk7-claude-drafts`)
and the runner request normalization (branch `codex/mounted-combat-chunk7-harness-fixes`) are the
other candidates for that successor.
