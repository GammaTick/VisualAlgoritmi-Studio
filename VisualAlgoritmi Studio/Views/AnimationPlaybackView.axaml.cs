using Avalonia.Controls;
using System;
using VisualAlgoritmi_Studio.ViewModels;

namespace VisualAlgoritmi_Studio.Views;

public partial class AnimationPlaybackView : UserControl
{
    private bool _didInitialCanvasReset;

    public AnimationPlaybackView()
    {
        InitializeComponent();

        LayoutUpdated += OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_didInitialCanvasReset)
        {
            return;
        }

        if (DataContext is not AnimationPlaybackViewModel vm)
        {
            return;
        }

        if (vm.VisualizerCanvas.Bounds.Width <= 0 ||
            vm.VisualizerCanvas.Bounds.Height <= 0)
        {
            return;
        }

        _didInitialCanvasReset = true;
        LayoutUpdated -= OnLayoutUpdated;

        vm.ResetCanvasView();
    }
}