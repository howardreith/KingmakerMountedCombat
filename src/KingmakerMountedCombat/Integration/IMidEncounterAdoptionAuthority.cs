using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // The paired lifecycle's half of the one combat-mount transaction.
    //
    // A voluntary combat Mount succeeds only when BOTH the relationship
    // attachment and the encounter adoption succeed. The relationship service
    // therefore plans the adoption before it commits, revalidates the plan
    // immediately before the commit, commits the adoption after it, and performs
    // exact compensating cleanup if that commit is refused. None of that is
    // expressible through an event whose handler's result is discarded, so the
    // authority is bound explicitly.
    internal interface IMidEncounterAdoptionAuthority
    {
        // False only when a required adoption cannot be planned. When there is
        // nothing to adopt — the paired lifecycle is not enabled — this returns
        // true with a null plan and the transition proceeds without adoption.
        bool TryPlanMidEncounterAdoption(
            UnitEntityData rider, UnitEntityData mount,
            out MidEncounterAdoptionPlan plan, out string refusal);

        // Exact field-by-field revalidation against freshly observed live state,
        // performed immediately before the relationship commit so that a change
        // refuses the transition while there is still nothing to compensate.
        bool RevalidateMidEncounterAdoptionPlan(
            MidEncounterAdoptionPlan plan, UnitEntityData rider, UnitEntityData mount, out string refusal);

        // Commits the planned adoption. Returns null on success, otherwise the
        // exact refusal. Every refusal path precedes the single native partner
        // preparation, so a refusal never leaves a prepared actor behind.
        string AdoptRunningEncounter(
            MidEncounterAdoptionPlan plan, UnitEntityData rider, UnitEntityData mount);

        // Removes KMC bookkeeping residue for an adoption that did not complete.
        // It writes no native resource and refunds no committed native action.
        void RollbackMidEncounterAdoption(string reason);
    }
}
