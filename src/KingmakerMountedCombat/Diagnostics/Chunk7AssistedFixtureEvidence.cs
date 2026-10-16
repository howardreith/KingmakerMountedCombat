using System;
using Newtonsoft.Json.Linq;
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class Chunk7AssistedFixtureEvidence
    {
        internal const string Contract="assisted-fixture-setup-v1";
        internal static JObject Capture(Chunk7AssistedFixtureBinding binding,Chunk7AssistedStoryDriver story,Chunk7AssistedShortfall shortfall)
        {
            if(binding==null)throw new ArgumentNullException("binding");var attempts=new JArray();
            if(story!=null)foreach(var a in story.Attempts)attempts.Add(new JObject{["sequence"]=a.Sequence,["offerIndex"]=a.OfferIndex,["rowIndex"]=a.RowIndex,["nativeIndex"]=a.NativeIndex,["systemButton"]=a.SystemButton,
                ["answer"]=a.Answer,["row"]=a.Row,["answerGuid"]=a.AnswerGuid,["nativeEntered"]=a.NativeEntered,["nativeReturned"]=a.NativeReturned,
                ["afterReadSucceeded"]=a.AfterReadSucceeded,["failure"]=a.Failure,["before"]=Story(a.Before),["after"]=Story(a.After)});
            return new JObject{["contract"]=Contract,["fixtureSetupOnly"]=true,["ordinaryAcquisitionProven"]=false,["ordinaryProgressionProven"]=false,["smallPoseQualified"]=false,
                ["runId"]=binding.RunId,["gameId"]=binding.CampaignId,["gameName"]=binding.CampaignName,["mainActorId"]=binding.MainId,["authorityGeneration"]=binding.Generation,
                ["story"]=new JObject{["policy"]="first-cached-enabled-common-answer",["cachedStateIsAuthorization"]=false,["objectLabelScope"]="story-driver-only",
                    ["maximumChoices"]=Chunk7AssistedStoryDriver.MaximumChoices,["maximumMilliseconds"]=Chunk7AssistedStoryDriver.MaximumMilliseconds,["progressMilliseconds"]=Chunk7AssistedStoryDriver.ProgressMilliseconds,
                    ["status"]=story==null?null:story.Status.ToString(),["failure"]=story==null?null:story.Failure,["nativeClickCount"]=story==null?0:story.NativeClickCount,["attemptCount"]=story==null?0:story.AttemptCount,
                    ["attempts"]=attempts,["lastObserved"]=Story(story==null?null:story.LastObserved)},
                ["experience"]=Effect(binding,shortfall==null?null:shortfall.ExperienceEffect),["hireFunds"]=Effect(binding,shortfall==null?null:shortfall.MoneyEffect),
                ["shortfallFailed"]=shortfall!=null&&shortfall.Failed};
        }
        private static JToken Story(Chunk7AssistedStoryEvidence s)
        {
            if(s==null)return JValue.CreateNull();return new JObject{["frame"]=s.Frame,["elapsedMilliseconds"]=s.ElapsedMilliseconds,["mode"]=s.Mode,["uiStatus"]=s.UiStatus,
                ["dialogGuid"]=s.DialogGuid,["cueGuid"]=s.CueGuid,["nativeBusy"]=s.NativeBusy,["transitionPending"]=s.TransitionPending,["dialogActive"]=s.DialogActive,["unexpectedPopup"]=s.UnexpectedPopup,
                ["mouseController"]=s.MouseController,["cutscene"]=s.Cutscene,["mainViewReady"]=s.MainViewReady,["managerKind"]=s.ManagerKind,["managerShown"]=s.ManagerShown,["pcActive"]=s.PcActive,
                ["controller"]=s.Controller,["dialog"]=s.Dialog,["cue"]=s.Cue,["ui"]=s.Ui,["common"]=s.Common,["manager"]=s.Manager,["pcDialog"]=s.PcDialog,["part"]=s.Part,["offersList"]=s.OffersList,["rowsList"]=s.RowsList,
                ["identity"]=s.Identity==null?JValue.CreateNull():(JToken)new JObject{["runId"]=s.Identity.RunId,["gameId"]=s.Identity.CampaignId,["gameName"]=s.Identity.CampaignName,["mainActorId"]=s.Identity.MainId,["generation"]=s.Identity.Generation,
                    ["gameMatches"]=s.Identity.GameMatches,["playerMatches"]=s.Identity.PlayerMatches,["saveManagerMatches"]=s.Identity.SaveManagerMatches,["mainMatches"]=s.Identity.MainMatches,["descriptorMatches"]=s.Identity.DescriptorMatches,["authorityMatches"]=s.Identity.AuthorityMatches,
                    ["exactIsolatedFreshCampaign"]=s.Identity.ExactIsolatedFreshCampaign,["noForeignOrMountedCustody"]=s.Identity.NoForeignOrMountedCustody},
                ["offerObjects"]=new JArray(s.OfferObjects),["offerSelectionObjects"]=new JArray(s.OfferSelectionObjects),["offerGuids"]=new JArray(s.OfferGuids),["offerSelectionTypes"]=new JArray(s.OfferSelectionTypes),
                ["rowObjects"]=new JArray(s.RowObjects),["rowAnswerObjects"]=new JArray(s.RowAnswerObjects),["rowNativeIndices"]=new JArray(s.RowNativeIndices),
                ["rowInitialized"]=new JArray(s.RowInitialized),["rowDisabled"]=new JArray(s.RowDisabled),["rowChoosingCharacter"]=new JArray(s.RowChoosingCharacter),
                ["rowInterchapter"]=new JArray(s.RowInterchapter),["rowActiveAndEnabled"]=new JArray(s.RowActiveAndEnabled),["rowActiveInHierarchy"]=new JArray(s.RowActiveInHierarchy)};
        }
        private static JToken Effect(Chunk7AssistedFixtureBinding binding,Chunk7AssistedShortfallEffect e)
        {if(e==null)return JValue.CreateNull();return new JObject{["kind"]=e.Kind,["delta"]=e.Delta,["attempted"]=e.Attempted,["nativeEntered"]=e.NativeEntered,["nativeReturned"]=e.NativeReturned,["afterReadSucceeded"]=e.AfterReadSucceeded,["noOp"]=e.NoOp,["failure"]=e.Failure,["before"]=Shortfall(binding,e.Before),["after"]=Shortfall(binding,e.After)};}
        private static JToken Shortfall(Chunk7AssistedFixtureBinding b,Chunk7AssistedShortfallSnapshot s)
        {
            if(s==null)return JValue.CreateNull();var i=s.Identity;return new JObject{["frame"]=s.Frame,["experience"]=s.Experience,["level"]=s.Level,["experienceRatePercent"]=s.ExperienceRate,
                ["targetLevel"]=s.TargetLevel,["targetThreshold"]=s.TargetThreshold,["nativeHireCost"]=s.NativeHireCost,["money"]=s.Money,["readyDefault"]=s.ReadyDefault,["livingIdleField"]=s.LivingIdleField,
                ["isPet"]=s.IsPet,["hasPet"]=s.HasPet,["hasDualCompanion"]=s.HasDualCompanion,["foreignBuilder"]=s.ForeignBuilder,["preservedStateSha256"]=s.PreservedStateSha256,
                ["identity"]=i==null?JValue.CreateNull():(JToken)new JObject{["runId"]=i.RunId,["gameId"]=i.CampaignId,["gameName"]=i.CampaignName,["mainActorId"]=i.MainId,["generation"]=i.Generation,
                    ["gameMatches"]=ReferenceEquals(b.Game,i.Game),["playerMatches"]=ReferenceEquals(b.Player,i.Player),["saveManagerMatches"]=ReferenceEquals(b.SaveManager,i.SaveManager),
                    ["mainMatches"]=ReferenceEquals(b.Main,i.Main),["descriptorMatches"]=ReferenceEquals(b.Descriptor,i.Descriptor),["authorityMatches"]=ReferenceEquals(b.Authority,i.Authority),
                    ["exactIsolatedFreshCampaign"]=i.ExactIsolatedFreshCampaign,["noForeignOrMountedCustody"]=i.NoForeignOrMountedCustody}};
        }
    }
}
