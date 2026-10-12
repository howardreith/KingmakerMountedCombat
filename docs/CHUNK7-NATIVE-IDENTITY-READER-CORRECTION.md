# Native object identity reader correction

Status: PASS for the focused reader repair. Native bootstrap/cold-load qualification remains IN PROGRESS.

The producer retains each exact native object and labels it with RuntimeHelpers.GetHashCode. These labels are nonzero signed Int32 values. The registry capacity of 128 limits how many objects are retained; it does not limit the numeric labels to 1–128. Bootstrap and cold-inventory readers incorrectly imposed that numeric range.

Each reader now validates a strict integer token in the signed Int32 range and rejects zero. Registry count/capacity, collisions, membership, callback/world equality, distinct roles, empty-menu null sentinels and cleanup checks remain unchanged. The exact native12+Horse Ranger gate is unchanged. No source decoding, command or save authority is added.

The matched three-family JSON reproduction passed its three original graphs and failed all six bijective positive/negative relabels (3 PASS / 6 FAIL; 2.007s). The identical nine cases passed after correction in2.335s, including the independently authored private Manual seed reader. The first attempt used an obsolete Ranger fixture and an output-variable collision; it is preserved as test setup evidence, not the valid label reproduction.

The new portable bootstrap/cold check passed84 in3.898s. It covers signed endpoints, mixed signs, exact128 entries, zero, malformed token types, out-of-range values, duplicate/missing labels, changed callback/world owners, registry faults, released state and count/capacity violations. Existing affected bootstrap96 and cold151 checks passed in2.336s and7.502s. These are JSON-reader tests with no game or archive operations; no product compilation was needed for the reader-only change.

Private evidence: analysis-cache/chunk7-continuation/native-label-domain-review. Integration map SHA256a3db41ee2bdf28c45252a6c13872d6d165c2789d07097a835472a17af27f8883. Independent root review root-review/source-review01.json SHA25659d73a97f711c3c8f79cc6f916265102ee020ee09191a091abbf8dbf23889b5e. Verified four-file integration root-integration01.json SHA2566db73215ad3df64928315b087e56f41eb2a2dccdd32bec52060cf3c647ff88f0. All original/new reader verdicts and raw native failures remain intact.

The Manual seed family still needs final combined integration/review. Preview.4 stopped before a native world graph existed; this correction cannot turn that failure into PASS. The next candidate still needs a successful guarded base-only bootstrap, separate cold inventory and exact restoration before its derivatives can qualify.
