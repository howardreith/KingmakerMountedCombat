using KingmakerMountedCombat.Integration;

namespace KingmakerMountedCombat.Tests
{
    internal static class ReactiveBooleanValueReaderTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("reactive boolean reader accepts Kingmaker-style public fields", ReadsPublicField);
            runner.Run("reactive boolean reader retains public-property compatibility", ReadsPublicProperty);
            runner.Run("reactive boolean reader rejects missing or non-boolean values", RejectsUnavailableValues);
        }

        private static void ReadsPublicField()
        {
            var owner = new FieldBackedOwner();
            TestRunner.Equal("True", ReactiveBooleanValueReader.Read(owner, "Active"), "Public reactive field was unavailable.");
            TestRunner.Equal("False", ReactiveBooleanValueReader.Read(owner, "CanUseAbilities"), "False public reactive field changed value.");
        }

        private static void ReadsPublicProperty()
        {
            var owner = new PropertyBackedOwner();
            TestRunner.Equal("True", ReactiveBooleanValueReader.Read(owner, "Active"), "Public reactive property compatibility was lost.");
        }

        private static void RejectsUnavailableValues()
        {
            var owner = new FieldBackedOwner();
            TestRunner.Equal(ReactiveBooleanValueReader.Unavailable, ReactiveBooleanValueReader.Read(null, "Active"), "Null owner was accepted.");
            TestRunner.Equal(ReactiveBooleanValueReader.Unavailable, ReactiveBooleanValueReader.Read(owner, "Missing"), "Missing member was accepted.");
            TestRunner.Equal(ReactiveBooleanValueReader.Unavailable, ReactiveBooleanValueReader.Read(owner, "NotBoolean"), "Non-boolean reactive value was accepted.");
        }

        private sealed class ReactiveBoolean
        {
            public ReactiveBoolean(bool value)
            {
                Value = value;
            }

            public bool Value { get; }
        }

        private sealed class ReactiveString
        {
            public string Value => "not-boolean";
        }

        private sealed class FieldBackedOwner
        {
            public readonly ReactiveBoolean Active = new ReactiveBoolean(true);
            public readonly ReactiveBoolean CanUseAbilities = new ReactiveBoolean(false);
            public readonly ReactiveString NotBoolean = new ReactiveString();
        }

        private sealed class PropertyBackedOwner
        {
            public ReactiveBoolean Active { get; } = new ReactiveBoolean(true);
        }
    }
}
