using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues S02, review R1 (2026-11-16): a pair deferred at its arrival placement keeps that placement's own
    // geometry while neither unit has moved. The after-entry autosave records such a pair as retained intent, and the
    // reviewed implementation wrote only its identity, so the same pair nine metres apart restored in the session of
    // the arrival and was dropped after loading that autosave.
    //
    // What runs here is production code: the custody-row to save-row conversion (MountedRetainedIntentCapture.Row),
    // the pending-row selection, the recordable filter and the codec (MountedRetainedIntentCapture.SaveText,
    // MountedRetainedIntentRecord.Decode), the load admission and the adopted rows (MountedRetainedIntentAdoption),
    // and then the same restoration policies, coordinator and registry as the other S02 tests. The harness only holds
    // the rows and resolves units by id where the integration does (CaptureRetainedIntentRecord,
    // AdoptLoadedRetainedIntent). That the compiled integration calls these same pieces, and copies the placement
    // between its custody row and the save row, is checked by scripts/Test-PostLoadViewReadinessContracts.ps1.
    // Nothing here is native evidence.
    internal static partial class PostLoadViewReadinessTests
    {
        private static void RegisterSaveAndLoad(TestRunner runner)
        {
            runner.Run("S02 R1 a pair deferred at a nine-metre arrival placement restores exactly once after loading the save written before its remount, as in the session of the arrival", ArrivalPlacementSurvivesSaveAndLoad);
            runner.Run("S02 R1 the reviewed row conversion loses the placement: the same save reloads under the six-metre rule and the pair is dropped", ReviewedConversionDropsThePair);
            runner.Run("S02 R1 ordinary retained intent and every version 1 record keep the distance rule; version 1 text is written and read exactly as before", OrdinaryIntentAndLegacyRecords);
            runner.Run("S02 R1 a relocated pair does not keep the arrival placement, before or after the save and its load", RelocatedPairAcrossSaveAndLoad);
            runner.Run("S02 R1 another campaign, area or game time, a replaced load, an archived pair, a missing actor and a newer transfer cannot use a saved placement", StaleIdentityAcrossSaveAndLoad);
            runner.Run("S02 R1 other pairs and control rows are saved, adopted and judged exactly as before beside a deferred pair", OtherPairsAndControlRows);
            runner.Run("S02 R1 the version 2 text is exact, strict and bounded", RecordVersion2Codec);
        }

        private const string SaveCampaign = "f7e1c1a8-2b7d-4a55-9c3e-5d1b0a6e4f21", OtherCampaign = "7f9619ff-8b86-d011-b42d-00cf4fc964ff";
        private const string SaveArea = "0123456789abcdef0123456789abcdef", OtherArea = "fedcba9876543210fedcba9876543210";
        private const long SaveTicks = 864000000000L;

        private sealed partial class Harness
        {
            // AreaTransfer.Slots of the retained custody.
            internal readonly List<MountedRetainedIntentSlotRecord> Slots = new List<MountedRetainedIntentSlotRecord>();
            // False converts a custody row to a save row as the reviewed implementation did: identity fields only.
            internal bool CarryArrival = true;

            // CaptureRetainedIntentRecord at the save header barrier, after the arrival was attempted: the production
            // row conversion (rows of the retained custody), pending-row selection, recordable filter and codec.
            internal string Save(string campaign, string area, long ticks)
            {
                var rows = MountedRetainedIntentCapture.PendingRows(Retained.Select(row => MountedRetainedIntentCapture.Row(
                    row.Pair.Rider.Id, row.Pair.Mount.Id, MountedRiderProfileCatalog.MediumHorseId, row.OwnerId.ToString("N"), row.Generation,
                    CarryArrival ? row.Arrival : null, true)), null, true);
                var mounted = new HashSet<string>(Pairs.Where(pair => pair.Mounted).SelectMany(pair => new[] { pair.Rider.Id, pair.Mount.Id }),
                    StringComparer.Ordinal);
                return MountedRetainedIntentCapture.SaveText(campaign, area, ticks, Kind, rows, mounted, Slots);
            }

            // The same units as a load deserialises them into this (new) world: their stable ids, the positions the save
            // holds, and views that are inactive until the first Default tick.
            internal Pair AddLoaded(Pair saved)
            {
                var pair = new Pair(saved.Rider.Id, saved.Mount.Id);
                pair.Rider.X = saved.Rider.X; pair.Mount.X = saved.Mount.X;
                pair.Rider.Sleep(); pair.Mount.Sleep();
                Pairs.Add(pair);
                return pair;
            }

            // SelectRetainedIntentRecord and AdoptLoadedRetainedIntent in the world this harness stands for: production
            // decode, load admission, world match and adopted rows. False when nothing was adopted.
            internal bool Adopt(string text, string campaign, string area, long ticks, long selectingLoad = 1, long currentLoad = 1,
                ICollection<string> archivedActors = null)
            {
                MountedRetainedIntentRecord record;
                // "Saved retained mounted intent could not be read; the pairs load dismounted."
                try { record = MountedRetainedIntentRecord.Decode(text); }
                catch (System.IO.InvalidDataException) { return false; }
                if (!MountedRetainedIntentAdoption.SelectingLoadIsCurrent(selectingLoad, true, currentLoad, currentLoad)) return false;
                // The world a load created has that save's area loaded.
                Area = area;
                var adopted = MountedRetainedIntentAdoption.Pairs(record, campaign, area, ticks, archivedActors);
                if (adopted.Length == 0) return false;
                foreach (var saved in adopted)
                {
                    // ResolveUnique: an actor the loaded world does not hold stays unresolved.
                    var pair = Pairs.SingleOrDefault(candidate => candidate.Rider.Id == saved.RiderId && candidate.Mount.Id == saved.MountId);
                    if (pair == null)
                    {
                        pair = new Pair(saved.RiderId, saved.MountId);
                        pair.Rider.Present = false; pair.Mount.Present = false;
                        Pairs.Add(pair);
                    }
                    Retained.Add(new Row { Pair = pair, OwnerId = saved.OwnerId != null ? Guid.ParseExact(saved.OwnerId, "N") : Guid.Empty,
                        Generation = saved.Generation, World = World, Arrival = saved.Arrival });
                }
                Slots.AddRange(MountedRetainedIntentAdoption.Slots(record, adopted));
                Kind = record.Kind;
                ResetSettle();
                return true;
            }
        }

        private static MountedRetainedIntentSlotRecord SlotRow(string actor, int index, int kind) =>
            new MountedRetainedIntentSlotRecord { ActorId = actor, Index = index, Kind = kind };

        private static string SlotText(IEnumerable<MountedRetainedIntentSlotRecord> slots) =>
            string.Join(",", slots.Select(slot => slot.ActorId + "|" + slot.Index + "|" + slot.Kind).ToArray());

        // Settled ordinary play in a loaded world: the loader is idle and a Default tick has woken the party.
        private static float WakeAndPlay(Harness world, float from, int frames, params Pair[] pairs)
        {
            world.Loading = false;
            foreach (var pair in pairs) { pair.Rider.Wake(); pair.Mount.Wake(); }
            return world.Play(from, frames);
        }

        private static void ArrivalPlacementSurvivesSaveAndLoad()
        {
            // 1 and 2. The supported nine-metre formation spacing, inactive views: deferred, nothing attached.
            var session = ArriveFromWorldMap(out var pair, out var row, riderX: 0f, mountX: 9f);
            TestRunner.True(row.Arrival != null && !pair.Mounted && pair.SavedRestores == 0 && session.Retained.Count == 1,
                "The nine-metre arrival was not deferred without attachment.");
            session.Slots.Add(SlotRow(pair.Rider.Id, 9, MountedControlSlotPolicy.SavedMountState));

            // 3. The after-entry autosave: the pending pair is captured and encoded.
            var text = session.Save(SaveCampaign, SaveArea, SaveTicks);
            TestRunner.True(text != null && text.StartsWith(MountedRetainedIntentRecord.ArrivalHeader + "\n", StringComparison.Ordinal),
                "The save of a deferred arrival pair did not carry its arrival placement.");

            // 4. The load decodes it; the matching loaded world adopts it.
            var loaded = new Harness();
            var again = loaded.AddLoaded(pair);
            TestRunner.True(loaded.Adopt(text, SaveCampaign, SaveArea, SaveTicks), "The exact saved world did not adopt the record.");
            var adopted = loaded.Retained.Single();
            TestRunner.True(adopted.Arrival != null && adopted.Arrival.RiderX == 0f && adopted.Arrival.MountX == 9f && adopted.Arrival.AreaId == SaveArea &&
                adopted.OwnerId == row.OwnerId && adopted.Generation == row.Generation, "The adopted row lost its placement or its identity.");
            TestRunner.Equal(pair.Rider.Id + "|9|" + MountedControlSlotPolicy.SavedMountState, SlotText(loaded.Slots), "The pair's control row did not travel.");

            // 5. Still at the accepted placement, inside the load and then with views that are not active yet: nothing attaches.
            loaded.Loading = true;
            var now = loaded.Play(0f, 10);
            loaded.Loading = false;
            now = loaded.Play(now, 10);
            TestRunner.True(!again.Mounted && again.SavedRestores == 0 && loaded.Retained.Count == 1 && loaded.Dropped.Count == 0,
                "The loaded pair was attached before its views were active, or was dropped while it waited.");

            // 6. Views ready, ordinary play settled: restored exactly once, with the same pair identity, by a saved
            // restore (no voluntary Mount, so no action, preparation or participation is involved).
            now = WakeAndPlay(loaded, now, 10, again);
            TestRunner.True(again.Mounted && loaded.Retained.Count == 0 && loaded.Dropped.Count == 0 && loaded.Latch == null,
                "A valid deferred pair at its accepted nine-metre arrival placement did not restore after loading.");
            TestRunner.True(again.Owner.Id == row.OwnerId && again.Owner.Generation == row.Generation, "The loaded pair was restored under another identity.");
            loaded.Play(now, 30);
            TestRunner.True(again.Mounted && again.SavedRestores == 1 && again.Runtime.Attach == 1 && again.Runtime.Authority == 1 && again.VoluntaryMounts == 0,
                "The loaded pair was not restored exactly once by a saved restore.");

            // The session of the arrival, continued instead of loaded: the same outcome.
            WakeAndPlay(session, 0f, 10, pair);
            TestRunner.True(pair.Mounted && pair.SavedRestores == 1 && pair.Owner.Id == row.OwnerId, "The session of the arrival did not restore the same pair.");
        }

        private static void ReviewedConversionDropsThePair()
        {
            // The reviewed implementation (f71fabe7) converted a custody row to a save row from its identity fields only.
            var session = ArriveFromWorldMap(out var pair, out var row, riderX: 0f, mountX: 9f);
            session.CarryArrival = false;
            var text = session.Save(SaveCampaign, SaveArea, SaveTicks);
            TestRunner.True(row.Arrival != null && text.StartsWith(MountedRetainedIntentRecord.Header + "\n", StringComparison.Ordinal) &&
                text.IndexOf("arrival", StringComparison.Ordinal) < 0, "The reviewed conversion wrote an arrival placement.");
            var loaded = new Harness();
            var again = loaded.AddLoaded(pair);
            TestRunner.True(loaded.Adopt(text, SaveCampaign, SaveArea, SaveTicks) && loaded.Retained.Single().Arrival == null,
                "The reviewed record was not adopted as ordinary retained intent.");
            WakeAndPlay(loaded, 0f, 10, again);
            // The regression the review reported: continuing the session restores this pair, loading drops it.
            TestRunner.True(!again.Mounted && again.SavedRestores == 0 && loaded.Dropped.Count == 1 && loaded.Dropped[0] == "separated",
                "The reviewed conversion did not reproduce the reported drop after loading.");
            WakeAndPlay(session, 0f, 10, pair);
            TestRunner.True(pair.Mounted, "The session of the arrival did not restore the pair the load dropped.");
        }

        private static void OrdinaryIntentAndLegacyRecords()
        {
            // Retained at arrival for another refusal (the arrival mode), nine metres apart: never an arrival placement.
            var session = ArriveFromWorldMap(out var pair, out var row, riderX: 0f, mountX: 9f, arrivalMode: "Cutscene");
            var text = session.Save(SaveCampaign, SaveArea, SaveTicks);
            TestRunner.True(row.Arrival == null && text.StartsWith(MountedRetainedIntentRecord.Header + "\n", StringComparison.Ordinal) &&
                text.IndexOf("arrival", StringComparison.Ordinal) < 0, "Ordinary retained intent was saved with an arrival placement.");
            var loaded = new Harness();
            var again = loaded.AddLoaded(pair);
            TestRunner.True(loaded.Adopt(text, SaveCampaign, SaveArea, SaveTicks), "Ordinary retained intent was not adopted.");
            WakeAndPlay(loaded, 0f, 10, again);
            TestRunner.True(!again.Mounted && loaded.Dropped.Count == 1 && loaded.Dropped[0] == "separated",
                "Ordinary retained intent was seated across nine metres after loading.");

            // A version 1 text as every earlier build wrote it, stated by hand. Loaded in a local area it is ordinary
            // retained intent: no arrival placement is invented for it, whatever the positions in the loaded world.
            var rider = Guid.NewGuid().ToString(); var mount = Guid.NewGuid().ToString();
            var owner = Guid.NewGuid();
            var legacy = "KMC-RETAINED-INTENT 1\n" +
                "campaign " + SaveCampaign + "\n" +
                "area " + SaveArea + "\n" +
                "time 864000000000\n" +
                "kind WorldMapTravel\n" +
                "pair " + rider + " " + mount + " medium-humanoid-horse-v1 " + owner.ToString("N") + " 4\n" +
                "slot " + rider + " 9 " + MountedControlSlotPolicy.SavedMountState + "\n";
            var decoded = MountedRetainedIntentRecord.Decode(legacy);
            TestRunner.True(decoded.Pairs.Length == 1 && decoded.Pairs[0].Arrival == null && decoded.Slots.Length == 1,
                "A version 1 record was not read with its established meaning.");
            // The same content is still written as exactly that text.
            TestRunner.Equal(legacy, decoded.Encode(), "The version 1 text changed.");
            TestRunner.Equal(legacy, decoded.EncodeForSave(), "A save without a deferred arrival pair no longer writes the version 1 text.");
            foreach (var separation in new[] { 9f, 4f })
            {
                var world = new Harness();
                var units = new Pair(rider, mount);
                units.Rider.X = 0f; units.Mount.X = separation;
                var old = world.AddLoaded(units);
                TestRunner.True(world.Adopt(legacy, SaveCampaign, SaveArea, SaveTicks) && world.Retained.Single().Arrival == null,
                    "A version 1 record gained an arrival placement at adoption.");
                WakeAndPlay(world, 0f, 10, old);
                if (separation > MountedRetainedIntentPolicy.MaximumRestoreSeparationMetres)
                    TestRunner.True(!old.Mounted && world.Dropped.Count == 1 && world.Dropped[0] == "separated", "A version 1 pair was seated across nine metres.");
                else
                    TestRunner.True(old.Mounted && old.SavedRestores == 1 && old.Owner.Id == owner && old.Owner.Generation == 4L,
                        "A version 1 pair within reach was not restored with its saved identity.");
            }
        }

        private static void RelocatedPairAcrossSaveAndLoad()
        {
            // Relocated before the save: the save holds the arrival placement and the world it loads holds the new
            // position, so the loaded pair is judged by the ordinary rule.
            var before = ArriveFromWorldMap(out var movedEarly, out _, riderX: 0f, mountX: 9f);
            movedEarly.Mount.X = 12f;
            var world = new Harness();
            var early = world.AddLoaded(movedEarly);
            TestRunner.True(world.Adopt(before.Save(SaveCampaign, SaveArea, SaveTicks), SaveCampaign, SaveArea, SaveTicks) &&
                world.Retained.Single().Arrival.MountX == 9f, "The saved placement was not the arrival placement.");
            WakeAndPlay(world, 0f, 10, early);
            TestRunner.True(!early.Mounted && early.SavedRestores == 0 && world.Dropped.Count == 1 && world.Dropped[0] == "separated",
                "A pair relocated before the save kept the arrival allowance after loading.");

            // Relocated before the save to within reach: the ordinary rule restores it.
            var near = ArriveFromWorldMap(out var movedNear, out _, riderX: 0f, mountX: 9f);
            movedNear.Mount.X = 4f;
            world = new Harness();
            var gathered = world.AddLoaded(movedNear);
            world.Adopt(near.Save(SaveCampaign, SaveArea, SaveTicks), SaveCampaign, SaveArea, SaveTicks);
            WakeAndPlay(world, 0f, 10, gathered);
            TestRunner.True(gathered.Mounted && gathered.SavedRestores == 1, "A relocated pair within reach was not restored by the ordinary rule after loading.");

            // Saved at the arrival placement, relocated after the load and before the restoration.
            var held = ArriveFromWorldMap(out var movedLate, out _, riderX: 0f, mountX: 9f);
            var text = held.Save(SaveCampaign, SaveArea, SaveTicks);
            world = new Harness();
            var late = world.AddLoaded(movedLate);
            world.Adopt(text, SaveCampaign, SaveArea, SaveTicks);
            late.Rider.X = -3.5f;
            WakeAndPlay(world, 0f, 10, late);
            TestRunner.True(!late.Mounted && world.Dropped.Count == 1 && world.Dropped[0] == "separated",
                "A pair relocated after the load kept the arrival allowance.");

            // A drift within the placement tolerance is still the arrival placement, after a load as before it.
            world = new Harness();
            var drifted = world.AddLoaded(movedLate);
            world.Adopt(text, SaveCampaign, SaveArea, SaveTicks);
            drifted.Mount.X = 9.05f;
            WakeAndPlay(world, 0f, 10, drifted);
            TestRunner.True(drifted.Mounted, "A unit within the placement tolerance was treated as relocated after loading.");

            // A placement belongs to the area it was observed in. A save of another area never writes it, and a row of a
            // transfer still in flight never carries one.
            var elsewhere = ArriveFromWorldMap(out _, out var elsewhereRow, riderX: 0f, mountX: 9f);
            var foreign = elsewhere.Save(SaveCampaign, OtherArea, SaveTicks);
            TestRunner.True(elsewhereRow.Arrival != null && elsewhereRow.Arrival.AreaId == SaveArea &&
                foreign.StartsWith(MountedRetainedIntentRecord.Header + "\n", StringComparison.Ordinal) &&
                MountedRetainedIntentRecord.Decode(foreign).Pairs[0].Arrival == null, "A placement was saved into a save of another area.");
            TestRunner.True(MountedRetainedIntentCapture.Row("r", "m", "p", null, 0, elsewhereRow.Arrival, false).Arrival == null &&
                ReferenceEquals(MountedRetainedIntentCapture.Row("r", "m", "p", null, 0, elsewhereRow.Arrival, true).Arrival, elsewhereRow.Arrival),
                "A row of a transfer in flight carried a placement, or a retained row lost its own.");
            // A custody that outlived its area without a transfer (the mod was disabled across the change) is judged in
            // the area now loaded: its placement means nothing there, and the ordinary rule applies.
            var strayed = ArriveFromWorldMap(out var stray, out var strayRow, riderX: 0f, mountX: 9f);
            strayed.Area = OtherArea;
            WakeAndPlay(strayed, 0f, 10, stray);
            TestRunner.True(strayRow.Arrival != null && !stray.Mounted && strayed.Dropped.Count == 1 && strayed.Dropped[0] == "separated",
                "A placement observed in one area was honoured in another.");

            // The rule itself, from the recorded placement.
            var placement = new MountedRetainedArrivalPlacement(SaveArea, 1f, 2f, 3f, 10f, 2f, 3f);
            var tolerance = MountedRetainedIntentPolicy.ArrivalPlacementToleranceMetres;
            TestRunner.True(MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, SaveArea, true, 1f, 2f, 3f, 10f, 2f, 3f) &&
                MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, SaveArea, true, 1f + tolerance * 0.9f, 2f, 3f, 10f, 2f, 3f - tolerance * 0.9f),
                "An unmoved pair does not stand at its arrival placement.");
            TestRunner.True(!MountedRetainedIntentPolicy.StandsAtArrivalPlacement(null, SaveArea, true, 1f, 2f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, SaveArea, false, 1f, 2f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, SaveArea, true, 1f + tolerance * 1.1f, 2f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, SaveArea, true, 1f, 2f, 3f, 10f, 2f + tolerance * 1.1f, 3f) &&
                // The displacement is a distance in space, not per axis: 0.08 m on two axes is 0.113 m.
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, SaveArea, true, 1.08f, 2.08f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, SaveArea, true, float.NaN, 2f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(new MountedRetainedArrivalPlacement(SaveArea, float.NaN, 0f, 0f, 0f, 0f, 0f), SaveArea, true, 0f, 0f, 0f, 0f, 0f, 0f),
                "A row without a placement, an unresolved or moved unit, or an unknown position held the arrival placement.");
            // Another area, an unknown area, and a placement that names no area.
            TestRunner.True(!MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, OtherArea, true, 1f, 2f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(placement, null, true, 1f, 2f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(new MountedRetainedArrivalPlacement(null, 1f, 2f, 3f, 10f, 2f, 3f), null, true, 1f, 2f, 3f, 10f, 2f, 3f) &&
                !MountedRetainedIntentPolicy.StandsAtArrivalPlacement(new MountedRetainedArrivalPlacement("", 1f, 2f, 3f, 10f, 2f, 3f), "", true, 1f, 2f, 3f, 10f, 2f, 3f),
                "A placement was honoured outside the area it was observed in.");
        }

        private static void StaleIdentityAcrossSaveAndLoad()
        {
            var session = ArriveFromWorldMap(out var pair, out _, riderX: 0f, mountX: 9f);
            var text = session.Save(SaveCampaign, SaveArea, SaveTicks);

            // Another campaign, another area, another native game time: nothing is adopted, so nothing can restore.
            foreach (var world in new[] {
                new { Campaign = OtherCampaign, Area = SaveArea, Ticks = SaveTicks },
                new { Campaign = SaveCampaign, Area = OtherArea, Ticks = SaveTicks },
                new { Campaign = SaveCampaign, Area = SaveArea, Ticks = SaveTicks + 1 } })
            {
                var other = new Harness();
                var units = other.AddLoaded(pair);
                TestRunner.True(!other.Adopt(text, world.Campaign, world.Area, world.Ticks) && other.Retained.Count == 0,
                    "A record was adopted by a world it was not written into.");
                WakeAndPlay(other, 0f, 30, units);
                TestRunner.True(!units.Mounted && units.SavedRestores == 0, "A mismatched world revived the pair.");
            }

            // A replaced or cancelled load: the load that selected the record is no longer the current one.
            var replaced = new Harness();
            var stale = replaced.AddLoaded(pair);
            TestRunner.True(!replaced.Adopt(text, SaveCampaign, SaveArea, SaveTicks, selectingLoad: 1, currentLoad: 2) && replaced.Retained.Count == 0,
                "A replaced load adopted the record of the load it replaced.");
            WakeAndPlay(replaced, 0f, 30, stale);
            TestRunner.True(!stale.Mounted && stale.SavedRestores == 0, "A replaced load revived the pair.");
            TestRunner.True(MountedRetainedIntentAdoption.SelectingLoadIsCurrent(3, true, 3, 3) &&
                !MountedRetainedIntentAdoption.SelectingLoadIsCurrent(3, false, 3, 3) && !MountedRetainedIntentAdoption.SelectingLoadIsCurrent(3, true, 4, 4) &&
                !MountedRetainedIntentAdoption.SelectingLoadIsCurrent(3, true, 3, 4) && !MountedRetainedIntentAdoption.SelectingLoadIsCurrent(3, true, 4, 3),
                "A load other than the selecting, still current load may adopt a record.");

            // A pair the mounted-state member of the same save already restores is never adopted twice.
            var archived = new Harness();
            archived.AddLoaded(pair);
            TestRunner.True(!archived.Adopt(text, SaveCampaign, SaveArea, SaveTicks, archivedActors: new HashSet<string> { pair.Mount.Id }) &&
                archived.Retained.Count == 0, "A pair restored from the mounted-state member was adopted again.");

            // The loaded world does not hold the saved actors: the row is dropped, nothing else is touched.
            var missing = new Harness();
            var bystander = missing.Add();
            TestRunner.True(missing.Adopt(text, SaveCampaign, SaveArea, SaveTicks), "The record of the exact world was refused.");
            WakeAndPlay(missing, 0f, 10, bystander);
            TestRunner.True(missing.Retained.Count == 0 && missing.Dropped.Count == 1 && missing.Pairs.All(candidate => !candidate.Mounted) &&
                bystander.SavedRestores == 0 && bystander.Runtime.Attach == 0, "A missing actor was restored, or another pair was touched.");

            // The saved identity is already in use in the loaded world (the player mounted that pair again): dropped.
            var inUse = new Harness();
            var remounted = inUse.AddLoaded(pair);
            inUse.Adopt(text, SaveCampaign, SaveArea, SaveTicks);
            inUse.Loading = false;
            remounted.Rider.Wake(); remounted.Mount.Wake();
            remounted.Mount.X = 1f;
            inUse.MountByPlayer(remounted, 7);
            inUse.Play(0f, 10);
            TestRunner.True(remounted.Mounted && remounted.SavedRestores == 0 && remounted.Owner.Generation == 7L && inUse.Retained.Count == 0,
                "A saved row re-attached a pair the player had already mounted again.");

            // A newer transfer copies its carried rows without the placement (BeginAreaTransition through
            // AreaPair.CarriedCopy), so a placement adopted from a save never reaches the next arrival. The harness's
            // Arrive stands for that copy here; the compiled copy and its three call sites are checked by
            // scripts/Test-PostLoadViewReadinessContracts.ps1. What this asserts is the consequence: without the
            // placement the pair is ordinary retained intent, in the retry and in the next save.
            var travelled = new Harness();
            var traveller = travelled.AddLoaded(pair);
            travelled.Adopt(text, SaveCampaign, SaveArea, SaveTicks);
            travelled.Loading = true;
            travelled.Mode = "Cutscene";
            travelled.Arrive(FullLoadTransfer());
            travelled.Mode = "Default";
            TestRunner.True(travelled.Retained.Count == 1 && travelled.Retained[0].Arrival == null, "A saved placement travelled into a newer transfer.");
            var later = travelled.Save(SaveCampaign, SaveArea, SaveTicks + 600);
            TestRunner.True(later.StartsWith(MountedRetainedIntentRecord.Header + "\n", StringComparison.Ordinal) && later.IndexOf("arrival", StringComparison.Ordinal) < 0,
                "The save of the later custody still carried the earlier placement.");
            WakeAndPlay(travelled, 0f, 10, traveller);
            TestRunner.True(!traveller.Mounted && travelled.Dropped.Count == 1 && travelled.Dropped[0] == "separated",
                "A newer transfer used the placement of an earlier arrival.");
        }

        private static void OtherPairsAndControlRows()
        {
            // Two pairs arrive from the world map nine metres apart each. The first is eligible and only its views hold
            // it back (deferred at its arrival placement). The second is refused for another reason (its rider is not
            // directly controllable at arrival), so it is ordinary retained intent.
            var session = new Harness();
            var deferred = session.Add(); var ordinary = session.Add();
            session.MountByPlayer(deferred); session.MountByPlayer(ordinary);
            var deferredRow = session.Suspend(deferred); var ordinaryRow = session.Suspend(ordinary);
            foreach (var pair in new[] { deferred, ordinary }) { pair.Rider.Sleep(); pair.Mount.Sleep(); pair.Rider.X = 0f; pair.Mount.X = 9f; }
            ordinary.Rider.Controllable = false;
            session.Loading = true;
            session.Arrive(FullLoadTransfer());
            TestRunner.True(deferredRow.Arrival != null && ordinaryRow.Arrival == null && session.Retained.Count == 2 && !deferred.Mounted && !ordinary.Mounted,
                "The two pairs were not retained as one deferred arrival and one ordinary intent.");
            session.Slots.Add(SlotRow(deferred.Rider.Id, 9, MountedControlSlotPolicy.SavedMountState));
            session.Slots.Add(SlotRow(ordinary.Rider.Id, 0, MountedControlSlotPolicy.SavedPlacementRecord));
            session.Slots.Add(SlotRow(ordinary.Mount.Id, 11, MountedControlSlotPolicy.SavedMountState));
            var text = session.Save(SaveCampaign, SaveArea, SaveTicks);

            // The version 2 text is the reviewed (version 1) text of the same save plus the header and one arrival row:
            // every pair row and every control row is written exactly as before.
            session.CarryArrival = false;
            var reviewed = session.Save(SaveCampaign, SaveArea, SaveTicks);
            session.CarryArrival = true;
            var arrivalRows = text.Split('\n').Where(line => line.StartsWith("arrival ", StringComparison.Ordinal)).ToArray();
            TestRunner.True(arrivalRows.Length == 1 && arrivalRows[0] == "arrival " + deferred.Rider.Id + " " + deferred.Mount.Id + " 0 0 0 9 0 0",
                "The save did not hold exactly one arrival row, for the deferred pair only.");
            TestRunner.Equal(reviewed, text.Replace(MountedRetainedIntentRecord.ArrivalHeader, MountedRetainedIntentRecord.Header).Replace(arrivalRows[0] + "\n", ""),
                "A pair row or a control row of the save changed beside the arrival row.");

            // Loaded: the deferred pair restores at its placement; the other pair, nine metres apart, is dropped by the
            // ordinary rule exactly as before and gains nothing from its neighbour.
            var loaded = new Harness();
            var deferredAgain = loaded.AddLoaded(deferred); var ordinaryAgain = loaded.AddLoaded(ordinary);
            TestRunner.True(loaded.Adopt(text, SaveCampaign, SaveArea, SaveTicks) && loaded.Retained.Count == 2, "Both pairs were not adopted.");
            TestRunner.True(loaded.Retained.Single(candidate => candidate.Pair == deferredAgain).Arrival != null &&
                loaded.Retained.Single(candidate => candidate.Pair == ordinaryAgain).Arrival == null, "The placement was adopted for the wrong pair.");
            TestRunner.Equal(SlotText(session.Slots), SlotText(loaded.Slots), "The control rows of the two pairs did not travel unchanged.");
            WakeAndPlay(loaded, 0f, 10, deferredAgain, ordinaryAgain);
            TestRunner.True(deferredAgain.Mounted && deferredAgain.SavedRestores == 1 && deferredAgain.Owner.Id == deferredRow.OwnerId,
                "The deferred pair did not restore beside another retained pair.");
            TestRunner.True(!ordinaryAgain.Mounted && ordinaryAgain.SavedRestores == 0 && loaded.Dropped.Count == 1 && loaded.Dropped[0] == "separated",
                "An ordinary retained pair nine metres apart was not judged by the ordinary rule beside a deferred pair.");

            // The same save with the ordinary pair within reach: it restores by the ordinary rule, with its own identity.
            ordinary.Mount.X = 4f;
            var within = new Harness();
            var first = within.AddLoaded(deferred); var second = within.AddLoaded(ordinary);
            within.Adopt(text, SaveCampaign, SaveArea, SaveTicks);
            WakeAndPlay(within, 0f, 10, first, second);
            TestRunner.True(first.Mounted && second.Mounted && second.SavedRestores == 1 && second.Owner.Id == ordinaryRow.OwnerId && within.Dropped.Count == 0,
                "An ordinary retained pair within reach was not restored beside a deferred pair.");

            // Only the adopted pairs' own control rows are adopted.
            var partial = new Harness();
            partial.AddLoaded(deferred); partial.AddLoaded(ordinary);
            partial.Adopt(text, SaveCampaign, SaveArea, SaveTicks, archivedActors: new HashSet<string> { deferred.Rider.Id });
            TestRunner.True(partial.Retained.Count == 1 && partial.Retained[0].Arrival == null, "An archived pair was adopted, or its placement moved to another pair.");
            TestRunner.Equal(ordinary.Rider.Id + "|0|" + MountedControlSlotPolicy.SavedPlacementRecord + "," + ordinary.Mount.Id + "|11|" + MountedControlSlotPolicy.SavedMountState,
                SlotText(partial.Slots), "A control row of a pair that was not adopted travelled.");
        }

        private static void RecordVersion2Codec()
        {
            var rider = Guid.NewGuid().ToString(); var mount = Guid.NewGuid().ToString();
            var owner = Guid.NewGuid().ToString("N");
            MountedRetainedIntentPairRecord PairRow(string riderId, string mountId, string ownerId, MountedRetainedArrivalPlacement arrival) =>
                MountedRetainedIntentCapture.Row(riderId, mountId, MountedRiderProfileCatalog.MediumHorseId, ownerId, 2, arrival, true);
            MountedRetainedIntentRecord Record(params MountedRetainedIntentPairRecord[] recorded) => new MountedRetainedIntentRecord {
                CampaignId = SaveCampaign, AreaId = SaveArea, GameTimeTicks = SaveTicks, Kind = MountedRetainedIntentKind.WorldMapTravel, Pairs = recorded };

            // Exact: every coordinate is read back as the float that was written.
            var placement = new MountedRetainedArrivalPlacement(SaveArea, 12.345679f, -0.1f, 1e-5f, 123456.79f, -3.4e10f, 0f);
            var record = Record(PairRow(rider, mount, owner, placement));
            record.Slots = new[] { SlotRow(rider, 9, MountedControlSlotPolicy.SavedMountState) };
            var text = record.Encode();
            var read = MountedRetainedIntentRecord.Decode(text).Pairs.Single().Arrival;
            TestRunner.True(read != null && read.AreaId == SaveArea && read.RiderX == placement.RiderX && read.RiderY == placement.RiderY && read.RiderZ == placement.RiderZ &&
                read.MountX == placement.MountX && read.MountY == placement.MountY && read.MountZ == placement.MountZ,
                "The arrival placement did not round-trip exactly.");
            TestRunner.Equal(text, MountedRetainedIntentRecord.Decode(text).Encode(), "A version 2 text is not stable through decode and encode.");
            var lines = text.Split('\n');
            TestRunner.True(lines[0] == "KMC-RETAINED-INTENT 2" && lines[5].StartsWith("pair ", StringComparison.Ordinal) &&
                lines[6].StartsWith("arrival " + rider + " " + mount + " ", StringComparison.Ordinal) && lines[7].StartsWith("slot ", StringComparison.Ordinal),
                "The version 2 shape is not header, fields, pair rows, arrival rows, slot rows.");

            // A placement that is not a finite position is no evidence: it is not recordable, and never encodable.
            var unknown = new MountedRetainedArrivalPlacement(SaveArea, float.NaN, 0f, 0f, 0f, 0f, float.PositiveInfinity);
            TestRunner.True(MountedRetainedIntentRecord.Recordable(new[] { PairRow(rider, mount, owner, unknown) }, new HashSet<string>()).Single().Arrival == null,
                "A non-finite placement was recorded.");
            var refusedEncode = false;
            try { Record(PairRow(rider, mount, owner, unknown)).Encode(); } catch (System.IO.InvalidDataException) { refusedEncode = true; }
            TestRunner.True(refusedEncode, "A non-finite placement was encoded.");
            // A placement observed in another area is not part of this area's record: the save conversion leaves it out
            // (the pair is saved as ordinary intent) and the codec itself refuses it.
            var foreignPlacement = new MountedRetainedArrivalPlacement(OtherArea, 0f, 0f, 0f, 9f, 0f, 0f);
            var foreignSave = MountedRetainedIntentCapture.SaveText(SaveCampaign, SaveArea, SaveTicks, MountedRetainedIntentKind.WorldMapTravel,
                new[] { PairRow(rider, mount, owner, foreignPlacement) }, new HashSet<string>(), null);
            var refusedForeign = false;
            try { Record(PairRow(rider, mount, owner, foreignPlacement)).Encode(); } catch (System.IO.InvalidDataException) { refusedForeign = true; }
            TestRunner.True(refusedForeign && foreignSave.StartsWith(MountedRetainedIntentRecord.Header + "\n", StringComparison.Ordinal) &&
                MountedRetainedIntentRecord.Decode(foreignSave).Pairs.Single().Arrival == null, "A placement of another area was written into this area's record.");

            // Strict: any malformed or unsupported version 2 text is refused whole.
            var arrivalRow = lines[6];
            var coordinates = arrivalRow.Substring(("arrival " + rider + " " + mount + " ").Length);
            string WithCoordinates(string replacement) => text.Replace(arrivalRow, "arrival " + rider + " " + mount + " " + replacement);
            foreach (var malformed in new[] {
                // An arrival row under the version 1 header, and a version 2 header without one.
                text.Replace(MountedRetainedIntentRecord.ArrivalHeader, MountedRetainedIntentRecord.Header),
                text.Replace(arrivalRow + "\n", ""),
                // A later, unknown version.
                text.Replace(MountedRetainedIntentRecord.ArrivalHeader, "KMC-RETAINED-INTENT 3"),
                // One pair twice; no recorded pair; rider and mount exchanged.
                text.Replace(arrivalRow + "\n", arrivalRow + "\n" + arrivalRow + "\n"),
                text.Replace(arrivalRow, "arrival " + Guid.NewGuid() + " " + mount + " " + coordinates),
                text.Replace(arrivalRow, "arrival " + mount + " " + rider + " " + coordinates),
                // After the slot rows.
                text.Replace(arrivalRow + "\n", "") + arrivalRow + "\n",
                // Too few, too many, and invalid coordinates.
                WithCoordinates("0 0 0 9 0"), WithCoordinates("0 0 0 9 0 0 0"), WithCoordinates("0 0 0 9 0 "),
                WithCoordinates("NaN 0 0 9 0 0"), WithCoordinates("0 Infinity 0 9 0 0"), WithCoordinates("0 0 -Infinity 9 0 0"),
                WithCoordinates("0 0 0 1E+39 0 0"), WithCoordinates("0 0 0 0x10 0 0"), WithCoordinates("0 0 0 9,5 0 0"),
                WithCoordinates("0 0 0 1,000 0 0"), WithCoordinates("0 0 0 (9) 0 0"), WithCoordinates("0 0 0 9 0 0.00000000000000001") })
            {
                var refused = false;
                try { MountedRetainedIntentRecord.Decode(malformed); } catch (System.IO.InvalidDataException) { refused = true; }
                TestRunner.True(refused, "A malformed version 2 record was accepted: " + malformed.Replace("\n", "/"));
            }

            // Not canonical: a coordinate is read as any finite decimal number of bounded length (as the integer fields
            // accept leading zeros). Its value is what counts; it is written back in the one form the writer uses.
            var loose = MountedRetainedIntentRecord.Decode(WithCoordinates("+0 0.0 0 009 0E0 1e1"));
            TestRunner.True(loose.Pairs.Single().Arrival.MountX == 9f && loose.Pairs.Single().Arrival.MountZ == 10f &&
                loose.Encode().Split('\n')[6] == "arrival " + rider + " " + mount + " 0 0 0 9 0 10", "A non-canonical coordinate was not read as its value.");

            // Bounded: the member bound is unchanged. Six pairs with sixty control rows fit as version 1; their six
            // arrival rows would not. The save then writes the version 1 text (ordinary retained intent after loading)
            // rather than losing the record, and the strict encoder still refuses the oversized text.
            var pairs = new List<MountedRetainedIntentPairRecord>();
            var slots = new List<MountedRetainedIntentSlotRecord>();
            for (var i = 0; i < MountedRetainedIntentRecord.MaximumPairs; i++)
                pairs.Add(PairRow(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"),
                    new MountedRetainedArrivalPlacement(SaveArea, i, 0f, 0f, i + 9f, 0f, 0f)));
            for (var i = 0; i < 60; i++)
                slots.Add(SlotRow(i % 2 == 0 ? pairs[i % 6].RiderId : pairs[i % 6].MountId, 100 + i / 6, MountedControlSlotPolicy.SavedMountState));
            var full = Record(pairs.ToArray());
            full.Slots = MountedRetainedIntentRecord.RecordableSlots(slots, pairs);
            TestRunner.Equal(60, full.Slots.Length, "The bound case did not hold sixty distinct control rows.");
            var overBound = false;
            try { full.Encode(); } catch (System.IO.InvalidDataException) { overBound = true; }
            TestRunner.True(overBound, "The strict encoder wrote a text beyond the member bound.");
            var saved = full.EncodeForSave();
            var reread = MountedRetainedIntentRecord.Decode(saved);
            TestRunner.True(saved.Length <= MountedRetainedIntentRecord.MaximumCharacters && saved.StartsWith(MountedRetainedIntentRecord.Header + "\n", StringComparison.Ordinal) &&
                reread.Pairs.Length == 6 && reread.Slots.Length == 60 && reread.Pairs.All(pair => pair.Arrival == null),
                "A record whose arrival rows exceed the bound was not saved as its version 1 text.");
            // Within the bound the arrival rows are written, for every pair that has one.
            full.Slots = full.Slots.Take(12).ToArray();
            var within = MountedRetainedIntentRecord.Decode(full.EncodeForSave());
            TestRunner.True(within.Pairs.Length == 6 && within.Pairs.All(pair => pair.Arrival != null) &&
                within.Pairs.Select((pair, i) => pair.Arrival.RiderX == i && pair.Arrival.MountX == i + 9f).All(same => same),
                "Six arrival placements within the bound did not round-trip to their own pairs.");
        }
    }
}
