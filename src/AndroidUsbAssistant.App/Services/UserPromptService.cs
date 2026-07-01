using System.Windows.Forms;
using AndroidUsbAssistant.Core.Interfaces;
using AndroidUsbAssistant.App.Forms;

namespace AndroidUsbAssistant.App.Services;

public class UserPromptService : IUserPromptService
{
    public Task<bool> PromptTetherConfirmAsync(string deviceSerial)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (SynchronizationContext.Current != null)
        {
            SynchronizationContext.Current.Post(_ =>
            {
                using var form = new TetherPromptForm(deviceSerial);
                var result = form.ShowDialog();
                tcs.SetResult(result == DialogResult.Yes);
            }, null);
        }
        else
        {
            using var form = new TetherPromptForm(deviceSerial);
            var result = form.ShowDialog();
            tcs.SetResult(result == DialogResult.Yes);
        }

        return tcs.Task;
    }
    public Task<bool> PromptYesNoAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        if (SynchronizationContext.Current != null)
        {
            SynchronizationContext.Current.Post(_ =>
            {
                var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                tcs.SetResult(result == DialogResult.Yes);
            }, null);
        }
        else
        {
            var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            tcs.SetResult(result == DialogResult.Yes);
        }

        return tcs.Task;
    }
}
