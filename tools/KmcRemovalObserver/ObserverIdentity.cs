namespace KmcRemovalObserver
{
    // The observer's own version. It is deliberately independent of the KMC
    // product version: the binding that matters is the commit both packages are
    // built from, which the launcher checks between the two package manifests.
    internal static class ObserverIdentity
    {
        internal const string ProductVersion = "0.1.0-observer.1";
    }
}
