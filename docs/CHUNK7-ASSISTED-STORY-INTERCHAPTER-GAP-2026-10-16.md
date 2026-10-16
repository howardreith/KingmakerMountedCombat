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
   numbered reviewed full audit before native attempt 12, which it duly admitted; eleven of 25 attempts were used when that change was written.

The composed owned schema1 and area Auto drafts, the runner request normalization and this product change are all composed on the successor branch `codex/mounted-combat-chunk7-successor`, from which preview.11, preview.12 and preview.13 are built.

---

# The complete dialog-affordance record (attempts 11 to 14)

The interchapter gap above was the first of three distinct dialog affordances the assisted producer
had to drive. Each one was found by a separate capped native attempt, fixed in a separate successor
product candidate, and then confirmed by the next attempt. Fourteen of 25 native attempts are used,
all final FAIL; every one restored Mods, Working, the immutable baseline, protected saves and the
profile bytes exactly, and every failure was independently attributed to product logic rather than
to restoration.

| Attempt | Product | Native clicks | Outcome |
| --- | --- | --- | --- |
| 11 | preview10 `0e1ce2d9` | 0 | Could not advance the opening Interchapter page at all. |
| 12 | preview.11 `d8fcba89` | 1 | Interchapter advanced (fix validated); stalled at a cutscene-embedded common continue cue with zero materialized rows. |
| 13 | preview.12 `859596f2` | 4 | Interchapter plus three system-continue advances (fix validated); refused the narration's terminal Exit answer by design. |
| 14 | preview.13 `91fd310a` | 13 | Exit answer validated; stalled when the engine re-entered an identical still-active cue in a five-choice conversation hub. |

Dialog input in this game comes in exactly two forms, and the producer needs both:

1. **Answer rows** — `TempOptionUI` instances cached in `DialogCurrentPart.m_AnswersList`
   (`0x04002ED8`), driven by `TempOptionUI.OnClick()` (`0x06004616`). The common PC dialog, the
   book pages and the interchapter option are all rows.
2. **System answers** — the four singleton answers on `BlueprintRoot.Dialog` (`0x040060F5`),
   routed not to a row but to `DialogController.SystemButton` (`0x04002EC0`), a
   `DialogueSystemButton` driven by its own `OnClick()` (`0x0600460D`).
   `DialogCurrentPart.Fill` (`0x060045FF`) skips every answer for which
   `DialogExtensions.IsSystem` (`0x0600977A`) holds before it populates `m_AnswersList`, which is
   why a system answer can be offered while **zero** rows exist.

| `DialogRoot` system answer | Token |
| --- | --- |
| `ContinueAnswer` | `0x0400618E` |
| `ExitAnswer` | `0x0400618D` |
| `InterchapterContinueAnswer` | `0x04006190` |
| `InterchapterExitAnswer` | `0x0400618F` |

`DialogueSystemButton`: `m_IsEnable` (`0x04002EDA`), `get_IsEnable` (`0x06004608`), `m_Answer`
(`0x04002EDC`), `OnClick` (`0x0600460D`). By the installed IL, `OnClick` is gated on
`get_IsEnable` and submits only `this.m_Answer` with a null manual-selection unit;
`DialogController.SelectAnswer` (`0x06009325`) bypasses the `m_Answers.Contains` membership check
for a system answer and errors only when that unit is null **and**
`CharacterSelection.SelectionType` (`0x040055FD`) is `Manual` (4). For an Exit answer
`SelectNextCue` (`0x06009326`) returns null, so `SelectAnswer` calls `StopDialog` (`0x06009324`):
clicking Exit is the player's own "close this dialog" action and cannot select a narrative answer.

## Attempt 12 (preview.11): the interchapter fix works; cutscene continue cues do not

Run `20261016-chunk7b-world11-assisted-fixture-01` (preview.11 `d8fcba89`, package
`78e5384f0c92da3edf5b6df6b1a87d921eca885a9406ec8d448982f0426c4ada`, DLL
`0d56cbf6ee2f2d56ba43f9b0130c5cbe620ff7ffb29cd37738d6d81b6a812fd0`, MVID
`384abb4c-7b09-4b89-ac8f-ac1a8f23de01`) clicked the opening Interchapter option at frame 3997
(`uiStatus` `interchapter-rows`, `managerKind` 2, one offer, one initialized enabled interchapter
row, answer `9663056282078714a8d8c8ef3daaea90`) and the page advanced and closed. The
interchapter/book support and the corrected pending-transition definition are therefore validated
in the live game.

The run then stalled one stage later and failed at its 30-second progress bound (frame 6535,
30.159 s): a **common** dialog (`managerKind` 0, `uiStatus` `pc-cached-rows`, dialog
`ae816e1a804eee342b378957097ed70b`) playing **inside a cutscene**, presenting one offer
(`selectionType` 1, answer `17cf846dddf09244a93b53eaf26c8539`) and **zero** cached rows. The
offered answer is the singleton `DialogRoot.ContinueAnswer`, reused for every continue cue, and the
row cache is empty because `Fill` had skipped it as a system answer. The row path had nothing to
click.

## Attempt 13 (preview.12): the system-continue fix works; the narration ends on Exit

preview.12 added the `DialogueSystemButton` continue path (package
`0e9dae62e854d0510c04865535621e37d010389f83ce85abe74e91c0253b5520`, DLL
`598ea086c4cb034418f52d19a879d1d1bf5aa4afc90f9f47d268e77d39f9ebb7`, MVID
`7940dc44-1bf5-4fa8-b4f2-7e9f9f627021`, manifest
`4610a9515af743f49beaac819af6226e55724122deec6812131a707d33a302b1`, suite
`chunk7b-world12-20261016`). Run `20261016-chunk7b-world12-assisted-fixture-01` made **four**
successful native clicks: the opening Interchapter row again, then three successive common
system-continue advances through the prologue narration (frames 4703, 4704 and 4705; `managerKind`
0, one offer, zero cached rows, `cutscene` true, `rowIndex` and `nativeIndex` both -1; each
`nativeEntered` and `nativeReturned` true; each advancing the same dialog to a new cue that again
presented a continue button and no rows).

It then reached the narration's terminal cue, whose sole system answer is `ExitAnswer`, and threw
the deliberate refusal "An Exit system answer ends the dialog and is not driven by assisted
continue; only continue answers advance." — exactly the liveness residual that the independent
review of the continue fix had predicted from blueprint-independent reasoning. The run failed at
8.5 s of story time with an explicit refusal rather than by exhausting a timeout, which is the
diagnostic the conservative policy was chosen to produce.

## preview.13: permitting Exit, and what it does not cover

The Exit permission reuses the same verified `DialogueSystemButton.OnClick` path under every
existing guard: a real selectable row always wins; `Rows.Length == 0`; the button present and
`IsEnable` true; exactly one offer whose answer is reference-identical to the button's `m_Answer`
(ambiguity refuses); every offer a `DialogRoot` system answer by reference identity rather than by
calling `IsSystem`; the acted answer classified as Exit by that same identity set;
`SelectionType != Manual(4)`; an empty `OnSelect` action list; the same
(controller, dialog, cue, answer) dedupe; the 16-choice cap and the 300-second and 30-second bounds
unchanged; and never a direct `SelectAnswer` or any other UI bypass. Classifying Exit by blueprint
identity rather than by `m_ContinueCue == null` is deliberate and strictly safer: it preserves the
refusal of a continue-identity answer that has no displayed cue, which `SelectNextCue` would
otherwise treat as a continue.

The post-game evidence reader was extended at the same time. It had validated every attempt as a
row attempt, so a run that **succeeded** by system-button clicks would have been rejected after the
game exited; a system-button attempt is now its own evidence kind with its own refusals, and
neither kind can masquerade as the other.

One limit is recorded rather than fixed by the Exit permission itself: the system-answer
classification is computed only inside the `DialogType.Common` branch, so a terminal
**Interchapter** Exit page would still be refused. Interchapter answers are `TempOptionUI` rows
handled by the row path, so that shape is reachable only if an interchapter page ever presents its
exit as a system answer alone. Every shape the driver does not handle is a bounded explicit refusal
costing one capped attempt, never a hang and never a wrong click.

## Attempt 14 (preview.13): Exit validated, thirteen clicks, and a conversation hub

Run `20261016-chunk7b-world13-assisted-fixture-01` (preview.13 `91fd310a`, package
`58e2fb19f95c7ca2a48076305307cef41fd35c909e0405b3e2af94285dfa1e59`, DLL
`d2493aa53426a925218db2f72fb4bd9cb27a7bfcf4e80615f25e576e0e8bc86a`, MVID
`2effa43d-d0e3-4823-9402-136dcff8c7c0`, suite `chunk7b-world13-20261016`) made **thirteen**
successful native clicks, twelve of which produced observed native advance:

| Clicks | Shape |
| --- | --- |
| 1 | the opening Interchapter row (frame 4004, `managerKind` 2) |
| 2-4 | common `DialogueSystemButton` continue advances, zero cached rows |
| **5** | the narration's **terminal Exit answer** (frame 4725) — the preview.13 permission |
| 6-8 | three more common continue advances |
| 9 | a **five-offer/five-row** common PC dialog (cue `e637f827`) — clicked row 0 |
| 10-12 | three more common continue advances |
| 13 | a five-offer/five-row dialog at a **different** cue (`d8670845`) — row 0 again |

**The Exit permission is validated.** The after-read of click 5 carries the `StopDialog` signature —
mode `Cutscene`, `uiStatus` `not-dialog`, null dialog and cue guids, `dialogActive` false and every
list identity 0 — which only the two Exit answers produce, while all nine continue clicks left the
dialog active. Attempt 13 on preview.12 had refused this very answer by policy. Note that the
weaker argument "a system-button click on a non-continue answer can only be an Exit answer" is
**unsound**: the product classifies four `DialogRoot` system answers by reference identity and its
guard throws only when the answer is not an Exit answer *and* no continue cue is displayed, so
`InterchapterContinueAnswer` is also reachable whenever that cue is shown. The conclusion rests on
the after-read, not on necessity. The evidence records no per-attempt Exit/continue flags, which is
why the identity had to be inferred at all.

### Why the fourteenth click never happened

The producer stopped on a common PC **conversation hub**: five offers, five rows, every row
initialized, active, enabled, non-chooser and non-interchapter, and every offer `selectionType` 0 —
entirely clickable. It was not a native limitation and not the dedupe. After click 13 the engine
re-entered the **identical** dialog object, cue object and five answer objects with the dialog still
active. No progress clause can fire on an unchanged observation, so the pending click never cleared,
and the early return for a pending click short-circuits *ahead of* selection, the choice cap and the
dedupe. The progress bound then expired exactly 30,004 ms after the click.

The `(controller, dialog, cue, answer)` dedupe did **not** fire, and could not have: it *throws*
rather than skipping, the receipts carry the progress-bound exception instead, and click 13 was at a
different cue from click 9 in any case. The choice cap (13 of 16), the cutscene gate, `NativeBusy`
and `TransitionPending` are each independently excluded.

Two defects therefore sit in series, and the second only becomes visible once the first is cleared:

1. The progress test is blind to **same-cue re-entry** after an owned click, so a click the engine
   consumed without advancing the cue is indistinguishable from a click that was never observed.
2. Selection takes only the **first** eligible offer. With the first offer's answer already used,
   clearing the liveness gate alone would convert the stall into the dedupe's exception on the same
   tuple. Both halves need fixing together.

Restoration was clean and independently reviewed: all five restoration flags true, the campaign's own
`Auto_1.zks` inside the run-owned isolated profile the only native save write, zero unauthorized
saves or loads, profile bytes restored exactly, all three transaction records at phase `restored`,
suite continuity PASS. The human-save claim was established four ways, including that none of the 275
live save files has a write or creation time inside the run window and that every fixture save and
protected-save authority record still hashes exactly as the qualification suite records it.

Fourteen of 25 native attempts are used, all final FAIL, and each has moved the frontier with a
precisely diagnosed successor: 0 clicks, then 1, then 4, then 13.
