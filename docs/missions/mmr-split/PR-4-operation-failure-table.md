# PR-4 Horse Trip repair: operation and failure table (round 11)

Owner instruction 2026-11-26f section 7 asked for this table before the round-9 implementation; owner instruction 2026-11-27a section 7
asks that it describe the mechanism actually implemented. For every file operation of the repair lifecycle it lists:
- **Authoritative bytes:** whose bytes decide.
- **File identity:** how the exact file is held.
- **Publication point:** when an archive becomes visible (readable).
- **Recovery state:** what a failure at each step leaves for the same session and for a fresh session.

Names are relative to the KMC user-data folder: `R` is the request file `horse-trip-repair-request.txt`, `C` the claim (`R.claimed`) and
`T` the terminal archive (`R.applied` / `.interrupted` / `.superseded`, or a time-stamped variant when the plain name exists).

Platforms:
- **W:** Windows, handle-bound kernel32 operations (production on Windows).
- **U:** Linux/macOS Mono, descriptor-bound libc operations (`UnixFiles` over `IPosixFiles`, production `LibcPosixFiles`). On this Windows
  machine U runs only over a simulated POSIX boundary (`SimulatedPosix` in the tests, through the `ForcePathnameOperations` seam); the
  Linux syscall semantics it relies on were observed in `decomposition/pr4/r10/linux-fs-probe.txt`. The C# libc calls themselves were not
  executed on Linux or Unity Mono.

| Step | Authoritative bytes | File identity | Publication point | Failure leaves | Same / fresh session recovery |
|---|---|---|---|---|---|
| 1. Read request (`TryReadRequest`) | the bytes of `R` read once | W: CreateFileW no-follow (FILE_FLAG_OPEN_REPARSE_POINT), sharing **read and delete only** (R9-F1: a handle another process holds with write access refuses the open), attribute check before reading (R8-F1). U: one `open(O_RDONLY\|O_NOFOLLOW\|O_NONBLOCK)`, regular-file type from `fstat` of that descriptor, then (Linux, round 11 R10-F2) a read lease (`F_SETLEASE F_RDLCK`, refused with EAGAIN while any other descriptor has the file open for writing; released at once), bytes read through it (R9-F2) | none | a link (W reparse point / U ELOOP), directory or other non-regular file: ineligible-entry refusal before any claim, selection, removal or copy, left in place. W: a request still being written: sharing violation; U Linux: the lease is refused; nothing claimed. U macOS has no lease (limitation) | the poll logs and retries; the completed file is decided once its writer has closed it |
| 2. Claim (`Claim`) | — | atomic rename `R` → `C` (the entry itself; a link is never followed) | none | `R` unchanged | ordinary retry |
| 3. Read claim (`TryReadClaim`) | the bytes of `C`; their SHA-256 is the claim identity | as step 1, on `C` | none | ineligible `C` refused before selection, removal or archival; `C` left in place. W: a writer holding `C` refuses the read | same |
| 4. Decide and remove | the claim identity | session record (Undecided → Decided) | none | a removal is verified before the decision is frozen | the session record, or Interrupted for an unowned claim |
| 5. Commit archive W | the verified claim bytes (`Close` re-reads `C` through step 3's eligible read and compares with the identity) | exclusive kernel32 handle on a private staged file; FileRenameInfo on that handle to `T`, ReplaceIfExists false | the handle-bound rename: the first bytes visible under `T` are the complete payload | staged file deleted through its own handle; `T` absent | the claim stays at `C`; the next poll commits again |
| 5. Commit archive U, Linux (R5-F1, R9-F3) | the verified claim bytes, held in memory (never re-read from a file) | an **unnamed** file of the folder (`open(dir, O_TMPFILE)`): no directory entry exists, so no other process can replace or edit it by name; written and `fsync`ed through its descriptor | `linkat(/proc/self/fd/<fd>, T, AT_SYMLINK_FOLLOW)`: one non-overwriting link of that open file; `T` appears with the complete bytes or not at all | nothing: the unnamed file ends with its descriptor; an existing entry at `T` (EEXIST, a directory included) is untouched | the claim stays at `C`; the retry commits once (a time-stamped name when `T` is taken by a file); repeated obstructed polls leave no file |
| 5. Commit archive U, protected create (macOS; no O_TMPFILE; no /proc; a failed descriptor link) | the same in-memory payload | ONE owned attempt kept in the session across polls (round 11, R9-F3/R10-F1): `T` created once with `O_RDWR\|O_CREAT\|O_EXCL\|O_NOFOLLOW` and permission bits 0; `fstat` of the descriptor must report permission bits 0 (R5-F1: protection in effect on this filesystem) before any payload byte is written; each attempt truncates and rewrites through the same descriptor, `fsync`s and reads the bytes back | `fchmod(fd, 0600)` (owner-only, R10-F5): the first readable bytes are the complete payload; then `lstat(T)` must be the descriptor's file (device, inode) with the payload's size | nothing is unlinked by name (R10-F1). A write, flush, `fstat` or `lstat` failure keeps the attempt (its descriptor and name) for the next poll; only an observed different file at `T` releases it (the replacement is left in place). A filesystem that does not apply permission bits gets an empty, never partial, file and a kept claim | the retry continues the same attempt (no new names); after an observed replacement a new attempt uses a time-stamped name; a crash can leave at most the one unreadable file
| 6. Retire claim W | the claim identity | exclusive no-follow handle on `C`; bytes verified through it; FileDispositionInfo on that handle | — | a different file (a replacement) is left untouched | the replacement gets its own Interrupted result and archive |
| 6. Retire claim U (R4-F3) | the claim identity | `C` renamed atomically to `C.<id8>.retiring`; verified there through step 1's eligible read; renamed to `C.<id8>.<identity16>.settled`; verified again | — | a pending entry is a `.retiring` entry of that exact shape, or a `.settled` entry of that exact shape whose bytes differ from the identity in its name; a completed retirement is `.settled` whose bytes match its name | `RecoverRetiring` (every claim read, so the same and a fresh session) restores the oldest pending entry to `C` when `C` is free. The replacement is then decided Interrupted with its own byte-exact archive and named result before any newer request, and the original request's removal is not repeated |
| 7. Recovery ownership (R9-F4, R10-F3, R10-F4) | an entry's WHOLE name (`\A...\z`), then its bytes through the eligible read | the shapes the retirement writes are candidates: `C.<8 hex>.retiring`, the round-8 `C.<32 hex>.retiring`, and `C.<8 hex>.<identity16>.settled` with bytes not matching its identity; after the move the claim taken is checked again and returned to its own name if it is no longer pending (a completed record or an ineligible entry) | — | every other name (the round-8 completed record `C.<32 hex>.settled`, backups, a name with data after the suffix, anything unrecognized) and any link or non-regular entry is left exactly as it is | nothing unrecognized is moved, archived or decided |

## Platform limits

- These guarantees cover the local filesystem with ordinary, non-hostile processes (not root, which bypasses permission bits). The
  instruction does not ask for guarantees against hostile processes, network filesystems or power loss.
- U supports the x86_64 Linux and macOS builds Kingmaker ships (struct stat layouts in `LibcPosixFiles`); another architecture refuses the
  operation (the request stays) instead of guessing a layout.
- Evidence: W ran natively on Windows; U ran on Windows over the simulated POSIX boundary; the Linux syscall semantics (O_TMPFILE, linkat of
  the descriptor, EEXIST, O_NOFOLLOW/ELOOP, permission-0 denial to a same-uid process, struct stat offsets) were observed with Perl on
  Debian 13 under the docker-desktop WSL2 kernel 6.18. No Unity Mono and no macOS run.
