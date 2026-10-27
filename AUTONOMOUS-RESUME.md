# Chunk 7 finalization resume — IN PROGRESS

Read [CURRENT](CURRENT.md), [validation policy](docs/VALIDATION-POLICY.md) and
[the active checkpoint](docs/CHUNK7-CODEX-TAKEOVER-2026-10-26.md).

Machine/worktree: DESKTOP-SRJJ623,
C:\Dev\KingmakerMountedCombatLab\repo\KingmakerMountedCombat.
Branch: `codex/mounted-combat-chunk7-successor`.
Corrective code commit: `f91d63452cb80ef1470859d86febc9ad4f2a3c75`, preview.96.
Last published head: `ebeaf8f6b3d81ed7a189ca4b99fe2e95ceb56ad8`.
This records checkpoint is to be committed with the reviewed offline-test fix;
read `analysis-cache/chunk7-continuation/takeover-active.json` for the exact later
freeze HEAD/worktree and any live custody. No corrected package exists yet.

Custody at2026-10-27 07:34 UTC: no game, wrapper or active transaction; Mods empty.
F6 s1r, the sole B10 startup retry, finished07:32:15 UTC, PASS11/0, exact restoration;
its result was consumed through AwaitOnly/MapKey s1. Do not repeat it. F6 is now
closed as a partial FAIL; do not launch B11 from F6. All existing frozen worktrees,
packages, source branches and failed evidence remain retained.

F6 partial result:81 rows/82 planned launches;50 planned tags launched,52 actual
launches,49 PASS/3 FAIL. Successful attempts contain267/0 subscenarios; all attempts
contain274/3. Original FAILs: r09 native7/1, r28 startup0/1, s1 startup0/1. The
predeclared retries r28r and s1r passed.32 unlaunched slots remain TODO, not excluded.
F6 performance is TODO (not executed). Every attempt restored protected saves,
Mods and settings. k1 admitted five settled native achievement-cache changes;
its profile bytes were not exact. All other attempts restored exactly.
`final6-partial-disposition.json` SHA256
`b95f4a25503de1d20149d19ecbad29240236ea0a75ec0d941ddbd95b0ac83c51`
binds all52 original results/checks. B10 receipt is final6-batch10-complete.json.

The exact installed assembly confirms r09's diagnostic omission: a pre-window
reaction cooldown .82590276 expired across1.621s native game time, renewing count0
to unchanged capacity1 with no new AoO/grant/reset. No standard/move/swift debt
rose. Later r09 stages never ran. The correction observes this bounded native
renewal without resource writes. The original full FAIL remains unchanged.
Reviews: takeover-f6-r09-reaction-renewal-incident-review.md;
takeover-f6-r09-renewal-correction-review.md SHA256
`d50d4dc2437f7de066c459cb224f9d1894185da3d38d978b822a97831baf184c`;
takeover-b219-successor-readiness-review.md SHA256
`e4e8e0c5ce98f17a961d990ed55ef6f504a64064dd113777c54ea98e7c0a42dd`.
Focused03:72 compiled actual-method checks plus31 reader cases,2.1124934s.
Affected owned reader:1462/0,323.3035764s. Preview.96 source validation149/0.
Retain focused01 missing serializer dependencies and02 array-shape failures.
Exact old=false/new=true boundary replay is regression evidence, not a new native
PASS. See final6-r09-renewal-correction.md for exact receipts and hashes.

Next operations: commit the reviewed test-only upgrade replay correction and
coherent records; create a new clean b219 pilot worktree on that HEAD. Use the
reviewed b219-configure-freeze.ps1 and existing Package/Candidate/suite helpers.
Build once, preserve exact hashes/MVID and run one r09 pilot using b219-prepare-tag.ps1
with F6 r01/r02 CrossLineage inputs. Do not launch before package/Candidate/suite/
WhatIf gates. See b219-pilot-plan.md. Corrected native execution remains required.
After pilot PASS, guarded-push source, freeze the successor final suite and execute
the same full81-row/82-launch plan on exact delivered bytes. No F6 or pilot native
PASS transfers to a changed DLL. Fixed F11 limits/protocol remain unchanged.

F6 preserved executor:
C:\Dev\KingmakerMountedCombatLab\worktrees\chunk7-b218-pilot-ebeaf8f6,
branch `codex/mounted-combat-chunk7-b218-pilot-ebeaf8f6`, clean Ebeaf.
F6/b218 package: artifacts/KingmakerMountedCombat-0.1.0-chunk7b-preview.95-c7e-b218-pilots-diagnostic.zip,
SHA256 `5a0d5281d376cac6577a593dbef9166e47556420513355188468b8bd4ea38149`;
DLL `7f48118c7b16025447ff68c2f6138e2d33ee7aa81231578611b59a24569cc576`,
MVID `32ac6aca-d9b4-4db8-9b49-2b5c5fe0f328`.
F6 freeze44bdb802d94414281a7f4d170022b85105f1abab8be89aa8d6009cab0a12e02e,
suite chunk7e-final-ebeaf8f6-20261027,
SHA2562ea084a946f98dbb57931c97ba0ab167fe9b9633319dd47d5608f1e2af9cea2b.
The twelve b218 pilots had passed80/0; their own development credit is preserved.

F6 offline: rawFULL FAIL/exit1,539.6s; remaining249 steps242PASS/7FAIL,0skips,
2672.410s. Six historical failures retain accepted classifications. Step176's
version replay originally failed235/5; reviewed root test correction passed242/0
in32.292s using explicitly pinned historical readers plus current hash/version
negative checks. Original and intermediate241/1 retained. No production change
from that test fix. final6-offline-disposition.json SHA256
`b6030a674b0652d5aa943089cca9bee541787ea6eb997900078621bf857032cc`;
review397060494585ccc3f03dcefa58851bebc67bdaf998a2e2e8d7c1e8e1077cbd28.

Fresh remote inspection07:33 UTC: main d56ff670cca263da5197fd89f69cefd967e2d6b4
is an ancestor; PR5 remains open/draft at Ebeaf, not merged. Recheck before final
freeze/promotion. The owner authorizes the gated merge, with required repository
checks/reviews, exact expected head and independent main/product/package readback.
No release/tag or permanent installation; owner play/visual acceptance remains TODO.

[Prior resume chronology, preserved byte-for-byte](docs/history/AUTONOMOUS-RESUME-before-codex-takeover-2026-10-26.md).
