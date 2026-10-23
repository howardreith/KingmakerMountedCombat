# Chunk 7 batch191: request-free KMC ordinary controls under an external observer

Status: **development PASS** for one declared native scenario (attempt s2), after one
preserved FAIL (attempt s1). This is request-free KMC ordinary controls under an external
observer, on the identified frozen batch190 KMC candidate. It is not an uninstrumented
session, not isolated KMC Update timing, not performance acceptance, and not human
physical-input or visual approval. The standing completion mission remains **IN PROGRESS**.

## What was established

KMC was staged from its package and launched with **no automation request**. The process
carried only the observer's own `-kmcObserver*` arguments. The isolated base-game tree held
exactly the pinned game, loader and Harmony, plus two packages: KMC and the external
observer (`base-game-only-kmc-observer`). On a disposable copy of the cold-qualified
unmounted six-Horse Manual308, the observer drove the player's own native inputs in order:

| Step | Native entrypoint | Observed |
| --- | --- | --- |
| Mount | Rider's own Mount ability via `SelectedAbilityHandler`, on its own Horse | Command `Success`; rider attached to `30b79228…` (its native pet); neighbor pair untouched |
| Mounted move | One ground click with the rider selected | Rider 4.39 m and Horse 4.03 m together, still attached, settled in 116 frames; neighbor untouched |
| Dismount | Rider's Dismount ability | Command `Success`; rider detached; neighbor untouched |
| Unmounted move | One ground click | Rider 4.20 m under ordinary authority, Horse 0.00 m, settled in 134 frames; neighbor untouched |

Read-only checks at the menu, after load and at the end all confirmed the following:

- UMM ran exactly two active entries, KMC and the observer.
- The loaded KMC assembly was the candidate (`34e8ed39…`, MVID `57fc82b9…`), and the
  running observer was its declared package.
- KMC's Harmony owners and root were active.
- KMC's automation host field, static automation host, measurement session and movement
  telemetry were all null.
- There were no diagnostic overlay objects.

The effective shipped settings read from the running product were:

- overlay off;
- Mounted Charge off;
- paired activation on;
- paired command scheduler off;
- legacy unified turn off;
- movement experiment on.

KMC's own one-session UMM log, read after exit, contains:

- its ordinary load line for `0.1.0-chunk7b-preview.94`;
- `diagnostic overlay=False`;
- no `Runtime automation request accepted` line;
- no error or exception line (51 KMC lines in total).

**Filesystem.** No save file was added or removed. Member by member, the owned fixture copy
differs from its qualified source only in `header.json`, which is the engine's own header
rewrite on load. Native saves and Areas were routed into the owned profile before the first
load.

## Identities

| Item | Identity |
| --- | --- |
| KMC (frozen, reused) | batch190 package `ee84e933…`, manifest `d10bbb63…`, source `62a68e64`, via `FrozenSourceRoot` and freeze record `b191-kmc-b190-freeze.json` (`4b079cab…`) |
| Observer | `KmcRemovalObserver-0.1.0-observer.5-c7b-world191-normalplayer-a3`, package `7e7ba626…`, manifest `095d3017…`, built at harness `454177a6` |
| Harness | `454177a6` (reviewed change `ac1d4e78` + `108289ca`, then `b25030ab`, `2b0e1e77`, `454177a6`) |
| Suite | `chunk7b-completion-b190-sixmeasure-20261022` (`b0207210…`) |
| Fixture | `Manual_308_KMC_C7_SIX_HORSE_WORLD.zks` `b1e817a6…`, through the batch185 source gate |

## Attempt s1 — preserved FAIL (`20261023-c7b-world191-b191s1-normalplayer`)

All control steps passed (41 checks), but two checks failed:

- the observer's whole-archive "unchanged" rule;
- its save-listing rule.

Both were tripped by the engine's native header rewrite, which raises the length by one
byte; the only changed member is `header.json`.

Separately, the profile restoration refused the UMM `Params.xml` rewrite. The owner's
settled parameters already contained an observer entry, and UMM wrote the found pair in its
own load order (KMC, then the observer). The co-staged delta had assumed the kept entry
stays first. Mods, saves, Baseline and Working were restored by the run. The guarded
`Recover-KmcPersistenceProfile.ps1` was then run: WhatIf PASS, then PASS. It restored
`Params.xml` byte-identically (`343997c8…`) and released the lock. Both rules were corrected
(`2b0e1e77`, `454177a6`) with regression checks. The s1 evidence is kept as recorded.

## Attempt s2 — PASS (`20261023-c7b-world191-b191s2-normalplayer`)

Results:

- **Observer:** PASS 42/0.
- **Outer:** PASS, with Mods restored, save protection, Baseline immutable, Working restored
  and the write allowlist.
- **Profile:** bytes restored exactly, zero cache changes.
- **Params and lock:** `Params.xml` equals the original; no lock or game remains.

Receipts:

| File | SHA-256 |
| --- | --- |
| Runtime result | `365771aa…` |
| Game record | `54d80369…` |
| Observer result | `0bcfd5cd…` |
| UMM-log summary | `120c17dd…` |

## Observations to carry forward

- **Hotbar.** In this fixture the rider owned the native Mount fact before mounting and the
  Dismount fact while mounted, but neither was on the hotbar before or after.
  - KMC registers its abilities with action-bar autofill disabled.
  - So an ordinary player reaches them through the native ability list and places them;
    this is a usability item for the owner-play checklist.
  - Earlier runs bound hotbar slots by explicit native slot assignment; that is not proof
    that the UI pre-fills them.
- **Game log.**
  - The game's own log had one unattributed `NullReferenceException` and two native
    asset-bundle duplicate-load errors during load.
  - None was attributed to KMC, and KMC's UMM log was clean.
  - They are recorded, not explained.
- **Whole-frame idle windows (240 frames, external observer present).**

  | Window | Mean | P50 | P95 | Max |
  | --- | --- | --- | --- | --- |
  | Unmounted | 11.67 ms | 11.01 ms | 11.73 ms | 168.8 ms (load-settling spike) |
  | Mounted | 11.15 ms | 11.01 ms | 11.64 ms | 36.6 ms |

  These are frame times including the game, rendering and the observer, not KMC Update
  timing.
- **Pause.** The observer unpaused the loaded game once, as a player would.

## Not claimed

None of these is established here:

- final-package performance acceptance;
- an uninstrumented session;
- the complete loaded-assembly inventory (the observer proves the two UMM entries and both
  assembly identities, and loader files are verified on disk);
- human input feel or visuals.
