using CompMs.App.Msdial.Dto;
using CompMs.Graphics.UI;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Properties
{
    internal sealed partial class Settings : System.Configuration.ApplicationSettingsBase {
        [System.Configuration.UserScopedSetting()]
        [System.Configuration.DefaultSettingValue(null)]
        public List<ProjectCrumb> PreviousProjects {
            get {
                return (List<ProjectCrumb>)this["PreviousProjects"];
            }
            set {
                this["PreviousProjects"] = value;
            }
        }

        [System.Configuration.UserScopedSetting()]
        [System.Configuration.DefaultSettingValue(null)]
        public DockLayoutElement ChromatogramsViewLayoutTemplate {
            get {
                return (DockLayoutElement)this["ChromatogramsViewLayoutTemplate"];
            }
            set {
                this["ChromatogramsViewLayoutTemplate"] = value;
            }
        }

        [System.Configuration.UserScopedSetting()]
        [System.Configuration.DefaultSettingValue(null)]
        public DockLayoutElement AccumulatedSpectrumViewLayoutTemplate {
            get {
                return (DockLayoutElement)this["AccumulatedSpectrumViewLayoutTemplate"];
            }
            set {
                this["AccumulatedSpectrumViewLayoutTemplate"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(null)]
        public string RHome {
            get {
                return ((string)(this["RHome"]));
            }
            set {
                this["RHome"] = value;
            }
        }
    }
}
