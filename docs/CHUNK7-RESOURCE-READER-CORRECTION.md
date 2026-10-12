# Chunk 7 fractional native-resource evidence correction

Status: PASS for the focused reader correction; native qualification remains IN PROGRESS. The external exploration reader now passes
an explicit double zero to Math.Max when computing remaining native cooldown.
PowerShell's previous integer overload rounded a fractional expected value:
before 3.0, elapsed 1.5 seconds and after 2.0 could be accepted, while the lawful
1.5 result could be refused. The correction matches the production double
max(0, before - elapsed) calculation. It changes no native action or expenditure.

The existing 0.03 tolerance, thirty-second window, finite numeric token checks,
actor identity set, preparation, initiative and reaction checks remain unchanged.
The original reader failed four of eighteen focused synthetic cases; the repaired
reader passed all eighteen. All 176 existing world-reader checks also passed in a
private shadow. Independent source review passed. The earlier fourteen/three
reproduction and every failed log remain preserved.

This is a reader correction. Earlier native failures retain their candidate,
observer and reader identities and remain FAIL. Any later replay needs a separate
reader receipt; it cannot invent missing observations or turn incomplete native
coverage into qualification. The next selected native exploration rerun still
needs the exact reviewed candidate, fixture and restoration evidence.
