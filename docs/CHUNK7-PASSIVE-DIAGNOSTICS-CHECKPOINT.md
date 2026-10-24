# Chunk 7 passive diagnostics checkpoint

Status: IN PROGRESS. This source checkpoint prepares private preview8; it does not establish native qualification or breadth readiness. Continue the [sustained mission](../planning/CHUNK7-CONTINUATION-MISSION.md).

## Implemented behavior

The bootstrap observer now retains at most four passive snapshots: readiness, cleanup entry, cleanup settlement and host terminal. It records native mode, whether Dialog mode is active, controller/dialog/cue object identities, bounded blueprint identifiers, scheduling flags and the separate message-box state. Exact installed member/module contracts are checked at the read boundary. It reads no dialog text, answers or conditions and invokes no story or mode transition. Identity custody is bounded to 32 objects and released at terminal completion, including read/publication failure paths.

The existing bootstrap artifact carries the first three snapshots. An optional bootstrap-only schema-2 game-result property carries the terminal snapshot too. Historical result absence remains supported. Readers enforce exact types, bounds, phase order and complete nullable observation shapes; a destroyed Unity popup may retain a CLR identity with both popup subobservations unavailable. Partial availability is refused. These are raw observations only. The existing terminal movement-disabled, Default-mode and loaded-area requirements remain mandatory.

The Working actor-graph reader adds two non-authorizing observations. `partialInline` compares only the literal identities in the prefix which passed the existing inline validation. `lexicalAliases` reports capped same-document reference/definition counts, key names, value kinds and relative parsed positions. It exposes no raw aliases or new scalar identity values, resolves no references and never compares aliases across documents. Census exhaustion is explicitly uncertain. Existing parser limits, completed-comparison fields, unsupported-reference FAIL and every native authorization flag remain unchanged.

This follows actual03, which refused Working record 2, a sole string `$ref`, after two full records. That original incomplete comparison is preserved and does not prove a missing main actor. [Guarded diagnostic history](CHUNK7-WORKING-ACTOR-GRAPH-DIAGNOSTIC.md).

## Focused evidence

The private source maps were independently reviewed before integration. No full diagnostic aggregate or native launch formed part of this source pass. Counts overlap and are not native cases.

| Selected check | Actual result | Duration / boundary |
| --- | --- | --- |
| Complete Working Analyze feature baseline | 5 PASS / 31 FAIL, preserved | 0.6888245 s outer; missing newly requested fields |
| Complete Working Analyze final generated JSON | 36 PASS / 0 FAIL | 0.6870307 s outer; C# 7.3 / .NET Framework 4.7 |
| Existing Working structural neighbors on changed reader | 27 PASS / 0 FAIL | 0.6456354 s outer |
| Integrated Working generated archive and guard neighbors | 91 PASS / 0 FAIL | 3.3617759 s outer; 2.6007061 s checks; synthetic archives only |
| Passive recorder/actual DTO/native-read body with inert native facades | 40 PASS / 0 FAIL | Part of the finite observer selection below |
| Strict optional observation/result readers | 63 PASS / 0 FAIL | Includes unchanged final-mode/settings refusals |
| Destroyed-popup reader parity | Original 4 PASS / 1 FAIL; corrected 5 PASS / 0 FAIL | Both verdicts retained on the same actual producer fixture |
| Existing bootstrap adapter | 72 PASS / 0 FAIL | Actual methods with explicit native facade |
| Actual host terminal/custody methods | 50 PASS / 0 FAIL | Compile 0.253 s; execution 0.103 s |
| Host completion neighbor | 17 PASS / 0 FAIL | Raw log establishes count; receipt count fields are null; compile 0.206 s, execution 0.068 s |
| Private complete production compilation | PASS, 466 sources | 1.988 s; installed references; no native execution |

The finite three-stage observer invocation was actual `4bda26`, exit 0, 5.5888171 s outer / 4.8932515 s supervisor. Its stages took 3.773654 s (40+63), 0.3370169 s (popup parity) and 0.6439387 s (72). All pinned inputs remained unchanged. Initial facade compilation and serializer-resolution failures are retained in the private lane; no installed Unity binary was copied.

Private evidence is under `analysis-cache/chunk7-continuation`:

- Working final map `7c9b9847acb2ad5072a28bc62edb661338da82b036bf1e612d6769e493d6ed76`; independent review `e03a4b5c4344848276f69ea594d4c9046dc136da1c0bc4eedccc481d49ef3129`; root application `66017e456421c0133b529a82c18f0fa2c3266959d4d0cbb6a7336246bf9108af`.
- Observer final map `92b05d1c516e3f5994e57688a207f1a9ea374c1d21c3044e645dc457a4ba867c`; source reviews `882d299c68b3271e65cb3eec8cf099d876f93a2bb3fb1b5288274f9434cf3b30` and `2d96f65f413def2fed64ef25814a47121e8e6fa77240e6cae986bf40f112fae2`; root application `ad9aae9a9f62a7e4ee3fd45c1e2233f98d0a8c0685e0191366f2d9d0e82f4548`.
- Finite observer receipt `26145ecb0874cff8451612cdc25fef4b568a295cfcec941bb9f0dc546956e414`; actual caller `ffe776878a53532de1b36e531e9de00f95328add1b499245bf12e58cf66d2d76`.
- Integrated generated-archive receipt `2832793888ef57156d626c44603c602dbf86d14040206d3c86a3469851ed5a9e`; actual caller `c08fbda3f7f882cc2d70701a18a7613f6ebfdd4ecf6f0017f8476914b1d5df5e`.

## Selected continuation

Private preview8 is selected for one build and Candidate/Kingmaker verification using that artifact. The next finite native selection is the affected installed-configuration two-pair exploration control and the base-game bootstrap with passive snapshots, at most two more attempts under the 25-attempt mission budget (six used). Each needs fresh applicable guarded WhatIf proof on the changed inputs. Prior preview7 admission is not reusable. Plan 180 minutes per full audit based on its prior measured 8503.3262789 s; allow safe settlement rather than killing a transaction.

Combat and persistence runners require base-game-only mode. Installed-configuration exploration does not satisfy that gate or ordinary acquisition. The exact base-game fixture, ordinary progression, declared Small profile, six-pair stress, measured hardening and unmounted controls remain required. Any final bootstrap FAIL remains ineligible as a cold-load or Manual source. No release, permanent installation or HUMAN PLAY acceptance is selected.
