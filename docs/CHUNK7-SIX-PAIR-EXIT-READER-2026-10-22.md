# Chunk 7 — the unattended six-pair exit's FAIL through the full game-result reader

Status: **offline PASS; closes SHOULD-FIX 3** from the batch188 preparation review. No native
exit was exercised. The standing completion mission remains IN PROGRESS.

## Why

The unattended six-pair exit (`RuntimeAutomationHost.SixPairFailure.cs`) publishes a terminal
FAIL result and asks Unity to quit when a disposable six-pair run's cleanup cannot settle.
`Test-Chunk7SixPairFailureExit.ps1` runs that method's actual C#, but against a facade result
type. Nobody had shown that an exit-shaped evidence root is accepted by the complete
`Test-RuntimeGameResult.ps1`, the reader the runner applies before its base-game manifest check.

## The check

`scripts/chunk7/Test-Chunk7SixPairExitReader.ps1` covers all three exit lineages. Each starts
from real native PASS evidence, pinned by SHA-256 and only read:

| Lineage | Real source run |
| --- | --- |
| Zero-write six-pair stress | `20261022-c7b-world190-b190s1-sixmeasure` |
| Six-mounted Manual producer | `20261022-c7b-world189-b189s1-sixsave` |
| Separate-process cold consumer | `20261022-c7b-world189-b189s2-sixcold` |

It rebuilds the evidence root exactly as the exit path leaves it:

- **Engine `RetainCleanup`:** a retained `chunk7-world-cleanup-pending.json` or
  `chunk7-persistence-cleanup-pending.json` in the real `WriteArtifact` envelope, with status
  FAIL, the engine error and the boundary fields. There is no completed engine evidence.
- **Host `PublishRuntimeArtifactManifest`:** the loader inventory first, then the receipt.
- **Host `CreateRetainedSixPairFailure` over `CreateBootstrapFailureV2`:** status FAIL, the
  engine rows with the cleanup row failed, errors plus the exit reason, totals and the
  manifest hash.

Before anything is built, the test pins every product statement it mirrors:
- each exit `result.*` assignment;
- the bootstrap identity and platform initializers;
- the exact field sets of both;
- both `RetainCleanup` writers;
- the manifest publisher's order and kinds;
- the artifact envelope.

The exit reason is read from the source. A product change to any of these fails the test
instead of letting the fixture drift.

It then runs the complete unchanged reader and the runner's base-game check (manifest hash and
same-process loader inventory) inside one run-scoped
`runtime-evidence/harness-test-sixexit-<guid>` root. This is the harness's own pattern; the
root is removed afterwards under a prefix and exact-leaf guard.

## Result

25/0 in about 15 s, with no leftover fixture root.

| Group | Checks |
| --- | --- |
| Source pins and field sets | 3 |
| Honest exits: stress, producer, cold, an earlier causal failure, the unavailable fallback, retained mounted custody | 6 |
| Refusals, each with the reader's own expected message | 16 |

The refusals cover:
- `-RequirePass`;
- receipts promoted to PASS, labelled PASS over errors, without errors, without host-boundary
  custody, or from a foreign family;
- results without errors, with all rows passing, labelled PASS, with mismatched totals, with
  unknown or duplicated rows, or from another process;
- a manifest republished after the result;
- a receipt changed after its manifest.

The independent review was CLEAR. Its two SHOULD-FIX items (complete exit and bootstrap pins)
and three notes (honest runner comparison, a mounted relationship variant) are applied.

## Limits

- The retained observations come from the completed runs. A real retained failure stops earlier.
- Host completion errors are modelled empty.
- The persistence engine's optional checkpoint and area captures are absent.

The FAIL branch of the reader inspects none of these. It requires only a non-empty error list,
and the receipt envelope and boundary flag. Not exercised: the outer `Test-RuntimeResult.ps1`,
`-VerifyLiveWorkingIdentity` and `Assert-KmcBaseGameNativeInventory`; the inventory used is a
byte copy of one already validated natively. An exit-produced result remains a FAIL and never
qualifies.
