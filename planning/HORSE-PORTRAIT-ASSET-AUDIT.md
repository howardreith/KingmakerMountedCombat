# Horse portrait asset audit

Status: `PASS (technical) - HUMAN ART REVIEW REQUIRED`

Date: 2026-08-28

## Established defect

The exact `CR1_HorseRiding` unit (`9e9e75c484e68734487e609714565202`) has no usable `BlueprintPortrait`. Current production code therefore falls through to the stock Mammoth unit portrait and the Horse feature falls through to a non-Horse companion icon. Human dev.27 evidence confirms Mammoth art on the Horse large and party surfaces.

## Required bounded native search

Search the initialized Kingmaker library for Horse/Pony-named `BlueprintPortrait`, unit portrait references, portrait data, companion/selection icons, and Horse/Pony feature or unit icons. Record exact GUID/name/type/owner surfaces and whether the reference can be used without extracting or redistributing a proprietary file.

Candidate order:

1. suitable native Kingmaker Horse portrait referenced by blueprint;
2. suitable native Kingmaker Horse/Pony icon for small feature/toolbar use;
3. original KMC-owned redistributable art if no suitable native portrait set exists.

The supplied Kingmaker and YouTube/Wrath screenshots are evidence only. They may not be cropped, traced, transformed, committed, or shipped. No Wrath asset may be used.

The runtime audit executes after KMC registration. Schema v3 must therefore prove the exact Horse unit/feature/upgrade and four native-control abilities are reference-identical KMC-owned definitions, exclude only those seven references, and scan the resulting stock projection. A matching GUID owned by any other object remains a fatal collision. This prevents the current KMC Mammoth fallback from being rediscovered and mislabeled as a native Horse candidate.

## Dev.2 admission-only failure

The immutable dev.2 reservation `20260829T023000Z-phase3c-dev2-stock-portrait-passA` produced no asset result. The exact dev.2 package loaded, but a stale compiled dev.27 product-version check rejected the request before the audit began. No game result or `horse-native-asset-audit.json` exists, so neither native-asset presence nor absence may be inferred. Independent suite/restoration audit passed before the output log was interpreted.

Dev.3 centralizes the compiled product version and binds it to the repository version source. A fresh clean package/suite/WhatIf and one corrected stock-only observation are required before selecting the portrait source.

## Acceptance

The final Horse unit must expose large, medium, and small Horse art; the party portrait, Ranger selection, Horse feature, and native mounted controls must not show Mammoth art. Asset provenance, source files, dimensions, package paths, and hashes must be recorded. The Mammoth keeps its own portrait unchanged.

## Completed audit and disposition - 2026-08-30

Corrected initialized-library run `20260829T043500Z-phase3c-dev3-stock-portrait-passA` inspected 104,667 objects after excluding only the exact KMC definitions. It found no suitable stock Kingmaker Horse/Pony `BlueprintPortrait` or icon set. `CR1_HorseRiding` has no usable portrait, confirming the exact Mammoth-fallback cause.

KMC therefore uses an original project-owned, redistributable portrait set. It was not copied, cropped, traced, or derived from Kingmaker, Wrath, YouTube, or the supplied human screenshots. Runtime portrait GUID is `6874a165bf8bda3531ee4e2abc10c899`.

| File | Dimensions | SHA-256 |
|---|---:|---|
| `HorsePortraitOriginalMaster.png` | 1024x1536 | `0b623b98440de8131c138d08f45d87e02b51f034cba313aeb36f81cbe078520f` |
| `HorsePortraitLarge.png` | 692x1024 | `8b7b4386de1b5adbd9f7f9f1c3728de32325b03c5f2dfc2fe6c7babf95a712e7` |
| `HorsePortraitMedium.png` | 330x432 | `890327ecc9e9b092b4343140fd9eb839800bb1044d8e4aeafeaaa1476a44ba61` |
| `HorsePortraitSmall.png` | 185x242 | `d0c5c876a827a0b8842d35833492e2d40b632ad5fbec7e70c8c2d72f7209fa16` |
| `HorseIcon.png` | 128x128 | `b088d4b29de3cdfc536c254cf47abbe52af4000aa8f25ac742c3d0612a253f02` |

Registration and UX evidence prove exact KMC Horse portrait/icon identities and no Mammoth reference on Horse-specific surfaces. Human review remains responsible for composition, readability, and aesthetic acceptance.

## Phase 3D small/party close-up - 2026-08-30

Human Phase 3C review accepted the original Horse artwork but requested a tighter small/party crop. The large and medium portraits and `HorsePortraitOriginalMaster.png` remain byte-identical to the accepted Phase 3C set. Only the small surface changes.

OpenAI ImageGen edited only the project-owned `HorsePortraitOriginalMaster.png`; no Kingmaker, Wrath, YouTube, or human-review screenshot was used as an image reference. The edit prompt required the same chestnut Horse identity, blaze, bridle, painterly treatment, light, and forest while reframing ears, face, eyes, muzzle, and upper neck for 48–64 pixel readability. It prohibited new tack, riders, text, logos, borders, and new objects. The generated close-up master was center-cropped to the exact native small-portrait aspect and reduced with high-quality bicubic sampling.

| File | Dimensions | SHA-256 | Phase 3D disposition |
|---|---:|---|---|
| `HorsePortraitOriginalMaster.png` | 1024x1536 | `0b623b98440de8131c138d08f45d87e02b51f034cba313aeb36f81cbe078520f` | unchanged accepted master |
| `HorsePortraitLarge.png` | 692x1024 | `8b7b4386de1b5adbd9f7f9f1c3728de32325b03c5f2dfc2fe6c7babf95a712e7` | unchanged |
| `HorsePortraitMedium.png` | 330x432 | `890327ecc9e9b092b4343140fd9eb839800bb1044d8e4aeafeaaa1476a44ba61` | unchanged |
| `HorsePortraitSmallCloseupMaster.png` | 1097x1434 | `4f3a190fdb44a1c59e8c2034ecdc2401e6b550526996c9e2b3d38cc2ad962912` | new original close-up source |
| `HorsePortraitSmall.png` | 185x242 | `5154944913525ff6596aab2bc6cbd623a08557c595b63a1b63eb880f203a99c1` | new embedded small/party crop |
| `HorseIcon.png` | 128x128 | `b088d4b29de3cdfc536c254cf47abbe52af4000aa8f25ac742c3d0612a253f02` | unchanged Horse identity icon |

The close-up is an original KMC-distributed derivative of KMC's original source. Runtime party/tracker rendering and final aesthetic judgment remain focused human gates.
