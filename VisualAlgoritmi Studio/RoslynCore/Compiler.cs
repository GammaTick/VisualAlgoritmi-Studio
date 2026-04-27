using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VisualAlgoritmi_Studio.Controls.Canvas;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    public sealed class Compiler
    {
        private readonly CodeAnalysisSession _codeAnalysisSession;
        private readonly ConsoleRedirectWriter _consoleRedirectWriter;
        private readonly ConsoleRedirectReader _consoleRedirectReader;
        private readonly DataStructureMetadata _dataStructureMetadata;

        public Compiler(CodeAnalysisSession codeAnalysisSession,
            ConsoleRedirectWriter consoleRedirectWriter,
            ConsoleRedirectReader consoleRedirectReader,
            DataStructureMetadata dataStructureMetadata)
        {
            _codeAnalysisSession = codeAnalysisSession;
            _consoleRedirectWriter = consoleRedirectWriter;
            _consoleRedirectReader = consoleRedirectReader;
            _dataStructureMetadata = dataStructureMetadata;
        }

        public async Task<CompileResult> CompileAndRun()
        {
            var document = _codeAnalysisSession.GetDocument();

            if (document == null)
            {
                return CompileResult.Failure("Document not found.");
            }

            var compilation = await document.Project.GetCompilationAsync();

            if (compilation == null)
            {
                return CompileResult.Failure("Compilation not found.");
            }

            var syntaxTree = await document.GetSyntaxTreeAsync();

            if (syntaxTree == null)
            {
                return CompileResult.Failure("Syntax tree not found.");
            }

            var newCompilation = compilation;

            var bclTypeDef = compilation.GetTypeByMetadataName(_dataStructureMetadata.OriginalTypeMetadataName);

            if (bclTypeDef != null)
            {
                var rewritten = await Rewrite(document, compilation, syntaxTree, bclTypeDef);
                
                if (rewritten != null)
                {
                    newCompilation = rewritten;
                }
            }

            using var ms = new MemoryStream();
            using var pdbMs = new MemoryStream();

            var emitResult = newCompilation.Emit(ms, pdbMs, 
                options: new Microsoft.CodeAnalysis.Emit.EmitOptions(debugInformationFormat: Microsoft.CodeAnalysis.Emit.DebugInformationFormat.PortablePdb));

            if (!emitResult.Success)
            {
                return CompileResult.CompilationError(emitResult.Diagnostics.ToList());
            }

            ms.Seek(0, SeekOrigin.Begin);
            pdbMs.Seek(0, SeekOrigin.Begin);

            var assembly = Assembly.Load(ms.ToArray(), pdbMs.ToArray());
            var entry = assembly.EntryPoint;

            if (entry == null)
            {
                return CompileResult.Failure("No Main method found.");
            }

            var parameters = entry.GetParameters().Length == 0
                ? null
                : new object[] { Array.Empty<string>() };

            var originalOut = Console.Out;
            var originalIn = Console.In;

            Console.SetOut(_consoleRedirectWriter);
            Console.SetIn(_consoleRedirectReader);

            Exception? userException = null;

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        entry.Invoke(null, parameters);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is not null)
                    {
                        userException = ex.InnerException;
                    }
                    catch (Exception ex)
                    {
                        userException = ex;
                    }
                });
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetIn(originalIn);
            }

            return userException is null
                ? CompileResult.Success()
                : CompileResult.RuntimeError(userException); 
        }

        private async Task<Compilation?> Rewrite(
            Document document,
            Compilation compilation,
            SyntaxTree syntaxTree,
            INamedTypeSymbol bclTypeDef)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: true);
            var root = await syntaxTree.GetRootAsync();

            var replacements = new System.Collections.Generic.Dictionary<INamedTypeSymbol, string>(
                SymbolEqualityComparer.Default)
            {
                [bclTypeDef] = _dataStructureMetadata.ReplacementTypeMetadataName
            };

            if (_dataStructureMetadata.OriginalTypeMetadataName == "System.Collections.Generic.LinkedList`1")
            {
                var linkedListNodeTypeDef = compilation.GetTypeByMetadataName(
                    "System.Collections.Generic.LinkedListNode`1");

                if (linkedListNodeTypeDef == null)
                {
                    return null;
                }

                replacements[linkedListNodeTypeDef] = "VisualLinkedListNode";
            }

            var rewriter = new TypesToVisualTypesSemanticRewriter(
                semanticModel,
                replacements);

            var newRoot = rewriter.Visit(root);

            if (newRoot == null)
            {
                return null;
            }

            var compilationUnit = (CompilationUnitSyntax)newRoot;

            if (!HasUsing(compilationUnit, _dataStructureMetadata.CanvasNamespace))
            {
                compilationUnit = compilationUnit.AddUsings(
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.ParseName(_dataStructureMetadata.CanvasNamespace)),
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.ParseName(_dataStructureMetadata.ReplacementTypeRuntimeType.Namespace!))
                );

                newRoot = compilationUnit;
            }

            var newDoc = document.WithSyntaxRoot(newRoot);

            return await newDoc.Project.GetCompilationAsync();
        }

        private static bool HasUsing(CompilationUnitSyntax compilationUnit, string namespaceName)
        {
            return compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName);
        }
    }
}