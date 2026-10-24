# Owned schema1 and area Auto draft composition — October 15, 2026

Status: COMPOSED, COMPILED, FOCUSED-TESTED, INDEPENDENTLY REVIEWED offline. No native run, candidate build, archive or purity work is part of this checkpoint. The two [inactive drafts](../planning/drafts/chunk7-owner-stop-2026-10-14/README.md) backed up at the owner's October 14 stop are now one reviewed source composition on the private branch `codex/mounted-combat-chunk7-claude-drafts` (worktree `worktrees/chunk7-claude-drafts`, base `568a175817ab28caf8600c8b4700eee24f3fbb90`, composition commit `94b53f1d3e921af897d986e09ff0b1fa3671d2e2`, gap-closure commit `f880751b1baafc9f6697d569fc06341ab0c170b9`). The patch backups remain historical custody.

## Method

Both private maps were pinned by hash (`owned-schema1-next01/integration-map01.json` `912451cf…`, `owned-area-autosave-next01/integration-map01.json` `da62f4ea…`). Every captured base matched the worktree bytes and every payload matched its pin before anything was written. The 31 target paths (schema1 20 plus one unchanged copy, area 21, 11 overlapping) were composed by `Compose-Drafts01.ps1` in the private lane `analysis-cache/chunk7-continuation/claude-draft-composition-next01`: single-draft paths copied verbatim; overlapping paths merged three-way with `git merge-file` (schema1 payload, captured base, area payload). Eight conflict hunks in five files were resolved as unions so that neither draft's cases, naming, authority keys or observers were dropped. Receipts: `composition01.json`, `composition-checkpoint01.json`.

## Corrections beyond the drafts

- `scripts/runtime/Chunk7OwnedSchema1Fixtures.ps1` arrived as two spliced copies with a broken pinned-read function; it was rebuilt as one 143-line version that keeps the stricter pinned reads (hash, length and ticks re-checked; derivation receipts constrained to the owned analysis-cache parent).
- `Chunk7PersistenceScenario.Schema1.cs` `ValidateOwnedSchema1Field` now requires every actor native to be a JSON object before the Id lookup, so an array-shaped native refuses through the guard's own exception.
- `tests/Chunk7Diagnostics/OwnedSchema1EvidenceFixtures.json`: the generated source fixture lacked `saveRequested.nativeName`, which the product emits (`Chunk7PersistenceScenario.RequestSave`); added.
- `tests/Chunk7Diagnostics/OwnedPairsAuthorityTests.cs`: the unsupported-checkpoint mutation uses a genuinely unsupported value now that `multi-area-autosave` is a supported owned checkpoint.
- `scripts/chunk7/Test-Chunk7OwnedPairsEvidence.ps1`: unselected-checkpoint expectations follow the merged refusal text and cases.
- `scripts/chunk7/Test-Chunk7OwnedPairsWiring.ps1`: the quota evaluation defines the assisted-fixture flags. This failure (89/46) also reproduces at the unchanged root HEAD and predates the composition; the harness, not the product, was stale.

## Closed draft gaps

- Area boundary: `scripts/chunk7/Test-Chunk7OwnedAreaAutosaveBoundary.ps1` extracts the six actual members (service header observer, completion DTO, descriptor projection, header/completion/release observers) into the template and runs them over the template facades: 40/0.
- Schema1 input: `scripts/chunk7/Test-Chunk7OwnedSchema1Input.ps1` exercises the metadata-only derivative, the locked CREATE_NEW provenance copy with its overwrite and foreign-owner refusals, and cold-source qualification positives and negatives over a synthetic one-owner archive in a virtual lab root: 14/0. The completed-run qualifier, lock owner and process checks are facades.
- Registration: the area evidence, area boundary and schema1 input scripts are registered in `scripts/Test-Chunk7NativeDiagnostics.ps1`; the aggregate itself was not run.

## Offline evidence

Release build passed twice (36.5 s before and 39.9 s after the guard fix). Fast tier passed in 28.7 s: components 817/0, source 147/0. Sixteen focused scripts pass: authority 173/0, DTO 92/0, source 71/0, evidence 115/0, wiring 135/0, manifest 93/0, replay epochs 81/0, replay evidence 29+97+59/0, area persistence evidence 46/0, area autosave evidence 41/0, area boundary 40/0, schema1 persistence 102/0, schema1 evidence 54/0, schema1 native guards 29/0, schema1 input 14/0, persistence evidence 29/0. Counts overlap and are not unique features. Logs: `focused01.log`, `focused02.log`, `fast01.log`, `compile01.log`, `compile02.log` in the composition lane.

Independent review (`independent-review01.json`, SHA256 `53c4b01089bc377c25784985addf79160324bae34536a0128db06d9dcbfd9c81`): PASS on merge completeness, guard integrity, request validation, custody semantics, corrections, test closure and style; zero blockers; one minor finding; six notes.

## Open

- Native evidence for every new case: the owned schema1 source Manual, its derivative cold load and schema3 resave, and the owned area-only Auto save and cold consumption. All depend on a qualified same-candidate two-owner Manual, which depends on the assisted producer chain.
- A successor candidate containing this composition; preview10 is untouched.
- The minor finding: a failure after `BeginAreaAutosave` and before native dispatch keeps `areaAutosavePending`, so the inherited fixture refuses release and the scenario reports a repeated runtime failure at the host boundary instead of a cleanup-pending artifact. Custody is retained and reported, not silently released; the reporting shape follows the existing repeated-replay semantics and can be refined with native evidence.
- The runner's request normalization and the real qualifier/lock path behind the input-test facades remain exercised only by existing owned neighbours.
