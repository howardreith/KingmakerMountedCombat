using System;
using System.Linq;
using KingmakerMountedCombat.Diagnostics;
namespace KingmakerMountedCombat.Tests
{
    internal static class ManualDerivedAdmissionTests
    {
        private static int pass, fail;
        private const string MainId = "10000000-0000-0000-0000-000000000001", Target = "20000000-0000-0000-0000-000000000002";
        private static readonly string Hash = new string('a', 64);
        private static Chunk7ManualDerivedSelection Existing() => new Chunk7ManualDerivedSelection {
            Case = Chunk7ManualDerivedSelection.ExistingCase, SourceRunId = "completed-manual", SourceArtifactSha256 = Hash, SourceArchiveSha256 = new string('b',64),
            BaseGameOnly = true, ExpectedNativeLoadCount = 1, ExpectedNativeWriteCount = 1, NativeWriteLeaf = "Auto_1.zks",
            Readiness = new Chunk7ManualDerivedReadinessPins { MainActorId = MainId, MainLevel = 1, MainExperience = 0, Money = 10, ExperienceRatePercent = 100,
                NativeHireCost = 50, DefaultHireExperience = 0, PartyCharacterIds = new[] { MainId, Target } },
            ExistingActorId = Target, ExistingLevel = 1, ExistingExperience = 400, ExistingRangerLevel = 0 };
        private static Chunk7ManualDerivedSelection Paid()
        {
            var selection = Existing(); selection.Case = Chunk7ManualDerivedSelection.PaidSmallCase;
            selection.ExistingActorId = null; selection.ExistingLevel = selection.ExistingExperience = selection.ExistingRangerLevel = null;
            selection.SmallRaceGuid = new string('c',32); selection.SmallPresetGuid = new string('d',32); selection.SkeletonName = "observed-native-small";
            selection.Gender = "Female"; selection.LeftHanded = false; selection.Readiness.MainLevel = 4; selection.Readiness.MainExperience = 300;
            selection.Readiness.DefaultHireExperience = 300; selection.Readiness.Money = 50; return selection;
        }
        private static Chunk7ManualDerivedSelection Medium()
        {
            var selection = Paid(); selection.Case = Chunk7ManualDerivedSelection.PaidMediumCase;
            selection.MediumBody = new Chunk7MediumHireBodyPins { TemplateActorId = MainId, RaceGuid = new string('e', 32), PresetGuid = new string('f', 32),
                SkeletonName = "observed-native-medium", Gender = "Male", LeftHanded = true };
            selection.SmallRaceGuid = selection.SmallPresetGuid = selection.SkeletonName = selection.Gender = null; selection.LeftHanded = null;
            return selection;
        }
        private static void RejectMedium(string name, Action<Chunk7ManualDerivedSelection,Chunk7ManualDerivedNativeReadiness> mutate)
        { var s = Medium(); var n = Native(s); mutate(s,n); Check(name,!Admits(s,n)); }
        private static Chunk7ManualDerivedNativeReadiness Native(Chunk7ManualDerivedSelection selection) => new Chunk7ManualDerivedNativeReadiness {
            MainActorId = selection.Readiness.MainActorId, MainLevel = selection.Readiness.MainLevel, MainExperience = selection.Readiness.MainExperience,
            Money = selection.Readiness.Money, ExperienceRatePercent = selection.Readiness.ExperienceRatePercent, NativeHireCost = selection.Readiness.NativeHireCost,
            DefaultHireExperience = selection.Readiness.DefaultHireExperience, PartyCharacterIds = selection.Readiness.PartyCharacterIds.ToArray(),
            XpThresholds = Enumerable.Range(0,21).Select(level => level <= 1 ? 0 : (level-1)*100).ToArray(),
            ExistingActorId = selection.ExistingActorId, ExistingLevel = selection.ExistingLevel ?? 0, ExistingExperience = selection.ExistingExperience ?? 0,
            ExistingRangerLevel = selection.ExistingRangerLevel ?? 0, TargetLiving = true, TargetIsPet = false, TargetHasPet = false, TargetHasDualCompanion = false,
            TargetInField = true, TargetIdle = true, TargetCanLevelUp = true, ExactSmallCatalogueMember = true, ExactMediumBodyMember = true };
        private static void Check(string name, bool value) { if(value) {pass++;Console.WriteLine("PASS "+name);} else {fail++;Console.WriteLine("FAIL "+name);} }
        private static bool Admits(Chunk7ManualDerivedSelection selection, Chunk7ManualDerivedNativeReadiness actual) => Chunk7ManualDerivedAdmission.Validate(selection,"new-run",actual).Count == 0;
        private static void Reject(string name, Action<Chunk7ManualDerivedSelection,Chunk7ManualDerivedNativeReadiness> mutate, bool paid = false)
        { var s=paid?Paid():Existing();var n=Native(s);mutate(s,n);Check(name,!Admits(s,n)); }
        public static int MainMethod()
        {
            var s=Existing();var n=Native(s);Check("eligible pet-free owner needs no hire funds",Admits(s,n));
            s=Paid();n=Native(s);Check("exact paid Small boundary at actual cost admits",Admits(s,n));
            s=Existing();n=Native(s);Array.Reverse(n.PartyCharacterIds);Check("native roster order is not membership authority",Admits(s,n));
            s=Existing();s.Readiness.PartyCharacterIds=new[]{MainId,Target,"30000000-0000-0000-0000-000000000003","40000000-0000-0000-0000-000000000004","50000000-0000-0000-0000-000000000005","60000000-0000-0000-0000-000000000006"};n=Native(s);
            Check("existing acquisition does not need a seventh field slot",Admits(s,n));
            s=Existing();s.ExistingLevel=3;s.ExistingRangerLevel=2;s.ExistingExperience=400;n=Native(s);Check("remaining native Ranger levels account for existing class rank",Admits(s,n));
            s=Existing();s.ExistingActorId=MainId;s.ExistingExperience=400;s.Readiness.MainExperience=400;n=Native(s);Check("existing main is eligible for acquisition without claiming removable progression",Admits(s,n));
            Reject("unearned XP refuses",(a,b)=>{a.ExistingExperience=100;b.ExistingExperience=100;});
            Reject("unspent main XP cannot replace attained paid-hire level",(a,b)=>{a.Readiness.MainLevel=b.MainLevel=1;a.Readiness.MainExperience=b.MainExperience=1000;a.Readiness.DefaultHireExperience=b.DefaultHireExperience=0;},true);
            Reject("short native funds refuse",(a,b)=>{a.Readiness.Money=b.Money=49;},true);
            Reject("paid body must match actual catalogue",(a,b)=>b.ExactSmallCatalogueMember=false,true);
            Reject("paid full original party refuses",(a,b)=>{a.Readiness.PartyCharacterIds=new[]{MainId,Target,"30000000-0000-0000-0000-000000000003","40000000-0000-0000-0000-000000000004","50000000-0000-0000-0000-000000000005","60000000-0000-0000-0000-000000000006"};b.PartyCharacterIds=a.Readiness.PartyCharacterIds.ToArray();},true);
            Reject("source XP drift refuses",(a,b)=>b.ExistingExperience++);
            Reject("source class drift refuses",(a,b)=>b.ExistingRangerLevel++);
            Reject("source main XP drift refuses",(a,b)=>b.MainExperience++);
            Reject("source money drift refuses",(a,b)=>b.Money++);
            Reject("source attained main drift refuses",(a,b)=>b.MainLevel++);
            Reject("source native cost drift refuses",(a,b)=>b.NativeHireCost++);
            Reject("source default hire XP drift refuses",(a,b)=>b.DefaultHireExperience++);
            Reject("source actor replacement refuses",(a,b)=>b.ExistingActorId=MainId);
            Reject("source main replacement refuses",(a,b)=>b.MainActorId=Target);
            Reject("duplicate live roster refuses",(a,b)=>b.PartyCharacterIds=new[]{MainId,MainId});
            Reject("foreign live roster refuses",(a,b)=>b.PartyCharacterIds=new[]{MainId,"90000000-0000-0000-0000-000000000009"});
            Reject("missing native table refuses",(a,b)=>b.XpThresholds=null);
            Reject("short native table refuses",(a,b)=>b.XpThresholds=b.XpThresholds.Take(20).ToArray());
            Reject("descending native table refuses",(a,b)=>b.XpThresholds[4]=1);
            Reject("negative native threshold refuses",(a,b)=>b.XpThresholds[0]=-1);
            Reject("default XP must come from native attained level table",(a,b)=>{a.Readiness.DefaultHireExperience=b.DefaultHireExperience=20;});
            Reject("dead owner refuses",(a,b)=>b.TargetLiving=false);
            Reject("pet cannot be owner",(a,b)=>b.TargetIsPet=true);
            Reject("existing pet refuses duplicate acquisition",(a,b)=>b.TargetHasPet=true);
            Reject("dual companion requires a distinct selected route",(a,b)=>b.TargetHasDualCompanion=true);
            Reject("remote existing actor refuses",(a,b)=>b.TargetInField=false);
            Reject("busy existing actor refuses",(a,b)=>b.TargetIdle=false);
            Reject("native eligibility false is not replaced by arithmetic",(a,b)=>b.TargetCanLevelUp=false);
            Reject("unsupported campaign XP scaling refuses",(a,b)=>{a.Readiness.ExperienceRatePercent=b.ExperienceRatePercent=200;});
            Reject("repeat current run cannot qualify itself",(a,b)=>a.SourceRunId="new-run");
            Reject("foreign case refuses",(a,b)=>a.Case="manufacture-horse");
            Reject("write quota cannot expand",(a,b)=>a.ExpectedNativeWriteCount=2);
            Reject("load quota cannot expand",(a,b)=>a.ExpectedNativeLoadCount=2);
            Reject("foreign output leaf refuses",(a,b)=>a.NativeWriteLeaf="Auto_2.zks");
            Reject("installed-mods context cannot qualify base-only",(a,b)=>a.BaseGameOnly=false);
            Reject("source hash casing refuses",(a,b)=>a.SourceArtifactSha256=new string('A',64));
            Reject("selection cannot change existing body",(a,b)=>a.SmallRaceGuid=new string('c',32));
            Reject("paid cannot reuse existing actor identity",(a,b)=>a.ExistingActorId=Target,true);
            Reject("already past native Ranger boundary refuses",(a,b)=>{a.ExistingLevel=b.ExistingLevel=4;a.ExistingRangerLevel=b.ExistingRangerLevel=4;});
            Reject("noncanonical actor ID refuses",(a,b)=>a.ExistingActorId=Target.ToUpperInvariant()+" ");
            Reject("unsorted source pins refuse",(a,b)=>Array.Reverse(a.Readiness.PartyCharacterIds));
            Check("missing selection refuses",Chunk7ManualDerivedAdmission.Validate(null,"new-run",n).Count!=0);
            Check("missing native observations refuse",Chunk7ManualDerivedAdmission.Validate(Existing(),"new-run",null).Count!=0);
            Check("legacy family is not widened",Existing().Validate(Chunk7OrdinaryAcquisitionSelection.Scenario,"new-run").Count!=0);
            s=Existing();n=Native(s);var party=n.PartyCharacterIds.ToArray();var table=n.XpThresholds.ToArray();var xp=n.ExistingExperience;var money=n.Money;
            Admits(s,n);Check("admission does not change XP funds roster or thresholds",n.ExistingExperience==xp&&n.Money==money&&n.PartyCharacterIds.SequenceEqual(party)&&n.XpThresholds.SequenceEqual(table));
            s=Medium(); n=Native(s); n.ExactSmallCatalogueMember=false; Check("observed Medium body admits without Small catalogue authority",Admits(s,n));
            RejectMedium("Medium source/native body disagreement refuses",(a,b)=>b.ExactMediumBodyMember=false);
            RejectMedium("Medium template must be native field character",(a,b)=>a.MediumBody.TemplateActorId="90000000-0000-0000-0000-000000000009");
            RejectMedium("Medium body pins required",(a,b)=>a.MediumBody=null);
            RejectMedium("Medium legacy body pins forbidden",(a,b)=>a.SmallRaceGuid=new string('c',32));
            RejectMedium("Medium legacy sex pin forbidden",(a,b)=>a.Gender="Male");
            RejectMedium("Medium legacy handedness forbidden",(a,b)=>a.LeftHanded=false);
            RejectMedium("Medium cannot reuse existing actor",(a,b)=>a.ExistingActorId=MainId);
            RejectMedium("Medium native funds remain required",(a,b)=>{a.Readiness.Money=b.Money=49;});
            RejectMedium("Medium attained level required",(a,b)=>{a.Readiness.MainLevel=b.MainLevel=1;a.Readiness.DefaultHireExperience=b.DefaultHireExperience=0;});
            RejectMedium("Medium blueprint casing refuses",(a,b)=>a.MediumBody.RaceGuid=new string('E',32));
            RejectMedium("Medium empty rig refuses",(a,b)=>a.MediumBody.SkeletonName=" ");
            RejectMedium("Medium unknown gender refuses",(a,b)=>a.MediumBody.Gender="Unknown");
            RejectMedium("Medium handedness required",(a,b)=>a.MediumBody.LeftHanded=null);
            RejectMedium("Medium foreign roster refuses",(a,b)=>b.PartyCharacterIds=new[]{MainId});
            s=Paid();n=Native(s);s.MediumBody=Medium().MediumBody;Check("legacy Small rejects Medium body authority",!Admits(s,n));
            s=Existing();n=Native(s);s.MediumBody=Medium().MediumBody;Check("existing actor rejects Medium body authority",!Admits(s,n));
            Check("legacy selection omits additive Medium JSON property",!Paid().ShouldSerializeMediumBody()&&!Existing().ShouldSerializeMediumBody());
            Check("Medium selection emits typed body property",Medium().ShouldSerializeMediumBody());
            Console.WriteLine("RESULT "+pass+" PASS / "+fail+" FAIL");return fail==0?0:1;
        }
        public static int Main(string[] args) => MainMethod();
    }
}
