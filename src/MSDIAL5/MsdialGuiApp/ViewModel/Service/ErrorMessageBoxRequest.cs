using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Service
{
    public class ErrorMessageBoxRequest
    {
        public string Caption { get; set; } = "Error";

        public string Content { get; set; } = string.Empty;

        public MessageBoxButton ButtonType { get; set; } = MessageBoxButton.OK;

        public MessageBoxResult Result { get; set; } = MessageBoxResult.Cancel;
    }
}
