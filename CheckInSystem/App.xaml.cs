using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CheckInSystem;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    void App_Startup(object sender, StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += log;
        try
        {
            if (!Database.Database.EnsureDatabaseAvailable())
            {
                MessageBox.Show("Kunne ikke oprette forbindelse til databasen!",
                    "Uforventet Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();

                return;
            }

            CheckInSystem.Startup.Run();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception);
            throw;
        }
    }

    private static void log(object sender, UnhandledExceptionEventArgs e)
    {
        string filePath = Environment.ExpandEnvironmentVariables(@"%AppData%\checkInSystem");
        if (!Directory.Exists(filePath))
        {
            Directory.CreateDirectory(filePath);
        }
        filePath += @"\log.txt";
        File.AppendAllText(filePath, $"At {DateTime.Now} {e}\r\n");
    }
    
}
