using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using VisualAlgoritmi_Studio.Config;
using VisualAlgoritmi_Studio.Diagnostics;
using VisualAlgoritmi_Studio.ViewModels;
using VisualAlgoritmi_Studio.Views;

namespace VisualAlgoritmi_Studio
{
    public partial class App : Application
    {
        public static Settings Settings { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                CrashReporter.WriteCrashReport(e.Exception, "Avalonia UI thread exception");
            };

            Settings = SettingsIO.Load();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                    WindowState = Settings.StartWindowMaximized
                        ? WindowState.Maximized
                        : WindowState.Normal
                };

                desktop.MainWindow = mainWindow;

                desktop.Exit += (_, _) =>
                {
                    SettingsIO.Save(Settings);
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}