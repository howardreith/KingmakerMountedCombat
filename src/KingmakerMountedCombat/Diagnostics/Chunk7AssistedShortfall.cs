using System;
namespace KingmakerMountedCombat.Diagnostics
{
    internal interface IChunk7AssistedShortfallBoundary
    {
        Chunk7AssistedShortfallSnapshot Read();
        void GainExperience(Chunk7AssistedShortfallSnapshot expected,int delta,Action enteringNative);
        void GainMoney(Chunk7AssistedShortfallSnapshot expected,long delta,Action enteringNative);
    }
    internal sealed class Chunk7AssistedShortfallSnapshot
    {
        internal Chunk7AssistedIdentity Identity;
        internal int Frame,Experience,Level,ExperienceRate,TargetLevel,TargetThreshold,NativeHireCost;
        internal long Money;
        internal bool ReadyDefault,LivingIdleField,IsPet,HasPet,HasDualCompanion,ForeignBuilder;
        // The connected host supplies a bounded canonical observation of fields
        // that this XP/money-only step must preserve, including actor/reference
        // roster and native action/resource state. It must exclude XP/money only.
        internal string PreservedStateSha256;
    }
    internal sealed class Chunk7AssistedShortfallEffect
    {
        internal string Kind,Failure;internal long Delta;
        internal bool Attempted,NativeEntered,NativeReturned,AfterReadSucceeded,NoOp;
        internal Chunk7AssistedShortfallSnapshot Before,After;
    }
    // Setup assistance only. A successful result never establishes native ordinary
    // acquisition/progression or resource entitlement. No rollback/retry of a
    // possibly mutating native exception is attempted.
    internal sealed class Chunk7AssistedShortfall
    {
        internal const int MaximumTargetLevel=8,MaximumExperienceShortfall=200000;
        internal const long MaximumMoneyShortfall=100000;
        private readonly Chunk7AssistedFixtureBinding binding;
        private readonly IChunk7AssistedShortfallBoundary boundary;
        private bool xpUsed,moneyUsed,executing,failed;
        internal Chunk7AssistedShortfallEffect ExperienceEffect {get;private set;}
        internal Chunk7AssistedShortfallEffect MoneyEffect {get;private set;}
        internal bool Failed {get{return failed;}}
        internal Chunk7AssistedShortfall(Chunk7AssistedFixtureBinding binding,IChunk7AssistedShortfallBoundary boundary)
        {this.binding=binding??throw new ArgumentNullException("binding");this.boundary=boundary??throw new ArgumentNullException("boundary");}
        internal Chunk7AssistedShortfallEffect EnsureExperience(){return Apply(true);}
        internal Chunk7AssistedShortfallEffect EnsureOneNativeHireCost(){return Apply(false);}
        private Chunk7AssistedShortfallEffect Apply(bool experience)
        {
            if(failed)throw new InvalidOperationException("Assisted shortfall setup is already failed.");
            if(executing)throw new InvalidOperationException("Reentrant assistance is forbidden.");
            if(experience?xpUsed:moneyUsed)throw new InvalidOperationException("Assisted setup step cannot be repeated.");
            executing=true;
            var effect=new Chunk7AssistedShortfallEffect{Kind=experience?"native-xp-shortfall":"native-hire-money-shortfall"};
            if(experience){xpUsed=true;ExperienceEffect=effect;}else{moneyUsed=true;MoneyEffect=effect;}
            try
            {
                var before=boundary.Read();RequireSnapshot(before);effect.Before=before;
                long delta;
                if(experience)
                {
                    if(before.TargetLevel<4||before.TargetLevel>MaximumTargetLevel||before.TargetThreshold<0)
                        throw new InvalidOperationException("The exact bounded native XP-table target is unavailable.");
                    delta=before.TargetThreshold>before.Experience?checked(before.TargetThreshold-before.Experience):0;
                    if(delta>MaximumExperienceShortfall)throw new InvalidOperationException("Native XP shortfall exceeds the setup ceiling.");
                    if(delta>0&&(before.ExperienceRate!=100||before.IsPet||before.HasPet||before.HasDualCompanion))
                        throw new InvalidOperationException("XP assistance requires rate100 and one exact petless/non-dual main actor.");
                    checked {var scaled=(int)delta*before.ExperienceRate;var after=before.Experience+scaled/100;if(after!=before.Experience+delta)throw new InvalidOperationException("Native XP arithmetic would differ from the declared exact shortfall.");}
                }
                else
                {
                    if(before.Level<4||before.Level>MaximumTargetLevel||before.NativeHireCost<=0)
                        throw new InvalidOperationException("Paid hire requires observed attained level4..8 and a positive native cost.");
                    delta=before.NativeHireCost>before.Money?checked((long)before.NativeHireCost-before.Money):0;
                    if(delta>MaximumMoneyShortfall)throw new InvalidOperationException("Native funds shortfall exceeds the setup ceiling.");
                    checked {var after=before.Money+delta;if(after<before.Money)throw new InvalidOperationException("Native money arithmetic would overflow.");}
                }
                effect.Delta=delta;
                if(delta==0){effect.NoOp=true;effect.After=before;effect.AfterReadSucceeded=true;return effect;}
                effect.Attempted=true;
                Action entering=()=>{if(effect.NativeEntered)throw new InvalidOperationException("Native assistance entry is duplicated.");effect.NativeEntered=true;};
                if(experience)boundary.GainExperience(before,(int)delta,entering);else boundary.GainMoney(before,delta,entering);
                if(!effect.NativeEntered)throw new InvalidOperationException("Native assistance entry was not observed.");
                effect.NativeReturned=true;
                var afterRead=boundary.Read();RequireSnapshot(afterRead);effect.After=afterRead;effect.AfterReadSucceeded=true;
                RequirePreserved(before,afterRead,experience?checked(before.Experience+(int)delta):before.Experience,
                    experience?before.Money:checked(before.Money+delta));
                return effect;
            }
            catch(Exception error){failed=true;effect.Failure=Bound(error.GetType().Name+": "+error.Message);throw;}
            finally{executing=false;}
        }
        internal static void RequireSameInput(Chunk7AssistedShortfallSnapshot a,Chunk7AssistedShortfallSnapshot b)
        {if(a==null||b==null||a.Frame!=b.Frame)throw new InvalidOperationException("Assistance frame changed before native entry.");RequirePreserved(a,b,a.Experience,a.Money);}
        private void RequireSnapshot(Chunk7AssistedShortfallSnapshot s)
        {
            if(s==null)throw new InvalidOperationException("Assisted shortfall state is unavailable.");binding.Require(s.Identity);
            if(s.Frame<0||!s.ReadyDefault||!s.LivingIdleField||s.ForeignBuilder||s.Experience<0||s.Money<0||s.Level<1||s.Level>MaximumTargetLevel||
                s.ExperienceRate<=0||!HashShape(s.PreservedStateSha256))
                throw new InvalidOperationException("Assistance lacks an exact stable default living idle field and preserved-state observation.");
        }
        private static void RequirePreserved(Chunk7AssistedShortfallSnapshot a,Chunk7AssistedShortfallSnapshot b,int xp,long money)
        {
            if(a.Identity==null||b.Identity==null||!ReferenceEquals(a.Identity.Game,b.Identity.Game)||!ReferenceEquals(a.Identity.Player,b.Identity.Player)||
                !ReferenceEquals(a.Identity.Main,b.Identity.Main)||!ReferenceEquals(a.Identity.Descriptor,b.Identity.Descriptor)||!ReferenceEquals(a.Identity.SaveManager,b.Identity.SaveManager)||
                !ReferenceEquals(a.Identity.Authority,b.Identity.Authority)||a.Identity.Generation!=b.Identity.Generation||a.Identity.RunId!=b.Identity.RunId||a.Identity.CampaignId!=b.Identity.CampaignId||
                a.Identity.CampaignName!=b.Identity.CampaignName||a.Identity.MainId!=b.Identity.MainId||a.Level!=b.Level||a.ExperienceRate!=b.ExperienceRate||a.TargetLevel!=b.TargetLevel||
                a.TargetThreshold!=b.TargetThreshold||a.NativeHireCost!=b.NativeHireCost||a.PreservedStateSha256!=b.PreservedStateSha256||a.IsPet!=b.IsPet||a.HasPet!=b.HasPet||a.HasDualCompanion!=b.HasDualCompanion||
                !b.ReadyDefault||!b.LivingIdleField||b.ForeignBuilder||b.Experience!=xp||b.Money!=money)
                throw new InvalidOperationException("Native setup changed more than its exact declared XP or money shortfall.");
        }
        private static bool HashShape(string s){if(s==null||s.Length!=64)return false;foreach(var c in s)if(!(c>='0'&&c<='9')&&!(c>='a'&&c<='f'))return false;return true;}
        private static string Bound(string text){return text.Length>256?text.Substring(0,256):text;}
    }
}
