using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.App.Msdial.Model.Notification.Tests
{
    [TestClass()]
    public class UpdateReminderTests
    {
        private static readonly DateTime Now = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

        [TestMethod()]
        public void RemindLaterHoldsTheNotificationForOneInterval() {
            var stored = UpdateReminder.RemindAfterFrom(Now);

            Assert.IsTrue(UpdateReminder.IsSnoozed(stored, Now));
            Assert.IsTrue(UpdateReminder.IsSnoozed(stored, Now + UpdateReminder.RemindLaterInterval - TimeSpan.FromMinutes(1)));
            Assert.IsFalse(UpdateReminder.IsSnoozed(stored, Now + UpdateReminder.RemindLaterInterval));
        }

        [TestMethod()]
        public void TheStoredTimeIsReadTheSameInAnyTimeZoneKind() {
            var stored = UpdateReminder.RemindAfterFrom(Now);

            Assert.IsTrue(UpdateReminder.IsSnoozed(stored, Now.ToLocalTime()));
        }

        [TestMethod()]
        public void NothingStoredOrAnUnreadableValueDoesNotSnooze() {
            Assert.IsFalse(UpdateReminder.IsSnoozed(null, Now));
            Assert.IsFalse(UpdateReminder.IsSnoozed(string.Empty, Now));
            Assert.IsFalse(UpdateReminder.IsSnoozed("next week", Now));
        }

        /// <summary>
        /// A reminder further away than one interval means the clock was moved back after it was
        /// stored. Honouring it would silence the notification for an unknown time.
        /// </summary>
        [TestMethod()]
        public void AReminderBeyondOneIntervalIsIgnored() {
            var stored = UpdateReminder.RemindAfterFrom(Now + TimeSpan.FromDays(30));

            Assert.IsFalse(UpdateReminder.IsSnoozed(stored, Now));
        }

        [TestMethod()]
        public void SkipAppliesOnlyToTheSkippedVersion() {
            Assert.IsTrue(UpdateReminder.IsSkipped("5.5.261105", "5.5.261105"));
            Assert.IsFalse(UpdateReminder.IsSkipped("5.5.261201", "5.5.261105"));
            Assert.IsFalse(UpdateReminder.IsSkipped("5.5.261105", null));
            Assert.IsFalse(UpdateReminder.IsSkipped("5.5.261105", string.Empty));
        }
    }
}
