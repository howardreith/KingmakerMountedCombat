# Release 0.2.16-alpha.2 evidence

| File | Producer | SHA-256 |
|---|---|---|
| `api-audit-mod.json` | `C:/Dev/missions/kingmaker-umm-0311/tools/audit-loader-api.sh` (LoaderApiAudit, sources in `docs/evidence/umm-0311/loader-api-audit-*.txt`) on the DLL inside `KingmakerMountedCombat-0.2.16-alpha.2-release-diagnostic.zip` (DLL `2105f305…`), built by `scripts/Package.ps1 -IncludeRemovalObserver -ArtifactQualifier release` at `7a9861a5` | `032298b1104f98cee1ff5285d8cf2aaebe48891aef7eab31ccc8c09760195f4c` |
| `api-audit-observer.json` | the same tool on the DLL inside `KmcRemovalObserver-0.1.0-observer.8-release-diagnostic.zip` (DLL `f9697f91…`) from the same packaging run | `972926f092c82d8ce107b05e942c5c68ecde185d61c6160e2d0dca770fe654e7` |

Profiles: UMM 0.31.1, 0.32.4, 0.33.4 (component-assembled from official NuGet packages) and the owner's installed 0.33.0
(read-only copy); see `docs/evidence/umm-0311/loader-profiles.md`. Static evidence only.
