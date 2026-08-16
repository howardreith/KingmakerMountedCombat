# Autonomous blockers

Status: IN PROGRESS

Branch: `codex/mounted-combat-phase2-alpha`

## Intake and authorization disposition

Repository identity, exact ancestry, origin/upstream, mission hash, frozen Phase 1 evidence, installed environment, schema-v2 protected-save authority, package pins, and post-review external restoration are PASS. The user explicitly accepted exact Phase 2A commit `09a63729e0847c540ae7e79e9e3876d005ee9afe` and package SHA-256 `80b0498cd9d1e6d8dd518ffd8d343af56bcc711306492548c221b9559c98cef7`. The manual visual gate is closed and combat Tranches C-F are authorized without intermediate approval. No critical blocker is active.

## Preserved manual-session discrepancy

The sole manual session reached exact `READY`, bound the accepted package/DLL/MVID, remained read-only, and restored Mods, Working, Baseline, all protected saves, locks, sentinels, processes, and live deployment exactly. Contrary to the user's observed terminal PASS, the immutable durable `manual-review-result.json` and orchestration records say `FAIL`: normal exit removed loaded fixture objects roughly four seconds before process termination, and the in-game monitor classified that teardown as a boundary escape. The visual acceptance remains explicit and valid; the terminal result is not relabeled. A bounded, one-way post-READY teardown guard plus deterministic tests repair the attributable monitor defect before combat runtime use.

## Protected external state

Exactly one Baseline and one Working exist, with zero KMC near-matches. Baseline is immutable. Only exact `KMC_AUTOMATION_WORKING` may change inside the guarded transaction. Chained authority epoch `20260816T0115379973604Z-user-attested-quicksave-continuity`, SHA-256 `111c794baf8bd1062ef7ecf8c307f4e3badb7a519cf3e66ae1f8f2442b770701`, protects inventory digest `03a91757453a8e875e376f31f1664decb0a6e60dcf6433f2348795a90be634c9` and pin-set digest `6f137d57e3a519b3a315a4c654b5feee51c8495f7104c26bfd4da097930c9ff4`. Its schema-v1 parent remains byte-identical at SHA-256 `77fd415d177423dd7da58e3685e418f38cf954dead75068424a717e2be6b3ca9`. `Quick_3.zks` and `Quick_438.zks` remain protected and non-writable. Any later non-Working drift, restoration failure, unexpected process/dialog, Steam/account/update/cloud prompt, or transaction ambiguity is an immediate critical stop.

## Known presentation issues, not blockers

- slight rider seat gap/hover above the Mammoth back;
- stiff analytical pose and no saddle or reins;
- evidence limited to the exact Medium-humanoid/Mammoth/one-handed profile;
- the accepted profile is Mammoth-specific and cannot be generalized;
- contradictory mounted feedback observed during review, now covered by a narrow state-projection repair;
- fixture occlusion, selection silhouette, camera feel, and pointer feel remain private-alpha limitations.

## Exact next action

Probe N `20260816T075000Z-rider-hit-rt-probeN` from guarded-published `8e857527dc1ba789cb434a7230b8706f5d3d92b7` truthfully fails `43/4` with no qualification credit, but the native child now reaches exact `Success` and terminal `completed`. The only failures are missing global Rulebook observations/forced d20 and missing rider Standard delta. Exact installed contracts prove the probe used a non-subscribing plain handler interface and the wrapper manually set `IsActed` before `UnitActionController.TickCommand` could observe its required false-to-true transition. The narrow worktree uses `IGlobalRulebookHandler<T>` and defers child ticking until the base wrapper publishes that native transition; it never edits cooldowns or fabricates rule evidence. Complete offline gates pass (`21/0`, `192/0`, `17/0`, `155/0`, assembly `173/0`). Commit and guarded-publish, package clean HEAD, pass full-pin WhatIf, then run fresh Probe O and independently audit restoration before evidence inspection. Do not weaken product/native gates, launch Wrath, touch another project, create a public release, or merge `main`.
