# Chunk 7 schema-2 native resave checkpoint

Status: IN PROGRESS. The selected cold migration now has a native schema-3
resave path. Focused checks and independent correction review pass; native proof
remains required. The sustained 7B–7D mission continues.

The previous cold case restored and continued its migrated graph without making
a native schema-3 save. The original reader also accepted evidence with that
resave omitted. Its actual failing regression is retained.

## Implemented behavior

For cold legacy-mounted-pending-tb-manual only, retain the restoration entry
observer and owned pause after complete native restoration. Verify exact owners,
slots, actor debt and zero Prepare/new-round entries, then request one fresh native
Manual save before continuation. Historical Manual_300_KMC_C7_WORLD.zks remains
read-only. Separately authorize Manual_301_KMC_C7_SCHEMA2.zks with exactly one load
and one write. Source-case schema-2 writing and the schema-1 path are preserved.

Require callback, operation/worker/fence settlement, actual schema-3 archive
readback and exactly one snapshot. Validate both complete graphs before comparing
owners/generations, pending/split release rounds, contexts, allocations, reactions,
controls and actor expenditure. Only bounded elapsed native cooldowns and
nonsemantic collection enumeration order may differ. Preserve the original
migrated graph as the continuation baseline. Cleanup retains unfinished ownership
and verifies the input again.

Readers require the migration/write/readback/before-continuation observations,
scalar identity tokens and exact isolated output path. Both closed registries
admit the emitted success row and reject undeclared neighboring rows. No resource
or progression grants, command replay or native Prepare call is introduced.

## Focused verification

Commands used Windows PowerShell -NoProfile -NonInteractive -File. Private
supervisors preserve exact child arguments, exit codes, durations and log hashes.
Build command: MSBuild KingmakerMountedCombat.sln /t:Build
/p:Configuration=Release /v:minimal /nologo. Production source count: 451.

| Selected check | Result | Seconds |
| --- | --- | ---: |
| Final Release compile | PASS / exit0 | 2.7045608 |
| Test-Chunk7Schema2Resave.ps1 | PASS 26/0 | 1.0580688 |
| Test-Chunk7OrdinaryWiring.ps1 | PASS 54/0 | 6.0728683 |
| Test-Chunk7PersistenceCleanupAdapter.ps1 | PASS 94/0 | 1.5550468 |
| Test-Chunk7CombatPersistenceSource.ps1 | PASS 29/0 | 24.6146440 |
| Final Test-Chunk7SaveBoundaryProtocol.ps1 | PASS 43/0 plus 2 metadata checks | 1.7667846 |
| Final Test-Chunk7PersistenceWiring.ps1 | PASS 113/0 | 4.4405439 |
| Final Test-Chunk7CombatPersistenceObservation.ps1 | PASS 70/0 | 21.4721367 |
| Final Test-Chunk7Schema1Persistence.ps1 | PASS 102/0 | 5.4340851 |

Four affected neighbors completed in 35.7349547 seconds. The five-stage correction
run completed in 36.0352895 seconds, actual exit0. Imported reader/test counts overlap
and are not summed. Continuity executes the actual production comparer and DTO
validators; cleanup uses actual methods with explicit external native substitutes.
Source admission reads generated private ZIPs under the owner's archive approval.
These checks prove no Unity save callback, restoration or gameplay. No candidate,
package or native qualification ran for this checkpoint.

## Retained failures and independent review

The original reader accepted an omitted native write: actual exit1,
10.4681042seconds. The first focused run retained two setup failures: the isolated
DTO executable lacked its installed Unity dependency, and the extracted quota
test lacked the existing native-bootstrap selector variable. Affected repairs
pass; neither was a production save defect.

Independent read-only review found two real integration defects in the first
frozen 14-path subset: the success row was missing from both result registries,
and new identity fields admitted matching one-element arrays. Actual consumers
reproduced both before repair: two registry FAIL results and one reader FAIL for
accepted array-input-hash. Original sources, logs and receipts remain intact.
The corrected 18-path subset has independent PASS with no remaining source blocker.
The reviewer checked hashes, diffs and recorded logs, without rerunning tests,
accessing archives or writing receipts. Root records that communicated review.

Private root: analysis-cache/chunk7-continuation/schema2-resave-next.

| Binding | SHA256 |
| --- | --- |
| checkpoint-evidence01.json | 2164ab1d5d90da5c0a68590825efe38f9a802c65c3c82ee0c611ca52738741d5 |
| review02/subset02.json | 38319142de657793425db144baac9d4aa75e9c4a1b974e3c7173da6260184e2b |
| review-repair01/focused01.json | fe3adfb2dcb493132882293b4380fdd66dfcba8a8b0312940a261627457f2f21 |
| review-repair01/caller01.json | e3e117a62e533d9209264070eef5b2cf3a37408e7223242c839836adc2ed3fde |
| review-regression01/focused01.json | ea9e506bc98557288ab68a3bb860b43af40f3961029cefb115cb3ce6de94da68 |
| array-regression01/focused01.json | f3886b2f3ce88df83a2d5638759a20b1cf71f50726ee382bc7d90367b823e067 |

Parent: bc79eed9e14d304efa7e066e7b86136fbcbe5c9e on
codex/mounted-combat-chunk7-foundation. Commit 206cd79a88b7b504cdc3994dab9dbb340ccfd2b4
is guarded-pushed and remote-verified.
Frozen preview.5 remains unchanged. All four prior native attempts remain FAIL
and fully settled; no game/transaction is active.

Next: integrate reviewed cumulative owner1→owner2 acquisition
and typed owned-two-pair intake, then qualify a fresh private candidate. Its
fixture chain must produce a qualified schema-2 mounted/pending-only input,
cold restore it, natively resave schema 3 and continue without free preparation
or delayed release. The owned-fixture branch needs its actual next Manual number
and must preserve historical 300→301. Native Small, six-pair, progression and
measured hardening remain selected in the [active plan](../planning/CHUNK7-IMPLEMENTATION-PLAN.md).
