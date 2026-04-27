using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VisualAlgoritmi_Studio.Controls.Canvas.Registry;
using VisualAlgoritmi_Studio.DotNetInternals;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Core
{
    public abstract class VisualizerCanvasBase : Control, IDisposable
    {
        private const double PanStep = 20;
        private const double ZoomFactor = 1.1;
        private const double MinZoom = 0.05;
        private const double MaxZoom = 7.0;

        protected Typeface _typeface;
        protected SolidColorBrush _foregroundBrush;
        protected IReadOnlyList<CanvasOpLogger>? _loggers;
        protected int _currentStep = -1;

        private double _offsetX;
        private double _offsetY;
        private double _zoom = 1.0;
        private bool _isPanning;
        private Point _lastPanPoint;
        private bool _disposed;
        private Matrix _viewMatrix;
        private Matrix _zoomMatrix;
        private Matrix _translationMatrix;

        public event EventHandler? ViewChanged;

        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<VisualizerCanvasBase, IBrush>(
                nameof(Background),
                new SolidColorBrush(new Color(255, 255, 255, 255))
            );

        public static readonly StyledProperty<Color> ForegroundProperty =
            AvaloniaProperty.Register<VisualizerCanvasBase, Color>(
                nameof(Foreground),
                Colors.Black
            );

        public static readonly StyledProperty<double> DefaultFontSizeProperty =
            AvaloniaProperty.Register<VisualizerCanvasBase, double>(nameof(DefaultFontSize), 20);

        public static readonly StyledProperty<double> MinimumFontSizeProperty =
            AvaloniaProperty.Register<VisualizerCanvasBase, double>(
                nameof(MinimumFontSize),
                10
            );

        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.Register<VisualizerCanvasBase, FontFamily>(nameof(FontFamily), new FontFamily("Cascadia Mono"));

        static VisualizerCanvasBase()
        {
            AffectsRender<VisualizerCanvasBase>(
                BackgroundProperty,
                FontFamilyProperty,
                DefaultFontSizeProperty,
                MinimumFontSizeProperty,
                ForegroundProperty
            );
        }

        public IBrush Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double DefaultFontSize
        {
            get => GetValue(DefaultFontSizeProperty);
            set => SetValue(DefaultFontSizeProperty, value);
        }

        public double MinimumFontSize
        {
            get => GetValue(MinimumFontSizeProperty);
            set => SetValue(MinimumFontSizeProperty, value);
        }

        public Color Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public double ZoomPercentage => _zoom * 100.0;

        public (int X, int Y) GetOffsetFromCenter()
        {
            return ((int)Math.Round(_offsetX), (int)Math.Round(_offsetY));
        }

        public int CurrentStep => _currentStep;

        public bool HasExecuted => _loggers != null;

        public int StepCount
        {
            get
            {
                if (_loggers == null || _loggers.Count == 0)
                {
                    return 0;
                }

                return _loggers[0].StepsCount;
            }
        }

        public VisualizerCanvasBase()
        {
            _typeface = new Typeface(FontFamily, FontStyle.Normal, FontWeight.Normal);
            _foregroundBrush = new SolidColorBrush(Foreground);

            ResetViewToDefault();

            Focusable = true;
            Cursor = new Cursor(StandardCursorType.Arrow);
        }

        public virtual void OnExecutionEnded()
        {
            _loggers = VisualDataStructuresRegister.GetLoggers(this);
            _currentStep = -1;
            ResetView();
        }

        public void LoadLoggers(IReadOnlyList<CanvasOpLogger> loggers)
        {
            _loggers = loggers;
            _currentStep = -1;
            ResetView();
        }

        public void StepForward()
        {
            if (_loggers == null || _loggers.Count == 0)
            {
                return;
            }

            if (_currentStep == _loggers[0].StepsCount)
            {
                return;
            }

            SetStep(_currentStep + 1);
        }

        public void StepBack()
        {
            if (_loggers == null || _loggers.Count == 0)
            {
                return;
            }

            if (_currentStep == 0)
            {
                return;
            }

            SetStep(_currentStep - 1);
        }

        public string GetOperationsAtCurrentStep()
        {
            if (_loggers == null || _loggers.Count == 0)
            {
                return string.Empty;
            }

            if (_currentStep < 0 || _currentStep >= _loggers[0].StepsCount)
            {
                return string.Empty;
            }

            var logger = _loggers[0];
            var step = logger.Steps[_currentStep];

            return string.Join(Environment.NewLine, step.Operations.Select(op => op.Description));
        }

        public int GetOperationCountAtCurrentStep()
        {
            if (_loggers == null || _loggers.Count == 0)
            {
                return 0;
            }

            if (_currentStep < 0 || _currentStep >= _loggers[0].StepsCount)
            {
                return 0;
            }

            return _loggers[0].Steps[_currentStep].Operations.Count;
        }

        public void ResetSteps()
        {
            if (_loggers == null || _loggers.Count == 0)
            {
                return;
            }

            SetStep(0);
        }

        private void SetStep(int step)
        {
            if (_loggers == null || _loggers.Count == 0)
            {
                return;
            }

            if (step < 0 || step >= _loggers[0].StepsCount)
            {
                return;
            }

            if (step < _currentStep)
            {
                StepBack(step);
            }
            else if (step > _currentStep)
            {
                StepForward(step);
            }

            _currentStep = step;
            InvalidateVisual();
        }

        protected abstract void StepForward(int targetStep);
        protected abstract void StepBack(int targetStep);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    {
                        _offsetX -= PanStep;
                        break;
                    }

                case Key.Right:
                    {
                        _offsetX += PanStep;
                        break;
                    }

                case Key.Up:
                    {
                        _offsetY -= PanStep;
                        break;
                    }

                case Key.Down:
                    {
                        _offsetY += PanStep;
                        break;
                    }
            }

            UpdateTranslationMatrix();

            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isPanning = true;
                _lastPanPoint = e.GetPosition(this);

                Cursor = new Cursor(StandardCursorType.SizeAll);

                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (!_isPanning)
            {
                return;
            }

            Point current = e.GetPosition(this);

            double dx = current.X - _lastPanPoint.X;
            double dy = current.Y - _lastPanPoint.Y;

            _offsetX -= dx;
            _offsetY -= dy;

            UpdateTranslationMatrix();

            _lastPanPoint = current;

            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            _isPanning = false;

            Focus();

            Cursor = new Cursor(StandardCursorType.Arrow);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                Point mousePos = e.GetPosition(this);
                double oldZoom = _zoom;

                if (e.Delta.Y > 0)
                {
                    _zoom *= ZoomFactor;
                }
                else
                {
                    _zoom /= ZoomFactor;
                }

                _zoom = Math.Clamp(_zoom, MinZoom, MaxZoom);

                // Adjust offset so the world point under the mouse stays fixed.
                // View transform: screen = world * zoom - offset  =>  world = (screen + offset) / zoom
                _offsetX = (mousePos.X + _offsetX) / oldZoom * _zoom - mousePos.X;
                _offsetY = (mousePos.Y + _offsetY) / oldZoom * _zoom - mousePos.Y;

                UpdateZoomMatrix();
                UpdateTranslationMatrix();

                InvalidateVisual();

                e.Handled = true;
                return;
            }

            base.OnPointerWheelChanged(e);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == FontFamilyProperty)
            {
                _typeface = new Typeface(FontFamily);
            }
            else if (change.Property == ForegroundProperty)
            {
                _foregroundBrush = new SolidColorBrush(Foreground);
            }
        }

        protected Rect ViewportBounds
        {
            get
            {
                double w = Bounds.Width / _zoom;
                double h = Bounds.Height / _zoom;
                double x = _offsetX / _zoom;
                double y = _offsetY / _zoom;
                return new Rect(x, y, w, h);
            }
        }

        public virtual void ResetView()
        {
            ResetViewToDefault();
        }

        private void ResetViewToDefault()
        {
            SetView(0, 0, 1.0);
        }

        protected void SetView(double offsetX, double offsetY, double zoom)
        {
            _offsetX = offsetX;
            _offsetY = offsetY;
            _zoom = zoom;

            _zoomMatrix = Matrix.CreateScale(_zoom, _zoom);
            _translationMatrix = Matrix.CreateTranslation(-_offsetX, -_offsetY);
            _viewMatrix = _zoomMatrix * _translationMatrix;

            ViewChanged?.Invoke(this, EventArgs.Empty);
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.FillRectangle(Background, new Rect(Bounds.Size));

            using (context.PushTransform(_viewMatrix))
            {
                RenderCore(context);
            }
        }

        public async Task TakeScreenshotAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            RenderTargetBitmap? bitmap = null;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PixelSize pixelSize = GetScreenshotPixelSize();

                bitmap = new RenderTargetBitmap(pixelSize, new Vector(256, 256));
                bitmap.Render(this);
            });

            if (bitmap is null)
            {
                return;
            }

            await Task.Run(() =>
            {
                using (bitmap)
                {
                    bitmap.Save(filePath);
                }
            });
        }

        private PixelSize GetScreenshotPixelSize()
        {
            int width = Math.Max(1, (int)Math.Ceiling(Bounds.Width));
            int height = Math.Max(1, (int)Math.Ceiling(Bounds.Height));

            return new PixelSize(width, height);
        }

        public abstract void RenderCore(DrawingContext context);

        public string SerializeAnimation()
        {
            if (_loggers == null)
            {
                return string.Empty;
            }

            CanvasOpLogger? logger = _loggers[0];

            if (logger == null)
            {
                return string.Empty;
            }

            return CanvasOpLoggerIO.Serialize(logger);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateZoomMatrix()
        {
            _zoomMatrix = Matrix.CreateScale(_zoom, _zoom);
            _viewMatrix = _zoomMatrix * _translationMatrix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateTranslationMatrix()
        {
            _translationMatrix = Matrix.CreateTranslation(-_offsetX, -_offsetY);
            _viewMatrix = _zoomMatrix * _translationMatrix;
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TextLayout BuildLayout(
           string text,
           double fontSize,
           TextTrimming trimming,
           double maxW,
           double maxH,
           TextWrapping textWrapping = TextWrapping.Wrap,
           int maxLines = 100)
        {
            return new TextLayout(
                text,
                _typeface,
                fontSize,
                _foregroundBrush,
                TextAlignment.Center,
                textWrapping,
                trimming,
                textDecorations: null,
                FlowDirection.LeftToRight,
                maxWidth: maxW,
                maxHeight: maxH,
                lineHeight: fontSize + 2,
                letterSpacing: 0,
                maxLines: maxLines,
                textStyleOverrides: null
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool Fits(TextLayout layout, double maxWidth, double maxHeight)
        {
            return layout.Width <= maxWidth + 0.01
                && layout.Height <= maxHeight + 0.01;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            DisposeCore();

            GC.SuppressFinalize(this);
        }

        protected abstract void DisposeCore();

        protected class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowCanvasLoggerNullException()
            {
                throw new ArgumentNullException("_canvasOpLogger");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowCountArgumentOutOfException()
            {
                throw new ArgumentOutOfRangeException("count");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal static void ThrowIndexArgumentOutOfException()
            {
                throw new ArgumentOutOfRangeException("index");
            }
        }
    }

    public abstract class VisualizerCanvasBase<T> : VisualizerCanvasBase where T : VisualNode
    {
        protected List<T> _visibleElements = [];
        protected VisualStateCacher<T> _visualStateCacher = new();

        protected sealed override void StepForward(int targetStep)
        {
            if (_visualStateCacher.TryGetVisualState(targetStep, out var cachedState))
            {
                _visibleElements = cachedState;
                return;
            }

            if (_loggers == null || _loggers.Count == 0)
            {
                return;
            }

            var logger = _loggers[0];

            if (_currentStep == logger.StepsCount)
            {
                return;
            }

            (int stepIndex, List<T>? visualState) = _visualStateCacher.GetLastCachedVisualState();

            if (stepIndex == -1)
            {
                visualState = [];
            }

            var visualStateCopy = DeepCopyVisualState(visualState!);

            _currentStep++;

            StepForwardCore(visualStateCopy, logger.Steps[_currentStep].Operations);

            _visibleElements = visualStateCopy;

            _visualStateCacher.CacheVisualState(targetStep, visualStateCopy);
        }

        protected sealed override void StepBack(int targetStep)
        {
            if (_visualStateCacher.TryGetVisualState(targetStep, out var cachedState))
            {
                _visibleElements = cachedState;
                return;
            }

            if (_loggers == null || _loggers.Count == 0)
            {
                return;
            }

            var logger = _loggers[0];

            if (_currentStep == 0)
            {
                return;
            }

            (int stepIndex, List<T>? visualState) = _visualStateCacher.GetLastCachedVisualState();

            if (stepIndex == -1)
            {
                visualState = [];
            }

            var visualStateCopy = DeepCopyVisualState(visualState!);

            _currentStep--;

            StepBackCore(visualStateCopy, logger.Steps[_currentStep].Operations);

            _visibleElements = visualStateCopy;

            _visualStateCacher.CacheVisualState(targetStep, visualStateCopy);
        }

        private static List<T> DeepCopyVisualState(List<T> visualState)
        {
            List<T> copy = new List<T>(visualState.Count);

            for (int i = 0; i < visualState.Count; i++)
            {
                copy.Add((T)visualState[i].Clone());
            }

            return copy;
        }

        public override void OnExecutionEnded()
        {
            base.OnExecutionEnded();

            _visibleElements.Clear();
            _visualStateCacher.Clear();
        }
        
        protected override void DisposeCore()
        {
            foreach (var item in _visibleElements)
            {
                item.Dispose();
            }

            _visibleElements.Clear();
            _visualStateCacher.Clear();
        }

        protected abstract void StepForwardCore(List<T> previousVisualState, IReadOnlyList<ICanvasOp> canvasOps);
        protected abstract void StepBackCore(List<T> previousVisualState, IReadOnlyList<ICanvasOp> canvasOps);
    }
}
