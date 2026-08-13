using System;

namespace KingmakerMountedCombat.Logging
{
    internal interface IModLogger
    {
        void Info(string message);

        void Warning(string message);

        void Error(string message);

        void Exception(string context, Exception exception);
    }
}
