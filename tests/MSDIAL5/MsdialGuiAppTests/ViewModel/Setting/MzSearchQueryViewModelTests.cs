using CompMs.Common.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    [TestClass]
    public class MzSearchQueryViewModelTests
    {
        [TestMethod]
        public void AValidEditIsWrittenToTheQueryAndAnInvalidOneIsNot() {
            var query = new MzSearchQuery { Mass = 149d, MassTolerance = 0.5, };
            var row = new MzSearchQueryViewModel(query);

            row.Mass = 149.0233;
            row.MassTolerance = 2.5;
            Assert.IsFalse(row.HasValidationErrors);
            Assert.AreEqual(149.0233, query.Mass);
            Assert.AreEqual(2.5, query.MassTolerance);

            row.Mass = -1d;
            row.MassTolerance = 0d;
            Assert.IsTrue(row.ContainsError(nameof(row.Mass)));
            Assert.IsTrue(row.ContainsError(nameof(row.MassTolerance)));
            Assert.AreEqual(149.0233, query.Mass);
            Assert.AreEqual(2.5, query.MassTolerance);
        }

        // The setting view models re-read HasValidationErrors when a row raises PropertyChanged.
        [TestMethod]
        public void HasValidationErrorsIsRaisedAfterTheEditIsValidated() {
            var row = new MzSearchQueryViewModel(new MzSearchQuery { Mass = 149.0233, MassTolerance = 0.01, });
            var raised = new List<bool>();
            row.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(row.HasValidationErrors)) {
                    raised.Add(row.HasValidationErrors);
                }
            };

            row.MassTolerance = -0.01;
            row.MassTolerance = 0.01;
            CollectionAssert.AreEqual(new[] { true, false, }, raised);
        }
    }
}
