using System.Windows.Forms;
using AndroidUsbAssistant.Core.Interfaces;

namespace AndroidUsbAssistant.App.Services;

public class UserPromptService : IUserPromptService
{
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
