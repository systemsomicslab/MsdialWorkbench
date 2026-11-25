using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Graphics.AxisManager.Tests;

[TestClass]
public class CategoryAxisManagerTests
{
    [STATestMethod]
    public void ReorderingCategoriesRaisesMappingChangedAfterUpdatingValues() {
        var axis = new CategoryAxisManager { ItemsSource = new[] { "A", "B" } };
        var range = axis.Range;
        var notifications = 0;
        axis.AxisValueMappingChanged += (sender, args) => {
            notifications++;
            Assert.AreSame(axis, sender);
            Assert.AreEqual(1.5d, axis.TranslateToAxisValue("A").Value);
            Assert.AreEqual(0.5d, axis.TranslateToAxisValue("B").Value);
        };

        axis.ItemsSource = new[] { "B", "A" };

        Assert.AreEqual(1, notifications);
        Assert.AreEqual(range, axis.Range);
    }
}
