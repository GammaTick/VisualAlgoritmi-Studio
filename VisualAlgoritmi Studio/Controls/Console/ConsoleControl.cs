using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Text;
using VisualAlgoritmi_Studio.RoslynCore;

namespace VisualAlgoritmi_Studio.Controls.Console;

public class ConsoleControl : UserControl
{
    private readonly TextBlock _outputTextBlock;
    private readonly ScrollViewer _scrollViewer;
    private readonly StringBuilder _outputBuffer = new();
    private readonly StringBuilder _inputLineBuffer = new();

    private ConsoleRedirectWriter? _writer;
    private ConsoleRedirectReader? _reader;
    private bool _isWaitingForInput;
    private bool _caretVisible;
    private readonly DispatcherTimer _caretTimer;

    public ConsoleControl()
    {
        Focusable = true;

        _caretTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(530) };
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

    public void SetWriter(ConsoleRedirectWriter writer)
    {
        _writer = writer;
        writer.SetOutputCallback(AppendOutput);
    }

    public void SetReader(ConsoleRedirectReader reader)
    {
        _reader = reader;
        reader.SetInputRequestedCallback(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _isWaitingForInput = true;
                _caretVisible = true;
                _caretTimer.Start();
                Focus();
            });
        });
    }

    public void Clear()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _outputBuffer.Clear();
            _inputLineBuffer.Clear();
            _isWaitingForInput = false;
            _caretVisible = false;
            _caretTimer.Stop();
            _outputTextBlock.Text = string.Empty;
        }
        else
        {
            Dispatcher.UIThread.Post(Clear);
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (!_isWaitingForInput || string.IsNullOrEmpty(e.Text))
            return;

        _inputLineBuffer.Append(e.Text);
        RefreshDisplay();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!_isWaitingForInput)
            return;

        if (e.Key == Key.Enter)
        {
            var input = _inputLineBuffer.ToString();
            _inputLineBuffer.Clear();
            _isWaitingForInput = false;
            _caretVisible = false;
            _caretTimer.Stop();

            _outputBuffer.Append(input);
            _outputBuffer.Append(Environment.NewLine);
            RefreshDisplay();

            _reader?.SubmitInput(input);
            e.Handled = true;
        }
        else if (e.Key == Key.Back && _inputLineBuffer.Length > 0)
        {
            _inputLineBuffer.Remove(_inputLineBuffer.Length - 1, 1);
            RefreshDisplay();
            e.Handled = true;
        }
    }

    private void AppendOutput(string text)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            _outputBuffer.Append(text);
            RefreshDisplay();
        }
        else
        {
            Dispatcher.UIThread.Post(() => AppendOutput(text));
        }
    }

    private void RefreshDisplay()
    {
        var caret = (_isWaitingForInput && _caretVisible) ? "|" : string.Empty;
        if (_isWaitingForInput)
        {
            _outputTextBlock.Text = _outputBuffer.ToString() + _inputLineBuffer.ToString() + caret;
        }
        else
        {
            _outputTextBlock.Text = _outputBuffer.ToString();
        }

        _scrollViewer.Offset = new Vector(0, _scrollViewer.Extent.Height);
    }

    public void FocusConsole()
    {
        this.Focus();
    }
}
