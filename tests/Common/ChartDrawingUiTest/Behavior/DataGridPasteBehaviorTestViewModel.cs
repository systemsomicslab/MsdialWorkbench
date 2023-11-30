using CompMs.CommonMVVM;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChartDrawingUiTest.Behavior
{
    internal sealed class DataGridPasteBehaviorTestViewModel : ViewModelBase
    {
        public DataGridPasteBehaviorTestViewModel()
        {
            Records = new List<Record>
            {
                new Record { X = -1, Y = -1d, Z = "-1", },
                new Record { X = 0, Y = 0d, Z = "", },
                new Record { X = 1, Y = 1d, Z = "1", }
            };
        }

        public List<Record> Records { get; }

        internal sealed class Record : ViewModelBase {
            [RegularExpression(@"\d+", ErrorMessage = "X error")]
            public int X {
                get => _x;
                set => SetProperty(ref _x, value);
            }
            private int _x;

            [RegularExpression(@"\d*.?\d+", ErrorMessage = "Y error")]
            public double Y {
                get => _y;
                set => SetProperty(ref _y, value);
            }
            private double _y;

            [Required(ErrorMessage = "Z error")]
            public string Z {
                get => _z;
                set => SetProperty(ref _z, value);
            }
            private string _z;
        }
    }
}
