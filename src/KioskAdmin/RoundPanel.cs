using System.Drawing.Drawing2D;

namespace KioskAdmin;

public sealed class RoundPanel : Panel
{
    public Color BorderColor { get; set; } = Color.FromArgb(229, 231, 235);
    public int CornerRadius { get; set; } = 10;
    public bool DrawBorder { get; set; } = true;

    public RoundPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedPath(rect, CornerRadius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        if (DrawBorder)
        {
            using var pen = new Pen(BorderColor, 1.5f);
            e.Graphics.DrawPath(pen, path);
        }
    }

    private static GraphicsPath RoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0) { path.AddRectangle(rect); return path; }
        var r = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2);
        path.AddArc(rect.X, rect.Y, r, r, 180, 90);
        path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
        path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
        path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
        path.CloseFigure();
        return path;
    }
}
