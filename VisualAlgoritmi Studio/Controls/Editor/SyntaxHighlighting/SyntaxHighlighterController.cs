using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using VisualAlgoritmi_Studio.RoslynCore;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.CompilerServices;

namespace VisualAlgoritmi_Studio.Controls.Editor.SyntaxHighlighting
{
    internal sealed class SyntaxHighlighterController
    {
        private static readonly IReadOnlyList<ValueSpan<TextRunProperties>> _emptyHighlighting = [];

        private readonly CodeEditor _codeEditor;
        private readonly TextBuffer _textBuffer;
        private readonly CodeAnalysisSession _codeAnalysisSession;

        private Dictionary<int, IReadOnlyList<HighlightingSpan>> _lexicalHighlightingCache = [];
        private Dictionary<int, LexerLineState> _lineEndStates = [];
        private readonly Dictionary<IBrush, TextRunProperties> _textRunPropertiesCache = [];

        private SemanticModel? _semanticModel;

        public event EventHandler? NewHighlightingAvailable;

        public SyntaxHighlighterController(
            CodeEditor codeEditor,
            TextBuffer textBuffer,
            CodeAnalysisSession codeAnalysisSession)
        {
            _codeEditor = codeEditor;
            _textBuffer = textBuffer;
            _codeAnalysisSession = codeAnalysisSession;
        }

        /// <summary>
        /// Applies lexical highlighting for a single document line.
        /// Returns true if the line's end state changed, meaning subsequent lines may need re-highlighting.
        /// </summary>
        public bool ApplyLexicalHighlightingForLine(int documentLine)
        {
            var fastTree = _codeAnalysisSession.GetFastSyntaxTree();
            var root = fastTree.GetRoot();

            if (root == null)
            {
                return false;
            }

            var textLine = _textBuffer.GetLine(documentLine);
            var lineSpan = textLine.Span;
            int lineStart = lineSpan.Start;
            int lineEnd = lineSpan.End;

            var tokens = root.DescendantTokens(lineSpan, descendIntoTrivia: true);

            var spans = new List<HighlightingSpan>();
            var endState = LexerLineState.Default;

            foreach (var token in tokens)
            {
                // 1. ALWAYS process leading trivia
                foreach (var trivia in token.LeadingTrivia)
                {
                    if (!IsComment(trivia))
                    {
                        continue;
                    }

                    var span = trivia.Span;

                    int triviaStart = Math.Max(span.Start, lineStart);
                    int triviaEnd = Math.Min(span.End, lineEnd);

                    if (triviaEnd > triviaStart)
                    {
                        spans.Add(
                            new HighlightingSpan(
                                triviaStart - lineStart,
                                triviaEnd - triviaStart,
                                SyntaxBrushCache.CommentBrush));
                    }

                    if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) && span.End > lineEnd)
                    {
                        endState = LexerLineState.InBlockComment;
                    }
                }

                // 2. THEN process token
                var tokenSpan = token.Span;

                int start = Math.Max(tokenSpan.Start, lineStart);
                int end = Math.Min(tokenSpan.End, lineEnd);

                if (end > start)
                {
                    spans.Add(
                        new HighlightingSpan(
                            start - lineStart,
                            end - start,
                            SyntaxBrushCache.GetBrushForFastTokenType(token)));
                }

                if (tokenSpan.End > lineEnd && IsMultiLineStringToken(token.Kind()))
                {
                    endState = LexerLineState.InVerbatimString;
                }

                // 3. ALWAYS process trailing trivia
                foreach (var trivia in token.TrailingTrivia)
                {
                    if (!IsComment(trivia))
                    {
                        continue;
                    }

                    var span = trivia.Span;

                    int triviaStart = Math.Max(span.Start, lineStart);
                    int triviaEnd = Math.Min(span.End, lineEnd);

                    if (triviaEnd > triviaStart)
                    {
                        spans.Add(
                            new HighlightingSpan(
                                triviaStart - lineStart,
                                triviaEnd - triviaStart,
                                SyntaxBrushCache.CommentBrush));
                    }

                    if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) && span.End > lineEnd)
                    {
                        endState = LexerLineState.InBlockComment;
                    }
                }
            }

            _lexicalHighlightingCache[documentLine] = spans;

            _lineEndStates.TryGetValue(documentLine, out var previousEndState);
            _lineEndStates[documentLine] = endState;

            NewHighlightingAvailable?.Invoke(this, EventArgs.Empty);

            return endState != previousEndState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsComment(SyntaxTrivia trivia)
        {
            return trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsMultiLineStringToken(SyntaxKind kind)
        {
            return kind == SyntaxKind.StringLiteralToken
                || kind == SyntaxKind.Utf8StringLiteralToken
                || kind == SyntaxKind.InterpolatedStringTextToken
                || kind == SyntaxKind.MultiLineRawStringLiteralToken
                || kind == SyntaxKind.Utf8MultiLineRawStringLiteralToken;
        }

        public IReadOnlyList<ValueSpan<TextRunProperties>> GetHighlightingForLine(int line)
        {
            _lexicalHighlightingCache.TryGetValue(line, out var lexical);
            bool hasLexical = lexical != null && lexical.Count > 0;

            // Compute semantic spans on-the-fly from the current semantic model.
            // Skip when the model is stale (text length mismatch) to avoid misaligned offsets.
            List<(int Start, int Length, IBrush Brush)>? semantic = null;

            if (_semanticModel != null)
            {
                var sourceText = _semanticModel.SyntaxTree.GetText();

                if (sourceText.Length == _textBuffer.TextLength && line < sourceText.Lines.Count)
                {
                    var textLine = sourceText.Lines[line];
                    var root = _semanticModel.SyntaxTree.GetRoot();

                    foreach (var token in root.DescendantTokens(textLine.Span))
                    {
                        if (token.Span.Start < textLine.Start)
                        {
                            continue;
                        }

                        var semanticBrush = SyntaxBrushCache.GetBrushForTokenType(token, _semanticModel);
                        var lexicalBrush = SyntaxBrushCache.GetBrushForFastTokenType(token);

                        if (ReferenceEquals(semanticBrush, lexicalBrush))
                        {
                            continue;
                        }

                        int relativeStart = token.Span.Start - textLine.Start;

                        semantic ??= new();
                        semantic.Add((relativeStart, token.Span.Length, semanticBrush));
                    }
                }
            }

            bool hasSemantic = semantic != null && semantic.Count > 0;

            if (!hasLexical && !hasSemantic)
            {
                return _emptyHighlighting;
            }

            // No semantic overrides — return lexical spans as-is.
            if (!hasSemantic)
            {
                var lexicalResult = new List<ValueSpan<TextRunProperties>>(lexical!.Count);

                foreach (var span in lexical)
                {
                    lexicalResult.Add(
                        new ValueSpan<TextRunProperties>(
                            span.Start,
                            span.Length,
                            GetTextRunProperties(span.ForegroundBrush)));
                }

                return lexicalResult;
            }

            var result = new List<ValueSpan<TextRunProperties>>((lexical?.Count ?? 0) + semantic!.Count);

            int lexicalIndex = 0;
            int semanticIndex = 0;

            while (hasLexical && lexicalIndex < lexical!.Count && semanticIndex < semantic.Count)
            {
                var lexicalSpan = lexical[lexicalIndex];
                var semanticSpan = semantic[semanticIndex];

                if (lexicalSpan.Start < semanticSpan.Start)
                {
                    result.Add(
                        new ValueSpan<TextRunProperties>(
                            lexicalSpan.Start,
                            lexicalSpan.Length,
                            GetTextRunProperties(lexicalSpan.ForegroundBrush)));

                    lexicalIndex++;
                    continue;
                }

                if (lexicalSpan.Start > semanticSpan.Start)
                {
                    result.Add(
                        new ValueSpan<TextRunProperties>(
                            semanticSpan.Start,
                            semanticSpan.Length,
                            GetTextRunProperties(semanticSpan.Brush)));

                    semanticIndex++;
                    continue;
                }

                result.Add(
                    new ValueSpan<TextRunProperties>(
                        semanticSpan.Start,
                        semanticSpan.Length,
                        GetTextRunProperties(semanticSpan.Brush)));

                lexicalIndex++;
                semanticIndex++;
            }

            if (hasLexical)
            {
                while (lexicalIndex < lexical!.Count)
                {
                    var lexicalSpan = lexical[lexicalIndex];

                    result.Add(
                        new ValueSpan<TextRunProperties>(
                            lexicalSpan.Start,
                            lexicalSpan.Length,
                            GetTextRunProperties(lexicalSpan.ForegroundBrush)));

                    lexicalIndex++;
                }
            }

            while (semanticIndex < semantic.Count)
            {
                var semanticSpan = semantic[semanticIndex];

                result.Add(
                    new ValueSpan<TextRunProperties>(
                        semanticSpan.Start,
                        semanticSpan.Length,
                        GetTextRunProperties(semanticSpan.Brush)));

                semanticIndex++;
            }

            return result;
        }

        /// <summary>
        /// Clears the per-brush TextRunProperties cache.
        /// Must be called whenever FontSize or Typeface changes so rebuilt layouts pick up the
        /// new metrics.
        /// </summary>
        public void InvalidateTextRunPropertiesCache()
        {
            _textRunPropertiesCache.Clear();
        }

        private TextRunProperties GetTextRunProperties(IBrush foregroundBrush)
        {
            if (_textRunPropertiesCache.TryGetValue(foregroundBrush, out var cachedProperties))
            {
                return cachedProperties;
            }

            var properties = new GenericTextRunProperties(
                _codeEditor.Typeface,
                _codeEditor.FontSize,
                foregroundBrush: foregroundBrush);

            _textRunPropertiesCache[foregroundBrush] = properties;

            return properties;
        }

        public void SetSemanticModel(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        /// <summary>
        /// Shifts lexical caches to stay consistent after lines are inserted or deleted.
        /// Call this immediately when the line count changes, before layouts are rebuilt.
        /// <paramref name="afterLine"/> is the last line that stayed in place (0-based).
        /// <paramref name="delta"/> is positive for insertions and negative for deletions.
        /// </summary>
        public void ShiftHighlightingCaches(int afterLine, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            if (delta > 0)
            {
                ShiftCacheUp(_lexicalHighlightingCache, afterLine, delta);
                ShiftCacheUp(_lineEndStates, afterLine, delta);
            }
            else
            {
                int deletedStart = afterLine + 1;
                int deletedEnd = afterLine - delta; // afterLine + |delta|

                ShiftCacheDown(_lexicalHighlightingCache, deletedStart, deletedEnd, delta);
                ShiftCacheDown(_lineEndStates, deletedStart, deletedEnd, delta);
            }
        }

        private static void ShiftCacheUp<TValue>(Dictionary<int, TValue> cache, int afterLine, int delta)
        {
            // Iterate in descending order so we don't overwrite entries that haven't been moved yet.
            var keys = new List<int>(cache.Count);

            foreach (var key in cache.Keys)
            {
                if (key > afterLine)
                {
                    keys.Add(key);
                }
            }

            keys.Sort((a, b) => b.CompareTo(a));

            foreach (var key in keys)
            {
                cache[key + delta] = cache[key];
                cache.Remove(key);
            }
        }

        private static void ShiftCacheDown<TValue>(Dictionary<int, TValue> cache, int deletedStart, int deletedEnd, int delta)
        {
            // Remove entries for the deleted lines.
            for (int k = deletedStart; k <= deletedEnd; k++)
            {
                cache.Remove(k);
            }

            // Shift entries after the deleted range downward (delta is negative).
            var keys = new List<int>(cache.Count);

            foreach (var key in cache.Keys)
            {
                if (key > deletedEnd)
                {
                    keys.Add(key);
                }
            }

            keys.Sort();

            foreach (var key in keys)
            {
                cache[key + delta] = cache[key];
                cache.Remove(key);
            }
        }

        private readonly record struct HighlightingSpan(int Start, int Length, IBrush ForegroundBrush);
    }
}