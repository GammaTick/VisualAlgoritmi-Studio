using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace VisualAlgoritmi_Studio.Controls.CreditsFooter;

public partial class CreditsFooter : UserControl
{
    public CreditsFooter()
    {
        InitializeComponent();
    }

    private async void GitHubLink_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.Launcher is not null)
        {
            await topLevel.Launcher.LaunchUriAsync(new Uri("https://github.com/GammaTick"));
        }
    }
}
