using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VisualAlgoritmi_Studio.Rewriting
{
    internal sealed class TypesToVisualTypesSemanticRewriter : CSharpSyntaxRewriter
    {
        private readonly SemanticModel _semanticModel;
        private readonly Dictionary<INamedTypeSymbol, SyntaxToken> _replacements;
        private readonly HashSet<string> _candidateNames;

        public TypesToVisualTypesSemanticRewriter(
            SemanticModel semanticModel,
            Dictionary<INamedTypeSymbol, string> replacements)
        {
            _semanticModel = semanticModel;

            _replacements = new Dictionary<INamedTypeSymbol, SyntaxToken>(
                SymbolEqualityComparer.Default);

            _candidateNames = new HashSet<string>();

            foreach (var replacement in replacements)
            {
                _replacements[replacement.Key] = SyntaxFactory.Identifier(replacement.Value);

                _candidateNames.Add(replacement.Key.Name);
            }
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (!_candidateNames.Contains(node.Identifier.Text))
            {
                return base.VisitIdentifierName(node);
            }

            return RewriteName(node, node.Identifier.Text) 
                ?? base.VisitIdentifierName(node);
        }

        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            if (!_candidateNames.Contains(node.Identifier.Text))
            {
                return base.VisitGenericName(node);
            }

            return RewriteName(node, node.Identifier.Text) 
                ?? base.VisitGenericName(node);
        }

        private SyntaxNode? RewriteName(SimpleNameSyntax node, string name)
        {
            var symbol = _semanticModel.GetSymbolInfo(node).Symbol as INamedTypeSymbol;

            if (symbol == null)
            {
                symbol = _semanticModel.GetTypeInfo(node).Type as INamedTypeSymbol;
            }

            if (symbol == null)
            {
                return null;
            }

            if (!_replacements.TryGetValue(symbol.OriginalDefinition, out var replacementToken))
            {
                return null;
            }

            return node switch
            {
                IdentifierNameSyntax identifierName => identifierName.WithIdentifier(replacementToken),

                GenericNameSyntax genericName => genericName.WithIdentifier(replacementToken),

                _ => null
            };
        }
    }
}