using System;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPlayerActionOverlay : MonoBehaviour
    {
        private const float Width = 330f;
        private const float Height = 126f;
        private MountedPlayerActionController controller;

        public void Configure(MountedPlayerActionController actionController)
        {
            controller = actionController ?? throw new ArgumentNullException(nameof(actionController));
        }

        public void Deconfigure()
        {
            controller = null;
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            try
            {
                var availability = controller.GetAvailability();
                if (!availability.IsVisible)
                {
                    return;
                }

                var left = Math.Max(10f, Screen.width - Width - 18f);
                var top = Math.Max(10f, Screen.height - Height - 18f);
                var panel = new Rect(left, top, Width, Height);
                GUI.Box(panel, "Kingmaker Mounted Combat");

                var priorEnabled = GUI.enabled;
                GUI.enabled = availability.IsEnabled;
                if (GUI.Button(new Rect(left + 12f, top + 27f, Width - 24f, 32f), availability.Label))
                {
                    controller.ObserveOverlayButtonActivation();
                    controller.Activate();
                }
                GUI.enabled = priorEnabled;

                var feedback = availability.IsEnabled
                    ? controller.LastFeedback
                    : availability.Feedback;
                GUI.Label(new Rect(left + 12f, top + 64f, Width - 24f, 54f), feedback);
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    controller.ObserveOverlayRepaint(availability, panel, feedback);
                }
            }
            catch (Exception exception)
            {
                controller.HandleOverlayFailure(exception);
            }
        }
    }
}
