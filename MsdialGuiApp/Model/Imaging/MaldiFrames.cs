using CompMs.Common.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class MaldiFrames
    {
        private readonly List<MaldiFrameInfo> _infos;

        public MaldiFrames(IEnumerable<MaldiFrameInfo> infos, MaldiFrames masterFrames) {
            _infos = infos.ToList();
            Infos = _infos.AsReadOnly();

            XIndexPosMin = masterFrames.XIndexPosMin;
            XIndexPosMax = masterFrames.XIndexPosMax;
            YIndexPosMin = masterFrames.YIndexPosMin;
            YIndexPosMax = masterFrames.YIndexPosMax;
        }

        public MaldiFrames(IEnumerable<MaldiFrameInfo> infos) {
            _infos = infos.ToList();
            Infos = _infos.AsReadOnly();

            XIndexPosMin = infos.Select(info => info.XIndexPos).DefaultIfEmpty(0).Min();
            XIndexPosMax = infos.Select(info => info.XIndexPos).DefaultIfEmpty(1).Max();
            YIndexPosMin = infos.Select(info => info.YIndexPos).DefaultIfEmpty(0).Min();
            YIndexPosMax = infos.Select(info => info.YIndexPos).DefaultIfEmpty(1).Max();
        }

        public ReadOnlyCollection<MaldiFrameInfo> Infos { get; }

        public int XIndexPosMin { get; }
        public int XIndexPosMax { get; }
        public int YIndexPosMin { get; }
        public int YIndexPosMax { get; }
        public int XIndexWidth => XIndexPosMax - XIndexPosMin + 1;
        public int YIndexHeight => YIndexPosMax - YIndexPosMin + 1;
    }
}
