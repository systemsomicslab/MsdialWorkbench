using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public class BarChartModel : ValidatableBase
    {
        public BarChartModel(
            AxisData horizontalData,
            AxisData verticalData,
            IBarItemsLoader loader) {
            this.loader = loader;
            HorizontalData = horizontalData;
            VerticalData = verticalData;
        }

        private readonly IBarItemsLoader loader;

        public AxisData HorizontalData {
            get => horizontalData;
            set => SetProperty(ref horizontalData, value);
        }
        private AxisData horizontalData;

        public AxisData VerticalData {
            get => verticalData;
            set => SetProperty(ref verticalData, value);
        }
        private AxisData verticalData;

        public List<BarItem> BarItems {
            get => barItems;
            set => SetProperty(ref barItems, value);
        }
        private List<BarItem> barItems = new List<BarItem>();

        public async Task LoadBarItemsAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            BarItems = await loader.LoadBarItemsAsync(target, token);

            HorizontalData = new AxisData(
                new CategoryAxisManager<string>(BarItems.Select(item => item.Class).ToList()),
                HorizontalData.Property,
                HorizontalData.Title);
            VerticalData = new AxisData(
                ContinuousAxisManager<double>.Build(BarItems, item => item.Height, VerticalData.Axis.Bounds),
                VerticalData.Property,
                VerticalData.Title);
        }
    }
}
