using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace KingmakerMountedCombat.Diagnostics
{
    internal interface IChunk7AssistedStoryBoundary
    {
        Chunk7AssistedStorySnapshot Read();
        // Implementations re-read the same native fields/list objects and current
        // frame immediately before callback entry; no deferred callback is valid.
        void Click(Chunk7AssistedStorySnapshot expected, int offerIndex, int rowIndex, Action enteringNative);
        // Advances the sole DialogRoot system answer through the native DialogueSystemButton
        // after re-reading the live state; used only when no TempOptionUI row is eligible.
        void ClickSystemButton(Chunk7AssistedStorySnapshot expected, Action enteringNative);
    }
    internal enum Chunk7AssistedStoryStatus { Waiting, Clicked, ReadyAtObservedFrame, Failed, Disposed }
    internal sealed class Chunk7AssistedStorySnapshot
    {
        internal Chunk7AssistedIdentity Identity;
        internal long ElapsedMilliseconds;
        internal int Frame;
        internal string Mode, UiStatus, DialogGuid, CueGuid;
        internal bool MouseController, UnexpectedPopup, NativeBusy, Cutscene, TransitionPending, DialogActive, MainViewReady;
        // Read-only native sources of TransitionPending, retained at timeout
        // so a stalled authored-campaign transition has a distinguishing cause.
        internal bool ScheduledStart, CuePlayScheduled, DialogStopScheduled;
        internal object Controller, Dialog, Cue, Ui, Common, Manager, PcDialog, CurrentPart, OffersList, RowsList;
        // SystemButton/ContinueCue describe the COMMON continue/exit affordance observed from the PC dialog.
        internal object SystemButton, SystemButtonAnswer, ContinueCue;
        internal int? ManagerKind;
        internal bool? ManagerShown, PcActive, SystemButtonEnabled;
        internal Chunk7AssistedStoryOffer[] Offers;
        internal Chunk7AssistedStoryRow[] Rows;
    }
    internal sealed class Chunk7AssistedStoryOffer
    { internal object Answer,Selection; internal string Guid; internal int? SelectionType; internal bool IsSystemAnswer,IsExitAnswer; internal int OnSelectActionTypeCount; }
    internal sealed class Chunk7AssistedStoryRow
    {
        internal object View, Answer; internal string Guid; internal int NativeIndex;
        internal bool Initialized, Disabled, ChoosingCharacter, Interchapter, ActiveAndEnabled, ActiveInHierarchy;
    }
    internal sealed class Chunk7AssistedStoryEvidence
    {
        internal int Frame; internal long ElapsedMilliseconds; internal string Mode,UiStatus,DialogGuid,CueGuid;
        internal bool NativeBusy,TransitionPending,DialogActive,UnexpectedPopup,MouseController,Cutscene,MainViewReady;
        internal int? ManagerKind;internal bool? ManagerShown,PcActive;
        internal int Controller,Dialog,Cue,Ui,Common,Manager,PcDialog,Part,OffersList,RowsList;
        internal Chunk7AssistedStoryIdentityEvidence Identity;
        internal int[] OfferObjects,OfferSelectionObjects,RowObjects,RowAnswerObjects,RowNativeIndices;
        internal string[] OfferGuids;
        internal int?[] OfferSelectionTypes;
        internal bool[] RowInitialized,RowDisabled,RowChoosingCharacter,RowInterchapter,RowActiveAndEnabled,RowActiveInHierarchy;
    }
    internal sealed class Chunk7AssistedStoryIdentityEvidence
    {
        internal string RunId,CampaignId,CampaignName,MainId;internal int Generation;
        internal bool GameMatches,PlayerMatches,SaveManagerMatches,MainMatches,DescriptorMatches,AuthorityMatches,ExactIsolatedFreshCampaign,NoForeignOrMountedCustody;
    }
    internal sealed class Chunk7AssistedStoryAttempt
    {
        internal int Sequence,OfferIndex,RowIndex,NativeIndex,Answer,Row;
        internal string AnswerGuid,Failure;
        internal bool NativeEntered,NativeReturned,AfterReadSucceeded,SystemButton;
        // The observed native kind of the clicked answer, recorded per attempt so the clicked identity is
        // read out of the evidence instead of inferred from the after-read. IsExitAnswer is the DialogRoot
        // Exit/InterchapterExit identity; IsContinueAnswer is the Continue/InterchapterContinue identity
        // (IsSystemAnswer without IsExitAnswer); ContinueCueDisplayed is the controller's displayed
        // continue cue at the click. A cached PC row records all three as observed, normally all false.
        internal bool IsExitAnswer,IsContinueAnswer,ContinueCueDisplayed;
        internal Chunk7AssistedStoryEvidence Before,After;
    }
    // Fixture setup only. A cached-enabled row is the selected UI affordance,
    // not a claim about predicate purity, narrative meaning or terminal success.
    internal sealed class Chunk7AssistedStoryDriver : IDisposable
    {
        // The 300000 ms total deadline and the 30000 ms no-progress bound are the real safety nets and are
        // unchanged. MaximumChoices is only a coarse COUNT bound: the anti-replay guarantee is structural,
        // because a (controller, dialog, cue, answer) tuple is acted on at most once and the used set only
        // grows, so no cue can be re-driven by the same answer however many ticks run. The observed
        // prologue alone consumed 13 of the previous 16 (one interchapter row, nine DialogueSystemButton
        // continue advances, the terminal Exit answer and three PC choices) before reaching the first
        // conversation hub, so 16 could not reach the non-dialog stages at all. 48 is three times that
        // observed prologue cost, stays far below the structural ceiling (at most MaximumOffers distinct
        // answers per cue), and keeps a run strictly bounded in count as well as in time; the evidence
        // stays bounded with it, since each attempt records two snapshots of at most MaximumOffers offers
        // and MaximumRows rows.
        internal const int MaximumChoices=48, MaximumOffers=32, MaximumRows=32;
        internal const long MaximumMilliseconds=300000, ProgressMilliseconds=30000;
        internal const string PcRows="pc-cached-rows", BookRows="book-rows", InterchapterRows="interchapter-rows";
        // Common PC dialogues, book pages and interchapter pages are the selected native kinds; each has one ready status.
        internal static string SelectedStatus(int kind){switch(kind){case 0:return PcRows;case 1:return BookRows;case 2:return InterchapterRows;default:return null;}}
        private readonly Chunk7AssistedFixtureBinding binding;
        private IChunk7AssistedStoryBoundary boundary;
        private readonly List<Chunk7AssistedStoryAttempt> attempts=new List<Chunk7AssistedStoryAttempt>();
        private readonly List<UsedContext> used=new List<UsedContext>();
        private readonly Dictionary<object,int> identities=new Dictionary<object,int>(new References());
        private Chunk7AssistedStorySnapshot pending;
        private Chunk7AssistedStoryAttempt pendingAttempt;
        private long lastTime=-1, progressStart;
        private int lastFrame=-1, nativeClicks;
        private bool executing, failed, disposed;
        internal Chunk7AssistedStoryStatus Status {get;private set;}
        internal string Failure {get;private set;}
        internal int NativeClickCount {get{return nativeClicks;}}
        internal int AttemptCount {get{return attempts.Count;}}
        internal Chunk7AssistedStoryEvidence LastObserved {get;private set;}
        internal IList<Chunk7AssistedStoryAttempt> Attempts {get{return attempts.AsReadOnly();}}
        internal Chunk7AssistedStoryDriver(Chunk7AssistedFixtureBinding binding,IChunk7AssistedStoryBoundary boundary)
        { this.binding=binding??throw new ArgumentNullException("binding");this.boundary=boundary??throw new ArgumentNullException("boundary");Status=Chunk7AssistedStoryStatus.Waiting; }
        internal Chunk7AssistedStoryStatus Tick()
        {
            if(disposed)throw new ObjectDisposedException("Chunk7AssistedStoryDriver");
            if(failed)throw new InvalidOperationException("Assisted story setup is already failed: "+Failure);
            if(executing)throw new InvalidOperationException("Reentrant assisted story input is forbidden.");
            executing=true;
            try
            {
                var current=boundary.Read(); RequireSnapshot(current); LastObserved=Capture(current);
                if(current.ElapsedMilliseconds>MaximumMilliseconds)throw new InvalidOperationException("Assisted story setup exceeded its total deadline.");
                if(lastTime<0)progressStart=current.ElapsedMilliseconds;
                var previousFrame=lastFrame;
                lastTime=current.ElapsedMilliseconds; lastFrame=current.Frame;
                if(current.UnexpectedPopup)throw new InvalidOperationException("Unexpected non-story dialog refuses all fixture input.");
                if(!current.MouseController)throw new InvalidOperationException("Assisted story input requires the native PC/mouse controller.");
                if(current.Mode=="Default"&&!current.NativeBusy&&!current.Cutscene&&!current.TransitionPending&&!current.DialogActive&&current.MainViewReady)
                {
                    // Even a synchronous native transition cannot settle an owned
                    // click until a strictly later observed frame.
                    if(pending!=null&&current.Frame<=pending.Frame)
                    {
                        if(current.ElapsedMilliseconds-progressStart>ProgressMilliseconds)throw NoProgress(current);
                        return Status=Chunk7AssistedStoryStatus.Waiting;
                    }
                    pending=null;pendingAttempt=null;progressStart=current.ElapsedMilliseconds;return Status=Chunk7AssistedStoryStatus.ReadyAtObservedFrame;
                }
                if(pending!=null && Progressed(pending,current)) {pending=null;pendingAttempt=null;progressStart=current.ElapsedMilliseconds;}
                // Liveness, not a timeout. An owned click can be CONSUMED by the engine without advancing
                // the cue: the observed stall re-entered the identical dialog, cue and answer objects with
                // the dialog still active, a state no Progressed clause can ever report, so the pending
                // click could never settle and the gate below short-circuited ahead of selection, the
                // choice cap and the dedupe until the no-progress bound threw. A strictly later observed
                // frame that still presents that identical active cue resolves the click as consumed and
                // lets selection proceed on this observation. It never silently forgets an unobserved
                // outcome: the frame must have advanced past the click, the click must have been observed
                // to enter, return and read back, and the engine must be idle. The progress clock is NOT
                // re-armed here - no cue advanced - so a genuinely frozen game still trips the unchanged
                // bound, and the unchanged total deadline still ends any run.
                else if(pending!=null && Consumed(pendingAttempt,pending,current)) {pending=null;pendingAttempt=null;}
                // The prologue narration plays INSIDE a cutscene, so a closed dialog can leave the engine
                // running that cutscene with nothing else observable for longer than ProgressMilliseconds.
                // With no settled click outstanding, no active dialog and a cutscene playing, a strictly
                // increasing native frame count is genuine engine advance and re-arms the progress clock.
                // A frozen frame count re-arms nothing and still trips the unchanged bound below; a cutscene
                // that never ends is still terminated by the unchanged total deadline.
                if(pending==null && !current.DialogActive && (current.Mode=="Cutscene"||current.Cutscene) && previousFrame>=0 && current.Frame>previousFrame)
                    progressStart=current.ElapsedMilliseconds;
                if(current.ElapsedMilliseconds-progressStart>ProgressMilliseconds)throw NoProgress(current);
                if(current.NativeBusy||current.TransitionPending || current.Mode=="Cutscene"||current.Cutscene)
                    return Status=Chunk7AssistedStoryStatus.Waiting;
                if(current.Mode!="Dialog")throw new InvalidOperationException("Only native Default, pending transition or ordinary PC story Dialog is selected.");
                if(current.ManagerKind.HasValue && SelectedStatus(current.ManagerKind.Value)==null)throw new InvalidOperationException("Only common, book and interchapter native dialog kinds are selected.");
                if(!current.ManagerKind.HasValue || current.UiStatus!=SelectedStatus(current.ManagerKind.Value) || current.ManagerShown!=true || current.PcActive!=true || !current.DialogActive)
                    return Status=Chunk7AssistedStoryStatus.Waiting;
                ValidateChoices(current);
                if(pending!=null || current.Offers.Length==0)return Status=Chunk7AssistedStoryStatus.Waiting;
                var selectedOffer=-1;var selectedRow=-1;var eligibleRows=0;var eligibleRowsUsed=0;
                for(var i=0;i<current.Offers.Length;i++)
                {
                    for(var j=0;j<current.Rows.Length;j++)if(ReferenceEquals(current.Offers[i].Answer,current.Rows[j].Answer))
                    {
                        var row=current.Rows[j];
                        if(!row.Initialized||row.ChoosingCharacter||row.Interchapter&&current.ManagerKind.Value!=2)throw new InvalidOperationException("A current owned answer row is uninitialized or has foreign chooser/interchapter custody.");
                        // Walk the eligible rows in the native offered order and take the FIRST whose
                        // (controller, dialog, cue, answer) tuple this run has not used. Consulting the
                        // used set here, ahead of the dedupe below, is what stops a story hub that
                        // re-presents the same answers from either fixating on one row forever or making
                        // the dedupe throw on a tuple the policy could have walked past. The used set only
                        // grows, so each cue is still acted on at most once per distinct answer.
                        if(!row.Disabled&&row.ActiveAndEnabled&&row.ActiveInHierarchy)
                        {
                            eligibleRows++;
                            if(Used(current,current.Offers[i].Answer))eligibleRowsUsed++;
                            else if(selectedOffer<0){selectedOffer=i;selectedRow=j;}
                        }
                    }
                }
                if(selectedOffer<0&&eligibleRows>0&&eligibleRows==eligibleRowsUsed)throw Exhausted(current);
                if(selectedOffer<0)
                {
                    // No eligible TempOptionUI row. A root SYSTEM answer (DialogRoot.ContinueAnswer or
                    // DialogRoot.ExitAnswer for a COMMON cue) is excluded from m_AnswersList by Fill and routed
                    // to the DialogueSystemButton. Prefer a real row when one exists; only advance the system
                    // button when none is eligible.
                    var systemOffer=-1;
                    if(current.SystemButton!=null && current.SystemButtonEnabled==true && current.Rows.Length==0)
                        for(var i=0;i<current.Offers.Length;i++)
                            if(ReferenceEquals(current.Offers[i].Answer,current.SystemButtonAnswer)){if(systemOffer>=0){systemOffer=-1;break;}systemOffer=i;}
                    var everyOfferIsSystem=current.Offers.Length>0;
                    foreach(var candidate in current.Offers)if(!candidate.IsSystemAnswer)everyOfferIsSystem=false;
                    if(systemOffer<0 || !everyOfferIsSystem || !current.Offers[systemOffer].IsSystemAnswer)return Status=Chunk7AssistedStoryStatus.Waiting;
                    var systemAnswer=current.Offers[systemOffer];
                    if(systemAnswer.SelectionType==4)throw new InvalidOperationException("Manual character selection needs a separately owned native chooser and is not selected.");
                    if(systemAnswer.OnSelectActionTypeCount!=0)throw new InvalidOperationException("A root system answer carrying OnSelect actions is not auto-advanced by assisted continue.");
                    // A terminal DialogRoot Exit answer is the finished narration's own dismiss affordance: its
                    // SelectNextCue returns null, so the native SelectAnswer stops the dialog instead of
                    // scheduling a cue, and the closed dialog is ordinary observed progress (Progressed/Default
                    // readiness). It is advanced only through the same verified DialogueSystemButton click.
                    // A continue answer still requires the displayed native continue cue, exactly as before.
                    if(!systemAnswer.IsExitAnswer&&current.ContinueCue==null)throw new InvalidOperationException("A continue system answer without the displayed native continue cue is not driven by assisted continue.");
                    // The sole root system answer is the cue's only affordance, so there is nothing for the
                    // policy to walk past: an already-used one is an exhausted cue, named explicitly rather
                    // than left to the dedupe below.
                    if(Used(current,systemAnswer.Answer))throw Exhausted(current);
                    if(attempts.Count>=MaximumChoices)throw new InvalidOperationException("Assisted story choice cap reached.");
                    // Retained invariant guard, now unreachable: the explicit refusal above already covers
                    // a used root system answer. It stays so no path can ever replay an owned tuple.
                    if(Used(current,systemAnswer.Answer))throw new InvalidOperationException("A previously attempted story context cannot be clicked again.");
                    // Retain attempted provenance before the potentially mutating native continue.
                    var systemAttempt=new Chunk7AssistedStoryAttempt{Sequence=attempts.Count+1,OfferIndex=systemOffer,RowIndex=-1,NativeIndex=-1,
                        Answer=Identity(systemAnswer.Answer),Row=0,AnswerGuid=systemAnswer.Guid,SystemButton=true,
                        IsExitAnswer=systemAnswer.IsExitAnswer,IsContinueAnswer=systemAnswer.IsSystemAnswer&&!systemAnswer.IsExitAnswer,ContinueCueDisplayed=current.ContinueCue!=null,Before=LastObserved};
                    attempts.Add(systemAttempt);used.Add(new UsedContext{Controller=current.Controller,Dialog=current.Dialog,Cue=current.Cue,Answer=systemAnswer.Answer});
                    pending=current;pendingAttempt=systemAttempt;progressStart=current.ElapsedMilliseconds;
                    try
                    {
                        boundary.ClickSystemButton(current,()=>{if(systemAttempt.NativeEntered)throw new InvalidOperationException("Duplicate native system-button dispatch is forbidden.");systemAttempt.NativeEntered=true;nativeClicks++;});
                        if(!systemAttempt.NativeEntered)throw new InvalidOperationException("Native system-button entry was not observed.");
                        systemAttempt.NativeReturned=true;
                        var afterSystem=boundary.Read();RequireSnapshot(afterSystem);systemAttempt.After=Capture(afterSystem);systemAttempt.AfterReadSucceeded=true;LastObserved=systemAttempt.After;
                    }
                    catch(Exception error){systemAttempt.Failure=Bound(error.GetType().Name+": "+error.Message);throw;}
                    return Status=Chunk7AssistedStoryStatus.Clicked;
                }
                var offer=current.Offers[selectedOffer];var selected=current.Rows[selectedRow];
                if(offer.SelectionType==4)throw new InvalidOperationException("Manual character selection needs a separately owned native chooser and is not selected.");
                if(attempts.Count>=MaximumChoices)throw new InvalidOperationException("Assisted story choice cap reached.");
                // Retained invariant guard. Selection above already walked past every used tuple, so this
                // can no longer fire on a tuple the policy could have skipped.
                if(Used(current,offer.Answer))throw new InvalidOperationException("A previously attempted story context cannot be clicked again.");
                // Retain attempted provenance before the potentially mutating call.
                var attempt=new Chunk7AssistedStoryAttempt{Sequence=attempts.Count+1,OfferIndex=selectedOffer,RowIndex=selectedRow,
                    NativeIndex=selected.NativeIndex,Answer=Identity(offer.Answer),Row=Identity(selected.View),AnswerGuid=offer.Guid,
                    IsExitAnswer=offer.IsExitAnswer,IsContinueAnswer=offer.IsSystemAnswer&&!offer.IsExitAnswer,ContinueCueDisplayed=current.ContinueCue!=null,Before=LastObserved};
                attempts.Add(attempt);used.Add(new UsedContext{Controller=current.Controller,Dialog=current.Dialog,Cue=current.Cue,Answer=offer.Answer});
                pending=current;pendingAttempt=attempt;progressStart=current.ElapsedMilliseconds;
                try
                {
                    boundary.Click(current,selectedOffer,selectedRow,()=>{if(attempt.NativeEntered)throw new InvalidOperationException("Duplicate native row dispatch is forbidden.");attempt.NativeEntered=true;nativeClicks++;});
                    if(!attempt.NativeEntered)throw new InvalidOperationException("Native row entry was not observed.");
                    attempt.NativeReturned=true;
                    var after=boundary.Read();RequireSnapshot(after);attempt.After=Capture(after);attempt.AfterReadSucceeded=true;LastObserved=attempt.After;
                }
                catch(Exception error){attempt.Failure=Bound(error.GetType().Name+": "+error.Message);throw;}
                return Status=Chunk7AssistedStoryStatus.Clicked;
            }
            catch(Exception error){failed=true;Failure=Bound(error.GetType().Name+": "+error.Message);Status=Chunk7AssistedStoryStatus.Failed;throw;}
            finally{executing=false;}
        }
        private static InvalidOperationException NoProgress(Chunk7AssistedStorySnapshot current)
        {
            return new InvalidOperationException("Assisted story setup made no observed native progress within its bound. " +
                "mode="+current.Mode+" frame="+current.Frame+" transitionPending="+current.TransitionPending+
                " scheduledStart="+current.ScheduledStart+" cuePlayScheduled="+current.CuePlayScheduled+
                " dialogStopScheduled="+current.DialogStopScheduled+" nativeBusy="+current.NativeBusy+
                " cutscene="+current.Cutscene+" dialogActive="+current.DialogActive);
        }
        internal static void RequireSameClickState(Chunk7AssistedStorySnapshot expected,Chunk7AssistedStorySnapshot actual)
        {
            if(expected==null||actual==null||expected.Frame!=actual.Frame||expected.Mode!=actual.Mode||expected.UiStatus!=actual.UiStatus||
                !ReferenceEquals(expected.Controller,actual.Controller)||!ReferenceEquals(expected.Dialog,actual.Dialog)||!ReferenceEquals(expected.Cue,actual.Cue)||
                !ReferenceEquals(expected.Ui,actual.Ui)||!ReferenceEquals(expected.Common,actual.Common)||!ReferenceEquals(expected.Manager,actual.Manager)||
                !ReferenceEquals(expected.PcDialog,actual.PcDialog)||!ReferenceEquals(expected.CurrentPart,actual.CurrentPart)||
                !ReferenceEquals(expected.OffersList,actual.OffersList)||!ReferenceEquals(expected.RowsList,actual.RowsList)||
                !ReferenceEquals(expected.SystemButton,actual.SystemButton)||expected.SystemButtonEnabled!=actual.SystemButtonEnabled||!ReferenceEquals(expected.SystemButtonAnswer,actual.SystemButtonAnswer)||
                expected.DialogGuid!=actual.DialogGuid||expected.CueGuid!=actual.CueGuid||expected.ManagerKind!=actual.ManagerKind||!actual.ManagerKind.HasValue||actual.UiStatus!=SelectedStatus(actual.ManagerKind.Value)||actual.ManagerShown!=true||actual.PcActive!=true||
                !actual.MouseController||actual.UnexpectedPopup||actual.NativeBusy||actual.TransitionPending||!actual.DialogActive)
                throw new InvalidOperationException("Native story context or frame changed before row dispatch.");
            ValidateChoices(expected);ValidateChoices(actual);
            if(expected.Offers.Length!=actual.Offers.Length||expected.Rows.Length!=actual.Rows.Length)throw new InvalidOperationException("Native story list length changed.");
            for(var i=0;i<expected.Offers.Length;i++)if(!ReferenceEquals(expected.Offers[i].Answer,actual.Offers[i].Answer)||!ReferenceEquals(expected.Offers[i].Selection,actual.Offers[i].Selection)||expected.Offers[i].Guid!=actual.Offers[i].Guid||expected.Offers[i].SelectionType!=actual.Offers[i].SelectionType)
                throw new InvalidOperationException("Native offered answer changed before dispatch.");
            for(var i=0;i<expected.Rows.Length;i++)
            {var a=expected.Rows[i];var b=actual.Rows[i];if(!ReferenceEquals(a.View,b.View)||!ReferenceEquals(a.Answer,b.Answer)||a.Guid!=b.Guid||a.NativeIndex!=b.NativeIndex||a.Initialized!=b.Initialized||a.Disabled!=b.Disabled||a.ChoosingCharacter!=b.ChoosingCharacter||a.Interchapter!=b.Interchapter||a.ActiveAndEnabled!=b.ActiveAndEnabled||a.ActiveInHierarchy!=b.ActiveInHierarchy)
                throw new InvalidOperationException("Native owned row changed before dispatch.");}
        }
        private void RequireSnapshot(Chunk7AssistedStorySnapshot value)
        {
            if(value==null)throw new InvalidOperationException("Assisted story snapshot is unavailable.");binding.Require(value.Identity);
            if(value.Frame<0||value.Frame<lastFrame||value.ElapsedMilliseconds<0||value.ElapsedMilliseconds<lastTime||value.Mode==null||value.Mode.Length>64||value.UiStatus!=null&&value.UiStatus.Length>64)
                throw new InvalidOperationException("Assisted story snapshot has invalid time or bounded mode data.");
            if(value.Offers!=null&&value.Offers.Length>MaximumOffers||value.Rows!=null&&value.Rows.Length>MaximumRows)throw new InvalidOperationException("Assisted story snapshot exceeds its collection bound.");
        }
        private static void ValidateChoices(Chunk7AssistedStorySnapshot state)
        {
            if(state.Controller==null||state.Dialog==null||state.Cue==null||state.Ui==null||state.Common==null||state.Manager==null||state.PcDialog==null||state.CurrentPart==null||state.OffersList==null||state.RowsList==null||
                state.Offers==null||state.Rows==null||state.Offers.Length>MaximumOffers||state.Rows.Length>MaximumRows||!GuidShape(state.DialogGuid)||!GuidShape(state.CueGuid))
                throw new InvalidOperationException("A complete bounded owned PC dialogue context is required.");
            for(var i=0;i<state.Offers.Length;i++)
            {
                var a=state.Offers[i];if(a==null||a.Answer==null||a.Selection==null||!GuidShape(a.Guid)||!a.SelectionType.HasValue||a.SelectionType.Value<0||a.SelectionType.Value>5)
                    throw new InvalidOperationException("Offered native answer identity/selection is unavailable or unsupported.");
                for(var j=0;j<i;j++)if(ReferenceEquals(a.Answer,state.Offers[j].Answer)||a.Guid==state.Offers[j].Guid)throw new InvalidOperationException("Duplicate offered answer identity is ambiguous.");
            }
            for(var i=0;i<state.Rows.Length;i++)
            {
                var row=state.Rows[i];if(row==null||row.View==null||row.Answer==null||!GuidShape(row.Guid)||row.NativeIndex<0||row.NativeIndex>MaximumRows)
                    throw new InvalidOperationException("Native owned row identity/index is unavailable.");
                int matches=0;foreach(var a in state.Offers)if(ReferenceEquals(a.Answer,row.Answer)&&a.Guid==row.Guid)matches++;
                if(matches!=1)throw new InvalidOperationException("A native row belongs to a foreign or ambiguous offer.");
                for(var j=0;j<i;j++)if(ReferenceEquals(row.View,state.Rows[j].View)||ReferenceEquals(row.Answer,state.Rows[j].Answer)||row.NativeIndex==state.Rows[j].NativeIndex)
                    throw new InvalidOperationException("Duplicate native PC row identity/index is ambiguous.");
            }
        }
        private static bool Progressed(Chunk7AssistedStorySnapshot before,Chunk7AssistedStorySnapshot after)
        {
            if(after.Frame<=before.Frame)return false;
            if(after.Mode=="Default"&&!after.DialogActive)return true;
            // A dialog that was active and is no longer active is the observed native effect of the owned
            // click whatever mode the engine returns to: the terminal Exit click's StopDialog can return the
            // prologue narration's surrounding Cutscene mode, whose snapshot carries no dialog and no offers.
            if(before.DialogActive&&!after.DialogActive)return true;
            if(after.Mode!="Dialog"||after.Controller==null||after.Dialog==null||after.Cue==null)return false;
            if(!ReferenceEquals(before.Controller,after.Controller))throw new InvalidOperationException("Native dialogue controller changed while awaiting an owned click.");
            if(!ReferenceEquals(before.Dialog,after.Dialog)||!ReferenceEquals(before.Cue,after.Cue))return true;
            if(after.Offers==null)return false;
            if(after.Offers.Length!=before.Offers.Length)return true;
            for(var i=0;i<before.Offers.Length;i++)if(!ReferenceEquals(before.Offers[i].Answer,after.Offers[i].Answer))return true;
            return false;
        }
        // A pending owned click is resolved as consumed-without-advance ONLY on a strictly later observed
        // frame that still presents the identical native controller, dialog, cue and answer objects with
        // the dialog active and the engine idle. Every clause is an identity or observed-state comparison
        // (the same ReferenceEquals identity the dispatch boundary uses); none is time-based. A click with
        // no observed native entry, no observed return, no successful after read, or no later observed
        // frame is never resolved here and still has to settle or trip the unchanged progress bound.
        private static bool Consumed(Chunk7AssistedStoryAttempt attempt,Chunk7AssistedStorySnapshot before,Chunk7AssistedStorySnapshot after)
        {
            if(attempt==null||!attempt.NativeEntered||!attempt.NativeReturned||!attempt.AfterReadSucceeded)return false;
            if(after.Frame<=before.Frame||after.NativeBusy||after.TransitionPending)return false;
            if(after.Mode!="Dialog"||!before.DialogActive||!after.DialogActive)return false;
            if(after.Controller==null||after.Dialog==null||after.Cue==null)return false;
            if(!ReferenceEquals(before.Controller,after.Controller)||!ReferenceEquals(before.Dialog,after.Dialog)||!ReferenceEquals(before.Cue,after.Cue))return false;
            if(before.Offers==null||after.Offers==null||before.Offers.Length==0||after.Offers.Length!=before.Offers.Length)return false;
            for(var i=0;i<before.Offers.Length;i++)
            {
                if(before.Offers[i]==null||after.Offers[i]==null||!ReferenceEquals(before.Offers[i].Answer,after.Offers[i].Answer))return false;
            }
            return true;
        }
        private bool Used(Chunk7AssistedStorySnapshot state,object answer)
        {
            foreach(var prior in used)if(ReferenceEquals(prior.Controller,state.Controller)&&ReferenceEquals(prior.Dialog,state.Dialog)&&
                ReferenceEquals(prior.Cue,state.Cue)&&ReferenceEquals(prior.Answer,answer))return true;
            return false;
        }
        // The explicit bounded refusal for an exhausted cue. Reaching it means the engine keeps presenting
        // a cue whose every selected owned answer this run already clicked, which is a finished story
        // position the fixture cannot drive any further - never a silent stall and never a replay.
        private static InvalidOperationException Exhausted(Chunk7AssistedStorySnapshot state)
        {return new InvalidOperationException("Every selected owned answer at this story cue is already used: dialog "+state.DialogGuid+" cue "+state.CueGuid+".");}
        private Chunk7AssistedStoryEvidence Capture(Chunk7AssistedStorySnapshot s)
        {
            var offers=s.Offers??new Chunk7AssistedStoryOffer[0];var rows=s.Rows??new Chunk7AssistedStoryRow[0];
            var e=new Chunk7AssistedStoryEvidence{Frame=s.Frame,ElapsedMilliseconds=s.ElapsedMilliseconds,Mode=s.Mode,UiStatus=s.UiStatus,
                DialogGuid=SafeGuid(s.DialogGuid),CueGuid=SafeGuid(s.CueGuid),NativeBusy=s.NativeBusy,TransitionPending=s.TransitionPending,DialogActive=s.DialogActive,UnexpectedPopup=s.UnexpectedPopup,
                MouseController=s.MouseController,Cutscene=s.Cutscene,MainViewReady=s.MainViewReady,ManagerKind=s.ManagerKind,ManagerShown=s.ManagerShown,PcActive=s.PcActive,
                Controller=Identity(s.Controller),Dialog=Identity(s.Dialog),Cue=Identity(s.Cue),Ui=Identity(s.Ui),Common=Identity(s.Common),Manager=Identity(s.Manager),PcDialog=Identity(s.PcDialog),Part=Identity(s.CurrentPart),OffersList=Identity(s.OffersList),RowsList=Identity(s.RowsList),
                Identity=new Chunk7AssistedStoryIdentityEvidence{RunId=s.Identity.RunId,CampaignId=s.Identity.CampaignId,CampaignName=s.Identity.CampaignName,MainId=s.Identity.MainId,Generation=s.Identity.Generation,
                    GameMatches=ReferenceEquals(binding.Game,s.Identity.Game),PlayerMatches=ReferenceEquals(binding.Player,s.Identity.Player),SaveManagerMatches=ReferenceEquals(binding.SaveManager,s.Identity.SaveManager),
                    MainMatches=ReferenceEquals(binding.Main,s.Identity.Main),DescriptorMatches=ReferenceEquals(binding.Descriptor,s.Identity.Descriptor),AuthorityMatches=ReferenceEquals(binding.Authority,s.Identity.Authority),
                    ExactIsolatedFreshCampaign=s.Identity.ExactIsolatedFreshCampaign,NoForeignOrMountedCustody=s.Identity.NoForeignOrMountedCustody},
                OfferObjects=new int[offers.Length],OfferSelectionObjects=new int[offers.Length],OfferGuids=new string[offers.Length],OfferSelectionTypes=new int?[offers.Length],
                RowObjects=new int[rows.Length],RowAnswerObjects=new int[rows.Length],RowNativeIndices=new int[rows.Length],RowInitialized=new bool[rows.Length],RowDisabled=new bool[rows.Length],
                RowChoosingCharacter=new bool[rows.Length],RowInterchapter=new bool[rows.Length],RowActiveAndEnabled=new bool[rows.Length],RowActiveInHierarchy=new bool[rows.Length]};
            for(int i=0;i<offers.Length;i++){var a=offers[i];if(a==null)continue;e.OfferObjects[i]=Identity(a.Answer);e.OfferSelectionObjects[i]=Identity(a.Selection);e.OfferGuids[i]=SafeGuid(a.Guid);e.OfferSelectionTypes[i]=a.SelectionType;}
            for(int i=0;i<rows.Length;i++){var r=rows[i];if(r==null)continue;e.RowObjects[i]=Identity(r.View);e.RowAnswerObjects[i]=Identity(r.Answer);e.RowNativeIndices[i]=r.NativeIndex;e.RowInitialized[i]=r.Initialized;e.RowDisabled[i]=r.Disabled;e.RowChoosingCharacter[i]=r.ChoosingCharacter;e.RowInterchapter[i]=r.Interchapter;e.RowActiveAndEnabled[i]=r.ActiveAndEnabled;e.RowActiveInHierarchy[i]=r.ActiveInHierarchy;}
            return e;
        }
        private int Identity(object value){if(value==null)return 0;int id;if(identities.TryGetValue(value,out id))return id;if(identities.Count>=2048)throw new InvalidOperationException("Assisted story identity bound exceeded.");id=identities.Count+1;identities.Add(value,id);return id;}
        private static bool GuidShape(string value){if(value==null||value.Length!=32)return false;foreach(var c in value)if(!(c>='0'&&c<='9')&&!(c>='a'&&c<='f'))return false;return true;}
        private static string SafeGuid(string value){if(value==null)return null;if(!GuidShape(value))throw new InvalidOperationException("Native blueprint GUID is malformed.");return value;}
        private static string Bound(string text){return text.Length>256?text.Substring(0,256):text;}
        public void Dispose(){if(executing)throw new InvalidOperationException("Cannot release story custody during native dispatch.");disposed=true;pending=null;pendingAttempt=null;used.Clear();identities.Clear();boundary=null;Status=Chunk7AssistedStoryStatus.Disposed;}
        private sealed class UsedContext{internal object Controller,Dialog,Cue,Answer;}
        private sealed class References:IEqualityComparer<object>{public new bool Equals(object x,object y){return ReferenceEquals(x,y);}public int GetHashCode(object x){return RuntimeHelpers.GetHashCode(x);}}
    }
}
