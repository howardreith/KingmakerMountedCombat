using System;
using System.Reflection;
using Kingmaker;
using Kingmaker.Controllers.Clicks;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Original KMC diagnostic input adapter. Draft for the discriminating input
    // experiment; no attack mode, resource or command state is assigned here.
    internal sealed class NativeOrdinaryAttackInput : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly PropertyInfo PointerOn = typeof(PointerController).GetProperty("PointerOn", Flags);
        private static readonly PropertyInfo WorldPosition = typeof(PointerController).GetProperty("WorldPosition", Flags);
        private static readonly FieldInfo SimulatedPosition = typeof(PointerController).GetField("m_WorldPositionForSimulation", Flags);
        private static readonly FieldInfo SimulatedHandler = typeof(PointerController).GetField("m_SimulateClickHandler", Flags);
        private static readonly FieldInfo MouseHandler = typeof(PointerController).GetField("m_MouseDownHandler", Flags);
        private static readonly MethodInfo Predictions = typeof(TurnController).GetMethod("UpdateActionPredictions", Flags);
        private readonly PointerController pointer;
        private readonly UnitEntityData target;
        private readonly Vector3 point;
        private readonly object[] saved;
        private bool disposed;

        internal NativeOrdinaryAttackInput(UnitEntityData target) : this(target,
            target?.View != null ? target.Position : throw new ArgumentException("An exact visible fixture target is required.", nameof(target))) { }

        internal NativeOrdinaryAttackInput(Vector3 point) : this(null, point) { }

        private NativeOrdinaryAttackInput(UnitEntityData target, Vector3 point)
        {
            if (Predictions == null || Predictions.MetadataToken != 0x06000C6E ||
                PointerOn?.GetSetMethod(true)?.MetadataToken != 0x060093B2 ||
                WorldPosition?.GetSetMethod(true)?.MetadataToken != 0x060093B4 ||
                SimulatedPosition?.MetadataToken != 0x04005EAD ||
                SimulatedHandler?.MetadataToken != 0x04005EAC || MouseHandler?.MetadataToken != 0x04005EAB)
                throw new MissingMemberException("Exact native pointer prediction contract changed.");
            this.target = target;
            this.point = point;
            pointer = Game.Instance.DefaultPointerController;
            saved = new[] { PointerOn.GetValue(pointer, null), WorldPosition.GetValue(pointer, null),
                SimulatedPosition.GetValue(pointer), SimulatedHandler.GetValue(pointer), MouseHandler.GetValue(pointer) };
            try
            {
                PointerOn.SetValue(pointer, target?.View.gameObject, null);
                WorldPosition.SetValue(pointer, point, null);
                SimulatedPosition.SetValue(pointer, point);
                pointer.UpdateSelectedClickHandler();
                var handler = SimulatedHandler.GetValue(pointer);
                if (target != null ? !(handler is ClickUnitHandler) : !(handler is ClickGroundHandler))
                    throw new InvalidOperationException("Native pointer priority did not select the requested fixture handler.");
            }
            catch { Dispose(); throw; }
        }

        internal void Predict()
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            if (turn == null) return;
            turn.OnHoverObjectChanged(target?.View.gameObject, null);
            if (target != null) turn.OnHoverObjectChanged(null, target.View.gameObject);
            else turn.UpdatePredictionsOnCursorMoved();
            Predictions.Invoke(turn, null);
        }

        internal bool Click(bool respectNativeAdmission = true, int button = 0)
        {
            var turn = Game.Instance.TurnBasedCombatController.CurrentTurn;
            if (respectNativeAdmission && button == 0 && turn != null && turn.IgnoreClick()) return false;
            var handler = (IClickEventHandler)SimulatedHandler.GetValue(pointer);
            return handler.OnClick(target?.View.gameObject, point, button, false, false);
        }

        public void Dispose()
        {
            if (disposed) return;
            PointerOn.SetValue(pointer, saved[0], null);
            WorldPosition.SetValue(pointer, saved[1], null);
            SimulatedPosition.SetValue(pointer, saved[2]);
            SimulatedHandler.SetValue(pointer, saved[3]);
            MouseHandler.SetValue(pointer, saved[4]);
            disposed = true;
        }
    }
}
