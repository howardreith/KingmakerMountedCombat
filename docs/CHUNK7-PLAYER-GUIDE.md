# Chunk 7 private candidate guide

Status: IN PROGRESS. Use the exact candidate and qualified behavior table in the
final engineering handoff; native breadth and owner play acceptance are pending.
This guide does not authorize installation into a human campaign.

The current private implementation admits up to six mounted pairs. Ordinary two-pair
combat is the qualified play breadth; six pairs are qualified for exploration lifecycle,
Manual save and cold restore, not for six-pair combat. Its declared
profiles are eligible Medium riders on the KMC Horse or supported native Mammoth,
plus one exact Small profile: the stock default **male Halfling on the KMC Horse**
(`small-default-halfling-horse-v1`). The Small rider must retain the measured
male Halfling preset, native skeleton, handedness, body scale, default Doll and
visual equipment, and the exact twelve observed equipment slots. Rider and
Horse must be unpolymorphed; the Horse must have its measured native Large size.
Other Small races, Halfling presets or sexes, altered gear/rig/body, Small on
Mammoth and unsupported forms are outside this declaration and can refuse Mount
or saved restoration. Changing render scale does not make another body eligible.
The exact Small [native Mount/movement/primary result](CHUNK7-BATCH72-SMALL-ORDINARY-NATIVE-2026-10-20.md)
and later [mounted Manual/cold result](CHUNK7-BATCH100-SMALL-RETRAIN-COLD-2026-10-20.md)
are development evidence on their recorded candidates; representative
spell/item presentation, Medium breadth, conditional six-pair stress and final
exact-package acceptance remain open.

Select the rider and open their ability drawer. Use **Mount Companion** on that
rider's own supported animal companion, and **Dismount** on the rider to separate
them. KMC does not fill action-bar slots automatically. You can drag a control to
an empty slot. The Horse is a Ranger companion choice;
KMC owns its addition to the native choice list, while Kingmaker owns acquisition
and progression. An arbitrary Small character is not implicitly supported.

The supported preset uses the mount to carry and move the pair. Rider and mount
retain their own actions, health, defenses and effects. Use ordinary movement,
attacks and spells. Rider Primary and Mount Primary choose the corresponding
actor's attack. Changing selection does not retarget a command already admitted.
Each pair retains its own cleanup obligations after dismount.

Paired turns are the normal configuration. Historical experimental turn modes and
the old attack overlay are not ordinary settings. Charge is off by default; change
it outside combat with every pair dismounted. Supported real-time Charge requires
its documented native eligibility. Turn-based Charge remains unavailable and
refuses without spending actions. The defensive Mounted Combat feat is not
implemented.

Read the native ability's refusal text when an action is unavailable. Open
Kingmaker Mounted Combat's page in Unity Mod Manager (UMM) for selected-pair
feedback and save/removal status. Unsupported ownership, size, form or rig,
occupied actors and unfinished cleanup can refuse Mount Companion. Resolve the
stated cause before another attempt; a selection change or repeated click cannot
grant actions or transfer old command ownership.

Saving waits for unsettled work across every pair. Schema-3 native archives retain
all admitted pair and actor obligations; valid historical schema-1/2 data uses the
explicit migration adapter. Unknown or invalid data remains non-restoring and is
preserved when safe. Keep pre-migration saves and the earlier package for rollback;
old binaries are not certified to resume new-format gameplay.

Known issue in the released preview.211: on an installation without Call of the Wild
it logs "[Exception] Horse companion blueprint registration: InvalidOperationException -
The exact Ranger companion AllFeatures surface no longer has seven stock options." in the
UMM log at start, because it recognized only the Call of the Wild Ranger companion list.
Upgrading to the Chunk 7 build fixes it. Rolling back to preview.211 brings the message back.

Progression and the mounted pair:
- Leveling keeps a mounted pair.
- Dismissing the companion, retraining the Horse or replacing it (for example a Mammoth at
  companion rank 7) retires that pair; mount the new companion again.
- In an area where the game does not let you control a companion directly (the capital, where only
  your main character and its own companion are controllable), KMC dismounts that pair on arrival and
  says so on its UMM page; mount again after leaving.
- KMC's Mount and Dismount abilities are not placed on the action bar automatically. Find
  them in the character's ability list and place them where you want them.

Before removing KMC, use its clean-save preparation flow. A campaign containing a
KMC Horse still requires the mod. Permanent installation, release rehearsal and
human play acceptance are separate from this engineering campaign.

Owner review should name the exact package, DLL and save copy, then assess pointer
feel, target feedback, action bar/portrait selection, leg and saddle contact,
weapons, camera and overall play behavior. Automated observations do not grant
human visual acceptance. Record reproduction steps and the affected pair/actor
when a command, view or resource behaves unexpectedly.
