using VisualAlgoritmi_Studio.Controls.Editor.CursorState;
using VisualAlgoritmi_Studio.Controls.Editor.Text;
using Xunit;

namespace VisualAlgoritmiStudio.Tests;

public class SelectionControllerTests
{
    [Fact]
    public void NewController_HasNoSelection_AndVersionIsZero()
    {
        var textBuffer = new TextBuffer();
        var selectionController = new SelectionController(textBuffer);

        Assert.False(selectionController.HasSelection);
        Assert.Equal(0, selectionController.Version);
        Assert.Equal((0, 0, 0, 0), selectionController.GetRawPositions());
    }

    [Fact]
    public void BeginSelection_SetsAnchorAndActiveToSamePosition()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 3);

        Assert.False(selectionController.HasSelection);
        Assert.Equal((0, 3, 0, 3), selectionController.GetRawPositions());
        Assert.Equal(1, selectionController.Version);
    }

    [Fact]
    public void ExtendTo_DifferentPosition_CreatesSelection()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 1);
        selectionController.ExtendTo(0, 4);

        Assert.True(selectionController.HasSelection);
        Assert.Equal((0, 1, 0, 4), selectionController.GetRawPositions());
        Assert.Equal((0, 1, 0, 4), selectionController.GetNormalizedSelection());
        Assert.Equal(2, selectionController.Version);
    }

    [Fact]
    public void GetNormalizedSelection_WhenSelectionIsBackward_ReturnsOrderedPositions()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 4);
        selectionController.ExtendTo(0, 1);

        Assert.True(selectionController.HasSelection);
        Assert.Equal((0, 1, 0, 4), selectionController.GetNormalizedSelection());
    }

    [Fact]
    public void CollapseTo_SamePosition_DoesNotIncreaseVersion()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.CollapseTo(0, 2);
        int versionAfterFirstCollapse = selectionController.Version;

        selectionController.CollapseTo(0, 2);

        Assert.Equal(versionAfterFirstCollapse, selectionController.Version);
        Assert.False(selectionController.HasSelection);
        Assert.Equal((0, 2, 0, 2), selectionController.GetRawPositions());
    }

    [Fact]
    public void CollapseTo_NewPosition_ResetsSelectionToSingleCaret()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 1);
        selectionController.ExtendTo(0, 4);

        selectionController.CollapseTo(0, 3);

        Assert.False(selectionController.HasSelection);
        Assert.Equal((0, 3, 0, 3), selectionController.GetRawPositions());
        Assert.Equal((0, 3, 0, 3), selectionController.GetNormalizedSelection());
    }

    [Fact]
    public void CollapseTo_ClampsLineAndColumnToValidRange()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.CollapseTo(100, 100);

        Assert.Equal((0, 5, 0, 5), selectionController.GetRawPositions());
    }

    [Fact]
    public void BeginSelection_ClampsNegativeLineAndColumnToZero()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(-10, -5);

        Assert.Equal((0, 0, 0, 0), selectionController.GetRawPositions());
        Assert.Equal(1, selectionController.Version);
    }

    [Fact]
    public void ExtendTo_SamePosition_DoesNotIncreaseVersion()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 2);
        int versionBefore = selectionController.Version;

        selectionController.ExtendTo(0, 2);

        Assert.Equal(versionBefore, selectionController.Version);
        Assert.False(selectionController.HasSelection);
    }

    [Fact]
    public void ExtendTo_ClampsToEndOfLastLine()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 0);
        selectionController.ExtendTo(50, 999);

        Assert.Equal((0, 0, 0, 5), selectionController.GetRawPositions());
        Assert.True(selectionController.HasSelection);
    }

    [Fact]
    public void SelectAll_SelectsEntireSingleLineBuffer()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("Hello");
        var selectionController = new SelectionController(textBuffer);

        selectionController.SelectAll();

        Assert.True(selectionController.HasSelection);
        Assert.Equal((0, 0, 0, 5), selectionController.GetRawPositions());
        Assert.Equal((0, 0, 0, 5), selectionController.GetNormalizedSelection());
        Assert.Equal(1, selectionController.Version);
    }

    [Fact]
    public void SelectAll_SelectsEntireMultiLineBuffer()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"Hello{Environment.NewLine}World");
        var selectionController = new SelectionController(textBuffer);

        selectionController.SelectAll();

        Assert.True(selectionController.HasSelection);
        Assert.Equal((0, 0, 1, 5), selectionController.GetRawPositions());
        Assert.Equal((0, 0, 1, 5), selectionController.GetNormalizedSelection());
    }

    [Fact]
    public void SetRawPositions_SetsPositionsAndIncreasesVersion()
    {
        var textBuffer = new TextBuffer();
        var selectionController = new SelectionController(textBuffer);

        selectionController.SetRawPositions(2, 3, 4, 5);

        Assert.Equal((2, 3, 4, 5), selectionController.GetRawPositions());
        Assert.True(selectionController.HasSelection);
        Assert.Equal(1, selectionController.Version);
    }

    [Fact]
    public void SetRawPositions_BackwardPositions_AreNormalizedByGetNormalizedSelection()
    {
        var textBuffer = new TextBuffer();
        var selectionController = new SelectionController(textBuffer);

        selectionController.SetRawPositions(3, 7, 1, 2);

        Assert.Equal((1, 2, 3, 7), selectionController.GetNormalizedSelection());
    }

    [Fact]
    public void Version_Increases_OnEachRealMutation()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText($"Hello{Environment.NewLine}World");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 1);
        selectionController.ExtendTo(1, 2);
        selectionController.CollapseTo(0, 0);
        selectionController.SelectAll();

        Assert.Equal(4, selectionController.Version);
    }

    [Fact]
    public void ExtendSelection_PastAnchorInOppositeDirection_NormalizesCorrectly()
    {
        var textBuffer = new TextBuffer();
        textBuffer.SetText("0123456789");
        var selectionController = new SelectionController(textBuffer);

        selectionController.BeginSelection(0, 5);
        selectionController.ExtendTo(0, 8);
        selectionController.ExtendTo(0, 2);

        Assert.True(selectionController.HasSelection);
        Assert.Equal((0, 2, 0, 5), selectionController.GetNormalizedSelection());
        Assert.Equal((0, 5, 0, 2), selectionController.GetRawPositions());
    }
}