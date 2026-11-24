# UMM baseline follow-up: validation and factory continuation

Date: 2026-11-23. Base: Mounted Shield PR #61 at
`f19c8333b7aac863e94c5b5c7dfadd89d7ad4a00`.

The owner selected the earlier **0.31.1 compatibility/build baseline** used by Tabletop Rules and Cutscene Skip.
The current Windows native target is separately the reviewed installed **0.33.0** identity. This patch is a
companion development change, not a factory acceptance receipt or a release.

## Executed checks

The genuine upstream 0.31.1 package and DLL matched the sibling archive and DLL SHA-256 pins. Version, MVID,
length and UI tokens were read from the matching DLL's CLI metadata, with two parsers agreeing. The
[metadata record](loader-metadata.json) contains the exact values and scope.

| Check | Result | Actual scope |
|---|---|---|
| Production reference restore helper | 9 passed, 0 failed | Final tracked `scripts/Test-UmmBuildReference.ps1`, PowerShell 7.7, genuine package bytes. Cached extraction/reuse, DLL/archive tamper, missing-cache restore, failed download cleanup, changed manifest and mismatched minimum. Only external download transport was stubbed. |
| MSBuild reference enforcement | 10 passed, 0 failed | The two unchanged production XML targets ran in a minimal project with SDK 10.0.404. Their `Exec` ran the actual helper using PowerShell 7.7 through a `powershell.exe` symlink. Classic/Roslyn references, deduplication, substituted/missing/multiple references, altered cached bytes, changed warm-cache manifest, mismatched Info minimum and missing-DLL restoration were exercised. No product compilation. |
| Profile selection, identity refusals and syntax | 21 passed, 0 failed | Actual pure production profile helpers: 12 behavioral checks, eight changed PowerShell files parsed, and the project XML parsed. No successful .NET Framework reflection-only binding check is implied. |
| Native inventory reader logic | 19 passed, 0 failed | The actual reader and five actual helper bodies, selected through the PowerShell AST, ran against fresh synthetic POSIX paths. Exact 0.33.0 passed; alternate/mixed identities, missing receipts, duplicate creators, altered candidate bytes, foreign paths and malformed cache suffixes failed. This is reader logic evidence, not an observed native inventory. |
| Frozen-product input comparison | 10 passed, 0 failed | Actual production Git-diff/refusal statements ran against real disposable Git commits. Pin, restore-helper, build-bootstrap and product changes refused reuse; identical/restored inputs and a documentation-only descendant remained admissible. The full Windows custody/launcher path was not run. |

The final build targets and restore helper used for the MSBuild probe were:

```text
src/KingmakerMountedCombat/KingmakerMountedCombat.csproj
SHA256 fef015081f799dad136256fa0aca29111c9a8ed80a064b99d9d3bd231d31f396

scripts/Restore-UmmBuildReference.ps1
SHA256 362c02592f455e2f7ba68114805972952d607406f47aa5db2eef96706454ea79
```

Independent source reviews covered the build/restore patch, the exact native/profile changes and the provenance
integration. No blocking defect remained in this companion patch after the final warm-cache correction. These
reviews do not close findings in the underlying Mounted Shield implementation or count as controller review rounds.

## Not executed here

- Windows PowerShell 5.1 and the Windows .NET Framework product build.
- The compiled candidate's complete binding sweep against genuine 0.31.1 and the selected installed loader.
- The full Windows harness and native inventory contract scripts, Windows path/custody/restoration behavior,
  packaging, game startup, a qualified save, Mounted Shield gameplay, or Linux/Proton gameplay.

Portable test execution on Linux establishes only the stated tooling behavior. It is not Linux game compatibility.
Previously recorded native failures and partial/blocked acceptance rows remain unchanged.

## Continuation through the existing factory run

Import this companion commit into the existing clean Mounted Shield worktree and record the owner's baseline
decision using the installed controller's supported mission-amendment route. Retain the existing enrollment,
findings, counters and evidence. Push and verify through that enrollment's guarded route.

The changed compile reference requires a fresh product build. Verify its bindings against both the pinned 0.31.1
reference and the exact selected 0.33.0 runtime using `scripts/Test-UmmLoaderContracts.ps1`; its `-Reference`,
`-Profile` and `-IgnoreLocalProfile` arguments already distinguish these checks. Run the changed Windows reader
contracts and the current feature's focused gate without silently launching an unrelated historical campaign.

The declared minimum and package validator now both require 0.31.1. The loader remains a compile reference with
`Private=False`; the existing package allowlist still excludes loader DLLs. The reference pin and restore/build
helpers are included in frozen-product source comparison, so a change cannot silently reuse an older product.

Finish the outstanding Mounted Shield code corrections and gameplay driver before the next coherent independent
review and guarded native run. Native execution still needs an admitted disposable fixture. The exact loader pin
update resolves the stale 0.33.2 expectation; it does not supply that fixture or create an accepted factory review.
