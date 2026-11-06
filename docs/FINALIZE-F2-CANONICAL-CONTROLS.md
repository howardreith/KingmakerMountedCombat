# F2 — one persistent mount control and ordinary Charge

Requirements ART-02, CTRL-01, CTRL-02, CTRL-03. Base: main `66e96f90` (merge of F1 PR #16, head `8c7d3722`). Branch `codex/kmc-finalize-f2-canonical-controls`.

## Identity and dispatch table (traced before editing)

| Identity | GUID | Before (preview.103 / F1) | After (F2) |
|---|---|---|---|
| Stock Charge | `c78506dd0e14f7c45a599990e4e65038` | Refused for a mounted pair with "While mounted, use Mounted Charge from the rider's abilities." | The one public Charge. Unmounted: untouched native. Mounted rider: availability, reason and target checks come from the mounted Charge evaluator, and the slot click selects the hidden mounted shell. The mount itself stays refused. A stock Charge command that reaches admission for a mounted pair (AI, auto-use, or one queued before the rider mounted) is cancelled, never converted. |
| Mounted Charge shell `KMC_MountedChargeAbility` | `d79eaec224a7a832e738eb81baef9d49` | Public ability in the rider's drawer; unknown-key name and description in preview.103 (F1 added the keys) | Hidden delivery shell, still leased to the rider while Mounted Charge is enabled. Shown as "Charge" in logs and target selection. Live and saved old slots are repaired to stock Charge. |
| Mount / Dismount `KMC_MountStateAbility` | `26151090ad4744ea96dbb7e0537a4a7f` (new) | — | The one public mount-state control. Leased whenever the unit owns a supported mount or rides a mounted or faulted pair, so it does not vanish when the state changes. Its availability and reason mirror the current Mount or Dismount decision; a click runs the current hidden shell through the native slot path. It never executes itself. |
| Mount Companion `KMC_MountCompanionAbility` | `f053faad986631688defa003cd7bda0e` | Public, leased only while unmounted | Hidden delivery shell; lease, targeting, approach and Move cost unchanged. |
| Dismount `KMC_DismountAbility` | `3af2b81f4d72bbb30501fa730fcdf36e` | Public, leased only to the mounted or faulted rider (escape hatch) | Hidden delivery shell; lease and escape unchanged. |
| Rider/Mount Primary | `27364df661b3c121eabb97a31aa73a83`, `f88a50d6fdbebbd709c3e323d2f52f5e` | Retired; slots cleared | Unchanged. |
| Mounted Combat Reaction toggle | `3c81f5a02be74d96a4e07d1c92b6f385` | Public activatable | Unchanged. |

The mount-state intent is captured at admission: the click creates either a Mount or a Dismount shell command, and the existing shell binding (caster, target, relationship generation) refuses a stale shell after the relationship changes. A queued Mount is never delivered as a Dismount, or the reverse.

## Saved control slots (archive schema 3 `Slots`)

| Kind | Meaning | Restore in F2 |
|---|---|---|
| 1, 2 | Old Mount / Dismount slot | Mount / Dismount control at the same index; the actor counts as placed |
| 3, 4 | Retired primary helpers | Not restored (unchanged) |
| 5 | Old Mounted Charge slot | Stock Charge at the same index, unless Charge is already on the bar |
| 6 | Mount / Dismount slot (new) | Same index |
| 7 | Placement record (new; index 0) | The actor is never placed again, including after deliberate removal |

**Duplicate old aliases (the chosen behaviour).** Mount, Dismount and the public control collapse to one public control, at the first saved index restored. A later old Mount or Dismount row for the same actor leaves its slot empty: it is not kept as a second copy. A live old Mount or Dismount binding found while the public control is already on the bar is emptied the same way. An old Mounted Charge row or binding becomes stock Charge only when the bar holds no stock Charge yet; otherwise its slot is emptied, so an existing canonical Charge never gets a duplicate. Every other slot keeps its content and position.

**Native evidence (F2f, run `20261105-finalize-f2-r05`).** The released preview.211 Quick (`83e92e1a…`, schema 2) carries Dismount at 14 and the retired rider primary at 15. After the native load, the live rows were the public control at 14 and its placement record (one migration); slot 15 was empty. The two Manuals the run wrote held exactly {6@9, 7@0} (the control swapped to slot 9) and {7@0} (the control removed). No native archive member named the public control.

KMC slots are still blanked during native serialization, so the native hotbar never stores a KMC identity. Only KMC's own metadata carries kinds 6 and 7. An older KMC build reads an F2 archive holding kind 6 or 7 as invalid KMC metadata and keeps its original bytes. Downgrading is not a supported path.

## First placement

Placement happens once per actor, after the selected load's slot restoration has settled. It puts the control in the first empty main-bar slot (0–13) when the native "autofill action bar" setting is on. It never overwrites a slot. When the main bar is full it records the attempt and places nothing. A placed, migrated or player-placed control marks the actor as recorded. Removing the control afterwards is remembered across save and load.
