using CompMs.App.Msdial.Dto;
using CompMs.Graphics.UI;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Properties
{
    internal sealed partial class Settings : System.Configuration.ApplicationSettingsBase {
        [System.Configuration.UserScopedSetting()]
        public List<ProjectCrumb> PreviousProjects {
            get {
                return (List<ProjectCrumb>)this["PreviousProjects"];
            }
            set {
                this["PreviousProjects"] = value;
            }
        }

        [System.Configuration.UserScopedSetting()]
        public DockLayoutElement ChromatogramsViewLayoutTemplate {
            get {
                return (DockLayoutElement)this["ChromatogramsViewLayoutTemplate"];
            }
            set {
                this["ChromatogramsViewLayoutTemplate"] = value;
            }
        }

        [System.Configuration.UserScopedSetting()]
        public DockLayoutElement AccumulatedSpectrumViewLayoutTemplate {
            get {
                return (DockLayoutElement)this["AccumulatedSpectrumViewLayoutTemplate"];
            }
            set {
                this["AccumulatedSpectrumViewLayoutTemplate"] = value;
            }
        }
    }
}
