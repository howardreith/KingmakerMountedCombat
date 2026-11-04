# Sprint 6: the Mounted Combat feat (2026-11-03)

**SPRINT 6 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

Branch `codex/mounted-combat-sprint6-defensive-feat` from `main` `5cebdcef`. This corrects the Sprint 5 record: PR #10 is merged, and its "no merge" line predates that. Final compiled candidate sprint6i (`95b2945a`; DLL `24167ae8…`, MVID `c4fd7d09…`). Later commits change docs only. Budget `sprint6`: 74.43 of 90 minutes. The four accepted runs stay on sprint6g (`83e56374`, DLL `91a9b6df…`); the final correction's own run is on sprint6i.

- **FEAT-01:** a selectable combat feat through the ordinary and combat-bonus selections, requiring one trained Mobility rank and no companion.
- When a rider's mount is hit: one native Mobility check, strictly greater than the attack result, before any consumer sees the hit, giving the native negated result.
- Once per round and the Swift opportunity are separate. Own turn spends that turn's Swift; off-turn borrows the next one, charged once at the next native preparation. Real time uses the native Swift cooldown.
- State is ordinary rider buffs: no archive change, and it survives a cold load without refund.

- A hit a cutscene's `ForceAlwaysHit` will force afterward is refused before anything is spent. Ordinary rolled projectiles stay eligible.

**Stated for review:** a turn paying a debt cannot natively Delay, and shows no move cursor icon, though it moves.

Record: [docs/SPRINT6-MOUNTED-COMBAT-FEAT.md](docs/SPRINT6-MOUNTED-COMBAT-FEAT.md). Prior state: [CURRENT before Sprint 6](docs/history/CURRENT-before-sprint6-2026-11-03.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves restored; no transaction lock or game process; no candidate installed. Owner play acceptance of Sprints 1 to 6 and the Sprint 3 trap check remain separate.
