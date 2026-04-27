using System;
using System.IO;
using System.Text;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    public sealed class ConsoleRedirectWriter : TextWriter
    {
        private Action<string>? _onWrite;

        public ConsoleRedirectWriter() { }

        public ConsoleRedirectWriter(Action<string> onWrite)
        {
            _onWrite = onWrite;
        }

        public void SetOutputCallback(Action<string> callback)
        {
            _onWrite = callback;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            _onWrite?.Invoke(value.ToString());
        }

        public override void Write(string? value)
        {
            if (value != null)
            {
                _onWrite?.Invoke(value);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer != null && count > 0)
            {
                _onWrite?.Invoke(new string(buffer, index, count));
            }
        }

        public override void WriteLine()
        {
            _onWrite?.Invoke(NewLine);
        }

        public override void WriteLine(string? value)
        {
            _onWrite?.Invoke((value ?? string.Empty) + NewLine);
        }
    }
}