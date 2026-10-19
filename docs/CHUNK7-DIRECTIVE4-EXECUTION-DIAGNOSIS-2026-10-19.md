# Chunk 7 Directive 4 execution and fixture diagnosis — 19 October 2026

## Batch34 Working diagnosis and exact recovery

The owner-authorized effective execution context is `DESKTOP-SRJJ623\Howie`,
SID `S-1-5-21-1108991106-2476425705-1721024090-1001`, medium integrity.
It can stage and restore through the existing guarded transaction, as the
slot1 owner-seed control and later failed-run restoration demonstrated. This
does not prove that the earlier sandbox account's denied directory move was
fixed. The batch34 environment record is distinct from earlier accounts and
fixtures. The exited game process SID was not retained, so these comparisons
name the guarded runner, profile and staging context rather than asserting a
separately observed game-process identity.

The frozen preview.52 base-only Working observer in batch34 slot2
(`20261019-c7b-p07-howie-base-working-b34s2`) failed its Harmony patch
signature with `Invalid return type Boolean ... LoadRoutine.MoveNext_Patch0`
before any Working load. It made zero loads and saves and has a separate
original game/runtime FAIL. Its Mods, protected saves and profile restored.
A corrected command followed one mistyped package-path invocation that had
failed before transaction creation; that invocation provides no game result.

The existing load-only `fixture-intake` route in slot3
(`20261019-c7b-p07-howie-base-fixture-b34s3`) made one request for the same
legacy Working archive SHA-256
`5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`
and wrote no save. Native `Player.PostLoad` again threw
`InvalidOperationException: Sequence contains no matching element` at its
first `Single` lookup before the exact Working load callback. This run used
the Howie context and base-only staging that successfully loaded the distinct
qualified owner-seed checkpoint in slot1. It therefore distinguishes a
Working-specific prerequisite from a broadly broken load environment in
this context. The October 12 base-only Working failure and installed-mods
Working world PASS strengthen the mod-dependency hypothesis, but product
versions, installed sets and configuration also differed. Neither the
missing main-character entity's origin nor any particular mod is proved.
The slot3 original game/runtime result remains FAIL (0/1 assertions), not a
P07 cleanup verdict. No cleanup save or DLL-absent load occurred.

Slot3's guarded Mods and protected-save transaction restored digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`
and inventory digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
The profile guard retained the lock after seven newly written `Areas` files,
an admitted base-only UMM rewrite and changes to exactly three native startup
PlayerPrefs keys. The repository-owned exact-run
`scripts/runtime/Recover-KmcB34S3Profile.ps1` binds the failed run, Howie SID,
original snapshot, seven file hashes, startup settings hashes, combined
transaction state and current Mods/save digests. It preserves the seven files
under that run's backup `Areas-after`, writes only the verified saved UMM
bytes and original three registry values, and supports an interrupted
quarantine/settings restore without broadening its accepted pins. WhatIf,
independent source review and focused positive/negative checks passed before
actual: exact script SHA-256
`c4b25dc031ff96494647a60019ac556790389b5f6fa5e3fd2227de288ec085ef`,
review receipt SHA-256
`67eb362b8adddfa0d6045a74043c826d2d882d43cabfa11b238eb64aa6a6ae82`,
34/0 synthetic settings-state cases, actual seven-file inventory positive,
closed-inventory negative, and parse 0. Earlier source and test failures
remain preserved. The actual recovery passed. Independent read-back confirms exact
profile, UMM, PlayerPrefs, Mods and protected saves; the lock and game are
absent. The profile snapshot SHA-256 was
`37ff7aa3d9fa0a18957d7ce080369671002d518fcf003b3c7c7fb5a742419c14`.
The original slot3 FAIL and `restoration-blocked` orchestration record remain
unaltered; the separate recovery receipt records restoration, not gameplay
success. Batch27's accepted cache changes retain their own non-exact status.

The next P07 route must change an actual prerequisite: identify a compatible
legacy Working dependency set, or qualify a reference-free positive-cleanup
derivative of the owner seed with a supported stock companion. Another empty
zero-pair load cannot prove positive mounted cleanup. The schema-2 pending
debt and area destination questions below remain independent. No product DLL
changed in batch34, and no new build is warranted by this external recovery.

Status: IN PROGRESS. The original read-only diagnosis used pushed
`e67dafa4ce53a23a1a41573b9c6a40a0d1ebe73b`, retained private receipts,
the exact installed Kingmaker assembly and then-current filesystem state.
The historical batch33 update below records a guarded prelaunch FAIL and its
then-active recovery blocker; the next section supersedes that live state.
The original batch32, batch33 and schema-2 results remain FAIL.

## Batch33 recovery and batch34 execution context

After the owner enabled full access, `whoami /user` identified the effective
runner as `DESKTOP-SRJJ623\Howie`, SID
`S-1-5-21-1108991106-2476425705-1721024090-1001`, at medium integrity.
This is the account used for batch32 slot2 and differs from the sandbox
runner ending `-1005`. It does not prove that the sandbox directory-move
permission was repaired. The owner explicitly authorized continuation in the
new access context. No credentials were inspected.

The repository-owned `Recover-KingmakerRuntimeTransaction.ps1` passed exact
stale-state WhatIf, then actual recovery of
`20261019-c7b-p07-sandbox-fixtureload-b33s1` under Howie. The durable combined
and Mods transaction phases are `restored`; the exact 80-file pre-run Mods
digest is `b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`,
and protected-save inventory before/after is
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
Baseline and Working are immutable/restored, the latter SHA-256
`5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`.
The lock is absent and no Kingmaker process remains. The failed run stopped
before game launch and had no profile/registry snapshot or mutation; this
generic recovery touched Mods and saves, not another user's profile or hive.
Its original runtime result remains FAIL.

The next batch declared one native control under this Howie environment:
`20261019-c7b-p07-howie-zero-control-b34s1`. A byte-identical copy of the
already accepted batch30 zero-mounted selection (copy SHA-256
`8424ce6a6606f17053ae8535db6225f5b914f1600b72457c057941328c3c3aef`)
selected its immutable checkpoint SHA-256
`692e17881b61db0bb1aefb0f20fb5d545540782896934a71ced13f56e7a1a3a4`.
The frozen preview.52 DLL SHA-256 was
`18f619bd28e872b73470944aaf46671887d91d99ff26f80f30a8fd50019a1433`;
the package and suite were the pinned `head7348` inputs. Development-scoped
WhatIf passed. Actual guarded base-only execution passed 3/0 native assertions
(pair identity/no-prepare, native combat continuation and cleanup), one load
and zero saves. The game reached the checkpoint world under this account.
The run/Mods transactions restored the same digests above; the profile receipt
reports exact bytes restored and zero accepted cache changes. The lock and
Kingmaker process are absent. The retained game process PID was 44488; its
Windows SID was not captured, so the runner SID is not silently assigned to it.

This changed-input control rules out a broadly broken native load environment
for the current runner and frozen candidate. It does not settle the different
legacy Working archive: batch32 used the current six-mod installed clone,
while batch34 used base-only staging and a qualified owner-seed-derived save.
The next useful P07 experiment must isolate the Working archive and mod-set
requirements or use an honestly qualified reference-free positive-cleanup
derivative. Repeating batch32 slot2 unchanged is not selected. Schema-2's
native re-entry debt failure remains independent and unresolved; the original
batch19 FAIL and all earlier PASSes retain their own input identities.

## Batch33 slot1 update: managed permission still inactive

The owner reported adding the installed-game parent as a managed writable
root. Under the existing `CodexSandboxOffline` session, a scoped guarded
WhatIf for `fixture-intake` passed against the unchanged preview.52 package,
suite and legacy Working. The actual single declared run
`20261019-c7b-p07-sandbox-fixtureload-b33s1` then failed **before game
launch**, again after moving the original Mods tree into its exact transaction
backup: moving `Mods-ready` to the absent live Mods path returned Access
Denied, as did moving `Mods-original` back. The same-account guarded recovery
passed WhatIf but actual recovery was denied at that rollback move. The
original run remains FAIL; it did not test the Working load. No game process
was observed, Working retains SHA-256 `5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`,
the live Mods root is absent, and the exact original tree remains in
`runtime-backups/20261019-c7b-p07-sandbox-fixtureload-b33s1/Mods-original`.
The durable Mods state is `original-moved`, before digest
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`;
an independent read-only custody check matched that digest and all 80 files
after the failed recovery, and observed protected-save metadata digest
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
The private custody receipt is
`analysis-cache/chunk7-continuation/candidate-preview52-head7348/b33s1-custody.json`
(SHA-256 `6b64694273c2b57d1e1a08cc8c34c77f74136d3db29f9f126374284fa83ee0a1`).
The transaction lock is retained. The requested managed parent root is not
effective for this active session. No further native launch is safe until the
root is effective and the same-account guarded recovery completes and verifies
all exposed surfaces. The owner has been asked to activate that exact root;
there is no request to change ACLs, run the game as another identity, or move
Mods manually.

## Execution boundary

The current branch is `codex/mounted-combat-chunk7-successor` at `e67dafa4`;
the tracked remote ref matched and the worktree was clean before this note.
Other worktrees were inventoried read-only and left untouched. The transaction
lock is absent, no Kingmaker process was observed, and live Mods exists.

The earlier sandboxed runner and batch32 slot1 used
`DESKTOP-SRJJ623\CodexSandboxOffline`, SID
`S-1-5-21-1108991106-2476425705-1721024090-1005`, medium integrity.
Batch32 slot2's elevated runner used `DESKTOP-SRJJ623\Howie`, the same SID
prefix ending `-1001`. This is a changed runner account, not merely a changed
elevation level. The game process SID was not captured in the retained receipts;
the Steam-mediated launch and the profile path do not prove it. Slot2 recorded
Kingmaker PID 56248 and Steam PID 1920, both now exited. The game executable is
the installed Kingmaker 2.1.7b; the guarded script requested Steam `-applaunch
640820` with a hash-pinned KMC request and transaction token. The isolated
Working copy was under
`runtime-staging/persistence-20261019-c7b-p07-head7348-b32s2-removal/Saved Games`.
The protected live save root was
`C:\Users\Howie\AppData\LocalLow\Owlcat Games\Pathfinder Kingmaker\Saved Games`;
the profile snapshot uses its parent and UMM `Params.xml` under the installed
game. PlayerPrefs are read from HKCU `Software\Owlcat Games\Pathfinder
Kingmaker`; which user hive the exited game process used is not proven by the
retained process records. No credentials were inspected.

Slot1's `launcher-failure.json` records failure after the original live Mods
tree was moved to `runtime-backups/<run>/Mods-original`. The forward
`Move-Item` from `runtime-staging/<run>/Mods-ready` to the now-absent installed
`Mods` path was denied; rollback `Move-Item` from `Mods-original` back to live
`Mods` was also denied. PowerShell recorded `Access to the path ... is denied`
for each source path, without a Win32 error code or sharing-violation evidence.
The installed game parent ACL grants `BUILTIN\Users` full control, while the
Mods and lab transaction roots grant `CodexSandboxUsers` Modify; the managed
sandbox permits writes to live `Mods` but not to its installed-game parent.
This explains why file-write or WhatIf success does not establish the two
directory moves. No manual move or permission experiment was performed.
Guarded recovery restored the original Mods and Working after slot1. Slot2's
combined transaction restored Mods and saves, and its separately reviewed,
exact-run recovery restored UMM parameters and three observed PlayerPrefs
deltas and released the lock. Batch27's accepted cache changes remain a
separate exception to exact profile-byte equality.

The smallest supported owner action for another sandbox-context native run is
to add exactly `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder
Kingmaker` as a writable root for this `CodexSandboxOffline` session. The
existing lab transaction roots remain writable.
This is a managed sandbox boundary request, not an ACL reset or a request to
operate the game as administrator. Only the repository-owned runner may stage
or restore Mods; the root addition alone is not native qualification. Its first
approved transaction must verify both forward and rollback moves and all
current-state restoration under that same account. A WhatIf cannot prove them.

## P07 Working load

The exact Working archive SHA-256
`5eb4e0b4cbd8d60dc879a02ff71aadfde3f517304754857f0cc68d0f9a93f1c6`
was selected in all three relevant runs. October 12 base-only preview.3 made
one Working load request and failed at native `Player.PostLoad`, whose first
`Single` lookup found no cross-scene entity matching the main-character ID.
The October 12 installed-mods comparison loaded the same Working archive into
the world, then failed later on a ground-click assertion. Batch32 slot2 again
made one Working load request, zero saves, and failed at that same native
post-load lookup before P07's callback. Its current `output_log.txt` reports
no KMC metadata in the legacy archive and contains the complete native stack.

All three runs used Kingmaker 2.1.7b, the same `Assembly-CSharp.dll` SHA-256
`3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`
and the same Harmony12 DLL SHA-256
`aa1cd48317254985d8b700cc74953477d1b40c3022ce9aa4c95ed2b8327e1292`.
The successful October 12 comparison loaded Call of the Wild and Craft Magic
Items as well as Bag of Tricks, Cheat Menu, Buff Planner, KMC and Skip Intro.
Batch32 loaded Bag of Tricks, Cheat Menu, Buff Planner, Dice Roller, KMC and
Skip Intro; Call of the Wild and Craft Magic Items were absent. Buff Planner,
KMC and UMM binary identities also differ. The exact game-process SID and
whether any missing blueprint/type binding caused the absent entity remain
unrecorded. The mod-set difference is a strong diagnostic lead, not a proven
cause; the changed runner account is another unisolated variable. A clean
owner-seed load in the chosen context could establish broad environment
viability, but cannot certify Working. Once execution is settled, prefer a
one-load/zero-save Working control under the chosen identity/configuration
before repeating the P07 cleanup sequence. If Working remains unsuitable,
investigate an honestly labeled, reference-free derivative of the already
qualified `KMC_C7_NO_MODS_OWNER_SEED`; a zero-pair load alone cannot prove
positive cleanup or genuine DLL absence.

## Schema-2 pending-only debt

The batch19 schema-3 producer wrote a genuine current archive (SHA-256
`1af73c83c1ce7f9eca660619db2d6390aa40f66fbb3f4237703a9003c994ebb2`).
The guarded historical copy changed only `kmc-mounted-state`: its 22 other ZIP
members have identical names, lengths and hashes in the pinned derivation
receipt. The schema-2 derivative is SHA-256
`8e50a00ffb4ad687fe4781129df48815d52b2acf15866f3c657d2afe14fc8477`;
receipt SHA-256
`c0ad5a9ee1ed51fdf80bb41cdaa2f3581847805ab44546f3b00bd809d0b82906`.
The metadata projects one pending owner and retains the source combat actor
debt. This proves the native ZIP members were held fixed, not that the schema-2
loader reproduces every runtime behavior of schema 3.

The native schema-3 producer itself has one pair, one pending participation,
and seven combat actors. Its rider A is already `InCombat=false`,
`Prepared=false`, with 6/3/6 debt. The schema-2 projection keeps that combat
record and all native ZIP members; it changes the mounted metadata envelope
and deterministically derives the historical owner identity. KMC's only
`JoinCombat` call in actor restoration is conditional on saved `InCombat`.
It therefore does not call `JoinCombat` for this rider while restoring the
saved debt. The observed join comes later, after KMC's presentation restoration
completed and while native callbacks were passing. This narrows the failure
to native re-entry/preparation after a truthful restore, while leaving open
whether migration changes the later lifecycle compared with schema 3.

Batch19's cold restoration trace shows rider A saved outside combat and
`Prepared=false` with Standard/Move/Swift 6/3/6. `RestoreDebt` restored all
seven saved native fields. Later in frame 3839, native `JoinCombat` changed
that rider to in-combat while preserving 6/3/6, and native
`UnitCombatPrepareController.Tick` then changed it to `Prepared=true`,
Standard/Move/Swift 0/0/0 and Initiative 0.5. The actor's saved KMC allocation
still names round 1 and grant `b22af3966b1a428dbf3642a984bde9cf:1`;
the evidence does not show a new round or a new KMC grant. Installed native
`Prepare.Tick` clears cooldowns for unprepared in-combat actors. Thus the
selected pending-only continuation conflicts with native re-entry during the
same retained allocation. The current evidence does not decide whether this
native join is a lawful fresh entitlement outside KMC's saved turn contract or
an ownership/order defect that KMC can prevent without fabricating resources.
No product fix or assertion relaxation is justified yet. The next bounded
discriminator, once native execution is available, is an exact cold load of
the unchanged schema-3 source under the same candidate and context, compared
with the schema-2 derivative's existing trace. If the same re-entry occurs,
investigate shared restore/turn ownership; if it does not, investigate the
historical projection/loader path. Keep the original schema-2 FAIL either way.

## Area fixture and remaining endpoint

The qualified Spider Cave source's recorded locations include Spider Cave and
Oleg's Trading Post as open/explored; Oleg is recorded as visited. Batch23's
actual native `LocationData.AreaEntrance` resolved to Oleg and
`destinationIsCapital=true`; the strict preflight made zero transfer and zero
save requests. No admitted visited non-capital destination has been observed
on this source. Area Auto/cold remains IN PROGRESS on a fixture prerequisite;
doors remain DEFER — EVIDENCED for the current route. No owner seed master,
human save, foreign mod, or runtime setting was changed by this diagnosis.
