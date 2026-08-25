# Horse risk and kill criteria

Status: IN PROGRESS

| ID | Risk | Required mitigation | Kill / stop criterion |
|---|---|---|---|
| H1 | Native horse is a campaign prototype, not a pet | original KMC blueprint trio using exact stock AddPet/AddClassLevels contract | no sound ownership/progression contract without replacing stock content |
| H2 | Pony identity inferred from name | exact blueprint/resource/reverse-reference audit | pony cannot be resolved without asset extraction or unsafe game mutation |
| H3 | Runtime blueprint GUID collision | deterministic GUIDs plus fail-closed library validation | any GUID resolves to a non-KMC blueprint |
| H4 | Ranger selection mutation conflicts with another mod | append-once transaction and compare-before-restore | exact selection cannot be changed without overwriting unrelated mutation |
| H5 | Save references disappear on uninstall | explicit respec-before-uninstall policy and tested disable/reload behavior | implementation would corrupt or silently orphan a tested save |
| H6 | Horse is unsound unmounted | complete unmounted progression/combat/lifecycle qualification first | genuine creation, control, combat, save/load, death/recovery, or respec failure after bounded repair |
| H7 | Mammoth profile leaks into horse | separate profile ID/data and exact regression tests | horse qualification requires changing accepted Mammoth transforms or gates |
| H8 | Horse animation/rig cannot support rider | Chest/stirrup measurements and fresh human review | material presentation change requires judgment or is visibly unusable |
| H9 | Shared subsystem regresses Mammoth | targeted same-package Mammoth regression | irreducible movement, action ownership, door, lifecycle, or cleanup regression |
| H10 | Target-selected Mount leaves save/UI residue | transient state machine; overlay fallback; no persistent fact/hotbar entry | persistent residue is required without a separate persistence decision |
| H11 | Wrath model tempts broad port | exact responsibility map and later dedicated tranche | horse work requires a broad generic initiative/action rewrite |
| H12 | External environment is not exactly restored | guarded transactions and independent audit before evidence | any exact save/Mods/process/lock/sentinel/transaction restoration failure |
| H13 | Proprietary payload enters Git/package | source/package prohibited-payload gates | any Wrath or extracted Kingmaker asset/code/assembly is included |
| H14 | Scope expands to unauthorized rules/content | explicit source and review gates | Paladin implementation, small riders, ranged/spells/feats/AoO/charge/enemy riders/persistent mounting/auto-remount appears |

Failed evidence is preserved and never relabeled. A build or main-menu load is not runtime qualification. A technically qualified horse still stops for the required human visual/gameplay judgment.

