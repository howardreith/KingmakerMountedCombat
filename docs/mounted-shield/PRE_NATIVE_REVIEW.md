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

## Round 5 — candidate `1bac699ec01c003e4aaf7d8317632aa11e488846` (production `bb0c53d5`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-1bac699e`. Started 2026-11-23, 19.87 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS.
- **Verdict: CHANGES REQUIRED.** Prior findings: PN4-F1, PN4-F5, PN4-F6 resolved; PN4-F2 resolved for the reported double
  drift; PN4-F3, PN4-F4, PN4-F7, PN4-F8, PN4-F9 and PN4-F10 partial (their remainders are PN5-F1 to PN5-F7). The PN3-F5
  one-frame race was accepted as a nonblocking compatibility limit.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN5-F1 | Resuming after a suspension rejected a foreign-changed list, and the required-path scan then withdrew Mounted Shield for KMC's own removal. | **Fixed** in `fb19d496`: the suspension records each array's value before and after it; resume writes back exactly the value before wherever the array still holds the value it left (foreign entries and order kept), moves KMC's exact values to the full publication, and reports any list changed again since; a publication while suspended keeps the records, a disable clears them. Component case with persistent appends on a required and an extra list (no withdrawal, exact values back, Mounted Archery untouched). |
| PN5-F2 | A stop before registration had no publication to record it on, and a toggle that faulted a pending registration kept the requested state. | **Fixed** in `fb19d496`: the stop is kept on the service and applied by the production `LeaseKmcCombatFeatSelections` before the first publication; the pending-to-faulted return restores the stored state. Both through the production methods in `Test-MountedShieldSelectionRollback.ps1` (registration faulted by a genuine locale-key collision; 64/0). |
| PN5-F3 | Driver setup cleanup could lose ownership (a write throwing before its handle returns, items tracked after insertion, a one-shot drain). | **Fixed** in `fb19d496`: setup modifiers are owned by `Sprint6ShieldSetupModifiers` (recorded before the write, own source component, removed through the stat with `RemoveExact`, kept until verified); created shields tracked before insertion and disposed on release; buffs verified; the drain retries each frame within the Sprint 6 cleanup bound and records residue it could not clear. Native contract on a genuine stat whose callback throws (83/0). |
| PN5-F4 | The handoff offered a newly qualified Working, which the persistence route's pin refuses. | **Fixed** in the records: the input is the exact historical pair (Working `5eb4e0b4…`, Baseline `c29d965c…`) with its qualification records; the pin is kept. |
| PN5-F5 | Archive scans accepted one present member; the cold pair could be any authenticated actors; a wrong contribution at restoration passed. | **Fixed** in `fb19d496`: both nonempty `party.json` and the saved area's member are required; warm and cold observations are bound to the authenticated first rider and first mount in those roles; a contribution present at restoration must be the current one. Reader refusals added. |
| PN5-F6 | The stronger-shield check could borrow another roll's modifier evidence. | **Fixed** in `fb19d496`: each attempt is paired with its own native roll in order (the Sprint 6 pairing, the unrelated check set aside) and the paired roll must match it. Refusal added for the borrowed case. |
| PN5-F7 | The band credited Mounted Shield's full amount even when a competing Shield reduced its AC effect. | **Fixed** in `fb19d496`: the driver reads the effective AC change from the native modifiers at the band, skips the band when it is zero, and the miss must reach the AC without Mounted Shield. |
| PN5-F8 | Boundary rows could pass on the commitment label alone. | **Fixed** in `fb19d496`: every attempted Mounted Shield row must show the real-time window and charged Swift, the own-turn spent Swift, or the off-turn borrow, from the record's before/after state. |
| PN5-F9 | Any equip failure skipped the tower; foreign library items could stand in for stock shields. | **Fixed** in `fb19d496`: only a native `CanInsertItem` refusal skips the tower (other failures propagate); shields are selected from exact stock types loaded from the game's assets (positive Unity instance ids), each identity recorded and required by the reader. |
| Optional | Sprite name comparison. | Not done. |

Fast gate on `7661a0ec` (production `fb19d496`): PASS, 52.8 s (components 1508/0, Validate-Source 153/0, focused 18/18).
DLL `70fdc113565e344a1dc477641b855dd894dff3f5c337d82a822c926236ff7108`, MVID `bb2bf2b0-66ac-4526-bb85-3d5445b46119`. Budget
`mounted-shield` 4.2 of 90 min. To be reviewed in round 6.

## Round 6 — candidate `1fc9ddb07277abef5ebd737a28ab195086182a26` (production `fb19d496`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-1fc9ddb0`. Started 2026-11-23, 18.57 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS. Evidence: `analysis-cache/mounted-shield-pre-native-1fc9ddb0/`.
- **Verdict: CHANGES REQUIRED.** Prior findings: PN2-F4/PN3-F3/PN4-F2/PN5-F2, PN4-F7/PN5-F4 and PN4-F9/PN5-F6 resolved;
  PN5-F1, PN5-F3, PN5-F5, PN5-F7, PN5-F8 and PN5-F9 partial (their remainders are PN6-F1 to PN6-F7).

| ID | Finding | Disposition |
| --- | --- | --- |
| PN6-F1 | Resume after a terminal suspension could write back stale array contents, erasing a foreign in-place edit or a foreign feat added during the suspension. | **Fixed** in `4092676b`: resume no longer writes recorded arrays back. For each list it keeps the current entries and their order and reinserts Mounted Shield (after the last preceding KMC entry in Mounted Combat, Mounted Archery, Mounted Shield order) only where KMC's own suspension removed it, or, for a suspension that preceded the first publication, where the full publication places it; a list it cannot write is reported. Component cases `SuspensionIsReversible` (in-place edit and late foreign append on required and extra lists during suspension, a publication while suspended, the explicit-enable resume) and `ResumeKeepsForeignRemoval` (1512/0 with the custody cases); compiled rollback 64/0. |
| PN6-F2 | Fixture equipment setup ignored a refused native removal and could overwrite a non-removable original; release removed from the collection before verifying detachment; the temporary Greater Shield Focus had no cleanup obligation. | **Fixed** in `4092676b`: the hands are held by a production `Domain/FixtureHandCustody` bound to the native `HandSlot` calls. A slot is vacated only when `CanRemoveItem` allows it, an item is placed only into an empty slot that accepts it, every step is verified by occupant and holder, and restoration works off-hands first and reports each slot still wrong (retriable). Release vacates through the custody and verifies before the collection removal and disposal. Greater Shield Focus is refused if already owned, owned by the drain while present, and verified removed. Component cases on a slot model: a refused removal changes nothing, a failed first restoration reports and the retry restores every original, and a dropped two-handed primary returns. **Limitation:** these cases model the slot; the native-slot binding is exercised only in a native run. |
| PN6-F3 | The spell selector required one component and so excluded the stock `MageShieldBuff` (two components), allowing a substitute or an anonymous fallback. | **Fixed** in `4092676b`: the buff is resolved by GUID `9c0fa9b438ada3f43864be8dd8b3e741`, name `MageShieldBuff`, and accepted only with its AC Shield-descriptor bonus among its components. A unit already holding it is not touched (recorded unavailable); no fallback modifier. The reader requires the modifier's source GUID on the mount and on the rider, or an explicit unavailable observation. |
| PN6-F4 | Stock selection used instance ids and armor-type GUIDs; enhancement came from `EnchantmentValue` (cost-derived). | **Fixed** in `4092676b`: seven stock items are pinned by item GUID, name and armor type read from the installed `sharedassets1.assets` (buckler, light, heavy, tower; heavy +1, +3, +5), each resolved by GUID and verified; the native armor enhancement is read with `GameHelper.GetItemEnhancementBonus(ArmorComponent)`. The reader requires every pinned identity, each category on its own item with zero enhancement, and +1, +3 and +5 each observed once by native enhancement. The persistence case uses the pinned heavy shield. Reader fixtures use the real GUIDs. |
| PN6-F5 | The persistence reader took the area from the scan, ignored the file name, accepted a stale cold contribution and trusted the continuation's role order. | **Fixed** in `4092676b`: the caller passes the already validated archive. Each scan must name the validated file (cold: `loadedDescriptor.fileName`; warm: the leaf of `nativeWrite.path`) and its area (`AreaId`, 32 hex digits, so `party` cannot stand in); the shield pair must be the archive's Mammoth-profile rider and mount; a contribution present at first restore must agree with amount 3, the mounted state and Shield Focus at that moment. Reader refusals for each. |
| PN6-F6 | The band used shield modifiers sampled before the wait. | **Fixed** in `4092676b`: the rule observer records, with each attack on the mount, Mounted Shield's own modifier, the strongest other Shield modifier and the resulting AC change; the driver selects and the reader checks the miss from that attack's own context (the recorded miss must equal the attack's). Reader cases include an equal total AC from a different modifier set. |
| PN6-F7 | A double Swift charge or a pre-existing debt passed. | **Fixed** in `4092676b`: real time needs mode RT, no window, use or debt before, Swift cooldown 0 before and 5.5–6.05 after; own turn needs mode TB, nothing spent or owed before and a 5.9–6.05 increase; borrow needs nothing spent or owed before, spent and owed after and an unchanged cooldown. Double-charge and pre-existing-debt negatives added. |

Fast gate (controller, focused) on `4092676b`: PASS, 60.3 s (components 1512/0, Validate-Source 153/0, focused 18/18; rollback
64/0, shield readers 117/0). DLL `2cc090ed9cbf45a132434cb66a41f6ba1ac74c094c8a4d5bf7a8f7d368e348e7`, MVID `4e4f4e83-bd9f-481b-983d-f2c5aab06a46`. Budget
`mounted-shield` 5.2 of 90 min. To be reviewed in round 7.

## Round 7 — candidate `47a882df2372df502ec7eb5a6dd9fdc2afb138dc` (production `4092676b`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-47a882df`. Started 2026-11-23, 18.31 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS. Evidence: `analysis-cache/mounted-shield-pre-native-47a882df/`.
- **Verdict: CHANGES REQUIRED.** Prior findings: PN6-F3 resolved; PN4-F8/PN5-F5/PN6-F5 resolved for archive and actor
  binding; PN6-F1, PN6-F2, PN6-F4, PN6-F6 and PN6-F7 partial (their remainders are PN7-F1 to PN7-F6). PN7-F7 is new.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN7-F1 | After a stop that preceded registration, a harmless foreign append to a required list made the explicit enable's resume skip that list and then withdraw Mounted Shield for the session. | **Fixed** in `9bf75488`: the publication records each list KMC itself published while withholding a suspended feat; resume reinserts only Mounted Shield into those lists (and those the suspension took it from) as they now stand, keeping foreign entries and order. The flag-based rule is gone. Compiled through the production explicit-enable path in `Test-MountedShieldSelectionRollback.ps1` (68/0): a foreign append on a required list before the resume, Shield reinserted once after Mounted Archery, the append kept, nothing withdrawn. The same case crashes the round-6 code (its withdrawal path), so the regression discriminates. |
| PN7-F2 | The persistence producer held the equipment custody in a local, so a setup that failed part way lost its displaced original and probe item. | **Fixed** in `9bf75488`: the custody is a scenario field from before the first hand change; the persistence cleanup restores it only after its save, worker and scope obligations are clear (so after the measured warm continuation, and after the save worker on failure), retries until clean and records `shieldEquipmentCleanup`; the written Manual keeps its shield. The reader requires that record clean for the producer. |
| PN7-F3 | A stat-change callback fault during a setup removal was dropped by later releases, so the row could look clean. | **Fixed** in `9bf75488`: the setup custody keeps every fault for the row (`allFaults`); a release that faults fails the row; the drain is not clean while any fault was recorded; the reader refuses any recorded fault. Native contract on a genuine stat whose callback throws: the fault is kept and a later release that finds nothing left still reports it (86/0). |
| PN7-F4 | The stronger-shield check and the band read shield terms from a snapshot or from the hit-roll callback, not from the AC calculation the attack used. | **Fixed** in `9bf75488`: the rule observer binds the mount's shield terms to each attack at its own nested `RuleCalculateAC` (same attacker and target, inside the open `RuleAttackRoll`), records that calculation's result, attack type and flat-footed state, and attaches it only when the result is the attack's `TargetAC`. The reader requires, for the checked attack, Mounted Shield +3 with the competing +4 at that calculation (no AC change from Mounted Shield), and for the band miss terms from its own calculation against melee/ranged AC (not touch). Reader negatives for the +4 missing at calculation, missing terms, a different calculation and touch AC. |
| PN7-F5 | A Swift spent from a positive cooldown, a pre-existing window and real-time debt passed. | **Fixed** in `9bf75488`: each commitment requires the eligible before-state (no window, use or debt; cooldown 0 for a Swift spent now) and the complete after-state (real time: window only; own turn: spent only, cooldown 5.9–6.05; borrow: spent and owed, cooldown unchanged). Borrowing after a Swift legitimately spent on the rider's own turn stays accepted. Negatives for 6 → 12, an open window before each turn-based commitment and real-time debt. |
| PN7-F6 | The persistence observation recorded the requested heavy shield over the observed one. | **Fixed** in `9bf75488`: the observation keeps the item the rider holds (blueprint, process-local object identity — 2.1.7b items have no persistent id — and native armor enhancement) separately from the requested blueprint; the reader requires the stock heavy shield with no enhancement at the save, the warm continuation, the cold restore and the cold continuation, and the same object within each process. Negatives with the same-base +1 item, another object and an enhancement. |
| PN7-F7 | The shield rows' host deadline (640 s) exceeds the runner's 360 s default, so a valid run would hit the leave-running exit. | **Fixed** in `9bf75488`: the guarded runner declares 900 s for `sprint6-shield-rt` and `sprint6-shield-tb` and refuses less (the value it passes to budget admission and its deadline). The native contract parses the runner and checks the declared value against the compiled host deadline. Not run as a WhatIf here (no admitted fixture). |
| Optional | Sprite name comparison. | Not done. |

Fast gate (controller, focused) on `9bf75488`: PASS, 57.4 s (components 1512/0, Validate-Source 153/0, focused 18/18; rollback
68/0, shield readers 134/0, native contract 86/0). DLL `362c2b141429ca5adace316b0497d2de898fc0a6e4447641446c0afc00824cb2`, MVID `50eafd79-0c67-440e-a4e7-aeeb8b93b33a`.
Budget `mounted-shield` 6.15 of 90 min. To be reviewed in round 8.

## Round 8 — candidate `a9cb432a4972f9dc11c64cb414393e9548921f12` (production `9bf75488`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-a9cb432a`. Started 2026-11-23, 16.61 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS. Evidence: `analysis-cache/mounted-shield-pre-native-a9cb432a/`.
- **Verdict: CHANGES REQUIRED.** One production publication defect and four qualification-judgement defects; PN7-F4
  remained open under PN8-F2.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN8-F1 | The explicit enable publishes before it resumes; a non-required list another mod took Mounted Shield from already matched the withholding target and was recorded as withheld, so the resume put Shield back. | **Fixed** in `05ef7f8f`: a list is recorded as withheld only when KMC actually wrote it without the suspended feat. Component case through the full enable order (publish, foreign removal, stop, publish, resume): the list keeps its contents and array reference, the required lists offer Shield once, nothing is withdrawn; it fails on the round-7 code. The PN7-F1 compiled regression still passes (68/0). |
| PN8-F2 | The observer overwrote an attack's AC sample on every nested calculation, so a critical-confirmation calculation could replace the primary one. | **Fixed** in `05ef7f8f`: samples are kept per `RuleCalculateAC` object by a production `Domain/AttackCalculationSamples`, and the hit takes the sample of the calculation it references (`RuleAttackRoll.ACRule`, verified on the installed assembly), still requiring its result to be `TargetAC`. Component cases: a second calculation in the same attack keeps the primary sample, equal totals with different shield terms stay distinct, innermost ownership and closing. |
| PN8-F3 | The equipment reader compared the native AC rules with ordinary AC although outside combat the rules choose flat-footed AC. | **Fixed** in `05ef7f8f`: each probe records the rule's `IsTargetFlatFooted`, and the reader compares melee with ordinary or flat-footed AC and touch with touch or flat-footed touch accordingly; shieldless-reference and touch-exclusion checks unchanged. Reader cases: exploration flat-footed values accepted, a flat-footed rule returning ordinary AC and a missing choice refused. |
| PN8-F4 | The RT row's ordinary and stronger-shield successes and any additional attempts were not held to the real-time commitment. | **Fixed** in `05ef7f8f`: every attempted record on the RT row must be mode RT with `RealTimeWindow` and its exact transition, and every attempted record on the TB row a turn-based spend or borrow with its exact transition; forced-total checks stay with the planned attacks. Negatives: a TB borrow at RT step 9, a double charge on an additional natural hit (RT) and an additional TB use from an unavailable Swift, each with an accepted single-charge control. |
| PN8-F5 | The three-feat observation accepted Mounted Archery absent. | **Fixed** in `05ef7f8f`: the producer records the last three published entries; the reader requires exactly one of each feat and the tail Mounted Combat, Mounted Archery, Mounted Shield. Negatives: Archery absent (with and without the tail claim) and another order. |

Fast gate (controller, focused) on `05ef7f8f`: PASS, 58.0 s (components 1516/0, Validate-Source 153/0, focused 18/18; rollback
68/0, shield readers 145/0, native contract 86/0). DLL `e8d6ff437e1cfa475c7bd260349f68c14ee94d9841778c9c18e0c24cf060a22b`, MVID `b854eb4b-dcf6-458d-9840-7c3567e3495e`.
Budget `mounted-shield` 7.12 of 90 min. To be reviewed in round 9.

## Round 9 — candidate `03bf2ae4024ff6db9a285eaa9f216a411ed717a1` (production `05ef7f8f`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-03bf2ae4`. Started 2026-11-23, 17.11 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS. Evidence: `analysis-cache/mounted-shield-pre-native-03bf2ae4/`.
- **Verdict: CHANGES REQUIRED.** Two production defects (PN9-F1, PN9-F2) and four driver/reader defects.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN9-F1 | Against a Brilliant Energy weapon, installed `RuleCalculateAC` subtracts the raw sum of armor and shield modifiers, including a Mounted Shield +3 that a competing +4 kept from raising the AC, so the mount's AC fell below its value without the feat. | **Fixed** in `d7e1ee0c` (production): `MountedShieldService.OnEventDidTrigger(RuleCalculateAC)` gives back the part of KMC's raw Shield value that did not raise the AC the calculation used (nothing on touch AC; above the strongest other Shield modifier otherwise), through the rule's own `TargetAC` and `BrilliantEnergyBonus` setters; the sheet, ordinary AC and the rider's check are untouched. Native contract on a genuine rule whose target reaches a genuine stat's modifiers, calling the installed `CalculateArmorAndShieldBonuses`: with a competing +4 the same melee and touch attacks see the same AC with and without Mounted Shield (10 and 10; the round-8 code gives 7); without a competing shield no correction; no Brilliant Energy, no correction (91/0). **Limitation:** offline the armor-class stat cannot run its own update (a Unity ECall), so the rule reads the genuine stat's modifier store through an armor-class view; the full `OnTrigger` (flat-footed check through the rulebook) is exercised only natively. |
| PN9-F2 | The explicit enable re-applied an already-live publication: it wrote Mounted Archery back into a list another mod had edited and recorded that write as withholding, so Resume also restored Mounted Shield. | **Fixed** in `d7e1ee0c` (production): re-applying a live publication writes nothing (a terminal stop leaves the lists published), so only a transition from unpublished writes and records withholding; Resume restores only where KMC's own suspension or publication made the feat absent. Component case with another mod removing both feats from a non-required list through the full stop, publish, resume order (contents and array reference kept, required lists offer both once, nothing withdrawn); the PN4-F3 case now asserts that the re-application writes no list; PN7-F1 compiled regression 68/0. |
| PN9-F3 | A planned roll could control another pair's defense, and its record could complete the step. | **Fixed** in `d7e1ee0c`: on the shield rows the planned total applies only to the measured rider's hit on the measured mount by the row's hostile, and a step completes only on that pair's record; the reader takes the measured pair's single record per step, pairs only the measured rider's attempts with its own rolls, and refuses a planned roll on any other pair. Negative with another pair's forced defense at a planned step, and an accepted control where it rolls naturally. The base Sprint 6 rows are unchanged. |
| PN9-F4 | An additional turn-based attempt could spend Swift off turn, and natural rolls' +3 was not checked. | **Fixed** in `d7e1ee0c`: every attempted TB record must spend on the rider's turn and borrow otherwise, and every attempted RT and TB record of the measured pair must show exactly one native +3 in its own roll, forced or natural. Negatives for both reversed turn roles and for a natural roll without the +3. |
| PN9-F5 | The selection reader accepted any two tail rows and pooled eligibility, so a fighter list that always refused could pass. | **Fixed** in `d7e1ee0c`: the producer names each three-feat row; the reader binds them to distinct lists of the producer's own selection inventory, the ordinary and fighter lists among them, and requires a positive native eligibility observation on an available (unselected) choice in each with both prerequisites in the preview. The driver now takes Mounted Shield, like its prerequisites, at a level offering both choices (level bound raised from 8 to 12), so the run observes both. Negatives for a duplicate list, a row outside the inventory and a fighter list that always refuses. |
| PN9-F6 | The two-pair row credited the UI selection switch without observing it. | **Fixed** in `d7e1ee0c`: the driver makes the first rider the single native selection before the switch and records `SingleSelectedUnit` before the switch, with the post-switch snapshots and after returning; the reader requires first, second and first rider respectively. Negatives for a switch that left the first rider selected and an unobserved selection. |
| Optional | Sprite name comparison. | Not done. |

Fast gate (controller, focused) on `d7e1ee0c`: PASS, 79.6 s (components 1517/0, Validate-Source 153/0, focused 18/18; rollback
68/0, shield readers 155/0, native contract 91/0). DLL `eb20b6308825d61a82a00558f5a167e00a739b47ebb69d13f6919725f95f7cbc`, MVID `52301c4b-76a2-4caa-b6bb-77c3ae43980c`.
Budget `mounted-shield` 8.43 of 90 min. To be reviewed in round 10.

## Round 10 — candidate `a809fb06500a181af1215b950a53445bcb622192` (production `d7e1ee0c`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-a809fb06`. Started 2026-11-23, 15.66 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS. Evidence: `analysis-cache/mounted-shield-pre-native-a809fb06/`.
- **Verdict: CHANGES REQUIRED.** One production ownership defect (PN10-F1) and four diagnostic defects.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN10-F1 | Resume wrote the full offered set into a recorded list, so an Archery removal another mod made during the suspension was undone. | **Fixed** in `241e68be` (production): resume inserts only the suspended feat into the list as it now stands (after the last preceding KMC entry); it never replaces a list with KMC's full value. Component case: suspension, then another mod removes Archery from a non-required list, then the explicit enable (publish, resume): the removal survives, Shield returns once after Mounted Combat, the required lists offer each feat once, nothing withdrawn. PN7-F1 compiled regression 68/0. |
| PN10-F2 | A planned die stayed armed after its attempt (a hit during the renewal wait received a second planned total), and the step's record boundary was set one tick late. | **Fixed** in `241e68be`: a production `Domain/PlannedAttempt` arms one plan for the measured rider, mount and hostile, together with the step's record boundary (`ShieldArm`), is consumed by the first qualifying attempt, and is cleared on step exit and at the drain. Component cases: consumed once (a hit during the renewal wait rolls naturally), another pair or attacker neither receives nor consumes it, a hit right after arming receives it, clearing disarms. |
| PN10-F3 | The reader accepted a defense record whose attack rule targeted another mount and rejected a legitimate same-step follow-up strike. | **Fixed** in `241e68be`: the driver records the record that completed each step (`stepRecords`) and the attempt that consumed each plan (`plannedAttempts`, with its hostile); the reader takes exactly that record, requires the plan's consumer to be it, and requires every record's attack rule to name the same attacker and mount; further records of the step are validated by the all-attempt checks. The round-9 two-pair fixture now targets the second mount. Negatives for a mismatched target (other pair and measured pair), an unrecorded completing attack and a planned attempt that is not the completing record; an accepted UsageSpent follow-up in a planned step. |
| PN10-F4 | Other riders' natural defenses escaped mode, turn and Swift accounting. | **Fixed** in `241e68be`: the RT and TB all-attempt checks cover every attempted defense of every pair (mode, turn ownership, commitment, exact cooldown or debt, attack rule) and pair each attempt with its own rider's roll, which must hold that pair's Mounted Shield amount (the measured pair +3). Negatives for a double charge and a turn-based commitment on another rider's RT defense, with the accepted natural control. |
| PN10-F5 | A supported selection could be missing from the three-feat evidence. | **Fixed** in `241e68be`: the three-feat rows and the selection inventory must have the same unique GUID set, each row valid. A three-list control is accepted and fails when the third observation is removed. |
| Optional | Sprite name comparison. | Not done. |

Fast gate (controller, focused) on `241e68be`: PASS, 88.6 s (components 1521/0, Validate-Source 153/0, focused 18/18; rollback
68/0, shield readers 164/0, native contract 91/0). DLL `a5ff8e54008789f0c3079fbd0fe3ceb8c8a94f4988aec5bef474e6df0f027e8f`, MVID `48c32ec4-a5dc-4aa8-aae9-f611608d3cb3`.
Budget `mounted-shield` 9.9 of 90 min. To be reviewed in round 11.

## Round 11 — candidate `64648d26101237d903c68befdeb71cf2da6b8800` (production `241e68be`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-64648d26`. Started 2026-11-23, 15.58 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS. Evidence: `analysis-cache/mounted-shield-pre-native-64648d26/`.
- **Verdict: CHANGES REQUIRED.** Two production defects in the suspension bookkeeping (PN11-F1, PN11-F2) and five
  driver/reader defects.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN11-F1 | A list where KMC removed Shield but no KMC entry remained (another mod removed Mounted Combat, Archery unavailable) was skipped by Resume, and the refresh then withdrew Shield globally, disabling both benefits. | **Fixed** in `720a51ee` (production): the suspension bookkeeping is now one per-feat record of the lists where KMC itself caused the feat's absence; Resume returns the feat to each of them as it stands, after the last preceding KMC entry or, with none left, appended (foreign order kept). Component case: Archery unregistered, Mounted Combat removed from the fighter list by another mod, stop and explicit enable: Shield appended once, foreign order kept, available and never withdrawn. |
| PN11-F2 | A Shield offer another mod reintroduced during the suspension was stripped without a record and never restored. | **Fixed** in `720a51ee` (production): every removal made while enforcing a suspension (`RemoveClosed`, the explicit enable's refresh included) is added to the same record, never for a permanent withdrawal. Component case: the extra list lacks Shield at the stop, another mod puts it back, KMC strips it, the resume restores it once; the controls where a foreign removal persists still pass. |
| PN11-F3 | A follow-up refusal arriving with the planned success replaced it as the completing record, and a refusal before the qualifying attack could advance the step. | **Fixed** in `720a51ee`: a planned step completes only on exactly the attempt that consumed its plan (`PlannedAttempt.CompletingIndex`, used by the driver's step completion); a natural step on the first record of the measured pair. Component cases with both records present and with a refusal arriving first. |
| PN11-F4 | Additional refusals escaped target and rider-cost validation. | **Fixed** in `720a51ee`: every defense record of a check row is bound to its attack rule's attacker and mount, and every refusal must leave the hit standing and the rider's Swift cooldown, window, use and debt unchanged. Negatives for a refused follow-up that charged Swift, targeted another mount, or turned the hit. |
| PN11-F5 | A forced roll outside the plan could pass. | **Fixed** in `720a51ee`: the planned steps come from the scenario (RT 2 equal, 4 success, 9 success; TB 2 success, 6 equal); the planned attempts must be exactly those, each the designated hostile's completing attack, and the forced records exactly those attempts. Negatives for a forced boundary record relabeled natural and a forced extra hit. |
| PN11-F6 | The second rider's check could carry the first rider's +3. | **Fixed** in `720a51ee`: each attempt's expected Mounted Shield amount is its pair's own: the measured pair +3, the second pair identified in MS-pairs its observed amount (+2), anyone else 0. Negative with +3 on the second rider's roll; the +2 control is accepted. |
| PN11-F7 | The band reader required one hostile across the row and at least one driver strike. | **Fixed** in `720a51ee`: the driver records the row's designated hostile at spawn; the qualifying miss must be that hostile's; other hostiles' attacks are judged on their own; 0 to 120 driver strikes. Controls for a second hostile's attack and a miss before the first strike; negatives for another hostile's miss and a missing designation. The PN4-F10 case now checks the 120-strike bound. |
| Optional | Sprite name comparison. | Not done. |

Fast gate (controller, focused) on `720a51ee`: PASS, 61.2 s (components 1524/0, Validate-Source 153/0, focused 18/18; rollback
68/0, shield readers 174/0, native contract 91/0). DLL `51e100c5e68d795c45fdb3af367571edf9d74c85e6030368b7debfee34c0f4bc`, MVID `b1300e95-ac52-417d-85b8-788b56a6448e`.
Budget `mounted-shield` 10.92 of 90 min. To be reviewed in round 12.

## Round 12 — candidate `df564e749268f856020c78a7a1395049156663e0` (production `720a51ee`)

- **Transport:** as round 4, fresh snapshot `worktrees/mounted-shield-review-df564e74`. Started 2026-11-23, 19.6 minutes,
  exit 0, empty stderr; identity and cleanliness recheck PASS. Evidence: `analysis-cache/mounted-shield-pre-native-df564e74/`.
- **Verdict: CHANGES REQUIRED.** The round-11 publication fixes address both reported production defects; no new production
  defect in the shipped feature. Six findings in the diagnostic roll injection and the qualification reader.

| ID | Finding | Disposition |
| --- | --- | --- |
| PN12-F1 | A planned check could submit a d20 outside 1-20 (calibration from displayed Mobility, native rule-local penalties in the roll), and the reader rejected a legal roll whose aggregate bonus fell below +3 through a native penalty. | **Fixed** in `ed717538`: the production `ControlledSkillCheck` (used only through the diagnostic scenario hook) injects a die only when a legal 1-20 reaches the planned total at the native roll boundary; otherwise the native die rolls, the use stays committed, and the record is marked `plannedUnattainable` (not forced), which fails the measurement. The reader judges KMC's contribution by its identified modifier, not by the aggregate bonus. Native contract: an equality total with a native -2 reached by die 19 on the production check, and an equality needing 22 reported unattainable (94/0). Reader: unattainable fails the row; an in-range check with a native penalty is accepted. |
| PN12-F2 | A refused follow-up could suppress the native hit and pass on the service's own before/after strings. | **Fixed** in `ed717538`: for every refusal of a check row, the bound native attack must still hit at the event and after the rule, keep its result (not Parried or Miss), keep its critical state and deal its damage. Negative with only the native observer suppressed. |
| PN12-F3 | Forced-roll accounting stopped at the check rows. | **Fixed** in `ed717538`: every forced defense of the whole measurement (any case, or none at cleanup) must be one of the check rows' planned attempts, matched by case, step, rule, rider, mount and hostile. Negatives for a forced defense under MS-pairs and with no case; a natural control is accepted. |
| PN12-F4 | The forced boundary could pass against the defense service's copy of the attack operands. | **Fixed** in `ed717538`: every record's attack d20 and bonus must equal the bound native attack's, and its attack total their sum. Negative changing only the native attack's bonus. |
| PN12-F5 | A known rider could defend the other known mount. | **Fixed** in `ed717538`: each known mount (the measured mount; on RT the second mount established in MS-pairs) may be defended only by the rider established for it, before amount or cost is judged. Negative with the second rider defending the first mount; the +2 second-pair control is accepted. |
| PN12-F6 | The equipment-swap check ignored the Swift cooldown and window expiry. | **Fixed** in `ed717538`: the three same-frame samples must also agree on `swift` and `windowEndTicks`, both present. Negatives for a refunded cooldown and an extended expiry; the fixture now carries both fields. |
| Optional | Sprite name comparison. | Not done. |

Fast gate (controller, focused) on `ed717538`: PASS, 64.9 s (components 1524/0, Validate-Source 153/0, focused 18/18; rollback
68/0, shield readers 184/0, native contract 94/0). DLL `069f9fb906382216b3b41343dc53da43a9521808316f54d8e8b7dfa338c283d4`, MVID `65a1a09b-ce5a-46c1-aac2-5e86344d7018`.
Budget `mounted-shield` 11.98 of 90 min. To be reviewed in round 13.
