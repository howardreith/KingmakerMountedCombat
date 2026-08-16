using System;
using System.Reflection;
using Kingmaker.UI.SettingsUI;

namespace KingmakerMountedCombat.Diagnostics
{
    /// <summary>
    /// Exercises the exact Kingmaker settings callback -> EventBus path without
    /// invoking the CurrentValue setter or writing SettingsProvider/PlayerPrefs.
    /// The cached value and persisted value are restored and verified exactly.
    /// </summary>
    internal sealed class NativeModeTransitionProbe : IDisposable
    {
        private const int ExpectedCachedFieldToken = 0x04002275;
        private const int ExpectedInvokeCallbackToken = 0x06003359;
        private static readonly Guid ExpectedKingmakerMvid = new Guid("07fa1e4d-8618-41b3-9b8d-faa17d3b26f7");

        private readonly SettingsEntityBool setting;
        private readonly FieldInfo cachedField;
        private readonly bool? originalRawCache;
        private readonly string persistedValueBefore;
        private bool temporaryAttempted;
        private bool restoreCompleted;
        private bool disposed;

        public NativeModeTransitionProbe()
        {
            setting = SettingsRoot.Instance == null ? null : SettingsRoot.Instance.EnableTurnBasedMode;
            if (setting == null)
            {
                throw new InvalidOperationException("Exact EnableTurnBasedMode setting is unavailable.");
            }

            var assembly = typeof(SettingsEntityBool).Assembly;
            if (assembly.ManifestModule.ModuleVersionId != ExpectedKingmakerMvid)
            {
                throw new InvalidOperationException("Exact Kingmaker Assembly-CSharp MVID mismatch for native mode probe.");
            }

            cachedField = typeof(SettingsEntityBool).GetField(
                "m_Cached",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var callbackMethod = typeof(SettingsEntityBase).GetMethod(
                "OnInvokeUpdateCallback",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null);
            if (cachedField == null || cachedField.MetadataToken != ExpectedCachedFieldToken ||
                cachedField.FieldType != typeof(bool?) || callbackMethod == null ||
                callbackMethod.MetadataToken != ExpectedInvokeCallbackToken)
            {
                throw new MissingMemberException("Exact Kingmaker turn-based cached-setting callback contract changed.");
            }
            if (setting.OnOptionUpdatedCallback == null)
            {
                throw new InvalidOperationException("GameSettingsController has not registered the native turn-based callback.");
            }

            originalRawCache = (bool?)cachedField.GetValue(setting);
            persistedValueBefore = setting.GetSavedValueString();
            OriginalValue = setting.CurrentValue;
            TemporaryValue = !OriginalValue;
        }

        public bool OriginalValue { get; }

        public bool TemporaryValue { get; }

        public bool OriginalRawCacheHadValue => originalRawCache.HasValue;

        public string PersistedValueBefore => persistedValueBefore;

        public string PersistedValueAfter { get; private set; }

        public bool PersistedValueUnchanged { get; private set; }

        public bool TemporaryDeliveryAttempted => temporaryAttempted;

        public bool TemporaryValueIsCurrent => temporaryAttempted && setting.CurrentValue == TemporaryValue;

        public bool RestoreDeliveryCompleted => restoreCompleted;

        public void DispatchTemporaryValue()
        {
            ThrowIfDisposed();
            if (temporaryAttempted)
            {
                throw new InvalidOperationException("Native mode temporary value was already dispatched.");
            }

            temporaryAttempted = true;
            cachedField.SetValue(setting, (bool?)TemporaryValue);
            setting.OnInvokeUpdateCallback();
        }

        public void DispatchRestoreAndRestoreRawCache()
        {
            ThrowIfDisposed();
            if (!temporaryAttempted)
            {
                throw new InvalidOperationException("Native mode restore cannot precede temporary delivery.");
            }
            if (restoreCompleted)
            {
                throw new InvalidOperationException("Native mode restore was already dispatched.");
            }

            try
            {
                cachedField.SetValue(setting, (bool?)OriginalValue);
                setting.OnInvokeUpdateCallback();
                restoreCompleted = true;
            }
            finally
            {
                cachedField.SetValue(setting, originalRawCache);
                PersistedValueAfter = setting.GetSavedValueString();
                PersistedValueUnchanged = string.Equals(
                    persistedValueBefore,
                    PersistedValueAfter,
                    StringComparison.Ordinal);
            }

            if (!PersistedValueUnchanged)
            {
                throw new InvalidOperationException("Native mode diagnostic callback changed the persisted settings value.");
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Exception restoreFailure = null;
            try
            {
                if (temporaryAttempted && !restoreCompleted)
                {
                    DispatchRestoreAndRestoreRawCache();
                }
                else
                {
                    cachedField.SetValue(setting, originalRawCache);
                }
            }
            catch (Exception exception)
            {
                restoreFailure = exception;
                cachedField.SetValue(setting, originalRawCache);
            }
            finally
            {
                disposed = true;
            }

            if (restoreFailure != null)
            {
                throw new InvalidOperationException("Native mode probe could not restore the exact cached setting state.", restoreFailure);
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(NativeModeTransitionProbe));
            }
        }
    }
}
