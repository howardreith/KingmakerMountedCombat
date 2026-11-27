# PR-4 Horse Trip repair: operation and failure table (round 9)

Owner instruction 2026-11-26f section 7 asks for this table before the round-9 implementation. It lists, for every file operation of
the repair lifecycle:
- **Authoritative bytes:** whose bytes decide.
- **File identity:** how the exact file is held.
- **Publication point:** when an archive becomes visible.
- **Recovery state:** what a failure at each step leaves for the same session and for a fresh session.

Names are relative to the KMC user-data folder: `R` is the request file `horse-trip-repair-request.txt`, `C` the claim (`R.claimed`) and
`T` the terminal archive (`R.applied` / `.interrupted` / `.superseded`, or a time-stamped variant when the plain name exists).

Platforms:
- **W:** Windows, handle-bound kernel32 operations.
- **P:** the pathname branch used on Linux/macOS Mono. On Windows it runs only through the test seam `ForcePathnameOperations`, so those
  runs are labelled "Windows fallback execution".

| Step | Authoritative bytes | File identity | Publication point | Failure leaves | Same / fresh session recovery |
|---|---|---|---|---|---|
| 1. Read request (`TryReadRequest`) | the bytes of `R` read once | W: CreateFileW no-follow, FILE_FLAG_OPEN_REPARSE_POINT, attribute check before reading (R8-F1); P: lstat-style attribute check, then read | none | a link, reparse point or directory at `R`: ineligible-entry refusal before any claim, selection, removal or copy; `R` left in place | the poll logs once and retries; nothing changes until an ordinary file replaces it |
| 2. Claim (`Claim`) | — | atomic rename `R` → `C` (the entry itself; a link is never followed) | none | `R` unchanged | ordinary retry |
| 3. Read claim (`TryReadClaim`) | the bytes of `C`; their SHA-256 is the claim identity | as step 1, on `C` | none | ineligible `C` refused before selection, removal or archival; `C` left in place | same |
| 4. Decide and remove | the claim identity | session record (Undecided → Decided) | none | a removal is verified before the decision is frozen | the session record, or Interrupted for an unowned claim |
| 5. Commit archive W | the verified claim bytes (`Close` re-reads `C` through step 3's eligible read and compares with the identity) | exclusive kernel32 handle on a private staged file; FileRenameInfo on that handle to `T`, ReplaceIfExists false | the handle-bound rename: the first bytes visible under `T` are the complete payload | staged file deleted through its own handle; `T` absent | the claim stays at `C`; the next poll commits again |
| 5. Commit archive P (R5-F1) | the verified claim bytes | private `C.<id>.pending` created exclusively and written and flushed through its handle, kept open; published by creating a hard link `T` → that file: Linux `linkat` of `/proc/self/fd/<fd>` (the open file itself), otherwise `link(2)`; Windows fallback: CreateHardLinkW. Hard-link creation never replaces an existing `T` | the link: `T` appears complete or not at all | `T` absent; the private pending file is the attempt's own and is left in place (never another name) | the claim stays at `C`; the retry commits once; an entry that appeared under `T` is preserved and the retry uses a time-stamped name |
| 6. Retire claim W | the claim identity | exclusive no-follow handle on `C`; bytes verified through it; FileDispositionInfo on that handle | — | a different file (a replacement) is left untouched | the replacement gets its own Interrupted result and archive |
| 6. Retire claim P (R4-F3) | the claim identity | `C` renamed atomically to `C.<id>.retiring` (pending verification); verified there; renamed to `C.<id>.<identity>.settled`; verified again under that name | — | a pending entry is either `.retiring`, or `.settled` whose bytes differ from the identity in its name; a completed retirement is `.settled` whose bytes match its name | `RecoverRetiring` (every claim read, so the same and a fresh session) restores the oldest pending entry to `C` when `C` is free. The replacement is then decided Interrupted with its own byte-exact archive and named result before any newer request, and the original request's removal is not repeated |

## Platform limits

- These guarantees cover the local filesystem with ordinary, non-hostile processes. The instruction does not ask for guarantees against
  hostile processes, network filesystems or power loss.
- The pathname branch's Linux `linkat` and libc `link` calls are compiled and wired, but this machine cannot execute them: it has no
  Linux or Unity-Mono runtime. They are reported as not executed natively.
