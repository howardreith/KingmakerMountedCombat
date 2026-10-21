# Chunk 7 batch123: Old Cyclops Tomb fixture route, development

Status: **reviewed offline; native fixture/inventory result pending.** The standing
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

Next: commit and freeze a candidate, perform scoped preflight, then run at most
one native Tomb preparation/inventory case. If no qualifying ordinary door is
observed, keep the door gate open and continue the standing mission's other
selected work. Preserve exact restoration and all older negative scene results.
