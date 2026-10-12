# Comparable resident-pool measurements

Status: IN PROGRESS. This adds measurement support for the selected Chunk 7D
campaign; native timing and retention acceptance remain open.

The two-pair exploration scenario keeps its original world evidence and adds
`chunk7-matched-pool-measurements.json`, contract
`chunk7-matched-resident-pool-v1`. The new artifact binds the exact original
world-artifact hash and candidate identity. Old evidence is not upgraded into
this contract by a newer reader.

The first existing mount/dismount cycle warms the resident services. Four windows
then observe zero pairs after cycle one, one and two pairs during cycle two, and
zero pairs after cycle two. The original three cycles and twelve native mounted
ability inputs remain unchanged. Sampling adds no command, resource reset,
preparation, save, load or mode transition.

Every window retains and compares the same 22 distinct native object references:
world, Game, Player, persistent/area state and main actor; two relationship
boundaries and two turn participants; and four actor/descriptor/view triples.
It also validates the actual owner/generation sequence and active pair counts
0/1/2/0. Replaced or aliased references, unsettled native command slots,
preparation, shells, loading, workers or serialization refuse sampling. A
finished command may remain in its native slot briefly; the observer waits for
native Empty within the existing scenario deadline, without clearing the slot
or issuing another input. Sampler and identity custody remain owned until
release succeeds.

Reported Update cost includes KMC safety, scenario and sampler work. Whole-frame
elapsed time also includes the game and rendering. Managed heap occupancy and GC
collection counts are observable, but heap growth is not allocated bytes, an
allocation rate, or proof of a leak. Equal resident pools remove one known
confounder; time/order effects and observer overhead remain. Quiet diagnostics
still perform heartbeat, world and command polling, so these windows are not a
measurement of fully inactive diagnostics.

No optimization or numeric performance acceptance follows from the synthetic
checks. Native comparison requires an admitted field fixture and all four
settled windows on the identified candidate. Six-pair stress, repeated area/load
settlement, and actual diagnostics-off comparison remain separate required
observations. The earlier failed preview.3 run lacks a settled-zero window and
uses unmatched resident pools; its original evidence and limitations remain.
