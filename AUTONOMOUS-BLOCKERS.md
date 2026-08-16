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

Probe I `20260816T061000Z-rider-hit-rt-probeI` from guarded-published `17900ac59bbbd2f59ac19417e8fab0a5fe0099bc` proves exact Standard-slot ownership, then truthfully fails `27/2` while Kingmaker remains in combat-entry pause: range/ownership/`CanAct`/cooldown pass, but hands and equipment remain busy and `CanActInCombat` remains false. There is no qualification credit and production must not bypass those stock gates. The diagnostic worktree leases/restores exact pause state, unpauses only the real-time row after combat entry, waits for every pinned native start gate, adds schema-v2 dispatch evidence, retains schema-v1 history, and gives cleanup an independent bounded drain. Complete offline gates pass (`21/0`, `185/0`, `17/0`, `153/0`, assembly `158/0`). Commit and guarded-publish, package clean HEAD, pass full-pin WhatIf, then run fresh Probe J. Do not alter product cooldowns/initiative/equipment state, launch Wrath, touch another project, create a public release, or merge `main`.
