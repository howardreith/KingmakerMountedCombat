# Phase 3G implementation and qualification

## Intake and bounded plan — 2026-09-05

IN PROGRESS from clean local/upstream/host-verified remote `6d59b5f42ff5d675e390e55e64e89029c96255ec` on `codex/mounted-combat-phase3f-playable-core`. The Phase 3G mission supersedes historical phase scope and unexecuted review preparation. Keep unified turns, paired scheduler, and overlay false. One active pair; separate native actor turns. No permanent replacement of preview.4 is authorized.

1. Preserve the current installation/cache, saves, settings and human evidence; establish exact current-starting-state guarded continuity.
2. Repair stationary actor-local TB admission and native paused order queuing; preserve native authority and action costs. Trace accepted requests through native start/effect/termination.
3. Make equivalent ordinary clicks continue the owned intent. Repair Horse animation lifecycle, lower only the Horse visual seat, and replace rejected saddle artwork with original painted art.
4. Implement only verified pair-local movement reconciliation at native preparation/movement boundaries. Keep any remaining counterexample a release blocker.
5. Focused regressions, coherent source commit, exact private package, and at most eight new guarded transactions (reserve final RT/TB). Restore intake preview.4 after every transaction. Complete one final applicable gate; publish only through the approved host helper.

## Preserved human baseline

The supplied Phase 3G packet's seven payloads match its SHA-256 manifest. All three unique screenshots were inspected: unmounted native controls, the internal Wrath saddle style reference, and excessive rider height. These are still images, not motion certification. The Wrath image is not a distributable asset.

Howie reports preview.4 native Mount/Dismount, RTWP approach/rider melee, whole-party selection, order precedence, dismount followed by unmounted attacks, and one-click longbow repetition working. The Horse attacks mechanically without visible animation. Repeated clicks on the same enemy delay the next longbow shot. Rider longbow/melee and Horse attacks fail in TB. Paused Mount is disabled. Rider seating needs a small downward Y correction; horizontal placement is close. The clearer saddle art is aesthetically rejected. His wording for the unmounted test is “Successfully did some ranged attacks and reloaded.” This does not establish a bow reload mechanic or a save/load matrix. The historical unmounted Sling fixture timeout remains separate; it is not a blanket unmounted ranged failure.

Current human log preserved read-only at `analysis-cache/phase3g-intake-20260905T233500Z/current-human-output_log.txt`, SHA-256 `4d34d97f2cefabca55676f770d6eabc008765fc1cdb09918a920baf9a9c66084`. Stable source last-write `2026-09-05T23:20:41.2421162Z`, captured `23:35:31.7126097Z`, 141171 bytes. Startup identifies preview.4; stack MVID matches `b3541fbb-9ad7-44d6-b561-1ac341046927`. This is distinct from the historical fallback log. Accepted stock rider ranged/melee and mount commands repeatedly terminate `Interrupt`, zero child attacks; first native failing boundary still under investigation. Buff Planner exceptions remain unattributed foreign diagnostics.

Intake installation is preview.4 plus its legitimate `KingmakerMountedCombat.dll.20370.cache`, both DLL SHA `0dd6913ae2fb21b94d0681d355fc293747123508d32bebe9145aac153e975c07`; installed KMC digest `c5afcfcb17bc3f4813572bd6b0d233a55158576c0e9b0aaf985124014212e93c`. Old suite4 fallback pins are historical. At intake Steam is running; no Kingmaker/UMM process. No live transaction yet (0/8).

## Contracts and first boundaries

- Same-target clicks currently cancel before beginning another generation. Continuation must match exact pair, native actor turn/control mode, target and weapon/action context; cancellation invalidates it. Other party commands remain native.
- `Pause` is currently rejected together with modal UI by action admission. Loaded-world order admission must be separate from execution. Native `UnitUseAbility` retains exact caster/target and owns cancellation; no acquisition or resource consumption before execution.
- Native TB command prediction uses a temporary `UnitCommands` container. Simulation must never acquire real pair intent or dispatch a real attack. Native action/controller progression remains authoritative.
- Horse-only seat correction belongs in mount-root up, preserving the existing backward correction, animated projection, mechanics root, and Mammoth calibration.
- Native `Prepare` clears actor cooldowns. Movement projection currently copies only Move and consults rider Standard/readiness. Neither is a complete pair resource contract; a guessed round counter or rider tax is forbidden.

All new gameplay/visual acceptance remains NOT RUN until exact candidate evidence is recorded. Human baseline above remains HUMAN-REPORTED on preview.4.

## First guarded transaction and second checkpoint

Preview.1 source `e0aab35e135f95ca0c8498be6abf0110b43adefd` was clean packaged and host-published. Run `20260906-phase3g-preview1-tb-A` reached native Horse creation and submitted Mount, then expired at the 300-second host deadline before any Phase 3G attack case. It is FAIL, not evidence that the three TB attacks succeeded or failed at their requested boundary. The manager's blocking UI appears in the trace, without a causal claim about foreign Buff Planner messages. The interrupted parent also lacks a fully reconciled terminal registration/mission result; protocol rejection is retained. Independent audit at `2026-09-06T01:16:12Z` proves exact intake saves/Mods/cache restored and no process/lock.

The next candidate adds a native UMM-close input under the existing reversible overlay lease for these disposable fixtures and a 25-second initial Mount deadline with exact queue/progress state. It adds native paused Dismount, Mount/Stop and Mount/unpause cases. Production pending Mount/Dismount shells are canceled through the existing lifecycle notification and before transient fact serialization/removal; started effects and foreign commands are excluded.

Seating correction is confined to the animated visual projection: Horse mount-root-up `Y=-0.08`, existing `Z=-0.18`, with the historical fixed mechanics anchor unchanged at `Y=-0.08`. The preview.1 anchor adjustment was rejected during source review and is not the delivered calibration. Mammoth remains independent. Native view/motion acceptance remains pending.

The original Horse large portrait was losslessly recompressed, retaining every ARGB pixel and all transparency. PNG bytes `1822711 -> 1197418`, SHA `8b7b4386de1b5adbd9f7f9f1c3728de32325b03c5f2dfc2fe6c7babf95a712e7 -> 07a86203bed11697dcf6ec30d6ad2e54bc3c0fc9ffca14ce3053fbfacc5733de`; decoded pixel SHA `80b990fec63fd32088034e240cf5184a2030df5dbb8b1b3f48f932f059ff049f`. Evidence `analysis-cache/phase3g-intake-20260905T233500Z/portrait-repack-evidence.json`. This preserves the unchanged 4 MB package DLL ceiling without changing portrait art.

Preview.2 source `25632150a70cb5bd7bc6dd852e3d388526e3b427`, run `20260906-phase3g-preview2-tb-A`, mounted successfully then failed before the first attack: the fixture requested a 1.5 m target spawn, below the unchanged 3 m safety boundary. No attack acceptance is claimed. Independent audit at `2026-09-06T01:43:51Z` restored exact intake saves/Mods/cache, game closed and locks clear; preserved log SHA `5f368f54ea6ae57f53e449099ee0347fa3fa79cdaeb6d856330e20e10df7a8b5`. Preview.3 corrects only the fixture spawn separation, requires measured stationary TB attacks, and prevents repeated delivery of an already submitted native mode toggle. Runtime budget used: 2/8; neither prior run reached an attack case.
