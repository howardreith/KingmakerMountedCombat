using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Integration;
using KingmakerMountedCombat.Domain;
using Newtonsoft.Json;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimeAutomationHost
    {
        // Json.NET otherwise coerces strings/numbers into Boolean properties.
        // This checks only the new top-level token before either host reader.
        private static void AssertWorkingObservationToken(string json)
        {
            bool seen = false;
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.PropertyName || reader.Depth != 1 ||
                        !string.Equals((string)reader.Value, "observeWorkingDeserialization", StringComparison.OrdinalIgnoreCase)) continue;
                    if (seen || !string.Equals((string)reader.Value, "observeWorkingDeserialization", StringComparison.Ordinal) ||
                        !reader.Read() || reader.TokenType != JsonToken.Boolean || !(bool)reader.Value)
                        throw new InvalidOperationException("Working observation opt-in must be one exact true JSON Boolean.");
                    seen = true;
                }
            }
        }
        private WorkingLoadDiagnosticSession workingLoadObservation;
        private bool workingLoadCompletionPending, workingLoadPollDeadlineReached;
        private string workingLoadCompletionStatus;
        private IReadOnlyList<string> workingLoadCompletionErrors;
        private double WorkingLoadPollDeadline => HorseCompanionScenarioDeadlinePolicy.HostDeadlineSeconds(false, 0d);

        private void BeginWorkingLoadObservation(Game game, SaveManager manager, SaveInfo descriptor)
        {
            if (request.ObserveWorkingDeserialization != true || workingLoadObservation != null)
                throw new InvalidOperationException("Working observer admission is absent or already owned.");
            // The exact guarded loader invokes this only after the second hash
            // and descriptor verification and before its one callback/load.
            workingLoadObservation = new WorkingLoadDiagnosticSession(request, game, manager, descriptor);
            workingLoadObservation.Install();
        }
        private bool WorkingObservationNativeBusy()
        {
            var loading = LoadingProcess.Instance;
            return loading != null && (loading.IsLoadingInProcess || loading.QueuedNames.Any()) ||
                NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending ||
                persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence;
        }
        private void BeginWorkingLoadCompletion(string status, IReadOnlyList<string> errors)
        {
            if (!workingLoadCompletionPending || status == "FAIL")
            {
                workingLoadCompletionStatus = status;
                workingLoadCompletionErrors = errors == null ? new string[0] : errors.ToArray();
            }
            workingLoadCompletionPending = true;
        }
        private void DrainWorkingLoadCompletion()
        {
            if (!workingLoadCompletionPending || workingLoadObservation == null || workingLoadPollDeadlineReached) return;
            var busy = WorkingObservationNativeBusy();
            if (!workingLoadObservation.TryRelease(busy, saveAuthorization.AuthorizedLoadCount))
            {
                workingLoadObservation.Pending(fixtureLoader?.LoadCallbackObserved == true, fixtureIdentityVerified,
                    busy, elapsedSeconds, WorkingLoadPollDeadline);
                if (elapsedSeconds >= WorkingLoadPollDeadline)
                {
                    workingLoadPollDeadlineReached = true;
                    logger.Warning("Working observer polling deadline reached with custody retained; no cleanup or completion is asserted.");
                }
                return;
            }
            workingLoadCompletionPending = false;
            CompleteSaveBackedSafely(workingLoadCompletionStatus ?? "FAIL", workingLoadCompletionErrors);
        }
        private void RequireWorkingObserverReleasedForDispose()
        {
            if (workingLoadObservation == null || workingLoadObservation.Released) return;
            BeginWorkingLoadCompletion("FAIL", new[] { "Host disposal requested while Working observer custody was retained." });
            if (!workingLoadObservation.TryRelease(WorkingObservationNativeBusy(), saveAuthorization.AuthorizedLoadCount))
                throw new InvalidOperationException("Working observer, callbacks and exact native task remain owned; disposal is not complete.");
        }
        private void PublishWorkingObservation(string status)
        {
            workingLoadObservation?.PublishFinal(status, fixtureLoader?.LoadCallbackObserved == true,
                fixtureIdentityVerified, saveAuthorization.AuthorizedLoadCount, saveAuthorization.AuthorizedWriteCount);
        }
    }
}
