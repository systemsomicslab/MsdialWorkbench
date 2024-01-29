using System;

namespace CompMs.App.Msdial.Model.Service
{
    internal sealed class OpenFileRequest
    {
        private readonly Action<string>? handler;

        public OpenFileRequest(Action<string>? handler) {
            this.handler = handler;
        }

        public void Run(string filePath) {
            handler?.Invoke(filePath);
        }

        public string? Title { get; set; }
        public string? Filter { get; set; }
        public bool RestoreDirectory { get; set; } = true;
    }
}
