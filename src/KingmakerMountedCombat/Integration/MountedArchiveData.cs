using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KingmakerMountedCombat.Integration
{
    // Schema 3 is an in-memory foundation, not a native restoration permission.
    // One combat snapshot still owns roster, current turn, clock and allocations.
    internal sealed class MountedArchiveData
    {
        internal const int Schema = 3;
        internal const int MaximumPairs = 6;
        internal const int MaximumParticipations = 12;
        public int SchemaVersion { get; set; }
        public string CampaignId { get; set; }
        public string AreaId { get; set; }
        public long GameTimeTicks { get; set; }
        public string Policy { get; set; }
        public string RulesId { get; set; }
        public SavedMountedPair[] Pairs { get; set; }
        public SavedPairParticipation[] Participations { get; set; }
        public SavedMountedSlot[] Slots { get; set; }
        public SavedCombatData Combat { get; set; }

        internal void Validate()
        {
            // Versioned identity/profile/slot checks retain the actual archive
            // arrays. Shared combat and native debt still use their existing
            // validators; participation has one bounded collection authority.
            Require(Pairs != null && Pairs.Length <= MaximumPairs && Participations != null &&
                Participations.Length <= MaximumParticipations && SchemaVersion == Schema,
                "Mounted archive collections or schema are invalid.");
            Require(Combat?.Paired == null, "Schema 3 cannot carry a second singular participation authority.");
            Require(Guid.TryParse(CampaignId, out var campaign) && campaign != Guid.Empty &&
                MountedSaveData.HexId(AreaId) && GameTimeTicks >= 0 &&
                Policy == MountedSaveData.PairedPolicy && RulesId == MountedSaveData.Rules,
                "Mounted archive identity, clock or policy is invalid.");
            Combat?.Validate(GameTimeTicks);
            ValidateSlots();
            var pairIds = new HashSet<Guid>();
            var activeActors = new Dictionary<Guid, SavedMountedPair>();
            foreach (var pair in Pairs)
            {
                Require(pair != null && ValidOwner(pair.PairId, pair.Generation) && pairIds.Add(Guid.Parse(pair.PairId)),
                    "Invalid or duplicate mounted owner identity.");
                Require(IsSupportedArchiveProfile(pair.ProfileId) && pair.Rider != null && pair.Mount != null,
                    "Saved archive pair/profile is incomplete or unsupported.");
                pair.Rider.Validate(); pair.Mount.Validate();
                Require(Guid.Parse(pair.Rider.Id) != Guid.Parse(pair.Mount.Id), "Saved pair identifies one actor twice.");
                Require(Combat == null || MountedSaveData.SameActorDebt(pair.Rider,
                    Combat.Actors.SingleOrDefault(a => a.Native.Id == pair.Rider.Id)?.Native) &&
                    MountedSaveData.SameActorDebt(pair.Mount, Combat.Actors.SingleOrDefault(a => a.Native.Id == pair.Mount.Id)?.Native),
                    "Pair and shared combat actor snapshots disagree.");
                Require(!activeActors.ContainsKey(Guid.Parse(pair.Rider.Id)) && !activeActors.ContainsKey(Guid.Parse(pair.Mount.Id)),
                    "An actor has duplicate or cross-role mounted membership.");
                activeActors.Add(Guid.Parse(pair.Rider.Id), pair); activeActors.Add(Guid.Parse(pair.Mount.Id), pair);
            }
            Require(Participations.Length == 0 || Combat != null, "Participation has no shared combat snapshot.");
            var actorIds = new HashSet<string>(Combat?.Actors.Select(a => a.Native.Id) ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var owners = new HashSet<Guid>();
            var participants = new HashSet<Guid>();
            var currentClaims = 0;
            foreach (var record in Participations)
            {
                Require(record != null && ValidOwner(record.OwnerId, record.Generation) &&
                    owners.Add(Guid.Parse(record.OwnerId)) && record.State != null, "Invalid or duplicate participation owner.");
                record.State.Validate(actorIds, Combat.Current, GameTimeTicks);
                Require(record.State.RiderId != null || record.State.MountId != null || record.State.PendingSplitId != null,
                    "A participation owner has no native actor obligation.");
                if (record.State.BoundaryIsCurrent) currentClaims++;
                Require(currentClaims <= 1, "Two owners claim the same native current turn boundary.");
                SavedMountedPair mounted = null;
                if (record.MountedPairId != null)
                {
                    Require(MountedSaveData.ActorId(record.MountedPairId), "Invalid mounted-owner reference.");
                    mounted = Pairs.SingleOrDefault(p => Guid.Parse(p.PairId) == Guid.Parse(record.MountedPairId));
                    Require(mounted != null && Guid.Parse(record.OwnerId) == Guid.Parse(mounted.PairId) &&
                        mounted.Generation == record.Generation && SavedPairParticipation.BelongsToMountedPair(
                            record.State, mounted.Rider.Id, mounted.Mount.Id), "Participation references a different or missing mounted owner.");
                }
                else Require(!pairIds.Contains(Guid.Parse(record.OwnerId)), "A detached participation aliases a mounted owner.");
                var recordActors = new HashSet<Guid>();
                foreach (var id in new[] { record.State.RiderId, record.State.MountId, record.State.PendingSplitId })
                {
                    if (id == null || !recordActors.Add(Guid.Parse(id))) continue;
                    Require(participants.Add(Guid.Parse(id)), "An actor has conflicting retained participation owners.");
                    Require(!activeActors.TryGetValue(Guid.Parse(id), out var active) || ReferenceEquals(active, mounted),
                        "Detached participation conflicts with another active membership.");
                }
            }
            Require(pairIds.Union(owners).Count() <= MaximumParticipations,
                "Mounted and retained ownership together exceed the registry bound.");
        }

        private void ValidateSlots()
        {
            Require(Slots != null && Slots.Length <= 128, "Owned archive control slots are not bounded.");
            var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var slot in Slots)
                Require(slot != null && MountedSaveData.ActorId(slot.ActorId) && slot.Index >= 0 && slot.Index <= 127 &&
                    slot.Kind >= 1 && slot.Kind <= 5 && slots.Add(Guid.Parse(slot.ActorId).ToString("N") + ":" + slot.Index),
                    "Invalid or duplicate owned archive control slot.");
        }

        // New-format eligibility is versioned here; adding a reviewed profile
        // must never broaden schema-1/2's historical Medium-only contract.
        internal static bool IsSupportedArchiveProfile(string profileId) =>
            profileId == "medium-humanoid-mammoth-v1" || profileId == "medium-humanoid-horse-v1";

        private static bool ValidOwner(string id, long generation) => MountedSaveData.ActorId(id) && generation > 0;
        private static void Require(bool condition, string message) => SaveStateValidation.Require(condition, message);
    }

    internal sealed class SavedMountedPair
    {
        public string PairId { get; set; }
        public long Generation { get; set; }
        public string ProfileId { get; set; }
        public SavedNativeActor Rider { get; set; }
        public SavedNativeActor Mount { get; set; }
    }

    internal sealed class SavedPairParticipation
    {
        public string OwnerId { get; set; }
        public long Generation { get; set; }
        public string MountedPairId { get; set; }
        public SavedPairedState State { get; set; }

        internal static bool BelongsToMountedPair(SavedPairedState state, string riderId, string mountId)
        {
            // Native removal may discard the activation and armed identities
            // while the attachment survives. The pending mount still belongs to
            // that owner; linking custody must not manufacture an activation.
            return state != null && riderId != null && mountId != null &&
                ((state.RiderId == riderId && state.MountId == mountId) ||
                (state.RiderId == null && state.MountId == null && state.Activation == null &&
                    state.PendingSplitId == mountId));
        }
    }
}
