using System;
using System.Globalization;
using System.Text;

namespace KingmakerMountedCombat.Domain
{
    public enum CompanionNameVerdict
    {
        Accepted,
        Empty,
        TooLong,
        ControlCharacter,
        MarkupCharacter
    }

    // PET-01: the typed name is stored exactly as the player reads it. Nothing is escaped or rewritten for a renderer;
    // instead the few characters the native labels would interpret are refused up front, so every consumer shows it as text.
    public static class CompanionNamePolicy
    {
        // Visible characters (text elements), so an accented or combined letter counts once.
        public const int MaxTextElements = 24;
        public const int MaxUtf16Length = 64;
        // < and > open TextMeshPro rich-text tags in every native label and log line; { and } open the game's text templates.
        private const string MarkupCharacters = "<>{}";

        // Trims the ends and collapses each run of whitespace to one space. Returns Accepted with the stored name, or the reason.
        public static CompanionNameVerdict Normalize(string input, out string name)
        {
            name = null;
            if (input == null) return CompanionNameVerdict.Empty;
            var builder = new StringBuilder(input.Length);
            var pendingSpace = false;
            for (var index = 0; index < input.Length; index++)
            {
                var character = input[index];
                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= input.Length || !char.IsLowSurrogate(input[index + 1])) return CompanionNameVerdict.ControlCharacter;
                    AppendPending(builder, ref pendingSpace);
                    builder.Append(character).Append(input[index + 1]);
                    index++;
                    continue;
                }
                if (char.IsLowSurrogate(character)) return CompanionNameVerdict.ControlCharacter;
                if (char.IsWhiteSpace(character)) { pendingSpace = builder.Length > 0; continue; }
                if (char.IsControl(character) || IsInvisibleFormat(character)) return CompanionNameVerdict.ControlCharacter;
                if (MarkupCharacters.IndexOf(character) >= 0) return CompanionNameVerdict.MarkupCharacter;
                AppendPending(builder, ref pendingSpace);
                builder.Append(character);
            }
            if (builder.Length == 0) return CompanionNameVerdict.Empty;
            var normalized = builder.ToString();
            if (normalized.Length > MaxUtf16Length || new StringInfo(normalized).LengthInTextElements > MaxTextElements)
                return CompanionNameVerdict.TooLong;
            name = normalized;
            return CompanionNameVerdict.Accepted;
        }

        private static void AppendPending(StringBuilder builder, ref bool pendingSpace)
        {
            if (pendingSpace) builder.Append(' ');
            pendingSpace = false;
        }

        // Format characters draw nothing (zero-width spaces, bidirectional overrides). Joiners stay: scripts and emoji need them.
        private static bool IsInvisibleFormat(char character) =>
            CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.Format &&
            character != '‌' && character != '‍';
    }

    // The facts that make one unit a renameable companion, read at open and again at confirmation.
    public sealed class CompanionRenameSubject
    {
        public string UnitId { get; set; }
        public string MasterId { get; set; }
        public bool IsPet { get; set; }
        public bool MasterOwnsThisPet { get; set; }
        public bool MasterIsPlayerCharacter { get; set; }
        public bool IsPlayerFaction { get; set; }
        public bool IsSummoned { get; set; }
    }

    public static class CompanionRenameEligibility
    {
        // Null when the unit is a permanent animal companion the player owns; otherwise the reason it is refused.
        public static string Refusal(CompanionRenameSubject subject)
        {
            if (subject == null || string.IsNullOrEmpty(subject.UnitId)) return "no unit";
            if (!subject.IsPet) return "not a companion animal";
            if (subject.IsSummoned) return "a temporary summon";
            if (string.IsNullOrEmpty(subject.MasterId) || !subject.MasterOwnsThisPet) return "not its master's companion";
            if (!subject.MasterIsPlayerCharacter || !subject.IsPlayerFaction) return "not the player's companion";
            return null;
        }

        // Confirmation renames only the exact unit the dialog opened for, still owned by the same master.
        public static string ConfirmationRefusal(CompanionRenameSubject opened, CompanionRenameSubject current)
        {
            if (opened == null || current == null) return "the companion is gone";
            if (!string.Equals(opened.UnitId, current.UnitId, StringComparison.Ordinal)) return "a different unit";
            if (!string.Equals(opened.MasterId, current.MasterId, StringComparison.Ordinal)) return "the companion changed master";
            return Refusal(current);
        }
    }
}
