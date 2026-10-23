# Chunk 7 F11 acceptance protocol for the final candidate

Status: **predeclared protocol.** It is written before any final-candidate measurement
exists. It does not judge any result. The standing completion mission remains IN PROGRESS.

## Inputs that informed this protocol (development evidence only)

| Source | Quantity | Values |
| --- | --- | --- |
| Batch157 (two-pair matched pool, profiler absent) | KMC Update median of window means, 0/1/2/0 pairs | 0.198 / 0.238 / 0.258 / 0.206 ms |
| Batch190 (six-pair lifecycle, custody at window boundaries) | Same, 0/1/2/6/0 pairs | 0.294 / 0.339 / 0.404 / 0.486 / 0.290 ms |
| Batch156 | Admitted managed allocation-event observer | development rate only |
| Batch191 (request-free, external observer) | Whole-frame idle P50 | 11.01 ms; KMC log clean |

These numbers include instrumentation. Batch190 keeps six resident boundaries even at zero
active pairs, and its timed update includes some observer work. Frame times include the game,
rendering and any external observer. Retained heap is not an allocation rate. These limits
carry into the final measurement and are reported with it.

## Final measurements (on the delivered candidate, inside private 7E)

1. **Matched two-pair pool.** The existing matched-pool scenario: 0/1/2/0 settled pairs in the
   same scene and party, three labeled windows per state, declared warmup and sample counts,
   and no command, save, overlay or load inside a window.
2. **Six-pair lifecycle stress.** The existing scenario, with full custody at window
   boundaries. Five states, three windows each.
3. **Allocation events.** The admitted batch156 observer, in its own declared run: bytes per
   Update and per sample second, around the same Update boundary.
4. **Request-free ordinary controls.** The batch191 scenario on the delivered package, for
   quiet startup, logs and controls.

Each window reports:
- the mean, P50, P95 and maximum;
- collections observed inside the window.

Collection-inclusive tails are always reported next to the typical cost. A collection landing
in a timed interval does not identify which subsystem allocated.

## Acceptance criteria (fixed now; not moved after observation)

| Criterion | Rule | Kind |
| --- | --- | --- |
| Population stability | Every window in a state has the same actor, owner, subscription and residue population. Every settled lifecycle cycle returns to the zero-pair population and subscription counts. | Required |
| Quiet logging | Zero unprompted KMC warnings or errors in quiet windows. Zero KMC error or exception lines in the request-free session's UMM log. | Required |
| Scaling | Compare the median at N pairs minus the 0-pair median against the 0-pair window-to-window spread (noise band). The per-pair increment must not grow with N beyond that band. | Investigate any breach; a demonstrated super-linear growth is a defect |
| Absolute budget | Median KMC Update ≤ 0.5 ms at two pairs, and ≤ 1.0 ms at six pairs (diagnostic stress population), instrumentation included. These are about 3% and 6% of a 60 fps frame (16.7 ms). | Required |
| Allocation | No increasing per-Update allocation trend across repeated windows of the same state. | Investigate a monotonic increase |
| Whole frame | P95 is reported as player-impact context only, never as KMC cost. | Context |

The budget is justified as a small share of a 60 fps frame. It keeps the ordinary two-pair
case well below perceptible cost and allows the larger diagnostic six-pair population.
Development numbers informed this budget, which is disclosed here; acceptance is evaluated
only on the final candidate's own fresh measurements. A failing criterion is a defect to fix
and requalify, not a reason to change the rule.

## Not claimed

No final measurement, acceptance verdict, uninstrumented-session result or human visual and
physical-input approval exists yet.
