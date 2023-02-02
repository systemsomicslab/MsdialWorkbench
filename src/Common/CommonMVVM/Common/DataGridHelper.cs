using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Rfx.Riken.OsakaUniv
{
    public static class DataGridHelper
    {
        /// <summary>
        /// This is used in xaml code to focus on the cell of data grid by the user click.
        /// If this program is not used, the user should be using double click to re-write the cell contents.
        /// </summary>
        /// <param name="dataGridCellInfo"></param>
        /// <returns></returns>
        public static DataGridCell GetCell(DataGridCellInfo dataGridCellInfo)
        {
            if (!dataGridCellInfo.IsValid)
            {
                return null;
            }

            var cellContent = dataGridCellInfo.Column.GetCellContent(dataGridCellInfo.Item);
            if (cellContent != null)
            {
                return (DataGridCell)cellContent.Parent;
            }
            else
            {
                return null;
            }
        }
    }
}

namespace CompMs.CommonMVVM.Helper {
    public static class DataGridHelper {
        /// <summary>
        /// This is used in xaml code to focus on the cell of data grid by the user click.
        /// If this program is not used, the user should be using double click to re-write the cell contents.
        /// </summary>
        /// <param name="dataGridCellInfo"></param>
        /// <returns></returns>
        public static DataGridCell GetCell(DataGridCellInfo dataGridCellInfo) {
            if (!dataGridCellInfo.IsValid) {
                return null;
            }

            var cellContent = dataGridCellInfo.Column.GetCellContent(dataGridCellInfo.Item);
            if (cellContent != null) {
                return (DataGridCell)cellContent.Parent;
            }
            else {
                return null;
            }
        }
    }
}
