using Avalonia.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VisualAlgoritmi_Studio.Controls.Editor.SyntaxHighlighting
{
    internal sealed class SyntaxBrushCache
    {
        public static readonly SolidColorBrush NumbersBrush = new(Color.FromRgb(181, 206, 168));
        public static readonly SolidColorBrush VariableTypesBrush = new(Color.FromRgb(86, 156, 214));
        public static readonly SolidColorBrush UsingKeywordBrush = new(Color.FromRgb(86, 156, 214));
        public static readonly SolidColorBrush ModifiersBrush = new(Color.FromRgb(86, 156, 214));
        public static readonly SolidColorBrush LiteralsBrush = new(Color.FromRgb(86, 156, 214));
        public static readonly SolidColorBrush ControlFlowBranchingBrush = new(Color.FromRgb(216, 160, 223));
        public static readonly SolidColorBrush ControlFlowLoopsBrush = new(Color.FromRgb(216, 160, 223));
        public static readonly SolidColorBrush ControlFlowJumpsBrush = new(Color.FromRgb(216, 160, 223));
        public static readonly SolidColorBrush ExceptionHandlingBrush = new(Color.FromRgb(216, 160, 223));
        public static readonly SolidColorBrush ParameterModifiersBrush = new(Color.FromRgb(86, 156, 214));
        public static readonly SolidColorBrush KeywordTypeDeclarationBrush = new(Color.FromRgb(86, 156, 214));
        public static readonly SolidColorBrush CommentBrush = new(Color.FromRgb(87, 166, 74));
        public static readonly SolidColorBrush StringBrush = new(Color.FromRgb(214, 157, 133));
        public static readonly SolidColorBrush MethodBrush = new(Color.FromRgb(220, 220, 170));
        public static readonly SolidColorBrush ConstructorBrush = new(Color.FromRgb(78, 201, 176));
        public static readonly SolidColorBrush ClassBrush = new(Color.FromRgb(78, 201, 176));
        public static readonly SolidColorBrush InterfaceBrush = new(Color.FromRgb(184, 215, 163));
        public static readonly SolidColorBrush LocalVariableBrush = new(Color.FromRgb(156, 220, 254));
        public static readonly SolidColorBrush DefaultColorBrush = new(Color.FromRgb(230, 230, 230));

        public static SolidColorBrush GetBrushForFastTokenType(SyntaxToken token)
        {
            return GetBrushForNonSemanticTokenType(token) ?? DefaultColorBrush;
        }

        public static SolidColorBrush GetBrushForTokenType(SyntaxToken token, SemanticModel? semanticModel)
        {
            var nonSemanticBrush = GetBrushForNonSemanticTokenType(token);
            if (nonSemanticBrush != null)
            {
                return nonSemanticBrush;
            }

            if (semanticModel == null)
            {
                return DefaultColorBrush;
            }

            if (!token.IsKind(SyntaxKind.IdentifierToken))
            {
                return DefaultColorBrush;
            }

            // `var` is an IdentifierToken whose parent is an implicitly-typed IdentifierNameSyntax.
            // GetSymbolInfo on it returns the resolved concrete type (INamedTypeSymbol), which would
            // wrongly be colored as a class. Treat it like a built-in type keyword instead.
            if (token.Parent is IdentifierNameSyntax { IsVar: true })
            {
                return VariableTypesBrush;
            }

            var symbol =
                semanticModel.GetDeclaredSymbol(token.Parent!) ??
                semanticModel.GetSymbolInfo(token.Parent!).Symbol;

            if (symbol == null)
            {
                return DefaultColorBrush;
            }

            return symbol switch
            {
                IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } => ConstructorBrush,
                IMethodSymbol => MethodBrush,
                INamedTypeSymbol { TypeKind: TypeKind.Interface } => InterfaceBrush,
                INamedTypeSymbol => ClassBrush,
                ILocalSymbol => LocalVariableBrush,
                IParameterSymbol => LocalVariableBrush,
                _ => DefaultColorBrush
            };
        }

        private static SolidColorBrush? GetBrushForNonSemanticTokenType(SyntaxToken token)
        {
            return token.Kind() switch
            {
                SyntaxKind.NumericLiteralToken => NumbersBrush,

                SyntaxKind.IntKeyword or
                SyntaxKind.StringKeyword or
                SyntaxKind.BoolKeyword or
                SyntaxKind.DoubleKeyword or
                SyntaxKind.FloatKeyword or
                SyntaxKind.DecimalKeyword or
                SyntaxKind.ByteKeyword or
                SyntaxKind.CharKeyword or
                SyntaxKind.LongKeyword or
                SyntaxKind.ShortKeyword or
                SyntaxKind.VoidKeyword or
                SyntaxKind.ObjectKeyword => VariableTypesBrush,

                SyntaxKind.PublicKeyword or
                SyntaxKind.PrivateKeyword or
                SyntaxKind.ProtectedKeyword or
                SyntaxKind.InternalKeyword or
                SyntaxKind.StaticKeyword or
                SyntaxKind.SealedKeyword or
                SyntaxKind.AbstractKeyword or
                SyntaxKind.VirtualKeyword or
                SyntaxKind.OverrideKeyword or
                SyntaxKind.ReadOnlyKeyword or
                SyntaxKind.ConstKeyword => ModifiersBrush,

                SyntaxKind.TrueKeyword or
                SyntaxKind.FalseKeyword or
                SyntaxKind.NullKeyword => LiteralsBrush,

                SyntaxKind.IfKeyword or
                SyntaxKind.ElseKeyword or
                SyntaxKind.SwitchKeyword or
                SyntaxKind.CaseKeyword => ControlFlowBranchingBrush,

                SyntaxKind.ForKeyword or
                SyntaxKind.ForEachKeyword or
                SyntaxKind.WhileKeyword or
                SyntaxKind.DoKeyword => ControlFlowLoopsBrush,

                SyntaxKind.BreakKeyword or
                SyntaxKind.ContinueKeyword or
                SyntaxKind.ReturnKeyword => ControlFlowJumpsBrush,

                SyntaxKind.TryKeyword or
                SyntaxKind.CatchKeyword or
                SyntaxKind.FinallyKeyword or
                SyntaxKind.ThrowKeyword => ExceptionHandlingBrush,

                SyntaxKind.RefKeyword or
                SyntaxKind.OutKeyword or
                SyntaxKind.ParamsKeyword or
                SyntaxKind.InKeyword => ParameterModifiersBrush,

                SyntaxKind.ClassKeyword or
                SyntaxKind.StructKeyword or
                SyntaxKind.InterfaceKeyword or
                SyntaxKind.EnumKeyword => KeywordTypeDeclarationBrush,

                SyntaxKind.StringLiteralToken or
                SyntaxKind.CharacterLiteralToken or
                SyntaxKind.InterpolatedStringTextToken or
                SyntaxKind.InterpolatedStringStartToken or
                SyntaxKind.InterpolatedStringEndToken or
                SyntaxKind.InterpolatedVerbatimStringStartToken or
                SyntaxKind.InterpolatedSingleLineRawStringStartToken or
                SyntaxKind.InterpolatedMultiLineRawStringStartToken or
                SyntaxKind.InterpolatedRawStringEndToken or
                SyntaxKind.Utf8StringLiteralToken or
                SyntaxKind.SingleLineRawStringLiteralToken or
                SyntaxKind.MultiLineRawStringLiteralToken => StringBrush,

                SyntaxKind.PlusToken or
                SyntaxKind.PlusPlusToken or
                SyntaxKind.MinusToken or
                SyntaxKind.MinusMinusToken or
                SyntaxKind.AsteriskToken or
                SyntaxKind.SlashToken or
                SyntaxKind.PercentToken or
                SyntaxKind.EqualsToken or
                SyntaxKind.EqualsEqualsToken or
                SyntaxKind.ExclamationEqualsToken or
                SyntaxKind.LessThanToken or
                SyntaxKind.GreaterThanToken or
                SyntaxKind.LessThanEqualsToken or
                SyntaxKind.GreaterThanEqualsToken or
                SyntaxKind.AmpersandAmpersandToken or
                SyntaxKind.BarBarToken or
                SyntaxKind.OpenParenToken or
                SyntaxKind.CloseParenToken or
                SyntaxKind.OpenBraceToken or
                SyntaxKind.CloseBraceToken or
                SyntaxKind.OpenBracketToken or
                SyntaxKind.CloseBracketToken or
                SyntaxKind.SemicolonToken or
                SyntaxKind.CommaToken or
                SyntaxKind.DotToken or
                SyntaxKind.ColonToken => DefaultColorBrush,

                SyntaxKind.UsingKeyword or
                SyntaxKind.NewKeyword => UsingKeywordBrush,

                _ => null
            };
        }
    }
}