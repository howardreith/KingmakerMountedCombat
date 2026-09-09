# Chunk 4: sustained play and core safety

**IN PROGRESS â€” checkpoint at the owner's request, 2026-09-09.** Chunk 4 is unfinished. The runtime campaign is stopped, with actual preview.37 and human data restored. Branch: `codex/mounted-combat-phase3f-playable-core`; tested gameplay source remains `10c72ef56a201f51c43508ccd842917a2348078c`, `0.1.0-chunk4-preview.9`. This checkpoint changes documentation only. Prepared next diagnostics remain local and unqualified.

Reviewed baseline documentation `e6d89bff8c44ecbc21104b733703be9401c3671e` and gameplay `ec5d44e6eddc9839d273176b345f7c9701520450` retain their scope in the [completed paired milestone](PAIRED-ACTIVATION-MILESTONE.md). No preview.37 human approval is inferred. Test configuration is paired activation **true**, unified turn/scheduler/overlay **false**; one pre-combat Horse/Mammoth pair, rider-principal activation, distinct native budgets and current-weapon reach. Mid-combat mounting, cross-round Delay and conservative mode switching remain limitations. Persistence is Chunk 5; full Charge and advanced features are Chunk 6. Neither next implementation is started.

## New native evidence

**19 new transactions Aâ€“S**, distinct from retained preview.37 qualification. All original failures remain intact. Aâ€“D Charge discovery/compatibility history and earlier checkpoint details remain in [the journal](../MOUNTED-COMBAT-JOURNAL.md) and report history at `10c72ef`.

| Runs | Candidate | Native assertions | New scope and result |
|---|---|---|---|
| E/F | preview.6 `a2f89b3` | 50/0 each | Charge safety RT/TB: five cases each PASS, including genuine unmounted Charge and queued-state transition controls |
| G | preview.7 `b915102` | 39/0 | Sustained melee: adjacent/approaching, held/repeated input; three complete rider routines per condition, eligible mount routines, cadence and costs PASS |
| H/I | preview.7 | 47/2 each | Ranged continuation / premature TB terminal observation FAIL; retained discovery evidence |
| J | preview.8 `a1a6b73` | 46/0 | One vanilla unmounted mixed-range routine PASS; three bow deliveries and native rejection of the planned out-of-range bite |
| K/L | preview.8 | 47/2; 48/2 | Ranged cached/native visibility mismatch / invalid next-control TB positioning FAIL |
| M | preview.9 `10c72ef` | 49/0 | Sustained ranged RT: all four held/repeated, adjacent/approaching conditions PASS; three complete eligible routines each |
| N | preview.9 | 51/0 | Six TB cases PASS: rider first, mount first, either actor exhausted, early End and fresh subsequent activation |
| O | preview.9 | 45/2 | Native rider incapacitation reached Unconscious and cleaned the pair; fixture FAIL waiting for a retained split identity to become null during combat |
| P | preview.9 | 46/2 | Rider native heal PASS; area gate FAIL with two Horse saves and one rider save in the same update; callback attribution unresolved |
| Q | preview.9 | 47/0 | Mount native heal and hostile targeting PASS; own AC15 observed, two native rolls, health/condition preserved |
| R | preview.9 | 51/0 | Six melee interruption cases PASS: pause/resume, Stop/recovery, moving target, retarget during windup, target death before delivery and midroutine |
| S | preview.9 | 47/2 | Ranged pause/resume and Stop/recovery PASS; moving-target case FAIL after native visibility loss and one delivered shot; later ranged cases unrun |

Stable roots are `chunk4-charge-safety-{rt,tb}`, `chunk4-sustained-{melee-rt,ranged-rt,tb}`, `chunk4-ranged-native-control-rt`, `chunk4-rider-incapacitation-tb`, `chunk4-targeting-{rider,mount}-rt` and `chunk4-interrupt-{melee,ranged}-rt`. Evidence and independent receipts remain outside Git/packages at `runtime-evidence/20260908-chunk4-{A..S}` and `analysis-cache/chunk4-native`.

M recorded 12 delivered/resolved attacks in each adjacent condition and nine in each approaching condition: the native four-entry mixed-range plan ends after three eligible bow deliveries when the bite is out of reach. Held/repeated periods were approximately 6.01â€“6.16 seconds, with no accelerated cadence under the existing 0.10-second tolerance. Rider Move stayed zero. N preserved distinct budgets, including paid Horse Move `.140193909` while the rider remained exhausted at Standard/Move 6/3. R's moving target travelled `2.950625` metres; all 30 cumulative observed attacks resolved. Both target-death cases used one actual native damage effect and retained the native death result.

## Causes, changes and remaining gates

**Charge safety is demonstrated on preview.6, with final-candidate repetition still required.** Exact native blueprint `c78506dd0e14f7c45a599990e4e65038`, `AbilityCustomCharge`, was unsafely accepted in A/B. The existing pair-local policy now rejects this path before approach/start/expenditure and gives â€œCharge is not yet supported while mounted.â€ Queries, ordinary input, queue and execution revalidation preserve unrelated actors/abilities and real prior costs. Installed Call of the Wild's query replacement required restricting the final query result; no foreign mod was changed or required. Safe rejection leaves the owner's actual Charge feature request outstanding.

**Sustained ranged and TB corrections are newly verified by M/N.** The narrow ranged-tail continuation retains native timing, plans, costs and projectiles. Preview.9 uses the exact command geometry query instead of an additional cached-visibility restriction; M confirms the correction. The TB fixture uses a paid lateral move to retain legal reach for the next isolated control; no scheduler, reach or resource grant was changed.

**S is a different unresolved boundary.** Command `-2042525184` delivered once at frame7731, then received native target invalidation/Interrupt at frame7774 while the target was alive. Native command visibility was false, cached actor visibility true, and melee-tail rejection false. Rider Standard remained `3.954204`, Move zero. Do not classify this as the stationary mixed-range-tail defect or widen continuation without a matched native control. Next: distinguish legitimate visibility cancellation from a routing regression and exercise legal recovery.

**Targeting/death remain open.** Q's prepared heal independently restored the mount's three native wound points; P similarly healed the rider. Rider hostile targeting was not reached after P's area failure. Exact local area inspection shows distinct unit-entry and round actions can occur in one update; this is a possible explanation, not native attribution of P's extra save. Prepared diagnostics record actual area/callback identity and independent Reflex stats, plus an unmounted area control. O's next fixture observes the existing split identity and requires actual native encounter exit/record retirement. Actual rider death, mount-death regression and subsequent unrelated participation still need new passing evidence.

**Other mandatory gates:** remaining ranged retarget/projectile/death cases; real obstruction; independent inspection and area targeting; Horse strike/recovery capture and usable countdown/selection; normal door, blocked/narrow route, slope/turn, party movement and native area cleanup; representative RT/TB sessions and bounded repeated mount/encounter/dismount cleanup. Then run all new gates on the exact final candidate, paired three-activation loops in both initiative arrangements, final A05 and accepted A10 with relevant Horse/Mammoth regression. No missing native gate is credited by historical results.

## Identities and evidence categories

Frozen preview.9 private diagnostic package: `KingmakerMountedCombat-0.1.0-chunk4-preview.9-sustained-core-diagnostic.zip`; SHA256 `3a3a26e4655cabb792dee524445ee3f4952558bbf7e70f4b6b44085b97ce4740`. DLL SHA256 `a63e45b675acbbefdb491b4860be8aaadcd087441f8e0b65dc54e29dbbb93be6`, MVID `ad0f275c-aa80-4251-8cbb-573a3ead6870`. Suite9 SHA256 `5d4a515b3095e1a4270cf6f50e68ed1ebba6bb31c351889f5f4f801c61818142`; WhatIf purity PASS. No payload is rebuilt or installed by this documentation checkpoint.

**COMPONENT, source9:** build/source22/0, deterministic runner374/0, sustained106/0, extended271/0, core110/0, Charge86/0, harness247/0, package11/0. **ASSEMBLY CONTRACT, source9:** 490/0. **NATIVE INTEGRATION:** only the actual runs above. **HUMAN PLAY:** prior preview.13 feedback retains its scope; preview.37/current-candidate approval and physical/visual checks remain pending. Desktop capture approval timed out; no replacement automation was built.

**Unapplied local drafts:** 16 combined C# overlays compile; area/life parser226/0, obstruction82/0, traversal74/0, outer artifact dispatcher45/0 and expanded installed-assembly contracts538/0. These are offline results only. Thirty-five original source/test draft files are preserved by `analysis-cache/chunk4-native/wrap-20260909-draft-manifest.json`, SHA256 `e18d29521b164e223603a2e40a7365ae70ab86b946350cf0198b2381f5635cc7`. They are neither committed gameplay nor an installable package. [Resume instructions](../AUTONOMOUS-RESUME.md) identify the review/apply prerequisites.

## Restored state and targeted manual checklist

All **19 actual-intake restorations PASS**. S restored at `2026-09-09T07:01:06.8184302Z`; log SHA256 `b3e1ae7f20c9de1daaf874aa51bc16630369d8174fa85388a4f11c8eb55e9e1d`. Independent wrap audit at `2026-09-09T11:51:17.5916293Z` again matched full saves `bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d`, full Mods `02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878` and UMM Params `dd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e`; zero game processes and no transaction lock. Actual preview.37 DLL/cache remains `20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb`. Preview.13 is a separate backup. No permanent installation, protected-save edits or foreign-mod changes occurred.

After native gates, targeted HUMAN PLAY remains: mouse Charge feedback/recovery in both modes; held/repeated melee/bow orders with retarget/Stop; rider/mount inspection and incoming targeting usability; unobstructed seated Horse strike/recovery and native countdown; normal door/narrow route and party selection. Resume Chunk 4 when the owner requests continuation; do not begin Chunk 5 implementation.