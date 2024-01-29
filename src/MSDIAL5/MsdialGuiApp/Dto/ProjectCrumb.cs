using CompMs.MsdialCore.Parameter;
using System;
using System.Xml.Serialization;

namespace CompMs.App.Msdial.Dto
{
    [XmlRoot("ProjectCrumb")]
    public sealed class ProjectCrumb
    {
        public ProjectCrumb() {

        }

        public ProjectCrumb(ProjectParameter parameter) {
            Title = parameter.Title;
            FilePath = parameter.FilePath;
            FinalSavedDate = parameter.FinalSavedDate;
        }

        [XmlElement("Title")]
        public string? Title { get; set; }
        [XmlElement("FilePath")]
        public string? FilePath { get; set; }
        [XmlElement("FinalSavedDate")]
        public DateTime FinalSavedDate { get; set; }

        public bool MaybeSame(ProjectCrumb other) {
            return Title == other.Title;
        }
    }
}
