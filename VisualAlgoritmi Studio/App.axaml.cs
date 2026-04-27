using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using VisualAlgoritmi_Studio.Config;
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
            Settings = SettingsIO.Load();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();

                mainWindow.DataContext = new MainWindowViewModel();

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