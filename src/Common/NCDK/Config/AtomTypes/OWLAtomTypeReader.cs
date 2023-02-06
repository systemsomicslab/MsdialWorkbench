using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using static NCDK.Config.AtomTypes.OWLConstants;

namespace NCDK.Config.AtomTypes
{
    public class OWLAtomTypeReader 
        : IDisposable
    {
        private readonly IChemObjectBuilder builder = Silent.ChemObjectBuilder.Instance;
        private TextReader input;

        public OWLAtomTypeReader(Stream input)
        {
            this.input = new StreamReader(input);
        }

        public OWLAtomTypeReader(TextReader input)
        {
            this.input = input;
        }

        public IReadOnlyList<IAtomType> ReadAtomTypes()
        {
            var ret = new List<IAtomType>();

            var doc = XElement.Load(input);

            foreach (var atomTypeElm in doc.Elements(XName_AtomType))
            {
                var anAtomType = builder.NewAtomType("H");
                anAtomType.AtomicNumber = 0;
                anAtomType.AtomTypeName = atomTypeElm.Attribute(XName_ID)?.Value;

                int piBondCount = 0;
                int neighborCount = 0;
                BondOrder maxBondOrder = BondOrder.Unset;
                double bondOrderSum = 0.0;

                foreach (var elm in atomTypeElm.Elements())
                {
                    if (elm.Name == XName_hasElement)
                    {
                        var aa = elm.Attribute(XName_rdf_resource)?.Value;
                        anAtomType.Symbol = aa.Substring(aa.IndexOf('#') + 1);
                    }
                    else if (elm.Name == XName_formalBondType)
                    {
                        neighborCount++;
                        var aa = elm.Attribute(XName_rdf_resource)?.Value;
                        var bondType = aa.Substring(aa.IndexOf('#') + 1);
                        int bondOrder = 0;
                        switch (bondType)
                        {
                            case "single":
                                bondOrder = 1;
                                break;
                            case "double":
                                bondOrder = 2;
                                break;
                            case "triple":
                                bondOrder = 3;
                                break;
                            case "quadruple":
                                bondOrder = 4;
                                break;
                            default:
                                throw new Exception();
                        }
                        maxBondOrder = BondManipulator.GetMaximumBondOrder(maxBondOrder, (BondOrder)bondOrder);
                        piBondCount += (bondOrder - 1);
                        bondOrderSum += bondOrder;
                    }
                    else if (elm.Name == XName_hybridization)
                    {
                        var aa = elm.Attribute(XName_rdf_resource)?.Value;
                        var hybridization = aa.Substring(aa.IndexOf('#') + 1);
                        anAtomType.Hybridization = HybridizationTools.ToHybridization(hybridization);
                    }
                    else if (elm.Name == XName_formalCharge)
                        anAtomType.FormalCharge = int.Parse(elm.Value, NumberFormatInfo.InvariantInfo);
                    else if (elm.Name == XName_formalNeighbourCount)
                        neighborCount = int.Parse(elm.Value, NumberFormatInfo.InvariantInfo);
                    else if (elm.Name == XName_lonePairCount)
                        anAtomType.SetProperty(CDKPropertyName.LonePairCount, int.Parse(elm.Value, NumberFormatInfo.InvariantInfo));
                    else if (elm.Name == XName_singleElectronCount)
                        anAtomType.SetProperty(CDKPropertyName.SingleElectronCount, int.Parse(elm.Value, NumberFormatInfo.InvariantInfo));
                    else if (elm.Name == XName_piBondCount)
                        piBondCount = int.Parse(elm.Value, NumberFormatInfo.InvariantInfo);
                }

                anAtomType.SetProperty(CDKPropertyName.PiBondCount, piBondCount);
                anAtomType.FormalNeighbourCount = neighborCount;
                if (maxBondOrder != BondOrder.Unset)
                    anAtomType.MaxBondOrder = maxBondOrder;
                if (bondOrderSum > 0.1)
                    anAtomType.BondOrderSum = bondOrderSum;

                ret.Add(anAtomType);
            }
            return ret;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input.Dispose();
                }

                input = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
