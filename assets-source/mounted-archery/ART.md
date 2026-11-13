# Mounted Archery original icon

## Intended identity

One original muted-red ring encloses a mounted archer aiming a drawn bow. The horseback silhouette communicates mounted combat; the bow/arrow distinguishes this passive accuracy feat from the existing rider-and-shield Mounted Combat icon and firearm-loading Rapid Reload icon.

The motif is a bow for readability, while the implemented feat applies to qualifying ranged weapon attacks generally. The art does not imply an active attack button, magical power, Charge action or a bow-only prerequisite.

## Inspected style references

The following repository-owned references were opened and visually inspected during preparation. Reference PNGs are not redistributed in this packet and no pixels from them were pasted into the new icon.

| Reference | Authority and source |
|---|---|
| Approved Rapid Reload v2 | Flat muted terra-cotta circular combat emblem, transparent negative space. [512px source](https://github.com/howardreith/KingmakerGunslingerAndOtherTabletopRules/blob/a3912f569ac53558529dfe3f97d137125c0715a7/assets-source/original-icons/icon-overhaul-v2/pilot/sources/rapid-reload.png). Dominant core reference color #A6533F; 64px export hash 5c3280145815f7b161602da161264ac1f43fb3ac3c4bf47ff907b0919a2aa9d3. |
| Current Mounted Combat feat | Rider, horse and shield in a red ring. [512px source](https://github.com/howardreith/KingmakerMountedCombat/blob/6b6a3e6a7afc24239b2c77be99ba118bdee864ba/assets-source/finalize-f1/MountedCombatFeat.512.png). Used for sibling distinction and target-mod coherence; this does not invent an owner approval for that reference. |
| Canonical style guide | [ICON-ART-GUIDE.md](https://github.com/howardreith/KingmakerGunslingerAndOtherTabletopRules/blob/a3912f569ac53558529dfe3f97d137125c0715a7/docs/ICON-ART-GUIDE.md) and [reference index](https://github.com/howardreith/KingmakerGunslingerAndOtherTabletopRules/blob/a3912f569ac53558529dfe3f97d137125c0715a7/docs/art/ICON-REFERENCE-INDEX.md). The mundane combat-emblem family uses transparent 64px RGBA exports, broad silhouettes, muted ink and no baked square frame. |

The older rejected thick partial Rapid Reload arc was not used as an approved anchor. Painted magical/racial icon conventions were not substituted for this mundane combat feat.

## Generation and preserved lineage

The production tool was the built-in OpenAI ImageGen tool, with a transparent-background request.

1. A new composition was generated from text. The prompt described the inspected emblem vocabulary; the first call supplied no external reference image file.
2. A second image-generation edit used only that original generated image as its edit target. It requested cleanup, flatter color and more breathing room while preserving the mounted-archer pose.
3. Both original returned files are preserved byte-for-byte. The selected second result is the master; the first result remains in revisions. Exact prompts are included as separate UTF-8 text files.

| File | Role |
|---|---|
| assets/revisions/MountedArcheryFeat.initial.png | Original first generated image, 1254×1254 RGBA; input to cleanup |
| assets/prompts/01-generation.txt | Exact initial prompt |
| assets/prompts/02-cleanup.txt | Exact cleanup/edit prompt |
| assets/MountedArcheryFeat.generated.png | Selected original generated master, 1254×1254 RGBA |
| assets/MountedArcheryFeat.512.png | 512×512 RGBA derivative for comparison/review |
| assets/MountedArcheryFeat.png | 64×64 RGBA runtime candidate |

All illustrative pixels in the supplied icon originate in these image-generation calls. No extracted Kingmaker/Wrath asset, game screenshot, game font, third-party character or downloaded stock image was incorporated.

The prompt's target was a uniform muted terra-cotta emblem. The returned raster has modest antialiasing/color variation; the delivered pixels, rather than the idealized prompt, are the authoritative candidate. Do not claim a mathematically single-color source. The actual 64px export was inspected on both light and dark fields.

## Export and technical inspection

The exports use ImageMagick 7.1.1-43 Q16, Linux x86_64. Only resizing and PNG encoding were used for the production derivatives; no repainting, sharpening, background replacement or recoloring was applied after the model edit.

From the packet root:

```sh
magick assets/MountedArcheryFeat.generated.png -filter Lanczos -resize 512x512 PNG32:assets/MountedArcheryFeat.512.png
magick assets/MountedArcheryFeat.generated.png -filter Lanczos -resize 64x64 PNG32:assets/MountedArcheryFeat.png
```

This is a recorded reproduction recipe, not a new production dependency. Commit and use the provided runtime PNG; normal builds should neither invoke ImageGen nor require ImageMagick. Keep the recorded bytes unless deliberately revising the asset. A different toolchain or metadata policy may produce a different file hash; do not silently replace the manifest identity.

The runtime PNG is square, 8-bit RGBA, with genuine transparent corners and internal negative space. The authoring master remains separate from runtime embedding. Exact dimensions, hashes and alpha inspection are in `assets/icon-manifest.json`.

`assets/MountedArcheryFeat-review.png` shows:

- Actual 32, 48 and 64px reductions on parchment and dark backgrounds.
- A 4× nearest-neighbor enlargement of the 64px export for edge inspection.
- A grayscale silhouette comparison.

The reviewer can distinguish the horse, mounted rider, drawn bow and arrow at the inspected sizes. The image contains no text, square UI frame, resource counter or selected/disabled overlay. The 32px reduction is a stress test; small arrow/string details naturally simplify there.

This review sheet is a composed desktop export inspection, not a native Kingmaker screenshot.

## Integration mapping

| Consumer | Proposed identity |
|---|---|
| Blueprint | MountedArcheryFeature |
| Stable GUID | b81926bca1d24a3997cdc8471a84014b (proposed; recheck current collisions before first use) |
| Player-facing name | Mounted Archery |
| Source/provenance directory | assets-source/mounted-archery/ |
| Runtime source file | src/KingmakerMountedCombat/Assets/MountedArcheryFeat.png |
| Embedded logical name | KingmakerMountedCombat.Assets.MountedArcheryFeat.png |
| Sprite name | KMC_MountedArchery_Feat_Icon |
| Loader | Existing CreateEmbeddedSprite |
| Binding | Existing SetFeaturePresentation on the owned feature |
| Visible surfaces | Ordinary/combat feat selection, selected feat summary, character-sheet feat list and tooltip |

There is no new ability/toggle/buff consumer by default. If implementation discovers a necessary hidden internal fact, that does not authorize a second visible icon or action. Never replace the existing Mounted Combat or reaction art.

## Review status

| Gate | Status |
|---|---|
| Original generation lineage and complete prompts | Included |
| PNG dimensions, RGBA/alpha and SHA-256 identities | Checked |
| 32/48/64px desktop visual inspection | Checked; supplied candidate is readable |
| Loaded production blueprint and embedded sprite | NOT RUN — implementation pending |
| Actual native UI placement, selected/disabled state and tooltip | NOT RUN |
| Owner aesthetic acceptance | Pending |

Technical inspection is sufficient to supply an implementation candidate. It is not a substitute for native UI review or Howie's aesthetic judgment, and does not require stopping the rest of the implementation.
