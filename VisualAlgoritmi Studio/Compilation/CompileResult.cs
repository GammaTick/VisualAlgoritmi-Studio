using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace VisualAlgoritmi_Studio.Compilation
{
    public readonly struct CompileResult
    {
        public CompileResultStatus Status { get; }

        public string? AssemblyPath { get; }
        public string? PdbPath { get; }

        public Exception? UserException { get; }
        public string? FailureMessage { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public bool IsSuccess => Status == CompileResultStatus.Success;

        private CompileResult(
            CompileResultStatus status,
            string? assemblyPath = null,
            string? pdbPath = null,
            Exception? userException = null,
            string? failureMessage = null,
            IEnumerable<Diagnostic>? diagnostics = null)
        {
            Status = status;
            AssemblyPath = assemblyPath;
            PdbPath = pdbPath;
            UserException = userException;
            FailureMessage = failureMessage;
            Diagnostics = diagnostics?.ToImmutableArray() ?? ImmutableArray<Diagnostic>.Empty;
        }

        public static CompileResult Success(string assemblyPath, string pdbPath)
        {
            return new CompileResult(
                CompileResultStatus.Success,
                assemblyPath: assemblyPath,
                pdbPath: pdbPath);
        }

        public static CompileResult CompilationError(IEnumerable<Diagnostic> diagnostics)
        {
            return new CompileResult(
                CompileResultStatus.CompilationError,
                failureMessage: "Compilation failed.",
                diagnostics: diagnostics);
        }
    }

    public enum CompileResultStatus
    {
        Success,
        CompilationError
    }
}