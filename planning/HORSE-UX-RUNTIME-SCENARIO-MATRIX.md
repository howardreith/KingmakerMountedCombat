# Horse UX runtime scenario matrix

Status: `IN PROGRESS`

Date: 2026-08-28

All live rows require stable-suite admission, KMC Working-only save authority, KMC-only Mods staging, targeted WhatIf, audit-before-evidence, same-package identity, and exact restoration.

| Area | Required scenarios | Evidence boundary |
|---|---|---|
| native controls | native Mount present; exact Horse/Mammoth target; invalid target; native Dismount; save/load presence; respec away/back; disable/re-enable; no duplicate fact or hotbar binding; no user-slot overwrite; overlay default hidden/debug fallback | exact blueprint/fact/slot identities plus physical native cursor/click where safe |
| TB primaries | rider turn; Horse turn; both wrong-turn rejections; spent Standard; target-click admission; invalid target feedback | native ability activation -> pointer -> target -> `UnitUseAbility` -> exact KMC command -> terminal result |
| RT primaries | Rider and Horse primary plus non-mounted control | one attack/roll/damage/resource chain; no stock right-click claim |
| portrait/UI | large/small/party/selection/feature/control icons; inventory Horse preview | no Horse surface reference-identical to Mammoth art; no attributable IK exception |
| movement | unmounted Horse, mounted pair, mixed group, stock controls, RT/TB | measured displacement and animation state, not sheet Speed alone |
| animation/pose | Horse primary RT/TB; idle/walk/run/turn/stop; bounded pose candidates | visible native attack; zero duplicate rule chain; Mammoth profile byte/source unchanged |
| lifecycle/isolation | save and area clean dismount; inventory preserves pair; door busy/success; Wild Shape; explicit Dismount; Mammoth targeted regression; foreign-mod isolation | exact cleanup and terminology; no mutation outside KMC |

Physical pointer feel, icon readability, portrait quality, final pose, attack-animation readability, and ordinary gameplay flow remain manual gates when automation cannot prove them truthfully.

