using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    // Supplemental state missing from the native archive. Health, buffs, actor
    // objects, command instances, views and subscriptions remain native-owned.
    internal sealed class SavedCombatData
    {
        public bool TurnBased { get; set; }
        public int Round { get; set; }
        public long StartTicks { get; set; }
        public long RoundStartTicks { get; set; }
        public long TurnStartTicks { get; set; }
        public float TimeSinceStart { get; set; }
        public float TimeToNextRound { get; set; }
        public bool HasSurpriseRound { get; set; }
        public bool HasEnemy { get; set; }
        public bool HadEnemy { get; set; }
        public string NextActor { get; set; }
        public string BeforeCombatSelected { get; set; }
        public SavedCombatActor[] Actors { get; set; }
        public SavedRosterActor[] Roster { get; set; }
        public SavedEngagement[] Engagements { get; set; }
        public SavedTurnContext Current { get; set; }
        public SavedPairedState Paired { get; set; }
        public SavedMovementAllocation[] Allocations { get; set; }

        internal void Validate(long gameTicks)
        {
            SaveStateValidation.Require(Actors != null && Actors.Length > 0 && Actors.Length <= 64 &&
                Roster != null && Roster.Length <= 64 && Engagements != null && Engagements.Length <= 128 &&
                Allocations != null && Allocations.Length <= 64, "Combat collections are not bounded.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var nativeIds = new HashSet<Guid>();
            foreach (var actor in Actors)
            {
                SaveStateValidation.Require(actor?.Native != null, "Saved combat actor is absent.");
                actor.Native.Validate();
                SaveStateValidation.Require(ids.Add(actor.Native.Id) && nativeIds.Add(Guid.Parse(actor.Native.Id)),
                    "Duplicate combat actor identity.");
            }
            foreach (var actor in Actors) actor.Validate(ids, gameTicks);
            SaveStateValidation.ActorReference(NextActor, ids);
            SaveStateValidation.ActorReference(BeforeCombatSelected, ids);
            var rosterIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in Roster)
            {
                SaveStateValidation.Require(row != null && ids.Contains(row.ActorId) && rosterIds.Add(row.ActorId) &&
                    row.Sequence >= 0 && row.Sequence <= 1000000 &&
                    (!row.InitiativeOverride.HasValue || Math.Abs((long)row.InitiativeOverride.Value) <= 1000000),
                    "Roster identity or initiative ordering is invalid.");
            }
            SaveStateValidation.Require(Round >= 0 && Round <= 1000000 &&
                SaveStateValidation.PastTime(StartTicks, gameTicks) &&
                SaveStateValidation.PastTime(RoundStartTicks, gameTicks) &&
                SaveStateValidation.PastTime(TurnStartTicks, gameTicks) &&
                SaveStateValidation.Number(TimeSinceStart) && SaveStateValidation.Number(TimeToNextRound, 86400f),
                "Combat clock is invalid.");
            SaveStateValidation.Require(TurnBased ? Roster.Length > 0 :
                Roster.Length == 0 && Current == null && NextActor == null, "Combat mode and roster disagree.");
            Current?.Validate(ids);
            if (Current != null) SaveStateValidation.Require(rosterIds.Contains(Current.ActorId), "Current actor is outside roster.");
            if (NextActor != null) SaveStateValidation.Require(rosterIds.Contains(NextActor), "Next actor is outside roster.");
            var edges = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in Engagements)
                SaveStateValidation.Require(edge != null && ids.Contains(edge.From) && ids.Contains(edge.To) &&
                    edge.From != edge.To && edges.Add(edge.From + ":" + edge.To) &&
                    SaveStateValidation.PastTime(edge.SinceTicks, gameTicks), "Engagement identity or time is invalid.");
            var allocations = new HashSet<string>(StringComparer.Ordinal);
            foreach (var allocation in Allocations)
            {
                SaveStateValidation.Require(allocation != null && allocations.Add(allocation.ActorId),
                    "Duplicate movement allocation.");
                allocation.Validate(ids, gameTicks);
            }
            Paired?.Validate(ids, Current, gameTicks);
        }
    }

    internal sealed class SavedCombatActor
    {
        public SavedNativeActor Native { get; set; }
        public bool InCombat { get; set; }
        public bool Prepared { get; set; }
        public bool HitThisRound { get; set; }
        public int InitiativeRoll { get; set; }
        public int? CachedInitiative { get; set; }
        public int InitiativeRandom { get; set; }
        public int ExecutedAttacks { get; set; }
        public long LastMoveTicks { get; set; }
        public long LastDeflectTicks { get; set; }
        public long StoryImmunity { get; set; }
        public bool EnergyDrainImmunity { get; set; }
        public bool PreventNextReaction { get; set; }
        public float[] ReturnPoint { get; set; }
        public float? ReturnYaw { get; set; }
        public string[] DisengageTargets { get; set; }
        public SavedAiAction[] AiActions { get; set; }
        public long AiDelayTicks { get; set; }

        internal void Validate(HashSet<string> ids, long gameTicks)
        {
            SaveStateValidation.Require((!CachedInitiative.HasValue || Math.Abs((long)CachedInitiative.Value) <= 1000000) &&
                Math.Abs((long)InitiativeRoll) <= 1000000 &&
                Math.Abs((long)InitiativeRandom) <= int.MaxValue && ExecutedAttacks >= 0 && ExecutedAttacks <= 1024 &&
                SaveStateValidation.PastTime(LastMoveTicks, gameTicks) &&
                SaveStateValidation.PastTime(LastDeflectTicks, gameTicks) && StoryImmunity >= 0 &&
                AiDelayTicks >= 0 && AiDelayTicks <= TimeSpan.TicksPerDay &&
                DisengageTargets != null && DisengageTargets.Length <= 64 &&
                AiActions != null && AiActions.Length <= 64, "Combat actor state is invalid.");
            SaveStateValidation.Require((ReturnPoint == null || ReturnPoint.Length == 3 &&
                ReturnPoint.All(v => !float.IsNaN(v) && !float.IsInfinity(v) && Math.Abs(v) <= 1000000f)) &&
                (!ReturnYaw.HasValue || !float.IsNaN(ReturnYaw.Value) && !float.IsInfinity(ReturnYaw.Value) &&
                Math.Abs(ReturnYaw.Value) <= 1000000f), "Native combat return position is invalid.");
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var target in DisengageTargets)
                SaveStateValidation.Require(target != Native.Id && ids.Contains(target) && targets.Add(target),
                    "Disengage obligation identifies an invalid actor.");
            var actions = new HashSet<string>(StringComparer.Ordinal);
            foreach (var action in AiActions)
                SaveStateValidation.Require(action != null && MountedSaveData.HexId(action.BlueprintId) &&
                    actions.Add(action.BlueprintId) && action.Cooldown >= 0 && action.Cooldown <= 1000000 &&
                    action.Count >= 0 && action.Count <= 1000000, "AI action obligation is invalid.");
        }
    }

    internal sealed class SavedAiAction
    {
        public string BlueprintId { get; set; }
        public int Cooldown { get; set; }
        public int Count { get; set; }
    }

    internal sealed class SavedEngagement
    {
        public string From { get; set; }
        public string To { get; set; }
        public long SinceTicks { get; set; }
    }

    internal sealed class SavedRosterActor
    {
        public string ActorId { get; set; }
        public bool InitiativeProcessed { get; set; }
        public bool Surprising { get; set; }
        public bool Surprised { get; set; }
        public bool ActingSurprise { get; set; }
        public int? InitiativeOverride { get; set; }
        public int Sequence { get; set; }
    }

    internal sealed class SavedMovementValues
    {
        public float TimeMoved { get; set; }
        public float TimeForced { get; set; }
        public float TimeStepped { get; set; }
        public float MetresStepped { get; set; }
        public bool StepImmune { get; set; }
        public bool AiStep { get; set; }
        public bool AutoStop { get; set; }

        internal void Validate()
        {
            SaveStateValidation.Require(SaveStateValidation.Number(TimeMoved, 86400f) &&
                SaveStateValidation.Number(TimeForced, 86400f) && SaveStateValidation.Number(TimeStepped, 86400f) &&
                SaveStateValidation.Number(MetresStepped, 1000000f) &&
                TimeForced <= TimeMoved + 0.001f && TimeStepped <= TimeMoved + 0.001f,
                "Saved movement or step commitment is invalid.");
        }
    }

    internal sealed class SavedTurnContext
    {
        public string ActorId { get; set; }
        public int Status { get; set; }
        public SavedMovementValues Movement { get; set; }
        public int AttackMode { get; set; }
        public int MovementLimit { get; set; }
        public int GroundLimit { get; set; }
        public bool ModifyCurrent { get; set; }
        public bool ModifyRequested { get; set; }
        public int SmartIndex { get; set; }
        public bool SmartManual { get; set; }
        public bool ActingSurprise { get; set; }
        public bool WaitingGetUp { get; set; }
        public bool WasProne { get; set; }
        public bool CanGetUp { get; set; }
        public float WaitedToEnd { get; set; }
        public string DelayTarget { get; set; }

        internal void Validate(HashSet<string> ids)
        {
            SaveStateValidation.Require(ids.Contains(ActorId) && Status >= 0 && Status <= 6 &&
                Movement != null && AttackMode >= 0 && AttackMode <= 2 &&
                MovementLimit >= 0 && MovementLimit <= 2 && GroundLimit >= 0 && GroundLimit <= 2 &&
                SmartIndex >= -1 && SmartIndex <= 7 && SaveStateValidation.Number(WaitedToEnd, 86400f),
                "Saved turn context is invalid.");
            Movement.Validate();
            SaveStateValidation.ActorReference(DelayTarget, ids);
        }
    }

    internal sealed class SavedMovementAllocation
    {
        public string ActorId { get; set; }
        public int Round { get; set; }
        public long RoundStartTicks { get; set; }
        public string GrantId { get; set; }
        public bool Prepared { get; set; }
        public float MoveObserved { get; set; }
        public bool StandardCommitted { get; set; }
        public SavedMovementValues Movement { get; set; }

        internal void Validate(HashSet<string> ids, long gameTicks)
        {
            SaveStateValidation.Require(ids.Contains(ActorId) && Round >= 0 && Round <= 1000000 &&
                SaveStateValidation.PastTime(RoundStartTicks, gameTicks) &&
                SaveStateValidation.Number(MoveObserved, 86400f) && Movement != null,
                "Saved movement allocation is invalid.");
            if (GrantId != null)
            {
                var parts = GrantId.Split(':');
                SaveStateValidation.Require(parts.Length == 2 && GrantId.Length <= 80 &&
                    (parts[0] == "native" ? Guid.TryParseExact(parts[1], "N", out _) :
                    Guid.TryParseExact(parts[0], "N", out _) && long.TryParse(parts[1], out var sequence) && sequence > 0),
                    "Saved movement grant identity is invalid.");
            }
            Movement.Validate();
        }
    }

    internal sealed class SavedPairedState
    {
        public string RiderId { get; set; }
        public string MountId { get; set; }
        public SavedActivation Activation { get; set; }
        public bool BoundaryIsCurrent { get; set; }
        public SavedTurnContext Boundary { get; set; }
        public SavedTurnContext Partner { get; set; }
        public int SplitReleaseRound { get; set; }
        public string PendingSplitId { get; set; }
        public int PendingSplitRound { get; set; }
        public long RenewalNotBeforeTicks { get; set; }

        internal void Validate(HashSet<string> ids, SavedTurnContext current, long gameTicks)
        {
            SaveStateValidation.ActorReference(RiderId, ids);
            SaveStateValidation.ActorReference(MountId, ids);
            SaveStateValidation.ActorReference(PendingSplitId, ids);
            SaveStateValidation.Require((RiderId == null) == (MountId == null) &&
                (RiderId == null || RiderId != MountId) && SplitReleaseRound >= -1 && SplitReleaseRound <= 1000000 &&
                PendingSplitRound >= -1 && PendingSplitRound <= 1000000 &&
                RenewalNotBeforeTicks >= 0 && (RenewalNotBeforeTicks <= gameTicks ||
                RenewalNotBeforeTicks - gameTicks <= TimeSpan.TicksPerDay),
                "Saved paired ownership or renewal time is invalid.");
            Boundary?.Validate(ids); Partner?.Validate(ids);
            if (Activation == null)
            {
                SaveStateValidation.Require(!BoundaryIsCurrent && Boundary == null && Partner == null,
                    "Turn contexts have no paired activation.");
                return;
            }
            SaveStateValidation.Require(RiderId != null && MountId != null, "Activation has no actor identities.");
            var snapshot = Activation.ToSnapshot();
            var boundary = BoundaryIsCurrent ? current : Boundary;
            SaveStateValidation.Require(!BoundaryIsCurrent || Boundary == null, "Paired boundary is ambiguous.");
            SaveStateValidation.Require(snapshot.Sequence == 0 ? boundary == null && Partner == null :
                boundary != null && boundary.ActorId == RiderId, "Activation boundary identity is invalid.");
            SaveStateValidation.Require(Partner == null || Partner.ActorId == MountId, "Partner context identifies another actor.");
        }
    }

    internal sealed class SavedActivation
    {
        public string EncounterId { get; set; }
        public long Sequence { get; set; }
        public bool Ending { get; set; }
        public bool Finalized { get; set; }
        public bool Split { get; set; }
        public bool Suspended { get; set; }
        public SavedParticipation Rider { get; set; }
        public SavedParticipation Mount { get; set; }

        internal PairedActivationSnapshot ToSnapshot()
        {
            if (!Guid.TryParse(EncounterId, out var id)) throw new InvalidDataException("Activation encounter identity is invalid.");
            return new PairedActivationSnapshot(id, Sequence, Ending, Finalized, Split, Suspended,
                Rider?.ToSnapshot(), Mount?.ToSnapshot());
        }
    }

    internal sealed class SavedParticipation
    {
        public bool Granted { get; set; }
        public bool Prepared { get; set; }
        public bool Ended { get; set; }
        public float StandardObserved { get; set; }
        public float MoveObserved { get; set; }
        public float SwiftObserved { get; set; }
        public bool ForfeitRecorded { get; set; }
        public bool ForfeitSettled { get; set; }
        public float ForfeitAdded { get; set; }

        internal PairedActorSnapshot ToSnapshot() => new PairedActorSnapshot(Granted, Prepared, Ended,
            StandardObserved, MoveObserved, SwiftObserved, ForfeitRecorded, ForfeitSettled, ForfeitAdded);
    }

    internal static class SaveStateValidation
    {
        internal static void Require(bool condition, string message)
        { if (!condition) throw new InvalidDataException(message); }

        internal static bool Number(float value, float maximum = float.MaxValue) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= maximum;

        internal static bool PastTime(long ticks, long gameTicks) => ticks >= 0 && ticks <= gameTicks;

        internal static void ActorReference(string id, HashSet<string> ids) =>
            Require(id == null || ids.Contains(id), "Saved reference is outside the loaded combat actor set.");
    }
}
