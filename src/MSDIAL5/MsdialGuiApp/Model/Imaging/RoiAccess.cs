using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imaging;

internal sealed class RoiAccess
{
    private readonly List<int> _idxs;

    public RoiAccess(RoiModel subset, RoiModel? master)
    {
        if (master is null) {
            _idxs = Enumerable.Range(0, subset.Frames.Infos.Count).ToList();
            return;
        }

        var map = new Dictionary<(int, int), int>();
        for (int i = 0; i < master.Frames.Infos.Count; i++) {
            var info = master.Frames.Infos[i];
            map[(info.XIndexPos, info.YIndexPos)] = i;
        }

        var idxs = new List<int>(subset.Frames.Infos.Count);
        foreach (var info in subset.Frames.Infos) {
            idxs.Add(map[(info.XIndexPos, info.YIndexPos)]);
        }
        _idxs = idxs;
    }

    public T[] Access<T>(T[] array) {
        var result = new T[_idxs.Count];
        for (int i = 0; i < _idxs.Count; i++) {
            result[i] = array[_idxs[i]];
        }
        return result;
    }
}
