namespace CompMs.App.SpectrumViewer.Model
{
    public class FileOpenRequest
    {
        public FileOpenRequest(string fileName) {
            FileName = fileName;
        }

        public string FileName { get; }
    }
}
