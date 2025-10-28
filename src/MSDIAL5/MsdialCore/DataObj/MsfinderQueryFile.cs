using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>
    /// mat: raw data file
    /// fgt: formula finder result file
    /// sfd: structure finder result file
    /// fdb: formula database
    /// ndb: neutral loss database
    /// </summary>
    //internal enum SaveFileFormat { msp, mat, fgt, sfd, fdb, ndb, efd, esd, apf, anf, msd }

    public class MsfinderQueryFile
    {
        //private SolidColorBrush bgColor; // temp
        public MsfinderQueryFile(string rawDataFilePath)
        {
            RawDataFilePath = rawDataFilePath;
            RawDataFileName = Path.GetFileName(rawDataFilePath);
            FormulaFilePath = Path.ChangeExtension(rawDataFilePath, ".fgt");
            FormulaFileName = Path.GetFileName(FormulaFilePath);
            StructureFolderPath = Path.ChangeExtension(rawDataFilePath, null);
            StructureFolderName = Path.GetFileName(StructureFolderPath);
            //this.bgColor = Brushes.White;
        }

        public string RawDataFilePath { get; }

        public string RawDataFileName { get; }

        public string FormulaFilePath { get; }
        
        public string FormulaFileName { get; }

        public string StructureFolderPath { get; }

        public string StructureFolderName { get; }

        public bool FormulaFileExists => File.Exists(FormulaFilePath);

        public bool StructureFileExists => File.Exists(StructureFolderPath);
    }
}