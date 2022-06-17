using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    internal interface ILipidDatabase : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IReferenceDataBase, IDisposable
    {
        List<MoleculeMsReference> Generates(IEnumerable<ILipid> lipids, ILipid seed, AdductIon adduct, MoleculeMsReference baseReference);

        List<MoleculeMsReference> GetReferences();
        void SetReferences(IEnumerable<MoleculeMsReference> references);
    }
}
