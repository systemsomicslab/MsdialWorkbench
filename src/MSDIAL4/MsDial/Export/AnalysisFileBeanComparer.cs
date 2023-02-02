using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Rfx.Riken.OsakaUniv.Export
{
	/// <summary>
	/// This class contains properties that a View can data bind to.
	/// <para>
	/// See http://www.galasoft.ch/mvvm
	/// </para>
	/// </summary>
	public class AnalysisFileBeanComparer : IEqualityComparer<AnalysisFileBean>
	{

		public bool Equals(AnalysisFileBean file1, AnalysisFileBean file2)
		{
			Debug.WriteLine("Comparing: " + file1.AnalysisFilePropertyBean.AnalysisFileId +
				" with " + file2.AnalysisFilePropertyBean.AnalysisFileId);
			return file1.AnalysisFilePropertyBean.AnalysisFileId.Equals(file2.AnalysisFilePropertyBean.AnalysisFileId);
		}

		public int GetHashCode(AnalysisFileBean file)
		{
			return file.AnalysisFilePropertyBean.AnalysisFileId;
		}
	}
}