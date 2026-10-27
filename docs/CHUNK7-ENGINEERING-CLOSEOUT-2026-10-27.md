# Chunk 7 engineering closeout — IN PROGRESS

Final qualification is running. PR #5 is open/draft; it has not merged. Owner play
and visual acceptance remain pending. The owner authorized a conditional merge
after engineering, review, restoration and repository gates pass. No release/tag
or permanent installation is selected.

F6 cannot close the mandatory matrix: r09 failed7/1, so PR promotion remains
blocked by that engineering gate. A bounded diagnostic correction has passed
independent review and focused checks; its compiled native pilot and successor
qualification are still pending. F6 is closed after B10, with32 planned launch slots
still TODO. This document records F6's actual
identities and outcomes until the qualified successor is established.

## Frozen candidate

| Identity | Value |
| --- | --- |
| Source commit | `ebeaf8f6b3d81ed7a189ca4b99fe2e95ceb56ad8` |
| Source tree | `d644ca52cab712aa4b68006852dc85698ee11d11` |
| Product source subtree (`src`) | `494d64b1530425470c972cc9dd1d05ee0c54e749` |
| Harness subtree (`scripts`) | `ab52bf8286f50549c03d5dd6cadbb881db1747e5` |
| Tools subtree | `24ea33e0f9997f8b763912b5235c75ba0e660302` |
| Tests subtree | `50d8b26ec13b069543e8913fa6e8781cb0ad239d` |
| Published review branch | `codex/mounted-combat-chunk7-successor` |
| Frozen executor | `C:\Dev\KingmakerMountedCombatLab\worktrees\chunk7-b218-pilot-ebeaf8f6` |
| Frozen local branch | `codex/mounted-combat-chunk7-b218-pilot-ebeaf8f6` |
| Version | `0.1.0-chunk7b-preview.95` |
| DLL SHA-256 | `7f48118c7b16025447ff68c2f6138e2d33ee7aa81231578611b59a24569cc576` |
| DLL MVID | `32ac6aca-d9b4-4db8-9b49-2b5c5fe0f328` |

Private package:
`C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.95-c7e-b218-pilots-diagnostic.zip`.
SHA-256 `5a0d5281d376cac6577a593dbef9166e47556420513355188468b8bd4ea38149`.
Manifest SHA-256 `c3d420daad54e9e104d842d2be2bdf73972b6ea4e53c5e6a6ca5796fc96893ca`.
The archive contains exactly `Info.json` (329 bytes) and
`KingmakerMountedCombat.dll` (8,196,096 bytes). The package is retained unchanged
from b218; its filename is provenance, not a claim that pilot results are final credit.

Observer.6 package SHA-256
`dbed011be38c0d221ddecb6e39c592c54a60a3be2a966bf368905495b82ab20a`,
manifest `e3cb84163fd3951b464bfe97f1b1b320444f6adc84de95c31340443990374102`,
DLL `be461deab283702e35279c5c9c5c511e2a827a4e7f24125b4ee4c405651cfeef`,
MVID `de961e8e-0cb4-4ae8-ae7b-bf1c0fa8aece`. It is a lab observer, not part of the
two-file player package.

F6 suite: `chunk7e-final-ebeaf8f6-20261027`, SHA-256
`2ea084a946f98dbb57931c97ba0ab167fe9b9633319dd47d5608f1e2af9cea2b`.
Snapshot save digest `e4811c3a89deca122b997115540ddc364aa8a54fc85718ea59133dfe81094963`;
Mods digest `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
The snapshot binds exact environment, fixtures, configuration, protected-save
authority and executor identities. The private freeze record is
`analysis-cache/chunk7-continuation/final6-freeze.json`, SHA-256
`44bdb802d94414281a7f4d170022b85105f1abab8be89aa8d6009cab0a12e02e`.

## Qualification state

| Gate | Actual disposition |
| --- | --- |
| Package/source validation | PASS: source149/0, package11/0, observer7/0 |
| Candidate tier, reused exact build | PASS6/0, inner751/0, exit0, 6.22 minutes |
| Affected native pilot | PASS12 launches, 80/0 subscenarios; six affected paths and required source chain |
| Independent pilot evidence binding | PASS; exact candidate, observations, result hashes and restoration checked |
| Scenario dispatch | PASS181/0, exit0 |
| Raw FULL | FAIL, exit1, 539.6 seconds; known SupportingEvidence stop described below |
| Remaining offline inventory | Raw242 PASS/7 FAIL, no skips, 2672.410 seconds; six accepted historical failures; step176 separately corrected by reviewed test-only PASS242/0 in32.292 seconds; successful FULL prefix reused |
| Final native matrix | F6 partial FAIL: planned81 rows/82 launches/17 batches;52 actual launches,49 PASS/3 FAIL. r09 native FAIL7/1 and two signature-matched startup failures with permitted successful retries retained.32 planned slots TODO, no exclusions added |
| F11 performance | TODO: fixed protocol; no result from an older candidate is final credit |
| Closeout/final repository review | IN PROGRESS |
| PR #5 merge and main readback | TODO |
| Owner play/visual acceptance | TODO; every owner checklist box remains unchecked |

The [matrix](../planning/CHUNK7-7E-QUALIFICATION-MATRIX.md) and
[final run plan](../planning/CHUNK7-7E-FINAL-RUN-PLAN.md) select the complete coverage.
Run IDs begin `20261029-c7e-final-B<n>-<tag>` to avoid all retained F1–F5 IDs;
the date is a campaign discriminator. Actual start/end timestamps are in each receipt.
Final row verdicts, source chains, fixture identities and exceptions will be bound
here after the final campaign; no pilot result transfers to this matrix.

F6 r09 failed when Horse B's reaction, spent before the player-click window,
renewed by native cooldown expiry during that window. Standard/move/swift debts
stayed zero; independent exact-assembly inspection established the native
renewal contract. The correction adds only the bounded zero-new-AoO observation
path, preserving native resource ownership and all limits. It passed72 compiled
checks,31 reader cases and the1462/0 owned-pairs reader regression, plus independent
review. The original full native FAIL stays FAIL: later stages never executed.
This compiled diagnostic change will produce different DLL bytes and needs new
native evidence. The detailed private record is
`analysis-cache/chunk7-continuation/final6-r09-renewal-correction.md`.

FULL stopped at `Test-Chunk6aSupportingEvidence.ps1` (`Test.ps1` line114):
`Supporting artifact lacks exact PASS row: CM01-combat-mount-preparing-admitted`.
This is the accepted superseded Preparing expectation, not a passing FULL result.
The full transcript SHA-256 is
`14ef5ad1b83ecc6fe01cef7adfe6121627517816b7901be45dc313e00b75e622`.
The five separately classified historical ledger failures retain their unresolved
cause and original verdicts. Step176 originally failed235/5 because historical
preview.94 requests reached the correct preview.95 version guard. A bounded
test-only correction now passes242/0: current binders use pinned historical full
readers for those old records; the current request-hash guard and strict version
refusal are independently exercised. No historical evidence is relabelled and no
runtime reader or product bytes changed. The original235/5 and intermediate241/1
remain retained. The offline disposition receipt SHA256 is
`b6030a674b0652d5aa943089cca9bee541787ea6eb997900078621bf857032cc`;
independent correction review SHA256 is
`397060494585ccc3f03dcefa58851bebc67bdaf998a2e2e8d7c1e8e1077cbd28`.

Performance follows the [fixed F11 protocol](CHUNK7-F11-ACCEPTANCE-PROTOCOL-2026-10-23.md):
GC-inclusive windows, declared medians, 0.5 ms two-pair and 1 ms six-pair budgets,
and the existing scaling/allocation rules. No threshold change, window removal
or repeated benchmark for a favorable sample is authorized.

## Restoration, limits and owner handoff

The last pilot, `20261027-c7e-b218-x4cr`, and all other b218 launches restored
exactly, with zero admitted cache changes. Their protected-save restoration digest is
`60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1`.
Mods, settings and profile restoration were verified separately.
Final-campaign restoration is pending each actual transaction. Any admitted native
cache exception will be named separately from exact byte restoration. Human saves,
the baseline fixture, owner masters, prior candidates and failed evidence are retained.

All F6 attempts through B9 restored exactly. B10 k1 passed4/0 with the existing
`native-achievement-cache-settled-size` exception: four rewritten cache leaves and
one created leaf, all12288 bytes. Mods, settings and protected saves were restored;
the profile was not byte-identical. Receipt `final6-k1-cache-exception.json`, SHA256
`28fbd48c0fef0048e12254aee0fdab0e14980e3b6e613e8b90c298f7f698dad0`,
binds the original cache-change receipt and settled check. This exception is not
described as exact profile restoration.

The accepted limitation remains: no mounted archive actually written by preview.211
has undergone an automated upgrade. Migrated schema derivatives and installation/
cache upgrade rehearsals do not supply that proof. Rollback uses the pre-upgrade
save. Haste variation and six-pair combat remain owner-excluded; the full existing
dispositions are in the matrix. Prepared casting, BAB6 full attacks, prone stand-up,
defensive casting and the pending-only cold-load path remain mandatory final rows.

See the [changelog](CHUNK7-CHANGELOG.md), [support guide](CHUNK7-SUPPORT-CONFIG.md),
[installation/migration/rollback/removal guide](CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md)
and [owner play checklist](CHUNK7-OWNER-PLAY-CHECKLIST.md). Qualification outcomes
and the exact private completion package will be finalized after the remaining gates.
The final PR head and merge commit will be mapped back to the frozen product and
execution trees; documentation-only closeout edits do not require rebuilding them.
