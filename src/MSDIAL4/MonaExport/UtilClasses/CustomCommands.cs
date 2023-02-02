using System.Windows.Input;

namespace edu.ucdavis.fiehnlab.MonaExport
{
	public static class CustomCommands {
        public static RoutedCommand MarkForExport = new RoutedCommand();
        public static RoutedCommand ExportMultiple = new RoutedCommand();
        public static RoutedCommand ExportSingle = new RoutedCommand();   
    }
}
