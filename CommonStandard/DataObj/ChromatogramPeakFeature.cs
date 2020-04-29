using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj {
    public class ChromatogramPeakFeature: IChromatogramPeakFeature {

        // basic property of IChromatogramPeakFeature
        public int ChromScanIdLeft { get; set; }
        public int ChromScanIdTop { get; set; }
        public int ChromScanIdRight { get; set; }
        public Times TimesLeft { get; set; }
        public Times TimesTop { get; set; }
        public Times TimesRight { get; set; }
        public float PeakHeightLeft { get; set; }
        public float PeakHeightTop { get; set; }
        public float PeakHeightRight { get; set; }
        public float PeakAreaAboveZero { get; set; }
        public float PeakAreaAboveBaseline { get; set; }

        // set for IMMScanProperty
        public double PrecursorMz { get; set; }
        public IonMode IonMode { get; set; }

        // set for IMoleculeProperty (for representative)
        public string Name { get; set; } = string.Empty;
        public Formula Formula { get; set; } = new Formula();
        public string Ontology { get; set; } = string.Empty;
        public string SMILES { get; set; } = string.Empty;
        public string InChIKey { get; set; } = string.Empty;

        // ion physiochemical information
        public double CollisionCrossSection { get; set; }
       
        // molecule annotation results
        public MsScanMatchResult MspBasedMatchResult { get; set; }
        public MsScanMatchResult TextDbBasedMatchResult { get; set; }
        public string Comment { get; set; }

        // peak characters
        public ChromatogramPeakCharacter PeakCharacter { get; set; }

        // IDs to link properties
        public int MspID { get; set; } // representative msp id
        public List<int> MspIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (optional)
        public int TextDbID { get; set; }// representative text id
        public List<int> TextDbIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (optional)
        public int DeconvolutionID { get; set; } // 
        public long SeekPointToDCLFile { get; set; } // deconvoluted spectrum is stored in dcl file, and this is the seek pointer
        public int MasterPeakID { get; set; } // sequential IDs parsing all peak features extracted from an MS data
        public int PeakID { get; set; } // sequential IDs from the same dimmension e.g. RT vs MZ or IM vs MZ
        public int ParentPeakID { get; set; } // for LC-IM-MS/MS. The parent peak ID generating the daughter peak IDs

        // others
        public bool IsFragmentQueryExist { get; set; }

        List<ChromatogramPeakFeature> DriftChromFeatures { get; set; } = null;

        // link to raw data
        long MS1RawSpectrumID { get; set; }
        long MS1RawSpectrumIDatAccumulatedMS1 { get; set; } // used for LC-IM-MS/MS
        List<long> MS2RawSpectrumIDs { get; set; }

    }

    [MessagePackObject]
    public class LinkedPeakFeature {
        [Key(0)]
        public int LinkedPeakID { get; set; }
        [Key(1)]
        public PeakLinkFeatureEnum Character { get; set; }
    }
}
