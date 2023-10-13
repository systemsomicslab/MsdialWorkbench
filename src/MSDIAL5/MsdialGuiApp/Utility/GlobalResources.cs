using CompMs.App.Msdial.Properties;
using System;
using System.Linq;

namespace CompMs.App.Msdial.Utility
{
    internal sealed class GlobalResources
    {
        public static GlobalResources Instance { get; } = new GlobalResources();

        // DON'T EXPLICITLY CREATE NEW INSTANCES!
        // DON'T ADD MUTABLE PROPERTIES!
        private GlobalResources() {

        }

        public string Version {
            get {
                return Resources.VERSION;
            }
        }

        public string LatestUpdate {
            get {
                var versionDate = new string(Version.TakeWhile(c => c != '-').ToArray()).Split('.')[2]; // (major).(minor).(buildDate)-(dev)
                var date = DateTime.ParseExact(versionDate, "yyMMdd", new System.Globalization.CultureInfo("en-us"));
                var suffix = "th";
                switch (date.Day) {
                    case 1:
                    case 21:
                    case 31:
                        suffix = "st";
                        break;
                    case 2:
                    case 22:
                        suffix = "nd";
                        break;
                    case 3:
                    case 23:
                        suffix = "rd";
                        break;
                }
                return string.Format("{0:MMM}. {1}{2}, {0:yyyy}", date, date.Day, suffix);
            }
        }

        public bool IsLabPrivate {
            get {
                var version = Version;
                return version.EndsWith("-tada")
                    || version.EndsWith("-dev");
            }
        }
    }
}
