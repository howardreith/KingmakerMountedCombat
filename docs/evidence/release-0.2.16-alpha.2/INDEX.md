# Release 0.2.16-alpha.2 evidence

Raw SHA-256 is of the original output bytes as written on Windows (CRLF line endings where the producer wrote them).
Committed SHA-256 is of the Git blob of each file in this folder (LF; local path prefixes replaced by `<lab>`, `<fleet>`,
`<user-profile>` and `<dev>`; a byte-order mark dropped; nothing else edited). A Windows checkout with `core.autocrlf`
may show CRLF in the working tree, so hash the blob: `git cat-file blob HEAD:docs/evidence/release-0.2.16-alpha.2/<file> | sha256sum`.

| File | Producer | Raw SHA-256 (Windows output) | Committed SHA-256 (Git blob) |
|---|---|---|---|
| `api-audit-mod.json` | `<fleet>/tools/audit-loader-api.sh` (LoaderApiAudit; sources in `docs/evidence/umm-0311/loader-api-audit-*.txt`) on the DLL inside `KingmakerMountedCombat-0.2.16-alpha.2-release-diagnostic.zip` (DLL `2105f305…`), built by `scripts/Package.ps1 -IncludeRemovalObserver -ArtifactQualifier release` at `7a9861a5` | `032298b1104f98cee1ff5285d8cf2aaebe48891aef7eab31ccc8c09760195f4c` | `82cdd02910fae3e4c677f2be73b2def591c3d2d465474b6502277efda2cd159d` |
| `api-audit-observer.json` | the same tool on the DLL inside `KmcRemovalObserver-0.1.0-observer.8-release-diagnostic.zip` (DLL `f9697f91…`) from the same packaging run | `972926f092c82d8ce107b05e942c5c68ecde185d61c6160e2d0dca770fe654e7` | `25b22eaeb318e9878dce452861de5553bcf2a26fe9cba4ea942adbac7b014abb` |
| `KingmakerMountedCombat-0.2.16-alpha.2-release-diagnostic.zip.manifest.json` | `scripts/Package.ps1 -IncludeRemovalObserver -ArtifactQualifier release` at `7a9861a5` (clean worktree), written beside the mod ZIP | `c1cc8a83d970d11529c10309c1646f3c5e629058780d38880cd20fbad69a5e84` | `e84a9ac0c21b7eaf2edd60b0ad37cae1fa28cc204f5c3af7cac3e41e7890d440` |
| `KmcRemovalObserver-0.1.0-observer.8-release-diagnostic.zip.manifest.json` | the same packaging run, written beside the observer ZIP | `539dfa99ae93db2dfaa4d9e26b854d4bf47fa63d780c53e2f6f77502aeec0940` | `da0a77aa00e0bb7c6a26a3ddbaf7f0c092317b52bb388376a6a270bc9ba096bd` |
| `validate-mod-frozen.txt` | `scripts/Validate-Package.ps1` (this checkout; product inputs as at `7a9861a5`) on the frozen mod ZIP `64a4317c…`, 2026-11-26 16:45 UTC | `3c01ebb5913977d86b0c8c395e45219f58f45400a6d3cb793fe2638bc1ad653b` | `cec8cf5990bcdc714f413959f507db7180ca2b681134ae23d0e861e61e559601` |
| `validate-observer-frozen.txt` | `scripts/Validate-ObserverPackage.ps1` on the frozen observer ZIP `2c0fe382…`, same run | `c2f3081d00c329af5ce368619576f42a0f7ce63176d08861947d3e1d86a19138` | `150abb82730c486f203daf8b994bbb6bb9ae84cdcc7cd2a6c32b38760701cbdb` |
| `continuation-records.md` | excerpts written for this record from the owner's mission records and the controller command results it lists (round 13: complete denied command and the #69 closure record referenced) | `20d3f9f76632aa23fbbe1992ffa5bd5a19a4631da455d5fd4ea9826b5b41fbc5` | `20d3f9f76632aa23fbbe1992ffa5bd5a19a4631da455d5fd4ea9826b5b41fbc5` |
| `permission-denial-20261126T132727Z.json` | the denied tool call and its classifier response, from the session transcript (lines 31704-31705); raw digest of the unsanitized extract | `8684a5b459b1adb11f4f518dada5559dc3848a24c7119cc1964a202278868281` | `c7bb19ec3974514d031308480a1ed10880c074d504d46f46becf846be24fd5c7` |
| `pr69-closure.json` | `gh pr view 69 --json ...` (read-only) on 2026-11-26; raw digest of the full response | `df8150fbcb308e8c58d6d8433a6411446aec84c269a3fa4cd42c3d910d8c042c` | `50ec7ac5a50835252bf9130beef7f5cc2967499d105804421fd897bb9cbabc0b` |

Profiles: UMM 0.31.1, 0.32.4, 0.33.4 (component-assembled from official NuGet packages) and the owner's installed 0.33.0
(read-only copy); see `docs/evidence/umm-0311/loader-profiles.md`. Static evidence only.

The two `.manifest.json` files are the packager's qualification manifests: each names the producing commit `7a9861a5`,
a clean worktree, the ZIP and DLL digests and the DLL MVID. `validate-*-frozen.txt` measure those same frozen ZIPs with
the standalone validators (mod 12/0, observer 8/0; the ZIP digests in their last line equal the manifests').
