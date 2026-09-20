using System;
using KingmakerMountedCombat.Domain;
using UnityEngine;

namespace KingmakerMountedCombat.Integration
{
    internal sealed class MountedPlayerActionOverlay : MonoBehaviour
    {
        private const float Width = 330f;
        private const float Height = 194f;
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
                    if (availability.Action == MountedPlayerActionKind.Mount)
                    {
                        controller.ArmMountTargetFromOverlay();
                    }
                    else
                    {
                        controller.ObserveOverlayButtonActivation();
                        controller.Activate();
                    }
                }
                GUI.enabled = priorEnabled;

                var feedbackTop = top + 64f;
                var feedbackHeight = 54f;
                if (availability.Action == MountedPlayerActionKind.Mount)
                {
                    var fallbackPriorEnabled = GUI.enabled;
                    GUI.enabled = availability.IsEnabled;
                    if (GUI.Button(new Rect(left + 12f, top + 63f, Width - 24f, 28f), "Mount active companion (fallback)"))
                    {
                        controller.ObserveOverlayButtonActivation();
                        controller.Activate();
                    }
                    GUI.enabled = fallbackPriorEnabled;
                    feedbackTop = top + 99f;
                    feedbackHeight = 72f;
                }
                else if (controller.CombatActionsVisible)
                {
                    var combatPriorEnabled = GUI.enabled;
                    GUI.enabled = true;
                    if (GUI.Button(new Rect(left + 12f, top + 63f, 148f, 28f), "Rider melee"))
                    {
                        controller.ArmRiderPrimaryFromOverlay();
                    }
                    if (GUI.Button(new Rect(left + 170f, top + 63f, 148f, 28f), controller.MountPrimaryLabel))
                    {
                        controller.ArmCombatActionFromOverlay(MountedCombatActionKind.MountPrimaryNatural);
                    }
                    GUI.enabled = combatPriorEnabled;
                    feedbackTop = top + 94f;
                    feedbackHeight = 54f;
                }

                var feedback = controller.CombatActionsVisible
                    ? controller.CombatFeedback
                    : availability.IsEnabled
                        ? controller.LastFeedback
                        : availability.Feedback;
                GUI.Label(new Rect(left + 12f, feedbackTop, Width - 24f, feedbackHeight), feedback);
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
