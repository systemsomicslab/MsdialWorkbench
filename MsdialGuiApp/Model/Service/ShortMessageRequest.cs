using CompMs.CommonMVVM;
using System;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Service
{
    internal sealed class ShortMessageRequest : BindableBase
    {
        public ShortMessageRequest(string content) {
            Content = content;
        }

        public string Content { get; }
        public bool? Result {
            get => _result;
            set => SetProperty(ref _result, value);
        }
        private bool? _result;
    }

    internal sealed class ProcessMessageRequest : BindableBase
    {
        public ProcessMessageRequest(string content, Func<Task> asyncAction) {
            Content = content;
            AsyncAction = asyncAction;
        }

        public string Content { get; }
        public Func<Task> AsyncAction { get; }
        public bool? Result {
            get => _result;
            set => SetProperty(ref _result, value);
        }
        private bool? _result;
    }
}
