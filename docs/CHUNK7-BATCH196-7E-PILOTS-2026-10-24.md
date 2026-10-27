# Chunk 7 batch196: 7E development pilots, fourth pass

Status: **stopped on a diagnostic defect; every run restored exactly.** Nine native runs on one frozen
candidate (`241bbd8b`). The acquired chain (c1–c5), the X4 Manual producer and the **X4 three-area cold
pilot on the Manticore route passed**. Row 58 (capital arrival) failed closed on its first native
assumption: the product dismounted the companion pair correctly, but the arrival fixture's roster check
refused because native Oleg's takes the hired companion pair out of the scene. Row 57 failed on a
diagnostic counting defect (null paired identities counted as present). Per the standing rule the batch
stopped there: row 59, the two-pair native-life pilot and the Small presentation item pilot did not run.
Development pilots only; not 7E qualification. The standing mission remains IN PROGRESS.

## Candidate (development pilot)

| Item | Identity |
| --- | --- |
| Source | `241bbd8b`, frozen worktree `worktrees/chunk7-7e-b196-source-241bbd8b` (LF re-materialized), on the local branch `codex/mounted-combat-c7e-b196-pilot-source` (not pushed) |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.94-c7e-b196w-pilots-diagnostic.zip`, `043990e8…`, 3,684,106 bytes; `Info.json` (329) and the DLL (7,973,376) only |
| Manifest / DLL / MVID | `a35d6acf…` / `5a34f1c3…` / `11bce9bd-75f4-4206-bb03-6912417bbc56` |
| Candidate tier | 6/0 (3.97 min) in the frozen worktree on that build, receipt `b196w-candidate-01.log` |
| Suite | `chunk7e-b196-pilots-20261024`, `7beb6e87…` (save digest `4f6375da…`, Mods digest `ed57b9d2…`) |
| Root `bin\Release` DLL | `5a34f1c3…` before and after (equal to the package DLL; the worktree build did not touch it) |
| Helpers | `analysis-cache/chunk7-continuation/b196-stage.ps1`, `b196-run.ps1`, `b196-native-launch.ps1`, `b196-derive.ps1` (mechanical re-pin), `b196-check.ps1` (post-run check), `b196w-freeze-step.ps1`, `new-suite-b196.ps1`; freeze record `b196-freeze.json` (`f547f9af…`) |

**Why the worktree branch.** The first package was built from the root checkout at `241bbd8b`
(`…c7e-b196-pilots-diagnostic.zip`, `c93094ac…`, same DLL `5a34f1c3…`; Candidate 6/0, receipt
`b196-candidate-01.log`). While its Candidate tier ran, the root branch fast-forwarded to the doc-only
commit `4309e7cd` (X6 record by another agent). The suite generator binds the package manifest to the
root's current HEAD and refused (`b196-suite-dryrun-00-root-refused.log`), and every run would refuse the
same way. So the candidate was rebuilt, tier-tested, suite-frozen and executed from the frozen worktree on a
local branch at the same commit. The rebuild produced the identical DLL. The root package is kept as a
superseded artifact. One helper launch (`b196w-package-01`) failed before running anything because of a
path typo in the helper; it is kept.

Every run passed its development-scoped WhatIf first. The selections for c2–c5 and x4s were derived
mechanically by `b196-derive.ps1`, which reproduces all five b195 selections byte-for-byte from the b195
receipts (checked before use); only lineage pins and minted actor ids change.

## Runs

| Batch | Run (`20261024-c7e-b196-…`) | Verdict | Key evidence |
| --- | --- | --- | --- |
| A | `c1-ownerseed-cold` | **PASS** (4/0, 1.6 min) | artifact `81782351…` |
| A | `c2-ownerfield` | **PASS** (7/0) | Manual 304 `38daa64f…`; new Horse `1c7727cb…`; artifact `4b1e1128…` |
| A | `c3-acquired-cold` | **PASS** (8/0) | Manual 305 `dd7f9dd7…`; artifact `47dad8f4…` |
| A | `c4-cumulative` | **PASS** (7/0) | Manual 306 `09036dea…`; Small Ranger `f3a56b5f…`, Horse `68bc3719…`; artifact `300a983d…` |
| A | `c5-acquired-postfield` | **PASS** (8/0) | Manual 307 `9baf5228…`, artifact `2903b434…`; two owners, seven-actor field |
| B | `x4s-mounted-save` | **PASS** (5/0) | persistence artifact `ef856638…` |
| B | `x4c-three-area-cold` | **PASS** (2/0, 3.6 min) | artifact `594b5bf6…`; see below |
| B | `r58-capital-arrival` | **FAIL** (1/2) | native assumption 1 falsified; see below |
| C | `r57-ownerseed-life` | **FAIL** (8/1) | diagnostic defect; see below |

After every run (PASS and FAIL alike): outer result Mods restored, save protection passed, Working
restored, Baseline unchanged, write allowlist passed, restored save inventory digest `95fabf2e…`; the frozen
external `Test-RuntimeResult` (with its case readers) read every result without error; `Params.xml` and
PlayerPrefs (including `KingdomDifficulty`) equal the run's own pre-launch snapshot and
`Assert-KmcPersistenceProfileUnchanged` passed with zero admitted changes; no Kingmaker process and no
transaction lock remained. Both FAIL runs exited through the bounded retained-failure exit; no recovery was
needed.

Not run: row 59 (needs row 58's Manual), the two-pair native-life pilot (same diagnostic code as row 57, so
it would fail the same way), and `compiled-owner-seed-fixture-small-horse-presentation-rt` (stopped by the
defect rule; its selection is unchanged from b136).

### X4: three area transfers PASS on the Manticore route

Route `dfbc2423…`/`314e8c3b…` (Manticore Lair) → `00becd53…`/`b10e8651…` (Spider Cave) → Manticore Lair.
The repeated-world record holds four worlds (Spider Cave, Manticore, Spider Cave, Manticore), three
transfer requests, both pairs active with two boundaries and two participants in every world, no failed
construction, no residue beyond the paired presentations, three suspensions, input archive unchanged and
a settled final world. The Horse subscriber fix (b194) and the capital-free route (b195) both hold.

### Row 58: the product retired the companion pair, the fixture refused the roster

The cold load restored both pairs (4 semantic restores, 2 presentations, no Mount, no preparation). One
native transfer to Oleg's (`fd225a93…` → `ead426a6…`, autosave None) then settled with
`destinationIsCapital: true`, mode Default, **retired 1, failed pairs 0, active owners 1, save allowed**, and
the UMM line exactly as designed: "Dismounted on arrival because the companion is not directly controllable
in this area: KMC_C7_20261024-c7e-b196-c4-cumulative and Horse. Mount again after leaving. 1 of 2 mounted
relationships restored without Mount or preparation." So the owner-decided product behavior worked
natively on its first exposure.

The run then failed in `Chunk7OwnedPairFixture.AdoptCapitalArrival` → `RequirePreserved`: "Inherited complete
native field roster changed; captured actors cannot be silently removed, replaced or moved remote." The
recorded arrival world explains it: the main Ranger (`73681ef2…`) and its Horse are in game and directly
controllable; the hired Small Ranger (`f3a56b5f…`) and its Horse (`68bc3719…`) are **`inGame: false`** and
not controllable. **Native assumption 1 ("arriving in Oleg's keeps the full field roster in game") is
false** for this hired companion; native Oleg's moves it out of the scene. This is a fixture expectation,
not a product refusal. Cleanup could not adopt the arrival world for the same reason, kept custody, and the
bounded owned-persistence exit (`d0f842a0`) quit the game normally. Evidence:
`chunk7-persistence-cleanup-pending.json` (no Manual written).

Open for review (not changed here): whether the capital arrival may admit a hired companion pair that native
Oleg's takes out of game (an exact "moved remote by the capital" classification) and how row 59 then
treats it. Assumptions 2–7 were not reached.

#### Offline follow-up: the out-of-scene rule (not natively run)

The fixture now admits exactly the retired pair (inherited pair B, the companion and its reciprocal pet)
as out of the scene, only while the loaded area is a capital, and records it
(`Chunk7OwnedPairFixture.CapitalRoster.cs`). Both units must be `IsInGame` false and not directly
controllable; the main character stays `Actors[0]`. The pair's native roster membership was not recorded by
b196, so the rule accepts one closed, recorded shape: **field** (still in Party/PartyCharacters, not remote),
**remote** (out of the roster, the rider is a remote companion) or **absent** (out of the roster, not
remote); a pair both in the field and remote, or split, is refused. Every other captured actor keeps the
exact roster contract, and inherited fact, rank, progression, money and receipt custody are unchanged.

- Row 58 records `capitalArrival.retiredOutOfScene` (ids, shape, per-actor inGame/inState/control/view,
  `liveRigValidated: false`) and a per-actor `roster` row on every capital world row. Retirement keeps
  every observable check: not controllable, reciprocal, finite positions, original views (row 58 arrives
  with them), no parenting, no override, no rider agent. The main pair must be in the scene.
- Row 59 binds the companion pair by its selected ids from native state, requires it out of the scene,
  records `capitalCold.outOfScene` and skips readiness, PartyCharacters/remote and pose checks only for those
  two units (explicit in the bind). A view that a capital cold load never spawned is recorded
  (`viewPresent: false`) and only its view checks are not applied.
- The readers require the exact observed shape: B `inGame: false`, A `inGame: true`, everything in game before the
  transfer, the record equal across arrival/bind and cleanup, view presence equal to the record, and roster
  rows consistent with the shape. `Assert-KmcChunk7CapitalArrivalShape` passes the b196 r58 arrival evidence.

Assumption re-examination against b196 r58: 1 falsified (now the out-of-scene rule); 3 (exactly the main
rider's two slots), 4 (exact UMM line, Default mode at adoption), 6 (main-pair movement authority shape) and
7 (retirement re-validation) confirmed; 2 (cleanup selection) and 5 (row 59 control rule) unobserved.
Open: the pair's roster shape (the next pilot records it), whether other story companions also leave the
scene in a capital (they stay under the exact roster rule; the roster rows will show it), and whether a
capital cold load spawns the out-of-scene views.

### Row 57: null paired identities are counted as present (diagnostic defect)

The pilot reached the new order-derived check. Native next-round order put the healthy rider first and
the affected rider before its mount (`riderSlotsBeforeRenewal` 0, `expectedRiderSlots` 1). The incapacitated
rider `fd213210…` got exactly the derived native turn in round 3: one Prepare entry and exit, one end, no
forced end, no forbidden boundary, status Preparing, Standard 6 after the end, one `native:` grant
identity, unconscious and alive. The check still failed with `"pairedIdentities":41`.

Cause: `UnconsciousNativeTurn` (`Diagnostics/Chunk7CombatNativeLife.cs`) counts window events whose
`state.pairedGrantIdentity` token is non-null and not of type Null. The trace writes that field from a
`string` (`NativeActorAllocationTrace.cs`, `["pairedGrantIdentity"] = actorCombat?.PairedActivationIdentity`),
and Newtonsoft's implicit `string` conversion makes a null string a `JValue` of type **String** with a null
value. So every one of the 41 window events counted, although the serialized trace shows
`"pairedGrantIdentity": null` on all of them. A fix would test the value (`string.IsNullOrEmpty((string)token)`)
rather than the token type; the external reader parses JSON, where the same field is a real Null. Not
changed here (diagnostic product code; needs review). The unmounted control (`8265f8cf…`, slot 3 before the
survivor's slot 5) was selected and damaged (37) but its comparison never ran. The two-pair native-life
pilot uses the same check and would fail identically, so it was not run.

## Next

1. Review and fix the row-57 paired-identity count (value, not token type), then re-pilot row 57 and the
   two-pair native life on a new candidate.
2. Decide the row-58 roster rule for a hired companion that native Oleg's moves out of the scene, then
   re-pilot rows 58 and 59 (row 58 needs an x4s producer on the same candidate).
3. Run the Small presentation item pilot (selection unchanged from b136).
