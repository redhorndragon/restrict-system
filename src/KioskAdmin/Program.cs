using System.Security.Principal;

namespace KioskAdmin;

static class Program
{
    private static readonly Mutex SingleInstance = new(true, "Local\\KioskAdmin_SingleInstance");

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            MessageBox.Show("本工具需要管理员权限。请以 gbr 管理员账户运行。", "权限不足",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!SingleInstance.WaitOne(TimeSpan.Zero, true)) return;

        try
        {
            Application.Run(new MainForm(identity.Name));
        }
        finally
        {
            SingleInstance.ReleaseMutex();
        }
    }
}
