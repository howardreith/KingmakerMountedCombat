# PR-2 formal-submission checklist (PR #70 review R1-F11)

Owner instruction 2026-11-26f section 6 asks that the pre-review accounting omission not consume another formal round. Before each
`review` call on enrollment `kmc-codex-mmr-tb-approach-20261126213805-50cc`, the developer completes every item below, in order.
The PR-2 mission file is not edited: the controller pins its snapshot.

1. Every blocking finding of the pending review has a disposition with evidence in the developer report. Each evidence item names
   its kind: production code; harness through production members, with the simulated engine boundary stated; component test; or
   installed-contract check. Offline evidence is never described as gameplay.
2. A `pre_review_check` entry is appended to the operator ledger
   (`analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, hash-chained). It records:
   - the candidate identity: the records commit that regenerates the excerpt below;
   - the applicable owner authority;
   - PR-2's usage against its allowance;
   - the recorded results;
   - executed reviews, child and lifetime, plus infrastructure reviews where an infrastructure task is open;
   - a freshly read budget line.
3. `docs/missions/mmr-split/PR-2-ledger-excerpt.md` is regenerated after that entry, so the excerpt ends with it, and committed in the
   candidate. Ledger lines are copied byte for byte, each with its SHA-256.
4. The candidate is pushed through the controller's `push`, and the required focused gate (`verify --gate focused`) passes on that
   exact commit.
5. The report's validation entry cites the gate receipt and lists every other metered run since the previous submission.

A late or corrected record is appended as a new entry that names the entry it corrects. Earlier entries are never rewritten.
