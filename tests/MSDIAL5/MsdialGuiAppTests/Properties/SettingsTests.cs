using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using System.Linq;

namespace CompMs.App.Msdial.Properties.Tests
{
    [TestClass()]
    public class SettingsTests
    {
        /// <summary>
        /// Every setting property can be described by ApplicationSettingsBase.
        /// </summary>
        /// <remarks>
        /// A setting declared <c>string?</c> made the first access to ANY setting throw on .NET
        /// Framework, because of how ApplicationSettingsBase collects attributes. It surfaced only in
        /// an unrelated test that happened to read RHome, so this checks the whole class directly.
        /// </remarks>
        [TestMethod()]
        public void EverySettingPropertyCanBeDescribed() {
            var names = new Settings().Properties.Cast<SettingsProperty>().Select(p => p.Name).ToList();

            CollectionAssert.Contains(names, nameof(Settings.RHome));
            CollectionAssert.Contains(names, nameof(Settings.UpdateRemindAfterUtc));
            CollectionAssert.Contains(names, nameof(Settings.UpdateSkippedVersion));
        }
    }
}
