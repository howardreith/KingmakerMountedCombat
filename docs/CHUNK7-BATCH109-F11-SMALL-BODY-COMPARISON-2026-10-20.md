# Chunk 7 batch109: bounded Small-body comparison hardening

The mounted Small body policy no longer sorts and copies observed equipment
names during its repeated active-pair comparison. `DeclaredSmallRiderBody`
compares the bounded actual arrays with its already sorted declaration by
counting each distinct ordinal value, and compares the twelve ordered current
equipment slots directly. It preserves duplicate rejection for Doll IDs,
visual-name multiplicity, null and whitespace refusal, and the existing body
admission policy. The native capture still allocates its observation arrays;
this change does not establish an allocation rate or measured speedup.

One focused regression covers accepted repeated visual names and a rejected
different multiplicity. `scripts/Test-Tiers.ps1 -Tier Fast` passed 3/0 in
0.68 minutes, including component tests and 147 source checks. The affected
world115 quiet/defaults contract passed 28/0, and the diagnostics-off source
contract passed 29/0 before this source edit; those checks are bound to the
earlier world115 DLL and are **not** a native or new-byte qualification.
An independent read-only review found no concrete behavioral regression in
the comparison; it did not run the tests or game. Its noted limit is the
quadratic bounded scan (maximum 128 names), while the declared Halfling
profile has four Doll IDs and eleven visual names.

No game, package, preview or runtime transaction was run for this batch.
Batch108 native PASSes remain evidence for their frozen world115 candidate,
not for these newly compiled source bytes. F11 allocation-rate measurement,
native diagnostics-off and ordinary-player controls remain open, as do other
standing-mission gates. The next native Small-profile candidate should cover
the affected admission behavior and a neighboring unmounted or Medium control.
