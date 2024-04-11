using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Chart
{
    class GraphElements : BindableBase
    {
        public string? GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string? graphTitle;

        public string? HorizontalTitle {
            get => horizontalTitle;
            set => SetProperty(ref horizontalTitle, value);
        }
        private string? horizontalTitle;

        public string? VerticalTitle {
            get => verticalTitle;
            set => SetProperty(ref verticalTitle, value);
        }
        private string? verticalTitle;

        public string? HorizontalProperty {
            get => horizontalProperty;
            set => SetProperty(ref horizontalProperty, value);
        }
        private string? horizontalProperty;

        public string? VerticalProperty {
            get => verticalProperty;
            set => SetProperty(ref verticalProperty, value);
        }
        private string? verticalProperty;
    }
}
