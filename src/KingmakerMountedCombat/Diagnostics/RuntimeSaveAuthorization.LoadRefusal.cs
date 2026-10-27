using System;
using System.IO;
using System.Threading;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimeSaveAuthorization
    {
        private OwnerSeedLoadRefusalProbeLease loadRefusalProbe;
        internal bool HasArmedOwnerSeedLoadRefusal { get { lock (sync) return loadRefusalProbe?.Armed == true; } }

        internal OwnerSeedLoadRefusalProbeLease BeginOwnerSeedLoadRefusalProbe(RuntimeRequest request, PersistenceSaveAuthorization authority)
        {
            lock (sync)
            {
                if (loadRefusalProbe != null || request?.OwnedPairs?.IsOwnerSeedLoadRefusal != true || request.Validate().Count != 0 ||
                    activeScope == null || !IsExactPersistenceScope(authority) || !authority.MatchesOwnerSeedLoadRefusalRequest(request) ||
                    authority.MaximumNativeLoadRequests != 1 || authority.MaximumNativeWriteRequests != 0 ||
                    authorizedLoadCount != 1 || authorizedWriteCount != 0 || unauthorizedLoadCount != 0 ||
                    unauthorizedWriteCount != 0 || fatalViolationCount != 0)
                    throw new InvalidOperationException("Load-refusal observation requires its exact exhausted one-load, zero-write diagnostic scope.");
                loadRefusalProbe = new OwnerSeedLoadRefusalProbeLease(this, request, authority, generation);
                return loadRefusalProbe;
            }
        }

        // Called only AFTER the real mounted metadata policy. An unexpected
        // acceptance is a fatal experiment failure, blocked before game disposal.
        // This never substitutes a diagnostic rejection for the policy's result.
        internal bool FenceOwnerSeedLoadRefusalDecision(bool accepted, RuntimeSaveTarget target, string root, string nativeEntry)
        {
            lock (sync)
            {
                if (loadRefusalProbe?.Armed != true) return accepted;
                return loadRefusalProbe.ObserveDecision(accepted, target, root, nativeEntry);
            }
        }

        internal sealed class OwnerSeedLoadRefusalProbeLease : IDisposable
        {
            private static readonly string[] Entries = { "Game.LoadGameFromMainMenu", "Game.LoadGame", "Game.LoadGameForSmokeTest" };
            private RuntimeSaveAuthorization owner;
            private readonly RuntimeRequest request;
            private readonly Chunk7OwnedPairsSelection selection;
            private readonly RuntimeSaveDescriptor primary, alternate;
            private readonly PersistenceSaveAuthorization authority;
            private readonly int authorityGeneration, thread;
            private readonly string token, derivation, root, targetKey;
            private string armedEntry;
            private bool earlyObserved, decisionObserved;
            internal bool Armed => armedEntry != null;
            internal int EarlyIdentityCount { get; private set; }
            internal int RejectedDecisionCount { get; private set; }
            internal int UnexpectedAcceptedCount { get; private set; }
            internal int IncompleteEntryCount { get; private set; }
            internal bool Released => owner == null;

            internal OwnerSeedLoadRefusalProbeLease(RuntimeSaveAuthorization owner, RuntimeRequest request,
                PersistenceSaveAuthorization authority, int generation)
            {
                this.owner=owner;this.request=request;this.authority=authority;authorityGeneration=generation;
                selection=request.OwnedPairs;primary=request.PersistenceLoad;alternate=request.PersistenceAlternate;
                token=request.TransactionToken;derivation=request.LoadRefusalDerivationSha256;root=authority.Root;
                targetKey=DescriptorKey(alternate);thread=Thread.CurrentThread.ManagedThreadId;
            }
            private bool Current() => owner != null && ReferenceEquals(owner.loadRefusalProbe,this) &&
                owner.activeScope != null && owner.generation == authorityGeneration && Thread.CurrentThread.ManagedThreadId == thread &&
                owner.IsExactPersistenceScope(authority) && ReferenceEquals(request.OwnedPairs,selection) &&
                ReferenceEquals(request.PersistenceLoad,primary) && ReferenceEquals(request.PersistenceAlternate,alternate) &&
                request.TransactionToken == token && request.LoadRefusalDerivationSha256 == derivation &&
                authority.Root == root && DescriptorKey(alternate) == targetKey && authority.MatchesOwnerSeedLoadRefusalRequest(request) &&
                authority.AdmittedNativeLoadRequests == 1 && authority.AdmittedNativeWriteRequests == 0 &&
                owner.authorizedLoadCount == 1 && owner.authorizedWriteCount == 0 && owner.unauthorizedLoadCount == 0 &&
                owner.unauthorizedWriteCount == 0 && owner.fatalViolationCount == 0;
            internal IDisposable Arm(string entry)
            {
                if (!Current() || Armed || RejectedDecisionCount >= Entries.Length || IncompleteEntryCount != 0 ||
                    entry != Entries[RejectedDecisionCount])
                    throw new InvalidOperationException("Load-refusal entry is foreign, stale, repeated or out of order.");
                armedEntry=entry;earlyObserved=decisionObserved=false;
                return new EntryLease(this,entry);
            }
            internal string ValidateEarly(RuntimeSaveTarget target,string observedRoot,string entry)
            {
                if (!Current() || !Armed || earlyObserved || entry != armedEntry || !ExactTarget(target,observedRoot))
                    return "Load-refusal early identity observation differs from its exact armed alternate/request.";
                var rejection=authority.ValidateArchiveIdentity(target,observedRoot);
                if (rejection != null) return rejection;
                rejection=authority.ValidateArchiveIdentity(new RuntimeSaveTarget { FileName=primary.FileName,InternalName=primary.InternalName,
                    FullPath=Path.Combine(root,primary.FileName),SaveType="Manual",GameId=primary.GameId,GameName=primary.GameName,Area=primary.Area },observedRoot);
                if (rejection != null) return rejection;
                if (!ExactFileFacts(primary,root) || !ExactFileFacts(alternate,root))
                    return "Load-refusal immutable source/alternate length or timestamp changed.";
                earlyObserved=true;EarlyIdentityCount++;
                return null;
            }
            internal bool ObserveDecision(bool accepted,RuntimeSaveTarget target,string observedRoot,string entry)
            {
                if (!Current() || !Armed || !earlyObserved || decisionObserved || entry != armedEntry || !ExactTarget(target,observedRoot))
                {
                    owner?.ReportFatalViolation(RuntimeSaveOperation.Load,"Load-refusal decision lost its exact early identity observation.");
                    return false;
                }
                decisionObserved=true;
                if (accepted)
                {
                    UnexpectedAcceptedCount++;
                    owner.ReportFatalViolation(RuntimeSaveOperation.Load,"Real mounted metadata policy unexpectedly accepted the selected refusal alternate; world replacement remains blocked.");
                    return false;
                }
                RejectedDecisionCount++;
                return false;
            }
            private bool ExactTarget(RuntimeSaveTarget target,string observedRoot) => target != null &&
                NormalizeRoot(observedRoot) == root && NormalizeFilePath(target.FullPath) == Path.Combine(root,alternate.FileName) &&
                target.FileName == alternate.FileName && target.InternalName == alternate.InternalName && target.SaveType == "Manual" &&
                target.GameId == alternate.GameId && target.GameName == alternate.GameName && target.Area == alternate.Area;
            private static string DescriptorKey(RuntimeSaveDescriptor value) => value == null ? null : string.Join("\n",value.FileName,
                value.InternalName,value.GameId,value.GameName,value.Area,value.Sha256,value.Length.ToString(),value.LastWriteTimeUtcTicks.ToString());
            private static bool ExactFileFacts(RuntimeSaveDescriptor value,string root)
            {
                var file=new FileInfo(Path.Combine(root,value.FileName));
                return file.Exists && (file.Attributes & FileAttributes.ReparsePoint)==0 && file.Length==value.Length &&
                    file.LastWriteTimeUtc.Ticks==value.LastWriteTimeUtcTicks;
            }
            private void EndEntry(string entry)
            {
                if (armedEntry != entry) return;
                if (!earlyObserved || !decisionObserved) IncompleteEntryCount++;
                armedEntry=null;
            }
            public void Dispose()
            {
                var current=owner;if(current==null)return;
                lock(current.sync){if(Armed)EndEntry(armedEntry);if(ReferenceEquals(current.loadRefusalProbe,this))current.loadRefusalProbe=null;owner=null;}
            }
            private sealed class EntryLease : IDisposable
            {
                private OwnerSeedLoadRefusalProbeLease owner;private readonly string entry;
                internal EntryLease(OwnerSeedLoadRefusalProbeLease owner,string entry){this.owner=owner;this.entry=entry;}
                public void Dispose(){var current=owner;owner=null;current?.EndEntry(entry);}
            }
        }
    }
}
