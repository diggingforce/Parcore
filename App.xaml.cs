using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Parcore;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        SettingsManager.Load();
        SettingsManager.ApplyTheme();
        base.OnStartup(e);
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log(e.Exception);
        e.Handled = true;
        MessageBox.Show(e.Exception.Message, "Parcore crashed", MessageBoxButton.OK, MessageBoxImage.Error);
        Shutdown(1);
    }

    private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Log(ex);
    }

    private static void Log(Exception ex)
    {
        try
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "parcore_crash.log");

            File.AppendAllText(path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n\n");
        }
        catch { }
    }
}
