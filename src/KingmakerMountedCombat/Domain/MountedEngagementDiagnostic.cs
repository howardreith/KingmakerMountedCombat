using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // MMR-04 owner-log evidence (reviews R1-F11, R2, R3). Where the pair's real-time AUTOMATIC engagement first stopped, for
    // the first automatic order of a combat, read once a few seconds after the combat window opened. The window opens at the
    // first of: the first real-time combat update or the first automatic proposal (a proposal can precede the first update).
    // Proposal, refusal, dispatch and mount displacement are attributed to the same order (its intent generation): the mount's
    // movement counts as the order's own only while that order's own pair command or delegated move is live and no player
    // operation is; movement under a player operation is reported separately and never as automatic progress. The target is
    // the rider brain's own native proposal; the line carries native's ranking for that exact proposal.
    public sealed class MountedEngagementRecord
    {
        public const double ReportAfterSeconds = 3d;
        public const float DisplacementEpsilonMetres = 0.25f;

        private double openedAt;

        public bool IsOpen { get; private set; }
        public bool Reported { get; private set; }
        public long Proposals { get; private set; }
        public long OrderGeneration { get; private set; } = -1;
        public string ProposalTarget { get; private set; }
        // Review R5-F8: the target the pair selected (the equal-rank tie-break can differ from native's proposal).
        public string SelectedTarget { get; private set; }
        public string ProposalRanking { get; private set; }
        public string FirstRefusal { get; private set; }
        public bool Dispatched { get; private set; }
        public float OwnDisplacementMetres { get; private set; }
        public float PlayerDisplacementMetres { get; private set; }
        public bool PlayerOperationBeforeDispatch { get; private set; }

        public void Reset()
        {
            IsOpen = false; Reported = false; Proposals = 0; OrderGeneration = -1; ProposalTarget = null; SelectedTarget = null; ProposalRanking = null;
            FirstRefusal = null; Dispatched = false; OwnDisplacementMetres = 0f; PlayerDisplacementMetres = 0f;
            PlayerOperationBeforeDispatch = false; openedAt = 0d;
        }

        public void Open(double nowSeconds)
        {
            if (IsOpen) return;
            Reset();
            IsOpen = true;
            openedAt = nowSeconds;
        }

        // Every automatic proposal of the window. The first admitted one is the order this record follows.
        public void ObserveProposal(double nowSeconds, string proposed, string selected, string ranking, bool admitted, long generation, string refusal)
        {
            Open(nowSeconds);
            if (Reported) return;
            Proposals++;
            if (ProposalTarget == null || OrderGeneration < 0 && admitted)
            {
                ProposalTarget = proposed;
                SelectedTarget = selected;
                ProposalRanking = ranking;
            }
            if (admitted && OrderGeneration < 0) OrderGeneration = generation;
            if (!admitted && FirstRefusal == null) FirstRefusal = refusal ?? "<unknown>";
        }

        public void ObserveDispatch(long generation)
        {
            if (IsOpen && !Reported && generation >= 0 && generation == OrderGeneration) Dispatched = true;
        }

        // Review R1-F11: one sampling interval of mount movement. It is the order's own travel only when the SAME exact carrier
        // (the order's delegated move, or its own approaching command) owned the mount's movement at both ends of the
        // interval; it is player travel only when a player operation was live at both ends. Any change of owner credits
        // nothing for that interval, so the next interval starts from a fresh baseline.
        public void ObserveInterval(float metres, object carrierAtStart, object carrierAtEnd, bool playerAtStart, bool playerAtEnd)
        {
            if (!IsOpen || Reported || metres <= 0f) return;
            if (playerAtStart && playerAtEnd) PlayerDisplacementMetres += metres;
            else if (!playerAtStart && !playerAtEnd && carrierAtStart != null && ReferenceEquals(carrierAtStart, carrierAtEnd))
                OwnDisplacementMetres += metres;
        }

        public void ObservePlayerOperation()
        {
            if (IsOpen && !Reported && !Dispatched) PlayerOperationBeforeDispatch = true;
        }

        public bool Due(double nowSeconds) => IsOpen && !Reported && nowSeconds - openedAt >= ReportAfterSeconds;

        public string FirstStop() =>
            PlayerOperationBeforeDispatch ? "player-command" :
            Dispatched && OwnDisplacementMetres >= DisplacementEpsilonMetres ? "dispatched-moved" :
            Dispatched ? "dispatched-not-moved" :
            Proposals == 0 ? "no-proposal" :
            OrderGeneration >= 0 ? "accepted-not-dispatched" :
            "refused:" + (FirstRefusal ?? "<unknown>");

        public string Report()
        {
            Reported = true;
            return "firstStop=" + FirstStop() + "; order=" + OrderGeneration + "; proposals=" + Proposals +
                "; proposed=" + (ProposalTarget ?? "<none>") + "; selected=" + (SelectedTarget ?? "<none>") + "; nativeRanking=" + (ProposalRanking ?? "<none>") +
                "; ownDisplacement=" + OwnDisplacementMetres.ToString("R", CultureInfo.InvariantCulture) +
                "; playerDisplacement=" + PlayerDisplacementMetres.ToString("R", CultureInfo.InvariantCulture);
        }
    }

    public struct NativeRankedCandidate
    {
        public object Unit;
        public float Weight;
        public float Distance;
        public bool Eligible;
    }

    // MMR-04 (review R1-F11): native's TargetInfo.Sort orders by weight only and List.Sort is unstable, so candidates with the
    // same weight (in reach: 0, or +1 for the last target) arrive in arbitrary order and the first one with the full score
    // wins. Among the candidates native ranked EQUAL to its proposal and that native would score equally (Eligible), the
    // mounted pair takes the nearest; a live automatic order already on one of them keeps it. Different weights (manual
    // target, out-of-reach distance) are never reordered.
    public static class NativeAttackTieBreak
    {
        // Review R5-F7: applies only to the supported rider MELEE proposal; a ranged (mounted archery) proposal keeps native's choice.
        public static object Choose(System.Collections.Generic.IList<NativeRankedCandidate> ranked, object proposed, object currentOrderTarget,
            bool riderMeleeProposal)
        {
            if (!riderMeleeProposal || ranked == null || proposed == null) return proposed;
            var found = false; var weight = 0f; var proposedDistance = 0f; var proposedEligible = false;
            foreach (var candidate in ranked)
                if (ReferenceEquals(candidate.Unit, proposed))
                {
                    found = true; weight = candidate.Weight; proposedDistance = candidate.Distance; proposedEligible = candidate.Eligible; break;
                }
            if (!found || ReferenceEquals(currentOrderTarget, proposed)) return proposed;
            // PR-1 review R7-F1: the proposal is the distance to beat only when it is itself eligible; an ineligible proposal yields to the
            // nearest eligible equal-ranked candidate, however far (if none is eligible the proposal is returned and refused as before).
            var best = proposed;
            var bestDistance = proposedEligible ? proposedDistance : float.PositiveInfinity;
            foreach (var candidate in ranked)
            {
                if (candidate.Unit == null || !SameRank(candidate.Weight, weight, currentOrderTarget) || ReferenceEquals(candidate.Unit, proposed) ||
                    !candidate.Eligible) continue;
                if (currentOrderTarget != null && ReferenceEquals(candidate.Unit, currentOrderTarget)) return currentOrderTarget;
                if (candidate.Distance < bestDistance) { best = candidate.Unit; bestDistance = candidate.Distance; }
            }
            return best;
        }

        // PR-1 review R1-F10: native weights an in-reach candidate 0, or 1 when it is the rider's LastTarget. That bonus is not a
        // current player target (a manual target weighs +1000000) nor a live order (currentOrderTarget); on fresh acquisition the
        // in-reach weights 0 and 1 therefore rank equal and the nearest eligible enemy is taken.
        private static bool SameRank(float candidate, float proposal, object currentOrderTarget) =>
            candidate == proposal || currentOrderTarget == null && InReach(candidate) && InReach(proposal);

        private static bool InReach(float weight) => weight == 0f || weight == 1f;
    }

    // MMR-04 (review R1-F11): the installed native comparison that produces the rider brain's automatic attack proposal,
    // traced from Kingmaker 2.1.7b and pinned by scripts/Test-MountedAlpha2NativeContract.ps1. This is a MODEL of that code
    // for documentation and offline consequence checks, not the production selector (KMC selects nothing; the live proposal and
    // native's own ranking for it are logged by the controller). Rules, in native order:
    //  1. Candidates (DecisionContext.Enemies): the rider group's memory enemies, minus dead, untargetable, aggro-filtered
    //     and (for the party) conscious enemies whose own group has no player character in memory (unaware enemies); a live
    //     manual target is added if missing.
    //  2. Sort (TargetInfo.FillWeight, descending): manual target +1000000; inside action range (+corpulences) +1 if it is
    //     the LastTarget, else 0; outside action range in real time -0.001 x distance.
    //  0. Character_Brain lists ManualAttackAiAction first (BaseScore 10; actor HasManualTarget 1/0; target ManualOnly
    //     1/0/0; UseWhenAIDisabled): a live manual target that passes IsTargetValid scores 10 and AttackAiAction (BaseScore 1)
    //     is then skipped, so an explicit player target is kept whatever else is nearer or already in reach.
    //  3. AiBrainController.SelectAction walks that order; AiAction.ScoreTarget: BlueprintAiAction.IsTargetValid (when the
    //     rider's AiMovementForbidden, or the target is in AIData.UnreachableUnits, only an in-range target in LOS is valid),
    //     then AttackAiAction's TargetConsiderations = AttackTargetsPriority (ComplexConsideration, multiplied):
    //     StandStill (InRangeConsideration 1.0 / 0.8), UnconsciousPenalty (LifeStateConsideration alive 1, dead 0,
    //     unconscious 0.001), NotImpatient (NotImpatientConsideration disliked 0.5 / 1.0).
    //  4. The first candidate with a strictly higher score wins; a candidate scoring the full actor score ends the walk.
    public struct NativeAttackCandidate
    {
        public string Id;
        public float Distance;
        public bool InActionRange;
        public bool ManualTarget;
        public bool LastTarget;
        public bool Excluded;          // rule 1
        public bool Unreachable;       // in AIData.UnreachableUnits
        public bool InLineOfSight;
        public bool Unconscious;
        public bool Disliked;
    }

    public static class NativeAttackTargetChoiceModel
    {
        public static float Weight(NativeAttackCandidate candidate, bool turnBased)
        {
            var weight = candidate.ManualTarget ? 1000000f : 0f;
            if (candidate.InActionRange) { if (candidate.LastTarget) weight += 1f; }
            else if (!turnBased) weight += candidate.Distance * -0.001f;
            return weight;
        }

        public static float Score(NativeAttackCandidate candidate, bool riderMovementForbidden)
        {
            if ((riderMovementForbidden || candidate.Unreachable) && (!candidate.InActionRange || !candidate.InLineOfSight)) return 0f;
            return (candidate.InActionRange ? 1f : 0.8f) * (candidate.Unconscious ? 0.001f : 1f) * (candidate.Disliked ? 0.5f : 1f);
        }

        public static string Choose(NativeAttackCandidate[] candidates, bool turnBased, bool riderMovementForbidden)
        {
            var eligible = new System.Collections.Generic.List<NativeAttackCandidate>();
            foreach (var candidate in candidates) if (!candidate.Excluded) eligible.Add(candidate);
            // Stage 0: ManualAttackAiAction.
            foreach (var candidate in eligible)
                if (candidate.ManualTarget && !candidate.Unconscious && Score(candidate, riderMovementForbidden) > 0f) return candidate.Id;
            // Stable for the model; native List.Sort is unstable among equal weights (in-range targets tie at 0).
            var ordered = new System.Collections.Generic.List<NativeAttackCandidate>(eligible);
            ordered.Sort((a, b) =>
            {
                var c = Weight(b, turnBased).CompareTo(Weight(a, turnBased));
                return c != 0 ? c : eligible.IndexOf(a).CompareTo(eligible.IndexOf(b));
            });
            string best = null; var bestScore = 0f;
            foreach (var candidate in ordered)
            {
                var score = Score(candidate, riderMovementForbidden);
                if (score > bestScore)
                {
                    bestScore = score; best = candidate.Id;
                    if (System.Math.Abs(bestScore - 1f) < 1e-6f) break;
                }
            }
            return best;
        }
    }
}
