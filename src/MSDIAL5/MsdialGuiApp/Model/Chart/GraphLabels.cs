using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Chart
{
    public class GraphLabels : BindableBase
    {
        public GraphLabels(
            string graphTitle,
            string horizontalTitle,
            string verticalTitle,
            string annotationLabelProperty,
            string annotationOrderProperty) {
            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;
            AnnotationLabelProperty = annotationLabelProperty;
            AnnotationOrderProperty = annotationOrderProperty;
        }

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

        public string? AnnotationLabelProperty {
            get => annotationLabelProperty;
            set => SetProperty(ref annotationLabelProperty, value);
        }
        private string? annotationLabelProperty;

        public string? AnnotationOrderProperty {
            get => annotationOrderProperty;
            set => SetProperty(ref annotationOrderProperty, value);
        }
        private string? annotationOrderProperty;
    }
}
