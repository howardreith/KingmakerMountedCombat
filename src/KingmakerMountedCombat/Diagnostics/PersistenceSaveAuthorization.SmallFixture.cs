using System;
using System.IO;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class PersistenceSaveAuthorization
    {
        private RuntimeRequest smallFixtureRequest;
        private object smallFixtureGame, smallFixtureManager;
        private string smallFixtureKey;
        private int smallFixtureNumber, smallFixtureReservations;
        internal int SmallFixtureNumber { get { lock (sync) return smallFixtureNumber; } }
        internal int SmallFixtureReservations { get { lock (sync) return smallFixtureReservations; } }
        internal string SmallFixtureOutputLeaf { get { lock (sync) return smallFixtureReservations == 1
            ? smallFixtureRequest.SmallFixture.SelectedManualLeaf(smallFixtureNumber) : null; } }

        private static string SmallFixtureKey(RuntimeRequest request)
        {
            if (request?.Scenario != Chunk7SmallFixtureSelection.Scenario || request.SmallFixture == null ||
                request.Validate().Count != 0 || !Chunk7SmallFixtureSelection.IsSource(request.PersistenceLoad)) return null;
            // Only writable public properties of the closed request DTO graph.
            // No serializer, game object, computed getter or external dependency
            // belongs at this storage authority boundary.
            var key = new System.Text.StringBuilder("small-fixture-request-v1;");
            var count = 0;
            return AppendSmallFixtureKey(key, request, 0, new System.Collections.Generic.List<object>(), ref count)
                ? key.ToString() : null;
        }
        private static bool AppendSmallFixtureKey(System.Text.StringBuilder key, object value, int depth,
            System.Collections.Generic.List<object> stack, ref int count)
        {
            if (++count > 512 || depth > 8) return false;
            if (value == null) { key.Append("N;"); return true; }
            if (value is string)
            {
                var text = (string)value; if (text.Length > 16384) return false;
                key.Append('S').Append(text.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(text);
                return true;
            }
            if (value is bool) { key.Append((bool)value ? "B1;" : "B0;"); return true; }
            if (value is int) { key.Append('I').Append(((int)value).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';'); return true; }
            if (value is long) { key.Append('L').Append(((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(';'); return true; }
            if (stack.Any(item => ReferenceEquals(item, value))) return false;
            var type = value.GetType();
            var allowed = type == typeof(RuntimeRequest) || type == typeof(RuntimeFixtureIdentity) ||
                type == typeof(RuntimeSaveDescriptor) || type == typeof(RuntimeSaveWriteAuthorization) ||
                type == typeof(RuntimeQualificationSuiteIdentity) || type == typeof(Chunk7SmallFixtureSelection) ||
                type == typeof(Chunk7StockMammothFixtureSelection);
            if (!allowed) return false;
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();
            key.Append('O').Append(type.Name.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(type.Name)
                .Append(';').Append(properties.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('{');
            stack.Add(value);
            try
            {
                foreach (var property in properties)
                {
                    key.Append(property.Name.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(':').Append(property.Name);
                    if (!AppendSmallFixtureKey(key, property.GetValue(value, null), depth + 1, stack, ref count)) return false;
                }
            }
            finally { stack.RemoveAt(stack.Count - 1); }
            key.Append('}'); return true;
        }
        private bool SmallFixtureUnchanged() => smallFixtureRequest == null ||
            smallFixtureKey != null && smallFixtureKey == SmallFixtureKey(smallFixtureRequest);
        private bool SmallFixtureInput(RuntimeSaveDescriptor source)
        {
            return Chunk7SmallFixtureSelection.IsSource(source) && source.GameId == gameId && source.GameName == gameName &&
                entries.TryGetValue(source.FileName, out var input) && input.Name == source.InternalName && input.Type == "Manual" &&
                input.Area == source.Area && input.Hash == source.Sha256 && !input.Writable && !input.Writing &&
                !input.Bootstrap && !input.AdmitsBeforeArea && input.AdmissionArea == null &&
                input.ForeignGameId == null && input.ForeignGameName == null;
        }
        internal void ConfigureSmallFixture(RuntimeRequest request, object game, object manager)
        {
            lock (sync)
            {
                var key = SmallFixtureKey(request);
                if (smallFixtureRequest != null || key == null || game == null || manager == null ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 ||
                    admittedNativeLoadRequests != 0 || admittedNativeWriteRequests != 0 || entries.Count != 1 ||
                    declaresBootstrap || bootstrapWindowOpen || acquiredManualRequest != null || ownedPairsRequest != null ||
                    assistedRequest != null || assistedColdRequest != null || progressionManualRequest != null ||
                    !SmallFixtureInput(request.PersistenceLoad))
                    throw new InvalidOperationException("F09 fixture requires an unused exact one-source one-load one-Manual plan.");
                smallFixtureRequest = request; smallFixtureGame = game; smallFixtureManager = manager; smallFixtureKey = key;
            }
        }
        internal bool MatchesSmallFixturePlan(RuntimeRequest request, object game, object manager)
        {
            lock (sync)
            {
                if (!ReferenceEquals(request, smallFixtureRequest) || !ReferenceEquals(game, smallFixtureGame) ||
                    !ReferenceEquals(manager, smallFixtureManager) || !SmallFixtureUnchanged() ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 ||
                    admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1 ||
                    entries.Count != 1 + smallFixtureReservations || smallFixtureReservations > 1 ||
                    !SmallFixtureInput(request.PersistenceLoad)) return false;
                if (smallFixtureReservations == 0) return admittedNativeWriteRequests == 0;
                return entries.TryGetValue(request.SmallFixture.SelectedManualLeaf(smallFixtureNumber), out var output) &&
                    output.Name == request.SmallFixture.SelectedOutputName && output.Type == "Manual" &&
                    output.Area == request.PersistenceLoad.Area && output.Writable && !output.Bootstrap &&
                    !output.AdmitsBeforeArea && output.AdmissionArea == null && output.ForeignGameId == null && output.ForeignGameName == null &&
                    (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
            }
        }
        internal void ReserveSmallFixtureOutput(RuntimeRequest request, object game, object manager, int observedNumber)
        {
            lock (sync)
            {
                if (!MatchesSmallFixturePlan(request, game, manager) || smallFixtureReservations != 0 ||
                    admittedNativeLoadRequests != 1 || admittedNativeWriteRequests != 0)
                    throw new InvalidOperationException("F09 Manual reservation requires its settled native load and unused write cap.");
                var leaf = request.SmallFixture.SelectedManualLeaf(observedNumber);
                var paths = Directory.EnumerateFileSystemEntries(Root).ToArray();
                if (paths.Length != 1 || paths[0] != Path.Combine(Root, request.PersistenceLoad.FileName) || Directory.Exists(paths[0]))
                    throw new InvalidOperationException("F09 isolated source inventory changed before native numbering.");
                var output = new Entry { Name = request.SmallFixture.SelectedOutputName, Type = "Manual",
                    Area = request.PersistenceLoad.Area, Writable = true };
                VerifyFile(leaf, output);
                entries.Add(leaf, output); smallFixtureNumber = observedNumber; smallFixtureReservations = 1;
            }
        }
        private string SmallFixtureAdmission(RuntimeSaveOperation operation)
        {
            if (smallFixtureRequest == null) return null;
            if (!SmallFixtureUnchanged()) return "F09 captured request changed.";
            if (operation == RuntimeSaveOperation.Load) return null;
            return operation == RuntimeSaveOperation.Write && smallFixtureReservations == 1 && admittedNativeLoadRequests == 1
                ? null : "F09 permits only its reserved native Manual after the sole native load.";
        }
    }
}
