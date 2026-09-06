# Preview 13 local installation and human checks

The owner separately requested “Please install it to the local UMM and tell me what to test.” This authorizes the local installation after the temporary-only engineering campaign. The installed candidate is **0.1.0-chunk1-preview.13**, source `a8745640e18ce068e412b4e360c7b0a3d46c738a`; repository HEAD at installation was `b3f063337644215312de97d9736892212777ac1c`. [Package and native qualification identities](CHUNK1-ORDINARY-ATTACKS.md#exact-candidate) remain unchanged.

Guarded Replace completed `2026-09-06T23:25:51Z`; VerifyInstalled and independent checks passed at `23:26:44Z`. Package validation: 10 PASS / 0 FAIL. KMC is enabled in the existing UMM configuration, at `C:/Program Files (x86)/Steam/steamapps/common/Pathfinder Kingmaker/Mods/KingmakerMountedCombat`. Installed DLL SHA-256: `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`.

The helper backed up the complete current preview 7 directory, including its loader cache, to `C:/Dev/KingmakerMountedCombatLab/runtime-backups/deployment-management/20260906T2325510451470Z-f006ae8a6ed14a3399e3e368e0f59b3e/KingmakerMountedCombat`. All 275 save-file identities, UMM parameters and foreign Mods content remained unchanged. No game was launched. Local proof: `analysis-cache/chunk1-preview13-human-install-20260906/verified-install.json`; authoritative operation: `runtime-state/deployment-operations/20260906T2325516182989Z-10125642bbfe4317b02f6e703236a759.json` (paths relative to the lab).

Automatic approval initially treated the earlier installation restriction as still active. The same unchanged guarded command was accepted after the latest explicit owner authorization and fresh safety evidence were supplied. No policy or guard was changed.

## Focused manual checks

Launch normally and confirm UMM shows preview 13 enabled. Use an expendable manual-play save; keep the protected automation fixtures out of casual play. Leave unified mounted turn, paired scheduler and diagnostic overlay disabled. Use one supported rider/Horse or rider/Mammoth pair. Mount before combat so mounting's action cost does not contaminate the stationary test.

1. **TB stationary ordinary attack:** on a fresh rider turn with Standard and Move available, use the normal enemy click with a longbow target already in range. Compare with an unmounted control under the same equipment, feats and action state. Expect the native full sequence for that character. Repeat with Rapid Shot off/on, and haste if available; inspect combat-log attack counts and modifiers. There is no universal five-shot expectation.
2. **Explicit single actions:** on separate fresh turns, use Rider Primary, Mount Primary and the game's native Single choice. Each should deliver one attack owned by the intended actor. A spent Standard must not permit an extra attack. Compare the visible action costs of full and single attacks.
3. **Hover and repeat clicks:** hover repeatedly before ordering; actions and pair position should stay unchanged. Repeated clicks on the same enemy during a sequence should not restart it or duplicate attacks.
4. **RT attacks and range:** try ordinary rider melee/ranged and Horse attacks, both adjacent and requiring approach. Watch Horse Bite's visible strike and recovery. A later bite must not hit an enemy from bow distance. Include one Mammoth Primary if available.
5. **Control smoke:** pause, queue an attack, use Stop, then resume. Check party selection and movement, then dismount and confirm ordinary melee/ranged controls still work normally.

Report PASS/FAIL by row. For a failure, include RT/TB, rider and mount, weapon, Rapid Shot/haste state, who moved, actions remaining, expected versus observed attacks, and a combat-log screenshot or short clip if useful. Human play is still pending until these observations are supplied. Complete actor allocations and pair-aware activations remain later chunks; this installation does not qualify those workstreams or mod-absent save safety.
