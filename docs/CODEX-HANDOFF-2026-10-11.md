> Current qualification: the owner subsequently authorized final execution, and
> the unchanged preview.211 campaign, both completion ledgers and final FULL passed.
> See [the engineering closeout](CHUNK6-ENGINEERING-CLOSEOUT-2026-10-11.md).
> The record below is historical: its assumed outcomes and original failures
> remain unchanged. Its old next-command recipes are not an active assignment.
> Human acceptance remains pending; the October 11 focused policy still governs.

> Current owner directive, October 11, 2026: docs/VALIDATION-POLICY.md governs
> validation frequency. The campaign below remains pending final qualification,
> not this workflow repair's first task or a requirement for ordinary review.
> Preserve all historical evidence and actual current installation protection.

# Handoff to Codex — 2026-10-11

You are taking over the Kingmaker Mounted Combat mod after the Chunk 6 closeout. Read
`docs/CHUNK6-CLOSEOUT-SUMMARY.md` first: it states what was observed and what was assumed. This document is the operating
guide — where things are, what to do next, and what will bite you.

## Where the work stands in one paragraph

Branch `codex/mounted-combat-phase3f-playable-core` is merged to `main` and released as `v0.1.0-chunk6-preview.211`. Every
named gap of the 2026-10-09 consolidation is closed in code and proven on candidate preview.209, whose ledger reads 86 PASS
/ 0 FAIL / 1 BLOCKED with the Chunk 5 completion gate passing on all 105 mandatory behaviours. Two real product defects
were found by running those cases and fixed (the deferred-save deadlock, and the delivered ranged attack's reach at the
envelope boundary). The released candidate preview.211 adds the second of those fixes plus a fixture change that finally
produces the last missing row, but **its acceptance campaign was stopped at stage 25 of 109 on the owner's instruction**,
so no ledger yet describes the released binary.

## Pending final qualification: acceptance gate on preview.211

The candidate is already frozen, its purity proof already passed, and the stage bindings already exist. Nothing needs
rebuilding. When final qualification is explicitly selected, run the campaign, then the Chunk 5 matrix, then the ledgers.

Lab root: `C:\Dev\KingmakerMountedCombatLab`. Pipeline directory:
`analysis-cache\chunk6-continuation\20261005-6br` (referred to below as `$lab`).

```powershell
# 1. Campaign. Stages 1-24 already PASS under the prefix c6-final211-a and are skipped automatically.
#    Launch detached - never as a foreground or background shell job.
& "$lab\Start-DetachedController.ps1" -Name 'final211-batch-b' `
  -WrapperArguments "-NoProfile -ExecutionPolicy Bypass -File $lab\Run-FinalBatch211.ps1 -FirstStage 25 -LastStage 109"

# 2. Chunk 5 matrix on the same payload, after the campaign receipt appears.
& "$lab\Start-DetachedController.ps1" -Name 'final211-matrix-b' `
  -WrapperArguments "-NoProfile -ExecutionPolicy Bypass -File $lab\Chain-Chunk5Matrix211.ps1"

# 3. Any stage that retains a failure can only be re-run under a FRESH prefix. Plan b is already cloned:
& "$lab\Make-Plan211b.ps1"        # binds every PASS stage, re-runs only the rest as c6-final211-b
& "$lab\Write-Bindings211b.ps1"
& "$lab\Start-DetachedController.ps1" -Name 'final211b-batch' `
  -WrapperArguments "-NoProfile -ExecutionPolicy Bypass -File $lab\Run-FinalBatch211b.ps1 -FirstStage 1 -LastStage <n>"

# 4. Closure records (must run with HEAD == the freeze source and a clean worktree).
& "$lab\Close-CampaignFinal211.ps1"  -Summary '<what happened>'
& "$lab\Close-CampaignFinal211b.ps1" -Summary '<what happened>'

# 5. Chunk 5 ledger, then its gate. In-process: the override hashtable does not survive a -File hop.
& 'C:\Dev\KingmakerMountedCombatLab\analysis-cache\chunk5-persistence\Build-Chunk5Ledger211.ps1'
& '.\scripts\Test-Chunk5Ledger.ps1' -LedgerPath '...\chunk5-ledger-final211.json' -LabRoot 'C:\Dev\KingmakerMountedCombatLab' -Completion

# 6. Merge the plans, build the 6A ledger, validate it, then install both ledgers into docs\.
& "$lab\Merge-Plans211Settled.ps1"
& "$lab\Build-FinalLedger211.ps1" -FreezePath "$lab\candidate211-freeze.json" `
  -PlanPaths @("$lab\final-plan211-merged.json") -PurityReceiptName 'purity-receipt-preview211.json' `
  -PurityRunId 'c6-final211-a-whatif' -OutPath "$lab\chunk6a-ledger-final211.json" `
  -Chunk5LedgerRelativePath 'analysis-cache/chunk5-persistence/chunk5-ledger-final211.json'
& '.\scripts\Test-Chunk6aLedger.ps1' -LedgerPath "$lab\chunk6a-ledger-final211.json" -LabRoot 'C:\Dev\KingmakerMountedCombatLab' -Completion

# 7. Only then: FULL tier once, and the documents.
& '.\scripts\Test-Tiers.ps1' -Tier Full -AssemblyTarget Kingmaker
```

Expect roughly four hours for the campaign, three and a half for the matrix, and ten minutes each for the ledger build and
its validation. Everything is detached; watch the receipts, not the logs.

## Identity rules that decide how much work a change triggers

- `Test-Chunk6aLedger.ps1` re-derives the harness identity from the repository's **current** `scripts/` bytes and refuses a
  ledger whose recorded `harness` differs. Commit reader corrections **before** building a ledger. Any later reader change
  invalidates the installed ledger until it is rebuilt.
- A composite's Chunk 5 binding requires the Chunk 5 ledger's `commit`, `dllSha256`, `dllMvid`, `version` and
  `packageSha256` to equal the 6A ledger's payload. **A new product candidate therefore needs a fresh campaign *and* a
  fresh 105-row matrix.** No PASS is ever carried across binaries.
- Only a native-PASS run whose outer errors are all admitted reader refusals binds through `Get-KmcChunk6aReevaluation`.
  The admitted prefixes include `Chunk 6A closure: ` but **not** the `6C ` or `6E ` family prefixes; a reader-refused 6C or
  6E stage is settled by its family reader plus a harness-evaluation record instead.
- A stage with retained evidence can only be re-run under a fresh run prefix. The runner refuses "Run already exists".

## Operating rules you must keep

- **Launch every multi-minute job detached** through `Start-DetachedController.ps1`. A tool-host memory kill once lost a
  97-minute proof that had been started as a background shell job.
- **Never edit the worktree while frozen stages remain.** Reader bytes are hashed into the freeze; a stash or checkout that
  rewrites line endings breaks every frozen hash.
- **Never remove a runtime lock by hand.** Use `scripts/runtime/Recover-KingmakerRuntimeTransaction.ps1 -Confirm:$false`.
- **The native engine owns every cost.** The product routes to and observes native cost owners; it never writes, clears,
  refunds or synthesizes resource state. Several readers exist solely to enforce that.
- **Retain every failure.** No timeout increase, fixture manipulation or relaxed assertion merely to obtain a PASS. If you
  cannot resolve a failure, it stays a bounded known issue with its evidence.
- **Do not touch** the pinned game installation, the owner's `preview.105` install, the protected saves, UMM or settings.
  `SkipIntro` in the Mods folder is the owner's own mod, not contamination.
- Files carry **mixed line endings**. Match a patch anchor exactly as it appears; a blanket CRLF or LF conversion will
  silently fail to match or will rewrite unrelated lines.

## Known intermittents, with their signatures

| Signature | Reading | Recovery |
|---|---|---|
| `Dismount ground setup lost native engagement: riderEngagesTarget` | the engine clears engagement in the first motion frame of a short ride | fresh-prefix re-run |
| `A native achievement cache leaf changed length ... achievements.dat 0 to 12288` | the native profile writes its achievement cache during an owned process; leaves a lock | recover the transaction, then fresh-prefix re-run |
| `P03 condition stage timed out: 5` | the restored turn-based continuation did not reach its attack in the window | fresh-prefix re-run (passed in 1.6 min on re-run) |
| `ItemNotFoundException: ... orchestration.json` with `result=PASS` | harness observation race; the run itself passed | re-run, or bind the run's own settled result |
| `The exact approach, invalidation stimulus, terminal or restoration did not settle at the unchanged 30-second deadline` | death-restoration timing: the engine recomputes `LifeState` on its own next tick after `Resurrect` | fresh-prefix re-run |

## Native facts worth not rediscovering

- `Game.SetCutsceneLock` (`06000CE6`) holds the Cutscene mode; a bare `StartMode` reverts within a frame.
- The UI End Turn is `TurnController.ForceToEnd(true)` (`06000C47`) admitted by `CanEndTurn` (`06000C4A`); `PauseBind` only
  speeds the game up. `End` → `InterruptCommands` → `UnitCommands.InterruptAll`.
- `UnitActionController.TickCommandTurnBased` → `ForceFinishForTurnBased` → `ForceFinish` marks a turn-based in-combat
  command `IsActed`/`Success` with no `OnAction` and no spend: flag bookkeeping, not a delivery.
- `UnitDescriptor.ResurrectAndFullRestore` (`06001F12`) clears `FinallyDead`, damage and conditions but **not** `LifeState`;
  `UnitLifeController.TickOnUnit` (`06009162`) recomputes it next tick.
- Turn-based cooldowns of **every** unit decay with game time while another unit acts. Any "unchanged debt" rule is wrong.
- The native `SaveManager` lists a new save under its own `SaveInfo`; re-query it by name rather than reusing the requested
  descriptor.
- The KMC ledger's admit-to-settle window is synchronous inside one call; observe `OwnsUnsettledRelationshipShell` instead.
- A mounted ranged full attack's plan ends with the mount's natural melee attack, so firing from range ends in a
  target-invalid interrupt. Both reach comparisons must carry `MountedCombatSpatialPolicy.RangeTolerance` (0.05 m), because
  the native approach stops the pair exactly at the envelope and it drifts millimetres before the terminal tick.
- The real-time foundation pair starts about 1.8 m apart, inside the 2.85 m Mount approach radius; a case that needs a real
  approach must move the rider out first.

## Where everything lives

- Status and history: `CURRENT.md`, `AGENTS.md`, `docs/CHUNK6-CONTINUATION.md`, `docs/CHUNK6A-COMBAT-MOUNT.md`.
- Action contract, one row per action with its cost owner and boundaries: `planning/CHUNK6-ACTION-CONTRACT.md`.
- Acceptance ledgers: `docs/chunk6a-ledger.json` (accepted preview.205, now stale),
  `docs/chunk6a-ledger-preview209.json` and `docs/chunk5-ledger-preview209.json` (the most complete validated state),
  `docs/chunk5-ledger.json` (accepted preview.105).
- Validators: `scripts/Test-Chunk6aLedger.ps1`, `scripts/Test-Chunk5Ledger.ps1`. Tiers: `scripts/Test-Tiers.ps1`.
- Readers (the sole acceptance authority for native evidence): `scripts/runtime/*Evidence.ps1`.
- Lab receipts, freezes, plans, closures and evaluations: `analysis-cache/chunk6-continuation/20261005-6br`.
- Runtime evidence, one directory per run: `runtime-evidence/<runId>`.

## What not to claim

Do not claim ENGINEERING COMPLETE for Chunk 6 until a ledger built on the released candidate passes
`Test-Chunk6aLedger.ps1 -Completion`, and do not claim owner acceptance or HUMAN PLAY acceptance at all — those are the
owner's to give. The release cut on 2026-10-11 was made on the owner's instruction with the remaining stages assumed to
pass; that assumption is recorded, not proven.
