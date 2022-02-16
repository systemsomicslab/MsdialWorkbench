using CompMs.Common.MessagePack;
using MessagePack;
using System;
using System.IO;

namespace CompMs.MsdialCore.Parameter
{
    [MessagePackObject]
    public class ProjectParameter
    {
        [SerializationConstructor]
        public ProjectParameter(DateTime startDate, string folderPath, string title) {
            StartDate = startDate;
            FolderPath = folderPath;
            Title = title;
        }

        [Key(nameof(StartDate))]
        public DateTime StartDate { get; }

        [Key(nameof(FinalSavedDate))]
        public DateTime FinalSavedDate { get; private set; }

        [Key(nameof(FolderPath))]
        public string FolderPath { get; }

        [Key(nameof(Title))]
        public string Title { get; }

        public void Save(Stream stream) {
            FinalSavedDate = DateTime.UtcNow;
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static ProjectParameter Load(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<ProjectParameter>(stream);
        }
    }
}
