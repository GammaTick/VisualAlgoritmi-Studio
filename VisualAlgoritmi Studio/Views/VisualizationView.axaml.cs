using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using VisualAlgoritmi_Studio.Models;
using VisualAlgoritmi_Studio.ViewModels;
using Avalonia.Input.Platform;
using VisualAlgoritmi_Studio.Controls.Editor;

namespace VisualAlgoritmi_Studio.Views;

public partial class VisualizationView : UserControl
{
    // Guard that prevents a scrollbar-value set from bouncing back into ScrollTo
    private bool _updatingScrollBars;

    public VisualizationView()
    {
        InitializeComponent();

        SetCodeEditorFontFromSettings();

        DataContextChanged += (_, _) =>
        {
            if (DataContext is VisualizationViewModel vm)
            {
                _ = vm.AttachCodeEditor(CodeEditorControl);
                vm.AttachConsoleControl(ConsoleOutputControl);
            }
        };

        CodeEditorControl.FontSizeChanged += CodeEditorControl_FontSizeChanged;
        CodeEditorControl.ScrollMetricsChanged += OnEditorScrollMetricsChanged;
        EditorVerticalScrollBar.Scroll += OnVerticalScrollBarChanged;
        EditorHorizontalScrollBar.Scroll += OnHorizontalScrollBarChanged;
    }

    private void SetCodeEditorFontFromSettings()
    {
        double font = App.Settings.CodeEditorFontSize;

        font = Math.Clamp(font, CodeEditor.MinEditorFontSize, CodeEditor.MaxEditorFontSize);

        CodeEditorControl.FontSize = font;
    }

    private void CodeEditorControl_FontSizeChanged(object? sender, double e)
    {
        App.Settings.CodeEditorFontSize = e;
        App.Settings.Save();
    }

    private void OnEditorScrollMetricsChanged(object? sender, EventArgs e)
    {
        if (_updatingScrollBars)
        {
            return;
        }

        UpdateScrollBars();
    }

    private void UpdateScrollBars()
    {
        _updatingScrollBars = true;

        try
        {
            var editor = CodeEditorControl;

            double viewportH = editor.ViewportHeight;

            EditorVerticalScrollBar.Minimum = 0;
            EditorVerticalScrollBar.Maximum = editor.MaxScrollY;
            EditorVerticalScrollBar.ViewportSize = viewportH;
            EditorVerticalScrollBar.Value = editor.ScrollY;

            double viewportW = editor.ViewportWidth;

            EditorHorizontalScrollBar.Minimum = 0;
            EditorHorizontalScrollBar.Maximum = editor.MaxScrollX;
            EditorHorizontalScrollBar.ViewportSize = viewportW;
            EditorHorizontalScrollBar.Value = editor.ScrollX;
        }
        finally
        {
            _updatingScrollBars = false;
        }

        // If the Avalonia scrollbar coerced Value (e.g. content shrank and
        // the old offset is now out of range), sync the editor to the new,
        // clamped position.
        double coercedY = EditorVerticalScrollBar.Value;
        double coercedX = EditorHorizontalScrollBar.Value;

        bool yMismatch = Math.Abs(coercedY - CodeEditorControl.ScrollY) > 0.5;
        bool xMismatch = Math.Abs(coercedX - CodeEditorControl.ScrollX) > 0.5;

        if (yMismatch || xMismatch)
        {
            CodeEditorControl.ScrollTo(coercedX, coercedY);
        }
    }

    private void OnVerticalScrollBarChanged(object? sender, ScrollEventArgs e)
    {
        if (_updatingScrollBars)
        {
            return;
        }

        CodeEditorControl.ScrollTo(CodeEditorControl.ScrollX, e.NewValue);
    }

    private void OnHorizontalScrollBarChanged(object? sender, ScrollEventArgs e)
    {
        if (_updatingScrollBars)
        {
            return;
        }

        CodeEditorControl.ScrollTo(e.NewValue, CodeEditorControl.ScrollY);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        bool isCtrlOrCmd = OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()
            ? e.KeyModifiers.HasFlag(KeyModifiers.Meta)
            : e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (isCtrlOrCmd && e.Key == Key.S && DataContext is VisualizationViewModel vm)
        {
            if (vm.SaveCodeCommand.CanExecute(null))
            {
                vm.SaveCodeCommand.Execute(null);
            }
            e.Handled = true;
        }
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null && DataContext is VisualizationViewModel vm)
        {
            vm.AttachStorageProvider(topLevel.StorageProvider);
            if (topLevel.Clipboard != null)
            {
                vm.AttachClipboard(topLevel.Clipboard);
            }
        }
    }

    private async void OnCopyErrorMessageClicked(object? sender, RoutedEventArgs e)
    {
        EditorError? error = null;

        if (sender is MenuItem menuItem)
        {
            if (menuItem.DataContext is EditorError e1)
                error = e1;
            else if (menuItem.Parent is ContextMenu cm && cm.DataContext is EditorError e2)
                error = e2;
        }

        if (error != null)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel?.Clipboard != null)
            {
                string text = $"{error.Code}  {error.Message}  (Line {error.Line}, Column {error.Column})";
                await topLevel.Clipboard.SetTextAsync(text);
            }
        }
    }
}
