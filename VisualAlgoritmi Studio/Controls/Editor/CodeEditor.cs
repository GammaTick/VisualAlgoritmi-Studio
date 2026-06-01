using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.Input;
using VisualAlgoritmi_Studio.Controls.Editor.Internal;
using VisualAlgoritmi_Studio.Controls.Editor.Invalidation;
using VisualAlgoritmi_Studio.Controls.Editor.LayoutsManagement;
using VisualAlgoritmi_Studio.Controls.Editor.SyntaxHighlighting;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Viewport;
using VisualAlgoritmi_Studio.RoslynCore;

namespace VisualAlgoritmi_Studio.Controls.Editor;

public class CodeEditor : Control
{
    public const double MinEditorFontSize = 4;
    public const double DefaultEditorFontSize = 14;
    public const double MaxEditorFontSize = 30;
    private const double WheelScrollLinesPerDelta = 2.0;
    private readonly Brush ErrorUnderlineBrush = new SolidColorBrush(Color.FromRgb(255, 80, 70));

    public static readonly StyledProperty<IBrush> BackgroundProperty =
        AvaloniaProperty.Register<CodeEditor, IBrush>(nameof(Background), new SolidColorBrush(new Color(255, 31, 31, 31)));

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<CodeEditor, IBrush>(nameof(Foreground), Brushes.White);

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<CodeEditor, FontFamily>(nameof(FontFamily), new FontFamily("Cascadia Mono"));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<CodeEditor, double>(nameof(FontSize), DefaultEditorFontSize);

    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<CodeEditor, IBrush>(nameof(SelectionBrush), new SolidColorBrush(Color.Parse("#264F78")));

    public static readonly StyledProperty<bool> ShouldCaretBlinkProperty =
        AvaloniaProperty.Register<CodeEditor, bool>(nameof(ShouldCaretBlink), true);

    public static readonly StyledProperty<bool> DisplayLineNumbersProperty =
        AvaloniaProperty.Register<CodeEditor, bool>(nameof(DisplayLineNumbers), true);

    public static readonly StyledProperty<IBrush> LineNumbersForegroundProperty =
        AvaloniaProperty.Register<CodeEditor, IBrush>(nameof(LineNumbersForeground), new SolidColorBrush(new Color(255, 150, 150, 150)));

    static CodeEditor()
    {
        AffectsRender<CodeEditor>(
            ForegroundProperty,
            BackgroundProperty,
            FontFamilyProperty,
            FontSizeProperty,
            SelectionBrushProperty,
            ShouldCaretBlinkProperty,
            DisplayLineNumbersProperty,
            LineNumbersForegroundProperty
        );
    }

    public IBrush Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set
        {
            SetValue(FontSizeProperty, ClampFontSize(value));
            FontSizeChanged?.Invoke(this, value);
        }
    }

    public IBrush SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public bool ShouldCaretBlink
    {
        get => GetValue(ShouldCaretBlinkProperty);
        set => SetValue(ShouldCaretBlinkProperty, value);
    }

    public bool DisplayLineNumbers
    {
        get => GetValue(DisplayLineNumbersProperty);
        set => SetValue(DisplayLineNumbersProperty, value);
    }

    public IBrush LineNumbersForeground
    {
        get => GetValue(LineNumbersForegroundProperty);
        set => SetValue(LineNumbersForegroundProperty, value);
    }

    public double ScrollX => _viewportManager.ScrollX;
    public double ScrollY => _viewportManager.ScrollY;

    public double ViewportHeight => GetCodeAreaHeight();
    public double ViewportWidth => Math.Max(0, Bounds.Width - _lineNumbersLayoutManager.GetLayoutEndX() - ContentMargin);

    public double TotalContentHeight => _textBuffer.LineCount * GetLineHeight();
    public double MaxScrollY => Math.Max(0, TotalContentHeight - ViewportHeight);
    public double MaxScrollX => Math.Max(0, TotalContentWidth - ViewportWidth);

    public double TotalContentWidth
    {
        get
        {
            if (_cachedTextVersion != _textBuffer.Version)
            {
                _cachedTextVersion = _textBuffer.Version;
                _cachedMaxLineWidth = ComputeMaxLineWidth();
            }

            return _cachedMaxLineWidth + _lineNumbersLayoutManager.GetLayoutEndX() + ContentMargin * 2;
        }
    }

    public Typeface Typeface { get; private set; }
    public double ContentMargin { get; } = 4;

    private readonly RoslynHost _roslynHost;
    private readonly CodeAnalysisSession _codeAnalysisSession;
    private readonly TextBuffer _textBuffer;
    private readonly IGraphemeNavigator _graphemeNavigator;
    private readonly CodeLayoutManager _codeLayoutManager;
    private readonly CaretController _caretController;
    private readonly SelectionController _selectionController;
    private readonly UndoRedoManager _undoRedoManager;
    private readonly LineNumbersLayoutManager _lineNumbersLayoutManager;
    private readonly ViewportManager _viewportManager;
    private readonly EditorStateTracker _editorStateTracker;
    private readonly EditorRenderCache _editorRenderCache;
    private readonly TextMutationSyncPipeline _textMutationSyncPipeline;
    private readonly KeyboardInputHandler _keyboardInputHandler;
    private readonly SyntaxHighlighterController _syntaxHighlighterController;

    private TextLayout? _spaceLayout;

    private Pen _caretPen;
    private bool _isHoldingLeftMouseButton;

    private readonly CaretBlinkController _caretBlink;
    private int _diagnosticsUpdateVersion;
    
    public event Action<IReadOnlyList<Diagnostic>>? DiagnosticsUpdated;
    public event EventHandler? ScrollMetricsChanged;
    public event EventHandler? CodeContentChanged;

    public event EventHandler<double>? FontSizeChanged;

    public CodeAnalysisSession CodeAnalysisSession => _codeAnalysisSession;

    private IReadOnlyList<Diagnostic> _currentErrors = [];
    private readonly List<(int AfterLine, int Delta)> _pendingLineAdjustments = [];

    private double _cachedMaxLineWidth = 0;
    private int _cachedTextVersion = -1;

    public CodeEditor()
    {
        Typeface = new Typeface(FontFamily);
        _caretPen = new Pen(Foreground, 1);

        _roslynHost = new RoslynHost();
        _codeAnalysisSession = new CodeAnalysisSession(_roslynHost);
        _textBuffer = new TextBuffer();
        _undoRedoManager = new UndoRedoManager();
        _viewportManager = new ViewportManager(this, _textBuffer);        

        _syntaxHighlighterController = new SyntaxHighlighterController(this,
            _textBuffer,
            _codeAnalysisSession);

        _codeLayoutManager = new CodeLayoutManager(this, _textBuffer, _viewportManager, _syntaxHighlighterController);
        _lineNumbersLayoutManager = new LineNumbersLayoutManager(this, _viewportManager);
        _graphemeNavigator = new GraphemeNavigator(_textBuffer, _codeLayoutManager, _viewportManager);
        _caretController = new CaretController(_textBuffer, _graphemeNavigator);
        _selectionController = new SelectionController(_textBuffer);
        _editorStateTracker = new EditorStateTracker(_textBuffer, _caretController, _selectionController, _viewportManager);
        _editorRenderCache = new EditorRenderCache();

        _textMutationSyncPipeline = new TextMutationSyncPipeline(_textBuffer,
            _caretController,
            _selectionController,
            _codeLayoutManager,
            _viewportManager,
            _codeAnalysisSession,
            onLineCountChanged: (afterLine, delta) =>
            {
                _pendingLineAdjustments.Add((afterLine, delta));
                _syntaxHighlighterController.ShiftHighlightingCaches(afterLine, delta);
            });

        _keyboardInputHandler = new KeyboardInputHandler(_textMutationSyncPipeline,
            _caretController,
            _selectionController,
            _textBuffer,
            _viewportManager,
            _graphemeNavigator,
            _undoRedoManager,
            _codeAnalysisSession,
            () => TopLevel.GetTopLevel(this)?.Clipboard);

        _editorRenderCache.EndOfLineCellWidth = GetSelectionEolCellWidth();

        _caretBlink = new CaretBlinkController(
            TimeSpan.FromMilliseconds(500),
            () => ShouldCaretBlink
        );

        _caretBlink.BlinkStateChanged += InvalidateVisual;
        _codeAnalysisSession.DocumentUpdated += CodeEditor_DocumentUpdated;

        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Ibeam);
    }

    private async void CodeEditor_DocumentUpdated(object? sender, EventArgs e)
    {
        int diagnosticsUpdateVersion = Interlocked.Increment(ref _diagnosticsUpdateVersion);
        var document = _codeAnalysisSession!.GetDocument();

        if (document == null)
        {
            return;
        }

        var compilation = await document.Project.GetCompilationAsync();

        if (compilation == null)
        {
            return;
        }

        if (diagnosticsUpdateVersion != Volatile.Read(ref _diagnosticsUpdateVersion))
        {
            return;
        }

        // Semantic highlighting — full redraw on document update.
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();

        if (syntaxTree != null)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            _syntaxHighlighterController.SetSemanticModel(semanticModel);
            _codeLayoutManager.RebuildFullLayout();
        }

        _pendingLineAdjustments.Clear();
        _currentErrors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        InvalidateVisual();
        DiagnosticsUpdated?.Invoke(_currentErrors);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        InvalidateVisual();
        _lineNumbersLayoutManager.RebuildLayout();
        _codeLayoutManager.RebuildFullLayout();
        ScrollMetricsChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        bool shouldRerenderEditor = false;

        var changedProperty = change.Property;

        if (changedProperty == BackgroundProperty)
        {
            shouldRerenderEditor = true;
        }
        else if (changedProperty == ForegroundProperty)
        {
            _caretPen = new Pen(Foreground, 1);

            _codeLayoutManager.RebuildFullLayout();
            _lineNumbersLayoutManager.RebuildLayout();

            shouldRerenderEditor = true;
        }
        else if (changedProperty == FontFamilyProperty)
        {
            Typeface = new Typeface(FontFamily);

            _syntaxHighlighterController.InvalidateTextRunPropertiesCache();
            RebuildAllLayouts();
            ScrollMetricsChanged?.Invoke(this, EventArgs.Empty);

            shouldRerenderEditor = true;
        }
        else if (changedProperty == FontSizeProperty &&
            change.NewValue is double fontSize)
        {
            double clampedFontSize = ClampFontSize(fontSize);

            if (!fontSize.Equals(clampedFontSize))
            {
                if (!FontSize.Equals(clampedFontSize))
                {
                    FontSize = clampedFontSize;
                }

                return;
            }

            _viewportManager.InvalidateVisibleRange();
            _syntaxHighlighterController.InvalidateTextRunPropertiesCache();
            RebuildAllLayouts();
            ScrollMetricsChanged?.Invoke(this, EventArgs.Empty);

            shouldRerenderEditor = true;
        }
        else if (changedProperty == SelectionBrushProperty)
        {
            shouldRerenderEditor = true;
        }
        else if (changedProperty == ShouldCaretBlinkProperty)
        {
            if (ShouldCaretBlink)
            {
                _caretBlink.Start();
            }
            else
            {
                _caretBlink.Stop();
            }

            shouldRerenderEditor = true;
        }  
        else if (changedProperty == DisplayLineNumbersProperty)
        {
            shouldRerenderEditor = true;
        }
        else if (changedProperty == LineNumbersForegroundProperty)
        {
            _lineNumbersLayoutManager.RebuildLayout();
            shouldRerenderEditor = true;
        }
      
        if (shouldRerenderEditor)
        {
            InvalidateVisual();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RebuildAllLayouts()
    {
        _codeLayoutManager.RebuildFullLayout();
        _lineNumbersLayoutManager.RebuildLayout();
        _editorRenderCache.EndOfLineCellWidth = GetSelectionEolCellWidth();
        InvalidateContentWidthCache();
        RecalculateCaretPosOnScreen();
        RebuildSelectionGeometry();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InvalidateContentWidthCache()
    {
        _cachedTextVersion = -1;
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        string? text = e.Text;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _editorStateTracker.Snapshot();

        _keyboardInputHandler.HandleTextInput(text);

        _caretBlink.ResetBlink();

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();
        
        EnsureFreshLayout(editorDirtyFlags);
    }

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        _editorStateTracker.Snapshot();

        await _keyboardInputHandler.HandleKeyPress(e);

        _caretBlink.ResetBlink();

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        _viewportManager.InvalidateVisibleRange();

        int previousVisibleLineCount = _codeLayoutManager.LineLayouts.Count;

        _codeLayoutManager.SynchWithViewport();

        if (_codeLayoutManager.LineLayouts.Count != previousVisibleLineCount)
        {
            RecalculateCaretPosOnScreen();
            RebuildSelectionGeometry();
            _lineNumbersLayoutManager.RebuildLayout();
        }

        ScrollMetricsChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _caretBlink.Start();
        _caretBlink.ResetBlink();
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _caretBlink.Stop();
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
   
        if (!_viewportManager.AreThereVisibleLines())
        {
            return;
        }

        (int line, int column) = GetCaretLocationFromPointer(e);

        _editorStateTracker.Snapshot();

        var currentPoint = e.GetCurrentPoint(this);

        if (currentPoint.Properties.IsLeftButtonPressed && e.ClickCount >= 3)
        {
            SelectLine(line);
        }
        else if (currentPoint.Properties.IsLeftButtonPressed && e.ClickCount == 2)
        {
            _caretController.SetPosition(line, column);

            if (!SelectWordAt(line, column))
            {
                _selectionController.CollapseTo(line, column);
            }
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _caretController.SetPosition(line, column);
            _selectionController.ExtendTo(line, column);
        }
        else
        {
            _selectionController.CollapseTo(line, column);
            _caretController.SetPosition(line, column);
            _selectionController.BeginSelection(line, column);
        }

        _caretBlink.ResetBlink();

        e.Handled = true;

        _isHoldingLeftMouseButton = currentPoint.Properties.IsLeftButtonPressed && e.ClickCount < 2;

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isHoldingLeftMouseButton || !_viewportManager.AreThereVisibleLines())
        {
            return;
        }

        (int line, int column) = GetCaretLocationFromPointer(e);

        _editorStateTracker.Snapshot();

        _caretController.SetPosition(line, column);
        _selectionController.ExtendTo(line, column);
  
        _caretBlink.ResetBlink();

        e.Handled = true;

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        _caretBlink.ResetBlink();

        _isHoldingLeftMouseButton = false;
    }

    private double _wheelLineAccumulator;

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _editorStateTracker.Snapshot();

            if (e.Delta.Y != 0)
            {
                AdjustFontSize(Math.Sign(e.Delta.Y));
            }

            e.Handled = true;
            return;
        }

        double lineHeight = GetLineHeight();
        double targetScrollX = ScrollX;
        double targetScrollY = ScrollY;

        if (e.Delta.X != 0)
        {
            targetScrollX += -e.Delta.X * lineHeight * WheelScrollLinesPerDelta;
        }

        if (e.Delta.Y != 0)
        {
            _wheelLineAccumulator += -e.Delta.Y * WheelScrollLinesPerDelta;

            int lineDelta = (int)_wheelLineAccumulator;

            if (lineDelta != 0)
            {
                _wheelLineAccumulator -= lineDelta;
                targetScrollY += lineDelta * lineHeight;
            }
        }

        targetScrollX = Math.Clamp(targetScrollX, 0, MaxScrollX);
        targetScrollY = Math.Clamp(targetScrollY, 0, MaxScrollY);

        if (targetScrollX.Equals(ScrollX) && targetScrollY.Equals(ScrollY))
        {
            return;
        }

        e.Handled = true;
        ScrollTo(targetScrollX, targetScrollY);
    }

    private (int Line, int Column) GetCaretLocationFromPointer(PointerEventArgs e)
    {
        if (!_viewportManager.AreThereVisibleLines())
        {
            return (0, 0);
        }

        Point pointerPosition = e.GetPosition(this);

        double caretLayoutXPos = pointerPosition.X - _lineNumbersLayoutManager.GetLayoutEndX() + _viewportManager.ScrollX;
        double caretLayoutYPos = pointerPosition.Y - ContentMargin + _viewportManager.VerticalOffsetWithinFirstLine;

        if (caretLayoutXPos < 0)
        {
            caretLayoutXPos = 0;
        }

        if (caretLayoutYPos < 0)
        {
            caretLayoutYPos = 0;
        }

        double accumulatedLineHeight = 0;
        int caretLineIndex = 0;
        var codeLineLayouts = _codeLayoutManager.LineLayouts;

        // Determine which text line the caret is on by walking line heights.
        // We accumulate line heights until the caret's Y position falls
        // within the vertical span of the current line.
        foreach (var lineLayout in codeLineLayouts)
        {
            var line = lineLayout.TextLayout;

            if (caretLayoutYPos < accumulatedLineHeight + line.Height)
            {
                break;
            }

            caretLineIndex++;
            accumulatedLineHeight += line.Height;
        }

        // If the caret Y position is below the last line,
        // clamp the caret to the final line and rewind the accumulated height
        // to the top of that line.
        if (caretLineIndex >= codeLineLayouts.Count)
        {
            caretLineIndex = codeLineLayouts.Count - 1;
            accumulatedLineHeight -= codeLineLayouts[caretLineIndex].TextLayout.Height;
        }

        // Convert caret position into line-local coordinates and hit-test
        // the text layout to resolve the exact document line and column.
        double localY = caretLayoutYPos - accumulatedLineHeight;
        TextHitTestResult hitResult = codeLineLayouts[caretLineIndex].TextLayout.HitTestPoint(new Point(caretLayoutXPos, localY));

        int documentLine = codeLineLayouts[caretLineIndex].DocumentLine;
        int documentColumn = hitResult.TextPosition;

        return (documentLine, documentColumn);
    }

    private bool SelectWordAt(int line, int column)
    {
        string lineText = _textBuffer.GetLine(line).ToString();

        if (lineText.Length == 0)
        {
            return false;
        }

        int probeIndex = Math.Clamp(column, 0, lineText.Length);

        if (probeIndex == lineText.Length)
        {
            probeIndex--;
        }
        else if (!IsWordChar(lineText[probeIndex]) && probeIndex > 0 && IsWordChar(lineText[probeIndex - 1]))
        {
            probeIndex--;
        }

        if (!IsWordChar(lineText[probeIndex]))
        {
            return false;
        }

        int startColumn = probeIndex;
        int endColumn = probeIndex + 1;

        while (startColumn > 0 && IsWordChar(lineText[startColumn - 1]))
        {
            startColumn--;
        }

        while (endColumn < lineText.Length && IsWordChar(lineText[endColumn]))
        {
            endColumn++;
        }

        _selectionController.CollapseTo(line, startColumn);
        _selectionController.ExtendTo(line, endColumn);
        _caretController.SetPosition(line, endColumn);

        return true;
    }

    private void SelectLine(int line)
    {
        int lineLength = _textBuffer.GetLineLength(line);

        _selectionController.CollapseTo(line, 0);
        _selectionController.ExtendTo(line, lineLength);
        _caretController.SetPosition(line, lineLength);
    }

    private static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private void EnsureFreshLayout(EditorDirtyFlags editorDirtyFlags)
    {
        bool rerenderEditor = false;
        bool invalidateVisibleRange = false;
        bool rebuildCodeLayout = false;
        bool rebuildLineNumsLayout = false;
        bool recalcSelectionGeometry = false;
        bool recalculateCaretOnScreen = false;

        if ((editorDirtyFlags & EditorDirtyFlags.TextBuffer) != 0)
        {
            rerenderEditor = true;
            CodeContentChanged?.Invoke(this, EventArgs.Empty);
        }

        if ((editorDirtyFlags & EditorDirtyFlags.Selection) != 0)
        {
            recalcSelectionGeometry = true;
            rerenderEditor = true;
        }

        if ((editorDirtyFlags & EditorDirtyFlags.Caret) != 0)
        {
            recalculateCaretOnScreen = true;
            rerenderEditor = true;
        }

        if ((editorDirtyFlags & EditorDirtyFlags.LineCount) != 0)
        {
            rebuildLineNumsLayout = true;
            invalidateVisibleRange = true;
            rerenderEditor = true;
        }

        if ((editorDirtyFlags & EditorDirtyFlags.Viewport) != 0)
        {
            invalidateVisibleRange = true;
            rebuildCodeLayout = true;
            rebuildLineNumsLayout = true;
            recalculateCaretOnScreen = true;
            recalcSelectionGeometry = true;
            rerenderEditor = true;
        }

        if (invalidateVisibleRange)
        {
            _viewportManager.InvalidateVisibleRange();
        }

        if (rebuildCodeLayout)
        {
            _codeLayoutManager.RebuildFullLayout();
        }

        if (rebuildLineNumsLayout)
        {
            _lineNumbersLayoutManager.RebuildLayout();
        }

        if (recalculateCaretOnScreen)
        {
            RecalculateCaretPosOnScreen();
        }

        if (recalcSelectionGeometry)
        {
            RebuildSelectionGeometry();
        }

        _editorStateTracker.Snapshot();

        if (rerenderEditor)
        {
            InvalidateVisual();
            ScrollMetricsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        context.FillRectangle(Background, new Rect(Bounds.Size));

        if (DisplayLineNumbers)
        {
            _lineNumbersLayoutManager.Draw(context);
        }

        double xPosOfLine = _lineNumbersLayoutManager.GetLayoutEndX();
        double verticalScrollOffset = _viewportManager.VerticalOffsetWithinFirstLine;
        double yPosOfLine = ContentMargin - verticalScrollOffset;
        double codeAreaWidth = Math.Max(0, Bounds.Width - xPosOfLine - ContentMargin);
        double codeAreaHeight = Math.Max(0, GetCodeAreaHeight());
        var codeAreaClip = new Rect(xPosOfLine, ContentMargin, codeAreaWidth, codeAreaHeight);

        using (context.PushClip(codeAreaClip))
        using (context.PushTransform(Matrix.CreateTranslation(-_viewportManager.ScrollX, 0)))
        {
            if (_selectionController.HasSelection)
            {
                DrawSelection(context);
            }

            foreach (var line in _codeLayoutManager.LineLayouts)
            {
                line.TextLayout.Draw(context, new Point(xPosOfLine, yPosOfLine));
                yPosOfLine += line.TextLayout.Height;
            }

            if (_editorRenderCache.IsCaretInVisibleArea && _caretBlink.CaretVisible)
            {
                DrawCaret(context);
            }

            DrawErrors(context);
        }
    }

    private void DrawErrors(DrawingContext context)
    {
        if (_currentErrors.Count == 0 || !_viewportManager.AreThereVisibleLines()) 
        {
            return;
        }

        double xOffset = _lineNumbersLayoutManager.GetLayoutEndX();
        var codeLineLayouts = _codeLayoutManager.LineLayouts;

        double circleDiameter = FontSize / DefaultEditorFontSize * 2;
        double circleRadius = circleDiameter / 2;
        double circleSpacing = circleDiameter * 1.5;

        foreach (var error in _currentErrors)
        {
            var lineSpan = error.Location.GetLineSpan();
            int errorLine = lineSpan.StartLinePosition.Line;

            foreach (var (afterLine, delta) in _pendingLineAdjustments)
            {
                if (errorLine > afterLine)
                    errorLine += delta;
            }

            if (!_viewportManager.IsDocumentLineVisible(errorLine)) 
            {
                continue;
            }

            int localLine = ConvertGlobalDocLineToLocal(errorLine); 

            if ((uint)localLine >= (uint)codeLineLayouts.Count) 
            {
                continue;
            }

            var layout = codeLineLayouts[localLine].TextLayout;
            int lineLength = layout.TextLines[0].Length;
            int startColumn = Math.Min(lineSpan.StartLinePosition.Character, lineLength);
            int endColumn = Math.Min(lineSpan.EndLinePosition.Character, lineLength);

            if (endColumn < startColumn) 
            {
                endColumn = startColumn;
            }

            double yOffset = ContentMargin - _viewportManager.VerticalOffsetWithinFirstLine + localLine * layout.Height;
            double startX = layout.HitTestTextPosition(startColumn).X + xOffset;
            double endX = layout.HitTestTextPosition(endColumn).X + xOffset;
            double baselineY = yOffset + layout.Height - circleRadius;

            double x = startX + circleRadius;

            do
            {
                context.DrawEllipse(
                    ErrorUnderlineBrush,
                    null,
                    new Point(x, baselineY),
                    circleRadius,
                    circleRadius
                );

                x += circleSpacing;
            } while (x - circleRadius < endX);
        }
    }

    private void DrawSelection(DrawingContext context)
    {
        var selectionRects = _editorRenderCache.CachedSelectionRects;

        if (selectionRects.Count == 0)
        {
            return;
        }

        var bounds = Bounds;
        double lineHeight = Math.Ceiling(GetLineHeight());
        double xOffset = _lineNumbersLayoutManager.GetLayoutEndX();

        foreach (var rect in selectionRects)
        {
            double x = rect.X + xOffset;
            double y = rect.Y + ContentMargin - _viewportManager.VerticalOffsetWithinFirstLine;

            if (y > bounds.Height)
            {
                continue;
            }

            double availableWidth = bounds.Width - x;

            if (availableWidth <= 0)
            {
                continue;
            }

            double width = Math.Min(rect.Width, availableWidth);

            context.FillRectangle(
                SelectionBrush,
                new Rect(x, y, width, lineHeight)
            );
        }
    }

    private void DrawCaret(DrawingContext context)
    {
        if (!_editorRenderCache.IsCaretInVisibleArea)
        {
            return;
        }

        // These coordinates are already in local text layout coordinates space
        var (X, Y) = _editorRenderCache.CaretPosInCodeLayout;

        X += _lineNumbersLayoutManager.GetLayoutEndX();
        Y += ContentMargin - _viewportManager.VerticalOffsetWithinFirstLine;

        context.DrawLine(
            _caretPen,
            new Point(X, Y),
            new Point(X, Y + GetLineHeight())
        );
    }

    private void RebuildSelectionGeometry()
    {
        if (!_selectionController.HasSelection || !_viewportManager.AreThereVisibleLines())
        {
            _editorRenderCache.CachedSelectionRects.Clear();
            return;
        } 

        (int firstVisibleLineIndex, int lastVisibleLineIndex) = _viewportManager.GetVisibleVerticalRange();
        (int startLine, int startColumn, int endLine, int endColumn) = _selectionController.GetNormalizedSelection();

        int visibleStartLine = Math.Max(startLine, firstVisibleLineIndex);
        int visibleEndLine = Math.Min(endLine, lastVisibleLineIndex);

        if (visibleStartLine > visibleEndLine)
        {
            _editorRenderCache.CachedSelectionRects.Clear();
            return;
        }

        int localStartLine = visibleStartLine - firstVisibleLineIndex;
        int localEndLine = visibleEndLine - firstVisibleLineIndex;

        var codeLineLayouts = _codeLayoutManager.LineLayouts;

        if ((uint)localStartLine >= (uint)codeLineLayouts.Count)
        {
            _editorRenderCache.CachedSelectionRects.Clear();
            return;
        }

        if ((uint)localEndLine >= (uint)codeLineLayouts.Count)
        {
            localEndLine = codeLineLayouts.Count - 1;
        }

        double lineStartYCoord = GetLineHeight() * localStartLine;
        List<Rect> selectionRects = [];
        int caretLine = _caretController.Line;
        int caretColumn = _caretController.Column;

        for (int localLine = localStartLine; localLine <= localEndLine; localLine++)
        {
            int documentLine = firstVisibleLineIndex + localLine;

            var currentLayout = codeLineLayouts[localLine].TextLayout;
            var currentTextLine = currentLayout.TextLines[0];

            int lineStartColumn = documentLine == startLine
                ? startColumn
                : 0;

            int lineLength = currentTextLine.Length;

            if (lineStartColumn > lineLength)
            {
                lineStartColumn = lineLength;
            }

            double lineStartXCoord = currentLayout.HitTestTextPosition(lineStartColumn).X;

            int lineEndColumn = documentLine == endLine
                ? endColumn
                : lineLength;

            if (lineEndColumn > lineLength)
            {
                lineEndColumn = lineLength;
            }

            if (lineEndColumn < lineStartColumn)
            {
                lineEndColumn = lineStartColumn;
            }

            double lineEndXCoord = currentLayout.HitTestTextPosition(lineEndColumn).X;

            bool shouldIncludeLineBreakSlot =
                startLine != endLine &&
                documentLine < endLine;

            double width = lineEndXCoord - lineStartXCoord;

            if (shouldIncludeLineBreakSlot)
            {
                width += _editorRenderCache.EndOfLineCellWidth;
            }

            selectionRects.Add(new Rect(lineStartXCoord, lineStartYCoord, width, currentLayout.Height));
            lineStartYCoord += currentLayout.Height;
        }

        _editorRenderCache.CachedSelectionRects = selectionRects;
    }

    private void RecalculateCaretPosOnScreen()
    {
        int caretLinePos = _caretController.Line;

        if (!_viewportManager.IsDocumentLineVisible(caretLinePos))
        {
            _editorRenderCache.CaretPosInCodeLayout = (0, 0);
            _editorRenderCache.IsCaretInVisibleArea = false;
            return;
        }

        // Convert from global to local text layout 
        int localLine = ConvertGlobalDocLineToLocal(caretLinePos);

        var codeLineLayouts = _codeLayoutManager.LineLayouts;
        var hitTestRect = codeLineLayouts[localLine].TextLayout.HitTestTextPosition(_caretController.Column);

        double y = 0;

        for (int i = 0; i < localLine; i++)
        {
            y += codeLineLayouts[i].TextLayout.Height;
        }

        _editorRenderCache.CaretPosInCodeLayout = (hitTestRect.X, y);
        _editorRenderCache.IsCaretInVisibleArea = true;
    }

    private double GetSelectionEolCellWidth() // Eol -> End of line
    {
        _spaceLayout?.Dispose();
        _spaceLayout = new TextLayout(
            " ",
            Typeface,
            FontSize,
            Foreground,
            TextAlignment.Left,
            lineHeight: GetLineHeight()
        );

        double spaceRectWidth = _spaceLayout!.HitTestTextRange(0, 1).Sum(r => r.Width);

        if (spaceRectWidth <= 0)
        {
            spaceRectWidth = FontSize * 0.6;
        }

        return spaceRectWidth;
    }

    private void AdjustFontSize(double delta)
    {
        int nextFontSize = ClampFontSize(FontSize + delta);

        if (!nextFontSize.Equals(FontSize))
        {
            FontSize = nextFontSize;
        }
    }

    private static int ClampFontSize(double fontSize)
    {
        double clamped = Math.Clamp(fontSize, MinEditorFontSize, MaxEditorFontSize);
        return (int)Math.Round(clamped, MidpointRounding.AwayFromZero);
    }

    private int ConvertGlobalDocLineToLocal(int line)
    {
        if (!_viewportManager.AreThereVisibleLines())
        {
            return 0;
        }

        (int firstVisibleLineIndex, _) = _viewportManager.GetVisibleVerticalRange();

        int localLine = line - firstVisibleLineIndex;

        if (localLine < 0)
        {
            localLine = 0;
        }

        return localLine;
    }

    // The viewport is the vertical area available for rendering code text.
    // In practice, this is the visible height of the code layout area.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetCodeAreaHeight()
    {
        return Math.Max(0, Bounds.Height - 2 * ContentMargin);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetLineHeight()
    {
        double padding = FontSize / DefaultEditorFontSize * 4;
        return FontSize + padding;
    }

    public void SetCode(string code)
    {
        _editorStateTracker.Snapshot();

        _textBuffer.SetText(code);
        _undoRedoManager.Clear();
        _caretController.SetPosition(0, 0);
        _selectionController.CollapseTo(0, 0);
        _codeLayoutManager.RebuildFullLayout();

        _codeAnalysisSession?.SetPendingSourceText(_textBuffer.SourceText);

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    public string GetCode()
    {
        return _textBuffer.GetText();
    }

    public int GetCodeLength()
    {
        return _textBuffer.TextLength;
    }

    /// <summary>
    /// Scrolls the editor to the given pixel offset, clamped to the valid
    /// scroll range. Fires <see cref="ScrollMetricsChanged"/> when done.
    /// </summary>
    public void ScrollTo(double x, double y)
    {
        _viewportManager.ScrollToX(Math.Clamp(x, 0, MaxScrollX));
        _viewportManager.ScrollToY(Math.Clamp(y, 0, MaxScrollY));

        _codeLayoutManager.SynchWithViewport();
        _lineNumbersLayoutManager.RebuildLayout();
        RecalculateCaretPosOnScreen();
        RebuildSelectionGeometry();
        InvalidateVisual();
        ScrollMetricsChanged?.Invoke(this, EventArgs.Empty);
    }

    private double ComputeMaxLineWidth()
    {
        double charWidth = _editorRenderCache.EndOfLineCellWidth;

        if (charWidth <= 0)
        {
            return 0;
        }

        int maxLen = 0;

        for (int i = 0; i < _textBuffer.LineCount; i++)
        {
            int len = _textBuffer.GetLineLength(i);

            if (len > maxLen)
            {
                maxLen = len;
            }
        }

        return maxLen * charWidth;
    }

    public void CommentOutSelectedLines()
    {
        _editorStateTracker.Snapshot();

        _keyboardInputHandler.ForceComment();

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    public void UncommentSelectedLines()
    {
        _editorStateTracker.Snapshot();

        _keyboardInputHandler.ForceUncomment();

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    public void UndoChange()
    {
        _editorStateTracker.Snapshot();

        _keyboardInputHandler.Undo();

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    public void RedoChange()
    {
        _editorStateTracker.Snapshot();

        _keyboardInputHandler.Redo();

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    public void SetCaretPosition(int line, int column)
    {
        _editorStateTracker.Snapshot();

        _caretController.SetPosition(line, column);
        _selectionController.CollapseTo(line, column);
        _viewportManager.EnsureCaretIsVisible(_caretController);

        Focus();

        EditorDirtyFlags editorDirtyFlags = _editorStateTracker.ComputeDirtyFlags();

        EnsureFreshLayout(editorDirtyFlags);
    }

    public bool HasErrors()
    {
        return _currentErrors.Count > 0;
    }
}
