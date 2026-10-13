# Chunk 7 native checkpoint — 12 October 2026

Status: IN PROGRESS. All six final native attempts remain FAIL and are settled.
Root was the sole live executor. The latest preview.7 result and exact restoration
are recorded below; earlier sections retain their original candidate evidence.
Later isolated native autosaves do not qualify a follow-up fixture.

Frozen product: b97db97f8f9641eea802a725295161ebdac4ad03, preview.3,
DLL SHA256 096671ee0629acd32d1c3a16906bac508e7afa2d6b2a75d5890ddd2755aee2a4,
MVID 76544775-d44a-443b-8de7-12f2b3e191fa. Package SHA256
6e3de9177c92accb009145bcda6bb03be127fec2bdf2460851bceb3276678418.
Harness HEAD: e557064c773b07a1dc036b7ab8a015f4492fc672. Suite:
chunk7b-world03-20261012, SHA256 aa94fa4c955fccf66e8273aa6af228448c1518ec18d5dc04f7dbc10c2da29cfd.
Private execution manifests and raw logs remain under analysis-cache/chunk7-continuation;
original native records remain under runtime-evidence/<run-id>.

## Actual execution

The unchanged full guarded WhatIf passed in 9400.5291734 seconds (156m40s),
400.5291734 seconds beyond its separately declared 150-minute planning estimate.
It completed without live mutation. No cancelled audit is counted as PASS.

| Run | Configuration | Duration | Actual result |
| --- | --- | ---: | --- |
| 20261012-chunk7b-world03 | KMC with installed UMM/Harmony only | 133.9013171 s | FAIL before fixture completion, 0 passed / 1 failed assertion |
| 20261012-chunk7b-world03-installed-control | Current installed Mods cloned transactionally, unchanged candidate | 105.9559007 s | FAIL, 4 passed / 1 failed assertion |

The first run failed in native Player.PostLoad (token 06000DC7): the saved
MainCharacter identity was absent from CrossSceneState.AllEntityData at the
native Single predicate. Horse registration separately rejected a Ranger
AllFeatures count different from the strict seven-GUID contract. The actual
isolated list has not yet been observed. The loader reader also rejected the
installed HarmonySharedState in-memory assembly and UMM's candidate cache path;
the independently reviewed loader-only correction passes 93 focused checks. It accepts only the observed pinned creator/cache contracts, preserves candidate hashes/MVIDs, and does not change the native FAIL.

The comparison changed the configuration to distinguish the isolation dependency;
it is not base-game proof or a general compatibility matrix. It verified Working
load, native diagnostic Ranger choice commits, and two normal native Mount inputs.
Both exact command/process bindings survived selection changes, with conserved
exploration resources. This fixture construction is not ordinary acquisition proof.
The first ground click then failed the exact-command assertion. Runtime cleanup
passed; independent movement, unmounted movement control and repeated settled cycles
remain unproven. The raw failed artifact remains FAIL and cannot qualify source saves.

## Preliminary measurements

The comparison recorded 30 warmup frames and 120 samples in each completed phase:

| Phase | KMC update median / p95 | Whole-frame median / p95 | KMC messages |
| --- | --- | --- | ---: |
| Zero pairs | 0.1609 / 0.2766 ms | 11.0100 / 11.6784 ms | 0 |
| One pair | 0.2292 / 0.3181 ms | 11.0047 / 11.5239 ms | 0 |
| Two pairs | 0.2462 / 0.3489 ms | 11.0069 / 11.5655 ms | 0 |

These include native diagnostic observer overhead and the installed configuration.
There is no settled-zero or six-pair sample, allocation-rate observation, accepted
performance threshold, or hardening PASS. Process heap deltas are not allocations.
The native catalogue observed Gnome and Halfling Small races; it did not create or
qualify a Small mounted body/pose. Both existing rider fixtures lacked natural next-level XP.

## Restoration and next dependencies

Both combined transaction receipts report phase=restored, no restoration errors,
Mods restored, protected saves passed, Baseline immutable, Working restored and
save-write allowlist passed. Before/after Mods digest:
02b2efc8c35dc0bbd7b041b6fa1f6f9888b51bf0490ab7b9ca34dc0901977dc0.
Before/after save inventory digest:
2e69c4a4b4611327b0acb09d002a996e42980ff23f2ce256ac3d7dd064d9c74a.
The isolated run additionally verifies exact profile bytes and unchanged loader
dependencies. Both attributed game processes exited and the transaction lock was released.

Automatic approval review refused direct save archive inspection and then the
existing read-only fixture guard because both parse save contents. It permitted
only names/lengths/timestamps/raw-hash continuity inspection. No such rejected
command ran; no alternative archive reader was used. Native gameplay comparison
was separately approved through the unchanged guarded runner. Further archive-content
diagnosis needs that access boundary resolved; native logs/source work remain available.

The ground callback defect was reproduced 12 PASS / 7 FAIL and repaired 19/0: a per-owner
stock-intent predicate prevents post-input cleanup from cancelling the fresh move.
Explicit Stop and pre-admission cancellation are unchanged; native rerun is required.
The strict Ranger option guard now reports a bounded observed list on refusal (11/0);
its seven-option acceptance rule is unchanged and the actual base-only list remains unknown.

The subsequent 140-path combined source checkpoint passes production compilation, 805
component tests and 22 selected non-archive checks. Progression diagnostics are integrated;
closed native bootstrap review is still pending. These offline results do not change the
native verdicts above. Next: finish bootstrap integration and affected checks, freeze the
reviewed candidate, then rerun the affected native rows. The original preview.3 evidence
and all failed reproductions remain immutable.
A qualified base-only fixture/registration path remains a required dependency. Missing
native combat/persistence/progression/Small/six-pair evidence and human play/visual review
remain open. No 7B–7D completion, owner-play readiness, or 7E promotion is claimed.

## Preview.4 authored bootstrap

Run `20261012-chunk7b-world04-bootstrap` used source
`953e75be422a28047c352348c5aac9ee83276f42`, harness
`8a31478c749c44d759fa866a9cf59b6d82a2e155`, DLL SHA256
`254e084721c978fd2de061f219d123b8a6cfdfd4832e6ef35559823abbb490df`,
MVID `9dfca5f8-43b7-472f-a522-221d25b74f84`, package SHA256
`8cc053ffdd227d861efa9d0be0a6793940f208f30fc825c4fc45437ec25a4dca`.
The unchanged full WhatIf passed in 9164.7308559 seconds, within its separate
180-minute planning budget. Exact manifest, arguments, frozen source, clean root
and no-game admission were rechecked before launch.

The invocation failed in 136.0062906 seconds. The raw game ran 47.9552938 seconds
and reported preflight FAIL / cleanup PASS. The strict Ranger registrar observed
twelve actual AllFeatures GUIDs instead of the historical exact seven; it refused
before native new-game dispatch. Authorized loads, writes and unauthorized
requests were all zero. This supplies the previously missing bounded list; it
does not authorize relaxing the registration guard.

The producer also wrote `chunk7-native-bootstrap-evidence.json` without adding
it to `runtime-artifacts.json`. The strict launcher correctly refused the missing
record, resulting in 0 passed/1 failed launcher assertion. The original manifest,
raw game 1/1 result and both failures remain unchanged. Loader inventory exists,
but its validation was not reached; `nativeInventoryObserved=false` and
`nativeCompatibilityQualified=false` remain the actual isolation verdicts.

Combined transaction phase is `restored`, with `restorationErrors=[]`,
`modsRestored=true`, `saveProtectionPassed=true`, `baselineImmutable=true`,
`workingRestored=true` and `saveWriteAllowlistPassed=true`. Before/after digests
equal the Mods and saves digests above. The attributed process exited, a later
process query found zero Kingmaker processes, and `active-transaction.lock` is
absent. There is no recovery transaction to settle.

The profile record explicitly differs: six accepted native cache changes under
`native-achievement-cache-settled-size` (one achievement cache rewrite and five
analytics dispatch entries), `profileBytesRestoredExactly=false`. This is not
whole-profile byte equality. The native receipt and restoration bindings are in
`analysis-cache/chunk7-continuation/native-checkpoint03.json`, SHA256
`183789a8689ea2db790ed4b3803ed513c14675bb698d2f516273382ff2f495eb`.

Both archive-access approvals were subsequently received and recorded; the
generated progression15 and mandatory lab archive checks passed. The earlier
access refusals above are historical, not a current blocker. The next candidate
must repair the demonstrated manifest defect and establish the exact native
Ranger registration contract before an affected bootstrap rerun. The reviewed
[cold-inventory and hardening checkpoint](CHUNK7-COLD-INVENTORY-HARDENING-CHECKPOINT.md)
adds independent source work without changing these native FAIL verdicts.


## Preview.5 dispatched bootstrap — FAIL, settled

Run20261012-chunk7b-world05-bootstrap used frozen productd506ce4185e0116d5f365317f0775bc28c83b768 and harness9c9cf6723385f3fac2d34a02a468a18793c13f42. Its113.7347668-second invocation exited1; the game ran56.432249seconds. Exact Ranger13 preflight passed. Native new-game dispatch, callback entry/return and worker settlement were observed; loads0, authorized writes1. The diagnostic probe recorded entered1/exited1/minted0/saveRequests1/prepared1 and refused its identity/world/autosave predicates. Native runtime FAIL and cleanup PASS remain the actual result. Its generated output is not a qualified cold-load or acquisition fixture.

The missing mint observation is an observer boundary under investigation. The real native identity and one save do not replace the required temporal proof. A changed observer requires a newly identified binary and affected native rerun; reader reinterpretation cannot manufacture the missing callback.

Both transaction receipts report restored, no errors, exact Mods and protected-save digests matching the values above, Baseline immutable and Working restored. Profile bytes also match exactly this time, with zero accepted native cache changes. Attributed processes exited and the lock is absent. checkpoint04 SHA25623c32cafe949f07cdd33f38fa3ba7d4e8542297d7ad7eb3800859ea185eb4cf8 binds all evidence and caller exit. All1154 input pins matched a separate post-exit root check; the original supervisor flag afterPinsVerified=false remains unchanged because the exception bypassed its loop.

The preceding full WhatIf audit remains FAIL/exit1 after8599.5957737seconds: its terminal success sentence wrapped across two physical lines and the supervisor recognized only one-line output. A separately reviewed reader reevaluated the immutable raw log and all required original records, passed in2.7360926seconds with actual caller exit0, and retained the original FAIL. Its proof SHA25662f3154746ea93fdbb1397dff5bfb9f13fa27d1d929bee6810a63462bd32ac00 was consumed by the reviewed live supervisor. This is a reader-only reevaluation under VALIDATION-POLICY§5, not a repeated whole audit or gameplay PASS. The historical root-equality claim is launcher-code-mediated; per-root digests were not captured. Both reader verdicts and all failed evidence are preserved privately.

Native budget is4/25. Failed preview.5 prevents its dependent cold/Manual chain; independent source work continues. No breadth-ready, base-only gameplay compatibility, Small, six-pair or HUMAN PLAY claim is made.


## Preview.6 authored bootstrap — October13

Status: FAIL; attempt5 is settled. The fresh full WhatIf passed in8616.1109392s, actual exit0/chunkf21d8b, on clean endpoint harness69bbe062938ae252ad225c42f3bd75900eeeba55 and frozen product887a8cc41a44ed4a6be01921814cffbaadc4844d. It captured1251 tracked and20 additional inputs before/after, verified1278 pins, and produced one full purity assertion. The transient five-file root cache preservation is disclosed in the [source checkpoint](CHUNK7-PROFILE-PROGRESSION-INTAKE-CHECKPOINT.md); this is not a claim of continuous cleanliness.

Run20261013-chunk7b-world06-bootstrap used package14dd6e26a4e98a8ca0ca0c9c94f0cc0e339f428b28878dc2da81c8d9c1ac5f85, DLLd209489d395a85f8f3b92e79ac5039979a691fde0c7af313fd2072dd649fefee and MVIDf104c91e-6b70-456d-b672-ed48ef8071f4. Exact independently reviewed admission5047e212b8d834e5b5b40463dd49bd2964483768c8942354360033acd7c4ae68 admitted only this base-game bootstrap,0loads/1 isolated Auto write.

The actual supervisor exited1/chunk896395 after116.5392515s; game time was56.9304484s. Preflight and cleanup passed; runtime failed. Ranger registration had13 options. Native entry, exit, campaign mint, save request and preparation callback were each observed once, but completion remained false. The prepared-save callback reported its compound descriptor/world/leaf/category check failure. No successful prepared event or qualified output inventory exists. Native SaveRoutine cloning of the request descriptor is a separate local-contract/reproduction diagnosis, not an observation invented from that compound error. The callsite observer now fires; the overall bootstrap remains FAIL.

Both run and Mods transactions report restored with restorationErrors=[]; actual before/after Mods digest02b2efc8c35dc0bbd7b041b6fa1f6f9888b51bf0490ab7b9ca34dc0901977dc0 and protected-save digest2e69c4a4b4611327b0acb09d002a996e42980ff23f2ce256ac3d7dd064d9c74a match. Baseline is immutable, Working is restored, write allowlist passes, profile bytes match and accepted cache changes are0. The attributed process exited; the later read876513 found no Kingmaker/Wrath and no active lock. Isolation records native inventory observed but nativeCompatibilityQualified=false.

Private native-checkpoint05.json SHAafc32f9d5d977b6e0436018e32798530e15d7a3c54003efeff88480725a2af03 binds exact evidence, transactions and caller exit. Independent failed-run/restoration review passed all17 referenced pins. Original native afterPinsVerified=false remains because its exception skipped that loop; separate postcheckea940a92d8437d9f8335bc8bf92f3efb70ca861462d89dafdfc5b8d091496b16 verified1278/0 before source integration. No raw record was rewritten.

Cold inventory and Manual seed require successful same-candidate bootstrap ancestry and cannot use this failed output. Native budget5/25; all five attempts remain FAIL. The unchanged legacy Working loader also retains its earlier base-only saved-main failure. Continue the focused observer repair, independent source work and affected new-candidate proof; do not retry dependent cases or claim breadth readiness.


## Preview.7 terminal boundary — October13

Status: FAIL; attempt6 is settled. The fresh full WhatIf passed in8503.3262789s on product6f6dfaf5a6d8545d9f38f88e8549755b36d2199d and harness e4c25dc3e3b12b905c74e840385f88e218aaf2fc. Actual6706c0 exit0 closed provider24780. Independent review152808be6cc73eeb938407500be0f849898c55cd7b18a612cdc22d57d30d321d verified1,323 pins (1,296 tracked/20 additional/7 named), before/after inventories, exact caller/executor/argv/cwd/chronology and one full purity assertion. Individual root digests were not serialized; the inherited claim remains code-mediated per-root before/after equality, not atomic or continuous proof.

Run20261013-chunk7b-world07-bootstrap used the frozen preview.7 packageSHA3a73ff4f57a5f7cb8259b785b6c8d69de78f1641342eddee80579711faddc9c9, DLL550213b24253f13dabfe797442ffe81fbe24d53d0cef62bad78adf0b277dcd73/MVID76c2f3f5-20a2-406b-82d8-8df0c933be16. Admission8d1e169a4a84b80fba92c73c0a08cbfc175ec97b2ae34f463cfe21d8c389784a selected only this base-only authored bootstrap, zero loads/one isolated Auto,600s plus safe settlement. Supervisor actual387805 exit1 closed provider14967 after114.0094233s; game elapsed56.0873487s.

The prepared-descriptor repair reached its native observation: requested and prepared objects were distinct, completion matched the prepared storage descriptor, and the native autosave completed. Raw game evidence reports5/5 assertions and one write/zero loads. The produced Auto_1.zks is517,542bytes, SHA093a360075050294e43582d220a56f9917144b5545488bd25fa3f5774a2cdaee. It is **unqualified**: the final launcher validator refused movementExperimentEnabled=true and currentGameMode=Dialog against the existing false/Default completion boundary. Game-side PASS does not override launcher/supervisor FAIL or provide cold-load, ordinary acquisition, base-only compatibility or mounted behavior proof.

Source inspection found a concrete host omission: ActivateSaveBackedBoundary enables movement diagnostics, but CompleteSaveBackedCore shuts them down only for non-Chunk7 families. The mode mismatch is separate. Bootstrap captured Default at native-world frame3992; cleanup completed later and final host publication observed Dialog. The specific native story trigger was not observed. Repair must retain cleanup custody, disable diagnostics after settlement, and report invalid final state honestly without forcing story or game modes or weakening the existing validator. Changed observer/host bytes require new candidate identity and affected native proof.

Both run and Mods transactions are restored, with restorationErrors=[]. Actual before/restored Mods digest02b2efc8c35dc0bbd7b041b6fa1f6f9888b51bf0490ab7b9ca34dc0901977dc0 and protected-save digest2e69c4a4b4611327b0acb09d002a996e42980ff23f2ce256ac3d7dd064d9c74a match. Baseline remained immutable, Working restored, the write allowlist passed, and profile bytes restored exactly with zero accepted cache changes. The attributed game exited; root359d14 exit0 verified Kingmaker0 and clean source before subsequent integration. Independent restoration metadata review5cdcce4ab15af59dbaefd2cb99c51ab81b9c90efecf53a29736afcc0b9fb3cb6 checked all12 bound receipt paths, shared transaction token, profile inventory projection and matching recorded digests; it did not rescan live bytes.

Private candidate-world07/settled-native-failure-resume09.json SHA0c33ec085e81b1dd3dddec8d01d8b009f6225ebecef9bd86d991462b4d1e7398 binds the result, raw observation, caller and restoration. Original supervisorSHAde3aafbabb9fbdf719bc214720e53aa096ddc6f1c3856911ba9ebbf72cb6c026 retains afterPinsVerified=false; separate postcheck87fae78c4948b64380f1ae92e935f21a7f9972feecf2e1baf4ab1ce01e28f5ce verified1,323/0 without rewriting it. CallerSHAfbd33663adb47c119d0ed8ec665a0f4a108b3a65248930dd51c91ea9d6edde7a records actual exit1. All raw failed evidence and frozen bytes remain preserved.

Native budget6/25. No cold inventory or Manual seed is admitted from this output. The root has since integrated reviewed independent replay and diagnostic source; preview.7 qualification remains attached to its original bytes and cannot authorize the changed tree. Host repair, a bounded approved Working structural observation, and genuinely eligible native fixture ancestry remain the next dependencies. The breadth candidate is not ready.
