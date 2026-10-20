using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace KingmakerMountedCombat.Diagnostics
{
    // A distinct read-only cold input. The historical Working identity is never
    // changed or reused as this separately qualified assisted campaign.
    public sealed class Chunk7AssistedColdInventorySelection
    {
        public const string Scenario = "chunk7-assisted-cold-inventory";
        public const string CaseName = "assisted-manual-readonly-inventory";
        public const string SourceLeaf = "assisted-cold-source.json";
        public const string ManualName = "KMC_C7_ASSISTED_FIXTURE";
        // Owner-supplied seed. Its exact internal and file names come from the
        // reviewed harness registry (data, so a new seed needs no product rebuild);
        // the product admits them generically and never as the producer fixture.
        public const string OwnerSeedCaseName = "owner-seed-readonly-inventory";
        internal bool IsOwnerSeed => Case == OwnerSeedCaseName;
        public const string SmallFixtureCaseName = "small-fixture-manual-readonly-inventory";
        internal bool IsSmallFixture => Case == SmallFixtureCaseName;
        // Kingmaker save names legitimately contain spaces (the engine writes them as
        // underscores only in the file name). Single interior spaces only: no leading,
        // trailing or repeated space, no tab, newline or other whitespace, at most 64
        // characters, first character alphanumeric and last a name character.
        internal const string OwnerSeedNamePattern = "^(?=[\\s\\S]{1,64}\\z)[A-Za-z0-9][A-Za-z0-9_.-]*(?: [A-Za-z0-9_.-]+)*\\z";
        internal static bool IsOwnerSeedName(string value) =>
            value != null && Regex.IsMatch(value, OwnerSeedNamePattern, RegexOptions.CultureInvariant) && value != ManualName;
        internal static bool IsOwnerSeedLeaf(string value) =>
            value != null && Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9_.-]{0,95}\\.zks$") &&
            !value.EndsWith("_" + ManualName + ".zks", StringComparison.Ordinal);
        internal static bool IsManualLeaf(string value)
        {
            const string prefix = "Manual_", suffix = "_KMC_C7_ASSISTED_FIXTURE.zks";
            if (value == null || value.Length <= prefix.Length + suffix.Length || !value.StartsWith(prefix, StringComparison.Ordinal) || !value.EndsWith(suffix, StringComparison.Ordinal)) return false;
            var digits = value.Substring(prefix.Length, value.Length - prefix.Length - suffix.Length);
            return int.TryParse(digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) && number > 0 &&
                value == prefix + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + suffix;
        }
        public string Case { get; set; }
        public string SourceRunId { get; set; }
        public string SourceArtifactSha256 { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public bool BaseGameOnly { get; set; }
        public int ExpectedNativeLoadCount { get; set; }
        public int ExpectedNativeWriteCount { get; set; }
        public string SeedId { get; set; }
        public string SeedSha256 { get; set; }
        // The producer shape keeps its exact seven keys; the owner-seed shape
        // carries its two seed pins instead of the three producer pins.
        public bool ShouldSerializeSourceRunId() => !IsOwnerSeed || SourceRunId != null;
        public bool ShouldSerializeSourceArtifactSha256() => !IsOwnerSeed || SourceArtifactSha256 != null;
        public bool ShouldSerializeSourceArchiveSha256() => !IsOwnerSeed || SourceArchiveSha256 != null;
        public bool ShouldSerializeSeedId() => SeedId != null;
        public bool ShouldSerializeSeedSha256() => SeedSha256 != null;

        public IReadOnlyList<string> Validate(string scenario, string runId)
        {
            if (IsOwnerSeed) return ValidateOwnerSeed(scenario, runId);
            var errors = new List<string>();
            if (scenario != Scenario || Case != CaseName && Case != SmallFixtureCaseName) errors.Add("Assisted cold inventory requires its exact closed read-only family.");
            if (!Regex.IsMatch(SourceRunId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SourceRunId == runId)
                errors.Add("A distinct completed assisted fixture source is required.");
            foreach (var value in new[] { SourceArtifactSha256, SourceArchiveSha256 })
                if (!Regex.IsMatch(value ?? "", "^[a-f0-9]{64}$")) errors.Add("Exact immutable assisted fixture artifact/archive hashes are required.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 0)
                errors.Add("Assisted cold inventory requires base-only staging, one native load and zero writes.");
            if (SeedId != null || SeedSha256 != null) errors.Add("A producer cold selection cannot carry owner-seed pins.");
            return errors;
        }
        private IReadOnlyList<string> ValidateOwnerSeed(string scenario, string runId)
        {
            var errors = new List<string>();
            if (scenario != Scenario) errors.Add("Owner-seed cold inventory requires its exact closed read-only family.");
            if (SourceRunId != null || SourceArtifactSha256 != null || SourceArchiveSha256 != null)
                errors.Add("Owner-seed cold inventory has no producer run, artifact or archive pins.");
            if (!Regex.IsMatch(SeedId ?? "", "^[A-Za-z0-9][A-Za-z0-9._-]{0,95}$") || SeedId == runId)
                errors.Add("An exact registered owner seed identity is required.");
            if (!Regex.IsMatch(SeedSha256 ?? "", "^[a-f0-9]{64}$")) errors.Add("An exact owner seed SHA256 is required.");
            if (!BaseGameOnly || ExpectedNativeLoadCount != 1 || ExpectedNativeWriteCount != 0)
                errors.Add("Owner-seed cold inventory requires base-only staging, one native load and zero writes.");
            return errors;
        }
    }

    // Pure classification of an owner-seed load outcome from the exact observed
    // names and messages. It never infers a cause the evidence does not name.
    internal static class Chunk7OwnerSeedFailureClassifier
    {
        internal const string SeedDependency = "seed-dependency", KmcOrLoader = "kmc-or-loader",
            RunnerOrAuthority = "runner-or-authority", Unclassified = "unclassified", None = "none";
        private static readonly string[] NativeAssemblies = { "Assembly-CSharp", "Assembly-CSharp-firstpass", "mscorlib", "System", "System.Core", "UnityEngine", "UnityEngine.CoreModule", "Newtonsoft.Json" };
        private static readonly Regex[] TypePatterns = {
            new Regex("Error resolving type specified in JSON '([^']{1,512})'", RegexOptions.CultureInvariant),
            new Regex("Could not convert string '([^']{1,512})' to dictionary key type 'System\\.Type'", RegexOptions.CultureInvariant),
            new Regex("Type specified in JSON '([^']{1,512})' is not compatible", RegexOptions.CultureInvariant) };
        private static readonly Regex AssemblyPattern = new Regex("Could not load (?:assembly|file or assembly) '([^',]{1,256})", RegexOptions.CultureInvariant);
        private static readonly Regex FindTypePattern = new Regex("Could not find type '([^']{1,512})' in assembly '([^',]{1,256})", RegexOptions.CultureInvariant);

        // "Namespace.Type, Assembly[, Version=...]" -> (type, simple assembly name).
        internal static bool TrySplitTypeName(string value, out string typeName, out string assemblyName)
        {
            typeName = null; assemblyName = null;
            if (string.IsNullOrWhiteSpace(value) || value.Length > 512) return false;
            var comma = value.IndexOf(',');
            if (comma <= 0) return false;
            typeName = value.Substring(0, comma).Trim();
            var rest = value.Substring(comma + 1); var next = rest.IndexOf(',');
            assemblyName = (next < 0 ? rest : rest.Substring(0, next)).Trim();
            return typeName.Length > 0 && assemblyName.Length > 0;
        }
        internal static bool IsNativeAssembly(string assemblyName) => Array.IndexOf(NativeAssemblies, assemblyName) >= 0;
        internal static bool IsKmcAssembly(string assemblyName) => assemblyName == "KingmakerMountedCombat";

        // Exact type/assembly names named by serializer or loader messages.
        internal static List<string> NamedAssemblies(IEnumerable<string> messages, List<string> namedTypes)
        {
            var assemblies = new List<string>();
            foreach (var message in messages ?? new string[0])
            {
                if (message == null) continue;
                foreach (var pattern in TypePatterns)
                    foreach (Match match in pattern.Matches(message))
                        if (TrySplitTypeName(match.Groups[1].Value, out var type, out var assembly))
                        { Add(namedTypes, type + ", " + assembly); Add(assemblies, assembly); }
                foreach (Match match in FindTypePattern.Matches(message))
                { Add(namedTypes, match.Groups[1].Value + ", " + match.Groups[2].Value.Trim()); Add(assemblies, match.Groups[2].Value.Trim()); }
                foreach (Match match in AssemblyPattern.Matches(message)) Add(assemblies, match.Groups[1].Value.Trim());
            }
            return assemblies;
        }
        private static void Add(List<string> list, string value) { if (!string.IsNullOrEmpty(value) && !list.Contains(value) && list.Count < 256) list.Add(value); }

        internal const int StackBound = 4000;
        internal static string BoundStack(string stack) => stack == null ? null : stack.Length > StackBound ? stack.Substring(0, StackBound) : stack;
        // A managed frame of the KMC assembly (Mono "  at KingmakerMountedCombat.X.Y (...)",
        // optionally behind a "(wrapper ...)" marker).
        private static readonly Regex KmcFrame = new Regex("(?m)^\\s*at\\s+(?:\\(wrapper [^)]{1,64}\\)\\s+)?KingmakerMountedCombat\\.", RegexOptions.CultureInvariant);
        internal static bool HasKmcFrame(string stack) => !string.IsNullOrEmpty(stack) && KmcFrame.IsMatch(stack);
        internal const int FrameLineCapacity = 8;
        // The KMC frame lines of a FULL stack. Storage bounds (BoundStack) apply only
        // after this verdict, so a deep KMC frame is never lost to truncation.
        internal static string[] KmcFrameLines(string stack)
        {
            if (string.IsNullOrEmpty(stack)) return new string[0];
            var lines = new List<string>();
            foreach (var line in stack.Split('\n'))
                if (KmcFrame.IsMatch(line) && lines.Count < FrameLineCapacity)
                { var value = line.TrimEnd('\r'); lines.Add(value.Length > 512 ? value.Substring(0, 512) : value); }
            return lines.ToArray();
        }
        // Every exception of the chain (inner exceptions included), each full stack.
        internal static string[] KmcFrameLines(Exception error)
        {
            var lines = new List<string>(); var depth = 0;
            for (var current = error; current != null && depth < 8; current = current.InnerException, depth++)
                foreach (var line in KmcFrameLines(current.StackTrace)) if (lines.Count < FrameLineCapacity) lines.Add(line);
            return lines.ToArray();
        }

        internal sealed class Decision { internal string Label, DecidedBy; }

        // declaredForeignTypes: archive-declared types whose assembly is absent;
        // unresolvedBlueprints: native library misses; messages: exact logged/loader errors;
        // stacks: KMC frame evidence taken from the full stacks of the observed
        // exceptions (never the harness's own assertion); assertionIds: the stable
        // identifiers of the harness assertions that fired (Chunk7OwnerSeedAssertions).
        // Precedence: a runner/authority refusal decides first, whatever else was
        // observed; then a KMC stack frame; then a KMC-invariant assertion; then a
        // seed-content assertion; any other assertion is unclassified (a non-refusal
        // assertion never reaches seed-dependency through names); only without any
        // assertion, or with only the native-load-incomplete outcome, do the names decide.
        // An incomplete load reaches seed-dependency only when a foreign assembly is
        // named: a blueprint miss records nothing about who requested it, and a KMC
        // fault can leave no KMC frame, so blueprint misses alone are unclassified.
        internal static string Classify(IEnumerable<string> declaredForeignTypes, IEnumerable<string> unresolvedBlueprints,
            IEnumerable<string> messages, bool harnessRefused, IEnumerable<string> stacks = null, IEnumerable<string> assertionIds = null) =>
            Decide(declaredForeignTypes, unresolvedBlueprints, messages, harnessRefused, stacks, assertionIds).Label;

        internal static Decision Decide(IEnumerable<string> declaredForeignTypes, IEnumerable<string> unresolvedBlueprints,
            IEnumerable<string> messages, bool harnessRefused, IEnumerable<string> stacks = null, IEnumerable<string> assertionIds = null)
        {
            if (harnessRefused) return new Decision { Label = RunnerOrAuthority, DecidedBy = "runner-or-authority-refusal" };
            if ((stacks ?? new string[0]).Any(HasKmcFrame)) return new Decision { Label = KmcOrLoader, DecidedBy = "kmc-stack-frame" };
            var ids = (assertionIds ?? new string[0]).Where(value => value != null).Distinct(StringComparer.Ordinal).ToArray();
            var invariant = ids.FirstOrDefault(value => Chunk7OwnerSeedAssertions.KindOf(value) == Chunk7OwnerSeedAssertions.Kind.KmcInvariant);
            if (invariant != null) return new Decision { Label = KmcOrLoader, DecidedBy = "assertion:" + invariant };
            var names = ClassifyNames(declaredForeignTypes, unresolvedBlueprints, messages, out var kmcNamed, out var foreignAssemblyNamed);
            var content = ids.FirstOrDefault(value => Chunk7OwnerSeedAssertions.KindOf(value) == Chunk7OwnerSeedAssertions.Kind.SeedContent);
            if (content != null) return new Decision { Label = kmcNamed ? Unclassified : SeedDependency, DecidedBy = "assertion:" + content };
            var other = ids.FirstOrDefault(value => Chunk7OwnerSeedAssertions.KindOf(value) != Chunk7OwnerSeedAssertions.Kind.LoadOutcome);
            if (other != null) return new Decision { Label = Unclassified, DecidedBy = "assertion:" + other };
            if (ids.Length > 0 && names == SeedDependency && !foreignAssemblyNamed)
                return new Decision { Label = Unclassified, DecidedBy = "assertion:" + ids[0] + ";blueprint-misses-only" };
            return new Decision { Label = names, DecidedBy = names == None ? "none" : "named-evidence" };
        }

        private static string ClassifyNames(IEnumerable<string> declaredForeignTypes, IEnumerable<string> unresolvedBlueprints,
            IEnumerable<string> messages, out bool kmc, out bool foreignAssembly)
        {
            var namedTypes = new List<string>();
            var assemblies = NamedAssemblies(messages, namedTypes);
            foreach (var declared in declaredForeignTypes ?? new string[0])
                if (TrySplitTypeName(declared, out var type, out var assembly)) Add(assemblies, assembly);
            foreignAssembly = assemblies.Any(value => !IsNativeAssembly(value) && !IsKmcAssembly(value));
            var foreign = foreignAssembly || (unresolvedBlueprints ?? new string[0]).Any();
            var list = (messages ?? new string[0]).Where(value => value != null).ToList();
            // A KMC type/assembly name is decisive; a loader failure alone is a
            // consequence that a foreign name explains.
            kmc = assemblies.Any(IsKmcAssembly) || list.Any(value => value.IndexOf("KingmakerMountedCombat", StringComparison.Ordinal) >= 0);
            var loader = list.Any(value => value.IndexOf("WorkingFixtureLoader", StringComparison.Ordinal) >= 0);
            if (foreign && !kmc) return SeedDependency;
            if ((kmc || loader) && !foreign) return KmcOrLoader;
            if (foreign) return Unclassified;
            return list.Count == 0 ? None : Unclassified;
        }
    }

    // Stable identifiers of the owner-seed harness assertions and what each asserts.
    // The mapping is explicit; message text never decides a label. An identifier not
    // listed here (including Unidentified) is neither a KMC invariant nor seed content
    // and can only be unclassified.
    internal static class Chunk7OwnerSeedAssertions
    {
        internal enum Kind { Other, KmcInvariant, SeedContent, LoadOutcome }
        internal const string AssertionIdKey = "KingmakerMountedCombat.HarnessAssertionId";
        // KMC invariants: a breach is a KMC (or KMC observer) fault, never the seed's.
        internal const string MountedCustody = "kmc-mounted-custody";
        internal const string NativeEffects = "kmc-native-write-cast-restoration";
        internal const string NativeRestorationEntries = "kmc-native-restoration-entries";
        internal const string MetadataNotMissing = "kmc-metadata-not-missing";
        internal const string ObserverFault = "kmc-observer-fault";
        internal const string ObserverNotInstalled = "kmc-observer-not-installed";
        // The seed's own content: lost or unresolved content, DLC.
        internal const string SeedLostContent = "seed-lost-content";
        internal const string SeedDlcUnavailable = "seed-dlc-unavailable";
        // The header was verified against the registry before launch; a post-load
        // mismatch can come from a transition or a harness error, so it claims no cause.
        internal const string SeedIdentityMismatch = "seed-identity-mismatch";
        // The native load did not complete: an outcome that claims no cause either way,
        // so the observed evidence (frames, then names) decides the label.
        internal const string NativeLoadIncomplete = "native-load-incomplete";
        // Everything else the harness asserts.
        internal const string GameLoggedException = "game-logged-exception";
        internal const string DeclaredTypeScan = "declared-type-scan-error";
        internal const string NativeStructure = "native-structure-bound";
        internal const string ControlState = "native-control-state";
        internal const string Qualification = "owner-seed-qualification";
        internal const string Unidentified = "unidentified";
        private static readonly Dictionary<string, Kind> Kinds = new Dictionary<string, Kind>(StringComparer.Ordinal) {
            [MountedCustody] = Kind.KmcInvariant, [NativeEffects] = Kind.KmcInvariant, [NativeRestorationEntries] = Kind.KmcInvariant,
            [MetadataNotMissing] = Kind.KmcInvariant, [ObserverFault] = Kind.KmcInvariant, [ObserverNotInstalled] = Kind.KmcInvariant,
            [SeedLostContent] = Kind.SeedContent, [SeedDlcUnavailable] = Kind.SeedContent,
            [NativeLoadIncomplete] = Kind.LoadOutcome,
            [SeedIdentityMismatch] = Kind.Other, [GameLoggedException] = Kind.Other, [DeclaredTypeScan] = Kind.Other, [NativeStructure] = Kind.Other, [ControlState] = Kind.Other,
            [Qualification] = Kind.Other, [Unidentified] = Kind.Other };
        internal static Kind KindOf(string id) => id != null && Kinds.TryGetValue(id, out var kind) ? kind : Kind.Other;
        internal static bool IsKnown(string id) => id != null && Kinds.ContainsKey(id);
    }

    // The declared-type scan records a null error on success. A null string assigned to
    // a JObject property is a JValue of type String whose value is null (it serializes
    // as null), so callers pass (string)declared["error"]: only non-null text is a failure.
    internal static class Chunk7OwnerSeedScan
    {
        internal static bool Failed(string errorText) => errorText != null;
    }

    // Exception-safe observation: an observer body runs inside Run, so a fault in it
    // is recorded and never escapes into the observed game call. Run has no
    // return value and touches nothing of the caller, so the original method's
    // arguments, result and control flow are unchanged. Any recorded fault fails
    // qualification under the named reason below.
    internal sealed class Chunk7OwnerSeedObserverFaults
    {
        internal const string Reason = "Observer fault";
        internal const int Capacity = 16;
        internal sealed class Fault { internal string Hook, ExceptionType, Message, Stack; internal string[] KmcFrameLines = new string[0]; }
        private readonly object sync = new object();
        private readonly List<Fault> faults = new List<Fault>();
        private int count;
        internal int Count { get { lock (sync) return count; } }
        internal Fault[] Snapshot() { lock (sync) return faults.ToArray(); }
        // KMC frame lines decided on each fault's full stack (Stack itself is bounded storage).
        internal string[] KmcFrameLines() { lock (sync) return faults.SelectMany(value => value.KmcFrameLines).ToArray(); }
        internal void Run(string hook, Action body)
        {
            try { body(); }
            catch (Exception error) { Record(hook, error); }
        }
        internal void Record(string hook, Exception error)
        {
            try
            {
                lock (sync)
                {
                    count++;
                    if (faults.Count < Capacity)
                        faults.Add(new Fault { Hook = hook, ExceptionType = error?.GetType().FullName,
                            Message = error?.Message == null ? null : error.Message.Length > 2048 ? error.Message.Substring(0, 2048) : error.Message,
                            Stack = Chunk7OwnerSeedFailureClassifier.BoundStack(error?.StackTrace),
                            KmcFrameLines = Chunk7OwnerSeedFailureClassifier.KmcFrameLines(error) });
                }
            }
            catch { }
        }
        // The named qualification failure, or null when no observer body faulted.
        internal string QualificationFailure()
        {
            lock (sync)
                return count == 0 ? null : Reason + ": " + count + " observation hook fault(s): " +
                    string.Join(" | ", faults.Select(value => value.Hook + " " + value.ExceptionType + ": " + value.Message));
        }
    }
}
