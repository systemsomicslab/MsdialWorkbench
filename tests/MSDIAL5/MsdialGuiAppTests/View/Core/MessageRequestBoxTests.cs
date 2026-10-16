using CompMs.App.Msdial.Model.Service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace CompMs.App.Msdial.View.Core.Tests
{
    [TestClass()]
    public class MessageRequestBoxTests
    {
        // A message a model publishes is shown with the buttons it asks for and the icon of its severity,
        // as it was when the model set them on an ErrorMessageBoxRequest.
        [TestMethod()]
        public void AMessageIsShownWithItsButtonsAndTheIconOfItsSeverity() {
            Assert.AreEqual(MessageBoxButton.YesNo, MessageRequestBox.ButtonsOf(new MessageRequest { AsksYesNo = true, }));
            Assert.AreEqual(MessageBoxButton.OK, MessageRequestBox.ButtonsOf(new MessageRequest { AsksYesNo = false, }));

            Assert.AreEqual(MessageBoxImage.Information, MessageRequestBox.IconOf(new MessageRequest { Severity = MessageSeverity.Information, }));
            Assert.AreEqual(MessageBoxImage.Warning, MessageRequestBox.IconOf(new MessageRequest { Severity = MessageSeverity.Warning, }));
            Assert.AreEqual(MessageBoxImage.Error, MessageRequestBox.IconOf(new MessageRequest { Severity = MessageSeverity.Error, }));
        }

        // What a model leaves unset is what an ErrorMessageBoxRequest showed: the caption "Error", an OK button and the error icon.
        [TestMethod()]
        public void AMessageWithNothingSetIsAnErrorWithAnOkButton() {
            var request = new MessageRequest();

            Assert.AreEqual("Error", request.Caption);
            Assert.AreEqual(MessageBoxButton.OK, MessageRequestBox.ButtonsOf(request));
            Assert.AreEqual(MessageBoxImage.Error, MessageRequestBox.IconOf(request));
            Assert.IsNull(request.Result);
        }

        [TestMethod()]
        public void OnlyYesOrOkIsAnAcceptance() {
            Assert.IsTrue(MessageRequestBox.IsAccepted(MessageBoxResult.Yes));
            Assert.IsTrue(MessageRequestBox.IsAccepted(MessageBoxResult.OK));
            Assert.IsFalse(MessageRequestBox.IsAccepted(MessageBoxResult.No));
            Assert.IsFalse(MessageRequestBox.IsAccepted(MessageBoxResult.Cancel));
            Assert.IsFalse(MessageRequestBox.IsAccepted(MessageBoxResult.None));
        }
    }
}
