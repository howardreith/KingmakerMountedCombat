# F4 — ENGINEERING PARTIAL, ready for review (2026-11-06)

Branch codex/kmc-finalize-f4-orders-ai-transitions, from main a92b043a. Read the [handoff](docs/FINALIZE-F4-REVIEW-HANDOFF.md).

F3 closeout: the F3 engineering review was accepted at PR #18 head 5dd64825. The owner merged it as a92b043a. Owner play acceptance is separate. F3's evidence and verdicts stand as recorded in the [F3 handoff](docs/FINALIZE-F3-REVIEW-HANDOFF.md).

What F4 changes (product behaviour is identical in candidates f4a, f4b and f4c):
- **Chests and loot (WORLD-03).** A group chest or loot order given to a mounted rider is carried by the Horse, and the rider opens it once.
- **Traps (WORLD-02).** An accepted trap order's native group stop also stops the selected riders' Horses.
- **Dungeon entry (WORLD-01).** The party-selection screen (the Beneath the Stolen Lands entrance) no longer dismounts riders who stay in the party.
- **Attacks (AI-01, AI-02).** A started routine continues on the next enemy after its target dies, by the native rule. The automatic approach keeps chasing an enemy it is closing on.
- **Gait (GAIT-01).** Under party pace, an unimpaired Horse shows its run cycle; this is presentation only.

Native results:
- **r01** (f4a): the original FAIL is kept (registration). Its reader-only re-evaluation PASSes.
- **r02** (f4b): FAIL. A legacy comparison leg had no route. All F4 exploration facts were recorded.
- **r03** (f4c): FAIL. A Hold-in-bow-reach stall in the controls row; its cause is unidentified. The F4 rows PASS at row level.

Not shown natively:
- the trap order (input blocked: no admissible save beside a reachable discovered trap);
- the full hub→dungeon load with its autosave;
- the excluded-area and floor→floor controls.

Spend: finalize-f4 23.85/75 min, 3 of 3 starts. No game, lock or installed candidate remains. No merge, release, install or F5.
