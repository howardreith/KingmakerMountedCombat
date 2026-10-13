# Chunk 7 preview8 private candidate

Status: IN PROGRESS; breadth candidate not ready. This is an offline-qualified private artifact for the [selected continuation](../planning/CHUNK7-CONTINUATION-MISSION.md), not owner-play acceptance or a permanent installation.

## Exact artifact and source

- Branch: `codex/mounted-combat-chunk7-foundation`; source `8ff4413a6c02a9fda60b52891812d719c56b28bd`, tree `be4a98c494cc353a2ddfd41e8e5c5fef06d60a18`; guarded-pushed and remote verified.
- Product `0.1.0-chunk7b-preview.8`; package `C:/Dev/KingmakerMountedCombatLab/artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.8-c7b-world08-diagnostic.zip`. Package SHA256 `c870d0bfdc4e5f32565b1b1a8d11bbdc5fbd4297cd072ea70af967b72e08077d`; manifest `aa506ccbcb18bddf22e39a96b23fbb7070a03400e167ad7d21f18e575ec2fed5`.
- DLL SHA256 `6e3700f9110aaf9be93dabbc4042c0f689617c41716829b33f2ceab67cca9c59`; MVID `d4ca19f7-6a1a-4068-895c-be51e8175a73`.
- Frozen detached source: `C:/Dev/KingmakerMountedCombatLab/worktrees/chunk7b-preview8-8ff4413`. It remains distinct from later documentation/harness commits.
- Private freeze `analysis-cache/chunk7-continuation/candidate-world08/freeze01.json`, SHA256 `39dffa17af8c6221219183a5564f46805946050489a65a8c84a0c486cd4e207e`. Suite `chunk7b-world08-20261013`, SHA256 `2d025294e37c6f4852f718ef664992aa1b9474316de2f3861f9227604ff5054c`.

## Actual qualification

The independently reviewed [passive diagnostics](CHUNK7-PASSIVE-DIAGNOSTICS-CHECKPOINT.md) join the earlier [world replay and host repair](CHUNK7-REPLAY-HOST-INTEGRATION-CHECKPOINT.md). Source composition18 plus documentation5 passed independent review `a1531a19870596b05c9460e30f429c739754c6525c20313aaf6b31a403bd4852`; clean published-source binding `ad107d26fa0b3beeea26ff98a4273c9a36253fc75c32031f9fd775583272b404` preserved all23 hashes.

`Package.ps1 -ArtifactQualifier c7b-world08` built Release once. Actual outer caller bd81b5 exited0 in8.7061402s; supervisor8.4440032s; package validation11/0. `Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker -SkipBuild` passed6/6 in206.9234567s, including817 components,147 source checks and701 Kingmaker assembly contracts. Caller a52a67/provider86189 completed5ef9a6 exit0; source, DLL and package stayed unchanged. No separate unchanged Fast or Full campaign ran. Counts overlap with focused source evidence.

The suite snapshot WhatIf passed95.098179s (8b9c8b/provider4923→afad2f exit0), then append-only creation passed41.7031153s (49b28e/provider89458→a13927 exit0). This read the current inventory and preserved all existing worktrees, artifacts, protected saves and Mods. Suite inventory digests use their own qualification inventory format and are not substituted for transaction-restoration digests. The later native attempt and its restoration are recorded below; this suite receipt remains bound to its original creation.

The same specifically approved guarded Working diagnostic ran on this source:99b3fd exit1,2.1063158s outer/1.6939189s supervisor; all144 pins unchanged. It remains unsupported-reference FAIL at record2, but the valid inline prefix contains one literal main-ID match at record0. A single backward lexical target exists for record2 within party.json. Neither proves native resolution or eligibility. [Actual04 details](CHUNK7-WORKING-ACTOR-GRAPH-DIAGNOSTIC.md).

## Native attempt and remaining guarded case

The installed-configuration case `20261013-chunk7b-world08-installed-control` is settled FAIL. Its fresh full WhatIf passed8566.4301769s on harnessf48602ea2ae33d135b70a83399c6ec2ed0c13815;1,340 input pins unchanged. Native supervisor exited1 after99.0097999s; game43.5175909s. Both exact native mounts and three descriptive sampling windows completed; the first ground click failed. Raw4/1 and cleanupPASS endedDefault/loaded/movementfalse. No successful movement/cycles/matched-pool or ordinary progression result is claimed. [Exact native result and restoration](CHUNK7-NATIVE-OBSERVATIONS-2026-10-12.md).

Restoration review75df1791181d5caeaece72649e2ec9f68e01d926ec20ebee54ff990e465fb8b8 passed22 metadata checks: exact Mods and protected-save before/restored digests, Baseline immutable, original Working restored, errors[]. Changed Working8d8363d2 was quarantined despite game saveRequests0; cause is unknown. Profile/cache snapshotting was not selected for this case. The original nativeFAIL/exit1/afterPins=false and separate1,340/0 postcheck remain distinct.

The remaining case is `20261013-chunk7b-world08-bootstrap` (`chunk7-native-main-bootstrap`, BaseGameOnly, authored-main-campaign). Prepare a fresh full WhatIf on the new clean documentation/harness HEAD with this unchanged frozen product. The unexecuted f486 bootstrap concrete01 is superseded and preserved; exploration admission cannot be reused. Plan180minutes per audit based on the measured143-minute scan; keep original guards and no kill timer. Native budget7/25, all seven attemptsfinalFAIL; one of this candidate's two selected cases remains, with no automatic retry.

Bootstrap passively diagnoses the actual Dialog transition and still requires disabled movement diagnostics, Default mode and a loaded area. A failed Auto is ineligible to seed cold/Manual. Direct combat/persistence require BaseGameOnly; the installed-configuration exploration does not satisfy those gates. The private41-path exact-actor/lifetime/Working observer composition is reviewed and compiled but not integrated or included in this artifact.

Supported limits remain native2/offline6/retained12; native schema3 writer with explicit schema1/2 adapters, no decode-derived restoration authority; Medium Horse/Mammoth, Small catalogueEMPTY; RTCharge default-off; TBCharge cost-free refusal and defensivefeat deferred. Required native combat/persistence, progression, Small/stress, measurements and unmounted controls remain open. HUMAN PLAY and full7E are not selected here. Use the private artifact only through the repository guarded harness; no permanent owner-play installation is instructed.
