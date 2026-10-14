using System;
namespace KingmakerMountedCombat.Diagnostics
{
    // Created only by the selected isolated fresh-campaign host factory. This
    // immutable record is an input to guards, not a replacement for that factory.
    internal sealed class Chunk7AssistedFixtureBinding
    {
        internal const string Scenario = "chunk7-assisted-fixture-preparation";
        internal readonly object Game, Player, SaveManager, Main, Descriptor, Authority;
        internal readonly string RunId, CampaignId, CampaignName, MainId;
        internal readonly int Generation;
        internal Chunk7AssistedFixtureBinding(string runId, string campaignId, string campaignName, string mainId,
            object game, object player, object saveManager, object main, object descriptor, object authority, int generation)
        {
            RequireText(runId, 128); RequireText(campaignId, 128); RequireText(campaignName, 256); RequireText(mainId, 128);
            Guid value; if (!Guid.TryParse(campaignId, out value) || value == Guid.Empty) throw new ArgumentException("A native-minted campaign identity is required.");
            if (game == null || player == null || saveManager == null || main == null || descriptor == null || authority == null || generation <= 0)
                throw new ArgumentException("An exact fresh-campaign authority and world are required.");
            RunId=runId; CampaignId=campaignId; CampaignName=campaignName; MainId=mainId;
            Game=game; Player=player; SaveManager=saveManager; Main=main; Descriptor=descriptor; Authority=authority; Generation=generation;
        }
        internal void Require(Chunk7AssistedIdentity current)
        {
            if(current==null || !ReferenceEquals(Game,current.Game) || !ReferenceEquals(Player,current.Player) ||
                !ReferenceEquals(SaveManager,current.SaveManager) || !ReferenceEquals(Main,current.Main) ||
                !ReferenceEquals(Descriptor,current.Descriptor) || !ReferenceEquals(Authority,current.Authority) ||
                RunId!=current.RunId || CampaignId!=current.CampaignId || CampaignName!=current.CampaignName || MainId!=current.MainId ||
                Generation!=current.Generation || !current.ExactIsolatedFreshCampaign || !current.NoForeignOrMountedCustody)
                throw new InvalidOperationException("Assisted fixture lost its exact isolated run, minted campaign or main actor.");
        }
        private static void RequireText(string value,int cap)
        { if(string.IsNullOrWhiteSpace(value)||value.Length>cap)throw new ArgumentException("A bounded exact identity is required."); }
    }
    internal sealed class Chunk7AssistedIdentity
    {
        internal object Game,Player,SaveManager,Main,Descriptor,Authority;
        internal string RunId,CampaignId,CampaignName,MainId;
        internal int Generation;
        internal bool ExactIsolatedFreshCampaign,NoForeignOrMountedCustody;
    }
}
