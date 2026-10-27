// Facade for the Small serialization check: the Small calibration selection validates the
// acquired Manual leaf shape through this type; the real source would pull the whole
// acquisition selection family into a check about Small DTO projection only.
namespace KingmakerMountedCombat.Diagnostics
{
    internal static class Chunk7AcquiredManualSelection
    {
        public const string OutputName = "KMC_C7_ACQUIRED_FIELD";
        internal static string NativeOutputLeaf(int number)
        {
            if (number <= 0) throw new System.ArgumentOutOfRangeException(nameof(number));
            return "Manual_" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + OutputName + ".zks";
        }
    }
}