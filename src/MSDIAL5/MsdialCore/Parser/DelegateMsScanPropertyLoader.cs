using CompMs.Common.Interfaces;
using System;

namespace CompMs.MsdialCore.Parser;

public class DelegateMsScanPropertyLoader<T>(Func<T, IMSScanProperty> map) : IMsScanPropertyLoader<T>
{
    IMSScanProperty IMsScanPropertyLoader<T>.Load(T source) {
        return map(source);
    }
}
