# Chunk 1: Ordinary attack correctness

Status: **PASS — engineering qualification**, 2026-09-06T19:20:29.992664+00:00. C01-C03 and applicable regression pass in NATIVE INTEGRATION on one exact candidate. HUMAN PLAY and mod-absent certification remain pending under the owner's stated exceptions. This closes ordinary attack correctness, not mounted-combat completion.

## Exact candidate

- Branch: `codex/mounted-combat-phase3f-playable-core`.
- Clean intake/reviewed ancestor and verified starting remote: `1d2b8c3ccad14009653af9dc6420ee9af7b2e804`. All descendants were preserved.
- Historical candidate: `0.1.0-phase3h-preview.6`, packaging source `fb65cf826f5914fc29ab5035e6409685bd982c66` ([frozen report](PHASE3H-IMPLEMENTATION.md)).
- Final source: `a8745640e18ce068e412b4e360c7b0a3d46c738a`; version `0.1.0-chunk1-preview.13`. Later documentation/publication commits do not change this binary source identity.
- Local package: `KingmakerMountedCombat-0.1.0-chunk1-preview.13-actor-isolation-diagnostic.zip`; ZIP SHA-256 `62ddcff6f26e02c6abb6ce80e02a14276b856caa8911a72a5f9f764042173e74`.
- Manifest SHA-256 `8080a6b487c5ea9b9e5fa03298b5a83d71691115f48a5ffa4e5201d212f77bac`; DLL SHA-256 `282f0ad326fdfcd11b8d547cecd70456fa1c90c5a8e68aebd8ab5db3fc9c3864`; MVID `330cbe32-5811-4aa8-b059-7da710ab1ce3`.

The package contains only the project DLL and Info.json. Actual Kingmaker 2.1.7b, UMM 0.28.2, legacy Harmony12, .NET Framework 4.7 and C# 7.3 remain authoritative. No dependency or loader downgrade was introduced.

## Causal finding and repair

**Harness/input defect:** the first trace (`20260906-chunk1-trace-tb-A`, source `665a559d94d842d16fc3ecf9cd37ab530d30a2c2`) showed native Single mode before mounted routing. Hover named the enemy, but PointerOn was null and prediction still selected ClickGroundHandler. The fixture now supplies coherent native pointer state and calls the real prediction/handler pipeline through an exception-safe lease. It uses native cursor input for Single selection. No production prediction defect was demonstrated in this campaign, so no new decision or scheduler framework was added.

**Product defect:** a later natural bite in a mixed bow/bite plan retained the first bow's range. `MountedPairSingleAttack.EvaluateCurrentNativeAdmission` now derives admission radius from the current native PlannedAttack weapon and the authoritative actor's corpulence, guarding the uninitialized plan. Native planning, animation, weapon/rule ownership and charging remain unchanged. The distant B/C controls each plan five, deliver four bow attacks, and reject the out-of-range bite. No full-mode flag, attack list, restriction, or cooldown is forced.

Native contracts were verified locally: prediction 0x06000C6E, cursor mode 0x06000C3F, simulated click 0x060093C7, hostile click 0x060093ED, command admission 0x060026B2, attack planning/start/delivery 0x0600267C/0x0600267E/0x06002681, terminal 0x060027B2, cooldown 0x06009120, sight 0x0600121A. The trace records original/replacement identity, actor/turn, provenance, modes, restrictions, approach, plans, modifiers, delivery and costs. Exact Kingmaker assembly SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

## Qualification

Commands: `scripts/Test.ps1`, `scripts/Package.ps1 -ArtifactQualifier actor-isolation`, and the guarded `RuntimeTest` scenarios below. Full logs are in `analysis-cache/runtime-evidence/chunk1-20260906/`. Host registration/path checks: 15 PASS / 0 FAIL; only exact scenario entries and CURRENT.md documentation classification were added, preserving save/path/process/integrity guards.

The publication helper's first WhatIf rejected ten original PNGs already in the reviewed ancestor. Its registration now permits only their exact paths and SHA-256 values, all verified unchanged from `1d2b8c3c`; the remaining payload prohibitions are preserved. Publication registration tests: 28 PASS / 0 FAIL, including changed bytes, renamed/case-changed files, foreign payloads, outside paths and hashing under WhatIf. A read-only .NET hash avoids Windows PowerShell suppressing its Get-FileHash lookup during WhatIf. The complete guarded dry run passed and confirmed the remote still at the reviewed ancestor. Guard SHA-256 `20f3452da95052d8eeffd0b29f3f2df79310c72f2985c38694c9fa304a4901e6`; exact registrations and tests are retained in the local campaign evidence. This changes no source asset or packaged binary.

| Evidence class | Result |
| --- | --- |
| COMPONENT | 345 PASS / 0 FAIL; real existing service/domain objects |
| ASSEMBLY CONTRACT | 439 PASS / 0 FAIL (415 Kingmaker, 24 read-only Wrath reference) |
| Protocol/build/package | source 22, visual 23, inventory 10, harness 243, Phase 3G 14/Phase 3H 29, ordinary 39, package 10; all PASS/0. Harness includes nine positive/negative focused unmounted envelopes, not gameplay proof. |
| NATIVE INTEGRATION | **32 focused/regression cases PASS / 0 FAIL**, below |
| HUMAN PLAY | TODO: native desktop pipe unavailable (OS error 2); no physical-input/visual approval claimed |
| Mod-absent control A | TODO: requires a safe native fixture without permanent KMC references; no save was stripped |

| Native scope | PASS / FAIL | Exact run |
| --- | --- | --- |
| C01-C03 and mixed-weapon regression | 19 / 0 | `20260906-chunk1-final-ordinary-tb-A` |
| RT ordinary/Primary, approach, pause and Stop | 9 / 0 | `20260906-chunk1-final-rt-A` |
| Unmounted melee and Sling | 2 / 0 | `20260906-chunk1-focused-unmounted-rt-C` |
| Mammoth Primary | 1 / 0 | `20260906-chunk1-final-mammoth-rt-A` |
| Party selection and formation | 1 / 0 | `20260906-chunk1-final-party-A` |

Mammoth passed 62 assertions; party selection/formation passed 60. The final ordinary trace has 2,849 events/0 drops/0 observation errors. C01 B/C native plans and deliveries are 5/5 (four bows plus a bite, not five bow shots); explicit Primary 1/1. Rapid Shot off 4/4; BAB 6 with Rapid Shot off 3/3; Haste 4/4. Native Rapid Shot minus 2, Haste plus 1, iterative penalties, weapon identity, engagement terms and installed difficulty arithmetic are verified. Native full attacks cost Standard 6/Move 3; Primary, native Single and Staggered cost Standard 6/Move 0. A second click after genuine Standard expenditure starts no second attack. Repeated prediction preserves live pair/resources/commands; repeated same-target input preserves the active sequence; a restriction added after prediction is revalidated by native planning.

Rider-owned movement spends rider Move and yields the native single plan. Carried movement spends mount Move (1.1027422 in this run), leaves rider Move 0 before attack, and preserves its native full plan; the rider attack does not alter mount costs. This is the existing CRPG preset, not full allocation/exhaustion qualification. All three experimental flags remain false.

## Retained failures and limits

The broad historical RT auto-repeat fixture failed after 5 passing rows: forced criticals exhausted its 1 HP + 128 temporary HP target before the second rider command; its validator also assumes one rule per dispatch. That contract is superseded by native multi-attack commands. Its failure/assertions remain intact (`20260906-chunk1-unmounted-rt-A`); sustained auto-repeat redesign is DEFER — EVIDENCED for Chunk 4. The applicable replacement is the native RT sequence/Primary controls plus C02 continuity, not relaxed cardinality checks.

Stable `unmounted-attack-controls-rt` reuses the existing two controls and validators. Its first run exposed premature Dismount admission; the second passed melee but lost the Sling target before measured input. Existing native readiness and reversible rider-AI isolation resolved these fixture gates. Final controls use natural rolls, zero pre-input damage, exact stock UnitAttack, no mounted intent/routing, and exact Horse/rider AI restoration. Missing rows, wrong executor, foreign rules, invalid charge, premature admission, setup damage and unrestored AI are rejected. [Iteration record](CHUNK1-ITERATION-EVIDENCE.md) and the [journal](../MOUNTED-COMBAT-JOURNAL.md) preserve earlier evidence and rejected hypotheses.

## Restoration, evidence and next dependency

All 19 completed campaign transactions independently restored the actual intake installation, caches/settings and complete save/Mods trees. Last audit `2026-09-06T19:20:29.3863897Z`: saves `bddad0065064ebc166c8414e660b938fe9be2e4894c3b9916cdd972c8bf05913`, Mods `0e45b19883f5405a076df6a92ef5175282817516aafdc6f343dbbbcf2e10e835`; no runtime lock or game remains. Installed intake stays `0.1.0-phase3g-preview.7`, DLL/cache `6bc01b982309125963d2355983b37391f244cfea314093fe4b008ad21a241ff3`. No permanent install, main merge or release occurred.

Local evidence: `runtime-evidence/<run-id>/` and `analysis-cache/runtime-evidence/chunk1-20260906/final-qualification-index.json`. The index binds host results, artifact hashes and every restoration log; raw screenshots/traces remain local.

| Run | Primary native artifact SHA-256 |
| --- | --- |
| `20260906-chunk1-final-ordinary-tb-A` | `e77f1d17888b12e3d8b88d83e71240518288e0c47484142872a50fd2bfe91899` |
| `20260906-chunk1-final-rt-A` | `bbbac85437d3059e3ff9946e3d679f8fb7bf301f678c174ea4fd015e22d89210` |
| `20260906-chunk1-focused-unmounted-rt-C` | `82dc424410018f5ae95a8704472a4651b925629281e8032a7ccd2d46e140033a` |
| `20260906-chunk1-final-mammoth-rt-A` | `09a82a2a2aaa8a8795f96c6ee2a13f4685f1457fb7c8c78f079f9aeb8bf30f15` |
| `20260906-chunk1-final-party-A` | `76c811c5a406e420cca442aa80054437ef999643881aba205c63299b6544fb43` |

Next dependency: **Chunk 2 actor-owned allocations at native preparation/refresh boundaries**, including full movement exhaustion/conversion and both actor orders; then Chunk 3 pair-aware activations. Existing Horse Bite visual concerns remain unqualified. No scheduler/content/persistence expansion is included.

Brief human checklist for this exact preview 13: visually compare stationary ordinary attack with Primary and native Single choice; confirm pause/Stop/selection behavior and observe Horse strike/recovery. Mod-absent comparison requires its separately safe fixture. Neither is recorded as performed here.
