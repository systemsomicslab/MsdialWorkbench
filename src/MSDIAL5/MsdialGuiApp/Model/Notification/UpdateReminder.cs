using System;
using System.Globalization;

namespace CompMs.App.Msdial.Model.Notification
{
    /// <summary>
    /// What the user answered to the update notification.
    /// </summary>
    public enum UpdateNotificationChoice
    {
        OpenReleasePage,
        RemindLater,
        SkipThisVersion,
    }

    /// <summary>
    /// Decides whether the update notification is held back by an earlier "Remind me later" or
    /// "Skip this version".
    /// </summary>
    internal static class UpdateReminder
    {
        public static readonly TimeSpan RemindLaterInterval = TimeSpan.FromDays(7);

        /// <summary>
        /// The value to store when the user asks to be reminded later.
        /// </summary>
        /// <remarks>
        /// Stored as a round-trip UTC string so that the time zone and the user's culture cannot
        /// change what is read back.
        /// </remarks>
        public static string RemindAfterFrom(DateTime nowUtc) {
            return (nowUtc + RemindLaterInterval).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// True while a "Remind me later" is still running.
        /// </summary>
        /// <remarks>
        /// A stored time further away than one interval cannot have come from <see cref="RemindAfterFrom"/>
        /// unless the clock was moved back, so it is ignored rather than silencing the notification
        /// for an unknown length of time. An unreadable value is ignored too.
        /// </remarks>
        public static bool IsSnoozed(string? remindAfterUtc, DateTime nowUtc) {
            if (string.IsNullOrEmpty(remindAfterUtc)
                || !DateTime.TryParse(remindAfterUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var remindAfter)) {
                return false;
            }
            var remaining = remindAfter.ToUniversalTime() - nowUtc.ToUniversalTime();
            return remaining > TimeSpan.Zero && remaining <= RemindLaterInterval;
        }

        /// <summary>
        /// True when the user chose "Skip this version" for exactly this version. A newer release
        /// is notified again.
        /// </summary>
        public static bool IsSkipped(string? latestVersion, string? skippedVersion) {
            return !string.IsNullOrEmpty(skippedVersion)
                && string.Equals(latestVersion, skippedVersion, StringComparison.Ordinal);
        }
    }
}
