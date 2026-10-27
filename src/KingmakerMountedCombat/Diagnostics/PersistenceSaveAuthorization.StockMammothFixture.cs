using System;
using System.IO;
using System.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class PersistenceSaveAuthorization
    {
        private RuntimeRequest stockMammothFixtureRequest;
        private object stockMammothFixtureGame, stockMammothFixtureManager;
        private string stockMammothFixtureKey;
        private int stockMammothFixtureNumber, stockMammothFixtureReservations;
        internal int StockMammothFixtureNumber { get { lock (sync) return stockMammothFixtureNumber; } }
        internal int StockMammothFixtureReservations { get { lock (sync) return stockMammothFixtureReservations; } }
        internal string StockMammothFixtureOutputLeaf { get { lock (sync) return stockMammothFixtureReservations == 1
            ? Chunk7StockMammothFixtureSelection.ManualLeaf(stockMammothFixtureNumber) : null; } }

        private static string StockMammothFixtureKey(RuntimeRequest request)
        {
            if (request?.Scenario != Chunk7StockMammothFixtureSelection.Scenario || request.StockMammothFixture == null ||
                request.Validate().Count != 0 || !Chunk7StockMammothFixtureSelection.IsSource(request.PersistenceLoad)) return null;
            var key = new System.Text.StringBuilder("stock-mammoth-fixture-request-v1;");
            var count = 0;
            return AppendSmallFixtureKey(key, request, 0, new System.Collections.Generic.List<object>(), ref count)
                ? key.ToString() : null;
        }
        private bool StockMammothFixtureUnchanged() => stockMammothFixtureRequest == null ||
            stockMammothFixtureKey != null && stockMammothFixtureKey == StockMammothFixtureKey(stockMammothFixtureRequest);
        internal void ConfigureStockMammothFixture(RuntimeRequest request, object game, object manager)
        {
            lock (sync)
            {
                var key = StockMammothFixtureKey(request);
                if (stockMammothFixtureRequest != null || key == null || game == null || manager == null ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 ||
                    admittedNativeLoadRequests != 0 || admittedNativeWriteRequests != 0 || entries.Count != 1 ||
                    declaresBootstrap || bootstrapWindowOpen || smallFixtureRequest != null || acquiredManualRequest != null ||
                    ownedPairsRequest != null || assistedRequest != null || assistedColdRequest != null ||
                    progressionManualRequest != null || !SmallFixtureInput(request.PersistenceLoad))
                    throw new InvalidOperationException("Stock Mammoth fixture requires an unused exact clean source, one load and one Manual.");
                stockMammothFixtureRequest = request; stockMammothFixtureGame = game;
                stockMammothFixtureManager = manager; stockMammothFixtureKey = key;
            }
        }
        internal bool MatchesStockMammothFixturePlan(RuntimeRequest request, object game, object manager)
        {
            lock (sync)
            {
                if (!ReferenceEquals(request, stockMammothFixtureRequest) || !ReferenceEquals(game, stockMammothFixtureGame) ||
                    !ReferenceEquals(manager, stockMammothFixtureManager) || !StockMammothFixtureUnchanged() ||
                    maximumNativeLoadRequests != 1 || maximumNativeWriteRequests != 1 ||
                    admittedNativeLoadRequests > 1 || admittedNativeWriteRequests > 1 ||
                    entries.Count != 1 + stockMammothFixtureReservations || stockMammothFixtureReservations > 1 ||
                    !SmallFixtureInput(request.PersistenceLoad)) return false;
                if (stockMammothFixtureReservations == 0) return admittedNativeWriteRequests == 0;
                return entries.TryGetValue(Chunk7StockMammothFixtureSelection.ManualLeaf(stockMammothFixtureNumber), out var output) &&
                    output.Name == Chunk7StockMammothFixtureSelection.OutputName && output.Type == "Manual" &&
                    output.Area == request.PersistenceLoad.Area && output.Writable && !output.Bootstrap &&
                    !output.AdmitsBeforeArea && output.AdmissionArea == null && output.ForeignGameId == null && output.ForeignGameName == null &&
                    (output.Hash == null && !output.Writing || admittedNativeWriteRequests == 1);
            }
        }
        internal void ReserveStockMammothFixtureOutput(RuntimeRequest request, object game, object manager, int observedNumber)
        {
            lock (sync)
            {
                if (!MatchesStockMammothFixturePlan(request, game, manager) || stockMammothFixtureReservations != 0 ||
                    admittedNativeLoadRequests != 1 || admittedNativeWriteRequests != 0)
                    throw new InvalidOperationException("Stock Mammoth Manual reservation requires its settled native load and unused write cap.");
                var leaf = Chunk7StockMammothFixtureSelection.ManualLeaf(observedNumber);
                var paths = Directory.EnumerateFileSystemEntries(Root).ToArray();
                if (paths.Length != 1 || paths[0] != Path.Combine(Root, request.PersistenceLoad.FileName) || Directory.Exists(paths[0]))
                    throw new InvalidOperationException("Stock Mammoth isolated source changed before native numbering.");
                var output = new Entry { Name = Chunk7StockMammothFixtureSelection.OutputName, Type = "Manual",
                    Area = request.PersistenceLoad.Area, Writable = true };
                VerifyFile(leaf, output);
                entries.Add(leaf, output); stockMammothFixtureNumber = observedNumber; stockMammothFixtureReservations = 1;
            }
        }
        private string StockMammothFixtureAdmission(RuntimeSaveOperation operation)
        {
            if (stockMammothFixtureRequest == null) return null;
            if (!StockMammothFixtureUnchanged()) return "Stock Mammoth captured request changed.";
            if (operation == RuntimeSaveOperation.Load) return null;
            return operation == RuntimeSaveOperation.Write && stockMammothFixtureReservations == 1 && admittedNativeLoadRequests == 1
                ? null : "Stock Mammoth fixture permits only its reserved native Manual after the sole load.";
        }
    }
}
