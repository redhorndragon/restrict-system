using System.Runtime.InteropServices;

namespace KioskLauncher;

public sealed class KioskForm : Form
{
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_TASKLIST = 0xF130;
    private const int SC_KEYMENU = 0xF100;
    private const int SC_MOVE = 0xF010;
    private const int SC_MINIMIZE = 0xF020;
    private const int SC_CLOSE = 0xF060;

    public KioskForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        WindowState = FormWindowState.Maximized;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        BackColor = Color.Black;
        Cursor.Hide();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_SYSCOMMAND)
        {
            var cmd = m.WParam.ToInt32() & 0xFFF0;
            if (cmd == SC_TASKLIST || cmd == SC_KEYMENU || cmd == SC_MOVE ||
                cmd == SC_MINIMIZE || cmd == SC_CLOSE) return;
        }
        base.WndProc(ref m);
    }

    protected override void OnClosed(EventArgs e)
    {
        Cursor.Show();
        base.OnClosed(e);
    }
}
