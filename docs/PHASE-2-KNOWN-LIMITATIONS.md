# Phase 2 known limitations

Recorded: `2026-08-25T12:00:39Z`

These limitations are part of the accepted Mammoth private-alpha contract. They distinguish supported behavior from future work; they are not release-note euphemisms for implemented features.

## Accepted limitations

- Mounted rider attacks are invoked through the KMC Rider primary control. A normal stock attack/right-click does not initiate them.
- The Mammoth natural attack is invoked through the KMC Mammoth primary control.
- Stock mounted auto-attack is absent.
- Rider and Mammoth have separate turn-based turns. Rider-turn movement is delegated to the Mammoth, but the turns are not combined.
- Mammoth turn-based movement can visually slide without the expected locomotion animation.
- Mounted ranged attacks reject visibly and deterministically; mounted ranged combat is absent.
- The relationship is runtime-only and nonserialized.
- Save, load, and area-transition boundaries intentionally cleanly dismount. Reload and arrival remain unmounted.
- Automatic remount is absent.
- The Mammoth pose remains private-alpha quality: seat gap, stiffness, no saddle, and no reins.

## Supported controls and lifecycle

Within those limits, mounting, ordinary mounted movement, explicit dismount, KMC-controlled Rider primary, KMC-controlled Mammoth primary, menu continuity, Wild Shape cleanup, save/load/area clean dismount, and distant-door approach/open/traverse are supported by the accepted private alpha.

## Historical diagnostics, not current product failures

Failed and diagnostic runtime packages remain immutable evidence. In particular, native path objects can be replaced during stock post-door tile rebuilding. The final evidence attributes those replacements to `TileHandler` frame advancement while the exact command stays healthy and reaches the target. Raw path identity is therefore telemetry, not an independent acceptance gate.

The classification does not excuse command replacement, path failure, missed targets, excessive unattributed repath churn, visible oscillation or backtracking, duplicate interactions, regressions outside mounting, or cleanup/restoration failures.

## Unimplemented Phase 2 features

- stock attack/right-click integration;
- mounted auto-attack;
- a proven Wrath-style unified mounted command surface;
- combined/coordinated turn presentation beyond the accepted separate-turn fallback;
- mounted ranged combat and mounted spellcasting rules;
- mounted attacks of opportunity and charge;
- persistent mounted state and automatic remount;
- saddle, reins, or release-quality presentation;
- Small riders, enemy riders, additional mount species, and public release.

## Future work boundary

Horse Tranches A-C may proceed without resolving the Mammoth backlog. Exact Wrath command-model research can inform a later generic improvement, but the accepted separate-turn and overlay-control model remains the safe fallback until a replacement is independently proven. The horse implementation must use an independent profile and must not change the accepted Mammoth offsets or behavior.
