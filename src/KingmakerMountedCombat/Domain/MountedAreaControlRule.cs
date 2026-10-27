namespace KingmakerMountedCombat.Domain
{
    // Native Kingmaker 2.1.7b UnitEntityData.IsDirectlyControllable: in a capital area only the
    // main character and units whose Master is the main character are directly controllable.
    // A saved pair refused on area arrival is retired (dismounted) only when that native rule is
    // the whole reason; every other refusal keeps the all-pair failure.
    internal static class MountedAreaControlRule
    {
        internal const string ControlRefusal = "Both units must be directly controllable.";

        internal static bool IsCapitalControlRetirement(bool areaIsCapital, string refusal, string refusalWithControlAssumed,
            bool riderControllable, bool riderIsMainOrMainPet, bool mountControllable, bool mountIsMainOrMainPet)
        {
            if (!areaIsCapital || refusal != ControlRefusal || refusalWithControlAssumed != null) return false;
            if (riderControllable && mountControllable) return false;
            // Each uncontrollable unit must be explained by the capital rule, and a unit the rule
            // admits (main character or main's pet) must still be controllable.
            return (riderControllable ? true : !riderIsMainOrMainPet) && (mountControllable ? true : !mountIsMainOrMainPet) &&
                (!riderIsMainOrMainPet || riderControllable) && (!mountIsMainOrMainPet || mountControllable);
        }
    }
}
