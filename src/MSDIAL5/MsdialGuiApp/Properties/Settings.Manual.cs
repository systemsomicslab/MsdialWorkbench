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

        // A SETTING PROPERTY MUST NOT BE DECLARED string?.
        //
        // The compiler marks a string? property with NullableAttribute, and ApplicationSettingsBase
        // stores each property's attributes in a table keyed by attribute type. On .NET Framework that
        // throws "Item has already been added" for NullableAttribute, and because every property is
        // read at once on first access, every setting fails -- not just the nullable one. The values
        // below can still be null; callers treat them as string?.

        /// <summary>
        /// Until when "Remind me later" holds back the update notification, as a round-trip UTC string.
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(null)]
        public string UpdateRemindAfterUtc {
            get {
                return ((string)(this["UpdateRemindAfterUtc"]));
            }
            set {
                this["UpdateRemindAfterUtc"] = value;
            }
        }

        /// <summary>
        /// The release version for which the user chose "Skip this version".
        /// </summary>
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(null)]
        public string UpdateSkippedVersion {
            get {
                return ((string)(this["UpdateSkippedVersion"]));
            }
            set {
                this["UpdateSkippedVersion"] = value;
            }
        }
    }
}
