# Chunk 7 known product debt

## October 19 - preview.43 closure and next source group IN PROGRESS

Preview.43 worker cancel/drain and nonselected-owner global save barrier now
PASS natively. Pending-schema2 and schema1 producers remain preserved FAIL;
their dependent cold loads were not launched. All four runs restored all three
transactions and profile bytes exactly; no game or lock remains. The schema2
test must preserve the correct future-data guard and derive historical metadata
from a genuine current pending save. The schema1 observer must preserve null
hotbar collection custody without allocating native state.

Exact source/package/DLL/run hashes, durations and remaining scope are in
[the active Directive 4 checkpoint](CHUNK7-CODEX-DIRECTIVE4-CHECKPOINT-2026-10-18.md).
The source freeze is closed. Compose and review the private group44 drafts,
run focused affected checks, commit/guarded-push, then build the next candidate
once and declare bounded native retries. Directive 4 remains IN PROGRESS.

## Native driver selection and incomplete control-fact evidence — IN PROGRESS

Preview.42 b13s1 worker and b13s2 barrier remain FAIL despite game PASS.
The worker's restored-start path omitted normal Select(A); strict selection
validation stays intact. Barrier preparation exposed a distinction between stale
hotbar AbilityData templates and live fact-bound controls. Four raw templates
and aggregate facts6→4 cannot prove exact actor ownership. Independent review
requires native per-actor/per-kind fact observations, including active state,
fact/Data blueprints, caster and native lookup, with compensated-leak negatives.
No threshold-only repair or deletion of native hotbar templates is accepted.
Preview.43 will require new observations and fresh native attempts. All failed
raw evidence is immutable, not reused as an accepted producer. Batch13 is fully
restored; its read-only route inventory passed but proves no area transfer.

Bounded known issues found by native runs, with their current disposition. Resolved entries are kept so the history of each finding stays readable.

## Completed native boundary movement lost during cold restoration — PASS (October 18)

Batch 11 slot 2 found a real product defect: `RestoreParticipation` restored an
ended, non-current boundary and then overwrote its saved `MetresStepped=2.286`
with its actor allocation's zero. A resave would drift. Claude's `f1e54002` limits
that allocation copy to the current boundary and preserves the live partner copy.
Codex independently reviewed the fix, then accepted the native regression
`20261018-c7b-world41-ownerseed-b12s1-tbload` on source `6652c7fe`: the unchanged
accepted batch-11 archive restored 2.286 at both shared-world and presentation
boundaries, with native continuation, no extra preparation and verified restoration.
The original preview.40 failure stays FAIL. Focused current reader tests also reject
a reset of this value at either phase.

## Zero-mounted checkpoint reader conflates save and attack phases — PASS

`20261018-c7b-world41-ownerseed-b12s2-zerosave` remains FAIL (game PASS, reader FAIL).
The measured attacks happen while each pair is mounted; its native Dismount follows.
Rider B's own Horse legitimately paid approach move 0.0990104 to 0.7453083. The reader
used the final zero-mounted case name to prohibit that earlier paired approach.
The current correction requires scalar before/after owner/turn observations bound
to the retained owner identity/generation and exact actor roles, refuses duplicate
owner/cross-role claims, and retains native timeMoved/displacement/budget checks.
Three retained FAIL reviews found malformed-input and conflicting-role holes, now
repaired with focused negatives. Final independent review PASS and 126/0 focused
checks bind the current reader. Fresh native producer b12s4 and separate-process
cold load b12s5 both PASS on the unchanged preview.41 product and source 2b3d6702
reader, with exact restoration. Their zero-pair/two-retained-owner graph and native
continuation are accepted. The original failed producer was never reused.
A diagnostic re-evaluation never promotes the original failed run.

## RT party-brain attack proposals after a mounted pair attack (2026-10-18) — RESOLVED in preview.35

**Found by** run `20261018-c7b-world29-ownerseed-b4s2d-rt` (preview.29); the diagnosis is in lab file `analysis-cache/chunk7-continuation/owner-seed-batch04/rt-companion-diagnosis01.json`.

**What happens.** After a real-time mounted attack, the attacking pair actors keep `CombatState.ManualTarget` set to the target. Two things set it: the native click handler sets it after `Run`, and KMC's acceptance path sets it too. The native party brain then proposes a `UnitAttack` on that target about every 0.35 s.

**Two effects were recorded:**
- **Warning spam:** hidden-mount proposals raised "Select the rider to attack, or use the mount on its own turn."
- **Click integrity.** The review (Directive 4 R1) described this as a refused proposal erasing a pending click. The observed flaw was in the observation itself: it was keyed only on actor, target and frame (±1) and cleared by any routed command. A refused proposal could erase it, and a click whose own `Run` carried something else could leave it for a brain `UnitAttack` on the same target to borrow. Examples of "something else": an auto-use cast, a `CanNotAttack` move, or an earlier refusal such as a running save worker.

**Fix** (`4da0bec4`, reviewed `d4-controls-review01`):
- `ObservedPlayerAttackRequest` binds the native `OnAttackRequested` observation to the exact command its own synchronous `Run` carries. The `Run` prefix binds first, before any refusal.
- Only that command is player input, and only it consumes the observation.
- Brain proposals are refused silently.
- Real hidden-mount and inexact clicks keep one bounded warning each.

**Native acceptance:** `20261018-c7b-world35-ownerseed-b8s1-rt` (preview.35). With retained ManualTargets on rider A and Horse B and 5 proposals refused in the window:
- one real click started exactly one pair-A intent;
- it caused exactly one rider-A native attack on its target;
- no warning appeared;
- the player's Stop ended the intent.

**Remaining, by design:**
- **ManualTarget is left as native player intent.** Dismount cancels KMC's stock intent and active command, so any later attack on the retained target is a native command, never a stale KMC one.
- **A harmless residue.** A bound observation whose `Run` was refused before routing stays inert until the next click (review note N3).

## Chunk 6A rule R3 superseded; current probe versioned (2026-10-18) — RESOLVED for current entrypoints

**Decision.** The owner's October 18 decision admits a voluntary turn-based Mount/Dismount in the rider's prepared (Preparing) turn (`06ec7180`).

**What changed in `7930313e`:**
- The current 6A probe emits `CM01-combat-mount-preparing-admitted`.
- It also records `CM01-combat-mount-foreign-turn-refused` when the native order puts another actor's turn first.
- Current readers and registrations use the new row.

**History is kept.** The released preview.211 ledgers keep `CM01-combat-mount-preparing-refused` under their own inputs. `scripts/Test-Chunk6aLedger.ps1` is labelled a historical contract: it pins the 6A reader revision and already exits 1 on the successor because many Chunk 7 readers have changed. It is not a current development prerequisite.

**Native first-action evidence:**
- **First-action Dismount** (preview.34): batch 6 slots 4–5 and batch 7.
- **First-action combat Mount** (preview.35) with both adoption dispositions: `PreparePartnerThisRound` in `…b8s2-tbrider` and `RetainPartnerParticipation` in `…b8s3-tbmount`.

## Recorded combat fixtures predate the native rider-removal forfeit (2026-10-18) — RESOLVED

The `tests/Chunk7Diagnostics/OwnedPairsEvidenceFixtures.json` combat fixtures now carry the observed native paired-removal forfeit directly (`641bc0da`). They were generated, not natively recorded; the file is labelled synthetic. The hidden load-time shaping is removed. Only the code-built TB fixture keeps the labelled generator `scripts/chunk7/Chunk7NativeForfeitFixture.ps1`.
