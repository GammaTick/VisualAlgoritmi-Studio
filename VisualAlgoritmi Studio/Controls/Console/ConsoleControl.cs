using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Text;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.Controls.Console;

public class ConsoleControl : UserControl
{
    private readonly TextBlock _outputTextBlock;
    private readonly ScrollViewer _scrollViewer;

    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _inputLineBuffer = new();

    private readonly DispatcherTimer _caretTimer;

    private bool _inputEnabled;
    private bool _caretVisible;
    private bool _scrollQueued;

    public Func<string, Task>? InputSubmittedAsync { get; set; }

    public ConsoleControl()
    {
        Focusable = true;

        _caretTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(530)
        };

        _caretTimer.Tick += (_, _) =>
        {
            _caretVisible = !_caretVisible;
            RefreshDisplay();
        };

        _outputTextBlock = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.Parse("#D4D4D4")),
            FontFamily = new FontFamily("Cascadia Mono"),
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(4),
        };

        _scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _outputTextBlock,
        };

        Content = _scrollViewer;
    }

    public void BeginSession()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(BeginSession);
            return;
        }

        _inputLineBuffer.Clear();

        _inputEnabled = true;
        _caretVisible = true;

        _caretTimer.Start();

        Focus();
        RefreshDisplay();
    }

    public void EndSession()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(EndSession);
            return;
        }

        _inputEnabled = false;
        _inputLineBuffer.Clear();

        _caretVisible = false;
        _caretTimer.Stop();

        RefreshDisplay();
    }

    public void Clear()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Clear);
            return;
        }

        _outputBuffer.Clear();
        _inputLineBuffer.Clear();

        _inputEnabled = false;
        _caretVisible = false;

        _caretTimer.Stop();

        _outputTextBlock.Text = string.Empty;

        QueueScrollToEnd();
    }

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AppendOutput(text));
            return;
        }

        _outputBuffer.Append(text);
        RefreshDisplay();
    }

    public void AppendError(string text)
    {
        AppendOutput(text);
    }

    public void AppendSystemMessage(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        AppendOutput($"{Environment.NewLine}{text}{Environment.NewLine}");
    }

    public void FocusConsole()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(FocusConsole);
            return;
        }

        Focus();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        Focus();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (!_inputEnabled || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        _inputLineBuffer.Append(e.Text);

        _caretVisible = true;

        RefreshDisplay();

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!_inputEnabled)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            SubmitCurrentLine();

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Back)
        {
            Backspace();

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            _inputLineBuffer.Clear();

            _caretVisible = true;

            RefreshDisplay();

            e.Handled = true;
        }
    }

    private void SubmitCurrentLine()
    {
        string input = _inputLineBuffer.ToString();

        _inputLineBuffer.Clear();

        _outputBuffer.Append(input);
        _outputBuffer.Append(Environment.NewLine);

        _caretVisible = true;

        RefreshDisplay();

        _ = SubmitInputAsync(input);
    }

    private void Backspace()
    {
        if (_inputLineBuffer.Length == 0)
        {
            return;
        }

        _inputLineBuffer.Remove(_inputLineBuffer.Length - 1, 1);

        _caretVisible = true;

        RefreshDisplay();
    }

    private async Task SubmitInputAsync(string input)
    {
        try
        {
            Func<string, Task>? handler = InputSubmittedAsync;

            if (handler != null)
            {
                await handler(input);
            }
        }
        catch (Exception ex)
        {
            AppendSystemMessage($"Console input failed: {ex.Message}");
        }
    }

    private void RefreshDisplay()
    {
        string caret = _inputEnabled && _caretVisible ? "|" : string.Empty;

        _outputTextBlock.Text =
            _outputBuffer.ToString() +
            _inputLineBuffer.ToString() +
            caret;

        QueueScrollToEnd();
    }

    private void QueueScrollToEnd()
    {
        if (_scrollQueued)
        {
            return;
        }

        _scrollQueued = true;

        Dispatcher.UIThread.Post(() =>
        {
            _scrollQueued = false;
            _scrollViewer.Offset = new Vector(0, _scrollViewer.Extent.Height);
        }, DispatcherPriority.Background);
    }
}