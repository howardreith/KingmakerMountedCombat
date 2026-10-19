# Chunk 7 Directive 4 execution and fixture diagnosis — 19 October 2026

Status: IN PROGRESS. This is a read-only diagnosis against pushed
`e67dafa4ce53a23a1a41573b9c6a40a0d1ebe73b`, retained private receipts,
the exact installed Kingmaker assembly and current filesystem state. It does
not change a native verdict, qualify an execution context, or select another
game run. The original batch32 and schema-2 results remain FAIL.

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
