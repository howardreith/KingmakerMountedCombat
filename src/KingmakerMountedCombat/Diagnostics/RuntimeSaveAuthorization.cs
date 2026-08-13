using System;
using System.Collections.Generic;
using System.IO;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum RuntimeSaveOperation
    {
        Load,
        Write
    }

    internal sealed class RuntimeSaveTarget
    {
        public string InternalName { get; set; }

        public string FileName { get; set; }

        public string FullPath { get; set; }

        public string SaveType { get; set; }

        public string GameId { get; set; }

        public string GameName { get; set; }

        public string Area { get; set; }
    }

    internal sealed class RuntimeSaveAuthorizationDecision
    {
        public RuntimeSaveAuthorizationDecision(bool allowed, bool fatalViolation, string reason)
        {
            Allowed = allowed;
            FatalViolation = fatalViolation;
            Reason = reason;
        }

        public bool Allowed { get; }

        public bool FatalViolation { get; }

        public string Reason { get; }
    }

    internal sealed class RuntimeSaveAuthorization
    {
        private const string ManualSaveType = "Manual";
        private readonly object sync = new object();
        private AuthorizationScope activeScope;
        private int generation;
        private int fatalViolationCount;
        private string lastFatalViolation;

        public event Action<string> FatalViolation;

        public bool IsActive
        {
            get
            {
                lock (sync)
                {
                    return activeScope != null;
                }
            }
        }

        public int FatalViolationCount
        {
            get
            {
                lock (sync)
                {
                    return fatalViolationCount;
                }
            }
        }

        public string LastFatalViolation
        {
            get
            {
                lock (sync)
                {
                    return lastFatalViolation;
                }
            }
        }

        public IDisposable Activate(RuntimeFixtureIdentity fixture, string saveRoot, bool allowWorkingWrites)
        {
            if (fixture == null)
            {
                throw new ArgumentNullException(nameof(fixture));
            }

            var errors = fixture.Validate();
            if (errors.Count != 0)
            {
                throw new ArgumentException("Fixture identity is invalid: " + string.Join("; ", errors), nameof(fixture));
            }

            var normalizedRoot = NormalizeRoot(saveRoot);
            var scope = new AuthorizationScope(
                normalizedRoot,
                fixture.Baseline.FileName,
                fixture.Working.FileName,
                fixture.Working.InternalName,
                fixture.Working.GameId,
                fixture.Working.GameName,
                fixture.Working.Area,
                allowWorkingWrites);

            lock (sync)
            {
                if (activeScope != null)
                {
                    throw new InvalidOperationException("Runtime save authorization is already active.");
                }

                activeScope = scope;
                generation++;
                fatalViolationCount = 0;
                lastFatalViolation = null;
                return new AuthorizationLease(this, generation);
            }
        }

        public RuntimeSaveAuthorizationDecision Authorize(RuntimeSaveOperation operation, RuntimeSaveTarget target, string observedSaveRoot)
        {
            AuthorizationScope scope;
            lock (sync)
            {
                scope = activeScope;
            }

            if (scope == null)
            {
                return new RuntimeSaveAuthorizationDecision(true, false, "Runtime automation save authorization is inactive.");
            }

            var rejection = ValidateActiveRequest(scope, operation, target, observedSaveRoot);
            if (rejection == null)
            {
                return new RuntimeSaveAuthorizationDecision(true, false, "Exact KMC Working save target authorized.");
            }

            ReportFatal(rejection);
            return new RuntimeSaveAuthorizationDecision(false, true, rejection);
        }

        public void ReportBoundaryFailure(RuntimeSaveOperation operation, string reason)
        {
            ReportFatalViolation(operation, "mounted cleanup failed: " + reason);
        }

        public void ReportFatalViolation(RuntimeSaveOperation operation, string reason)
        {
            lock (sync)
            {
                if (activeScope == null)
                {
                    return;
                }
            }

            ReportFatal("Blocked " + OperationName(operation) + ": " + reason);
        }

        private static string ValidateActiveRequest(AuthorizationScope scope, RuntimeSaveOperation operation, RuntimeSaveTarget target, string observedSaveRoot)
        {
            if (target == null)
            {
                return "Blocked " + OperationName(operation) + ": SaveInfo was null.";
            }

            string normalizedObservedRoot;
            string normalizedTargetPath;
            try
            {
                normalizedObservedRoot = NormalizeRoot(observedSaveRoot);
                normalizedTargetPath = NormalizeFilePath(target.FullPath);
            }
            catch (Exception exception)
            {
                return "Blocked " + OperationName(operation) + ": save path normalization failed (" + exception.GetType().Name + ").";
            }

            if (!string.Equals(normalizedObservedRoot, scope.SaveRoot, StringComparison.OrdinalIgnoreCase))
            {
                return "Blocked " + OperationName(operation) + ": observed SaveManager.SavePath differs from the activated fixture root.";
            }

            if (!string.Equals(target.InternalName, scope.WorkingInternalName, StringComparison.Ordinal))
            {
                return "Blocked " + OperationName(operation) + ": internal save name is not exact Working.";
            }

            if (!string.Equals(target.FileName, scope.WorkingFileName, StringComparison.Ordinal))
            {
                return "Blocked " + OperationName(operation) + ": filename is not the exact canonical Working leaf.";
            }

            var expectedPath = NormalizeFilePath(Path.Combine(scope.SaveRoot, scope.WorkingFileName));
            var targetParent = NormalizeRoot(Path.GetDirectoryName(normalizedTargetPath));
            if (!string.Equals(targetParent, scope.SaveRoot, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(normalizedTargetPath, expectedPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Blocked " + OperationName(operation) + ": target is not the direct Working child of SaveManager.SavePath.";
            }

            if (string.Equals(target.FileName, scope.BaselineFileName, StringComparison.OrdinalIgnoreCase))
            {
                return "Blocked " + OperationName(operation) + ": Baseline is immutable.";
            }

            if (!string.Equals(target.SaveType, ManualSaveType, StringComparison.Ordinal))
            {
                return "Blocked " + OperationName(operation) + ": save type is not Manual.";
            }

            if (!string.Equals(target.GameId, scope.GameId, StringComparison.Ordinal) ||
                !string.Equals(target.GameName, scope.GameName, StringComparison.Ordinal) ||
                !string.Equals(target.Area, scope.Area, StringComparison.Ordinal))
            {
                return "Blocked " + OperationName(operation) + ": GameId, GameName, or Area differs from the validated fixture identity.";
            }

            if (operation == RuntimeSaveOperation.Write && !scope.AllowWorkingWrites)
            {
                return "Blocked write: this runtime request did not expressly authorize Working writes.";
            }

            return null;
        }

        private static string NormalizeRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Save root is required.", nameof(path));
            }

            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrEmpty(fullPath) || string.Equals(fullPath, Path.GetPathRoot(fullPath), StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Save root must be a bounded directory below a filesystem root.", nameof(path));
            }

            return fullPath;
        }

        private static string NormalizeFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Save file path is required.", nameof(path));
            }

            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.IsNullOrEmpty(Path.GetFileName(fullPath)))
            {
                throw new ArgumentException("Save file path must include a leaf filename.", nameof(path));
            }

            return fullPath;
        }

        private static string OperationName(RuntimeSaveOperation operation)
        {
            return operation == RuntimeSaveOperation.Write ? "write" : "load";
        }

        private void ReportFatal(string reason)
        {
            Action<string> reporter;
            lock (sync)
            {
                fatalViolationCount++;
                lastFatalViolation = reason;
                reporter = FatalViolation;
            }

            if (reporter == null)
            {
                return;
            }

            foreach (Action<string> subscriber in reporter.GetInvocationList())
            {
                try
                {
                    subscriber(reason);
                }
                catch
                {
                    // Authorization remains fail-closed even when a telemetry observer fails.
                }
            }
        }

        private void Release(int leaseGeneration)
        {
            lock (sync)
            {
                if (activeScope != null && generation == leaseGeneration)
                {
                    activeScope = null;
                }
            }
        }

        private sealed class AuthorizationScope
        {
            public AuthorizationScope(
                string saveRoot,
                string baselineFileName,
                string workingFileName,
                string workingInternalName,
                string gameId,
                string gameName,
                string area,
                bool allowWorkingWrites)
            {
                SaveRoot = saveRoot;
                BaselineFileName = baselineFileName;
                WorkingFileName = workingFileName;
                WorkingInternalName = workingInternalName;
                GameId = gameId;
                GameName = gameName;
                Area = area;
                AllowWorkingWrites = allowWorkingWrites;
            }

            public string SaveRoot { get; }

            public string BaselineFileName { get; }

            public string WorkingFileName { get; }

            public string WorkingInternalName { get; }

            public string GameId { get; }

            public string GameName { get; }

            public string Area { get; }

            public bool AllowWorkingWrites { get; }
        }

        private sealed class AuthorizationLease : IDisposable
        {
            private RuntimeSaveAuthorization owner;
            private readonly int leaseGeneration;

            public AuthorizationLease(RuntimeSaveAuthorization owner, int leaseGeneration)
            {
                this.owner = owner;
                this.leaseGeneration = leaseGeneration;
            }

            public void Dispose()
            {
                var currentOwner = owner;
                if (currentOwner == null)
                {
                    return;
                }

                owner = null;
                currentOwner.Release(leaseGeneration);
            }
        }
    }
}
