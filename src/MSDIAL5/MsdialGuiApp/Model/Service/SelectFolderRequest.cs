using System;

namespace CompMs.App.Msdial.Model.Service
{
    internal sealed class SelectFolderRequest
    {
        private readonly Action<string>? handler;

        public SelectFolderRequest() {

        }

        public SelectFolderRequest(Action<string> handler) {
            this.handler = handler;
        }

        public void Run(string filePath) {
            SelectedPath = filePath;
            handler?.Invoke(filePath);
        }

        public string? Title { get; set; }
        public string? SelectedPath { get; set; }
    }
}
