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

The runtime audit executes after KMC registration. Schema v3 must therefore prove the exact Horse unit/feature/upgrade and four native-control abilities are reference-identical KMC-owned definitions, exclude only those seven references, and scan the resulting stock projection. A matching GUID owned by any other object remains a fatal collision. This prevents the current KMC Mammoth fallback from being rediscovered and mislabeled as a native Horse candidate.

## Dev.2 admission-only failure

The immutable dev.2 reservation `20260829T023000Z-phase3c-dev2-stock-portrait-passA` produced no asset result. The exact dev.2 package loaded, but a stale compiled dev.27 product-version check rejected the request before the audit began. No game result or `horse-native-asset-audit.json` exists, so neither native-asset presence nor absence may be inferred. Independent suite/restoration audit passed before the output log was interpreted.

Dev.3 centralizes the compiled product version and binds it to the repository version source. A fresh clean package/suite/WhatIf and one corrected stock-only observation are required before selecting the portrait source.

## Acceptance

The final Horse unit must expose large, medium, and small Horse art; the party portrait, Ranger selection, Horse feature, and native mounted controls must not show Mammoth art. Asset provenance, source files, dimensions, package paths, and hashes must be recorded. The Mammoth keeps its own portrait unchanged.
