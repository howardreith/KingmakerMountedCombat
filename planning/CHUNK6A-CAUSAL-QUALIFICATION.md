# Chunk 6A causal qualification repair

Status: IN PROGRESS. Owner instruction received 2026-09-26.

Intake branch: codex/mounted-combat-phase3f-playable-core.
Intake HEAD: 14966a7c65921797395ecbc4a5b7f9c0c631f991, a clean descendant of
requested 0a92076da39515b40203ff7b8f7dfc9ef311f5af. Preserve all descendants.

The positive approach combined geometry from before a compensated Mount with
the successful transition of a later Mount. It does not qualify CM02-approach-arrival.
The acted boundary must be observed for the exact command, never inferred from
cooldown endpoints. All existing run artifacts remain unchanged.

Required order: split positive and compensation RT/TB; exact rider selection before
baseline; immutable command/shell/process/context identity; native callback resource
windows; separate exploration Mount/Dismount; retain failures and correct unsupported
ledger claims; focused regressions and complete offline umbrella; guarded publication;
new immutable package and unchanged fresh purity proof; positive approach; isolated
compensation RT/TB; geometry change; obstruction; full RT and fresh TB; every mandatory
6A row on one frozen candidate. No 6B before that gate, no PR/merge/tag/release/permanent
install/HUMAN PLAY acceptance.

At intake no Kingmaker or project proof process was running. Locate and preserve
prior purity receipts; do not infer a proof result from absence of a process.

## 2026-09-27 reaction-resource observation contract

Pinned Kingmaker Assembly-CSharp SHA-256 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, MVID 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7. Bounded local inspections are in analysis-cache/chunk6a-causal (UnitCombatState, UnitCombatCooldownsController and TurnController); no proprietary implementation is committed.

- Discrete allowance: UnitCombatState.AttackOfOpportunityCount, getter/setter 0x06009377/0x06009378. It does not decay with time. Native AttackOfOpportunity 0x060093A1 can consume one; on the first reaction of an allocation it sets Cooldowns.AttackOfOpportunity to approximately 5.4 seconds. The conservation windows declare no permitted opportunity consumption, including consumption later hidden by a refresh.
- AoO cooldown: Cooldowns.AttackOfOpportunity, getter/setter 0x0600C3BC/0x0600C3BD. UnitCombatCooldownsController.TickOnUnit 0x0600934A owns normal RT decay. If waiting for initiative, the tick decreases only the initiative cooldown. Otherwise it decreases the AoO cooldown and, at zero, restores the allowance to AttackOfOpportunityPerRound only when the maximum is positive and the old allowance is no larger than that maximum.
- TB tick branch 0x06009349: active combat ticks do nothing except during native passing time. Passing first consumes a nonsurprised unit's positive initiative delay, then decays the other timers with the remaining delta. It does not replenish the discrete reaction allowance. Out-of-combat units take the RT branch only while passing.
- Initiative ordering is UnitCombatState.Initiative (integer getter 0x06009379), distinct from Cooldowns.Initiative (float getter 0x0600C3B4). Ordering must remain exact throughout every measured window. A cooldown change requires the observed native tick or declared preparation; elapsed wall time alone is insufficient.
- Cooldowns.Clear 0x0600C3BE clears both cooldowns but leaves the discrete allowance and ordering intact. TurnController.Prepare 0x06000C3C performs that clear, then conditionally restores the discrete allowance using the same positive-maximum/old-allowance condition. Only the declared pending partner preparation is permitted; no rider preparation is allowed.

Schema 30 retains the accepted command, selection and exploration structure. The allocation trace observes exact tick and opportunity entry/exit only during a command window. Producer and external validator replay ordered native event pairs and command-boundary samples for both actors. Missing callback ends, unexplained intermediate changes, consumption/refund/refresh without an allowed event, changed ordering and undeclared clears/preparations fail. Native event clock/phase metadata is evidence, never authority to write a resource. No production resource write was added.


## 2026-09-27 - released diagnostic observer

IN PROGRESS: c6a-reaction-a-approach on preview.111 reached cleanup, then its liveness sampler dereferenced the released allocation trace and prevented exact command evidence export. All external restoration PASS; no native qualification credited. Preserve its unchanged purity PASS and overall native FAIL. Preview.112 adds a cleanup guard and first-exception evidence, with source and compiled-entry regressions. Exact identities and assertions: [Chunk 6A report](../docs/CHUNK6A-COMBAT-MOUNT.md). Fresh immutable candidate/proof is required before another positive run; no inference of callback qualification from the UMM log.
