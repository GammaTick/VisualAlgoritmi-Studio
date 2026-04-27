using System;
using Microsoft.CodeAnalysis.Text;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using Xunit;

namespace VisualAlgoritmiStudio.Tests;

public class TextBufferTests
{
    private static string GetLineText(TextBuffer textBuffer, int line)
    {
        return textBuffer.SourceText.ToString(textBuffer.GetLine(line).Span);
    }

    [Fact]
    public void NewBuffer_HasExpectedInitialState()
    {
        var textBuffer = new TextBuffer();

        Assert.Equal(string.Empty, textBuffer.GetText());
        Assert.Equal(0, textBuffer.TextLength);
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal(0, textBuffer.Version);
    }

    [Fact]
    public void InsertText_IntoEmptyBuffer_InsertsTextCorrectly()
    {
        var textBuffer = new TextBuffer();

        var change = textBuffer.InsertText(0, 0, "Hello");

        Assert.True(change.HasValue);
        Assert.Equal("Hello", textBuffer.GetText());
        Assert.Equal("Hello", GetLineText(textBuffer, 0));
        Assert.Equal(5, textBuffer.TextLength);
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal(1, textBuffer.Version);
    }

    [Fact]
    public void InsertText_WithEmptyString_ReturnsNull_AndDoesNotChangeAnything()
    {
        var textBuffer = new TextBuffer();

        var change = textBuffer.InsertText(0, 0, string.Empty);

        Assert.False(change.HasValue);
        Assert.Equal(string.Empty, textBuffer.GetText());
        Assert.Equal(0, textBuffer.TextLength);
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal(0, textBuffer.Version);
    }

    [Fact]
    public void InsertText_InMiddleOfLine_InsertsAtCorrectPosition()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Helo");

        var change = textBuffer.InsertText(0, 2, "l");

        Assert.True(change.HasValue);
        Assert.Equal("Hello", textBuffer.GetText());
        Assert.Equal("Hello", GetLineText(textBuffer, 0));
    }

    [Fact]
    public void InsertText_Null_ThrowsArgumentNullException()
    {
        var textBuffer = new TextBuffer();

        Assert.Throws<ArgumentNullException>(() => textBuffer.InsertText(0, 0, null!));
    }

    [Fact]
    public void InsertText_InvalidLine_ThrowsArgumentOutOfRangeException()
    {
        var textBuffer = new TextBuffer();

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.InsertText(1, 0, "A"));
    }

    [Fact]
    public void InsertText_InvalidColumn_ThrowsArgumentOutOfRangeException()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.InsertText(0, 99, "A"));
    }

    [Fact]
    public void InsertNewLineAtPosition_SplitsLineCorrectly()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("HelloWorld");

        var change = textBuffer.InsertNewLineAtPosition(0, 5);

        Assert.True(change.HasValue);
        Assert.Equal($"Hello{Environment.NewLine}World", textBuffer.GetText());
        Assert.Equal(2, textBuffer.LineCount);
        Assert.Equal("Hello", GetLineText(textBuffer, 0));
        Assert.Equal("World", GetLineText(textBuffer, 1));
    }

    [Fact]
    public void MergeLineWithPrevious_WhenLineIsZero_ReturnsNull_AndDoesNothing()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"A{Environment.NewLine}B");

        var versionBefore = textBuffer.Version;
        var change = textBuffer.MergeLineWithPrevious(0);

        Assert.False(change.HasValue);
        Assert.Equal($"A{Environment.NewLine}B", textBuffer.GetText());
        Assert.Equal(versionBefore, textBuffer.Version);
    }

    [Fact]
    public void MergeLineWithPrevious_RemovesLineBreakAndMergesLines()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"ABC{Environment.NewLine}DEF");

        var change = textBuffer.MergeLineWithPrevious(1);

        Assert.True(change.HasValue);
        Assert.Equal("ABCDEF", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal("ABCDEF", GetLineText(textBuffer, 0));
    }

    [Fact]
    public void DeleteLine_OnSingleLine_ClearsTheBuffer()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        textBuffer.DeleteLine(0);

        Assert.Equal(string.Empty, textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal(0, textBuffer.TextLength);
    }

    [Fact]
    public void DeleteLine_FirstLineFromMultipleLines_RemovesThatLineAndItsLineBreak()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"A{Environment.NewLine}B{Environment.NewLine}C");

        textBuffer.DeleteLine(0);

        Assert.Equal($"B{Environment.NewLine}C", textBuffer.GetText());
        Assert.Equal(2, textBuffer.LineCount);
        Assert.Equal("B", GetLineText(textBuffer, 0));
        Assert.Equal("C", GetLineText(textBuffer, 1));
    }

    [Fact]
    public void DeleteLine_LastLine_RemovesPreviousLineBreakToo()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"A{Environment.NewLine}B");

        textBuffer.DeleteLine(1);

        Assert.Equal("A", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal("A", GetLineText(textBuffer, 0));
    }

    [Fact]
    public void DeleteRange_WithinSingleLine_RemovesCorrectText()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello World");

        textBuffer.DeleteRange(0, 5, 0, 6);

        Assert.Equal("HelloWorld", textBuffer.GetText());
    }

    [Fact]
    public void DeleteRange_AcrossLines_RemovesCorrectSpan()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"Hello{Environment.NewLine}World");

        textBuffer.DeleteRange(0, 2, 1, 3);

        Assert.Equal("Held", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
    }

    [Fact]
    public void DeleteRange_WhenArgumentsAreReversed_NormalizesThem()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        textBuffer.DeleteRange(0, 4, 0, 1);

        Assert.Equal("Ho", textBuffer.GetText());
    }

    [Fact]
    public void ReplaceSpan_ReplacesCorrectPartOfTheText()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        textBuffer.ReplaceSpan(1, 3, "i");

        Assert.Equal("Hio", textBuffer.GetText());
    }

    [Fact]
    public void ReplaceSpan_InvalidStart_ThrowsArgumentOutOfRangeException()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.ReplaceSpan(99, 1, "A"));
    }

    [Fact]
    public void ReplaceSpan_NullText_ThrowsArgumentNullException()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        Assert.Throws<ArgumentNullException>(() => textBuffer.ReplaceSpan(0, 1, null!));
    }

    [Fact]
    public void GetRange_ReturnsCorrectText()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        string result = textBuffer.GetRange(0, 1, 0, 4);

        Assert.Equal("ell", result);
    }

    [Fact]
    public void GetRange_WhenArgumentsAreReversed_NormalizesThem()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        string result = textBuffer.GetRange(0, 4, 0, 1);

        Assert.Equal("ell", result);
    }

    [Fact]
    public void GetAbsolutePosition_ReturnsCorrectIndex()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"ABC{Environment.NewLine}DEF");

        int position = textBuffer.GetAbsolutePosition(1, 2);

        Assert.Equal($"ABC{Environment.NewLine}DE".Length, position);
    }

    [Fact]
    public void GetAbsolutePosition_InvalidLine_ThrowsArgumentOutOfRangeException()
    {
        var textBuffer = new TextBuffer();

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.GetAbsolutePosition(5, 0));
    }

    [Fact]
    public void GetAbsolutePosition_InvalidColumn_ThrowsArgumentOutOfRangeException()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("ABC");

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.GetAbsolutePosition(0, 10));
    }

    [Fact]
    public void GetLineIndentEndColumn_ReturnsCorrectIndentSize()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("    hello");

        int indent = textBuffer.GetLineIndentEndColumn(0);

        Assert.Equal(4, indent);
    }

    [Fact]
    public void GetLineLength_ReturnsCorrectLength()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        int length = textBuffer.GetLineLength(0);

        Assert.Equal(5, length);
    }

    [Fact]
    public void SetText_ReplacesEntireContent()
    {
        var textBuffer = new TextBuffer();
        textBuffer.InsertText(0, 0, "Old");

        textBuffer.SetText("New");

        Assert.Equal("New", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
    }

    [Fact]
    public void SetText_Null_ThrowsArgumentNullException()
    {
        var textBuffer = new TextBuffer();

        Assert.Throws<ArgumentNullException>(() => textBuffer.SetText(null!));
    }

    [Fact]
    public void ApplyChange_AppliesTextChangeCorrectly()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Helo");

        textBuffer.ApplyChange(new TextChange(new TextSpan(2, 0), "l"));

        Assert.Equal("Hello", textBuffer.GetText());
    }

    [Fact]
    public void IncreaseVersion_IncrementsVersion()
    {
        var textBuffer = new TextBuffer();

        textBuffer.IncreaseVersion();
        textBuffer.IncreaseVersion();

        Assert.Equal(2, textBuffer.Version);
    }

    [Fact]
    public void InsertNewLineAtPosition_WhenMaxLinesReached_RaisesEventAndReturnsNull()
    {
        var textBuffer = new TextBuffer();

        string bigText = string.Join(Environment.NewLine, new string[3000].Select((_, i) => $"Line{i}"));
        textBuffer.SetText(bigText);

        bool eventRaised = false;
        textBuffer.MaxLinesReached += () => eventRaised = true;

        var change = textBuffer.InsertNewLineAtPosition(0, 0);

        Assert.False(change.HasValue);
        Assert.True(eventRaised);
        Assert.Equal(3000, textBuffer.LineCount);
    }

    [Fact]
    public void SetText_WhenTextExceedsMaxLines_ClampsToMaximumAllowedLines()
    {
        var textBuffer = new TextBuffer();

        string bigText = string.Join(Environment.NewLine, new string[3005].Select((_, i) => $"Line{i}"));

        textBuffer.SetText(bigText);

        Assert.Equal(3000, textBuffer.LineCount);
        Assert.Equal("Line0", GetLineText(textBuffer, 0));
        Assert.Equal("Line2999", GetLineText(textBuffer, 2999));
    }

    [Fact]
    public void Complex_EditSequence_ProducesCorrectFinalText()
    {
        var buffer = new TextBuffer();

        buffer.InsertText(0, 0, "Hello");
        buffer.InsertNewLineAtPosition(0, 5);
        buffer.InsertText(1, 0, "World");
        buffer.DeleteRange(0, 0, 0, 1); // remove H

        Assert.Equal($"ello{Environment.NewLine}World", buffer.GetText());
    }

    [Fact]
    public void DeleteRange_WithSameStartAndEnd_DoesNotChangeText_ButStillIncrementsVersion()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        int versionBefore = textBuffer.Version;

        textBuffer.DeleteRange(0, 2, 0, 2);

        Assert.Equal("Hello", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal(versionBefore + 1, textBuffer.Version);
    }

    [Fact]
    public void ReplaceSpan_WithZeroLength_BehavesLikeInsert()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Helo");

        textBuffer.ReplaceSpan(2, 0, "l");

        Assert.Equal("Hello", textBuffer.GetText());
        Assert.Equal("Hello", GetLineText(textBuffer, 0));
    }

    [Fact]
    public void DeleteRange_WholeBuffer_ClearsEverything()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"Hello{Environment.NewLine}World");

        textBuffer.DeleteRange(0, 0, 1, 5);

        Assert.Equal(string.Empty, textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal(0, textBuffer.TextLength);
    }

    [Fact]
    public void GetLine_InvalidLine_ThrowsArgumentOutOfRangeException()
    {
        var textBuffer = new TextBuffer();

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.GetLine(1));
    }

    [Fact]
    public void GetLineIndentEndColumn_WhenLineContainsOnlySpaces_ReturnsEntireLineLength()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("    ");

        int indent = textBuffer.GetLineIndentEndColumn(0);

        Assert.Equal(4, indent);
    }

    [Fact]
    public void GetLineIndentEndColumn_StopsAtTab_BecauseOnlySpacesCountAsIndent()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("  \tHello");

        int indent = textBuffer.GetLineIndentEndColumn(0);

        Assert.Equal(2, indent);
    }

    [Fact]
    public void InsertNewLineAtPosition_AtEndOfLine_CreatesEmptyTrailingLine()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        var change = textBuffer.InsertNewLineAtPosition(0, 5);

        Assert.True(change.HasValue);
        Assert.Equal($"Hello{Environment.NewLine}", textBuffer.GetText());
        Assert.Equal(2, textBuffer.LineCount);
        Assert.Equal("Hello", GetLineText(textBuffer, 0));
        Assert.Equal(string.Empty, GetLineText(textBuffer, 1));
    }

    [Fact]
    public void InsertNewLineAtPosition_InvalidColumn_ThrowsArgumentOutOfRangeException()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");

        Assert.Throws<ArgumentOutOfRangeException>(() => textBuffer.InsertNewLineAtPosition(0, 6));
    }

    [Fact]
    public void ApplyChange_RemovingLineBreak_MergesLines()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"A{Environment.NewLine}B");

        var firstLine = textBuffer.GetLine(0);
        int lineBreakLength = firstLine.EndIncludingLineBreak - firstLine.End;

        textBuffer.ApplyChange(new TextChange(new TextSpan(firstLine.End, lineBreakLength), string.Empty));

        Assert.Equal("AB", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal("AB", GetLineText(textBuffer, 0));
    }

    [Fact]
    public void SetText_WithExactlyMaxLines_DoesNotTruncate()
    {
        var textBuffer = new TextBuffer();

        string exactMaxText = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, 3000).Select(i => $"Line{i}"));

        textBuffer.SetText(exactMaxText);

        Assert.Equal(3000, textBuffer.LineCount);
        Assert.Equal("Line0", GetLineText(textBuffer, 0));
        Assert.Equal("Line2999", GetLineText(textBuffer, 2999));
    }

    [Fact]
    public void DeleteCrossLineRange_ThenInsertReplacement_ProducesExpectedMergedText()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"Hello{Environment.NewLine}Beautiful{Environment.NewLine}World");

        textBuffer.DeleteRange(0, 2, 2, 3); // removes "llo\nBeautiful\nWor"
        textBuffer.InsertText(0, 2, "X");

        Assert.Equal("HeXld", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
    }

    [Fact]
    public void MultipleEdits_KeepBufferStateConsistent()
    {
        var textBuffer = new TextBuffer();

        textBuffer.InsertText(0, 0, "abc");
        textBuffer.InsertNewLineAtPosition(0, 3);
        textBuffer.InsertText(1, 0, "def");
        textBuffer.ReplaceSpan(1, 2, "XYZ");
        textBuffer.MergeLineWithPrevious(1);

        Assert.Equal("aXYZdef", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
        Assert.Equal(textBuffer.GetText().Length, textBuffer.TextLength);
    }

    [Fact]
    public void DeleteRange_AcrossMultipleLines_MergesTextCorrectly()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"Hello{Environment.NewLine}Beautiful{Environment.NewLine}World");

        textBuffer.DeleteRange(0, 2, 2, 3);

        Assert.Equal("Held", textBuffer.GetText());
        Assert.Equal(1, textBuffer.LineCount);
    }
}