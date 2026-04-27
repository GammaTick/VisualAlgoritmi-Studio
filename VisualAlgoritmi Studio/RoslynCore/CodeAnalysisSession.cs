using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    public sealed class CodeAnalysisSession : IDisposable
    {
        private readonly RoslynHost _roslynHost;
        private readonly object _lock = new object();

        private SourceText? _pendingSourceText;
        private CancellationTokenSource? _debounceCts;
        private SyntaxTree _fastTree = SyntaxFactory.ParseSyntaxTree(string.Empty);

        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(300);
        private readonly TimeSpan _maxDebounceTime = TimeSpan.FromSeconds(2);
        private DateTime? _debounceStartTime;

        public event EventHandler? DocumentUpdated;

        public CodeAnalysisSession(RoslynHost roslynHost)
        {
            _roslynHost = roslynHost;
        }

        public Task<SyntaxTree?> GetSyntaxTreeAsync()
        {
            return _roslynHost.GetSyntaxTreeAsync();
        }

        public Document? GetDocument()
        {
            return _roslynHost.GetDocument();
        }

        public void SetPendingSourceText(SourceText sourceText)
        {
            bool forceFlush;
            CancellationTokenSource? newCts = null;

            _fastTree = _fastTree.WithChangedText(sourceText);

            lock (_lock)
            {
                _pendingSourceText = sourceText;

                _debounceStartTime ??= DateTime.UtcNow;
                forceFlush = (DateTime.UtcNow - _debounceStartTime.Value) >= _maxDebounceTime;

                _debounceCts?.Cancel();
                _debounceCts?.Dispose();

                if (forceFlush)
                {
                    _debounceCts = null;
                    _debounceStartTime = null;
                }
                else
                {
                    _debounceCts = new CancellationTokenSource();
                    newCts = _debounceCts;
                }
            }

            if (forceFlush)
            {
                FlushPendingSourceTextAsync();
                return;
            }

            _ = DebounceApplyAsync(newCts!.Token);
        }

        public SyntaxTree GetFastSyntaxTree()
        {
            return _fastTree;
        }

        public void FlushPendingSourceTextAsync()
        {
            SourceText? sourceText;

            lock (_lock)
            {
                sourceText = _pendingSourceText;
                _pendingSourceText = null;
                _debounceStartTime = null;
            }

            if (sourceText == null)
            {
                return;
            }

            var document = _roslynHost.GetDocument();

            if (document == null)
            {
                return;
            }

            var newDocument = document.WithText(sourceText);
            _roslynHost.UpdateDocument(newDocument);

            DocumentUpdated?.Invoke(this, EventArgs.Empty);
        }

        private async Task DebounceApplyAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_debounceDelay, cancellationToken);
                FlushPendingSourceTextAsync();
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
                _debounceCts = null;
            }
        }
    }
}
