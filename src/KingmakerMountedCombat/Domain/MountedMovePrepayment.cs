using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>Preserves movement paid before native mount preparation. Never creates an allowance.</summary>
    public sealed class MountedMovePrepayment
    {
        private object mount;
        private object controller;
        private int nativeRound;
        private long nativeRoundStart;
        private bool mountPrepared;
        private float prepaidMove;

        public bool Owns(object actor) { return mount != null && ReferenceEquals(mount, actor); }

        public void ObserveEpoch(object exactMount, object nativeController, int round, long roundStart)
        {
            if (exactMount == null || nativeController == null) { throw new ArgumentNullException(); }
            if (ReferenceEquals(mount, exactMount) && ReferenceEquals(controller, nativeController) &&
                nativeRound == round && nativeRoundStart == roundStart) { return; }
            mount = exactMount;
            controller = nativeController;
            nativeRound = round;
            nativeRoundStart = roundStart;
            mountPrepared = false;
            prepaidMove = 0f;
        }

        public void RecordPhysicalMove(float before, float after)
        {
            if (mount == null || float.IsNaN(before) || float.IsNaN(after) ||
                float.IsInfinity(before) || float.IsInfinity(after) || before < 0f || after < before)
                throw new InvalidOperationException("Invalid native mounted movement accounting.");
            if (!mountPrepared) { prepaidMove += after - before; }
        }

        public float ReconcileNativePreparation(float nativeMove)
        {
            if (nativeMove < 0f || float.IsNaN(nativeMove) || float.IsInfinity(nativeMove))
                throw new InvalidOperationException("Invalid prepared native movement accounting.");
            if (mountPrepared) { return nativeMove; }
            mountPrepared = true;
            var reconciled = Math.Max(nativeMove, prepaidMove);
            prepaidMove = 0f;
            return reconciled;
        }
    }
}
