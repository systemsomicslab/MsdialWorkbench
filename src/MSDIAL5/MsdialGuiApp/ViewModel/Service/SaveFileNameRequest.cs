using System;

namespace CompMs.App.Msdial.ViewModel.Service
{
    public class SaveFileNameRequest
    {
        private readonly Action<string> handler;

        public SaveFileNameRequest(Action<string> handler) {
            this.handler = handler;
        }

        public void Run(string filePath) {
            handler?.Invoke(filePath);
        }

        public string Title { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public bool RestoreDirectory { get; set; } = true;
        public bool AddExtension { get; set; } = true;

        public bool? Result { get; set; } = null;
    }
}
