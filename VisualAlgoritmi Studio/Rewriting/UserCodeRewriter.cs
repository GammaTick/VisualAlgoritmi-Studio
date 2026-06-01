using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VisualAlgoritmi_Studio.RoslynCore.Metadata;

namespace VisualAlgoritmi_Studio.Rewriting
{
    internal static class UserCodeRewriter
    {
        public static async Task<Microsoft.CodeAnalysis.Compilation> RewriteAsync(
            Document document,
            Microsoft.CodeAnalysis.Compilation compilation,
            SyntaxTree syntaxTree,
            DataStructureMetadata dataStructureMetadata)
        {
            INamedTypeSymbol? bclTypeDef = compilation.GetTypeByMetadataName(dataStructureMetadata.OriginalTypeMetadataName);

            if (bclTypeDef == null)
            {
                return compilation;
            }

            SemanticModel semanticModel = compilation.GetSemanticModel(
                syntaxTree,
                ignoreAccessibility: true);

            SyntaxNode root = await syntaxTree.GetRootAsync();

            var replacements = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default)
            {
                [bclTypeDef] = dataStructureMetadata.ReplacementTypeMetadataName
            };

            var rewriter = new TypesToVisualTypesSemanticRewriter(
                semanticModel,
                replacements);

            SyntaxNode? rewrittenRoot = rewriter.Visit(root);

            if (rewrittenRoot == null)
            {
                return compilation;
            }

            if (rewrittenRoot is not CompilationUnitSyntax compilationUnit)
            {
                return compilation;
            }

            string? replacementNamespace = dataStructureMetadata.ReplacementTypeRuntimeType.Namespace;

            if (!string.IsNullOrWhiteSpace(replacementNamespace))
            {
                compilationUnit = AddUsingIfMissing(compilationUnit, replacementNamespace);
            }

            Document rewrittenDocument = document.WithSyntaxRoot(compilationUnit);

            Microsoft.CodeAnalysis.Compilation? rewrittenCompilation = await rewrittenDocument.Project.GetCompilationAsync();

            return rewrittenCompilation ?? compilation;
        }

        private static CompilationUnitSyntax AddUsingIfMissing(
            CompilationUnitSyntax compilationUnit,
            string namespaceName)
        {
            if (HasUsing(compilationUnit, namespaceName))
            {
                return compilationUnit;
            }

            return compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName)));
        }

        private static bool HasUsing(CompilationUnitSyntax compilationUnit, string namespaceName)
        {
            return compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName);
        }
    }
}