using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.Controllers.Dialog;
using Kingmaker.DialogSystem;
using Kingmaker.DialogSystem.Blueprints;
using Kingmaker.DialogSystem.State;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.BookEvent;
using Kingmaker.UI.Dialog;
using Kingmaker.UnitLogic.Parts;
using UnityEngine;
using NativeController=Kingmaker.Controllers.Dialog.DialogController;
using PcController=Kingmaker.UI.Dialog.DialogController;
namespace KingmakerMountedCombat.Diagnostics
{
    // The connected selected host must supply the exact request/isolation proof,
    // pending-write/loading fence, no-mounted-custody proof, and canonical native
    // preservation observation. No general runtime entry point installs this.
    internal sealed class Chunk7AssistedFixtureNativeBoundary:IChunk7AssistedStoryBoundary,IChunk7AssistedShortfallBoundary
    {
        private readonly Chunk7AssistedFixtureBinding binding;
        private readonly Game game;private readonly Player player;private readonly UnitEntityData main;
        private readonly Action assertOwnedSetupAuthority;
        private readonly Func<bool> nativeBusy,noMountedCustody;
        private readonly Func<string> preservedStateSha256;
        private readonly int targetLevel;
        private readonly Stopwatch elapsed=Stopwatch.StartNew();
        private readonly int ownerThread;
        internal Chunk7AssistedFixtureNativeBoundary(Chunk7AssistedFixtureBinding binding,Action assertOwnedSetupAuthority,
            Func<bool> nativeBusy,Func<bool> noMountedCustody,Func<string> preservedStateSha256,int targetLevel)
        {
            this.binding=binding??throw new ArgumentNullException("binding");
            this.assertOwnedSetupAuthority=assertOwnedSetupAuthority??throw new ArgumentNullException("assertOwnedSetupAuthority");
            this.nativeBusy=nativeBusy??throw new ArgumentNullException("nativeBusy");this.noMountedCustody=noMountedCustody??throw new ArgumentNullException("noMountedCustody");
            this.preservedStateSha256=preservedStateSha256??throw new ArgumentNullException("preservedStateSha256");
            if(targetLevel<4||targetLevel>Chunk7AssistedShortfall.MaximumTargetLevel)throw new ArgumentOutOfRangeException("targetLevel");this.targetLevel=targetLevel;
            game=binding.Game as Game;player=binding.Player as Player;main=binding.Main as UnitEntityData;
            ownerThread=System.Threading.Thread.CurrentThread.ManagedThreadId;
            if(game==null||player==null||main==null)throw new InvalidOperationException("Exact native game/player/main binding is unavailable.");
            RequireMethod(typeof(TempOptionUI),"OnClick",0x06004616,typeof(void),Type.EmptyTypes);
            RequireMethod(typeof(DialogueSystemButton),"OnClick",0x0600460D,typeof(void),Type.EmptyTypes);
            RequireMethod(typeof(Kingmaker.UnitLogic.UnitProgressionData),"GainExperience",0x06001F80,typeof(void),new[]{typeof(int),typeof(bool)});
            RequireMethod(typeof(Player),"GainMoney",0x06000DD0,typeof(void),new[]{typeof(long)});
            ReadIdentity();
        }
        Chunk7AssistedStorySnapshot IChunk7AssistedStoryBoundary.Read(){return ReadStory();}
        Chunk7AssistedShortfallSnapshot IChunk7AssistedShortfallBoundary.Read(){return ReadShortfall();}
        private Chunk7AssistedIdentity ReadIdentity()
        {
            if(System.Threading.Thread.CurrentThread.ManagedThreadId!=ownerThread)throw new InvalidOperationException("Assisted fixture calls left their owning host thread.");
            assertOwnedSetupAuthority();
            var current=Game.Instance;var p=current==null?null:current.Player;var actor=p==null?null:p.MainCharacter.Value;
            var result=new Chunk7AssistedIdentity{Game=current,Player=p,SaveManager=current==null?null:current.SaveManager,Main=actor,
                Descriptor=actor==null?null:actor.Descriptor,Authority=binding.Authority,RunId=binding.RunId,Generation=binding.Generation,
                CampaignId=p==null?null:p.GameId,CampaignName=actor==null?null:actor.CharacterName,MainId=actor==null?null:actor.UniqueId,
                ExactIsolatedFreshCampaign=true,NoForeignOrMountedCustody=noMountedCustody()};
            binding.Require(result);return result;
        }
        internal Chunk7AssistedStorySnapshot ReadStory()
        {
            var frame=Time.frameCount;var identity=ReadIdentity();
            var controller=ReadField<NativeController>(typeof(Game),game,"DialogController",0x040006C7);
            var dialog=controller==null?null:ReadField<BlueprintDialog>(typeof(NativeController),controller,"Dialog",0x04005E5C);
            var cue=controller==null?null:ReadField<BlueprintCue>(typeof(NativeController),controller,"m_CurrentCue",0x04005E5A);
            var dialogState=player.Dialog;if(dialogState==null)throw new InvalidOperationException("Native dialog scheduling state is unavailable.");
            var scheduled=ReadField<StartDialogData>(typeof(DialogState),dialogState,"<Scheduled>k__BackingField",0x04005611);
            // m_CueToPlay keeps the last scheduled cue until Clear() at dialog start/stop and m_ContinueCue
            // names a displayed continue affordance; neither is a pending transition. Only a scheduled
            // dialog start, a scheduled cue play or a scheduled stop defers input (installed IL: ScheduleCue
            // stores both cue fields, Tick clears only m_CuePlayScheduled, Clear clears m_CueToPlay).
            var play=controller!=null&&ReadField<bool>(typeof(NativeController),controller,"m_CuePlayScheduled",0x04005E6F);
            var stop=controller!=null&&ReadField<bool>(typeof(NativeController),controller,"m_DialogStopScheduled",0x04005E70);
            var popup=DialogMessageBox.Instance;
            var result=new Chunk7AssistedStorySnapshot{Identity=identity,Frame=frame,ElapsedMilliseconds=elapsed.ElapsedMilliseconds,
                Mode=game.CurrentMode.ToString(),MouseController=game.IsControllerMouse,NativeBusy=nativeBusy(),Cutscene=game.CutsceneLock,
                UnexpectedPopup=popup!=null&&popup.IsShown,TransitionPending=!ReferenceEquals(scheduled,null)||play||stop,
                DialogActive=game.IsModeActive(GameModeType.Dialog),MainViewReady=main.View!=null&&main.View.AgentASP!=null,
                Controller=controller,Dialog=dialog,Cue=cue,DialogGuid=dialog==null?null:dialog.AssetGuidThreadSafe,CueGuid=cue==null?null:cue.AssetGuidThreadSafe,
                Offers=new Chunk7AssistedStoryOffer[0],Rows=new Chunk7AssistedStoryRow[0],UiStatus="not-dialog"};
            if(game.CurrentMode==GameModeType.Dialog && !result.NativeBusy)
            {
                if(controller==null||dialog==null||cue==null){result.UiStatus="missing-controller-context";}
                else
                {
                    var actualKind=ReadField<DialogType>(typeof(BlueprintDialog),dialog,"Type",0x04005655);
                    if(actualKind!=DialogType.Common&&actualKind!=DialogType.Book&&actualKind!=DialogType.Interchapter){result.ManagerKind=(int)actualKind;result.UiStatus="unsupported-dialog-kind";}
                    else
                    {
                        var raw=Chunk7DialogueChoiceObservation.ReadNative(game,controller,dialog,cue);
                        if(raw.Error!=null)throw new InvalidOperationException("Passive native PC capture failed: "+raw.Error);
                        result.Ui=raw.Ui;result.Common=raw.Common;result.Manager=raw.Manager;result.PcDialog=raw.PcDialog;result.CurrentPart=raw.CurrentPart;
                        result.ManagerKind=raw.ManagerKind;result.ManagerShown=raw.ManagerShown;result.PcActive=raw.PcDialogActive;result.UiStatus=raw.UiStatus;
                        result.SystemButton=raw.SystemButton;result.SystemButtonEnabled=raw.SystemButtonEnabled;result.SystemButtonAnswer=raw.SystemButtonAnswer;result.ContinueCue=raw.ContinueCue;
                        var offers=ReadField<List<BlueprintAnswer>>(typeof(NativeController),controller,"m_Answers",0x04005E67);result.OffersList=offers;
                        if(offers==null||offers.Count>Chunk7AssistedStoryDriver.MaximumOffers||raw.Offered==null||offers.Count!=raw.Offered.Length)
                            throw new InvalidOperationException("Actual offered list is absent, changed or over bound.");
                        result.Offers=new Chunk7AssistedStoryOffer[offers.Count];
                        for(var i=0;i<offers.Count;i++)
                        {
                            var item=offers[i];if(item==null||!ReferenceEquals(item,raw.Offered[i].Answer))throw new InvalidOperationException("Native offered references changed.");
                            var selection=ReadField<CharacterSelection>(typeof(BlueprintAnswer),item,"CharacterSelection",0x0400561C);
                            var kind=selection==null?(int?)null:(int)ReadField<CharacterSelection.Type>(typeof(CharacterSelection),selection,"SelectionType",0x040055FD);
                            if(kind!=raw.Offered[i].SelectionType||item.AssetGuidThreadSafe!=raw.Offered[i].Guid)throw new InvalidOperationException("Native offered metadata changed.");
                            result.Offers[i]=new Chunk7AssistedStoryOffer{Answer=item,Selection=selection,Guid=raw.Offered[i].Guid,SelectionType=kind,
                                IsSystemAnswer=raw.Offered[i].IsSystemAnswer,IsExitAnswer=raw.Offered[i].IsExitAnswer,OnSelectActionTypeCount=raw.Offered[i].ActionTypes==null?0:raw.Offered[i].ActionTypes.Length};
                        }
                        if(actualKind!=DialogType.Common)ReadBookEventRows(result,raw,actualKind);
                        else if(raw.CurrentPart!=null)
                        {
                            var rows=ReadField<List<TempOptionUI>>(typeof(DialogCurrentPart),raw.CurrentPart,"m_AnswersList",0x04002ED8);result.RowsList=rows;
                            if(rows==null||rows.Count>Chunk7AssistedStoryDriver.MaximumRows||raw.Rows==null||rows.Count!=raw.Rows.Length)
                                throw new InvalidOperationException("Actual PC row list is absent, changed or over bound.");
                            result.Rows=new Chunk7AssistedStoryRow[rows.Count];
                            for(var i=0;i<rows.Count;i++)
                            {
                                var row=rows[i];var observed=raw.Rows[i];
                                if(row==null||!ReferenceEquals(row,observed.View))throw new InvalidOperationException("Native PC row reference changed.");
                                var answer=ReadField<BlueprintAnswer>(typeof(TempOptionUI),row,"m_Answer",0x04002EE0);
                                var index=ReadField<int>(typeof(TempOptionUI),row,"m_Index",0x04002EE1);
                                var choose=ReadField<bool>(typeof(TempOptionUI),row,"m_ChooseCharacterInit",0x04002EE2);
                                var init=ReadField<bool>(typeof(TempOptionUI),row,"m_IsInit",0x04002EE3);
                                var disabled=ReadField<bool>(typeof(TempOptionUI),row,"m_Disabled",0x04002EE4);
                                var inter=ReadField<bool>(typeof(TempOptionUI),row,"m_Interchapter",0x04002EE8);
                                if(!ReferenceEquals(answer,observed.Answer)||index!=observed.Index||choose!=observed.ChoosingCharacter||init!=observed.Initialized||disabled!=observed.Disabled||inter!=observed.Interchapter)
                                    throw new InvalidOperationException("Native PC row fields changed during capture.");
                                result.Rows[i]=new Chunk7AssistedStoryRow{View=row,Answer=answer,Guid=observed.Guid,NativeIndex=index,ChoosingCharacter=choose,Initialized=init,Disabled=disabled,Interchapter=inter,
                                    ActiveAndEnabled=row.isActiveAndEnabled,ActiveInHierarchy=row.gameObject!=null&&row.gameObject.activeInHierarchy};
                            }
                            if(!ReferenceEquals(ReadField<List<TempOptionUI>>(typeof(DialogCurrentPart),raw.CurrentPart,"m_AnswersList",0x04002ED8),rows))throw new InvalidOperationException("Native PC list object changed.");
                        }
                        if(!ReferenceEquals(ReadField<List<BlueprintAnswer>>(typeof(NativeController),controller,"m_Answers",0x04005E67),offers))throw new InvalidOperationException("Native offered list object changed.");
                    }
                }
            }
            if(Time.frameCount!=frame||!ReferenceEquals(Game.Instance,game)||!ReferenceEquals(ReadField<NativeController>(typeof(Game),game,"DialogController",0x040006C7),controller)||
                controller!=null&&(!ReferenceEquals(ReadField<BlueprintDialog>(typeof(NativeController),controller,"Dialog",0x04005E5C),dialog)||!ReferenceEquals(ReadField<BlueprintCue>(typeof(NativeController),controller,"m_CurrentCue",0x04005E5A),cue)))
                throw new InvalidOperationException("Native story context/frame changed during capture.");
            return result;
        }
        // Book-event pages (DialogType.Book and Interchapter) keep their options on the
        // manager's book controllers instead of the PC dialogue part. The same TempOptionUI
        // row contract, bounds, cached flags and verified click path apply; the page view,
        // its GameObject and the option container stand in for the PC view/part/list roles.
        private void ReadBookEventRows(Chunk7AssistedStorySnapshot result,Chunk7DialogueChoiceObservation.RawState raw,DialogType kind)
        {
            if(raw.Manager==null)return;
            if(raw.ManagerKind!=(int)kind)throw new InvalidOperationException("Native dialog kind differs between the blueprint and the UI manager.");
            var manager=(DialogBookEventUIManager)raw.Manager;
            TempOptionUI[] rows;object view,part,container;bool active;
            if(kind==DialogType.Interchapter)
            {
                var inter=ReadField<BookEventInterchapterController>(typeof(DialogBookEventUIManager),manager,"Interchapter",0x04002EBB);
                if(inter==null){result.UiStatus="missing-book-view";return;}
                var option=ReadField<TempOptionUI>(typeof(BookEventInterchapterController),inter,"m_Answer",0x04002D9A);
                view=inter;part=inter.gameObject;container=inter.transform;active=inter.isActiveAndEnabled&&inter.gameObject!=null&&inter.gameObject.activeInHierarchy;
                rows=option==null||!ReadField<bool>(typeof(TempOptionUI),option,"m_IsInit",0x04002EE3)?new TempOptionUI[0]:new[]{option};
            }
            else
            {
                var book=ReadField<BookEventController>(typeof(DialogBookEventUIManager),manager,"BookEvent",0x04002EBA);
                if(book==null){result.UiStatus="missing-book-view";return;}
                var answers=ReadField<Transform>(typeof(BookEventController),book,"Answers",0x04002D8F);
                if(answers==null){result.UiStatus="missing-book-answers";return;}
                view=book;part=book.gameObject;container=answers;active=book.isActiveAndEnabled&&book.gameObject!=null&&book.gameObject.activeInHierarchy;
                var found=answers.GetComponentsInChildren<TempOptionUI>(true);
                if(found==null||found.Length>Chunk7AssistedStoryDriver.MaximumRows)throw new InvalidOperationException("Actual book answer rows are absent or over bound.");
                var initialized=new List<TempOptionUI>();
                foreach(var candidate in found)if(candidate!=null&&ReadField<bool>(typeof(TempOptionUI),candidate,"m_IsInit",0x04002EE3))initialized.Add(candidate);
                rows=initialized.ToArray();
            }
            if(rows.Length>Chunk7AssistedStoryDriver.MaximumRows)throw new InvalidOperationException("Actual book answer rows are over bound.");
            result.PcDialog=view;result.CurrentPart=part;result.RowsList=container;result.PcActive=active;
            result.Rows=new Chunk7AssistedStoryRow[rows.Length];
            for(var i=0;i<rows.Length;i++)
            {
                var row=rows[i];if(row==null)throw new InvalidOperationException("Native book answer row is null or destroyed.");
                var answer=ReadField<BlueprintAnswer>(typeof(TempOptionUI),row,"m_Answer",0x04002EE0);
                result.Rows[i]=new Chunk7AssistedStoryRow{View=row,Answer=answer,Guid=answer==null?null:answer.AssetGuidThreadSafe,
                    NativeIndex=ReadField<int>(typeof(TempOptionUI),row,"m_Index",0x04002EE1),ChoosingCharacter=ReadField<bool>(typeof(TempOptionUI),row,"m_ChooseCharacterInit",0x04002EE2),
                    Initialized=ReadField<bool>(typeof(TempOptionUI),row,"m_IsInit",0x04002EE3),Disabled=ReadField<bool>(typeof(TempOptionUI),row,"m_Disabled",0x04002EE4),
                    Interchapter=ReadField<bool>(typeof(TempOptionUI),row,"m_Interchapter",0x04002EE8),ActiveAndEnabled=row.isActiveAndEnabled,ActiveInHierarchy=row.gameObject!=null&&row.gameObject.activeInHierarchy};
            }
            // Pages are rebuilt behind a fade: until SetPage ran, the previous page's initialized options are
            // still visible against the new offers. Such rows are a pending page, never a foreign offer.
            var pending=rows.Length==0;
            foreach(var row in result.Rows){var offered=false;foreach(var offer in raw.Offered)if(offer!=null&&ReferenceEquals(offer.Answer,row.Answer))offered=true;if(row.Answer==null||!offered)pending=true;}
            result.UiStatus=pending?"book-page-pending":game.CurrentMode==GameModeType.Dialog&&game.IsModeActive(GameModeType.Dialog)&&result.ManagerShown==true&&active?
                Chunk7AssistedStoryDriver.SelectedStatus((int)kind):"inactive-dialog";
        }
        public void Click(Chunk7AssistedStorySnapshot expected,int offerIndex,int rowIndex,Action enteringNative)
        {
            if(enteringNative==null)throw new ArgumentNullException("enteringNative");
            var actual=ReadStory();Chunk7AssistedStoryDriver.RequireSameClickState(expected,actual);
            if(offerIndex<0||offerIndex>=actual.Offers.Length||rowIndex<0||rowIndex>=actual.Rows.Length)throw new InvalidOperationException("Selected story indices changed.");
            var offer=actual.Offers[offerIndex];var row=actual.Rows[rowIndex];
            if(!ReferenceEquals(offer.Answer,row.Answer)||!row.Initialized||row.Disabled||row.ChoosingCharacter||row.Interchapter&&actual.ManagerKind!=2||!row.ActiveAndEnabled||!row.ActiveInHierarchy||offer.SelectionType==4)
                throw new InvalidOperationException("The selected current row is not an ordinary native cached-enabled affordance.");
            // ReadStory just performed authority, global/controller, exact list,
            // row/cache/selection, frame and active hierarchy checks on this thread.
            enteringNative();((TempOptionUI)row.View).OnClick();
        }
        public void ClickSystemButton(Chunk7AssistedStorySnapshot expected,Action enteringNative)
        {
            if(enteringNative==null)throw new ArgumentNullException("enteringNative");
            var actual=ReadStory();Chunk7AssistedStoryDriver.RequireSameClickState(expected,actual);
            if(actual.Rows.Length!=0)throw new InvalidOperationException("A system continue is advanced only when no native row is present.");
            var systemButton=actual.SystemButton as DialogueSystemButton;
            if(systemButton==null||actual.SystemButtonEnabled!=true)throw new InvalidOperationException("The native DialogueSystemButton is absent or disabled; the UI continue cannot be bypassed.");
            var offerIndex=-1;
            for(var i=0;i<actual.Offers.Length;i++)if(ReferenceEquals(actual.Offers[i].Answer,actual.SystemButtonAnswer)){if(offerIndex>=0)throw new InvalidOperationException("The system continue button answer is ambiguous.");offerIndex=i;}
            if(offerIndex<0)throw new InvalidOperationException("The native system button answer is not an offered answer.");
            var offer=actual.Offers[offerIndex];
            if(!offer.IsSystemAnswer||offer.SelectionType==4||offer.OnSelectActionTypeCount!=0)throw new InvalidOperationException("The system button answer is not a non-Manual, action-free DialogRoot system answer.");
            foreach(var other in actual.Offers)if(!other.IsSystemAnswer)throw new InvalidOperationException("A genuine player choice is present; the system continue is not advanced.");
            // A terminal Exit system answer (identity: DialogRoot.ExitAnswer/InterchapterExitAnswer) dismisses the
            // finished narration through this same button; only a continue answer needs its displayed continue cue.
            if(!offer.IsExitAnswer&&actual.ContinueCue==null)throw new InvalidOperationException("A continue system answer without the displayed native continue cue is not advanced by assisted continue.");
            // ReadStory just re-verified authority, global/controller, exact lists, frame and the SystemButton
            // identity/enabled/answer custody on this thread. Advance the real UI affordance, never SelectAnswer directly.
            enteringNative();systemButton.OnClick();
        }
        internal Chunk7AssistedShortfallSnapshot ReadShortfall()
        {
            var story=ReadStory();var progression=main.Descriptor.Progression;
            var builder=game.UI==null?null:game.UI.CharacterBuildController;
            var result=new Chunk7AssistedShortfallSnapshot{Identity=story.Identity,Frame=story.Frame,Experience=progression.Experience,Level=progression.CharacterLevel,
                ExperienceRate=player.ExperienceRatePercent,TargetLevel=targetLevel,TargetThreshold=BlueprintRoot.Instance.Progression.XPTable.GetBonus(targetLevel),
                Money=player.Money,NativeHireCost=player.GetCustomCompanionCost(),ReadyDefault=story.Mode=="Default"&&!story.DialogActive&&!story.NativeBusy&&!story.Cutscene&&!story.TransitionPending&&!story.UnexpectedPopup,
                LivingIdleField=main.IsInState&&main.IsInGame&&main.IsDirectlyControllable&&main.View!=null&&main.View.AgentASP!=null&&main.Descriptor.State.IsConscious&&!main.Descriptor.State.IsDead&&main.Commands.Empty&&!main.IsInCombat,
                IsPet=main.Descriptor.Master.Value!=null,HasPet=main.Descriptor.Pet!=null,HasDualCompanion=main.Get<UnitPartDualCompanion>()!=null,
                ForeignBuilder=builder==null||builder.LevelUpController!=null||builder.Unit!=null,PreservedStateSha256=preservedStateSha256()};
            if(Time.frameCount!=story.Frame)throw new InvalidOperationException("Native assistance frame changed during capture.");return result;
        }
        public void GainExperience(Chunk7AssistedShortfallSnapshot expected,int delta,Action enteringNative)
        {
            var actual=ReadShortfall();Chunk7AssistedShortfall.RequireSameInput(expected,actual);
            if(delta<=0||delta>Chunk7AssistedShortfall.MaximumExperienceShortfall||actual.ExperienceRate!=100||actual.IsPet||actual.HasPet||actual.HasDualCompanion||
                checked(actual.Experience+checked(delta*100)/100)!=actual.TargetThreshold)throw new InvalidOperationException("Native XP shortfall entry differs from the exact plan.");
            if(enteringNative==null)throw new ArgumentNullException("enteringNative");enteringNative();main.Descriptor.Progression.GainExperience(delta,false);
        }
        public void GainMoney(Chunk7AssistedShortfallSnapshot expected,long delta,Action enteringNative)
        {
            var actual=ReadShortfall();Chunk7AssistedShortfall.RequireSameInput(expected,actual);
            if(delta<=0||delta>Chunk7AssistedShortfall.MaximumMoneyShortfall||actual.NativeHireCost<=0||actual.Level<4||checked(actual.Money+delta)!=actual.NativeHireCost)
                throw new InvalidOperationException("Native money shortfall entry differs from the exact observed hire cost.");
            if(enteringNative==null)throw new ArgumentNullException("enteringNative");enteringNative();player.GainMoney(delta);
        }
        private static T ReadField<T>(Type type,object owner,string name,int token)
        {
            RequireModule(type);var f=type.GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
            if(f==null||f.MetadataToken!=token||f.FieldType!=typeof(T)||f.IsStatic)throw new InvalidOperationException("Installed assisted fixture field changed: "+name);return (T)f.GetValue(owner);
        }
        internal static void RequireMethod(Type type,string name,int token,Type returns,Type[] parameters)
        {
            RequireModule(type);var m=type.GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,parameters,null);
            if(m==null||m.MetadataToken!=token||m.ReturnType!=returns||m.IsStatic||m.IsVirtual)throw new InvalidOperationException("Installed assisted fixture method changed: "+name);
        }
        private static void RequireModule(Type t){if(t.Module.ModuleVersionId!=new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7"))throw new InvalidOperationException("Installed assisted fixture module changed.");}
    }
}
