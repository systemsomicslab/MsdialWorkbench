using CompMs.App.Msdial.Dto;
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
    }
}
