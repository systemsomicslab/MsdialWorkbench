using CompMs.Common.MessagePack;
using MessagePack;
using System;
using System.IO;

namespace CompMs.MsdialCore.Parameter
{
    [MessagePackObject]
    public class ProjectParameter
    {
        public ProjectParameter(DateTime startDate, string folderPath, string title) {
            StartDate = startDate;
            FolderPath = folderPath;
            Title = title;
        }

        /// <summary>
        /// DO NOT USE. This constructor is for MessagePack for C#.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="finalSavedDate"></param>
        /// <param name="folderPath"></param>
        /// <param name="title"></param>
        [SerializationConstructor]
        public ProjectParameter(DateTime startDate, DateTime finalSavedDate, string folderPath, string title) {
            StartDate = startDate.ToLocalTime();
            FinalSavedDate = finalSavedDate.ToLocalTime();
            FolderPath = folderPath;
            Title = title;
        }

        [Key(nameof(StartDate))]
        public DateTime StartDate { get; }

        [Key(nameof(FinalSavedDate))]
        public DateTime FinalSavedDate { get; private set; }

        [Key(nameof(FolderPath))]
        public string FolderPath { get; private set; }

        [Key(nameof(Title))]
        public string Title { get; }

        [IgnoreMember]
        public string FilePath => Path.Combine(FolderPath, Title);

        public void FixProjectFolder(string newFolderPath) {
            FolderPath = newFolderPath;
        }

        public void Save(Stream stream) {
            FinalSavedDate = DateTime.Now;
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static ProjectParameter Load(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<ProjectParameter>(stream);
        }
    }
}
