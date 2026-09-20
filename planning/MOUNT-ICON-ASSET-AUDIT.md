# Mount icon asset audit

Status: `PASS (original fallback selected) - RUNTIME/UI HUMAN REVIEW REQUIRED`

Date: 2026-08-30

## Native Kingmaker audit

The exact installed Kingmaker data remains a read-only evidence source. Before creating replacement art, two bounded discoverability scans were run against `Kingmaker_Data/StreamingAssets/Bundles`:

1. a filename/path scan for standalone `saddle` or `tack` terms;
2. a manifest-text scan for the same terms across every `*.manifest` file.

Both scans returned zero matches. The earlier initialized-library Horse asset audit inspected 104,667 blueprint objects and likewise recorded no suitable native Horse/Pony portrait or Horse-identity icon set. That earlier artifact did not claim an exhaustive saddle search, so Phase 3D will add a runtime blueprint/icon-name projection before final qualification. The zero manifest/path result is nevertheless dispositive for the immediate asset choice: no native saddle/tack icon has a discoverable resource path, blueprint owner, or stable GUID that KMC can reference safely. An opaque or unrelated icon is not accepted by resemblance alone, and no proprietary texture is extracted or redistributed.

## Disposition

KMC uses original saddle artwork. The Mount and Dismount abilities use the saddle icon; the Horse companion feature and Rider/Horse primary controls retain the accepted Horse identity icon. This keeps action semantics distinct from companion identity.

The saddle artwork was generated from text only with OpenAI ImageGen for this project. No Kingmaker, Wrath, YouTube, screenshot, logo, or third-party image was supplied as an image reference. The source prompt was:

> Create a brand-new original square bitmap asset for the Kingmaker Mounted Combat mod: a medieval riding saddle and tack ability icon, viewed in clear three-quarter profile. Center one compact dark oxblood-brown leather saddle with a high pommel and cantle, one visible brass stirrup hanging below, short leather straps, and a small folded saddle blanket in muted deep red. Painterly late-medieval fantasy CRPG inventory-icon style, warm amber rim light, rich brown leather, aged brass, strong readable silhouette, crisp focal edges, restrained texture, dramatic but uncluttered dark umber vignette background. Designed to remain instantly recognizable at 48–64 pixels. No horse, no horse head, no rider, no human, no text, no letters, no logo, no watermark, no decorative UI frame, no copied game icon, no photorealism. Square 1:1 composition with generous breathing room around the saddle.

## Project-owned files

| File | Role | Dimensions | SHA-256 |
|---|---|---:|---|
| `Assets/MountSaddleIconMaster.png` | original high-resolution source | 1254x1254 | `0f8efb42227c375e61ebdd0b0254a71cca5f4372a862b4ae8d558497600b8d23` |
| `Assets/MountSaddleIcon.png` | embedded runtime ability sprite | 128x128 | `75b02adb8eeef7bc0676a8c3f8b9d22603f524a416bffa94c33fc306cfdaa5a1` |

The 128x128 derivative is a high-quality bicubic reduction of the committed source. Both are original KMC-distributed assets; they contain no extracted proprietary pixels. Final ability-bar legibility remains a focused human gate.
