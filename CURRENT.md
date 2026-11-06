# F4 — ENGINEERING PARTIAL after the completion mission, ready for final review (2026-11-06)

Branch codex/kmc-finalize-f4-orders-ai-transitions, from main a92b043a. Read the [handoff](docs/FINALIZE-F4-REVIEW-HANDOFF.md).

F3 closeout: the F3 engineering review was accepted at PR #18 head 5dd64825. The owner merged it as a92b043a. Owner play acceptance is separate. F3's evidence and verdicts stand as recorded in the [F3 handoff](docs/FINALIZE-F3-REVIEW-HANDOFF.md).

What F4 changes:
- **Chests and loot (WORLD-03).** A group chest or loot order given to a mounted rider is carried by the Horse, and the rider opens it once.
- **Traps (WORLD-02).** An accepted trap order's native group stop also stops the selected riders' Horses.
- **Dungeon entry (WORLD-01).** The party-selection screen (the Beneath the Stolen Lands entrance) no longer dismounts riders who stay in the party.
- **Attacks (AI-01, AI-02).** A started routine continues on the next enemy after its target dies, by the native rule. The automatic approach keeps chasing an enemy it is closing on.
- **Gait (GAIT-01).** Under party pace, an unimpaired Horse shows its run cycle; this is presentation only.

Product behaviour is identical in candidates f4a, f4b, f4c and f4d.

Completion mission (3 more starts on the same record, 6 of 6 used, 47.88 of 75 min):
- **r04** (f4d): FAIL, kept. The r03 Hold cause is resolved natively: under native Hold the pair dispatched without moving, with the rider's Hold alone. A native floor hostile kept the pair in combat, so the melee and hold regression rows did not start.
- **r05** (f4d): **PASS**. The chest order was given during group travel and carried. The party-pace trot, the solo control and the native Slowed control (Slow gait) are inspected in ordered motion frames.
- **r06** (f4d): FAIL, kept. The real hub→dungeon entrance ran through the native `Go`, and the one authorized after-entry Auto was written on the floor with the same pair. Then a scenario defect stopped the run before its in-game checks: the post-Go selector read hit the destroyed UI. It is corrected in c1c56439 and not re-run. The reload was not run.
- **Trap order:** INPUT BLOCKED. It needs an owner Manual save beside a revealed, reachable trap.

No game, lock or installed candidate remains. No merge, release, install or F5.
