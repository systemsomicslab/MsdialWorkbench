using CompMs.Common.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Algorithm
{
    public interface IDataProvider
    {
        // TODO: in future, rawmeasurement will delete.
        RawMeasurement LoadMeasurement();
        List<RawSpectrum> LoadRawSpectrum();
    }
}
