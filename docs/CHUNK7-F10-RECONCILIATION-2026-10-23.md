# Chunk 7 F10 reconciliation: progression behaviors against their evidence

Status: **development closure for F10's selected representative paths.** All entries are
development evidence on their own preview candidates. Executing them on the delivered package
is a separate 7E decision under the reuse rules. The standing mission remains IN PROGRESS.

## Requirement

The original scope (`handoffs/.../KMC_Chunk7_Original_Scope_and_Matrices.md:28-30`, matrix
:115-116) asks Chunk 7 to:

- retain the existing Horse acquisition architecture;
- verify new character, existing campaign, level progression, dismissal/replacement and
  retraining;
- verify documented cleanup and a mod-absent load where claimed.

Paladin Divine Steed and other class grants stay outside this release. Fresh install,
upgrade, rollback and removal belong to F12/7E. F10 is a finite set of representative
supported paths, not every class, race or campaign.

## Behaviors

| Behavior | Selected representative path | Evidence | Verdict |
| --- | --- | --- | --- |
| New character / new campaign | Fresh native campaign; a newly created character from native paid CharGen chooses the Horse inside CharGen; then a Manual save, mounted play and a separate cold load. | One package (DLL `0a3db888…`), b154s1/s2/s3/s5 → b155s1/s2: assisted fresh campaign, paid level-5 Halfling Ranger/Horse (12,500, `ordinaryAcquisitionProven=true`, zero XP/money writes in the measured run), acquired Manual 302, two-pair mounted Manual 303 and an independent cold load. Repeated in b150 and b158. Setup is disclosed: the main's XP shortfall and the hire-money shortfall were granted in the assisted producer, before measurement. | Covered |
| Existing campaign | Owner-seed existing main reaches Ranger 4 and takes the native Horse choice. | b131–133 (field Manual 304, independent cold, acquired Manual 305); repeated b159–161; cumulative one-owner and two-owner Manuals 306–308 with a separate cold load in b163–166. | Covered |
| Leveling | A mounted rider levels natively while its pair is retained. | b108: Small rider Ranger 4→5, Horse rank 2, neighbor unchanged, Manual 306 plus separate cold load. b116/117/120: Medium respec to Ranger 10, companion rank 7. | Covered |
| Dismissal | Native `DismissCompanion` of a paired companion. | b89: target pair retired with no residue, unrelated pair kept, exact restoration. | Covered |
| Replacement | Paid Medium Horse replaced by a Mammoth through native respec. | b116→b117, b120: Manual 306, independent cold load, rank 7 Large, old Horse absent, control pair kept; native Mount, move and Dismount on the new Mammoth. | Covered |
| Retraining | Mounted Small Horse retrained through the native UI. | b99/b100: native money credits, `AddPet` and feature reapply, new reciprocal Horse, former pair removed, separate cold load. | Covered |
| Documented cleanup and removal | Clean-save preparation, then a genuine DLL-absent cold load. | b65 positive cleanup; b66–67 DLL-absent cold load (7B row). | Covered for development; the player guide states the effects (below). Execution is F12/7E. |

## Not selected, with the replacement route

**Player's own main made in main-menu CharGen.** Not selected:
- A level-1 main can never take the Horse, so main-menu CharGen never reaches KMC's Ranger seam.
- The seam is exercised by paid CharGen above, and on the main at Ranger 4 in the existing-campaign path.
- Selecting it would need a new headless-CharGen seam: the batch145 UI fault, and relaxing the
  `NewGameUnit == null` guards.

**Stale level-up Auto families.** Retired and not selected; their prelaunch refusals remain:
- `chunk7-ordinary-acquisition`, replaced by the manual-derived and cumulative routes;
- `chunk7-native-progression` `horse-native-*`, replaced by `small-horse-native-level`,
  `small-horse-native-retrain-horse` and `medium-horse-native-retrain-mammoth`.

They wait for a level-up Auto that the installed game never requests.

**Legacy cumulative Manual 302→303.** Retired and not selected; its refusal remains:
- A one-owner 302 source cannot be produced from the selected paths (b158).
- The owner-seed one-owner route 305→308 (b163–166) replaces it.

No family was repaired or relabeled to obtain a PASS. Earlier FAILs keep their records.

## Remaining for 7E

Decide exact-package reuse for these rows. If they must run on the delivered package, the
smallest representative set is:

1. the assisted producer;
2. its cold consumer;
3. paid CharGen acquisition;
4. one mounted leveling case.

The mounted Manual/cold chain is covered by the 7E persistence rows.
