using System;

namespace KingmakerMountedCombat.Domain
{
    // Presentation only: unknown feedback, including every refusal/failure, stays visible.
    public static class MountedFeedbackPresentationPolicy
    {
        public static bool IsRoutine(string feedback)
        {
            if (string.IsNullOrEmpty(feedback)) return true;
            switch (feedback)
            {
                case "Mounted combat is idle.":
                case "Ready to mount when the selected rider is eligible.":
                case "Ready to mount.":
                case "Mounted relationship is active.":
                case "Mounted pair attack completed.":
                case "Mounted door interaction completed.":
                case "Mounted ground movement completed.":
                    return true;
            }
            return feedback.StartsWith("PASS: state=", StringComparison.Ordinal) ||
                feedback.StartsWith("Mounted pair command accepted: ", StringComparison.Ordinal) ||
                feedback.StartsWith("Mounted stock attack intent accepted: ", StringComparison.Ordinal) ||
                feedback.StartsWith("Mounted ground movement active: ", StringComparison.Ordinal) ||
                feedback.StartsWith("Mounted door interaction accepted: ", StringComparison.Ordinal);
        }
    }
}
