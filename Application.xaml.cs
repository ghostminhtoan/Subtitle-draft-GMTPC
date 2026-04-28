using System.Windows;
using Subtitle_draft_GMTPC;
using Subtitle_draft_GMTPC.Services;

/// <summary>
/// Application-level events, such as Startup, Exit, and DispatcherUnhandledException
/// can be handled in this file.
/// </summary>
public partial class Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        PortableAppBootstrapper.Initialize();

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
