using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Tests
{
    internal sealed class TestRunner
    {
        private readonly List<string> failures = new List<string>();
        private int passed;

        public int Run(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("PASS " + name);
            }
            catch (Exception exception)
            {
                failures.Add(name + ": " + exception.Message);
                Console.WriteLine("FAIL " + name + ": " + exception.Message);
            }

            return failures.Count;
        }

        public int Complete()
        {
            Console.WriteLine("TOTAL PASS=" + passed + " FAIL=" + failures.Count);
            return failures.Count == 0 ? 0 : 1;
        }

        public static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual);
            }
        }

        public static void True(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
