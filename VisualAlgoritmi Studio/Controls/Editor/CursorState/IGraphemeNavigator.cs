using Avalonia.Media;

namespace VisualAlgoritmi_Studio.Controls.Editor.CursorState
{
    internal interface IGraphemeNavigator
    {
        int GetNextIndex(int line, ref CharacterHit characterHit);
        int GetPreviousIndex(int line, ref CharacterHit characterHit);
        int GetPreviousIndex(int line, int column);
        int SnapToBoundary(int line, int column);
    }
}