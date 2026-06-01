using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;

namespace VisualAlgoritmi_Studio.Views.Dialogs;

public partial class MessageBoxDialog : Window
{
    private static readonly System.Collections.Generic.Dictionary<MessageBoxIcon, string> IconPaths = new()
    {
        [MessageBoxIcon.Error] = "/Assets/Icons/critical-error.svg",
        [MessageBoxIcon.Warning] = "/Assets/Icons/warning.svg",
    };

    public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

    public MessageBoxDialog()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Configure(string title, string message, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
    {
        this.FindControl<TextBlock>("TitleText")!.Text = title;
        this.FindControl<TextBlock>("MessageText")!.Text = message;

        if (icon != MessageBoxIcon.None && IconPaths.TryGetValue(icon, out var iconPath))
        {
            var iconSvg = this.FindControl<Avalonia.Svg.Skia.Svg>("IconSvg")!;
            iconSvg.Path = iconPath;
            iconSvg.IsVisible = true;
        }

        var panel = this.FindControl<StackPanel>("ButtonPanel")!;

        switch (buttons)
        {
            case MessageBoxButtons.Ok:
                panel.Children.Add(CreateButton("ОК", MessageBoxResult.Ok, isPrimary: true));
                break;
            case MessageBoxButtons.OkCancel:
                panel.Children.Add(CreateButton("Отказ", MessageBoxResult.Cancel));
                panel.Children.Add(CreateButton("ОК", MessageBoxResult.Ok, isPrimary: true));
                break;
            case MessageBoxButtons.YesNo:
                panel.Children.Add(CreateButton("Не", MessageBoxResult.No));
                panel.Children.Add(CreateButton("Да", MessageBoxResult.Yes, isPrimary: true));
                break;
            case MessageBoxButtons.YesCancel:
                panel.Children.Add(CreateButton("Отказ", MessageBoxResult.Cancel));
                panel.Children.Add(CreateButton("Да", MessageBoxResult.Yes, isPrimary: true));
                break;
            case MessageBoxButtons.OkCopy:
                panel.Children.Add(CreateCopyButton(title, message));
                panel.Children.Add(CreateButton("ОК", MessageBoxResult.Ok, isPrimary: true));
                break;
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) 
        {
            BeginMoveDrag(e);
        }
    }

    private Button CreateCopyButton(string title, string message)
    {
        var btn = new Button
        {
            Content = "Копирай съобщението",
            MinWidth = 80,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Avalonia.Thickness(12, 6),
            FontSize = 13,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(3),
        };

        btn.Classes.Add("msgbox-secondary");

        btn.Click += async (_, _) =>
        {
            var topLevel = TopLevel.GetTopLevel(this);

            if (topLevel?.Clipboard != null)
            {
                await topLevel.Clipboard.SetTextAsync($"{title}\n\n{message}");
            }
        };

        return btn;
    }

    private Button CreateButton(string text, MessageBoxResult result, bool isPrimary = false)
    {
        var btn = new Button
        {
            Content = text,
            MinWidth = 80,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Avalonia.Thickness(12, 6),
            FontSize = 13,
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(3),
        };

        btn.Classes.Add(isPrimary ? "msgbox-primary" : "msgbox-secondary");

        btn.Click += (_, _) =>
        {
            Result = result;
            Close();
        };

        return btn;
    }
}
