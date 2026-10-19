# Chunk 7 Directive 4 Codex checkpoint

## October 18 — preview.42 batch 13 settled; preview.43 source IN PROGRESS

Published source `b9631449357b2ef191256ded54d21c975c12cd64` on the successor branch.
One preview.42 package build was reused for Candidate **PASS 6/6** in
221.2420399 seconds. Package SHA-256
`286cc78595f59393739c917c86a02db7a3d1d3d9a9331531bac361a6c5c7bc95`,
DLL SHA-256 `11e36f03fd8d58763516ca7291508c4bd367ad1ea47e8f12c67a2904b5de288e`,
MVID `bfb6ec50-babc-4f92-9d2b-c6a4dd070b5c`. Exact private receipts are in
`analysis-cache/chunk7-continuation/owner-seed-batch13/`.

Batch 13 is **FAIL**: three actual runs, one PASS and two preserved FAILs;
slots 3/4 were DEFER — EVIDENCED and never launched.

- `20261018-c7b-world42-ownerseed-b13s1-worker`: FAIL, 90.8167054 s, game PASS.
  The restored-start driver left all six field actors selected. The strict
  selected-A native-save evidence contract rejected it. Add normal Select(A)
  after world/authority admission, with a regression; preserve this FAIL.
- `20261018-c7b-world42-ownerseed-b13s2-barrier`: FAIL, 90.4350347 s, game PASS.
  Normal B Dismount retired B and its live slot bindings, but four recognized
  hotbar templates remained. Aggregate facts 6→4 cannot independently exclude
  an active B fact masked by an absent A fact. Independent review requires a
  per-actor/per-kind native fact inventory and fresh binary/native retry.
- `20261018-c7b-world42-ownerseed-b13s5-inventory`: PASS, 84.6206638 s,
  evidence SHA `5a339ee104ccf2aa8062501d53b760816e1b9af59681121bfc090e10683966a3`.
  Native ToIndoor/ToOutdoor both resolve to the current SpiderCave area, not a
  second area. MapExit resolves GlobalMapArea; all three use BeforeExit.
  This was read-only discovery, with zero travel/interaction/save requests.

All actual runs have five restoration flags true, all three transaction phases
restored, profile bytes restored exactly, and no remaining game or lock. Failed
outputs are not accepted cold sources. Native barriers/worker/migration are still
unaccepted. The original preview.41 TB and zero save/cold PASS results retain
their original inputs.

Preview.43 source checks are now PASS on the affected inputs: C# authority431,
worker adapter82, preparation/facts39, current-parent guards56, schema1/schema2
guards29/26, actual fixture bind-mode48, source147; PS owned reader680,
source247, boundary218, schema1 reader54 and actual derivative input14, all
with zero failures in their final runs. Affected product compile03 PASS in
26.6666074 s. Exact commands, durations, hashes and retained failures are in
`codex-takeover/group43/`; C# summary03 SHA
`ef98c536fda77d7c2ad300af4884540938adb6d7be1df249b3bb1239ef1417ee`.

Independent review found and closed the actual schema1-current fixture-mode
guard omission, external result-row registration and source-schema evidence
gaps. A generated in-memory projection of failed worker evidence also exposed
compact owner IDs versus native hyphenated actor IDs; the reader now uses the
existing strict native identity comparator. The original worker FAIL stays
unchanged, and its synthetic projection has no native credit. Preparation may
retain four templates; completed save/cold boundaries still require exactly
their saved live slots. The final review receipt is
`owner-seed-review/d4-group43-review01.json` and is required before qualification.

Private batches14/15 declare worker/barrier retries and both migration chains
(historical input, native schema3 resave, separate third-process actual schema3
load). Existing positive P07 cleanup is not runnable unchanged: its diagnostic
driver still reads the legacy schema1/2 format after the current schema3 writer.
Those two removal slots are unrun DEFER - EVIDENCED pending a bounded current
observer/reader connection; no Working incompatibility is inferred. Area route
preparation must respect actual GlobalMap cleanup policy. No native run or
Candidate packaging is claimed by these source checks.

Next coherent group (preview.43) is IN PROGRESS: worker selection setup, exact
control-fact evidence, schema1 source/historical migration/native resave and
both schema1/schema2 third-process loads of actual schema3 native output. Root
is sole native executor. Review, focused checks, one clean candidate build,
Candidate and scoped WhatIf precede fresh bounded native batches. Area transfer,
repeated lifecycle, registered disable/permanent Horse refusal, positive cleanup
and genuine no-DLL load, native malformed/future refusal and opportunistic doors
remain required. No merge/release/permanent installation/full7E/HUMAN PLAY.
