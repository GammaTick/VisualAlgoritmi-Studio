using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace VisualAlgoritmi_Studio.Views.Dialogs;

public static class MessageBox
{
    public static Task<MessageBoxResult> ShowAsync(
        string title,
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        var owner = GetOwnerWindow();

        var dialog = new MessageBoxDialog();
        dialog.Configure(title, message, buttons, icon);

        if (owner is not null)
        {
            return ShowDialog(dialog, owner);
        }

        // Fallback: show as a standalone window (shouldn't normally happen)
        var tcs = new TaskCompletionSource<MessageBoxResult>();
        dialog.Closed += (_, _) => tcs.TrySetResult(dialog.Result);
        dialog.Show();
        return tcs.Task;
    }

    private static async Task<MessageBoxResult> ShowDialog(MessageBoxDialog dialog, Window owner)
    {
        await dialog.ShowDialog(owner);
        return dialog.Result;
    }

    private static Window? GetOwnerWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        // Prefer the currently active (focused) window
        var active = desktop.Windows.FirstOrDefault(w => w.IsActive);
        if (active is not null)
            return active;

        // Fallback to main window
        return desktop.MainWindow;
    }
}
