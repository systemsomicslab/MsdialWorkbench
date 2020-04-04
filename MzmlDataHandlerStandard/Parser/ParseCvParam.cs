using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MzmlHandler.Parser
{
    public class ParseCvParam
    {
    }

    public enum MzMlDataFileContent
    {
        Undefined, MassSpectrum, ChargeInversionMassSpectrum, ConstantNeutralGainSpectrum, ConstantNeutralLossSpectrum, PrecursorIonSpectrum, ProductIonSpectrum,
        MS1Spectrum, MSnSpectrum, CRMSpectrum, SIMSpectrum, SRMSpectrum, PDASpectrum, EnhancedMultiplyChargedSpectrum, TimeDelayedFragmentationSpectrum,
        ElectromagneticRadiationSpectrum, EmissionSpectrum, AbsorptionSpectrum,
        TICchromatogram, BasepeakChromatogram, SICchromatogram, MassChromatogram, ElectromagneticRadiationChromatogram, AbsorptionChromatogram, EmissionChromatogram,
        SIMchromatogram, SRMchromatogram, CRMchromatogram,
    }

    public enum MzMlScanPolarity
    {
        Undefined, Positive, Negative, Alternating
    }

    public enum MzMlSpectrumRepresentation
    {
        Undefined, Centroid, Profile
    }

    public enum MzMlUnits
    {
        Undefined, Second, Minute, Mz, NumberOfCounts, ElectronVolt, Millisecond
    }

    public enum MzMlDissociationMethods
    {
        Undefined, CID, PD, PSD, SID, BIRD, ECD, IRMPD, SORI, HCD, LowEnergyCID, MPD, ETD, PQD, InSourceCID, LIFT,
    }

    public partial class MzmlReader
    {
        public enum CvParamTypes
        {
            Undefined, DataFileContent,
            MsLevel, ScanPolarity, SpectrumRepresentation,
            BasePeakMz, BasePeakIntensity, TotalIonCurrent, HighestObservedMz, LowestObservedMz,
            ScanStartTime, ScanWindowLowerLimit, ScanWindowUpperLimit, IsolationWindowTargetMz, IsolationWindowLowerOffset, IsolationWindowUpperOffset,
            SelectedIonMz, CollisionEnergy, DissociationMethod, IonMobilityDriftTime
        }

        public class CvParamValue
        {
            internal CvParamTypes paramType = CvParamTypes.Undefined;
            internal string valueString = "";
            internal object value = null;
            internal MzMlUnits unit = MzMlUnits.Undefined;
        }

        internal CvParamValue parseCvParam()
        {
            var ret = new CvParamValue();
            while (this.xmlRdr.MoveToNextAttribute())
            {
                switch (this.xmlRdr.Name)
                {
                    case "accession":
                        switch (this.xmlRdr.Value)
                        {
                            #region cases
                            case "MS:1000235": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.TICchromatogram; break;
                            case "MS:1000294": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.MassSpectrum; break;
                            case "MS:1000322": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.ChargeInversionMassSpectrum; break;
                            case "MS:1000325": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.ConstantNeutralGainSpectrum; break;
                            case "MS:1000326": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.ConstantNeutralLossSpectrum; break;
                            case "MS:1000341": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.PrecursorIonSpectrum; break;
                            case "MS:1000343": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.ProductIonSpectrum; break;
                            case "MS:1000579": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.MS1Spectrum; break;
                            case "MS:1000580": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.MSnSpectrum; break;
                            case "MS:1000581": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.CRMSpectrum; break;
                            case "MS:1000582": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.SIMSpectrum; break;
                            case "MS:1000583": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.SRMSpectrum; break;
                            case "MS:1000620": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.PDASpectrum; break;
                            case "MS:1000627": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.SICchromatogram; break;
                            case "MS:1000628": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.BasepeakChromatogram; break;
                            case "MS:1000789": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.EnhancedMultiplyChargedSpectrum; break;
                            case "MS:1000790": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.TimeDelayedFragmentationSpectrum; break;
                            case "MS:1000804": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.ElectromagneticRadiationSpectrum; break;
                            case "MS:1000805": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.EmissionSpectrum; break;
                            case "MS:1000806": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.AbsorptionSpectrum; break;
                            case "MS:1000810": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.MassChromatogram; break;
                            case "MS:1000811": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.ElectromagneticRadiationChromatogram; break;
                            case "MS:1000812": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.AbsorptionChromatogram; break;
                            case "MS:1000813": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.EmissionChromatogram; break;
                            case "MS:1001472": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.SIMchromatogram; break;
                            case "MS:1001473": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.SRMchromatogram; break;
                            case "MS:1001474": ret.paramType = CvParamTypes.DataFileContent; ret.value = MzMlDataFileContent.CRMchromatogram; break;
                            case "MS:1000511": ret.paramType = CvParamTypes.MsLevel; break;
                            case "MS:1000129": ret.paramType = CvParamTypes.ScanPolarity; ret.value = MzMlScanPolarity.Negative; break;
                            case "MS:1000130": ret.paramType = CvParamTypes.ScanPolarity; ret.value = MzMlScanPolarity.Positive; break;
                            case "MS:1000127": ret.paramType = CvParamTypes.SpectrumRepresentation; ret.value = MzMlSpectrumRepresentation.Centroid; break;
                            case "MS:1000128": ret.paramType = CvParamTypes.SpectrumRepresentation; ret.value = MzMlSpectrumRepresentation.Profile; break;
                            case "MS:1000504": ret.paramType = CvParamTypes.BasePeakMz; break;
                            case "MS:1000505": ret.paramType = CvParamTypes.BasePeakIntensity; break;
                            case "MS:1000285": ret.paramType = CvParamTypes.TotalIonCurrent; break;
                            case "MS:1000527": ret.paramType = CvParamTypes.HighestObservedMz; break;
                            case "MS:1000528": ret.paramType = CvParamTypes.LowestObservedMz; break;
                            case "MS:1000016": ret.paramType = CvParamTypes.ScanStartTime; break;
                            case "MS:1002476": ret.paramType = CvParamTypes.IonMobilityDriftTime; break;
                            case "MS:1000500": ret.paramType = CvParamTypes.ScanWindowUpperLimit; break;
                            case "MS:1000501": ret.paramType = CvParamTypes.ScanWindowLowerLimit; break;
                            case "MS:1000827": ret.paramType = CvParamTypes.IsolationWindowTargetMz; break;
                            case "MS:1000828": ret.paramType = CvParamTypes.IsolationWindowLowerOffset; break;
                            case "MS:1000829": ret.paramType = CvParamTypes.IsolationWindowUpperOffset; break;
                            case "MS:1000744": ret.paramType = CvParamTypes.SelectedIonMz; break;
                            case "MS:1000133": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.CID; break;
                            case "MS:1000134": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.PD; break;
                            case "MS:1000135": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.PSD; break;
                            case "MS:1000136": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.SID; break;
                            case "MS:1000242": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.BIRD; break;
                            case "MS:1000250": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.ECD; break;
                            case "MS:1000262": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.IRMPD; break;
                            case "MS:1000282": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.SORI; break;
                            case "MS:1000422": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.HCD; break;
                            case "MS:1000433": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.LowEnergyCID; break;
                            case "MS:1000435": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.MPD; break;
                            case "MS:1000598": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.ETD; break;
                            case "MS:1000599": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.PQD; break;
                            case "MS:1001880": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.InSourceCID; break;
                            case "MS:1002000": ret.paramType = CvParamTypes.DissociationMethod; ret.value = MzMlDissociationMethods.LIFT; break;
                            case "MS:1000045": ret.paramType = CvParamTypes.CollisionEnergy; break;
                            default: 
                                //Debug.Print("!!! parseCvParam: Unsupported accession={0}; D={1}; RdSt={2}", this.xmlRdr.Value, this.xmlRdr.Depth, this.xmlRdr.ReadState); 
                                break;
                            #endregion
                        }
                        break;
                    case "value": ret.valueString = this.xmlRdr.Value; break;
                    case "unitAccession":
                        switch (this.xmlRdr.Value)
                        {
                            #region cases
                            case "UO:0000010": ret.unit = MzMlUnits.Second; break;
                            case "UO:0000031": ret.unit = MzMlUnits.Minute; break;
                            case "UO:0000266": ret.unit = MzMlUnits.ElectronVolt; break;
                            case "UO:0000028": ret.unit = MzMlUnits.Millisecond; break;
                            case "MS:1000040": ret.unit = MzMlUnits.Mz; break;
                            case "MS:1000131": ret.unit = MzMlUnits.NumberOfCounts; break;
                            default: 
                                //Debug.Print("!!! parseCvParam: Unsupported unitAccession={0}; D={1}; RdSt={2}", this.xmlRdr.Value, this.xmlRdr.Depth, this.xmlRdr.ReadState); 
                                break;
                            #endregion
                        }
                        break;
                }
            }

            int n; double d;
            switch (ret.paramType)
            {
                #region cases
                case CvParamTypes.MsLevel:
                    if (Int32.TryParse(ret.valueString, out n)) ret.value = n;
                    break;
                case CvParamTypes.BasePeakMz:
                case CvParamTypes.BasePeakIntensity:
                case CvParamTypes.TotalIonCurrent:
                case CvParamTypes.HighestObservedMz:
                case CvParamTypes.LowestObservedMz:
                case CvParamTypes.ScanStartTime:
                case CvParamTypes.IonMobilityDriftTime:
                case CvParamTypes.ScanWindowUpperLimit:
                case CvParamTypes.ScanWindowLowerLimit:
                case CvParamTypes.IsolationWindowTargetMz:
                case CvParamTypes.IsolationWindowLowerOffset:
                case CvParamTypes.IsolationWindowUpperOffset:
                case CvParamTypes.SelectedIonMz:
                case CvParamTypes.CollisionEnergy:
                    if (Double.TryParse(ret.valueString, out d)) ret.value = d;
                    break;
                default:
                    if (ret.value == null) ret.value = ret.valueString;
                    break;
                #endregion
            }
            return ret;
        }
    }
}
