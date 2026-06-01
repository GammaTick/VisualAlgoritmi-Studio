using Avalonia;
using Avalonia.Controls;

namespace VisualAlgoritmi_Studio.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property != WindowStateProperty)
            {
                return;
            }

            if (WindowState == WindowState.Minimized)
            {
                return;
            }

            App.Settings.StartWindowMaximized = WindowState == WindowState.Maximized;
            App.Settings.Save();
        }
    }
}