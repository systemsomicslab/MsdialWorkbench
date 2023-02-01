using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex = new Mutex(false, "MSFINDER");

        private void Application_Startup(object sender, StartupEventArgs e) {
            mutex = new System.Threading.Mutex(false, "MSFINDER");
            var mainWindow = new MainWindow();

            if (mutex.WaitOne(0, false)) {
                IpcServerChannel serverChannel = new IpcServerChannel("MSFINDER");
                ChannelServices.RegisterChannel(serverChannel, true);

                AppRemoteHandler message = new AppRemoteHandler();
                message.handler = delegate (string[] args) {
                    if (args.Length > 1) {
                        var folderPath = string.Empty;
                        foreach (var arg in args) folderPath += arg + " ";
                        folderPath = folderPath.Substring(0, folderPath.Length - 1);

                        mainWindow.MainWindowVM.DataStorageBean.ImportFolderPath = folderPath;
                        mainWindow.MainWindowVM.Refresh_ImportFolder(folderPath);
                    }
                    else if (args.Length == 1) {
                        mainWindow.MainWindowVM.DataStorageBean.ImportFolderPath = args[0];
                        if (args[0] != string.Empty) mainWindow.MainWindowVM.Refresh_ImportFolder(args[0]);
                    }
                };

                if (e.Args.Length == 1) mainWindow.MainWindowVM.DataStorageBean.ImportFolderPath = e.Args[0];
                else if (e.Args.Length > 1) {
                    var folderPath = string.Empty;
                    foreach (var arg in e.Args) folderPath += arg + " ";
                    folderPath = folderPath.Substring(0, folderPath.Length - 1);

                    mainWindow.MainWindowVM.DataStorageBean.ImportFolderPath = folderPath;
                }

                RemotingServices.Marshal(message, "Message");

                mainWindow.Show();
            }
            else {
                IpcClientChannel clientChannel = new IpcClientChannel();
                ChannelServices.RegisterChannel(clientChannel, true);
                AppRemoteHandler m = RemotingServices.Connect(typeof(AppRemoteHandler), "ipc://MSFINDER/Message") as AppRemoteHandler;
                m.RaiseHandler(e.Args);

                Shutdown();
            }
        }

        delegate void ConnectHandler(string[] args);

        class AppRemoteHandler : MarshalByRefObject {
            public ConnectHandler handler;

            public void RaiseHandler(string[] args) {
                Application.Current.Dispatcher.BeginInvoke(handler, new object[] { args });
            }

            public override object InitializeLifetimeService() {
                return null;
            }
        }
    }
}
