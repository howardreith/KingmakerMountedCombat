# PR-4 amendment test mapping (round 12)

OWNER AMENDMENT revision 1 (owner instruction 2026-11-27b section 4; report citation: prepared under owner amendments revision 1 sha256
`f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28`) withdraws the requirement to start the existing-horse repair through a
filesystem request and to implement a persistent claim/archive/publication/recovery protocol for it. It replaces that interface with a
confirmed in-game maintenance action. The request ingestion, polling, claim, archive and recovery code and its Windows/POSIX file and lease
machinery are removed from the product (`Test-MmrHorseTripNativeContract.ps1` checks that they are absent).

Each test of the earlier component suite (`tests/HorseTripRepairPolicyTests.cs` at `68a19050`) is listed below with its disposition.
- **Retired (file protocol):** it exercised behaviour of the withdrawn file protocol only. This does not say the old implementation was
  correct or that its failing review scenarios passed; the findings stay in the review history.
- **Kept:** unchanged.
- **Successor:** the underlying requirement (exact removal, classification, idempotence, gates, protected neighbours) is now established
  by the named round-13 test on the confirmed-action path (HorseTripRepairUpdater.Run).

| Earlier test | Disposition | Round-13 successor |
|---|---|---|
| `CandidatePredicate`: MMR-06: a candidate needs the exact Horse, the stock identity and shape, and no provenance | Kept | `CandidatePredicate` |
| `RequestParsing`: R4-F4: the whole request is validated; two horses or extra content select nothing | Retired (file protocol); requirement kept | `SourcedModifiedAmbiguous (more than one candidate is never offered)` |
| `ExactRequestApplies`: MMR-06/R4-F6: the exact request removes one fact, is claimed and closed in user data, Mods tree untouched | Retired (file protocol); requirement kept | `TwoHorsesAndCancellation / NeighboursStay` |
| `NegativeCases`: MMR-06: legacy-ordered and independently granted facts stay without a request; refusals change nothing | Retired (file protocol); requirement kept | `SourcedModifiedAmbiguous / NeighboursStay` |
| `PendingAndRetry`: MMR-06: a closed gate, an unloaded horse and a failed removal keep the request for a retry | Retired (file protocol); requirement kept | `GatesRefuse` |
| `ReportingFailures`: R4-F5: failures after removal, at the claim, the result write and the final rename never record a refusal | Retired (file protocol) | (none: withdrawn by the amendment) |
| `StrandedClaims`: R4-F5: a stranded claim finishes as applied without a second removal, also after a reload; a restart closes it unchanged | Retired (file protocol) | (none: withdrawn by the amendment) |
| `ReplacedBeforeClaim`: R5-F1: a request replaced between observation and claim is judged by the claimed bytes (apply and refusal) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `WrittenAfterClaim`: R5-F1: a request written after the claim is a separate later request; a waiting claim yields to it unchanged | Retired (file protocol) | (none: withdrawn by the amendment) |
| `TransientFailureAfterClaim`: R6-F4: a one-time read or lookup failure after the claim keeps this process's exact claim; the next poll applies it once | Retired (file protocol) | (none: withdrawn by the amendment) |
| `TerminalDecisionsStayTerminal`: R7-F5: a refusal, supersession or application frozen before reporting stays terminal through result-write and archive failures and candidate changes | Retired (file protocol) | (none: withdrawn by the amendment) |
| `RepairFolderSelection`: PR-4 R1-F1: a guarded run without an isolated root leaves the player's repair files byte-identical; isolated and ordinary runs use only their own folder | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UncertainRemovalIsTerminal`: PR-4 R1-F2: an unverifiable or inexact removal is terminal (one removal in total, truthful result); an exact removal that then threw is applied | Retired (file protocol); requirement kept | `RemovalOutcomes (target and a neighbour removed; post-removal read fails)` |
| `ClaimIdentityIsBytes`: PR-4 R1-F4: the claim is bound to its raw bytes; a transient observation failure keeps the frozen outcome | Retired (file protocol) | (none: withdrawn by the amendment) |
| `ReturnRenameRace`: PR-4 R1-F5: a newer request winning the return rename supersedes the waiting claim; the waiting horse is untouched | Retired (file protocol) | (none: withdrawn by the amendment) |
| `NativeBoundaryRemoval`: PR-4 R1-F2: a native removal that takes a neighbour and throws before any check is terminal (no Applied, no second removal after reload); an exact removal that throws is applied | Retired (file protocol); requirement kept | `RemovalOutcomes (throws after the exact removal; target and a neighbour removed)` |
| `UnchangedRemovalRetries`: PR-4 R2-F1: a native removal that returns with nothing changed keeps the request; the retry removes once; a newer request supersedes it unchanged | Retired (file protocol); requirement kept | `RemovalOutcomes (throws before mutating: verified unchanged, no repeat)` |
| `ReplacedDuringPublication`: PR-4 R1-F4: a claim replaced during publication never receives the decided suffix; each result names its own horse; one removal | Retired (file protocol) | (none: withdrawn by the amendment) |
| `DiagnosticFailureAfterRemoval`: PR-4 R3-F1: a failing removal diagnostic after an exact removal keeps the applied decision and archive; a reloaded fact is preserved | Retired (file protocol); requirement kept | `ReportingFailureDoesNotRepeat` |
| `FinalPublicationFailureResumes`: PR-4 R3-F2: a failed final archive write is resumed before a newer request; a fresh session settles it as interrupted | Retired (file protocol) | (none: withdrawn by the amendment) |
| `ChangedAfterVerification`: PR-4 R1-F4: bytes changed after verification never enter the decision's archive and keep their own disposition | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UnreadableNativeMessage`: PR-4 R3-F1: an exact removal whose native exception message is unreadable is applied once; a reloaded fact is preserved | Retired (file protocol); requirement kept | `RemovalOutcomes (native message getter throws)` |
| `FailedCommitLeavesNoPartialArchive`: PR-4 R4-F1: an obstructed final archive rename (after a complete write and flush) leaves no partial terminal file; the retry commits one complete archive | Retired (file protocol) | (none: withdrawn by the amendment) |
| `WriteOrFlushFailureLeavesNoPartialArchive`: PR-4 R4-F1 (round 6): a failure during the actual payload write or flush leaves no partial terminal archive; the retry commits one complete archive, one removal | Retired (file protocol) | (none: withdrawn by the amendment) |
| `PendingArchiveChangeIsExcludedOrRefused`: PR-4 R5-F1 (round 6): a change to the staged archive at publication is excluded or refused; the final archive holds exactly A, one removal | Retired (file protocol) | (none: withdrawn by the amendment) |
| `ReplacementAtRetirementTakesItsOwnLifecycle`: PR-4 R4-F3/R5-F2 (round 6): a replacement at retirement keeps its own Interrupted result and archive, survives its failed publication and settles before C; A removed once | Retired (file protocol) | (none: withdrawn by the amendment) |
| `LockedRecoveredClaimArchivesOnce`: PR-4 R4-F2 (round 6): a locked recovered claim in a fresh session produces one interruption archive, then completes; the waiting request then runs | Retired (file protocol) | (none: withdrawn by the amendment) |
| `ReplacementAfterFailedRetirementHasItsOwnRecord`: PR-4 R6-F1 (round 7): a replacement after A's committed archive and failed retirement keeps its own archive record and result; C then runs | Retired (file protocol) | (none: withdrawn by the amendment) |
| `VerifiedFileOperationsBindTheFinalOperation`: PR-4 R5-F1/R4-F3 (round 7): publication and retirement act on the verified file's own handle; late rename-replacements are excluded (Windows) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `RestartReportingIsTruthful`: PR-4 R6-F2 (round 7): fresh-session recovery after an applied removal reports truthfully, with exact retry guidance, and removes nothing more | Retired (file protocol) | (none: withdrawn by the amendment) |
| `LinkedClaimRetirementLeavesItsTarget`: PR-4 R7-F1 (round 8): retiring a linked claim leaves its sibling target intact; ordinary retirement still works (Windows) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `PathnameRetirementNeverConsumesAReplacement`: PR-4 R4-F3 (round 8): the pathname branch never deletes; a replacement at its final retirement operation returns and settles before C | Retired (file protocol) | (none: withdrawn by the amendment) |
| `PathnamePublicationIsComplete`: PR-4 R5-F1 (round 9): the pathname branch publishes the complete payload by one non-overwriting link; a partial-write fault leaves no terminal archive and a foreign entry untouched (simulated POSIX boundary since round 10) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `PathnameRetirementRecoversAfterSettlingFailures`: PR-4 R4-F3 (round 9): a failure at the settling check or move back leaves the replacement recoverable in the same and a fresh session (simulated POSIX boundary since round 10) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `LinkedEntriesAreRefusedBeforeActing`: PR-4 R8-F1 (round 9): linked requests and claims are refused before selection, removal or copying (actual junctions and a simulated symbolic-link boundary); ordinary and locked-regular controls | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UnixPublicationIsProtected`: PR-4 R5-F1/R9-F3 (round 10): Unix publication has no staging name: unnamed-file link, and the protected exclusive create after a failed or unavailable descriptor link; first readable bytes are A's; in-place change and name replacement are excluded or refused (simulated POSIX boundary) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UnixStagingStaysBounded`: PR-4 R9-F3 (round 10): an obstructed terminal across repeated polls leaves no staging file under either Unix mechanism; then one archive, one removal, no alias, the foreign file preserved (simulated POSIX boundary) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `WindowsWriterIsExcluded`: PR-4 R9-F1 (round 10): a request still held by its writer is never decided on Windows (zero removals); the completed two-horse request is refused as malformed; a read-only holder stays compatible | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UnixReadsBindTheDescriptor`: PR-4 R9-F2 (round 10): a link substituted at the open boundary of an undecided claim or a recovery entry supplies no removal or archive; ordinary-file controls (simulated POSIX boundary) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `RecoveryIsConservative`: PR-4 R9-F4 (round 10): a fresh session preserves an older completed record, its Applied result and archive and a similarly named backup; no Interrupted archive (Windows and simulated POSIX) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UnixFallbackAttemptIsOwned`: PR-4 R10-F1/R9-F3/R10-F5 (round 11): the Unix fallback is one owned attempt: a write fault, a replacement after the fault and metadata failures never unlink by name or name a second file, publish A once with owner-only access (simulated POSIX boundary) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UnixFallbackNeedsEffectiveProtection`: PR-4 R5-F1 (round 11): where the filesystem does not apply the requested permission bits the fallback writes nothing and keeps the claim, bounded across polls (simulated POSIX boundary) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `UnixWriterIsExcluded`: PR-4 R10-F2 (round 11): a Unix request still held by its writer is never decided (zero removals); completed, it is refused as malformed; a completed single-horse request applies (simulated POSIX boundary) | Retired (file protocol) | (none: withdrawn by the amendment) |
| `RecoveryNamesAndMovesAreExact`: PR-4 R9-F4/R10-F3/R10-F4 (round 11): recovery names must match whole; the round-8 retiring shape is recovered before a queued request; an entry that became a completed record before the move is returned unrecovered | Retired (file protocol) | (none: withdrawn by the amendment) |
| `BlockedRetirementCommitsOnce`: PR-4 R4-F2: a blocked claim retirement commits the archive once; the waiting request then proceeds | Retired (file protocol) | (none: withdrawn by the amendment) |
| `ReplacementDuringRetirement`: PR-4 R3-F2/R4-F3: replacement bytes during retirement keep their own disposition before newer work; A removed once | Retired (file protocol) | (none: withdrawn by the amendment) |

Round-13 tests (all through HorseTripRepairUpdater.Run): `DiscoveryIsReadOnly`, `TwoHorsesAndCancellation`, `RepeatedClicksAndUpdates`, `ReloadInvalidatesConsent`,
`DisableAndFailureStop`, `PartyUnavailable`, `StableIdAndRename`, `RefreshCannotEraseConsent`, `GatesRefuse`, `SourcedModifiedAmbiguous`, `RemovalOutcomes`,
`ReportingFailureDoesNotRepeat`, `OldRequestFilesAreInert` and `NeighboursStay`. The verified
new-horse filter, the candidate predicate and the stock data contract stay in `Test-MmrHorseTripNativeContract.ps1`.
