namespace CompMs.App.Msdial.ViewModel.Service
{
    public class ErrorMessageBoxRequest
    {
        public string Caption { get; set; } = "Error";

        public string Content { get; set; } = string.Empty;

        public bool Result { get; set; }
    }
}
