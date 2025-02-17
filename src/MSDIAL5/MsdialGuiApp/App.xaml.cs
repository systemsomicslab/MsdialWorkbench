using System;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            DispatcherUnhandledException += (s, e) => {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, e) => {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source) {
            // Log the exception
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, Msdial.Properties.Resources.VERSION);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write($"Exception in LogUnhandledException\n\n{ex.Message}");
            }
            finally
            {
                System.Diagnostics.Debug.Write($"{message}\n\n{exception.Message}");
                MessageBox.Show($"{message}\n\n{exception.Message}\n\nStack trace below:\n\n{exception.StackTrace}", $"Unexpected exception occured.");
            }
        }
    }
}
