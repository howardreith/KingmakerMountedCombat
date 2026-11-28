namespace KingmakerMountedCombat.Domain
{
    public enum PairedAttackClickAdmission
    {
        // Native IgnoreClick's own result stands.
        NativeDecision,
        // The mounted strike is already admissible from where the pair stands: no movement is involved.
        AdmitInRange,
        // A proven executable route of the mount's own agent (native Complete, destination within the mount's granted movement,
        // proven arrival) ends where the strike is admissible.
        AdmitRoute
    }

    // MMR-02 (reviews R1-F3, R3-F2). Native TurnController.IgnoreClick (2.1.7b, 06000C2F) checks the predicted rider
    // UnitAttack's own approach radius (rider corpulence + target corpulence + reach) against the visualized path, which under
    // paired controls is the MOUNT's path (KMC's PairedPathUnitTranspiler), and against the rider's position when that path is
    // empty. The strike KMC executes for that order is admitted from the mount origin (mount corpulence + target corpulence +
    // planned weapon range, with the native command line of sight). This decides when KMC can prove a dropped click legal by
    // its own rule: only for the exact predicted plain melee UnitAttack by the selected, addressable rider on the highlighted
    // hostile, with the rider's attack admitted by the dispatcher, and either (a) no movement needed, or (b) a complete native
    // route of the mount's own agent whose cost is within the mount's actual granted movement for the current step mode and
    // whose endpoint admits the strike. Everything else keeps native's refusal, which leaves the previous order untouched.
    public static class PairedAttackClickAdmissionPolicy
    {
        // PR #70 R1-F1: only a PROVEN executable route authorizes movement (native Complete outcome, a destination with a proven
        // arrival radius, cost measured along the route to that destination) and its cost must be within the mount's grant, with no
        // tolerance: a diagnostic distance (for example the unbounded walk's reach beyond the grant) never authorizes anything.
        public static bool RouteWithinGrant(bool routeProven, float routeMetres, float mountGrantedMetres) =>
            routeProven && !float.IsNaN(routeMetres) && !float.IsNaN(mountGrantedMetres) && mountGrantedMetres > 0f && routeMetres <= mountGrantedMetres;

        public static PairedAttackClickAdmission Decide(
            bool nativeIgnored,
            bool riderBoundaryContext,
            bool riderAddressable,
            bool riderSelected,
            bool riderCommandsEmpty,
            bool predictionsCurrent,
            bool overTerrain,
            bool abilitySelected,
            bool predictedMeleeAttackOnHighlightedHostile,
            bool riderAttackAdmitted,
            bool strikeAdmissibleNow,
            bool routeComplete,
            bool routeProven,
            float routeMetres,
            float mountGrantedMetres,
            out string reason)
        {
            reason = null;
            if (!nativeIgnored) { reason = "native-accepted"; return PairedAttackClickAdmission.NativeDecision; }
            if (!riderBoundaryContext || !riderAddressable || !riderSelected || !riderCommandsEmpty || !predictionsCurrent ||
                overTerrain || abilitySelected)
            {
                reason = "not-rider-attack-input";
                return PairedAttackClickAdmission.NativeDecision;
            }
            if (!predictedMeleeAttackOnHighlightedHostile) { reason = "not-melee-attack-prediction"; return PairedAttackClickAdmission.NativeDecision; }
            if (!riderAttackAdmitted) { reason = "rider-attack-refused"; return PairedAttackClickAdmission.NativeDecision; }
            if (strikeAdmissibleNow) { reason = "in-range"; return PairedAttackClickAdmission.AdmitInRange; }
            if (!routeComplete) { reason = "no-complete-route"; return PairedAttackClickAdmission.NativeDecision; }
            if (!routeProven) { reason = "no-proven-route"; return PairedAttackClickAdmission.NativeDecision; }
            if (!RouteWithinGrant(routeProven, routeMetres, mountGrantedMetres))
            {
                reason = "insufficient-movement";
                return PairedAttackClickAdmission.NativeDecision;
            }
            reason = "route-within-grant";
            return PairedAttackClickAdmission.AdmitRoute;
        }
    }
}
