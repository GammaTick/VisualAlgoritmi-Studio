using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Collections.Generic;

namespace VisualAlgoritmi_Studio.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private async void BrowseProject_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
        {
            return;
        }

        IStorageProvider storage = topLevel.StorageProvider;

        if (!storage.CanOpen)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files =
            await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select VisualAlgoritmi Studio Project",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("VisualAlgoritmi Studio Project")
                    {
                        Patterns = ["*.vasproj"]
                    }
                ]
            });

        if (files.Count == 0)
        {
            return;
        }

        if (DataContext is ViewModels.HomeViewModel vm)
        {
            await vm.LoadProjectFromConfig(files[0].Path.LocalPath);
        }
    }

    private async void BrowseAnimation_Click(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
        {
            return;
        }

        IStorageProvider storage = topLevel.StorageProvider;

        if (!storage.CanOpen)
        {
            return;
        }

        IReadOnlyList<IStorageFile> files =
            await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open VisualAlgoritmi Animation",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("VisualAlgoritmi Animation")
                    {
                        Patterns = ["*.vaanim"]
                    }
                ]
            });

        if (files.Count == 0)
        {
            return;
        }

        if (DataContext is ViewModels.HomeViewModel vm)
        {
            await vm.LoadAnimationFromFile(files[0].Path.LocalPath);
        }
    }
}