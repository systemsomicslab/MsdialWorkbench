namespace CompMs.App.Msdial.Utility
{
    internal sealed class GlobalResources
    {
        public static GlobalResources Instance { get; } = new GlobalResources();

        // DON'T EXPLICITLY CREATE NEW INSTANCES!
        // DON'T ADD MUTABLE PROPERTIES!
        private GlobalResources() {

        }

        public bool IsLabPrivate {
            get {
                var version = Properties.Resources.VERSION;
                return version.EndsWith("-tada")
                    || version.EndsWith("-alpha")
                    || version.EndsWith("-beta")
                    || version.EndsWith("-dev");
            }
        }
    }
}
