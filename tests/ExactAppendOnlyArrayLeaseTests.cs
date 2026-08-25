using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class ExactAppendOnlyArrayLeaseTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("append lease preserves exact source order", PreservesExactSourceOrder);
            runner.Run("append lease restores only exact owned value", RestoresOnlyExactOwnedValue);
            runner.Run("append lease restore is idempotent", RestoreIsIdempotent);
            runner.Run("append lease rejects duplicate exact item", RejectsDuplicateExactItem);
            runner.Run("append lease rejects third-party mutation", RejectsThirdPartyMutation);
        }

        private static void PreservesExactSourceOrder()
        {
            var first = new object();
            var second = new object();
            var item = new object();
            var lease = new ExactAppendOnlyArrayLease<object>(new[] { first, second }, item);
            var appended = lease.CreateAppendedValue();
            TestRunner.Equal(3, appended.Length, "Append length changed.");
            TestRunner.True(ReferenceEquals(first, appended[0]), "First source reference changed.");
            TestRunner.True(ReferenceEquals(second, appended[1]), "Second source reference changed.");
            TestRunner.True(ReferenceEquals(item, appended[2]), "Item was not appended exactly once at the end.");
        }

        private static void RestoresOnlyExactOwnedValue()
        {
            var first = new object();
            var item = new object();
            var lease = new ExactAppendOnlyArrayLease<object>(new[] { first }, item);
            object[] restored;
            string error;
            TestRunner.True(lease.TryRestore(lease.CreateAppendedValue(), out restored, out error), error);
            TestRunner.Equal(1, restored.Length, "Restored length differs.");
            TestRunner.True(ReferenceEquals(first, restored[0]), "Restored source reference differs.");
        }

        private static void RestoreIsIdempotent()
        {
            var first = new object();
            var lease = new ExactAppendOnlyArrayLease<object>(new[] { first }, new object());
            object[] restored;
            string error;
            TestRunner.True(lease.TryRestore(lease.CreateOriginalValue(), out restored, out error), error);
            TestRunner.True(lease.MatchesOriginal(restored), "Idempotent restore did not preserve the original value.");
        }

        private static void RejectsDuplicateExactItem()
        {
            var item = new object();
            var threw = false;
            try { new ExactAppendOnlyArrayLease<object>(new[] { item }, item); }
            catch (ArgumentException) { threw = true; }
            TestRunner.True(threw, "Duplicate exact append item was accepted.");
        }

        private static void RejectsThirdPartyMutation()
        {
            var lease = new ExactAppendOnlyArrayLease<object>(new[] { new object() }, new object());
            object[] restored;
            string error;
            TestRunner.True(!lease.TryRestore(new[] { new object(), new object() }, out restored, out error),
                "Third-party mutation was overwritten.");
            TestRunner.True(restored == null && !string.IsNullOrEmpty(error), "Rejected restore did not report its ambiguity.");
        }
    }
}
