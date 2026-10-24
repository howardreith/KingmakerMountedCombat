# Chunk 7 owner HUMAN PLAY checklist

Use a **copy** of a save, never your only one. Write down:
- Package: [TBD after 7E freeze]
- DLL SHA-256: ______
- Save copy: ______
- Date: ______

Tick each box when it behaves as described. If it does not, note the steps, the pair (rider and mount)
and which character, then see "Reporting" at the end.

## 1. Install

- [ ] Back up the Saved Games folder and keep the preview.211 package.
- [ ] Close the game. Put `Info.json` and `KingmakerMountedCombat.dll` in `Mods\KingmakerMountedCombat\`.
- [ ] Start the game. In UMM (Ctrl+F10), Kingmaker Mounted Combat is on and shows the expected version.
- [ ] The UMM log has no KMC `[Exception]` or error line at start.

## 2. Put the controls on the action bar (by hand)

KMC does **not** fill action-bar slots.
- [ ] Select a rider, open the ability drawer, find **Mount Companion**, and drag it to an empty slot.
- [ ] Do the same for **Dismount**.
- [ ] Optional: place **Rider Primary** and **Mount Primary**.
- [ ] The slots are still there after a save and a reload.

## 3. Mount and dismount

- [ ] Use Mount Companion on the rider's own Horse or Mammoth. The rider sits in the saddle. The legs and
  saddle look right, and the weapons look right.
- [ ] Use Dismount. The rider stands beside the mount, and both can be controlled separately.
- [ ] Try Mount Companion on someone else's companion. It refuses with a clear reason and spends nothing.

## 4. Moving together

- [ ] Click the ground with the mounted rider selected. The pair moves together, smoothly, and the camera
  follows.
- [ ] Select a portrait or another character, then come back. The pair is still mounted and moves
  correctly.
- [ ] With two pairs mounted, move each one. The other pair does not move.

## 5. Combat: real time

- [ ] Start a fight with a pair mounted. Rider and mount both attack normally (try Rider Primary and Mount
  Primary).
- [ ] Pause, give a ground order, then unpause. The order runs.
- [ ] Target feedback and the pointer behave as they do for unmounted characters.

## 6. Combat: turn-based

- [ ] Rider and mount each get their turn in either order. Neither gets extra actions.
- [ ] Move on the mount's turn. The pair moves together, and the cost comes off the mount's movement.
- [ ] Mount or Dismount on the rider's own turn works, and End Turn works.
- [ ] Turn-based Charge is unavailable and spends nothing.

## 7. Door

- [ ] With a mounted rider selected, click an ordinary door. The pair walks to it and the door opens once.

## 8. Area transition

- [ ] Travel to another area with pairs mounted. They arrive still mounted.

## 9. Capital

- [ ] Arrive at the capital with a companion's pair mounted. That pair is dismounted. The KMC UMM page
  says "Dismounted on arrival because the companion is not directly controllable in this area: … Mount
  again after leaving." Your main character's own pair stays mounted.
- [ ] You can still save in the capital.
- [ ] Leave the capital and use Mount Companion on that pair again. It works.

## 10. Save and load

- [ ] Save while mounted (Manual and Quick). Load each one. Every pair is mounted again, and no one gains
  actions.
- [ ] Quit to the desktop and load the same save after restarting. It gives the same result.
- [ ] Save in turn-based combat partway through a turn and load it. The turn continues correctly.

## 11. Upgrade from a preview.211 save

- [ ] Load a save made under preview.211 (a copy). It loads. Any pair saved mounted is mounted again.
  **Automation has not tested this:** no save written by preview.211 was ever upgraded in the test lab
  (only migrated test derivatives and the install/upgrade/rollback rehearsal), so watch it closely.
- [ ] The UMM log does not show the "no longer has seven stock options" exception.

## 12. Removal

- [ ] On a copy of a campaign **without** a KMC Horse, press "Prepare to disable / remove KMC". It reports
  "Prepared: cleanup save …".
- [ ] On a campaign **with** a KMC Horse, the same button refuses and names the Horse.
- [ ] Remove the mod folder, start the game and load the cleanup save. The game plays normally.

## Overall feel

- [ ] Pointer feel, portraits and selection, seat and leg contact, weapons, camera: acceptable for play.

## Reporting

For each failed box:
- copy the UMM log (`<Kingmaker install>\Kingmaker_Data\Managed\UnityModManager\Log.txt`) before
  restarting;
- keep the save;
- note the steps, the pair and the character, and what you expected and saw;
- take a screenshot of the KMC UMM page.
