# Chunk 7A source checkpoint — PASS, October 12, 2026

Implemented the selected production ownership/routing and versioned persistence
foundation on `codex/mounted-combat-chunk7-foundation`, based on refreshed main
`96c7fa1919f371ebd1100aab66f40b5f471644bb`. Source, focused tests and durable brief:
`21df88347b98532fd59b82d3ae05078f7bac64e9`. This handoff is a separate documentation
commit; its exact HEAD and remote result are recorded in the publication receipt
below. Existing dirty/frozen worktrees were preserved.

The existing one-pair production path now reserves/indexes both actors through a
session-aware owner before attachment, retains exact owner/generation bindings
for native shell/context and ordinary command delivery, routes actor lifecycle
callbacks, and retains detached participation and unfinished cleanup. Readiness
aggregates retained owners. One shared turn/allocation authority remains; no
native action spending, preparation, refund or world replay is added.

Native admission remains limited to one pair (including retained custody before
another pair can reserve). Native writes remain schema 2; schema-1/schema-2
restoration stays explicit. Schema 3 is a tested in-memory codec/migration
foundation with bounded multiple records and one combat snapshot. A successful
new-codec decode never authorizes native restore; unsupported/invalid original
payload preservation and unreadable-original write refusal remain intact.
See the [durable brief and ownership/migration map](planning/CHUNK7-DEVELOPMENT-BRIEF.md).

Focused command, using existing installed Kingmaker/Harmony12 references:

```powershell
.\scripts\Test-Tiers.ps1 -Tier Fast -Focused 'Test-PersistenceData.ps1,Test-ChargeOwnershipContracts.ps1,Test-MidEncounterNativePreparation.ps1'
```

| Final tracked-source pass | Result |
| --- | --- |
| Production compilation (.NET Framework 4.7 / C# 7.3) | PASS, 4.1 s |
| Components | 727 PASS / 0 FAIL, 0.9 s |
| Original source contracts | 147 PASS / 0 FAIL, 3.1 s |
| Historical / Chunk 7 persistence | 56 / 43 PASS, 0 FAIL, 0.8 s |
| Compiled ownership/Charge adapter | 121 PASS / 0 FAIL, 1.1 s |
| Native preparation contracts | 16 PASS / 0 FAIL, 0.3 s |
| Fast orchestration | 6 PASS / 0 FAIL; 10.56 s wall clock |

The component seam uses an external movement/view lease substitute. The compiled
adapter probes use detached native actors/containers and substituted external
engine/logging boundaries. No two-pair Unity gameplay is claimed. Source guards
were unchanged. The final pass followed the last capacity-routing correction
and staging of every new source file.

Private evidence root: `C:\Dev\KingmakerMountedCombatLab\analysis-cache\chunk7-foundation`.
Final command/reader results: `fast-06.json` and `fast-06.log`; source/test/script
Git-tree identities: `source-checkpoint.json`; DLL: `dll-identity.json`.
Earlier receipts remain: `fast-01` (5.27 s, external test cleanup substitute
failed to throw; 726/1 components), `fast-02` (6.78 s) and `focused-03`
(3.23 s, 144/3 original source guards), then `fast-04` (9.92 s, PASS) and
`fast-05` (8.68 s, PASS). Production delivery retained the explicit guards and
bound their service references to captured owners; no assertion was weakened.
The six measured invocations total 44.43 s, below the selected validation budget.

Development DLL `bin/Release/KingmakerMountedCombat.dll`: SHA-256
`ee2c898f4b95e75e29df47d4b6602ebd3bde10dbdc09931164110c2f0621491a`, MVID
`57afab37-560c-48c3-a8cf-255d04c89bcb`. This is a changed development binary;
preview.211 native qualification belongs only to its frozen historical bytes.
No runtime observer/fixture source changed, no candidate/package was created, and no
native game, save write, staging, installation, purity/Full campaign, PR, merge
or release action ran.

Publication destination: verified origin
`https://github.com/howardreith/KingmakerMountedCombat.git`, same review branch.
No repository-owned push helper exists in this checkout; publication uses the
current AGENTS fallback: audited text-only payload, clean tree, exact origin/ref,
ancestry/fast-forward checks, dry run and ordinary non-force push. The final
attempt, exact documentation HEAD, remote equality and worktree status are bound
in `publication-checkpoint.json` in the private evidence root.

Next native dependency: integrate all-pair capture/load, area transfer, lifecycle,
disable/removal, worker fencing and native participation under the existing
single world authority before enabling a second native pair or schema-3 native
writes/restoration. Then select two-pair exploration/RT/TB, both initiative
arrangements, separate expenditure, area and save/cold-load proof. That is a
later selected tranche. This checkpoint is not full Chunk 7 completion. Chunk 6
owner/HUMAN PLAY acceptance remains pending; its historical evidence is below.

# Current state — final Chunk 6 qualification, October 11, 2026

The owner-selected campaign, persistence matrix, both completion gates and final
FULL passed on the unchanged released preview.211. Owner acceptance remains
pending. Read [the engineering closeout](docs/CHUNK6-ENGINEERING-CLOSEOUT-2026-10-11.md)
for exact identities, receipts, retained failures, restoration and the owner checklist.

Campaign: 109/109 qualified PASS. Chunk 5: all 105 mandatory behaviors PASS,
record checks 106/106, one nonmandatory NOTRUN. Chunk 6A: all 87 mandatory behaviors
PASS, record checks 87/87; 40 historical failures retained. Original A stage 25
and B stage 2 remain failed evidence in their closures; fresh successors passed.

Product source remains a0d70fa41f25498aa2f31dfdfebc1ad29750f4a6; execution harness
and acceptance-reader revision is 7e49e0348ee944108af4f8ccec5dda899097f1c1.
The review branch codex/mounted-combat-chunk6-final-qualification includes the
October 11 workflow reset. Documentation provenance is separate from both.
Completed preview.211 ledgers are active; named preview.105, .205 and .209
ledgers preserve historical evidence. No product or release asset was replaced.

Supported: RT/TB legal combat Mount/Dismount, RT pair-owned Charge, ordinary
mounted casting/items and staged actions on one native activation. TB Charge
delivery remains DEFER — EVIDENCED. The defensive Mounted Combat feat remains
NOT IMPLEMENTED, with bounded feasibility complete and the feat deferred.
HUMAN PLAY, visual/HUD and physical-input acceptance remain outstanding.

All actual campaign/matrix transactions recorded restoration. Final native work
left no game or active transaction, and the human preview.105 installation was
preserved. These are transaction receipts plus bounded state checks, not a new
whole-installation inventory. Always snapshot actual current intake for future work.

The default remains [focused development](docs/VALIDATION-POLICY.md). This explicitly
selected final qualification is finished; do not restart campaigns without a new
assigned need. No further main merge, PR, tag, release or installation is authorized.

# Historical workflow-reset checkpoint — October 11, 2026

The active workflow default is docs/VALIDATION-POLICY.md: focused development,
review before broad qualification, immutable candidates and preserved live safety.
This workflow repair does not resume the final acceptance campaign.

Released preview.211 (v0.1.0-chunk6-preview.211): product source
a0d70fa41f25498aa2f31dfdfebc1ad29750f4a6; DLL
11e993093d2042f615cfc9d6aecc33d630d2e41e8125dd680656b6a1057a48ea;
MVID 0aa68ba0-f293-480e-8ecb-d7bdaeca73d0; package
e89f431ae1303b217e987b2ff12171a05bac434d97809962c3596e2942874039.
Its campaign observed 24 PASS stages, stopped by the owner at stage 25 of 109.
No complete final acceptance ledger describes that binary; the 105-row Chunk 5
matrix and complete campaign/ledgers remain pending. Earlier preview.209 ledgers
and preview.205 accepted ledger remain evidence of their own inputs only.

Supported scope is recorded in docs/CHUNK6-CLOSEOUT-SUMMARY.md: combat
Mount/Dismount, real-time pair-owned Charge, ordinary mounted casting/item use,
and staged actions. Turn-based Charge is DEFER - EVIDENCED; the defensive Mounted
Combat feat is deferred/unimplemented. HUMAN PLAY acceptance is separate and
pending. Verify actual current human installation as every transaction's intake.

See docs/WORKFLOW-RESET-HANDOFF.md for this review branch and focused verification.
Full prior chronology is retained in docs/history/CURRENT-before-workflow-reset-2026-10-11.md.
