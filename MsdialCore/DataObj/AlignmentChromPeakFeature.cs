using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using MessagePack;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class AlignmentChromPeakFeature : IChromatogramPeakFeature {

        // ID metadata
        [Key(0)]
        public int FileID { get; set; }
        [Key(1)]
        public string FileName { get; set; } = string.Empty;
        [Key(2)]
        public int MasterPeakID { get; set; } // sequential IDs parsing all peak features extracted from an MS data
        [Key(3)]
        public int PeakID { get; set; } // sequential IDs from the same dimmension e.g. RT vs MZ or IM vs MZ
        [Key(4)]
        public int ParentPeakID { get; set; } // for LC-IM-MS/MS. The parent peak ID generating the daughter peak IDs
        [Key(5)]
        public int DeconvolutionID { get; set; } // 
        [Key(6)]
        public long SeekPointToDCLFile { get; set; } // deconvoluted spectrum is stored in dcl file, and this is the seek pointer
        [Key(7)]
        public int MS1RawSpectrumID { get; set; }
        [Key(8)]
        public int MS1RawSpectrumIDatAccumulatedMS1 { get; set; } // used for LC-IM-MS/MS
        [Key(9)]
        public int MS2RawSpectrumID { get; set; } // representative ID
        [Key(10)]
        public List<int> MS2RawSpectrumIDs { get; set; }

        // basic property of IChromatogramPeakFeature
        [Key(11)]
        public int ChromScanIdLeft { get; set; }
        [Key(12)]
        public int ChromScanIdTop { get; set; }
        [Key(13)]
        public int ChromScanIdRight { get; set; }
        [Key(14)]
        public ChromXs ChromXsLeft { get; set; }
        [Key(15)]
        public ChromXs ChromXsTop { get; set; }
        [Key(16)]
        public ChromXs ChromXsRight { get; set; }
        [Key(17)]
        public double PeakHeightLeft { get; set; }
        [Key(18)]
        public double PeakHeightTop { get; set; }
        [Key(19)]
        public double PeakHeightRight { get; set; }
        [Key(20)]
        public double PeakAreaAboveZero { get; set; }
        [Key(21)]
        public double PeakAreaAboveBaseline { get; set; }

        public double PeakWidth(ChromXType type) {
            switch (type) {
                case ChromXType.RT: return ChromXsRight.RT.Value - ChromXsLeft.RT.Value;
                case ChromXType.RI: return ChromXsRight.RI.Value - ChromXsLeft.RI.Value;
                case ChromXType.Drift: return ChromXsRight.Drift.Value - ChromXsLeft.Drift.Value;
                default: return ChromXsRight.Value - ChromXsLeft.Value;
            }
        }

        // set for IMMScanProperty
        [Key(22)]
        public double PrecursorMz { get; set; }
        [Key(23)]
        public IonMode IonMode { get; set; }

        // set for IMoleculeProperty (for representative)
        [Key(24)]
        public string Name { get; set; } = string.Empty;
        [Key(25)]
        public Formula Formula { get; set; } = new Formula();
        [Key(26)]
        public string Ontology { get; set; } = string.Empty;
        [Key(27)]
        public string SMILES { get; set; } = string.Empty;
        [Key(28)]
        public string InChIKey { get; set; } = string.Empty;

        // ion physiochemical information
        [Key(29)]
        public double CollisionCrossSection { get; set; }

        // molecule annotation results
        [Key(30)]
        public int MspID { get; set; } // representative msp id
        [Key(31)]
        public List<int> MspIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (optional)
        [Key(32)]
        public int TextDbID { get; set; }// representative text id
        [Key(33)]
        public List<int> TextDbIDs { get; set; } // ID list having the metabolite candidates exceeding the threshold (optional)
        [Key(34)]
        public MsScanMatchResult MspBasedMatchResult { get; set; }
        [Key(35)]
        public MsScanMatchResult TextDbBasedMatchResult { get; set; }

        // peak characters
        [Key(36)]
        public IonFeatureCharacter PeakCharacter { get; set; }
        [Key(37)]
        public ChromatogramPeakShape PeakShape { get; set; }

      
      
    }
}
