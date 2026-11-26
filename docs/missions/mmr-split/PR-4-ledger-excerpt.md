# PR-4 supplemental ledger excerpt (operator records, not controller output)

Scoped, read-only excerpt of `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, the
append-only, hash-chained operator ledger the owner asked for (each entry carries the SHA-256 of the previous line in
`prev_sha256`). The lines below are copied byte for byte; each is followed by its own SHA-256. Generated 2026-11-26T02:34:21+00:00.

```json
{"branch": "codex/mmr-horse-trip", "enrollment": "kmc-codex-mmr-horse-trip-20261125213249-4b8b", "first_gate": "gate-focused-5099cd120474-d43d449b6c4-a3357fdbeb8-20261126T023359939397Z pass", "initial_push": "audited ordinary push; receipt push-audits/codex_mmr-horse-trip-5099cd12-2026-11-26T023246+0000.json", "kind": "enrollment", "max_rounds": 2, "pr": "PR-4", "pr_number": 66, "prev_sha256": "e5e75214fe978088fb099fc2d6e6e9c6721f9636ea313255ec8d6679c71404fa", "seq": 15, "target": "main", "utc": "2026-11-26T02:34:21+00:00"}
```
SHA-256 `0c7cd85902763e2647896b8ebebbc7dfe5ae82289ea7ee08aca4c76c40e20c67`

```json
{"budget": "79.45 of 150 min after the first gate (approx.); 0 unreconciled", "candidate": "the commit on codex/mmr-horse-trip whose parent is 5099cd12 and which adds docs/missions/mmr-split/PR-4-ledger-excerpt.md", "child_rounds": "0 of 2 used", "kind": "pre_review_check", "pr": "PR-4", "prev_sha256": "0c7cd85902763e2647896b8ebebbc7dfe5ae82289ea7ee08aca4c76c40e20c67", "program_rounds": "10 of 25 lifetime (3 of 18 allocated child reviews)", "round": 1, "seq": 16, "utc": "2026-11-26T02:34:21+00:00"}
```
SHA-256 `24fd98877f75f26165bcc5c63f3179058dd3368055c415c602f27908e0868354`

The owner instruction of 2026-11-26 (seq 14) sets this enrollment to 2 rounds and the validation allowance to 150 minutes; seq 16 records the
round-1 check for the candidate that adds this file.
