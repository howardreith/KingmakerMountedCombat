# Chunk 7 engineering closeout

Engineering qualification: **PASS** on F7, preview.96. The selected endpoint is
**CHUNK 7 ENGINEERING COMPLETE — PR #5 MERGED — OWNER PLAY/VISUAL ACCEPTANCE PENDING**.
Promotion is governed by the owner's conditional merge authority and the actual
[PR #5 state](https://github.com/howardreith/KingmakerMountedCombat/pull/5).
Owner play and visual acceptance remain **TODO**. No release/tag or permanent
installation is selected.

## Exact delivered candidate

| Identity | Value |
| --- | --- |
| Qualified source | `650dd8a3bb53811ecd5eaf5510bf1b797738fd12` |
| Qualified tree | `b3ceb13f19d32608583e76309ec98798172e89a7` |
| Product source subtree | `c2a1fb4ce3e889b66c34648cb95b1390d3fdea30` |
| Qualified scripts subtree | `d017e7dad91c978001de61661a9d883ad1aa1fc6` |
| Tools subtree | `24ea33e0f9997f8b763912b5235c75ba0e660302` |
| Tests subtree | `e14f83d743b0630f0bf3ab0ab51ba90212ee75e4` |
| Version | `0.1.0-chunk7b-preview.96` |
| DLL SHA-256 | `9d090645a570e27777dc6fca92ee2e35a7b31e8696b580212923511933134a07` |
| DLL MVID | `54e9d969-4052-4bd4-b4d4-ec81adeca856` |
| ZIP SHA-256 | `42312267c91d7923dca6b4d5b1e8ab76312cfe072c6c22026abe66f0c4ac0d84` |
| Manifest SHA-256 | `7638fea2e10010b37e5d1374c79ecf279ef3e8697425ce626dd36c89503d78a9` |

Private delivery directory:
`C:\Dev\KingmakerMountedCombatLab\handoffs\KMC_Chunk7_F7_Closeout_2026-10-27`.

Package:
`KingmakerMountedCombat-0.1.0-chunk7b-preview.96-c7e-b219-pilots-diagnostic.zip`.
The original remains in `C:\Dev\KingmakerMountedCombatLab\artifacts`.
Both copies have the ZIP hash above. The package contains only
`KingmakerMountedCombat/Info.json` (340 bytes) and
`KingmakerMountedCombat/KingmakerMountedCombat.dll` (8,197,632 bytes).
No game assemblies, saves, proprietary assets, raw logs or observer payload are
included. The filename preserves build provenance; the exact build was reused.

Frozen executor:
`C:\Dev\KingmakerMountedCombatLab\worktrees\chunk7-b219-pilot-650dd8a3`,
branch `codex/mounted-combat-chunk7-b219-pilot-650dd8a3`, clean at the qualified
source after all native runs. Review branch:
`codex/mounted-combat-chunk7-successor`. The exact installed Kingmaker/Harmony12
contracts, .NET Framework 4.7 and C# 7.3 remain the target. Local reference
configuration SHA-256:
`6e9989b60b5c8e7f59fc15656bdfb6ab5dceb9dc0c894f19da1f0549e82fc413`.

Observer.6 is separate lab tooling. ZIP SHA-256
`bb78d97f33054df5027e50d0976f2b886ea5a7ff31fd28b7fad7678acfc76885`;
DLL SHA-256 `be461deab283702e35279c5c9c5c511e2a827a4e7f24125b4ee4c405651cfeef`;
MVID `de961e8e-0cb4-4ae8-ae7b-bf1c0fa8aece`.
The retained preview.211-b control ZIP remains
`e89f431ae1303b217e987b2ff12171a05bac434d97809962c3596e2942874039`.

## Qualification and evidence

| Gate | Actual disposition |
| --- | --- |
| Source/package/observer validation | PASS: 149/0, 11/0 and 7/0 |
| Candidate tier | PASS 6/0, inner 751/0, exit 0, 3.5 minutes; unchanged build reused |
| Corrected compiled r09 pilot | PASS 11/0, exact restoration; bounded branch limitation below |
| Dispatch | PASS 181/0 |
| Final native matrix | PASS: 81 rows, 82 planned launches, 404/0 passing subchecks |
| Actual launches | 83: 82 PASS, one retained signature-matched startup FAIL |
| Final all-attempt re-read | Exit 0; 83 reads, 82 PASS/1 retained FAIL, zero verdict or result-hash changes |
| Consumer bindings | PASS: row 42, rows 55–56, row 32 cleanup lineage |
| F11 performance | PASS under fixed protocol, including explicit allocation-rate investigation |
| Raw FULL | FAIL, exit 1; 560.5971811 seconds; historical SupportingEvidence signature |
| Remaining offline inventory | Raw 242 PASS/7 FAIL/0 skipped, 249 checks, 2,955.3 summed step seconds |
| Applicable current offline gate | PASS with separately reviewed test correction; raw failures retained |
| Owner play/visual acceptance | TODO; checklist remains unchecked |

The [81-row record](CHUNK7-FINAL-QUALIFICATION-2026-10-27.md) maps every selected
row to its actual run. The [matrix](../planning/CHUNK7-7E-QUALIFICATION-MATRIX.md)
and [run plan](../planning/CHUNK7-7E-FINAL-RUN-PLAN.md) retain requirement definitions
and approved exclusions. All native execution occurred October 27, 2026.
`20261030-c7e-final-B<n>-<tag>` is a unique run identifier, not the execution date.

Private receipts live under
`C:\Dev\KingmakerMountedCombatLab\analysis-cache\chunk7-continuation`.
`final7-ledger.json` binds all rows, requests, results, reader/binder verdicts,
scope limits, restoration and original attempts. Its SHA-256 is
`1e3fa11460f64e92a28aa51d86bd2f003d2a446434a12a7e7b7426c32ad102c2`.
The final re-read `final7-section7.json` has SHA-256
`a77f6c102f483ec96db4cdea09f8a05c74fdf881f80228a08e87f958d9dc08c9`
and completed in 339.159 seconds with exit 0.

Suite `chunk7e-final-650dd8a3-20261027` has SHA-256
`185bd51d921141989cbcdf02d86aa990926704b9ac79b767f7f7939d390fd2ab`.
Its protected-save snapshot digest is
`e4811c3a89deca122b997115540ddc364aa8a54fc85718ea59133dfe81094963`.
Every required immediate producer/consumer chain belongs to F7. The 19 historical
fixture links retain the declared Manticore, Small, Medium and sixth-Horse input
provenance; none transfers an older candidate's behavioral PASS. The only false
same-candidate flags are the explicitly admitted r28 and s1 fixture inputs.
Their descendants use the required current producer/consumer chains.

The all-attempt ledger preserves original B7-r30 FAIL, before an atomic game
result, with the exact predeclared `steam_api64` startup signature and verified
restoration. Its sole permitted identical-selection retry B7-r30r passed 8/0.
No other F7 native retry occurred. F1–F6, b215–b219, failed inputs and frozen
worktrees remain retained. F6 remains partial FAIL: 52 actual launches,
49 PASS/3 FAIL, 32 unlaunched slots and no F11 result.

### Corrective coverage and limits

- r11/r12 demonstrate sequential native expenditures by distinct pair actors,
  debts 4.10454273 and 3.92431736 seconds at serialization, and separate-process
  cold continuation. Simultaneous inputs are covered separately by row 9:
  both inputs in frame 4297, two native attacks under distinct resource owners.
- r27 demonstrates new Horse reference serials across three loads; row 64/x4c
  retains the same serials across three area transfers. Both preserve input
  bytes, make zero writes, report zero registry faults and release held references.
- r07/r08 execute both searched movement setups and two measured paired steps
  each, within unchanged distance/action bounds.
- Row 70/st completes all ten stages using geometry v3. The measured
  20.5900116 m target distance lies inside the unchanged
  19.839999747–21.339999747 m band. Native approach stops at 16.8873634 m for a
  16.84 m radius with the original 0.1 m allowance. A spare was present; native
  spareless execution is not claimed. Actual-method regressions cover that branch.
- Corrected r09 callers ran in the complete pilot and final case, but unchanged
  reaction counts short-circuited the new renewal branch. Positive branch evidence
  is the retained F6 native event, exact-assembly inspection, compiled-source
  regressions and immutable boundary replay. No native carry-event execution on
  preview.96 is claimed; the original F6 FAIL stays FAIL.
- Prone stand-up, defensive casting, prepared mounted casting, BAB6 attack modes
  and pending-only schema-3 continuation all have their required final rows.
  Row 69's unexercised prone half is covered by row 57; row 70's unexercised
  defensive-cast half is covered by row 69.

Focused corrective-evidence review PASS:
`takeover-final7-corrective-evidence-review.md`,
SHA-256 `2a07a0b942845ad8971d78c540b9281ee51c447fb79791162b3ff5bda3441cd0`.
Prior source/delta reviews remain applicable.

### Fixed performance result

| Measurement | GC-inclusive median of window means | Fixed limit |
| --- | ---: | ---: |
| r40, two pairs | 0.2693058333 ms | 0.5 ms |
| r39, two pairs | 0.4088075 ms | 0.5 ms |
| r39, six pairs | 0.4960833333 ms | 1.0 ms |

All 15 r39 and 12 r40 timing windows are retained. The zero-pair r39 GC window
had mean 1.2418808333 ms and maximum 112.6477 ms; the first six-pair window also
included GC (mean 0.5093066667 ms, maximum 1.2109 ms). These observations were
not removed or retried. Population and zero-pair return checks passed, quiet KMC
messages/errors were zero, and per-pair increments decreased in both timing series.
Whole-frame P95 remains context, not a substituted acceptance metric.

Allocation bytes per Update were constant in every sample: 25,338 / 26,538 /
27,958 for 0 / 1 / 2 pairs. The post-zero per-second rate increased 0.0212860663%:
the same 3,040,560 bytes over 120 updates were divided by elapsed times
1.3213323001, 1.3213317059 and 1.3210511003 seconds. This trend is **investigated**,
not described as absent. It reflects the observed cadence denominator; no
per-Update allocation growth was observed. No limit, window or benchmark changed.

`final7-f11.json` SHA-256:
`3e0f7aa688ff8aea3029d7c7bf597b7788b1ff5d28fe577a87e74dfcb34eff13`.
The explicit investigation is
`takeover-final7-f11-allocation-investigation.md`,
SHA-256 `2ae099a2da5084d6eb93ac951b520963e3a22a66a23ec7c37da6e55dd83a2f88`.
Independent F11/consumer/cache review PASS:
`0e040be828bb9449fd86f0874a79c9bdd53a6cdc38261b6099fa2d4210d6ed4c`.

Timing includes declared observer overhead. Whole-frame time includes native
game/rendering. Heap occupancy is not allocation or leak proof. The native
allocation probe observes managed allocation on the KMC Update OS thread;
other threads, native allocations and hooks outside Update are excluded.

### Offline outcomes and report corrections

Raw FULL stopped at the accepted historical
`Supporting artifact lacks exact PASS row: CM01-combat-mount-preparing-admitted`
expectation. Its successful F7 prefix was reused. The five historical ledger
failures remain FAIL with their original reader-revision mismatch and unresolved
cause, as the owner directed. Neither raw FULL nor the raw inventory is called PASS.

Inventory step 74 additionally failed because its factory-extraction regex
accepted LF but not the frozen checkout's CRLF. The reviewed one-line offline
test correction accepts the line ending without changing uniqueness, method
selection or assertions. It passed 50/0 against the exact frozen source, exit 0
in 1.117 seconds. Upgrade rehearsal separately passed 242/0 on F7.
The full offline disposition, including all 249 log bindings, is
`final7-offline-disposition.json`, SHA-256
`0292ad6848a4c6f333cb39d981a81f4cb6897bcbfcc694ad75bbe58eccc1255e`.
CRLF correction review:
`0c6cbef166fbf39bc0c774f7bfe2a2778ad583943c297c52c708304f7247a9c4`.

The original consumer-binding helper wrote all PASS results, then threw while
printing an absent optional error key. That exception and original output remain
retained. A separate reporting helper changed only optional-key guards and output
path, then exited 0 with equivalent bindings. Corrected output SHA-256:
`00577554ab9c34d3901350b960aa0c4d18aabf50882128c5b0e17299c05581dd`.
No runtime assertion or input changed.

Two metadata errata preserve their originals: the freeze's stale F6 kind label
is identified as F7 from actual source/package/suite/request identities, and the
r02 cache summary's array count of three is corrected to its raw per-file lengths.
The freeze's branch names the published source branch; requests name the frozen
execution branch. Neither erratum changes execution or package bytes.

## Restoration and supported limits

All 83 attempts settled. Mods, settings, protected human saves, immutable baseline,
working-fixture restoration and save-write allowlists were verified. Protected
save restoration digest:
`60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1`.
No game or transaction lock remained after the final native run.

Two runs used the existing `native-achievement-cache-settled-size` policy:
r02 admitted three rewrites; r54 admitted three rewrites and one created leaf,
each 12,288 bytes. Those two profiles were **not byte-identical**. All other
attempts restored exactly. The raw cache records and errata are bound by the ledger.

The accepted upgrade limitation remains: no mounted save actually written by
preview.211 underwent an automated upgrade. Schema migration derivatives and the
27/0 → 42/0 → 27/0 installation/cache rehearsal do not prove that path.
Rollback uses a pre-upgrade save. Removal passed on the stock-Mammoth fixture and
its exact cleanup archive with KMC absent; a campaign containing a KMC Horse still
requires the mod.

Owner-approved exclusions remain unchanged: Haste variation, six-pair combat,
main-menu CharGen, stale level-up Auto families, the legacy Manual 302→303 route,
retired Small calibration routes, unselected extra-mod combinations and the
declared out-of-scope feat/class content. Turn-based Charge remains a cost-free
refusal; optional real-time melee Charge remains off by default. Only the exact
supported Small profile is admitted. Human play/visual acceptance is separate.

## Publication and owner handoff

The closeout head differs from qualified source only in documentation and the
reviewed offline CRLF test. Product, build inputs, runtime readers and native
execution semantics remain unchanged. The private final-head mapping and promotion
receipts record the exact reviewed/published head, actual merge commit, remote
main, product-tree comparison and unchanged package hash. A documentation or merge
commit hash does not require rebuilding identical product bytes.

The owner explicitly authorized the gated merge of PR #5, superseding older
no-merge task instructions without changing historical records. Promotion must
use the exact expected head, satisfy actual repository rules, and preserve the
review branch, all frozen worktrees, prior packages and evidence.

See the [player guide](CHUNK7-PLAYER-GUIDE.md), [changelog](CHUNK7-CHANGELOG.md),
[support guide](CHUNK7-SUPPORT-CONFIG.md),
[install/migration/rollback/removal guide](CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md)
and [unchecked owner play/visual checklist](CHUNK7-OWNER-PLAY-CHECKLIST.md).
The private completion directory carries these documents and final verification
receipts. Public release/tag creation and permanent installation remain unselected.
