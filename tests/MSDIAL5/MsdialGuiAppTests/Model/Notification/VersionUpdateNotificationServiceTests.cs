using CompMs.App.Msdial.Model.Notification;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.Notification.Tests
{
    /// <summary>
    /// Reading a release tag as a version.
    /// </summary>
    /// <remarks>
    /// The strip used to be <c>TagName.TrimStart("MSDIAL-v".ToCharArray())</c>, which trims ANY of
    /// {M,S,D,I,A,L,-,v} repeatedly rather than the prefix as a unit. It gave the right answer only
    /// because the release selection above it requires the tag to start "MSDIAL-v5", so the trim
    /// stopped on a '5' that is not in the set -- accidental safety resting on a guard in another
    /// method.
    ///
    /// The consequence of the trap firing is quiet and long-lived: a mangled version never equals the
    /// local one, so the update dialog appears on every start for a release nobody can identify.
    /// </remarks>
    [TestClass()]
    public class VersionUpdateNotificationServiceTests
    {
        [TestMethod()]
        public void ATagYieldsTheVersionAfterThePrefix() {
            Assert.AreEqual("5.5.241113", VersionUpdateNotificationService.WithoutTagPrefix("MSDIAL-v5.5.241113"));
            Assert.AreEqual("5.5.250403-beta", VersionUpdateNotificationService.WithoutTagPrefix("MSDIAL-v5.5.250403-beta"));
        }

        /// <summary>
        /// THE CASE THE CHARACTER-SET TRIM WOULD HAVE EATEN.
        /// </summary>
        /// <remarks>
        /// Every version here begins with a character that was in the trim set, so the old code
        /// would have removed it -- silently, and only for tag schemes nobody had tried yet.
        /// </remarks>
        [TestMethod()]
        public void AVersionStartingWithAPrefixCharacterSurvives() {
            Assert.AreEqual("dev-5.6", VersionUpdateNotificationService.WithoutTagPrefix("MSDIAL-vdev-5.6"));
            Assert.AreEqual("v6.0", VersionUpdateNotificationService.WithoutTagPrefix("MSDIAL-vv6.0"));
            Assert.AreEqual("-rc1", VersionUpdateNotificationService.WithoutTagPrefix("MSDIAL-v-rc1"));
            Assert.AreEqual("LTS-5.5", VersionUpdateNotificationService.WithoutTagPrefix("MSDIAL-vLTS-5.5"));
        }

        /// <summary>
        /// A tag that is not one of ours is returned whole rather than partly eaten.
        /// </summary>
        [TestMethod()]
        public void AnUnrecognisedTagIsLeftAlone() {
            Assert.AreEqual("MSFINDER-v3.73", VersionUpdateNotificationService.WithoutTagPrefix("MSFINDER-v3.73"));
            Assert.AreEqual("5.5.241113", VersionUpdateNotificationService.WithoutTagPrefix("5.5.241113"));
        }

        [TestMethod()]
        public void AMissingTagIsEmptyRatherThanNull() {
            Assert.AreEqual(string.Empty, VersionUpdateNotificationService.WithoutTagPrefix(null));
            Assert.AreEqual(string.Empty, VersionUpdateNotificationService.WithoutTagPrefix(string.Empty));
        }
    }
}
