# Horse portrait asset audit

Status: `IN PROGRESS`

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

## Acceptance

The final Horse unit must expose large, medium, and small Horse art; the party portrait, Ranger selection, Horse feature, and native mounted controls must not show Mammoth art. Asset provenance, source files, dimensions, package paths, and hashes must be recorded. The Mammoth keeps its own portrait unchanged.

