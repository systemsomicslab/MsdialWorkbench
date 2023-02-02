using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Utility;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MsfinderCommon.Query {
    public class MsfinderQueryStorage {
        #region members
        private string importFolderPath;
        private ObservableCollection<MsfinderQueryFile> queryFiles;
        private Rfx.Riken.OsakaUniv.RawData rawData;
        private List<FormulaResult> formualResults;
        private List<FragmenterResult> fragmenterResults;
        private AnalysisParamOfMsfinder analysisParameter;
        #endregion

        #region properties
        public string ImportFolderPath {
            get { return importFolderPath; }
            set { importFolderPath = value; }
        }

        public ObservableCollection<MsfinderQueryFile> QueryFiles {
            get { return queryFiles; }
            set { queryFiles = value; }
        }

        public Rfx.Riken.OsakaUniv.RawData RawData {
            get { return rawData; }
            set { rawData = value; }
        }

        public List<FormulaResult> FormualResults {
            get { return formualResults; }
            set { formualResults = value; }
        }

        public List<FragmenterResult> FragmenterResults {
            get { return fragmenterResults; }
            set { fragmenterResults = value; }
        }

        public AnalysisParamOfMsfinder AnalysisParameter {
            get { return analysisParameter; }
            set { analysisParameter = value; }
        }
        #endregion

        #region constructor
        public MsfinderQueryStorage() {
            this.importFolderPath = string.Empty;
            this.queryFiles = new ObservableCollection<MsfinderQueryFile>();
            this.analysisParameter = MsFinderIniParcer.Read();
            this.rawData = new Rfx.Riken.OsakaUniv.RawData();
            this.formualResults = new List<FormulaResult>();
            this.fragmenterResults = new List<FragmenterResult>();
        }

        public MsfinderQueryStorage(string importFolderPath, AnalysisParamOfMsfinder analysisParam = null) {
            this.importFolderPath = importFolderPath;
            this.queryFiles = FileStorageUtility.GetAnalysisFileBeanCollection(this.importFolderPath);
            this.rawData = new Rfx.Riken.OsakaUniv.RawData();
            this.formualResults = new List<FormulaResult>();
            this.fragmenterResults = new List<FragmenterResult>();

            if (analysisParam == null) this.analysisParameter = MsFinderIniParcer.Read();
            else this.analysisParameter = analysisParam;
        }
        #endregion

    }
}
