# Chunk 7 batch105: current-candidate diagnostics-off contracts

This is a focused **offline contract PASS**, not a native diagnostics-off or
performance PASS. There was no product edit, rebuild, package, game launch or
runtime transaction. The tested `bin/Release/KingmakerMountedCombat.dll` was
hash-checked against world114's unchanged DLL SHA-256
`49e3b899e32bbe996b9449e86c9871cfac9ebeb934aa0339e774f4e169080b86`.

`scripts/chunk7/Test-Chunk7QuietWindowContracts.ps1` passed 28/0 against that
actual assembly and the exact installed Kingmaker managed-assembly pin. Its
actual-method checks found no quiet/measurement observer on ordinary assembly
load, diagnostic overlay disabled in default settings, and a null automation
host when no runtime request is present. They also exercised opt-in quiet-window
ownership, timeout and cleanup/refusal behavior. Test source SHA-256 is
`cdfa0f771cde10bf8ed959b414a89a4b63c24a07bcff674c15905f6975db94d2`;
compiled test executable SHA-256 is
`578b7306b1db9075bcbad98a017d9037c39f41d6cd063d1a115bc21dbf31c65b`.
Private output: `analysis-cache/chunk7-continuation/codex-takeover/quiet-world114-b105-20261021/`.

`scripts/chunk7/Test-Chunk7DiagnosticsOff.ps1` passed 29/0 on the current
actual cooldown patch, automation host and consumer methods. An absent/removed
observer could not receive a callback, and an inactive prefix could not be
inherited by a later consumer. This adapter stubs external native boundaries;
its receipt explicitly reports `nativeProof=false` and
`measuredPerformanceProof=false`. Receipt:
`analysis-cache/chunk7-continuation/codex-takeover/diagnostics-off-world114-b105-20261021/receipt.json`,
SHA-256 `e27995c0cf755e0a9d95cd70b035b6321b88ac8abcef160e2d5ae4de7b2a5e91`.

F11 remains **IN PROGRESS**. A guarded native run with the normal diagnostics
configuration must still show ordinary controls, no automatic diagnostic
actions/overlay, stable subscriptions and logs; the exact final candidate also
needs an honest allocation-event/byte-rate instrument or an explicit unresolved
gate. The current quiet-window result does not substitute for those outcomes.
