# Horse implementation report

Status: IN PROGRESS

## Starting point

- branch: `codex/mounted-combat-phase3-horse`
- exact Phase 2 closure base: `ecb89500eb36eabbf889ccda7185843bd1e3e7c5`
- accepted Mammoth implementation: `1241222459209aea1e6127bedd7d630df3940b99`
- inherited Phase 2 product version: `0.1.0-phase2b-dev.1`
- active Horse Tranche A version: `0.1.0-phase3a-dev.2`
- dev.2 version-bound offline gates: source `21/0`, Release, component `241/0`, visual/source-order `17/0`, harness `229/0`, assembly `314/0` (`290` Kingmaker + `24` Wrath)

Phase 2 remains accepted with its documented private-alpha limitations. Horse work does not retroactively claim stock right-click mounted attacks, mounted auto-attack, unified Wrath-style turns, animated Mammoth TB locomotion, or public-release quality.

## Current result

Tranche 0 assembly forensics is complete. Exact Wrath evidence supports a later coordinated shared-turn design but not a broad port during horse development. The Phase 2 separate-turn model remains the functioning fallback.

The exact native `CR1_HorseRiding` file identities have been reverified. First bounded run `20260825T162200Z-horse-native-asset-audit-passA` restored exactly and is preserved uncredited `FAIL 15/5`: it established exact `PonySummoned` GUID/resource and class/companion inputs, but a wrong parameterless view-load reflection assumption prevented every view comparison. Exact installed token `0x06007478` proves `Load(Boolean ignorePreloadWarning=false)`; the one observation-only repair is offline-green and pending a clean audited retry. It does not alter ordinary gameplay.

KMC companion implementation, Ranger integration, unmounted qualification, mounted profile work, target-selected Mount, final packaging, and horse runtime evidence remain in progress.

## Implementation ledger

| Area | Status | Evidence |
|---|---|---|
| Wrath command/turn model | PASS | `planning/WOTR-MOUNTED-COMMAND-MODEL.md` |
| Horse native file identity | PASS | `planning/HORSE-PONY-ASSET-AUDIT.md` |
| Pony comparison | IN PROGRESS | exact `PonySummoned` identity established; repaired view audit pending |
| KMC blueprint trio | TODO | contract defined; production gate closed |
| Ranger selection | TODO | exact stock selection pending runtime audit |
| Unmounted horse | TODO | no credited runtime rows |
| Mounted horse profile | TODO | blocked on unmounted qualification |
| Target-selected Mount | TODO | design/implementation authorized |
| Mammoth regression | TODO | required only after relevant shared changes |
| Paladin Divine Steed | DESIGN ONLY | `docs/PALADIN-DIVINE-STEED-DESIGN.md` |

No horse package or runtime claim exists yet.
