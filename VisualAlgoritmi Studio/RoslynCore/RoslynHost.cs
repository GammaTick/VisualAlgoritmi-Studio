using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    public class RoslynHost : IDisposable
    {
        private readonly AdhocWorkspace _workspace;
        private readonly ProjectId _projectId;
        private DocumentId _documentId;
        private bool _disposed;

        public RoslynHost()
        {
            _workspace = new AdhocWorkspace();

            _projectId = ProjectId.CreateNewId();

            var parseOptions = new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp14,
                kind: SourceCodeKind.Regular
            );

            var compilationOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Debug,
                allowUnsafe: true,
                checkOverflow: false,
                warningLevel: 4,
                deterministic: true,
                nullableContextOptions: NullableContextOptions.Enable,
                platform: Platform.AnyCpu,
                assemblyIdentityComparer: AssemblyIdentityComparer.Default
            );

            var references = GetDefaultReferences();

            var projectInfo = ProjectInfo.Create(
                _projectId,
                VersionStamp.Create(),
                "VisualAlgoritmi_Studio",
                "VisualAlgoritmi_Studio",
                LanguageNames.CSharp,
                parseOptions: parseOptions,
                compilationOptions: compilationOptions,
                metadataReferences: references
            );

            _workspace.AddProject(projectInfo);

            var document = _workspace.AddDocument(_projectId, "Code.cs", SourceText.From(string.Empty));
            _documentId = document.Id;
        }

        public async Task<SyntaxTree?> GetSyntaxTreeAsync()
        {
            var document = _workspace.CurrentSolution.GetDocument(_documentId);

            if (document == null)
            {
                return null;
            }

            return await document.GetSyntaxTreeAsync();
        }

        public Document? GetDocument()
        {
            var document = _workspace.CurrentSolution.GetDocument(_documentId);

            if (document == null)
            {
                return null;
            }

            return document;
        }

        public void UpdateDocument(Document newDocument)
        {
            _workspace.TryApplyChanges(newDocument.Project.Solution);
        }

        private static List<MetadataReference> GetDefaultReferences()
        {
            return RoslynHostReferences.GetDefaultReferences();
        }

        public async Task<Microsoft.CodeAnalysis.Compilation?> GetCompilationAsync()
        {
            var document = _workspace.CurrentSolution.GetDocument(_documentId);

            if (document == null)
            {
                return null;
            }

            return await document.Project.GetCompilationAsync();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _workspace.Dispose();

            _disposed = true;
        }
    }
}
