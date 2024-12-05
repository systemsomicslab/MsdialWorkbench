using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.Common.StructureFinder;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class StructureFinderBatchProcess
    {
        public void Process(List<MsfinderQueryFile> analysisFiles, AnalysisParamOfMsfinder parameter,
            List<ExistStructureQuery> existStructureDB, List<ExistStructureQuery> userDefinedDB, List<ExistStructureQuery> mineStructureDB,
            List<FragmentOntology> fragmentOntologyDB, List<MoleculeMsReference> mspDB, List<FragmentLibrary> eiFragmentDB)
        {
            var progress = 0;
            var errorString = string.Empty;

            foreach (var file in analysisFiles)
            {
                var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, parameter);
                if (rawData == null || rawData.Name == null) continue;
                SingleSearchOfStructureFinder(file, rawData, parameter, existStructureDB,
                    userDefinedDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);

                progress++;
                //this.bgWorker.ReportProgress(progress);
            }

            //try
            //{
            //    if (parameter.IsPubChemAllTime || parameter.IsPubChemOnlyUseForNecessary)
            //    { // currently difficult to execute this program as parallel when we use pubchem rest service
            //        foreach (var file in analysisFiles)
            //        {
            //            var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, parameter);
            //            if (rawData == null || rawData.Name == null) continue;
            //            SingleSearchOfStructureFinder(file, rawData, parameter, existStructureDB,
            //                userDefinedDB,mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);

            //            progress++;
            //            //this.bgWorker.ReportProgress(progress);
            //        }
            //    }
            //    else
            //    { // use parallel mode unless pubchem rest is used.
            //        var syncObj = new object();
            //        Parallel.ForEach(analysisFiles, file => {

            //            var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, parameter);
            //            if (rawData == null || rawData.Name == null) return;
            //            SingleSearchOfStructureFinder(file, rawData, parameter, existStructureDB,
            //                userDefinedDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);

            //            lock (syncObj)
            //            {
            //                progress++;
            //                //this.bgWorker.ReportProgress(progress);
            //            }
            //        });
            //    }
            //}
            //catch (AggregateException ae)
            //{
            //    var exceptions = ae.Flatten().InnerExceptions;
            //    foreach (var ex in exceptions)
            //    {
            //        errorString += ex.GetType() + "\r\n";
            //    }
            //}
        }

        public void SingleSearchOfStructureFinder(MsfinderQueryFile file, RawData rawData, AnalysisParamOfMsfinder parameter,
            List<ExistStructureQuery> existStructureDB, List<ExistStructureQuery> userDefinedDB, List<ExistStructureQuery> mineStructureDB,
            List<FragmentOntology> fragmentOntologyDB, List<MoleculeMsReference> mspDB, List<FragmentLibrary> eiFragmentDB)
        {
            if (!Directory.Exists(file.StructureFolderPath))
            {
                Directory.CreateDirectory(file.StructureFolderPath);
            }
            
            var structureFiles = System.IO.Directory.GetFiles(file.StructureFolderPath, "*.sfd");
            if (structureFiles.Length > 0) FileStorageUtility.DeleteSfdFiles(structureFiles);

            var error = string.Empty;

            List<FragmentLibrary> fragmentDB = null;
            if (parameter.IsUseEiFragmentDB && eiFragmentDB != null && eiFragmentDB.Count > 0) fragmentDB = eiFragmentDB;

            if (!System.IO.File.Exists(file.FormulaFilePath)) return;
            var formulaResults = FormulaResultParcer.FormulaResultReader(file.FormulaFilePath, out error);
            if (error != string.Empty)
            {
                Console.WriteLine(error);
            }

            if (formulaResults == null || formulaResults.Count == 0) return;

            foreach (var formula in formulaResults.Where(f => f.IsSelected).ToList())
            {
                var exportFilePath = FileStorageUtility.GetStructureDataFilePath(file.StructureFolderPath, formula.Formula.FormulaString);
                var finder = new StructureFinder();
                finder.StructureFinderMainProcess(rawData, formula, parameter, exportFilePath, existStructureDB,
                    userDefinedDB, mineStructureDB, fragmentDB, fragmentOntologyDB, mspDB);
            }
        }
        public void DirectSingleSearchOfStructureFinder(RawData rawData, List<FormulaResult> formulaResults, AnalysisParamOfMsfinder parameter, string exportFolderPath,
            List<ExistStructureQuery> existStructureDB, List<ExistStructureQuery> userDefinedDB, List<ExistStructureQuery> mineStructureDB,
            List<FragmentOntology> fragmentOntologyDB, List<MoleculeMsReference> mspDB, List<FragmentLibrary> eiFragmentDB) {

            List<FragmentLibrary> fragmentDB = null;
            if (eiFragmentDB != null && eiFragmentDB.Count > 0) fragmentDB = eiFragmentDB;

            if (formulaResults == null || formulaResults.Count == 0) return;

            foreach (var formula in formulaResults.Where(f => f.IsSelected).ToList()) {
                var exportFilePath = FileStorageUtility.GetStructureDataFilePath(exportFolderPath, formula.Formula.FormulaString);
                var finder = new StructureFinder();
                finder.StructureFinderMainProcess(rawData, formula, parameter, exportFilePath, existStructureDB,
                    userDefinedDB, mineStructureDB, fragmentDB, fragmentOntologyDB, mspDB);
            }
        }
    }
}
