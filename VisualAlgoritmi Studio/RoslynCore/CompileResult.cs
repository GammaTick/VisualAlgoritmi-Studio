using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    public readonly struct CompileResult
    {
        public Exception? UserException { get; }
        public string? FailureMessage { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        private CompileResult(Exception? userException, string? failureMessage, IEnumerable<Diagnostic>? diagnostics = null)
        {
            UserException = userException;
            FailureMessage = failureMessage;
            Diagnostics = diagnostics?.ToImmutableArray() ?? ImmutableArray<Diagnostic>.Empty;
        }

        public static CompileResult Success() => new(null, null);

        public static CompileResult RuntimeError(Exception ex) => new(ex, null);

        public static CompileResult Failure(string message) => new(null, message);

        public static CompileResult CompilationError(IEnumerable<Diagnostic> diagnostics) 
            => new(null, "Compilation failed.", diagnostics);
    }
}