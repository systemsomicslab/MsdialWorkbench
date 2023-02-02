using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Rfx.Riken.OsakaUniv;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msdial.Common.Utility
{
    public class MsdialDataHandleUtility {
        public static List<SolidColorBrush> MsdialDefaultSolidColorBrushList {get;set;} = new List<SolidColorBrush>() { Brushes.Blue, Brushes.Red, Brushes.Green
            , Brushes.DarkBlue, Brushes.DarkRed, Brushes.DarkGreen, Brushes.DeepPink, Brushes.OrangeRed
            , Brushes.Purple, Brushes.Crimson, Brushes.DarkGoldenrod, Brushes.Black, Brushes.BlanchedAlmond
            , Brushes.BlueViolet, Brushes.Brown, Brushes.BurlyWood, Brushes.CadetBlue, Brushes.Aquamarine
            , Brushes.Yellow, Brushes.Crimson, Brushes.Chartreuse, Brushes.Chocolate, Brushes.Coral
            , Brushes.CornflowerBlue, Brushes.Cornsilk, Brushes.Crimson, Brushes.Cyan, Brushes.DarkCyan
            , Brushes.DarkKhaki, Brushes.DarkMagenta, Brushes.DarkOliveGreen, Brushes.DarkOrange, Brushes.DarkOrchid
            , Brushes.DarkSalmon, Brushes.DarkSeaGreen, Brushes.DarkSlateBlue, Brushes.DarkSlateGray
            , Brushes.DarkTurquoise, Brushes.DeepSkyBlue, Brushes.DodgerBlue, Brushes.Firebrick, Brushes.FloralWhite
            , Brushes.ForestGreen, Brushes.Fuchsia, Brushes.Gainsboro, Brushes.GhostWhite, Brushes.Gold
            , Brushes.Goldenrod, Brushes.Gray, Brushes.Navy, Brushes.DarkGreen, Brushes.Lime
            , Brushes.MediumBlue };

        public static Dictionary<string, SolidColorBrush> GetClassIdColorDictionary(IReadOnlyList<AnalysisFileBean> analysisFileBeanCollection, List<SolidColorBrush> solidColorBrushList) {
            Dictionary<string, SolidColorBrush> classId_SolidColorBrush = new Dictionary<string, SolidColorBrush>();

            //Initialize
            int counter = 0;
            string classId = analysisFileBeanCollection[0].AnalysisFilePropertyBean.AnalysisFileClass;

            if (counter <= solidColorBrushList.Count - 1)
                classId_SolidColorBrush[classId] = solidColorBrushList[counter];
            else
                classId_SolidColorBrush[classId] = solidColorBrushList[0];

            counter++;

            for (int i = 0; i < analysisFileBeanCollection.Count; i++) {
                classId = analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileClass;
                if (!classId_SolidColorBrush.ContainsKey(classId)) {
                    if (counter <= solidColorBrushList.Count - 1)
                        classId_SolidColorBrush[classId] = solidColorBrushList[counter];
                    else
                        classId_SolidColorBrush[classId] = solidColorBrushList[0];
                    counter++;
                }
            }

            return classId_SolidColorBrush;
        }

        public static Dictionary<int, SolidColorBrush> GetFileIdColorDictionary(IReadOnlyList<AnalysisFileBean> analysisFileBeanCollection, List<SolidColorBrush> solidColorBrushList)
        {
            Dictionary<string, SolidColorBrush> classId_SolidColorBrush = new Dictionary<string, SolidColorBrush>();
            Dictionary<int, SolidColorBrush> fileId_SolidColorBrush = new Dictionary<int, SolidColorBrush>();

            //Initialize
            int counter = 0;
            string classId = analysisFileBeanCollection[0].AnalysisFilePropertyBean.AnalysisFileClass;

            for (int i = 0; i < analysisFileBeanCollection.Count; i++)
            {
                classId = analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileClass;
                if (!classId_SolidColorBrush.ContainsKey(classId))
                {
                    if (counter <= solidColorBrushList.Count - 1)
                        classId_SolidColorBrush[classId] = solidColorBrushList[counter];
                    else
                        classId_SolidColorBrush[classId] = solidColorBrushList[0];
                    counter++;
                }
            }

            foreach (var analysisFile in analysisFileBeanCollection)
            {
                fileId_SolidColorBrush[analysisFile.AnalysisFilePropertyBean.AnalysisFileId] = classId_SolidColorBrush[analysisFile.AnalysisFilePropertyBean.AnalysisFileClass];
            }
            return fileId_SolidColorBrush;
        }


    }
}
