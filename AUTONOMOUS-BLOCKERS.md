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

The contract checkpoint is guarded-published at `b60c9f145827ce45ab75e93726fe56dc1b534f58`. The first `0.1.0-phase2b-dev.1` implementation is compile-clean and offline-green: pure ownership/range/transaction/target tests, exact one-Standard wrapper, scoped rider/Mammoth native attacks, Mammoth-only approach, exact TB movement charging/turn suppression, combat UI, AI lease, transient target manager, and rule probe. Commit and guarded-publish this implementation checkpoint, then extend the save-backed request/result protocol with strictly validated combat evidence before any live launch. No critical blocker is active. Do not launch Wrath, touch another project, create a public release, merge `main`, or implement any unauthorized feature.
