# Phase 3E Paired-Scheduler Playtest

Status: IN PROGRESS

Version `0.1.0-phase3e-dev.2` was packaged and exercised once in Kingmaker. No Phase 3E Gate 1 row is credited yet because the immutable run remains `FAIL`; version `0.1.0-phase3e-dev.3` is the diagnostic-only correction candidate.

The immutable observation input remains `20260904T060000Z-phase3e-dev1-mammoth-tb-observation-passA`: gameplay `FAIL 49/1` by design, exact command encounters `2,485`, stock false `2,485`, stock true `0`, scheduler drives `0`, and immediate independent restoration audit PASS. It selects Option A but is not a vertical-slice success.

## Immutable dev.2 attempt

Package `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-phase3e-dev.2-paired-scheduler-vertical-slice-diagnostic.zip` is bound to commit `20d71e5a5b64b85b1815e9ea0c00ff3d7f03dd4e`. ZIP/manifest/DLL SHA-256 are `70a060cc287fa379de35cb02337ba7e2a3348788db54374e4d3e6f6efd5a852d` / `4dc7fa9ba3ab246b939035f3f6f1bd9fc4ab9eb2f3a52aeeb7d6d0a1d4c6b3d2` / `d71529ba6006bc6fa2c8916953cb773f1c5319e49ce4a0c9d70420e1aee26d87`; DLL MVID is `89de0fcc-ca6f-41cd-944b-097a5860716c`. Suite `20260904T084500Z-phase3e-dev2-paired-scheduler-suite1` has snapshot SHA-256 `3349e429905be71e2bde6db01c4788647c890cfea8b947d1c8d527eebaf2920f`. Its focused WhatIf passed exact zero mutation.

Live run `20260904T094306Z-phase3e-dev2-mammoth-tb-passA` remains immutable `FAIL 69/1`. Request/combat/game/final/orchestration SHA-256 are `b345b142dab87dac64e4768f3d7947e0e80ce32836fc6347cd3b62a73a825cbd` / `cae5da2cd59f8ba89a3e8c6ff7a76adc3d6306e58eff9c399685376be45dd3de` / `2fe4316d545bf2009b87547bd0b0092a2bf9de9ddf01f2cc80d3ae60839c333c` / `f0a697c9948f877c222540d6d483b439e5dd5989c039a6e23a9fc6f0c12a9ebf` / `ffc8485c7924ea18cfaddf08a6e3643cc8419e125b39c98fb6db0de666b93603`.

The scheduler admitted at frame `3982`; native `WaitingForUI` delayed the first eligible grant until `4293`; native start followed at `4294`; the last once-per-frame drive was `4527`. The run records `235` drives, one start, one `Success` terminal result, one mount Standard charge (`0 -> 6`), rider Standard unchanged (`0 -> 0`), one exact Mammoth attack/roll/damage chain, mount rule/weapon/resource ownership, rider `CurrentTurn.Unit` throughout, zero duplicate-frame drives, zero foreign adoption, no mount turn, no fault, and exact disposal. The single failure was the diagnostic expression `start - admission <= 2`; it counted native UI-blocked frames as actionable.

The immediate independent suite-bound audit passed before evidence inspection: save content digest `ddba0c041443e8bd12e3cbf5929b6b6aba296b666b26e4d8e73bb97918ecbd45`, Mods digest `82f176c4cb9d8fcfcc21f84948b4fd4b8ed856a9c65dfc7836934a32e2fc61be`, baseline `c29d965c9ff5dc0f971659d9ae154877aa4a9a461ca220d1ce28e7c7fd9d2512`, Working `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`, and no process/lock/sentinel/live KMC.

## Next qualifying runs

The first credited gameplay gate remains the focused `mounted-mammoth-primary-hit-tb` scenario, represented by `shared-rider-turn-mount-primary-passA` and `shared-rider-turn-mount-primary-passB`. Both must use one immutable clean dev.3 package and suite, separate fresh game processes/run IDs, and audit-before-read after each process.

Required credit is one in-range mount-owned Standard command, first grant followed by native start within two actionable frames, one wrapper/child/animation/attack-roll chain, at most one damage event, one terminal result, one mount Standard charge, zero rider Standard cost, exact rider `CurrentTurn.Unit` throughout, zero native mount turn, zero duplicate-frame drive, zero foreign adoption, and exact slot/lease cleanup.

Dev.3 changes only the actionable-frame assertion to `start - firstGrant <= 2`; its synthetic evidence uses a `311`-frame UI wait and rejects a start three frames after grant. It does not alter scheduler production behavior or consume a scheduler repair cycle. If either fresh process fails, preserve it unchanged and attribute the exact native boundary before using a bounded repair. Do not weaken schema 56, relabel dev.2, broaden to a global controller, or begin later tranches first.
