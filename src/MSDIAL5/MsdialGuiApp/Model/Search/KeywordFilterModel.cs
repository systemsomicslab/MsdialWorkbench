using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
        private readonly List<string> _keywords;
        private IKeywordFiltering _method;

        public KeywordFilterModel(string label, KeywordFilteringType type = KeywordFilteringType.IgnoreIfWordIsNull) {
            _keywords = [];
            Keywords = _keywords.AsReadOnly();
            switch (type) {
                case KeywordFilteringType.ExactMatch:
                    _method = new ExactMatch();
                    break;
                case KeywordFilteringType.KeepIfWordIsNull:
                    _method = new KeepIfWordIsNull();
                    break;
                case KeywordFilteringType.IgnoreIfWordIsNull:
                case KeywordFilteringType.None:
                default:
                    _method = new IgnoreIfWordIsNull();
                    break;
            }
            Label = label;
        }

        public string Label { get; }

        public ReadOnlyCollection<string> Keywords { get; }

        public string KeywordText {
            get => _keywordText;
            set {
                if (_keywordText == value) return;
                _keywordText = value;
                SetKeywords(_method.Filter(value.Split()));
                OnPropertyChanged(nameof(KeywordText));
            }
        }
        private string _keywordText = string.Empty;

        public void ClearKeywords() => KeywordText = string.Empty;

        private void SetKeywords(IEnumerable<string> keywords) {
            _keywords.Clear();
            _keywords.AddRange(keywords);
            IsEnabled = _keywords.Count > 0;
            OnPropertyChanged(nameof(Keywords));
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

        // Keyword comparisons ignore case without changing the text the user typed.
        private static bool ContainsIgnoreCase(string word, string keyword) {
            return word.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        sealed class IgnoreIfWordIsNull : IKeywordFiltering {
            public IEnumerable<string> Filter(IEnumerable<string> keywords) {
                return keywords.Where(keyword => !string.IsNullOrEmpty(keyword));
            }

            public bool Match(string? word, List<string> keywords) {
                return keywords.All(keyword => word is not null && ContainsIgnoreCase(word, keyword));
            }
        }

        sealed class KeepIfWordIsNull : IKeywordFiltering {
            public IEnumerable<string> Filter(IEnumerable<string> keywords) {
                return keywords.Where(keyword => !string.IsNullOrEmpty(keyword));
            }

            public bool Match(string? word, List<string> keywords) {
                return word is null || keywords.All(keyword => ContainsIgnoreCase(word, keyword));
            }
        }

        sealed class ExactMatch : IKeywordFiltering {
            public IEnumerable<string> Filter(IEnumerable<string> keywords) {
                return keywords.Where(keyword => !string.IsNullOrEmpty(keyword));
            }

            public bool Match(string word, List<string> keywords) {
                return keywords.Count == 0 || keywords.Any(keyword => string.Equals(keyword, word, StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
