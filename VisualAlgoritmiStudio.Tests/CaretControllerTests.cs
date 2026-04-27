using System;
using Avalonia.Media;
using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using Xunit;

namespace VisualAlgoritmiStudio.Tests;

public class CaretControllerTests
{
    private sealed class FakeGraphemeNavigator : IGraphemeNavigator
    {
        public int GetNextIndex(int line, ref CharacterHit characterHit)
        {
            int next = characterHit.FirstCharacterIndex + characterHit.TrailingLength + 1;
            characterHit = new CharacterHit(next);
            return next;
        }

        public int GetPreviousIndex(int line, ref CharacterHit characterHit)
        {
            int prev = Math.Max(0, characterHit.FirstCharacterIndex + characterHit.TrailingLength - 1);
            characterHit = new CharacterHit(prev);
            return prev;
        }

        public int GetPreviousIndex(int line, int column)
        {
            return Math.Max(0, column - 1);
        }

        public int SnapToBoundary(int line, int column)
        {
            return column;
        }
    }

    private static CaretController CreateController(string text)
    {
        var buffer = new TextBuffer();
        buffer.SetText(text);

        return new CaretController(buffer, new FakeGraphemeNavigator());
    }

    [Fact]
    public void NewCaret_StartsAtZero()
    {
        var caret = CreateController("Hello");

        Assert.Equal(0, caret.Line);
        Assert.Equal(0, caret.Column);
        Assert.Equal(0, caret.Version);
    }

    [Fact]
    public void SetPosition_ClampsToValidRange()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(999, 999);

        Assert.Equal(0, caret.Line);
        Assert.Equal(5, caret.Column);
    }

    [Fact]
    public void SetPosition_SamePosition_DoesNotIncreaseVersion()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(0, 2);
        int v = caret.Version;

        caret.SetPosition(0, 2);

        Assert.Equal(v, caret.Version);
    }

    [Fact]
    public void MoveRight_MovesWithinLine()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(0, 2);
        caret.MoveRight();

        Assert.Equal(3, caret.Column);
        Assert.Equal(2, caret.Version);
    }

    [Fact]
    public void MoveRight_AtEnd_MovesToNextLineStart()
    {
        var caret = CreateController($"Hello{Environment.NewLine}World");

        caret.SetPosition(0, 5);
        caret.MoveRight();

        Assert.Equal(1, caret.Line);
        Assert.Equal(0, caret.Column);
    }

    [Fact]
    public void MoveLeft_MovesWithinLine()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(0, 3);
        caret.MoveLeft();

        Assert.Equal(2, caret.Column);
    }

    [Fact]
    public void MoveLeft_AtStart_MovesToPreviousLineEnd()
    {
        var caret = CreateController($"Hello{Environment.NewLine}World");

        caret.SetPosition(1, 0);
        caret.MoveLeft();

        Assert.Equal(0, caret.Line);
        Assert.Equal(5, caret.Column);
    }

    [Fact]
    public void MoveUp_MovesToPreviousLine()
    {
        var caret = CreateController($"Hello{Environment.NewLine}World");

        caret.SetPosition(1, 2);
        caret.MoveUp();

        Assert.Equal(0, caret.Line);
        Assert.Equal(2, caret.Column);
    }

    [Fact]
    public void MoveUp_AtTop_DoesNothing()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(0, 2);
        int v = caret.Version;

        caret.MoveUp();

        Assert.Equal(0, caret.Line);
        Assert.Equal(2, caret.Column);
        Assert.Equal(v, caret.Version);
    }

    [Fact]
    public void MoveDown_MovesToNextLine()
    {
        var caret = CreateController($"Hello{Environment.NewLine}World");

        caret.SetPosition(0, 3);
        caret.MoveDown();

        Assert.Equal(1, caret.Line);
        Assert.Equal(3, caret.Column);
    }

    [Fact]
    public void MoveDown_AtBottom_DoesNothing()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(0, 2);
        int v = caret.Version;

        caret.MoveDown();

        Assert.Equal(0, caret.Line);
        Assert.Equal(2, caret.Column);
        Assert.Equal(v, caret.Version);
    }

    [Fact]
    public void MoveToLineStart_SetsColumnToZero()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(0, 3);
        caret.MoveToLineStart();

        Assert.Equal(0, caret.Column);
    }

    [Fact]
    public void MoveToLineEnd_SetsColumnToLineLength()
    {
        var caret = CreateController("Hello");

        caret.SetPosition(0, 1);
        caret.MoveToLineEnd();

        Assert.Equal(5, caret.Column);
    }

    [Fact]
    public void MoveByCharCount_ForwardAcrossLines()
    {
        var caret = CreateController($"Hello{Environment.NewLine}World");

        caret.MoveByCharCount(7);

        Assert.Equal(1, caret.Line);
        Assert.Equal(0, caret.Column);
    }

    [Fact]
    public void MoveByCharCount_BackwardAcrossLines()
    {
        var caret = CreateController($"Hello{Environment.NewLine}World");

        caret.SetPosition(1, 3);
        caret.MoveByCharCount(-5);

        Assert.Equal(0, caret.Line);
        Assert.Equal("Hello".Length, caret.Column);
    }

    [Fact]
    public void MoveByCharCount_ClampsAtDocumentStart()
    {
        var caret = CreateController("Hello");

        caret.MoveByCharCount(-100);

        Assert.Equal(0, caret.Line);
        Assert.Equal(0, caret.Column);
    }

    [Fact]
    public void MoveByCharCount_ClampsAtDocumentEnd()
    {
        var caret = CreateController("Hello");

        caret.MoveByCharCount(100);

        Assert.Equal(0, caret.Line);
        Assert.Equal(5, caret.Column);
    }

    [Fact]
    public void Version_Increases_OnEachRealMove()
    {
        var caret = CreateController("Hello");

        caret.MoveRight();
        caret.MoveRight();
        caret.MoveLeft();

        Assert.Equal(3, caret.Version);
    }

    [Fact]
    public void MoveDown_ThroughShorterLine_ThenToLongerLine_UsesPreferredColumn()
    {
        var caret = CreateController($"123456{Environment.NewLine}12{Environment.NewLine}123456789");

        caret.SetPosition(0, 5);
        caret.MoveDown(); // short line
        caret.MoveDown(); // longer line

        Assert.Equal(2, caret.Line);
        Assert.Equal(5, caret.Column);
    }

    [Fact]
    public void MoveLeftAcrossLineBoundary_ThenMoveRight_ReturnsToStartOfSecondLine()
    {
        var caret = CreateController($"Hello{Environment.NewLine}World");

        caret.SetPosition(1, 0);
        caret.MoveLeft();
        caret.MoveRight();

        Assert.Equal(1, caret.Line);
        Assert.Equal(0, caret.Column);
    }
}