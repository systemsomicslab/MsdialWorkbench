using System;

namespace CompMs.Common.DataObj;

/// <summary>
/// Represents the type of spectrum identifier used in the <see cref="ISpectrumIdentifier"/> ID property.
/// </summary>
/// <remarks>
/// This enum is used to differentiate between two types of spectrum identifiers:
/// 
/// - <see cref="Index"/>: Represents a sequential index that corresponds to the position 
///   of a spectrum when all raw data is read sequentially from the beginning. 
///   It is a fixed, order-dependent identifier.
/// - <see cref="Raw"/>: Represents an identifier derived directly from the raw data. 
///   This ID depends on the data and the method used to load it. Unlike <see cref="Index"/>, 
///   it does not guarantee the same value across different loading methods and may not be sequential.
/// 
/// The need for this enum arose from the limitations of using <see cref="Index"/> alone. 
/// Index-based identifiers require all spectra in the raw data to be read sequentially, which 
/// can be inefficient or impractical when loading only specific spectra. The <see cref="Raw"/> 
/// identifier provides a more flexible option that enables partial data loading while maintaining 
/// a unique reference to individual spectra.
/// </remarks>
public enum SpectrumIDType {
    /// <summary>
    /// A sequential index representing the position of the spectrum 
    /// when all raw data is read sequentially from the beginning.
    /// </summary>
    Index,

    /// <summary>
    /// An identifier derived directly from the raw data. 
    /// This ID depends on the data and the loading method, 
    /// and it is not guaranteed to correspond to a sequential index.
    /// </summary>
    Raw,
}

public interface ISpectrumIdentifier : IEquatable<ISpectrumIdentifier>
{
    ulong ID { get; }
    SpectrumIDType IDType { get; }
}
