using System;
using System.IO;
using System.Threading;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    public sealed class ConsoleRedirectReader : TextReader
    {
        private Action? _onInputRequested;
        private string? _pendingInput;
        private readonly ManualResetEventSlim _inputReady = new(false);
        private volatile bool _cancelled;

        public void SetInputRequestedCallback(Action callback)
        {
            _onInputRequested = callback;
        }

        public override string? ReadLine()
        {
            _inputReady.Reset();
            _cancelled = false;
            _onInputRequested?.Invoke();
            _inputReady.Wait();

            if (_cancelled)
            {
                return null;
            }

            var input = _pendingInput;
            _pendingInput = null;
            return input;
        }

        public void SubmitInput(string input)
        {
            _pendingInput = input;
            _inputReady.Set();
        }

        /// <summary>
        /// Unblocks any pending <see cref="ReadLine"/> call, causing it to return <c>null</c>.
        /// </summary>
        public void CancelPendingRead()
        {
            _cancelled = true;
            _pendingInput = null;
            _inputReady.Set();
        }
    }
}