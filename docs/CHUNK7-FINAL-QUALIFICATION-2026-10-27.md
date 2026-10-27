# F7 final qualification row record

Engineering coverage: PASS, 81 rows / 82 planned launches. All 83 actual attempts re-read with zero verdict or result-hash changes.
Private final-ledger.json / final7-ledger.json SHA-256: `1e3fa11460f64e92a28aa51d86bd2f003d2a446434a12a7e7b7426c32ad102c2`.

Candidate: preview.96, source `650dd8a3bb53811ecd5eaf5510bf1b797738fd12`.
Package SHA-256: `42312267c91d7923dca6b4d5b1e8ab76312cfe072c6c22026abe66f0c4ac0d84`.

Requirement definitions and approved scope are in the [matrix](../planning/CHUNK7-7E-QUALIFICATION-MATRIX.md) and [run plan](../planning/CHUNK7-7E-FINAL-RUN-PLAN.md).
The private final ledger binds each request, result, reader, fixture/source chain and restoration receipt.
Run prefix `20261030-c7e-final-` is an identifier; actual execution was October 27, 2026.

| Matrix row | Run suffix after the prefix | Native/reader result |
| --- | --- | --- |
| 1 | B2-r01 | PASS |
| 2 | B2-r02 | PASS |
| 3 | B2-r03 | PASS |
| 4 | B2-r04 | PASS |
| 5 | B2-r05 | PASS |
| 6 | B3-r06 | PASS |
| 7 | B8-r07 | PASS |
| 8 | B8-r08 | PASS |
| 9 | B8-r09 | PASS |
| 10 | B8-r10 | PASS |
| 11 | B3-r11 | PASS |
| 12 | B3-r12 | PASS |
| 13 | B3-r13 | PASS |
| 14 | B3-r14 | PASS |
| 15 | B4-r15 | PASS |
| 16 | B4-r16 | PASS |
| 17 | B4-r17 | PASS |
| 18 | B4-r18 | PASS |
| 19 | B4-r19 | PASS |
| 20 | B5-r20 | PASS |
| 21 | B5-r21 | PASS |
| 22 | B5-r22 | PASS |
| 23 | B5-r23 | PASS |
| 24 | B5-r24 | PASS |
| 25 | B6-r25 | PASS |
| 26 | B6-r26 | PASS |
| 27 | B6-r27 | PASS |
| 28 | B6-r28 | PASS |
| 29 | B6-r29 | PASS |
| 30 | B7-r30r | PASS |
| 31 | B14-r31 | PASS |
| 32 | B14-r32 | PASS |
| 33 | B11-r33 | PASS |
| 34 | B11-r34 | PASS |
| 35 | B11-r35 | PASS |
| 36 | B11-r36 | PASS |
| 37 | B11-r37 | PASS |
| 38 | B15-r38 | PASS |
| 39 | B15-r39 | PASS |
| 40 | B15-r40 | PASS |
| 41 | B15-r41 | PASS |
| 42 | B15-r42 | PASS |
| 43 | B12-r43 | PASS |
| 44 | B12-r44 | PASS |
| 45 | B12-r45 | PASS |
| 46 | B1-c2 | PASS |
| 47 | B1-c3 | PASS |
| 48 | B12-r48 | PASS |
| 49 | B12-r49 | PASS |
| 50 | B13-r50 | PASS |
| 51 | B13-r51 | PASS |
| 52 | B13-r52 | PASS |
| 53 | B13-r53 | PASS |
| 54 | B13-r54 | PASS |
| 55 | B14-u1, B14-u2 | PASS |
| 56 | B14-u3 | PASS |
| 57 | B8-r57 | PASS |
| 58 | B7-r58 | PASS |
| 59 | B7-r59 | PASS |
| 60 | B1-c1 | PASS |
| 61 | B1-c4 | PASS |
| 62 | B1-c5 | PASS |
| 63 | B7-x4s | PASS |
| 64 | B7-x4c | PASS |
| 65 | B9-p2 | PASS |
| 66 | B10-s1 | PASS |
| 67 | B9-d1 | PASS |
| 68 | B9-am | PASS |
| 69 | B9-cr | PASS |
| 70 | B9-st | PASS |
| 71 | B10-k1 | PASS |
| 72 | B10-k2 | PASS |
| 73 | B10-k3 | PASS |
| 74 | B10-k4 | PASS |
| 75 | B16-x3 | PASS |
| 76 | B16-l6 | PASS |
| 77 | B17-m1 | PASS |
| 78 | B17-m2 | PASS |
| 79 | B17-m3 | PASS |
| 80 | B17-m4 | PASS |
| 81 | B17-m5 | PASS |

Row 55 includes previous-install and upgrade steps. Row 56 is rollback; the previous-install/rollback package is the declared preview.211 control. Row 32 is the declared mod-absent control.

Row 30 uses the sole permitted retry, B7-r30r. Original B7-r30 remains FAIL: the exact predeclared Steam startup signature occurred before an atomic game result, followed by exact restoration. It is retained among all 83 actual attempts.

Rows 9 and 11 retain distinct concurrency and sequential-persistence scopes. Row 64 demonstrates retained reference identity across area transfers. Row 70 used a spare-present layout; no native spareless execution is claimed. Row 69 retains its uncovered prone half (covered by row 57); row 70 retains its uncovered defensive-cast half (covered by row 69).

The approved scope exclusions and genuine preview.211-written-mounted-save upgrade limitation remain unchanged. Owner play/visual acceptance is pending.
