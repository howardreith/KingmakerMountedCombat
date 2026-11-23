# Mounted Shield — supplemental pre-native review record

The repository's independent review of the coherent change, run by the route in
[docs/missions/mounted-shield/PRE_NATIVE_REVIEW.md](../missions/mounted-shield/PRE_NATIVE_REVIEW.md). It is supplemental
code/safety evidence only: not an accepted factory round, not native evidence, not merge or release approval. Raw
prompts, event streams and results are kept privately in the lab at
`analysis-cache/mounted-shield-pre-native-<sha8>/` (not committed).

## Round 1 — candidate `1c49cb204967e113d0890df4d2a9617018f6eefb`

- **Transport:** `codex-cli 0.170.0`, `-a never exec --ignore-user-config -m gpt-6.2-astra -c model_reasoning_effort="max"
  -c windows.sandbox="elevated" --sandbox read-only`, the project's nine disabled features, prompt on stdin, fresh detached
  snapshot `worktrees/mounted-shield-review-1c49cb20`. Started 2026-11-23T17:12:48Z, 14.63 minutes, exit 0, empty stderr;
  candidate and snapshot identity and cleanliness rechecked afterwards: PASS.
- **Verdict: CHANGES REQUIRED.** The reviewer confirmed the build/gate claims independently (DLL `ae4f8a92…`, MVID
  `5fd8fafa…`, the single embedded icon `fd519874…`) and found no runner, fixture, backup or restoration change.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN1-F1 | A write whose stat-change callback threw leaves the modifier in the mount's AC without `AppliedTo`; the sweep's `Modifier.Remove()` returns false yet counted it; `ReleaseAll` failures did not stop disable/dispose. | **Fixed** in `16a4cc56`: removal goes through the owning stat by exact reference and is verified (`RemoveExact`, `IdentityModifiers`, membership via `Holds`); a failed write is recovered by identity at once; `ReleaseAll` is true only when no Mounted Shield modifier remains on a loaded unit; `Disable` re-enables on failure and the root refuses disable, dispose and update-failure cleanup while one remains. Reproduced and verified on a genuine `ModifiableValue` with a throwing callback (native contract 72/0). |
| PN1-F2 | The mount's value followed equipment changes only at the next frame, so an attack in between could read the previous value. | **Fixed** in `16a4cc56`: the rider's pair is reconciled in the mount's `RuleCalculateAC` about-to-trigger (before the attack reads AC) and on `IUnitEquipmentHandler` / `IUnitActiveEquipmentSetHandler` events, through the ledger's single-rider `ReconcileRider`, behind the same gates. Component case added; native observation remains BLOCKED. |
| PN1-F3 | An in-place edit removing Mounted Shield from a required list was missed by the per-frame reference check, so a level-up could offer it before the first combat use withdrew it. | **Fixed** in `16a4cc56`: the per-frame refresh is the full membership comparison (two short scans), which withdraws within a frame; Mounted Archery benefits too. Component case added. |
| PN1-F4 | Disable left KMC feats in a foreign-changed list while their services were off. | **Fixed** in `16a4cc56`: a foreign-changed array loses only KMC's own entries (foreign entries and order kept) and the drift is still reported. The existing Mounted Archery test that expected "overwrites none" now expects this narrow removal. |
| PN1-F5 | Duplicate detection checked only the ordinary list; a fighter-only competitor would sit beside KMC's feat. | **Fixed** in `16a4cc56`: `KmcCombatFeatCollision.FindCompeting` over every publication target; a competitor withdraws only Mounted Shield. Component case added. |

Native-plan note from the reviewer, kept for the future driver: the existing Sprint 6 forced-roll helper computes its
"equal" d20 from general Mobility only, so with a +3 shield its equality row would beat the attack by three. A Mounted
Shield driver must target and record the final native check total, and as compiled diagnostic code it needs a new DLL
identity and another supplemental review.

## Round 2 — candidate `70ac70c0f004caf5ed9d99fa3826bd6c6cb629b8` (production `16a4cc56`)

- **Transport:** as round 1, fresh snapshot `worktrees/mounted-shield-review-70ac70c0`, prior findings supplied. Started
  2026-11-23T17:35:44Z, 15.00 minutes, exit 0, empty stderr; identity and cleanliness recheck PASS.
- **Verdict: CHANGES REQUIRED.** Prior findings: PN1-F2 and PN1-F5 resolved; PN1-F1, PN1-F3 and PN1-F4 partially
  resolved, their remainders reported as PN2-F1, PN2-F3, PN2-F4 and PN2-F5. The reviewer re-verified the gate log hash,
  the DLL (`30bf64ab…`, MVID `a0761478…`) and the single icon resource.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN2-F1 | A failed shield release during update-failure cleanup threw without keeping the cleanup pending, so the shutdown was never completed. | **Fixed** in `1f1bb814`: `failureCleanupPending` is set before the throw; `Update` retries the cleanup and the shutdown completes only after a verified release. Root path verified by inspection (not hostable offline). |
| PN2-F2 | The ordinary `Update(true)` pass lacked the owned-load guard. | **Fixed** in `1f1bb814`: both passes are skipped while an archive worker runs or an owned load replaces the world. Inspection. |
| PN2-F3 | An ordinary feat pick could be committed between two frame scans after another mod removed Mounted Shield from the fighter list. | **Fixed** in `1f1bb814`: a hidden native `Prerequisite` (`MountedShieldAvailabilityPrerequisite`, Group All, `HideInUI`) answers the publication's full availability at `BlueprintFeature.MeetsPrerequisites`; the engine re-checks owned features only for stat prerequisites, so owners are never affected. Native contract exercises its check (74/0); the level-up UI itself remains BLOCKED. |
| PN2-F4 | A refused enable (Ranger drift) left the combat feats published with the services off; a refused disable the inverse. | **Fixed** in `1f1bb814`: on a refused change `SetSelectionEnabled` returns the KMC combat-feat publication to the state the root keeps. Inspection (root not hostable offline). |
| PN2-F5 | After disable stripped KMC's entries from a foreign-appended list, re-enable rejected it and withdrew both optional rules. | **Fixed** in `1f1bb814`: the stripped foreign value becomes that list's lease original (`Rebase`); re-enable appends KMC's entries after it and disabling again restores it exactly. Component case covers the full round trip; the Mounted Archery lifecycle case was updated to this behavior. |
| PN2-F6 | A copied second reference to an active KMC feat survived refresh and would be offered twice. | **Fixed** in `1f1bb814`: the full refresh keeps one occurrence of each KMC entry per list; foreign entries, including foreign duplicates, are untouched. Component case. |

## Round 3 — candidate `54de0408cb510c5ec84f9f2b4f8eb0e866701c9a` (production `1f1bb814`)

- **Transport:** as before, fresh snapshot `worktrees/mounted-shield-review-54de0408`, both prior rounds supplied. Started
  2026-11-23T17:56:08Z, 18.90 minutes, exit 0, empty stderr; identity and cleanliness recheck PASS.
- **Verdict: CHANGES REQUIRED.** Prior findings: PN1-F2, PN1-F5, PN2-F5 and PN2-F6 resolved; PN2-F3 resolved for the stock
  ordinary and fighter picks; the remainders reported as PN3 findings. One optional improvement (duplicate-scan
  allocation). The reviewer re-verified the gate log, the DLL (`8c40a66c…`, MVID `2c971267…`) and the icon resource.
- **Bound:** this review was bounded at three rounds. The round-3 corrections below were made and gated
  (`9bbd6112`) but **have not had a further supplemental review**.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN3-F1 | Failure cleanup and its retry could touch the outgoing world during an owned load; the second `failureCleanupPending` store broke the existing compiled contract (exactly one store). | **Fixed** in `9bbd6112`: the Mounted Shield stop is skipped while `LoadInFlight` and joins the one existing latch store; the retry also waits for the load. `Test-ContinuousDriveContracts.ps1` 59/0 (it failed on `1f1bb814`; now a focused child). |
| PN3-F2 | A successful failure shutdown left Mounted Shield enabled and selectable while the root blocked AC writes. | **Fixed** in `9bbd6112`: the stop calls `Disable` (both benefits off, verified release); the selection check requires the services on and the service enabled. Inspection; root not hostable offline. |
| PN3-F3 | A refused repeated disable republished the combat feats and re-enabled Mounted Shield while the services stayed off. | **Fixed** in `9bbd6112`: `SetSelectionEnabled` restores the publication state from before the call; the root re-enables the service only if it was enabled before. Inspection; Ranger selection contract 183/0. |
| PN3-F4 | The new equipment EventBus subscriptions changed the reviewed global-subscriber inventory (the replay-subscriber contract rejected the compiled type). | **Fixed** in `9bbd6112` by removing those two subscriptions (no protocol revision): the `RuleCalculateAC` rulebook handler still reconciles before an attack reads the mount's AC, and the sheet follows the per-frame reconcile. `chunk7\Test-Chunk7ReplaySubscriberContract.ps1` 21/0 (now a focused child). |
| PN3-F5 | A foreign selection (or a parent) with `IgnorePrerequisites` skips the hidden availability check, so between two frame refreshes a pick on such a list could still be committed after a required list lost the feat. | **Open (not fixed).** Stock targets do not set the flag (the reviewer confirmed); the case needs another mod's prerequisite-ignoring selection offering Power Attack and Dodge plus another mod's removal from a required list within one frame. The per-frame full refresh withdraws the feat on the next frame, and its benefits are then off for every owner. Recorded for the owner. |
| PN3-F6 | A locale pack with different text for a Mounted Shield key faulted the shared registration. | **Fixed** in `9bbd6112`: a pack's existing `KMC.MountedShield.*` text is kept (registration and locale replacement). Inspection. |
| PN3-F7 | Successful unload left the static selection check naming the disposed service. | **Fixed** in `9bbd6112`: successful teardown and feature removal release it when this service owns it; a refused teardown throws first and keeps it. Inspection. |
| Optional | The duplicate scan allocated every frame. | **Done** in `9bbd6112`: a copy is made only when a duplicate exists. |

Two contract scripts the reviewer named (`Test-ContinuousDriveContracts.ps1`, the chunk7 replay-subscriber contract) were not
in the first focused gate; both failed on `1f1bb814` and pass on `9bbd6112`. Six other scripts that read changed code fail
identically on base `fb743911` (HANDOFF section 4).

## Continuation (owner amendment 1) — dispositions reopened before round 4

Recorded 2026-11-23 under owner amendments revision 1 (sha256 `8ee86965…`), on production `cb74ad58` (companion UMM
baseline `5be0f737` imported by fast-forward). The three rounds above were not an owner-imposed cap; the review continues.

| ID | Earlier disposition | Current disposition |
| --- | --- | --- |
| PN3-F3 | "Fixed in `9bbd6112`" (publication restored to the pre-call state). | **Reopened, then fixed in `cb74ad58`.** The earlier fix restored the publication but not the stored `selectionDesired`, so a second refused disable withdrew the feats while the root stayed enabled, and a second refused enable published them while it stayed disabled (the owner's report). A refused call now restores `selectionDesired` and re-applies every surface the call changed (Riding Dog lists, the three-feat publication, Divine Bond slots); a service no longer `Registered` refuses without keeping the requested state. Reproduced through the compiled `SetSelectionEnabled` on genuine selections: `scripts/Test-MountedShieldSelectionRollback.ps1` fails 8 rows against the previous rollback (DLL `8600fb34…`, log `analysis-cache/mounted-shield-continuation/rollback-repro-old-rollback-8600fb34.log`) and passes 44/0 on `cb74ad58`, for three feats and for Mounted Archery absent, with foreign entries and the other mod's Ranger change preserved. To be reviewed in round 4. |
| PN3-F5 | "Open (not fixed)". | **Rebutted as a blocking violation; recorded as an optional compatibility limitation.** Installed 2.1.7b `BlueprintFeatureSelection.CanSelect` (IL read 2026-11-23) first requires `AllFeatures.Contains(item.Feature)`, then skips `MeetsPrerequisites` only when `FeatureSelectionState.IgnorePrerequisites`. KMC's withdrawal removes its own Mounted Shield entry from every list that holds it, so after withdrawal no selection, prerequisite-ignoring or not, can offer or commit the feat. The withdrawal runs at the per-frame full refresh and whenever the hidden prerequisite is asked. The window is therefore one frame after another mod removes Mounted Shield from a required list at runtime, and only for a foreign selection that ignores all prerequisites (Mounted Combat and Shield Focus included, by its own design); no stock publication target sets the flag (round 3). A pick in that window yields a feat whose benefits stay off for every owner (both benefits require availability), with nothing written to a mount. Closing the window would need a broad interception of native selection (`CanSelect`/`MeetsPrerequisites`), which the owner asked to avoid without a supported reproducible path, and none exists. |
| Round 3 note | The planned forced-roll helper. | **Addressed in `cb74ad58`:** `MountedCombatDefenseService.ScenarioCheckTotal` takes the final native total and `ControlledSkillCheck` chooses the die inside the native roll after `StatValue` and every `Bonus`. `Test-MountedShieldNativeContract.ps1` (77/0) runs the production class on a genuine `RuleSkillCheck`: the planned equality with +3 lands on the attack result (die 9), and a Mobility-only die (12) would have landed three above. |

New compiled diagnostic code in `cb74ad58` (the gameplay driver `Sprint6ShieldScenario.cs`, the persistence case
`Chunk7PersistenceScenario.MountedShield.cs`, the registries and readers) and the imported UMM baseline are in the scope of
round 4. No game launch, staging, installation or runner/fixture/backup/restoration change is part of this candidate.

## Round 4 — candidate `f4d0078ac769ed0bfb8a6128b39d39bd2bb47b73` (production `cb74ad58`)

- **Transport:** `codex-cli 0.170.0`, the same arguments, fresh snapshot `worktrees/mounted-shield-review-f4d0078a`, prompt
  rendered from the packet template with the owner amendment, rounds 1–3 and the continuation table supplied. Started
  2026-11-23T20:39:28Z, 17.25 minutes, exit 0, empty stderr; identity and cleanliness recheck PASS.
- **Verdict: CHANGES REQUIRED.** The reviewer re-verified the gate receipt, DLL `1a80bc57…`, MVID `d7587812…`, package
  `ba7f4e4d…` and the single 64×64 icon resource `fd519874…`. Prior findings: PN1-F2, PN1-F5, PN2-F6, PN3-F4, PN3-F6, PN3-F7
  resolved; PN3-F1 open at initial dispatch (PN4-F1); PN3-F2's selection remainder and the PN3-F5 bypass for a stopped
  service open (PN4-F3); PN3-F3 complete rollback open (PN4-F2). The PN3-F5 rebuttal was accepted for the between-frame
  case.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN4-F1 | An update failure during an owned load dismounted pairs (and released Mounted Shield contributions) in the outgoing world before the load guard. | **Fixed** in `bb0c53d5`: the first dispatch latches when an owned save **or load** is in flight (the same single latch store); the retry already waits for the load. Compiled order checked in `Test-MountedShieldNativeContract.ps1` (load read and latch before `PerformFailureCleanup`). The root is not hostable offline. |
| PN4-F2 | A refused toggle could set a session-long withdrawal (foreign change on a required list plus Ranger drift), losing both optional feats at the next enable. | **Fixed** in `bb0c53d5`: `SetSelectionEnabled` asks the deciding Ranger lease first; a refusal changes no surface and no publication state. `Test-MountedShieldSelectionRollback.ps1` drifts a required combat list and the Ranger list: two refused enables write neither list and leave no withdrawal; the next enable publishes all three feats. |
| PN4-F3 | After a terminal stop the feat stayed in the lists, so a prerequisite-ignoring selection could still take a stopped feat. | **Fixed** in `bb0c53d5`: the publication set gains a reversible suspension (excluded from offers and from required-path watching, removed from every list, never a withdrawal); the terminal stop suspends Mounted Shield after its benefits stop, and only an explicit enable resumes it. Mounted Combat and Mounted Archery are untouched. Component case (1508/0), the compiled transition on genuine selections with the native `CanSelect` refusing the suspended feat at its membership test, and the compiled order in the root. |
| PN4-F4 | Equipment cleanup could report success with a two-handed original displaced by the native slot; the reader accepted a missing drain. | **Fixed** in `bb0c53d5`: every hand slot of every set is captured before any change, created shields are verified out of every slot and collection, every original is verified back in its slot and the weapon set restored, failures recorded; the reader requires drain evidence for each actor with no error and no setup-modifier fault. |
| PN4-F5 | Back reused selection states from a discarded preview. | **Fixed** in `bb0c53d5`: every choice and the Back step resolve states from the controller's current state; the evidence records both prerequisites chosen again on that level and the reader requires them committed there. |
| PN4-F6 | The second rider lacked the Ranger experience and could already own Shield Focus. | **Fixed** in `bb0c53d5`: the actor must lack Shield Focus, and receives disclosed native experience for the four Ranger levels; every level still passes the native entitlement check. |
| PN4-F7 | The owned-pairs and owner-seed checkpoint routes do not admit `mounted-shield-tb-manual`. | **Route documented, not connected.** Like `mounted-combat-feat-tb-debt-manual` (run as `chunk7-persistence-save` then `chunk7-persistence-load` on the qualified Working fixture, docs/SPRINT6-MOUNTED-COMBAT-FEAT.md), the shield case is a combat checkpoint of the base persistence route, which admits it (runner, protocol and persistence-case registries). HANDOFF section 0 now names that route and its fixture; the owned-pairs/owner-seed validators are not extended. |
| PN4-F8 | A genuine cold artifact could not pass the reader (it required producer state), and a post-cleanup sample could not show what was serialized. | **Fixed** in `bb0c53d5`: producer and consumer evidence are read in their own shapes; both the written and the loaded archive are read through a clone of the native saver and must hold no Mounted Shield source tag; the cold pair is bound to the authenticated continuation's actors. Reader cases cover both shapes and the refusals. |
| PN4-F9 | Planned totals could need an impossible die; no production check ran while a stronger shield covered the AC. | **Fixed** in `bb0c53d5`: before each planned step the hostile's native attack bonus is set to the rider's check base (equal) or one less (success), so the planned die is the attack's own d20; the reader requires 1..20. A production check now runs under a competing +4 Shield on the mount and must carry +3 (one modifier in the native roll). |
| PN4-F10 | The band probe counted waiting frames and the reader searched the whole run. | **Fixed** in `bb0c53d5`: only strikes count; the qualifying miss is recorded and the reader checks that exact attack (hostile, mount, band, not evaluated). |
| Optional | Compare the recorded sprite name. | Not done (the sprite reference and dimensions are already checked). |

Fast gate on `bb0c53d5`: PASS, 61.6 s (components 1508/0, Validate-Source 153/0, focused 18/18; rollback 57/0, shield readers
81/0, native contract 80/0). DLL `5978e285ccc0d6cc3163bd176e5205728b68fb57d2fd52087c36c5e9c63ff6f4`, MVID
`94c4e4d2-c673-4afd-b4f8-50845118888d`. Budget `mounted-shield` 3.33 of 90 min. To be reviewed in round 5.
