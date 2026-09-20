# Chunk 5: save-scoped persistence and cold-load recovery

Status: IN PROGRESS. Integration branch `codex/mounted-combat-phase3f-playable-core` preserves reviewed `8a297fa019ff205f50fd7b911b6f6e03d46fbb1a` and native-qualified Chunk 4 source `429377d707a9976be65639e0c27954d8b4ff3717`. Chunk 4 native engineering is accepted; visual/HUD/physical-input and HUMAN PLAY remain TODO.

## Storage and save semantics

The verified installed transaction supports an extensionless `kmc-mounted-state` archive member. Unknown `.json` members are interpreted as area files. Schema2 uses bounded primitive JSON (32KiB, depth12, 4096 tokens), explicit properties/types, finite numbers, bounded collections and unique native IDs; no serialized CLR type construction. An independent serializer avoids the game's global opt-in JSON contracts. Schema1 migrates in memory with no invented combat state. Missing metadata creates no pair. Future schema data is retained and reported; loading never rewrites source metadata.

The native header-write barrier captures one immutable game-thread snapshot before entity shutdown and worker serialization. Metadata enters that native writer before commit; there is no sidecar or post-finalization rewrite. Owned facts/hotbar leases and the serialized AI field are temporarily suspended without dismount, End or resource forfeiture. P01-save-D and P02-save-B prove real Manual writes and same-session control/debt retention followed by ordinary play. Overlapping requests, native quick/auto, overwrite/rotation and abandoned/faulted operations still need P05/P07 work.

Schema2 supplements native saves with combat actor current debt/reaction obligations, initiative/roster/current turn, native AI cooldown/use obligations, game-clock timers, paired participation/condition forfeiture and movement/step/conversion commitments. Historical high-water observations remain bookkeeping; they are never written into native current cooldowns as expenditure. The supported initial combat barrier waits for finished commands in TB. RT and active-command semantics are not yet qualified.

## Load semantics

Restoration belongs to the selected archive's actual load enumeration and newly deserialized Player. Native actor PostLoad precedes publication of Player.GameId, so early restoration binds the new world first and later checks selected campaign/area. Native combat state and TurnControllers are recreated on load. Early actor restoration and constructor-only roster/current/partner rebind restore the saved activation identity before native preparation/command admission. No Start/Prepare/End or completed round effect is invoked by rebind. World housekeeping discards old references without gameplay forfeiture.

Presentation then resolves the same unique eligible actors and restores owned controls once, without acquisition, a Mount cast or a new activation. P02-load-A retained exact saved current debt, native round and paired grant, completed ordinary movement/attack and advanced through two true paired activations with refreshed resources. Remaining End/split/condition/round-effect cases are mandatory. Invalid/missing actor, configuration incompatibility and canceled-load handling still need native P06/P07 qualification; no fresh-action fallback is certified.

## Isolation and evidence

A persistence-specific run-owned root/campaign/native-type/leaf/hash contract protects human saves. Native enumeration, descriptors, screenshots, stash and ZIP temporary paths resolve within the isolated profile. Old scenarios retain strict Working-only authorization. The present mode admits one real Manual destination per owned run. Cold input is the actual prior PASS archive, copied after source ownership/restoration/hash validation. The runner supplies archive/header identity only. The selected native load's header-counter rewrite is suppressed to preserve source bytes; no suppressed save is counted as a write.

| Native evidence | Result |
|---|---|
| isolation-C, preview.2 | PASS14/0; native isolated enumeration/load |
| P01-save-D, preview.6/source f81749d | PASS23/0 + outer PASS; Manual write and usable same-session pair |
| P01-load-A, preview.6 | FAIL1/1; campaign checked before native publication |
| P01-load-B, preview.7/source9da3e05, PID13092 | PASS20/0 + outer PASS; same pair/controls, movement/attack in fresh process |
| P02-save-A, preview.8/source2712f15 | FAIL5/1 before movement/write; native settings refresh discarded the declared temporary TB cache |
| P02-save-B, preview.9/sourcec06098e, PID16516 | PASS26/0 + outer PASS; partial transport, real Manual write, same-session movement/attack and two later paired activations |
| P02-load-A, same preview.9, PID16172 | PASS20/0 + outer PASS; four actor records, saved grant/round/remainder, controls once, movement/attack and two later paired activations |
| P02-rider-save-A, preview.10/sourceaeb8e27, PID15396 | FAIL24/1 after write and mount movement; Primary target rejected as not a valid living target; no cold run |
| P02-rider-save-B, preview.11/sourceabb9afc, PID4040 | FAIL24/1; target had1 HP and was unconscious after2 native damage, so rejection was correct; no cold run |
| P02-rider-save-C / load-C, preview.12/source251a62a, PID12252 / 12324 | PASS36/0 and29/0, both outer PASS; rider spent, mount remainder, native health, spent-work rejection and two later grants |
| P02-partner-save-A, same preview.12, PID14672 | FAIL23/1 after write; mount stopped at longer reach, so rider lacked legal approach after mount expenditure; no cold run |

Earlier P01 failures and exact historical identities remain in the journal and immutable lab evidence. P01-save-D archive SHA256 `84ffb91c85fe10b0ccacc92befe9a1e4fd2be88e639f197f79bf9bd678c83270`; P02-save-B archive SHA256 `e65a6d5ca94596c4d7f68e5e6b945a6022ea6a6c50dc6f8f86b6247697787439`, under its owned `runtime-staging/persistence-20260920-chunk5-P02-save-B/Saved Games/Manual_300_KMC_P01.zks`. The cold process reads those bytes and supplies no missing gameplay state. At round1/grant1, mount Move0.17698051 and rider Standard/Move0 survive; grants2/3 refresh normally. Source PID16516 exited and fully restored intake21:54:15.0066094Z; cold process started22:00:52.1487984Z.

The P02-only diagnostic setting lease now reapplies its declared temporary boolean cache after the verified native SettingsRoot.HandleSettingsUpdated cache reset. It does not set persisted settings or actor/action state. Save-B confirms the causal fix; production restoration still respects active policy.

## Frozen checkpoint and remaining gates

Preview.9 source `c06098e2917b8cacdd839b85fa5e1fbe375f2e93`; DLL SHA256 `c22180b317555b81682ae9055e43d5906d3cfe145cc071b1eb35a969fd3c8dc3`, MVID `52ff0010-ff9c-48e0-b34a-1ec87572297f`. Private `KingmakerMountedCombat-0.1.0-chunk5-preview.9-p02-config-refresh-diagnostic.zip` SHA256 `956ac1aaa394275a70aef8090d5d10c38584ec1e6ebcba408db8bf5929eca869`; manifest `811d510371ca47d77426925b16b7e2646e3abd61a7240018a5c9a65d0985ef56`. Later documentation does not rebuild this payload.

Build/source22/0, package11/0 and installed-assembly/storage contracts30/0 PASS. Relevant unchanged components412/0, data42/0, harness253/0 and owned-copy guards11/0 PASS. The owner-approved DLL entry cap is5 MiB; ZIP/Info limits, allowlists, hashes, dependency checks and runtime protections are unchanged. These checks are separate from native qualification.

| Final scenario | Status and remaining work |
|---|---|
| P01 | Causal round trip PASS across previews6/7; exact-final rerun required |
| P02 | Partial movement PASS on9 and rider-spent PASS on12; exhausted, End/pending and partner-order cold boundaries remain |
| P03 | TODO: step/conversion/reaction, conditions, split/suspension and exactly-once round effects |
| P04 | TODO: native RT unmounted control, active commands/projectiles and loaded outcomes |
| P05 | TODO: manual/quick/auto, overwrite/rotation, repeated requests, copies/renames and alternating saves |
| P06 | TODO: native legacy/current/malformed/future schema, invalid pair and campaign cases |
| P07 | TODO: failed/canceled operations, views/areas, disable/re-enable and bounded removal |
| P08 | TODO: exact-final accepted gameplay regression with post-load variants |

Paired activation=true; both legacy authorities and overlay=false. Full Charge remains Chunk6; safe rejection is not its completion. Every mandatory row must pass on the final frozen candidate. Visual/physical-input review is separate.

Preview.10 adds exact P02 checkpoint parameters and unrelated-turn-order/rejected-work assertions. Components413/0, fixture25/0 and harness254/0 pass. Preview.11's life observation explains the failed Primary cases: the stock companion blueprint produces a one-HP enemy without a master. Preview.12 provisions native HP BaseValue256 only at owned enemy creation, with no temporary modifier, blueprint edit or cold-process repair. CharacterStats.HitPoints and ModifiableValue.m_BaseValue are verified native JSON members; metadata duplicates no health state. Source22/0 and native contracts31/0 pass. Rider-save-C36/0 and cold-load-C29/0 on12 pass. Archive f47704e40ab3fc273a4656c54f2c2b4c56e6a3a5475487b5e068fecb5deb3f18 carries the rider's spent Standard and legitimate mount remainder. Partner-save-A failed because mount reach exceeded rider reach; preview13 moves the fixture within measured rider reach using native ground input before mount expenditure. No production reach, cost or persistence change; native13 remains pending.

## Human restoration, removal and manual review

Actual human preview.54 DLL `2203a68ca13dfebd1fc52be7c15521f3c2503c98cd53a891dd210ba0611019e9`, protected saves/settings/caches/foreign Mods were restored after each completed transaction. Rider cold PID12324 completed22:48:19.4710003Z and restored22:48:35.5329574Z. Latest failed partner source PID14672 completed22:52:58.9764128Z; full restoration22:53:14.7110767Z. No game/transaction remains. Separate preview.37/preview.13 backups are retained. Recheck actual intake before each transaction; historical pins are not rollback authority.

Prepare-to-Disable/removal implementation and native qualification remain TODO. Permanent custom Horse dependencies are separate from transient pair metadata; arbitrary DLL deletion is not certified. No permanent candidate deployment, main merge or public release is authorized.

Manual checklist after engineering qualification, using an authorized disposable save:

1. Mount the existing pair, move/attack, and note remaining actions.
2. Make a real save; verify the pair, controls and remaining actions still work.
3. Quit fully, launch a fresh process, and load that exact save.
4. Verify the same actors, controls and legitimate remainder; advance two paired activations.
5. Save again, then repeat a quick/auto and copied-save round trip.
