using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace VisualAlgoritmi_Studio.Views;

public partial class NewProjectView : UserControl
{
    public NewProjectView()
    {
        InitializeComponent();
        Resources["BoolToColorConverter"] = new BoolToColorConverter();
    }

    private async void BrowseProjectLocation_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is null)
        {
            return;
        }

        IStorageProvider storage = topLevel.StorageProvider;

        if (!storage.CanPickFolder)
        {
            return;
        }

        IReadOnlyList<IStorageFolder> folders =
            await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select project folder",
                AllowMultiple = false
            });

        if (folders.Count == 0)
        {
            return;
        }

        if (DataContext is ViewModels.NewProjectViewModel vm)
        {
            vm.ProjectParentDirectory = folders[0].Path.LocalPath;
        }
    }
}

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return Color.Parse("#00FFFF");
        }

        return Color.Parse("#404040"); 
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}