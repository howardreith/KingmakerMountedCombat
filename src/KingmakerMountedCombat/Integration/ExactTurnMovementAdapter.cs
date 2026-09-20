using System;
using System.Reflection;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal static class ExactTurnMovementAdapter
    {
        private const int TickMovementToken = 0x06000C37;
        private static readonly MethodInfo TickMovementMethod = ResolveTickMovement();

        public static void Tick(TurnController turn, ref float deltaTime)
        {
            if (turn == null)
            {
                throw new ArgumentNullException(nameof(turn));
            }
            var arguments = new object[] { deltaTime, false };
            TickMovementMethod.Invoke(turn, arguments);
            deltaTime = (float)arguments[0];
        }

        private static MethodInfo ResolveTickMovement()
        {
            var method = typeof(TurnController).GetMethod(
                "TickMovement",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(float).MakeByRefType(), typeof(bool) },
                null);
            if (method == null || method.MetadataToken != TickMovementToken || method.ReturnType != typeof(void))
            {
                throw new MissingMethodException(typeof(TurnController).FullName, "TickMovement exact token " + TickMovementToken.ToString("X8"));
            }
            return method;
        }
    }
}
