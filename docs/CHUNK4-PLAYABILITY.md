# Chunk 4: sustained play and core safety

**IN PROGRESS - 2026-09-13 UTC.** Twenty-eight new guarded transactions have completed and restored the actual human preview.37 installation and data. New targeting controls pass on preview.14. Native rider death was reached, but its full cleanup/turn-progression gate remains open because the fixture used the wrong expected turn order. Preview.15 corrects that fixture and strengthens evidence validation; it has no native qualification yet.

The reviewed documentation e6d89bff8c44ecbc21104b733703be9401c3671e and qualified gameplay ec5d44e6eddc9839d273176b345f7c9701520450 retain their scope in the [paired milestone](PAIRED-ACTIVATION-MILESTONE.md). Earlier preview.13 human feedback remains separate. No preview.37 or new-candidate human approval is inferred.

## Current new native evidence

These are actual game runs, not retained-result revalidation. All used paired activation true, unified turn/scheduler/overlay false; actual overlay absence was recorded.

| Run | Exact candidate | Native assertions | Result and limit |
|---|---|---|---|
| 20260912-chunk4-Z | preview.14 / f12e043 | 47/0 | PASS unmounted native area control after native Dismount; one entry Reflex save per actor, rider +8 and Horse +3 |
| 20260912-chunk4-AA | same | 49/0 | PASS rider native heal, area affecting both mounted actors, and hostile native attack against rider defenses |
| 20260912-chunk4-AB | same | 46/2 | FAIL complete rider-death gate at expected unrelated order; actual Dead state, live-command interruption and attachment/context cleanup observed |

Z's entry totals were rider12/Horse11 against DC16. AA's entry totals were rider28/Horse22; separately identified native round callbacks produced rider26/Horse7. Each callback resolved once per eligible actor using its own Reflex stat. Both actors were inside the same actual area entity. This explains why entry and round saves can share a frame without being duplicate delivery.

AA healed three actual rider wounds to zero, left the Horse unchanged, spent the native prepared spell slot, and retained unchanged paused state for 0.3557908 seconds. The hostile native command resolved two rolls against rider AC24, bonus2, with ShieldAC/ArmorAC results and no forced hit/miss. Health remained zero wounds. Both Z and AA record the parent setup probe absent at entry and exit.

AB applied one native RuleDealDamage: requested595, actual119 at unchanged DamageToParty0.2, against rider HP103/Con14/death threshold117. Native UnitLifeController produced LifeState Dead, IsDead=true, IsFinallyDead=false; that actual policy result was preserved. The Horse's live unacted Primary became Interrupt. The rider's already spent Standard6 remained spent; native turn-finalization costs were retained. Attachment restored, private context/commands cleared, Horse remained conscious with two enabled renderers and no injury, and both actors' native grant counts stayed2. Main then prepared at frame6437. The fixture failed before observing its second unrelated turn or native encounter exit; neither is certified by this run.

## Changes and their causes

- **Mounted Charge remains an owner-reported missing feature.** A/B reproduced unsafe acceptance of actual native Charge blueprint c78506dd0e14f7c45a599990e4e65038 / AbilityCustomCharge. The scoped policy now rejects mounted Charge before approach/start/expenditure, with player feedback, and revalidates queued execution. E/F each passed50/0 with native unmounted/unrelated controls and subsequent legal work. Full mounted Charge remains Chunk6; exact-final repetition is still required.
- **Sustained combat:** G/M/N/R/T provided new melee/ranged held-versus-repeat, three-routine cadence, mixed-range delivery, TB actor-order/exhaustion/early-End, pause/Stop, retarget, movement and target-death evidence. A reproduced ranged-tail defect received a narrow continuation repair preserving native geometry, timing, costs and released projectiles. Failed TB positioning and moving-target fixtures were corrected using paid native movement and observed native paths. Separate obstruction remains open.
- **Fixture isolation:** X proved a parent preliminary D20 override was still armed at handoff. Preview.13 retires it before child scenarios; Y/Z/AA/AB newly prove absence. Earlier Chunk4 Horse-roll evidence retains this limitation and cannot replace final clean qualification. The accepted historical paired milestone is not reopened.
- **Incoming fixtures:** missing exact registration, missing native Dismount, an incorrect mounted-only readiness argument, and type/Type diagnostic JSON collision were separately reproduced and repaired. No incoming gameplay targeting policy was changed.
- **Current preview.15 repair:** native ChooseNextUnit advances after the current actor and wraps the roster. AB's fixture instead expected the roster head. It now records the eligible native roster and principal index before damage, derives the cyclic unrelated order, and keeps the same two-turn, no-extra-grant, real-death and native-encounter-exit assertions. The artifact validator independently checks that derivation. No scheduler behavior changes.

Earlier A-Y results, failed hypotheses and package checkpoints remain in [the journal](../MOUNTED-COMBAT-JOURNAL.md) and Git history. Evidence directories are outside Git/packages: runtime-evidence/20260908-chunk4-A..S, 20260909-chunk4-T..X, and 20260912-chunk4-Y..AB. Original failed artifacts are never rewritten into passes.

## Identities and evidence categories

Branch: codex/mounted-combat-phase3f-playable-core. Z/AA/AB source: f12e0433c51190960b77def625c7cd31b4c196ca, product0.1.0-chunk4-preview.14.

| Item | Identity |
|---|---|
| Private package14 | KingmakerMountedCombat-0.1.0-chunk4-preview.14-native-area-readiness-diagnostic.zip |
| ZIP / manifest SHA256 | 8c3a8a5b4358b94fa21fcb26839cdac6beab6ebfd0d99b8f61d1797a0e51452d / 9f5d6fd160d1db11b7526e5777f0285d5999ea9ba388de9557a985c13a9145b5 |
| DLL SHA256 / MVID | 38f0fa73b73eed2249397b0c7cc231956c3c88b85130727065696cd29d94928d / e582268d-445b-462a-a05d-17ea77af5c33 |
| Suite14 SHA256 | 70fcb4454b750d4e370c3d71c02f85b3a0a49655de24f1928d7e82c114cdd1e2 |

COMPONENT: source14 real-service components376/0, native metadata2/0, harness247/0, core protocol226/0, play106/0, extended277/0, traversal74/0, obstruction82/0, outer artifact45/0 and Charge86/0. Build/source22/0 and package11/0 pass. Preview.15 build passes; its new parser regression first reproduced acceptance of a false order, then passed238/0. Full preview.15 checks also PASS (tests15.txt SHA2563b28996ab9f8ede9ddbcf0aeccaf09b30f3598ab4d40ba7b0b93bb5a64bb13de); other counts are unchanged. Packaging and fresh native qualification follow.

ASSEMBLY CONTRACT: Kingmaker538/0, read-only Wrath24/0 and detached patch construction30/0 plus observers on source14. Exact Kingmaker SHA2563b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, MVID07fa1e4d-8618-41b3-9b8d-faa17d3b26f7; .NET4.7/C#7.3/Harmony12. Native ordering: SortedUnits06000BC7, ChooseNextUnit06000BD2, RemoveUnit06000BE6, HandleUnitDeath06000BF2. Contracts do not substitute for native execution.

NATIVE INTEGRATION: new results and their exact scope above. Outstanding: full rider-death progression/encounter exit, incapacitation retry, mount-death regression, mount targeting on final candidate, inspection, Horse strike/countdown, obstruction/door/narrow route/slope/turn/party/area boundary, RT/TB encounter cycles and bounded state/subscription/log cleanup. Then run all new gates on the exact final candidate, followed by both paired full-round orders, final A05 and accepted A10 with Horse/Mammoth.

HUMAN PLAY: pending. Prior native PNGs inspected were black. The existing Windows tool located the guarded Kingmaker window on September12, but game-state capture's app-approval request timed out; no image or app input resulted. No replacement desktop automation was built, and no animation handle/renderer count is visual approval.

## Restoration and manual checklist

All28 transactions have independent restoration receipts. AB's guarded completion was September12; its delayed audit on September13 at00:43:12.4964978Z independently matched actual intake, with no game/lock. Full saves SHA256bc345a41d72f5c1538c9a5dcefc577202b0eb8279adbf1fa958cd63fb643519d; full Mods02aa64faba191ca51d80c2c35f521f2d6ef65c77911f86b3fb0e144a1d6c7878; UMM Paramsdd22dc5aad012f0bca721de37d888a9163e9e759c44f7fa705e3355302be7e1e. Human preview.37 DLL/cache remain20080fdcf83c7628611c3f6354a3e47c3065e2b25998a9c01b39a69796ed57bb. Protected campaigns, KMC_AUTOMATION_BASELINE and the separate historical preview.13 backup are preserved. No permanent candidate installation, foreign-mod change, main merge or public release occurred.

Targeted manual checks, through a guarded disposable session:

1. Compare unobstructed mounted/unmounted Horse strike and recovery; check seated motion and native countdown/feedback.
2. Inspect/select rider and mount separately; verify visible health, targeting and ordinary command feedback.
3. In RT/TB, check Charge rejection, then legal move/attack and End Turn using physical controls.
4. Play a representative encounter and traverse a door, narrow obstruction, slope/turn and party route.

One pre-combat Horse/Mammoth pair, rider-principal activation, distinct native budgets, weapon reach, conditions, cancellation, collision and in-flight projectiles remain the contract. Transport alone adds no rider Move tax. Mid-combat mounting, cross-round Delay and conservative mode-switch limits remain explicit. Chunk5 persistence is the next roadmap item only; no persistence or Chunk6 implementation has begun.