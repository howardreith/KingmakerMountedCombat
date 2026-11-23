# Mounted Shield — final monochrome artwork

## Authoritative design and color

One terracotta circle encloses a large shield outline with a horse head inside. Howie liked the design and explicitly corrected the first version's color: all elements must be uniform and match Mounted Archery. The final runtime icon implements that correction: **ring, shield and horse all use RGB (155, 77, 58), hex #9B4D3A**. No gold or color gradient remains.

This is the most common RGB color among near-opaque pixels (alpha at least 250) in the supplied `MountedArcheryFeat.512.png`. The reference SHA-256 and sample method are recorded in `assets/icon-manifest.json`. The existing source reference is [Mounted Archery's 512px export](https://github.com/howardreith/KingmakerMountedCombat/blob/fb743911200f150af407198c6c28519e522ef58c/assets-source/mounted-archery/MountedArcheryFeat.512.png). The uploaded raster has small color variation; the new production asset intentionally uses one exact ink.

The reference establishes style and color only; none of its pixels was composited into the new symbol. `OWNER_REQUEST.txt` preserves the original request and later color correction. Approval of the design does not invent owner approval of the corrected export before Howie has assessed it.

## Generation and production export

The original design was generated with the built-in OpenAI ImageGen tool and transparent output. A second ImageGen edit used the original as its edit target and the owner's Mounted Archery image as the color/style reference. It retained the shield-and-horse design and removed the gold. Exact prompts and original returned files are preserved.

The selected generated edit still contained small RGB variation. Production export therefore normalizes RGB to the requested single ink while preserving the selected image's alpha. Edge smoothness comes from alpha coverage rather than lighter/darker foreground colors. Fully transparent pixels have RGB zero; their alpha remains zero. No shapes were redrawn in this export step.

| File | Role |
| --- | --- |
| `assets/MountedShieldFeat.png` | **Final runtime file:** 64×64, 8-bit RGBA, one uniform foreground RGB. |
| `assets/MountedShieldFeat.512.png` | Final 512×512 monochrome export. |
| `assets/MountedShieldFeat.master.png` | Final 1254×1254 master with exact ink and preserved alpha. |
| `assets/MountedShieldFeat.generated.png` | Unmodified selected ImageGen color-correction result. |
| `assets/MountedShieldFeat-review.png` | Final actual-size 32/48/64px inspection on dark and parchment fields. |
| `assets/prompts/01-generation.txt` | Original design prompt; its two-color direction is superseded. |
| `assets/prompts/02-color-correction.txt` | Exact ImageGen edit prompt for the owner's correction. |
| `assets/revisions/MountedShieldFeat.initial-two-color.png` | Rejected color revision, retained only as edit-input provenance. Never use in the game. |
| `assets/icon-manifest.json` | Final hashes, dimensions, alpha and uniform-color inspection. |

These are versions of one feat image, not additional toggles or game assets. Embed only the final 64px file.

## Export recipe

ImageMagick 7.1.1-43 Q16 was used for exact ink normalization, Lanczos reductions and PNG32 encoding:

```sh
magick assets/MountedShieldFeat.generated.png -channel RGB -fill '#9B4D3A' -colorize 100 +channel -background '#000000' -alpha background PNG32:assets/MountedShieldFeat.master.png
magick assets/MountedShieldFeat.master.png -filter Lanczos -resize 512x512 -channel RGB -fill '#9B4D3A' -colorize 100 +channel -background '#000000' -alpha background PNG32:assets/MountedShieldFeat.512.png
magick assets/MountedShieldFeat.master.png -filter Lanczos -resize 64x64 -channel RGB -fill '#9B4D3A' -colorize 100 +channel -background '#000000' -alpha background PNG32:assets/MountedShieldFeat.png
```

This is provenance, not a build step. **Copy the supplied PNG byte-for-byte.** Encoder metadata/tool differences may change hashes. ImageMagick is not a new mod dependency. The normal build must not generate, recolor or resize artwork.

The normalized full-size master preserves the selected generated alpha byte-for-byte. The 512/64px files resample alpha normally and reassert the exact ink. Every nonzero-alpha pixel in all three final exports was checked to be RGB (155, 77, 58); their corners are transparent. The review sheet composites those exports for inspection and is not a native Kingmaker screenshot.

## Integration

| Item | Value |
| --- | --- |
| Runtime destination | `src/KingmakerMountedCombat/Assets/MountedShieldFeat.png` |
| Embedded resource | `KingmakerMountedCombat.Assets.MountedShieldFeat.png` |
| Sprite name | `KMC_MountedShield_Feat_Icon` |
| Proposed blueprint | `MountedShieldFeature`; choose/check a fresh stable GUID before registration |
| Localization | `KMC.MountedShield.Name` / `KMC.MountedShield.Description` |
| Loader / binding | Existing `CreateEmbeddedSprite` / `SetFeaturePresentation` |
| Source convention | `assets-source/mounted-shield/`, or preserved complete packet with clear mapping |
| Visible consumers | Feat selection/details, acquired feat list and tooltip |

Explicit project resource:

```xml
<EmbeddedResource Include="Assets\MountedShieldFeat.png">
  <LogicalName>KingmakerMountedCombat.Assets.MountedShieldFeat.png</LogicalName>
</EmbeddedResource>
```

The existing loader accepts PNG through `ImageConversion.LoadImage` and creates a full-rect centered sprite with Clamp/Bilinear and 100 pixels per unit. Reuse the loader and its ownership/disposal. No DDS, Unity editor import, asset bundle, `.meta` file or second runtime icon is needed.

Compare SHA-256 of the supplied runtime file, production copy and extracted named DLL resource. Confirm the actual loaded blueprint sprite and native UI; a resource declaration alone is insufficient.

## Preparation status

| Check | Result |
| --- | --- |
| Owner color correction | Incorporated |
| Exact uniform foreground RGB in master/512/64 exports | Verified: #9B4D3A |
| Normalized master alpha | Identical to selected generated alpha |
| Runtime dimensions/mode/transparency | Verified: 64×64, RGBA8, transparent corners |
| Dark/parchment desktop readability at 32/48/64px | Inspected; horse, shield and circle remain distinguishable |
| Prompt and generation lineage, hashes | Included |
| Embedded resource / loaded blueprint / actual game UI | NOT_RUN — implementation pending |
| Owner aesthetic assessment of corrected export | Not presumed |

Continue implementation with this candidate. Native behavior/UI and the owner's aesthetic assessment remain separate from completed file-format and color checks.
