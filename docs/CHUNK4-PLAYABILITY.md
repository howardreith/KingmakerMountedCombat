# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-13 UTC.** Thirty-two new guarded transactions restored the actual human preview.37 installation and data. Preview.18 passed47/0 native assertions for persistent rider death, unrelated turns, native encounter retirement and exact fixture restoration. Its outer validator failed because it treated an integer error counter as an array. Preview.19 corrects that schema check; fresh complete validation and the remaining Chunk4 gates are pending. No scheduler defect is established.

The reviewed documentation e6d89bff8c44ecbc21104b733703be9401c3671e and qualified gameplay ec5d44e6eddc9839d273176b345f7c9701520450 retain their scope in the [paired milestone](PAIRED-ACTIVATION-MILESTONE.md). Earlier preview.13 human feedback remains separate. No preview.37 or new-candidate human approval is inferred.

## Current new native evidence

These are actual game runs, not retained-result revalidation. All used paired activation true, unified turn/scheduler/overlay false; actual overlay absence was recorded.

| Run | Exact candidate | Native assertions | Result and limit |
|---|---|---|---|
| 20260912-chunk4-Z | preview.14 / f12e043 | 47/0 | PASS unmounted native area control after native Dismount; one entry Reflex save per actor, rider +8 and Horse +3 |
| 20260912-chunk4-AA | same | 49/0 | PASS rider native heal, area affecting both mounted actors, and hostile native attack against rider defenses |
| 20260912-chunk4-AB | same | 46/2 | FAIL complete rider-death gate at expected unrelated order; actual Dead state, live-command interruption and attachment/context cleanup observed |
| 20260913-chunk4-AC | preview.15 / 751efec | 46/2 | FAIL no-recovery expectation; both unrelated turns and actual encounter exit/zero records passed before native difficulty recovery |
| 20260913-chunk4-AD | preview.16 / 3625619 | host14/1, outer0/1 | FAIL300-second host deadline; child artifact absent. Counts cover registration/timeout, not death qualification |
| 20260913-chunk4-AE | preview.17 / 5699d39 | host45/6, child0/2, outer0/1 | FAIL fixture survivor-turn/AI/selection expectations; actual persistent rider death and native independent Horse round3 preparation observed |
| 20260913-chunk4-AF | preview.18 / 2bc7a21 | host47/0, child1/0, outer0/1 | Native persistent-death/cleanup PASS; outer FAIL due integer error-counter schema |

Z's entry totals were rider12/Horse11 against DC16. AA's entry totals were rider28/Horse22; separately identified native round callbacks produced rider26/Horse7. Each callback resolved once per eligible actor using its own Reflex stat. Both actors were inside the same actual area entity. This explains why entry and round saves can share a frame without being duplicate delivery.

AA healed three actual rider wounds to zero, left the Horse unchanged, spent the native prepared spell slot, and retained unchanged paused state for 0.3557908 seconds. The hostile native command resolved two rolls against rider AC24, bonus2, with ShieldAC/ArmorAC results and no forced hit/miss. Health remained zero wounds. Both Z and AA record the parent setup probe absent at entry and exit.

AB applied one native RuleDealDamage: requested595, actual119 at unchanged DamageToParty0.2, against rider HP103/Con14/death threshold117. Native UnitLifeController produced LifeState Dead, IsDead=true, IsFinallyDead=false; that actual policy result was preserved. The Horse's live unacted Primary became Interrupt. The rider's already spent Standard6 remained spent; native turn-finalization costs were retained. Attachment restored, private context/commands cleared, Horse remained conscious with two enabled renderers and no injury, and both actors' native grant counts stayed2. Main then prepared at frame6437. The fixture failed before observing its second unrelated turn or native encounter exit; neither is certified by this run.

AC repeated actual119 native damage and preserved Dead state through Main frame6240 and hostile frame6364, with grants2/2 unchanged. Native encounter exit cleared both actors' combat state, TB initialization, actor records and private context. At frame6367 the rider recovered from119 to92 wounds under actual TrueDeath=false. Local assembly inspection identifies the native return-to-conscious controller and its HP-minus-level health rule. This is not evidence of KMC resurrection. Persistent native rider death remains open.

AD lacked its child artifact after a host deadline. Preview.17 extends the existing30-second cleanup FAIL export to native-life roots; AE then exported the actual cause within137.0999788 seconds. Native119 damage produced rider Dead/FinallyDead at frame6522. The unrelated d79 actor prepared at6569/round2; Horse independently prepared at6784/round3 with null pair identity and grant3 from2. The old fixture incorrectly rejected that legitimate intervening turn. Horse remained conscious, visible and uninjured. Actual encounter exit was not reached.

AE restored rider AI raw/effective true with empty commands, but native final death made the rider not directly controllable. That invalidated the fixture's old context and selected-rider restoration checks. Preview.18 accepts only the exact subject with observed native damage and final-death callback, preserving all generic AI identity/raw/effective/command checks. Selection retains surviving original actors, with an explicit native Main fallback if only the dead rider was selected. It records full cyclic successor order, ordinary End inputs and exactly one independent native Prepare for an intervening survivor in a new round; subject grants cannot increase. No resurrection or gameplay cleanup change.

AF completed104.4984472 seconds. Actual119 damage produced persistent rider death, retained through Main6338 and enemy6450 in native round2 order with grants2/2 unchanged. Actual enemy defeat led to native encounter exit6455 and zero actor records/context/commands. After exact policy restoration and further simulation6478, the rider remained finally dead with119 damage. Horse stayed visible, conscious and uninjured. Both AI leases and child/parent selection restored exactly, excluding only the observed dead rider. AF's order did not interpose Horse; AE remains the observed legitimate Horse round3 preparation.

AF's native host included all three correctly named results. The outer validator's primary failure was treating observationErrors=0 as an array; its later registration-mapping error was secondary. Preview.19 requires exact integer zero for dropped/errors. The actual-shape fixture first reproduced the failure;325/0 component cases now pass, including six additional invalid-counter cases. Corrected read-only validation accepts AF's immutable child; its original outer FAIL remains retained. A fresh native run will verify the full path.
## Changes and their causes

- **Mounted Charge remains an owner-reported missing feature.** A/B reproduced unsafe acceptance of actual native Charge blueprint c78506dd0e14f7c45a599990e4e65038 / AbilityCustomCharge. The scoped policy now rejects mounted Charge before approach/start/expenditure, with player feedback, and revalidates queued execution. E/F each passed50/0 with native unmounted/unrelated controls and subsequent legal work. Full mounted Charge remains Chunk6; exact-final repetition is still required.
- **Sustained combat:** G/M/N/R/T provided new melee/ranged held-versus-repeat, three-routine cadence, mixed-range delivery, TB actor-order/exhaustion/early-End, pause/Stop, retarget, movement and target-death evidence. A reproduced ranged-tail defect received a narrow continuation repair preserving native geometry, timing, costs and released projectiles. Failed TB positioning and moving-target fixtures were corrected using paid native movement and observed native paths. Separate obstruction remains open.
- **Fixture isolation:** X proved a parent preliminary D20 override was still armed at handoff. Preview.13 retires it before child scenarios; Y/Z/AA/AB newly prove absence. Earlier Chunk4 Horse-roll evidence retains this limitation and cannot replace final clean qualification. The accepted historical paired milestone is not reopened.
- **Incoming fixtures:** missing exact registration, missing native Dismount, an incorrect mounted-only readiness argument, and type/Type diagnostic JSON collision were separately reproduced and repaired. No incoming gameplay targeting policy was changed.
- **Death fixtures:** preview.15 fixed AB's roster-head assumption using the actual cyclic native order. AC passed that boundary. Preview.16 temporarily selects native true death without Death's Door for the persistent rider-death case, preserves/restores exact setting caches and saved values, and changes no native life flags or gameplay cleanup. Nonfinal cases retain their native policy and require one correctly timed recovery from the exact native controller, the native health result, zero pair ownership and subsequent simulation after policy restoration. The validator also rejects premature/duplicate recovery, false finality and changed settings.

Earlier results and package checkpoints remain in [the journal](../MOUNTED-COMBAT-JOURNAL.md) and Git history. Evidence directories are outside Git/packages: runtime-evidence/20260908-chunk4-A..S, 20260909-chunk4-T..X, 20260912-chunk4-Y..AB and 20260913-chunk4-AC..AF. Original failed artifacts remain failures.

## Identities and evidence categories

Branch: codex/mounted-combat-phase3f-playable-core. AF source: 2bc7a2144d4dc91a54cf336e5595dd90286bf6f2, product0.1.0-chunk4-preview.18. AE source: 5699d391ccaa7ae66005cf6a9f1c9a3ede956d93. AD source: 36256190ec1a49568451a63fccbcd123a4a901c6. AC source: 751efecc374e97961403f6053deeaf9808db82cd. Z/AA/AB source: f12e0433c51190960b77def625c7cd31b4c196ca.

| Item | Identity |
|---|---|
| Private package18 | KingmakerMountedCombat-0.1.0-chunk4-preview.18-native-survivor-order-diagnostic.zip |
| ZIP / manifest SHA256 | f34c2d26f8d49452b9a256e4cd76e4ffe9460b830de3cf12a947af0ff16191ae / 8fcac75d4887e5366adbdbd0d46b3d7a47b5ee198841dd50bcd22ab56323167f |
| DLL SHA256 / MVID | e9b77511efbe9222deb5bffbe9fba449ec92174e9b406b970022a22c2061e1cb / ffcc512c-3611-411b-b177-8c7bc9cdcc81 |
| Suite18 SHA256 | df90661f90f5e6b3c385131c799a845e8df82b447289ee79761194986a53a575 |

COMPONENT: full preview.18 checks PASS exit0: real components376/0, metadata2/0, harness247/0, core319/0, play106/0, extended277/0, obstruction82/0, traversal74/0, outer45/0, Charge86/0; build/source22/0 and package11/0. Log tests18.txt SHA2564a57e21da606c744ac559ed2aa03392e3b34db0018e3e51309316e6016d2ae3c. Preview.19 build passes; affected core325/0, obstruction82/0 and outer45/0. Full preview.19 checks PASS exit0: components376/0, metadata2/0, harness247/0, core325/0, play106/0, extended277/0, obstruction82/0, traversal74/0, outer45/0, Charge86/0; Kingmaker546/0, read-only Wrath24/0, detached construction30/0 plus observers. Log tests19.txt SHA256f7278ed9c2a7399710b37a410573eba1596509405d0b4cd0b5075fcb1fcbc8db. Parser envelopes never certify gameplay. Earlier check histories remain in the journal/Git.
ASSEMBLY CONTRACT: Kingmaker538/0, read-only Wrath24/0 and detached patch construction30/0 plus observers on source14. Exact Kingmaker SHA2563b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, MVID07fa1e4d-8618-41b3-9b8d-faa17d3b26f7; .NET4.7/C#7.3/Harmony12. Native ordering: SortedUnits06000BC7, ChooseNextUnit06000BD2, RemoveUnit06000BE6, HandleUnitDeath06000BF2. Contracts do not substitute for native execution.

New recovery identities: TrueDeath06000CF9 inverses DeadCompanionsRiseAfterCombat04007CB5; DeathDoorCondition06000CF8 reads DeathDoor04007CB4. UnitReturnToConsciousController.Tick0600918E skips finally-dead actors; MakeUnitConscious06009191 applies native recovery then ForceUnitConscious06009163/SetLifeState06009164. Only local signatures and original summaries are committed.

NATIVE INTEGRATION: new results and their exact scope above. Outstanding: fresh complete persistent rider-death validation with the corrected counter schema, incapacitation retry, mount-death regression, mount targeting on final candidate, inspection, Horse strike/countdown, obstruction/door/narrow route/slope/turn/party/area boundary, RT/TB encounter cycles and bounded state/subscription/log cleanup. Then run all new gates on the exact final candidate, followed by both paired full-round orders, final A05 and accepted A10 with Horse/Mammoth.

HUMAN PLAY: pending. Prior native PNGs inspected were black. The existing Windows tool located the guarded Kingmaker window on September12, but game-state capture's app-approval request timed out; no image or app input resulted. No replacement desktop automation was built, and no animation handle/renderer count is visual approval.

## Restoration and manual checklist

All32 transactions have independent restoration receipts, latest AF at2026-09-13T04:30:39.9067297Z, no game/lock. Full saves SHA256bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d; full Mods02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878; UMM Paramsdd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e. Human preview.37 DLL/cache remain20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb. Protected campaigns, KMC_AUTOMATION_BASELINE and the separate historical preview.13 backup are preserved. No permanent candidate installation, foreign-mod change, main merge or public release occurred.

Targeted manual checks, through a guarded disposable session:

1. Compare unobstructed mounted/unmounted Horse strike and recovery; check seated motion and native countdown/feedback.
2. Inspect/select rider and mount separately; verify visible health, targeting and ordinary command feedback.
3. In RT/TB, check Charge rejection, then legal move/attack and End Turn using physical controls.
4. Play a representative encounter and traverse a door, narrow obstruction, slope/turn and party route.

One pre-combat Horse/Mammoth pair, rider-principal activation, distinct native budgets, weapon reach, conditions, cancellation, collision and in-flight projectiles remain the contract. Transport alone adds no rider Move tax. Mid-combat mounting, cross-round Delay and conservative mode-switch limits remain explicit. Chunk5 persistence is the next roadmap item only; no persistence or Chunk6 implementation has begun.
