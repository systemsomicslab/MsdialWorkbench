using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Parser;

public sealed class RIChromatogramSerializerDecorator : ChromatogramSerializer<ChromatogramSpotInfo>
{
    private readonly ChromatogramSerializer<ChromatogramSpotInfo> _serializer;
    private readonly IReadOnlyDictionary<int, RetentionIndexHandler> _handlers;

    public RIChromatogramSerializerDecorator(ChromatogramSerializer<ChromatogramSpotInfo> serializer, IReadOnlyDictionary<int, RetentionIndexHandler> handlers)
    {
        _serializer = serializer;
        _handlers = handlers;
    }

    public override ChromatogramSpotInfo Deserialize(Stream stream) {
        var result = _serializer.Deserialize(stream);
        return Convert(result);
    }

    public override IEnumerable<ChromatogramSpotInfo> DeserializeAll(Stream stream) {
        var results = _serializer.DeserializeAll(stream);
        return results.Select(Convert).ToList();
    }

    public override ChromatogramSpotInfo DeserializeAt(Stream stream, int index) {
        var result = _serializer.DeserializeAt(stream, index);
        return Convert(result);
    }

    public override IEnumerable<ChromatogramSpotInfo> DeserializeEach(Stream stream, IEnumerable<int> indices) {
        var results = _serializer.DeserializeEach(stream, indices);
        return results.Select(Convert).ToList();
    }

    public override void Serialize(Stream stream, ChromatogramSpotInfo chromatogram) {
        _serializer.Serialize(stream, chromatogram);
    }

    public override void SerializeAll(Stream stream, IEnumerable<ChromatogramSpotInfo> chromatograms) {
        _serializer.SerializeAll(stream, chromatograms);
    }

    public override void SerializeN(Stream stream, IEnumerable<ChromatogramSpotInfo> chromatograms, int num) {
        _serializer.SerializeN(stream, chromatograms, num);
    }

    private ChromatogramSpotInfo Convert(ChromatogramSpotInfo spotInfo) {
        if (spotInfo.ChromXs.MainType != Common.Components.ChromXType.RI) {
            return spotInfo;
        }
        foreach (var peak in spotInfo.PeakInfos) {
            var handler = _handlers[peak.FileID];
            peak.ChromXsLeft.RT = handler.ConvertBack(peak.ChromXsLeft.RI);
            peak.ChromXsTop.RT = handler.ConvertBack(peak.ChromXsTop.RI);
            peak.ChromXsRight.RT = handler.ConvertBack(peak.ChromXsRight.RI);
        }
        return spotInfo;
    }
}
