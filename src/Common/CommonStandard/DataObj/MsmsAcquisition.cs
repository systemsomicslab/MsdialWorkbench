using CompMs.Common.Enum;
using System.Collections.Generic;

namespace CompMs.Common.DataObj;

public sealed class MsmsAcquisition
{
    private static readonly Dictionary<AcquisitionType, MsmsAcquisition> _acquisitionTypeMap = new() {
        [AcquisitionType.DDA] = new MsmsAcquisition { IsDda = true, },
        [AcquisitionType.SWATH] = new MsmsAcquisition { IsDda = false, },
        [AcquisitionType.AIF] = new MsmsAcquisition { IsDda = false, MultipleCollisionEnergy = true, },
        [AcquisitionType.ZTScan] = new MsmsAcquisition { IsDda = false, NeedQ1Deconvolution = true, },
    };

    public static readonly MsmsAcquisition None = new();

    private MsmsAcquisition() { }

    public bool IsDda { get; private set; } = true;

    public bool IsDia => !IsDda;

    public bool MultipleCollisionEnergy { get; private set; } = false;

    public bool NeedQ1Deconvolution { get; private set; } = false;

    public static MsmsAcquisition? Get(AcquisitionType type) {
        if (_acquisitionTypeMap.TryGetValue(type, out var acquisition)) {
            return acquisition;
        }
        return null;
    }

    public static MsmsAcquisition GetOrDefault(AcquisitionType type) {
        return Get(type) ?? None;
    }
}
