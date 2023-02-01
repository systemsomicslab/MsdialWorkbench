using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.Graphics.Core.Base
{
    public class DataPointCollection : ObservableCollection<DataPoint>
    {
        public DataPointCollection() {

        }

        public DataPointCollection(IEnumerable<DataPoint> points) : base(points) {

        }

        public DataPointCollection Accumulate() {
            var result = Items.Select(item => item.Clone()).ToList();

            var typeToAcc = result.Select(item => item.Type).ToHashSet().ToDictionary(type => type, _ => 0d);
            foreach (var item in result) {
                item.Y = (typeToAcc[item.Type] += item.Y);
            }

            return new DataPointCollection(result);
        }
    }
}
