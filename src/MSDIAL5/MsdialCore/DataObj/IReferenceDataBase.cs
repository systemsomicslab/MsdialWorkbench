using System;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    public interface IReferenceDataBase
    {
        string Id { get; }
        void Save(Stream stream, bool forceSerialize = false);
        void Load(Stream stream, string folderpath);
    }
}

