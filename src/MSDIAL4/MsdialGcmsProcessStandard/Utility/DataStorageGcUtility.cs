using CompMs.Common.MessagePack;
using edu.ucdavis.fiehnlab.msdial.Readers;
using edu.ucdavis.fiehnlab.msdial.Writers;
using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
//using System.Windows;

namespace Msdial.Gcms.Dataprocess.Utility
{
    public sealed class DataStorageGcUtility
    {
        private const int DCL_VERSION = 2; // version 2 started in 2018/12/22
		private static bool SHOW_WARNING = true;

        public static void SaveToXmlFile(object obj, string path, Type type)
        {
            var serializer = new DataContractSerializer(type);
            FileStream fs = new FileStream(path, FileMode.Create);
            serializer.WriteObject(fs, obj);
            fs.Close();
        }

        public static object LoadFromXmlFile(string path, Type type)
        {
            var serializer = new DataContractSerializer(type);
            FileStream fs = new FileStream(path, FileMode.Open);
            object obj = serializer.ReadObject(fs);
            fs.Close();
            return obj;
        }

        public static List<PeakAreaBean> GetPeakAreaList(string filepath)
        {
            return MessagePackHandler.LoadFromFile<List<PeakAreaBean>>(filepath);
            //return (List<PeakAreaBean>)LoadFromXmlFile(filepath, typeof(List<PeakAreaBean>));
        }

        public static SavePropertyBean GetSavePropertyBean(ProjectPropertyBean projectProp, RdamPropertyBean rdamProp, List<MspFormatCompoundInformationBean> mspDB, 
			IupacReferenceBean iupacRef, AnalysisParamOfMsdialGcms param, 
			ObservableCollection<AnalysisFileBean> analysisFiles, ObservableCollection<AlignmentFileBean> alignmentFiles)
        {

            projectProp.FinalSavedDate = DateTime.Now;

            var savePropertyBean = new SavePropertyBean();
            savePropertyBean.ProjectPropertyBean = projectProp;
            savePropertyBean.RdamPropertyBean = rdamProp;
            savePropertyBean.MspFormatCompoundInformationBeanList = mspDB;
            savePropertyBean.IupacReferenceBean = iupacRef;
            savePropertyBean.AnalysisParamForGC = param;
            savePropertyBean.AnalysisParametersBean = new AnalysisParametersBean();
            savePropertyBean.AnalysisFileBeanCollection = analysisFiles;
            savePropertyBean.AlignmentFileBeanCollection = alignmentFiles;

            return savePropertyBean;
        }

        #region MS1Dec Result Writer
        public static void WriteMs1DecResults(string file, List<MS1DecResult> ms1DecResults)
        {
            using (var fs = File.Open(file, FileMode.Create, FileAccess.ReadWrite))
            {
                var totalPeakNumber = ms1DecResults.Count;
                var seekPointer = new List<long>();

				WriteHeaders(fs, seekPointer, totalPeakNumber);
                for (int i = 0; i < ms1DecResults.Count; i++)
                {
                    var seekpoint = fs.Position;
                    seekPointer.Add(seekpoint);

                    ms1DecResults[i].SeekPoint = seekpoint;
                    GCDecWriterVer1.Write(fs, ms1DecResults[i], i);
                }
                WriteSeekpointer(fs, seekPointer);
            }
        }

		public static void WriteHeaders(FileStream fs, List<long> seekPointer, int totalPeakNumber)
        {
			Debug.WriteLine("Writing deconvolution file: " + fs.Name + " (Ver: DC" + DCL_VERSION, "INFO");

            //first header
            seekPointer.Add(fs.Position);
            fs.Write(Encoding.ASCII.GetBytes("DC"), 0, 2);
            fs.Write(BitConverter.GetBytes(DCL_VERSION), 0, 4);

            //second header
            seekPointer.Add(fs.Position);
            fs.Write(BitConverter.GetBytes(totalPeakNumber), 0, 4);

            //third header
            var buffer = new byte[totalPeakNumber * 8];
            seekPointer.Add(fs.Position);
            fs.Write(buffer, 0, buffer.Length);
        }

        public static void WriteSeekpointer(FileStream fs, List<long> seekPointer)
        {
            //Finalize
            fs.Seek(seekPointer[2], SeekOrigin.Begin);
            for (int i = 3; i < seekPointer.Count; i++)
                fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
        }

        #endregion

        #region MS1Dec Result Reader
        public static List<MS1DecResult> ReadMS1DecResults(string file)
        {
            using (var fs = File.Open(file, FileMode.Open, FileAccess.Read))
            {
				var buffer = new byte[6];
				fs.Seek(0, SeekOrigin.Begin);
				fs.Read(buffer, 0, 2);
				fs.Read(buffer, 2, 4);

				string name = Encoding.ASCII.GetString(buffer, 0, 2);
				int version = BitConverter.ToInt32(buffer, 2);

				Debug.WriteLine("name: " + name);
				Debug.WriteLine("version: " + version);

				if (name.Equals("DC") && version == 1) {
					Debug.WriteLine("Reading deconvolution file " + file + " (V." + DCL_VERSION + ")", "INFO");
					return GCDecReaderVer1.ReadAll(fs);
				}
                else if (name.Equals("DC") && version == 2) {
                    Debug.WriteLine("Reading deconvolution file " + file + " (V." + DCL_VERSION + ")", "INFO");
                    return GCDecReaderVer2.ReadAll(fs);
                }
                else {
					Debug.WriteLine("Reading deconvolution file " + file + " (Legacy)", "INFO");
					if (SHOW_WARNING) {
                        Console.WriteLine("The deconvolution file/s is/are outdated. Please reprocess your data.");
						//MessageBox.Show("The deconvolution file/s is/are outdated. Please reprocess your data.", "INFO", MessageBoxButton.OK, MessageBoxImage.Information);
						SHOW_WARNING = false;
					}
					return GCDecReaderLegacy.ReadAll(fs);
				}
			}
		}

        public static MS1DecResult ReadMS1DecResult(string file, long seekPoint)
        {
            using (var fs = File.Open(file, FileMode.Open, FileAccess.ReadWrite))
            {
				var buffer = new byte[6];
				fs.Seek(0, SeekOrigin.Begin);
				fs.Read(buffer, 0, 2);
				fs.Read(buffer, 2, 4);

				string name = Encoding.ASCII.GetString(buffer, 0, 2);
				int version = BitConverter.ToInt32(buffer, 2);

				Debug.WriteLine("name: " + name);
				Debug.WriteLine("version: " + version);

				if (name.Equals("DC") && version == 1) {
					return GCDecReaderVer1.Read(fs, seekPoint);
				}
                else if (name.Equals("DC") && version == 2) {
                    return GCDecReaderVer2.Read(fs, seekPoint);
                }
                else {
					return GCDecReaderLegacy.Read(fs, seekPoint);
				}
            }
        }

        #endregion
	}
}
