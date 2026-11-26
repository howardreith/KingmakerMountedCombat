<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r11-request part=1/1 sha256=a5c58ec1ae1be2a073a9077744a293695ac0ab6c59cce2d92f4c9c9c51a34063 -->
### Claude Opus — review request — round 11 — candidate `082e8063a1`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r11-010b60` |
| Checkpoint | `final` (final) |
| Candidate H | `082e8063a1ff55929094d857eaa8bfd072a84fed` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `kingmaker-umm-0311-mission-20261123` sha256 `f7d990f396` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 2, sha256 `0fae88d4efb378d9cd56cb26af6a0f53c92a8a460fe4e32617072804ddaa47ff` |
| Developer observed | foreground session `e178a93b-ca53-4b6b-a4d2-a357d48eaa77`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 11 of 13 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Prepared under owner amendments revision 2 sha256 0fae88d4efb378d9cd56cb26af6a0f53c92a8a460fe4e32617072804ddaa47ff. Mounted Combat integration and alpha-release continuation through PR #62's own enrollment (owner amendment 2). PR #62 (UMM 0.31.1 baseline) was owner-accepted at reviewed source 060d21c1 / archive 20bf1351 after 10/10 correction rounds; that record is kept as history. PR #62's branch was fast-forwarded (normal push, no rewrite) from 20bf1351 to the integrated candidate first prepared on codex/umm-0311-integration-20261126 (draft PR #69, whose enrollment never reached a review and is being closed as superseded): 3e2c44a6, the merge of main 783b2b3d (Mounted Shield, PR #61) with 20bf1351 as first parent; 7a9861a5, the 0.2.16-alpha.2 release preparation; ce89fe48, records (CURRENT.md section, docs/evidence/release-0.2.16-alpha.2/); 082e8063, records only (CURRENT.md, AUTONOMOUS-RESUME.md and one release-notes line: PR #62 carries the candidate, and the tag binds to the producing commit). The archive 20bf1351 is unchanged and an ancestor of the head.

Merge resolution (details in the 3e2c44a6 message). main carried #63's earlier companion pin 5be0f737; PR #62 had already reconciled #63 in 0cbf0167 (adopted its native-target alignment, frozen inputs and compiler-input check adapted to PR #62's UnityModManager+Harmony12 pins; did not carry #63's separate UMM-only reference, restore script and records). (a) Thirteen files that main changed only through 5be0f737 keep PR #62's reconciled form: nine conflicted (docs/CHUNK7-NATIVE-LOADER-IDENTITY.md, docs/CURRENT-SUPPORT.md, planning/UMM-LOADER-PROFILES.json, scripts/Test-FrozenProductSource.ps1, scripts/Test-Harness.ps1, scripts/Validate-Package.ps1, scripts/runtime/Chunk7WorldEvidence.ps1, scripts/runtime/FrozenProductSource.ps1, scripts/runtime/Test-RuntimeGameResult.ps1) and four auto-merged (LocalGamePaths.props.example, planning/ENVIRONMENT-FINGERPRINT.json, scripts/Build-Local.ps1, scripts/Test-Chunk7NativeLoaderContracts.ps1) where the auto-merge had re-added #63 lines. 'git log fb743911..783b2b3d -- <file>' lists only 5be0f737 for each. (b) KingmakerMountedCombat.csproj: PR #62's pinned references and targets plus exactly main's Mounted Shield additions (MountedShieldFeat.png embedded resource and the eight Mounted Shield/support sources). (c) Info.json keeps main's 0.2.16-alpha.1 version line and ManagerVersion 0.31.1. (d) #63's scripts/Restore-UmmBuildReference.ps1, scripts/Test-UmmBuildReference.ps1 and planning/UMM-BUILD-REFERENCE.json are removed: nothing compiles against obj/references/umm-0.31.1 any more. scripts/Test-MountedShieldFocused.ps1 drops that one child (18 -> 17 children) and points its 0.31.1 loader-binding check at the build's own pin obj/loader-references/UnityModManager.dll (the same genuine bytes, sha256 2e038ff2..., per scripts/loader-reference-pins.json and the removed planning file). The dated docs/umm-baseline records stay unchanged as history. (e) CURRENT.md and AUTONOMOUS-RESUME.md keep both sides' sections (PR #62's first, then main's Mounted Shield sections; the shared 0.2.15 section once).

Release preparation 7a9861a5: version 0.2.16-alpha.2 in version.json, Info.json and BuildIdentity.cs (repository convention; assembly version stays 0.1.0.0). The removal observer keeps 0.1.0-observer.8 (its Info.json and ObserverIdentity agree; PR #62 set its minimum to 0.31.1; no observer.8 package was ever a release asset). New docs/RELEASE-NOTES-0.2.16-alpha.2.md (alpha prerelease, 0.2.15 stays stable/Latest; Mounted Shield alpha limitations carried from docs/mounted-shield/PRIVATE-ALPHA.md; loader evidence table distinguishing declared minimum, pinned compile baseline, static audit, offline binding checks and native NOT RUN); README, CURRENT-SUPPORT, CURRENT.md and AUTONOMOUS-RESUME.md status. Publication plan after readiness: merge PR #62 to main; annotated tag v0.2.16-alpha.2 on 7a9861a5, the commit that built both packages (their manifests record commit 7a9861a5, worktreeClean true); a GitHub prerelease (--prerelease --latest=false; 0.2.15 stays Latest) with both packages and their manifests, published asset hashes verified after upload. Later commits change records only (no package entry: each ZIP holds only Info.json and the DLL). Native gameplay and combined-stack testing: NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| KMC_WORKFLOW_BUDGET_ID=release-0.2.16-alpha.2 python agent_review.py verify --enrollment kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 --gate focused -- -Tier Fast -Focused Test-MountedShieldFocused.ps1 (candidate 082e8063) | yes | pass | 78.7s | This enrollment's receipt gate-focused-082e8063a1ff-d43d449b6c4-aa425160ecb-20261126T162532687087Z.log on the reviewed head; the same selection passed in this enrollment on ce89fe48 in 126.5 s (gate-focused-ce89fe48d6d3-d43d449b6c4-aa425160ecb-20261126T162156379573Z.log). Components 1525/0, Validate-Source 153/0, MOUNTED-SHIELD FOCUSED 17 of 17, including the UMM loader bindings 25/0 on the 0.31.1 compile pin and 25/0 on the installed 0.33.0. Offline only. |
| Earlier receipts of the superseded #69 enrollment kmc-codex-umm-0311-integration-20261126-20261126080859-0649 (cited as history, not as this enrollment's runs) | no | pass | 140.6s + 265.2s + 85.5s | Mounted Shield focus on 7a9861a5 (gate-focused-7a9861a5f56c-d43d449b6c4-aa425160ecb-20261126T131606985379Z.log), Test-PackageAdmissionAndReuse focus on 7a9861a5 (gate-focused-7a9861a5f56c-d43d449b6c4-aa0bb756c59-20261126T132044213200Z.log: components 1525/0, Validate-Source 153/0, package admission and reuse 65/0), Mounted Shield focus on ce89fe48 (gate-focused-ce89fe48d6d3-d43d449b6c4-aa425160ecb-20261126T132557848834Z.log). Same source; counted in the same release budget. |
| scripts/Package.ps1 -IncludeRemovalObserver -ArtifactQualifier release (at 7a9861a5, clean tree) | no | pass | 52.2s | One clean build, both products: KingmakerMountedCombat-0.2.16-alpha.2-release-diagnostic.zip sha256 64a4317c..., DLL 2105f305... (the DLL both gates tested), MVID 34815fb3..., validator 12/0; KmcRemovalObserver-0.1.0-observer.8-release-diagnostic.zip sha256 2c0fe382..., DLL f9697f91... (byte-identical to PR #62's observer), validator 8/0. Identities recorded in CURRENT.md at ce89fe48. Counted in the budget by append-only import. |
| Static loader API audit (LoaderApiAudit) of both packaged DLLs on UMM 0.31.1, 0.32.4, owner 0.33.0, 0.33.4 | no | pass | 0.4s | docs/evidence/release-0.2.16-alpha.2/api-audit-mod.json (14 types/42 members) and api-audit-observer.json (8 types/20 members), all resolved on each profile. Static evidence only. |
| Workflow budget release-0.2.16-alpha.2 | no | pass | 0 | Separate release budget, 30-minute owner allowance (adjustment 90 -> 30 recorded with the authorizing text; kept, not renewed, by owner amendment 2); about 12.5 minutes spent with this enrollment's two gates; the completed correction budget umm-0311-baseline is unchanged (47.64 of 48.62 min; file hash identical before and after). Includes a failed first gate attempt (4.7 s, build step: pins not yet acquired in the new worktree) and the imported direct package and audit runs. |
| Native checks | no | not_run | 0 | Not authorized (no installation, launch or save change); release notes state NOT RUN, startup included. |

**Evidence references:**
- prepared under owner amendments revision 2 sha256 0fae88d4efb378d9cd56cb26af6a0f53c92a8a460fe4e32617072804ddaa47ff
- CURRENT.md
- docs/RELEASE-NOTES-0.2.16-alpha.2.md
- docs/evidence/release-0.2.16-alpha.2/INDEX.md
- scripts/Test-MountedShieldFocused.ps1
- src/KingmakerMountedCombat/KingmakerMountedCombat.csproj

**Owner amendments to the mission — revision 2, captured-source sha256 `0fae88d4efb378d9cd56cb26af6a0f53c92a8a460fe4e32617072804ddaa47ff`.** Applied by the owner with `resume --amend-mission`; each owner message is preserved verbatim, in order. The effective task is the original mission plus these amendments. The reported work comes from a developer invocation whose input included them, and the reviewer of this round receives a byte-exact copy of the captured document. Public rendering after the controller's standard sanitization; the digest identifies the private captured bytes, which this rendering can differ from.

```text
# Owner amendments to the mission

Cumulative record kept by the agent-review controller. Each owner message below was applied by the owner through `resume --amend-mission` and is preserved verbatim, in order, between its labeled marker lines. Later amendments supersede conflicting earlier requirements; requirements they do not change still apply.

----- BEGIN OWNER AMENDMENT 1 (captured 2026-11-24T21:39:33Z; message sha256 29ebc773d4294f39ce94c84cdd004f90e665f4da94a292a354842ae576b1eb2e) -----
Freeze the UMM 0.31.1 mission scope.

Remaining work must directly establish the agreed UMM baseline, substantiate
the agreed compatibility claims, or resolve a regression introduced by this
mission.

For each proposed correction, identify that connection before implementing it.
Disposition findings outside those boundaries as separate follow-up work
through the existing review process.

Prefer the smallest correction. If unnecessary tooling introduced by this
mission causes a blocker, consider removing that addition rather than
completing or expanding it. Preserve unrelated work and existing safeguards.

Use focused behavioral verification and existing required gates. Do not add
new frameworks, qualification campaigns, or acceptance requirements.

This amendment grants no additional review rounds, validation time, native
execution, merges, or releases.

The endpoint remains reviewable UMM 0.31.1 baseline PRs with accurately stated
compatibility evidence.
----- END OWNER AMENDMENT 1 -----

----- BEGIN OWNER AMENDMENT 2 (captured 2026-11-26T16:18:15Z; message sha256 e68496978aae6ed4e97eeaff2055ed3e5660cb8984676f230fab7bb8ecef156b) -----
MOUNTED — USE THE ORIGINAL ENROLLMENT

I explicitly authorize continuing the integration and alpha-release work
through Mounted PR #62's existing enrollment.

This includes the supported owner amendment while #62 is open, draft,
and AWAITING_OWNER_REVIEW. Preserve the earlier acceptance as historical
evidence while allowing the controller to supersede the CURRENT handoff
for the new integrated candidate.

The prior accepted source and archive remain:
- Reviewed source: 060d21c1.
- Archive: 20bf1351.

The already-prepared integrated candidate is on:
- Branch: codex/umm-0311-integration-20261126.
- PR: #69.
- Reported head: ce89fe48.
- Integration merge: 3e2c44a6.
- Release preparation: 7a9861a5.

Verify those live identities and ancestry before proceeding.

1. Inspect the exact command and stated reason for the earlier automatic
   permission-review rejection. Preserve that record.

2. Apply the supported owner amendment to #62's existing enrollment,
   authorizing only this integration and alpha-release continuation.

3. In a SEPARATE supported command, set the absolute review cap to 13:
   ten completed correction reviews plus the three integration reviews
   previously allocated to #69. #69 spent none of those reviews.

   Do not combine --amend-mission and the changed --max-rounds in one
   command; use the documented amendment and extension sequence.

4. Through the normal guarded route, fast-forward #62's original branch
   to the existing integrated candidate after the amendment is admitted.

   Preserve the reviewed archive byte-for-byte. Preserve Mounted Shield,
   the UMM 0.31.1 changes, and the release preparation.

   No force-push, history rewrite, archive transplant onto main,
   replacement enrollment, or controller-state manipulation.

5. Obtain the required gate and independent integration review in #62's
   enrollment. Reuse existing evidence only as the workflow permits.
   Never copy or edit another enrollment's gate receipt to impersonate
   a new run.

6. Once the supported #62 continuation is established, cancel/close the
   redundant #69 route as superseded, using normal supported controls.
   Preserve its discussion and records.

7. After the integrated candidate is ready, complete the previously
   authorized merge and GitHub alpha/prerelease publication.

   Use 0.2.16-alpha.2 if it remains the correct unused version.
   Package and validate the main mod and removal observer through the
   existing release workflow.

   Do not mark the alpha Latest or advance a stable-only update feed.
   Preserve the existing stable release.

   Bind the release tag and published assets to their actual producing
   source and verify the published asset hashes.

Keep the existing release-0.2.16-alpha.2 budget:
- Total allowance: 30 minutes.
- Preserve all spending, reported at approximately 9.1 minutes.
- Do not create a fresh release budget.
- Keep the completed correction campaign's ledger unchanged.

This authorization does not permit changing permission settings, guards,
or controller code. If automatic permission review still rejects a
supported operation, stop that operation and report the exact command
and stated reason. Do not retry through a different wrapper or route.
Continue independent authorized work in the other repositories.
----- END OWNER AMENDMENT 2 -----
```

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
