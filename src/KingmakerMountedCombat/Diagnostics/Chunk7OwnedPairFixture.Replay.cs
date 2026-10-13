using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7OwnedPairFixture
    {
        // A later native load gets a new fixture owner. The old owner is retained
        // until the exact observed replacement is bound and its registry releases.
        private int replayLoadOrdinal = 1, replayAreaOrdinal, replayExpectedLoad, replayExpectedArea;
        private object admittedPlayer, admittedState;
        private BlueprintArea replayAreaBlueprint;
        private Chunk7OwnedPairFixture replayPredecessor, replayRetiredTo;
        private JObject replayRetirement;
        internal int ReplayLoadOrdinal => replayLoadOrdinal;

        internal void BeginRepeatedLoad(int ordinal)
        {
            Require(selection.Cold && request.PersistenceCase == Chunk7PersistenceScenario.ThreeLoadsCase &&
                bound && !released && replayExpectedLoad == 0 && replayExpectedArea == 0 &&
                ordinal == replayLoadOrdinal + 1 && ordinal >= 2 && ordinal <= 3,
                "Inherited load replacement is not the next selected native epoch.");
            RequirePreserved(); RequireReplayFieldReady();
            replayExpectedLoad = ordinal;
        }
        internal Chunk7OwnedPairFixture CreateRepeatedLoadCandidate(int ordinal, bool exactCallbackObserved)
        {
            RequireRequest(); RequireAuthority(ordinal);
            Require(bound && !released && exactCallbackObserved && selection.Cold &&
                request.PersistenceCase == Chunk7PersistenceScenario.ThreeLoadsCase &&
                replayExpectedLoad == ordinal && ordinal == replayLoadOrdinal + 1 && ordinal >= 2 && ordinal <= 3 &&
                !ReferenceEquals(admittedPlayer, game.Player) && !ReferenceEquals(admittedState, game.State) &&
                persistence.NativeWorldDisposalCount == ordinal - 1 && persistence.SemanticRestoreCount == ordinal * 4 &&
                persistence.PresentationRestoreCount == ordinal * 2 && persistence.ArchivePostLoadObservedCount == 4 &&
                persistence.ArchiveRestorePhase == "all-pair-presentation-restored",
                "A fresh inherited fixture requires the exact callback and complete native replacement proof.");
            return new Chunk7OwnedPairFixture(request, horses, world, controls, persistence, authorization, authority, loader) {
                replayLoadOrdinal = ordinal, replayPredecessor = this };
        }
        internal JObject RetireAfterRepeatedLoad(Chunk7OwnedPairFixture next, Func<object, int> identity)
        {
            if (released)
            {
                Require(ReferenceEquals(next, replayRetiredTo) && replayRetirement != null && identities.Released &&
                    identities.RetainedCount == 0 && identities.FaultCount == 0,
                    "Released inherited epoch has no exact successor and frozen release receipt.");
                return (JObject)replayRetirement.DeepClone();
            }
            RequireRequest();
            Require(next != null && identity != null && bound && next.bound && !next.released &&
                ReferenceEquals(next.replayPredecessor, this) && ReferenceEquals(next.request, request) &&
                ReferenceEquals(next.authorization, authorization) && ReferenceEquals(next.authority, authority) &&
                ReferenceEquals(next.game, game) && ReferenceEquals(next.manager, manager) &&
                next.requestKey == requestKey && next.authorityGeneration == authorityGeneration &&
                replayExpectedLoad == replayLoadOrdinal + 1 && next.replayLoadOrdinal == replayExpectedLoad &&
                next.sourcePath == sourcePath && next.sourceLength == sourceLength && next.sourceTicks == sourceTicks &&
                !ReferenceEquals(next.admittedPlayer, admittedPlayer) && !ReferenceEquals(next.admittedState, admittedState),
                "Old inherited custody cannot retire to an unproved or unrelated native epoch.");
            next.RequirePreserved();
            Require(next.Party.Length == Party.Length && next.Party.All(actor => Party.All(old => !ReferenceEquals(old, actor))) &&
                next.Party.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(Party.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)) &&
                next.partyCharacters.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)
                    .SequenceEqual(partyCharacters.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)) &&
                next.descriptors.All(value => descriptors.All(old => !ReferenceEquals(old, value))) &&
                next.views.All(value => views.All(old => !ReferenceEquals(old, value))) &&
                next.facts.All(value => facts.All(old => !ReferenceEquals(old, value))) &&
                next.ranks.All(value => ranks.All(old => !ReferenceEquals(old, value))) &&
                JToken.DeepEquals(sourceArrangement, next.sourceArrangement) && identities.FaultCount == 0 && identities.Ids.Length == 8,
                "Native replay replaced or reused the wrong inherited roster, descriptor, view, fact or arrangement.");
            var receipt = new JObject { ["fromLoad"] = replayLoadOrdinal, ["toLoad"] = next.replayLoadOrdinal,
                ["oldPlayer"] = identity(admittedPlayer), ["newPlayer"] = identity(next.admittedPlayer),
                ["oldState"] = identity(admittedState), ["newState"] = identity(next.admittedState),
                ["previousIds"] = new JArray(identities.Ids), ["capacity"] = identities.Capacity,
                ["factRemovalCount"] = 0, ["nativeDeletionRequested"] = false };
            identities.Dispose();
            Require(identities.Released && identities.RetainedCount == 0 && identities.FaultCount == 0,
                "Previous inherited identity custody did not release completely.");
            receipt["released"] = true; receipt["retainedAfterRelease"] = identities.RetainedCount;
            replayRetirement = receipt; replayRetiredTo = next; released = true;
            return (JObject)receipt.DeepClone();
        }
        internal void BeginRepeatedArea(int ordinal, BlueprintArea destination)
        {
            Require(selection.Cold && request.PersistenceCase == Chunk7PersistenceScenario.ThreeAreasCase &&
                bound && !released && replayExpectedLoad == 0 && replayExpectedArea == 0 &&
                ordinal == replayAreaOrdinal + 1 && ordinal >= 1 && ordinal <= 3 && destination != null &&
                request.PersistenceReplayTargets?.Length == 3 &&
                destination.AssetGuidThreadSafe == request.PersistenceReplayTargets[ordinal - 1].Area &&
                request.PersistenceReplayTargets[ordinal - 1].AutoSaveMode == "None" &&
                !ReferenceEquals(destination, game.CurrentlyLoadedArea) && destination.AssetGuidThreadSafe != game.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                "Inherited area replacement is not the exact next declared native destination.");
            RequirePreserved(); RequireReplayFieldReady();
            replayAreaBlueprint = destination; replayExpectedArea = ordinal;
        }
        internal void AdoptRepeatedArea(int ordinal, BlueprintArea destination)
        {
            RequireRequest(); RequireAuthority();
            Require(selection.Cold && request.PersistenceCase == Chunk7PersistenceScenario.ThreeAreasCase &&
                bound && !released && replayExpectedLoad == 0 && replayExpectedArea == ordinal && ordinal == replayAreaOrdinal + 1 &&
                ordinal >= 1 && ordinal <= 3 && ReferenceEquals(destination, replayAreaBlueprint) && destination != null &&
                destination.AssetGuidThreadSafe == request.PersistenceReplayTargets[ordinal - 1].Area &&
                persistence.AreaSuspensionCount == ordinal && persistence.AreaResumeCount == ordinal * 2 &&
                persistence.NativeWorldDisposalCount == 0 && persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2,
                "Inherited area custody cannot adopt before its exact native restoration boundary.");
            Scope.RequireDeclaredAreaDestination(destination.AssetGuidThreadSafe, destination);
            var prior = Scope;
            var candidate = new Chunk7NativeWorldScope(request, Party, destination.AssetGuidThreadSafe);
            // This is a read-only validation interval. Nothing can use the candidate
            // outside this call until the complete unchanged field/fact checks pass.
            Scope = candidate;
            try { RequirePreserved(); }
            catch { Scope = prior; throw; }
            replayAreaOrdinal = ordinal; replayExpectedArea = 0; replayAreaBlueprint = null;
        }
        private void RequireReplayFieldReady() => Require(Party != null && Party.All(actor => actor != null &&
            actor.IsInState && actor.IsInGame && actor.View != null && actor.Commands != null && actor.Commands.Empty &&
            actor.Commands.Queue != null && actor.Commands.Queue.Count == 0 && !actor.IsInCombat),
            "The full inherited field roster has unfinished native commands or world readiness.");
        internal JObject CaptureReplayField(Func<object, int> identity, string phase)
        {
            Require(identity != null, "Full inherited replay field needs the retained native identity registry.");
            RequirePreserved(); RequireReplayFieldReady();
            foreach (var value in facts.Concat(ranks)) identity(value);
            return new JObject { ["loadOrdinal"] = replayLoadOrdinal, ["areaOrdinal"] = replayAreaOrdinal,
                ["custody"] = Capture(phase), ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["characters"] = new JArray(partyCharacters.OrderBy(actor => actor.UniqueId, StringComparer.Ordinal)
                    .Select(actor => new JObject { ["id"] = actor.UniqueId, ["object"] = identity(actor) })),
                ["actors"] = new JArray(Party.OrderBy(actor => actor.UniqueId, StringComparer.Ordinal).Select(actor => new JObject {
                    ["id"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid, ["object"] = identity(actor),
                    ["descriptor"] = identity(actor.Descriptor), ["viewObject"] = identity(actor.View), ["view"] = actor.View.GetInstanceID(),
                    ["petId"] = actor.Descriptor.Pet?.UniqueId, ["masterId"] = actor.Descriptor.Master.Value?.UniqueId,
                    ["inState"] = actor.IsInState, ["inGame"] = actor.IsInGame, ["inCombat"] = actor.IsInCombat, ["commandsEmpty"] = actor.Commands.Empty, ["queuedCommands"] = actor.Commands.Queue.Count })) };
        }
    }
}
