# Phase 3 expansion mission draft

Status: `DRAFT — NOT EXECUTION AUTHORITY`

This draft is tailored from Phase 2 evidence for a future user decision. It authorizes no implementation, branch change, public release, merge to `main`, live deployment, or scope expansion.

## Evidence inherited from Phase 2

Any future mission must preserve these settled boundaries unless contradictory new evidence is produced:

- Architecture B: rider remains player-facing principal; Mammoth remains the sole pathfinding authority.
- Runtime-only, nonserialized relationship with synchronous cleanup at save/load/area/mode/mod/lifecycle boundaries and no automatic remount.
- Exact Medium-humanoid/Mammoth presentation profile with accepted slight seat gap, stiff pose, and no saddle/reins.
- One rider-owned basic melee action and one independently Mammoth-owned primary natural action; exactly one actor, command, weapon, rule chain, and resource ledger per action.
- Mode-specific RT/TB movement and Standard accounting, exact cancellation/interruption, and non-mounted isolation.
- Suite-scoped external-state admission, Working-only save mutation, KMC-only Mods staging, restoration-before-evidence, and guarded non-force publication.
- Explicit mounted AoO and basic mounted charge are `DEFER — EVIDENCED` and default-off because installed Kingmaker exposes no narrow ownership seam compatible with Architecture B.

Historical failed runs and authorities remain immutable evidence. A new mission must not reinterpret them as qualification credit.

## Proposed objective

Evaluate whether the private alpha can expand beyond one Mammoth profile while improving presentation and player-facing integration without weakening movement ownership, action economy, lifecycle cleanup, or external-state safety. Produce separately reviewable tranches and stop before public release.

## Required authorization choices

The user should explicitly select which tranches are authorized. No tranche is implied by accepting this draft.

### A. Mammoth presentation hardening

- Author an original Mammoth-specific seated pose/animation strategy; import no Wrath assets or code.
- Reduce seat gap and stiffness, and investigate original saddle/rein presentation.
- Preserve exact transform/IK restoration and allocation/performance bounds.
- Require a new manual presentation checkpoint if the mechanism or silhouette materially changes.

### B. Player-facing UI integration

- Evaluate a native-feeling hotkey/action-bar surface without serialized blueprint/fact residue.
- Retain the transient overlay as the safe fallback.
- Prove selection, portrait, action bar, cursor, camera, feedback, disable, and uninstall behavior directly.

### C. Additional rider or mount profiles

- Treat every rider body category and mount species as an independent anatomy/rig/pathing profile.
- Repeat assembly/asset mapping, size rules, anchor/pose work, doorway/formation controls, lifecycle, RT/TB combat, and manual presentation review per profile.
- Do not generalize Mammoth offsets or pose values to horses or another anatomy.

### D. Reach generalization

- Add reach weapons or size-altering effects only after exact live weapon/radius mutation contracts are mapped.
- Prove independent rider/mount targetability, inside/outside boundaries, no global blueprint/unit mutation, and non-mounted controls.

### E. Focused mounted-AoO research

- Design an original pair-wide engagement and opportunity ownership model before code.
- Specify one attacker-selection rule, one counter/refresh ledger, threat-hand ownership, RT/TB timing, movement interaction, and cancellation/cleanup.
- Reject any solution requiring broad global engagement mutation, dual stock emission, or synthetic bypass of stock eligibility.
- Keep the feature default-off unless fresh guarded RT/TB A/B and non-mounted controls pass.

### F. Focused mounted-charge research

- Design an original pair-owned charge transaction that explicitly separates Mammoth movement from rider attack/resource ownership.
- Specify straight-path and obstacle rules, min/max distance, speed, charge buff/state owner, full action cost, interruption/cancellation, target movement, exact one-strike semantics, and RT/TB behavior.
- Do not patch or copy stock `AbilityCustomCharge` broadly.
- Keep the feature default-off unless complete guarded qualification passes.

### G. Compatibility and performance

- Build an explicit compatibility matrix for selected popular mods without adopting or mutating their content.
- Measure steady-state allocations, pose cost, command overhead, long-session cleanup, and repeated mount/dismount behavior.
- Preserve package exclusion of every foreign payload and all proprietary reference material.

## Still excluded unless separately authorized

```text
ranged mounted combat
mounted spellcasting
mounted feats or Cavalier content
enemy or AI riders
persistent mounted state
automatic remount
public release
merge to main
```

If a future mission authorizes one of these, it must define its own assembly contracts, ownership model, kill criteria, validators, runtime matrix, presentation boundary, and rollback plan. Nothing in Phase 2 qualifies it implicitly.

## Mandatory gate sequence for every authorized tranche

1. Reconcile the newest durable Phase 2 checkpoint and exact installed game/UMM assembly fingerprints.
2. Update contract, risk, action-economy, targeting, lifecycle, presentation, and runtime matrices before production code.
3. Add deterministic behavior and exact assembly/source regressions for every new seam and defect.
4. Run complete source, Release build, component, visual/source-order, harness/protocol, assembly, parser, diff, and prohibited-payload gates.
5. Commit and publish only through the guarded non-force helper.
6. Create a clean-HEAD package and sidecar manifest; bind exact package/DLL hashes and MVID.
7. Admit stable save/Mods state through one append-only suite snapshot.
8. Run separate RT and TB WhatIf gates and independent zero-mutation audits.
9. Run fresh same-package A/B processes sequentially, auditing restoration before gameplay evidence.
10. Update durable records with exact claims, failures, limitations, and the next authorized action.

## Proposed kill criteria

Stop or defer the affected tranche if evidence shows any of the following cannot be isolated:

1. two competing pathfinding agents or an enabled rider path while mounted;
2. duplicate attack, roll, damage, command, cooldown, turn, AoO, or charge chains;
3. wrong attacker, target, weapon, movement owner, or resource owner;
4. a broad patch that materially changes non-mounted behavior;
5. serialized relationship, orphaned fact/buff/part/hotbar state, or automatic remount;
6. incomplete relationship, view, pose, command, selection, save, or Mods restoration;
7. unsafe generalization across rider bodies or mount anatomies;
8. presentation regression requiring but lacking manual acceptance;
9. proprietary code/asset redistribution or incompatible licensing;
10. nonrepeatable RT/TB behavior under exact package/suite identity.

An optional profile, AoO, charge, UI, or presentation failure must be contained and may be `DEFER — EVIDENCED`; it does not invalidate the qualified Phase 2 core unless it proves an Architecture B invariant unsafe.

## Proposed deliverables

- user-approved Phase 3 mission and branch name;
- updated contract/dependency/risk/runtime matrices;
- original code and tests only;
- guarded RT/TB evidence and independent restoration audits;
- per-profile visual acceptance where required;
- private diagnostic package and playtest guide;
- explicit feature disposition and known limitations;
- no public release or `main` merge without a later, separate authorization.
