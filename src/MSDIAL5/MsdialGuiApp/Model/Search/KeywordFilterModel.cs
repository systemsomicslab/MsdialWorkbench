using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search
{
    public enum KeywordFilteringType {
        None,
        IgnoreIfWordIsNull,
        KeepIfWordIsNull,
        ExactMatch,
    }

    public sealed class KeywordFilterModel : DisposableModelBase
    {
        private readonly SemaphoreSlim _sem;
        private readonly List<string> _keywords;
        private IKeywordFiltering _method;

        public KeywordFilterModel(string label, KeywordFilteringType type = KeywordFilteringType.IgnoreIfWordIsNull) {
            _sem = new SemaphoreSlim(1, 1).AddTo(Disposables);
            _keywords = new List<string>();
            Keywords = _keywords.AsReadOnly();
            switch (type) {
                case KeywordFilteringType.ExactMatch:
                    _method = new ExactMatch();
                    break;
                case KeywordFilteringType.KeepIfWordIsNull:
                    _method = new KeepIfWordIsNull();
                    break;
                case KeywordFilteringType.IgnoreIfWordIsNull:
                default:
                    _method = new IgnoreIfWordIsNull();
                    break;
            }
            Label = label;
        }

        public string Label { get; }

        public ReadOnlyCollection<string> Keywords { get; }

        public async Task SetKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await _sem.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetKeywords(_method.Filter(keywords));
            }
            finally {
                _sem.Release();
            }
        }

        private void SetKeywords(IEnumerable<string> keywords) {
            _keywords.Clear();
            _keywords.AddRange(keywords);
            IsEnabled = _keywords.Count > 0;
        }

        public bool Match(string word) {
            return _method.Match(word, _keywords);
        }

        public bool IsEnabled {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        private bool _isEnabled;

        interface IKeywordFiltering {
            IEnumerable<string> Filter(IEnumerable<string> keywords);
            bool Match(string word, List<string> keywords);
        }

        sealed class IgnoreIfWordIsNull : IKeywordFiltering {
            public IEnumerable<string> Filter(IEnumerable<string> keywords) {
                return keywords.Where(keyword => !string.IsNullOrEmpty(keyword)).Select(keyword => keyword.ToLower());
            }

            public bool Match(string? word, List<string> keywords) {
                word = word?.ToLower();
                return keywords.All(keyword => word?.Contains(keyword) ?? false);
            }
        }

        sealed class KeepIfWordIsNull : IKeywordFiltering {
            public IEnumerable<string> Filter(IEnumerable<string> keywords) {
                return keywords.Where(keyword => !string.IsNullOrEmpty(keyword)).Select(keyword => keyword.ToLower());
            }

            public bool Match(string? word, List<string> keywords) {
                word = word?.ToLower();
                return word is null || keywords.All(keyword => word.Contains(keyword));
            }
        }

        sealed class ExactMatch : IKeywordFiltering {
            public IEnumerable<string> Filter(IEnumerable<string> keywords) {
                return keywords.Where(keyword => !string.IsNullOrEmpty(keyword));
            }

            public bool Match(string word, List<string> keywords) {
                return keywords.Count == 0 || keywords.Any(keyword => keyword == word);
            }
        }
    }
}
