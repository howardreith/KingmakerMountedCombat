<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r03-review part=1/1 sha256=59b5c60fbd6565cfc02e5bb9e0a9ab696d12ac974d3ff98099f7f05fa31bbed9 -->
### Codex GPT-6.2 Astra — review — round 3 — candidate `6369c5992c`

**Decision:** `changes_required` — The focused gate passed on the candidate. Three failure paths still affect the saved-horse repair. A logging exception can reopen a completed removal, an archive error can strand a decided request, and the final rename can publish different bytes from those verified.

## Continuation for Claude

Make the classified removal outcome independent of diagnostic formatting and logging. Complete archive publication from the verified bytes, and keep an interrupted staging operation recoverable before admitting another request. Exercise the actual diagnostic call and the interval between staging and final rename in the existing focused fixtures. Keep the corrections within the repair service and retain the guarded-folder, donor, provenance, no-change and Mounted Shield controls. Supply the metered focused Fast receipt for the corrected candidate. N07 and N10 remain owner playtest observations.

#### Findings

**R3-F1** · P1 · defect · blocks readiness — A diagnostic exception can reopen a verified removal
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs:205`; acceptance: MMR-06, S06
- Trigger: Reading the horse's display name or calling logger.Info throws after native removal and post-state classification.
- Expected: Once the collection has been classified, a diagnostic failure preserves the decision and its single-use effect.
- Actual: TripRepairFileHost.RemoveExactly increments the applied count and logs before returning the classification. If that diagnostic throws, Process leaves the claim Undecided. The next poll can publish a refusal because the fact is gone, or remove a restored fact again after reload. The test host returns the classification directly and skips this diagnostic.
- Why it blocks / does not: One owner request can produce a false refusal or a second removal after the first removal was already verified.
- Requested correction: Return the classification to the policy before fallible diagnostics, or contain both message construction and the logger call so the classified outcome always reaches the session.
- Requested evidence: Make the production removal diagnostic throw after an exact removal. Reload a horse with the same ID and one candidate. Verify an applied result and archive, one native removal in total and preservation of the reloaded fact.

**R3-F2** · P2 · defect · blocks readiness — A failed final rename strands the decided request
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:535`; acceptance: MMR-06, S06
- Trigger: The staging rename succeeds and verification or the final archive move fails. A directory occupying the intended archive filename is one concrete case.
- Expected: The current session finishes the frozen archive before taking newer work. A fresh session can settle a stranded claim conservatively.
- Actual: Close moves the only .claimed file to a timestamped .closing path. A later read or move failure leaves it there. The next poll looks only for .claimed, clears the session at line 136 and can process a newer request. The existing FailClose fixture throws before either production rename.
- Why it blocks / does not: An ordinary I/O failure defeats the required archive retry. A newer result can overwrite the previous result while the original request still has no terminal archive.
- Requested correction: Keep the claim discoverable throughout finalization or make the staged file part of recovery. Resume the pending archive using its bound identity, including when a newer request is waiting.
- Requested evidence: Force the actual final rename to fail after staging succeeds, then clear the obstruction. Verify that the original bytes reach the correct archive before the newer request runs. Exercise fresh-session interruption from the same staged state with zero further removals.

**R1-F4** · P2 · defect · blocks readiness — The staged claim can change after its final hash check
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:542`; acceptance: MMR-06, S06
- Trigger: A writer changes the captured file between the staged read at line 539 and the terminal rename at line 542.
- Expected: The bytes published under a terminal suffix are the bytes bound to that decision through the final move.
- Actual: File.ReadAllBytes closes the staged file before the digest comparison and final File.Move. That move resolves the source by filename again. An intervening edit to the staged file, including an in-place edit through another name for the same file, can still put horse B's bytes in horse A's .applied archive.
- Why it blocks / does not: The terminal archive can still attribute an applied removal to the wrong request.
- Requested correction: Publish the archive from the verified bytes or protect the captured file's identity and contents through publication. Preserve replacement bytes for their own disposition.
- Requested evidence: Change the staged source after its verification and before terminal publication. Verify that B's bytes never acquire A's .applied suffix, B receives its own disposition and only A's removal executes.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — The composition root recognizes the launcher's exact -kmcObserverRequest argument with a null automation host. Without an isolated root, ProcessFolder exits before constructing a host. Three recorded polls preserved the seeded player filenames and hashes, and the ordinary and isolated controls passed.
- **R1-F2** — `fix_verified` — The removal helper compares the feature instances before and after native return or exception. The gate exercised target-plus-neighbor removal followed by a throw and recorded a terminal failure. Exact target-only removal followed by a throw recorded Applied. R3-F1 occurs after that classification.
- **R1-F4** — `fix_rejected` — The recorded replacement at the .claimed name during WriteResult is handled correctly. The staged verification still releases the file before the rename that assigns the suffix, leaving the exact-byte obligation open at that final boundary.
- **R2-F1** — `fix_verified` — A normal native return with the original feature instances present now yields Unchanged. The recorded cases returned the original request for one successful retry and preserved a newer request byte-for-byte through supersession.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoff, prior findings, developer report, transcript notice, controller receipt and gate log.; All 18 changed files in M..H and the correction since b36f08a7, including the repair policy, game adapter, composition root, launcher arguments, file fixtures and focused contracts.; The tracked operational authorization records match the hashes in the supplemental ledger. The gate recorded 105.03 of 150 validation minutes spent.
- **Executed verification:** Verified HEAD, merge base, mission SHA-256 and the controller log SHA-256. The log matched receipt digest 5112a3fa516891ac34b53f3c85dbb3347cfef80b9d5d6ff356517b804b0ffa63. Git diff --check exited 0.; The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at H. It exited 0 in 35.9 seconds. Components passed 1544/1544, Validate-Source 153/153, the Trip contract 50/50 and the focused wrapper 12/12 with nativeProof=false.; The log records the observer-only folder test, both native-removal exception controls, no-change retry and supersession, and replacement of the claim during the result write. Mounted Shield passed 114 contract checks and 68 rollback checks. The packet, production and embedded icon SHA-256 values matched.
- **Source inspection only:** Traced the three reported failure paths through the production adapter, Close, and the next Process poll. The supplied gate exercises different fault locations.; Confirmed the construction filter is confined to the KMC Horse donor copy and that saved-horse mutation requires the owner request, the matching horse, one candidate and the world write gate.
- **Required gaps:** Show that a diagnostic exception after classification preserves the terminal outcome and one removal.; Show recovery after the production staging move succeeds and the final archive operation fails, including recovery in a fresh session.; Show that changing the captured file after verification cannot put replacement bytes in the original decision's archive.
- **Limitations:** The review used read-only inspection and the controller's recorded execution. The foreground developer transcript was withheld.; The component tests use real temporary files and simulated horses and facts. Detached Kingmaker object checks support the construction contract. Live hit, damage and save/load behavior await owner observation.

#### Summary

- **Delivered behavior:** The installed-data contract identifies the Mammoth donor's stock Bite-to-Trip component. Fresh KMC Horse construction filters that grant and retains one bite and two hooves. Existing horses receive an explicit, gated repair path scoped to one matching instance.
- **Scope reviewed:** Final PR-4 review of MMR-06, S06, S08 and the carried terminal-decision regression at commit 6369c5992c86df106c0ad91b9d4a6171abb57496.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N07 — observe a plain horse landing a bite and dealing damage without automatic Trip, with a legitimate canine or explicitly granted maneuver control where available.; N10 — observe a repaired existing horse across reload or area transition, including pair identity, shield effects and normal action availability.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a217df-fe80-7742-9800-9107268ce2d2`, duration 913.6s, response sha256 `79757ed1bda09daab2cbade4384406a00cbed8b53695c1659c565c4466830912`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r03-2d168a`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "6369c5992c86df106c0ad91b9d4a6171abb57496",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the candidate. Three failure paths still affect the saved-horse repair. A logging exception can reopen a completed removal, an archive error can strand a decided request, and the final rename can publish different bytes from those verified.\n\n## Continuation for Claude\n\nMake the classified removal outcome independent of diagnostic formatting and logging. Complete archive publication from the verified bytes, and keep an interrupted staging operation recoverable before admitting another request. Exercise the actual diagnostic call and the interval between staging and final rename in the existing focused fixtures. Keep the corrections within the repair service and retain the guarded-folder, donor, provenance, no-change and Mounted Shield controls. Supply the metered focused Fast receipt for the corrected candidate. N07 and N10 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified HEAD, merge base, mission SHA-256 and the controller log SHA-256. The log matched receipt digest 5112a3fa516891ac34b53f3c85dbb3347cfef80b9d5d6ff356517b804b0ffa63. Git diff --check exited 0.",
      "The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at H. It exited 0 in 35.9 seconds. Components passed 1544/1544, Validate-Source 153/153, the Trip contract 50/50 and the focused wrapper 12/12 with nativeProof=false.",
      "The log records the observer-only folder test, both native-removal exception controls, no-change retry and supersession, and replacement of the claim during the result write. Mounted Shield passed 114 contract checks and 68 rollback checks. The packet, production and embedded icon SHA-256 values matched."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoff, prior findings, developer report, transcript notice, controller receipt and gate log.",
      "All 18 changed files in M..H and the correction since b36f08a7, including the repair policy, game adapter, composition root, launcher arguments, file fixtures and focused contracts.",
      "The tracked operational authorization records match the hashes in the supplemental ledger. The gate recorded 105.03 of 150 validation minutes spent."
    ],
    "limitations": [
      "The review used read-only inspection and the controller's recorded execution. The foreground developer transcript was withheld.",
      "The component tests use real temporary files and simulated horses and facts. Detached Kingmaker object checks support the construction contract. Live hit, damage and save/load behavior await owner observation."
    ],
    "required_gaps": [
      "Show that a diagnostic exception after classification preserves the terminal outcome and one removal.",
      "Show recovery after the production staging move succeeds and the final archive operation fails, including recovery in a fresh session.",
      "Show that changing the captured file after verification cannot put replacement bytes in the original decision's archive."
    ],
    "source_inspection_only": [
      "Traced the three reported failure paths through the production adapter, Close, and the next Process poll. The supplied gate exercises different fault locations.",
      "Confirmed the construction filter is confined to the KMC Horse donor copy and that saved-horse mutation requires the owner request, the matching horse, one candidate and the world write gate."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "TripRepairFileHost.RemoveExactly increments the applied count and logs before returning the classification. If that diagnostic throws, Process leaves the claim Undecided. The next poll can publish a refusal because the fact is gone, or remove a restored fact again after reload. The test host returns the classification directly and skips this diagnostic.",
      "blocking_rationale": "One owner request can produce a false refusal or a second removal after the first removal was already verified.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once the collection has been classified, a diagnostic failure preserves the decision and its single-use effect.",
      "id": "R3-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs",
        "line": 205
      },
      "priority": "P1",
      "requested_correction": "Return the classification to the policy before fallible diagnostics, or contain both message construction and the logger call so the classified outcome always reaches the session.",
      "requested_evidence": "Make the production removal diagnostic throw after an exact removal. Reload a horse with the same ID and one candidate. Verify an applied result and archive, one native removal in total and preservation of the reloaded fact.",
      "title": "A diagnostic exception can reopen a verified removal",
      "trigger": "Reading the horse's display name or calling logger.Info throws after native removal and post-state classification."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Close moves the only .claimed file to a timestamped .closing path. A later read or move failure leaves it there. The next poll looks only for .claimed, clears the session at line 136 and can process a newer request. The existing FailClose fixture throws before either production rename.",
      "blocking_rationale": "An ordinary I/O failure defeats the required archive retry. A newer result can overwrite the previous result while the original request still has no terminal archive.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The current session finishes the frozen archive before taking newer work. A fresh session can settle a stranded claim conservatively.",
      "id": "R3-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 535
      },
      "priority": "P2",
      "requested_correction": "Keep the claim discoverable throughout finalization or make the staged file part of recovery. Resume the pending archive using its bound identity, including when a newer request is waiting.",
      "requested_evidence": "Force the actual final rename to fail after staging succeeds, then clear the obstruction. Verify that the original bytes reach the correct archive before the newer request runs. Exercise fresh-session interruption from the same staged state with zero further removals.",
      "title": "A failed final rename strands the decided request",
      "trigger": "The staging rename succeeds and verification or the final archive move fails. A directory occupying the intended archive filename is one concrete case."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "File.ReadAllBytes closes the staged file before the digest comparison and final File.Move. That move resolves the source by filename again. An intervening edit to the staged file, including an in-place edit through another name for the same file, can still put horse B's bytes in horse A's .applied archive.",
      "blocking_rationale": "The terminal archive can still attribute an applied removal to the wrong request.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The bytes published under a terminal suffix are the bytes bound to that decision through the final move.",
      "id": "R1-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 542
      },
      "priority": "P2",
      "requested_correction": "Publish the archive from the verified bytes or protect the captured file's identity and contents through publication. Preserve replacement bytes for their own disposition.",
      "requested_evidence": "Change the staged source after its verification and before terminal publication. Verify that B's bytes never acquire A's .applied suffix, B receives its own disposition and only A's removal executes.",
      "title": "The staged claim can change after its final hash check",
      "trigger": "A writer changes the captured file between the staged read at line 539 and the terminal rename at line 542."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "The composition root recognizes the launcher's exact -kmcObserverRequest argument with a null automation host. Without an isolated root, ProcessFolder exits before constructing a host. Three recorded polls preserved the seeded player filenames and hashes, and the ordinary and isolated controls passed."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F2",
      "rationale": "The removal helper compares the feature instances before and after native return or exception. The gate exercised target-plus-neighbor removal followed by a throw and recorded a terminal failure. Exact target-only removal followed by a throw recorded Applied. R3-F1 occurs after that classification."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F4",
      "rationale": "The recorded replacement at the .claimed name during WriteResult is handled correctly. The staged verification still releases the file before the rename that assigns the suffix, leaving the exact-byte obligation open at that final boundary."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F1",
      "rationale": "A normal native return with the original feature instances present now yields Unchanged. The recorded cases returned the original request for one successful retry and preserved a newer request byte-for-byte through supersession."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r03-2d168a",
  "round": 3,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The installed-data contract identifies the Mammoth donor's stock Bite-to-Trip component. Fresh KMC Horse construction filters that grant and retains one bite and two hooves. Existing horses receive an explicit, gated repair path scoped to one matching instance.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N07 \u2014 observe a plain horse landing a bite and dealing damage without automatic Trip, with a legitimate canine or explicitly granted maneuver control where available.",
      "N10 \u2014 observe a repaired existing horse across reload or area transition, including pair identity, shield effects and normal action availability."
    ],
    "scope_reviewed": "Final PR-4 review of MMR-06, S06, S08 and the carried terminal-decision regression at commit 6369c5992c86df106c0ad91b9d4a6171abb57496.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
