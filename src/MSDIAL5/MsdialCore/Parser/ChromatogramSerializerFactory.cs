using System;
using System.Collections.Generic;

using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialCore.Parser
{
    public sealed class ChromatogramSerializerFactory
    {
        /*
         * File format
         * CSS1
         * 
         * Version infomation: 10 byte
         * 
         * Content chunk
         *   Number of spot: 4 byte
         *   Seek pointers to spot: 8 byte x number of spot
         * 
         *   Spot Chunks
         *     Retension time: 4 byte
         *     Retension index: 4 byte
         *     Mass: 4 byte
         *     Drift time: 4 byte
         *     MainType: 1 byte
         *     Number of sample: 4 byte
         *     
         *     Peak info chunks
         *       File id: 4 byte
         *       Number of Chromatogram peak: 4 byte
         *       PeakTop horizontal axis value: 4 byte
         *       PeakLeft horizontal axis value: 4 byte
         *       PeakRight horizontal axis value: 4 byte
         *       
         *       Chromatogram chunks
         *         Horizontal value: 4 byte
         *         Intensity: 4 byte
         */

        public static ChromatogramSerializer<ChromatogramSpotInfo>? CreateSpotSerializer(string version, ChromXType mainType = ChromXType.RT) {
            ChromatogramSerializer<ChromatogramSpotInfo> result = null;
            if (version == "CSS1") {
                var ps = new PeakInfoSerializer(mainType);
                var ss = new SpotInfoSerializer(ps);
                var ads = new ChromatogramAccessDecorator<ChromatogramSpotInfo>(ss);
                result = new ChromatogramVersionDecorator<ChromatogramSpotInfo>(ads, version);
            }
            return result;
        }

        /*
         * File format
         * CPS1
         * 
         * Version infomation: 10 byte
         * 
         * Content chunk
         *   Number of peak: 4 byte
         *   Seek pointers to spot: 8 byte x number of peak
         * 
         *   Peak info chunks
         *     File id: 4 byte
         *     Number of Chromatogram peak: 4 byte
         *     PeakTop horizontal axis value: 4 byte
         *     PeakLeft horizontal axis value: 4 byte
         *     PeakRight horizontal axis value: 4 byte
         *     
         *     Chromatogram chunks
         *       Horizontal value: 4 byte
         *       Intensity: 4 byte
         *
         *------------------------------------------------------
         * CPSTMP
         * 
         * Peak info chunks
         *   File id: 4 byte
         *   Number of Chromatogram peak: 4 byte
         *   PeakTop horizontal axis value: 4 byte
         *   PeakLeft horizontal axis value: 4 byte
         *   PeakRight horizontal axis value: 4 byte
         *   
         *   Chromatogram chunks
         *     Horizontal value: 4 byte
         *     Intensity: 4 byte
         */

        public static ChromatogramSerializer<ChromatogramPeakInfo>? CreatePeakSerializer(string version, ChromXType mainType = ChromXType.RT) {
            if (version == "CPS1") {
                var ps = new PeakInfoSerializer(mainType);
                var ads = new ChromatogramAccessDecorator<ChromatogramPeakInfo>(ps);
                return new ChromatogramVersionDecorator<ChromatogramPeakInfo>(ads, version);
            }
            if (version == "CPSTMP") {
                var ps = new PeakInfoSerializer(mainType);
                return new ChromatogramAccessDecorator<ChromatogramPeakInfo>(ps);
            }
            return null;
        }
    }
}
