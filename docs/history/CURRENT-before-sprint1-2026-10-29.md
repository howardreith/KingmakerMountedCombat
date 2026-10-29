# Chunk 7 — engineering qualification PASS

F7 qualified preview.96 on source
`650dd8a3bb53811ecd5eaf5510bf1b797738fd12`: **81 rows / 82 planned launches PASS**,
404/0 passing subchecks. All 83 actual attempts were re-read with zero verdict or
result-hash changes. The original Steam startup FAIL is retained separately from
its sole permitted successful retry.

F11 passed the fixed GC-inclusive protocol: two-pair medians 0.2693058333 ms and
0.4088075 ms; six-pair median 0.4960833333 ms. The slow GC window is retained.
The small per-second allocation increase was investigated as unchanged bytes per
Update divided by shorter observed frame intervals.

Raw FULL remains **FAIL/exit 1**. The remaining inventory retains 242 PASS/7 FAIL,
with no skips: six accepted historical failures and a separately corrected CRLF
test. The reviewed actual-method correction passed 50/0. No raw FULL PASS is claimed.

Every native transaction settled; no game or lock remained. Mods, settings and
protected saves were restored. r02 admitted three cache rewrites and r54 three
rewrites plus one created leaf, all 12,288 bytes under the existing policy.
Those profiles were not byte-identical; all other attempts restored exactly.

The owner explicitly selected conditional merge of
[PR #5](https://github.com/howardreith/KingmakerMountedCombat/pull/5), superseding
older no-merge task instructions. Engineering evidence does not bypass repository
requirements. The live PR and private promotion readback record the actual head,
merge commit and remote main. **Owner play/visual acceptance remains TODO.**
No public release/tag or permanent installation is selected.

Exact ZIP SHA-256:
`42312267c91d7923dca6b4d5b1e8ab76312cfe072c6c22026abe66f0c4ac0d84`.
Private package and completion records:
`C:\Dev\KingmakerMountedCombatLab\handoffs\KMC_Chunk7_F7_Closeout_2026-10-27`.

The [closeout](docs/CHUNK7-ENGINEERING-CLOSEOUT-2026-10-27.md),
[81-row record](docs/CHUNK7-FINAL-QUALIFICATION-2026-10-27.md) and
[resume](AUTONOMOUS-RESUME.md) hold exact identities, review and promotion steps.
The closeout delta is documentation plus one reviewed offline test correction;
product/build/runtime execution inputs remain the qualified ones.

The genuine preview.211-written-mounted-save upgrade remains untested by the
accepted scope decision. F1–F6, b215–b219, earlier packages and frozen worktrees
are retained. No earlier candidate's native PASS was transferred into F7.

[Prior CURRENT chronology](docs/history/CURRENT-before-codex-takeover-2026-10-26.md).
