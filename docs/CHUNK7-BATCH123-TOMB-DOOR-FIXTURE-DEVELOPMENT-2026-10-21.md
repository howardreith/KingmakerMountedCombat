# Chunk 7 batch123: Old Cyclops Tomb fixture route, development

Status: **native fixture/inventory PASS; ordinary door remains unqualified.** The standing
[completion mission](../planning/CHUNK7-COMPLETION-MISSION.md) remains active.
This route is one targeted ordinary-door fixture discriminator, not a door PASS.

The accepted mounted owner-seed Manual source is
`runtime-staging/persistence-20261018-c7b-world26-ownerseed-b3s1d-save/Saved Games/Manual_304_KMC_C7_SEED_WORLD.zks`,
SHA-256 `b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003`.
Retained stock metadata identifies a revealed, open, unexplored Old Cyclops
Tomb location `38638e43953432e42a037956f798f15e`, its actual entrance
`c23675e173391ad4297dbe3720122bca`, and non-capital area
`81b13ce8765469045864ffb878b0944b`. The native admission rechecks all
three identities and the saved location flags before dispatch. It refuses an
explicit target or replay route.

The new source-only case borrows the established Manticore transaction custody:
one fresh independent source copy, two native Dismounts, one native
`LoadArea(entry, None)`, non-capital full-party/free-control and unmounted
checks, two native Mounts, and one distinct `KMC_C7_TOMB_DOOR_FIXTURE` Manual.
The after-remount inventory reads at most 32 nearest `StandardDoor` records and
both mount positions without closing, clicking or changing a door. It records
`interactionTested=false` and cannot establish an ordinary-door PASS. The
scenario also reads Unity `Debug.isDebugBuild` once to select the next F11
measurement route. Tomb is in the closed Areas isolation selection; failed
transfer cleanup retains the preexisting source/destination custody rules.

Independent read-only review found no production, admission or restoration
blocker. It noted that the inventory has no independent remount-frame position
witness; a future door test must bind an actual native click/approach to the
chosen door, pair and start positions. No human navigation is required on the
current source evidence.

Focused offline checks: Fast 3/0 including build; Manticore/Tomb reader 75/0;
Areas isolation 37/0; owned DTO 182/0; owned source 284/0; owned manifest
143/0; owned reader 690/0; persistence evidence 29/0; persistence wiring
158/0; area source 11/0. The initial synthetic Tomb reader result 74/1 was
caused by its copied world-authority name still identifying the Manticore
Manual; that test fixture was corrected, and its later 75/0 result is separate.
The area source fixture likewise needed the existing bounded continuation
plans supplied to both producer and cold observations; its preceding failures
remain test evidence, not native results. No game process, Mods staging, live
profile write, package or save transaction was started during this checkpoint.

## One native discriminator on preview.88

The source was committed at `d945903799fa169af8ad8b5a07004db094730686`;
the exact candidate version commit is
`1fa14c63434aa7d7fa796e6e602b6eb79b8cc36e`. The delivered diagnostic
package for this run is
`C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.88-c7b-world88-tomb-door-inventory-diagnostic.zip`,
SHA-256 `4a75fa433fe2e134bbc5a3b914830098220028f49663099a61c5c794a50ea049`;
DLL SHA-256 `94ef562f1f92a5fe9f41895c6ff54e7ca07ae1e9842f15a95800d944d0386aab`,
MVID `6a49b94a-8f1f-4cd3-a66b-e0b4c3773d8c`. The previously created,
misnamed preview.87-plus-preview.88 package was never staged or run. Candidate
checks passed 6/0 in 4.34 minutes with the package build reused.

The append-only current-state suite `chunk7b-completion-b123-20261021` is
SHA-256 `6d094c85abd9bc3492f4564bacb764e1b4e7c0b5b4af299e37f6204638855b5b`.
The sole declared slot is in the private
`analysis-cache/chunk7-continuation/b123-tomb-door/declared-b123.json`;
the fresh selection copy is byte-identical to the prior admitted Manticore
owner-seed selection, SHA-256
`0a240abd3d199c470322d9f9c6268ac2cc82882f89b685c868c3df54470ae3cb`.
Scoped WhatIf passed. The first live command was rejected during parameter
binding because one suite-hash character was omitted; it acquired no lock and
started no game. The corrected invocation executed exactly one native scenario
under the approved Howie SID.

`20261021-c7b-world88-b123s1-tomb-door` passed the game and outer reader
**5/0**, with one source load and one genuine destination Manual. Runtime
result SHA-256 `b0cefe847e4b40fe8b22ed80325af46b7f76eef02bfc444def95ff20de2a696a`;
game result SHA-256 `491753cbbbad44d15b33c6ee559fd2f79b23ea651e6b446433084598d953e684`;
persistence evidence SHA-256
`159d9a206c11e59c28fd5e2b8382476db780f2a0ec8f0e874cfc9b4a10962653`.
The output `Manual_305_KMC_C7_TOMB_DOOR_FIXTURE.zks` is 1,037,548 bytes,
SHA-256 `41deb60f5f512080858c519ce7bdcb70f2af8025d79075f76e40f7027a41fba4`,
saved in the exact Tomb area. This is a prepared fixture, not a successful
ordinary door click.

The after-remount inventory found one native `StandardDoor`, named
`SecretDoorOpen`, at `(-4.93, 0.7, -10.48)`. It was ineligible as a door
candidate: `open=false`, `disableCutWhenOpen=false`, and its two mounted
actors were 29.663 m and 31.011 m away, beyond the 28 m scan radius.
`eligible=false`, `interactionTested=false`; no door mutation or interaction
was made. The distinct Spider Cave and Manticore zero-door observations remain
on their own earlier inputs. Tomb establishes no ordinary-door coverage.
`Debug.isDebugBuild=false` on the actual game player, ruling out the Unity
2018.4 development-player-only Recorder route for F11; it does not measure
allocation rate.

The transaction state is `restored`, SHA-256
`e282156fb7b153279bcb58bda4aceb99707b44883382c0c9f66f0fc106e99d0b`.
Mods digest before/after is
`b592f4009833072b60604852ad00660a6d236957712b63ae40f28ed1b7f67cda`;
protected-save inventory digest before/after is
`df434471b6f2bb3ade60ecccaa524aa7537e17f63db3d0bcce6d5a7eb70561ab`.
Baseline immutable, Working restored, save allowlist and save protection passed.
Original live `Areas` was restored exactly, profile bytes are exact, and there
are zero accepted cache exceptions. No Kingmaker process or runtime lock remains.

Continue the ordinary-door fixture search without replaying these three
unchanged scenes. Continue F11 through a separately reviewed allocation-event
route and the remaining progression/profile/performance work under the active
standing mission. No 7E final-package claim follows from this development run.
