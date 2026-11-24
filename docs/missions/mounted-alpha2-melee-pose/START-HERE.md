# Mounted Combat: melee and presentation repair mission

Prepared for Howie and Claude on 2026-11-24. Packet revision 1.

**Deliver the next private alpha of Kingmaker Mounted Combat, repairing the melee and mounting issues Howie found while testing Mounted Shield.** This packet is a task definition and evidence bundle, not an implemented fix or an installable mod.

## Start

1. Read `MISSION.md`, `OWNER_REQUEST.md`, `EVIDENCE.md`, and all six screenshots.
2. Read `CODE_MAP.md` and `RULES_AND_ASSETS.md` for source-verified leads and their limits.
3. Read `ACCEPTANCE.md`, `NATIVE_TESTING.md`, and `FACTORY_WORKFLOW.md`. Follow the repository's current instructions and the installed factory, preserving this mission's explicit private-alpha scope.
4. Verify packet integrity with `python tools/verify_packet.py`. This verifies these materials, not the mod.
5. Execute through a **new foreground factory enrollment and new draft PR**. The accepted Mounted Shield run remains untouched.

The short launch instruction is in `CLAUDE_START_PROMPT.txt`. Copy the complete packet into the new worktree at `docs/missions/mounted-alpha2-melee-pose/` before enrollment; `MISSION.md` is the single governing mission input and explicitly incorporates its companions.

## Seven work items

| ID | Requested outcome |
| --- | --- |
| MMR-01 | Rider seating is stable on first mount, mounted load, and remount. |
| MMR-02 | A legal turn-based attack order closes the gap using legal mount movement, without a second ground click. |
| MMR-03 | An ordinary pair attack lets the horse use its own eligible attacks, without a redundant separate mount order. |
| MMR-04 | In real time, an idle AI-enabled melee pair acquires and approaches an eligible nearby enemy. |
| MMR-05 | An explicit real-time attack order starts legal movement promptly, without waiting unnecessarily for the strike to become ready. |
| MMR-06 | A plain horse bite does not incorrectly grant automatic Trip; legitimate dog/wolf and granted maneuvers remain intact. |
| MMR-07 | The real horse bite has readable native animation where the installed asset supports it; preserve working hoof animation and native damage timing. |

Mounted Shield's passive AC and Mounted Combat check benefits, the existing feat icon, loader baseline, and other mounts are regression constraints.

## Baseline and endpoint

At preparation, PR #61 is open and draft at `122a9f94680d1fbf512a0f9ec7386a1447d0c1f7`, and main lacks its changes. The supported route is a new branch and stacked draft PR targeting `codex/mounted-shield`. If #61 has merged before execution, verify the accepted implementation on current main and use that base instead. Exact instructions and the archive-integrity reason are in `FACTORY_WORKFLOW.md`.

The endpoint is a reviewed, validated **private alpha package** and truthful owner handoff. Visual/gameplay/save-load observations remain pending until actually performed. This task does not require vanished historical saves or a new general game-automation framework. A newly created disposable fixture is authorized if real tools, safeguards, and the bounded allowance make it practical.

Preparation performed read-only source/rules research and preserved the supplied screenshots. It did not enroll a mission, modify a remote repository, start Kingmaker, install a mod, or validate a repaired binary.
