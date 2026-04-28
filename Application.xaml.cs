using System.Windows;
using Subtitle_draft_GMTPC;
using Subtitle_draft_GMTPC.Services;

/// <summary>
/// Application-level events, such as Startup, Exit, and DispatcherUnhandledException
/// can be handled in this file.
/// </summary>
public partial class Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        PortableAppBootstrapper.Initialize();

        try
        {
            await WebView2RuntimeManager.EnsureInstalledAsync();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(
                "Không thể chuẩn bị WebView2 Runtime. Một số phần của app có thể không hiển thị đúng.\n\n" + ex.Message,
                "Cảnh báo WebView2",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
